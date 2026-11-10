using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DataStandardizer.Communication.Mime;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Reads and writes the bodies of the MIME header fields whose values are not types of this package: MIME-Version, Content-Type, Content-Transfer-Encoding and
    /// Content-Language.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each one is a structured field, so comments and folding white space may come between its tokens (RFC 5322 §3.2.2).  Each format method gives an unfolded body
    /// with a space at each place it can be folded, for <see cref="ImfHeaderFieldKind.Structured"/>.
    /// </para>
    /// <para>
    /// The other MIME fields have their own types.  A Content-ID is an <see cref="ImfMessageId"/> (RFC 2045 §7), a Content-Description is the unstructured text of
    /// <see cref="ImfHeaderText"/> (RFC 2045 §8), and a Content-Disposition is an <see cref="ImfContentDisposition"/> (RFC 2183).
    /// </para>
    /// </remarks>
    public static class ImfMimeFields
    {
        /// <summary>
        /// The accommodations that <see cref="ImfOptions.AllowMalformedParameters"/> asks for.
        /// </summary>
        internal const MimeMediaTypeParseOptions MalformedParameterOptions = MimeMediaTypeParseOptions.AllowNonstandardSections
            | MimeMediaTypeParseOptions.AllowNonstandardExtendedValues
            | MimeMediaTypeParseOptions.AllowUnknownCharsets
            | MimeMediaTypeParseOptions.AllowInvalidOctets;

        // ref. RFC 5322 §2.1.1: a line SHOULD be no more than 78 characters.  A section goes on a line of its own, after a space and before a semicolon.
        private const int MaxSectionLength = 76;

        // ref. RFC 2017 §3.1: URL-word := token ; Must not exceed 40 characters in length
        private const int MaxUrlWordLength = 40;

        private const string ContentNameParameterName = "name";
        private const string InvalidContentLanguageMessage = "The Content-Language field body is not in a valid format.";
        private const string InvalidContentTypeMessage = "The Content-Type field body is not in a valid format.";
        private const string InvalidMimeVersionMessage = "The MIME-Version field body is not in a valid format.";
        private const string InvalidTransferEncodingMessage = "The Content-Transfer-Encoding field body is not in a valid format.";

        private static readonly char[] WhiteSpace = { ' ', '\t' };

        /// <summary>
        /// The version that RFC 2045 §4 defines and requires a message to give, which formats as <c>1.0</c>.
        /// </summary>
        public static readonly Version MimeVersion = new Version(1, 0);

        /// <summary>
        /// Parses the body of a MIME-Version field.
        /// </summary>
        /// <remarks>
        /// Any version is read, but only <see cref="MimeVersion"/> is defined, and RFC 2045 §4 says that a message with another can't be assumed to follow it.  A number
        /// too large for an <see cref="int"/> is rejected.
        /// </remarks>
        /// <param name="value">The field body as UTF-8, which may have comments, such as <c>1.(produced by MetaSend Vx.x)0</c>.</param>
        /// <returns>The version.</returns>
        /// <exception cref="FormatException">The value is not a valid version.</exception>
        public static Version ParseMimeVersion(ReadOnlySpan<byte> value)
        {
            if (!TryParseMimeVersion(value, out var result)) throw new FormatException(InvalidMimeVersionMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a MIME-Version field.
        /// </summary>
        /// <remarks>
        /// Any version is read, but only <see cref="MimeVersion"/> is defined, and RFC 2045 §4 says that a message with another can't be assumed to follow it.  A number
        /// too large for an <see cref="int"/> is rejected.
        /// </remarks>
        /// <param name="value">The field body, which may have comments, such as <c>1.(produced by MetaSend Vx.x)0</c>.</param>
        /// <returns>The version.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid version.</exception>
        public static Version ParseMimeVersion(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParseMimeVersion(value, out var result)) throw new FormatException(InvalidMimeVersionMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a MIME-Version field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may have comments, such as <c>1.(produced by MetaSend Vx.x)0</c>.</param>
        /// <param name="result">The version, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid version; otherwise <c>false</c>.</returns>
        public static bool TryParseMimeVersion(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out Version result)
        {
            // ref. RFC 2045 §4: version := "MIME-Version" ":" 1*DIGIT "." 1*DIGIT, and comments are to be ignored.
            result = null;
            if (!ImfTokenizer.IsWellFormed(value)) return false;

            var position = 0;
            if (!TryReadVersionNumber(value, ref position, out var major)) return false;
            if (position == value.Length || value[position] != (byte)'.') return false;

            position++;
            if (!TryReadVersionNumber(value, ref position, out var minor) || position != value.Length) return false;

            result = new Version(major, minor);
            return true;
        }

        /// <summary>
        /// Parses the body of a MIME-Version field.
        /// </summary>
        /// <param name="value">The field body, which may have comments, such as <c>1.(produced by MetaSend Vx.x)0</c>.</param>
        /// <param name="result">The version, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid version; otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParseMimeVersion([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out Version result)
        {
            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParseMimeVersion(bytes, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/> describes.</exception>
        public static MimeMediaType ParseContentType(ReadOnlySpan<byte> value)
        {
            return ParseContentType(value, ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.  Bytes that aren't valid
        /// UTF-8 are decoded with <see cref="ImfOptions.HeaderFallbackCharset"/>, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedParameters"/>,
        /// <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/> describes.</exception>
        public static MimeMediaType ParseContentType(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParseContentType(value, options, out var result)) throw new FormatException(InvalidContentTypeMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/> describes.</exception>
        public static MimeMediaType ParseContentType(string value)
        {
            return ParseContentType(value, ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedParameters"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The media type.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/> describes.</exception>
        public static MimeMediaType ParseContentType(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParseContentType(value, options, out var result)) throw new FormatException(InvalidContentTypeMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/> describes; otherwise <c>false</c>.</returns>
        public static bool TryParseContentType(ReadOnlySpan<byte> value, out MimeMediaType result)
        {
            return TryParseContentType(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The grammar is that of RFC 2045 §5.1, so a name may be any token, not only the narrower names that RFC 6838 §4.2 requires of a registration.  Parameters in the
        /// forms of RFC 2231 are joined and decoded.  A quoted-string may hold raw UTF-8, as RFC 6532 §3.2 allows.  A token must be US-ASCII, as RFC 2045 §5.1 requires.
        /// </para>
        /// <para>
        /// A value breaks RFC 2231 if its sections or extended value aren't valid, or a character set can't be resolved, and it breaks RFC 2047 §5 if a parameter holds an
        /// encoded word, which is never allowed there.  Each is rejected unless <see cref="ImfOptions.AllowMalformedParameters"/> is <c>true</c>, which describes what is
        /// then accepted.
        /// </para>
        /// </remarks>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.  Bytes that aren't valid
        /// UTF-8 are decoded with <see cref="ImfOptions.HeaderFallbackCharset"/>, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowMalformedParameters"/>,
        /// <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParseContentType(ReadOnlySpan<byte> value, ImfOptions options, out MimeMediaType result)
        {
            ImfHeaderText.CheckOptions(options);

            return TryParseParameterized(value, string.Empty, ContentNameParameterName, options.AllowMalformedParameters, GetContentTypeParseOptions(options), options, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/> describes; otherwise
        /// <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParseContentType([AllowNull, NotNullWhen(true)] string value, out MimeMediaType result)
        {
            return TryParseContentType(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Type field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>text/plain; charset=us-ascii (Plain text)</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowMalformedParameters"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="result">The media type, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/> describes; otherwise
        /// <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParseContentType([AllowNull, NotNullWhen(true)] string value, ImfOptions options, out MimeMediaType result)
        {
            ImfHeaderText.CheckOptions(options);

            result = default;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParseContentType(bytes, options, out result);
        }

        /// <summary>
        /// Writes a media type as the body of a Content-Type field, such as <c>text/plain; charset=us-ascii</c>.
        /// </summary>
        /// <remarks>
        /// A parameter is written as a token or a quoted-string.  One that isn't US-ASCII, or that has a language, is written as an extended value, and one too long for a
        /// line of 78 characters is split into sections (RFC 2231).
        /// </remarks>
        /// <param name="mediaType">The media type.</param>
        /// <returns>The field body, unfolded.</returns>
        /// <exception cref="ArgumentException"><paramref name="mediaType"/> is not initialised.</exception>
        public static string FormatContentType(MimeMediaType mediaType)
        {
            if (mediaType == default) throw new ArgumentException("The media type is not initialised.", nameof(mediaType));

            var output = new StringBuilder();
            output.Append(mediaType.Type).Append('/').Append(mediaType.Subtype);
            AppendParameters(mediaType.Parameters, output);
            return output.ToString();
        }

        /// <summary>
        /// Parses the body of a Content-Transfer-Encoding field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>base64 (binary data)</c>.</param>
        /// <returns>The transfer encoding.</returns>
        /// <exception cref="FormatException">The value is not a valid mechanism (RFC 2045 §6.1).</exception>
        public static MimeTransferEncoding ParseContentTransferEncoding(ReadOnlySpan<byte> value)
        {
            if (!TryParseContentTransferEncoding(value, out var result)) throw new FormatException(InvalidTransferEncodingMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Transfer-Encoding field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>base64 (binary data)</c>.</param>
        /// <returns>The transfer encoding.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid mechanism (RFC 2045 §6.1).</exception>
        public static MimeTransferEncoding ParseContentTransferEncoding(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParseContentTransferEncoding(value, out var result)) throw new FormatException(InvalidTransferEncodingMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Transfer-Encoding field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>base64 (binary data)</c>.</param>
        /// <param name="result">The transfer encoding, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mechanism (RFC 2045 §6.1); otherwise <c>false</c>.</returns>
        public static bool TryParseContentTransferEncoding(ReadOnlySpan<byte> value, out MimeTransferEncoding result)
        {
            result = default;
            return ImfTokenizer.IsWellFormed(value) && TryRemoveCfws(value, out var text) && MimeTransferEncoding.TryParse(text, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Transfer-Encoding field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>base64 (binary data)</c>.</param>
        /// <param name="result">The transfer encoding, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mechanism (RFC 2045 §6.1); otherwise <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParseContentTransferEncoding([AllowNull, NotNullWhen(true)] string value, out MimeTransferEncoding result)
        {
            result = default;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParseContentTransferEncoding(bytes, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.</param>
        /// <returns>The language tags, in order.</returns>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentLanguage(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{Bcp47LanguageTag})"/> describes.</exception>
        public static IReadOnlyList<Bcp47LanguageTag> ParseContentLanguage(ReadOnlySpan<byte> value)
        {
            return ParseContentLanguage(value, ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.  Bytes that aren't valid UTF-8 are
        /// decoded with <see cref="ImfOptions.HeaderFallbackCharset"/>, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowInvalidValues"/>,
        /// <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <returns>The language tags, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentLanguage(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{Bcp47LanguageTag})"/> describes.</exception>
        public static IReadOnlyList<Bcp47LanguageTag> ParseContentLanguage(ReadOnlySpan<byte> value, ImfOptions options)
        {
            if (!TryParseContentLanguage(value, options, out var result)) throw new FormatException(InvalidContentLanguageMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.</param>
        /// <returns>The language tags, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentLanguage(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{Bcp47LanguageTag})"/> describes.</exception>
        public static IReadOnlyList<Bcp47LanguageTag> ParseContentLanguage(string value)
        {
            return ParseContentLanguage(value, ImfHeaderText.DefaultOptions);
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <returns>The language tags, in order.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        /// <exception cref="FormatException">The value is not valid, as <see cref="TryParseContentLanguage(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{Bcp47LanguageTag})"/> describes.</exception>
        public static IReadOnlyList<Bcp47LanguageTag> ParseContentLanguage(string value, ImfOptions options)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));
            if (!TryParseContentLanguage(value, options, out var result)) throw new FormatException(InvalidContentLanguageMessage);

            return result;
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <param name="value">The field body as UTF-8, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.</param>
        /// <param name="result">The language tags, in order, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParseContentLanguage(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{Bcp47LanguageTag})"/> describes; otherwise <c>false</c>.</returns>
        public static bool TryParseContentLanguage(ReadOnlySpan<byte> value, [MaybeNullWhen(false)] out IReadOnlyList<Bcp47LanguageTag> result)
        {
            return TryParseContentLanguage(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <remarks>
        /// RFC 3282 §2 needs at least one tag, and no empty elements.  A tag has to be well-formed by the current edition of BCP 47 (RFC 5646 §2.2.9).  One that isn't is
        /// rejected, unless <see cref="ImfOptions.AllowInvalidValues"/> is <c>true</c>, when it is left out of the list.  The list is then empty if no tag was well-formed.
        /// </remarks>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.  Bytes that aren't valid UTF-8 are
        /// decoded with <see cref="ImfOptions.HeaderFallbackCharset"/>, if <see cref="ImfOptions.AllowEightBitHeaders"/> is <c>true</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowEightBitHeaders"/>, <see cref="ImfOptions.AllowInvalidValues"/>,
        /// <see cref="ImfOptions.CharsetResolver"/> and <see cref="ImfOptions.HeaderFallbackCharset"/>.</param>
        /// <param name="result">The language tags, in order, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParseContentLanguage(ReadOnlySpan<byte> value, ImfOptions options, [MaybeNullWhen(false)] out IReadOnlyList<Bcp47LanguageTag> result)
        {
            ImfHeaderText.CheckOptions(options);

            // TODO: RFC 3282 §2 cites RFC 3066, whose tags can be wider than a Bcp47LanguageTag, such as en-a.  Revisit this if per-edition language tag types are added.
            // ref. RFC 3282 §2: Language-List = Language-Tag [CFWS] *("," [CFWS] Language-Tag [CFWS])
            result = null;
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8)) return false;

            var tags = new List<Bcp47LanguageTag>();
            var position = 0;
            while (true)
            {
                if (!ImfTokenizer.TrySkipCfws(utf8, ref position)) return false;

                var start = position;
                while (position < utf8.Length && !IsLanguageTagEnd(utf8[position]))
                {
                    position++;
                }

                if (position == start) return false;

                var text = Encoding.UTF8.GetString(utf8.Slice(start, position - start).ToArray(), 0, position - start);
                if (Bcp47LanguageTag.TryCreate(text, out var tag))
                {
                    tags.Add(tag);
                }
                else if (!options.AllowInvalidValues)
                {
                    return false;
                }

                if (!ImfTokenizer.TrySkipCfws(utf8, ref position)) return false;

                if (position == utf8.Length) break;
                if (utf8[position] != (byte)',') return false;

                position++;
            }

            result = new ReadOnlyCollection<Bcp47LanguageTag>(tags);
            return true;
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.</param>
        /// <param name="result">The language tags, in order, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParseContentLanguage(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{Bcp47LanguageTag})"/> describes; otherwise
        /// <c>false</c>, including for <c>null</c>.</returns>
        public static bool TryParseContentLanguage([AllowNull, NotNullWhen(true)] string value, [MaybeNullWhen(false)] out IReadOnlyList<Bcp47LanguageTag> result)
        {
            return TryParseContentLanguage(value, ImfHeaderText.DefaultOptions, out result);
        }

        /// <summary>
        /// Parses the body of a Content-Language field.
        /// </summary>
        /// <param name="value">The field body, which may be folded and have comments, such as <c>en, fr (This is a dictionary)</c>.</param>
        /// <param name="options">The options, of which this uses <see cref="ImfOptions.AllowInvalidValues"/> and <see cref="ImfOptions.CharsetResolver"/>.</param>
        /// <param name="result">The language tags, in order, if the value is valid.</param>
        /// <returns><c>true</c> if the value is valid, as <see cref="TryParseContentLanguage(ReadOnlySpan{byte}, ImfOptions, out IReadOnlyList{Bcp47LanguageTag})"/> describes; otherwise
        /// <c>false</c>, including for <c>null</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or a
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public static bool TryParseContentLanguage([AllowNull, NotNullWhen(true)] string value, ImfOptions options, [MaybeNullWhen(false)] out IReadOnlyList<Bcp47LanguageTag> result)
        {
            ImfHeaderText.CheckOptions(options);

            result = null;
            return ImfGrammar.TryGetBytes(value, out var bytes) && TryParseContentLanguage(bytes, options, out result);
        }

        /// <summary>
        /// Writes language tags as the body of a Content-Language field, such as <c>en, fr</c>.
        /// </summary>
        /// <param name="languageTags">The language tags, in order.</param>
        /// <returns>The field body, unfolded.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="languageTags"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="languageTags"/> is empty, as RFC 3282 §2 needs at least one, or holds a tag that is not initialised.</exception>
        public static string FormatContentLanguage(IEnumerable<Bcp47LanguageTag> languageTags)
        {
            if (languageTags is null) throw new ArgumentNullException(nameof(languageTags));

            var output = new StringBuilder();
            foreach (var languageTag in languageTags)
            {
                if (languageTag == default) throw new ArgumentException("A language tag is not initialised.", nameof(languageTags));

                if (output.Length > 0) output.Append(", ");

                output.Append(languageTag.ToString());
            }

            if (output.Length == 0) throw new ArgumentException("The list has no language tags.", nameof(languageTags));

            return output.ToString();
        }

        /// <summary>
        /// Parses a value made of a name and parameters, with the media type parser, which takes the parameter forms of RFC 2231 and rejects those of RFC 2047.
        /// </summary>
        /// <param name="value">The field body.</param>
        /// <param name="prefix">Text to put before the value once it has no comments, so that what follows is what the media type parser expects.</param>
        /// <param name="wordParameterName">The name of the parameter whose encoded words are decoded, with <see cref="ImfOptions.AllowMalformedParameters"/>.</param>
        /// <param name="allowFinalSemicolon">Whether one semicolon may end the value.</param>
        /// <param name="options">The options.</param>
        /// <param name="result">The value.</param>
        /// <returns><c>false</c> if the value is not valid.</returns>
        /// <summary>
        /// Parses the body of a Content-Location field (RFC 2557 §4.1): a URI between comments and white space, folded as §4.4.2 allows, with any RFC 2047 encoded words of §4.4.1 decoded.
        /// </summary>
        internal static bool TryParseContentLocation(string value, ImfOptions options, [MaybeNullWhen(false)] out Uri result)
        {
            result = null;
            if (!TryRemoveOuterComments(value, out var body)) return false;

            var isEncoded = HasEncodedWord(body, options);
            var text = isEncoded ? ImfHeaderText.DecodeUnstructured(body, options).Trim(WhiteSpace) : RemoveWhiteSpace(body);
            if (text.Length == 0) return false;

            foreach (var c in text)
            {
                if (c < 0x20 || c == 0x7F) return false;
            }

            // ref. RFC 2557 §4.4.1: what a header can't hold has to be in an encoded word.
            if (!isEncoded && !IsWritableUrl(text)) return false;

            // The scheme decides, as Uri takes a path that starts with a slash to be an absolute file path on some operating systems.
            return Uri.TryCreate(text, MhtmlUrl.IsAbsolute(text) ? UriKind.Absolute : UriKind.Relative, out result);
        }

        /// <summary>
        /// Formats the body of a Content-Location field, with a space at each place it can be folded.
        /// </summary>
        /// <exception cref="ArgumentException">The URI holds an unpaired surrogate.</exception>
        internal static string FormatContentLocation(Uri location)
        {
            var text = location.OriginalString;
            if (!IsWritableUrl(text) || text.Contains("=?"))
            {
                // ref. RFC 2557 §4.4.1.
                var encoded = new StringBuilder();
                foreach (var word in MimeEncodedWord.EncodeText(text, MimeEncodedWordContext.Text))
                {
                    if (encoded.Length > 0) encoded.Append(' ');

                    encoded.Append(word);
                }

                return encoded.ToString();
            }

            if (text.Length <= MaxSectionLength) return text;

            // ref. RFC 2017 §3.1, which RFC 2557 §4.4.2 refers to: a reader takes the white space out again.
            var output = new StringBuilder(text.Length + (text.Length / MaxUrlWordLength));
            for (var index = 0; index < text.Length; index += MaxUrlWordLength)
            {
                if (index > 0) output.Append(' ');

                output.Append(text, index, Math.Min(MaxUrlWordLength, text.Length - index));
            }

            return output.ToString();
        }

        internal static bool TryParseParameterized(ReadOnlySpan<byte> value, string prefix, string wordParameterName, bool allowFinalSemicolon, MimeMediaTypeParseOptions parseOptions, ImfOptions options, out MimeMediaType result)
        {
            result = default;
            if (!ImfHeaderText.TryGetUtf8(value, options, out var utf8) || !TryRemoveCfws(utf8, out var text)) return false;

            if (allowFinalSemicolon && text.Length > 0 && text[text.Length - 1] == ';') text = text.Substring(0, text.Length - 1);

            if (!MimeMediaType.TryParse(prefix + text, parseOptions, options.CharsetResolver, out var parsed)) return false;

            result = parsed;
            foreach (var parameter in parsed.Parameters)
            {
                if (parameter.Charset.Length != 0 || !HasEncodedWord(parameter.Value, options.CharsetResolver)) continue;

                // ref. RFC 2047 §5: an encoded word "MUST NOT be used in parameter of a MIME Content-Type or Content-Disposition field".
                if (!options.AllowMalformedParameters) return false;

                if (string.Equals(parameter.Name, wordParameterName, StringComparison.OrdinalIgnoreCase))
                {
                    var decoded = ImfHeaderText.DecodeRun(parameter.Value, MimeEncodedWordContext.Text, ImfHeaderText.MalformedWordOptions, options.CharsetResolver);
                    result = result.WithParameter(parameter.Name, decoded);
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the accommodations of the media type parser that some options ask for.
        /// </summary>
        private static bool HasEncodedWord(string body, ImfOptions options)
        {
            var wordOptions = ImfHeaderText.GetWordOptions(options);
            foreach (var word in body.Split(WhiteSpace, StringSplitOptions.RemoveEmptyEntries))
            {
                if (MimeEncodedWord.TryParse(word, MimeEncodedWordContext.Text, wordOptions, options.CharsetResolver, out _)) return true;
            }

            return false;
        }

        private static string RemoveWhiteSpace(string body)
        {
            return body.Replace(" ", string.Empty).Replace("\t", string.Empty);
        }

        // Only what no URI can hold in a header is refused, as RFC 2557 §4.1 leaves the URI schemes open.
        private static bool IsWritableUrl(string text)
        {
            foreach (var c in text)
            {
                if (c <= 0x20 || c >= 0x7F || c == '"' || c == '<' || c == '>' || c == '\\') return false;
            }

            return true;
        }

        /// <summary>
        /// Takes the comments before and after a URI out of the body of a field.  A parenthesis is a character of a URL, so only one set off by white space starts a comment after it.
        /// </summary>
        private static bool TryRemoveOuterComments(string value, out string body)
        {
            // Each character is looked at once, so that a field of nothing but comments costs no more than its length.
            body = string.Empty;
            var text = value.Trim(WhiteSpace);
            var start = SkipComments(text, 0);
            if (start < 0) return false;

            for (var index = start + 1; index < text.Length; index++)
            {
                if (text[index] != '(' || (text[index - 1] != ' ' && text[index - 1] != '\t')) continue;

                var next = SkipComments(text, index);
                if (next < 0) return false;

                if (next == text.Length)
                {
                    body = text.Substring(start, index - start).TrimEnd(WhiteSpace);
                    return true;
                }

                index = next - 1;
            }

            body = start >= text.Length ? string.Empty : text.Substring(start);
            return true;
        }

        /// <summary>
        /// Skips any comments, and the white space around them, from a position.
        /// </summary>
        /// <returns>The position after them, or -1 if one isn't closed.</returns>
        private static int SkipComments(string text, int start)
        {
            var position = start;
            while (position < text.Length && text[position] == '(')
            {
                var end = FindCommentEnd(text, position);
                if (end < 0) return -1;

                position = end + 1;
                while (position < text.Length && (text[position] == ' ' || text[position] == '\t'))
                {
                    position++;
                }
            }

            return position;
        }

        /// <summary>
        /// Finds the parenthesis that closes a comment, or gives -1 if it isn't closed.
        /// </summary>
        private static int FindCommentEnd(string text, int start)
        {
            var depth = 0;
            for (var index = start; index < text.Length; index++)
            {
                var c = text[index];
                if (c == '\\') index++;
                else if (c == '(') depth++;
                else if (c == ')' && --depth == 0) return index;
            }

            return -1;
        }

        internal static MimeMediaTypeParseOptions GetParseOptions(ImfOptions options)
        {
            // ref. RFC 2045 §5.1: a name in a field is a token, as the narrower names of RFC 6838 §4.2 are only for a registration.
            return MimeMediaTypeParseOptions.AllowRfc2045Tokens | (options.AllowMalformedParameters ? MalformedParameterOptions : MimeMediaTypeParseOptions.None);
        }

        internal static MimeMediaTypeParseOptions GetContentTypeParseOptions(ImfOptions options)
        {
            // ref. RFC 6532 §3.2: a message's header may hold UTF-8 in a quoted-string, which RFC 2045 §5.1 makes a parameter value.
            return GetParseOptions(options) | MimeMediaTypeParseOptions.AllowUtf8QuotedStrings;
        }

        /// <summary>
        /// Writes parameters with a space before each, where a field can be folded, and with any that won't fit on a line split into sections (RFC 2231 §3).
        /// </summary>
        internal static void AppendParameters(IReadOnlyList<MimeParameter> parameters, StringBuilder output)
        {
            foreach (var parameter in parameters)
            {
                foreach (var section in parameter.FormatSections(MaxSectionLength))
                {
                    output.Append("; ").Append(section);
                }
            }
        }

        /// <summary>
        /// Takes the folding white space and comments out from between the tokens of a field body (RFC 5322 §3.2.2), and the folds out of its quoted-strings.  Two tokens
        /// with nothing but those between them keep a space, so that they don't become one.
        /// </summary>
        /// <returns><c>false</c> if a comment or quoted-string isn't closed, or a carriage return is not part of a fold.</returns>
        private static bool TryRemoveCfws(ReadOnlySpan<byte> value, out string text)
        {
            text = string.Empty;

            // A space takes the place of at least one octet, so the text is never longer than the value.
            var output = new byte[value.Length];
            var length = 0;
            var position = 0;
            while (true)
            {
                var start = position;
                if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;

                if (position == value.Length) break;

                var current = value[position];
                if (position > start && length > 0 && !IsDelimiter(output[length - 1]) && !IsDelimiter(current)) output[length++] = (byte)' ';

                if (current == (byte)'"')
                {
                    if (!TryCopyQuotedString(value, ref position, output, ref length)) return false;

                    continue;
                }

                // Anything else that isn't valid is left for the parser of the value to reject.
                do
                {
                    output[length++] = value[position++];
                }
                while (position < value.Length && !IsTokenEnd(value[position]));
            }

            text = Encoding.UTF8.GetString(output, 0, length);
            return true;
        }

        private static bool TryCopyQuotedString(ReadOnlySpan<byte> value, ref int position, byte[] output, ref int length)
        {
            output[length++] = value[position++];
            while (position < value.Length)
            {
                var current = value[position];
                if (current == (byte)'\r')
                {
                    // ref. RFC 5322 §3.2.2: only a CRLF before a space or tab is a fold, and only the CRLF goes.
                    if (position + 2 >= value.Length || value[position + 1] != (byte)'\n' || !IsWhitespace(value[position + 2])) return false;

                    position += 2;
                    continue;
                }

                output[length++] = current;
                position++;
                if (current == (byte)'"') return true;

                if (current == (byte)'\\')
                {
                    if (position == value.Length) return false;

                    output[length++] = value[position++];
                }
            }

            return false;
        }

        private static bool TryReadVersionNumber(ReadOnlySpan<byte> value, ref int position, out int number)
        {
            number = 0;
            if (!ImfTokenizer.TrySkipCfws(value, ref position)) return false;

            var start = position;
            long total = 0;
            while (position < value.Length && value[position] >= (byte)'0' && value[position] <= (byte)'9')
            {
                total = total * 10 + (value[position] - (byte)'0');
                if (total > int.MaxValue) return false;

                position++;
            }

            number = (int)total;
            return position > start && ImfTokenizer.TrySkipCfws(value, ref position);
        }

        // An encoded word has to be a word of its own (RFC 2047 §5), and a text that only looks like one, such as =?x?=, is ordinary text in a parameter.
        private static bool HasEncodedWord(string value, IMimeCharsetResolver charsetResolver)
        {
            if (value.IndexOf("=?", StringComparison.Ordinal) < 0) return false;

            foreach (var word in value.Split(' ', '\t'))
            {
                if (MimeEncodedWord.TryParse(word, MimeEncodedWordContext.Text, ImfHeaderText.MalformedWordOptions, charsetResolver, out _)) return true;
            }

            return false;
        }

        private static bool IsDelimiter(byte value)
        {
            return value == (byte)';' || value == (byte)'=' || value == (byte)'/';
        }

        private static bool IsTokenEnd(byte value)
        {
            return IsDelimiter(value) || IsWhitespace(value) || value == (byte)'\r' || value == (byte)'(' || value == (byte)'"';
        }

        private static bool IsLanguageTagEnd(byte value)
        {
            return value == (byte)',' || IsWhitespace(value) || value == (byte)'\r' || value == (byte)'(';
        }

        private static bool IsWhitespace(byte value)
        {
            return value == (byte)' ' || value == (byte)'\t';
        }
    }
}
