using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso3166LanguageAttributeTests
{
    // ISO 639-2/T code for French, an administrative language of Burkina Faso and of DR Congo in ISO 3166-1.
    private const string Iso639Part2TFrench = "fra";

    #region Test: IsAdministrative_OnCountryThatReplacedAnotherCode

    [Theory]
    [MemberData(nameof(IsAdministrative_OnCountryThatReplacedAnotherCode_TestCaseGenerator.TestCases), MemberType = typeof(IsAdministrative_OnCountryThatReplacedAnotherCode_TestCaseGenerator))]
    public void IsAdministrative_OnCountryThatReplacedAnotherCode_HasOnlyTheCurrentCodesLanguageRow(string countryCode, Func<IEnumerable<Iso3166LanguageAttribute>> getLanguageAttributes)
    {
        // act
        var testResult = getLanguageAttributes();

        // assert
        testResult.Should().ContainSingle(attribute => attribute.Iso639Part2TCode == Iso639Part2TFrench, countryCode)
            .Which.IsAdministrative.Should().BeTrue(countryCode);
    }

    private class IsAdministrative_OnCountryThatReplacedAnotherCode_TestCaseGenerator
    {
        // BF replaced HV and CD replaced ZR, keeping their numeric codes (854 and 180). Only the current code's rows apply.
        public static TheoryData<string, Func<IEnumerable<Iso3166LanguageAttribute>>> TestCases => new TheoryData<string, Func<IEnumerable<Iso3166LanguageAttribute>>>
        {
            { nameof(Iso3166Part1Alpha2Country.BF), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.BF))!.GetCustomAttributes<Iso3166LanguageAttribute>() },
            { nameof(Iso3166Part1Alpha2Country.CD), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.CD))!.GetCustomAttributes<Iso3166LanguageAttribute>() },
            { nameof(Iso3166Part1Alpha3Country.BFA), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.BFA))!.GetCustomAttributes<Iso3166LanguageAttribute>() },
            { nameof(Iso3166Part1Alpha3Country.COD), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.COD))!.GetCustomAttributes<Iso3166LanguageAttribute>() },
        };
    }

    #endregion
}
