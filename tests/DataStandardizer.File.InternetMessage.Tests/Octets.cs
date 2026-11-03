using System;
using System.Text;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Converts between bytes and strings that hold one byte in each character, so that the tests can write the exact bytes of a header section
    /// as a string literal, whatever they are.
    /// </summary>
    internal static class Octets
    {
        /// <summary>
        /// Gets the bytes of a string, each of whose characters is one byte.
        /// </summary>
        public static byte[] Of(string text)
        {
            var bytes = new byte[text.Length];
            for (var index = 0; index < text.Length; index++)
            {
                bytes[index] = checked((byte)text[index]);
            }

            return bytes;
        }

        /// <summary>
        /// Gets a string that holds each byte in one character.
        /// </summary>
        public static string ToText(ReadOnlySpan<byte> bytes)
        {
            var text = new StringBuilder(bytes.Length);
            foreach (var value in bytes)
            {
                text.Append((char)value);
            }

            return text.ToString();
        }
    }
}
