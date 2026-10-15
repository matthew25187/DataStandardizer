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

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a reader of jCal data, the JSON form of iCalendar (RFC 7265), into the component object model: a sequence of
    /// <see cref="IcalCalendar"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is a JSON document in UTF-8, which may start with a byte order mark.  It is either the component array of one calendar,
    /// <c>["vcalendar", [...], [...]]</c>, or an array of them (RFC 7265 §3.2).  The calendars are read one at a time, so a document need not
    /// fit in memory.  Component, property and parameter names are converted to upper case, and each value to the text form of RFC 5545 (RFC
    /// 7265 §4), so the calendars are the same as those <see cref="IcalReader"/> reads from the same data in <c>.ics</c> form, and can be
    /// written in either form.  In particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>A value whose type identifier is not the property's default adds a <c>VALUE</c> parameter, after the other parameters
    /// (RFC 7265 §3.5.1).</description></item>
    /// <item><description>A value with the <c>unknown</c> type is kept as raw text, with no <c>VALUE</c> parameter (RFC 7265 §5).  A type
    /// identifier that is not one of RFC 5545's, such as <c>x-mytype</c>, is an extension value type: the value is kept as raw text, with a
    /// <c>VALUE</c> parameter naming the type.</description></item>
    /// <item><description>The values of a multi-valued property, such as CATEGORIES or EXDATE, are joined into a list (RFC 7265
    /// §3.4).</description></item>
    /// <item><description>A RECUR object keeps the order of its members as the order of the rule parts.</description></item>
    /// <item><description>A PERIOD value may be an array of two strings, as RFC 7265 §3.6.9 gives it, or one string with the parts joined by a
    /// solidus, as its Appendix B gives it.</description></item>
    /// <item><description>A string is accepted as the value of any type, so that a value that does not follow its type is kept.  A number is
    /// accepted only for INTEGER and FLOAT, <c>true</c> and <c>false</c> only for BOOLEAN, an array only for PERIOD, GEO and REQUEST-STATUS,
    /// and an object only for RECUR.</description></item>
    /// </list>
    /// <para>
    /// A document that is not valid JSON, or does not follow the structure of jCal, throws <see cref="IcalFileException"/>.  Its data holds the
    /// 0-based offset of the JSON token in bytes under <see cref="IcalFileException.DataItemName.BytePosition"/> and, where the token has been
    /// read, the JSONPath of the value under <see cref="IcalFileException.DataItemName.JsonPath"/>, such as <c>$[2][0][1][3]</c>.  For data that
    /// is not valid JSON, it also holds the line and the byte position on the line under <see cref="IcalFileException.DataItemName.LineNumber"/>
    /// and <see cref="IcalFileException.DataItemName.LinePosition"/>.
    /// </para>
    /// <para>
    /// Each calendar is checked once its component array has been read, as <see cref="IcalComponent.Validate"/> does, against the same rules and
    /// options as <see cref="IcalReader"/>.  The diagnostics have no line numbers, since a jCal document has no content lines; their component
    /// path gives the place.  The first problem that the options do not accept throws <see cref="IcalFileException"/>; reading can then continue
    /// with the next calendar.  A value that does not follow its type is kept as it is, so the check reports it, and
    /// <see cref="IcalOptions.AllowInvalidValues"/> lets it through.  Of the other options, only <see cref="IcalOptions.Encoding"/> applies, and
    /// it must be UTF-8 if it is set.
    /// </para>
    /// <para>
    /// The JSON is read with <see cref="Utf8JsonReader"/> over pooled blocks of bytes, so a read does no I/O of its own beyond filling the next
    /// block; the asynchronous members fill it asynchronously, and do no synchronous I/O.  Any other exception from a read, including a
    /// cancellation, leaves the reader unable to continue, and later reads throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while
    /// another is in progress throws <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// jCal needs System.Text.Json, so this type is not available on netstandard1.1.
    /// </para>
    /// </remarks>
    public sealed class JcalReader : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// The size of the first block of bytes; a block grows when a single JSON token does not fit it.
        /// </summary>
        private const int InitialBufferSize = 16384;

        private enum ReadResult
        {
            Calendar,
            NeedData,
            End
        }

        private readonly JcalDocumentBuilder _builder = new JcalDocumentBuilder();
        private readonly IcalOptions _options;
        private readonly bool _ownsStream;
        private readonly Stream _stream;

        private byte[] _buffer;

        // The absolute offset, in the data, of the byte at _start.
        private long _bufferPosition;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? _calendar;
