using System;
using System.Buffers;
using System.IO;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a writer that provides fast, non-cached, forward-only output of whole Internet messages, with their MIME entities (RFC 2045, RFC 2046), from
    /// a description of each message.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>All at once.</b>  The writer checks a whole message before it writes any of it, so a message it refuses leaves nothing in the output, and writing
    /// can continue with another.  It reads a body from a stream block by block, and doesn't hold it in memory.
    /// </para>
    /// <para>
    /// <b>Header fields.</b>  Fields are written as <see cref="ImfHeaderWriter"/> writes them, in the order they were added.  After them, unless they include
    /// the same field, the writer adds <c>MIME-Version: 1.0</c> to the header of a message (RFC 2045 §4), then the Content-Type field, then a
    /// Content-Transfer-Encoding field unless the encoding is a <c>7bit</c> that was not asked for (RFC 2045 §6.1).
    /// </para>
    /// <para>
    /// <b>Transfer encoding.</b>  Unless <see cref="ImfEntitySource.TransferEncoding"/> says which to use, each body gets the lightest that fits.
    /// <c>7bit</c> fits if its lines are at most 998 octets with CRLF line breaks, and it has no NULs or octets above 127 (RFC 2045 §2.7).  <c>8bit</c>, which
    /// also allows octets above 127, is next if <see cref="ImfOptions.AllowEightBitTransferEncoding"/> is set.  <c>binary</c>, which fits anything, is next if
    /// <see cref="ImfOptions.AllowBinaryTransferEncoding"/> is set.  Otherwise it is <c>quoted-printable</c> or <c>base64</c>, whichever an estimate of their
    /// sizes with line breaks says is smaller, with <c>quoted-printable</c> winning a tie.  The estimate can differ from the output by a few bytes, as an
    /// encoded octet is never split across a soft line break.
    /// </para>
    /// <para>
    /// A body that is encoded decodes to exactly the bytes given, with one exception.  In <c>quoted-printable</c>, a CRLF in a <c>text</c> body is written as a
    /// line break, as RFC 2045 §6.7 rule 4 says; if the body also holds a bare CR or LF, or is not <c>text</c>, each CR and LF is encoded, with only soft line
    /// breaks.  The exception is a <c>text</c> body from a stream that doesn't support seeking, which can't be looked at first: it is treated as text in the
    /// canonical form of RFC 2046 §4.1.1, so a bare LF in it is written as a line break, and decodes to a CRLF.
    /// </para>
    /// <para>
    /// A multipart entity or embedded message gets the identity encoding that covers what it holds (RFC 2045 §6.4), so one with an <c>8bit</c> part is labelled
    /// <c>8bit</c>.  Such an entity has no encoding of its own, so a body of content with a multipart or message type can only be given an identity encoding,
    /// except <c>message/global</c> (RFC 6532 §3.7).
    /// </para>
    /// <para>
    /// <b>Boundaries.</b>  A boundary that was not given is random, 34 characters from the RFC 2046 §5.1.1 <c>bchars</c>, and holds <c>=_</c>, which
    /// <c>quoted-printable</c> output can't, as RFC 2045 §6.7 has every <c>=</c> followed by two hexadecimal digits or a line break.  It is checked against
    /// the enclosing boundaries, and against every line of the content it encloses that could hold it.  If it collides, a new one is picked.  A boundary that
    /// was given is checked in the same way, and a collision is refused.
    /// </para>
    /// <para>
    /// <b>Raw passthrough.</b>  An entity from <see cref="ImfEntitySource.FromRaw(ReadOnlyMemory{byte})"/> is copied byte for byte, which keeps a
    /// <c>multipart/signed</c> or DKIM signature over it valid.  The same goes for a field added with <see cref="ImfEntitySource.AddField(ImfHeaderFieldView)"/>
    /// or <see cref="ImfEntitySource.AddRawField"/>.
    /// </para>
    /// <para>
    /// <b>Limits.</b>  <see cref="ImfOptions.MaxNestingDepth"/> and <see cref="ImfOptions.MaxPartCount"/> apply, as to a reader, which also keeps a tree that
    /// holds itself from being followed for ever.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another is in
    /// progress throws <see cref="InvalidOperationException"/>.  After a write fails or is canceled once output has started, a message is incomplete, so the
    /// writer cannot continue.
    /// </para>
    /// <para>
    /// A file-path constructor opens the file with <c>FileOptions.Asynchronous</c>, so the asynchronous methods do no synchronous I/O.  To write a file
    /// only synchronously, without the small cost that option adds on Windows, pass a <c>FileStream</c> opened without it to a <see cref="Stream"/>
    /// constructor.
    /// </para>
    /// </remarks>
    public sealed class ImfEntityWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const byte CarriageReturn = 0x0D;
        private const int DefaultBlockSize = 16 * 1024;
        private const string GeneratedBoundaryPrefix = "=_";
        private const string GuidFormatWithoutSeparators = "N";

        private const int EncodedBlockSize = 16 * 1024;

        private readonly int _blockSize;
        private readonly Func<string> _boundaryGenerator;
        private readonly bool _lineFeedIsLineBreak;
        private readonly ImfOptions _options;
        private readonly ImfOutput _output;

        private long _entitiesWritten;
        private long _fieldsWritten;

        /// <summary>
        /// Creates a writer of messages to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.</exception>
        public ImfEntityWriter(Stream messageStream) : this(messageStream, new ImfOptions())
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
        /// <paramref name="options"/> has a limit that is less than 1.</exception>
        public ImfEntityWriter(Stream messageStream, ImfOptions options)
            : this(messageStream, options, DefaultBlockSize, false, GenerateBoundary)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of messages to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public ImfEntityWriter(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of messages to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1.</exception>
        public ImfEntityWriter(string messageFilePath, ImfOptions options)
            : this(CheckOptions(options), OpenFile(messageFilePath), true, DefaultBlockSize, false, GenerateBoundary)
        {
        }
#endif

        /// <summary>
        /// Creates a writer with a specific block size, line break setting and source of boundaries.  Allows tests to force a body to be split at chosen
        /// points and to choose the boundaries, and the writers of container formats to write the LF line breaks of their messages.
        /// </summary>
        internal ImfEntityWriter(Stream messageStream, ImfOptions options, int blockSize, bool lineFeedIsLineBreak, Func<string> boundaryGenerator)
            : this(CheckOptions(options), ImfOutput.ValidateStream(messageStream), false, blockSize, lineFeedIsLineBreak, boundaryGenerator)
        {
        }

        // The options come first, so that bad options are found before a file is opened.
        private ImfEntityWriter(ImfOptions options, Stream stream, bool ownsStream, int blockSize, bool lineFeedIsLineBreak, Func<string> boundaryGenerator)
        {
            if (blockSize < 1)
                throw new ArgumentOutOfRangeException(nameof(blockSize));

            _options = options;
            _output = new ImfOutput(stream, ownsStream, GetType().Name);
            _blockSize = blockSize;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
            _boundaryGenerator = boundaryGenerator;
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
        /// Gets the number of entities written so far, at any depth, including each message.  An entity written from its raw bytes counts as one, whatever it
        /// holds.
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
        /// Gets the number of header fields written so far, including those the writer adds.  The fields inside an entity written from its raw bytes are not
        /// counted.
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
        /// <param name="progress">Receives the progress after each block of output has been handed to the stream, synchronously on the writing thread; or
        /// <c>null</c>.  An exception from <see cref="IProgress{T}.Report"/> ends the call, leaving the message incomplete.  <see cref="Progress{T}"/> runs its handler later, so writing
        /// doesn't wait for the handler, and an exception from the handler doesn't end the call.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The message can't be written: its <see cref="ImfFileException.DataItemName.EntityPath"/> item says which entity is
        /// at fault.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteMessage(ImfEntitySource message, IProgress<ImfWriteProgress>? progress = null)
#else
        public void WriteMessage(ImfEntitySource message, IProgress<ImfWriteProgress> progress = null)
#endif
        {
            _output.ThrowIfDisposed();
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            _output.BeginOperation();
            var readBlock = ArrayPool<byte>.Shared.Rent(_blockSize);
            var encodedBlock = ArrayPool<byte>.Shared.Rent(EncodedBlockSize);
            try
            {
                var plan = new ImfEntityWritePlan(message, _options, _lineFeedIsLineBreak, _boundaryGenerator);
                do
                {
                    foreach (var scan in plan.PendingScans)
                    {
                        plan.BeginScan(scan);
                        ReadScan(scan, readBlock);
                    }
                } while (plan.CompletePass());

                var items = plan.BuildItems();
                try
                {
                    foreach (var item in items)
                    {
                        _output.ThrowIfDisposed();
                        if (item.IsContent)
                        {
                            WriteContent(item, readBlock, encodedBlock, progress);
                        }
                        else
                        {
                            _output.Write(item.Bytes.Span);
                            _fieldsWritten += item.FieldsWritten;
                            _entitiesWritten += item.EntitiesWritten;
                            progress?.Report(GetProgress());
                        }
                    }
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
                ArrayPool<byte>.Shared.Return(encodedBlock);
                ArrayPool<byte>.Shared.Return(readBlock);
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
        /// <param name="progress">Receives the progress after each block of output has been handed to the stream, synchronously on the writing thread; or
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
        /// <exception cref="ImfFileException">By the task: the message can't be written: its <see cref="ImfFileException.DataItemName.EntityPath"/> item says
        /// which entity is at fault.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteMessageAsync(ImfEntitySource message, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)
#else
        public Task WriteMessageAsync(ImfEntitySource message, IProgress<ImfWriteProgress> progress = null, CancellationToken cancellationToken = default)
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

        private async Task WriteMessageCoreAsync(ImfEntitySource message,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<ImfWriteProgress>? progress,
#else
            IProgress<ImfWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            var readBlock = ArrayPool<byte>.Shared.Rent(_blockSize);
            var encodedBlock = ArrayPool<byte>.Shared.Rent(EncodedBlockSize);
            var hasStartedOutput = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var plan = new ImfEntityWritePlan(message, _options, _lineFeedIsLineBreak, _boundaryGenerator);
                do
                {
                    foreach (var scan in plan.PendingScans)
                    {
                        plan.BeginScan(scan);
                        await ReadScanAsync(scan, readBlock, cancellationToken).ConfigureAwait(false);
                    }
                } while (plan.CompletePass());

                var items = plan.BuildItems();
                hasStartedOutput = true;
                foreach (var item in items)
                {
                    _output.ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    if (item.IsContent)
                    {
                        await WriteContentAsync(item, readBlock, encodedBlock, progress, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await _output.WriteAsync(item.Bytes, cancellationToken).ConfigureAwait(false);
                        _fieldsWritten += item.FieldsWritten;
                        _entitiesWritten += item.EntitiesWritten;
                        progress?.Report(GetProgress());
                    }
                }

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
                ArrayPool<byte>.Shared.Return(encodedBlock);
                ArrayPool<byte>.Shared.Return(readBlock);
                if (_output.EndOperation()) await _output.ReleaseAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask ReadScanAsync(ImfBodyScan scan, byte[] block, CancellationToken cancellationToken)
        {
            if (scan.IsStream)
            {
                int count;
                while ((count = await ReadAsync(scan.Stream, block, _blockSize, cancellationToken).ConfigureAwait(false)) > 0)
                {
                    scan.Add(new ReadOnlySpan<byte>(block, 0, count));
                }
            }
            else
            {
                scan.Add(scan.Memory.Span);
            }

            scan.Complete();
        }

        private async ValueTask WriteContentAsync(ImfWriteItem item, byte[] readBlock, byte[] encodedBlock,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<ImfWriteProgress>? progress,
#else
            IProgress<ImfWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            RewindContent(item);
            var isStream = !ReferenceEquals(item.Stream, Stream.Null);
            var remaining = item.Bytes;
            while (true)
            {
                ReadOnlyMemory<byte> block;
                bool isFinalBlock;
                if (isStream)
                {
                    var count = await ReadAsync(item.Stream, readBlock, _blockSize, cancellationToken).ConfigureAwait(false);
                    block = new ReadOnlyMemory<byte>(readBlock, 0, count);
                    isFinalBlock = count == 0;
                }
                else
                {
                    var count = Math.Min(remaining.Length, _blockSize);
                    block = remaining.Slice(0, count);
                    remaining = remaining.Slice(count);
                    isFinalBlock = remaining.IsEmpty;
                }

                _output.ThrowIfDisposed();
                if (item.Encoder is null)
                {
                    await _output.WriteAsync(block, cancellationToken).ConfigureAwait(false);
                    progress?.Report(GetProgress());
                }
                else
                {
                    var source = block;
                    OperationStatus status;
                    do
                    {
                        int length;
                        status = EncodeBlock(item.Encoder, source.Span, encodedBlock, isFinalBlock, out var consumed, out length);
                        source = source.Slice(consumed);
                        await _output.WriteAsync(new ReadOnlyMemory<byte>(encodedBlock, 0, length), cancellationToken).ConfigureAwait(false);
                        progress?.Report(GetProgress());
                    } while (status != OperationStatus.Done);
                }

                if (isFinalBlock) return;
            }
        }

        private static async ValueTask<int> ReadAsync(Stream stream, byte[] block, int count, CancellationToken cancellationToken)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            return await stream.ReadAsync(new Memory<byte>(block, 0, count), cancellationToken).ConfigureAwait(false);
#else
            return await stream.ReadAsync(block, 0, count, cancellationToken).ConfigureAwait(false);
#endif
        }

        private static FileStream OpenFile(string path)
        {
            if (path is null)
                throw new ArgumentNullException("messageFilePath");

            return ImfFileStreams.OpenWrite(path, ImfOutput.FileBufferSize);
        }
#endif

        private void ReadScan(ImfBodyScan scan, byte[] block)
        {
            if (scan.IsStream)
            {
                int count;
                while ((count = scan.Stream.Read(block, 0, _blockSize)) > 0)
                {
                    scan.Add(new ReadOnlySpan<byte>(block, 0, count));
                }
            }
            else
            {
                scan.Add(scan.Memory.Span);
            }

            scan.Complete();
        }

        private void WriteContent(ImfWriteItem item, byte[] readBlock, byte[] encodedBlock,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<ImfWriteProgress>? progress)
#else
            IProgress<ImfWriteProgress> progress)
#endif
        {
            RewindContent(item);
            var isStream = !ReferenceEquals(item.Stream, Stream.Null);
            var remaining = item.Bytes;
            while (true)
            {
                ReadOnlySpan<byte> block;
                bool isFinalBlock;
                if (isStream)
                {
                    var count = item.Stream.Read(readBlock, 0, _blockSize);
                    block = new ReadOnlySpan<byte>(readBlock, 0, count);
                    isFinalBlock = count == 0;
                }
                else
                {
                    var count = Math.Min(remaining.Length, _blockSize);
                    block = remaining.Span.Slice(0, count);
                    remaining = remaining.Slice(count);
                    isFinalBlock = remaining.IsEmpty;
                }

                _output.ThrowIfDisposed();
                if (item.Encoder is null)
                {
                    _output.Write(block);
                    progress?.Report(GetProgress());
                }
                else
                {
                    OperationStatus status;
                    do
                    {
                        int consumed, length;
                        status = EncodeBlock(item.Encoder, block, encodedBlock, isFinalBlock, out consumed, out length);
                        block = block.Slice(consumed);
                        _output.Write(new ReadOnlySpan<byte>(encodedBlock, 0, length));
                        progress?.Report(GetProgress());
                    } while (status != OperationStatus.Done);
                }

                if (isFinalBlock) return;
            }
        }

        private OperationStatus EncodeBlock(ImfBodyEncoder encoder, ReadOnlySpan<byte> source, byte[] encodedBlock, bool isFinalBlock, out int consumed, out int length)
        {
            var status = encoder.Encode(source, encodedBlock, out consumed, out length, isFinalBlock);
            if (_lineFeedIsLineBreak)
            {
                length = RemoveCarriageReturns(encodedBlock, length);
            }

            return status;
        }

        private ImfWriteProgress GetProgress()
        {
            return new ImfWriteProgress(_fieldsWritten, _entitiesWritten, _output.BytePosition);
        }

        /// <summary>
        /// Takes the carriage returns out of encoded data for a container format.  The encoders write them only in line breaks, as they encode the rest.
        /// </summary>
        private static int RemoveCarriageReturns(byte[] block, int length)
        {
            var kept = 0;
            for (var index = 0; index < length; index++)
            {
                if (block[index] != CarriageReturn) block[kept++] = block[index];
            }

            return kept;
        }

        private static void RewindContent(ImfWriteItem item)
        {
            item.Encoder?.Reset();
            if (!ReferenceEquals(item.Stream, Stream.Null) && item.Stream.CanSeek)
            {
                item.Stream.Position = item.StartPosition;
            }
        }

        private static string GenerateBoundary()
        {
            // ref. RFC 2046 §5.1.1, RFC 2045 §6.7: all of these are bchars, and quoted-printable output can't hold "=_".
            return GeneratedBoundaryPrefix + Guid.NewGuid().ToString(GuidFormatWithoutSeparators);
        }

        private static ImfOptions CheckOptions(ImfOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.MaxNestingDepth < 1)
                throw new ArgumentException("The maximum nesting depth must be at least 1.", nameof(options));
            if (options.MaxPartCount < 1)
                throw new ArgumentException("The maximum part count must be at least 1.", nameof(options));

            return options;
        }
    }
}
