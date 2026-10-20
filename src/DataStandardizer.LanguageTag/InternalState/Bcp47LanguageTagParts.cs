using System;
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
    /// directly: <see cref="Copy{T}"/> gives each caller its own, because the public properties return mutable arrays (#255).
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

#if NETCOREAPP3_0_OR_GREATER
        private Bcp47LanguageTagParts(string primaryLanguage, string[] extendedLanguages, string? script, string? region, string[] variants, Bcp47KeyedSubtag[] extensions, Bcp47KeyedSubtag? privateUse)
#else
        private Bcp47LanguageTagParts([NotNull] string primaryLanguage, [NotNull] string[] extendedLanguages, [CanBeNull] string script, [CanBeNull] string region, [NotNull] string[] variants, [NotNull] Bcp47KeyedSubtag[] extensions, Bcp47KeyedSubtag? privateUse)
#endif
        {
            PrimaryLanguage = primaryLanguage;
            ExtendedLanguages = extendedLanguages;
            Script = script;
            Region = region;
            Variants = variants;
            Extensions = extensions;
            PrivateUse = privateUse;
        }

        /// <summary>
        /// The Primary Language subtag; the empty string for a grandfathered or private use language tag.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal string PrimaryLanguage { get; }
#else
        [NotNull] internal string PrimaryLanguage { get; }
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal string[] ExtendedLanguages { get; }
#else
        [NotNull] internal string[] ExtendedLanguages { get; }
#endif

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

#if NETCOREAPP3_0_OR_GREATER
        internal string[] Variants { get; }
#else
        [NotNull] internal string[] Variants { get; }
#endif

#if NETCOREAPP3_0_OR_GREATER
        internal Bcp47KeyedSubtag[] Extensions { get; }
#else
        [NotNull] internal Bcp47KeyedSubtag[] Extensions { get; }
#endif

        internal Bcp47KeyedSubtag? PrivateUse { get; }

        /// <summary>
        /// Decompose a language tag from where the parser found each kind of subtag.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static Bcp47LanguageTagParts FromRanges(string languageTag, Bcp47LanguageTagRanges ranges)
#else
        [NotNull]
        internal static Bcp47LanguageTagParts FromRanges([NotNull] string languageTag, Bcp47LanguageTagRanges ranges)
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
                ranges.PrimaryLanguage.IsEmpty ? string.Empty : Substring(languageTag, ranges.PrimaryLanguage),
                ranges.ExtendedLanguages.IsEmpty ? NoSubtags : Bcp47LanguageTagParser.GetSubtags(languageTag, ranges.ExtendedLanguages),
                ranges.Script.IsEmpty ? null : Substring(languageTag, ranges.Script),
                ranges.Region.IsEmpty ? null : Substring(languageTag, ranges.Region),
                ranges.Variants.IsEmpty ? NoSubtags : Bcp47LanguageTagParser.GetSubtags(languageTag, ranges.Variants),
                extensions,
                ranges.PrivateUse.IsEmpty ? (Bcp47KeyedSubtag?)null : new Bcp47KeyedSubtag(Substring(languageTag, ranges.PrivateUse)));
        }

        /// <summary>
        /// Copy an array of subtags for a caller.
        /// </summary>
        /// <remarks>
        /// An empty array is returned as it is, since it cannot be changed.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        internal static T[] Copy<T>(T[] subtags)
#else
        [NotNull]
        internal static T[] Copy<T>([NotNull] T[] subtags)
#endif
        {
            if (subtags.Length == 0) return subtags;

            var copy = new T[subtags.Length];
            Array.Copy(subtags, copy, subtags.Length);
            return copy;
        }

        private static string Substring(string languageTag, Bcp47SubtagRange range)
        {
            return languageTag.Substring(range.Start, range.Length);
        }
    }
}
