using System.Collections.Concurrent;
using FluentAssertions;

namespace DataStandardizer.File.CSV.IntegrationTests;

public class ParallelRunnerTests
{
    private const int CallCount = 4;

    [Fact]
    public void Run_SeveralCalls_ReturnsResultOfEachCallAtItsIndex()
    {
        // act
        var testResult = ParallelRunner.Run(CallCount, index => index);

        // assert
        testResult.Should().Equal(0, 1, 2, 3);
    }

    [Fact]
    public void Run_SeveralCalls_MakesEachCallOnce()
    {
        // arrange
        var calledIndexes = new ConcurrentQueue<int>();

        // act
        _ = ParallelRunner.Run(CallCount, index =>
        {
            calledIndexes.Enqueue(index);
            return index;
        });

        // assert
        calledIndexes.Should().BeEquivalentTo(new[] { 0, 1, 2, 3 });
    }

    [Fact]
    public void Run_CallThrows_ThrowsAggregateExceptionWithCallException()
    {
        // act
        Action testAction = () => ParallelRunner.Run<int>(CallCount, _ => throw new InvalidOperationException());

        // assert
        testAction.Should().Throw<AggregateException>().WithInnerException<InvalidOperationException>();
    }
}
