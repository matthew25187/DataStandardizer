using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class CurrencyFormatInfoTests
{
    private const string NewZealandCulture = "en-NZ";
    private const string GermanCulture = "de-DE";
    private const int FirstGroupSize = 3;
    private const int LaterGroupSize = 2;
    private const int ChangedGroupSize = 1;
    private const int InvariantGroupSize = 3;
    private const string SetterParameterName = "value";
    private static readonly int[] ExpectedGroupSizes = { FirstGroupSize, LaterGroupSize };

    #region Test: InvariantInfo

    [Fact]
    public void InvariantInfo_WhenRead_ReturnsCultureIndependentInformation()
    {
        // act
        var testResult = CurrencyFormatInfo.InvariantInfo;

        // assert
        testResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.XXX), "the invariant information denotes no currency");
        testResult.CurrencyDecimalSeparator.Should().Be(".");
        testResult.CurrencyGroupSeparator.Should().Be(",");
        testResult.CurrencyGroupSizes.Should().Equal(InvariantGroupSize);
        testResult.NegativeSign.Should().Be("-");
        testResult.IsReadOnly.Should().BeTrue("the shared instance must not be modifiable");
    }

    [Fact]
    public void InvariantInfo_WhenModified_ThrowsInvalidOperationException()
    {
        // act
        Action testAction = () => CurrencyFormatInfo.InvariantInfo.CurrencyCode = nameof(Iso4217CurrencyCurrent.NZD);

        // assert
        testAction.Should().Throw<InvalidOperationException>("the instance is read only");
    }

    #endregion

    #region Test: CurrencyGroupSizes

    [Fact]
    public void CurrencyGroupSizes_WhenReturnedArrayIsChanged_KeepsTheOriginalSizes()
    {
        // arrange
        var testInfo = new CurrencyFormatInfo { CurrencyGroupSizes = new[] { FirstGroupSize, LaterGroupSize } };
        var returnedSizes = testInfo.CurrencyGroupSizes;

        // act
        returnedSizes[0] = ChangedGroupSize;
        var testResult = testInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(ExpectedGroupSizes, "the getter must return a copy of the sizes");
    }

    [Fact]
    public void CurrencyGroupSizes_WhenArrayReturnedByInvariantInfoIsChanged_KeepsTheInvariantSizes()
    {
        // arrange
        var returnedSizes = CurrencyFormatInfo.InvariantInfo.CurrencyGroupSizes;

        // act
        returnedSizes[0] = ChangedGroupSize;
        var testResult = CurrencyFormatInfo.InvariantInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(new[] { InvariantGroupSize }, "a read-only instance must not be changed through the array it returns");
    }

    [Fact]
    public void CurrencyGroupSizes_WhenAssignedArrayIsChangedAfterwards_KeepsTheSizesAsSet()
    {
        // arrange
        var assignedSizes = new[] { FirstGroupSize, LaterGroupSize };
        var testInfo = new CurrencyFormatInfo { CurrencyGroupSizes = assignedSizes };

        // act
        assignedSizes[0] = ChangedGroupSize;
        var testResult = testInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(ExpectedGroupSizes, "the setter must store a copy of the sizes");
    }

    [Fact]
    public void CurrencyGroupSizes_WhenSetToNull_ThrowsArgumentNullException()
    {
        // arrange
        var testInfo = new CurrencyFormatInfo();

        // act
        Action testAction = () => testInfo.CurrencyGroupSizes = null!;

        // assert
        testAction.Should().Throw<ArgumentNullException>().WithParameterName(SetterParameterName);
    }

    [Fact]
    public void CurrencyGroupSizes_WhenReadOnlyInstanceIsSetToNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => CurrencyFormatInfo.InvariantInfo.CurrencyGroupSizes = null!;

        // assert
        testAction.Should().Throw<ArgumentNullException>("the argument is checked before the instance is, as NumberFormatInfo does").WithParameterName(SetterParameterName);
    }

    [Theory]
    [MemberData(nameof(InvalidGroupSizesTestCases))]
    public void CurrencyGroupSizes_WhenSetToInvalidSizes_ThrowsArgumentException(int[] testSizes)
    {
        // arrange
        var testInfo = new CurrencyFormatInfo();

        // act
        Action testAction = () => testInfo.CurrencyGroupSizes = testSizes;

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName(SetterParameterName);
    }

    [Theory]
    [MemberData(nameof(ValidGroupSizesTestCases))]
    public void CurrencyGroupSizes_WhenSetToValidSizes_StoresTheSizes(int[] testSizes, int[] expectedSizes)
    {
        // arrange
        var testInfo = new CurrencyFormatInfo();

        // act
        testInfo.CurrencyGroupSizes = testSizes;
        var testResult = testInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(expectedSizes);
    }

    // The rule NumberFormatInfo.NumberGroupSizes applies: each size is from 1 to 9, and only the last may be 0.
    public static TheoryData<int[]> InvalidGroupSizesTestCases => new()
    {
        new[] { 0, 3 },
        new[] { -1 },
        new[] { 3, -1 },
        new[] { 10 },
        new[] { 3, 10 }
    };

    public static TheoryData<int[], int[]> ValidGroupSizesTestCases => new()
    {
        { new int[0], new int[0] },
        { new[] { 1 }, new[] { 1 } },
        { new[] { 9 }, new[] { 9 } },
        { new[] { 0 }, new[] { 0 } },
        { new[] { 3, 0 }, new[] { 3, 0 } }
    };

    #endregion

    #region Test: CreateForCulture

    [Theory]
    [InlineData(NewZealandCulture, nameof(Iso4217CurrencyCurrent.NZD), ".", ",")]
    [InlineData(GermanCulture, nameof(Iso4217CurrencyCurrent.EUR), ",", ".")]
    [InlineData("pt-BR", nameof(Iso4217CurrencyCurrent.BRL), ",", ".")]
    [InlineData("ja-JP", nameof(Iso4217CurrencyCurrent.JPY), ".", ",")]
    public void CreateForCulture_ForCultureWithResources_ReturnsInformationForThatCulture(string testCulture, string expectedCurrencyCode, string expectedDecimalSeparator, string expectedGroupSeparator)
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo(testCulture));

        // assert
        testResult.CurrencyCode.Should().Be(expectedCurrencyCode);
        testResult.CurrencyDecimalSeparator.Should().Be(expectedDecimalSeparator);
        testResult.CurrencyGroupSeparator.Should().Be(expectedGroupSeparator);
    }

    [Fact]
    public void CreateForCulture_ForIndianCulture_ReturnsVariableGroupSizes()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo("en-IN"));

        // assert
        testResult.CurrencyGroupSizes.Should().Equal(new[] { 3, 2 }, "the Indian numbering system groups the leading digits in pairs");
    }

    [Fact]
    public void CreateForCulture_ForCultureWithNonAsciiGroupSeparator_PreservesTheSeparator()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo("ru-RU"));

        // assert
        testResult.CurrencyGroupSeparator.Should().Be("\x00A0", "the group separator is a no-break space");
    }

    [Fact]
    public void CreateForCulture_ForNullCulture_ReturnsCultureIndependentInformation()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(null);

        // assert
        testResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.XXX), "the neutral resources denote no currency");
    }

    [Fact]
    public void CreateForCulture_ForCultureWithoutResources_FallsBackToNeutralResources()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo("cy-GB"));

        // assert
        testResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.XXX), "no resources are defined for this culture");
    }

    [Fact]
    public void CreateForCulture_ForCultureWithResources_ReturnsAWritableInstance()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo(NewZealandCulture));

        // assert
        testResult.IsReadOnly.Should().BeFalse("sealing the instance is the responsibility of whatever takes ownership of it");
    }

    #endregion

    #region Test: CurrentInfo

    [Fact]
    public void CurrentInfo_AfterCurrentCultureChanges_ReflectsTheNewCulture()
    {
        // arrange
        using var newZealandCultureScope = new CurrentCultureScope(new CultureInfo(NewZealandCulture));

        // act
        var newZealandResult = CurrencyFormatInfo.CurrentInfo;
        using var germanCultureScope = new CurrentCultureScope(new CultureInfo(GermanCulture));
        var germanResult = CurrencyFormatInfo.CurrentInfo;

        // assert
        newZealandResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.NZD), "the information reflects the culture in effect when it is read");
        germanResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.EUR), "the information must track a change of culture");
    }

    [Fact]
    public void CurrentInfo_WhenRead_ReturnsAReadOnlyInstance()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(new CultureInfo(NewZealandCulture));

        // act
        var testResult = CurrencyFormatInfo.CurrentInfo;

        // assert
        testResult.IsReadOnly.Should().BeTrue("the instance is shared between callers");
    }

    #endregion
}
