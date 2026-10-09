using System.Globalization;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class MoneyInfoTests
{
    #region Test: GetFormat

    [Fact]
    public void GetFormat_ForCustomFormatter_ReturnsAFormatter()
    {
        // arrange
        var testSubject = MoneyInfo.InvariantMoney;

        // act
        var testResult = testSubject.GetFormat(typeof(ICustomFormatter));

        // assert
        testResult.Should().BeOfType<MoneyFormatter>("a monetary value is formatted by a custom formatter");
    }

    [Fact]
    public void GetFormat_ForCurrencyFormatInformation_ReturnsTheCurrencyFormat()
    {
        // arrange
        var testSubject = MoneyInfo.GetMoneyInfo(new CultureInfo("en-NZ"));

        // act
        var testResult = testSubject.GetFormat(typeof(CurrencyFormatInfo));

        // assert
        testResult.Should().BeSameAs(testSubject.CurrencyFormat);
    }

    [Fact]
    public void GetFormat_ForAnUnsupportedType_ReturnsNull()
    {
        // arrange
        var testSubject = MoneyInfo.InvariantMoney;

        // act
        var testResult = testSubject.GetFormat(typeof(NumberFormatInfo));

        // assert
        testResult.Should().BeNull();
    }

    #endregion

    #region Test: Constructors

    [Fact]
    public void Constructor_WithNoArguments_ReturnsCultureIndependentInformation()
    {
        // act
        var testResult = new MoneyInfo();

        // assert
        testResult.CurrencyFormat.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.XXX));
    }

    [Fact]
    public void Constructor_WithLanguageAndCountry_ReturnsInformationForThatCulture()
    {
        // act
        var testResult = new MoneyInfo((Iso639Part1Language)"en", Iso3166Part1Alpha2Country.NZ);

        // assert
        testResult.CurrencyFormat.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.NZD));
    }

    [Fact]
    public void Constructor_WithCulture_ReturnsInformationForThatCulture()
    {
        // act
        var testResult = new MoneyInfo(new CultureInfo("de-DE"));

        // assert
        testResult.CurrencyFormat.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.EUR));
    }

    [Fact]
    public void Constructor_WithLanguageAndCountryWhichAreNotACulture_FallsBackToCultureIndependentInformation()
    {
        // act
        var testAction = () => new MoneyInfo((Iso639Part1Language)"aa", Iso3166Part1Alpha2Country.AQ);

        // assert
        // Not every well-formed combination of a language and a country is a culture the host knows about.
        testAction.Should().NotThrow("an unknown culture must not prevent the information from being constructed");
    }

    #endregion

    #region Test: IsReadOnly

    [Fact]
    public void CurrencyFormat_WhenSetOnAReadOnlyInstance_ThrowsInvalidOperationException()
    {
        // arrange
        var testSubject = MoneyInfo.GetMoneyInfo(new CultureInfo("en-NZ"));

        // act
        var testAction = () => testSubject.CurrencyFormat = new CurrencyFormatInfo();

        // assert
        testAction.Should().Throw<InvalidOperationException>("the instance is read only");
    }

    [Fact]
    public void GetMoneyInfo_ReturnsAReadOnlyInstance()
    {
        // act
        var testResult = MoneyInfo.GetMoneyInfo(new CultureInfo("en-NZ"));

        // assert
        testResult.IsReadOnly.Should().BeTrue();
        testResult.CurrencyFormat.IsReadOnly.Should().BeTrue("sealing propagates to the currency format information");
    }

    #endregion

    #region Test: CurrentMoney

    [Fact]
    public void CurrentMoney_WhenCurrentCultureChanges_ReflectsTheNewCulture()
    {
        // arrange
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // act
            CultureInfo.CurrentCulture = new CultureInfo("en-NZ");
            var newZealandResult = MoneyInfo.CurrentMoney.CurrencyFormat.CurrencyCode;

            CultureInfo.CurrentCulture = new CultureInfo("ja-JP");
            var japaneseResult = MoneyInfo.CurrentMoney.CurrencyFormat.CurrencyCode;

            // assert
            newZealandResult.Should().Be(nameof(Iso4217CurrencyCurrent.NZD));
            japaneseResult.Should().Be(nameof(Iso4217CurrencyCurrent.JPY), "the information must track a change of culture");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void CurrentMoney_WhenRead_DoesNotThrow()
    {
        // act
        var testAction = () => MoneyInfo.CurrentMoney;

        // assert
        testAction.Should().NotThrow("the current information is derived from the current culture");
    }

    #endregion
}
