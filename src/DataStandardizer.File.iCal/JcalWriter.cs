#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a writer of the component object model as jCal data, the JSON form of iCalendar (RFC 7265).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is written as UTF-8 with no byte order mark, as compact JSON with no indentation.  A single calendar is written as its
    /// component array, <c>["vcalendar", [...], [...]]</c>, and two or more as an array of component arrays (RFC 7265 §3.2).  Since which of
    /// the two the document is depends on whether a second calendar follows, the first calendar is held in memory until a second is written or
    /// the writer is disposed; <see cref="Flush"/> does not write it.  If no calendar is written, nothing is.
    /// </para>
    /// <para>
    /// Component, property and parameter names are written in lower case, and each value as the JSON value of its type, converted from the text
    /// form of RFC 5545 (RFC 7265 §3).  In particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <c>VALUE</c> parameter is not written, since the type identifier gives the type (RFC 7265
    /// §3.5.1).</description></item>
    /// <item><description>The value of a property whose type is not known, such as an <c>X-</c> property with no <c>VALUE</c> parameter, is
    /// written as it is with the <c>unknown</c> type (RFC 7265 §5).  A <c>VALUE</c> parameter that names a value type RFC 5545 does not define,
    /// such as <c>X-MYTYPE</c>, is written as the type identifier, and the value as it is.</description></item>
    /// <item><description>Each value of a multi-valued property, such as CATEGORIES or EXDATE, is an element of the property array (RFC 7265
    /// §3.4), and a parameter with more than one value is an array of strings (RFC 7265 §3.5.2).</description></item>
    /// <item><description>INTEGER and FLOAT values are JSON numbers, BOOLEAN values JSON literals, a PERIOD an array of two strings, a RECUR
    /// value an object whose members keep the order of its rule parts, and GEO and REQUEST-STATUS values arrays.</description></item>
    /// <item><description>A value that is not valid for its type, which <see cref="IcalOptions.AllowInvalidValues"/> lets a calendar hold, is
    /// written as it is with the <c>unknown</c> type, keeping any <c>VALUE</c> parameter.</description></item>
    /// </list>
    /// <para>
    /// So a calendar written by this writer and read back by <see cref="JcalReader"/> has the same properties, with the same values; only the
    /// spelling of a value may change, such as TEXT escaping, the case of a BOOLEAN, the digits of an INTEGER, or the place of a <c>VALUE</c>
    /// parameter, which is read back after the other parameters.  A <c>VALUE</c> parameter that names the property's default type is not read
    /// back, since jCal cannot tell it apart from none.  The calendar's <c>VERSION</c> is always written as 2.0, in the place of the calendar's
    /// own <c>VERSION</c>, or first if it has none.
    /// </para>
    /// <para>
    /// Before a calendar is written, it is checked as <see cref="IcalComponent.Validate"/> does, against the same rules and options as
    /// <see cref="IcalWriter"/>.  If it breaks a rule of RFC 5545 that the options do not accept, <see cref="IcalFileException"/> is thrown,
    /// carrying the component path in its data, and nothing of that calendar is written.  The calendar is also refused if a name is not a valid
    /// iCalendar name, a property has two parameters of the same name, which a JSON object cannot hold, or a value holds a lone surrogate.
    /// </para>
    /// <para>
    /// Each calendar is encoded with <see cref="Utf8JsonWriter"/> into pooled blocks of bytes before any of it is written to the stream; the
    /// asynchronous members then write it asynchronously, and do no synchronous I/O.  Of the options, only <see cref="IcalOptions.Encoding"/>
    /// applies, and it must be UTF-8 if it is set; those that concern <c>.ics</c> content lines, such as <see cref="IcalOptions.LineBreak"/>, do
    /// not.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another
    /// is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// jCal needs System.Text.Json, so this type is not available on netstandard1.1.
    /// </para>
    /// </remarks>
    public sealed class JcalWriter : IDisposable, IAsyncDisposable
    {
        private static readonly byte[] ArrayStart = { (byte)'[' };
        private static readonly byte[] ArrayEnd = { (byte)']' };

        private long _calendarsWritten;
        private long _componentsWritten;

        // The calendar being encoded; and the first calendar, held until it is known whether the document is one calendar or an array of them.
        private readonly JcalBufferWriter _encoded = new JcalBufferWriter();
        private readonly JcalBufferWriter _held = new JcalBufferWriter();
        private bool _isArrayStarted;
        private bool _isCanceled;
        private bool _isHeld;
        private readonly Utf8JsonWriter _jsonWriter;
        private long _linesWritten;
        private readonly IcalOptions _options;
        private readonly bool _ownsStream;
        private long _propertiesWritten;
        private readonly Stream _stream;

        // These are changed with Interlocked, so that an operation started on another thread while one is in progress is refused, and a Dispose
        // from another thread releases the writer once that operation has finished.
        private int _isDisposed;
        private int _isOperationInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a writer of jCal data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="jcalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcalStream"/> does not support writing.</exception>
        public JcalWriter(Stream jcalStream) : this(jcalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of jCal data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="jcalStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCal is always
        /// UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcalStream"/> does not support writing.
        /// -or-
        /// <see cref="IcalOptions.Encoding"/> is not UTF-8.</exception>
        public JcalWriter(Stream jcalStream, IcalOptions options) : this(ValidateStream(jcalStream, options), false, options)
        {
        }

        /// <summary>
        /// Creates a writer of a jCal file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="jcalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalFilePath"/> is <c>null</c>.</exception>
        public JcalWriter(string jcalFilePath) : this(jcalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a writer of a jCal file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="jcalFilePath">The path of the file.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCal is always
        /// UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is not UTF-8.</exception>
        public JcalWriter(string jcalFilePath, IcalOptions options) : this(OpenFile(jcalFilePath, options), true, options)
        {
        }

        private JcalWriter(Stream stream, bool ownsStream, IcalOptions options)
        {
            _stream = stream;
            _ownsStream = ownsStream;
            _options = options;

            // Characters outside ASCII are written as they are, not as \u escapes, since the data is not embedded in HTML.
            _jsonWriter = new Utf8JsonWriter(_encoded, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }

        /// <summary>
        /// Gets the number of VCALENDAR objects written so far, including a first calendar that is held until the next is written.
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
        /// Ends the document, writing the first calendar if it is the only one, then flushes the stream, closes the file the writer opened and
        /// returns the pooled buffers.  A caller-supplied stream is flushed but left open.
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

        /// <summary>
        /// Ends the document asynchronously, writing the first calendar if it is the only one, then flushes the stream, closes the file the writer
        /// opened and returns the pooled buffers.  A caller-supplied stream is flushed but left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the document is ended once that operation has finished.  The file is closed even if ending
        /// the document throws.
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

        /// <summary>
        /// Flushes the stream.  A first calendar that is held until the next is written is not written.
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
        /// Writes a calendar.
        /// </summary>
        /// <param name="calendar">The calendar.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendar"/> is <c>null</c>.</exception>
        /// <exception cref="IcalFileException">The calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be written in
        /// jCal, and nothing has been written.</exception>
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
        /// <param name="progress">Receives the progress after each calendar has been written, synchronously on the writing thread; or
        /// <c>null</c>.  Its <see cref="IcalWriteProgress.LinesWritten"/> is the number of content lines the calendars written so far would have
        /// in <c>.ics</c> form, so it is the same as <see cref="IcalWriter"/> reports.  An exception it throws ends the call; the calendar it was
        /// reporting has been written.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="calendars"/> contains <c>null</c>.  The calendars before it have been
        /// written.</exception>
        /// <exception cref="IcalFileException">A calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be written in
        /// jCal.  The calendars before it have been written.</exception>
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

        /// <summary>
        /// Flushes the stream asynchronously.  A first calendar that is held until the next is written is not written.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the flush is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once the stream has been flushed.</returns>
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
        /// <param name="calendar">The calendar.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked before the calendar is written, so that a
        /// canceled write never leaves part of a calendar.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the calendar has been written to the stream, or held as the first calendar.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendar"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="IcalFileException">By the task: the calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be
        /// written in jCal, and nothing has been written.</exception>
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
        /// <param name="progress">Receives the progress after each calendar has been written, synchronously on the writing thread; or
        /// <c>null</c>.  Its <see cref="IcalWriteProgress.LinesWritten"/> is the number of content lines the calendars written so far would have
        /// in <c>.ics</c> form.  An exception it throws ends the call; the calendar it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked between calendars.  If the write is
        /// canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once every calendar has been written.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="calendars"/> contains <c>null</c>.  The calendars before it have
        /// been written.</exception>
        /// <exception cref="IcalFileException">By the task: a calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be
        /// written in jCal.  The calendars before it have been written.</exception>
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
        /// <param name="progress">Receives the progress after each calendar has been written, synchronously on the writing thread; or
        /// <c>null</c>.  Its <see cref="IcalWriteProgress.LinesWritten"/> is the number of content lines the calendars written so far would have
        /// in <c>.ics</c> form.  An exception it throws ends the call; the calendar it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked between calendars.  If the write is
        /// canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once every calendar has been written.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed
        /// while the write is in progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="calendars"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, so the writer cannot continue.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="calendars"/> contains <c>null</c>.  The calendars before it have
        /// been written.</exception>
        /// <exception cref="IcalFileException">By the task: a calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be
        /// written in jCal.  The calendars before it have been written.</exception>
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
                cancellationToken.ThrowIfCancellationRequested();
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
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
        /// Checks and encodes a calendar, then writes it to the stream asynchronously.
        /// </summary>
        private async Task WriteCalendarCoreAsync(IcalCalendar calendar, CancellationToken cancellationToken)
        {
            // Cancellation is checked only before the calendar is started, so that a canceled write never leaves part of a calendar that
            // disposal would then close as though it were complete.
            cancellationToken.ThrowIfCancellationRequested();
            int componentCount, propertyCount;
            Encode(calendar, out componentCount, out propertyCount);
            if (_calendarsWritten != 0)
            {
                if (_isHeld)
                {
                    await _stream.WriteAsync(ArrayStart, 0, ArrayStart.Length, CancellationToken.None).ConfigureAwait(false);
                    await _stream.WriteAsync(_held.Buffer, 0, _held.Count, CancellationToken.None).ConfigureAwait(false);
                    ReleaseHeld();
                }

                await _stream.WriteAsync(_encoded.Buffer, 0, _encoded.Count, CancellationToken.None).ConfigureAwait(false);
            }

            CountCalendar(componentCount, propertyCount);
        }

        /// <summary>
        /// Ends the document, flushes the stream and closes the file the writer opened, asynchronously.
        /// </summary>
        private async ValueTask ReleaseAsync()
        {
            try
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
        /// Checks and encodes a calendar, then writes it to the stream.
        /// </summary>
        private void WriteCalendarCore(IcalCalendar calendar)
        {
            int componentCount, propertyCount;
            Encode(calendar, out componentCount, out propertyCount);
            if (_calendarsWritten != 0)
            {
                if (_isHeld)
                {
                    _stream.Write(ArrayStart, 0, ArrayStart.Length);
                    _stream.Write(_held.Buffer, 0, _held.Count);
                    ReleaseHeld();
                }

                _stream.Write(_encoded.Buffer, 0, _encoded.Count);
            }

            CountCalendar(componentCount, propertyCount);
        }

        /// <summary>
        /// Checks a calendar, and encodes it as JSON into the buffer, after a comma if it follows another.  The first calendar is held.
        /// </summary>
        /// <exception cref="IcalFileException">The calendar breaks a rule of RFC 5545 that the options do not accept, or cannot be written in
        /// jCal.</exception>
        private void Encode(IcalCalendar calendar, out int componentCount, out int propertyCount)
        {
            var diagnostics = new List<IcalDiagnostic>();
            IcalConformance.Validate(calendar, true, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!IcalConformance.IsAccepted(diagnostic.Kind, _options))
                    throw IcalConformance.BuildException(diagnostic);
            }

            // Encode the whole calendar first, so that a calendar that cannot be written in jCal, or is changed by another thread, fails before
            // any of it is written.
            var buffer = _calendarsWritten == 0 ? _held : _encoded;
            buffer.Clear();
            if (_calendarsWritten != 0)
            {
                buffer.Write((byte)',');
            }

            _jsonWriter.Reset(buffer);
            componentCount = 0;
            propertyCount = 0;
            try
            {
                _jsonWriter.WriteStartArray();
                _jsonWriter.WriteStringValue(JcalConverter.ToJcalComponentName(calendar));
                _jsonWriter.WriteStartArray();

                // ref. RFC 7265§3.2: the calendar's VERSION is written as 2.0, in its place, or first if it has none.
                var version = new IcalContentLine(IcalPropertyNames.Version, IcalConformance.SupportedVersion);
                var hasVersion = false;
                foreach (var property in calendar.Properties)
                {
                    if (IcalComponent.IsNamed(property, IcalPropertyNames.Version))
                    {
                        hasVersion = true;
                        break;
                    }
                }

                if (!hasVersion)
                {
                    JcalConverter.WriteProperty(_jsonWriter, version);
                    propertyCount++;
                }

                var isVersionWritten = false;
                foreach (var property in calendar.Properties)
                {
                    if (IcalComponent.IsNamed(property, IcalPropertyNames.Version))
                    {
                        if (isVersionWritten) continue;

                        JcalConverter.WriteProperty(_jsonWriter, version);
                        isVersionWritten = true;
                    }
                    else
                    {
                        JcalConverter.WriteProperty(_jsonWriter, property);
                    }

                    propertyCount++;
                }

                _jsonWriter.WriteEndArray();
                WriteComponents(calendar, ref componentCount, ref propertyCount);
                _jsonWriter.WriteEndArray();
                _jsonWriter.Flush();
            }
            catch
            {
                buffer.Clear();
                throw;
            }

            if (_calendarsWritten == 0)
            {
                _isHeld = true;
            }
        }

        private void WriteComponent(IcalComponent component, ref int componentCount, ref int propertyCount)
        {
            // ref. RFC 7265§3.3: an array of the name in lower case, the properties and the sub-components, which is there even if empty.
            _jsonWriter.WriteStartArray();
            _jsonWriter.WriteStringValue(JcalConverter.ToJcalComponentName(component));
            _jsonWriter.WriteStartArray();
            foreach (var property in component.Properties)
            {
                JcalConverter.WriteProperty(_jsonWriter, property);
                propertyCount++;
            }

            _jsonWriter.WriteEndArray();
            WriteComponents(component, ref componentCount, ref propertyCount);
            _jsonWriter.WriteEndArray();
            componentCount++;
        }

        private void WriteComponents(IcalComponent component, ref int componentCount, ref int propertyCount)
        {
            _jsonWriter.WriteStartArray();
            foreach (var child in component.Components)
            {
                WriteComponent(child, ref componentCount, ref propertyCount);
            }

            _jsonWriter.WriteEndArray();
        }

        private void CountCalendar(int componentCount, int propertyCount)
        {
            if (_calendarsWritten == 1)
            {
                _isArrayStarted = true;
            }

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
        /// Marks the first calendar as written, once it has been written at the start of the array of calendars.
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
        private static IcalCalendar CheckCalendar(IcalCalendar? calendar)
#else
        private static IcalCalendar CheckCalendar(IcalCalendar calendar)
#endif
        {
            if (calendar is null)
                throw new ArgumentException("The calendars cannot contain null.", "calendars");

            return calendar;
        }

        private static void ValidateOptions(IcalOptions options)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null && options.Encoding.CodePage != Encoding.UTF8.CodePage)
                throw new ArgumentException("jCal is always UTF-8, so the encoding must be null or UTF-8.", nameof(options));
        }

        private static Stream ValidateStream(Stream stream, IcalOptions options)
        {
            if (stream is null)
                throw new ArgumentNullException("jcalStream");

            ValidateOptions(options);
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "jcalStream");

            return stream;
        }

        private static Stream OpenFile(string path, IcalOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("jcalFilePath");

            ValidateOptions(options);
            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous);
        }
    }
}
#endif
