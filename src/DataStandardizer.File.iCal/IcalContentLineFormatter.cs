using System;
using System.Buffers;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Formats content lines (RFC 5545 §3.1) into a pooled line buffer, folded and with their line break, ready to be written in one call.  It
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
    internal sealed class IcalContentLineFormatter
    {
        private static class ErrorMessage
        {
            internal const string InvalidParameterNameTemplate = "The parameter name \"{0}\" is not valid.  A name has one or more letters, digits or '-'.";
            internal const string InvalidParameterValueCharacterTemplate = "Found an invalid character U+{0:X4} in a value of parameter {1}.";
            internal const string InvalidPropertyNameTemplate = "The property name \"{0}\" is not valid.  A name has one or more letters, digits or '-'.";
            internal const string InvalidValueCharacterTemplate = "Found an invalid character U+{0:X4} in the property value.";
            internal const string LineTooLong = "The content line is too long to write.";
        }

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
        private readonly int _foldLength;
        private readonly char _foldWhitespace;
        private readonly string _lineBreak;

        /// <param name="foldLength">The length, in octets, at which lines are folded.</param>
        /// <param name="foldWhitespace">The white space at the start of each continuation line.</param>
        /// <param name="lineBreak">The line break.</param>
        internal IcalContentLineFormatter(int foldLength, char foldWhitespace, string lineBreak)
        {
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
        /// <param name="line">The line.</param>
        /// <param name="lineNumber">The 1-based number the line will have once written, for error reports.</param>
        /// <returns>The number of characters of <see cref="Buffer"/> that hold the formatted line.</returns>
        /// <exception cref="IcalFileException">The line cannot be written: a name is not valid, or a parameter value or the value holds a
        /// character that is not allowed.</exception>
        internal int Format(IcalContentLine line, long lineNumber)
        {
            var logicalLength = Measure(line, lineNumber);
            var start = EnsureCapacity(logicalLength, lineNumber, line.Name);

            var position = start;
            position = Append(line.Name, position);
            var rawParameters = line.RawParameters;
            if (rawParameters != null)
            {
                position = Append(rawParameters, position);
            }
            else
            {
                position = AppendParameters(line.ParameterArray, position);
            }

            _buffer[position++] = ':';
            Append(line.Value, position);

            return Fold(start);
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
        private static long Measure(IcalContentLine line, long lineNumber)
        {
            // ref. RFC 5545§3.1: contentline = name *(";" param ) ":" value CRLF
            var name = line.Name;
            if (!IsName(name))
            {
                throw BuildException(string.Format(ErrorMessage.InvalidPropertyNameTemplate, name), lineNumber, null);
            }

            var invalidIndex = IndexOfInvalidValueCharacter(line.Value);
            if (invalidIndex >= 0)
            {
                throw BuildException(string.Format(ErrorMessage.InvalidValueCharacterTemplate, (int)line.Value[invalidIndex]), lineNumber, name);
            }

            long length = name.Length + 1 + line.Value.Length;
            var rawParameters = line.RawParameters;
            if (rawParameters != null)
            {
                // The reader has checked the syntax, but a caller-supplied text reader can still hand it a lone surrogate.
                invalidIndex = IndexOfInvalidValueCharacter(rawParameters);
                if (invalidIndex >= 0)
                {
                    throw BuildException(string.Format(ErrorMessage.InvalidValueCharacterTemplate, (int)rawParameters[invalidIndex]), lineNumber, name);
                }

                return length + rawParameters.Length;
            }

            foreach (var parameter in line.ParameterArray)
            {
                // ref. RFC 5545§3.1: param = param-name "=" param-value *("," param-value)
                if (!IsName(parameter.Name))
                {
                    throw BuildException(string.Format(ErrorMessage.InvalidParameterNameTemplate, parameter.Name), lineNumber, name);
                }

                length += 1 + parameter.Name.Length + 1;
                var values = parameter.ValueArray;
                for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    int invalidCharacter;
                    var valueLength = MeasureParameterValue(values[valueIndex], out invalidCharacter);
                    if (valueLength < 0)
                    {
                        var message = string.Format(ErrorMessage.InvalidParameterValueCharacterTemplate, invalidCharacter, parameter.Name);
                        throw BuildException(message, lineNumber, name);
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
        private int EnsureCapacity(long logicalLength, long lineNumber, string name)
        {
            // Every physical line but the last holds at least one character, and at least FoldLength - 4 octets once that is positive, since a
            // character is at most 4 octets.  A character is also at most 3 octets per UTF-16 code unit.
            var maxFolds = Math.Min(logicalLength, 3 * logicalLength / Math.Max(1, _foldLength - 4) + 1);
            var capacity = logicalLength + maxFolds * (_lineBreak.Length + 1) + _lineBreak.Length;
            if (capacity > int.MaxValue)
            {
                throw BuildException(ErrorMessage.LineTooLong, lineNumber, name);
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

        private int AppendParameters(IcalContentLineParameter[] parameters, int position)
        {
            var buffer = _buffer;
            for (var parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
            {
                var parameter = parameters[parameterIndex];
                buffer[position++] = ';';
                position = Append(parameter.Name, position);
                buffer[position++] = '=';

                var values = parameter.ValueArray;
                for (var valueIndex = 0; valueIndex < values.Length; valueIndex++)
                {
                    if (valueIndex > 0)
                    {
                        buffer[position++] = ',';
                    }

                    position = AppendParameterValue(values[valueIndex], position);
                }
            }

            return position;
        }

        /// <summary>
        /// Appends a parameter value with RFC 6868 caret encoding, quoted if it holds a ':', ';' or ','.  The value has been checked by
        /// <see cref="MeasureParameterValue"/>.
        /// </summary>
        private int AppendParameterValue(string value, int position)
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
                var character = value[index++];
                switch (character)
                {
                    case '^':
                        buffer[position++] = '^';
                        buffer[position++] = '^';
                        break;
                    case Quote:
                        buffer[position++] = '^';
                        buffer[position++] = '\'';
                        break;
                    case '\r':
                    case '\n':
                        if (character == '\r' && index < value.Length && value[index] == '\n')
                        {
                            index++;
                        }

                        buffer[position++] = '^';
                        buffer[position++] = 'n';
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

        private static IcalFileException BuildException(string message, long lineNumber,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? propertyName)
#else
            string propertyName)
#endif
        {
            var exception = new IcalFileException(message);
            exception.Data.Add(IcalFileException.DataItemName.LineNumber, lineNumber);
            if (propertyName != null)
            {
                exception.Data.Add(IcalFileException.DataItemName.PropertyName, propertyName);
            }

            return exception;
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
        /// Measures a parameter value once caret encoded and, if need be, quoted.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="invalidCharacter">The first character that cannot be written, if any.</param>
        /// <returns>The length; or -1 if the value holds a control character other than a tab or line break, or a lone surrogate.</returns>
        private static int MeasureParameterValue(string value, out int invalidCharacter)
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
                    case Quote:
                        length++;
                        break;
                    case '\r':
                        if (index + 1 < value.Length && value[index + 1] == '\n')
                        {
                            index++;
                        }
                        else
                        {
                            length++;
                        }

                        break;
                    case '\n':
                        length++;
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
