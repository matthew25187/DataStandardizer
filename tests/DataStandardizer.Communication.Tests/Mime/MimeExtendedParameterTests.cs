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

        // ref. RFC 3629 §3: FF is never a UTF-8 octet, and U+FFFD is EF BF BD in UTF-8.
        private static readonly byte[] InvalidUtf8Octets = { 0xFF };
        private static readonly byte[] ReplacementCharacterUtf8Octets = { 0xEF, 0xBF, 0xBD };

        public static TheoryData<byte[], string, bool> DecodedTexts => new TheoryData<byte[], string, bool>
        {
            { new byte[] { (byte)'a' }, "a", true },
            { InvalidUtf8Octets, ReplacementCharacter, false },
            { ReplacementCharacterUtf8Octets, ReplacementCharacter, true },
        };

        [Theory]
        [MemberData(nameof(DecodedTexts))]
        public void IsDecodedExactly_TextReadWithReplacementFallback_ReturnsWhetherOctetsWereValid(byte[] octets, string text, bool expected)
        {
            // act
            var result = MimeExtendedParameter.IsDecodedExactly(Encoding.UTF8, octets, octets.Length, text);

            // assert
            result.Should().Be(expected);
        }
    }
}
