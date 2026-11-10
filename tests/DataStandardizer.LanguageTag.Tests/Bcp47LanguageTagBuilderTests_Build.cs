using System;
using System.Linq;
using DataStandardizer.Core;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.Tests
{
    public class Bcp47LanguageTagBuilderTests_Build
    {
        #region Declarations

        private const string DefaultPrimaryLanguageSubtag = "en";

        /// <summary>
        /// What each test case factory puts before the subtag under test, so that the expected language tag is <see cref="DefaultPrimaryLanguageSubtag"/> followed by the subtag.
        /// </summary>
        private const string DefaultPrimaryLanguagePrefix = DefaultPrimaryLanguageSubtag + "-";

        /// <summary>
        /// The format of a three-digit UN M49 code as a Region subtag (RFC 5646 §2.2.4¶4).
        /// </summary>
        private const string UnM49CodeFormat = "000";

        /// <summary>
        /// A Private Use subtag (RFC 5646 §2.2.7), from the RFC 5646 Appendix A example <c>zh-CN-a-myext-x-private</c>.
        /// </summary>
        private const string PrivateUseSubtag = "x-private";

        #endregion

        #region Test: Build_ForLanguageTag

        [Theory]
        [InlineData("en"), InlineData("es-419"), InlineData("rm-sursilv"), InlineData("sr-Cyrl"), InlineData("nan-Hant-TW"), InlineData("yue-Hant-HK"), InlineData("gsw-u-sd-chzh")]
        public void Build_ForLanguageTag_ProducesBcp47LanguageTag(string testValue)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingLanguageTag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(testValue);
        }

        #endregion

        #region Test: Build_ForPrimaryLanguageSubtag

        [Theory]
        [InlineData("af"), InlineData("am"), InlineData("ar"), InlineData("arn"), InlineData("ary"), InlineData("as"), InlineData("az"), InlineData("ba"), InlineData("be"), InlineData("bg"), InlineData("bn"),
         InlineData("bo"), InlineData("br"), InlineData("bs"), InlineData("ca"), InlineData("ckb"), InlineData("co"), InlineData("cs"), InlineData("cy"), InlineData("da"), InlineData("de"), InlineData("dsb"),
         InlineData("dv"), InlineData("el"), InlineData("en"), InlineData("es"), InlineData("et"), InlineData("eu"), InlineData("fa"), InlineData("fi"), InlineData("fil"), InlineData("fo"), InlineData("fr"),
         InlineData("fy"), InlineData("ga"), InlineData("gd"), InlineData("gil"), InlineData("gl"), InlineData("gsw"), InlineData("gu"), InlineData("ha"), InlineData("he"), InlineData("hi"), InlineData("hr"),
         InlineData("hsb"), InlineData("hu"), InlineData("hy"), InlineData("id"), InlineData("ig"), InlineData("ii"), InlineData("is"), InlineData("it"), InlineData("iu"), InlineData("ja"), InlineData("ka"),
         InlineData("kk"), InlineData("kl"), InlineData("km"), InlineData("kn"), InlineData("ko"), InlineData("kok"), InlineData("ku"), InlineData("ky"), InlineData("lb"), InlineData("lo"), InlineData("lt"),
         InlineData("lv"), InlineData("mi"), InlineData("mk"), InlineData("ml"), InlineData("mn"), InlineData("moh"), InlineData("mr"), InlineData("ms"), InlineData("mt"), InlineData("my"), InlineData("nb"),
         InlineData("ne"), InlineData("nl"), InlineData("nn"), InlineData("no"), InlineData("oc"), InlineData("or"), InlineData("pap"), InlineData("pa"), InlineData("pl"), InlineData("prs"), InlineData("ps"),
         InlineData("pt"), InlineData("quc"), InlineData("qu"), InlineData("rm"), InlineData("ro"), InlineData("ru"), InlineData("rw"), InlineData("sa"), InlineData("sah"), InlineData("se"), InlineData("si"),
         InlineData("sk"), InlineData("sl"), InlineData("sma"), InlineData("smj"), InlineData("smn"), InlineData("sms"), InlineData("sq"), InlineData("sr"), InlineData("st"), InlineData("sv"), InlineData("sw"),
         InlineData("syc"), InlineData("ta"), InlineData("te"), InlineData("tg"), InlineData("th"), InlineData("tk"), InlineData("tn"), InlineData("tr"), InlineData("tt"), InlineData("tzm"), InlineData("ug"),
         InlineData("uk"), InlineData("ur"), InlineData("uz"), InlineData("vi"), InlineData("wo"), InlineData("xh"), InlineData("yo"), InlineData("zh"), InlineData("zu")]
        public void Build_ForPrimaryLanguageSubtag_ProducesBcp47LanguageTag(string testValue)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(testValue, "a language tag with only a Primary Language subtag is that subtag");
        }

        #endregion

        #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part1

        /// <summary>
        /// Every ISO 639-1 code, with its name, which is the language tag that has only that code as its Primary Language subtag (RFC 5646 §2.2.1¶2).
        /// </summary>
        public static TheoryData<Iso639Part1Language, string> Iso639Part1Codes
        {
            get
            {
                var data = new TheoryData<Iso639Part1Language, string>();
                foreach (var testValue in StringEnum.GetValues<Iso639Part1Language>())
                {
                    data.Add(testValue, StringEnum.GetName(testValue));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Iso639Part1Codes), DisableDiscoveryEnumeration = true)]
        public void Build_ForPrimaryLanguageSubtagFromIso639Part1_ProducesBcp47LanguageTag(Iso639Part1Language testValue, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part2T

        /// <summary>
        /// Every ISO 639-2/T code, with its name, which is the language tag that has only that code as its Primary Language subtag (RFC 5646 §2.2.1¶2).
        /// </summary>
        public static TheoryData<Iso639Part2TLanguage, string> Iso639Part2TCodes
        {
            get
            {
                var data = new TheoryData<Iso639Part2TLanguage, string>();
                foreach (var testValue in StringEnum.GetValues<Iso639Part2TLanguage>())
                {
                    data.Add(testValue, StringEnum.GetName(testValue));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Iso639Part2TCodes), DisableDiscoveryEnumeration = true)]
        public void Build_ForPrimaryLanguageSubtagFromIso639Part2T_ProducesBcp47LanguageTag(Iso639Part2TLanguage testValue, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part3

        /// <summary>
        /// Every ISO 639-3 code, with its name, which is the language tag that has only that code as its Primary Language subtag (RFC 5646 §2.2.1¶2).
        /// </summary>
        public static TheoryData<Iso639Part3Language, string> Iso639Part3Codes
        {
            get
            {
                var data = new TheoryData<Iso639Part3Language, string>();
                foreach (var testValue in StringEnum.GetValues<Iso639Part3Language>())
                {
                    data.Add(testValue, StringEnum.GetName(testValue));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Iso639Part3Codes), DisableDiscoveryEnumeration = true)]
        public void Build_ForPrimaryLanguageSubtagFromIso639Part3_ProducesBcp47LanguageTag(Iso639Part3Language testValue, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part5

        /// <summary>
        /// Every ISO 639-5 code, with its name, which is the language tag that has only that code as its Primary Language subtag (RFC 5646 §2.2.1¶2).
        /// </summary>
        public static TheoryData<Iso639Part5LanguageFamily, string> Iso639Part5Codes
        {
            get
            {
                var data = new TheoryData<Iso639Part5LanguageFamily, string>();
                foreach (var testValue in StringEnum.GetValues<Iso639Part5LanguageFamily>())
                {
                    data.Add(testValue, StringEnum.GetName(testValue));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Iso639Part5Codes), DisableDiscoveryEnumeration = true)]
        public void Build_ForPrimaryLanguageSubtagFromIso639Part5_ProducesBcp47LanguageTag(Iso639Part5LanguageFamily testValue, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForExtendedLanguageSubtags

        [Fact]
        public void Build_ForExtendedLanguageSubtags_ProducesBcp47LanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag).UsingExtendedLanguageSubtags("aaa");

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("en-aaa");
        }

        #endregion

        #region Test: Build_ForScriptSubtag

        [Fact]
        public void Build_ForScriptSubtag_ProducesBcp47LanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(Iso639Part1Language.zh).UsingScriptSubtag("Hans");

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("zh-Hans");
        }

        #endregion

        #region Test: Build_ForScriptSubtagFromIso15924

        /// <summary>
        /// Every ISO 15924 code, with the language tag that has it as its Script subtag (RFC 5646 §2.2.3¶2).
        /// </summary>
        public static TheoryData<Iso15924Script, string> Iso15924Codes
        {
            get
            {
                var data = new TheoryData<Iso15924Script, string>();
                foreach (var testValue in Enum.GetValues(typeof(Iso15924Script)).Cast<Iso15924Script>())
                {
                    data.Add(testValue, DefaultPrimaryLanguagePrefix + testValue);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Iso15924Codes))]
        public void Build_ForScriptSubtagFromIso15924_ProducesBcp47LanguageTag(Iso15924Script testValue, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag).UsingScriptSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForRegionSubtag

        [Theory]
        [InlineData("fr", "CA", "fr-CA"), InlineData("es", "419", "es-419")]
        public void Build_ForRegionSubtag_ProducesBcp47LanguageTag(string primaryLanguageSubtag, string regionSubtag, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(primaryLanguageSubtag).UsingRegionSubtag(regionSubtag);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForRegionSubtagFromIso3166Part1Alpha2

        /// <summary>
        /// Every ISO 3166-1 alpha-2 code, with the language tag that has it as its Region subtag (RFC 5646 §2.2.4¶4A).
        /// </summary>
        public static TheoryData<Iso3166Part1Alpha2Country, string> Iso3166Part1Alpha2Codes
        {
            get
            {
                var data = new TheoryData<Iso3166Part1Alpha2Country, string>();
                foreach (var testValue in Enum.GetValues(typeof(Iso3166Part1Alpha2Country)).Cast<Iso3166Part1Alpha2Country>())
                {
                    data.Add(testValue, DefaultPrimaryLanguagePrefix + testValue);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(Iso3166Part1Alpha2Codes))]
        public void Build_ForRegionSubtagFromIso3166Part1Alpha2_ProducesBcp47LanguageTag(Iso3166Part1Alpha2Country testValue, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag).UsingRegionSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForRegionSubtagFromUnM49Area

        /// <summary>
        /// Every supranational UN M49 area, with the language tag that has its code as its Region subtag (RFC 5646 §2.2.4¶4B).
        /// </summary>
        public static TheoryData<UnM49Area, string> SupranationalUnM49Areas
        {
            get
            {
                var data = new TheoryData<UnM49Area, string>();
                foreach (var testValue in Enum.GetValues(typeof(UnM49Area)).Cast<UnM49Area>().Where(code => code.GetLevel() != UnM49AreaLevel.CountryOrArea))
                {
                    data.Add(testValue, DefaultPrimaryLanguagePrefix + ((ushort)testValue).ToString(UnM49CodeFormat));
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(SupranationalUnM49Areas))]
        public void Build_ForRegionSubtagFromUnM49Area_ProducesBcp47LanguageTag(UnM49Area testValue, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag).UsingRegionSubtag(testValue);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForVariantSubtag

        /// <summary>
        /// Every variant subtag in the IANA Language Subtag Registry fixture (RFC 5646 §2.2.5, §3.1), with the language tag that has it after
        /// <see cref="DefaultPrimaryLanguageSubtag"/>.
        /// </summary>
        public static TheoryData<string, string> RegisteredVariantSubtags
        {
            get
            {
                var variantSubtags = new[]
                {
                    "1606nict", "1694acad", "1901", "1959acad", "1994", "1996", "abl1943", "akuapem", "alalc97", "aluku", "anpezo", "ao1990", "aranes", "arevela", "arevmda", "arkaika", "asante", "auvern", "baku1926", "balanka", "barla", "basiceng",
                    "bauddha",
                    "bciav", "bcizbl", "biscayan", "biske", "blasl", "bohoric", "boont", "bornholm", "cisaup", "colb1945", "cornu", "creiss", "dajnko", "ekavsk", "emodeng", "fascia", "fodom", "fonipa", "fonkirsh", "fonnapa", "fonupa", "fonxsamp", "gallo",
                    "gascon", "gherd", "grclass", "grital", "grmistr", "hanoi", "hepburn", "heploc", "hognorsk", "hsistemo", "huett", "ijekavsk", "itihasa", "ivanchov", "jauer", "jyutping", "kkcor", "kleinsch", "kociewie", "kscor", "laukika", "leidentr",
                    "lemosin", "lengadoc", "lipaw", "ltg1929", "ltg2007", "luna1918", "mdcegyp", "mdctrans", "metelko", "monoton", "ndyuka", "nedis", "newfound", "nicard", "njiva", "nulik", "osojs", "oxendict", "pahawh2", "pahawh3", "pahawh4", "pamaka",
                    "peano", "pehoeji", "petr1708", "pinyin", "polyton", "provenc", "puter", "rigik", "rozaj", "rumgr", "saigon", "scotland", "scouse", "simple", "solba", "sotav", "spanglis", "surmiran", "sursilv", "sutsilv", "synnejyl", "tailo",
                    "tarask",
                    "tongyong", "tunumiit", "uccor", "ucrcor", "ulster", "unifon", "vaidika", "valbadia", "valencia", "vallader", "vecdruka", "vivaraup", "wadegile", "xsistemo"
                };
                var data = new TheoryData<string, string>();
                foreach (var variantSubtag in variantSubtags)
                {
                    data.Add(variantSubtag, DefaultPrimaryLanguagePrefix + variantSubtag);
                }

                return data;
            }
        }

        [Theory]
        [MemberData(nameof(RegisteredVariantSubtags))]
        public void Build_ForVariantSubtag_ProducesBcp47LanguageTag(string variantSubtag, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(Iso639Part1Language.en).UsingVariantSubtags(variantSubtag);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        #endregion

        #region Test: Build_ForExtensionSubtag

        [Theory]
        [InlineData("en", "t-jp", "en-t-jp"), InlineData("gsw", "u-sd-chzh", "gsw-u-sd-chzh"), InlineData("ar", "u-nu-latn", "ar-u-nu-latn")]
        public void Build_ForExtensionSubtag_ProducesBcp47LanguageTag(string primaryLanguageSubtag, string extensionSubtag, string expectedResult)
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(primaryLanguageSubtag).UsingExtensionSubtags(extensionSubtag);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Fact]
        public void Build_ForExtensionSubtagAfterRegionSubtag_ProducesBcp47LanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag("he").UsingRegionSubtag("IL").UsingExtensionSubtags("u-ca-hebrew-tz-jeruslm");

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("he-IL-u-ca-hebrew-tz-jeruslm");
        }

        #endregion

        #region Test: Build_ForPrivateUseSubtag

        [Fact]
        public void Build_ForPrivateUseSubtag_ProducesBcp47LanguageTag()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag).UsingPrivateUseSubtag("x-private-use-subtag");

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("en-x-private-use-subtag");
        }

        #endregion

        #region Test: Build_ForEverySubtag

        [Fact]
        public void Build_ForEverySubtagWithUnM49Region_ProducesLanguageTagString()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder()
                .UsingPrimaryLanguageSubtag(Iso639Part1Language.zh)
                .UsingExtendedLanguageSubtags("yue")
                .UsingScriptSubtag(Iso15924Script.Hant)
                .UsingRegionSubtag(UnM49Area._005)
                .UsingVariantSubtags("rozaj", "biske")
                .UsingExtensionSubtags("a-bbb", "u-ca-hebrew")
                .UsingPrivateUseSubtag(PrivateUseSubtag);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("zh-yue-Hant-005-rozaj-biske-a-bbb-u-ca-hebrew-x-private");
        }

        [Fact]
        public void Build_ForEverySubtagWithIso3166Region_ProducesLanguageTagString()
        {
            // arrange
            var builder = new Bcp47LanguageTagBuilder()
                .UsingPrimaryLanguageSubtag("zh")
                .UsingExtendedLanguageSubtags("yue")
                .UsingScriptSubtag("Hant")
                .UsingRegionSubtag(Iso3166Part1Alpha2Country.HK)
                .UsingVariantSubtags("1994")
                .UsingExtensionSubtags("u-nu-hanidec")
                .UsingPrivateUseSubtag(PrivateUseSubtag);

            // act
            string testResult = builder.Build();

            // assert
            testResult.Should().Be("zh-yue-Hant-HK-1994-u-nu-hanidec-x-private");
        }

        #endregion
    }
}
