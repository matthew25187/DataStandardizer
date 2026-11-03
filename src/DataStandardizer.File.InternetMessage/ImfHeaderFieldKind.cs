namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// How an Internet message writer may fold a header field (RFC 5322 §2.2).
    /// </summary>
    public enum ImfHeaderFieldKind
    {
        /// <summary>
        /// An unstructured field, such as Subject, which may be folded at any space or tab (RFC 5322 §2.2.1).
        /// </summary>
        Unstructured = 0,

        /// <summary>
        /// A structured field, such as To or Content-Type, which may be folded only between its tokens (RFC 5322 §2.2.2): at a space or tab outside a
        /// quoted string, a comment and a domain literal.
        /// </summary>
        Structured = 1,
    }
}
