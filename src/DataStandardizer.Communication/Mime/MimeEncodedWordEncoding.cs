namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The encoding of the text of an RFC 2047 encoded word (RFC 2047 §4).
    /// </summary>
    public enum MimeEncodedWordEncoding
    {
        /// <summary>
        /// The "B" encoding of RFC 2047 §4.1, which is base64. It suits text that is mostly not ASCII.
        /// </summary>
        B = 0,

        /// <summary>
        /// The "Q" encoding of RFC 2047 §4.2, which is like quoted-printable. It suits text that is mostly ASCII, which it leaves readable.
        /// </summary>
        Q = 1,
    }
}
