namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The lexical tokens of a structured header field (RFC 5322 §3.2).
    /// </summary>
    internal enum ImfTokenKind
    {
        /// <summary>
        /// There is no more input.
        /// </summary>
        End = 0,

        /// <summary>
        /// A run of <c>atext</c> (RFC 5322 §3.2.3).  A dot-atom is a series of these separated by <see cref="Period"/>.
        /// </summary>
        Atom,

        /// <summary>
        /// A quoted-string, with its quotes (RFC 5322 §3.2.4).
        /// </summary>
        QuotedString,

        /// <summary>
        /// A domain-literal, with its brackets (RFC 5322 §3.4.1).
        /// </summary>
        DomainLiteral,

        /// <summary>
        /// A whole comment, with its parentheses and any comments nested in it (RFC 5322 §3.2.2).
        /// </summary>
        Comment,

        /// <summary>
        /// <c>&lt;</c>
        /// </summary>
        LessThan,

        /// <summary>
        /// <c>&gt;</c>
        /// </summary>
        GreaterThan,

        /// <summary>
        /// <c>@</c>
        /// </summary>
        At,

        /// <summary>
        /// <c>,</c>
        /// </summary>
        Comma,

        /// <summary>
        /// <c>:</c>
        /// </summary>
        Colon,

        /// <summary>
        /// <c>;</c>
        /// </summary>
        Semicolon,

        /// <summary>
        /// <c>.</c>
        /// </summary>
        Period
    }
}
