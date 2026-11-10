using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class DateTimeExtensionsTests
{
    private const long UnixTime20000101T000002Z = 946684802L;

    [Theory]
    [InlineData(1980, 1, 1, 0, 0, 0, 0x210000),
     InlineData(1980, 1, 1, 23, 59, 58, 0x21BF7D),
     InlineData(1984, 2, 29, 12, 30, 0, 0x85D63C0),
     InlineData(1999, 12, 31, 6, 15, 30, 0x279F31EF),
     InlineData(2000, 1, 1, 15, 45, 10, 0x28217DA5),
     InlineData(2025, 11, 23, 8, 5, 2, 0x5B7740A1),
     InlineData(2107, 12, 31, 23, 59, 58, 0xFF9FBF7D)]
    public void ToDosDateTime_OnDateTimeValue_ReturnsDosDateTimeEquivalent(ushort testYear, ushort testMonth, ushort testDay, ushort testHour, ushort testMinute, ushort testSecond, uint expectedResult)
    {
        // arrange
        var testDateTime = new DateTime(testYear, testMonth, testDay, testHour, testMinute, testSecond);

        // act
        var testResult = testDateTime.ToDosDateTime();

        // assert
        ((uint)testResult).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(2000, 1, 1, 15, 0, 0, 946738800L),
     InlineData(2000, 1, 1, 0, 0, 2, UnixTime20000101T000002Z),
     InlineData(1980, 1, 1, 0, 0, 2, 315532802L),
     InlineData(2023, 11, 14, 22, 13, 20, 1700000000L),
     InlineData(1970, 1, 1, 0, 0, 0, 0L),
     InlineData(1969, 12, 31, 23, 59, 59, -1L),
     InlineData(1969, 12, 29, 16, 26, 42, -199998L)]
    public void ToUnixTime_OnDateTimeValue_ReturnsUnixTimeEquivalent(ushort testYear, ushort testMonth, ushort testDay, ushort testHour, ushort testMinute, ushort testSecond, long expectedResult)
    {
        // arrange
        var testDateTime = new DateTime(testYear, testMonth, testDay, testHour, testMinute, testSecond);

        // act
        var testResult = testDateTime.ToUnixTime();

        // assert
        ((long)testResult).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc, UnixTime20000101T000002Z),
     InlineData(DateTimeKind.Unspecified, UnixTime20000101T000002Z)]
    public void ToUnixTime_OnUtcOrUnspecifiedDateTime_ReadsFieldsAsUtc(DateTimeKind testKind, long expectedResult)
    {
        // arrange
        var testDateTime = new DateTime(2000, 1, 1, 0, 0, 2, testKind);

        // act
        var testResult = testDateTime.ToUnixTime();

        // assert
        ((long)testResult).Should().Be(expectedResult);
    }

    [Fact]
    public void ToUnixTime_OnLocalDateTime_ThrowsArgumentException()
    {
        // arrange
        var testDateTime = new DateTime(2000, 1, 1, 0, 0, 2, DateTimeKind.Local);

        // act
        Action testAction = () => testDateTime.ToUnixTime();

        // assert
        testAction.Should().Throw<ArgumentException>().WithParameterName("dateTime");
    }
}