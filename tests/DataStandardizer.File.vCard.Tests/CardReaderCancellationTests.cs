using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests that the asynchronous reads of <see cref="VcardReader"/>, <see cref="JcardReader"/> and <see cref="XcardReader"/> are canceled only
    /// when the caller's token is canceled, whatever token an <see cref="OperationCanceledException"/> from the source carries.
    /// </summary>
    public class CardReaderCancellationTests
    {
        // Each way a source can report a failure.
        private static readonly FailureTiming[] FailureTimings =
        {
            FailureTiming.FromCall, FailureTiming.AsFaultedTask, FailureTiming.AsCanceledTask, FailureTiming.AfterYielding
        };


        // Each card reader, created over a source that fails with the given exception, at the given time, after running the given callback.
        // Each gives the asynchronous read, which returns a task, for that reader.
        private static readonly (string Name, FailureTiming[] Timings, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> Create)[] AsyncReads =
        {
            ("VcardReader", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new VcardReader(new FailingTextReader(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardAsync(token).AsTask();
            }),
            ("JcardReader", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new JcardReader(new FailingReadStream(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardAsync(token).AsTask();
            }),
            ("XcardReader over a text reader", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new XcardReader(new FailingTextReader(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardAsync(token).AsTask();
            }),
            ("XcardReader over a stream", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new XcardReader(new FailingReadStream(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardAsync(token).AsTask();
            }),
            ("XcardReader over a stream, with an encoding", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new XcardReader(new FailingReadStream(createException, timing) { OnFailure = onFailure }, new VcardOptions { MaxLineLength = null, Encoding = Encoding.UTF8 });
                return token => reader.ReadCardAsync(token).AsTask();
            }),
        };

        // Each card reader, as above, giving the first MoveNextAsync of its ReadCardsAsync.
        private static readonly (string Name, FailureTiming[] Timings, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> Create)[] AsyncEnumerations =
        {
            ("VcardReader", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new VcardReader(new FailingTextReader(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardsAsync(token).GetAsyncEnumerator().MoveNextAsync().AsTask();
            }),
            ("JcardReader", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new JcardReader(new FailingReadStream(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardsAsync(token).GetAsyncEnumerator().MoveNextAsync().AsTask();
            }),
            ("XcardReader over a text reader", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new XcardReader(new FailingTextReader(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardsAsync(token).GetAsyncEnumerator().MoveNextAsync().AsTask();
            }),
            ("XcardReader over a stream", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new XcardReader(new FailingReadStream(createException, timing) { OnFailure = onFailure });
                return token => reader.ReadCardsAsync(token).GetAsyncEnumerator().MoveNextAsync().AsTask();
            }),
            ("XcardReader over a stream, with an encoding", FailureTimings, (createException, timing, onFailure) =>
            {
                var reader = new XcardReader(new FailingReadStream(createException, timing) { OnFailure = onFailure }, new VcardOptions { MaxLineLength = null, Encoding = Encoding.UTF8 });
                return token => reader.ReadCardsAsync(token).GetAsyncEnumerator().MoveNextAsync().AsTask();
            }),
        };

        // Each OperationCanceledException the source can fail with while the caller's token isn't canceled.
        private static readonly (string Name, Func<Exception> Create)[] SourceCancellations =
        {
            ("no token", () => new OperationCanceledException()),
            ("another, canceled token", () => new OperationCanceledException(new CancellationToken(true))),
        };

        // Each read, with each way the source can report a failure, each OperationCanceledException it can fail with while the caller's token
        // isn't canceled, and the exception the task then faults with.  A canceled task carries no exception of its own, so awaiting it gives a
        // TaskCanceledException, whatever exception the source was given.
        public static TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming, Func<Exception>, Type> ReadsWithSourceCancellationFaults()
        {
            var data = new TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming, Func<Exception>, Type>();
            foreach (var (name, timings, create) in AsyncReads)
            {
                foreach (var timing in timings)
                {
                    foreach (var (exceptionName, createException) in SourceCancellations)
                    {
                        var expectedType = timing == FailureTiming.AsCanceledTask ? typeof(TaskCanceledException) : typeof(OperationCanceledException);
                        data.Add(name + ", " + timing + ", " + exceptionName, create, timing, createException, expectedType);
                    }
                }
            }

            return data;
        }

        // Each read, with each way the source can report a failure.
        public static TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming> ReadsWithSourceFailures()
        {
            return WithTimings(AsyncReads);
        }

        // Each enumeration, with each way the source can report a failure.
        public static TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming> EnumerationsWithSourceFailures()
        {
            return WithTimings(AsyncEnumerations);
        }

        [Theory]
        [MemberData(nameof(ReadsWithSourceCancellationFaults), DisableDiscoveryEnumeration = true)]
        public async Task ReadCardAsync_SourceFailsWithOperationCanceledWhileTokenIsNotCanceled_FaultsTask(string scenario,
            Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> create, FailureTiming timing, Func<Exception> createException,
            Type expectedExceptionType)
        {
            // arrange
            _ = scenario;
            var read = create(createException, timing, null);

            // act: wait for the read to finish without throwing, so that its status can be checked.
            var task = read(CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType(expectedExceptionType);
        }

        [Theory]
        [MemberData(nameof(ReadsWithSourceFailures), DisableDiscoveryEnumeration = true)]
        public async Task ReadCardAsync_TokenCanceledAndSourceFailsWithCancellationOfAnotherToken_CancelsTask(string scenario,
            Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> create, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var read = create(() => new OperationCanceledException(new CancellationToken(true)), timing, cancellationTokenSource.Cancel);

                // act: wait for the read to finish without throwing, so that its status can be checked.
                var task = read(cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsCanceled.Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(EnumerationsWithSourceFailures), DisableDiscoveryEnumeration = true)]
        public async Task ReadCardsAsync_SourceFailsWithOperationCanceledWhileTokenIsNotCanceled_CancelsMoveNextAsyncOnEveryTarget(string scenario,
            Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> create, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            var moveNext = create(() => new OperationCanceledException(), timing, null);

            // act: wait for MoveNextAsync to finish without throwing, so that its status can be checked.
            var task = moveNext(CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsCanceled.Should().BeTrue();
        }

        private static TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming> WithTimings(
            (string Name, FailureTiming[] Timings, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> Create)[] operations)
        {
            var data = new TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming>();
            foreach (var (name, timings, create) in operations)
            {
                foreach (var timing in timings)
                {
                    data.Add(name + ", " + timing, create, timing);
                }
            }

            return data;
        }
    }
}
