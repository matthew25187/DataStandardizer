using System;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A transfer encoding mechanism, as in a Content-Transfer-Encoding header field (RFC 2045 §6.1), such as <c>base64</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The known mechanisms are <c>7bit</c>, <c>8bit</c>, <c>binary</c>, <c>quoted-printable</c> and <c>base64</c>, which are written in
    /// lower case. Any other token is an extension mechanism: an <c>x-</c> token, or one registered with IANA (RFC 4289 §3). An extension
    /// mechanism is kept as written.
    /// </para>
    /// <para>
    /// Mechanisms are compared case-insensitively.
    /// </para>
    /// </remarks>
    public readonly struct MimeTransferEncoding : IEquatable<MimeTransferEncoding>
#if NET7_0_OR_GREATER
        , ISpanFormattable, ISpanParsable<MimeTransferEncoding>
#elif NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string InvalidFormatMessage = "The transfer encoding is not in a valid format.";
        private const string UninitializedMessage = nameof(MimeTransferEncoding) + " is uninitialized.";

        /// <summary>
        /// <c>7bit</c>, which labels 7bit data that is not encoded (RFC 2045 §6.2).
        /// </summary>
        public static readonly MimeTransferEncoding SevenBit = new MimeTransferEncoding("7bit", Mechanism.SevenBit);

        /// <summary>
        /// <c>8bit</c>, which labels 8bit data that is not encoded (RFC 2045 §6.2).
        /// </summary>
        public static readonly MimeTransferEncoding EightBit = new MimeTransferEncoding("8bit", Mechanism.EightBit);

        /// <summary>
        /// <c>binary</c>, which labels binary data that is not encoded (RFC 2045 §6.2).
        /// </summary>
        public static readonly MimeTransferEncoding Binary = new MimeTransferEncoding("binary", Mechanism.Binary);

        /// <summary>
        /// <c>quoted-printable</c>, which encodes mostly-text data as 7bit data (RFC 2045 §6.7).
        /// </summary>
        public static readonly MimeTransferEncoding QuotedPrintable = new MimeTransferEncoding("quoted-printable", Mechanism.QuotedPrintable);

        /// <summary>
        /// <c>base64</c>, which encodes any data as 7bit data (RFC 2045 §6.8).
        /// </summary>
        public static readonly MimeTransferEncoding Base64 = new MimeTransferEncoding("base64", Mechanism.Base64);

        private readonly string _name;
        private readonly Mechanism _mechanism;

        private MimeTransferEncoding(string name, Mechanism mechanism)
        {
            _name = name;
            _mechanism = mechanism;
        }

        private enum Mechanism : byte
        {
            Extension = 0,
            SevenBit,
            EightBit,
            Binary,
            QuotedPrintable,
            Base64,
        }

        /// <summary>
        /// Gets the name of the mechanism, such as <c>base64</c>.
        /// </summary>
        /// <returns>The name in lower case for a known mechanism; the name as written for an extension mechanism.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeTransferEncoding"/> is not initialised.</exception>
        public string Name => _name ?? throw new InvalidOperationException(UninitializedMessage);

        /// <summary>
        /// Gets whether the mechanism is one defined by RFC 2045, rather than an extension mechanism.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeTransferEncoding"/> is not initialised.</exception>
        public bool IsKnown
        {
            get
            {
                CheckInitialized();

                return _mechanism != Mechanism.Extension;
            }
        }

        /// <summary>
        /// Gets whether the mechanism leaves the data as it is, and only labels its domain: <c>7bit</c>, <c>8bit</c> or <c>binary</c>
        /// (RFC 2045 §6.2).
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeTransferEncoding"/> is not initialised.</exception>
        public bool IsIdentity
        {
            get
            {
                CheckInitialized();

                return _mechanism == Mechanism.SevenBit || _mechanism == Mechanism.EightBit || _mechanism == Mechanism.Binary;
            }
        }

        /// <summary>
        /// Gets the domain of the encoded data, which the transport must be able to carry.
        /// </summary>
        /// <returns>The domain that an identity mechanism labels; <see cref="MimeDataDomain.SevenBit"/> for <c>quoted-printable</c> and
        /// <c>base64</c>; <see cref="MimeDataDomain.Unknown"/> for an extension mechanism.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeTransferEncoding"/> is not initialised.</exception>
        public MimeDataDomain Domain
        {
            get
            {
                CheckInitialized();

                switch (_mechanism)
                {
                    case Mechanism.SevenBit:
                    case Mechanism.QuotedPrintable:
                    case Mechanism.Base64:
                        return MimeDataDomain.SevenBit;
                    case Mechanism.EightBit:
                        return MimeDataDomain.EightBit;
                    case Mechanism.Binary:
                        return MimeDataDomain.Binary;
                    default:
                        return MimeDataDomain.Unknown;
                }
            }
        }

        /// <summary>
        /// Parses a transfer encoding.
        /// </summary>
        /// <param name="value">The mechanism, such as <c>base64</c>, with no comments or whitespace.</param>
        /// <param name="result">The transfer encoding, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mechanism; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, out MimeTransferEncoding result)
        {
            // ref. RFC 2045 §6.1: mechanism := "7bit" / "8bit" / "binary" / "quoted-printable" / "base64" / ietf-token / x-token
            if (!MimeGrammar.IsToken(value))
            {
                result = default(MimeTransferEncoding);
                return false;
            }

            if (MimeGrammar.EqualsIgnoreCase(value, SevenBit._name)) result = SevenBit;
            else if (MimeGrammar.EqualsIgnoreCase(value, EightBit._name)) result = EightBit;
            else if (MimeGrammar.EqualsIgnoreCase(value, Binary._name)) result = Binary;
            else if (MimeGrammar.EqualsIgnoreCase(value, QuotedPrintable._name)) result = QuotedPrintable;
            else if (MimeGrammar.EqualsIgnoreCase(value, Base64._name)) result = Base64;
            else result = new MimeTransferEncoding(value.ToString(), Mechanism.Extension);

            return true;
        }

        /// <summary>
        /// Parses a transfer encoding.
        /// </summary>
        /// <param name="value">The mechanism, such as <c>base64</c>, with no comments or whitespace.</param>
        /// <param name="result">The transfer encoding, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid mechanism; otherwise <c>false</c>, including for <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, out MimeTransferEncoding result)
