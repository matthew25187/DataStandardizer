using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using DataStandardizer.Communication.E164;
using FluentAssertions;

namespace DataStandardizer.Communication.Tests.E164
{
    /// <summary>
    /// Regression tests covering concurrent access to the parse expression cache shared by every
    /// ITU E.164 number structure type. Before the cache was synchronized, concurrent parsing could
    /// throw from the unguarded <see cref="Dictionary{TKey,TValue}.Add"/>, return a torn value, or
    /// spin forever on a corrupted bucket chain.
    /// </summary>
    public class ItuE164InternationalNumberConcurrencyTests
    {
        private const int ParallelCallCount = 32;

        /// <summary>
        /// A number that no structure type accepts, so that parsing it walks all five structures and
        /// populates all five parse expression cache entries in a single call.
        /// </summary>
        private const string UnparsableNumber = "+0 999 9999";

        private const string ValidNumberForGeographicArea = "+64 411 5550100";

        private static readonly TimeSpan ParallelCallTimeout = TimeSpan.FromSeconds(30);

        [Fact]
        public void Parse_CalledConcurrentlyWithColdParseExpressionCache_ReturnsTheSameNumberOnEveryThread()
        {
            // arrange
            var expectedResult = ItuE164InternationalNumber.Parse(ValidNumberForGeographicArea, ItuE164InternationalNumberStyles.Any).Number;
            ResetParseExpressionCache();
            var testResults = new ulong[ParallelCallCount];

            // act
            var testExceptions = RunInParallel(index => testResults[index] = ItuE164InternationalNumber.Parse(ValidNumberForGeographicArea, ItuE164InternationalNumberStyles.Any).Number);

            // assert
            testExceptions.Should().BeEmpty();
            testResults.Should().AllBeEquivalentTo(expectedResult);
        }

        [Fact]
        public void TryParse_CalledConcurrentlyWithColdParseExpressionCacheAndAnUnparsableNumber_ReturnsFalseWithoutThrowing()
        {
            // arrange
            ItuE164InternationalNumber.TryParse(UnparsableNumber, ItuE164InternationalNumberStyles.Any, out _)
                .Should().BeFalse("the test relies on a number that exercises every structure type's parse expression");
            ResetParseExpressionCache();
            var testResults = new bool[ParallelCallCount];

            // act
            var testExceptions = RunInParallel(index => testResults[index] = ItuE164InternationalNumber.TryParse(UnparsableNumber, ItuE164InternationalNumberStyles.Any, out _));

            // assert
            testExceptions.Should().BeEmpty();
            testResults.Should().AllBeEquivalentTo(false);
        }

        [Fact]
        public void Parse_CalledConcurrentlyWithColdParseExpressionCacheAcrossDistinctStyles_CachesOneExpressionPerStyle()
        {
            // arrange
            var testStyles = new[]
            {
                ItuE164InternationalNumberStyles.None,
                ItuE164InternationalNumberStyles.AllowInternationalPrefixSymbol,
                ItuE164InternationalNumberStyles.AllowLeadingWhite,
                ItuE164InternationalNumberStyles.AllowTrailingWhite,
                ItuE164InternationalNumberStyles.AllowLeadingWhite | ItuE164InternationalNumberStyles.AllowTrailingWhite,
                ItuE164InternationalNumberStyles.Any
            };
            ResetParseExpressionCache();

            // act
            var testExceptions = RunInParallel(index => ItuE164InternationalNumber.TryParse(UnparsableNumber, testStyles[index % testStyles.Length], out _));

            // assert
            testExceptions.Should().BeEmpty();
            GetParseExpressionCache().Count.Should().Be(testStyles.Length * NumberStructureTypeCount, "each style should cache exactly one parse expression per structure type");
        }

        #region Private Methods

        /// <summary>
        /// Runs <paramref name="parseAction"/> on <see cref="ParallelCallCount"/> dedicated threads that
        /// are released simultaneously, so that they contend for the cold parse expression cache.
        /// </summary>
        private static IReadOnlyCollection<Exception> RunInParallel(Action<int> parseAction)
        {
            var callBarrier = new Barrier(ParallelCallCount);
            var callExceptions = new ConcurrentQueue<Exception>();
            var callTasks = Enumerable.Range(0, ParallelCallCount)
                .Select(index => Task.Factory.StartNew(
                    () =>
                    {
                        callBarrier.SignalAndWait();
                        try
                        {
                            parseAction(index);
                        }
                        catch (Exception exception)
                        {
                            callExceptions.Enqueue(exception);
                        }
                    },
                    TaskCreationOptions.LongRunning))
                .ToArray();

            // A corrupted cache can leave a reader spinning on a cyclic bucket chain, so fail rather than hang.
            Task.WaitAll(callTasks, ParallelCallTimeout).Should().BeTrue("parsing should not deadlock or spin on the shared parse expression cache");

            return callExceptions;
        }

        private static int NumberStructureTypeCount => typeof(ItuE164InternationalNumberStructureBase).Assembly.GetTypes()
            .Count(type => !type.GetTypeInfo().IsAbstract && typeof(ItuE164InternationalNumberStructureBase).IsAssignableFrom(type));

        private static IDictionary GetParseExpressionCache()
        {
            var cacheField = typeof(ItuE164InternationalNumberStructureBase).GetField("ParseExpressions", BindingFlags.NonPublic | BindingFlags.Static);
            cacheField.Should().NotBeNull("the parse expression cache is accessed by name");
            return (IDictionary)cacheField!.GetValue(null)!;
        }

        private static void ResetParseExpressionCache()
        {
            var lockField = typeof(ItuE164InternationalNumberStructureBase).GetField("ParseExpressionsLock", BindingFlags.NonPublic | BindingFlags.Static);
            lockField.Should().NotBeNull("the parse expression cache lock is accessed by name");
            lock (lockField!.GetValue(null)!)
            {
                GetParseExpressionCache().Clear();
            }
        }

        #endregion
    }
}
