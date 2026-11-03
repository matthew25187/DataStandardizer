using System;
using System.Buffers;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Finds and parses the fields of a header section (RFC 5322 §2.2) in a pooled byte block.  It does no I/O: when it needs more input it says
    /// so, and a driver refills the block and calls it again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A whole field, folds included, is kept in one contiguous run of the block, which is never changed, so its raw bytes stay available.  On
    /// refill the block is compacted from the start of the current field, and a field longer than half the block makes the scanner rent a larger
    /// one.  Scanning resumes where it stopped, so the work is linear in the input however it is split.
    /// </para>
    /// <para>
    /// The raw bytes, the name and the value are held as slices of the block.  The value of a folded field is unfolded only when asked for, into
    /// a pooled scratch buffer.
    /// </para>
    /// </remarks>
    internal sealed class ImfHeaderScanner
    {
        internal enum ScanStatus
        {
            FieldRead,
            NeedMoreInput,
            EndOfHeader,
            EndOfInput
        }

        private static class ErrorMessage
        {
            internal const string BareCarriageReturn = "Found a carriage return without a line feed.  RFC 5322 §2.2 allows CR only in a CRLF line break.";
            internal const string BareLineFeed = "Found a line feed without a carriage return.  RFC 5322 §2.2 allows LF only in a CRLF line break.";
            internal const string ExpectedColon = "Expected ':' after the field name.  A field name holds only printable US-ASCII characters (RFC 5322 §3.6.8).";
            internal const string ExpectedFieldName = "Expected a field name at the start of the header field (RFC 5322 §3.6.8).";
            internal const string InvalidUtf8 = "Found bytes in the field body that are not valid UTF-8 (RFC 6532 §3.2).";
            internal const string LineTooLong = "The line is longer than the maximum of 998 octets (RFC 5322 §2.1.1).";
            internal const string TooManyFieldsTemplate = "The header section has more than the maximum of {0} fields.";
            internal const string SectionTooLongTemplate = "The header section is longer than the maximum of {0} octets.";
            internal const string UnterminatedField = "The input ended inside a header field.  RFC 5322 §3.6.8 requires a field to end with CRLF.";
        }

        private const byte CarriageReturn = 0x0D;
        private const byte Colon = 0x3A;
        private const byte LineFeed = 0x0A;
        private const byte Space = 0x20;
        private const byte Tab = 0x09;

        // ref. RFC 5322 §2.1.1
        private const int MaxLineLength = 998;

        // The most a byte array can hold.
        private const int MaxBufferLength = 0x7FFFFFC7;

        private static readonly byte[] EmptyBytes = new byte[0];

        private readonly bool _acceptBareLineFeed;
        private readonly bool _allowEightBitHeaders;
        private readonly bool _allowLongLines;
        private readonly int _bufferSize;
        private readonly IMimeCharsetResolver _charsetResolver;
        private readonly MimeEncodedWordParseOptions _encodedWordOptions;
        private readonly Encoding _fallbackEncoding;
        private readonly bool _lineFeedIsLineBreak;
        private readonly int _maxFieldCount;
        private readonly int _maxSectionLength;

        private byte[] _buffer = EmptyBytes;
        private int _bufferEnd;
        private int _bufferLength;
        private long _bytePosition;
        private int _contentEnd;
        private bool _endOfInput;
        private long _fieldBytePosition;
        private int _fieldEnd;
        private bool _fieldInProgress;
        private long _fieldLineNumber;
        private long _fieldsRead;
        private int _fieldStart;
        private bool _isFolded;
        private int _lineStart;
        private int _nameLength;
        private bool _needsFallbackCharset;
        private long _physicalLinesRead;
        private int _position;
        private int _scanPosition;
        private byte[] _scratch = EmptyBytes;
        private ScanStatus _sectionEnd = ScanStatus.FieldRead;
        private int _sectionEndLineBreakLength;
        private int _sectionFieldCount;
        private long _sectionStart;
        private int _unfoldedLength = -1;
        private int _valueStart;

        /// <param name="bufferSize">The initial size, in bytes, of the block.</param>
        /// <param name="options">The options.</param>
        /// <param name="fallbackEncoding">The encoding for a field body that is not valid UTF-8.  Used only if the options allow such a field.</param>
        /// <param name="lineFeedIsLineBreak"><c>true</c> if a line feed is the line break, as in the messages inside mbox and EMLX files, which is
        /// those formats' own rule and not a leniency.  A carriage return before the line feed is then part of the line.</param>
        internal ImfHeaderScanner(int bufferSize, ImfOptions options, Encoding fallbackEncoding, bool lineFeedIsLineBreak)
        {
            _bufferSize = bufferSize;
            _charsetResolver = options.CharsetResolver;
            _encodedWordOptions = ImfHeaderText.GetWordOptions(options);
            _acceptBareLineFeed = options.AcceptBareLineFeed;
            _allowEightBitHeaders = options.AllowEightBitHeaders;
            _allowLongLines = options.AllowLongLines;
            _fallbackEncoding = fallbackEncoding;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
            _maxFieldCount = options.MaxHeaderFieldCount;
            _maxSectionLength = options.MaxHeaderSectionLength;
        }

        /// <summary>
        /// Gets the block a driver reads input into, at the position given by <see cref="PrepareRefill"/>.
        /// </summary>
        internal byte[] Buffer => _buffer;

        /// <summary>
        /// Gets the index in <see cref="Buffer"/> of the end of the input read so far.
        /// </summary>
        internal int BufferEnd => _bufferEnd;

        /// <summary>
        /// Gets the number of bytes consumed so far: the fields found, and the empty line that ends the section once it has been found.
        /// </summary>
        internal long BytePosition => _bytePosition;

        /// <summary>
        /// Gets the byte position of the first byte of the current field.
        /// </summary>
        internal long FieldBytePosition => _fieldBytePosition;

        /// <summary>
        /// Gets the 1-based number of the physical line on which the current field starts.
        /// </summary>
        internal long FieldLineNumber => _fieldLineNumber;

        /// <summary>
        /// Gets the number of fields found so far, including any that failed to parse.
        /// </summary>
        internal long FieldsRead => _fieldsRead;

        /// <summary>
        /// Gets a flag indicating that a line break or limit error was found, after which the field boundaries are unknown.
        /// </summary>
        internal bool IsFaulted { get; private set; }

        /// <summary>
        /// Gets a flag indicating that the driver has reported the end of the input.
        /// </summary>
        internal bool IsEndOfInput => _endOfInput;

        /// <summary>
        /// Gets the index in <see cref="Buffer"/> of the first byte not yet consumed.
        /// </summary>
        internal int Position => _position;

        /// <summary>
        /// Gets the length of the line break of the empty line that ended the section, or 0 if the input ended the section.
        /// </summary>
        internal int SectionEndLineBreakLength => _sectionEndLineBreakLength;

        /// <summary>
        /// Gets a flag indicating that the body of the current field is not valid UTF-8.
        /// </summary>
        internal bool NeedsFallbackCharset => _needsFallbackCharset;

        /// <summary>
        /// Gets the number of physical lines consumed so far.
        /// </summary>
        internal long PhysicalLinesRead => _physicalLinesRead;

        /// <summary>
        /// Gets the whole of the current field as it appears in the input, with its folds and its final line break.
        /// </summary>
        internal ReadOnlyMemory<byte> RawBytes => new ReadOnlyMemory<byte>(_buffer, _fieldStart, _fieldEnd - _fieldStart);

        /// <summary>
        /// Gets the name of the current field.
        /// </summary>
        internal ReadOnlyMemory<byte> Name => new ReadOnlyMemory<byte>(_buffer, _fieldStart, _nameLength);

        /// <summary>
        /// Gets the body of the current field, unfolded.  A folded body is unfolded once per field and kept.
        /// </summary>
        internal ReadOnlyMemory<byte> Value
        {
            get
            {
                if (!_isFolded) return new ReadOnlyMemory<byte>(_buffer, _valueStart, _contentEnd - _valueStart);

                EnsureUnfolded();
                return new ReadOnlyMemory<byte>(_scratch, 0, _unfoldedLength);
            }
        }

        /// <summary>
        /// Finds the next field.
        /// </summary>
        /// <returns>
        /// <see cref="ScanStatus.FieldRead"/> when a whole field is in the block, and the driver should call <see cref="ParseField"/>;
        /// <see cref="ScanStatus.NeedMoreInput"/> when the driver must call <see cref="PrepareRefill"/> and <see cref="CompleteRefill"/>, then
        /// call this method again; <see cref="ScanStatus.EndOfHeader"/> when the empty line that ends the section has been consumed;
        /// <see cref="ScanStatus.EndOfInput"/> when the input ended with no such line.  Once the section has ended, every call gives the same
        /// result.
        /// </returns>
        /// <exception cref="ImfFileException">A line break that is not accepted, a line that is too long, a limit that is passed, or the end of the
        /// input inside a field.  The scanner is then faulted.</exception>
        internal ScanStatus Scan()
        {
            // Every early return leaves the state as it was at a point the scan can restart from.
            if (_sectionEnd != ScanStatus.FieldRead) return _sectionEnd;

            if (!_fieldInProgress)
            {
                if (_position == _bufferEnd)
                {
                    return _endOfInput ? EndSection(ScanStatus.EndOfInput) : ScanStatus.NeedMoreInput;
                }

                _fieldStart = _lineStart = _scanPosition = _position;
                _fieldBytePosition = _bytePosition;
                _fieldLineNumber = _physicalLinesRead + 1;
                _isFolded = false;
                _fieldInProgress = true;
            }

            while (true)
            {
                var unread = new ReadOnlySpan<byte>(_buffer, _scanPosition, _bufferEnd - _scanPosition);
                var breakIndex = unread.IndexOfAny(CarriageReturn, LineFeed);
                if (breakIndex < 0)
                {
                    CheckLine(_bufferEnd, _bufferEnd);
                    _scanPosition = _bufferEnd;
                    if (_endOfInput) throw Fault(ErrorMessage.UnterminatedField, _bufferEnd);

                    return ScanStatus.NeedMoreInput;
                }

                _scanPosition += breakIndex;
                var contentEnd = _scanPosition;
                var breakEnd = _scanPosition + 1;
                if (_buffer[_scanPosition] == CarriageReturn)
                {
                    if (breakEnd == _bufferEnd)
                    {
                        CheckLine(contentEnd, contentEnd);
                        if (_endOfInput) throw Fault(ErrorMessage.BareCarriageReturn, contentEnd);

                        return ScanStatus.NeedMoreInput;
                    }

                    if (_buffer[breakEnd] != LineFeed) throw Fault(ErrorMessage.BareCarriageReturn, contentEnd);

                    breakEnd++;
                    if (_lineFeedIsLineBreak)
                    {
                        // The carriage return is part of the line, as only the line feed is a line break.
                        contentEnd++;
                    }
                }
                else if (!_lineFeedIsLineBreak && !_acceptBareLineFeed)
                {
                    throw Fault(ErrorMessage.BareLineFeed, contentEnd);
                }

                CheckLine(contentEnd, breakEnd);

                // ref. RFC 5322 §2.1: the empty line needs no look ahead, as the bytes after it are the body, even if they start with white space.
                if (contentEnd == _fieldStart)
                {
                    _physicalLinesRead++;
                    _bytePosition = _fieldBytePosition + (breakEnd - _fieldStart);
                    _position = breakEnd;
                    _fieldInProgress = false;
                    _sectionEndLineBreakLength = breakEnd - contentEnd;
                    return EndSection(ScanStatus.EndOfHeader);
                }

                // Look past the line break for the white space that makes it a fold.
                if (breakEnd == _bufferEnd && !_endOfInput) return ScanStatus.NeedMoreInput;

                _physicalLinesRead++;
                if (breakEnd < _bufferEnd && IsWhiteSpace(_buffer[breakEnd]))
                {
                    // ref. RFC 5322 §2.2.3: a line break followed by white space is a fold.
                    _isFolded = true;
                    _lineStart = _scanPosition = breakEnd;
                    continue;
                }

                return CompleteField(contentEnd, breakEnd);
            }
        }

        /// <summary>
        /// Starts a new section at the current position, whose fields and length are counted afresh.  Lets the entity reader read the header
        /// section of each entity of a message from the one block.
        /// </summary>
        internal void BeginSection()
        {
            _sectionEnd = ScanStatus.FieldRead;
            _sectionEndLineBreakLength = 0;
            _sectionFieldCount = 0;
            _sectionStart = _bytePosition;
        }

        /// <summary>
        /// Consumes bytes that are not part of a header section, as the entity reader does with the body of an entity.
        /// </summary>
        /// <param name="end">The index in <see cref="Buffer"/> of the end of the bytes to consume.</param>
        internal void ConsumeBody(int end)
        {
            var consumed = new ReadOnlySpan<byte>(_buffer, _position, end - _position);
            int lineFeedIndex;
            while ((lineFeedIndex = consumed.IndexOf(LineFeed)) >= 0)
            {
                _physicalLinesRead++;
                consumed = consumed.Slice(lineFeedIndex + 1);
            }

            _bytePosition += end - _position;
            _position = end;
        }

        /// <summary>
        /// Builds an exception for a problem in the meaning of the current field, whose syntax has been found to be sound.
        /// </summary>
        /// <param name="message">The message.</param>
        internal ImfFileException BuildCurrentFieldException(string message)
        {
            return BuildFieldException(message, 0);
        }

        /// <summary>
        /// Builds an exception for a problem at the end of the bytes consumed so far.
        /// </summary>
        /// <param name="message">The message.</param>
        internal ImfFileException BuildPositionException(string message)
        {
            return BuildException(message, _bytePosition, _physicalLinesRead + 1, null);
        }

        /// <summary>
        /// Makes room in the block for at least one more byte.
        /// </summary>
        /// <param name="offset">The index in <see cref="Buffer"/> to read into.</param>
        /// <param name="count">The number of bytes that may be read.</param>
        internal void PrepareRefill(out int offset, out int count)
        {
            var retainStart = _fieldInProgress ? _fieldStart : _position;
            var retainedLength = _bufferEnd - retainStart;

            var capacity = _bufferLength;
            if (capacity == retainedLength || retainedLength > capacity / 2)
            {
                capacity = (int)Math.Min(Math.Max(Math.Max(_bufferSize, retainedLength + 1L), capacity * 2L), MaxBufferLength);
            }

            var target = capacity == _bufferLength ? _buffer : ArrayPool<byte>.Shared.Rent(capacity);
            if (retainedLength > 0 && (target != _buffer || retainStart != 0))
            {
                Array.Copy(_buffer, retainStart, target, 0, retainedLength);
            }

            if (target != _buffer)
            {
                if (_bufferLength > 0)
                {
                    ArrayPool<byte>.Shared.Return(_buffer);
                }

                _buffer = target;
                _bufferLength = capacity;
            }

            _position -= retainStart;
            _bufferEnd = retainedLength;
            if (_fieldInProgress)
            {
                _fieldStart = 0;
                _lineStart -= retainStart;
                _scanPosition -= retainStart;
            }

            offset = _bufferEnd;
            count = _bufferLength - _bufferEnd;
        }

        /// <summary>
        /// Records the result of reading into the block.
        /// </summary>
        /// <param name="bytesRead">The number of bytes read, or 0 at the end of the input.</param>
        internal void CompleteRefill(int bytesRead)
        {
            if (bytesRead == 0)
            {
                _endOfInput = true;
            }
            else
            {
                _bufferEnd += bytesRead;
            }
        }

        /// <summary>
        /// Splits the field found by <see cref="Scan"/> into its name and value, and checks them.
        /// </summary>
        /// <exception cref="ImfFileException">The field does not follow the syntax of RFC 5322 §3.6.8 and §4.5, or its body is not valid UTF-8 and
        /// that is not accepted.  The field has been consumed, so the next field can still be read.</exception>
        internal void ParseField()
        {
            _nameLength = 0;
            _needsFallbackCharset = false;
            _unfoldedLength = -1;

            var field = new ReadOnlySpan<byte>(_buffer, _fieldStart, _contentEnd - _fieldStart);

            // ref. RFC 5322 §3.6.8: field-name = 1*ftext
            var index = 0;
            while (index < field.Length && IsFieldNameByte(field[index]))
            {
                index++;
            }

            if (index == 0) throw BuildFieldException(ErrorMessage.ExpectedFieldName, 0);

            _nameLength = index;

            // ref. RFC 5322 §4.5.8: obs-optional = field-name *WSP ":" unstructured CRLF.  Only WSP is allowed there, not a fold.
            while (index < field.Length && IsWhiteSpace(field[index]))
            {
                index++;
            }

            if (index == field.Length || field[index] != Colon) throw BuildFieldException(ErrorMessage.ExpectedColon, index);

            index++;
            _valueStart = _fieldStart + index;

            // ref. RFC 6532 §3.2: the field body may hold UTF-8.  CR and LF are ASCII, so folds in the body don't need to be taken out first.
            var invalidIndex = IndexOfInvalidUtf8(field.Slice(index));
            if (invalidIndex >= 0)
            {
                if (!_allowEightBitHeaders) throw BuildFieldException(ErrorMessage.InvalidUtf8, index + invalidIndex);

                _needsFallbackCharset = true;
            }
        }

        /// <summary>
        /// Decodes the name of the current field, which <see cref="ParseField"/> has checked is ASCII.
        /// </summary>
        internal string GetName()
        {
            return Encoding.UTF8.GetString(_buffer, _fieldStart, _nameLength);
        }

        /// <summary>
        /// Decodes the unfolded body of the current field: as UTF-8, or with the fallback encoding if it is not valid UTF-8.
        /// </summary>
        internal string GetValue()
        {
            var encoding = _needsFallbackCharset ? _fallbackEncoding : Encoding.UTF8;
            if (!_isFolded) return encoding.GetString(_buffer, _valueStart, _contentEnd - _valueStart);

            EnsureUnfolded();
            return encoding.GetString(_scratch, 0, _unfoldedLength);
        }

        /// <summary>
        /// Decodes the unfolded body of the current field as an unstructured field.  Encoded words are read after any fallback encoding has been applied.
        /// </summary>
        /// <exception cref="InvalidOperationException">The options have no character set resolver.</exception>
        internal string GetDecodedValue()
        {
            if (_charsetResolver is null)
                throw new InvalidOperationException("A character set resolver is required to decode encoded words.");

            return ImfHeaderText.DecodeUnstructuredCore(GetValue(), _encodedWordOptions, _charsetResolver);
        }

        /// <summary>
        /// Returns the pooled blocks.  The scanner must not be used afterwards.
        /// </summary>
        internal void ReturnBuffers()
        {
            if (_bufferLength > 0)
            {
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = EmptyBytes;
                _bufferLength = 0;
            }

            if (_scratch.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_scratch);
                _scratch = EmptyBytes;
            }
        }

        /// <summary>
        /// Checks the limits that apply as a line is scanned, so a hostile line can't fill memory before its end is found.
        /// </summary>
        /// <param name="contentEnd">The index in the block of the end of what is known of the current line, without its line break.</param>
        /// <param name="sectionEnd">The index in the block of the end of what is known to belong to the section.</param>
        private void CheckLine(int contentEnd, int sectionEnd)
        {
            if (!_allowLongLines && contentEnd - _lineStart > MaxLineLength)
            {
                throw Fault(ErrorMessage.LineTooLong, _lineStart + MaxLineLength);
            }

            var fieldOffset = _fieldBytePosition - _sectionStart;
            var sectionLength = fieldOffset + (sectionEnd - _fieldStart);
            if (sectionLength > _maxSectionLength)
            {
                throw Fault(string.Format(ErrorMessage.SectionTooLongTemplate, _maxSectionLength), _fieldStart + (int)(_maxSectionLength - fieldOffset));
            }
        }

        private ScanStatus CompleteField(int contentEnd, int fieldEnd)
        {
            _contentEnd = contentEnd;
            _fieldEnd = fieldEnd;
            _position = fieldEnd;
            _bytePosition = _fieldBytePosition + (fieldEnd - _fieldStart);
            _fieldInProgress = false;
            _fieldsRead++;
            _sectionFieldCount++;

            if (_sectionFieldCount > _maxFieldCount)
            {
                IsFaulted = true;
                throw BuildException(string.Format(ErrorMessage.TooManyFieldsTemplate, _maxFieldCount), _fieldBytePosition, _fieldLineNumber, null);
            }

            return ScanStatus.FieldRead;
        }

        private ScanStatus EndSection(ScanStatus status)
        {
            _sectionEnd = status;
            return status;
        }

        /// <summary>
        /// Unfolds the body of the current field into the scratch buffer, the first time it is asked for.
        /// </summary>
        private void EnsureUnfolded()
        {
            if (_unfoldedLength >= 0) return;

            var source = new ReadOnlySpan<byte>(_buffer, _valueStart, _contentEnd - _valueStart);
            if (_scratch.Length < source.Length)
            {
                if (_scratch.Length > 0)
                {
                    ArrayPool<byte>.Shared.Return(_scratch);
                }

                _scratch = ArrayPool<byte>.Shared.Rent(source.Length);
            }

            // ref. RFC 5322 §2.2.3: unfold by removing each line break, and keeping the white space after it.  Scanning has checked that every
            // line feed in the field is a line break.
            var target = new Span<byte>(_scratch);
            var length = 0;
            int lineFeedIndex;
            while ((lineFeedIndex = source.IndexOf(LineFeed)) >= 0)
            {
                var contentLength = !_lineFeedIsLineBreak && lineFeedIndex > 0 && source[lineFeedIndex - 1] == CarriageReturn
                    ? lineFeedIndex - 1
                    : lineFeedIndex;
                source.Slice(0, contentLength).CopyTo(target.Slice(length));
                length += contentLength;
                source = source.Slice(lineFeedIndex + 1);
            }

            source.CopyTo(target.Slice(length));
            _unfoldedLength = length + source.Length;
        }

        /// <summary>
        /// Builds an exception for a problem found while finding a field's boundaries, and faults the scanner, since where the field ends is then
        /// unknown.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="index">The index in the block of the byte at fault.</param>
        private ImfFileException Fault(string message, int index)
        {
            IsFaulted = true;
            return BuildException(message, _fieldBytePosition + (index - _fieldStart), _physicalLinesRead + 1, null);
        }

        /// <summary>
        /// Builds an exception for a problem in the current field, whose boundaries are known.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="offset">The offset from the start of the field of the byte at fault.</param>
        private ImfFileException BuildFieldException(string message, int offset)
        {
            // Each line feed before the byte at fault is a line break, as scanning has checked.
            var lineNumber = _fieldLineNumber;
            var preceding = new ReadOnlySpan<byte>(_buffer, _fieldStart, offset);
            int lineFeedIndex;
            while ((lineFeedIndex = preceding.IndexOf(LineFeed)) >= 0)
            {
                lineNumber++;
                preceding = preceding.Slice(lineFeedIndex + 1);
            }

            var fieldName = _nameLength > 0 ? GetName() : null;
            return BuildException(message, _fieldBytePosition + offset, lineNumber, fieldName);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static ImfFileException BuildException(string message, long bytePosition, long lineNumber, string? fieldName)
#else
        private static ImfFileException BuildException(string message, long bytePosition, long lineNumber, string fieldName)
#endif
        {
            var exception = new ImfFileException(message);
            exception.Data.Add(ImfFileException.DataItemName.BytePosition, bytePosition);
            exception.Data.Add(ImfFileException.DataItemName.LineNumber, lineNumber);
            if (fieldName != null)
            {
                exception.Data.Add(ImfFileException.DataItemName.FieldName, fieldName);
            }

            return exception;
        }

        /// <summary>
        /// Finds the first byte that is not part of a well-formed UTF-8 sequence (RFC 3629 §4), which excludes overlong forms, surrogates and code
        /// points above U+10FFFF.
        /// </summary>
        /// <returns>The index of the first byte of the invalid sequence, or -1 if all of the bytes are valid.</returns>
        internal static int IndexOfInvalidUtf8(ReadOnlySpan<byte> bytes)
        {
            var index = 0;
            while (index < bytes.Length)
            {
                var first = bytes[index];
                if (first < 0x80)
                {
                    index++;
                    continue;
                }

                int sequenceLength;
                byte secondMinimum = 0x80, secondMaximum = 0xBF;
                if (first >= 0xC2 && first <= 0xDF)
                {
                    sequenceLength = 2;
                }
                else if (first >= 0xE0 && first <= 0xEF)
                {
                    sequenceLength = 3;
                    if (first == 0xE0) secondMinimum = 0xA0;
                    if (first == 0xED) secondMaximum = 0x9F;
                }
                else if (first >= 0xF0 && first <= 0xF4)
                {
                    sequenceLength = 4;
                    if (first == 0xF0) secondMinimum = 0x90;
                    if (first == 0xF4) secondMaximum = 0x8F;
                }
                else
                {
                    return index;
                }

                if (index + sequenceLength > bytes.Length) return index;

                var second = bytes[index + 1];
                if (second < secondMinimum || second > secondMaximum) return index;

                for (var continuation = 2; continuation < sequenceLength; continuation++)
                {
                    if ((bytes[index + continuation] & 0xC0) != 0x80) return index;
                }

                index += sequenceLength;
            }

            return -1;
        }

        private static bool IsFieldNameByte(byte value)
        {
            // ref. RFC 5322 §3.6.8: ftext = %d33-57 / %d59-126
            return value >= 33 && value <= 126 && value != Colon;
        }

        private static bool IsWhiteSpace(byte value)
        {
            return value == Space || value == Tab;
        }
    }
}
