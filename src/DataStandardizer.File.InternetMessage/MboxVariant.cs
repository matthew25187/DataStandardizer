namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The variants of the mbox format, which differ in how they keep a line of a message that starts with <c>From </c> from being taken for a separator line.
    /// </summary>
    /// <remarks>
    /// RFC 4155 Appendix A prescribes no way of doing this, and the variants can't be told apart reliably from what a file holds, so a caller has to say which one a file uses.
    /// </remarks>
    public enum MboxVariant
    {
        /// <summary>
        /// Any line matching <c>&gt;*From </c> gets one more <c>&gt;</c>, and reading removes one, so a message reads back exactly.  This is the default of <see cref="MboxReader"/> and
        /// <see cref="MboxWriter"/>.
        /// </summary>
        Mboxrd = 0,

        /// <summary>
        /// A line starting <c>From </c> is written as <c>&gt;From </c>, and a line that already starts <c>&gt;From </c> is not changed.  Reading can't tell these apart, so the quotes are kept.
        /// </summary>
        Mboxo = 1,

        /// <summary>
        /// The quoting of <see cref="Mboxo"/>, with a Content-Length field that gives the length of the body as it is stored in the file, quotes included.  The quotes are kept when reading.
        /// </summary>
        Mboxcl = 2,

        /// <summary>
        /// A Content-Length field that gives the length of the body, with no quoting, so a message reads back exactly.
        /// </summary>
        Mboxcl2 = 3,
    }
}
