using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

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
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while
    /// another is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class IcalContentLineReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int DefaultBufferSize = 16 * 1024;

        /// <summary>
        /// Size, in bytes, of the buffers of the <see cref="StreamReader"/> and <c>FileStream</c> the reader creates itself.  A
        /// caller-supplied <see cref="TextReader"/> keeps its own buffering.
        /// </summary>
        internal const int StreamBufferSize = 16 * 1024;

        /// <summary>
        /// Encoding used when none is specified: UTF-8, the default charset of RFC 5545 §3.1.4.
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Gives the result of <see cref="ReadContentLine"/>: whether a line was read.
        /// </summary>
        private static readonly Func<IcalContentLineReader, bool, bool> LineReadResult = (reader, lineRead) => lineRead;

        /// <summary>
        /// Gives the result of <see cref="ReadLine"/>: the line read, or <c>null</c>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static readonly Func<IcalContentLineReader, bool, IcalContentLine?> LineResult =
#else
        private static readonly Func<IcalContentLineReader, bool, IcalContentLine> LineResult =
#endif
            (reader, lineRead) => lineRead ? IcalContentLine.FromScanner(reader._scanner) : null;

        private bool _hasCurrentLine;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif

        // These are changed with Interlocked, so that a Dispose from another thread while a read is in progress returns the buffers once only,
        // after that read has finished with them.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

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
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers and
        /// stream are released once it has finished with them.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            _hasCurrentLine = false;

            if (Volatile.Read(ref _isReadInProgress) == 0)
            {
                Release();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Releases the reader's buffers, and the stream or file it opened itself.  A caller-supplied stream or text reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the buffers and stream are released once it has finished with them.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
#endif

        /// <summary>
        /// Reads the next content line, without allocation.  The line is then available from <see cref="CurrentLine"/>.
        /// </summary>
        /// <returns><c>true</c> if a line was read; <c>false</c> if the end of the data has been reached.</returns>
        /// <exception cref="IcalFileException">The line does not follow the content line syntax of RFC 5545 §3.1, has a line break that is not
        /// accepted, or is longer than <see cref="IcalOptions.MaxLineLength"/>.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break or line length error, or was canceled, so the reader cannot continue.</exception>
        public bool ReadContentLine()
        {
            return Read(LineReadResult);
        }

        /// <summary>
        /// Reads the next content line.
        /// </summary>
        /// <returns>The line; or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="IcalFileException">The line does not follow the content line syntax of RFC 5545 §3.1, has a line break that is not
        /// accepted, or is longer than <see cref="IcalOptions.MaxLineLength"/>.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break or line length error, or was canceled, so the reader cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IcalContentLine? ReadLine()
#else
        public IcalContentLine ReadLine()
#endif
        {
            return Read(LineResult);
        }

        /// <summary>
        /// Reads the remaining content lines.
        /// </summary>
        /// <returns>The lines, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="IcalFileException">During enumeration: a line does not follow the content line syntax, has a line break that is not
        /// accepted, or is too long.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read found a line break or
        /// line length error or was canceled.</exception>
        public IEnumerable<IcalContentLine> ReadLines()
        {
            // Check now, so the exception comes from this call and not from the first MoveNext.
            ThrowIfDisposed();

            return ReadLinesIterator();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the next content line asynchronously, without allocation.  The line is then available from <see cref="CurrentLine"/>.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the reader's buffer already holds the whole line.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot
        /// continue.</param>
        /// <returns>A task whose result is <c>true</c> if a line was read, or <c>false</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break or line length error, or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="IcalFileException">By the task: the line does not follow the content line syntax of RFC 5545 §3.1, has a line break
        /// that is not accepted, or is longer than <see cref="IcalOptions.MaxLineLength"/>.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask<bool> ReadContentLineAsync(CancellationToken cancellationToken = default)
        {
            return ReadAsync(LineReadResult, cancellationToken);
        }

        /// <summary>
        /// Reads the next content line asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the reader's buffer already holds the whole line.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot
        /// continue.</param>
        /// <returns>A task whose result is the line, or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break or line length error, or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="IcalFileException">By the task: the line does not follow the content line syntax of RFC 5545 §3.1, has a line break
        /// that is not accepted, or is longer than <see cref="IcalOptions.MaxLineLength"/>.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ValueTask<IcalContentLine?> ReadLineAsync(CancellationToken cancellationToken = default)
#else
        public ValueTask<IcalContentLine> ReadLineAsync(CancellationToken cancellationToken = default)
#endif
        {
            return ReadAsync(LineResult, cancellationToken);
        }

        /// <summary>
        /// Reads the remaining content lines asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is combined with any token passed to
        /// <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.  If a read is canceled, the reader cannot continue.</param>
        /// <returns>The lines, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="IcalFileException">During enumeration: a line does not follow the content line syntax, has a line break that is not
        /// accepted, or is too long.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read found a line break or
        /// line length error or was canceled.</exception>
        /// <exception cref="OperationCanceledException">During enumeration: the read was canceled.</exception>
        public IAsyncEnumerable<IcalContentLine> ReadLinesAsync(CancellationToken cancellationToken = default)
        {
            // Check now, so the exception comes from this call and not from the first MoveNextAsync.
            ThrowIfDisposed();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            return ReadLinesAsyncIterator(cancellationToken);
#else
            return new AsyncLineEnumerable(this, cancellationToken);
#endif
        }
#endif

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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async IAsyncEnumerable<IcalContentLine> ReadLinesAsyncIterator([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (true)
            {
                var line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null) yield break;

                yield return line;
            }
        }
#endif

        /// <summary>
        /// Drives the scanner synchronously until it has a whole line or the data ends.
        /// </summary>
        /// <param name="result">Gives the result from whether a line was read.  It is called before the read ends, while the line is still
        /// current.</param>
        private TResult Read<TResult>(Func<IcalContentLineReader, bool, TResult> result)
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                StartLine();
                while (true)
                {
                    var status = _scanner.Scan();
                    if (status != IcalContentLineScanner.ScanStatus.NeedMoreInput)
                    {
                        return result(this, CompleteLine(status));
                    }

                    int offset, count;
                    _scanner.PrepareRefill(out offset, out count);
                    var charactersRead = _reader.Read(_scanner.Buffer, offset, count);
                    ThrowIfDisposed();
                    _scanner.CompleteRefill(charactersRead);
                }
            }
            finally
            {
                EndRead();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Drives the scanner asynchronously until it has a whole line or the data ends.  Completes synchronously, with no state machine, when no
        /// refill is needed.
        /// </summary>
        /// <param name="result">Gives the result from whether a line was read.  It is called before the read ends, while the line is still
        /// current.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        private ValueTask<TResult> ReadAsync<TResult>(Func<IcalContentLineReader, bool, TResult> result, CancellationToken cancellationToken)
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            var isPending = false;
            try
            {
                StartLine();
                if (cancellationToken.IsCancellationRequested)
                {
                    _isCanceled = true;
                    return new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));
                }

                try
                {
                    var status = _scanner.Scan();
                    if (status != IcalContentLineScanner.ScanStatus.NeedMoreInput)
                    {
                        return new ValueTask<TResult>(result(this, CompleteLine(status)));
                    }
                }
                catch (IcalFileException exception)
                {
                    return new ValueTask<TResult>(Task.FromException<TResult>(exception));
                }

                isPending = true;
                return ReadWithRefillAsync(result, cancellationToken);
            }
            finally
            {
                if (!isPending)
                {
                    EndRead();
                }
            }
        }

        /// <summary>
        /// Continues <see cref="ReadAsync{TResult}"/> once the scanner needs more input, and ends the read.
        /// </summary>
        private async ValueTask<TResult> ReadWithRefillAsync<TResult>(Func<IcalContentLineReader, bool, TResult> result, CancellationToken cancellationToken)
        {
            try
            {
                IcalContentLineScanner.ScanStatus status;
                do
                {
                    int offset, count;
                    _scanner.PrepareRefill(out offset, out count);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    var charactersRead = await _reader.ReadAsync(new Memory<char>(_scanner.Buffer, offset, count), cancellationToken).ConfigureAwait(false);
#else
                    // This overload takes no cancellation token, so check before each read instead.
                    cancellationToken.ThrowIfCancellationRequested();
                    var charactersRead = await _reader.ReadAsync(_scanner.Buffer, offset, count).ConfigureAwait(false);
#endif
                    ThrowIfDisposed();
                    _scanner.CompleteRefill(charactersRead);
                    status = _scanner.Scan();
                } while (status == IcalContentLineScanner.ScanStatus.NeedMoreInput);

                return result(this, CompleteLine(status));
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                EndRead();
            }
        }
