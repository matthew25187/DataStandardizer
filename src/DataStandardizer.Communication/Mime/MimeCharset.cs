using System;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A character set name (RFC 2978 §2.3), as in the <c>charset</c> parameter of a media type (RFC 2046 §4.1.2), such as <c>UTF-8</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A name is kept as written, and names are compared case-insensitively. An alias is a different name, so <c>latin1</c> doesn't equal
    /// <c>ISO-8859-1</c>, although both resolve to the same encoding. The name is not checked against the IANA registry, so an unregistered
    /// name such as <c>x-mac-roman</c> is valid.
    /// </para>
    /// <para>
    /// Use <see cref="MimeCharsetResolver"/> to get the <see cref="System.Text.Encoding"/> for a character set.
    /// </para>
    /// </remarks>
    public readonly struct MimeCharset : IEquatable<MimeCharset>
#if NET7_0_OR_GREATER
        , ISpanFormattable, ISpanParsable<MimeCharset>
#elif NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string InvalidFormatMessage = "The character set name is not in a valid format.";
        private const string UninitializedMessage = nameof(MimeCharset) + " is uninitialized.";

        /// <summary>
        /// <c>US-ASCII</c> (RFC 2046 §4.1.2).
        /// </summary>
        public static readonly MimeCharset UsAscii = new MimeCharset("US-ASCII");

        /// <summary>
        /// <c>ISO-8859-1</c>, also known as Latin-1.
        /// </summary>
        public static readonly MimeCharset Iso88591 = new MimeCharset("ISO-8859-1");

        /// <summary>
        /// <c>UTF-8</c> (RFC 3629).
        /// </summary>
        public static readonly MimeCharset Utf8 = new MimeCharset("UTF-8");

        /// <summary>
        /// <c>UTF-16</c> (RFC 2781 §3.1), which is big-endian unless the text starts with a byte order mark that says otherwise.
        /// </summary>
        public static readonly MimeCharset Utf16 = new MimeCharset("UTF-16");

        /// <summary>
        /// <c>UTF-16BE</c>, which is UTF-16 in big-endian byte order (RFC 2781 §3.2).
        /// </summary>
        public static readonly MimeCharset Utf16BigEndian = new MimeCharset("UTF-16BE");

        /// <summary>
        /// <c>UTF-16LE</c>, which is UTF-16 in little-endian byte order (RFC 2781 §3.2).
        /// </summary>
        public static readonly MimeCharset Utf16LittleEndian = new MimeCharset("UTF-16LE");

        /// <summary>
        /// <c>UTF-32</c>, which is big-endian unless the text starts with a byte order mark that says otherwise (The Unicode Standard §3.10,
        /// D101).
        /// </summary>
        public static readonly MimeCharset Utf32 = new MimeCharset("UTF-32");

        /// <summary>
        /// <c>UTF-32BE</c>, which is UTF-32 in big-endian byte order.
        /// </summary>
        public static readonly MimeCharset Utf32BigEndian = new MimeCharset("UTF-32BE");

        /// <summary>
        /// <c>UTF-32LE</c>, which is UTF-32 in little-endian byte order.
        /// </summary>
        public static readonly MimeCharset Utf32LittleEndian = new MimeCharset("UTF-32LE");

        private readonly string _name;

        private MimeCharset(string name)
        {
            _name = name;
        }

        /// <summary>
        /// Gets the name, as written.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeCharset"/> is not initialised.</exception>
        public string Name => _name ?? throw new InvalidOperationException(UninitializedMessage);

        internal bool IsInitialized => _name != null;

        /// <summary>
        /// Gets the character set that text of a media type is in when the media type has no <c>charset</c> parameter.
        /// </summary>
        /// <param name="mediaType">The media type. Its parameters are ignored.</param>
        /// <param name="charset">The default character set, if the media type has one.</param>
        /// <returns><c>true</c> if the media type has a default character set; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// <para>
        /// A <c>text</c> media type defaults to <c>US-ASCII</c> (RFC 2046 §4.1.2), unless its registration says otherwise (RFC 6657 §3):
        /// </para>
        /// <list type="bullet">
        /// <item><c>text/calendar</c> and <c>text/vcard</c> default to <c>UTF-8</c> (RFC 5545 §8.1, RFC 6350 §10.1).</item>
        /// <item><c>text/html</c> and <c>text/xml</c> have no default, as the character set is given within the text (HTML Living Standard
        /// §16.1.2, RFC 7303 §3.2).</item>
        /// <item><c>text/markdown</c> has no default, as its <c>charset</c> parameter is required (RFC 7763 §2).</item>
        /// </list>
        /// <para>
        /// Other media types have no default.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="mediaType"/> is not initialised.</exception>
        public static bool TryGetDefault(MimeMediaType mediaType, out MimeCharset charset)
        {
            if (!mediaType.IsInitialized) throw new ArgumentException("The media type is not initialised.", nameof(mediaType));

            charset = default(MimeCharset);
            if (!MimeGrammar.EqualsIgnoreCase(mediaType.Type.AsSpan(), MimeTopLevelTypes.Text)) return false;

            var subtype = mediaType.Subtype.AsSpan();
            if (MimeGrammar.EqualsIgnoreCase(subtype, "calendar") || MimeGrammar.EqualsIgnoreCase(subtype, "vcard"))
            {
                charset = Utf8;
                return true;
            }

            if (MimeGrammar.EqualsIgnoreCase(subtype, "html") || MimeGrammar.EqualsIgnoreCase(subtype, "xml") ||
                MimeGrammar.EqualsIgnoreCase(subtype, "markdown"))
            {
                return false;
            }

            charset = UsAscii;
            return true;
        }

        /// <summary>
        /// Parses a character set name.
        /// </summary>
        /// <param name="value">The name, such as <c>UTF-8</c>, with no quotes or whitespace.</param>
        /// <param name="result">The character set, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid name; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, out MimeCharset result)
        {
            if (!MimeGrammar.IsCharsetName(value))
            {
                result = default(MimeCharset);
                return false;
            }

            result = new MimeCharset(value.ToString());
            return true;
        }

        /// <summary>
        /// Parses a character set name.
        /// </summary>
        /// <param name="value">The name, such as <c>UTF-8</c>, with no quotes or whitespace.</param>
        /// <param name="result">The character set, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid name; otherwise <c>false</c>, including for <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, out MimeCharset result)
