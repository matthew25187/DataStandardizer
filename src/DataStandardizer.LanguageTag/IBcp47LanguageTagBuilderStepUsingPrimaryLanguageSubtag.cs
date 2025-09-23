using DataStandardizer.Language;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the Primary Language subtag to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtag
    {
        /// <summary>
        /// Build a language tag using a Primary Language subtag from ISO 639-1.
        /// </summary>
        /// <param name="primaryLanguageSubtag">ISO 639-1 code to use for the Primary Language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part1Language primaryLanguageSubtag); 
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part1Language primaryLanguageSubtag); 
#endif

        /// <summary>
        /// Build a language tag using a Primary Language subtag from ISO 639-2T.
        /// </summary>
        /// <param name="primaryLanguageSubtag">ISO 639-2T code to use for the Primary Language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part2TLanguage primaryLanguageSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part2TLanguage primaryLanguageSubtag); 
#endif

        /// <summary>
        /// Build a language tag using a Primary Language subtag from ISO 639-3.
        /// </summary>
        /// <param name="primaryLanguageSubtag">ISO 639-3 code to use for the Primary Language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part3Language primaryLanguageSubtag); 
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part3Language primaryLanguageSubtag); 
#endif

        /// <summary>
        /// Build a language tag using a Primary Language subtag from ISO 639-5.
        /// </summary>
        /// <param name="primaryLanguageSubtag">ISO 639-5 code to use for the Primary Language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part5LanguageFamily primaryLanguageSubtag); 
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(Iso639Part5LanguageFamily primaryLanguageSubtag); 
#endif

        /// <summary>
        /// Build a language tag using a Primary Language subtag registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="primaryLanguageSubtag">Primary language subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag(string primaryLanguageSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext UsingPrimaryLanguageSubtag([NotNull] string primaryLanguageSubtag);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Primary Language subtag.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtagNext : IBcp47LanguageTagBuilderStepUsingExtendedLanguageSubtags, IBcp47LanguageTagBuilderStepUsingScriptSubtag, IBcp47LanguageTagBuilderStepUsingRegionSubtag, IBcp47LanguageTagBuilderStepUsingVariantSubtags,
        IBcp47LanguageTagBuilderStepUsingExtensionSubtags, IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag, IBcp47LanguageTagBuilderStepBuild
    {
    }
}