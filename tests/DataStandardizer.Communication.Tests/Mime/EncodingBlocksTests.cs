using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    /// <summary>
    /// Tests of <see cref="EncodingBlocks.DecodeAfterHeldBytes(Encoding, byte[], byte[])"/>.
    /// </summary>
    public class EncodingBlocksTests
    {
        // ref. RFC 3629 §3: U+00E9 is C3 A9 in UTF-8, so C3 alone is the start of a character that a decoder holds back.
        private static readonly byte[] Utf8LeadByte = { 0xC3 };
        private static readonly byte[] Utf8TrailByte = { 0xA9 };
        private const string Utf8Character = "\xE9";

        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER, which a flush reads an unfinished character as.
        private const string ReplacementCharacter = "\xFFFD";

        [Fact]
        public void DecodeAfterHeldBytes_CharacterFinishedByBlock_ReturnsCharacter()
        {
            // arrange
            var encoding = new UTF8Encoding(false);

            // act
            var testResult = EncodingBlocks.DecodeAfterHeldBytes(encoding, Utf8LeadByte, Utf8TrailByte);

            // assert
            testResult.Should().Be(Utf8Character);
        }

        [Fact]
        public void DecodeAfterHeldBytes_EmptyBlock_FlushesHeldBytes()
        {
            // arrange
            var encoding = new UTF8Encoding(false);

            // act
            var testResult = EncodingBlocks.DecodeAfterHeldBytes(encoding, Utf8LeadByte, new byte[0]);

            // assert
            testResult.Should().Be(ReplacementCharacter);
        }
    }
}
