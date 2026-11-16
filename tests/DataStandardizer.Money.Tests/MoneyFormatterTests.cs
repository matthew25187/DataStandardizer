using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class MoneyFormatterTests
{
    private const string NewZealandCulture = "en-NZ";
    private const string GermanCulture = "de-DE";
    private const string IndianCulture = "en-IN";
    private const string DutchCulture = "nl-NL";
    private const string UnitedStatesCulture = "en-US";

    private const decimal TestAmount = 1234.5m;
    private const decimal NegativeTestAmount = -1234.5m;
    private const decimal SevenDigitAmount = 1234567.5m;
    private const decimal OneUnitAmount = 1m;
    private const decimal FiveAmount = 5m;
    private const decimal NegativeFiveAmount = -5m;

    // An amount which rounds to 1.01 at a precision of two decimal places, whatever the midpoint rounding.
    private const decimal UnroundedAmount = 1.006m;
    private const int RoundingToCents = 2;

    private const string StandardSymbolFormat = "C";
    private const string NarrowSymbolFormat = "H";
    private const string CurrencyCodeFormat = "I";
    private const string CurrencyNameFormat = "N";
    private const string GeneralFormat = "G";
    private const string NoDecimalsStandardSymbolFormat = "C0";

    // The representations of TestAmount in New Zealand Dollars under the en-NZ culture.
    private const string NzdStandardSymbolResult = "NZ$1,234.50";
    private const string NarrowDollarResult = "$1,234.50";
    private const string NzdCurrencyCodeResult = "NZD1,234.50";
    private const string NzdCurrencyNameResult = "New Zealand Dollar1,234.50";
    private const string NzdNoDecimalsResult = "NZ$1,235";
    private const string GeneralResult = "1234.5";
    private const string UsdFiveResult = "$5.00";

    private const string ChfResult = "CHF1,234.50";

    // CHF has the narrow symbol "Fr." in the Unicode CLDR 49.0.0 currency data for the en locale.
    private const string ChfNarrowResult = "Fr.1,234.50";
    private const string GermanNzdResult = "1.234,50 NZ$";
    private const string SymbolSpaceAmountNzdResult = "NZ$ 1.234,50";
    private const string IndianGroupedNzdResult = "NZ$12,34,567.50";
    private const string GroupedAmountResult = "1,234.50";
    private const string EuroOneUnitSymbolResult = "\x20AC1.00";

    private const string MinusSign = "\x2212";

    // TestAmount with the decimal separator "," and the group separator ".".
    private const string CommaDecimalGroupedAmountResult = "1.234,50";

    // NegativeTestAmount with the negative sign U+2212 MINUS SIGN.
    private const string MinusSignGroupedAmountResult = "\x22121,234.50";

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

    #region Test: Format_StandardSpecifier

    [Theory]
    [InlineData(StandardSymbolFormat, NzdStandardSymbolResult)]
    [InlineData("c", NzdStandardSymbolResult)]
    [InlineData(NarrowSymbolFormat, NarrowDollarResult)]
    [InlineData("h", NarrowDollarResult)]
    [InlineData(CurrencyCodeFormat, NzdCurrencyCodeResult)]
    [InlineData("i", NzdCurrencyCodeResult)]
    [InlineData(CurrencyNameFormat, NzdCurrencyNameResult)]
    [InlineData(GeneralFormat, GeneralResult)]
    [InlineData(null, GeneralResult)]
    public void Format_StandardSpecifier_ReturnsExpectedRepresentation(string? testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(testFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(expectedResult, "the format {0} was requested", testFormat);
    }

    #endregion

    #region Test: Format_CurrencySymbolSpecifier

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD, NzdStandardSymbolResult)]
    [InlineData(Iso4217CurrencyCurrent.CAD, "CA$1,234.50")]
    [InlineData(Iso4217CurrencyCurrent.CHF, ChfResult)]
    public void Format_StandardSymbolSpecifier_UsesStandardSymbol(Iso4217CurrencyCurrent testCurrency, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, testCurrency);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(expectedResult, "the standard symbol is unambiguous");
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD, NarrowDollarResult)]
    [InlineData(Iso4217CurrencyCurrent.CAD, NarrowDollarResult)]
    [InlineData(Iso4217CurrencyCurrent.CHF, ChfNarrowResult)]
    public void Format_NarrowSymbolSpecifier_UsesNarrowSymbol(Iso4217CurrencyCurrent testCurrency, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, testCurrency);

        // act
        var testResult = testSubject.ToString(NarrowSymbolFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(expectedResult, "the narrow symbol is the shortest recognisable form");
    }

    // CLDR root symbols: '$', '£', '¥' and '₩' are the standard symbols of USD, GBP, JPY and KRW, and the narrow
    // symbols of other currencies too. MoneyParsingTests reads these renderings back.

    private const string BritishCulture = "en-GB";
    private const string JapaneseCulture = "ja-JP";
    private const string KoreanCulture = "ko-KR";
    private const string DanishCulture = "da-DK";

    private const decimal WholeAmount = 500m; // a whole amount, so currencies without minor units render it unrounded
    private const string UsdSymbolResult = "$500.00";

    [Theory]
    [MemberData(nameof(StandardSymbolSharedAsNarrowSymbolTestCases))]
    public void Format_StandardSymbolSpecifierForSymbolSharedAsNarrowSymbol_UsesStandardSymbol(string testCulture, Iso4217CurrencyCurrent testCurrency, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(WholeAmount, testCurrency);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the currency's standard symbol is rendered under the culture {0}", testCulture);
    }

    public static TheoryData<string, Iso4217CurrencyCurrent, string> StandardSymbolSharedAsNarrowSymbolTestCases => new()
    {
        { UnitedStatesCulture, Iso4217CurrencyCurrent.USD, UsdSymbolResult },
        { BritishCulture, Iso4217CurrencyCurrent.GBP, "\x00A3500.00" },
        { JapaneseCulture, Iso4217CurrencyCurrent.JPY, "\x00A5500" },
        { KoreanCulture, Iso4217CurrencyCurrent.KRW, "\x20A9500" },
    };

    [Theory]
    [InlineData(UnitedStatesCulture, Iso4217CurrencyCurrent.USD, UsdSymbolResult)]
    [InlineData(DanishCulture, Iso4217CurrencyCurrent.DKK, "500,00 kr")]
    public void Format_NarrowSymbolSpecifierInTheCurrencysCulture_UsesNarrowSymbol(string testCulture, Iso4217CurrencyCurrent testCurrency, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(WholeAmount, testCurrency);

        // act
        var testResult = testSubject.ToString(NarrowSymbolFormat, MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the currency's narrow symbol is rendered under the culture {0}", testCulture);
    }

    [Fact]
    public void Format_CurrencySymbolSpecifierForCurrencyWithoutSymbol_FallsBackToCurrencyCode()
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.CHF);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(ChfResult, "a currency without a distinct symbol is denoted by its code");
    }

    #endregion

    #region Test: Format_PrecisionSpecifier

    [Theory]
    [InlineData(NoDecimalsStandardSymbolFormat, NzdNoDecimalsResult)]
    [InlineData("C2", NzdStandardSymbolResult)]
    [InlineData("C4", "NZ$1,234.5000")]
    [InlineData("I3", "NZD1,234.500")]
    public void Format_PrecisionSpecifier_AppliesRequestedPrecision(string testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(testFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD, NzdStandardSymbolResult)]
    [InlineData(Iso4217CurrencyCurrent.JPY, "\x00A51,235")]
    [InlineData(Iso4217CurrencyCurrent.KWD, "KWD1,234.500")]
    public void Format_NoPrecisionSpecifier_UsesMinorUnitsOfCurrency(Iso4217CurrencyCurrent testCurrency, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, testCurrency);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(expectedResult, "the default precision is the number of minor units of {0}", testCurrency);
    }

    #endregion

    #region Test: Format_DisplayRounding

    // 1.0049 rounds to 1.005 at three decimal places, a midpoint at two.
    private const decimal ThreeDigitMidpointAmount = 1.0049m;
    private const int RoundingToThreeDigits = 3;

    // 1.125 is a midpoint at two decimal places, and is unchanged by rounding at four.
    private const decimal TwoDigitMidpointAmount = 1.125m;
    private const int RoundingToFourDigits = 4;

    private const string TwoDecimalsStandardSymbolFormat = "C2";

    // 1.125 rounded to two decimal places away from zero, as decimal rounds it.
    private const string TwoDigitMidpointAwayFromZeroResult = "$1.13";

    [Theory]
    [MemberData(nameof(DisplayRoundingTestCases))]
    public void Format_ValueRoundedToGreaterPrecisionThanDisplayed_RoundsMidpointAwayFromZeroAsDecimalDoes(Money testSubject, string testFormat, string expectedResult)
    {
        // arrange
        var testProvider = MoneyInfoFor(UnitedStatesCulture);

        // act
        var testResult = testSubject.ToString(testFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "the rounded amount is rounded again to the display precision as decimal rounds it, whatever the value's rounding method");
    }

    public static TheoryData<Money, string, string> DisplayRoundingTestCases => new()
    {
        { Money.Create(ThreeDigitMidpointAmount, Iso4217CurrencyCurrent.USD, RoundingToThreeDigits, MidpointRounding.ToEven), StandardSymbolFormat, "$1.01" },
        { Money.Create(TwoDigitMidpointAmount, Iso4217CurrencyCurrent.USD, RoundingToFourDigits, MidpointRounding.ToEven), StandardSymbolFormat, TwoDigitMidpointAwayFromZeroResult },
        { Money.Create(TwoDigitMidpointAmount, Iso4217CurrencyCurrent.USD, RoundingToFourDigits, MidpointRounding.ToEven), TwoDecimalsStandardSymbolFormat, TwoDigitMidpointAwayFromZeroResult },
        { Money.Create(TwoDigitMidpointAmount, Iso4217CurrencyCurrent.USD, RoundingToCents, MidpointRounding.ToEven), StandardSymbolFormat, "$1.12" },
    };

    #endregion

    #region Test: Format_CurrencyCodeFormat

    [Fact]
    public void Format_CurrencyCodeFormatMatchingTheValue_ReturnsCurrencyCodeRepresentation()
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(nameof(Iso4217CurrencyCurrent.NZD), MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(NzdCurrencyCodeResult);
    }

    [Fact]
    public void Format_CurrencyCodeFormatNotMatchingTheValue_ThrowsFormatException()
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        Action testAction = () => testSubject.ToString(nameof(Iso4217CurrencyCurrent.USD), MoneyInfoFor(NewZealandCulture));

        // assert
        testAction.Should().Throw<FormatException>("the format names a currency other than that of the value");
    }

    #endregion

    #region Test: Format_CultureGovernsPresentationOnly

    [Theory]
    [InlineData(NewZealandCulture, "NZ$1,234,567.50")]
    [InlineData(IndianCulture, IndianGroupedNzdResult)]
    public void Format_ValueOfForeignCurrency_AppliesCultureGroupingButRetainsCurrencyOfValue(string testCulture, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(SevenDigitAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the culture {0} governs grouping, and the value governs the currency", testCulture);
    }

    [Fact]
    public void Format_ValueOfCurrencyWithoutMinorUnitsUnderCultureWithMinorUnits_UsesMinorUnitsOfCurrency()
    {
        // arrange
        var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.JPY);

        // act
        var testResult = testSubject.ToString(CurrencyCodeFormat, MoneyInfoFor(IndianCulture));

        // assert
        testResult.Should().Be("JPY1,234", "the currency has no minor units even though the culture has two");
    }

    #endregion

    #region Test: Format_CulturePatterns

    [Theory]
    [InlineData(NewZealandCulture, NzdStandardSymbolResult)]
    [InlineData(GermanCulture, GermanNzdResult)]
    [InlineData("pt-BR", SymbolSpaceAmountNzdResult)]
    [InlineData(DutchCulture, SymbolSpaceAmountNzdResult)]
    public void Format_PositiveValue_AppliesCurrencyPositivePatternOfCulture(string testCulture, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the culture {0} places the currency token according to its own pattern", testCulture);
    }

    [Theory]
    [InlineData(NewZealandCulture, "-NZ$1,234.50")]
    [InlineData(GermanCulture, "-1.234,50 NZ$")]
    [InlineData(DutchCulture, "NZ$ -1.234,50")]
    public void Format_NegativeValue_AppliesCurrencyNegativePatternOfCulture(string testCulture, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(NegativeTestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the culture {0} denotes a negative value according to its own pattern", testCulture);
    }

    [Fact]
    public void Format_NegativeValueForCultureWithNonAsciiNegativeSign_UsesNegativeSignOfCulture()
    {
        // arrange
        const int amountSpaceSymbolPositivePattern = 3; // "n $", as in NumberFormatInfo.CurrencyPositivePattern
        const int negativeAmountSpaceSymbolNegativePattern = 8; // "-n $", as in NumberFormatInfo.CurrencyNegativePattern
        var testSubject = Money.Create(NegativeTestAmount, Iso4217CurrencyCurrent.NZD);
        var testProvider = new MoneyInfo
        {
            CurrencyFormat = new CurrencyFormatInfo
            {
                CurrencyCode = nameof(Iso4217CurrencyCurrent.SEK),
                CurrencyDecimalDigits = 2,
                CurrencyDecimalSeparator = ",",
                CurrencyGroupSeparator = " ",
                CurrencyGroupSizes = new[] { 3 },
                CurrencyNegativePattern = negativeAmountSpaceSymbolNegativePattern,
                CurrencyPositivePattern = amountSpaceSymbolPositivePattern,
                NegativeSign = MinusSign
            }
        };

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, testProvider);

        // assert
        testResult.Should().StartWith(MinusSign, "the negative sign of the culture is a minus sign rather than a hyphen");
    }

    [Theory]
    [MemberData(nameof(NegativePatternTestCases))]
    public void Format_NegativeValueWithEachNegativePattern_PlacesTheTokenAsNumberFormatInfoDoes(int testPattern, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(NegativeFiveAmount, Iso4217CurrencyCurrent.USD);
        var testProvider = new CurrencyFormatInfo { CurrencyNegativePattern = testPattern };

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "pattern {0} has the layout NumberFormatInfo.CurrencyNegativePattern gives it", testPattern);
    }

    [Theory]
    [MemberData(nameof(PositivePatternTestCases))]
    public void Format_PositiveValueWithEachPositivePattern_PlacesTheTokenAsNumberFormatInfoDoes(int testPattern, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(FiveAmount, Iso4217CurrencyCurrent.USD);
        var testProvider = new CurrencyFormatInfo { CurrencyPositivePattern = testPattern };

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "pattern {0} has the layout NumberFormatInfo.CurrencyPositivePattern gives it", testPattern);
    }

    // Every index NumberFormatInfo.CurrencyNegativePattern accepts, 0 to 16, with the layout it documents, for
    // USD 5 under the invariant separators and hyphen-minus sign.
    public static TheoryData<int, string> NegativePatternTestCases => new()
    {
        { 0, "($5.00)" },
        { 1, "-$5.00" },
        { 2, "$-5.00" },
        { 3, "$5.00-" },
        { 4, "(5.00$)" },
        { 5, "-5.00$" },
        { 6, "5.00-$" },
        { 7, "5.00$-" },
        { 8, "-5.00 $" },
        { 9, "-$ 5.00" },
        { 10, "5.00 $-" },
        { 11, "$ 5.00-" },
        { 12, "$ -5.00" },
        { 13, "5.00- $" },
        { 14, "($ 5.00)" },
        { 15, "(5.00 $)" },
        { 16, "$- 5.00" }
    };

    // Every index NumberFormatInfo.CurrencyPositivePattern accepts, 0 to 3, with the layout it documents.
    public static TheoryData<int, string> PositivePatternTestCases => new()
    {
        { 0, UsdFiveResult },
        { 1, "5.00$" },
        { 2, "$ 5.00" },
        { 3, "5.00 $" }
    };

    #endregion

    #region Test: Format_NegativeValueRoundingToZero

    [Theory]
    [MemberData(nameof(NegativeValueRoundingToZeroTestCases), DisableDiscoveryEnumeration = true)]
    public void Format_NegativeValueRoundingToZeroAtDisplayPrecision_AppliesPositivePattern(Money testSubject, string testFormat, IFormatProvider testProvider, string expectedResult)
    {
        // arrange

        // act
        var testResult = testSubject.ToString(testFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "a value shown as zero is not negative, as for decimal");
    }

    [Theory]
    [MemberData(nameof(NegativeValueNonZeroAtDisplayPrecisionTestCases), DisableDiscoveryEnumeration = true)]
    public void Format_NegativeValueNonZeroAtDisplayPrecision_AppliesNegativePattern(Money testSubject, string testFormat, IFormatProvider testProvider, string expectedResult)
    {
        // arrange

        // act
        var testResult = testSubject.ToString(testFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "a value shown as a nonzero amount keeps its sign");
    }

    // An amount which is negative but rounds to zero at a precision of two decimal places, and one which rounds
    // to zero at a precision of none.
    private const decimal NegativeAmountRoundingToZeroCents = -0.001m;
    private const decimal NegativeAmountRoundingToZeroUnits = -0.4m;

    // An amount which is negative and rounds to -0.01 at a precision of two decimal places, whatever the midpoint rounding.
    private const decimal NegativeAmountRoundingToOneCent = -0.006m;

    // Negative midpoints at a precision of two decimal places and of none, which round away from zero as decimal rounds them.
    private const decimal NegativeMidpointCentAmount = -0.005m;
    private const decimal NegativeMidpointUnitAmount = -0.5m;

    private const int ParenthesesNegativePattern = 0; // "($n)", as in NumberFormatInfo.CurrencyNegativePattern

    // A precision greater than the 28 digits a decimal holds after the decimal point.
    private const string BeyondDecimalScaleStandardSymbolFormat = "C29";

    private const string UsdZeroCentsResult = "$0.00";
    private const string UsdNegativeOneCentResult = "-$0.01";
    private const string NegativeOneCentResult = "-0.01";

    // USD -5 at a precision of 29 decimal places.
    private const string UsdNegativeFiveAtBeyondDecimalScaleResult = "-$5.00000000000000000000000000000";

    public static TheoryData<Money, string, IFormatProvider, string> NegativeValueRoundingToZeroTestCases => new()
    {
        { Money.Create(NegativeAmountRoundingToZeroCents, Iso4217CurrencyCurrent.USD), StandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), UsdZeroCentsResult },
        { Money.Create(NegativeAmountRoundingToZeroCents, Iso4217CurrencyCurrent.USD), StandardSymbolFormat, new CurrencyFormatInfo { CurrencyNegativePattern = ParenthesesNegativePattern }, UsdZeroCentsResult },
        { Money.Create(NegativeAmountRoundingToZeroUnits, Iso4217CurrencyCurrent.USD), NoDecimalsStandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), "$0" },
        { Money.Create(NegativeAmountRoundingToZeroCents), StandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), "0.00" },
        { Money.Create(NegativeAmountRoundingToZeroUnits), NoDecimalsStandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), "0" }
    };

    public static TheoryData<Money, string, IFormatProvider, string> NegativeValueNonZeroAtDisplayPrecisionTestCases => new()
    {
        { Money.Create(NegativeAmountRoundingToOneCent, Iso4217CurrencyCurrent.USD), StandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), UsdNegativeOneCentResult },
        { Money.Create(NegativeAmountRoundingToOneCent), StandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), NegativeOneCentResult },
        { Money.Create(NegativeMidpointCentAmount, Iso4217CurrencyCurrent.USD), StandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), UsdNegativeOneCentResult },
        { Money.Create(NegativeMidpointCentAmount), StandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), NegativeOneCentResult },
        { Money.Create(NegativeMidpointUnitAmount, Iso4217CurrencyCurrent.USD), NoDecimalsStandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), "-$1" },
        { Money.Create(NegativeFiveAmount, Iso4217CurrencyCurrent.USD), BeyondDecimalScaleStandardSymbolFormat, MoneyInfoFor(UnitedStatesCulture), UsdNegativeFiveAtBeyondDecimalScaleResult }
    };

    #endregion

    #region Test: Format_ValueWithoutCurrency

    [Theory]
    [InlineData(StandardSymbolFormat)]
    [InlineData(NarrowSymbolFormat)]
    [InlineData(CurrencyCodeFormat)]
    [InlineData(CurrencyNameFormat)]
    public void Format_ValueWithoutCurrency_ReturnsAmountWithoutCurrencyToken(string testFormat)
    {
        // arrange
        var testSubject = Money.Create(TestAmount);

        // act
        var testResult = testSubject.ToString(testFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(GroupedAmountResult, "a value with no currency has no currency token to denote");
    }

    [Theory]
    [MemberData(nameof(ValueWithoutCurrencyNumberFormattingTestCases), DisableDiscoveryEnumeration = true)]
    public void Format_ValueWithoutCurrencyWhereNumberAndCurrencySeparatorsDiffer_UsesTheSeparatorsOfTheGeneralFormat(decimal testAmount, string testFormat, IFormatProvider testProvider, string expectedResult)
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(new CultureInfo(UnitedStatesCulture));
        var testSubject = Money.Create(testAmount);

        // act
        var testResult = testSubject.ToString(testFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "without a currency token the amount is presented as the general format presents it, so that it reads back the same");
    }

    // The currency formatting of en-US, from Unicode CLDR, has the decimal separator "." and the group separator ",".
    // Each provider's number formatting has the decimal separator "," and the group separator ".", or the sign U+2212.
    public static TheoryData<decimal, string, IFormatProvider, string> ValueWithoutCurrencyNumberFormattingTestCases => new()
    {
        { TestAmount, StandardSymbolFormat, CommaDecimalCulture(), CommaDecimalGroupedAmountResult },
        { TestAmount, NarrowSymbolFormat, CommaDecimalCulture(), CommaDecimalGroupedAmountResult },
        { TestAmount, CurrencyCodeFormat, CommaDecimalCulture(), CommaDecimalGroupedAmountResult },
        { TestAmount, CurrencyNameFormat, CommaDecimalCulture(), CommaDecimalGroupedAmountResult },
        { TestAmount, StandardSymbolFormat, new MoneyInfo(CommaDecimalCulture()), CommaDecimalGroupedAmountResult },
        { TestAmount, StandardSymbolFormat, CommaDecimalCulture().NumberFormat, CommaDecimalGroupedAmountResult },
        { NegativeTestAmount, StandardSymbolFormat, MinusSignNumberFormat(), MinusSignGroupedAmountResult },
    };

    #endregion

    #region Test: Format_OtherFormats

    [Theory]
    [InlineData("#,##0.00", GroupedAmountResult)]
    [InlineData("0.000", "1234.500")]
    public void Format_CustomNumericFormat_FormatsAmountAsNumber(string testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(testFormat, CultureInfo.InvariantCulture);

        // assert
        testResult.Should().Be(expectedResult, "a custom numeric format applies to the amount");
    }

    [Theory]
    [InlineData("Q")]
    [InlineData("C999")]
    public void Format_InvalidFormat_ThrowsFormatException(string testFormat)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        Action testAction = () => testSubject.ToString(testFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testAction.Should().Throw<FormatException>("the format string is not valid");
    }

    [Fact]
    public void Format_ArgumentIsNotMoneyValue_FormatsTheArgument()
    {
        // arrange
        var testSubject = new MoneyFormatter();

        // act
        var testResult = testSubject.Format("N2", TestAmount, CultureInfo.InvariantCulture);

        // assert
        testResult.Should().Be(GroupedAmountResult, "an argument which is not a monetary value is formatted as itself");
    }

    [Fact]
    public void Format_ArgumentIsNull_ReturnsEmptyString()
    {
        // arrange
        var testSubject = new MoneyFormatter();

        // act
        var testResult = testSubject.Format(StandardSymbolFormat, null, CultureInfo.InvariantCulture);

        // assert
        testResult.Should().BeEmpty();
    }

    [Fact]
    public void Format_MoneyValueWithUnrecognisedFormat_FormatsAmountWithoutRecursing()
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        // A monetary value is itself formattable, so resolving an unrecognised format through the
        // formattable argument would resolve this formatter again and would not terminate.
        var testResult = testSubject.ToString("0.00", MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be("1234.50", "an unrecognised format applies to the amount without recursing through the formatter");
    }

    #endregion

    #region Test: Format_Alignment

    [Theory]
    [MemberData(nameof(InvocationFormTestCases), DisableDiscoveryEnumeration = true)]
    public void Format_EveryInvocationForm_ProducesTheSameRepresentation(Func<Money, IFormatProvider, string> testInvocation, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);
        var testProvider = MoneyInfoFor(NewZealandCulture);

        // act
        var testResult = testInvocation(testSubject, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "ToString, IFormattable and string.Format must agree");
    }

    public static TheoryData<Func<Money, IFormatProvider, string>, string> InvocationFormTestCases => new()
    {
        { (value, provider) => value.ToString(StandardSymbolFormat, provider), NzdStandardSymbolResult },
        { (value, provider) => ((IFormattable)value).ToString(StandardSymbolFormat, provider), NzdStandardSymbolResult },
        { (value, provider) => string.Format(provider, "{0:C}", value), NzdStandardSymbolResult },
        { (value, provider) => value.ToString(NarrowSymbolFormat, provider), NarrowDollarResult },
        { (value, provider) => ((IFormattable)value).ToString(NarrowSymbolFormat, provider), NarrowDollarResult },
        { (value, provider) => string.Format(provider, "{0:H}", value), NarrowDollarResult },
        { (value, provider) => value.ToString(CurrencyCodeFormat, provider), NzdCurrencyCodeResult },
        { (value, provider) => ((IFormattable)value).ToString(CurrencyCodeFormat, provider), NzdCurrencyCodeResult },
        { (value, provider) => string.Format(provider, "{0:I}", value), NzdCurrencyCodeResult },
        { (value, provider) => value.ToString(CurrencyNameFormat, provider), NzdCurrencyNameResult },
        { (value, provider) => ((IFormattable)value).ToString(CurrencyNameFormat, provider), NzdCurrencyNameResult },
        { (value, provider) => string.Format(provider, "{0:N}", value), NzdCurrencyNameResult },
        { (value, provider) => value.ToString(NoDecimalsStandardSymbolFormat, provider), NzdNoDecimalsResult },
        { (value, provider) => ((IFormattable)value).ToString(NoDecimalsStandardSymbolFormat, provider), NzdNoDecimalsResult },
        { (value, provider) => string.Format(provider, "{0:C0}", value), NzdNoDecimalsResult },
        { (value, provider) => value.ToString(GeneralFormat, provider), GeneralResult },
        { (value, provider) => ((IFormattable)value).ToString(GeneralFormat, provider), GeneralResult },
        { (value, provider) => string.Format(provider, "{0:G}", value), GeneralResult },
    };

    #endregion

    #region Test: Format_CultureInfoProvider

    [Fact]
    public void Format_ProviderIsCultureInfo_ResolvesMonetaryInformationForThatCulture()
    {
        // arrange
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(StandardSymbolFormat, new CultureInfo(GermanCulture));

        // assert
        testResult.Should().Be(GermanNzdResult, "a culture may be supplied where monetary information is expected");
    }

    #endregion

    #region Test: Format_ProviderOfNumberFormatting

    [Theory]
    [MemberData(nameof(NumberFormattingProviderTestCases), DisableDiscoveryEnumeration = true)]
    public void Format_ProviderIsNotMoneyInfo_UsesNumberFormattingOfTheProviderRatherThanTheCurrentCulture(string threadCulture, Money testSubject, string? testFormat, IFormatProvider testProvider, string expectedResult)
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(new CultureInfo(threadCulture));

        // act
        var testResult = testSubject.ToString(testFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "the provider supplied by the caller decides the formatting, whatever the culture of the thread");
    }

    // Each case runs under a thread culture whose separators differ from those of the provider.
    public static TheoryData<string, Money, string?, IFormatProvider, string> NumberFormattingProviderTestCases => new()
    {
        { UnitedStatesCulture, Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR), GeneralFormat, new CultureInfo(GermanCulture), "1234,5" },
        { UnitedStatesCulture, Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR), null, new CultureInfo(GermanCulture), "1234,5" },
        { UnitedStatesCulture, Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR), "0.00", new CultureInfo(GermanCulture), "1234,50" },
        { GermanCulture, Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR), GeneralFormat, NumberFormatInfo.InvariantInfo, GeneralResult },
        { GermanCulture, Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR), null, NumberFormatInfo.InvariantInfo, GeneralResult },
        { GermanCulture, Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR), "0.00", NumberFormatInfo.InvariantInfo, "1234.50" },
        { GermanCulture, Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR), GeneralFormat, CurrencyFormatInfo.InvariantInfo, GeneralResult },
        { GermanCulture, Money.Create(OneUnitAmount, Iso4217CurrencyCurrent.EUR), StandardSymbolFormat, CurrencyFormatInfo.InvariantInfo, EuroOneUnitSymbolResult },
        { GermanCulture, Money.Create(OneUnitAmount, Iso4217CurrencyCurrent.EUR), NarrowSymbolFormat, CurrencyFormatInfo.InvariantInfo, EuroOneUnitSymbolResult },
        { GermanCulture, Money.Create(OneUnitAmount, Iso4217CurrencyCurrent.EUR), CurrencyCodeFormat, CurrencyFormatInfo.InvariantInfo, "EUR1.00" },
        { GermanCulture, Money.Create(OneUnitAmount, Iso4217CurrencyCurrent.EUR), CurrencyNameFormat, CurrencyFormatInfo.InvariantInfo, "Euro1.00" },
        { GermanCulture, Money.Create(UnroundedAmount, Iso4217CurrencyCurrent.USD, RoundingToCents), "0.000", CurrencyFormatInfo.InvariantInfo, "1.010" },
    };

    [Fact]
    public void Format_GeneralFormatOfNegativeValueWithCurrencyFormatInfoHavingEmptyNegativeSign_EmitsNoNegativeSign()
    {
        // arrange
        var testSubject = Money.Create(NegativeTestAmount, Iso4217CurrencyCurrent.EUR);
        var testProvider = new CurrencyFormatInfo { NegativeSign = string.Empty };

        // act
        var testResult = testSubject.ToString(GeneralFormat, testProvider);

        // assert
        testResult.Should().Be(GeneralResult, "the general form uses the negative sign of the currency formatting information, as the money forms do");
    }

    #endregion

    #region Test: Format_GeneralFormat

    public static TheoryData<decimal, string, string> GeneralFormatTestCases() => new()
    {
        // The precision of G is a number of significant digits, and may give scientific notation.
        { 1234.56m, "G2", "1.2E+03" },
        // Without a precision, the minor units of the currency (2 for USD) are not applied.
        { FiveAmount, GeneralFormat, "5" },
    };

    [Theory]
    [MemberData(nameof(GeneralFormatTestCases))]
    public void Format_GeneralFormat_FormatsAsDecimal(decimal testAmount, string testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(testAmount, Iso4217CurrencyCurrent.USD);

        // act
        var testResult = testSubject.ToString(testFormat, NumberFormatInfo.InvariantInfo);

        // assert
        testResult.Should().Be(expectedResult, "G and its precision mean what they mean for decimal, so the precision is significant digits and the minor units are not applied");
    }

    #endregion

    #region Test: Format_ProviderWithCustomFormatterFallingBackToFormattable

    [Theory]
    [InlineData(StandardSymbolFormat, UsdFiveResult)]
    [InlineData(CurrencyCodeFormat, "USD5.00")]
    [InlineData(GeneralFormat, "5")]
    public void Format_ProviderWithCustomFormatterFallingBackToFormattable_UsesTheMoneyFormatter(string testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(FiveAmount, Iso4217CurrencyCurrent.USD);
        var testProvider = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResult = testSubject.ToString(testFormat, testProvider);

        // assert
        testResult.Should().Be(expectedResult, "a caller's custom formatter is not called back, as decimal never calls it");
    }

    [Fact]
    public void Format_GeneralFormatWithProviderWithCustomFormatter_UsesTheSeparatorsOfItsCurrencyFormatInfo()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(new CultureInfo(GermanCulture));
        var testSubject = Money.Create(TestAmount, Iso4217CurrencyCurrent.EUR);
        var testProvider = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResult = testSubject.ToString(GeneralFormat, testProvider);

        // assert
        testResult.Should().Be(GeneralResult, "the provider's currency formatting information, not the current culture, gives the separators");
    }

    [Fact]
    public void Format_CompositeFormatWithProviderWithCustomFormatter_CallsTheCustomFormatterOnce()
    {
        // arrange
        var testSubject = Money.Create(FiveAmount, Iso4217CurrencyCurrent.USD);
        var testProvider = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResult = string.Format(testProvider, "{0:C}", testSubject);

        // assert
        testResult.Should().Be(UsdFiveResult, "string.Format calls the custom formatter, which formats the value without being called back");
    }

    #endregion
}
