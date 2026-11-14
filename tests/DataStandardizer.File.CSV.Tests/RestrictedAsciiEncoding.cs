using System;
using System.Text;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// A custom single-byte encoding of US-ASCII without some of its characters, like a national variant of ISO 646, which .NET doesn't know.
    /// </summary>
    /// <remarks>
    /// Each character up to U+007F that isn't left out is encoded as the byte of the same value, and any other character as '?'.  Each byte up to
    /// 0x7F decodes to the character of the same value, and any other byte to '?'.  It doesn't override <see cref="Encoding.WebName"/>, so, like
    /// any encoding of a code page .NET doesn't know, it has no web name.
    /// </remarks>
    internal sealed class RestrictedAsciiEncoding : Encoding
    {
        private const char LastAscii = '\x7F';
        private const byte ReplacementByte = (byte)'?';

        private readonly char[] _missingCharacters;

        public RestrictedAsciiEncoding(params char[] missingCharacters)
        {
            _missingCharacters = missingCharacters;
        }

        public override int GetByteCount(char[] chars, int index, int count) => count;

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (var offset = 0; offset < charCount; offset++)
            {
                var character = chars[charIndex + offset];
                var isEncodable = character <= LastAscii && Array.IndexOf(_missingCharacters, character) < 0;
                bytes[byteIndex + offset] = isEncodable ? (byte)character : ReplacementByte;
            }

            return charCount;
        }

        public override int GetCharCount(byte[] bytes, int index, int count) => count;

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (var offset = 0; offset < byteCount; offset++)
            {
                var value = bytes[byteIndex + offset];
                chars[charIndex + offset] = value <= LastAscii ? (char)value : (char)ReplacementByte;
            }

            return byteCount;
        }

        public override int GetMaxByteCount(int charCount) => charCount;

        public override int GetMaxCharCount(int byteCount) => byteCount;
    }
}
