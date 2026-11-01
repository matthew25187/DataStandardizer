using System;
using System.Text;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.Mime
{
    public class MimeRfc2231Tests
    {
        // ref. RFC 2231 §3: a parameter value split into two sections.
        private const string Rfc2231Section3Example =
            "message/external-body; access-type=URL; URL*0=\"ftp://\"; URL*1=\"cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar\"";
        private const string Rfc2231Section3ExampleName = "URL";
        private const string Rfc2231Section3ExampleValue = "ftp://cs.utk.edu/pub/moore/bulk-mailer/bulk-mailer.tar";

        // ref. RFC 2231 §4: an extended value with a character set and language.
        private const string Rfc2231Section4Example = "application/x-stuff; title*=us-ascii'en-us'This%20is%20%2A%2A%2Afun%2A%2A%2A";
        private const string Rfc2231Section4ExampleValue = "This is ***fun***";
        private const string Rfc2231Section4ExampleLanguage = "en-us";

        // ref. RFC 2231 §4.1: extended and plain sections of one value.
        private const string Rfc2231Section41Example =
            "application/x-stuff; title*0*=us-ascii'en'This%20is%20even%20more%20; title*1*=%2A%2A%2Afun%2A%2A%2A%20; title*2=\"isn't it!\"";
        private const string Rfc2231Section41ExampleValue = "This is even more ***fun*** isn't it!";
        private const string Rfc2231Section41ExampleLanguage = "en";

        // ref. RFC 2231 §4, §4.1: the character set and parameter name of the §4 and §4.1 examples.
        private const string Rfc2231ExampleCharset = "us-ascii";
        private const string Rfc2231ExampleName = "title";

        private const MimeMediaTypeParseOptions AllRfc2231Options = MimeMediaTypeParseOptions.AllowNonstandardSections |
            MimeMediaTypeParseOptions.AllowUnknownCharsets | MimeMediaTypeParseOptions.AllowNonstandardExtendedValues |
            MimeMediaTypeParseOptions.AllowInvalidOctets;
        private const MimeMediaTypeParseOptions MissingSectionsAndUnknownCharsets =
            MimeMediaTypeParseOptions.AllowMissingSections | MimeMediaTypeParseOptions.AllowUnknownCharsets;

        private const string ParameterName = "name";
        private const string ParameterValue = "value";
        private const string English = "en";
        private const string Utf8 = "UTF-8";
        private const string EAcute = "\xE9";
        private const string Euro = "\x20AC";

        // ref. The Unicode Standard §23.8: U+FFFD REPLACEMENT CHARACTER, which an invalid octet sequence is read as.
        private const string ReplacementCharacter = "\xFFFD";

        // The letter a, followed by U+FFFD for the first octet of a two-octet UTF-8 sequence with no second octet.
        private const string ACharacterThenReplacementCharacter = "a\xFFFD";

        // An octet that is not valid UTF-8 (RFC 3629 §3: 0xFF never appears), in a character set that only a test resolver knows.
        private const string InvalidUtf8MediaType = "text/plain;a*=x-test''%FF";

        // The euro sign in UTF-8, in a character set that the default resolver doesn't know.
        private const string UnknownCharsetMediaType = "text/plain;a*=x-test''%E2%82%AC";

        // A parameter whose value is e-acute, which is written as two octets of UTF-8.
        private const string EAcuteParameter = "name*=UTF-8''%C3%A9";
        private const int EAcuteParameterLength = 19;
        private const int ShorterThanEAcuteParameter = 18;

        // A data URI of one octet, 0x01, whose media type has a parameter whose value is the euro sign.
        private const string EuroDataUri = "data:text/plain;name*=UTF-8''%25E2%2582%25AC;base64,AQ==";

        private const string TitleParameterName = "title";
        private const string FileNameParameterName = "filename";
        private const string UpperCaseParameterName = "NAME";
        private const string ResumeTitle = "Lebenslauf";
        private const string German = "de";
        private const string AmericanEnglish = "en-US";
        private const string Japanese = "ja";
        private const string JapaneseFileName = "\x65E5\x672C\x8A9E\x306E\x30D5\x30A1\x30A4\x30EB\x540D.txt";

        // An e-acute in ISO-8859-1, which decodes to the same value as EAcute.
        private const string Latin1EAcuteMediaType = "text/plain;name*=iso-8859-1''%E9";

        private const string ResumeMediaType = "application/octet-stream;name*=UTF-8''r%C3%A9sum%C3%A9.pdf;title*=UTF-8'de'Lebenslauf";
        private const string ResumeFileName = "r\xE9sum\xE9.pdf";

        public delegate bool TryParseCall(out MimeMediaType result);

        public static TheoryData<string> DeviationValues => CreateDeviationValues();

        public static TheoryData<string, MimeMediaTypeParseOptions> DeviationValuesWithOtherOptions => CreateDeviationValuesWithOtherOptions();

        public static TheoryData<string, MimeMediaTypeParseOptions, string> Deviations => CreateDeviations();

        public static TheoryData<string, string> SameLanguagesInAnyCase => new TheoryData<string, string>
        {
            { English, English },
            { English, "EN" },
            { AmericanEnglish, "en-us" },
        };

        public static TheoryData<Func<MimeMediaType>> ParseCallsWithResolver => new TheoryData<Func<MimeMediaType>>
        {
            () => MimeMediaType.Parse(UnknownCharsetMediaType, MimeMediaTypeParseOptions.None, CreateResolver()),
            () => MimeMediaType.Parse(UnknownCharsetMediaType.AsSpan(), MimeMediaTypeParseOptions.None, CreateResolver()),
        };

        public static TheoryData<TryParseCall> TryParseCallsWithResolver => new TheoryData<TryParseCall>
        {
            (out MimeMediaType result) => MimeMediaType.TryParse(UnknownCharsetMediaType, MimeMediaTypeParseOptions.None, CreateResolver(), out result),
            (out MimeMediaType result) => MimeMediaType.TryParse(UnknownCharsetMediaType.AsSpan(), MimeMediaTypeParseOptions.None, CreateResolver(), out result),
        };

        public static TheoryData<Action> CallsWithNullResolver => new TheoryData<Action>
        {
            () => MimeMediaType.Parse(UnknownCharsetMediaType, MimeMediaTypeParseOptions.None, null),
            () => MimeMediaType.Parse(UnknownCharsetMediaType.AsSpan(), MimeMediaTypeParseOptions.None, null),
            () => MimeMediaType.TryParse(UnknownCharsetMediaType, MimeMediaTypeParseOptions.None, null, out _),
            () => MimeMediaType.TryParse(UnknownCharsetMediaType.AsSpan(), MimeMediaTypeParseOptions.None, null, out _),
        };

        [Fact]
        public void Parse_Rfc2231Section3Example_JoinsSections()
        {
            // act
            var result = MimeMediaType.Parse(Rfc2231Section3Example, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

            // assert
            result.Parameters.Should().HaveCount(2);
            result.Parameters[1].Name.Should().Be(Rfc2231Section3ExampleName);
            result.Parameters[1].Value.Should().Be(Rfc2231Section3ExampleValue);
            result.Parameters[1].Charset.Should().BeEmpty();
            result.Parameters[1].Language.Should().BeEmpty();
        }

        [Fact]
        public void Parse_Rfc2231Section4Example_DecodesCharsetAndLanguage()
        {
            // act
            var result = MimeMediaType.Parse(Rfc2231Section4Example, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

            // assert
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Name.Should().Be(Rfc2231ExampleName);
            result.Parameters[0].Value.Should().Be(Rfc2231Section4ExampleValue);
            result.Parameters[0].Charset.Should().Be(Rfc2231ExampleCharset);
            result.Parameters[0].Language.Should().Be(Rfc2231Section4ExampleLanguage);
        }

        [Fact]
        public void Parse_Rfc2231Section41Example_JoinsExtendedAndPlainSections()
        {
            // act
            var result = MimeMediaType.Parse(Rfc2231Section41Example, MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters);

            // assert
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Name.Should().Be(Rfc2231ExampleName);
            result.Parameters[0].Value.Should().Be(Rfc2231Section41ExampleValue);
            result.Parameters[0].Charset.Should().Be(Rfc2231ExampleCharset);
            result.Parameters[0].Language.Should().Be(Rfc2231Section41ExampleLanguage);
        }

        [Theory]
        [InlineData("text/plain;name*=UTF-8''na%C3%AFve.txt", "na\xEFve.txt", Utf8)]
        [InlineData("text/plain;name*=iso-8859-1''%A3%20rates", "\xA3 rates", "iso-8859-1")]
        [InlineData("text/plain;name*=UTF-8''%F0%9F%98%80", "\xD83D\xDE00", Utf8)]
        [InlineData("text/plain;name*=''abc", "abc", "")]
        [InlineData("text/plain;name*=UTF-8''", "", Utf8)]
        [InlineData("text/plain;name*0*=UTF-8''%E2%82;name*1*=%AC", Euro, Utf8)]
        [InlineData("text/plain;name*0*=UTF-8''a;name*1=%41", "a%41", Utf8)]
        [InlineData("text/plain;name*0=a;name*1*=%41", "aA", "")]
        public void Parse_ExtendedValue_DecodesCharset(string value, string expected, string charset)
        {
            // act
            var result = MimeMediaType.Parse(value);

            // assert
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be(expected);
            result.Parameters[0].Charset.Should().Be(charset);
            result.Parameters[0].Language.Should().BeEmpty();
        }

        [Fact]
        public void Parse_SectionsOutOfOrder_JoinsInNumericOrder()
        {
            // act
            var result = MimeMediaType.Parse("text/plain;x=1;a*11=l;a*10=k;a*9=j;a*8=i;a*7=h;a*6=g;a*5=f;a*4=e;a*3=d;a*2=c;a*1=b;a*0=a;y=2");

            // assert
            result.Parameters.Should().HaveCount(3);
            result.Parameters[0].Name.Should().Be("x");
            result.Parameters[1].Name.Should().Be("a");
            result.Parameters[1].Value.Should().Be("abcdefghijkl");
            result.Parameters[2].Name.Should().Be("y");
        }

        [Theory]
        // ref. RFC 2231 §7: extended-other-values := *(ext-octet / attribute-char)
        [InlineData("text/plain;a*0*=UTF-8''abc;a*1*=", "abc")]
        [InlineData("text/plain;a*0*=UTF-8'';a*1*=abc", "abc")]
        public void Parse_EmptyExtendedSection_JoinsSections(string value, string expected)
        {
            // act
            var result = MimeMediaType.Parse(value);

            // assert
            result.Parameters[0].Value.Should().Be(expected);
        }

        [Theory]
        // ref. RFC 2231 §7: extended-initial-value := [charset] "'" [language] "'" extended-other-values, and a regular value is a token.
        [InlineData("text/plain;a*=")]
        [InlineData("text/plain;a*0*=;a*1*=abc")]
        [InlineData("text/plain;a*0=abc;a*1=")]
        public void TryParse_EmptyValueThatMustNotBeEmpty_ReturnsFalse(string value)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Fact]
        public void TryParse_OctetsThatTheEncodingRejects_ReturnsFalse()
        {
            // arrange
            var resolver = new MimeCharsetResolver(new ThrowingUtf8CharsetResolver());

            // act
            var parsed = MimeMediaType.TryParse(InvalidUtf8MediaType, MimeMediaTypeParseOptions.None, resolver, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Fact]
        public void Parse_OctetsThatTheEncodingRejectsWithInvalidOctetsAllowed_ReadsReplacementCharacter()
        {
            // arrange
            var resolver = new MimeCharsetResolver(new ThrowingUtf8CharsetResolver());

            // act
            var result = MimeMediaType.Parse(InvalidUtf8MediaType, MimeMediaTypeParseOptions.AllowInvalidOctets, resolver);

            // assert
            result.Parameters[0].Value.Should().Be(ReplacementCharacter);
        }

        [Fact]
        public void Parse_SectionsPastNine_JoinsInNumericOrder()
        {
            // act
            var result = MimeMediaType.Parse("text/plain;a*10=k;a*0=a;a*1=b;a*2=c;a*3=d;a*4=e;a*5=f;a*6=g;a*7=h;a*8=i;a*9=j");

            // assert
            result.Parameters[0].Value.Should().Be("abcdefghijk");
        }

        [Fact]
        public void Parse_SectionNamesInMixedCase_AreOneParameter()
        {
            // act
            var result = MimeMediaType.Parse("text/plain;Title*0=a;TITLE*1=b");

            // assert
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
            // act
            var result = MimeMediaType.Parse(value);

            // assert
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be("real");
        }

        [Fact]
        public void Parse_PlainAndRfc2231Forms_KeepsPositionOfFirstForm()
        {
            // act
            var result = MimeMediaType.Parse("text/plain;name=fallback;x=1;name*=UTF-8''real");

            // assert
            result.Parameters.Should().HaveCount(2);
            result.Parameters[0].Name.Should().Be(ParameterName);
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
        // A parameter that is given twice, or both as name* and in sections.
        [InlineData("text/plain;name*=''a;name*=''b")]
        [InlineData("text/plain;name*=''a;name*0=b")]
        [InlineData("text/plain;name*0=b;name*=''a")]
        [InlineData("text/plain;name=a;name=b;name*=''c")]
        public void TryParse_InvalidRfc2231Form_ReturnsFalse(string value)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, AllRfc2231Options, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Theory]
        [InlineData("text/plain;a*0=x;a*2=z", MimeMediaTypeParseOptions.AllowNonstandardSections)]
        [InlineData("text/plain;a*0=x;a*0=y", MimeMediaTypeParseOptions.AllowNonstandardSections)]
        [InlineData("text/plain;a*00=x;a*01=y", MimeMediaTypeParseOptions.AllowNonstandardSections)]
        [InlineData("text/plain;a*=\"UTF-8''a%20b\"", MimeMediaTypeParseOptions.AllowNonstandardExtendedValues)]
        [InlineData("text/plain;a*=UTF-8''%C3%a9", MimeMediaTypeParseOptions.AllowNonstandardExtendedValues)]
        public void TryParse_DeviationWithItsGroupOption_ReturnsTrue(string value, MimeMediaTypeParseOptions options)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, options, out _);

            // assert
            parsed.Should().BeTrue();
        }

        [Fact]
        public void TryParse_UnknownCharsetWithNonstandardExtendedValues_ReturnsFalse()
        {
            // act
            var parsed = MimeMediaType.TryParse(UnknownCharsetMediaType, MimeMediaTypeParseOptions.AllowNonstandardExtendedValues, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DeviationValues))]
        public void TryParse_Rfc2231DeviationParsedStrictly_ReturnsFalse(string value)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(DeviationValuesWithOtherOptions))]
        public void TryParse_Rfc2231DeviationWithEveryOtherOption_ReturnsFalse(string value, MimeMediaTypeParseOptions otherOptions)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, otherOptions, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Deviations))]
        public void Parse_Rfc2231DeviationOptedIn_ReturnsDecodedValue(string value, MimeMediaTypeParseOptions option, string expected)
        {
            // act
            var result = MimeMediaType.Parse(value, option);

            // assert
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be(expected);
        }

        [Fact]
        public void TryParse_MissingSectionZeroWithOctetsThatAreNotUsAscii_ReturnsFalse()
        {
            // act
            var parsed = MimeMediaType.TryParse("text/plain;a*1*=%C3%A9", MimeMediaTypeParseOptions.AllowMissingSections, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Fact]
        public void Parse_MissingSectionZeroWithUnknownCharsetsAllowed_KeepsOctets()
        {
            // act
            var result = MimeMediaType.Parse("text/plain;a*1*=%C3%A9", MissingSectionsAndUnknownCharsets);

            // assert
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be("\xC3\xA9");
            result.Parameters[0].Charset.Should().BeEmpty();
        }

        [Fact]
        public void Parse_UnknownCharsetAllowed_KeepsCharsetNameAndOctets()
        {
            // act
            var result = MimeMediaType.Parse("text/plain;a*=x-no-such-charset'fr'%E2%82%AC", MimeMediaTypeParseOptions.AllowUnknownCharsets);

            // assert
            result.Parameters.Should().ContainSingle();
            result.Parameters[0].Value.Should().Be("\xE2\x82\xAC");
            result.Parameters[0].Charset.Should().Be("x-no-such-charset");
            result.Parameters[0].Language.Should().Be("fr");
        }

        [Fact]
        public void Parse_Rfc2045TokensAllowed_StillReadsRfc2231Forms()
        {
            // act
            var result = MimeMediaType.Parse("text/plain;a~b*=UTF-8''%41;c*d=e", MimeMediaTypeParseOptions.AllowRfc2045Tokens);

            // assert
            result.Parameters.Should().HaveCount(2);
            result.Parameters[0].Name.Should().Be("a~b");
            result.Parameters[0].Value.Should().Be("A");
            result.Parameters[1].Name.Should().Be("c*d");
            result.Parameters[1].Value.Should().Be("e");
        }

        [Fact]
        public void TryParse_CharsetTheDefaultResolverDoesNotKnow_ReturnsFalse()
        {
            // act
            var parsed = MimeMediaType.TryParse(UnknownCharsetMediaType, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ParseCallsWithResolver))]
        public void Parse_CharsetResolver_UsesResolver(Func<MimeMediaType> parse)
        {
            // act
            var result = parse();

            // assert
            result.Parameters[0].Value.Should().Be(Euro);
        }

        [Theory]
        [MemberData(nameof(TryParseCallsWithResolver))]
        public void TryParse_CharsetResolver_UsesResolver(TryParseCall tryParse)
        {
            // arrange
            MimeMediaType result;

            // act
            var parsed = tryParse(out result);

            // assert
            parsed.Should().BeTrue();
            result.Parameters[0].Value.Should().Be(Euro);
        }

        [Fact]
        public void TryParse_NullWithCharsetResolver_ReturnsFalse()
        {
            // arrange
            var resolver = CreateResolver();

            // act
            var parsed = MimeMediaType.TryParse((string)null, MimeMediaTypeParseOptions.None, resolver, out _);

            // assert
            parsed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(CallsWithNullResolver))]
        public void Parse_NullCharsetResolver_ThrowsArgumentNullException(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("text/plain;title*=us-ascii'en-us'abc", "en-us")]
        [InlineData("text/plain;title*=us-ascii'i-klingon'abc", "i-klingon")]
        [InlineData("text/plain;title*=us-ascii'abcdefgh-a'abc", "abcdefgh-a")]
        [InlineData("text/plain;title*=us-ascii''abc", "")]
        [InlineData("text/plain;title=abc", "")]
        public void Parse_Language_KeepsRfc1766LanguageTag(string value, string language)
        {
            // act
            var result = MimeMediaType.Parse(value);

            // assert
            result.Parameters[0].Language.Should().Be(language);
        }

        [Theory]
        // ref. RFC 1766 §2: Language-Tag = Primary-tag *( "-" Subtag ); Primary-tag = 1*8ALPHA; Subtag = 1*8ALPHA
        [InlineData("text/plain;title*=us-ascii'es-419'abc")]
        [InlineData("text/plain;title*=us-ascii'de-1996'abc")]
        [InlineData("text/plain;title*=us-ascii'123'abc")]
        [InlineData("text/plain;title*=us-ascii'abcdefghi'abc")]
        [InlineData("text/plain;title*=us-ascii'en-abcdefghi'abc")]
        [InlineData("text/plain;title*=us-ascii'-en'abc")]
        [InlineData("text/plain;title*=us-ascii'en-'abc")]
        [InlineData("text/plain;title*=us-ascii'en--us'abc")]
        public void TryParse_LanguageThatIsNotRfc1766_ReturnsFalse(string value)
        {
            // act
            var parsed = MimeMediaType.TryParse(value, AllRfc2231Options, out _);

            // assert
            parsed.Should().BeFalse();
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
            // arrange
            var parameter = new MimeParameter(ParameterName, value);

            // act
            var result = parameter.ToString();

            // assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("text/plain;name*=UTF-8''na%C3%AFve.txt", "na\xEFve.txt")]
        [InlineData("text/plain;name*=UTF-8''a%0D%0Ab", "a\r\nb")]
        [InlineData("text/plain;name*=UTF-8''%F0%9F%98%80", "\xD83D\xDE00")]
        [InlineData("text/plain;name*=UTF-8''caf%C3%A9%20%2A%27%25", "caf\xE9 *'%")]
        public void Parse_WrittenExtendedValue_ReturnsParameter(string value, string parameterValue)
        {
            // arrange
            var expected = new MimeParameter(ParameterName, parameterValue);

            // act
            var result = MimeMediaType.Parse(value);

            // assert
            result.Parameters[0].Should().Be(expected);
        }

        [Fact]
        public void ToString_Language_WritesExtendedValue()
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, "a b", AmericanEnglish);

            // act
            var result = parameter.ToString();

            // assert
            result.Should().Be("name*=UTF-8'en-US'a%20b");
        }

        [Fact]
        public void Language_ConstructedWithLanguage_ReturnsLanguageAsGiven()
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, ParameterValue, AmericanEnglish);

            // act
            var result = parameter.Language;

            // assert
            result.Should().Be(AmericanEnglish);
        }

        [Fact]
        public void Charset_ConstructedWithLanguage_ReturnsEmpty()
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, ParameterValue, English);

            // act
            var result = parameter.Charset;

            // assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ToString_ParsedParameterInOtherCharset_WritesUtf8()
        {
            // arrange
            var parameter = MimeMediaType.Parse("text/plain;name*=iso-8859-1'en'%A3").Parameters[0];

            // act
            var result = parameter.ToString();

            // assert
            result.Should().Be("name*=UTF-8'en'%C2%A3");
        }

        [Fact]
        public void ToString_ParsedUsAsciiValueWithNoLanguage_WritesPlainValue()
        {
            // arrange
            var parameter = MimeMediaType.Parse("text/plain;name*=us-ascii''a%20b").Parameters[0];

            // act
            var result = parameter.ToString();

            // assert
            result.Should().Be("name=\"a b\"");
        }

        [Theory]
        [InlineData("")]
        [InlineData("es-419")]
        [InlineData("abcdefghi")]
        [InlineData("en-")]
        [InlineData("en_US")]
        public void Constructor_LanguageThatIsNotRfc1766_ThrowsArgumentException(string language)
        {
            // act
            Action testAction = () => new MimeParameter(ParameterName, ParameterValue, language);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullLanguage_ThrowsArgumentNullException()
        {
            // act
            Action testAction = () => new MimeParameter(ParameterName, ParameterValue, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryFormat_ExtendedValue_WritesValue()
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, EAcute);
            var buffer = new char[EAcuteParameterLength];
            int charsWritten;

            // act
            var formatted = parameter.TryFormat(buffer, out charsWritten);

            // assert
            formatted.Should().BeTrue();
            charsWritten.Should().Be(EAcuteParameterLength);
            new string(buffer).Should().Be(EAcuteParameter);
        }

        [Fact]
        public void TryFormat_ExtendedValueDestinationTooShort_ReturnsFalse()
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, EAcute);
            var buffer = new char[ShorterThanEAcuteParameter];
            int charsWritten;

            // act
            var formatted = parameter.TryFormat(buffer, out charsWritten);

            // assert
            formatted.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void ToString_MediaTypeWithExtendedParameters_WritesExtendedValues()
        {
            // arrange
            var mediaType = MimeMediaType.ApplicationOctetStream
                .WithParameter(ParameterName, ResumeFileName)
                .WithParameter(new MimeParameter(TitleParameterName, ResumeTitle, German));

            // act
            var result = mediaType.ToString();

            // assert
            result.Should().Be(ResumeMediaType);
        }

        [Fact]
        public void Parse_MediaTypeWithExtendedParameters_ReturnsMediaType()
        {
            // arrange
            var expected = MimeMediaType.ApplicationOctetStream
                .WithParameter(ParameterName, ResumeFileName)
                .WithParameter(new MimeParameter(TitleParameterName, ResumeTitle, German));

            // act
            var result = MimeMediaType.Parse(ResumeMediaType);

            // assert
            result.Should().Be(expected);
        }

        [Fact]
        public void Create_MediaTypeWithExtendedParameter_PercentEncodesItsPercentSigns()
        {
            // arrange
            var mediaType = MimeMediaType.TextPlain.WithParameter(ParameterName, Euro);
            var data = new byte[] { 1 };

            // act
            var result = MimeDataUri.Create(mediaType, data).ToString();

            // assert
            result.Should().Be(EuroDataUri);
        }

        [Fact]
        public void Parse_DataUriWithExtendedParameter_DecodesParameter()
        {
            // arrange
            var expected = MimeMediaType.TextPlain.WithParameter(ParameterName, Euro);

            // act
            var result = MimeDataUri.Parse(EuroDataUri);

            // assert
            result.MediaType.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(SameLanguagesInAnyCase))]
        public void Equals_SameLanguageInAnyCase_ReturnsTrue(string leftLanguage, string rightLanguage)
        {
            // arrange
            var left = new MimeParameter(ParameterName, ParameterValue, leftLanguage);
            var right = new MimeParameter(UpperCaseParameterName, ParameterValue, rightLanguage);

            // act
            var result = left.Equals(right);

            // assert
            result.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(SameLanguagesInAnyCase))]
        public void GetHashCode_SameLanguageInAnyCase_ReturnsSameHashCode(string leftLanguage, string rightLanguage)
        {
            // arrange
            var left = new MimeParameter(ParameterName, ParameterValue, leftLanguage);
            var right = new MimeParameter(UpperCaseParameterName, ParameterValue, rightLanguage);

            // act
            var result = (Left: left.GetHashCode(), Right: right.GetHashCode());

            // assert
            result.Left.Should().Be(result.Right);
        }

        [Fact]
        public void Equals_DifferentLanguages_ReturnsFalse()
        {
            // arrange
            var left = new MimeParameter(ParameterName, ParameterValue, English);
            var right = new MimeParameter(ParameterName, ParameterValue, "fr");

            // act
            var result = left.Equals(right);

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_LanguageAndNoLanguage_ReturnsFalse()
        {
            // arrange
            var left = new MimeParameter(ParameterName, ParameterValue, English);
            var right = new MimeParameter(ParameterName, ParameterValue);

            // act
            var result = left.Equals(right);

            // assert
            result.Should().BeFalse();
        }

        [Fact]
        public void Equals_DifferentCharsets_ReturnsTrue()
        {
            // arrange
            var parsed = MimeMediaType.Parse(Latin1EAcuteMediaType).Parameters[0];
            var constructed = new MimeParameter(ParameterName, EAcute);

            // act
            var result = parsed.Equals(constructed);

            // assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetHashCode_DifferentCharsets_ReturnsSameHashCode()
        {
            // arrange
            var parsed = MimeMediaType.Parse(Latin1EAcuteMediaType).Parameters[0];
            var constructed = new MimeParameter(ParameterName, EAcute);

            // act
            var result = (Parsed: parsed.GetHashCode(), Constructed: constructed.GetHashCode());

            // assert
            result.Parsed.Should().Be(result.Constructed);
        }

        [Fact]
        public void FormatSections_ParameterFits_ReturnsParameter()
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, EAcute);

            // act
            var result = parameter.FormatSections(EAcuteParameterLength);

            // assert
            result.Should().Equal(EAcuteParameter);
        }

        [Fact]
        public void FormatSections_LongUsAsciiValue_SplitsIntoPlainSections()
        {
            // arrange
            const int maxSectionLength = 20;
            var parameter = new MimeParameter(FileNameParameterName, "a-very-long-file-name-that-does-not-fit.txt");

            // act
            var result = parameter.FormatSections(maxSectionLength);

            // assert
            result.Should().Equal("filename*0=a-very-lo", "filename*1=ng-file-n", "filename*2=ame-that-", "filename*3=does-not-",
                "filename*4=fit.txt");
        }

        [Fact]
        public void FormatSections_ValueThatNeedsQuoting_QuotesEachSectionThatIsNotAToken()
        {
            // arrange
            const int maxSectionLength = 18;
            var parameter = new MimeParameter(TitleParameterName, "two words \"quoted\" and-a-token");

            // act
            var result = parameter.FormatSections(maxSectionLength);

            // assert
            result.Should().Equal("title*0=\"two word\"", "title*1=\"s \\\"quot\"", "title*2=\"ed\\\" and\"", "title*3=-a-token");
        }

        [Fact]
        public void FormatSections_LongExtendedValue_SplitsIntoExtendedSectionsWithoutSplittingCharacters()
        {
            // arrange
            const int maxSectionLength = 40;
            var parameter = new MimeParameter(FileNameParameterName,
                "\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC\x20AC", English);

            // act
            var result = parameter.FormatSections(maxSectionLength);

            // assert
            result.Should().Equal(
                "filename*0*=UTF-8'en'%E2%82%AC%E2%82%AC",
                "filename*1*=%E2%82%AC%E2%82%AC%E2%82%AC",
                "filename*2*=%E2%82%AC%E2%82%AC%E2%82%AC",
                "filename*3*=%E2%82%AC%E2%82%AC%E2%82%AC",
                "filename*4*=%E2%82%AC%E2%82%AC%E2%82%AC",
                "filename*5*=%E2%82%AC%E2%82%AC%E2%82%AC",
                "filename*6*=%E2%82%AC%E2%82%AC%E2%82%AC");
        }

        [Fact]
        public void FormatSections_ValueOfSeveralCharactersInEachSection_SplitsAtCharacters()
        {
            // arrange
            const int maxSectionLength = 26;
            var parameter = new MimeParameter(ParameterName, JapaneseFileName, Japanese);

            // act
            var result = parameter.FormatSections(maxSectionLength);

            // assert
            result.Should().Equal(
                "name*0*=UTF-8'ja'%E6%97%A5",
                "name*1*=%E6%9C%AC%E8%AA%9E",
                "name*2*=%E3%81%AE%E3%83%95",
                "name*3*=%E3%82%A1%E3%82%A4",
                "name*4*=%E3%83%AB%E5%90%8D",
                "name*5*=.txt");
        }

        [Fact]
        public void Parse_SectionsOfSeveralCharacters_JoinsValue()
        {
            // arrange
            var expected = new MimeParameter(ParameterName, JapaneseFileName, Japanese);

            // act
            var result = MimeMediaType.Parse(
                "text/plain;name*0*=UTF-8'ja'%E6%97%A5;name*1*=%E6%9C%AC%E8%AA%9E;name*2*=%E3%81%AE%E3%83%95;name*3*=%E3%82%A1%E3%82%A4;name*4*=%E3%83%AB%E5%90%8D;name*5*=.txt");

            // assert
            result.Parameters[0].Should().Be(expected);
        }

        [Fact]
        public void FormatSections_ManySections_NumbersThemInOrder()
        {
            // arrange
            // Sections 0 to 9 hold three characters each, and the longer names of the sections after them leave room for two.
            const int maxSectionLength = 7;
            var parameter = new MimeParameter("a", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx");

            // act
            var result = parameter.FormatSections(maxSectionLength);

            // assert
            result.Should().Equal("a*0=xxx", "a*1=xxx", "a*2=xxx", "a*3=xxx", "a*4=xxx", "a*5=xxx", "a*6=xxx", "a*7=xxx", "a*8=xxx",
                "a*9=xxx", "a*10=xx", "a*11=xx", "a*12=xx", "a*13=xx", "a*14=xx");
        }

        [Fact]
        public void FormatSections_BudgetTooSmallForOneCharacter_WritesOneCharacterPerSection()
        {
            // arrange
            const int maxSectionLength = 1;
            var parameter = new MimeParameter(ParameterName, "\xE9\xD83D\xDE00", English);

            // act
            var result = parameter.FormatSections(maxSectionLength);

            // assert
            result.Should().Equal("name*0*=UTF-8'en'%C3%A9", "name*1*=%F0%9F%98%80");
        }

        [Fact]
        public void FormatSections_EmptyValueWithLanguage_WritesOneSection()
        {
            // arrange
            const int maxSectionLength = 5;
            var parameter = new MimeParameter(ParameterName, string.Empty, English);

            // act
            var result = parameter.FormatSections(maxSectionLength);

            // assert
            result.Should().Equal("name*0*=UTF-8'en'");
        }

        [Theory]
        [InlineData("text/plain;name*0*=UTF-8'en'%C3%A9;name*1*=%F0%9F%98%80", "\xE9\xD83D\xDE00")]
        [InlineData("text/plain;name*0*=UTF-8'en'", "")]
        public void Parse_SectionsWithLanguage_ReturnsParameter(string value, string parameterValue)
        {
            // arrange
            var expected = new MimeParameter(ParameterName, parameterValue, English);

            // act
            var result = MimeMediaType.Parse(value);

            // assert
            result.Parameters[0].Should().Be(expected);
        }

        // The shortest section length that FormatSections accepts.
        private const int MinimumSectionLength = 1;

        [Fact]
        public void FormatSections_EmptyValueThatDoesNotFit_ReturnsOneSection()
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, string.Empty);

            // act
            var result = parameter.FormatSections(MinimumSectionLength);

            // assert
            result.Should().Equal("name*0=\"\"");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void FormatSections_LengthLessThanOne_ThrowsArgumentOutOfRangeException(int maxSectionLength)
        {
            // arrange
            var parameter = new MimeParameter(ParameterName, ParameterValue);

            // act
            Action testAction = () => parameter.FormatSections(maxSectionLength);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        private static MimeCharsetResolver CreateResolver()
        {
            return new MimeCharsetResolver(new Utf8CharsetResolver());
        }

        private static (string Value, MimeMediaTypeParseOptions Option, string Expected)[] GetDeviations()
        {
            return new[]
            {
                // ref. RFC 2231 §3: sections are numbered from 0, with no gaps, each number once, and with no leading zero.
                ("text/plain;a*0=x;a*2=z", MimeMediaTypeParseOptions.AllowMissingSections, "xz"),
                ("text/plain;a*1=y", MimeMediaTypeParseOptions.AllowMissingSections, "y"),
                ("text/plain;a*1*=%41", MimeMediaTypeParseOptions.AllowMissingSections, "A"),
                // ref. RFC 2231 §3: section := "0" / [1-9] *DIGIT, with no limit on the number of digits.
                ("text/plain;a*1234567890=x", MimeMediaTypeParseOptions.AllowMissingSections, "x"),
                ("text/plain;a*0=x;a*100000000000000000000=z;a*99999999999999999999=y", MimeMediaTypeParseOptions.AllowMissingSections, "xyz"),
                ("text/plain;a*0=x;a*0=y", MimeMediaTypeParseOptions.AllowDuplicateSections, "x"),
                ("text/plain;a*1=z;a*0=x;a*1=y", MimeMediaTypeParseOptions.AllowDuplicateSections, "xz"),
                ("text/plain;a*00=x;a*01=y", MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers, "xy"),
                ("text/plain;a*01=y;a*0=x", MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers, "xy"),
                ("text/plain;a*0000000000001=y;a*0=x", MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers, "xy"),
                // ref. RFC 2231 §4: the character set must be known, and with none, the value is US-ASCII.
                ("text/plain;a*=x-no-such-charset''%E9t%E9", MimeMediaTypeParseOptions.AllowUnknownCharsets, "\xE9t\xE9"),
                ("text/plain;a*=x.y''abc", MimeMediaTypeParseOptions.AllowUnknownCharsets, "abc"),
                ("text/plain;a*=''%E9", MimeMediaTypeParseOptions.AllowUnknownCharsets, EAcute),
                // ref. RFC 2231 §4: the octets are text in the character set. RFC 3629 §3: 0xFF never appears in UTF-8, and 0xC3 starts a
                // two-octet sequence. RFC 2781 §2.2: in UTF-16, a high surrogate, 0xD800, is followed by a low surrogate.
                ("text/plain;a*=UTF-8''%FF", MimeMediaTypeParseOptions.AllowInvalidOctets, ReplacementCharacter),
                ("text/plain;a*=UTF-8''a%C3", MimeMediaTypeParseOptions.AllowInvalidOctets, ACharacterThenReplacementCharacter),
                ("text/plain;a*=UTF-16''%D8%00", MimeMediaTypeParseOptions.AllowInvalidOctets, ReplacementCharacter),
                // ref. RFC 2231 §7: an extended value is not a quoted-string.
                ("text/plain;a*=\"UTF-8''a%20b\"", MimeMediaTypeParseOptions.AllowQuotedExtendedValues, "a b"),
                ("text/plain;a*=\"UTF-8'en'a b;c\"", MimeMediaTypeParseOptions.AllowQuotedExtendedValues, "a b;c"),
                ("text/plain;a*0*=\"UTF-8''a%20\";a*1*=b", MimeMediaTypeParseOptions.AllowQuotedExtendedValues, "a b"),
                // ref. RFC 2231 §7: ext-octet := "%" 2(DIGIT / "A" / "B" / "C" / "D" / "E" / "F")
                ("text/plain;a*=utf-8''na%c3%afve.txt", MimeMediaTypeParseOptions.AllowLowercaseHexDigits, "na\xEFve.txt"),
                ("text/plain;a*=UTF-8''%C3%a9", MimeMediaTypeParseOptions.AllowLowercaseHexDigits, EAcute),
                ("text/plain;a*0*=UTF-8''x;a*1*=%c3%A9", MimeMediaTypeParseOptions.AllowLowercaseHexDigits, "x\xE9"),
            };
        }

        private static TheoryData<string> CreateDeviationValues()
        {
            var data = new TheoryData<string>();
            foreach (var deviation in GetDeviations())
            {
                data.Add(deviation.Value);
            }

            return data;
        }

        private static TheoryData<string, MimeMediaTypeParseOptions> CreateDeviationValuesWithOtherOptions()
        {
            var data = new TheoryData<string, MimeMediaTypeParseOptions>();
            foreach (var deviation in GetDeviations())
            {
                data.Add(deviation.Value, AllRfc2231Options & ~deviation.Option);
            }

            return data;
        }

        private static TheoryData<string, MimeMediaTypeParseOptions, string> CreateDeviations()
        {
            var data = new TheoryData<string, MimeMediaTypeParseOptions, string>();
            foreach (var deviation in GetDeviations())
            {
                data.Add(deviation.Value, deviation.Option, deviation.Expected);
            }

            return data;
        }

        // Resolves every character set as UTF-8 that throws on bytes that are not valid UTF-8.
        private sealed class ThrowingUtf8CharsetResolver : IMimeCharsetResolver
        {
            public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
            {
                encoding = new UTF8Encoding(false, true);
                return true;
            }
        }

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
