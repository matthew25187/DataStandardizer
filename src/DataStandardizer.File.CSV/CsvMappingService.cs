using System;
using System.Collections.Generic;
using System.Linq;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    internal static class CsvMappingService
    {
        private const string FieldIndexDataItem = "FieldIndex";
        private const string FieldNameDataItem = "FieldName";
        private const string PropertyDataItem = "Property";
        private const string RepeatedFieldNameNotRenamedTemplate =
            "The header line has {0} fields named '{1}', so the mapping of property '{2}' must give its field a name of its own with UseFieldName.";
        private const string SharedFieldRenamedTemplate =
            "The mappings of properties '{0}' and '{1}' share the field at index {2}, but give it different names, '{3}' and '{4}'.";

        /// <summary>
        /// Gets the name of the field a mapping is mapped to, as a line knows it: the name the mapping gives it with
        /// <see cref="CsvFieldMapping.UseFieldName"/>, else its field name, else its property key.
        /// </summary>
        internal static string GetFieldNameFromMapping(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
        {
            return GetFieldNameFromMapping(propertyFieldMapping.Key, propertyFieldMapping.Value);
        }

        /// <inheritdoc cref="GetFieldNameFromMapping(KeyValuePair{string, CsvFieldMapping})"/>
        internal static string GetFieldNameFromMapping(string propertyKey, CsvFieldMapping fieldMapping)
        {
            return fieldMapping.UseFieldName ?? fieldMapping.FieldName ?? propertyKey;
        }

        /// <summary>
        /// Gets the name of the field a mapping is mapped to, as a header line in a file names it: its field name, else its property key.
        /// </summary>
        internal static string GetFileFieldNameFromMapping(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
        {
            return propertyFieldMapping.Value.FieldName ?? propertyFieldMapping.Key;
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
                var fieldName = GetFieldNameFromMapping(mapping);
                if (string.Equals(fieldName, mapping.Key, StringComparison.Ordinal))
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
        /// already names a field that: the first mapping at an index, or an earlier mapping without one.  Otherwise it takes the lowest position that no mapping has taken yet, the mappings without a field index
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
                    // Only the first mapping at an index names the field there (GetFieldNamesByPosition), so only its name is the field's.
                    positions[mappingIndex] = Tuple.Create(mapping.Key, fieldIndex);
                    var fieldName = GetFieldNameFromMapping(mapping);
                    if (takenPositions.Add(fieldIndex) && !positionsByFieldName.ContainsKey(fieldName))
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

        /// <summary>
        /// Gives the fields of a header line in a file the names they are known by once read, as the mappings of a mapper give them with
        /// <see cref="CsvFieldMapping.UseFieldName"/>.
        /// </summary>
        /// <param name="fileFieldNames">The names on the header line, as they are in the file.</param>
        /// <param name="mapper">The mapper that reads the header line.</param>
        /// <param name="fieldNames">The field names once read, by position.  They may repeat a name that no mapping renames; the caller checks.</param>
        /// <param name="errorMessage">Why the mapper can't name the fields, or empty if it can.</param>
        /// <param name="errorData">The data items of the error, or <c>null</c> if there is none.</param>
        /// <returns><c>true</c> if the mapper names the fields; <c>false</c> if a mapping of a repeated name doesn't rename its field, or mappings
        /// that share a field give it different names.</returns>
        /// <remarks>
        /// A mapping by index takes the field at its index.  The mappings by name are given their fields after them: a field of a repeated name
        /// that a mapping by index takes isn't given to a mapping by name, unless every field of that name is.  Mappings by the same name take the
        /// first, second and so on of its fields left, in mapper order, and one past the last takes the last.  A mapping that gives the same name
        /// with <see cref="CsvFieldMapping.UseFieldName"/> as an earlier mapping by the same name shares its field rather than taking the next one.
        /// Every mapping of a field must give it the same name, or none.  RFC 4180 §2 ¶3 allows a header line to repeat a name, but a line's field
        /// names must be unique, so a mapping of a repeated name must rename its field.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryResolveHeaderFieldNames(IReadOnlyList<string> fileFieldNames, ICsvFileMapper mapper, out string[] fieldNames,
            out string errorMessage, out Dictionary<string, object>? errorData)
#else
        internal static bool TryResolveHeaderFieldNames(IReadOnlyList<string> fileFieldNames, ICsvFileMapper mapper, out string[] fieldNames,
            out string errorMessage, [MaybeNull] out Dictionary<string, object> errorData)
#endif
        {
            fieldNames = fileFieldNames.ToArray();
            errorMessage = string.Empty;
            errorData = null;
            if (mapper.Count == 0)
            {
                return true;
            }

            // The first mapping of each field, and the name it gives the field once read (null if it keeps its name in the file).
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            var firstPropertyKeys = new string?[fieldNames.Length];
            var useFieldNames = new string?[fieldNames.Length];
#else
            var firstPropertyKeys = new string[fieldNames.Length];
            var useFieldNames = new string[fieldNames.Length];
#endif
            var isIndexed = new bool[fieldNames.Length];
            foreach (var mapping in mapper)
            {
                if (mapping.Value.FieldIndex is int fieldIndex && fieldIndex < fieldNames.Length)
                {
                    isIndexed[fieldIndex] = true;
                    if (!TryBindHeaderField(fileFieldNames, fieldNames, firstPropertyKeys, useFieldNames, fieldIndex, mapping, out errorMessage, out errorData))
                    {
                        return false;
                    }
                }
            }

            var positionsByFileFieldName = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            for (var position = 0; position < fieldNames.Length; position++)
            {
                if (!positionsByFileFieldName.TryGetValue(fileFieldNames[position], out var namePositions))
                {
                    namePositions = new List<int>();
                    positionsByFileFieldName.Add(fileFieldNames[position], namePositions);
                }

                namePositions.Add(position);
            }

            // How many fields of each name the mappings by name have taken, and the field each name given with UseFieldName took, by file name.
            var takenFieldCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var renamedPositions = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            foreach (var mapping in mapper)
            {
                if (mapping.Value.FieldIndex.HasValue)
                {
                    continue;
                }

                var fileFieldName = GetFileFieldNameFromMapping(mapping);
                if (!positionsByFileFieldName.TryGetValue(fileFieldName, out var namePositions))
                {
                    continue;
                }

                var freePositions = namePositions.Where(position => !isIndexed[position]).ToList();
                if (freePositions.Count == 0)
                {
                    freePositions = namePositions;
                }

                var useFieldName = mapping.Value.UseFieldName;
                if (freePositions.Count > 1 && useFieldName is null)
                {
                    errorMessage = string.Format(RepeatedFieldNameNotRenamedTemplate, freePositions.Count, fileFieldName, mapping.Key);
                    errorData = new Dictionary<string, object> { { FieldNameDataItem, fileFieldName }, { PropertyDataItem, mapping.Key } };
                    return false;
                }

                if (!renamedPositions.TryGetValue(fileFieldName, out var positionsByUseFieldName))
                {
                    positionsByUseFieldName = new Dictionary<string, int>(StringComparer.Ordinal);
                    renamedPositions.Add(fileFieldName, positionsByUseFieldName);
                }

                if (useFieldName is null || !positionsByUseFieldName.TryGetValue(useFieldName, out var fieldPosition))
                {
                    takenFieldCounts.TryGetValue(fileFieldName, out var takenFieldCount);
                    takenFieldCounts[fileFieldName] = takenFieldCount + 1;
                    fieldPosition = freePositions[Math.Min(takenFieldCount, freePositions.Count - 1)];
                    if (useFieldName != null)
                    {
                        positionsByUseFieldName.Add(useFieldName, fieldPosition);
                    }
                }

                if (!TryBindHeaderField(fileFieldNames, fieldNames, firstPropertyKeys, useFieldNames, fieldPosition, mapping, out errorMessage, out errorData))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Binds a mapping to a field of a header line, giving the field the mapping's name if it is the field's first mapping, else checking that
        /// it gives the same name as the first one.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static bool TryBindHeaderField(IReadOnlyList<string> fileFieldNames, string[] fieldNames, string?[] firstPropertyKeys, string?[] useFieldNames,
            int position, KeyValuePair<string, CsvFieldMapping> mapping, out string errorMessage, out Dictionary<string, object>? errorData)
#else
        private static bool TryBindHeaderField(IReadOnlyList<string> fileFieldNames, string[] fieldNames, string[] firstPropertyKeys, string[] useFieldNames,
            int position, KeyValuePair<string, CsvFieldMapping> mapping, out string errorMessage, [MaybeNull] out Dictionary<string, object> errorData)
#endif
        {
            errorMessage = string.Empty;
            errorData = null;
            var useFieldName = mapping.Value.UseFieldName;
            if (firstPropertyKeys[position] is string firstPropertyKey)
            {
                if (!string.Equals(useFieldNames[position], useFieldName, StringComparison.Ordinal))
                {
                    var fileFieldName = fileFieldNames[position];
                    errorMessage = string.Format(SharedFieldRenamedTemplate, firstPropertyKey, mapping.Key, position, useFieldNames[position] ?? fileFieldName,
                        useFieldName ?? fileFieldName);
                    errorData = new Dictionary<string, object> { { FieldIndexDataItem, position }, { PropertyDataItem, mapping.Key } };
                    return false;
                }

                return true;
            }

            firstPropertyKeys[position] = mapping.Key;
            useFieldNames[position] = useFieldName;
            if (useFieldName != null)
            {
                fieldNames[position] = useFieldName;
            }

            return true;
        }
    }
}