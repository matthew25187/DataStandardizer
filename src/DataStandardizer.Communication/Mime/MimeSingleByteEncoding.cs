#if !NET5_0_OR_GREATER
using System;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// An encoding in which each byte up to a limit is the character with the same code: ISO-8859-1 for the targets that don't have
    /// <c>Encoding.Latin1</c>, and US-ASCII for those that don't have <c>Encoding.ASCII</c>.
    /// </summary>
    /// <remarks>
    /// A byte above the limit is read as U+FFFD, and a character above the limit is written as <c>?</c>, as the runtime's encodings do with
    /// a replacement fallback. Each half of a surrogate pair is a character above the limit.
    /// </remarks>
    internal sealed class MimeSingleByteEncoding : Encoding
    {
        internal static readonly MimeSingleByteEncoding Latin1 = new MimeSingleByteEncoding("iso-8859-1", "Western European (ISO)", '\xFF');

#if !(NETSTANDARD1_3_OR_GREATER || NETCOREAPP)
        internal static readonly MimeSingleByteEncoding Ascii = new MimeSingleByteEncoding("us-ascii", "US-ASCII", '\x7F');
#endif

        private const byte ReplacementByte = (byte)'?';
        private const char ReplacementChar = '\uFFFD';

        private readonly string _webName;
        private readonly string _encodingName;
        private readonly char _maxChar;

        private MimeSingleByteEncoding(string webName, string encodingName, char maxChar)
        {
            _webName = webName;
            _encodingName = encodingName;
            _maxChar = maxChar;
        }

        public override string WebName => _webName;

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        public override string EncodingName => _encodingName;

        public override bool IsSingleByte => true;
#endif

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
            if (charCount < 0) throw new ArgumentOutOfRangeException(nameof(charCount));

            return charCount;
        }

        public override int GetMaxCharCount(int byteCount)
        {
            if (byteCount < 0) throw new ArgumentOutOfRangeException(nameof(byteCount));

            return byteCount;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj);
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
    }
}
#endif
