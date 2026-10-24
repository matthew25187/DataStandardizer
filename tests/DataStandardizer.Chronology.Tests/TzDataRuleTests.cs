using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class TzDataRuleTests
{
    private static TzDataRule CreateRule(int month, TzDataDayKind dayKind, int? day, DayOfWeek? dayOfWeek, int fromYear = 1900, int toYear = int.MaxValue)
    {
        return new TzDataRule("Test", fromYear, toYear, month, dayKind, day, dayOfWeek, TimeSpan.FromHours(2), TzDataTimeReference.Wall, TimeSpan.FromHours(1), true, "D");
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
        var testAction = () => testRule.GetTransitionDate(testYear);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("year");
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
    public void GetTransitionDate_ReturnsMidnightOfUnspecifiedKind()
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
    [InlineData(2007, 2007, 2007)]
    [InlineData(2007, int.MaxValue, 2007)]
    [InlineData(2007, int.MaxValue, 9999)]
    public void GetTransitionDate_YearWithinRule_DoesNotThrow(int testFromYear, int testToYear, int testYear)
    {
        // arrange
        var testRule = CreateRule(3, TzDataDayKind.WeekdayOnOrAfter, 8, DayOfWeek.Sunday, testFromYear, testToYear);

        // act
        var testAction = () => testRule.GetTransitionDate(testYear);

        // assert
        testAction.Should().NotThrow();
    }

    [Theory]
    [InlineData(2007, 2007, 2006)]
    [InlineData(2007, 2007, 2008)]
    [InlineData(2007, int.MaxValue, 2006)]
    [InlineData(2007, int.MaxValue, 10000)]
    public void GetTransitionDate_YearOutsideRuleOrDateTimeRange_ThrowsArgumentOutOfRangeException(int testFromYear, int testToYear, int testYear)
    {
        // arrange
        var testRule = CreateRule(3, TzDataDayKind.WeekdayOnOrAfter, 8, DayOfWeek.Sunday, testFromYear, testToYear);

        // act
        var testAction = () => testRule.GetTransitionDate(testYear);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("year");
    }

    [Fact]
    public void Constructor_ValidArguments_SetsProperties()
    {
        // act
        var testResult = new TzDataRule("Eire", 1981, int.MaxValue, 10, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, TimeSpan.FromHours(1), TzDataTimeReference.Universal, TimeSpan.FromHours(-1), true, string.Empty);

        // assert
        testResult.Name.Should().Be("Eire");
        testResult.FromYear.Should().Be(1981);
        testResult.ToYear.Should().Be(int.MaxValue);
        testResult.Month.Should().Be(10);
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
            yield return new object?[] { 0, TzDataDayKind.DayOfMonth, 1, null, "month" };
            yield return new object?[] { 13, TzDataDayKind.DayOfMonth, 1, null, "month" };
            yield return new object?[] { 1, TzDataDayKind.DayOfMonth, null, null, "day" };
            yield return new object?[] { 1, TzDataDayKind.DayOfMonth, 0, null, "day" };
            yield return new object?[] { 4, TzDataDayKind.DayOfMonth, 31, null, "day" };
            yield return new object?[] { 2, TzDataDayKind.DayOfMonth, 30, null, "day" };
            yield return new object?[] { 1, TzDataDayKind.DayOfMonth, 1, DayOfWeek.Sunday, "dayOfWeek" };
            yield return new object?[] { 1, TzDataDayKind.LastWeekday, 1, DayOfWeek.Sunday, "day" };
            yield return new object?[] { 1, TzDataDayKind.LastWeekday, null, null, "dayOfWeek" };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrAfter, 8, null, "dayOfWeek" };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrBefore, null, DayOfWeek.Sunday, "day" };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrBefore, 32, DayOfWeek.Sunday, "day" };
            yield return new object?[] { 1, TzDataDayKind.WeekdayOnOrAfter, 8, (DayOfWeek)7, "dayOfWeek" };
            yield return new object?[] { 1, (TzDataDayKind)4, 8, DayOfWeek.Sunday, "dayKind" };
        }
    }

    [Theory]
    [MemberData(nameof(Constructor_InvalidDaySpecification_TestCases))]
    public void Constructor_InvalidDaySpecification_ThrowsArgumentException(int testMonth, TzDataDayKind testDayKind, int? testDay, DayOfWeek? testDayOfWeek, string expectedParamName)
    {
        // act
        var testAction = () => CreateRule(testMonth, testDayKind, testDay, testDayOfWeek);

        // assert
        testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be(expectedParamName);
    }

    [Fact]
    public void Constructor_ToYearBeforeFromYear_ThrowsArgumentOutOfRangeException()
    {
        // act
        var testAction = () => CreateRule(3, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, 2007, 2006);

        // assert
        testAction.Should().Throw<ArgumentOutOfRangeException>().Which.ParamName.Should().Be("toYear");
    }

    [Fact]
    public void Constructor_NullLetter_ThrowsArgumentNullException()
    {
        // act
        var testAction = () => new TzDataRule("Test", 2007, 2007, 3, TzDataDayKind.LastWeekday, null, DayOfWeek.Sunday, TimeSpan.Zero, TzDataTimeReference.Wall, TimeSpan.Zero, false, null!);

        // assert
        testAction.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("letter");
    }
}
