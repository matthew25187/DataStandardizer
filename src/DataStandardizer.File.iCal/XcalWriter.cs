using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a writer of the component object model as xCal data, the XML form of iCalendar (RFC 6321).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The calendars are written in an <c>icalendar</c> element in the namespace <c>urn:ietf:params:xml:ns:icalendar-2.0</c>, which is started
    /// with the first calendar and ended when the writer is disposed.  Component, property and parameter names are written in lower case, and
    /// each value in its typed value element, such as <c>date-time</c>, converted from the text form of RFC 5545 (RFC 6321 §3).  In
    /// particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <c>VALUE</c> parameter is not written, since the value element gives the type (RFC 6321
    /// §3.5.1).</description></item>
    /// <item><description>The value of a property whose type is not known, such as an <c>X-</c> property with no <c>VALUE</c> parameter, is
    /// written as it is in an <c>unknown</c> element (RFC 6321 §5).</description></item>
    /// <item><description>Each value of a multi-valued property, such as CATEGORIES or EXDATE, is an element of its own (RFC 6321
    /// §3.4.1.1).</description></item>
    /// <item><description>PERIOD, RECUR, GEO and REQUEST-STATUS values are written as child elements, and a RECUR value keeps the order of its
    /// rule parts.</description></item>
    /// <item><description>A value that is not valid for its type, which <see cref="IcalOptions.AllowInvalidValues"/> lets a calendar hold, is
    /// written as it is in an <c>unknown</c> element, keeping any <c>VALUE</c> parameter.</description></item>
    /// </list>
    /// <para>
    /// So a calendar written by this writer and read back by <see cref="XcalReader"/> has the same properties, with the same values; only the
    /// spelling of a value may change, such as TEXT escaping, the case of a BOOLEAN, or the place of a <c>VALUE</c> parameter, which is read
    /// back after the other parameters.  A <c>VALUE</c> parameter that names the property's default type is not read back, since xCal cannot
    /// tell it apart from none.  The calendar's <c>VERSION</c> is always written as 2.0, in the place of the calendar's own <c>VERSION</c>, or
    /// first if it has none.
    /// </para>
    /// <para>
    /// Before a calendar is written, it is checked as <see cref="IcalComponent.Validate"/> does, against the same rules and options as
    /// <see cref="IcalWriter"/>.  If it breaks a rule of RFC 5545 that the options do not accept, <see cref="IcalFileException"/> is thrown,
    /// carrying the component path in its data, and nothing of that calendar is written.  The calendar is also refused if a name cannot be an
    /// XML element name, or a value holds a character that XML cannot hold.
    /// </para>
    /// <para>
    /// Of the options, <see cref="IcalOptions.Encoding"/> sets the encoding of a stream or file, and <see cref="IcalOptions.LineBreak"/> the
    /// line breaks of the indented XML.  The others that concern <c>.ics</c> content lines do not apply.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another
    /// is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class XcalWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Encoding used when none is specified: UTF-8 without a byte order mark, throwing on invalid characters.
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        private long _calendarsWritten;
        private long _componentsWritten;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif
        private readonly bool _isInternalWriter;
        private bool _isRootStarted;
        private long _linesWritten;

        // The number of elements started and not yet ended, including the root, so that disposal can end them.
        private int _openElementCount;
        private long _propertiesWritten;
        private readonly IcalOptions _options;
        private readonly XmlWriter _writer;

        // These are changed with Interlocked, so that an operation started on another thread while one is in progress is refused, and a Dispose
        // from another thread releases the writer once that operation has finished.
        private int _isDisposed;
        private int _isOperationInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a writer of xCal data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="xcalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcalStream"/> does not support writing.</exception>
        public XcalWriter(Stream xcalStream) : this(xcalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of xCal data to a stream.
        /// </summary>
        /// <param name="xcalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcalStream"/> does not support writing.
        /// -or-
        /// <see cref="IcalOptions.LineBreak"/> is not valid.</exception>
        public XcalWriter(Stream xcalStream, IcalOptions options) : this(CreateStreamWriter(xcalStream, options, false), true, options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of an xCal file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="xcalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalFilePath"/> is <c>null</c>.</exception>
        public XcalWriter(string xcalFilePath) : this(xcalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of an xCal file.  An existing file is overwritten.
        /// </summary>
        /// <param name="xcalFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.LineBreak"/> is not valid.</exception>
        public XcalWriter(string xcalFilePath, IcalOptions options) : this(CreateFileWriter(xcalFilePath, options), true, options)
        {
        }
#endif

        /// <summary>
        /// Creates a writer of xCal data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is not closed when the writer is disposed, but what has been written is flushed to it.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public XcalWriter(TextWriter writer) : this(writer, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of xCal data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is not closed when the writer is disposed, but what has been written is flushed to it.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the text writer does its own
        /// encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="IcalOptions.LineBreak"/> is not valid.</exception>
        public XcalWriter(TextWriter writer, IcalOptions options) : this(CreateTextWriter(writer, options), true, options)
        {
        }

        /// <summary>
        /// Creates a writer of xCal data to an XML writer, which may be writing a larger document.
        /// </summary>
        /// <remarks>
        /// The <c>icalendar</c> element is written at the XML writer's current position, and ended when the writer is disposed.  The XML writer's
        /// settings apply, such as its indentation.
        /// </remarks>
        /// <param name="writer">The XML writer.  It is neither flushed nor closed when the writer is disposed.  For the asynchronous members, its
        /// <see cref="XmlWriterSettings.Async"/> setting must be <c>true</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public XcalWriter(XmlWriter writer) : this(writer, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of xCal data to an XML writer, which may be writing a larger document.
        /// </summary>
        /// <remarks>
        /// The <c>icalendar</c> element is written at the XML writer's current position, and ended when the writer is disposed.  The XML writer's
        /// settings apply, such as its indentation.
        /// </remarks>
        /// <param name="writer">The XML writer.  It is neither flushed nor closed when the writer is disposed.  For the asynchronous members, its
        /// <see cref="XmlWriterSettings.Async"/> setting must be <c>true</c>.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the XML writer does its own
        /// encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.</exception>
        public XcalWriter(XmlWriter writer, IcalOptions options) : this(ValidateXmlWriter(writer, options), false, options)
        {
        }

        private XcalWriter(XmlWriter writer, bool isInternalWriter, IcalOptions options)
        {
            _writer = writer;
            _isInternalWriter = isInternalWriter;
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
        /// Gets the number of properties written so far, in all components and calendars.
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
        /// Ends the <c>icalendar</c> element, then flushes and closes the XML writer the writer created itself, with the stream or file it opened.
        /// A caller-supplied stream or text writer is left open, and a caller-supplied XML writer is neither flushed nor closed.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and the
        /// document is ended once that operation has finished.
        /// </remarks>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return;

            if (Volatile.Read(ref _isOperationInProgress) == 0 && MarkReleased())
            {
                Release();
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Ends the <c>icalendar</c> element, then flushes and closes, asynchronously, the XML writer the writer created itself, with the stream or
        /// file it opened.  A caller-supplied stream or text writer is left open, and a caller-supplied XML writer is neither flushed nor closed.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the document is ended once that operation has finished.  The XML writer is disposed even if
        /// ending the document throws.
        /// </remarks>
        /// <returns>A task that completes once the document has been ended and the writer released.</returns>
        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return default(ValueTask);

            if (Volatile.Read(ref _isOperationInProgress) == 0 && MarkReleased())
            {
                return ReleaseAsync();
            }

            return default(ValueTask);
        }
#endif

        /// <summary>
        /// Flushes the XML writer, whether the writer created it or it was supplied by the caller.  The <c>icalendar</c> element is not ended.
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
                if (EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Writes a calendar.
        /// </summary>
        /// <param name="calendar">The calendar.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendar"/> is <c>null</c>.</exception>
        /// <exception cref="IcalFileException">The calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be written in
        /// xCal, and nothing has been written.</exception>
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
                if (EndOperation())
                {
                    Release();
                }
            }
        }

        /// <summary>
        /// Writes calendars.
        /// </summary>
        /// <remarks>
        /// The whole call is one operation, so neither <paramref name="calendars"/> nor <paramref name="progress"/> may call the writer.
        /// </remarks>
        /// <param name="calendars">The calendars.</param>
        /// <param name="progress">Receives the progress after each calendar has been handed to the XML writer, synchronously on the writing
        /// thread; or <c>null</c>.  Its <see cref="IcalWriteProgress.LinesWritten"/> is the number of content lines the calendars written so far
        /// would have in <c>.ics</c> form, so it is the same as <see cref="IcalWriter"/> reports.  An exception it throws ends the call; the
        /// calendar it was reporting has been written.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="calendars"/> contains <c>null</c>.  The calendars before it have been
        /// written.</exception>
        /// <exception cref="IcalFileException">A calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be written in
        /// xCal.  The calendars before it have been written.</exception>
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
                if (EndOperation())
                {
                    Release();
                }
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Flushes the XML writer asynchronously, whether the writer created it or it was supplied by the caller.  The <c>icalendar</c> element is
        /// not ended.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the flush is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the XML writer has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the flush is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.
        /// -or-
        /// The writer was created from an XML writer whose <see cref="XmlWriterSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public Task FlushAsync(CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            CheckAsync();
            BeginOperation();
            return FlushCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Writes a calendar asynchronously.
        /// </summary>
        /// <param name="calendar">The calendar.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the calendar has been handed to the XML writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendar"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.
        /// -or-
        /// The writer was created from an XML writer whose <see cref="XmlWriterSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="IcalFileException">By the task: the calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be
        /// written in xCal, and nothing has been written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteCalendarAsync(IcalCalendar calendar, CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the calendar or the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            if (calendar is null)
                throw new ArgumentNullException(nameof(calendar));

            CheckAsync();
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
        /// <param name="progress">Receives the progress after each calendar has been handed to the XML writer, synchronously on the writing
        /// thread; or <c>null</c>.  Its <see cref="IcalWriteProgress.LinesWritten"/> is the number of content lines the calendars written so far
        /// would have in <c>.ics</c> form.  An exception it throws ends the call; the calendar it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every calendar has been handed to the XML writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.
        /// -or-
        /// The writer was created from an XML writer whose <see cref="XmlWriterSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="calendars"/> contains <c>null</c>.  The calendars before it have
        /// been written.</exception>
        /// <exception cref="IcalFileException">By the task: a calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be
        /// written in xCal.  The calendars before it have been written.</exception>
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

            CheckAsync();
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
        /// <param name="progress">Receives the progress after each calendar has been handed to the XML writer, synchronously on the writing
        /// thread; or <c>null</c>.  Its <see cref="IcalWriteProgress.LinesWritten"/> is the number of content lines the calendars written so far
        /// would have in <c>.ics</c> form.  An exception it throws ends the call; the calendar it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every calendar has been handed to the XML writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.
        /// -or-
        /// The writer was created from an XML writer whose <see cref="XmlWriterSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="calendars"/> contains <c>null</c>.  The calendars before it have
        /// been written.</exception>
        /// <exception cref="IcalFileException">By the task: a calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be
        /// written in xCal.  The calendars before it have been written.</exception>
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

            CheckAsync();
            BeginOperation();
            return WriteAsyncCalendarsCoreAsync(calendars, progress, cancellationToken);
        }

        private async Task FlushCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _writer.FlushAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _isCanceled = true;
                throw;
            }
            finally
            {
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
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
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
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
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
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
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Checks a calendar and hands its elements to the XML writer, asynchronously.
        /// </summary>
        private async Task WriteCalendarCoreAsync(IcalCalendar calendar, CancellationToken cancellationToken)
        {
            // Cancellation is checked only before the calendar is started, so that a canceled write never leaves part of a calendar that
            // disposal would then close as though it were complete.
            cancellationToken.ThrowIfCancellationRequested();
            int componentCount, propertyCount;
            var element = PrepareCalendar(calendar, out componentCount, out propertyCount);
            if (!_isRootStarted)
            {
                if (_isInternalWriter)
                {
                    await _writer.WriteStartDocumentAsync().ConfigureAwait(false);
                }

                await _writer.WriteStartElementAsync(null, XcalNames.ICalendar, XcalNames.Namespace).ConfigureAwait(false);
                _openElementCount++;
                _isRootStarted = true;
            }

            await WriteElementAsync(element).ConfigureAwait(false);
            CountCalendar(componentCount, propertyCount);
        }

        private async Task WriteElementAsync(XcalElement element)
        {
            await _writer.WriteStartElementAsync(null, element.Name, XcalNames.Namespace).ConfigureAwait(false);
            _openElementCount++;
            if (element.Children.Count == 0)
            {
                await _writer.WriteStringAsync(element.Text).ConfigureAwait(false);
            }

            foreach (var child in element.Children)
            {
                await WriteElementAsync(child).ConfigureAwait(false);
            }

            await _writer.WriteEndElementAsync().ConfigureAwait(false);
            _openElementCount--;
        }

        /// <summary>
        /// Ends the document, then flushes and closes the XML writer the writer created itself, asynchronously.
        /// </summary>
        private async ValueTask ReleaseAsync()
        {
            if (!IsAsync(_writer))
            {
                Release();
                return;
            }

            try
            {
                if (_writer.WriteState != WriteState.Error && _writer.WriteState != WriteState.Closed)
                {
                    while (_openElementCount > 0)
                    {
                        await _writer.WriteEndElementAsync().ConfigureAwait(false);
                        _openElementCount--;
                    }

                    if (_isInternalWriter)
                    {
                        await _writer.FlushAsync().ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                if (_isInternalWriter)
                {
#if NETCOREAPP3_0_OR_GREATER
                    await _writer.DisposeAsync().ConfigureAwait(false);
#else
                    // XmlWriter has no DisposeAsync here, and Dispose would flush the caller's stream or text writer synchronously, even with
                    // nothing left to write.  An XML writer over the caller's output leaves it open, so disposing it would do nothing else.
                    if (_writer.Settings is null || _writer.Settings.CloseOutput)
                    {
                        _writer.Dispose();
                    }
#endif
                }
            }
        }

        private void CheckAsync()
        {
            if (!IsAsync(_writer))
                throw new InvalidOperationException("The XML writer does not allow asynchronous writes.  Create it with XmlWriterSettings.Async set to true.");
        }

        private static bool IsAsync(XmlWriter writer)
        {
            return writer.Settings is null || writer.Settings.Async;
        }
#endif

        /// <summary>
        /// Checks a calendar and hands its elements to the XML writer.
        /// </summary>
        private void WriteCalendarCore(IcalCalendar calendar)
        {
            int componentCount, propertyCount;
            var element = PrepareCalendar(calendar, out componentCount, out propertyCount);
            if (!_isRootStarted)
            {
                if (_isInternalWriter)
                {
                    _writer.WriteStartDocument();
                }

                _writer.WriteStartElement(XcalNames.ICalendar, XcalNames.Namespace);
                _openElementCount++;
                _isRootStarted = true;
            }

            WriteElement(element);
            CountCalendar(componentCount, propertyCount);
        }

        private void WriteElement(XcalElement element)
        {
            _writer.WriteStartElement(element.Name, XcalNames.Namespace);
            _openElementCount++;
            if (element.Children.Count == 0)
            {
                _writer.WriteString(element.Text);
            }

            foreach (var child in element.Children)
            {
                WriteElement(child);
            }

            _writer.WriteEndElement();
            _openElementCount--;
        }

        /// <summary>
        /// Checks a calendar, and builds the elements to write for it.
        /// </summary>
        /// <exception cref="IcalFileException">The calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be written in
        /// xCal.</exception>
        private XcalElement PrepareCalendar(IcalCalendar calendar, out int componentCount, out int propertyCount)
        {
            var diagnostics = new List<IcalDiagnostic>();
            IcalConformance.Validate(calendar, true, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!IcalConformance.IsAccepted(diagnostic.Kind, _options))
                    throw IcalConformance.BuildException(diagnostic);
            }

            // Build the elements first, so that a calendar that cannot be written in xCal, or is changed by another thread, fails before any of
            // it is written.
            componentCount = 0;
            var version = XcalConverter.ToElement(new IcalContentLine(IcalPropertyNames.Version, IcalConformance.SupportedVersion));
            var propertyElements = new List<XcalElement>();
            var isVersionWritten = false;
            foreach (var property in calendar.Properties)
            {
                if (IcalComponent.IsNamed(property, IcalPropertyNames.Version))
                {
                    if (isVersionWritten) continue;

                    propertyElements.Add(version);
                    isVersionWritten = true;
                }
                else
                {
                    propertyElements.Add(XcalConverter.ToElement(property));
                }
            }

            if (!isVersionWritten)
            {
                propertyElements.Insert(0, version);
            }

            propertyCount = propertyElements.Count;
            var element = new XcalElement(XcalConverter.ToElementName(calendar.Name, calendar.LineNumber));
            var properties = element.Add(new XcalElement(XcalNames.Properties));
            foreach (var propertyElement in propertyElements)
            {
                properties.Add(propertyElement);
            }

            // ref. RFC 6321 Appendix A §3.6: a vcalendar has a components element, even if it is empty.
            var components = element.Add(new XcalElement(XcalNames.Components));
            foreach (var component in calendar.Components)
            {
                AddComponent(component, components, ref componentCount, ref propertyCount);
            }

            return element;
        }

        private static void AddComponent(IcalComponent component, XcalElement parent, ref int componentCount, ref int propertyCount)
        {
            var element = parent.Add(new XcalElement(XcalConverter.ToElementName(component.Name, component.LineNumber)));
            var properties = element.Add(new XcalElement(XcalNames.Properties));
            foreach (var property in component.Properties)
            {
                properties.Add(XcalConverter.ToElement(property));
                propertyCount++;
            }

            if (component.Components.Count != 0)
            {
                var components = element.Add(new XcalElement(XcalNames.Components));
                foreach (var child in component.Components)
                {
                    AddComponent(child, components, ref componentCount, ref propertyCount);
                }
            }

            componentCount++;
        }

        private void CountCalendar(int componentCount, int propertyCount)
        {
            _componentsWritten += componentCount;
            _propertiesWritten += propertyCount;
            _calendarsWritten++;

            // The lines of the calendar in .ics form: a BEGIN and an END for it and each component, and one for each property.
            _linesWritten += 2 * (componentCount + 1) + propertyCount;
        }

        private IcalWriteProgress GetProgress()
        {
            return new IcalWriteProgress(_linesWritten, _componentsWritten, _calendarsWritten);
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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            if (_isCanceled)
            {
                if (EndOperation())
                {
                    Release();
                }

                throw new InvalidOperationException("The writer cannot continue after an operation was canceled.");
            }
#endif
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
        /// Ends the document, then flushes and closes the XML writer the writer created itself.
        /// </summary>
        private void Release()
        {
            try
            {
                if (_writer.WriteState != WriteState.Error && _writer.WriteState != WriteState.Closed)
                {
                    while (_openElementCount > 0)
                    {
                        _writer.WriteEndElement();
                        _openElementCount--;
                    }
                }
            }
            finally
            {
                if (_isInternalWriter)
                {
                    _writer.Dispose();
                }
            }
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

        private static XmlWriterSettings CreateSettings(IcalOptions options, Encoding encoding, bool closeOutput)
        {
            if (options.LineBreak != "\r\n" && options.LineBreak != "\n")
                throw new ArgumentException("The line break must be CRLF or, as an opt-in, a bare line feed.", nameof(options));

            var settings = new XmlWriterSettings
            {
                CloseOutput = closeOutput,
                Encoding = encoding,
                Indent = true,
                IndentChars = "  ",
                NewLineChars = options.LineBreak
            };
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            settings.Async = true;
#endif
            return settings;
        }

        private static XmlWriter CreateStreamWriter(Stream stream, IcalOptions options, bool ownsStream)
        {
            if (stream is null)
                throw new ArgumentNullException("xcalStream");
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "xcalStream");

            return XmlWriter.Create(stream, CreateSettings(options, options.Encoding ?? DefaultEncoding, ownsStream));
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static XmlWriter CreateFileWriter(string path, IcalOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("xcalFilePath");
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            var settings = CreateSettings(options, options.Encoding ?? DefaultEncoding, true);
            var fileStream = IcalFileStreams.OpenWrite(path, 4096);
            try
            {
                return XmlWriter.Create(fileStream, settings);
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
        }
#endif

        private static XmlWriter CreateTextWriter(TextWriter writer, IcalOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(options));

            return XmlWriter.Create(writer, CreateSettings(options, DefaultEncoding, false));
        }

        private static XmlWriter ValidateXmlWriter(XmlWriter writer, IcalOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(options));

            return writer;
        }
    }
}
