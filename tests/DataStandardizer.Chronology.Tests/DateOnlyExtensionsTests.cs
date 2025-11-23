using FluentAssertions;

namespace DataStandardizer.Chronology.Tests;

public class DateOnlyExtensionsTests
{
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