namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// What <see cref="ContentLineFormatter"/> does with the characters of a parameter value that only RFC 6868 caret encoding can represent: a double
    /// quote and a line break.
    /// </summary>
    internal enum ContentLineCaretMode
    {
        /// <summary>Caret encoding applies: <c>^</c> becomes <c>^^</c>, a double quote <c>^'</c> and a line break <c>^n</c>.</summary>
        Encode,

        /// <summary>Caret encoding doesn't apply, so <c>^</c> is an ordinary character, and a double quote or a line break is an error.</summary>
        Reject,

        /// <summary>Caret encoding doesn't apply, so <c>^</c> is an ordinary character, a double quote is left out and a line break becomes a space.</summary>
        Omit
    }
}
