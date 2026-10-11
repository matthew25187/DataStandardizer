using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.CSV
{
    public abstract class CsvFileIoBase<TRecordLine> : CsvFileCacheRepositoryBase where TRecordLine : CsvFileRecordLine
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private ICsvFileMapper? _resolvedMapper;
#else
        [CanBeNull] private ICsvFileMapper _resolvedMapper;
#endif
        private int _resolvedMapperVersion;
        private bool _isDisposed;

        protected CsvFileIoBase()
        {
        }

        /// <summary>
        /// Register a mapper for CSV lines.
        /// </summary>
        /// <typeparam name="TMapper">Type of the mapper.</typeparam>
        /// <exception cref="ObjectDisposedException">The reader or writer has been disposed.</exception>
        public void RegisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            ThrowIfDisposed();

            AddImperativeMapper(typeof(TRecordLine), () => new TMapper());
        }

        /// <summary>
        /// Unregister a mapper for CSV lines.
        /// </summary>
        /// <typeparam name="TMapper">Type of the mapper.</typeparam>
        /// <exception cref="ObjectDisposedException">The reader or writer has been disposed.</exception>
        public void UnregisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            ThrowIfDisposed();

            RemoveImperativeMapper(typeof(TRecordLine));
        }

        /// <summary>
        /// Marks the reader or writer as disposed.
        /// </summary>
        /// <returns><c>true</c> if this call disposed the object; <c>false</c> if it had already been disposed.</returns>
        private protected bool MarkDisposed()
        {
            if (_isDisposed) return false;

            _isDisposed = true;
            return true;
        }

        private protected void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        protected CsvFileException BuildException(string message, IDictionary<string, object> dataItems)
        {
            var exception = new CsvFileException(message);
            foreach (var dataItem in dataItems)
            {
                exception.Data.Add(dataItem.Key, dataItem.Value);
            }

            return exception;
        }

        protected bool DoesFieldValueRequireQuoting(string rawFieldValue, CsvFileOptions options)
        {
            var quotingRequired = false;
            switch (options.QuoteHandling)
            {
                case CsvFieldQuoteHandling.Always:
                    quotingRequired = true;
                    break;
                case CsvFieldQuoteHandling.Auto:
                    if (!string.IsNullOrWhiteSpace(rawFieldValue) && !long.TryParse(rawFieldValue, out _) && !decimal.TryParse(rawFieldValue, out _))
                        quotingRequired = true;
                    break;
                case CsvFieldQuoteHandling.Required:
                    // ref. RFC 4180§2¶6
                    if (options.EmbeddedLineBreak is null && StandardLineBreaks.Any(rawFieldValue.Contains))
                        quotingRequired = true;
                    else if (options.EmbeddedLineBreak != null && rawFieldValue.Contains(options.EmbeddedLineBreak))
                        quotingRequired = true;
                    else if (rawFieldValue.Contains('"'))
                        quotingRequired = true;
                    else if (rawFieldValue.Contains(options.FieldDelimiterCharacter))
                        quotingRequired = true;

                    break;
            }

            return quotingRequired;
        }

#if NET5_0_OR_GREATER
        protected T? DeserializeCsvLineFieldValue<T>(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue)
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        [return: System.Diagnostics.CodeAnalysis.MaybeNull]
        protected T DeserializeCsvLineFieldValue<T>(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue)
#else
        [CanBeNull]
        protected T DeserializeCsvLineFieldValue<T>([CanBeNull] CsvFileHeaderLine headerLine, [NotNull] TRecordLine recordLine, [NotNull] CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
            [CanBeNull] object fieldValue)
