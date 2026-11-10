using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    public class DifferentialRunnerTests
    {
        [Fact]
        public void FindMismatches_WhenEveryObservationIsExpected_ReturnsNoMismatches()
        {
            // arrange
            var cases = new[] { new DifferentialCase<int>("a", 1), new DifferentialCase<int>("bb", 2) };
            Func<string, int> observe = input => input.Length;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, observe);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void FindMismatches_WhenObservationsDiffer_ReturnsEachMismatchInCorpusOrder()
        {
            // arrange
            var cases = new[] { new DifferentialCase<int>("a", 2), new DifferentialCase<int>("bb", 2), new DifferentialCase<int>("ccc", 4) };
            Func<string, int> observe = input => input.Length;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, observe);

            // assert
            testResult.Should().BeEquivalentTo(
                new[] { new { Input = "\"a\"", Expected = 2, Actual = 1 }, new { Input = "\"ccc\"", Expected = 4, Actual = 3 } },
                options => options.WithStrictOrdering());
        }

        [Fact]
        public void FindMismatches_WhenInputHasControlOrNonAsciiCharacters_DescribesThemAsEscapes()
        {
            // arrange
            var cases = new[] { new DifferentialCase<int>("en\n\x212A", 0) };
            Func<string, int> observe = input => input.Length;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, observe);

            // assert
            testResult.Should().ContainSingle().Which.Input.Should().Be("\"en\\u000A\\u212A\"");
        }
    }
}
