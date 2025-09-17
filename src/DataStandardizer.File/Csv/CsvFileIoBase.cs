using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.Csv
{
    public abstract class CsvFileIoBase<TRecordLine> : CsvFileCacheRepositoryBase where TRecordLine : CsvFileRecordLine
    {
        /// <summary>
        /// Register a mapper for CSV lines.
        /// </summary>
        /// <typeparam name="TMapper">Type of the mapper.</typeparam>
        public void RegisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            if (ImperativeMapperCache.ContainsKey(typeof(TMapper)))
            {
                return;
            }

            var mapper = new TMapper();
            ImperativeMapperCache.Add(typeof(TRecordLine), mapper);
        }

        /// <summary>
        /// Unregister a mapper for CSV lines.
        /// </summary>
        /// <typeparam name="TMapper">Type of the mapper.</typeparam>
        public void UnregisterMapper<TMapper>() where TMapper : CsvFileMapperBase<TRecordLine>, new()
        {
            if (ImperativeMapperCache.ContainsKey(typeof(TRecordLine)))
            {
                ImperativeMapperCache.Remove(typeof(TRecordLine));
            }
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

#if NETCOREAPP3_0_OR_GREATER
        protected T? DeserializeCsvLineFieldValue<T>(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, object? fieldValue)
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
#if NETCOREAPP3_0_OR_GREATER
            T? result = default;
#else
            T result = default;
#endif
            if (propertyFieldMapping.Value.FromStringConverter != null)
            {
                var converterMethodDefinition = this.GetType().GetTypeInfo().DeclaredMethods
                    .Single(method => method.Name == nameof(GetConvertedFieldValue))
                    .GetGenericMethodDefinition();
                var converterMethod = converterMethodDefinition.MakeGenericMethod(typeof(T));

                var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping);
                var context = new CsvFieldContext<TRecordLine>(options)
                {
                    HeaderLine = headerLine,
                    Model = recordLine,
                    FieldName = mappedFieldName,
                    FieldIndex = propertyFieldMapping.Value.FieldIndex
                };
#if NETCOREAPP3_0_OR_GREATER
                result = (T?)converterMethod.Invoke(this, new object?[] { propertyFieldMapping.Value.FromStringConverter, context });
#else
                result = (T)converterMethod.Invoke(this, new object[] { propertyFieldMapping.Value.FromStringConverter, context });
#endif
            }
            else if (propertyFieldMapping.Value.TypeConverterType != null)
            {
                var typeConverter = GetTypeConverter(propertyFieldMapping.Value.TypeConverterType);
                if ((typeConverter?.CanConvertFrom(typeof(string))).GetValueOrDefault())
                {
                    result = options.Culture != null
#if NETCOREAPP3_0_OR_GREATER
                        ? (T?)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)
                        : (T?)typeConverter?.ConvertFromInvariantString(serializedFieldValue);
#else
                        ? (T)typeConverter?.ConvertFromString(null, options.Culture, serializedFieldValue)
                        : (T)typeConverter?.ConvertFromInvariantString(serializedFieldValue);
#endif
                }
            }

            return result;
        }

#if NETCOREAPP3_0_OR_GREATER
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
                var generatorMethodDefinition = this.GetType().GetTypeInfo().DeclaredMethods
                    .Single(method => method.Name == nameof(GetGeneratedFieldValue))
                    .GetGenericMethodDefinition();
                var generatorMethod = generatorMethodDefinition.MakeGenericMethod(propertyFieldMapping.Value.PropertyType);
#if NETCOREAPP3_0_OR_GREATER
                fieldValue = generatorMethod.Invoke(this, new object?[] { propertyFieldMapping.Value.VariableValueGenerator });
#else
                fieldValue = generatorMethod.Invoke(this, new object[] { propertyFieldMapping.Value.VariableValueGenerator });
#endif
            }

            return fieldValue;
        }

        protected ICsvFileMapper GetMapper(TRecordLine recordLine)
        {
            ICsvFileMapper mapper;
            if (ImperativeMapperCache.TryGetValue(typeof(TRecordLine), out var imperativeMapper))
            {
                mapper = imperativeMapper;
            }
            else if (DeclarativeMapperCache.TryGetValue(typeof(TRecordLine), out var declarativeMapper))
            {
                mapper = declarativeMapper;
            }
            else
            {
                mapper = recordLine.CreateMapper();
                if (mapper.Count > 0)
                {
                    DeclarativeMapperCache.Add(typeof(TRecordLine), mapper);
                }
            }

            return mapper;
        }

        protected string NormalizeFieldValueLineBreaks(string fieldValue, CsvFileOptions options)
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

#if NETCOREAPP3_0_OR_GREATER
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
#if NETCOREAPP3_0_OR_GREATER
            string? result = fieldValue.ToString();
#else
            string result = fieldValue.ToString();
#endif

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

        private T GetConvertedFieldValue<T>(Delegate converterDelegate, CsvFieldContext<TRecordLine> context)
        {
            var converter = (CsvFieldConvertFromString<TRecordLine, T>)converterDelegate;
            return converter(context);
        }

        private T GetGeneratedFieldValue<T>(Delegate generatorDelegate)
        {
            var generator = (CsvFieldGenerate<T>)generatorDelegate;
            return generator();
        }
    }
}