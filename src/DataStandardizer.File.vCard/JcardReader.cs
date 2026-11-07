#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a reader of jCard data, the JSON form of vCard (RFC 7095), into the card object model: a sequence of <see cref="VcardCard"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is a JSON document in UTF-8, which may start with a byte order mark.  It is either the jCard object of one card, <c>["vcard", [...]]</c>, or an array of them (RFC 7095
    /// §3.2).  The cards are read one at a time, so a document need not fit in memory.  Property and parameter names are converted to upper case (RFC 7095 §4), the group parameter
    /// becomes the group of the property, in upper case (§3.3.1.2.1), and each value is converted to the text form of RFC 6350 (§3.1), so the cards are the same as those
    /// <see cref="VcardReader"/> reads from the same data in <c>.vcf</c> form, and can be written in either form.  In particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>A value whose type identifier is not the property's default adds a <c>VALUE</c> parameter, after the other parameters (RFC 7095 §3.4.1).</description></item>
    /// <item><description>A value with the <c>unknown</c> type is kept as it is, with no <c>VALUE</c> parameter (RFC 7095 §5.2).  A type identifier that is not one of RFC 6350's, such
    /// as <c>x-mytype</c>, is an extension value type: the value is kept as it is, with a <c>VALUE</c> parameter naming the type.</description></item>
    /// <item><description>The values of a multi-valued property, such as <c>CATEGORIES</c>, are joined into a list (RFC 7095 §3.3), and the elements of a structured value, such as
    /// that of <c>N</c>, into its components (§3.3.1.3).  A structured value may also be a single string, which is its one component.</description></item>
    /// <item><description>An INTEGER that is a JSON number with a fraction or an exponent is taken without them, its fraction rounded toward zero, and a FLOAT without its exponent, as
    /// RFC 7095 §3.5.9 and §3.5.10 say they "MUST be eliminated".</description></item>
    /// <item><description>A string is accepted as the value of any type, so that a value that does not follow its type is kept.  A number is accepted only for INTEGER and FLOAT,
    /// <c>true</c> and <c>false</c> only for BOOLEAN, and an array only for a structured value: that of N, ADR, ORG, GENDER or CLIENTPIDMAP, or of a property whose type isn't
    /// known.</description></item>
    /// <item><description>A value that isn't in the form jCard gives its type, such as a string for an INTEGER or BOOLEAN, or a date in the basic format of ISO 8601, is kept as it
    /// is and reported as <see cref="VcardDiagnosticKind.InvalidValue"/>, even where its text is a valid value of vCard.</description></item>
    /// </list>
    /// <para>
    /// A document that is not valid JSON, or does not follow the structure of jCard, throws <see cref="VcardFileException"/>.  Its data holds the 0-based offset of the JSON token in
    /// bytes under <see cref="VcardFileException.DataItemName.BytePosition"/> and, where the token has been read, the JSONPath of the value under
    /// <see cref="VcardFileException.DataItemName.JsonPath"/>, such as <c>$[1][3][0]</c>.  For data that is not valid JSON, it also holds the line and the byte position on the line
    /// under <see cref="VcardFileException.DataItemName.LineNumber"/> and <see cref="VcardFileException.DataItemName.LinePosition"/>.  The structure of jCard includes these rules of
    /// RFC 7095, each a MUST: names are in lower case (§3.3 and §3.4); <c>BEGIN</c> and <c>END</c> are not properties (§3.2); the parameters object has no <c>value</c> parameter
    /// (§3.4.1); and the group parameter is a string of letters, digits and hyphens (§7.1).  A <c>VERSION</c> other than 4.0 throws too, since jCard represents vCard 4.0 alone.
    /// </para>
    /// <para>
    /// Each card is checked once its jCard object has been read, as <see cref="VcardCard.Validate()"/> does, and for its <c>VERSION</c>, against the same rules and options as
    /// <see cref="VcardReader"/>.  The diagnostics have no line numbers, since a jCard document has no content lines; their card index and property give the place.  The first problem
    /// that the options do not accept throws <see cref="VcardFileException"/>; reading can then continue with the next card.  A value that does not follow its type is kept as it is,
    /// so the check reports it, and <see cref="VcardOptions.AllowInvalidValues"/> lets it through.  Of the other options, those that concern <c>.vcf</c> content lines, such as
    /// <see cref="VcardOptions.MaxLineLength"/>, do not apply, and <see cref="VcardOptions.Encoding"/> must be UTF-8 if it is set.
    /// </para>
    /// <para>
    /// The JSON is read with <see cref="Utf8JsonReader"/> over pooled blocks of bytes, so a read does no I/O of its own beyond filling the next block; the asynchronous members fill it
    /// asynchronously, and do no synchronous I/O.  Any other exception from a read, including a cancellation, leaves the reader unable to continue, and later reads throw
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while another is in progress throws
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// jCard needs System.Text.Json, so this type is not available on netstandard1.1.
    /// </para>
    /// </remarks>
    public sealed class JcardReader : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The size of the first block of bytes; a block grows when a single JSON token does not fit it.
        /// </summary>
        private const int InitialBufferSize = 16384;

        private enum ReadResult
        {
            Card,
            NeedData,
            End
        }

        private readonly JcardDocumentBuilder _builder;
        private readonly VcardOptions _options;
        private readonly bool _ownsStream;
        private readonly Stream _stream;

        private byte[] _buffer;

        // The absolute offset, in the data, of the byte at _start.
        private long _bufferPosition;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? _card;
