using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a writer of the component object model as iCalendar data (RFC 5545).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each calendar is written as content lines by an <see cref="IcalContentLineWriter"/>, so the same options and rules apply to its lines:
    /// folding, line breaks, and the encoding of parameter values.  Properties are written in order, each followed by the components nested in
    /// it.  A property read by <see cref="IcalReader"/> keeps its parameters exactly as they were read.
    /// </para>
    /// <para>
    /// The calendar's <c>VERSION</c> is always written as <c>VERSION:2.0</c>, in the place of the calendar's own <c>VERSION</c>, or first if it
    /// has none.
    /// </para>
    /// <para>
    /// Before a calendar is written, it is checked as <see cref="IcalComponent.Validate"/> does.  If it breaks a rule of RFC 5545 that the
    /// options do not accept, <see cref="IcalFileException"/> is thrown, carrying the component path in its data, and nothing of that calendar is
    /// written.  The accommodations are <see cref="IcalOptions.AllowMissingRequiredProperties"/>,
    /// <see cref="IcalOptions.AllowCardinalityViolations"/>, <see cref="IcalOptions.AllowUndefinedTimeZones"/> and
    /// <see cref="IcalOptions.AllowInvalidValues"/>, all off by default.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another
    /// is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class IcalWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private long _calendarsWritten;
        private long _componentsWritten;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif
        private readonly IcalOptions _options;
        private readonly IcalContentLineWriter _writer;

        // These are changed with Interlocked, so that an operation started on another thread while one is in progress is refused.
        private int _isDisposed;
        private int _isOperationInProgress;

        /// <summary>
        /// Creates a writer of iCalendar data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support writing.</exception>
        public IcalWriter(Stream icalStream) : this(icalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of iCalendar data to a stream.
        /// </summary>
        /// <param name="icalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="icalStream"/> does not support writing.
        /// -or-
        /// <see cref="IcalOptions.FoldLength"/>, <see cref="IcalOptions.FoldWhitespace"/> or <see cref="IcalOptions.LineBreak"/> is not
        /// valid.</exception>
        public IcalWriter(Stream icalStream, IcalOptions options) : this(new IcalContentLineWriter(icalStream, options), options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of an iCalendar file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> is <c>null</c>.</exception>
        public IcalWriter(string icalFilePath) : this(icalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of an iCalendar file.  An existing file is overwritten.
        /// </summary>
        /// <param name="icalFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="icalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.FoldLength"/>, <see cref="IcalOptions.FoldWhitespace"/> or
        /// <see cref="IcalOptions.LineBreak"/> is not valid.</exception>
        public IcalWriter(string icalFilePath, IcalOptions options) : this(new IcalContentLineWriter(icalFilePath, options), options)
        {
        }
#endif

        /// <summary>
        /// Creates a writer of iCalendar data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public IcalWriter(TextWriter writer) : this(writer, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of iCalendar data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is neither flushed nor closed when the writer is disposed; call <see cref="Flush"/> first.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the text writer does its own
        /// encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="IcalOptions.FoldLength"/>, <see cref="IcalOptions.FoldWhitespace"/> or <see cref="IcalOptions.LineBreak"/> is not
        /// valid.</exception>
        public IcalWriter(TextWriter writer, IcalOptions options) : this(new IcalContentLineWriter(writer, options), options)
        {
        }

        private IcalWriter(IcalContentLineWriter writer, IcalOptions options)
        {
            _writer = writer;
            _options = options;
        }

        /// <summary>
        /// Gets the number of VCALENDAR objects written so far.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long CalendarsWritten
        {
            get
            {
                ThrowIfDisposed();
                return _calendarsWritten;
            }
        }

        /// <summary>
        /// Gets the number of components written so far, at any depth and not counting the VCALENDAR objects.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public long ComponentsWritten
        {
            get
            {
                ThrowIfDisposed();
                return _componentsWritten;
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
        /// Writes a calendar.
        /// </summary>
        /// <param name="calendar">The calendar.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendar"/> is <c>null</c>.</exception>
        /// <exception cref="IcalFileException">The calendar breaks a rule of RFC 5545 that the options do not accept, and nothing has been
        /// written.
        /// -or-
        /// A content line cannot be written: a property name is not valid, or a value holds a character that is not allowed.  The lines before
        /// it have been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        public void WriteCalendar(IcalCalendar calendar)
        {
            ThrowIfDisposed();
            if (calendar is null)
                throw new ArgumentNullException(nameof(calendar));

            BeginOperation();
            try
            {
                WriteCalendarCore(calendar);
            }
            finally
            {
                EndOperation();
            }
        }

        /// <summary>
        /// Writes calendars.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="calendars"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="calendars">The calendars.</param>
        /// <param name="progress">Receives the progress after each calendar has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the calendar it was reporting has been written.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="calendars"/> contains <c>null</c>.  The calendars before it have been
        /// written.</exception>
        /// <exception cref="IcalFileException">A calendar breaks a rule of RFC 5545 that the options do not accept, or one of its content lines
        /// cannot be written.  The calendars before it have been written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void WriteCalendars(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null)
#else
        public void WriteCalendars(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress> progress = null)
#endif
        {
            ThrowIfDisposed();
            if (calendars is null)
                throw new ArgumentNullException(nameof(calendars));

            BeginOperation();
            try
            {
                foreach (var calendar in calendars)
                {
                    ThrowIfDisposed();
                    WriteCalendarCore(CheckCalendar(calendar));
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
        /// Writes a calendar asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the underlying text writer takes every line without waiting, as a <see cref="StreamWriter"/>
        /// does while the lines fit its buffer.
        /// </remarks>
        /// <param name="calendar">The calendar.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the calendar has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendar"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="IcalFileException">By the task: the calendar breaks a rule of RFC 5545 that the options do not accept, and nothing
        /// has been written; or a content line cannot be written, and the lines before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteCalendarAsync(IcalCalendar calendar, CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the calendar or the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            if (calendar is null)
                throw new ArgumentNullException(nameof(calendar));

            BeginOperation();
            return WriteCalendarOperationAsync(calendar, cancellationToken);
        }

        /// <summary>
        /// Writes calendars asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="calendars"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="calendars">The calendars.</param>
        /// <param name="progress">Receives the progress after each calendar has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the calendar it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every calendar has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="calendars"/> contains <c>null</c>.  The calendars before it have
        /// been written.</exception>
        /// <exception cref="IcalFileException">By the task: a calendar breaks a rule of RFC 5545 that the options do not accept, or one of its
        /// content lines cannot be written.  The calendars before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteCalendarsAsync(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null,
#else
        public Task WriteCalendarsAsync(IEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (calendars is null)
                throw new ArgumentNullException(nameof(calendars));

            BeginOperation();
            return WriteCalendarsCoreAsync(calendars, progress, cancellationToken);
        }

        /// <summary>
        /// Writes calendars asynchronously.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="calendars"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="calendars">The calendars.  <paramref name="cancellationToken"/> is passed to their enumerator.</param>
        /// <param name="progress">Receives the progress after each calendar has been handed to the underlying text writer, synchronously on the
        /// writing thread; or <c>null</c>.  An exception it throws ends the call; the calendar it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every calendar has been handed to the underlying text writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="calendars"/> contains <c>null</c>.  The calendars before it have
        /// been written.</exception>
        /// <exception cref="IcalFileException">By the task: a calendar breaks a rule of RFC 5545 that the options do not accept, or one of its
        /// content lines cannot be written.  The calendars before it have been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Task WriteCalendarsAsync(IAsyncEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress>? progress = null,
#else
        public Task WriteCalendarsAsync(IAsyncEnumerable<IcalCalendar> calendars, IProgress<IcalWriteProgress> progress = null,
#endif
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            if (calendars is null)
                throw new ArgumentNullException(nameof(calendars));

            BeginOperation();
            return WriteAsyncCalendarsCoreAsync(calendars, progress, cancellationToken);
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

        private async ValueTask WriteCalendarOperationAsync(IcalCalendar calendar, CancellationToken cancellationToken)
        {
            try
            {
                await WriteCalendarCoreAsync(calendar, cancellationToken).ConfigureAwait(false);
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

        private async Task WriteCalendarsCoreAsync(IEnumerable<IcalCalendar> calendars,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<IcalWriteProgress>? progress,
#else
            IProgress<IcalWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            try
            {
                foreach (var calendar in calendars)
                {
                    ThrowIfDisposed();
                    cancellationToken.ThrowIfCancellationRequested();
                    await WriteCalendarCoreAsync(CheckCalendar(calendar), cancellationToken).ConfigureAwait(false);
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

        private async Task WriteAsyncCalendarsCoreAsync(IAsyncEnumerable<IcalCalendar> calendars,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IProgress<IcalWriteProgress>? progress,
#else
            IProgress<IcalWriteProgress> progress,
#endif
            CancellationToken cancellationToken)
        {
            try
            {
                var enumerator = calendars.GetAsyncEnumerator(cancellationToken);
                try
                {
                    while (true)
                    {
                        ThrowIfDisposed();
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) break;

                        ThrowIfDisposed();
                        cancellationToken.ThrowIfCancellationRequested();
                        await WriteCalendarCoreAsync(CheckCalendar(enumerator.Current), cancellationToken).ConfigureAwait(false);
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
        /// Checks a calendar and hands its lines to the content-line writer, asynchronously.
        /// </summary>
        private async Task WriteCalendarCoreAsync(IcalCalendar calendar, CancellationToken cancellationToken)
        {
            int componentCount;
            var lines = PrepareCalendar(calendar, out componentCount);
            await _writer.WriteLinesAsync(lines, null, cancellationToken).ConfigureAwait(false);
            CountCalendar(componentCount);
        }
#endif

        /// <summary>
        /// Checks a calendar and hands its lines to the content-line writer.
        /// </summary>
        private void WriteCalendarCore(IcalCalendar calendar)
        {
            int componentCount;
            var lines = PrepareCalendar(calendar, out componentCount);
            _writer.WriteLines(lines);
            CountCalendar(componentCount);
        }

        /// <summary>
        /// Checks a calendar, and gets the lines to write for it.
        /// </summary>
        /// <exception cref="IcalFileException">The calendar breaks a rule of RFC 5545 that the options do not accept.</exception>
        private List<IcalContentLine> PrepareCalendar(IcalCalendar calendar, out int componentCount)
        {
            var diagnostics = new List<IcalDiagnostic>();
            IcalConformance.Validate(calendar, true, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!IcalConformance.IsAccepted(diagnostic.Kind, _options))
                    throw IcalConformance.BuildException(diagnostic);
            }

            // Gather the lines first, so that a calendar changed by another thread cannot fail part way through for that reason.
            var lines = new List<IcalContentLine>();
            componentCount = 0;
            lines.Add(new IcalContentLine("BEGIN", calendar.Name));
            var isVersionWritten = false;
            foreach (var property in calendar.Properties)
            {
                if (IcalComponent.IsNamed(property, "VERSION"))
                {
                    if (isVersionWritten) continue;

                    lines.Add(new IcalContentLine("VERSION", IcalConformance.SupportedVersion));
                    isVersionWritten = true;
                }
                else
                {
                    lines.Add(property);
                }
            }

            if (!isVersionWritten)
            {
                lines.Insert(1, new IcalContentLine("VERSION", IcalConformance.SupportedVersion));
            }

            foreach (var component in calendar.Components)
            {
                AddComponent(component, lines, ref componentCount);
            }

            lines.Add(new IcalContentLine("END", calendar.Name));
            return lines;
        }

        private static void AddComponent(IcalComponent component, List<IcalContentLine> lines, ref int componentCount)
        {
            lines.Add(new IcalContentLine("BEGIN", component.Name));
            foreach (var property in component.Properties)
            {
                lines.Add(property);
            }

            foreach (var child in component.Components)
            {
                AddComponent(child, lines, ref componentCount);
            }

            lines.Add(new IcalContentLine("END", component.Name));
            componentCount++;
        }

        private void CountCalendar(int componentCount)
        {
            _componentsWritten += componentCount;
            _calendarsWritten++;
        }

        private IcalWriteProgress GetProgress()
        {
            return new IcalWriteProgress(_writer.LinesWritten, _componentsWritten, _calendarsWritten);
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
        private static IcalCalendar CheckCalendar(IcalCalendar? calendar)
#else
        private static IcalCalendar CheckCalendar(IcalCalendar calendar)
#endif
        {
            if (calendar is null)
                throw new ArgumentException("The calendars cannot contain null.", "calendars");

            return calendar;
        }
    }
}
