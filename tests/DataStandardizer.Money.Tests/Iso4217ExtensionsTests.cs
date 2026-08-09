using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class Iso4217ExtensionsTests
{
    #region Test: GetCurrencyName_OnIso4217CurrentCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.INR, "Indian Rupee")]
    [InlineData(Iso4217CurrencyCurrent.NOK, "Norwegian Krone")]
    [InlineData(Iso4217CurrencyCurrent.USD, "US Dollar")]
    public void GetCurrencyName_OnIso4217CurrentCurrencyCode_ReturnsCurrencyName(Iso4217CurrencyCurrent testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencyName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion

    #region Test: GetCurrencyName_OnIso4217HistoricCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyHistoric.BEF, "Belgian Franc")]
    [InlineData(Iso4217CurrencyHistoric.DDM, "Mark der DDR")]
    public void GetCurrencyName_OnIso4217HistoricCurrencyCode_ReturnsCurrencyName(Iso4217CurrencyHistoric testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencyName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion

    #region Test: GetMinorUnits_OnIso4217CurrentCurrencyCode

    [Theory]
    [MemberData(nameof(GetMinorUnits_OnIso4217CurrentCurrencyCode_TestCaseGenerator.TestCases), MemberType = typeof(GetMinorUnits_OnIso4217CurrentCurrencyCode_TestCaseGenerator))]
    public void GetMinorUnits_OnIso4217CurrentCurrencyCode_ReturnsMinorDigits(Iso4217CurrencyCurrent testCode, byte? expectedResult)
    {
        // act
        var testResult = testCode.GetMinorUnits();

        // assert
        testResult.Should().Be(expectedResult);
    }

    private class GetMinorUnits_OnIso4217CurrentCurrencyCode_TestCaseGenerator
    {
        public static IEnumerable<object?[]> TestCases
        {
            get
            {
                yield return new object[] { Iso4217CurrencyCurrent.BIF, (byte)0 };
                yield return new object[] { Iso4217CurrencyCurrent.USD, (byte)2 };
                yield return new object[] { Iso4217CurrencyCurrent.BHD, (byte)3 };
                yield return new object[] { Iso4217CurrencyCurrent.UYW, (byte)4 };
                yield return new object?[] { Iso4217CurrencyCurrent.XTS, null };
            }
        }
    }

    #endregion

    #region Test: IsFundCode_OnIso4217CurrentCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.GBP, false)]
    [InlineData(Iso4217CurrencyCurrent.CLF, true)]
    public void IsFundCode_OnIso4217CurrentCurrencyCode_ReturnsFundsFlag(Iso4217CurrencyCurrent testCode, bool expectedResult)
    {
        // act
        var testResult = testCode.IsFundCode();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion

    #region Test: IsNationalCurrency_OnIso4217CurrentCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.EUR, true)]
    [InlineData(Iso4217CurrencyCurrent.AUD, true)]
    [InlineData(Iso4217CurrencyCurrent.XAU, false)]
    [InlineData(Iso4217CurrencyCurrent.XTS, false)]
    public void IsNationalCurrency_OnIso4217CurrentCurrencyCode_ReturnsNationalCurrencyFlag(Iso4217CurrencyCurrent testCode, bool expectedResult)
    {
        // act
        var testResult = testCode.IsNationalCurrency();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion

    #region Test: IsSupranationalCurrency_OnIso4217CurrentCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.TWD, false)]
    [InlineData(Iso4217CurrencyCurrent.NZD, false)]
    [InlineData(Iso4217CurrencyCurrent.ZAR, false)]
    [InlineData(Iso4217CurrencyCurrent.XAF, true)]
    [InlineData(Iso4217CurrencyCurrent.XCD, true)]
    [InlineData(Iso4217CurrencyCurrent.XCG, true)]
    [InlineData(Iso4217CurrencyCurrent.XOF, true)]
    [InlineData(Iso4217CurrencyCurrent.XPF,true)]
    public void IsSupranationalCurrency_OnIso4217CurrentCurrencyCode_ReturnsSupranationalCurrencyFlag(Iso4217CurrencyCurrent testCode, bool expectedResult)
    {
        // act
        var testResult = testCode.IsSupranationalCurrency();

        // assert
        testResult.Should().Be(expectedResult);
    }

    #endregion

    #region Test: GetCurrencySymbol_OnIso4217CurrentCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.USD, "$")]
    [InlineData(Iso4217CurrencyCurrent.EUR, "€")]
    [InlineData(Iso4217CurrencyCurrent.INR, "₹")]
    [InlineData(Iso4217CurrencyCurrent.NZD, "NZ$")]
    [InlineData(Iso4217CurrencyCurrent.CAD, "CA$")]
    [InlineData(Iso4217CurrencyCurrent.BRL, "R$")]
    [InlineData(Iso4217CurrencyCurrent.CHF, null)]
    [InlineData(Iso4217CurrencyCurrent.RON, null)]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCode_ReturnsStandardSymbol(Iso4217CurrencyCurrent testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD, "$")]
    [InlineData(Iso4217CurrencyCurrent.CAD, "$")]
    [InlineData(Iso4217CurrencyCurrent.CZK, "Kč")]
    [InlineData(Iso4217CurrencyCurrent.PLN, "zł")]
    [InlineData(Iso4217CurrencyCurrent.HUF, "Ft")]
    [InlineData(Iso4217CurrencyCurrent.SEK, "kr")]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCodeRequestingNarrowForm_ReturnsNarrowSymbol(Iso4217CurrencyCurrent testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.USD)]
    [InlineData(Iso4217CurrencyCurrent.EUR)]
    [InlineData(Iso4217CurrencyCurrent.CHF)]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCodeWithNoDistinctNarrowForm_FallsBackToStandardSymbol(Iso4217CurrencyCurrent testCode)
    {
        // act
        var narrowResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);
        var standardResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Standard);

        // assert
        narrowResult.Should().Be(standardResult, "a currency without a distinct narrow form falls back to its standard form");
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD)]
    [InlineData(Iso4217CurrencyCurrent.CHF)]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCodeWithoutKind_MatchesStandardForm(Iso4217CurrencyCurrent testCode)
    {
        // act
        var testResult = testCode.GetCurrencySymbol();

        // assert
        testResult.Should().Be(testCode.GetCurrencySymbol(CurrencySymbolKind.Standard), "the default form is the standard form");
    }

    #endregion

    #region Test: GetCurrencySymbol_OnIso4217HistoricCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyHistoric.ESP, "₧")]
    [InlineData(Iso4217CurrencyHistoric.HRK, "kn")]
    [InlineData(Iso4217CurrencyHistoric.LTL, "Lt")]
    public void GetCurrencySymbol_OnIso4217HistoricCurrencyCodeRequestingNarrowForm_ReturnsNarrowSymbol(Iso4217CurrencyHistoric testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Iso4217CurrencyHistoric.DDM)]
    [InlineData(Iso4217CurrencyHistoric.BEF)]
    public void GetCurrencySymbol_OnIso4217HistoricCurrencyCodeWithNoSymbol_ReturnsNull(Iso4217CurrencyHistoric testCode)
    {
        // act
        var testResult = testCode.GetCurrencySymbol();

        // assert
        testResult.Should().BeNull("no symbol is defined for this currency");
    }

    #endregion
}
