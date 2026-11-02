using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// Differential test of the well-formed path against a regular expression transcribed from the ABNF of RFC 5646 §2.1 (#503).
    /// </summary>
    /// <remarks>
    /// For every input in <see cref="ParityCorpus"/>, <see cref="Bcp47LanguageTag.IsWellFormed(string)"/>, <c>TryCreate</c> and the parser with the syntax classifier must give the same
    /// result as <see cref="Rfc5646AbnfExpression"/>, and decompose the tag into the same subtags.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Collection(Bcp47LanguageTagParityCollection.Name)]
    public class Bcp47LanguageTagWellFormedParityTests
    {
        [Fact]
        public void IsWellFormed_OverCorpus_AgreesWithAbnfExpression()
        {
            // arrange
            var cases = WellFormedParityCases.Shared.Validity;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveWellFormedness);

            // assert
            testResult.Should().BeEmpty("IsWellFormed, TryCreate and the parser must agree with the ABNF of RFC 5646 §2.1");
        }

        [Fact]
        public void Parser_OverCorpus_DecomposesLikeAbnfExpression()
        {
            // arrange
            var cases = WellFormedParityCases.Shared.Decomposition;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveWellFormedDecomposition);

            // assert
            testResult.Should().BeEmpty("the parser decomposes any input that matches the grammar into the subtags the grammar matched");
        }

        [Fact]
        public void SubtagProperties_OverWellFormedCorpus_AreTheSubtagsAbnfExpressionMatched()
        {
            // arrange
            var cases = WellFormedParityCases.Shared.Properties;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveWellFormedProperties);

            // assert
            testResult.Should().BeEmpty("the subtag properties of a well-formed language tag are the subtags the grammar matched in it");
        }

        /// <summary>
        /// Inputs the corpus must have for the comparison to cover the well-formed path: subtags that no list holds, repeated subtags, and the RFC 5646 §2.1 boundaries of each production.
        /// </summary>
        [Theory]
        [InlineData("zz"), InlineData("en-Zzzz"), InlineData("en-830"), InlineData("en-1901-1901"), InlineData("en-a-bb-A-cc"), InlineData("abcdefgh"), InlineData("abcdefghi"), InlineData("x-a")]
        public void Inputs_OfCorpus_IncludeWellFormedBoundary(string expectedInput)
        {
            // act
            var testResult = ParityCorpus.Inputs;

            // assert
            testResult.Should().Contain(expectedInput);
        }
    }
}
