using System;
using System.Buffers;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The state of a quoted-printable decoder, which <see cref="MimeQuotedPrintableDecoder"/> and the one-shot methods of
    /// <see cref="MimeQuotedPrintable"/> share.
    /// </summary>
    internal struct MimeQuotedPrintableDecoderState
    {
        // The "Q" modes have no whitespace.
        private static readonly byte[] NoWhitespace = new byte[0];

        private readonly MimeQuotedPrintableMode _mode;
        private readonly bool _allowLowercase;
        private readonly bool _allowBareLineFeeds;
        private readonly bool _allowLiteralEquals;
        private readonly bool _allowLongLines;

        // A run of spaces and tabs in a body, which is removed if a line break follows it (RFC 2045 §6.7 rule 3). The buffer holds as
        // much of the run as could still be part of the line; a longer run is only counted, as it can only be transport padding. When
        // long lines are allowed, the buffer grows to hold the whole run.
        private byte[] _whitespace;
        private int _whitespaceStart;
        private int _whitespaceCount;

        // The number of characters of the current line of a body read so far, not counting a run of whitespace that may turn out to be
        // transport padding.
        private int _lineLength;

        private Pending _pending;
        private int _highDigit;

        internal MimeQuotedPrintableDecoderState(MimeQuotedPrintableMode mode, MimeQuotedPrintableDecoderOptions options)
        {
            _mode = mode;
            _allowLowercase = (options & MimeQuotedPrintableDecoderOptions.AllowLowercaseHexDigits) != 0;
            _allowBareLineFeeds = (options & MimeQuotedPrintableDecoderOptions.AllowBareLineFeeds) != 0;
            _allowLiteralEquals = (options & MimeQuotedPrintableDecoderOptions.AllowLiteralEqualsSigns) != 0;
            _allowLongLines = (options & MimeQuotedPrintableDecoderOptions.AllowLongLines) != 0;
            _whitespace = mode == MimeQuotedPrintableMode.Body ? new byte[MimeQuotedPrintable.MaxLineLength] : NoWhitespace;
            _whitespaceStart = 0;
            _whitespaceCount = 0;
            _lineLength = 0;
            _pending = Pending.None;
            _highDigit = 0;
        }

        // What the decoder has read that it can't decode until more data arrives.
        private enum Pending
        {
            None,

            // "=", which starts an escape or a soft line break.
            EqualsSign,

            // "=" and one hexadecimal digit.
            EqualsSignAndDigit,

            // "=" and whitespace, which is a soft line break if a line break follows; the whitespace is in the run.
            EqualsSignAndWhitespace,

            // The CR of a line break.
            CarriageReturn,

            // The CR of a soft line break.
            SoftCarriageReturn,
        }

        private bool IsBody => _mode == MimeQuotedPrintableMode.Body;

        internal void Reset()
        {
            _whitespaceStart = 0;
            _whitespaceCount = 0;
            _lineLength = 0;
            _pending = Pending.None;
            _highDigit = 0;
        }

        internal OperationStatus Decode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            var consumed = 0;
            var written = 0;
            while (consumed < source.Length)
            {
                var status = Process(source[consumed], destination, ref written);
                if (status != OperationStatus.Done)
                {
                    bytesConsumed = consumed;
                    bytesWritten = written;
                    return status;
                }

                consumed++;
            }

            bytesConsumed = consumed;
            var finalStatus = Finish(isFinalBlock, destination, ref written);
            bytesWritten = written;
            return finalStatus;
        }

        internal OperationStatus Decode(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, bool isFinalBlock)
        {
            var consumed = 0;
            var written = 0;
            while (consumed < source.Length)
            {
                var status = Process(source[consumed], destination, ref written);
                if (status != OperationStatus.Done)
                {
                    charsConsumed = consumed;
                    bytesWritten = written;
                    return status;
                }

                consumed++;
            }

            charsConsumed = consumed;
            var finalStatus = Finish(isFinalBlock, destination, ref written);
            bytesWritten = written;
            return finalStatus;
        }

        private OperationStatus Finish(bool isFinalBlock, Span<byte> destination, ref int written)
        {
            if (!isFinalBlock) return OperationStatus.Done;

            switch (_pending)
            {
                case Pending.CarriageReturn:
                case Pending.SoftCarriageReturn:
                    // The data is truncated inside a line break.
                    return OperationStatus.InvalidData;
                case Pending.EqualsSign:
                case Pending.EqualsSignAndWhitespace:
                    // ref. RFC 2045 §6.7 note (2): an "=" at the end of the data is illegal.
                    if (!_allowLiteralEquals) return OperationStatus.InvalidData;
                    if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)'=';
                    break;
                case Pending.EqualsSignAndDigit:
                    if (!_allowLiteralEquals) return OperationStatus.InvalidData;
                    if (destination.Length - written < 2) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)'=';
                    destination[written++] = (byte)_highDigit;
                    break;
            }

            // ref. RFC 2045 §6.7 rule 3: whitespace at the end of the last line is trailing whitespace too.
            Reset();
            return OperationStatus.Done;
        }

        // Processes one unit of encoded data, returning Done to go on to the next.
        private OperationStatus Process(int unit, Span<byte> destination, ref int written)
        {
            switch (_pending)
            {
                case Pending.CarriageReturn:
                    if (unit != '\n') return OperationStatus.InvalidData;
                    if (destination.Length - written < 2) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)'\r';
                    destination[written++] = (byte)'\n';
                    _pending = Pending.None;
                    return OperationStatus.Done;

                case Pending.SoftCarriageReturn:
                    if (unit != '\n') return OperationStatus.InvalidData;

                    _pending = Pending.None;
                    return OperationStatus.Done;

                case Pending.EqualsSign:
                    if (GetDigitValue(unit) >= 0)
                    {
                        if (!TryAddToLine()) return OperationStatus.InvalidData;

                        _highDigit = unit;
                        _pending = Pending.EqualsSignAndDigit;
                        return OperationStatus.Done;
                    }

                    if (IsBody)
                    {
                        // ref. RFC 2045 §6.7 rule 5: "=" at the end of a line is a soft line break, and rule 3: whitespace may have been
                        // added after it.
                        if (MimeQuotedPrintable.IsWhitespace(unit))
                        {
                            _pending = Pending.EqualsSignAndWhitespace;
                            return AddWhitespace(unit);
                        }

                        if (unit == '\r')
                        {
                            _lineLength = 0;
                            _pending = Pending.SoftCarriageReturn;
                            return OperationStatus.Done;
                        }

                        if (unit == '\n' && _allowBareLineFeeds)
                        {
                            _lineLength = 0;
                            _pending = Pending.None;
                            return OperationStatus.Done;
                        }
                    }

                    if (!_allowLiteralEquals) return OperationStatus.InvalidData;
                    if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)'=';
                    _pending = Pending.None;
                    break;

                case Pending.EqualsSignAndDigit:
                    var low = GetDigitValue(unit);
                    if (low >= 0)
                    {
                        if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;
                        if (!TryAddToLine()) return OperationStatus.InvalidData;

                        destination[written++] = (byte)((GetDigitValue(_highDigit) << 4) | low);
                        _pending = Pending.None;
                        return OperationStatus.Done;
                    }

                    if (!_allowLiteralEquals) return OperationStatus.InvalidData;
                    if (destination.Length - written < 2) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)'=';
                    destination[written++] = (byte)_highDigit;
                    _pending = Pending.None;
                    break;

                case Pending.EqualsSignAndWhitespace:
                    if (MimeQuotedPrintable.IsWhitespace(unit)) return AddWhitespace(unit);

                    if (unit == '\r')
                    {
                        ClearWhitespace();
                        _lineLength = 0;
                        _pending = Pending.SoftCarriageReturn;
                        return OperationStatus.Done;
                    }

                    if (unit == '\n' && _allowBareLineFeeds)
                    {
                        ClearWhitespace();
                        _lineLength = 0;
                        _pending = Pending.None;
                        return OperationStatus.Done;
                    }

                    // The "=" is literal, and the whitespace after it is an ordinary run.
                    if (!_allowLiteralEquals) return OperationStatus.InvalidData;
                    if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)'=';
                    _pending = Pending.None;
                    break;
            }

            return ProcessUnit(unit, destination, ref written);
        }

        // Processes a unit that doesn't continue anything pending.
        private OperationStatus ProcessUnit(int unit, Span<byte> destination, ref int written)
        {
            if (IsBody)
            {
                if (MimeQuotedPrintable.IsWhitespace(unit)) return AddWhitespace(unit);

                // ref. RFC 2045 §6.7 rule 3: trailing whitespace on a line is removed.
                if (unit == '\r')
                {
                    ClearWhitespace();
                    _lineLength = 0;
                    _pending = Pending.CarriageReturn;
                    return OperationStatus.Done;
                }

                if (unit == '\n' && _allowBareLineFeeds)
                {
                    if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;

                    ClearWhitespace();
                    _lineLength = 0;
                    destination[written++] = (byte)'\n';
                    return OperationStatus.Done;
                }

                // The whitespace is followed by something other than a line break, so it is part of the data and of the line. ref.
                // RFC 2045 §6.7 rule 5: the line, with this unit, is at most 76 characters.
                // With long lines allowed, a run longer than the decoder holds is not valid either.
                if (_allowLongLines
                    ? _whitespaceCount > MimeQuotedPrintable.MaxHeldWhitespace
                    : _lineLength + _whitespaceCount >= MimeQuotedPrintable.MaxLineLength)
                {
                    return OperationStatus.InvalidData;
                }

                while (_whitespaceCount > 0)
                {
                    if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;

                    destination[written++] = _whitespace[_whitespaceStart++];
                    _whitespaceCount--;
                    _lineLength++;
                }

                _whitespaceStart = 0;
            }

            if (unit == '=')
            {
                if (!TryAddToLine()) return OperationStatus.InvalidData;

                _pending = Pending.EqualsSign;
                return OperationStatus.Done;
            }

            int value;
            if (MimeQuotedPrintable.IsLiteral(unit, _mode))
            {
                value = unit;
            }
            else if (!IsBody && unit == '_')
            {
                // ref. RFC 2047 §4.2 rule 2: "_" always represents hexadecimal 20.
                value = 0x20;
            }
            else
            {
                return OperationStatus.InvalidData;
            }

            if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;
            if (!TryAddToLine()) return OperationStatus.InvalidData;

            destination[written++] = (byte)value;
            return OperationStatus.Done;
        }

        // Counts a unit as part of the current line of a body, or returns false if the line is already 76 characters long (RFC 2045 §6.7
        // rule 5). Call it only once nothing else can stop the unit being consumed.
        private bool TryAddToLine()
        {
            if (!IsBody || _allowLongLines) return true;
            if (_lineLength >= MimeQuotedPrintable.MaxLineLength) return false;

            _lineLength++;
            return true;
        }

        private OperationStatus AddWhitespace(int unit)
        {
            // ref. RFC 2045 §6.7 rule 3: transport padding may be of any length. A run that no longer fits on the line can only be
            // padding, so the rest of it is counted but not kept, and it is not valid if anything but a line break follows it. With long
            // lines allowed, the run is kept up to MaxHeldWhitespace, so the decoder's memory stays bounded.
            if (IsWhitespaceRunFull())
            {
                if (_whitespaceCount <= MimeQuotedPrintable.MaxHeldWhitespace) _whitespaceCount++;

                return OperationStatus.Done;
            }

            if (_whitespaceStart + _whitespaceCount == _whitespace.Length)
            {
                if (_whitespaceStart == 0)
                {
                    Array.Resize(ref _whitespace, Math.Min(_whitespace.Length * 2, MimeQuotedPrintable.MaxHeldWhitespace));
                }
                else
                {
                    Array.Copy(_whitespace, _whitespaceStart, _whitespace, 0, _whitespaceCount);
                    _whitespaceStart = 0;
                }
            }

            _whitespace[_whitespaceStart + _whitespaceCount++] = (byte)unit;
            return OperationStatus.Done;
        }

        // Whether the run of whitespace can't be part of the data: with a unit after it, the line would be longer than 76 characters, or
        // with long lines allowed, the run is longer than the decoder holds.
        private bool IsWhitespaceRunFull()
        {
            return _allowLongLines
                ? _whitespaceCount >= MimeQuotedPrintable.MaxHeldWhitespace
                : _lineLength + _whitespaceCount >= MimeQuotedPrintable.MaxLineLength;
        }

        private void ClearWhitespace()
        {
            _whitespaceStart = 0;
            _whitespaceCount = 0;
        }

        // Gets the value of a hexadecimal digit, or -1. ref. RFC 2045 §6.7 rule 1: the digits are upper case.
        private int GetDigitValue(int unit)
        {
            if (unit >= '0' && unit <= '9') return unit - '0';
            if (unit >= 'A' && unit <= 'F') return unit - 'A' + 10;
            if (_allowLowercase && unit >= 'a' && unit <= 'f') return unit - 'a' + 10;

            return -1;
        }
    }
}
