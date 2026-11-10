﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif
using DataStandardizer.File.ContentLine;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to the content lines (RFC 6350 §3.3) of vCard data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Lines are unfolded (RFC 6350 §3.2), and their structure is checked against RFC 6350 §3.3: an optional group, a name, any parameters and a
    /// value.  The meaning of properties and values is not checked, and neither is the structure of a card, so a line may appear outside one.  TEXT
    /// unescaping (RFC 6350 §3.4) depends on the value type, so it is done only when asked for.
    /// </para>
    /// <para>
    /// RFC 6868 caret encoding in parameter values is decoded only on the lines of a vCard 4.0 card, from its <c>VERSION:4.0</c> line up to and
    /// including its <c>END:VCARD</c> line.  The reader tracks this from the lines themselves: <c>BEGIN:VCARD</c> forgets the version, a
    /// <c>VERSION</c> line sets it, and the line after <c>END:VCARD</c> forgets it again.  <see cref="VcardOptions.AcceptCaretEncodingInAllVersions"/>
    /// decodes it on every line.
    /// </para>
    /// <para>
    /// The lines of a vCard 2.1 card, from its <c>VERSION:2.1</c> line up to and including its <c>END:VCARD</c> line, follow the vCard 2.1 specification.  A parameter may have
    /// no name, as in <c>TEL;HOME;VOICE:</c>; it is given as the value of the parameter it belongs to, such as <c>TYPE</c>, and <see cref="VcardContentLine.LegacySyntax"/>
    /// says so.  A quoted-printable value is joined across its soft line breaks, and a <c>BASE64</c> value across its lines, up to the empty line that ends it.  A fold
    /// keeps the white space after its line break (§2.1.3), unless <see cref="VcardOptions.AcceptRfc6350FoldsInVersion21"/> is set.  A nested card, as in an <c>AGENT</c>
    /// property, is part of the card around it.  <see cref="VcardOptions.AcceptLegacySyntax"/> gives every line these forms, other than the folds.  A value isn't decoded unless
    /// asked for; see <see cref="VcardContentLine.GetDecodedText()"/>.
    /// </para>
    /// <para>
    /// Every content line ends with a CRLF line break, including the last (RFC 6350 §3.3).  A bare line feed is accepted only with
    /// <see cref="VcardOptions.AcceptBareLineFeed"/>, a last line without a line break only with <see cref="VcardOptions.AcceptMissingFinalLineBreak"/>,
    /// and a bare carriage return is never a line break.  Line breaks never depend on <see cref="Environment.NewLine"/>.
    /// </para>
    /// <para>
    /// A <see cref="Stream"/> or file is read as strict UTF-8, the only charset of RFC 6350 §3.1, so a byte sequence that isn't valid UTF-8 is an
    /// error, and a byte order mark neither selects an encoding nor is skipped.  See <see cref="VcardOptions.Encoding"/> and
    /// <see cref="VcardOptions.AcceptByteOrderMark"/>.
    /// </para>
    /// <para>
    /// When the data is read from a <see cref="Stream"/> or file as UTF-8, a fold that splits a multi-octet UTF-8 sequence is repaired before
    /// decoding, as RFC 6350 §3.2 says it should be.  A caller-supplied <see cref="TextReader"/> has already decoded the data, so such a fold
    /// cannot be repaired.
    /// </para>
    /// <para>
    /// If a line does not follow the content line syntax, a <see cref="VcardFileException"/> is thrown after the line has been consumed, so
    /// reading can continue with the next line.  If the line break or line length is at fault, where the line ends is unknown, and if the data
    /// can't be decoded or the source throws an exception, the place to resume from is unknown, so later reads throw
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while
    /// another is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class VcardContentLineReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private const int DefaultBufferSize = 16 * 1024;

        /// <summary>
        /// Size, in bytes, of the buffers of the <see cref="StreamReader"/> and <c>FileStream</c> the reader creates itself.  A
        /// caller-supplied <see cref="TextReader"/> keeps its own buffering.
        /// </summary>
        internal const int StreamBufferSize = 16 * 1024;

        /// <summary>
        /// Encoding used when none is specified: UTF-8, the charset of RFC 6350 §3.1.  It throws on invalid data, as the data then breaks the
        /// grammar, and has no preamble, so that a <see cref="StreamReader"/> leaves a byte order mark for the scanner to judge.
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        /// <summary>
        /// Gives the result of <see cref="ReadContentLine"/>: whether a line was read.
        /// </summary>
        private static readonly Func<VcardContentLineReader, bool, bool> LineReadResult = (reader, lineRead) => lineRead;

        /// <summary>
        /// Gives the result of <see cref="ReadLine"/>: the line read, or <c>null</c>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static readonly Func<VcardContentLineReader, bool, VcardContentLine?> LineResult =
