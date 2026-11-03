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
    /// Represents a writer that provides fast, non-cached, forward-only output of the fields of one header section of an Internet message (RFC 5322 §2.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Line breaks.</b>  Every line ends in CRLF, whatever the platform.
    /// </para>
    /// <para>
    /// <b>Folding.</b>  A field given by name and body is folded at spaces and tabs already in the body, aiming for lines of 78 characters and never
    /// passing 998 octets (RFC 5322 §2.1.1 and §2.2.3, RFC 6532 §3.4).  No white space is added or removed, except that one space is written after the
    /// colon if the body has none, so unfolding gives the same body back.  A <see cref="ImfHeaderFieldKind.Structured"/> field is not folded inside a
    /// quoted string or domain literal.  A field that can't be folded within 998 octets is refused, and a line may pass 78 characters when
    /// it has nowhere to fold.
    /// </para>
    /// <para>
    /// <b>Encoded words.</b>  <see cref="WriteTextField"/> writes text as an unstructured field, with what has to be encoded as RFC 2047 encoded words unless
    /// <see cref="ImfOptions.HeaderEncoding"/> is <see cref="ImfHeaderEncoding.Utf8"/>.  Each word is folded whole, so a line holds at most 76 characters of encoded
    /// words.  <see cref="WriteField(string, string, ImfHeaderFieldKind)"/> writes a field body as it is given: get a body with encoded words from
    /// <see cref="ImfAddress.ToString(ImfHeaderEncoding)"/>, <see cref="ImfAddressList.ToString(ImfHeaderEncoding)"/>,
    /// <see cref="ImfPhraseList.ToString(ImfHeaderEncoding)"/> or <see cref="ImfHeaderText.FormatComment"/>.
    /// </para>
    /// <para>
    /// <b>Refusals.</b>  A field name must be printable US-ASCII with no colon (RFC 5322 §3.6.8).  A body must be given unfolded, with no control
    /// characters.  It must be US-ASCII, unless <see cref="ImfOptions.HeaderEncoding"/> is <see cref="ImfHeaderEncoding.Utf8"/> and it is valid UTF-8
    /// (RFC 6532).  A field that is refused throws <see cref="ImfFileException"/> before any of it is written, so writing can continue with the next field.
    /// </para>
    /// <para>
    /// <b>Raw passthrough.</b>  A field written from an <see cref="ImfHeaderFieldView"/> is copied byte for byte, including its folding and line breaks,
    /// whichever options it was read under, so that signatures over it stay valid.  So are bytes given to <see cref="WriteRawField"/>, once they have been
    /// checked to be one whole field.
    /// </para>
    /// <para>
    /// Nothing is held back from the stream, so a body can be written straight to it after <see cref="WriteEndOfHeader"/>.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another is in
    /// progress throws <see cref="InvalidOperationException"/>.  After a write to the stream fails or is canceled, the writer cannot continue.
    /// </para>
    /// <para>
    /// A file-path constructor opens the file with <c>FileOptions.Asynchronous</c>, so the asynchronous methods do no synchronous I/O.  To write a file
    /// only synchronously, without the small cost that option adds on Windows, pass a <c>FileStream</c> opened without it to a <see cref="Stream"/>
    /// constructor.
    /// </para>
    /// </remarks>
    public sealed class ImfHeaderWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int InitialBufferSize = 1024;

        private static readonly byte[] CrLf = { 0x0D, 0x0A };
        private static readonly byte[] Lf = { 0x0A };

        private readonly bool _acceptBareLineFeed;
        private readonly ImfHeaderEncoding _encoding;
        private readonly bool _lineFeedIsLineBreak;
        private readonly byte[] _lineBreak;
        private readonly ImfOutput _output;

        private byte[] _buffer;
        private long _fieldsWritten;
        private bool _hasEnded;

        /// <summary>
        /// Creates a writer of a header section to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.</exception>
        public ImfHeaderWriter(Stream messageStream) : this(messageStream, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of a header section to a stream.
        /// </summary>
        /// <param name="messageStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="messageStream"/> does not support writing.</exception>
        public ImfHeaderWriter(Stream messageStream, ImfOptions options) : this(messageStream, options, false)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of a header section to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> is <c>null</c>.</exception>
        public ImfHeaderWriter(string messageFilePath) : this(messageFilePath, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a writer of a header section to a file.  An existing file is overwritten.
        /// </summary>
        /// <param name="messageFilePath">The path of the file.  The file is opened for asynchronous writing, and is closed when the writer is
        /// disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="messageFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        public ImfHeaderWriter(string messageFilePath, ImfOptions options)
            : this(CheckOptions(options), OpenFile(messageFilePath), true, false)
        {
        }
#endif

        /// <summary>
        /// Creates a writer with a line break setting, for the writers of container formats, whose messages have LF line breaks.  Folding limits are
        /// counted without the line break either way.
        /// </summary>
        internal ImfHeaderWriter(Stream messageStream, ImfOptions options, bool lineFeedIsLineBreak)
            : this(CheckOptions(options), ImfOutput.ValidateStream(messageStream), false, lineFeedIsLineBreak)
        {
        }

        // The options come first, so that bad options are found before a file is opened.
        private ImfHeaderWriter(ImfOptions options, Stream stream, bool ownsStream, bool lineFeedIsLineBreak)
        {
            _output = new ImfOutput(stream, ownsStream, GetType().Name);
            _encoding = options.HeaderEncoding;
            _acceptBareLineFeed = options.AcceptBareLineFeed;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
            _lineBreak = lineFeedIsLineBreak ? Lf : CrLf;
            _buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
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
        /// Gets the number of fields written so far.
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
        /// Releases the writer's buffer, and closes the file it opened itself.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffer and file
        /// are released once it has finished with them.
        /// </remarks>
        public void Dispose()
        {
            if (_output.MarkDisposed()) Release();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Releases the writer's buffer, and closes the file it opened itself, asynchronously.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the buffer and file are released once it has finished with them.
        /// </remarks>
        /// <returns>A task that completes once any operation in progress has finished, and the file has been closed.</returns>
        public ValueTask DisposeAsync()
        {
            return _output.MarkDisposed() ? ReleaseAsync() : _output.WaitForReleaseAsync();
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
                EndOperation();
            }
        }

        /// <summary>
        /// Writes the empty line that ends the header section (RFC 5322 §2.1).  No more fields can be written after it.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// The section has already ended.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void WriteEndOfHeader()
        {
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            try
            {
                ThrowIfEnded();
                _output.Write(_lineBreak);
                _hasEnded = true;
                _output.ThrowIfDisposed();
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes a field, folded as its kind allows.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The unfolded field body.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="ImfHeaderFieldKind"/> member.</exception>
        /// <exception cref="ImfFileException">The field can't be written.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// The section has ended.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void WriteField(string name, string value, ImfHeaderFieldKind kind)
        {
            _output.ThrowIfDisposed();
            CheckText(name, value, kind);

            _output.BeginOperation();
            try
            {
                ThrowIfEnded();
                WriteFormattedField(ImfHeaderFormatter.GetBytes(name), ImfHeaderFormatter.GetBytes(value), kind);
                _output.ThrowIfDisposed();
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes a field, folded as its kind allows.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The unfolded field body.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="ImfHeaderFieldKind"/> member.</exception>
        /// <exception cref="ImfFileException">The field can't be written.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// The section has ended.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void WriteField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, ImfHeaderFieldKind kind)
        {
            _output.ThrowIfDisposed();
            CheckKind(kind);

            _output.BeginOperation();
            try
            {
                ThrowIfEnded();
                WriteFormattedField(name, value, kind);
                _output.ThrowIfDisposed();
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes text as an unstructured field, such as Subject: encoded as <see cref="ImfHeaderText.FormatUnstructured"/> describes, and folded between words.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="text">The text.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The field can't be written, as the text holds a control character other than tab or an unpaired surrogate.  Nothing has
        /// been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// The section has ended.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void WriteTextField(string name, string text)
        {
            _output.ThrowIfDisposed();
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            _output.BeginOperation();
            try
            {
                ThrowIfEnded();
                var nameBytes = ImfHeaderFormatter.GetBytes(name);
                WriteFormattedField(nameBytes, ImfHeaderFormatter.GetTextBody(nameBytes, text, _encoding), ImfHeaderFieldKind.Unstructured);
                _output.ThrowIfDisposed();
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes a field that a reader has just read, byte for byte.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, or the reader of the field has.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// The section has ended.
        /// -or-
        /// The reader has moved on from the field, or the view was not obtained from a reader.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void WriteField(ImfHeaderFieldView field)
        {
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            try
            {
                ThrowIfEnded();
                _output.Write(field.RawBytes);
                _fieldsWritten++;
                _output.ThrowIfDisposed();
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes the raw bytes of a field byte for byte, including its folding and its final line break, after checking that they are one whole field.
        /// </summary>
        /// <remarks>
        /// The check is of the structure only: a field name and a colon, line breaks that are CRLF (or LF, with <see cref="ImfOptions.AcceptBareLineFeed"/>),
        /// a space or tab after every line break but the last, and a line break at the end.  A body that a reader accepted under its options, such as one
        /// with a long line or bytes that are not UTF-8, is copied as it is.
        /// </remarks>
        /// <param name="rawField">The bytes of the field.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ImfFileException">The bytes are not one whole field.  Nothing has been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// The section has ended.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
        public void WriteRawField(ReadOnlySpan<byte> rawField)
        {
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            try
            {
                ThrowIfEnded();
                ImfHeaderFormatter.CheckRawField(rawField, _lineFeedIsLineBreak, _acceptBareLineFeed);
                _output.Write(rawField);
                _fieldsWritten++;
                _output.ThrowIfDisposed();
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Copies the rest of the fields of a reader, byte for byte.  The empty line that ends the section is not written.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so <paramref name="progress"/> may not call the writer.
        /// </remarks>
        /// <param name="reader">The reader.</param>
        /// <param name="progress">Receives the progress after each field has been handed to the stream, synchronously on the writing thread; or
        /// <c>null</c>.  An exception from <see cref="IProgress{T}.Report"/> ends the call; the field it was reporting has been written.  <see cref="Progress{T}"/> runs its handler later,
        /// so writing doesn't wait for the handler, and an exception from the handler doesn't end the call.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it, or the reader has.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException">The reader found a field it can't read.  The fields before it have been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress on the writer or the reader.
        /// -or-
        /// The section has ended.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteFields(ImfHeaderReader reader, IProgress<ImfWriteProgress>? progress = null)
#else
        public void WriteFields(ImfHeaderReader reader, IProgress<ImfWriteProgress> progress = null)
#endif
        {
            _output.ThrowIfDisposed();
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));

            _output.BeginOperation();
            try
            {
                ThrowIfEnded();
                while (reader.ReadField() is ImfHeaderFieldView field)
                {
                    _output.ThrowIfDisposed();
                    _output.Write(field.RawBytes);
                    _fieldsWritten++;
                    progress?.Report(new ImfWriteProgress(_fieldsWritten, 0, _output.BytePosition));
                }

                _output.ThrowIfDisposed();
            }
            finally
            {
                EndOperation();
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
        /// Writes the empty line that ends the header section (RFC 5322 §2.1), asynchronously.  No more fields can be written after it.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the line has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.
        /// -or-
        /// By the task: the section has already ended.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteEndOfHeaderAsync(CancellationToken cancellationToken = default)
        {
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            return WriteEndOfHeaderCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Writes a field asynchronously, folded as its kind allows.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The unfolded field body.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the field has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="ImfHeaderFieldKind"/> member.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.
        /// -or-
        /// By the task: the section has ended.</exception>
        /// <exception cref="ImfFileException">By the task: the field can't be written.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteFieldAsync(string name, string value, ImfHeaderFieldKind kind, CancellationToken cancellationToken = default)
        {
            _output.ThrowIfDisposed();
            CheckText(name, value, kind);

            _output.BeginOperation();
            return WriteTextFieldCoreAsync(name, value, kind, cancellationToken);
        }

        /// <summary>
        /// Writes a field asynchronously, folded as its kind allows.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="value">The unfolded field body.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the field has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="ImfHeaderFieldKind"/> member.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.
        /// -or-
        /// By the task: the section has ended.</exception>
        /// <exception cref="ImfFileException">By the task: the field can't be written.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteFieldAsync(ReadOnlyMemory<byte> name, ReadOnlyMemory<byte> value, ImfHeaderFieldKind kind, CancellationToken cancellationToken = default)
        {
            _output.ThrowIfDisposed();
            CheckKind(kind);

            _output.BeginOperation();
            return WriteBytesFieldCoreAsync(name, value, kind, cancellationToken);
        }

        /// <summary>
        /// Writes text as an unstructured field, such as Subject, asynchronously: encoded as <see cref="ImfHeaderText.FormatUnstructured"/> describes, and folded
        /// between words.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <param name="text">The text.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the field has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.
        /// -or-
        /// By the task: the section has ended.</exception>
        /// <exception cref="ImfFileException">By the task: the field can't be written, as the text holds a control character other than tab or an unpaired surrogate.
        /// Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteTextFieldAsync(string name, string text, CancellationToken cancellationToken = default)
        {
            _output.ThrowIfDisposed();
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            _output.BeginOperation();
            return WriteEncodedTextFieldCoreAsync(name, text, cancellationToken);
        }

        /// <summary>
        /// Writes a field that a reader has just read, byte for byte, asynchronously.
        /// </summary>
        /// <remarks>
        /// The field must stay current until the task completes, so don't read from the reader until then.
        /// </remarks>
        /// <param name="field">The field.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the field has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.  By the task: the reader of the field has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.
        /// -or-
        /// By the task: the section has ended, or the reader has moved on from the field, or the view was not obtained from a reader.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteFieldAsync(ImfHeaderFieldView field, CancellationToken cancellationToken = default)
        {
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            return WriteViewCoreAsync(field, cancellationToken);
        }

        /// <summary>
        /// Writes the raw bytes of a field byte for byte, asynchronously, after checking that they are one whole field.
        /// </summary>
        /// <remarks>
        /// The check is the one <see cref="WriteRawField"/> describes.
        /// </remarks>
        /// <param name="rawField">The bytes of the field.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the field has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.
        /// -or-
        /// By the task: the section has ended.</exception>
        /// <exception cref="ImfFileException">By the task: the bytes are not one whole field.  Nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteRawFieldAsync(ReadOnlyMemory<byte> rawField, CancellationToken cancellationToken = default)
        {
            _output.ThrowIfDisposed();
            _output.BeginOperation();
            return WriteRawFieldCoreAsync(rawField, cancellationToken);
        }

        /// <summary>
        /// Copies the rest of the fields of a reader, byte for byte, asynchronously.  The empty line that ends the section is not written.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so <paramref name="progress"/> may not call the writer.
        /// </remarks>
        /// <param name="reader">The reader.</param>
        /// <param name="progress">Receives the progress after each field has been handed to the stream, synchronously on the writing thread; or
        /// <c>null</c>.  An exception from <see cref="IProgress{T}.Report"/> ends the call; the field it was reporting has been written.  <see cref="Progress{T}"/> runs its handler later,
        /// so writing doesn't wait for the handler, and an exception from the handler doesn't end the call.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once every field has been handed to the stream.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the
        /// write is in progress.  By the task: the reader has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress on the writer.
        /// -or-
        /// An earlier write failed or was canceled, so the writer cannot continue.
        /// -or-
        /// By the task: the section has ended, or another read is in progress on the reader.</exception>
        /// <exception cref="ImfFileException">By the task: the reader found a field it can't read.  The fields before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteFieldsAsync(ImfHeaderReader reader, IProgress<ImfWriteProgress>? progress = null, CancellationToken cancellationToken = default)
#else
        public Task WriteFieldsAsync(ImfHeaderReader reader, IProgress<ImfWriteProgress> progress = null, CancellationToken cancellationToken = default)
#endif
        {
            _output.ThrowIfDisposed();
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));

            _output.BeginOperation();
            return WriteFieldsCoreAsync(reader, progress, cancellationToken);
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
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask WriteEndOfHeaderCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfEnded();
                cancellationToken.ThrowIfCancellationRequested();
                await _output.WriteAsync(_lineBreak, cancellationToken).ConfigureAwait(false);
                _hasEnded = true;
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask WriteTextFieldCoreAsync(string name, string value, ImfHeaderFieldKind kind, CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfEnded();
                cancellationToken.ThrowIfCancellationRequested();
                var length = FormatField(ImfHeaderFormatter.GetBytes(name), ImfHeaderFormatter.GetBytes(value), kind);
                await WriteBufferAsync(length, cancellationToken).ConfigureAwait(false);
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask WriteEncodedTextFieldCoreAsync(string name, string text, CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfEnded();
                cancellationToken.ThrowIfCancellationRequested();
                var nameBytes = ImfHeaderFormatter.GetBytes(name);
                var length = FormatField(nameBytes, ImfHeaderFormatter.GetTextBody(nameBytes, text, _encoding), ImfHeaderFieldKind.Unstructured);
                await WriteBufferAsync(length, cancellationToken).ConfigureAwait(false);
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask WriteBytesFieldCoreAsync(ReadOnlyMemory<byte> name, ReadOnlyMemory<byte> value, ImfHeaderFieldKind kind, CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfEnded();
                cancellationToken.ThrowIfCancellationRequested();
                var length = FormatField(name.Span, value.Span, kind);
                await WriteBufferAsync(length, cancellationToken).ConfigureAwait(false);
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask WriteViewCoreAsync(ImfHeaderFieldView field, CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfEnded();
                cancellationToken.ThrowIfCancellationRequested();
                await _output.WriteAsync(field.RawBytesMemory, cancellationToken).ConfigureAwait(false);
                _fieldsWritten++;
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask WriteRawFieldCoreAsync(ReadOnlyMemory<byte> rawField, CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfEnded();
                cancellationToken.ThrowIfCancellationRequested();
                ImfHeaderFormatter.CheckRawField(rawField.Span, _lineFeedIsLineBreak, _acceptBareLineFeed);
                await _output.WriteAsync(rawField, cancellationToken).ConfigureAwait(false);
                _fieldsWritten++;
                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async Task WriteFieldsCoreAsync(ImfHeaderReader reader,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<ImfWriteProgress>? progress,
#else
            IProgress<ImfWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfEnded();
                while (await reader.ReadFieldAsync(cancellationToken).ConfigureAwait(false) is ImfHeaderFieldView field)
                {
                    _output.ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await _output.WriteAsync(field.RawBytesMemory, cancellationToken).ConfigureAwait(false);
                    _fieldsWritten++;
                    progress?.Report(new ImfWriteProgress(_fieldsWritten, 0, _output.BytePosition));
                }

                _output.ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _output.MarkBroken();
                throw;
            }
            finally
            {
                await EndOperationAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask WriteBufferAsync(int length, CancellationToken cancellationToken)
        {
            await _output.WriteAsync(new ReadOnlyMemory<byte>(_buffer, 0, length), cancellationToken).ConfigureAwait(false);
            _fieldsWritten++;
        }

        /// <summary>
        /// Marks the operation as finished, and releases the writer asynchronously if it was disposed while the operation was in progress.
        /// </summary>
        private async ValueTask EndOperationAsync()
        {
            if (_output.EndOperation())
            {
                await ReleaseAsync().ConfigureAwait(false);
            }
        }

        private async ValueTask ReleaseAsync()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            await _output.ReleaseAsync().ConfigureAwait(false);
        }

        private static FileStream OpenFile(string path)
        {
            if (path is null)
                throw new ArgumentNullException("messageFilePath");

            return ImfFileStreams.OpenWrite(path, ImfOutput.FileBufferSize);
        }
#endif

        private void WriteFormattedField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, ImfHeaderFieldKind kind)
        {
            var length = FormatField(name, value, kind);
            _output.Write(new ReadOnlySpan<byte>(_buffer, 0, length));
            _fieldsWritten++;
        }

        private int FormatField(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value, ImfHeaderFieldKind kind)
        {
            return ImfHeaderFormatter.Format(name, value, kind, _encoding, _lineBreak, ref _buffer);
        }

        private void ThrowIfEnded()
        {
            if (_hasEnded)
                throw new InvalidOperationException("The header section has ended.");
        }

        /// <summary>
        /// Marks the operation as finished, and releases the writer if it was disposed while the operation was in progress.
        /// </summary>
        private void EndOperation()
        {
            if (_output.EndOperation()) Release();
        }

        private void Release()
        {
            try
            {
                _output.Release();
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(_buffer);
            }
        }

        private static void CheckText(string name, string value, ImfHeaderFieldKind kind)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            CheckKind(kind);
        }

        private static void CheckKind(ImfHeaderFieldKind kind)
        {
            if (kind != ImfHeaderFieldKind.Unstructured && kind != ImfHeaderFieldKind.Structured)
                throw new ArgumentOutOfRangeException(nameof(kind));
        }

        private static ImfOptions CheckOptions(ImfOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            return options;
        }
    }
}
