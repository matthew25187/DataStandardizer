using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Pins how the reader that the CSV reader creates for a stream decodes it: each byte the decoder can't decode reads as the lone surrogate
    /// U+DC00 plus the byte, and a byte order mark chooses the encoding only when none is declared.
    /// </summary>
    /// <remarks>
    /// Every case runs for each way the reader finds the bytes it can't decode, by fallback or by exception, which must escape the same bytes, and
    /// for each block size, so that a byte sequence straddles the end of a block.  The cases aren't serializable, so that xUnit doesn't pass their
    /// strings, which hold lone surrogates, through UTF-8.
    /// </remarks>
    public class CsvStreamTextReaderTests
    {
        // The smallest block the reader reads, the length of the longest byte order mark, then one that ends each block one byte later, and one
        // that holds the whole stream.
        private static readonly int[] BlockSizes = new int[] { 4, 5, 4096 };
        private static readonly bool[] EscapesByException = new bool[] { false, true };

        // A declared encoding of UTF-8 without a preamble, so that a byte order mark isn't passed over only because the encoding writes one.
        private static readonly Encoding Utf8WithoutPreamble = new UTF8Encoding(false);

        // 63 bytes of "a", then "\x00E9", whose two bytes straddle the end of the first 64-byte run the reader counts in a block it falls back
        // in, then "b" and 0xFF, which is never in UTF-8.
        private static readonly byte[] LongRunBytes = Enumerable.Repeat((byte)0x61, 63).Concat(new byte[] { 0xC3, 0xA9, 0x62, 0xFF }).ToArray();
        private static readonly string LongRunExpected = new string('a', 63) + "\x00E9b\xDCFF";

        private static readonly DecodingCase[] Cases = new DecodingCase[]
        {
            // "a\x00E9": valid UTF-8 decodes as it is.
            new DecodingCase("Utf8Valid", new byte[] { 0x61, 0xC3, 0xA9 }, Encoding.UTF8, false, "a\x00E9"),
            // A lead byte without its continuation byte, before an ASCII character.
            new DecodingCase("Utf8LeadByteBeforeAscii", new byte[] { 0x61, 0xC3, 0x62 }, Encoding.UTF8, false, "a\xDCC3b"),
            // A byte that is never in UTF-8.
            new DecodingCase("Utf8InvalidByte", new byte[] { 0x61, 0xFF, 0x62 }, Encoding.UTF8, false, "a\xDCFFb"),
            // The first two bytes of a three-byte character, at the end of the stream.
            new DecodingCase("Utf8TruncatedAtEnd", new byte[] { 0x61, 0xE4, 0xB8 }, Encoding.UTF8, false, "a\xDCE4\xDCB8"),
            // The first two bytes of a three-byte character, before an ASCII character.
            new DecodingCase("Utf8TruncatedBeforeAscii", new byte[] { 0xE4, 0xB8, 0x61 }, Encoding.UTF8, false, "\xDCE4\xDCB8a"),
            // A lead byte, then a whole two-byte character that starts with the same lead byte.
            new DecodingCase("Utf8LeadByteBeforeCharacter", new byte[] { 0xC3, 0xC3, 0xA9 }, Encoding.UTF8, false, "\xDCC3\x00E9"),
            // U+FFFD in the file is a character like any other, and stays one beside a byte that isn't.
            new DecodingCase("Utf8ReplacementCharacterBeforeInvalidByte", new byte[] { 0xEF, 0xBF, 0xBD, 0xFF }, Encoding.UTF8, false, "\xFFFD\xDCFF"),
            new DecodingCase("Utf8InvalidByteBeforeReplacementCharacter", new byte[] { 0xFF, 0xEF, 0xBF, 0xBD }, Encoding.UTF8, false, "\xDCFF\xFFFD"),
            // A high surrogate without its low surrogate, before U+FFFD, in UTF-16 little endian: the decoder only finds the high surrogate alone
            // at the last byte of the character after it.
            new DecodingCase("Utf16LoneHighSurrogateBeforeReplacementCharacter", new byte[] { 0x00, 0xD8, 0xFD, 0xFF }, Encoding.Unicode, true,
                "\xDC00\xDCD8\xFFFD"),
            // Half of a UTF-16 code unit at the end of the stream.
            new DecodingCase("Utf16OddByteAtEnd", new byte[] { 0x61, 0x00, 0x62 }, Encoding.Unicode, true, "a\xDC62"),
            // A byte outside US-ASCII.
            new DecodingCase("AsciiByteOutsideAscii", new byte[] { 0x61, 0xE9 }, Encoding.ASCII, true, "a\xDCE9"),
            // Without a declared encoding, a byte order mark chooses it, and isn't read.
            new DecodingCase("Utf16LittleEndianByteOrderMark", new byte[] { 0xFF, 0xFE, 0x61, 0x00 }, Encoding.UTF8, false, "a"),
            new DecodingCase("Utf16BigEndianByteOrderMark", new byte[] { 0xFE, 0xFF, 0x00, 0x61 }, Encoding.UTF8, false, "a"),
            new DecodingCase("Utf32LittleEndianByteOrderMark", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00 }, Encoding.UTF8, false, "a"),
            new DecodingCase("Utf32BigEndianByteOrderMark", new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x61 }, Encoding.UTF8, false, "a"),
            new DecodingCase("Utf8ByteOrderMark", new byte[] { 0xEF, 0xBB, 0xBF, 0x61 }, Encoding.UTF8, false, "a"),
            // A declared encoding passes over its own byte order mark, even when it doesn't write one.
            new DecodingCase("DeclaredUtf8WithItsByteOrderMark", new byte[] { 0xEF, 0xBB, 0xBF, 0x61 }, Utf8WithoutPreamble, true, "a"),
            new DecodingCase("DeclaredUtf16WithItsByteOrderMark", new byte[] { 0xFF, 0xFE, 0x61, 0x00 }, Encoding.Unicode, true, "a"),
            // UTF-16 little endian's mark, then U+0000, looks like UTF-32 little endian's mark, but the declared encoding's own mark is passed over.
            new DecodingCase("DeclaredUtf16WithItsByteOrderMarkBeforeNull", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x61, 0x00 }, Encoding.Unicode, true,
                "\x0000a"),
            new DecodingCase("DeclaredUtf32WithItsByteOrderMark", new byte[] { 0xFF, 0xFE, 0x00, 0x00, 0x61, 0x00, 0x00, 0x00 }, Encoding.UTF32, true, "a"),
            // A byte that isn't valid after a run of bytes that are, with a character that straddles the end of the run.
            new DecodingCase("Utf8InvalidByteAfterLongRun", LongRunBytes, Encoding.UTF8, false, LongRunExpected),
            // A declared encoding wins over the byte order mark of another, which it reads as its own bytes.
            new DecodingCase("DeclaredUtf8WithUtf16ByteOrderMark", new byte[] { 0xFF, 0xFE, 0x61, 0x00 }, Utf8WithoutPreamble, true, "\xDCFF\xDCFEa\x0000"),
            // A stream shorter than any byte order mark.
            new DecodingCase("ShorterThanByteOrderMark", new byte[] { 0x61 }, Encoding.UTF8, false, "a"),
            new DecodingCase("Empty", new byte[0], Encoding.UTF8, false, string.Empty),
        };

        private static readonly ByteOrderMarkCase[] ByteOrderMarkCases = new ByteOrderMarkCase[]
        {
            new ByteOrderMarkCase("Utf16LittleEndianByteOrderMark", new byte[] { 0xFF, 0xFE, 0x61, 0x00 }, false, "utf-16"),
            new ByteOrderMarkCase("Utf32BigEndianByteOrderMark", new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0x00, 0x00, 0x00, 0x61 }, false, "utf-32BE"),
            new ByteOrderMarkCase("NoByteOrderMark", new byte[] { 0x61 }, false, "utf-8"),
            new ByteOrderMarkCase("DeclaredUtf8WithUtf16ByteOrderMark", new byte[] { 0xFF, 0xFE, 0x61, 0x00 }, true, "utf-8"),
        };

        public static TheoryData<DecodingCase, int, bool> DecodingCases()
        {
            var data = new TheoryData<DecodingCase, int, bool>();
            foreach (var decodingCase in Cases)
            {
                foreach (var blockSize in BlockSizes)
                {
                    foreach (var escapesByException in EscapesByException)
                    {
                        data.Add(decodingCase, blockSize, escapesByException);
                    }
                }
            }

            return data;
        }

        public static TheoryData<ByteOrderMarkCase> CurrentEncodingCases()
        {
            var data = new TheoryData<ByteOrderMarkCase>();
            foreach (var byteOrderMarkCase in ByteOrderMarkCases)
            {
                data.Add(byteOrderMarkCase);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(DecodingCases))]
        public void ReadToEnd_Bytes_ReadsCharactersAndEscapesBytesItCantDecode(DecodingCase decodingCase, int blockSize, bool escapesByException)
        {
            // arrange
            using (var testReader = new CsvStreamTextReader(new MemoryStream(decodingCase.Bytes), decodingCase.Encoding, decodingCase.IsEncodingDeclared,
                blockSize, escapesByException))
            {

                // act
                var testResult = testReader.ReadToEnd();

                // assert
                testResult.Should().Be(decodingCase.Expected);
            }
        }

        [Theory]
        [MemberData(nameof(CurrentEncodingCases))]
        public void CurrentEncoding_AfterReadingStart_IsEncodingByteOrderMarkNamesUnlessDeclared(ByteOrderMarkCase byteOrderMarkCase)
        {
            // arrange
            using (var testReader = new CsvStreamTextReader(new MemoryStream(byteOrderMarkCase.Bytes), Encoding.UTF8, byteOrderMarkCase.IsEncodingDeclared,
                BlockSizes[0]))
            {

                // act
                _ = testReader.ReadToEnd();
                var testResult = testReader.CurrentEncoding;

                // assert
                testResult.WebName.Should().Be(byteOrderMarkCase.ExpectedWebName);
            }
        }

        [Fact]
        public void Read_InvalidByte_ReturnsEscapedByte()
        {
            // arrange
            // 0xFF is never in UTF-8.
            const int EscapedInvalidByte = 0xDCFF;
            using (var testReader = new CsvStreamTextReader(new MemoryStream(new byte[] { 0xFF }), Encoding.UTF8, false, BlockSizes[0]))
            {

                // act
                var testResult = testReader.Read();

                // assert
                testResult.Should().Be(EscapedInvalidByte);
            }
        }

        [Fact]
        public void Peek_InvalidByte_ReturnsEscapedByte()
        {
            // arrange
            // 0xFF is never in UTF-8.
            const int EscapedInvalidByte = 0xDCFF;
            using (var testReader = new CsvStreamTextReader(new MemoryStream(new byte[] { 0xFF }), Encoding.UTF8, false, BlockSizes[0]))
            {

                // act
                var testResult = testReader.Peek();

                // assert
                testResult.Should().Be(EscapedInvalidByte);
            }
        }

        [Fact]
        public void Read_EndOfStream_ReturnsMinusOne()
        {
            // arrange
            const int EndOfStream = -1;
            using (var testReader = new CsvStreamTextReader(new MemoryStream(new byte[0]), Encoding.UTF8, false, BlockSizes[0]))
            {

                // act
                var testResult = testReader.Read();

                // assert
                testResult.Should().Be(EndOfStream);
            }
        }

        /// <summary>
        /// A stream's bytes, the encoding the reader is created with, whether it is declared, and the characters the reader reads.
        /// </summary>
        public sealed class DecodingCase
        {
            internal DecodingCase(string name, byte[] bytes, Encoding encoding, bool isEncodingDeclared, string expected)
            {
                Name = name;
                Bytes = bytes;
                Encoding = encoding;
                IsEncodingDeclared = isEncodingDeclared;
                Expected = expected;
            }

            internal string Name { get; }

            internal byte[] Bytes { get; }

            internal Encoding Encoding { get; }

            internal bool IsEncodingDeclared { get; }

            internal string Expected { get; }

            public override string ToString() => Name;
        }

        /// <summary>
        /// A stream's bytes, whether UTF-8 is declared for it, and the web name of the encoding the reader decodes it in.
        /// </summary>
        public sealed class ByteOrderMarkCase
        {
            internal ByteOrderMarkCase(string name, byte[] bytes, bool isEncodingDeclared, string expectedWebName)
            {
                Name = name;
                Bytes = bytes;
                IsEncodingDeclared = isEncodingDeclared;
                ExpectedWebName = expectedWebName;
            }

            internal byte[] Bytes { get; }

            internal bool IsEncodingDeclared { get; }

            internal string ExpectedWebName { get; }

            internal string Name { get; }

            public override string ToString() => Name;
        }
    }
}
