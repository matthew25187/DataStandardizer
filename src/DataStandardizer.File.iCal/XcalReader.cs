using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Represents a reader of xCal data, the XML form of iCalendar (RFC 6321), into the component object model: a sequence of
    /// <see cref="IcalCalendar"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is an XML document whose root is the <c>icalendar</c> element in the namespace <c>urn:ietf:params:xml:ns:icalendar-2.0</c>.
    /// Each <c>vcalendar</c> element in it is read as a calendar, one at a time, so a document need not fit in memory.  Component, property and
    /// parameter names are converted to upper case, and each value to the text form of RFC 5545 (RFC 6321 §4), so the calendars are the same as
    /// those <see cref="IcalReader"/> reads from the same data in <c>.ics</c> form, and can be written in either form.  In particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>A value element whose type is not the property's default adds a <c>VALUE</c> parameter, after the other parameters
    /// (RFC 6321 §3.5.1).</description></item>
    /// <item><description>An <c>unknown</c> value element is kept as raw text, with no <c>VALUE</c> parameter (RFC 6321 §5).</description></item>
    /// <item><description>The values of a multi-valued property, such as CATEGORIES or EXDATE, are joined into a list (RFC 6321
    /// §3.4.1.1).</description></item>
    /// <item><description>A RECUR value keeps the order of its rule parts.</description></item>
    /// <item><description>Elements in another namespace are extensions, and are skipped with their content.  They are not converted to the
    /// <c>XML</c> property that RFC 6321 §4.2 describes.</description></item>
    /// </list>
    /// <para>
    /// A document that is not well-formed XML, or does not follow the structure of xCal, throws <see cref="IcalFileException"/>.  Its data
    /// holds the line of the XML document under <see cref="IcalFileException.DataItemName.LineNumber"/> and the position on that line under
    /// <see cref="IcalFileException.DataItemName.LinePosition"/>.  A document type definition (DTD) is not allowed.
    /// </para>
    /// <para>
    /// Each calendar is checked once its <c>vcalendar</c> element has been read, as <see cref="IcalComponent.Validate"/> does, against the
    /// same rules and options as <see cref="IcalReader"/>; the line numbers in its diagnostics are those of the XML document.  The first problem
    /// that the options do not accept throws <see cref="IcalFileException"/>; reading can then continue with the next calendar.  A value that is
    /// not valid in its value element is kept as it is, so the check reports it, and <see cref="IcalOptions.AllowInvalidValues"/> lets it
    /// through.  Of the other options, only <see cref="IcalOptions.Encoding"/> applies.
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
    public sealed class XcalReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private readonly XcalDocumentBuilder _builder = new XcalDocumentBuilder();
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? _calendar;
        private readonly IXmlLineInfo? _lineInfo;
#else
        private IcalCalendar _calendar;
        private readonly IXmlLineInfo _lineInfo;
