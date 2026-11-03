namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// How an Internet message writer writes header text that is not US-ASCII.
    /// </summary>
    public enum ImfHeaderEncoding
    {
        /// <summary>
        /// As RFC 2047 encoded words, in UTF-8.  A field body given to <see cref="ImfHeaderWriter.WriteField(string, string, ImfHeaderFieldKind)"/> or
        /// <see cref="ImfEntitySource.AddField(string, string, ImfHeaderFieldKind)"/> is written as it is, so one that is not US-ASCII is refused.  Text given to
        /// <see cref="ImfHeaderWriter.WriteTextField"/> or <see cref="ImfEntitySource.AddTextField"/> is encoded.
        /// </summary>
        EncodedWords = 0,

        /// <summary>
        /// As UTF-8 (RFC 6532), which a transport can carry only if it supports internationalized email (RFC 6531).
        /// </summary>
        Utf8 = 1,
    }
}
