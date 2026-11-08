namespace DataStandardizer.Geography
{
    /// <summary>
    /// Compares ISO 639 language codes.
    /// </summary>
    internal static class Iso639LanguageCode
    {
        /// <summary>
        /// Determine whether a language code given by a caller matches a code from a code list.
        /// </summary>
        /// <param name="languageCode">The language code given by the caller, which may be <c>null</c>.</param>
        /// <param name="code">The code from the code list, which may be <c>null</c>.</param>
        /// <returns><c>true</c> if both are present and are the same apart from the case of their ASCII letters; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// ref. RFC 5646 §2.1.1: language codes are case-insensitive.  ISO 639 codes are made of the ASCII letters a-z (RFC 5646 §2.1, ALPHA
        /// in RFC 5234 Appendix B.1), so only ASCII letters are folded, and no other character matches one of them, whatever the target.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        internal static bool Matches(string? languageCode, string? code)
#else
        internal static bool Matches(string languageCode, string code)
#endif
        {
            if (languageCode is null || code is null || languageCode.Length != code.Length) return false;

            for (var index = 0; index < code.Length; index++)
            {
                var left = languageCode[index];
                var right = code[index];
                if (left != right && !(IsAsciiLetter(left) && (left | 0x20) == (right | 0x20))) return false;
            }

            return true;
        }

        private static bool IsAsciiLetter(char value)
        {
            return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
        }
    }
}
