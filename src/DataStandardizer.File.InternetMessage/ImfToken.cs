namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Where an <see cref="ImfTokenizer"/> found a token in its input.
    /// </summary>
    internal readonly struct ImfToken
    {
        internal ImfToken(ImfTokenKind kind, int start, int length, bool isPrecededByCfws, bool isPrecededByComment = false)
        {
            Kind = kind;
            Start = start;
            Length = length;
            IsPrecededByCfws = isPrecededByCfws;
            IsPrecededByComment = isPrecededByComment;
        }

        internal ImfTokenKind Kind { get; }

        internal int Start { get; }

        internal int Length { get; }

        /// <summary>
        /// Gets a flag indicating that folding white space or a comment comes directly before the token.
        /// </summary>
        internal bool IsPrecededByCfws { get; }

        /// <summary>
        /// Gets a flag indicating that a comment comes before the token, with nothing but folding white space between them.  It is set only by
        /// <see cref="ImfTokenizer.TryRead"/>.
        /// </summary>
        internal bool IsPrecededByComment { get; }

        /// <summary>
        /// Gets a flag indicating that the token is a word: an atom or a quoted-string (RFC 5322 §3.2.5).
        /// </summary>
        internal bool IsWord => Kind == ImfTokenKind.Atom || Kind == ImfTokenKind.QuotedString;
    }
}
