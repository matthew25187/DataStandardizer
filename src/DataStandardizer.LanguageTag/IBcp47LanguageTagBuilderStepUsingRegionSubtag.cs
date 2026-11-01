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
        /// Build a language tag using a Region subtag from a UN M49 area.
        /// </summary>
        /// <param name="regionSubtag">UN M49 area to use for the Region subtag.</param>
        /// <returns>Language tag builder.</returns>
        /// <remarks>
        /// BCP 47 allows only the UN M49 codes for areas made up of more than one country, such as <see cref="UnM49Area._419"/> (Latin America and the Caribbean).
        /// A UN M49 country code is not a valid Region subtag; use the <see cref="Iso3166Part1Alpha2Country"/> overload for a country instead.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(UnM49Area regionSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingRegionSubtagNext UsingRegionSubtag(UnM49Area regionSubtag);
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