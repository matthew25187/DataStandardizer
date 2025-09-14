using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DataStandardizer.File.Csv
{
    internal static class CsvMappingService
    {
#if NETCOREAPP3_0_OR_GREATER
        internal static IReadOnlyDictionary<string, string?> GetFieldToPropertyMappings<T>(T model, ICsvFileMapper mapper) 
#else
        internal static IReadOnlyDictionary<string, string> GetFieldToPropertyMappings<T>(T model, ICsvFileMapper mapper) 
#endif
            where T:class
        {
#if NETCOREAPP3_0_OR_GREATER
            var mappings = new Dictionary<string, string?>(); 
#else
            var mappings = new Dictionary<string, string>();
#endif

            // Get ordered list of field mappings.
            var maximumFieldIndex = mapper
                .Max(mapping => mapping.Value.FieldIndex.GetValueOrDefault());
            int pseudoFieldIndex = 0, pseudoFieldIndex2 = 0;
            var orderedFieldMappings = mapper
                .OrderBy(item => item.Value.FieldIndex ?? maximumFieldIndex + ++pseudoFieldIndex)
                .Select(item => new { FieldIndex = item.Value.FieldIndex ?? maximumFieldIndex + ++pseudoFieldIndex2, PropertyName = item.Key, FieldMapping = item.Value })
                .ToArray();
            var orderedFieldMappingsIndex = 0;

            // Get existing field names on the model.
#if NET8_0_OR_GREATER
            string[] existingFieldNames = []; 
#else
            string[] existingFieldNames = Array.Empty<string>();
#endif
            var existingFieldNamesIndex = 0;
            if (model is ICsvFileLine csvLine)
            {
                existingFieldNames = csvLine.Keys.Cast<string>().ToArray();
            }
            // Merge existing field names with mapped field names.
            var maximumIndex = maximumFieldIndex + pseudoFieldIndex;
            var mappingIndex = 0;
            while (mappingIndex<=maximumIndex||existingFieldNamesIndex<existingFieldNames.Length)
            {
                if (orderedFieldMappingsIndex < orderedFieldMappings.Length && orderedFieldMappings[orderedFieldMappingsIndex].FieldIndex == mappingIndex)
                {
                    var orderedFieldMapping = orderedFieldMappings[orderedFieldMappingsIndex];
                    
                    var fieldName = orderedFieldMapping.FieldMapping.FieldName ?? orderedFieldMapping.PropertyName;
                    var propertyName = orderedFieldMapping.PropertyName;
                    mappings.Add(fieldName, propertyName);
                    orderedFieldMappingsIndex++;
                }
                else if(existingFieldNamesIndex<existingFieldNames.Length)
                {
                    var fieldName = existingFieldNames[existingFieldNamesIndex];
                    mappings.Add(fieldName, null);
                    existingFieldNamesIndex++;
                }

                mappingIndex++;
            }

            return mappings;
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static IReadOnlyDictionary<string, string?> GetPropertyToFieldMappings<T>(T model, ICsvFileMapper mapper)
#else
        internal static IReadOnlyDictionary<string, string> GetPropertyToFieldMappings<T>(T model, ICsvFileMapper mapper)
#endif
            where T : class
        {
            return model.GetType().GetTypeInfo().DeclaredProperties
                .ToDictionary(
                    property => property.Name,
                    property =>
                    {
                        var fieldMapping = mapper.TryGetValue(property.Name, out var mapping) ? mapping : null;
                        return fieldMapping?.FieldName;
                    });
        }

        internal static string[] GetSortedFieldNamesFromMapper(ICsvFileMapper mapper)
        {
            var pseudoFieldIndex = 0;
            return mapper
                .OrderBy(item =>
                {
                    if (item.Value.FieldIndex.HasValue)
                    {
                        pseudoFieldIndex = item.Value.FieldIndex.Value + 1;
                        return item.Value.FieldIndex.Value;
                    }

                    return pseudoFieldIndex++;
                })
                .Select(GetFieldNameFromMapping)
                .ToArray();
        }

        internal static string GetFieldNameFromMapping(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
        {
            return propertyFieldMapping.Value.FieldName ?? propertyFieldMapping.Key;
        }
    }
}