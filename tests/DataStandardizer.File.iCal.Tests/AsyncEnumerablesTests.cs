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
        public void Completed_MoveNextAsyncWithAnItem_CompletesBeforeItReturns()
        {
            // arrange
            var enumerator = AsyncEnumerables.Completed(new[] { Item }).GetAsyncEnumerator();

            // act
            var testResult = enumerator.MoveNextAsync();

            // assert
            testResult.IsCompleted.Should().BeTrue();
            testResult.Result.Should().BeTrue();
            enumerator.Current.Should().Be(Item);
        }

        [Fact]
        public void Completed_MoveNextAsyncAtTheEnd_CompletesWithFalse()
        {
            // arrange
            var enumerator = AsyncEnumerables.Completed(new int[0]).GetAsyncEnumerator();

            // act
            var testResult = enumerator.MoveNextAsync();

            // assert
            testResult.IsCompleted.Should().BeTrue();
            testResult.Result.Should().BeFalse();
        }

        [Fact]
        public async Task Yielding_MoveNextAsyncWithAnItem_GivesTheItem()
        {
            // arrange
            var enumerator = AsyncEnumerables.Yielding(new[] { Item }).GetAsyncEnumerator();

            // act
            var testResult = await enumerator.MoveNextAsync();

            // assert
            testResult.Should().BeTrue();
            enumerator.Current.Should().Be(Item);
        }
    }
}