#else
        private VcardCard _card;
#endif
        private int _end;
        private bool _isByteOrderMarkChecked;
        private bool _isEndOfData;
        private bool _isFaulted;
        private bool _isFinished;
        private int _start;
        private JsonReaderState _state;

        // These are changed with Interlocked, so that a read started on another thread while one is in progress is refused, and a Dispose from another thread releases the reader
        // once that read has finished.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a reader for jCard data in a stream.
        /// </summary>
        /// <param name="jcardStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcardStream"/> does not support reading.</exception>
        public JcardReader(Stream jcardStream) : this(jcardStream, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a reader for jCard data in a stream.
        /// </summary>
        /// <param name="jcardStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCard is always UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcardStream"/> does not support reading.
        /// -or-
        /// <see cref="VcardOptions.Encoding"/> is not UTF-8.</exception>
        public JcardReader(Stream jcardStream, VcardOptions options) : this(ValidateStream(jcardStream, options), false, options)
        {
        }

        /// <summary>
        /// Creates a reader for a jCard file.
        /// </summary>
        /// <param name="jcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardFilePath"/> is <c>null</c>.</exception>
        public JcardReader(string jcardFilePath) : this(jcardFilePath, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a reader for a jCard file.
        /// </summary>
        /// <param name="jcardFilePath">The path of the file.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCard is always UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is not UTF-8.</exception>
        public JcardReader(string jcardFilePath, VcardOptions options) : this(OpenFile(jcardFilePath, options), true, options)
        {
        }

        private JcardReader(Stream stream, bool ownsStream, VcardOptions options)
        {
            _stream = stream;
            _ownsStream = ownsStream;
            _options = options;
            _builder = new JcardDocumentBuilder(options);
            _buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
        }

        /// <summary>
        /// Gets the number of cards read so far, including any that failed a conformance check.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long CardsRead
        {
            get
            {
                ThrowIfDisposed();
                return _builder.CardsRead;
            }
        }

        /// <summary>
        /// Gets the number of properties read so far, in all cards, not counting <c>VERSION</c>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long PropertiesRead
        {
            get
            {
                ThrowIfDisposed();
                return _builder.PropertiesRead;
            }
        }

        /// <summary>
        /// Releases the pooled buffer, and the file the reader opened.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the reader is released once that read has finished
        /// with it.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            if (Volatile.Read(ref _isReadInProgress) == 0 && MarkReleased())
            {
                Release();
            }
        }

        /// <summary>
        /// Releases the pooled buffer, and the file the reader opened.  A caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the
        /// reader is released once that read has finished with it.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default(ValueTask);
        }

        /// <summary>
        /// Reads the next card.
        /// </summary>
        /// <returns>The card; or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="VcardFileException">The data is not valid jCard: it is not valid JSON, a value is not allowed where it is or is not valid, the <c>VERSION</c> is not 4.0, or
        /// the data holds no card.
        /// -or-
        /// The card breaks a rule of RFC 6350 that the options do not accept.  The card has been read, and reading can continue.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public VcardCard? ReadCard()
#else
        public VcardCard ReadCard()
