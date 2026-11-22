using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Accommodations for RFC 2047 encoded words that don't follow the rules, as many mail clients write them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, an encoded word must follow the RFC 2047 §2 grammar, be at most 75 characters long, have a character set that the
    /// <see cref="IMimeCharsetResolver"/> resolves, and decode to octets that are valid in that character set. An encoded word may have an
    /// RFC 2231 §5 language.
    /// </para>
    /// <para>
    /// <see cref="MimeEncodedWord.TryParse(ReadOnlySpan{char}, MimeEncodedWordContext, MimeEncodedWordParseOptions, IMimeCharsetResolver, out MimeEncodedWord)"/>
    /// rejects a word that breaks a rule that no option here accepts. <see cref="MimeEncodedWord.DecodeText(ReadOnlySpan{char}, MimeEncodedWordContext, MimeEncodedWordParseOptions, IMimeCharsetResolver)"/>
    /// leaves such a word as it is written, as RFC 2047 §6.1 and §6.2 say to.
    /// </para>
    /// </remarks>
    [Flags]
    public enum MimeEncodedWordParseOptions
    {
        /// <summary>
        /// Parse strictly.
        /// </summary>
        None = 0,

        /// <summary>
        /// Accept an encoded word longer than 75 characters, which RFC 2047 §2 doesn't allow, and which RFC 2047 §6.1 says to read as
        /// ordinary text.
        /// </summary>
        AllowLongWords = 1 << 0,

        /// <summary>
        /// In <see cref="MimeEncodedWord.DecodeText(ReadOnlySpan{char}, MimeEncodedWordContext, MimeEncodedWordParseOptions, IMimeCharsetResolver)"/>,
        /// decode an encoded word that is not separated from the text or encoded word next to it by whitespace, such as the one in
        /// <c>Re:=?UTF-8?Q?caf=C3=A9?=</c>, which RFC 2047 §5 doesn't allow, and which RFC 2047 §6.1 says to read as ordinary text.
        /// </summary>
        /// <remarks>
        /// The text around such a word is kept as it is. Whitespace is still dropped only between two encoded words. This option has no
        /// effect on <see cref="MimeEncodedWord.TryParse(ReadOnlySpan{char}, out MimeEncodedWord)"/>, which reads one whole word.
        /// </remarks>
        AllowEmbeddedWords = 1 << 1,

        /// <summary>
        /// Accept an encoded word whose character set the <see cref="IMimeCharsetResolver"/> doesn't resolve. The name of the character set
        /// must still be a valid <c>mime-charset</c> (RFC 2978 §2.3).
        /// </summary>
        /// <remarks>
        /// Each octet of the text is read as the character with the same number, as ISO-8859-1 does, so that the caller can recover the
        /// octets and decode them. This is the "best effort" display that RFC 2047 §6.2 (b) allows; by default the word is read as ordinary
        /// text, as RFC 2047 §6.2 (a) allows. <see cref="MimeEncodedWord.Charset"/> holds the character set name as written.
        /// </remarks>
        AllowUnknownCharsets = 1 << 2,

        /// <summary>
        /// This option has no effect. Lower-case hexadecimal digits after <c>=</c> in "Q"-encoded text, as in <c>=c3=a9</c>, are always
        /// accepted.
        /// </summary>
        /// <remarks>
        /// RFC 2047 §4.2 (1) only says that upper case should be used for the digits. The requirement of RFC 2045 §6.7 rule 1 is for
        /// quoted-printable bodies, where <see cref="MimeQuotedPrintableDecoderOptions.AllowLowercaseHexDigits"/> accepts lower-case
        /// digits.
        /// </remarks>
        AllowLowercaseHexDigits = 1 << 3,

        /// <summary>
        /// Accept an encoded word whose octets are not valid in its character set, such as <c>=?UTF-8?Q?=FF?=</c> (RFC 2047 §6.3 (2);
        /// RFC 3629 §3 for UTF-8), as from a client that labels the text with the wrong character set. Each invalid sequence is read as
        /// U+FFFD.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A word in a character set that switches modes, such as ISO-2022-JP (RFC 1468), must still switch back to ASCII mode by its end
        /// (RFC 2047 §3), so <c>=?ISO-2022-JP?B?GyRCMGw=?=</c> is not accepted.
        /// </para>
        /// <para>
        /// The netstandard1.1 build finds <c>Encoding.Clone</c> and <c>Encoding.DecoderFallback</c> at run time. On a runtime whose
        /// reflection metadata leaves them out, such as .NET Native, it can't change how an encoding treats invalid octets. So there,
        /// without this option, the text that the encoding from the <see cref="IMimeCharsetResolver"/> reads is accepted only if writing it
        /// with the encoding gives the same octets back, and with this option, the invalid octets are read as the encoding's own fallback
        /// reads them.
        /// </para>
        /// </remarks>
        AllowInvalidOctets = 1 << 4,

        /// <summary>
        /// In <see cref="MimeEncodedWord.DecodeText(ReadOnlySpan{char}, MimeEncodedWordContext, MimeEncodedWordParseOptions, IMimeCharsetResolver)"/>,
        /// read a character that an encoded word ends partway through with the octets that finish it at the start of the next words that
        /// have the same character set and language, so that a character split between words is read whole. RFC 2047 §5 says that each
        /// encoded word must hold a whole number of characters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A word that holds whole characters is decoded by itself. The unfinished part of a character is at most 3 octets, as a character
        /// is at most 4 octets in the multi-octet character sets in use, such as UTF-8 (RFC 3629 §3). Each word in a run of such words is
        /// read in the state that the words before it leave: the byte order of UTF-16 or UTF-32 given by a byte order mark at the start of
        /// the run (RFC 2781 §3.2), or the mode of a character set of the ISO/IEC 2022 family, such as ISO-2022-JP (RFC 1468): the sets
        /// designated to G0 and G1, and which of them is shifted in. A word that ends partway through a character or an escape sequence in
        /// such a character set may end outside ASCII mode, but a word of whole characters, and the run, must end in it (RFC 2047 §3).
        /// The octets of the run are then decoded together. If a run doesn't finish its last character, or its octets aren't valid
        /// together, its first word is decoded by itself, as it is without this option, and the words after it are read again.
        /// </para>
        /// <para>
        /// This option has no effect on <see cref="MimeEncodedWord.TryParse(ReadOnlySpan{char}, out MimeEncodedWord)"/>, which reads one
        /// word.
        /// </para>
        /// </remarks>
        AllowSplitCharacters = 1 << 5,
    }
}
