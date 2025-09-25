#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the IANA Language Subtag Registry to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistry
    {
        /// <summary>
        /// Build a language tag with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="subtagRegistry">A copy of the Subtag Registry.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext WithLanguageSubtagRegistry(SubtagRegistry.SubtagRegistry subtagRegistry);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext WithLanguageSubtagRegistry([NotNull] SubtagRegistry.SubtagRegistry subtagRegistry);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified whether to use the IANA Language Subtag Registry.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistryNext : IBcp47LanguageTagBuilderStepWithTimeout, IBcp47LanguageTagBuilderStepUsingLanguageTag, IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtag
    {
    }
}