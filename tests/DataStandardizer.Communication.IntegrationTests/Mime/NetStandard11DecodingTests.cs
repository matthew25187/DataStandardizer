// AssemblyLoadContext is only on .NET, and the netstandard1.1 build behaves the same whatever runtime loads it.
#if NETCOREAPP
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests.Mime
{
    /// <summary>
    /// The tests of <see cref="MimeCharsetDecodingTests"/>, of the netstandard1.1 build, which has no <c>Encoding.Clone</c> or
    /// <c>DecoderFallback</c> to compile against, and expects the same results.
    /// </summary>
    /// <remarks>
    /// They load that build from the file system, and need the runtime's code pages, so they are integration tests; see
    /// <see cref="NetStandard11Build"/> and <see cref="CodePageProvider"/>.
    /// </remarks>
    public class NetStandard11DecodingTests
    {
        // ref. RFC 1468: ESC $ B switches ISO-2022-JP to JIS X 0208-1983, where 0x306C is U+4E00, and ESC ( B switches it back to ASCII.
        // The first word is 1B 24 42 30 6C, which stays in JIS X 0208 mode; the second is 1B 24 42 30 6C 1B 28 42.
        private const string Iso2022JpWordEndingInJisMode = "=?ISO-2022-JP?B?GyRCMGw=?=";
        private const string Iso2022JpWordEndingInAsciiMode = "=?ISO-2022-JP?B?GyRCMGwbKEI=?=";

        // ref. RFC 1468: ESC ( J switches ISO-2022-JP to JIS X 0201-Roman, which isn't ASCII: 1B 24 42 30 6C 1B 28 4A.
        private const string Iso2022JpWordEndingInJisRomanMode = "=?ISO-2022-JP?B?GyRCMGwbKEo=?=";

        // ref. RFC 1557: ESC $ ) C designates KS C 5601 to G1 in ISO-2022-KR, and SO shifts to it; the word, 1B 24 29 43 0E 30 21, has no SI
        // to shift back to ASCII.
        private const string Iso2022KrWordEndingShiftedOut = "=?ISO-2022-KR?B?GyQpQw4wIQ==?=";
        private const string Iso2022JpWordText = "\x4E00";

        // ref. RFC 1468: two U+4E00 (0x306C in JIS X 0208) with the second split between the words, 1B 24 42 30 6C 30 and 6C 1B 28 42, so
        // the first word ends in JIS X 0208 mode and the second starts in it.
        private const string Iso2022JpKanjiSplitInJisMode = "=?ISO-2022-JP?B?GyRCMGww?= =?ISO-2022-JP?B?bBsoQg==?=";
        private const string TwoIso2022JpKanji = "\x4E00\x4E00";

        // ref. RFC 1468: ESC $ B split between the words, 1B 24 and 42 30 6C 1B 28 42, which is U+4E00.
        private const string Iso2022JpEscapeSequenceSplit = "=?ISO-2022-JP?B?GyQ=?= =?ISO-2022-JP?B?QjBsGyhC?=";

        // ref. RFC 1557: in ISO-2022-KR, 0x3021 in KS C 5601 is U+AC00, split between the words, 1B 24 29 43 0E 30 and 21 0F, so the first
        // word ends shifted out to G1 and the second starts that way.
        private const string Iso2022KrHangulSplitShiftedOut = "=?ISO-2022-KR?B?GyQpQw4w?= =?ISO-2022-KR?B?IQ8=?=";
        private const string Iso2022KrHangul = "\xAC00";

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

        // The names of MimeEncodedWordParseOptions and MimeMediaTypeParseOptions members.
        private const string NoOptions = "None";
        private const string AllowInvalidOctets = "AllowInvalidOctets";
        private const string AllowSplitCharacters = "AllowSplitCharacters";

        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER.
        private const string ReplacementCharacter = "\xFFFD";

        [Theory]
        [InlineData(Iso2022JpWordEndingInJisMode)]
        [InlineData(Iso2022JpWordEndingInJisRomanMode)]
        [InlineData(Iso2022KrWordEndingShiftedOut)]
        public void DecodeText_WordNotEndingInAsciiMode_LeavesWordAsWritten(string word)
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = NetStandard11Build.DecodeText(word, NoOptions);

            // assert
            testResult.Should().Be(word);
        }

        [Theory]
        [InlineData(Iso2022JpWordEndingInJisMode)]
        [InlineData(Iso2022JpWordEndingInJisRomanMode)]
        [InlineData(Iso2022KrWordEndingShiftedOut)]
        public void DecodeText_WordNotEndingInAsciiModeWithAllowInvalidOctets_LeavesWordAsWritten(string word)
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = NetStandard11Build.DecodeText(word, AllowInvalidOctets);

            // assert
            testResult.Should().Be(word);
        }

        [Fact]
        public void DecodeText_Iso2022JpWordEndingInAsciiMode_DecodesWord()
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = NetStandard11Build.DecodeText(Iso2022JpWordEndingInAsciiMode, NoOptions);

            // assert
            testResult.Should().Be(Iso2022JpWordText);
        }

        [Theory]
        [InlineData(Iso2022JpKanjiSplitInJisMode, TwoIso2022JpKanji)]
        [InlineData(Iso2022JpEscapeSequenceSplit, Iso2022JpWordText)]
        [InlineData(Iso2022KrHangulSplitShiftedOut, Iso2022KrHangul)]
        public void DecodeText_Iso2022CharacterSplitWithAllowSplitCharacters_ReadsNextWordInSameMode(string value, string expected)
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = NetStandard11Build.DecodeText(value, AllowSplitCharacters);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void TryParse_Iso2022JpValueInJisC6226_ReadsValue()
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = NetStandard11Build.ParseFirstParameterValue(Iso2022JpJisC6226Parameter, NoOptions);

            // assert
            testResult.Should().Be(Iso2022JpJisC6226Text);
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
            var testResult = NetStandard11Build.ParseFirstParameterValue(value, AllowInvalidOctets);

            // assert
            testResult.Should().Be(ReplacementCharacter);
        }

        [Theory]
        [InlineData(RuntimeAsciiInvalidOctetParameter)]
        [InlineData(ShiftJisIncompleteCharacterParameter)]
        [InlineData(BuiltInAsciiInvalidOctetParameter)]
        [InlineData(BuiltInUtf32InvalidCodeUnitParameter)]
        public void TryParse_InvalidOctet_ReadsNoMediaType(string value)
        {
            // arrange
            CodePageProvider.Register();

            // act
            var testResult = NetStandard11Build.ParseFirstParameterValue(value, NoOptions);

            // assert
            testResult.Should().BeNull();
        }
    }
}
#endif
