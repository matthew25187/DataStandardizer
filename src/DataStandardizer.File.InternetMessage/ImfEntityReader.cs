using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to a whole Internet message as a stream of events: entity start, header
    /// field, header end, body chunk and entity end.  It follows RFC 2046 §5 into multipart and embedded message entities, and never holds a whole body
    /// in memory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Structure.</b>  Each entity's header section is read as an <see cref="ImfHeaderReader"/> reads one, under the same options, and the limits on a
    /// section apply to each section.  An entity with no Content-Type field is <c>text/plain; charset=us-ascii</c> (RFC 2045 §5.2), or
    /// <c>message/rfc822</c> if it is a part of a <c>multipart/digest</c> entity (RFC 2046 §5.1.5).  A message with a MIME field and no MIME-Version field, which RFC 2045 §4
    /// requires, causes an <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowMissingMimeVersion"/> is set.  A field that is not valid, or a second Content-Type or Content-Transfer-Encoding field, is
    /// an error.
    /// </para>
    /// <para>
    /// <b>Multipart entities.</b>  A delimiter is a line break, <c>--</c>, the boundary, optional spaces and tabs, and a line break.  A close delimiter has
    /// <c>--</c> after the boundary, and may end the data with no line break.  The line break before a delimiter belongs to the delimiter, not to the
    /// part (RFC 2046 §5.1.1).  So a part with no body ends with the empty line that shares its line break with the delimiter, and a part with headers
    /// and no empty line before its delimiter is not valid.  A line is a delimiter of an open multipart entity only if it matches exactly, the innermost entity first, so a
    /// boundary may be a prefix of another, and a line that merely starts with a boundary is content.  A line longer than the 998 octets that RFC 5322
    /// §2.1.1 allows is never a delimiter.
    /// </para>
    /// <para>
    /// A delimiter of an enclosing entity ends every entity inside it.  A multipart entity that ends without its close delimiter, because of that or the
    /// end of the data, is an error unless <see cref="ImfOptions.AllowMissingBoundaryTerminator"/> is set.  With that option, a part's header section may
    /// end at the end of the data with no empty line, as there is then no delimiter to need one.
    /// </para>
    /// <para>
    /// <b>Embedded messages.</b>  The body of a <c>message/rfc822</c> or <c>message/global</c> (RFC 6532) entity is read as a nested entity, except that a
    /// <c>message/global</c> body with a <c>quoted-printable</c> or <c>base64</c> transfer encoding, which RFC 6532 §3.7 allows, is given as body chunks.
    /// The bodies of <c>message/partial</c> and <c>message/external-body</c> entities are given as body chunks too, and are not reassembled or followed: a
    /// fragment after the first has no header (RFC 2046 §5.2.2).
    /// </para>
    /// <para>
    /// <b>Bodies.</b>  Body chunks are raw bytes unless <see cref="ImfOptions.DecodeTransferEncoding"/> is set.  A transfer encoding other than <c>7bit</c>,
    /// <c>8bit</c> or <c>binary</c> on a multipart or message entity is an error (RFC 2045 §6.4), and <c>message/partial</c> and <c>message/external-body</c>
    /// must be <c>7bit</c> (RFC 2046 §5.2).  The bytes of a body are checked against the rules of RFC 5322 §2.1.1 and §2.3, and its transfer encoding and that of what holds it (RFC 2045
    /// §2.7, §2.8, §6.4, §6.7 and §6.8), unless <see cref="ImfOptions.AllowLongLines"/> or <see cref="ImfOptions.AllowInvalidBodyOctets"/> is set.
    /// </para>
    /// <para>
    /// <b>Errors.</b>  Any <see cref="ImfFileException"/> ends reading, so later reads throw <see cref="InvalidOperationException"/>.  Its
    /// <see cref="ImfFileException.DataItemName.EntityPath"/> item says which entity it was found in.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while another is in
    /// progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class ImfEntityReader : IDisposable, IImfHeaderFieldSource
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int DefaultBufferSize = 16 * 1024;

        private readonly ReadOnlyCollection<ImfDiagnostic> _diagnostics;
        private readonly ImfEntityScanner _entityScanner;
        private readonly bool _ownsStream;
        private readonly Stream _stream;

        private bool _hasCurrentEvent;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif

        // These are changed with Interlocked, so that a Dispose from another thread while a read is in progress returns the buffers once only, after
        // that read has finished with them.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        private int _eventVersion;

        /// <summary>
        /// Creates a reader for the message at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.</exception>
        public ImfEntityReader(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the message at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.
        /// -or-
        /// <paramref name="options"/> has a limit that is less than 1, no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public ImfEntityReader(Stream messageStream, ImfOptions options) : this(messageStream, options, DefaultBufferSize, false)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for the message at the start of a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public ImfEntityReader(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the message at the start of a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1, no
        /// <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public ImfEntityReader(string messageFilePath, ImfOptions options)
            : this(CheckOptions(options), ImfHeaderReader.OpenFile(messageFilePath), true, options, DefaultBufferSize, false)
        {
        }
#endif

        /// <summary>
        /// Creates a reader with a specific size for the internal block and a line break setting.  Allows tests to force refills at chosen points,
        /// and the readers of container formats to read the LF line breaks of their messages.
        /// </summary>
        internal ImfEntityReader(Stream messageStream, ImfOptions options, int bufferSize, bool lineFeedIsLineBreak)
            : this(CheckOptions(options), ImfHeaderReader.ValidateStream(messageStream), false, options, bufferSize, lineFeedIsLineBreak)
        {
        }

        // The encoding comes first, so that bad options are found before a file is opened.
        private ImfEntityReader(Encoding fallbackEncoding, Stream stream, bool ownsStream, ImfOptions options, int bufferSize, bool lineFeedIsLineBreak)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _stream = stream;
            _ownsStream = ownsStream;
            _entityScanner = new ImfEntityScanner(new ImfHeaderScanner(bufferSize, options, fallbackEncoding, lineFeedIsLineBreak), options, lineFeedIsLineBreak);
            _diagnostics = new ReadOnlyCollection<ImfDiagnostic>(_entityScanner.Diagnostics);
        }

        /// <summary>
        /// Gets the number of bytes of the data that the events read so far take up.  Decoded or not, this counts the raw bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long BytePosition
        {
            get
            {
                ThrowIfDisposed();
                return _entityScanner.HeaderScanner.BytePosition;
            }
        }

        /// <summary>
        /// Gets the problems found so far that did not stop reading.  The list is read-only, and grows as the reader finds more.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public IReadOnlyList<ImfDiagnostic> Diagnostics
        {
            get
            {
                ThrowIfDisposed();
                return _diagnostics;
            }
        }

        /// <summary>
        /// Gets the number of entities found so far, including the message itself.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long EntitiesRead
        {
            get
            {
                ThrowIfDisposed();
                return _entityScanner.EntitiesRead;
            }
        }

        /// <summary>
        /// Gets or sets the index of the message in the mbox file it came from, for the exceptions of this reader, or -1 if it didn't come from one.
        /// </summary>
        internal long MessageIndex { get; set; } = -1;

        /// <summary>
        /// Releases the reader's buffers, and the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers and
        /// file are released once it has finished with them.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            _hasCurrentEvent = false;

            if (Volatile.Read(ref _isReadInProgress) == 0)
            {
                Release();
            }
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
            Dispose();
            return default;
        }
