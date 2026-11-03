using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a writer of whole Internet messages from <see cref="ImfMessage"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>All at once.</b>  The writer checks a whole message before it writes any of it, so a message it refuses leaves nothing in the output, and writing can continue with another.
    /// </para>
    /// <para>
    /// <b>Conformance.</b>  The header is checked as <see cref="ImfMessage.Validate"/> describes, under the options of the writer.  A missing required field, a second field of a kind
    /// that may appear only once, and a field body that breaks its grammar each cause an <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowMissingRequiredFields"/>,
    /// <see cref="ImfOptions.AllowCardinalityViolations"/> or <see cref="ImfOptions.AllowInvalidValues"/> is set.  With them set, the problem is recorded in
    /// <see cref="Diagnostics"/> instead.
    /// </para>
    /// <para>
    /// <b>Unchanged fields.</b>  A field that was read is written byte for byte, whichever options it was read under, so that signatures over it stay valid.  That includes any
    /// obsolete syntax it holds.  So does the line break of the empty line that ended the header, and its absence if the data ended first and there is still no body.
    /// </para>
    /// <para>
    /// <b>Other fields.</b>  A field that was set is formatted in the modern form, as <see cref="ImfHeaderWriter"/> formats it, with CRLF line breaks, and with the
    /// <see cref="ImfOptions.HeaderEncoding"/> of the writer.
    /// </para>
    /// <para>
    /// <b>Body.</b>  <see cref="ImfMessage.RawBody"/> is written as it is.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another is in
    /// progress throws <see cref="InvalidOperationException"/>.  After a write fails once output has started, a message is incomplete, so the writer cannot continue, and the
    /// same goes after any write is canceled, whether or not output had started.
    /// </para>
    /// <para>
    /// A file-path constructor opens the file with <c>FileOptions.Asynchronous</c>, so the asynchronous methods do no synchronous I/O.  To write a file
    /// only synchronously, without the small cost that option adds on Windows, pass a <c>FileStream</c> opened without it to a <see cref="Stream"/>
    /// constructor.
    /// </para>
    /// </remarks>
    public sealed class ImfWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int BlockSize = 16 * 1024;
        private const int InitialBufferSize = 1024;

        private static readonly byte[] CrLf = { 0x0D, 0x0A };
        private static readonly byte[] Lf = { 0x0A };

        private readonly List<ImfDiagnostic> _diagnostics = new List<ImfDiagnostic>();
        private readonly byte[] _lineBreak;
        private readonly ImfOptions _options;
        private readonly ImfOutput _output;
        private readonly ReadOnlyCollection<ImfDiagnostic> _readOnlyDiagnostics;

        private long _entitiesWritten;
        private long _fieldsWritten;

        /// <summary>
        /// Creates a writer of messages to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.</exception>
        public ImfWriter(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of messages to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.
        /// -or-
        /// <paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public ImfWriter(Stream messageStream, ImfOptions options) : this(messageStream, options, false)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of messages to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public ImfWriter(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of messages to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that
        /// is needed and can't be resolved.</exception>
        public ImfWriter(string messageFilePath, ImfOptions options)
            : this(CheckOptions(options), OpenFile(messageFilePath), true, false)
        {
        }
#endif

        /// <summary>
        /// Creates a writer with a line break setting.  Allows the writers of container formats to write the LF line breaks of their messages.
        /// </summary>
        internal ImfWriter(Stream messageStream, ImfOptions options, bool lineFeedIsLineBreak)
            : this(CheckOptions(options), ImfOutput.ValidateStream(messageStream), false, lineFeedIsLineBreak)
        {
        }

        // The options come first, so that bad options are found before a file is opened.
        private ImfWriter(ImfOptions options, Stream stream, bool ownsStream, bool lineFeedIsLineBreak)
        {
            _options = options;
            _output = new ImfOutput(stream, ownsStream, GetType().Name);
            _lineBreak = lineFeedIsLineBreak ? Lf : CrLf;
            _readOnlyDiagnostics = new ReadOnlyCollection<ImfDiagnostic>(_diagnostics);
        }

        /// <summary>
        /// Gets the number of bytes written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long BytePosition
        {
            get
            {
                _output.ThrowIfDisposed();
                return _output.BytePosition;
            }
        }

        /// <summary>
        /// Gets the problems found so far that did not stop writing, as the options allow.  The list is read-only, and grows as the writer finds more.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public IReadOnlyList<ImfDiagnostic> Diagnostics
        {
            get
            {
                _output.ThrowIfDisposed();
                return _readOnlyDiagnostics;
            }
        }

        /// <summary>
        /// Gets the number of messages written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long EntitiesWritten
        {
            get
            {
                _output.ThrowIfDisposed();
                return _entitiesWritten;
            }
        }

        /// <summary>
        /// Gets the number of header fields written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long FieldsWritten
        {
            get
            {
                _output.ThrowIfDisposed();
                return _fieldsWritten;
            }
        }

        /// <summary>
        /// Closes the file the writer opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the file is closed once
        /// it has finished with it.
        /// </remarks>
        public void Dispose()
        {
            if (_output.MarkDisposed()) _output.Release();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Closes the file the writer opened itself, asynchronously.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the file is closed once it has finished with it.
        /// </remarks>
        /// <returns>A task that completes once any operation in progress has finished, and the file has been closed.</returns>
        public ValueTask DisposeAsync()
        {
            return _output.MarkDisposed() ? _output.ReleaseAsync() : _output.WaitForReleaseAsync();
        }
#endif

        /// <summary>
        /// Flushes the stream, whether the writer opened it or it was supplied by the caller.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void Flush()
        {
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            try
            {
                _output.Flush();
                _output.ThrowIfDisposed();
            }
            finally
            {
                if (_output.EndOperation()) _output.Release();
            }
        }

        /// <summary>
        /// Writes a whole message.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so <paramref name="progress"/> may not call the writer.  Any number of messages can be written, one after another,
        /// with nothing between them.
        /// </remarks>
        /// <param name="message">The message.</param>
        /// <param name="progress">Receives the progress after each field and each block of the body has been handed to the stream, synchronously on the writing thread; or
        /// <c>null</c>.  An exception from <see cref="IProgress{T}.Report"/> ends the call, leaving the message incomplete.  <see cref="Progress{T}"/> runs its handler later, so writing
        /// doesn't wait for the handler, and an exception from the handler doesn't end the call.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The message can't be written.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteMessage(ImfMessage message, IProgress<ImfWriteProgress>? progress = null)
#else
        public void WriteMessage(ImfMessage message, IProgress<ImfWriteProgress> progress = null)
#endif
        {
            _output.ThrowIfDisposed();
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            _output.BeginOperation();
            try
            {
                var fields = Plan(message);
                try
                {
                    foreach (var field in fields)
                    {
                        _output.ThrowIfDisposed();
                        _output.Write(field.Span);
                        _fieldsWritten++;
                        progress?.Report(GetProgress());
                    }

                    _output.Write(GetHeaderEnd(message).Span);
                    for (var remaining = message.RawBody; !remaining.IsEmpty; remaining = remaining.Slice(Math.Min(remaining.Length, BlockSize)))
                    {
                        _output.ThrowIfDisposed();
                        _output.Write(remaining.Span.Slice(0, Math.Min(remaining.Length, BlockSize)));
                        progress?.Report(GetProgress());
                    }

                    _entitiesWritten++;
                    progress?.Report(GetProgress());
                }
                catch
                {
                    _output.MarkBroken();
                    throw;
                }

                _output.ThrowIfDisposed();
            }
            finally
            {
                if (_output.EndOperation()) _output.Release();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Flushes the stream asynchronously, whether the writer opened it or it was supplied by the caller.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the flush is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the stream has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// flush is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the I/O, and cancellation, go to the task.
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            return FlushCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Writes a whole message asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so <paramref name="progress"/> may not call the writer.  Any number of messages can be written, one after another,
        /// with nothing between them.
        /// </remarks>
        /// <param name="message">The message.</param>
        /// <param name="progress">Receives the progress after each field and each block of the body has been handed to the stream, synchronously on the writing thread; or
        /// <c>null</c>.  An exception from <see cref="IProgress{T}.Report"/> ends the call, leaving the message incomplete.  <see cref="Progress{T}"/> runs its handler later, so writing
        /// doesn't wait for the handler, and an exception from the handler doesn't end the call.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the whole message has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ImfFileException">By the task: the message can't be written.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteMessageAsync(ImfMessage message, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)
#else
        public Task WriteMessageAsync(ImfMessage message, IProgress<ImfWriteProgress> progress = null, CancellationToken cancellationToken = default)
#endif
        {
            _output.ThrowIfDisposed();
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            _output.BeginOperation();
            return WriteMessageCoreAsync(message, progress, cancellationToken);
        }

        private async Task FlushCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                if (_output.EndOperation()) await _output.ReleaseAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteMessageCoreAsync(ImfMessage message,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<ImfWriteProgress>? progress,
#else
            IProgress<ImfWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            var hasStartedOutput = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fields = Plan(message);
                hasStartedOutput = true;
                foreach (var field in fields)
                {
                    _output.ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await _output.WriteAsync(field, cancellationToken).ConfigureAwait(false);
                    _fieldsWritten++;
                    progress?.Report(GetProgress());
                }

                await _output.WriteAsync(GetHeaderEnd(message), cancellationToken).ConfigureAwait(false);
                for (var remaining = message.RawBody; !remaining.IsEmpty; remaining = remaining.Slice(Math.Min(remaining.Length, BlockSize)))
                {
                    _output.ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await _output.WriteAsync(remaining.Slice(0, Math.Min(remaining.Length, BlockSize)), cancellationToken).ConfigureAwait(false);
                    progress?.Report(GetProgress());
                }

                _entitiesWritten++;
                progress?.Report(GetProgress());
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            catch when (hasStartedOutput)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                if (_output.EndOperation()) await _output.ReleaseAsync().ConfigureAwait(false);
            }
        }

        private static FileStream OpenFile(string path)
        {
            if (path is null)
                throw new ArgumentNullException("messageFilePath");

            return ImfFileStreams.OpenWrite(path, ImfOutput.FileBufferSize);
        }
#endif

        /// <summary>
        /// Checks a message, and gets the bytes of its fields, so that a message that is refused leaves nothing in the output.
        /// </summary>
        /// <exception cref="ImfFileException">The message can't be written.</exception>
        private ReadOnlyMemory<byte>[] Plan(ImfMessage message)
        {
            var diagnostics = ImfConformance.Check(message, _options);
            foreach (var diagnostic in diagnostics)
            {
                if (!ImfConformance.IsAllowed(diagnostic.Kind, _options))
                    throw ImfConformance.BuildException(diagnostic, false);
            }

            var fields = new ReadOnlyMemory<byte>[message.Fields.Count];
            var buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
            try
            {
                for (var index = 0; index < fields.Length; index++)
                {
                    fields[index] = message.Fields[index].GetOutput(_options.HeaderEncoding, _lineBreak, ref buffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            _diagnostics.AddRange(diagnostics);
            return fields;
        }

        private ReadOnlyMemory<byte> GetHeaderEnd(ImfMessage message)
        {
            var headerEnd = message.HeaderEnd;
            if (headerEnd is null || (headerEnd.Length == 0 && !message.RawBody.IsEmpty)) return _lineBreak;

            return headerEnd;
        }

        private ImfWriteProgress GetProgress()
        {
            return new ImfWriteProgress(_fieldsWritten, _entitiesWritten, _output.BytePosition);
        }

        private static ImfOptions CheckOptions(ImfOptions options)
        {
            ImfHeaderText.CheckOptions(options);
            return options;
        }
    }
}
