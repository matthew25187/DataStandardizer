using System.Collections.Generic;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method that supplies field names when there is no header line to take them from.
    /// </summary>
    /// <param name="headerLine">
    /// The header line the reader takes field names from.  This is always <c>null</c>, because the reader calls the delegate only when
    /// <see cref="ICsvFileOptions.HeaderHandling"/> is <see cref="CsvFileHeaderHandling.None"/> or <see cref="CsvFileHeaderHandling.Ignore"/>.
    /// </param>
    /// <returns>The field names of the records, in field order.</returns>
    /// <remarks>
    /// The reader calls the delegate once, as it reads the first record.  When <see cref="ICsvFileOptions.HeaderHandling"/> is
    /// <see cref="CsvFileHeaderHandling.Use"/>, the field names come from the file's header line and the reader never calls the delegate.
    /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    public delegate IReadOnlyList<string> CsvFileHeader(CsvFileHeaderLine? headerLine);
#else
    public delegate IReadOnlyList<string> CsvFileHeader([AllowNull] CsvFileHeaderLine headerLine);
#endif
}