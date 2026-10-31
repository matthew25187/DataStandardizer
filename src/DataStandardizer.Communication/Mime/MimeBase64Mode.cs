namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A form of base64, which sets how encoded data is laid out.
    /// </summary>
    /// <remarks>
    /// Both forms use the base64 alphabet of RFC 4648 §4, with <c>=</c> padding the last group, and both decode to the same bytes.
    /// </remarks>
    public enum MimeBase64Mode
    {
        /// <summary>
        /// The base64 of RFC 4648 §4: padding is required, and line breaks, whitespace and other characters outside the alphabet are not
        /// allowed.
        /// </summary>
        /// <remarks>
        /// This is the form of an iCalendar BINARY value (RFC 5545 §3.3.1), and of the RFC 2047 §4.1 "B" encoding used in encoded words
        /// and in vCard 3.0 <c>ENCODING=b</c>.
        /// </remarks>
        Strict = 0,

        /// <summary>
        /// The <c>base64</c> Content-Transfer-Encoding of RFC 2045 §6.8: the encoded data is broken into lines of at most 76 characters,
        /// separated by CRLF.
        /// </summary>
        /// <remarks>
        /// When decoding, line breaks and any other characters outside the alphabet are ignored, as RFC 2045 §6.8 requires, unless
        /// <see cref="MimeBase64DecoderOptions.RejectNonAlphabetCharacters"/> is set.
        /// </remarks>
        Mime = 1,
    }
}
