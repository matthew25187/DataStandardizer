using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests the <see cref="RestrictedAsciiEncoding"/> test helper, which other tests rely on to stand in for a code page .NET doesn't know.
    /// </summary>
    public class RestrictedAsciiEncodingTests
    {
        private const char LeftSquareBracket = '[';
        private const string BracketedText = "a[b";
        private const string LatinSmallLetterEWithAcute = "\x00E9";

        [Fact]
        public void GetBytes_CharactersKept_EncodesEachAsItsValue()
        {
            // arrange
            var encoding = new RestrictedAsciiEncoding(LeftSquareBracket);

            // act
            var testResult = encoding.GetBytes("a]b");

            // assert
            testResult.Should().Equal(0x61, 0x5D, 0x62);
        }

        [Fact]
        public void GetBytes_MissingCharacter_EncodesQuestionMark()
        {
            // arrange
            var encoding = new RestrictedAsciiEncoding(LeftSquareBracket);

            // act
            var testResult = encoding.GetBytes(BracketedText);

            // assert
            testResult.Should().Equal(0x61, 0x3F, 0x62);
        }

        [Fact]
        public void GetBytes_CharacterOutsideAscii_EncodesQuestionMark()
        {
            // arrange
            var encoding = new RestrictedAsciiEncoding();

            // act
            var testResult = encoding.GetBytes(LatinSmallLetterEWithAcute);

            // assert
            testResult.Should().Equal(0x3F);
        }

        [Fact]
        public void GetString_BytesUpToAscii_DecodesEachAsItsValue()
        {
            // arrange
            var encoding = new RestrictedAsciiEncoding(LeftSquareBracket);

            // act
            var testResult = encoding.GetString(new byte[] { 0x61, 0x5B, 0x62 });

            // assert
            testResult.Should().Be(BracketedText);
        }

        [Fact]
        public void GetString_ByteAboveAscii_DecodesQuestionMark()
        {
            // arrange
            var encoding = new RestrictedAsciiEncoding();

            // act
            var testResult = encoding.GetString(new byte[] { 0xE9 });

            // assert
            testResult.Should().Be("?");
        }

        [Fact]
        public void WebName_Get_ThrowsNotSupportedException()
        {
            // arrange
            Encoding encoding = new RestrictedAsciiEncoding();

            // act
            Action testAction = () => _ = encoding.WebName;

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }
    }
}
