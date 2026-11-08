using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Geography.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso3166ExtensionTests
{
    // ISO 639-1 two-letter language codes.
    private const string Iso639Part1Arabic = "ar";
    private const string Iso639Part1English = "en";
    private const string Iso639Part1German = "de";
    private const string Iso639Part1Greek = "el";
    private const string Iso639Part1French = "fr";
    private const string Iso639Part1Italian = "it";
    private const string Iso639Part1Khmer = "km";
    private const string Iso639Part1Latin = "la";
    private const string Iso639Part1Russian = "ru";
    private const string Iso639Part1Spanish = "es";

    // ISO 639-2 three-letter language codes.
    private const string Iso639Part2Arabic = "ara";
    private const string Iso639Part2Belarusian = "bel";
    private const string Iso639Part2English = "eng";
    private const string Iso639Part2French = "fra";
    private const string Iso639Part2Italian = "ita";
    private const string Iso639Part2Spanish = "spa";
    private const string Iso639Part2Turkish = "tur";

    // ISO 3166-1 country names.
    private const string BoliviaShortNameEnglish = "Bolivia (Plurinational State of)";
    private const string BoliviaShortUpperNameEnglish = "BOLIVIA, PLURINATIONAL STATE OF";
    private const string BoliviaFullNameEnglish = "the Plurinational State of Bolivia";
    private const string BoliviaShortNameFrench = "Bolivie (État plurinational de)";
    private const string BoliviaShortUpperNameFrench = "BOLIVIE, ÉTAT PLURINATIONAL DE";
    private const string BoliviaFullNameFrench = "l'État plurinational de Bolivie";
    private const string GermanyShortNameEnglish = "Germany";
    private const string GermanyShortUpperNameEnglish = "GERMANY";
    private const string GermanyFullNameEnglish = "the Federal Republic of Germany";
    private const string ItalyShortNameEnglish = "Italy";
    private const string ItalyShortUpperNameEnglish = "ITALY";
    private const string ItalyFullNameEnglish = "the Republic of Italy";
    private const string ItalyShortNameFrench = "Italie (l')";
    private const string ItalyShortUpperNameFrench = "ITALIE";
    private const string ItalyFullNameFrench = "la République italienne";

    // ISO 3166-2 subdivision category identifiers.
    private const ushort Iso3166Part2ProvinceCategoryId = 107;
    private const ushort Iso3166Part2CityCategoryId = 182;
    private const ushort Iso3166Part2KyrgyzstanCityCategoryId = 187;
    private const ushort Iso3166Part2DistrictCategoryId = 297;

    // ISO 3166-2 subdivision category names.
    private const string CyprusDistrictCategoryNameGreek = "eparchia";
    private const string CyprusDistrictCategoryNameEnglish = "district";
    private const string CyprusDistrictCategoryNameFrench = "district";
    private const string CyprusDistrictCategoryNameTurkish = "kaza";
    private const string FranceMetropolitanRegionCategoryNamePluralEnglish = "metropolitan regions";

    // ISO 3166-2 subdivision names and the romanization systems they are given in.
    private const string NorthwestTerritoriesNameEnglish = "Northwest Territories";
    private const string NorthwestTerritoriesNameFrench = "Territoires du Nord-Ouest";
    private const string BgnPcgn1956RomanizationSystem = "BGN/PCGN 1956";
    private const string ConventionalNamesRomanizationSystem = "conventional names";
    private const string AnjouanNameArabicBgnPcgn1956 = "Anjwān";
    private const string AnjouanNameArabicConventional = "Andjouân";
    private const string LibertadorGeneralBernardoOHigginsLocalVariantSpanish = "O'Higgins";
    private const string BgnPcgn1947RomanizationSystem = "BGN/PCGN 1947";
    private const string BgnPcgn1972RomanizationSystem = "BGN/PCGN 1972";
    private const string BgnPcgn1979RomanizationSystem = "BGN/PCGN 1979";
    private const string Gost1983RomanizationSystem = "GOST 1983 = UN V/18 1987";
    private const string NationalDraft1997RomanizationSystem = "National draft 1997";
    private const string UnX6Of2012RomanizationSystem = "UN X/6 2012";
    private const string OshNameRussianBgnPcgn1947 = "Gorod Osh";
    private const string OshNameRussianGost1983 = "Gorod Oš";
    private const string BishkekNameRussianBgnPcgn1947 = "Gorod Bishkek";
    private const string BishkekNameRussianGost1983 = "Gorod Biškek";
    private const string MinskNameBelarusian = "Horad Minsk";
    private const string MinskNameRussian = "Gorod Minsk";
    private const string SiemReapLocalVariantKhmerNationalDraft1997 = "Siem Reap";

    // ISO 3166-2 subdivision codes.
    private const string GreeceEastMacedoniaAndThraceSubdivisionCode = "GR-A";
    private const string GreeceAgionOrosSubdivisionCode = "GR-69";
    private const string EnglandSubdivisionCode = "GB-ENG";
    private const string NorthernIrelandSubdivisionCode = "GB-NIR";
    private const string ScotlandSubdivisionCode = "GB-SCT";
    private const string WalesSubdivisionCode = "GB-WLS";
    #region Test: GetEnglishName_OnIso3166Part1Alpha2CountryCode

    [Theory]
    [MemberData(nameof(GetEnglishName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator))]
    public void GetEnglishName_OnIso3166Part1Alpha2CountryCode_ReturnsEnglishName(Iso3166Part1Alpha2Country testCode, Iso3166CountryName nameType, string expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishName(nameType);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetEnglishName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso3166CountryName.Short, BoliviaShortNameEnglish };
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso3166CountryName.ShortUpper, BoliviaShortUpperNameEnglish };
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso3166CountryName.Full, BoliviaFullNameEnglish };
            }
        }
    }

    #endregion

    #region Test: GetNativeName_OnIso3166Part1Alpha2CountryCode

    [Theory]
    [MemberData(nameof(GetNativeName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetNativeName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator))]
    public void GetNativeName_OnIso3166Part1Alpha2CountryCode_ReturnsNativeName(Iso3166Part1Alpha2Country testCode, string languageCode, Iso3166CountryName nameType, string? expectedResult)
    {
        // act
        var testResult = testCode.GetNativeName(languageCode, nameType);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetNativeName_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso639Part1English, Iso3166CountryName.Short, BoliviaShortNameEnglish };
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso639Part1English, Iso3166CountryName.ShortUpper, BoliviaShortUpperNameEnglish };
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso639Part1English, Iso3166CountryName.Full, BoliviaFullNameEnglish };
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso639Part2French, Iso3166CountryName.Short, BoliviaShortNameFrench };
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso639Part2French, Iso3166CountryName.ShortUpper, BoliviaShortUpperNameFrench };
                yield return new object[] { Iso3166Part1Alpha2Country.BO, Iso639Part2French, Iso3166CountryName.Full, BoliviaFullNameFrench };
                yield return new object?[] { Iso3166Part1Alpha2Country.BO, Iso639Part1Spanish, Iso3166CountryName.Full, null };
                yield return new object?[] { Iso3166Part1Alpha2Country.BO, Iso639Part2Spanish, Iso3166CountryName.Full, null };
            }
        }
    }

    #endregion

    #region Test: IsIndependent_OnIso3166Part1Alpha2CountryCode

    [Theory]
    [MemberData(nameof(IsIndependent_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(IsIndependent_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator))]
    public void IsIndependent_OnIso3166Part1Alpha2CountryCode_ReturnsIndependenceFlag(Iso3166Part1Alpha2Country testCode, bool expectedResult)
    {
        // act
        var testResult = testCode.IsIndependent();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class IsIndependent_OnIso3166Part1Alpha2CountryCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part1Alpha2Country.AR, true };
                yield return new object[] { Iso3166Part1Alpha2Country.AS, false };
            }
        }
    }

    #endregion

    #region Test: GetEnglishName_OnIso3166Part1Alpha3CountryCode

    [Theory]
    [MemberData(nameof(GetEnglishName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator))]
    public void GetEnglishName_OnIso3166Part1Alpha3CountryCode_ReturnsEnglishName(Iso3166Part1Alpha3Country testCode, Iso3166CountryName nameType, string expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishName(nameType);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetEnglishName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part1Alpha3Country.DEU, Iso3166CountryName.Short, GermanyShortNameEnglish };
                yield return new object[] { Iso3166Part1Alpha3Country.DEU, Iso3166CountryName.ShortUpper, GermanyShortUpperNameEnglish };
                yield return new object[] { Iso3166Part1Alpha3Country.DEU, Iso3166CountryName.Full, GermanyFullNameEnglish };
            }
        }
    }

    #endregion

    #region Test: GetNativeName_OnIso3166Part1Alpha3CountryCode

    [Theory]
    [MemberData(nameof(GetNativeName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(GetNativeName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator))]
    public void GetNativeName_OnIso3166Part1Alpha3CountryCode_ReturnsNativeName(Iso3166Part1Alpha3Country testCode, string languageCode, Iso3166CountryName nameType, string? expectedResult)
    {
        // act
        var testResult = testCode.GetNativeName(languageCode, nameType);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetNativeName_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part1Alpha3Country.ITA, Iso639Part1English, Iso3166CountryName.Short, ItalyShortNameEnglish };
                yield return new object[] { Iso3166Part1Alpha3Country.ITA, Iso639Part1English, Iso3166CountryName.ShortUpper, ItalyShortUpperNameEnglish };
                yield return new object[] { Iso3166Part1Alpha3Country.ITA, Iso639Part1English, Iso3166CountryName.Full, ItalyFullNameEnglish };
                yield return new object[] { Iso3166Part1Alpha3Country.ITA, Iso639Part2French, Iso3166CountryName.Short, ItalyShortNameFrench };
                yield return new object[] { Iso3166Part1Alpha3Country.ITA, Iso639Part2French, Iso3166CountryName.ShortUpper, ItalyShortUpperNameFrench };
                yield return new object[] { Iso3166Part1Alpha3Country.ITA, Iso639Part2French, Iso3166CountryName.Full, ItalyFullNameFrench };
                yield return new object?[] { Iso3166Part1Alpha3Country.ITA, Iso639Part1Italian, Iso3166CountryName.Full, null };
                yield return new object?[] { Iso3166Part1Alpha3Country.ITA, Iso639Part2Italian, Iso3166CountryName.Full, null };
            }
        }
    }

    #endregion

    #region Test: IsIndependent_OnIso3166Part1Alpha3CountryCode

    [Theory]
    [MemberData(nameof(IsIndependent_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator.TestCases), MemberType = typeof(IsIndependent_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator))]
    public void IsIndependent_OnIso3166Part1Alpha3CountryCode_ReturnsIndependenceFlag(Iso3166Part1Alpha3Country testCode, bool expectedResult)
    {
        // act
        var testResult = testCode.IsIndependent();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class IsIndependent_OnIso3166Part1Alpha3CountryCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part1Alpha3Country.FRA, true };
                yield return new object[] { Iso3166Part1Alpha3Country.ATF, false };
            }
        }
    }

    #endregion

    #region Test: GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode

    [Theory]
    [MemberData(nameof(GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
    public void GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_ReturnsCategoryIdentifier(Iso3166Part2Subdivision testCode, ushort expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionCategoryIdentifier();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionCategoryIdentifier_OnIso3166Part2SubdivisionCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part2Subdivision.BZ._BZ, Iso3166Part2DistrictCategoryId };
                yield return new object[] { Iso3166Part2Subdivision.AR._C, Iso3166Part2CityCategoryId };
                yield return new object[] { Iso3166Part2Subdivision.AR._A, Iso3166Part2ProvinceCategoryId };
            }
        }
    }

    #endregion

    #region Test: GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode

    [Theory]
    [MemberData(nameof(GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
    public void GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_ReturnsCategoryName(Iso3166Part2Subdivision testCode, string languageCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionCategoryName(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionCategoryName_OnIso3166Part2SubdivisionCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part2Subdivision.CY._01, Iso639Part1Greek, CyprusDistrictCategoryNameGreek };
                yield return new object[] { Iso3166Part2Subdivision.CY._02, Iso639Part2English, CyprusDistrictCategoryNameEnglish };
                yield return new object[] { Iso3166Part2Subdivision.CY._03, Iso639Part1French, CyprusDistrictCategoryNameFrench };
                yield return new object[] { Iso3166Part2Subdivision.CY._04, Iso639Part2Turkish, CyprusDistrictCategoryNameTurkish };
                yield return new object?[] { Iso3166Part2Subdivision.CY._05, Iso639Part1German, null };
            }
        }
    }

    #endregion

    #region Test: GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode

    [Theory]
    [MemberData(nameof(GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
    public void GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_ReturnsCategoryNamePlural(Iso3166Part2Subdivision testCode, string languageCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionCategoryNamePlural(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionCategoryNamePlural_OnIso3166Part2SubdivisionCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part2Subdivision.FR._NOR, Iso639Part1English, FranceMetropolitanRegionCategoryNamePluralEnglish };
                yield return new object?[] { Iso3166Part2Subdivision.FR._NOR, Iso639Part2French, null };
                yield return new object?[] { Iso3166Part2Subdivision.FR._NOR, Iso639Part1Arabic, null };
            }
        }
    }

    #endregion

    #region Test: GetSubdivisionCode_OnIso3166Part2SubdivisionCode

    [Theory]
    [MemberData(nameof(GetSubdivisionCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
    public void GetSubdivisionCode_OnIso3166Part2SubdivisionCode_ReturnsSubdivisionCode(Iso3166Part2Subdivision testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part2Subdivision.GR._69, GreeceAgionOrosSubdivisionCode };
                yield return new object[] { Iso3166Part2Subdivision.GR._A, GreeceEastMacedoniaAndThraceSubdivisionCode };
            }
        }
    }

    #endregion

    #region Test: GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode

    [Theory]
    [MemberData(nameof(GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
    public void GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_ReturnsNativeName(Iso3166Part2Subdivision testCode, string languageCode, string? romanizationSystem, string? expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionNativeName(languageCode, romanizationSystem: romanizationSystem);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionNativeName_OnIso3166Part2SubdivisionCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object?[] { Iso3166Part2Subdivision.CA._NT, Iso639Part1English, null, NorthwestTerritoriesNameEnglish };
                yield return new object?[] { Iso3166Part2Subdivision.CA._NT, Iso639Part2French, null, NorthwestTerritoriesNameFrench };
                yield return new object?[] { Iso3166Part2Subdivision.CA._NS, Iso639Part1Latin, null, null };

                yield return new object[] { Iso3166Part2Subdivision.KM._A, Iso639Part1Arabic, BgnPcgn1956RomanizationSystem, AnjouanNameArabicBgnPcgn1956 };
                yield return new object[] { Iso3166Part2Subdivision.KM._A, Iso639Part2Arabic, ConventionalNamesRomanizationSystem, AnjouanNameArabicConventional };

                // Several names in one language: without a romanization system, the name ISO 3166-2 lists first.
                yield return new object?[] { Iso3166Part2Subdivision.KG._GO, Iso639Part1Russian, null, OshNameRussianBgnPcgn1947 };
                yield return new object?[] { Iso3166Part2Subdivision.KG._GB, Iso639Part1Russian, null, BishkekNameRussianGost1983 };
                yield return new object?[] { Iso3166Part2Subdivision.BY._HM, Iso639Part2Belarusian, null, MinskNameBelarusian };
                yield return new object?[] { Iso3166Part2Subdivision.BY._HM, Iso639Part1Russian, null, MinskNameRussian };

                // Several names in one language: each romanization system named explicitly.
                yield return new object[] { Iso3166Part2Subdivision.KG._GO, Iso639Part1Russian, BgnPcgn1947RomanizationSystem, OshNameRussianBgnPcgn1947 };
                yield return new object[] { Iso3166Part2Subdivision.KG._GO, Iso639Part1Russian, Gost1983RomanizationSystem, OshNameRussianGost1983 };
                yield return new object[] { Iso3166Part2Subdivision.KG._GB, Iso639Part1Russian, BgnPcgn1947RomanizationSystem, BishkekNameRussianBgnPcgn1947 };
                yield return new object[] { Iso3166Part2Subdivision.KG._GB, Iso639Part1Russian, Gost1983RomanizationSystem, BishkekNameRussianGost1983 };
                yield return new object[] { Iso3166Part2Subdivision.BY._HM, Iso639Part2Belarusian, UnX6Of2012RomanizationSystem, MinskNameBelarusian };
                yield return new object[] { Iso3166Part2Subdivision.BY._HM, Iso639Part2Belarusian, BgnPcgn1979RomanizationSystem, MinskNameBelarusian };
                yield return new object[] { Iso3166Part2Subdivision.BY._HM, Iso639Part1Russian, BgnPcgn1947RomanizationSystem, MinskNameRussian };
                yield return new object[] { Iso3166Part2Subdivision.BY._HM, Iso639Part1Russian, Gost1983RomanizationSystem, MinskNameRussian };
            }
        }
    }

    #endregion

    #region Test: GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode

    [Theory]
    [MemberData(nameof(GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
    public void GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_ReturnsNativeNameLocalVariant(Iso3166Part2Subdivision testCode, string languageCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionNativeNameLocalVariant(languageCode);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionNativeNameLocalVariant_OnIso3166Part2SubdivisionCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object[] { Iso3166Part2Subdivision.CL._LI, Iso639Part1Spanish, LibertadorGeneralBernardoOHigginsLocalVariantSpanish };
                yield return new object?[] { Iso3166Part2Subdivision.CL._RM, Iso639Part2Spanish, null };
                yield return new object?[] { Iso3166Part2Subdivision.CL._TA, Iso639Part1German, null };
            }
        }
    }

    [Theory]
    [MemberData(nameof(GetSubdivisionNativeNameLocalVariant_SeveralNamesInLanguage_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionNativeNameLocalVariant_SeveralNamesInLanguage_TestCaseGenerator))]
    public void GetSubdivisionNativeNameLocalVariant_SeveralNamesInLanguage_ReturnsLocalVariantOfSelectedName(Iso3166Part2Subdivision testCode, string languageCode, string? romanizationSystem, string? expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionNativeNameLocalVariant(languageCode, romanizationSystem: romanizationSystem);

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionNativeNameLocalVariant_SeveralNamesInLanguage_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object?[] { Iso3166Part2Subdivision.KH._17, Iso639Part1Khmer, null, SiemReapLocalVariantKhmerNationalDraft1997 };
                yield return new object[] { Iso3166Part2Subdivision.KH._17, Iso639Part1Khmer, NationalDraft1997RomanizationSystem, SiemReapLocalVariantKhmerNationalDraft1997 };
                yield return new object?[] { Iso3166Part2Subdivision.KH._17, Iso639Part1Khmer, BgnPcgn1972RomanizationSystem, null };
            }
        }
    }

    #endregion

    #region Test: SelectSubdivisionNameAttribute

    [Fact]
    public void SelectSubdivisionNameAttribute_SeveralNamesInReverseSequence_ReturnsLowestSequence()
    {
        // arrange
        var testAttributes = new[]
        {
            new Iso3166SubdivisionNameAttribute(Iso639Part1Russian, "rus", Iso3166Part2KyrgyzstanCityCategoryId, BishkekNameRussianBgnPcgn1947, 1) { RomanizationSystem = BgnPcgn1947RomanizationSystem },
            new Iso3166SubdivisionNameAttribute(Iso639Part1Russian, "rus", Iso3166Part2KyrgyzstanCityCategoryId, BishkekNameRussianGost1983, 0) { RomanizationSystem = Gost1983RomanizationSystem },
        };

        // act
        var testResult = Iso3166Extensions.SelectSubdivisionNameAttribute(testAttributes, Iso3166Part2KyrgyzstanCityCategoryId, Iso639Part1Russian, null);

        // assert
        testResult!.SubdivisionName.Should().Be(BishkekNameRussianGost1983);
    }

    #endregion

    #region Test: GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode

    [Theory]
    [MemberData(nameof(GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator.TestCases), MemberType = typeof(GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator))]
    public void GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_ReturnsParentCode(Iso3166Part2Subdivision testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetSubdivisionParentCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetSubdivisionParentCode_OnIso3166Part2SubdivisionCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object?[] { Iso3166Part2Subdivision.FI._11, null };
                yield return new object?[] { Iso3166Part2Subdivision.GB._ENG, null };
                yield return new object?[] { Iso3166Part2Subdivision.GB._NIR, null };
                yield return new object[] { Iso3166Part2Subdivision.GB._DEV, EnglandSubdivisionCode };
                yield return new object[] { Iso3166Part2Subdivision.GB._BFS, NorthernIrelandSubdivisionCode };
                yield return new object[] { Iso3166Part2Subdivision.GB._ANS, ScotlandSubdivisionCode };
                yield return new object[] { Iso3166Part2Subdivision.GB._CRF, WalesSubdivisionCode };
            }
        }
    }

    #endregion
}