#endif

        /// <summary>
        /// Marks a read as in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another read is in progress.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        private void BeginRead()
        {
            if (Interlocked.CompareExchange(ref _isReadInProgress, 1, 0) != 0)
                throw new InvalidOperationException("Another read is in progress.  Only one read may be in progress at a time.");

            // Dispose may have run between the caller's check and this one.
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                EndRead();
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Marks the read as finished, and releases the buffers and stream if the reader was disposed while it was in progress.
        /// </summary>
        private void EndRead()
        {
            Interlocked.Exchange(ref _isReadInProgress, 0);
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                Release();
            }
        }

        /// <summary>
        /// Checks that the reader can continue, and moves on from the current line.
        /// </summary>
        /// <exception cref="InvalidOperationException">An earlier read found a line break or line length error, or was canceled.</exception>
        private void StartLine()
        {
            if (_scanner.IsFaulted)
                throw new InvalidOperationException("The reader cannot continue after a line break or line length error.");
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
                throw new InvalidOperationException("The reader cannot continue after a read was canceled.");
#endif

            // Any view of the previous line is now out of date.
            unchecked
            {
                _lineVersion++;
            }

            _hasCurrentLine = false;
        }

        /// <summary>
        /// Parses the line found by the scanner, if any, and makes it current.
        /// </summary>
        /// <returns><c>true</c> if a line was read; <c>false</c> if the end of the data has been reached.</returns>
        private bool CompleteLine(IcalContentLineScanner.ScanStatus status)
        {
            if (status != IcalContentLineScanner.ScanStatus.LineRead) return false;

            _scanner.ParseLine();
            _hasCurrentLine = true;
            return true;
        }

        /// <summary>
        /// Releases the buffers, and the stream or file the reader opened itself, the first time it is called.
        /// </summary>
        private void Release()
        {
            if (Interlocked.Exchange(ref _isReleased, 1) != 0) return;

            if (_isInternalReader)
            {
                _reader.Dispose();
            }

            _scanner.ReturnBuffers();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
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

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER) && NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// The lines of <see cref="ReadLinesAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncLineEnumerable : IAsyncEnumerable<IcalContentLine>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly IcalContentLineReader _reader;

            internal AsyncLineEnumerable(IcalContentLineReader reader, CancellationToken cancellationToken)
            {
                _reader = reader;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<IcalContentLine> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
            {
                return new Enumerator(_reader, _cancellationToken, cancellationToken);
            }

            private sealed class Enumerator : IAsyncEnumerator<IcalContentLine>
            {
                private readonly CancellationToken _cancellationToken;
                private bool _isFinished;
                private readonly CancellationTokenSource _linkedTokenSource;
                private readonly IcalContentLineReader _reader;

                internal Enumerator(IcalContentLineReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
                {
                    _reader = reader;

                    // Combine the tokens as an async iterator with [EnumeratorCancellation] does.
                    if (readerToken.CanBeCanceled && enumeratorToken.CanBeCanceled && readerToken != enumeratorToken)
                    {
                        _linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(readerToken, enumeratorToken);
                        _cancellationToken = _linkedTokenSource.Token;
                    }
                    else
                    {
                        _cancellationToken = readerToken.CanBeCanceled ? readerToken : enumeratorToken;
                    }
                }

                public IcalContentLine Current { get; private set; }

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (_isFinished) return false;

                    var line = await _reader.ReadLineAsync(_cancellationToken).ConfigureAwait(false);
                    Current = line;
                    _isFinished = line is null;
                    return !_isFinished;
                }

                public ValueTask DisposeAsync()
                {
                    _isFinished = true;
                    Current = null;
                    _linkedTokenSource?.Dispose();
                    return default(ValueTask);
                }
            }
        }
#endif
    }
}
