using System.Collections;
using DataStandardizer.Core;
using DataStandardizer.ISO15924;
using DataStandardizer.ISO3166;
using DataStandardizer.ISO639;
using DataStandardizer.UNM49;
using FluentAssertions;

namespace DataStandardizer.BCP47.Tests;

public class Bcp47LanguageTagBuilderTests_Build
{
    private const string DefaultPrimaryLanguageSubtag = "en";
    
    #region Test: Build_ForLanguageTag

    [Theory]
    [MemberData(nameof(Build_ForLanguageTag_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForLanguageTag_TestCaseGenerator))]
    public void Build_ForLanguageTag_ProducesBcp47LanguageTag(string testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder();

        // act
        var testResult = builder.UsingLanguageTag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForLanguageTag_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { "en", Bcp47LanguageTag.Create("en") };
                yield return new object[] { "es-419", Bcp47LanguageTag.Create("es-419") };
                yield return new object[] { "rm-sursilv", Bcp47LanguageTag.Create("rm-sursilv") };
                yield return new object[] { "sr-Cyrl", Bcp47LanguageTag.Create("sr-Cyrl") };
                yield return new object[] { "nan-Hant-TW", Bcp47LanguageTag.Create("nan-Hant-TW") };
                yield return new object[] { "yue-Hant-HK", Bcp47LanguageTag.Create("yue-Hant-HK") };
                yield return new object[] { "gsw-u-sd-chzh", Bcp47LanguageTag.Create("gsw-u-sd-chzh") };
            }
        }
    }

    #endregion

    #region Test: Build_ForPrimaryLanguageSubtag

    [Theory]
    [MemberData(nameof(Build_ForPrimaryLanguageSubtag_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForPrimaryLanguageSubtag_TestCaseGenerator))]
    public void Build_ForPrimaryLanguageSubtag_ProducesBcp47LanguageTag(string testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder();

        // act
        var testResult = builder.UsingPrimaryLanguageSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForPrimaryLanguageSubtag_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { "af", Bcp47LanguageTag.Create("af") };
                yield return new object[] { "am", Bcp47LanguageTag.Create("am") };
                yield return new object[] { "ar", Bcp47LanguageTag.Create("ar") };
                yield return new object[] { "arn", Bcp47LanguageTag.Create("arn") };
                yield return new object[] { "ary", Bcp47LanguageTag.Create("ary") };
                yield return new object[] { "as", Bcp47LanguageTag.Create("as") };
                yield return new object[] { "az", Bcp47LanguageTag.Create("az") };
                yield return new object[] { "ba", Bcp47LanguageTag.Create("ba") };
                yield return new object[] { "be", Bcp47LanguageTag.Create("be") };
                yield return new object[] { "bg", Bcp47LanguageTag.Create("bg") };
                yield return new object[] { "bn", Bcp47LanguageTag.Create("bn") };
                yield return new object[] { "bo", Bcp47LanguageTag.Create("bo") };
                yield return new object[] { "br", Bcp47LanguageTag.Create("br") };
                yield return new object[] { "bs", Bcp47LanguageTag.Create("bs") };
                yield return new object[] { "ca", Bcp47LanguageTag.Create("ca") };
                yield return new object[] { "ckb", Bcp47LanguageTag.Create("ckb") };
                yield return new object[] { "co", Bcp47LanguageTag.Create("co") };
                yield return new object[] { "cs", Bcp47LanguageTag.Create("cs") };
                yield return new object[] { "cy", Bcp47LanguageTag.Create("cy") };
                yield return new object[] { "da", Bcp47LanguageTag.Create("da") };
                yield return new object[] { "de", Bcp47LanguageTag.Create("de") };
                yield return new object[] { "dsb", Bcp47LanguageTag.Create("dsb") };
                yield return new object[] { "dv", Bcp47LanguageTag.Create("dv") };
                yield return new object[] { "el", Bcp47LanguageTag.Create("el") };
                yield return new object[] { "en", Bcp47LanguageTag.Create("en") };
                yield return new object[] { "es", Bcp47LanguageTag.Create("es") };
                yield return new object[] { "et", Bcp47LanguageTag.Create("et") };
                yield return new object[] { "eu", Bcp47LanguageTag.Create("eu") };
                yield return new object[] { "fa", Bcp47LanguageTag.Create("fa") };
                yield return new object[] { "fi", Bcp47LanguageTag.Create("fi") };
                yield return new object[] { "fil", Bcp47LanguageTag.Create("fil") };
                yield return new object[] { "fo", Bcp47LanguageTag.Create("fo") };
                yield return new object[] { "fr", Bcp47LanguageTag.Create("fr") };
                yield return new object[] { "fy", Bcp47LanguageTag.Create("fy") };
                yield return new object[] { "ga", Bcp47LanguageTag.Create("ga") };
                yield return new object[] { "gd", Bcp47LanguageTag.Create("gd") };
                yield return new object[] { "gil", Bcp47LanguageTag.Create("gil") };
                yield return new object[] { "gl", Bcp47LanguageTag.Create("gl") };
                yield return new object[] { "gsw", Bcp47LanguageTag.Create("gsw") };
                yield return new object[] { "gu", Bcp47LanguageTag.Create("gu") };
                yield return new object[] { "ha", Bcp47LanguageTag.Create("ha") };
                yield return new object[] { "he", Bcp47LanguageTag.Create("he") };
                yield return new object[] { "hi", Bcp47LanguageTag.Create("hi") };
                yield return new object[] { "hr", Bcp47LanguageTag.Create("hr") };
                yield return new object[] { "hsb", Bcp47LanguageTag.Create("hsb") };
                yield return new object[] { "hu", Bcp47LanguageTag.Create("hu") };
                yield return new object[] { "hy", Bcp47LanguageTag.Create("hy") };
                yield return new object[] { "id", Bcp47LanguageTag.Create("id") };
                yield return new object[] { "ig", Bcp47LanguageTag.Create("ig") };
                yield return new object[] { "ii", Bcp47LanguageTag.Create("ii") };
                yield return new object[] { "is", Bcp47LanguageTag.Create("is") };
                yield return new object[] { "it", Bcp47LanguageTag.Create("it") };
                yield return new object[] { "iu", Bcp47LanguageTag.Create("iu") };
                yield return new object[] { "ja", Bcp47LanguageTag.Create("ja") };
                yield return new object[] { "ka", Bcp47LanguageTag.Create("ka") };
                yield return new object[] { "kk", Bcp47LanguageTag.Create("kk") };
                yield return new object[] { "kl", Bcp47LanguageTag.Create("kl") };
                yield return new object[] { "km", Bcp47LanguageTag.Create("km") };
                yield return new object[] { "kn", Bcp47LanguageTag.Create("kn") };
                yield return new object[] { "ko", Bcp47LanguageTag.Create("ko") };
                yield return new object[] { "kok", Bcp47LanguageTag.Create("kok") };
                yield return new object[] { "ku", Bcp47LanguageTag.Create("ku") };
                yield return new object[] { "ky", Bcp47LanguageTag.Create("ky") };
                yield return new object[] { "lb", Bcp47LanguageTag.Create("lb") };
                yield return new object[] { "lo", Bcp47LanguageTag.Create("lo") };
                yield return new object[] { "lt", Bcp47LanguageTag.Create("lt") };
                yield return new object[] { "lv", Bcp47LanguageTag.Create("lv") };
                yield return new object[] { "mi", Bcp47LanguageTag.Create("mi") };
                yield return new object[] { "mk", Bcp47LanguageTag.Create("mk") };
                yield return new object[] { "ml", Bcp47LanguageTag.Create("ml") };
                yield return new object[] { "mn", Bcp47LanguageTag.Create("mn") };
                yield return new object[] { "moh", Bcp47LanguageTag.Create("moh") };
                yield return new object[] { "mr", Bcp47LanguageTag.Create("mr") };
                yield return new object[] { "ms", Bcp47LanguageTag.Create("ms") };
                yield return new object[] { "mt", Bcp47LanguageTag.Create("mt") };
                yield return new object[] { "my", Bcp47LanguageTag.Create("my") };
                yield return new object[] { "nb", Bcp47LanguageTag.Create("nb") };
                yield return new object[] { "ne", Bcp47LanguageTag.Create("ne") };
                yield return new object[] { "nl", Bcp47LanguageTag.Create("nl") };
                yield return new object[] { "nn", Bcp47LanguageTag.Create("nn") };
                yield return new object[] { "no", Bcp47LanguageTag.Create("no") };
                yield return new object[] { "oc", Bcp47LanguageTag.Create("oc") };
                yield return new object[] { "or", Bcp47LanguageTag.Create("or") };
                yield return new object[] { "pap", Bcp47LanguageTag.Create("pap") };
                yield return new object[] { "pa", Bcp47LanguageTag.Create("pa") };
                yield return new object[] { "pl", Bcp47LanguageTag.Create("pl") };
                yield return new object[] { "prs", Bcp47LanguageTag.Create("prs") };
                yield return new object[] { "ps", Bcp47LanguageTag.Create("ps") };
                yield return new object[] { "pt", Bcp47LanguageTag.Create("pt") };
                yield return new object[] { "quc", Bcp47LanguageTag.Create("quc") };
                yield return new object[] { "qu", Bcp47LanguageTag.Create("qu") };
                yield return new object[] { "rm", Bcp47LanguageTag.Create("rm") };
                yield return new object[] { "ro", Bcp47LanguageTag.Create("ro") };
                yield return new object[] { "ru", Bcp47LanguageTag.Create("ru") };
                yield return new object[] { "rw", Bcp47LanguageTag.Create("rw") };
                yield return new object[] { "sa", Bcp47LanguageTag.Create("sa") };
                yield return new object[] { "sah", Bcp47LanguageTag.Create("sah") };
                yield return new object[] { "se", Bcp47LanguageTag.Create("se") };
                yield return new object[] { "si", Bcp47LanguageTag.Create("si") };
                yield return new object[] { "sk", Bcp47LanguageTag.Create("sk") };
                yield return new object[] { "sl", Bcp47LanguageTag.Create("sl") };
                yield return new object[] { "sma", Bcp47LanguageTag.Create("sma") };
                yield return new object[] { "smj", Bcp47LanguageTag.Create("smj") };
                yield return new object[] { "smn", Bcp47LanguageTag.Create("smn") };
                yield return new object[] { "sms", Bcp47LanguageTag.Create("sms") };
                yield return new object[] { "sq", Bcp47LanguageTag.Create("sq") };
                yield return new object[] { "sr", Bcp47LanguageTag.Create("sr") };
                yield return new object[] { "st", Bcp47LanguageTag.Create("st") };
                yield return new object[] { "sv", Bcp47LanguageTag.Create("sv") };
                yield return new object[] { "sw", Bcp47LanguageTag.Create("sw") };
                yield return new object[] { "syc", Bcp47LanguageTag.Create("syc") };
                yield return new object[] { "ta", Bcp47LanguageTag.Create("ta") };
                yield return new object[] { "te", Bcp47LanguageTag.Create("te") };
                yield return new object[] { "tg", Bcp47LanguageTag.Create("tg") };
                yield return new object[] { "th", Bcp47LanguageTag.Create("th") };
                yield return new object[] { "tk", Bcp47LanguageTag.Create("tk") };
                yield return new object[] { "tn", Bcp47LanguageTag.Create("tn") };
                yield return new object[] { "tr", Bcp47LanguageTag.Create("tr") };
                yield return new object[] { "tt", Bcp47LanguageTag.Create("tt") };
                yield return new object[] { "tzm", Bcp47LanguageTag.Create("tzm") };
                yield return new object[] { "ug", Bcp47LanguageTag.Create("ug") };
                yield return new object[] { "uk", Bcp47LanguageTag.Create("uk") };
                yield return new object[] { "ur", Bcp47LanguageTag.Create("ur") };
                yield return new object[] { "uz", Bcp47LanguageTag.Create("uz") };
                yield return new object[] { "vi", Bcp47LanguageTag.Create("vi") };
                yield return new object[] { "wo", Bcp47LanguageTag.Create("wo") };
                yield return new object[] { "xh", Bcp47LanguageTag.Create("xh") };
                yield return new object[] { "yo", Bcp47LanguageTag.Create("yo") };
                yield return new object[] { "zh", Bcp47LanguageTag.Create("zh") };
                yield return new object[] { "zu", Bcp47LanguageTag.Create("zu") };
            }
        }
    }

    #endregion

    #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part1

    [Theory]
    [MemberData(nameof(Build_ForPrimaryLanguageSubtagFromIso639Part1_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForPrimaryLanguageSubtagFromIso639Part1_TestCaseGenerator))]
    public void Build_ForPrimaryLanguageSubtagFromIso639Part1_ProducesBcp47LanguageTag(Iso639Part1 testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder();

        // act
        var testResult = builder.UsingPrimaryLanguageSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForPrimaryLanguageSubtagFromIso639Part1_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var testValue in StringEnum.GetValues<Iso639Part1>())
                {
                    var languageTag = StringEnum.GetName(testValue);
                    yield return new object[] { testValue, Bcp47LanguageTag.Create(languageTag!) };
                }
            }
        }
    }

    #endregion

    #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part2T

    [Theory]
    [MemberData(nameof(Build_ForPrimaryLanguageSubtagFromIso639Part2T_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForPrimaryLanguageSubtagFromIso639Part2T_TestCaseGenerator))]
    public void Build_ForPrimaryLanguageSubtagFromIso639Part2T_ProducesBcp47LanguageTag(Iso639Part2T testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder();

        // act
        var testResult = builder.UsingPrimaryLanguageSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForPrimaryLanguageSubtagFromIso639Part2T_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var testValue in StringEnum.GetValues<Iso639Part2T>())
                {
                    var languageTag = StringEnum.GetName(testValue);
                    yield return new object[] { testValue, Bcp47LanguageTag.Create(languageTag!) };
                }
            }
        }
    }

    #endregion

    #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part3

    [Theory]
    [ClassData(typeof(Build_ForPrimaryLanguageSubtagFromIso639Part3_TestCaseFactory))]
    public void Build_ForPrimaryLanguageSubtagFromIso639Part3_ProducesBcp47LanguageTag(Iso639Part3 testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder();

        // act
        var testResult = builder.UsingPrimaryLanguageSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForPrimaryLanguageSubtagFromIso639Part3_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> TestCases;

        static Build_ForPrimaryLanguageSubtagFromIso639Part3_TestCaseFactory()
        {
            TestCases = typeof(Iso639Part3)
                .GetFields()
                .Where(field => field.IsStatic && field.FieldType == typeof(Iso639Part3))
                .Select(field => new object[] { field.GetValue(null)!, Bcp47LanguageTag.Create(field.Name) })
                .AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return TestCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)TestCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: Build_ForPrimaryLanguageSubtagFromIso639Part5

    [Theory]
    [MemberData(nameof(Build_ForPrimaryLanguageSubtagFromIso639Part5_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForPrimaryLanguageSubtagFromIso639Part5_TestCaseGenerator))]
    public void Build_ForPrimaryLanguageSubtagFromIso639Part5_ProducesBcp47LanguageTag(Iso639Part5 testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder();

        // act
        var testResult = builder.UsingPrimaryLanguageSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForPrimaryLanguageSubtagFromIso639Part5_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var testValue in StringEnum.GetValues<Iso639Part5>())
                {
                    var languageTag = StringEnum.GetName(testValue);
                    yield return new object[] { testValue, Bcp47LanguageTag.Create(languageTag!) };
                }
            }
        }
    }

    #endregion

    #region Test: Build_ForExtendedLanguageSubtags

    [Fact]
    public void Build_ForExtendedLanguageSubtags_ProducesBcp47LanguageTag()
    {
        // arrange
        const string testValue = "aaa";
        var expectedResult = Bcp47LanguageTag.Create($"{Iso639Part1.en}-{testValue}");

        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        var testResult = builder.UsingExtendedLanguageSubtags(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion

    #region Test: Build_ForScriptSubtag

    [Fact]
    public void Build_ForScriptSubtag_ProducesBcp47LanguageTag()
    {
        // arrange
        var expectedResult = Bcp47LanguageTag.Create("zh-Hans");
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(Iso639Part1.zh);

        // act
        var testResult = builder.UsingScriptSubtag("Hans").Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion

    #region Test: Build_ForScriptSubtagFromIso15924

    [Theory]
    [MemberData(nameof(Build_ForScriptSubtagFromIso15924_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForScriptSubtagFromIso15924_TestCaseGenerator))]
    public void Build_ForScriptSubtagFromIso15924_ProducesBcp47LanguageTag(Iso15924 testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        var testResult = builder.UsingScriptSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForScriptSubtagFromIso15924_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var testValue in Enum.GetValues<Iso15924>())
                {
                    yield return new object[] { testValue, Bcp47LanguageTag.Create($"{Iso639Part1.en}-{testValue}") };
                }
            }
        }
    }

    #endregion

    #region Test: Build_ForRegionSubtag

    [Theory]
    [MemberData(nameof(Build_ForRegionSubtag_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForRegionSubtag_TestCaseGenerator))]
    public void Build_ForRegionSubtag_ProducesBcp47LanguageTag(string primaryLanguageSubtag, string regionSubtag, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(primaryLanguageSubtag);

        // act
        var testResult = builder.UsingRegionSubtag(regionSubtag).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForRegionSubtag_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { "fr", "CA", Bcp47LanguageTag.Create("fr-CA") };
                yield return new object[] { "es", "419", Bcp47LanguageTag.Create("es-419") };
            }
        }
    }

    #endregion

    #region Test: Build_ForRegionSubtagFromIso3166Part1Alpha2

    [Theory]
    [MemberData(nameof(Build_ForRegionSubtagFromIso3166Part1Alpha2_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForRegionSubtagFromIso3166Part1Alpha2_TestCaseGenerator))]
    public void Build_ForRegionSubtagFromIso3166Part1Alpha2_ProducesBcp47LanguageTag(Iso3166Part1Alpha2 testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        var testResult = builder.UsingRegionSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForRegionSubtagFromIso3166Part1Alpha2_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var testValue in Enum.GetValues<Iso3166Part1Alpha2>())
                {
                    yield return new object[] { testValue, Bcp47LanguageTag.Create($"{Iso639Part1.en}-{testValue}") };
                }
            }
        }
    }

    #endregion

    #region Test: Build_ForRegionSubtagFromUnM49ByAlpha2Code

    [Theory]
    [ClassData(typeof(Build_ForRegionSubtagFromUnM49ByAlpha2Code_TestCaseFactory))]
    public void Build_ForRegionSubtagFromUnM49ByAlpha2Code_ProducesBcp47LanguageTag(UnM49ByAlpha2Code testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        var testResult = builder.UsingRegionSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForRegionSubtagFromUnM49ByAlpha2Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> TestCases;

        static Build_ForRegionSubtagFromUnM49ByAlpha2Code_TestCaseFactory()
        {
            var globalCodes = Enum.GetValues<UnM49ByAlpha2Code>().Select(code => code.GetGlobalCode()).Where(code => code is not null).Cast<ushort>();
            var regionCodes = Enum.GetValues<UnM49ByAlpha2Code>().Select(code => code.GetRegionCode()).Where(code => code is not null).Cast<ushort>();
            var subRegionCodes = Enum.GetValues<UnM49ByAlpha2Code>().Select(code => code.GetSubRegionCode()).Where(code => code is not null).Cast<ushort>();
            var intermediateRegionCodes = Enum.GetValues<UnM49ByAlpha2Code>().Select(code => code.GetIntermediateRegionCode()).Where(code => code is not null).Cast<ushort>();
            TestCases = globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCodes).Select(code => new object[] { code, Bcp47LanguageTag.Create($"{Iso639Part1.en}-{code:000}") }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return TestCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)TestCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: Build_ForRegionSubtagFromUnM49ByAlpha3Code

    [Theory]
    [ClassData(typeof(Build_ForRegionSubtagFromUnM49ByAlpha3Code_TestCaseFactory))]
    public void Build_ForRegionSubtagFromUnM49ByAlpha3Code_ProducesBcp47LanguageTag(UnM49ByAlpha3Code testValue, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        var testResult = builder.UsingRegionSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForRegionSubtagFromUnM49ByAlpha3Code_TestCaseFactory : IEnumerable<object[]>
    {
        private static readonly IEnumerable<object[]> TestCases;

        static Build_ForRegionSubtagFromUnM49ByAlpha3Code_TestCaseFactory()
        {
            var globalCodes = Enum.GetValues<UnM49ByAlpha3Code>().Select(code => code.GetGlobalCode()).Where(code => code is not null).Cast<ushort>();
            var regionCodes = Enum.GetValues<UnM49ByAlpha3Code>().Select(code => code.GetRegionCode()).Where(code => code is not null).Cast<ushort>();
            var subRegionCodes = Enum.GetValues<UnM49ByAlpha3Code>().Select(code => code.GetSubRegionCode()).Where(code => code is not null).Cast<ushort>();
            var intermediateRegionCodes = Enum.GetValues<UnM49ByAlpha3Code>().Select(code => code.GetIntermediateRegionCode()).Where(code => code is not null).Cast<ushort>();
            TestCases = globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCodes).Select(code => new object[] { code, Bcp47LanguageTag.Create($"{Iso639Part1.en}-{code:000}") }).AsEnumerable();
        }

        public IEnumerator<object[]> GetEnumerator()
        {
            return TestCases.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)TestCases).GetEnumerator();
        }
    }

    #endregion

    #region Test: Build_ForVariantSubtag

    [Theory]
    [MemberData(nameof(Build_ForVariantSubtag_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForVariantSubtag_TestCaseGenerator))]
    public void Build_ForVariantSubtag_ProducesBcp47LanguageTag(string variantSubtag, Bcp47LanguageTag expectedResult)
    {
        // arrange
        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(Build_ForVariantSubtag_TestCaseGenerator.PrimaryLanguageSubtag);

        // act
        var testResult = builder.UsingVariantSubtags(variantSubtag).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForVariantSubtag_TestCaseGenerator
    {
        internal static readonly Iso639Part1 PrimaryLanguageSubtag = Iso639Part1.en;
        private static readonly string[] TestValues;

        static Build_ForVariantSubtag_TestCaseGenerator()
        {
            TestValues =
                new[]
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
        }

        public static IEnumerable<object[]> TestCases
        {
            get
            {
                foreach (var testValue in TestValues)
                {
                    yield return new object[] { testValue, Bcp47LanguageTag.Create($"{PrimaryLanguageSubtag}-{testValue}") };
                }
            }
        }
    }

    #endregion

    #region Test: Build_ForExtensionSubtag

    [Theory]
    [MemberData(nameof(Build_ForExtensionSubtag_TestCaseGenerator.TestCases), MemberType = typeof(Build_ForExtensionSubtag_TestCaseGenerator))]
    public void Build_ForExtensionSubtag_ProducesBcp47LanguageTag(string primaryLanguageSubtag, string? regionSubtag, string extensionSubtag, Bcp47LanguageTag expectedResult)
    {
        // arrange
        IBcp47LanguageTagBuilderStepUsingExtensionSubtags builder = regionSubtag is null
            ? new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(primaryLanguageSubtag)
            : new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(primaryLanguageSubtag).UsingRegionSubtag(regionSubtag);

        // act
        var testResult = builder.UsingExtensionSubtags(extensionSubtag).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class Build_ForExtensionSubtag_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object?[] { "en", null, "t-jp", Bcp47LanguageTag.Create("en-t-jp") };
                yield return new object?[] { "gsw", null, "u-sd-chzh", Bcp47LanguageTag.Create("gsw-u-sd-chzh") };
                yield return new object?[] { "ar", null, "u-nu-latn", Bcp47LanguageTag.Create("ar-u-nu-latn") };
                yield return new object?[] { "he", "IL", "u-ca-hebrew-tz-jeruslm", Bcp47LanguageTag.Create("he-IL-u-ca-hebrew-tz-jeruslm") };
            }
        }
    }

    #endregion

    #region Test: Build_ForPrivateUseSubtag

    [Fact]
    public void Build_ForPrivateUseSubtag_ProducesBcp47LanguageTag()
    {
        // arrange
        const string testValue = "x-private-use-subtag";
        var expectedResult = Bcp47LanguageTag.Create($"{Iso639Part1.en}-{testValue}");

        var builder = new Bcp47LanguageTagBuilder().UsingPrimaryLanguageSubtag(DefaultPrimaryLanguageSubtag);

        // act
        var testResult = builder.UsingPrivateUseSubtag(testValue).Build();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion
}