#else
        private IcalCalendar _calendar;
#endif
        private int _end;
        private bool _isByteOrderMarkChecked;
        private bool _isEndOfData;
        private bool _isFaulted;
        private bool _isFinished;
        private int _start;
        private JsonReaderState _state;

        // These are changed with Interlocked, so that a read started on another thread while one is in progress is refused, and a Dispose from
        // another thread releases the reader once that read has finished.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a reader for jCal data in a stream.
        /// </summary>
        /// <param name="jcalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcalStream"/> does not support reading.</exception>
        public JcalReader(Stream jcalStream) : this(jcalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for jCal data in a stream.
        /// </summary>
        /// <param name="jcalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCal is always
        /// UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="jcalStream"/> does not support reading.
        /// -or-
        /// <see cref="IcalOptions.Encoding"/> is not UTF-8.</exception>
        public JcalReader(Stream jcalStream, IcalOptions options) : this(ValidateStream(jcalStream, options), false, options)
        {
        }

        /// <summary>
        /// Creates a reader for a jCal file.
        /// </summary>
        /// <param name="jcalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalFilePath"/> is <c>null</c>.</exception>
        public JcalReader(string jcalFilePath) : this(jcalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for a jCal file.
        /// </summary>
        /// <param name="jcalFilePath">The path of the file.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c> or UTF-8, since jCal is always
        /// UTF-8.</param>
        /// <exception cref="ArgumentNullException"><paramref name="jcalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is not UTF-8.</exception>
        public JcalReader(string jcalFilePath, IcalOptions options) : this(OpenFile(jcalFilePath, options), true, options)
        {
        }

        private JcalReader(Stream stream, bool ownsStream, IcalOptions options)
        {
            _stream = stream;
            _ownsStream = ownsStream;
            _options = options;
            _buffer = ArrayPool<byte>.Shared.Rent(InitialBufferSize);
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
        /// Gets the number of properties read so far, in all components and calendars.
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
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the reader is
        /// released once that read has finished with it.
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
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the reader is released once that read has finished with it.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default(ValueTask);
        }

        /// <summary>
        /// Reads the next calendar.
        /// </summary>
        /// <returns>The calendar; or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="IcalFileException">The data is not valid jCal: it is not valid JSON, a value is not allowed where it is or is not
        /// valid, the <c>VERSION</c> does not include 2.0, or the data holds no calendar.
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
                    ThrowIfDisposed();
                    var result = Process();
                    if (result != ReadResult.NeedData) return TakeCalendar(result);

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
        /// Reads the remaining calendars.
        /// </summary>
        /// <returns>The calendars, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="IcalFileException">During enumeration: the data is not valid jCal, or a calendar breaks a rule of RFC 5545 that the
        /// options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was
        /// canceled.</exception>
        public IEnumerable<IcalCalendar> ReadCalendars()
        {
            // Check now, so the exception comes from this call and not from the first MoveNext.
            ThrowIfDisposed();

            return ReadCalendarsIterator();
        }

        /// <summary>
        /// Reads the next calendar asynchronously.
        /// </summary>
        /// <remarks>
        /// The task completes synchronously when the buffer already holds the whole calendar.  Cancellation is checked before each block of the
        /// data is read.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot
        /// continue.</param>
        /// <returns>A task whose result is the calendar, or <c>null</c> if the end of the data has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.</exception>
        /// <exception cref="IcalFileException">By the task: the data is not valid jCal, or the calendar breaks a rule of RFC 5545 that the
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
        /// <exception cref="IcalFileException">During enumeration: the data is not valid jCal, or a calendar breaks a rule of RFC 5545 that the
        /// options do not accept.</exception>
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

        /// <summary>
        /// Continues <see cref="ReadCalendarAsync"/>, and ends the read.  Completes synchronously, without allocating a task, while the buffer
        /// holds the data.
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
                    ThrowIfDisposed();
                    var result = Process();
                    if (result != ReadResult.NeedData) return TakeCalendar(result);

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
            finally
            {
                EndRead();
            }
        }

        /// <summary>
        /// Reads the tokens in the buffer, until a calendar ends or more data is needed.
        /// </summary>
        /// <exception cref="IcalFileException">The data is not valid jCal, or the calendar that ended breaks a rule of RFC 5545 that the options
        /// do not accept.</exception>
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
                // Where the calendar being built ends is now unknown.
                _isFaulted = true;
                throw;
            }

            if (result != ReadResult.Calendar) return result;

            // The calendar has been read in full, so a problem with it does not stop the reader.
            var diagnostics = new List<IcalDiagnostic>();
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            IcalConformance.Validate(_calendar!, diagnostics);
#else
            IcalConformance.Validate(_calendar, diagnostics);
