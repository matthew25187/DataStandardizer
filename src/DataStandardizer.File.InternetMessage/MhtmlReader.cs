using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a reader of an MHTML file (<c>.mht</c>, <c>.mhtml</c>), or any other aggregate document (RFC 2557), into an <see cref="MhtmlDocument"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An MHTML file is a MIME message, so this reads it as an <see cref="ImfReader"/> does, under the same options, and gives the same tree of entities.  It differs in two ways.
    /// </para>
    /// <para>
    /// <b>Required fields.</b>  The top level of an MHTML file is a MIME entity as much as a message, and some programs write no Date or From field.  So a Date or From field that RFC 5322
    /// §3.6 requires, or a Sender field that §3.6.2 requires, is recorded in <see cref="Diagnostics"/> if it is missing, whatever <see cref="ImfOptions.AllowMissingRequiredFields"/> says.
    /// The fields that §3.6.6 requires in a block of resent fields are not relaxed.  Every other check stays as strict as that of <see cref="ImfReader"/>, including the grammar of a
    /// field that is there.  So the <c>From: &lt;Saved by Blink&gt;</c> that Chromium writes needs <see cref="ImfOptions.AllowInvalidValues"/>.
    /// </para>
    /// <para>
    /// <b>Structure.</b>  Once it has been read, the message is checked as <see cref="MhtmlDocument"/> describes.  So a <c>multipart/related</c> entity with no <c>type</c> parameter, as Word
    /// writes, needs <see cref="ImfOptions.AllowInvalidRelatedStructure"/>.
    /// </para>
    /// <para>
    /// <b>Character sets.</b>  Word writes its HTML in <c>windows-1252</c>.  On .NET Core and .NET 5 and later, <see cref="ImfEntity.GetText"/> can't resolve that character set until the
    /// application registers the code pages or supplies an <see cref="ImfOptions.CharsetResolver"/>, as <see cref="DataStandardizer.Communication.Mime.MimeCharsetResolver"/> describes.
    /// Reading the document doesn't need it.
    /// </para>
    /// <para>
    /// The data holds one document, so it can be read only once, and any <see cref="ImfFileException"/> ends reading.  Later reads throw <see cref="InvalidOperationException"/>.  A read
    /// started while another is in progress does too.
    /// </para>
    /// </remarks>
    public sealed class MhtmlReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int DefaultBufferSize = 16 * 1024;

        private readonly ImfReader _reader;

        /// <summary>
        /// Creates a reader for the document at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.</exception>
        public MhtmlReader(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the document at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.
        /// -or-
        /// <paramref name="options"/> has a limit that is less than 1, no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public MhtmlReader(Stream messageStream, ImfOptions options)
        {
            _reader = new ImfReader(messageStream, options, DefaultBufferSize, false, ImfConformanceProfile.Mhtml);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for the document in a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public MhtmlReader(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the document in a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1, no
        /// <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public MhtmlReader(string messageFilePath, ImfOptions options)
        {
            _reader = new ImfReader(messageFilePath, options, ImfConformanceProfile.Mhtml);
        }
#endif

        /// <summary>
        /// Gets the problems found so far that did not stop reading.  The list is read-only, and grows as the reader finds more.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public IReadOnlyList<ImfDiagnostic> Diagnostics => _reader.Diagnostics;

        /// <summary>
        /// Gets the number of bytes of the data read into the document so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long BytePosition => _reader.BytePosition;

        /// <summary>
        /// Releases the reader's buffers, and the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers and
        /// file are released once it has finished with them.
        /// </remarks>
        public void Dispose()
        {
            _reader.Dispose();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Releases the reader's buffers, and the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the buffers and file are released once it has finished with them.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            return _reader.DisposeAsync();
        }
#endif

        /// <summary>
        /// Reads the document.
        /// </summary>
        /// <param name="baseUri">The absolute URI for a relative one to resolve against when the document has none of its own, or <c>null</c> for none.</param>
        /// <returns>The document.</returns>
        /// <exception cref="ArgumentException"><paramref name="baseUri"/> is not absolute.</exception>
        /// <exception cref="ImfFileException">The document breaks a rule of its format that the options don't accept, or passes a limit.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// The document has already been read, or an earlier read failed or was canceled.</exception>
        public MhtmlDocument ReadDocument([AllowNull] Uri baseUri = null)
        {
            MhtmlDocument.CheckBaseUri(baseUri, nameof(baseUri));
            return Complete(_reader.ReadMessage(), baseUri);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the document asynchronously.
        /// </summary>
        /// <param name="baseUri">The absolute URI for a relative one to resolve against when the document has none of its own, or <c>null</c> for none.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot continue.</param>
        /// <returns>A task whose result is the document.</returns>
        /// <exception cref="ArgumentException"><paramref name="baseUri"/> is not absolute.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// The document has already been read, or an earlier read failed or was canceled.</exception>
        /// <exception cref="ImfFileException">By the task: the document breaks a rule of its format that the options don't accept, or passes a limit.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public Task<MhtmlDocument> ReadDocumentAsync([AllowNull] Uri baseUri = null, CancellationToken cancellationToken = default)
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            MhtmlDocument.CheckBaseUri(baseUri, nameof(baseUri));
            return CompleteAsync(_reader.ReadMessageAsync(cancellationToken), baseUri);
        }

        private async Task<MhtmlDocument> CompleteAsync(Task<ImfMessage> read, [AllowNull] Uri baseUri)
        {
            return Complete(await read.ConfigureAwait(false), baseUri);
        }
#endif

        private MhtmlDocument Complete(ImfMessage message, [AllowNull] Uri baseUri)
        {
            var document = new MhtmlDocument(message, baseUri);
            _reader.AddDiagnostics(document.Diagnostics);
            return document;
        }
    }
}
