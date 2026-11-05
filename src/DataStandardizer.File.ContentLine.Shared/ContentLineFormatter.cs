using System;
using System.Buffers;
using System.Collections.Generic;

namespace DataStandardizer.File.ContentLine
{
    /// <summary>
    /// Formats content lines (RFC 5545 §3.1, RFC 6350 §3.3) into a pooled line buffer, folded and with their line break, ready to be written in one call.  It
    /// does no I/O, so a sync driver and an async driver can share it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A line is formatted in three passes.  The first checks the line and measures its logical (unfolded) length, so a line that cannot be
    /// written fails before anything is formatted.  The second formats the logical line into the tail of the buffer.  The third folds it
    /// forward into the head of the same buffer.  The buffer is sized for the most folds the line could need, so the folded text never
    /// overtakes the logical text it is read from.
    /// </para>
    /// <para>
    /// Fold lengths are counted in octets of the UTF-8 encoding.  A fold never splits a character or a surrogate pair.
    /// </para>
    /// </remarks>
    internal sealed class ContentLineFormatter
    {
        private const string Base64Indent = "    ";

        // ref. vCard 2.1 §2.2.3 (PHOTO): the example of an inline image has an indent of 4 characters and 64 characters of base64 on a line.
        private const int Base64LineLength = 64;

        // ref. vCard 2.1 §2.1.3: "Quoted-Printable lines of text must also be limited to less than 76 characters", not counting the line break.
        private const int MaxQuotedPrintableLineLength = 75;

        private const int MinimumBufferSize = 256;
        private const char Quote = '"';

        private static readonly char[] EmptyCharacters = new char[0];
        private static readonly char[] QuotedCharacters = { ':', ';', ',' };

#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> NameCharacters = SearchValues.Create("-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        private static readonly SearchValues<char> ParameterValueSpecialCharacters = SearchValues.Create(GetControlAndSurrogateCharacters("\"^:;,"));
        private static readonly SearchValues<char> ValueSpecialCharacters = SearchValues.Create(GetControlAndSurrogateCharacters(string.Empty));
#endif

        private char[] _buffer = EmptyCharacters;
        private readonly IContentLineErrorFactory _errorFactory;
        private readonly int _foldLength;
        private readonly char _foldWhitespace;
        private readonly string _lineBreak;

        /// <param name="foldLength">The length, in octets, at which lines are folded.</param>
        /// <param name="foldWhitespace">The white space at the start of each continuation line.</param>
        /// <param name="lineBreak">The line break.</param>
        /// <param name="errorFactory">Builds the exception for each problem found.</param>
        internal ContentLineFormatter(int foldLength, char foldWhitespace, string lineBreak, IContentLineErrorFactory errorFactory)
        {
            _errorFactory = errorFactory;
            _foldLength = foldLength;
            _foldWhitespace = foldWhitespace;
            _lineBreak = lineBreak;
        }

        /// <summary>
        /// Gets the buffer holding the line last formatted, from index 0.
        /// </summary>
        internal char[] Buffer => _buffer;

        /// <summary>
        /// Formats a content line, folded and with its line break, into <see cref="Buffer"/>.
        /// </summary>
        /// <typeparam name="TLine">The type of the line, a struct wherever possible so that its members are called directly.</typeparam>
        /// <param name="line">The line.</param>
        /// <param name="lineNumber">The 1-based number the line will have once written, for error reports.</param>
        /// <param name="caretMode">What to do with the characters of a parameter value that only caret encoding can represent.  It doesn't apply to
        /// <see cref="IContentLineData.RawParameters"/>, which are written as they are.</param>
        /// <param name="foldMode">How to break the line if it is long.</param>
        /// <returns>The number of characters of <see cref="Buffer"/> that hold the formatted line.</returns>
        /// <exception cref="Exception">What the error factory builds: the line cannot be written, because the group or a name is not valid, or a
        /// parameter value or the value holds a character that is not allowed.</exception>
        internal int Format<TLine>(TLine line, long lineNumber, ContentLineCaretMode caretMode = ContentLineCaretMode.Encode,
            ContentLineFoldMode foldMode = ContentLineFoldMode.Fold)
            where TLine : IContentLineData
        {
            var logicalLength = Measure(line, lineNumber, caretMode);
            var start = EnsureCapacity(logicalLength, foldMode, lineNumber, line.Name, line.Group);

            var position = start;
            var group = line.Group;
            if (group != null)
            {
                position = Append(group, position);
                _buffer[position++] = '.';
            }

            position = Append(line.Name, position);
            var rawParameters = line.RawParameters;
            if (rawParameters != null)
            {
                position = Append(rawParameters, position);
            }
            else
            {
                position = AppendParameters(line, position, caretMode);
            }

            _buffer[position++] = ':';
            Append(line.Value, position);

            switch (foldMode)
            {
                case ContentLineFoldMode.Fold:
                    return Fold(start);
                case ContentLineFoldMode.QuotedPrintableSoftBreak:
                    return BreakQuotedPrintable(start, line.Value.Length);
                case ContentLineFoldMode.Base64Lines:
                    return BreakBase64(start, line.Value.Length);
                default:
                    return MoveUnfolded(start);
            }
        }

