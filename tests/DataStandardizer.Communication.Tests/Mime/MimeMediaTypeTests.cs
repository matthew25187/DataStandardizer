using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeMediaTypeTests
    {
        [Theory]
        // ref. RFC 2045 §5.1, RFC 2046 §4.1.2, §5.1.1
        [InlineData("text/plain;charset=us-ascii", "text", "plain", "charset", "us-ascii")]
        [InlineData("text/plain;charset=\"us-ascii\"", "text", "plain", "charset", "us-ascii")]
        [InlineData("multipart/mixed;boundary=gc0p4Jq0M2Yt08j34c0p", "multipart", "mixed", "boundary", "gc0p4Jq0M2Yt08j34c0p")]
        [InlineData("multipart/mixed;boundary=\"simple boundary\"", "multipart", "mixed", "boundary", "simple boundary")]
        [InlineData("message/external-body;access-type=local-file", "message", "external-body", "access-type", "local-file")]
        [InlineData("text/plain;name=\"a \\\"quoted\\\" \\\\ name\"", "text", "plain", "name", "a \"quoted\" \\ name")]
        [InlineData("text/plain;name=\"\\a\\b\"", "text", "plain", "name", "ab")]
        [InlineData("text/plain;name=\"\"", "text", "plain", "name", "")]
        [InlineData("text/plain;name=\"tab\there\"", "text", "plain", "name", "tab\there")]
        [InlineData("text/plain;name=\"a;b=c\"", "text", "plain", "name", "a;b=c")]
        public void Parse_OneParameter_ReturnsDecodedParts(string value, string type, string subtype, string name, string parameterValue)
        {
            var result = MimeMediaType.Parse(value);

            result.Type.Should().Be(type);
            result.Subtype.Should().Be(subtype);
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Name.Should().Be(name);
            result.Parameters[0].Value.Should().Be(parameterValue);
            result.Parameters[0].Charset.Should().BeEmpty();
            result.Parameters[0].Language.Should().BeEmpty();
        }

        [Fact]
        public void Parse_SeveralParameters_KeepsTheirOrder()
        {
            var result = MimeMediaType.Parse("text/calendar;method=REQUEST;charset=UTF-8;component=VEVENT");

            result.Parameters.Should().HaveCount(3);
            result.Parameters[0].Name.Should().Be("method");
            result.Parameters[1].Name.Should().Be("charset");
            result.Parameters[2].Name.Should().Be("component");
        }

        [Fact]
        public void Parse_NoParameters_ReturnsEmptyParameters()
        {
            var result = MimeMediaType.Parse("application/octet-stream");

            result.Type.Should().Be("application");
            result.Subtype.Should().Be("octet-stream");
            result.Parameters.Should().BeEmpty();
        }

        [Fact]
        public void Parse_MixedCase_KeepsCase()
        {
            var result = MimeMediaType.Parse("Text/HTML;Charset=UTF-8");

            result.Type.Should().Be("Text");
            result.Subtype.Should().Be("HTML");
            result.Parameters[0].Name.Should().Be("Charset");
        }

        [Fact]
        public void Parse_Span_ReturnsMediaType()
        {
            var result = MimeMediaType.Parse("xx text/plain;a=b xx".AsSpan(3, 14));

            result.Should().Be(MimeMediaType.TextPlain.WithParameter("a", "b"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("text")]
        [InlineData("text/")]
        [InlineData("/plain")]
        [InlineData("text/plain;")]
        [InlineData("text/plain;charset")]
        [InlineData("text/plain;charset=")]
        [InlineData("text/plain;=us-ascii")]
        [InlineData("text/plain;charset=\"us-ascii")]
        [InlineData("text/plain;charset=\"us-ascii\"x")]
        [InlineData("text/plain;name=\"a\\")]
        [InlineData("text/plain;name=\"café\"")]
        [InlineData("text/plain;name=café")]
        [InlineData("text/plain;name=\"a\rb\"")]
        [InlineData("text/plain;name=a b")]
        [InlineData("text/plain;name=a/b")]
        [InlineData("text/plain/x")]
        [InlineData("text/plain;charset=us-ascii;Charset=utf-8")]
        [InlineData("text/plain (comment)")]
        [InlineData("text/plain;charset=us-ascii (Plain text)")]
        [InlineData(" text/plain")]
        [InlineData("text /plain")]
        [InlineData("text/ plain")]
        [InlineData("text/plain ;charset=us-ascii")]
        [InlineData("text/plain; charset=us-ascii")]
        [InlineData("text/plain;charset =us-ascii")]
        [InlineData("text/plain;charset= us-ascii")]
        [InlineData("text/plain ")]
        public void TryParse_Strict_RejectsInvalid(string value)
        {
            MimeMediaType result;
            MimeMediaType.TryParse(value, out result).Should().BeFalse();
            MimeMediaType.TryParse(value.AsSpan(), out result).Should().BeFalse();
            result.Should().Be(default(MimeMediaType));
        }

        [Fact]
        public void TryParse_Null_ReturnsFalse()
        {
            MimeMediaType result;
            MimeMediaType.TryParse((string)null, out result).Should().BeFalse();
        }

        [Fact]
        public void Parse_Invalid_ThrowsFormatException()
        {
            Action act = () => MimeMediaType.Parse("text");

            act.Should().Throw<FormatException>();
        }

        [Fact]
        public void Parse_Null_ThrowsArgumentNullException()
        {
            Action act = () => MimeMediaType.Parse((string)null);

            act.Should().Throw<ArgumentNullException>();
        }

        // The lowest bit that no MimeMediaTypeParseOptions member uses.
        // The lowest bit that no option uses.
        private const MimeMediaTypeParseOptions UndefinedOption = (MimeMediaTypeParseOptions)(1 << 8);

        [Fact]
        public void TryParse_UndefinedOptions_ThrowsArgumentOutOfRangeException()
        {
            // act
            Action testAction = () => MimeMediaType.TryParse("text/plain", UndefinedOption, out _);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData("text/plain; charset=us-ascii")]
        [InlineData("text/plain ; charset = us-ascii ")]
        [InlineData("text/plain\t;\tcharset\t=\t\"us-ascii\"")]
        public void TryParse_AllowWhitespaceAroundDelimiters_AcceptsWhitespace(string value)
        {
            MimeMediaType result;
            MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters, out result).Should().BeTrue();

            result.Should().Be(MimeMediaType.TextPlain.WithParameter("charset", "us-ascii"));
        }

        [Theory]
        [InlineData(" text/plain")]
        [InlineData("text /plain")]
        [InlineData("text/ plain")]
        [InlineData("text/plain (comment)")]
        [InlineData("text/plain;\r\n charset=us-ascii")]
        public void TryParse_AllowWhitespaceAroundDelimiters_RejectsOtherWhitespaceAndComments(string value)
        {
            MimeMediaType result;
            MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters, out result).Should().BeFalse();
        }

        public static TheoryData<string> Rfc2045TokensThatAreNotRfc6838Names => new TheoryData<string>
        {
            "application/*",
            "application/x~y",
            "application/-dash",
            "application/.dot",
            "x{y}/plain",
            "text/plain;na|me=x",
        };

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
            var value = "application/" + new string('a', 127);

            MimeMediaType result;
            MimeMediaType.TryParse(value, out result).Should().BeTrue();
            result.Subtype.Should().HaveLength(127);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void TryParse_NameOver127Characters_IsOptIn(bool longType, bool longSubtype, bool longParameterName)
        {
            var longName = new string('a', 128);
            var value = (longType ? longName : "text") + "/" + (longSubtype ? longName : "plain") + ";" + (longParameterName ? longName : "a") + "=b";

            MimeMediaType result;
            MimeMediaType.TryParse(value, out result).Should().BeFalse();
            MimeMediaType.TryParse(value, MimeMediaTypeParseOptions.AllowRfc2045Tokens, out result).Should().BeTrue();
        }

        private const MimeMediaTypeParseOptions WhitespaceAndRfc2045Tokens =
            MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters | MimeMediaTypeParseOptions.AllowRfc2045Tokens;

        [Fact]
        public void Parse_BothOptions_AcceptsBoth()
        {
            // act
            var result = MimeMediaType.Parse("application/x~y; a~b=c", WhitespaceAndRfc2045Tokens);

            // assert
            result.Subtype.Should().Be("x~y");
            result.Parameters[0].Name.Should().Be("a~b");
        }

        [Theory]
        [InlineData("application/ld+json", "json")]
        [InlineData("application/vnd.api+json", "json")]
        [InlineData("image/svg+xml", "xml")]
        [InlineData("application/a+b+cbor", "cbor")]
        [InlineData("application/json", "")]
        [InlineData("application/a+", "")]
        public void Suffix_ReturnsTextAfterLastPlus(string value, string suffix)
        {
            MimeMediaType.Parse(value, MimeMediaTypeParseOptions.AllowRfc2045Tokens).Suffix.Should().Be(suffix);
        }

        [Theory]
        [InlineData("text/plain", MimeRegistrationTree.Standards)]
        [InlineData("application/vnd.ms-excel", MimeRegistrationTree.Vendor)]
        [InlineData("application/VND.ms-excel", MimeRegistrationTree.Vendor)]
        [InlineData("application/prs.example", MimeRegistrationTree.Personal)]
        [InlineData("application/x.example", MimeRegistrationTree.Unregistered)]
        [InlineData("application/x-www-form-urlencoded", MimeRegistrationTree.Unregistered)]
        [InlineData("application/X-tar", MimeRegistrationTree.Unregistered)]
        [InlineData("application/vnd", MimeRegistrationTree.Standards)]
        [InlineData("application/xml", MimeRegistrationTree.Standards)]
        public void Tree_ReturnsTreeOfSubtypePrefix(string value, MimeRegistrationTree tree)
        {
            MimeMediaType.Parse(value).Tree.Should().Be(tree);
        }

        [Theory]
        [InlineData("text/plain", "TEXT/PLAIN")]
        [InlineData("text/plain;charset=us-ascii", "Text/Plain;CHARSET=US-ASCII")]
        [InlineData("text/plain;charset=us-ascii", "text/plain;charset=\"us-ascii\"")]
        [InlineData("text/plain;a=1;b=2", "text/plain;b=2;a=1")]
        [InlineData("text/plain;a=1;B=2;c=3", "text/plain;c=3;A=1;b=2")]
        public void Equals_EquivalentMediaTypes_ReturnsTrue(string left, string right)
        {
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            leftMediaType.Equals(rightMediaType).Should().BeTrue();
            (leftMediaType == rightMediaType).Should().BeTrue();
            (leftMediaType != rightMediaType).Should().BeFalse();
            leftMediaType.Equals((object)rightMediaType).Should().BeTrue();
            leftMediaType.GetHashCode().Should().Be(rightMediaType.GetHashCode());
        }

        [Theory]
        [InlineData("text/plain", "text/html")]
        [InlineData("text/plain", "image/plain")]
        [InlineData("text/plain", "text/plain;a=1")]
        [InlineData("text/plain;name=File", "text/plain;name=file")]
        [InlineData("text/plain;a=1", "text/plain;b=1")]
        [InlineData("text/plain;a=1;b=2", "text/plain;a=1;c=2")]
        [InlineData("multipart/mixed;boundary=AbC", "multipart/mixed;boundary=abc")]
        public void Equals_DifferentMediaTypes_ReturnsFalse(string left, string right)
        {
            var leftMediaType = MimeMediaType.Parse(left);
            var rightMediaType = MimeMediaType.Parse(right);

            leftMediaType.Equals(rightMediaType).Should().BeFalse();
            (leftMediaType == rightMediaType).Should().BeFalse();
            (leftMediaType != rightMediaType).Should().BeTrue();
        }

        [Fact]
        public void Equals_DefaultInstances_ReturnsTrue()
        {
            default(MimeMediaType).Equals(default(MimeMediaType)).Should().BeTrue();
            default(MimeMediaType).Equals(MimeMediaType.TextPlain).Should().BeFalse();
            default(MimeMediaType).GetHashCode().Should().Be(0);
        }

        [Fact]
        public void Equals_OtherObject_ReturnsFalse()
        {
            MimeMediaType.TextPlain.Equals("text/plain").Should().BeFalse();
            MimeMediaType.TextPlain.Equals(null).Should().BeFalse();
        }

        [Theory]
        [InlineData("text/plain")]
        [InlineData("text/plain;charset=us-ascii")]
        [InlineData("multipart/mixed;boundary=\"simple boundary\"")]
        [InlineData("text/plain;name=\"a \\\"quoted\\\" \\\\ name\"")]
        [InlineData("text/plain;name=\"\"")]
        [InlineData("text/plain;name=\"a;b=c\";title=\"x@y.example\"")]
        [InlineData("application/ld+json;profile=\"http://www.w3.org/ns/json-ld#compacted\"")]
        public void TryFormat_CanonicalForm_RoundTrips(string value)
        {
            var mediaType = MimeMediaType.Parse(value);
            var buffer = new char[value.Length];

            int charsWritten;
            mediaType.TryFormat(buffer, out charsWritten).Should().BeTrue();

            new string(buffer, 0, charsWritten).Should().Be(value);
            mediaType.ToString().Should().Be(value);
            MimeMediaType.Parse(mediaType.ToString()).Should().Be(mediaType);
        }

        [Theory]
        [InlineData("text/plain;charset=\"us-ascii\"", "text/plain;charset=us-ascii")]
        [InlineData("text/plain;name=\"\\a\\b\"", "text/plain;name=ab")]
        [InlineData("text/plain ; charset = \"us-ascii\" ", "text/plain;charset=us-ascii")]
        public void ToString_QuotesOnlyValuesThatAreNotTokens(string value, string expected)
        {
            MimeMediaType.Parse(value, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters).ToString().Should().Be(expected);
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            var mediaType = MimeMediaType.Parse("text/plain;name=\"a b\"");
            var formatted = mediaType.ToString();

            for (var length = 0; length < formatted.Length; length++)
            {
                int charsWritten;
                mediaType.TryFormat(new char[length], out charsWritten).Should().BeFalse();
                charsWritten.Should().Be(0);
            }
        }

        [Fact]
        public void TryFormat_DefaultInstance_WritesNothing()
        {
            int charsWritten;
            default(MimeMediaType).TryFormat(new char[0], out charsWritten).Should().BeTrue();

            charsWritten.Should().Be(0);
            default(MimeMediaType).ToString().Should().BeEmpty();
        }

        [Fact]
        public void Properties_DefaultInstance_ThrowInvalidOperationException()
        {
            var mediaType = default(MimeMediaType);

            ((Action)(() => _ = mediaType.Type)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = mediaType.Subtype)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = mediaType.Suffix)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = mediaType.Tree)).Should().Throw<InvalidOperationException>();
            ((Action)(() => _ = mediaType.Parameters)).Should().Throw<InvalidOperationException>();
            ((Action)(() => mediaType.WithoutParameters())).Should().Throw<InvalidOperationException>();
            ((Action)(() => mediaType.WithParameter("a", "b"))).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Constructor_ValidNames_CreatesMediaType()
        {
            var mediaType = new MimeMediaType("text", "plain", new[] { new MimeParameter("charset", "us-ascii") });

            mediaType.ToString().Should().Be("text/plain;charset=us-ascii");
            new MimeMediaType("text", "plain").Should().Be(MimeMediaType.TextPlain);
        }

        [Theory]
        [InlineData("", "plain")]
        [InlineData("text", "")]
        [InlineData("te xt", "plain")]
        [InlineData("text", "x~y")]
        [InlineData("-text", "plain")]
        [InlineData("text/plain", "plain")]
        public void Constructor_InvalidName_ThrowsArgumentException(string type, string subtype)
        {
            Action act = () => new MimeMediaType(type, subtype);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NameOver127Characters_ThrowsArgumentException()
        {
            Action act = () => new MimeMediaType("application", new string('a', 128));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullArgument_ThrowsArgumentNullException()
        {
            ((Action)(() => new MimeMediaType(null, "plain"))).Should().Throw<ArgumentNullException>();
            ((Action)(() => new MimeMediaType("text", null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => new MimeMediaType("text", "plain", null))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_DuplicateParameterNames_ThrowsArgumentException()
        {
            var parameters = new[] { new MimeParameter("charset", "us-ascii"), new MimeParameter("CHARSET", "utf-8") };
            Action act = () => new MimeMediaType("text", "plain", parameters);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_UninitialisedParameter_ThrowsArgumentException()
        {
            Action act = () => new MimeMediaType("text", "plain", new[] { default(MimeParameter) });

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_ParametersChangedAfterwards_KeepsCopy()
        {
            var parameters = new List<MimeParameter> { new MimeParameter("a", "1") };
            var mediaType = new MimeMediaType("text", "plain", parameters);

            parameters.Add(new MimeParameter("b", "2"));

            mediaType.Parameters.Should().ContainSingle();
        }

        [Fact]
        public void Parameters_CannotBeCastToMutableList()
        {
            var parameters = MimeMediaType.Parse("text/plain;a=1").Parameters;

            parameters.Should().NotBeAssignableTo<MimeParameter[]>();
            ((ICollection<MimeParameter>)parameters).IsReadOnly.Should().BeTrue();
        }

        [Fact]
        public void TryGetParameter_IgnoresCaseOfName()
        {
            var mediaType = MimeMediaType.Parse("text/plain;Charset=us-ascii");

            MimeParameter parameter;
            mediaType.TryGetParameter("CHARSET", out parameter).Should().BeTrue();
            parameter.Value.Should().Be("us-ascii");
            mediaType.TryGetParameter("format", out parameter).Should().BeFalse();
            parameter.Should().Be(default(MimeParameter));
        }

        [Fact]
        public void WithParameter_NewName_AppendsParameter()
        {
            var original = MimeMediaType.Parse("text/plain;a=1");

            var result = original.WithParameter("b", "two words");

            result.ToString().Should().Be("text/plain;a=1;b=\"two words\"");
            original.ToString().Should().Be("text/plain;a=1");
        }

        [Fact]
        public void WithParameter_ExistingName_ReplacesInPlace()
        {
            var original = MimeMediaType.Parse("text/plain;charset=us-ascii;format=flowed");

            var result = original.WithParameter(new MimeParameter("CHARSET", "utf-8"));

            result.ToString().Should().Be("text/plain;CHARSET=utf-8;format=flowed");
            original.ToString().Should().Be("text/plain;charset=us-ascii;format=flowed");
        }

        [Fact]
        public void WithParameter_UninitialisedParameter_ThrowsArgumentException()
        {
            Action act = () => MimeMediaType.TextPlain.WithParameter(default(MimeParameter));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithParameter_InvalidName_ThrowsArgumentException()
        {
            Action act = () => MimeMediaType.TextPlain.WithParameter("a*", "b");

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void WithoutParameter_RemovesParameterIgnoringCase()
        {
            var original = MimeMediaType.Parse("text/plain;a=1;b=2;c=3");

            original.WithoutParameter("B").ToString().Should().Be("text/plain;a=1;c=3");
            original.WithoutParameter("d").Should().Be(original);
            MimeMediaType.Parse("text/plain;a=1").WithoutParameter("a").Parameters.Should().BeEmpty();
        }

        [Fact]
        public void WithoutParameters_ComparesTypeAndSubtypeOnly()
        {
            var mediaType = MimeMediaType.Parse("text/calendar;method=REQUEST;charset=UTF-8");

            mediaType.Should().NotBe(MimeMediaType.TextCalendar);
            mediaType.WithoutParameters().Should().Be(MimeMediaType.TextCalendar);
        }

        [Fact]
        public void Constants_HaveExpectedValues()
        {
            MimeMediaType.TextPlain.ToString().Should().Be("text/plain");
            MimeMediaType.TextCalendar.ToString().Should().Be("text/calendar");
            MimeMediaType.TextVcard.ToString().Should().Be("text/vcard");
            MimeMediaType.TextDirectory.ToString().Should().Be("text/directory");
            MimeMediaType.MessageRfc822.ToString().Should().Be("message/rfc822");
            MimeMediaType.MultipartMixed.ToString().Should().Be("multipart/mixed");
            MimeMediaType.MultipartAlternative.ToString().Should().Be("multipart/alternative");
            MimeMediaType.MultipartRelated.ToString().Should().Be("multipart/related");
            MimeMediaType.ApplicationOctetStream.ToString().Should().Be("application/octet-stream");
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
        public void TopLevelTypes_AreValidTypeNames(string type)
        {
            new MimeMediaType(type, "example").Type.Should().Be(type);
        }

#if NET7_0_OR_GREATER
        [Fact]
        public void ISpanParsable_ParsesStrictly()
        {
            ParseGeneric<MimeMediaType>("text/plain;a=b").Should().Be(MimeMediaType.TextPlain.WithParameter("a", "b"));
            TryParseGeneric<MimeMediaType>("text/plain; a=b").Should().BeFalse();
        }

        [Fact]
        public void ISpanFormattable_FormatsMediaType()
        {
            var mediaType = MimeMediaType.Parse("text/plain;a=\"b c\"");

            $"{mediaType}".Should().Be("text/plain;a=\"b c\"");
            ((IFormattable)mediaType).ToString(null, null).Should().Be("text/plain;a=\"b c\"");
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
