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
    /// Represents a writer of mbox files (RFC 4155): many Internet messages in one file, each after a separator line and ended by an empty line.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Messages.</b>  Each message is checked and written as an <see cref="ImfWriter"/> does, under the same options, so a message that it refuses leaves nothing in the output, and writing
    /// can continue with another.  A message read from a file with the same line breaks stays byte for byte as it was, apart from what the variant adds.
    /// </para>
    /// <para>
    /// <b>Variants.</b>  With <see cref="MboxVariant.Mboxrd"/> and <see cref="MboxVariant.Mboxcl2"/>, a message reads back exactly.  With <see cref="MboxVariant.Mboxo"/> and
    /// <see cref="MboxVariant.Mboxcl"/>, the quotes added to lines starting <c>From </c> stay when it is read.  With <see cref="MboxVariant.Mboxcl"/> and <see cref="MboxVariant.Mboxcl2"/>, the
    /// Content-Length field is set to the length of the body as the file holds it, replacing any that is wrong, and a header section with no empty line gets one.  The message itself isn't
    /// changed.  Other variants leave any Content-Length field as it is.
    /// </para>
    /// <para>
    /// <b>Line breaks.</b>  A message in a file uses the line break of the file, which is the LF that RFC 4155 Appendix A requires, unless <see cref="MboxLineBreak.CarriageReturnLineFeed"/> is
    /// given.  So one read from an <c>.eml</c> file changes when written here.  A line that still ends in CRLF in a file of LF, from a file read with
    /// <see cref="ImfOptions.AllowMboxCarriageReturnLineFeed"/>, causes an <see cref="ImfFileException"/> unless that option is set.  A message that has to change this way can't have a <c>binary</c> entity (RFC 2045 §2.9) or an extension mechanism (§6.3), as a line break in it may not be
    /// one, which causes an <see cref="ImfFileException"/>.
    /// </para>
    /// <para>
    /// <b>Seven bits.</b>  RFC 4155 Appendix A requires seven-bit characters, so an octet above 127 causes an <see cref="ImfFileException"/>, unless
    /// <see cref="ImfOptions.AllowEightBitMboxContent"/> is set.  A body written by this package gets a transfer encoding that keeps to seven bits by default.
    /// </para>
    /// <para>
    /// <b>Line length.</b>  RFC 4155 Appendix A requires each message to follow the formatting rules of RFC 2822, so RFC 5322 §2.1.1 limits each line in the file to 998 octets.  A quote that takes
    /// a line past that causes an <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowLongLines"/> is set, or it is in a body that has no line limit, such as a <c>binary</c> one.  A
    /// variant that adds no quotes, such as <see cref="MboxVariant.Mboxcl2"/>, can write the line.
    /// </para>
    /// <para>
    /// <b>Last line.</b>  The empty line that ends a message has to be a line of its own, so a message whose last line has no line break gets one.  With
    /// <see cref="MboxVariant.Mboxcl"/> and <see cref="MboxVariant.Mboxcl2"/>, the Content-Length field doesn't count it, so the message reads back without it.  With the others, it reads back
    /// with it.
    /// </para>
    /// <para>
    /// A stream that already holds an mbox file can be appended to by positioning it at the end, and a file-path constructor overwrites the file.  Mailbox locking and changing a mailbox in
    /// place are not supported.  Only one operation may be in progress at a time.  After a write fails once output has started, the file is incomplete, so the writer cannot continue, and the
    /// same goes after any write is canceled.
    /// </para>
    /// </remarks>
    public sealed class MboxWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const string CarriageReturnLineFeedMessage = "A line ends in a carriage return and a line feed, which RFC 4155 Appendix A doesn't allow.  ImfOptions.AllowMboxCarriageReturnLineFeed accepts it.";
        private const string EightBitMessage = "The message holds an octet above 127, which RFC 4155 Appendix A doesn't allow.  ImfOptions.AllowEightBitMboxContent accepts it.";
        private const string InvalidSeparatorMessage = "The separator line doesn't have the form that RFC 4155 Appendix A requires.  ImfOptions.AllowInvalidMboxSeparator accepts it.";

        private const int QuotedLineScanBufferSize = 16 * 1024;

        private const byte CarriageReturn = 0x0D;
        private const byte HighestSevenBitOctet = 0x7F;
        private const byte LineFeed = 0x0A;

        private readonly Func<string> _boundaryGenerator;
        private readonly List<ImfDiagnostic> _diagnostics = new List<ImfDiagnostic>();
        private readonly bool _isCrLf;
        private readonly ImfOptions _options;
        private readonly ImfOutput _output;
        private readonly ReadOnlyCollection<ImfDiagnostic> _readOnlyDiagnostics;
        private readonly MboxVariant _variant;

        private long _fieldsWritten;
        private long _messagesWritten;

        /// <summary>
        /// Creates a writer of an mbox file in the <see cref="MboxVariant.Mboxrd"/> variant, at the current position of a stream.
        /// </summary>
        /// <param name="mailboxStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="mailboxStream"/> does not support writing.</exception>
        public MboxWriter(Stream mailboxStream) : this(mailboxStream, MboxVariant.Mboxrd, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of an mbox file with LF line breaks, at the current position of a stream.
        /// </summary>
        /// <param name="mailboxStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="variant">The variant of the format.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="variant"/> is not a variant.</exception>
        /// <exception cref="ArgumentException"><paramref name="mailboxStream"/> does not support writing.
        /// -or-
        /// <paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public MboxWriter(Stream mailboxStream, MboxVariant variant, ImfOptions options) : this(mailboxStream, variant, options, MboxLineBreak.LineFeed)
        {
        }

        /// <summary>
        /// Creates a writer of an mbox file, at the current position of a stream.
        /// </summary>
        /// <param name="mailboxStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="variant">The variant of the format.</param>
        /// <param name="options">The options.</param>
        /// <param name="lineBreak">The line breaks of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="variant"/> is not a variant, or <paramref name="lineBreak"/> is not a line break.</exception>
        /// <exception cref="ArgumentException"><paramref name="mailboxStream"/> does not support writing.
        /// -or-
        /// <paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public MboxWriter(Stream mailboxStream, MboxVariant variant, ImfOptions options, MboxLineBreak lineBreak)
            : this(CheckArguments(variant, options, lineBreak), ValidateStream(mailboxStream), false, variant, lineBreak, ImfEntityWriter.GenerateBoundary)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of an mbox file in the <see cref="MboxVariant.Mboxrd"/> variant.  An existing file is overwritten.
        /// </summary>
        /// <param name="mailboxFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxFilePath"/> is <c>null</c>.</exception>
        public MboxWriter(string mailboxFilePath) : this(mailboxFilePath, MboxVariant.Mboxrd, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of an mbox file with LF line breaks.  An existing file is overwritten.
        /// </summary>
        /// <param name="mailboxFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is disposed.</param>
        /// <param name="variant">The variant of the format.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="mailboxFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="variant"/> is not a variant.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and
        /// can't be resolved.</exception>
        public MboxWriter(string mailboxFilePath, MboxVariant variant, ImfOptions options)
            : this(CheckArguments(variant, options, MboxLineBreak.LineFeed), OpenFile(mailboxFilePath), true, variant, MboxLineBreak.LineFeed, ImfEntityWriter.GenerateBoundary)
        {
        }

        private static FileStream OpenFile(string path)
        {
            if (path is null)
                throw new ArgumentNullException("mailboxFilePath");

            return ImfFileStreams.OpenWrite(path, ImfOutput.FileBufferSize);
        }
#endif

        /// <summary>
        /// Creates a writer with a source of boundaries.  Allows tests to choose the boundaries.
        /// </summary>
        internal MboxWriter(Stream mailboxStream, MboxVariant variant, ImfOptions options, MboxLineBreak lineBreak, Func<string> boundaryGenerator)
            : this(CheckArguments(variant, options, lineBreak), ValidateStream(mailboxStream), false, variant, lineBreak, boundaryGenerator)
        {
        }

        // The options come first, so that bad options are found before a file is opened.
        private MboxWriter(ImfOptions options, Stream stream, bool ownsStream, MboxVariant variant, MboxLineBreak lineBreak, Func<string> boundaryGenerator)
        {
            _options = options;
            _variant = variant;
            _isCrLf = lineBreak == MboxLineBreak.CarriageReturnLineFeed;
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
        /// Gets the number of messages written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long MessagesWritten
        {
            get
            {
                _output.ThrowIfDisposed();
                return _messagesWritten;
            }
        }

        /// <summary>
        /// Closes the file the writer opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the file is closed once it has finished with it.
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
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the file
        /// is closed once it has finished with it.
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
        /// Writes a message.
        /// </summary>
        /// <param name="envelope">The separator line.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="envelope"/> or <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The message or separator line can't be written.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void WriteMessage(MboxEnvelope envelope, ImfMessage message)
        {
            _output.ThrowIfDisposed();
            if (envelope is null)
                throw new ArgumentNullException(nameof(envelope));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            _output.BeginOperation();
            try
            {
                WriteOne(envelope, message);
                _output.ThrowIfDisposed();
            }
            finally
            {
                if (_output.EndOperation()) _output.Release();
            }
        }

        /// <summary>
        /// Writes many messages, one after another.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="entries"/> nor <paramref name="progress"/> may call the writer.  Entries are taken one at a time, so an enumerator that
        /// reads them from another mailbox takes the same memory for any number of them.
        /// </remarks>
        /// <param name="entries">The messages.</param>
        /// <param name="progress">Receives the progress after each message has been handed to the stream, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="ImfWriteProgress.EntitiesWritten"/> counts messages.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="entries"/> gave a <c>null</c> entry.  The ones before it have been written.</exception>
        /// <exception cref="ImfFileException">A message or separator line can't be written.  The ones before it have been written, and nothing of it has.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteMessages(IEnumerable<MboxEntry> entries, IProgress<ImfWriteProgress>? progress = null)
#else
        public void WriteMessages(IEnumerable<MboxEntry> entries, IProgress<ImfWriteProgress> progress = null)
#endif
        {
            _output.ThrowIfDisposed();
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            _output.BeginOperation();
            try
            {
                foreach (var entry in entries)
                {
                    _output.ThrowIfDisposed();
                    CheckEntry(entry);
                    WriteOne(entry.Envelope, entry.Message);
                    progress?.Report(GetProgress());
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
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the flush is in progress.</exception>
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
        /// Writes a message asynchronously.
        /// </summary>
        /// <param name="envelope">The separator line.</param>
        /// <param name="message">The message.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The write is canceled only if this token is canceled, and the writer then cannot
        /// continue.</param>
        /// <returns>A task that completes once the whole message has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="envelope"/> or <paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ImfFileException">By the task: the message or separator line can't be written.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the operation failed with an
        /// <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/> was canceled.  The task is canceled.  An
        /// <see cref="OperationCanceledException"/> while <paramref name="cancellationToken"/> isn't canceled faults the task, as any other exception does.</exception>
        public Task WriteMessageAsync(MboxEnvelope envelope, ImfMessage message, CancellationToken cancellationToken = default)
        {
            _output.ThrowIfDisposed();
            if (envelope is null)
                throw new ArgumentNullException(nameof(envelope));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            _output.BeginOperation();
            return AsyncOutcome.Decide(WriteMessageCoreAsync(envelope, message, cancellationToken), cancellationToken, _output);
        }

        /// <summary>
        /// Writes many messages asynchronously, one after another.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="entries"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="entries">The messages.</param>
        /// <param name="progress">Receives the progress after each message has been handed to the stream, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="ImfWriteProgress.EntitiesWritten"/> counts messages.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The write is canceled only if this token is canceled, and the writer then cannot
        /// continue.</param>
        /// <returns>A task that completes once every message has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="entries"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="entries"/> gave a <c>null</c> entry.  The ones before it have been written.</exception>
        /// <exception cref="ImfFileException">By the task: a message or separator line can't be written.  The ones before it have been written, and nothing of it has.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the operation failed with an
        /// <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/> was canceled.  The task is canceled.  An
        /// <see cref="OperationCanceledException"/> while <paramref name="cancellationToken"/> isn't canceled faults the task, as any other exception does.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteMessagesAsync(IEnumerable<MboxEntry> entries, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)
#else
        public Task WriteMessagesAsync(IEnumerable<MboxEntry> entries, IProgress<ImfWriteProgress> progress = null, CancellationToken cancellationToken = default)
#endif
        {
            _output.ThrowIfDisposed();
            if (entries is null)
                throw new ArgumentNullException(nameof(entries));

            _output.BeginOperation();
            return AsyncOutcome.Decide(WriteMessagesCoreAsync(entries, progress, cancellationToken), cancellationToken, _output);
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

        private async Task WriteMessageCoreAsync(MboxEnvelope envelope, ImfMessage message, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteOneAsync(envelope, message, cancellationToken).ConfigureAwait(false);
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

        private async Task WriteMessagesCoreAsync(IEnumerable<MboxEntry> entries,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<ImfWriteProgress>? progress,
#else
            IProgress<ImfWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            try
            {
                // Check here as well, so that a canceled token cancels the call when there are no messages.
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var entry in entries)
                {
                    _output.ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    CheckEntry(entry);
                    await WriteOneAsync(entry.Envelope, entry.Message, cancellationToken).ConfigureAwait(false);
                    progress?.Report(GetProgress());
                }

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

        private async Task WriteOneAsync(MboxEnvelope envelope, ImfMessage message, CancellationToken cancellationToken)
        {
            var bytes = Plan(envelope, message, out var diagnostics, out var fieldCount);
            await _output.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            Complete(diagnostics, fieldCount);
        }
#endif

        private void WriteOne(MboxEnvelope envelope, ImfMessage message)
        {
            var bytes = Plan(envelope, message, out var diagnostics, out var fieldCount);
            _output.Write(bytes);
            Complete(diagnostics, fieldCount);
        }

        private void Complete(List<ImfDiagnostic> diagnostics, long fieldCount)
        {
            _diagnostics.AddRange(diagnostics);
            _fieldsWritten += fieldCount;
            _messagesWritten++;
        }

        /// <summary>
        /// Checks a message and gets the bytes that the file holds for it, so that one that is refused leaves nothing in the output.
        /// </summary>
        /// <exception cref="ImfFileException">The message or separator line can't be written.</exception>
        private byte[] Plan(MboxEnvelope envelope, ImfMessage message, out List<ImfDiagnostic> diagnostics, out long fieldCount)
        {
            try
            {
                diagnostics = new List<ImfDiagnostic>();
                if (!envelope.IsConforming)
                {
                    if (!_options.AllowInvalidMboxSeparator)
                        throw BuildException(InvalidSeparatorMessage, ImfDiagnosticKind.InvalidMboxSeparator);

                    diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.InvalidMboxSeparator, InvalidSeparatorMessage, -1, string.Empty));
                }

                // A message is written in the line breaks it has, so that what hasn't changed stays as it is, and then they are changed all at once if the file has others.
                var output = new MemoryStream();
                var hasLineFeedBreaks = message.LineFeedIsLineBreak;
                using (var writer = new ImfWriter(output, _options, hasLineFeedBreaks, _boundaryGenerator))
                {
                    writer.WriteMessage(message);
                    diagnostics.AddRange(writer.Diagnostics);
                    fieldCount = writer.FieldsWritten;
                }

                var bytes = new ReadOnlySpan<byte>(output.ToArray());
                if (hasLineFeedBreaks == _isCrLf) bytes = ImfLineBreaks.Convert(message, bytes, _isCrLf);

                var formatted = MboxFormatter.Format(envelope, bytes, _variant, _isCrLf, out var fieldChange);
                fieldCount += fieldChange;
                CheckContent(formatted, diagnostics);
                CheckLineLengths(formatted, bytes, diagnostics);
                return formatted;
            }
            catch (ImfFileException exception) when (AddMessageIndex(exception))
            {
                throw;
            }
        }

        private ImfWriteProgress GetProgress()
        {
            return new ImfWriteProgress(_fieldsWritten, _messagesWritten, _output.BytePosition);
        }

        // ref. RFC 4155 Appendix A: seven-bit characters, and a line feed with no carriage return.
        private void CheckContent(ReadOnlySpan<byte> bytes, List<ImfDiagnostic> diagnostics)
        {
            var hasEightBit = false;
            var hasCarriageReturnLineFeed = false;
            for (var index = 0; index < bytes.Length; index++)
            {
                hasEightBit |= bytes[index] > HighestSevenBitOctet;
                hasCarriageReturnLineFeed |= !_isCrLf && bytes[index] == LineFeed && index > 0 && bytes[index - 1] == CarriageReturn;
            }

            if (hasEightBit) Report(ImfDiagnosticKind.EightBitMboxContent, EightBitMessage, diagnostics);
            if (hasCarriageReturnLineFeed) Report(ImfDiagnosticKind.MboxCarriageReturnLineFeed, CarriageReturnLineFeedMessage, diagnostics);
        }

        // ref. RFC 4155 Appendix A: each message follows RFC 2822, whose §2.1.1 limits the lines in the file, so a quote counts.  Most messages have no long line in the file, so they don't need to
        // be read to find what holds it.  A reader of what was written treats each line as this does, and refuses what the message can't be read with, such as a long line of a header.
        private void CheckLineLengths(ReadOnlySpan<byte> formatted, ReadOnlySpan<byte> message, List<ImfDiagnostic> diagnostics)
        {
            if (!HasLongLine(formatted)) return;

            var lines = FindLongQuotedLines(message);
            using (var reader = new ImfEntityReader(new MemoryStream(message.ToArray()), _options, QuotedLineScanBufferSize, !_isCrLf) { LongLinesOutsideTheData = () => lines })
            {
                try
                {
                    while (!(reader.ReadEvent() is null))
                    {
                    }
                }
                catch (ImfFileException exception)
                {
                    Report(ImfDiagnosticKind.LongBodyLine, exception.Message, diagnostics);
                    return;
                }

                foreach (var diagnostic in reader.Diagnostics)
                {
                    if (diagnostic.Message == ImfBodyCheck.LongQuotedLineMessage) diagnostics.Add(new ImfDiagnostic(diagnostic.Kind, diagnostic.Message, -1, diagnostic.EntityPath));
                }
            }
        }

        // ImfWriter has looked at the lines of each body as they are, so only those that a quote takes past the limit are left.
        private List<ImfLongLine> FindLongQuotedLines(ReadOnlySpan<byte> message)
        {
            var lines = new List<ImfLongLine>();
            if (_variant == MboxVariant.Mboxcl2) return lines;

            var offset = 0;
            while (offset < message.Length)
            {
                var rest = message.Slice(offset);
                var lineFeed = rest.IndexOf(LineFeed);
                var next = lineFeed < 0 ? rest.Length : lineFeed + 1;
                var length = lineFeed < 0 ? rest.Length : lineFeed > 0 && rest[lineFeed - 1] == CarriageReturn ? lineFeed - 1 : lineFeed;
                if (length == ImfBodyCheck.MaxLineLength && MboxFormatter.NeedsQuote(rest.Slice(0, length), _variant)) lines.Add(new ImfLongLine(offset, -1, 0));

                offset += next;
            }

            return lines;
        }

        private static bool HasLongLine(ReadOnlySpan<byte> bytes)
        {
            // The separator line isn't a line of the message.
            var rest = bytes.Slice(bytes.IndexOf(LineFeed) + 1);
            while (rest.Length > ImfBodyCheck.MaxLineLength)
            {
                var lineFeed = rest.IndexOf(LineFeed);
                var length = lineFeed < 0 ? rest.Length : lineFeed > 0 && rest[lineFeed - 1] == CarriageReturn ? lineFeed - 1 : lineFeed;
                if (length > ImfBodyCheck.MaxLineLength) return true;

                if (lineFeed < 0) return false;

                rest = rest.Slice(lineFeed + 1);
            }

            return false;
        }

        private void Report(ImfDiagnosticKind kind, string message, List<ImfDiagnostic> diagnostics)
        {
            if (!ImfConformance.IsAllowed(kind, _options))
                throw BuildException(message, kind);

            diagnostics.Add(new ImfDiagnostic(kind, message, -1, string.Empty));
        }

        private static ImfFileException BuildException(string message, ImfDiagnosticKind kind)
        {
            var exception = new ImfFileException(message);
            exception.Data[ImfFileException.DataItemName.DiagnosticKind] = kind;
            return exception;
        }

        // Used as an exception filter that never catches, so that the exception keeps its stack trace.
        private bool AddMessageIndex(ImfFileException exception)
        {
            exception.Data[ImfFileException.DataItemName.MessageIndex] = _messagesWritten;
            return false;
        }

        private static void CheckEntry(MboxEntry entry)
        {
            if (entry is null)
                throw new ArgumentException("An entry is null.", "entries");
        }

        private static Stream ValidateStream(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException("mailboxStream");
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "mailboxStream");

            return stream;
        }

        private static ImfOptions CheckArguments(MboxVariant variant, ImfOptions options, MboxLineBreak lineBreak)
        {
            MboxReader.CheckVariant(variant, nameof(variant));
            if (lineBreak < MboxLineBreak.LineFeed || lineBreak > MboxLineBreak.CarriageReturnLineFeed)
                throw new ArgumentOutOfRangeException(nameof(lineBreak));

            ImfHeaderText.CheckOptions(options);
            return options;
        }
    }
}
