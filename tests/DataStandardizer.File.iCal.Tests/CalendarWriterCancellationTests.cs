using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of when an asynchronous operation of <see cref="IcalWriter"/>, <see cref="JcalWriter"/> or <see cref="XcalWriter"/> is canceled: only
    /// when the caller's token is canceled, whatever token an <see cref="OperationCanceledException"/> carries.  Otherwise the exception faults
    /// the task, and the writer can continue.
    /// </summary>
    public class CalendarWriterCancellationTests
    {
        private const string CanceledMessage = "*canceled*";

        // The calendar of an operation whose progress callback failed, which was written before the callback was called, and the next one.
        private const long TwoCalendars = 2;

        // A value long enough that writing it reaches the target of each writer as the calendar is written, rather than waiting in a buffer: it
        // is longer than the 16 KiB buffer of the content-line writer's stream writer, and the XML writer's buffer.
        private const int ValueLongerThanEveryBuffer = 64 * 1024;

        /// <summary>
        /// A writer with an asynchronous operation on it.
        /// </summary>
        public sealed class WriterOperation
        {
            public WriterOperation(Func<CancellationToken, Task> run, Func<Task> writeCalendarAsync, Action writeCalendar, Func<long> calendarsWritten)
            {
                Run = run;
                WriteCalendarAsync = writeCalendarAsync;
                WriteCalendar = writeCalendar;
                CalendarsWritten = calendarsWritten;
            }

            /// <summary>
            /// Gets the operation, given the caller's token.
            /// </summary>
            public Func<CancellationToken, Task> Run { get; }

            /// <summary>
            /// Gets a function that writes another calendar to the same writer asynchronously.
            /// </summary>
            public Func<Task> WriteCalendarAsync { get; }

            /// <summary>
            /// Gets an action that writes another calendar to the same writer synchronously.
            /// </summary>
            public Action WriteCalendar { get; }

            /// <summary>
            /// Gets a function that gives the writer's <c>CalendarsWritten</c>.
            /// </summary>
            public Func<long> CalendarsWritten { get; }
        }

        #region The target fails

        // Each way the target can report a failure.
        private static readonly FailureTiming[] FailureTimings =
        {
            FailureTiming.FromCall, FailureTiming.AsFaultedTask, FailureTiming.AsCanceledTask, FailureTiming.AfterYielding
        };

        // The way a text writer under XcalWriter's XML writer can report a failure of a write that the XML writer passes on, on every target
        // framework.  On .NET Framework, an XML writer whose text writer fails a write without waiting completes as though the write succeeded.
        private static readonly FailureTiming[] YieldingFailureTimings = { FailureTiming.AfterYielding };

        // Each asynchronous operation of each writer, over a target that fails with the given exception at the given time.  Each operation
        // reaches the target: IcalWriter writes each line through, XcalWriter's calendar is longer than its XML writer's buffer, and JcalWriter
        // writes a calendar only once it follows another, so a first calendar is written before the operation.  Each has the ways its target
        // can report a failure.
        private static readonly (string Name, FailureTiming[] Timings, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> Create)[] TargetOperations =
        {
            ("IcalWriter.WriteCalendarAsync", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new IcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.WriteCalendarAsync(CreateCalendar(), token).AsTask();
            }),
            ("IcalWriter.WriteCalendarsAsync", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new IcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.WriteCalendarsAsync(new[] { CreateCalendar() }, cancellationToken: token);
            }),
            ("IcalWriter.WriteCalendarsAsync from an asynchronous sequence", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new IcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(new[] { CreateCalendar() }), cancellationToken: token);
            }),
            ("IcalWriter.FlushAsync", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new IcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.FlushAsync(token);
            }),
            ("JcalWriter.WriteCalendarAsync", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new JcalWriter(new FailingStream(createException, timing) { OnFailure = onFailure });
                writer.WriteCalendar(CreateCalendar());
                return token => writer.WriteCalendarAsync(CreateCalendar(), token).AsTask();
            }),
            ("JcalWriter.WriteCalendarsAsync", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new JcalWriter(new FailingStream(createException, timing) { OnFailure = onFailure });
                writer.WriteCalendar(CreateCalendar());
                return token => writer.WriteCalendarsAsync(new[] { CreateCalendar() }, cancellationToken: token);
            }),
            ("JcalWriter.WriteCalendarsAsync from an asynchronous sequence", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new JcalWriter(new FailingStream(createException, timing) { OnFailure = onFailure });
                writer.WriteCalendar(CreateCalendar());
                return token => writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(new[] { CreateCalendar() }), cancellationToken: token);
            }),
            ("JcalWriter.FlushAsync", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new JcalWriter(new FailingStream(createException, timing) { OnFailure = onFailure });
                return token => writer.FlushAsync(token);
            }),
            ("XcalWriter.WriteCalendarAsync", YieldingFailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new XcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.WriteCalendarAsync(CreateLongCalendar(), token).AsTask();
            }),
            ("XcalWriter.WriteCalendarsAsync", YieldingFailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new XcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.WriteCalendarsAsync(new[] { CreateLongCalendar() }, cancellationToken: token);
            }),
            // The sequence doesn't yield, so that the writer reaches its target before the operation returns, as the target needs for its failure to
            // be held back until then.
            ("XcalWriter.WriteCalendarsAsync from an asynchronous sequence", YieldingFailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new XcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.WriteCalendarsAsync(AsyncEnumerables.Completed(new[] { CreateLongCalendar() }), cancellationToken: token);
            }),
            ("XcalWriter.FlushAsync", FailureTimings, (createException, timing, onFailure) =>
            {
                var writer = new XcalWriter(new FailingTextWriter(createException, timing) { OnFailure = onFailure });
                return token => writer.FlushAsync(token);
            }),
        };


        // The operation, with the failures of the target that yield held back until the operation has returned its task, so that the writer is
        // always given a task that has not completed.  Whether a yielding task has completed by then depends on the thread pool.
        private static Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> Deferring(
            Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> create)
        {
            return (createException, timing, onFailure) =>
            {
                var run = create(createException, timing, onFailure);
                return token => DeferredFailures.Run(() => run(token));
            };
        }

        // Each operation, with each way the target can report a failure, each OperationCanceledException it can fail with while the caller's
        // token isn't canceled, and the exception the task then faults with.  A canceled task carries no exception of its own, so awaiting it
        // gives a TaskCanceledException, whatever exception the target was given.
        public static TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming, Func<Exception>, Type> TargetCancellationFaults()
        {
            var exceptions = new (string, Func<Exception>)[]
            {
                ("no token", () => new OperationCanceledException()),
                ("another, canceled token", () => new OperationCanceledException(new CancellationToken(true))),
            };
            var data = new TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming, Func<Exception>, Type>();
            foreach (var (name, timings, create) in TargetOperations)
            {
                foreach (var timing in timings)
                {
                    foreach (var (exceptionName, createException) in exceptions)
                    {
                        var expectedType = timing == FailureTiming.AsCanceledTask ? typeof(TaskCanceledException) : typeof(OperationCanceledException);
                        data.Add(name + ", " + timing + ", " + exceptionName, Deferring(create), timing, createException, expectedType);
                    }
                }
            }

            return data;
        }

        // Each operation, with each way the target can report a failure.
        public static TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming> TargetFailures()
        {
            var data = new TheoryData<string, Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>>, FailureTiming>();
            foreach (var (name, timings, create) in TargetOperations)
            {
                foreach (var timing in timings)
                {
                    data.Add(name + ", " + timing, Deferring(create), timing);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(TargetCancellationFaults), DisableDiscoveryEnumeration = true)]
        public async Task AsyncOperation_TargetFailsWithOperationCanceledWhileTokenIsNotCanceled_FaultsTask(string scenario,
            Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> create, FailureTiming timing, Func<Exception> createException,
            Type expectedExceptionType)
        {
            // arrange
            _ = scenario;
            var run = create(createException, timing, null);

            // act: wait for the operation to finish without throwing, so that its status can be checked.
            var task = run(CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType(expectedExceptionType);
        }

        [Theory]
        [MemberData(nameof(TargetFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncOperation_TokenCanceledAndTargetFailsWithCancellationOfAnotherToken_CancelsTask(string scenario,
            Func<Func<Exception>, FailureTiming, Action, Func<CancellationToken, Task>> create, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var run = create(() => new OperationCanceledException(new CancellationToken(true)), timing, cancellationTokenSource.Cancel);

                // act: wait for the operation to finish without throwing, so that its status can be checked.
                var task = run(cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsCanceled.Should().BeTrue();
            }
        }

        #endregion

        #region The progress callback fails

        // Each writer's WriteCalendarsAsync overloads, over a target that takes every write, with a progress callback that runs the given action
        // and then throws the given exception.
        private static readonly (string Name, Func<Action, Func<Exception>, WriterOperation> Create)[] ProgressOperations =
        {
            ("IcalWriter.WriteCalendarsAsync", (onReport, createException) =>
            {
                var writer = new IcalWriter(new AsyncOnlyTextWriter());
                var progress = CreateFailingProgress(onReport, createException);
                return new WriterOperation(token => writer.WriteCalendarsAsync(new[] { CreateCalendar() }, progress, token),
                    () => writer.WriteCalendarAsync(CreateCalendar()).AsTask(), () => writer.WriteCalendar(CreateCalendar()), () => writer.CalendarsWritten);
            }),
            ("IcalWriter.WriteCalendarsAsync from an asynchronous sequence", (onReport, createException) =>
            {
                var writer = new IcalWriter(new AsyncOnlyTextWriter());
                var progress = CreateFailingProgress(onReport, createException);
                return new WriterOperation(token => writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(new[] { CreateCalendar() }), progress, token),
                    () => writer.WriteCalendarAsync(CreateCalendar()).AsTask(), () => writer.WriteCalendar(CreateCalendar()), () => writer.CalendarsWritten);
            }),
            ("JcalWriter.WriteCalendarsAsync", (onReport, createException) =>
            {
                var writer = new JcalWriter(new AsyncOnlyWriteStream());
                var progress = CreateFailingProgress(onReport, createException);
                return new WriterOperation(token => writer.WriteCalendarsAsync(new[] { CreateCalendar() }, progress, token),
                    () => writer.WriteCalendarAsync(CreateCalendar()).AsTask(), () => writer.WriteCalendar(CreateCalendar()), () => writer.CalendarsWritten);
            }),
            ("JcalWriter.WriteCalendarsAsync from an asynchronous sequence", (onReport, createException) =>
            {
                var writer = new JcalWriter(new AsyncOnlyWriteStream());
                var progress = CreateFailingProgress(onReport, createException);
                return new WriterOperation(token => writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(new[] { CreateCalendar() }), progress, token),
                    () => writer.WriteCalendarAsync(CreateCalendar()).AsTask(), () => writer.WriteCalendar(CreateCalendar()), () => writer.CalendarsWritten);
            }),
            ("XcalWriter.WriteCalendarsAsync", (onReport, createException) =>
            {
                var writer = new XcalWriter(new AsyncOnlyTextWriter());
                var progress = CreateFailingProgress(onReport, createException);
                return new WriterOperation(token => writer.WriteCalendarsAsync(new[] { CreateCalendar() }, progress, token),
                    () => writer.WriteCalendarAsync(CreateCalendar()).AsTask(), () => writer.WriteCalendar(CreateCalendar()), () => writer.CalendarsWritten);
            }),
            ("XcalWriter.WriteCalendarsAsync from an asynchronous sequence", (onReport, createException) =>
            {
                var writer = new XcalWriter(new AsyncOnlyTextWriter());
                var progress = CreateFailingProgress(onReport, createException);
                return new WriterOperation(token => writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(new[] { CreateCalendar() }), progress, token),
                    () => writer.WriteCalendarAsync(CreateCalendar()).AsTask(), () => writer.WriteCalendar(CreateCalendar()), () => writer.CalendarsWritten);
            }),
        };

        // Each operation whose progress callback fails.
        public static TheoryData<string, Func<Action, Func<Exception>, WriterOperation>> ProgressFailures()
        {
            var data = new TheoryData<string, Func<Action, Func<Exception>, WriterOperation>>();
            foreach (var (name, create) in ProgressOperations)
            {
                data.Add(name, create);
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task WriteCalendarsAsync_ProgressThrowsOperationCanceledWhileTokenIsNotCanceled_FaultsTask(string scenario,
            Func<Action, Func<Exception>, WriterOperation> create)
        {
            // arrange
            _ = scenario;
            var operation = create(null, () => new OperationCanceledException());

            // act: wait for the operation to finish without throwing, so that its status can be checked.
            var task = operation.Run(CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType<OperationCanceledException>();
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task WriteCalendarAsync_AfterOperationCanceledWhileTokenIsNotCanceled_WritesTheCalendar(string scenario,
            Func<Action, Func<Exception>, WriterOperation> create)
        {
            // arrange
            _ = scenario;
            var operation = create(null, () => new OperationCanceledException());
            await Record.ExceptionAsync(() => operation.Run(CancellationToken.None));

            // act
            await operation.WriteCalendarAsync();

            // assert
            operation.CalendarsWritten().Should().Be(TwoCalendars);
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task WriteCalendarsAsync_TokenCanceledAndProgressThrowsIOException_FaultsTask(string scenario,
            Func<Action, Func<Exception>, WriterOperation> create)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var operation = create(cancellationTokenSource.Cancel, () => new IOException());

                // act: wait for the operation to finish without throwing, so that its status can be checked.
                var task = operation.Run(cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsFaulted.Should().BeTrue();
                task.Exception.InnerException.Should().BeOfType<IOException>();
            }
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task WriteCalendarAsync_AfterTokenCanceledAndProgressThrowsIOException_WritesTheCalendar(string scenario,
            Func<Action, Func<Exception>, WriterOperation> create)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var operation = create(cancellationTokenSource.Cancel, () => new IOException());
                await Record.ExceptionAsync(() => operation.Run(cancellationTokenSource.Token));

                // act
                await operation.WriteCalendarAsync();

                // assert
                operation.CalendarsWritten().Should().Be(TwoCalendars);
            }
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task WriteCalendarsAsync_TokenCanceledAndProgressThrowsCancellationOfNoToken_CancelsTask(string scenario,
            Func<Action, Func<Exception>, WriterOperation> create)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var operation = create(cancellationTokenSource.Cancel, () => new OperationCanceledException());

                // act: wait for the operation to finish without throwing, so that its status can be checked.
                var task = operation.Run(cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsCanceled.Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(ProgressFailures), DisableDiscoveryEnumeration = true)]
        public async Task WriteCalendar_AfterTokenCanceledAndProgressThrowsCancellationOfNoToken_ThrowsInvalidOperation(string scenario,
            Func<Action, Func<Exception>, WriterOperation> create)
        {
            // arrange
            _ = scenario;
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var operation = create(cancellationTokenSource.Cancel, () => new OperationCanceledException());
                await Record.ExceptionAsync(() => operation.Run(cancellationTokenSource.Token));

                // act
                Action testAction = operation.WriteCalendar;

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
            }
        }

        #endregion

        #region The release fails

        // Each asynchronous operation that releases a writer, over a stream whose flush throws an OperationCanceledException when the writer is
        // released.  Each but DisposeAsync disposes of the writer during the operation.
        public static TheoryData<string, Func<Func<Task>>> OperationsWhoseReleaseFailsWithOperationCanceled => new TheoryData<string, Func<Func<Task>>>
        {
            { "IcalWriter.DisposeAsync", () => CreateDisposeAsync(new IcalWriter(CreateFailingFlushStream())) },
            { "JcalWriter.DisposeAsync", () => CreateDisposeAsync(new JcalWriter(CreateFailingFlushStream())) },
            { "JcalWriter.WriteCalendarAsync", CreateJcalDisposalDuringWrite },
            { "JcalWriter.WriteCalendarsAsync", () => CreateDisposalDuringProgress(stream => new JcalWriter(stream), (writer, progress) => writer.WriteCalendarsAsync(new[] { CreateCalendar() }, progress)) },
            { "JcalWriter.FlushAsync", () => CreateDisposalDuringFlush(stream => new JcalWriter(stream), writer => writer.FlushAsync()) },
            { "XcalWriter.DisposeAsync", () => CreateDisposeAsync(new XcalWriter(CreateFailingFlushStream())) },
            { "XcalWriter.WriteCalendarsAsync", () => CreateDisposalDuringProgress(stream => new XcalWriter(stream), (writer, progress) => writer.WriteCalendarsAsync(new[] { CreateCalendar() }, progress)) },
            { "XcalWriter.FlushAsync", () => CreateDisposalDuringFlush(stream => new XcalWriter(stream), writer => writer.FlushAsync()) },
        };

        [Theory]
        [MemberData(nameof(OperationsWhoseReleaseFailsWithOperationCanceled), DisableDiscoveryEnumeration = true)]
        public async Task AsyncOperation_ReleaseFailsWithOperationCanceled_FaultsTask(string scenario, Func<Func<Task>> createOperation)
        {
            // arrange
            _ = scenario;
            var operation = createOperation();

            // act: wait for the operation to finish without throwing, so that its status can be checked.
            var task = operation();
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType<OperationCanceledException>();
        }

        #endregion

        private static IcalCalendar CreateCalendar()
        {
            return ModelTestData.ReadCalendar(RfcFixtures.Rfc5545Event);
        }

        // A calendar with a property whose value is longer than any writer's buffer.
        private static IcalCalendar CreateLongCalendar()
        {
            var calendar = CreateCalendar();
            calendar.Properties.Add(new IcalContentLine("X-LONG", new string('a', ValueLongerThanEveryBuffer)));
            return calendar;
        }

        // A progress callback that runs onReport, if it isn't null, then throws the exception that createException creates.
        private static IProgress<IcalWriteProgress> CreateFailingProgress(Action onReport, Func<Exception> createException)
        {
            return new RecordingProgress(_ =>
            {
                onReport?.Invoke();
                throw createException();
            });
        }

        // A stream whose writes succeed and whose flush throws an OperationCanceledException.
        private static AsyncOnlyWriteStream CreateFailingFlushStream()
        {
            return new AsyncOnlyWriteStream { CreateFlushException = () => new OperationCanceledException() };
        }

        // A JcalWriter over a stream that disposes of the writer as the second calendar is written, and whose flush, when the writer is then
        // released, throws.
        private static Func<Task> CreateJcalDisposalDuringWrite()
        {
            var stream = CreateFailingFlushStream();
            var writer = new JcalWriter(stream);
            writer.WriteCalendar(CreateCalendar());
            stream.OnWrite = writer.Dispose;
            return () => writer.WriteCalendarAsync(CreateCalendar()).AsTask();
        }

        // The DisposeAsync of a writer.
        private static Func<Task> CreateDisposeAsync(IAsyncDisposable writer)
        {
            return () => writer.DisposeAsync().AsTask();
        }

        // A writer over a stream whose flush throws, with a WriteCalendarsAsync whose progress callback disposes of the writer.
        private static Func<Task> CreateDisposalDuringProgress<TWriter>(Func<Stream, TWriter> createWriter,
            Func<TWriter, IProgress<IcalWriteProgress>, Task> writeCalendarsAsync)
            where TWriter : IDisposable
        {
            var writer = createWriter(CreateFailingFlushStream());
            var progress = new RecordingProgress(_ => writer.Dispose());
            return () => writeCalendarsAsync(writer, progress);
        }

        // A writer over a stream that disposes of the writer as it is flushed.  That flush succeeds, and the flush when the writer is then
        // released throws.
        private static Func<Task> CreateDisposalDuringFlush<TWriter>(Func<Stream, TWriter> createWriter, Func<TWriter, Task> flushAsync)
            where TWriter : IDisposable
        {
            var isFirstFlush = true;
            var stream = new AsyncOnlyWriteStream
            {
                // The XML writer may flush the stream more than once as it is released, so every flush after the first throws.
                CreateFlushException = () =>
                {
                    var exception = isFirstFlush ? null : new OperationCanceledException();
                    isFirstFlush = false;
                    return exception;
                },
            };
            var writer = createWriter(stream);
            stream.OnFlush = writer.Dispose;
            return () => flushAsync(writer);
        }
    }
}
