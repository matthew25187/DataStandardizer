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
        /// The rules of an MHTML file, whose top level is a MIME entity, so a missing origination field of RFC 5322 §3.6 is a diagnostic.  A writer also checks the structure of its
        /// <c>multipart/related</c> entities.
        /// </summary>
        Mhtml = 1,
    }
}
