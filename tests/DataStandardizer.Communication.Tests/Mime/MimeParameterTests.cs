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

        [Theory]
        [InlineData("café")]
        [InlineData("a\rb")]
        [InlineData("a\nb")]
        [InlineData("a\0b")]
        [InlineData("a\u007Fb")]
        public void Constructor_ValueThatCannotBeWritten_ThrowsArgumentException(string value)
        {
            Action act = () => new MimeParameter("name", value);

            act.Should().Throw<ArgumentException>();
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

        [Fact]
        public void DefaultInstance_IsUninitialised()
        {
            var parameter = default(MimeParameter);

            ((Action)(() => _ = parameter.Name)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = parameter.Value)).Should().Throw<InvalidOperationException>();
            ((Action)(() => parameter.TryFormat(new char[10], out _))).Should().Throw<InvalidOperationException>();
            parameter.ToString().Should().BeEmpty();
            parameter.Equals(default(MimeParameter)).Should().BeTrue();
            parameter.GetHashCode().Should().Be(0);
        }
    }
}
