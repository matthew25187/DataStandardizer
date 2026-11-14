using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Reads the characters of a stream in an encoding, and reads each byte that isn't part of a character of the encoding as a lone surrogate.
    /// </summary>
    /// <remarks>
    /// A <see cref="StreamReader"/> decodes a byte sequence that isn't valid in its encoding as a replacement, such as U+FFFD or '?', which a CSV
    /// reader can't tell from the same character in the file.  This reader decodes each byte the decoder can't decode as the lone low surrogate
    /// <c>U+DC00</c> plus the byte, as Python's <c>surrogateescape</c> error handler does.  A lone surrogate isn't a character of any character set,
    /// so the CSV reader reports the field that holds it as a bad value, and its raw value shows the bytes.  A lone low surrogate never joins the
    /// character before it, as a decoder never leaves a high surrogate without its low surrogate.
    /// <para>
    /// A decoder fallback can't write a lone surrogate, so the reader finds the bytes the decoder can't decode in one of two ways, which escape the
    /// same bytes.  Where .NET lets a decoder have a custom fallback, the fallback writes U+FFFD for each byte and records the bytes.  The reader
    /// counts the characters of each block of bytes first, which doesn't change the decoder's state, and if the decoder fell back, decodes the
    /// block a byte at a time, so that the characters the fallback wrote are the first decoded from each byte, and are replaced.  On
    /// .NET Standard 1.1, which has no custom fallback, the decoder throws instead.  The reader then keeps the decoder empty between blocks,
    /// carrying an unfinished character into the next block, so that it always knows which bytes the decoder holds.  When the decoder throws for
    /// the first of them, the reader escapes them, resets the decoder and decodes the bytes after them again.
    /// </para>
    /// <para>
    /// A byte order mark at the start of the stream chooses the encoding when none is declared, as for a <see cref="StreamReader"/> that detects
    /// one.  When an encoding is declared, a byte order mark of that encoding is passed over, and any other is read as the declared encoding's
    /// characters, as the declared encoding wins.  Those that aren't valid in it are escaped, and those that are, such as a UTF-8 mark read as
    /// ISO 8859-1, are read as the characters they are.
    /// </para>
    /// <para>
    /// A custom <see cref="Encoding"/> whose decoder ignores <c>Decoder.Fallback</c> decodes a byte it can't decode its own way, so such a byte
    /// isn't escaped.
    /// </para>
    /// </remarks>
    internal sealed class CsvStreamTextReader : TextReader
    {
        private const char FirstEscapedByte = '\xDC00';
        private const int ByteOrderMarkMaxLength = 4;

        // The longest character of a .NET encoding is four bytes, so an unfinished one at the end of a block is at most three.
        private const int MaxUnfinishedCharacterLength = 3;

        // The number of bytes counted at a time in a block the decoder falls back in, so that only the runs with a byte it can't decode are
        // decoded a byte at a time.
        private const int CheckedRunLength = 64;

        private const string Utf8WebName = "utf-8";
        private const string Utf16LittleEndianWebName = "utf-16";
        private const string Utf16BigEndianWebName = "utf-16BE";
        private const string Utf32LittleEndianWebName = "utf-32";
        private const string Utf32BigEndianWebName = "utf-32BE";

        // Byte order marks, U+FEFF in each Unicode encoding.  UTF-32 little endian's begins with UTF-16 little endian's, so it is checked first.
        private static readonly KeyValuePair<string, byte[]>[] ByteOrderMarks = new KeyValuePair<string, byte[]>[]
        {
            new KeyValuePair<string, byte[]>(Utf32LittleEndianWebName, new byte[] { 0xFF, 0xFE, 0x00, 0x00 }),
            new KeyValuePair<string, byte[]>(Utf32BigEndianWebName, new byte[] { 0x00, 0x00, 0xFE, 0xFF }),
            new KeyValuePair<string, byte[]>(Utf8WebName, new byte[] { 0xEF, 0xBB, 0xBF }),
            new KeyValuePair<string, byte[]>(Utf16LittleEndianWebName, new byte[] { 0xFF, 0xFE }),
            new KeyValuePair<string, byte[]>(Utf16BigEndianWebName, new byte[] { 0xFE, 0xFF }),
        };

        private static readonly byte[] NoBytes = new byte[0];

        private readonly Stream _stream;
        private readonly bool _isEncodingDeclared;
        private readonly bool _escapesByException;
        private readonly byte[] _bytes;
#if !NETSTANDARD1_1
        private readonly EscapingDecoderFallback _fallback = new EscapingDecoderFallback();
#endif

        // The encoding's decoder, until the start of the stream settles the encoding and the decoder is set up to escape bytes.
        private Decoder _decoder;
        private int _byteStart;
        private int _byteEnd;
        private bool _isStarted;
        private bool _isEndOfStream;
        private bool _isFlushed;
        private bool _isTailCarried;
        private char[] _chars;
        private int _charStart;
        private int _charEnd;

        /// <summary>
        /// Creates a reader of a stream.
        /// </summary>
        /// <param name="stream">The stream, which the reader disposes of.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="isEncodingDeclared">
        /// <c>true</c> if the caller declared the encoding, so a byte order mark of another encoding doesn't replace it; <c>false</c> if it is the
        /// default, which a byte order mark replaces.
        /// </param>
        /// <param name="bufferSize">The number of bytes to read from the stream at a time, which is at least the length of any byte order mark.</param>
        internal CsvStreamTextReader(Stream stream, Encoding encoding, bool isEncodingDeclared, int bufferSize)
#if NETSTANDARD1_1
            : this(stream, encoding, isEncodingDeclared, bufferSize, escapeByException: true)
#else
            : this(stream, encoding, isEncodingDeclared, bufferSize, escapeByException: false)
#endif
        {
        }

        /// <summary>
        /// Creates a reader of a stream that finds the bytes it can't decode by the decoder throwing, as on .NET Standard 1.1, or by a fallback.
        /// Allows tests to show that both ways escape the same bytes.
        /// </summary>
        internal CsvStreamTextReader(Stream stream, Encoding encoding, bool isEncodingDeclared, int bufferSize, bool escapeByException)
        {
            _stream = stream;
            CurrentEncoding = encoding;
            _isEncodingDeclared = isEncodingDeclared;
            _bytes = new byte[Math.Max(bufferSize, ByteOrderMarkMaxLength)];
            _chars = new char[_bytes.Length];
            _escapesByException = escapeByException;
            _decoder = encoding.GetDecoder();
        }

        /// <summary>
        /// Gets the encoding the reader decodes: the one it was created with, or the one a byte order mark at the start of the stream names.
        /// </summary>
        internal Encoding CurrentEncoding { get; private set; }

        public override int Peek()
        {
            if (_charStart == _charEnd && !FillChars()) return -1;

            return _chars[_charStart];
        }

        public override int Read()
        {
            if (_charStart == _charEnd && !FillChars()) return -1;

            return _chars[_charStart++];
        }

        public override int Read(char[] buffer, int index, int count)
        {
            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (buffer.Length - index < count)
                throw new ArgumentException("The buffer is too small for the index and count given.");

            if (count == 0) return 0;
            if (_charStart == _charEnd && !FillChars()) return 0;

            var read = Math.Min(count, _charEnd - _charStart);
            Array.Copy(_chars, _charStart, buffer, index, read);
            _charStart += read;
            return read;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Decodes more characters, reading more of the stream if needed.
        /// </summary>
        /// <returns><c>true</c> if there are characters to read; <c>false</c> if the end of the stream has been reached.</returns>
        private bool FillChars()
        {
            _charStart = _charEnd = 0;
            while (_charEnd == 0)
            {
                if (_isFlushed) return false;

                // More of the stream is read once the bytes read are decoded, or only an unfinished character is left of them.
                if (!_isEndOfStream && (_byteStart == _byteEnd || _isTailCarried))
                {
                    ReadBytes();
                    continue;
                }

                // At the end of the stream, a byte sequence the decoder holds unfinished can't be decoded.  A fallback finds it once the bytes
                // read are decoded, and an exception as they are.
                _isFlushed = _isEndOfStream && (_byteStart == _byteEnd || _escapesByException);
                DecodeBytes(_isFlushed);
            }

            return true;
        }

        private void ReadBytes()
        {
            // An unfinished character left at the end of the last block moves to the front, and the stream is read after it.
            var carried = _byteEnd - _byteStart;
            Array.Copy(_bytes, _byteStart, _bytes, 0, carried);
            _byteStart = 0;
            _byteEnd = carried;
            _isTailCarried = false;
            if (!_isStarted)
            {
                // Read enough of the start of the stream to hold any byte order mark.
                _isStarted = true;
                while (_byteEnd < ByteOrderMarkMaxLength && !_isEndOfStream)
                {
                    ReadMoreBytes();
                }

                StartDecoding();
                return;
            }

            ReadMoreBytes();
        }

        private void ReadMoreBytes()
        {
            var read = _stream.Read(_bytes, _byteEnd, _bytes.Length - _byteEnd);
            if (read == 0)
            {
                _isEndOfStream = true;
                return;
            }

            _byteEnd += read;
        }

        /// <summary>
        /// Settles the encoding from any byte order mark at the start of the stream, passes over the mark, and creates the decoder.
        /// </summary>
        private void StartDecoding()
        {
            var declaredWebName = _isEncodingDeclared ? CsvTextData.GetWebName(CurrentEncoding) : null;
            foreach (var byteOrderMark in ByteOrderMarks)
            {
                if (!StartsWith(byteOrderMark.Value)) continue;

                if (_isEncodingDeclared)
                {
                    // Only the declared encoding's own mark is passed over.  UTF-16 little endian's mark followed by U+0000 looks like UTF-32 little
                    // endian's, so every mark is tried.
                    if (!string.Equals(declaredWebName, byteOrderMark.Key, StringComparison.OrdinalIgnoreCase)) continue;
                }
                else
                {
                    // The first mark that matches names the encoding, so a UTF-32 little endian mark isn't taken for UTF-16 little endian's.
                    CurrentEncoding = CreateUnicodeEncoding(byteOrderMark.Key);
                }

                _byteStart = byteOrderMark.Value.Length;
                break;
            }

            var decoder = CurrentEncoding.GetDecoder();
            if (_escapesByException)
            {
                SetExceptionFallback(decoder);
            }
#if !NETSTANDARD1_1
            else
            {
                decoder.Fallback = _fallback;
            }
#endif

            _decoder = decoder;
        }

        private bool StartsWith(byte[] bytes)
        {
            if (_byteEnd < bytes.Length) return false;

            for (var index = 0; index < bytes.Length; index++)
            {
                if (_bytes[index] != bytes[index]) return false;
            }

            return true;
        }

        private static Encoding CreateUnicodeEncoding(string webName)
        {
            switch (webName)
            {
#if NETSTANDARD1_1
                case Utf32LittleEndianWebName:
                case Utf32BigEndianWebName:
                    return Encoding.GetEncoding(webName);
#else
                case Utf32LittleEndianWebName:
                    return new UTF32Encoding(false, true);
                case Utf32BigEndianWebName:
                    return new UTF32Encoding(true, true);
#endif
                case Utf16LittleEndianWebName:
                    return new UnicodeEncoding(false, true);
                case Utf16BigEndianWebName:
                    return new UnicodeEncoding(true, true);
                default:
                    return new UTF8Encoding(true);
            }
        }

        /// <summary>
        /// Makes a decoder throw for a byte sequence it can't decode.
        /// </summary>
        /// <remarks>
        /// .NET Standard 1.1 has no <c>Decoder.Fallback</c>, though every .NET runtime that loads it does, so the property is found at run time.
        /// A runtime whose reflection metadata leaves it out, such as .NET Native, keeps the encoding's own fallback, so it reads a byte it can't
        /// decode as that fallback's replacement, as a caller's <see cref="StreamReader"/> does, rather than failing to read any stream.
        /// </remarks>
        private static void SetExceptionFallback(Decoder decoder)
        {
#if NETSTANDARD1_1
            var fallbackProperty = typeof(Decoder).GetRuntimeProperty("Fallback");
            var exceptionFallbackType = typeof(Decoder).GetTypeInfo().Assembly.GetType("System.Text.DecoderExceptionFallback");
            if (fallbackProperty is null || exceptionFallbackType is null) return;

            fallbackProperty.SetValue(decoder, Activator.CreateInstance(exceptionFallbackType));
#else
            decoder.Fallback = DecoderFallback.ExceptionFallback;
#endif
        }

        /// <summary>
        /// Decodes the unread bytes, or, with <paramref name="flush"/>, whatever the decoder holds at the end of the stream.
        /// </summary>
        private void DecodeBytes(bool flush)
        {
            if (_escapesByException)
            {
                DecodeBytesByException(flush);
                return;
            }
#if !NETSTANDARD1_1
            DecodeBytesByFallback(flush);
            _byteStart = _byteEnd;
#endif
        }

#if !NETSTANDARD1_1
        private void DecodeBytesByFallback(bool flush)
        {
            var decoder = _decoder;
            var bytes = flush ? NoBytes : _bytes;
            var byteStart = flush ? 0 : _byteStart;
            var byteCount = flush ? 0 : _byteEnd - _byteStart;

            // Counting doesn't change the decoder's state, so the bytes can still be decoded a byte at a time if the decoder falls back.  At the end
            // of the stream, every character the decoder writes is a fallback for the bytes it holds.
            _fallback.Reset();
            var charCount = decoder.GetCharCount(bytes, byteStart, byteCount, flush);
            if (!_fallback.HasFallenBack || flush)
            {
                EnsureCharCapacity(charCount);
                _fallback.Reset();
                _charEnd = decoder.GetChars(bytes, byteStart, byteCount, _chars, 0, flush);
                EscapeFallbackCharacters(0, _charEnd);
                return;
            }

            // Each run of bytes is counted the same way, so only a run the decoder falls back in is decoded a byte at a time.
            var end = byteStart + byteCount;
            for (var runStart = byteStart; runStart < end; runStart += CheckedRunLength)
            {
                var runLength = Math.Min(CheckedRunLength, end - runStart);
                _fallback.Reset();
                var runCharCount = decoder.GetCharCount(bytes, runStart, runLength, false);
                if (!_fallback.HasFallenBack)
                {
                    EnsureCharCapacity(_charEnd + runCharCount);
                    _charEnd += decoder.GetChars(bytes, runStart, runLength, _chars, _charEnd, false);
                    continue;
                }

                for (var index = runStart; index < runStart + runLength; index++)
                {
                    EnsureCharCapacity(_charEnd + decoder.GetCharCount(bytes, index, 1, false));
                    _fallback.Reset();
                    var decoded = decoder.GetChars(bytes, index, 1, _chars, _charEnd, false);
                    EscapeFallbackCharacters(_charEnd, decoded);
                    _charEnd += decoded;
                }
            }
        }

        /// <summary>
        /// Replaces the characters the fallback wrote, which come first in the characters decoded, with a lone surrogate for each byte.
        /// </summary>
        private void EscapeFallbackCharacters(int start, int count)
        {
            var unknownBytes = _fallback.UnknownBytes;
            var escapedCount = Math.Min(unknownBytes.Count, count);
            for (var index = 0; index < escapedCount; index++)
            {
                _chars[start + index] = (char)(FirstEscapedByte + unknownBytes[index]);
            }
        }
#endif

        /// <summary>
        /// Decodes the unread bytes with a decoder that throws for bytes it can't decode, and, with <paramref name="flush"/>, whatever the decoder
        /// holds at the end of the stream.
        /// </summary>
        /// <remarks>
        /// The decoder holds nothing at the start of a block.  A block that decodes whole, or does once the last few bytes of an unfinished
        /// character are left for the next block, is decoded in one call.  Otherwise its bytes are decoded one at a time.  The decoder holds the
        /// bytes since the last that gave a character, so when it throws, the first of those, as many as the exception gives, are escaped, the
        /// decoder is reset, and the bytes after them are decoded again.  An exception's index isn't used, as a UTF-16 decoder gives one that
        /// doesn't reach back to the bytes it held.
        /// </remarks>
        private void DecodeBytesByException(bool flush)
        {
            if (TryDecodeBlock(_byteEnd, flush)) return;

            if (!flush)
            {
                for (var tailLength = 1; tailLength <= MaxUnfinishedCharacterLength && _byteEnd - tailLength > _byteStart; tailLength++)
                {
                    if (TryDecodeBlock(_byteEnd - tailLength, false)) return;
                }
            }

            var decoder = _decoder;
            var heldStart = _byteStart;
            var index = _byteStart;
            while (true)
            {
                var isFlush = index == _byteEnd;
                if (isFlush && (!flush || heldStart == _byteEnd)) break;

                var count = isFlush ? 0 : 1;
                try
                {
                    EnsureCharCapacity(_charEnd + decoder.GetCharCount(_bytes, index, count, isFlush));
                    var decoded = decoder.GetChars(_bytes, index, count, _chars, _charEnd, isFlush);
                    _charEnd += decoded;
                    if (isFlush)
                    {
                        heldStart = _byteEnd;
                        break;
                    }

                    index++;
                    if (decoded > 0) heldStart = index;
                }
                catch (DecoderFallbackException exception)
                {
                    var heldCount = index + count - heldStart;
                    var unknownCount = Math.Max(1, Math.Min(exception.BytesUnknown?.Length ?? 0, heldCount));
                    EnsureCharCapacity(_charEnd + unknownCount);
                    for (var offset = 0; offset < unknownCount; offset++)
                    {
                        _chars[_charEnd++] = (char)(FirstEscapedByte + _bytes[heldStart + offset]);
                    }

                    decoder.Reset();
                    heldStart += unknownCount;
                    index = heldStart;
                }
            }

            // Bytes the decoder still holds are an unfinished character, which the next block finishes.
            if (heldStart < _byteEnd)
            {
                decoder.Reset();
                _isTailCarried = true;
            }

            _byteStart = heldStart;
        }

        /// <summary>
        /// Decodes the unread bytes up to <paramref name="end"/> in one call, if they hold only whole characters the decoder can decode.
        /// </summary>
        /// <remarks>
        /// Counting as if flushing finds an unfinished character at the end as well as bytes the decoder can't decode, and doesn't change its
        /// state.  The bytes are then decoded without flushing, unless the stream has ended, so that a stateful encoding, such as ISO-2022-JP,
        /// keeps its shift state into the next block.
        /// </remarks>
        private bool TryDecodeBlock(int end, bool flush)
        {
            var decoder = _decoder;
            try
            {
                EnsureCharCapacity(decoder.GetCharCount(_bytes, _byteStart, end - _byteStart, true));
                _charEnd = decoder.GetChars(_bytes, _byteStart, end - _byteStart, _chars, 0, flush);
            }
            catch (DecoderFallbackException)
            {
                return false;
            }

            _isTailCarried = end < _byteEnd;
            _byteStart = end;
            return true;
        }

        private void EnsureCharCapacity(int capacity)
        {
            if (_chars.Length >= capacity) return;

            var chars = new char[Math.Max(capacity, _chars.Length * 2)];
            Array.Copy(_chars, chars, _charEnd);
            _chars = chars;
        }

#if !NETSTANDARD1_1
        /// <summary>
        /// A decoder fallback that writes U+FFFD for each byte it is given, and records the bytes, so that the reader can replace each U+FFFD with
        /// the byte's lone surrogate.
        /// </summary>
        private sealed class EscapingDecoderFallback : DecoderFallback
        {
            // The longest byte sequence a decoder of .NET falls back on at once is that of one character, at most four bytes.
            private const int MaxUnknownByteCount = 4;

            internal List<byte> UnknownBytes { get; } = new List<byte>();

            internal bool HasFallenBack { get; private set; }

            public override int MaxCharCount => MaxUnknownByteCount;

            public override DecoderFallbackBuffer CreateFallbackBuffer() => new EscapingDecoderFallbackBuffer(this);

            internal void Reset()
            {
                UnknownBytes.Clear();
                HasFallenBack = false;
            }

            internal void Add(byte[] bytesUnknown)
            {
                UnknownBytes.AddRange(bytesUnknown);
                HasFallenBack = true;
            }
        }

        private sealed class EscapingDecoderFallbackBuffer : DecoderFallbackBuffer
        {
            private const char ReplacementCharacter = '\xFFFD';

            private readonly EscapingDecoderFallback _fallback;
            private int _count;
            private int _remaining;

            internal EscapingDecoderFallbackBuffer(EscapingDecoderFallback fallback)
            {
                _fallback = fallback;
            }

            public override int Remaining => _remaining;

            public override bool Fallback(byte[] bytesUnknown, int index)
            {
                _fallback.Add(bytesUnknown);
                _count = _remaining = bytesUnknown.Length;
                return _count > 0;
            }

            public override char GetNextChar()
            {
                if (_remaining == 0) return '\0';

                _remaining--;
                return ReplacementCharacter;
            }

            public override bool MovePrevious()
            {
                if (_remaining == _count) return false;

                _remaining++;
                return true;
            }

            public override void Reset()
            {
                _count = _remaining = 0;
            }
        }
#endif
    }
}
