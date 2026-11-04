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
        /// A carriage return and a line feed, which RFC 4155 Appendix A doesn't allow, for a mailbox that a program expects in this form.  An <see cref="MboxReader"/> reads it only with
        /// <see cref="ImfOptions.AllowMboxCarriageReturnLineFeed"/>.
        /// </summary>
        CarriageReturnLineFeed = 1,
    }
}
