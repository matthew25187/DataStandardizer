using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeMediaTypeTests
    {
        // ref. RFC 2046 §4.1, §4.1.3: the text type and its plain subtype, and §4.1.2: its charset parameter.
        private const string Text = "text";
        private const string Plain = "plain";
        private const string TextPlain = "text/plain";
        private const string Charset = "charset";
        private const string UsAscii = "us-ascii";
        private const string Utf8 = "utf-8";

        // ref. RFC 2045 §5.1: the example media type, written without and with quotation marks around the parameter value.
        private const string TextPlainUsAscii = "text/plain;charset=us-ascii";
        private const string TextPlainQuotedUsAscii = "text/plain;charset=\"us-ascii\"";
        private const string TextPlainSpaceUsAscii = "text/plain; charset=us-ascii";

        // ref. RFC 2046 §5.1.1: the multipart type, its mixed subtype and boundary parameter, and the example boundary, which isn't a token.
        private const string Multipart = "multipart";
        private const string Mixed = "mixed";
        private const string Boundary = "boundary";
        private const string MultipartSimpleBoundary = "multipart/mixed;boundary=\"simple boundary\"";

        // ref. RFC 2046 §4.5.1: the application type and its octet-stream subtype.
        private const string Application = "application";
        private const string ApplicationOctetStream = "application/octet-stream";

        // ref. RFC 6839 §3.1: the structured syntax suffix of JSON.
        private const string Json = "json";

        // A parameter name, and media types with quoted parameter values that decode to "a "quoted" \ name", "ab" and an empty value.
        private const string Name = "name";
        private const string QuotedPairsValue = "text/plain;name=\"a \\\"quoted\\\" \\\\ name\"";
        private const string QuotedLettersValue = "text/plain;name=\"\\a\\b\"";
        private const string EmptyQuotedValue = "text/plain;name=\"\"";

        // Media types with short parameters, and a parameter value that isn't a token.
        private const string A = "a";
        private const string B = "b";
        private const string TextPlainA1 = "text/plain;a=1";
        private const string TextPlainA1B2 = "text/plain;a=1;b=2";
        private const string TextPlainQuotedValue = "text/plain;a=\"b c\"";
        private const string TextPlainCharsetAndFormat = "text/plain;charset=us-ascii;format=flowed";

        // Media types with whitespace or a comment where RFC 2045 §5.1 allows none.
        private const string SpaceBeforeType = " text/plain";
        private const string SpaceBeforeSlash = "text /plain";
        private const string SpaceAfterSlash = "text/ plain";
        private const string Comment = "text/plain (comment)";

        // ref. RFC 6838 §4.2: names are at most 127 characters.
        private const string LetterName127 =
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        private const string LetterName128 = LetterName127 + "a";
        private const int MaximumNameLength = 127;
        private const string SubtypeOf127Characters = "application/" + LetterName127;

        // A name of a registered tree with a character that RFC 6838 §4.2 allows, but RFC 2045 §5.1 doesn't need.
        private const string TildeSubtype = "x~y";

        // The lowest bit that no MimeMediaTypeParseOptions member uses.
        private const MimeMediaTypeParseOptions UndefinedOption = (MimeMediaTypeParseOptions)(1 << 9);

        private const MimeMediaTypeParseOptions WhitespaceAndRfc2045Tokens =
            MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters | MimeMediaTypeParseOptions.AllowRfc2045Tokens;

        // "text/plain;a=b" in the middle of other text, at the span from SpanStart of SpanLength characters.
        private const string MediaTypeAmongOtherText = "xx text/plain;a=b xx";
        private const int SpanStart = 3;
        private const int SpanLength = 14;

        // A media type with a parameter value that isn't a token, and so is quoted.
        private const string TextPlainQuotedName = "text/plain;name=\"a b\"";

        #region Test case factories

        public static TheoryData<string> InvalidMediaTypes => new TheoryData<string>
        {
            "",
            Text,
            "text/",
            "/plain",
            "text/plain;",
            "text/plain;charset",
            "text/plain;charset=",
            "text/plain;=us-ascii",
            "text/plain;charset=\"us-ascii",
            "text/plain;charset=\"us-ascii\"x",
            "text/plain;name=\"a\\",
            "text/plain;name=\"caf\x00E9\"",
            "text/plain;name=caf\x00E9",
            "text/plain;name=\"a\rb\"",
            "text/plain;name=a b",
            "text/plain;name=a/b",
            "text/plain/x",
            "text/plain;charset=us-ascii;Charset=utf-8",
            Comment,
            "text/plain;charset=us-ascii (Plain text)",
            SpaceBeforeType,
            SpaceBeforeSlash,
            SpaceAfterSlash,
            "text/plain ;charset=us-ascii",
            TextPlainSpaceUsAscii,
            "text/plain;charset =us-ascii",
            "text/plain;charset= us-ascii",
            "text/plain ",
        };

        public static TheoryData<string> Rfc2045TokensThatAreNotRfc6838Names => new TheoryData<string>
        {
            "application/*",
            "application/x~y",
            "application/-dash",
            "application/.dot",
            "x{y}/plain",
            "text/plain;na|me=x",
        };

        // A type, a subtype and a parameter name, each of 128 characters, one more than RFC 6838 §4.2 allows.
        public static TheoryData<string> NamesOver127Characters => new TheoryData<string>
        {
            LetterName128 + "/plain;a=b",
            "text/" + LetterName128 + ";a=b",
            "text/plain;" + LetterName128 + "=b",
        };

        public static TheoryData<string, string> EquivalentMediaTypes => new TheoryData<string, string>
        {
            { TextPlain, "TEXT/PLAIN" },
            { TextPlainUsAscii, "Text/Plain;CHARSET=US-ASCII" },
            { TextPlainUsAscii, TextPlainQuotedUsAscii },
            { TextPlainA1B2, "text/plain;b=2;a=1" },
            { "text/plain;a=1;B=2;c=3", "text/plain;c=3;A=1;b=2" },
        };

        public static TheoryData<string, string> DifferentMediaTypes => new TheoryData<string, string>
        {
            { TextPlain, "text/html" },
            { TextPlain, "image/plain" },
            { TextPlain, TextPlainA1 },
            { "text/plain;name=File", "text/plain;name=file" },
            { TextPlainA1, "text/plain;b=1" },
            { TextPlainA1B2, "text/plain;a=1;c=2" },
            { "multipart/mixed;boundary=AbC", "multipart/mixed;boundary=abc" },
        };

        public static TheoryData<string> CanonicalMediaTypes => new TheoryData<string>
        {
            TextPlain,
            TextPlainUsAscii,
            MultipartSimpleBoundary,
            QuotedPairsValue,
            EmptyQuotedValue,
            "text/plain;name=\"a;b=c\";title=\"x@y.example\"",
            "application/ld+json;profile=\"http://www.w3.org/ns/json-ld#compacted\"",
        };

        // Each length of destination too short for TextPlainQuotedName, which is 21 characters.
        public static IEnumerable<object[]> DestinationsTooShortForQuotedName()
        {
            for (var length = 0; length < TextPlainQuotedName.Length; length++)
            {
                yield return new object[] { length };
            }
        }

        public static TheoryData<Action> MembersOfDefaultInstance => new TheoryData<Action>
        {
            () => _ = default(MimeMediaType).Type,
            () => _ = default(MimeMediaType).Subtype,
            () => _ = default(MimeMediaType).Suffix,
            () => _ = default(MimeMediaType).Tree,
            () => _ = default(MimeMediaType).Parameters,
            () => default(MimeMediaType).WithoutParameters(),
            () => default(MimeMediaType).WithParameter(A, B),
        };

        public static TheoryData<Action> ConstructorsWithNullArgument => new TheoryData<Action>
        {
            () => _ = new MimeMediaType(null, Plain),
            () => _ = new MimeMediaType(Text, null),
            () => _ = new MimeMediaType(Text, Plain, null),
        };

        public static TheoryData<MimeMediaType, string> Constants => new TheoryData<MimeMediaType, string>
        {
            { MimeMediaType.TextPlain, TextPlain },
            { MimeMediaType.TextCalendar, "text/calendar" },
            { MimeMediaType.TextVcard, "text/vcard" },
            { MimeMediaType.TextDirectory, "text/directory" },
            { MimeMediaType.MessageRfc822, "message/rfc822" },
            { MimeMediaType.MultipartMixed, "multipart/mixed" },
            { MimeMediaType.MultipartAlternative, "multipart/alternative" },
            { MimeMediaType.MultipartRelated, "multipart/related" },
            { MimeMediaType.ApplicationOctetStream, ApplicationOctetStream },
        };

        public static TheoryData<object> OtherObjects => new TheoryData<object> { TextPlain, null };

        #endregion

        #region Parsing

        [Theory]
        // ref. RFC 2045 §5.1, RFC 2046 §4.1.2, §5.1.1
        [InlineData(TextPlainUsAscii, Text, Plain, Charset, UsAscii)]
        [InlineData(TextPlainQuotedUsAscii, Text, Plain, Charset, UsAscii)]
        [InlineData("multipart/mixed;boundary=gc0p4Jq0M2Yt08j34c0p", Multipart, Mixed, Boundary, "gc0p4Jq0M2Yt08j34c0p")]
        [InlineData(MultipartSimpleBoundary, Multipart, Mixed, Boundary, "simple boundary")]
        [InlineData("message/external-body;access-type=local-file", "message", "external-body", "access-type", "local-file")]
        [InlineData(QuotedPairsValue, Text, Plain, Name, "a \"quoted\" \\ name")]
        [InlineData(QuotedLettersValue, Text, Plain, Name, "ab")]
        [InlineData(EmptyQuotedValue, Text, Plain, Name, "")]
        [InlineData("text/plain;name=\"tab\there\"", Text, Plain, Name, "tab\there")]
        [InlineData("text/plain;name=\"a;b=c\"", Text, Plain, Name, "a;b=c")]
        public void Parse_OneParameter_ReturnsDecodedParts(string value, string type, string subtype, string name, string parameterValue)
        {
            // act
            var testResult = MimeMediaType.Parse(value);

            // assert
            testResult.Type.Should().Be(type);
            testResult.Subtype.Should().Be(subtype);
            testResult.Parameters.Should().ContainSingle();
            testResult.Parameters[0].Name.Should().Be(name);
            testResult.Parameters[0].Value.Should().Be(parameterValue);
            testResult.Parameters[0].Charset.Should().BeEmpty();
            testResult.Parameters[0].Language.Should().BeEmpty();
        }

        [Fact]
        public void Parse_SeveralParameters_KeepsTheirOrder()
        {
            // act
            var testResult = MimeMediaType.Parse("text/calendar;method=REQUEST;charset=UTF-8;component=VEVENT");

            // assert
            testResult.Parameters.Should().HaveCount(3);
            testResult.Parameters[0].Name.Should().Be("method");
            testResult.Parameters[1].Name.Should().Be(Charset);
            testResult.Parameters[2].Name.Should().Be("component");
        }

        [Fact]
        public void Parse_NoParameters_ReturnsEmptyParameters()
        {
            // act
            var testResult = MimeMediaType.Parse(ApplicationOctetStream);

            // assert
            testResult.Type.Should().Be(Application);
            testResult.Subtype.Should().Be("octet-stream");
            testResult.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void Parse_MixedCase_KeepsCase()
        {
            // act
            var testResult = MimeMediaType.Parse("Text/HTML;Charset=UTF-8");

            // assert
            testResult.Type.Should().Be("Text");
            testResult.Subtype.Should().Be("HTML");
            testResult.Parameters[0].Name.Should().Be("Charset");
        }

        [Fact]
        public void Parse_Span_ReturnsMediaType()
        {
            // arrange
            var value = MediaTypeAmongOtherText.AsSpan(SpanStart, SpanLength);
            var expected = MimeMediaType.TextPlain.WithParameter(A, B);

            // act
            var testResult = MimeMediaType.Parse(value);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(InvalidMediaTypes))]
        public void TryParse_Strict_RejectsInvalid(string value)
        {
            // act
            var testResult = MimeMediaType.TryParse(value, out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeMediaType));
        }

        [Theory]
        [MemberData(nameof(InvalidMediaTypes))]
        public void TryParse_StrictSpan_RejectsInvalid(string value)
        {
            // act
            var testResult = MimeMediaType.TryParse(value.AsSpan(), out var result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(default(MimeMediaType));
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            // act
            var testResult = MimeMediaType.TryParse((string)null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Parse_Invalid_ThrowsFormatException()
        {
            // act
            Action testAction = () => MimeMediaType.Parse(Text);

            // assert
            testAction.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => MimeMediaType.Parse((string)null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryParse_UndefinedOptions_ThrowsArgumentOutOfRangeException()
        {
            // act
            Action testAction = () => MimeMediaType.TryParse(TextPlain, UndefinedOption, out _);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData(TextPlainSpaceUsAscii)]
        [InlineData("text/plain ; charset = us-ascii ")]
        [InlineData("text/plain\t;\tcharset\t=\t\"us-ascii\"")]
        public void TryParse_AllowWhitespaceAroundDelimiters_AcceptsWhitespace(string value)
        {
            // arrange
            var expected = MimeMediaType.TextPlain.WithParameter(Charset, UsAscii);

            // act
            var testResult = MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(SpaceBeforeType)]
        [InlineData(SpaceBeforeSlash)]
        [InlineData(SpaceAfterSlash)]
        [InlineData(Comment)]
        [InlineData("text/plain;\r\n charset=us-ascii")]
        public void TryParse_AllowWhitespaceAroundDelimiters_RejectsOtherWhitespaceAndComments(string value)
        {
            // act
            var testResult = MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Rfc2045TokensThatAreNotRfc6838Names))]
        public void TryParse_Rfc2045TokenNotRfc6838NameParsedStrictly_ReturnsFalse(string value)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Rfc2045TokensThatAreNotRfc6838Names))]
        public void TryParse_Rfc2045TokenNotRfc6838NameWithRfc2045TokensAllowed_ReturnsTrue(string value)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowRfc2045Tokens, out _);

            // assert
            parsed.Should().BeTrue();
        }

        [Fact]
        public void TryParse_NameOf127Characters_IsAccepted()
        {
            // act
            var testResult = MimeMediaType.TryParse(SubtypeOf127Characters, out var result);

            // assert
            testResult.Should().BeTrue();
            result.Subtype.Should().HaveLength(MaximumNameLength);
        }

        [Theory]
        [MemberData(nameof(NamesOver127Characters))]
        public void TryParse_NameOver127CharactersParsedStrictly_ReturnsFalse(string value)
        {
            // act
            var testResult = MimeMediaType.TryParse(value, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(NamesOver127Characters))]
        public void TryParse_NameOver127CharactersWithRfc2045TokensAllowed_ReturnsTrue(string value)
        {
            // act
            var testResult = MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowRfc2045Tokens, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Parse_BothOptions_AcceptsBoth()
        {
            // act
            var testResult = MimeMediaType.Parse("application/x~y; a~b=c", WhitespaceAndRfc2045Tokens);

            // assert
            testResult.Subtype.Should().Be(TildeSubtype);
            testResult.Parameters[0].Name.Should().Be("a~b");
        }

        #endregion

        #region Suffix and tree

        [Theory]
        [InlineData("application/ld+json", Json)]
        [InlineData("application/vnd.api+json", Json)]
        [InlineData("image/svg+xml", "xml")]
        [InlineData("application/a+b+cbor", "cbor")]
        [InlineData("application/json", "")]
        [InlineData("application/a+", "")]
        public void Suffix_Subtype_ReturnsTextAfterLastPlus(string value, string suffix)
        {
            // arrange
            var mediaType = MimeMediaType.Parse(value, MimeMediaTypeParseOptions.AllowRfc2045Tokens);

            // act
            var testResult = mediaType.Suffix;

            // assert
            testResult.Should().Be(suffix);
        }

        [Theory]
        [InlineData(TextPlain, MimeRegistrationTree.Standards)]
        [InlineData("application/vnd.ms-excel", MimeRegistrationTree.Vendor)]
        [InlineData("application/VND.ms-excel", MimeRegistrationTree.Vendor)]
        [InlineData("application/prs.example", MimeRegistrationTree.Personal)]
        [InlineData("application/x.example", MimeRegistrationTree.Unregistered)]
        [InlineData("application/x-www-form-urlencoded", MimeRegistrationTree.Unregistered)]
        [InlineData("application/X-tar", MimeRegistrationTree.Unregistered)]
        [InlineData("application/vnd", MimeRegistrationTree.Standards)]
        [InlineData("application/xml", MimeRegistrationTree.Standards)]
        public void Tree_Subtype_ReturnsTreeOfSubtypePrefix(string value, MimeRegistrationTree tree)
        {
            // arrange
            var mediaType = MimeMediaType.Parse(value);

            // act
            var testResult = mediaType.Tree;

            // assert
            testResult.Should().Be(tree);
        }

        #endregion

        #region Equality

        [Theory]
        [MemberData(nameof(EquivalentMediaTypes))]
        public void Equals_EquivalentMediaTypes_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            // act
            var testResult = leftMediaType.Equals(rightMediaType);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EquivalentMediaTypes))]
        public void EqualsObject_EquivalentMediaTypes_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            object rightMediaType = MimeMediaType.Parse(right);

            // act
            var testResult = leftMediaType.Equals(rightMediaType);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EquivalentMediaTypes))]
        public void EqualityOperator_EquivalentMediaTypes_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            // act
            var testResult = leftMediaType == rightMediaType;

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(EquivalentMediaTypes))]
        public void InequalityOperator_EquivalentMediaTypes_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            // act
            var testResult = leftMediaType != rightMediaType;

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(EquivalentMediaTypes))]
        public void GetHashCode_EquivalentMediaTypes_ReturnsSameHashCode(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            var rightHashCode = MimeMediaType.Parse(right).GetHashCode();

            // act
            var testResult = leftMediaType.GetHashCode();

            // assert
            testResult.Should().Be(rightHashCode);
        }

        [Theory]
        [MemberData(nameof(DifferentMediaTypes))]
        public void Equals_DifferentMediaTypes_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            // act
            var testResult = leftMediaType.Equals(rightMediaType);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DifferentMediaTypes))]
        public void EqualityOperator_DifferentMediaTypes_ReturnsFalse(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            // act
            var testResult = leftMediaType == rightMediaType;

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DifferentMediaTypes))]
        public void InequalityOperator_DifferentMediaTypes_ReturnsTrue(string left, string right)
        {
            // arrange
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            // act
            var testResult = leftMediaType != rightMediaType;

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultInstances_ReturnsTrue()
        {
            // act
            var testResult = default(MimeMediaType).Equals(default(MimeMediaType));

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultInstanceAndMediaType_ReturnsFalse()
        {
            // act
            var testResult = default(MimeMediaType).Equals(MimeMediaType.TextPlain);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_DefaultInstance_ReturnsZero()
        {
            // act
            var testResult = default(MimeMediaType).GetHashCode();

            // assert
            testResult.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(OtherObjects))]
        public void EqualsObject_OtherObject_ReturnsFalse(object other)
        {
            // act
            var testResult = MimeMediaType.TextPlain.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        #endregion

        #region Formatting

        [Theory]
        [MemberData(nameof(CanonicalMediaTypes))]
        public void TryFormat_CanonicalForm_WritesItAsParsed(string value)
        {
            // arrange
            var mediaType = MimeMediaType.Parse(value);
            var destination = new char[value.Length];

            // act
            var testResult = mediaType.TryFormat(destination, out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(value.Length);
            new string(destination).Should().Be(value);
        }

        [Theory]
        [MemberData(nameof(CanonicalMediaTypes))]
        public void ToString_CanonicalForm_ReturnsItAsParsed(string value)
        {
            // arrange
            var mediaType = MimeMediaType.Parse(value);

            // act
            var testResult = mediaType.ToString();

            // assert
            testResult.Should().Be(value);
        }

        [Theory]
        [MemberData(nameof(CanonicalMediaTypes))]
        public void Parse_CanonicalFormOfParsed_ReturnsEqualMediaType(string value)
        {
            // arrange
            var mediaType = MimeMediaType.Parse(value);
            var formatted = mediaType.ToString();

            // act
            var testResult = MimeMediaType.Parse(formatted);

            // assert
            testResult.Should().Be(mediaType);
        }

        [Theory]
        [InlineData(TextPlainQuotedUsAscii, TextPlainUsAscii)]
        [InlineData(QuotedLettersValue, "text/plain;name=ab")]
        [InlineData("text/plain ; charset = \"us-ascii\" ", TextPlainUsAscii)]
        public void ToString_ParsedWithQuotesOrWhitespace_QuotesOnlyValuesThatAreNotTokens(string value, string expected)
        {
            // arrange
            var mediaType = MimeMediaType.Parse(value, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

            // act
            var testResult = mediaType.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(DestinationsTooShortForQuotedName))]
        public void TryFormat_DestinationTooShort_ReturnsFalse(int length)
        {
            // arrange
            var mediaType = MimeMediaType.Parse(TextPlainQuotedName);
            var destination = new char[length];

            // act
            var testResult = mediaType.TryFormat(destination, out var charsWritten);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void TryFormat_DefaultInstance_WritesNothing()
        {
            // act
            var testResult = default(MimeMediaType).TryFormat(new char[0], out var charsWritten);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void ToString_DefaultInstance_ReturnsEmpty()
        {
            // act
            var testResult = default(MimeMediaType).ToString();

            // assert
            testResult.Should().BeEmpty();
        }

        #endregion

        #region Construction

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
        public void Constructor_ValidNamesAndParameter_CreatesMediaType()
        {
            // arrange
            var parameters = new[] { new MimeParameter(Charset, UsAscii) };
            var expected = MimeMediaType.Parse(TextPlainUsAscii);

            // act
            var testResult = new MimeMediaType(Text, Plain, parameters);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Constructor_ValidNames_CreatesMediaTypeWithoutParameters()
        {
            // act
            var testResult = new MimeMediaType(Text, Plain);

            // assert
            testResult.Should().Be(MimeMediaType.TextPlain);
        }

        [Theory]
        [InlineData("", Plain)]
        [InlineData(Text, "")]
        [InlineData("te xt", Plain)]
        [InlineData(Text, TildeSubtype)]
        [InlineData("-text", Plain)]
        [InlineData(TextPlain, Plain)]
        public void Constructor_InvalidName_ThrowsArgumentException(string type, string subtype)
        {
            // act
            Action testAction = () => _ = new MimeMediaType(type, subtype);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NameOver127Characters_ThrowsArgumentException()
        {
            // act
            Action testAction = () => _ = new MimeMediaType(Application, LetterName128);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

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
        public void Constructor_DuplicateParameterNames_ThrowsArgumentException()
        {
            // arrange
            var parameters = new[] { new MimeParameter(Charset, UsAscii), new MimeParameter("CHARSET", Utf8) };

            // act
            Action testAction = () => _ = new MimeMediaType(Text, Plain, parameters);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_UninitialisedParameter_ThrowsArgumentException()
        {
            // arrange
            var parameters = new[] { default(MimeParameter) };

            // act
            Action testAction = () => _ = new MimeMediaType(Text, Plain, parameters);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_ParametersChangedAfterwards_KeepsCopy()
        {
            // arrange
            var parameters = new List<MimeParameter> { new MimeParameter(A, "1") };
            var mediaType = new MimeMediaType(Text, Plain, parameters);

            // act
            parameters.Add(new MimeParameter(B, "2"));

            // assert
            mediaType.Parameters.Should().ContainSingle();
        }

        [Fact]
        public void Parameters_Parsed_IsReadOnly()
        {
            // arrange
            var mediaType = MimeMediaType.Parse(TextPlainA1);

            // act
            var testResult = mediaType.Parameters;

            // assert
            testResult.Should().NotBeAssignableTo<MimeParameter[]>();
            ((ICollection<MimeParameter>)testResult).IsReadOnly.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(Constants))]
        public void ToString_Constant_ReturnsMediaType(MimeMediaType mediaType, string expected)
        {
            // act
            var testResult = mediaType.ToString();

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(MimeTopLevelTypes.Text)]
        [InlineData(MimeTopLevelTypes.Image)]
        [InlineData(MimeTopLevelTypes.Audio)]
        [InlineData(MimeTopLevelTypes.Video)]
        [InlineData(MimeTopLevelTypes.Application)]
        [InlineData(MimeTopLevelTypes.Multipart)]
        [InlineData(MimeTopLevelTypes.Message)]
        [InlineData(MimeTopLevelTypes.Font)]
        [InlineData(MimeTopLevelTypes.Model)]
        [InlineData(MimeTopLevelTypes.Haptics)]
        public void Constructor_TopLevelType_IsValidTypeName(string type)
        {
            // act
            var testResult = new MimeMediaType(type, "example");

            // assert
            testResult.Type.Should().Be(type);
        }

        #endregion

        #region Parameters

        [Fact]
        public void TryGetParameter_NameInOtherCase_ReturnsParameter()
        {
            // arrange
            var mediaType = MimeMediaType.Parse("text/plain;Charset=us-ascii");

            // act
            var testResult = mediaType.TryGetParameter("CHARSET", out var parameter);

            // assert
            testResult.Should().BeTrue();
            parameter.Value.Should().Be(UsAscii);
        }

        [Fact]
        public void TryGetParameter_MissingName_ReturnsFalse()
        {
            // arrange
            var mediaType = MimeMediaType.Parse(TextPlainUsAscii);

            // act
            var testResult = mediaType.TryGetParameter("format", out var parameter);

            // assert
            testResult.Should().BeFalse();
            parameter.Should().Be(default(MimeParameter));
        }

        [Fact]
        public void WithParameter_NewName_AppendsParameter()
        {
            // arrange
            var original = MimeMediaType.Parse(TextPlainA1);

            // act
            var testResult = original.WithParameter(B, "two words");

            // assert
            testResult.Parameters.Should().Equal(new MimeParameter(A, "1"), new MimeParameter(B, "two words"));
        }

        [Fact]
        public void WithParameter_NewName_LeavesOriginalUnchanged()
        {
            // arrange
            var original = MimeMediaType.Parse(TextPlainA1);

            // act
            original.WithParameter(B, "two words");

            // assert
            original.Parameters.Should().Equal(new MimeParameter(A, "1"));
        }

        [Fact]
        public void WithParameter_ExistingName_ReplacesInPlace()
        {
            // arrange
            var original = MimeMediaType.Parse(TextPlainCharsetAndFormat);

            // act
            var testResult = original.WithParameter(new MimeParameter("CHARSET", Utf8));

            // assert
            testResult.Parameters.Should().SatisfyRespectively(
                first =>
                {
                    first.Name.Should().Be("CHARSET");
                    first.Value.Should().Be(Utf8);
                },
                second => second.Name.Should().Be("format"));
        }

        [Fact]
        public void WithParameter_ExistingName_LeavesOriginalUnchanged()
        {
            // arrange
            var original = MimeMediaType.Parse(TextPlainCharsetAndFormat);

            // act
            original.WithParameter(new MimeParameter("CHARSET", Utf8));

            // assert
            original.Parameters[0].Value.Should().Be(UsAscii);
        }

        [Fact]
        public void WithParameter_UninitialisedParameter_ThrowsArgumentException()
        {
            // act
            Action testAction = () => MimeMediaType.TextPlain.WithParameter(default(MimeParameter));

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithParameter_InvalidName_ThrowsArgumentException()
        {
            // act
            Action testAction = () => MimeMediaType.TextPlain.WithParameter("a*", B);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithoutParameter_NameInOtherCase_RemovesParameter()
        {
            // arrange
            var original = MimeMediaType.Parse("text/plain;a=1;b=2;c=3");

            // act
            var testResult = original.WithoutParameter("B");

            // assert
            testResult.Parameters.Should().Equal(new MimeParameter(A, "1"), new MimeParameter("c", "3"));
        }

        [Fact]
        public void WithoutParameter_MissingName_ReturnsEqualMediaType()
        {
            // arrange
            var original = MimeMediaType.Parse(TextPlainA1B2);

            // act
            var testResult = original.WithoutParameter("d");

            // assert
            testResult.Should().Be(original);
        }

        [Fact]
        public void WithoutParameter_OnlyParameter_LeavesNoParameters()
        {
            // arrange
            var original = MimeMediaType.Parse(TextPlainA1);

            // act
            var testResult = original.WithoutParameter(A);

            // assert
            testResult.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void Equals_MediaTypeWithParametersAndWithout_ReturnsFalse()
        {
            // arrange
            var mediaType = MimeMediaType.Parse("text/calendar;method=REQUEST;charset=UTF-8");

            // act
            var testResult = mediaType.Equals(MimeMediaType.TextCalendar);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void WithoutParameters_MediaTypeWithParameters_EqualsTypeAndSubtype()
        {
            // arrange
            var mediaType = MimeMediaType.Parse("text/calendar;method=REQUEST;charset=UTF-8");

            // act
            var testResult = mediaType.WithoutParameters();

            // assert
            testResult.Should().Be(MimeMediaType.TextCalendar);
        }

        #endregion

#if NET7_0_OR_GREATER
        [Fact]
        public void ISpanParsableParse_Strict_ReturnsMediaType()
        {
            // arrange
            var expected = MimeMediaType.TextPlain.WithParameter(A, B);

            // act
            var testResult = ParseGeneric<MimeMediaType>("text/plain;a=b");

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void IParsableTryParse_WhitespaceAroundDelimiter_ReturnsFalse()
        {
            // act
            var testResult = TryParseGeneric<MimeMediaType>("text/plain; a=b");

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ISpanFormattableTryFormat_QuotedValue_WritesMediaType()
        {
            // arrange
            ISpanFormattable mediaType = MimeMediaType.Parse(TextPlainQuotedValue);
            var destination = new char[TextPlainQuotedValue.Length];

            // act
            var testResult = mediaType.TryFormat(destination, out var charsWritten, default, null);

            // assert
            testResult.Should().BeTrue();
            charsWritten.Should().Be(TextPlainQuotedValue.Length);
            new string(destination).Should().Be(TextPlainQuotedValue);
        }

        [Fact]
        public void IFormattableToString_QuotedValue_ReturnsMediaType()
        {
            // arrange
            IFormattable mediaType = MimeMediaType.Parse(TextPlainQuotedValue);

            // act
            var testResult = mediaType.ToString(null, null);

            // assert
            testResult.Should().Be(TextPlainQuotedValue);
        }

        private static T ParseGeneric<T>(string value) where T : ISpanParsable<T>
        {
            return T.Parse(value.AsSpan(), null);
        }

        private static bool TryParseGeneric<T>(string value) where T : IParsable<T>
        {
            return T.TryParse(value, null, out _);
        }
#endif
    }
}
