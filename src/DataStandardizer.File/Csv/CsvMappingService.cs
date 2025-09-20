using System;
using System.Collections.Generic;
using System.Linq;

namespace DataStandardizer.File.Csv
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

        internal static string[] GetSortedFieldNames(ICsvFileLine csvLine, ICsvFileMapper mapper)
        {
            var lineFieldNames = csvLine.Keys.Cast<string>();
            var mapperFieldNames = GetSortedFieldNamesFromMapper(mapper);
            return mapperFieldNames.Concat(lineFieldNames.Except(mapperFieldNames)).ToArray();
        }

        internal static string[] GetSortedFieldNamesFromMapper(ICsvFileMapper mapper)
        {
            var source = mapper
                .Select(mapping => Tuple.Create(mapping.Key, mapping.Value.FieldIndex))
                .ToArray();
#if NETCOREAPP2_0_OR_GREATER
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