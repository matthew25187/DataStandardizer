namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// The rules <see cref="ContentLineScanner"/> scans by.
    /// </summary>
    internal readonly struct ContentLineScannerSettings
    {
        /// <param name="acceptBareLineFeed"><c>true</c> to accept a bare line feed as a line break.</param>
        /// <param name="maxLineLength">The maximum length of a logical line, or <see cref="int.MaxValue"/> for no limit.</param>
        /// <param name="allowGroupPrefix"><c>true</c> to accept a group before the name; see <see cref="AllowGroupPrefix"/>.</param>
        /// <param name="rejectByteOrderMark"><c>true</c> to treat a byte order mark like any other character; see <see cref="RejectByteOrderMark"/>.</param>
        /// <param name="requireFinalLineBreak"><c>true</c> to reject a last line with no line break; see <see cref="RequireFinalLineBreak"/>.</param>
        /// <param name="continuation">Decides whether a line continues past the end of a physical line, other than by folding; see <see cref="Continuation"/>.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal ContentLineScannerSettings(bool acceptBareLineFeed, int maxLineLength, bool allowGroupPrefix = false, bool rejectByteOrderMark = false,
            bool requireFinalLineBreak = false, IContentLineContinuation? continuation = null)
#else
        internal ContentLineScannerSettings(bool acceptBareLineFeed, int maxLineLength, bool allowGroupPrefix = false, bool rejectByteOrderMark = false,
            bool requireFinalLineBreak = false, IContentLineContinuation continuation = null)
#endif
        {
            AcceptBareLineFeed = acceptBareLineFeed;
            MaxLineLength = maxLineLength;
            AllowGroupPrefix = allowGroupPrefix;
            RejectByteOrderMark = rejectByteOrderMark;
            RequireFinalLineBreak = requireFinalLineBreak;
            Continuation = continuation;
        }

        /// <summary>
        /// Gets whether a bare line feed is accepted as a line break.
        /// </summary>
        internal bool AcceptBareLineFeed { get; }

        /// <summary>
        /// Gets whether a line may start with <c>group "."</c> (RFC 6350 §3.3), where group is <c>1*(ALPHA / DIGIT / "-")</c>.  The group is then
        /// available from <see cref="ContentLineScanner.Group"/>.  RFC 5545 §3.1 has no groups, so a format that follows it leaves this off.
        /// </summary>
        internal bool AllowGroupPrefix { get; }

        /// <summary>
        /// Gets what decides whether a line continues past the end of a physical line, other than by folding; or <c>null</c> if nothing does, as for a format
        /// that follows RFC 5545 §3.1 or RFC 6350 §3.3.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal IContentLineContinuation? Continuation { get; }
#else
        internal IContentLineContinuation Continuation { get; }
#endif

        /// <summary>
        /// Gets the maximum length of a logical line, or <see cref="int.MaxValue"/> for no limit.
        /// </summary>
        /// <remarks>
        /// It is checked as the text is read, so that the limit also bounds the memory a line takes.  What <see cref="Continuation"/> then removes from the end of a
        /// physical line has already counted.
        /// </remarks>
        internal int MaxLineLength { get; }

        /// <summary>
        /// Gets whether a byte order mark at the start of the data is left in the first line, where parsing rejects it, rather than skipped.
        /// </summary>
        internal bool RejectByteOrderMark { get; }

        /// <summary>
        /// Gets whether the last line must end with a line break, as the <c>contentline</c> rule of RFC 6350 §3.3 requires.  The line is then
        /// parsed and reported as <see cref="ContentLineErrorKind.MissingLineBreak"/>, after any syntax error it has.
        /// </summary>
        internal bool RequireFinalLineBreak { get; }
    }
}
