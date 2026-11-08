using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class SystemTimeExtensionsTests
{
    [Theory]
    [InlineData(2451545.125, 2000, 1, 1, 15, 0, 0),
     InlineData(2451544.625, 2000,1,1,3, 0, 0)]
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
    [InlineData(2451545.125, 15, 0, 0),
     InlineData(2451544.625,3,0,0)]
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

    // About half a second after noon on 2000-01-01.
    [Theory]
    [InlineData(2451545.000005787, 2000, 1, 1, 12, 0, 0)]
    public void ToDateTime_OnSystemTimeValueWithFractionOfSecond_DropsFraction(decimal testJdn, ushort expectedYear, ushort expectedMonth, ushort expectedDay, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond);
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    // Each Julian Day Number of these values is inexact, and falls a tiny fraction below the whole second it stands for.
    [Theory]
    [InlineData(-199998L, 1969, 12, 29, 16, 26, 42),
     InlineData(-1L, 1969, 12, 31, 23, 59, 59),
     InlineData(0L, 1970, 1, 1, 0, 0, 0),
     InlineData(2L, 1970, 1, 1, 0, 0, 2),
     InlineData(86399L, 1970, 1, 1, 23, 59, 59),
     InlineData(123456789L, 1973, 11, 29, 21, 33, 9),
     InlineData(946684802L, 2000, 1, 1, 0, 0, 2),
     InlineData(1700000000L, 2023, 11, 14, 22, 13, 20),
     InlineData(4102444799L, 2099, 12, 31, 23, 59, 59)]
    public void ToDateTime_OnUnixTimeValue_ReturnsDateTimeEquivalent(long testUnixTime, ushort expectedYear, ushort expectedMonth, ushort expectedDay, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond);
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(-199998L),
     InlineData(-1L),
     InlineData(0L),
     InlineData(2L),
     InlineData(86399L),
     InlineData(123456789L),
     InlineData(946684802L),
     InlineData(1700000000L),
     InlineData(4102444799L)]
    public void ToDateTime_OnUnixTimeValue_RoundTripsThroughToUnixTime(long testUnixTime)
    {
        // arrange
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToDateTime().ToUnixTime();

        // assert
        ((long)testResult).Should().Be(testUnixTime);
    }

    // DOS time 0x0001 is two seconds past midnight; a DOS time counts seconds in units of two.
    [Theory]
    [InlineData(0x00210001U, 1980, 1, 1, 0, 0, 2)]
    public void ToDateTime_OnDosDateTimeValue_ReturnsDateTimeEquivalent(uint testDosDateTime, ushort expectedYear, ushort expectedMonth, ushort expectedDay, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new DateTime(expectedYear, expectedMonth, expectedDay, expectedHour, expectedMinute, expectedSecond);
        var testValue = new DosDateTime(testDosDateTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(-199998L, 16, 26, 42),
     InlineData(2L, 0, 0, 2),
     InlineData(1700000000L, 22, 13, 20)]
    public void ToTimeOnly_OnUnixTimeValue_ReturnsTimeEquivalent(long testUnixTime, ushort expectedHour, ushort expectedMinute, ushort expectedSecond)
    {
        // arrange
        var expectedResult = new TimeOnly(expectedHour, expectedMinute, expectedSecond);
        var testValue = new UnixTime(testUnixTime);

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