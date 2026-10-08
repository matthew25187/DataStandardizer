using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UnM49ExtensionsTests
{
    private static readonly ushort[] _hierarchicalM49Codes;

    static UnM49ExtensionsTests()
    {
        _hierarchicalM49Codes = [001, 002, 015, 202, 011, 014, 017, 018, 019, 021, 419, 005, 013, 029, 142, 030, 034, 035, 143, 145, 150, 039, 151, 154, 155, 009, 053, 054, 057, 061];
    }

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
                yield return new object[] { UnM49AreaByAlpha2CountryCode.DO, "en", "Dominican Republic" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.MS, "zh", "蒙特塞拉特" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PH, "ru", "Филиппины" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.UA, "fr", "Ukraine" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.NL, "es", "Países Bajos (Reino de los)" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.NC, "ar", "كاليدونيا الجديدة" };
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
                yield return new object[] { UnM49AreaByAlpha3CountryCode.DOM, "en", "Dominican Republic" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.MSR, "zh", "蒙特塞拉特" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.PHL, "ru", "Филиппины" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.UKR, "fr", "Ukraine" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.NLD, "es", "Países Bajos (Reino de los)" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.NCL, "ar", "كاليدونيا الجديدة" };
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
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, 1 }; }
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
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, 1 }; }
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
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "en", "World" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "zh", "世界" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "ru", "Весь мир" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "fr", "Monde" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "es", "Mundo" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.AT, "ar", "العالم" };
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
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "en", "World" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "zh", "世界" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "ru", "Весь мир" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "fr", "Monde" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "es", "Mundo" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.AUT, "ar", "العالم" };
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
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, 005 }; }
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
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, 005 }; }
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
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "en", "South America" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "zh", "南美洲" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "ru", "Южная Америка" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "fr", "Amérique du Sud" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "es", "América del Sur" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.GY, "ar", "أمريكا الجنوبية" };
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
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "en", "South America" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "zh", "南美洲" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "ru", "Южная Америка" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "fr", "Amérique du Sud" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "es", "América del Sur" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.GUY, "ar", "أمريكا الجنوبية" };
            }
        }
    }

    #endregion

    [Fact]
    public void GetM49Codes_WithM49CodesFromByAlpha2CodeEnum_ReturnsAllM49Codes()
    {
        // arrange
        var m49Codes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>().Cast<ushort>();
        var expectedResult = _hierarchicalM49Codes.Union(m49Codes);

        // act
        var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49AreaByAlpha2CountryCode));

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public void GetM49Codes_WithM49CodesFromByAlpha3CodeEnum_ReturnsAllM49Codes()
    {
        // arrange
        var m49Codes = Enum.GetValues<UnM49AreaByAlpha3CountryCode>().Cast<ushort>();
        var expectedResult = _hierarchicalM49Codes.Union(m49Codes);

        // act
        var testResult = UnM49Extensions.GetM49Codes(typeof(UnM49AreaByAlpha3CountryCode));

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

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
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, 2 }; }
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
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, 2 }; }
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
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "en", "Africa" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "zh", "非洲" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "ru", "Африка" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "fr", "Afrique" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "es", "África" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.CG, "ar", "أفريقيا" };
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
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "en", "Africa" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "zh", "非洲" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "ru", "Африка" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "fr", "Afrique" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "es", "África" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.COG, "ar", "أفريقيا" };
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
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, 419 }; }
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
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.PAN, 419 }; }
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
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "en", "Latin America and the Caribbean" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "zh", "拉丁美洲和加勒比" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ru", "Латинская Америка и Карибский бассейн" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "fr", "Amérique latine et Caraïbes" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "es", "América Latina y el Caribe" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ar", "أمريكا اللاتينية ومنطقة البحر الكاريبي" };
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
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "en", "Latin America and the Caribbean" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "zh", "拉丁美洲和加勒比" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ru", "Латинская Америка и Карибский бассейн" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "fr", "Amérique latine et Caraïbes" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "es", "América Latina y el Caribe" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.PA, "ar", "أمريكا اللاتينية ومنطقة البحر الكاريبي" };
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
        // act
        var testResult = ((UnM49Area)9999).GetLevel();

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
        // act
        var testResult = ((UnM49Area)9999).GetParent();

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
                yield return new object[] { UnM49Area._894, "en", "Zambia" };
                yield return new object[] { UnM49Area._010, "en", "Antarctica" };

                yield return new object[] { UnM49Area._001, "ar", "العالم" };
                yield return new object[] { UnM49Area._002, "zh", "非洲" };
                yield return new object[] { UnM49Area._202, "ru", "Африка к югу от Сахары" };
                yield return new object[] { UnM49Area._014, "fr", "Afrique orientale" };
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
                yield return new object[] { UnM49AreaByAlpha2CountryCode.ZM, "en", "Zambia" };
                yield return new object[] { UnM49AreaByAlpha2CountryCode.NC, "ar", "كاليدونيا الجديدة" };
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
                yield return new object[] { UnM49AreaByAlpha3CountryCode.ZMB, "en", "Zambia" };
                yield return new object[] { UnM49AreaByAlpha3CountryCode.UKR, "fr", "Ukraine" };
            }
        }
    }

    #endregion

    #region Test: GetName_WithUndefinedM49AreaCode

    [Fact]
    public void GetName_WithUndefinedM49AreaCode_ReturnsNull()
    {
        // act
        var testResult = ((UnM49Area)9999).GetName("en");

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: GetAncestorName_WithM49AreaCode

    [Theory]
    [MemberData(nameof(GetAncestorName_WithM49AreaCode_TestCaseGenerator.TestCases), MemberType = typeof(GetAncestorName_WithM49AreaCode_TestCaseGenerator))]
    public void GetAncestorName_WithM49AreaCode_ReturnsCascadedName(UnM49Area testCode, string expectedRegionName, string expectedGlobalName)
    {
        // act
        var testRegionName = testCode.GetRegionName("en");
        var testGlobalName = testCode.GetGlobalName("en");

        // assert
        testRegionName.Should().Be(expectedRegionName);
        testGlobalName.Should().Be(expectedGlobalName);
    }

    private class GetAncestorName_WithM49AreaCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                // Names of every level cascade down to the decorated code, mirroring the numeric codes.
                yield return new object[] { UnM49Area._894, "Africa", "World" };
                yield return new object[] { UnM49Area._004, "Asia", "World" };
                yield return new object[] { UnM49Area._014, "Africa", "World" };
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
        var testResult = testCode.GetCountryOrAreaName("en");

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
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.NZ, "NZ" }; }
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
            get { yield return new object[] { UnM49AreaByAlpha2CountryCode.NZ, "NZL" }; }
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
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.NZL, "NZ" }; }
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
            get { yield return new object[] { UnM49AreaByAlpha3CountryCode.NZL, "NZL" }; }
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
            get { yield return new object[] { UnM49Area._554, "NZ" }; }
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
            get { yield return new object[] { UnM49Area._554, "NZL" }; }
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
        // act
        var testResult = ((UnM49Area)9999).GetIso3166Part1Alpha2Code();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_WithUndefinedM49AreaCode

    [Fact]
    public void GetIso3166Part1Alpha3Code_WithUndefinedM49AreaCode_ReturnsNull()
    {
        // act
        var testResult = ((UnM49Area)9999).GetIso3166Part1Alpha3Code();

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: GetIso3166Part1Alpha2Code_ForEveryMemberOfUnM49AreaByAlpha2CountryCode

    [Fact]
    public void GetIso3166Part1Alpha2Code_ForEveryMemberOfUnM49AreaByAlpha2CountryCode_MatchesTheMemberName()
    {
        // arrange
        var testCodes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>();

        // act, assert
        testCodes.Should().NotBeEmpty();
        foreach (var testCode in testCodes)
        {
            // Every member of this enum is named for its ISO 3166 Part 1 Alpha2 country code, so the
            // metadata carried by the code attribute must agree with the member name.
            testCode.GetIso3166Part1Alpha2Code().Should().Be(Enum.GetName(testCode));
        }
    }

    #endregion

    #region Test: GetIso3166Part1Alpha3Code_ForEveryMemberOfUnM49AreaByAlpha3CountryCode

    [Fact]
    public void GetIso3166Part1Alpha3Code_ForEveryMemberOfUnM49AreaByAlpha3CountryCode_MatchesTheMemberName()
    {
        // arrange
        var testCodes = Enum.GetValues<UnM49AreaByAlpha3CountryCode>();

        // act, assert
        testCodes.Should().NotBeEmpty();
        foreach (var testCode in testCodes)
        {
            // Every member of this enum is named for its ISO 3166 Part 1 Alpha3 country code, so the
            // metadata carried by the code attribute must agree with the member name.
            testCode.GetIso3166Part1Alpha3Code().Should().Be(Enum.GetName(testCode));
        }
    }

    #endregion

    #region Test: GetIso3166Part1Codes_ForCorrelatedAlpha2AndAlpha3Codes

    [Fact]
    public void GetIso3166Part1Codes_ForCorrelatedAlpha2AndAlpha3Codes_Agree()
    {
        // arrange
        var testCodes = Enum.GetValues<UnM49AreaByAlpha2CountryCode>();

        // act, assert
        testCodes.Should().NotBeEmpty();
        foreach (var testCode in testCodes)
        {
            // The same area reached through either enum, or through the numeric area enum, must
            // report the same pair of ISO 3166 country codes.
            var alpha3Code = (UnM49AreaByAlpha3CountryCode)testCode;
            var areaCode = (UnM49Area)testCode;

            alpha3Code.GetIso3166Part1Alpha2Code().Should().Be(testCode.GetIso3166Part1Alpha2Code());
            alpha3Code.GetIso3166Part1Alpha3Code().Should().Be(testCode.GetIso3166Part1Alpha3Code());
            areaCode.GetIso3166Part1Alpha2Code().Should().Be(testCode.GetIso3166Part1Alpha2Code());
            areaCode.GetIso3166Part1Alpha3Code().Should().Be(testCode.GetIso3166Part1Alpha3Code());
        }
    }

    #endregion

    #region Test: GetM49Codes_WithM49CodesFromAreaEnum

    [Fact]
    public void GetM49Codes_WithM49CodesFromAreaEnum_ReturnsAllM49Codes()
    {
        // act
        var testResult = UnM49Extensions.GetM49Codes<UnM49Area>();

        // assert
        testResult.Should().Contain(_hierarchicalM49Codes);
    }

    #endregion
}
