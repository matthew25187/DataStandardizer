using System;
using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// Checks the bodies of the Subtag, Tag, Prefix, Suppress-Script, Macrolanguage and Preferred-Value fields of a Subtag Registry record against the
    /// forms that RFC 5646 gives them.
    /// </summary>
    internal static class SubtagRegistryValueFormat
    {
        /// <summary>
        /// Gets whether a Subtag field body is a subtag of the record's type, or a range of two of them (ref. RFC 5646 §3.1.1), in the case that
        /// RFC 5646 §3.1.4 gives that type: titlecase for a script, upper case for a region, and lower case for every other type.
        /// </summary>
        /// <remarks>
        /// The forms are those of RFC 5646 §2.2.1 to §2.2.5: a language subtag is 2 to 8 letters, an extended language subtag 3 letters, a script
        /// subtag 4 letters, a region subtag 2 letters or 3 digits, and a variant subtag 5 to 8 letters and digits, or a digit and 3 letters and digits.
        /// A range is two such subtags joined by "..", of the same length, with a letter or a digit in the same places, and the first not after the
        /// second.
        /// </remarks>
        internal static bool IsSubtag(SubtagRegistryRecordType type, string value)
        {
            var separatorIndex = value.IndexOf(SubtagRegistrySubtagRange.Separator, System.StringComparison.Ordinal);
            if (separatorIndex < 0) return IsSingleSubtag(type, value, 0, value.Length);

            var lastStart = separatorIndex + SubtagRegistrySubtagRange.Separator.Length;
            return IsSingleSubtag(type, value, 0, separatorIndex) && IsSingleSubtag(type, value, lastStart, value.Length - lastStart) &&
                SubtagRegistrySubtagRange.IsRange(value.Substring(0, separatorIndex), value.Substring(lastStart));
        }

        /// <summary>
        /// Gets whether a Tag field body is formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4): subtags of 1 to 8 letters and digits
        /// separated by hyphens (ref. RFC 5646 §2.1), in lower case, except that a subtag of letters alone that is neither the first nor after a
        /// singleton is in upper case when it has two letters and in titlecase when it has four.
        /// </summary>
        internal static bool IsTag(string value)
        {
            var start = 0;
            var isAfterSingleton = false;
            while (true)
            {
                var end = value.IndexOf('-', start);
                if (end < 0) end = value.Length;

                var length = end - start;
                if (length < 1 || length > 8) return false;

                // only a subtag of two or four letters has a capital (ref. RFC 5646 §2.1.1), so one with a digit is lower case
                var hasCapital = start != 0 && !isAfterSingleton && (length == 2 || length == 4) && IsLetters(value, start, length);
                for (var index = start; index < end; index++)
                {
                    var character = value[index];
                    if (IsDigit(character)) continue;
                    if (!IsLetter(character)) return false;

                    var isUpper = hasCapital && (length == 2 || index == start);
                    if (IsUpperLetter(character) != isUpper) return false;
                }

                isAfterSingleton |= length == 1;
                if (end == value.Length) return true;

                start = end + 1;
            }
        }

        /// <summary>
        /// Gets whether a Prefix field body is a language tag in the form that a valid one has (ref. RFC 5646 §3.1.2, §3.1.8): well-formed, and with no
        /// variant subtag or extension singleton that appears twice (ref. RFC 5646 §2.2.9), in any case (ref. RFC 5646 §2.1.1).
        /// </summary>
        /// <remarks>
        /// Whether the tag's subtags appear in the registry, the other condition of validity (ref. RFC 5646 §2.2.9), is not checked here, as the records
        /// that hold them may come later in the registry.
        /// </remarks>
        internal static bool IsPrefix(string value)
        {
            return Bcp47LanguageTagParser.Parse(value.AsSpan(), Bcp47SyntaxSubtagClassifier.Instance, out _) == Bcp47ParseStatus.Accepted;
        }

        /// <summary>
        /// Gets whether a Suppress-Script field body is a script subtag (ref. RFC 5646 §3.1.2, §3.1.9): four letters (ref. RFC 5646 §2.2.3), in any
        /// case (ref. RFC 5646 §2.1.1).
        /// </summary>
        internal static bool IsScriptSubtag(string value)
        {
            return value.Length == 4 && IsLetters(value, 0, value.Length);
        }

        /// <summary>
        /// Gets whether a Macrolanguage field body is a primary language subtag (ref. RFC 5646 §3.1.2, §3.1.10): two to eight letters
        /// (ref. RFC 5646 §2.2.1), in any case (ref. RFC 5646 §2.1.1).
        /// </summary>
        internal static bool IsPrimaryLanguageSubtag(string value)
        {
            return value.Length >= 2 && value.Length <= 8 && IsLetters(value, 0, value.Length);
        }

        /// <summary>
        /// Gets whether a Preferred-Value field body has the form that RFC 5646 §3.1.2 gives it for the record's type, in any case
        /// (ref. RFC 5646 §2.1.1): a primary language subtag for a language record, a subtag of the same type for a script, region or variant record
        /// (ref. RFC 5646 §2.2.3 to §2.2.5), and an extended language range (ref. RFC 4647 §2.2) for a grandfathered or redundant record.
        /// </summary>
        /// <remarks>
        /// An extlang record's Preferred-Value is identical to its Subtag (ref. RFC 5646 §2.2.2), which the reader checks instead.
        /// </remarks>
        internal static bool IsPreferredValue(SubtagRegistryRecordType type, string value)
        {
            var length = value.Length;
            switch (type)
            {
                case SubtagRegistryRecordType.Language:
                    return IsPrimaryLanguageSubtag(value);
                case SubtagRegistryRecordType.Script:
                    return IsScriptSubtag(value);
                case SubtagRegistryRecordType.Region:
                    return length == 2 && IsLetters(value, 0, length) || length == 3 && IsDigits(value, 0, length);
                case SubtagRegistryRecordType.Variant:
                    return (length >= 5 && length <= 8 || length == 4 && IsDigit(value[0])) && IsLettersOrDigits(value, 0, length);
                case SubtagRegistryRecordType.Grandfathered:
                case SubtagRegistryRecordType.Redundant:
                    return IsExtendedLanguageRange(value);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Gets whether a value is an extended language range (ref. RFC 4647 §2.2):
        /// <c>(1*8ALPHA / "*") *("-" (1*8alphanum / "*"))</c>.
        /// </summary>
        private static bool IsExtendedLanguageRange(string value)
        {
            var start = 0;
            while (true)
            {
                var end = value.IndexOf('-', start);
                if (end < 0) end = value.Length;

                var length = end - start;
                var isWildcard = length == 1 && value[start] == '*';
                var isSubtag = length >= 1 && length <= 8 && (start == 0 ? IsLetters(value, start, length) : IsLettersOrDigits(value, start, length));
                if (!isWildcard && !isSubtag) return false;
                if (end == value.Length) return true;

                start = end + 1;
            }
        }

        private static bool IsSingleSubtag(SubtagRegistryRecordType type, string value, int start, int length)
        {
            switch (type)
            {
                case SubtagRegistryRecordType.Language:
                    return length >= 2 && length <= 8 && IsLowerLetters(value, start, length);
                case SubtagRegistryRecordType.ExtendedLanguage:
                    return length == 3 && IsLowerLetters(value, start, length);
                case SubtagRegistryRecordType.Script:
                    return length == 4 && IsUpperLetter(value[start]) && IsLowerLetters(value, start + 1, length - 1);
                case SubtagRegistryRecordType.Region:
                    return length == 2 && IsUpperLetter(value[start]) && IsUpperLetter(value[start + 1]) ||
                        length == 3 && IsDigit(value[start]) && IsDigit(value[start + 1]) && IsDigit(value[start + 2]);
                case SubtagRegistryRecordType.Variant:
                    return (length >= 5 && length <= 8 || length == 4 && IsDigit(value[start])) && IsLowerLettersOrDigits(value, start, length);
                default:
                    return false;
            }
        }

        private static bool IsLowerLetters(string value, int start, int length)
        {
            for (var index = start; index < start + length; index++)
            {
                if (!IsLowerLetter(value[index])) return false;
            }

            return true;
        }

        private static bool IsLetters(string value, int start, int length)
        {
            for (var index = start; index < start + length; index++)
            {
                if (!IsLetter(value[index])) return false;
            }

            return true;
        }

        private static bool IsLettersOrDigits(string value, int start, int length)
        {
            for (var index = start; index < start + length; index++)
            {
                if (!IsLetter(value[index]) && !IsDigit(value[index])) return false;
            }

            return true;
        }

        private static bool IsDigits(string value, int start, int length)
        {
            for (var index = start; index < start + length; index++)
            {
                if (!IsDigit(value[index])) return false;
            }

            return true;
        }

        private static bool IsLowerLettersOrDigits(string value, int start, int length)
        {
            for (var index = start; index < start + length; index++)
            {
                if (!IsLowerLetter(value[index]) && !IsDigit(value[index])) return false;
            }

            return true;
        }

        private static bool IsLetter(char character) => IsLowerLetter(character) || IsUpperLetter(character);

        private static bool IsLowerLetter(char character) => character >= 'a' && character <= 'z';

        private static bool IsUpperLetter(char character) => character >= 'A' && character <= 'Z';

        private static bool IsDigit(char character) => character >= '0' && character <= '9';
    }
}
