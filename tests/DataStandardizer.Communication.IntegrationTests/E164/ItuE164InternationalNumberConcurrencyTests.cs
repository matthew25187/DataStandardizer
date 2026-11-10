using System;
using DataStandardizer.Communication.E164;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests.E164
{
    /// <summary>
    /// Regression tests covering concurrent access to the parse expression cache shared by every
    /// ITU E.164 number structure type. Before the cache was synchronized, concurrent parsing could
    /// throw from the unguarded <c>Dictionary.Add</c>, return a torn value, or spin forever on a
    /// corrupted bucket chain.
    /// </summary>
    /// <remarks>
    /// What these exercise depends on how the thread scheduler interleaves the calls, so they are integration tests. Each one empties the
    /// cache first, so they run one at a time, as the tests of a class do, and no other test in this project parses a number.
    /// </remarks>
    public class ItuE164InternationalNumberConcurrencyTests
    {
        private const int ParallelCallCount = 32;

        /// <summary>
        /// A number that no structure type accepts, so that parsing it walks all five structures and
        /// populates all five parse expression cache entries in a single call.
        /// </summary>
        private const string UnparsableNumber = "+0 999 9999";

        private const string ValidNumberForGeographicArea = "+64 411 5550100";
        private const ulong ValidNumberForGeographicAreaValue = 644115550100L;

        // The styles of Parse_CalledConcurrentlyWithColdParseExpressionCacheAcrossDistinctStyles_CachesOneExpressionPerStyle, each parsed by
        // CallsPerStyle threads at once.
        private static readonly ItuE164InternationalNumberStyles[] DistinctStyles =
        {
            ItuE164InternationalNumberStyles.None,
            ItuE164InternationalNumberStyles.AllowInternationalPrefixSymbol,
            ItuE164InternationalNumberStyles.AllowLeadingWhite,
            ItuE164InternationalNumberStyles.AllowTrailingWhite,
            ItuE164InternationalNumberStyles.AllowLeadingWhite | ItuE164InternationalNumberStyles.AllowTrailingWhite,
            ItuE164InternationalNumberStyles.Any,
        };

        private const int CallsPerStyle = 5;

        // One parse expression for each of the six DistinctStyles and each of the five number structure types: geographic areas, global
        // services, networks, groups of countries and trials.
        private const int ExpectedParseExpressionCount = 30;

        private static readonly TimeSpan ParallelCallTimeout = TimeSpan.FromSeconds(30);

        [Fact]
        public void Parse_CalledConcurrentlyWithColdParseExpressionCache_ReturnsTheSameNumberOnEveryThread()
        {
            // arrange
            ItuE164InternationalNumberStructureBase.ClearParseExpressions();

            // act
            var testResult = ConcurrentRunner.Run(new[] { ValidNumberForGeographicArea }, ParallelCallCount,
                number => ItuE164InternationalNumber.Parse(number, ItuE164InternationalNumberStyles.Any).Number, ParallelCallTimeout);

            // assert
            testResult.Completed.Should().BeTrue("parsing should not deadlock or spin on the shared parse expression cache");
            testResult.Exceptions.Should().BeEmpty();
            testResult.Results.Should().HaveCount(ParallelCallCount).And.OnlyContain(number => number == ValidNumberForGeographicAreaValue);
        }

        [Fact]
        public void TryParse_CalledConcurrentlyWithColdParseExpressionCacheAndAnUnparsableNumber_ReturnsFalseWithoutThrowing()
        {
            // arrange
            ItuE164InternationalNumberStructureBase.ClearParseExpressions();

            // act
            var testResult = ConcurrentRunner.Run(new[] { UnparsableNumber }, ParallelCallCount,
                number => ItuE164InternationalNumber.TryParse(number, ItuE164InternationalNumberStyles.Any, out _), ParallelCallTimeout);

            // assert
            testResult.Completed.Should().BeTrue("parsing should not deadlock or spin on the shared parse expression cache");
            testResult.Exceptions.Should().BeEmpty();
            testResult.Results.Should().HaveCount(ParallelCallCount).And.OnlyContain(parsed => !parsed);
        }

        [Fact]
        public void Parse_CalledConcurrentlyWithColdParseExpressionCacheAcrossDistinctStyles_CachesOneExpressionPerStyle()
        {
            // arrange
            ItuE164InternationalNumberStructureBase.ClearParseExpressions();

            // act
            var testResult = ConcurrentRunner.Run(DistinctStyles, CallsPerStyle,
                style => ItuE164InternationalNumber.TryParse(UnparsableNumber, style, out _), ParallelCallTimeout);

            // assert
            testResult.Completed.Should().BeTrue("parsing should not deadlock or spin on the shared parse expression cache");
            testResult.Exceptions.Should().BeEmpty();
            ItuE164InternationalNumberStructureBase.ParseExpressionCount.Should().Be(ExpectedParseExpressionCount,
                "each style should cache exactly one parse expression per structure type");
        }
    }
}