#endif
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                while (true)
                {
                    ThrowIfDisposed();
                    var result = Process();
                    if (result != ReadResult.NeedData) return TakeCard(result);

                    try
                    {
                        PrepareFill();
                        CompleteFill(_stream.Read(_buffer, _end, _buffer.Length - _end));
                    }
                    catch
                    {
                        _isFaulted = true;
                        throw;
                    }
                }
            }
            finally
            {
                EndRead();
            }
        }

        /// <summary>
        /// Reads the remaining cards.
        /// </summary>
        /// <returns>The cards, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="VcardFileException">During enumeration: the data is not valid jCard, or a card breaks a rule of RFC 6350 that the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was canceled.</exception>
        public IEnumerable<VcardCard> ReadCards()
        {
            // Check now, so the exception comes from this call and not from the first MoveNext.
            ThrowIfDisposed();

            return ReadCardsIterator();
        }

        /// <summary>
        /// Reads the next card asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the buffer already holds the whole card.  Cancellation is checked before each block of the data is read.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The read is canceled only if this token is
        /// canceled, and the reader then cannot continue.</param>
        /// <returns>A task whose result is the card, or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed while the read is in
        /// progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="VcardFileException">By the task: the data is not valid jCard, or the card breaks a rule of RFC 6350 that the options do not accept.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// read failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  An <see cref="OperationCanceledException"/> from the stream while the token isn't canceled
        /// faults the task, as any other failure of the stream does.  Either way, the reader then cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ValueTask<VcardCard?> ReadCardAsync(CancellationToken cancellationToken = default)
#else
        public ValueTask<VcardCard> ReadCardAsync(CancellationToken cancellationToken = default)
