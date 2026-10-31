using System;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Parses language tags and the subtags that follow the same rules in every mode (ref. RFC 5646 §2.1).
    /// </summary>
    /// <remarks>
    /// The grammar is unambiguous by position, length, first character and set membership, so one greedy left-to-right pass over the
    /// hyphen-separated subtags finds the only decomposition there is. Characters are matched in the ASCII range only.
    /// </remarks>
    internal static class Bcp47LanguageTagParser
    {
        private const char SubtagSeparator = '-';

        /// <summary>
        /// Parse a language tag.
        /// </summary>
        /// <param name="languageTag">Language tag to parse.</param>
        /// <param name="classifier">Decides which subtags are valid at each position.</param>
        /// <param name="ranges">When this method returns other than <see cref="Bcp47ParseStatus.Invalid"/>, where each kind of subtag lies in <paramref name="languageTag"/>; otherwise, empty ranges.</param>
        /// <returns>Whether the language tag is well-formed.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static Bcp47ParseStatus Parse(ReadOnlySpan<char> languageTag, Bcp47SubtagClassifier classifier, out Bcp47LanguageTagRanges ranges)
#else
        internal static Bcp47ParseStatus Parse(ReadOnlySpan<char> languageTag, [NotNull] Bcp47SubtagClassifier classifier, out Bcp47LanguageTagRanges ranges)
#endif
        {
            ranges = default;

            // Grandfathered tags come first, so that the regular ones are taken as a whole rather than as subtags (ref. RFC 5646 §2.2.8).
            if (classifier.IsGrandfathered(languageTag)) return Bcp47ParseStatus.WellFormed;

            var start = 0;
            var length = GetSubtagLength(languageTag, start);
            var subtag = languageTag.Slice(start, length);

            if (IsPrivateUseSingleton(subtag))
            {
                if (!IsPrivateUse(languageTag)) return Bcp47ParseStatus.Invalid;

                ranges.PrivateUse = new Bcp47SubtagRange(0, languageTag.Length);
                return Bcp47ParseStatus.WellFormed;
            }

            if (!classifier.IsPrimaryLanguage(subtag, out var allowsExtendedLanguage)) return Bcp47ParseStatus.Invalid;

            var result = default(Bcp47LanguageTagRanges);
            result.PrimaryLanguage = new Bcp47SubtagRange(start, length);
            var hasSubtag = MoveToNextSubtag(languageTag, ref start, ref length);
            int firstStart, end;

            if (allowsExtendedLanguage && hasSubtag && classifier.IsExtendedLanguage(languageTag.Slice(start, length)))
            {
                firstStart = start;
                var count = 0;
                do
                {
                    end = start + length;
                    hasSubtag = MoveToNextSubtag(languageTag, ref start, ref length);
                } while (++count < 3 && hasSubtag && classifier.IsExtendedLanguage(languageTag.Slice(start, length)));

                result.ExtendedLanguages = new Bcp47SubtagRange(firstStart, end - firstStart);
            }

            if (hasSubtag && classifier.IsScript(languageTag.Slice(start, length)))
            {
                result.Script = new Bcp47SubtagRange(start, length);
                hasSubtag = MoveToNextSubtag(languageTag, ref start, ref length);
            }

            if (hasSubtag && classifier.IsRegion(languageTag.Slice(start, length)))
            {
                result.Region = new Bcp47SubtagRange(start, length);
                hasSubtag = MoveToNextSubtag(languageTag, ref start, ref length);
            }

            var hasRepeatedSubtag = false;

            if (hasSubtag && classifier.IsVariant(languageTag.Slice(start, length)))
            {
                firstStart = start;
                do
                {
                    // A variant must not repeat an earlier one (ref. RFC 5646 §2.2.5¶5).
                    if (start > firstStart && ContainsSubtag(languageTag.Slice(firstStart, start - 1 - firstStart), languageTag.Slice(start, length))) hasRepeatedSubtag = true;

                    end = start + length;
                    hasSubtag = MoveToNextSubtag(languageTag, ref start, ref length);
                } while (hasSubtag && classifier.IsVariant(languageTag.Slice(start, length)));

                result.Variants = new Bcp47SubtagRange(firstStart, end - firstStart);
            }

            if (hasSubtag && IsExtensionSingleton(languageTag.Slice(start, length)))
            {
                firstStart = start;
                var singletons = 0UL;
                do
                {
                    // An extension singleton must not repeat an earlier one (ref. RFC 5646 §2.2.6¶3).
                    var singletonBit = 1UL << GetSingletonIndex(languageTag[start]);
                    if ((singletons & singletonBit) != 0) hasRepeatedSubtag = true;
                    singletons |= singletonBit;

                    // A singleton is followed by one or more subtags of two to eight characters.
                    hasSubtag = MoveToNextSubtag(languageTag, ref start, ref length);
                    if (!hasSubtag || !IsExtensionSubtag(languageTag.Slice(start, length))) return Bcp47ParseStatus.Invalid;

                    do
                    {
                        end = start + length;
                        hasSubtag = MoveToNextSubtag(languageTag, ref start, ref length);
                    } while (hasSubtag && IsExtensionSubtag(languageTag.Slice(start, length)));
                } while (hasSubtag && IsExtensionSingleton(languageTag.Slice(start, length)));

                result.Extensions = new Bcp47SubtagRange(firstStart, end - firstStart);
            }

            if (hasSubtag && IsPrivateUseSingleton(languageTag.Slice(start, length)))
            {
                // Private use runs to the end of the tag.
                if (!IsPrivateUse(languageTag.Slice(start))) return Bcp47ParseStatus.Invalid;

                result.PrivateUse = new Bcp47SubtagRange(start, languageTag.Length - start);
                hasSubtag = false;
            }

            if (hasSubtag) return Bcp47ParseStatus.Invalid;

            ranges = result;
            return hasRepeatedSubtag ? Bcp47ParseStatus.RepeatedSubtag : Bcp47ParseStatus.WellFormed;
        }

        /// <summary>
        /// Determine whether a string is one extension: a singleton other than <c>x</c> followed by one or more subtags of two to eight characters (ref. RFC 5646 §2.2.6).
        /// </summary>
        internal static bool IsExtension(ReadOnlySpan<char> extension)
        {
            var start = 0;
            var length = GetSubtagLength(extension, start);
            if (!IsExtensionSingleton(extension.Slice(start, length))) return false;

            var count = 0;
            while (MoveToNextSubtag(extension, ref start, ref length))
            {
                if (!IsExtensionSubtag(extension.Slice(start, length))) return false;
                count++;
            }

            return count > 0;
        }

        /// <summary>
        /// Determine whether a string is a private use sequence: <c>x</c> followed by one or more subtags of one to eight characters (ref. RFC 5646 §2.2.7).
        /// </summary>
        internal static bool IsPrivateUse(ReadOnlySpan<char> privateUse)
        {
            var start = 0;
            var length = GetSubtagLength(privateUse, start);
            if (!IsPrivateUseSingleton(privateUse.Slice(start, length))) return false;

            var count = 0;
            while (MoveToNextSubtag(privateUse, ref start, ref length))
            {
                if (length < 1 || length > 8 || !IsAsciiLettersOrDigits(privateUse.Slice(start, length))) return false;
                count++;
            }

            return count > 0;
        }

        /// <summary>
        /// Split a range of a language tag into its subtags.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static string[] GetSubtags(string languageTag, Bcp47SubtagRange range)
#else
        [NotNull]
        internal static string[] GetSubtags([NotNull] string languageTag, Bcp47SubtagRange range)
#endif
        {
            var subtags = new string[CountSeparators(languageTag, range) + 1];
            var start = range.Start;
            var end = range.Start + range.Length;
            for (var index = 0; index < subtags.Length; index++)
            {
                var length = GetSubtagLength(languageTag, start, end);
                subtags[index] = languageTag.Substring(start, length);
                start += length + 1;
            }

            return subtags;
        }

        /// <summary>
        /// Split the extensions range of a language tag into its extensions, each a singleton followed by its subtags.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static string[] GetExtensions(string languageTag, Bcp47SubtagRange range)
#else
        [NotNull]
        internal static string[] GetExtensions([NotNull] string languageTag, Bcp47SubtagRange range)
#endif
        {
            // Extension subtags have two to eight characters, so each one-character subtag starts an extension.
            var end = range.Start + range.Length;
            var count = 0;
            for (var start = range.Start; start < end; start += GetSubtagLength(languageTag, start, end) + 1)
            {
                if (GetSubtagLength(languageTag, start, end) == 1) count++;
            }

            var extensions = new string[count];
            var extensionStart = range.Start;
            var index = 0;
            for (var start = range.Start + 2; start < end; start += GetSubtagLength(languageTag, start, end) + 1)
            {
                if (GetSubtagLength(languageTag, start, end) != 1) continue;

                extensions[index++] = languageTag.Substring(extensionStart, start - 1 - extensionStart);
                extensionStart = start;
            }

            extensions[index] = languageTag.Substring(extensionStart, end - extensionStart);
            return extensions;
        }

        internal static bool IsAsciiDigit(char character)
        {
            return (uint)(character - '0') <= (uint)('9' - '0');
        }

        internal static bool IsAsciiLetter(char character)
        {
            return (uint)((character | 0x20) - 'a') <= (uint)('z' - 'a');
        }

        internal static bool IsAsciiLetters(ReadOnlySpan<char> value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                if (!IsAsciiLetter(value[index])) return false;
            }

            return true;
        }

        internal static bool IsAsciiLettersOrDigits(ReadOnlySpan<char> value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!IsAsciiLetter(character) && !IsAsciiDigit(character)) return false;
            }

            return true;
        }

        private static bool IsExtensionSingleton(ReadOnlySpan<char> subtag)
        {
            if (subtag.Length != 1) return false;

            var character = subtag[0];
            return IsAsciiDigit(character) || (IsAsciiLetter(character) && (character | 0x20) != 'x');
        }

        private static bool IsExtensionSubtag(ReadOnlySpan<char> subtag)
        {
            return subtag.Length >= 2 && subtag.Length <= 8 && IsAsciiLettersOrDigits(subtag);
        }

        private static bool IsPrivateUseSingleton(ReadOnlySpan<char> subtag)
        {
            return subtag.Length == 1 && (subtag[0] | 0x20) == 'x';
        }

        /// <summary>
        /// Index of a singleton among the 36 letters and digits, the same for either case of a letter.
        /// </summary>
        private static int GetSingletonIndex(char singleton)
        {
            return IsAsciiDigit(singleton) ? singleton - '0' : 10 + ((singleton | 0x20) - 'a');
        }

        /// <summary>
        /// Determine whether a run of letter and digit subtags contains a subtag, compared case-insensitively (ref. RFC 5646 §2.1.1).
        /// </summary>
        private static bool ContainsSubtag(ReadOnlySpan<char> subtags, ReadOnlySpan<char> subtag)
        {
            var start = 0;
            var length = GetSubtagLength(subtags, start);
            do
            {
                if (length == subtag.Length && EqualsIgnoreCase(subtags.Slice(start, length), subtag)) return true;
            } while (MoveToNextSubtag(subtags, ref start, ref length));

            return false;
        }

        /// <summary>
        /// Compare two runs of ASCII letters and digits case-insensitively.
        /// </summary>
        private static bool EqualsIgnoreCase(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
        {
            for (var index = 0; index < left.Length; index++)
            {
                // Setting 0x20 lower-cases an ASCII letter and leaves an ASCII digit unchanged.
                if ((left[index] | 0x20) != (right[index] | 0x20)) return false;
            }

            return true;
        }

        /// <summary>
        /// Move to the subtag after the current one.
        /// </summary>
        /// <returns><c>true</c> if there is a next subtag, which may be empty; <c>false</c> if the current subtag is the last.</returns>
        private static bool MoveToNextSubtag(ReadOnlySpan<char> value, ref int start, ref int length)
        {
            var end = start + length;
            if (end >= value.Length) return false;

            start = end + 1;
            length = GetSubtagLength(value, start);
            return true;
        }

        private static int GetSubtagLength(ReadOnlySpan<char> value, int start)
        {
            var end = start;
            while (end < value.Length && value[end] != SubtagSeparator) end++;

            return end - start;
        }

        private static int GetSubtagLength(string value, int start, int limit)
        {
            var end = start;
            while (end < limit && value[end] != SubtagSeparator) end++;

            return end - start;
        }

        private static int CountSeparators(string value, Bcp47SubtagRange range)
        {
            var count = 0;
            for (var index = range.Start; index < range.Start + range.Length; index++)
            {
                if (value[index] == SubtagSeparator) count++;
            }

            return count;
        }
    }
}
