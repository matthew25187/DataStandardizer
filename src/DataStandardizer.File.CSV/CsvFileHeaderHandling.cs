namespace DataStandardizer.File.CSV
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
        /// The file has a header line, and it should be used to set the field names.  When there is a <see cref="CsvFileHeader"/>
        /// delegate, the field names it returns for the header line are used instead.
        /// </summary>
        Use,

        /// <summary>
        /// The file has a header line, and it should be ignored, as if the file had no header line.  Field names can be supplied by a
        /// <see cref="CsvFileHeader"/> delegate, which is passed the ignored header line.
        /// </summary>
        Ignore
    }
}