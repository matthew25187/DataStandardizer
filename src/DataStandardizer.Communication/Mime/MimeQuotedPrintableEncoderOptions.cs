using System;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// How a quoted-printable encoder treats the data it encodes.
    /// </summary>
    [Flags]
    public enum MimeQuotedPrintableEncoderOptions
    {
        /// <summary>
        /// Encode the data as text: in <see cref="MimeQuotedPrintableMode.Body"/> mode, each CRLF or bare LF in the data is a line break,
        /// and is written as a CRLF line break.
        /// </summary>
        None = 0,

        /// <summary>
        /// In <see cref="MimeQuotedPrintableMode.Body"/> mode, encode every CR and LF in the data as <c>=0D</c> and <c>=0A</c>, so the
        /// encoded data has only soft line breaks.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §6.7 rule 4 says to encode data that is not text this way, as its line breaks are not CRLF line breaks of text. The
        /// "Q" modes always encode CR and LF, so this option has no effect on them.
        /// </remarks>
        Binary = 1,
    }
}
