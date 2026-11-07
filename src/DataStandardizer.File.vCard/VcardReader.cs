using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a reader of vCard data (RFC 6350 and RFC 2426) into the card object model: a sequence of <see cref="VcardCard"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is read with a <see cref="VcardContentLineReader"/>, so the same options and rules apply to its content lines.  A stream can hold many cards, of different versions.  Each
    /// <c>BEGIN:VCARD</c> line must be paired with an <c>END:VCARD</c> line, and a card can't hold another.  Unknown and <c>X-</c> properties are kept, in order, with their
    /// parameters.
    /// </para>
    /// <para>
    /// vCard 4.0, vCard 3.0 and vCard 2.1 are read.  A vCard 3.0 card is checked by the rules of its version (RFC 2426, RFC 2739 and RFC 4770), and a vCard 2.1 card by the specification of
    /// 1996, and each property is then converted to the canonical form of the model; see <see cref="VcardCard.SourceVersion"/>.  The card an <c>AGENT</c> of a vCard 2.1 card holds, on the
    /// lines after it (§2.5.4), is read with it, under the same options.  <see cref="VcardOptions.AcceptLegacySyntax"/> accepts the quoted-printable that some software writes in a vCard 3.0
    /// card, which RFC 2426 §5 eliminated.  Any
    /// other <c>VERSION</c> throws <see cref="VcardFileException"/> as soon as it is read, and the reader can't continue.  To read the content lines of such data, use
    /// <see cref="VcardContentLineReader"/>, which does not check <c>VERSION</c>.
    /// </para>
    /// <para>
    /// Each card is checked once its <c>END:VCARD</c> has been read, as <see cref="VcardCard.Validate"/> does, and for the structure of its <c>BEGIN</c>, <c>VERSION</c> and
    /// <c>END</c> lines.  The first problem that the options do not accept throws <see cref="VcardFileException"/>, carrying the card index and the property in its data; reading can
    /// then continue with the next card.  The accommodations are <see cref="VcardOptions.AllowVersionNotFirst"/>, <see cref="VcardOptions.AllowMissingRequiredProperties"/>,
    /// <see cref="VcardOptions.AllowCardinalityViolations"/>, <see cref="VcardOptions.AllowInvalidValues"/> and <see cref="VcardOptions.AcceptLegacySyntax"/>, all off by default.
    /// </para>
    /// <para>
    /// Any other exception from a read, including a cancellation, leaves the reader unable to continue, and later reads throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while another is in progress throws
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class VcardReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private readonly VcardCardBuilder _builder;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? _card;
#else
        private VcardCard _card;
