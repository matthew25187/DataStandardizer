using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Subtags of a language tag, decomposed once when the tag is created.
    /// </summary>
    /// <remarks>
    /// Instances are immutable and may be shared between copies of a <see cref="Bcp47LanguageTag"/>. The arrays are never handed out
    /// directly: callers get a read-only view of each, created on first read and then reused, so a caller can neither change the
    /// decomposition nor reach the array behind it by a cast.
    /// </remarks>
    internal sealed class Bcp47LanguageTagParts
    {
#if NETSTANDARD1_3_OR_GREATER || NET
        private static readonly string[] NoSubtags = Array.Empty<string>();
        private static readonly Bcp47KeyedSubtag[] NoKeyedSubtags = Array.Empty<Bcp47KeyedSubtag>();
#else
        private static readonly string[] NoSubtags = new string[0];
        private static readonly Bcp47KeyedSubtag[] NoKeyedSubtags = new Bcp47KeyedSubtag[0];
#endif
        private static readonly ReadOnlyCollection<string> NoSubtagsView = new ReadOnlyCollection<string>(NoSubtags);
        private static readonly ReadOnlyCollection<Bcp47KeyedSubtag> NoKeyedSubtagsView = new ReadOnlyCollection<Bcp47KeyedSubtag>(NoKeyedSubtags);

        // Read-only views of the arrays, created on first read. A race only creates an equivalent view, so no lock is taken.
#if NETCOREAPP3_0_OR_GREATER
        private ReadOnlyCollection<string>? _extendedLanguagesView;
        private ReadOnlyCollection<string>? _variantsView;
        private ReadOnlyCollection<Bcp47KeyedSubtag>? _extensionsView;
#else
        [CanBeNull] private ReadOnlyCollection<string> _extendedLanguagesView;
        [CanBeNull] private ReadOnlyCollection<string> _variantsView;
        [CanBeNull] private ReadOnlyCollection<Bcp47KeyedSubtag> _extensionsView;
#endif

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47LanguageTagParts(string? primaryLanguage, string[] extendedLanguages, string? script, string? region, string[] variants, Bcp47KeyedSubtag[] extensions, Bcp47KeyedSubtag? privateUse, LanguageTagConformance conformance)
#else
        private Bcp47LanguageTagParts([CanBeNull] string primaryLanguage, [NotNull] string[] extendedLanguages, [CanBeNull] string script, [CanBeNull] string region, [NotNull] string[] variants, [NotNull] Bcp47KeyedSubtag[] extensions, Bcp47KeyedSubtag? privateUse, LanguageTagConformance conformance)
#endif
        {
            PrimaryLanguage = primaryLanguage;
            _extendedLanguages = extendedLanguages;
            Script = script;
            Region = region;
            _variants = variants;
            _extensions = extensions;
            PrivateUse = privateUse;
            Conformance = conformance;
        }

        /// <summary>
        /// The Primary Language subtag; <c>null</c> for a grandfathered or private use language tag.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal string? PrimaryLanguage { get; }
#else
        [CanBeNull] internal string PrimaryLanguage { get; }
#endif

        private readonly string[] _extendedLanguages;

        /// <summary>
        /// The Extended Language subtags, as a read-only view.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal IReadOnlyList<string> ExtendedLanguages
#else
        [NotNull] internal IReadOnlyList<string> ExtendedLanguages
#endif
        {
            get { return _extendedLanguages.Length == 0 ? NoSubtagsView : _extendedLanguagesView ?? (_extendedLanguagesView = new ReadOnlyCollection<string>(_extendedLanguages)); }
        }

#if NETCOREAPP3_0_OR_GREATER
        internal string? Script { get; }
#else
        [CanBeNull] internal string Script { get; }
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal string? Region { get; }
#else
        [CanBeNull] internal string Region { get; }
#endif

        private readonly string[] _variants;

        /// <summary>
        /// The Variant subtags, as a read-only view.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal IReadOnlyList<string> Variants
#else
        [NotNull] internal IReadOnlyList<string> Variants
#endif
        {
            get { return _variants.Length == 0 ? NoSubtagsView : _variantsView ?? (_variantsView = new ReadOnlyCollection<string>(_variants)); }
        }

        private readonly Bcp47KeyedSubtag[] _extensions;

        /// <summary>
        /// The Extension subtags, as a read-only view.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal IReadOnlyList<Bcp47KeyedSubtag> Extensions
#else
        [NotNull] internal IReadOnlyList<Bcp47KeyedSubtag> Extensions
#endif
        {
            get { return _extensions.Length == 0 ? NoKeyedSubtagsView : _extensionsView ?? (_extensionsView = new ReadOnlyCollection<Bcp47KeyedSubtag>(_extensions)); }
        }

        internal Bcp47KeyedSubtag? PrivateUse { get; }

        /// <summary>
        /// What was checked when the language tag was created.
        /// </summary>
        internal LanguageTagConformance Conformance { get; }

        /// <summary>
        /// Decompose a language tag from where the parser found each kind of subtag.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static Bcp47LanguageTagParts FromRanges(string languageTag, Bcp47LanguageTagRanges ranges, LanguageTagConformance conformance)
#else
        [NotNull]
        internal static Bcp47LanguageTagParts FromRanges([NotNull] string languageTag, Bcp47LanguageTagRanges ranges, LanguageTagConformance conformance)
#endif
        {
            var extensions = NoKeyedSubtags;
            if (!ranges.Extensions.IsEmpty)
            {
                var extensionStrings = Bcp47LanguageTagParser.GetExtensions(languageTag, ranges.Extensions);
                extensions = new Bcp47KeyedSubtag[extensionStrings.Length];
                for (var index = 0; index < extensionStrings.Length; index++)
                {
                    extensions[index] = new Bcp47KeyedSubtag(extensionStrings[index]);
                }
            }

            return new Bcp47LanguageTagParts(
                ranges.PrimaryLanguage.IsEmpty ? null : Substring(languageTag, ranges.PrimaryLanguage),
                ranges.ExtendedLanguages.IsEmpty ? NoSubtags : Bcp47LanguageTagParser.GetSubtags(languageTag, ranges.ExtendedLanguages),
                ranges.Script.IsEmpty ? null : Substring(languageTag, ranges.Script),
                ranges.Region.IsEmpty ? null : Substring(languageTag, ranges.Region),
                ranges.Variants.IsEmpty ? NoSubtags : Bcp47LanguageTagParser.GetSubtags(languageTag, ranges.Variants),
                extensions,
                ranges.PrivateUse.IsEmpty ? (Bcp47KeyedSubtag?)null : new Bcp47KeyedSubtag(Substring(languageTag, ranges.PrivateUse)),
                conformance);
        }

        private static string Substring(string languageTag, Bcp47SubtagRange range)
        {
            return languageTag.Substring(range.Start, range.Length);
        }
    }
}
