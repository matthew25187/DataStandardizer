using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// Differential test of the parser that validates language tags against a Subtag Registry against the regular expression it replaced (#223).
    /// </summary>
    /// <remarks>
    /// For every input in <see cref="ParityCorpus"/>, the registry-based overloads must give the same result as <see cref="FrozenRegistryBasedExpression"/>
    /// and decompose the tag into the same subtags, against the IANA registry and against a trimmed registry that has no grandfathered or variant
    /// records. The one intended difference is #253, which <see cref="RegistryBasedParityCases"/> describes.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Collection(Bcp47LanguageTagParityCollection.Name)]
    public class Bcp47LanguageTagRegistryParityTests
    {
        [Fact]
        public void TryCreate_WithIanaSubtagRegistryOverCorpus_FindsTheSameLanguageTagsAsFrozenRegularExpression()
        {
            // arrange
            var cases = RegistryBasedParityCases.Iana;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases.Validity, input => ParserObserver.ObserveValidity(input, cases.Registry));

            // assert
            testResult.Should().BeEmpty("TryCreate and the parser must agree with the regular expression they replaced");
        }

        [Fact]
        public void Parser_WithIanaSubtagRegistryOverCorpus_DecomposesLikeFrozenRegularExpression()
        {
            // arrange
            var cases = RegistryBasedParityCases.Iana;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases.Decomposition, input => ParserObserver.ObserveDecomposition(input, cases.Registry));

            // assert
            testResult.Should().BeEmpty("the parser decomposes any input with the syntax of a language tag into the subtags the regular expression matched");
        }

        [Fact]
        public void SubtagProperties_WithIanaSubtagRegistryOverValidCorpus_AreTheSubtagsFrozenRegularExpressionMatched()
        {
            // arrange
            var cases = RegistryBasedParityCases.Iana;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases.Properties, input => ParserObserver.ObserveProperties(input, cases.Registry));

            // assert
            testResult.Should().BeEmpty("the subtag properties of a language tag are the subtags the regular expression matched in it");
        }

        [Fact]
        public void TryCreate_WithTrimmedSubtagRegistryOverCorpus_FindsTheSameLanguageTagsAsFrozenRegularExpression()
        {
            // arrange
            var cases = RegistryBasedParityCases.Trimmed;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases.Validity, input => ParserObserver.ObserveValidity(input, cases.Registry));

            // assert
            testResult.Should().BeEmpty("TryCreate and the parser must agree with the regular expression they replaced, except for an empty subtag (#253)");
        }

        [Fact]
        public void Parser_WithTrimmedSubtagRegistryOverCorpus_DecomposesLikeFrozenRegularExpression()
        {
            // arrange
            var cases = RegistryBasedParityCases.Trimmed;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases.Decomposition, input => ParserObserver.ObserveDecomposition(input, cases.Registry));

            // assert
            testResult.Should().BeEmpty("the parser decomposes any input with the syntax of a language tag into the subtags the regular expression matched");
        }

        [Fact]
        public void SubtagProperties_WithTrimmedSubtagRegistryOverValidCorpus_AreTheSubtagsFrozenRegularExpressionMatched()
        {
            // arrange
            var cases = RegistryBasedParityCases.Trimmed;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases.Properties, input => ParserObserver.ObserveProperties(input, cases.Registry));

            // assert
            testResult.Should().BeEmpty("the subtag properties of a language tag are the subtags the regular expression matched in it");
        }

        [Fact]
        public void Match_WithTrimmedSubtagRegistryAndEmptyInput_MatchesThroughTheEmptyAlternation()
        {
            // arrange
            var frozen = RegistryBasedParityCases.Trimmed.Frozen;

            // act
            var testResult = frozen.Match("");

            // assert
            testResult.Success.Should().BeTrue("the frozen expression has an empty alternation for a registry with no grandfathered records, so the corpus covers #253");
        }

        [Fact]
        public void Match_WithIanaSubtagRegistryAndEmptyInput_DoesNotMatch()
        {
            // arrange
            var frozen = RegistryBasedParityCases.Iana.Frozen;

            // act
            var testResult = frozen.Match("");

            // assert
            testResult.Success.Should().BeFalse("the frozen expression has no empty alternation for a registry with records of every type");
        }

        [Fact]
        public void CreateTrimmedSubtagRegistry_FromFixture_HasNoGrandfatheredOrVariantRecords()
        {
            // act
            var testResult = RegistryBasedParityCases.CreateTrimmedSubtagRegistry();

            // assert
            testResult.GrandfatheredTags.Should().BeEmpty();
            testResult.Variants.Should().BeEmpty();
        }
    }
}