#endif
        private bool _isFaulted;
        private readonly VcardOptions _options;
        private readonly VcardContentLineReader _reader;

        // These are changed with Interlocked, so that a read started on another thread while one is in progress is refused.
        private int _isDisposed;
        private int _isReadInProgress;

        /// <summary>
        /// Creates a reader for vCard data in a stream, read as UTF-8.
        /// </summary>
        /// <param name="vcardStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="vcardStream"/> does not support reading.</exception>
        public VcardReader(Stream vcardStream) : this(vcardStream, new VcardOptions())
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
        public VcardReader(Stream vcardStream, VcardOptions options) : this(new VcardContentLineReader(vcardStream, options), options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for a vCard file, read as UTF-8.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> is <c>null</c>.</exception>
        public VcardReader(string vcardFilePath) : this(vcardFilePath, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a reader for a vCard file.
        /// </summary>
        /// <param name="vcardFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="vcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.MaxLineLength"/> is less than 1.</exception>
        public VcardReader(string vcardFilePath, VcardOptions options) : this(new VcardContentLineReader(vcardFilePath, options), options)
        {
        }
#endif

        /// <summary>
        /// Creates a reader for vCard data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public VcardReader(TextReader reader) : this(reader, new VcardOptions())
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
        public VcardReader(TextReader reader, VcardOptions options) : this(new VcardContentLineReader(reader, options), options)
        {
        }

        /// <summary>
        /// Creates a reader with a specific size for the internal character block of its content-line reader.  Allows tests to force refills at
        /// chosen points.
        /// </summary>
        internal VcardReader(TextReader reader, VcardOptions options, int bufferSize) : this(new VcardContentLineReader(reader, options, bufferSize), options)
        {
        }

        /// <summary>
        /// Creates a reader of a nested card, which has a version it is taken to be of, and a depth.
        /// </summary>
        internal VcardReader(TextReader reader, VcardOptions options, VcardVersionMapping defaultMapping, int depth) : this(new VcardContentLineReader(reader, options), options, defaultMapping, depth)
        {
        }

        private VcardReader(VcardContentLineReader reader, VcardOptions options) : this(reader, options, VcardVersionMapping.Canonical, 0)
        {
        }

        private VcardReader(VcardContentLineReader reader, VcardOptions options, VcardVersionMapping defaultMapping, int depth)
        {
            _reader = reader;
            _options = options;
            _builder = new VcardCardBuilder(options, defaultMapping, depth);
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
        /// Gets the number of logical (unfolded) lines read so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long LinesRead
        {
            get
            {
                ThrowIfDisposed();
                return _reader.LinesRead;
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
                return _reader.PhysicalLinesRead;
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

            _reader.Dispose();
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
        /// Reads the next card.
        /// </summary>
        /// <returns>The card; or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="VcardFileException">The data is not valid vCard: a content line is not valid, a <c>BEGIN</c> or <c>END</c> is not
        /// paired, the <c>VERSION</c> is not 4.0, 3.0 or 2.1, or the data holds no card.
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    VcardContentLine? line;
#else
                    VcardContentLine line;
#endif
                    try
                    {
                        line = _reader.ReadLine();
                    }
                    catch
                    {
                        _isFaulted = true;
                        throw;
                    }

                    if (AddLine(line)) return TakeCard();
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
        /// <exception cref="VcardFileException">During enumeration: the data is not valid vCard, or a card breaks a rule of RFC 6350 that
        /// the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was
        /// canceled.</exception>
        public IEnumerable<VcardCard> ReadCards()
        {
            // Check now, so the exception comes from this call and not from the first MoveNext.
            ThrowIfDisposed();

            return ReadCardsIterator();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the next card asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the buffers of the content-line reader already hold the whole card.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  The read is canceled only if this token is
        /// canceled, and the reader then cannot continue.</param>
        /// <returns>A task whose result is the card, or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="VcardFileException">By the task: the data is not valid vCard, or the card breaks a rule of RFC 6350 that the
        /// options do not accept.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled before the call, or the
        /// read failed with an <see cref="OperationCanceledException"/>, whatever token it carries, while <paramref name="cancellationToken"/>
        /// was canceled.  The task is canceled.  An <see cref="OperationCanceledException"/> from the source while the token isn't canceled
        /// faults the task, as any other failure of the source does.  Either way, the reader then cannot continue.</exception>
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
        /// <exception cref="VcardFileException">During enumeration: the data is not valid vCard, or a card breaks a rule of RFC 6350 that
        /// the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was
        /// canceled.</exception>
        /// <exception cref="OperationCanceledException">During enumeration: a read was canceled, which it is only if the token is canceled; or
        /// the source failed with one while the token isn't canceled.  Either one leaves the reader unable to continue.  On every target, the
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
#endif

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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Continues <see cref="ReadCardAsync"/>, and ends the read.  Completes synchronously, without allocating a task, while the
        /// content-line reader completes synchronously.
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    VcardContentLine? line;
#else
                    VcardContentLine line;
#endif
                    try
                    {
                        line = await _reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        _isFaulted = true;
                        throw;
                    }

                    if (AddLine(line))
                    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                        return new ValueTask<VcardCard?>(TakeCard());
#else
                        return new ValueTask<VcardCard>(TakeCard());
#endif
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
#endif

        /// <summary>
        /// Adds a line, or the end of the data, to the card being built.
        /// </summary>
        /// <param name="line">The line; or <c>null</c> at the end of the data.</param>
        /// <returns><c>true</c> if the read has its result: a card, ended by this line and checked, or <c>null</c> at the end of the data;
        /// see <see cref="TakeCard"/>.</returns>
        /// <exception cref="VcardFileException">The line is not valid where it is, or the card it ends breaks a rule of RFC 6350 that the
        /// options do not accept.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private bool AddLine(VcardContentLine? line)
#else
        private bool AddLine(VcardContentLine line)
#endif
        {
            try
            {
                if (line is null)
                {
                    _builder.Complete();
                    _card = null;
                    return true;
                }

                _card = _builder.Add(line);
            }
            catch
            {
                // Where the card being built ends is now unknown.
                _isFaulted = true;
                throw;
            }

            if (_card is null) return false;

            // The card has been read in full, so a problem with it does not stop the reader.
            var structure = _builder.TakeStructureDiagnostics();
            var conformance = new List<VcardDiagnostic>();
            try
            {
                // A card of another version was checked by its own rules before it was converted, and the rules of vCard 4.0 aren't its rules.
                if (_card.SourceVersion == VcardVersion.V4_0)
                {
                    VcardConformance.Validate(_card, conformance);
                }
            }
            catch
            {
                // A problem in a card is a diagnostic, so this came from something else, such as a CharsetResolver of the caller's.
                _isFaulted = true;
                _card = null;
                throw;
            }

            // A stable sort, so that the first problem on the earliest line is the one reported.
            var diagnostics = structure.Concat(conformance).OrderBy(diagnostic => diagnostic.LineNumber);
            foreach (var diagnostic in diagnostics)
            {
                if (!VcardConformance.IsAccepted(diagnostic.Kind, _options, false))
                {
                    _card = null;
                    throw VcardConformance.BuildException(diagnostic);
                }
            }

            return true;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? TakeCard()
#else
        private VcardCard TakeCard()
#endif
        {
            var card = _card;
            _card = null;

            // The reader may have been disposed while the card was being checked.
            ThrowIfDisposed();
            return card;
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

        private void EndRead()
        {
            Interlocked.Exchange(ref _isReadInProgress, 0);
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER) && NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// The cards of <see cref="ReadCardsAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncCardEnumerable : IAsyncEnumerable<VcardCard>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly VcardReader _reader;

            internal AsyncCardEnumerable(VcardReader reader, CancellationToken cancellationToken)
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
                private readonly VcardReader _reader;

                internal Enumerator(VcardReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
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
