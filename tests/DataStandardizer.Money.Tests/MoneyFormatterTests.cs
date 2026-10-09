using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class MoneyFormatterTests
{
    private static MoneyInfo MoneyInfoFor(string cultureName) => MoneyInfo.GetMoneyInfo(new CultureInfo(cultureName));

    #region Test: Format_StandardSpecifier

    [Theory]
    [InlineData("C", "NZ$1,234.50")]
    [InlineData("c", "NZ$1,234.50")]
    [InlineData("H", "$1,234.50")]
    [InlineData("h", "$1,234.50")]
    [InlineData("I", "NZD1,234.50")]
    [InlineData("i", "NZD1,234.50")]
    [InlineData("N", "New Zealand Dollar1,234.50")]
    [InlineData("G", "1234.5")]
    [InlineData(null, "1234.5")]
    public void Format_StandardSpecifier_ReturnsExpectedRepresentation(string? testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(testFormat, MoneyInfoFor("en-NZ"));

        // assert
        testResult.Should().Be(expectedResult, "the format {0} was requested", testFormat ?? "(null)");
    }

    #endregion

    #region Test: Format_CurrencySymbolSpecifier

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD, "NZ$1,234.50", "$1,234.50")]
    [InlineData(Iso4217CurrencyCurrent.CAD, "CA$1,234.50", "$1,234.50")]
    [InlineData(Iso4217CurrencyCurrent.CHF, "CHF1,234.50", "CHF1,234.50")]
    public void Format_CurrencySymbolSpecifier_DistinguishesStandardFromNarrowForm(Iso4217CurrencyCurrent testCurrency, string expectedStandard, string expectedNarrow)
    {
        // arrange
        var testSubject = Money.Create(1234.5m, testCurrency);
        var testProvider = MoneyInfoFor("en-NZ");

        // act
        var standardResult = testSubject.ToString("C", testProvider);
        var narrowResult = testSubject.ToString("H", testProvider);

        // assert
        standardResult.Should().Be(expectedStandard, "the standard symbol is unambiguous");
        narrowResult.Should().Be(expectedNarrow, "the narrow symbol is the shortest recognisable form");
    }

    [Fact]
    public void Format_CurrencySymbolSpecifierForCurrencyWithoutSymbol_FallsBackToCurrencyCode()
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.CHF);

        // act
        var testResult = testSubject.ToString("C", MoneyInfoFor("en-NZ"));

        // assert
        testResult.Should().Be("CHF1,234.50", "a currency without a distinct symbol is denoted by its code");
    }

    #endregion

    #region Test: Format_PrecisionSpecifier

    [Theory]
    [InlineData("C0", "NZ$1,235")]
    [InlineData("C2", "NZ$1,234.50")]
    [InlineData("C4", "NZ$1,234.5000")]
    [InlineData("I3", "NZD1,234.500")]
    public void Format_PrecisionSpecifier_AppliesRequestedPrecision(string testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString(testFormat, MoneyInfoFor("en-NZ"));

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD, "NZ$1,234.50")]
    [InlineData(Iso4217CurrencyCurrent.JPY, "¥1,235")]
    [InlineData(Iso4217CurrencyCurrent.KWD, "KWD1,234.500")]
    public void Format_NoPrecisionSpecifier_UsesMinorUnitsOfCurrency(Iso4217CurrencyCurrent testCurrency, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(1234.5m, testCurrency);

        // act
        var testResult = testSubject.ToString("C", MoneyInfoFor("en-NZ"));

        // assert
        testResult.Should().Be(expectedResult, "the default precision is the number of minor units of {0}", testCurrency);
    }

    #endregion

    #region Test: Format_CurrencyCodeFormat

    [Fact]
    public void Format_CurrencyCodeFormatMatchingTheValue_ReturnsCurrencyCodeRepresentation()
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString("NZD", MoneyInfoFor("en-NZ"));

        // assert
        testResult.Should().Be("NZD1,234.50");
    }

    [Fact]
    public void Format_CurrencyCodeFormatNotMatchingTheValue_ThrowsFormatException()
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testAction = () => testSubject.ToString("USD", MoneyInfoFor("en-NZ"));

        // assert
        testAction.Should().Throw<FormatException>("the format names a currency other than that of the value");
    }

    #endregion

    #region Test: Format_CultureGovernsPresentationOnly

    [Theory]
    [InlineData("en-NZ", "NZ$12,34,567.50")]
    [InlineData("en-IN", "NZ$12,34,567.50")]
    public void Format_ValueOfForeignCurrency_AppliesCultureGroupingButRetainsCurrencyOfValue(string testCulture, string _)
    {
        // arrange
        var testSubject = Money.Create(1234567.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString("C", MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Contain("NZ$", "the currency of the value is denoted, never that of the culture");
        testResult.Should().NotContain("₹", "the currency symbol of the culture must not be substituted");
    }

    [Fact]
    public void Format_ValueOfForeignCurrencyUnderIndianCulture_AppliesIndianGrouping()
    {
        // arrange
        var testSubject = Money.Create(1234567.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString("C", MoneyInfoFor("en-IN"));

        // assert
        testResult.Should().Be("NZ$12,34,567.50", "the culture governs grouping, and the value governs the currency");
    }

    [Fact]
    public void Format_ValueOfCurrencyWithoutMinorUnitsUnderCultureWithMinorUnits_UsesMinorUnitsOfCurrency()
    {
        // arrange
        var testSubject = Money.Create(1234m, Iso4217CurrencyCurrent.JPY);

        // act
        var testResult = testSubject.ToString("I", MoneyInfoFor("en-IN"));

        // assert
        testResult.Should().Be("JPY1,234", "the currency has no minor units even though the culture has two");
    }

    #endregion

    #region Test: Format_CulturePatterns

    [Theory]
    [InlineData("en-NZ", "NZ$1,234.50")]
    [InlineData("de-DE", "1.234,50 NZ$")]
    [InlineData("pt-BR", "NZ$ 1.234,50")]
    [InlineData("nl-NL", "NZ$ 1.234,50")]
    public void Format_PositiveValue_AppliesCurrencyPositivePatternOfCulture(string testCulture, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString("C", MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the culture {0} places the currency token according to its own pattern", testCulture);
    }

    [Theory]
    [InlineData("en-NZ", "-NZ$1,234.50")]
    [InlineData("de-DE", "-1.234,50 NZ$")]
    [InlineData("nl-NL", "NZ$ -1.234,50")]
    public void Format_NegativeValue_AppliesCurrencyNegativePatternOfCulture(string testCulture, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(-1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString("C", MoneyInfoFor(testCulture));

        // assert
        testResult.Should().Be(expectedResult, "the culture {0} denotes a negative value according to its own pattern", testCulture);
    }

    [Fact]
    public void Format_NegativeValueForCultureWithNonAsciiNegativeSign_UsesNegativeSignOfCulture()
    {
        // arrange
        var testSubject = Money.Create(-1234.5m, Iso4217CurrencyCurrent.NZD);
        var testProvider = new MoneyInfo
        {
            CurrencyFormat = new CurrencyFormatInfo
            {
                CurrencyCode = nameof(Iso4217CurrencyCurrent.SEK),
                CurrencyDecimalDigits = 2,
                CurrencyDecimalSeparator = ",",
                CurrencyGroupSeparator = " ",
                CurrencyGroupSizes = new[] { 3 },
                CurrencyNegativePattern = 8,
                CurrencyPositivePattern = 3,
                NegativeSign = "−"
            }
        };

        // act
        var testResult = testSubject.ToString("C", testProvider);

        // assert
        testResult.Should().StartWith("−", "the negative sign of the culture is a minus sign rather than a hyphen");
    }

    #endregion

    #region Test: Format_ValueWithoutCurrency

    [Theory]
    [InlineData("C")]
    [InlineData("H")]
    [InlineData("I")]
    [InlineData("N")]
    public void Format_ValueWithoutCurrency_ReturnsAmountWithoutCurrencyToken(string testFormat)
    {
        // arrange
        var testSubject = Money.Create(1234.5m);

        // act
        var testResult = testSubject.ToString(testFormat, MoneyInfoFor("en-NZ"));

        // assert
        testResult.Should().Be("1,234.50", "a value with no currency has no currency token to denote");
        testResult.Should().NotContain("$", "the currency symbol of the culture must not be substituted");
    }

    #endregion

    #region Test: Format_OtherFormats

    [Theory]
    [InlineData("#,##0.00", "1,234.50")]
    [InlineData("0.000", "1234.500")]
    public void Format_CustomNumericFormat_FormatsAmountAsNumber(string testFormat, string expectedResult)
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

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
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testAction = () => testSubject.ToString(testFormat, MoneyInfoFor("en-NZ"));

        // assert
        testAction.Should().Throw<FormatException>("the format string is not valid");
    }

    [Fact]
    public void Format_ArgumentIsNotMoneyValue_FormatsTheArgument()
    {
        // arrange
        var testSubject = new MoneyFormatter();

        // act
        var testResult = testSubject.Format("N2", 1234.5m, CultureInfo.InvariantCulture);

        // assert
        testResult.Should().Be("1,234.50", "an argument which is not a monetary value is formatted as itself");
    }

    [Fact]
    public void Format_ArgumentIsNull_ReturnsEmptyString()
    {
        // arrange
        var testSubject = new MoneyFormatter();

        // act
        var testResult = testSubject.Format("C", null, CultureInfo.InvariantCulture);

        // assert
        testResult.Should().BeEmpty();
    }

    [Fact]
    public void Format_MoneyValueWithUnrecognisedFormat_DoesNotRecurse()
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testAction = () => testSubject.ToString("0.00", MoneyInfoFor("en-NZ"));

        // assert
        // A monetary value is itself formattable, so resolving an unrecognised format through the
        // formattable argument would resolve this formatter again and would not terminate.
        testAction.Should().NotThrow("formatting must not recurse through the formatter");
        testSubject.ToString("0.00", MoneyInfoFor("en-NZ")).Should().Be("1234.50");
    }

    #endregion

    #region Test: Format_Alignment

    [Theory]
    [InlineData("C")]
    [InlineData("H")]
    [InlineData("I")]
    [InlineData("N")]
    [InlineData("C0")]
    [InlineData("G")]
    public void Format_EveryInvocationForm_ProducesIdenticalOutput(string testFormat)
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);
        var testProvider = MoneyInfoFor("en-NZ");

        // act
        var toStringResult = testSubject.ToString(testFormat, testProvider);
        var stringFormatResult = string.Format(testProvider, "{0:" + testFormat + "}", testSubject);
        var formattableResult = ((IFormattable)testSubject).ToString(testFormat, testProvider);

        // assert
        stringFormatResult.Should().Be(toStringResult, "string.Format must agree with ToString");
        formattableResult.Should().Be(toStringResult, "IFormattable must agree with ToString");
    }

    #endregion

    #region Test: Format_CultureInfoProvider

    [Fact]
    public void Format_ProviderIsCultureInfo_ResolvesMonetaryInformationForThatCulture()
    {
        // arrange
        var testSubject = Money.Create(1234.5m, Iso4217CurrencyCurrent.NZD);

        // act
        var testResult = testSubject.ToString("C", new CultureInfo("de-DE"));

        // assert
        testResult.Should().Be("1.234,50 NZ$", "a culture may be supplied where monetary information is expected");
    }

    #endregion
}
