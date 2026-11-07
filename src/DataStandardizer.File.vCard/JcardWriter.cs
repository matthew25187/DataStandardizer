#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a writer of the card object model as jCard data, the JSON form of vCard (RFC 7095).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is written as UTF-8 with no byte order mark, as compact JSON with no indentation.  A single card is written as its jCard object, <c>["vcard", [...]]</c>, and two or
    /// more as an array of jCard objects (RFC 7095 §3.2).  Since which of the two the document is depends on whether a second card follows, the first card is held in memory until a
    /// second is written or the writer is disposed; <see cref="Flush"/> does not write it.  If no card is written, nothing is.
    /// </para>
    /// <para>
    /// jCard represents vCard 4.0 alone, so each card is written in the form of vCard 4.0, as <see cref="VcardWriter"/> writes it with <see cref="VcardOptions.WriteVersion"/> set to
    /// vCard 4.0, whatever its <see cref="VcardCard.SourceVersion"/>.  The <c>version</c> property comes first, with the value 4.0 (RFC 7095 §3.3.1.1).  Property and parameter names
    /// are written in lower case (§3.3 and §3.4), the group of a property as its group parameter, in lower case (§3.3.1.2.1), and each value as the JSON value of its type, converted
    /// from the text form of RFC 6350 (§3.5).  In particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <c>VALUE</c> parameter is not written, since the type identifier gives the type (RFC 7095 §3.4.1).</description></item>
    /// <item><description>The value of a property whose type is not known, such as an <c>X-</c> property with no <c>VALUE</c> parameter, is written as it is with the <c>unknown</c>
    /// type (RFC 7095 §5.1).  A <c>VALUE</c> parameter that names a value type RFC 6350 does not define, such as <c>x-mytype</c>, is written as the type identifier, and the value as
    /// it is.</description></item>
    /// <item><description>Each value of a multi-valued property, such as <c>CATEGORIES</c>, is an element of the property array (RFC 7095 §3.3).  The value of <c>N</c>, <c>ADR</c>,
    /// <c>ORG</c>, <c>GENDER</c> and <c>CLIENTPIDMAP</c> is an array with an element for each of its components, and an array for a component with more than one value; a value of one
    /// component is written as a string (§3.3.1.3).  The number of components is the number the value has, so an <c>N</c> without the components of RFC 9554 has five.</description></item>
    /// <item><description>The values of the <c>TYPE</c>, <c>PID</c> and <c>SORT-AS</c> parameters are split on their commas, and those of a repeated <c>TYPE</c> are joined, into
    /// an array of strings when there is more than one (RFC 7095 §3.4.2).</description></item>
    /// <item><description>INTEGER and FLOAT values are JSON numbers, BOOLEAN values JSON literals, and the dates and times in the extended format of ISO 8601 where it applies.  A URI
    /// has no escaping of its commas.</description></item>
    /// <item><description>A value that is not valid for its type, which <see cref="VcardOptions.AllowInvalidValues"/> lets a card hold, is written as a string with its type
    /// identifier, which reads back the same, as a value that <see cref="VcardOptions.AllowInvalidValues"/> accepts.  A TEXT, URI or structured value that isn't valid is written as it is with the <c>unknown</c> type instead, so that its escaping isn't
    /// changed, and loses any <c>VALUE</c> parameter.</description></item>
    /// </list>
    /// <para>
    /// So a card written by this writer and read back by <see cref="JcardReader"/> has the same properties, with the same values; only the spelling may change, such as the case of a
    /// name, a group or a BOOLEAN, the escaping of a TEXT value, the digits of an INTEGER, the commas of a list parameter, or the place of a <c>VALUE</c> parameter, which is read back
    /// after the other parameters.  A <c>VALUE</c> parameter that names the property's default type is not read back, since jCard cannot tell it apart from none.
    /// </para>
    /// <para>
    /// Before a card is written, it is checked as <see cref="VcardCard.Validate(VcardVersion)"/> does for vCard 4.0, against the same rules and options as <see cref="VcardWriter"/>.
    /// If it breaks a rule that the options do not accept, <see cref="VcardFileException"/> is thrown, carrying the property in its data, and nothing of that card is written.  The card
    /// is also refused if a name is not a valid vCard name, it has a <c>BEGIN</c> or <c>END</c> property (RFC 7095 §3.2), a property has two parameters of the same name other than a
    /// list parameter, which a JSON object cannot hold, or a <c>GROUP</c> parameter, which is reserved for jCard (§7.1), or a value holds a lone surrogate.
    /// </para>
    /// <para>
    /// Each card is encoded with <see cref="Utf8JsonWriter"/> into pooled blocks of bytes before any of it is written to the stream; the asynchronous members then write it
    /// asynchronously, and do no synchronous I/O.  Of the options, <see cref="VcardOptions.Encoding"/> must be UTF-8 if it is set, and those that concern <c>.vcf</c> content lines,
    /// such as <see cref="VcardOptions.LineBreak"/> and <see cref="VcardOptions.WriteVersion"/>, do not apply.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another is in progress throws
    /// <see cref="InvalidOperationException"/>.  A write to the stream that fails leaves what reached it unknown, so later calls throw <see cref="InvalidOperationException"/>, and
    /// disposal writes nothing more to the stream.
    /// </para>
    /// <para>
    /// jCard needs System.Text.Json, so this type is not available on netstandard1.1.
    /// </para>
    /// </remarks>
    public sealed class JcardWriter : IDisposable, IAsyncDisposable
    {
        private static readonly byte[] ArrayStart = { (byte)'[' };
        private static readonly byte[] ArrayEnd = { (byte)']' };

        // ref. RFC 7095 §3.3.1.1: the version property, "the first element of the array containing the properties of a jCard".
        private static readonly VcardContentLine VersionLine = new VcardContentLine(VcardPropertyNames.Version, VcardVersionMapping.Canonical.VersionText);

        private long _cardsWritten;

        // The card being encoded; and the first card, held until it is known whether the document is one card or an array of them.
        private readonly JcardBufferWriter _encoded = new JcardBufferWriter();
        private readonly JcardBufferWriter _held = new JcardBufferWriter();
        private bool _isArrayStarted;
        private bool _isCanceled;
        private bool _isFaulted;
        private bool _isHeld;
        private readonly Utf8JsonWriter _jsonWriter;
        private long _linesWritten;
        private readonly VcardOptions _options;
        private readonly bool _ownsStream;
        private long _propertiesWritten;
        private readonly Stream _stream;

        // These are changed with Interlocked, so that an operation started on another thread while one is in progress is refused, and a Dispose from another thread releases the
        // writer once that operation has finished.
        private int _isDisposed;
        private int _isOperationInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a writer of jCard data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="jcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcardStream"/> does not support writing.</exception>
        public JcardWriter(Stream jcardStream) : this(jcardStream, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of jCard data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="jcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCard is always UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcardStream"/> does not support writing.
        /// -or-
        /// <see cref="VcardOptions.Encoding"/> is not UTF-8.</exception>
        public JcardWriter(Stream jcardStream, VcardOptions options) : this(ValidateStream(jcardStream, options), false, options)
        {
        }

        /// <summary>
        /// Creates a writer of a jCard file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="jcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardFilePath"/> is <c>null</c>.</exception>
        public JcardWriter(string jcardFilePath) : this(jcardFilePath, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of a jCard file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="jcardFilePath">The path of the file.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCard is always UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is not UTF-8.</exception>
        public JcardWriter(string jcardFilePath, VcardOptions options) : this(OpenFile(jcardFilePath, options), true, options)
        {
        }

        private JcardWriter(Stream stream, bool ownsStream, VcardOptions options)
        {
            _stream = stream;
            _ownsStream = ownsStream;
            _options = options;

            // Characters outside ASCII are written as they are, not as \u escapes, since the data is not embedded in HTML.
            _jsonWriter = new Utf8JsonWriter(_encoded, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        /// <summary>
        /// Gets the number of cards written so far, including a first card that is held until the next is written.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long CardsWritten
        {
            get
            {
                ThrowIfDisposed();
                return _cardsWritten;
            }
        }

        /// <summary>
        /// Gets the number of properties written so far, in all cards, not counting <c>version</c>.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long PropertiesWritten
        {
            get
            {
                ThrowIfDisposed();
                return _propertiesWritten;
            }
        }

        /// <summary>
        /// Ends the document, writing the first card if it is the only one, then flushes the stream, closes the file the writer opened and returns the pooled buffers.  A
        /// caller-supplied stream is flushed but left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the document is ended once that operation has
        /// finished.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            if (Volatile.Read(ref _isOperationInProgress) == 0 && MarkReleased())
            {
                Release();
            }
        }

        /// <summary>
        /// Ends the document asynchronously, writing the first card if it is the only one, then flushes the stream, closes the file the writer opened and returns the pooled buffers.
        /// A caller-supplied stream is flushed but left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>,
        /// and the document is ended once that operation has finished, so the task this returns has completed before then; await the operation as well before using the stream.  The
        /// file is closed even if ending the document throws, and the task then faults with that exception, even an <see cref="OperationCanceledException"/>.
        /// </remarks>
        /// <returns>A task that completes once the document has been ended and the writer released; or a task that has already completed, if an operation is in progress, which then
        /// ends the document.</returns>
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return default(ValueTask);

            if (Volatile.Read(ref _isOperationInProgress) == 0 && MarkReleased())
            {
                return new ValueTask(AsyncOutcome.Unwrap(AsyncOutcome.ReleaseThenReportAsync(ReleaseAsync(), Task.CompletedTask)));
            }

            return default(ValueTask);
        }

        /// <summary>
        /// Flushes the stream.  A first card that is held until the next is written is not written.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write to the stream failed, so the writer cannot continue.</exception>
        public void Flush()
        {
            ThrowIfDisposed();
            BeginOperation();
            try
            {
                _stream.Flush();
            }
            finally
            {
                if (EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Writes a card.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is <c>null</c>.</exception>
        /// <exception cref="VcardFileException">The card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in jCard, and nothing has been
        /// written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write to the stream failed, so the writer cannot continue.</exception>
        public void WriteCard(VcardCard card)
        {
            ThrowIfDisposed();
            if (card is null)
                throw new ArgumentNullException(nameof(card));

            BeginOperation();
            try
            {
                WriteCardCore(card);
            }
            finally
            {
                if (EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Writes cards.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="cards"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="cards">The cards.</param>
        /// <param name="progress">Receives the progress after each card has been written, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="VcardWriteProgress.LinesWritten"/> is the number of content lines the cards written so far would have in <c>.vcf</c> form, as <see cref="VcardWriter"/> would
        /// write them in vCard 4.0.  An exception it throws ends the call; the card it was reporting has been written.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="cards"/> contains <c>null</c>.  The cards before it have been written.</exception>
        /// <exception cref="VcardFileException">A card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in jCard.  The cards before it have been
        /// written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write to the stream failed, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteCards(IEnumerable<VcardCard> cards, IProgress<VcardWriteProgress>? progress = null)
#else
        public void WriteCards(IEnumerable<VcardCard> cards, IProgress<VcardWriteProgress> progress = null)
#endif
        {
            ThrowIfDisposed();
            if (cards is null)
                throw new ArgumentNullException(nameof(cards));

            BeginOperation();
            try
            {
                foreach (var card in cards)
                {
                    ThrowIfDisposed();
                    WriteCardCore(CheckCard(card));
                    progress?.Report(GetProgress());
                }

                ThrowIfDisposed();
            }
            finally
            {
                if (EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Flushes the stream asynchronously.  A first card that is held until the next is written is not written.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The flush is canceled only if this token is canceled, and the writer then
        /// cannot continue.</param>
        /// <returns>A task that completes once the stream has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the flush is in
        /// progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write to the stream failed, so the writer cannot continue.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// flush failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  Any other exception faults the task even while <paramref name="cancellationToken"/> is canceled,
        /// and an <see cref="OperationCanceledException"/> while it isn't faults the task too, as an I/O error does.</exception>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            BeginOperation();
            return AsyncOutcome.Unwrap(FlushCoreAsync(cancellationToken));
        }

        /// <summary>
        /// Writes a card asynchronously.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked before the card is written, so that a canceled write never leaves part of a
        /// card.  The write is canceled only if this token is canceled, and the writer then
        /// cannot continue.</param>
        /// <returns>A task that completes once the card has been written to the stream, or held as the first card.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in
        /// progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write to the stream failed, so the writer cannot continue.</exception>
        /// <exception cref="VcardFileException">By the task: the card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in jCard, and nothing has been
        /// written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// write failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  Any other exception faults the task even while <paramref name="cancellationToken"/> is canceled,
        /// and an <see cref="OperationCanceledException"/> while it isn't faults the task too, as an I/O error does.</exception>
        public ValueTask WriteCardAsync(VcardCard card, CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the card or the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            if (card is null)
                throw new ArgumentNullException(nameof(card));

            BeginOperation();
            return new ValueTask(AsyncOutcome.Unwrap(WriteCardOperationAsync(card, cancellationToken)));
        }

        /// <summary>
        /// Writes cards asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="cards"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="cards">The cards.</param>
        /// <param name="progress">Receives the progress after each card has been written, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="VcardWriteProgress.LinesWritten"/> is the number of content lines the cards written so far would have in <c>.vcf</c> form.  An exception it throws ends the
        /// call; the card it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked between cards.  The write is canceled only if this token is
        /// canceled, and the writer then cannot continue.</param>
        /// <returns>A task that completes once every card has been written.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in
        /// progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write to the stream failed, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="cards"/> contains <c>null</c>.  The cards before it have been written.</exception>
        /// <exception cref="VcardFileException">By the task: a card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in jCard.  The cards before it have
        /// been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// write failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  Any other exception faults the task even while <paramref name="cancellationToken"/> is canceled,
        /// and an <see cref="OperationCanceledException"/> while it isn't faults the task too, whether from the underlying writer,
        /// <paramref name="cards"/> or <paramref name="progress"/>, as an I/O error does.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteCardsAsync(IEnumerable<VcardCard> cards, IProgress<VcardWriteProgress>? progress = null,
#else
        public Task WriteCardsAsync(IEnumerable<VcardCard> cards, IProgress<VcardWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (cards is null)
                throw new ArgumentNullException(nameof(cards));

            BeginOperation();
            return AsyncOutcome.Unwrap(WriteCardsCoreAsync(cards, progress, cancellationToken));
        }

        /// <summary>
        /// Writes cards asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="cards"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="cards">The cards.  <paramref name="cancellationToken"/> is passed to their enumerator.</param>
        /// <param name="progress">Receives the progress after each card has been written, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="VcardWriteProgress.LinesWritten"/> is the number of content lines the cards written so far would have in <c>.vcf</c> form.  An exception it throws ends the
        /// call; the card it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked between cards.  The write is canceled only if this token is
        /// canceled, and the writer then cannot continue.</param>
        /// <returns>A task that completes once every card has been written.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in
        /// progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write to the stream failed, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="cards"/> contains <c>null</c>.  The cards before it have been written.</exception>
        /// <exception cref="VcardFileException">By the task: a card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in jCard.  The cards before it have
        /// been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// write failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  Any other exception faults the task even while <paramref name="cancellationToken"/> is canceled,
        /// and an <see cref="OperationCanceledException"/> while it isn't faults the task too, whether from the underlying writer,
        /// <paramref name="cards"/> or <paramref name="progress"/>, as an I/O error does.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteCardsAsync(IAsyncEnumerable<VcardCard> cards, IProgress<VcardWriteProgress>? progress = null,
#else
        public Task WriteCardsAsync(IAsyncEnumerable<VcardCard> cards, IProgress<VcardWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (cards is null)
                throw new ArgumentNullException(nameof(cards));

            BeginOperation();
            return AsyncOutcome.Unwrap(WriteAsyncCardsCoreAsync(cards, progress, cancellationToken));
        }

        private async ValueTask<Task> FlushCoreAsync(CancellationToken cancellationToken)
        {
            Task outcome;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);

                // The writer may have been disposed while the flush was in progress.
                ThrowIfDisposed();
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = AsyncOutcome.Fail(exception, cancellationToken, ref _isCanceled);
            }

            return EndOperation() ? await AsyncOutcome.ReleaseThenReportAsync(ReleaseAsync(), outcome).ConfigureAwait(false) : outcome;
        }

        private async ValueTask<Task> WriteCardOperationAsync(VcardCard card, CancellationToken cancellationToken)
        {
            Task outcome;
            try
            {
                await WriteCardCoreAsync(card, cancellationToken).ConfigureAwait(false);

                // The writer may have been disposed while the card was being written.
                ThrowIfDisposed();
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = AsyncOutcome.Fail(exception, cancellationToken, ref _isCanceled);
            }

            return EndOperation() ? await AsyncOutcome.ReleaseThenReportAsync(ReleaseAsync(), outcome).ConfigureAwait(false) : outcome;
        }

        private async ValueTask<Task> WriteCardsCoreAsync(IEnumerable<VcardCard> cards,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<VcardWriteProgress>? progress,
#else
            IProgress<VcardWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            Task outcome;
            try
            {
                // Check here as well, so that a canceled token cancels the call when there are no cards, as it does for an asynchronous sequence.
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var card in cards)
                {
                    ThrowIfDisposed();
                    await WriteCardCoreAsync(CheckCard(card), cancellationToken).ConfigureAwait(false);
                    progress?.Report(GetProgress());
                }

                ThrowIfDisposed();
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = AsyncOutcome.Fail(exception, cancellationToken, ref _isCanceled);
            }

            return EndOperation() ? await AsyncOutcome.ReleaseThenReportAsync(ReleaseAsync(), outcome).ConfigureAwait(false) : outcome;
        }

        private async ValueTask<Task> WriteAsyncCardsCoreAsync(IAsyncEnumerable<VcardCard> cards,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<VcardWriteProgress>? progress,
#else
            IProgress<VcardWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            Task outcome;
            try
            {
                var enumerator = cards.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (true)
                    {
                        ThrowIfDisposed();
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) break;

                        ThrowIfDisposed();
                        await WriteCardCoreAsync(CheckCard(enumerator.Current), cancellationToken).ConfigureAwait(false);
                        progress?.Report(GetProgress());
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }

                ThrowIfDisposed();
                outcome = Task.CompletedTask;
            }
            catch (Exception exception)
            {
                outcome = AsyncOutcome.Fail(exception, cancellationToken, ref _isCanceled);
            }

            return EndOperation() ? await AsyncOutcome.ReleaseThenReportAsync(ReleaseAsync(), outcome).ConfigureAwait(false) : outcome;
        }

        /// <summary>
        /// Checks and encodes a card, then writes it to the stream asynchronously.
        /// </summary>
        private async Task WriteCardCoreAsync(VcardCard card, CancellationToken cancellationToken)
        {
            // Cancellation is checked only before the card is started, so that a canceled write never leaves part of a card that disposal would then close as though it were complete.
            cancellationToken.ThrowIfCancellationRequested();
            var propertyCount = Encode(card);
            if (_cardsWritten != 0)
            {
                try
                {
                    if (_isHeld)
                    {
                        await _stream.WriteAsync(ArrayStart, 0, ArrayStart.Length, CancellationToken.None).ConfigureAwait(false);
                        await _stream.WriteAsync(_held.Buffer, 0, _held.Count, CancellationToken.None).ConfigureAwait(false);
                        ReleaseHeld();
                    }

                    await _stream.WriteAsync(_encoded.Buffer, 0, _encoded.Count, CancellationToken.None).ConfigureAwait(false);
                }
                catch
                {
                    // What reached the stream is now unknown.
                    _isFaulted = true;
                    throw;
                }
            }

            CountCard(propertyCount);
        }

        /// <summary>
        /// Ends the document, flushes the stream and closes the file the writer opened, asynchronously.
        /// </summary>
        private async ValueTask ReleaseAsync()
        {
            try
            {
                if (!_isFaulted)
                {
                    if (_isHeld)
                    {
                        await _stream.WriteAsync(_held.Buffer, 0, _held.Count, CancellationToken.None).ConfigureAwait(false);
                    }
                    else if (_isArrayStarted)
                    {
                        await _stream.WriteAsync(ArrayEnd, 0, ArrayEnd.Length, CancellationToken.None).ConfigureAwait(false);
                    }

                    await _stream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
            finally
            {
                ReleaseBuffers();
                if (_ownsStream)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    await _stream.DisposeAsync().ConfigureAwait(false);
#else
                    _stream.Dispose();
#endif
                }
            }
        }

        /// <summary>
        /// Checks and encodes a card, then writes it to the stream.
        /// </summary>
        private void WriteCardCore(VcardCard card)
        {
            var propertyCount = Encode(card);
            if (_cardsWritten != 0)
            {
                try
                {
                    if (_isHeld)
                    {
                        _stream.Write(ArrayStart, 0, ArrayStart.Length);
                        _stream.Write(_held.Buffer, 0, _held.Count);
                        ReleaseHeld();
                    }

                    _stream.Write(_encoded.Buffer, 0, _encoded.Count);
                }
                catch
                {
                    // What reached the stream is now unknown.
                    _isFaulted = true;
                    throw;
                }
            }

            CountCard(propertyCount);
        }

        /// <summary>
        /// Checks a card, and encodes it as JSON into the buffer, after a comma if it follows another.  The first card is held.
        /// </summary>
        /// <returns>The number of properties written, not counting <c>version</c>.</returns>
        /// <exception cref="VcardFileException">The card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in jCard.</exception>
        private int Encode(VcardCard card)
        {
            // Gather the lines first, so that a card changed by another thread cannot fail part way through for that reason.  ref. RFC 7095 §3.3.1.1: the card is written in the form
            // of vCard 4.0, and checked by its rules.
            var mapping = VcardVersionMapping.Canonical;
            var diagnostics = new List<VcardDiagnostic>();
            var properties = mapping.FromCanonical(card, _options, diagnostics);
            mapping.Validate(properties, card, _options, 0, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!VcardConformance.IsAccepted(diagnostic.Kind, _options, true))
                    throw VcardConformance.BuildException(diagnostic);
            }

            // Encode the whole card first, so that a card that cannot be written in jCard fails before any of it is written.
            var buffer = _cardsWritten == 0 ? _held : _encoded;
            buffer.Clear();
            if (_cardsWritten != 0)
            {
                buffer.Write((byte)',');
            }

            _jsonWriter.Reset(buffer);
            try
            {
                // ref. RFC 7095 §3.2: an array of the string "vcard" and an array of the properties.
                _jsonWriter.WriteStartArray();
                _jsonWriter.WriteStringValue(JcardConverter.CardName);
                _jsonWriter.WriteStartArray();
                JcardConverter.WriteProperty(_jsonWriter, VersionLine);
                foreach (var property in properties)
                {
                    JcardConverter.WriteProperty(_jsonWriter, property);
                }

                _jsonWriter.WriteEndArray();
                _jsonWriter.WriteEndArray();
                _jsonWriter.Flush();
            }
            catch
            {
                buffer.Clear();
                throw;
            }

            if (_cardsWritten == 0)
            {
                _isHeld = true;
            }

            return properties.Count;
        }

        private void CountCard(int propertyCount)
        {
            if (_cardsWritten == 1)
            {
                _isArrayStarted = true;
            }

            _propertiesWritten += propertyCount;
            _cardsWritten++;

            // The lines of the card in .vcf form: BEGIN, VERSION and END, and one for each property.
            _linesWritten += 3 + propertyCount;
        }

        private VcardWriteProgress GetProgress()
        {
            return new VcardWriteProgress(_linesWritten, _cardsWritten);
        }

        /// <summary>
        /// Marks the first card as written, once it has been written at the start of the array of cards.
        /// </summary>
        private void ReleaseHeld()
        {
            _isHeld = false;
            _held.Clear();
        }

        /// <summary>
        /// Marks an operation as in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another operation is in progress, or an earlier one was canceled.</exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        private void BeginOperation()
        {
            if (Interlocked.CompareExchange(ref _isOperationInProgress, 1, 0) != 0)
                throw new InvalidOperationException("Another operation is in progress.  Only one operation may be in progress at a time.");

            // Dispose may have run between the caller's check and this one.
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                if (EndOperation())
                {
                    Release();
                }

                throw new ObjectDisposedException(GetType().Name);
            }

            if (_isCanceled)
            {
                if (EndOperation())
                {
                    Release();
                }

                throw new InvalidOperationException("The writer cannot continue after an operation was canceled.");
            }

            if (_isFaulted)
            {
                if (EndOperation())
                {
                    Release();
                }

                throw new InvalidOperationException("The writer cannot continue after a write to the stream failed.");
            }
        }

        /// <summary>
        /// Marks the operation as finished.
        /// </summary>
        /// <returns><c>true</c> if the writer was disposed while the operation was in progress, and the caller must now release it.</returns>
        private bool EndOperation()
        {
            Interlocked.Exchange(ref _isOperationInProgress, 0);
            return Volatile.Read(ref _isDisposed) != 0 && MarkReleased();
        }

        /// <summary>
        /// Marks the writer as released.
        /// </summary>
        /// <returns><c>true</c> the first time it is called, when the caller must release the writer.</returns>
        private bool MarkReleased()
        {
            return Interlocked.Exchange(ref _isReleased, 1) == 0;
        }

        /// <summary>
        /// Ends the document, flushes the stream and closes the file the writer opened.
        /// </summary>
        private void Release()
        {
            try
            {
                if (!_isFaulted)
                {
                    if (_isHeld)
                    {
                        _stream.Write(_held.Buffer, 0, _held.Count);
                    }
                    else if (_isArrayStarted)
                    {
                        _stream.Write(ArrayEnd, 0, ArrayEnd.Length);
                    }

                    _stream.Flush();
                }
            }
            finally
            {
                ReleaseBuffers();
                if (_ownsStream)
                {
                    _stream.Dispose();
                }
            }
        }

        private void ReleaseBuffers()
        {
            _isHeld = false;
            _isArrayStarted = false;
            _jsonWriter.Dispose();
            _encoded.Release();
            _held.Release();
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static VcardCard CheckCard(VcardCard? card)
#else
        private static VcardCard CheckCard(VcardCard card)
#endif
        {
            if (card is null)
                throw new ArgumentException("The cards cannot contain null.", "cards");

            return card;
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
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "jcardStream");

            return stream;
        }

        private static Stream OpenFile(string path, VcardOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("jcardFilePath");

            ValidateOptions(options);
            return VcardFileStreams.OpenWrite(path, 4096);
        }
    }
}
#endif
