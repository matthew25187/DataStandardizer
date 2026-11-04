using System;
using System.IO;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// An entity that holds a message (RFC 2046 §5.2.1, RFC 6532 §3.7), such as one that is being forwarded.
    /// </summary>
    /// <remarks>
    /// <see cref="ImfEntity.GetContent"/> gives the bytes of the whole message.
    /// </remarks>
    public sealed class ImfMessagePart : ImfEntity
    {
        private const string GlobalSubtype = "global";
        private const string Rfc822Subtype = "rfc822";

        private ImfMessage _message;
        private ImfMessage _readMessage;

        /// <summary>
        /// Creates an entity of type <c>message/rfc822</c> that holds a message, whose accessors use the options of the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        public ImfMessagePart(ImfMessage message) : base(GetOptions(message), MimeMediaType.MessageRfc822)
        {
            _message = message;
            _readMessage = message;
        }

        /// <summary>
        /// Creates an entity for what a reader read, whose fields are added afterwards.
        /// </summary>
        internal ImfMessagePart(ImfMessage message, MimeMediaType defaultContentType) : base(message.Options, defaultContentType)
        {
            _message = message;
            _readMessage = message;
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <exception cref="ArgumentNullException">On set: the value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">On set: the message holds this entity.</exception>
        public ImfMessage Message
        {
            get => _message;
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));

                var body = value.LoadedBody;
                if (!(body is null) && body.Holds(this))
                    throw new ArgumentException("An entity can't hold a message that holds it.", nameof(value));

                _message = value;
            }
        }

        /// <inheritdoc />
        internal override bool HasChangedBody => !IsRead || !ReferenceEquals(_message, _readMessage) || _message.HasChanged;

        /// <inheritdoc />
        public override ReadOnlyMemory<byte> GetContent()
        {
            if (!HasChangedBody) return RawBody;

            var output = new MemoryStream();
            using (var writer = new ImfEntityWriter(output, Options))
            {
                writer.WriteMessage(_message.Body.ToSource(Options.HeaderEncoding, LineFeedIsLineBreak ? new byte[] { 0x0A } : new byte[] { 0x0D, 0x0A }));
            }

            return output.ToArray();
        }

        /// <inheritdoc />
        internal override ImfEntitySource ToChangedSource(ImfHeaderEncoding encoding, byte[] lineBreak)
        {
            var contentType = ContentType;
            if (!IsMessageType(contentType))
                throw new ImfFileException("The Content-Type field of an entity that holds a message must be message/rfc822 or message/global.");

            var source = ImfEntitySource.FromMessage(contentType, _message.Body.ToSource(encoding, lineBreak));
            AddFields(source, encoding, lineBreak, null);
            if (IsRead)
            {
                source.RawHeaderEnd = RawHeaderEnd;
                source.IsPreserved = true;
            }

            return source;
        }

        /// <inheritdoc />
        internal override void CheckContentType(MimeMediaType contentType, string parameterName)
        {
            if (!IsMessageType(contentType))
                throw new ArgumentException("Expected a media type of message/rfc822 or message/global.", parameterName);
        }

        /// <inheritdoc />
        internal override void CheckTransferEncoding(MimeTransferEncoding encoding, string parameterName)
        {
            // ref. RFC 2045 §6.4: a composite entity has no transfer encoding of its own, only a label for what it holds.
            if (encoding != default(MimeTransferEncoding) && !encoding.IsIdentity)
                throw new ArgumentException("An entity that holds a message can have only an identity transfer encoding (RFC 2045 §6.4).", parameterName);
        }

        /// <inheritdoc />
        internal override void PrepareForTransferEncoding()
        {
            // The label says nothing about how the message is written.
        }

        /// <inheritdoc />
        internal override void CheckCanSetContent()
        {
            throw new InvalidOperationException("An entity that holds a message has no content of its own to set.  Set its Message.");
        }

        private static bool IsMessageType(MimeMediaType contentType)
        {
            return string.Equals(contentType.Type, MimeTopLevelTypes.Message, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(contentType.Subtype, Rfc822Subtype, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(contentType.Subtype, GlobalSubtype, StringComparison.OrdinalIgnoreCase));
        }

        private static ImfOptions GetOptions(ImfMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return message.Options;
        }
    }
}
