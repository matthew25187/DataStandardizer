#if NET9_0_OR_GREATER
using System;
#endif
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
#if NET9_0_OR_GREATER

        /// <summary>
        /// Build a language tag using Variant subtags registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="variantSubtag">First variant subtag.</param>
        /// <param name="variantSubtags">Remaining variant subtags.</param>
        /// <returns>Language tag builder.</returns>
        /// <remarks>
        /// <see cref="Bcp47LanguageTagBuilder"/> reads the subtags from the span, without first copying them to an array of their own.
        /// The default implementation copies them to an array and calls <see cref="UsingVariantSubtags(string, string[])"/>.
        /// </remarks>
        IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext UsingVariantSubtags(string variantSubtag, params ReadOnlySpan<string> variantSubtags)
        {
            return UsingVariantSubtags(variantSubtag, variantSubtags.ToArray());
        }
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Variant subtags.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingVariantSubtagsNext : IBcp47LanguageTagBuilderStepUsingExtensionSubtags, IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag, IBcp47LanguageTagBuilderStepBuild
    {
    }
}