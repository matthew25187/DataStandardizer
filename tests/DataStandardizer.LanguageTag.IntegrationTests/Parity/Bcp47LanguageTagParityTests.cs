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
    /// Differential test of the parser that validates language tags by the default BCP 47 rules against the regular expressions it replaced (#221).
    /// </summary>
    /// <remarks>
    /// For every input in <see cref="ParityCorpus"/>, the parser must give the same result as <see cref="FrozenRulesBasedExpressions"/> for each
    /// <c>Check*</c> method and for the whole tag, and decompose it into the same subtags, including tags that repeat a subtag and so are not
    /// well-formed. <see cref="RulesBasedParityCases"/> works out what the frozen expressions give, and <see cref="ParserObserver"/> what the
    /// parser gives. The corpus covers every code list, the IANA registry, combinations and random strings, so it is long, and these are
    /// integration tests.
    /// </remarks>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Collection(Bcp47LanguageTagParityCollection.Name)]
    public class Bcp47LanguageTagParityTests
    {
        [Fact]
        public void CheckSubtag_OverCorpus_AgreesWithFrozenRegularExpressions()
        {
            // arrange
            var cases = RulesBasedParityCases.Shared.SubtagChecks;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveSubtagChecks);

            // assert
            testResult.Should().BeEmpty("each Check* method must agree with the regular expression it replaced");
        }

        [Fact]
        public void Parser_OverCorpus_FindsTheSameLanguageTagsAsFrozenRegularExpressions()
        {
            // arrange
            var cases = RulesBasedParityCases.Shared.Validity;

            // act
            var testResult = DifferentialRunner.FindMismatches(cases, ParserObserver.ObserveValidity);

            // assert
            testResult.Should().BeEmpty("IsWellFormedLanguageTagString, TryCreate and the parser must agree with the regular expressions they replaced");
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
    }
}
