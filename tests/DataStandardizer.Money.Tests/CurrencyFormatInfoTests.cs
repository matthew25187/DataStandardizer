using System.Globalization;
using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class CurrencyFormatInfoTests
{
    private const string NewZealandCulture = "en-NZ";
    private const string GermanCulture = "de-DE";
    private const int FirstGroupSize = 3;
    private const int LaterGroupSize = 2;
    private const int ChangedGroupSize = 1;
    private const int InvariantGroupSize = 3;
    private const string SetterParameterName = "value";
    private static readonly int[] ExpectedGroupSizes = { FirstGroupSize, LaterGroupSize };

    // The invariant information, which a culture CLDR resolves to root, or to nothing, keeps.
    private const string InvariantDecimalSeparator = ".";
    private const string InvariantGroupSeparator = ",";
    private const int InvariantPositivePattern = 0;
    private const int InvariantNegativePattern = 0;
    private const int InvariantDecimalDigits = 2;

    // Separators in Unicode CLDR 49.0.0 symbols-numberSystem-latn.
    private const string CldrFullStop = ".";
    private const string CldrComma = ",";
    private const string CldrNoBreakSpace = "\x00A0";
    private const string CldrApostrophe = "'";
    private const string CldrHyphenMinus = "-";
    private const string CldrMinusSign = "\x2212";
    private const string CldrLeftToRightMarkHyphenMinus = "\x200E-";

    // Every CLDR 49.0.0 currency pattern has two fraction digits ("#,##0.00").
    private const int CldrPatternFractionDigits = 2;

    // NumberFormatInfo.CurrencyPositivePattern and CurrencyNegativePattern indices, by layout.
    private const int SymbolNumberPattern = 0;              // $n
    private const int SymbolSpaceNumberPattern = 2;         // $ n
    private const int NumberSpaceSymbolPattern = 3;         // n $
    private const int SignSymbolNumberPattern = 1;          // -$n
    private const int SymbolSignNumberPattern = 2;          // $-n
    private const int SignNumberSpaceSymbolPattern = 8;     // -n $
    private const int SignSymbolSpaceNumberPattern = 9;     // -$ n

    // ISO 4217 minor units.
    private const int EuroMinorUnits = 2;
    private const int PoundMinorUnits = 2;
    private const int CanadianDollarMinorUnits = 2;
    private const int SwissFrancMinorUnits = 2;
    private const int IranianRialMinorUnits = 2;
    private const int DollarMinorUnits = 2;
    private const int YenMinorUnits = 0;
    private const int PesoMinorUnits = 2;
    private const int NorwegianKroneMinorUnits = 2;

    #region Test: InvariantInfo

    [Fact]
    public void InvariantInfo_WhenRead_ReturnsCultureIndependentInformation()
    {
        // act
        var testResult = CurrencyFormatInfo.InvariantInfo;

        // assert
        testResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.XXX), "the invariant information denotes no currency");
        testResult.CurrencyDecimalSeparator.Should().Be(".");
        testResult.CurrencyGroupSeparator.Should().Be(",");
        testResult.CurrencyGroupSizes.Should().Equal(InvariantGroupSize);
        testResult.NegativeSign.Should().Be("-");
        testResult.IsReadOnly.Should().BeTrue("the shared instance must not be modifiable");
    }

    [Fact]
    public void InvariantInfo_WhenModified_ThrowsInvalidOperationException()
    {
        // act
        Action testAction = () => CurrencyFormatInfo.InvariantInfo.CurrencyCode = nameof(Iso4217CurrencyCurrent.NZD);

        // assert
        testAction.Should().Throw<InvalidOperationException>("the instance is read only");
    }

    #endregion

    #region Test: CurrencyGroupSizes

    [Fact]
    public void CurrencyGroupSizes_WhenReturnedArrayIsChanged_KeepsTheOriginalSizes()
    {
        // arrange
        var testInfo = new CurrencyFormatInfo { CurrencyGroupSizes = new[] { FirstGroupSize, LaterGroupSize } };
        var returnedSizes = testInfo.CurrencyGroupSizes;

        // act
        returnedSizes[0] = ChangedGroupSize;
        var testResult = testInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(ExpectedGroupSizes, "the getter must return a copy of the sizes");
    }

    [Fact]
    public void CurrencyGroupSizes_WhenArrayReturnedByInvariantInfoIsChanged_KeepsTheInvariantSizes()
    {
        // arrange
        var returnedSizes = CurrencyFormatInfo.InvariantInfo.CurrencyGroupSizes;

        // act
        returnedSizes[0] = ChangedGroupSize;
        var testResult = CurrencyFormatInfo.InvariantInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(new[] { InvariantGroupSize }, "a read-only instance must not be changed through the array it returns");
    }

    [Fact]
    public void CurrencyGroupSizes_WhenAssignedArrayIsChangedAfterwards_KeepsTheSizesAsSet()
    {
        // arrange
        var assignedSizes = new[] { FirstGroupSize, LaterGroupSize };
        var testInfo = new CurrencyFormatInfo { CurrencyGroupSizes = assignedSizes };

        // act
        assignedSizes[0] = ChangedGroupSize;
        var testResult = testInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(ExpectedGroupSizes, "the setter must store a copy of the sizes");
    }

    [Fact]
    public void CurrencyGroupSizes_WhenSetToNull_ThrowsArgumentNullException()
    {
        // arrange
        var testInfo = new CurrencyFormatInfo();

        // act
        Action testAction = () => testInfo.CurrencyGroupSizes = null!;

        // assert
        testAction.Should().Throw<ArgumentNullException>().WithParameterName(SetterParameterName);
    }

    [Fact]
    public void CurrencyGroupSizes_WhenReadOnlyInstanceIsSetToNull_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => CurrencyFormatInfo.InvariantInfo.CurrencyGroupSizes = null!;

        // assert
        testAction.Should().Throw<ArgumentNullException>("the argument is checked before the instance is, as NumberFormatInfo does").WithParameterName(SetterParameterName);
    }

    [Theory]
    [MemberData(nameof(InvalidGroupSizesTestCases))]
    public void CurrencyGroupSizes_WhenSetToInvalidSizes_ThrowsArgumentException(int[] testSizes)
    {
        // arrange
        var testInfo = new CurrencyFormatInfo();

        // act
        Action testAction = () => testInfo.CurrencyGroupSizes = testSizes;

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName(SetterParameterName);
    }

    [Theory]
    [MemberData(nameof(ValidGroupSizesTestCases))]
    public void CurrencyGroupSizes_WhenSetToValidSizes_StoresTheSizes(int[] testSizes, int[] expectedSizes)
    {
        // arrange
        var testInfo = new CurrencyFormatInfo();

        // act
        testInfo.CurrencyGroupSizes = testSizes;
        var testResult = testInfo.CurrencyGroupSizes;

        // assert
        testResult.Should().Equal(expectedSizes);
    }

    // The rule NumberFormatInfo.NumberGroupSizes applies: each size is from 1 to 9, and only the last may be 0.
    public static TheoryData<int[]> InvalidGroupSizesTestCases => new()
    {
        new[] { 0, 3 },
        new[] { -1 },
        new[] { 3, -1 },
        new[] { 10 },
        new[] { 3, 10 }
    };

    public static TheoryData<int[], int[]> ValidGroupSizesTestCases => new()
    {
        { new int[0], new int[0] },
        { new[] { 1 }, new[] { 1 } },
        { new[] { 9 }, new[] { 9 } },
        { new[] { 0 }, new[] { 0 } },
        { new[] { 3, 0 }, new[] { 3, 0 } }
    };

    #endregion

    #region Test: CreateForCulture

    [Theory]
    [InlineData(NewZealandCulture, nameof(Iso4217CurrencyCurrent.NZD), ".", ",")]
    [InlineData(GermanCulture, nameof(Iso4217CurrencyCurrent.EUR), ",", ".")]
    [InlineData("pt-BR", nameof(Iso4217CurrencyCurrent.BRL), ",", ".")]
    [InlineData("ja-JP", nameof(Iso4217CurrencyCurrent.JPY), ".", ",")]
    public void CreateForCulture_ForSpecificCulture_ReturnsInformationForThatCulture(string testCulture, string expectedCurrencyCode, string expectedDecimalSeparator, string expectedGroupSeparator)
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo(testCulture));

        // assert
        testResult.CurrencyCode.Should().Be(expectedCurrencyCode);
        testResult.CurrencyDecimalSeparator.Should().Be(expectedDecimalSeparator);
        testResult.CurrencyGroupSeparator.Should().Be(expectedGroupSeparator);
    }

    [Fact]
    public void CreateForCulture_ForIndianCulture_ReturnsVariableGroupSizes()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo("en-IN"));

        // assert
        testResult.CurrencyGroupSizes.Should().Equal(new[] { 3, 2 }, "the Indian numbering system groups the leading digits in pairs");
    }

    [Fact]
    public void CreateForCulture_ForCultureWithNonAsciiGroupSeparator_PreservesTheSeparator()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo("ru-RU"));

        // assert
        testResult.CurrencyGroupSeparator.Should().Be("\x00A0", "the group separator is a no-break space");
    }

    [Fact]
    public void CreateForCulture_ForNullCulture_ReturnsCultureIndependentInformation()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(null);

        // assert
        testResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.XXX), "a null culture is given the invariant information, which denotes no currency");
    }

    [Fact]
    public void CreateForCulture_ForSpecificCulture_ReturnsAWritableInstance()
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCulture(new CultureInfo(NewZealandCulture));

        // assert
        testResult.IsReadOnly.Should().BeFalse("sealing the instance is the responsibility of whatever takes ownership of it");
    }

    #endregion

    #region Test: CreateForCultureName

    [Theory]
    [MemberData(nameof(CldrCultureNameTestCases))]
    public void CreateForCultureName_ForCultureName_ReturnsTheCldrInformationItResolvesTo(string testCultureName, string expectedCurrencyCode, string expectedDecimalSeparator, string expectedGroupSeparator, int expectedPositivePattern, int expectedNegativePattern, int expectedDecimalDigits)
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCultureName(testCultureName);

        // assert
        testResult.CurrencyCode.Should().Be(expectedCurrencyCode);
        testResult.CurrencyDecimalSeparator.Should().Be(expectedDecimalSeparator);
        testResult.CurrencyGroupSeparator.Should().Be(expectedGroupSeparator);
        testResult.CurrencyPositivePattern.Should().Be(expectedPositivePattern);
        testResult.CurrencyNegativePattern.Should().Be(expectedNegativePattern);
        testResult.CurrencyDecimalDigits.Should().Be(expectedDecimalDigits);
    }

    // Values from Unicode CLDR 49.0.0: the symbols-numberSystem-latn and currencyFormats-numberSystem-latn
    // "standard" pattern of the locale the name resolves to, and the region's currency from
    // supplemental/currencyData.json. Decimal digits are the ISO 4217 minor units of that currency.
    public static TheoryData<string, string, string, string, int, int, int> CldrCultureNameTestCases => new()
    {
        // de-AT: currencyGroup "." overrides the group separator U+00A0; "¤ #,##0.00".
        { "de-AT", nameof(Iso4217CurrencyCurrent.EUR), CldrComma, CldrFullStop, SymbolSpaceNumberPattern, SignSymbolSpaceNumberPattern, EuroMinorUnits },
        // cy, a locale the package had no resources for; "¤#,##0.00".
        { "cy-GB", nameof(Iso4217CurrencyCurrent.GBP), CldrFullStop, CldrComma, SymbolNumberPattern, SignSymbolNumberPattern, PoundMinorUnits },
        // fr-CA: group U+00A0; "#,##0.00 ¤".
        { "fr-CA", nameof(Iso4217CurrencyCurrent.CAD), CldrComma, CldrNoBreakSpace, NumberSpaceSymbolPattern, SignNumberSpaceSymbolPattern, CanadianDollarMinorUnits },
        // de-CH: group U+0027 (apostrophe); an explicit negative subpattern, "¤ #,##0.00;¤-#,##0.00".
        { "de-CH", nameof(Iso4217CurrencyCurrent.CHF), CldrFullStop, CldrApostrophe, SymbolSpaceNumberPattern, SymbolSignNumberPattern, SwissFrancMinorUnits },
        // pt-FR is not a modern locale; its CLDR parent is pt-PT, "#,##0.00 ¤", not pt, "¤ #,##0.00".
        { "pt-FR", nameof(Iso4217CurrencyCurrent.EUR), CldrComma, CldrNoBreakSpace, NumberSpaceSymbolPattern, SignNumberSpaceSymbolPattern, EuroMinorUnits },
        // az-IR is likely az-Arab-IR, and az-Arab inherits from root, not from az: the invariant layout.
        { "az-IR", nameof(Iso4217CurrencyCurrent.IRR), InvariantDecimalSeparator, InvariantGroupSeparator, InvariantPositivePattern, InvariantNegativePattern, IranianRialMinorUnits },
        // Cyrl is not the likely script of en, so en-Cyrl inherits from root: the invariant layout.
        { "en-Cyrl-US", nameof(Iso4217CurrencyCurrent.USD), InvariantDecimalSeparator, InvariantGroupSeparator, InvariantPositivePattern, InvariantNegativePattern, DollarMinorUnits },
        // A neutral culture names no region, so no currency; the digits are those of the pattern.
        { "de", nameof(Iso4217CurrencyCurrent.XXX), CldrComma, CldrFullStop, NumberSpaceSymbolPattern, SignNumberSpaceSymbolPattern, CldrPatternFractionDigits },
        // ja: "¤#,##0.00", with the JPY minor units in place of the pattern's two digits.
        { "ja-JP", nameof(Iso4217CurrencyCurrent.JPY), CldrFullStop, CldrComma, SymbolNumberPattern, SignSymbolNumberPattern, YenMinorUnits },
        // A Windows sort order does not change the presentation of de-DE, which is that of de.
        { "de-DE_phoneb", nameof(Iso4217CurrencyCurrent.EUR), CldrComma, CldrFullStop, NumberSpaceSymbolPattern, SignNumberSpaceSymbolPattern, EuroMinorUnits },
        // Region 419 (Latin America) has no currency of its own; es-419 is "¤#,##0.00".
        { "es-419", nameof(Iso4217CurrencyCurrent.XXX), CldrFullStop, CldrComma, SymbolNumberPattern, SignSymbolNumberPattern, CldrPatternFractionDigits },
        // tl is a legacy code that CLDR's language aliases replace with fil, whose pattern is "¤#,##0.00".
        { "tl-PH", nameof(Iso4217CurrencyCurrent.PHP), CldrFullStop, CldrComma, SymbolNumberPattern, SignSymbolNumberPattern, PesoMinorUnits },
        // nb is not a modern locale; its CLDR parent is no, "#,##0.00 ¤" with group U+00A0.
        { "nb-NO", nameof(Iso4217CurrencyCurrent.NOK), CldrComma, CldrNoBreakSpace, NumberSpaceSymbolPattern, SignNumberSpaceSymbolPattern, NorwegianKroneMinorUnits },
        // Bulgaria's current tender is EUR from 2026-01-01; bg is "#,##0.00 ¤" with group U+00A0.
        { "bg-BG", nameof(Iso4217CurrencyCurrent.EUR), CldrComma, CldrNoBreakSpace, NumberSpaceSymbolPattern, SignNumberSpaceSymbolPattern, EuroMinorUnits },
        // 1901 is a variant, not a script (a script is four letters), so de-1901 inherits from de.
        { "de-1901", nameof(Iso4217CurrencyCurrent.XXX), CldrComma, CldrFullStop, NumberSpaceSymbolPattern, SignNumberSpaceSymbolPattern, CldrPatternFractionDigits },
        // The invariant culture's empty name resolves to nothing.
        { "", nameof(Iso4217CurrencyCurrent.XXX), InvariantDecimalSeparator, InvariantGroupSeparator, InvariantPositivePattern, InvariantNegativePattern, InvariantDecimalDigits }
    };

    [Theory]
    [MemberData(nameof(CldrSignAndGroupingTestCases))]
    public void CreateForCultureName_ForCultureName_ReturnsTheCldrSignAndGroupSizes(string testCultureName, string expectedNegativeSign, int[] expectedGroupSizes)
    {
        // act
        var testResult = CurrencyFormatInfo.CreateForCultureName(testCultureName);

        // assert
        testResult.NegativeSign.Should().Be(expectedNegativeSign);
        testResult.CurrencyGroupSizes.Should().Equal(expectedGroupSizes);
    }

    // Values from Unicode CLDR 49.0.0: the minusSign of symbols-numberSystem-latn, and the grouping of the
    // currencyFormats-numberSystem-latn "standard" pattern.
    public static TheoryData<string, string, int[]> CldrSignAndGroupingTestCases => new()
    {
        // en: "-", "¤#,##0.00".
        { "en-US", CldrHyphenMinus, new[] { FirstGroupSize } },
        // sv: U+2212 MINUS SIGN, "#,##0.00 ¤".
        { "sv-SE", CldrMinusSign, new[] { FirstGroupSize } },
        // ar-SA: U+200E LEFT-TO-RIGHT MARK then "-".
        { "ar-SA", CldrLeftToRightMarkHyphenMinus, new[] { FirstGroupSize } },
        // hi: "¤#,##,##0.00", the Indian grouping.
        { "hi-IN", CldrHyphenMinus, new[] { FirstGroupSize, LaterGroupSize } }
    };

    #endregion

    #region Test: CurrentInfo

    [Fact]
    public void CurrentInfo_AfterCurrentCultureChanges_ReflectsTheNewCulture()
    {
        // arrange
        using var newZealandCultureScope = new CurrentCultureScope(new CultureInfo(NewZealandCulture));

        // act
        var newZealandResult = CurrencyFormatInfo.CurrentInfo;
        using var germanCultureScope = new CurrentCultureScope(new CultureInfo(GermanCulture));
        var germanResult = CurrencyFormatInfo.CurrentInfo;

        // assert
        newZealandResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.NZD), "the information reflects the culture in effect when it is read");
        germanResult.CurrencyCode.Should().Be(nameof(Iso4217CurrencyCurrent.EUR), "the information must track a change of culture");
    }

    [Fact]
    public void CurrentInfo_WhenRead_ReturnsAReadOnlyInstance()
    {
        // arrange
        using var cultureScope = new CurrentCultureScope(new CultureInfo(NewZealandCulture));

        // act
        var testResult = CurrencyFormatInfo.CurrentInfo;

        // assert
        testResult.IsReadOnly.Should().BeTrue("the instance is shared between callers");
    }

    #endregion
}