#endif

        /// <summary>
        /// Reads the next event, without allocation for a body chunk.
        /// </summary>
        /// <returns>A view of the event, valid until the next read; or <c>null</c> if the message has ended.</returns>
        /// <exception cref="ImfFileException">The message breaks a rule of its format, or passes a limit.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found an error, or was canceled, so the reader cannot continue.</exception>
        public ImfEntityEvent? ReadEvent()
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                StartEvent();
                while (true)
                {
                    var status = _entityScanner.Next();
                    if (status != ImfEntityScanner.Status.NeedMoreInput)
                    {
                        return CompleteEvent(status);
                    }

                    int offset, count;
                    _entityScanner.PrepareRefill(out offset, out count);
                    var bytesRead = _stream.Read(_entityScanner.HeaderScanner.Buffer, offset, count);
                    ThrowIfDisposed();
                    _entityScanner.HeaderScanner.CompleteRefill(bytesRead);
                }
            }
            catch (ImfFileException exception) when (AddMessageIndex(exception))
            {
                throw;
            }
            finally
            {
                EndRead();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the next event asynchronously, without allocation for a body chunk when the task completes synchronously, which it does when the reader's
        /// buffer already holds what the event needs.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot
        /// continue.</param>
        /// <returns>A task whose result is a view of the event, valid until the next read; or <c>null</c> if the message has ended.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found an error, or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="ImfFileException">By the task: the message breaks a rule of its format, or passes a limit.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask<ImfEntityEvent?> ReadEventAsync(CancellationToken cancellationToken = default)
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            var isPending = false;
            try
            {
                StartEvent();
                if (cancellationToken.IsCancellationRequested)
                {
                    _isCanceled = true;
                    return new ValueTask<ImfEntityEvent?>(Task.FromCanceled<ImfEntityEvent?>(cancellationToken));
                }

                try
                {
                    var status = _entityScanner.Next();
                    if (status != ImfEntityScanner.Status.NeedMoreInput)
                    {
                        return new ValueTask<ImfEntityEvent?>(CompleteEvent(status));
                    }
                }
                catch (ImfFileException exception)
                {
                    AddMessageIndex(exception);
                    return new ValueTask<ImfEntityEvent?>(Task.FromException<ImfEntityEvent?>(exception));
                }

                isPending = true;
                return ReadWithRefillAsync(cancellationToken);
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
        /// Continues <see cref="ReadEventAsync"/> once the scanner needs more input, and ends the read.
        /// </summary>
        private async ValueTask<ImfEntityEvent?> ReadWithRefillAsync(CancellationToken cancellationToken)
        {
            try
            {
                ImfEntityScanner.Status status;
                do
                {
                    int offset, count;
                    _entityScanner.PrepareRefill(out offset, out count);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    var bytesRead = await _stream.ReadAsync(new Memory<byte>(_entityScanner.HeaderScanner.Buffer, offset, count), cancellationToken).ConfigureAwait(false);
#else
                    var bytesRead = await _stream.ReadAsync(_entityScanner.HeaderScanner.Buffer, offset, count, cancellationToken).ConfigureAwait(false);
#endif
                    ThrowIfDisposed();
                    _entityScanner.HeaderScanner.CompleteRefill(bytesRead);
                    status = _entityScanner.Next();
                } while (status == ImfEntityScanner.Status.NeedMoreInput);

                return CompleteEvent(status);
            }
            catch (ImfFileException exception) when (AddMessageIndex(exception))
            {
                throw;
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

        /// <inheritdoc />
        ImfHeaderScanner IImfHeaderFieldSource.GetScanner(int fieldVersion)
        {
            GetEntityScanner(fieldVersion);
            return _entityScanner.HeaderScanner;
        }

        /// <summary>
        /// Gets the scanner holding the current event, for a view of that event.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on from the event the view was taken for.</exception>
        internal ImfEntityScanner GetEntityScanner(int eventVersion)
        {
            ThrowIfDisposed();
            if (!_hasCurrentEvent || eventVersion != _eventVersion)
                throw new InvalidOperationException("The reader has moved on from the event of this view.");

            return _entityScanner;
        }

        // Used as an exception filter that never catches, so that the exception keeps its stack trace.
        private bool AddMessageIndex(ImfFileException exception)
        {
            if (MessageIndex >= 0) exception.Data[ImfFileException.DataItemName.MessageIndex] = MessageIndex;

            return false;
        }

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
        /// Marks the read as finished, and releases the buffers and file if the reader was disposed while it was in progress.
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
        /// Checks that the reader can continue, and moves on from the current event.
        /// </summary>
        /// <exception cref="InvalidOperationException">An earlier read found an error, or was canceled.</exception>
        private void StartEvent()
        {
            if (_entityScanner.IsFaulted || _entityScanner.HeaderScanner.IsFaulted)
                throw new InvalidOperationException("The reader cannot continue after an error in the message.");
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
                throw new InvalidOperationException("The reader cannot continue after a read was canceled.");
#endif

            // Any view of the previous event is now out of date.
            unchecked
            {
                _eventVersion++;
            }

            _hasCurrentEvent = false;
        }

        /// <summary>
        /// Makes the event found by the scanner current, if any.
        /// </summary>
        private ImfEntityEvent? CompleteEvent(ImfEntityScanner.Status status)
        {
            if (status != ImfEntityScanner.Status.EventRead) return null;

            _hasCurrentEvent = true;
            return new ImfEntityEvent(this, _eventVersion);
        }

        /// <summary>
        /// Releases the buffers, and the file the reader opened itself, the first time it is called.
        /// </summary>
        private void Release()
        {
            if (Interlocked.Exchange(ref _isReleased, 1) != 0) return;

            if (_ownsStream)
            {
                _stream.Dispose();
            }

            _entityScanner.ReturnBuffers();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// Checks the options, and resolves the encoding for a field body that is not valid UTF-8.
        /// </summary>
        private static Encoding CheckOptions(ImfOptions options)
        {
            var encoding = ImfHeaderReader.GetFallbackEncoding(options);
            if (options.MaxNestingDepth < 1)
                throw new ArgumentException("The maximum nesting depth must be at least 1.", nameof(options));
            if (options.MaxPartCount < 1)
                throw new ArgumentException("The maximum part count must be at least 1.", nameof(options));
            if (options.CharsetResolver is null)
                throw new ArgumentException("A character set resolver is required to read entities.", nameof(options));

            return encoding;
        }
    }
}
