using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataZoneCalculatorTests
{
    private const string TestRuleName = "Test";
    private const string TestFormat = "T%sT";
    private const string TestLetter = "D";
    private const int January = 1;
    private const int February = 2;
    private const int February29 = 29;
    private const string YearParameterName = "year";

    // A leap year and the common year after it.
    private const int LeapYear = 2000;
    private const int CommonYearAfterLeapYear = 2001;

    private static TzDataZoneLine CreateZoneLine(TzDataRule rule)
    {
        return new TzDataZoneLine(TimeSpan.Zero, TzDataZoneRuleKind.RuleSet, null, new[] { rule }, TestFormat, null);
    }

    private static TzDataRule CreateRule(int fromYear, int toYear, int month, TzDataDayKind dayKind, int day, DayOfWeek? dayOfWeek)
    {
        return new TzDataRule(TestRuleName, fromYear, toYear, month, dayKind, day, dayOfWeek, TimeSpan.Zero, TzDataTimeReference.Wall, TimeSpan.FromHours(1), true, TestLetter);
    }

    [Fact]
    public void Constructor_RuleOnFeb29InCommonYear_ThrowsArgumentOutOfRangeException()
    {
        // arrange
        var testZoneLine = CreateZoneLine(CreateRule(LeapYear, CommonYearAfterLeapYear, February, TzDataDayKind.DayOfMonth, February29, null));

        // act
        Action testAction = () => new TzDataZoneCalculator(new[] { testZoneLine });

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be(YearParameterName);
    }

    [Fact]
    public void Constructor_RuleBeforeFirstDateTimeDay_DoesNotThrow()
    {
        // arrange
        // January 1 of year 1 is a Monday, so Sun<=1 falls before the first day that DateTime can represent.
        var testZoneLine = CreateZoneLine(CreateRule(int.MinValue, int.MaxValue, January, TzDataDayKind.WeekdayOnOrBefore, 1, DayOfWeek.Sunday));

        // act
        Action testAction = () => new TzDataZoneCalculator(new[] { testZoneLine });

        // assert
        testAction.Should().NotThrow();
    }
}
