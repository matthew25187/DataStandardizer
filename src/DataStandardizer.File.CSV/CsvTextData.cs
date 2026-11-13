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
        private const char FirstC1Control = '\x80';
        private const char LastC1Control = '\x9F';
        private const int PageSize = 256;

        // The name of US-ASCII.
        private const string AsciiWebName = "us-ascii";

        // Names of the encodings whose character set is all of Unicode: UTF-7, UTF-8, UTF-16 and UTF-32, in both byte orders.
        private static readonly string[] UnicodeWebNames = { "utf-7", "utf-8", "utf-16", "utf-16BE", "utf-32", "utf-32BE" };

        private static readonly char[] NoCharacters = new char[0];
        private static readonly CsvTextData AsciiTextData = new CsvTextData(CharacterSetKind.Ascii, null, AsciiWebName);
        private static readonly CsvTextData[] UnicodeTextData = CreateUnicodeTextData();

        // A string is UTF-16.
        private static readonly CsvTextData StringTextData = UnicodeTextData[Array.IndexOf(UnicodeWebNames, "utf-16")];

        private readonly CharacterSetKind _kind;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly Encoding? _encoding;
#else
        private readonly Encoding _encoding;
#endif
        private readonly char[] _missingPrintableAscii = NoCharacters;
        private readonly byte[][] _knownCharacterPages = new byte[(char.MaxValue + 1) / PageSize][];
        private readonly char[] _characterBuffer = new char[2];
        private readonly char[] _decodedBuffer = new char[4];

        private enum CharacterSetKind
        {
            Ascii,
            Unicode,
            Encoded
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvTextData(CharacterSetKind kind, Encoding? encoding, string name)
#else
        private CsvTextData(CharacterSetKind kind, Encoding encoding, string name)
#endif
        {
            _kind = kind;
            _encoding = encoding;
            Name = name;

            if (kind == CharacterSetKind.Encoded)
            {
                // Nearly every character set has all of printable US-ASCII, which lets the searches pass over it without checking each character.
                // A national variant of ISO 646, such as one without '[' or '@', doesn't, so the characters it lacks are checked like any other.
                var missingPrintableAscii = new List<char>();
                for (var character = FirstPrintableAscii; character <= LastPrintableAscii; character++)
                {
                    if (!IsEncodable(character, character, 1)) missingPrintableAscii.Add(character);
                }

                _missingPrintableAscii = missingPrintableAscii.ToArray();
            }
        }

        /// <summary>
        /// Gets the name of the character set, for messages: the encoding's web name, or its type name if it has none.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the field characters of an encoding's character set, or of US-ASCII when there is no encoding.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static CsvTextData ForEncoding(Encoding? encoding)
#else
        internal static CsvTextData ForEncoding(Encoding encoding)
#endif
        {
            if (encoding is null) return AsciiTextData;

            // An encoding of a code page that .NET doesn't know, such as a custom Encoding class, throws for its web name.  Its character set is
            // found by encoding each character, as for any other encoding that isn't US-ASCII or Unicode.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? webName;
#else
            string webName;
#endif
            try
            {
                webName = encoding.WebName;
            }
            catch (NotSupportedException)
            {
                webName = null;
            }

            if (webName != null)
            {
                if (string.Equals(webName, AsciiWebName, StringComparison.OrdinalIgnoreCase)) return AsciiTextData;
                for (var index = 0; index < UnicodeWebNames.Length; index++)
                {
                    if (string.Equals(webName, UnicodeWebNames[index], StringComparison.OrdinalIgnoreCase)) return UnicodeTextData[index];
                }
            }

            return new CsvTextData(CharacterSetKind.Encoded, encoding, webName ?? encoding.GetType().Name);
        }

        private static CsvTextData[] CreateUnicodeTextData()
        {
            var textData = new CsvTextData[UnicodeWebNames.Length];
            for (var index = 0; index < textData.Length; index++)
            {
                textData[index] = new CsvTextData(CharacterSetKind.Unicode, null, UnicodeWebNames[index]);
            }

            return textData;
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
            if (reader is StringReader) return StringTextData;

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
            if (character >= FirstPrintableAscii && character <= LastPrintableAscii)
            {
                return _missingPrintableAscii.Length == 0 || Array.IndexOf(_missingPrintableAscii, character) < 0;
            }

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

        /// <summary>
        /// Indicates whether the character set has a character, printable or not, such as a character of the field delimiter or terminator line
        /// break, which a writer writes as it is.
        /// </summary>
        internal bool IsInCharacterSet(char character)
        {
            switch (_kind)
            {
                case CharacterSetKind.Unicode:
                    return !char.IsSurrogate(character);
                case CharacterSetKind.Encoded:
                    return !char.IsSurrogate(character) && IsEncodable(character, character, 1);
                default:
                    return character <= LastAscii;
            }
        }

        private bool IsEncodable(char first, char second, int length)
        {
            // The answer for a single character is kept, in pages that are only allocated once a character in them is checked, so each character is
            // encoded at most once.  A surrogate pair's answer isn't kept, as there are too many.
            var page = length == 1 ? _knownCharacterPages[first / PageSize] : null;
            if (page != null && page[first % PageSize] != 0) return page[first % PageSize] == 1;

            var encoding = _encoding;
            if (encoding is null) return false;

            // A character the character set doesn't have is encoded as a replacement, such as '?', or a best fit, such as 'A' for U+0100, or it
            // throws, so it doesn't decode back to itself.  Every target can do this, unlike setting an exception fallback.
            _characterBuffer[0] = first;
            _characterBuffer[1] = second;
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
                if (page is null)
                {
                    page = new byte[PageSize];
                    _knownCharacterPages[first / PageSize] = page;
                }

                page[first % PageSize] = isEncodable ? (byte)1 : (byte)2;
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
        /// Gets the index of the first character that has to be checked, or -1 if there is none: one that isn't printable US-ASCII the character
        /// set has, unless the character set has all of Unicode and it isn't a control character, and isn't one of <paramref name="exemptCharacters"/>.
        /// </summary>
        internal int IndexOfCharacterToCheck(ReadOnlySpan<char> value, char[] exemptCharacters)
        {
            var hasAllPrintableAscii = _missingPrintableAscii.Length == 0;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character >= FirstPrintableAscii && character <= LastPrintableAscii
                    && (hasAllPrintableAscii || Array.IndexOf(_missingPrintableAscii, character) < 0)) continue;
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
        /// <param name="searchC1Separately">
        /// <c>true</c> if the characters returned leave out the C1 control characters, <c>%x80-9F</c>, which the caller then searches for with
        /// <c>IndexOfAnyInRange</c> up to where the search stopped.
        /// </param>
        /// <remarks>
        /// Unless the character set has all of Unicode, every character outside US-ASCII stops the search to be checked, so the set is the
        /// characters passed over, nearly all in US-ASCII.  With all of Unicode, the characters that stop the search are the control characters
        /// and <paramref name="stopCharacters"/>.  A set of US-ASCII characters is searched with vector instructions, and one with a character
        /// above <c>%x7F</c> isn't, so the C1 controls are left out of it and searched for as a range, unless another character of the set is above
        /// <c>%x7F</c> anyway or one of them is exempt.
        /// </remarks>
        internal char[] GetSearchCharacters(char[] exemptCharacters, char[] stopCharacters, out bool searchExcept, out bool searchC1Separately)
        {
            var characters = new List<char>();
            searchExcept = _kind != CharacterSetKind.Unicode;
            searchC1Separately = false;
            if (searchExcept)
            {
                for (var character = '\0'; character <= LastAscii; character++)
                {
                    var isPrintable = character >= FirstPrintableAscii && character <= LastPrintableAscii
                                      && Array.IndexOf(_missingPrintableAscii, character) < 0;
                    var isPassedOver = isPrintable || Array.IndexOf(exemptCharacters, character) >= 0;
                    if (isPassedOver && Array.IndexOf(stopCharacters, character) < 0) characters.Add(character);
                }

                foreach (var character in exemptCharacters)
                {
                    if (character > LastAscii && Array.IndexOf(stopCharacters, character) < 0 && !characters.Contains(character)) characters.Add(character);
                }

                return characters.ToArray();
            }

            var hasNonAsciiStop = false;
            foreach (var character in stopCharacters)
            {
                if (character > LastAscii) hasNonAsciiStop = true;
            }

            var hasExemptC1 = false;
            foreach (var character in exemptCharacters)
            {
                if (character >= FirstC1Control && character <= LastC1Control) hasExemptC1 = true;
            }

            searchC1Separately = !hasNonAsciiStop && !hasExemptC1;
            var lastControl = searchC1Separately ? LastAscii : LastC1Control;
            for (var character = '\0'; character <= lastControl; character++)
            {
                var isControl = character < FirstPrintableAscii || character > LastPrintableAscii;
                if ((isControl && Array.IndexOf(exemptCharacters, character) < 0) || Array.IndexOf(stopCharacters, character) >= 0) characters.Add(character);
            }

            foreach (var character in stopCharacters)
            {
                if (character > lastControl && !characters.Contains(character)) characters.Add(character);
            }

            return characters.ToArray();
        }

        /// <summary>
        /// Gets the index of the first C1 control character, <c>%x80-9F</c>, in a value, or -1 if there is none.
        /// </summary>
        internal static int IndexOfC1Control(ReadOnlySpan<char> value) => value.IndexOfAnyInRange(FirstC1Control, LastC1Control);
#endif
    }
}
