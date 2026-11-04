using System;
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
    /// Represents a reader of an mbox file (RFC 4155): many Internet messages in one file, each after a separator line.  Messages are read one at a time, so a mailbox of any size can be
    /// processed without loading it whole.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Variants.</b>  The variants of the format can't be told apart reliably from what a file holds, so a caller says which one a file uses, and the default is
    /// <see cref="MboxVariant.Mboxrd"/>.  With <see cref="MboxVariant.Mboxcl"/> and <see cref="MboxVariant.Mboxcl2"/>, the Content-Length field finds the end of each message.
    /// </para>
    /// <para>
    /// <b>Line breaks.</b>  RFC 4155 Appendix A requires LF, so a message in a file uses that, which is the format's own rule and doesn't depend on
    /// <see cref="ImfOptions.AcceptBareLineFeed"/>.  With <see cref="ImfOptions.AllowMboxCarriageReturnLineFeed"/>, a file whose first separator line ends in CRLF is read with CRLF line breaks
    /// throughout.
    /// </para>
    /// <para>
    /// <b>Messages.</b>  A message ends at the empty line before the next separator line or the end of the file, and that empty line is not part of it.  Each is read as an
    /// <see cref="ImfReader"/> reads a message, under the same options, so the limits on a message apply to each.  An <see cref="ImfFileException"/> from a message that the file holds
    /// correctly ends only that message: the next read goes on to the next one.
    /// </para>
    /// <para>
    /// <b>The format.</b>  A first line that isn't a separator line, a message not ended by an empty line, a Content-Length field that is missing or
    /// wrong, a CRLF line break and an octet above 127 each cause an <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowMissingMboxSeparator"/>,
    /// <see cref="ImfOptions.AllowInvalidMboxSeparator"/>, <see cref="ImfOptions.AllowMissingMboxBlankLine"/>, <see cref="ImfOptions.AllowInvalidMboxContentLength"/>,
    /// <see cref="ImfOptions.AllowMboxCarriageReturnLineFeed"/> or <see cref="ImfOptions.AllowEightBitMboxContent"/> is set.  Reading then ends, and later reads throw
    /// <see cref="InvalidOperationException"/>.  RFC 4155 Appendix A prescribes no way to quote a line of a message that starts <c>From </c>, so a later line that isn't a full separator line is
    /// part of the message, with an <see cref="ImfDiagnostic"/>.
    /// </para>
    /// <para>
    /// <b>Line length.</b>  RFC 4155 Appendix A requires each message to follow the formatting rules of RFC 2822, so RFC 5322 §2.1.1 limits each line in the file to 998 octets.  With
    /// <see cref="MboxVariant.Mboxrd"/>, a line whose quote takes it past that, though the line of the message fits once the quote is off, is treated as any long line of the message is: it causes an
    /// <see cref="ImfFileException"/> for that message once it has been read, unless <see cref="ImfOptions.AllowLongLines"/> is set, or the message has a <c>binary</c> entity, which has no lines.
    /// </para>
    /// <para>
    /// Mailbox locking and changing a mailbox in place are not supported.  Only one read may be in progress at a time, and a read started while another is in progress throws
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class MboxReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int MessageBufferSize = 16 * 1024;
        private const int DiscardBufferSize = 4 * 1024;

        private readonly ImfOptions _options;
        private readonly bool _ownsStream;
        private readonly ReadOnlyCollection<ImfDiagnostic> _readOnlyDiagnostics;
        private readonly MboxScanner _scanner;
        private readonly Stream _stream;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private byte[]? _discardBuffer;
        private ImfEntityReader? _entityReader;
#else
        private byte[] _discardBuffer;
        private ImfEntityReader _entityReader;
