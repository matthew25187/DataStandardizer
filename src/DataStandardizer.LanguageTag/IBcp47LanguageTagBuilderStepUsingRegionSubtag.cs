using DataStandardizer.Geography;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the Region subtag to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingRegionSubtag
    {
        /// <summary>
        /// Build a language tag using a Region subtag from ISO 3166-1 alpha-2.
        /// </summary>
        /// <param name="regionSubtag">ISO 3166-1 alpha-2 code to use for the Region subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(Iso3166Part1Alpha2Country regionSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(Iso3166Part1Alpha2Country regionSubtag);
#endif

        /// <summary>
        /// Build a language tag using a Region subtag from UN M49.
        /// </summary>
        /// <param name="regionSubtag">UN M49 code to use for the Region subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(UnM49AreaByAlpha2CountryCode regionSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(UnM49AreaByAlpha2CountryCode regionSubtag);
#endif
        /// <summary>
        /// Build a language tag using a Region subtag from UN M49.
        /// </summary>
        /// <param name="regionSubtag">UN M49 code to use for the Region subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(UnM49AreaByAlpha3CountryCode regionSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(UnM49AreaByAlpha3CountryCode regionSubtag);
#endif
        /// <summary>
        /// Build a language tag using a Region subtag from ISO 3166-1 alpha-2 or UN M49.
        /// </summary>
        /// <param name="regionSubtag">ISO 3166-1 alpha-2 or UN M49 code to use for the Region subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(string regionSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag([NotNull] string regionSubtag);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Region subtag.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingRegionSubtagNext : IBcp47LanguageTagBuilderStepUsingVariantSubtags, IBcp47LanguageTagBuilderStepUsingExtensionSubtags, IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag, IBcp47LanguageTagBuilderStepBuild
    {
    }
}