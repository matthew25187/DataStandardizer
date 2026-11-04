using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Follows the structure of a message (RFC 2046 §5), event by event.  It does no I/O: when it needs more input it says so, and a driver refills
    /// the block of the <see cref="ImfHeaderScanner"/> it shares, and calls it again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack.
    /// </para>
    /// <para>
    /// The search for a delimiter looks at each line start once.  A line is compared with the open boundaries only if it starts with <c>--</c> and
    /// has been found to end within the longest line that RFC 5322 §2.1.1 allows, so the scanner holds back at most that much of a body, and the
    /// search resumes at the end of a line that is not a delimiter.  The work is linear in the input for a given depth of nesting, however the input
    /// is split.
    /// </para>
    /// </remarks>
    internal sealed class ImfEntityScanner
    {
        internal enum Status
        {
            EventRead,
            NeedMoreInput,
            EndOfMessage
        }

        private enum State
        {
            EntityStart,
            Header,
            BeforeEmbeddedMessage,
            Content,
            EpilogueSeparator,
            Unwinding,
            End
        }

        private enum ContentResult
        {
            Chunk,
            NeedMoreInput,
            Delimiter,
            EndOfData
        }

        private enum LineMatch
        {
            NoMatch,
            NeedMoreInput,
            Delimiter
        }

        private enum BodyDecoding
        {
            None,
            QuotedPrintable,
            Base64
        }

        private static class ErrorMessage
        {
            internal const string BoundaryInvalid = "The boundary parameter must be 1 to 70 characters of the set that RFC 2046 §5.1.1 allows, and must not end with a space.";
            internal const string BoundaryMissing = "A multipart entity requires a boundary parameter (RFC 2046 §5.1.1).";
            internal const string CloseWithoutPart = "Found the close delimiter of a multipart entity with no parts.  RFC 2046 §5.1.1 requires at least one.";
            internal const string CompositeEncodingTemplate = "The transfer encoding '{0}' is not allowed on a '{1}' entity, which may use only 7bit, 8bit or binary (RFC 2045 §6.4).";
            internal const string ContentTypeDuplicate = "The header section has more than one Content-Type field (RFC 2045 §3).";
            internal const string ContentTypeInvalid = "The Content-Type field is not valid (RFC 2045 §5.1).";
            internal const string InvalidBase64 = "The body is not valid base64 (RFC 2045 §6.8).";
            internal const string InvalidQuotedPrintable = "The body is not valid quoted-printable (RFC 2045 §6.7).";
            internal const string MissingCloseDelimiter = "A multipart entity has no close delimiter (RFC 2046 §5.1.1).";
            internal const string NestedTooDeepTemplate = "The entities are nested deeper than the maximum of {0}.";
            internal const string SevenBitOnlyTemplate = "The transfer encoding '{0}' is not allowed on a '{1}' entity, which must use 7bit (RFC 2046 §5.2).";
            internal const string TooManyPartsTemplate = "The message has more than the maximum of {0} parts.";
            internal const string TransferEncodingDuplicate = "The header section has more than one Content-Transfer-Encoding field (RFC 2045 §3).";
            internal const string TransferEncodingInvalid = "The Content-Transfer-Encoding field is not valid (RFC 2045 §6.1).";
            internal const string MissingMimeVersion = "The header of the message has a MIME field and no MIME-Version field (RFC 2045 §4).";
            internal const string UnknownTransferEncodingTemplate = "The transfer encoding '{0}' is not known, so the body has not been decoded.";
        }

        /// <summary>
        /// An entity whose end has not been reached.
        /// </summary>
        // The bytes of what a part or a message holds, in the order they were read.
        private sealed class BodyRange
        {
            internal long Start;
            internal long End;
            internal string Path = string.Empty;
            internal bool IsBinary;
        }

        private sealed class Frame
        {
            internal byte[] Boundary = EmptyBytes;
            internal MimeMediaType ContentType;
            internal int Depth;
            internal bool HasContentType;
            internal bool HasInvalidHeaderOctet;
            internal bool HasMimeField;
            internal bool HasMimeVersion;
            internal bool HasTransferEncoding;
            internal bool IsBoundaryActive;
            internal bool IsHeaderEnded;
            internal bool IsMessage;
            internal bool IsMime;
            internal MimeDataDomain AllowedDomain = MimeDataDomain.Binary;
            internal int PartCount;
            internal int PathNumber;
            internal long Start;
            internal MimeTransferEncoding TransferEncoding;
        }

        private const byte CarriageReturn = 0x0D;
        private const byte Hyphen = 0x2D;
        private const byte LineFeed = 0x0A;
        private const byte Space = 0x20;
        private const byte Tab = 0x09;

        private const int DashLength = 2;
        private const int LineBreakLength = 2;

        // ref. RFC 5322 §2.1.1: a line that is longer can't be a delimiter line, so the whole of a delimiter line is never held for long.
        private const int MaxLineLength = 998;

        // ref. RFC 2046 §5.1.1: boundary := 0*69<bchars> bcharsnospace
        private const int MaxBoundaryLength = 70;

        private const int MinDecodeBufferLength = 256;

        private const string ContentTypeFieldName = "content-type";
        private const string TransferEncodingFieldName = "content-transfer-encoding";
        private const string MimeVersionFieldName = "mime-version";
        private const string ContentIdFieldName = "content-id";
        private const string ContentDescriptionFieldName = "content-description";
        private const string ContentDispositionFieldName = "content-disposition";
        private const string ContentLanguageFieldName = "content-language";
        private const string BoundaryParameterName = "boundary";
        private const string DigestSubtype = "digest";
        private const string ExternalBodySubtype = "external-body";
        private const string GlobalSubtype = "global";
        private const string PartialSubtype = "partial";
        private const string Rfc822Subtype = "rfc822";

        private static readonly byte[] EmptyBytes = new byte[0];

        // ref. RFC 2045 §5.2: the default media type.
        private static readonly MimeMediaType DefaultContentType = MimeMediaType.TextPlain.WithParameter("charset", "us-ascii");

        private readonly bool _acceptBareLineFeed;
        private readonly bool _allowInvalidBodyOctets;
        private readonly bool _allowLongLines;
        private readonly bool _allowMissingMimeVersion;
        private readonly ImfBodyCheck _bodyCheck;
        private readonly bool _allowMissingBoundaryTerminator;
        private readonly MimeBase64Decoder _base64Decoder = new MimeBase64Decoder(MimeBase64Mode.Mime);
        private readonly IMimeCharsetResolver _charsetResolver;
        private readonly MimeMediaTypeParseOptions _contentTypeOptions;
        private bool _decodeTransferEncoding;
        private readonly List<ImfDiagnostic> _diagnostics = new List<ImfDiagnostic>();
        private readonly List<Frame> _frames = new List<Frame>();
        private readonly bool _lineFeedIsLineBreak;
        private readonly int _maxNestingDepth;
        private readonly int _maxPartCount;
        private readonly MimeQuotedPrintableDecoder _quotedPrintableDecoder;
        private readonly ImfHeaderScanner _scanner;

        private int _activeBoundaryCount;
        private bool _atLineStart;
        private int _bodyStartLineBreakLength;
        private int _closeLineBreakLength;
        private ImfEntityEventKind _contentKind;
        private int _dataLength;
        private int _dataStart;
        private byte[] _decodeBuffer = EmptyBytes;
        private BodyDecoding _decoding;
        private bool _decodingFinished;
        private byte[] _eventData = EmptyBytes;
        private long _eventByteLength;
        private long _eventByteOffset;
        private ImfEntityEventKind _eventKind;
        private bool _isEventDataDecoded;
        private bool _isEntityEndPending;
        private int _lineScan = -1;
        private int _matchContentEnd;
        private int _matchFrameIndex;
        private bool _matchIsClose;
        private int _matchLineEnd;
        private int _partCount;
        private long _pendingRawHeld;
        private int _pendingRawLength;
        private int _search;
        private readonly List<BodyRange> _bodyRanges = new List<BodyRange>();
        private State _state = State.EntityStart;
        private long _unwindEnd;
        private bool _unwindIsDelimiter;
        private long _entitiesRead;

        /// <param name="scanner">The scanner to read header sections with, whose block also holds the bodies.</param>
        /// <param name="options">The options.</param>
        /// <param name="lineFeedIsLineBreak"><c>true</c> if a line feed is the line break, as in the messages inside mbox and EMLX files.</param>
        internal ImfEntityScanner(ImfHeaderScanner scanner, ImfOptions options, bool lineFeedIsLineBreak)
        {
            _scanner = scanner;
            _allowInvalidBodyOctets = options.AllowInvalidBodyOctets;
            _allowLongLines = options.AllowLongLines;
            _allowMissingMimeVersion = options.AllowMissingMimeVersion;
            _bodyCheck = new ImfBodyCheck(options, lineFeedIsLineBreak);
            _acceptBareLineFeed = options.AcceptBareLineFeed;
            _allowMissingBoundaryTerminator = options.AllowMissingBoundaryTerminator;
            _charsetResolver = options.CharsetResolver;
            _contentTypeOptions = ImfMimeFields.GetContentTypeParseOptions(options) | MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters;
            _decodeTransferEncoding = options.DecodeTransferEncoding;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
            _maxNestingDepth = options.MaxNestingDepth;
            _maxPartCount = options.MaxPartCount;
            _quotedPrintableDecoder = new MimeQuotedPrintableDecoder(
                MimeQuotedPrintableMode.Body,
                options.AcceptBareLineFeed || lineFeedIsLineBreak ? MimeQuotedPrintableDecoderOptions.AllowBareLineFeeds : MimeQuotedPrintableDecoderOptions.None);

            _frames.Add(new Frame { IsMessage = true });
            _entitiesRead = 1;
        }

        /// <summary>
        /// Leaves bodies as raw bytes, whatever the options say, for a caller that goes only by the ranges of the data.
        /// </summary>
        internal void LeaveBodiesRaw()
        {
            _decodeTransferEncoding = false;
        }

        /// <summary>
        /// Gets the scanner that reads the header sections, whose block a driver refills.
        /// </summary>
        internal ImfHeaderScanner HeaderScanner => _scanner;

        /// <summary>
        /// Gets the problems found so far that did not stop reading.
        /// </summary>
        internal List<ImfDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// Gets the number of entities started so far, including the message itself.
        /// </summary>
        internal long EntitiesRead => _entitiesRead;

        /// <summary>
        /// Gets a flag indicating that a problem was found, after which the structure of the message is unknown.
        /// </summary>
        internal bool IsFaulted { get; private set; }

        /// <summary>
        /// Gets or sets what gives, once the message has ended, the lines of it that are too long in a form that this scanner doesn't see, as with a quote that a container took off.
        /// </summary>
        [AllowNull]
        internal Func<List<ImfLongLine>> LongLinesOutsideTheData { get; set; }

        internal ImfEntityEventKind EventKind => _eventKind;

        internal long EventByteLength => _eventByteLength;

        internal long EventByteOffset => _eventByteOffset;

        internal ReadOnlyMemory<byte> Data => new ReadOnlyMemory<byte>(_eventData, _dataStart, _dataLength);

        internal bool IsDataDecoded => _isEventDataDecoded;

        internal int Depth => CurrentFrame.Depth;

        internal string EntityPath => BuildPath();

        internal MimeMediaType ContentType
        {
            get
            {
                CheckHeaderEnded();
                return CurrentFrame.ContentType;
            }
        }

        internal MimeTransferEncoding TransferEncoding
        {
            get
            {
                CheckHeaderEnded();
                return CurrentFrame.TransferEncoding;
            }
        }

        private Frame CurrentFrame => _frames[_frames.Count - 1];

        /// <summary>
        /// Checks that the current event is a header field.
        /// </summary>
        /// <exception cref="InvalidOperationException">The current event is of another kind.</exception>
        internal void CheckIsHeaderField()
        {
            if (_eventKind != ImfEntityEventKind.HeaderField)
                throw new InvalidOperationException("The event is not a header field.");
        }

        /// <summary>
        /// Makes room in the block for at least one more byte.
        /// </summary>
        /// <param name="offset">The index in the block to read into.</param>
        /// <param name="count">The number of bytes that may be read.</param>
        internal void PrepareRefill(out int offset, out int count)
        {
            var positionBefore = _scanner.Position;
            _scanner.PrepareRefill(out offset, out count);

            var shift = positionBefore - _scanner.Position;
            _search -= shift;
            if (_lineScan >= 0)
            {
                _lineScan -= shift;
            }
        }

        /// <summary>
        /// Finds the next event.
        /// </summary>
        /// <returns><see cref="Status.EventRead"/> when there is an event; <see cref="Status.NeedMoreInput"/> when the driver must refill the block and
        /// call this method again; <see cref="Status.EndOfMessage"/> when the message has ended, which every later call also gives.</returns>
        /// <exception cref="ImfFileException">The message breaks a rule of its format, or passes a limit.  The scanner is then faulted.</exception>
        internal Status Next()
        {
            try
            {
                return Advance();
            }
            catch (ImfFileException exception)
            {
                IsFaulted = true;
                // A problem found once the message has ended has said which entity it is in.
                if (!exception.Data.Contains(ImfFileException.DataItemName.EntityPath)) exception.Data[ImfFileException.DataItemName.EntityPath] = BuildPath();

                throw;
            }
        }

        /// <summary>
        /// Returns the pooled blocks.  The scanner must not be used afterwards.
        /// </summary>
        internal void ReturnBuffers()
        {
            _scanner.ReturnBuffers();
            if (_decodeBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_decodeBuffer);
                _decodeBuffer = EmptyBytes;
            }
        }

        private Status Advance()
        {
            ClearEventData();

            while (true)
            {
                if (_isEntityEndPending)
                {
                    PopFrame();
                }

                switch (_state)
                {
                    case State.EntityStart:
                        _scanner.BeginSection();
                        _state = State.Header;
                        return Emit(ImfEntityEventKind.EntityStart, CurrentFrame.Start, 0);

                    case State.Header:
                        var scanStatus = _scanner.Scan();
                        if (scanStatus == ImfHeaderScanner.ScanStatus.NeedMoreInput) return Status.NeedMoreInput;

                        if (scanStatus == ImfHeaderScanner.ScanStatus.FieldRead)
                        {
                            _scanner.ParseField();
                            ReadMimeField(CurrentFrame);
                            CheckNestedHeaderField(CurrentFrame);
                            return Emit(ImfEntityEventKind.HeaderField, _scanner.FieldBytePosition, _scanner.RawBytes.Length);
                        }

                        EndHeader(CurrentFrame);
                        return Emit(ImfEntityEventKind.HeaderEnd, CurrentFrame.Start, _scanner.BytePosition - CurrentFrame.Start);

                    case State.BeforeEmbeddedMessage:
                        if (!ReadBeforeEmbeddedMessage()) return Status.NeedMoreInput;

                        break;

                    case State.Content:
                        if (_pendingRawLength > 0)
                        {
                            if (DecodePending()) return Status.EventRead;

                            break;
                        }

                        int chunkEnd;
                        var contentResult = ScanContent(out chunkEnd);
                        if (contentResult == ContentResult.NeedMoreInput) return Status.NeedMoreInput;

                        if (contentResult == ContentResult.Chunk)
                        {
                            if (ReadChunk(chunkEnd)) return Status.EventRead;

                            break;
                        }

                        ReportBodyProblems(false, _bodyCheck.End(), 0, 0);
                        if (FinishDecoding()) return Status.EventRead;

                        StartUnwinding(contentResult == ContentResult.Delimiter);
                        break;

                    case State.EpilogueSeparator:
                        if (!ReadEpilogueSeparator()) return Status.NeedMoreInput;

                        break;

                    case State.Unwinding:
                        var unwindTarget = _unwindIsDelimiter ? _matchFrameIndex : -1;
                        if (_frames.Count - 1 > unwindTarget)
                        {
                            _isEntityEndPending = true;
                            return Emit(ImfEntityEventKind.EntityEnd, CurrentFrame.Start, _unwindEnd - CurrentFrame.Start);
                        }

                        if (_unwindIsDelimiter)
                        {
                            FollowDelimiter();
                        }
                        else
                        {
                            _state = State.End;
                            CheckLongLinesOutsideTheData();
                        }

                        break;

                    default:
                        return Status.EndOfMessage;
                }
            }
        }

        private void ClearEventData()
        {
            _eventData = EmptyBytes;
            _dataStart = 0;
            _dataLength = 0;
            _isEventDataDecoded = false;
        }

        private Status Emit(ImfEntityEventKind kind, long byteOffset, long byteLength)
        {
            _eventKind = kind;
            _eventByteOffset = byteOffset;
            _eventByteLength = byteLength;
            return Status.EventRead;
        }

        private void PopFrame()
        {
            _isEntityEndPending = false;
            var frame = CurrentFrame;
            if (frame.IsBoundaryActive)
            {
                frame.IsBoundaryActive = false;
                _activeBoundaryCount--;
            }

            _frames.RemoveAt(_frames.Count - 1);
        }

        /// <summary>
        /// Takes what this layer needs from a Content-Type or Content-Transfer-Encoding field.
        /// </summary>
        private void ReadMimeField(Frame frame)
        {
            var name = _scanner.Name.Span;
            if (EqualsIgnoreCase(name, MimeVersionFieldName))
            {
                frame.HasMimeVersion = true;
            }
            else if (EqualsIgnoreCase(name, ContentIdFieldName) || EqualsIgnoreCase(name, ContentDescriptionFieldName)
                || EqualsIgnoreCase(name, ContentDispositionFieldName) || EqualsIgnoreCase(name, ContentLanguageFieldName))
            {
                frame.HasMimeField = true;
            }
            else if (EqualsIgnoreCase(name, ContentTypeFieldName))
            {
                frame.HasMimeField = true;
                if (frame.HasContentType) throw _scanner.BuildCurrentFieldException(ErrorMessage.ContentTypeDuplicate);

                frame.ContentType = ParseContentType(_scanner.GetValue());
                frame.HasContentType = true;
            }
            else if (EqualsIgnoreCase(name, TransferEncodingFieldName))
            {
                frame.HasMimeField = true;
                if (frame.HasTransferEncoding) throw _scanner.BuildCurrentFieldException(ErrorMessage.TransferEncodingDuplicate);

                frame.TransferEncoding = ParseTransferEncoding(_scanner.GetValue());
                frame.HasTransferEncoding = true;
            }
        }

        /// <summary>
        /// Checks the current field of a nested entity against the entity that holds it, whose encoding limits what it holds (RFC 2045 §6.4).  The header of the message itself has nothing
        /// holding it, and RFC 6532 lets it hold UTF-8.
        /// </summary>
        private void CheckNestedHeaderField(Frame frame)
        {
            if (_frames.Count < 2 || frame.HasInvalidHeaderOctet) return;
            if (!ImfBodyCheck.HasInvalidHeaderOctet(_scanner.RawBytes.Span, _frames[_frames.Count - 2].AllowedDomain)) return;

            frame.HasInvalidHeaderOctet = true;
            if (!_allowInvalidBodyOctets)
            {
                var exception = _scanner.BuildCurrentFieldException(ImfBodyCheck.InvalidHeaderOctetsMessage);
                exception.Data[ImfFileException.DataItemName.DiagnosticKind] = ImfDiagnosticKind.InvalidBodyOctets;
                throw exception;
            }

            _diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.InvalidBodyOctets, ImfBodyCheck.InvalidHeaderOctetsMessage, _scanner.FieldBytePosition, BuildPath()));
        }

        /// <summary>
        /// Keeps where a run of content is, if there will be lines to find it for.
        /// </summary>
        private void RecordBodyRange(long start, long length)
        {
            if (LongLinesOutsideTheData is null) return;

            var path = BuildPath();
            var last = _bodyRanges.Count == 0 ? null : _bodyRanges[_bodyRanges.Count - 1];
            if (!(last is null) && last.End == start && last.Path == path)
            {
                last.End = start + length;
                return;
            }

            // ref. RFC 2045 §2.9: binary data has no lines.  A preamble or epilogue isn't in the encoding of the entity, only under its label.
            var isBinary = _contentKind == ImfEntityEventKind.BodyChunk && CurrentFrame.TransferEncoding == MimeTransferEncoding.Binary;
            _bodyRanges.Add(new BodyRange { Start = start, End = start + length, Path = path, IsBinary = isBinary });
        }

        private void CheckLongLinesOutsideTheData()
        {
            if (LongLinesOutsideTheData is null) return;

            foreach (var line in LongLinesOutsideTheData())
            {
                var range = _bodyRanges.Find(body => line.MessageOffset >= body.Start && line.MessageOffset < body.End);
                if (!(range is null) && range.IsBinary) continue;

                var diagnostic = new ImfDiagnostic(ImfDiagnosticKind.LongBodyLine, ImfBodyCheck.LongQuotedLineMessage, line.BytePosition, range?.Path ?? string.Empty, string.Empty, line.LineNumber);
                if (!_allowLongLines)
                    throw ImfConformance.BuildException(diagnostic, true);

                _diagnostics.Add(diagnostic);
            }
        }

        /// <summary>
        /// Parses the value of a Content-Type field, with comments, and white space around the delimiters.
        /// </summary>
        /// <remarks>
        /// The RFC 2045 §5.1 grammar is applied, not the narrower names that RFC 6838 §4.2 requires of a registration.  A field that breaks RFC 2231 is
        /// rejected as a whole, as is one that names a character set that can't be resolved.
        /// </remarks>
        private MimeMediaType ParseContentType(string value)
        {
            string text;
            MimeMediaType mediaType;
            if (!ImfFieldComments.TryRemove(value, out text)
                || !MimeMediaType.TryParse(text, _contentTypeOptions, _charsetResolver, out mediaType))
            {
                throw _scanner.BuildCurrentFieldException(ErrorMessage.ContentTypeInvalid);
            }

            return mediaType;
        }

        private MimeTransferEncoding ParseTransferEncoding(string value)
        {
            string text;
            MimeTransferEncoding encoding;
            if (!ImfFieldComments.TryRemove(value, out text) || !MimeTransferEncoding.TryParse(text.Trim(' ', '\t'), out encoding))
            {
                throw _scanner.BuildCurrentFieldException(ErrorMessage.TransferEncodingInvalid);
            }

            return encoding;
        }

        /// <summary>
        /// Works out from the finished header section how the body is to be read.
        /// </summary>
        private void EndHeader(Frame frame)
        {
            // ref. RFC 2045 §4: the field is required at the top level of a message, and in an embedded message that claims to follow MIME.
            if (frame.IsMessage && frame.HasMimeField && !frame.HasMimeVersion)
            {
                if (_frames.Count == 1 && !_allowMissingMimeVersion)
                    throw BuildKindError(ErrorMessage.MissingMimeVersion, ImfDiagnosticKind.MissingMimeVersion);

                _diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.MissingMimeVersion, ErrorMessage.MissingMimeVersion, _scanner.BytePosition, BuildPath()));
            }

            if (!frame.HasContentType)
            {
                // ref. RFC 2046 §5.1.5: the default in a digest is a message.
                frame.ContentType = IsDirectPartOfDigest(frame) ? MimeMediaType.MessageRfc822 : DefaultContentType;
            }

            if (!frame.HasTransferEncoding)
            {
                // ref. RFC 2045 §6.1
                frame.TransferEncoding = MimeTransferEncoding.SevenBit;
            }

            var enclosing = _frames.Count > 1 ? _frames[_frames.Count - 2] : null;
            frame.IsMime = frame.HasMimeVersion || frame.HasMimeField || (!(enclosing is null) && enclosing.IsMime);
            frame.AllowedDomain = ImfBodyCheck.GetAllowedDomain(frame.TransferEncoding, enclosing is null ? MimeDataDomain.Binary : enclosing.AllowedDomain);

            frame.IsHeaderEnded = true;
            _bodyStartLineBreakLength = _scanner.SectionEndLineBreakLength;
            _atLineStart = true;
            _search = _scanner.Position;
            _lineScan = -1;

            var type = frame.ContentType.Type;
            var subtype = frame.ContentType.Subtype;
            var encoding = frame.TransferEncoding;
            if (EqualsIgnoreCase(type, MimeTopLevelTypes.Multipart))
            {
                CheckCompositeEncoding(frame);
                frame.Boundary = GetBoundary(frame.ContentType);
                frame.IsBoundaryActive = true;
                _activeBoundaryCount++;
                StartContent(ImfEntityEventKind.Preamble, BodyDecoding.None);
                return;
            }

            if (EqualsIgnoreCase(type, MimeTopLevelTypes.Message))
            {
                // ref. RFC 2046 §5.2.2 and §5.2.3: a fragment or a reference is not a whole message, so it is left as it is, and it must be 7bit.
                if (EqualsIgnoreCase(subtype, PartialSubtype) || EqualsIgnoreCase(subtype, ExternalBodySubtype))
                {
                    if (encoding != MimeTransferEncoding.SevenBit)
                        throw BuildError(string.Format(ErrorMessage.SevenBitOnlyTemplate, encoding.Name, frame.ContentType.Type + "/" + subtype));

                    StartContent(ImfEntityEventKind.BodyChunk, BodyDecoding.None);
                    return;
                }

                // ref. RFC 6532 §3.7: unlike the other message types, message/global may have any transfer encoding, so it can only be followed
                // when that leaves the embedded message readable.
                var isGlobal = EqualsIgnoreCase(subtype, GlobalSubtype);
                if (!isGlobal)
                {
                    CheckCompositeEncoding(frame);
                }

                if ((isGlobal && encoding.IsIdentity) || EqualsIgnoreCase(subtype, Rfc822Subtype))
                {
                    _state = State.BeforeEmbeddedMessage;
                    return;
                }
            }

            StartContent(ImfEntityEventKind.BodyChunk, GetBodyDecoding(frame));
        }

        private bool IsDirectPartOfDigest(Frame frame)
        {
            if (frame.IsMessage || _frames.Count < 2) return false;

            var parent = _frames[_frames.Count - 2];
            return EqualsIgnoreCase(parent.ContentType.Type, MimeTopLevelTypes.Multipart) && EqualsIgnoreCase(parent.ContentType.Subtype, DigestSubtype);
        }

        private void CheckCompositeEncoding(Frame frame)
        {
            // ref. RFC 2045 §6.4
            if (!frame.TransferEncoding.IsIdentity)
                throw BuildError(string.Format(ErrorMessage.CompositeEncodingTemplate, frame.TransferEncoding.Name, frame.ContentType.Type + "/" + frame.ContentType.Subtype));
        }

        private byte[] GetBoundary(MimeMediaType contentType)
        {
            MimeParameter parameter;
            if (!contentType.TryGetParameter(BoundaryParameterName, out parameter))
                throw BuildError(ErrorMessage.BoundaryMissing);

            var boundary = parameter.Value;
            if (boundary.Length < 1 || boundary.Length > MaxBoundaryLength || boundary[boundary.Length - 1] == ' ')
                throw BuildError(ErrorMessage.BoundaryInvalid);

            var bytes = new byte[boundary.Length];
            for (var index = 0; index < boundary.Length; index++)
            {
                if (!IsBoundaryCharacter(boundary[index])) throw BuildError(ErrorMessage.BoundaryInvalid);

                bytes[index] = (byte)boundary[index];
            }

            return bytes;
        }

        private BodyDecoding GetBodyDecoding(Frame frame)
        {
            if (!_decodeTransferEncoding) return BodyDecoding.None;

            var encoding = frame.TransferEncoding;
            if (encoding == MimeTransferEncoding.QuotedPrintable) return BodyDecoding.QuotedPrintable;
            if (encoding == MimeTransferEncoding.Base64) return BodyDecoding.Base64;

            if (!encoding.IsKnown)
            {
                _diagnostics.Add(new ImfDiagnostic(
                    ImfDiagnosticKind.UnknownTransferEncoding,
                    string.Format(ErrorMessage.UnknownTransferEncodingTemplate, encoding.Name),
                    _scanner.BytePosition,
                    BuildPath()));
            }

            return BodyDecoding.None;
        }

        private void StartContent(ImfEntityEventKind kind, BodyDecoding decoding)
        {
            var frame = CurrentFrame;
            _bodyCheck.Start(frame.AllowedDomain, frame.IsMime, kind == ImfEntityEventKind.BodyChunk ? ImfBodyCheck.GetMaxLineLength(frame.TransferEncoding) : ImfBodyCheck.MaxLineLength);
            _contentKind = kind;
            _decoding = decoding;
            _decodingFinished = false;
            _pendingRawHeld = 0;
            _state = State.Content;
        }

        /// <summary>
        /// Reads what follows the header of an entity that holds an embedded message: nothing, if a delimiter or the end of the data comes first, or the
        /// embedded message.
        /// </summary>
        /// <returns><c>false</c> if more input is needed.</returns>
        private bool ReadBeforeEmbeddedMessage()
        {
            var position = _scanner.Position;
            if (position == _scanner.BufferEnd)
            {
                if (!_scanner.IsEndOfInput) return false;

                StartUnwinding(false);
                return true;
            }

            // ref. RFC 2046 §5.1.1: the line break of the empty line can be the one that starts the delimiter, leaving the part with no body.
            int resumeAt;
            var match = MatchLine(position, out resumeAt);
            if (match == LineMatch.NeedMoreInput) return false;

            if (match == LineMatch.Delimiter)
            {
                StartUnwinding(true);
                return true;
            }

            // ref. RFC 3501 §6.4.5: a message part and the message in it have the one number, but the body of a message is its part 1.
            PushFrame(CurrentFrame.IsMessage ? 1 : 0, true);
            return true;
        }

        /// <summary>
        /// Starts a child of the current entity at the current position.
        /// </summary>
        private void PushFrame(int pathNumber, bool isMessage)
        {
            var depth = CurrentFrame.Depth + 1;
            if (depth > _maxNestingDepth)
                throw BuildError(string.Format(ErrorMessage.NestedTooDeepTemplate, _maxNestingDepth));

            _partCount++;
            if (_partCount > _maxPartCount)
                throw BuildError(string.Format(ErrorMessage.TooManyPartsTemplate, _maxPartCount));

            _frames.Add(new Frame { Depth = depth, IsMessage = isMessage, PathNumber = pathNumber, Start = _scanner.BytePosition });
            _entitiesRead++;
            _state = State.EntityStart;
        }

        /// <summary>
        /// Handles the line break after a close delimiter, which starts the epilogue unless it starts a delimiter of an enclosing entity.
        /// </summary>
        /// <returns><c>false</c> if more input is needed.</returns>
        private bool ReadEpilogueSeparator()
        {
            var lineStart = _scanner.Position + _closeLineBreakLength;
            if (_closeLineBreakLength > 0)
            {
                int resumeAt;
                var match = MatchLine(lineStart, out resumeAt);
                if (match == LineMatch.NeedMoreInput) return false;

                if (match == LineMatch.Delimiter)
                {
                    StartUnwinding(true);
                    return true;
                }

                _scanner.ConsumeBody(lineStart);
            }

            _search = lineStart;
            _atLineStart = false;
            StartContent(ImfEntityEventKind.Epilogue, BodyDecoding.None);
            return true;
        }

        /// <summary>
        /// Finds how much of the input can be given as content, up to a delimiter or the end of the data.
        /// </summary>
        /// <param name="chunkEnd">For <see cref="ContentResult.Chunk"/>, the index in the block of the end of the chunk.</param>
        private ContentResult ScanContent(out int chunkEnd)
        {
            var position = _scanner.Position;
            var end = _scanner.BufferEnd;
            var isEndOfInput = _scanner.IsEndOfInput;
            chunkEnd = position;

            if (_activeBoundaryCount == 0)
            {
                _bodyStartLineBreakLength = 0;
                if (position == end) return isEndOfInput ? ContentResult.EndOfData : ContentResult.NeedMoreInput;

                chunkEnd = end;
                return ContentResult.Chunk;
            }

            int resumeAt;
            if (_atLineStart)
            {
                // ref. RFC 2046 §5.1.1: a delimiter at the start of the body has no line break of its own.  The first one of a multipart entity needs
                // none, and the one that ends a part with no body shares the line break of the empty line.
                var match = MatchLine(position, out resumeAt);
                if (match == LineMatch.NeedMoreInput) return ContentResult.NeedMoreInput;
                if (match == LineMatch.Delimiter) return ContentResult.Delimiter;

                _atLineStart = false;
                _bodyStartLineBreakLength = 0;
                _search = resumeAt;
            }

            var buffer = _scanner.Buffer;
            while (true)
            {
                var lineFeedOffset = new ReadOnlySpan<byte>(buffer, _search, end - _search).IndexOf(LineFeed);
                if (lineFeedOffset < 0)
                {
                    // A carriage return at the end may be the start of a line break.
                    var safeEnd = end;
                    if (!isEndOfInput && !_lineFeedIsLineBreak && end > position && buffer[end - 1] == CarriageReturn)
                    {
                        safeEnd = end - 1;
                    }

                    _search = safeEnd;
                    if (safeEnd > position)
                    {
                        chunkEnd = safeEnd;
                        return ContentResult.Chunk;
                    }

                    return isEndOfInput ? ContentResult.EndOfData : ContentResult.NeedMoreInput;
                }

                var lineFeed = _search + lineFeedOffset;
                var hasCarriageReturn = !_lineFeedIsLineBreak && lineFeed > position && buffer[lineFeed - 1] == CarriageReturn;
                if (!hasCarriageReturn && !_lineFeedIsLineBreak && !_acceptBareLineFeed)
                {
                    // ref. RFC 2046 §5.1.1: only CRLF starts a delimiter.
                    _search = lineFeed + 1;
                    continue;
                }

                var breakStart = hasCarriageReturn ? lineFeed - 1 : lineFeed;
                var lineMatch = MatchLine(lineFeed + 1, out resumeAt);
                if (lineMatch == LineMatch.NoMatch)
                {
                    _search = Math.Max(resumeAt, lineFeed + 1);
                    continue;
                }

                // The content before the line break comes first.  The next call finds this line again, with its scan of the line kept.
                _search = lineFeed;
                if (breakStart > position)
                {
                    chunkEnd = breakStart;
                    return ContentResult.Chunk;
                }

                return lineMatch == LineMatch.Delimiter ? ContentResult.Delimiter : ContentResult.NeedMoreInput;
            }
        }

        /// <summary>
        /// Looks at the line that starts at an index in the block, to see if it is a delimiter of an open multipart entity.
        /// </summary>
        /// <param name="lineStart">The index in the block of the start of the line.</param>
        /// <param name="resumeAt">For <see cref="LineMatch.NoMatch"/>, the index in the block to go on searching from.</param>
        private LineMatch MatchLine(int lineStart, out int resumeAt)
        {
            resumeAt = lineStart;
            if (_activeBoundaryCount == 0) return LineMatch.NoMatch;

            var buffer = _scanner.Buffer;
            var end = _scanner.BufferEnd;
            var isEndOfInput = _scanner.IsEndOfInput;
            var available = end - lineStart;
            if (available >= 1 && buffer[lineStart] != Hyphen) return LineMatch.NoMatch;
            if (available >= 2 && buffer[lineStart + 1] != Hyphen) return LineMatch.NoMatch;
            if (available < DashLength) return isEndOfInput ? LineMatch.NoMatch : LineMatch.NeedMoreInput;

            // The line break of a line of the longest length must be inside this window, or the line is too long to be a delimiter line.
            var windowLength = MaxLineLength + LineBreakLength;
            var scanFrom = _lineScan >= 0 ? _lineScan : lineStart + DashLength;
            var windowEnd = available >= windowLength ? lineStart + windowLength : end;
            var lineFeedOffset = new ReadOnlySpan<byte>(buffer, scanFrom, windowEnd - scanFrom).IndexOf(LineFeed);
            if (lineFeedOffset < 0)
            {
                if (available >= windowLength)
                {
                    _lineScan = -1;
                    resumeAt = windowEnd;
                    return LineMatch.NoMatch;
                }

                if (!isEndOfInput)
                {
                    _lineScan = windowEnd;
                    return LineMatch.NeedMoreInput;
                }

                // ref. RFC 2046 §5.1.1: only the close delimiter can be the last thing in the data.
                _lineScan = -1;
                resumeAt = end;
                if (available > MaxLineLength) return LineMatch.NoMatch;

                return MatchDelimiter(lineStart, end, -1) ? LineMatch.Delimiter : LineMatch.NoMatch;
            }

            _lineScan = -1;
            var lineFeed = scanFrom + lineFeedOffset;
            resumeAt = lineFeed;

            var contentEnd = lineFeed;
            if (!_lineFeedIsLineBreak)
            {
                if (buffer[lineFeed - 1] == CarriageReturn)
                {
                    contentEnd = lineFeed - 1;
                }
                else if (!_acceptBareLineFeed)
                {
                    return LineMatch.NoMatch;
                }
            }

            if (contentEnd - lineStart > MaxLineLength) return LineMatch.NoMatch;

            return MatchDelimiter(lineStart, contentEnd, lineFeed + 1) ? LineMatch.Delimiter : LineMatch.NoMatch;
        }

        /// <summary>
        /// Compares a whole line with the open boundaries, the innermost first.
        /// </summary>
        /// <param name="lineStart">The index in the block of the start of the line.</param>
        /// <param name="contentEnd">The index in the block of the end of the line, without its line break.</param>
        /// <param name="lineEnd">The index in the block of the end of the line break, or -1 if the line ends at the end of the data.</param>
        private bool MatchDelimiter(int lineStart, int contentEnd, int lineEnd)
        {
            var buffer = _scanner.Buffer;

            // ref. RFC 2046 §5.1.1: transport-padding := *LWSP-char
            var tokenEnd = contentEnd;
            while (tokenEnd > lineStart && (buffer[tokenEnd - 1] == Space || buffer[tokenEnd - 1] == Tab))
            {
                tokenEnd--;
            }

            var tokenStart = lineStart + DashLength;
            if (tokenEnd <= tokenStart) return false;

            var token = new ReadOnlySpan<byte>(buffer, tokenStart, tokenEnd - tokenStart);
            for (var index = _frames.Count - 1; index >= 0; index--)
            {
                var frame = _frames[index];
                if (!frame.IsBoundaryActive) continue;

                var isOpen = lineEnd >= 0 && token.SequenceEqual(frame.Boundary);
                var isClose = !isOpen
                    && token.Length == frame.Boundary.Length + DashLength
                    && token.Slice(0, frame.Boundary.Length).SequenceEqual(frame.Boundary)
                    && token[token.Length - 1] == Hyphen
                    && token[token.Length - 2] == Hyphen;

                if (!isOpen && !isClose) continue;

                _matchFrameIndex = index;
                _matchIsClose = isClose;
                _matchContentEnd = contentEnd;
                _matchLineEnd = lineEnd;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gives the content up to an index in the block: as it is, or to the decoder.
        /// </summary>
        /// <returns><c>true</c> if there is an event.</returns>
        private bool ReadChunk(int chunkEnd)
        {
            var position = _scanner.Position;
            var offset = _scanner.BytePosition;
            var length = chunkEnd - position;
            _bodyCheck.Check(new ReadOnlySpan<byte>(_scanner.Buffer, position, length), out var foundLongLine, out var foundInvalidOctet, out var longLineOffset, out var invalidOctetOffset);
            ReportBodyProblems(foundLongLine, foundInvalidOctet, longLineOffset, invalidOctetOffset);
            if (_decoding != BodyDecoding.None)
            {
                _pendingRawLength = length;
                return DecodePending();
            }

            _scanner.ConsumeBody(chunkEnd);
            _eventData = _scanner.Buffer;
            _dataStart = position;
            _dataLength = length;
            RecordBodyRange(offset, length);
            Emit(_contentKind, offset, length);
            return true;
        }

        /// <summary>
        /// Decodes as much as one event can hold of the raw bytes that have been found to be content.  They stay in the block, as it is refilled only once
        /// they are all done, and are consumed as they are decoded, so that an error is reported at the byte and line of the problem.
        /// </summary>
        /// <returns><c>true</c> if there is an event.</returns>
        private bool DecodePending()
        {
            EnsureDecodeBuffer(_pendingRawLength);

            var source = new ReadOnlySpan<byte>(_scanner.Buffer, _scanner.Position, _pendingRawLength);
            int bytesConsumed, bytesWritten;
            var status = Decode(source, new Span<byte>(_decodeBuffer), out bytesConsumed, out bytesWritten, false);
            _scanner.ConsumeBody(_scanner.Position + bytesConsumed);
            if (status == OperationStatus.InvalidData) throw BuildDecodingError();

            _pendingRawLength -= bytesConsumed;
            _pendingRawHeld += bytesConsumed;
            if (bytesWritten == 0) return false;

            EmitDecoded(bytesWritten);
            return true;
        }

        /// <summary>
        /// Takes from the decoder what it was holding, at the end of a body.  Raw bytes that gave no output, such as a last line break, still get an event,
        /// so that the chunks of a body cover all of its raw bytes.
        /// </summary>
        /// <returns><c>true</c> if there is an event.</returns>
        private bool FinishDecoding()
        {
            if (_decoding == BodyDecoding.None || _decodingFinished) return false;

            EnsureDecodeBuffer(0);

            int bytesConsumed, bytesWritten;
            var status = Decode(ReadOnlySpan<byte>.Empty, new Span<byte>(_decodeBuffer), out bytesConsumed, out bytesWritten, true);
            if (status == OperationStatus.InvalidData) throw BuildDecodingError();

            _decodingFinished = status == OperationStatus.Done;
            if (bytesWritten == 0 && _pendingRawHeld == 0) return false;

            EmitDecoded(bytesWritten);
            return true;
        }

        private void EmitDecoded(int bytesWritten)
        {
            _eventData = _decodeBuffer;
            _dataStart = 0;
            _dataLength = bytesWritten;
            _isEventDataDecoded = true;
            RecordBodyRange(_scanner.BytePosition - _pendingRawHeld, _pendingRawHeld);
            Emit(_contentKind, _scanner.BytePosition - _pendingRawHeld, _pendingRawHeld);
            _pendingRawHeld = 0;
        }

        private OperationStatus Decode(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock)
        {
            return _decoding == BodyDecoding.Base64
                ? _base64Decoder.Decode(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock)
                : _quotedPrintableDecoder.Decode(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);
        }

        private void EnsureDecodeBuffer(int sourceLength)
        {
            var length = Math.Max(sourceLength, MinDecodeBufferLength);
            if (_decodeBuffer.Length >= length) return;

            if (_decodeBuffer.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(_decodeBuffer);
            }

            _decodeBuffer = ArrayPool<byte>.Shared.Rent(length);
        }

        private ImfFileException BuildDecodingError()
        {
            return _scanner.BuildPositionException(_decoding == BodyDecoding.Base64 ? ErrorMessage.InvalidBase64 : ErrorMessage.InvalidQuotedPrintable);
        }

        /// <summary>
        /// Ends the content of the current entity, and the entities that end with it.
        /// </summary>
        /// <param name="isDelimiter"><c>true</c> if a delimiter ended it, which <see cref="MatchDelimiter"/> has described; <c>false</c> if the data
        /// ended.</param>
        private void StartUnwinding(bool isDelimiter)
        {
            _unwindIsDelimiter = isDelimiter;

            var lowestFrame = isDelimiter ? _matchFrameIndex + 1 : 0;
            if (!_allowMissingBoundaryTerminator)
            {
                for (var index = lowestFrame; index < _frames.Count; index++)
                {
                    if (_frames[index].IsBoundaryActive) throw BuildError(ErrorMessage.MissingCloseDelimiter);
                }
            }

            if (isDelimiter && _matchIsClose && _frames[_matchFrameIndex].PartCount == 0)
                throw BuildError(ErrorMessage.CloseWithoutPart);

            // ref. RFC 2046 §5.1.1: the line break of an empty line that a delimiter follows belongs to the delimiter, not to the entity.
            _unwindEnd = _scanner.BytePosition - (isDelimiter ? _bodyStartLineBreakLength : 0);
            _bodyStartLineBreakLength = 0;
            _state = State.Unwinding;
        }

        /// <summary>
        /// Goes on from a delimiter, once the entities inside the multipart entity it belongs to have ended.
        /// </summary>
        private void FollowDelimiter()
        {
            var frame = CurrentFrame;
            if (_matchIsClose)
            {
                frame.IsBoundaryActive = false;
                _activeBoundaryCount--;
                _closeLineBreakLength = _matchLineEnd < 0 ? 0 : _matchLineEnd - _matchContentEnd;
                _scanner.ConsumeBody(_matchContentEnd);
                _state = State.EpilogueSeparator;
                return;
            }

            _scanner.ConsumeBody(_matchLineEnd);
            frame.PartCount++;
            PushFrame(frame.PartCount, false);
        }

        /// <summary>
        /// Reports what a check of the bytes at the current position found, at the offsets from there that it gives.  An error consumes the bytes before the problem, so that it is reported at
        /// the byte and line of the problem, as reading ends there.
        /// </summary>
        private void ReportBodyProblems(bool foundLongLine, bool foundInvalidOctet, int longLineOffset, int invalidOctetOffset)
        {
            if (foundLongLine)
            {
                if (!_allowLongLines)
                {
                    _scanner.ConsumeBody(_scanner.Position + longLineOffset);
                    throw BuildKindError(ImfBodyCheck.LongLineMessage, ImfDiagnosticKind.LongBodyLine);
                }

                _diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.LongBodyLine, ImfBodyCheck.LongLineMessage, _scanner.BytePosition + longLineOffset, BuildPath()));
            }

            if (foundInvalidOctet)
            {
                if (!_allowInvalidBodyOctets)
                {
                    _scanner.ConsumeBody(_scanner.Position + invalidOctetOffset);
                    throw BuildKindError(ImfBodyCheck.InvalidOctetsMessage, ImfDiagnosticKind.InvalidBodyOctets);
                }

                _diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.InvalidBodyOctets, ImfBodyCheck.InvalidOctetsMessage, _scanner.BytePosition + invalidOctetOffset, BuildPath()));
            }
        }

        private ImfFileException BuildKindError(string message, ImfDiagnosticKind kind)
        {
            var exception = BuildError(message);
            exception.Data[ImfFileException.DataItemName.DiagnosticKind] = kind;
            return exception;
        }

        private ImfFileException BuildError(string message)
        {
            return _scanner.BuildPositionException(message);
        }

        /// <summary>
        /// Builds the path of the current entity from those of the entities that hold it.
        /// </summary>
        private string BuildPath()
        {
            var path = new StringBuilder();
            foreach (var frame in _frames)
            {
                if (frame.PathNumber == 0) continue;

                if (path.Length > 0)
                {
                    path.Append('.');
                }

                path.Append(frame.PathNumber);
            }

            return path.ToString();
        }

        private void CheckHeaderEnded()
        {
            if (!CurrentFrame.IsHeaderEnded)
                throw new InvalidOperationException("The entity's header section has not ended yet.");
        }

        private static bool IsBoundaryCharacter(char value)
        {
            // ref. RFC 2046 §5.1.1: bchars
            return (value >= '0' && value <= '9')
                || (value >= 'A' && value <= 'Z')
                || (value >= 'a' && value <= 'z')
                || value == '\'' || value == '(' || value == ')' || value == '+' || value == '_' || value == ','
                || value == '-' || value == '.' || value == '/' || value == ':' || value == '=' || value == '?' || value == ' ';
        }

        private static bool EqualsIgnoreCase(ReadOnlySpan<byte> bytes, string lowerCaseName)
        {
            if (bytes.Length != lowerCaseName.Length) return false;

            for (var index = 0; index < bytes.Length; index++)
            {
                var value = bytes[index];
                if (value >= (byte)'A' && value <= (byte)'Z')
                {
                    value = (byte)(value + ('a' - 'A'));
                }

                if (value != lowerCaseName[index]) return false;
            }

            return true;
        }

        private static bool EqualsIgnoreCase(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
