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
        /// In <see cref="MimeQuotedPrintableMode.Body"/> mode, accept lower-case hexadecimal digits after <c>=</c>, as in <c>=3d</c>.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §6.7 rule 1 requires upper-case digits in a body, and says a robust decoder might accept lower-case ones. The "Q" modes
        /// always accept lower-case digits, as RFC 2047 §4.2 (1) only says that upper case should be used, so this option has no effect on
        /// them.
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

        /// <summary>
        /// In <see cref="MimeQuotedPrintableMode.Body"/> mode, accept encoded lines longer than 76 characters.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §6.7 rule 5 requires that an encoded line, without its transport padding and CRLF, be at most 76 characters long,
        /// and note (5) says a robust decoder might decode longer lines. The option is also for data whose soft line breaks have already
        /// been removed, such as a vCard 2.1 value that has been unfolded. So that the decoder holds a bounded amount of data, a run of
        /// more than 998 spaces and tabs (the line limit of RFC 5322 §2.1.1) is still not valid unless a line break follows it. The "Q"
        /// modes have no lines, so this option has no effect on them.
        /// </remarks>
        AllowLongLines = 8,
    }
}
