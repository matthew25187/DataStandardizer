// AssemblyLoadContext is only on .NET, and the netstandard1.1 build behaves the same whatever runtime loads it.
#if NETCOREAPP
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests.Mime
{
    /// <summary>
    /// Tests of the <see cref="NetStandard11Build"/> helpers that call the netstandard1.1 build's parsers.
    /// </summary>
    public class NetStandard11BuildTests
    {
        // The names of MimeEncodedWordParseOptions and MimeMediaTypeParseOptions members.
        private const string NoOptions = "None";
        private const string AllowLowercaseHexDigits = "AllowLowercaseHexDigits";

        // ref. RFC 2047 §4.2: "café" in UTF-8, Q-encoded with upper-case and with lower-case hexadecimal digits.
        private const string EncodedWord = "=?UTF-8?Q?caf=C3=A9?=";
        private const string LowercaseHexEncodedWord = "=?UTF-8?Q?caf=c3=a9?=";
        private const string EncodedWordText = "caf\xE9";

        [Fact]
        public void ParseFirstParameterValue_MediaTypeWithParameters_ReturnsFirstValue()
        {
            // act
            var testResult = NetStandard11Build.ParseFirstParameterValue("text/plain;a=b;c=d", NoOptions);

            // assert
            testResult.Should().Be("b");
        }

        [Fact]
        public void ParseFirstParameterValue_MediaTypeWithoutParameters_ReturnsNull()
        {
            // act
            var testResult = NetStandard11Build.ParseFirstParameterValue("text/plain", NoOptions);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ParseFirstParameterValue_InvalidMediaType_ReturnsNull()
        {
            // act
            var testResult = NetStandard11Build.ParseFirstParameterValue("text", NoOptions);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void DecodeText_EncodedWord_ReturnsText()
        {
            // act
            var testResult = NetStandard11Build.DecodeText(EncodedWord, NoOptions);

            // assert
            testResult.Should().Be(EncodedWordText);
        }

        [Fact]
        public void DecodeText_LowercaseHexWithoutOption_LeavesWordAsWritten()
        {
            // act
            var testResult = NetStandard11Build.DecodeText(LowercaseHexEncodedWord, NoOptions);

            // assert
            testResult.Should().Be(LowercaseHexEncodedWord);
        }

        [Fact]
        public void DecodeText_LowercaseHexWithOption_ReturnsText()
        {
            // act
            var testResult = NetStandard11Build.DecodeText(LowercaseHexEncodedWord, AllowLowercaseHexDigits);

            // assert
            testResult.Should().Be(EncodedWordText);
        }
    }
}
#endif
