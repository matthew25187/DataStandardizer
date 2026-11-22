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

        private static readonly byte[] CrLf = { 0x0D, 0x0A };
        private static readonly byte[] Lf = { 0x0A };

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

            _message.Body.CheckChangedDepth(Options.MaxNestingDepth);

            var output = new MemoryStream();
            using (var writer = new ImfEntityWriter(output, Options, ImfEntityWriter.DefaultBlockSize, LineFeedIsLineBreak, ImfEntityWriter.GenerateBoundary))
            {
                writer.WriteMessage(GetMessageSource(Options, LineFeedIsLineBreak ? Lf : CrLf, ImfEntityWriter.GenerateBoundary));
            }

            return output.ToArray();
        }

        /// <summary>
        /// Describes the message for a writer whose line break is <paramref name="lineBreak"/>.  A message with other line breaks, such as one read from an mbox or EMLX file that
        /// is held by a message with CRLF line breaks, is written with its own, and then changed to those of the writer (RFC 5322 §2.1), as a writer changes such a message on its
        /// own.  It can't be changed if it has a <c>binary</c> entity (RFC 2045 §2.9), or one with an extension mechanism (§6.3), and a body whose CR and LF octets are data isn't
        /// labelled <c>7bit</c> or <c>8bit</c>.  The caller has checked the depth of what has changed
        /// (<see cref="ImfEntity.CheckChangedDepth"/>), which takes in the message.
        /// </summary>
        /// <exception cref="ImfFileException">The message can't be written, or can't change its line breaks.</exception>
        private ImfEntitySource GetMessageSource(ImfOptions options, byte[] lineBreak, Func<string> boundaryGenerator)
        {
            var toCrLf = lineBreak.Length == CrLf.Length;
            if (_message.LineFeedIsLineBreak != toCrLf) return _message.Body.ToSource(options, lineBreak, boundaryGenerator);

            // The line breaks change after the message is written, so the writer labels nothing in it binary, and refuses what is.  It writes under the options of the writer that
            // the message is for, as that writer would have.
            var output = new MemoryStream();
            int heldEntityCount;
            using (var writer = new ImfEntityWriter(options, new ImfOutput(output, false, nameof(ImfEntityWriter)), ImfEntityWriter.DefaultBlockSize, _message.LineFeedIsLineBreak, true,
                boundaryGenerator))
            {
                writer.WriteMessage(_message.Body.ToSource(options, _message.LineFeedIsLineBreak ? Lf : CrLf, boundaryGenerator));
                heldEntityCount = (int)(writer.EntitiesWritten - 1);
            }

            // What it holds is now other bytes than were read, so it is checked again as the writer's own, and what it holds counts toward ImfOptions.MaxPartCount as if the writer
            // had written it.
            var source = ImfEntitySource.FromRaw(ImfLineBreaks.Convert(_message, output.ToArray(), toCrLf));
            source.HeldEntityCount = heldEntityCount;
            return source;
        }

        /// <inheritdoc />
        internal override ImfEntitySource ToChangedSource(ImfOptions options, byte[] lineBreak, Func<string> boundaryGenerator)
        {
            var contentType = ContentType;
            if (!IsMessageType(contentType))
                throw new ImfFileException("The Content-Type field of an entity that holds a message must be message/rfc822 or message/global.");

            var source = ImfEntitySource.FromMessage(contentType, GetMessageSource(options, lineBreak, boundaryGenerator));
            AddFields(source, options.HeaderEncoding, lineBreak, null);
            if (IsRead)
            {
                KeepHeaderEnd(source);
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
