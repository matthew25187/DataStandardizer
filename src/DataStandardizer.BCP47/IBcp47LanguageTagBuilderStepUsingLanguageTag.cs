#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.BCP47
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the IETF BCP 47 language tag to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingLanguageTag
    {
        /// <summary>
        /// Build a language tag using a string formatted as an IETF BCP 47 language tag.
        /// </summary>
        /// <param name="languageTag">IETF BCP 47 language tag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepBuild UsingLanguageTag(string languageTag); 
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepBuild UsingLanguageTag([NotNull] string languageTag); 
#endif
    }
}