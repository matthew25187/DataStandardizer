﻿using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso3166LanguageAttributeTests
{
    // ISO 639-1 two-letter language codes.
    private const string Iso639Part1Belarusian = "be";
    private const string Iso639Part1English = "en";
    private const string Iso639Part1French = "fr";
    private const string Iso639Part1Burmese = "my";
    private const string Iso639Part1Portuguese = "pt";
    private const string Iso639Part1Russian = "ru";

    // ISO 639-2/T three-letter language codes.
    private const string Iso639Part2TBelarusian = "bel";
    private const string Iso639Part2TEnglish = "eng";
    private const string Iso639Part2TFrench = "fra";
    private const string Iso639Part2TBurmese = "mya";
    private const string Iso639Part2TPortuguese = "por";
    private const string Iso639Part2TRussian = "rus";
    private const string Iso639Part2TTetum = "tet"; // ISO 3166-1 gives Tetum no ISO 639-1 code.

    #region Test: GetCustomAttributes_OnCountryThatReplacedAnotherCode

    [Theory]
    [MemberData(nameof(GetCustomAttributes_OnCountryThatReplacedAnotherCode_TestCaseGenerator.TestCases), MemberType = typeof(GetCustomAttributes_OnCountryThatReplacedAnotherCode_TestCaseGenerator))]
    public void GetCustomAttributes_OnCountryThatReplacedAnotherCode_HasOnlyTheCurrentCodesLanguageRows(string countryCode, Func<IEnumerable<Iso3166LanguageAttribute>> getLanguageAttributes, object[] expectedLanguages)
    {
        // act
        var testResult = getLanguageAttributes();

        // assert
        testResult.Should().BeEquivalentTo(expectedLanguages, countryCode);
    }

    private class GetCustomAttributes_OnCountryThatReplacedAnotherCode_TestCaseGenerator
    {
        // Each of these codes replaced another that kept its numeric code (BU/MM 104, BYS/BY 112, HV/BF 854, ZR/CD 180,
        // TP/TL 626). The language rows are those ISO 3166-1 gives for the officially assigned code only.
        private static readonly object[] BurkinaFasoLanguages =
        {
            new { Iso639Part1Code = Iso639Part1English, Iso639Part2TCode = Iso639Part2TEnglish, IsAdministrative = false },
            new { Iso639Part1Code = Iso639Part1French, Iso639Part2TCode = Iso639Part2TFrench, IsAdministrative = true },
        };

        private static readonly object[] BelarusLanguages =
        {
            new { Iso639Part1Code = Iso639Part1Belarusian, Iso639Part2TCode = Iso639Part2TBelarusian, IsAdministrative = true },
            new { Iso639Part1Code = Iso639Part1English, Iso639Part2TCode = Iso639Part2TEnglish, IsAdministrative = false },
            new { Iso639Part1Code = Iso639Part1French, Iso639Part2TCode = Iso639Part2TFrench, IsAdministrative = false },
            new { Iso639Part1Code = Iso639Part1Russian, Iso639Part2TCode = Iso639Part2TRussian, IsAdministrative = true },
        };

        private static readonly object[] DemocraticRepublicOfTheCongoLanguages =
        {
            new { Iso639Part1Code = Iso639Part1English, Iso639Part2TCode = Iso639Part2TEnglish, IsAdministrative = false },
            new { Iso639Part1Code = Iso639Part1French, Iso639Part2TCode = Iso639Part2TFrench, IsAdministrative = true },
        };

        private static readonly object[] MyanmarLanguages =
        {
            new { Iso639Part1Code = Iso639Part1English, Iso639Part2TCode = Iso639Part2TEnglish, IsAdministrative = false },
            new { Iso639Part1Code = Iso639Part1French, Iso639Part2TCode = Iso639Part2TFrench, IsAdministrative = false },
            new { Iso639Part1Code = Iso639Part1Burmese, Iso639Part2TCode = Iso639Part2TBurmese, IsAdministrative = true },
        };

        private static readonly object[] TimorLesteLanguages =
        {
            new { Iso639Part1Code = (string?)Iso639Part1English, Iso639Part2TCode = Iso639Part2TEnglish, IsAdministrative = false },
            new { Iso639Part1Code = (string?)Iso639Part1French, Iso639Part2TCode = Iso639Part2TFrench, IsAdministrative = false },
            new { Iso639Part1Code = (string?)Iso639Part1Portuguese, Iso639Part2TCode = Iso639Part2TPortuguese, IsAdministrative = true },
            new { Iso639Part1Code = (string?)null, Iso639Part2TCode = Iso639Part2TTetum, IsAdministrative = true },
        };

        public static TheoryData<string, Func<IEnumerable<Iso3166LanguageAttribute>>, object[]> TestCases => new TheoryData<string, Func<IEnumerable<Iso3166LanguageAttribute>>, object[]>
        {
            { nameof(Iso3166Part1Alpha2Country.BF), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.BF))!.GetCustomAttributes<Iso3166LanguageAttribute>(), BurkinaFasoLanguages },
            { nameof(Iso3166Part1Alpha2Country.BY), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.BY))!.GetCustomAttributes<Iso3166LanguageAttribute>(), BelarusLanguages },
            { nameof(Iso3166Part1Alpha2Country.CD), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.CD))!.GetCustomAttributes<Iso3166LanguageAttribute>(), DemocraticRepublicOfTheCongoLanguages },
            { nameof(Iso3166Part1Alpha2Country.MM), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.MM))!.GetCustomAttributes<Iso3166LanguageAttribute>(), MyanmarLanguages },
            { nameof(Iso3166Part1Alpha2Country.TL), () => typeof(Iso3166Part1Alpha2Country).GetField(nameof(Iso3166Part1Alpha2Country.TL))!.GetCustomAttributes<Iso3166LanguageAttribute>(), TimorLesteLanguages },
            { nameof(Iso3166Part1Alpha3Country.BFA), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.BFA))!.GetCustomAttributes<Iso3166LanguageAttribute>(), BurkinaFasoLanguages },
            { nameof(Iso3166Part1Alpha3Country.BLR), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.BLR))!.GetCustomAttributes<Iso3166LanguageAttribute>(), BelarusLanguages },
            { nameof(Iso3166Part1Alpha3Country.COD), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.COD))!.GetCustomAttributes<Iso3166LanguageAttribute>(), DemocraticRepublicOfTheCongoLanguages },
            { nameof(Iso3166Part1Alpha3Country.MMR), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.MMR))!.GetCustomAttributes<Iso3166LanguageAttribute>(), MyanmarLanguages },
            { nameof(Iso3166Part1Alpha3Country.TLS), () => typeof(Iso3166Part1Alpha3Country).GetField(nameof(Iso3166Part1Alpha3Country.TLS))!.GetCustomAttributes<Iso3166LanguageAttribute>(), TimorLesteLanguages },
        };
    }

    #endregion
}
