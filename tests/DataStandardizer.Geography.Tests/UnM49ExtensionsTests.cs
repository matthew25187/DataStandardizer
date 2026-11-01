using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UnM49ExtensionsTests
{
    // ISO 639-1 two-letter codes of the six official UN languages, the languages UN M49 gives its names in.
    private const string Iso639Part1Arabic = "ar";
    private const string Iso639Part1Chinese = "zh";
    private const string Iso639Part1English = "en";
    private const string Iso639Part1French = "fr";
    private const string Iso639Part1Russian = "ru";
    private const string Iso639Part1Spanish = "es";

    // UN M49 area codes.
    private const ushort WorldM49Code = 001;
    private const ushort AfricaM49Code = 002;
    private const ushort SouthAmericaM49Code = 005;
    private const ushort LatinAmericaAndTheCaribbeanM49Code = 419;

    // A number that UN M49 doesn't assign: its codes have three digits.
    private const ushort UndefinedM49Code = 9999;

    // ISO 3166-1 codes of New Zealand.
    private const string NewZealandIso3166Part1Alpha2Code = "NZ";
    private const string NewZealandIso3166Part1Alpha3Code = "NZL";

    // UN M49 names of countries or areas.
    private const string AntarcticaNameEnglish = "Antarctica";
    private const string DominicanRepublicNameEnglish = "Dominican Republic";
    private const string MontserratNameChinese = "蒙特塞拉特";
    private const string NetherlandsNameSpanish = "Países Bajos (Reino de los)";
    private const string NewCaledoniaNameArabic = "كاليدونيا الجديدة";
    private const string PhilippinesNameRussian = "Филиппины";
    private const string UkraineNameFrench = "Ukraine";
    private const string ZambiaNameEnglish = "Zambia";

    // UN M49 names of the world, regions, sub-regions and intermediate regions.
    private const string WorldNameArabic = "العالم";
    private const string WorldNameChinese = "世界";
    private const string WorldNameEnglish = "World";
    private const string WorldNameFrench = "Monde";
    private const string WorldNameRussian = "Весь мир";
    private const string WorldNameSpanish = "Mundo";
    private const string AfricaNameArabic = "أفريقيا";
    private const string AfricaNameChinese = "非洲";
    private const string AfricaNameEnglish = "Africa";
    private const string AfricaNameFrench = "Afrique";
    private const string AfricaNameRussian = "Африка";
    private const string AfricaNameSpanish = "África";
    private const string AsiaNameEnglish = "Asia";
    private const string SubSaharanAfricaNameRussian = "Африка к югу от Сахары";
    private const string EasternAfricaNameFrench = "Afrique orientale";
    private const string LatinAmericaAndTheCaribbeanNameArabic = "أمريكا اللاتينية ومنطقة البحر الكاريبي";
    private const string LatinAmericaAndTheCaribbeanNameChinese = "拉丁美洲和加勒比";
    private const string LatinAmericaAndTheCaribbeanNameEnglish = "Latin America and the Caribbean";
    private const string LatinAmericaAndTheCaribbeanNameFrench = "Amérique latine et Caraïbes";
    private const string LatinAmericaAndTheCaribbeanNameRussian = "Латинская Америка и Карибский бассейн";
    private const string LatinAmericaAndTheCaribbeanNameSpanish = "América Latina y el Caribe";
    private const string SouthAmericaNameArabic = "أمريكا الجنوبية";
    private const string SouthAmericaNameChinese = "南美洲";
    private const string SouthAmericaNameEnglish = "South America";
    private const string SouthAmericaNameFrench = "Amérique du Sud";
    private const string SouthAmericaNameRussian = "Южная Америка";
    private const string SouthAmericaNameSpanish = "América del Sur";

    /// <summary>
    /// Every UN M49 code: the world, its regions, sub-regions and intermediate regions, then every country or area.
    /// </summary>
    private static ushort[] AllM49Codes() =>
    [
        // The world, regions, sub-regions and intermediate regions.
        001, 002, 005, 009, 011, 013, 014, 015, 017, 018, 019, 021, 029, 030, 034, 035, 039, 053, 054, 057,
        061, 142, 143, 145, 150, 151, 154, 155, 202, 419,

        // Countries or areas.
        004, 008, 010, 012, 016, 020, 024, 028, 031, 032, 036, 040, 044, 048, 050, 051, 052, 056, 060, 064,
        068, 070, 072, 074, 076, 084, 086, 090, 092, 096, 100, 104, 108, 112, 116, 120, 124, 132, 136, 140,
        144, 148, 152, 156, 162, 166, 170, 174, 175, 178, 180, 184, 188, 191, 192, 196, 203, 204, 208, 212,
        214, 218, 222, 226, 231, 232, 233, 234, 238, 239, 242, 246, 248, 250, 254, 258, 260, 262, 266, 268,
        270, 275, 276, 288, 292, 296, 300, 304, 308, 312, 316, 320, 324, 328, 332, 334, 336, 340, 344, 348,
        352, 356, 360, 364, 368, 372, 376, 380, 384, 388, 392, 398, 400, 404, 408, 410, 414, 417, 418, 422,
        426, 428, 430, 434, 438, 440, 442, 446, 450, 454, 458, 462, 466, 470, 474, 478, 480, 484, 492, 496,
        498, 499, 500, 504, 508, 512, 516, 520, 524, 528, 531, 533, 534, 535, 540, 548, 554, 558, 562, 566,
        570, 574, 578, 580, 581, 583, 584, 585, 586, 591, 598, 600, 604, 608, 612, 616, 620, 624, 626, 630,
        634, 638, 642, 643, 646, 652, 654, 659, 660, 662, 663, 666, 670, 674, 678, 682, 686, 688, 690, 694,
        702, 703, 704, 705, 706, 710, 716, 724, 728, 729, 732, 740, 744, 748, 752, 756, 760, 762, 764, 768,
        772, 776, 780, 784, 788, 792, 795, 796, 798, 800, 804, 807, 818, 826, 831, 832, 833, 834, 840, 850,
        854, 858, 860, 862, 876, 882, 887, 894,
    ];

    #region Test: GetCountryOrAreaName_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetCountryOrAreaName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetCountryOrAreaName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetCountryOrAreaName_WithM49CodeByAlpha2Code_ReturnsCountryOrAreaName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetCountryOrAreaName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetCountryOrAreaName_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha2CountryCode.DO, Iso639Part1English, DominicanRepublicNameEnglish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.MS, Iso639Part1Chinese, MontserratNameChinese };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PH, Iso639Part1Russian, PhilippinesNameRussian };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.UA, Iso639Part1French, UkraineNameFrench };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.NL, Iso639Part1Spanish, NetherlandsNameSpanish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.NC, Iso639Part1Arabic, NewCaledoniaNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetCountryOrAreaName_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetCountryOrAreaName_WithM49CodeByAlpha3Code_ReturnsCountryOrAreaName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetCountryOrAreaName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetCountryOrAreaName_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha3CountryCode.DOM, Iso639Part1English, DominicanRepublicNameEnglish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.MSR, Iso639Part1Chinese, MontserratNameChinese };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PHL, Iso639Part1Russian, PhilippinesNameRussian };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.UKR, Iso639Part1French, UkraineNameFrench };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.NLD, Iso639Part1Spanish, NetherlandsNameSpanish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.NCL, Iso639Part1Arabic, NewCaledoniaNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetGlobalCode_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetGlobalCode_WithM49CodeByAlpha2Code_ReturnsGlobalCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetGlobalCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetGlobalCode_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, WorldM49Code }; }
        }
    }

    #endregion

    #region Test: GetGlobalCode_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetGlobalCode_WithM49CodeByAlpha3Code_ReturnsGlobalCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetGlobalCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetGlobalCode_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, WorldM49Code }; }
        }
    }

    #endregion

    #region Test: GetGlobalName_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetGlobalName_WithM49CodeByAlpha2Code_ReturnsGlobalName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetGlobalName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetGlobalName_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, Iso639Part1English, WorldNameEnglish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, Iso639Part1Chinese, WorldNameChinese };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, Iso639Part1Russian, WorldNameRussian };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, Iso639Part1French, WorldNameFrench };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, Iso639Part1Spanish, WorldNameSpanish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, Iso639Part1Arabic, WorldNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetGlobalName_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetGlobalName_WithM49CodeByAlpha3Code_ReturnsGlobalName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetGlobalName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetGlobalName_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, Iso639Part1English, WorldNameEnglish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, Iso639Part1Chinese, WorldNameChinese };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, Iso639Part1Russian, WorldNameRussian };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, Iso639Part1French, WorldNameFrench };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, Iso639Part1Spanish, WorldNameSpanish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, Iso639Part1Arabic, WorldNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetIntermediateRegionCode_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetIntermediateRegionCode_WithM49CodeByAlpha2Code_ReturnsIntermediateRegionCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetIntermediateRegionCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIntermediateRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, SouthAmericaM49Code }; }
        }
    }

    #endregion

    #region Test: GetIntermediateRegionCode_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetIntermediateRegionCode_WithM49CodeByAlpha3Code_ReturnsIntermediateRegionCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetIntermediateRegionCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIntermediateRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, SouthAmericaM49Code }; }
        }
    }

    #endregion

    #region Test: GetIntermediateRegionName_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetIntermediateRegionName_WithM49CodeByAlpha2Code_ReturnsIntermediateRegionName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIntermediateRegionName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIntermediateRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, Iso639Part1English, SouthAmericaNameEnglish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, Iso639Part1Chinese, SouthAmericaNameChinese };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, Iso639Part1Russian, SouthAmericaNameRussian };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, Iso639Part1French, SouthAmericaNameFrench };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, Iso639Part1Spanish, SouthAmericaNameSpanish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, Iso639Part1Arabic, SouthAmericaNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetIntermediateRegionName_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetIntermediateRegionName_WithM49CodeByAlpha3Code_ReturnsIntermediateRegionName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIntermediateRegionName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIntermediateRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, Iso639Part1English, SouthAmericaNameEnglish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, Iso639Part1Chinese, SouthAmericaNameChinese };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, Iso639Part1Russian, SouthAmericaNameRussian };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, Iso639Part1French, SouthAmericaNameFrench };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, Iso639Part1Spanish, SouthAmericaNameSpanish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, Iso639Part1Arabic, SouthAmericaNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetM49Codes

    [Fact]
    public void GetM49Codes_WithM49CodesFromByAlpha2CodeEnum_ReturnsAllM49Codes()
    {
        // arrange
        var expectedResult = AllM49Codes();

        // act
        var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49AreaByAlpha2CountryCode));

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void GetM49Codes_WithM49CodesFromByAlpha3CodeEnum_ReturnsAllM49Codes()
    {
        // arrange
        var expectedResult = AllM49Codes();

        // act
        var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49AreaByAlpha3CountryCode));

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void GetM49Codes_WithM49CodesFromAreaEnum_ReturnsAllM49Codes()
    {
        // arrange
        var expectedResult = AllM49Codes();

        // act
        var testResult = UnM49Extensions.GetM49Codes<UnM49Area>();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    #endregion

    #region Test: GetRegionCode_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetRegionCode_WithM49CodeByAlpha2Code_ReturnsRegionCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetRegionCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, AfricaM49Code }; }
        }
    }

    #endregion

    #region Test: GetRegionCode_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetRegionCode_WithM49CodeByAlpha3Code_ReturnsRegionCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetRegionCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, AfricaM49Code }; }
        }
    }

    #endregion

    #region Test: GetRegionName_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetRegionName_WithM49CodeByAlpha2Code_ReturnsRegionName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetRegionName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, Iso639Part1English, AfricaNameEnglish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, Iso639Part1Chinese, AfricaNameChinese };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, Iso639Part1Russian, AfricaNameRussian };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, Iso639Part1French, AfricaNameFrench };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, Iso639Part1Spanish, AfricaNameSpanish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, Iso639Part1Arabic, AfricaNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetRegionName_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetRegionName_WithM49CodeByAlpha3Code_ReturnsRegionName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetRegionName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, Iso639Part1English, AfricaNameEnglish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, Iso639Part1Chinese, AfricaNameChinese };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, Iso639Part1Russian, AfricaNameRussian };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, Iso639Part1French, AfricaNameFrench };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, Iso639Part1Spanish, AfricaNameSpanish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, Iso639Part1Arabic, AfricaNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetSubRegionCode_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetSubRegionCode_WithM49CodeByAlpha2Code_ReturnsSubRegionCode(UnM49AreaByAlpha2CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetSubRegionCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubRegionCode_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, LatinAmericaAndTheCaribbeanM49Code }; }
        }
    }

    #endregion

    #region Test: GetSubRegionCode_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetSubRegionCode_WithM49CodeByAlpha3Code_ReturnsSubRegionCode(UnM49AreaByAlpha3CountryCode testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetSubRegionCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubRegionCode_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, LatinAmericaAndTheCaribbeanM49Code }; }
        }
    }

    #endregion

    #region Test: GetSubRegionName_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetSubRegionName_WithM49CodeByAlpha2Code_ReturnsSubRegionName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetSubRegionName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubRegionName_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, Iso639Part1English, LatinAmericaAndTheCaribbeanNameEnglish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, Iso639Part1Chinese, LatinAmericaAndTheCaribbeanNameChinese };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, Iso639Part1Russian, LatinAmericaAndTheCaribbeanNameRussian };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, Iso639Part1French, LatinAmericaAndTheCaribbeanNameFrench };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, Iso639Part1Spanish, LatinAmericaAndTheCaribbeanNameSpanish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, Iso639Part1Arabic, LatinAmericaAndTheCaribbeanNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetSubRegionName_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetSubRegionName_WithM49CodeByAlpha3Code_ReturnsSubRegionName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetSubRegionName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubRegionName_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, Iso639Part1English, LatinAmericaAndTheCaribbeanNameEnglish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, Iso639Part1Chinese, LatinAmericaAndTheCaribbeanNameChinese };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, Iso639Part1Russian, LatinAmericaAndTheCaribbeanNameRussian };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, Iso639Part1French, LatinAmericaAndTheCaribbeanNameFrench };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, Iso639Part1Spanish, LatinAmericaAndTheCaribbeanNameSpanish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, Iso639Part1Arabic, LatinAmericaAndTheCaribbeanNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetLevel_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetLevel_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetLevel_WithM49AreaCode_TestCaseGenerator))]
    public void GetLevel_WithM49AreaCode_ReturnsLevel(UnM49Area testCode, UnM49AreaLevel expectedResult)
    {
        // act
        var testResult = testCode.GetLevel();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetLevel_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49Area._001, UnM49AreaLevel.Global };
                yield return new object[] { UnM49Area._002, UnM49AreaLevel.Region };
                yield return new object[] { UnM49Area._015, UnM49AreaLevel.SubRegion };
                yield return new object[] { UnM49Area._202, UnM49AreaLevel.SubRegion };
                yield return new object[] { UnM49Area._014, UnM49AreaLevel.IntermediateRegion };
                yield return new object[] { UnM49Area._004, UnM49AreaLevel.CountryOrArea };

                // The intermediate region and the country carry identical codes on their attributes, so
                // the level is distinguished by whether the code itself appears among those codes.
                yield return new object[] { UnM49Area._894, UnM49AreaLevel.CountryOrArea };

                // Antarctica carries only a global code, as does the world itself.
                yield return new object[] { UnM49Area._010, UnM49AreaLevel.CountryOrArea };
            }
        }
    }

    #endregion

    #region Test: GetLevel_WithUndefinedM49AreaCode

    [Fact]
    public void GetLevel_WithUndefinedM49AreaCode_ReturnsNull()
    {
        // arrange
        var testCode = (UnM49Area)UndefinedM49Code;

        // act
        var testResult = testCode.GetLevel();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: GetParent_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetParent_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetParent_WithM49AreaCode_TestCaseGenerator))]
    public void GetParent_WithM49AreaCode_ReturnsParent(UnM49Area testCode, UnM49Area? expectedResult)
    {
        // act
        var testResult = testCode.GetParent();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetParent_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object?[] { UnM49Area._894, UnM49Area._014 };
                yield return new object?[] { UnM49Area._014, UnM49Area._202 };
                yield return new object?[] { UnM49Area._202, UnM49Area._002 };
                yield return new object?[] { UnM49Area._002, UnM49Area._001 };

                // The hierarchy is sparse: Afghanistan has no intermediate region, so its parent is its
                // sub-region, and Antarctica has no region at all, so its parent is the world.
                yield return new object?[] { UnM49Area._004, UnM49Area._034 };
                yield return new object?[] { UnM49Area._010, UnM49Area._001 };

                // The world has no parent.
                yield return new object?[] { UnM49Area._001, null };
            }
        }
    }

    #endregion

    #region Test: GetParent_WithUndefinedM49AreaCode

    [Fact]
    public void GetParent_WithUndefinedM49AreaCode_ReturnsNull()
    {
        // arrange
        var testCode = (UnM49Area)UndefinedM49Code;

        // act
        var testResult = testCode.GetParent();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: IsWithin_WithM49AreaCode

    [Theory]
    [MemberData(nameof(IsWithin_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(IsWithin_WithM49AreaCode_TestCaseGenerator))]
    public void IsWithin_WithM49AreaCode_ReturnsContainment(UnM49Area testCode, UnM49Area otherCode, bool expectedResult)
    {
        // act
        var testResult = testCode.IsWithin(otherCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class IsWithin_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // Containment is transitive across every level of the hierarchy.
                yield return new object[] { UnM49Area._894, UnM49Area._014, true };
                yield return new object[] { UnM49Area._894, UnM49Area._202, true };
                yield return new object[] { UnM49Area._894, UnM49Area._002, true };
                yield return new object[] { UnM49Area._894, UnM49Area._001, true };

                // Zambia is in Africa, not Oceania.
                yield return new object[] { UnM49Area._894, UnM49Area._009, false };

                // An area does not fall within itself.
                yield return new object[] { UnM49Area._894, UnM49Area._894, false };
                yield return new object[] { UnM49Area._001, UnM49Area._001, false };

                // Containment does not hold in the opposite direction.
                yield return new object[] { UnM49Area._002, UnM49Area._894, false };
            }
        }
    }

    #endregion

    #region Test: GetName_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetName_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetName_WithM49AreaCode_TestCaseGenerator))]
    public void GetName_WithM49AreaCode_ReturnsNameOfOwnLevel(UnM49Area testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetName_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // A country resolves to its own name, not to the name of any of its ancestors.
                yield return new object[] { UnM49Area._894, Iso639Part1English, ZambiaNameEnglish };
                yield return new object[] { UnM49Area._010, Iso639Part1English, AntarcticaNameEnglish };

                yield return new object[] { UnM49Area._001, Iso639Part1Arabic, WorldNameArabic };
                yield return new object[] { UnM49Area._002, Iso639Part1Chinese, AfricaNameChinese };
                yield return new object[] { UnM49Area._202, Iso639Part1Russian, SubSaharanAfricaNameRussian };
                yield return new object[] { UnM49Area._014, Iso639Part1French, EasternAfricaNameFrench };
            }
        }
    }

    #endregion

    #region Test: GetName_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetName_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetName_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetName_WithM49CodeByAlpha2Code_ReturnsCountryOrAreaName(UnM49AreaByAlpha2CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetName_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha2CountryCode.ZM, Iso639Part1English, ZambiaNameEnglish };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.NC, Iso639Part1Arabic, NewCaledoniaNameArabic };
            }
        }
    }

    #endregion

    #region Test: GetName_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetName_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetName_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetName_WithM49CodeByAlpha3Code_ReturnsCountryOrAreaName(UnM49AreaByAlpha3CountryCode testCode, string languageCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetName_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { UnM49AreaByAlpha3CountryCode.ZMB, Iso639Part1English, ZambiaNameEnglish };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.UKR, Iso639Part1French, UkraineNameFrench };
            }
        }
    }

    #endregion

    #region Test: GetName_WithUndefinedM49AreaCode

    [Fact]
    public void GetName_WithUndefinedM49AreaCode_ReturnsNull()
    {
        // arrange
        var testCode = (UnM49Area)UndefinedM49Code;

        // act
        var testResult = testCode.GetName(Iso639Part1English);

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: GetRegionName_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetRegionName_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetRegionName_WithM49AreaCode_TestCaseGenerator))]
    public void GetRegionName_WithM49AreaCode_ReturnsCascadedRegionName(UnM49Area testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetRegionName(Iso639Part1English);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetRegionName_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // Names of every level cascade down to the decorated code, mirroring the numeric codes.
                yield return new object[] { UnM49Area._894, AfricaNameEnglish };
                yield return new object[] { UnM49Area._004, AsiaNameEnglish };
                yield return new object[] { UnM49Area._014, AfricaNameEnglish };
            }
        }
    }

    #endregion

    #region Test: GetGlobalName_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetGlobalName_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetGlobalName_WithM49AreaCode_TestCaseGenerator))]
    public void GetGlobalName_WithM49AreaCode_ReturnsCascadedGlobalName(UnM49Area testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetGlobalName(Iso639Part1English);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetGlobalName_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // Names of every level cascade down to the decorated code, mirroring the numeric codes.
                yield return new object[] { UnM49Area._894, WorldNameEnglish };
                yield return new object[] { UnM49Area._004, WorldNameEnglish };
                yield return new object[] { UnM49Area._014, WorldNameEnglish };
            }
        }
    }

    #endregion

    #region Test: GetCountryOrAreaName_WithNonCountryM49AreaCode

    [Theory]
    [MemberData(nameof(GetCountryOrAreaName_WithNonCountryM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetCountryOrAreaName_WithNonCountryM49AreaCode_TestCaseGenerator))]
    public void GetCountryOrAreaName_WithNonCountryM49AreaCode_ReturnsNull(UnM49Area testCode)
    {
        // act
        var testResult = testCode.GetCountryOrAreaName(Iso639Part1English);

        // assert
        testResult.Should().BeNull();
    }

    private class GetCountryOrAreaName_WithNonCountryM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // A code above the country or area level has no country or area name.
                yield return new object[] { UnM49Area._001 };
                yield return new object[] { UnM49Area._002 };
                yield return new object[] { UnM49Area._015 };
                yield return new object[] { UnM49Area._014 };
            }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha2Code_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha2Code_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha2Code_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetIso3166Part1Alpha2Code_WithM49CodeByAlpha2Code_ReturnsIso3166Part1Alpha2Code(UnM49AreaByAlpha2CountryCode testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha2Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha2Code_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.NZ, NewZealandIso3166Part1Alpha2Code }; }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_WithM49CodeByAlpha2Code

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha3Code_WithM49CodeByAlpha2Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha3Code_WithM49CodeByAlpha2Code_TestCaseGenerator))]
    public void GetIso3166Part1Alpha3Code_WithM49CodeByAlpha2Code_ReturnsIso3166Part1Alpha3Code(UnM49AreaByAlpha2CountryCode testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha3Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha3Code_WithM49CodeByAlpha2Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.NZ, NewZealandIso3166Part1Alpha3Code }; }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha2Code_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha2Code_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha2Code_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetIso3166Part1Alpha2Code_WithM49CodeByAlpha3Code_ReturnsIso3166Part1Alpha2Code(UnM49AreaByAlpha3CountryCode testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha2Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha2Code_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.NZL, NewZealandIso3166Part1Alpha2Code }; }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_WithM49CodeByAlpha3Code

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha3Code_WithM49CodeByAlpha3Code_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha3Code_WithM49CodeByAlpha3Code_TestCaseGenerator))]
    public void GetIso3166Part1Alpha3Code_WithM49CodeByAlpha3Code_ReturnsIso3166Part1Alpha3Code(UnM49AreaByAlpha3CountryCode testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha3Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha3Code_WithM49CodeByAlpha3Code_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.NZL, NewZealandIso3166Part1Alpha3Code }; }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha2Code_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha2Code_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha2Code_WithM49AreaCode_TestCaseGenerator))]
    public void GetIso3166Part1Alpha2Code_WithM49AreaCode_ReturnsIso3166Part1Alpha2Code(UnM49Area testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha2Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha2Code_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49Area._554, NewZealandIso3166Part1Alpha2Code }; }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha3Code_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha3Code_WithM49AreaCode_TestCaseGenerator))]
    public void GetIso3166Part1Alpha3Code_WithM49AreaCode_ReturnsIso3166Part1Alpha3Code(UnM49Area testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha3Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha3Code_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { UnM49Area._554, NewZealandIso3166Part1Alpha3Code }; }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha2Code_WithNonCountryM49AreaCode

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha2Code_WithNonCountryM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha2Code_WithNonCountryM49AreaCode_TestCaseGenerator))]
    public void GetIso3166Part1Alpha2Code_WithNonCountryM49AreaCode_ReturnsNull(UnM49Area testCode)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha2Code();

        // assert
        testResult.Should().BeNull();
    }

    private class GetIso3166Part1Alpha2Code_WithNonCountryM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // A code above the country or area level has no ISO 3166 country code.
                yield return new object[] { UnM49Area._001 };
                yield return new object[] { UnM49Area._002 };
                yield return new object[] { UnM49Area._015 };
                yield return new object[] { UnM49Area._419 };
            }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_WithNonCountryM49AreaCode

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha3Code_WithNonCountryM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha3Code_WithNonCountryM49AreaCode_TestCaseGenerator))]
    public void GetIso3166Part1Alpha3Code_WithNonCountryM49AreaCode_ReturnsNull(UnM49Area testCode)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha3Code();

        // assert
        testResult.Should().BeNull();
    }

    private class GetIso3166Part1Alpha3Code_WithNonCountryM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // A code above the country or area level has no ISO 3166 country code.
                yield return new object[] { UnM49Area._001 };
                yield return new object[] { UnM49Area._002 };
                yield return new object[] { UnM49Area._015 };
                yield return new object[] { UnM49Area._419 };
            }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha2Code_WithUndefinedM49AreaCode

    [Fact]
    public void GetIso3166Part1Alpha2Code_WithUndefinedM49AreaCode_ReturnsNull()
    {
        // arrange
        var testCode = (UnM49Area)UndefinedM49Code;

        // act
        var testResult = testCode.GetIso3166Part1Alpha2Code();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_WithUndefinedM49AreaCode

    [Fact]
    public void GetIso3166Part1Alpha3Code_WithUndefinedM49AreaCode_ReturnsNull()
    {
        // arrange
        var testCode = (UnM49Area)UndefinedM49Code;

        // act
        var testResult = testCode.GetIso3166Part1Alpha3Code();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: GetIso3166Part1Alpha2Code_ForEveryMemberOfUnM49AreaByAlpha2CountryCode

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha2Code_ForEveryMemberOfUnM49AreaByAlpha2CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha2Code_ForEveryMemberOfUnM49AreaByAlpha2CountryCode_TestCaseGenerator))]
    public void GetIso3166Part1Alpha2Code_ForEveryMemberOfUnM49AreaByAlpha2CountryCode_MatchesTheMemberName(UnM49AreaByAlpha2CountryCode testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha2Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha2Code_ForEveryMemberOfUnM49AreaByAlpha2CountryCode_TestCaseGenerator
    {
        public static TheoryData<UnM49AreaByAlpha2CountryCode, string> TestCases
        {
            get
            {
                // Every member of this enum is named for its ISO 3166 Part 1 Alpha2 country code, so the
                // metadata carried by the code attribute must agree with the member name.
                var testCases = new TheoryData<UnM49AreaByAlpha2CountryCode, string>();
                foreach (var testCode in Enum.GetValues<UnM49AreaByAlpha2CountryCode>())
                {
                    testCases.Add(testCode, Enum.GetName(testCode)!);
                }

                return testCases;
            }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_ForEveryMemberOfUnM49AreaByAlpha3CountryCode

    [Theory]
    [MemberData(nameof(GetIso3166Part1Alpha3Code_ForEveryMemberOfUnM49AreaByAlpha3CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Alpha3Code_ForEveryMemberOfUnM49AreaByAlpha3CountryCode_TestCaseGenerator))]
    public void GetIso3166Part1Alpha3Code_ForEveryMemberOfUnM49AreaByAlpha3CountryCode_MatchesTheMemberName(UnM49AreaByAlpha3CountryCode testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetIso3166Part1Alpha3Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetIso3166Part1Alpha3Code_ForEveryMemberOfUnM49AreaByAlpha3CountryCode_TestCaseGenerator
    {
        public static TheoryData<UnM49AreaByAlpha3CountryCode, string> TestCases
        {
            get
            {
                // Every member of this enum is named for its ISO 3166 Part 1 Alpha3 country code, so the
                // metadata carried by the code attribute must agree with the member name.
                var testCases = new TheoryData<UnM49AreaByAlpha3CountryCode, string>();
                foreach (var testCode in Enum.GetValues<UnM49AreaByAlpha3CountryCode>())
                {
                    testCases.Add(testCode, Enum.GetName(testCode)!);
                }

                return testCases;
            }
        }
    }

    #endregion

    #region Test: GetIso3166Part1Codes_ForCorrelatedAlpha2AndAlpha3Codes

    [Theory]
    [MemberData(nameof(GetIso3166Part1Codes_ForCorrelatedAlpha2AndAlpha3Codes_TestCaseGenerator.TestCases), MemberType = typeof(GetIso3166Part1Codes_ForCorrelatedAlpha2AndAlpha3Codes_TestCaseGenerator), DisableDiscoveryEnumeration = true)]
    public void GetIso3166Part1Codes_ForCorrelatedAlpha2AndAlpha3Codes_Agree(string entryPoint, Func<string?> getCode, string expectedResult)
    {
        // act
        var testResult = getCode();

        // assert
        testResult.Should().Be(expectedResult, "{0} reports the ISO 3166 country code of the same area", entryPoint);
    }

    private class GetIso3166Part1Codes_ForCorrelatedAlpha2AndAlpha3Codes_TestCaseGenerator
    {
        public static TheoryData<string, Func<string?>, string> TestCases
        {
            get
            {
                // The same area reached through either enum, or through the numeric area enum, must report
                // the same pair of ISO 3166 country codes: the names of its members of the two code enums.
                var testCases = new TheoryData<string, Func<string?>, string>();
                foreach (var alpha2Code in Enum.GetValues<UnM49AreaByAlpha2CountryCode>())
                {
                    var alpha3Code = (UnM49AreaByAlpha3CountryCode)alpha2Code;
                    var areaCode = (UnM49Area)alpha2Code;
                    var expectedAlpha2Code = Enum.GetName(alpha2Code)!;
                    var expectedAlpha3Code = Enum.GetName(alpha3Code)!;

                    testCases.Add($"{nameof(UnM49AreaByAlpha2CountryCode)}.{alpha2Code}.{nameof(UnM49Extensions.GetIso3166Part1Alpha3Code)}", () => alpha2Code.GetIso3166Part1Alpha3Code(), expectedAlpha3Code);
                    testCases.Add($"{nameof(UnM49AreaByAlpha3CountryCode)}.{alpha3Code}.{nameof(UnM49Extensions.GetIso3166Part1Alpha2Code)}", () => alpha3Code.GetIso3166Part1Alpha2Code(), expectedAlpha2Code);
                    testCases.Add($"{nameof(UnM49Area)}.{areaCode}.{nameof(UnM49Extensions.GetIso3166Part1Alpha2Code)}", () => areaCode.GetIso3166Part1Alpha2Code(), expectedAlpha2Code);
                    testCases.Add($"{nameof(UnM49Area)}.{areaCode}.{nameof(UnM49Extensions.GetIso3166Part1Alpha3Code)}", () => areaCode.GetIso3166Part1Alpha3Code(), expectedAlpha3Code);
                }

                return testCases;
            }
        }
    }

    #endregion
}
