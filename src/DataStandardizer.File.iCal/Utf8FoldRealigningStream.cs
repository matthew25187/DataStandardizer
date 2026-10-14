using System;
using System.IO;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A read-only stream over UTF-8 iCalendar data that moves each fold found inside a multi-octet UTF-8 sequence to just after that sequence.
    /// </summary>
    /// <remarks>
    /// <para>
    /// RFC 5545 §3.1 notes that simple writers may fold in the middle of a multi-octet UTF-8 sequence, and that readers must unfold so as to
    /// restore it.  The content line scanner unfolds characters, after a <see cref="StreamReader"/> has decoded them, and decoding a sequence with
    /// a fold inside it gives replacement characters.  Moving the fold rather than removing it leaves the decoded line the same once unfolded,
    /// and keeps every line break, so physical line numbers stay right.
    /// </para>
    /// <para>
    /// A fold is a line break (CRLF, or a bare line feed) followed by a space or horizontal tab.  Bytes that are not part of a fold inside a
    /// sequence pass through unchanged, including invalid UTF-8, which is left for the decoder.  If the data starts with a UTF-16 or UTF-32 byte
    /// order mark, which the <see cref="StreamReader"/> will honour, every byte passes through unchanged.
    /// </para>
    /// </remarks>
    internal sealed class Utf8FoldRealigningStream : Stream
    {
        private const byte CarriageReturn = 0x0D;
        private const byte LineFeed = 0x0A;
        private const byte Space = 0x20;
        private const byte Tab = 0x09;

        private int _continuationCount;
        private byte[] _deferred = new byte[8];
        private int _deferredCount;
        private bool _hasCheckedByteOrderMark;
        private readonly Stream _inner;
        private bool _innerEnded;
        private readonly byte[] _input;
        private int _inputEnd;
        private int _inputStart;
        private bool _isDisposed;
        private bool _isPassThrough;
        private readonly bool _leaveOpen;
        private byte[] _output;
        private int _outputEnd;
        private int _outputStart;

        /// <param name="inner">The stream to read from.</param>
        /// <param name="leaveOpen"><c>true</c> to leave <paramref name="inner"/> open when this stream is disposed.</param>
        /// <param name="bufferSize">The size, in bytes, of the input buffer.</param>
        internal Utf8FoldRealigningStream(Stream inner, bool leaveOpen, int bufferSize)
        {
            _inner = inner;
            _leaveOpen = leaveOpen;

            // The buffer must hold a whole fold and the four bytes of the byte order mark check.
            _input = new byte[Math.Max(bufferSize, 4)];
            _output = new byte[_input.Length + _deferred.Length];
        }

        public override bool CanRead => !_isDisposed;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (count < 0 || count > buffer.Length - offset)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
            if (count == 0) return 0;

            while (_outputStart == _outputEnd)
            {
                if (!Process()) return 0;
            }

            var copyCount = Math.Min(count, _outputEnd - _outputStart);
            Buffer.BlockCopy(_output, _outputStart, buffer, offset, copyCount);
            _outputStart += copyCount;
            return copyCount;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                _isDisposed = true;
                if (!_leaveOpen)
                {
                    _inner.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Moves bytes from the input buffer to the output buffer, reading more input when the input buffer is empty.
        /// </summary>
        /// <returns><c>false</c> if the inner stream has ended and there is no more output.</returns>
        private bool Process()
        {
            _outputStart = _outputEnd = 0;

            if (!_hasCheckedByteOrderMark)
            {
                while (_inputEnd - _inputStart < 4 && FillInput())
                {
                }

                _isPassThrough = StartsWithWideByteOrderMark();
                _hasCheckedByteOrderMark = true;
            }

            if (_inputStart == _inputEnd && !FillInput())
            {
                // The data ended inside a sequence, which the decoder will report; keep any fold found in it.
                FlushDeferred();
                return _outputEnd > 0;
            }

            if (_isPassThrough)
            {
                Emit(_input, _inputStart, _inputEnd - _inputStart);
                _inputStart = _inputEnd;
                return true;
            }

            while (_inputStart < _inputEnd)
            {
                var value = _input[_inputStart];
                if (_continuationCount == 0)
                {
                    // Pass ASCII through in runs.
                    var runEnd = _inputStart;
                    while (runEnd < _inputEnd && _input[runEnd] < 0x80)
                    {
                        runEnd++;
                    }

                    if (runEnd > _inputStart)
                    {
                        Emit(_input, _inputStart, runEnd - _inputStart);
                        _inputStart = runEnd;
                        continue;
                    }

                    Emit(value);
                    _continuationCount = GetContinuationCount(value);
                    _inputStart++;
                    continue;
                }

                if ((value & 0xC0) == 0x80)
                {
                    Emit(value);
                    _inputStart++;
                    if (--_continuationCount == 0)
                    {
                        FlushDeferred();
                    }

                    continue;
                }

                if (value == CarriageReturn || value == LineFeed)
                {
                    var foldLength = GetFoldLength();
                    if (foldLength < 0)
                    {
                        // Read on to see whether the line break is a fold, then look again.
                        FillInput();
                        continue;
                    }

                    if (foldLength > 0)
                    {
                        Defer(foldLength);
                        continue;
                    }
                }

                // Anything else ends the sequence early, which is invalid UTF-8 the decoder will report.  Keep any fold found in it, then handle
                // this byte afresh.
                _continuationCount = 0;
                FlushDeferred();
            }

            return true;
        }

        private void Defer(int count)
        {
            if (_deferredCount + count > _deferred.Length)
            {
                Array.Resize(ref _deferred, Math.Max(_deferred.Length * 2, _deferredCount + count));
            }

            Buffer.BlockCopy(_input, _inputStart, _deferred, _deferredCount, count);
            _deferredCount += count;
            _inputStart += count;
        }

        private void Emit(byte value)
        {
            EnsureOutputCapacity(1);
            _output[_outputEnd++] = value;
        }

        private void Emit(byte[] source, int offset, int count)
        {
            EnsureOutputCapacity(count);
            Buffer.BlockCopy(source, offset, _output, _outputEnd, count);
            _outputEnd += count;
        }

        private void EnsureOutputCapacity(int count)
        {
            if (_outputEnd + count > _output.Length)
            {
                Array.Resize(ref _output, Math.Max(_output.Length * 2, _outputEnd + count));
            }
        }

        /// <summary>
        /// Reads more input after any unprocessed input.
        /// </summary>
        /// <returns><c>false</c> if the inner stream has ended.</returns>
        private bool FillInput()
        {
            if (_innerEnded) return false;

            if (_inputStart > 0)
            {
                Buffer.BlockCopy(_input, _inputStart, _input, 0, _inputEnd - _inputStart);
                _inputEnd -= _inputStart;
                _inputStart = 0;
            }

            var read = _inner.Read(_input, _inputEnd, _input.Length - _inputEnd);
            if (read == 0)
            {
                _innerEnded = true;
                return false;
            }

            _inputEnd += read;
            return true;
        }

        private void FlushDeferred()
        {
            if (_deferredCount == 0) return;

            Emit(_deferred, 0, _deferredCount);
            _deferredCount = 0;
        }

        /// <summary>
        /// Gets the length of the fold starting at the line break at the start of the input.
        /// </summary>
        /// <returns>The number of bytes in the fold; 0 if the line break does not start a fold; or -1 if more input is needed to tell.</returns>
        private int GetFoldLength()
        {
            var available = _inputEnd - _inputStart;
            var lineBreakLength = 1;
            if (_input[_inputStart] == CarriageReturn)
            {
                if (available < 2) return _innerEnded ? 0 : -1;
                if (_input[_inputStart + 1] != LineFeed) return 0;

                lineBreakLength = 2;
            }

            if (available < lineBreakLength + 1) return _innerEnded ? 0 : -1;

            var whiteSpace = _input[_inputStart + lineBreakLength];
            return whiteSpace == Space || whiteSpace == Tab ? lineBreakLength + 1 : 0;
        }

        private bool StartsWithWideByteOrderMark()
        {
            var available = _inputEnd - _inputStart;
            if (available < 2) return false;

            var first = _input[_inputStart];
            var second = _input[_inputStart + 1];

            // UTF-16 LE and UTF-32 LE start FF FE, UTF-16 BE starts FE FF, and UTF-32 BE starts 00 00 FE FF.
            return (first == 0xFF && second == 0xFE)
                   || (first == 0xFE && second == 0xFF)
                   || (available >= 4 && first == 0x00 && second == 0x00 && _input[_inputStart + 2] == 0xFE && _input[_inputStart + 3] == 0xFF);
        }

        /// <summary>
        /// Gets the number of continuation bytes that follow a UTF-8 lead byte.
        /// </summary>
        private static int GetContinuationCount(byte leadByte)
        {
            if (leadByte >= 0xC2 && leadByte <= 0xDF) return 1;
            if (leadByte >= 0xE0 && leadByte <= 0xEF) return 2;
            if (leadByte >= 0xF0 && leadByte <= 0xF4) return 3;
            return 0;
        }
    }
}
