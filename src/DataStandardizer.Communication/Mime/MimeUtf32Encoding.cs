#if !(NETSTANDARD1_3_OR_GREATER || NETCOREAPP)
using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// UTF-32 in one byte order, with no byte order mark, for the targets that don't have <c>UTF32Encoding</c>.
    /// </summary>
    /// <remarks>
    /// A code unit that is not a Unicode scalar value, and the bytes of an incomplete code unit at the end, are read as U+FFFD. An unpaired
    /// surrogate is written as U+FFFD.
    /// </remarks>
    internal sealed class MimeUtf32Encoding : Encoding
    {
        internal static readonly MimeUtf32Encoding BigEndian = new MimeUtf32Encoding(true);
        internal static readonly MimeUtf32Encoding LittleEndian = new MimeUtf32Encoding(false);

        private const int UnitLength = 4;
        private const int ReplacementCodePoint = 0xFFFD;

        private readonly bool _bigEndian;

        private MimeUtf32Encoding(bool bigEndian)
        {
            _bigEndian = bigEndian;
        }

        public override string WebName => _bigEndian ? "utf-32BE" : "utf-32";

        public override int GetByteCount(char[] chars, int index, int count)
        {
            CheckRange(chars, nameof(chars), index, count);

            char pendingHighSurrogate;
            return Encode('\0', chars, index, count, true, null, 0, out pendingHighSurrogate);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            CheckRange(chars, nameof(chars), charIndex, charCount);
            CheckDestination(bytes, nameof(bytes), byteIndex);

            char pendingHighSurrogate;
            return Encode('\0', chars, charIndex, charCount, true, bytes, byteIndex, out pendingHighSurrogate);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            CheckRange(bytes, nameof(bytes), index, count);

            int pendingByteCount;
            return Decode(bytes, index, count, true, null, 0, out pendingByteCount);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            CheckRange(bytes, nameof(bytes), byteIndex, byteCount);
            CheckDestination(chars, nameof(chars), charIndex);

            int pendingByteCount;
            return Decode(bytes, byteIndex, byteCount, true, chars, charIndex, out pendingByteCount);
        }

        public override int GetMaxByteCount(int charCount)
        {
            if (charCount < 0) throw new ArgumentOutOfRangeException(nameof(charCount));

            // One more character, for a high surrogate held back by an encoder.
            return (charCount + 1) * UnitLength;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));

            // Each code unit gives at most a surrogate pair, and bytes held back by a decoder may complete one more.
            return (byteCount / UnitLength + 1) * 2;
        }

        public override Encoder GetEncoder()
        {
            return new Utf32Encoder(this);
        }

        public override Decoder GetDecoder()
        {
            return new Utf32Decoder(this);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return WebName.GetHashCode();
        }

        /// <summary>
        /// Encode characters, or count the bytes they encode to.
        /// </summary>
        /// <param name="leadingHighSurrogate">A high surrogate held back from the previous block; or <c>'\0'</c> if there is none.</param>
        /// <param name="chars">The characters.</param>
        /// <param name="index">The position of the first character.</param>
        /// <param name="count">The number of characters.</param>
        /// <param name="flush">Whether this is the last block, so that a high surrogate at the end is not held back.</param>
        /// <param name="bytes">The array to write to; or <c>null</c> to only count the bytes.</param>
        /// <param name="byteIndex">The position to write the first byte to.</param>
        /// <param name="pendingHighSurrogate">A high surrogate at the end, held back for the next block; or <c>'\0'</c> if there is none.</param>
        /// <returns>The number of bytes.</returns>
        private int Encode(char leadingHighSurrogate, char[] chars, int index, int count, bool flush, byte[] bytes, int byteIndex,
            out char pendingHighSurrogate)
        {
            var position = byteIndex;
            var highSurrogate = leadingHighSurrogate;
            var end = index + count;
            for (var charIndex = index; charIndex < end; charIndex++)
            {
                var c = chars[charIndex];
                if (highSurrogate != '\0')
                {
                    var paired = char.IsLowSurrogate(c);
                    WriteUnit(paired ? 0x10000 + ((highSurrogate - 0xD800) << 10) + (c - 0xDC00) : ReplacementCodePoint, bytes, ref position);
                    highSurrogate = '\0';
                    if (paired) continue;
                }

                if (char.IsHighSurrogate(c))
                {
                    highSurrogate = c;
                    continue;
                }

                WriteUnit(char.IsLowSurrogate(c) ? ReplacementCodePoint : c, bytes, ref position);
            }

            if (highSurrogate != '\0' && flush)
            {
                WriteUnit(ReplacementCodePoint, bytes, ref position);
                highSurrogate = '\0';
            }

            pendingHighSurrogate = highSurrogate;
            return position - byteIndex;
        }

        private void WriteUnit(int codePoint, byte[] bytes, ref int position)
        {
            if (bytes != null)
            {
                if (position > bytes.Length - UnitLength) throw new ArgumentException("The byte array is too short.", nameof(bytes));

                for (var byteIndex = 0; byteIndex < UnitLength; byteIndex++)
                {
                    var shift = _bigEndian ? (UnitLength - 1 - byteIndex) * 8 : byteIndex * 8;
                    bytes[position + byteIndex] = (byte)(codePoint >> shift);
                }
            }

            position += UnitLength;
        }

        /// <summary>
        /// Decode bytes, or count the characters they decode to.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The position of the first byte.</param>
        /// <param name="count">The number of bytes.</param>
        /// <param name="flush">Whether this is the last block, so that an incomplete code unit at the end is not held back.</param>
        /// <param name="chars">The array to write to; or <c>null</c> to only count the characters.</param>
        /// <param name="charIndex">The position to write the first character to.</param>
        /// <param name="pendingByteCount">The number of bytes at the end held back for the next block.</param>
        /// <returns>The number of characters.</returns>
        private int Decode(byte[] bytes, int index, int count, bool flush, char[] chars, int charIndex, out int pendingByteCount)
        {
            var position = charIndex;
            var unitCount = count / UnitLength;
            for (var unit = 0; unit < unitCount; unit++)
            {
                var codePoint = 0;
                for (var byteIndex = 0; byteIndex < UnitLength; byteIndex++)
                {
                    var shift = _bigEndian ? (UnitLength - 1 - byteIndex) * 8 : byteIndex * 8;
                    codePoint |= bytes[index + unit * UnitLength + byteIndex] << shift;
                }

                // A Unicode scalar value is U+0000 to U+10FFFF, apart from the surrogates. The cast keeps a code unit above 0x7FFFFFFF out.
                if ((uint)codePoint > 0x10FFFF || (codePoint >= 0xD800 && codePoint <= 0xDFFF)) codePoint = ReplacementCodePoint;

                if (codePoint < 0x10000)
                {
                    WriteChar((char)codePoint, chars, ref position);
                }
                else
                {
                    codePoint -= 0x10000;
                    WriteChar((char)(0xD800 + (codePoint >> 10)), chars, ref position);
                    WriteChar((char)(0xDC00 + (codePoint & 0x3FF)), chars, ref position);
                }
            }

            pendingByteCount = count % UnitLength;
            if (pendingByteCount > 0 && flush)
            {
                WriteChar((char)ReplacementCodePoint, chars, ref position);
                pendingByteCount = 0;
            }

            return position - charIndex;
        }

        private static void WriteChar(char c, char[] chars, ref int position)
        {
            if (chars != null)
            {
                if (position >= chars.Length) throw new ArgumentException("The character array is too short.", nameof(chars));

                chars[position] = c;
            }

            position++;
        }

        private static void CheckRange<T>(T[] array, string name, int index, int count)
        {
            if (array is null) throw new ArgumentNullException(name);
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > array.Length - index) throw new ArgumentOutOfRangeException(nameof(count));
        }

        private static void CheckDestination<T>(T[] array, string name, int index)
        {
            if (array is null) throw new ArgumentNullException(name);
            if (index < 0 || index > array.Length) throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// Encodes characters that may arrive in several blocks, holding back a high surrogate at the end of a block.
        /// </summary>
        private sealed class Utf32Encoder : Encoder
        {
            private readonly MimeUtf32Encoding _encoding;
            private char _pendingHighSurrogate;

            internal Utf32Encoder(MimeUtf32Encoding encoding)
            {
                _encoding = encoding;
            }

            public override int GetByteCount(char[] chars, int index, int count, bool flush)
            {
                CheckRange(chars, nameof(chars), index, count);

                char pendingHighSurrogate;
                return _encoding.Encode(_pendingHighSurrogate, chars, index, count, flush, null, 0, out pendingHighSurrogate);
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
            {
                CheckRange(chars, nameof(chars), charIndex, charCount);
                CheckDestination(bytes, nameof(bytes), byteIndex);

                return _encoding.Encode(_pendingHighSurrogate, chars, charIndex, charCount, flush, bytes, byteIndex, out _pendingHighSurrogate);
            }
        }

        /// <summary>
        /// Decodes bytes that may arrive in several blocks, holding back an incomplete code unit at the end of a block.
        /// </summary>
        private sealed class Utf32Decoder : Decoder
        {
            private readonly MimeUtf32Encoding _encoding;
            private readonly byte[] _pending = new byte[UnitLength];
            private int _pendingCount;

            internal Utf32Decoder(MimeUtf32Encoding encoding)
            {
                _encoding = encoding;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return GetCharCount(bytes, index, count, false);
            }

            public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
            {
                CheckRange(bytes, nameof(bytes), index, count);

                var combined = Combine(bytes, index, count);
                int pendingByteCount;
                return _encoding.Decode(combined, 0, combined.Length, flush, null, 0, out pendingByteCount);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
            {
                CheckRange(bytes, nameof(bytes), byteIndex, byteCount);
                CheckDestination(chars, nameof(chars), charIndex);

                var combined = Combine(bytes, byteIndex, byteCount);
                int pendingByteCount;
                var charCount = _encoding.Decode(combined, 0, combined.Length, flush, chars, charIndex, out pendingByteCount);
                Array.Copy(combined, combined.Length - pendingByteCount, _pending, 0, pendingByteCount);
                _pendingCount = pendingByteCount;
                return charCount;
            }

            public override void Reset()
            {
                _pendingCount = 0;
            }

            /// <summary>
            /// Get the bytes held back, followed by some new bytes.
            /// </summary>
            private byte[] Combine(byte[] bytes, int index, int count)
            {
                var combined = new byte[_pendingCount + count];
                Array.Copy(_pending, combined, _pendingCount);
                Array.Copy(bytes, index, combined, _pendingCount, count);
                return combined;
            }
        }
    }
}
#endif
