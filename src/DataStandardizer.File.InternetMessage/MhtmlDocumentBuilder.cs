using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Builds an aggregate document (RFC 2557) from a root document and its resources: a <c>multipart/related</c> entity whose <c>type</c> and <c>start</c> parameters name the root, with
    /// each part labelled with its URL.
    /// </summary>
    /// <remarks>
    /// The root gets a generated Content-ID for <c>start</c> to name, unless its URL is a <c>cid:</c> URI.  A <c>cid:</c> URL becomes the Content-ID of a part, as RFC 2557 §8.3 doesn't match
    /// one in a Content-Location field, and any other becomes its Content-Location.  The builder doesn't make up a Date or From field: set them on <see cref="Header"/> to send the document as
    /// a message.  A writer generates the boundary, and chooses each transfer encoding.
    /// </remarks>
    public sealed class MhtmlDocumentBuilder
    {
        private const string CidScheme = "cid";
        private const string GeneratedIdDomain = "mhtml.invalid";
        private const string MimeVersion = "1.0";
        private const string StartParameterName = "start";
        private const string TypeParameterName = "type";

        private static readonly MimeMediaType HtmlType = new MimeMediaType(MimeTopLevelTypes.Text, "html");

        private readonly Func<ImfMessageId> _contentIdGenerator;
        private readonly List<Func<ImfEntity>> _resources = new List<Func<ImfEntity>>();

        [AllowNull]
        private Uri _location;
        [AllowNull]
        private Func<ImfEntity> _root;
        [AllowNull]
        private Uri _rootLocation;
        private MimeMediaType _rootType;

        /// <summary>
        /// Creates a builder that uses the default options.
        /// </summary>
        public MhtmlDocumentBuilder() : this(new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a builder.
        /// </summary>
        /// <param name="options">The options of the message and its entities.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/>
        /// that is needed and can't be resolved.</exception>
        public MhtmlDocumentBuilder(ImfOptions options) : this(options, GenerateContentId)
        {
        }

        /// <summary>
        /// Creates a builder with a source of Content-IDs.  Allows tests to choose the Content-ID of the root.
        /// </summary>
        internal MhtmlDocumentBuilder(ImfOptions options, Func<ImfMessageId> contentIdGenerator)
        {
            Header = new ImfMessage(options);
            _contentIdGenerator = contentIdGenerator;
        }

        /// <summary>
        /// Gets the message whose fields are copied to the header of the message that is built, so that they can be set with its accessors.  Its body is not used, and a field whose
        /// name starts with <c>Content-</c> gives way to those of the body.
        /// </summary>
        public ImfMessage Header { get; }

        /// <summary>
        /// Sets the URI of the whole aggregate, which is written as the Content-Location of the <c>multipart/related</c> entity, and is the base of the relative URIs in the document
        /// (RFC 2557 §4.3 and §5).
        /// </summary>
        /// <param name="location">The absolute URI.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="location"/> is not absolute.</exception>
        public MhtmlDocumentBuilder SetLocation(Uri location)
        {
            if (location is null)
                throw new ArgumentNullException(nameof(location));

            MhtmlDocument.CheckBaseUri(location, nameof(location));
            _location = location;
            return this;
        }

        /// <summary>
        /// Sets the root document to HTML in UTF-8.
        /// </summary>
        /// <param name="location">The URL of the document, absolute or relative.</param>
        /// <param name="html">The HTML.  Each line break becomes a CRLF (RFC 2046 §4.1.1).</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> or <paramref name="html"/> is <c>null</c>.</exception>
        public MhtmlDocumentBuilder SetHtml(Uri location, string html)
        {
            return SetHtml(location, html, MimeCharset.Utf8);
        }

        /// <summary>
        /// Sets the root document to HTML.
        /// </summary>
        /// <param name="location">The URL of the document, absolute or relative.</param>
        /// <param name="html">The HTML.  Each line break becomes a CRLF (RFC 2046 §4.1.1).</param>
        /// <param name="charset">The character set to write it in, which is checked by <see cref="Build"/>.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> or <paramref name="html"/> is <c>null</c>.</exception>
        public MhtmlDocumentBuilder SetHtml(Uri location, string html, MimeCharset charset)
        {
            if (location is null)
                throw new ArgumentNullException(nameof(location));
            if (html is null)
                throw new ArgumentNullException(nameof(html));

            _rootLocation = location;
            _rootType = HtmlType;
            _root = () =>
            {
                var entity = new ImfEntity(Header.Options) { ContentType = HtmlType };
                entity.SetText(html, charset);
                return entity;
            };
            return this;
        }

        /// <summary>
        /// Sets the root document.
        /// </summary>
        /// <param name="location">The URL of the document, absolute or relative.</param>
        /// <param name="contentType">The media type.</param>
        /// <param name="content">The content.  The memory must not change until the last write of a document that is built.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised, or is of the top-level type <c>multipart</c>.</exception>
        public MhtmlDocumentBuilder SetRoot(Uri location, MimeMediaType contentType, ReadOnlyMemory<byte> content)
        {
            if (location is null)
                throw new ArgumentNullException(nameof(location));

            CheckContentType(contentType, nameof(contentType));
            _rootLocation = location;
            _rootType = contentType;
            _root = () => CreateLeaf(contentType, content);
            return this;
        }

        /// <summary>
        /// Adds a resource that the document refers to by a URL.
        /// </summary>
        /// <param name="location">The URL, absolute or relative.  Two parts can't have one that resolves to the same URL (RFC 2557 §7), which is checked by <see cref="Build"/>.</param>
        /// <param name="contentType">The media type.</param>
        /// <param name="content">The content.  The memory must not change until the last write of a document that is built.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="location"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised, or is of the top-level type <c>multipart</c>, or
        /// <paramref name="location"/> has the <c>cid</c> scheme and is not valid.</exception>
        public MhtmlDocumentBuilder AddResource(Uri location, MimeMediaType contentType, ReadOnlyMemory<byte> content)
        {
            if (location is null)
                throw new ArgumentNullException(nameof(location));

            CheckContentType(contentType, nameof(contentType));
            CheckCidUri(location, nameof(location));
            _resources.Add(() =>
            {
                var entity = CreateLeaf(contentType, content);
                Label(entity, location);
                return entity;
            });
            return this;
        }

        /// <summary>
        /// Builds a document, whose message shares nothing that can change with any other built from this builder, except the options given to the builder, which they all use, so
        /// do not change those afterwards.
        /// </summary>
        /// <returns>The document, with the base URI given to <see cref="SetLocation"/>, if any.</returns>
        /// <exception cref="InvalidOperationException">There is no root document.</exception>
        /// <exception cref="ArgumentException">A character set can't be resolved, or can't hold the characters of its text, or the text is too long for
        /// <see cref="ImfOptions.MaxEntitySize"/>, or the URL of the root has the <c>cid</c> scheme and is not valid.</exception>
        /// <exception cref="ImfFileException">Two parts have the same Content-ID, or a URL that resolves to the same URL (RFC 2557 §7), whatever the options say.</exception>
        public MhtmlDocument Build()
        {
            if (_root is null)
                throw new InvalidOperationException("A document needs a root.");

            CheckCidUri(_rootLocation, "location");

            var message = new ImfMessage(Header.Options);
            foreach (var field in Header.Fields)
            {
                message.Fields.Add(field);
            }

            if (!message.Fields.Contains(ImfFieldNames.MimeVersion)) message.Fields.Add(ImfHeaderField.FromValue(ImfFieldNames.MimeVersion, encoding => MimeVersion));

            var root = _root();
            Label(root, _rootLocation);
            if (root.ContentId is null) root.ContentId = _contentIdGenerator();

            // ref. RFC 2387 §3.1 and §3.2.
            var contentType = MimeMediaType.MultipartRelated
                .WithParameter(TypeParameterName, _rootType.Type + "/" + _rootType.Subtype)
                .WithParameter(StartParameterName, root.ContentId.ToString());
            var related = new ImfMultipart(contentType, Header.Options);
            if (!(_location is null)) related.ContentLocation = _location;

            related.Parts.Add(root);
            foreach (var resource in _resources)
            {
                related.Parts.Add(resource());
            }

            message.Body = related;

            foreach (var diagnostic in MhtmlStructure.Read(message, MhtmlUrl.ThisMessageBase).Diagnostics)
            {
                if (diagnostic.Kind == ImfDiagnosticKind.InvalidRelatedStructure) throw ImfConformance.BuildException(diagnostic, false);
            }

            return new MhtmlDocument(message);
        }

        private ImfEntity CreateLeaf(MimeMediaType contentType, ReadOnlyMemory<byte> content)
        {
            var entity = new ImfEntity(Header.Options) { ContentType = contentType };
            entity.SetContent(content);
            return entity;
        }

        private static void Label(ImfEntity entity, Uri location)
        {
            if (MhtmlUrl.HasScheme(location.OriginalString, CidScheme))
            {
                entity.ContentId = ImfMessageId.ParseCidUri(location.OriginalString);
            }
            else
            {
                entity.ContentLocation = location;
            }
        }

        private static void CheckCidUri(Uri location, string parameterName)
        {
            if (MhtmlUrl.HasScheme(location.OriginalString, CidScheme) && !ImfMessageId.TryParseCidUri(location.OriginalString, out _))
                throw new ArgumentException("The cid: URI is not in a valid format.", parameterName);
        }

        private static void CheckContentType(MimeMediaType contentType, string parameterName)
        {
            if (contentType == default(MimeMediaType))
                throw new ArgumentException("The media type is not initialised.", parameterName);
            if (string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A media type of the top-level type multipart has parts, not content.", parameterName);
        }

        private static ImfMessageId GenerateContentId()
        {
            return new ImfMessageId(Guid.NewGuid().ToString("N"), GeneratedIdDomain);
        }
    }
}
