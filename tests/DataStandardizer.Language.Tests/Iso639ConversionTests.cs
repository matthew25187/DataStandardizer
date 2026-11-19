using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Language.Tests;

/// <summary>
/// Tests of the explicit conversions from <see cref="string"/> to the ISO 639 string enumerations.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso639ConversionTests
{
    private const string EmptyCode = "";
    private const string NotATwoLetterCode = "xx";
    private const string NotAThreeLetterCode = "xxx";

    // ISO 639-1 and ISO 639-2 code for English.
    private const string EnglishTwoLetterCode = "en";
    private const string EnglishTwoLetterCodeUpperCase = "EN";
    private const string EnglishThreeLetterCode = "eng";
    private const string EnglishThreeLetterCodeUpperCase = "ENG";

    // ISO 639-2/B code for German; its ISO 639-2/T code is "deu".
    private const string GermanBibliographicCode = "ger";
    private const string GermanTerminologicCode = "deu";

    // ISO 639-5 code for Germanic languages.
    private const string GermanicLanguagesCode = "gem";
    private const string GermanicLanguagesCodeUpperCase = "GEM";

    #region Test: ExplicitOperator_NullString

    [Theory]
    [MemberData(nameof(ExplicitOperator_NullString_TestCaseGenerator.TestCases), MemberType = typeof(ExplicitOperator_NullString_TestCaseGenerator))]
    public void ExplicitOperator_NullString_ThrowsInvalidCastException(Action conversion)
    {
        // act
        var testAction = conversion;

        // assert
        testAction.Should().Throw<InvalidCastException>();
    }

    private class ExplicitOperator_NullString_TestCaseGenerator
    {
        public static TheoryData<Action> TestCases => new()
        {
            () => _ = (Iso639Part1Language)(string)null!,
            () => _ = (Iso639Part2BLanguage)(string)null!,
            () => _ = (Iso639Part2TLanguage)(string)null!,
            () => _ = (Iso639Part3Language)(string)null!,
            () => _ = (Iso639Part5LanguageFamily)(string)null!,
        };
    }

    #endregion

    #region Test: ExplicitOperator_NotACode

    [Theory]
    [MemberData(nameof(ExplicitOperator_NotACode_TestCaseGenerator.TestCases), MemberType = typeof(ExplicitOperator_NotACode_TestCaseGenerator))]
    public void ExplicitOperator_NotACode_ThrowsInvalidCastException(Action conversion)
    {
        // act
        var testAction = conversion;

        // assert
        testAction.Should().Throw<InvalidCastException>();
    }

    private class ExplicitOperator_NotACode_TestCaseGenerator
    {
        public static TheoryData<Action> TestCases => new()
        {
            () => _ = (Iso639Part1Language)NotATwoLetterCode,
            () => _ = (Iso639Part1Language)EmptyCode,
            () => _ = (Iso639Part1Language)EnglishTwoLetterCodeUpperCase,
            () => _ = (Iso639Part1Language)EnglishThreeLetterCode,
            () => _ = (Iso639Part2BLanguage)NotAThreeLetterCode,
            () => _ = (Iso639Part2BLanguage)EmptyCode,
            () => _ = (Iso639Part2BLanguage)EnglishThreeLetterCodeUpperCase,
            () => _ = (Iso639Part2BLanguage)GermanTerminologicCode,
            () => _ = (Iso639Part2TLanguage)NotAThreeLetterCode,
            () => _ = (Iso639Part2TLanguage)EmptyCode,
            () => _ = (Iso639Part2TLanguage)EnglishThreeLetterCodeUpperCase,
            () => _ = (Iso639Part2TLanguage)GermanBibliographicCode,
            () => _ = (Iso639Part3Language)NotAThreeLetterCode,
            () => _ = (Iso639Part3Language)EmptyCode,
            () => _ = (Iso639Part3Language)EnglishThreeLetterCodeUpperCase,
            () => _ = (Iso639Part5LanguageFamily)NotAThreeLetterCode,
            () => _ = (Iso639Part5LanguageFamily)EmptyCode,
            () => _ = (Iso639Part5LanguageFamily)GermanicLanguagesCodeUpperCase,
            () => _ = (Iso639Part5LanguageFamily)EnglishThreeLetterCode,
        };
    }

    #endregion

    #region Test: ExplicitOperator_Code

    [Theory]
    [MemberData(nameof(ExplicitOperator_Code_TestCaseGenerator.TestCases), MemberType = typeof(ExplicitOperator_Code_TestCaseGenerator))]
    public void ExplicitOperator_Code_ReturnsMemberWithCode(Func<object> conversion, object expectedResult)
    {
        // act
        var testResult = conversion();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class ExplicitOperator_Code_TestCaseGenerator
    {
        public static TheoryData<Func<object>, object> TestCases => new()
        {
            { () => (Iso639Part1Language)EnglishTwoLetterCode, Iso639Part1Language.en },
            { () => (Iso639Part2BLanguage)EnglishThreeLetterCode, Iso639Part2BLanguage.eng },
            { () => (Iso639Part2BLanguage)GermanBibliographicCode, Iso639Part2BLanguage.ger },
            { () => (Iso639Part2TLanguage)EnglishThreeLetterCode, Iso639Part2TLanguage.eng },
            { () => (Iso639Part2TLanguage)GermanTerminologicCode, Iso639Part2TLanguage.deu },
            { () => (Iso639Part3Language)EnglishThreeLetterCode, Iso639Part3Language.eng },
            { () => (Iso639Part5LanguageFamily)GermanicLanguagesCode, Iso639Part5LanguageFamily.gem },
        };
    }

    #endregion
}
