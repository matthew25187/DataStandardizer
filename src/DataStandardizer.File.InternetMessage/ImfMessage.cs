using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// An Internet message (RFC 5322): an ordered list of header fields, with typed accessors over them, and a body.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Header fields.</b>  <see cref="Fields"/> keeps every field in order, so a field that hasn't changed is written back byte for byte.  Each accessor reads and writes through
    /// it.  Setting one replaces the field where it is, or adds it after the other fields if there is none, and removes any others of that name.  Setting one to <c>null</c>, or to
    /// an empty list, removes the field.  Other fields are not touched.
    /// </para>
    /// <para>
    /// <b>Values.</b>  An accessor gives <c>null</c>, or nothing, if the field is absent or its body is not valid, which <see cref="Validate"/> reports.  If there is more than one
    /// of a field that may appear only once, it gives the first.  Text and display names are decoded from RFC 2047 encoded words.  A value that is set is formatted when it is written,
    /// with the <see cref="ImfOptions.HeaderEncoding"/> of the writer.
    /// </para>
    /// <para>
    /// <b>Body.</b>  <see cref="RawBody"/> holds the bytes after the empty line that ends the header, and <see cref="Body"/> gives the MIME entities they hold.  The fields of
    /// <see cref="Body"/> are those of the message.
    /// </para>
    /// </remarks>
    public sealed class ImfMessage
    {
        private const string PlainSubtype = "plain";
        private const string HtmlSubtype = "html";
        private const string RelatedSubtype = "related";
        private const string AlternativeSubtype = "alternative";
        private const string StartParameterName = "start";

        private static readonly ImfHeaderField[] NoFields = new ImfHeaderField[0];

        [AllowNull]
        private ImfEntity _body;
        private bool _isBodyReplaced;
        private ReadOnlyMemory<byte> _rawBody;

        /// <summary>
        /// Creates a message with no fields and no body, whose accessors use the default options.
        /// </summary>
        public ImfMessage() : this(new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a message with no fields and no body.
        /// </summary>
        /// <param name="options">The options that the accessors and <see cref="Validate"/> parse fields under.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/>
        /// that is needed and can't be resolved.</exception>
        public ImfMessage(ImfOptions options)
        {
            ImfHeaderText.CheckOptions(options);
            Options = options;
        }

        /// <summary>
        /// Gets the options that the accessors and <see cref="Validate"/> parse fields under.
        /// </summary>
        public ImfOptions Options { get; }

        /// <summary>
        /// Gets the header fields, in order.
        /// </summary>
        public ImfHeaderFieldCollection Fields { get; } = new ImfHeaderFieldCollection();

        /// <summary>
        /// Gets or sets the body: the bytes after the empty line that ends the header.  They are written as they are.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The memory must not change until a write has finished.
        /// </para>
        /// <para>
        /// Setting this replaces <see cref="Body"/>, which is read again from these bytes when it is next asked for.  A change made through <see cref="Body"/> doesn't change these
        /// bytes: a writer writes the change, and this still gives what was read or set.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<byte> RawBody
        {
            get => _rawBody;
            set
            {
                _body?.DetachFromMessage();
                _body = null;
                _isBodyReplaced = false;
                _rawBody = value;
            }
        }

        /// <summary>
        /// Gets or sets the body as a MIME entity: an <see cref="ImfMultipart"/> for a multipart body, an <see cref="ImfMessagePart"/> for a <c>message/rfc822</c> body, and otherwise an
        /// <see cref="ImfEntity"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Its fields are those of the message, so its <see cref="ImfEntity.ContentType"/>, for one, reads and writes the Content-Type field of the message.
        /// </para>
        /// <para>
        /// Setting this takes the fields whose names start with <c>Content-</c> out of the message, and adds those of the entity after the other fields.
        /// </para>
        /// <para>
        /// The type of the entity, and the parts it holds, are settled when it is read.  Replacing the Content-Type field through <see cref="Fields"/> afterwards doesn't read the body
        /// again, so to have it read under a new media type, set <see cref="RawBody"/>.  To change the media type of what the entity already holds, use
        /// <see cref="ImfEntity.ContentType"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ImfFileException">On get: <see cref="RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        /// <exception cref="ArgumentNullException">On set: the value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">On set: the entity holds this message.</exception>
        /// <exception cref="InvalidOperationException">On set: the entity is already the body of a message or a part of a multipart entity.</exception>
        public ImfEntity Body
        {
            get
            {
                if (_body is null) _body = ImfEntityTree.Read(this, new List<ImfDiagnostic>());

                return _body;
            }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                if (ReferenceEquals(value, _body)) return;
                if (value.IsAttached)
                    throw new InvalidOperationException("The entity is already the body of a message or a part of a multipart entity.");
                if (value.Holds(this))
                    throw new ArgumentException("An entity can't be the body of a message that it holds.", nameof(value));

                _body?.DetachFromMessage();
                _body = value;
                _isBodyReplaced = true;
                value.AttachToMessage(Fields);
            }
        }

        /// <summary>
        /// Gets the entity of the plain text of the message, or <c>null</c> if it has none.
        /// </summary>
        /// <remarks>
        /// The search follows the structure of the message.  Of the parts of a <c>multipart/alternative</c> entity, it takes the last that has one, as RFC 2046 §5.1.4 puts the best
        /// last.  In a <c>multipart/related</c> entity, it looks only in the root part (RFC 2387 §3.2).  In any other multipart entity, it takes the first part that has one.  An
        /// entity with a Content-Disposition that <see cref="ImfContentDisposition.IsAttachment"/> says is an attachment isn't the text of the message, and neither is what it holds,
        /// or what is inside an embedded message.
        /// </remarks>
        /// <exception cref="ImfFileException"><see cref="RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        [MaybeNull]
        public ImfEntity TextBody => FindBodyPart(Body, PlainSubtype);

        /// <summary>
        /// Gets the entity of the HTML text of the message, or <c>null</c> if it has none.  It is found as <see cref="TextBody"/> describes.
        /// </summary>
        /// <exception cref="ImfFileException"><see cref="RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        [MaybeNull]
        public ImfEntity HtmlBody => FindBodyPart(Body, HtmlSubtype);

        /// <summary>
        /// Gets the attachments of the message: each entity with a Content-Disposition that <see cref="ImfContentDisposition.IsAttachment"/> says is one, and each entity without
        /// parts that is not text and has a <see cref="ImfEntity.FileName"/>, in the order of the message.  The list is read-only, and doesn't follow later changes.  What is inside
        /// an embedded message is not looked at, as that belongs to that message, and neither are the parts of a multipart entity that is an attachment.  Such an entity is listed, and
        /// has parts rather than content.
        /// </summary>
        /// <exception cref="ImfFileException"><see cref="RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        public IReadOnlyList<ImfEntity> Attachments
        {
            get
            {
                var attachments = new List<ImfEntity>();
                foreach (var node in Enumerate(false, true))
                {
                    if (IsAttachment(node.Entity) || (!(node.Entity is ImfMultipart) && !IsText(node.Entity) && !(node.Entity.FileName is null))) attachments.Add(node.Entity);
                }

                return new ReadOnlyCollection<ImfEntity>(attachments);
            }
        }

        /// <summary>
        /// Gets or sets the date and time of the Date field (RFC 5322 §3.6.1).
        /// </summary>
        /// <exception cref="ArgumentException">On set: the value was made with <c>default</c>.</exception>
        public ImfDateTime? Date
        {
            get => ImfFieldValues.GetDate(Fields.Find(ImfFieldNames.Date), Options);
            set
            {
                if (value.HasValue && value.Value == default(ImfDateTime))
                    throw new ArgumentException("The date-time is not initialised.", nameof(value));

                var text = value.HasValue ? value.Value.ToString() : null;
                Fields.Set(ImfFieldNames.Date, text is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.Date, encoding => text) });
            }
        }

        /// <summary>
        /// Gets or sets the addresses of the From field (RFC 5322 §3.6.2, RFC 6854).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList From
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.From), Options);
            set => SetAddressList(ImfFieldNames.From, value);
        }

        /// <summary>
        /// Gets or sets the address of the Sender field (RFC 5322 §3.6.2, RFC 6854).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddress Sender
        {
            get => ImfFieldValues.GetSender(Fields.Find(ImfFieldNames.Sender), Options);
            set
            {
                var address = value;
                Fields.Set(ImfFieldNames.Sender, address is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.Sender, address.ToString) });
            }
        }

        /// <summary>
        /// Gets or sets the addresses of the Reply-To field (RFC 5322 §3.6.2).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList ReplyTo
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.ReplyTo), Options);
            set => SetAddressList(ImfFieldNames.ReplyTo, value);
        }

        /// <summary>
        /// Gets or sets the addresses of the To field (RFC 5322 §3.6.3).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList To
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.To), Options);
            set => SetAddressList(ImfFieldNames.To, value);
        }

        /// <summary>
        /// Gets or sets the addresses of the Cc field (RFC 5322 §3.6.3).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList Cc
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.Cc), Options);
            set => SetAddressList(ImfFieldNames.Cc, value);
        }

        /// <summary>
        /// Gets or sets the addresses of the Bcc field (RFC 5322 §3.6.3).  It also gives <c>null</c> for a field with no addresses, which that section allows, and which is among the
        /// <see cref="Fields"/>.
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList Bcc
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.Bcc), Options);
            set => SetAddressList(ImfFieldNames.Bcc, value);
        }

        /// <summary>
        /// Gets or sets the identifier of the Message-ID field (RFC 5322 §3.6.4).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfMessageId MessageId
        {
            get => ImfFieldValues.GetMessageId(Fields.Find(ImfFieldNames.MessageId));
            set
            {
                var messageId = value;
                Fields.Set(ImfFieldNames.MessageId, messageId is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.MessageId, encoding => messageId.ToString()) });
            }
        }

        /// <summary>
        /// Gets or sets the identifiers of the In-Reply-To field (RFC 5322 §3.6.4).
        /// </summary>
        /// <exception cref="ArgumentException">On set: the identifiers hold a <c>null</c> element.</exception>
        [AllowNull, MaybeNull]
        public IReadOnlyList<ImfMessageId> InReplyTo
        {
            get => ImfFieldValues.GetMessageIds(Fields.Find(ImfFieldNames.InReplyTo));
            set => SetMessageIds(ImfFieldNames.InReplyTo, value);
        }

        /// <summary>
        /// Gets or sets the identifiers of the References field (RFC 5322 §3.6.4).
        /// </summary>
        /// <exception cref="ArgumentException">On set: the identifiers hold a <c>null</c> element.</exception>
        [AllowNull, MaybeNull]
        public IReadOnlyList<ImfMessageId> References
        {
            get => ImfFieldValues.GetMessageIds(Fields.Find(ImfFieldNames.References));
            set => SetMessageIds(ImfFieldNames.References, value);
        }

        /// <summary>
        /// Gets or sets the text of the Subject field (RFC 5322 §3.6.5).
        /// </summary>
        /// <exception cref="ImfFileException">On set: the text is not valid Unicode, or holds a control character other than tab.</exception>
        [AllowNull, MaybeNull]
        public string Subject
        {
            get => Fields.Find(ImfFieldNames.Subject)?.GetDecodedValue(Options);
            set
            {
                var text = value;
                Fields.Set(ImfFieldNames.Subject, text is null ? NoFields : new[] { ImfHeaderField.FromText(ImfFieldNames.Subject, text) });
            }
        }

        /// <summary>
        /// Gets or sets the text of each Comments field, in order (RFC 5322 §3.6.5).
        /// </summary>
        /// <exception cref="ArgumentNullException">On set: the value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">On set: the value holds a <c>null</c> element.</exception>
        /// <exception cref="ImfFileException">On set: a text is not valid Unicode, or holds a control character other than tab.</exception>
        public IReadOnlyList<string> Comments
        {
            get
            {
                var fields = Fields.FindAll(ImfFieldNames.Comments);
                var comments = new string[fields.Count];
                for (var index = 0; index < comments.Length; index++)
                {
                    comments[index] = fields[index].GetDecodedValue(Options);
                }

                return new ReadOnlyCollection<string>(comments);
            }
            set
            {
                var texts = ImfGrammar.CopyElements(value, nameof(value));
                var fields = new ImfHeaderField[texts.Length];
                for (var index = 0; index < fields.Length; index++)
                {
                    fields[index] = ImfHeaderField.FromText(ImfFieldNames.Comments, texts[index]);
                }

                Fields.Set(ImfFieldNames.Comments, fields);
            }
        }

        /// <summary>
        /// Gets or sets the phrases of each Keywords field, in order (RFC 5322 §3.6.5).  A field that is not valid is left out.
        /// </summary>
        /// <exception cref="ArgumentNullException">On set: the value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">On set: the value holds a <c>null</c> element.</exception>
        public IReadOnlyList<ImfPhraseList> Keywords
        {
            get
            {
                var keywords = new List<ImfPhraseList>();
                foreach (var keywordField in Fields.FindAll(ImfFieldNames.Keywords))
                {
                    if (ImfPhraseList.TryParse(keywordField.ValueBytes.Span, Options, out var phrases)) keywords.Add(phrases);
                }

                return new ReadOnlyCollection<ImfPhraseList>(keywords);
            }
            set
            {
                var lists = ImfGrammar.CopyElements(value, nameof(value));
                var fields = new ImfHeaderField[lists.Length];
                for (var index = 0; index < fields.Length; index++)
                {
                    fields[index] = ImfHeaderField.FromValue(ImfFieldNames.Keywords, lists[index].ToString);
                }

                Fields.Set(ImfFieldNames.Keywords, fields);
            }
        }

        /// <summary>
        /// Gets the blocks of resent fields, in the order of the header (RFC 5322 §3.6.6).  The list is read-only, and doesn't follow later changes to the fields.  To add a block,
        /// add its fields to <see cref="Fields"/>.
        /// </summary>
        /// <remarks>
        /// RFC 5322 gives no mark between blocks, and no order within one.  So a block ends at any other field, or at a field that it already has.
        /// </remarks>
        public IReadOnlyList<ImfResentBlock> ResentBlocks => ImfResentBlock.GetBlocks(Fields, Options);

        /// <summary>
        /// Gets the Return-Path and Received fields, in the order of the header (RFC 5322 §3.6.7).  The list is read-only, and doesn't follow later changes to the fields.  To add
        /// one, add it to <see cref="Fields"/>.
        /// </summary>
        public IReadOnlyList<ImfTraceField> TraceFields => ImfTraceField.GetFields(Fields, Options);

        /// <summary>
        /// Gets the line break of the empty line that ended the header of a message that was read, which is empty if the data ended first; or <c>null</c> if the message
        /// was not read, in which case a writer adds one.
        /// </summary>
        [MaybeNull]
        internal byte[] HeaderEnd { get; set; }

        /// <summary>
        /// Gets the position of the end of the header of a message that was read, or -1 if the message was not read.
        /// </summary>
        internal long HeaderEndBytePosition { get; set; } = -1;

        /// <summary>
        /// Gets the 1-based number of the line at the end of the header of a message that was read, or 0 if the message was not read.
        /// </summary>
        internal long HeaderEndLineNumber { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating that a line feed is the line break of the message, as in the messages inside mbox and EMLX files.
        /// </summary>
        internal bool LineFeedIsLineBreak { get; set; }

        /// <summary>
        /// Gets a flag indicating that the body differs from <see cref="RawBody"/>, without reading <see cref="Body"/> if it has not been asked for.
        /// </summary>
        internal bool IsBodyChanged => _isBodyReplaced || (!(_body is null) && _body.HasChangedBody);

        /// <summary>
        /// Gets a flag indicating that the message differs from what was read, for the entity that holds it.
        /// </summary>
        internal bool HasChanged => _body is null || _isBodyReplaced || _body.HasChangedFields || _body.HasChangedBody;

        /// <summary>
        /// Gets the body if it has been read or set, without reading it.
        /// </summary>
        [MaybeNull]
        internal ImfEntity LoadedBody => _body;

        /// <summary>
        /// Reads <see cref="Body"/> from <see cref="RawBody"/>.
        /// </summary>
        /// <param name="diagnostics">Receives the problems that did not stop reading.</param>
        /// <exception cref="ImfFileException">The body breaks a rule of RFC 2046 §5.1 that the options don't accept, or passes a limit.</exception>
        internal void ReadBody(List<ImfDiagnostic> diagnostics)
        {
            _body = ImfEntityTree.Read(this, diagnostics);
        }

        /// <summary>
        /// Gives a message that <see cref="ImfEntityTree"/> has made from the data of another its body.
        /// </summary>
        internal void SetReadBody(ImfEntity body, ReadOnlyMemory<byte> rawBody)
        {
            _rawBody = rawBody;
            _body = body;
        }

        /// <summary>
        /// Enumerates the entities of the message, depth first, each with its path.
        /// </summary>
        /// <remarks>
        /// The paths are those of <see cref="ImfEntityEvent.EntityPath"/>, so an <see cref="ImfMessagePart"/> and the <see cref="Body"/> of the message inside it have the same path, and
        /// differ in depth.  The first entity is <see cref="Body"/>, with the empty path.  Changing the tree while this is enumerating gives undefined results.
        /// </remarks>
        /// <returns>The entities, including those of embedded messages.</returns>
        /// <exception cref="ImfFileException"><see cref="RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        public IEnumerable<ImfEntityNode> EnumerateEntities()
        {
            return Enumerate(true, false);
        }

        /// <summary>
        /// Finds the entity with a Content-ID within this message (RFC 2392 §2), the first in the order of <see cref="EnumerateEntities"/>.  What is inside an embedded message is not
        /// looked at, as a <c>cid:</c> URI refers to the message that holds it.
        /// </summary>
        /// <param name="contentId">The Content-ID.</param>
        /// <returns>The entity, or <c>null</c> if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="contentId"/> is <c>null</c>.</exception>
        /// <exception cref="ImfFileException"><see cref="RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        [return: MaybeNull]
        public ImfEntity FindByContentId(ImfMessageId contentId)
        {
            if (contentId is null)
                throw new ArgumentNullException(nameof(contentId));

            foreach (var node in Enumerate(false, false))
            {
                if (node.Entity.ContentId == contentId) return node.Entity;
            }

            return null;
        }

        /// <summary>
        /// Finds the entity that a <c>cid:</c> URI refers to, as <see cref="FindByContentId(ImfMessageId)"/> does.
        /// </summary>
        /// <param name="cidUri">The URI, such as <c>cid:image1@example.org</c> (RFC 2392 §2).</param>
        /// <returns>The entity, or <c>null</c> if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="cidUri"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="cidUri"/> is not a valid <c>cid:</c> URI.</exception>
        /// <exception cref="ImfFileException"><see cref="RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        [return: MaybeNull]
        public ImfEntity FindByContentId(string cidUri)
        {
            return FindByContentId(ImfMessageId.ParseCidUri(cidUri));
        }

        /// <summary>
        /// Checks the header against RFC 5322 §3.6, without throwing: the required fields, the fields that may appear only once, and the grammar of the body of each field that §3.6
        /// defines.  The options don't change what is reported, except in how a field body is read.
        /// </summary>
        /// <returns>Every problem found, in a read-only list.</returns>
        public IReadOnlyList<ImfDiagnostic> Validate()
        {
            return new ReadOnlyCollection<ImfDiagnostic>(ImfConformance.Check(this, Options));
        }

        private IEnumerable<ImfEntityNode> Enumerate(bool followMessages, bool stopAtAttachments)
        {
            // The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack, or cost a call for each level.
            var pending = new Stack<ImfEntityNode>();
            pending.Push(new ImfEntityNode(Body, string.Empty, 0, true));
            while (pending.Count > 0)
            {
                var node = pending.Pop();
                yield return node;

                if (node.Entity is ImfMultipart multipart)
                {
                    if (stopAtAttachments && IsAttachment(multipart)) continue;

                    for (var index = multipart.Parts.Count - 1; index >= 0; index--)
                    {
                        var number = (index + 1).ToString(CultureInfo.InvariantCulture);
                        pending.Push(new ImfEntityNode(multipart.Parts[index], node.Path.Length == 0 ? number : node.Path + "." + number, node.Depth + 1, false));
                    }
                }
                else if (followMessages && node.Entity is ImfMessagePart messagePart)
                {
                    // ref. RFC 3501 §6.4.5: a message part and the message in it have the one number, but the body of a message is its part 1.
                    var path = !node.IsMessageBody ? node.Path : node.Path.Length == 0 ? "1" : node.Path + ".1";
                    pending.Push(new ImfEntityNode(messagePart.Message.Body, path, node.Depth + 1, true));
                }
            }
        }

        [return: MaybeNull]
        private static ImfEntity FindBodyPart(ImfEntity entity, string subtype)
        {
            // The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack.  What is pushed last is looked at first.
            var pending = new Stack<ImfEntity>();
            pending.Push(entity);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (IsAttachment(current)) continue;

                if (current is ImfMultipart multipart)
                {
                    PushBodyCandidates(multipart, pending);
                }
                else if (!(current is ImfMessagePart) && IsText(current) && HasSubtype(current, subtype))
                {
                    return current;
                }
            }

            return null;
        }

        private static void PushBodyCandidates(ImfMultipart multipart, Stack<ImfEntity> pending)
        {
            var parts = multipart.Parts;
            if (HasSubtype(multipart, RelatedSubtype))
            {
                var root = GetRelatedRoot(multipart);
                if (!(root is null)) pending.Push(root);
            }
            else if (HasSubtype(multipart, AlternativeSubtype))
            {
                // ref. RFC 2046 §5.1.4: "the best choice" is the last that can be shown.
                foreach (var part in parts)
                {
                    pending.Push(part);
                }
            }
            else
            {
                for (var index = parts.Count - 1; index >= 0; index--)
                {
                    pending.Push(parts[index]);
                }
            }
        }

        private static bool IsAttachment(ImfEntity entity)
        {
            var disposition = entity.ContentDisposition;
            return !(disposition is null) && disposition.IsAttachment;
        }

        /// <summary>
        /// Gets the root part of a <c>multipart/related</c> entity: the one its <c>start</c> parameter names, or otherwise the first (RFC 2387 §3.2).
        /// </summary>
        [return: MaybeNull]
        private static ImfEntity GetRelatedRoot(ImfMultipart multipart)
        {
            if (multipart.ContentType.TryGetParameter(StartParameterName, out var start) && ImfMessageId.TryParse(start.Value, out var contentId))
            {
                foreach (var part in multipart.Parts)
                {
                    if (part.ContentId == contentId) return part;
                }
            }

            return multipart.Parts.Count == 0 ? null : multipart.Parts[0];
        }

        private static bool IsText(ImfEntity entity)
        {
            return string.Equals(entity.ContentType.Type, MimeTopLevelTypes.Text, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasSubtype(ImfEntity entity, string subtype)
        {
            return string.Equals(entity.ContentType.Subtype, subtype, StringComparison.OrdinalIgnoreCase);
        }

        private void SetAddressList(string name, [AllowNull] ImfAddressList value)
        {
            var addresses = value;
            Fields.Set(name, addresses is null ? NoFields : new[] { ImfHeaderField.FromValue(name, addresses.ToString) });
        }

        private void SetMessageIds(string name, [AllowNull] IReadOnlyList<ImfMessageId> value)
        {
            if (value is null)
            {
                Fields.Set(name, NoFields);
                return;
            }

            var messageIds = ImfGrammar.CopyElements(value, nameof(value));
            if (messageIds.Length == 0)
            {
                Fields.Set(name, NoFields);
                return;
            }

            var text = new StringBuilder();
            foreach (var messageId in messageIds)
            {
                if (text.Length > 0) text.Append(' ');

                text.Append(messageId);
            }

            var body = text.ToString();
            Fields.Set(name, new[] { ImfHeaderField.FromValue(name, encoding => body) });
        }
    }
}