#else
        private static readonly Func<VcardContentLineReader, bool, VcardContentLine> LineResult =
#endif
            (reader, lineRead) => lineRead
                ? VcardContentLine.FromScanner(reader._scanner, reader.DecodesCaretEncoding, reader._state.CardIndexOfLine, reader.ValueDecoder, reader.LineVersion)
                : null;

        private readonly bool _acceptCaretEncodingInAllVersions;
        private readonly bool _acceptLegacySyntax;
        private readonly bool _acceptRfc6350FoldsInVersion21;
        private bool _hasCurrentLine;
        private bool _isSourceFaulted;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif

        // These are changed with Interlocked, so that a Dispose from another thread while a read is in progress returns the buffers once only,
        // after that read has finished with them.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        private readonly bool _isInternalReader;
        private int _lineVersion;
        private readonly TextReader _reader;
        private readonly ContentLineScanner _scanner;
        private readonly VcardLexicalState _state = new VcardLexicalState();

        /// <summary>
        /// Gets the text reader that the lines are read from, so that tests can check how a constructor built it.
        /// </summary>
        internal TextReader UnderlyingReader => _reader;

        /// <summary>
        /// Creates a reader for vCard data in a stream, read as UTF-8.
        /// </summary>
        /// <param name="vcardStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="vcardStream"/> does not support reading.</exception>
        public VcardContentLineReader(Stream vcardStream) : this(vcardStream, new VcardOptions { MaxLineLength = null })
        {
        }

        /// <summary>
        /// Creates a reader for vCard data in a stream.
        /// </summary>
        /// <param name="vcardStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="vcardStream"/> does not support reading.
        /// -or-
        /// <see cref="VcardOptions.MaxLineLength"/> is less than 1.</exception>
        public VcardContentLineReader(Stream vcardStream, VcardOptions options) : this(vcardStream, options, StreamBufferSize, DefaultBufferSize)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for a vCard file, read as UTF-8.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> is <c>null</c>.</exception>
        public VcardContentLineReader(string vcardFilePath) : this(vcardFilePath, new VcardOptions { MaxLineLength = null })
        {
        }

        /// <summary>
        /// Creates a reader for a vCard file.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.MaxLineLength"/> is less than 1.</exception>
        public VcardContentLineReader(string vcardFilePath, VcardOptions options) : this(CreateFileReader(vcardFilePath, options), true, options, DefaultBufferSize)
        {
        }
