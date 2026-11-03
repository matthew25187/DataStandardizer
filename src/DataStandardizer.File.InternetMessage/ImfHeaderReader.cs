using System;
using System.IO;
using System.Text;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to the fields of one header section of an Internet message
    /// (RFC 5322 §2.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The section ends at an empty line or at the end of the data, since a message may have no body.  The bytes after the empty line are not
    /// read as fields.  The reader reads ahead, so the position of a stream afterwards is not the start of the body; that is at
    /// <see cref="BytePosition"/> from where the reader started.
    /// </para>
    /// <para>
    /// Each field is a name of printable US-ASCII characters, a colon and a body.  White space between the name and the colon is accepted, as RFC
    /// 5322 §4.5 requires of a reader.  The rest of the syntax of a field body, including its obsolete forms, is left to the code that reads the
    /// body, with the exception of line breaks: CR and LF may appear only together, as a CRLF.  A bare line feed is accepted only with
    /// <see cref="ImfOptions.AcceptBareLineFeed"/>, and a bare carriage return is never accepted, although RFC 5322 §4.1 lists both in its
    /// obsolete unstructured text, because where a line ends would then be unclear.  Every field must end with a line break, including the last
    /// (RFC 5322 §3.6.8).
    /// </para>
    /// <para>
    /// A line over 998 octets (RFC 5322 §2.1.1) is rejected unless <see cref="ImfOptions.AllowLongLines"/> is set.  A field body must be valid
    /// UTF-8 (RFC 6532) unless <see cref="ImfOptions.AllowEightBitHeaders"/> is set.  <see cref="ImfOptions.MaxHeaderFieldCount"/> and
    /// <see cref="ImfOptions.MaxHeaderSectionLength"/> limit what hostile input can make the reader do.
    /// </para>
    /// <para>
    /// If a field's name or the UTF-8 of its body is at fault, an <see cref="ImfFileException"/> is thrown after the field has been consumed, so
    /// reading can continue with the next field.  If a line break, line length or limit is at fault, or the data ends inside a field, where the
    /// field ends is unknown, so later reads throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while another
    /// is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class ImfHeaderReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int DefaultBufferSize = 16 * 1024;

        private bool _hasCurrentField;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif

        // These are changed with Interlocked, so that a Dispose from another thread while a read is in progress returns the buffers once only,
        // after that read has finished with them.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        private int _fieldVersion;
        private readonly bool _ownsStream;
        private readonly ImfHeaderScanner _scanner;
        private readonly Stream _stream;

        /// <summary>
        /// Creates a reader for the header section at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.</exception>
        public ImfHeaderReader(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the header section at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.
        /// -or-
        /// <paramref name="options"/> has a limit that is less than 1, no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public ImfHeaderReader(Stream messageStream, ImfOptions options) : this(messageStream, options, DefaultBufferSize, false)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for the header section at the start of a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public ImfHeaderReader(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the header section at the start of a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1, no
        /// <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public ImfHeaderReader(string messageFilePath, ImfOptions options)
            : this(GetFallbackEncoding(options), OpenFile(messageFilePath), true, options, DefaultBufferSize, false)
        {
        }
#endif

        /// <summary>
        /// Creates a reader with a specific size for the internal block and a line break setting.  Allows tests to force refills at chosen points,
        /// and the readers of container formats to read the LF line breaks of their messages.
        /// </summary>
        internal ImfHeaderReader(Stream messageStream, ImfOptions options, int bufferSize, bool lineFeedIsLineBreak)
            : this(GetFallbackEncoding(options), ValidateStream(messageStream), false, options, bufferSize, lineFeedIsLineBreak)
        {
        }

        // The encoding comes first, so that bad options are found before a file is opened.
        private ImfHeaderReader(Encoding fallbackEncoding, Stream stream, bool ownsStream, ImfOptions options, int bufferSize, bool lineFeedIsLineBreak)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _stream = stream;
            _ownsStream = ownsStream;
            _scanner = new ImfHeaderScanner(bufferSize, options, fallbackEncoding, lineFeedIsLineBreak);
        }

        /// <summary>
        /// Gets the number of bytes of the data that the fields read so far take up, including the empty line that ends the section once it has
        /// been read.  At the end of the section, this is the offset of the body from where the reader started.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long BytePosition
        {
            get
            {
                ThrowIfDisposed();
                return _scanner.BytePosition;
            }
        }

        /// <summary>
        /// Gets the number of fields read so far, including any that failed to parse.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long FieldsRead
        {
            get
            {
                ThrowIfDisposed();
                return _scanner.FieldsRead;
            }
        }

        /// <summary>
        /// Gets the number of physical lines read so far.  A folded field spans several physical lines.
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
        /// Releases the reader's buffers, and the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers and
        /// file are released once it has finished with them.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            _hasCurrentField = false;

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
        /// Reads the next field of the section, without allocation.
        /// </summary>
        /// <returns>A view of the field, valid until the next read; or <c>null</c> if the section has ended.</returns>
        /// <exception cref="ImfFileException">The field does not follow the syntax of RFC 5322, has a line break that is not accepted, or passes a
        /// limit.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break, line length or limit error, or the data ended inside a field, or a read was canceled, so the reader
        /// cannot continue.</exception>
        public ImfHeaderFieldView? ReadField()
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                StartField();
                while (true)
                {
                    var status = _scanner.Scan();
                    if (status != ImfHeaderScanner.ScanStatus.NeedMoreInput)
                    {
                        return CompleteField(status);
                    }

                    int offset, count;
                    _scanner.PrepareRefill(out offset, out count);
                    var bytesRead = _stream.Read(_scanner.Buffer, offset, count);
                    ThrowIfDisposed();
                    _scanner.CompleteRefill(bytesRead);
                }
            }
            finally
            {
                EndRead();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the next field of the section asynchronously, without allocation when the task completes synchronously, which it does when the
        /// reader's buffer already holds the whole field.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot
        /// continue.</param>
        /// <returns>A task whose result is a view of the field, valid until the next read; or <c>null</c> if the section has ended.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break, line length or limit error, or the data ended inside a field, or a read was canceled, so the reader
        /// cannot continue.</exception>
        /// <exception cref="ImfFileException">By the task: the field does not follow the syntax of RFC 5322, has a line break that is not accepted,
        /// or passes a limit.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask<ImfHeaderFieldView?> ReadFieldAsync(CancellationToken cancellationToken = default)
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            var isPending = false;
            try
            {
                StartField();
                if (cancellationToken.IsCancellationRequested)
                {
                    _isCanceled = true;
                    return new ValueTask<ImfHeaderFieldView?>(Task.FromCanceled<ImfHeaderFieldView?>(cancellationToken));
                }

                try
                {
                    var status = _scanner.Scan();
                    if (status != ImfHeaderScanner.ScanStatus.NeedMoreInput)
                    {
                        return new ValueTask<ImfHeaderFieldView?>(CompleteField(status));
                    }
                }
                catch (ImfFileException exception)
                {
                    return new ValueTask<ImfHeaderFieldView?>(Task.FromException<ImfHeaderFieldView?>(exception));
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
        /// Continues <see cref="ReadFieldAsync"/> once the scanner needs more input, and ends the read.
        /// </summary>
        private async ValueTask<ImfHeaderFieldView?> ReadWithRefillAsync(CancellationToken cancellationToken)
        {
            try
            {
                ImfHeaderScanner.ScanStatus status;
                do
                {
                    int offset, count;
                    _scanner.PrepareRefill(out offset, out count);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    var bytesRead = await _stream.ReadAsync(new Memory<byte>(_scanner.Buffer, offset, count), cancellationToken).ConfigureAwait(false);
#else
                    var bytesRead = await _stream.ReadAsync(_scanner.Buffer, offset, count, cancellationToken).ConfigureAwait(false);
#endif
                    ThrowIfDisposed();
                    _scanner.CompleteRefill(bytesRead);
                    status = _scanner.Scan();
                } while (status == ImfHeaderScanner.ScanStatus.NeedMoreInput);

                return CompleteField(status);
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
        /// Gets the scanner holding the current field, for a view of that field.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on from the field the view was taken for.</exception>
        internal ImfHeaderScanner GetScanner(int fieldVersion)
        {
            ThrowIfDisposed();
            if (!_hasCurrentField || fieldVersion != _fieldVersion)
                throw new InvalidOperationException("The reader has moved on from the header field of this view.");

            return _scanner;
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
        /// Checks that the reader can continue, and moves on from the current field.
        /// </summary>
        /// <exception cref="InvalidOperationException">An earlier read found an error that ends reading, or was canceled.</exception>
        private void StartField()
        {
            if (_scanner.IsFaulted)
                throw new InvalidOperationException("The reader cannot continue after a line break, line length or limit error, or the end of the data inside a field.");
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
                throw new InvalidOperationException("The reader cannot continue after a read was canceled.");
#endif

            // Any view of the previous field is now out of date.
            unchecked
            {
                _fieldVersion++;
            }

            _hasCurrentField = false;
        }

        /// <summary>
        /// Parses the field found by the scanner, if any, and makes it current.
        /// </summary>
        private ImfHeaderFieldView? CompleteField(ImfHeaderScanner.ScanStatus status)
        {
            if (status != ImfHeaderScanner.ScanStatus.FieldRead) return null;

            _scanner.ParseField();
            _hasCurrentField = true;
            return new ImfHeaderFieldView(this, _fieldVersion);
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

            _scanner.ReturnBuffers();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static FileStream OpenFile(string path)
        {
            if (path is null)
                throw new ArgumentNullException("messageFilePath");

            return ImfFileStreams.OpenRead(path);
        }
#endif

        private static Stream ValidateStream(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException("messageStream");
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "messageStream");

            return stream;
        }

        /// <summary>
        /// Checks the options, and resolves the encoding for a field body that is not valid UTF-8.
        /// </summary>
        /// <returns>The encoding, or UTF-8 if the options don't accept such a field, in which case it is never used.</returns>
        private static Encoding GetFallbackEncoding(ImfOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.MaxHeaderFieldCount < 1)
                throw new ArgumentException("The maximum header field count must be at least 1.", nameof(options));
            if (options.MaxHeaderSectionLength < 1)
                throw new ArgumentException("The maximum header section length must be at least 1.", nameof(options));
            if (!options.AllowEightBitHeaders) return Encoding.UTF8;

            if (options.CharsetResolver is null)
                throw new ArgumentException("A character set resolver is required to allow 8-bit headers.", nameof(options));
            if (options.HeaderFallbackCharset == default(MimeCharset))
                throw new ArgumentException("A fallback character set is required to allow 8-bit headers.", nameof(options));

            if (!options.CharsetResolver.TryGetEncoding(options.HeaderFallbackCharset, out var encoding))
                throw new ArgumentException($"The fallback character set '{options.HeaderFallbackCharset.Name}' could not be resolved.", nameof(options));

            return encoding;
        }
    }
}
