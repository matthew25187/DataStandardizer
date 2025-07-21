using DataStandardizer.ISO15924;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.BCP47
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the Script subtag to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingScriptSubtag
    {
        /// <summary>
        /// Build a language tag using a Script subtag from ISO 15924.
        /// </summary>
        /// <param name="scriptSubtag">ISO 15924 code to use for the Script subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingScriptSubtagNext UsingScriptSubtag(Iso15924 scriptSubtag); 
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingScriptSubtagNext UsingScriptSubtag(Iso15924 scriptSubtag); 
#endif

        /// <summary>
        /// Build a language tag using a Script subtag from ISO 15924.
        /// </summary>
        /// <param name="scriptSubtag">ISO 15924 code to use for the Script subtag.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingScriptSubtagNext UsingScriptSubtag(string scriptSubtag);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingScriptSubtagNext UsingScriptSubtag([NotNull] string scriptSubtag);
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Script subtag.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingScriptSubtagNext : IBcp47LanguageTagBuilderStepUsingRegionSubtag, IBcp47LanguageTagBuilderStepUsingVariantSubtags, IBcp47LanguageTagBuilderStepUsingExtensionSubtags, IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag,
        IBcp47LanguageTagBuilderStepBuild
    {
    }
}