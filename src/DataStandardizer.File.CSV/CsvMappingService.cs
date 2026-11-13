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
            return GetFieldPositions(mapper)
                .OrderBy(item => item.Item2)
                .Select(item => GetFieldNameFromMapping(new KeyValuePair<string, CsvFieldMapping>(item.Item1, mapper[item.Item1])))
                .ToArray();
        }

        /// <summary>
        /// Gets the property key of each mapping, in the order of the positions of the fields they are mapped to.  Mappings at the same position keep
        /// their mapper order.
        /// </summary>
        internal static string[] GetPropertyKeysByPosition(ICsvFileMapper mapper)
        {
            return GetFieldPositions(mapper)
                .OrderBy(item => item.Item2)
                .Select(item => item.Item1)
                .ToArray();
        }

        /// <summary>
        /// Gets the field name of each mapping at the zero-based position of the field it is mapped to.
        /// </summary>
        /// <returns>The field names keyed by position.  A position no mapping is mapped to has no entry.  Where more than one mapping is mapped to a
        /// position, the first one's field name is used.</returns>
        internal static Dictionary<int, string> GetFieldNamesByPosition(ICsvFileMapper mapper)
        {
            var fieldNames = new Dictionary<int, string>();
            foreach (var position in GetFieldPositions(mapper))
            {
                if (!fieldNames.ContainsKey(position.Item2))
                {
                    fieldNames.Add(position.Item2, GetFieldNameFromMapping(new KeyValuePair<string, CsvFieldMapping>(position.Item1, mapper[position.Item1])));
                }
            }

            return fieldNames;
        }

        /// <summary>
        /// Gets the property key and field position of each mapping, in mapper order.  A mapping without a field index takes the first position after
        /// the previous mapping's that no mapping has a field index for.
        /// </summary>
        private static Tuple<string, int>[] GetFieldPositions(ICsvFileMapper mapper)
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
                .ToArray();
        }
    }
}