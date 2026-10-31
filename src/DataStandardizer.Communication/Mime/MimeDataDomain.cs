namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The kind of data a transport must carry for a body (RFC 2045 §2.7–§2.9).
    /// </summary>
    public enum MimeDataDomain
    {
        /// <summary>
        /// The domain is not known, as for an extension transfer encoding, whose output RFC 2045 and RFC 4289 don't constrain.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 7bit data: short lines of US-ASCII characters, with no NULs, and CR and LF only as a CRLF line break (RFC 2045 §2.7).
        /// </summary>
        SevenBit = 1,

        /// <summary>
        /// 8bit data: short lines that may contain octets above 127, with no NULs, and CR and LF only as a CRLF line break (RFC 2045 §2.8).
        /// </summary>
        EightBit = 2,

        /// <summary>
        /// Binary data: any sequence of octets (RFC 2045 §2.9).
        /// </summary>
        Binary = 3,
    }
}
