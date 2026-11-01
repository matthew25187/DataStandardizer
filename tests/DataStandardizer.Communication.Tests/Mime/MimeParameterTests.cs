using System;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeParameterTests
    {
        // ref. RFC 2045 §5.1: a parameter name, and values that are and aren't tokens.
        private const string Name = "name";
        private const string Value = "value";
        private const string ValueThatIsNotAToken = "two words";
        private const string QuotedValueThatIsNotAToken = "name=\"two words\"";

        // ref. RFC 2046 §4.1.2: the charset parameter of text/plain.
        private const string CharsetName = "charset";
        private const string UsAscii = "us-ascii";

        // One character too few for QuotedValueThatIsNotAToken.
        private const int OneTooFewForQuotedValue = 15;

        public static TheoryData<string, string, string> FormattedParameters => new TheoryData<string, string, string>
        {
            { CharsetName, UsAscii, "charset=us-ascii" },
            { Name, "", "name=\"\"" },
            { Name, ValueThatIsNotAToken, QuotedValueThatIsNotAToken },
            { Name, "a\"b\\c", "name=\"a\\\"b\\\\c\"" },
            { Name, "a\tb", "name=\"a\tb\"" },
            { Name, "a=b", "name=\"a=b\"" },
            { Name, "a~b*c", "name=a~b*c" },
        };

        [Theory]
        [MemberData(nameof(FormattedParameters))]
        public void ToString_Value_QuotesValuesThatAreNotTokens(string name, string value, string expected)
        {
            // arrange
            var parameter = new MimeParameter(name, value);

            // act
            var testResult = parameter.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(FormattedParameters))]
        public void TryFormat_Value_QuotesValuesThatAreNotTokens(string name, string value, string expected)
        {
            // arrange
            var parameter = new MimeParameter(name, value);
            var destination = new char[expected.Length];

            // act
            var testResult = parameter.TryFormat(destination, out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(expected.Length);
            new string(destination).Should().Be(expected);
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            // arrange
            var parameter = new MimeParameter(Name, ValueThatIsNotAToken);

            // act
            var testResult = parameter.TryFormat(new char[OneTooFewForQuotedValue], out var charsWritten);

            // assert
            testResult.Should().BeFalse();
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
            // act
            Action testAction = () => _ = new MimeParameter(name, Value);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        public static TheoryData<string> UnpairedSurrogates => new TheoryData<string> { "a\xD800", "\xDC00a", "\xDC00\xD800" };

        // Not enumerated at discovery, as the test runner may not keep unpaired surrogates when it serializes the data.
        [Theory]
        [MemberData(nameof(UnpairedSurrogates), DisableDiscoveryEnumeration = true)]
        public void Constructor_UnpairedSurrogate_ThrowsArgumentException(string value)
        {
            // act
            Action testAction = () => _ = new MimeParameter(Name, value);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        public static TheoryData<Action> ConstructorsWithNullArgument => new TheoryData<Action>
        {
            () => _ = new MimeParameter(null, Value),
            () => _ = new MimeParameter(Name, null),
        };

        [Theory]
        [MemberData(nameof(ConstructorsWithNullArgument))]
        public void Constructor_NullArgument_ThrowsArgumentNullException(Action constructor)
        {
            // act
            var testAction = constructor;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_NameAndValue_HasNoCharsetOrLanguage()
        {
            // act
            var testResult = new MimeParameter("title", Value);

            // assert
            testResult.Charset.Should().BeEmpty();
            testResult.Language.Should().BeEmpty();
        }

        // Pairs of parameters that are equal: names are compared without regard to case, and a charset value is too.
        public static TheoryData<string, string, string, string> EqualParameters => new TheoryData<string, string, string, string>
        {
            { Name, Value, "NAME", Value },
            { CharsetName, UsAscii, "Charset", "US-ASCII" },
        };

        // Pairs of parameters that aren't equal: most values are compared with regard to case, and the names differ.
        public static TheoryData<string, string, string, string> UnequalParameters => new TheoryData<string, string, string, string>
        {
            { Name, Value, Name, "VALUE" },
            { Name, Value, "other", Value },
        };

        [Theory]
        [MemberData(nameof(EqualParameters))]
        public void Equals_EqualParameters_ReturnsTrue(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            var right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left.Equals(right);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EqualParameters))]
        public void EqualsObject_EqualParameters_ReturnsTrue(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            object right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left.Equals(right);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EqualParameters))]
        public void EqualityOperator_EqualParameters_ReturnsTrue(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            var right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left == right;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EqualParameters))]
        public void InequalityOperator_EqualParameters_ReturnsFalse(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            var right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left != right;

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(EqualParameters))]
        public void GetHashCode_EqualParameters_ReturnsSameHashCode(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            var rightHashCode = new MimeParameter(rightName, rightValue).GetHashCode();

            // act
            var testResult = left.GetHashCode();

            // assert
            testResult.Should().Be(rightHashCode);
        }

        [Theory]
        [MemberData(nameof(UnequalParameters))]
        public void Equals_UnequalParameters_ReturnsFalse(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            var right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left.Equals(right);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(UnequalParameters))]
        public void EqualsObject_UnequalParameters_ReturnsFalse(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            object right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left.Equals(right);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(UnequalParameters))]
        public void EqualityOperator_UnequalParameters_ReturnsFalse(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            var right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left == right;

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(UnequalParameters))]
        public void InequalityOperator_UnequalParameters_ReturnsTrue(string leftName, string leftValue, string rightName, string rightValue)
        {
            // arrange
            var left = new MimeParameter(leftName, leftValue);
            var right = new MimeParameter(rightName, rightValue);

            // act
            var testResult = left != right;

            // assert
            testResult.Should().BeTrue();
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
