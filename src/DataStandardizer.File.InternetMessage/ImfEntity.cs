using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using DataStandardizer.Communication.Mime;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A MIME entity (RFC 2045 §2.4): an ordered list of header fields, with typed accessors over them, and a body.  An <see cref="ImfMultipart"/> holds other entities, an
    /// <see cref="ImfMessagePart"/> holds a message, and any other entity holds content.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Header fields.</b>  Each accessor reads and writes through <see cref="Fields"/>, as those of <see cref="ImfMessage"/> do.  An accessor gives <c>null</c> if the field is absent
    /// or its body is not valid.  Changing a field rewrites that field only.
    /// </para>
    /// <para>
    /// <b>Content.</b>  What was read is held in memory as it is, and is decoded each time it is asked for.  An entity that has not changed is written back byte for byte.
    /// <see cref="SetContent"/> holds new content, for which a writer chooses a transfer encoding.
    /// </para>
    /// </remarks>
    public class ImfEntity
    {
        private const string CharsetParameterName = "charset";
        private const string ContentNameParameterName = "name";
        private const string ContentFieldPrefix = "Content-";
        private const string TextReplacementCharacter = "�";

        private static readonly ImfHeaderField[] NoFields = new ImfHeaderField[0];

        private ReadOnlyMemory<byte> _content;
        private MimeMediaType _defaultContentType;
        private bool _hasRawEntity;
        private bool _isContentSet;
        private bool _isMoved;
        private bool _isRead;
        private bool _lineFeedIsLineBreak;
        private ReadOnlyMemory<byte> _rawBody;
        private ReadOnlyMemory<byte> _rawEntity;
        private ReadOnlyMemory<byte> _rawHeaderEnd;
        private ImfHeaderField[] _readFields = NoFields;

        /// <summary>
        /// Creates an entity with no fields and no content, whose accessors use the default options.
        /// </summary>
        public ImfEntity() : this(new ImfOptions())
        {
        }

        /// <summary>
        /// Creates an entity with no fields and no content.
        /// </summary>
        /// <param name="options">The options that the accessors parse fields under, and that decide how content is read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/>
        /// that is needed and can't be resolved.</exception>
        public ImfEntity(ImfOptions options) : this(options, ImfEntityTree.DefaultContentType)
        {
            _isContentSet = true;
        }

        /// <summary>
        /// Creates an entity for a subclass, with the media type it has if it has no Content-Type field.
        /// </summary>
        internal ImfEntity(ImfOptions options, MimeMediaType defaultContentType)
        {
            ImfHeaderText.CheckOptions(options);
            Options = options;
            _defaultContentType = defaultContentType;
            Fields = new ImfHeaderFieldCollection();
        }

        /// <summary>
        /// Gets the options that the accessors parse fields under, and that decide how content is read.
        /// </summary>
        public ImfOptions Options { get; }

        /// <summary>
        /// Gets the header fields, in order.  For the <see cref="ImfMessage.Body"/> of a message, these are the <see cref="ImfMessage.Fields"/>.
        /// </summary>
        public ImfHeaderFieldCollection Fields { get; private set; }

        /// <summary>
        /// Gets or sets the media type of the Content-Type field (RFC 2045 §5).  If the field is absent, it gives the default of <c>text/plain; charset=us-ascii</c> (RFC 2045 §5.2), or
        /// <c>message/rfc822</c> for a part of a <c>multipart/digest</c> entity (RFC 2046 §5.1.5).
        /// </summary>
        /// <remarks>
        /// A reader goes by the parameters it needs to find the parts of a message, and accepts a field that <see cref="ImfMimeFields.TryParseContentType(ReadOnlySpan{byte}, ImfOptions, out MimeMediaType)"/>
        /// doesn't, such as one with an encoded word in a parameter, which RFC 2047 §5 doesn't allow.  This gives what the reader found for such a field, with the parameter as it was
        /// written.  With <see cref="ImfOptions.AllowMalformedParameters"/>, the parameter is decoded as that option describes.
        /// </remarks>
        /// <exception cref="ArgumentException">On set: the value is not initialised, or is of a kind that this type of entity doesn't hold.</exception>
        public MimeMediaType ContentType
        {
            get
            {
                var headerField = Fields.Find(ImfFieldNames.ContentType);
                return !(headerField is null) && ImfMimeFields.TryParseContentType(headerField.ValueBytes.Span, Options, out var mediaType) ? mediaType : _defaultContentType;
            }
            set
            {
                if (value == default(MimeMediaType))
                    throw new ArgumentException("The media type is not initialised.", nameof(value));

                CheckContentType(value, nameof(value));
                Fields.Set(ImfFieldNames.ContentType, new[] { ImfHeaderField.FromValue(ImfFieldNames.ContentType, encoding => ImfMimeFields.FormatContentType(value)) });
            }
        }

        /// <summary>
        /// Gets or sets the transfer encoding of the Content-Transfer-Encoding field (RFC 2045 §6).  If the field is absent or not valid, it gives the default of <c>7bit</c>
        /// (RFC 2045 §6.1).
        /// </summary>
        /// <remarks>
        /// Setting an encoding has a writer use it for this entity, and setting the default value of <see cref="MimeTransferEncoding"/> removes the field and leaves the choice to the
        /// writer.  Content that was read is decoded first, and encoded again when it is written.
        /// </remarks>
        /// <exception cref="ArgumentException">On set: the entity holds other entities, which can have only an identity encoding (RFC 2045 §6.4).</exception>
        /// <exception cref="ImfFileException">On set: the content that was read can't be decoded.</exception>
        public MimeTransferEncoding ContentTransferEncoding
        {
            get
            {
                var headerField = Fields.Find(ImfFieldNames.ContentTransferEncoding);
                return !(headerField is null) && ImfMimeFields.TryParseContentTransferEncoding(headerField.ValueBytes.Span, out var encoding) ? encoding : MimeTransferEncoding.SevenBit;
            }
            set
            {
                CheckTransferEncoding(value, nameof(value));
                PrepareForTransferEncoding();
                Fields.Set(ImfFieldNames.ContentTransferEncoding, value == default(MimeTransferEncoding)
                    ? NoFields
                    : new[] { ImfHeaderField.FromValue(ImfFieldNames.ContentTransferEncoding, encoding => value.Name) });
            }
        }

        /// <summary>
        /// Gets or sets the disposition of the Content-Disposition field (RFC 2183).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfContentDisposition ContentDisposition
        {
            get
            {
                var headerField = Fields.Find(ImfFieldNames.ContentDisposition);
                return !(headerField is null) && ImfContentDisposition.TryParse(headerField.ValueBytes.Span, Options, out var disposition) ? disposition : null;
            }
            set
            {
                var disposition = value;
                Fields.Set(ImfFieldNames.ContentDisposition, disposition is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.ContentDisposition, encoding => disposition.ToString()) });
            }
        }

        /// <summary>
        /// Gets or sets the identifier of the Content-ID field (RFC 2045 §7).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfMessageId ContentId
        {
            get => ImfFieldValues.GetMessageId(Fields.Find(ImfFieldNames.ContentId));
            set
            {
                var contentId = value;
                Fields.Set(ImfFieldNames.ContentId, contentId is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.ContentId, encoding => contentId.ToString()) });
            }
        }

        /// <summary>
        /// Gets or sets the text of the Content-Description field (RFC 2045 §8).
        /// </summary>
        /// <exception cref="ImfFileException">On set: the text is not valid Unicode, or holds a control character other than tab.</exception>
        [AllowNull, MaybeNull]
        public string ContentDescription
        {
            get => Fields.Find(ImfFieldNames.ContentDescription)?.GetDecodedValue(Options);
            set
            {
                var text = value;
                Fields.Set(ImfFieldNames.ContentDescription, text is null ? NoFields : new[] { ImfHeaderField.FromText(ImfFieldNames.ContentDescription, text) });
            }
        }

        /// <summary>
        /// Gets or sets the language tags of the Content-Language field (RFC 3282).
        /// </summary>
        /// <remarks>
        /// Setting <c>null</c>, or an empty list, removes the field.
        /// </remarks>
        /// <exception cref="ArgumentException">On set: the list holds a tag that is not initialised.</exception>
        [AllowNull, MaybeNull]
        public IReadOnlyList<Bcp47LanguageTag> ContentLanguage
        {
            get
            {
                var headerField = Fields.Find(ImfFieldNames.ContentLanguage);
                return !(headerField is null) && ImfMimeFields.TryParseContentLanguage(headerField.ValueBytes.Span, Options, out var languageTags) ? languageTags : null;
            }
            set
            {
                if (value is null || value.Count == 0)
                {
                    Fields.Set(ImfFieldNames.ContentLanguage, NoFields);
                    return;
                }

                var body = ImfMimeFields.FormatContentLanguage(value);
                Fields.Set(ImfFieldNames.ContentLanguage, new[] { ImfHeaderField.FromValue(ImfFieldNames.ContentLanguage, encoding => body) });
            }
        }

        /// <summary>
        /// Gets or sets the URI of the Content-Location field (RFC 2557 §4), which is absolute or relative, and is kept as it was written, so <see cref="Uri.OriginalString"/> gives it
        /// octet by octet.  It labels the entity, and doesn't say where it came from (RFC 2557 §11.1).
        /// </summary>
        /// <remarks>
        /// Reading unfolds the URI and decodes any RFC 2047 encoded words (RFC 2557 §4.4.3).  Writing folds a long URI, and writes one with characters that a header can't hold as encoded
        /// words (RFC 2557 §4.4.1 and §4.4.2).  To resolve one that is relative, use an <see cref="MhtmlDocument"/>.
        /// </remarks>
        /// <exception cref="ArgumentException">On set: the URI holds an unpaired surrogate.</exception>
        [AllowNull, MaybeNull]
        public Uri ContentLocation
        {
            get
            {
                var headerField = Fields.Find(ImfFieldNames.ContentLocation);
                return !(headerField is null) && ImfMimeFields.TryParseContentLocation(headerField.Value, Options, out var location) ? location : null;
            }
            set
            {
                var location = value;
                Fields.Set(ImfFieldNames.ContentLocation, location is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.ContentLocation, encoding => ImfMimeFields.FormatContentLocation(location)) });
            }
        }

        /// <summary>
        /// Gets the suggested file name of the entity: the <c>filename</c> parameter of the Content-Disposition field, in the form of RFC 2231 if it has one (<c>filename*</c>), or the
        /// <c>name</c> parameter of the Content-Type field, which RFC 2046 §4.5.1 leaves in use for this; or <c>null</c> if it has none.
        /// </summary>
        /// <remarks>
        /// A file name comes from whoever sent the message.  See the remarks on <see cref="ImfContentDisposition"/> before using it.
        /// </remarks>
        [MaybeNull]
        public string FileName
        {
            get
            {
                var fileName = ContentDisposition?.FileName;
                if (!(fileName is null)) return fileName;

                return ContentType.TryGetParameter(ContentNameParameterName, out var name) ? name.Value : null;
            }
        }

        /// <summary>
        /// Gets or sets a flag indicating that the entity is the body of a message or a part of a multipart entity.
        /// </summary>
        internal bool IsAttached { get; private set; }

        /// <summary>
        /// Gets the body as it was read, which is empty for an entity that was not read.
        /// </summary>
        internal ReadOnlyMemory<byte> RawBody => _rawBody;

        /// <summary>
        /// Gets a flag indicating that the entity was read, rather than made by a caller.
        /// </summary>
        internal bool IsRead => _isRead;

        /// <summary>
        /// Gets a flag indicating that the line break of the entity is a line feed, as in the messages inside mbox and EMLX files.
        /// </summary>
        internal bool LineFeedIsLineBreak => _lineFeedIsLineBreak;

        /// <summary>
        /// Gets a flag indicating that the body differs from what was read.  The fields are not looked at, except as they change what the body means.
        /// </summary>
        internal virtual bool HasChangedBody => _isContentSet;

        /// <summary>
        /// Gets a flag indicating that the fields differ from what was read.
        /// </summary>
        internal bool HasChangedFields
        {
            get
            {
                if (Fields.Count != _readFields.Length) return true;

                for (var index = 0; index < _readFields.Length; index++)
                {
                    if (!ReferenceEquals(Fields[index], _readFields[index])) return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Gets the content of the entity, decoded from its transfer encoding (RFC 2045 §6).
        /// </summary>
        /// <remarks>
        /// The bytes are not to be changed.  An identity encoding gives the bytes that the message holds, and the others give new bytes each time.  An embedded message gives the
        /// bytes of the whole message.
        /// </remarks>
        /// <returns>The bytes.</returns>
        /// <exception cref="ImfFileException">The body is not valid in its transfer encoding, or the encoding is an extension mechanism, which can't be decoded (RFC 2045 §6.3).</exception>
        /// <exception cref="InvalidOperationException">The entity is an <see cref="ImfMultipart"/>, which has parts and no content of its own.</exception>
        public virtual ReadOnlyMemory<byte> GetContent()
        {
            return _isContentSet ? _content : Decode(_rawBody);
        }

        /// <summary>
        /// Opens a read-only stream over the content, as <see cref="GetContent"/> gives it.
        /// </summary>
        /// <returns>The stream, which doesn't need to be disposed.</returns>
        /// <exception cref="ImfFileException">The body is not valid in its transfer encoding, or the encoding is an extension mechanism, which can't be decoded (RFC 2045 §6.3).</exception>
        /// <exception cref="InvalidOperationException">The entity is an <see cref="ImfMultipart"/>, which has parts and no content of its own.</exception>
        public Stream OpenContentStream()
        {
            var content = GetContent();
            var bytes = GetArray(content, out var offset);
            return new MemoryStream(bytes, offset, content.Length, false);
        }

        /// <summary>
        /// Gets the content as text, in the character set of the <c>charset</c> parameter of the Content-Type field.  With no such parameter, it is the default for the type, or
        /// otherwise US-ASCII (RFC 2046 §4.1.2, RFC 6657 §3).
        /// </summary>
        /// <remarks>
        /// The character set is resolved by <see cref="ImfOptions.CharsetResolver"/>.  The line breaks are those of the content, which are CRLF in a <c>text</c> entity that follows
        /// RFC 2046 §4.1.1.
        /// </remarks>
        /// <returns>The text.</returns>
        /// <exception cref="ImfFileException">The body is not valid in its transfer encoding, the character set can't be resolved, or the content is not valid in it and
        /// <see cref="ImfOptions.AllowInvalidTextOctets"/> is not set.</exception>
        /// <exception cref="InvalidOperationException">The entity is not of the top-level type <c>text</c>.</exception>
        public string GetText()
        {
            var contentType = ContentType;
            if (!string.Equals(contentType.Type, MimeTopLevelTypes.Text, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only an entity of the top-level type text has text.");

            return DecodeText(GetContent(), GetCharset(contentType));
        }

        /// <summary>
        /// Sets the content of the entity.  A writer chooses a transfer encoding for it, as <see cref="ImfEntityWriter"/> describes, so this removes any Content-Transfer-Encoding field.
        /// To ask for an encoding, set <see cref="ContentTransferEncoding"/> afterwards.  The other fields are not touched, so set <see cref="ContentType"/> to say what the content is.
        /// </summary>
        /// <param name="content">The content, before any transfer encoding.  A writer gives it back unchanged after decoding, so give text with CRLF line breaks (RFC 2046 §4.1.1).  The
        /// memory must not change until a write has finished.</param>
        /// <exception cref="ArgumentException"><paramref name="content"/> is longer than <see cref="ImfOptions.MaxEntitySize"/>.</exception>
        /// <exception cref="InvalidOperationException">The entity is an <see cref="ImfMultipart"/> or <see cref="ImfMessagePart"/>, which has no content of its own to set.</exception>
        public void SetContent(ReadOnlyMemory<byte> content)
        {
            CheckCanSetContent();
            if (content.Length > Options.MaxEntitySize)
                throw new ArgumentException($"The content is longer than the maximum of {Options.MaxEntitySize} octets.", nameof(content));

            Fields.RemoveAll(ImfFieldNames.ContentTransferEncoding);
            _content = content;
            _isContentSet = true;
        }

        /// <summary>
        /// Sets the content of the entity to text in UTF-8, as <see cref="SetText(string, MimeCharset)"/> does.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="text"/> is not valid Unicode, or is too long for <see cref="ImfOptions.MaxEntitySize"/>.</exception>
        /// <exception cref="InvalidOperationException">The entity is not of the top-level type <c>text</c>, or is an <see cref="ImfMultipart"/> or <see cref="ImfMessagePart"/>.</exception>
        public void SetText(string text)
        {
            SetText(text, MimeCharset.Utf8);
        }

        /// <summary>
        /// Sets the content of the entity to text, and the <c>charset</c> parameter of its Content-Type field to say what character set it is in (RFC 2046 §4.1.2).
        /// </summary>
        /// <remarks>
        /// Each line break of the text, whether CR, LF or CRLF, becomes a CRLF, the only line break that RFC 2046 §4.1.1 allows in a <c>text</c> entity.  The rest is as
        /// <see cref="SetContent"/> describes.
        /// </remarks>
        /// <param name="text">The text.</param>
        /// <param name="charset">The character set, which <see cref="ImfOptions.CharsetResolver"/> must resolve.</param>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="charset"/> is not initialised or can't be resolved, or <paramref name="text"/> has characters that it can't hold, or
        /// is too long for <see cref="ImfOptions.MaxEntitySize"/>.</exception>
        /// <exception cref="InvalidOperationException">The entity is not of the top-level type <c>text</c>, or is an <see cref="ImfMultipart"/> or <see cref="ImfMessagePart"/>.</exception>
        public void SetText(string text, MimeCharset charset)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            if (charset == default(MimeCharset))
                throw new ArgumentException("The character set is not initialised.", nameof(charset));

            CheckCanSetContent();
            var contentType = ContentType;
            if (!string.Equals(contentType.Type, MimeTopLevelTypes.Text, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only an entity of the top-level type text can have text.");
            if (!Options.CharsetResolver.TryGetEncoding(charset, out var encoding))
                throw new ArgumentException($"The character set '{charset}' can't be resolved.", nameof(charset));

            var canonical = NormalizeLineBreaks(text);
            var bytes = encoding.GetBytes(canonical);
            if (encoding.GetString(bytes, 0, bytes.Length) != canonical)
                throw new ArgumentException($"The text has characters that the character set '{charset}' can't hold.", nameof(text));

            SetContent(bytes);
            ContentType = contentType.WithParameter(CharsetParameterName, charset.Name);
        }

        /// <summary>
        /// Sets what a reader read for the entity, whose fields have been added.
        /// </summary>
        /// <param name="rawEntity">All of the entity as it was read, if <paramref name="hasRawEntity"/> is <c>true</c>.</param>
        /// <param name="hasRawEntity"><c>false</c> if the fields are held somewhere else, as those of a message are.</param>
        /// <param name="rawHeaderEnd">The line break of the empty line that ended the header.</param>
        /// <param name="rawBody">The body.</param>
        /// <param name="defaultContentType">The media type of an entity with no Content-Type field where it was read.</param>
        /// <param name="lineFeedIsLineBreak"><c>true</c> if a line feed is the line break.</param>
        internal void SetRead(ReadOnlyMemory<byte> rawEntity, bool hasRawEntity, ReadOnlyMemory<byte> rawHeaderEnd, ReadOnlyMemory<byte> rawBody, MimeMediaType defaultContentType, bool lineFeedIsLineBreak)
        {
            _rawEntity = rawEntity;
            _hasRawEntity = hasRawEntity;
            _rawHeaderEnd = rawHeaderEnd;
            _rawBody = rawBody;
            _defaultContentType = defaultContentType;
            _lineFeedIsLineBreak = lineFeedIsLineBreak;
            _isContentSet = false;
            _isRead = true;

            var fields = new ImfHeaderField[Fields.Count];
            for (var index = 0; index < fields.Length; index++)
            {
                fields[index] = Fields[index];
            }

            _readFields = fields;
        }

        /// <summary>
        /// Records that the entity is in a place in a tree.
        /// </summary>
        internal void Attach()
        {
            IsAttached = true;
        }

        /// <summary>
        /// Records that the entity has left its place in a tree.  What was read in it was found not to hold the delimiters of that place, and that says nothing about any other.
        /// </summary>
        internal void Detach()
        {
            IsAttached = false;
            _isMoved = true;
        }

        /// <summary>
        /// Makes the fields of a message those of this entity, as its body, without copying them to it, for a message that was read.
        /// </summary>
        internal void ShareFields(ImfHeaderFieldCollection fields)
        {
            Fields = fields;
            IsAttached = true;
        }

        /// <summary>
        /// Makes the entity the body of a message: its fields go to the message, in the place of those whose names start with <c>Content-</c>, and it uses the fields of the message
        /// from now on.
        /// </summary>
        internal void AttachToMessage(ImfHeaderFieldCollection fields)
        {
            for (var index = fields.Count - 1; index >= 0; index--)
            {
                if (fields[index].Name.StartsWith(ContentFieldPrefix, StringComparison.OrdinalIgnoreCase)) fields.RemoveAt(index);
            }

            foreach (var field in Fields)
            {
                fields.Add(field);
            }

            // ref. RFC 2045 §4: a part that was read needs no such field, but the header of a message does, and a writer adds one only to an entity that was not read.
            if (_isRead && !fields.Contains(ImfFieldNames.MimeVersion))
            {
                fields.Add(ImfHeaderField.FromValue(ImfFieldNames.MimeVersion, encoding => "1.0"));
            }

            // The entity as it was read included fields that it no longer holds in the same way.
            _hasRawEntity = false;
            ShareFields(fields);
        }

        /// <summary>
        /// Makes the entity stop being the body of a message: it keeps a copy of the fields that describe it.
        /// </summary>
        internal void DetachFromMessage()
        {
            var fields = new ImfHeaderFieldCollection();
            foreach (var field in Fields)
            {
                if (field.Name.StartsWith(ContentFieldPrefix, StringComparison.OrdinalIgnoreCase)) fields.Add(field);
            }

            Fields = fields;
            Detach();
            _hasRawEntity = false;
        }

        /// <summary>
        /// Determines if the entity holds an object, or is that object, at any depth, so that adding it there would make a tree that holds itself.
        /// </summary>
        internal bool Holds(object target)
        {
            var pending = new Stack<ImfEntity>();
            pending.Push(this);
            while (pending.Count > 0)
            {
                var entity = pending.Pop();
                if (ReferenceEquals(entity, target)) return true;

                if (entity is ImfMultipart multipart)
                {
                    foreach (var part in multipart.Parts)
                    {
                        pending.Push(part);
                    }
                }
                else if (entity is ImfMessagePart messagePart)
                {
                    if (ReferenceEquals(messagePart.Message, target)) return true;

                    var body = messagePart.Message.LoadedBody;
                    if (!(body is null)) pending.Push(body);
                }
            }

            return false;
        }

        /// <summary>
        /// Describes the entity for an <see cref="ImfEntityWriter"/>: what has not changed as the bytes that were read, and the rest as what it now holds.
        /// </summary>
        /// <param name="encoding">How the writer writes header text that is not US-ASCII.</param>
        /// <param name="lineBreak">The line break of the writer.</param>
        /// <exception cref="ImfFileException">The entity can't be written.</exception>
        internal ImfEntitySource ToSource(ImfHeaderEncoding encoding, byte[] lineBreak)
        {
            if (HasChangedBody) return ToChangedSource(encoding, lineBreak);

            ImfEntitySource source;
            if (_hasRawEntity && !HasChangedFields)
            {
                source = ImfEntitySource.FromRaw(_rawEntity);
            }
            else
            {
                // The body is written back as it is, after the fields as they now are.
                var output = new MemoryStream();
                foreach (var bytes in GetFieldOutput(encoding, lineBreak))
                {
                    output.Write(bytes, 0, bytes.Length);
                }

                output.Write(_rawHeaderEnd.ToArray(), 0, _rawHeaderEnd.Length);
                output.Write(_rawBody.ToArray(), 0, _rawBody.Length);
                source = ImfEntitySource.FromRaw(new ReadOnlyMemory<byte>(output.ToArray()));
            }

            source.IsPreserved = !_isMoved;
            return source;
        }

        /// <summary>
        /// Describes an entity whose body has changed.
        /// </summary>
        /// <exception cref="ImfFileException">The entity can't be written.</exception>
        internal virtual ImfEntitySource ToChangedSource(ImfHeaderEncoding encoding, byte[] lineBreak)
        {
            var source = ImfEntitySource.FromContent(ContentType, GetContent());
            AddFields(source, encoding, lineBreak, null);
            KeepHeaderEnd(source);
            return source;
        }

        /// <summary>
        /// Has a writer write the empty line that ended the header as it was read.  A part with no body has none of its own, as it shared the line break of a delimiter, and needs
        /// the writer's now that something comes after its header.
        /// </summary>
        internal void KeepHeaderEnd(ImfEntitySource source)
        {
            if (_isRead && !_rawHeaderEnd.IsEmpty) source.RawHeaderEnd = _rawHeaderEnd;
        }

        /// <summary>
        /// Checks the depth of what has changed in the tree before anything follows it on the call stack, which a tree of any depth that a caller made could overflow.  What has not
        /// changed is written as one run of bytes, so its depth doesn't matter.
        /// </summary>
        /// <exception cref="ImfFileException">The entities that have changed are nested deeper than <see cref="ImfOptions.MaxNestingDepth"/> allows.</exception>
        internal void CheckChangedDepth(int maxNestingDepth)
        {
            var pending = new Stack<KeyValuePair<ImfEntity, int>>();
            pending.Push(new KeyValuePair<ImfEntity, int>(this, 0));
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                if (current.Value > maxNestingDepth)
                    throw new ImfFileException($"The entities are nested more than {maxNestingDepth} levels deep, the most that ImfOptions.MaxNestingDepth allows.");

                if (!current.Key.HasChangedBody) continue;

                if (current.Key is ImfMultipart multipart)
                {
                    foreach (var part in multipart.Parts)
                    {
                        pending.Push(new KeyValuePair<ImfEntity, int>(part, current.Value + 1));
                    }
                }
                else if (current.Key is ImfMessagePart messagePart)
                {
                    var body = messagePart.Message.LoadedBody;
                    if (!(body is null)) pending.Push(new KeyValuePair<ImfEntity, int>(body, current.Value + 1));
                }
            }
        }

        /// <summary>
        /// Adds the fields of the entity to its description, except any with a name.
        /// </summary>
        internal void AddFields(ImfEntitySource source, ImfHeaderEncoding encoding, byte[] lineBreak, [AllowNull] string omittedName)
        {
            var buffer = new byte[1024];
            foreach (var field in Fields)
            {
                if (omittedName is null || !field.HasName(omittedName)) source.AddOutputField(field.GetOutput(encoding, lineBreak, ref buffer));
            }
        }

        /// <summary>
        /// Checks that this type of entity can have a media type.
        /// </summary>
        /// <exception cref="ArgumentException">It can't.</exception>
        internal virtual void CheckContentType(MimeMediaType contentType, string parameterName)
        {
            if (string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only an ImfMultipart can be of the top-level type multipart.", parameterName);
        }

        /// <summary>
        /// Checks that this type of entity can have a transfer encoding.
        /// </summary>
        /// <exception cref="ArgumentException">It can't.</exception>
        internal virtual void CheckTransferEncoding(MimeTransferEncoding encoding, string parameterName)
        {
        }

        /// <summary>
        /// Makes the entity ready for a change of transfer encoding: what was read has to be decoded with the encoding it has now.
        /// </summary>
        /// <exception cref="ImfFileException">It can't be decoded.</exception>
        internal virtual void PrepareForTransferEncoding()
        {
            if (_isContentSet) return;

            _content = Decode(_rawBody);
            _isContentSet = true;
        }

        /// <summary>
        /// Checks that this type of entity can have its content set.
        /// </summary>
        /// <exception cref="InvalidOperationException">It can't.</exception>
        internal virtual void CheckCanSetContent()
        {
        }

        private List<byte[]> GetFieldOutput(ImfHeaderEncoding encoding, byte[] lineBreak)
        {
            var output = new List<byte[]>(Fields.Count);
            var buffer = new byte[1024];
            foreach (var field in Fields)
            {
                output.Add(field.GetOutput(encoding, lineBreak, ref buffer).ToArray());
            }

            return output;
        }

        private ReadOnlyMemory<byte> Decode(ReadOnlyMemory<byte> raw)
        {
            var transferEncoding = ContentTransferEncoding;
            if (transferEncoding.IsIdentity) return raw;

            int bytesConsumed, bytesWritten;
            System.Buffers.OperationStatus status;
            byte[] decoded;
            if (transferEncoding == MimeTransferEncoding.Base64)
            {
                decoded = new byte[MimeBase64.GetMaxDecodedLength(raw.Length)];
                status = new MimeBase64Decoder(MimeBase64Mode.Mime).Decode(raw.Span, decoded, out bytesConsumed, out bytesWritten, true);
            }
            else if (transferEncoding == MimeTransferEncoding.QuotedPrintable)
            {
                decoded = new byte[MimeQuotedPrintable.GetMaxDecodedLength(raw.Length)];
                var options = Options.AcceptBareLineFeed || _lineFeedIsLineBreak ? MimeQuotedPrintableDecoderOptions.AllowBareLineFeeds : MimeQuotedPrintableDecoderOptions.None;
                status = new MimeQuotedPrintableDecoder(MimeQuotedPrintableMode.Body, options).Decode(raw.Span, decoded, out bytesConsumed, out bytesWritten, true);
            }
            else
            {
                throw new ImfFileException($"The transfer encoding '{transferEncoding.Name}' is an extension mechanism, which can't be decoded (RFC 2045 §6.3).");
            }

            if (status != System.Buffers.OperationStatus.Done)
                throw new ImfFileException($"The body is not valid {transferEncoding.Name} (RFC 2045 §6.7 and §6.8).");

            return new ReadOnlyMemory<byte>(decoded, 0, bytesWritten);
        }

        private MimeCharset GetCharset(MimeMediaType contentType)
        {
            if (contentType.TryGetParameter(CharsetParameterName, out var parameter))
            {
                if (!MimeCharset.TryParse(parameter.Value, out var charset))
                    throw new ImfFileException($"The character set '{parameter.Value}' is not a valid name (RFC 2978 §2.3).");

                return charset;
            }

            // ref. RFC 2046 §4.1.2: US-ASCII is what text with no charset parameter is taken to be, where its type has no default of its own (RFC 6657 §3).
            return MimeCharset.TryGetDefault(contentType, out var defaultCharset) ? defaultCharset : MimeCharset.UsAscii;
        }

        private string DecodeText(ReadOnlyMemory<byte> content, MimeCharset charset)
        {
            if (!Options.CharsetResolver.TryGetEncoding(charset, out var encoding))
                throw new ImfFileException($"The character set '{charset}' can't be resolved.");

            var bytes = GetArray(content, out var offset);
            if (!TryDecodeText(encoding, bytes, offset, content.Length, Options.AllowInvalidTextOctets, out var text))
                throw new ImfFileException($"The content is not valid in the character set '{charset}' (RFC 2046 §4.1.2).");

            return text;
        }

        private static byte[] GetArray(ReadOnlyMemory<byte> memory, out int offset)
        {
            if (MemoryMarshal.TryGetArray(memory, out var segment) && !(segment.Array is null))
            {
                offset = segment.Offset;
                return segment.Array;
            }

            offset = 0;
            return memory.ToArray();
        }

        private static bool TryDecodeText(Encoding encoding, byte[] bytes, int offset, int count, bool allowInvalidOctets, out string text)
        {
            text = string.Empty;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP
            var decoding = (Encoding)encoding.Clone();
            decoding.DecoderFallback = allowInvalidOctets ? new DecoderReplacementFallback(TextReplacementCharacter) : DecoderFallback.ExceptionFallback;
            try
            {
                text = decoding.GetString(bytes, offset, count);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
#else
            // TODO: netstandard1.1 has no Encoding.Clone or DecoderFallback, so the octets are read as the encoding reads them, and then checked by writing the text again.  Decode
            // strictly, as the other builds do, if netstandard1.1 is dropped.
            var decoded = encoding.GetString(bytes, offset, count);
            if (!allowInvalidOctets && !EndsWith(bytes, offset, count, encoding.GetBytes(decoded))) return false;

            text = decoded;
            return true;
#endif
        }

#if !(NETSTANDARD2_0_OR_GREATER || NETCOREAPP)
        // A byte order mark that was read isn't written again, so only the end of the bytes has to match.  It is at most 4 octets.
        private static bool EndsWith(byte[] bytes, int offset, int count, byte[] written)
        {
            var start = count - written.Length;
            if (start < 0 || start > 4) return false;

            for (var index = 0; index < written.Length; index++)
            {
                if (written[index] != bytes[offset + start + index]) return false;
            }

            return true;
        }
#endif

        private static string NormalizeLineBreaks(string text)
        {
            var output = new StringBuilder(text.Length);
            for (var index = 0; index < text.Length; index++)
            {
                var value = text[index];
                if (value == '\r')
                {
                    if (index + 1 < text.Length && text[index + 1] == '\n') index++;

                    output.Append("\r\n");
                }
                else if (value == '\n')
                {
                    output.Append("\r\n");
                }
                else
                {
                    output.Append(value);
                }
            }

            return output.ToString();
        }
    }
}
