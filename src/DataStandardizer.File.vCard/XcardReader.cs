using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Xml;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a reader of xCard data, the XML form of vCard (RFC 6351), into the card object model: a sequence of <see cref="VcardCard"/> objects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The data is an XML document whose root is the <c>vcards</c> element in the namespace <c>urn:ietf:params:xml:ns:vcard-4.0</c>, which RFC 6351 §5 says MUST be present even
    /// for a single card.  Each <c>vcard</c> element in it is read as a card of vCard 4.0, one at a time, so a document need not fit in memory.  The namespace gives the version, so a
    /// card has no <c>VERSION</c> element, and is given <c>VERSION:4.0</c> (§5.1).  Property and parameter names are converted to upper case, a <c>group</c> element gives its
    /// properties the group its <c>name</c> attribute holds, as it is, and each value is converted to the text form of RFC 6350 (§6), so the cards are the same as those
    /// <see cref="VcardReader"/> reads from the same data in <c>.vcf</c> form, and can be written in either form.  In particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>A value element whose type is not the property's default adds a <c>VALUE</c> parameter, after the other parameters.  A <c>date</c>, <c>date-time</c> or
    /// <c>time</c> element of a property whose type is DATE-AND-OR-TIME, such as <c>BDAY</c>, adds none, and a time alone has a <c>T</c> put before it.</description></item>
    /// <item><description>An <c>unknown</c> value element is kept as it is, with no <c>VALUE</c> parameter (RFC 6351 §6).</description></item>
    /// <item><description>The value elements of a multi-valued property, such as <c>CATEGORIES</c>, are joined into a list, and the elements of a structured value, such as the
    /// <c>surname</c> and <c>given</c> elements of <c>N</c>, into its components.  The text elements of <c>ORG</c> are its components.  The components that RFC 9554 adds to
    /// <c>N</c> and <c>ADR</c>, which RFC 9554 gives no xCard form, are read from the elements <c>surname2</c> and <c>generation</c>, and <c>room</c>, <c>apartment</c>,
    /// <c>floor</c>, <c>streetnumber</c>, <c>streetname</c>, <c>building</c>, <c>block</c>, <c>subdistrict</c>, <c>district</c>, <c>landmark</c> and
    /// <c>direction</c>.</description></item>
    /// <item><description>A child of a <c>vcard</c> or <c>group</c> element in another namespace is wrapped as it is in an <c>XML</c> property (RFC 6351 §6), with its text and
    /// attributes, though not the spelling of its markup, such as the quotes around an attribute value.  Any other element or attribute whose name isn't recognised is ignored
    /// (§5.1), as are comments and processing instructions.</description></item>
    /// <item><description>A value that isn't in the form xCard gives its type, such as a BOOLEAN of <c>TRUE</c>, a DATE of a year alone, or a structured value that lacks the element
    /// of a component, is kept as it is and reported as <see cref="VcardDiagnosticKind.InvalidValue"/>, even where its text is a valid value of vCard.  So is a parameter value in
    /// an element other than the one RFC 6351 gives the parameter, or <c>unknown</c>.</description></item>
    /// </list>
    /// <para>
    /// A document that is not well-formed XML, or does not follow the structure of xCard, throws <see cref="VcardFileException"/>.  Its data holds the line of the XML document
    /// under <see cref="VcardFileException.DataItemName.LineNumber"/> and the position on that line under <see cref="VcardFileException.DataItemName.LinePosition"/>.  The
    /// structure includes these rules: names are in lower case (RFC 6351 §5.1); there are no <c>begin</c>, <c>end</c> or <c>version</c> properties, nor a <c>value</c> parameter,
    /// since the elements take their place; a property's <c>parameters</c> element comes first; a group has a name and holds no group; and the elements of a structured value are
    /// in the order of its components.  A document type definition (DTD) is not allowed, and throws even with an XML reader that allows one.
    /// </para>
    /// <para>
    /// Each card is checked once its <c>vcard</c> element has been read, as <see cref="VcardCard.Validate()"/> does, against the same rules and options as <see cref="VcardReader"/>.
    /// The diagnostics have no line numbers, since an xCard document has no content lines; their card index and property give the place.  The first problem that the options do not
    /// accept throws <see cref="VcardFileException"/>; reading can then continue with the next card.  A value that does not follow its type is kept as it is, so the check reports it,
    /// and <see cref="VcardOptions.AllowInvalidValues"/> lets it through.  Of the other options, only <see cref="VcardOptions.Encoding"/> applies.
    /// </para>
    /// <para>
    /// Any other exception from a read, including a cancellation, leaves the reader unable to continue, and later reads throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Only one read may be in progress at a time.  Synchronous and asynchronous reads may follow one another, but a read started while another is in progress throws
    /// <see cref="InvalidOperationException"/>.  The asynchronous members, and the constructors that take a file path, are not available on netstandard1.1.
    /// </para>
    /// </remarks>
    public sealed class XcardReader : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        private readonly XcardDocumentBuilder _builder;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? _card;
        private readonly Stream? _fileStream;
        private readonly IXmlLineInfo? _lineInfo;
