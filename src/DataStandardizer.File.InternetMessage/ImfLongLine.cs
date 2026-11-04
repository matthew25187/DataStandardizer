namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A line of a message that is too long in a form that a reader of the message doesn't see, as with a quote in an mbox file.
    /// </summary>
    internal struct ImfLongLine
    {
        internal readonly long MessageOffset;
        internal readonly long BytePosition;
        internal readonly long LineNumber;

        /// <param name="messageOffset">Where the line starts in the bytes of the message.</param>
        /// <param name="bytePosition">Where the line starts in the data that holds the message, or -1 if there is none.</param>
        /// <param name="lineNumber">The line of the data that holds the message, or 0 if there is none.</param>
        internal ImfLongLine(long messageOffset, long bytePosition, long lineNumber)
        {
            MessageOffset = messageOffset;
            BytePosition = bytePosition;
            LineNumber = lineNumber;
        }
    }
}
