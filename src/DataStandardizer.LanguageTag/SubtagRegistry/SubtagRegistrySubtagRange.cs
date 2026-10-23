using System;
using System.Collections.Generic;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// Expands the <c>Subtag</c> field of a registry record that denotes a range, such as <c>qaa..qtz</c>, into the subtags it stands for (RFC 5646 §3.1.1, §3.1.4).
    /// </summary>
    internal static class SubtagRegistrySubtagRange
    {
        internal const string Separator = "..";

        /// <summary>
        /// Returns every subtag in the range <paramref name="subtag"/> denotes, in order and in the case of its first subtag, or <paramref name="subtag"/> itself when it is not a range.
        /// </summary>
        /// <remarks>
        /// A range is two subtags of the same length joined by <see cref="Separator"/>, each character of which is a letter or digit, with the first not after the second when compared case-insensitively.
        /// Each position runs through the letters or digits independently, so <c>Qaaa..Qabx</c> denotes <c>Qaaa</c> to <c>Qaaz</c> followed by <c>Qaba</c> to <c>Qabx</c>.
        /// A value that does not have this form is returned unchanged.
        /// </remarks>
        internal static IEnumerable<string> Expand(string subtag)
        {
            var separatorIndex = subtag.IndexOf(Separator, StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                return new[] { subtag };
            }

            var first = subtag.Substring(0, separatorIndex);
            var last = subtag.Substring(separatorIndex + Separator.Length);
            if (!IsRange(first, last))
            {
                return new[] { subtag };
            }

            return ExpandRange(first, last);
        }

        private static bool IsRange(string first, string last)
        {
            if (first.Length == 0 || first.Length != last.Length)
            {
                return false;
            }

            for (var index = 0; index < first.Length; index++)
            {
                var firstCharacter = char.ToLowerInvariant(first[index]);
                var lastCharacter = char.ToLowerInvariant(last[index]);
                if (!IsAsciiLetter(firstCharacter) && !IsAsciiDigit(firstCharacter)
                    || IsAsciiLetter(firstCharacter) != IsAsciiLetter(lastCharacter)
                    || IsAsciiDigit(firstCharacter) != IsAsciiDigit(lastCharacter))
                {
                    return false;
                }
            }

            return string.Compare(first, last, StringComparison.OrdinalIgnoreCase) <= 0;
        }

        private static IEnumerable<string> ExpandRange(string first, string last)
        {
            var current = first.ToCharArray();
            while (true)
            {
                var subtag = new string(current);
                yield return subtag;
                if (string.Equals(subtag, last, StringComparison.OrdinalIgnoreCase))
                {
                    yield break;
                }

                for (var index = current.Length - 1; index >= 0; index--)
                {
                    if (current[index] != GetHighest(current[index]))
                    {
                        current[index]++;
                        break;
                    }

                    current[index] = GetLowest(current[index]);
                }
            }
        }

        private static char GetLowest(char character) => character >= 'a' ? 'a' : character >= 'A' ? 'A' : '0';

        private static char GetHighest(char character) => character >= 'a' ? 'z' : character >= 'A' ? 'Z' : '9';

        private static bool IsAsciiLetter(char character) => character >= 'a' && character <= 'z';

        private static bool IsAsciiDigit(char character) => character >= '0' && character <= '9';
    }
}
