using System;
using System.Linq;
using System.Text;
using DataStandardizer.Communication.Mime;
using DataStandardizer.LanguageTag;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    public class ImfMimeFieldsTests
    {
        // ref. RFC 2045 §4: the four equivalent forms of the field.
        private const string VersionPlain = "1.0";
        private const string VersionCommentAfter = "1.0 (produced by MetaSend Vx.x)";
        private const string VersionCommentBefore = "(produced by MetaSend Vx.x) 1.0";
        private const string VersionCommentInside = "1.(produced by MetaSend Vx.x)0";

        // ref. RFC 2045 §5.1: the three equivalent forms of the field.
        private const string ContentTypePlain = "text/plain; charset=us-ascii";
        private const string ContentTypeWithComment = "text/plain; charset=us-ascii (Plain text)";
        private const string ContentTypeQuoted = "text/plain; charset=\"us-ascii\"";
        private const string ContentTypeCompact = "text/plain;charset=us-ascii";

        // ref. RFC 2231 §3
        private const string Rfc2231Continuation = "message/external-body; access-type=URL;\r\n URL*0=\"ftp://\";\r\n URL*1=\"cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar\"";
        private const string Rfc2231ContinuationUrl = "ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar";

        // ref. RFC 2231 §4
        private const string Rfc2231Extended = "application/x-stuff;\r\n title*=us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A";
        private const string Rfc2231ExtendedTitle = "This is ***fun***";

        // ref. RFC 2231 §4.1
        private const string Rfc2231Combined = "application/x-stuff;\r\n title*0*=us-ascii'en'This%20is%20even%20more%20;\r\n title*1*=%2A%2A%2Afun%2A%2A%2A%20;\r\n title*2=\"isn't it!\"";
        private const string Rfc2231CombinedTitle = "This is even more ***fun*** isn't it!";

        // ref. RFC 3282 §3
        private const string LanguageWithComment = "en, fr (This is a dictionary)";
        private const string LanguageList = "da, de, el, en, fr, it";

        private const string EncodedWordName = "application/pdf; name=\"=?UTF-8?Q?caf=C3=A9.pdf?=\"";
        private const string EncodedWordOther = "application/pdf; other=\"=?UTF-8?Q?caf=C3=A9.pdf?=\"";
        private const string DecodedFileName = "caf\x00E9.pdf";
        private const string EncodedWordText = "=?UTF-8?Q?caf=C3=A9.pdf?=";
        private const string MissingSection = "a/b; c*1=d";
        private const string FinalSemicolon = "a/b;";

        private static readonly ImfOptions MalformedParameters = new ImfOptions { AllowMalformedParameters = true };
        private static readonly ImfOptions InvalidValues = new ImfOptions { AllowInvalidValues = true };

        public static TheoryData<string> ValidVersions => new TheoryData<string>
        {
            VersionPlain,
            VersionCommentAfter,
            VersionCommentBefore,
            VersionCommentInside,
            " 1 . 0 ",
            "1.\r\n 0",
        };

        public static TheoryData<string> InvalidVersions => new TheoryData<string>
        {
            "",
            "1",
            "1.",
            ".0",
            "1 0",
            "a.b",
            "1.0 x",
            "1.0 (x",
            "1.0.0",
            "99999999999.0",
        };

        public static TheoryData<string, string> ValidContentTypes => new TheoryData<string, string>
        {
            // field body, expected compact form
            { ContentTypePlain, ContentTypeCompact },
            { ContentTypeWithComment, ContentTypeCompact },
            { ContentTypeQuoted, ContentTypeCompact },
            { "TEXT/Plain", "TEXT/Plain" },
            { " (a) text (b) / (c) plain (d) ; (e) charset (f) = (g) us-ascii (h) ", ContentTypeCompact },
            { "text/plain;\r\n charset=us-ascii", ContentTypeCompact },
            { "a/b; c=\"d\r\n e\"", "a/b;c=\"d e\"" },
            { "a/b; c=\"d (e) f\"", "a/b;c=\"d (e) f\"" },
            { "a/b; c=\"\\\"\"", "a/b;c=\"\\\"\"" },

            // ref. RFC 2045 §5.1: a name is any token, which is wider than RFC 6838 §4.2 allows.
            { "x~y/z", "x~y/z" },
        };

        public static TheoryData<string> InvalidContentTypes => new TheoryData<string>
        {
            "",
            "(a)",
            "text",
            "text plain",
            "te xt/plain",
            "text/pl ain",
            "text/plain charset=us-ascii",
            "a/b; c=d e",
            "a/b; c=\"d\" e",
            "a/b; c=\"d",
            "a/b; c=\"d\\",
            "a/b; c=\"d\re\"",
            "a/b (c",
            "a/b\re",
            "a/b; c=d; c=e",
            FinalSemicolon,

            // ref. RFC 6532 §3.2 allows this in a quoted-string, but the media type parser takes only US-ASCII.
            "a/b; c=\"\x00E9\"",

            // ref. RFC 2231 §3 and §7
            MissingSection,
            "a/b; c*=d",

            // ref. RFC 2047 §5
            EncodedWordName,
            EncodedWordOther,
        };

        public static TheoryData<string, string> ValidTransferEncodings => new TheoryData<string, string>
        {
            { "base64", "base64" },
            { "BASE64", "base64" },
            { "base64 (binary data)", "base64" },
            { "(a) 7bit", "7bit" },
            { " 8bit ", "8bit" },
            { "\r\n quoted-printable", "quoted-printable" },
            { "x-other", "x-other" },
        };

        public static TheoryData<string> InvalidTransferEncodings => new TheoryData<string>
        {
            "",
            "(a)",
            "base 64",
            "\"base64\"",
            "base64;",
            "base64 (a",
        };

        public static TheoryData<string, string[]> ValidLanguages => new TheoryData<string, string[]>
        {
            // ref. RFC 3282 §3
            { "en-scouse", new[] { "en-scouse" } },
            { "i-mingo", new[] { "i-mingo" } },
            { LanguageWithComment, new[] { "en", "fr" } },
            { LanguageList, new[] { "da", "de", "el", "en", "fr", "it" } },
            { "i-klingon", new[] { "i-klingon" } },

            // ref. RFC 3282 §2: CFWS may go anywhere between the tags.
            { " (a) en (b) ,\r\n (c) fr (d) ", new[] { "en", "fr" } },
            { "es-419", new[] { "es-419" } },
        };

        public static TheoryData<string> MalformedLanguageLists => new TheoryData<string>
        {
            "",
            "(a)",
            ",",
            "en,",
            ",en",
            "en,,fr",
            "en fr",
            "en (a",
        };

        [Theory]
        [MemberData(nameof(ValidVersions))]
        public void ParseMimeVersion_ValidForm_GivesVersion10(string value)
        {
            // act
            var testResult = ImfMimeFields.ParseMimeVersion(value);

            // assert
            testResult.Should().Be(ImfMimeFields.MimeVersion);
        }

        [Fact]
        public void ParseMimeVersion_AnotherVersion_IsRead()
        {
            // act
            var testResult = ImfMimeFields.ParseMimeVersion("2.1");

            // assert
            testResult.Should().Be(new Version(2, 1));
        }

        [Fact]
        public void ParseMimeVersion_Bytes_GivesVersion10()
        {
            // arrange
            var value = Encoding.ASCII.GetBytes(VersionCommentInside);

            // act
            var testResult = ImfMimeFields.ParseMimeVersion(value);

            // assert
            testResult.Should().Be(ImfMimeFields.MimeVersion);
        }

        [Theory]
        [MemberData(nameof(InvalidVersions))]
        public void ParseMimeVersion_InvalidForm_Throws(string value)
        {
            // act
            Action testAction = () => ImfMimeFields.ParseMimeVersion(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ParseMimeVersion_Null_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.ParseMimeVersion((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryParseMimeVersion_Null_ReturnsFalse()
        {
            // act
            var testResult = ImfMimeFields.TryParseMimeVersion((string)null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void MimeVersion_Always_FormatsAsTheFieldBody()
        {
            // act
            var testResult = ImfMimeFields.MimeVersion.ToString();

            // assert
            testResult.Should().Be(VersionPlain);
        }

        [Theory]
        [MemberData(nameof(ValidContentTypes))]
        public void ParseContentType_ValidForm_GivesTheMediaType(string value, string expected)
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(value);

            // assert
            testResult.ToString().Should().Be(expected);
        }

        [Fact]
        public void ParseContentType_Bytes_GivesTheMediaType()
        {
            // arrange
            var value = Encoding.ASCII.GetBytes(ContentTypeWithComment);

            // act
            var testResult = ImfMimeFields.ParseContentType(value);

            // assert
            testResult.ToString().Should().Be(ContentTypeCompact);
        }

        [Theory]
        [MemberData(nameof(InvalidContentTypes))]
        public void ParseContentType_InvalidForm_Throws(string value)
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentType(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ParseContentType_Null_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentType((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ParseContentType_NullOptions_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentType(ContentTypePlain, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryParseContentType_Null_ReturnsFalse()
        {
            // act
            var testResult = ImfMimeFields.TryParseContentType((string)null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseContentType_InvalidUtf8_ReturnsFalse()
        {
            // arrange
            var value = new byte[] { (byte)'a', (byte)'/', (byte)'b', 0xFF };

            // act
            var testResult = ImfMimeFields.TryParseContentType(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ParseContentType_Rfc2231Continuation_JoinsTheSections()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(Rfc2231Continuation);

            // assert
            testResult.TryGetParameter("URL", out var parameter).Should().BeTrue();
            parameter.Value.Should().Be(Rfc2231ContinuationUrl);
        }

        [Fact]
        public void ParseContentType_Rfc2231Extended_DecodesTheValue()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(Rfc2231Extended);

            // assert
            testResult.TryGetParameter("title", out var parameter).Should().BeTrue();
            parameter.Value.Should().Be(Rfc2231ExtendedTitle);
            parameter.Language.Should().Be("en-us");
        }

        [Fact]
        public void ParseContentType_Rfc2231Combined_JoinsAndDecodes()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(Rfc2231Combined);

            // assert
            testResult.TryGetParameter("title", out var parameter).Should().BeTrue();
            parameter.Value.Should().Be(Rfc2231CombinedTitle);
            parameter.Language.Should().Be("en");
        }

        [Fact]
        public void ParseContentType_MissingSectionWithAllowMalformedParameters_JoinsWhatThereIs()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(MissingSection, MalformedParameters);

            // assert
            testResult.TryGetParameter("c", out var parameter).Should().BeTrue();
            parameter.Value.Should().Be("d");
        }

        [Fact]
        public void ParseContentType_EncodedNameWithAllowMalformedParameters_DecodesIt()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(EncodedWordName, MalformedParameters);

            // assert
            testResult.TryGetParameter("name", out var parameter).Should().BeTrue();
            parameter.Value.Should().Be(DecodedFileName);
        }

        [Fact]
        public void ParseContentType_EncodedOtherParameterWithAllowMalformedParameters_LeavesItAsWritten()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(EncodedWordOther, MalformedParameters);

            // assert
            testResult.TryGetParameter("other", out var parameter).Should().BeTrue();
            parameter.Value.Should().Be(EncodedWordText);
        }

        [Fact]
        public void ParseContentType_TextThatOnlyLooksLikeAnEncodedWord_IsAccepted()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType("a/b; c=\"=?x?=\"");

            // assert
            testResult.TryGetParameter("c", out var parameter).Should().BeTrue();
            parameter.Value.Should().Be("=?x?=");
        }

        [Fact]
        public void ParseContentType_FinalSemicolonWithAllowMalformedParameters_IsAccepted()
        {
            // act
            var testResult = ImfMimeFields.ParseContentType(FinalSemicolon, MalformedParameters);

            // assert
            testResult.ToString().Should().Be("a/b");
        }

        [Fact]
        public void ParseContentType_EightBitBytesWithAllowEightBitHeaders_DecodesThemForTheParser()
        {
            // arrange
            var value = new byte[] { (byte)'a', (byte)'/', (byte)'b', (byte)' ', (byte)'(', 0xFF, (byte)')' };
            var options = new ImfOptions { AllowEightBitHeaders = true };

            // act
            var testResult = ImfMimeFields.ParseContentType(value, options);

            // assert
            testResult.ToString().Should().Be("a/b");
        }

        [Fact]
        public void FormatContentType_Parameters_PutsASpaceWhereItCanBeFolded()
        {
            // arrange
            var mediaType = MimeMediaType.TextPlain.WithParameter("charset", "us-ascii").WithParameter("a", "b c");

            // act
            var testResult = ImfMimeFields.FormatContentType(mediaType);

            // assert
            testResult.Should().Be("text/plain; charset=us-ascii; a=\"b c\"");
        }

        [Fact]
        public void FormatContentType_NonAsciiValue_WritesAnExtendedValue()
        {
            // arrange
            var mediaType = MimeMediaType.ApplicationOctetStream.WithParameter("name", DecodedFileName);

            // act
            var testResult = ImfMimeFields.FormatContentType(mediaType);

            // assert
            testResult.Should().Be("application/octet-stream; name*=UTF-8''caf%C3%A9.pdf");
        }

        [Fact]
        public void FormatContentType_Uninitialised_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.FormatContentType(default);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ParseContentType_WrittenByAWriter_GivesTheSameMediaType()
        {
            // arrange
            var mediaType = MimeMediaType.Parse("application/x-stuff").WithParameter("title", Rfc2231CombinedTitle).WithParameter("name", DecodedFileName);
            var written = MimeFieldRoundTrips.WriteAndRead("Content-Type", ImfMimeFields.FormatContentType(mediaType));

            // act
            var testResult = ImfMimeFields.ParseContentType(written);

            // assert
            testResult.Should().Be(mediaType);
        }

        [Theory]
        [MemberData(nameof(ValidTransferEncodings))]
        public void ParseContentTransferEncoding_ValidForm_GivesTheEncoding(string value, string expected)
        {
            // act
            var testResult = ImfMimeFields.ParseContentTransferEncoding(value);

            // assert
            testResult.Name.Should().Be(expected);
        }

        [Fact]
        public void ParseContentTransferEncoding_Bytes_GivesTheEncoding()
        {
            // arrange
            var value = Encoding.ASCII.GetBytes("base64 (a)");

            // act
            var testResult = ImfMimeFields.ParseContentTransferEncoding(value);

            // assert
            testResult.Should().Be(MimeTransferEncoding.Base64);
        }

        [Theory]
        [MemberData(nameof(InvalidTransferEncodings))]
        public void ParseContentTransferEncoding_InvalidForm_Throws(string value)
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentTransferEncoding(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ParseContentTransferEncoding_Null_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentTransferEncoding((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ParseContentTransferEncoding_WrittenByAWriter_GivesTheSameEncoding()
        {
            // arrange
            var written = MimeFieldRoundTrips.WriteAndRead("Content-Transfer-Encoding", MimeTransferEncoding.QuotedPrintable.ToString());

            // act
            var testResult = ImfMimeFields.ParseContentTransferEncoding(written);

            // assert
            testResult.Should().Be(MimeTransferEncoding.QuotedPrintable);
        }

        [Theory]
        [MemberData(nameof(ValidLanguages))]
        public void ParseContentLanguage_ValidForm_GivesTheTags(string value, string[] expected)
        {
            // act
            var testResult = ImfMimeFields.ParseContentLanguage(value);

            // assert
            testResult.Select(tag => tag.ToString()).Should().Equal(expected);
        }

        [Fact]
        public void ParseContentLanguage_Bytes_GivesTheTags()
        {
            // arrange
            var value = Encoding.ASCII.GetBytes(LanguageWithComment);

            // act
            var testResult = ImfMimeFields.ParseContentLanguage(value);

            // assert
            testResult.Select(tag => tag.ToString()).Should().Equal("en", "fr");
        }

        [Theory]
        [MemberData(nameof(MalformedLanguageLists))]
        public void ParseContentLanguage_MalformedList_Throws(string value)
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentLanguage(value);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Theory]
        [MemberData(nameof(MalformedLanguageLists))]
        public void ParseContentLanguage_MalformedListWithAllowInvalidValues_StillThrows(string value)
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentLanguage(value, InvalidValues);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ParseContentLanguage_InvalidTag_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentLanguage("en, en_US");

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void ParseContentLanguage_InvalidTagWithAllowInvalidValues_LeavesItOut()
        {
            // act
            var testResult = ImfMimeFields.ParseContentLanguage("en, en_US, fr", InvalidValues);

            // assert
            testResult.Select(tag => tag.ToString()).Should().Equal("en", "fr");
        }

        [Fact]
        public void ParseContentLanguage_Null_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.ParseContentLanguage((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryParseContentLanguage_Null_ReturnsFalse()
        {
            // act
            var testResult = ImfMimeFields.TryParseContentLanguage((string)null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void FormatContentLanguage_Tags_SeparatesThemWithACommaAndASpace()
        {
            // arrange
            var tags = new[] { Bcp47LanguageTag.Create("da"), Bcp47LanguageTag.Create("de"), Bcp47LanguageTag.Create("el"), Bcp47LanguageTag.Create("en"), Bcp47LanguageTag.Create("fr"), Bcp47LanguageTag.Create("it") };

            // act
            var testResult = ImfMimeFields.FormatContentLanguage(tags);

            // assert
            testResult.Should().Be(LanguageList);
        }

        [Fact]
        public void FormatContentLanguage_NoTags_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.FormatContentLanguage(new Bcp47LanguageTag[0]);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FormatContentLanguage_UninitialisedTag_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.FormatContentLanguage(new[] { default(Bcp47LanguageTag) });

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FormatContentLanguage_Null_Throws()
        {
            // act
            Action testAction = () => ImfMimeFields.FormatContentLanguage(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void ParseContentLanguage_WrittenByAWriter_GivesTheSameTags()
        {
            // arrange
            var tags = new[] { Bcp47LanguageTag.Create("da"), Bcp47LanguageTag.Create("de"), Bcp47LanguageTag.Create("el"), Bcp47LanguageTag.Create("en"), Bcp47LanguageTag.Create("fr"), Bcp47LanguageTag.Create("it") };
            var written = MimeFieldRoundTrips.WriteAndRead("Content-Language", ImfMimeFields.FormatContentLanguage(tags));

            // act
            var testResult = ImfMimeFields.ParseContentLanguage(written);

            // assert
            testResult.Should().Equal(tags);
        }
    }
}
