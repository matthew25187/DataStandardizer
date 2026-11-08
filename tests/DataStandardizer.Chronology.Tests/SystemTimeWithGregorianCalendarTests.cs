using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class SystemTimeWithGregorianCalendarTests
{
    [Theory]
    [InlineData(2451545.125, 1)]
    public void Day_OnSystemTime_ReturnsDateDay(decimal testJdn, ushort expectedResult)
    {
        // arrange
        var systemTime = new TestSystemTime(testJdn);
        var testValue = new SystemTimeWithGregorianCalendar(systemTime);

        // act
        var testResult = testValue.Day;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 15)]
    public void Hour_OnSystemTime_ReturnsTimeHour(decimal testJdn, ushort expectedResult)
    {
        // arrange
        var systemTime = new TestSystemTime(testJdn);
        var testValue = new SystemTimeWithGregorianCalendar(systemTime);

        // act
        var testResult = testValue.Hour;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 0)]
    public void Minute_OnSystemTime_ReturnsTimeMinute(decimal testJdn, ushort expectedResult)
    {
        // arrange
        var systemTime = new TestSystemTime(testJdn);
        var testValue = new SystemTimeWithGregorianCalendar(systemTime);

        // act
        var testResult = testValue.Minute;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 1)]
    public void Month_OnSystemTime_ReturnsDateMonth(decimal testJdn, ushort expectedResult)
    {
        // arrange
        var systemTime = new TestSystemTime(testJdn);
        var testValue = new SystemTimeWithGregorianCalendar(systemTime);

        // act
        var testResult = testValue.Month;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 0)]
    public void Second_OnSystemTime_ReturnsTimeSecond(decimal testJdn, ushort expectedResult)
    {
        // arrange
        var systemTime = new TestSystemTime(testJdn);
        var testValue = new SystemTimeWithGregorianCalendar(systemTime);

        // act
        var testResult = testValue.Second;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(-199998L, 42),
     InlineData(2L, 2),
     InlineData(1700000000L, 20)]
    public void Second_OnUnixTime_ReturnsTimeSecond(long testUnixTime, int expectedResult)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new UnixTime(testUnixTime));

        // act
        var testResult = testValue.Second;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(-1L, 1969),
     InlineData(4102444799L, 2099)]
    public void Year_OnUnixTimeJustBeforeMidnight_ReturnsDateYear(long testUnixTime, int expectedResult)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new UnixTime(testUnixTime));

        // act
        var testResult = testValue.Year;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 2000)]
    public void Year_OnSystemTime_ReturnsDateYear(decimal testJdn, ushort expectedResult)
    {
        // arrange
        var systemTime = new TestSystemTime(testJdn);
        var testValue = new SystemTimeWithGregorianCalendar(systemTime);

        // act
        var testResult = testValue.Year;

        // assert
        testResult.Should().Be(expectedResult);
    }

    private struct TestSystemTime : ISystemTime
    {
        public TestSystemTime(decimal jdn)
        {
            JulianDayNumber = jdn;
        }

        public decimal JulianDayNumber { get; }
    }
}