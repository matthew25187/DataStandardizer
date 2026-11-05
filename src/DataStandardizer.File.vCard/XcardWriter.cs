using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Represents a writer of the card object model as xCard data, the XML form of vCard (RFC 6351).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The cards are written in a <c>vcards</c> element in the namespace <c>urn:ietf:params:xml:ns:vcard-4.0</c>, which is started with the first card and ended when the writer is
    /// disposed; if no card is written, nothing is.  xCard represents vCard 4.0 alone, so each card is written in the form of vCard 4.0, as <see cref="VcardWriter"/> writes it with
    /// <see cref="VcardOptions.WriteVersion"/> set to vCard 4.0, whatever its <see cref="VcardCard.SourceVersion"/>, and without its <c>VERSION</c>, whose role the namespace plays
    /// (RFC 6351 §5.1).  Property and parameter names are written in lower case, the properties of a group in a <c>group</c> element whose <c>name</c> attribute holds the group as it
    /// is (§5), and each value in its typed value element, such as <c>date-time</c>, converted from the text form of RFC 6350 (§6).  In particular:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The <c>VALUE</c> parameter is not written, since the value element gives the type.  A DATE-AND-OR-TIME is written in a <c>date</c>, <c>date-time</c> or
    /// <c>time</c> element, with no <c>T</c> before a time alone.</description></item>
    /// <item><description>The value of a property whose type is not known, such as an <c>X-</c> property with no <c>VALUE</c> parameter, is written as it is in an <c>unknown</c>
    /// element (RFC 6351 §6), as is one whose <c>VALUE</c> parameter names a value type RFC 6350 does not define, which loses the parameter.</description></item>
    /// <item><description>Each value of a multi-valued property, such as <c>CATEGORIES</c>, is an element of its own.  The value of <c>N</c>, <c>ADR</c>, <c>GENDER</c> and
    /// <c>CLIENTPIDMAP</c> is written in the elements of its components, such as <c>surname</c> and <c>given</c>, one for each value of a component and an empty one for a
    /// component with none, and the components of <c>ORG</c> in <c>text</c> elements (RFC 6351 Appendix A).  The components that RFC 9554 adds to <c>N</c> and <c>ADR</c>, which
    /// RFC 9554 gives no xCard form, are written in the elements <c>surname2</c> and <c>generation</c>, and <c>room</c>, <c>apartment</c>, <c>floor</c>, <c>streetnumber</c>,
    /// <c>streetname</c>, <c>building</c>, <c>block</c>, <c>subdistrict</c>, <c>district</c>, <c>landmark</c> and <c>direction</c>.</description></item>
    /// <item><description>Each value of a parameter is in the value element RFC 6351 gives the parameter, or in an <c>unknown</c> element for a parameter that isn't recognised
    /// (§6) or a value that isn't valid for its type.  The values of the <c>TYPE</c>, <c>PID</c> and <c>SORT-AS</c> parameters are split on their commas, and those of a repeated
    /// one are joined.</description></item>
    /// <item><description>The value of an <c>XML</c> property with no parameters is copied as it is, as an element in its own namespace (§6).</description></item>
    /// <item><description>A value that is not valid for its type, which <see cref="VcardOptions.AllowInvalidValues"/> lets a card hold, is written as it is in the element of its
    /// type, which reads back the same, as a value that <see cref="VcardOptions.AllowInvalidValues"/> accepts.  A TEXT, URI, DATE-AND-OR-TIME or structured value that isn't valid
    /// is written as it is in an <c>unknown</c> element instead, and loses any <c>VALUE</c> parameter.</description></item>
    /// <item><description>A DATE of a year alone, such as <c>BDAY:1985</c>, which xCard has no form for, is written as it is in an <c>unknown</c> element, which reads back the same
    /// when the type is the property's default.  One with a <c>VALUE</c> parameter, whose type would be lost, is refused.</description></item>
    /// </list>
    /// <para>
    /// So a card written by this writer and read back by <see cref="XcardReader"/> has the same properties, with the same values; only the spelling may change, such as the case of a
    /// name or a BOOLEAN, the escaping of a TEXT value, the commas of a list parameter, the markup of an <c>XML</c> property, or the place of a <c>VALUE</c> parameter, which is read
    /// back after the other parameters.  A <c>VALUE</c> parameter that names the property's default type is not read back, since xCard cannot tell it apart from none.
    /// </para>
    /// <para>
    /// Before a card is written, it is checked as <see cref="VcardCard.Validate(VcardVersion)"/> does for vCard 4.0, against the same rules and options as <see cref="VcardWriter"/>.
    /// If it breaks a rule that the options do not accept, <see cref="VcardFileException"/> is thrown, carrying the property in its data, and nothing of that card is written.  The card
    /// is also refused if a name or a group is not a valid vCard name, it has a <c>BEGIN</c>, <c>END</c> or <c>GROUP</c> property, whose names xCard gives to its elements, a property
    /// has more than one <c>VALUE</c> parameter or a <c>VALUE</c> parameter and a DATE of a year alone, or a value holds a character that XML cannot hold.
    /// </para>
    /// <para>
    /// Of the options, <see cref="VcardOptions.Encoding"/> sets the encoding of a stream or file, UTF-8 with no byte order mark by default, and <see cref="VcardOptions.LineBreak"/>
    /// the line breaks of the indented XML.  The others that concern <c>.vcf</c> content lines, such as <see cref="VcardOptions.FoldLength"/> and
    /// <see cref="VcardOptions.WriteVersion"/>, do not apply.
    /// </para>
    /// <para>
    /// Only one operation may be in progress at a time.  Synchronous and asynchronous calls may follow one another, but a call made while another is in progress throws
    /// <see cref="InvalidOperationException"/>.  A write to the XML writer that fails leaves what reached it unknown, so later calls throw <see cref="InvalidOperationException"/>, and
    /// disposal ends no element of its own.  The asynchronous members, and the constructors that take a file path, are not available on netstandard1.1.
    /// </para>
    /// </remarks>
    public sealed class XcardWriter : IDisposable
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// Encoding used when none is specified: UTF-8 without a byte order mark, throwing on invalid characters.
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);

        private long _cardsWritten;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private bool _isCanceled;
