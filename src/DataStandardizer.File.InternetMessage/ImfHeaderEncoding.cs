namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// How an Internet message writer writes header text that is not US-ASCII.
    /// </summary>
    public enum ImfHeaderEncoding
    {
        /// <summary>
        /// As RFC 2047 encoded words.  <see cref="ImfHeaderWriter"/> and <see cref="ImfEntityWriter"/> don't encode text themselves, so they refuse a
        /// field body that is not US-ASCII.
        /// </summary>
        EncodedWords = 0,

        /// <summary>
        /// As UTF-8 (RFC 6532), which a transport can carry only if it supports internationalized email (RFC 6531).
        /// </summary>
        Utf8 = 1,
    }
}
