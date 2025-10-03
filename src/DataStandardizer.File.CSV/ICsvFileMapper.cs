using System.Collections.Generic;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Mapper of line model object properties to CSV fields.
    /// </summary>
    public interface ICsvFileMapper : IReadOnlyDictionary<string, CsvFieldMapping>
    {
    }
}