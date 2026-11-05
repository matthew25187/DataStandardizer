using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a writer of the card object model as vCard data (RFC 6350).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each card is written as content lines by a <see cref="VcardContentLineWriter"/>, so the same options and rules apply to its lines: folding, line breaks, and the encoding of
    /// parameter values.  The card is written as <c>BEGIN:VCARD</c>, then <c>VERSION:4.0</c>, then its properties in order, then <c>END:VCARD</c>.  A property read by
    /// <see cref="VcardReader"/> keeps its group and parameters exactly as they were read.
    /// </para>
    /// <para>
    /// Before a card is written, it is checked as <see cref="VcardCard.Validate"/> does.  If it breaks a rule of RFC 6350 that the options do not accept,
    /// <see cref="VcardFileException"/> is thrown, carrying the property in its data, and nothing of that card is written.  The accommodations are
    /// <see cref="VcardOptions.AllowMissingRequiredProperties"/>, <see cref="VcardOptions.AllowCardinalityViolations"/> and <see cref="VcardOptions.AllowInvalidValues"/>, all off by
    /// default.  The forms of vCard 2.1 are never written in a vCard 4.0 card, whatever <see cref="VcardOptions.AcceptLegacySyntax"/> says.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another is in progress throws
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class VcardWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private long _cardsWritten;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif
        private readonly VcardOptions _options;
        private readonly VcardContentLineWriter _writer;

        // These are changed with Interlocked, so that an operation started on another thread while one is in progress is refused.
        private int _isDisposed;
        private int _isOperationInProgress;

        /// <summary>
        /// Creates a writer of iCard data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="vcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="vcardStream"/> does not support writing.</exception>
        public VcardWriter(Stream vcardStream) : this(vcardStream, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of iCard data to a stream.
        /// </summary>
        /// <param name="vcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="vcardStream"/> does not support writing.
        /// -or-
        /// <see cref="VcardOptions.FoldLength"/>, <see cref="VcardOptions.FoldWhitespace"/> or <see cref="VcardOptions.LineBreak"/> is not
        /// valid.</exception>
        public VcardWriter(Stream vcardStream, VcardOptions options) : this(new VcardContentLineWriter(vcardStream, options), options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of an iCard file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> is <c>null</c>.</exception>
        public VcardWriter(string vcardFilePath) : this(vcardFilePath, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of an iCard file.  An existing file is overwritten.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.FoldLength"/>, <see cref="VcardOptions.FoldWhitespace"/> or
        /// <see cref="VcardOptions.LineBreak"/> is not valid.</exception>
        public VcardWriter(string vcardFilePath, VcardOptions options) : this(new VcardContentLineWriter(vcardFilePath, options), options)
        {
        }
#endif

        /// <summary>
        /// Creates a writer of iCard data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public VcardWriter(TextWriter writer) : this(writer, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of iCard data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c>, since the text writer does its own
        /// encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="VcardOptions.FoldLength"/>, <see cref="VcardOptions.FoldWhitespace"/> or <see cref="VcardOptions.LineBreak"/> is not
        /// valid.</exception>
        public VcardWriter(TextWriter writer, VcardOptions options) : this(new VcardContentLineWriter(writer, options), options)
        {
        }

        private VcardWriter(VcardContentLineWriter writer, VcardOptions options)
        {
            _writer = writer;
            _options = options;
        }

        /// <summary>
        /// Gets the number of cards written so far.
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
        /// Gets the number of logical (unfolded) content lines written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long LinesWritten
        {
            get
            {
                ThrowIfDisposed();
                return _writer.LinesWritten;
            }
        }

        /// <summary>
        /// Flushes and closes the stream writer or file the writer created itself, and releases the writer's buffer.  A caller-supplied text
        /// writer is neither flushed nor closed, and a caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the writer
        /// is flushed and released once that operation has finished.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            _writer.Dispose();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Flushes and closes, asynchronously, the stream writer or file the writer created itself, and releases the writer's buffer.  A
        /// caller-supplied text writer is neither flushed nor closed, and a caller-supplied stream is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the writer is flushed and released once that operation has finished.  The writer is
        /// disposed even if the flush throws.
        /// </remarks>
        /// <returns>A task that completes once the writer has been flushed and released.</returns>
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return default(ValueTask);

            return _writer.DisposeAsync();
        }
#endif

        /// <summary>
        /// Flushes the underlying text writer, whether the writer created it or it was supplied by the caller.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        public void Flush()
        {
            ThrowIfDisposed();
            BeginOperation();
            try
            {
                _writer.Flush();
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes a card.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is <c>null</c>.</exception>
        /// <exception cref="VcardFileException">The card breaks a rule of RFC 6350 that the options do not accept, and nothing has been
        /// written.
        /// -or-
        /// A content line cannot be written: a property name is not valid, or a value holds a character that is not allowed.  The lines before
        /// it have been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
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
                EndOperation();
            }
        }

        /// <summary>
        /// Writes cards.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="cards"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="cards">The cards.</param>
        /// <param name="progress">Receives the progress after each card has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the card it was reporting has been written.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="cards"/> contains <c>null</c>.  The cards before it have been
        /// written.</exception>
        /// <exception cref="VcardFileException">A card breaks a rule of RFC 6350 that the options do not accept, or one of its content lines
        /// cannot be written.  The cards before it have been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
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
                EndOperation();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Flushes the underlying text writer asynchronously, whether the writer created it or it was supplied by the caller.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the flush is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the text writer has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the flush is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            BeginOperation();
            return FlushCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Writes a card asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the underlying text writer takes every line without waiting, as a <see cref="StreamWriter"/>
        /// does while the lines fit its buffer.
        /// </remarks>
        /// <param name="card">The card.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the card has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="VcardFileException">By the task: the card breaks a rule of RFC 6350 that the options do not accept, and nothing
        /// has been written; or a content line cannot be written, and the lines before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteCardAsync(VcardCard card, CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the card or the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            if (card is null)
                throw new ArgumentNullException(nameof(card));

            BeginOperation();
            return WriteCardOperationAsync(card, cancellationToken);
        }

        /// <summary>
        /// Writes cards asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="cards"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="cards">The cards.</param>
        /// <param name="progress">Receives the progress after each card has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the card it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every card has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="cards"/> contains <c>null</c>.  The cards before it have
        /// been written.</exception>
        /// <exception cref="VcardFileException">By the task: a card breaks a rule of RFC 6350 that the options do not accept, or one of its
        /// content lines cannot be written.  The cards before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
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
            return WriteCardsCoreAsync(cards, progress, cancellationToken);
        }

        /// <summary>
        /// Writes cards asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="cards"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="cards">The cards.  <paramref name="cancellationToken"/> is passed to their enumerator.</param>
        /// <param name="progress">Receives the progress after each card has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the card it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every card has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="cards"/> contains <c>null</c>.  The cards before it have
        /// been written.</exception>
        /// <exception cref="VcardFileException">By the task: a card breaks a rule of RFC 6350 that the options do not accept, or one of its
        /// content lines cannot be written.  The cards before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
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
            return WriteAsyncCardsCoreAsync(cards, progress, cancellationToken);
        }

        private async Task FlushCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                EndOperation();
            }
        }

        private async ValueTask WriteCardOperationAsync(VcardCard card, CancellationToken cancellationToken)
        {
            try
            {
                await WriteCardCoreAsync(card, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                EndOperation();
            }
        }

        private async Task WriteCardsCoreAsync(IEnumerable<VcardCard> cards,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<VcardWriteProgress>? progress,
#else
            IProgress<VcardWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var card in cards)
                {
                    ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await WriteCardCoreAsync(CheckCard(card), cancellationToken).ConfigureAwait(false);
                    progress?.Report(GetProgress());
                }

                ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                EndOperation();
            }
        }

        private async Task WriteAsyncCardsCoreAsync(IAsyncEnumerable<VcardCard> cards,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<VcardWriteProgress>? progress,
#else
            IProgress<VcardWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
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
                        cancellationToken.ThrowIfCancellationRequested();
                        await WriteCardCoreAsync(CheckCard(enumerator.Current), cancellationToken).ConfigureAwait(false);
                        progress?.Report(GetProgress());
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }

                ThrowIfDisposed();
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Checks a card and hands its lines to the content-line writer, asynchronously.
        /// </summary>
        private async Task WriteCardCoreAsync(VcardCard card, CancellationToken cancellationToken)
        {
            var lines = PrepareCard(card);
            await _writer.WriteLinesAsync(lines, null, cancellationToken).ConfigureAwait(false);
            _cardsWritten++;
        }
