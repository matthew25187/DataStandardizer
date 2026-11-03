using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfMessageIdCidUriTests
    {
        // ref. RFC 2392 §2: the URI of its example, and the identifier that its conversion rule gives, with the escape converted.
        private const string ExampleUri = "cid:foo4%25foo1@bar.net";
        private const string ExampleMessageId = "<foo4%foo1@bar.net>";

        public static TheoryData<string, string> ValidUris => new TheoryData<string, string>
        {
            { ExampleUri, ExampleMessageId },
            { "CID:a@b", "<a@b>" },
            { "cid:a@b.c", "<a@b.c>" },
            { "cid:a%2fb@c", "<a/b@c>" },
            { "cid:a/b@c", "<a/b@c>" },
            { "cid:%22a%20b%22@c", "<\"a b\"@c>" },
            { "cid:a@%5Bb%5D", "<a@[b]>" },
            { "cid:%7E@b", "<~@b>" },
        };

        public static TheoryData<string> InvalidUris => new TheoryData<string>
        {
            "",
            "cid",
            "cid:",
            "a@b",
            "mid:a@b",
            "cid:a",
            "cid:<a@b>",
            "cid:a%@b",
            "cid:a%2@b",
            "cid:a%GG@b",
            "cid:a@b%",

            // ref. RFC 2392 §2: the escapes stand for US-ASCII characters.
            "cid:%C3%A9@b",
            "cid:\x00E9@b",

            // ref. RFC 1738 §2.2: these must be escaped.
            "cid:a b@c",
            "cid:a~b@c",
            "cid:a#b@c",
            "cid:\"a\"@b",
            "cid:a@[b]",

            // An escape can't make it something other than one identifier.
            "cid:a@b%3E",
            "cid:a@b%3E,%3Cc@d",
            "cid:a@b%3E%20%3Cc@d",
        };

        public static TheoryData<string, string> Formats => new TheoryData<string, string>
        {
            { ExampleMessageId, ExampleUri },
            { "<a@b.c>", "cid:a@b.c" },
            { "<a/b@c>", "cid:a%2Fb@c" },
            { "<a=b?c&d@e>", "cid:a%3Db%3Fc%26d@e" },
            { "<~#@b>", "cid:%7E%23@b" },
            { "<\"a b\"@c>", "cid:%22a%20b%22@c" },
            { "<a@[b]>", "cid:a@%5Bb%5D" },
        };

        [Theory]
        [MemberData(nameof(ValidUris))]
        public void ParseCidUri_ValidUri_GivesTheIdentifier(string value, string expected)
        {
            // act
            var testResult = ImfMessageId.ParseCidUri(value);

            // assert
            testResult.ToString().Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(InvalidUris))]
        public void ParseCidUri_InvalidUri_Throws(string value)
        {
            // act
            Action testAction = () => ImfMessageId.ParseCidUri(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ParseCidUri_Null_Throws()
        {
            // act
            Action testAction = () => ImfMessageId.ParseCidUri(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryParseCidUri_Null_ReturnsFalse()
        {
            // act
            var testResult = ImfMessageId.TryParseCidUri(null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ToCidUri_Identifier_EscapesWhatAUrlCantHold(string value, string expected)
        {
            // arrange
            var messageId = ImfMessageId.Parse(value);

            // act
            var testResult = messageId.ToCidUri();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToCidUri_NonAsciiIdentifier_Throws()
        {
            // arrange
            var messageId = ImfMessageId.Parse("<\x00E9@b>");

            // act
            Action testAction = () => messageId.ToCidUri();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ParseCidUri_UriFromAnIdentifier_GivesTheSameIdentifier(string value, string uri)
        {
            // arrange
            var expected = ImfMessageId.Parse(value);

            // act
            var testResult = ImfMessageId.ParseCidUri(uri);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Parse_ContentIdWrittenByAWriter_GivesTheSameIdentifier()
        {
            // arrange
            var written = MimeFieldRoundTrips.WriteAndRead("Content-ID", ExampleMessageId);

            // act
            var testResult = ImfMessageId.Parse(written);

            // assert
            testResult.ToString().Should().Be(ExampleMessageId);
        }
    }
}
