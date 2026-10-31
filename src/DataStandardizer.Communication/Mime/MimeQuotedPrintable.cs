using System;
using System.Buffers;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Encodes and decodes whole values in quoted-printable: the <c>quoted-printable</c> Content-Transfer-Encoding of RFC 2045 §6.7, or
    /// the RFC 2047 §4.2 "Q" encoding of encoded words.
    /// </summary>
    /// <remarks>
    /// To encode or decode data that arrives in parts, use <see cref="MimeQuotedPrintableEncoder"/> or
    /// <see cref="MimeQuotedPrintableDecoder"/>.
    /// </remarks>
    public static class MimeQuotedPrintable
    {
        // ref. RFC 2045 §6.7 rule 5: the encoded lines must not be longer than 76 characters, not counting the CRLF.
        internal const int MaxLineLength = 76;

        internal const string HexDigits = "0123456789ABCDEF";

        private const string InvalidFormatMessage = "The value is not valid quoted-printable.";

        private const MimeQuotedPrintableDecoderOptions AllDecoderOptions = MimeQuotedPrintableDecoderOptions.AllowLowercaseHexDigits
            | MimeQuotedPrintableDecoderOptions.AllowBareLineFeeds
            | MimeQuotedPrintableDecoderOptions.AllowLiteralEqualsSigns;

        /// <summary>
        /// Gets the largest number of characters that some data can encode to.
        /// </summary>
        /// <param name="length">The number of bytes to encode.</param>
        /// <param name="mode">The form of quoted-printable.</param>
        /// <returns>The largest number of characters in the encoding, including any line breaks, whatever the data and options.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative, or its encoding could be longer than
        /// <see cref="int.MaxValue"/>; or <paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/> member.</exception>
        public static int GetMaxEncodedLength(int length, MimeQuotedPrintableMode mode)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            CheckMode(mode);

            // Each byte is at most 3 characters. A line before a soft break is at least 73 characters of whole tokens, so at least 25
            // bytes, and each soft break adds 3 characters.
            var encodedLength = 3L * length;
            if (mode == MimeQuotedPrintableMode.Body)
            {
                encodedLength += 3L * (length / 25);
            }

            if (encodedLength > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(length));

            return (int)encodedLength;
        }

        /// <summary>
        /// Gets the largest number of bytes that some quoted-printable data can decode to.
        /// </summary>
        /// <param name="length">The number of characters of encoded data.</param>
        /// <returns>The largest number of bytes the data can decode to, whatever its mode and options.</returns>
        /// <remarks>
        /// A <see cref="MimeQuotedPrintableDecoder"/> that holds data from an earlier call can write up to 77 more bytes in one call.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static int GetMaxDecodedLength(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

            return length;
        }

        /// <summary>
        /// Encodes data in quoted-printable.
        /// </summary>
        /// <param name="source">The data.</param>
        /// <param name="destination">The span to write to. <see cref="GetMaxEncodedLength"/> gives a length that is always enough.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="mode">The form of quoted-printable.</param>
        /// <param name="options">How to treat the data.</param>
        /// <returns><c>true</c> if the data was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/> member,
        /// or <paramref name="options"/> is not a combination of <see cref="MimeQuotedPrintableEncoderOptions"/> flags.</exception>
        public static bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)
        {
            CheckMode(mode);
            CheckOptions(options);

            var encoder = new MimeQuotedPrintableEncoderState(mode, options);
            int bytesConsumed;
            if (encoder.Encode(source, destination, out bytesConsumed, out charsWritten, true) == OperationStatus.Done) return true;

            charsWritten = 0;
            return false;
        }

        /// <summary>
        /// Encodes data in quoted-printable.
        /// </summary>
        /// <param name="source">The data.</param>
        /// <param name="mode">The form of quoted-printable.</param>
        /// <param name="options">How to treat the data.</param>
        /// <returns>The encoded data.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/> member,
        /// or <paramref name="options"/> is not a combination of <see cref="MimeQuotedPrintableEncoderOptions"/> flags, or the encoding
        /// could be longer than <see cref="int.MaxValue"/>.</exception>
        public static string Encode(ReadOnlySpan<byte> source, MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)
        {
            var buffer = new char[GetMaxEncodedLength(source.Length, mode)];
            int charsWritten;
            TryEncode(source, buffer, out charsWritten, mode, options);

            return new string(buffer, 0, charsWritten);
        }

        /// <summary>
        /// Decodes quoted-printable data.
        /// </summary>
        /// <param name="source">The encoded data.</param>
        /// <param name="mode">The form of quoted-printable.</param>
        /// <param name="options">The forms to accept beyond those of <paramref name="mode"/>.</param>
        /// <param name="result">The data, if the encoded data is valid.</param>
        /// <returns><c>true</c> if the encoded data is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/> member,
        /// or <paramref name="options"/> is not a combination of <see cref="MimeQuotedPrintableDecoderOptions"/> flags.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryDecode(ReadOnlySpan<char> source, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options, [NotNullWhen(true)] out byte[]? result)
#else
        public static bool TryDecode(ReadOnlySpan<char> source, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options, out byte[] result)
