using System.Globalization;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class MoneyInfoTests
{
    private const string NewZealandCulture = "en-NZ";

    // ISO 639-1 language codes.
    private const string EnglishLanguageCode = "en";
    private const string AfarLanguageCode = "aa";

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
        var testSubject = MoneyInfo.GetMoneyInfo(new CultureInfo(NewZealandCulture));

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
        var testResult = new MoneyInfo((Iso639Part1Language)EnglishLanguageCode, Iso3166Part1Alpha2Country.NZ);

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
        var testResult = new MoneyInfo((Iso639Part1Language)AfarLanguageCode, Iso3166Part1Alpha2Country.AQ);

        // assert
        // Not every well-formed combination of a language and a country is a culture the host knows about.
        testResult.CurrencyFormat.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.XXX), "an unknown culture falls back to the culture-independent information");
    }

    #endregion

    #region Test: IsReadOnly

    [Fact]
    public void CurrencyFormat_WhenSetOnAReadOnlyInstance_ThrowsInvalidOperationException()
    {
        // arrange
        var testSubject = MoneyInfo.GetMoneyInfo(new CultureInfo(NewZealandCulture));

        // act
        Action testAction = () => testSubject.CurrencyFormat = new CurrencyFormatInfo();

        // assert
        testAction.Should().Throw<InvalidOperationException>("the instance is read only");
    }

    [Fact]
    public void GetMoneyInfo_ForCulture_ReturnsAReadOnlyInstance()
    {
        // act
        var testResult = MoneyInfo.GetMoneyInfo(new CultureInfo(NewZealandCulture));

        // assert
        testResult.IsReadOnly.Should().BeTrue();
        testResult.CurrencyFormat.IsReadOnly.Should().BeTrue("sealing propagates to the currency format information");
    }

    #endregion

    #region Test: CurrentMoney

    [Fact]
    public void CurrentMoney_AfterCurrentCultureChanges_ReflectsTheNewCulture()
    {
        // arrange
        using var newZealandCultureScope = new CurrentCultureScope(new CultureInfo(NewZealandCulture));

        // act
        var newZealandResult = MoneyInfo.CurrentMoney;
        using var japaneseCultureScope = new CurrentCultureScope(new CultureInfo("ja-JP"));
        var japaneseResult = MoneyInfo.CurrentMoney;

        // assert
        newZealandResult.CurrencyFormat.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.NZD), "the information reflects the culture in effect when it is read");
        japaneseResult.CurrencyFormat.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.JPY), "the information must track a change of culture");
    }

    [Fact]
    public void CurrentMoney_WhenRead_DoesNotThrow()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(new CultureInfo(NewZealandCulture));

        // act
        Action testAction = () => _ = MoneyInfo.CurrentMoney;

        // assert
        testAction.Should().NotThrow("the current information is derived from the current culture");
    }

    #endregion
}
