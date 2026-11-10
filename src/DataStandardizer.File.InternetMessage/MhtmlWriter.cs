using System;
using System.Collections.Generic;
using System.IO;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a writer of MHTML files (<c>.mht</c>, <c>.mhtml</c>), or any other aggregate documents (RFC 2557), from <see cref="MhtmlDocument"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This writes the message of a document as an <see cref="ImfWriter"/> does, under the same options, so anything that hasn't changed is written byte for byte, and with the same checks.
    /// </para>
    /// <para>
    /// <b>Structure.</b>  The message as it now is, not as it was when the document was made, is checked as <see cref="MhtmlDocument"/> describes, under the options of the writer and with no
    /// base URI, and a message that it refuses leaves nothing in the output.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  After a write fails once output has started, a document is incomplete, so the writer cannot continue, and the same goes after any
    /// write is canceled.
    /// </para>
    /// </remarks>
    public sealed class MhtmlWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private readonly ImfWriter _writer;

        /// <summary>
        /// Creates a writer of documents to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.</exception>
        public MhtmlWriter(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of documents to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.
        /// -or-
        /// <paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public MhtmlWriter(Stream messageStream, ImfOptions options) : this(messageStream, options, ImfEntityWriter.GenerateBoundary)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of documents to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public MhtmlWriter(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of documents to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that
        /// is needed and can't be resolved.</exception>
        public MhtmlWriter(string messageFilePath, ImfOptions options)
        {
            _writer = new ImfWriter(messageFilePath, options, ImfConformanceProfile.Mhtml);
        }
#endif

        /// <summary>
        /// Creates a writer with a source of boundaries.  Allows tests to choose the boundaries.
        /// </summary>
        internal MhtmlWriter(Stream messageStream, ImfOptions options, Func<string> boundaryGenerator)
        {
            _writer = new ImfWriter(messageStream, options, false, boundaryGenerator, ImfConformanceProfile.Mhtml);
        }

        /// <summary>
        /// Gets the number of bytes written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long BytePosition => _writer.BytePosition;

        /// <summary>
        /// Gets the problems found so far that did not stop writing.  The list is read-only, and grows as the writer finds more.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public IReadOnlyList<ImfDiagnostic> Diagnostics => _writer.Diagnostics;

        /// <summary>
        /// Closes the file the writer opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the file is closed once
        /// it has finished with it.
        /// </remarks>
        public void Dispose()
        {
            _writer.Dispose();
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
            return _writer.DisposeAsync();
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
            _writer.Flush();
        }

        /// <summary>
        /// Writes a whole document.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="progress">Receives the progress after each field and each block of the body has been handed to the stream, as <see cref="ImfWriter.WriteMessage"/> describes;
        /// or <c>null</c>.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The document can't be written.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteDocument(MhtmlDocument document, IProgress<ImfWriteProgress>? progress = null)
#else
        public void WriteDocument(MhtmlDocument document, IProgress<ImfWriteProgress> progress = null)
#endif
        {
            _writer.ThrowIfDisposed();
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            _writer.WriteMessage(document.Message, progress);
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
            return _writer.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Writes a whole document asynchronously.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="progress">Receives the progress after each field and each block of the body has been handed to the stream, as <see cref="ImfWriter.WriteMessageAsync"/>
        /// describes; or <c>null</c>.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the whole document has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ImfFileException">By the task: the document can't be written.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteDocumentAsync(MhtmlDocument document, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)
#else
        public Task WriteDocumentAsync(MhtmlDocument document, IProgress<ImfWriteProgress> progress = null, CancellationToken cancellationToken = default)
#endif
        {
            _writer.ThrowIfDisposed();
            if (document is null)
                throw new ArgumentNullException(nameof(document));

            return _writer.WriteMessageAsync(document.Message, progress, cancellationToken);
        }
#endif
    }
}
