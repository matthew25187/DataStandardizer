using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class MoneyParsingTests
{
    private const string NewZealandCulture = "en-NZ";
    private const string GermanCulture = "de-DE";
    private const string IndianCulture = "en-IN";
    private const string BrazilianCulture = "pt-BR";
    private const string UnitedStatesCulture = "en-US";
    private const string FrenchCulture = "fr-FR";

    private const string StandardSymbolFormat = "C";
    private const string NarrowSymbolFormat = "H";
    private const string CurrencyCodeFormat = "I";

    private const decimal TestAmount = 1234.50m;
    private const string NzdCurrencyCodeValue = "NZD1,234.50";
    private const string NzdStandardSymbolValue = "NZ$1,234.50";
    private const string AmbiguousDollarValue = "$100";

    private const MoneyStyles CurrencyCodeOnlyStyles = MoneyStyles.Number | MoneyStyles.AllowCurrencyCode;
    private const MoneyStyles CurrencyStylesWithoutParentheses = MoneyStyles.Number | MoneyStyles.AllowCurrencyCode | MoneyStyles.AllowCurrencySymbol;

    // The representations of RoundTripAmount in the currency of each culture, under that culture.
    private const decimal RoundTripAmount = 1234.56m;
    private const string NewZealandCurrencyCodeRepresentation = "NZD1,234.56";
    private const string NewZealandStandardSymbolRepresentation = "NZ$1,234.56";
    private const string NewZealandNarrowSymbolRepresentation = "$1,234.56";
    private const string GermanCurrencyCodeRepresentation = "1.234,56 EUR";
    private const string GermanStandardSymbolRepresentation = "1.234,56 \x20AC";
    private const string IndianCurrencyCodeRepresentation = "INR1,234.56";
    private const string IndianStandardSymbolRepresentation = "\x20B91,234.56";
    private const string BrazilianCurrencyCodeRepresentation = "BRL 1.234,56";
    private const string BrazilianStandardSymbolRepresentation = "R$ 1.234,56";
    private const string UnitedStatesCurrencyCodeRepresentation = "USD1,234.56";
    private const string FrenchCurrencyCodeRepresentation = "1\x202F234,56 EUR"; // the group separator is a narrow no-break space

    private static MoneyInfo MoneyInfoFor(string cultureName) => MoneyInfo.GetMoneyInfo(new CultureInfo(cultureName));

    #region Test: TryParse_CurrencyCode

    [Theory]
    [InlineData(NzdCurrencyCodeValue, 1234.50)]
    [InlineData("1,234.50NZD", 1234.50)]
    [InlineData("NZD 1234.50", 1234.50)]
    [InlineData("-NZD1,234.50", -1234.50)]
    [InlineData("(NZD1,234.50)", -1234.50)]
    public void TryParse_InputContainsCurrencyCode_ReturnsTrueAndResolvesCurrency(string testValue, decimal expectedAmount)
    {
        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, MoneyInfoFor(NewZealandCulture), out var result);

        // assert
        testResult.Should().BeTrue("a currency code identifies a currency without ambiguity");
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD);
        ((decimal)result).Should().Be(expectedAmount);
    }

    [Fact]
    public void TryParse_InputContainsGroupSeparator_RetainsMinorUnits()
    {
        // act
        var testResult = Money.TryParse(NzdCurrencyCodeValue, MoneyStyles.Currency, MoneyInfoFor(NewZealandCulture), out var result);

        // assert
        testResult.Should().BeTrue();
        ((decimal)result).Should().Be(TestAmount, "the minor units must not be discarded when a group separator is present");
    }

    #endregion

    #region Test: TryParse_UnambiguousCurrencySymbol

    [Theory]
    [InlineData("\x20B91,234.50", Iso4217CurrencyCurrent.INR)]
    [InlineData(NzdStandardSymbolValue, Iso4217CurrencyCurrent.NZD)]
    [InlineData("CA$1,234.50", Iso4217CurrencyCurrent.CAD)]
    [InlineData("K\x010D1234", Iso4217CurrencyCurrent.CZK)]
    [InlineData("Ft1234", Iso4217CurrencyCurrent.HUF)]
    public void TryParse_InputContainsUnambiguousCurrencySymbol_ResolvesCurrencyWithoutCulture(string testValue, Iso4217CurrencyCurrent expectedCurrency)
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, null, out var result);

        // assert
        testResult.Should().BeTrue("a symbol denoting exactly one currency needs no culture to resolve it");
        result.IsoCurrencyCode.Should().Be(expectedCurrency);
    }

    [Fact]
    public void TryParse_InputContainsSymbolWhichIsAlsoTheSuffixOfALongerSymbol_PrefersTheLongerSymbol()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

        // act
        var testResult = Money.TryParse("NZ$100", MoneyStyles.Currency, null, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD, "NZ$ is matched in preference to $");
    }

    #endregion

    #region Test: TryParse_AmbiguousCurrencySymbol

    [Theory]
    [InlineData(NewZealandCulture, Iso4217CurrencyCurrent.NZD)]
    [InlineData("en-AU", Iso4217CurrencyCurrent.AUD)]
    [InlineData(UnitedStatesCulture, Iso4217CurrencyCurrent.USD)]
    [InlineData("en-CA", Iso4217CurrencyCurrent.CAD)]
    public void TryParse_InputContainsAmbiguousCurrencySymbolAndCultureResolvesIt_ReturnsTrue(string testCulture, Iso4217CurrencyCurrent expectedCurrency)
    {
        // act
        var testResult = Money.TryParse(AmbiguousDollarValue, MoneyStyles.Any, MoneyInfoFor(testCulture), out var result);

        // assert
        testResult.Should().BeTrue("the culture {0} accounts for the dollar sign", testCulture);
        result.IsoCurrencyCode.Should().Be(expectedCurrency);
    }

    [Fact]
    public void TryParse_InputContainsAmbiguousCurrencySymbolWithoutOptIn_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse(AmbiguousDollarValue, MoneyStyles.Currency, MoneyInfoFor(NewZealandCulture), out _);

        // assert
        testResult.Should().BeFalse("resolving a shared symbol must be asked for explicitly");
    }

    [Fact]
    public void TryParse_InputContainsAmbiguousCurrencySymbolAndCultureDoesNotResolveIt_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse(AmbiguousDollarValue, MoneyStyles.Any, MoneyInfoFor(GermanCulture), out _);

        // assert
        testResult.Should().BeFalse("the currency of the culture is not denoted by the dollar sign");
    }

    [Fact]
    public void TryParse_InputContainsAmbiguousCurrencySymbolWithoutProvider_ReturnsFalse()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

        // act
        var testResult = Money.TryParse(AmbiguousDollarValue, MoneyStyles.Any, null, out _);

        // assert
        testResult.Should().BeFalse("without a culture there is no context in which to resolve a shared symbol");
    }

    #endregion

    #region Test: TryParse_Styles

    [Fact]
    public void TryParse_StylesPermitCurrencyCodeOnlyAndInputContainsCurrencyCode_ReturnsTrue()
    {
        // act
        var testResult = Money.TryParse("NZD1234.50", CurrencyCodeOnlyStyles, MoneyInfoFor(NewZealandCulture), out _);

        // assert
        testResult.Should().BeTrue("a currency code is permitted");
    }

    [Fact]
    public void TryParse_StylesPermitCurrencyCodeOnlyAndInputContainsCurrencySymbol_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse("\x20B9100", CurrencyCodeOnlyStyles, MoneyInfoFor(NewZealandCulture), out _);

        // assert
        testResult.Should().BeFalse("a currency symbol is not permitted");
    }

    [Theory]
    [InlineData(MoneyStyles.None, "1,234.50", false)]
    [InlineData(MoneyStyles.None, "1234", true)]
    [InlineData(MoneyStyles.AllowThousands, "1,234", true)]
    [InlineData(MoneyStyles.AllowDecimalPoint, "1234.50", true)]
    public void TryParse_StylesGovernPermittedElements_ReturnsExpectedOutcome(MoneyStyles testStyles, string testValue, bool expectedResult)
    {
        // act
        var testResult = Money.TryParse(testValue, testStyles, MoneyInfoFor(NewZealandCulture), out _);

        // assert
        testResult.Should().Be(expectedResult, "the styles {0} were requested for the input {1}", testStyles, testValue);
    }

    [Fact]
    public void TryParse_StylesExcludeParentheses_RejectsParenthesisedValue()
    {
        // act
        var testResult = Money.TryParse("(NZD1234.50)", CurrencyStylesWithoutParentheses, MoneyInfoFor(NewZealandCulture), out _);

        // assert
        testResult.Should().BeFalse("parentheses are not permitted");
    }

    #endregion

    #region Test: TryParse_CultureSeparators

    [Theory]
    [InlineData(GermanCulture, "z\x01421.234,50", 1234.50)]
    [InlineData(NewZealandCulture, "R$1,234.50", 1234.50)]
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
        // act
        var testResult = Money.TryParseExact(NzdCurrencyCodeValue, new[] { CurrencyCodeFormat }, MoneyInfoFor(NewZealandCulture), MoneyStyles.Currency, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD);
    }

    [Fact]
    public void TryParseExact_InputIsInADifferentFormat_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParseExact(NzdStandardSymbolValue, new[] { CurrencyCodeFormat }, MoneyInfoFor(NewZealandCulture), MoneyStyles.Currency, out _);

        // assert
        testResult.Should().BeFalse("the value carries a currency symbol rather than a currency code");
    }

    [Fact]
    public void TryParseExact_InputMatchesOneOfSeveralFormats_ReturnsTrue()
    {
        // act
        var testResult = Money.TryParseExact(NzdStandardSymbolValue, new[] { CurrencyCodeFormat, StandardSymbolFormat }, MoneyInfoFor(NewZealandCulture), MoneyStyles.Currency, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.NZD);
    }

    [Fact]
    public void TryParseExact_FormatNamesADifferentCurrency_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParseExact(NzdCurrencyCodeValue, new[] { nameof(Iso4217CurrencyCurrent.USD) }, MoneyInfoFor(NewZealandCulture), MoneyStyles.Currency, out _);

        // assert
        testResult.Should().BeFalse("the format names a currency other than that of the value");
    }

    #endregion

    #region Test: RoundTrip

    // A round trip is shown in two halves: ToString produces each representation below, and TryParse
    // returns the original value from that same representation.

    [Theory]
    [MemberData(nameof(RoundTripFormatTestCases))]
    public void ToString_RoundTripFormat_ProducesParseableRepresentation(string testCulture, Iso4217CurrencyCurrent testCurrency, string testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(RoundTripAmount, testCurrency);

        // act
        var testResult = testSubject.ToString(testFormat, MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the format {0} was requested under the culture {1}", testFormat, testCulture);
    }

    public static TheoryData<string, Iso4217CurrencyCurrent, string, string> RoundTripFormatTestCases => new()
    {
        { NewZealandCulture, Iso4217CurrencyCurrent.NZD, CurrencyCodeFormat, NewZealandCurrencyCodeRepresentation },
        { GermanCulture, Iso4217CurrencyCurrent.EUR, CurrencyCodeFormat, GermanCurrencyCodeRepresentation },
        { IndianCulture, Iso4217CurrencyCurrent.INR, CurrencyCodeFormat, IndianCurrencyCodeRepresentation },
        { BrazilianCulture, Iso4217CurrencyCurrent.BRL, CurrencyCodeFormat, BrazilianCurrencyCodeRepresentation },
        { UnitedStatesCulture, Iso4217CurrencyCurrent.USD, CurrencyCodeFormat, UnitedStatesCurrencyCodeRepresentation },
        { FrenchCulture, Iso4217CurrencyCurrent.EUR, CurrencyCodeFormat, FrenchCurrencyCodeRepresentation },
        { NewZealandCulture, Iso4217CurrencyCurrent.NZD, StandardSymbolFormat, NewZealandStandardSymbolRepresentation },
        { GermanCulture, Iso4217CurrencyCurrent.EUR, StandardSymbolFormat, GermanStandardSymbolRepresentation },
        { IndianCulture, Iso4217CurrencyCurrent.INR, StandardSymbolFormat, IndianStandardSymbolRepresentation },
        { BrazilianCulture, Iso4217CurrencyCurrent.BRL, StandardSymbolFormat, BrazilianStandardSymbolRepresentation },
        { NewZealandCulture, Iso4217CurrencyCurrent.NZD, NarrowSymbolFormat, NewZealandNarrowSymbolRepresentation },
    };

    [Theory]
    [MemberData(nameof(RoundTripParseTestCases))]
    public void TryParse_RoundTripRepresentation_ReturnsTheFormattedValue(string testCulture, MoneyStyles testStyles, string testValue, Iso4217CurrencyCurrent expectedCurrency)
    {
        // act
        var testResult = Money.TryParse(testValue, testStyles, MoneyInfoFor(testCulture), out var result);

        // assert
        testResult.Should().BeTrue("the representation {0} must be parseable under the culture {1}", testValue, testCulture);
        result.IsoCurrencyCode.Should().Be(expectedCurrency, "the currency must survive the round trip");
        ((decimal)result).Should().Be(RoundTripAmount, "the amount must survive the round trip");
    }

    public static TheoryData<string, MoneyStyles, string, Iso4217CurrencyCurrent> RoundTripParseTestCases => new()
    {
        { NewZealandCulture, MoneyStyles.Currency, NewZealandCurrencyCodeRepresentation, Iso4217CurrencyCurrent.NZD },
        { GermanCulture, MoneyStyles.Currency, GermanCurrencyCodeRepresentation, Iso4217CurrencyCurrent.EUR },
        { IndianCulture, MoneyStyles.Currency, IndianCurrencyCodeRepresentation, Iso4217CurrencyCurrent.INR },
        { BrazilianCulture, MoneyStyles.Currency, BrazilianCurrencyCodeRepresentation, Iso4217CurrencyCurrent.BRL },
        { UnitedStatesCulture, MoneyStyles.Currency, UnitedStatesCurrencyCodeRepresentation, Iso4217CurrencyCurrent.USD },
        { FrenchCulture, MoneyStyles.Currency, FrenchCurrencyCodeRepresentation, Iso4217CurrencyCurrent.EUR },
        // The standard symbol of each of these currencies denotes exactly one currency.
        { NewZealandCulture, MoneyStyles.Currency, NewZealandStandardSymbolRepresentation, Iso4217CurrencyCurrent.NZD },
        { GermanCulture, MoneyStyles.Currency, GermanStandardSymbolRepresentation, Iso4217CurrencyCurrent.EUR },
        { IndianCulture, MoneyStyles.Currency, IndianStandardSymbolRepresentation, Iso4217CurrencyCurrent.INR },
        { BrazilianCulture, MoneyStyles.Currency, BrazilianStandardSymbolRepresentation, Iso4217CurrencyCurrent.BRL },
        // The narrow symbol is shared by several currencies, so it is resolved only by opting in and from the culture.
        { NewZealandCulture, MoneyStyles.Any, NewZealandNarrowSymbolRepresentation, Iso4217CurrencyCurrent.NZD },
    };

    [Fact]
    public void TryParse_RoundTripNarrowSymbolRepresentationWithoutOptIn_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse(NewZealandNarrowSymbolRepresentation, MoneyStyles.Currency, MoneyInfoFor(NewZealandCulture), out _);

        // assert
        testResult.Should().BeFalse("a shared symbol is not resolved without opting in");
    }

    #endregion
}
