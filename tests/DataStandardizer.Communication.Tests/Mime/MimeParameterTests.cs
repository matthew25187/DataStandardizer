using System;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeParameterTests
    {
        [Theory]
        [InlineData("charset", "us-ascii", "charset=us-ascii")]
        [InlineData("name", "", "name=\"\"")]
        [InlineData("name", "two words", "name=\"two words\"")]
        [InlineData("name", "a\"b\\c", "name=\"a\\\"b\\\\c\"")]
        [InlineData("name", "a\tb", "name=\"a\tb\"")]
        [InlineData("name", "a=b", "name=\"a=b\"")]
        [InlineData("name", "a~b*c", "name=a~b*c")]
        public void ToString_QuotesValuesThatAreNotTokens(string name, string value, string expected)
        {
            var parameter = new MimeParameter(name, value);

            parameter.ToString().Should().Be(expected);

            var buffer = new char[expected.Length];
            int charsWritten;
            parameter.TryFormat(buffer, out charsWritten).Should().BeTrue();
            new string(buffer, 0, charsWritten).Should().Be(expected);
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            var parameter = new MimeParameter("name", "two words");

            int charsWritten;
            parameter.TryFormat(new char[parameter.ToString().Length - 1], out charsWritten).Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Theory]
        [InlineData("")]
        [InlineData("a b")]
        [InlineData("a*")]
        [InlineData("-a")]
        [InlineData("a=b")]
        public void Constructor_InvalidName_ThrowsArgumentException(string name)
        {
            Action act = () => new MimeParameter(name, "value");

            act.Should().Throw<ArgumentException>();
        }

        public static TheoryData<string> UnpairedSurrogates => new TheoryData<string> { "a\xD800", "\xDC00a", "\xDC00\xD800" };

        // Not enumerated at discovery, as the test runner may not keep unpaired surrogates when it serializes the data.
        [Theory]
        [MemberData(nameof(UnpairedSurrogates), DisableDiscoveryEnumeration = true)]
        public void Constructor_UnpairedSurrogate_ThrowsArgumentException(string value)
        {
            // act
            Action testAction = () => new MimeParameter("name", value);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullArgument_ThrowsArgumentNullException()
        {
            ((Action)(() => new MimeParameter(null, "value"))).Should().Throw<ArgumentNullException>();
            ((Action)(() => new MimeParameter("name", null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void CharsetAndLanguage_AreEmpty()
        {
            var parameter = new MimeParameter("title", "value");

            parameter.Charset.Should().BeEmpty();
            parameter.Language.Should().BeEmpty();
        }

        [Theory]
        [InlineData("name", "value", "NAME", "value", true)]
        [InlineData("name", "value", "name", "VALUE", false)]
        [InlineData("charset", "us-ascii", "Charset", "US-ASCII", true)]
        [InlineData("name", "value", "other", "value", false)]
        public void Equals_ComparesNamesCaseInsensitivelyAndValuesByParameter(string leftName, string leftValue, string rightName, string rightValue, bool expected)
        {
            var left = new MimeParameter(leftName, leftValue);
            var right = new MimeParameter(rightName, rightValue);

            left.Equals(right).Should().Be(expected);
            (left == right).Should().Be(expected);
            (left != right).Should().Be(!expected);
            left.Equals((object)right).Should().Be(expected);
            if (expected) left.GetHashCode().Should().Be(right.GetHashCode());
        }

        // ref. RFC 5322 §2.1.1: lines should be no more than 78 characters.
        private const int RecommendedLineLength = 78;

        // Any destination will do, as an uninitialised parameter throws before it writes.
        private const int AnyDestinationLength = 10;

        public static TheoryData<Action> MembersOfDefaultInstance => new TheoryData<Action>
        {
            () => _ = default(MimeParameter).Name,
            () => _ = default(MimeParameter).Value,
            () => _ = default(MimeParameter).Charset,
            () => _ = default(MimeParameter).Language,
            () => default(MimeParameter).FormatSections(RecommendedLineLength),
            () => default(MimeParameter).TryFormat(new char[AnyDestinationLength], out _),
        };

        [Theory]
        [MemberData(nameof(MembersOfDefaultInstance))]
        public void Members_DefaultInstance_ThrowInvalidOperationException(Action member)
        {
            // act
            var testAction = member;

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ToString_DefaultInstance_ReturnsEmpty()
        {
            // arrange
            var parameter = default(MimeParameter);

            // act
            var result = parameter.ToString();

            // assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void Equals_DefaultInstances_ReturnsTrue()
        {
            // arrange
            var parameter = default(MimeParameter);

            // act
            var result = parameter.Equals(default(MimeParameter));

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_DefaultInstance_ReturnsZero()
        {
            // arrange
            var parameter = default(MimeParameter);

            // act
            var result = parameter.GetHashCode();

            // assert
            result.Should().Be(0);
        }
    }
}
