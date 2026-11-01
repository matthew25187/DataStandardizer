using System.Collections.Generic;

namespace DataStandardizer.Communication.Tests.Mime
{
    /// <summary>
    /// The names the built-in encodings of <c>MimeCharsetResolver</c> resolve, with some text and its bytes in each.
    /// </summary>
    internal static class MimeCharsetResolverTestData
    {
        // ref. IANA Character Sets registry; RFC 2781 §3.3, §4.3: UTF-16 and UTF-32 are read by their byte order mark, and are
        // big-endian without one.
        public const string Utf16Name = "UTF-16";
        public const string Utf32Name = "UTF-32";

        private const string AsciiText = "Hello";
        private static readonly byte[] AsciiBytes = { 0x48, 0x65, 0x6C, 0x6C, 0x6F };

        private const string Latin1Text = "caf\x00E9 \x00FF";
        private static readonly byte[] Latin1Bytes = { 0x63, 0x61, 0x66, 0xE9, 0x20, 0xFF };

        private const string Utf8Text = "caf\x00E9 \xD83D\xDE00";
        private static readonly byte[] Utf8Bytes = { 0x63, 0x61, 0x66, 0xC3, 0xA9, 0x20, 0xF0, 0x9F, 0x98, 0x80 };

        private const string UnicodeText = "\x00E9\xD83D\xDE00";
        private static readonly byte[] Utf16BigEndianBytes = { 0x00, 0xE9, 0xD8, 0x3D, 0xDE, 0x00 };
        private static readonly byte[] Utf16LittleEndianBytes = { 0xE9, 0x00, 0x3D, 0xD8, 0x00, 0xDE };
        private static readonly byte[] Utf32BigEndianBytes = { 0x00, 0x00, 0x00, 0xE9, 0x00, 0x01, 0xF6, 0x00 };
        private static readonly byte[] Utf32LittleEndianBytes = { 0xE9, 0x00, 0x00, 0x00, 0x00, 0xF6, 0x01, 0x00 };

        // UnicodeText after a byte order mark, U+FEFF, in each byte order.
        private static readonly byte[] Utf16BigEndianMarkedBytes = { 0xFE, 0xFF, 0x00, 0xE9, 0xD8, 0x3D, 0xDE, 0x00 };
        private static readonly byte[] Utf16LittleEndianMarkedBytes = { 0xFF, 0xFE, 0xE9, 0x00, 0x3D, 0xD8, 0x00, 0xDE };
        private static readonly byte[] Utf32BigEndianMarkedBytes = { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0xE9, 0x00, 0x01, 0xF6, 0x00 };
        private static readonly byte[] Utf32LittleEndianMarkedBytes = { 0xFF, 0xFE, 0x00, 0x00, 0xE9, 0x00, 0x00, 0x00, 0x00, 0xF6, 0x01, 0x00 };

        /// <summary>
        /// Each built-in name and alias, with text and its bytes. The bytes of <c>UTF-16</c> and <c>UTF-32</c> have no byte order mark, so
        /// they are big-endian.
        /// </summary>
        /// <remarks>
        /// ref. IANA Character Sets registry: the names and aliases that are valid mime-charset names.
        /// </remarks>
        public static IEnumerable<object[]> BuiltInNames()
        {
            foreach (var name in new[] { "US-ASCII", "iso-ir-6", "ISO646-US", "us", "IBM367", "cp367", "csASCII" })
            {
                yield return new object[] { name, AsciiText, AsciiBytes };
            }

            foreach (var name in new[] { "ISO-8859-1", "ISO_8859-1", "iso-ir-100", "latin1", "l1", "IBM819", "CP819", "csISOLatin1" })
            {
                yield return new object[] { name, Latin1Text, Latin1Bytes };
            }

            foreach (var name in new[] { "UTF-8", "csUTF8" })
            {
                yield return new object[] { name, Utf8Text, Utf8Bytes };
            }

            foreach (var name in new[] { Utf16Name, "csUTF16", "UTF-16BE", "csUTF16BE" })
            {
                yield return new object[] { name, UnicodeText, Utf16BigEndianBytes };
            }

            foreach (var name in new[] { "UTF-16LE", "csUTF16LE" })
            {
                yield return new object[] { name, UnicodeText, Utf16LittleEndianBytes };
            }

            foreach (var name in new[] { Utf32Name, "csUTF32", "UTF-32BE", "csUTF32BE" })
            {
                yield return new object[] { name, UnicodeText, Utf32BigEndianBytes };
            }

            foreach (var name in new[] { "UTF-32LE", "csUTF32LE" })
            {
                yield return new object[] { name, UnicodeText, Utf32LittleEndianBytes };
            }
        }

        /// <summary>
        /// Each built-in name and alias.
        /// </summary>
        public static IEnumerable<object[]> BuiltInNameList()
        {
            foreach (var row in BuiltInNames())
            {
                yield return new[] { row[0] };
            }
        }

        /// <summary>
        /// Each built-in name and alias, with the same name in upper case and in lower case.
        /// </summary>
        public static IEnumerable<object[]> BuiltInNamesInOtherCase()
        {
            foreach (var row in BuiltInNames())
            {
                var name = (string)row[0];
                yield return new object[] { name, name.ToUpperInvariant() };
                yield return new object[] { name, name.ToLowerInvariant() };
            }
        }

        /// <summary>
        /// The character sets that are read by their byte order mark, with text after the mark in each byte order.
        /// </summary>
        public static IEnumerable<object[]> ByteOrderMarks()
        {
            yield return new object[] { Utf16Name, Utf16BigEndianMarkedBytes, UnicodeText };
            yield return new object[] { Utf16Name, Utf16LittleEndianMarkedBytes, UnicodeText };
            yield return new object[] { Utf32Name, Utf32BigEndianMarkedBytes, UnicodeText };
            yield return new object[] { Utf32Name, Utf32LittleEndianMarkedBytes, UnicodeText };
        }
    }
}
