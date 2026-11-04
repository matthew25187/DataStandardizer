using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using DataStandardizer.Communication.Mime;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Represents a reader of an Apple Mail EMLX file (<c>.emlx</c>, <c>.partial.emlx</c>) into an <see cref="EmlxEntry"/>: one message, with a byte count before it and a property list of metadata
    /// after it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Format.</b>  EMLX is proprietary and undocumented, so this follows the layout that open-source readers agree on.  The first line is the length of the message in bytes, in ASCII digits,
    /// which may have spaces after them, and then a line feed.  The message follows, with LF line breaks, and then an XML property list.  Anything in the property list that this package doesn't
    /// know is kept, not rejected.
    /// </para>
    /// <para>
    /// <b>Errors.</b>  A byte count that isn't a number or doesn't fit the file causes an <see cref="ImfFileException"/>, unless <see cref="ImfOptions.AllowInvalidEmlxByteCount"/> is set.  A
    /// property list that is missing or not well formed does too, unless <see cref="ImfOptions.AllowInvalidEmlxMetadata"/> is set.  With them set, the problem is recorded in
    /// <see cref="Diagnostics"/> instead.
    /// </para>
    /// <para>
    /// <b>Message.</b>  The message is read as an <see cref="ImfReader"/> reads it, under the same options and with the same checks, except that a line feed is its line break.  A message with
    /// CRLF line breaks therefore causes an <see cref="ImfFileException"/>.  Writing the message with an <see cref="ImfWriter"/> gives it CRLF line breaks, so it can be saved as an
    /// <c>.eml</c> file.
    /// </para>
    /// <para>
    /// <b>Partial files.</b>  Apple Mail may keep a large part of a message in a <c>.emlxpart</c> file of its own, leaving an entity with an <c>X-Apple-Content-Length</c> field and an empty body.
    /// Such an entity is found by its content, whatever the name of the file.  With no <see cref="EmlxPartResolver"/>, or one that returns <c>null</c>, the message is read without the content, with
    /// an <see cref="ImfDiagnosticKind.MissingEmlxPart"/> diagnostic and an entry in <see cref="EmlxEntry.MissingParts"/> for the part, and no opt-in is needed.  A resolver's content goes into the
    /// message at that entity, and any CRLF line breaks in it become LF, unless the part is <c>binary</c> or has an extension mechanism (RFC 2045 §2.9 and §6.3).  The message is then whole, and so
    /// it is what a writer writes.
    /// </para>
    /// <para>
    /// <b>Memory.</b>  The whole file is held in memory while it is read, up to the sum of <see cref="ImfOptions.MaxEntitySize"/>, <see cref="ImfOptions.MaxHeaderSectionLength"/> and
    /// <see cref="ImfOptions.MaxEmlxMetadataLength"/>, and the message then takes as much again.  The layout is checked before the message, so that a wrong count is reported as one, and not as
    /// whatever it does to the message.
    /// </para>
    /// <para>
    /// The data holds one entry, so it can be read only once, and any <see cref="ImfFileException"/> ends reading.  Later reads throw <see cref="InvalidOperationException"/>.  A read started while
    /// another is in progress does too.
    /// </para>
    /// </remarks>
    public sealed class EmlxReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int BlockSize = 16 * 1024;
        private const int MaxArrayLength = 0x7FFFFFC7;

        // Room for a byte count of any size, and the spaces after it.
        private const int MaxByteCountLineLength = 64;

        private const byte CarriageReturn = 0x0D;
        private const byte LineFeed = 0x0A;

        private const string FileTooLongMessage = "The file is longer than its message and property list may be under the limits of the options.";
        private const string MessageTooLongMessage = "The message and the parts supplied for it are longer than the limits of the options allow.";
        private const string MissingPartTemplate = "Part {0} is stored in another file, and no content was supplied for it.";

        private static readonly byte[] NoContent = new byte[0];

        private readonly ReadOnlyCollection<ImfDiagnostic> _readOnlyDiagnostics;
        private readonly List<ImfDiagnostic> _diagnostics = new List<ImfDiagnostic>();
        private readonly long _maxFileLength;
        private readonly ImfOptions _options;
        private readonly bool _ownsStream;
        private readonly Stream _stream;

        private long _bytePosition;
        private bool _hasStarted;

        // These are changed with Interlocked, so that a Dispose from another thread while a read is in progress closes the file once only, after that read has finished with it.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a reader for the file at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.</exception>
        public EmlxReader(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for the file at the current position of a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support reading.
        /// -or-
        /// <paramref name="options"/> has a limit that is less than 1, no <see cref="ImfOptions.CharsetResolver"/>, or an
        /// <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public EmlxReader(Stream messageStream, ImfOptions options) : this(CheckOptions(options), ImfHeaderReader.ValidateStream(messageStream), false)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public EmlxReader(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a reader for a file.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous reading, and is closed when the reader is
        /// disposed, even if nothing has been read.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has a limit that is less than 1, no
        /// <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be
        /// resolved.</exception>
        public EmlxReader(string messageFilePath, ImfOptions options) : this(CheckOptions(options), ImfHeaderReader.OpenFile(messageFilePath), true)
        {
        }
#endif

        // The options come first, so that bad options are found before a file is opened.
        private EmlxReader(ImfOptions options, Stream stream, bool ownsStream)
        {
            _options = options;
            _stream = stream;
            _ownsStream = ownsStream;
            _maxFileLength = (long)options.MaxEntitySize + options.MaxHeaderSectionLength + options.MaxEmlxMetadataLength + MaxByteCountLineLength;
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
        /// Gets the number of bytes of the file read so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long BytePosition
        {
            get
            {
                ThrowIfDisposed();
                return Volatile.Read(ref _bytePosition);
            }
        }

        /// <summary>
        /// Closes the file the reader opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the file is closed once it has finished with it.
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
        /// Closes the file the reader opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the file is closed
        /// once it has finished with it.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
#endif

        /// <summary>
        /// Reads the file.
        /// </summary>
        /// <param name="partResolver">Supplies the content of a part that a partial file stores elsewhere, or <c>null</c> to read the message without it.</param>
        /// <returns>The entry.</returns>
        /// <exception cref="ImfFileException">The file breaks a rule of its layout, or the message a rule of its format, that the options don't accept, or something passes a limit.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// The file has already been read, or an earlier read failed or was canceled.</exception>
        public EmlxEntry ReadEntry([AllowNull] EmlxPartResolver partResolver = null)
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                Start();

                var buffer = new ReadBuffer(_maxFileLength, FileTooLongMessage);
                while (true)
                {
                    buffer.PrepareRefill(out var offset, out var count);
                    var bytesRead = _stream.Read(buffer.Bytes, offset, count);
                    ThrowIfDisposed();
                    if (bytesRead == 0) break;

                    buffer.CompleteRefill(bytesRead);
                    Volatile.Write(ref _bytePosition, buffer.Length);
                    buffer.ThrowIfPastLimit();
                }

                var layout = EmlxLayout.Read(buffer.Bytes, buffer.Length, _options, _diagnostics);
                var diagnostics = new List<ImfDiagnostic>();
                var message = ReadMessage(layout.Message, diagnostics);

                var contents = new List<byte[]>();
                var parts = FindMissingParts(message);
                if (!(partResolver is null))
                {
                    var room = GetMessageLimit() - layout.Message.Count;
                    foreach (var part in parts)
                    {
                        var content = ReadPart(partResolver, part, room);
                        ThrowIfDisposed();
                        contents.Add(content);
                        room -= content.Length;
                    }
                }

                return Complete(layout, message, parts, contents, diagnostics);
            }
            finally
            {
                EndRead();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the file asynchronously.
        /// </summary>
        /// <param name="partResolver">Supplies the content of a part that a partial file stores elsewhere, or <c>null</c> to read the message without it.  It is called on the reading thread, and
        /// the streams it gives are read asynchronously.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot continue.</param>
        /// <returns>A task whose result is the entry.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// The file has already been read, or an earlier read failed or was canceled.</exception>
        /// <exception cref="ImfFileException">By the task: the file breaks a rule of its layout, or the message a rule of its format, that the options don't accept, or something passes a
        /// limit.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public Task<EmlxEntry> ReadEntryAsync([AllowNull] EmlxPartResolver partResolver = null, CancellationToken cancellationToken = default)
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            var isPending = false;
            try
            {
                Start();
                isPending = true;
                return ReadEntryCoreAsync(partResolver, cancellationToken);
            }
            finally
            {
                if (!isPending)
                {
                    EndRead();
                }
            }
        }

        private async Task<EmlxEntry> ReadEntryCoreAsync([AllowNull] EmlxPartResolver partResolver, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var buffer = new ReadBuffer(_maxFileLength, FileTooLongMessage);
                while (true)
                {
                    var bytesRead = await RefillAsync(_stream, buffer, cancellationToken).ConfigureAwait(false);
                    ThrowIfDisposed();
                    if (bytesRead == 0) break;

                    Volatile.Write(ref _bytePosition, buffer.Length);
                    buffer.ThrowIfPastLimit();
                }

                var layout = EmlxLayout.Read(buffer.Bytes, buffer.Length, _options, _diagnostics);
                var diagnostics = new List<ImfDiagnostic>();
                var message = ReadMessage(layout.Message, diagnostics);

                var contents = new List<byte[]>();
                var parts = FindMissingParts(message);
                if (!(partResolver is null))
                {
                    var room = GetMessageLimit() - layout.Message.Count;
                    foreach (var part in parts)
                    {
                        var content = await ReadPartAsync(partResolver, part, room, cancellationToken).ConfigureAwait(false);
                        ThrowIfDisposed();
                        contents.Add(content);
                        room -= content.Length;
                    }
                }

                return Complete(layout, message, parts, contents, diagnostics);
            }
            finally
            {
                EndRead();
            }
        }

        private async Task<byte[]> ReadPartAsync(EmlxPartResolver partResolver, EmlxMissingPart part, long room, CancellationToken cancellationToken)
        {
            using (var stream = partResolver(part))
            {
                if (stream is null) return NoContent;

                var buffer = new ReadBuffer(Math.Min(_options.MaxEntitySize, room), MessageTooLongMessage);
                while (await RefillAsync(stream, buffer, cancellationToken).ConfigureAwait(false) != 0)
                {
                    buffer.ThrowIfPastLimit();
                }

                return buffer.ToArray();
            }
        }

        private static async Task<int> RefillAsync(Stream stream, ReadBuffer buffer, CancellationToken cancellationToken)
        {
            buffer.PrepareRefill(out var offset, out var count);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            var bytesRead = await stream.ReadAsync(new Memory<byte>(buffer.Bytes, offset, count), cancellationToken).ConfigureAwait(false);
#else
            var bytesRead = await stream.ReadAsync(buffer.Bytes, offset, count, cancellationToken).ConfigureAwait(false);
#endif
            buffer.CompleteRefill(bytesRead);
            return bytesRead;
        }