#endif
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif

        // These are changed with Interlocked, so that a Dispose from another thread while a read is in progress returns the buffers once only, after
        // that read has finished with them.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a reader of an mbox file in the <see cref="MboxVariant.Mboxrd"/> variant, at the current position of a stream.
        /// </summary>
        /// <param name="mailboxStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mailboxStream"/> does not support reading.</exception>
        public MboxReader(Stream mailboxStream) : this(mailboxStream, MboxVariant.Mboxrd, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader of an mbox file, at the current position of a stream.
        /// </summary>
        /// <param name="mailboxStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="variant">The variant of the format.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="variant"/> is not a variant.</exception>
        /// <exception cref="ArgumentException"><paramref name="mailboxStream"/> does not support reading.
        /// -or-
        /// <paramref name="options"/> has a limit that is less than 1, no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public MboxReader(Stream mailboxStream, MboxVariant variant, ImfOptions options)
            : this(CheckArguments(variant, options), ValidateStream(mailboxStream), false, variant)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader of an mbox file in the <see cref="MboxVariant.Mboxrd"/> variant.
        /// </summary>
        /// <param name="mailboxFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxFilePath"/> is <c>null</c>.</exception>
        public MboxReader(string mailboxFilePath) : this(mailboxFilePath, MboxVariant.Mboxrd, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader of an mbox file.
        /// </summary>
        /// <param name="mailboxFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is disposed, even if nothing has been read.</param>
        /// <param name="variant">The variant of the format.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="variant"/> is not a variant.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1, no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public MboxReader(string mailboxFilePath, MboxVariant variant, ImfOptions options)
            : this(CheckArguments(variant, options), OpenFile(mailboxFilePath), true, variant)
        {
        }

        private static FileStream OpenFile(string path)
        {
            if (path is null)
                throw new ArgumentNullException("mailboxFilePath");

            return ImfFileStreams.OpenRead(path);
        }
#endif

        // The options come first, so that bad options are found before a file is opened.
        private MboxReader(ImfOptions options, Stream stream, bool ownsStream, MboxVariant variant)
        {
            _options = options;
            _stream = stream;
            _ownsStream = ownsStream;
            _scanner = new MboxScanner(variant, options);
            _readOnlyDiagnostics = new ReadOnlyCollection<ImfDiagnostic>(_scanner.Diagnostics);
        }

        /// <summary>
        /// Gets the number of bytes of the file read so far.
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
        /// Gets the problems with the format of the file found so far in the current message, and before the first, that did not stop reading.  The list is read-only, and starts again with
        /// each message, so that a mailbox of any size takes the same memory.  The problems in the message itself are in <see cref="MboxEntry.Diagnostics"/>, or the
        /// <see cref="ImfEntityReader.Diagnostics"/> of the reader that <see cref="ReadEntityEvents"/> gave.
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
        /// Gets the separator line of the current message, or <c>null</c> before the first.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public MboxEnvelope? Envelope
#else
        public MboxEnvelope Envelope
#endif
        {
            get
            {
                ThrowIfDisposed();
                return _scanner.Envelope;
            }
        }

        /// <summary>
        /// Gets the number of messages found so far, including the current one.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long MessagesRead
        {
            get
            {
                ThrowIfDisposed();
                return _scanner.MessagesFound;
            }
        }

        /// <summary>
        /// Releases the reader's buffers, the reader that <see cref="ReadEntityEvents"/> last gave, and the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers and file are released once it has finished with them.
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
        /// Releases the reader's buffers, the reader that <see cref="ReadEntityEvents"/> last gave, and the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers
        /// and file are released once it has finished with them.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
#endif

        /// <summary>
        /// Reads the next message, skipping what is left of the current one.
        /// </summary>
        /// <returns>The message with its separator line, or <c>null</c> at the end of the file.</returns>
        /// <exception cref="ImfFileException">The message or the file breaks a rule of its format that the options don't accept, or passes a limit.  Its
        /// <see cref="ImfFileException.DataItemName.MessageIndex"/> item says which message.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public MboxEntry? ReadMessage()
#else
        public MboxEntry ReadMessage()
#endif
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                if (!MoveToMessage()) return null;

                using (var reader = new ImfReader(new MboxMessageStream(this, false), _options, MessageBufferSize, !_scanner.IsCrLf))
                {
                    var entry = BuildEntry(reader.ReadMessage(), reader);
                    ThrowIfDisposed();
                    return entry;
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

        /// <summary>
        /// Moves to the next message, skipping what is left of the current one, and gives a reader of its events, which never holds a whole body in memory.
        /// </summary>
        /// <remarks>
        /// <see cref="Envelope"/> then gives the separator line.  The reader that this gives is disposed by the next read of this object.  Each read of its events is a read of this object, so
        /// only one may be in progress at a time.  Problems with the format of the file come from reads of its events, in the same way as those of the message.
        /// </remarks>
        /// <returns>A reader of the events of the message, or <c>null</c> at the end of the file.</returns>
        /// <exception cref="ImfFileException">The file breaks a rule of its format that the options don't accept, or passes a limit.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ImfEntityReader? ReadEntityEvents()
#else
        public ImfEntityReader ReadEntityEvents()
#endif
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                return MoveToMessage() ? CreateEntityReader() : null;
            }
            finally
            {
                EndRead();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the next message asynchronously, skipping what is left of the current one.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot continue.</param>
        /// <returns>A task whose result is the message with its separator line, or <c>null</c> at the end of the file.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="ImfFileException">By the task: the message or the file breaks a rule of its format that the options don't accept, or passes a limit.  Its
        /// <see cref="ImfFileException.DataItemName.MessageIndex"/> item says which message.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task<MboxEntry?> ReadMessageAsync(CancellationToken cancellationToken = default)
#else
        public Task<MboxEntry> ReadMessageAsync(CancellationToken cancellationToken = default)
#endif
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            return ReadMessageCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Moves to the next message asynchronously, skipping what is left of the current one, and gives a reader of its events, as <see cref="ReadEntityEvents"/> describes.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot continue.</param>
        /// <returns>A task whose result is a reader of the events of the message, or <c>null</c> at the end of the file.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found an error in the format of the file, or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="ImfFileException">By the task: the file breaks a rule of its format that the options don't accept, or passes a limit.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task<ImfEntityReader?> ReadEntityEventsAsync(CancellationToken cancellationToken = default)
#else
        public Task<ImfEntityReader> ReadEntityEventsAsync(CancellationToken cancellationToken = default)
#endif
        {
            ThrowIfDisposed();
            BeginRead();
            return ReadEntityEventsCoreAsync(cancellationToken);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async Task<MboxEntry?> ReadMessageCoreAsync(CancellationToken cancellationToken)
#else
        private async Task<MboxEntry> ReadMessageCoreAsync(CancellationToken cancellationToken)
#endif
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!await MoveToMessageAsync(cancellationToken).ConfigureAwait(false)) return null;

                using (var reader = new ImfReader(new MboxMessageStream(this, false), _options, MessageBufferSize, !_scanner.IsCrLf))
                {
                    var entry = BuildEntry(await reader.ReadMessageAsync(cancellationToken).ConfigureAwait(false), reader);
                    ThrowIfDisposed();
                    return entry;
                }
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async Task<ImfEntityReader?> ReadEntityEventsCoreAsync(CancellationToken cancellationToken)
#else
        private async Task<ImfEntityReader> ReadEntityEventsCoreAsync(CancellationToken cancellationToken)
#endif
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await MoveToMessageAsync(cancellationToken).ConfigureAwait(false) ? CreateEntityReader() : null;
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

        private async Task<bool> MoveToMessageAsync(CancellationToken cancellationToken)
        {
            StartMove();
            while (!_scanner.IsMessageEnded)
            {
                _discardBuffer = _discardBuffer ?? new byte[DiscardBufferSize];
                await ReadContentCoreAsync(_discardBuffer, cancellationToken).ConfigureAwait(false);
            }

            while (true)
            {
                var status = _scanner.MoveToMessage();
                if (status != MboxScanner.Status.NeedMoreInput) return status == MboxScanner.Status.MessageFound;

                await RefillAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets the next bytes of the current message asynchronously, for its stream.
        /// </summary>
        internal async Task<int> ReadContentAsync(Memory<byte> destination, bool isOwnOperation, CancellationToken cancellationToken)
        {
            if (isOwnOperation) BeginRead();
            try
            {
                return await ReadContentCoreAsync(destination, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                if (isOwnOperation) EndRead();
            }
        }

        private async Task<int> ReadContentCoreAsync(Memory<byte> destination, CancellationToken cancellationToken)
        {
            ThrowIfFaulted();
            while (true)
            {
                var status = _scanner.Read(destination.Span, out var written);
                if (status != MboxScanner.Status.NeedMoreInput) return written;

                await RefillAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task RefillAsync(CancellationToken cancellationToken)
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
#endif

        /// <summary>
        /// Gets the next bytes of the current message, for its stream.
        /// </summary>
        internal int ReadContent(Memory<byte> destination, bool isOwnOperation)
        {
            if (isOwnOperation) BeginRead();
            try
            {
                return ReadContentCore(destination.Span);
            }
            finally
            {
                if (isOwnOperation) EndRead();
            }
        }

        private int ReadContentCore(Span<byte> destination)
        {
            ThrowIfFaulted();
            while (true)
            {
                var status = _scanner.Read(destination, out var written);
                if (status != MboxScanner.Status.NeedMoreInput) return written;

                Refill();
            }
        }

        private void Refill()
        {
            int offset, count;
            _scanner.PrepareRefill(out offset, out count);
            var bytesRead = _stream.Read(_scanner.Buffer, offset, count);
            ThrowIfDisposed();
            _scanner.CompleteRefill(bytesRead);
        }

        private bool MoveToMessage()
        {
            StartMove();
            while (!_scanner.IsMessageEnded)
            {
                _discardBuffer = _discardBuffer ?? new byte[DiscardBufferSize];
                ReadContentCore(_discardBuffer);
            }

            while (true)
            {
                var status = _scanner.MoveToMessage();
                if (status != MboxScanner.Status.NeedMoreInput) return status == MboxScanner.Status.MessageFound;

                Refill();
            }
        }

        /// <summary>
        /// Checks that the reader can continue, and ends the reader of the events of the current message.
        /// </summary>
        /// <exception cref="InvalidOperationException">An earlier read found an error in the format of the file, or was canceled.</exception>
        private void StartMove()
        {
            ThrowIfFaulted();
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
                throw new InvalidOperationException("The reader cannot continue after a read was canceled.");
#endif

            _entityReader?.Dispose();
            _entityReader = null;
        }

        private void ThrowIfFaulted()
        {
            if (_scanner.IsFaulted)
                throw new InvalidOperationException("The reader cannot continue after an error in the format of the file.");
        }

        private ImfEntityReader CreateEntityReader()
        {
            var reader = new ImfEntityReader(new MboxMessageStream(this, true), _options, MessageBufferSize, !_scanner.IsCrLf)
            {
                HasLongLineOutsideTheData = () => _scanner.HasLongQuotedLine,
                MessageIndex = _scanner.MessageIndex
            };
            _entityReader = reader;
            return reader;
        }

        private MboxEntry BuildEntry(ImfMessage message, ImfReader reader)
        {
            var diagnostics = new List<ImfDiagnostic>(_scanner.Diagnostics);
            diagnostics.AddRange(reader.Diagnostics);
            if (_scanner.HasLongQuotedLine && !ImfConformance.HasBinaryEntity(message)) AddLongQuotedLine(diagnostics);

            return new MboxEntry(_scanner.Envelope ?? throw new InvalidOperationException("There is no current message."), message, diagnostics);
        }

        private void AddLongQuotedLine(List<ImfDiagnostic> diagnostics)
        {
            var diagnostic = new ImfDiagnostic(ImfDiagnosticKind.LongBodyLine, ImfBodyCheck.LongQuotedLineMessage, _scanner.LongQuotedLineBytePosition, string.Empty, string.Empty, _scanner.LongQuotedLineNumber);
            if (!_options.AllowLongLines)
                throw ImfConformance.BuildException(diagnostic, true);

            diagnostics.Add(diagnostic);
        }

        // Used as an exception filter that never catches, so that the exception keeps its stack trace.
        private bool AddMessageIndex(ImfFileException exception)
        {
            // The limit on the number of messages has already said which one it refused, which isn't the current one.
            if (!exception.Data.Contains(ImfFileException.DataItemName.MessageIndex))
                exception.Data[ImfFileException.DataItemName.MessageIndex] = _scanner.MessageIndex;

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
        /// Releases the buffers, and the file the reader opened itself, the first time it is called.
        /// </summary>
        private void Release()
        {
            if (Interlocked.Exchange(ref _isReleased, 1) != 0) return;

            _entityReader?.Dispose();
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

        private static Stream ValidateStream(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException("mailboxStream");
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "mailboxStream");

            return stream;
        }

        private static ImfOptions CheckArguments(MboxVariant variant, ImfOptions options)
        {
            CheckVariant(variant, nameof(variant));
            ImfReader.CheckOptions(options);
            if (options.MaxMessageCount < 1)
                throw new ArgumentException("The maximum message count must be at least 1.", nameof(options));

            return options;
        }

        internal static void CheckVariant(MboxVariant variant, string parameterName)
        {
            if (variant < MboxVariant.Mboxrd || variant > MboxVariant.Mboxcl2)
                throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}
