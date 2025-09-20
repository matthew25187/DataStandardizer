using System.Collections.Generic;

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Mapper of line model object properties to CSV fields.
    /// </summary>
    public interface ICsvFileMapper : IReadOnlyDictionary<string, CsvFieldMapping>
    {
    }
}