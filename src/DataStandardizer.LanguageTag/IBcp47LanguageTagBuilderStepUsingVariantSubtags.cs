#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the Variant subtags to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingVariantSubtags
    {
        /// <summary>
        /// Build a language tag using Variant subtags registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="variantSubtag">First variant subtag.</param>
        /// <param name="variantSubtags">Remaining variant subtags.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext UsingVariantSubtags(string variantSubtag, params string[] variantSubtags);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext UsingVariantSubtags([NotNull] string variantSubtag, [NotNull] params string[] variantSubtags);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Variant subtags.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext : IBcp47LanguageTagBuilderStepUsingExtensionSubtags, IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag, IBcp47LanguageTagBuilderStepBuild
    {
    }
}