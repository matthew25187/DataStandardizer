using System;
using System.Collections.Generic;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Decides which characters a field value may hold, from RFC 4180 <c>TEXTDATA</c> and the <see cref="CsvExtendedCharacters"/> the options allow.
    /// </summary>
    internal static class CsvTextData
    {
        private const char FirstPrintableAscii = '\x20';
        private const char LastPrintableAscii = '\x7E';
        private const char Delete = '\x7F';
        private const char LastC1Control = '\x9F';

        /// <summary>
        /// Indicates whether a field value may hold a character, leaving aside the characters that delimit fields and records.
        /// </summary>
        /// <remarks>
        /// <c>TEXTDATA</c> is the printable characters of US-ASCII, <c>%x20-7E</c>, less DQUOTE and COMMA (RFC 4180§2).  DQUOTE, the field delimiter
        /// and the terminator line break, which take the place of COMMA and CRLF, are left to the caller, so every other printable character of
        /// US-ASCII, COMMA included, is allowed here.  A control character needs <see cref="CsvExtendedCharacters.Control"/>, a character outside
        /// US-ASCII needs <see cref="CsvExtendedCharacters.NonAscii"/>, and a control character outside US-ASCII, <c>%x80-9F</c>, needs both.
        /// </remarks>
        internal static bool IsAllowed(char character, CsvExtendedCharacters extendedCharacters)
        {
            if (character >= FirstPrintableAscii && character <= LastPrintableAscii) return true;
            if (character < FirstPrintableAscii || character == Delete) return (extendedCharacters & CsvExtendedCharacters.Control) != 0;
            if (character <= LastC1Control) return AllowsAll(extendedCharacters);

            return (extendedCharacters & CsvExtendedCharacters.NonAscii) != 0;
        }

        /// <summary>
        /// Indicates whether every character is allowed: both <see cref="CsvExtendedCharacters.Control"/> and
        /// <see cref="CsvExtendedCharacters.NonAscii"/> are set, whatever other bits are.
        /// </summary>
        internal static bool AllowsAll(CsvExtendedCharacters extendedCharacters)
        {
            const CsvExtendedCharacters ControlAndNonAscii = CsvExtendedCharacters.Control | CsvExtendedCharacters.NonAscii;
            return (extendedCharacters & ControlAndNonAscii) == ControlAndNonAscii;
        }

        /// <summary>
        /// Gets the characters that an escaped field holds whatever the options, though they aren't in TEXTDATA or delimit fields and records: the
        /// field delimiter, which takes the place of COMMA, CR and LF, and the characters of the terminator line break, which takes the place of
        /// CRLF.
        /// </summary>
        /// <remarks>ref. RFC 4180§2 (escaped)</remarks>
        internal static char[] GetEscapedFieldCharacters(char fieldDelimiter, string terminator)
        {
            var characters = new List<char> { fieldDelimiter, '\r', '\n' };
            foreach (var character in terminator)
            {
                if (!characters.Contains(character)) characters.Add(character);
            }

            return characters.ToArray();
        }

#if !NET8_0_OR_GREATER
        /// <summary>
        /// Gets the index of the first character that a field value may not hold, or -1 if it may hold them all.
        /// </summary>
        /// <param name="value">The characters to check.</param>
        /// <param name="extendedCharacters">The kinds of character outside <c>TEXTDATA</c> that are allowed.</param>
        /// <param name="exemptCharacters">Characters allowed whatever <paramref name="extendedCharacters"/> is, such as those an escaped field may hold.</param>
        internal static int IndexOfDisallowed(ReadOnlySpan<char> value, CsvExtendedCharacters extendedCharacters, char[] exemptCharacters)
        {
            if (AllowsAll(extendedCharacters)) return -1;

            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!IsAllowed(character, extendedCharacters) && Array.IndexOf(exemptCharacters, character) < 0) return index;
            }

            return -1;
        }
#else
        /// <summary>
        /// Gets the characters to search a value for, so that the search stops at the first character a field value may not hold, or at one of
        /// <paramref name="stopCharacters"/>.
        /// </summary>
        /// <param name="extendedCharacters">The kinds of character outside <c>TEXTDATA</c> that are allowed.</param>
        /// <param name="exemptCharacters">Characters allowed whatever <paramref name="extendedCharacters"/> is.</param>
        /// <param name="stopCharacters">Characters the search stops at though a field value may hold them, such as a quote.</param>
        /// <param name="searchExcept">
        /// <c>true</c> if the characters returned are those the search passes over, for an <c>IndexOfAnyExcept</c>; <c>false</c> if they are
        /// those it stops at, for an <c>IndexOfAny</c>.
        /// </param>
        /// <remarks>
        /// Without <see cref="CsvExtendedCharacters.NonAscii"/>, every character outside US-ASCII stops the search, so the set is the characters
        /// passed over, all of them in US-ASCII.  With it, the characters that stop the search are all below <c>%xA0</c>, so the set is those.
        /// </remarks>
        internal static char[] GetSearchCharacters(CsvExtendedCharacters extendedCharacters, char[] exemptCharacters, char[] stopCharacters, out bool searchExcept)
        {
            var characters = new List<char>();
            searchExcept = (extendedCharacters & CsvExtendedCharacters.NonAscii) == 0;
            if (searchExcept)
            {
                for (var character = '\0'; character <= Delete; character++)
                {
                    if ((IsAllowed(character, extendedCharacters) || Array.IndexOf(exemptCharacters, character) >= 0)
                        && Array.IndexOf(stopCharacters, character) < 0)
                    {
                        characters.Add(character);
                    }
                }

                foreach (var character in exemptCharacters)
                {
                    if (character > Delete && Array.IndexOf(stopCharacters, character) < 0 && !characters.Contains(character)) characters.Add(character);
                }

                return characters.ToArray();
            }

            for (var character = '\0'; character <= LastC1Control; character++)
            {
                if ((!IsAllowed(character, extendedCharacters) && Array.IndexOf(exemptCharacters, character) < 0)
                    || Array.IndexOf(stopCharacters, character) >= 0)
                {
                    characters.Add(character);
                }
            }

            foreach (var character in stopCharacters)
            {
                if (character > LastC1Control && !characters.Contains(character)) characters.Add(character);
            }

            return characters.ToArray();
        }
#endif
    }
}
