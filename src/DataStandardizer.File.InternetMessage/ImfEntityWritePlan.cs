using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// One run of the output of an <see cref="ImfEntityWriter"/>.
    /// </summary>
    internal readonly struct ImfWriteItem
    {
        internal ImfWriteItem(ReadOnlyMemory<byte> bytes, Stream stream, long startPosition,
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            ImfBodyEncoder? encoder,
#else
            ImfBodyEncoder encoder,
#endif
            bool isContent, int fieldsWritten, int entitiesWritten)
        {
            Bytes = bytes;
            Stream = stream;
            StartPosition = startPosition;
            Encoder = encoder;
            IsContent = isContent;
            FieldsWritten = fieldsWritten;
            EntitiesWritten = entitiesWritten;
        }

        internal ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// Gets the stream of the bytes, or <see cref="Stream.Null"/> if they are in <see cref="Bytes"/>.
        /// </summary>
        internal Stream Stream { get; }

        internal long StartPosition { get; }

        /// <summary>
        /// Gets the encoder of the bytes, or <c>null</c> to write them as they are.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal ImfBodyEncoder? Encoder { get; }
#else
        internal ImfBodyEncoder Encoder { get; }
#endif

        /// <summary>
        /// Gets a flag indicating that the bytes are read in blocks.
        /// </summary>
        internal bool IsContent { get; }

        /// <summary>
        /// Gets the number of fields that are complete once this item has been written.
        /// </summary>
        internal int FieldsWritten { get; }

        /// <summary>
        /// Gets the number of entities that are complete once this item has been written.
        /// </summary>
        internal int EntitiesWritten { get; }
    }

    /// <summary>
    /// What an <see cref="ImfEntityWriter"/> works out before it writes anything, so that a message it refuses leaves nothing in the output.
    /// </summary>
    /// <remarks>
    /// The writer's sync and async paths both read <see cref="PendingScans"/> into their <see cref="ImfBodyScan"/> objects and call
    /// <see cref="CompletePass"/>, until that says it is done.  After that, <see cref="BuildItems"/> gives the output.
    /// </remarks>
    internal sealed class ImfEntityWritePlan
    {
        private const string BoundaryParameterName = "boundary";
        private const byte CarriageReturn = 0x0D;
        private const byte Colon = 0x3A;
        private const byte LineFeed = 0x0A;
        private const string ContentTransferEncodingFieldName = "Content-Transfer-Encoding";
        private const string ContentTypeFieldName = "Content-Type";
        private const string EntityPathOfMessage = "";
        private const int MaxBoundaryAttempts = 16;

        // ref. RFC 2046 §5.1.1: boundary := 0*69<bchars> bcharsnospace
        private const int MaxBoundaryLength = 70;
        private const string MimeVersionFieldName = "MIME-Version";
        private const string MimeVersionValue = "1.0";

        private static readonly byte[] CrLf = { 0x0D, 0x0A };
        private static readonly byte[] DelimiterDashes = { 0x2D, 0x2D };
        private static readonly byte[] Lf = { 0x0A };
        private static readonly ImfBoundary[] NoBoundaries = new ImfBoundary[0];
        private static readonly byte[] NoBytes = new byte[0];

        private readonly List<ImfBoundary> _boundaries = new List<ImfBoundary>();
        private readonly Func<string> _boundaryGenerator;
        private readonly bool _lineFeedIsLineBreak;
        private readonly byte[] _lineBreak;
        private readonly ImfOptions _options;
        private readonly Node _root;
        private readonly List<ImfBodyScan> _scans = new List<ImfBodyScan>();
        private readonly List<Node> _nodes = new List<Node>();
        private bool _isFirstPass = true;
        private int _partCount;

        internal ImfEntityWritePlan(ImfEntitySource source, ImfOptions options, bool lineFeedIsLineBreak, Func<string> boundaryGenerator)
        {
            _options = options;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
            _lineBreak = lineFeedIsLineBreak ? Lf : CrLf;
            _boundaryGenerator = boundaryGenerator;
            _root = Walk(source, EntityPathOfMessage, 0, true, NoBoundaries);
            ResolveBoundaryConflicts();
            PendingScans = new List<ImfBodyScan>(_scans);
        }

        /// <summary>
        /// Gets the scans to read in this pass.  Each is read between <see cref="BeginScan"/> and <see cref="ImfBodyScan.Complete"/>.
        /// </summary>
        internal List<ImfBodyScan> PendingScans { get; private set; }

        /// <summary>
        /// Prepares a scan to be read in this pass.
        /// </summary>
        internal void BeginScan(ImfBodyScan scan)
        {
            scan.Begin(!_isFirstPass && scan.Check == ImfBodyScan.CheckKind.Output);
        }

        /// <summary>
        /// Acts on what the pending scans found.
        /// </summary>
        /// <returns><c>true</c> if there are scans to read again, in another pass.</returns>
        /// <exception cref="ImfFileException">The entity can't be written.</exception>
        internal bool CompletePass()
        {
            var wasFirstPass = _isFirstPass;
            if (wasFirstPass)
            {
                _isFirstPass = false;
                ChooseEncodings(_root);
                SetBodyChecks();
            }

            var hasReplaced = false;
            foreach (var scan in PendingScans)
            {
                // The first pass looks at the bytes, which say nothing about the lines of an encoded body.
                if (wasFirstPass && scan.Check != ImfBodyScan.CheckKind.Bytes) continue;

                foreach (var boundary in scan.Collisions)
                {
                    if (!boundary.IsGenerated)
                        throw new ImfFileException($"The boundary '{boundary.Value}' appears at the start of a line of the content it encloses (RFC 2046 §5.1.1).");

                    ReplaceBoundary(boundary);
                    hasReplaced = true;
                }
            }

            var nextScans = new List<ImfBodyScan>();
            foreach (var scan in _scans)
            {
                if ((scan.Check == ImfBodyScan.CheckKind.Bytes && hasReplaced) || (scan.Check == ImfBodyScan.CheckKind.Output && wasFirstPass))
                {
                    nextScans.Add(scan);
                }
            }

            PendingScans = nextScans;
            return nextScans.Count > 0;
        }

        /// <summary>
        /// Gets the output, in order.
        /// </summary>
        internal List<ImfWriteItem> BuildItems()
        {
            var builder = new ItemBuilder();
            AddEntity(_root, builder);
            builder.Flush();
            return builder.Items;
        }

        private Node Walk(ImfEntitySource source, string path, int depth, bool isMessage, ImfBoundary[] enclosing)
        {
            if (depth > _options.MaxNestingDepth)
                throw BuildError($"The entities are nested more than {_options.MaxNestingDepth} levels deep, the most that ImfOptions.MaxNestingDepth allows.", path);

            if (depth > 0)
            {
                _partCount++;
                if (_partCount > _options.MaxPartCount)
                    throw BuildError($"The message has more than {_options.MaxPartCount} entities, the most that ImfOptions.MaxPartCount allows.", path);
            }

            var node = new Node(source, path, isMessage);
            _nodes.Add(node);

            if (source.Kind == ImfEntitySource.SourceKind.Raw)
            {
                AddRawScan(node, depth, enclosing);
                return node;
            }

            node.RequestedEncoding = source.TransferEncoding;
            FormatFields(node);
            if (depth > 0)
            {
                node.HeaderScan = AddScan(node.HeaderBytes, Stream.Null, enclosing);
            }

            switch (source.Kind)
            {
                case ImfEntitySource.SourceKind.Content:
                    AddContentScan(node, enclosing);
                    break;
                case ImfEntitySource.SourceKind.Multipart:
                    WalkMultipart(node, depth, enclosing);
                    break;
                default:
                    // ref. RFC 3501 §6.4.5: a message part and the message in it have the one number, but the body of a message is its part 1.
                    node.Children.Add(Walk(source.Children[0], !isMessage ? path : path.Length == 0 ? "1" : path + ".1", depth + 1, true, enclosing));
                    break;
            }

            return node;
        }

        private void WalkMultipart(Node node, int depth, ImfBoundary[] enclosing)
        {
            var source = node.Source;
            MimeParameter parameter;
            var boundary = source.ContentType.TryGetParameter(BoundaryParameterName, out parameter)
                ? new ImfBoundary(parameter.Value, false, enclosing)
                : new ImfBoundary(string.Empty, true, enclosing);
            if (!boundary.IsGenerated && !IsValidBoundary(boundary.Value))
                throw BuildError($"The boundary '{boundary.Value}' is not valid: it must be 1 to 70 of the characters RFC 2046 §5.1.1 allows, and not end in a space.", node.Path);

            if (boundary.IsGenerated && node.HasContentTypeField)
                throw BuildError("A Content-Type field added to a multipart entity must have a boundary parameter (RFC 2046 §5.1.1).", node.Path);

            boundary.IsPreserved = !(source.RawFraming is null);
            _boundaries.Add(boundary);
            node.Boundary = boundary;

            var inner = new ImfBoundary[enclosing.Length + 1];
            Array.Copy(enclosing, inner, enclosing.Length);
            inner[enclosing.Length] = boundary;

            if (source.Preamble is ReadOnlyMemory<byte> preamble)
            {
                node.PreambleScan = AddScan(preamble, Stream.Null, inner);
            }

            for (var index = 0; index < source.Children.Count; index++)
            {
                var number = (index + 1).ToString(CultureInfo.InvariantCulture);
                node.Children.Add(Walk(source.Children[index], node.Path.Length == 0 ? number : node.Path + "." + number, depth + 1, false, inner));
            }

            if (source.Epilogue is ReadOnlyMemory<byte> epilogue)
            {
                node.EpilogueScan = AddScan(epilogue, Stream.Null, enclosing);
            }
        }

        private void AddRawScan(Node node, int depth, ImfBoundary[] enclosing)
        {
            // A whole message written from its raw bytes has nothing to be checked against, and nothing depends on its domain.
            if (depth == 0) return;

            CheckCanRead(node.Source, node.Path);
            node.BodyScan = AddScan(node.Source.Content, node.Source.ContentStream, node.Source.IsPreserved ? WithoutPreserved(enclosing) : enclosing);
        }

        private void AddContentScan(Node node, ImfBoundary[] enclosing)
        {
            var source = node.Source;
            var explicitEncoding = node.RequestedEncoding;
            var isExplicit = explicitEncoding != default(MimeTransferEncoding);
            if (isExplicit && !explicitEncoding.IsKnown)
                throw BuildError($"The transfer encoding '{explicitEncoding.Name}' is an extension mechanism, which can't be applied here.  Write the entity from its raw bytes.", node.Path);

            if (IsCompositeContentType(source.ContentType) && isExplicit && !explicitEncoding.IsIdentity)
                throw BuildCompositeEncodingError(node);

            var isStream = !ReferenceEquals(source.ContentStream, Stream.Null);
            if (isStream && !source.ContentStream.CanSeek)
            {
                // ref. RFC 2045 §6.7 and §6.8: the output of these can't hold a delimiter, so nothing depends on seeing the content first.
                var isSafe = explicitEncoding == MimeTransferEncoding.Base64
                    || (explicitEncoding == MimeTransferEncoding.QuotedPrintable && AllAreQuotedPrintableSafe(enclosing));
                if (!isSafe)
                    throw BuildError("A stream that doesn't support seeking can't be checked before it is written, so its entity must have a TransferEncoding of base64, or of quoted-printable with every enclosing boundary holding \"=_\".", node.Path);

                node.Encoding = explicitEncoding;
                node.UsesUnscannedStream = true;
                return;
            }

            if (explicitEncoding == MimeTransferEncoding.Base64)
            {
                // A base64 body needs no look: it can't hold a delimiter, and it encodes any data.
                node.Encoding = explicitEncoding;
                node.UsesUnscannedStream = isStream;
                return;
            }

            node.BodyScan = AddScan(source.Content, source.ContentStream, enclosing);
        }

        /// <summary>
        /// Gets the boundaries that bytes that were read have to be checked against: a reader found none of the others in them.
        /// </summary>
        private static ImfBoundary[] WithoutPreserved(ImfBoundary[] boundaries)
        {
            return new List<ImfBoundary>(boundaries).FindAll(boundary => !boundary.IsPreserved).ToArray();
        }

        private ImfBodyScan AddScan(ReadOnlyMemory<byte> memory, Stream stream, ImfBoundary[] enclosing)
        {
            var scan = new ImfBodyScan(memory, stream, enclosing, _lineFeedIsLineBreak);
            _scans.Add(scan);
            return scan;
        }

        private void CheckCanRead(ImfEntitySource source, string path)
        {
            if (!source.ContentStream.CanSeek)
                throw BuildError("A stream that doesn't support seeking can't be checked before it is written, so here it must support seeking.", path);
        }

        private void FormatFields(Node node)
        {
            var output = new MemoryStream();
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                foreach (var field in node.Source.Fields)
                {
                    ReadOnlySpan<byte> name;
                    if (field.IsRaw)
                    {
                        try
                        {
                            if (field.NeedsCheck) ImfHeaderFormatter.CheckRawField(field.Value, _lineFeedIsLineBreak, _options.AcceptBareLineFeed);
                            name = ImfHeaderFormatter.GetRawFieldName(field.Value);
                        }
                        catch (ImfFileException exception)
                        {
                            exception.Data[ImfFileException.DataItemName.EntityPath] = node.Path;
                            throw;
                        }

                        output.Write(field.Value, 0, field.Value.Length);
                    }
                    else
                    {
                        name = field.Name;
                        int length;
                        try
                        {
                            var value = field.IsText
                                ? ImfHeaderFormatter.GetTextBody(name, Encoding.UTF8.GetString(field.Value, 0, field.Value.Length), _options.HeaderEncoding)
                                : field.Value;
                            length = ImfHeaderFormatter.Format(name, value, field.Kind, _options.HeaderEncoding, _lineBreak, ref buffer);
                        }
                        catch (ImfFileException exception)
                        {
                            exception.Data[ImfFileException.DataItemName.EntityPath] = node.Path;
                            throw;
                        }

                        output.Write(buffer, 0, length);
                    }

                    if (EqualsIgnoreCase(name, ContentTypeFieldName)) ReadContentTypeField(node, field);
                    else if (EqualsIgnoreCase(name, ContentTransferEncodingFieldName)) ReadTransferEncodingField(node, field);
                    else if (EqualsIgnoreCase(name, MimeVersionFieldName)) node.HasMimeVersion = true;

                    node.FieldCount++;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            node.HeaderBytes = output.ToArray();
        }

        /// <summary>
        /// Takes note of a Content-Type field the caller added, which is written in place of the one the writer would add, so it must say the same.
        /// </summary>
        private void ReadContentTypeField(Node node, ImfEntitySource.FieldEntry field)
        {
            if (node.HasContentTypeField)
                throw BuildError("An entity must not have two Content-Type fields (RFC 2045 §5).", node.Path);

            string text;
            var mediaType = default(MimeMediaType);
            var isValid = ImfFieldComments.TryRemove(GetFieldBodyText(field), out text)
                && MimeMediaType.TryParse(text, ImfMimeFields.GetParseOptions(_options) | MimeMediaTypeParseOptions.AllowWhitespaceAroundDelimiters, _options.CharsetResolver ?? MimeCharsetResolver.Default, out mediaType);
            if (field.IsModelField)
            {
                // The object model reads parameters more closely than this, so only what decides how the entity is written has to agree.
                if (!isValid || !IsSameStructure(mediaType, node.Source.ContentType))
                    throw BuildError($"The Content-Type field of the entity is not valid, or does not say the same as the media type it is being written as, '{node.Source.ContentType}' (RFC 2045 §5.1).", node.Path);

                node.HasContentTypeField = true;
                return;
            }

            if (!isValid || mediaType != node.Source.ContentType)
                throw BuildError($"A Content-Type field added to an entity must say the same as the media type the entity was made with, '{node.Source.ContentType}'.", node.Path);

            node.HasContentTypeField = true;
        }

        /// <summary>
        /// Takes note of a Content-Transfer-Encoding field the caller added, which is written in place of the one the writer would add, and which asks for
        /// that encoding.
        /// </summary>
        private void ReadTransferEncodingField(Node node, ImfEntitySource.FieldEntry field)
        {
            if (node.HasTransferEncodingField)
                throw BuildError("An entity must not have two Content-Transfer-Encoding fields (RFC 2045 §6).", node.Path);

            string text;
            MimeTransferEncoding encoding;
            if (!ImfFieldComments.TryRemove(GetFieldBodyText(field), out text) || !MimeTransferEncoding.TryParse(text.Trim(' ', '\t'), out encoding)
                || (node.Source.TransferEncoding != default(MimeTransferEncoding) && node.Source.TransferEncoding != encoding))
                throw BuildError("A Content-Transfer-Encoding field added to an entity must be valid, and say the same as its TransferEncoding.", node.Path);

            node.HasTransferEncodingField = true;
            node.RequestedEncoding = encoding;
        }

        private static bool IsSameStructure(MimeMediaType left, MimeMediaType right)
        {
            MimeParameter leftBoundary, rightBoundary;
            var hasLeftBoundary = left.TryGetParameter(BoundaryParameterName, out leftBoundary);
            var hasRightBoundary = right.TryGetParameter(BoundaryParameterName, out rightBoundary);
            return string.Equals(left.Type, right.Type, StringComparison.OrdinalIgnoreCase)
                && string.Equals(left.Subtype, right.Subtype, StringComparison.OrdinalIgnoreCase)
                && hasLeftBoundary == hasRightBoundary
                && (!hasLeftBoundary || leftBoundary.Value == rightBoundary.Value);
        }

        private static string GetFieldBodyText(ImfEntitySource.FieldEntry field)
        {
            var bytes = field.Value;
            var start = field.IsRaw ? Array.IndexOf(bytes, Colon) + 1 : 0;
            var text = new StringBuilder(bytes.Length - start);
            for (var index = start; index < bytes.Length; index++)
            {
                if (bytes[index] != CarriageReturn && bytes[index] != LineFeed) text.Append((char)bytes[index]);
            }

            return text.ToString();
        }

        /// <summary>
        /// Generates the boundaries that have not been given, and keeps each boundary from being a prefix of one around it.
        /// </summary>
        private void ResolveBoundaryConflicts()
        {
            foreach (var boundary in _boundaries)
            {
                if (boundary.IsGenerated) ReplaceBoundary(boundary);
            }

            foreach (var boundary in _boundaries)
            {
                foreach (var enclosing in boundary.Enclosing)
                {
                    // ref. RFC 2046 §5.1.1: a reader matches a whole line, so a boundary that was read may be a prefix of another that was.
                    if (!boundary.ConflictsWith(enclosing.Value) || (boundary.IsPreserved && enclosing.IsPreserved)) continue;

                    if (boundary.IsGenerated)
                    {
                        ReplaceBoundary(boundary);
                    }
                    else if (enclosing.IsGenerated)
                    {
                        ReplaceBoundary(enclosing);
                    }
                    else
                    {
                        throw new ImfFileException($"The boundaries '{enclosing.Value}' and '{boundary.Value}' can't be nested, as a delimiter of one starts with the other (RFC 2046 §5.1.1).");
                    }
                }
            }
        }

        /// <summary>
        /// Generates a new value for a boundary, unlike each boundary around it or inside it.  A sibling doesn't matter, as neither encloses the other.
        /// </summary>
        private void ReplaceBoundary(ImfBoundary boundary)
        {
            var previous = boundary.Value;
            for (var attempt = 0; attempt < MaxBoundaryAttempts; attempt++)
            {
                var candidate = _boundaryGenerator();
                var isUsable = IsValidBoundary(candidate) && candidate != previous;
                foreach (var other in _boundaries)
                {
                    if (other.Value.Length > 0 && (Encloses(other, boundary) || Encloses(boundary, other)) && other.ConflictsWith(candidate)) isUsable = false;
                }

                if (isUsable)
                {
                    boundary.Value = candidate;
                    return;
                }
            }

            throw new ImfFileException($"No usable boundary was generated in {MaxBoundaryAttempts} attempts.");
        }

        private static bool Encloses(ImfBoundary outer, ImfBoundary inner)
        {
            return Array.IndexOf(inner.Enclosing, outer) >= 0;
        }

        /// <summary>
        /// Chooses the transfer encoding of each body from what the scans found, and works out the domain of each entity.
        /// </summary>
        /// <returns>The domain of the whole entity: its header, and its body as it will be written.</returns>
        private MimeDataDomain ChooseEncodings(Node node)
        {
            if (node.Source.Kind == ImfEntitySource.SourceKind.Raw)
            {
                return node.BodyScan?.Domain ?? MimeDataDomain.Binary;
            }

            MimeDataDomain bodyDomain;
            switch (node.Source.Kind)
            {
                case ImfEntitySource.SourceKind.Content:
                    bodyDomain = ChooseContentEncoding(node);
                    break;
                default:
                    bodyDomain = ChooseCompositeEncoding(node);
                    break;
            }

            return Max(bodyDomain, node.HeaderScan?.Domain ?? MimeDataDomain.SevenBit);
        }

        private MimeDataDomain ChooseContentEncoding(Node node)
        {
            if (node.BodyScan is null)
            {
                return MimeDataDomain.SevenBit;
            }

            var scan = node.BodyScan;
            var source = node.Source;
            var domain = scan.Domain;
            var requested = node.RequestedEncoding;
            var isExplicit = requested != default(MimeTransferEncoding);
            node.EncodeLineBreaks = scan.HasBareLineBreak || !IsTextType(source.ContentType);
            if (isExplicit)
            {
                if (requested.IsIdentity && requested.Domain < domain)
                    throw BuildError($"The content does not fit the transfer encoding '{requested.Name}' (RFC 2045 §2.7 and §2.8): its lines must be at most 998 octets with CRLF line breaks, and it must have no NULs.  7bit must also have no octets above 127.", node.Path);

                CheckRestrictedType(node, requested.Domain);
                node.Encoding = requested;
                return requested.Domain;
            }

            if (domain == MimeDataDomain.SevenBit)
            {
                node.Encoding = MimeTransferEncoding.SevenBit;
                return domain;
            }

            if (domain == MimeDataDomain.EightBit && (_options.AllowEightBitTransferEncoding || _options.AllowBinaryTransferEncoding))
            {
                CheckRestrictedType(node, domain);
                node.Encoding = _options.AllowEightBitTransferEncoding ? MimeTransferEncoding.EightBit : MimeTransferEncoding.Binary;
                return node.Encoding.Domain;
            }

            if (_options.AllowBinaryTransferEncoding)
            {
                CheckRestrictedType(node, domain);
                node.Encoding = MimeTransferEncoding.Binary;
                return domain;
            }

            if (IsCompositeContentType(source.ContentType))
                throw BuildCompositeEncodingError(node);

            node.Encoding = scan.GetQuotedPrintableLength(node.EncodeLineBreaks) <= scan.Base64Length
                ? MimeTransferEncoding.QuotedPrintable
                : MimeTransferEncoding.Base64;
            return MimeDataDomain.SevenBit;
        }

        private MimeDataDomain ChooseCompositeEncoding(Node node)
        {
            // What was read and hasn't changed stays as it is, even if its label doesn't cover it, which is common, so only what has changed can call for another label.
            var isPreserved = node.Source.IsPreserved;
            var domain = MimeDataDomain.SevenBit;
            foreach (var child in node.Children)
            {
                var childDomain = ChooseEncodings(child);
                if (!isPreserved || !child.Source.IsPreserved || child.Source.Kind != ImfEntitySource.SourceKind.Raw) domain = Max(domain, childDomain);
            }

            domain = Max(domain, CheckDiscardText(node, node.PreambleScan));
            domain = Max(domain, CheckDiscardText(node, node.EpilogueScan));

            var transferEncoding = node.RequestedEncoding;
            if (transferEncoding != default(MimeTransferEncoding))
            {
                // ref. RFC 2045 §6.4: a composite entity has no transfer encoding of its own, only a label for what it holds.
                if (!transferEncoding.IsIdentity)
                    throw BuildCompositeEncodingError(node);
                if (transferEncoding.Domain < domain)
                    throw BuildError($"The transfer encoding '{transferEncoding.Name}' does not cover what the entity holds (RFC 2045 §6.4).", node.Path);

                node.Encoding = transferEncoding;
                return transferEncoding.Domain;
            }

            node.Encoding = domain == MimeDataDomain.Binary ? MimeTransferEncoding.Binary
                : domain == MimeDataDomain.EightBit ? MimeTransferEncoding.EightBit
                : MimeTransferEncoding.SevenBit;
            return domain;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private MimeDataDomain CheckDiscardText(Node node, ImfBodyScan? scan)
#else
        private MimeDataDomain CheckDiscardText(Node node, ImfBodyScan scan)
#endif
        {
            if (scan is null) return MimeDataDomain.SevenBit;

            var domain = scan.Domain;
            if (domain == MimeDataDomain.Binary ? !_options.AllowBinaryTransferEncoding
                : domain == MimeDataDomain.EightBit && !_options.AllowEightBitTransferEncoding && !_options.AllowBinaryTransferEncoding)
                throw BuildError("A preamble or epilogue must be lines of text (RFC 2046 §5.1.1), which the options must allow if they have octets above 127.", node.Path);

            return domain;
        }

        private void CheckRestrictedType(Node node, MimeDataDomain domain)
        {
            // ref. RFC 2046 §5.2.2 and §5.2.3: these must be 7bit.
            var contentType = node.Source.ContentType;
            if (domain != MimeDataDomain.SevenBit && string.Equals(contentType.Type, MimeTopLevelTypes.Message, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(contentType.Subtype, "partial", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(contentType.Subtype, "external-body", StringComparison.OrdinalIgnoreCase)))
                throw BuildError($"A message/{contentType.Subtype} entity must be 7bit (RFC 2046 §5.2).", node.Path);
        }

        /// <summary>
        /// Says how each body is to be checked against the boundaries around it, now that its encoding is known.
        /// </summary>
        private void SetBodyChecks()
        {
            foreach (var node in _nodes)
            {
                var scan = node.BodyScan;
                if (scan is null || node.Source.Kind != ImfEntitySource.SourceKind.Content) continue;

                if (node.Encoding == MimeTransferEncoding.Base64)
                {
                    scan.Check = ImfBodyScan.CheckKind.None;
                }
                else if (node.Encoding == MimeTransferEncoding.QuotedPrintable)
                {
                    var isSafe = true;
                    foreach (var boundary in scan.Enclosing) isSafe &= boundary.IsQuotedPrintableSafe;

                    scan.Check = isSafe ? ImfBodyScan.CheckKind.None : ImfBodyScan.CheckKind.Output;
                    scan.OutputEncoder = CreateEncoder(node) ?? scan.OutputEncoder;
                }
            }
        }

        private void AddEntity(Node node, ItemBuilder builder)
        {
            var source = node.Source;
            if (source.Kind == ImfEntitySource.SourceKind.Raw)
            {
                builder.AddContent(source.Content, source.ContentStream, node.StartPosition, null);
                builder.CompleteEntity();
                return;
            }

            builder.AddLiteral(node.HeaderBytes);
            builder.CompleteFields(node.FieldCount);
            AddGeneratedFields(node, builder);
            if (source.RawHeaderEnd is ReadOnlyMemory<byte> headerEnd) builder.AddLiteral(headerEnd.Span);
            else builder.AddLiteral(_lineBreak);

            switch (source.Kind)
            {
                case ImfEntitySource.SourceKind.Content:
                    builder.AddContent(source.Content, source.ContentStream, node.StartPosition, CreateEncoder(node));
                    break;
                case ImfEntitySource.SourceKind.Multipart:
                    AddMultipartBody(node, builder);
                    break;
                default:
                    AddEntity(node.Children[0], builder);
                    break;
            }

            builder.CompleteEntity();
        }

        private void AddGeneratedFields(Node node, ItemBuilder builder)
        {
            if (node.IsMessage && !node.HasMimeVersion && !node.Source.IsPreserved)
            {
                AddGeneratedField(MimeVersionFieldName, MimeVersionValue, builder);
            }

            if (!node.HasContentTypeField)
            {
                var contentType = node.Boundary is null || !node.Boundary.IsGenerated
                    ? node.Source.ContentType
                    : node.Source.ContentType.WithParameter(BoundaryParameterName, node.Boundary.Value);
                AddGeneratedField(ContentTypeFieldName, ImfMimeFields.FormatContentType(contentType), builder);
            }

            // ref. RFC 2045 §6.1: "Content-Transfer-Encoding: 7BIT" is assumed if the field is not present.
            if (!node.HasTransferEncodingField && (node.Encoding != MimeTransferEncoding.SevenBit || node.RequestedEncoding == MimeTransferEncoding.SevenBit))
            {
                AddGeneratedField(ContentTransferEncodingFieldName, node.Encoding.Name, builder);
            }
        }

        private void AddGeneratedField(string name, string value, ItemBuilder builder)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(1024);
            try
            {
                var length = ImfHeaderFormatter.Format(ImfHeaderFormatter.GetBytes(name), ImfHeaderFormatter.GetBytes(value), ImfHeaderFieldKind.Structured,
                    _options.HeaderEncoding, _lineBreak, ref buffer);
                builder.AddLiteral(new ReadOnlySpan<byte>(buffer, 0, length));
                builder.CompleteFields(1);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private void AddMultipartBody(Node node, ItemBuilder builder)
        {
            var source = node.Source;
            var boundary = node.Boundary;
            if (boundary is null)
                throw new InvalidOperationException("A multipart entity has no boundary.");

            if (!(source.RawFraming is null))
            {
                for (var index = 0; index < node.Children.Count; index++)
                {
                    builder.AddLiteral(source.RawFraming[index].Span);
                    AddEntity(node.Children[index], builder);
                }

                builder.AddLiteral(source.RawFraming[node.Children.Count].Span);
                return;
            }

            var delimiter = GetDelimiter(boundary.Value);
            if (source.Preamble is ReadOnlyMemory<byte> preamble)
            {
                builder.AddLiteral(preamble.Span);
                builder.AddLiteral(_lineBreak);
            }

            for (var index = 0; index < node.Children.Count; index++)
            {
                if (index > 0) builder.AddLiteral(_lineBreak);

                builder.AddLiteral(delimiter);
                builder.AddLiteral(_lineBreak);
                AddEntity(node.Children[index], builder);
            }

            builder.AddLiteral(_lineBreak);
            builder.AddLiteral(delimiter);
            builder.AddLiteral(DelimiterDashes);

            if (source.Epilogue is ReadOnlyMemory<byte> epilogue)
            {
                builder.AddLiteral(_lineBreak);
                builder.AddLiteral(epilogue.Span);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static ImfBodyEncoder? CreateEncoder(Node node)
#else
        private static ImfBodyEncoder CreateEncoder(Node node)
#endif
        {
            if (node.Encoding == MimeTransferEncoding.Base64) return new ImfBodyEncoder.Base64();
            if (node.Encoding != MimeTransferEncoding.QuotedPrintable) return null;

            // ref. RFC 2045 §6.7 rule 4: only a line break of text is written as a line break.  A stream that wasn't looked at has only its type to go by.
            var encodeLineBreaks = node.UsesUnscannedStream ? !IsTextType(node.Source.ContentType) : node.EncodeLineBreaks;
            return new ImfBodyEncoder.QuotedPrintable(encodeLineBreaks ? MimeQuotedPrintableEncoderOptions.Binary : MimeQuotedPrintableEncoderOptions.None);
        }

        private static byte[] GetDelimiter(string boundary)
        {
            var delimiter = new byte[boundary.Length + DelimiterDashes.Length];
            DelimiterDashes.CopyTo(delimiter, 0);
            for (var index = 0; index < boundary.Length; index++)
            {
                delimiter[index + DelimiterDashes.Length] = (byte)boundary[index];
            }

            return delimiter;
        }

        private static bool AllAreQuotedPrintableSafe(ImfBoundary[] boundaries)
        {
            foreach (var boundary in boundaries)
            {
                if (boundary.IsGenerated || boundary.IsQuotedPrintableSafe) continue;

                return false;
            }

            return true;
        }

        private static ImfFileException BuildCompositeEncodingError(Node node)
        {
            return BuildError($"A {node.Source.ContentType.Type}/{node.Source.ContentType.Subtype} entity can have only an identity transfer encoding, 7bit, 8bit or binary (RFC 2045 §6.4), which the options or its TransferEncoding must allow.", node.Path);
        }

        private static ImfFileException BuildError(string message, string path)
        {
            var error = new ImfFileException(message);
            error.Data[ImfFileException.DataItemName.EntityPath] = path;
            return error;
        }

        private static bool IsCompositeContentType(MimeMediaType contentType)
        {
            if (string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase)) return true;

            // ref. RFC 6532 §3.7: message/global may have any transfer encoding.
            return string.Equals(contentType.Type, MimeTopLevelTypes.Message, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(contentType.Subtype, "global", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTextType(MimeMediaType contentType)
        {
            return string.Equals(contentType.Type, MimeTopLevelTypes.Text, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidBoundary(string boundary)
        {
            if (boundary.Length < 1 || boundary.Length > MaxBoundaryLength || boundary[boundary.Length - 1] == ' ') return false;

            foreach (var value in boundary)
            {
                // ref. RFC 2046 §5.1.1: bchars
                var isValid = (value >= '0' && value <= '9')
                    || (value >= 'A' && value <= 'Z')
                    || (value >= 'a' && value <= 'z')
                    || value == '\'' || value == '(' || value == ')' || value == '+' || value == '_' || value == ','
                    || value == '-' || value == '.' || value == '/' || value == ':' || value == '=' || value == '?' || value == ' ';
                if (!isValid) return false;
            }

            return true;
        }

        private static bool EqualsIgnoreCase(ReadOnlySpan<byte> name, string expected)
        {
            if (name.Length != expected.Length) return false;

            for (var index = 0; index < name.Length; index++)
            {
                var value = name[index];
                var other = expected[index];
                if (value >= 'A' && value <= 'Z') value += 'a' - 'A';
                if (other >= 'A' && other <= 'Z') other = (char)(other + ('a' - 'A'));
                if (value != other) return false;
            }

            return true;
        }

        private static MimeDataDomain Max(MimeDataDomain left, MimeDataDomain right)
        {
            return left > right ? left : right;
        }

        /// <summary>
        /// One entity of the tree being written.
        /// </summary>
        private sealed class Node
        {
            internal Node(ImfEntitySource source, string path, bool isMessage)
            {
                Source = source;
                Path = path;
                IsMessage = isMessage;
                HeaderBytes = NoBytes;
                Children = new List<Node>();
                StartPosition = source.ContentStream.CanSeek ? source.ContentStream.Position : 0;
            }

            internal ImfEntitySource Source { get; }

            internal string Path { get; }

            internal bool IsMessage { get; }

            internal List<Node> Children { get; }

            /// <summary>
            /// Gets where the stream of the content was when the write began.
            /// </summary>
            internal long StartPosition { get; }

            internal byte[] HeaderBytes { get; set; }

            internal int FieldCount { get; set; }

            internal bool HasContentTypeField { get; set; }

            internal bool HasTransferEncodingField { get; set; }

            internal bool HasMimeVersion { get; set; }

            /// <summary>
            /// Gets or sets the transfer encoding the caller asked for, or the default value if the choice is the writer's.
            /// </summary>
            internal MimeTransferEncoding RequestedEncoding { get; set; }

            internal bool EncodeLineBreaks { get; set; }

            internal bool UsesUnscannedStream { get; set; }

            internal MimeTransferEncoding Encoding { get; set; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal ImfBoundary? Boundary { get; set; }

            internal ImfBodyScan? BodyScan { get; set; }

            internal ImfBodyScan? HeaderScan { get; set; }

            internal ImfBodyScan? PreambleScan { get; set; }

            internal ImfBodyScan? EpilogueScan { get; set; }
#else
            internal ImfBoundary Boundary { get; set; }

            internal ImfBodyScan BodyScan { get; set; }

            internal ImfBodyScan HeaderScan { get; set; }

            internal ImfBodyScan PreambleScan { get; set; }

            internal ImfBodyScan EpilogueScan { get; set; }
#endif
        }

        /// <summary>
        /// Joins neighbouring literal bytes into one item, and keeps count of what each item completes.
        /// </summary>
        private sealed class ItemBuilder
        {
            private readonly MemoryStream _literal = new MemoryStream();
            private int _entities;
            private int _fields;

            internal List<ImfWriteItem> Items { get; } = new List<ImfWriteItem>();

            internal void AddLiteral(ReadOnlySpan<byte> bytes)
            {
                foreach (var value in bytes)
                {
                    _literal.WriteByte(value);
                }
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal void AddContent(ReadOnlyMemory<byte> bytes, Stream stream, long startPosition, ImfBodyEncoder? encoder)
#else
            internal void AddContent(ReadOnlyMemory<byte> bytes, Stream stream, long startPosition, ImfBodyEncoder encoder)
#endif
            {
                Flush();
                Items.Add(new ImfWriteItem(bytes, stream, startPosition, encoder, true, 0, 0));
            }

            internal void CompleteFields(int count)
            {
                _fields += count;
            }

            internal void CompleteEntity()
            {
                _entities++;
            }

            internal void Flush()
            {
                if (_literal.Length == 0 && _fields == 0 && _entities == 0) return;

                Items.Add(new ImfWriteItem(_literal.ToArray(), Stream.Null, 0, null, false, _fields, _entities));
                _literal.SetLength(0);
                _fields = 0;
                _entities = 0;
            }
        }
    }
}
