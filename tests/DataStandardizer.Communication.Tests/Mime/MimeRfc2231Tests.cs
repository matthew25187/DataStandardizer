using System;
using System.Text;
using DataStandardizer.Communication.Mime;
using DataStandardizer.LanguageTag;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeRfc2231Tests
    {
        private const MimeMediaTypeParseOptions AllRfc2231Options = MimeMediaTypeParseOptions.AllowMissingSections |
            MimeMediaTypeParseOptions.AllowDuplicateSections | MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers |
            MimeMediaTypeParseOptions.AllowUnknownCharsets | MimeMediaTypeParseOptions.AllowQuotedExtendedValues;

        private const string ThreeEuros = "%E2%82%AC%E2%82%AC%E2%82%AC";

        [Fact]
        public void Parse_Rfc2231Section3Example_JoinsSections()
        {
            var result = MimeMediaType.Parse(
                "message/external-body; access-type=URL; URL*0=\"ftp://\"; URL*1=\"cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar\"",
                MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

            result.Parameters.Should().HaveCount(2);
            result.Parameters[1].Name.Should().Be("URL");
            result.Parameters[1].Value.Should().Be("ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar");
            result.Parameters[1].Charset.Should().BeEmpty();
            result.Parameters[1].Language.Should().BeEmpty();
        }

        [Fact]
        public void Parse_Rfc2231Section4Example_DecodesCharsetAndLanguage()
        {
            var result = MimeMediaType.Parse("application/x-stuff; title*=us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A",
                MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Name.Should().Be("title");
            result.Parameters[0].Value.Should().Be("This is ***fun***");
            result.Parameters[0].Charset.Should().Be("us-ascii");
            result.Parameters[0].Language.Should().Be("en-us");
        }

        [Fact]
        public void Parse_Rfc2231Section41Example_JoinsExtendedAndPlainSections()
        {
            var result = MimeMediaType.Parse(
                "application/x-stuff; title*0*=us-ascii'en'This%20is%20even%20more%20; title*1*=%2A%2A%2Afun%2A%2A%2A%20; title*2=\"isn't it!\"",
                MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Name.Should().Be("title");
            result.Parameters[0].Value.Should().Be("This is even more ***fun*** isn't it!");
            result.Parameters[0].Charset.Should().Be("us-ascii");
            result.Parameters[0].Language.Should().Be("en");
        }

        [Theory]
        [InlineData("text/plain;name*=UTF-8''na%C3%AFve.txt", "na\xEFve.txt", "UTF-8")]
        [InlineData("text/plain;name*=utf-8''na%c3%afve.txt", "na\xEFve.txt", "utf-8")]
        [InlineData("text/plain;name*=iso-8859-1''%A3%20rates", "\xA3 rates", "iso-8859-1")]
        [InlineData("text/plain;name*=UTF-8''%F0%9F%98%80", "\xD83D\xDE00", "UTF-8")]
        [InlineData("text/plain;name*=''abc", "abc", "")]
        [InlineData("text/plain;name*=UTF-8''", "", "UTF-8")]
        [InlineData("text/plain;name*0*=UTF-8''%E2%82;name*1*=%AC", "\x20AC", "UTF-8")]
        [InlineData("text/plain;name*0*=UTF-8''a;name*1=%41", "a%41", "UTF-8")]
        [InlineData("text/plain;name*0=a;name*1*=%41", "aA", "")]
        public void Parse_ExtendedValue_DecodesCharset(string value, string expected, string charset)
        {
            var result = MimeMediaType.Parse(value);

            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be(expected);
            result.Parameters[0].Charset.Should().Be(charset);
            result.Parameters[0].Language.Should().BeEmpty();
        }

        [Fact]
        public void Parse_SectionsOutOfOrder_JoinsInNumericOrder()
        {
            var result = MimeMediaType.Parse("text/plain;x=1;a*11=l;a*10=k;a*9=j;a*8=i;a*7=h;a*6=g;a*5=f;a*4=e;a*3=d;a*2=c;a*1=b;a*0=a;y=2");

            result.Parameters.Should().HaveCount(3);
            result.Parameters[0].Name.Should().Be("x");
            result.Parameters[1].Name.Should().Be("a");
            result.Parameters[1].Value.Should().Be("abcdefghijkl");
            result.Parameters[2].Name.Should().Be("y");
        }

        [Fact]
        public void Parse_SectionNamesInMixedCase_AreOneParameter()
        {
            var result = MimeMediaType.Parse("text/plain;Title*0=a;TITLE*1=b");

            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Name.Should().Be("Title");
            result.Parameters[0].Value.Should().Be("ab");
        }

        [Theory]
        [InlineData("text/plain;name=fallback;name*=UTF-8''real")]
        [InlineData("text/plain;name*=UTF-8''real;name=fallback")]
        [InlineData("text/plain;NAME=fallback;name*0=re;name*1=al")]
        public void Parse_PlainAndRfc2231Forms_UsesRfc2231Form(string value)
        {
            var result = MimeMediaType.Parse(value);

            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be("real");
        }

        [Fact]
        public void Parse_PlainAndRfc2231Forms_KeepsPositionOfFirstForm()
        {
            var result = MimeMediaType.Parse("text/plain;name=fallback;x=1;name*=UTF-8''real");

            result.Parameters.Should().HaveCount(2);
            result.Parameters[0].Name.Should().Be("name");
            result.Parameters[1].Name.Should().Be("x");
        }

        [Theory]
        // Not one of the RFC 2231 forms.
        [InlineData("text/plain;name*=abc")]
        [InlineData("text/plain;name*=UTF-8'abc")]
        [InlineData("text/plain;name*=UTF-8''%2")]
        [InlineData("text/plain;name*=UTF-8''%ZZ")]
        [InlineData("text/plain;name*=UTF-8''a'b")]
        [InlineData("text/plain;name*=UTF-8''a*b")]
        [InlineData("text/plain;name*=UTF-8'e_n'abc")]
        [InlineData("text/plain;name*0*=abc")]
        [InlineData("text/plain;*=x")]
        [InlineData("text/plain;*0=x")]
        [InlineData("text/plain;name*1234567890=x")]
        // A parameter that is given twice, or both as name* and in sections.
        [InlineData("text/plain;name*=''a;name*=''b")]
        [InlineData("text/plain;name*=''a;name*0=b")]
        [InlineData("text/plain;name*0=b;name*=''a")]
        [InlineData("text/plain;name=a;name=b;name*=''c")]
        public void TryParse_InvalidRfc2231Form_ReturnsFalse(string value)
        {
            MimeMediaType result;
            MimeMediaType.TryParse(value, AllRfc2231Options, out result).Should().BeFalse();
        }

        [Theory]
        [InlineData("text/plain;a*0=x;a*2=z", MimeMediaTypeParseOptions.AllowMissingSections, "xz")]
        [InlineData("text/plain;a*1=y", MimeMediaTypeParseOptions.AllowMissingSections, "y")]
        [InlineData("text/plain;a*1*=%41", MimeMediaTypeParseOptions.AllowMissingSections, "A")]
        [InlineData("text/plain;a*0=x;a*0=y", MimeMediaTypeParseOptions.AllowDuplicateSections, "x")]
        [InlineData("text/plain;a*1=z;a*0=x;a*1=y", MimeMediaTypeParseOptions.AllowDuplicateSections, "xz")]
        [InlineData("text/plain;a*00=x;a*01=y", MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers, "xy")]
        [InlineData("text/plain;a*01=y;a*0=x", MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers, "xy")]
        [InlineData("text/plain;a*0000000000001=y;a*0=x", MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers, "xy")]
        [InlineData("text/plain;a*=x-no-such-charset''%E9t%E9", MimeMediaTypeParseOptions.AllowUnknownCharsets, "\xE9t\xE9")]
        [InlineData("text/plain;a*=x.y''abc", MimeMediaTypeParseOptions.AllowUnknownCharsets, "abc")]
        [InlineData("text/plain;a*=''%E9", MimeMediaTypeParseOptions.AllowUnknownCharsets, "\xE9")]
        [InlineData("text/plain;a*=\"UTF-8''a%20b\"", MimeMediaTypeParseOptions.AllowQuotedExtendedValues, "a b")]
        [InlineData("text/plain;a*=\"UTF-8'en'a b;c\"", MimeMediaTypeParseOptions.AllowQuotedExtendedValues, "a b;c")]
        [InlineData("text/plain;a*0*=\"UTF-8''a%20\";a*1*=b", MimeMediaTypeParseOptions.AllowQuotedExtendedValues, "a b")]
        public void TryParse_Rfc2231Error_IsRejectedUnlessOptedIn(string value, MimeMediaTypeParseOptions option, string expected)
        {
            MimeMediaType strict, otherOptions, optedIn;
            MimeMediaType.TryParse(value, out strict).Should().BeFalse();
            MimeMediaType.TryParse(value, AllRfc2231Options & ~option, out otherOptions).Should().BeFalse();
            MimeMediaType.TryParse(value, option, out optedIn).Should().BeTrue();

            optedIn.Parameters.Should().ContainSingle();
            optedIn.Parameters[0].Value.Should().Be(expected);
        }

        [Fact]
        public void TryParse_MissingSectionZero_HasNoCharset()
        {
            MimeMediaType result;
            MimeMediaType.TryParse("text/plain;a*1*=%C3%A9", MimeMediaTypeParseOptions.AllowMissingSections, out result).Should().BeFalse();
        }

        [Fact]
        public void TryParse_MissingSectionZeroWithUnknownCharsetsAllowed_KeepsOctets()
        {
            var result = MimeMediaType.Parse("text/plain;a*1*=%C3%A9",
                MimeMediaTypeParseOptions.AllowMissingSections | MimeMediaTypeParseOptions.AllowUnknownCharsets);

            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be("\xC3\xA9");
            result.Parameters[0].Charset.Should().BeEmpty();
        }

        [Fact]
        public void TryParse_AllowUnknownCharsets_KeepsCharsetNameAndOctets()
        {
            var result = MimeMediaType.Parse("text/plain;a*=x-no-such-charset'fr'%E2%82%AC", MimeMediaTypeParseOptions.AllowUnknownCharsets);

            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be("\xE2\x82\xAC");
            result.Parameters[0].Charset.Should().Be("x-no-such-charset");
            result.Parameters[0].Language.Should().Be("fr");
        }

        [Fact]
        public void TryParse_AllowRfc2045Tokens_StillReadsRfc2231Forms()
        {
            var result = MimeMediaType.Parse("text/plain;a~b*=UTF-8''%41;c*d=e", MimeMediaTypeParseOptions.AllowRfc2045Tokens);

            result.Parameters.Should().HaveCount(2);
            result.Parameters[0].Name.Should().Be("a~b");
            result.Parameters[0].Value.Should().Be("A");
            result.Parameters[1].Name.Should().Be("c*d");
            result.Parameters[1].Value.Should().Be("e");
        }

        [Fact]
        public void TryParse_CharsetTheDefaultResolverDoesNotKnow_ReturnsFalse()
        {
            MimeMediaType result;
            MimeMediaType.TryParse("text/plain;a*=x-test''%E2%82%AC", out result).Should().BeFalse();
        }

        [Fact]
        public void Parse_StringWithCharsetResolver_UsesResolver()
        {
            var result = MimeMediaType.Parse("text/plain;a*=x-test''%E2%82%AC", MimeMediaTypeParseOptions.None, TestResolver);

            result.Parameters[0].Value.Should().Be("\x20AC");
        }

        [Fact]
        public void Parse_SpanWithCharsetResolver_UsesResolver()
        {
            var result = MimeMediaType.Parse("text/plain;a*=x-test''%E2%82%AC".AsSpan(), MimeMediaTypeParseOptions.None, TestResolver);

            result.Parameters[0].Value.Should().Be("\x20AC");
        }

        [Fact]
        public void TryParse_StringWithCharsetResolver_UsesResolver()
        {
            MimeMediaType result;
            MimeMediaType.TryParse("text/plain;a*=x-test''%E2%82%AC", MimeMediaTypeParseOptions.None, TestResolver, out result).Should().BeTrue();

            result.Parameters[0].Value.Should().Be("\x20AC");
        }

        [Fact]
        public void TryParse_SpanWithCharsetResolver_UsesResolver()
        {
            MimeMediaType result;
            MimeMediaType.TryParse("text/plain;a*=x-test''%E2%82%AC".AsSpan(), MimeMediaTypeParseOptions.None, TestResolver, out result)
                .Should().BeTrue();

            result.Parameters[0].Value.Should().Be("\x20AC");
        }

        [Fact]
        public void TryParse_NullWithCharsetResolver_ReturnsFalse()
        {
            MimeMediaType result;
            MimeMediaType.TryParse((string)null, MimeMediaTypeParseOptions.None, TestResolver, out result).Should().BeFalse();
        }

        [Fact]
        public void Parse_NullCharsetResolver_ThrowsArgumentNullException()
        {
            MimeMediaType result;
            ((Action)(() => MimeMediaType.Parse("text/plain", MimeMediaTypeParseOptions.None, null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => MimeMediaType.Parse("text/plain".AsSpan(), MimeMediaTypeParseOptions.None, null))).Should().Throw<ArgumentNullException>();
            ((Action)(() => MimeMediaType.TryParse("text/plain", MimeMediaTypeParseOptions.None, null, out result))).Should().Throw<ArgumentNullException>();
            ((Action)(() => MimeMediaType.TryParse("text/plain".AsSpan(), MimeMediaTypeParseOptions.None, null, out result))).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryGetLanguageTag_ValidLanguage_ReturnsTag()
        {
            var parameter = MimeMediaType.Parse("text/plain;title*=us-ascii'en-us'abc").Parameters[0];

            Bcp47LanguageTag languageTag;
            parameter.TryGetLanguageTag(out languageTag).Should().BeTrue();
            languageTag.Should().Be(Bcp47LanguageTag.Create("en-US"));
        }

        [Theory]
        [InlineData("text/plain;title*=us-ascii'123456789'abc", "123456789")]
        [InlineData("text/plain;title*=us-ascii''abc", "")]
        [InlineData("text/plain;title=abc", "")]
        public void TryGetLanguageTag_NoValidLanguage_ReturnsFalse(string value, string language)
        {
            var parameter = MimeMediaType.Parse(value).Parameters[0];

            Bcp47LanguageTag languageTag;
            parameter.TryGetLanguageTag(out languageTag).Should().BeFalse();
            parameter.Language.Should().Be(language);
        }

        [Theory]
        [InlineData("na\xEFve.txt", "name*=UTF-8''na%C3%AFve.txt")]
        [InlineData("a\r\nb", "name*=UTF-8''a%0D%0Ab")]
        [InlineData("a\x7F", "name*=UTF-8''a%7F")]
        [InlineData("\x20AC 5", "name*=UTF-8''%E2%82%AC%205")]
        [InlineData("\xD83D\xDE00", "name*=UTF-8''%F0%9F%98%80")]
        [InlineData("caf\xE9 *'%", "name*=UTF-8''caf%C3%A9%20%2A%27%25")]
        public void ToString_ValueThatIsNotUsAscii_WritesExtendedValue(string value, string expected)
        {
            var parameter = new MimeParameter("name", value);

            parameter.ToString().Should().Be(expected);
        }

        [Theory]
        [InlineData("text/plain;name*=UTF-8''na%C3%AFve.txt", "na\xEFve.txt")]
        [InlineData("text/plain;name*=UTF-8''a%0D%0Ab", "a\r\nb")]
        [InlineData("text/plain;name*=UTF-8''%F0%9F%98%80", "\xD83D\xDE00")]
        [InlineData("text/plain;name*=UTF-8''caf%C3%A9%20%2A%27%25", "caf\xE9 *'%")]
        public void Parse_WrittenExtendedValue_RoundTrips(string value, string expected)
        {
            var result = MimeMediaType.Parse(value);

            result.Parameters[0].Should().Be(new MimeParameter("name", expected));
        }

        [Fact]
        public void ToString_Language_WritesExtendedValue()
        {
            var parameter = new MimeParameter("title", "This is ***fun***", Bcp47LanguageTag.Create("en-US"));

            parameter.Language.Should().Be("en-US");
            parameter.Charset.Should().BeEmpty();
            parameter.ToString().Should().Be("title*=UTF-8'en-US'This%20is%20%2A%2A%2Afun%2A%2A%2A");
        }

        [Fact]
        public void TryGetLanguageTag_ConstructedWithLanguage_ReturnsTag()
        {
            var parameter = new MimeParameter("title", "value", Bcp47LanguageTag.Create("en-US"));

            Bcp47LanguageTag languageTag;
            parameter.TryGetLanguageTag(out languageTag).Should().BeTrue();
            languageTag.ToString().Should().Be("en-US");
        }

        [Fact]
        public void ToString_ParsedParameterInOtherCharset_WritesUtf8()
        {
            var parameter = MimeMediaType.Parse("text/plain;name*=iso-8859-1'en'%A3").Parameters[0];

            parameter.ToString().Should().Be("name*=UTF-8'en'%C2%A3");
        }

        [Fact]
        public void ToString_ParsedUsAsciiValueWithNoLanguage_WritesPlainValue()
        {
            var parameter = MimeMediaType.Parse("text/plain;name*=us-ascii''a%20b").Parameters[0];

            parameter.ToString().Should().Be("name=\"a b\"");
        }

        [Fact]
        public void Constructor_UninitialisedLanguage_ThrowsArgumentException()
        {
            Action act = () => new MimeParameter("title", "value", default(Bcp47LanguageTag));

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryFormat_ExtendedValue_WritesValue()
        {
            var parameter = new MimeParameter("name", "\xE9");
            var buffer = new char[19];

            int charsWritten;
            parameter.TryFormat(buffer, out charsWritten).Should().BeTrue();

            new string(buffer, 0, charsWritten).Should().Be("name*=UTF-8''%C3%A9");
        }

        [Fact]
        public void TryFormat_ExtendedValueDestinationTooShort_ReturnsFalse()
        {
            var parameter = new MimeParameter("name", "\xE9");

            int charsWritten;
            parameter.TryFormat(new char[18], out charsWritten).Should().BeFalse();

            charsWritten.Should().Be(0);
        }

        [Fact]
        public void ToString_MediaTypeWithExtendedParameters_WritesExtendedValues()
        {
            var mediaType = MimeMediaType.ApplicationOctetStream
                .WithParameter("name", "r\xE9sum\xE9.pdf")
                .WithParameter(new MimeParameter("title", "Lebenslauf", Bcp47LanguageTag.Create("de")));

            mediaType.ToString().Should().Be("application/octet-stream;name*=UTF-8''r%C3%A9sum%C3%A9.pdf;title*=UTF-8'de'Lebenslauf");
        }

        [Fact]
        public void Parse_MediaTypeWithExtendedParameters_RoundTrips()
        {
            var expected = MimeMediaType.ApplicationOctetStream
                .WithParameter("name", "r\xE9sum\xE9.pdf")
                .WithParameter(new MimeParameter("title", "Lebenslauf", Bcp47LanguageTag.Create("de")));

            var result = MimeMediaType.Parse("application/octet-stream;name*=UTF-8''r%C3%A9sum%C3%A9.pdf;title*=UTF-8'de'Lebenslauf");

            result.Should().Be(expected);
        }

        [Fact]
        public void DataUri_ExtendedParameter_RoundTrips()
        {
            var mediaType = MimeMediaType.TextPlain.WithParameter("name", "\x20AC");
            var dataUri = MimeDataUri.Create(mediaType, new byte[] { 1 });

            var result = MimeDataUri.Parse(dataUri.ToString());

            result.MediaType.Should().Be(mediaType);
        }

        [Theory]
        [InlineData("en", "en")]
        [InlineData("en", "EN")]
        [InlineData("en-US", "en-us")]
        public void Equals_SameLanguageInAnyCase_ReturnsTrue(string left, string right)
        {
            var leftParameter = new MimeParameter("name", "value", Bcp47LanguageTag.Create(left));
            var rightParameter = new MimeParameter("NAME", "value", Bcp47LanguageTag.Create(right));

            leftParameter.Equals(rightParameter).Should().BeTrue();
            leftParameter.GetHashCode().Should().Be(rightParameter.GetHashCode());
        }

        [Fact]
        public void Equals_DifferentLanguages_ReturnsFalse()
        {
            var left = new MimeParameter("name", "value", Bcp47LanguageTag.Create("en"));
            var right = new MimeParameter("name", "value", Bcp47LanguageTag.Create("fr"));

            left.Equals(right).Should().BeFalse();
        }

        [Fact]
        public void Equals_LanguageAndNoLanguage_ReturnsFalse()
        {
            var left = new MimeParameter("name", "value", Bcp47LanguageTag.Create("en"));
            var right = new MimeParameter("name", "value");

            left.Equals(right).Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentCharsets_ReturnsTrue()
        {
            var parsed = MimeMediaType.Parse("text/plain;name*=iso-8859-1''%E9").Parameters[0];
            var constructed = new MimeParameter("name", "\xE9");

            parsed.Equals(constructed).Should().BeTrue();
            parsed.GetHashCode().Should().Be(constructed.GetHashCode());
        }

        [Fact]
        public void FormatSections_ParameterFits_ReturnsParameter()
        {
            var parameter = new MimeParameter("name", "\xE9");

            var sections = parameter.FormatSections(19);

            sections.Should().Equal("name*=UTF-8''%C3%A9");
        }

        [Fact]
        public void FormatSections_LongUsAsciiValue_SplitsIntoPlainSections()
        {
            var parameter = new MimeParameter("filename", "a-very-long-file-name-that-does-not-fit.txt");

            var sections = parameter.FormatSections(20);

            sections.Should().Equal("filename*0=a-very-lo", "filename*1=ng-file-n", "filename*2=ame-that-", "filename*3=does-not-",
                "filename*4=fit.txt");
        }

        [Fact]
        public void FormatSections_ValueThatNeedsQuoting_QuotesEachSectionThatIsNotAToken()
        {
            var parameter = new MimeParameter("title", "two words \"quoted\" and-a-token");

            var sections = parameter.FormatSections(18);

            sections.Should().Equal("title*0=\"two word\"", "title*1=\"s \\\"quot\"", "title*2=\"ed\\\" and\"", "title*3=-a-token");
        }

        [Fact]
        public void FormatSections_LongExtendedValue_SplitsIntoExtendedSectionsWithoutSplittingCharacters()
        {
            var parameter = new MimeParameter("filename", new string('\x20AC', 20), Bcp47LanguageTag.Create("en"));

            var sections = parameter.FormatSections(40);

            sections.Should().Equal(
                "filename*0*=UTF-8'en'%E2%82%AC%E2%82%AC",
                "filename*1*=" + ThreeEuros,
                "filename*2*=" + ThreeEuros,
                "filename*3*=" + ThreeEuros,
                "filename*4*=" + ThreeEuros,
                "filename*5*=" + ThreeEuros,
                "filename*6*=" + ThreeEuros);
        }

        [Fact]
        public void FormatSections_ManySections_NumbersThemInOrder()
        {
            // Sections 0 to 9 hold three characters each, and the longer names of the sections after them leave room for two.
            var parameter = new MimeParameter("a", new string('x', 40));

            var sections = parameter.FormatSections(7);

            sections.Should().Equal("a*0=xxx", "a*1=xxx", "a*2=xxx", "a*3=xxx", "a*4=xxx", "a*5=xxx", "a*6=xxx", "a*7=xxx", "a*8=xxx",
                "a*9=xxx", "a*10=xx", "a*11=xx", "a*12=xx", "a*13=xx", "a*14=xx");
        }

        [Fact]
        public void FormatSections_BudgetTooSmallForOneCharacter_WritesOneCharacterPerSection()
        {
            var parameter = new MimeParameter("name", "\xE9\xD83D\xDE00", Bcp47LanguageTag.Create("en"));

            var sections = parameter.FormatSections(1);

            sections.Should().Equal("name*0*=UTF-8'en'%C3%A9", "name*1*=%F0%9F%98%80");
        }

        [Fact]
        public void FormatSections_EmptyValueWithLanguage_WritesOneSection()
        {
            var parameter = new MimeParameter("name", string.Empty, Bcp47LanguageTag.Create("en"));

            var sections = parameter.FormatSections(5);

            sections.Should().Equal("name*0*=UTF-8'en'");
        }

        [Theory]
        [InlineData("text/plain;name*0*=UTF-8'en'%C3%A9;name*1*=%F0%9F%98%80", "\xE9\xD83D\xDE00")]
        [InlineData("text/plain;name*0*=UTF-8'en'", "")]
        public void Parse_SectionsWithLanguage_RoundTrips(string value, string expected)
        {
            var result = MimeMediaType.Parse(value);

            result.Parameters[0].Should().Be(new MimeParameter("name", expected, Bcp47LanguageTag.Create("en")));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void FormatSections_LengthLessThanOne_ThrowsArgumentOutOfRangeException(int maxSectionLength)
        {
            Action act = () => new MimeParameter("name", "value").FormatSections(maxSectionLength);

            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Theory]
        [InlineData("plain-token", 12)]
        [InlineData("a \"quoted\" \\ value", 15)]
        [InlineData("caf\xE9 cr\xE8me br\xFBl\x00E9e", 20)]
        [InlineData("\x65E5\x672C\x8A9E\x306E\x30D5\x30A1\x30A4\x30EB\x540D.txt", 30)]
        [InlineData("tab\there\r\nnew line", 16)]
        public void FormatSections_NoLanguage_RoundTripsWithinLength(string value, int maxSectionLength)
        {
            var parameter = new MimeParameter("name", value);

            var sections = parameter.FormatSections(maxSectionLength);

            sections.Should().OnlyContain(section => section.Length <= maxSectionLength);
            MimeMediaType.Parse("text/plain;" + string.Join(";", sections)).Parameters[0].Should().Be(parameter);
        }

        [Theory]
        [InlineData("plain-token", 18)]
        [InlineData("a \"quoted\" \\ value", 23)]
        [InlineData("caf\xE9 cr\xE8me br\xFBl\x00E9e", 23)]
        [InlineData("\x65E5\x672C\x8A9E\x306E\x30D5\x30A1\x30A4\x30EB\x540D.txt", 26)]
        [InlineData("tab\there\r\nnew line", 18)]
        public void FormatSections_Language_RoundTripsWithinLength(string value, int maxSectionLength)
        {
            var parameter = new MimeParameter("name", value, Bcp47LanguageTag.Create("ja"));

            var sections = parameter.FormatSections(maxSectionLength);

            sections.Should().OnlyContain(section => section.Length <= maxSectionLength);
            MimeMediaType.Parse("text/plain;" + string.Join(";", sections)).Parameters[0].Should().Be(parameter);
        }

        private static readonly MimeCharsetResolver TestResolver = new MimeCharsetResolver(new Utf8CharsetResolver());

        // Resolves every character set as UTF-8.
        private sealed class Utf8CharsetResolver : IMimeCharsetResolver
        {
            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
            {
                encoding = Encoding.UTF8;
                return true;
            }
        }
    }
}