        /// <summary>
        /// Returns the pooled buffer.  The formatter must not be used afterwards.
        /// </summary>
        internal void ReturnBuffer()
        {
            if (_buffer.Length > 0)
            {
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = EmptyCharacters;
            }
        }

        /// <summary>
        /// Checks that a line can be written, and measures its logical (unfolded) length.
        /// </summary>
        private long Measure<TLine>(TLine line, long lineNumber, ContentLineCaretMode caretMode)
            where TLine : IContentLineData
        {
            // ref. RFC 6350 §3.3: contentline = [group "."] name *(";" param) ":" value CRLF; group = 1*(ALPHA / DIGIT / "-")
            var group = line.Group;
            if (group != null && !IsName(group))
            {
                throw _errorFactory.CreateException(new ContentLineError(ContentLineErrorKind.InvalidGroupToWrite, lineNumber, null, null, group));
            }

            // ref. RFC 5545§3.1: contentline = name *(";" param ) ":" value CRLF
            var name = line.Name;
            if (!IsName(name))
            {
                throw _errorFactory.CreateException(new ContentLineError(ContentLineErrorKind.InvalidPropertyNameToWrite, lineNumber, null, null, name, group: group));
            }

            var invalidIndex = IndexOfInvalidValueCharacter(line.Value);
            if (invalidIndex >= 0)
            {
                throw BuildInvalidValueCharacterError(line.Value[invalidIndex], lineNumber, name, group);
            }

            long length = (group is null ? 0 : group.Length + 1) + name.Length + 1 + line.Value.Length;
            var rawParameters = line.RawParameters;
            if (rawParameters != null)
            {
                // The reader has checked the syntax, but a caller-supplied text reader can still hand it a lone surrogate.
                invalidIndex = IndexOfInvalidValueCharacter(rawParameters);
                if (invalidIndex >= 0)
                {
                    throw BuildInvalidValueCharacterError(rawParameters[invalidIndex], lineNumber, name, group);
                }

                return length + rawParameters.Length;
            }

            var parameterCount = line.ParameterCount;
            for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                // ref. RFC 5545§3.1: param = param-name "=" param-value *("," param-value)
                var parameterName = line.GetParameterName(parameterIndex);
                if (!IsName(parameterName))
                {
                    throw _errorFactory.CreateException(new ContentLineError(ContentLineErrorKind.InvalidParameterNameToWrite, lineNumber, null, name, parameterName, group: group));
                }

                length += 1 + parameterName.Length + 1;
                var valueCount = line.GetParameterValueCount(parameterIndex);
                for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    int invalidCharacter;
                    var valueLength = MeasureParameterValue(line.GetParameterValue(parameterIndex, valueIndex), caretMode, out invalidCharacter);
                    if (valueLength < 0)
                    {
                        var kind = invalidCharacter == Quote || invalidCharacter == '\r' || invalidCharacter == '\n'
                            ? ContentLineErrorKind.ParameterValueNeedsCaretEncoding
                            : ContentLineErrorKind.InvalidParameterValueCharacterToWrite;
                        throw _errorFactory.CreateException(new ContentLineError(kind, lineNumber, null, name, parameterName, invalidCharacter, group: group));
                    }

                    length += valueLength + (valueIndex > 0 ? 1 : 0);
                }
            }

