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

        // ref. RFC 2781 §2: U+FFFD in UTF-16BE and UTF-16LE, and an unpaired high surrogate (D800) in UTF-16BE.
        private static readonly byte[] BigEndianReplacementCharacter = { 0xFF, 0xFD };
        private static readonly byte[] LittleEndianReplacementCharacter = { 0xFD, 0xFF };
        private static readonly byte[] UnpairedHighSurrogate = { 0xD8, 0x00 };

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
            { MimeUnicodeSchemeEncoding.Utf16.BigEndian, BigEndianReplacementCharacter, ReplacementCharacter, true },
            { MimeUnicodeSchemeEncoding.Utf16.LittleEndian, LittleEndianReplacementCharacter, ReplacementCharacter, true },
            { MimeUnicodeSchemeEncoding.Utf16.BigEndian, UnpairedHighSurrogate, ReplacementCharacter, false },
        };

        [Theory]
        [MemberData(nameof(DecodedTexts))]
        public void IsDecodedExactly_TextReadWithReplacementFallback_ReturnsWhetherOctetsWereValid(Encoding encoding, byte[] octets, string text,
            bool expected)
        {
            // act
            var result = MimeExtendedParameter.IsDecodedExactly(encoding, octets, 0, octets.Length, text);

            // assert
            result.Should().Be(expected);
        }
    }
}
