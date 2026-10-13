using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class CurrencyFormatInfoTests
{
    #region Test: InvariantInfo

    [Fact]
    public void InvariantInfo_ReturnsCultureIndependentInformation()
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
        var testAction = () => CurrencyFormatInfo.InvariantInfo.CurrencyCode = nameof(Iso4217CurrencyCurrent.NZD);

        // assert
        testAction.Should().Throw<InvalidOperationException>("the instance is read only");
    }

    #endregion

    #region Test: CreateForCulture

    [Theory]
    [InlineData("en-NZ", nameof(Iso4217CurrencyCurrent.NZD), ".", ",")]
    [InlineData("de-DE", nameof(Iso4217CurrencyCurrent.EUR), ",", ".")]
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
        testResult.CurrencyGroupSeparator.Should().Be(" ", "the group separator is a no-break space");
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
    public void CreateForCulture_ReturnsAWritableInstance()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo("en-NZ"));

        // assert
        testResult.IsReadOnly.Should().BeFalse("sealing the instance is the responsibility of whatever takes ownership of it");
    }

    #endregion

    #region Test: CurrentInfo

    [Fact]
    public void CurrentInfo_WhenCurrentCultureChanges_ReflectsTheNewCulture()
    {
        // arrange
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // act
            CultureInfo.CurrentCulture = new CultureInfo("en-NZ");
            var newZealandResult = CurrencyFormatInfo.CurrentInfo.CurrencyCode;

            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var germanResult = CurrencyFormatInfo.CurrentInfo.CurrencyCode;

            // assert
            newZealandResult.Should().Be(nameof(Iso4217CurrencyCurrent.NZD));
            germanResult.Should().Be(nameof(Iso4217CurrencyCurrent.EUR), "the information must track a change of culture");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void CurrentInfo_ReturnsAReadOnlyInstance()
    {
        // act
        var testResult = CurrencyFormatInfo.CurrentInfo;

        // assert
        testResult.IsReadOnly.Should().BeTrue("the instance is shared between callers");
    }

    #endregion
}
