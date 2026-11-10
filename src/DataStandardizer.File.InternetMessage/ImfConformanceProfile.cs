namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The conformance rules that an <see cref="ImfReader"/> or <see cref="ImfWriter"/> goes by.
    /// </summary>
    internal enum ImfConformanceProfile
    {
        /// <summary>
        /// The rules of RFC 5322 for a message.
        /// </summary>
        Message = 0,

        /// <summary>
        /// The rules of an MHTML file: those of a message, and a writer also checks the structure of its <c>multipart/related</c> entities.
        /// </summary>
        Mhtml = 1,
    }
}
