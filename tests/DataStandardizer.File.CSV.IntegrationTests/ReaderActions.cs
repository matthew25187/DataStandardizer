namespace DataStandardizer.File.CSV.IntegrationTests;

/// <summary>
/// Actions on a reader for tests that measure what reading costs, whether or not the data is valid.
/// </summary>
internal static class ReaderActions
{
    /// <summary>
    /// Reads a line, and ignores the <see cref="CsvFileException"/> that invalid data causes.
    /// </summary>
    internal static void ReadLineIgnoringFailure(CsvFileReader<CsvFileRecordLine> reader)
    {
        try
        {
            reader.ReadLine();
        }
        catch (CsvFileException)
        {
            // The caller measures the read, not its result.
        }
    }
}