#else
        public static bool TryParse(string value, out MimeTransferEncoding result)
#endif
        {
            if (value is null)
            {
                result = default(MimeTransferEncoding);
                return false;
            }

            return TryParse(value.AsSpan(), out result);
        }

        /// <summary>
        /// Parses a transfer encoding.
        /// </summary>
        /// <param name="value">The mechanism, such as <c>base64</c>, with no comments or whitespace.</param>
        /// <returns>The transfer encoding.</returns>
        /// <exception cref="FormatException">The value is not a valid mechanism.</exception>
        public static MimeTransferEncoding Parse(ReadOnlySpan<char> value)
        {
            MimeTransferEncoding result;
            if (!TryParse(value, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a transfer encoding.
        /// </summary>
        /// <param name="value">The mechanism, such as <c>base64</c>, with no comments or whitespace.</param>
        /// <returns>The transfer encoding.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid mechanism.</exception>
        public static MimeTransferEncoding Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            return Parse(value.AsSpan());
        }

#if NET7_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static MimeTransferEncoding IParsable<MimeTransferEncoding>.Parse(string s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static bool IParsable<MimeTransferEncoding>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MimeTransferEncoding result)
        {
            return TryParse(s, out result);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static MimeTransferEncoding ISpanParsable<MimeTransferEncoding>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static bool ISpanParsable<MimeTransferEncoding>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MimeTransferEncoding result)
        {
            return TryParse(s, out result);
        }
#endif

        /// <summary>
        /// Formats the transfer encoding into a span, such as <c>base64</c>.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the transfer encoding was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <remarks>
        /// Nothing is written if the <see cref="MimeTransferEncoding"/> is not initialised.
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
        /// Formats the transfer encoding, such as <c>base64</c>.
        /// </summary>
        /// <returns>The name of the mechanism; an empty string if the <see cref="MimeTransferEncoding"/> is not initialised.</returns>
        public override string ToString()
        {
            return _name ?? string.Empty;
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a transfer encoding has only one form.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten);
        }

        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a transfer encoding has only one form.</remarks>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }
#endif

        /// <summary>
        /// Indicates whether this transfer encoding equals another, comparing the mechanisms case-insensitively.
        /// </summary>
        public bool Equals(MimeTransferEncoding other)
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
            return obj is MimeTransferEncoding other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(_name);
        }

        /// <summary>
        /// Indicates whether two transfer encodings are equal.
        /// </summary>
        public static bool operator ==(MimeTransferEncoding left, MimeTransferEncoding right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two transfer encodings differ.
        /// </summary>
        public static bool operator !=(MimeTransferEncoding left, MimeTransferEncoding right)
        {
            return !left.Equals(right);
        }

        private void CheckInitialized()
        {
            if (_name is null) throw new InvalidOperationException(UninitializedMessage);
        }
    }
}
