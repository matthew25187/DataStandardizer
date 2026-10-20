using System;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.Bcp47Constants;

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
        /// Decompose a language tag from a match of a Subtag Registry language tag expression.
        /// </summary>
        /// <returns>The subtags; <c>null</c> if the match failed or the tag repeats a variant subtag or an extension singleton, so it is not well-formed.</returns>
        /// <remarks>
        /// The expression checks the syntax of each subtag but not that variant subtags and extension singletons are unique (ref. RFC 5646 §2.2.5¶5, §2.2.6¶3).
        /// Private use subtags may repeat.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        internal static Bcp47LanguageTagParts? FromMatch(Match languageTagMatch)
#else
        [CanBeNull]
        internal static Bcp47LanguageTagParts FromMatch([NotNull] Match languageTagMatch)
#endif
        {
            if (!languageTagMatch.Success) return null;

            var groups = languageTagMatch.Groups;

            var variants = GetCaptures(groups[LanguageTagSubtagGroupName.Variant]);
            if (Bcp47LanguageTag.IndexOfRepeatedVariantSubtag(variants) >= 0) return null;

            var extensionStrings = GetCaptures(groups[LanguageTagSubtagGroupName.Extension]);
            if (Bcp47LanguageTag.IndexOfRepeatedExtensionSingleton(extensionStrings) >= 0) return null;

            var extensions = NoKeyedSubtags;
            if (extensionStrings.Length > 0)
            {
                extensions = new Bcp47KeyedSubtag[extensionStrings.Length];
                for (var index = 0; index < extensionStrings.Length; index++)
                {
                    extensions[index] = new Bcp47KeyedSubtag(extensionStrings[index]);
                }
            }

            var privateUseGroup = groups[LanguageTagSubtagGroupName.PrivateUse];

            return new Bcp47LanguageTagParts(
                groups[LanguageTagSubtagGroupName.PrimaryLanguage].Value,
                GetCaptures(groups[LanguageTagSubtagGroupName.ExtendedLanguage]),
                GetValue(groups[LanguageTagSubtagGroupName.Script]),
                GetValue(groups[LanguageTagSubtagGroupName.Region]),
                variants,
                extensions,
                privateUseGroup.Success ? new Bcp47KeyedSubtag(privateUseGroup.Value) : (Bcp47KeyedSubtag?)null);
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

        private static string[] GetCaptures(Group group)
        {
            var captures = group.Captures;
            if (captures.Count == 0) return NoSubtags;

            var values = new string[captures.Count];
            for (var index = 0; index < values.Length; index++)
            {
                values[index] = captures[index].Value;
            }

            return values;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? GetValue(Group group)
#else
        [CanBeNull]
        private static string GetValue(Group group)
#endif
        {
            return group.Success ? group.Value : null;
        }
    }
}