#endif

        /// <summary>
        /// Creates a reader for vCard data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public VcardContentLineReader(TextReader reader) : this(reader, new VcardOptions { MaxLineLength = null })
        {
        }

        /// <summary>
        /// Creates a reader for vCard data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c>, since the text reader has already decoded
        /// the data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="VcardOptions.MaxLineLength"/> is less than 1.</exception>
        public VcardContentLineReader(TextReader reader, VcardOptions options) : this(reader, options, DefaultBufferSize)
        {
        }

        /// <summary>
        /// Creates a reader with specific buffer sizes.  Allows tests to force refills at chosen points.
        /// </summary>
        internal VcardContentLineReader(Stream vcardStream, VcardOptions options, int streamBufferSize, int bufferSize)
            : this(CreateStreamReader(vcardStream, options, streamBufferSize), true, options, bufferSize)
        {
        }

        /// <summary>
        /// Creates a reader with a specific size for the internal character block.  Allows tests to force refills at chosen points.
        /// </summary>
        internal VcardContentLineReader(TextReader reader, VcardOptions options, int bufferSize) : this(ValidateTextReader(reader, options), false, options, bufferSize)
        {
        }

        private VcardContentLineReader(TextReader reader, bool isInternalReader, VcardOptions options, int bufferSize)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _reader = reader;
            _isInternalReader = isInternalReader;
            _acceptCaretEncodingInAllVersions = options.AcceptCaretEncodingInAllVersions;
            _acceptLegacySyntax = options.AcceptLegacySyntax;
            _acceptRfc6350FoldsInVersion21 = options.AcceptRfc6350FoldsInVersion21;
            ValueDecoder = new VcardValueDecoder(options);

            // ref. RFC 6350 §3.3: contentline = [group "."] name *(";" param) ":" value CRLF
            var settings = new ContentLineScannerSettings(options.AcceptBareLineFeed, options.MaxLineLength ?? int.MaxValue, allowGroupPrefix: true,
                rejectByteOrderMark: !options.AcceptByteOrderMark, requireFinalLineBreak: !options.AcceptMissingFinalLineBreak,
                continuation: new VcardLineContinuation(_state, options.AcceptLegacySyntax));
            _scanner = new ContentLineScanner(bufferSize, settings, new VcardContentLineErrorFactory(_state));
        }

        /// <summary>
        /// Gets a view of the line last read by <see cref="ReadContentLine"/> or <see cref="ReadLine"/>, without allocation.
        /// </summary>
        /// <remarks>
        /// The view, and any span or memory taken from it, is valid only until the next read or until the reader is disposed.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">No line is current: none has been read yet, the last read reached the end of the data, or
        /// it threw an exception.</exception>
        public VcardContentLineView CurrentLine
        {
            get
            {
                ThrowIfDisposed();
                if (!_hasCurrentLine)
                    throw new InvalidOperationException("No content line is current.");

                return new VcardContentLineView(this, _lineVersion);
            }
        }

        /// <summary>
        /// Gets the number of logical (unfolded) lines read so far, including any that failed to parse.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long LinesRead
        {
            get
            {
                ThrowIfDisposed();
                return _scanner.LinesRead;
            }
        }

        /// <summary>
        /// Gets the number of physical lines read so far.  A folded logical line spans several physical lines.
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
        /// Releases the reader's buffers, and the stream or file it opened itself.  A caller-supplied stream or text reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the buffers and
        /// stream are released once it has finished with them.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            _hasCurrentLine = false;

            if (Volatile.Read(ref _isReadInProgress) == 0)
            {
                Release();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Releases the reader's buffers, and the stream or file it opened itself.  A caller-supplied stream or text reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the buffers and stream are released once it has finished with them.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
#endif

        /// <summary>
        /// Reads the next content line, without allocation.  The line is then available from <see cref="CurrentLine"/>.
        /// </summary>
        /// <returns><c>true</c> if a line was read; <c>false</c> if the end of the data has been reached.</returns>
        /// <exception cref="VcardFileException">The line does not follow the content line syntax of RFC 6350 §3.3, has a line break that is not
        /// accepted, or is longer than <see cref="VcardOptions.MaxLineLength"/>.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break, line length or decoding error, its source threw an exception, or it was canceled, so the reader cannot
        /// continue.</exception>
        public bool ReadContentLine()
        {
            return Read(LineReadResult);
        }

        /// <summary>
        /// Reads the next content line.
        /// </summary>
        /// <returns>The line; or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="VcardFileException">The line does not follow the content line syntax of RFC 6350 §3.3, has a line break that is not
        /// accepted, or is longer than <see cref="VcardOptions.MaxLineLength"/>.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break, line length or decoding error, its source threw an exception, or it was canceled, so the reader cannot
        /// continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardContentLine? ReadLine()
#else
        public VcardContentLine ReadLine()
#endif
        {
            return Read(LineResult);
        }

        /// <summary>
        /// Reads the remaining content lines.
        /// </summary>
        /// <returns>The lines, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="VcardFileException">During enumeration: a line does not follow the content line syntax, has a line break that is not
        /// accepted, or is too long.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read found a line break,
        /// line length or decoding error or was canceled.</exception>
        public IEnumerable<VcardContentLine> ReadLines()
        {
            // Check now, so the exception comes from this call and not from the first MoveNext.
            ThrowIfDisposed();

            return ReadLinesIterator();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the next content line asynchronously, without allocation.  The line is then available from <see cref="CurrentLine"/>.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the reader's buffer already holds the whole line.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The read is canceled only if this token is
        /// canceled, and the reader then cannot continue.</param>
        /// <returns>A task whose result is <c>true</c> if a line was read, or <c>false</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break, line length or decoding error, its source threw an exception, or it was canceled, so the reader cannot
        /// continue.</exception>
        /// <exception cref="VcardFileException">By the task: the line does not follow the content line syntax of RFC 6350 §3.3, has a line break
        /// that is not accepted, or is longer than <see cref="VcardOptions.MaxLineLength"/>.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// read failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  An <see cref="OperationCanceledException"/> from the source while the token isn't canceled
        /// faults the task, as any other failure of the source does, and the reader then cannot continue.</exception>
        public ValueTask<bool> ReadContentLineAsync(CancellationToken cancellationToken = default)
        {
            return ReadAsync(LineReadResult, cancellationToken);
        }

        /// <summary>
        /// Reads the next content line asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the reader's buffer already holds the whole line.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The read is canceled only if this token is
        /// canceled, and the reader then cannot continue.</param>
        /// <returns>A task whose result is the line, or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read found a line break, line length or decoding error, its source threw an exception, or it was canceled, so the reader cannot
        /// continue.</exception>
        /// <exception cref="VcardFileException">By the task: the line does not follow the content line syntax of RFC 6350 §3.3, has a line break
        /// that is not accepted, or is longer than <see cref="VcardOptions.MaxLineLength"/>.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// read failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  An <see cref="OperationCanceledException"/> from the source while the token isn't canceled
        /// faults the task, as any other failure of the source does, and the reader then cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ValueTask<VcardContentLine?> ReadLineAsync(CancellationToken cancellationToken = default)
#else
        public ValueTask<VcardContentLine> ReadLineAsync(CancellationToken cancellationToken = default)
#endif
        {
            return ReadAsync(LineResult, cancellationToken);
        }

        /// <summary>
        /// Reads the remaining content lines asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is combined with any token passed to
        /// <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.  A read is canceled only if this token is canceled, and the
        /// reader then cannot continue.</param>
        /// <returns>The lines, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="VcardFileException">During enumeration: a line does not follow the content line syntax, has a line break that is not
        /// accepted, or is too long.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read found a line break,
        /// line length or decoding error or was canceled.</exception>
        /// <exception cref="OperationCanceledException">During enumeration: a read was canceled, which it is only if the token is canceled; or
        /// the source failed with one while the token isn't canceled, which leaves the reader unable to continue, as any other failure of the
        /// source does.  On every target, the task of <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> is canceled for either, as an async
        /// iterator's always is for an <see cref="OperationCanceledException"/>, so check the token to tell which it was.</exception>
        public IAsyncEnumerable<VcardContentLine> ReadLinesAsync(CancellationToken cancellationToken = default)
        {
            // Check now, so the exception comes from this call and not from the first MoveNextAsync.
            ThrowIfDisposed();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            return ReadLinesAsyncIterator(cancellationToken);
#else
            return new AsyncLineEnumerable(this, cancellationToken);
#endif
        }
#endif

        /// <summary>
        /// Gets a flag indicating if RFC 6868 caret encoding is decoded on the current line.
        /// </summary>
        internal bool DecodesCaretEncoding => _acceptCaretEncodingInAllVersions || _state.IsVersion40;

        /// <summary>
        /// Gets a flag indicating if the current line is in a card that has said it is vCard 2.1.
        /// </summary>
        internal bool IsVersion21Line => _state.IsVersion21;

        /// <summary>
        /// Gets the version of the card the current line is in, as far as a line's value depends on it.
        /// </summary>
        internal VcardLineVersion LineVersion =>
            _state.IsVersion21 ? VcardLineVersion.Version21 : _state.IsVersion40 ? VcardLineVersion.Version40 : VcardLineVersion.Other;

        /// <summary>
        /// Gets what decodes the values of the lines read.
        /// </summary>
        internal VcardValueDecoder ValueDecoder { get; }

        /// <summary>
        /// Gets the 0-based index of the card the current line is in, or -1 if it is outside a card.
        /// </summary>
        internal int CardIndexOfLine => _state.CardIndexOfLine;

        /// <summary>
        /// Gets the scanner holding the current line, for a view of that line.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The reader has moved on from the line the view was taken for.</exception>
        internal ContentLineScanner GetScanner(int lineVersion)
        {
            ThrowIfDisposed();
            if (!_hasCurrentLine || lineVersion != _lineVersion)
                throw new InvalidOperationException("The reader has moved on from the content line of this view.");

            return _scanner;
        }

        private IEnumerable<VcardContentLine> ReadLinesIterator()
        {
            while (true)
            {
                var line = ReadLine();
                if (line is null) yield break;

                yield return line;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async IAsyncEnumerable<VcardContentLine> ReadLinesAsyncIterator([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (true)
            {
                var line = await ReadLineAsync(cancellationToken).ConfigureAwait(false);
                if (line is null) yield break;

                yield return line;
            }
        }
#endif

        /// <summary>
        /// Drives the scanner synchronously until it has a whole line or the data ends.
        /// </summary>
        /// <param name="result">Gives the result from whether a line was read.  It is called before the read ends, while the line is still
        /// current.</param>
        private TResult Read<TResult>(Func<VcardContentLineReader, bool, TResult> result)
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                StartLine();
                while (true)
                {
                    var status = _scanner.Scan();
                    if (status != ContentLineScanner.ScanStatus.NeedMoreInput)
                    {
                        return result(this, CompleteLine(status));
                    }

                    int offset, count;
                    _scanner.PrepareRefill(out offset, out count);
                    int charactersRead;
                    try
                    {
                        charactersRead = _reader.Read(_scanner.Buffer, offset, count);
                    }
                    catch (DecoderFallbackException exception) when (_isInternalReader)
                    {
                        throw FaultDecoding(exception);
                    }
                    catch (Exception)
                    {
                        // Characters may have been lost, so a later read could join what follows them onto this line.
                        _isSourceFaulted = true;
                        throw;
                    }

                    ThrowIfDisposed();
                    _scanner.CompleteRefill(charactersRead);
                }
            }
            finally
            {
                EndRead();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Drives the scanner asynchronously until it has a whole line or the data ends.  Completes synchronously, with no state machine, when no
        /// refill is needed.
        /// </summary>
        /// <param name="result">Gives the result from whether a line was read.  It is called before the read ends, while the line is still
        /// current.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        private ValueTask<TResult> ReadAsync<TResult>(Func<VcardContentLineReader, bool, TResult> result, CancellationToken cancellationToken)
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            var isPending = false;
            try
            {
                StartLine();
                if (cancellationToken.IsCancellationRequested)
                {
                    _isCanceled = true;
                    return new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));
                }

                try
                {
                    var status = _scanner.Scan();
                    if (status != ContentLineScanner.ScanStatus.NeedMoreInput)
                    {
                        return new ValueTask<TResult>(result(this, CompleteLine(status)));
                    }
                }
                catch (VcardFileException exception)
                {
                    return new ValueTask<TResult>(Task.FromException<TResult>(exception));
                }

                isPending = true;
                return AsyncOutcome.Unwrap(ReadWithRefillAsync(result, cancellationToken));
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
        /// Continues <see cref="ReadAsync{TResult}"/> once the scanner needs more input, and ends the read.
        /// </summary>
        private async ValueTask<ValueTask<TResult>> ReadWithRefillAsync<TResult>(Func<VcardContentLineReader, bool, TResult> result, CancellationToken cancellationToken)
        {
            ValueTask<TResult> outcome;
            try
            {
                ContentLineScanner.ScanStatus status;
                do
                {
                    int offset, count;
                    _scanner.PrepareRefill(out offset, out count);
                    int charactersRead;
                    try
                    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                        charactersRead = await _reader.ReadAsync(new Memory<char>(_scanner.Buffer, offset, count), cancellationToken).ConfigureAwait(false);
#else
                        // This overload takes no cancellation token, so check before each read instead.
                        cancellationToken.ThrowIfCancellationRequested();
                        charactersRead = await _reader.ReadAsync(_scanner.Buffer, offset, count).ConfigureAwait(false);
#endif
                    }
                    catch (Exception exception) when (!(exception is OperationCanceledException) && !(exception is DecoderFallbackException && _isInternalReader))
                    {
                        // Characters may have been lost, so a later read could join what follows them onto this line.
                        _isSourceFaulted = true;
                        throw;
                    }

                    ThrowIfDisposed();
                    _scanner.CompleteRefill(charactersRead);
                    status = _scanner.Scan();
                } while (status == ContentLineScanner.ScanStatus.NeedMoreInput);

                outcome = new ValueTask<TResult>(result(this, CompleteLine(status)));
            }
            catch (DecoderFallbackException exception) when (_isInternalReader)
            {
                outcome = new ValueTask<TResult>(Task.FromException<TResult>(FaultDecoding(exception)));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // A cancellation, whatever token the exception carries, or if it carries none.
                _isCanceled = true;
                outcome = new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));
            }
            catch (OperationCanceledException exception)
            {
                // Only the text reader throws one while the token isn't canceled.  That is a failure of the source, as any other exception
                // from it is: characters may have been lost, so a later read could join what follows them onto this line.
                _isSourceFaulted = true;
                outcome = new ValueTask<TResult>(Task.FromException<TResult>(exception));
            }
            catch (Exception exception)
            {
                outcome = new ValueTask<TResult>(Task.FromException<TResult>(exception));
            }
            finally
            {
                // The release disposes only a file that the reader opened itself, which doesn't throw an OperationCanceledException.
                EndRead();
            }

            return outcome;
        }
#endif

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
        /// Marks the read as finished, and releases the buffers and stream if the reader was disposed while it was in progress.
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
        /// Checks that the reader can continue, and moves on from the current line.
        /// </summary>
        /// <exception cref="InvalidOperationException">An earlier read found a line break, line length or decoding error, its source threw an
        /// exception, or it was canceled.</exception>
        private void StartLine()
        {
            if (_scanner.IsFaulted)
                throw new InvalidOperationException("The reader cannot continue after a line break or line length error.");
            if (_isSourceFaulted)
                throw new InvalidOperationException("The reader cannot continue after a failure reading or decoding its source.");
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
                throw new InvalidOperationException("The reader cannot continue after a read was canceled.");
#endif

            // Any view of the previous line is now out of date.
            unchecked
            {
                _lineVersion++;
            }

            _hasCurrentLine = false;
            _state.MoveToNextLine();

            // ref. vCard 2.1 §2.1.3 and §2.9.  The version can only change on a line that is read as the one before it says.
            _scanner.AllowValuelessParameters = _state.IsVersion21 || _acceptLegacySyntax;
            _scanner.KeepFoldWhitespace = _state.IsVersion21 && !_acceptRfc6350FoldsInVersion21;
        }

        /// <summary>
        /// Builds the exception for data that can't be decoded, and faults the reader, since the place to resume from is unknown.
        /// </summary>
        private VcardFileException FaultDecoding(DecoderFallbackException exception)
        {
            _isSourceFaulted = true;
            return VcardContentLineErrorFactory.BuildDecodingException(exception);
        }

        /// <summary>
        /// Parses the line found by the scanner, if any, and makes it current.
        /// </summary>
        /// <returns><c>true</c> if a line was read; <c>false</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader was disposed during the read, which needed no refill to get this far.</exception>
        internal bool CompleteLine(ContentLineScanner.ScanStatus status)
        {
            ThrowIfDisposed();
            if (status != ContentLineScanner.ScanStatus.LineRead) return false;

            _scanner.ParseLine();
            CheckValuelessParameters();
            _state.ObserveLine(_scanner);
            _hasCurrentLine = true;
            return true;
        }

        /// <summary>
        /// Rejects a parameter with no name that vCard 2.1 doesn't allow, unless <see cref="VcardOptions.AcceptLegacySyntax"/> accepts it.
        /// </summary>
        private void CheckValuelessParameters()
        {
            if (_acceptLegacySyntax) return;

            for (var parameterIndex = 0; parameterIndex < _scanner.ParameterCount; parameterIndex++)
            {
                if (_scanner.IsParameterValueless(parameterIndex) &&
                    !VcardLegacyTokens.IsValuelessAllowedInVersion21(_scanner.GetParameterName(parameterIndex).Span))
                {
                    throw _scanner.BuildValuelessParameterError(parameterIndex);
                }
            }
        }

        /// <summary>
        /// Releases the buffers, and the stream or file the reader opened itself, the first time it is called.
        /// </summary>
        private void Release()
        {
            if (Interlocked.Exchange(ref _isReleased, 1) != 0) return;

            if (_isInternalReader)
            {
                _reader.Dispose();
            }

            _scanner.ReturnBuffers();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

        private static TextReader CreateStreamReader(Stream stream, VcardOptions options, int bufferSize)
        {
            if (stream is null)
                throw new ArgumentNullException("vcardStream");
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "vcardStream");
            ValidateOptions(options);

            return CreateStreamReader(stream, false, options.Encoding ?? DefaultEncoding, bufferSize);
        }

        /// <summary>
        /// Creates the stream reader, repairing folds inside UTF-8 sequences when the data is UTF-8.  A byte order mark never selects the encoding,
        /// and the stream reader never removes one, so that only <see cref="VcardOptions.AcceptByteOrderMark"/> decides whether it is skipped.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="ownsStream"><c>true</c> if disposing the stream reader should dispose <paramref name="stream"/>.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="bufferSize">The buffer size, in bytes.</param>
        private static StreamReader CreateStreamReader(Stream stream, bool ownsStream, Encoding encoding, int bufferSize)
        {
            // A StreamReader removes the encoding's preamble from the start of the data, whatever detectEncodingFromByteOrderMarks says.
            var decodingEncoding = encoding.GetPreamble().Length == 0 ? encoding : new PreamblelessEncoding(encoding);
            if (encoding is UTF8Encoding)
            {
                var foldRealigningStream = new Utf8FoldRealigningStream(stream, !ownsStream, bufferSize);
                return new StreamReader(foldRealigningStream, decodingEncoding, false, bufferSize, false);
            }

            return new StreamReader(stream, decodingEncoding, false, bufferSize, !ownsStream);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static StreamReader CreateFileReader(string path, VcardOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("vcardFilePath");
            ValidateOptions(options);

            // Open the file stream explicitly so both buffering layers are sized together.
            var fileStream = VcardFileStreams.OpenRead(path, StreamBufferSize);
            try
            {
                return CreateStreamReader(fileStream, true, options.Encoding ?? DefaultEncoding, StreamBufferSize);
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
        }
#endif

        private static TextReader ValidateTextReader(TextReader reader, VcardOptions options)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));
            ValidateOptions(options);
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing reader.  Specify the encoding when creating the reader.", nameof(options));

            return reader;
        }

        private static void ValidateOptions(VcardOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.MaxLineLength < 1)
                throw new ArgumentException("The maximum line length must be at least 1.", nameof(options));
        }

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER) && NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// The lines of <see cref="ReadLinesAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncLineEnumerable : IAsyncEnumerable<VcardContentLine>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly VcardContentLineReader _reader;

            internal AsyncLineEnumerable(VcardContentLineReader reader, CancellationToken cancellationToken)
            {
                _reader = reader;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<VcardContentLine> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
            {
                return new Enumerator(_reader, _cancellationToken, cancellationToken);
            }

            private sealed class Enumerator : IAsyncEnumerator<VcardContentLine>
            {
                private readonly CancellationToken _cancellationToken;
                private bool _isFinished;
                private readonly CancellationTokenSource _linkedTokenSource;
                private readonly VcardContentLineReader _reader;

                internal Enumerator(VcardContentLineReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
                {
                    _reader = reader;

                    // Combine the tokens as an async iterator with [EnumeratorCancellation] does.
                    if (readerToken.CanBeCanceled && enumeratorToken.CanBeCanceled && readerToken != enumeratorToken)
                    {
                        _linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(readerToken, enumeratorToken);
                        _cancellationToken = _linkedTokenSource.Token;
                    }
                    else
                    {
                        _cancellationToken = readerToken.CanBeCanceled ? readerToken : enumeratorToken;
                    }
                }

                public VcardContentLine Current { get; private set; }

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (_isFinished) return false;

                    var line = await _reader.ReadLineAsync(_cancellationToken).ConfigureAwait(false);
                    Current = line;
                    _isFinished = line is null;
                    return !_isFinished;
                }

                public ValueTask DisposeAsync()
                {
                    _isFinished = true;
                    Current = null;
                    _linkedTokenSource?.Dispose();
                    return default(ValueTask);
                }
            }
        }
#endif
    }
}
