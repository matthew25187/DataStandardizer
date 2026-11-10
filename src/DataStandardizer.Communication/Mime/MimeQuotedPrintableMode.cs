namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A form of quoted-printable: the <c>quoted-printable</c> Content-Transfer-Encoding of a body, or the "Q" encoding of an encoded word
    /// in one of the places it can appear.
    /// </summary>
    /// <remarks>
    /// Every form writes an octet that needs it as <c>=</c> and two upper-case hexadecimal digits. The "Q" forms write a space as
    /// <c>_</c>, and have no line breaks.
    /// </remarks>
    public enum MimeQuotedPrintableMode
    {
        /// <summary>
        /// The <c>quoted-printable</c> Content-Transfer-Encoding of RFC 2045 §6.7: the encoded data is in lines of at most 76 characters,
        /// with <c>=</c> at the end of a line marking a soft line break.
        /// </summary>
        /// <remarks>
        /// This is also the form of vCard 2.1 <c>ENCODING=QUOTED-PRINTABLE</c> values.
        /// </remarks>
        Body = 0,

        /// <summary>
        /// The "Q" encoding of RFC 2047 §4.2, for an encoded word that replaces <c>text</c> in a <c>*text</c> header field such as
        /// Subject (RFC 2047 §5(1)): any printable ASCII character other than <c>=</c>, <c>?</c> and <c>_</c> can be written as itself.
        /// </summary>
        EncodedWordText = 1,

        /// <summary>
        /// The "Q" encoding of RFC 2047 §4.2, for an encoded word in a comment (RFC 2047 §5(2)): as
        /// <see cref="EncodedWordText"/>, except that <c>(</c>, <c>)</c> and <c>"</c> are encoded too.
        /// </summary>
        /// <remarks>
        /// When encoding, <c>\</c> is written as <c>=5C</c> as well, as it would start a quoted-pair in a comment (RFC 5322 §3.2.2). When
        /// decoding, a literal <c>\</c> is accepted, as RFC 2047 §5(2) allows it.
        /// </remarks>
        EncodedWordComment = 2,

        /// <summary>
        /// The "Q" encoding of RFC 2047 §4.2, for an encoded word that replaces a word in a phrase, such as the display name of an
        /// address (RFC 2047 §5(3)): only ASCII letters, digits, <c>!</c>, <c>*</c>, <c>+</c>, <c>-</c> and <c>/</c> can be written as
        /// themselves.
        /// </summary>
        EncodedWordPhrase = 3,
    }
}
