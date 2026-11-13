using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The characters a field value may hold: RFC 4180 <c>TEXTDATA</c>, read as the printable characters of the character set the file is in.
    /// </summary>
    /// <remarks>
    /// RFC 4180§2 builds both escaped and non-escaped fields from <c>TEXTDATA = %x20-21 / %x23-2B / %x2D-7E</c>, the printable characters of
    /// US-ASCII, and RFC 4180§3 lets a file use another character set, named by the <c>charset</c> parameter of its media type.  The encoding of a
    /// reader or writer plays the part of that parameter, so a field may hold the printable characters of the encoding's character set.  Without
    /// an encoding, the character set is US-ASCII.
    /// <para>
    /// A control character, <c>%x00-1F</c>, <c>%x7F</c> or <c>%x80-9F</c>, isn't printable, so it is never allowed, whatever the character set.
    /// DQUOTE, the field delimiter and the terminator line break are left to the caller.
    /// </para>
    /// </remarks>
    internal sealed class CsvTextData
    {
        private const char FirstPrintableAscii = '\x20';
        private const char LastPrintableAscii = '\x7E';
        private const char LastAscii = '\x7F';
        private const char LastC1Control = '\x9F';

        // The name of US-ASCII.
        private const string AsciiWebName = "us-ascii";

        // Names of the encodings whose character set is all of Unicode: UTF-7, UTF-8, UTF-16 and UTF-32, in both byte orders.
        private static readonly string[] UnicodeWebNames = { "utf-7", "utf-8", "utf-16", "utf-16BE", "utf-32", "utf-32BE" };

        private static readonly CsvTextData AsciiTextData = new CsvTextData(CharacterSetKind.Ascii, null);
        private static readonly CsvTextData UnicodeTextData = new CsvTextData(CharacterSetKind.Unicode, null);

        private readonly CharacterSetKind _kind;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly Encoding? _encoding;
        private byte[]? _knownCharacters;
#else
        private readonly Encoding _encoding;
        private byte[] _knownCharacters;
#endif
        private readonly char[] _characterBuffer = new char[2];
        private readonly char[] _decodedBuffer = new char[4];

        private enum CharacterSetKind
        {
            Ascii,
            Unicode,
            Encoded
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvTextData(CharacterSetKind kind, Encoding? encoding)
#else
        private CsvTextData(CharacterSetKind kind, Encoding encoding)
#endif
        {
            _kind = kind;
            _encoding = encoding;
        }

        /// <summary>
        /// Indicates whether the character set has every character outside US-ASCII other than the control characters, as Unicode does.
        /// </summary>
        internal bool HasAllNonAscii => _kind == CharacterSetKind.Unicode;

        /// <summary>
        /// Gets the field characters of an encoding's character set, or of US-ASCII when there is no encoding.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static CsvTextData ForEncoding(Encoding? encoding)
#else
        internal static CsvTextData ForEncoding(Encoding encoding)
#endif
        {
            if (encoding is null || string.Equals(encoding.WebName, AsciiWebName, StringComparison.OrdinalIgnoreCase)) return AsciiTextData;
            foreach (var unicodeWebName in UnicodeWebNames)
            {
                if (string.Equals(encoding.WebName, unicodeWebName, StringComparison.OrdinalIgnoreCase)) return UnicodeTextData;
            }

            return new CsvTextData(CharacterSetKind.Encoded, encoding);
        }

        /// <summary>
        /// Gets the field characters of the character set a reader reads.
        /// </summary>
        /// <remarks>
        /// A <see cref="StreamReader"/> decodes its encoding, which, when the CSV reader creates it, is the one in the options or the default.  A
        /// <see cref="StringReader"/> reads a string, which is UTF-16.  Any other reader has no encoding, so the character set is US-ASCII.
        /// </remarks>
        internal static CsvTextData ForReader(TextReader reader)
        {
            if (reader is StreamReader streamReader) return ForEncoding(streamReader.CurrentEncoding);
            if (reader is StringReader) return UnicodeTextData;

            return AsciiTextData;
        }

        /// <summary>
        /// Indicates whether a field value may hold a character that isn't half of a surrogate pair, leaving aside DQUOTE and the characters that
        /// delimit fields and records.
        /// </summary>
        /// <remarks>
        /// COMMA is allowed here, as the field delimiter takes its place.
        /// </remarks>
        internal bool IsAllowed(char character)
        {
            if (character >= FirstPrintableAscii && character <= LastPrintableAscii) return true;
            if (character <= LastC1Control) return false;

            switch (_kind)
            {
                case CharacterSetKind.Unicode:
                    return true;
                case CharacterSetKind.Encoded:
                    return !char.IsSurrogate(character) && IsEncodable(character, character, 1);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Indicates whether a field value may hold the character that a surrogate pair stands for.
        /// </summary>
        internal bool IsAllowed(char highSurrogate, char lowSurrogate)
        {
            switch (_kind)
            {
                case CharacterSetKind.Unicode:
                    return true;
                case CharacterSetKind.Encoded:
                    return IsEncodable(highSurrogate, lowSurrogate, 2);
                default:
                    return false;
            }
        }

        private bool IsEncodable(char first, char second, int length)
        {
            // The answer for a single character is kept, so each is encoded at most once.  A surrogate pair's isn't, as there are too many.
            if (length == 1 && _knownCharacters != null && _knownCharacters[first] != 0) return _knownCharacters[first] == 1;

            var encoding = _encoding;
            if (encoding is null) return false;

            _characterBuffer[0] = first;
            _characterBuffer[1] = second;
            // A character the character set doesn't have is encoded as a replacement, such as '?', or a best fit, such as 'A' for U+0100, or it
            // throws, so it doesn't decode back to itself.  Every target can do this, unlike setting an exception fallback.
            bool isEncodable;
            try
            {
                var bytes = encoding.GetBytes(_characterBuffer, 0, length);
                isEncodable = encoding.GetCharCount(bytes, 0, bytes.Length) == length
                              && encoding.GetChars(bytes, 0, bytes.Length, _decodedBuffer, 0) == length
                              && _decodedBuffer[0] == first
                              && (length == 1 || _decodedBuffer[1] == second);
            }
            catch (ArgumentException)
            {
                // An encoder or decoder whose fallback throws throws an EncoderFallbackException or DecoderFallbackException, both of which are
                // ArgumentException.
                isEncodable = false;
            }

            if (length == 1)
            {
                if (_knownCharacters is null) _knownCharacters = new byte[char.MaxValue + 1];
                _knownCharacters[first] = isEncodable ? (byte)1 : (byte)2;
            }

            return isEncodable;
        }

        /// <summary>
        /// Gets the characters that an escaped field holds though they aren't in <c>TEXTDATA</c> or delimit fields and records: the field delimiter,
        /// which takes the place of COMMA, CR and LF, and the characters of the terminator line break, which takes the place of CRLF.
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
        /// Gets the index of the first character that has to be checked, or -1 if there is none: one that isn't printable US-ASCII, unless the
        /// character set has all of Unicode and it isn't a control character, and isn't one of <paramref name="exemptCharacters"/>.
        /// </summary>
        internal int IndexOfCharacterToCheck(ReadOnlySpan<char> value, char[] exemptCharacters)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character >= FirstPrintableAscii && character <= LastPrintableAscii) continue;
                if (character > LastC1Control && _kind == CharacterSetKind.Unicode) continue;
                if (Array.IndexOf(exemptCharacters, character) >= 0) continue;

                return index;
            }

            return -1;
        }
#else
        /// <summary>
        /// Gets the characters to search a value for, so that the search stops at each character that has to be checked, and at each of
        /// <paramref name="stopCharacters"/>.
        /// </summary>
        /// <param name="exemptCharacters">Characters the search passes over, though they aren't in <c>TEXTDATA</c>.</param>
        /// <param name="stopCharacters">Characters the search stops at, though they are in <c>TEXTDATA</c>, such as a quote.</param>
        /// <param name="searchExcept">
        /// <c>true</c> if the characters returned are those the search passes over, for an <c>IndexOfAnyExcept</c>; <c>false</c> if they are
        /// those it stops at, for an <c>IndexOfAny</c>.
        /// </param>
        /// <remarks>
        /// Unless the character set has all of Unicode, every character outside US-ASCII stops the search to be checked, so the set is the
        /// characters passed over, nearly all in US-ASCII.  With all of Unicode, the characters that stop the search are the control characters
        /// and <paramref name="stopCharacters"/>, so the set is those.
        /// </remarks>
        internal char[] GetSearchCharacters(char[] exemptCharacters, char[] stopCharacters, out bool searchExcept)
        {
            var characters = new List<char>();
            searchExcept = _kind != CharacterSetKind.Unicode;
            if (searchExcept)
            {
                for (var character = '\0'; character <= LastAscii; character++)
                {
                    var isPassedOver = (character >= FirstPrintableAscii && character <= LastPrintableAscii) || Array.IndexOf(exemptCharacters, character) >= 0;
                    if (isPassedOver && Array.IndexOf(stopCharacters, character) < 0) characters.Add(character);
                }

                foreach (var character in exemptCharacters)
                {
                    if (character > LastAscii && Array.IndexOf(stopCharacters, character) < 0 && !characters.Contains(character)) characters.Add(character);
                }

                return characters.ToArray();
            }

            for (var character = '\0'; character <= LastC1Control; character++)
            {
                var isControl = character < FirstPrintableAscii || character > LastPrintableAscii;
                if ((isControl && Array.IndexOf(exemptCharacters, character) < 0) || Array.IndexOf(stopCharacters, character) >= 0) characters.Add(character);
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
