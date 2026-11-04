namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The line breaks that an <see cref="MboxWriter"/> writes.
    /// </summary>
    public enum MboxLineBreak
    {
        /// <summary>
        /// A line feed, which RFC 4155 Appendix A requires of a canonical mbox database.  This is the default.
        /// </summary>
        LineFeed = 0,

        /// <summary>
        /// A carriage return and a line feed, for a mailbox that is not going to be transferred as <c>application/mbox</c> and that a program expects in this form.
        /// </summary>
        CarriageReturnLineFeed = 1,
    }
}