#endif
        {
            if (fieldValue is null)
            {
                return default; // if there is no field value then there is nothing to deserialize
            }
            else if (fieldValue is T deserializedFieldValue)
            {
                return deserializedFieldValue; // if the field value is already of the target type then just return it
            }

            // Deserialize the field value to the field type.
            var serializedFieldValue = fieldValue as string;
            if (serializedFieldValue is null)
            {
                return default; // if the field value is not a string at this point then it can't be deserialized
            }

            // Attempt to convert the field value to the target data type.
#if NET5_0_OR_GREATER
            T? result = default;
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            T result = default!;
#else
            T result = default;
#endif
            if (propertyFieldMapping.Value.FromStringConverter != null)
            {
                var converter = (CsvFieldConvertFromString<TRecordLine, T>)propertyFieldMapping.Value.FromStringConverter;

                var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping);
                var context = new CsvFieldContext<TRecordLine>(options)
                {
                    HeaderLine = headerLine,
                    Model = recordLine,
                    FieldName = mappedFieldName,
                    FieldIndex = propertyFieldMapping.Value.FieldIndex
                };
                result = converter(context);
            }
            else if (propertyFieldMapping.Value.TypeConverterType != null)
            {
                var typeConverter = GetTypeConverter(propertyFieldMapping.Value.TypeConverterType);
                if ((typeConverter?.CanConvertFrom(typeof(string))).GetValueOrDefault())
                {
                    result = options.Culture != null
#if NET5_0_OR_GREATER
                        ? (T?)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)
                        : (T?)typeConverter?.ConvertFromInvariantString(serializedFieldValue);
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                        ? (T)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)!
                        : (T)typeConverter?.ConvertFromInvariantString(serializedFieldValue)!;
#else
                        ? (T)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)
                        : (T)typeConverter?.ConvertFromInvariantString(serializedFieldValue);
