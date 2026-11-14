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
        /// Gets the property key and field position of each mapping, in mapper order.
        /// </summary>
        /// <remarks>
        /// A mapping with a field index is at that index.  A mapping without one is at the position of the field with its name, if a mapping
        /// already places that name; otherwise it takes the lowest position that no mapping has taken yet, the mappings without a field index
        /// taking theirs in mapper order.  So a gap below a field index is filled by the fields mapped by name, and mapper order decides only
        /// the order of those fields among themselves.
        /// </remarks>
        internal static Tuple<string, int>[] GetFieldPositions(ICsvFileMapper mapper)
        {
            var positions = new Tuple<string, int>[mapper.Count];
            var takenPositions = new HashSet<int>();
            var positionsByFieldName = new Dictionary<string, int>(StringComparer.Ordinal);

            // The mappings with a field index are placed first, so a mapping by name that shares their field name joins them, wherever it is in the
            // mapper.
            var mappingIndex = 0;
            foreach (var mapping in mapper)
            {
                if (mapping.Value.FieldIndex is int fieldIndex)
                {
                    positions[mappingIndex] = Tuple.Create(mapping.Key, fieldIndex);
                    takenPositions.Add(fieldIndex);
                    var fieldName = GetFieldNameFromMapping(mapping);
                    if (!positionsByFieldName.ContainsKey(fieldName))
                    {
                        positionsByFieldName.Add(fieldName, fieldIndex);
                    }
                }

                mappingIndex++;
            }

            var nextPosition = 0;
            mappingIndex = 0;
            foreach (var mapping in mapper)
            {
                if (!mapping.Value.FieldIndex.HasValue)
                {
                    var fieldName = GetFieldNameFromMapping(mapping);
                    if (!positionsByFieldName.TryGetValue(fieldName, out var position))
                    {
                        while (takenPositions.Contains(nextPosition))
                        {
                            nextPosition++;
                        }

                        position = nextPosition;
                        takenPositions.Add(position);
                        positionsByFieldName.Add(fieldName, position);
                    }

                    positions[mappingIndex] = Tuple.Create(mapping.Key, position);
                }

                mappingIndex++;
            }

            return positions;
        }
    }
}