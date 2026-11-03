#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the Extended Language subtags to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtags
    {
        /// <summary>
        /// Build a language tag using Extended Language subtags registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="firstExtendedLanguageSubtag">First extended language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext UsingExtendedLanguageSubtags(string firstExtendedLanguageSubtag);
#else
        [return: NotNull]
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext UsingExtendedLanguageSubtags([DisallowNull] string firstExtendedLanguageSubtag);
#endif
        /// <summary>
        /// Build a language tag using Extended Language subtags registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="firstExtendedLanguageSubtag">First extended language subtag.</param>
        /// <param name="secondExtendedLanguageSubtag">Second extended language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext UsingExtendedLanguageSubtags(string firstExtendedLanguageSubtag, string secondExtendedLanguageSubtag);
#else
        [return: NotNull]
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext UsingExtendedLanguageSubtags([DisallowNull] string firstExtendedLanguageSubtag, [DisallowNull] string secondExtendedLanguageSubtag);
#endif
        /// <summary>
        /// Build a language tag using Extended Language subtags registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="firstExtendedLanguageSubtag">First extended language subtag.</param>
        /// <param name="secondExtendedLanguageSubtag">Second extended language subtag.</param>
        /// <param name="thirdExtendedLanguageSubtag">Third extended language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext UsingExtendedLanguageSubtags(string firstExtendedLanguageSubtag, string secondExtendedLanguageSubtag, string thirdExtendedLanguageSubtag);
#else
        [return: NotNull]
        IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext UsingExtendedLanguageSubtags([DisallowNull] string firstExtendedLanguageSubtag, [DisallowNull] string secondExtendedLanguageSubtag, [DisallowNull] string thirdExtendedLanguageSubtag);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Extended Language subtags.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtagsNext : IBcp47LanguageTagBuilderStepUsingScriptSubtag, IBcp47LanguageTagBuilderStepUsingRegionSubtag, IBcp47LanguageTagBuilderStepUsingVariantSubtags,
        IBcp47LanguageTagBuilderStepUsingExtensionSubtags, IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag, IBcp47LanguageTagBuilderStepBuild
    {
    }
}