namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// Checks the bodies of the Subtag and Tag fields of a Subtag Registry record against the forms that RFC 5646 gives them.
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
        /// separated by hyphens (ref. RFC 5646 §2.1), in lower case, except that a subtag that is neither the first nor after a singleton is in upper
        /// case when it has two letters and in titlecase when it has four.
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

                var isFirst = start == 0;
                for (var index = start; index < end; index++)
                {
                    var character = value[index];
                    if (IsDigit(character)) continue;
                    if (!IsLetter(character)) return false;

                    var isUpper = !isFirst && !isAfterSingleton && (length == 2 || length == 4 && index == start);
                    if (IsUpperLetter(character) != isUpper) return false;
                }

                isAfterSingleton |= length == 1;
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
