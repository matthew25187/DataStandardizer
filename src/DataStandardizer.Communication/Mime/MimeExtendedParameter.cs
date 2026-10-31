using System;
using System.Collections.Generic;
using System.Text;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// Reads and writes RFC 2231 parameter value continuations and extended values.
    /// </summary>
    internal static class MimeExtendedParameter
    {
        // Enough for any section number that fits in an int, with room to spare.
        private const int MaxSectionDigits = 9;

        /// <summary>
        /// A <c>name=value</c> pair as written, before RFC 2231 sections are joined.
        /// </summary>
        internal struct RawParameter
        {
            /// <summary>The name without any RFC 2231 section number or <c>*</c>.</summary>
            internal string Name;

            /// <summary>The section number; or -1 for a parameter that is not split, such as <c>name</c> or <c>name*</c>.</summary>
            internal int Section;

            /// <summary>Whether the name ends with <c>*</c>, so the value is percent-encoded.</summary>
            internal bool IsExtended;

            /// <summary>Whether the name has an RFC 2231 form: <c>name*</c>, <c>name*N</c> or <c>name*N*</c>.</summary>
            internal bool IsRfc2231;

            /// <summary>Whether the section number has a leading zero, such as <c>name*01</c>.</summary>
            internal bool HasLeadingZero;

            /// <summary>The value, with any quoting removed.</summary>
            internal string Value;

            /// <summary>Whether the value was a quoted-string.</summary>
            internal bool IsQuoted;
        }

        /// <summary>
        /// Split a parameter name into its base name and its RFC 2231 section number and <c>*</c> (RFC 2231 §3, §4).
        /// </summary>
        /// <param name="name">The parameter name as written.</param>
        /// <param name="baseNameLength">The length of the base name at the start of <paramref name="name"/>.</param>
        /// <param name="parameter">The parameter, whose section number and flags are set.</param>
        /// <returns><c>false</c> if the section number is too large.</returns>
        /// <remarks>
        /// A name that has a <c>*</c> but is not one of the RFC 2231 forms, such as <c>a*b</c>, is returned whole as a plain name.
        /// </remarks>
        internal static bool TrySplitName(ReadOnlySpan<char> name, out int baseNameLength, ref RawParameter parameter)
        {
            baseNameLength = name.Length;
            parameter.Section = -1;

            var star = name.IndexOf('*');
            if (star < 0) return true;

            var rest = name.Slice(star + 1);
            if (rest.IsEmpty)
            {
                // ref. RFC 2231 §4: an asterisk at the end of a parameter name acts as an indicator that character set and language
                // information may appear at the beginning of the parameter value.
                baseNameLength = star;
                parameter.IsExtended = true;
                parameter.IsRfc2231 = true;
                return true;
            }

            var isExtended = rest[rest.Length - 1] == '*';
            var digits = isExtended ? rest.Slice(0, rest.Length - 1) : rest;
            if (digits.IsEmpty) return true;

            foreach (var c in digits)
            {
                if (c < '0' || c > '9') return true;
            }

            // ref. RFC 2231 §3: section := "0" / [1-9] *DIGIT
            parameter.HasLeadingZero = digits.Length > 1 && digits[0] == '0';
            while (digits.Length > 1 && digits[0] == '0')
            {
                digits = digits.Slice(1);
            }

            if (digits.Length > MaxSectionDigits) return false;

            var section = 0;
            foreach (var c in digits)
            {
                section = section * 10 + (c - '0');
            }

            baseNameLength = star;
            parameter.Section = section;
            parameter.IsExtended = isExtended;
            parameter.IsRfc2231 = true;
            return true;
        }

        /// <summary>
        /// Join the RFC 2231 forms of one parameter into a parameter, decoding any extended value.
        /// </summary>
        /// <param name="forms">The forms of the parameter, in the order they were written. All have the same base name.</param>
        /// <param name="options">The parse options.</param>
        /// <param name="charsetResolver">The resolver for the character set of an extended value.</param>
        /// <param name="parameter">The joined parameter.</param>
        /// <returns><c>false</c> if the forms are not valid.</returns>
        internal static bool TryJoin(List<RawParameter> forms, MimeMediaTypeParseOptions options, IMimeCharsetResolver charsetResolver,
            out MimeParameter parameter)
        {
            parameter = default(MimeParameter);

            List<RawParameter> sections;
            if (forms[0].Section < 0)
            {
                // A name* parameter can't also be split into sections, and it can't be repeated (ref. RFC 6838 §4.3).
                if (forms.Count > 1) return false;

                sections = forms;
            }
            else
            {
                foreach (var form in forms)
                {
                    if (form.Section < 0) return false;
                }

                if (!TryOrderSections(forms, options, out sections)) return false;
            }

            var name = sections[0].Name;
            var anyExtended = false;
            var allowQuoted = (options & MimeMediaTypeParseOptions.AllowQuotedExtendedValues) != 0;
            var allowLowercase = (options & MimeMediaTypeParseOptions.AllowLowercaseHexDigits) != 0;
            foreach (var section in sections)
            {
                if (!section.IsExtended) continue;
                if (section.IsQuoted && !allowQuoted) return false;

                anyExtended = true;
            }

            if (!anyExtended)
            {
                // ref. RFC 2231 §3: the sections are simply concatenated.
                var builder = new StringBuilder();
                foreach (var section in sections)
                {
                    builder.Append(section.Value);
                }

                parameter = MimeParameter.CreateUnchecked(name, builder.ToString(), null, null);
                return true;
            }

            // ref. RFC 2231 §4.1: the character set and language are given only by the initial section, and the octets of all the sections
            // are joined before they are decoded, so a character may span sections.
            var charsetName = string.Empty;
            var language = string.Empty;
            var byteCount = 0;
            foreach (var section in sections)
            {
                byteCount += section.Value.Length;
            }

            var bytes = new byte[byteCount];
            byteCount = 0;
            for (var index = 0; index < sections.Count; index++)
            {
                var section = sections[index];
                var text = section.Value;
                if (!section.IsExtended)
                {
                    // A value that is not extended is printable US-ASCII.
                    foreach (var c in text)
                    {
                        bytes[byteCount++] = (byte)c;
                    }

                    continue;
                }

                var start = 0;
                if (index == 0 && section.Section <= 0 && !TryReadCharsetAndLanguage(text, ref start, out charsetName, out language)) return false;
                if (!TryDecodeOctets(text, start, section.IsQuoted, allowLowercase, bytes, ref byteCount)) return false;
            }

            string value;
            if (!TryDecodeText(bytes, byteCount, charsetName, options, charsetResolver, out value)) return false;

            parameter = MimeParameter.CreateUnchecked(name, value, charsetName.Length == 0 ? null : charsetName, language.Length == 0 ? null : language);
            return true;
        }

        /// <summary>
        /// Get the number of characters a value takes when written as an RFC 2231 extended value, without its character set and language.
        /// </summary>
        internal static int GetEncodedLength(string value)
        {
            var length = 0;
            for (var index = 0; index < value.Length;)
            {
                int charCount;
                length += GetEncodedUnitLength(value, index, out charCount);
                index += charCount;
            }

            return length;
        }

        /// <summary>
        /// Get the number of characters that one character, or one surrogate pair, of a value takes when written as UTF-8 in an RFC 2231
        /// extended value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="index">The position of the character.</param>
        /// <param name="charCount">The number of characters read: 2 for a surrogate pair, otherwise 1.</param>
        /// <remarks>
        /// An unpaired surrogate is written as U+FFFD, as UTF-8 can't hold it.
        /// </remarks>
        internal static int GetEncodedUnitLength(string value, int index, out int charCount)
        {
            var c = value[index];
            charCount = 1;
            if (c < '\x80') return IsAttributeChar(c) ? 1 : 3;
            if (c < '\x800') return 6;
            if (!IsSurrogatePair(value, index)) return 9;

            charCount = 2;
            return 12;
        }

        /// <summary>
        /// Write one character, or one surrogate pair, of a value as percent-encoded UTF-8, unless it is an attribute-char.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="index">The position of the character.</param>
        /// <param name="destination">The span to write to, which must have room for the unit.</param>
        /// <param name="position">The position to write at; moved past what was written.</param>
        /// <returns>The number of characters read: 2 for a surrogate pair, otherwise 1.</returns>
        /// <remarks>
        /// An unpaired surrogate is written as U+FFFD, as UTF-8 can't hold it.
        /// </remarks>
        internal static int WriteEncodedUnit(string value, int index, Span<char> destination, ref int position)
        {
            // ref. RFC 2231 §7: extended-other-values := *(ext-octet / attribute-char); ext-octet := "%" 2(DIGIT / "A" / "B" / "C" / "D" / "E" / "F")
            int codePoint = value[index];
            if (codePoint < 0x80)
            {
                if (IsAttributeChar((char)codePoint))
                {
                    destination[position++] = (char)codePoint;
                }
                else
                {
                    WriteOctet((byte)codePoint, destination, ref position);
                }

                return 1;
            }

            // ref. RFC 3629 §3: UTF-8 definition.
            if (codePoint < 0x800)
            {
                WriteOctet((byte)(0xC0 | (codePoint >> 6)), destination, ref position);
                WriteOctet((byte)(0x80 | (codePoint & 0x3F)), destination, ref position);
                return 1;
            }

            if (!IsSurrogatePair(value, index))
            {
                if (char.IsSurrogate((char)codePoint)) codePoint = 0xFFFD;

                WriteOctet((byte)(0xE0 | (codePoint >> 12)), destination, ref position);
                WriteOctet((byte)(0x80 | ((codePoint >> 6) & 0x3F)), destination, ref position);
                WriteOctet((byte)(0x80 | (codePoint & 0x3F)), destination, ref position);
                return 1;
            }

            codePoint = char.ConvertToUtf32((char)codePoint, value[index + 1]);
            WriteOctet((byte)(0xF0 | (codePoint >> 18)), destination, ref position);
            WriteOctet((byte)(0x80 | ((codePoint >> 12) & 0x3F)), destination, ref position);
            WriteOctet((byte)(0x80 | ((codePoint >> 6) & 0x3F)), destination, ref position);
            WriteOctet((byte)(0x80 | (codePoint & 0x3F)), destination, ref position);
            return 2;
        }

        /// <summary>
        /// Determine if a character may appear unencoded in an RFC 2231 extended value.
        /// </summary>
        /// <remarks>
        /// attribute-char := &lt;any (US-ASCII) CHAR except SPACE, CTLs, "*", "'", "%", or tspecials&gt; (ref. RFC 2231 §7)
        /// </remarks>
        internal static bool IsAttributeChar(char c)
        {
            return c != '*' && c != '\'' && c != '%' && MimeGrammar.IsTokenChar(c);
        }

        /// <summary>
        /// Determine if a string is an RFC 1766 §2 language tag, which is what RFC 2231 §4 calls for.
        /// </summary>
        /// <remarks>
        /// Language-Tag = Primary-tag *( "-" Subtag ); Primary-tag = 1*8ALPHA; Subtag = 1*8ALPHA
        /// </remarks>
        internal static bool IsLanguage(ReadOnlySpan<char> value)
        {
            // TODO: RFC 2231 still refers to RFC 1766, so a language is not a BCP 47 language tag here, and tags such as es-419 are not
            // valid. Revisit this, and use Bcp47LanguageTag, if the MIME implementation is revised to a standard that calls for BCP 47.
            var subtagLength = 0;
            foreach (var c in value)
            {
                if (c == '-')
                {
                    if (subtagLength == 0) return false;

                    subtagLength = 0;
                    continue;
                }

                if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z')) return false;
                if (++subtagLength > 8) return false;
            }

            return subtagLength > 0;
        }

        private static bool IsSurrogatePair(string value, int index)
        {
            return char.IsHighSurrogate(value[index]) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]);
        }

        private static bool TryOrderSections(List<RawParameter> forms, MimeMediaTypeParseOptions options, out List<RawParameter> sections)
        {
            sections = forms;
            if ((options & MimeMediaTypeParseOptions.AllowLeadingZeroSectionNumbers) == 0)
            {
                foreach (var form in forms)
                {
                    if (form.HasLeadingZero) return false;
                }
            }

            // Sort by section number, and then by position, so that of two sections with the same number, the first written comes first.
            var keys = new long[forms.Count];
            for (var index = 0; index < forms.Count; index++)
            {
                keys[index] = ((long)forms[index].Section << 32) | (uint)index;
            }

            Array.Sort(keys);

            sections = new List<RawParameter>(forms.Count);
            var allowDuplicates = (options & MimeMediaTypeParseOptions.AllowDuplicateSections) != 0;
            var allowMissing = (options & MimeMediaTypeParseOptions.AllowMissingSections) != 0;
            var expected = 0;
            foreach (var key in keys)
            {
                var form = forms[(int)(key & 0xFFFFFFFF)];
                if (form.Section < expected)
                {
                    // ref. RFC 2231 §3: each section number appears once.
                    if (!allowDuplicates) return false;

                    continue;
                }

                // ref. RFC 2231 §3: the sections are numbered from 0, with no gaps.
                if (form.Section > expected && !allowMissing) return false;

                sections.Add(form);
                expected = form.Section + 1;
            }

            return true;
        }

        private static bool TryReadCharsetAndLanguage(string text, ref int position, out string charsetName, out string language)
        {
            // ref. RFC 2231 §7: extended-initial-value := [charset] "'" [language] "'" extended-other-values
            charsetName = string.Empty;
            language = string.Empty;

            var firstQuote = text.IndexOf('\'');
            if (firstQuote < 0) return false;

            var secondQuote = text.IndexOf('\'', firstQuote + 1);
            if (secondQuote < 0) return false;

            var languageSpan = text.AsSpan(firstQuote + 1, secondQuote - firstQuote - 1);
            if (!languageSpan.IsEmpty && !IsLanguage(languageSpan)) return false;

            charsetName = text.Substring(0, firstQuote);
            language = languageSpan.ToString();
            position = secondQuote + 1;
            return true;
        }

        private static bool TryDecodeOctets(string text, int start, bool isQuoted, bool allowLowercase, byte[] bytes, ref int byteCount)
        {
            for (var index = start; index < text.Length; index++)
            {
                var c = text[index];
                if (c == '%')
                {
                    int high, low;
                    if (index + 2 >= text.Length || (high = HexValue(text[index + 1], allowLowercase)) < 0 ||
                        (low = HexValue(text[index + 2], allowLowercase)) < 0)
                    {
                        return false;
                    }

                    bytes[byteCount++] = (byte)((high << 4) | low);
                    index += 2;
                    continue;
                }

                // A quoted value, which is accepted only as an opt-in, may also hold the characters that a token can't.
                if (!(isQuoted ? MimeGrammar.IsValueChar(c) && c != '\'' : IsAttributeChar(c))) return false;

                bytes[byteCount++] = (byte)c;
            }

            return true;
        }

        private static bool TryDecodeText(byte[] bytes, int byteCount, string charsetName, MimeMediaTypeParseOptions options,
            IMimeCharsetResolver charsetResolver, out string value)
        {
            value = string.Empty;

#if NETCOREAPP3_0_OR_GREATER
            Encoding? encoding;
#else
            Encoding encoding;
#endif
            MimeCharset charset;
            if (charsetName.Length == 0)
            {
                // With no character set, the value can only be US-ASCII.
                var isAscii = true;
                for (var index = 0; index < byteCount; index++)
                {
                    if (bytes[index] >= 0x80) isAscii = false;
                }

                if (isAscii)
                {
                    value = DecodeLatin1(bytes, byteCount);
                    return true;
                }
            }
            else if (MimeCharset.TryParse(charsetName, out charset) && charsetResolver.TryGetEncoding(charset, out encoding))
            {
                value = encoding.GetString(bytes, 0, byteCount);
                return true;
            }

            if ((options & MimeMediaTypeParseOptions.AllowUnknownCharsets) == 0) return false;

            value = DecodeLatin1(bytes, byteCount);
            return true;
        }

        private static string DecodeLatin1(byte[] bytes, int byteCount)
        {
            // Each octet becomes the character with the same number, so the octets can be recovered.
            var chars = new char[byteCount];
            for (var index = 0; index < byteCount; index++)
            {
                chars[index] = (char)bytes[index];
            }

            return new string(chars);
        }

        private static void WriteOctet(byte value, Span<char> destination, ref int position)
        {
            destination[position++] = '%';
            destination[position++] = MimeQuotedPrintable.HexDigits[value >> 4];
            destination[position++] = MimeQuotedPrintable.HexDigits[value & 0xF];
        }

        private static int HexValue(char c, bool allowLowercase)
        {
            // ref. RFC 2231 §7: ext-octet := "%" 2(DIGIT / "A" / "B" / "C" / "D" / "E" / "F")
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (allowLowercase && c >= 'a' && c <= 'f') return c - 'a' + 10;

            return -1;
        }
    }
}
