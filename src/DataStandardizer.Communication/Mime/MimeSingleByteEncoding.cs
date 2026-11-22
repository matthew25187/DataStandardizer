#if !(NETSTANDARD1_3_OR_GREATER || NETCOREAPP)
using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// An encoding in which each byte up to a limit is the character with the same code: US-ASCII and ISO-8859-1 for netstandard1.1, which
    /// can't give the runtime's encodings the fallbacks the other builds give them.
    /// </summary>
    /// <remarks>
    /// A byte above the limit is read as U+FFFD, and a character above the limit is written as <c>?</c>, as the runtime's encodings do with
    /// a replacement fallback. Each half of a surrogate pair is a character above the limit.
    /// </remarks>
    internal sealed class MimeSingleByteEncoding : Encoding
    {
        internal static readonly MimeSingleByteEncoding Ascii = new MimeSingleByteEncoding("us-ascii", '\x7F');
        internal static readonly MimeSingleByteEncoding Latin1 = new MimeSingleByteEncoding("iso-8859-1", '\xFF');

        private const byte ReplacementByte = (byte)'?';
        private const char ReplacementChar = '\xFFFD';

        private readonly string _webName;
        private readonly char _maxChar;

        private MimeSingleByteEncoding(string webName, char maxChar)
        {
            _webName = webName;
            _maxChar = maxChar;
        }

        public override string WebName => _webName;

        public override int GetByteCount(char[] chars, int index, int count)
        {
            CheckRange(chars, nameof(chars), index, count);

            return count;
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            CheckRange(chars, nameof(chars), charIndex, charCount);
            if (bytes is null) throw new ArgumentNullException(nameof(bytes));
            if (byteIndex < 0 || byteIndex > bytes.Length) throw new ArgumentOutOfRangeException(nameof(byteIndex));

            if (charCount > bytes.Length - byteIndex) throw new ArgumentException("The byte array is too short.", nameof(bytes));

            for (var index = 0; index < charCount; index++)
            {
                var c = chars[charIndex + index];
                bytes[byteIndex + index] = c <= _maxChar ? (byte)c : ReplacementByte;
            }

            return charCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            CheckRange(bytes, nameof(bytes), index, count);

            return count;
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            CheckRange(bytes, nameof(bytes), byteIndex, byteCount);
            if (chars is null) throw new ArgumentNullException(nameof(chars));
            if (charIndex < 0 || charIndex > chars.Length) throw new ArgumentOutOfRangeException(nameof(charIndex));
            if (byteCount > chars.Length - charIndex) throw new ArgumentException("The character array is too short.", nameof(chars));

            for (var index = 0; index < byteCount; index++)
            {
                var c = (char)bytes[byteIndex + index];
                chars[charIndex + index] = c <= _maxChar ? c : ReplacementChar;
            }

            return byteCount;
        }

        public override int GetMaxByteCount(int charCount)
        {
            // One more, as the runtime's encodings allow, for a high surrogate held back by an encoder that holds one.
            if (charCount < 0 || charCount == int.MaxValue) throw new ArgumentOutOfRangeException(nameof(charCount));

            return charCount + 1;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));

            return byteCount;
        }

        public override Encoder GetEncoder()
        {
            return new SingleByteEncoder(this);
        }

        public override Decoder GetDecoder()
        {
            return new SingleByteDecoder(this);
        }

        public override bool Equals(object obj)
        {
            return obj is MimeSingleByteEncoding other && other._webName == _webName;
        }

        public override int GetHashCode()
        {
            return _webName.GetHashCode();
        }

        private static void CheckRange<T>(T[] array, string name, int index, int count)
        {
            if (array is null) throw new ArgumentNullException(name);
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0 || count > array.Length - index) throw new ArgumentOutOfRangeException(nameof(count));
        }

        /// <summary>
        /// Encodes characters that may arrive in several blocks. Each character is written by itself, so nothing is held back.
        /// </summary>
        private sealed class SingleByteEncoder : MimeEncoder
        {
            private readonly MimeSingleByteEncoding _encoding;

            internal SingleByteEncoder(MimeSingleByteEncoding encoding)
            {
                _encoding = encoding;
            }

            public override int GetByteCount(char[] chars, int index, int count, bool flush)
            {
                return _encoding.GetByteCount(chars, index, count);
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush)
            {
                return _encoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
            }
        }

        /// <summary>
        /// Decodes bytes that may arrive in several blocks. Each byte is read by itself, so nothing is held back.
        /// </summary>
        private sealed class SingleByteDecoder : MimeDecoder
        {
            private readonly MimeSingleByteEncoding _encoding;

            internal SingleByteDecoder(MimeSingleByteEncoding encoding)
            {
                _encoding = encoding;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                return _encoding.GetCharCount(bytes, index, count);
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                return _encoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
            }
        }
    }
}
#endif
