using System;
using System.Buffers;
#if NET10_0_OR_GREATER
using System.Buffers.Text;
#endif

namespace DataStandardizer.Communication.Mime
{
    /// <summary>
    /// The state of a base64 decoder, which <see cref="MimeBase64Decoder"/> and the one-shot methods of <see cref="MimeBase64"/> share.
    /// </summary>
    internal struct MimeBase64DecoderState
    {
        private readonly MimeBase64Mode _mode;
        private readonly bool _rejectNonAlphabet;
        private int _group;
        private int _count;
        private bool _padded;
        private bool _ended;
        private bool _carriageReturn;

        internal MimeBase64DecoderState(MimeBase64Mode mode, MimeBase64DecoderOptions options)
        {
            _mode = mode;
            _rejectNonAlphabet = (options & MimeBase64DecoderOptions.RejectNonAlphabetCharacters) != 0;
            _group = 0;
            _count = 0;
            _padded = false;
            _ended = false;
            _carriageReturn = false;
        }

#if NET10_0_OR_GREATER
        // Strict decoding of whole quanta, with no padding, gives the same result as the BCL.
        private bool CanDecodeQuanta => _mode == MimeBase64Mode.Strict && _count == 0 && !_padded && !_ended;
#endif

        internal void Reset()
        {
            _group = 0;
            _count = 0;
            _padded = false;
            _ended = false;
            _carriageReturn = false;
        }

        internal OperationStatus Decode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            var consumed = 0;
            var written = 0;
#if NET10_0_OR_GREATER
            var quantaFrom = 0;
#endif
            while (consumed < source.Length)
            {
#if NET10_0_OR_GREATER
                if (consumed >= quantaFrom && CanDecodeQuanta)
                {
                    var run = source.Slice(consumed);
                    var length = run.IndexOfAnyExcept(MimeBase64.Utf8Alphabet);
                    var count = Math.Min((length < 0 ? run.Length : length) / 4, (destination.Length - written) / 3);
                    if (count > 0)
                    {
                        Base64.DecodeFromUtf8(run.Slice(0, count * 4), destination.Slice(written), out _, out var decoded, true);
                        consumed += count * 4;
                        written += decoded;
                    }

                    quantaFrom = consumed + 1;
                    continue;
                }
#endif
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
            bytesWritten = written;
            return Finish(isFinalBlock);
        }

        internal OperationStatus Decode(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten, bool isFinalBlock)
        {
            var consumed = 0;
            var written = 0;
#if NET10_0_OR_GREATER
            var quantaFrom = 0;
#endif
            while (consumed < source.Length)
            {
#if NET10_0_OR_GREATER
                if (consumed >= quantaFrom && CanDecodeQuanta)
                {
                    var run = source.Slice(consumed);
                    var length = run.IndexOfAnyExcept(MimeBase64.CharAlphabet);
                    var count = Math.Min((length < 0 ? run.Length : length) / 4, (destination.Length - written) / 3);
                    if (count > 0)
                    {
                        Convert.TryFromBase64Chars(run.Slice(0, count * 4), destination.Slice(written), out var decoded);
                        consumed += count * 4;
                        written += decoded;
                    }

                    quantaFrom = consumed + 1;
                    continue;
                }
#endif
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
            bytesWritten = written;
            return Finish(isFinalBlock);
        }

        private OperationStatus Finish(bool isFinalBlock)
        {
            if (!isFinalBlock) return OperationStatus.Done;

            // The data is truncated if it ends inside a quantum or a line break.
            if (_count != 0 || _carriageReturn) return OperationStatus.InvalidData;

            Reset();
            return OperationStatus.Done;
        }

        // Processes one unit of encoded data, returning Done to go on to the next.
        private OperationStatus Process(int unit, Span<byte> destination, ref int written)
        {
            if (_carriageReturn)
            {
                if (unit != '\n') return OperationStatus.InvalidData;

                _carriageReturn = false;
                return OperationStatus.Done;
            }

            var value = unit < MimeBase64.DecodeMap.Length ? MimeBase64.DecodeMap[unit] : -1;
            if (value >= 0)
            {
                // ref. RFC 4648 §4: padding ends the data.
                if (_padded || _ended) return OperationStatus.InvalidData;

                if (_count < 3)
                {
                    _group = (_group << 6) | value;
                    _count++;
                    return OperationStatus.Done;
                }

                if (destination.Length - written < 3) return OperationStatus.DestinationTooSmall;

                var group = (_group << 6) | value;
                destination[written++] = (byte)(group >> 16);
                destination[written++] = (byte)(group >> 8);
                destination[written++] = (byte)group;
                _group = 0;
                _count = 0;
                return OperationStatus.Done;
            }

            if (unit == '=')
            {
                // ref. RFC 4648 §4: a final quantum of 2 characters is padded with "==", and one of 3 characters with "=". The pad bits
                // aren't checked (RFC 4648 §3.5).
                if (_count == 2 && !_padded)
                {
                    _padded = true;
                    return OperationStatus.Done;
                }

                if (_count == 2)
                {
                    if (destination.Length - written < 1) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)(_group >> 4);
                }
                else if (_count == 3)
                {
                    if (destination.Length - written < 2) return OperationStatus.DestinationTooSmall;

                    destination[written++] = (byte)(_group >> 10);
                    destination[written++] = (byte)(_group >> 2);
                }
                else
                {
                    return OperationStatus.InvalidData;
                }

                _group = 0;
                _count = 0;
                _padded = false;
                _ended = true;
                return OperationStatus.Done;
            }

            if (_mode == MimeBase64Mode.Strict) return OperationStatus.InvalidData;

            // ref. RFC 2045 §6.8: characters outside the alphabet, including line breaks, are ignored.
            if (!_rejectNonAlphabet) return OperationStatus.Done;

            if (unit != '\r') return OperationStatus.InvalidData;

            _carriageReturn = true;
            return OperationStatus.Done;
        }
    }
}
