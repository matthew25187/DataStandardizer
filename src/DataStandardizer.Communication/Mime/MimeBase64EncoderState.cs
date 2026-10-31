using System;
using System.Buffers;
#if NET10_0_OR_GREATER
using System.Buffers.Text;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The state of a base64 encoder, which <see cref="MimeBase64Encoder"/> and the one-shot methods of <see cref="MimeBase64"/> share.
    /// </summary>
    internal struct MimeBase64EncoderState
    {
        private readonly bool _wrapLines;
        private int _held;
        private int _heldCount;
        private int _lineLength;

        internal MimeBase64EncoderState(MimeBase64Mode mode)
        {
            _wrapLines = mode == MimeBase64Mode.Mime;
            _held = 0;
            _heldCount = 0;
            _lineLength = 0;
        }

        // A quantum is 4 characters, and in MIME mode the line break before it if the line is full.
        private int QuantumLength => _wrapLines && _lineLength == MimeBase64.MaxLineLength ? 6 : 4;

        internal void Reset()
        {
            _held = 0;
            _heldCount = 0;
            _lineLength = 0;
        }

        internal OperationStatus Encode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            var consumed = 0;
            var written = 0;
            while (_heldCount + source.Length - consumed >= 3)
            {
#if NET10_0_OR_GREATER
                if (_heldCount == 0 && !_wrapLines)
                {
                    var count = Math.Min((source.Length - consumed) / 3, (destination.Length - written) / 4);
                    if (count > 0)
                    {
                        Base64.EncodeToUtf8(source.Slice(consumed, count * 3), destination.Slice(written), out _, out var encoded, true);
                        consumed += count * 3;
                        written += encoded;
                        continue;
                    }
                }
#endif
                if (destination.Length - written < QuantumLength)
                {
                    bytesConsumed = consumed;
                    bytesWritten = written;
                    return OperationStatus.DestinationTooSmall;
                }

                var group = _held;
                for (; _heldCount < 3; _heldCount++)
                {
                    group = (group << 8) | source[consumed++];
                }

                WriteQuantum(group, 3, destination, ref written);
            }

            if (!isFinalBlock)
            {
                Hold(source, ref consumed);
            }
            else if (_heldCount + source.Length - consumed > 0)
            {
                if (destination.Length - written < QuantumLength)
                {
                    bytesConsumed = consumed;
                    bytesWritten = written;
                    return OperationStatus.DestinationTooSmall;
                }

                var count = _heldCount + source.Length - consumed;
                WriteQuantum(GetFinalGroup(source, ref consumed), count, destination, ref written);
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
            while (_heldCount + source.Length - consumed >= 3)
            {
#if NET10_0_OR_GREATER
                if (_heldCount == 0 && !_wrapLines)
                {
                    var count = Math.Min((source.Length - consumed) / 3, (destination.Length - written) / 4);
                    if (count > 0)
                    {
                        Convert.TryToBase64Chars(source.Slice(consumed, count * 3), destination.Slice(written), out var encoded);
                        consumed += count * 3;
                        written += encoded;
                        continue;
                    }
                }
#endif
                if (destination.Length - written < QuantumLength)
                {
                    bytesConsumed = consumed;
                    charsWritten = written;
                    return OperationStatus.DestinationTooSmall;
                }

                var group = _held;
                for (; _heldCount < 3; _heldCount++)
                {
                    group = (group << 8) | source[consumed++];
                }

                WriteQuantum(group, 3, destination, ref written);
            }

            if (!isFinalBlock)
            {
                Hold(source, ref consumed);
            }
            else if (_heldCount + source.Length - consumed > 0)
            {
                if (destination.Length - written < QuantumLength)
                {
                    bytesConsumed = consumed;
                    charsWritten = written;
                    return OperationStatus.DestinationTooSmall;
                }

                var count = _heldCount + source.Length - consumed;
                WriteQuantum(GetFinalGroup(source, ref consumed), count, destination, ref written);
            }

            if (isFinalBlock)
            {
                Reset();
            }

            bytesConsumed = consumed;
            charsWritten = written;
            return OperationStatus.Done;
        }

        // Keeps the last 1 or 2 bytes of a block for the next call, as they don't fill a quantum.
        private void Hold(ReadOnlySpan<byte> source, ref int consumed)
        {
            for (; consumed < source.Length; consumed++)
            {
                _held = (_held << 8) | source[consumed];
                _heldCount++;
            }
        }

        // Gets the group of the last 1 or 2 bytes, aligned as if it were a full group of 3.
        private int GetFinalGroup(ReadOnlySpan<byte> source, ref int consumed)
        {
            var group = _held;
            var count = _heldCount;
            for (; consumed < source.Length; consumed++, count++)
            {
                group = (group << 8) | source[consumed];
            }

            return group << (8 * (3 - count));
        }

        private void WriteQuantum(int group, int byteCount, Span<byte> destination, ref int written)
        {
            if (_wrapLines && _lineLength == MimeBase64.MaxLineLength)
            {
                destination[written++] = (byte)'\r';
                destination[written++] = (byte)'\n';
                _lineLength = 0;
            }

            // ref. RFC 4648 §4: each byte short of a full group of 3 leaves one "=" at the end of the quantum.
            for (var offset = 0; offset < 4; offset++)
            {
                destination[written++] = (byte)(offset <= byteCount ? MimeBase64.Alphabet[(group >> (18 - 6 * offset)) & 0x3F] : '=');
            }

            if (_wrapLines)
            {
                _lineLength += 4;
            }

            _held = 0;
            _heldCount = 0;
        }

        private void WriteQuantum(int group, int byteCount, Span<char> destination, ref int written)
        {
            if (_wrapLines && _lineLength == MimeBase64.MaxLineLength)
            {
                destination[written++] = '\r';
                destination[written++] = '\n';
                _lineLength = 0;
            }

            // ref. RFC 4648 §4: each byte short of a full group of 3 leaves one "=" at the end of the quantum.
            for (var offset = 0; offset < 4; offset++)
            {
                destination[written++] = offset <= byteCount ? MimeBase64.Alphabet[(group >> (18 - 6 * offset)) & 0x3F] : '=';
            }

            if (_wrapLines)
            {
                _lineLength += 4;
            }

            _held = 0;
            _heldCount = 0;
        }
    }
}
