using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a reader of iCalendar data (RFC 5545) into the component object model: a sequence of <see cref="IcalCalendar"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is read with an <see cref="IcalContentLineReader"/>, so the same options and rules apply to its content lines.  Each
    /// <c>BEGIN</c> line must be matched by an <c>END</c> line, and components must be properly nested.  Components RFC 5545 defines become
    /// the typed components, such as <see cref="IcalEvent"/>; any other becomes an <see cref="IcalComponent"/>.  Unknown and <c>X-</c>
    /// properties are kept, in order, with their parameters.
    /// </para>
    /// <para>
    /// Only iCalendar 2.0 is read.  A <c>VERSION</c> that does not include 2.0, such as the <c>VERSION:1.0</c> of vCalendar 1.0, throws
    /// <see cref="IcalFileException"/> as soon as it is read.  To read the content lines of such data, use <see cref="IcalContentLineReader"/>,
    /// which does not check <c>VERSION</c>.
    /// </para>
    /// <para>
    /// Each calendar is checked once its <c>END:VCALENDAR</c> has been read, as <see cref="IcalComponent.Validate"/> does.  The first problem
    /// that the options do not accept throws <see cref="IcalFileException"/>, carrying the component path in its data; reading can then continue
    /// with the next calendar.  The accommodations are <see cref="IcalOptions.AllowMissingRequiredProperties"/>,
    /// <see cref="IcalOptions.AllowCardinalityViolations"/>, <see cref="IcalOptions.AllowUndefinedTimeZones"/> and
    /// <see cref="IcalOptions.AllowInvalidValues"/>, all off by default.
    /// </para>
    /// <para>
    /// Any other exception from a read, including a cancellation, leaves the reader unable to continue, and later reads throw
    /// <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while
    /// another is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class IcalReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private readonly IcalCalendarBuilder _builder = new IcalCalendarBuilder();
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? _calendar;
#else
        private IcalCalendar _calendar;