#endif

        // The room is what the message has left under its limit, so that many parts can't together hold more in memory than one message may.
        private byte[] ReadPart(EmlxPartResolver partResolver, EmlxMissingPart part, long room)
        {
            using (var stream = partResolver(part))
            {
                if (stream is null) return NoContent;

                var buffer = new ReadBuffer(Math.Min(_options.MaxEntitySize, room), MessageTooLongMessage);
                while (true)
                {
                    buffer.PrepareRefill(out var offset, out var count);
                    var bytesRead = stream.Read(buffer.Bytes, offset, count);
                    if (bytesRead == 0) return buffer.ToArray();

                    buffer.CompleteRefill(bytesRead);
                    buffer.ThrowIfPastLimit();
                }
            }
        }

        private long GetMessageLimit()
        {
            return (long)_options.MaxEntitySize + _options.MaxHeaderSectionLength;
        }

        private ImfMessage ReadMessage(ArraySegment<byte> bytes, List<ImfDiagnostic> diagnostics)
        {
            using (var reader = new ImfReader(new MemoryStream(bytes.Array ?? NoContent, bytes.Offset, bytes.Count, false), _options, BlockSize, true))
            {
                var message = reader.ReadMessage();
                diagnostics.AddRange(reader.Diagnostics);
                return message;
            }
        }

        private EmlxEntry Complete(EmlxLayout layout, ImfMessage message, List<EmlxMissingPart> parts, List<byte[]> contents, List<ImfDiagnostic> diagnostics)
        {
            var hasContent = false;
            foreach (var content in contents)
            {
                hasContent |= content.Length > 0;
            }

            if (hasContent)
            {
                // A message with the parts in it is another message, which is read again so that its entities and its checks are those of what it now is.
                var whole = InsertParts(layout.Message, message, parts, contents);
                diagnostics.Clear();
                message = ReadMessage(new ArraySegment<byte>(whole), diagnostics);
                parts = FindMissingParts(message);
            }

            foreach (var part in parts)
            {
                diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.MissingEmlxPart, string.Format(MissingPartTemplate, part.Path), -1, part.EntityPath));
            }

            _diagnostics.AddRange(diagnostics);
            return new EmlxEntry(message, layout.Metadata, layout.ByteCountLine, new List<ImfDiagnostic>(_diagnostics), parts);
        }

        private static List<EmlxMissingPart> FindMissingParts(ImfMessage message)
        {
            var parts = new List<EmlxMissingPart>();
            foreach (var node in message.EnumerateEntities())
            {
                var entity = node.Entity;
                if (entity is ImfMultipart || !entity.RawBody.IsEmpty || !entity.Fields.Contains(EmlxMissingPart.ContentLengthFieldName)) continue;

                parts.Add(new EmlxMissingPart(node));
            }

            return parts;
        }

        // Every entity of a message that was read refers to the bytes of its body, so where a body starts in them says where its content goes.
        private byte[] InsertParts(ArraySegment<byte> messageBytes, ImfMessage message, List<EmlxMissingPart> parts, List<byte[]> contents)
        {
            var source = new ReadOnlySpan<byte>(messageBytes.Array, messageBytes.Offset, messageBytes.Count);
            var headerLength = source.Length - message.RawBody.Length;
            var limit = GetMessageLimit();
            var output = new MemoryStream(source.Length);
            var position = 0;
            for (var index = 0; index < parts.Count; index++)
            {
                var content = contents[index];
                if (content.Length == 0 || !MemoryMarshal.TryGetArray(parts[index].Entity.RawBody, out var body)) continue;

                MemoryMarshal.TryGetArray(message.RawBody, out var messageBody);
                var end = headerLength + (body.Offset - messageBody.Offset);
                ImfLineBreaks.Write(output, source.Slice(position, end - position));
                position = end;

                WriteHeaderEnd(output, source.Slice(0, end));
                ImfLineBreaks.Write(output, IsOpaque(parts[index].Entity) ? content : ToLineFeeds(content));
                if (output.Length + (source.Length - position) > limit)
                    throw new ImfFileException(MessageTooLongMessage);
            }

            ImfLineBreaks.Write(output, source.Slice(position));
            return output.ToArray();
        }

        // ref. RFC 2046 §5.1.1: a part with no body may stop before the empty line that ends its header, whose line break belongs to the delimiter, so a body needs that empty line added.
        private static void WriteHeaderEnd(MemoryStream output, ReadOnlySpan<byte> before)
        {
            var hasLineBreak = before.Length >= 1 && before[before.Length - 1] == LineFeed;
            var hasEmptyLine = hasLineBreak && before.Length >= 2 && before[before.Length - 2] == LineFeed;
            if (!hasLineBreak) output.WriteByte(LineFeed);
            if (!hasEmptyLine) output.WriteByte(LineFeed);
        }

        private static bool IsOpaque(ImfEntity entity)
        {
            var encoding = entity.ContentTransferEncoding;
            return encoding == MimeTransferEncoding.Binary || !encoding.IsKnown;
        }

        private static byte[] ToLineFeeds(byte[] content)
        {
            var output = new MemoryStream(content.Length);
            for (var index = 0; index < content.Length; index++)
            {
                if (content[index] == CarriageReturn && index + 1 < content.Length && content[index + 1] == LineFeed) continue;

                output.WriteByte(content[index]);
            }

            return output.ToArray();
        }

        /// <summary>
        /// Marks the one read of the file as started.
        /// </summary>
        /// <exception cref="InvalidOperationException">It has already been started.</exception>
        private void Start()
        {
            if (_hasStarted)
                throw new InvalidOperationException("The file has already been read, or an earlier read failed or was canceled.");

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
        /// Marks the read as finished, and closes the file if the reader was disposed while it was in progress.
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
        /// Closes the file the reader opened itself, the first time it is called.
        /// </summary>
        private void Release()
        {
            if (Interlocked.Exchange(ref _isReleased, 1) != 0) return;

            if (_ownsStream)
            {
                _stream.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

        private static ImfOptions CheckOptions(ImfOptions options)
        {
            ImfReader.CheckOptions(options);
            if (options.MaxEmlxMetadataLength < 1)
                throw new ArgumentException("The maximum metadata length must be at least 1.", nameof(options));

            return options;
        }

        /// <summary>
        /// Bytes being read to the end of a stream, in a block that grows up to a limit.
        /// </summary>
        private sealed class ReadBuffer
        {
            private readonly long _limit;
            private readonly string _limitMessage;

            internal ReadBuffer(long limit, string limitMessage)
            {
                // One less than the longest array, so that there is always room for the octet that shows a stream has passed the limit.
                _limit = Math.Max(0, Math.Min(limit, MaxArrayLength - 1));
                _limitMessage = limitMessage;
                Bytes = new byte[Math.Min(BlockSize, _limit + 1)];
            }

            internal byte[] Bytes { get; private set; }

            internal int Length { get; private set; }

            // There is always room for one more than the limit, so that a stream that passes it can be told from one that ends at it.
            internal void PrepareRefill(out int offset, out int count)
            {
                if (Length == Bytes.Length)
                {
                    var bytes = Bytes;
                    Array.Resize(ref bytes, (int)Math.Min(Bytes.Length * 2L, _limit + 1));
                    Bytes = bytes;
                }

                offset = Length;
                count = Bytes.Length - Length;
            }

            internal void CompleteRefill(int bytesRead)
            {
                Length += bytesRead;
            }

            internal void ThrowIfPastLimit()
            {
                if (Length > _limit)
                    throw new ImfFileException(_limitMessage);
            }

            internal byte[] ToArray()
            {
                var bytes = new byte[Length];
                Array.Copy(Bytes, bytes, Length);
                return bytes;
            }
        }
    }
}
