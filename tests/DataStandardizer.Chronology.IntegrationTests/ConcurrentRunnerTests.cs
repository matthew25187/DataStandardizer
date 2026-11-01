using FluentAssertions;

namespace DataStandardizer.Chronology.IntegrationTests;

public class ConcurrentRunnerTests
{
    private const int ThreadCount = 4;
    private const int CallResult = 42;
    private const string FirstCall = "first";
    private const string SecondCall = "second";
    private const string ThirdCall = "third";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    // A timeout that a call lasting LongCall outlasts many times over.
    private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan LongCall = TimeSpan.FromSeconds(5);

    [Fact]
    public void RunOnEveryThread_CallReturnsValue_ReturnsValueOfEveryThread()
    {
        // act
        var testResult = ConcurrentRunner.RunOnEveryThread(ThreadCount, () => CallResult, Timeout);

        // assert
        testResult.Joined.Should().BeTrue();
        testResult.Exceptions.Should().HaveCount(ThreadCount).And.OnlyContain(exception => exception == null);
        testResult.Results.Should().Equal(CallResult, CallResult, CallResult, CallResult);
    }

    [Fact]
    public void RunOnEveryThread_CallThrows_ReturnsExceptionOfEveryThread()
    {
        // act
        var testResult = ConcurrentRunner.RunOnEveryThread<int>(ThreadCount, () => throw new InvalidOperationException(), Timeout);

        // assert
        testResult.Joined.Should().BeTrue();
        testResult.Exceptions.Should().HaveCount(ThreadCount).And.AllBeOfType<InvalidOperationException>();
        testResult.Results.Should().Equal(0, 0, 0, 0);
    }

    [Fact]
    public void RunOnEveryThread_CallsWaitForEachOther_ReleasesThreadsTogether()
    {
        // arrange
        // Every call returns true only if all the threads reach the rendezvous before the timeout, which they cannot do one at a time.
        using var testRendezvous = new Barrier(ThreadCount);

        // act
        var testResult = ConcurrentRunner.RunOnEveryThread(ThreadCount, () => testRendezvous.SignalAndWait(Timeout), Timeout);

        // assert
        testResult.Joined.Should().BeTrue();
        testResult.Exceptions.Should().OnlyContain(exception => exception == null);
        testResult.Results.Should().Equal(true, true, true, true);
    }

    [Fact]
    public void RunOnEveryThread_CallOutlastsTimeout_ReportsThreadsNotJoined()
    {
        // arrange
        // The event is never set, so every call waits for LongCall. It is not disposed, because the threads still wait on it.
        var testNeverSet = new ManualResetEventSlim();

        // act
        var testResult = ConcurrentRunner.RunOnEveryThread(ThreadCount, () => testNeverSet.Wait(LongCall), ShortTimeout);

        // assert
        testResult.Joined.Should().BeFalse();
    }

    [Fact]
    public void RunInOrders_DifferentOrders_ReturnsResultsInOrderOfCalls()
    {
        // arrange
        Func<string>[] testCalls = [() => FirstCall, () => SecondCall, () => ThirdCall];
        int[][] testOrders = [[0, 1, 2], [2, 1, 0], [1, 2, 0]];

        // act
        var testResult = ConcurrentRunner.RunInOrders(testCalls, testOrders, Timeout);

        // assert
        testResult.Joined.Should().BeTrue();
        testResult.Exceptions.Should().HaveCount(testOrders.Length).And.OnlyContain(exception => exception == null);
        testResult.Results.Should().AllSatisfy(results => results.Should().Equal(FirstCall, SecondCall, ThirdCall));
    }

    [Fact]
    public void RunInOrders_OneThread_MakesCallsInOrderGiven()
    {
        // arrange
        var testCallsMade = new List<string>();
        Func<int>[] testCalls =
        [
            () => { testCallsMade.Add(FirstCall); return 0; },
            () => { testCallsMade.Add(SecondCall); return 0; },
            () => { testCallsMade.Add(ThirdCall); return 0; },
        ];
        int[][] testOrders = [[2, 0, 1]];

        // act
        var testResult = ConcurrentRunner.RunInOrders(testCalls, testOrders, Timeout);

        // assert
        testResult.Joined.Should().BeTrue();
        testCallsMade.Should().Equal(ThirdCall, FirstCall, SecondCall);
    }
}
