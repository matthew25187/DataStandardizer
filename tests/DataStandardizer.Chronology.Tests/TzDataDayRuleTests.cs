using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

// Resolve is tested through TzDataRule.GetTransitionDate in TzDataRuleTests. TryResolve has no public entry point.
public class TzDataDayRuleTests
{
    private const int January = 1;
    private const int February = 2;
    private const int December = 12;
    private const int FirstDayOfMonth = 1;
    private const int LastDayOfDecember = 31;
    private const int February29 = 29;
    private const string YearParameterName = "year";

    // The first year that DateTime can represent, in which January 1 is a Monday, and the last, in which December 31 is a Friday.
    private const int FirstDateTimeYear = 1;
    private const int LastDateTimeYear = 9999;

    // The year before the first year that DateTime can represent, and the year after the last.
    private const int YearBeforeFirstDateTimeYear = 0;
    private const int YearAfterLastDateTimeYear = 10000;

    // A common year.
    private const int CommonYear = 2026;

    [Theory]
    [InlineData(FirstDateTimeYear, January, TzDataDayKind.WeekdayOnOrBefore, FirstDayOfMonth, DayOfWeek.Sunday)]
    [InlineData(LastDateTimeYear, December, TzDataDayKind.WeekdayOnOrAfter, LastDayOfDecember, DayOfWeek.Sunday)]
    [InlineData(YearBeforeFirstDateTimeYear, January, TzDataDayKind.DayOfMonth, FirstDayOfMonth, null)]
    [InlineData(YearAfterLastDateTimeYear, January, TzDataDayKind.DayOfMonth, FirstDayOfMonth, null)]
    public void TryResolve_DateOutsideDateTimeRange_ReturnsFalse(int testYear, int testMonth, TzDataDayKind testDayKind, int testDay, DayOfWeek? testDayOfWeek)
    {
        // arrange

        // act
        var testResult = TzDataDayRule.TryResolve(testYear, testMonth, testDayKind, testDay, testDayOfWeek, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void TryResolve_DateWithinDateTimeRange_ReturnsTrueAndDate()
    {
        // arrange

        // act
        var testResult = TzDataDayRule.TryResolve(FirstDateTimeYear, January, TzDataDayKind.WeekdayOnOrAfter, FirstDayOfMonth, DayOfWeek.Sunday, out var testDate);

        // assert
        testResult.Should().BeTrue();
        testDate.Should().Be(new DateTime(FirstDateTimeYear, January, 7));
    }

    [Theory]
    [InlineData(TzDataDayKind.DayOfMonth, null)]
    [InlineData(TzDataDayKind.WeekdayOnOrAfter, DayOfWeek.Sunday)]
    public void TryResolve_Feb29CommonYear_ThrowsArgumentOutOfRangeException(TzDataDayKind testDayKind, DayOfWeek? testDayOfWeek)
    {
        // arrange

        // act
        Action testAction = () => TzDataDayRule.TryResolve(CommonYear, February, testDayKind, February29, testDayOfWeek, out _);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(YearParameterName);
    }
}
