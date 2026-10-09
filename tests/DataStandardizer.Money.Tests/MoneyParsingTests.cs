using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class MoneyParsingTests
{
    private static MoneyInfo MoneyInfoFor(string cultureName) => MoneyInfo.GetMoneyInfo(new CultureInfo(cultureName));

    #region Test: TryParse_CurrencyCode

    [Theory]
    [InlineData("NZD1,234.50", 1234.50)]
    [InlineData("1,234.50NZD", 1234.50)]
    [InlineData("NZD 1234.50", 1234.50)]
    [InlineData("-NZD1,234.50", -1234.50)]
    [InlineData("(NZD1,234.50)", -1234.50)]
    public void TryParse_InputContainsCurrencyCode_ReturnsTrueAndResolvesCurrency(string testValue, decimal expectedAmount)
    {
        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, MoneyInfoFor("en-NZ"), out var result);

        // assert
        testResult.Should().BeTrue("a currency code identifies a currency without ambiguity");
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD);
        ((decimal)result).Should().Be(expectedAmount);
    }

    [Fact]
    public void TryParse_InputContainsGroupSeparator_RetainsMinorUnits()
    {
        // act
        var testResult = Money.TryParse("NZD1,234.50", MoneyStyles.Currency, MoneyInfoFor("en-NZ"), out var result);

        // assert
        testResult.Should().BeTrue();
        ((decimal)result).Should().Be(1234.50m, "the minor units must not be discarded when a group separator is present");
    }

    #endregion

    #region Test: TryParse_UnambiguousCurrencySymbol

    [Theory]
    [InlineData("₹1,234.50", Iso4217CurrencyCurrent.INR)]
    [InlineData("NZ$1,234.50", Iso4217CurrencyCurrent.NZD)]
    [InlineData("CA$1,234.50", Iso4217CurrencyCurrent.CAD)]
    [InlineData("Kč1234", Iso4217CurrencyCurrent.CZK)]
    [InlineData("Ft1234", Iso4217CurrencyCurrent.HUF)]
    public void TryParse_InputContainsUnambiguousCurrencySymbol_ResolvesCurrencyWithoutCulture(string testValue, Iso4217CurrencyCurrent expectedCurrency)
    {
        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, null, out var result);

        // assert
        testResult.Should().BeTrue("a symbol denoting exactly one currency needs no culture to resolve it");
        result.IsoCurrencyCode.Should().Be(expectedCurrency);
    }

    [Fact]
    public void TryParse_InputContainsSymbolWhichIsAlsoTheSuffixOfALongerSymbol_PrefersTheLongerSymbol()
    {
        // act
        var testResult = Money.TryParse("NZ$100", MoneyStyles.Currency, null, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD, "NZ$ is matched in preference to $");
    }

    #endregion

    #region Test: TryParse_AmbiguousCurrencySymbol

    [Theory]
    [InlineData("en-NZ", Iso4217CurrencyCurrent.NZD)]
    [InlineData("en-AU", Iso4217CurrencyCurrent.AUD)]
    [InlineData("en-US", Iso4217CurrencyCurrent.USD)]
    [InlineData("en-CA", Iso4217CurrencyCurrent.CAD)]
    public void TryParse_InputContainsAmbiguousCurrencySymbolAndCultureResolvesIt_ReturnsTrue(string testCulture, Iso4217CurrencyCurrent expectedCurrency)
    {
        // act
        var testResult = Money.TryParse("$100", MoneyStyles.Any, MoneyInfoFor(testCulture), out var result);

        // assert
        testResult.Should().BeTrue("the culture {0} accounts for the dollar sign", testCulture);
        result.IsoCurrencyCode.Should().Be(expectedCurrency);
    }

    [Fact]
    public void TryParse_InputContainsAmbiguousCurrencySymbolWithoutOptIn_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse("$100", MoneyStyles.Currency, MoneyInfoFor("en-NZ"), out _);

        // assert
        testResult.Should().BeFalse("resolving a shared symbol must be asked for explicitly");
    }

    [Fact]
    public void TryParse_InputContainsAmbiguousCurrencySymbolAndCultureDoesNotResolveIt_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse("$100", MoneyStyles.Any, MoneyInfoFor("de-DE"), out _);

        // assert
        testResult.Should().BeFalse("the currency of the culture is not denoted by the dollar sign");
    }

    [Fact]
    public void TryParse_InputContainsAmbiguousCurrencySymbolWithoutProvider_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse("$100", MoneyStyles.Any, null, out _);

        // assert
        testResult.Should().BeFalse("without a culture there is no context in which to resolve a shared symbol");
    }

    #endregion

    #region Test: TryParse_Styles

    [Fact]
    public void TryParse_StylesPermitCurrencyCodeOnly_RejectsCurrencySymbol()
    {
        // arrange
        const MoneyStyles testStyles = MoneyStyles.Number | MoneyStyles.AllowCurrencyCode;

        // act
        var codeResult = Money.TryParse("NZD1234.50", testStyles, MoneyInfoFor("en-NZ"), out _);
        var symbolResult = Money.TryParse("₹100", testStyles, MoneyInfoFor("en-NZ"), out _);

        // assert
        codeResult.Should().BeTrue("a currency code is permitted");
        symbolResult.Should().BeFalse("a currency symbol is not permitted");
    }

    [Theory]
    [InlineData(MoneyStyles.None, "1,234.50", false)]
    [InlineData(MoneyStyles.None, "1234", true)]
    [InlineData(MoneyStyles.AllowThousands, "1,234", true)]
    [InlineData(MoneyStyles.AllowDecimalPoint, "1234.50", true)]
    public void TryParse_StylesGovernPermittedElements_ReturnsExpectedOutcome(MoneyStyles testStyles, string testValue, bool expectedResult)
    {
        // act
        var testResult = Money.TryParse(testValue, testStyles, MoneyInfoFor("en-NZ"), out _);

        // assert
        testResult.Should().Be(expectedResult, "the styles {0} were requested for the input {1}", testStyles, testValue);
    }

    [Fact]
    public void TryParse_StylesExcludeParentheses_RejectsParenthesisedValue()
    {
        // arrange
        var testStyles = MoneyStyles.Currency & ~MoneyStyles.AllowParentheses;

        // act
        var testResult = Money.TryParse("(NZD1234.50)", testStyles, MoneyInfoFor("en-NZ"), out _);

        // assert
        testResult.Should().BeFalse("parentheses are not permitted");
    }

    #endregion

    #region Test: TryParse_CultureSeparators

    [Theory]
    [InlineData("de-DE", "zł1.234,50", 1234.50)]
    [InlineData("en-NZ", "R$1,234.50", 1234.50)]
    public void TryParse_SeparatorsOfSuppliedCulture_InterpretsAmountAccordingly(string testCulture, string testValue, decimal expectedAmount)
    {
        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, MoneyInfoFor(testCulture), out var result);

        // assert
        testResult.Should().BeTrue();
        ((decimal)result).Should().Be(expectedAmount, "the separators of {0} apply", testCulture);
    }

    #endregion

    #region Test: ParseExact

    [Fact]
    public void TryParseExact_InputMatchesRequestedFormat_ReturnsTrue()
    {
        // arrange
        var testProvider = MoneyInfoFor("en-NZ");
        var testValue = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD).ToString("I", testProvider);

        // act
        var testResult = Money.TryParseExact(testValue, new[] { "I" }, testProvider, MoneyStyles.Currency, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD);
    }

    [Fact]
    public void TryParseExact_InputIsInADifferentFormat_ReturnsFalse()
    {
        // arrange
        var testProvider = MoneyInfoFor("en-NZ");
        var testValue = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD).ToString("C", testProvider);

        // act
        var testResult = Money.TryParseExact(testValue, new[] { "I" }, testProvider, MoneyStyles.Currency, out _);

        // assert
        testResult.Should().BeFalse("the value carries a currency symbol rather than a currency code");
    }

    [Fact]
    public void TryParseExact_InputMatchesOneOfSeveralFormats_ReturnsTrue()
    {
        // arrange
        var testProvider = MoneyInfoFor("en-NZ");
        var testValue = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD).ToString("C", testProvider);

        // act
        var testResult = Money.TryParseExact(testValue, new[] { "I", "C" }, testProvider, MoneyStyles.Currency, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD);
    }

    [Fact]
    public void TryParseExact_FormatNamesADifferentCurrency_ReturnsFalse()
    {
        // arrange
        var testProvider = MoneyInfoFor("en-NZ");
        var testValue = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD).ToString("I", testProvider);

        // act
        var testResult = Money.TryParseExact(testValue, new[] { "USD" }, testProvider, MoneyStyles.Currency, out _);

        // assert
        testResult.Should().BeFalse("the format names a currency other than that of the value");
    }

    #endregion

    #region Test: RoundTrip

    [Theory]
    [InlineData("en-NZ", Iso4217CurrencyCurrent.NZD)]
    [InlineData("de-DE", Iso4217CurrencyCurrent.EUR)]
    [InlineData("en-IN", Iso4217CurrencyCurrent.INR)]
    [InlineData("pt-BR", Iso4217CurrencyCurrent.BRL)]
    [InlineData("en-US", Iso4217CurrencyCurrent.USD)]
    [InlineData("fr-FR", Iso4217CurrencyCurrent.EUR)]
    public void RoundTrip_CurrencyCodeFormat_ReturnsAnEquivalentValue(string testCulture, Iso4217CurrencyCurrent testCurrency)
    {
        // arrange
        var testProvider = MoneyInfoFor(testCulture);
        var testSubject = Money.Create(1234.56m, testCurrency);

        // act
        var formatted = testSubject.ToString("I", testProvider);
        var testResult = Money.TryParse(formatted, MoneyStyles.Currency, testProvider, out var result);

        // assert
        testResult.Should().BeTrue("the currency code form must be parseable, for the input {0}", formatted);
        result.IsoCurrencyCode.Should().Be(testSubject.IsoCurrencyCode, "the currency must survive the round trip");
        ((decimal)result).Should().Be((decimal)testSubject, "the amount must survive the round trip");
    }

    [Theory]
    [InlineData("en-NZ", Iso4217CurrencyCurrent.NZD)]
    [InlineData("de-DE", Iso4217CurrencyCurrent.EUR)]
    [InlineData("en-IN", Iso4217CurrencyCurrent.INR)]
    [InlineData("pt-BR", Iso4217CurrencyCurrent.BRL)]
    public void RoundTrip_CurrencySymbolFormatForUnambiguousSymbol_ReturnsAnEquivalentValue(string testCulture, Iso4217CurrencyCurrent testCurrency)
    {
        // arrange
        var testProvider = MoneyInfoFor(testCulture);
        var testSubject = Money.Create(1234.56m, testCurrency);

        // act
        var formatted = testSubject.ToString("C", testProvider);
        var testResult = Money.TryParse(formatted, MoneyStyles.Currency, testProvider, out var result);

        // assert
        testResult.Should().BeTrue("the standard symbol of {0} denotes exactly one currency, for the input {1}", testCurrency, formatted);
        result.IsoCurrencyCode.Should().Be(testSubject.IsoCurrencyCode);
        ((decimal)result).Should().Be((decimal)testSubject);
    }

    [Fact]
    public void RoundTrip_NarrowSymbolSharedBySeveralCurrencies_RequiresCultureContext()
    {
        // arrange
        var testProvider = MoneyInfoFor("en-NZ");
        var testSubject = Money.Create(1234.56m, Iso4217CurrencyCurrent.NZD);
        var formatted = testSubject.ToString("H", testProvider);

        // act
        var withoutOptIn = Money.TryParse(formatted, MoneyStyles.Currency, testProvider, out _);
        var withOptIn = Money.TryParse(formatted, MoneyStyles.Any, testProvider, out var result);

        // assert
        formatted.Should().StartWith("$", "the narrow symbol of the New Zealand Dollar is the dollar sign");
        withoutOptIn.Should().BeFalse("a shared symbol is not resolved without opting in");
        withOptIn.Should().BeTrue("the culture accounts for the dollar sign");
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD);
    }

    #endregion
}
