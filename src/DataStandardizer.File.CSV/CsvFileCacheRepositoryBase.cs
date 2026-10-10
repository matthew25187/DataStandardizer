using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
#if NET9_0_OR_GREATER
using System.Threading;
#endif
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.File.CSV
{
    public abstract class CsvFileCacheRepositoryBase
    {
        /// <summary>
        /// Mappers built from declarative configuration on record lines, keyed by record-line type.
        /// </summary>
        /// <remarks>
        /// Access from within this package is synchronised.  Direct access from a subclass is not synchronised, so must
        /// not happen while readers or writers are in use on other threads (see issue #143).
        /// </remarks>
        protected internal static readonly Dictionary<Type, ICsvFileMapper> DeclarativeMapperCache = new Dictionary<Type, ICsvFileMapper>();

        /// <summary>
        /// Mappers registered through <c>RegisterMapper</c>, keyed by record-line type.
        /// </summary>
        /// <remarks>
        /// Access from within this package is synchronised.  Direct access from a subclass is not synchronised, so must
        /// not happen while readers or writers are in use on other threads (see issue #143).
        /// </remarks>
        protected internal static readonly Dictionary<Type, ICsvFileMapper> ImperativeMapperCache = new Dictionary<Type, ICsvFileMapper>();
        protected static readonly string[] StandardLineBreaks = new[] { "\n", "\r", "\r\n" };
        private static readonly ConcurrentDictionary<Type, TypeConverter> TypeConverterCache = new ConcurrentDictionary<Type, TypeConverter>();

        // Guards both mapper caches.  Never pass or convert it as object, or the lock statement falls back to Monitor.
#if NET9_0_OR_GREATER
        private static readonly Lock MapperCacheLock = new Lock();
#else
        private static readonly object MapperCacheLock = new object();
#endif

        // Incremented whenever a mapper is registered, unregistered or cleared, so readers and writers know to discard the mapper they resolved.
        private static volatile int _mapperCacheVersion;

        internal static int MapperCacheVersion => _mapperCacheVersion;

        internal static IReadOnlyDictionary<Type, ICsvFileMapper> ImperativeMappers { get; } = new SynchronizedMapperView(ImperativeMapperCache);

        internal static void ClearMapperCaches()
        {
            lock (MapperCacheLock)
            {
                ImperativeMapperCache.Clear();
                DeclarativeMapperCache.Clear();
                _mapperCacheVersion++;
            }
        }

        internal static void AddImperativeMapper(Type recordLineType, Func<ICsvFileMapper> mapperFactory)
        {
            lock (MapperCacheLock)
            {
                if (ImperativeMapperCache.TryGetValue(recordLineType, out _))
                {
                    return;
                }

                ImperativeMapperCache.Add(recordLineType, mapperFactory());
                _mapperCacheVersion++;
            }
        }

        internal static void RemoveImperativeMapper(Type recordLineType)
        {
            lock (MapperCacheLock)
            {
                if (ImperativeMapperCache.Remove(recordLineType))
                {
                    _mapperCacheVersion++;
                }
            }
        }

        internal static ICsvFileMapper GetOrAddMapper(Type recordLineType, CsvFileRecordLine recordLine, out bool isCached)
        {
            lock (MapperCacheLock)
            {
                if (ImperativeMapperCache.TryGetValue(recordLineType, out var imperativeMapper))
                {
                    isCached = true;
                    return imperativeMapper;
                }

                if (DeclarativeMapperCache.TryGetValue(recordLineType, out var declarativeMapper))
                {
                    isCached = true;
                    return declarativeMapper;
                }

                var mapper = recordLine.CreateMapper();
                isCached = mapper.Count > 0;
                if (isCached)
                {
                    DeclarativeMapperCache.Add(recordLineType, mapper);
                }

                return mapper;
            }
        }

#if NETCOREAPP3_0_OR_GREATER
        protected TypeConverter? GetTypeConverter(string typeName)
#else
        [CanBeNull]
        protected TypeConverter GetTypeConverter(string typeName)
#endif
        {
            var typeConverterType = Type.GetType(typeName);
            if (typeConverterType is null)
            {
                return null;
            }

            return GetTypeConverter(typeConverterType);
        }

#if NETCOREAPP3_0_OR_GREATER
        protected TypeConverter? GetTypeConverter(Type typeConverterType)
#else
        [CanBeNull]
        protected TypeConverter GetTypeConverter(Type typeConverterType)
#endif
        {
            if (TypeConverterCache.TryGetValue(typeConverterType, out var typeConverter))
            {
                return typeConverter;
            }

            typeConverter = Activator.CreateInstance(typeConverterType) as TypeConverter;
            if (typeConverter is null)
            {
                return null;
            }

            // Another thread may have added a converter first; use whichever one won so all callers share an instance.
            return TypeConverterCache.GetOrAdd(typeConverterType, typeConverter);
        }

        /// <summary>
        /// Read-only, live view of a mapper cache that takes the mapper cache lock on every access.
        /// </summary>
        private sealed class SynchronizedMapperView : IReadOnlyDictionary<Type, ICsvFileMapper>
        {
            private readonly Dictionary<Type, ICsvFileMapper> _mappers;

            internal SynchronizedMapperView(Dictionary<Type, ICsvFileMapper> mappers)
            {
                _mappers = mappers;
            }

            public int Count
            {
                get
                {
                    lock (MapperCacheLock)
                    {
                        return _mappers.Count;
                    }
                }
            }

            public ICsvFileMapper this[Type key]
            {
                get
                {
                    lock (MapperCacheLock)
                    {
                        return _mappers[key];
                    }
                }
            }

            public IEnumerable<Type> Keys
            {
                get
                {
                    lock (MapperCacheLock)
                    {
                        return new List<Type>(_mappers.Keys);
                    }
                }
            }

            public IEnumerable<ICsvFileMapper> Values
            {
                get
                {
                    lock (MapperCacheLock)
                    {
                        return new List<ICsvFileMapper>(_mappers.Values);
                    }
                }
            }

            public bool ContainsKey(Type key)
            {
                lock (MapperCacheLock)
                {
                    return _mappers.ContainsKey(key);
                }
            }

#if NETCOREAPP3_0_OR_GREATER
            public bool TryGetValue(Type key, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out ICsvFileMapper value)
#else
            public bool TryGetValue(Type key, out ICsvFileMapper value)
#endif
            {
                lock (MapperCacheLock)
                {
                    return _mappers.TryGetValue(key, out value);
                }
            }

            // Enumerate a snapshot so the lock is not held while the caller iterates.
            public IEnumerator<KeyValuePair<Type, ICsvFileMapper>> GetEnumerator()
            {
                List<KeyValuePair<Type, ICsvFileMapper>> snapshot;
                lock (MapperCacheLock)
                {
                    snapshot = new List<KeyValuePair<Type, ICsvFileMapper>>(_mappers);
                }

                return snapshot.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
