using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfAddressTests
    {
        public static TheoryData<string> Mailboxes => new TheoryData<string>
        {
            Rfc5322Examples.A11From,
            Rfc5322Examples.A5From,
            "a@b",
        };

        public static TheoryData<string> Groups => new TheoryData<string>
        {
            Rfc5322Examples.A13To,
            Rfc5322Examples.A13Cc,
            Rfc5322Examples.A5To,
        };

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            "",
            "a",
            "a@b,",
            "a@b, c@d",
            "a:;,",
        };

        [Theory]
        [MemberData(nameof(Mailboxes))]
        public void Parse_Mailbox_GivesAMailbox(string value)
        {
            // act
            var testResult = ImfAddress.Parse(value);

            // assert
            testResult.Should().BeOfType<ImfMailbox>();
        }

        [Theory]
        [MemberData(nameof(Groups))]
        public void Parse_Group_GivesAGroup(string value)
        {
            // act
            var testResult = ImfAddress.Parse(value);

            // assert
            testResult.Should().BeOfType<ImfGroup>();
        }

        [Fact]
        public void Parse_Bytes_GivesTheAddress()
        {
            // arrange
            var value = Octets.Of("a:;");

            // act
            var testResult = ImfAddress.Parse(value);

            // assert
            testResult.DisplayName.Should().Be("a");
        }

        [Fact]
        public void TryParse_Bytes_GivesTheAddress()
        {
            // arrange
            var value = Octets.Of("c <a@b>");

            // act
            var testResult = ImfAddress.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.DisplayName.Should().Be("c");
        }

        [Fact]
        public void TryParse_String_GivesTheAddress()
        {
            // act
            var testResult = ImfAddress.TryParse("c <a@b>", out var result);

            // assert
            testResult.Should().BeTrue();
            result.DisplayName.Should().Be("c");
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfAddress.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfAddress.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Parse_InvalidBytes_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("\x00FF@b");

            // act
            Action testAction = () => ImfAddress.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfAddress.Parse(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfAddress.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_MailboxAndGroup_ReturnsFalse()
        {
            // arrange
            var subject = ImfAddress.Parse("a@b");
            var other = ImfAddress.Parse("a:;");

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_GroupAndMailbox_ReturnsFalse()
        {
            // arrange
            var subject = ImfAddress.Parse("a:;");
            var other = ImfAddress.Parse("a@b");

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_AddressOfTheSameValue_ReturnsTrue()
        {
            // arrange
            var subject = ImfAddress.Parse("a:;");
            var other = ImfAddress.Parse("\"a\":;");

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void EqualityOperator_AddressesOfTheSameValue_ReturnsTrue()
        {
            // arrange
            var subject = ImfAddress.Parse("a@b");
            var other = ImfAddress.Parse("a@B");

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void InequalityOperator_AddressesOfDifferentValues_ReturnsTrue()
        {
            // arrange
            var subject = ImfAddress.Parse("a@b");
            var other = ImfAddress.Parse("c@d");

            // act
            var testResult = subject != other;

            // assert
            testResult.Should().BeTrue();
        }
    }
}
