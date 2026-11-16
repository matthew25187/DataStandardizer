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

    // Currency names from ISO 4217 List Three of the most recently withdrawn member of each group of
    // members that share a numeric code (or, for codes with no numeric code, share -1). The withdrawal
    // date each was chosen by is in the comment.
    private const string NewKwanzaName = "New Kwanza";                     // AON, 2000-02 (024)
    private const string AustralName = "Austral";                          // ARA, 1992-01 (032)
    private const string CruzeiroName = "Cruzeiro";                        // BRE, 1993-03 (076)
    private const string LevName = "Lev";                                  // BGL, 2003-11 (100)
    private const string NewZaireName = "New Zaire";                       // ZRN, 1999-06 (180)
    private const string CroatianKunaName = "Croatian Kuna";               // HRK, 2015-06 (191)
    private const string SyliName = "Syli";                                // GNE, 1989-12 (324)
    private const string OldShekelName = "Old Shekel";                     // ILR, 1989 to 1990 (376)
    private const string LatvianLatsName = "Latvian Lats";                 // LVL, 2014-01 (428)
    private const string LithuanianLitasName = "Lithuanian Litas";         // LTL, 2014-12 (440)
    private const string MalteseLiraName = "Maltese Lira";                 // MTL, 2008-01 (470)
    private const string MozambiqueMeticalName = "Mozambique Metical";     // MZM, 2006-06 (508)
    private const string NuevoSolName = "Nuevo Sol ";                      // PEN, 2015-12 (604); List Three's trailing space
    private const string GuineaBissauPesoName = "Guinea-Bissau Peso";      // GWP, 1997-05 (624)
    private const string OldLeuName = "Old Leu";                           // ROL, 2005-06 (642)
    private const string ZimbabweDollarOldName = "Zimbabwe Dollar (old)";  // ZWD, 2006-08 (716)
    private const string SudaneseDinarName = "Sudanese Dinar";             // SDD, 2007-07 (736)
    private const string OldShillingName = "Old Shilling";                 // UGW, 1989 to 1990 (800)
    private const string RussianRubleName = "Russian Ruble";               // RUR, 1994-08 (810)
    private const string UruguayanPesoName = "Uruguayan Peso";             // UYP, 1993-03 (858)
    private const string YugoslavianDinarName = "Yugoslavian Dinar";       // YUN, 1995-11 (890)
    private const string SerbianDinarName = "Serbian Dinar";               // CSD, 2006-10 (891)
    private const string UicFrancName = "UIC-Franc";                       // XFU, 2013-11 (no numeric code)

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
    private const string CroatianKunaNarrowSymbol = "kn";
    private const string LithuanianLitasNarrowSymbol = "Lt";
    private const string LatvianLatsNarrowSymbol = "Ls";

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

    [Theory]
    [InlineData(Iso4217CurrencyHistoric.AOK, NewKwanzaName)]
    [InlineData(Iso4217CurrencyHistoric.ARY, AustralName)]
    [InlineData(Iso4217CurrencyHistoric.BRN, CruzeiroName)]
    [InlineData(Iso4217CurrencyHistoric.BGJ, LevName)]
    [InlineData(Iso4217CurrencyHistoric.ZRZ, NewZaireName)]
    [InlineData(Iso4217CurrencyHistoric.HRD, CroatianKunaName)]
    [InlineData(Iso4217CurrencyHistoric.GNS, SyliName)]
    [InlineData(Iso4217CurrencyHistoric.ILP, OldShekelName)]
    [InlineData(Iso4217CurrencyHistoric.LVR, LatvianLatsName)]
    [InlineData(Iso4217CurrencyHistoric.LTT, LithuanianLitasName)]
    [InlineData(Iso4217CurrencyHistoric.MTP, MalteseLiraName)]
    [InlineData(Iso4217CurrencyHistoric.MZE, MozambiqueMeticalName)]
    [InlineData(Iso4217CurrencyHistoric.PES, NuevoSolName)]
    [InlineData(Iso4217CurrencyHistoric.GWE, GuineaBissauPesoName)]
    [InlineData(Iso4217CurrencyHistoric.ROK, OldLeuName)]
    [InlineData(Iso4217CurrencyHistoric.RHD, ZimbabweDollarOldName)]
    [InlineData(Iso4217CurrencyHistoric.SDP, SudaneseDinarName)]
    [InlineData(Iso4217CurrencyHistoric.UGS, OldShillingName)]
    [InlineData(Iso4217CurrencyHistoric.SUR, RussianRubleName)]
    [InlineData(Iso4217CurrencyHistoric.UYN, UruguayanPesoName)]
    [InlineData(Iso4217CurrencyHistoric.YUD, YugoslavianDinarName)]
    [InlineData(Iso4217CurrencyHistoric.YUM, SerbianDinarName)]
    [InlineData(Iso4217CurrencyHistoric.XRE, UicFrancName)]
    public void GetCurrencyName_OnIso4217HistoricCurrencyCodeSharingValue_ReturnsNameOfMostRecentlyWithdrawn(Iso4217CurrencyHistoric testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencyName();

        // assert
        testResult.Should().Be(expectedResult);
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
    [InlineData(Iso4217CurrencyHistoric.HRK, CroatianKunaNarrowSymbol)]
    [InlineData(Iso4217CurrencyHistoric.LTL, LithuanianLitasNarrowSymbol)]
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

    // The CLDR data has symbols only for the most recently withdrawn of these members, so every member
    // that shares the value returns that member's symbol.
    [Theory]
    [InlineData(Iso4217CurrencyHistoric.HRD, CroatianKunaNarrowSymbol)]
    [InlineData(Iso4217CurrencyHistoric.LTT, LithuanianLitasNarrowSymbol)]
    [InlineData(Iso4217CurrencyHistoric.LVR, LatvianLatsNarrowSymbol)]
    public void GetCurrencySymbol_OnIso4217HistoricCurrencyCodeSharingValueRequestingNarrowForm_ReturnsNarrowSymbolOfMostRecentlyWithdrawn(Iso4217CurrencyHistoric testCode, string expectedResult)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);

        // assert
        testResult.Should().Be(expectedResult);
    }

    // The most recently withdrawn member of each of these groups has no symbol in the CLDR data, so no
    // member that shares the value returns one.
    [Theory]
    [InlineData(Iso4217CurrencyHistoric.AOK)]
    [InlineData(Iso4217CurrencyHistoric.ARY)]
    [InlineData(Iso4217CurrencyHistoric.BRN)]
    [InlineData(Iso4217CurrencyHistoric.BGJ)]
    [InlineData(Iso4217CurrencyHistoric.ZRZ)]
    [InlineData(Iso4217CurrencyHistoric.GNS)]
    [InlineData(Iso4217CurrencyHistoric.ILP)]
    [InlineData(Iso4217CurrencyHistoric.MTP)]
    [InlineData(Iso4217CurrencyHistoric.MZE)]
    [InlineData(Iso4217CurrencyHistoric.PES)]
    [InlineData(Iso4217CurrencyHistoric.GWE)]
    [InlineData(Iso4217CurrencyHistoric.ROK)]
    [InlineData(Iso4217CurrencyHistoric.RHD)]
    [InlineData(Iso4217CurrencyHistoric.SDP)]
    [InlineData(Iso4217CurrencyHistoric.UGS)]
    [InlineData(Iso4217CurrencyHistoric.SUR)]
    [InlineData(Iso4217CurrencyHistoric.UYN)]
    [InlineData(Iso4217CurrencyHistoric.YUD)]
    [InlineData(Iso4217CurrencyHistoric.YUM)]
    [InlineData(Iso4217CurrencyHistoric.XRE)]
    public void GetCurrencySymbol_OnIso4217HistoricCurrencyCodeSharingValueRequestingNarrowForm_ReturnsNullWhenMostRecentlyWithdrawnHasNone(Iso4217CurrencyHistoric testCode)
    {
        // act
        var testResult = testCode.GetCurrencySymbol(CurrencySymbolKind.Narrow);

        // assert
        testResult.Should().BeNull("the most recently withdrawn member sharing this value has no symbol");
    }

    #endregion
}