#endif
        {
            CheckMode(mode);
            CheckOptions(options);

            var buffer = new byte[GetMaxDecodedLength(source.Length)];
            var decoder = new MimeQuotedPrintableDecoderState(mode, options);
            int charsConsumed;
            int bytesWritten;
            if (decoder.Decode(source, buffer, out charsConsumed, out bytesWritten, true) != OperationStatus.Done)
            {
                result = null;
                return false;
            }

            if (bytesWritten != buffer.Length)
            {
                Array.Resize(ref buffer, bytesWritten);
            }

            result = buffer;
            return true;
        }

        /// <summary>
        /// Decodes quoted-printable data.
        /// </summary>
        /// <param name="source">The encoded data.</param>
        /// <param name="mode">The form of quoted-printable.</param>
        /// <param name="options">The forms to accept beyond those of <paramref name="mode"/>.</param>
        /// <returns>The data.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeQuotedPrintableMode"/> member,
        /// or <paramref name="options"/> is not a combination of <see cref="MimeQuotedPrintableDecoderOptions"/> flags.</exception>
        /// <exception cref="FormatException">The encoded data is not valid.</exception>
        public static byte[] Decode(ReadOnlySpan<char> source, MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options)
        {
#if NETCOREAPP3_0_OR_GREATER
            byte[]? result;
#else
            byte[] result;
#endif
            if (!TryDecode(source, mode, options, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        /// <summary>
        /// Finds out whether a line of quoted-printable body data ends with a soft line break.
        /// </summary>
        /// <param name="line">The line, without its line break.</param>
        /// <param name="dataLength">The length of the line before the soft line break; or the length of <paramref name="line"/> if it
        /// doesn't end with one.</param>
        /// <returns><c>true</c> if the line ends with a soft line break; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// A soft line break is an <c>=</c> at the end of the line, which may be followed by spaces and tabs that a transport added
        /// (RFC 2045 §6.7 rules 3 and 5). A format that carries quoted-printable values in its own lines, such as vCard 2.1, uses this to
        /// join a value that continues on the next line: the value is the first <paramref name="dataLength"/> characters of the line,
        /// then the next line.
        /// </remarks>
        public static bool EndsWithSoftLineBreak(ReadOnlySpan<char> line, out int dataLength)
        {
            var end = line.Length;
            while (end > 0 && IsWhitespace(line[end - 1]))
            {
                end--;
            }

            if (end > 0 && line[end - 1] == '=')
            {
                dataLength = end - 1;
                return true;
            }

            dataLength = line.Length;
            return false;
        }

        /// <summary>
        /// Finds out whether a line of quoted-printable body data, in UTF-8 or ASCII, ends with a soft line break.
        /// </summary>
        /// <param name="line">The line, without its line break.</param>
        /// <param name="dataLength">The length of the line before the soft line break; or the length of <paramref name="line"/> if it
        /// doesn't end with one.</param>
        /// <returns><c>true</c> if the line ends with a soft line break; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// A soft line break is an <c>=</c> at the end of the line, which may be followed by spaces and tabs that a transport added
        /// (RFC 2045 §6.7 rules 3 and 5).
        /// </remarks>
        public static bool EndsWithSoftLineBreak(ReadOnlySpan<byte> line, out int dataLength)
        {
            var end = line.Length;
            while (end > 0 && IsWhitespace(line[end - 1]))
            {
                end--;
            }

            if (end > 0 && line[end - 1] == '=')
            {
                dataLength = end - 1;
                return true;
            }

            dataLength = line.Length;
            return false;
        }

        internal static void CheckMode(MimeQuotedPrintableMode mode)
        {
            if (mode < MimeQuotedPrintableMode.Body || mode > MimeQuotedPrintableMode.EncodedWordPhrase) throw new ArgumentOutOfRangeException(nameof(mode));
        }

        internal static void CheckOptions(MimeQuotedPrintableEncoderOptions options)
        {
            if ((options & ~MimeQuotedPrintableEncoderOptions.Binary) != 0) throw new ArgumentOutOfRangeException(nameof(options));
        }

        internal static void CheckOptions(MimeQuotedPrintableDecoderOptions options)
        {
            if ((options & ~AllDecoderOptions) != 0) throw new ArgumentOutOfRangeException(nameof(options));
        }

        // ref. RFC 2045 §6.7: "white space" is SPACE and TAB.
        internal static bool IsWhitespace(int unit)
        {
            return unit == ' ' || unit == '\t';
        }

        // Whether an octet can be written as itself, apart from whitespace in a body.
        internal static bool IsLiteral(int unit, MimeQuotedPrintableMode mode)
        {
            switch (mode)
            {
                case MimeQuotedPrintableMode.Body:
                    // ref. RFC 2045 §6.7 rule 2: octets 33 to 60 and 62 to 126.
                    return unit >= 33 && unit <= 126 && unit != '=';
                case MimeQuotedPrintableMode.EncodedWordText:
                    // ref. RFC 2047 §4.2 rule 3: printable ASCII other than "=", "?" and "_".
                    return unit >= 33 && unit <= 126 && unit != '=' && unit != '?' && unit != '_';
                case MimeQuotedPrintableMode.EncodedWordComment:
                    // ref. RFC 2047 §5(2): nor "(", ")" or the double quote.
                    return unit >= 33 && unit <= 126 && unit != '=' && unit != '?' && unit != '_' && unit != '(' && unit != ')' && unit != '"';
                default:
                    // ref. RFC 2047 §5(3): letters, digits, "!", "*", "+", "-" and "/" ("=" and "_" have their "Q" meanings).
                    return (unit >= 'A' && unit <= 'Z') || (unit >= 'a' && unit <= 'z') || (unit >= '0' && unit <= '9')
                        || unit == '!' || unit == '*' || unit == '+' || unit == '-' || unit == '/';
            }
        }
    }
}
