#if NET9_0_OR_GREATER
using System;
#endif
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying the Extension subtags to be used.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingExtensionSubtags
    {
        /// <summary>
        /// Build a language tag using Extension subtags registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="extensionSubtag">First extension subtag.</param>
        /// <param name="extensionSubtags">Remaining extension subtags.</param>
        /// <returns>Language tag builder.</returns>
#if NETCOREAPP3_0_OR_GREATER
        IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext UsingExtensionSubtags(string extensionSubtag, params string[] extensionSubtags);
#else
        [NotNull]
        IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext UsingExtensionSubtags([NotNull] string extensionSubtag, [NotNull] params string[] extensionSubtags);
#endif
#if NET9_0_OR_GREATER

        /// <summary>
        /// Build a language tag using Extension subtags registered with the IANA Language Subtag Registry.
        /// </summary>
        /// <param name="extensionSubtag">First extension subtag.</param>
        /// <param name="extensionSubtags">Remaining extension subtags.</param>
        /// <returns>Language tag builder.</returns>
        /// <remarks>
        /// <see cref="Bcp47LanguageTagBuilder"/> reads the subtags from the span, without first copying them to an array of their own.
        /// The default implementation copies them to an array and calls <see cref="UsingExtensionSubtags(string, string[])"/>.
        /// </remarks>
        IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext UsingExtensionSubtags(string extensionSubtag, params ReadOnlySpan<string> extensionSubtags)
        {
            return UsingExtensionSubtags(extensionSubtag, extensionSubtags.ToArray());
        }
#endif
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified the Extension subtags.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepUsingExtensionSubtagsNext : IBcp47LanguageTagBuilderStepUsingPrivateUseSubtag, IBcp47LanguageTagBuilderStepBuild
    {
    }
}