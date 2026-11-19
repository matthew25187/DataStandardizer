using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataDayRuleTests
{
    private const int February = 2;
    private const int January = 1;
    private const int March = 3;
    private const int February29 = 29;
    private const string YearParameterName = "year";

    // A common year in which March 1 is a Sunday, so counting back from the day after February 28 would give March 1.
    private const int CommonYearMarch1Sunday = 2026;

    // A common year in which February 28 is itself a Sunday.
    private const int CommonYearFebruary28Sunday = 2027;

    // A leap year in which February 29 is a Sunday.
    private const int LeapYearFebruary29Sunday = 2004;

    // A leap year in which February 29 is a Thursday.
    private const int LeapYearFebruary29Thursday = 2024;

    // The first year that DateTime can represent, in which January 1 is a Monday, and the last.
    private const int FirstDateTimeYear = 1;
    private const int LastDateTimeYear = 9999;

    [Theory]
    [InlineData(CommonYearMarch1Sunday, 22)]
    [InlineData(CommonYearFebruary28Sunday, 28)]
    public void Resolve_WeekdayOnOrBeforeFeb29CommonYear_CountsBackFromFeb28(int testYear, int expectedDay)
    {
        // arrange

        // act
        var testResult = TzDataDayRule.Resolve(testYear, February, TzDataDayKind.WeekdayOnOrBefore, February29, DayOfWeek.Sunday);

        // assert
        testResult.Should().Be(new DateTime(testYear, February, expectedDay));
    }

    [Fact]
    public void Resolve_WeekdayOnOrAfterFeb29CommonYear_Throws()
    {
        // arrange

        // act
        Action testAction = () => TzDataDayRule.Resolve(CommonYearMarch1Sunday, February, TzDataDayKind.WeekdayOnOrAfter, February29, DayOfWeek.Sunday);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(YearParameterName);
    }

    [Fact]
    public void Resolve_DayOfMonthFeb29CommonYear_Throws()
    {
        // arrange

        // act
        Action testAction = () => TzDataDayRule.Resolve(CommonYearMarch1Sunday, February, TzDataDayKind.DayOfMonth, February29, null);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(YearParameterName);
    }

    [Theory]
    [InlineData(TzDataDayKind.WeekdayOnOrBefore, DayOfWeek.Sunday)]
    [InlineData(TzDataDayKind.WeekdayOnOrAfter, DayOfWeek.Sunday)]
    [InlineData(TzDataDayKind.DayOfMonth, null)]
    public void Resolve_Feb29LeapYear_ReturnsFeb29(TzDataDayKind testDayKind, DayOfWeek? testDayOfWeek)
    {
        // arrange

        // act
        var testResult = TzDataDayRule.Resolve(LeapYearFebruary29Sunday, February, testDayKind, February29, testDayOfWeek);

        // assert
        testResult.Should().Be(new DateTime(LeapYearFebruary29Sunday, February, February29));
    }

    [Theory]
    [InlineData(TzDataDayKind.WeekdayOnOrBefore, LeapYearFebruary29Thursday, February, 25)]
    [InlineData(TzDataDayKind.WeekdayOnOrAfter, LeapYearFebruary29Thursday, March, 3)]
    public void Resolve_WeekdayFromFeb29LeapYear_CountsFromFeb29(TzDataDayKind testDayKind, int testYear, int expectedMonth, int expectedDay)
    {
        // arrange

        // act
        var testResult = TzDataDayRule.Resolve(testYear, February, testDayKind, February29, DayOfWeek.Sunday);

        // assert
        testResult.Should().Be(new DateTime(testYear, expectedMonth, expectedDay));
    }

    [Theory]
    [InlineData(FirstDateTimeYear, January, TzDataDayKind.WeekdayOnOrBefore, 1, DayOfWeek.Sunday)]
    [InlineData(LastDateTimeYear, 12, TzDataDayKind.WeekdayOnOrAfter, 31, DayOfWeek.Sunday)]
    [InlineData(0, January, TzDataDayKind.DayOfMonth, 1, null)]
    [InlineData(10000, January, TzDataDayKind.DayOfMonth, 1, null)]
    public void TryResolve_DateOutsideDateTimeRange_ReturnsFalse(int testYear, int testMonth, TzDataDayKind testDayKind, int testDay, DayOfWeek? testDayOfWeek)
    {
        // arrange

        // act
        var testResult = TzDataDayRule.TryResolve(testYear, testMonth, testDayKind, testDay, testDayOfWeek, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void TryResolve_DayOfMonthFeb29CommonYear_Throws()
    {
        // arrange

        // act
        Action testAction = () => TzDataDayRule.TryResolve(CommonYearMarch1Sunday, February, TzDataDayKind.DayOfMonth, February29, null, out _);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(YearParameterName);
    }
}
