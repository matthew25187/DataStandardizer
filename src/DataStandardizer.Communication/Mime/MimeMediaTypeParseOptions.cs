using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Accommodations for media types that don't follow the strict grammar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// By default, a media type is parsed by the RFC 2045 §5.1 grammar, with no whitespace or comments, and its type, subtype and parameter
    /// names must follow the RFC 6838 §4.2 naming rules.
    /// </para>
    /// <para>
    /// Parameter values split into sections, or given with a character set and language, are decoded by the RFC 2231 rules. A parameter
    /// whose sections or character set break those rules makes the whole media type invalid, unless an option here accepts it.
    /// </para>
    /// </remarks>
    [Flags]
    public enum MimeMediaTypeParseOptions
    {
        /// <summary>
        /// Parse strictly.
        /// </summary>
        None = 0,

        /// <summary>
        /// Accept spaces and horizontal tabs before and after each <c>;</c> and <c>=</c> delimiter, and at the end, as in
        /// <c>text/plain; charset = us-ascii</c>.
        /// </summary>
        /// <remarks>
        /// Comments and folding are still not accepted. Remove them first.
        /// </remarks>
        AllowWhitespaceAroundDelimiters = 1 << 0,

        /// <summary>
        /// Accept type, subtype and parameter names that are RFC 2045 §5.1 tokens but not RFC 6838 §4.2 restricted names, such as names
        /// longer than 127 characters, names that start with a symbol, or names that contain <c>*</c>, <c>%</c>, <c>'</c>, <c>`</c>,
        /// <c>{</c>, <c>|</c>, <c>}</c> or <c>~</c>.
        /// </summary>
        /// <remarks>
        /// A parameter name in an RFC 2231 form, such as <c>title*</c>, <c>title*1</c> or <c>title*1*</c>, is always read as that form, and
        /// only the part before the form must be a valid name. The form is read from the end of the name, so <c>c*d*0</c> is section 0 of
        /// <c>c*d</c>. A name whose part before the form itself ends as a form does, such as <c>a**</c> or <c>a*1*2</c>, is not valid, as
        /// it can't be written back unambiguously.
        /// </remarks>
        AllowRfc2045Tokens = 1 << 1,

        /// <summary>
        /// Accept a parameter whose sections are not numbered from 0 with no gaps, such as <c>title*0</c> and <c>title*2</c> with no
        /// <c>title*1</c> (RFC 2231 §3). The sections there are joined in numeric order.
        /// </summary>
        /// <remarks>
        /// When there is no section 0, the value has no character set or language, so its extended sections must be US-ASCII unless
        /// <see cref="AllowUnknownCharsets"/> is also given.
        /// </remarks>
        AllowMissingSections = 1 << 2,

        /// <summary>
        /// Accept a parameter with more than one section of the same number, such as two <c>title*1</c> sections (RFC 2231 §3). The first
        /// is used, and the others are ignored.
        /// </summary>
        AllowDuplicateSections = 1 << 3,

        /// <summary>
        /// Accept a section number with a leading zero, such as <c>title*01</c> (RFC 2231 §3). It is read as the number without the zeros,
        /// so <c>title*01</c> is section 1.
        /// </summary>
        AllowLeadingZeroSectionNumbers = 1 << 4,

        /// <summary>
        /// Accept an extended parameter value whose character set the <see cref="IMimeCharsetResolver"/> doesn't resolve, or whose octets
        /// are not US-ASCII when it gives no character set (RFC 2231 §4). The name of the character set must still be a valid
        /// <c>mime-charset</c> (RFC 2978 §2.3).
        /// </summary>
        /// <remarks>
        /// Each octet of the value is read as the character with the same number, as ISO-8859-1 does, so that the caller can recover the
        /// octets and decode them. <see cref="MimeParameter.Charset"/> holds the character set name as written.
        /// </remarks>
        AllowUnknownCharsets = 1 << 5,

        /// <summary>
        /// Accept an extended parameter value written as a quoted-string, such as <c>title*="utf-8''a%20b"</c>, which RFC 2231 §7 doesn't
        /// allow. The value may then also hold spaces and the other characters that a quoted-string can.
        /// </summary>
        AllowQuotedExtendedValues = 1 << 6,

        /// <summary>
        /// Accept lower-case hexadecimal digits in the <c>%</c> escapes of an extended parameter value, such as <c>%c3%a9</c>, which
        /// RFC 2231 §7 doesn't allow.
        /// </summary>
        AllowLowercaseHexDigits = 1 << 7,

        /// <summary>
        /// Accept an extended parameter value whose octets are not valid in its character set, such as <c>title*=UTF-8''%FF</c>
        /// (RFC 2231 §4; RFC 3629 §3 for UTF-8), as from a client that labels the value with the wrong character set. Each invalid
        /// sequence is read as U+FFFD.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The octets of a value with no character set must still be US-ASCII, unless <see cref="AllowUnknownCharsets"/> is also given.
        /// </para>
        /// <para>
        /// The netstandard1.1 build can't change how an encoding treats invalid octets, so there, the value is read as the encoding from the
        /// <see cref="IMimeCharsetResolver"/> reads it. Without this option, a value whose encoding throws <see cref="DecoderFallbackException"/>
        /// is not valid, nor is one read as text with U+FFFD, the character that the built-in encodings read invalid octets as, unless
        /// writing the text with the encoding gives the same octets back.
        /// </para>
        /// </remarks>
        AllowInvalidOctets = 1 << 8,

        /// <summary>
        /// Accept a parameter whose sections break any of the RFC 2231 §3 numbering rules: <see cref="AllowMissingSections"/>,
        /// <see cref="AllowDuplicateSections"/> and <see cref="AllowLeadingZeroSectionNumbers"/>.
        /// </summary>
        AllowNonstandardSections = AllowMissingSections | AllowDuplicateSections | AllowLeadingZeroSectionNumbers,

        /// <summary>
        /// Accept an extended parameter value that breaks the RFC 2231 §7 grammar in either of the ways that mail clients commonly do:
        /// <see cref="AllowQuotedExtendedValues"/> and <see cref="AllowLowercaseHexDigits"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="AllowUnknownCharsets"/> is not included, as it changes how a value is decoded rather than which values are accepted.
        /// Give it as well to accept character sets that the <see cref="IMimeCharsetResolver"/> doesn't resolve.
        /// </remarks>
        AllowNonstandardExtendedValues = AllowQuotedExtendedValues | AllowLowercaseHexDigits,
    }
}
