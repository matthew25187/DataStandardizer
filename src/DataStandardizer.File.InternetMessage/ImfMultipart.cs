using System;
using System.Collections.Generic;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A multipart entity (RFC 2046 §5.1): an ordered list of entities, with a preamble and an epilogue.
    /// </summary>
    /// <remarks>
    /// <para>
    /// While its parts stay the same, and its boundary, preamble and epilogue are not changed, a writer writes its delimiter lines as they were read, even if a part has changed.
    /// Otherwise it writes new ones, and it uses the <c>boundary</c> parameter if there is one, or else generates a boundary each time it writes.
    /// </para>
    /// <para>
    /// A multipart entity has parts and no content of its own, so <see cref="ImfEntity.GetContent"/> and the members that use it throw <see cref="InvalidOperationException"/>.
    /// </para>
    /// </remarks>
    public sealed class ImfMultipart : ImfEntity
    {
        private const string BoundaryParameterName = "boundary";
        private const byte Hyphen = 0x2D;

        private ReadOnlyMemory<byte>? _epilogue;
        private bool _isFramingChanged = true;
        private ReadOnlyMemory<byte>? _preamble;
        private ReadOnlyMemory<byte>[] _rawFraming = new ReadOnlyMemory<byte>[0];
        private string _readBoundary = string.Empty;
        private ImfEntity[] _readParts = new ImfEntity[0];

        /// <summary>
        /// Creates a multipart entity with no parts, whose accessors use the default options.
        /// </summary>
        /// <param name="contentType">The media type, which must be of the top-level type <c>multipart</c>.  Without a <c>boundary</c> parameter, a writer generates a boundary.</param>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised or not <c>multipart</c>.</exception>
        public ImfMultipart(MimeMediaType contentType) : this(contentType, new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a multipart entity with no parts.
        /// </summary>
        /// <param name="contentType">The media type, which must be of the top-level type <c>multipart</c>.  Without a <c>boundary</c> parameter, a writer generates a boundary.</param>
        /// <param name="options">The options that the accessors parse fields under.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised or not <c>multipart</c>, or <paramref name="options"/> has no
        /// <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/> that is needed and can't be resolved.</exception>
        public ImfMultipart(MimeMediaType contentType, ImfOptions options) : base(options, MimeMediaType.MultipartMixed)
        {
            Parts = new ImfEntityCollection(this);
            ContentType = contentType;
        }

        /// <summary>
        /// Creates a multipart entity for what a reader read, whose fields are added and whose parts are given afterwards.
        /// </summary>
        internal ImfMultipart(ImfOptions options) : base(options, MimeMediaType.MultipartMixed)
        {
            Parts = new ImfEntityCollection(this);
        }

        /// <summary>
        /// Gets the parts, in order.
        /// </summary>
        public ImfEntityCollection Parts { get; }

        /// <summary>
        /// Gets or sets the part of the entity before its first delimiter (RFC 2046 §5.1.1), or <c>null</c> for none.  It doesn't include the line break that belongs to the delimiter.
        /// </summary>
        /// <remarks>
        /// The memory must not change until a write has finished.  Setting this means that the delimiter lines are written again.
        /// </remarks>
        public ReadOnlyMemory<byte>? Preamble
        {
            get => _preamble;
            set
            {
                _preamble = value;
                _isFramingChanged = true;
            }
        }

        /// <summary>
        /// Gets or sets the part of the entity after its close delimiter (RFC 2046 §5.1.1), or <c>null</c> for none.  It doesn't include the line break after that delimiter, so an empty
        /// epilogue is just that line break.
        /// </summary>
        /// <remarks>
        /// The memory must not change until a write has finished.  Setting this means that the delimiter lines are written again.
        /// </remarks>
        public ReadOnlyMemory<byte>? Epilogue
        {
            get => _epilogue;
            set
            {
                _epilogue = value;
                _isFramingChanged = true;
            }
        }

        /// <inheritdoc />
        internal override bool HasChangedBody
        {
            get
            {
                if (!IsFramingKept) return true;

                foreach (var part in Parts)
                {
                    if (part.HasChangedFields || part.HasChangedBody) return true;
                }

                return false;
            }
        }

        private bool IsFramingKept
        {
            get
            {
                if (_isFramingChanged || GetBoundary() != _readBoundary || Parts.Count != _readParts.Length) return false;

                for (var index = 0; index < _readParts.Length; index++)
                {
                    if (!ReferenceEquals(Parts[index], _readParts[index])) return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Has no content to give: a multipart entity has parts.
        /// </summary>
        /// <returns>Nothing.</returns>
        /// <exception cref="InvalidOperationException">Always.</exception>
        public override ReadOnlyMemory<byte> GetContent()
        {
            throw new InvalidOperationException("A multipart entity has parts and no content of its own.");
        }

        /// <summary>
        /// Completes a multipart entity that a reader read.
        /// </summary>
        /// <param name="rawFraming">The bytes between the header and the first part, between each two parts, and after the last.</param>
        /// <param name="preamble">The preamble, or <c>null</c> for none.</param>
        /// <param name="epilogue">The epilogue, or <c>null</c> for none.</param>
        internal void SetReadFraming(ReadOnlyMemory<byte>[] rawFraming, ReadOnlyMemory<byte>? preamble, ReadOnlyMemory<byte>? epilogue)
        {
            _rawFraming = rawFraming;
            _preamble = preamble;
            _epilogue = epilogue;
            _readBoundary = GetBoundary();
            _isFramingChanged = false;
            _readParts = new ImfEntity[Parts.Count];
            for (var index = 0; index < _readParts.Length; index++)
            {
                _readParts[index] = Parts[index];
            }
        }

        /// <inheritdoc />
        internal override ImfEntitySource ToChangedSource(ImfHeaderEncoding encoding, byte[] lineBreak)
        {
            var contentType = ContentType;
            if (!string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase))
                throw new ImfFileException("The Content-Type field of a multipart entity must be of the top-level type multipart.");
            if (Parts.Count == 0)
                throw new ImfFileException("A multipart entity must have at least one part (RFC 2046 §5.1.1).");

            var parts = new List<ImfEntitySource>(Parts.Count);
            foreach (var part in Parts)
            {
                parts.Add(part.ToSource(encoding, lineBreak));
            }

            var source = ImfEntitySource.FromParts(contentType, parts);

            // A writer adds the Content-Type field itself, with a boundary, for an entity that has none yet.
            AddFields(source, encoding, lineBreak, contentType.TryGetParameter(BoundaryParameterName, out _) ? null : ImfFieldNames.ContentType);
            if (IsFramingKept)
            {
                source.RawFraming = GetFramingToWrite(lineBreak);
                source.RawHeaderEnd = RawHeaderEnd;
                source.IsPreserved = true;
            }
            else
            {
                source.Preamble = _preamble;
                source.Epilogue = _epilogue;
            }

            return source;
        }

        /// <inheritdoc />
        internal override void CheckContentType(MimeMediaType contentType, string parameterName)
        {
            if (!string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Expected a media type of the top-level type multipart.", parameterName);
        }

        /// <inheritdoc />
        internal override void CheckTransferEncoding(MimeTransferEncoding encoding, string parameterName)
        {
            // ref. RFC 2045 §6.4: a composite entity has no transfer encoding of its own, only a label for what it holds.
            if (encoding != default(MimeTransferEncoding) && !encoding.IsIdentity)
                throw new ArgumentException("A multipart entity can have only an identity transfer encoding (RFC 2045 §6.4).", parameterName);
        }

        /// <inheritdoc />
        internal override void PrepareForTransferEncoding()
        {
            // The label says nothing about how the parts are written.
        }

        /// <inheritdoc />
        internal override void CheckCanSetContent()
        {
            throw new InvalidOperationException("A multipart entity has parts and no content of its own.");
        }

        private string GetBoundary()
        {
            return ContentType.TryGetParameter(BoundaryParameterName, out var boundary) ? boundary.Value : string.Empty;
        }

        /// <summary>
        /// Gets the bytes to write around the parts.  A multipart entity that was read with no close delimiter, which <see cref="ImfOptions.AllowMissingBoundaryTerminator"/> allows, gets
        /// one here: this entity is being written again, and RFC 2046 §5.1.1 requires it.
        /// </summary>
        private ReadOnlyMemory<byte>[] GetFramingToWrite(byte[] lineBreak)
        {
            if (!_rawFraming[_rawFraming.Length - 1].IsEmpty) return _rawFraming;

            var boundary = _readBoundary;
            var close = new byte[lineBreak.Length + boundary.Length + 4];
            lineBreak.CopyTo(close, 0);
            close[lineBreak.Length] = Hyphen;
            close[lineBreak.Length + 1] = Hyphen;
            for (var index = 0; index < boundary.Length; index++)
            {
                close[lineBreak.Length + 2 + index] = (byte)boundary[index];
            }

            close[close.Length - 2] = Hyphen;
            close[close.Length - 1] = Hyphen;

            var framing = (ReadOnlyMemory<byte>[])_rawFraming.Clone();
            framing[framing.Length - 1] = close;
            return framing;
        }
    }
}
