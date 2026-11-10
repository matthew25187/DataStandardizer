using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfFieldCommentsTests
    {
        public static TheoryData<string, string> Values => new TheoryData<string, string>
        {
            { "", "" },
            { "text/plain", "text/plain" },
            { " \ttext/plain", "text/plain" },

            // ref. RFC 2045 §5.1: the example of a comment.
            { "text/plain; charset=us-ascii (Plain text)", "text/plain; charset=us-ascii  " },

            // ref. RFC 5322 §3.2.2: comments nest, and a quoted-pair can hide a parenthesis.
            { "a(b(c)d)e", "a e" },
            { "a(\\)b)c", "a c" },
            { "(a)b", "b" },

            // A comment or white space around the first slash goes.
            { "a / b", "a/b" },
            { "a\t/\tb", "a/b" },
            { "a (c) / (d) b", "a/b" },
            { "a/(d) b", "a/b" },

            // The slash of a later parameter is left alone.
            { "a/b; c=d / e", "a/b; c=d / e" },

            // A quoted string is left alone, with its parentheses, slashes and quoted-pairs.
            { "a=\"(b) / c\"", "a=\"(b) / c\"" },
            { "a=\"b\\\"(c)\"", "a=\"b\\\"(c)\"" },
        };

        public static TheoryData<string> Unclosed => new TheoryData<string>
        {
            "(a",
            "(a(b)",
            "(a\\)",
            "(a\\",
            "\"a",
            "\"a\\\"",
        };

        [Theory]
        [MemberData(nameof(Values))]
        public void TryRemove_Value_GivesItWithoutComments(string value, string expected)
        {
            // act
            var testResult = ImfFieldComments.TryRemove(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Unclosed))]
        public void TryRemove_SomethingNotClosed_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfFieldComments.TryRemove(value, out _);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
