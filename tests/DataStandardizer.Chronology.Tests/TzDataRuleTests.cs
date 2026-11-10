using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataRuleTests
{
    private const string TestRuleName = "Test";
    private const string DayParameterName = "day";
    private const string DayOfWeekParameterName = "dayOfWeek";
    private const string MonthParameterName = "month";
    private const string YearParameterName = "year";

    // The rule of TZ Database 2026e for the end of Irish Standard Time from 1996: Rule Eire 1996 max - Oct lastSun 1:00u -1:00 -
    // Its negative save is daylight saving time, and its LETTER/S column, "-", is an empty letter.
    private const string EireRuleName = "Eire";
    private const int EireFromYear = 1996;
    private const int EireMonth = 10;

    // The first year of the rules whose years are tested, the year before it and the year after it.
    private const int RuleFromYear = 2007;
    private const int YearBeforeRuleFromYear = 2006;
    private const int YearAfterRuleFromYear = 2008;

    // The last year that DateTime can represent, and the year after it.
    private const int LastDateTimeYear = 9999;
    private const int YearAfterLastDateTimeYear = 10000;

    // A value that is not a member of DayOfWeek, and one that is not a member of TzDataDayKind.
    private const DayOfWeek UndefinedDayOfWeek = (DayOfWeek)7;
    private const TzDataDayKind UndefinedDayKind = (TzDataDayKind)4;

    private static TzDataRule CreateRule(int month, TzDataDayKind dayKind, int? day, DayOfWeek? dayOfWeek, int fromYear = 1900, int toYear = int.MaxValue)
    {
        return new TzDataRule(TestRuleName, fromYear, toYear, month, dayKind, day, dayOfWeek, TimeSpan.FromHours(2), TzDataTimeReference.Wall, TimeSpan.FromHours(1), true, "D");
    }

    [Theory]
    [InlineData(10, 5, 2024, 2024, 10, 5)]
    [InlineData(4, 30, 2024, 2024, 4, 30)]
    [InlineData(12, 31, 2025, 2025, 12, 31)]
    [InlineData(2, 28, 2023, 2023, 2, 28)]
    [InlineData(2, 29, 2024, 2024, 2, 29)]
    [InlineData(2, 29, 2000, 2000, 2, 29)]
    public void GetTransitionDate_DayOfMonth_ReturnsThatDay(int testMonth, int testDay, int testYear, int expectedYear, int expectedMonth, int expectedDay)
    {
        // arrange
        var testRule = CreateRule(testMonth, TzDataDayKind.DayOfMonth, testDay, null);

        // act
        var testResult = testRule.GetTransitionDate(testYear);

        // assert
        testResult.Should().Be(new DateTime(expectedYear, expectedMonth, expectedDay));
    }

    [Theory]
    [InlineData(2023)]
    [InlineData(1900)]
    [InlineData(2100)]
    public void GetTransitionDate_February29InCommonYear_ThrowsArgumentOutOfRangeException(int testYear)
    {
        // arrange
        var testRule = CreateRule(2, TzDataDayKind.DayOfMonth, 29, null);

        // act
        Action testAction = () => testRule.GetTransitionDate(testYear);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(YearParameterName);
    }

    [Theory]
    [InlineData(3, DayOfWeek.Sunday, 2024, 2024, 3, 31)]
    [InlineData(10, DayOfWeek.Sunday, 2024, 2024, 10, 27)]
    [InlineData(3, DayOfWeek.Sunday, 2026, 2026, 3, 29)]
    [InlineData(2, DayOfWeek.Sunday, 2024, 2024, 2, 25)]
    [InlineData(2, DayOfWeek.Sunday, 2026, 2026, 2, 22)]
    [InlineData(2, DayOfWeek.Thursday, 2024, 2024, 2, 29)]
    [InlineData(2, DayOfWeek.Thursday, 2023, 2023, 2, 23)]
    [InlineData(4, DayOfWeek.Tuesday, 2025, 2025, 4, 29)]
    public void GetTransitionDate_LastWeekday_ReturnsLastSuchWeekdayOfMonth(int testMonth, DayOfWeek testDayOfWeek, int testYear, int expectedYear, int expectedMonth, int expectedDay)
    {
        // arrange
        var testRule = CreateRule(testMonth, TzDataDayKind.LastWeekday, null, testDayOfWeek);

        // act
        var testResult = testRule.GetTransitionDate(testYear);

        // assert
        testResult.Should().Be(new DateTime(expectedYear, expectedMonth, expectedDay));
    }

    [Theory]
    [InlineData(3, DayOfWeek.Sunday, 8, 2024, 2024, 3, 10)]
    [InlineData(3, DayOfWeek.Sunday, 8, 2026, 2026, 3, 8)]
    [InlineData(11, DayOfWeek.Sunday, 1, 2024, 2024, 11, 3)]
    [InlineData(2, DayOfWeek.Sunday, 29, 2024, 2024, 3, 3)]
    [InlineData(2, DayOfWeek.Sunday, 29, 2023, 2023, 3, 5)]
    [InlineData(4, DayOfWeek.Sunday, 30, 2023, 2023, 4, 30)]
    [InlineData(4, DayOfWeek.Monday, 30, 2023, 2023, 5, 1)]
    [InlineData(12, DayOfWeek.Monday, 31, 2024, 2025, 1, 6)]
    public void GetTransitionDate_WeekdayOnOrAfter_ReturnsFirstSuchWeekdayFromDay(int testMonth, DayOfWeek testDayOfWeek, int testDay, int testYear, int expectedYear, int expectedMonth, int expectedDay)
    {
        // arrange
        var testRule = CreateRule(testMonth, TzDataDayKind.WeekdayOnOrAfter, testDay, testDayOfWeek);

        // act
        var testResult = testRule.GetTransitionDate(testYear);

        // assert
        testResult.Should().Be(new DateTime(expectedYear, expectedMonth, expectedDay));
    }

    [Theory]
    [InlineData(3, DayOfWeek.Sunday, 25, 2024, 2024, 3, 24)]
    [InlineData(3, DayOfWeek.Sunday, 25, 2026, 2026, 3, 22)]
    [InlineData(3, DayOfWeek.Sunday, 25, 2018, 2018, 3, 25)]
    [InlineData(4, DayOfWeek.Friday, 1, 2010, 2010, 3, 26)]
    [InlineData(4, DayOfWeek.Friday, 1, 2011, 2011, 4, 1)]
    [InlineData(1, DayOfWeek.Saturday, 1, 2025, 2024, 12, 28)]
    [InlineData(3, DayOfWeek.Thursday, 1, 2024, 2024, 2, 29)]
    public void GetTransitionDate_WeekdayOnOrBefore_ReturnsLastSuchWeekdayToDay(int testMonth, DayOfWeek testDayOfWeek, int testDay, int testYear, int expectedYear, int expectedMonth, int expectedDay)
    {
        // arrange
        var testRule = CreateRule(testMonth, TzDataDayKind.WeekdayOnOrBefore, testDay, testDayOfWeek);

        // act
        var testResult = testRule.GetTransitionDate(testYear);

        // assert
        testResult.Should().Be(new DateTime(expectedYear, expectedMonth, expectedDay));
    }

    [Fact]
    public void GetTransitionDate_AnyRule_ReturnsMidnightOfUnspecifiedKind()
    {
        // arrange
        var testRule = CreateRule(3, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday);

        // act
        var testResult = testRule.GetTransitionDate(2024);

        // assert
        testResult.TimeOfDay.Should().Be(TimeSpan.Zero);
        testResult.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(RuleFromYear, RuleFromYear, RuleFromYear)]
    [InlineData(RuleFromYear, int.MaxValue, RuleFromYear)]
    [InlineData(RuleFromYear, int.MaxValue, LastDateTimeYear)]
    public void GetTransitionDate_YearWithinRule_DoesNotThrow(int testFromYear, int testToYear, int testYear)
    {
        // arrange
        var testRule = CreateRule(3, TzDataDayKind.WeekdayOnOrAfter, 8, DayOfWeek.Sunday, testFromYear, testToYear);

        // act
        Action testAction = () => testRule.GetTransitionDate(testYear);

        // assert
        testAction.Should().NotThrow();
    }

    [Theory]
    [InlineData(RuleFromYear, RuleFromYear, YearBeforeRuleFromYear)]
    [InlineData(RuleFromYear, RuleFromYear, YearAfterRuleFromYear)]
    [InlineData(RuleFromYear, int.MaxValue, YearBeforeRuleFromYear)]
    [InlineData(RuleFromYear, int.MaxValue, YearAfterLastDateTimeYear)]
    public void GetTransitionDate_YearOutsideRuleOrDateTimeRange_ThrowsArgumentOutOfRangeException(int testFromYear, int testToYear, int testYear)
    {
        // arrange
        var testRule = CreateRule(3, TzDataDayKind.WeekdayOnOrAfter, 8, DayOfWeek.Sunday, testFromYear, testToYear);

        // act
        Action testAction = () => testRule.GetTransitionDate(testYear);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(YearParameterName);
    }

    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        // act
        var testResult = new TzDataRule(EireRuleName, EireFromYear, int.MaxValue, EireMonth, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, TimeSpan.FromHours(1), TzDataTimeReference.Universal, TimeSpan.FromHours(-1), true, string.Empty);

        // assert
        testResult.Name.Should().Be(EireRuleName);
        testResult.FromYear.Should().Be(EireFromYear);
        testResult.ToYear.Should().Be(int.MaxValue);
        testResult.Month.Should().Be(EireMonth);
        testResult.DayKind.Should().Be(TzDataDayKind.LastWeekday);
        testResult.Day.Should().BeNull();
        testResult.DayOfWeek.Should().Be(DayOfWeek.Sunday);
        testResult.AtTime.Should().Be(TimeSpan.FromHours(1));
        testResult.AtTimeReference.Should().Be(TzDataTimeReference.Universal);
        testResult.Save.Should().Be(TimeSpan.FromHours(-1));
        testResult.IsDaylight.Should().BeTrue();
        testResult.Letter.Should().BeEmpty();
    }

    public static IEnumerable<object?[]> Constructor_InvalidDaySpecification_TestCases
    {
        get
        {
            yield return new object?[] { 0, TzDataDayKind.DayOfMonth, 1, null, MonthParameterName };
            yield return new object?[] { 13, TzDataDayKind.DayOfMonth, 1, null, MonthParameterName };
            yield return new object?[] { 1, TzDataDayKind.DayOfMonth, null, null, DayParameterName };
            yield return new object?[] { 1, TzDataDayKind.DayOfMonth, 0, null, DayParameterName };
            yield return new object?[] { 4, TzDataDayKind.DayOfMonth, 31, null, DayParameterName };
            yield return new object?[] { 2, TzDataDayKind.DayOfMonth, 30, null, DayParameterName };
            yield return new object?[] { 1, TzDataDayKind.DayOfMonth, 1, DayOfWeek.Sunday, DayOfWeekParameterName };
            yield return new object?[] { 1, TzDataDayKind.LastWeekday, 1, DayOfWeek.Sunday, DayParameterName };
            yield return new object?[] { 1, TzDataDayKind.LastWeekday, null, null, DayOfWeekParameterName };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrAfter, 8, null, DayOfWeekParameterName };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrBefore, null, DayOfWeek.Sunday, DayParameterName };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrBefore, 32, DayOfWeek.Sunday, DayParameterName };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrAfter, 8, UndefinedDayOfWeek, DayOfWeekParameterName };
            yield return new object?[] { 1, UndefinedDayKind, 8, DayOfWeek.Sunday, "dayKind" };
        }
    }

    [Theory]
    [MemberData(nameof(Constructor_InvalidDaySpecification_TestCases))]
    public void Constructor_InvalidDaySpecification_ThrowsArgumentException(int testMonth, TzDataDayKind testDayKind, int? testDay, DayOfWeek? testDayOfWeek, string expectedParamName)
    {
        // act
        Action testAction = () => CreateRule(testMonth, testDayKind, testDay, testDayOfWeek);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(expectedParamName);
    }

    [Fact]
    public void Constructor_ToYearBeforeFromYear_ThrowsArgumentOutOfRangeException()
    {
        // act
        Action testAction = () => CreateRule(3, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, RuleFromYear, YearBeforeRuleFromYear);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("toYear");
    }

    [Fact]
    public void Constructor_NullLetter_ThrowsArgumentNullException()
    {
        // act
        Action testAction = () => new TzDataRule(TestRuleName, RuleFromYear, RuleFromYear, 3, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, TimeSpan.Zero, TzDataTimeReference.Wall, TimeSpan.Zero, false, null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("letter");
    }
}