#endif
            foreach (var diagnostic in diagnostics)
            {
                if (!IcalConformance.IsAccepted(diagnostic.Kind, _options))
                {
                    _calendar = null;
                    throw IcalConformance.BuildException(diagnostic);
                }
            }

            return ReadResult.Calendar;
        }

        private ReadResult ReadTokens()
        {
            // ref. RFC 8259§8.1: a parser MAY ignore a byte order mark.  It is skipped before the JSON reader sees it.
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
                    var calendar = _builder.Add(reader.TokenType, GetText(ref reader), _bufferPosition + reader.TokenStartIndex);
                    if (calendar != null)
                    {
                        Consume(ref reader);
                        _calendar = calendar;
                        return ReadResult.Calendar;
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
            _builder.Complete();
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
                        var fileException = new IcalFileException("The data holds a JSON string that is not valid Unicode text.", exception);
                        fileException.Data[IcalFileException.DataItemName.BytePosition] = _bufferPosition + reader.TokenStartIndex;
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
        private IcalCalendar? TakeCalendar(ReadResult result)
#else
        private IcalCalendar TakeCalendar(ReadResult result)
#endif
        {
            var calendar = result == ReadResult.Calendar ? _calendar : null;
            _calendar = null;
            return calendar;
        }

        /// <summary>
        /// Builds the exception for data that is not valid JSON.
        /// </summary>
        private static IcalFileException BuildJsonException(JsonException exception, long bytePosition)
        {
            var fileException = new IcalFileException("The data is not valid JSON: " + exception.Message, exception);
            fileException.Data[IcalFileException.DataItemName.BytePosition] = bytePosition;
            if (exception.LineNumber.HasValue)
            {
                fileException.Data[IcalFileException.DataItemName.LineNumber] = exception.LineNumber.Value + 1;
            }

            if (exception.BytePositionInLine.HasValue)
            {
                fileException.Data[IcalFileException.DataItemName.LinePosition] = exception.BytePositionInLine.Value + 1;
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
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "jcalStream");

            return stream;
        }

        private static Stream OpenFile(string path, IcalOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("jcalFilePath");

            ValidateOptions(options);
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER)
        /// <summary>
        /// The calendars of <see cref="ReadCalendarsAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncCalendarEnumerable : IAsyncEnumerable<IcalCalendar>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly JcalReader _reader;

            internal AsyncCalendarEnumerable(JcalReader reader, CancellationToken cancellationToken)
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
                private readonly JcalReader _reader;

                internal Enumerator(JcalReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
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
#endif
