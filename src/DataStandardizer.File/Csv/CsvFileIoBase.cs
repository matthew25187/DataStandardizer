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
        protected T? DeserializeCsvLineFieldValue<T>(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, string fieldName)
#else
        [CanBeNull]
        protected T DeserializeCsvLineFieldValue<T>([CanBeNull] CsvFileHeaderLine headerLine, [NotNull] TRecordLine recordLine, [NotNull] CsvFileOptions options, [NotNull] string fieldName)
#endif
        {
            ICsvFileLine csvLine = recordLine;

            // Get a mapping for the field.
            var mapper = GetMapper(recordLine);
            var fieldMapping = mapper.TryGetValue(fieldName, out var mapping) ? mapping : null;

            // Get the mapped field value.
            var fieldValue = csvLine[fieldName];
            if (fieldValue is null && fieldMapping?.ConstantValue != null)
            {
                fieldValue = fieldMapping.ConstantValue;
            }
            else if (fieldValue is null && fieldMapping?.VariableValueGenerator != null)
            {
                var generatorMethodDefinition = this.GetType().GetTypeInfo().DeclaredMethods
                    .Single(method => method.Name == nameof(GetGeneratedFieldValue))
                    .GetGenericMethodDefinition();
                var generatorMethod = generatorMethodDefinition.MakeGenericMethod(fieldMapping.PropertyType);
                fieldValue = generatorMethod.Invoke(this, new object[] { fieldMapping.VariableValueGenerator });
            }

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

#if NETCOREAPP3_0_OR_GREATER
            T? result = default;
#else
            T result = default;
#endif
            var fieldNames = CsvMappingService.GetSortedFieldNames(csvLine, mapper);
            var fieldIndex = Array.IndexOf(fieldNames, fieldName);
            if (fieldMapping?.FromStringConverter != null)
            {
                var converterMethodDefinition = this.GetType().GetTypeInfo().DeclaredMethods
                    .Single(method => method.Name == nameof(GetConvertedFieldValue))
                    .GetGenericMethodDefinition();
                var converterMethod = converterMethodDefinition.MakeGenericMethod(typeof(T));
                var context = new CsvFieldContext<TRecordLine>(options)
                {
                    FieldIndex = fieldIndex,
                    FieldName = fieldName,
                    HeaderLine = headerLine, 
                    Model = recordLine
                };
#if NETCOREAPP3_0_OR_GREATER
                result = (T?)converterMethod.Invoke(this, new object[] { fieldMapping.FromStringConverter, context });
#else
                result = (T)converterMethod.Invoke(this, new object[] { fieldMapping.FromStringConverter, context });
#endif
            }
            else if (fieldMapping?.TypeConverterType != null)
            {
                var typeConverter = GetTypeConverter(fieldMapping.TypeConverterType);
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
                if (mapper.Count>0)
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
            using(var lineReader=new StringReader(fieldValue))
            using (var lineWriter = new StringWriter(fieldValueBuilder))
            {
                var line = lineReader.ReadLine();
                while (line!=null)
                {
                    lineWriter.Write(line);

                    line = lineReader.ReadLine();

                    if (line!=null)
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
        protected string? SerializeCsvLineFieldValue(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, string fieldName)
#else
        [CanBeNull]
        protected string SerializeCsvLineFieldValue([CanBeNull] CsvFileHeaderLine headerLine, [NotNull] TRecordLine recordLine, [NotNull] CsvFileOptions options, [NotNull] string fieldName)
#endif
        {
            ICsvFileLine csvLine = recordLine;

            // If the field value is already a string, return that.
            var serializedFieldValue = csvLine[fieldName] as string;
            if (serializedFieldValue != null)
            {
                return serializedFieldValue;
            }

            // Get a mapping for the field.
            var mapper = GetMapper(recordLine);
            var fieldMapping = mapper.TryGetValue(fieldName, out var mapping) ? mapping : null;

            // Get the mapped field value.
            var fieldValue = csvLine[fieldName];
            if (fieldValue is null && fieldMapping?.ConstantValue != null)
            {
                fieldValue = fieldMapping.ConstantValue;
            }
            else if (fieldValue is null && fieldMapping?.VariableValueGenerator != null)
            {
                var generatorMethodDefinition = this.GetType().GetTypeInfo().DeclaredMethods
                    .Single(method => method.Name == nameof(GetGeneratedFieldValue))
                    .GetGenericMethodDefinition();
                var generatorMethod = generatorMethodDefinition.MakeGenericMethod(fieldMapping.PropertyType);
                fieldValue = generatorMethod.Invoke(this, new object[] { fieldMapping.VariableValueGenerator });
            }

            if (fieldValue is null)
            {
                return null;    // if there is no field value then there is nothing to serialize
            }

            // Serialize the field value to a string.
            var fieldNames = CsvMappingService.GetSortedFieldNames(csvLine, mapper);
            var fieldIndex = Array.IndexOf(fieldNames, fieldName);
            if (fieldMapping?.ToStringConverter is CsvFieldConvertToString<TRecordLine> converter)
            {
                var context = new CsvFieldContext<TRecordLine>(options)
                {
                    FieldIndex = fieldIndex, 
                    FieldName = fieldName, 
                    HeaderLine = headerLine,
                    Model = recordLine
                };
                serializedFieldValue = converter(context);
            }
            else if (fieldMapping?.TypeConverterType != null)
            {
                var typeConverter = GetTypeConverter(fieldMapping.TypeConverterType);
                if ((typeConverter?.CanConvertTo(null, typeof(string))).GetValueOrDefault())
                {
                    serializedFieldValue = options.Culture != null
                        ? typeConverter?.ConvertToString(null, options.Culture, fieldValue)
                        : typeConverter?.ConvertToInvariantString(fieldValue);
                }
            }

            return serializedFieldValue;
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