using System;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Checks that a base64 decoder makes beyond those of its <see cref="MimeBase64Mode"/>.
    /// </summary>
    [Flags]
    public enum MimeBase64DecoderOptions
    {
        /// <summary>
        /// Decode as the mode requires.
        /// </summary>
        None = 0,

        /// <summary>
        /// In <see cref="MimeBase64Mode.Mime"/> mode, accept only CRLF line breaks between the characters of the alphabet, and reject any
        /// other character, including a bare CR or LF.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §6.8 says to ignore characters outside the alphabet, but also that they are probably a sign of a transmission error.
        /// This option finds that error. It has no effect in <see cref="MimeBase64Mode.Strict"/> mode, which accepts nothing outside the
        /// alphabet.
        /// </remarks>
        RejectNonAlphabetCharacters = 1,
    }
}
