using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// Represents a reader of a whole Internet message into an <see cref="ImfMessage"/>, which holds it in memory.  To read a message without holding it, use an
    /// <see cref="ImfEntityReader"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Header.</b>  The fields are read as an <see cref="ImfHeaderReader"/> reads them, under the same options, and kept with their raw bytes.  The obsolete syntax of RFC 5322
    /// §4 is accepted.
    /// </para>
    /// <para>
    /// <b>Conformance.</b>  Once the header has been read, it is checked as <see cref="ImfMessage.Validate"/> describes.  A field body that breaks its grammar causes an
    /// <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowInvalidValues"/> is set, and then the problem is recorded in <see cref="Diagnostics"/> instead.  A field that RFC 5322 §3.6
    /// requires being missing, and a second field of a kind that may appear only once, are always recorded in <see cref="Diagnostics"/>.  §3.5 lets a message follow <c>obs-fields</c>, which §4.5
    /// defines with no minimum or maximum for any field, and §4 says that a receiver MUST accept the obsolete syntax.
    /// </para>
    /// <para>
    /// <b>Body.</b>  The rest of the data is the body, up to <see cref="ImfOptions.MaxEntitySize"/>, which is held in memory as it is.  Its structure is read into
    /// <see cref="ImfMessage.Body"/> as an <see cref="ImfEntityReader"/> reads it, under the same options, so a multipart entity that breaks RFC 2046 §5.1 causes an
    /// <see cref="ImfFileException"/>.  The bytes of a body are decoded, and checked against the transfer encoding, only when they are asked for.  A message inside a
    /// <c>message/rfc822</c> entity must have one of From, Subject or Date, as RFC 2046 §5.2.1 says, unless <see cref="ImfOptions.AllowMissingRequiredFields"/> is set.  Call
    /// <see cref="ImfMessage.Validate"/> on it to check it against RFC 5322.
    /// </para>
    /// <para>
    /// The data holds one message, so it can be read only once, and any <see cref="ImfFileException"/> ends reading.  Later reads throw <see cref="InvalidOperationException"/>.  A
    /// read started while another is in progress does too.
    /// </para>
    /// </remarks>
    public sealed class ImfReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int DefaultBufferSize = 16 * 1024;
        private const string EntityTooLargeTemplate = "The body is longer than the maximum of {0} octets.";

        private static readonly byte[] NoBytes = new byte[0];

        private readonly ReadOnlyCollection<ImfDiagnostic> _readOnlyDiagnostics;
        private readonly List<ImfDiagnostic> _diagnostics = new List<ImfDiagnostic>();
        private readonly ImfOptions _options;
        private readonly bool _lineFeedIsLineBreak;
        private readonly bool _ownsStream;
        private readonly ImfHeaderScanner _scanner;
        private readonly Stream _stream;

        private byte[] _body = NoBytes;
        private int _bodyLength;
        private bool _hasEndedHeader;
        private bool _hasStarted;
        private readonly ImfMessage _message;

        // These are changed with Interlocked, so that a Dispose from another thread while a read is in progress returns the buffers once only, after
        // that read has finished with them.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a reader for the message at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.</exception>
        public ImfReader(Stream messageStream) : this(messageStream, new ImfOptions())
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
        public ImfReader(Stream messageStream, ImfOptions options) : this(messageStream, options, DefaultBufferSize, false)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for the message in a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public ImfReader(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the message in a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1, no
        /// <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public ImfReader(string messageFilePath, ImfOptions options)
            : this(CheckOptions(options), ImfHeaderReader.OpenFile(messageFilePath), true, options, DefaultBufferSize, false)
        {
        }
#endif

        /// <summary>
        /// Sets what gives, once the message has ended, the lines of it that are too long in a form that this reader doesn't see.
        /// </summary>
        [AllowNull]
        internal Func<List<ImfLongLine>> LongLinesOutsideTheData { private get; set; }

        /// <summary>
        /// Creates a reader with a specific size for the internal block and a line break setting.  Allows tests to force refills at chosen points,
        /// and the readers of container formats to read the LF line breaks of their messages.
        /// </summary>
        internal ImfReader(Stream messageStream, ImfOptions options, int bufferSize, bool lineFeedIsLineBreak)
            : this(CheckOptions(options), ImfHeaderReader.ValidateStream(messageStream), false, options, bufferSize, lineFeedIsLineBreak)
        {
        }

        // The encoding comes first, so that bad options are found before a file is opened.
        private ImfReader(Encoding fallbackEncoding, Stream stream, bool ownsStream, ImfOptions options, int bufferSize, bool lineFeedIsLineBreak)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _stream = stream;
            _ownsStream = ownsStream;
            _options = options;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
            _scanner = new ImfHeaderScanner(bufferSize, options, fallbackEncoding, lineFeedIsLineBreak);
            _message = new ImfMessage(options);
            _readOnlyDiagnostics = new ReadOnlyCollection<ImfDiagnostic>(_diagnostics);
        }

        /// <summary>
        /// Gets the problems found so far that did not stop reading, as the options allow.  The list is read-only, and grows as the reader finds more.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public IReadOnlyList<ImfDiagnostic> Diagnostics
        {
            get
            {
                ThrowIfDisposed();
                return _readOnlyDiagnostics;
            }
        }

        /// <summary>
        /// Gets the number of bytes of the data read into the message so far.
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
        /// Releases the reader's buffers, and the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers and
        /// file are released once it has finished with them.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

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
        /// Reads the message.
        /// </summary>
        /// <returns>The message.</returns>
        /// <exception cref="ImfFileException">The message breaks a rule of its format that the options don't accept, or passes a limit.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// The message has already been read, or an earlier read failed or was canceled.</exception>
        public ImfMessage ReadMessage()
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                Start();
                while (!Advance())
                {
                    int offset, count;
                    _scanner.PrepareRefill(out offset, out count);
                    var bytesRead = _stream.Read(_scanner.Buffer, offset, count);
                    ThrowIfDisposed();
                    _scanner.CompleteRefill(bytesRead);
                }

                ThrowIfDisposed();
                return Complete();
            }
            finally
            {
                EndRead();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the message asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot continue.</param>
        /// <returns>A task whose result is the message.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// The message has already been read, or an earlier read failed or was canceled.</exception>
        /// <exception cref="ImfFileException">By the task: the message breaks a rule of its format that the options don't accept, or passes a limit.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public Task<ImfMessage> ReadMessageAsync(CancellationToken cancellationToken = default)
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            var isPending = false;
            try
            {
                Start();
                isPending = true;
                return ReadMessageCoreAsync(cancellationToken);
            }
            finally
            {
                if (!isPending)
                {
                    EndRead();
                }
            }
        }

        private async Task<ImfMessage> ReadMessageCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                while (!Advance())
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
                }

                ThrowIfDisposed();
                return Complete();
            }
            finally
            {
                EndRead();
            }
        }
