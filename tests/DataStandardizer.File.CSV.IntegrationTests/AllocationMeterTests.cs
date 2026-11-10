using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class AllocationMeterTests
{
    private const int ArrayLength = 1_000_000;

    [Fact]
    public void BytesAllocatedBy_ActionAllocatingAnArray_CountsTheArray()
    {
        // arrange
        Action action = () => GC.KeepAlive(new byte[ArrayLength]);

        // act
        var testResult = AllocationMeter.BytesAllocatedBy(action, out _);

        // assert
        testResult.Should().BeGreaterThanOrEqualTo(ArrayLength);
    }

    [Fact]
    public void BytesAllocatedBy_ActionThatThrows_GivesTheException()
    {
        // arrange
        var thrown = new InvalidOperationException();
        Action action = () => throw thrown;

        // act
        AllocationMeter.BytesAllocatedBy(action, out var testResult);

        // assert
        testResult.Should().BeSameAs(thrown);
    }

    [Fact]
    public void BytesAllocatedBy_ActionThatDoesNotThrow_GivesNoException()
    {
        // arrange
        Action action = () => { };

        // act
        AllocationMeter.BytesAllocatedBy(action, out var testResult);

        // assert
        testResult.Should().BeNull();
    }
}
