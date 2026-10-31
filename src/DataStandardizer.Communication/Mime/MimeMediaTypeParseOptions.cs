using System;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Accommodations for media types that don't follow the strict grammar.
    /// </summary>
    /// <remarks>
    /// By default, a media type is parsed by the RFC 2045 §5.1 grammar, with no whitespace or comments, and its type, subtype and parameter
    /// names must follow the RFC 6838 §4.2 naming rules.
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
        AllowRfc2045Tokens = 2,
    }
}
