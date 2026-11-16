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

    // The generic currency sign U+00A4, which CLDR gives as the symbol of XXX, followed by the amount 5.
    // The escape is written with four digits so that the 5 is not read as part of it.
    private const string GenericCurrencySignValue = "\x00A45";
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

    private const decimal OneAndAHalf = 1.5m;
    private const decimal NegativeOneAndAHalf = -1.5m;

    // Amounts with the decimal separator "," and the group separator ".".
    private const string CommaDecimalOneAndAHalfValue = "1,5";
    private const string CommaDecimalGroupedValue = "1.234,56";

    // NegativeOneAndAHalf with the negative sign U+2212 MINUS SIGN.
    private const string MinusSignOneAndAHalfValue = "\x22121.5";

    // A currency code preceded by the negative sign U+2212 MINUS SIGN, which en-US currency formatting doesn't use.
    private const string MinusSignCurrencyCodeValue = "\x2212USD5";

    private static MoneyInfo MoneyInfoFor(string cultureName) => MoneyInfo.GetMoneyInfo(new CultureInfo(cultureName));

    /// <summary>
    /// An en-US culture whose number formatting has the decimal separator "," and the group separator ".",
    /// the reverse of the currency formatting Unicode CLDR gives en-US.
    /// </summary>
    private static CultureInfo CommaDecimalCulture()
    {
        var culture = new CultureInfo(UnitedStatesCulture);
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        return culture;
    }

    /// <summary>
    /// Invariant number formatting whose negative sign is U+2212 MINUS SIGN.
    /// </summary>
    private static NumberFormatInfo MinusSignNumberFormat() => new() { NegativeSign = MinusSign };

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

    // Funds codes from ISO 4217 List One.
    [Theory]
    [InlineData("USN 1")]
    [InlineData("CHE 1")]
    [InlineData("CLF 1")]
    [InlineData("BOV 1")]
    public void TryParse_InputContainsFundCode_ReturnsFalse(string testValue)
    {
        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, MoneyInfoFor(NewZealandCulture), out _);

        // assert
        testResult.Should().BeFalse("ISO 4217 lists funds codes apart from currencies");
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

    [Fact]
    public void TryParse_InputContainsGenericCurrencySign_ReturnsFalse()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

        // act
        var testResult = Money.TryParse(GenericCurrencySignValue, out _);

        // assert
        testResult.Should().BeFalse("the generic currency sign denotes XXX, which is not a currency a Money value can carry");
    }

    [Fact]
    public void Parse_InputContainsGenericCurrencySign_ThrowsFormatException()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(CultureInfo.InvariantCulture);

        // act
        Action testAction = () => Money.Parse(GenericCurrencySignValue);

        // assert
        testAction.Should().Throw<FormatException>();
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

    #region Test: TryParse_AmountWithoutCurrencyToken

    // Each value is what the general format, or a money format of a value with no currency, gives under the
    // same provider and thread culture, except where it has a currency token.
    [Theory]
    [MemberData(nameof(AmountSeparatorsTestCases), DisableDiscoveryEnumeration = true)]
    public void TryParse_SeparatorsOfNumberAndCurrencyFormattingDiffer_ReadsTheAmountWithTheSeparatorsItIsFormattedWith(CultureInfo threadCulture, IFormatProvider? testProvider, string testValue, decimal expectedAmount, Iso4217CurrencyCurrent expectedCurrency)
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(threadCulture);

        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, testProvider, out var result);

        // assert
        testResult.Should().BeTrue("{0} is how the amount is formatted under the provider", testValue);
        ((decimal)result).Should().Be(expectedAmount);
        result.IsoCurrencyCode.Should().Be(expectedCurrency);
    }

    // The currency formatting of de-DE and en-US comes from Unicode CLDR, by culture name: de-DE has the decimal
    // separator "," and the group separator ".", and en-US the reverse. Every number formatting is built here.
    public static TheoryData<CultureInfo, IFormatProvider?, string, decimal, Iso4217CurrencyCurrent> AmountSeparatorsTestCases => new()
    {
        // A NumberFormatInfo, whose currency formatting comes from the thread culture.
        { new CultureInfo(GermanCulture), NumberFormatInfo.InvariantInfo, "1.5", OneAndAHalf, Iso4217CurrencyCurrent.XXX },
        { new CultureInfo(UnitedStatesCulture), CommaDecimalCulture().NumberFormat, "1234,56", RoundTripAmount, Iso4217CurrencyCurrent.XXX },
        // A negative sign that differs from the currency's, with and without a currency token.
        { new CultureInfo(UnitedStatesCulture), MinusSignNumberFormat(), MinusSignOneAndAHalfValue, NegativeOneAndAHalf, Iso4217CurrencyCurrent.XXX },
        { new CultureInfo(UnitedStatesCulture), MinusSignNumberFormat(), UnitedStatesCurrencyCodeRepresentation, RoundTripAmount, Iso4217CurrencyCurrent.USD },
        // A culture, MoneyInfo or CurrencyFormatInfo whose number formatting differs from its currency formatting.
        { new CultureInfo(UnitedStatesCulture), CommaDecimalCulture(), CommaDecimalOneAndAHalfValue, OneAndAHalf, Iso4217CurrencyCurrent.XXX },
        { new CultureInfo(UnitedStatesCulture), CommaDecimalCulture(), CommaDecimalGroupedValue, RoundTripAmount, Iso4217CurrencyCurrent.XXX },
        { new CultureInfo(UnitedStatesCulture), new MoneyInfo(CommaDecimalCulture()), CommaDecimalOneAndAHalfValue, OneAndAHalf, Iso4217CurrencyCurrent.XXX },
        { new CultureInfo(UnitedStatesCulture), new CurrencyFormatInfo { CurrencyDecimalSeparator = ",", CurrencyGroupSeparator = "." }, CommaDecimalOneAndAHalfValue, OneAndAHalf, Iso4217CurrencyCurrent.XXX },
        // No provider, so the thread culture.
        { CommaDecimalCulture(), null, CommaDecimalOneAndAHalfValue, OneAndAHalf, Iso4217CurrencyCurrent.XXX },
        // A currency token, so the currency formatting.
        { new CultureInfo(UnitedStatesCulture), CommaDecimalCulture(), UnitedStatesCurrencyCodeRepresentation, RoundTripAmount, Iso4217CurrencyCurrent.USD },
    };

    [Fact]
    public void TryParse_AmountWithoutCurrencyTokenInTheSeparatorsOfTheCurrency_ReturnsFalse()
    {
        // act
        var testResult = Money.TryParse("1,234.56", MoneyStyles.Currency, CommaDecimalCulture(), out _);

        // assert
        testResult.Should().BeFalse("an amount without a currency token is read with the separators of the number formatting, in which \",\" is the decimal separator");
    }

    [Fact]
    public void TryParse_CurrencyTokenFoundOnlyAfterTheNegativeSignOfTheNumberFormatting_ReturnsFalse()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(new CultureInfo(UnitedStatesCulture));

        // act
        var testResult = Money.TryParse(MinusSignCurrencyCodeValue, MoneyStyles.Currency, MinusSignNumberFormat(), out _);

        // assert
        testResult.Should().BeFalse("a value with a currency token takes the negative sign of the currency, which is a hyphen-minus in en-US");
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

    #region Test: ParseExact_StandardSymbol

    // A standard symbol that is the standard symbol of one currency alone, but the narrow symbol of others as
    // well (CLDR root currency symbols), is read as that currency in the C format and nowhere else.

    private const string BritishCulture = "en-GB";
    private const string JapaneseCulture = "ja-JP";
    private const string KoreanCulture = "ko-KR";
    private const string DanishCulture = "da-DK";
    private const string AustralianCulture = "en-AU";

    private const decimal WholeAmount = 500m; // a whole amount, so currencies without minor units render it unrounded
    private const string UsdStandardSymbolValue = "$500.00";
    private const string GbpStandardSymbolValue = "\x00A3500.00";
    private const string JpyStandardSymbolValue = "\x00A5500";
    private const string KrwStandardSymbolValue = "\x20A9500";
    private const string DkkNarrowSymbolValue = "500,00 kr";
    private const string UsdNoDecimalsStandardSymbolValue = "$500";

    // MoneyFormatterTests pins the C and H renderings of WholeAmount that these values are.

    [Theory]
    [MemberData(nameof(StandardSymbolParseExactTestCases))]
    public void ParseExact_StandardSymbolFormatRendering_ReturnsTheFormattedValue(string testCulture, string testValue, Iso4217CurrencyCurrent expectedCurrency)
    {
        // act
        var testResult = Money.ParseExact(testValue, StandardSymbolFormat, MoneyInfoFor(testCulture));

        // assert
        testResult.IsoCurrencyCode.Should().Be(expectedCurrency, "the symbol is the standard symbol of that currency alone");
        ((decimal)testResult).Should().Be(WholeAmount);
    }

    public static TheoryData<string, string, Iso4217CurrencyCurrent> StandardSymbolParseExactTestCases => new()
    {
        { UnitedStatesCulture, UsdStandardSymbolValue, Iso4217CurrencyCurrent.USD },
        { BritishCulture, GbpStandardSymbolValue, Iso4217CurrencyCurrent.GBP },
        { JapaneseCulture, JpyStandardSymbolValue, Iso4217CurrencyCurrent.JPY },
        { KoreanCulture, KrwStandardSymbolValue, Iso4217CurrencyCurrent.KRW },
    };

    [Theory]
    [MemberData(nameof(StandardSymbolTryParseExactTestCases))]
    public void TryParseExact_StandardSymbolFormat_ReadsTheStandardSymbolCurrency(string testCulture, MoneyStyles testStyles, Iso4217CurrencyCurrent expectedCurrency)
    {
        // act
        var testResult = Money.TryParseExact(UsdStandardSymbolValue, new[] { StandardSymbolFormat }, MoneyInfoFor(testCulture), testStyles, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(expectedCurrency, "the C format carries the standard symbol, whatever the culture's currency");
    }

    public static TheoryData<string, MoneyStyles, Iso4217CurrencyCurrent> StandardSymbolTryParseExactTestCases => new()
    {
        { UnitedStatesCulture, MoneyStyles.Currency, Iso4217CurrencyCurrent.USD },
        { AustralianCulture, MoneyStyles.Currency, Iso4217CurrencyCurrent.USD },
        { AustralianCulture, MoneyStyles.Any, Iso4217CurrencyCurrent.USD },
    };

    [Theory]
    [MemberData(nameof(SharedSymbolWithoutOptInTestCases))]
    public void TryParseExact_SharedSymbolInAnotherFormatWithoutOptIn_ReturnsFalse(string testCulture, string testValue, string testFormat)
    {
        // act
        var testResult = Money.TryParseExact(testValue, new[] { testFormat }, MoneyInfoFor(testCulture), MoneyStyles.Currency, out _);

        // assert
        testResult.Should().BeFalse("outside the C format a shared symbol needs the opt-in");
    }

    public static TheoryData<string, string, string> SharedSymbolWithoutOptInTestCases => new()
    {
        // '$' is USD's standard symbol, but the H format renders a narrow symbol, which '$' is for some thirty currencies.
        { UnitedStatesCulture, UsdStandardSymbolValue, NarrowSymbolFormat },
        // 'kr' is the standard symbol of no currency, and the narrow symbol of several.
        { DanishCulture, DkkNarrowSymbolValue, NarrowSymbolFormat },
    };

    [Theory]
    [InlineData("c", UsdStandardSymbolValue)]
    [InlineData("C2", UsdStandardSymbolValue)]
    [InlineData("C0", UsdNoDecimalsStandardSymbolValue)]
    public void TryParseExact_StandardSymbolFormatWithCaseOrPrecision_ReadsTheStandardSymbolCurrency(string testFormat, string testValue)
    {
        // act
        var testResult = Money.TryParseExact(testValue, new[] { testFormat }, MoneyInfoFor(UnitedStatesCulture), MoneyStyles.Currency, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(Iso4217CurrencyCurrent.USD, "{0} is the C format", testFormat);
    }

    [Theory]
    [MemberData(nameof(MixedFormatsTestCases))]
    public void TryParseExact_FormatsMixingStandardSymbolFormat_ReadsTheMatchingCurrency(string testCulture, MoneyStyles testStyles, string[] testFormats, Iso4217CurrencyCurrent expectedCurrency)
    {
        // act
        var testResult = Money.TryParseExact(UsdStandardSymbolValue, testFormats, MoneyInfoFor(testCulture), testStyles, out var result);

        // assert
        testResult.Should().BeTrue();
        result.IsoCurrencyCode.Should().Be(expectedCurrency);
    }

    public static TheoryData<string, MoneyStyles, string[], Iso4217CurrencyCurrent> MixedFormatsTestCases => new()
    {
        // Only the C format matches.
        { UnitedStatesCulture, MoneyStyles.Currency, new[] { CurrencyCodeFormat, StandardSymbolFormat }, Iso4217CurrencyCurrent.USD },
        // The value is read as AUD for the I format, whose rendering doesn't match, and as USD for the C format.
        { AustralianCulture, MoneyStyles.Any, new[] { CurrencyCodeFormat, StandardSymbolFormat }, Iso4217CurrencyCurrent.USD },
        // Both formats read the value as USD.
        { UnitedStatesCulture, MoneyStyles.Any, new[] { StandardSymbolFormat, NarrowSymbolFormat }, Iso4217CurrencyCurrent.USD },
        // Without the opt-in, the H format can't read '$', so only the C format matches.
        { AustralianCulture, MoneyStyles.Currency, new[] { NarrowSymbolFormat, StandardSymbolFormat }, Iso4217CurrencyCurrent.USD },
    };

    [Theory]
    [MemberData(nameof(DifferentCurrenciesUnderDifferentFormatsTestCases))]
    public void TryParseExact_ValueIsTheRenderingOfDifferentCurrenciesUnderDifferentFormats_ReturnsFalse(string[] testFormats)
    {
        // act
        var testResult = Money.TryParseExact(UsdStandardSymbolValue, testFormats, MoneyInfoFor(AustralianCulture), MoneyStyles.Any, out _);

        // assert
        testResult.Should().BeFalse("'$500.00' is the C rendering of USD and, under en-AU, the H rendering of AUD");
    }

    public static TheoryData<string[]> DifferentCurrenciesUnderDifferentFormatsTestCases => new()
    {
        new[] { StandardSymbolFormat, NarrowSymbolFormat },
        new[] { NarrowSymbolFormat, StandardSymbolFormat },
    };

    [Theory]
    [MemberData(nameof(StandardSymbolWithoutFormatTestCases))]
    public void TryParse_StandardSymbolWithoutOptIn_ReturnsFalse(string testCulture, string testValue)
    {
        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, MoneyInfoFor(testCulture), out _);

        // assert
        testResult.Should().BeFalse("without a format, a symbol that is also a narrow symbol of other currencies stays ambiguous");
    }

    public static TheoryData<string, string> StandardSymbolWithoutFormatTestCases => new()
    {
        { UnitedStatesCulture, UsdStandardSymbolValue },
        { BritishCulture, GbpStandardSymbolValue },
        { JapaneseCulture, JpyStandardSymbolValue },
        { KoreanCulture, KrwStandardSymbolValue },
    };

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

    #region Test: TryParse_Sign

    private const decimal NegativeFive = -5m;

    [Theory]
    [MemberData(nameof(SingleSignTestCases))]
    public void TryParse_InputContainsOneSign_ReturnsTrueAndNegatesAmount(MoneyStyles testStyles, string testValue)
    {
        // act
        var testResult = Money.TryParse(testValue, testStyles, MoneyInfoFor(UnitedStatesCulture), out var result);

        // assert
        testResult.Should().BeTrue("{0} carries one sign", testValue);
        ((decimal)result).Should().Be(NegativeFive);
    }

    public static TheoryData<MoneyStyles, string> SingleSignTestCases => new()
    {
        // A sign outside the currency token is found before the token is removed.
        { MoneyStyles.Any, "-$5" },
        // A sign beside the amount is found after the currency token is removed.
        { MoneyStyles.Any, "$-5" },
        { MoneyStyles.Currency, "5-" },
        // Parentheses are a sign.
        { MoneyStyles.Currency, "(5)" },
    };

    [Theory]
    [MemberData(nameof(MultipleSignTestCases))]
    public void TryParse_InputContainsMoreThanOneSign_ReturnsFalse(MoneyStyles testStyles, string testValue)
    {
        // act
        var testResult = Money.TryParse(testValue, testStyles, MoneyInfoFor(UnitedStatesCulture), out _);

        // assert
        testResult.Should().BeFalse("a value carries at most one sign, and parentheses are a sign");
    }

    [Theory]
    [MemberData(nameof(MultipleSignTestCases))]
    public void Parse_InputContainsMoreThanOneSign_ThrowsFormatException(MoneyStyles testStyles, string testValue)
    {
        // arrange
        var testProvider = MoneyInfoFor(UnitedStatesCulture);

        // act
        Action testAction = () => Money.Parse(testValue, testStyles, testProvider);

        // assert
        testAction.Should().Throw<FormatException>();
    }

    public static TheoryData<MoneyStyles, string> MultipleSignTestCases => new()
    {
        { MoneyStyles.Currency, "--5" },
        { MoneyStyles.Currency, "-5-" },
        { MoneyStyles.Any, "-$-5" },
        { MoneyStyles.Currency, "(-5)" },
    };

    // NumberFormatInfo reads a hyphen-minus as the sign where the sign is a single dash such as U+2212.
    private const string MinusSign = "\x2212";
    private const string FullwidthHyphenMinus = "\xFF0D";
    private const string LeftToRightMarkHyphenMinus = "\x200E-";

    [Theory]
    [MemberData(nameof(HyphenMinusForDashSignTestCases))]
    public void TryParse_HyphenMinusWhereSignIsADash_ReturnsTrueAndNegatesAmount(string testNegativeSign, MoneyStyles testStyles, string testValue)
    {
        // arrange
        var testProvider = new CurrencyFormatInfo { NegativeSign = testNegativeSign };

        // act
        var testResult = Money.TryParse(testValue, testStyles, testProvider, out var result);

        // assert
        testResult.Should().BeTrue("decimal reads a hyphen-minus as the sign {0}", testNegativeSign);
        ((decimal)result).Should().Be(NegativeFive);
    }

    public static TheoryData<string, MoneyStyles, string> HyphenMinusForDashSignTestCases => new()
    {
        { MinusSign, MoneyStyles.AllowLeadingSign, "-5" },
        { MinusSign, MoneyStyles.AllowTrailingSign, "5-" },
        { FullwidthHyphenMinus, MoneyStyles.AllowLeadingSign, "-5" },
        // The sign itself is still read: U+2212 then 5 (an escape takes at most four hex digits).
        { MinusSign, MoneyStyles.AllowLeadingSign, "\x22125" },
    };

    [Fact]
    public void TryParse_HyphenMinusWhereSignHasABidiMark_ReturnsFalse()
    {
        // arrange
        var testProvider = new CurrencyFormatInfo { NegativeSign = LeftToRightMarkHyphenMinus };

        // act
        var testResult = Money.TryParse("-5", MoneyStyles.AllowLeadingSign, testProvider, out _);

        // assert
        testResult.Should().BeFalse("decimal reads only the sign itself where it is not a single dash");
    }

    #endregion

    #region Test: TryParse_WhiteSpace

    private const decimal Five = 5m;

    // NumberStyles reads U+0009 to U+000D and U+0020 as white space, and no other character.
    private const string EmSpace = "\x2003";
    private const string NoBreakSpace = "\x00A0";

    private const string NzdCode = "NZD";
    private const MoneyStyles CurrencyCodeWithoutWhiteStyles = MoneyStyles.AllowCurrencyCode;
    private const MoneyStyles CurrencyCodeAndTrailingSignStyles = MoneyStyles.AllowCurrencyCode | MoneyStyles.AllowTrailingSign;

    // The NumberFormatInfo.NumberNegativePattern "- n", under which NumberStyles reads white space between a
    // leading sign and the number.
    private const int SpaceAfterSignNumberNegativePattern = 2;

    /// <summary>
    /// An en-US culture whose number formatting has the negative pattern "- n".
    /// </summary>
    private static CultureInfo SpaceAfterSignCulture()
    {
        var culture = new CultureInfo(UnitedStatesCulture);
        culture.NumberFormat.NumberNegativePattern = SpaceAfterSignNumberNegativePattern;
        return culture;
    }

    // Each row is rejected by decimal.TryParse with the matching NumberStyles, the invariant culture and $ for
    // the currency token.
    [Theory]
    [MemberData(nameof(DisallowedWhiteSpaceTestCases))]
    public void TryParse_WhiteSpaceTheStylesDoNotAllow_ReturnsFalse(string testValue, MoneyStyles testStyles)
    {
        // act
        var testResult = Money.TryParse(testValue, testStyles, MoneyInfo.InvariantMoney, out _);

        // assert
        testResult.Should().BeFalse("decimal.TryParse with the matching NumberStyles rejects {0}", testValue);
    }

    public static TheoryData<string, MoneyStyles> DisallowedWhiteSpaceTestCases => new()
    {
        // After an opening parenthesis that no currency token precedes, whatever the styles.
        { "( 5 )", MoneyStyles.AllowParentheses },
        { "( 5)", MoneyStyles.Currency },
        { "( " + NzdCode + "5)", MoneyStyles.Currency },
        // After a leading sign that no currency token precedes, whatever the styles.
        { "-" + EmSpace + EmSpace + "5", MoneyStyles.AllowLeadingSign },
        { "- 5", MoneyStyles.Currency },
        { "- " + NzdCode + "5", MoneyStyles.Currency },
        // Without AllowTrailingWhite, before a closing parenthesis or trailing sign.
        { "(5 )", MoneyStyles.AllowParentheses },
        { "5 -", MoneyStyles.AllowTrailingSign },
        { NzdCode + "5 -", CurrencyCodeAndTrailingSignStyles },
        // Without AllowLeadingWhite or AllowTrailingWhite, between a currency token and the amount.
        { NzdCode + " 5", CurrencyCodeWithoutWhiteStyles },
        { "5 " + NzdCode, CurrencyCodeWithoutWhiteStyles },
        // Characters that are white space to Unicode but not to NumberStyles.
        { EmSpace + "5", MoneyStyles.AllowLeadingWhite },
        { "5" + EmSpace, MoneyStyles.AllowTrailingWhite },
        { NoBreakSpace + "5", MoneyStyles.Currency },
        { NzdCode + EmSpace + "5", MoneyStyles.Currency },
    };

    [Theory]
    [MemberData(nameof(AllowedWhiteSpaceTestCases))]
    public void TryParse_WhiteSpaceTheStylesAllow_ReturnsTrueAndReadsAmount(string testValue, MoneyStyles testStyles, decimal expectedAmount)
    {
        // act
        var testResult = Money.TryParse(testValue, testStyles, MoneyInfo.InvariantMoney, out var result);

        // assert
        testResult.Should().BeTrue("the styles allow the white space in {0}", testValue);
        ((decimal)result).Should().Be(expectedAmount);
    }

    public static TheoryData<string, MoneyStyles, decimal> AllowedWhiteSpaceTestCases => new()
    {
        // Each NumberStyles white-space character at the outer ends.
        { "\t5", MoneyStyles.AllowLeadingWhite, Five },
        { "\n5", MoneyStyles.AllowLeadingWhite, Five },
        { "\v5", MoneyStyles.AllowLeadingWhite, Five },
        { "\f5", MoneyStyles.AllowLeadingWhite, Five },
        { "\r5", MoneyStyles.AllowLeadingWhite, Five },
        { " 5", MoneyStyles.AllowLeadingWhite, Five },
        { "5 ", MoneyStyles.AllowTrailingWhite, Five },
        { " (5) ", MoneyStyles.Currency, NegativeFive },
        // Under AllowTrailingWhite, anywhere after the amount.
        { "(5 )", MoneyStyles.Currency, NegativeFive },
        { "5 -", MoneyStyles.Currency, NegativeFive },
        { NzdCode + "5 -", MoneyStyles.Currency, NegativeFive },
        { "5  " + NzdCode, MoneyStyles.Currency, Five },
        // Under AllowLeadingWhite, after a currency token, including either side of a sign that follows it.
        { NzdCode + "\t5", MoneyStyles.Currency, Five },
        { NzdCode + " - 5", MoneyStyles.Currency, NegativeFive },
    };

    [Theory]
    [MemberData(nameof(SpaceAfterSignNumberPatternTestCases))]
    public void TryParse_WhiteAfterSignWhereNumberPatternIsSpaceAfterSign_ReturnsTrueAndReadsAmount(string testValue)
    {
        // arrange
        var testProvider = new MoneyInfo(SpaceAfterSignCulture());

        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, testProvider, out var result);

        // assert
        testResult.Should().BeTrue("decimal.TryParse reads white space after the sign where the number pattern is \"- n\"");
        ((decimal)result).Should().Be(NegativeFive);
    }

    public static TheoryData<string> SpaceAfterSignNumberPatternTestCases => new()
    {
        "- 5",
        "- " + NzdCode + "5",
    };

    // The euro sign, the ISO 4217 code of the euro, and 5 as the invariant currency formatting renders it.
    private const string EuroSign = "\x20AC";
    private const string EuroCode = "EUR";
    private const string FiveRendering = "5.00";

    // Every NumberFormatInfo.CurrencyPositivePattern and CurrencyNegativePattern layout, as the C and I formats
    // render EUR 5 and EUR -5 under the invariant separators and hyphen-minus sign. The H format renders EUR
    // with the same symbol as C.
    [Theory]
    [MemberData(nameof(CurrencyPatternRenderingTestCases))]
    public void TryParse_CurrencyPatternRendering_ReturnsTrueAndReadsAmount(string testValue, decimal expectedAmount)
    {
        // act
        var testResult = Money.TryParse(testValue, MoneyStyles.Currency, MoneyInfo.InvariantMoney, out var result);

        // assert
        testResult.Should().BeTrue("{0} is a rendering of a currency pattern", testValue);
        result.Should().Be(Money.Create(expectedAmount, Iso4217CurrencyCurrent.EUR));
    }

    public static TheoryData<string, decimal> CurrencyPatternRenderingTestCases => new()
    {
        // Positive patterns 0 to 3.
        { EuroSign + FiveRendering, Five },
        { FiveRendering + EuroSign, Five },
        { EuroSign + " " + FiveRendering, Five },
        { FiveRendering + " " + EuroSign, Five },
        { EuroCode + FiveRendering, Five },
        { FiveRendering + EuroCode, Five },
        { EuroCode + " " + FiveRendering, Five },
        { FiveRendering + " " + EuroCode, Five },
        // Negative patterns 0 to 16.
        { "(" + EuroSign + FiveRendering + ")", NegativeFive },
        { "-" + EuroSign + FiveRendering, NegativeFive },
        { EuroSign + "-" + FiveRendering, NegativeFive },
        { EuroSign + FiveRendering + "-", NegativeFive },
        { "(" + FiveRendering + EuroSign + ")", NegativeFive },
        { "-" + FiveRendering + EuroSign, NegativeFive },
        { FiveRendering + "-" + EuroSign, NegativeFive },
        { FiveRendering + EuroSign + "-", NegativeFive },
        { "-" + FiveRendering + " " + EuroSign, NegativeFive },
        { "-" + EuroSign + " " + FiveRendering, NegativeFive },
        { FiveRendering + " " + EuroSign + "-", NegativeFive },
        { EuroSign + " " + FiveRendering + "-", NegativeFive },
        { EuroSign + " -" + FiveRendering, NegativeFive },
        { FiveRendering + "- " + EuroSign, NegativeFive },
        { "(" + EuroSign + " " + FiveRendering + ")", NegativeFive },
        { "(" + FiveRendering + " " + EuroSign + ")", NegativeFive },
        { EuroSign + "- " + FiveRendering, NegativeFive },
        { "(" + EuroCode + FiveRendering + ")", NegativeFive },
        { "-" + EuroCode + FiveRendering, NegativeFive },
        { EuroCode + "-" + FiveRendering, NegativeFive },
        { EuroCode + FiveRendering + "-", NegativeFive },
        { "(" + FiveRendering + EuroCode + ")", NegativeFive },
        { "-" + FiveRendering + EuroCode, NegativeFive },
        { FiveRendering + "-" + EuroCode, NegativeFive },
        { FiveRendering + EuroCode + "-", NegativeFive },
        { "-" + FiveRendering + " " + EuroCode, NegativeFive },
        { "-" + EuroCode + " " + FiveRendering, NegativeFive },
        { FiveRendering + " " + EuroCode + "-", NegativeFive },
        { EuroCode + " " + FiveRendering + "-", NegativeFive },
        { EuroCode + " -" + FiveRendering, NegativeFive },
        { FiveRendering + "- " + EuroCode, NegativeFive },
        { "(" + EuroCode + " " + FiveRendering + ")", NegativeFive },
        { "(" + FiveRendering + " " + EuroCode + ")", NegativeFive },
        { EuroCode + "- " + FiveRendering, NegativeFive },
    };

    #endregion
}
