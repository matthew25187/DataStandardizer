using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Finds the messages of an mbox file in a pooled byte block, and gives the bytes of each with its quotes removed.  It does no I/O: when it needs more input it says so, and a driver
    /// refills the block and calls it again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The block has a fixed size, so a mailbox of any size takes the same memory.  A line is looked at only for as long as it could be a separator line or a Content-Length field, which
    /// can't be longer than a line may be (RFC 5322 §2.1.1), and the rest of it is passed on as it arrives.
    /// </para>
    /// <para>
    /// The line break at the end of each line is held back until the next line has been looked at, as the line break of an empty line before a separator line is not part of the message.
    /// </para>
    /// </remarks>
    internal sealed class MboxScanner
    {
        internal enum Status
        {
            NeedMoreInput,
            MessageFound,
            Content,
            EndOfMessage,
            EndOfInput
        }

        private enum Phase
        {
            // Before the first separator line.
            FirstSeparator,
            // At the start of a line of a message whose end is found from its lines.
            LineStart,
            // In a run of '>' at the start of a line.
            Quotes,
            // Passing on the rest of a line.
            InLine,
            // Passing on a body of a length given by its Content-Length field.
            CountedBody,
            // Checking what follows that body.
            Terminator,
            // The message has ended, and the next separator line has been read, if there is one.
            MessageEnded,
            // There are no more messages.
            NoMoreMessages
        }

        private enum LineMatch
        {
            NeedMoreInput,
            No,
            Invalid,
            Yes
        }

        private static class ErrorMessage
        {
            internal const string ContentLengthInvalid = "The Content-Length field is missing, is not a single number, or appears more than once, so the end of the message can't be found.  ImfOptions.AllowInvalidMboxContentLength doesn't rely on it.";
            internal const string ContentLengthMismatch = "The body of the length given by the Content-Length field isn't followed by an empty line and then a separator line or the end of the file.  ImfOptions.AllowInvalidMboxContentLength doesn't rely on it.";
            internal const string ContentLengthTooLong = "The file ended before the end of the body of the length given by the Content-Length field.  ImfOptions.AllowInvalidMboxContentLength doesn't rely on it.";
            internal const string ContentLengthDiagnostic = "The Content-Length field is missing or not valid, or doesn't give the length of the body.";
            internal const string InvalidSeparator = "The line starts with \"From \" and doesn't have the form of a separator line (RFC 4155 Appendix A), so it needs a quote.  ImfOptions.AllowInvalidMboxSeparator accepts it.";
            internal const string InvalidSeparatorDiagnostic = "The line starts with \"From \" and doesn't have the form of a separator line that RFC 4155 Appendix A requires.";
            internal const string MissingBlankLine = "The separator line doesn't follow an empty line, which ends each message (RFC 4155 Appendix A).  ImfOptions.AllowMissingMboxBlankLine accepts it.";
            internal const string MissingBlankLineDiagnostic = "The message isn't ended by the empty line that RFC 4155 Appendix A requires.";
            internal const string CarriageReturnLineFeed = "The line ends in a carriage return and a line feed, which RFC 4155 Appendix A doesn't allow.  ImfOptions.AllowMboxCarriageReturnLineFeed accepts it.";
            internal const string CarriageReturnLineFeedDiagnostic = "The line ends in a carriage return and a line feed, which RFC 4155 Appendix A doesn't allow.";
            internal const string EightBit = "The message holds an octet above 127, which RFC 4155 Appendix A doesn't allow.  ImfOptions.AllowEightBitMboxContent accepts it.";
            internal const string EightBitDiagnostic = "The message holds an octet above 127, which RFC 4155 Appendix A doesn't allow.";
            internal const string MissingFinalBlankLine = "The file ended with no empty line to end the message (RFC 4155 Appendix A).  ImfOptions.AllowMissingMboxBlankLine accepts it.";
            internal const string MissingSeparator = "The file doesn't start with a separator line (RFC 4155 Appendix A).  ImfOptions.AllowMissingMboxSeparator skips what comes before the first.";
            internal const string MissingSeparatorDiagnostic = "The file doesn't start with a separator line, so what comes before the first has been skipped (RFC 4155 Appendix A).";
            internal const string TooManyMessagesTemplate = "The file has more than the maximum of {0} messages.";
        }

        private const int BlockSize = 16 * 1024;

        // ref. RFC 5322 §2.1.1.  The two octets are the line break.
        private const int MaxLineWindow = 998 + 2;

        private const byte CarriageReturn = 0x0D;
        private const byte Colon = 0x3A;
        private const byte HighestSevenBitOctet = 0x7F;
        private const byte LineFeed = 0x0A;
        private const byte Quote = 0x3E;
        private const byte Space = 0x20;
        private const byte Tab = 0x09;

        private const int FromLength = 5;
        private const int MaxHeldBytes = 4;
        private const string ContentLengthName = "content-length";

        // More digits than this could overflow, and no file is this long.
        private const int MaxContentLengthDigits = 18;

        private static readonly byte[] From = { (byte)'F', (byte)'r', (byte)'o', (byte)'m', Space };

        private readonly bool _allowCarriageReturnLineFeed;
        private readonly bool _allowEightBit;
        private readonly bool _allowInvalidContentLength;
        private readonly bool _allowInvalidSeparator;
        private readonly bool _allowMissingBlankLine;
        private readonly bool _allowMissingSeparator;
        private readonly int _maxMessageCount;
        private readonly bool _usesContentLength;
        private readonly MboxVariant _variant;

        // What has been decided on and not yet passed on: at most the held line break and the line break of an empty line.
        private readonly byte[] _queue = new byte[MaxHeldBytes];

        private byte[] _buffer;
        private bool _checksContent;
        private byte _previousOctet;
        private bool _reportedCarriageReturnLineFeed;
        private bool _reportedEightBit;
        private int _bufferEnd;
        private long _bodyBytes;
        private long _bytePosition;
        private long _contentLength;
        private bool _endOfInput;
        private bool _hasContentLength;
        private bool _hasMoveStarted;
        private int _heldLength;
        private bool _heldIsHeaderEnd;
        private bool _inHeader;
        private bool _invalidContentLength;
        private bool _isCrLf;
        private bool _isLineBreakKnown;
        private bool _lastEndsLine;
        private byte _lastOctet;
        private long _lineNumber = 1;
        private long _messagesFound;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private MboxEnvelope? _nextEnvelope;
#else
        private MboxEnvelope _nextEnvelope;
#endif
        private long _nextEnvelopeBytePosition;
        private long _nextEnvelopeLineNumber;
        private Phase _phase = Phase.FirstSeparator;
        private int _position;
        private bool _previousLineWasEmpty;
        private long _quoteCount;
        private long _quotesToWrite;
        private int _queueLength;
        private int _queueStart;
        private bool _reportedInvalidSeparator;
        private bool _reportedMissingSeparator;
        private long _remainingBody;
        private bool _skippingLine;

        internal MboxScanner(MboxVariant variant, ImfOptions options)
        {
            _variant = variant;
            _usesContentLength = variant == MboxVariant.Mboxcl || variant == MboxVariant.Mboxcl2;
            _allowCarriageReturnLineFeed = options.AllowMboxCarriageReturnLineFeed;
            _allowEightBit = options.AllowEightBitMboxContent;
            _allowInvalidContentLength = options.AllowInvalidMboxContentLength;
            _allowInvalidSeparator = options.AllowInvalidMboxSeparator;
            _allowMissingBlankLine = options.AllowMissingMboxBlankLine;
            _allowMissingSeparator = options.AllowMissingMboxSeparator;
            _maxMessageCount = options.MaxMessageCount;
            _buffer = ArrayPool<byte>.Shared.Rent(BlockSize);
        }

        /// <summary>
        /// Gets the block a driver reads input into, at the position given by <see cref="PrepareRefill"/>.
        /// </summary>
        internal byte[] Buffer => _buffer;

        /// <summary>
        /// Gets the number of bytes of the file consumed so far.
        /// </summary>
        internal long BytePosition => _bytePosition;

        /// <summary>
        /// Gets the problems with the format of the file found in the current message, and before it.
        /// </summary>
        internal List<ImfDiagnostic> Diagnostics { get; } = new List<ImfDiagnostic>();

        /// <summary>
        /// Gets the separator line of the current message.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal MboxEnvelope? Envelope { get; private set; }
#else
        internal MboxEnvelope Envelope { get; private set; }
#endif

        /// <summary>
        /// Gets a flag indicating if the line breaks of the file are CRLF.  This is known from the first separator line.
        /// </summary>
        internal bool IsCrLf => _isCrLf;

        /// <summary>
        /// Gets a flag indicating that a problem with the format of the file was found, after which the boundaries of the messages are unknown.
        /// </summary>
        internal bool IsFaulted { get; private set; }

        /// <summary>
        /// Gets a flag indicating that nothing more of the current message is left to read, or that there is no current message.
        /// </summary>
        internal bool IsMessageEnded => _phase == Phase.MessageEnded || _phase == Phase.FirstSeparator || _phase == Phase.NoMoreMessages;

        /// <summary>
        /// Gets the 0-based index of the current message, which is 0 before the first.
        /// </summary>
        internal long MessageIndex => Math.Max(_messagesFound - 1, 0);

        /// <summary>
        /// Gets the number of messages found so far.
        /// </summary>
        internal long MessagesFound => _messagesFound;

        /// <summary>
        /// Makes room in the block for at least one more byte.
        /// </summary>
        /// <param name="offset">The index in <see cref="Buffer"/> to read into.</param>
        /// <param name="count">The number of bytes that may be read.</param>
        internal void PrepareRefill(out int offset, out int count)
        {
            var retained = _bufferEnd - _position;
            if (retained > 0 && _position > 0)
            {
                Array.Copy(_buffer, _position, _buffer, 0, retained);
            }

            _position = 0;
            _bufferEnd = retained;
            offset = retained;
            count = _buffer.Length - retained;
        }

        /// <summary>
        /// Takes in what a driver read.
        /// </summary>
        /// <param name="bytesRead">The number of bytes read, or 0 at the end of the file.</param>
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
        /// Returns the block to the pool.  The scanner can't be used after this.
        /// </summary>
        internal void ReturnBuffers()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = new byte[0];
            _position = _bufferEnd = 0;
        }

        /// <summary>
        /// Moves to the next message, once the current one has been read to its end.
        /// </summary>
        /// <returns><see cref="Status.NeedMoreInput"/>, <see cref="Status.MessageFound"/>, or <see cref="Status.EndOfInput"/> if there are no more messages.</returns>
        /// <exception cref="ImfFileException">The file breaks a rule of its format that the options don't accept, or passes a limit.</exception>
        internal Status MoveToMessage()
        {
            if (!_hasMoveStarted)
            {
                Diagnostics.Clear();
                _hasMoveStarted = true;
            }

            if (_phase == Phase.NoMoreMessages) return Status.EndOfInput;

            if (_phase == Phase.FirstSeparator)
            {
                var status = FindFirstSeparator();
                if (status != Status.MessageFound) return status;
            }
            else if (_nextEnvelope is null)
            {
                _phase = Phase.NoMoreMessages;
                return Status.EndOfInput;
            }

            if (_messagesFound == _maxMessageCount)
                throw Fault(string.Format(ErrorMessage.TooManyMessagesTemplate, _maxMessageCount), null);

            StartMessage();
            return Status.MessageFound;
        }

        /// <summary>
        /// Gets the next bytes of the current message, with its quotes removed.
        /// </summary>
        /// <param name="destination">Where to put them.</param>
        /// <param name="written">The number of bytes put there.</param>
        /// <returns><see cref="Status.Content"/> if some were, <see cref="Status.NeedMoreInput"/>, or <see cref="Status.EndOfMessage"/> if none are left.</returns>
        /// <exception cref="ImfFileException">The file breaks a rule of its format that the options don't accept.</exception>
        internal Status Read(Span<byte> destination, out int written)
        {
            written = 0;
            while (true)
            {
                written += DrainQueue(destination.Slice(written));
                if (_queueLength > 0 || _quotesToWrite > 0) return Status.Content;

                if (_phase == Phase.MessageEnded) return written > 0 ? Status.Content : Status.EndOfMessage;

                if (written == destination.Length) return Status.Content;

                bool canContinue;
                switch (_phase)
                {
                    case Phase.LineStart:
                        canContinue = StepLineStart();
                        break;
                    case Phase.Quotes:
                        canContinue = StepQuotes();
                        break;
                    case Phase.InLine:
                        canContinue = StepInLine(destination.Slice(written), ref written);
                        break;
                    case Phase.CountedBody:
                        canContinue = StepCountedBody(destination.Slice(written), ref written);
                        break;
                    default:
                        canContinue = StepTerminator();
                        break;
                }

                if (!canContinue) return written > 0 ? Status.Content : Status.NeedMoreInput;
            }
        }

        private int DrainQueue(Span<byte> destination)
        {
            var written = 0;
            while (_quotesToWrite > 0 && written < destination.Length)
            {
                destination[written++] = Quote;
                _quotesToWrite--;
            }

            var count = Math.Min(_queueLength, destination.Length - written);
            if (count > 0)
            {
                new ReadOnlySpan<byte>(_queue, _queueStart, count).CopyTo(destination.Slice(written));
                _queueStart += count;
                _queueLength -= count;
                written += count;
            }

            return written;
        }

        private Status FindFirstSeparator()
        {
            while (true)
            {
                var available = _bufferEnd - _position;
                if (_skippingLine)
                {
                    var lineFeed = new ReadOnlySpan<byte>(_buffer, _position, available).IndexOf(LineFeed);
                    if (lineFeed >= 0)
                    {
                        Consume(lineFeed + 1);
                        _skippingLine = false;
                        continue;
                    }

                    Consume(available);
                    if (!_endOfInput) return Status.NeedMoreInput;
                }

                if (_position == _bufferEnd)
                {
                    if (!_endOfInput) return Status.NeedMoreInput;

                    _phase = Phase.NoMoreMessages;
                    return Status.EndOfInput;
                }

                var match = MatchSeparator(_position, out var envelope, out var length);
                switch (match)
                {
                    case LineMatch.NeedMoreInput:
                        return Status.NeedMoreInput;
                    case LineMatch.Yes:
                        // ref. #412 design decision 14: the first separator line says which line break the file uses.
                        _isCrLf = _buffer[_position + length - 2] == CarriageReturn;
                        _isLineBreakKnown = true;
                        if (_isCrLf) ReportCarriageReturnLineFeed();

                        RememberEnvelope(envelope, length);
                        return Status.MessageFound;
                    default:
                        if (!_allowMissingSeparator)
                            throw Fault(match == LineMatch.Invalid ? ErrorMessage.InvalidSeparator : ErrorMessage.MissingSeparator,
                                match == LineMatch.Invalid ? ImfDiagnosticKind.InvalidMboxSeparator : ImfDiagnosticKind.MissingMboxSeparator);

                        if (!_reportedMissingSeparator)
                        {
                            _reportedMissingSeparator = true;
                            AddDiagnostic(ImfDiagnosticKind.MissingMboxSeparator, ErrorMessage.MissingSeparatorDiagnostic);
                        }

                        _skippingLine = true;
                        break;
                }
            }
        }

        private void RememberEnvelope([AllowNull] MboxEnvelope envelope, int lineLength)
        {
            _nextEnvelope = envelope;
            _nextEnvelopeBytePosition = _bytePosition;
            _nextEnvelopeLineNumber = _lineNumber;
            _checksContent = false;
            Consume(lineLength);
        }

        private void StartMessage()
        {
            _messagesFound++;
            var envelope = _nextEnvelope;
            Envelope = envelope;
            _nextEnvelope = null;
            _hasMoveStarted = false;
            if (!(envelope is null) && !envelope.IsConforming)
            {
                Diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.InvalidMboxSeparator, ErrorMessage.InvalidSeparatorDiagnostic, _nextEnvelopeBytePosition, string.Empty, string.Empty, _nextEnvelopeLineNumber));
            }

            _phase = Phase.LineStart;
            _checksContent = true;
            _previousOctet = 0;
            _reportedCarriageReturnLineFeed = false;
            _reportedEightBit = false;
            _inHeader = _usesContentLength;
            _hasContentLength = false;
            _invalidContentLength = false;
            _contentLength = 0;
            _bodyBytes = 0;
            _heldLength = 0;
            _heldIsHeaderEnd = false;
            _previousLineWasEmpty = false;
            _reportedInvalidSeparator = false;
        }

        // Each of these steps returns false if it needs more input.
        private bool StepLineStart()
        {
            var available = _bufferEnd - _position;
            if (available == 0)
            {
                if (!_endOfInput) return false;

                EndAtEndOfInput();
                return true;
            }

            var first = _buffer[_position];

            if (first == LineFeed && !_isCrLf)
            {
                TakeEmptyLine(1);
                return true;
            }

            if (first == CarriageReturn && _isCrLf)
            {
                if (available < 2 && !_endOfInput) return false;

                if (available >= 2 && _buffer[_position + 1] == LineFeed)
                {
                    TakeEmptyLine(2);
                    return true;
                }
            }

            if (first == From[0])
            {
                var match = MatchSeparator(_position, out var envelope, out var length);
                if (match == LineMatch.NeedMoreInput) return false;

                if (match == LineMatch.Yes)
                {
                    if (TakeSeparator(envelope, length)) return true;
                }
                else if (match == LineMatch.Invalid)
                {
                    CheckInvalidSeparator();
                }
            }
            else if (_inHeader && (first == 'c' || first == 'C'))
            {
                if (!TryReadContentLength()) return false;
            }
            else if (first == Quote && _variant == MboxVariant.Mboxrd)
            {
                FlushHeld();
                _previousLineWasEmpty = false;
                _quoteCount = 0;
                _phase = Phase.Quotes;
                return true;
            }

            FlushHeld();
            _previousLineWasEmpty = false;
            _phase = Phase.InLine;
            return true;
        }

        private void TakeEmptyLine(int length)
        {
            FlushHeld();
            Consume(length);
            if (_inHeader && !_allowInvalidContentLength)
            {
                // ref. RFC 5322 §2.1: this line ends the header, so it is part of the message, and the length that the Content-Length field gives starts after it.
                Enqueue(length);
                _inHeader = false;
                if (!_hasContentLength || _invalidContentLength)
                    throw Fault(ErrorMessage.ContentLengthInvalid, ImfDiagnosticKind.InvalidMboxContentLength);

                _remainingBody = _contentLength;
                _lastEndsLine = true;
                _lastOctet = 0;
                _phase = Phase.CountedBody;
                return;
            }

            _heldIsHeaderEnd = _inHeader;
            _inHeader = false;
            _heldLength = length;
            _previousLineWasEmpty = true;
        }

        /// <summary>
        /// Deals with a separator line at the start of a line.
        /// </summary>
        /// <returns><c>false</c> if the line is part of the message after all.</returns>
        private bool TakeSeparator([AllowNull] MboxEnvelope envelope, int length)
        {
            if (_previousLineWasEmpty)
            {
                DropHeld();
            }
            else if (_allowMissingBlankLine)
            {
                AddDiagnostic(ImfDiagnosticKind.MissingMboxBlankLine, ErrorMessage.MissingBlankLineDiagnostic);
                FlushHeld();
            }
            else if (_usesContentLength && _allowInvalidContentLength)
            {
                // With no quoting, this can be a line of a body.
                return false;
            }
            else
            {
                throw Fault(ErrorMessage.MissingBlankLine, ImfDiagnosticKind.MissingMboxBlankLine);
            }

            CheckBodyLength();
            RememberEnvelope(envelope, length);
            _phase = Phase.MessageEnded;
            return true;
        }

        private void CheckInvalidSeparator()
        {
            // These variants don't rely on the lines of a message, and an obsolete "From : " field is one of them.
            if (_usesContentLength) return;

            if (!_allowInvalidSeparator)
                throw Fault(ErrorMessage.InvalidSeparator, ImfDiagnosticKind.InvalidMboxSeparator);

            if (!_reportedInvalidSeparator)
            {
                _reportedInvalidSeparator = true;
                AddDiagnostic(ImfDiagnosticKind.InvalidMboxSeparator, ErrorMessage.InvalidSeparatorDiagnostic);
            }
        }

        private void EndAtEndOfInput()
        {
            if (_inHeader && !_allowInvalidContentLength)
                throw Fault(ErrorMessage.ContentLengthInvalid, ImfDiagnosticKind.InvalidMboxContentLength);

            if (_previousLineWasEmpty)
            {
                DropHeld();
            }
            else
            {
                if (!_allowMissingBlankLine)
                    throw Fault(ErrorMessage.MissingFinalBlankLine, ImfDiagnosticKind.MissingMboxBlankLine);

                FlushHeld();
                AddDiagnostic(ImfDiagnosticKind.MissingMboxBlankLine, ErrorMessage.MissingBlankLineDiagnostic);
            }

            CheckBodyLength();
            _phase = Phase.MessageEnded;
        }

        private void CheckBodyLength()
        {
            if (_usesContentLength && _allowInvalidContentLength && (!_hasContentLength || _invalidContentLength || _inHeader || _contentLength != _bodyBytes))
            {
                AddDiagnostic(ImfDiagnosticKind.InvalidMboxContentLength, ErrorMessage.ContentLengthDiagnostic);
            }
        }

        private bool StepQuotes()
        {
            while (_position < _bufferEnd && _buffer[_position] == Quote)
            {
                _quoteCount++;
                Consume(1);
            }

            var available = _bufferEnd - _position;
            if (available == 0 && !_endOfInput) return false;

            var prefix = new ReadOnlySpan<byte>(_buffer, _position, Math.Min(available, FromLength));
            var isPrefix = prefix.SequenceEqual(new ReadOnlySpan<byte>(From, 0, prefix.Length));
            if (isPrefix && available < FromLength && !_endOfInput) return false;

            // ref. mboxrd: a line matching ">+From " has lost one quote when it is read.
            _quotesToWrite = isPrefix && available >= FromLength ? _quoteCount - 1 : _quoteCount;
            _phase = Phase.InLine;
            return true;
        }

        private bool StepInLine(Span<byte> destination, ref int written)
        {
            var available = _bufferEnd - _position;
            if (available == 0)
            {
                if (!_endOfInput) return false;

                EndAtEndOfInput();
                return true;
            }

            var unread = new ReadOnlySpan<byte>(_buffer, _position, available);
            var lineFeed = unread.IndexOf(LineFeed);
            int count;
            if (lineFeed < 0)
            {
                count = available;
                // A carriage return at the end of the block may be the start of a line break.
                if (_isCrLf && !_endOfInput && unread[available - 1] == CarriageReturn) count--;

                if (count == 0) return false;
            }
            else if (!_isCrLf)
            {
                if (lineFeed == 0)
                {
                    EndLine(1);
                    return true;
                }

                count = lineFeed;
            }
            else if (lineFeed > 0 && unread[lineFeed - 1] == CarriageReturn)
            {
                if (lineFeed == 1)
                {
                    EndLine(2);
                    return true;
                }

                count = lineFeed - 1;
            }
            else
            {
                // A line feed with no carriage return doesn't end a line of this file.
                count = lineFeed + 1;
            }

            count = Math.Min(count, destination.Length);
            unread.Slice(0, count).CopyTo(destination);
            written += count;
            if (!_inHeader) _bodyBytes += count;
            Consume(count);
            return true;
        }

        private void EndLine(int length)
        {
            Consume(length);
            _heldLength = length;
            _heldIsHeaderEnd = false;
            _previousLineWasEmpty = false;
            _phase = Phase.LineStart;
        }

        private bool StepCountedBody(Span<byte> destination, ref int written)
        {
            if (_remainingBody == 0)
            {
                _phase = Phase.Terminator;
                return true;
            }

            var available = _bufferEnd - _position;
            if (available == 0)
            {
                if (!_endOfInput) return false;

                throw Fault(ErrorMessage.ContentLengthTooLong, ImfDiagnosticKind.InvalidMboxContentLength);
            }

            var count = (int)Math.Min(Math.Min(available, destination.Length), _remainingBody);
            var bytes = new ReadOnlySpan<byte>(_buffer, _position, count);
            bytes.CopyTo(destination);

            var last = bytes[count - 1];
            _lastEndsLine = _isCrLf
                ? last == LineFeed && (count >= 2 ? bytes[count - 2] : _lastOctet) == CarriageReturn
                : last == LineFeed;
            _lastOctet = last;

            written += count;
            _remainingBody -= count;
            Consume(count);
            return true;
        }

        private bool StepTerminator()
        {
            // A body that ends a line is followed by the line break of the empty line.  One that doesn't has its own line break first, which isn't part of what the field counts.
            var required = _lastEndsLine ? 1 : 2;
            var breakLength = _isCrLf ? 2 : 1;
            var offset = _position;
            for (var index = 0; index < required; index++)
            {
                if (offset == _bufferEnd && _endOfInput)
                {
                    EndCountedAtEndOfInput(offset);
                    return true;
                }

                if (_bufferEnd - offset < breakLength)
                {
                    if (_endOfInput) throw Fault(ErrorMessage.ContentLengthMismatch, ImfDiagnosticKind.InvalidMboxContentLength);

                    return false;
                }

                if (_buffer[offset + breakLength - 1] != LineFeed || (_isCrLf && _buffer[offset] != CarriageReturn))
                    throw Fault(ErrorMessage.ContentLengthMismatch, ImfDiagnosticKind.InvalidMboxContentLength);

                offset += breakLength;
            }

            if (offset == _bufferEnd)
            {
                if (!_endOfInput) return false;

                Consume(offset - _position);
                _phase = Phase.MessageEnded;
                return true;
            }

            var match = MatchSeparator(offset, out var envelope, out var length);
            if (match == LineMatch.NeedMoreInput) return false;

            if (match != LineMatch.Yes)
                throw Fault(ErrorMessage.ContentLengthMismatch, ImfDiagnosticKind.InvalidMboxContentLength);

            Consume(offset - _position);
            RememberEnvelope(envelope, length);
            _phase = Phase.MessageEnded;
            return true;
        }

        private void EndCountedAtEndOfInput(int offset)
        {
            if (!_allowMissingBlankLine)
                throw Fault(ErrorMessage.MissingFinalBlankLine, ImfDiagnosticKind.MissingMboxBlankLine);

            Consume(offset - _position);
            AddDiagnostic(ImfDiagnosticKind.MissingMboxBlankLine, ErrorMessage.MissingBlankLineDiagnostic);
            _phase = Phase.MessageEnded;
        }

        /// <summary>
        /// Reads the Content-Length field at the start of a line, if that is what is there.  The line is left to be passed on.
        /// </summary>
        /// <returns><c>false</c> if it needs more input.</returns>
        private bool TryReadContentLength()
        {
            var available = _bufferEnd - _position;
            var window = new ReadOnlySpan<byte>(_buffer, _position, Math.Min(available, MaxLineWindow));
            var lineFeed = window.IndexOf(LineFeed);
            if (lineFeed < 0 && window.Length < MaxLineWindow && !_endOfInput) return false;

            var line = lineFeed < 0 ? window : window.Slice(0, lineFeed);
            if (!StartsWithContentLengthName(line, out var valueStart)) return true;

            if (_hasContentLength || lineFeed < 0 || !TryParseLength(line.Slice(valueStart), out _contentLength))
            {
                _invalidContentLength = true;
            }

            _hasContentLength = true;
            return true;
        }

        internal static bool StartsWithContentLengthName(ReadOnlySpan<byte> line, out int valueStart)
        {
            valueStart = 0;
            if (line.Length <= ContentLengthName.Length) return false;

            for (var index = 0; index < ContentLengthName.Length; index++)
            {
                var octet = line[index];
                if (octet >= 'A' && octet <= 'Z') octet = (byte)(octet + ('a' - 'A'));
                if (octet != ContentLengthName[index]) return false;
            }

            var position = ContentLengthName.Length;
            // ref. RFC 5322 §4.5: the obsolete syntax allows white space before the colon.
            while (position < line.Length && (line[position] == Space || line[position] == Tab)) position++;

            if (position == line.Length || line[position] != Colon) return false;

            valueStart = position + 1;
            return true;
        }

        internal static bool TryParseLength(ReadOnlySpan<byte> value, out long length)
        {
            length = 0;
            var start = 0;
            var end = value.Length;
            while (start < end && (value[start] == Space || value[start] == Tab)) start++;
            while (end > start && (value[end - 1] == Space || value[end - 1] == Tab || value[end - 1] == CarriageReturn)) end--;

            if (start == end || end - start > MaxContentLengthDigits) return false;

            for (var index = start; index < end; index++)
            {
                if (value[index] < '0' || value[index] > '9') return false;

                length = length * 10 + (value[index] - '0');
            }

            return true;
        }

        /// <summary>
        /// Looks at a line for a separator line.
        /// </summary>
        /// <param name="start">The index in the block of the start of the line.</param>
        /// <param name="envelope">The separator line, if it is one.</param>
        /// <param name="length">The length of the line, with its line break, if it is one.</param>
        private LineMatch MatchSeparator(int start, [MaybeNull] out MboxEnvelope envelope, out int length)
        {
            envelope = null;
            length = 0;
            var available = _bufferEnd - start;
            var prefix = new ReadOnlySpan<byte>(_buffer, start, Math.Min(available, FromLength));
            if (!prefix.SequenceEqual(new ReadOnlySpan<byte>(From, 0, prefix.Length))) return LineMatch.No;

            if (available < FromLength) return _endOfInput ? LineMatch.No : LineMatch.NeedMoreInput;

            var window = new ReadOnlySpan<byte>(_buffer, start, Math.Min(available, MaxLineWindow));
            var lineFeed = window.IndexOf(LineFeed);
            if (lineFeed < 0)
            {
                return window.Length == MaxLineWindow || _endOfInput ? LineMatch.Invalid : LineMatch.NeedMoreInput;
            }

            var hasCarriageReturn = lineFeed > 0 && window[lineFeed - 1] == CarriageReturn;
            bool usesCrLf;
            if (_isLineBreakKnown)
            {
                usesCrLf = _isCrLf;
                // A line feed with no carriage return doesn't end a line of this file, and one with a carriage return in a file of line feeds leaves it in the line, which no separator line has.
                if (usesCrLf && !hasCarriageReturn) return LineMatch.Invalid;
            }
            else
            {
                usesCrLf = hasCarriageReturn;
            }

            var contentLength = usesCrLf ? lineFeed - 1 : lineFeed;
            if (!MboxEnvelope.TryParse(window.Slice(0, contentLength), _allowInvalidSeparator, out envelope)) return LineMatch.Invalid;

            length = lineFeed + 1;
            return LineMatch.Yes;
        }

        private void FlushHeld()
        {
            if (_heldLength == 0) return;

            if (!_heldIsHeaderEnd && !_inHeader) _bodyBytes += _heldLength;

            Enqueue(_heldLength);
            _heldLength = 0;
            _heldIsHeaderEnd = false;
        }

        private void DropHeld()
        {
            _heldLength = 0;
            _heldIsHeaderEnd = false;
        }

        private void Enqueue(int lineBreakLength)
        {
            if (_queueLength == 0) _queueStart = 0;

            var at = _queueStart + _queueLength;
            if (lineBreakLength == 2) _queue[at++] = CarriageReturn;
            _queue[at] = LineFeed;
            _queueLength += lineBreakLength;
        }

        private void Consume(int count)
        {
            var bytes = new ReadOnlySpan<byte>(_buffer, _position, count);
            if (_checksContent && (!_reportedEightBit || (!_isCrLf && !_reportedCarriageReturnLineFeed)))
            {
                ConsumeChecked(bytes);
            }
            else
            {
                while (true)
                {
                    var lineFeed = bytes.IndexOf(LineFeed);
                    if (lineFeed < 0) break;

                    _lineNumber++;
                    bytes = bytes.Slice(lineFeed + 1);
                }

                _bytePosition += count;
            }

            _position += count;
        }

        // ref. RFC 4155 Appendix A: seven-bit characters, and a line feed with no carriage return.  A problem found is reported once for each message, so that it takes no more memory for a larger one.
        private void ConsumeChecked(ReadOnlySpan<byte> bytes)
        {
            foreach (var octet in bytes)
            {
                if (octet > HighestSevenBitOctet && !_reportedEightBit)
                {
                    _reportedEightBit = true;
                    if (!_allowEightBit) throw Fault(ErrorMessage.EightBit, ImfDiagnosticKind.EightBitMboxContent);

                    AddDiagnostic(ImfDiagnosticKind.EightBitMboxContent, ErrorMessage.EightBitDiagnostic);
                }

                if (octet == LineFeed)
                {
                    if (!_isCrLf && _previousOctet == CarriageReturn && !_reportedCarriageReturnLineFeed)
                    {
                        _reportedCarriageReturnLineFeed = true;
                        ReportCarriageReturnLineFeed();
                    }

                    _lineNumber++;
                }

                _previousOctet = octet;
                _bytePosition++;
            }
        }

        private void ReportCarriageReturnLineFeed()
        {
            if (!_allowCarriageReturnLineFeed)
                throw Fault(ErrorMessage.CarriageReturnLineFeed, ImfDiagnosticKind.MboxCarriageReturnLineFeed);

            AddDiagnostic(ImfDiagnosticKind.MboxCarriageReturnLineFeed, ErrorMessage.CarriageReturnLineFeedDiagnostic);
        }

        private void AddDiagnostic(ImfDiagnosticKind kind, string message)
        {
            Diagnostics.Add(new ImfDiagnostic(kind, message, _bytePosition, string.Empty, string.Empty, _lineNumber));
        }

        private ImfFileException Fault(string message, ImfDiagnosticKind? kind)
        {
            IsFaulted = true;
            var exception = new ImfFileException(message);
            exception.Data[ImfFileException.DataItemName.BytePosition] = _bytePosition;
            exception.Data[ImfFileException.DataItemName.LineNumber] = _lineNumber;
            exception.Data[ImfFileException.DataItemName.MessageIndex] = MessageIndex;
            if (kind.HasValue) exception.Data[ImfFileException.DataItemName.DiagnosticKind] = kind.Value;

            return exception;
        }
    }
}
