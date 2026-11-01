using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class CurrencyFormatInfoTests
{
    private const string NewZealandCulture = "en-NZ";
    private const string GermanCulture = "de-DE";

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
        testResult.CurrencyGroupSizes.Should().Equal(3);
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
        _ = CurrencyFormatInfo.CurrentInfo; // the information for the first culture is cached here
        using var germanCultureScope = new CurrentCultureScope(new CultureInfo(GermanCulture));

        // act
        var testResult = CurrencyFormatInfo.CurrentInfo;

        // assert
        testResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.EUR), "the information must track a change of culture");
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
