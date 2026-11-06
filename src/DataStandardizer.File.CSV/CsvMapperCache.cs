using System;
using System.Collections.Concurrent;
using System.ComponentModel;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The process-wide cache of mappers built from <see cref="CsvFieldMappingAttribute"/> attributes on record line types, and of the type converters
    /// those mappings name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every entry is worked out from a type alone, so readers and writers on any thread share them.  The cache is safe to use from several threads at
    /// once.
    /// </para>
    /// <para>
    /// Mappers registered with <see cref="CsvFileIoBase{TRecordLine}.RegisterMapper{TMapper}"/> are not held here: each reader or writer keeps its own.
    /// </para>
    /// </remarks>
    public static class CsvMapperCache
    {
        private static readonly ConcurrentDictionary<Type, ICsvFileMapper> DeclarativeMappers = new ConcurrentDictionary<Type, ICsvFileMapper>();
        private static readonly ConcurrentDictionary<Type, TypeConverter> TypeConverters = new ConcurrentDictionary<Type, TypeConverter>();

        /// <summary>
        /// Removes every cached mapper and type converter.
        /// </summary>
        /// <remarks>
        /// Readers and writers build the entries they need again on first use, so clearing the cache never changes how a line is read or written.
        /// Clear it to release the memory the entries hold, or the record line and type converter types they refer to, for example before unloading
        /// a collectible <c>AssemblyLoadContext</c> that defines them.  A reader or writer that is already open keeps the mapper it resolved.
        /// </remarks>
        public static void Clear()
        {
            DeclarativeMappers.Clear();
            TypeConverters.Clear();
        }

        /// <summary>
        /// Gets the declarative mapper of a record line's own type, building and caching it if necessary.
        /// </summary>
        /// <param name="recordLine">The record line, whose runtime type the mapper is built from and keyed by.</param>
        /// <param name="isCached"><c>true</c> if the mapper is held in the cache; <c>false</c> if the line's type has no mappings, so the mapper was
        /// created from <paramref name="recordLine"/> and is empty.</param>
        internal static ICsvFileMapper GetOrAddDeclarativeMapper(CsvFileRecordLine recordLine, out bool isCached)
        {
            // A declarative mapper is built from the line's own attributes, so key it by the line's runtime type rather than the reader's or writer's
            // record-line type, which may be a base of it.
            var lineType = recordLine.GetType();
            if (DeclarativeMappers.TryGetValue(lineType, out var mapper))
            {
                isCached = true;
                return mapper;
            }

            mapper = recordLine.CreateMapper();
            isCached = mapper.Count > 0;
            if (!isCached)
            {
                return mapper;
            }

            // Another thread may have added a mapper first; use whichever one won so all callers share an instance.
            return DeclarativeMappers.GetOrAdd(lineType, mapper);
        }

        /// <summary>
        /// Gets the shared instance of a type converter, creating and caching it if necessary.
        /// </summary>
        /// <returns>The type converter, or <c>null</c> if <paramref name="typeConverterType"/> is not a <see cref="TypeConverter"/>.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static TypeConverter? GetTypeConverter(Type typeConverterType)
#else
        [return: MaybeNull]
        internal static TypeConverter GetTypeConverter(Type typeConverterType)
#endif
        {
            if (TypeConverters.TryGetValue(typeConverterType, out var typeConverter))
            {
                return typeConverter;
            }

            typeConverter = Activator.CreateInstance(typeConverterType) as TypeConverter;
            if (typeConverter is null)
            {
                return null;
            }

            // Another thread may have added a converter first; use whichever one won so all callers share an instance.
            return TypeConverters.GetOrAdd(typeConverterType, typeConverter);
        }
    }
}
