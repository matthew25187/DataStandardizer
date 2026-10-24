using System;
using System.Buffers;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Finds and parses content lines (RFC 5545 §3.1) in a pooled character block.  It does no I/O: when it needs more input it says so, and a
    /// driver refills the block and calls it again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each logical (unfolded) content line is kept in one contiguous run of the block.  Unfolding happens in place while scanning, which only
    /// ever moves characters towards the start of the line.  On refill the block is compacted from the start of the current line, and a line
    /// longer than half the block makes the scanner rent a larger one.
    /// </para>
    /// <para>
    /// The name, each parameter name and value, and the value are held as <c>(offset, length)</c> slices of the block.  Caret decoding and
    /// TEXT unescaping are done only when asked for, into a pooled scratch buffer sized for the whole line, so the raw slices stay intact.
    /// </para>
    /// </remarks>
    internal sealed class IcalContentLineScanner
    {
        internal enum ScanStatus
        {
            LineRead,
            NeedMoreInput,
            EndOfInput
        }

        private static class ErrorMessage
        {
            internal const string BareLineFeed = "Found a line feed without a carriage return.  RFC 5545 requires a CRLF line break.";
            internal const string EmptyLine = "Found an empty content line.";
            internal const string ExpectedNameEnd = "Expected ';' or ':' after the property name.";
            internal const string ExpectedParameterEquals = "Expected '=' after the parameter name.";
            internal const string ExpectedParameterName = "Expected a parameter name after ';'.";
            internal const string ExpectedParameterValueEnd = "Expected ',', ';' or ':' after the parameter value.";
            internal const string ExpectedPropertyName = "Expected a property name at the start of the content line.";
            internal const string InvalidParameterValueCharacterTemplate = "Found an invalid character U+{0:X4} in a parameter value.";
            internal const string InvalidTextEscape = "Found a backslash that does not start a valid TEXT escape sequence.";
            internal const string InvalidValueCharacterTemplate = "Found an invalid character U+{0:X4} in the property value.";
            internal const string LineTooLongTemplate = "The content line is longer than the maximum of {0} characters.";
            internal const string MissingValue = "Expected ':' and a property value.";
            internal const string UnterminatedQuotedString = "Expected a closing '\"' for the quoted parameter value.";
        }

        private const char ByteOrderMark = '﻿';
        private const char Quote = '"';

        // Where a decoded value is held.
        private const byte DecodedNone = 0;
        private const byte DecodedInBlock = 1;
        private const byte DecodedInScratch = 2;

        private static readonly char[] EmptyCharacters = new char[0];

#if NET8_0_OR_GREATER
        private static readonly SearchValues<char> NameCharacters = SearchValues.Create("-0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
        private static readonly SearchValues<char> ControlCharacters = SearchValues.Create(GetControlCharacters(string.Empty));
        private static readonly SearchValues<char> ParameterTextStopCharacters = SearchValues.Create(GetControlCharacters("\";:,"));
        private static readonly SearchValues<char> QuotedStringStopCharacters = SearchValues.Create(GetControlCharacters("\""));
#endif

        private readonly bool _acceptBareLineFeed;
        private readonly int _bufferSize;
        private readonly int _maxLineLength;

        private char[] _buffer = EmptyCharacters;
        private int _bufferEnd;
        private int _bufferLength;
        private bool _endOfInput;
        private int[] _foldOffsets = new int[4];
        private int _foldCount;
        private int _lineEnd;
        private bool _lineInProgress;
        private long _lineNumber;
        private long _linesRead;
        private int _lineStart;
        private int _nameLength;
        private ParameterSlice[] _parameters = new ParameterSlice[4];
        private int _parameterCount;
        private ValueSlice[] _parameterValues = new ValueSlice[4];
        private int _parameterValueCount;
        private long _physicalLineNumber;
        private long _physicalLinesRead;
        private int _position;
        private int _requiredCount;
        private char[] _scratch = EmptyCharacters;
        private int _scratchEnd;
        private bool _started;
        private DecodedSlice _textValue;
        private int _valueLength;
        private int _valueOffset;

        /// <param name="bufferSize">The initial size, in characters, of the block.</param>
        /// <param name="acceptBareLineFeed"><c>true</c> to accept a bare line feed as a line break.</param>
        /// <param name="maxLineLength">The maximum length of a logical line, or <see cref="int.MaxValue"/> for no limit.</param>
        internal IcalContentLineScanner(int bufferSize, bool acceptBareLineFeed, int maxLineLength)
        {
            _bufferSize = bufferSize;
            _acceptBareLineFeed = acceptBareLineFeed;
            _maxLineLength = maxLineLength;
        }

        /// <summary>
        /// Gets the block a driver reads input into, at the position given by <see cref="PrepareRefill"/>.
        /// </summary>
        internal char[] Buffer => _buffer;

        /// <summary>
        /// Gets a flag indicating that a line break or line length error was found, after which the line boundaries are unknown.
        /// </summary>
        internal bool IsFaulted { get; private set; }

        /// <summary>
        /// Gets the 1-based number of the current logical line.
        /// </summary>
        internal long LineNumber => _lineNumber;

        /// <summary>
        /// Gets the number of logical lines found so far, including any that failed to parse.
        /// </summary>
        internal long LinesRead => _linesRead;

        /// <summary>
        /// Gets the 1-based number of the physical line on which the current logical line starts.
        /// </summary>
        internal long PhysicalLineNumber => _physicalLineNumber;

        /// <summary>
        /// Gets the number of physical lines consumed so far.
        /// </summary>
        internal long PhysicalLinesRead => _physicalLinesRead;

        /// <summary>
        /// Finds the next logical line and unfolds it in place.
        /// </summary>
        /// <returns>
        /// <see cref="ScanStatus.LineRead"/> when a whole line is in the block; <see cref="ScanStatus.NeedMoreInput"/> when the driver must call
        /// <see cref="PrepareRefill"/> and <see cref="CompleteRefill"/>, then call this method again; <see cref="ScanStatus.EndOfInput"/> when
        /// there are no more lines.
        /// </returns>
        /// <exception cref="IcalFileException">A bare line feed was found and is not accepted, or the line is longer than the maximum.  The scanner
        /// is then faulted.</exception>
        internal ScanStatus Scan()
        {
            // Every early return leaves the state as it was at a point the scan can restart from.
            if (!_started)
            {
                if (!HasAvailable(1) && !_endOfInput) return RequestInput(1);
                if (HasAvailable(1) && _buffer[_position] == ByteOrderMark) _position++;
                _started = true;
            }

            if (!_lineInProgress)
            {
                if (!HasAvailable(1)) return _endOfInput ? ScanStatus.EndOfInput : RequestInput(1);

                _lineStart = _lineEnd = _position;
                _foldCount = 0;
                _physicalLineNumber = _physicalLinesRead + 1;
                _lineInProgress = true;
            }

            while (true)
            {
                var unread = new ReadOnlySpan<char>(_buffer, _position, _bufferEnd - _position);
                var breakIndex = unread.IndexOfAny('\r', '\n');
                AppendToLine(breakIndex < 0 ? unread.Length : breakIndex);
                if (breakIndex < 0)
                {
                    if (!_endOfInput) return RequestInput(1);

                    // ref. RFC 5545§3.1: content lines are delimited by a line break, so the last one need not end with one.
                    _physicalLinesRead++;
                    return CompleteLine();
                }

                int lineBreakLength;
                if (_buffer[_position] == '\r')
                {
                    if (!HasAvailable(2) && !_endOfInput) return RequestInput(2);
                    if (!HasAvailable(2) || _buffer[_position + 1] != '\n')
                    {
                        // A bare carriage return is not a line break.  It stays in the line, where parsing rejects it.
                        AppendToLine(1);
                        continue;
                    }

                    lineBreakLength = 2;
                }
                else
                {
                    if (!_acceptBareLineFeed)
                    {
                        throw Fault(ErrorMessage.BareLineFeed);
                    }

                    lineBreakLength = 1;
                }

                // Look past the line break for the white space that makes it a fold.
                if (!HasAvailable(lineBreakLength + 1) && !_endOfInput) return RequestInput(lineBreakLength + 1);

                _physicalLinesRead++;
                if (HasAvailable(lineBreakLength + 1) && IsFoldWhiteSpace(_buffer[_position + lineBreakLength]))
                {
                    // ref. RFC 5545§3.1: unfold by removing the line break and the single white space character after it.
                    _position += lineBreakLength + 1;
                    AddFoldOffset(_lineEnd - _lineStart);
                    continue;
                }

                _position += lineBreakLength;
                return CompleteLine();
            }
        }

        /// <summary>
        /// Makes room in the block for at least as many characters as <see cref="Scan"/> asked for.
        /// </summary>
        /// <param name="offset">The index in <see cref="Buffer"/> to read into.</param>
        /// <param name="count">The number of characters that may be read.</param>
        internal void PrepareRefill(out int offset, out int count)
        {
            var lineStart = _lineInProgress ? _lineStart : _position;
            var lineLength = _lineInProgress ? _lineEnd - _lineStart : 0;
            var unread = _bufferEnd - _position;
            var requiredLength = lineLength + _requiredCount;

            var capacity = _bufferLength;
            if (capacity < requiredLength || lineLength + unread > capacity / 2)
            {
                capacity = Math.Max(Math.Max(_bufferSize, requiredLength), capacity * 2);
            }

            var target = capacity == _bufferLength ? _buffer : ArrayPool<char>.Shared.Rent(capacity);
            if (lineLength > 0 && (target != _buffer || lineStart != 0))
            {
                Array.Copy(_buffer, lineStart, target, 0, lineLength);
            }

            if (unread > 0 && (target != _buffer || _position != lineLength))
            {
                Array.Copy(_buffer, _position, target, lineLength, unread);
            }

            if (target != _buffer)
            {
                if (_bufferLength > 0)
                {
                    ArrayPool<char>.Shared.Return(_buffer);
                }

                _buffer = target;
                _bufferLength = capacity;
            }

            if (_lineInProgress)
            {
                _lineStart = 0;
                _lineEnd = lineLength;
            }

            _position = lineLength;
            _bufferEnd = lineLength + unread;

            offset = _bufferEnd;
            count = _bufferLength - _bufferEnd;
        }

        /// <summary>
        /// Records the result of reading into the block.
        /// </summary>
        /// <param name="charactersRead">The number of characters read, or 0 at the end of the input.</param>
        internal void CompleteRefill(int charactersRead)
        {
            if (charactersRead == 0)
            {
                _endOfInput = true;
            }
            else
            {
                _bufferEnd += charactersRead;
            }
        }

        /// <summary>
        /// Splits the line found by <see cref="Scan"/> into its name, parameters and value.
        /// </summary>
        /// <exception cref="IcalFileException">The line does not follow the content line syntax of RFC 5545 §3.1.  The line has been consumed, so
        /// the next line can still be read.</exception>
        internal void ParseLine()
        {
            _nameLength = 0;
            _parameterCount = 0;
            _parameterValueCount = 0;
            _scratchEnd = 0;
            _textValue = default(DecodedSlice);

            var line = new ReadOnlySpan<char>(_buffer, _lineStart, _lineEnd - _lineStart);
            if (line.Length == 0)
            {
                throw BuildException(ErrorMessage.EmptyLine, 0);
            }

            // ref. RFC 5545§3.1: contentline = name *(";" param ) ":" value CRLF
            var index = IndexOfNameEnd(line, 0);
            if (index == 0)
            {
                throw BuildException(ErrorMessage.ExpectedPropertyName, 0);
            }

            _nameLength = index;
            if (index == line.Length)
            {
                throw BuildException(ErrorMessage.MissingValue, index);
            }

            while (line[index] == ';')
            {
                // ref. RFC 5545§3.1: param = param-name "=" param-value *("," param-value)
                var parameterNameStart = ++index;
                index = IndexOfNameEnd(line, index);
                if (index == parameterNameStart)
                {
                    throw BuildException(ErrorMessage.ExpectedParameterName, index);
                }

                if (index == line.Length || line[index] != '=')
                {
                    throw BuildException(ErrorMessage.ExpectedParameterEquals, index);
                }

                var parameter = new ParameterSlice
                {
                    NameOffset = _lineStart + parameterNameStart,
                    NameLength = index - parameterNameStart,
                    FirstValue = _parameterValueCount
                };
                do
                {
                    index = ParseParameterValue(line, index + 1);
                    parameter.ValueCount++;
                } while (line[index] == ',');

                AddParameter(parameter);
            }

            if (line[index] != ':')
            {
                throw BuildException(ErrorMessage.ExpectedNameEnd, index);
            }

            // ref. RFC 5545§3.1: value = *VALUE-CHAR, which excludes every control character except horizontal tab.
            index++;
            var invalidIndex = IndexOfControlCharacter(line.Slice(index));
            if (invalidIndex >= 0)
            {
                var message = string.Format(ErrorMessage.InvalidValueCharacterTemplate, (int)line[index + invalidIndex]);
                throw BuildException(message, index + invalidIndex);
            }

            _valueOffset = _lineStart + index;
            _valueLength = line.Length - index;
        }

        /// <summary>
        /// Returns the pooled blocks.  The scanner must not be used afterwards.
        /// </summary>
        internal void ReturnBuffers()
        {
            if (_bufferLength > 0)
            {
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = EmptyCharacters;
                _bufferLength = 0;
            }

            if (_scratch.Length > 0)
            {
                ArrayPool<char>.Shared.Return(_scratch);
                _scratch = EmptyCharacters;
            }
        }

        internal ReadOnlyMemory<char> RawLine => new ReadOnlyMemory<char>(_buffer, _lineStart, _lineEnd - _lineStart);

        internal ReadOnlyMemory<char> Name => new ReadOnlyMemory<char>(_buffer, _lineStart, _nameLength);

        internal int ParameterCount => _parameterCount;

        internal ReadOnlyMemory<char> RawValue => new ReadOnlyMemory<char>(_buffer, _valueOffset, _valueLength);

        internal ReadOnlyMemory<char> GetParameterName(int parameterIndex)
        {
            var parameter = GetParameter(parameterIndex);
            return new ReadOnlyMemory<char>(_buffer, parameter.NameOffset, parameter.NameLength);
        }

        internal int GetParameterValueCount(int parameterIndex)
        {
            return GetParameter(parameterIndex).ValueCount;
        }

        internal ReadOnlyMemory<char> GetRawParameterValue(int parameterIndex, int valueIndex)
        {
            var value = _parameterValues[GetParameterValueIndex(parameterIndex, valueIndex)];
            return new ReadOnlyMemory<char>(_buffer, value.Offset, value.Length);
        }

        internal bool IsParameterValueQuoted(int parameterIndex, int valueIndex)
        {
            return _parameterValues[GetParameterValueIndex(parameterIndex, valueIndex)].IsQuoted;
        }

        /// <summary>
        /// Gets a parameter value with RFC 6868 caret encoding decoded.  The value is decoded once per line and kept.
        /// </summary>
        internal ReadOnlyMemory<char> GetParameterValue(int parameterIndex, int valueIndex)
        {
            ref var value = ref _parameterValues[GetParameterValueIndex(parameterIndex, valueIndex)];
            if (value.Decoded.Source == DecodedNone)
            {
                var raw = new ReadOnlySpan<char>(_buffer, value.Offset, value.Length);
                if (raw.IndexOf('^') < 0)
                {
                    value.Decoded = new DecodedSlice(DecodedInBlock, value.Offset, value.Length);
                }
                else
                {
                    var scratch = GetScratch();
                    var decodedLength = IcalEscaping.DecodeCaret(raw, scratch);
                    value.Decoded = new DecodedSlice(DecodedInScratch, _scratchEnd, decodedLength);
                    _scratchEnd += decodedLength;
                }
            }

            return GetDecoded(value.Decoded);
        }

        /// <summary>
        /// Gets the value with RFC 5545 §3.3.11 TEXT escaping removed.  The value is unescaped once per line and kept.
        /// </summary>
        /// <exception cref="IcalFileException">The value contains a backslash that does not start a valid escape sequence.</exception>
        internal ReadOnlyMemory<char> GetTextValue()
        {
            if (_textValue.Source == DecodedNone)
            {
                var raw = new ReadOnlySpan<char>(_buffer, _valueOffset, _valueLength);
                if (raw.IndexOf('\\') < 0)
                {
                    _textValue = new DecodedSlice(DecodedInBlock, _valueOffset, _valueLength);
                }
                else
                {
                    var scratch = GetScratch();
                    int unescapedLength, errorIndex;
                    if (!IcalEscaping.TryUnescapeText(raw, scratch, out unescapedLength, out errorIndex))
                    {
                        throw BuildException(ErrorMessage.InvalidTextEscape, _valueOffset - _lineStart + errorIndex);
                    }

                    _textValue = new DecodedSlice(DecodedInScratch, _scratchEnd, unescapedLength);
                    _scratchEnd += unescapedLength;
                }
            }

            return GetDecoded(_textValue);
        }

        /// <summary>
        /// Copies the offsets in the current line at which folds were removed.
        /// </summary>
        /// <returns>The offsets, or <c>null</c> if the line was not folded.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal int[]? CopyFoldOffsets()
#else
        internal int[] CopyFoldOffsets()
#endif
        {
            if (_foldCount == 0) return null;

            var foldOffsets = new int[_foldCount];
            Array.Copy(_foldOffsets, foldOffsets, _foldCount);
            return foldOffsets;
        }

        /// <summary>
        /// Gets the offset of the value from the start of the current line.
        /// </summary>
        internal int ValueOffsetInLine => _valueOffset - _lineStart;

        /// <summary>
        /// Gets the physical line on which a character of a logical line falls.
        /// </summary>
        /// <param name="physicalLineNumber">The physical line on which the logical line starts.</param>
        /// <param name="foldOffsets">The offsets at which folds were removed, in ascending order; or <c>null</c>.</param>
        /// <param name="foldCount">The number of offsets in <paramref name="foldOffsets"/>.</param>
        /// <param name="offset">The offset of the character from the start of the logical line.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static long GetPhysicalLineNumber(long physicalLineNumber, int[]? foldOffsets, int foldCount, int offset)
#else
        internal static long GetPhysicalLineNumber(long physicalLineNumber, int[] foldOffsets, int foldCount, int offset)
#endif
        {
            if (foldOffsets is null) return physicalLineNumber;

            // A fold removed at an offset puts the characters from that offset onwards on the next physical line.
            var lineNumber = physicalLineNumber;
            for (var foldIndex = 0; foldIndex < foldCount && foldOffsets[foldIndex] <= offset; foldIndex++)
            {
                lineNumber++;
            }

            return lineNumber;
        }

        /// <summary>
        /// Builds an exception for a problem at a character of a logical line.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static IcalFileException BuildException(string message, long lineNumber, long physicalLineNumber, string? propertyName)
#else
        internal static IcalFileException BuildException(string message, long lineNumber, long physicalLineNumber, string propertyName)
#endif
        {
            var exception = new IcalFileException(message);
            exception.Data.Add(IcalFileException.DataItemName.LineNumber, lineNumber);
            exception.Data.Add(IcalFileException.DataItemName.PhysicalLineNumber, physicalLineNumber);
            if (propertyName != null)
            {
                exception.Data.Add(IcalFileException.DataItemName.PropertyName, propertyName);
            }

            return exception;
        }

        internal static IcalFileException BuildInvalidTextEscapeException(long lineNumber, long physicalLineNumber, string propertyName)
        {
            return BuildException(ErrorMessage.InvalidTextEscape, lineNumber, physicalLineNumber, propertyName);
        }

        private IcalFileException BuildException(string message, int offsetInLine)
        {
            var physicalLineNumber = GetPhysicalLineNumber(_physicalLineNumber, _foldOffsets, _foldCount, offsetInLine);
            var propertyName = _nameLength > 0 ? new string(_buffer, _lineStart, _nameLength) : null;
            return BuildException(message, _lineNumber, physicalLineNumber, propertyName);
        }

        /// <summary>
        /// Builds an exception for a problem found while finding a line's boundaries, and faults the scanner, since where the line ends is
        /// then unknown.
        /// </summary>
        private IcalFileException Fault(string message)
        {
            IsFaulted = true;

            // The line has not been counted yet.
            return BuildException(message, _linesRead + 1, _physicalLinesRead + 1, null);
        }

        private void AddFoldOffset(int offset)
        {
            if (_foldCount == _foldOffsets.Length)
            {
                Array.Resize(ref _foldOffsets, _foldOffsets.Length * 2);
            }

            _foldOffsets[_foldCount++] = offset;
        }

        private void AddParameter(ParameterSlice parameter)
        {
            if (_parameterCount == _parameters.Length)
            {
                Array.Resize(ref _parameters, _parameters.Length * 2);
            }

            _parameters[_parameterCount++] = parameter;
        }

        private void AddParameterValue(int offsetInLine, int length, bool isQuoted)
        {
            if (_parameterValueCount == _parameterValues.Length)
            {
                Array.Resize(ref _parameterValues, _parameterValues.Length * 2);
            }

            _parameterValues[_parameterValueCount++] = new ValueSlice
            {
                Offset = _lineStart + offsetInLine,
                Length = length,
                IsQuoted = isQuoted
            };
        }

        /// <summary>
        /// Moves characters from the scan position to the end of the line, closing any gap left by folds removed earlier.
        /// </summary>
        private void AppendToLine(int count)
        {
            if (count == 0) return;

            if (_lineEnd - _lineStart + count > _maxLineLength)
            {
                throw Fault(string.Format(ErrorMessage.LineTooLongTemplate, _maxLineLength));
            }

            if (_lineEnd != _position)
            {
                Array.Copy(_buffer, _position, _buffer, _lineEnd, count);
            }

            _lineEnd += count;
            _position += count;
        }

        private ScanStatus CompleteLine()
        {
            _lineInProgress = false;
            _lineNumber = ++_linesRead;
            return ScanStatus.LineRead;
        }

        private ReadOnlyMemory<char> GetDecoded(DecodedSlice decoded)
        {
            return decoded.Source == DecodedInScratch
                ? new ReadOnlyMemory<char>(_scratch, decoded.Offset, decoded.Length)
                : new ReadOnlyMemory<char>(_buffer, decoded.Offset, decoded.Length);
        }

        private ParameterSlice GetParameter(int parameterIndex)
        {
            if ((uint)parameterIndex >= (uint)_parameterCount)
                throw new ArgumentOutOfRangeException(nameof(parameterIndex));

            return _parameters[parameterIndex];
        }

        private int GetParameterValueIndex(int parameterIndex, int valueIndex)
        {
            var parameter = GetParameter(parameterIndex);
            if ((uint)valueIndex >= (uint)parameter.ValueCount)
                throw new ArgumentOutOfRangeException(nameof(valueIndex));

            return parameter.FirstValue + valueIndex;
        }

        /// <summary>
        /// Gets the free part of the scratch buffer.  The buffer is sized for the whole line the first time it is needed for that line.  Decoding
        /// never lengthens a value and each value is decoded once, so it never has to grow while the line is current, and a decoded value taken
        /// out earlier stays valid.
        /// </summary>
        private Span<char> GetScratch()
        {
            var lineLength = _lineEnd - _lineStart;
            if (_scratchEnd == 0 && _scratch.Length < lineLength)
            {
                if (_scratch.Length > 0)
                {
                    ArrayPool<char>.Shared.Return(_scratch);
                }

                _scratch = ArrayPool<char>.Shared.Rent(lineLength);
            }

            return new Span<char>(_scratch, _scratchEnd, _scratch.Length - _scratchEnd);
        }

        private bool HasAvailable(int count)
        {
            return _bufferEnd - _position >= count;
        }

        /// <summary>
        /// Parses one parameter value, quoted or not, and checks the character after it.
        /// </summary>
        /// <returns>The index of the ',', ';' or ':' after the value.</returns>
        private int ParseParameterValue(ReadOnlySpan<char> line, int index)
        {
            int end;
            if (index < line.Length && line[index] == Quote)
            {
                // ref. RFC 5545§3.1: quoted-string = DQUOTE *QSAFE-CHAR DQUOTE
                var start = index + 1;
                var stopIndex = IndexOfQuotedStringStop(line.Slice(start));
                if (stopIndex < 0)
                {
                    throw BuildException(ErrorMessage.UnterminatedQuotedString, line.Length);
                }

                end = start + stopIndex;
                if (line[end] != Quote)
                {
                    throw BuildException(string.Format(ErrorMessage.InvalidParameterValueCharacterTemplate, (int)line[end]), end);
                }

                AddParameterValue(start, end - start, true);
                end++;
                if (end == line.Length || !IsParameterValueEnd(line[end]))
                {
                    throw BuildException(ErrorMessage.ExpectedParameterValueEnd, end);
                }

                return end;
            }

            // ref. RFC 5545§3.1: paramtext = *SAFE-CHAR
            var textStopIndex = IndexOfParameterTextStop(line.Slice(index));
            if (textStopIndex < 0)
            {
                throw BuildException(ErrorMessage.MissingValue, line.Length);
            }

            end = index + textStopIndex;
            if (!IsParameterValueEnd(line[end]))
            {
                throw BuildException(string.Format(ErrorMessage.InvalidParameterValueCharacterTemplate, (int)line[end]), end);
            }

            AddParameterValue(index, end - index, false);
            return end;
        }

        private ScanStatus RequestInput(int count)
        {
            _requiredCount = count;
            return ScanStatus.NeedMoreInput;
        }

        private static bool IsFoldWhiteSpace(char character)
        {
            return character == ' ' || character == '\t';
        }

        private static bool IsParameterValueEnd(char character)
        {
            return character == ',' || character == ';' || character == ':';
        }

#if NET8_0_OR_GREATER
        private static string GetControlCharacters(string additionalCharacters)
        {
            var characters = new List<char>();
            for (var character = '\0'; character < ' '; character++)
            {
                if (character != '\t') characters.Add(character);
            }

            characters.Add('\x7F');
            characters.AddRange(additionalCharacters);
            return new string(characters.ToArray());
        }

        private static int IndexOfNameEnd(ReadOnlySpan<char> line, int start)
        {
            var index = line.Slice(start).IndexOfAnyExcept(NameCharacters);
            return index < 0 ? line.Length : start + index;
        }

        private static int IndexOfControlCharacter(ReadOnlySpan<char> value)
        {
            return value.IndexOfAny(ControlCharacters);
        }

        private static int IndexOfParameterTextStop(ReadOnlySpan<char> value)
        {
            return value.IndexOfAny(ParameterTextStopCharacters);
        }

        private static int IndexOfQuotedStringStop(ReadOnlySpan<char> value)
        {
            return value.IndexOfAny(QuotedStringStopCharacters);
        }
#else
        private static bool IsControlCharacter(char character)
        {
            // ref. RFC 5545§3.1: CONTROL = %x00-08 / %x0A-1F / %x7F
            return (character < ' ' && character != '\t') || character == '\x7F';
        }

        private static bool IsNameCharacter(char character)
        {
            // ref. RFC 5545§3.1: iana-token = 1*(ALPHA / DIGIT / "-"); x-name uses the same characters.
            return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z') || (character >= '0' && character <= '9') || character == '-';
        }

        private static int IndexOfNameEnd(ReadOnlySpan<char> line, int start)
        {
            var index = start;
            while (index < line.Length && IsNameCharacter(line[index]))
            {
                index++;
            }

            return index;
        }

        private static int IndexOfControlCharacter(ReadOnlySpan<char> value)
        {
            for (var index = 0; index < value.Length; index++)
            {
                if (IsControlCharacter(value[index])) return index;
            }

            return -1;
        }

        private static int IndexOfParameterTextStop(ReadOnlySpan<char> value)
        {
            // ref. RFC 5545§3.1: SAFE-CHAR excludes the control characters, DQUOTE, ";", ":" and ",".
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (IsControlCharacter(character) || character == Quote || IsParameterValueEnd(character)) return index;
            }

            return -1;
        }

        private static int IndexOfQuotedStringStop(ReadOnlySpan<char> value)
        {
            // ref. RFC 5545§3.1: QSAFE-CHAR excludes the control characters and DQUOTE.
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (IsControlCharacter(character) || character == Quote) return index;
            }

            return -1;
        }
#endif

        private struct DecodedSlice
        {
            internal DecodedSlice(byte source, int offset, int length)
            {
                Source = source;
                Offset = offset;
                Length = length;
            }

            internal readonly byte Source;
            internal readonly int Offset;
            internal readonly int Length;
        }

        private struct ParameterSlice
        {
            internal int NameOffset;
            internal int NameLength;
            internal int FirstValue;
            internal int ValueCount;
        }

        private struct ValueSlice
        {
            internal int Offset;
            internal int Length;
            internal bool IsQuoted;
            internal DecodedSlice Decoded;
        }
    }
}
