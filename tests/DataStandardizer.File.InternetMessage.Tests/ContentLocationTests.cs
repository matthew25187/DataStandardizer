using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of <see cref="ImfEntity.ContentLocation"/> (RFC 2557 §4).
    /// </summary>
    public class ContentLocationTests
    {
        private const string FieldName = "Content-Location";
        private const string Url = "http://example.org/a";

        // 81 characters, which is too long for one line of 78.
        private const string LongUrl = "http://example.org/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        [Theory]
        [InlineData(" http://example.org/a", Url)]
        [InlineData(" (comment) http://example.org/a (another)", Url)]
        [InlineData(" http://example.org/a(b)", "http://example.org/a(b)")]
        // ref. RFC 2557 §4.4.2 and §4.4.3: a folded URI is one URI again.
        [InlineData(" http://example.org/ a", Url)]
        [InlineData(" images/ietflogo2.gif", "images/ietflogo2.gif")]
        [InlineData(" (a)(b) http://example.org/a (c) (d)", Url)]
        // A parenthesis is a character of a URL, so what isn't at the end isn't a comment.
        [InlineData(" http://example.org/a (b) c", "http://example.org/a(b)c")]
        [InlineData(" http://example.org/a ( (b) )c", "http://example.org/a((b))c")]
        [InlineData(" /images/ietflogo2.gif", "/images/ietflogo2.gif")]
        // ref. RFC 2557 §8.2 (a): "Do not transform "a%2eb/c%20d" into "a/b/c d"".
        [InlineData(" a%2eb/c%20d", "a%2eb/c%20d")]
        // ref. RFC 2557 §4.4.1: what a header can't hold is in an encoded word.
        [InlineData(" =?US-ASCII?Q?a=20b?=", "a b")]
        public void ContentLocation_ValidField_GivesTheUri(string body, string expected)
        {
            // arrange
            var subject = new ImfEntity();
            subject.Fields.Add(new ImfHeaderField(FieldName, body, ImfHeaderFieldKind.Structured));

            // act
            var testResult = subject.ContentLocation;

            // assert
            testResult.OriginalString.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" (only a comment)")]
        [InlineData(" (open http://example.org/a")]
        // ref. RFC 2557 §4.4.1: these have to be in an encoded word.
        [InlineData(" http://example.org/\"a\"")]
        [InlineData(" http://example.org/<a>")]
        [InlineData(" http://example.org/caf\x00E9")]
        // Not a URL at all: it has a scheme and nothing that a URI can parse.
        [InlineData(" http://")]
        // ref. RFC 1738 §3.3: an HTTP URL takes the form http://<host>.  RFC 2557 §9.6 prints this one, and §9 says that its examples are only illustrative.
        [InlineData(" http:images/ietflogo2.gif")]
        public void ContentLocation_InvalidField_IsNull(string body)
        {
            // arrange
            var subject = new ImfEntity();
            subject.Fields.Add(new ImfHeaderField(FieldName, body, ImfHeaderFieldKind.Structured));

            // act
            var testResult = subject.ContentLocation;

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void ContentLocation_EncodedWordInACharsetOnlyTheOptionsResolve_DecodesIt()
        {
            // arrange
            var subject = new ImfEntity(new ImfOptions { AllowMissingRequiredFields = true, CharsetResolver = new FixedCharsetResolver(Encoding.UTF8) });
            subject.Fields.Add(new ImfHeaderField(FieldName, " =?x-custom?Q?a=20b?=", ImfHeaderFieldKind.Structured));

            // act
            var testResult = subject.ContentLocation;

            // assert
            testResult.OriginalString.Should().Be("a b");
        }

        [Fact]
        public void ContentLocation_NoField_IsNull()
        {
            // arrange
            var subject = new ImfEntity();

            // act
            var testResult = subject.ContentLocation;

            // assert
            testResult.Should().BeNull();
        }

        [Theory]
        [InlineData(Url, UriKind.Absolute, Url)]
        [InlineData("a%2eb/c%20d", UriKind.Relative, "a%2eb/c%20d")]
        [InlineData("a b", UriKind.Relative, "=?UTF-8?Q?a_b?=")]
        public void ContentLocation_Set_FormatsTheFieldBody(string location, UriKind kind, string expected)
        {
            // arrange
            var subject = new ImfEntity { ContentLocation = new Uri(location, kind) };

            // act
            var testResult = subject.Fields[0].Value;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ContentLocation_SetLongUrl_FoldsAndReadsBackWhole()
        {
            // arrange
            var subject = new ImfEntity { ContentLocation = new Uri(LongUrl) };

            // act
            var testResult = subject.ContentLocation;

            // assert
            subject.Fields[0].Value.Should().Be("http://example.org/aaaaaaaaaaaaaaaaaaaaa aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa a");
            testResult.OriginalString.Should().Be(LongUrl);
        }

        [Theory]
        [InlineData("a b")]
        [InlineData("caf\x00E9.html")]
        [InlineData("a=?b")]
        public void ContentLocation_SetTextAHeaderCannotHold_ReadsBackTheSame(string location)
        {
            // arrange
            var subject = new ImfEntity { ContentLocation = new Uri(location, UriKind.Relative) };

            // act
            var testResult = subject.ContentLocation;

            // assert
            testResult.OriginalString.Should().Be(location);
        }

        [Fact]
        public void ContentLocation_SetNull_RemovesTheField()
        {
            // arrange
            var subject = new ImfEntity { ContentLocation = new Uri(Url) };

            // act
            subject.ContentLocation = null;

            // assert
            subject.Fields.Should().BeEmpty();
        }
    }
}
