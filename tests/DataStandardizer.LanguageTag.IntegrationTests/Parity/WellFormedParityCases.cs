using System;
using System.Collections.Generic;
using System.Linq;
using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// The test cases of <see cref="Bcp47LanguageTagWellFormedParityTests"/>: every input of the corpus, with what <see cref="Rfc5646AbnfExpression"/> gives for it.
    /// </summary>
    /// <remarks>
    /// This is the test case factory of the parity tests, so it may loop and branch: it runs the reference implementation, not the code under test.
    /// The cases are built once, on every core, the first time a test asks for them.
    /// </remarks>
    internal sealed class WellFormedParityCases
    {
        private static readonly Lazy<WellFormedParityCases> SharedCases = new Lazy<WellFormedParityCases>(() => new WellFormedParityCases());

        private WellFormedParityCases()
        {
            var expectations = ParityCorpus.Inputs
                .AsParallel()
                .AsOrdered()
                .Select(Expect)
                .ToList();

            SubtagChecks = ParityCorpus.Inputs.Select(input => new DifferentialCase<SubtagCheckResults>(input, Rfc5646AbnfExpression.CheckSubtag(input))).ToList();
            Validity = expectations.Select(expectation => new DifferentialCase<LanguageTagValidity>(expectation.Input, expectation.Validity)).ToList();
            Decomposition = expectations.Select(expectation => new DifferentialCase<SubtagTexts>(expectation.Input, expectation.Subtags)).ToList();
            Properties = expectations
                .Where(expectation => expectation.Validity.IsWellFormed)
                .Select(expectation => new DifferentialCase<SubtagTexts>(expectation.Input, expectation.Subtags))
                .ToList();
        }

        internal static WellFormedParityCases Shared => SharedCases.Value;

        /// <summary>
        /// Every input, with whether it matches each production of the grammar for one subtag.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagCheckResults>> SubtagChecks { get; }

        /// <summary>
        /// Every input, with whether it matches the grammar and whether the match repeats a subtag.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<LanguageTagValidity>> Validity { get; }

        /// <summary>
        /// Every input, with the subtags the grammar matched in it.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagTexts>> Decomposition { get; }

        /// <summary>
        /// Every input that matches the grammar, with the subtags the grammar matched in it.
        /// </summary>
        internal IReadOnlyList<DifferentialCase<SubtagTexts>> Properties { get; }

        private static (string Input, LanguageTagValidity Validity, SubtagTexts Subtags) Expect(string input)
        {
            var match = Rfc5646AbnfExpression.Match(input);
            var status = !match.Success ? Bcp47ParseStatus.Invalid : FrozenRulesBasedExpressions.IsWellFormed(match) ? Bcp47ParseStatus.Accepted : Bcp47ParseStatus.RepeatedSubtag;

            return (input, new LanguageTagValidity(match.Success, match.Success, status), FrozenSubtags.Of(match));
        }
    }
}
