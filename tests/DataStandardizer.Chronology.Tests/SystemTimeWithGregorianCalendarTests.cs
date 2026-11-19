using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class SystemTimeWithGregorianCalendarTests
{
    // The Julian Day Number of noon on 1 January 2000.
    private const decimal Jdn20000101At120000 = 2451545m;

    // What ToString returns, for a default instance as for any other: the full name of the type.
    private const string TypeFullName = "DataStandardizer.Chronology.SystemTimeWithGregorianCalendar";

    // Unix time 0 is the Unix epoch, 1970-01-01T00:00:00Z.
    private const long UnixEpoch = 0L;

    // DOS date 0x0021 is 1980-01-01; DOS time 0x0001 is two seconds past midnight, as a DOS time counts seconds in units of two.
    private const uint DosDateTime19800101T000002 = 0x00210001U;

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
    public void Equals_InitialisedAndDefaultInstance_ReturnsFalse()
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new TestSystemTime(Jdn20000101At120000));

        // act
        var testResult = testValue.Equals(default(SystemTimeWithGregorianCalendar));

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void Equals_InstancesWrappingEqualSystemTimes_ReturnsTrue()
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new TestSystemTime(Jdn20000101At120000));
        var testOther = new SystemTimeWithGregorianCalendar(new TestSystemTime(Jdn20000101At120000));

        // act
        var testResult = testValue.Equals(testOther);

        // assert
        testResult.Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_DefaultInstance_ReturnsZero()
    {
        // arrange
        var testValue = default(SystemTimeWithGregorianCalendar);

        // act
        var testResult = testValue.GetHashCode();

        // assert
        testResult.Should().Be(0);
    }

    [Fact]
    public void ToString_DefaultInstance_ReturnsTypeFullName()
    {
        // arrange
        var testValue = default(SystemTimeWithGregorianCalendar);

        // act
        var testResult = testValue.ToString();

        // assert
        testResult.Should().Be(TypeFullName);
    }

    [Fact]
    public void AsDosDateTime_DefaultInstance_ReturnsNull()
    {
        // arrange
        var testValue = default(SystemTimeWithGregorianCalendar);

        // act
        var testResult = testValue.AsDosDateTime();

        // assert
        testResult.Should().BeNull();
    }

    [Fact]
    public void AsUnixTime_DefaultInstance_ReturnsNull()
    {
        // arrange
        var testValue = default(SystemTimeWithGregorianCalendar);

        // act
        var testResult = testValue.AsUnixTime();

        // assert
        testResult.Should().BeNull();
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

    [Fact]
    public void Constructor_DefaultWrapper_ThrowsArgumentException()
    {
        // arrange
        ISystemTime testSystemTime = default(SystemTimeWithGregorianCalendar);

        // act
        Action testAction = () => _ = new SystemTimeWithGregorianCalendar(testSystemTime);

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName("systemTime");
    }

    [Fact]
    public void AsUnixTime_NestedWrapper_ReturnsInnerUnixTime()
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new SystemTimeWithGregorianCalendar(new UnixTime(UnixEpoch)));

        // act
        var testResult = testValue.AsUnixTime();

        // assert
        testResult.Should().Be(new UnixTime(UnixEpoch));
    }

    [Fact]
    public void AsDosDateTime_NestedWrapper_ReturnsInnerDosDateTime()
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new SystemTimeWithGregorianCalendar(new DosDateTime(DosDateTime19800101T000002)));

        // act
        var testResult = testValue.AsDosDateTime();

        // assert
        testResult.Should().Be(new DosDateTime(DosDateTime19800101T000002));
    }

    [Fact]
    public void ToDateTime_NestedWrapperOfUnixTime_ReturnsUtcDateTime()
    {
        // arrange
        var expectedResult = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var testValue = new SystemTimeWithGregorianCalendar(new SystemTimeWithGregorianCalendar(new UnixTime(UnixEpoch)));

        // act
        var testResult = testValue.ToDateTime();

        // assert
        testResult.Should().Be(expectedResult).And.BeIn(DateTimeKind.Utc);
    }

    [Fact]
    public void Equals_NestedAndSingleWrapperOfSameValue_ReturnsTrue()
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new SystemTimeWithGregorianCalendar(new UnixTime(UnixEpoch)));
        var testOther = new SystemTimeWithGregorianCalendar(new UnixTime(UnixEpoch));

        // act
        var testResult = testValue.Equals(testOther);

        // assert
        testResult.Should().BeTrue();
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

    // long.MaxValue is 15:30:07 and long.MinValue 08:29:52 on their days.
    [Theory]
    [InlineData(long.MaxValue, 15),
     InlineData(long.MinValue, 8)]
    public void Hour_OnUnixTimeFarFromEpoch_ReturnsTimeHour(long testUnixTime, int expectedResult)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new UnixTime(testUnixTime));

        // act
        var testResult = testValue.Hour;

        // assert
        testResult.Should().Be(expectedResult);
    }

    // long.MaxValue is 15:30:07 and long.MinValue 08:29:52 on their days.
    [Theory]
    [InlineData(long.MaxValue, 30),
     InlineData(long.MinValue, 29)]
    public void Minute_OnUnixTimeFarFromEpoch_ReturnsTimeMinute(long testUnixTime, int expectedResult)
    {
        // arrange
        var testValue = new SystemTimeWithGregorianCalendar(new UnixTime(testUnixTime));

        // act
        var testResult = testValue.Minute;

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(-199998L, 42),
     InlineData(2L, 2),
     InlineData(1700000000L, 20),
     // long.MaxValue is 15:30:07 and long.MinValue 08:29:52 on their days.
     InlineData(long.MaxValue, 7),
     InlineData(long.MinValue, 52)]
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