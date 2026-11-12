using System;
using System.Collections.Generic;
using System.Linq;

namespace DataStandardizer.File.CSV
{
    internal static class CsvMappingService
    {
        internal static string GetFieldNameFromMapping(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
        {
            return propertyFieldMapping.Value.FieldName ?? propertyFieldMapping.Key;
        }

        internal static string GetFieldNameFromMapping(string propertyKey, CsvFieldMapping fieldMapping)
        {
            return fieldMapping.FieldName ?? propertyKey;
        }

        /// <summary>
        /// Gets the field name of each mapping that maps a property to a field with another name, keyed by property key.
        /// </summary>
        /// <returns>The field names, or <c>null</c> if every mapping uses its property key as its field name.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static Dictionary<string, string>? GetMappedFieldNames(ICsvFileMapper mapper)
#else
        internal static Dictionary<string, string> GetMappedFieldNames(ICsvFileMapper mapper)
#endif
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Dictionary<string, string>? mappedFieldNames = null;
#else
            Dictionary<string, string> mappedFieldNames = null;
#endif
            foreach (var mapping in mapper)
            {
                var fieldName = mapping.Value.FieldName;
                if (fieldName is null || string.Equals(fieldName, mapping.Key, StringComparison.Ordinal))
                {
                    continue;
                }

                if (mappedFieldNames is null)
                {
                    mappedFieldNames = new Dictionary<string, string>(StringComparer.Ordinal);
                }

                mappedFieldNames[mapping.Key] = fieldName;
            }

            return mappedFieldNames;
        }

        internal static string[] GetSortedFieldNamesFromMapper(ICsvFileMapper mapper)
        {
            var source = mapper
                .Select(mapping => Tuple.Create(mapping.Key, mapping.Value.FieldIndex))
                .ToArray();
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            var usedIndices = source
                .Where(item => item.Item2.HasValue)
                .Select(item => item.Item2!.Value)
                .ToHashSet();
#else
            var existingIndices = source
                .Where(item => item.Item2.HasValue)
                .Select(item => item.Item2.Value);
            var usedIndices = new HashSet<int>(existingIndices);
#endif
            var nextAvailableIndex = 0;
            return source
                .Select(item =>
                {
                    var index = item.Item2 ?? Enumerable.Range(nextAvailableIndex, int.MaxValue - nextAvailableIndex).First(idx => !usedIndices.Contains(idx));
                    usedIndices.Add(index);
                    nextAvailableIndex = index + 1;
                    return Tuple.Create(item.Item1, index);
                })
                .OrderBy(item => item.Item2)
                .Select(item => GetFieldNameFromMapping(new KeyValuePair<string, CsvFieldMapping>(item.Item1, mapper[item.Item1])))
                .ToArray();
        }
    }
}