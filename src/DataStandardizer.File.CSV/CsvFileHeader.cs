using System.Collections.Generic;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Delegate for a method that supplies the field names a reader uses, in place of any header line in the file.
    /// </summary>
    /// <param name="headerLine">
    /// The header line read from the file, when <see cref="ICsvFileOptions.HeaderHandling"/> is <see cref="CsvFileHeaderHandling.Use"/>
    /// or <see cref="CsvFileHeaderHandling.Ignore"/>; <c>null</c> when it is <see cref="CsvFileHeaderHandling.None"/>.
    /// </param>
    /// <returns>The field names of the records, in field order.</returns>
    /// <remarks>
    /// <para>
    /// When a reader has this delegate, it calls it once, whatever <see cref="ICsvFileOptions.HeaderHandling"/> is, and the names it returns
    /// replace those of the file's header line.  The delegate can return names based on the header line, or names of its own.
    /// </para>
    /// <para>
    /// The delegate renames fields; it can't add or remove them.  With <see cref="CsvFileHeaderHandling.Use"/>, the header line sets how many
    /// fields each record is expected to have; with <see cref="CsvFileHeaderHandling.Ignore"/> and <see cref="CsvFileHeaderHandling.None"/>,
    /// the first record does, as if the file had no header line.  The names must be as many as that.  The names must not repeat.  A list that breaks either rule
    /// makes <see cref="CsvFileReader{TRecordLine}.ReadLine"/> throw.
    /// </para>
    /// <para>
    /// With <see cref="CsvFileHeaderHandling.Use"/>, the reader calls the delegate as it reads the header line, and returns a header line with
    /// the names the delegate gave.  With <see cref="CsvFileHeaderHandling.Ignore"/> and <see cref="CsvFileHeaderHandling.None"/>, it calls the
    /// delegate after it reads the first record and before it returns it, and doesn't return a header line.
    /// </para>
    /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    public delegate IReadOnlyList<string> CsvFileHeader(CsvFileHeaderLine? headerLine);
#else
    public delegate IReadOnlyList<string> CsvFileHeader([AllowNull] CsvFileHeaderLine headerLine);
#endif
}