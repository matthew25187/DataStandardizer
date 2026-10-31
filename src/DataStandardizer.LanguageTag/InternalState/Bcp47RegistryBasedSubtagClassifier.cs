using System;
using DataStandardizer.LanguageTag.SubtagRegistry;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Classifies subtags by the records of a Subtag Registry: a subtag is valid in a position if the registry has a record of that type for it.
    /// </summary>
    /// <remarks>
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
        internal Bcp47RegistryBasedSubtagClassifier(SubtagRegistry.SubtagRegistry subtagRegistry)
#else
        internal Bcp47RegistryBasedSubtagClassifier([NotNull] SubtagRegistry.SubtagRegistry subtagRegistry)
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

        internal override bool IsGrandfathered(ReadOnlySpan<char> languageTag)
        {
            return _grandfatheredLanguageTags.Contains(languageTag);
        }

        internal override bool IsPrimaryLanguage(ReadOnlySpan<char> subtag, out bool allowsExtendedLanguage)
        {
            // The registry's Prefix fields are not checked, so any registered Primary Language subtag may be followed by Extended Language subtags.
            allowsExtendedLanguage = _primaryLanguageSubtags.Contains(subtag);
            return allowsExtendedLanguage;
        }

        internal override bool IsExtendedLanguage(ReadOnlySpan<char> subtag)
        {
            return _extendedLanguageSubtags.Contains(subtag);
        }

        internal override bool IsScript(ReadOnlySpan<char> subtag)
        {
            return _scriptSubtags.Contains(subtag);
        }

        internal override bool IsRegion(ReadOnlySpan<char> subtag)
        {
            return _regionSubtags.Contains(subtag);
        }

        internal override bool IsVariant(ReadOnlySpan<char> subtag)
        {
            return _variantSubtags.Contains(subtag);
        }
    }
}
