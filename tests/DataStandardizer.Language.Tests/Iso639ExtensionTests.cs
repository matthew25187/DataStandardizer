using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Language.Tests;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso639ExtensionTests
{
    // ISO 639-1, ISO 639-2 and ISO 639-5 codes, and the English and French names of each language
    // and language family, from the code lists of the ISO 639-2 Registration Authority (Library of
    // Congress): https://www.loc.gov/standards/iso639-2/php/code_list.php and
    // https://www.loc.gov/standards/iso639-5/id.php
    // ISO 639-3 names and macrolanguage mappings from the code tables of the ISO 639-3 Registration
    // Authority (SIL International): https://iso639-3.sil.org/code_tables/download_tables

    // ISO 639-2 eng (ISO 639-1 en): English name; also the ISO 639-3 reference name of eng.
    private const string EnglishEnglishName = "English";

    // ISO 639-2 cat (ISO 639-1 ca): English names.
    private const string CatalanEnglishName = "Catalan";
    private const string CatalanAlternativeEnglishName = "Valencian";

    // ISO 639-5 gem: English name.
    private const string GermanicLanguagesEnglishName = "Germanic languages";

    // ISO 639-2 fre/fra (ISO 639-1 fr): French name.
    private const string FrenchFrenchName = "français";

    // ISO 639-5 ine: French name.
    private const string IndoEuropeanLanguagesFrenchName = "indo-européennes, langues";

    // ISO 639-2 gla (ISO 639-1 gd): French names.
    private const string ScottishGaelicFrenchName = "gaélique";
    private const string ScottishGaelicAlternativeFrenchName = "gaélique écossais";

    // ISO 639-2 gsw: French names.
    private const string SwissGermanFrenchName = "suisse alémanique";
    private const string SwissGermanAlemannicFrenchName = "alémanique";
    private const string SwissGermanAlsatianFrenchName = "alsacien";

    // ISO 639-2 nno: French names.
    private const string NorwegianNynorskFrenchName = "norvégien nynorsk";
    private const string NorwegianNynorskInvertedFrenchName = "nynorsk, norvégien";

    // ISO 639-5 cel: French names.
    private const string CelticLanguagesFrenchName = "celtiques, langues";
    private const string CelticLanguagesAlternativeFrenchName = "celtes, langues";

    // ISO 639-3 rcf: inverted name.
    private const string ReunionCreoleFrenchInvertedName = "Creole French, Réunion";

    // ISO 639-3 macrolanguage mappings: ind (Indonesian) is an individual language of msa (Malay).
    private const string IndonesianMacrolanguageCode = "msa";

    // ISO 639-2 arm/hye: ISO 639-1 code of Armenian.
    private const string ArmenianPart1Code = "hy";

    // ISO 639-3 mri: ISO 639-1 code of Māori.
    private const string MaoriPart1Code = "mi";

    // ISO 639-1 mi (ISO 639-3 mri): ISO 639-2/B code of Māori.
    private const string MaoriPart2BCode = "mao";

    // ISO 639-1 mi (ISO 639-3 mri): ISO 639-2/T code of Māori.
    private const string MaoriPart2TCode = "mri";

    // ISO 639-3 hun: print name.
    private const string HungarianPrintName = "Hungarian";

    #region Test: GetEnglishName_OnIso639Part1LanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishName_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part1LanguageCode_TestCaseGenerator))]
    public void GetEnglishName_OnIso639Part1LanguageCode_ReturnsEnglishName(Iso639Part1Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetEnglishName_OnIso639Part1LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part1Language.en, EnglishEnglishName }; }
        }
    }

    #endregion

    #region Test: GetEnglishName_OnIso639Part2BLanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishName_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part2BLanguageCode_TestCaseGenerator))]
    public void GetEnglishName_OnIso639Part2BLanguageCode_ReturnsEnglishName(Iso639Part2BLanguage testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetEnglishName_OnIso639Part2BLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2BLanguage.eng, EnglishEnglishName }; }
        }
    }

    #endregion

    #region Test: GetEnglishName_OnIso639Part2TLanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishName_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part2TLanguageCode_TestCaseGenerator))]
    public void GetEnglishName_OnIso639Part2TLanguageCode_ReturnsEnglishName(Iso639Part2TLanguage testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetEnglishName_OnIso639Part2TLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2TLanguage.eng, EnglishEnglishName }; }
        }
    }

    #endregion

    #region Test: GetEnglishName_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishName_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetEnglishName_OnIso639Part3LanguageCode_ReturnsEnglishName(Iso639Part3Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetEnglishName_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part3Language.eng, EnglishEnglishName }; }
        }
    }

    #endregion

    #region Test: GetEnglishName_OnIso639Part5LanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishName_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishName_OnIso639Part5LanguageCode_TestCaseGenerator))]
    public void GetEnglishName_OnIso639Part5LanguageCode_ReturnsEnglishName(Iso639Part5LanguageFamily testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetEnglishName_OnIso639Part5LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part5LanguageFamily.gem, GermanicLanguagesEnglishName }; }
        }
    }

    #endregion

    #region Test: GetEnglishNames_OnIso639Part1LanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishNames_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part1LanguageCode_TestCaseGenerator))]
    public void GetEnglishNames_OnIso639Part1LanguageCode_ReturnsEnglishNames(Iso639Part1Language testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetEnglishNames_OnIso639Part1LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part1Language.ca, new[] { CatalanEnglishName, CatalanAlternativeEnglishName } }; }
        }
    }

    #endregion

    #region Test: GetEnglishNames_OnIso639Part2BLanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishNames_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part2BLanguageCode_TestCaseGenerator))]
    public void GetEnglishNames_OnIso639Part2BLanguageCode_ReturnsEnglishNames(Iso639Part2BLanguage testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetEnglishNames_OnIso639Part2BLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2BLanguage.cat, new[] { CatalanEnglishName, CatalanAlternativeEnglishName } }; }
        }
    }

    #endregion

    #region Test: GetEnglishNames_OnIso639Part2TLanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishNames_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part2TLanguageCode_TestCaseGenerator))]
    public void GetEnglishNames_OnIso639Part2TLanguageCode_ReturnsEnglishNames(Iso639Part2TLanguage testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetEnglishNames_OnIso639Part2TLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2TLanguage.cat, new[] { CatalanEnglishName, CatalanAlternativeEnglishName } }; }
        }
    }

    #endregion

    #region Test: GetEnglishNames_OnIso639Part5LanguageCode

    [Theory]
    [MemberData(nameof(GetEnglishNames_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetEnglishNames_OnIso639Part5LanguageCode_TestCaseGenerator))]
    public void GetEnglishNames_OnIso639Part5LanguageCode_ReturnsEnglishNames(Iso639Part5LanguageFamily testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetEnglishNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetEnglishNames_OnIso639Part5LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part5LanguageFamily.gem, new[] { GermanicLanguagesEnglishName } }; }
        }
    }

    #endregion

    #region Test: GetFrenchName_OnIso639Part1LanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchName_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part1LanguageCode_TestCaseGenerator))]
    public void GetFrenchName_OnIso639Part1LanguageCode_ReturnsFrenchName(Iso639Part1Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetFrenchName_OnIso639Part1LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part1Language.fr, FrenchFrenchName }; }
        }
    }

    #endregion

    #region Test: GetFrenchName_OnIso639Part2BLanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchName_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part2BLanguageCode_TestCaseGenerator))]
    public void GetFrenchName_OnIso639Part2BLanguageCode_ReturnsFrenchName(Iso639Part2BLanguage testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetFrenchName_OnIso639Part2BLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2BLanguage.fre, FrenchFrenchName }; }
        }
    }

    #endregion

    #region Test: GetFrenchName_OnIso639Part2TLanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchName_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part2TLanguageCode_TestCaseGenerator))]
    public void GetFrenchName_OnIso639Part2TLanguageCode_ReturnsFrenchName(Iso639Part2TLanguage testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetFrenchName_OnIso639Part2TLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2TLanguage.fra, FrenchFrenchName }; }
        }
    }

    #endregion

    #region Test: GetFrenchName_OnIso639Part5LanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchName_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchName_OnIso639Part5LanguageCode_TestCaseGenerator))]
    public void GetFrenchName_OnIso639Part5LanguageCode_ReturnsFrenchName(Iso639Part5LanguageFamily testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetFrenchName_OnIso639Part5LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part5LanguageFamily.ine, IndoEuropeanLanguagesFrenchName }; }
        }
    }

    #endregion

    #region Test: GetFrenchNames_OnIso639Part1LanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchNames_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part1LanguageCode_TestCaseGenerator))]
    public void GetFrenchNames_OnIso639Part1LanguageCode_ReturnsFrenchNames(Iso639Part1Language testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetFrenchNames_OnIso639Part1LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part1Language.gd, new[] { ScottishGaelicFrenchName, ScottishGaelicAlternativeFrenchName } }; }
        }
    }

    #endregion

    #region Test: GetFrenchNames_OnIso639Part2BLanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchNames_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part2BLanguageCode_TestCaseGenerator))]
    public void GetFrenchNames_OnIso639Part2BLanguageCode_ReturnsFrenchNames(Iso639Part2BLanguage testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetFrenchNames_OnIso639Part2BLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2BLanguage.gsw, new[] { SwissGermanFrenchName, SwissGermanAlemannicFrenchName, SwissGermanAlsatianFrenchName } }; }
        }
    }

    #endregion

    #region Test: GetFrenchNames_OnIso639Part2TLanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchNames_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part2TLanguageCode_TestCaseGenerator))]
    public void GetFrenchNames_OnIso639Part2TLanguageCode_ReturnsFrenchNames(Iso639Part2TLanguage testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetFrenchNames_OnIso639Part2TLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2TLanguage.nno, new[] { NorwegianNynorskFrenchName, NorwegianNynorskInvertedFrenchName } }; }
        }
    }

    #endregion

    #region Test: GetFrenchNames_OnIso639Part5LanguageCode

    [Theory]
    [MemberData(nameof(GetFrenchNames_OnIso639Part5LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetFrenchNames_OnIso639Part5LanguageCode_TestCaseGenerator))]
    public void GetFrenchNames_OnIso639Part5LanguageCode_ReturnsFrenchNames(Iso639Part5LanguageFamily testCode, string[] expectedResult)
    {
        // act
        var testResult = testCode.GetFrenchNames();

        // assert
        testResult.Should().BeEquivalentTo(expectedResult);
    }

    private class GetFrenchNames_OnIso639Part5LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part5LanguageFamily.cel, new[] { CelticLanguagesFrenchName, CelticLanguagesAlternativeFrenchName } }; }
        }
    }

    #endregion

    #region Test: GetInvertedName_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetInvertedName_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetInvertedName_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetInvertedName_OnIso639Part3LanguageCode_ReturnsInvertedName(Iso639Part3Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetInvertedName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetInvertedName_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part3Language.rcf, ReunionCreoleFrenchInvertedName }; }
        }
    }

    #endregion

    #region Test: GetLanguageType_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetLanguageType_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetLanguageType_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetLanguageType_OnIso639Part3LanguageCode_ReturnsLanguageType(Iso639Part3Language testCode, Iso639LanguageType expectedResult)
    {
        // act
        var testResult = testCode.GetLanguageType();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetLanguageType_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso639Part3Language.deu, Iso639LanguageType.Living };
                yield return new object[] { Iso639Part3Language.arc, Iso639LanguageType.Historical };
                yield return new object[] { Iso639Part3Language.chh, Iso639LanguageType.Extinct };
                yield return new object[] { Iso639Part3Language.epo, Iso639LanguageType.Constructed };
                yield return new object[] { Iso639Part3Language.und, Iso639LanguageType.Special };
            }
        }
    }

    #endregion

    #region Test: GetMacrolanguageCode_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetMacrolanguageCode_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetMacrolanguageCode_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetMacrolanguageCode_OnIso639Part3LanguageCode_ReturnsMacroLanguageCode(Iso639Part3Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetMacrolanguageCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetMacrolanguageCode_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part3Language.ind, IndonesianMacrolanguageCode }; }
        }
    }

    #endregion

    #region Test: GetPart1Code_OnIso639Part2BLanguageCode

    [Theory]
    [MemberData(nameof(GetPart1Code_OnIso639Part2BLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart1Code_OnIso639Part2BLanguageCode_TestCaseGenerator))]
    public void GetPart1Code_OnIso639Part2BLanguageCode_ReturnsPart1LanguageCode(Iso639Part2BLanguage testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPart1Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPart1Code_OnIso639Part2BLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2BLanguage.arm, ArmenianPart1Code }; }
        }
    }

    #endregion

    #region Test: GetPart1Code_OnIso639Part2TLanguageCode

    [Theory]
    [MemberData(nameof(GetPart1Code_OnIso639Part2TLanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart1Code_OnIso639Part2TLanguageCode_TestCaseGenerator))]
    public void GetPart1Code_OnIso639Part2TLanguageCode_ReturnsPart1LanguageCode(Iso639Part2TLanguage testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPart1Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPart1Code_OnIso639Part2TLanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part2TLanguage.hye, ArmenianPart1Code }; }
        }
    }

    #endregion

    #region Test: GetPart1Code_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetPart1Code_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart1Code_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetPart1Code_OnIso639Part3LanguageCode_ReturnsPart1LanguageCode(Iso639Part3Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPart1Code();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPart1Code_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part3Language.mri, MaoriPart1Code }; }
        }
    }

    #endregion

    #region Test: GetPart2BCode_OnIso639Part1LanguageCode

    [Theory]
    [MemberData(nameof(GetPart2BCode_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2BCode_OnIso639Part1LanguageCode_TestCaseGenerator))]
    public void GetPart2BCode_OnIso639Part1LanguageCode_ReturnsPart2BLanguageCode(Iso639Part1Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPart2BCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPart2BCode_OnIso639Part1LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part1Language.mi, MaoriPart2BCode }; }
        }
    }

    #endregion

    #region Test: GetPart2BCode_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetPart2BCode_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2BCode_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetPart2BCode_OnIso639Part3LanguageCode_ReturnsPart2BLanguageCode(Iso639Part3Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPart2BCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPart2BCode_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part3Language.mri, MaoriPart2BCode }; }
        }
    }

    #endregion

    #region Test: GetPart2TCode_OnIso639Part1LanguageCode

    [Theory]
    [MemberData(nameof(GetPart2TCode_OnIso639Part1LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2TCode_OnIso639Part1LanguageCode_TestCaseGenerator))]
    public void GetPart2TCode_OnIso639Part1LanguageCode_ReturnsPart2TLanguageCode(Iso639Part1Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPart2TCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPart2TCode_OnIso639Part1LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part1Language.mi, MaoriPart2TCode }; }
        }
    }

    #endregion

    #region Test: GetPart2TCode_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetPart2TCode_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPart2TCode_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetPart2TCode_OnIso639Part3LanguageCode_ReturnsPart2TLanguageCode(Iso639Part3Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPart2TCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPart2TCode_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part3Language.mri, MaoriPart2TCode }; }
        }
    }

    #endregion

    #region Test: GetPrintName_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetPrintName_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetPrintName_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetPrintName_OnIso639Part3LanguageCode_ReturnsPrintName(Iso639Part3Language testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetPrintName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetPrintName_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get { yield return new object[] { Iso639Part3Language.hun, HungarianPrintName }; }
        }
    }

    #endregion

    #region Test: GetScope_OnIso639Part3LanguageCode

    [Theory]
    [MemberData(nameof(GetScope_OnIso639Part3LanguageCode_TestCaseGenerator.TestCases), MemberType = typeof(GetScope_OnIso639Part3LanguageCode_TestCaseGenerator))]
    public void GetScope_OnIso639Part3LanguageCode_ReturnsScope(Iso639Part3Language testCode, Iso639LanguageScope expectedResult)
    {
        // act
        var testResult = testCode.GetScope();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetScope_OnIso639Part3LanguageCode_TestCaseGenerator
    {
        public static IEnumerable<object[]> TestCases
        {
            get
            {
                yield return new object[] { Iso639Part3Language.eng, Iso639LanguageScope.Individual };
                yield return new object[] { Iso639Part3Language.est, Iso639LanguageScope.Macrolanguage };
                yield return new object[] { Iso639Part3Language.und, Iso639LanguageScope.Special };
            }
        }
    }

    #endregion
}