#endif
        private bool _isFaulted;
        private readonly bool _isInternalWriter;
        private bool _isRootStarted;
        private long _linesWritten;

        // The number of elements started and not yet ended, including the root, so that disposal can end them.
        private int _openElementCount;
        private readonly VcardOptions _options;
        private long _propertiesWritten;
        private readonly XmlWriter _writer;

        // These are changed with Interlocked, so that an operation started on another thread while one is in progress is refused, and a Dispose from another thread releases the
        // writer once that operation has finished.
        private int _isDisposed;
        private int _isOperationInProgress;
        private int _isReleased;

        /// <summary>
        /// Creates a writer of xCard data to a stream, as UTF-8 without a byte order mark.
        /// </summary>
        /// <param name="xcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardStream"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcardStream"/> does not support writing.</exception>
        public XcardWriter(Stream xcardStream) : this(xcardStream, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of xCard data to a stream.
        /// </summary>
        /// <param name="xcardStream">The stream.  It is left open when the writer is disposed.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardStream"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="xcardStream"/> does not support writing.
        /// -or-
        /// <see cref="VcardOptions.LineBreak"/> is not valid.</exception>
        public XcardWriter(Stream xcardStream, VcardOptions options) : this(CreateStreamWriter(xcardStream, options), true, options)
        {
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Creates a writer of an xCard file, as UTF-8 without a byte order mark.  An existing file is overwritten.
        /// </summary>
        /// <param name="xcardFilePath">The path of the file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardFilePath"/> is <c>null</c>.</exception>
        public XcardWriter(string xcardFilePath) : this(xcardFilePath, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of an xCard file.  An existing file is overwritten.
        /// </summary>
        /// <param name="xcardFilePath">The path of the file.</param>
        /// <param name="options">The options.</param>
        /// <exception cref="ArgumentNullException"><paramref name="xcardFilePath"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.LineBreak"/> is not valid.</exception>
        public XcardWriter(string xcardFilePath, VcardOptions options) : this(CreateFileWriter(xcardFilePath, options), true, options)
        {
        }
#endif

        /// <summary>
        /// Creates a writer of xCard data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is not closed when the writer is disposed, but what has been written is flushed to it.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public XcardWriter(TextWriter writer) : this(writer, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of xCard data to a text writer.
        /// </summary>
        /// <param name="writer">The text writer.  It is not closed when the writer is disposed, but what has been written is flushed to it.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c>, since the text writer does its own encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is set.
        /// -or-
        /// <see cref="VcardOptions.LineBreak"/> is not valid.</exception>
        public XcardWriter(TextWriter writer, VcardOptions options) : this(CreateTextWriter(writer, options), true, options)
        {
        }

        /// <summary>
        /// Creates a writer of xCard data to an XML writer, which may be writing a larger document.
        /// </summary>
        /// <remarks>
        /// The <c>vcards</c> element is written at the XML writer's current position, and ended when the writer is disposed.  The XML writer's settings apply, such as its
        /// indentation.
        /// </remarks>
        /// <param name="writer">The XML writer.  It is neither flushed nor closed when the writer is disposed.  For the asynchronous members, its
        /// <see cref="XmlWriterSettings.Async"/> setting must be <c>true</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> is <c>null</c>.</exception>
        public XcardWriter(XmlWriter writer) : this(writer, new VcardOptions())
        {
        }

        /// <summary>
        /// Creates a writer of xCard data to an XML writer, which may be writing a larger document.
        /// </summary>
        /// <remarks>
        /// The <c>vcards</c> element is written at the XML writer's current position, and ended when the writer is disposed.  The XML writer's settings apply, such as its
        /// indentation.
        /// </remarks>
        /// <param name="writer">The XML writer.  It is neither flushed nor closed when the writer is disposed.  For the asynchronous members, its
        /// <see cref="XmlWriterSettings.Async"/> setting must be <c>true</c>.</param>
        /// <param name="options">The options.  <see cref="VcardOptions.Encoding"/> must be <c>null</c>, since the XML writer does its own encoding.</param>
        /// <exception cref="ArgumentNullException"><paramref name="writer"/> or <paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><see cref="VcardOptions.Encoding"/> is set.</exception>
        public XcardWriter(XmlWriter writer, VcardOptions options) : this(ValidateXmlWriter(writer, options), false, options)
        {
        }

        private XcardWriter(XmlWriter writer, bool isInternalWriter, VcardOptions options)
        {
            _writer = writer;
            _isInternalWriter = isInternalWriter;
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
        /// Gets the number of properties written so far, in all cards.  A card's <c>VERSION</c> is not written, so it is not counted.
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
        /// Ends the <c>vcards</c> element, then flushes and closes the XML writer the writer created itself, with the stream or file it opened.  A caller-supplied stream or text writer
        /// is left open, and a caller-supplied XML writer is neither flushed nor closed.
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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Ends the <c>vcards</c> element, then flushes and closes, asynchronously, the XML writer the writer created itself, with the stream or file it opened.  A caller-supplied
        /// stream or text writer is left open, and a caller-supplied XML writer is neither flushed nor closed.
        /// </summary>
        /// <remarks>
        /// May be called more than once, and shares its state with <see cref="Dispose"/>.  If an operation is in progress, it completes with <see cref="ObjectDisposedException"/>, and
        /// the document is ended once that operation has finished, so the task this returns has completed before then; await the operation as well before using the output.  The XML
        /// writer is disposed even if ending the document throws.
        /// </remarks>
        /// <returns>A task that completes once the document has been ended and the writer released; or a task that has already completed, if an operation is in progress, which then
        /// ends the document.</returns>
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
        /// Flushes the XML writer, whether the writer created it or it was supplied by the caller.  The <c>vcards</c> element is not ended.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write failed, so the writer cannot continue.</exception>
        public void Flush()
        {
            ThrowIfDisposed();
            BeginOperation();
            try
            {
                _writer.Flush();
            }
            catch
            {
                _isFaulted = true;
                throw;
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
        /// <exception cref="VcardFileException">The card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in xCard, and nothing has been
        /// written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write failed, so the writer cannot continue.</exception>
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
        /// <param name="progress">Receives the progress after each card has been handed to the XML writer, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="VcardWriteProgress.LinesWritten"/> is the number of content lines the cards written so far would have in <c>.vcf</c> form, as <see cref="VcardWriter"/> would
        /// write them in vCard 4.0.  An exception it throws ends the call; the card it was reporting has been written.</param>
        /// <exception cref="ObjectDisposedException">The writer has been disposed, either before this call or during it.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="cards"/> contains <c>null</c>.  The cards before it have been written.</exception>
        /// <exception cref="VcardFileException">A card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in xCard.  The cards before it have been
        /// written.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write failed, so the writer cannot continue.</exception>
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

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        /// <summary>
        /// Flushes the XML writer asynchronously, whether the writer created it or it was supplied by the caller.  The <c>vcards</c> element is not ended.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  If the flush is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the XML writer has been flushed.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the flush is in
        /// progress.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write failed, so the writer cannot continue.
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
        /// Writes a card asynchronously.
        /// </summary>
        /// <param name="card">The card.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked before the card is written, so that a canceled write never leaves part of a
        /// card.  If the write is canceled, the writer cannot continue.</param>
        /// <returns>A task that completes once the card has been handed to the XML writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in
        /// progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="card"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write failed, so the writer cannot continue.
        /// -or-
        /// The writer was created from an XML writer whose <see cref="XmlWriterSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="VcardFileException">By the task: the card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in xCard, and nothing has been
        /// written.</exception>
        /// <exception cref="OperationCanceledException">By the task: <paramref name="cancellationToken"/> was canceled.</exception>
        public ValueTask WriteCardAsync(VcardCard card, CancellationToken cancellationToken = default)
        {
            // Errors in how the writer is used are thrown now; errors in the card or the I/O, and cancellation, go to the task.
            ThrowIfDisposed();
            if (card is null)
                throw new ArgumentNullException(nameof(card));

            CheckAsync();
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
        /// <param name="progress">Receives the progress after each card has been handed to the XML writer, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="VcardWriteProgress.LinesWritten"/> is the number of content lines the cards written so far would have in <c>.vcf</c> form.  An exception it throws ends the
        /// call; the card it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked between cards.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every card has been handed to the XML writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in
        /// progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write failed, so the writer cannot continue.
        /// -or-
        /// The writer was created from an XML writer whose <see cref="XmlWriterSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="cards"/> contains <c>null</c>.  The cards before it have been written.</exception>
        /// <exception cref="VcardFileException">By the task: a card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in xCard.  The cards before it have
        /// been written.</exception>
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

            CheckAsync();
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
        /// <param name="progress">Receives the progress after each card has been handed to the XML writer, synchronously on the writing thread; or <c>null</c>.  Its
        /// <see cref="VcardWriteProgress.LinesWritten"/> is the number of content lines the cards written so far would have in <c>.vcf</c> form.  An exception it throws ends the
        /// call; the card it was reporting has been written.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.  It is checked between cards.  If the write is canceled, the writer cannot
        /// continue.</param>
        /// <returns>A task that completes once every card has been handed to the XML writer.</returns>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.  Thrown by this call, or by the task if the writer is disposed while the write is in
        /// progress.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="cards"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Another operation is in progress.
        /// -or-
        /// An earlier asynchronous operation was canceled, or a write failed, so the writer cannot continue.
        /// -or-
        /// The writer was created from an XML writer whose <see cref="XmlWriterSettings.Async"/> setting is <c>false</c>.</exception>
        /// <exception cref="ArgumentException">By the task: <paramref name="cards"/> contains <c>null</c>.  The cards before it have been written.</exception>
        /// <exception cref="VcardFileException">By the task: a card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in xCard.  The cards before it have
        /// been written.</exception>
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

            CheckAsync();
            BeginOperation();
            return WriteAsyncCardsCoreAsync(cards, progress, cancellationToken);
        }

        private async Task FlushCoreAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await _writer.FlushAsync().ConfigureAwait(false);
                }
                catch
                {
                    _isFaulted = true;
                    throw;
                }

                // The writer may have been disposed while the flush was in progress.
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

        private async ValueTask WriteCardOperationAsync(VcardCard card, CancellationToken cancellationToken)
        {
            try
            {
                await WriteCardCoreAsync(card, cancellationToken).ConfigureAwait(false);

                // The writer may have been disposed while the card was being written.
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
                // Check here as well, so that a canceled token cancels the call when there are no cards, as it does for an asynchronous sequence.
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var card in cards)
                {
                    ThrowIfDisposed();
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
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
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
                if (EndOperation())
                {
                    await ReleaseAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Checks a card and hands its elements to the XML writer, asynchronously.
        /// </summary>
        private async Task WriteCardCoreAsync(VcardCard card, CancellationToken cancellationToken)
        {
            // Cancellation is checked only before the card is started, so that a canceled write never leaves part of a card that disposal would then close as though it were complete.
            cancellationToken.ThrowIfCancellationRequested();
            var properties = PrepareCard(card);
            try
            {
                if (!_isRootStarted)
                {
                    if (_isInternalWriter)
                    {
                        await _writer.WriteStartDocumentAsync().ConfigureAwait(false);
                    }

                    await _writer.WriteStartElementAsync(null, XcardNames.VCards, XcardNames.Namespace).ConfigureAwait(false);
                    _openElementCount++;
                    _isRootStarted = true;
                }

                await _writer.WriteStartElementAsync(null, XcardNames.VCard, XcardNames.Namespace).ConfigureAwait(false);
                _openElementCount++;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                string? group = null;
#else
                string group = null;
#endif
                foreach (var property in properties)
                {
                    if (property.Group != group)
                    {
                        if (group != null)
                        {
                            await _writer.WriteEndElementAsync().ConfigureAwait(false);
                            _openElementCount--;
                        }

                        group = property.Group;
                        if (group != null)
                        {
                            await _writer.WriteStartElementAsync(null, XcardNames.Group, XcardNames.Namespace).ConfigureAwait(false);
                            _openElementCount++;
                            await _writer.WriteAttributeStringAsync(null, XcardNames.GroupName, null, group).ConfigureAwait(false);
                        }
                    }

                    await WriteElementAsync(property.Element).ConfigureAwait(false);
                }

                if (group != null)
                {
                    await _writer.WriteEndElementAsync().ConfigureAwait(false);
                    _openElementCount--;
                }

                await _writer.WriteEndElementAsync().ConfigureAwait(false);
                _openElementCount--;
            }
            catch
            {
                // What reached the XML writer is now unknown.
                _isFaulted = true;
                throw;
            }

            CountCard(properties.Count);
        }

        private async Task WriteElementAsync(XcardElement element)
        {
            if (element.RawXml != null)
            {
                using (var reader = CreateRawXmlReader(element.RawXml))
                {
                    await reader.MoveToContentAsync().ConfigureAwait(false);
                    await _writer.WriteNodeAsync(reader, false).ConfigureAwait(false);
                }

                return;
            }

            await _writer.WriteStartElementAsync(null, element.Name, XcardNames.Namespace).ConfigureAwait(false);
            _openElementCount++;
            // An element with no text, such as that of an empty component, is written empty, as RFC 6351 §4 writes <additional/>.
            if (element.Children.Count == 0 && element.Text.Length != 0)
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
                if (!_isFaulted && _writer.WriteState != WriteState.Error && _writer.WriteState != WriteState.Closed)
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
                    // XmlWriter has no DisposeAsync here, and Dispose would flush the caller's stream or text writer synchronously, even with nothing left to write.  An XML writer
                    // over the caller's output leaves it open, so disposing it would do nothing else.
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
        /// Checks a card and hands its elements to the XML writer.
        /// </summary>
        private void WriteCardCore(VcardCard card)
        {
            var properties = PrepareCard(card);
            try
            {
                if (!_isRootStarted)
                {
                    if (_isInternalWriter)
                    {
                        _writer.WriteStartDocument();
                    }

                    _writer.WriteStartElement(XcardNames.VCards, XcardNames.Namespace);
                    _openElementCount++;
                    _isRootStarted = true;
                }

                _writer.WriteStartElement(XcardNames.VCard, XcardNames.Namespace);
                _openElementCount++;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                string? group = null;
#else
                string group = null;
#endif
                foreach (var property in properties)
                {
                    if (property.Group != group)
                    {
                        if (group != null)
                        {
                            _writer.WriteEndElement();
                            _openElementCount--;
                        }

                        group = property.Group;
                        if (group != null)
                        {
                            _writer.WriteStartElement(XcardNames.Group, XcardNames.Namespace);
                            _openElementCount++;
                            _writer.WriteAttributeString(XcardNames.GroupName, group);
                        }
                    }

                    WriteElement(property.Element);
                }

                if (group != null)
                {
                    _writer.WriteEndElement();
                    _openElementCount--;
                }

                _writer.WriteEndElement();
                _openElementCount--;
            }
            catch
            {
                // What reached the XML writer is now unknown.
                _isFaulted = true;
                throw;
            }

            CountCard(properties.Count);
        }

        private void WriteElement(XcardElement element)
        {
            if (element.RawXml != null)
            {
                using (var reader = CreateRawXmlReader(element.RawXml))
                {
                    reader.MoveToContent();
                    _writer.WriteNode(reader, false);
                }

                return;
            }

            _writer.WriteStartElement(element.Name, XcardNames.Namespace);
            _openElementCount++;
            // An element with no text, such as that of an empty component, is written empty, as RFC 6351 §4 writes <additional/>.
            if (element.Children.Count == 0 && element.Text.Length != 0)
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
        /// Checks a card, and builds the elements to write for it.
        /// </summary>
        /// <exception cref="VcardFileException">The card breaks a rule of RFC 6350 that the options do not accept, or cannot be written in xCard.</exception>
        private List<PreparedProperty> PrepareCard(VcardCard card)
        {
            // Gather the lines first, so that a card changed by another thread cannot fail part way through for that reason.  ref. RFC 6351 §5.1: the card is written in the form
            // of vCard 4.0, and checked by its rules.
            var mapping = VcardVersionMapping.Canonical;
            var diagnostics = new List<VcardDiagnostic>();
            var lines = mapping.FromCanonical(card, _options, diagnostics);
            mapping.Validate(lines, card, _options, 0, diagnostics);
            foreach (var diagnostic in diagnostics)
            {
                if (!VcardConformance.IsAccepted(diagnostic.Kind, _options, true))
                    throw VcardConformance.BuildException(diagnostic);
            }

            // Build the elements first, so that a card that cannot be written in xCard fails before any of it is written.
            var properties = new List<PreparedProperty>();
            foreach (var line in lines)
            {
                if (line.Group != null && !XcardConverter.IsVcardName(line.Group))
                {
                    var exception = new VcardFileException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "The group {0} cannot be written in xCard, since it is not one or more letters, digits and hyphens: RFC 6350 §3.3.", line.Group));
                    exception.Data[VcardFileException.DataItemName.PropertyName] = line.Name;
                    exception.Data[VcardFileException.DataItemName.Group] = line.Group;
                    throw exception;
                }

                properties.Add(new PreparedProperty(line.Group, XcardConverter.ToElement(line)));
            }

            return properties;
        }

        private void CountCard(int propertyCount)
        {
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
        /// Creates a reader of the value of an <c>XML</c> property, which has been checked to be one element.
        /// </summary>
        private static XmlReader CreateRawXmlReader(string xml)
        {
            var settings = new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, DtdProcessing = DtdProcessing.Prohibit };
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
            settings.Async = true;
            settings.XmlResolver = null;
#endif
            return XmlReader.Create(new StringReader(xml), settings);
        }

        /// <summary>
        /// Marks an operation as in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another operation is in progress, or an earlier one was canceled or failed to write.</exception>
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

            if (_isFaulted)
            {
                if (EndOperation())
                {
                    Release();
                }

                throw new InvalidOperationException("The writer cannot continue after a write failed.");
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
        /// Ends the document, then flushes and closes the XML writer the writer created itself.
        /// </summary>
        private void Release()
        {
            try
            {
                if (!_isFaulted && _writer.WriteState != WriteState.Error && _writer.WriteState != WriteState.Closed)
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
        private static VcardCard CheckCard(VcardCard? card)
#else
        private static VcardCard CheckCard(VcardCard card)
#endif
        {
            if (card is null)
                throw new ArgumentException("The cards cannot contain null.", "cards");

            return card;
        }

        private static XmlWriterSettings CreateSettings(VcardOptions options, Encoding encoding, bool closeOutput)
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

        private static XmlWriter CreateStreamWriter(Stream stream, VcardOptions options)
        {
            if (stream is null)
                throw new ArgumentNullException("xcardStream");
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "xcardStream");

            return XmlWriter.Create(stream, CreateSettings(options, options.Encoding ?? DefaultEncoding, false));
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private static XmlWriter CreateFileWriter(string path, VcardOptions options)
        {
            if (path is null)
                throw new ArgumentNullException("xcardFilePath");
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            var settings = CreateSettings(options, options.Encoding ?? DefaultEncoding, true);
            var fileStream = VcardFileStreams.OpenWrite(path, 4096);
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

        private static XmlWriter CreateTextWriter(TextWriter writer, VcardOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(options));

            return XmlWriter.Create(writer, CreateSettings(options, DefaultEncoding, false));
        }

        private static XmlWriter ValidateXmlWriter(XmlWriter writer, VcardOptions options)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.Encoding != null)
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(options));

            return writer;
        }

        /// <summary>
        /// A property ready to be written: its group and its element.
        /// </summary>
        private sealed class PreparedProperty
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal PreparedProperty(string? group, XcardElement element)
#else
            internal PreparedProperty(string group, XcardElement element)
#endif
            {
                Group = group;
                Element = element;
            }

            internal XcardElement Element { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal string? Group { get; }
#else
            internal string Group { get; }
#endif
        }
    }
}