#endif
        private bool _isFaulted;
        private bool _isFinished;
        private readonly bool _isInternalReader;
        private bool _isStarted;
        private readonly IcalOptions _options;
        private readonly XmlReader _reader;

        // These are changed with Interlocked, so that a read started on another thread while one is in progress is refused, and a Dispose from
        // another thread releases the reader once that read has finished.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a reader for xCal data in a stream, in the encoding the XML document declares, or UTF-8 if it declares none.
        /// </summary>
        /// <param name="xcalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcalStream"/> does not support reading.</exception>
        public XcalReader(Stream xcalStream) : this(xcalStream, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for xCal data in a stream.
        /// </summary>
        /// <param name="xcalStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  When <see cref="IcalOptions.Encoding"/> is set, it is used in place of the encoding the XML
        /// document declares, unless the stream starts with a byte order mark.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcalStream"/> does not support reading.</exception>
        public XcalReader(Stream xcalStream, IcalOptions options) : this(CreateStreamReader(xcalStream, options, false), true, options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for an xCal file, in the encoding the XML document declares, or UTF-8 if it declares none.
        /// </summary>
        /// <param name="xcalFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalFilePath"/> is <c>null</c>.</exception>
        public XcalReader(string xcalFilePath) : this(xcalFilePath, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for an xCal file.
        /// </summary>
        /// <param name="xcalFilePath">The path of the file.</param>
        /// <param name="options">The options.  When <see cref="IcalOptions.Encoding"/> is set, it is used in place of the encoding the XML
        /// document declares, unless the file starts with a byte order mark.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcalFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        public XcalReader(string xcalFilePath, IcalOptions options) : this(CreateFileReader(xcalFilePath, options), true, options)
        {
        }
#endif

        /// <summary>
        /// Creates a reader for xCal data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public XcalReader(TextReader reader) : this(reader, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for xCal data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the text reader has already decoded
        /// the data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.</exception>
        public XcalReader(TextReader reader, IcalOptions options) : this(CreateTextReader(reader, options), true, options)
        {
        }

        /// <summary>
        /// Creates a reader for xCal data from an XML reader, which may be part of a larger document.
        /// </summary>
        /// <remarks>
        /// Reading starts at the XML reader's current node, or at its first node if it has not started, and stops at the end of the
        /// <c>icalendar</c> element, leaving the XML reader on that element's end tag.  The XML reader's settings apply, such as whether it allows
        /// a DTD.
        /// </remarks>
        /// <param name="reader">The XML reader.  It is left open when the reader is disposed.  For the asynchronous members, its
        /// <see cref="XmlReaderSettings.Async"/> setting must be <c>true</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public XcalReader(XmlReader reader) : this(reader, new IcalOptions())
        {
        }

        /// <summary>
        /// Creates a reader for xCal data from an XML reader, which may be part of a larger document.
        /// </summary>
        /// <remarks>
        /// Reading starts at the XML reader's current node, or at its first node if it has not started, and stops at the end of the
        /// <c>icalendar</c> element, leaving the XML reader on that element's end tag.  The XML reader's settings apply, such as whether it allows
        /// a DTD.
        /// </remarks>
        /// <param name="reader">The XML reader.  It is left open when the reader is disposed.  For the asynchronous members, its
        /// <see cref="XmlReaderSettings.Async"/> setting must be <c>true</c>.</param>
        /// <param name="options">The options.  <see cref="IcalOptions.Encoding"/> must be <c>null</c>, since the XML reader has already decoded
        /// the data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="IcalOptions.Encoding"/> is set.</exception>
        public XcalReader(XmlReader reader, IcalOptions options) : this(ValidateXmlReader(reader, options), false, options)
        {
        }

        private XcalReader(XmlReader reader, bool isInternalReader, IcalOptions options)
        {
            _reader = reader;
            _isInternalReader = isInternalReader;
            _options = options;
            _lineInfo = reader as IXmlLineInfo;
            if (_lineInfo != null && !_lineInfo.HasLineInfo())
            {
                _lineInfo = null;
            }
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
        /// Releases the XML reader the reader created itself, and the stream or file it opened.  A caller-supplied stream, text reader or XML
        /// reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the XML reader is
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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Releases the XML reader the reader created itself, and the stream or file it opened.  A caller-supplied stream, text reader or XML
        /// reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with
        /// <see cref="ObjectDisposedException"/>, and the XML reader is released once that read has finished with it.
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
        /// <returns>The calendar; or <c>null</c> if the end of the <c>icalendar</c> element has been reached.</returns>
        /// <exception cref="IcalFileException">The data is not valid xCal: it is not well-formed XML, an element is not allowed where it is or
        /// is not valid, the <c>VERSION</c> does not include 2.0, or the data holds no calendar.
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
                    bool isNode;
                    try
                    {
                        ThrowIfDisposed();
                        isNode = MoveToNextNode();
                    }
                    catch (XmlException exception)
                    {
                        throw Fault(exception);
                    }
                    catch
                    {
                        _isFaulted = true;
                        throw;
                    }

                    var value = isNode && IsTextNode(_reader.NodeType) ? _reader.Value : null;
                    if (AddNode(isNode, value)) return TakeCalendar();
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
        /// <exception cref="IcalFileException">During enumeration: the data is not valid xCal, or a calendar breaks a rule of RFC 5545 that the
        /// options do not accept.</exception>
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
        /// The task completes synchronously when the XML reader already holds the whole calendar in its buffer.  Cancellation is checked before
        /// each node of the XML document is read.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot
        /// continue.</param>
        /// <returns>A task whose result is the calendar, or <c>null</c> if the end of the <c>icalendar</c> element has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed
        /// while the read is in progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.
        /// -or-
        /// The reader was created from an XML reader whose <see cref="XmlReaderSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="IcalFileException">By the task: the data is not valid xCal, or the calendar breaks a rule of RFC 5545 that the
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
            if (_reader.Settings != null && !_reader.Settings.Async)
                throw new InvalidOperationException("The XML reader does not allow asynchronous reads.  Create it with XmlReaderSettings.Async set to true.");

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
        /// <exception cref="IcalFileException">During enumeration: the data is not valid xCal, or a calendar breaks a rule of RFC 5545 that the
        /// options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, an earlier read failed or was canceled,
        /// or the XML reader does not allow asynchronous reads.</exception>
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
        /// Continues <see cref="ReadCalendarAsync"/>, and ends the read.  Completes synchronously, without allocating a task, while the XML
        /// reader completes synchronously.
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
                    bool isNode;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? value = null;
#else
                    string value = null;
#endif
                    try
                    {
                        ThrowIfDisposed();
                        cancellationToken.ThrowIfCancellationRequested();
                        isNode = await MoveToNextNodeAsync().ConfigureAwait(false);
                        if (isNode && IsTextNode(_reader.NodeType))
                        {
                            // The value of a text node may not have been read yet, and the Value property would read it synchronously.
                            value = await _reader.GetValueAsync().ConfigureAwait(false);
                        }
                    }
                    catch (XmlException exception)
                    {
                        throw Fault(exception);
                    }
                    catch
                    {
                        _isFaulted = true;
                        throw;
                    }

                    if (AddNode(isNode, value)) return TakeCalendar();
                }
            }
            finally
            {
                EndRead();
            }
        }

        /// <summary>
        /// Moves to the next node to add, asynchronously.
        /// </summary>
        /// <returns><c>true</c> if the reader is on a node; <c>false</c> at the end of the data, or once the xCal root element has ended.</returns>
        private async ValueTask<bool> MoveToNextNodeAsync()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                if (_reader.ReadState == ReadState.Interactive) return true;
            }

            if (_builder.IsRootEnded)
            {
                // A document the reader opened itself is read to its end, so that anything after the root element is checked.
                if (_isInternalReader)
                {
                    while (await _reader.ReadAsync().ConfigureAwait(false))
                    {
                    }
                }

                return false;
            }

            return await _reader.ReadAsync().ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Moves to the next node to add.
        /// </summary>
        /// <returns><c>true</c> if the reader is on a node; <c>false</c> at the end of the data, or once the xCal root element has ended.</returns>
        private bool MoveToNextNode()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                if (_reader.ReadState == ReadState.Interactive) return true;
            }

            if (_builder.IsRootEnded)
            {
                // A document the reader opened itself is read to its end, so that anything after the root element is checked.
                if (_isInternalReader)
                {
                    while (_reader.Read())
                    {
                    }
                }

                return false;
            }

            return _reader.Read();
        }

        /// <summary>
        /// Adds the reader's current node, or the end of the data, to the calendar being built.
        /// </summary>
        /// <param name="isNode"><c>true</c> if the reader is on a node; <c>false</c> at the end of the data.</param>
        /// <param name="value">The value of a text node; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the read has its result: a calendar, ended by this node and checked, or <c>null</c> at the end of the data;
        /// see <see cref="TakeCalendar"/>.</returns>
        /// <exception cref="IcalFileException">The node is not valid where it is, or the calendar it ends breaks a rule of RFC 5545 that the
        /// options do not accept.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private bool AddNode(bool isNode, string? value)
