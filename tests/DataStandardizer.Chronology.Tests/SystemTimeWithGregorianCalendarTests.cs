using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class SystemTimeWithGregorianCalendarTests
{
    // The Julian Day Number of noon on 1 January 2000.
    private const decimal Jdn20000101At120000 = 2451545m;

    public static TheoryData<string, Func<object>> DescribingMember_TestCases => new()
    {
        { nameof(SystemTimeWithGregorianCalendar.JulianDayNumber), () => default(SystemTimeWithGregorianCalendar).JulianDayNumber },
        { nameof(SystemTimeWithGregorianCalendar.Year), () => default(SystemTimeWithGregorianCalendar).Year },
        { nameof(SystemTimeWithGregorianCalendar.Month), () => default(SystemTimeWithGregorianCalendar).Month },
        { nameof(SystemTimeWithGregorianCalendar.Day), () => default(SystemTimeWithGregorianCalendar).Day },
        { nameof(SystemTimeWithGregorianCalendar.Hour), () => default(SystemTimeWithGregorianCalendar).Hour },
        { nameof(SystemTimeWithGregorianCalendar.Minute), () => default(SystemTimeWithGregorianCalendar).Minute },
        { nameof(SystemTimeWithGregorianCalendar.Second), () => default(SystemTimeWithGregorianCalendar).Second },
        { nameof(SystemTimeExtensions.ToDateTime), () => default(SystemTimeWithGregorianCalendar).ToDateTime() },
        { nameof(SystemTimeExtensions.ToDateOnly), () => default(SystemTimeWithGregorianCalendar).ToDateOnly() },
        { nameof(SystemTimeExtensions.ToTimeOnly), () => default(SystemTimeWithGregorianCalendar).ToTimeOnly() },
    };

    public static TheoryData<string, Func<object?>> BenignMember_TestCases => new()
    {
        { nameof(SystemTimeWithGregorianCalendar.GetHashCode), () => default(SystemTimeWithGregorianCalendar).GetHashCode() },
        { nameof(SystemTimeWithGregorianCalendar.ToString), () => default(SystemTimeWithGregorianCalendar).ToString() },
        { nameof(SystemTimeExtensions.AsDosDateTime), () => default(SystemTimeWithGregorianCalendar).AsDosDateTime() },
        { nameof(SystemTimeExtensions.AsUnixTime), () => default(SystemTimeWithGregorianCalendar).AsUnixTime() },
    };

    [Theory]
    [MemberData(nameof(DescribingMember_TestCases))]
    public void DescribingMember_DefaultInstance_ThrowsInvalidOperationException(string testMemberName, Func<object> testMember)
    {
        // arrange

        // act
        Action testAction = () => testMember();

        // assert
        testAction.Should().Throw<InvalidOperationException>(testMemberName);
    }

    [Theory]
    [MemberData(nameof(BenignMember_TestCases))]
    public void BenignMember_DefaultInstance_DoesNotThrow(string testMemberName, Func<object?> testMember)
    {
        // arrange

        // act
        Action testAction = () => testMember();

        // assert
        testAction.Should().NotThrow(testMemberName);
    }

    [Fact]
    public void Equals_TwoDefaultInstances_ReturnsTrue()
    {
        // arrange
        var testValue = default(SystemTimeWithGregorianCalendar);

        // act
        var testResult = testValue.Equals(default(SystemTimeWithGregorianCalendar));

        // assert
        testResult.Should().BeTrue();
    }

    [Fact]
    public void Equals_DefaultAndInitialisedInstance_ReturnsFalse()
    {
        // arrange
        var testValue = default(SystemTimeWithGregorianCalendar);
        var testOther = new SystemTimeWithGregorianCalendar(new TestSystemTime(Jdn20000101At120000));

        // act
        var testResult = testValue.Equals(testOther);

        // assert
        testResult.Should().BeFalse();
    }

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

    // About 3.2 million years from the epoch, too far for the day number arithmetic.
    [Theory]
    [InlineData(100000000000000L),
     InlineData(-100000000000000L)]
    public void Year_OnUnixTimeFarFromEpoch_ThrowsOverflowException(long testUnixTime)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new UnixTime(testUnixTime));

        // act
        Action testAction = () => _ = testValue.Year;

        // assert
        testAction.Should().Throw<OverflowException>();
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