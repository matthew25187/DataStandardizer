using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class MoneyFormatterTests
{
    private const string NewZealandCulture = "en-NZ";
    private const string GermanCulture = "de-DE";
    private const string IndianCulture = "en-IN";
    private const string DutchCulture = "nl-NL";

    private const decimal TestAmount = 1234.5m;
    private const decimal NegativeTestAmount = -1234.5m;
    private const decimal SevenDigitAmount = 1234567.5m;

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

    private const string ChfResult = "CHF1,234.50";
    private const string GermanNzdResult = "1.234,50 NZ$";
    private const string SymbolSpaceAmountNzdResult = "NZ$ 1.234,50";
    private const string IndianGroupedNzdResult = "NZ$12,34,567.50";
    private const string GroupedAmountResult = "1,234.50";

    private const string MinusSign = "\x2212";

    private static MoneyInfo MoneyInfoFor(string cultureName) => MoneyInfo.GetMoneyInfo(new CultureInfo(cultureName));

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
    [InlineData(Iso4217CurrencyCurrent.CHF, ChfResult)]
    public void Format_NarrowSymbolSpecifier_UsesNarrowSymbol(Iso4217CurrencyCurrent testCurrency, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(TestAmount, testCurrency);

        // act
        var testResult = testSubject.ToString(NarrowSymbolFormat, MoneyInfoFor(NewZealandCulture));

        // assert
        testResult.Should().Be(expectedResult, "the narrow symbol is the shortest recognisable form");
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
}
