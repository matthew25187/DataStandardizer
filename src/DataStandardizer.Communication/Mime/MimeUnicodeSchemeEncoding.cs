using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The UTF-16 or UTF-32 encoding scheme, whose byte order is given by a byte order mark, and is big-endian if there is none.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reading removes a leading byte order mark (RFC 2781 §4.3; The Unicode Standard §3.10, D98 and D101). Writing is big-endian with no
    /// byte order mark, which needs none.
    /// </para>
    /// <para>
    /// <c>Encoding.GetEncoding("utf-16")</c> and <c>Encoding.GetEncoding("utf-32")</c> are little-endian, and don't look for a byte order
    /// mark, so they can't be used for these character sets.
    /// </para>
    /// </remarks>
    internal sealed class MimeUnicodeSchemeEncoding : Encoding
    {
        internal static readonly MimeUnicodeSchemeEncoding Utf16 = new MimeUnicodeSchemeEncoding(
            "utf-16", "Unicode (UTF-16)", new UnicodeEncoding(true, false), new UnicodeEncoding(false, false),
            new byte[] { 0xFE, 0xFF }, new byte[] { 0xFF, 0xFE });

        internal static readonly MimeUnicodeSchemeEncoding Utf32 = new MimeUnicodeSchemeEncoding(
            "utf-32", "Unicode (UTF-32)",
#if NETSTANDARD1_3_OR_GREATER || NETCOREAPP
            new UTF32Encoding(true, false), new UTF32Encoding(false, false),
#else
            MimeUtf32Encoding.BigEndian, MimeUtf32Encoding.LittleEndian,
#endif
            new byte[] { 0x00, 0x00, 0xFE, 0xFF }, new byte[] { 0xFF, 0xFE, 0x00, 0x00 });

        private static readonly byte[] NoPreamble = new byte[0];

        private readonly string _webName;
        private readonly string _encodingName;
        private readonly byte[] _bigEndianMark;
        private readonly byte[] _littleEndianMark;

        private MimeUnicodeSchemeEncoding(string webName, string encodingName, Encoding bigEndian, Encoding littleEndian, byte[] bigEndianMark,
            byte[] littleEndianMark)
        {
            _webName = webName;
            _encodingName = encodingName;
            BigEndian = bigEndian;
            LittleEndian = littleEndian;
            _bigEndianMark = bigEndianMark;
            _littleEndianMark = littleEndianMark;
        }

        /// <summary>
        /// Gets the encoding of the scheme in big-endian byte order, with no byte order mark.
        /// </summary>
        internal Encoding BigEndian { get; }

        /// <summary>
        /// Gets the encoding of the scheme in little-endian byte order, with no byte order mark.
        /// </summary>
        internal Encoding LittleEndian { get; }

        public override string WebName => _webName;

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        public override string EncodingName => _encodingName;
#endif

        private int MarkLength => _bigEndianMark.Length;

        public override byte[] GetPreamble()
        {
            return NoPreamble;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return BigEndian.GetByteCount(chars, index, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return BigEndian.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            CheckRange(bytes, index, count);

            int markLength;
            var encoding = SelectEncoding(bytes, index, count, out markLength);
            return encoding.GetCharCount(bytes, index + markLength, count - markLength);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            CheckRange(bytes, byteIndex, byteCount);

            int markLength;
            var encoding = SelectEncoding(bytes, byteIndex, byteCount, out markLength);
            return encoding.GetChars(bytes, byteIndex + markLength, byteCount - markLength, chars, charIndex);
        }

        public override int GetMaxByteCount(int charCount)
        {
            return BigEndian.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return BigEndian.GetMaxCharCount(byteCount);
        }

        public override Encoder GetEncoder()
        {
            return BigEndian.GetEncoder();
        }

        public override Decoder GetDecoder()
        {
            return new SchemeDecoder(this);
        }

#if NETCOREAPP3_0_OR_GREATER
        public override bool Equals(object? obj)
#else
        public override bool Equals(object obj)
#endif
        {
            return ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return _webName.GetHashCode();
        }

        /// <summary>
        /// Select the encoding for the byte order shown by a byte order mark at the start of some bytes.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="index">The position of the first byte.</param>
        /// <param name="count">The number of bytes.</param>
        /// <param name="markLength">The length of the byte order mark; or 0 if there is none.</param>
        /// <returns>The little-endian encoding if there is a little-endian byte order mark; otherwise the big-endian encoding.</returns>
        private Encoding SelectEncoding(byte[] bytes, int index, int count, out int markLength)
        {
            markLength = MarkLength;
            if (StartsWith(bytes, index, count, _littleEndianMark)) return LittleEndian;
            if (StartsWith(bytes, index, count, _bigEndianMark)) return BigEndian;

            markLength = 0;
            return BigEndian;
        }

        private static bool StartsWith(byte[] bytes, int index, int count, byte[] prefix)
        {
            if (count < prefix.Length) return false;

            for (var prefixIndex = 0; prefixIndex < prefix.Length; prefixIndex++)
            {
                if (bytes[index + prefixIndex] != prefix[prefixIndex]) return false;
            }

            return true;
        }

        private static void CheckRange(byte[] bytes, int index, int count)
        {
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > bytes.Length - index) throw new ArgumentOutOfRangeException(nameof(count));
        }

        /// <summary>
        /// Decodes bytes that may arrive in several blocks, holding back the first bytes until there are enough to tell whether they are a
        /// byte order mark.
        /// </summary>
        private sealed class SchemeDecoder : Decoder
        {
            private readonly MimeUnicodeSchemeEncoding _encoding;
            private readonly byte[] _pending;
            private int _pendingCount;
#if NETCOREAPP3_0_OR_GREATER
            private Decoder? _inner;
#else
            private Decoder _inner;
#endif

            internal SchemeDecoder(MimeUnicodeSchemeEncoding encoding)
            {
                _encoding = encoding;
                _pending = new byte[encoding.MarkLength];
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return GetCharCount(bytes, index, count, false);
            }

            public override int GetCharCount(byte[] bytes, int index, int count, bool flush)
            {
                CheckRange(bytes, index, count);

                if (_inner != null) return _inner.GetCharCount(bytes, index, count, flush);

                var combined = Combine(bytes, index, count);
                if (combined.Length < _pending.Length && !flush) return 0;

                int markLength;
                var encoding = _encoding.SelectEncoding(combined, 0, combined.Length, out markLength);
                return encoding.GetDecoder().GetCharCount(combined, markLength, combined.Length - markLength, flush);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                return GetChars(bytes, byteIndex, byteCount, chars, charIndex, false);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush)
            {
                CheckRange(bytes, byteIndex, byteCount);

                if (_inner != null) return _inner.GetChars(bytes, byteIndex, byteCount, chars, charIndex, flush);

                var combined = Combine(bytes, byteIndex, byteCount);
                if (combined.Length < _pending.Length && !flush)
                {
                    Array.Copy(combined, _pending, combined.Length);
                    _pendingCount = combined.Length;
                    return 0;
                }

                int markLength;
                var encoding = _encoding.SelectEncoding(combined, 0, combined.Length, out markLength);
                var inner = encoding.GetDecoder();
                var charCount = inner.GetChars(combined, markLength, combined.Length - markLength, chars, charIndex, flush);
                _pendingCount = 0;
                _inner = flush ? null : inner;
                return charCount;
            }

            public override void Reset()
            {
                _pendingCount = 0;
                _inner = null;
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
