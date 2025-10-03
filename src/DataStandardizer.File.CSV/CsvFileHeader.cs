using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method to be called when the header line for a CSV file is being prepared.
    /// </summary>
    /// <param name="headerLine">Header line read from the CSV file, if any.</param>
    /// <returns>Sorted collection of field names from the header line.</returns>
#if NETCOREAPP3_0_OR_GREATER
    public delegate IReadOnlyList<string> CsvFileHeader(CsvFileHeaderLine? headerLine);
#else
    public delegate IReadOnlyList<string> CsvFileHeader([CanBeNull] CsvFileHeaderLine headerLine);
#endif
}