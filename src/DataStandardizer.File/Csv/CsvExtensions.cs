using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace DataStandardizer.File.Csv
{
    public static class CsvExtensions
    {
        private static class DataItemName
        {
            internal const string FieldIndex = "FieldIndex";
            internal const string FieldName = "FieldName";
            internal const string PropertyName = "PropertyName";
        }

        internal static string GetPropertyKey(this PropertyInfo pi)
        {
            var fieldAttribute = pi.GetCustomAttribute<CsvFieldAttribute>();
            return fieldAttribute?.FieldName ?? pi.Name;
        }

        /// <summary>
        /// Create a CSV field mapper based on declarative configuration on the record line object.
        /// </summary>
        /// <param name="recordLine">Record line object to be mapped.</param>
        /// <returns>Mapper containing collection of field mappings.</returns>
        public static ICsvFileMapper CreateMapper<TRecordLine>(this TRecordLine recordLine) where TRecordLine : CsvFileRecordLine
        {
            var propertyFieldMappings = new Dictionary<string, CsvFieldMapping>();

            var properties = recordLine.GetType().GetTypeInfo().DeclaredProperties;
            foreach (var pi in properties)
            {
                var fieldAttribute = pi.GetCustomAttribute<CsvFieldMappingAttribute>();
                if (fieldAttribute is null)
                {
                    continue;
                }

                var typeConverterAttribute = pi.GetCustomAttribute<TypeConverterAttribute>();
                var typeConverterType = !string.IsNullOrEmpty(typeConverterAttribute?.ConverterTypeName) ? Type.GetType(typeConverterAttribute.ConverterTypeName) : null;

                var fieldMapping = new CsvFieldMapping(pi.PropertyType)
                {
                    FieldIndex = fieldAttribute.FieldIndex,
                    FieldName = fieldAttribute.FieldName,
                    ConstantValue = fieldAttribute.ConstantValue,
                    IsOptional = fieldAttribute.IsOptional,
                    TypeConverterType = typeConverterType,
                };
                var propertyKey = pi.GetPropertyKey();
                propertyFieldMappings.Add(propertyKey, fieldMapping);
            }

            return new CsvFileMapper(propertyFieldMappings);
        }

        /// <summary>
        /// Convert a CSV line object to a custom line model object.
        /// </summary>
        /// <typeparam name="TRecordLine">Type of the CSV line.</typeparam>
        /// <typeparam name="TModel">Type of the custom line model.</typeparam>
        /// <param name="sourceLine">CSV line read from a CSV file.</param>
        /// <param name="mapper">Field mapping for the custom line model.</param>
        /// <returns>Custom object model representing the CSV line.</returns>
        public static TModel ToObject<TRecordLine, TModel>(this TRecordLine sourceLine, CsvFileCustomMapperBase<TModel> mapper)
            where TRecordLine : CsvFileRecordLine
            where TModel : class, new()
        {
            ICsvFileLine csvLine = sourceLine;
            var targetModel = new TModel();
            ICsvFileMapper csvMapper = mapper;

            var properties = targetModel.GetType().GetTypeInfo().DeclaredProperties
                .Where(property => property.CanWrite);
            foreach (var pi in properties)
            {
                // Get the field mapping for the current property.
                var propertyKey = pi.GetPropertyKey();
                if (!csvMapper.TryGetValue(propertyKey, out var fieldMapping))
                {
                    continue;
                }

                var fieldKey = CsvMappingService.GetFieldNameFromMapping(new KeyValuePair<string, CsvFieldMapping>(propertyKey, fieldMapping));
                if (!csvLine.Contains(fieldKey) && csvLine.Contains(propertyKey))
                {
                    fieldKey = propertyKey;
                }

                // If a field name was not found for the property, do not attempt to set the property on the custom model.
                if (!csvLine.Contains(fieldKey))
                {
                    if (fieldMapping.IsOptional)
                    {
                        continue;
                    }

                    var exception = new CsvFileException($"Property '{propertyKey}' unable to be mapped.");
                    exception.Data.Add(DataItemName.PropertyName, propertyKey);
                    exception.Data.Add(DataItemName.FieldIndex, fieldMapping.FieldIndex);
                    exception.Data.Add(DataItemName.FieldName, fieldMapping.FieldName);
                    throw exception;
                }

                // Copy the field value from the CSV line to the corresponding property on the custom model.
                // Assumes that the raw string value from the CSV file has already been converted in the CSV line.
                var fieldValue = csvLine[fieldKey];
                pi.SetValue(targetModel, fieldValue);
            }

            return targetModel;
        }

        /// <summary>
        /// Convert a custom model object to a CSV line object.
        /// </summary>
        /// <typeparam name="TModel">Type of the custom model.</typeparam>
        /// <typeparam name="TRecordLine">Type of the CSV line.</typeparam>
        /// <param name="sourceModel">Custom model object with values to be copied to a CSV line.</param>
        /// <param name="mapper">Field mapping for the CSV line.</param>
        /// <returns>CSV line representing the custom model object.</returns>
        public static TRecordLine ToCsvLine<TModel, TRecordLine>(this TModel sourceModel, CsvFileMapperBase<TRecordLine> mapper)
            where TModel : class
            where TRecordLine : CsvFileRecordLine, new()
        {
            var targetLine = new TRecordLine();
            ICsvFileLine csvLine = targetLine;
            ICsvFileMapper csvMapper = mapper;

            var properties = sourceModel.GetType().GetTypeInfo().DeclaredProperties
                .Where(property => property.CanRead);
            foreach (var pi in properties)
            {
                // Get the field mapping for the current property.
                var propertyKey = pi.GetPropertyKey();
                if (!csvMapper.TryGetValue(propertyKey, out var fieldMapping))
                {
                    continue;
                }

                var fieldKey = CsvMappingService.GetFieldNameFromMapping(propertyKey, fieldMapping);

                // Copy the field value from the CSV line to the corresponding property on the custom model.
                var fieldValue = pi.GetValue(sourceModel);
                csvLine[fieldKey] = fieldValue;
            }

            return targetLine;
        }
    }
}