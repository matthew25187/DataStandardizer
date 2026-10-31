using System;

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Decides which subtags are valid at each position of a language tag, for <see cref="Bcp47LanguageTagParser"/>.
    /// </summary>
    /// <remarks>
    /// Extension and private use subtags follow the same rules in every mode, so the parser checks those itself.
    /// </remarks>
    internal abstract class Bcp47SubtagClassifier
    {
        /// <summary>
        /// Determine whether a whole language tag is a grandfathered tag, which is interpreted as a whole (ref. RFC 5646 §2.2.8).
        /// </summary>
        internal abstract bool IsGrandfathered(ReadOnlySpan<char> languageTag);

        /// <summary>
        /// Determine whether a subtag is a valid Primary Language subtag.
        /// </summary>
        /// <param name="subtag">Subtag to check.</param>
        /// <param name="allowsExtendedLanguage">When this method returns <c>true</c>, whether Extended Language subtags may follow the subtag.</param>
        internal abstract bool IsPrimaryLanguage(ReadOnlySpan<char> subtag, out bool allowsExtendedLanguage);

        internal abstract bool IsExtendedLanguage(ReadOnlySpan<char> subtag);

        internal abstract bool IsScript(ReadOnlySpan<char> subtag);

        internal abstract bool IsRegion(ReadOnlySpan<char> subtag);

        internal abstract bool IsVariant(ReadOnlySpan<char> subtag);
    }
}
