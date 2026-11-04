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
        internal ContentLineScannerSettings(bool acceptBareLineFeed, int maxLineLength, bool allowGroupPrefix = false)
        {
            AcceptBareLineFeed = acceptBareLineFeed;
            MaxLineLength = maxLineLength;
            AllowGroupPrefix = allowGroupPrefix;
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
        /// Gets the maximum length of a logical line, or <see cref="int.MaxValue"/> for no limit.
        /// </summary>
        internal int MaxLineLength { get; }
    }
}
