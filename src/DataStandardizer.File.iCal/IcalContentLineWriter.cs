using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a writer that provides fast, non-cached, forward-only output of the content lines (RFC 5545 §3.1) of iCalendar data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each line is formatted into a pooled buffer, folded at <see cref="IcalOptions.FoldLength"/> octets and ended with
    /// <see cref="IcalOptions.LineBreak"/>, then handed to the underlying <see cref="TextWriter"/> in a single write.  A line that cannot be
    /// written throws <see cref="IcalFileException"/> before anything is written, so writing can continue with the next line.  Line breaks never
    /// depend on <see cref="Environment.NewLine"/>.
    /// </para>
    /// <para>
    /// Parameter values are caret encoded (RFC 6868), and quoted when they hold a ':', ';' or ','.  A line read by
    /// <see cref="IcalContentLineReader"/> is written back with its parameters exactly as they were read, so only its folding can differ.
    /// Values are written as they are, with any TEXT escaping already in place.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another
    /// is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// For asynchronous file output, pass a <see cref="FileStream"/> opened with <c>FileOptions.Asynchronous</c> to a <see cref="Stream"/>
    /// constructor.
    /// </para>
    /// </remarks>
    public sealed class IcalContentLineWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Size of the buffers of the <see cref="StreamWriter"/> (in characters) and <see cref="FileStream"/> (in bytes) the writer creates
        /// itself.  A caller-supplied <see cref="TextWriter"/> keeps its own buffering.
        /// </summary>
        internal const int StreamBufferSize = 16 * 1024;

        /// <summary>
        /// Encoding used when none is specified: UTF-8 without a byte order mark, throwing on invalid characters.
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        private readonly IcalContentLineFormatter _formatter;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif

        // These are changed with Interlocked, so that a Dispose from another thread while an operation is in progress releases the writer once
        // only, after that operation has finished with it.
        private int _isDisposed;
        private int _isOperationInProgress;
        private int _isReleased;

        private readonly bool _isInternalWriter;
        private long _linesWritten;
        private readonly TextWriter _writer;

        /// <summary>
        /// Creates a writer of iCalendar data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support writing.</exception>
        public IcalContentLineWriter(Stream icalStream) : this(icalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of iCalendar data to a stream.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support writing.
        /// -or-
        /// <see cref="IcalOptions.FoldLength"/>, <see cref="IcalOptions.FoldWhitespace"/> or <see cref="IcalOptions.LineBreak"/> is not
        /// valid.</exception>
        public IcalContentLineWriter(Stream icalStream, IcalOptions options) : this(icalStream, options, StreamBufferSize)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of an iCalendar file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> is <c>null</c>.</exception>
        public IcalContentLineWriter(string icalFilePath) : this(icalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of an iCalendar file.  An existing file is overwritten.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.FoldLength"/>, <see cref="IcalOptions.FoldWhitespace"/> or
        /// <see cref="IcalOptions.LineBreak"/> is not valid.</exception>
        public IcalContentLineWriter(string icalFilePath, IcalOptions options) : this(CreateFileWriter(icalFilePath, options), true, options)
        {
        }
#endif

        /// <summary>
        /// Creates a writer of iCalendar data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public IcalContentLineWriter(TextWriter writer) : this(writer, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of iCalendar data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the text writer does its own
        /// encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="IcalOptions.FoldLength"/>, <see cref="IcalOptions.FoldWhitespace"/> or <see cref="IcalOptions.LineBreak"/> is not
        /// valid.</exception>
        public IcalContentLineWriter(TextWriter writer, IcalOptions options) : this(ValidateTextWriter(writer, options), false, options)
        {
        }

        /// <summary>
        /// Creates a writer with a specific size for the buffers of the <see cref="StreamWriter"/> it creates.  Allows tests to force the stream
        /// writer to write through at chosen points.
        /// </summary>
        internal IcalContentLineWriter(Stream icalStream, IcalOptions options, int streamBufferSize)
            : this(CreateStreamWriter(icalStream, options, streamBufferSize), true, options)
        {
        }

        private IcalContentLineWriter(TextWriter writer, bool isInternalWriter, IcalOptions options)
        {
            _writer = writer;
            _isInternalWriter = isInternalWriter;
            _formatter = new IcalContentLineFormatter(options.FoldLength, options.FoldWhitespace, options.LineBreak);
        }

        /// <summary>
        /// Gets the number of logical (unfolded) content lines written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long LinesWritten
        {
            get
            {
                ThrowIfDisposed();
                return _linesWritten;
            }
        }

        /// <summary>
        /// Flushes and closes the stream writer or file the writer created itself, and releases the writer's buffer.  A caller-supplied text
        /// writer is neither flushed nor closed, and a caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the writer
        /// is flushed and released once that operation has finished.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            if (Volatile.Read(ref _isOperationInProgress) == 0 && MarkReleased())
            {
                Release();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Flushes and closes, asynchronously, the stream writer or file the writer created itself, and releases the writer's buffer.  A
        /// caller-supplied text writer is neither flushed nor closed, and a caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the writer is flushed and released once that operation has finished.  The writer is
        /// disposed even if the flush throws.
        /// </remarks>
        /// <returns>A task that completes once the writer has been flushed and released.</returns>
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return default(ValueTask);

            if (Volatile.Read(ref _isOperationInProgress) == 0 && MarkReleased())
            {
                return ReleaseAsync();
            }

            return default(ValueTask);
        }
#endif

        /// <summary>
        /// Flushes the underlying text writer, whether the writer created it or it was supplied by the caller.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        public void Flush()
        {
            ThrowIfDisposed();
            BeginOperation();
            try
            {
                _writer.Flush();
                ThrowIfDisposed();
            }
            finally
            {
                if (EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Writes a content line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is <c>null</c>.</exception>
        /// <exception cref="IcalFileException">The line cannot be written: a name is not valid, or a parameter value or the value holds a
        /// character that is not allowed.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        public void WriteLine(IcalContentLine line)
        {
            ThrowIfDisposed();
            if (line is null)
                throw new ArgumentNullException(nameof(line));

            BeginOperation();
            try
            {
                WriteLineCore(line);
                ThrowIfDisposed();
            }
            finally
            {
                if (EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Writes content lines.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="lines"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="lines">The lines.</param>
        /// <param name="progress">Receives the progress after each line has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the line it was reporting has been written.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="lines"/> contains <c>null</c>.  The lines before it have been written.</exception>
        /// <exception cref="IcalFileException">A line cannot be written.  The lines before it have been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteLines(IEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress>? progress = null)
#else
        public void WriteLines(IEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress> progress = null)
#endif
        {
            ThrowIfDisposed();
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            BeginOperation();
            try
            {
                foreach (var line in lines)
                {
                    ThrowIfDisposed();
                    WriteLineCore(ValidateLine(line));
                    progress?.Report(new IcalWriteProgress(_linesWritten));
                }

                ThrowIfDisposed();
            }
            finally
            {
                if (EndOperation())
                {
                    Release();
                }
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Flushes the underlying text writer asynchronously, whether the writer created it or it was supplied by the caller.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the flush is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the text writer has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the flush is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginOperation();
            return FlushCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Writes a content line asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the underlying text writer takes the line without waiting, as a <see cref="StreamWriter"/> does
        /// while the line fits its buffer.
        /// </remarks>
        /// <param name="line">The line.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the line has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="IcalFileException">By the task: the line cannot be written: a name is not valid, or a parameter value or the value
        /// holds a character that is not allowed.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteLineAsync(IcalContentLine line, CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the line or the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            if (line is null)
                throw new ArgumentNullException(nameof(line));

            BeginOperation();
            var isPending = false;
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _isCanceled = true;
                    return new ValueTask(Task.FromCanceled(cancellationToken));
                }

                ValueTask write;
                try
                {
                    write = WriteLineCoreAsync(line, cancellationToken);
                }
                catch (IcalFileException exception)
                {
                    return new ValueTask(Task.FromException(exception));
                }

                if (write.IsCompletedSuccessfully)
                {
                    return default(ValueTask);
                }

                isPending = true;
                return CompleteOperationAsync(write);
            }
            finally
            {
                if (!isPending && EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Writes content lines asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="lines"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="lines">The lines.</param>
        /// <param name="progress">Receives the progress after each line has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the line it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every line has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="lines"/> contains <c>null</c>.  The lines before it have been
        /// written.</exception>
        /// <exception cref="IcalFileException">By the task: a line cannot be written.  The lines before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteLinesAsync(IEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress>? progress = null,
#else
        public Task WriteLinesAsync(IEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            BeginOperation();
            return WriteLinesCoreAsync(lines, progress, cancellationToken);
        }

        /// <summary>
        /// Writes content lines asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="lines"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="lines">The lines.  <paramref name="cancellationToken"/> is passed to their enumerator.</param>
        /// <param name="progress">Receives the progress after each line has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the line it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every line has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="lines"/> contains <c>null</c>.  The lines before it have been
        /// written.</exception>
        /// <exception cref="IcalFileException">By the task: a line cannot be written.  The lines before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteLinesAsync(IAsyncEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress>? progress = null,
#else
        public Task WriteLinesAsync(IAsyncEnumerable<IcalContentLine> lines, IProgress<IcalWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            BeginOperation();
            return WriteAsyncLinesCoreAsync(lines, progress, cancellationToken);
        }
#endif

        /// <summary>
        /// Formats a line and hands it to the text writer in one write.
        /// </summary>
        private void WriteLineCore(IcalContentLine line)
        {
            var length = _formatter.Format(line, _linesWritten + 1);
            _writer.Write(_formatter.Buffer, 0, length);
            _linesWritten++;
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Formats a line and hands it to the text writer in one write, asynchronously.  Completes synchronously, with no state machine, when the
        /// text writer takes the line without waiting.
        /// </summary>
        /// <exception cref="IcalFileException">The line cannot be written.  Thrown by this call, not the task.</exception>
        private ValueTask WriteLineCoreAsync(IcalContentLine line, CancellationToken cancellationToken)
        {
            var length = _formatter.Format(line, _linesWritten + 1);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            var write = _writer.WriteAsync(new ReadOnlyMemory<char>(_formatter.Buffer, 0, length), cancellationToken);
#else
            // This overload takes no cancellation token, so check before each write instead.
            cancellationToken.ThrowIfCancellationRequested();
            var write = _writer.WriteAsync(_formatter.Buffer, 0, length);
#endif
            if (write.Status == TaskStatus.RanToCompletion)
            {
                _linesWritten++;
                return default(ValueTask);
            }

            return CompleteWriteAsync(write);
        }

        /// <summary>
        /// Continues <see cref="WriteLineCoreAsync"/> once the text writer has to wait.
        /// </summary>
        private async ValueTask CompleteWriteAsync(Task write)
        {
            await write.ConfigureAwait(false);
            _linesWritten++;
        }

        /// <summary>
        /// Continues <see cref="WriteLineAsync"/> once the write has to wait, and ends the operation.
        /// </summary>
        private async ValueTask CompleteOperationAsync(ValueTask write)
        {
            try
            {
                await write.ConfigureAwait(false);
                ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task FlushCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
#if NET8_0_OR_GREATER
                await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
#else
                // This overload takes no cancellation token, so check before the flush instead.
                await _writer.FlushAsync().ConfigureAwait(false);
#endif
                ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task WriteLinesCoreAsync(IEnumerable<IcalContentLine> lines,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<IcalWriteProgress>? progress,
#else
            IProgress<IcalWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var line in lines)
                {
                    ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await WriteLineCoreAsync(ValidateLine(line), cancellationToken).ConfigureAwait(false);
                    progress?.Report(new IcalWriteProgress(_linesWritten));
                }

                ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
            }
        }

        private async Task WriteAsyncLinesCoreAsync(IAsyncEnumerable<IcalContentLine> lines,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<IcalWriteProgress>? progress,
#else
            IProgress<IcalWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            try
            {
                var enumerator = lines.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (true)
                    {
                        ThrowIfDisposed();
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) break;

                        ThrowIfDisposed();
                        cancellationToken.ThrowIfCancellationRequested();
                        await WriteLineCoreAsync(ValidateLine(enumerator.Current), cancellationToken).ConfigureAwait(false);
                        progress?.Report(new IcalWriteProgress(_linesWritten));
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }

                ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Flushes and closes the text writer the writer created itself, asynchronously, and returns the buffer.
        /// </summary>
        private async ValueTask ReleaseAsync()
        {
            try
            {
                if (_isInternalWriter)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    await _writer.DisposeAsync().ConfigureAwait(false);
#else
                    try
                    {
                        await _writer.FlushAsync().ConfigureAwait(false);
                    }
                    finally
                    {
                        _writer.Dispose();
                    }
#endif
                }
            }
            finally
            {
                _formatter.ReturnBuffer();
            }
        }
#endif

        /// <summary>
        /// Marks an operation as in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another operation is in progress, or an earlier one was canceled.</exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        private void BeginOperation()
        {
            if (Interlocked.CompareExchange(ref _isOperationInProgress, 1, 0) != 0)
                throw new InvalidOperationException("Another operation is in progress.  Only one operation may be in progress at a time.");

            // Dispose may have run between the caller's check and this one.
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                if (EndOperation())
                {
                    Release();
                }

                throw new ObjectDisposedException(GetType().Name);
            }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
            {
                if (EndOperation())
                {
                    Release();
                }

                throw new InvalidOperationException("The writer cannot continue after an operation was canceled.");
            }
#endif
        }

        /// <summary>
        /// Marks the operation as finished.
        /// </summary>
        /// <returns><c>true</c> if the writer was disposed while the operation was in progress, and the caller must now release it.</returns>
        private bool EndOperation()
        {
            Interlocked.Exchange(ref _isOperationInProgress, 0);
            return Volatile.Read(ref _isDisposed) != 0 && MarkReleased();
        }

        /// <summary>
        /// Marks the writer as released.
        /// </summary>
        /// <returns><c>true</c> the first time it is called, when the caller must release the writer.</returns>
        private bool MarkReleased()
        {
            return Interlocked.Exchange(ref _isReleased, 1) == 0;
        }

        /// <summary>
        /// Flushes and closes the text writer the writer created itself, and returns the buffer.
        /// </summary>
        private void Release()
        {
            try
            {
                if (_isInternalWriter)
                {
                    _writer.Dispose();
                }
            }
            finally
            {
                _formatter.ReturnBuffer();
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static IcalContentLine ValidateLine(IcalContentLine? line)
#else
        private static IcalContentLine ValidateLine(IcalContentLine line)
#endif
        {
            if (line is null)
                throw new ArgumentException("The lines cannot contain null.", "lines");

            return line;
        }

        private static StreamWriter CreateStreamWriter(Stream stream, IcalOptions options, int bufferSize)
        {
            if (stream is null)
                throw new ArgumentNullException("icalStream");
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "icalStream");
            ValidateOptions(options);

            return new StreamWriter(stream, options.Encoding ?? DefaultEncoding, bufferSize, true);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static StreamWriter CreateFileWriter(string path, IcalOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("icalFilePath");
            ValidateOptions(options);

            // Open the file stream explicitly so both buffering layers are sized together.
            var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, StreamBufferSize);
            try
            {
                return new StreamWriter(fileStream, options.Encoding ?? DefaultEncoding, StreamBufferSize, false);
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
        }
#endif

        private static TextWriter ValidateTextWriter(TextWriter writer, IcalOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            ValidateOptions(options);
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(options));

            return writer;
        }

        private static void ValidateOptions(IcalOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.FoldLength < 4)
                throw new ArgumentException("The fold length must be at least 4, the length of the longest UTF-8 sequence.", nameof(options));
            if (options.FoldWhitespace != ' ' && options.FoldWhitespace != '\t')
                throw new ArgumentException("The fold white space must be a space or a horizontal tab.", nameof(options));
            if (options.LineBreak != "\r\n" && options.LineBreak != "\n")
                throw new ArgumentException("The line break must be CRLF or, as an opt-in, a bare line feed.", nameof(options));
        }
    }
}
