using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a writer of an Apple Mail EMLX file from an <see cref="EmlxEntry"/>: the byte count, the message and the property list of metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Format.</b>  EMLX is proprietary and undocumented, so this follows the layout that <see cref="EmlxReader"/> describes.
    /// </para>
    /// <para>
    /// <b>Byte count.</b>  The count is worked out from what is written.  An entry that was read keeps its first line if the count is still right, and otherwise the width of that line, so an unchanged
    /// file is written back byte for byte.
    /// </para>
    /// <para>
    /// <b>Message.</b>  The message is checked and written as an <see cref="ImfWriter"/> does, under the same options, so a message that it refuses leaves nothing in the output.  It is written with LF
    /// line breaks, so one read from an <c>.eml</c> file changes in that way.  A message that has to change this way can't have a <c>binary</c> entity (RFC 2045 §2.9) or an extension mechanism
    /// (§6.3), as a line break in it may not be one, which causes an <see cref="ImfFileException"/>.
    /// </para>
    /// <para>
    /// <b>Metadata.</b>  Metadata that was read and hasn't changed is written byte for byte.  A property list that was missing or not well formed when read causes an
    /// <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowInvalidEmlxMetadata"/> is set, which writes its bytes back with a diagnostic.  Values that nest more deeply than <see cref="ImfOptions.MaxNestingDepth"/> are
    /// treated the same way, and a property list longer than <see cref="ImfOptions.MaxEmlxMetadataLength"/> always causes an <see cref="ImfFileException"/>, as a reader with the same options wouldn't
    /// accept either.
    /// </para>
    /// <para>
    /// A file holds one entry, so a second write throws <see cref="InvalidOperationException"/>, though a write that was refused with nothing written can be tried again with another entry.  Only one
    /// operation may be in progress at a time.  After a write fails once output has started, the file is incomplete, so the writer cannot continue, and the same goes after any write is canceled.
    /// </para>
    /// </remarks>
    public sealed class EmlxWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int BlockSize = 16 * 1024;
        private const string InvalidMetadataMessage = "The metadata was read from a property list that is missing or not well formed.  ImfOptions.AllowInvalidEmlxMetadata accepts it.";
        private const string MetadataTooDeepMessage = "The values of the metadata nest more deeply than ImfOptions.MaxNestingDepth, so a reader wouldn't accept them.  ImfOptions.AllowInvalidEmlxMetadata accepts it.";
        private const string MetadataTooLongTemplate = "The property list is longer than the maximum of {0} octets.";

        private readonly Func<string> _boundaryGenerator;
        private readonly List<ImfDiagnostic> _diagnostics = new List<ImfDiagnostic>();
        private readonly ImfOptions _options;
        private readonly ImfOutput _output;
        private readonly ReadOnlyCollection<ImfDiagnostic> _readOnlyDiagnostics;

        private long _fieldsWritten;
        private bool _hasWritten;

        /// <summary>
        /// Creates a writer of a file to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.</exception>
        public EmlxWriter(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of a file to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.
        /// -or-
        /// <paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public EmlxWriter(Stream messageStream, ImfOptions options) : this(messageStream, options, ImfEntityWriter.GenerateBoundary)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public EmlxWriter(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that
        /// is needed and can't be resolved.</exception>
        public EmlxWriter(string messageFilePath, ImfOptions options) : this(CheckOptions(options), OpenFile(messageFilePath), true, ImfEntityWriter.GenerateBoundary)
        {
        }
#endif

        /// <summary>
        /// Creates a writer with a source of boundaries.  Allows tests to choose the boundaries.
        /// </summary>
        internal EmlxWriter(Stream messageStream, ImfOptions options, Func<string> boundaryGenerator)
            : this(CheckOptions(options), ImfOutput.ValidateStream(messageStream), false, boundaryGenerator)
        {
        }

        // The options come first, so that bad options are found before a file is opened.
        private EmlxWriter(ImfOptions options, Stream stream, bool ownsStream, Func<string> boundaryGenerator)
        {
            _options = options;
            _boundaryGenerator = boundaryGenerator;
            _output = new ImfOutput(stream, ownsStream, GetType().Name);
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
            return _output.MarkDisposed() ? AsyncOutcome.Decide(_output.ReleaseAsync(), CancellationToken.None, _output) : _output.WaitForReleaseAsync();
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
        /// Writes the file.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="progress">Receives the progress after each block has been handed to the stream, synchronously on the writing thread; or <c>null</c>.  Its fields and entities are counted once, at
        /// the end.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The entry can't be written.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An entry has already been written, or an earlier write failed or was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteEntry(EmlxEntry entry, IProgress<ImfWriteProgress>? progress = null)
#else
        public void WriteEntry(EmlxEntry entry, IProgress<ImfWriteProgress> progress = null)
#endif
        {
            _output.ThrowIfDisposed();
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            BeginWrite();
            try
            {
                var bytes = Plan(entry, out var diagnostics, out var fieldCount);
                try
                {
                    for (var remaining = new ReadOnlyMemory<byte>(bytes); !remaining.IsEmpty; remaining = remaining.Slice(Math.Min(remaining.Length, BlockSize)))
                    {
                        _output.ThrowIfDisposed();
                        _output.Write(remaining.Span.Slice(0, Math.Min(remaining.Length, BlockSize)));
                        progress?.Report(GetProgress(0));
                    }

                    Finish(diagnostics, fieldCount, progress);
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
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The flush is canceled only if this token is canceled, and the writer then cannot
        /// continue.</param>
        /// <returns>A task that completes once the stream has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// flush is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the operation failed with an
        /// <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/> was canceled.  The task is canceled.  An
        /// <see cref="OperationCanceledException"/> while <paramref name="cancellationToken"/> isn't canceled faults the task, as any other exception does.</exception>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the I/O, and cancellation, go to the task.
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            return AsyncOutcome.Decide(FlushCoreAsync(cancellationToken), cancellationToken, _output);
        }

        /// <summary>
        /// Writes the file asynchronously.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="progress">Receives the progress after each block has been handed to the stream, synchronously on the writing thread; or <c>null</c>.  Its fields and entities are counted once, at
        /// the end.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The write is canceled only if this token is canceled, and the writer then cannot
        /// continue.</param>
        /// <returns>A task that completes once the whole file has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="entry"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An entry has already been written, or an earlier write failed or was canceled.</exception>
        /// <exception cref="ImfFileException">By the task: the entry can't be written.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the operation failed with an
        /// <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/> was canceled.  The task is canceled.  An
        /// <see cref="OperationCanceledException"/> while <paramref name="cancellationToken"/> isn't canceled faults the task, as any other exception does.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteEntryAsync(EmlxEntry entry, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)
#else
        public Task WriteEntryAsync(EmlxEntry entry, IProgress<ImfWriteProgress> progress = null, CancellationToken cancellationToken = default)
#endif
        {
            _output.ThrowIfDisposed();
            if (entry is null)
                throw new ArgumentNullException(nameof(entry));

            BeginWrite();
            return AsyncOutcome.Decide(WriteEntryCoreAsync(entry, progress, cancellationToken), cancellationToken, _output);
        }

        private async Task FlushCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _output.FlushAsync(cancellationToken).ConfigureAwait(false);
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                if (_output.EndOperation()) await _output.ReleaseAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteEntryCoreAsync(EmlxEntry entry,
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
                var bytes = Plan(entry, out var diagnostics, out var fieldCount);
                hasStartedOutput = true;
                for (var remaining = new ReadOnlyMemory<byte>(bytes); !remaining.IsEmpty; remaining = remaining.Slice(Math.Min(remaining.Length, BlockSize)))
                {
                    _output.ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await _output.WriteAsync(remaining.Slice(0, Math.Min(remaining.Length, BlockSize)), cancellationToken).ConfigureAwait(false);
                    progress?.Report(GetProgress(0));
                }

                Finish(diagnostics, fieldCount, progress);
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
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
        /// Starts an operation that writes the one entry.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another operation is in progress, or the entry has already been written.</exception>
        private void BeginWrite()
        {
            _output.BeginOperation();
            if (_hasWritten)
            {
                if (_output.EndOperation()) _output.Release();

                throw new InvalidOperationException("An EMLX file holds one entry, and it has already been written.");
            }
        }

        /// <summary>
        /// Checks an entry, and gets the bytes of the file, so that one that is refused leaves nothing in the output.
        /// </summary>
        /// <exception cref="ImfFileException">The entry can't be written.</exception>
        private byte[] Plan(EmlxEntry entry, out List<ImfDiagnostic> diagnostics, out long fieldCount)
        {
            diagnostics = new List<ImfDiagnostic>();

            // A message is written in the line breaks it has, so that what hasn't changed stays as it is, and then they are changed all at once if the file needs others.
            var message = entry.Message;
            var hasLineFeedBreaks = message.LineFeedIsLineBreak;
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, _options, hasLineFeedBreaks, _boundaryGenerator))
            {
                writer.WriteMessage(message);
                diagnostics.AddRange(writer.Diagnostics);
                fieldCount = writer.FieldsWritten;
            }

            var messageBytes = hasLineFeedBreaks ? output.ToArray() : ImfLineBreaks.Convert(message, output.ToArray(), false);

            var metadata = entry.Metadata;
            if (!metadata.IsWellFormed) ReportInvalidMetadata(InvalidMetadataMessage, diagnostics);
            else if (EmlxPlist.GetDepth(metadata.Entries) > _options.MaxNestingDepth) ReportInvalidMetadata(MetadataTooDeepMessage, diagnostics);

            var metadataBytes = metadata.GetBytes();
            if (metadataBytes.Length > _options.MaxEmlxMetadataLength)
                throw new ImfFileException(string.Format(MetadataTooLongTemplate, _options.MaxEmlxMetadataLength));

            var byteCountLine = EmlxLayout.FormatByteCount(messageBytes.Length, entry.ByteCountLine);
            var file = new byte[byteCountLine.Length + messageBytes.Length + metadataBytes.Length];
            Array.Copy(byteCountLine, file, byteCountLine.Length);
            Array.Copy(messageBytes, 0, file, byteCountLine.Length, messageBytes.Length);
            Array.Copy(metadataBytes, 0, file, byteCountLine.Length + messageBytes.Length, metadataBytes.Length);

            _hasWritten = true;
            return file;
        }

        private void ReportInvalidMetadata(string message, List<ImfDiagnostic> diagnostics)
        {
            if (!ImfConformance.IsAllowed(ImfDiagnosticKind.InvalidEmlxMetadata, _options))
            {
                var exception = new ImfFileException(message);
                exception.Data[ImfFileException.DataItemName.DiagnosticKind] = ImfDiagnosticKind.InvalidEmlxMetadata;
                throw exception;
            }

            diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.InvalidEmlxMetadata, message, -1, string.Empty));
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private void Finish(List<ImfDiagnostic> diagnostics, long fieldCount, IProgress<ImfWriteProgress>? progress)
#else
        private void Finish(List<ImfDiagnostic> diagnostics, long fieldCount, IProgress<ImfWriteProgress> progress)
#endif
        {
            _diagnostics.AddRange(diagnostics);
            _fieldsWritten = fieldCount;
            progress?.Report(GetProgress(1));
        }

        private ImfWriteProgress GetProgress(long entitiesWritten)
        {
            return new ImfWriteProgress(_fieldsWritten, entitiesWritten, _output.BytePosition);
        }

        private static ImfOptions CheckOptions(ImfOptions options)
        {
            ImfHeaderText.CheckOptions(options);
            return options;
        }
    }
}