#endif
                }
            }

            return result;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        protected object? GetCsvLineMappedFieldValue(TRecordLine recordLine, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping) 
        #else
        [CanBeNull]
        protected object GetCsvLineMappedFieldValue([NotNull] TRecordLine recordLine, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
#endif
        {
            ICsvFileLine csvLine = recordLine;

            // Get the lookup key for the field on the CSV line.
            var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping);
            var fieldKey = mappedFieldName;
            if (!csvLine.Contains(fieldKey) && csvLine.Contains(propertyFieldMapping.Key))
            {
                fieldKey = propertyFieldMapping.Key;
            }

            // Get the mapped field value.
            var fieldValue = csvLine[fieldKey];
            if (propertyFieldMapping.Value.ConstantValue != null)
            {
                fieldValue = propertyFieldMapping.Value.ConstantValue;
            }
            else if (propertyFieldMapping.Value.VariableValueGenerator != null)
            {
                fieldValue = FieldValueAccessor.For(propertyFieldMapping.Value.PropertyType).Generate(propertyFieldMapping.Value.VariableValueGenerator);
            }

            return fieldValue;
        }

        protected ICsvFileMapper GetMapper(TRecordLine recordLine)
        {
            // Reuse the mapper this instance last resolved, so the per-line path does not take the cache lock, unless a
            // mapper has since been registered, unregistered or cleared.
            var mapperCacheVersion = MapperCacheVersion;
            var mapper = _resolvedMapper;
            if (mapper != null && _resolvedMapperVersion == mapperCacheVersion)
            {
                return mapper;
            }

            mapper = GetOrAddMapper(typeof(TRecordLine), recordLine, out var isCached);
            _resolvedMapper = isCached ? mapper : null;
            _resolvedMapperVersion = mapperCacheVersion;

            return mapper;
        }

        protected string NormalizeFieldValueLineBreaks(string fieldValue, ICsvFileOptions options)
        {
            var hasTrailingLineBreak = fieldValue.TrimEnd('\n', '\r').Length < fieldValue.Length;

            var fieldValueBuilder = new StringBuilder();
            using (var lineReader = new StringReader(fieldValue))
            using (var lineWriter = new StringWriter(fieldValueBuilder))
            {
                var line = lineReader.ReadLine();
                while (line != null)
                {
                    lineWriter.Write(line);

                    line = lineReader.ReadLine();

                    if (line != null)
                    {
                        lineWriter.Write(options.EmbeddedLineBreak);
                    }
                }

                if (hasTrailingLineBreak)
                {
                    lineWriter.Write(options.EmbeddedLineBreak);
                }
            }

            return fieldValueBuilder.ToString();
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        protected string? SerializeCsvLineFieldValue(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue) 
        #else
        [CanBeNull]
        protected string SerializeCsvLineFieldValue([CanBeNull] CsvFileHeaderLine headerLine, [NotNull] TRecordLine recordLine, [NotNull] CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
            [CanBeNull] object fieldValue)
#endif
        {
            if (fieldValue is null)
            {
                return null; // if there is no field value then there is nothing to serialize
            }
            else if (fieldValue is string serializedFieldValue)
            {
                return serializedFieldValue; // if the field value is already of the target type then just return it
            }

            // Attempt to convert the field value to the target data type.
            var result = options.Culture != null && fieldValue is IFormattable formattableFieldValue 
                ? formattableFieldValue.ToString(null, options.Culture) 
                : fieldValue.ToString();

            if (!string.IsNullOrEmpty(propertyFieldMapping.Key))
            {
                var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping);
                if (propertyFieldMapping.Value.ToStringConverter is CsvFieldConvertToString<TRecordLine> converter)
                {
                    var context = new CsvFieldContext<TRecordLine>(options)
                    {
                        HeaderLine = headerLine,
                        Model = recordLine,
                        FieldName = mappedFieldName,
                        FieldIndex = propertyFieldMapping.Value.FieldIndex
                    };
                    result = converter(context);
                }
                else if (propertyFieldMapping.Value.TypeConverterType != null)
                {
                    var typeConverter = GetTypeConverter(propertyFieldMapping.Value.TypeConverterType);
                    if ((typeConverter?.CanConvertTo(typeof(string))).GetValueOrDefault())
                    {
                        result = options.Culture != null
                            ? typeConverter?.ConvertToString(null, options.Culture, fieldValue)
                            : typeConverter?.ConvertToInvariantString(fieldValue);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Calls the generic field value members for a property type known only at run time, without reflection on each call.
        /// </summary>
        /// <remarks>
        /// One accessor is created per property type and shared by every reader and writer.  Accessors hold no state, so sharing them is thread-safe.
        /// </remarks>
        private protected abstract class FieldValueAccessor
        {
            private static readonly ConcurrentDictionary<Type, FieldValueAccessor> AccessorCache = new ConcurrentDictionary<Type, FieldValueAccessor>();

            internal static FieldValueAccessor For(Type propertyType)
            {
                if (AccessorCache.TryGetValue(propertyType, out var accessor))
                {
                    return accessor;
                }

                return AccessorCache.GetOrAdd(propertyType, CreateAccessor);
            }

            private static FieldValueAccessor CreateAccessor(Type propertyType)
            {
                var accessorType = typeof(CsvFileIoBase<>.FieldValueAccessor<>).MakeGenericType(typeof(TRecordLine), propertyType);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                return (FieldValueAccessor)Activator.CreateInstance(accessorType)!;
#else
                return (FieldValueAccessor)Activator.CreateInstance(accessorType);
#endif
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal abstract object? Deserialize(CsvFileIoBase<TRecordLine> io, CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
                object? fieldValue);

            internal abstract object? Generate(Delegate generator);
#else
            [CanBeNull]
            internal abstract object Deserialize([NotNull] CsvFileIoBase<TRecordLine> io, [CanBeNull] CsvFileHeaderLine headerLine, [NotNull] TRecordLine recordLine, [NotNull] CsvFileOptions options,
                KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, [CanBeNull] object fieldValue);

            [CanBeNull]
            internal abstract object Generate([NotNull] Delegate generator);
#endif
        }

        private sealed class FieldValueAccessor<T> : FieldValueAccessor
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal override object? Deserialize(CsvFileIoBase<TRecordLine> io, CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
                object? fieldValue)
            {
                return io.DeserializeCsvLineFieldValue<T>(headerLine, recordLine, options, propertyFieldMapping, fieldValue);
            }

            internal override object? Generate(Delegate generator)
            {
                return ((CsvFieldGenerate<T>)generator)();
            }
#else
            internal override object Deserialize(CsvFileIoBase<TRecordLine> io, CsvFileHeaderLine headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
                object fieldValue)
            {
                return io.DeserializeCsvLineFieldValue<T>(headerLine, recordLine, options, propertyFieldMapping, fieldValue);
            }

            internal override object Generate(Delegate generator)
            {
                return ((CsvFieldGenerate<T>)generator)();
            }
#endif
        }
    }
}