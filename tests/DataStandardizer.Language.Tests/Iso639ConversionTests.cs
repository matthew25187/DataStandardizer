using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Language.Tests;

/// <summary>
/// Tests of the explicit conversions from <see cref="string"/> to the ISO 639 string enumerations.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Iso639ConversionTests
{
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
}
