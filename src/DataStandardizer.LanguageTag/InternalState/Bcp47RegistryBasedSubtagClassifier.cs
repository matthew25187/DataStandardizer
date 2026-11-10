using System;
using DataStandardizer.LanguageTag.Registry;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Classifies subtags by the records of a Subtag Registry: a subtag is valid in a position if it has the shape of that position and the registry has a record of that type for it.
    /// </summary>
    /// <remarks>
    /// RFC 5646 §2.2.9's first condition of validity is that the tag is well-formed, so a record never admits what <see cref="Bcp47SyntaxSubtagClassifier"/> rejects, whatever the registry holds.
    /// The sets are built once, from the registry's records of each type, and never change, so one instance may be used from several threads.
    /// A record whose <c>Subtag</c> denotes a range, such as <c>qaa..qtz</c>, adds every subtag in the range (ref. RFC 5646 §3.1.1, §3.1.4).
    /// Validation needs only to know that a subtag is registered, so the classifier holds sets, and leaves the map from subtags to their records,
    /// which costs more to build on first use, to <see cref="Bcp47RegistrySubtagIndex"/>.
    /// </remarks>
    internal sealed class Bcp47RegistryBasedSubtagClassifier : Bcp47SubtagClassifier
    {
        private readonly Bcp47SubtagSet _grandfatheredLanguageTags;
        private readonly Bcp47SubtagSet _primaryLanguageSubtags;
        private readonly Bcp47SubtagSet _extendedLanguageSubtags;
        private readonly Bcp47SubtagSet _scriptSubtags;
        private readonly Bcp47SubtagSet _regionSubtags;
        private readonly Bcp47SubtagSet _variantSubtags;

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47RegistryBasedSubtagClassifier(SubtagRegistry subtagRegistry)
#else
        internal Bcp47RegistryBasedSubtagClassifier([DisallowNull] SubtagRegistry subtagRegistry)
#endif
        {
            var subtags = new Bcp47RegistrySubtagLists(subtagRegistry);

            _grandfatheredLanguageTags = subtags.GrandfatheredLanguageTags.ToSet();
            _primaryLanguageSubtags = subtags.PrimaryLanguageSubtags.ToSet();
            _extendedLanguageSubtags = subtags.ExtendedLanguageSubtags.ToSet();
            _scriptSubtags = subtags.ScriptSubtags.ToSet();
            _regionSubtags = subtags.RegionSubtags.ToSet();
            _variantSubtags = subtags.VariantSubtags.ToSet();
        }

        /// <summary>
        /// Determine whether the registry has a record of type <c>grandfathered</c> for a language tag, whether or not the ABNF lists the tag.
        /// </summary>
        internal bool HasGrandfatheredRecord(ReadOnlySpan<char> languageTag)
        {
            return _grandfatheredLanguageTags.Contains(languageTag);
        }

        internal override bool IsGrandfathered(ReadOnlySpan<char> languageTag)
        {
            return Bcp47SyntaxSubtagClassifier.Instance.IsGrandfathered(languageTag) && HasGrandfatheredRecord(languageTag);
        }

        internal override bool IsPrimaryLanguage(ReadOnlySpan<char> subtag, out bool allowsExtendedLanguage)
        {
            // RFC 5646 §2.2.9 does not list the registry's Prefix fields among the conditions of validity, so any registered two- or three-letter Primary Language subtag may be followed by Extended Language subtags.
            return Bcp47SyntaxSubtagClassifier.Instance.IsPrimaryLanguage(subtag, out allowsExtendedLanguage) && _primaryLanguageSubtags.Contains(subtag);
        }

        internal override bool IsExtendedLanguage(ReadOnlySpan<char> subtag)
        {
            return Bcp47SyntaxSubtagClassifier.Instance.IsExtendedLanguage(subtag) && _extendedLanguageSubtags.Contains(subtag);
        }

        internal override bool IsScript(ReadOnlySpan<char> subtag)
        {
            return Bcp47SyntaxSubtagClassifier.Instance.IsScript(subtag) && _scriptSubtags.Contains(subtag);
        }

        internal override bool IsRegion(ReadOnlySpan<char> subtag)
        {
            return Bcp47SyntaxSubtagClassifier.Instance.IsRegion(subtag) && _regionSubtags.Contains(subtag);
        }

        internal override bool IsVariant(ReadOnlySpan<char> subtag)
        {
            return Bcp47SyntaxSubtagClassifier.Instance.IsVariant(subtag) && _variantSubtags.Contains(subtag);
        }
    }
}