#else
        private bool AddNode(bool isNode, string value)
#endif
        {
            try
            {
                if (!isNode)
                {
                    if (!_isFinished)
                    {
                        _builder.Complete();
                        _isFinished = true;
                    }

                    _calendar = null;
                    return true;
                }

                var lineNumber = _lineInfo?.LineNumber ?? 0;
                var linePosition = _lineInfo?.LinePosition ?? 0;
                switch (_reader.NodeType)
                {
                    case XmlNodeType.Element:
                        _calendar = _builder.StartElement(_reader.LocalName, _reader.NamespaceURI, _reader.IsEmptyElement, lineNumber, linePosition);
                        break;
                    case XmlNodeType.EndElement:
                        _calendar = _builder.EndElement(lineNumber, linePosition);
                        break;
                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                        _builder.Text(value!, lineNumber, linePosition);
#else
                        _builder.Text(value, lineNumber, linePosition);
#endif
                        return false;
                    default:
                        return false;
                }
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
        /// Marks the reader as unable to continue, and builds the exception for XML that is not well-formed.
        /// </summary>
        private IcalFileException Fault(XmlException exception)
        {
            _isFaulted = true;
            var fileException = new IcalFileException("The data is not well-formed XML: " + exception.Message, exception);
            XcalConverter.AddLocation(fileException, exception.LineNumber, exception.LinePosition);
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
            if (_isInternalReader)
            {
                _reader.Dispose();
            }
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

        private static bool IsTextNode(XmlNodeType nodeType)
        {
            return nodeType == XmlNodeType.Text || nodeType == XmlNodeType.CDATA || nodeType == XmlNodeType.Whitespace ||
                   nodeType == XmlNodeType.SignificantWhitespace;
        }

        private static XmlReaderSettings CreateSettings(bool closeInput)
        {
            var settings = new XmlReaderSettings
            {
                CloseInput = closeInput,
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            settings.Async = true;
            settings.XmlResolver = null;
#endif
            return settings;
        }

        private static XmlReader CreateStreamReader(Stream stream, IcalOptions options, bool ownsStream)
        {
            if (stream is null)
                throw new ArgumentNullException("xcalStream");
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "xcalStream");

            if (options.Encoding is null) return XmlReader.Create(stream, CreateSettings(ownsStream));

            // The text reader decodes the data, so the encoding the document declares is not used.
            var textReader = new StreamReader(stream, options.Encoding, true, 4096, !ownsStream);
            return XmlReader.Create(textReader, CreateSettings(true));
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static XmlReader CreateFileReader(string path, IcalOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("xcalFilePath");
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            var fileStream = IcalFileStreams.OpenRead(path, 4096);
            try
            {
                return CreateStreamReader(fileStream, options, true);
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
        }
#endif

        private static XmlReader CreateTextReader(TextReader reader, IcalOptions options)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing reader.  Specify the encoding when creating the reader.", nameof(options));

            return XmlReader.Create(reader, CreateSettings(false));
        }

        private static XmlReader ValidateXmlReader(XmlReader reader, IcalOptions options)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing reader.  Specify the encoding when creating the reader.", nameof(options));

            return reader;
        }

#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER) && NETSTANDARD2_0_OR_GREATER
        /// <summary>
        /// The calendars of <see cref="ReadCalendarsAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncCalendarEnumerable : IAsyncEnumerable<IcalCalendar>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly XcalReader _reader;

            internal AsyncCalendarEnumerable(XcalReader reader, CancellationToken cancellationToken)
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
                private readonly XcalReader _reader;

                internal Enumerator(XcalReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
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
