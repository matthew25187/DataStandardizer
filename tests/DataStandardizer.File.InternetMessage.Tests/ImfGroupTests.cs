using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfGroupTests
    {
        private const string EncodedWord = "=?utf-8?Q?a?=";

        public static TheoryData<string, string, ImfMailbox[]> Parts => new TheoryData<string, string, ImfMailbox[]>
        {
            // value, display name, mailboxes

            { Rfc5322Examples.A13To, "A Group", new[] { new ImfMailbox("Ed Jones", "c", "a.test"), new ImfMailbox("joe", "where.test"), new ImfMailbox("John", "jdoe", "one.test") } },
            { Rfc5322Examples.A13Cc, "Undisclosed recipients", new ImfMailbox[0] },
            { Rfc5322Examples.A5To, "A Group", new[] { new ImfMailbox("Chris Jones", "c", "public.example"), new ImfMailbox("joe", "example.org"), new ImfMailbox("John", "jdoe", "one.test") } },
            { Rfc5322Examples.A5Cc, "Hidden recipients", new ImfMailbox[0] },

            { "a:;", "a", new ImfMailbox[0] },
            { "a: ;", "a", new ImfMailbox[0] },
            { "a:(b);", "a", new ImfMailbox[0] },
            { "a:b@c;", "a", new[] { new ImfMailbox("b", "c") } },
            { "\"a,b\":c@d;", "a,b", new[] { new ImfMailbox("c", "d") } },
            { "\"\":;", "", new ImfMailbox[0] },

            // ref. RFC 6532 §3.2
            { "Jos\x00E9: a@b;", "Jos\x00E9", new[] { new ImfMailbox("a", "b") } },

            // ref. RFC 5322 §4.1: obs-phrase
            { "a. b:;", "a. b", new ImfMailbox[0] },

            // ref. RFC 5322 §4.4: obs-group-list
            { "a:,;", "a", new ImfMailbox[0] },
            { "a: , (b), ;", "a", new ImfMailbox[0] },

            // ref. RFC 5322 §4.4: obs-mbox-list
            { "a:,b@c;", "a", new[] { new ImfMailbox("b", "c") } },
            { "a:b@c,;", "a", new[] { new ImfMailbox("b", "c") } },
            { "a:b@c, ,d@e,;", "a", new[] { new ImfMailbox("b", "c"), new ImfMailbox("d", "e") } },

            // ref. RFC 5322 §4.4: obs-route
            { "a:<@b:c@d>;", "a", new[] { new ImfMailbox("c", "d") } },
        };

        public static TheoryData<string, string> Formats => new TheoryData<string, string>
        {
            { Rfc5322Examples.A13To, Rfc5322Examples.A13ToModern },
            { Rfc5322Examples.A13Cc, Rfc5322Examples.A13Cc },
            { Rfc5322Examples.A5To, Rfc5322Examples.A5ToModern },
            { Rfc5322Examples.A5Cc, Rfc5322Examples.A5CcModern },

            { "a:,;", "a:;" },
            { "\"\":;", "\"\":;" },
            { "\"a,b\":c@d;", "\"a,b\": c@d;" },
            { "a. b:;", "\"a. b\":;" },
            { "a:b@c, ,d@e,;", "a: b@c, d@e;" },
            { EncodedWord + ":;", EncodedWord + ":;" },
            { EncodedWord + " \"" + EncodedWord + "\":;", EncodedWord + " \"" + EncodedWord + "\":;" },
        };

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            "",
            "a",
            "a:",
            "a;",
            ":;",
            ".:;",
            "(a):;",
            "a:b;",
            "a:b@c",
            "a:b@c d@e;",
            "a:b@c;d",
            "a:b@c;,",
            "a:;;",
            "a:b:;",
            "a:b:c@d;;",
            "a:(;",
            "\"a:;",
            "a@b",
            "<a@b>",
            "a <b@c>",
            "a@b:;",
        };

        public static TheoryData<string, string, bool> EqualityCases => new TheoryData<string, string, bool>
        {
            { "a:;", "a:;", true },
            { "a:;", "\"a\":(b);", true },
            { "a:b@c;", "a: b@C;", true },
            { "a:;", "A:;", false },
            { "a:;", "b:;", false },
            { "a:b@c;", "a:;", false },
            { "a:b@c;", "a:d@c;", false },
            { "a:b@c,d@e;", "a:d@e,b@c;", false },
            { EncodedWord + ":;", "\"" + EncodedWord + "\":;", false },
        };

        [Theory]
        [MemberData(nameof(Parts))]
        public void Parse_String_GivesItsParts(string value, string expectedDisplayName, ImfMailbox[] expectedMailboxes)
        {
            // act
            var testResult = ImfGroup.Parse(value);

            // assert
            testResult.DisplayName.Should().Be(expectedDisplayName);
            testResult.Mailboxes.Should().Equal(expectedMailboxes);
        }

        [Theory]
        [MemberData(nameof(Parts))]
        public void TryParse_String_GivesItsParts(string value, string expectedDisplayName, ImfMailbox[] expectedMailboxes)
        {
            // act
            var testResult = ImfGroup.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.DisplayName.Should().Be(expectedDisplayName);
            result.Mailboxes.Should().Equal(expectedMailboxes);
        }

        [Fact]
        public void Parse_Bytes_GivesItsParts()
        {
            // arrange
            var value = Octets.Of("Jos\x00C3\x00A9: a@b;");

            // act
            var testResult = ImfGroup.Parse(value);

            // assert
            testResult.DisplayName.Should().Be("Jos\x00E9");
            testResult.Mailboxes.Should().Equal(new ImfMailbox("a", "b"));
        }

        [Fact]
        public void TryParse_Bytes_GivesItsParts()
        {
            // arrange
            var value = Octets.Of("c:;");

            // act
            var testResult = ImfGroup.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.DisplayName.Should().Be("c");
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfGroup.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfGroup.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Parse_InvalidBytes_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("\x00FF:;");

            // act
            Action testAction = () => ImfGroup.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfGroup.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfGroup.Parse(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ToString_Parsed_GivesTheModernForm(string value, string expected)
        {
            // arrange
            var subject = ImfGroup.Parse(value);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_Constructed_GivesTheModernForm()
        {
            // arrange
            var subject = new ImfGroup("A, B", new[] { new ImfMailbox("c", "d"), new ImfMailbox("e", "f", "g") });

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be("\"A, B\": c@d, e <f@g>;");
        }

        [Fact]
        public void Constructor_NoMailboxes_GivesAnEmptyGroup()
        {
            // act
            var testResult = new ImfGroup("a", new ImfMailbox[0]);

            // assert
            testResult.Mailboxes.Should().BeEmpty();
        }

        [Fact]
        public void Constructor_ChangingTheSourceAfterwards_DoesNotChangeTheGroup()
        {
            // arrange
            var mailboxes = new[] { new ImfMailbox("a", "b") };
            var subject = new ImfGroup("c", mailboxes);

            // act
            mailboxes[0] = new ImfMailbox("d", "e");

            // assert
            subject.Mailboxes.Should().Equal(new ImfMailbox("a", "b"));
        }

        [Fact]
        public void Constructor_NullDisplayName_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfGroup(null, new ImfMailbox[0]);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("displayName");
        }

        [Fact]
        public void Constructor_NullMailboxes_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new ImfGroup("a", null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("mailboxes");
        }

        [Fact]
        public void Constructor_NullMailbox_ThrowsArgumentException()
        {
            // act
            Action testAction = () => new ImfGroup("a", new ImfMailbox[] { null });

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("mailboxes");
        }

        [Theory]
        [InlineData("a\r\nBcc: b@c")]
        [InlineData("a\0")]
        public void Constructor_InvalidDisplayName_ThrowsArgumentException(string displayName)
        {
            // act
            Action testAction = () => new ImfGroup(displayName, new ImfMailbox[0]);

            // assert
            testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("displayName");
        }

        [Fact]
        public void Mailboxes_CastToAList_CannotBeChanged()
        {
            // arrange
            var subject = ImfGroup.Parse("a:b@c;");
            var mailboxes = (IList<ImfMailbox>)subject.Mailboxes;

            // act
            Action testAction = () => mailboxes[0] = new ImfMailbox("d", "e");

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void Equals_Group_ComparesItsValue(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfGroup.Parse(left);
            var other = ImfGroup.Parse(right);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void GetHashCode_EqualGroups_AreTheSame()
        {
            // arrange
            var subject = ImfGroup.Parse("a:b@c;");
            var other = ImfGroup.Parse("\"a\":b@C;");

            // act
            var testResult = subject.GetHashCode();

            // assert
            testResult.Should().Be(other.GetHashCode());
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            var subject = ImfGroup.Parse("a:;");

            // act
            var testResult = subject.Equals((ImfGroup)null);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
