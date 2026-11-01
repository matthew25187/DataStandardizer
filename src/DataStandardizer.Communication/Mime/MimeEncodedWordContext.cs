namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The place in a header field where an RFC 2047 encoded word appears (RFC 2047 §5), which sets the characters that its "Q"-encoded text
    /// may hold.
    /// </summary>
    /// <remarks>
    /// The "B" encoding writes only characters that every context allows. Which context applies at a given place in a header field is set by
    /// the grammar of the field, which is up to the caller.
    /// </remarks>
    public enum MimeEncodedWordContext
    {
        /// <summary>
        /// An encoded word that replaces <c>text</c> in a <c>*text</c> header field, such as Subject (RFC 2047 §5(1)): its "Q"-encoded text
        /// may hold any printable ASCII character other than <c>?</c>.
        /// </summary>
        Text = 0,

        /// <summary>
        /// An encoded word in a comment (RFC 2047 §5(2)): as <see cref="Text"/>, except that its "Q"-encoded text may not hold <c>(</c>,
        /// <c>)</c> or <c>"</c>.
        /// </summary>
        Comment = 1,

        /// <summary>
        /// An encoded word that replaces a word in a phrase, such as the display name of an address (RFC 2047 §5(3)): its "Q"-encoded text
        /// may hold only ASCII letters, digits, <c>!</c>, <c>*</c>, <c>+</c>, <c>-</c>, <c>/</c>, <c>=</c> and <c>_</c>.
        /// </summary>
        Phrase = 2,
    }
}
