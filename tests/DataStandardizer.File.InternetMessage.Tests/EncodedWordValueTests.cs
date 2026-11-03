using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of RFC 2047 encoded words in the typed header values: <see cref="ImfMailbox"/>, <see cref="ImfGroup"/>, <see cref="ImfAddressList"/> and <see cref="ImfPhraseList"/>.
    /// </summary>
    public class EncodedWordValueTests
    {
        private const string EncodedA = "=?UTF-8?Q?a?=";
        private const string EncodedB = "=?UTF-8?Q?b?=";

        // ref. RFC 2047 §4: é as UTF-8 is the octets C3 A9, which are shorter in the "B" encoding.
        private const string WrittenEAcute = "=?UTF-8?B?w6k=?=";
        private const string EncodedEAcute = "=?UTF-8?Q?=C3=A9?=";
        private const string EAcute = "\x00E9";
        private const string UnknownCharsetWord = "=?x-unknown?Q?a?=";

        // ref. RFC 5322 §2.1.1: lines SHOULD be no more than 78 characters.
        private const int TargetLineLength = 78;

        public static TheoryData<string, string> DisplayNames => new TheoryData<string, string>
        {
            // ref. RFC 2047 §8
            { Rfc2047Examples.KeithMooreFrom, Rfc2047Examples.KeithMooreName },
            { Rfc2047Examples.KeldTo, Rfc2047Examples.KeldName },
            { Rfc2047Examples.AndreCc, Rfc2047Examples.AndreName },
            { Rfc2047Examples.OlleFrom, Rfc2047Examples.OlleName },
            { Rfc2047Examples.PatrikFrom, Rfc2047Examples.PatrikName },
            { Rfc2047Examples.NathanielFrom, Rfc2047Examples.NathanielName },

            // ref. RFC 2047 §6.2: white space between encoded words goes, and any other stays.
            { EncodedA + " " + EncodedB + " <c@d>", "ab" },
            { EncodedA + "\r\n  " + EncodedB + " <c@d>", "ab" },
            { EncodedA + " c " + EncodedB + " <c@d>", "a c b" },
            { "c " + EncodedA + " <c@d>", "c a" },

            // ref. RFC 2047 §5(3): a quoted-string is never decoded, and it ends a run of words.
            { "\"" + EncodedA + "\" <c@d>", EncodedA },
            { EncodedA + " \"" + EncodedB + "\" <c@d>", "a " + EncodedB },
            { "\"c d\" " + EncodedA + " <c@d>", "c d a" },
            { EncodedA + " \"c\" " + EncodedB + " <c@d>", "a c b" },

            // A comment is not white space, so the words on either side of one stay apart.
            { EncodedA + " (c) " + EncodedB + " <c@d>", "a b" },

            // A period ends a run of words in the obsolete form.
            { EncodedA + ". " + EncodedB + " <c@d>", "a. b" },

            // ref. RFC 2047 §6.2: a word that can't be decoded stays as ordinary text.
            { "=?UTF-8?Q?=FF?= <c@d>", "=?UTF-8?Q?=FF?=" },
            { "=?a?= <c@d>", "=?a?=" },

            // ref. RFC 2047 §5(3): in a phrase, "Q"-encoded text holds only some characters.
            { "=?UTF-8?Q?a&b?= <c@d>", "=?UTF-8?Q?a&b?=" },

            // Text in a word can hold what a phrase can't, as it isn't syntax.
            { "=?UTF-8?Q?a=2Cb?= <c@d>", "a,b" },
            { "=?UTF-8?Q?a=0D=0Ab?= <c@d>", "a\r\nb" },
        };

        // Each is a mailbox whose local part or domain has the form of an encoded word, which is not read as one (RFC 2047 §5(3)).
        public static TheoryData<string, string, string> AddrSpecs => new TheoryData<string, string, string>
        {
            { EncodedA + "@b", EncodedA, "b" },
            { "<" + EncodedA + "@b>", EncodedA, "b" },
            { "c <" + EncodedA + "@" + EncodedB + ">", EncodedA, EncodedB },
            { EncodedA + " <" + EncodedA + "@b>", EncodedA, "b" },
        };

        // The ways to read a display name with options, and to get it back.
        public static TheoryData<Func<string, ImfOptions, string>> DisplayNameParsers => new TheoryData<Func<string, ImfOptions, string>>
        {
            (value, options) => ImfAddress.Parse(value + " <c@d>", options).DisplayName,
            (value, options) => ImfAddress.Parse(Octets.Of(value + " <c@d>"), options).DisplayName,
            (value, options) => ImfMailbox.Parse(value + " <c@d>", options).DisplayName,
            (value, options) => ImfMailbox.Parse(Octets.Of(value + " <c@d>"), options).DisplayName,
            (value, options) => ImfGroup.Parse(value + ":;", options).DisplayName,
            (value, options) => ImfGroup.Parse(Octets.Of(value + ":;"), options).DisplayName,
            (value, options) => ImfAddressList.Parse(value + " <c@d>", options)[0].DisplayName,
            (value, options) => ImfAddressList.Parse(Octets.Of(value + " <c@d>"), options)[0].DisplayName,
            (value, options) => ImfGroup.Parse("e: " + value + " <c@d>;", options).Mailboxes[0].DisplayName,
            (value, options) => ImfPhraseList.Parse(value, options)[0],
            (value, options) => ImfPhraseList.Parse(Octets.Of(value), options)[0],
        };

        public static TheoryData<Func<ImfOptions, bool>> ByteTryParsers => new TheoryData<Func<ImfOptions, bool>>
        {
            options => ImfAddress.TryParse(Octets.Of("caf\x00E9 <c@d>"), options, out _),
            options => ImfMailbox.TryParse(Octets.Of("caf\x00E9 <c@d>"), options, out _),
            options => ImfGroup.TryParse(Octets.Of("caf\x00E9:;"), options, out _),
            options => ImfAddressList.TryParse(Octets.Of("caf\x00E9 <c@d>"), options, out _),
            options => ImfPhraseList.TryParse(Octets.Of("caf\x00E9"), options, out _),
        };

        public static TheoryData<Func<bool>> NullStringTryParsers => new TheoryData<Func<bool>>
        {
            () => ImfAddress.TryParse((string)null, new ImfOptions(), out _),
            () => ImfMailbox.TryParse((string)null, new ImfOptions(), out _),
            () => ImfGroup.TryParse((string)null, new ImfOptions(), out _),
            () => ImfAddressList.TryParse((string)null, new ImfOptions(), out _),
            () => ImfPhraseList.TryParse((string)null, new ImfOptions(), out _),
        };

        public static TheoryData<Func<bool>> ValidStringTryParsers => new TheoryData<Func<bool>>
        {
            () => ImfAddress.TryParse("c <c@d>", new ImfOptions(), out _),
            () => ImfMailbox.TryParse("c <c@d>", new ImfOptions(), out _),
            () => ImfGroup.TryParse("c:;", new ImfOptions(), out _),
            () => ImfAddressList.TryParse("c <c@d>", new ImfOptions(), out _),
            () => ImfPhraseList.TryParse("c", new ImfOptions(), out _),
        };

        public static TheoryData<Func<bool>> InvalidStringTryParsers => new TheoryData<Func<bool>>
        {
            () => ImfAddress.TryParse("c", new ImfOptions(), out _),
            () => ImfMailbox.TryParse("c:;", new ImfOptions(), out _),
            () => ImfGroup.TryParse("c@d", new ImfOptions(), out _),
            () => ImfAddressList.TryParse("", new ImfOptions(), out _),
            () => ImfPhraseList.TryParse(".", new ImfOptions(), out _),
        };

        public static TheoryData<Action> InvalidParses => new TheoryData<Action>
        {
            () => ImfAddress.Parse("c", new ImfOptions()),
            () => ImfAddress.Parse(Octets.Of("c"), new ImfOptions()),
            () => ImfMailbox.Parse("c:;", new ImfOptions()),
            () => ImfMailbox.Parse(Octets.Of("c:;"), new ImfOptions()),
            () => ImfGroup.Parse("c@d", new ImfOptions()),
            () => ImfGroup.Parse(Octets.Of("c@d"), new ImfOptions()),
            () => ImfAddressList.Parse("", new ImfOptions()),
            () => ImfAddressList.Parse(Octets.Of(""), new ImfOptions()),
            () => ImfPhraseList.Parse(".", new ImfOptions()),
            () => ImfPhraseList.Parse(Octets.Of("."), new ImfOptions()),
        };

        public static TheoryData<Action, string> NullArguments => new TheoryData<Action, string>
        {
            { () => ImfAddress.Parse("c@d", null), "options" },
            { () => ImfAddress.Parse((string)null, new ImfOptions()), "value" },
            { () => ImfAddress.Parse(Octets.Of("c@d"), null), "options" },
            { () => ImfAddress.TryParse("c@d", null, out _), "options" },
            { () => ImfAddress.TryParse(Octets.Of("c@d"), null, out _), "options" },
            { () => ImfMailbox.Parse("c@d", null), "options" },
            { () => ImfMailbox.Parse((string)null, new ImfOptions()), "value" },
            { () => ImfMailbox.Parse(Octets.Of("c@d"), null), "options" },
            { () => ImfMailbox.TryParse("c@d", null, out _), "options" },
            { () => ImfMailbox.TryParse(Octets.Of("c@d"), null, out _), "options" },
            { () => ImfGroup.Parse("c:;", null), "options" },
            { () => ImfGroup.Parse((string)null, new ImfOptions()), "value" },
            { () => ImfGroup.Parse(Octets.Of("c:;"), null), "options" },
            { () => ImfGroup.TryParse("c:;", null, out _), "options" },
            { () => ImfGroup.TryParse(Octets.Of("c:;"), null, out _), "options" },
            { () => ImfAddressList.Parse("c@d", null), "options" },
            { () => ImfAddressList.Parse((string)null, new ImfOptions()), "value" },
            { () => ImfAddressList.Parse(Octets.Of("c@d"), null), "options" },
            { () => ImfAddressList.TryParse("c@d", null, out _), "options" },
            { () => ImfAddressList.TryParse(Octets.Of("c@d"), null, out _), "options" },
            { () => ImfPhraseList.Parse("c", null), "options" },
            { () => ImfPhraseList.Parse((string)null, new ImfOptions()), "value" },
            { () => ImfPhraseList.Parse(Octets.Of("c"), null), "options" },
            { () => ImfPhraseList.TryParse("c", null, out _), "options" },
            { () => ImfPhraseList.TryParse(Octets.Of("c"), null, out _), "options" },
        };

        public static TheoryData<Func<string>> UnknownEncodings => new TheoryData<Func<string>>
        {
            () => ImfMailbox.Parse("c@d").ToString((ImfHeaderEncoding)2),
            () => ImfGroup.Parse("c:;").ToString((ImfHeaderEncoding)2),
            () => ImfAddressList.Parse("c@d").ToString((ImfHeaderEncoding)2),
            () => ImfPhraseList.Parse("c").ToString((ImfHeaderEncoding)2),
        };

        // Each is a parsed value, then what it writes as it holds it, as encoded words, and as UTF-8.
        private static readonly string[][] FormattedMailboxes =
        {
            new[] { "a <c@d>", "a <c@d>", "a <c@d>", "a <c@d>" },
            new[] { "c@d", "c@d", "c@d", "c@d" },

            // Text that is not US-ASCII, as it was read in UTF-8.
            new[] { "caf" + EAcute + " <c@d>", "caf" + EAcute + " <c@d>", "=?UTF-8?B?Y2Fmw6k=?= <c@d>", "caf" + EAcute + " <c@d>" },

            // ref. RFC 2047 §2, §5(3): each word is whole, and the spaces are in them.
            new[] { EAcute + " a  b <c@d>", EAcute + " a b <c@d>", "=?UTF-8?B?w6kgYSBi?= <c@d>", EAcute + " a b <c@d>" },

            // An encoded word stays as written, or is made again in UTF-8, or is written as its text.
            new[] { "=?iso-8859-1?Q?=E9?= <c@d>", "=?iso-8859-1?Q?=E9?= <c@d>", WrittenEAcute + " <c@d>", EAcute + " <c@d>" },
            new[] { EncodedA + " <c@d>", EncodedA + " <c@d>", "a <c@d>", "a <c@d>" },

            // ref. RFC 2047 §5(3): quoting and encoding together is written as encoded words, not as a quoted-string holding them.
            new[] { "\"caf" + EAcute + ", a\" <c@d>", "\"caf" + EAcute + ", a\" <c@d>", "=?UTF-8?B?Y2Fmw6ksIGE=?= <c@d>", "\"caf" + EAcute + ", a\" <c@d>" },

            // Quoting alone stays a quoted-string.
            new[] { "\"a, b\" <c@d>", "\"a, b\" <c@d>", "\"a, b\" <c@d>", "\"a, b\" <c@d>" },

            // Text of the form of an encoded word stays plain text.
            new[] { "\"" + EncodedA + "\" <c@d>", "\"" + EncodedA + "\" <c@d>", "\"" + EncodedA + "\" <c@d>", "\"" + EncodedA + "\" <c@d>" },

            // ref. RFC 2047 §5: a local part or domain is never encoded, so one that is not US-ASCII is left for a writer to refuse.
            new[] { "a <" + EAcute + "@d>", "a <" + EAcute + "@d>", "a <" + EAcute + "@d>", "a <" + EAcute + "@d>" },

            // ref. RFC 5322 §4.1: a period right next to an encoded word is obsolete syntax, which is not written, so the words are made again and hold it.
            new[] { EncodedA + ". <c@d>", "=?UTF-8?Q?a=2E?= <c@d>", "\"a.\" <c@d>", "\"a.\" <c@d>" },
            new[] { EncodedA + ". b <c@d>", "=?UTF-8?Q?a=2E_b?= <c@d>", "\"a. b\" <c@d>", "\"a. b\" <c@d>" },
            new[] { "c." + EncodedA + " <c@d>", "=?UTF-8?B?Yy5h?= <c@d>", "\"c.a\" <c@d>", "\"c.a\" <c@d>" },

            // With white space, the period stays in plain text of its own.
            new[] { "c. " + EncodedA + " <c@d>", "\"c.\" " + EncodedA + " <c@d>", "\"c. a\" <c@d>", "\"c. a\" <c@d>" },
            new[] { EncodedA + " .c <c@d>", EncodedA + " \".c\" <c@d>", "\"a .c\" <c@d>", "\"a .c\" <c@d>" },

            // A control character that a word decoded to has no form that can be written, and a line break never gets out.
            new[] { "=?UTF-8?Q?a=0D=0Ab?= <c@d>", "=?UTF-8?Q?a=0D=0Ab?= <c@d>", "\"ab\" <c@d>", "\"ab\" <c@d>" },
            new[] { "=?UTF-8?Q?=C3=A9=0Ab?= <c@d>", "=?UTF-8?Q?=C3=A9=0Ab?= <c@d>", "=?UTF-8?B?w6li?= <c@d>", "\"" + EAcute + "b\" <c@d>" },
        };

        // Each is a mailbox with a period by an encoded word, and its display name, which has to survive being written and read again.
        public static TheoryData<string, string> PeriodsByEncodedWords => new TheoryData<string, string>
        {
            { EncodedA + ". <c@d>", "a." },
            { EncodedA + ". b <c@d>", "a. b" },
            { EncodedA + "." + EncodedB + " <c@d>", "a.b" },
            { "c." + EncodedA + " <c@d>", "c.a" },
            { "c. " + EncodedA + " <c@d>", "c. a" },
            { EncodedA + " .c <c@d>", "a .c" },
            { EAcute + "." + EncodedA + " <c@d>", EAcute + ".a" },
        };

        public static TheoryData<string, string> MailboxesAsHeld => FormatColumn(1);

        public static TheoryData<string, string> MailboxesAsEncodedWords => FormatColumn(2);

        public static TheoryData<string, string> MailboxesAsUtf8 => FormatColumn(3);

        [Theory]
        [MemberData(nameof(DisplayNames))]
        public void Parse_EncodedWordsInDisplayName_DecodesThem(string value, string expected)
        {
            // act
            var testResult = ImfMailbox.Parse(value, Rfc2047Examples.Options).DisplayName;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Parse_EncodedWordWithDefaultOptions_DecodesIt()
        {
            // act
            var testResult = ImfAddress.Parse(EncodedA + " <c@d>").DisplayName;

            // assert
            testResult.Should().Be("a");
        }

        [Theory]
        [MemberData(nameof(DisplayNameParsers))]
        public void Parse_WithOptions_DecodesEncodedWords(Func<string, ImfOptions, string> parse)
        {
            // arrange
            var options = new ImfOptions();

            // act
            var testResult = parse(EncodedA + " " + EncodedB, options);

            // assert
            testResult.Should().Be("ab");
        }

        [Theory]
        [MemberData(nameof(DisplayNameParsers))]
        public void Parse_MalformedWordByDefault_StaysAsItIs(Func<string, ImfOptions, string> parse)
        {
            // arrange
            var options = new ImfOptions();

            // act
            var testResult = parse(UnknownCharsetWord, options);

            // assert
            testResult.Should().Be(UnknownCharsetWord);
        }

        [Theory]
        [MemberData(nameof(DisplayNameParsers))]
        public void Parse_MalformedWordWithOptIn_DecodesIt(Func<string, ImfOptions, string> parse)
        {
            // arrange
            var options = new ImfOptions { AllowMalformedEncodedWords = true };

            // act
            var testResult = parse(UnknownCharsetWord, options);

            // assert
            testResult.Should().Be("a");
        }

        [Fact]
        public void Parse_WordsWithSplitCharacterWithOptIn_ReadsTheCharacterWhole()
        {
            // arrange
            var options = new ImfOptions { AllowMalformedEncodedWords = true };

            // act
            var testResult = ImfMailbox.Parse("=?UTF-8?Q?=C3?= =?UTF-8?Q?=A9?= <c@d>", options).DisplayName;

            // assert
            testResult.Should().Be(EAcute);
        }

        [Theory]
        [MemberData(nameof(AddrSpecs))]
        public void Parse_EncodedWordFormInAddrSpec_IsNotDecoded(string value, string expectedLocalPart, string expectedDomain)
        {
            // act
            var testResult = ImfMailbox.Parse(value);

            // assert
            testResult.LocalPart.Should().Be(expectedLocalPart);
            testResult.Domain.Should().Be(expectedDomain);
        }

        [Fact]
        public void Parse_EncodedWordFormInMessageId_IsNotDecoded()
        {
            // act
            var testResult = ImfMessageId.Parse("<" + EncodedA + "@b>");

            // assert
            testResult.IdLeft.Should().Be(EncodedA);
        }

        [Theory]
        [MemberData(nameof(ByteTryParsers))]
        public void TryParse_BytesThatAreNotUtf8ByDefault_ReturnsFalse(Func<ImfOptions, bool> tryParse)
        {
            // arrange
            var options = new ImfOptions();

            // act
            var testResult = tryParse(options);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ByteTryParsers))]
        public void TryParse_BytesThatAreNotUtf8WithAllowEightBitHeaders_ReturnsTrue(Func<ImfOptions, bool> tryParse)
        {
            // arrange
            var options = new ImfOptions { AllowEightBitHeaders = true };

            // act
            var testResult = tryParse(options);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Parse_BytesThatAreNotUtf8WithAllowEightBitHeaders_UsesTheFallbackBeforeTheWords()
        {
            // arrange
            var value = Octets.Of("caf\x00E9 " + EncodedA + " <c@d>");
            var options = new ImfOptions { AllowEightBitHeaders = true };

            // act
            var testResult = ImfMailbox.Parse(value, options).DisplayName;

            // assert
            testResult.Should().Be("caf" + EAcute + " a");
        }

        [Fact]
        public void Parse_NoCharsetResolver_ThrowsArgumentException()
        {
            // arrange
            var options = new ImfOptions { CharsetResolver = null };

            // act
            Action testAction = () => ImfMailbox.Parse("c@d", options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        [Theory]
        [MemberData(nameof(NullStringTryParsers))]
        public void TryParse_NullStringWithOptions_ReturnsFalse(Func<bool> tryParse)
        {
            // act
            var testResult = tryParse();

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ValidStringTryParsers))]
        public void TryParse_ValidStringWithOptions_ReturnsTrue(Func<bool> tryParse)
        {
            // act
            var testResult = tryParse();

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(InvalidStringTryParsers))]
        public void TryParse_InvalidStringWithOptions_ReturnsFalse(Func<bool> tryParse)
        {
            // act
            var testResult = tryParse();

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(InvalidParses))]
        public void Parse_InvalidValueWithOptions_ThrowsFormatException(Action parse)
        {
            // act
            Action testAction = parse;

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(NullArguments))]
        public void Parse_NullArgument_ThrowsArgumentNullException(Action parse, string expectedParameterName)
        {
            // act
            Action testAction = parse;

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(expectedParameterName);
        }

        [Theory]
        [MemberData(nameof(MailboxesAsHeld))]
        public void ToString_Parsed_GivesWhatItHolds(string value, string expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(value);

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(MailboxesAsEncodedWords))]
        public void ToString_EncodedWords_GivesEncodedWords(string value, string expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(value);

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.EncodedWords);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(MailboxesAsUtf8))]
        public void ToString_Utf8_GivesUtf8(string value, string expected)
        {
            // arrange
            var subject = ImfMailbox.Parse(value);

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.Utf8);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(PeriodsByEncodedWords))]
        public void ToString_PeriodByEncodedWord_ReadsBackWithTheSameDisplayName(string value, string expected)
        {
            // arrange
            var written = ImfMailbox.Parse(value).ToString();

            // act
            var testResult = ImfMailbox.Parse(written).DisplayName;

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_ConstructedNonAsciiWithEncodedWords_GivesEncodedWords()
        {
            // arrange
            var subject = new ImfMailbox("caf" + EAcute, "c", "d");

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.EncodedWords);

            // assert
            testResult.Should().Be("=?UTF-8?B?Y2Fmw6k=?= <c@d>");
        }

        [Fact]
        public void ToString_GroupWithEncodedWords_EncodesEveryDisplayName()
        {
            // arrange
            var subject = ImfGroup.Parse(EAcute + ": " + EAcute + " <c@d>, e@f;");

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.EncodedWords);

            // assert
            testResult.Should().Be(WrittenEAcute + ": " + WrittenEAcute + " <c@d>, e@f;");
        }

        [Fact]
        public void ToString_GroupWithUtf8_WritesEveryDisplayNameAsItsText()
        {
            // arrange
            var subject = ImfGroup.Parse(EncodedEAcute + ": " + EncodedEAcute + " <c@d>;");

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.Utf8);

            // assert
            testResult.Should().Be(EAcute + ": " + EAcute + " <c@d>;");
        }

        [Fact]
        public void ToString_AddressListWithEncodedWords_EncodesEveryDisplayName()
        {
            // arrange
            var subject = ImfAddressList.Parse(EAcute + " <c@d>, " + EAcute + ":;");

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.EncodedWords);

            // assert
            testResult.Should().Be(WrittenEAcute + " <c@d>, " + WrittenEAcute + ":;");
        }

        [Fact]
        public void ToString_AddressListParsed_KeepsWordsAsWritten()
        {
            // arrange
            var subject = ImfAddressList.Parse("=?iso-8859-1?Q?=E9?= <c@d>, e@f");

            // act
            var testResult = subject.ToString();

            // assert
            testResult.Should().Be("=?iso-8859-1?Q?=E9?= <c@d>, e@f");
        }

        [Fact]
        public void ToString_PhraseListWithEncodedWords_EncodesEveryPhrase()
        {
            // arrange
            var subject = ImfPhraseList.Parse(EAcute + ", a b, " + EncodedA);

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.EncodedWords);

            // assert
            testResult.Should().Be(WrittenEAcute + ", a b, a");
        }

        [Fact]
        public void ToString_PhraseListWithUtf8_WritesEveryPhraseAsItsText()
        {
            // arrange
            var subject = ImfPhraseList.Parse(EncodedEAcute + ", a b");

            // act
            var testResult = subject.ToString(ImfHeaderEncoding.Utf8);

            // assert
            testResult.Should().Be(EAcute + ", a b");
        }

        [Theory]
        [MemberData(nameof(UnknownEncodings))]
        public void ToString_UnknownEncoding_ThrowsArgumentOutOfRangeException(Func<string> format)
        {
            // act
            Action testAction = () => format();

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("encoding");
        }

        [Fact]
        public void WriteField_LongDisplayNameAsEncodedWords_FoldsWithinTheLineLimitAndReadsBack()
        {
            // arrange
            var displayName = new string('\x00E9', 100);
            var value = new ImfMailbox(displayName, "c", "d").ToString(ImfHeaderEncoding.EncodedWords);
            var written = MessageWrites.WriteField("From", value, ImfHeaderFieldKind.Structured);

            // act
            var testResult = ImfMailbox.Parse(EncodedWordFields.ReadValue(written)).DisplayName;

            // assert
            EncodedWordFields.LongestLineLength(written).Should().BeLessThanOrEqualTo(TargetLineLength);
            testResult.Should().Be(displayName);
        }

        [Fact]
        public void WriteField_LongDisplayNameThatNeedsQuotingAsEncodedWords_ReadsBack()
        {
            // arrange
            var displayName = "\"Doe, " + new string('\x00E9', 60) + "\" and " + new string('x', 60);
            var value = new ImfMailbox(displayName, "c", "d").ToString(ImfHeaderEncoding.EncodedWords);
            var written = MessageWrites.WriteField("From", value, ImfHeaderFieldKind.Structured);

            // act
            var testResult = ImfMailbox.Parse(EncodedWordFields.ReadValue(written)).DisplayName;

            // assert
            EncodedWordFields.LongestLineLength(written).Should().BeLessThanOrEqualTo(TargetLineLength);
            testResult.Should().Be(displayName);
        }

        [Fact]
        public void WriteField_NonAsciiLocalPartAsEncodedWords_IsRefused()
        {
            // arrange
            var value = new ImfMailbox(EAcute, "c").ToString(ImfHeaderEncoding.EncodedWords);

            // act
            Action testAction = () => MessageWrites.WriteField("From", value, ImfHeaderFieldKind.Structured);

            // assert
            testAction.Should().Throw<ImfFileException>();
        }

        [Fact]
        public void WriteField_LongCommentOfEncodedWords_FoldsWithinTheLineLimitAndReadsBack()
        {
            // arrange
            var text = new string('\x00E9', 100);
            var value = "a@b " + ImfHeaderText.FormatComment(text, ImfHeaderEncoding.EncodedWords);
            var written = MessageWrites.WriteField("From", value, ImfHeaderFieldKind.Structured);

            // act
            var testResult = ImfHeaderText.GetComments(EncodedWordFields.ReadValue(written));

            // assert
            EncodedWordFields.LongestLineLength(written).Should().BeLessThanOrEqualTo(TargetLineLength);
            testResult.Should().Equal(text);
        }

        private static TheoryData<string, string> FormatColumn(int column)
        {
            var data = new TheoryData<string, string>();
            foreach (var row in FormattedMailboxes)
            {
                data.Add(row[0], row[column]);
            }

            return data;
        }
    }
}
