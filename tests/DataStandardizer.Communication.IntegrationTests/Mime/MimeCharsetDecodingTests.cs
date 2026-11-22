using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests.Mime
{
    /// <summary>
    /// Tests of how encoded words and RFC 2231 parameter values in the runtime's character sets are decoded. NetStandard11DecodingTests
    /// gives the netstandard1.1 build the same inputs, and expects the same results.
    /// </summary>
    /// <remarks>
    /// They need the runtime's code pages, which come from outside the project, so they are integration tests; see
    /// <see cref="CodePageProvider"/>.
    /// </remarks>
    public class MimeCharsetDecodingTests
    {
        // ref. RFC 1468: ESC $ B switches ISO-2022-JP to JIS X 0208-1983, where 0x306C is U+4E00, and ESC ( B switches it back to ASCII.
        // The first word is 1B 24 42 30 6C, which stays in JIS X 0208 mode; the second is 1B 24 42 30 6C 1B 28 42.
        private const string Iso2022JpWordEndingInJisMode = "=?ISO-2022-JP?B?GyRCMGw=?=";
        private const string Iso2022JpWordEndingInAsciiMode = "=?ISO-2022-JP?B?GyRCMGwbKEI=?=";
        private const string Iso2022JpWordText = "\x4E00";

        // ref. RFC 1468: ESC $ @ switches ISO-2022-JP to JIS C 6226-1978, where 0x467C 0x4B5C is U+65E5 U+672C, which ISO-2022-JP writes
        // with ESC $ B instead.
        private const string Iso2022JpJisC6226Parameter = "t/s;n*=iso-2022-jp''%1B%24%40%46%7C%4B%5C%1B%28%42";
        private const string Iso2022JpJisC6226Text = "\x65E5\x672C";

        // ref. RFC 20 §2: 0xFF isn't a US-ASCII octet. "ascii" isn't one of the resolver's built-in names, so the runtime resolves it.
        private const string RuntimeAsciiInvalidOctetParameter = "t/s;a*=ascii''%FF";

        // 0x81 is the first octet of a Shift_JIS character, so it isn't a character by itself.
        private const string ShiftJisIncompleteCharacterParameter = "t/s;a*=shift_jis''%81";

        // ref. RFC 20 §2 and The Unicode Standard §3.9, D76: 0xFF isn't a US-ASCII octet, and 0x00110000 isn't a Unicode scalar value. These
        // are built-in names, which the netstandard1.1 build reads with its hand-coded encodings.
        private const string BuiltInAsciiInvalidOctetParameter = "t/s;a*=us-ascii''%FF";
        private const string BuiltInUtf32InvalidCodeUnitParameter = "t/s;a*=utf-32be''%00%11%00%00";

        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER.
        private const string ReplacementCharacter = "\xFFFD";

        [Fact]
        public void DecodeText_Iso2022JpWordEndingInJisMode_LeavesWordAsWritten()
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = MimeEncodedWord.DecodeText(Iso2022JpWordEndingInJisMode);

            // assert
            testResult.Should().Be(Iso2022JpWordEndingInJisMode);
        }

        [Fact]
        public void DecodeText_Iso2022JpWordEndingInJisModeWithAllowInvalidOctets_DecodesWord()
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = MimeEncodedWord.DecodeText(Iso2022JpWordEndingInJisMode, MimeEncodedWordContext.Text,
                MimeEncodedWordParseOptions.AllowInvalidOctets, MimeCharsetResolver.Default);

            // assert
            testResult.Should().Be(Iso2022JpWordText);
        }

        [Fact]
        public void DecodeText_Iso2022JpWordEndingInAsciiMode_DecodesWord()
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = MimeEncodedWord.DecodeText(Iso2022JpWordEndingInAsciiMode);

            // assert
            testResult.Should().Be(Iso2022JpWordText);
        }

        [Fact]
        public void TryParse_Iso2022JpValueInJisC6226_ReadsValue()
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = MimeMediaType.TryParse(Iso2022JpJisC6226Parameter, MimeMediaTypeParseOptions.None, out var mediaType);

            // assert
            testResult.Should().BeTrue();
            mediaType.Parameters[0].Value.Should().Be(Iso2022JpJisC6226Text);
        }

        [Theory]
        [InlineData(RuntimeAsciiInvalidOctetParameter)]
        [InlineData(ShiftJisIncompleteCharacterParameter)]
        [InlineData(BuiltInAsciiInvalidOctetParameter)]
        [InlineData(BuiltInUtf32InvalidCodeUnitParameter)]
        public void TryParse_InvalidOctetWithAllowInvalidOctets_ReadsReplacementCharacter(string value)
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowInvalidOctets, out var mediaType);

            // assert
            testResult.Should().BeTrue();
            mediaType.Parameters[0].Value.Should().Be(ReplacementCharacter);
        }

        [Theory]
        [InlineData(RuntimeAsciiInvalidOctetParameter)]
        [InlineData(ShiftJisIncompleteCharacterParameter)]
        [InlineData(BuiltInAsciiInvalidOctetParameter)]
        [InlineData(BuiltInUtf32InvalidCodeUnitParameter)]
        public void TryParse_InvalidOctet_ReturnsFalse(string value)
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.None, out _);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
