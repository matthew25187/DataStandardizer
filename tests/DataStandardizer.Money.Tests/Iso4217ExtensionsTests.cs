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
    [InlineData(Iso4217CurrencyHistoric.ZWC, "Rhodesian Dollar")]
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
}