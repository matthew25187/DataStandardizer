using System;
using System.Collections.Concurrent;
using System.ComponentModel;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The process-wide cache of mappers built from <see cref="CsvFieldMappingAttribute"/> attributes on record line types, of the type converters
    /// those mappings name, and of the other details the package works out once per record line and property type.
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

        // Field value accessors keyed by the reader's or writer's record-line type, then property type.  Held here rather than in a static of the
        // generic reader or writer, so that clearing the cache also releases a property type when the record-line type itself is not unloaded.
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, object>> FieldValueAccessors =
            new ConcurrentDictionary<Type, ConcurrentDictionary<Type, object>>();

        /// <summary>
        /// Removes every cached entry: the mappers built from attributes, the type converters, and the property details of each record line type.
        /// </summary>
        /// <remarks>
        /// Readers, writers and record lines build the entries they need again on first use, so clearing the cache never changes how a line is read
        /// or written.  Clear it to release the memory the entries hold, or the record line, property and type converter types they refer to, for
        /// example before unloading a collectible <c>AssemblyLoadContext</c> that defines them.  A reader or writer that is already open keeps the
        /// mapper it resolved, so dispose of those first.
        /// </remarks>
        public static void Clear()
        {
            DeclarativeMappers.Clear();
            TypeConverters.Clear();
            FieldValueAccessors.Clear();
            CsvExtensions.ClearPropertyBindings();
            CsvFileLineBase.ClearPropertyKeys();
        }

        /// <summary>
        /// Gets the declarative mapper of a record line's own type, building and caching it if necessary.
        /// </summary>
        /// <param name="recordLine">The record line, whose runtime type the mapper is built from and keyed by.</param>
        /// <remarks>
        /// A type without mapping attributes gets an empty mapper, which is cached like any other.  Mappers are read-only, so sharing them is
        /// thread-safe.
        /// </remarks>
        internal static ICsvFileMapper GetOrAddDeclarativeMapper(CsvFileRecordLine recordLine)
        {
            // A declarative mapper is built from the line's own attributes, so key it by the line's runtime type rather than the reader's or writer's
            // record-line type, which may be a base of it.
            var lineType = recordLine.GetType();
            if (DeclarativeMappers.TryGetValue(lineType, out var mapper))
            {
                return mapper;
            }

            // Another thread may have added a mapper first; use whichever one won so all callers share an instance.
            return DeclarativeMappers.GetOrAdd(lineType, recordLine.CreateMapper());
        }

        /// <summary>
        /// Gets the field value accessor of a property type for a record-line type, creating and caching it if necessary.
        /// </summary>
        internal static object GetOrAddFieldValueAccessor(Type recordLineType, Type propertyType, Func<Type, object> accessorFactory)
        {
            if (!FieldValueAccessors.TryGetValue(recordLineType, out var accessors))
            {
                accessors = FieldValueAccessors.GetOrAdd(recordLineType, _ => new ConcurrentDictionary<Type, object>());
            }

            if (accessors.TryGetValue(propertyType, out var accessor))
            {
                return accessor;
            }

            return accessors.GetOrAdd(propertyType, accessorFactory);
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