#endif
        private bool _isFaulted;
        private readonly IcalOptions _options;
        private readonly IcalContentLineReader _reader;

        // These are changed with Interlocked, so that a read started on another thread while one is in progress is refused.
        private int _isDisposed;
        private int _isReadInProgress;

        /// <summary>
        /// Creates a reader for iCalendar data in a stream, read as UTF-8 unless the stream starts with a byte order mark.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support reading.</exception>
        public IcalReader(Stream icalStream) : this(icalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for iCalendar data in a stream.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support reading.
        /// -or-
        /// <see cref="IcalOptions.MaxLineLength"/> is less than 1.</exception>
        public IcalReader(Stream icalStream, IcalOptions options) : this(new IcalContentLineReader(icalStream, options), options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for an iCalendar file, read as UTF-8 unless the file starts with a byte order mark.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> is <c>null</c>.</exception>
        public IcalReader(string icalFilePath) : this(icalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for an iCalendar file.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.MaxLineLength"/> is less than 1.</exception>
        public IcalReader(string icalFilePath, IcalOptions options) : this(new IcalContentLineReader(icalFilePath, options), options)
        {
        }
#endif

        /// <summary>
        /// Creates a reader for iCalendar data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public IcalReader(TextReader reader) : this(reader, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for iCalendar data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the text reader has already decoded
        /// the data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="IcalOptions.MaxLineLength"/> is less than 1.</exception>
        public IcalReader(TextReader reader, IcalOptions options) : this(new IcalContentLineReader(reader, options), options)
        {
        }

        /// <summary>
        /// Creates a reader with a specific size for the internal character block of its content-line reader.  Allows tests to force refills at
        /// chosen points.
        /// </summary>
        internal IcalReader(TextReader reader, IcalOptions options, int bufferSize) : this(new IcalContentLineReader(reader, options, bufferSize), options)
        {
        }

        private IcalReader(IcalContentLineReader reader, IcalOptions options)
        {
            _reader = reader;
            _options = options;
        }

        /// <summary>
        /// Gets the number of VCALENDAR objects read so far, including any that failed a conformance check.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long CalendarsRead
        {
            get
            {
                ThrowIfDisposed();
                return _builder.CalendarsRead;
            }
        }

        /// <summary>
        /// Gets the number of components read so far, at any depth and not counting the VCALENDAR objects, including any in a calendar that
        /// failed a conformance check.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public long ComponentsRead
        {
            get
            {
                ThrowIfDisposed();
                return _builder.ComponentsRead;
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
        /// Reads the next calendar.
        /// </summary>
        /// <returns>The calendar; or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="IcalFileException">The data is not valid iCalendar: a content line is not valid, a <c>BEGIN</c> or <c>END</c> is not
        /// matched, the <c>VERSION</c> does not include 2.0, or the data holds no calendar.
        /// -or-
        /// The calendar breaks a rule of RFC 5545 that the options do not accept.  The calendar has been read, and reading can continue.</exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IcalCalendar? ReadCalendar()
#else
        public IcalCalendar ReadCalendar()
#endif
        {
            ThrowIfDisposed();
            BeginRead();
            try
            {
                while (true)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    IcalContentLine? line;
#else
                    IcalContentLine line;
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

                    if (AddLine(line)) return TakeCalendar();
                }
            }
            finally
            {
                EndRead();
            }
        }

        /// <summary>
        /// Reads the remaining calendars.
        /// </summary>
        /// <returns>The calendars, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="IcalFileException">During enumeration: the data is not valid iCalendar, or a calendar breaks a rule of RFC 5545 that
        /// the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was
        /// canceled.</exception>
        public IEnumerable<IcalCalendar> ReadCalendars()
        {
            // Check now, so the exception comes from this call and not from the first MoveNext.
            ThrowIfDisposed();

            return ReadCalendarsIterator();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Reads the next calendar asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the buffers of the content-line reader already hold the whole calendar.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot
        /// continue.</param>
        /// <returns>A task whose result is the calendar, or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="IcalFileException">By the task: the data is not valid iCalendar, or the calendar breaks a rule of RFC 5545 that the
        /// options do not accept.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ValueTask<IcalCalendar?> ReadCalendarAsync(CancellationToken cancellationToken = default)
#else
        public ValueTask<IcalCalendar> ReadCalendarAsync(CancellationToken cancellationToken = default)
#endif
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            BeginRead();
            return ReadCalendarCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Reads the remaining calendars asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is combined with any token passed to
        /// <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.  If a read is canceled, the reader cannot continue.</param>
        /// <returns>The calendars, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="IcalFileException">During enumeration: the data is not valid iCalendar, or a calendar breaks a rule of RFC 5545 that
        /// the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was
        /// canceled.</exception>
        /// <exception cref="OperationCanceledException">During enumeration: the read was canceled.</exception>
        public IAsyncEnumerable<IcalCalendar> ReadCalendarsAsync(CancellationToken cancellationToken = default)
        {
            // Check now, so the exception comes from this call and not from the first MoveNextAsync.
            ThrowIfDisposed();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            return ReadCalendarsAsyncIterator(cancellationToken);
#else
            return new AsyncCalendarEnumerable(this, cancellationToken);
#endif
        }
#endif

        private IEnumerable<IcalCalendar> ReadCalendarsIterator()
        {
            while (true)
            {
                var calendar = ReadCalendar();
                if (calendar is null) yield break;

                yield return calendar;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async IAsyncEnumerable<IcalCalendar> ReadCalendarsAsyncIterator([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            while (true)
            {
                var calendar = await ReadCalendarAsync(cancellationToken).ConfigureAwait(false);
                if (calendar is null) yield break;

                yield return calendar;
            }
        }
#endif

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Continues <see cref="ReadCalendarAsync"/>, and ends the read.  Completes synchronously, without allocating a task, while the
        /// content-line reader completes synchronously.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async ValueTask<IcalCalendar?> ReadCalendarCoreAsync(CancellationToken cancellationToken)
#else
        private async ValueTask<IcalCalendar> ReadCalendarCoreAsync(CancellationToken cancellationToken)
#endif
        {
            try
            {
                while (true)
                {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    IcalContentLine? line;
#else
                    IcalContentLine line;
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

                    if (AddLine(line)) return TakeCalendar();
                }
            }
            finally
            {
                EndRead();
            }
        }
#endif

        /// <summary>
        /// Adds a line, or the end of the data, to the calendar being built.
        /// </summary>
        /// <param name="line">The line; or <c>null</c> at the end of the data.</param>
        /// <returns><c>true</c> if the read has its result: a calendar, ended by this line and checked, or <c>null</c> at the end of the data;
        /// see <see cref="TakeCalendar"/>.</returns>
        /// <exception cref="IcalFileException">The line is not valid where it is, or the calendar it ends breaks a rule of RFC 5545 that the
        /// options do not accept.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private bool AddLine(IcalContentLine? line)
#else
        private bool AddLine(IcalContentLine line)
#endif
        {
            try
            {
                if (line is null)
                {
                    _builder.Complete();
                    _calendar = null;
                    return true;
                }

                _calendar = _builder.Add(line);
            }
            catch
            {
                // Where the calendar being built ends is now unknown.
                _isFaulted = true;
                throw;
            }

            if (_calendar is null) return false;

            // The calendar has been read in full, so a problem with it does not stop the reader.
            var diagnostics = new List<IcalDiagnostic>();
            IcalConformance.Validate(_calendar, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!IcalConformance.IsAccepted(diagnostic.Kind, _options))
                {
                    _calendar = null;
                    throw IcalConformance.BuildException(diagnostic);
                }
            }

            return true;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? TakeCalendar()
#else
        private IcalCalendar TakeCalendar()
#endif
        {
            var calendar = _calendar;
            _calendar = null;
            return calendar;
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
        /// The calendars of <see cref="ReadCalendarsAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncCalendarEnumerable : IAsyncEnumerable<IcalCalendar>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly IcalReader _reader;

            internal AsyncCalendarEnumerable(IcalReader reader, CancellationToken cancellationToken)
            {
                _reader = reader;
                _cancellationToken = cancellationToken;
            }

            public IAsyncEnumerator<IcalCalendar> GetAsyncEnumerator(CancellationToken cancellationToken = default(CancellationToken))
            {
                return new Enumerator(_reader, _cancellationToken, cancellationToken);
            }

            private sealed class Enumerator : IAsyncEnumerator<IcalCalendar>
            {
                private readonly CancellationToken _cancellationToken;
                private bool _isFinished;
                private readonly CancellationTokenSource _linkedTokenSource;
                private readonly IcalReader _reader;

                internal Enumerator(IcalReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
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

                public IcalCalendar Current { get; private set; }

                public async ValueTask<bool> MoveNextAsync()
                {
                    if (_isFinished) return false;

                    var calendar = await _reader.ReadCalendarAsync(_cancellationToken).ConfigureAwait(false);
                    Current = calendar;
                    _isFinished = calendar is null;
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
