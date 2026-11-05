namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// How <see cref="ContentLineFormatter"/> breaks a long content line.
    /// </summary>
    internal enum ContentLineFoldMode
    {
        /// <summary>Lines are folded at the fold length, with a line break and a white space character.</summary>
        Fold,

        /// <summary>Lines are not folded, whatever their length.</summary>
        None
    }
}
