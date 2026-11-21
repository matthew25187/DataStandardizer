using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the asynchronous sequences that <c>AsyncEnumerables</c> gives the tests.
    /// </summary>
    public class AsyncEnumerablesTests
    {
        private const int Item = 7;

        [Fact]
        public async Task Yielding_MoveNextAsyncWithAnItem_GivesTheItem()
        {
            // arrange
            var enumerator = AsyncEnumerables.Yielding(new[] { Item }).GetAsyncEnumerator();

            // act
            var testResult = await enumerator.MoveNextAsync();
            var current = enumerator.Current;

            // assert
            testResult.Should().BeTrue();
            current.Should().Be(Item);
        }

        [Fact]
        public async Task Yielding_MoveNextAsyncAtTheEnd_GivesFalse()
        {
            // arrange
            var enumerator = AsyncEnumerables.Yielding(new int[0]).GetAsyncEnumerator();

            // act
            var testResult = await enumerator.MoveNextAsync();

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public async Task Yielding_MoveNextAsyncWhoseItemsThrow_FaultsTheStep()
        {
            // arrange
            var enumerator = AsyncEnumerables.Yielding(ThrowingItems()).GetAsyncEnumerator();

            // act: wait for the step to finish without throwing, so that its status can be checked.
            var step = enumerator.MoveNextAsync().AsTask();
            await Task.WhenAny(step);

            // assert
            step.Status.Should().Be(TaskStatus.Faulted);
            step.Exception.InnerException.Should().BeOfType<InvalidOperationException>();
        }

        [Fact]
        public void Yielding_MoveNextAsyncDuringDeferredRun_IsNotCompletedWhenTheOperationReturns()
        {
            // arrange
            var enumerator = AsyncEnumerables.Yielding(new[] { Item }).GetAsyncEnumerator();
            var completedWhenReturned = true;
            Func<Task> operation = () =>
            {
                var step = enumerator.MoveNextAsync();
                completedWhenReturned = step.IsCompleted;
                return step.AsTask();
            };

            // act
            DeferredFailures.Run(operation);

            // assert
            completedWhenReturned.Should().BeFalse();
        }

        [Fact]
        public async Task Yielding_MoveNextAsyncDuringDeferredRun_GivesTheItemBeforeRunReturns()
        {
            // arrange
            var enumerator = AsyncEnumerables.Yielding(new[] { Item }).GetAsyncEnumerator();
            Task<bool> step = null;
            Func<Task> operation = () => step = enumerator.MoveNextAsync().AsTask();

            // act
            _ = DeferredFailures.Run(operation);
            var statusWhenRunReturned = step.Status;
            var testResult = await step;
            var current = enumerator.Current;

            // assert
            statusWhenRunReturned.Should().Be(TaskStatus.RanToCompletion);
            testResult.Should().BeTrue();
            current.Should().Be(Item);
        }

        private static IEnumerable<int> ThrowingItems()
        {
            throw new InvalidOperationException();
#pragma warning disable CS0162 // Unreachable code: the yield makes this an iterator, so that it throws from MoveNext.
            yield break;
#pragma warning restore CS0162
        }
    }
}
