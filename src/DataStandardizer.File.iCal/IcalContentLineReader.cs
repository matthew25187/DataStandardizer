using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to the content lines (RFC 5545 §3.1) of iCalendar data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lines are unfolded, and their structure is checked against RFC 5545 §3.1: a name, any parameters and a value.  The meaning of properties
    /// and values is not checked.  Parameter values have their RFC 6868 caret encoding decoded.  TEXT unescaping depends on the value type, so it
    /// is done only when asked for.
    /// </para>
    /// <para>
    /// A content line ends with a CRLF line break.  A bare line feed is accepted only with <see cref="IcalOptions.AcceptBareLineFeed"/>, and a bare
    /// carriage return is never a line break.  The last line need not end with a line break.  Line breaks never depend on
    /// <see cref="Environment.NewLine"/>.
    /// </para>
    /// <para>
    /// When the data is read from a <see cref="Stream"/> or file as UTF-8, a fold that splits a multi-octet UTF-8 sequence is repaired before
    /// decoding.  A caller-supplied <see cref="TextReader"/> has already decoded the data, so such a fold cannot be repaired.
    /// </para>
    /// <para>
    /// If a line does not follow the content line syntax, an <see cref="IcalFileException"/> is thrown after the line has been consumed, so
    /// reading can continue with the next line.  If the line break or line length is at fault, where the line ends is unknown, so later reads
    /// throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class IcalContentLineReader : IDisposable
    {
        private const int DefaultBufferSize = 16 * 1024;

        /// <summary>
        /// Size, in bytes, of the buffers of the <see cref="StreamReader"/> and <see cref="FileStream"/> the reader creates itself.  A
        /// caller-supplied <see cref="TextReader"/> keeps its own buffering.
        /// </summary>
        internal const int StreamBufferSize = 16 * 1024;

        /// <summary>
        /// Encoding used when none is specified: UTF-8, the default charset of RFC 5545 §3.1.4.
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        private bool _hasCurrentLine;
        private bool _isDisposed;
        private readonly bool _isInternalReader;
        private int _lineVersion;
        private readonly TextReader _reader;
        private readonly IcalContentLineScanner _scanner;

        /// <summary>
        /// Creates a reader for iCalendar data in a stream, read as UTF-8 unless the stream starts with a byte order mark.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support reading.</exception>
        public IcalContentLineReader(Stream icalStream) : this(icalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for iCalendar data in a stream.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support reading.
        /// -or-
        /// <see cref="IcalOptions.MaxLineLength"/> is less than 1.</exception>
        public IcalContentLineReader(Stream icalStream, IcalOptions options) : this(icalStream, options, StreamBufferSize, DefaultBufferSize)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for an iCalendar file, read as UTF-8 unless the file starts with a byte order mark.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> is <c>null</c>.</exception>
        public IcalContentLineReader(string icalFilePath) : this(icalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for an iCalendar file.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.MaxLineLength"/> is less than 1.</exception>
        public IcalContentLineReader(string icalFilePath, IcalOptions options) : this(CreateFileReader(icalFilePath, options), true, options, DefaultBufferSize)
        {
        }
#endif

        /// <summary>
        /// Creates a reader for iCalendar data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public IcalContentLineReader(TextReader reader) : this(reader, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for iCalendar data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the text reader has already decoded
        /// the data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="IcalOptions.MaxLineLength"/> is less than 1.</exception>
        public IcalContentLineReader(TextReader reader, IcalOptions options) : this(reader, options, DefaultBufferSize)
        {
        }

        /// <summary>
        /// Creates a reader with specific buffer sizes.  Allows tests to force refills at chosen points.
        /// </summary>
        internal IcalContentLineReader(Stream icalStream, IcalOptions options, int streamBufferSize, int bufferSize)
            : this(CreateStreamReader(icalStream, options, streamBufferSize), true, options, bufferSize)
        {
        }

        /// <summary>
        /// Creates a reader with a specific size for the internal character block.  Allows tests to force refills at chosen points.
        /// </summary>
        internal IcalContentLineReader(TextReader reader, IcalOptions options, int bufferSize) : this(ValidateTextReader(reader, options), false, options, bufferSize)
        {
        }

        private IcalContentLineReader(TextReader reader, bool isInternalReader, IcalOptions options, int bufferSize)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _reader = reader;
            _isInternalReader = isInternalReader;
            _scanner = new IcalContentLineScanner(bufferSize, options.AcceptBareLineFeed, options.MaxLineLength ?? int.MaxValue);
        }

        /// <summary>
        /// Gets a view of the line last read by <see cref="ReadContentLine"/> or <see cref="ReadLine"/>, without allocation.
        /// </summary>
        /// <remarks>
        /// The view, and any span or memory taken from it, is valid only until the next read or until the reader is disposed.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">No line is current: none has been read yet, the last read reached the end of the data, or
        /// it threw an exception.</exception>
        public IcalContentLineView CurrentLine
        {
            get
            {
                ThrowIfDisposed();
                if (!_hasCurrentLine)
                    throw new InvalidOperationException("No content line is current.");

                return new IcalContentLineView(this, _lineVersion);
            }
        }

        /// <summary>
        /// Gets the number of logical (unfolded) lines read so far, including any that failed to parse.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long LinesRead
        {
            get
            {
                ThrowIfDisposed();
                return _scanner.LinesRead;
            }
        }

        /// <summary>
        /// Gets the number of physical lines read so far.  A folded logical line spans several physical lines.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long PhysicalLinesRead
        {
            get
            {
                ThrowIfDisposed();
                return _scanner.PhysicalLinesRead;
            }
        }

        /// <summary>
        /// Releases the reader's buffers, and the stream or file it opened itself.  A caller-supplied stream or text reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.
        /// </remarks>
        public void Dispose()
        {
            if (_isDisposed) return;

            _isDisposed = true;
            _hasCurrentLine = false;

            if (_isInternalReader)
            {
                _reader.Dispose();
            }

            _scanner.ReturnBuffers();
        }

        /// <summary>
        /// Reads the next content line, without allocation.  The line is then available from <see cref="CurrentLine"/>.
        /// </summary>
        /// <returns><c>true</c> if a line was read; <c>false</c> if the end of the data has been reached.</returns>
        /// <exception cref="IcalFileException">The line does not follow the content line syntax of RFC 5545 §3.1, has a line break that is not
        /// accepted, or is longer than <see cref="IcalOptions.MaxLineLength"/>.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">An earlier read found a line break or line length error, so the reader cannot
        /// continue.</exception>
        public bool ReadContentLine()
        {
            ThrowIfDisposed();
            if (_scanner.IsFaulted)
                throw new InvalidOperationException("The reader cannot continue after a line break or line length error.");

            // Any view of the previous line is now out of date.
            unchecked
            {
                _lineVersion++;
            }

            _hasCurrentLine = false;

            if (!ScanLine()) return false;

            _scanner.ParseLine();
            _hasCurrentLine = true;
            return true;
        }

        /// <summary>
        /// Reads the next content line.
        /// </summary>
        /// <returns>The line; or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="IcalFileException">The line does not follow the content line syntax of RFC 5545 §3.1, has a line break that is not
        /// accepted, or is longer than <see cref="IcalOptions.MaxLineLength"/>.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">An earlier read found a line break or line length error, so the reader cannot
        /// continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IcalContentLine? ReadLine()
#else
        public IcalContentLine ReadLine()
#endif
        {
            return ReadContentLine() ? IcalContentLine.FromScanner(_scanner) : null;
        }

        /// <summary>
        /// Reads the remaining content lines.
        /// </summary>
        /// <returns>The lines, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="IcalFileException">During enumeration: a line does not follow the content line syntax, has a line break that is not
        /// accepted, or is too long.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: an earlier read found a line break or line length error.</exception>
        public IEnumerable<IcalContentLine> ReadLines()
        {
            // Check now, so the exception comes from this call and not from the first MoveNext.
            ThrowIfDisposed();

            return ReadLinesIterator();
        }

        /// <summary>
        /// Gets the scanner holding the current line, for a view of that line.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on from the line the view was taken for.</exception>
        internal IcalContentLineScanner GetScanner(int lineVersion)
        {
            ThrowIfDisposed();
            if (!_hasCurrentLine || lineVersion != _lineVersion)
                throw new InvalidOperationException("The reader has moved on from the content line of this view.");

            return _scanner;
        }

        private IEnumerable<IcalContentLine> ReadLinesIterator()
        {
            while (true)
            {
                var line = ReadLine();
                if (line is null) yield break;

                yield return line;
            }
        }

        /// <summary>
        /// Drives the scanner synchronously until it has a whole line or the data ends.
        /// </summary>
        private bool ScanLine()
        {
            while (true)
            {
                var status = _scanner.Scan();
                if (status != IcalContentLineScanner.ScanStatus.NeedMoreInput)
                {
                    return status == IcalContentLineScanner.ScanStatus.LineRead;
                }

                int offset, count;
                _scanner.PrepareRefill(out offset, out count);
                _scanner.CompleteRefill(_reader.Read(_scanner.Buffer, offset, count));
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(GetType().Name);
        }

        private static TextReader CreateStreamReader(Stream stream, IcalOptions options, int bufferSize)
        {
            if (stream is null)
                throw new ArgumentNullException("icalStream");
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "icalStream");
            ValidateOptions(options);

            return CreateStreamReader(stream, false, options.Encoding ?? DefaultEncoding, bufferSize);
        }

        /// <summary>
        /// Creates the stream reader, repairing folds inside UTF-8 sequences when the data is UTF-8.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="ownsStream"><c>true</c> if disposing the stream reader should dispose <paramref name="stream"/>.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="bufferSize">The buffer size, in bytes.</param>
        private static StreamReader CreateStreamReader(Stream stream, bool ownsStream, Encoding encoding, int bufferSize)
        {
            if (encoding is UTF8Encoding)
            {
                var foldRealigningStream = new Utf8FoldRealigningStream(stream, !ownsStream, bufferSize);
                return new StreamReader(foldRealigningStream, encoding, true, bufferSize, false);
            }

            return new StreamReader(stream, encoding, true, bufferSize, !ownsStream);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static StreamReader CreateFileReader(string path, IcalOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("icalFilePath");
            ValidateOptions(options);

            // Open the file stream explicitly so both buffering layers are sized together.
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.SequentialScan);
            try
            {
                return CreateStreamReader(fileStream, true, options.Encoding ?? DefaultEncoding, StreamBufferSize);
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
        }
#endif

        private static TextReader ValidateTextReader(TextReader reader, IcalOptions options)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));
            ValidateOptions(options);
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing reader.  Specify the encoding when creating the reader.", nameof(options));

            return reader;
        }

        private static void ValidateOptions(IcalOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.MaxLineLength < 1)
                throw new ArgumentException("The maximum line length must be at least 1.", nameof(options));
        }
    }
}