#endif

        /// <summary>
        /// Checks a card and hands its lines to the content-line writer.
        /// </summary>
        private void WriteCardCore(VcardCard card)
        {
            var lines = PrepareCard(card);
            _writer.WriteLines(lines);
            _cardsWritten++;
        }

        /// <summary>
        /// Checks a card, and gets the lines to write for it.
        /// </summary>
        /// <exception cref="VcardFileException">The card breaks a rule of RFC 6350 that the options do not accept.</exception>
        private List<VcardContentLine> PrepareCard(VcardCard card)
        {
            var diagnostics = new List<VcardDiagnostic>();
            VcardConformance.Validate(card, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!VcardConformance.IsAccepted(diagnostic.Kind, _options, true))
                    throw VcardConformance.BuildException(diagnostic);
            }

            // Gather the lines first, so that a card changed by another thread cannot fail part way through for that reason.
            var mapping = VcardVersionMapping.Canonical;
            var lines = new List<VcardContentLine>(card.Properties.Count + 3);
            lines.Add(new VcardContentLine(VcardPropertyNames.Begin, VcardCardBuilder.CardValue));
            lines.Add(new VcardContentLine(VcardPropertyNames.Version, mapping.VersionText));
            foreach (var property in card.Properties)
            {
                lines.Add(mapping.FromCanonical(property));
            }

            lines.Add(new VcardContentLine(VcardPropertyNames.End, VcardCardBuilder.CardValue));
            return lines;
        }

        private VcardWriteProgress GetProgress()
        {
            return new VcardWriteProgress(_writer.LinesWritten, _cardsWritten);
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
                EndOperation();
                throw new ObjectDisposedException(GetType().Name);
            }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
            {
                EndOperation();
                throw new InvalidOperationException("The writer cannot continue after an operation was canceled.");
            }
#endif
        }

        private void EndOperation()
        {
            Interlocked.Exchange(ref _isOperationInProgress, 0);
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
    }
}
