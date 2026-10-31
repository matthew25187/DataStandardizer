using System;
using System.Buffers;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// A <c>data:</c> URI (RFC 2397), which holds a media type and its data inline, such as
    /// <c>data:image/png;base64,iVBORw0KGgo=</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is base64 when the media type is followed by <c>;base64</c>, and otherwise percent-encoded. Parsing checks the data but
    /// keeps it encoded, and it is decoded only by <see cref="GetData"/> and <see cref="TryCopyData"/>, so a large value costs no more than
    /// its text until its data is needed.
    /// </para>
    /// <para>
    /// A parsed URI is formatted exactly as it was written. Two URIs are equal if they have the same media type and the same decoded data,
    /// however each is written.
    /// </para>
    /// </remarks>
    public readonly struct MimeDataUri : IEquatable<MimeDataUri>
#if NET7_0_OR_GREATER
        , ISpanFormattable, ISpanParsable<MimeDataUri>
#elif NET6_0_OR_GREATER
        , ISpanFormattable
#endif
    {
        private const string Scheme = "data:";
        private const string Base64Extension = ";base64";
        private const string TextPlain = "text/plain";
        private const string InvalidFormatMessage = "The data URI is not in a valid format.";
        private const string UninitializedMessage = nameof(MimeDataUri) + " is uninitialized.";

        // ref. RFC 2397 §2: if <mediatype> is omitted, it defaults to text/plain;charset=US-ASCII.
        private static readonly MimeMediaType DefaultMediaType = MimeMediaType.TextPlain.WithParameter("charset", "US-ASCII");

        private readonly string _value;
        private readonly int _dataStart;
        private readonly int _dataLength;
        private readonly MimeMediaType _mediaType;
        private readonly bool _isBase64;

        private MimeDataUri(string value, int dataStart, int dataLength, MimeMediaType mediaType, bool isBase64)
        {
            _value = value;
            _dataStart = dataStart;
            _dataLength = dataLength;
            _mediaType = mediaType;
            _isBase64 = isBase64;
        }

        /// <summary>
        /// Gets the media type of the data.
        /// </summary>
        /// <returns>The media type; <c>text/plain;charset=US-ASCII</c> if the URI has none (RFC 2397 §2), or <c>text/plain</c> with the
        /// parameters given if the URI has parameters but no type, such as <c>data:;charset=UTF-8,</c>.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeDataUri"/> is not initialised.</exception>
        public MimeMediaType MediaType
        {
            get
            {
                if (_value is null) throw new InvalidOperationException(UninitializedMessage);

                return _mediaType.IsInitialized ? _mediaType : DefaultMediaType;
            }
        }

        /// <summary>
        /// Gets whether the data is base64, rather than percent-encoded.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeDataUri"/> is not initialised.</exception>
        public bool IsBase64 => _value is null ? throw new InvalidOperationException(UninitializedMessage) : _isBase64;

        /// <summary>
        /// Gets the number of bytes of decoded data.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="MimeDataUri"/> is not initialised.</exception>
        public int DataLength => _value is null ? throw new InvalidOperationException(UninitializedMessage) : _dataLength;

        private ReadOnlySpan<char> EncodedData => _value.AsSpan(_dataStart);

        /// <summary>
        /// Creates a data URI with base64 data.
        /// </summary>
        /// <param name="mediaType">The media type of the data.</param>
        /// <param name="data">The data.</param>
        /// <returns>The data URI, such as <c>data:image/png;base64,iVBORw0KGgo=</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="mediaType"/> is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The URI would be longer than <see cref="int.MaxValue"/>.</exception>
        public static MimeDataUri Create(MimeMediaType mediaType, ReadOnlySpan<byte> data)
        {
            return Create(mediaType, data, true);
        }

        /// <summary>
        /// Creates a data URI.
        /// </summary>
        /// <param name="mediaType">The media type of the data. Any character of it that can't appear in a URI, such as the quotes around a
        /// parameter value, is percent-encoded.</param>
        /// <param name="data">The data.</param>
        /// <param name="base64"><c>true</c> to write the data in base64; <c>false</c> to percent-encode it, which suits text.</param>
        /// <returns>The data URI, such as <c>data:text/plain;charset=UTF-8,A%20brief%20note</c>.</returns>
        /// <exception cref="ArgumentException"><paramref name="mediaType"/> is not initialised.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The URI would be longer than <see cref="int.MaxValue"/>.</exception>
        public static MimeDataUri Create(MimeMediaType mediaType, ReadOnlySpan<byte> data, bool base64)
        {
            if (!mediaType.IsInitialized) throw new ArgumentException("The media type is not initialised.", nameof(mediaType));

            var header = mediaType.ToString();
            var headerLength = 0;
            foreach (var c in header)
            {
                headerLength += IsHeaderChar(c) ? 1 : 3;
            }

            var dataStart = Scheme.Length + headerLength + (base64 ? Base64Extension.Length : 0) + 1;
            long encodedLength = base64 ? MimeBase64.GetEncodedLength(data.Length, MimeBase64Mode.Strict) : GetPercentEncodedLength(data);
            if (dataStart + encodedLength > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(data));

            var buffer = new char[dataStart + encodedLength];
            var position = 0;
            MimeGrammar.TryWrite(Scheme, buffer, ref position);
            foreach (var c in header)
            {
                if (IsHeaderChar(c))
                {
                    buffer[position++] = c;
                }
                else
                {
                    WritePercentEncoded((byte)c, buffer, ref position);
                }
            }

            if (base64) MimeGrammar.TryWrite(Base64Extension, buffer, ref position);
            buffer[position++] = ',';

            if (base64)
            {
                int charsWritten;
                MimeBase64.TryEncode(data, buffer.AsSpan(position), out charsWritten, MimeBase64Mode.Strict);
            }
            else
            {
                foreach (var b in data)
                {
                    if (b < 0x80 && IsDataChar((char)b))
                    {
                        buffer[position++] = (char)b;
                    }
                    else
                    {
                        WritePercentEncoded(b, buffer, ref position);
                    }
                }
            }

            return new MimeDataUri(new string(buffer), dataStart, data.Length, mediaType, base64);
        }

        /// <summary>
        /// Gets the decoded data.
        /// </summary>
        /// <returns>The data.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeDataUri"/> is not initialised.</exception>
        public byte[] GetData()
        {
            var data = new byte[DataLength];
            DecodeData(data);
            return data;
        }

        /// <summary>
        /// Decodes the data into a span.
        /// </summary>
        /// <param name="destination">The span to write to. <see cref="DataLength"/> gives the length it needs.</param>
        /// <param name="bytesWritten">The number of bytes written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the data was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeDataUri"/> is not initialised.</exception>
        public bool TryCopyData(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < DataLength)
            {
                bytesWritten = 0;
                return false;
            }

            DecodeData(destination);
            bytesWritten = _dataLength;
            return true;
        }

        /// <summary>
        /// Gets the data URI as a <see cref="Uri"/>.
        /// </summary>
        /// <returns>The URI.</returns>
        /// <exception cref="InvalidOperationException">The <see cref="MimeDataUri"/> is not initialised.</exception>
        /// <exception cref="UriFormatException">The URI is longer than <see cref="Uri"/> allows on this platform.</exception>
        public Uri ToUri()
        {
            if (_value is null) throw new InvalidOperationException(UninitializedMessage);

            return new Uri(_value, UriKind.Absolute);
        }

        /// <summary>
        /// Parses a data URI.
        /// </summary>
        /// <param name="value">The data URI, such as <c>data:,A%20brief%20note</c>, with no fragment or whitespace.</param>
        /// <param name="result">The data URI, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid data URI; otherwise <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<char> value, out MimeDataUri result)
        {
            if (!TryParseCore(value, out var dataStart, out var dataLength, out var mediaType, out var isBase64))
            {
                result = default(MimeDataUri);
                return false;
            }

            result = new MimeDataUri(value.ToString(), dataStart, dataLength, mediaType, isBase64);
            return true;
        }

        /// <summary>
        /// Parses a data URI.
        /// </summary>
        /// <param name="value">The data URI, such as <c>data:,A%20brief%20note</c>, with no fragment or whitespace.</param>
        /// <param name="result">The data URI, if the value is valid.</param>
        /// <returns><c>true</c> if the value is a valid data URI; otherwise <c>false</c>, including for <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse([NotNullWhen(true)] string? value, out MimeDataUri result)
