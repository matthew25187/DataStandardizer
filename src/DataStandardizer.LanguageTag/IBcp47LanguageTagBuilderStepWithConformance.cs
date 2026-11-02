#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying what to check when the language tag is built, without a Subtag Registry.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithConformance
    {
        /// <summary>
        /// Build a language tag that satisfies a conformance that needs no Subtag Registry.
        /// </summary>
        /// <param name="conformance">What to check: <see cref="LanguageTagConformance.WellFormed"/>, which is what a builder checks if this step is left out, or <see cref="LanguageTagConformance.IsoCodeLists"/>.</param>
        /// <returns>Language tag builder.</returns>
        /// <remarks>
        /// To check validity (<see cref="LanguageTagConformance.Valid"/>), use <see cref="IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistry.WithLanguageSubtagRegistry"/> instead.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepWithConformanceNext WithConformance(LanguageTagConformance conformance);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepWithConformanceNext WithConformance(LanguageTagConformance conformance);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified what to check.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithConformanceNext : IBcp47LanguageTagBuilderStepUsingLanguageTag, IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtag
    {
    }
}