#else
        private VcardCard _card;
#if NETSTANDARD2_0_OR_GREATER
        private readonly Stream _fileStream;
#endif
        private readonly IXmlLineInfo _lineInfo;
#endif
        private bool _isFaulted;
        private bool _isFinished;
        private readonly bool _isInternalReader;

        private bool _isStarted;
        private readonly VcardOptions _options;
        private readonly XmlReader _reader;

        // These are changed with Interlocked, so that a read started on another thread while one is in progress is refused, and a Dispose from another thread releases the reader
        // once that read has finished.
        private int _isDisposed;
        private int _isReadInProgress;
        private int _isReleased;

        /// <summary>
        /// What <see cref="AddNode"/> found.
        /// </summary>
        private enum NodeResult
        {
            /// <summary>
            /// Carry on to the next node.
            /// </summary>
            Continue,

            /// <summary>
            /// The read has its result; see <see cref="TakeCard"/>.
            /// </summary>
            Done,

            /// <summary>
            /// Read the markup of the element the reader is on, and add it with <see cref="AddForeignProperty"/>.
            /// </summary>
            ReadForeignProperty
        }

        /// <summary>
        /// Creates a reader for xCard data in a stream, in the encoding the XML document declares, or UTF-8 if it declares none.
        /// </summary>
        /// <param name="xcardStream">The stream.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcardStream"/> does not support reading.</exception>
        public XcardReader(Stream xcardStream) : this(xcardStream, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a reader for xCard data in a stream.
        /// </summary>
        /// <param name="xcardStream">The stream.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  When <see cref="VcardOptions.Encoding"/> is set, it is used in place of the encoding the XML document declares, unless the stream starts
        /// with a byte order mark.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcardStream"/> does not support reading.</exception>
        public XcardReader(Stream xcardStream, VcardOptions options) : this(CreateStreamReader(xcardStream, options, false), true, options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a reader for an xCard file, in the encoding the XML document declares, or UTF-8 if it declares none.
        /// </summary>
        /// <param name="xcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardFilePath"/> is <c>null</c>.</exception>
        public XcardReader(string xcardFilePath) : this(xcardFilePath, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a reader for an xCard file.
        /// </summary>
        /// <param name="xcardFilePath">The path of the file.</param>
        /// <param name="options">The options.  When <see cref="VcardOptions.Encoding"/> is set, it is used in place of the encoding the XML document declares, unless the file starts
        /// with a byte order mark.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        public XcardReader(string xcardFilePath, VcardOptions options) : this(CreateFileReader(xcardFilePath, options, out var fileStream), true, options)
        {
            // The XML reader takes ownership of the file only when it is first read, so the file is closed here too.
            _fileStream = fileStream;
        }
#endif

        /// <summary>
        /// Creates a reader for xCard data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public XcardReader(TextReader reader) : this(reader, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a reader for xCard data from a text reader.
        /// </summary>
        /// <param name="reader">The text reader.  It is left open when the reader is disposed.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c>, since the text reader has already decoded the data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is set.</exception>
        public XcardReader(TextReader reader, VcardOptions options) : this(CreateTextReader(reader, options), true, options)
        {
        }

        /// <summary>
        /// Creates a reader for xCard data from an XML reader, which may be part of a larger document.
        /// </summary>
        /// <remarks>
        /// Reading starts at the XML reader's current node, or at its first node if it has not started, and stops at the end of the <c>vcards</c> element, leaving the XML reader on
        /// that element's end tag.  The XML reader's settings apply, except that a DTD the XML reader reports throws.  A DTD that the XML reader ignores, with
        /// <see cref="DtdProcessing.Ignore"/>, is not reported, so give it <see cref="DtdProcessing.Prohibit"/>, the default of <see cref="XmlReaderSettings"/>.
        /// </remarks>
        /// <param name="reader">The XML reader.  It is left open when the reader is disposed.  For the asynchronous members, its <see cref="XmlReaderSettings.Async"/> setting must be
        /// <c>true</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> is <c>null</c>.</exception>
        public XcardReader(XmlReader reader) : this(reader, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a reader for xCard data from an XML reader, which may be part of a larger document.
        /// </summary>
        /// <remarks>
        /// Reading starts at the XML reader's current node, or at its first node if it has not started, and stops at the end of the <c>vcards</c> element, leaving the XML reader on
        /// that element's end tag.  The XML reader's settings apply, except that a DTD the XML reader reports throws.  A DTD that the XML reader ignores, with
        /// <see cref="DtdProcessing.Ignore"/>, is not reported, so give it <see cref="DtdProcessing.Prohibit"/>, the default of <see cref="XmlReaderSettings"/>.
        /// </remarks>
        /// <param name="reader">The XML reader.  It is left open when the reader is disposed.  For the asynchronous members, its <see cref="XmlReaderSettings.Async"/> setting must be
        /// <c>true</c>.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c>, since the XML reader has already decoded the data.</param>
        /// <exception cref="ArgumentNullException"><paramref name="reader"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is set.</exception>
        public XcardReader(XmlReader reader, VcardOptions options) : this(ValidateXmlReader(reader, options), false, options)
        {
        }

        private XcardReader(XmlReader reader, bool isInternalReader, VcardOptions options)
        {
            _reader = reader;
            _isInternalReader = isInternalReader;
            _options = options;
            _builder = new XcardDocumentBuilder(options);
            _lineInfo = reader as IXmlLineInfo;
            if (_lineInfo != null && !_lineInfo.HasLineInfo())
            {
                _lineInfo = null;
            }
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
        /// Gets the number of properties read so far, in all cards, not counting the <c>VERSION</c> that each card is given.
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
        /// Releases the XML reader the reader created itself, and the stream or file it opened.  A caller-supplied stream, text reader or XML reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the XML reader is released once that read has
        /// finished with it.
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
        /// Releases the XML reader the reader created itself, and the stream or file it opened.  A caller-supplied stream, text reader or XML reader is left open.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If a read is in progress, it completes with <see cref="ObjectDisposedException"/>, and the
        /// XML reader is released once that read has finished with it.
        /// </remarks>
        /// <returns>A task that has already completed.</returns>
        public ValueTask DisposeAsync()
        {
            Dispose();
            return default(ValueTask);
        }
#endif

        /// <summary>
        /// Reads the next card.
        /// </summary>
        /// <returns>The card; or <c>null</c> if the end of the <c>vcards</c> element has been reached.</returns>
        /// <exception cref="VcardFileException">The data is not valid xCard: it is not well-formed XML, it has a DTD, an element is not allowed where it is or is not valid, or the
        /// data holds no card.
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
                    var result = AddNode(isNode, value);
                    if (result == NodeResult.ReadForeignProperty)
                    {
                        long lineNumber, linePosition;
                        GetPosition(out lineNumber, out linePosition);
                        string xml;
                        try
                        {
                            xml = ReadForeignElement();
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

                        AddForeignProperty(xml, lineNumber, linePosition);
                    }
                    else if (result == NodeResult.Done)
                    {
                        return TakeCard();
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
        /// <exception cref="VcardFileException">During enumeration: the data is not valid xCard, or a card breaks a rule of RFC 6350 that the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, or an earlier read failed or was canceled.</exception>
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
        /// The task completes synchronously when the XML reader already holds the whole card in its buffer.  Cancellation is checked before each node of the XML document is read.
        /// </remarks>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the read is canceled, the reader cannot continue.</param>
        /// <returns>A task whose result is the card, or <c>null</c> if the end of the <c>vcards</c> element has been reached.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.  Thrown by this call, or by the task if the reader is disposed while the read is in
        /// progress.</exception>
        /// <exception cref="InvalidOperationException">Another read is in progress.
        /// -or-
        /// An earlier read failed or was canceled, so the reader cannot continue.
        /// -or-
        /// The reader was created from an XML reader whose <see cref="XmlReaderSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="VcardFileException">By the task: the data is not valid xCard, or the card breaks a rule of RFC 6350 that the options do not accept.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ValueTask<VcardCard?> ReadCardAsync(CancellationToken cancellationToken = default)
#else
        public ValueTask<VcardCard> ReadCardAsync(CancellationToken cancellationToken = default)
#endif
        {
            // Errors in how the reader is used are thrown now; errors in the data, and cancellation, go to the task.
            ThrowIfDisposed();
            if (_reader.Settings != null && !_reader.Settings.Async)
                throw new InvalidOperationException("The XML reader does not allow asynchronous reads.  Create it with XmlReaderSettings.Async set to true.");

            BeginRead();
            return ReadCardCoreAsync(cancellationToken);
        }

        /// <summary>
        /// Reads the remaining cards asynchronously.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is combined with any token passed to
        /// <see cref="IAsyncEnumerable{T}.GetAsyncEnumerator"/>.  If a read is canceled, the reader cannot continue.</param>
        /// <returns>The cards, read as the sequence is enumerated.</returns>
        /// <exception cref="ObjectDisposedException">The reader has been disposed, either before this call or during enumeration.</exception>
        /// <exception cref="VcardFileException">During enumeration: the data is not valid xCard, or a card breaks a rule of RFC 6350 that the options do not accept.</exception>
        /// <exception cref="InvalidOperationException">During enumeration: another read is in progress, an earlier read failed or was canceled, or the XML reader does not allow
        /// asynchronous reads.</exception>
        /// <exception cref="OperationCanceledException">During enumeration: the read was canceled.</exception>
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
        /// Continues <see cref="ReadCardAsync"/>, and ends the read.  Completes synchronously, without allocating a task, while the XML reader completes synchronously.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private async ValueTask<VcardCard?> ReadCardCoreAsync(CancellationToken cancellationToken)
#else
        private async ValueTask<VcardCard> ReadCardCoreAsync(CancellationToken cancellationToken)
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

                    var result = AddNode(isNode, value);
                    if (result == NodeResult.ReadForeignProperty)
                    {
                        long lineNumber, linePosition;
                        GetPosition(out lineNumber, out linePosition);
                        string xml;
                        try
                        {
                            xml = await ReadForeignElementAsync(cancellationToken).ConfigureAwait(false);
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

                        AddForeignProperty(xml, lineNumber, linePosition);
                    }
                    else if (result == NodeResult.Done)
                    {
                        return TakeCard();
                    }
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
        /// <returns><c>true</c> if the reader is on a node; <c>false</c> at the end of the data, or once the xCard root element has ended.</returns>
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
                        CheckNotDocumentType();
                    }
                }

                return false;
            }

            return await _reader.ReadAsync().ConfigureAwait(false);
        }
#endif

        /// <summary>
        /// Copies the element the reader is on, a property in another namespace, as markup, leaving the reader on its end.
        /// </summary>
        private string ReadForeignElement()
        {
            var output = new StringWriter(CultureInfo.InvariantCulture);
            using (var writer = XmlWriter.Create(output, ForeignElementSettings))
            {
                var depth = _reader.Depth;
                while (true)
                {
                    CopyNode(writer, IsTextNode(_reader.NodeType) ? _reader.Value : null);
                    if (IsEndOfElement(depth) || !_reader.Read()) break;
                }
            }

            return output.ToString();
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Copies the element the reader is on, a property in another namespace, as markup, leaving the reader on its end, asynchronously.  Cancellation is checked before each
        /// node is read.
        /// </summary>
        private async ValueTask<string> ReadForeignElementAsync(CancellationToken cancellationToken)
        {
            var output = new StringWriter(CultureInfo.InvariantCulture);
            using (var writer = XmlWriter.Create(output, ForeignElementSettings))
            {
                var depth = _reader.Depth;
                while (true)
                {
                    // The value of a text node may not have been read yet, and the Value property would read it synchronously.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? value = null;
#else
                    string value = null;
#endif
                    if (IsTextNode(_reader.NodeType))
                    {
                        value = await _reader.GetValueAsync().ConfigureAwait(false);
                    }

                    CopyNode(writer, value);
                    if (IsEndOfElement(depth)) break;

                    cancellationToken.ThrowIfCancellationRequested();
                    if (!await _reader.ReadAsync().ConfigureAwait(false)) break;
                }
            }

            return output.ToString();
        }
#endif

        /// <summary>
        /// Gets whether the reader is on the end of the element that started at a depth: its end tag, or the element itself if it is empty.
        /// </summary>
        private bool IsEndOfElement(int depth)
        {
            return _reader.Depth == depth && (_reader.NodeType == XmlNodeType.EndElement || (_reader.NodeType == XmlNodeType.Element && _reader.IsEmptyElement));
        }

        /// <summary>
        /// Writes the node the reader is on: an element with its attributes, the end of one, or text.  Comments and processing instructions are left out, as the reader leaves
        /// them out everywhere else.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private void CopyNode(XmlWriter writer, string? value)
#else
        private void CopyNode(XmlWriter writer, string value)
#endif
        {
            switch (_reader.NodeType)
            {
                case XmlNodeType.Element:
                    // An attribute's value has been read with its element.  The writer declares any namespace the element or an attribute uses that an ancestor declared.
                    writer.WriteStartElement(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI);
                    var isEmpty = _reader.IsEmptyElement;
                    while (_reader.MoveToNextAttribute())
                    {
                        writer.WriteAttributeString(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI, _reader.Value);
                    }

                    _reader.MoveToElement();
                    if (isEmpty)
                    {
                        writer.WriteEndElement();
                    }

                    break;
                case XmlNodeType.EndElement:
                    writer.WriteFullEndElement();
                    break;
                case XmlNodeType.CDATA:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    writer.WriteCData(value!);
#else
                    writer.WriteCData(value);
#endif
                    break;
                case XmlNodeType.Text:
                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    writer.WriteString(value);
                    break;
            }
        }

        /// <summary>
        /// Moves to the next node to add.
        /// </summary>
        /// <returns><c>true</c> if the reader is on a node; <c>false</c> at the end of the data, or once the xCard root element has ended.</returns>
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
                        CheckNotDocumentType();
                    }
                }

                return false;
            }

            return _reader.Read();
        }

        /// <summary>
        /// Adds the reader's current node, or the end of the data, to the card being built.
        /// </summary>
        /// <param name="isNode"><c>true</c> if the reader is on a node; <c>false</c> at the end of the data.</param>
        /// <param name="value">The value of a text node; otherwise <c>null</c>.</param>
        /// <returns>What to do next.  <see cref="NodeResult.Done"/> means the read has its result: a card, ended by this node and checked, or <c>null</c> at the end of the data; see
        /// <see cref="TakeCard"/>.</returns>
        /// <exception cref="VcardFileException">The node is not valid where it is, or the card it ends breaks a rule of RFC 6350 that the options do not accept.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private NodeResult AddNode(bool isNode, string? value)
#else
        private NodeResult AddNode(bool isNode, string value)
#endif
        {
            try
            {
                if (!isNode)
                {
                    if (!_isFinished)
                    {
                        _isFinished = true;
                        _builder.Complete();
                    }

                    _card = null;
                    return NodeResult.Done;
                }

                long lineNumber, linePosition;
                GetPosition(out lineNumber, out linePosition);
                switch (_reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var result = _builder.StartElement(_reader.LocalName, _reader.NamespaceURI, _reader.GetAttribute(XcardNames.GroupName), _reader.IsEmptyElement, lineNumber,
                            linePosition, out _card);
                        if (result == XcardDocumentBuilder.StartResult.ReadForeignProperty) return NodeResult.ReadForeignProperty;

                        break;
                    case XmlNodeType.EndElement:
                        _card = _builder.EndElement(lineNumber, linePosition);
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
                        return NodeResult.Continue;
                    case XmlNodeType.DocumentType:
                        CheckNotDocumentType();
                        return NodeResult.Continue;
                    default:
                        return NodeResult.Continue;
                }
            }
            catch
            {
                // Where the card being built ends is now unknown.
                _isFaulted = true;
                throw;
            }

            return _card is null ? NodeResult.Continue : CheckCard();
        }

        /// <summary>
        /// Adds a property in another namespace, whose markup has been read, so that the reader is on its end.
        /// </summary>
        private void AddForeignProperty(string xml, long lineNumber, long linePosition)
        {
            try
            {
                _builder.AddForeignProperty(xml, lineNumber, linePosition);
            }
            catch
            {
                _isFaulted = true;
                throw;
            }
        }

        /// <summary>
        /// Checks the card that has been read in full.  A problem with it does not stop the reader.
        /// </summary>
        private NodeResult CheckCard()
        {
            // ref. RFC 6351 §5.1: the card is of vCard 4.0, so it is checked by those rules, after the problems found in its VERSION and the forms of its values.
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

            return NodeResult.Done;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? TakeCard()
#else
        private VcardCard TakeCard()
#endif
        {
            var card = _card;
            _card = null;

            // The reader may have been disposed while the card was being read or checked, and then a read in progress completes with ObjectDisposedException.
            ThrowIfDisposed();
            return card;
        }

        /// <summary>
        /// Throws if the reader is on a document type declaration, which xCard does not allow.
        /// </summary>
        /// <exception cref="VcardFileException">The reader is on a document type declaration.</exception>
        private void CheckNotDocumentType()
        {
            if (_reader.NodeType != XmlNodeType.DocumentType) return;

            _isFaulted = true;
            long lineNumber, linePosition;
            GetPosition(out lineNumber, out linePosition);
            throw XcardConverter.BuildException("The data has a document type definition (DTD), which an xCard document can't have.", lineNumber, linePosition, null);
        }

        private void GetPosition(out long lineNumber, out long linePosition)
        {
            lineNumber = _lineInfo?.LineNumber ?? 0;
            linePosition = _lineInfo?.LinePosition ?? 0;
        }

        /// <summary>
        /// Marks the reader as unable to continue, and builds the exception for XML that is not well-formed.
        /// </summary>
        private VcardFileException Fault(XmlException exception)
        {
            _isFaulted = true;
            var fileException = new VcardFileException("The data is not well-formed XML: " + exception.Message, exception);
            XcardConverter.AddLocation(fileException, exception.LineNumber, exception.LinePosition);
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
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            _fileStream?.Dispose();
#endif
        }

        private void ThrowIfDisposed()
        {
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(GetType().Name);
        }

        /// <summary>
        /// The settings of the markup of a property in another namespace: a fragment with no declaration and no indentation, whose line breaks are kept as they are on every
        /// platform, and a carriage return as a character reference, so that the text reads back the same.
        /// </summary>
        private static readonly XmlWriterSettings ForeignElementSettings = new XmlWriterSettings
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            NewLineHandling = NewLineHandling.Entitize,
            OmitXmlDeclaration = true
        };

        private static bool IsTextNode(XmlNodeType nodeType)
        {
            return nodeType == XmlNodeType.Text || nodeType == XmlNodeType.CDATA || nodeType == XmlNodeType.Whitespace || nodeType == XmlNodeType.SignificantWhitespace;
        }

        private static XmlReaderSettings CreateSettings(bool closeInput)
        {
            // ref. RFC 6351 §5.1: processing instructions whose target isn't recognised are ignored, and none is.  The data comes from outside, so it is not allowed a DTD.
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

        private static XmlReader CreateStreamReader(Stream stream, VcardOptions options, bool ownsStream)
        {
            if (stream is null)
                throw new ArgumentNullException("xcardStream");
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", "xcardStream");

            if (options.Encoding is null) return XmlReader.Create(stream, CreateSettings(ownsStream));

            // The text reader decodes the data, so the encoding the document declares is not used.
            var textReader = new StreamReader(stream, options.Encoding, true, 4096, !ownsStream);
            return XmlReader.Create(textReader, CreateSettings(true));
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static XmlReader CreateFileReader(string path, VcardOptions options, out Stream fileStream)
        {
            if (path is null)
                throw new ArgumentNullException("xcardFilePath");
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            fileStream = VcardFileStreams.OpenRead(path, 4096);
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

        private static XmlReader CreateTextReader(TextReader reader, VcardOptions options)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing reader.  Specify the encoding when creating the reader.", nameof(options));

            return XmlReader.Create(reader, CreateSettings(false));
        }

        private static XmlReader ValidateXmlReader(XmlReader reader, VcardOptions options)
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
        /// The cards of <see cref="ReadCardsAsync"/>, for a language version without async iterators.
        /// </summary>
        private sealed class AsyncCardEnumerable : IAsyncEnumerable<VcardCard>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly XcardReader _reader;

            internal AsyncCardEnumerable(XcardReader reader, CancellationToken cancellationToken)
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
                private readonly XcardReader _reader;

                internal Enumerator(XcardReader reader, CancellationToken readerToken, CancellationToken enumeratorToken)
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
