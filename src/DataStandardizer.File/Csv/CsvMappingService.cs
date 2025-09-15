using System.Collections.Generic;
using System.Linq;

namespace DataStandardizer.File.Csv
{
    internal static class CsvMappingService
    {
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