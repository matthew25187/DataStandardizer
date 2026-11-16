using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class Iso4217ExtensionsTests
{
    // Currency names from ISO 4217 List One (current currencies).
    private const string IndianRupeeName = "Indian Rupee";
    private const string NorwegianKroneName = "Norwegian Krone";
    private const string UsDollarName = "US Dollar";

    // Currency names from ISO 4217 List Three (historic denominations).
    private const string BelgianFrancName = "Belgian Franc";
    private const string MarkDerDdrName = "Mark der DDR";

    // Minor units from ISO 4217 List One.
    private const byte BurundiFrancMinorUnits = 0;
    private const byte UsDollarMinorUnits = 2;
    private const byte BahrainiDinarMinorUnits = 3;
    private const byte UnidadPrevisionalMinorUnits = 4;

    // Standard currency symbols from the Unicode CLDR 49 currency data for the en locale.
    private const string DollarSign = "$";
    private const string EuroSign = "\x20AC";
    private const string IndianRupeeSign = "\x20B9";
    private const string NewZealandDollarSymbol = "NZ$";
    private const string CanadianDollarSymbol = "CA$";
    private const string BrazilianRealSymbol = "R$";

    // Narrow currency symbols from the Unicode CLDR 49 currency data for the en locale.
    private const string CzechKorunaNarrowSymbol = "K\x010D";
    private const string PolishZlotyNarrowSymbol = "z\x0142";
    private const string HungarianForintNarrowSymbol = "Ft";
    private const string SwedishKronaNarrowSymbol = "kr";
    private const string SwissFrancNarrowSymbol = "Fr.";
    private const string SpanishPesetaNarrowSymbol = "\x20A7";
    private const string IndonesianRupiahNarrowSymbol = "Rp";
    private const string RomanianLeuNarrowSymbol = "lei";

    #region Test: GetCurrencyName_OnIso4217CurrentCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.INR, IndianRupeeName)]
    [InlineData(Iso4217CurrencyCurrent.NOK, NorwegianKroneName)]
    [InlineData(Iso4217CurrencyCurrent.USD, UsDollarName)]
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
    [InlineData(Iso4217CurrencyHistoric.BEF, BelgianFrancName)]
    [InlineData(Iso4217CurrencyHistoric.DDM, MarkDerDdrName)]
    public void GetCurrencyName_OnIso4217HistoricCurrencyCode_ReturnsCurrencyName(Iso4217CurrencyHistoric testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencyName();

        // assert
        testResult.Should().Be(expectedResult);
    }

    // One member of each group that shares a value: ISO 4217 List Three reuses numeric codes, and the
    // codes with no numeric code share -1. The other members of a group are the same value, so a row
    // for them would duplicate this one.
    [Theory]
    [InlineData(Iso4217CurrencyHistoric.AOK)]
    [InlineData(Iso4217CurrencyHistoric.ARA)]
    [InlineData(Iso4217CurrencyHistoric.BRB)]
    [InlineData(Iso4217CurrencyHistoric.BGJ)]
    [InlineData(Iso4217CurrencyHistoric.ZRN)]
    [InlineData(Iso4217CurrencyHistoric.HRD)]
    [InlineData(Iso4217CurrencyHistoric.GNE)]
    [InlineData(Iso4217CurrencyHistoric.ILP)]
    [InlineData(Iso4217CurrencyHistoric.LVL)]
    [InlineData(Iso4217CurrencyHistoric.LTL)]
    [InlineData(Iso4217CurrencyHistoric.MTL)]
    [InlineData(Iso4217CurrencyHistoric.MZE)]
    [InlineData(Iso4217CurrencyHistoric.PEH)]
    [InlineData(Iso4217CurrencyHistoric.GWE)]
    [InlineData(Iso4217CurrencyHistoric.ROK)]
    [InlineData(Iso4217CurrencyHistoric.RHD)]
    [InlineData(Iso4217CurrencyHistoric.SDD)]
    [InlineData(Iso4217CurrencyHistoric.UGS)]
    [InlineData(Iso4217CurrencyHistoric.RUR)]
    [InlineData(Iso4217CurrencyHistoric.UYN)]
    [InlineData(Iso4217CurrencyHistoric.YUD)]
    [InlineData(Iso4217CurrencyHistoric.CSD)]
    [InlineData(Iso4217CurrencyHistoric.XFO)]
    public void GetCurrencyName_OnIso4217HistoricCurrencyCodeSharingValue_ReturnsNull(Iso4217CurrencyHistoric testCode)
    {
        // act
        var testResult = testCode.GetCurrencyName();

        // assert
        testResult.Should().BeNull("members that share a value can't be told apart");
    }

    #endregion

    #region Test: GetMinorUnits_OnIso4217CurrentCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.BIF, BurundiFrancMinorUnits)]
    [InlineData(Iso4217CurrencyCurrent.USD, UsDollarMinorUnits)]
    [InlineData(Iso4217CurrencyCurrent.BHD, BahrainiDinarMinorUnits)]
    [InlineData(Iso4217CurrencyCurrent.UYW, UnidadPrevisionalMinorUnits)]
    [InlineData(Iso4217CurrencyCurrent.XTS, null)]
    public void GetMinorUnits_OnIso4217CurrentCurrencyCode_ReturnsMinorDigits(Iso4217CurrencyCurrent testCode, byte? expectedResult)
    {
        // act
        var testResult = testCode.GetMinorUnits();

        // assert
        testResult.Should().Be(expectedResult);
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

    // Funds codes from ISO 4217 List One.
    [Theory]
    [InlineData(Iso4217CurrencyCurrent.USN)]
    [InlineData(Iso4217CurrencyCurrent.CHE)]
    [InlineData(Iso4217CurrencyCurrent.CLF)]
    [InlineData(Iso4217CurrencyCurrent.BOV)]
    public void IsNationalCurrency_OnFundCode_ReturnsFalse(Iso4217CurrencyCurrent testCode)
    {
        // act
        var testResult = testCode.IsNationalCurrency();

        // assert
        testResult.Should().BeFalse("ISO 4217 lists {0} as a funds code, not a currency", testCode);
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
    [InlineData(Iso4217CurrencyCurrent.XPF, true)]
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
    [InlineData(Iso4217CurrencyCurrent.USD, DollarSign)]
    [InlineData(Iso4217CurrencyCurrent.EUR, EuroSign)]
    [InlineData(Iso4217CurrencyCurrent.INR, IndianRupeeSign)]
    [InlineData(Iso4217CurrencyCurrent.NZD, NewZealandDollarSymbol)]
    [InlineData(Iso4217CurrencyCurrent.CAD, CanadianDollarSymbol)]
    [InlineData(Iso4217CurrencyCurrent.BRL, BrazilianRealSymbol)]
    [InlineData(Iso4217CurrencyCurrent.CHF, null)]
    [InlineData(Iso4217CurrencyCurrent.RON, null)]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCode_ReturnsStandardSymbol(Iso4217CurrencyCurrent testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol();

        // assert
        testResult.Should().Be(expectedResult, "the default form is the standard form");
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.USD, DollarSign)]
    [InlineData(Iso4217CurrencyCurrent.NZD, NewZealandDollarSymbol)]
    [InlineData(Iso4217CurrencyCurrent.CHF, null)]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCodeRequestingStandardForm_ReturnsStandardSymbol(Iso4217CurrencyCurrent testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Standard);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.NZD, DollarSign)]
    [InlineData(Iso4217CurrencyCurrent.CAD, DollarSign)]
    [InlineData(Iso4217CurrencyCurrent.CZK, CzechKorunaNarrowSymbol)]
    [InlineData(Iso4217CurrencyCurrent.PLN, PolishZlotyNarrowSymbol)]
    [InlineData(Iso4217CurrencyCurrent.HUF, HungarianForintNarrowSymbol)]
    [InlineData(Iso4217CurrencyCurrent.SEK, SwedishKronaNarrowSymbol)]
    [InlineData(Iso4217CurrencyCurrent.CHF, SwissFrancNarrowSymbol)]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCodeRequestingNarrowForm_ReturnsNarrowSymbol(Iso4217CurrencyCurrent testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(Iso4217CurrencyCurrent.USD, DollarSign)]
    [InlineData(Iso4217CurrencyCurrent.EUR, EuroSign)]
    [InlineData(Iso4217CurrencyCurrent.BHD, null)]
    public void GetCurrencySymbol_OnIso4217CurrentCurrencyCodeWithNoDistinctNarrowFormRequestingNarrowForm_ReturnsStandardSymbol(Iso4217CurrencyCurrent testCode, string? expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);

        // assert
        testResult.Should().Be(expectedResult, "a currency without a distinct narrow form falls back to its standard form");
    }

    #endregion

    #region Test: GetCurrencySymbol_OnIso4217HistoricCurrencyCode

    [Theory]
    [InlineData(Iso4217CurrencyHistoric.ESP, SpanishPesetaNarrowSymbol)]
    [InlineData(Iso4217CurrencyHistoric.IDR, IndonesianRupiahNarrowSymbol)]
    [InlineData(Iso4217CurrencyHistoric.RON, RomanianLeuNarrowSymbol)]
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

    // The CLDR data has a narrow symbol for HRK, LTL and LVL, each of which shares its value with an
    // earlier denomination (HRD, LTT and LVR).
    [Theory]
    [InlineData(Iso4217CurrencyHistoric.HRK)]
    [InlineData(Iso4217CurrencyHistoric.LTL)]
    [InlineData(Iso4217CurrencyHistoric.LVL)]
    public void GetCurrencySymbol_OnIso4217HistoricCurrencyCodeSharingValueRequestingNarrowForm_ReturnsNull(Iso4217CurrencyHistoric testCode)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);

        // assert
        testResult.Should().BeNull("members that share a value can't be told apart");
    }

    [Theory]
    [InlineData(Iso4217CurrencyHistoric.HRK)]
    [InlineData(Iso4217CurrencyHistoric.LTL)]
    [InlineData(Iso4217CurrencyHistoric.LVL)]
    public void GetCurrencySymbol_OnIso4217HistoricCurrencyCodeSharingValue_ReturnsNull(Iso4217CurrencyHistoric testCode)
    {
        // act
        var testResult = testCode.GetCurrencySymbol();

        // assert
        testResult.Should().BeNull("members that share a value can't be told apart");
    }

    #endregion
}
