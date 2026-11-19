using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class UnM49AreaCodeAttributeTests
{
    // Codes from UN M49 (Standard country or area codes for statistical use).
    private const ushort UnM49CodeWorld = 1;   // 001
    private const ushort UnM49CodeAfrica = 2;  // 002

    // English names from UN M49 (Standard country or area codes for statistical use).
    private const string UnM49EnglishWorld = "World";
    private const string UnM49EnglishAfrica = "Africa";
    private const string UnM49EnglishAsia = "Asia";
    private const string UnM49EnglishSouthernAsia = "Southern Asia";
    private const string UnM49EnglishSouthAmerica = "South America";
    private const string UnM49EnglishAfghanistan = "Afghanistan";

    // French names from UN M49 (Standard country or area codes for statistical use).
    private const string UnM49FrenchWorld = "Monde";
    private const string UnM49FrenchAfrica = "Afrique";
    private const string UnM49FrenchAsia = "Asie";
    private const string UnM49FrenchSouthernAsia = "Asie m\x00E9ridionale";
    private const string UnM49FrenchSouthAmerica = "Am\x00E9rique du Sud";
    private const string UnM49FrenchAfghanistan = "Afghanistan";

    #region Test: EnglishNames_OnArea

    [Theory]
    [MemberData(nameof(AreaTestCaseGenerator.EnglishTestCases), MemberType = typeof(AreaTestCaseGenerator))]
    public void EnglishNames_OnArea_HoldsOnlyTheAreasOwnName(string areaCode, Func<UnM49AreaCodeAttribute> getAttribute, string expectedName)
    {
        // act
        var testResult = getAttribute().EnglishNames;

        // assert
        testResult.Should().Equal(new[] { expectedName }, areaCode);
    }

    #endregion

    #region Test: EnglishName_OnArea

    [Theory]
    [MemberData(nameof(AreaTestCaseGenerator.EnglishTestCases), MemberType = typeof(AreaTestCaseGenerator))]
    public void EnglishName_OnArea_IsTheAreasOwnName(string areaCode, Func<UnM49AreaCodeAttribute> getAttribute, string expectedName)
    {
        // act
        var testResult = getAttribute().EnglishName;

        // assert
        testResult.Should().Be(expectedName, areaCode);
    }

    #endregion

    #region Test: FrenchNames_OnArea

    [Theory]
    [MemberData(nameof(AreaTestCaseGenerator.FrenchTestCases), MemberType = typeof(AreaTestCaseGenerator))]
    public void FrenchNames_OnArea_HoldsOnlyTheAreasOwnName(string areaCode, Func<UnM49AreaCodeAttribute> getAttribute, string expectedName)
    {
        // act
        var testResult = getAttribute().FrenchNames;

        // assert
        testResult.Should().Equal(new[] { expectedName }, areaCode);
    }

    #endregion

    #region Test: FrenchName_OnArea

    [Theory]
    [MemberData(nameof(AreaTestCaseGenerator.FrenchTestCases), MemberType = typeof(AreaTestCaseGenerator))]
    public void FrenchName_OnArea_IsTheAreasOwnName(string areaCode, Func<UnM49AreaCodeAttribute> getAttribute, string expectedName)
    {
        // act
        var testResult = getAttribute().FrenchName;

        // assert
        testResult.Should().Be(expectedName, areaCode);
    }

    #endregion

    private class AreaTestCaseGenerator
    {
        public static TheoryData<string, Func<UnM49AreaCodeAttribute>, string> EnglishTestCases => new TheoryData<string, Func<UnM49AreaCodeAttribute>, string>
        {
            { nameof(UnM49Area._001), () => typeof(UnM49Area).GetField(nameof(UnM49Area._001))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49EnglishWorld },
            { nameof(UnM49Area._002), () => typeof(UnM49Area).GetField(nameof(UnM49Area._002))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49EnglishAfrica },
            { nameof(UnM49Area._034), () => typeof(UnM49Area).GetField(nameof(UnM49Area._034))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49EnglishSouthernAsia },
            { nameof(UnM49Area._005), () => typeof(UnM49Area).GetField(nameof(UnM49Area._005))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49EnglishSouthAmerica },
            { nameof(UnM49Area._004), () => typeof(UnM49Area).GetField(nameof(UnM49Area._004))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49EnglishAfghanistan },
            { nameof(UnM49AreaByAlpha2CountryCode.AF), () => typeof(UnM49AreaByAlpha2CountryCode).GetField(nameof(UnM49AreaByAlpha2CountryCode.AF))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49EnglishAfghanistan },
            { nameof(UnM49AreaByAlpha3CountryCode.AFG), () => typeof(UnM49AreaByAlpha3CountryCode).GetField(nameof(UnM49AreaByAlpha3CountryCode.AFG))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49EnglishAfghanistan },
        };

        public static TheoryData<string, Func<UnM49AreaCodeAttribute>, string> FrenchTestCases => new TheoryData<string, Func<UnM49AreaCodeAttribute>, string>
        {
            { nameof(UnM49Area._001), () => typeof(UnM49Area).GetField(nameof(UnM49Area._001))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49FrenchWorld },
            { nameof(UnM49Area._002), () => typeof(UnM49Area).GetField(nameof(UnM49Area._002))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49FrenchAfrica },
            { nameof(UnM49Area._034), () => typeof(UnM49Area).GetField(nameof(UnM49Area._034))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49FrenchSouthernAsia },
            { nameof(UnM49Area._005), () => typeof(UnM49Area).GetField(nameof(UnM49Area._005))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49FrenchSouthAmerica },
            { nameof(UnM49Area._004), () => typeof(UnM49Area).GetField(nameof(UnM49Area._004))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49FrenchAfghanistan },
            { nameof(UnM49AreaByAlpha2CountryCode.AF), () => typeof(UnM49AreaByAlpha2CountryCode).GetField(nameof(UnM49AreaByAlpha2CountryCode.AF))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49FrenchAfghanistan },
            { nameof(UnM49AreaByAlpha3CountryCode.AFG), () => typeof(UnM49AreaByAlpha3CountryCode).GetField(nameof(UnM49AreaByAlpha3CountryCode.AFG))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!, UnM49FrenchAfghanistan },
        };
    }

    #region Test: LevelName_OnArea

    [Theory]
    [MemberData(nameof(LevelNameTestCaseGenerator.TestCases), MemberType = typeof(LevelNameTestCaseGenerator))]
    public void LevelName_OnArea_IsThatLevelsName(string levelProperty, Func<string?> getLevelName, string expectedName)
    {
        // act
        var testResult = getLevelName();

        // assert
        testResult.Should().Be(expectedName, levelProperty);
    }

    private class LevelNameTestCaseGenerator
    {
        private static UnM49AreaCodeAttribute Afghanistan => typeof(UnM49Area).GetField(nameof(UnM49Area._004))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!;

        private static UnM49AreaCodeAttribute SouthAmerica => typeof(UnM49Area).GetField(nameof(UnM49Area._005))!.GetCustomAttribute<UnM49AreaCodeAttribute>()!;

        public static TheoryData<string, Func<string?>, string> TestCases => new TheoryData<string, Func<string?>, string>
        {
            { nameof(UnM49AreaCodeAttribute.EnglishGlobalName), () => Afghanistan.EnglishGlobalName, UnM49EnglishWorld },
            { nameof(UnM49AreaCodeAttribute.FrenchGlobalName), () => Afghanistan.FrenchGlobalName, UnM49FrenchWorld },
            { nameof(UnM49AreaCodeAttribute.EnglishRegionName), () => Afghanistan.EnglishRegionName, UnM49EnglishAsia },
            { nameof(UnM49AreaCodeAttribute.FrenchRegionName), () => Afghanistan.FrenchRegionName, UnM49FrenchAsia },
            { nameof(UnM49AreaCodeAttribute.EnglishSubRegionName), () => Afghanistan.EnglishSubRegionName, UnM49EnglishSouthernAsia },
            { nameof(UnM49AreaCodeAttribute.FrenchSubRegionName), () => Afghanistan.FrenchSubRegionName, UnM49FrenchSouthernAsia },
            { nameof(UnM49AreaCodeAttribute.EnglishIntermediateRegionName), () => SouthAmerica.EnglishIntermediateRegionName, UnM49EnglishSouthAmerica },
            { nameof(UnM49AreaCodeAttribute.FrenchIntermediateRegionName), () => SouthAmerica.FrenchIntermediateRegionName, UnM49FrenchSouthAmerica },
            { nameof(UnM49AreaCodeAttribute.EnglishCountryOrAreaName), () => Afghanistan.EnglishCountryOrAreaName, UnM49EnglishAfghanistan },
            { nameof(UnM49AreaCodeAttribute.FrenchCountryOrAreaName), () => Afghanistan.FrenchCountryOrAreaName, UnM49FrenchAfghanistan },
        };
    }

    #endregion

    #region Test: EnglishName_WithLevelNamesSetLowestFirst

    [Fact]
    public void EnglishName_WithLevelNamesSetLowestFirst_IsTheLowestLevelsName()
    {
        // act
        var testResult = new UnM49AreaCodeAttribute(UnM49CodeWorld, UnM49CodeAfrica) { EnglishRegionName = UnM49EnglishAfrica, EnglishGlobalName = UnM49EnglishWorld }.EnglishName;

        // assert
        testResult.Should().Be(UnM49EnglishAfrica);
    }

    #endregion

    #region Test: EnglishName_WithLowestLevelNameCleared

    [Fact]
    public void EnglishName_WithLowestLevelNameCleared_IsTheNextLevelsName()
    {
        // arrange
        var attribute = new UnM49AreaCodeAttribute(UnM49CodeWorld, UnM49CodeAfrica) { EnglishGlobalName = UnM49EnglishWorld, EnglishRegionName = UnM49EnglishAfrica };

        // act
        attribute.EnglishRegionName = null;
        var testResult = attribute.EnglishName;

        // assert
        testResult.Should().Be(UnM49EnglishWorld);
    }

    #endregion

    #region Test: EnglishNames_WithNoEnglishNameSet

    [Fact]
    public void EnglishNames_WithNoEnglishNameSet_IsOneEmptyString()
    {
        // act
        var testResult = new UnM49AreaCodeAttribute(UnM49CodeWorld).EnglishNames;

        // assert
        testResult.Should().Equal(string.Empty);
    }

    #endregion
}
