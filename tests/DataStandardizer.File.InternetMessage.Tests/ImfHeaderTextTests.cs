using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfHeaderTextTests
    {
        private const string EncodedA = "=?UTF-8?Q?a?=";

        // ref. RFC 2047 §4: é as UTF-8 is the octets C3 A9, which are shorter in the "B" encoding, and café likewise.
        private const string WrittenEAcute = "=?UTF-8?B?w6k=?=";
        private const string WrittenCafe = "=?UTF-8?B?Y2Fmw6k=?=";
        private const string EncodedEAcute = "=?UTF-8?Q?=C3=A9?=";
        private const string EAcute = "\x00E9";

        // ref. RFC 2047 §2: an encoded word is at most 75 characters long, and this is 82.
        private const string LongWordText = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string LongWord = "=?UTF-8?Q?" + LongWordText + "?=";

        // ref. RFC 2047 §4.2: the bytes of the text =?UTF-8?Q?a?=, in the encoding that is shortest for it.
        private const string EncodedTextOfEncodedA = "=?UTF-8?B?PT9VVEYtOD9RP2E/PQ==?=";

        public static TheoryData<string, string> UnstructuredValues => new TheoryData<string, string>
        {
            // ref. RFC 2047 §8
            { Rfc2047Examples.ExampleSubject, Rfc2047Examples.ExampleSubjectText },
            { Rfc2047Examples.OlleSubject, Rfc2047Examples.OlleSubject },
            { Rfc2047Examples.PatrikSubject, Rfc2047Examples.PatrikSubject },

            // ref. RFC 2047 §8: in unstructured text a parenthesis is not a delimiter, so a word next to one is ordinary text.
            { Rfc2047Examples.TableOneWord, Rfc2047Examples.TableOneWord },

            // ref. RFC 2047 §6.2: white space between encoded words goes, and any other stays.
            { EncodedA + " " + EncodedA, "aa" },
            { EncodedA + " \t " + EncodedA, "aa" },
            { EncodedA + " b", "a b" },
            { "b  " + EncodedA, "b  a" },
            { "b " + EncodedA + " c", "b a c" },

            // The white space around the body isn't part of the text, and a fold goes.
            { " a", "a" },
            { "\t a \t", "a" },
            { "", "" },
            { "   ", "" },
            { " a\r\n b", "a b" },
            { " " + EncodedA + "\r\n " + EncodedA, "aa" },

            // White space in an encoded word is text.
            { "=?UTF-8?Q?_a_?=", " a " },

            // ref. RFC 6532 §3.2
            { " caf" + EAcute, "caf" + EAcute },
        };

        // Each breaks a rule of RFC 2047, and what it reads as with AllowMalformedEncodedWords.
        public static TheoryData<string, string> MalformedWords => new TheoryData<string, string>
        {
            // ref. RFC 2047 §2
            { LongWord, LongWordText },

            // ref. RFC 2047 §5
            { "x" + EncodedA, "xa" },

            // ref. RFC 2047 §6.2
            { "=?x-unknown?Q?a?=", "a" },

            // ref. RFC 2047 §4.2
            { "=?UTF-8?Q?=c3=a9?=", EAcute },

            // ref. RFC 2047 §6.3 (2)
            { "=?UTF-8?Q?=FF?=", "\xFFFD" },

            // ref. RFC 2047 §5
            { "=?UTF-8?Q?=C3?= =?UTF-8?Q?=A9?=", EAcute },
        };

        public static TheoryData<string> MalformedWordValues => new TheoryData<string>
        {
            LongWord,
            "x" + EncodedA,
            "=?x-unknown?Q?a?=",
            "=?UTF-8?Q?=c3=a9?=",
            "=?UTF-8?Q?=FF?=",
            "=?UTF-8?Q?=C3?=",
        };

        public static TheoryData<string, string[]> Comments => new TheoryData<string, string[]>
        {
            // ref. RFC 2047 §8
            { Rfc2047Examples.NathanielFrom, new[] { Rfc2047Examples.NathanielComment } },
            { Rfc2047Examples.TableOneWord, new[] { "a" } },
            { Rfc2047Examples.TableWordAndText, new[] { "a b" } },
            { Rfc2047Examples.TableTwoWords, new[] { "ab" } },
            { Rfc2047Examples.TableTwoSpaces, new[] { "ab" } },
            { Rfc2047Examples.TableFolded, new[] { "ab" } },
            { Rfc2047Examples.TableEncodedSpace, new[] { "a b" } },
            { Rfc2047Examples.TableSpaceInSecondWord, new[] { "a b" } },

            // No comments, and more than one.
            { "", new string[0] },
            { "a@b", new string[0] },
            { "(a) b (c)", new[] { "a", "c" } },
            { "()", new[] { "" } },

            // ref. RFC 5322 §3.2.2: a nested comment, with a word in each.
            { "(a (b) c)", new[] { "a (b) c" } },
            { "(" + EncodedA + " (" + EncodedA + ") " + EncodedA + ")", new[] { "a (a) a" } },

            // A quoted-pair stands for its character, and can't be part of an encoded word.
            { "(a \\( \\\\b)", new[] { "a ( \\b" } },
            { "(\\" + EncodedA + ")", new[] { EncodedA } },

            // The fold goes, and the white space stays.
            { "(a\r\n b)", new[] { "a b" } },
            { "( a )", new[] { " a " } },

            // ref. RFC 2047 §5: not in anything but a comment.
            { "\"(" + EncodedA + ")\"", new string[0] },
            { "\"a\" (" + EncodedA + ")", new[] { "a" } },
        };

        public static TheoryData<string> InvalidCommentValues => new TheoryData<string>
        {
            "(a",
            "a)",
            "\"a",
            "\x0000",
        };

        public static TheoryData<string, ImfHeaderEncoding, string> FormattedUnstructured => new TheoryData<string, ImfHeaderEncoding, string>
        {
            { "", ImfHeaderEncoding.EncodedWords, "" },
            { "a b", ImfHeaderEncoding.EncodedWords, "a b" },
            { "a(b)\\", ImfHeaderEncoding.EncodedWords, "a(b)\\" },
            { "caf" + EAcute, ImfHeaderEncoding.EncodedWords, WrittenCafe },

            // Only the words that need it, and the white space around them stays.
            { "Re: caf" + EAcute + " au  lait", ImfHeaderEncoding.EncodedWords, "Re: " + WrittenCafe + " au  lait" },
            { " " + EAcute + "\t", ImfHeaderEncoding.EncodedWords, " " + WrittenEAcute + "\t" },
            { EAcute + " a " + EAcute, ImfHeaderEncoding.EncodedWords, WrittenEAcute + " a " + WrittenEAcute },

            // ref. RFC 2047 §6.2: white space between encoded words would be lost, so it goes in them.
            { EAcute + " " + EAcute, ImfHeaderEncoding.EncodedWords, "=?UTF-8?B?w6kgw6k=?=" },
            { EAcute + "  " + EAcute, ImfHeaderEncoding.EncodedWords, "=?UTF-8?B?w6kgIMOp?=" },

            // Text that looks like an encoded word has to stay text, whichever the encoding.
            { EncodedA, ImfHeaderEncoding.EncodedWords, EncodedTextOfEncodedA },
            { EncodedA, ImfHeaderEncoding.Utf8, EncodedTextOfEncodedA },
            { "x=?y", ImfHeaderEncoding.EncodedWords, "=?UTF-8?Q?x=3D=3Fy?=" },
            { "=x?y", ImfHeaderEncoding.EncodedWords, "=x?y" },

            // ref. RFC 6532 §3.2
            { "caf" + EAcute, ImfHeaderEncoding.Utf8, "caf" + EAcute },
            { EAcute + " " + EAcute, ImfHeaderEncoding.Utf8, EAcute + " " + EAcute },
        };

        public static TheoryData<string, ImfHeaderEncoding, string> FormattedComments => new TheoryData<string, ImfHeaderEncoding, string>
        {
            { "", ImfHeaderEncoding.EncodedWords, "()" },
            { "a b", ImfHeaderEncoding.EncodedWords, "(a b)" },

            // ref. RFC 5322 §3.2.2: ctext has no parenthesis or backslash.
            { "a (b)\\", ImfHeaderEncoding.EncodedWords, "(a \\(b\\)\\\\)" },
            { "a (b)\\", ImfHeaderEncoding.Utf8, "(a \\(b\\)\\\\)" },

            // ref. RFC 2047 §5(2): in a comment, "Q"-encoded text has no parenthesis, so one in a word that is encoded is encoded too.  Here "Q" is the shorter.
            { EAcute + "(aaaaaaaaaaaaaaaaaaaaaaaa)", ImfHeaderEncoding.EncodedWords, "(=?UTF-8?Q?=C3=A9=28aaaaaaaaaaaaaaaaaaaaaaaa=29?=)" },
            { EAcute + " (a)", ImfHeaderEncoding.EncodedWords, "(" + WrittenEAcute + " \\(a\\))" },

            // ref. RFC 6532 §3.2
            { "caf" + EAcute, ImfHeaderEncoding.Utf8, "(caf" + EAcute + ")" },
        };

        public static TheoryData<string> RoundTripTexts => new TheoryData<string>
        {
            "a",
            "a  b",
            "a (b) \\ c",
            EAcute,
            "a " + EAcute + "  " + EAcute + " b",
            EAcute + " (" + EAcute + ")",
            EncodedA,
            "x " + EncodedA + " y",
            "\xD83D\xDE00",
            "=?",
        };

        public static TheoryData<string> UnwritableTexts => new TheoryData<string>
        {
            "a\r\nBcc: b@c",
            "a\nb",
            "a\x0000",
            "a\x007F",
        };

        public static TheoryData<Func<string>> UnpairedSurrogateCalls => new TheoryData<Func<string>>
        {
            () => ImfHeaderText.FormatUnstructured("\xD83D", ImfHeaderEncoding.Utf8),
            () => ImfHeaderText.FormatComment("\xD83D", ImfHeaderEncoding.Utf8),
        };

        [Theory]
        [MemberData(nameof(UnstructuredValues))]
        public void DecodeUnstructured_String_GivesTheText(string value, string expected)
        {
            // act
            var testResult = ImfHeaderText.DecodeUnstructured(value, Rfc2047Examples.Options);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void DecodeUnstructured_StringWithDefaultOptions_DecodesWords()
        {
            // act
            var testResult = ImfHeaderText.DecodeUnstructured(EncodedA);

            // assert
            testResult.Should().Be("a");
        }

        [Fact]
        public void DecodeUnstructured_Bytes_DecodesWordsAndUtf8()
        {
            // arrange
            var value = Octets.Of("caf\x00C3\x00A9 " + EncodedA);

            // act
            var testResult = ImfHeaderText.DecodeUnstructured(value);

            // assert
            testResult.Should().Be("caf" + EAcute + " a");
        }

        [Fact]
        public void DecodeUnstructured_BytesThatAreNotUtf8_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("caf\x00E9");

            // act
            Action testAction = () => ImfHeaderText.DecodeUnstructured(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void DecodeUnstructured_BytesThatAreNotUtf8WithAllowEightBitHeaders_UsesTheFallbackBeforeTheWords()
        {
            // arrange
            var value = Octets.Of("caf\x00E9 " + EncodedEAcute);
            var options = new ImfOptions { AllowEightBitHeaders = true };

            // act
            var testResult = ImfHeaderText.DecodeUnstructured(value, options);

            // assert
            testResult.Should().Be("caf" + EAcute + " " + EAcute);
        }

        [Theory]
        [MemberData(nameof(MalformedWordValues))]
        public void DecodeUnstructured_MalformedWordByDefault_StaysAsItIs(string value)
        {
            // act
            var testResult = ImfHeaderText.DecodeUnstructured(value);

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void DecodeUnstructured_SplitCharacterByDefault_StaysAsItIs()
        {
            // act
            var testResult = ImfHeaderText.DecodeUnstructured("=?UTF-8?Q?=C3?= =?UTF-8?Q?=A9?=");

            // assert
            testResult.Should().Be("=?UTF-8?Q?=C3?= =?UTF-8?Q?=A9?=");
        }

        [Theory]
        [MemberData(nameof(MalformedWords))]
        public void DecodeUnstructured_MalformedWordWithOptIn_DecodesIt(string value, string expected)
        {
            // arrange
            var options = new ImfOptions { AllowMalformedEncodedWords = true };

            // act
            var testResult = ImfHeaderText.DecodeUnstructured(value, options);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void DecodeUnstructured_NullString_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => ImfHeaderText.DecodeUnstructured((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("value");
        }

        [Fact]
        public void DecodeUnstructured_NullOptions_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => ImfHeaderText.DecodeUnstructured("a", null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void DecodeUnstructured_NoCharsetResolver_ThrowsArgumentException()
        {
            // arrange
            var options = new ImfOptions { CharsetResolver = null };

            // act
            Action testAction = () => ImfHeaderText.DecodeUnstructured("a", options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        [Theory]
        [MemberData(nameof(Comments))]
        public void GetComments_String_GivesTheirText(string value, string[] expected)
        {
            // act
            var testResult = ImfHeaderText.GetComments(value, Rfc2047Examples.Options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void GetComments_Bytes_GivesTheirText()
        {
            // arrange
            var value = Octets.Of("a (" + EncodedA + " b)");

            // act
            var testResult = ImfHeaderText.GetComments(value);

            // assert
            testResult.Should().Equal("a b");
        }

        [Fact]
        public void GetComments_StringWithDefaultOptions_GivesTheirText()
        {
            // act
            var testResult = ImfHeaderText.GetComments("(" + EncodedA + ")");

            // assert
            testResult.Should().Equal("a");
        }

        [Fact]
        public void GetComments_MalformedWordByDefault_StaysAsItIs()
        {
            // act
            var testResult = ImfHeaderText.GetComments("(=?x-unknown?Q?a?=)");

            // assert
            testResult.Should().Equal("=?x-unknown?Q?a?=");
        }

        [Fact]
        public void GetComments_MalformedWordWithOptIn_DecodesIt()
        {
            // arrange
            var options = new ImfOptions { AllowMalformedEncodedWords = true };

            // act
            var testResult = ImfHeaderText.GetComments("(=?x-unknown?Q?a?=)", options);

            // assert
            testResult.Should().Equal("a");
        }

        [Fact]
        public void GetComments_BytesThatAreNotUtf8ByDefault_ThrowsFormatException()
        {
            // arrange
            var value = Octets.Of("(caf\x00E9)");

            // act
            Action testAction = () => ImfHeaderText.GetComments(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void GetComments_BytesThatAreNotUtf8WithAllowEightBitHeaders_UsesTheFallbackBeforeTheWords()
        {
            // arrange
            var value = Octets.Of("(caf\x00E9 " + EncodedA + ")");
            var options = new ImfOptions { AllowEightBitHeaders = true };

            // act
            var testResult = ImfHeaderText.GetComments(value, options);

            // assert
            testResult.Should().Equal("caf" + EAcute + " a");
        }

        [Theory]
        [MemberData(nameof(InvalidCommentValues))]
        public void GetComments_InvalidValue_ThrowsFormatException(string value)
        {
            // act
            Action testAction = () => ImfHeaderText.GetComments(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(InvalidCommentValues))]
        public void TryGetComments_InvalidValue_ReturnsFalse(string value)
        {
            // act
            var testResult = ImfHeaderText.TryGetComments(value, new ImfOptions(), out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryGetComments_Valid_ReturnsTrueAndTheirText()
        {
            // act
            var testResult = ImfHeaderText.TryGetComments("(" + EncodedA + ")", new ImfOptions(), out var comments);

            // assert
            testResult.Should().BeTrue();
            comments.Should().Equal("a");
        }

        [Fact]
        public void TryGetComments_NullString_ReturnsFalse()
        {
            // act
            var testResult = ImfHeaderText.TryGetComments((string)null, new ImfOptions(), out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryGetComments_NullOptions_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => ImfHeaderText.TryGetComments("a", null, out _);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void GetComments_Result_CannotBeCastBackAndChanged()
        {
            // act
            var testResult = ImfHeaderText.GetComments("(a)");

            // assert
            testResult.Should().NotBeAssignableTo<string[]>();
            testResult.Should().NotBeAssignableTo<List<string>>();
        }

        [Fact]
        public void GetComments_NullString_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => ImfHeaderText.GetComments((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("value");
        }

        [Theory]
        [MemberData(nameof(FormattedUnstructured))]
        public void FormatUnstructured_Text_GivesTheFieldBody(string text, ImfHeaderEncoding encoding, string expected)
        {
            // act
            var testResult = ImfHeaderText.FormatUnstructured(text, encoding);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(FormattedComments))]
        public void FormatComment_Text_GivesTheComment(string text, ImfHeaderEncoding encoding, string expected)
        {
            // act
            var testResult = ImfHeaderText.FormatComment(text, encoding);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(RoundTripTexts))]
        public void FormatUnstructured_ThenDecodeUnstructured_GivesTheSameText(string text)
        {
            // arrange
            var body = ImfHeaderText.FormatUnstructured(text, ImfHeaderEncoding.EncodedWords);

            // act
            var testResult = ImfHeaderText.DecodeUnstructured(body);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(RoundTripTexts))]
        public void FormatUnstructured_Utf8ThenDecodeUnstructured_GivesTheSameText(string text)
        {
            // arrange
            var body = ImfHeaderText.FormatUnstructured(text, ImfHeaderEncoding.Utf8);

            // act
            var testResult = ImfHeaderText.DecodeUnstructured(body);

            // assert
            testResult.Should().Be(text);
        }

        [Theory]
        [MemberData(nameof(RoundTripTexts))]
        public void FormatComment_ThenGetComments_GivesTheSameText(string text)
        {
            // arrange
            var comment = ImfHeaderText.FormatComment(text, ImfHeaderEncoding.EncodedWords);

            // act
            var testResult = ImfHeaderText.GetComments(comment);

            // assert
            testResult.Should().Equal(text);
        }

        [Theory]
        [MemberData(nameof(UnwritableTexts))]
        public void FormatUnstructured_UnwritableText_ThrowsArgumentException(string text)
        {
            // act
            Action testAction = () => ImfHeaderText.FormatUnstructured(text, ImfHeaderEncoding.EncodedWords);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("text");
        }

        [Theory]
        [MemberData(nameof(UnwritableTexts))]
        public void FormatComment_UnwritableText_ThrowsArgumentException(string text)
        {
            // act
            Action testAction = () => ImfHeaderText.FormatComment(text, ImfHeaderEncoding.EncodedWords);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("text");
        }

        [Theory]
        [MemberData(nameof(UnpairedSurrogateCalls))]
        public void Format_UnpairedSurrogate_ThrowsArgumentException(Func<string> call)
        {
            // act
            Action testAction = () => call();

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("text");
        }

        [Fact]
        public void FormatUnstructured_NullText_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => ImfHeaderText.FormatUnstructured(null, ImfHeaderEncoding.EncodedWords);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("text");
        }

        [Fact]
        public void FormatComment_NullText_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => ImfHeaderText.FormatComment(null, ImfHeaderEncoding.EncodedWords);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("text");
        }

        [Fact]
        public void FormatUnstructured_UnknownEncoding_ThrowsArgumentOutOfRangeException()
        {
            // act
            Action testAction = () => ImfHeaderText.FormatUnstructured("a", (ImfHeaderEncoding)2);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("encoding");
        }

        [Fact]
        public void FormatComment_UnknownEncoding_ThrowsArgumentOutOfRangeException()
        {
            // act
            Action testAction = () => ImfHeaderText.FormatComment("a", (ImfHeaderEncoding)2);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("encoding");
        }
    }
}
