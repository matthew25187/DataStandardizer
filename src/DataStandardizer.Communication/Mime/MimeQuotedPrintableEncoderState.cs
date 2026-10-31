using System;
using System.Buffers;

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The state of a quoted-printable encoder, which <see cref="MimeQuotedPrintableEncoder"/> and the one-shot methods of
    /// <see cref="MimeQuotedPrintable"/> share.
    /// </summary>
    internal struct MimeQuotedPrintableEncoderState
    {
        private readonly MimeQuotedPrintableMode _mode;
        private readonly bool _binary;
        private byte _held0;
        private byte _held1;
        private int _heldCount;
        private int _lineLength;

        internal MimeQuotedPrintableEncoderState(MimeQuotedPrintableMode mode, MimeQuotedPrintableEncoderOptions options)
        {
            _mode = mode;
            _binary = (options & MimeQuotedPrintableEncoderOptions.Binary) != 0;
            _held0 = 0;
            _held1 = 0;
            _heldCount = 0;
            _lineLength = 0;
        }

        internal void Reset()
        {
            _held0 = 0;
            _held1 = 0;
            _heldCount = 0;
            _lineLength = 0;
        }

        internal OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            var consumed = 0;
            var written = 0;
            while (_heldCount + source.Length - consumed > 0)
            {
                Step step;
                if (!TryGetStep(source, consumed, isFinalBlock, out step))
                {
                    Hold(source, ref consumed);
                    break;
                }

                if (destination.Length - written < step.Length)
                {
                    bytesConsumed = consumed;
                    bytesWritten = written;
                    return OperationStatus.DestinationTooSmall;
                }

                destination[written++] = (byte)step.First;
                if (step.Length > 1) destination[written++] = (byte)step.Second;
                if (step.Length > 2) destination[written++] = (byte)step.Third;
                Apply(step, ref consumed);
            }

            if (isFinalBlock)
            {
                Reset();
            }

            bytesConsumed = consumed;
            bytesWritten = written;
            return OperationStatus.Done;
        }

        internal OperationStatus Encode(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten, bool isFinalBlock)
        {
            var consumed = 0;
            var written = 0;
            while (_heldCount + source.Length - consumed > 0)
            {
                Step step;
                if (!TryGetStep(source, consumed, isFinalBlock, out step))
                {
                    Hold(source, ref consumed);
                    break;
                }

                if (destination.Length - written < step.Length)
                {
                    bytesConsumed = consumed;
                    charsWritten = written;
                    return OperationStatus.DestinationTooSmall;
                }

                destination[written++] = step.First;
                if (step.Length > 1) destination[written++] = step.Second;
                if (step.Length > 2) destination[written++] = step.Third;
                Apply(step, ref consumed);
            }

            if (isFinalBlock)
            {
                Reset();
            }

            bytesConsumed = consumed;
            charsWritten = written;
            return OperationStatus.Done;
        }

        // Gets the next thing to write, or returns false if that depends on data that hasn't arrived yet.
        private bool TryGetStep(ReadOnlySpan<byte> source, int consumed, bool isFinalBlock, out Step step)
        {
            var available = _heldCount + source.Length - consumed;
            var unit = Peek(source, consumed, 0);
            if (_mode != MimeQuotedPrintableMode.Body)
            {
                // ref. RFC 2047 §4.2 rule 2: "_" always represents hexadecimal 20.
                if (unit == 0x20) step = new Step('_', 1);
                else if (MimeQuotedPrintable.IsLiteral(unit, _mode)) step = new Step((char)unit, 1);
                else step = Step.Escape(unit, 1);

                return true;
            }

            // ref. RFC 2045 §6.7 rule 4: a line break in text is written as a CRLF line break.
            if (!_binary && unit == '\n')
            {
                step = Step.LineBreak(1);
                return true;
            }

            if (!_binary && unit == '\r')
            {
                if (available < 2 && !isFinalBlock)
                {
                    step = default(Step);
                    return false;
                }

                if (available >= 2 && Peek(source, consumed, 1) == '\n')
                {
                    step = Step.LineBreak(2);
                    return true;
                }
            }

            bool endsLine;
            if (available < 2)
            {
                if (!isFinalBlock)
                {
                    step = default(Step);
                    return false;
                }

                endsLine = true;
            }
            else if (_binary)
            {
                endsLine = false;
            }
            else
            {
                var next = Peek(source, consumed, 1);
                if (next == '\n')
                {
                    endsLine = true;
                }
                else if (next == '\r')
                {
                    if (available < 3 && !isFinalBlock)
                    {
                        step = default(Step);
                        return false;
                    }

                    endsLine = available >= 3 && Peek(source, consumed, 2) == '\n';
                }
                else
                {
                    endsLine = false;
                }
            }

            // ref. RFC 2045 §6.7 rule 3: a space or tab must not be written as itself at the end of an encoded line.
            var literal = MimeQuotedPrintable.IsLiteral(unit, _mode) || (MimeQuotedPrintable.IsWhitespace(unit) && !endsLine);
            var length = literal ? 1 : 3;

            // ref. RFC 2045 §6.7 rule 5: a line is at most 76 characters, including the "=" of a soft line break.
            if (_lineLength + length > (endsLine ? MimeQuotedPrintable.MaxLineLength : MimeQuotedPrintable.MaxLineLength - 1))
            {
                step = Step.SoftLineBreak();
                return true;
            }

            step = literal ? new Step((char)unit, 1) : Step.Escape(unit, 1);
            return true;
        }

        private void Apply(Step step, ref int consumed)
        {
            for (var count = 0; count < step.Units; count++)
            {
                if (_heldCount > 0)
                {
                    _held0 = _held1;
                    _heldCount--;
                }
                else
                {
                    consumed++;
                }
            }

            if (_mode == MimeQuotedPrintableMode.Body)
            {
                _lineLength = step.EndsLine ? 0 : _lineLength + step.Length;
            }
        }

        private int Peek(ReadOnlySpan<byte> source, int consumed, int offset)
        {
            if (offset < _heldCount) return offset == 0 ? _held0 : _held1;

            return source[consumed + offset - _heldCount];
        }

        // Keeps the last 1 or 2 bytes of a block for the next call, as how they are written depends on what follows them.
        private void Hold(ReadOnlySpan<byte> source, ref int consumed)
        {
            for (; consumed < source.Length; consumed++)
            {
                if (_heldCount == 0) _held0 = source[consumed];
                else _held1 = source[consumed];

                _heldCount++;
            }
        }

        // Up to 3 characters to write, and the number of bytes of data they encode.
        private struct Step
        {
            internal Step(char first, int units)
            {
                First = first;
                Second = '\0';
                Third = '\0';
                Length = 1;
                Units = units;
                EndsLine = false;
            }

            private Step(char first, char second, char third, int length, int units, bool endsLine)
            {
                First = first;
                Second = second;
                Third = third;
                Length = length;
                Units = units;
                EndsLine = endsLine;
            }

            internal char First { get; }

            internal char Second { get; }

            internal char Third { get; }

            internal int Length { get; }

            internal int Units { get; }

            internal bool EndsLine { get; }

            internal static Step Escape(int unit, int units)
            {
                return new Step('=', MimeQuotedPrintable.HexDigits[unit >> 4], MimeQuotedPrintable.HexDigits[unit & 0xF], 3, units, false);
            }

            internal static Step LineBreak(int units)
            {
                return new Step('\r', '\n', '\0', 2, units, true);
            }

            internal static Step SoftLineBreak()
            {
                return new Step('=', '\r', '\n', 3, 0, true);
            }
        }
    }
}