#endif
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            return AsyncOutcome.Unwrap(ReadCardOutcomeAsync(cancellationToken));
        }

        /// <summary>
        /// Reads the remaining cards asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is combined with any token passed to
        /// <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.  A read is canceled only if this token is canceled, and the reader then
        /// cannot continue.</param>
        /// <returns>The cards, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="VcardFileException">During enumeration: the data is not valid jCard, or a card breaks a rule of RFC 6350 that the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was canceled.</exception>
        /// <exception cref="OperationCanceledException">During enumeration: a read was canceled, which it is only if the token is canceled; or
        /// the stream failed with one while the token isn't canceled.  Either one leaves the reader unable to continue.  On every target, the
        /// task of <see cref="IAsyncEnumerator{T}.MoveNextAsync"/> is canceled for either, as an async iterator's always is for an
        /// <see cref="OperationCanceledException"/>, so check the token to tell which it was.</exception>
        public IAsyncEnumerable<VcardCard> ReadCardsAsync(CancellationToken cancellationToken = default)
        {
            // Check now, so the exception comes from this call and not from the first MoveNextAsync.
            ThrowIfDisposed();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            return ReadCardsAsyncIterator(cancellationToken);
#else
            return new AsyncCardEnumerable(this, cancellationToken);
#endif
        }

        private IEnumerable<VcardCard> ReadCardsIterator()
        {
            while (true)
            {
                var card = ReadCard();
                if (card is null) yield break;

                yield return card;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async IAsyncEnumerable<VcardCard> ReadCardsAsyncIterator([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (true)
            {
                var card = await ReadCardAsync(cancellationToken).ConfigureAwait(false);
                if (card is null) yield break;

                yield return card;
            }
        }
#endif

        /// <summary>
        /// Continues <see cref="ReadCardAsync"/>, and ends the read.  Completes synchronously, without allocating a task, while the buffer holds the data.
        /// Returns the read's outcome; see <see cref="AsyncOutcome"/>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async ValueTask<ValueTask<VcardCard?>> ReadCardOutcomeAsync(CancellationToken cancellationToken)
#else
        private async ValueTask<ValueTask<VcardCard>> ReadCardOutcomeAsync(CancellationToken cancellationToken)
#endif
        {
            try
            {
                while (true)
                {
                    ThrowIfDisposed();
                    var result = Process();
                    if (result != ReadResult.NeedData)
                    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                        return new ValueTask<VcardCard?>(TakeCard(result));
#else
                        return new ValueTask<VcardCard>(TakeCard(result));
#endif
                    }

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        PrepareFill();
                        CompleteFill(await _stream.ReadAsync(_buffer, _end, _buffer.Length - _end, cancellationToken).ConfigureAwait(false));
                    }
                    catch
                    {
                        _isFaulted = true;
                        throw;
                    }
                }
            }
            catch (Exception exception)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                return AsyncOutcome.Fail<VcardCard?>(exception, cancellationToken);
#else
                return AsyncOutcome.Fail<VcardCard>(exception, cancellationToken);
#endif
            }
            finally
            {
                EndRead();
            }
        }

        /// <summary>
        /// Reads the tokens in the buffer, until a card ends or more data is needed.
        /// </summary>
        /// <exception cref="VcardFileException">The data is not valid jCard, or the card that ended breaks a rule of RFC 6350 that the options do not accept.</exception>
        private ReadResult Process()
        {
            if (_isFinished) return ReadResult.End;

            ReadResult result;
            try
            {
                result = ReadTokens();
            }
            catch
            {
                // Where the card being built ends is now unknown.
                _isFaulted = true;
                throw;
            }

            if (result != ReadResult.Card) return result;

            // The card has been read in full, so a problem with it does not stop the reader.  ref. RFC 7095 §3.3.1.1: the card is of vCard 4.0, so it is checked by those rules,
            // after the problems found in its VERSION.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            var card = _card!;
#else
            var card = _card;
#endif
            var diagnostics = _builder.TakeStructureDiagnostics();
            VcardConformance.Validate(card, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!VcardConformance.IsAccepted(diagnostic.Kind, _options, false))
                {
                    _card = null;
                    throw VcardConformance.BuildException(diagnostic);
                }
            }

            return ReadResult.Card;
        }

        private ReadResult ReadTokens()
        {
            // ref. RFC 8259 §8.1: a parser MAY ignore a byte order mark.  It is skipped before the JSON reader sees it.
            if (!_isByteOrderMarkChecked)
            {
                if (_end - _start < 3 && !_isEndOfData) return ReadResult.NeedData;

                if (_end - _start >= 3 && _buffer[_start] == 0xEF && _buffer[_start + 1] == 0xBB && _buffer[_start + 2] == 0xBF)
                {
                    _start += 3;
                    _bufferPosition += 3;
                }

                _isByteOrderMarkChecked = true;
            }

            var reader = new Utf8JsonReader(new ReadOnlySpan<byte>(_buffer, _start, _end - _start), _isEndOfData, _state);
            try
            {
                while (reader.Read())
                {
                    var card = _builder.Add(reader.TokenType, GetText(ref reader), _bufferPosition + reader.TokenStartIndex);
                    if (card != null)
                    {
                        Consume(ref reader);
                        _card = card;
                        return ReadResult.Card;
                    }
                }
            }
            catch (JsonException exception)
            {
                throw BuildJsonException(exception, _bufferPosition + reader.BytesConsumed);
            }

            Consume(ref reader);
            if (!_isEndOfData) return ReadResult.NeedData;

            _isFinished = true;
            try
            {
                _builder.Complete();
            }
            catch (VcardFileException exception)
            {
                // The data held no card, which is found at its end.
                exception.Data[VcardFileException.DataItemName.BytePosition] = _bufferPosition;
                throw;
            }

            return ReadResult.End;
        }

        /// <summary>
        /// Moves past the bytes the JSON reader has consumed, and keeps its state for the next block.
        /// </summary>
        private void Consume(ref Utf8JsonReader reader)
        {
            var consumed = (int)reader.BytesConsumed;
            _start += consumed;
            _bufferPosition += consumed;
            _state = reader.CurrentState;
        }

        /// <summary>
        /// Makes room in the buffer for more data: moves the bytes not yet consumed to its start, and grows it if they fill it.
        /// </summary>
        private void PrepareFill()
        {
            var length = _end - _start;
            if (length == _buffer.Length)
            {
                // A single token does not fit the buffer.
                var buffer = ArrayPool<byte>.Shared.Rent(_buffer.Length * 2);
                Buffer.BlockCopy(_buffer, _start, buffer, 0, length);
                ArrayPool<byte>.Shared.Return(_buffer);
                _buffer = buffer;
            }
            else if (_start != 0)
            {
                Buffer.BlockCopy(_buffer, _start, _buffer, 0, length);
            }

            _start = 0;
            _end = length;
        }

        private void CompleteFill(int count)
        {
            if (count == 0)
            {
                _isEndOfData = true;
            }
            else
            {
                _end += count;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? GetText(ref Utf8JsonReader reader)
#else
        private string GetText(ref Utf8JsonReader reader)
#endif
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                case JsonTokenType.PropertyName:
                    try
                    {
                        return reader.GetString();
                    }
                    catch (InvalidOperationException exception)
                    {
                        // The string is not valid UTF-8, or escapes a lone surrogate.
                        var fileException = new VcardFileException("The data holds a JSON string that is not valid Unicode text.", exception);
                        fileException.Data[VcardFileException.DataItemName.BytePosition] = _bufferPosition + reader.TokenStartIndex;
                        throw fileException;
                    }
                case JsonTokenType.Number:
                    // A number is never escaped, so its bytes are its text, which is kept so that its digits are not changed.
                    return Encoding.UTF8.GetString(_buffer, _start + (int)reader.TokenStartIndex, reader.ValueSpan.Length);
                default:
                    return null;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? TakeCard(ReadResult result)
#else
        private VcardCard TakeCard(ReadResult result)
#endif
        {
            var card = result == ReadResult.Card ? _card : null;
            _card = null;

            // The reader may have been disposed while the card was being read or checked, and then a read in progress completes with ObjectDisposedException.
            ThrowIfDisposed();
            return card;
        }

        /// <summary>
        /// Builds the exception for data that is not valid JSON.
        /// </summary>
        private static VcardFileException BuildJsonException(JsonException exception, long bytePosition)
        {
            var fileException = new VcardFileException("The data is not valid JSON: " + exception.Message, exception);
            fileException.Data[VcardFileException.DataItemName.BytePosition] = bytePosition;
            if (exception.LineNumber.HasValue)
            {
                fileException.Data[VcardFileException.DataItemName.LineNumber] = exception.LineNumber.Value + 1;
            }

            if (exception.BytePositionInLine.HasValue)
            {
                fileException.Data[VcardFileException.DataItemName.LinePosition] = exception.BytePositionInLine.Value + 1;
            }

            return fileException;
        }

        /// <summary>
        /// Marks a read as in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another read is in progress, or an earlier read failed.</exception>
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

            if (_isFaulted)
            {
                EndRead();
                throw new InvalidOperationException("The reader cannot continue after a read failed or was canceled.");
            }
        }

        /// <summary>
        /// Marks the read as finished, and releases the reader if it was disposed while the read was in progress.
        /// </summary>
        private void EndRead()
        {
            Interlocked.Exchange(ref _isReadInProgress, 0);
            if (Volatile.Read(ref _isDisposed) != 0 && MarkReleased())
            {
                Release();
            }
        }

        /// <summary>
        /// Marks the reader as released.
        /// </summary>
        /// <returns><c>true</c> the first time it is called, when the caller must release the reader.</returns>
        private bool MarkReleased()
        {
            return Interlocked.Exchange(ref _isReleased, 1) == 0;
        }

        private void Release()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = new byte[0];
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

        private static void ValidateOptions(VcardOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null && options.Encoding.CodePage != Encoding.UTF8.CodePage)
                throw new ArgumentException("jCard is always UTF-8, so the encoding must be null or UTF-8.", nameof(options));
        }

        private static Stream ValidateStream(Stream stream, VcardOptions options)
        {
            if (stream is null)
                throw new ArgumentNullException("jcardStream");

            ValidateOptions(options);
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "jcardStream");

            return stream;
        }

        private static Stream OpenFile(string path, VcardOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("jcardFilePath");

            ValidateOptions(options);
            return VcardFileStreams.OpenRead(path, 4096);
        }

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER)
        /// <summary>
        /// The cards of <see cref="ReadCardsAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncCardEnumerable : IAsyncEnumerable<VcardCard>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly JcardReader _reader;

            internal AsyncCardEnumerable(JcardReader reader, CancellationToken cancellationToken)
            {
                _reader = reader;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<VcardCard> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
            {
                return new Enumerator(_reader, _cancellationToken, cancellationToken);
            }

            private sealed class Enumerator : IAsyncEnumerator<VcardCard>
            {
                private readonly CancellationToken _cancellationToken;
                private bool _isFinished;
                private readonly CancellationTokenSource _linkedTokenSource;
                private readonly JcardReader _reader;

                internal Enumerator(JcardReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
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

                public VcardCard Current { get; private set; }

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (_isFinished) return false;

                    // As an async iterator's does, the task is canceled for any OperationCanceledException, whether or not the token is
                    // canceled, so that MoveNextAsync behaves the same on every target.
                    var card = await _reader.ReadCardAsync(_cancellationToken).ConfigureAwait(false);
                    Current = card;
                    _isFinished = card is null;
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
#endif