#else
        public static bool TryParse(string value, out MimeDataUri result)
#endif
        {
            if (value is null || !TryParseCore(value.AsSpan(), out var dataStart, out var dataLength, out var mediaType, out var isBase64))
            {
                result = default(MimeDataUri);
                return false;
            }

            result = new MimeDataUri(value, dataStart, dataLength, mediaType, isBase64);
            return true;
        }

        /// <summary>
        /// Parses a data URI.
        /// </summary>
        /// <param name="value">The data URI, such as <c>data:,A%20brief%20note</c>, with no fragment or whitespace.</param>
        /// <returns>The data URI.</returns>
        /// <exception cref="FormatException">The value is not a valid data URI.</exception>
        public static MimeDataUri Parse(ReadOnlySpan<char> value)
        {
            MimeDataUri result;
            if (!TryParse(value, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Parses a data URI.
        /// </summary>
        /// <param name="value">The data URI, such as <c>data:,A%20brief%20note</c>, with no fragment or whitespace.</param>
        /// <returns>The data URI.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">The value is not a valid data URI.</exception>
        public static MimeDataUri Parse(string value)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            MimeDataUri result;
            if (!TryParse(value, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

#if NET7_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static MimeDataUri IParsable<MimeDataUri>.Parse(string s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static bool IParsable<MimeDataUri>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out MimeDataUri result)
        {
            return TryParse(s, out result);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static MimeDataUri ISpanParsable<MimeDataUri>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s);
        }

        /// <inheritdoc />
        /// <remarks>The provider is ignored.</remarks>
        static bool ISpanParsable<MimeDataUri>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out MimeDataUri result)
        {
            return TryParse(s, out result);
        }
#endif

        /// <summary>
        /// Formats the data URI into a span.
        /// </summary>
        /// <param name="destination">The span to write to.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <returns><c>true</c> if the data URI was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <remarks>
        /// A parsed URI is written as it was parsed. Nothing is written if the <see cref="MimeDataUri"/> is not initialised.
        /// </remarks>
        public bool TryFormat(Span<char> destination, out int charsWritten)
        {
            if (_value is null)
            {
                charsWritten = 0;
                return true;
            }

            if (!_value.AsSpan().TryCopyTo(destination))
            {
                charsWritten = 0;
                return false;
            }

            charsWritten = _value.Length;
            return true;
        }

        /// <summary>
        /// Formats the data URI.
        /// </summary>
        /// <returns>The data URI, as it was parsed; an empty string if the <see cref="MimeDataUri"/> is not initialised.</returns>
        public override string ToString()
        {
            return _value ?? string.Empty;
        }

#if NET6_0_OR_GREATER
        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a data URI has only one form.</remarks>
        bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            return TryFormat(destination, out charsWritten);
        }

        /// <inheritdoc />
        /// <remarks>The format and provider are ignored, as a data URI has only one form.</remarks>
        string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }
#endif

        /// <summary>
        /// Indicates whether this data URI equals another.
        /// </summary>
        /// <remarks>
        /// Data URIs are equal if they have equal media types and the same decoded data, whether each is written in base64 or
        /// percent-encoded. A URI with no media type equals one with <c>text/plain;charset=US-ASCII</c>.
        /// </remarks>
        public bool Equals(MimeDataUri other)
        {
            if (_value is null || other._value is null) return _value is null && other._value is null;
            if (_dataLength != other._dataLength || MediaType != other.MediaType) return false;
            if (_isBase64 == other._isBase64 && EncodedData.SequenceEqual(other.EncodedData)) return true;

            return GetData().AsSpan().SequenceEqual(other.GetData());
        }

        /// <inheritdoc />
#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return obj is MimeDataUri other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            // Must agree with Equals, which compares the decoded data; its length stands in for it, so as not to decode it.
            if (_value is null) return 0;

            unchecked
            {
                return MediaType.GetHashCode() * 31 + _dataLength;
            }
        }

        /// <summary>
        /// Indicates whether two data URIs are equal.
        /// </summary>
        public static bool operator ==(MimeDataUri left, MimeDataUri right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Indicates whether two data URIs differ.
        /// </summary>
        public static bool operator !=(MimeDataUri left, MimeDataUri right)
        {
            return !left.Equals(right);
        }

        private void DecodeData(Span<byte> destination)
        {
            var data = EncodedData;
            if (_isBase64)
            {
                var decoder = new MimeBase64DecoderState(MimeBase64Mode.Strict, MimeBase64DecoderOptions.None);
                int charsConsumed;
                int bytesWritten;
                decoder.Decode(data, destination, out charsConsumed, out bytesWritten, true);
                return;
            }

            var position = 0;
            for (var index = 0; index < data.Length; index++)
            {
                var c = data[index];
                if (c == '%')
                {
                    destination[position++] = (byte)((HexValue(data[index + 1]) << 4) | HexValue(data[index + 2]));
                    index += 2;
                }
                else
                {
                    destination[position++] = (byte)c;
                }
            }
        }

        private static bool TryParseCore(ReadOnlySpan<char> value, out int dataStart, out int dataLength, out MimeMediaType mediaType, out bool isBase64)
        {
            // ref. RFC 2397 §3: dataurl := "data:" [ mediatype ] [ ";base64" ] "," data
            //                   mediatype := [ type "/" subtype ] *( ";" parameter )
            dataStart = 0;
            dataLength = 0;
            mediaType = default(MimeMediaType);
            isBase64 = false;

            if (value.Length < Scheme.Length || !MimeGrammar.EqualsIgnoreCase(value.Slice(0, Scheme.Length), Scheme)) return false;

            var comma = value.Slice(Scheme.Length).IndexOf(',');
            if (comma < 0) return false;

            var header = value.Slice(Scheme.Length, comma);
            dataStart = Scheme.Length + comma + 1;
            if (header.Length >= Base64Extension.Length &&
                MimeGrammar.EqualsIgnoreCase(header.Slice(header.Length - Base64Extension.Length), Base64Extension))
            {
                isBase64 = true;
                header = header.Slice(0, header.Length - Base64Extension.Length);
            }

            if (!TryParseMediaType(header, out mediaType)) return false;

            var data = value.Slice(dataStart);
            return isBase64 ? TryGetBase64Length(data, out dataLength) : TryGetPercentDecodedLength(data, out dataLength);
        }

        private static bool TryParseMediaType(ReadOnlySpan<char> header, out MimeMediaType mediaType)
        {
            mediaType = default(MimeMediaType);
            if (header.IsEmpty) return true;

            // The media type is percent-decoded first, as its parameter values may hold characters that can't appear in a URI.
            var escapeCount = 0;
            for (var index = 0; index < header.Length; index++)
            {
                var c = header[index];
                if (c == '%')
                {
                    if (!IsPercentEncoded(header, index)) return false;

                    escapeCount++;
                    index += 2;
                }
                else if (!IsDataChar(c))
                {
                    return false;
                }
            }

            // ref. RFC 2397 §2: "text/plain" can be omitted but the charset parameter supplied.
            var prefixLength = header[0] == ';' ? TextPlain.Length : 0;
            if (escapeCount == 0 && prefixLength == 0) return MimeMediaType.TryParse(header, out mediaType);

            var decoded = new char[prefixLength + header.Length - escapeCount * 2];
            var position = 0;
            if (prefixLength != 0) MimeGrammar.TryWrite(TextPlain, decoded, ref position);
            for (var index = 0; index < header.Length; index++)
            {
                var c = header[index];
                if (c == '%')
                {
                    c = (char)((HexValue(header[index + 1]) << 4) | HexValue(header[index + 2]));

                    // A media type is US-ASCII.
                    if (c >= '\x80') return false;

                    index += 2;
                }

                decoded[position++] = c;
            }

            return MimeMediaType.TryParse(decoded.AsSpan(), out mediaType);
        }

        private static bool TryGetBase64Length(ReadOnlySpan<char> data, out int length)
        {
            // Checks the data as the strict decoder does, without decoding it: whole quanta of the alphabet, with at most two "=" padding
            // the last (ref. RFC 4648 §4).
            length = 0;
            if (data.Length % 4 != 0) return false;

            var padding = 0;
            if (data.Length > 0 && data[data.Length - 1] == '=') padding = data.Length > 1 && data[data.Length - 2] == '=' ? 2 : 1;

            foreach (var c in data.Slice(0, data.Length - padding))
            {
                if (c >= MimeBase64.DecodeMap.Length || MimeBase64.DecodeMap[c] < 0) return false;
            }

            length = data.Length / 4 * 3 - padding;
            return true;
        }

        private static bool TryGetPercentDecodedLength(ReadOnlySpan<char> data, out int length)
        {
            length = 0;
            for (var index = 0; index < data.Length; index++)
            {
                var c = data[index];
                if (c == '%')
                {
                    if (!IsPercentEncoded(data, index)) return false;

                    index += 2;
                }
                else if (!IsDataChar(c))
                {
                    return false;
                }

                length++;
            }

            return true;
        }

        private static long GetPercentEncodedLength(ReadOnlySpan<byte> data)
        {
            long length = 0;
            foreach (var b in data)
            {
                length += b < 0x80 && IsDataChar((char)b) ? 1 : 3;
            }

            return length;
        }

        private static void WritePercentEncoded(byte value, Span<char> destination, ref int position)
        {
            destination[position++] = '%';
            destination[position++] = MimeQuotedPrintable.HexDigits[value >> 4];
            destination[position++] = MimeQuotedPrintable.HexDigits[value & 0xF];
        }

        private static bool IsPercentEncoded(ReadOnlySpan<char> value, int index)
        {
            // ref. RFC 3986 §2.1: pct-encoded = "%" HEXDIG HEXDIG
            return index + 2 < value.Length && HexValue(value[index + 1]) >= 0 && HexValue(value[index + 2]) >= 0;
        }

        private static int HexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;

            return -1;
        }

        /// <summary>
        /// Determine if a character may appear unencoded in the data of a URI.
        /// </summary>
        /// <remarks>
        /// The characters of an RFC 3986 §3.4 query, other than <c>%</c>: unreserved / sub-delims / ":" / "@" / "/" / "?".
        /// </remarks>
        private static bool IsDataChar(char c)
        {
            if (MimeGrammar.IsAsciiLetterOrDigit(c)) return true;

            switch (c)
            {
                case '-': case '.': case '_': case '~':
                case '!': case '$': case '&': case '\'': case '(': case ')':
                case '*': case '+': case ',': case ';': case '=':
                case ':': case '@': case '/': case '?':
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determine if a character of a media type may be written unencoded in the URI. A comma would end the media type.
        /// </summary>
        private static bool IsHeaderChar(char c)
        {
            return c != ',' && IsDataChar(c);
        }
    }
}
