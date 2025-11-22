using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class SystemTimeExtensionsTests
{
    [Theory]
    [InlineData(2451545.125, 2000, 1, 1, 15, 0, 0)]
    public void ToDateTime_OnSystemTimeValue_ReturnsDateTimeEquivalent(decimal testJdn, ushort expectedYear, ushort expectedMonth, ushort expectedDay, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond);
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 2000, 1, 1)]
    public void ToDateOnly_OnSystemTimeValue_ReturnsDateEquivalent(decimal testJdn, ushort expectedYear, ushort expectedMonth, ushort expectedDay)
    {
        // arrange
        var expectedResult = new DateOnly(expectedYear, expectedMonth, expectedDay);
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToDateOnly();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2451545.125, 15, 0, 0)]
    public void ToTimeOnly_OnSystemTimeValue_ReturnsTimeEquivalent(decimal testJdn, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new TimeOnly(expectedHour, expectedMinute, expectedSecond);
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToTimeOnly();

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