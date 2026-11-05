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
        None,

        /// <summary>The value is quoted-printable, and is broken with soft line breaks (RFC 2045 §6.7 rule 5), so that no physical line reaches 76
        /// characters, as the vCard 2.1 specification §2.1.3 requires.  A <c>=</c> and the two digits after it stay together.</summary>
        QuotedPrintableSoftBreak,

        /// <summary>The value is base64, and is written as the examples of the vCard 2.1 specification have it: on the lines after the property's own, indented,
        /// ending at an empty line.  White space in the value is left out, since the line breaks and indents are this mode's own.</summary>
        Base64Lines
    }
}
