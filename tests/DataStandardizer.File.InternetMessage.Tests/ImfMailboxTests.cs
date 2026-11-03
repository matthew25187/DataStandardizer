using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfMailboxTests
    {
        private const string SimpleMailbox = "a@b";
        private const string EncodedWord = "=?utf-8?Q?a?=";
        private const string OtherEncodedWord = "=?utf-8?Q?b?=";

        public static TheoryData<string, string, string, string> Parts => new TheoryData<string, string, string, string>
        {
            // value, display name, local part, domain

            { "a@b", "", "a", "b" },
            { "<a@b>", "", "a", "b" },
            { " <a@b> ", "", "a", "b" },
            { "a.b.c@d.e.f", "", "a.b.c", "d.e.f" },
            { "John Doe <a@b>", "John Doe", "a", "b" },
            { "\"John Doe\" <a@b>", "John Doe", "a", "b" },
            { "\"Doe, John\" <a@b>", "Doe, John", "a", "b" },
            { "\"\" <a@b>", "", "a", "b" },

            // ref. RFC 5322 §3.4.1: a local part as a quoted-string, with what only a quoted-string can hold.
            { "\"a b\"@c", "", "a b", "c" },
            { "\"a@b\"@c", "", "a@b", "c" },
            { "\"a\\\"b\"@c", "", "a\"b", "c" },
            { "\"a\\\\b\"@c", "", "a\\b", "c" },
            { "\"\"@c", "", "", "c" },
            { "\"a\"@c", "", "a", "c" },
            { "\"\\a\"@c", "", "a", "c" },
            { "\"a\r\n b\"@c", "", "a b", "c" },

            // ref. RFC 5322 §3.4.1: domain literals
            { "a@[1.2.3.4]", "", "a", "[1.2.3.4]" },
            { "a@[IPv6:2001:db8::1]", "", "a", "[IPv6:2001:db8::1]" },
            { "a@[ 1.2.3.4 ]", "", "a", "[1.2.3.4]" },
            { "a@ [1.2.3.4] ", "", "a", "[1.2.3.4]" },
            { "a@(b)[1.2.3.4](c)", "", "a", "[1.2.3.4]" },

            // ref. RFC 5322 §3.2.2: nested comments and quoted-pairs in them, anywhere CFWS can be.
            { "(a)b@c", "", "b", "c" },
            { "(a(b(c)))d@e", "", "d", "e" },
            { "(a\\)b)c@d", "", "c", "d" },
            { "a(b)@(c)d(e)", "", "a", "d" },
            { "a (b) c <d@e>", "a c", "d", "e" },
            { "(a) <b@c>", "", "b", "c" },

            // ref. RFC 6532 §3.2: UTF-8 in display names, local parts and domains
            { "Jos\x00E9 <jos\x00E9@ex\x00E4mple.test>", "Jos\x00E9", "jos\x00E9", "ex\x00E4mple.test" },
            { "\"Jos\x00E9 Q.\" <\"jos\x00E9 q\"@b>", "Jos\x00E9 Q.", "jos\x00E9 q", "b" },
            { "(Jos\x00E9) a@b", "", "a", "b" },
            { "a@[\x00E9]", "", "a", "[\x00E9]" },

            // ref. RFC 5322 §4.1: obs-phrase
            { "Joe Q. Public <a@b>", "Joe Q. Public", "a", "b" },

            // ref. RFC 5322 §4.2: obs-FWS
            { "a\r\n \r\n <b@c>", "a", "b", "c" },

            // ref. RFC 5322 §4.4: obs-local-part
            { "a . b@c", "", "a.b", "c" },
            { "a (x) . (y) b@c", "", "a.b", "c" },
            { "\"a\".b@c", "", "a.b", "c" },
            { "\"a b\" . \"c\"@d", "", "a b.c", "d" },

            // ref. RFC 5322 §4.4: obs-domain
            { "a@b . c", "", "a", "b.c" },
            { "a@b(x).(y)c", "", "a", "b.c" },

            // ref. RFC 5322 §4.4: obs-route and obs-angle-addr
            { "<@b:a@c>", "", "a", "c" },
            { "d <@b:a@c>", "d", "a", "c" },
            { "<@b,@e:a@c>", "", "a", "c" },
            { "<,@b,,@e,:a@c>", "", "a", "c" },
            { "<(x),(y)@b (z) , (w) @e:a@c>", "", "a", "c" },
            { "<@[1.2.3.4]:a@c>", "", "a", "c" },

            // ref. RFC 5322 §4.1: obs-qp and obs-qtext
            { "\"a\x0001b\"@c", "", "a\x0001b", "c" },
            { "\"a\\\nb\"@c", "", "a\nb", "c" },

            // ref. RFC 5322 §4.4: obs-dtext
            { "a@[b\x0001c]", "", "a", "[b\x0001c]" },
            { "a@[b\\]c]", "", "a", "[b]c]" },
        };

        public static TheoryData<string, string> Formats => new TheoryData<string, string>
        {
            // value, modern form

            { Rfc5322Examples.A11From, Rfc5322Examples.A11From },
            { Rfc5322Examples.A12From, Rfc5322Examples.A12From },
            { Rfc5322Examples.A5From, Rfc5322Examples.A5FromModern },
            { Rfc5322Examples.A61From, Rfc5322Examples.A61FromModern },
            { Rfc5322Examples.A63From, Rfc5322Examples.A63FromModern },
            { Rfc5322Examples.A63To, Rfc5322Examples.A63ToModern },

            { "a@b", "a@b" },
            { "<a@b>", "a@b" },
            { "\"John Doe\" <a@b>", "John Doe <a@b>" },
            { "\"Doe, John\" <a@b>", "\"Doe, John\" <a@b>" },
            { "\"a  b\" <c@d>", "\"a  b\" <c@d>" },
            { "\" a\" <c@d>", "\" a\" <c@d>" },
            { "\"a\tb\" <c@d>", "\"a\tb\" <c@d>" },
            { "\"a\\\"b\" <c@d>", "\"a\\\"b\" <c@d>" },
            { "\"a\\\\b\" <c@d>", "\"a\\\\b\" <c@d>" },
            { "\"\" <a@b>", "a@b" },

            // A local part stays a dot-atom only if it is one.
            { "\"a\"@b", "a@b" },
            { "\"a.b\"@c", "a.b@c" },
            { "\".a\"@b", "\".a\"@b" },
            { "\"a.\"@b", "\"a.\"@b" },
            { "\"a..b\"@c", "\"a..b\"@c" },
            { "\"a b\"@c", "\"a b\"@c" },
            { "\"a\\\"b\"@c", "\"a\\\"b\"@c" },
            { "\"\"@c", "\"\"@c" },

            { "a@[1.2.3.4]", "a@[1.2.3.4]" },
            { "a@[ 1.2.3.4 ]", "a@[1.2.3.4]" },

            // ref. RFC 6532 §3.2
            { "Jos\x00E9 <jos\x00E9@ex\x00E4mple.test>", "Jos\x00E9 <jos\x00E9@ex\x00E4mple.test>" },

            // ref. RFC 5322 §4: each obsolete form is written in the modern form.
            { "Joe Q. Public <a@b>", "\"Joe Q. Public\" <a@b>" },
            { "a . b@c", "a.b@c" },
            { "\"a b\" . c@d", "\"a b.c\"@d" },
            { "a@b . c", "a@b.c" },
            { "<@b,@e:a@c>", "a@c" },
            { "d <@b:a@c>", "d <a@c>" },

            // ref. RFC 5322 §4.1 and §4.4: what has no modern form is left out, and a line break never gets out.
            { "\"a\x0001b\"@c", "\"ab\"@c" },
            { "\"a\\\nb\"@c", "\"ab\"@c" },
            { "\"a\\\r\\\nb\" <c@d>", "\"ab\" <c@d>" },
            { "a@[b\x0001c]", "a@[bc]" },
            { "a@[b\\]c]", "a@[bc]" },

            // ref. RFC 2047 §5(3): an atom may be an encoded word, and a quoted-string stays plain text.
            { EncodedWord + " <a@b>", EncodedWord + " <a@b>" },
            { "\"" + EncodedWord + "\" <a@b>", "\"" + EncodedWord + "\" <a@b>" },
            { "b " + EncodedWord + " \"c d\" <a@b>", "b " + EncodedWord + " c d <a@b>" },
            { "b " + EncodedWord + " \"c,\" <a@b>", "b " + EncodedWord + " \"c,\" <a@b>" },
            { EncodedWord + " \"c  d\" <a@b>", EncodedWord + " \"c  d\" <a@b>" },
            { EncodedWord + " " + OtherEncodedWord + " <a@b>", EncodedWord + " " + OtherEncodedWord + " <a@b>" },
            { EncodedWord + " \"" + OtherEncodedWord + "\" <a@b>", EncodedWord + " \"" + OtherEncodedWord + "\" <a@b>" },
            { "\"" + OtherEncodedWord + "\" " + EncodedWord + " <a@b>", "\"" + OtherEncodedWord + "\" " + EncodedWord + " <a@b>" },
            { "c \"" + OtherEncodedWord + "\" " + EncodedWord + " d <a@b>", "\"c " + OtherEncodedWord + "\" " + EncodedWord + " d <a@b>" },
            { "c. " + EncodedWord + " <a@b>", "\"c.\" " + EncodedWord + " <a@b>" },
        };

        public static TheoryData<string> InvalidValues => new TheoryData<string>
        {
            "",
            " ",
            "(a)",
            "a",
            "a b",
            "a@",
            "@b",
            "@",

            // ref. RFC 5322 §3.4.1: a missing or misplaced part of an addr-spec
            "a@b@c",
            "a b@c",
            "a@b c",
            ".a@b",
            "a.@b",
            "a..b@c",
            "a@.b",
            "a@b.",
            "a@b..c",
            "a@\"b\"",
            "\"a\"b@c",
            "[a]@b",
            "a@[b]c",
            "a@b[c]",

            // ref. RFC 5322 §3.4: angle-addr
            "<",
            ">",
            "<>",
            "<a>",
            "<a@b",
            "a@b>",
            "<a@b>>",
            "<a@b> c",
            "<a@b> <c@d>",
            "a <b>",
            "a <b@c",
            ". <a@b>",
            ".a <b@c>",

            // ref. RFC 5322 §4.4: obs-route
            "<@b>",
            "<@b:>",
            "<@b:a>",
            "<:a@b>",
            "<@:a@b>",
            "<@b c:a@d>",
            "<@b,c:a@d>",
            "<,:a@b>",
            "<,a@b>",

            // A list or a group is not a mailbox.
            "a@b,",
            ",a@b",
            "a@b, c@d",
            "a@b;",
            "a:;",
            "a: b@c;",

            // Unbalanced comments, and unterminated quoted-strings and domain-literals.
            "(a@b",
            "a@b)",
            "a(b@c",
            "a@b(",
            "a@b(c(d)",
            "\"a@b",
            "\"a\"@b\"",
            "\"a\\\"@b",
            "a@[b",
            "a@b]",

            // Characters that are no part of any token.
            "a\\@b",
            "a\0@b",
            "a\x0001@b",
            "a\x007F@b",
            "\"a\0\"@b",

            // ref. RFC 5322 §2.2: CR and LF only in a fold.
            "a\r@b",
            "a\n@b",
            "a\r\n@b",
            "a@b\r\n",
            "\"a\nb\"@c",
            "(a\rb) c@d",
        };

        public static TheoryData<byte[], string, string, string> ByteValues => new TheoryData<byte[], string, string, string>
        {
            { Octets.Of("a@b"), "", "a", "b" },
            { Octets.Of("John Doe\r\n <a@b>"), "John Doe", "a", "b" },

            // ref. RFC 6532 §3.2
            { Octets.Of("Jos\x00C3\x00A9 <\x00E2\x0082\x00AC@b>"), "Jos\x00E9", "\x20AC", "b" },
            { Octets.Of("\"\x00F0\x009F\x0098\x0080\" <a@b>"), "\xD83D\xDE00", "a", "b" },
        };

        public static TheoryData<byte[]> InvalidByteValues => new TheoryData<byte[]>
        {
            Octets.Of(""),
            Octets.Of("a"),

            // ref. RFC 6532 §3.2: bytes that are not well-formed UTF-8 (RFC 3629 §4), in each place text can be.
            Octets.Of("\x00FF@b"),
            Octets.Of("a@\x00FF"),
            Octets.Of("\"\x00FF\"@b"),
            Octets.Of("(\x00FF) a@b"),
            Octets.Of("a@[\x00FF]"),
            Octets.Of("\x00C3@b"),
            Octets.Of("a@b\x00C3"),
            Octets.Of("\x00C0\x0080@b"),
            Octets.Of("\x00ED\x00A0\x0080@b"),
            Octets.Of("\x00F4\x0090\x0080\x0080@b"),
            Octets.Of("\"\\\x00FF\"@b"),
        };

        public static TheoryData<string, string, string, string> Constructed => new TheoryData<string, string, string, string>
        {
            // display name, local part, domain, modern form

            { "", "a", "b", "a@b" },
            { "John Doe", "a", "b.c", "John Doe <a@b.c>" },
            { "Doe, John", "a b", "[1.2.3.4]", "\"Doe, John\" <\"a b\"@[1.2.3.4]>" },
            { "Joe Q. Public", "a", "[a b]", "\"Joe Q. Public\" <a@[a b]>" },
            { "a\tb", "", "b", "\"a\tb\" <\"\"@b>" },
            { "Jos\x00E9", "jos\x00E9", "\x00E9.test", "Jos\x00E9 <jos\x00E9@\x00E9.test>" },
            { "\xD83D\xDE00", "a", "b", "\xD83D\xDE00 <a@b>" },

            // ref. RFC 2047 §5(3): text that only looks like an encoded word has to stay plain text.
            { EncodedWord, "a", "b", "\"" + EncodedWord + "\" <a@b>" },
            { "b " + EncodedWord, "a", "b", "\"b " + EncodedWord + "\" <a@b>" },
        };

        public static TheoryData<string, string, string> InvalidConstructorArguments => new TheoryData<string, string, string>
        {
            // ref. RFC 5322 §2.2: nothing a caller gives may put a line break, or so a new field, in the output.
            { "a\r\nBcc: b@c", "d", "e" },
            { "a\nb", "c", "d" },
            { "a\rb", "c", "d" },
            { "a\0b", "c", "d" },
            { "a\x007F", "c", "d" },
            { "", "a\r\nBcc: b@c", "d" },
            { "", "a\0b", "c" },

            // ref. RFC 5322 §3.4.1: a domain is a dot-atom or a domain-literal.
            { "", "a", "" },
            { "", "a", "b c" },
            { "", "a", "b..c" },
            { "", "a", ".b" },
            { "", "a", "b." },
            { "", "a", "b@c" },
            { "", "a", "b\r\nBcc: c@d" },
            { "", "a", "[b" },
            { "", "a", "b]" },
            { "", "a", "[b]c]" },
            { "", "a", "[b\\]" },
            { "", "a", "[b\r\n]" },
        };

        public static TheoryData<string, string> ConstructedDomains => new TheoryData<string, string>
        {
            // ref. RFC 5322 §3.4.1: the same form that parsing gives, so that the two compare equal.
            { "b.c", "b.c" },
            { "[1.2.3.4]", "[1.2.3.4]" },
            { "[]", "[]" },
            { "[ ]", "[]" },
            { "[ b ]", "[b]" },
            { "[\tb\t]", "[b]" },
            { "[b  c]", "[b c]" },
            { "[b\tc]", "[b c]" },
        };

        public static TheoryData<string, string, bool> EqualityCases => new TheoryData<string, string, bool>
        {
            { "a@b", "a@b", true },

            // The local part is case-sensitive and the domain is not.
            { "a@b", "A@b", false },
            { "a@b", "a@B", true },
            { "a@[b]", "a@[B]", true },

            // What the address stands for counts, and not the form it was written in.
            { "\"a\"@b", "a@b", true },
            { "<a@b>", "a@b", true },
            { "a(c)@b", "a@b", true },
            { "<@c:a@b>", "a@b", true },
            { "a@b . c", "a@b.c", true },

            { "a@b", "a@c", false },
            { "a@b", "c@b", false },

            // The display name counts too, case-sensitively.
            { "c <a@b>", "c <a@b>", true },
            { "\"c\" <a@b>", "c <a@b>", true },
            { "c <a@b>", "C <a@b>", false },
            { "c <a@b>", "a@b", false },
            { "c <a@b>", "d <a@b>", false },

            // ref. RFC 2047 §5(3): a display name is compared as text, whichever form it was written in.
            { EncodedWord + " <a@b>", "a <a@b>", true },
            { "=?iso-8859-1?Q?=E9?= <a@b>", "=?utf-8?Q?=C3=A9?= <a@b>", true },

            // ref. RFC 2047 §5(3): an encoded word is not the same as a quoted-string that looks like one.
            { EncodedWord + " <a@b>", "\"" + EncodedWord + "\" <a@b>", false },
            { EncodedWord + " \"" + OtherEncodedWord + "\" <a@b>", "\"" + EncodedWord + "\" " + OtherEncodedWord + " <a@b>", false },
        };

        public static TheoryData<string, string, bool> SameAddressCases => new TheoryData<string, string, bool>
        {
            { "a@b", "a@b", true },
            { "c <a@b>", "d <a@b>", true },
            { "c <a@b>", "a@B", true },
            { "a@b", "A@b", false },
            { "a@b", "a@c", false },
        };

        public static TheoryData<string, string> EqualValues => new TheoryData<string, string>
        {
            { "a@b", "a@b" },
            { "a@b", "a@B" },
            { "c <a@b>", "\"c\" <a@B>" },
        };

        [Theory]
        [MemberData(nameof(Parts))]
        public void Parse_String_GivesItsParts(string value, string expectedDisplayName, string expectedLocalPart, string expectedDomain)
        {
            // act
            var testResult = ImfMailbox.Parse(value);

            // assert
            testResult.DisplayName.Should().Be(expectedDisplayName);
            testResult.LocalPart.Should().Be(expectedLocalPart);
            testResult.Domain.Should().Be(expectedDomain);
        }

        [Theory]
        [MemberData(nameof(ByteValues))]
        public void Parse_Bytes_GivesItsParts(byte[] value, string expectedDisplayName, string expectedLocalPart, string expectedDomain)
        {
            // act
            var testResult = ImfMailbox.Parse(value);

            // assert
            testResult.DisplayName.Should().Be(expectedDisplayName);
            testResult.LocalPart.Should().Be(expectedLocalPart);
            testResult.Domain.Should().Be(expectedDomain);
        }

        [Theory]
        [MemberData(nameof(ByteValues))]
        public void TryParse_Bytes_GivesItsParts(byte[] value, string expectedDisplayName, string expectedLocalPart, string expectedDomain)
        {
            // act
            var testResult = ImfMailbox.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.DisplayName.Should().Be(expectedDisplayName);
            result.LocalPart.Should().Be(expectedLocalPart);
            result.Domain.Should().Be(expectedDomain);
        }

        [Theory]
        [MemberData(nameof(Parts))]
        public void TryParse_String_GivesItsParts(string value, string expectedDisplayName, string expectedLocalPart, string expectedDomain)
        {
            // act
            var testResult = ImfMailbox.TryParse(value, out var result);

            // assert
            testResult.Should().BeTrue();
            result.DisplayName.Should().Be(expectedDisplayName);
            result.LocalPart.Should().Be(expectedLocalPart);
            result.Domain.Should().Be(expectedDomain);
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void Parse_InvalidString_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfMailbox.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidValues))]
        public void TryParse_InvalidString_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfMailbox.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(InvalidByteValues))]
        public void Parse_InvalidBytes_ThrowsFormatException(byte[] value)
        {
            // act
            Action testAction = () => ImfMailbox.Parse(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidByteValues))]
        public void TryParse_InvalidBytes_ReturnsFalse(byte[] value)
        {
            // act
            var testResult = ImfMailbox.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().BeNull();
        }

        [Fact]
        public void Parse_NullString_ThrowsArgumentNullException()
        {
            // arrange
            string value = null;

            // act
            Action testAction = () => ImfMailbox.Parse(value);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("value");
        }

        [Fact]
        public void TryParse_NullString_ReturnsFalse()
        {
            // arrange
            string value = null;

            // act
            var testResult = ImfMailbox.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Formats))]
        public void ToString_Parsed_GivesTheModernForm(string value, string expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(value);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("a@b", "a@b")]
        [InlineData("c <a@b>", "a@b")]
        [InlineData("\"a b\"@c", "\"a b\"@c")]
        [InlineData("<@d:a . b@c . e>", "a.b@c.e")]
        public void Address_Parsed_GivesTheAddrSpec(string value, string expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(value);

            // act
            var testResult = subject.Address;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Constructed))]
        public void ToString_Constructed_GivesTheModernForm(string displayName, string localPart, string domain, string expected)
        {
            // arrange
            var subject = new ImfMailbox(displayName, localPart, domain);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(ConstructedDomains))]
        public void Constructor_Domain_GivesTheParsedForm(string domain, string expected)
        {
            // act
            var testResult = new ImfMailbox("a", domain);

            // assert
            testResult.Domain.Should().Be(expected);
        }

        [Fact]
        public void HasSameAddress_ConstructedAndParsedDomainLiterals_ReturnsTrue()
        {
            // arrange
            var subject = new ImfMailbox("a", "[ b ]");
            var other = ImfMailbox.Parse("a@[ b ]");

            // act
            var testResult = subject.HasSameAddress(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Constructor_NoDisplayName_HasAnEmptyDisplayName()
        {
            // act
            var testResult = new ImfMailbox("a", "b");

            // assert
            testResult.DisplayName.Should().BeEmpty();
            testResult.LocalPart.Should().Be("a");
            testResult.Domain.Should().Be("b");
        }

        [Theory]
        [MemberData(nameof(InvalidConstructorArguments))]
        public void Constructor_InvalidArgument_ThrowsArgumentException(string displayName, string localPart, string domain)
        {
            // act
            Action testAction = () => new ImfMailbox(displayName, localPart, domain);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(null, "a", "b", "displayName")]
        [InlineData("", null, "b", "localPart")]
        [InlineData("", "a", null, "domain")]
        public void Constructor_NullArgument_ThrowsArgumentNullException(string displayName, string localPart, string domain, string expectedName)
        {
            // act
            Action testAction = () => new ImfMailbox(displayName, localPart, domain);

            // assert
            testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be(expectedName);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void Equals_Mailbox_ComparesItsValue(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(left);
            var other = ImfMailbox.Parse(right);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void EqualityOperator_Mailbox_ComparesItsValue(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(left);
            var other = ImfMailbox.Parse(right);

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(EqualityCases))]
        public void InequalityOperator_Mailbox_ComparesItsValue(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(left);
            var other = ImfMailbox.Parse(right);

            // act
            var testResult = subject != other;

            // assert
            testResult.Should().Be(!expected);
        }

        [Theory]
        [MemberData(nameof(EqualValues))]
        public void GetHashCode_EqualMailboxes_AreTheSame(string left, string right)
        {
            // arrange
            var subject = ImfMailbox.Parse(left);
            var other = ImfMailbox.Parse(right);

            // act
            var testResult = subject.GetHashCode();

            // assert
            testResult.Should().Be(other.GetHashCode());
        }

        [Fact]
        public void Equals_Null_ReturnsFalse()
        {
            // arrange
            var subject = ImfMailbox.Parse(SimpleMailbox);

            // act
            var testResult = subject.Equals((ImfMailbox)null);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_ObjectOfTheSameValue_ReturnsTrue()
        {
            // arrange
            var subject = ImfMailbox.Parse(SimpleMailbox);
            object other = ImfMailbox.Parse(SimpleMailbox);

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_ObjectOfAnotherType_ReturnsFalse()
        {
            // arrange
            var subject = ImfMailbox.Parse(SimpleMailbox);
            object other = "a@b";

            // act
            var testResult = subject.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_NullOnTheLeft_ReturnsFalse()
        {
            // arrange
            ImfMailbox subject = null;
            var other = ImfMailbox.Parse(SimpleMailbox);

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void EqualityOperator_BothNull_ReturnsTrue()
        {
            // arrange
            ImfMailbox subject = null;
            ImfMailbox other = null;

            // act
            var testResult = subject == other;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(SameAddressCases))]
        public void HasSameAddress_Mailbox_IgnoresTheDisplayName(string left, string right, bool expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(left);
            var other = ImfMailbox.Parse(right);

            // act
            var testResult = subject.HasSameAddress(other);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void HasSameAddress_Null_ReturnsFalse()
        {
            // arrange
            var subject = ImfMailbox.Parse(SimpleMailbox);

            // act
            var testResult = subject.HasSameAddress(null);

            // assert
            testResult.Should().BeFalse();
        }
    }
}
