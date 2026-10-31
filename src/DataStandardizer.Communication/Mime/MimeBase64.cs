using System;
using System.Buffers;
#if NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Encodes and decodes whole values in base64: the <c>base64</c> Content-Transfer-Encoding of RFC 2045 §6.8, or the strict base64 of
    /// RFC 4648 §4 that iCalendar BINARY values and RFC 2047 "B" encoded words use.
    /// </summary>
    /// <remarks>
    /// To encode or decode data that arrives in parts, use <see cref="MimeBase64Encoder"/> or <see cref="MimeBase64Decoder"/>.
    /// </remarks>
    public static class MimeBase64
    {
        // ref. RFC 4648 §4, Table 1
        internal const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

        // ref. RFC 2045 §6.8: the encoded output stream must be represented in lines of no more than 76 characters each.
        internal const int MaxLineLength = 76;

        private const int QuantaPerLine = MaxLineLength / 4;
        private const string InvalidFormatMessage = "The value is not valid base64.";

        // The value of each ASCII character in the alphabet, or -1.
        internal static readonly sbyte[] DecodeMap = CreateDecodeMap();

#if NET10_0_OR_GREATER
        internal static readonly SearchValues<byte> Utf8Alphabet = SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/"u8);
        internal static readonly SearchValues<char> CharAlphabet = SearchValues.Create(Alphabet);
#endif

        /// <summary>
        /// Gets the length of the base64 encoding of some data.
        /// </summary>
        /// <param name="length">The number of bytes to encode.</param>
        /// <param name="mode">The form of base64.</param>
        /// <returns>The number of characters in the encoding, including any line breaks.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative, or its encoding would be longer than
        /// <see cref="int.MaxValue"/>; or <paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member.</exception>
        public static int GetEncodedLength(int length, MimeBase64Mode mode)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
            CheckMode(mode);

            var quanta = (length + 2L) / 3;
            var encodedLength = quanta * 4;
            if (mode == MimeBase64Mode.Mime && quanta > 0)
            {
                encodedLength += (quanta - 1) / QuantaPerLine * 2;
            }

            if (encodedLength > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(length));

            return (int)encodedLength;
        }

        /// <summary>
        /// Gets the largest number of bytes that some base64 data can decode to.
        /// </summary>
        /// <param name="length">The number of characters of encoded data.</param>
        /// <returns>The largest number of bytes the data can decode to, whatever its mode.</returns>
        /// <remarks>
        /// A <see cref="MimeBase64Decoder"/> that holds part of a quantum from an earlier call can write up to 3 more bytes in one call.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is negative.</exception>
        public static int GetMaxDecodedLength(int length)
        {
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));

            return length / 4 * 3;
        }

        /// <summary>
        /// Encodes data in base64.
        /// </summary>
        /// <param name="source">The data.</param>
        /// <param name="destination">The span to write to. <see cref="GetEncodedLength"/> gives the length it needs.</param>
        /// <param name="charsWritten">The number of characters written; or 0 if <paramref name="destination"/> is too short.</param>
        /// <param name="mode">The form of base64.</param>
        /// <returns><c>true</c> if the data was written; <c>false</c> if <paramref name="destination"/> is too short.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member.</exception>
        public static bool TryEncode(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten, MimeBase64Mode mode)
        {
            CheckMode(mode);

            var encoder = new MimeBase64EncoderState(mode);
            int bytesConsumed;
            if (encoder.Encode(source, destination, out bytesConsumed, out charsWritten, true) == OperationStatus.Done) return true;

            charsWritten = 0;
            return false;
        }

        /// <summary>
        /// Encodes data in base64.
        /// </summary>
        /// <param name="source">The data.</param>
        /// <param name="mode">The form of base64.</param>
        /// <returns>The encoded data.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member, or the
        /// encoding would be longer than <see cref="int.MaxValue"/>.</exception>
        public static string Encode(ReadOnlySpan<byte> source, MimeBase64Mode mode)
        {
            var buffer = new char[GetEncodedLength(source.Length, mode)];
            int charsWritten;
            TryEncode(source, buffer, out charsWritten, mode);

            return new string(buffer);
        }

        /// <summary>
        /// Decodes base64 data.
        /// </summary>
        /// <param name="source">The encoded data.</param>
        /// <param name="mode">The form of base64.</param>
        /// <param name="options">Checks to make beyond those of <paramref name="mode"/>.</param>
        /// <param name="result">The data, if the encoded data is valid.</param>
        /// <returns><c>true</c> if the encoded data is valid; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeBase64DecoderOptions"/> flags.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryDecode(ReadOnlySpan<char> source, MimeBase64Mode mode, MimeBase64DecoderOptions options, [NotNullWhen(true)] out byte[]? result)
#else
        public static bool TryDecode(ReadOnlySpan<char> source, MimeBase64Mode mode, MimeBase64DecoderOptions options, out byte[] result)
#endif
        {
            CheckMode(mode);
            CheckOptions(options);

            var buffer = new byte[GetMaxDecodedLength(source.Length)];
            var decoder = new MimeBase64DecoderState(mode, options);
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
        /// Decodes base64 data.
        /// </summary>
        /// <param name="source">The encoded data.</param>
        /// <param name="mode">The form of base64.</param>
        /// <param name="options">Checks to make beyond those of <paramref name="mode"/>.</param>
        /// <returns>The data.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is not a <see cref="MimeBase64Mode"/> member, or
        /// <paramref name="options"/> is not a combination of <see cref="MimeBase64DecoderOptions"/> flags.</exception>
        /// <exception cref="FormatException">The encoded data is not valid.</exception>
        public static byte[] Decode(ReadOnlySpan<char> source, MimeBase64Mode mode, MimeBase64DecoderOptions options)
        {
#if NETCOREAPP3_0_OR_GREATER
            byte[]? result;
#else
            byte[] result;
#endif
            if (!TryDecode(source, mode, options, out result)) throw new FormatException(InvalidFormatMessage);

            return result;
        }

        internal static void CheckMode(MimeBase64Mode mode)
        {
            if (mode != MimeBase64Mode.Strict && mode != MimeBase64Mode.Mime) throw new ArgumentOutOfRangeException(nameof(mode));
        }

        internal static void CheckOptions(MimeBase64DecoderOptions options)
        {
            if ((options & ~MimeBase64DecoderOptions.RejectNonAlphabetCharacters) != 0) throw new ArgumentOutOfRangeException(nameof(options));
        }

        private static sbyte[] CreateDecodeMap()
        {
            var map = new sbyte[128];
            for (var index = 0; index < map.Length; index++)
            {
                map[index] = -1;
            }

            for (var index = 0; index < Alphabet.Length; index++)
            {
                map[Alphabet[index]] = (sbyte)index;
            }

            return map;
        }
    }
}
