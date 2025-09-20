namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Method for handling the header line in a CSV file.
    /// </summary>
    public enum CsvFileHeaderHandling
    {
        /// <summary>
        /// The file has no header line.
        /// </summary>
        None,

        /// <summary>
        /// The file has a header line, and it should be used to set the field names.
        /// </summary>
        Use,

        /// <summary>
        /// The file has a header line, and it should be ignored.  Field names can be customised using a <see cref="CsvFileHeader"/> delegate.
        /// </summary>
        Ignore
    }
}