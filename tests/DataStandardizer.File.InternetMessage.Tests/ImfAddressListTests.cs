using System;
using System.Collections;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfAddressListTests
    {
        private const string SimpleMailbox = "a@b";
        public static TheoryData<string, string> Formats => new TheoryData<string, string>
        {
            { Rfc5322Examples.A11From, Rfc5322Examples.A11From },
            { Rfc5322Examples.A12To, Rfc5322Examples.A12To },
            { Rfc5322Examples.A12Cc, Rfc5322Examples.A12CcModern },
            { Rfc5322Examples.A13To, Rfc5322Examples.A13ToModern },
            { Rfc5322Examples.A13Cc, Rfc5322Examples.A13Cc },
            { Rfc5322Examples.A5From, Rfc5322Examples.A5FromModern },
            { Rfc5322Examples.A5To, Rfc5322Examples.A5ToModern },
            { Rfc5322Examples.A5Cc, Rfc5322Examples.A5CcModern },
            { Rfc5322Examples.A61To, Rfc5322Examples.A61ToModern },
            { Rfc5322Examples.A63To, Rfc5322Examples.A63ToModern },

            // ref. RFC 6854 §2: groups can be anywhere an address can, among mailboxes.
            { "a@b, c: d@e, f@g;, h <i@j>, k:;", "a@b, c: d@e, f@g;, h <i@j>, k:;" },

            // ref. RFC 5322 §4.4: obs-addr-list
            { ",a@b", "a@b" },
            { "a@b,", "a@b" },
            { ", (x) ,a@b, (y) ,, c@d, (z)", "a@b, c@d" },
            { ",a:;,", "a:;" },
        };

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            // ref. RFC 5322 §3.4: there has to be at least one address.
            "",
            " ",
            "(a)",
            ",",
            " , (a) , ",

            "a",
            "a@b c@d",
            "a@b;",
            "a@b, c",
            "a@b, c@",
            "a:;b@c",
            "a:b:;;",
            "(a@b",
            "a@b, \"c",
            "a@b\r\n",
        };

        public static TheoryData<string, string, bool> EqualityCases => new TheoryData<string, string, bool>
        {
            { "a@b", "a@b", true },
            { "a@b, c:;", ",\"a\"@B,, c:(d);", true },
            { "a@b, c@d", "c@d, a@b", false },
            { "a@b", "a@b, c@d", false },
            { "a@b", "c@d", false },
            { "a:;", "a@b", false },
        };

        [Theory]
        [MemberData(nameof(Formats))]
        public void ToString_Parsed_GivesTheModernForm(string value, string expected)
        {
            // arrange
            var subject = ImfAddressList.Parse(value);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Parse_MailboxesAndGroup_GivesThemInOrder()
        {
            // act
            var testResult = ImfAddressList.Parse("a@b, c:;, d <e@f>");

            // assert
            testResult.Should().Equal(new ImfMailbox("a", "b"), new ImfGroup("c", new ImfMailbox[0]), new ImfMailbox("d", "e", "f"));
        }

        [Fact]
        public void Parse_Bytes_GivesTheList()
        {
            // arrange
            var value = Octets.Of("a@b, Jos\x00C3\x00A9 <c@d>");

            // act
            var testResult = ImfAddressList.Parse(value);

            // assert
            testResult.Should().Equal(new ImfMailbox("a", "b"), new ImfMailbox("Jos\x00E9", "c", "d"));
        }

        [Fact]
        public void TryParse_Bytes_GivesTheList()
        {
            // arrange
            var value = Octets.Of("a@b");

            // act
            var testResult = ImfAddressList.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Equal(new ImfMailbox("a", "b"));
        }

        [Fact]
        public void TryParse_String_GivesTheList()
        {
            // act
            var testResult = ImfAddressList.TryParse("a@b", out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Equal(new ImfMailbox("a", "b"));
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfAddressList.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfAddressList.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Parse_InvalidBytes_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("a@b, \x00FF@c");

            // act
            Action testAction = () => ImfAddressList.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfAddressList.Parse(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfAddressList.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Count_TwoAddresses_IsTwo()
        {
            // arrange
            var subject = ImfAddressList.Parse("a@b, c:;");

            // act
            var testResult = subject.Count;

            // assert
            testResult.Should().Be(2);
        }

        [Fact]
        public void Indexer_Position_GivesTheAddress()
        {
            // arrange
            var subject = ImfAddressList.Parse("a@b, c@d");

            // act
            var testResult = subject[1];

            // assert
            testResult.Should().Be(new ImfMailbox("c", "d"));
        }

        [Fact]
        public void Indexer_PastTheEnd_ThrowsArgumentOutOfRangeException()
        {
            // arrange
            var subject = ImfAddressList.Parse(SimpleMailbox);

            // act
            Action testAction = () => { var address = subject[1]; };

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GetEnumerator_NonGeneric_GivesTheAddresses()
        {
            // arrange
            IEnumerable subject = ImfAddressList.Parse(SimpleMailbox);

            // act
            var enumerator = subject.GetEnumerator();
            var testResult = enumerator.MoveNext();
            var current = enumerator.Current;

            // assert
            testResult.Should().BeTrue();
            current.Should().Be(new ImfMailbox("a", "b"));
        }

        [Fact]
        public void Constructor_Addresses_KeepsThemInOrder()
        {
            // act
            var testResult = new ImfAddressList(new ImfAddress[] { new ImfMailbox("a", "b"), new ImfGroup("c", new ImfMailbox[0]) });

            // assert
            testResult.ToString().Should().Be("a@b, c:;");
        }

        [Fact]
        public void Constructor_ChangingTheSourceAfterwards_DoesNotChangeTheList()
        {
            // arrange
            var addresses = new ImfAddress[] { new ImfMailbox("a", "b") };
            var subject = new ImfAddressList(addresses);

            // act
            addresses[0] = new ImfMailbox("c", "d");

            // assert
            subject.Should().Equal(new ImfMailbox("a", "b"));
        }

        [Fact]
        public void Constructor_Null_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfAddressList(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("addresses");
        }

        [Fact]
        public void Constructor_NoAddresses_ThrowsArgumentException()
        {
            // act
            Action testAction = () => new ImfAddressList(new ImfAddress[0]);

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("addresses");
        }

        [Fact]
        public void Constructor_NullAddress_ThrowsArgumentException()
        {
            // act
            Action testAction = () => new ImfAddressList(new ImfAddress[] { null });

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("addresses");
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void Equals_List_ComparesItsAddressesInOrder(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfAddressList.Parse(left);
            var other = ImfAddressList.Parse(right);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void EqualityOperator_List_ComparesItsAddressesInOrder(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfAddressList.Parse(left);
            var other = ImfAddressList.Parse(right);

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void InequalityOperator_List_ComparesItsAddressesInOrder(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfAddressList.Parse(left);
            var other = ImfAddressList.Parse(right);

            // act
            var testResult = subject != other;

            // assert
            testResult.Should().Be(!expected);
        }

        [Fact]
        public void Equals_ObjectOfTheSameValue_ReturnsTrue()
        {
            // arrange
            var subject = ImfAddressList.Parse(SimpleMailbox);
            object other = ImfAddressList.Parse("a@B");

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            var subject = ImfAddressList.Parse(SimpleMailbox);

            // act
            var testResult = subject.Equals((ImfAddressList)null);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_EqualLists_AreTheSame()
        {
            // arrange
            var subject = ImfAddressList.Parse("a@b, c:;");
            var other = ImfAddressList.Parse("a@B, \"c\":;");

            // act
            var testResult = subject.GetHashCode();

            // assert
            testResult.Should().Be(other.GetHashCode());
        }
    }
}
