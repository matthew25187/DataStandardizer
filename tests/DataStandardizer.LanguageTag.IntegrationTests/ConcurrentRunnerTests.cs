using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    public class ConcurrentRunnerTests
    {
        private const int ThreadCount = 4;

        /// <summary>
        /// How long <see cref="BarrierProbe"/> waits for the other threads, so that a runner that ran the work one thread at a time fails the
        /// test rather than hanging it.
        /// </summary>
        private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(30);

        [Fact]
        public void Run_WithWorkThatReturns_ReturnsTheValueOfEveryThread()
        {
            // arrange
            Func<string> work = () => "done";

            // act
            var testResult = ConcurrentRunner.Run(ThreadCount, work);

            // assert
            testResult.Should().HaveCount(ThreadCount).And.OnlyContain(outcome => outcome.Value == "done" && outcome.Exception == null);
        }

        [Fact]
        public void Run_WithWorkThatThrows_ReturnsTheExceptionOfEveryThread()
        {
            // arrange
            Func<string> work = () => throw new InvalidOperationException();

            // act
            var testResult = ConcurrentRunner.Run(ThreadCount, work);

            // assert
            testResult.Should().HaveCount(ThreadCount).And.OnlyContain(outcome => outcome.Value == null && outcome.Exception is InvalidOperationException);
        }

        [Fact]
        public void Run_WithWorkOnEveryThread_RunsTheWorkOnEveryThreadAtOnce()
        {
            // arrange
            var probe = new BarrierProbe(ThreadCount, ProbeTimeout);

            // act
            var testResult = ConcurrentRunner.Run<int>(ThreadCount, probe.Work);

            // assert
            testResult.Should().OnlyContain(outcome => outcome.Value == ThreadCount, "every thread is in the work at the same time, so each sees every thread start it");
        }

        /// <summary>
        /// Work that counts the threads that have started it, then waits for all of them before it reads the count, so the count each thread
        /// reads is the number of threads that were in the work at the same time.
        /// </summary>
        private sealed class BarrierProbe
        {
            private readonly CountdownEvent _allStarted;
            private readonly TimeSpan _timeout;
            private int _startedCount;

            internal BarrierProbe(int threadCount, TimeSpan timeout)
            {
                _allStarted = new CountdownEvent(threadCount);
                _timeout = timeout;
            }

            internal int Work()
            {
                Interlocked.Increment(ref _startedCount);
                _allStarted.Signal();
                _allStarted.Wait(_timeout);
                return Volatile.Read(ref _startedCount);
            }
        }
    }
}
