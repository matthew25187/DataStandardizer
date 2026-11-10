using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// Runs the parity tests on their own, after the tests that run in parallel.
    /// </summary>
    /// <remarks>
    /// The comparison uses every core. Run alongside other tests, it slowed down tests that depend on time, such as those with a short
    /// match timeout, enough to make them fail on a build agent.
    /// </remarks>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public class Bcp47LanguageTagParityCollection
    {
        internal const string Name = nameof(Bcp47LanguageTagParityCollection);
    }

    /// <summary>
    /// Differential test of the ISO code lists path (<see cref="LanguageTagConformance.IsoCodeLists"/>) against the regular expressions the parser replaced (#221).
    /// </summary>
    /// <remarks>
    /// For every input in <see cref="ParityCorpus"/>, the parser must give the same result as <see cref="FrozenRulesBasedExpressions"/> for each
    /// position of a tag and for the whole tag, and decompose it into the same subtags, including tags that repeat a subtag and so are not
    /// well-formed. <see cref="RulesBasedParityCases"/> works out what the frozen expressions give, and <see cref="ParserObserver"/> what the
    /// parser gives. The corpus covers every code list, the IANA registry, combinations and random strings, so it is long, and these are
    /// integration tests.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Collection(Bcp47LanguageTagParityCollection.Name)]
    public class Bcp47LanguageTagParityTests
    {
        [Fact]
        public void IsoCodeListClassifier_OverCorpus_AgreesWithFrozenRegularExpressions()
        {
            // arrange
            var cases = RulesBasedParityCases.Shared.SubtagChecks;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveIsoCodeListClassifier);

            // assert
            testResult.Should().BeEmpty("the classifier must agree at each position with the regular expression it replaced");
        }

        [Fact]
        public void Parser_OverCorpus_FindsTheSameLanguageTagsAsFrozenRegularExpressions()
        {
            // arrange
            var cases = RulesBasedParityCases.Shared.Validity;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveValidity);

            // assert
            testResult.Should().BeEmpty("IsValid, TryCreate and the parser must agree with the regular expressions they replaced");
        }

        [Fact]
        public void Parser_OverCorpus_DecomposesLikeFrozenRegularExpressions()
        {
            // arrange
            var cases = RulesBasedParityCases.Shared.Decomposition;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveDecomposition);

            // assert
            testResult.Should().BeEmpty("the parser decomposes any input with the syntax of a language tag, including one that repeats a subtag, into the subtags the regular expressions matched");
        }

        [Fact]
        public void SubtagProperties_OverWellFormedCorpus_AreTheSubtagsFrozenRegularExpressionsMatched()
        {
            // arrange
            var cases = RulesBasedParityCases.Shared.Properties;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveProperties);

            // assert
            testResult.Should().BeEmpty("the subtag properties of a language tag are the subtags the regular expressions matched in it");
        }

        [Fact]
        public void Inputs_OfCorpus_IncludeEveryRfc5646AppendixAExample()
        {
            // act
            var testResult = ParityCorpus.Inputs;

            // assert
            testResult.Should().Contain(ParityCorpus.RfcAppendixAExamples);
        }

        /// <summary>
        /// An input from each part of the corpus: the empty string, which only the trimmed registry's empty alternation matches (#253); an
        /// ISO 639-3 code; a subtag in another position; a combination of every kind of subtag; a tag with a line feed; and tags with the
        /// Kelvin sign U+212A in place of k and K, which are not ASCII letters (RFC 5646 §2.1).
        /// </summary>
        [Theory]
        [InlineData(""), InlineData("yue"), InlineData("en-Qaby"), InlineData("zh-cmn-YUE-Latn-US-rozaj"), InlineData("en-US\n"), InlineData("en-x-\x212A"), InlineData("\x212Aore")]
        public void Inputs_OfCorpus_IncludeAnInputFromEachPart(string expectedInput)
        {
            // act
            var testResult = ParityCorpus.Inputs;

            // assert
            testResult.Should().Contain(expectedInput);
        }
    }
}