#endif

        /// <summary>
        /// Takes in what the block holds.
        /// </summary>
        /// <returns><c>true</c> if the whole message has been read; <c>false</c> if the driver must refill the block.</returns>
        private bool Advance()
        {
            if (!_hasEndedHeader)
            {
                ImfHeaderScanner.ScanStatus status;
                while ((status = _scanner.Scan()) != ImfHeaderScanner.ScanStatus.EndOfHeader && status != ImfHeaderScanner.ScanStatus.EndOfInput)
                {
                    if (status == ImfHeaderScanner.ScanStatus.NeedMoreInput) return false;

                    _scanner.ParseField();
                    _message.Fields.Add(_scanner.ToField());
                }

                EndHeader();
            }

            var count = _scanner.BufferEnd - _scanner.Position;
            if (count > 0)
            {
                if (count > _options.MaxEntitySize - _bodyLength)
                    throw _scanner.BuildPositionException(string.Format(EntityTooLargeTemplate, _options.MaxEntitySize));

                if (count > _body.Length - _bodyLength)
                {
                    // Doubling keeps the copying linear in the size of the body, and the limit caps what hostile input can make it allocate.
                    Array.Resize(ref _body, (int)Math.Min(Math.Max(_body.Length * 2L, (long)_bodyLength + count), _options.MaxEntitySize));
                }

                Array.Copy(_scanner.Buffer, _scanner.Position, _body, _bodyLength, count);
                _bodyLength += count;
                _scanner.ConsumeBody(_scanner.BufferEnd);
            }

            return _scanner.IsEndOfInput;
        }

        /// <summary>
        /// Records how the header ended, and checks it, so that a message that is refused has its body left unread.
        /// </summary>
        private void EndHeader()
        {
            _hasEndedHeader = true;

            var lineBreakLength = _scanner.SectionEndLineBreakLength;
            var headerEnd = lineBreakLength == 0 ? NoBytes : new byte[lineBreakLength];
            Array.Copy(_scanner.Buffer, _scanner.Position - lineBreakLength, headerEnd, 0, lineBreakLength);

            _message.HeaderEnd = headerEnd;
            _message.HeaderEndBytePosition = _scanner.BytePosition;
            _message.HeaderEndLineNumber = _scanner.PhysicalLinesRead + 1;

            foreach (var diagnostic in ImfConformance.Check(_message, _options))
            {
                if (!ImfConformance.IsAllowedInHeaderWhenReading(diagnostic, _options))
                    throw ImfConformance.BuildException(diagnostic, true);

                _diagnostics.Add(diagnostic);
            }
        }

        /// <summary>
        /// Records problems that a format found in what was read, as another read can't be in progress after the one read of the message.
        /// </summary>
        internal void AddDiagnostics(IEnumerable<ImfDiagnostic> diagnostics)
        {
            _diagnostics.AddRange(diagnostics);
        }

        private ImfMessage Complete()
        {
            _message.RawBody = new ReadOnlyMemory<byte>(_body, 0, _bodyLength);
            _message.LineFeedIsLineBreak = _lineFeedIsLineBreak;
            _message.LongLinesOutsideTheData = LongLinesOutsideTheData;
            _message.ReadBody(_diagnostics);

            var embedded = new List<ImfDiagnostic>();
            ImfConformance.CheckEmbedded(_message, embedded);
            foreach (var diagnostic in embedded)
            {
                if (!ImfConformance.IsAllowed(diagnostic.Kind, _options))
                    throw ImfConformance.BuildException(diagnostic, true);

                _diagnostics.Add(diagnostic);
            }

            return _message;
        }

        /// <summary>
        /// Marks the one read of the message as started.
        /// </summary>
        /// <exception cref="InvalidOperationException">It has already been started.</exception>
        private void Start()
        {
            if (_hasStarted)
                throw new InvalidOperationException("The message has already been read, or an earlier read failed or was canceled.");

            _hasStarted = true;
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

        /// <summary>
        /// Checks the options, and resolves the encoding for a field body that is not valid UTF-8.
        /// </summary>
        internal static Encoding CheckOptions(ImfOptions options)
        {
            var encoding = ImfHeaderReader.GetFallbackEncoding(options);
            if (options.MaxEntitySize < 1)
                throw new ArgumentException("The maximum entity size must be at least 1.", nameof(options));
            if (options.MaxNestingDepth < 1)
                throw new ArgumentException("The maximum nesting depth must be at least 1.", nameof(options));
            if (options.MaxPartCount < 1)
                throw new ArgumentException("The maximum part count must be at least 1.", nameof(options));
            if (options.CharsetResolver is null)
                throw new ArgumentException("A character set resolver is required to read a message.", nameof(options));

            return encoding;
        }
    }
}
