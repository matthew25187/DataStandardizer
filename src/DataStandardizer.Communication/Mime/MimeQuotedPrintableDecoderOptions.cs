using System;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Forms that a quoted-printable decoder accepts although RFC 2045 §6.7 and RFC 2047 §4.2 don't allow them, because many producers
    /// write them.
    /// </summary>
    [Flags]
    public enum MimeQuotedPrintableDecoderOptions
    {
        /// <summary>
        /// Accept only the forms the mode allows.
        /// </summary>
        None = 0,

        /// <summary>
        /// Accept lower-case hexadecimal digits after <c>=</c>, as in <c>=3d</c>.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §6.7 rule 1 requires upper-case digits, and says a robust decoder might accept lower-case ones.
        /// </remarks>
        AllowLowercaseHexDigits = 1,

        /// <summary>
        /// In <see cref="MimeQuotedPrintableMode.Body"/> mode, accept a bare LF as a line break, which is decoded as LF.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §6.7 allows only CRLF line breaks. With this option, trailing whitespace before a bare LF is removed, and <c>=</c>
        /// before it is a soft line break. A bare CR is never accepted. The "Q" modes have no line breaks, so this option has no effect
        /// on them.
        /// </remarks>
        AllowBareLineFeeds = 2,

        /// <summary>
        /// Accept an <c>=</c> that is not followed by two hexadecimal digits or a line break, and decode it as itself, with the
        /// characters after it.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §6.7 note (2) suggests this for a robust decoder. It includes an <c>=</c> at the end of the data.
        /// </remarks>
        AllowLiteralEqualsSigns = 4,
    }
}