            return length;
        }

        /// <summary>
        /// Makes the buffer large enough to hold the logical line and to fold it in place.
        /// </summary>
        /// <returns>The index at which to format the logical line, so that it ends at the end of the buffer.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private int EnsureCapacity(long logicalLength, ContentLineFoldMode foldMode, long lineNumber, string name, string? group)
#else
        private int EnsureCapacity(long logicalLength, ContentLineFoldMode foldMode, long lineNumber, string name, string group)
#endif
        {
            // Every physical line but the last holds at least one character, and at least FoldLength - 4 octets once that is positive, since a
            // character is at most 4 octets.  A character is also at most 3 octets per UTF-16 code unit.
            var maxFolds = Math.Min(logicalLength, 3 * logicalLength / Math.Max(1, _foldLength - 4) + 1);
            var capacity = logicalLength + maxFolds * (_lineBreak.Length + 1) + _lineBreak.Length;
            if (foldMode == ContentLineFoldMode.QuotedPrintableSoftBreak)
            {
                // A soft line break is a '=' and a line break, after at least MaxQuotedPrintableLineLength - 3 characters of whole tokens.
                capacity = logicalLength + (logicalLength / (MaxQuotedPrintableLineLength - 3) + 2) * (_lineBreak.Length + 1) + _lineBreak.Length;
            }
            else if (foldMode == ContentLineFoldMode.Base64Lines)
            {
                // Every line of the value has an indent and a line break, and the property's own line and the empty line have a line break each.
                capacity = logicalLength + (logicalLength / Base64LineLength + 1) * (Base64Indent.Length + _lineBreak.Length) + 2 * _lineBreak.Length;
            }

            if (capacity > int.MaxValue)
            {
                throw _errorFactory.CreateException(new ContentLineError(ContentLineErrorKind.LineTooLongToWrite, lineNumber, null, name, group: group));
            }

            if (_buffer.Length < capacity)
            {
                ReturnBuffer();
                _buffer = ArrayPool<char>.Shared.Rent(Math.Max((int)capacity, MinimumBufferSize));
            }

            return _buffer.Length - (int)logicalLength;
        }

        /// <summary>
        /// Folds the logical line, which runs from <paramref name="start"/> to the end of the buffer, into the head of the buffer, and ends it
        /// with a line break.
        /// </summary>
        /// <returns>The length of the folded line.</returns>
        private int Fold(int start)
        {
            var buffer = _buffer;
            var end = buffer.Length;
            var read = start;
            var write = 0;
            var lineOctets = 0;
            var lineStartOctets = 0;

            while (read < end)
            {
                // Copy as much ASCII as fits on the physical line in one go.  A continuation line can already be over length, if it took a
                // character too long for it.
                var available = _foldLength - lineOctets;
                var asciiLength = CountAscii(buffer, read, Math.Max(0, Math.Min(available, end - read)));
                if (asciiLength > 0)
                {
                    Array.Copy(buffer, read, buffer, write, asciiLength);
                    read += asciiLength;
                    write += asciiLength;
                    lineOctets += asciiLength;
                    continue;
                }

                int characterLength, octets;
                GetUtf8Length(buffer, read, out characterLength, out octets);

                // A continuation line always takes one character, even one too long for it, so folding moves forward.
                if (octets > available && lineOctets > lineStartOctets)
                {
                    write = AppendLineBreak(write);
                    buffer[write++] = _foldWhitespace;
                    lineOctets = lineStartOctets = 1;
                    continue;
                }

                buffer[write++] = buffer[read++];
                if (characterLength == 2)
                {
                    buffer[write++] = buffer[read++];
                }

                lineOctets += octets;
            }

            return AppendLineBreak(write);
        }

        /// <summary>
        /// Moves the logical line, which runs from <paramref name="start"/> to the end of the buffer, to the head of the buffer, and ends it with a
        /// line break.
        /// </summary>
        /// <returns>The length of the line.</returns>
        private int MoveUnfolded(int start)
        {
            var length = _buffer.Length - start;
            Array.Copy(_buffer, start, _buffer, 0, length);
            return AppendLineBreak(length);
        }

        /// <summary>
        /// Breaks the logical line, which runs from <paramref name="start"/> to the end of the buffer, into the head of the buffer, with soft line breaks in its
        /// quoted-printable value, and ends it with a line break.  The value has been checked, so a <c>=</c> always has two digits after it.
        /// </summary>
        /// <returns>The length of the broken line.</returns>
        private int BreakQuotedPrintable(int start, int valueLength)
        {
            var buffer = _buffer;
            var end = buffer.Length;
            var read = end - valueLength;
            var prefixLength = read - start;
            Array.Copy(buffer, start, buffer, 0, prefixLength);

            var write = prefixLength;
            var lineLength = prefixLength;
            while (read < end)
            {
                var tokenLength = buffer[read] == '=' ? 3 : 1;

                // Every physical line but the last needs room for its '='.
                var limit = read + tokenLength == end ? MaxQuotedPrintableLineLength : MaxQuotedPrintableLineLength - 1;
                if (lineLength + tokenLength > limit)
                {
                    buffer[write++] = '=';
                    write = AppendLineBreak(write);
                    lineLength = 0;
                }

                for (var index = 0; index < tokenLength; index++)
                {
                    buffer[write++] = buffer[read++];
                }

                lineLength += tokenLength;
            }

            return AppendLineBreak(write);
        }

        /// <summary>
        /// Lays out the logical line, which runs from <paramref name="start"/> to the end of the buffer, into the head of the buffer, with its base64 value on the
        /// lines after the property's own and an empty line after them.
        /// </summary>
        /// <returns>The length of the lines.</returns>
        private int BreakBase64(int start, int valueLength)
        {
            var buffer = _buffer;
            var end = buffer.Length;
            var read = end - valueLength;
            var prefixLength = read - start;
            Array.Copy(buffer, start, buffer, 0, prefixLength);

            var write = AppendLineBreak(prefixLength);
            var lineLength = 0;
            for (; read < end; read++)
            {
                var character = buffer[read];
                if (character == ' ' || character == '\t') continue;

                if (lineLength == 0)
                {
                    write = Append(Base64Indent, write);
                }

                buffer[write++] = character;
                if (++lineLength == Base64LineLength)
                {
                    write = AppendLineBreak(write);
                    lineLength = 0;
                }
            }

            if (lineLength > 0)
            {
                write = AppendLineBreak(write);
            }

            return AppendLineBreak(write);
        }

        private int Append(string text, int position)
        {
            text.CopyTo(0, _buffer, position, text.Length);
            return position + text.Length;
        }

        private int AppendLineBreak(int position)
        {
            for (var index = 0; index < _lineBreak.Length; index++)
            {
                _buffer[position++] = _lineBreak[index];
            }

            return position;
        }

        private int AppendParameters<TLine>(TLine line, int position, ContentLineCaretMode caretMode)
            where TLine : IContentLineData
        {
            var buffer = _buffer;
            var parameterCount = line.ParameterCount;
            for (var parameterIndex = 0; parameterIndex < parameterCount; parameterIndex++)
            {
                buffer[position++] = ';';
                position = Append(line.GetParameterName(parameterIndex), position);
                buffer[position++] = '=';

                var valueCount = line.GetParameterValueCount(parameterIndex);
                for (var valueIndex = 0; valueIndex < valueCount; valueIndex++)
                {
                    if (valueIndex > 0)
                    {
                        buffer[position++] = ',';
                    }

                    position = AppendParameterValue(line.GetParameterValue(parameterIndex, valueIndex), position, caretMode);
                }
            }

            return position;
        }

        /// <summary>
        /// Appends a parameter value as <paramref name="caretMode"/> says, quoted if it holds a ':', ';' or ','.  The value has been checked by
        /// <see cref="MeasureParameterValue"/>.
        /// </summary>
        private int AppendParameterValue(string value, int position, ContentLineCaretMode caretMode)
        {
            var buffer = _buffer;
            var isQuoted = NeedsQuotes(value);
            if (isQuoted)
            {
                buffer[position++] = Quote;
            }

            var index = 0;
            while (index < value.Length)
            {
                var specialIndex = IndexOfParameterValueSpecialCharacter(value, index);
                var plainLength = (specialIndex < 0 ? value.Length : specialIndex) - index;
                value.CopyTo(index, buffer, position, plainLength);
                position += plainLength;
                index += plainLength;
                if (specialIndex < 0) break;

                // ref. RFC 6868§3: ^ becomes ^^, a newline ^n and a double quote ^'.
                var isCaretEncoded = caretMode == ContentLineCaretMode.Encode;
                var character = value[index++];
                switch (character)
                {
                    case '^':
                        buffer[position++] = '^';
                        if (isCaretEncoded)
                        {
                            buffer[position++] = '^';
                        }

                        break;
                    case Quote:
                        if (isCaretEncoded)
                        {
                            buffer[position++] = '^';
                            buffer[position++] = '\'';
                        }

                        break;
                    case '\r':
                    case '\n':
                        if (character == '\r' && index < value.Length && value[index] == '\n')
                        {
                            index++;
                        }

                        if (isCaretEncoded)
                        {
                            buffer[position++] = '^';
                            buffer[position++] = 'n';
                        }
                        else
                        {
                            buffer[position++] = ' ';
                        }

                        break;
                    default:
                        // A delimiter, a tab or a surrogate, which is written as it is.
                        buffer[position++] = character;
                        break;
                }
            }

            if (isQuoted)
            {
                buffer[position++] = Quote;
            }

            return position;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private Exception BuildInvalidValueCharacterError(int character, long lineNumber, string propertyName, string? group)
#else
        private Exception BuildInvalidValueCharacterError(int character, long lineNumber, string propertyName, string group)
#endif
        {
            return _errorFactory.CreateException(new ContentLineError(ContentLineErrorKind.InvalidValueCharacter, lineNumber, null, propertyName, character: character, group: group));
        }

        /// <summary>
        /// Gets the length of the character at an index and of its UTF-8 encoding.  Surrogates have been checked to be in pairs.
        /// </summary>
        private static void GetUtf8Length(char[] buffer, int index, out int characterLength, out int octets)
        {
            var character = buffer[index];
            characterLength = 1;
            if (character < 0x80)
            {
                octets = 1;
            }
            else if (character < 0x800)
            {
                octets = 2;
            }
            else if (char.IsHighSurrogate(character))
            {
                characterLength = 2;
                octets = 4;
            }
            else
            {
                octets = 3;
            }
        }

        /// <summary>
        /// Gets whether a character is a CONTROL character of RFC 5545 §3.1: any control character but horizontal tab.
        /// </summary>
        private static bool IsControlCharacter(char character)
        {
            return (character < ' ' && character != '\t') || character == '\x7F';
        }

        /// <summary>
        /// Measures a parameter value once written as <paramref name="caretMode"/> says and, if need be, quoted.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="caretMode">What to do with the characters that only caret encoding can represent.</param>
        /// <param name="invalidCharacter">The first character that cannot be written, if any.</param>
        /// <returns>The length; or -1 if the value holds a control character other than a tab or line break, or a lone surrogate, or, under
        /// <see cref="ContentLineCaretMode.Reject"/>, a double quote or a line break.</returns>
        private static int MeasureParameterValue(string value, ContentLineCaretMode caretMode, out int invalidCharacter)
        {
            invalidCharacter = 0;
            var length = value.Length;
            var index = IndexOfParameterValueSpecialCharacter(value, 0);
            var isQuoted = false;
            while (index >= 0)
            {
                var character = value[index];
                switch (character)
                {
                    case '^':
                        if (caretMode == ContentLineCaretMode.Encode)
                        {
                            length++;
                        }

                        break;
                    case Quote:
                        if (caretMode == ContentLineCaretMode.Reject)
                        {
                            invalidCharacter = character;
                            return -1;
                        }

                        length += caretMode == ContentLineCaretMode.Encode ? 1 : -1;
                        break;
                    case '\r':
                    case '\n':
                        if (caretMode == ContentLineCaretMode.Reject)
                        {
                            invalidCharacter = character;
                            return -1;
                        }

                        // A line break becomes "^n" or a space, whether it was CRLF or one character.
                        var isCrlf = character == '\r' && index + 1 < value.Length && value[index + 1] == '\n';
                        length += (caretMode == ContentLineCaretMode.Encode ? 2 : 1) - (isCrlf ? 2 : 1);
                        if (isCrlf)
                        {
                            index++;
                        }

                        break;
                    case ':':
                    case ';':
                    case ',':
                        isQuoted = true;
                        break;
                    case '\t':
                        break;
                    default:
                        if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                        {
                            index++;
                            break;
                        }

                        if (IsControlCharacter(character) || char.IsSurrogate(character))
                        {
                            invalidCharacter = character;
                            return -1;
                        }

                        break;
                }

                index = IndexOfParameterValueSpecialCharacter(value, index + 1);
            }

            return isQuoted ? length + 2 : length;
        }

        private static bool NeedsQuotes(string value)
        {
            // ref. RFC 5545§3.1: paramtext excludes DQUOTE, ";", ":" and ","; caret encoding has replaced any DQUOTE.
            return value.IndexOfAny(QuotedCharacters) >= 0;
        }

        /// <summary>
        /// Gets the index of the first character of a value that is a CONTROL character or a lone surrogate.
        /// </summary>
        /// <returns>The index; or -1 if every character can be written.</returns>
        private static int IndexOfInvalidValueCharacter(string value)
        {
            // ref. RFC 5545§3.1: value = *VALUE-CHAR, which excludes every control character except horizontal tab.
#if NET8_0_OR_GREATER
            var index = value.AsSpan().IndexOfAny(ValueSpecialCharacters);
            if (index < 0) return -1;
#else
            var index = 0;
#endif
            for (; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsHighSurrogate(character) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
                {
                    index++;
                    continue;
                }

                if (IsControlCharacter(character) || char.IsSurrogate(character)) return index;
            }

            return -1;
        }

        /// <summary>
        /// Gets the index, from a start index, of the next character of a parameter value that is encoded, quoted or checked.
        /// </summary>
        private static int IndexOfParameterValueSpecialCharacter(string value, int start)
        {
#if NET8_0_OR_GREATER
            var index = value.AsSpan(start).IndexOfAny(ParameterValueSpecialCharacters);
            return index < 0 ? -1 : start + index;
#else
            for (var index = start; index < value.Length; index++)
            {
                var character = value[index];
                if (character < ' ' || character == '\x7F' || char.IsSurrogate(character)) return index;

                switch (character)
                {
                    case '^':
                    case Quote:
                    case ':':
                    case ';':
                    case ',':
                        return index;
                }
            }

            return -1;
#endif
        }

        /// <summary>
        /// Counts the ASCII characters at the start of a range of the buffer.
        /// </summary>
        private static int CountAscii(char[] buffer, int start, int length)
        {
#if NET8_0_OR_GREATER
            var index = new ReadOnlySpan<char>(buffer, start, length).IndexOfAnyExceptInRange('\0', '\x7F');
            return index < 0 ? length : index;
#else
            var index = 0;
            while (index < length && buffer[start + index] < 0x80)
            {
                index++;
            }

            return index;
#endif
        }

        private static bool IsName(string name)
        {
            // ref. RFC 5545§3.1: iana-token = 1*(ALPHA / DIGIT / "-"); x-name uses the same characters.
            if (name.Length == 0) return false;

#if NET8_0_OR_GREATER
            return name.AsSpan().IndexOfAnyExcept(NameCharacters) < 0;
#else
            foreach (var character in name)
            {
                if (!((character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') ||
                    character == '-'))
                {
                    return false;
                }
            }

            return true;
#endif
        }

#if NET8_0_OR_GREATER
        private static string GetControlAndSurrogateCharacters(string additionalCharacters)
        {
            var characters = new List<char>();
            for (var character = '\0'; character < ' '; character++)
            {
                characters.Add(character);
            }

            characters.Add('\x7F');
            for (var character = '\uD800'; character <= '\uDFFF'; character++)
            {
                characters.Add(character);
            }

            characters.AddRange(additionalCharacters);
            return new string(characters.ToArray());
        }
#endif
    }
}
