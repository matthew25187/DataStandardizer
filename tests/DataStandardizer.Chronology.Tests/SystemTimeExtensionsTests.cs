using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class SystemTimeExtensionsTests
{
    // Unix time 0 is the Unix epoch, 1970-01-01T00:00:00Z.
    private const long UnixEpoch = 0L;
    private const long OneSecondBeforeEpoch = -1L;
    private const long UnixTime20231114T221320Z = 1700000000L;

    // About 3.2 million years from the epoch, too far for the day number arithmetic.
    private const long FarFromEpoch = 100000000000000L;

    // DOS date 0x0021 is 1980-01-01; DOS time 0x0001 is two seconds past midnight, as a DOS time counts seconds in units of two.
    private const uint DosDateTime19800101T000002 = 0x00210001U;

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

    // Julian Day Numbers a fraction of a second after noon on 2000-01-01 (JDN 2451545), and what each gives.
    public static TheoryData<decimal, DateTime> FractionOfSecondCases => new()
    {
        // Half a second after noon.
        { 2451545.0000057870370370370370m, new DateTime(2000, 1, 1, 12, 0, 0) },
        // Two nanoseconds before 12:00:01, outside the tolerance of half a nanosecond.
        { 2451545.0000115740740509259259m, new DateTime(2000, 1, 1, 12, 0, 0) },
        // 0.4 nanoseconds before 12:00:01, within the tolerance of half a nanosecond.
        { 2451545.0000115740740694444444m, new DateTime(2000, 1, 1, 12, 0, 1) },
    };

    [Theory]
    [MemberData(nameof(FractionOfSecondCases))]
    public void ToDateTime_OnSystemTimeValueWithFractionOfSecond_DropsFractionBeyondTolerance(decimal testJdn, DateTime expectedResult)
    {
        // arrange
        var testValue = new TestSystemTime(testJdn);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult);
    }

    // Each Julian Day Number of these values is inexact, and falls a tiny fraction below the whole second it stands for.
    [Theory]
    [InlineData(-199998L, 1969, 12, 29, 16, 26, 42),
     InlineData(OneSecondBeforeEpoch, 1969, 12, 31, 23, 59, 59),
     InlineData(UnixEpoch, 1970, 1, 1, 0, 0, 0),
     InlineData(2L, 1970, 1, 1, 0, 0, 2),
     InlineData(86399L, 1970, 1, 1, 23, 59, 59),
     InlineData(123456789L, 1973, 11, 29, 21, 33, 9),
     InlineData(946684802L, 2000, 1, 1, 0, 0, 2),
     InlineData(UnixTime20231114T221320Z, 2023, 11, 14, 22, 13, 20),
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
     InlineData(OneSecondBeforeEpoch),
     InlineData(UnixEpoch),
     InlineData(2L),
     InlineData(86399L),
     InlineData(123456789L),
     InlineData(946684802L),
     InlineData(UnixTime20231114T221320Z),
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

    [Theory]
    [InlineData(OneSecondBeforeEpoch),
     InlineData(UnixEpoch),
     InlineData(UnixTime20231114T221320Z)]
    public void ToDateTime_OnUnixTimeValue_ReturnsUtcKind(long testUnixTime)
    {
        // arrange
        var testValue = new UnixTime(testUnixTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(DosDateTime19800101T000002)]
    public void ToDateTime_OnDosDateTimeValue_ReturnsUnspecifiedKind(uint testDosDateTime)
    {
        // arrange
        var testValue = new DosDateTime(testDosDateTime);

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(UnixEpoch)]
    public void ToDateTime_OnSystemTimeWithGregorianCalendarWrappingUnixTime_ReturnsUtcKind(long testUnixTime)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new UnixTime(testUnixTime));

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Theory]
    [InlineData(DosDateTime19800101T000002)]
    public void ToDateTime_OnSystemTimeWithGregorianCalendarWrappingDosDateTime_ReturnsUnspecifiedKind(uint testDosDateTime)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new DosDateTime(testDosDateTime));

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Theory]
    [InlineData(FarFromEpoch),
     InlineData(-FarFromEpoch)]
    public void ToDateTime_OnUnixTimeFarFromEpoch_ThrowsOverflowException(long testUnixTime)
    {
        // arrange
        var testValue = new UnixTime(testUnixTime);

        // act
        Action testAction = () => testValue.ToDateTime();

        // assert
        testAction.Should().Throw<OverflowException>();
    }

    [Theory]
    [InlineData(DosDateTime19800101T000002, 1980, 1, 1, 0, 0, 2)]
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
     InlineData(UnixTime20231114T221320Z, 22, 13, 20)]
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