using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class DateOnlyExtensionsTests
{
    [Theory]
    [InlineData(1980, 1, 1, 0x00210000),
     InlineData(1980, 12, 31, 0x19F0000),
     InlineData(1984, 2, 29, 0x085d0000),
     InlineData(1999, 12, 31, 0x279F0000),
     InlineData(2000, 1, 1, 0x28210000),
     InlineData(2025, 11, 23, 0x5B770000),
     InlineData(2107, 12, 31, 0xFF9F0000)]
    public void ToDosDateTime_OnDateOnlyValue_ReturnsDosDateTimeEquivalent(ushort testYear, ushort testMonth, ushort testDay, uint expectedResult)
    {
        // arrange
        var testDate = new DateOnly(testYear, testMonth, testDay);

        // act
        var testResult = testDate.ToDosDateTime();

        // assert
        ((uint)testResult).Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(1970, 1, 1, 0),
     InlineData(1970, 1, 2, 86400)]
    public void ToUnixTime_OnDateOnlyValue_ReturnsUnixTimeEquivalent(ushort testYear, ushort testMonth, ushort testDay, long expectedResult)
    {
        // arrange
        var testDate = new DateOnly(testYear, testMonth, testDay);

        // act
        var testResult = testDate.ToUnixTime();

        // assert
        ((long)testResult).Should().Be(expectedResult);
    }
}