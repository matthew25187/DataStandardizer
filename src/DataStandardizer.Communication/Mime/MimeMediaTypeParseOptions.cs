using System;

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
        AllowWhitespaceAroundDelimiters = 1,

        /// <summary>
        /// Accept type, subtype and parameter names that are RFC 2045 §5.1 tokens but not RFC 6838 §4.2 restricted names, such as names
        /// longer than 127 characters, names that start with a symbol, or names that contain <c>*</c>, <c>%</c>, <c>'</c>, <c>`</c>,
        /// <c>{</c>, <c>|</c>, <c>}</c> or <c>~</c>.
        /// </summary>
        /// <remarks>
        /// A parameter name in an RFC 2231 form, such as <c>title*</c>, <c>title*1</c> or <c>title*1*</c>, is always read as that form, and
        /// only the part before the <c>*</c> must be a valid name.
        /// </remarks>
        AllowRfc2045Tokens = 2,

        /// <summary>
        /// Accept a parameter whose sections are not numbered from 0 with no gaps, such as <c>title*0</c> and <c>title*2</c> with no
        /// <c>title*1</c> (RFC 2231 §3). The sections there are joined in numeric order.
        /// </summary>
        /// <remarks>
        /// When there is no section 0, the value has no character set or language, so its extended sections must be US-ASCII unless
        /// <see cref="AllowUnknownCharsets"/> is also given.
        /// </remarks>
        AllowMissingSections = 4,

        /// <summary>
        /// Accept a parameter with more than one section of the same number, such as two <c>title*1</c> sections (RFC 2231 §3). The first
        /// is used, and the others are ignored.
        /// </summary>
        AllowDuplicateSections = 8,

        /// <summary>
        /// Accept a section number with a leading zero, such as <c>title*01</c> (RFC 2231 §3). It is read as the number without the zeros,
        /// so <c>title*01</c> is section 1.
        /// </summary>
        AllowLeadingZeroSectionNumbers = 16,

        /// <summary>
        /// Accept an extended parameter value whose character set the <see cref="IMimeCharsetResolver"/> doesn't resolve, or whose octets
        /// are not US-ASCII when it gives no character set (RFC 2231 §4).
        /// </summary>
        /// <remarks>
        /// Each octet of the value is read as the character with the same number, as ISO-8859-1 does, so that the caller can recover the
        /// octets and decode them. <see cref="MimeParameter.Charset"/> holds the character set name as written.
        /// </remarks>
        AllowUnknownCharsets = 32,

        /// <summary>
        /// Accept an extended parameter value written as a quoted-string, such as <c>title*="utf-8''a%20b"</c>, which RFC 2231 §7 doesn't
        /// allow. The value may then also hold spaces and the other characters that a quoted-string can.
        /// </summary>
        AllowQuotedExtendedValues = 64,
    }
}
