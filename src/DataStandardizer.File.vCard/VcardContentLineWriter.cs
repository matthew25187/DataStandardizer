using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using DataStandardizer.Communication.Mime;
using DataStandardizer.File.ContentLine;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a writer that provides fast, non-cached, forward-only output of the content lines (RFC 6350 §3.3) of vCard data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each line is formatted into a pooled buffer, with its group as a prefix, folded at <see cref="VcardOptions.FoldLength"/> octets (RFC 6350
    /// §3.2) and ended with <see cref="VcardOptions.LineBreak"/>, then handed to the underlying <see cref="TextWriter"/> in a single write.  A line
    /// that cannot be written throws <see cref="VcardFileException"/> before anything is written, so writing can continue with the next line.  Line
    /// breaks never depend on <see cref="Environment.NewLine"/>.  The meaning of properties and values is not checked, and neither is the structure of
    /// a card.
    /// </para>
    /// <para>
    /// Parameter values are quoted when they hold a ':', ';' or ','.  Values are written as they are, with any TEXT escaping already in place.
    /// </para>
    /// <para>
    /// The writer tracks each card's version from the lines it writes, as <see cref="VcardContentLineReader"/> does: <c>BEGIN:VCARD</c> forgets the
    /// version, a <c>VERSION</c> line sets it, and the line after <c>END:VCARD</c> forgets it again.  A line that could not be written doesn't count.
    /// </para>
    /// <list type="bullet">
    /// <item>On the lines of a vCard 4.0 card, from its <c>VERSION:4.0</c> line up to and including its <c>END:VCARD</c> line, parameter values are caret
    /// encoded (RFC 6868).</item>
    /// <item>On any other line, a caret is an ordinary character, since RFC 6868 updates neither RFC 2426 nor vCard 2.1.  A parameter value with a double
    /// quote or a line break, which only caret encoding can represent, can't be written, unless <see cref="VcardOptions.AllowLossyVersionConversion"/>
    /// is set.</item>
    /// <item>On the lines of a vCard 2.1 card, from its <c>VERSION:2.1</c> line up to and including its <c>END:VCARD</c> line, lines are never folded,
    /// because vCard 2.1 and later versions unfold them differently, so no fold is read the same way by every reader.  A value with
    /// <c>ENCODING=QUOTED-PRINTABLE</c> is broken with soft line breaks so that no line reaches 76 characters (vCard 2.1 §2.1.3); a line whose group, name and parameters leave no room for that on its first line can't be written.  A value with <c>ENCODING=BASE64</c> is
    /// written on the lines after the property's own, indented, with the empty line that ends it (vCard 2.1 §2.9).  The value must already be in the encoding, or the line
    /// can't be written.  A <c>TYPE</c> whose one value is a <c>knowntype</c> of vCard 2.1 §2.9, such as <c>WORK</c>, is written as that value alone, unless
    /// <see cref="VcardOptions.WriteNamedTypesInVersion21"/> is set, and every other parameter with its name.</item>
    /// </list>
    /// <para>
    /// A line read by <see cref="VcardContentLineReader"/> with its default options is written back as the same line: reading it again gives the same
    /// group, name, parameter values and value.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another
    /// is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// A file-path constructor opens the file with <c>FileOptions.Asynchronous</c>, so the asynchronous methods do no synchronous I/O.  To
    /// write a file only synchronously, without the small cost that option adds on Windows, pass a <c>FileStream</c> opened without it to
    /// a <see cref="Stream"/> constructor.
    /// </para>
    /// </remarks>
    public sealed class VcardContentLineWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Size of the buffers of the <see cref="StreamWriter"/> (in characters) and <c>FileStream</c> (in bytes) the writer creates
        /// itself.  A caller-supplied <see cref="TextWriter"/> keeps its own buffering.
        /// </summary>
        internal const int StreamBufferSize = 16 * 1024;

        /// <summary>
        /// Encoding used when none is specified: UTF-8 without a byte order mark, throwing on invalid characters.
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        private readonly bool _allowLossyVersionConversion;
        private readonly bool _writesNamedTypesInVersion21;
        private static readonly Func<string, bool> IsAlwaysQuoted = VcardPropertyParameterReader.IsAlwaysQuoted;

        private readonly ContentLineFormatter _formatter;
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

        // Whether the writer opened the file it writes to, rather than writing to output the caller supplied.
        private readonly bool _ownsOutput;
        // The card of the last line written, and of the line being written, which replaces it once the line has been handed to the text writer.  A line
        // that can't be written, or whose write fails, therefore doesn't count.
        private readonly VcardLexicalState _lineState = new VcardLexicalState();
        private readonly VcardLexicalState _writtenState = new VcardLexicalState();

        private readonly TextWriter _writer;

        /// <summary>
        /// Gets the text writer that the lines are written to, so that tests can check how a constructor built it.
        /// </summary>
        internal TextWriter UnderlyingWriter => _writer;

        /// <summary>
        /// Creates a writer of vCard data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="vcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="vcardStream"/> does not support writing.</exception>
        public VcardContentLineWriter(Stream vcardStream) : this(vcardStream, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of vCard data to a stream.
        /// </summary>
        /// <param name="vcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="vcardStream"/> does not support writing.
        /// -or-
        /// <see cref="VcardOptions.FoldLength"/>, <see cref="VcardOptions.FoldWhitespace"/> or <see cref="VcardOptions.LineBreak"/> is not
        /// valid.</exception>
        public VcardContentLineWriter(Stream vcardStream, VcardOptions options) : this(vcardStream, options, StreamBufferSize)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of a vCard file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> is <c>null</c>.</exception>
        public VcardContentLineWriter(string vcardFilePath) : this(vcardFilePath, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of a vCard file.  An existing file is overwritten.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.FoldLength"/>, <see cref="VcardOptions.FoldWhitespace"/> or
        /// <see cref="VcardOptions.LineBreak"/> is not valid.</exception>
        public VcardContentLineWriter(string vcardFilePath, VcardOptions options) : this(CreateFileWriter(vcardFilePath, options), true, true, options)
        {
        }
#endif

        /// <summary>
        /// Creates a writer of vCard data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public VcardContentLineWriter(TextWriter writer) : this(writer, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of vCard data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c>, since the text writer does its own
        /// encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="VcardOptions.FoldLength"/>, <see cref="VcardOptions.FoldWhitespace"/> or <see cref="VcardOptions.LineBreak"/> is not
        /// valid.</exception>
        public VcardContentLineWriter(TextWriter writer, VcardOptions options) : this(ValidateTextWriter(writer, options), false, false, options)
        {
        }

        /// <summary>
        /// Creates a writer with a specific size for the buffers of the <see cref="StreamWriter"/> it creates.  Allows tests to force the stream
        /// writer to write through at chosen points.
        /// </summary>
        internal VcardContentLineWriter(Stream vcardStream, VcardOptions options, int streamBufferSize)
            : this(CreateStreamWriter(vcardStream, options, streamBufferSize), true, false, options)
        {
        }

        private VcardContentLineWriter(TextWriter writer, bool isInternalWriter, bool ownsOutput, VcardOptions options)
        {
            _writer = writer;
            _isInternalWriter = isInternalWriter;
            _ownsOutput = ownsOutput;
            _allowLossyVersionConversion = options.AllowLossyVersionConversion;
            _writesNamedTypesInVersion21 = options.WriteNamedTypesInVersion21;
            _formatter = new ContentLineFormatter(options.FoldLength, options.FoldWhitespace, options.LineBreak, new VcardContentLineErrorFactory(_lineState));
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
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, unless it fails
        /// for another reason, and the writer is flushed and released once that operation has finished.
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
        /// <see cref="ObjectDisposedException"/>, unless it fails for another reason, and the writer is flushed and released once that operation
        /// has finished.  The writer is disposed even if the flush throws, and the task then faults with the flush's exception, even an
        /// <see cref="OperationCanceledException"/>.
        /// </remarks>
        /// <returns>A task that completes once the writer has been flushed and released, or immediately if an operation is in progress.</returns>
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return default(ValueTask);

            if (Volatile.Read(ref _isOperationInProgress) == 0 && MarkReleased())
            {
                return ReleaseThenCompleteAsync(Task.CompletedTask);
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
        /// <exception cref="VcardFileException">The line cannot be written: the group or a name is not valid, or a parameter value or the value
        /// holds a character that is not allowed, or that only caret encoding can represent outside a vCard 4.0 card.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        public void WriteLine(VcardContentLine line)
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
        /// <exception cref="VcardFileException">A line cannot be written.  The lines before it have been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteLines(IEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress>? progress = null)
#else
        public void WriteLines(IEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress> progress = null)
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
                    progress?.Report(new VcardWriteProgress(_linesWritten));
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
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The flush is canceled only if this token is
        /// canceled, and the writer then cannot continue.</param>
        /// <returns>A task that completes once the text writer has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the flush is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.  The task is canceled,
        /// whatever token the exception carries.  If <paramref name="cancellationToken"/> isn't canceled, an <see cref="OperationCanceledException"/>
        /// from the underlying text writer faults the task, as an I/O error does, and the writer can continue.</exception>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginOperation();
            return FlushCoreAsync(cancellationToken).Unwrap();
        }

        /// <summary>
        /// Writes a content line asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the underlying text writer takes the line without waiting, as a <see cref="StreamWriter"/> does
        /// while the line fits its buffer, unless the writer is disposed during the write; the task then completes once the writer has been
        /// flushed and released.
        /// </remarks>
        /// <param name="line">The line.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The write is canceled only if this token is
        /// canceled, and the writer then cannot continue.</param>
        /// <returns>A task that completes once the line has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="line"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="VcardFileException">By the task: the line cannot be written: the group or a name is not valid, or a parameter value or
        /// the value holds a character that is not allowed, or that only caret encoding can represent outside a vCard 4.0 card.  Nothing has been
        /// written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.  The task is canceled,
        /// whatever token the exception carries.  If <paramref name="cancellationToken"/> isn't canceled, an <see cref="OperationCanceledException"/>
        /// from the underlying text writer faults the task, as an I/O error does, and the writer can continue.</exception>
        public ValueTask WriteLineAsync(VcardContentLine line, CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the line or the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            if (line is null)
                throw new ArgumentNullException(nameof(line));

            BeginOperation();
            ValueTask write;
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _isCanceled = true;
                    write = new ValueTask(Task.FromCanceled(cancellationToken));
                }
                else
                {
                    write = WriteLineCoreAsync(line, cancellationToken);
                    if (write.IsCompleted && !write.IsCompletedSuccessfully)
                    {
                        // The text writer's task failed without waiting.  Take its exception, so that it is decided as one thrown by the call.
                        write.GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception exception)
            {
                // A line that can't be written, which writing continues past, costs nothing more than this failed task.
                write = new ValueTask(FailOperation(exception, cancellationToken));
            }

            if (!write.IsCompleted)
            {
                return CompleteOperationAsync(write, cancellationToken);
            }

            if (!write.IsCompletedSuccessfully)
            {
                return EndOperation() ? ReleaseThenCompleteAsync(write.AsTask()) : write;
            }

            // Read before the operation ends, as WriteLine and CompleteOperationAsync check, so that a Dispose that began during the write is
            // reported whichever call releases the writer.
            var isDisposed = Volatile.Read(ref _isDisposed) != 0;
            var mustRelease = EndOperation();
            if (isDisposed)
            {
                var disposed = Task.FromException(new ObjectDisposedException(GetType().Name));
                return mustRelease ? ReleaseThenCompleteAsync(disposed) : new ValueTask(disposed);
            }

            return mustRelease ? ReleaseThenCompleteAsync(Task.CompletedTask) : default(ValueTask);
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
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The write is canceled only if this token is
        /// canceled, and the writer then cannot continue.</param>
        /// <returns>A task that completes once every line has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="lines"/> contains <c>null</c>.  The lines before it have been
        /// written.</exception>
        /// <exception cref="VcardFileException">By the task: a line cannot be written.  The lines before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.  The task is canceled,
        /// whatever token the exception carries.  If <paramref name="cancellationToken"/> isn't canceled, an <see cref="OperationCanceledException"/>
        /// from the underlying text writer faults the task, as an I/O error does, and the writer can continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteLinesAsync(IEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress>? progress = null,
#else
        public Task WriteLinesAsync(IEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            BeginOperation();
            return WriteLinesCoreAsync(lines, progress, cancellationToken).Unwrap();
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
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The write is canceled only if this token is
        /// canceled, and the writer then cannot continue.</param>
        /// <returns>A task that completes once every line has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="lines"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="lines"/> contains <c>null</c>.  The lines before it have been
        /// written.</exception>
        /// <exception cref="VcardFileException">By the task: a line cannot be written.  The lines before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.  The task is canceled,
        /// whatever token the exception carries.  If <paramref name="cancellationToken"/> isn't canceled, an <see cref="OperationCanceledException"/>
        /// from the underlying text writer faults the task, as an I/O error does, and the writer can continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteLinesAsync(IAsyncEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress>? progress = null,
#else
        public Task WriteLinesAsync(IAsyncEnumerable<VcardContentLine> lines, IProgress<VcardWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            BeginOperation();
            return WriteAsyncLinesCoreAsync(lines, progress, cancellationToken).Unwrap();
        }
#endif

        /// <summary>
        /// Formats a line and hands it to the text writer in one write.
        /// </summary>
        private void WriteLineCore(VcardContentLine line)
        {
            var length = FormatLine(line);
            _writer.Write(_formatter.Buffer, 0, length);
            CompleteLine();
        }

        /// <summary>
        /// Formats a line as its card's version says.
        /// </summary>
        private int FormatLine(VcardContentLine line)
        {
            _lineState.CopyFrom(_writtenState);
            _lineState.MoveToNextLine();
            _lineState.ObserveLine(line);

            ContentLineCaretMode caretMode;
            if (_lineState.IsVersion40)
            {
                caretMode = ContentLineCaretMode.Encode;
            }
            else
            {
                caretMode = _allowLossyVersionConversion ? ContentLineCaretMode.Omit : ContentLineCaretMode.Reject;
            }

            var foldMode = _lineState.IsVersion21 ? GetVersion21FoldMode(line) : ContentLineFoldMode.Fold;
            // ref. RFC 6350 §5: the case of a value that isn't quoted is not significant.  The rule is of vCard 4.0, and vCard 2.1 has no quoted strings.
            var data = new VcardContentLineData(line, _lineState.IsVersion21 && !_writesNamedTypesInVersion21);
            return _formatter.Format(data, _linesWritten + 1, caretMode, foldMode, _lineState.IsVersion40 ? IsAlwaysQuoted : null);
        }

        /// <summary>
        /// Gets how a line of a vCard 2.1 card is broken, which depends on its encoding, and checks that its value is in that encoding.
        /// </summary>
        /// <exception cref="VcardFileException">The value isn't in the encoding its <c>ENCODING</c> parameter says.</exception>
        private ContentLineFoldMode GetVersion21FoldMode(VcardContentLine line)
        {
            var encoding = line.GetParameter(VcardLegacyTokens.EncodingName);
            if (encoding is null) return ContentLineFoldMode.None;

            var encodingName = encoding.ValueArray[0];
            var value = line.Value.AsSpan();
            if (VcardLegacyTokens.EqualsIgnoreCase(encodingName.AsSpan(), VcardLegacyTokens.QuotedPrintable))
            {
                if (MimeQuotedPrintable.TryDecode(value, MimeQuotedPrintableMode.Body, MimeQuotedPrintableDecoderOptions.None, out _)) return ContentLineFoldMode.QuotedPrintableSoftBreak;
            }
            else if (VcardLegacyTokens.EqualsIgnoreCase(encodingName.AsSpan(), VcardLegacyTokens.Base64))
            {
                if (VcardLineContinuation.IsBase64Text(value) && MimeBase64.TryDecode(value, MimeBase64Mode.Mime, MimeBase64DecoderOptions.None, out _)) return ContentLineFoldMode.Base64Lines;
            }
            else
            {
                return ContentLineFoldMode.None;
            }

            throw VcardContentLineErrorFactory.BuildEncodedValueToWriteException(encodingName, _linesWritten + 1, line.Name, line.Group, _lineState.CardIndexOfLine);
        }

        /// <summary>
        /// Counts a line that has been handed to the text writer.
        /// </summary>
        private void CompleteLine()
        {
            _linesWritten++;
            _writtenState.CopyFrom(_lineState);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Formats a line and hands it to the text writer in one write, asynchronously.  Completes synchronously, with no state machine, when the
        /// text writer takes the line without waiting.
        /// </summary>
        /// <exception cref="VcardFileException">The line cannot be written.  Thrown by this call, not the task.</exception>
        /// <exception cref="OperationCanceledException">Thrown by this call, or by the task.</exception>
        /// <remarks>
        /// An exception the text writer throws from its call, rather than its task, is thrown by this call too.
        /// </remarks>
        private ValueTask WriteLineCoreAsync(VcardContentLine line, CancellationToken cancellationToken)
        {
            var length = FormatLine(line);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            var write = _writer.WriteAsync(new ReadOnlyMemory<char>(_formatter.Buffer, 0, length), cancellationToken);
#else
            // This overload takes no cancellation token, so check before each write instead.
            cancellationToken.ThrowIfCancellationRequested();
            var write = _writer.WriteAsync(_formatter.Buffer, 0, length);
#endif
            if (write.Status == TaskStatus.RanToCompletion)
            {
                CompleteLine();
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
            CompleteLine();
        }

        /// <summary>
        /// Continues <see cref="WriteLineAsync"/> once the write has to wait, and ends the operation.
        /// </summary>
        private ValueTask CompleteOperationAsync(ValueTask write, CancellationToken cancellationToken)
        {
            return new ValueTask(CompleteOperationCoreAsync(write, cancellationToken).Unwrap());
        }

        private async Task<Task> CompleteOperationCoreAsync(ValueTask write, CancellationToken cancellationToken)
        {
            Task outcome;
            try
            {
                await write.ConfigureAwait(false);
                ThrowIfDisposed();
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = FailOperation(exception, cancellationToken);
            }

            return await EndOperationAsync(outcome).ConfigureAwait(false);
        }

        /// <summary>
        /// Decides how an asynchronous operation that threw <paramref name="exception"/> completes, and returns a task that completes that way.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An <see cref="OperationCanceledException"/> is a cancellation only if the caller's <paramref name="cancellationToken"/> is canceled,
        /// whatever token the exception carries.  The task is then canceled, and the writer cannot continue.  Any other exception, including an
        /// <see cref="OperationCanceledException"/> while the caller's token isn't canceled, faults the task, and the writer can continue.
        /// </para>
        /// <para>
        /// An async method can't be used to report the outcome, since it cancels its task for any <see cref="OperationCanceledException"/>.  The
        /// operations return the task this method returns, unwrapped from their own where they have to wait.
        /// </para>
        /// </remarks>
        private Task FailOperation(Exception exception, CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                _isCanceled = true;
                return Task.FromCanceled(cancellationToken);
            }

            return Task.FromException(exception);
        }

        /// <summary>
        /// Ends an asynchronous operation that completes as <paramref name="outcome"/>.  If the writer was disposed during the operation, releases
        /// it first.
        /// </summary>
        /// <returns><paramref name="outcome"/>, or a task faulted with the release's exception if the release fails.</returns>
        private ValueTask<Task> EndOperationAsync(Task outcome)
        {
            return EndOperation() ? ReleaseThenReportAsync(outcome) : new ValueTask<Task>(outcome);
        }

        /// <summary>
        /// Releases the writer, then completes as <paramref name="outcome"/>, or faults with the release's exception if the release fails.
        /// </summary>
        private ValueTask ReleaseThenCompleteAsync(Task outcome)
        {
            var report = ReleaseThenReportAsync(outcome);
            return report.IsCompletedSuccessfully ? new ValueTask(report.Result) : new ValueTask(report.AsTask().Unwrap());
        }

        /// <summary>
        /// Releases the writer, then returns <paramref name="outcome"/>, or a task faulted with the release's exception if the release fails.
        /// </summary>
        /// <remarks>
        /// The release takes no cancellation token, so an <see cref="OperationCanceledException"/> from it faults the task, as any other
        /// exception does.
        /// </remarks>
        private async ValueTask<Task> ReleaseThenReportAsync(Task outcome)
        {
            try
            {
                await ReleaseAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // The release failure is reported; observe the operation's, so that it isn't reported as unobserved.
                _ = outcome.Exception;
                return Task.FromException(exception);
            }

            return outcome;
        }

        private async Task<Task> FlushCoreAsync(CancellationToken cancellationToken)
        {
            Task outcome;
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
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = FailOperation(exception, cancellationToken);
            }

            return await EndOperationAsync(outcome).ConfigureAwait(false);
        }

        private async Task<Task> WriteLinesCoreAsync(IEnumerable<VcardContentLine> lines,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<VcardWriteProgress>? progress,
#else
            IProgress<VcardWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            Task outcome;
            try
            {
                // Check here as well, so that a canceled token cancels the call when there are no lines, as it does for an asynchronous sequence.
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var line in lines)
                {
                    ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await WriteLineCoreAsync(ValidateLine(line), cancellationToken).ConfigureAwait(false);
                    progress?.Report(new VcardWriteProgress(_linesWritten));
                }

                ThrowIfDisposed();
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = FailOperation(exception, cancellationToken);
            }

            return await EndOperationAsync(outcome).ConfigureAwait(false);
        }

        private async Task<Task> WriteAsyncLinesCoreAsync(IAsyncEnumerable<VcardContentLine> lines,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<VcardWriteProgress>? progress,
#else
            IProgress<VcardWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            Task outcome;
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
                        progress?.Report(new VcardWriteProgress(_linesWritten));
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }

                ThrowIfDisposed();
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = FailOperation(exception, cancellationToken);
            }

            return await EndOperationAsync(outcome).ConfigureAwait(false);
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
                        // TextWriter has no DisposeAsync here, and Dispose would flush the caller's stream synchronously, even with nothing left
                        // to write.  A stream writer over the caller's stream leaves the stream open, so disposing it would do nothing else.
                        if (_ownsOutput)
                        {
                            _writer.Dispose();
                        }
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
        private static VcardContentLine ValidateLine(VcardContentLine? line)
#else
        private static VcardContentLine ValidateLine(VcardContentLine line)
#endif
        {
            if (line is null)
                throw new ArgumentException("The lines cannot contain null.", "lines");

            return line;
        }

        private static StreamWriter CreateStreamWriter(Stream stream, VcardOptions options, int bufferSize)
        {
            if (stream is null)
                throw new ArgumentNullException("vcardStream");
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "vcardStream");
            ValidateOptions(options);

            return new StreamWriter(stream, options.Encoding ?? DefaultEncoding, bufferSize, true);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static StreamWriter CreateFileWriter(string path, VcardOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("vcardFilePath");
            ValidateOptions(options);

            // Open the file stream explicitly so both buffering layers are sized together.
            var fileStream = VcardFileStreams.OpenWrite(path, StreamBufferSize);
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

        private static TextWriter ValidateTextWriter(TextWriter writer, VcardOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            ValidateOptions(options);
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(options));

            return writer;
        }

        private static void ValidateOptions(VcardOptions options)
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
