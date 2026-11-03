using System;
using System.Collections.Generic;
using System.IO;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Describes one entity for an <see cref="ImfEntityWriter"/> to write: the fields and the content of a message or a part, or the raw bytes of one that
    /// has not changed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A writer has to see all of a body to choose its transfer encoding, which comes before it in the output, and all of a multipart entity to choose a
    /// boundary, which comes before its parts.  So it is given the whole entity at once, as a tree of these objects, and it reads a body from a stream
    /// without holding it in memory.
    /// </para>
    /// <para>
    /// A stream is read from its current position.  It is read at least twice if the writer has to look at its content, so it must support seeking.  A stream
    /// that doesn't can be used only where nothing needs checking: as the content of an entity with a <see cref="TransferEncoding"/> of <c>base64</c>, or
    /// of <c>quoted-printable</c> if every enclosing boundary holds <c>=_</c>, or as the raw bytes of a whole message.  The writer leaves a stream open, at
    /// the end of its data.
    /// </para>
    /// <para>
    /// The memory and streams must not change until the write has finished.
    /// </para>
    /// </remarks>
    public sealed class ImfEntitySource
    {
        private static readonly ImfEntitySource[] NoChildren = new ImfEntitySource[0];

        private readonly List<FieldEntry> _fields = new List<FieldEntry>();

        private ReadOnlyMemory<byte>? _epilogue;
        private ReadOnlyMemory<byte>? _preamble;
        private MimeTransferEncoding _transferEncoding;

        private ImfEntitySource(SourceKind kind, MimeMediaType contentType, ReadOnlyMemory<byte> content, Stream contentStream, IReadOnlyList<ImfEntitySource> children)
        {
            Kind = kind;
            ContentType = contentType;
            Content = content;
            ContentStream = contentStream;
            Children = children;
        }

        /// <summary>
        /// What an entity is made from.
        /// </summary>
        internal enum SourceKind
        {
            Content,
            Multipart,
            Message,
            Raw,
        }

        /// <summary>
        /// Gets or sets the part of a multipart entity before its first delimiter (RFC 2046 §5.1.1), or <c>null</c> for none.  It is written without the line
        /// break that separates it from the delimiter.
        /// </summary>
        /// <exception cref="InvalidOperationException">On set: the entity is not a multipart entity made with <see cref="FromParts"/>.</exception>
        public ReadOnlyMemory<byte>? Preamble
        {
            get => _preamble;
            set
            {
                CheckIsMultipart();
                _preamble = value;
            }
        }

        /// <summary>
        /// Gets or sets the part of a multipart entity after its close delimiter (RFC 2046 §5.1.1), or <c>null</c> for none.  It is written without the line
        /// break that separates it from the delimiter, which only an entity with an epilogue has, so an empty epilogue gives just the line break.
        /// </summary>
        /// <exception cref="InvalidOperationException">On set: the entity is not a multipart entity made with <see cref="FromParts"/>.</exception>
        public ReadOnlyMemory<byte>? Epilogue
        {
            get => _epilogue;
            set
            {
                CheckIsMultipart();
                _epilogue = value;
            }
        }

        /// <summary>
        /// Gets or sets the transfer encoding to use, which overrides the writer's choice.  The default value of <see cref="MimeTransferEncoding"/> leaves
        /// the choice to the writer.
        /// </summary>
        /// <remarks>
        /// The writer refuses an encoding that doesn't fit: <c>7bit</c> or <c>8bit</c> for data outside that domain (RFC 2045 §2.7 and §2.8), or anything but
        /// an identity encoding for a multipart or message entity (RFC 2045 §6.4).  An extension encoding can't be written, as the writer can't apply it.  For
        /// that, write the entity with <see cref="FromRaw(ReadOnlyMemory{byte})"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">On set: the entity is written from its raw bytes.</exception>
        public MimeTransferEncoding TransferEncoding
        {
            get => _transferEncoding;
            set
            {
                if (Kind == SourceKind.Raw)
                    throw new InvalidOperationException("An entity written from its raw bytes has no transfer encoding to set.");

                _transferEncoding = value;
            }
        }

        internal SourceKind Kind { get; }

        internal MimeMediaType ContentType { get; }

        internal ReadOnlyMemory<byte> Content { get; }

        /// <summary>
        /// Gets the stream of the content, or <see cref="Stream.Null"/> if the content is in <see cref="Content"/>.
        /// </summary>
        internal Stream ContentStream { get; }

        internal IReadOnlyList<ImfEntitySource> Children { get; }

        internal IReadOnlyList<FieldEntry> Fields => _fields;

        /// <summary>
        /// Describes an entity with content in memory.
        /// </summary>
        /// <param name="contentType">The media type, which is written as the Content-Type field.  Say what character set any text is in, with a
        /// <c>charset</c> parameter.</param>
        /// <param name="content">The content, before any transfer encoding.  A writer gives it back unchanged after decoding, so give text with CRLF line
        /// breaks (RFC 2046 §4.1.1).</param>
        /// <returns>The entity.</returns>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised.</exception>
        public static ImfEntitySource FromContent(MimeMediaType contentType, ReadOnlyMemory<byte> content)
        {
            CheckInitialized(contentType);
            return new ImfEntitySource(SourceKind.Content, contentType, content, Stream.Null, NoChildren);
        }

        /// <summary>
        /// Describes an entity with content in a stream, which is read when the entity is written.
        /// </summary>
        /// <param name="contentType">The media type, which is written as the Content-Type field.  Say what character set any text is in, with a
        /// <c>charset</c> parameter.</param>
        /// <param name="content">The stream of the content, before any transfer encoding.  A writer gives it back unchanged after decoding, so give text with
        /// CRLF line breaks (RFC 2046 §4.1.1).  The exception is <c>quoted-printable</c> text from a stream that doesn't support seeking, as it can't be
        /// checked first: a bare line feed in it is written as a line break.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised, or <paramref name="content"/> does not support
        /// reading.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="content"/> is <c>null</c>.</exception>
        public static ImfEntitySource FromContent(MimeMediaType contentType, Stream content)
        {
            CheckInitialized(contentType);
            CheckStream(content, nameof(content));
            return new ImfEntitySource(SourceKind.Content, contentType, default(ReadOnlyMemory<byte>), content, NoChildren);
        }

        /// <summary>
        /// Describes a multipart entity (RFC 2046 §5.1).
        /// </summary>
        /// <param name="contentType">The media type, which must be of the top-level type <c>multipart</c>.  Without a <c>boundary</c> parameter, the writer
        /// generates a boundary, and picks another if it collides with anything.  With one, the writer checks it, and refuses to write if it collides.</param>
        /// <param name="parts">The parts, of which there must be at least one.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised or not <c>multipart</c>, or <paramref name="parts"/> is empty or
        /// contains <c>null</c>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="parts"/> is <c>null</c>.</exception>
        public static ImfEntitySource FromParts(MimeMediaType contentType, IEnumerable<ImfEntitySource> parts)
        {
            CheckInitialized(contentType);
            if (!string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Expected a media type of the top-level type multipart.", nameof(contentType));
            if (parts is null)
                throw new ArgumentNullException(nameof(parts));

            var children = new List<ImfEntitySource>();
            foreach (var part in parts)
            {
                if (part is null)
                    throw new ArgumentException("The parts cannot contain null.", nameof(parts));

                children.Add(part);
            }

            // ref. RFC 2046 §5.1.1: multipart-body has at least one body-part.
            if (children.Count == 0)
                throw new ArgumentException("A multipart entity must have at least one part.", nameof(parts));

            return new ImfEntitySource(SourceKind.Multipart, contentType, default(ReadOnlyMemory<byte>), Stream.Null, children);
        }

        /// <summary>
        /// Describes an entity of type <c>message/rfc822</c> that holds a message (RFC 2046 §5.2.1).
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        public static ImfEntitySource FromMessage(ImfEntitySource message)
        {
            return FromMessage(MimeMediaType.MessageRfc822, message);
        }

        /// <summary>
        /// Describes an entity that holds a message (RFC 2046 §5.2.1, RFC 6532 §3.7).
        /// </summary>
        /// <param name="contentType">The media type, which must be <c>message/rfc822</c> or <c>message/global</c>.</param>
        /// <param name="message">The message.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised, or is another media type.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        public static ImfEntitySource FromMessage(MimeMediaType contentType, ImfEntitySource message)
        {
            CheckInitialized(contentType);
            if (!string.Equals(contentType.Type, MimeTopLevelTypes.Message, StringComparison.OrdinalIgnoreCase)
                || (!string.Equals(contentType.Subtype, "rfc822", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(contentType.Subtype, "global", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("Expected a media type of message/rfc822 or message/global.", nameof(contentType));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return new ImfEntitySource(SourceKind.Message, contentType, default(ReadOnlyMemory<byte>), Stream.Null, new[] { message });
        }

        /// <summary>
        /// Describes an entity that has not changed, by its raw bytes, which are copied byte for byte.  This keeps a signature over it valid.
        /// </summary>
        /// <remarks>
        /// With an <see cref="ImfEntityReader"/>, the raw bytes are the range of the data that an <see cref="ImfEntityEventKind.EntityEnd"/> event covers.  They
        /// don't include the line break before the delimiter that follows a part.  They are not checked, except against the enclosing boundaries.
        /// </remarks>
        /// <param name="rawBytes">The raw bytes.</param>
        /// <returns>The entity.</returns>
        public static ImfEntitySource FromRaw(ReadOnlyMemory<byte> rawBytes)
        {
            return new ImfEntitySource(SourceKind.Raw, default(MimeMediaType), rawBytes, Stream.Null, NoChildren);
        }

        /// <summary>
        /// Describes an entity that has not changed, by a stream of its raw bytes, which are copied byte for byte when the entity is written.  This keeps a
        /// signature over it valid.
        /// </summary>
        /// <remarks>
        /// The stream is read to its end, so to copy a part of a larger stream, give a stream of just that part.  The bytes are not checked, except against
        /// the enclosing boundaries.
        /// </remarks>
        /// <param name="rawBytes">The stream of the raw bytes.</param>
        /// <returns>The entity.</returns>
        /// <exception cref="ArgumentException"><paramref name="rawBytes"/> does not support reading.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="rawBytes"/> is <c>null</c>.</exception>
        public static ImfEntitySource FromRaw(Stream rawBytes)
        {
            CheckStream(rawBytes, nameof(rawBytes));
            return new ImfEntitySource(SourceKind.Raw, default(MimeMediaType), default(ReadOnlyMemory<byte>), rawBytes, NoChildren);
        }

        /// <summary>
        /// Adds a field to the header of the entity, to be folded as its kind allows, in the way <see cref="ImfHeaderWriter"/> describes.
        /// </summary>
        /// <remarks>
        /// The writer adds the Content-Type and Content-Transfer-Encoding fields itself, after these fields, and <c>MIME-Version: 1.0</c> to the header of a
        /// message, unless one is added here.  One added here must agree with the entity: a Content-Type field with the media type it was made with, and a
        /// Content-Transfer-Encoding field with its <see cref="TransferEncoding"/>, for which the field then asks if that has not been set.  This keeps the
        /// header of a multipart entity unchanged when only its parts have changed.  The field is checked when the entity is written.
        /// </remarks>
        /// <param name="name">The field name.</param>
        /// <param name="value">The unfolded field body.  One space is written before it if it doesn't start with a space or tab, and isn't empty.</param>
        /// <param name="kind">How the field may be folded.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not an <see cref="ImfHeaderFieldKind"/> member.</exception>
        /// <exception cref="ImfFileException"><paramref name="name"/> or <paramref name="value"/> is not valid Unicode.</exception>
        /// <exception cref="InvalidOperationException">The entity is written from its raw bytes.</exception>
        public ImfEntitySource AddField(string name, string value, ImfHeaderFieldKind kind)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (kind != ImfHeaderFieldKind.Unstructured && kind != ImfHeaderFieldKind.Structured)
                throw new ArgumentOutOfRangeException(nameof(kind));

            CheckHasFields();
            _fields.Add(new FieldEntry(ImfHeaderFormatter.GetBytes(name), ImfHeaderFormatter.GetBytes(value), kind));
            return this;
        }

        /// <summary>
        /// Adds a field to the header of the entity, copying what a reader has just read, to be written byte for byte.
        /// </summary>
        /// <remarks>
        /// The rules that <see cref="AddField(string, string, ImfHeaderFieldKind)"/> gives for the fields a writer adds itself apply here too.
        /// </remarks>
        /// <param name="field">The field.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ObjectDisposedException">The reader of the field has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The entity is written from its raw bytes.
        /// -or-
        /// The reader has moved on from the field, or the view was not obtained from a reader.</exception>
        public ImfEntitySource AddField(ImfHeaderFieldView field)
        {
            CheckHasFields();
            _fields.Add(new FieldEntry(field.RawBytes.ToArray(), false));
            return this;
        }

        /// <summary>
        /// Adds the raw bytes of a field to the header of the entity, to be written byte for byte, including its folding and its final line break.
        /// </summary>
        /// <remarks>
        /// The rules that <see cref="AddField(string, string, ImfHeaderFieldKind)"/> gives for the fields a writer adds itself apply here too.  When the entity
        /// is written, the bytes are checked in the way that <see cref="ImfHeaderWriter.WriteRawField"/> describes.
        /// </remarks>
        /// <param name="rawField">The bytes of the field.</param>
        /// <returns>This object.</returns>
        /// <exception cref="InvalidOperationException">The entity is written from its raw bytes.</exception>
        public ImfEntitySource AddRawField(ReadOnlyMemory<byte> rawField)
        {
            CheckHasFields();
            _fields.Add(new FieldEntry(rawField.ToArray(), true));
            return this;
        }

        private void CheckHasFields()
        {
            if (Kind == SourceKind.Raw)
                throw new InvalidOperationException("An entity written from its raw bytes already has its fields.");
        }

        private void CheckIsMultipart()
        {
            if (Kind != SourceKind.Multipart)
                throw new InvalidOperationException("Only a multipart entity made with FromParts has a preamble and epilogue.");
        }

        private static void CheckInitialized(MimeMediaType contentType)
        {
            if (contentType == default(MimeMediaType))
                throw new ArgumentException("The media type is not initialised.", nameof(contentType));
        }

        private static void CheckStream(Stream stream, string parameterName)
        {
            if (stream is null)
                throw new ArgumentNullException(parameterName);
            if (!stream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", parameterName);
        }

        /// <summary>
        /// A field of the header of an entity: either a name and body to format, or raw bytes to copy.
        /// </summary>
        internal readonly struct FieldEntry
        {
            internal FieldEntry(byte[] name, byte[] value, ImfHeaderFieldKind kind)
            {
                Name = name;
                Value = value;
                Kind = kind;
                IsRaw = false;
                NeedsCheck = false;
            }

            internal FieldEntry(byte[] rawBytes, bool needsCheck)
            {
                Name = rawBytes;
                Value = rawBytes;
                Kind = ImfHeaderFieldKind.Unstructured;
                IsRaw = true;
                NeedsCheck = needsCheck;
            }

            internal byte[] Name { get; }

            internal byte[] Value { get; }

            internal ImfHeaderFieldKind Kind { get; }

            internal bool IsRaw { get; }

            internal bool NeedsCheck { get; }
        }
    }
}
