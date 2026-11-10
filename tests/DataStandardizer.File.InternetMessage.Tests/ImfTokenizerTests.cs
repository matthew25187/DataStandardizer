using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfTokenizerTests
    {
        private const int DeepNesting = 100000;
        private const string EncodedWord = "=?utf-8?Q?a?=";

        public static TheoryData<string, string> Sequences => new TheoryData<string, string>
        {
            { "", "" },
            { " \t", "" },
            { "a", "Atom" },
            { "a.b", "Atom Period Atom" },
            { "a . b", "Atom +Period +Atom" },
            { "\"a b\"", "QuotedString" },
            { "[1.2.3.4]", "DomainLiteral" },
            { "<>@,:;.", "LessThan GreaterThan At Comma Colon Semicolon Period" },

            // ref. RFC 5322 §3.2.3: atext has these, and no others of the printable US-ASCII characters.
            { "!#$%&'*+-/=?^_`{|}~", "Atom" },

            // ref. RFC 6532 §3.2: atext takes UTF8-non-ascii.
            { "jos\x00E9", "Atom" },

            // ref. RFC 5322 §3.2.2: a comment is not a token here, but it does separate tokens.
            { "a(b)c", "Atom +Atom" },
            { "(a)", "" },
            { "(a(b(c)d)e)", "" },
            { "(\\))a", "+Atom" },
            { "(\\()a", "+Atom" },
            { "(\x00E9)a", "+Atom" },

            // ref. RFC 5322 §3.2.2: FWS
            { "a\r\n b", "Atom +Atom" },
            { "a\r\n\tb", "Atom +Atom" },

            // ref. RFC 5322 §4.2: obs-FWS can have lines of only white space.
            { "a \r\n \r\n b", "Atom +Atom" },

            // ref. RFC 5322 §3.2.4: a fold, an escaped quote and white space in a quoted-string
            { "\"a\r\n b\"", "QuotedString" },
            { "\"a\\\"b\"", "QuotedString" },

            // ref. RFC 5322 §4.1: obs-qtext, obs-ctext and obs-dtext are the controls but NUL, and obs-qp is a quoted-pair of NUL, CR, LF or another control.
            { "\"\x0001\x007F\"", "QuotedString" },
            { "(\x0001\x007F)", "" },
            { "[\x0001\x007F]", "DomainLiteral" },
            { "\"\\\0\\\r\\\n\\\x0001\"", "QuotedString" },
            { "(\\\0\\\r\\\n)", "" },

            // ref. RFC 5322 §4.4: obs-dtext has quoted-pairs too.
            { "[\\]]", "DomainLiteral" },
        };

        public static TheoryData<string> InvalidSequences => new TheoryData<string>
        {
            // Unbalanced comments.
            "(",
            "(a",
            "(a(b)",
            ")",
            "(\\",
            "(\\)",

            // An unterminated quoted-string or domain-literal.
            "\"",
            "\"a",
            "\"a\\\"",
            "\"a\\",
            "[",
            "[a",
            "[a\\]",
            "]",

            // ref. RFC 5322 §3.4.1: dtext has no opening bracket.
            "[[]",

            // A character that is no part of any token.
            "\\",
            "a\\",
            "\0",
            "\x0001",
            "\x007F",

            // ref. RFC 5322 §2.2: CR and LF only in a fold, which has white space after it.
            "\r",
            "\n",
            "a\r\n",
            "a\r\nb",
            "a\rb",
            "a\nb",
            "(a\nb)",
            "(a\r\nb)",
            "\"a\nb\"",
            "\"a\r\nb\"",
            "\"a\rb\"",
            "[a\nb]",
            "[a\r\nb]",

            // ref. RFC 5322 §4.1: NUL is only ever in a quoted-pair.
            "\"\0\"",
            "(\0)",
            "[\0]",
        };

        public static TheoryData<string, string> Texts => new TheoryData<string, string>
        {
            { "abc", "abc" },
            { "jos\x00E9", "jos\x00E9" },

            // ref. RFC 5322 §3.2.4: the quotes are not part of the string.
            { "\"\"", "" },
            { "\"a b\"", "a b" },

            // ref. RFC 5322 §3.2.1: the backslash of a quoted-pair is invisible.
            { "\"a\\\"b\"", "a\"b" },
            { "\"a\\\\b\"", "a\\b" },
            { "\"\\a\"", "a" },
            { "\"\\\x00E9\"", "\x00E9" },

            // ref. RFC 5322 §3.2.4: the line break of a fold is invisible, and its white space is not.
            { "\"a\r\n b\"", "a b" },
            { "\"a \r\n\tb\"", "a \tb" },

            // ref. RFC 5322 §4.1: obs-qp
            { "\"a\\\nb\"", "a\nb" },
            { "\"a\\\rb\"", "a\rb" },
            { "\"a\\\0b\"", "a\0b" },

            // ref. RFC 5322 §3.4.1: a domain-literal keeps its brackets.
            { "[1.2.3.4]", "[1.2.3.4]" },
            { "[]", "[]" },
            { "[ 1.2.3.4 ]", "[1.2.3.4]" },
            { "[\r\n 1.2.3.4\r\n ]", "[1.2.3.4]" },
            { "[a \r\n b]", "[a b]" },

            // ref. RFC 5322 §4.4: obs-dtext
            { "[a\\]b]", "[a]b]" },
        };

        public static TheoryData<string, string> SequencesWithComments => new TheoryData<string, string>
        {
            { "a", "Atom[a]" },

            // ref. RFC 2047 §5(2): an encoded word may be in a comment, so a comment has to be reported, with what is nested in it.
            { "(" + EncodedWord + ")", "Comment[(" + EncodedWord + ")]" },
            { "a (b(c)) d", "Atom[a] Comment[(b(c))] Atom[d]" },
        };

        public static TheoryData<string, string> Phrases => new TheoryData<string, string>
        {
            { "John", "John|False|End" },
            { "John Doe <", "John Doe|False|LessThan" },
            { "\"John Doe\":", "John Doe|False|Colon" },
            { "John  (a)  Doe", "John Doe|False|End" },
            { "\"a\"\"b\"", "a b|False|End" },
            { "a\"b\"", "a b|False|End" },

            // ref. RFC 5322 §4.1: obs-phrase
            { "Joe Q. Public", "Joe Q. Public|False|End" },
            { "a.b", "a.b|False|End" },
            { "a .b", "a .b|False|End" },
            { "a. b", "a. b|False|End" },
            { "a.", "a.|False|End" },

            // ref. RFC 2047 §5(3): an encoded word may replace a word of a phrase, but not be in a quoted-string.
            { EncodedWord, "a|True|End" },
            { "b " + EncodedWord + " c", "b a c|True|End" },
            { "\"" + EncodedWord + "\"", EncodedWord + "|False|End" },
            { "=?a", "=?a|False|End" },
            { "a?=", "a?=|False|End" },
            { "=?=", "=?=|False|End" },
        };

        public static TheoryData<string> NotPhrases => new TheoryData<string>
        {
            "",
            ".",
            ".a",
            "<",
            "a(",
        };

        public static TheoryData<string> DeepComment => new TheoryData<string>
        {
            new string('(', DeepNesting) + new string(')', DeepNesting),
        };

        public static TheoryData<string> DeepUnbalancedComment => new TheoryData<string>
        {
            new string('(', DeepNesting),
        };

        [Theory]
        [MemberData(nameof(Sequences))]
        public void TryRead_ValidInput_GivesTokens(string text, string expected)
        {
            // act
            var testResult = TokenSequences.Read(text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(InvalidSequences))]
        public void TryRead_InvalidInput_ReturnsFalse(string text)
        {
            // act
            var testResult = TokenSequences.Read(text);

            // assert
            testResult.Should().Be(TokenSequences.Invalid);
        }

        [Theory]
        [MemberData(nameof(DeepComment))]
        public void TryRead_DeeplyNestedComment_PassesOverIt(string text)
        {
            // act
            var testResult = TokenSequences.Read(text);

            // assert
            testResult.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(DeepUnbalancedComment))]
        public void TryRead_DeeplyNestedUnbalancedComment_ReturnsFalse(string text)
        {
            // act
            var testResult = TokenSequences.Read(text);

            // assert
            testResult.Should().Be(TokenSequences.Invalid);
        }

        [Theory]
        [MemberData(nameof(SequencesWithComments))]
        public void TryReadWithComments_Input_ReportsComments(string text, string expected)
        {
            // act
            var testResult = TokenSequences.ReadWithComments(text);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void TryReadWithComments_UnbalancedComment_ReturnsFalse()
        {
            // act
            var testResult = TokenSequences.ReadWithComments("(a");

            // assert
            testResult.Should().Be(TokenSequences.Invalid);
        }

        [Fact]
        public void TryPeek_Token_DoesNotMovePastIt()
        {
            // act
            var testResult = TokenSequences.PeekThenRead("a");

            // assert
            testResult.Should().Be("Atom Atom");
        }

        [Theory]
        [MemberData(nameof(Texts))]
        public void GetText_Token_GivesWhatItStandsFor(string text, string expected)
        {
            // act
            var testResult = TokenSequences.GetOnlyText(text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Phrases))]
        public void TryRead_Phrase_GivesItsText(string text, string expected)
        {
            // act
            var testResult = TokenSequences.ReadPhrase(text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(NotPhrases))]
        public void TryRead_NotAPhrase_ReturnsFalse(string text)
        {
            // act
            var testResult = TokenSequences.ReadPhrase(text);

            // assert
            testResult.Should().Be(TokenSequences.Invalid);
        }
    }
}
