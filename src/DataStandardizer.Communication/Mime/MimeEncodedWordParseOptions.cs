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
        /// Accept lower-case hexadecimal digits after <c>=</c> in "Q"-encoded text, as in <c>=c3=a9</c>, which RFC 2047 §4.2 (1) and
        /// RFC 2045 §6.7 rule 1 say are written in upper case.
        /// </summary>
        AllowLowercaseHexDigits = 1 << 3,

        /// <summary>
        /// Accept an encoded word whose octets are not valid in its character set, such as <c>=?UTF-8?Q?=FF?=</c> (RFC 2047 §6.3 (2);
        /// RFC 3629 §3 for UTF-8), as from a client that labels the text with the wrong character set. Each invalid sequence is read as
        /// U+FFFD.
        /// </summary>
        /// <remarks>
        /// The netstandard1.1 build can't change how an encoding treats invalid octets, so there, the text is read as the encoding from the
        /// <see cref="IMimeCharsetResolver"/> reads it. Without this option, a word whose encoding throws <see cref="DecoderFallbackException"/>
        /// is not valid, nor is one read as text with U+FFFD, the character that the built-in encodings read invalid octets as, unless
        /// writing the text with the encoding gives the same octets back.
        /// </remarks>
        AllowInvalidOctets = 1 << 4,

        /// <summary>
        /// In <see cref="MimeEncodedWord.DecodeText(ReadOnlySpan{char}, MimeEncodedWordContext, MimeEncodedWordParseOptions, IMimeCharsetResolver)"/>,
        /// join the octets of an encoded word that doesn't hold whole characters with those of the next words, up to 4 words in all, that
        /// have the same character set and language, so that a character split between the words is read whole. RFC 2047 §5 says that each
        /// encoded word must hold a whole number of characters.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A word that holds whole characters is decoded by itself, as are the words of a run whose joined octets are still not valid.
        /// </para>
        /// <para>
        /// This option has no effect on <see cref="MimeEncodedWord.TryParse(ReadOnlySpan{char}, out MimeEncodedWord)"/>, which reads one
        /// word.
        /// </para>
        /// </remarks>
        AllowSplitCharacters = 1 << 5,
    }
}
