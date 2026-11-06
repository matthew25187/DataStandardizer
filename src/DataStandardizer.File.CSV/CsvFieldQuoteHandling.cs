namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Methods for handling the quoting of CSV field values.
    /// </summary>
    public enum CsvFieldQuoteHandling
    {
        /// <summary>
        /// Always surround field values in double-quotes.
        /// </summary>
        Always,

        /// <summary>
        /// Surround field values in double-quotes when they are strings (i.e., non-numeric), or when they contain line breaks, double-quotes, or field
        /// delimiters.
        /// </summary>
        Auto,

        /// <summary>
        /// Surround field values in double-quotes when the value contains line breaks, double-quotes, or field delimiters.
        /// </summary>
        Required
    }
}