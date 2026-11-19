using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso3166CountryCodeAttributeTests
{
    // Short names of France from ISO 3166-1.
    private const string Iso3166EnglishShortNameFrance = "France";
    private const string Iso3166FrenchShortNameFrance = "France (la)";
    private const string Iso3166EnglishShortNameUpperFrance = "FRANCE";
    private const string Iso3166EnglishFullNameFrance = "the French Republic";

    #region Test: EnglishNames_OnCountry

    [Theory]
    [MemberData(nameof(FranceTestCaseGenerator.TestCases), MemberType = typeof(FranceTestCaseGenerator))]
    public void EnglishNames_OnCountry_HoldsOnlyTheEnglishShortName(string countryCode, Func<Iso3166CountryCodeAttribute> getAttribute)
    {
        // act
        var testResult = getAttribute().EnglishNames;

        // assert
        testResult.Should().Equal(new[] { Iso3166EnglishShortNameFrance }, countryCode);
    }

    #endregion

    #region Test: FrenchName_OnCountry

    [Theory]
    [MemberData(nameof(FranceTestCaseGenerator.TestCases), MemberType = typeof(FranceTestCaseGenerator))]
    public void FrenchName_OnCountry_IsTheFrenchShortName(string countryCode, Func<Iso3166CountryCodeAttribute> getAttribute)
    {
        // act
        var testResult = getAttribute().FrenchName;

        // assert
        testResult.Should().Be(Iso3166FrenchShortNameFrance, countryCode);
    }

    #endregion

    #region Test: FrenchNames_OnCountry

    [Theory]
    [MemberData(nameof(FranceTestCaseGenerator.TestCases), MemberType = typeof(FranceTestCaseGenerator))]
    public void FrenchNames_OnCountry_HoldsOnlyTheFrenchShortName(string countryCode, Func<Iso3166CountryCodeAttribute> getAttribute)
    {
        // act
        var testResult = getAttribute().FrenchNames;

        // assert
        testResult.Should().Equal(new[] { Iso3166FrenchShortNameFrance }, countryCode);
    }

    #endregion

    #region Test: EnglishShortNameUpper_OnCountry

    [Theory]
    [MemberData(nameof(FranceTestCaseGenerator.TestCases), MemberType = typeof(FranceTestCaseGenerator))]
    public void EnglishShortNameUpper_OnCountry_IsTheUpperCaseShortName(string countryCode, Func<Iso3166CountryCodeAttribute> getAttribute)
    {
        // act
        var testResult = getAttribute().EnglishShortNameUpper;

        // assert
        testResult.Should().Be(Iso3166EnglishShortNameUpperFrance, countryCode);
    }

    #endregion

    #region Test: EnglishFullName_OnCountry

    [Theory]
    [MemberData(nameof(FranceTestCaseGenerator.TestCases), MemberType = typeof(FranceTestCaseGenerator))]
    public void EnglishFullName_OnCountry_IsTheFullName(string countryCode, Func<Iso3166CountryCodeAttribute> getAttribute)
    {
        // act
        var testResult = getAttribute().EnglishFullName;

        // assert
        testResult.Should().Be(Iso3166EnglishFullNameFrance, countryCode);
    }

    #endregion

    #region Test: EnglishFullName_OnCountryWithNoFullName

    [Fact]
    public void EnglishFullName_OnCountryWithNoFullName_IsNull()
    {
        // arrange
        var attribute = typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.AI))!.GetCustomAttribute<Iso3166CountryCodeAttribute>()!;

        // act
        var testResult = attribute.EnglishFullName;

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    private class FranceTestCaseGenerator
    {
        public static TheoryData<string, Func<Iso3166CountryCodeAttribute>> TestCases => new TheoryData<string, Func<Iso3166CountryCodeAttribute>>
        {
            { nameof(Iso3166Part1Alpha2Country.FR), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.FR))!.GetCustomAttribute<Iso3166CountryCodeAttribute>()! },
            { nameof(Iso3166Part1Alpha3Country.FRA), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.FRA))!.GetCustomAttribute<Iso3166CountryCodeAttribute>()! },
        };
    }
}
