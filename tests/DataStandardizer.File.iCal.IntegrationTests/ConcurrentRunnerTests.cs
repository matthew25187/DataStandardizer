using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    public class ConcurrentRunnerTests
    {
        private const string FirstInput = "first";
        private const string SecondInput = "second";
        private const int TwoCallsPerInput = 2;
        private const int OneCallPerInput = 1;

        // Long enough for every thread to start on a busy machine.
        private static readonly TimeSpan GenerousTimeout = TimeSpan.FromSeconds(30);

        // Far shorter than BlockedCallDuration, so that a call that sleeps for it can't return in time.
        private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(100);
        private static readonly TimeSpan BlockedCallDuration = TimeSpan.FromSeconds(10);

        [Fact]
        public void Run_CallsThatReturn_ReturnsEachResultInTheOrderOfTheInputs()
        {
            // arrange
            var inputs = new[] { FirstInput, SecondInput };

            // act
            var testResult = ConcurrentRunner.Run(inputs, TwoCallsPerInput, input => input, GenerousTimeout);

            // assert
            testResult.Completed.Should().BeTrue();
            testResult.Exceptions.Should().BeEmpty();
            testResult.Results.Should().Equal(FirstInput, FirstInput, SecondInput, SecondInput);
        }

        [Fact]
        public void Run_CallThatThrows_ReturnsItsException()
        {
            // arrange
            var exception = new InvalidOperationException();

            // act
            var testResult = ConcurrentRunner.Run<string, string>(new[] { FirstInput }, OneCallPerInput, _ => throw exception, GenerousTimeout);

            // assert
            testResult.Completed.Should().BeTrue();
            testResult.Exceptions.Should().ContainSingle().Which.Should().BeSameAs(exception);
        }

        [Fact]
        public void Run_CallsThatWaitForEachOther_RunsThemAtTheSameTime()
        {
            // arrange
            // Each call returns true only once every call has signalled, which can't happen unless they all run at once.
            using (var allCallsStarted = new CountdownEvent(TwoCallsPerInput))
            {
                Func<string, bool> call = _ =>
                {
                    allCallsStarted.Signal();
                    return allCallsStarted.Wait(GenerousTimeout);
                };

                // act
                var testResult = ConcurrentRunner.Run(new[] { FirstInput }, TwoCallsPerInput, call, GenerousTimeout);

                // assert
                testResult.Completed.Should().BeTrue();
                testResult.Results.Should().Equal(true, true);
            }
        }

        [Fact]
        public void Run_CallThatDoesNotReturnInTime_ReturnsNotCompleted()
        {
            // arrange
            Func<string, bool> call = _ =>
            {
                Thread.Sleep(BlockedCallDuration);
                return true;
            };

            // act
            var testResult = ConcurrentRunner.Run(new[] { FirstInput }, OneCallPerInput, call, ShortTimeout);

            // assert
            testResult.Completed.Should().BeFalse();
        }
    }
}
