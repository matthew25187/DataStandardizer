using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfMessageIdTests
    {
        private const string SimpleMessageId = "<a@b>";

        public static TheoryData<string, string, string> Parts => new TheoryData<string, string, string>
        {
            // value, id-left, id-right

            { Rfc5322Examples.A11MessageId, "1234", "local.machine.example" },
            { Rfc5322Examples.A12MessageId, "5678.21-Nov-1997", "example.com" },
            { "<a@[b]>", "a", "[b]" },
            { " (x) <a@b> (y) ", "a", "b" },

            // ref. RFC 6532 §3.2
            { "<\x00E9@\x00E4>", "\x00E9", "\x00E4" },

            // ref. RFC 5322 §4.5.4: obs-id-left and obs-id-right
            { "<\"a\"@b>", "a", "b" },
            { "<\"a b\"@c>", "a b", "c" },
            { "<a . b@c . d>", "a.b", "c.d" },
            { "<a(x)@(y)b>", "a", "b" },
            { "<a@[ b ]>", "a", "[b]" },
            { "<a@[b c]>", "a", "[b c]" },
        };

        public static TheoryData<string, string> Formats => new TheoryData<string, string>
        {
            { Rfc5322Examples.A11MessageId, Rfc5322Examples.A11MessageId },
            { Rfc5322Examples.A12MessageId, Rfc5322Examples.A12MessageId },
            { "<a@[b]>", "<a@[b]>" },
            { " (x) <a@b> (y) ", "<a@b>" },

            // ref. RFC 5322 §4.5.4: each obsolete form is written in the modern form.
            { "<\"a\"@b>", "<a@b>" },
            { "<a . b@c . d>", "<a.b@c.d>" },
            { "<a@[ b ]>", "<a@[b]>" },

            // These have no modern form.  Changing them would make them different identifiers, but a line break still never gets out.
            { "<\"a b\"@c>", "<\"a b\"@c>" },
            { "<a@[b c]>", "<a@[b c]>" },
            { "<\"a\\\r\\\nb\"@c>", "<\"ab\"@c>" },
        };

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            "",
            "a",
            "a@b",
            "<a>",
            "<a@b",
            "a@b>",
            "<>",
            "<@b>",
            "<a@>",
            "<@c:a@b>",
            "<a@b> <c@d>",
            "<a@b>,",
            "<a@b> c",
            "b <a@b>",
            "<a@\"b\">",
            "<a@b(>",
            "<a@b>\r\n",
        };

        public static TheoryData<string, string> Constructed => new TheoryData<string, string>
        {
            { "a", "b" },
            { "a.b", "c.d" },
            { "a", "[b]" },
            { "a", "[]" },
            { "\x00E9", "\x00E4" },
        };

        public static TheoryData<string, string> InvalidConstructorArguments => new TheoryData<string, string>
        {
            // ref. RFC 5322 §3.6.4: id-left = dot-atom-text
            { "", "b" },
            { "a b", "c" },
            { ".a", "b" },
            { "a.", "b" },
            { "a..b", "c" },
            { "a@b", "c" },
            { "a\r\nBcc: b@c", "d" },

            // ref. RFC 5322 §3.6.4: id-right = dot-atom-text / no-fold-literal
            { "a", "" },
            { "a", "b c" },
            { "a", "[b c]" },
            { "a", "[b" },
            { "a", "[b]c]" },
            { "a", "[\r\n b]" },
        };

        public static TheoryData<string, ImfMessageId[]> Lists => new TheoryData<string, ImfMessageId[]>
        {
            { Rfc5322Examples.A11MessageId, new[] { new ImfMessageId("1234", "local.machine.example") } },
            { Rfc5322Examples.A2References, new[] { new ImfMessageId("1234", "local.machine.example"), new ImfMessageId("3456", "example.net") } },
            { "<a@b><c@d>", new[] { new ImfMessageId("a", "b"), new ImfMessageId("c", "d") } },
            { "<a@b>\r\n <c@d>", new[] { new ImfMessageId("a", "b"), new ImfMessageId("c", "d") } },
            { "(x) <a@b> (y) <c@d> (z)", new[] { new ImfMessageId("a", "b"), new ImfMessageId("c", "d") } },

            // ref. RFC 5322 §4.5.4: obs-in-reply-to and obs-references have any number of phrases and identifiers.
            { "", new ImfMessageId[0] },
            { "a", new ImfMessageId[0] },
            { "\"a\" b. c", new ImfMessageId[0] },
            { "a <b@c> \"d\" <e@f> g", new[] { new ImfMessageId("b", "c"), new ImfMessageId("e", "f") } },
        };

        public static TheoryData<string> InvalidLists => new TheoryData<string>
        {
            "<a@b>,",
            "<a@b>, <c@d>",
            "<a>",
            "<a@b",
            ".",
            ". a",
            "a@b",
            "(a",
        };

        public static TheoryData<string, string, bool> EqualityCases => new TheoryData<string, string, bool>
        {
            { "<a@b>", "<a@b>", true },
            { "<\"a\"@b>", "<a(c)@b>", true },

            // ref. RFC 5322 §3.6.4: both parts are compared exactly, unlike the domain of a mailbox.
            { "<a@b>", "<A@b>", false },
            { "<a@b>", "<a@B>", false },
            { "<a@b>", "<c@b>", false },
            { "<a@b>", "<a@c>", false },
        };

        [Theory]
        [MemberData(nameof(Parts))]
        public void Parse_String_GivesItsParts(string value, string expectedIdLeft, string expectedIdRight)
        {
            // act
            var testResult = ImfMessageId.Parse(value);

            // assert
            testResult.IdLeft.Should().Be(expectedIdLeft);
            testResult.IdRight.Should().Be(expectedIdRight);
        }

        [Theory]
        [MemberData(nameof(Parts))]
        public void TryParse_String_GivesItsParts(string value, string expectedIdLeft, string expectedIdRight)
        {
            // act
            var testResult = ImfMessageId.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.IdLeft.Should().Be(expectedIdLeft);
            result.IdRight.Should().Be(expectedIdRight);
        }

        [Fact]
        public void Parse_Bytes_GivesItsParts()
        {
            // arrange
            var value = Octets.Of("<\x00C3\x00A9@b>");

            // act
            var testResult = ImfMessageId.Parse(value);

            // assert
            testResult.IdLeft.Should().Be("\x00E9");
            testResult.IdRight.Should().Be("b");
        }

        [Fact]
        public void TryParse_Bytes_GivesItsParts()
        {
            // arrange
            var value = Octets.Of("<a@b>");

            // act
            var testResult = ImfMessageId.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.IdLeft.Should().Be("a");
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfMessageId.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfMessageId.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Parse_InvalidBytes_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("<\x00FF@b>");

            // act
            Action testAction = () => ImfMessageId.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfMessageId.Parse(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfMessageId.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ToString_Parsed_GivesTheModernForm(string value, string expected)
        {
            // arrange
            var subject = ImfMessageId.Parse(value);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Constructed))]
        public void Constructor_ValidParts_KeepsThem(string idLeft, string idRight)
        {
            // act
            var testResult = new ImfMessageId(idLeft, idRight);

            // assert
            testResult.IdLeft.Should().Be(idLeft);
            testResult.IdRight.Should().Be(idRight);
        }

        [Theory]
        [MemberData(nameof(InvalidConstructorArguments))]
        public void Constructor_InvalidPart_ThrowsArgumentException(string idLeft, string idRight)
        {
            // act
            Action testAction = () => new ImfMessageId(idLeft, idRight);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null, "b", "idLeft")]
        [InlineData("a", null, "idRight")]
        public void Constructor_NullPart_ThrowsArgumentNullException(string idLeft, string idRight, string expectedName)
        {
            // act
            Action testAction = () => new ImfMessageId(idLeft, idRight);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be(expectedName);
        }

        [Theory]
        [MemberData(nameof(Lists))]
        public void ParseList_String_GivesTheIdentifiers(string value, ImfMessageId[] expected)
        {
            // act
            var testResult = ImfMessageId.ParseList(value);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(Lists))]
        public void TryParseList_String_GivesTheIdentifiers(string value, ImfMessageId[] expected)
        {
            // act
            var testResult = ImfMessageId.TryParseList(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Equal(expected);
        }

        [Fact]
        public void ParseList_Bytes_GivesTheIdentifiers()
        {
            // arrange
            var value = Octets.Of("<a@b> <c@d>");

            // act
            var testResult = ImfMessageId.ParseList(value);

            // assert
            testResult.Should().Equal(new ImfMessageId("a", "b"), new ImfMessageId("c", "d"));
        }

        [Fact]
        public void TryParseList_Bytes_GivesTheIdentifiers()
        {
            // arrange
            var value = Octets.Of("<a@b>");

            // act
            var testResult = ImfMessageId.TryParseList(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Equal(new ImfMessageId("a", "b"));
        }

        [Theory]
        [MemberData(nameof(InvalidLists))]
        public void ParseList_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfMessageId.ParseList(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidLists))]
        public void TryParseList_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfMessageId.TryParseList(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void ParseList_InvalidBytes_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("<\x00FF@b>");

            // act
            Action testAction = () => ImfMessageId.ParseList(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ParseList_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfMessageId.ParseList(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void TryParseList_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfMessageId.TryParseList(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ParseList_CastToAList_CannotBeChanged()
        {
            // arrange
            var subject = (IList<ImfMessageId>)ImfMessageId.ParseList("<a@b>");

            // act
            Action testAction = () => subject[0] = new ImfMessageId("c", "d");

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void Equals_MessageId_ComparesBothPartsExactly(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfMessageId.Parse(left);
            var other = ImfMessageId.Parse(right);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void EqualityOperator_MessageId_ComparesBothPartsExactly(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfMessageId.Parse(left);
            var other = ImfMessageId.Parse(right);

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void InequalityOperator_MessageId_ComparesBothPartsExactly(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfMessageId.Parse(left);
            var other = ImfMessageId.Parse(right);

            // act
            var testResult = subject != other;

            // assert
            testResult.Should().Be(!expected);
        }

        [Fact]
        public void Equals_ObjectOfTheSameValue_ReturnsTrue()
        {
            // arrange
            var subject = ImfMessageId.Parse(SimpleMessageId);
            object other = ImfMessageId.Parse("<\"a\"@b>");

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            var subject = ImfMessageId.Parse(SimpleMessageId);

            // act
            var testResult = subject.Equals((ImfMessageId)null);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_EqualIdentifiers_AreTheSame()
        {
            // arrange
            var subject = ImfMessageId.Parse(SimpleMessageId);
            var other = ImfMessageId.Parse("<\"a\"@b>");

            // act
            var testResult = subject.GetHashCode();

            // assert
            testResult.Should().Be(other.GetHashCode());
        }
    }
}
