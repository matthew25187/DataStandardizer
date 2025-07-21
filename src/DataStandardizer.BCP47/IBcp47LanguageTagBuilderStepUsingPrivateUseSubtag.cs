#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.BCP47
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the Private Use subtag to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag
    {
        /// <summary>
        /// Build a language tag using a Private Use subtag.
        /// </summary>
        /// <param name="privateUseSubtag">Private use subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext UsingPrivateUseSubtag(string privateUseSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext UsingPrivateUseSubtag([NotNull] string privateUseSubtag);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Private Use subtag.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingPrivateUseSubtagNext : IBcp47LanguageTagBuilderStepBuild
    {
    }
}