#else
        public static bool TryParse(string value, out MimeCharset result)
#endif
        {
            if (value is null)
            {
                result = default(MimeCharset);
                return false;
            }

            return TryParse(value.AsSpan(), out result);
        }

        /// <summary>
        /// Parses a character set name.
        /// </summary>
        /// <param name="value">The name, such as <c>UTF-8</c>, with no quotes or whitespace.</param>
        /// <returns>The character set.</returns>
        /// <exception cref="FormatException">The value is not a valid name.</exception>
        public static MimeCharset Parse(ReadOnlySpan<char> value)
        {
            MimeCharset result;
            if (!TryParse(value, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a character set name.
        /// </summary>
        /// <param name="value">The name, such as <c>UTF-8</c>, with no quotes or whitespace.</param>
        /// <returns>The character set.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid name.</exception>
        public static MimeCharset Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Parse(value.AsSpan());
        }

#if NET7_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static MimeCharset IParsable<MimeCharset>.Parse(string s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static bool IParsable<MimeCharset>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MimeCharset result)
        {
            return TryParse(s, out result);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static MimeCharset ISpanParsable<MimeCharset>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static bool ISpanParsable<MimeCharset>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MimeCharset result)
        {
            return TryParse(s, out result);
        }
#endif

        /// <summary>
        /// Formats the character set name into a span, as written.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the name was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <remarks>
        /// Nothing is written if the <see cref="MimeCharset"/> is not initialised.
        /// </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            var position = 0;
            if (_name != null && !MimeGrammar.TryWrite(_name, destination, ref position))
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = position;
            return true;
        }

        /// <summary>
        /// Formats the character set name, as written.
        /// </summary>
        /// <returns>The name; an empty string if the <see cref="MimeCharset"/> is not initialised.</returns>
        public override string ToString()
        {
            return _name ?? string.Empty;
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a character set name has only one form.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten);
        }

        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a character set name has only one form.</remarks>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }
#endif

        /// <summary>
        /// Indicates whether this character set equals another, comparing the names case-insensitively.
        /// </summary>
        public bool Equals(MimeCharset other)
        {
            return string.Equals(_name, other._name, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is MimeCharset other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
        }

        /// <summary>
        /// Indicates whether two character sets are equal.
        /// </summary>
        public static bool operator ==(MimeCharset left, MimeCharset right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two character sets differ.
        /// </summary>
        public static bool operator !=(MimeCharset left, MimeCharset right)
        {
            return !left.Equals(right);
        }
    }
}
