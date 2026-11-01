using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeExtendedParameterTests
    {
        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER, which Encoding.UTF8 reads an invalid octet sequence as.
        private const string ReplacementCharacter = "\xFFFD";

        // ref. RFC 3629 §3 and RFC 20 §2: FF is neither a UTF-8 nor a US-ASCII octet, and U+FFFD is EF BF BD in UTF-8.
        private static readonly byte[] InvalidOctet = { 0xFF };
        private static readonly byte[] ReplacementCharacterUtf8Octets = { 0xEF, 0xBF, 0xBD };

        // ref. RFC 2781 §4.3: U+FFFD after a big-endian and a little-endian byte order mark, and an unpaired high surrogate (D800) after a
        // big-endian one.
        private static readonly byte[] BigEndianReplacementCharacter = { 0xFE, 0xFF, 0xFF, 0xFD };
        private static readonly byte[] LittleEndianReplacementCharacter = { 0xFF, 0xFE, 0xFD, 0xFF };
        private static readonly byte[] UnpairedHighSurrogate = { 0xFE, 0xFF, 0xD8, 0x00 };

        // US-ASCII encodings that read an invalid octet as '?', and as U+FFFD but can't write U+FFFD.
        private const string QuestionMark = "?";
        private static readonly Encoding QuestionMarkAscii = Encoding.GetEncoding(
            "us-ascii", EncoderFallback.ExceptionFallback, new DecoderReplacementFallback(QuestionMark));
        private static readonly Encoding ThrowingWriterAscii = Encoding.GetEncoding(
            "us-ascii", EncoderFallback.ExceptionFallback, new DecoderReplacementFallback(ReplacementCharacter));

        public static TheoryData<Encoding, byte[], string, bool> DecodedTexts => new TheoryData<Encoding, byte[], string, bool>
        {
            { Encoding.UTF8, new byte[] { (byte)'a' }, "a", true },
            { Encoding.UTF8, InvalidOctet, ReplacementCharacter, false },
            { Encoding.UTF8, ReplacementCharacterUtf8Octets, ReplacementCharacter, true },
            { QuestionMarkAscii, InvalidOctet, QuestionMark, false },
            { ThrowingWriterAscii, InvalidOctet, ReplacementCharacter, false },
            { MimeUnicodeSchemeEncoding.Utf16, BigEndianReplacementCharacter, ReplacementCharacter, true },
            { MimeUnicodeSchemeEncoding.Utf16, LittleEndianReplacementCharacter, ReplacementCharacter, true },
            { MimeUnicodeSchemeEncoding.Utf16, UnpairedHighSurrogate, ReplacementCharacter, false },
        };

        [Theory]
        [MemberData(nameof(DecodedTexts))]
        public void IsDecodedExactly_TextReadWithReplacementFallback_ReturnsWhetherOctetsWereValid(Encoding encoding, byte[] octets, string text,
            bool expected)
        {
            // act
            var result = MimeExtendedParameter.IsDecodedExactly(encoding, octets, octets.Length, text);

            // assert
            result.Should().Be(expected);
        }
    }
}
