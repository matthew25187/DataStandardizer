using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Builds a message from its header, its text, and what goes with the text, in the structure that those call for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Build"/> makes the structure from what it has, so that a caller doesn't have to know it (RFC 2046 §5.1, RFC 2387):
    /// </para>
    /// <list type="bullet">
    /// <item>Text or HTML alone is the body of the message.</item>
    /// <item>Text and HTML go in a <c>multipart/alternative</c> entity, with the text first, as RFC 2046 §5.1.4 has the best last.</item>
    /// <item>HTML with inline resources goes in a <c>multipart/related</c> entity, with the HTML as its root, inside any <c>multipart/alternative</c> entity.</item>
    /// <item>Attachments go in a <c>multipart/mixed</c> entity, after all of that.</item>
    /// </list>
    /// <para>
    /// The builder doesn't make up a Date or Message-ID, or anything else that isn't given.  A writer refuses a message that RFC 5322 §3.6 doesn't allow, and
    /// <see cref="ImfMessage.Validate"/> reports it.  A writer generates the boundaries, and chooses each transfer encoding.
    /// </para>
    /// </remarks>
    public sealed class ImfMessageBuilder
    {
        private const string RelatedTypeParameterName = "type";
        private const string TextHtml = "text/html";
        private const string MimeVersion = "1.0";

        private static readonly MimeMediaType HtmlType = new MimeMediaType(MimeTopLevelTypes.Text, "html");

        private readonly List<Func<ImfEntity>> _attachments = new List<Func<ImfEntity>>();
        private readonly List<Func<ImfEntity>> _resources = new List<Func<ImfEntity>>();

        private MimeCharset _htmlCharset;
        [AllowNull]
        private string _htmlText;
        private MimeCharset _plainCharset;
        [AllowNull]
        private string _plainText;

        /// <summary>
        /// Creates a builder that uses the default options.
        /// </summary>
        public ImfMessageBuilder() : this(new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a builder.
        /// </summary>
        /// <param name="options">The options of the message and its entities.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/>
        /// that is needed and can't be resolved.</exception>
        public ImfMessageBuilder(ImfOptions options)
        {
            Header = new ImfMessage(options);
        }

        /// <summary>
        /// Gets the message whose fields are copied to the header of the message that is built, so that they can be set with its accessors.  Its body is not used, and a field whose
        /// name starts with <c>Content-</c> gives way to those of the body, if the message that is built has one.
        /// </summary>
        public ImfMessage Header { get; }

        /// <summary>
        /// Sets the plain text of the message, in UTF-8.
        /// </summary>
        /// <param name="text">The text.  Each line break becomes a CRLF (RFC 2046 §4.1.1).</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public ImfMessageBuilder SetText(string text)
        {
            return SetText(text, MimeCharset.Utf8);
        }

        /// <summary>
        /// Sets the plain text of the message.
        /// </summary>
        /// <param name="text">The text.  Each line break becomes a CRLF (RFC 2046 §4.1.1).</param>
        /// <param name="charset">The character set to write it in, which is checked by <see cref="Build"/>.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="text"/> is <c>null</c>.</exception>
        public ImfMessageBuilder SetText(string text, MimeCharset charset)
        {
            _plainText = text ?? throw new ArgumentNullException(nameof(text));
            _plainCharset = charset;
            return this;
        }

        /// <summary>
        /// Sets the HTML text of the message, in UTF-8.
        /// </summary>
        /// <param name="html">The HTML.  Each line break becomes a CRLF (RFC 2046 §4.1.1).</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="html"/> is <c>null</c>.</exception>
        public ImfMessageBuilder SetHtml(string html)
        {
            return SetHtml(html, MimeCharset.Utf8);
        }

        /// <summary>
        /// Sets the HTML text of the message.
        /// </summary>
        /// <param name="html">The HTML.  Each line break becomes a CRLF (RFC 2046 §4.1.1).</param>
        /// <param name="charset">The character set to write it in, which is checked by <see cref="Build"/>.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="html"/> is <c>null</c>.</exception>
        public ImfMessageBuilder SetHtml(string html, MimeCharset charset)
        {
            _htmlText = html ?? throw new ArgumentNullException(nameof(html));
            _htmlCharset = charset;
            return this;
        }

        /// <summary>
        /// Adds an attachment.
        /// </summary>
        /// <param name="fileName">The suggested file name, which is written as RFC 2231 has it if it isn't US-ASCII.</param>
        /// <param name="contentType">The media type.</param>
        /// <param name="content">The content.  The memory must not change until the last write of a message that is built.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fileName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised, or is of the top-level type <c>multipart</c>.</exception>
        public ImfMessageBuilder AddAttachment(string fileName, MimeMediaType contentType, ReadOnlyMemory<byte> content)
        {
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            CheckContentType(contentType, nameof(contentType));
            _attachments.Add(() =>
            {
                var entity = CreateLeaf(contentType, content);
                entity.ContentDisposition = ImfContentDisposition.Attachment.WithFileName(fileName);
                return entity;
            });
            return this;
        }

        /// <summary>
        /// Adds a message as an attachment, in a <c>message/rfc822</c> entity (RFC 2046 §5.2.1), as when forwarding it.
        /// </summary>
        /// <param name="message">The message, which is held by each message that is built, and not copied.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is <c>null</c>.</exception>
        public ImfMessageBuilder AddAttachment(ImfMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            _attachments.Add(() => new ImfMessagePart(message) { ContentDisposition = ImfContentDisposition.Attachment });
            return this;
        }

        /// <summary>
        /// Adds a resource that the HTML refers to with a <c>cid:</c> URI (RFC 2392), such as an image.  It goes in a <c>multipart/related</c> entity with the HTML (RFC 2387).
        /// </summary>
        /// <param name="contentId">The Content-ID that the URI gives.</param>
        /// <param name="contentType">The media type.</param>
        /// <param name="content">The content.  The memory must not change until the last write of a message that is built.</param>
        /// <returns>This object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="contentId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="contentType"/> is not initialised, or is of the top-level type <c>multipart</c>.</exception>
        public ImfMessageBuilder AddInlineResource(ImfMessageId contentId, MimeMediaType contentType, ReadOnlyMemory<byte> content)
        {
            if (contentId is null)
                throw new ArgumentNullException(nameof(contentId));

            CheckContentType(contentType, nameof(contentType));
            _resources.Add(() =>
            {
                var entity = CreateLeaf(contentType, content);
                entity.ContentId = contentId;
                entity.ContentDisposition = ImfContentDisposition.Inline;
                return entity;
            });
            return this;
        }

        /// <summary>
        /// Builds a message, which shares nothing that can change with any other built from this builder.
        /// </summary>
        /// <returns>The message.  With no text, HTML or attachments, it has no body.</returns>
        /// <exception cref="InvalidOperationException">There are inline resources and no HTML to refer to them.</exception>
        /// <exception cref="ArgumentException">A character set can't be resolved, or can't hold the characters of its text, or the text is too long for
        /// <see cref="ImfOptions.MaxEntitySize"/>.</exception>
        public ImfMessage Build()
        {
            if (_resources.Count > 0 && _htmlText is null)
                throw new InvalidOperationException("An inline resource needs HTML to refer to it.");

            var message = new ImfMessage(Header.Options);
            foreach (var field in Header.Fields)
            {
                message.Fields.Add(field);
            }

            var body = BuildText();
            if (_attachments.Count > 0)
            {
                var mixed = new ImfMultipart(MimeMediaType.MultipartMixed, Header.Options);
                if (!(body is null)) mixed.Parts.Add(body);

                foreach (var attachment in _attachments)
                {
                    mixed.Parts.Add(attachment());
                }

                body = mixed;
            }

            if (!(body is null))
            {
                // ref. RFC 2045 §4: a message with a MIME body says so, which is clearest ahead of the fields that describe the body.
                if (!message.Fields.Contains(ImfFieldNames.MimeVersion)) message.Fields.Add(ImfHeaderField.FromValue(ImfFieldNames.MimeVersion, encoding => MimeVersion));

                message.Body = body;
            }

            return message;
        }

        [return: MaybeNull]
        private ImfEntity BuildText()
        {
            if (_plainText is null) return _htmlText is null ? null : BuildHtml(_htmlText);

            var plain = CreateText(MimeMediaType.TextPlain, _plainText, _plainCharset);
            if (_htmlText is null) return plain;

            var html = BuildHtml(_htmlText);

            var alternative = new ImfMultipart(MimeMediaType.MultipartAlternative, Header.Options);
            alternative.Parts.Add(plain);
            alternative.Parts.Add(html);
            return alternative;
        }

        private ImfEntity BuildHtml(string text)
        {
            var html = CreateText(HtmlType, text, _htmlCharset);
            if (_resources.Count == 0) return html;

            // ref. RFC 2387 §3.1: the type parameter is required, and is the media type of the root part, which is the first as there is no start parameter.
            var related = new ImfMultipart(MimeMediaType.MultipartRelated.WithParameter(RelatedTypeParameterName, TextHtml), Header.Options);
            related.Parts.Add(html);
            foreach (var resource in _resources)
            {
                related.Parts.Add(resource());
            }

            return related;
        }

        private ImfEntity CreateText(MimeMediaType contentType, string text, MimeCharset charset)
        {
            var entity = new ImfEntity(Header.Options) { ContentType = contentType };
            entity.SetText(text, charset);
            return entity;
        }

        private ImfEntity CreateLeaf(MimeMediaType contentType, ReadOnlyMemory<byte> content)
        {
            var entity = new ImfEntity(Header.Options) { ContentType = contentType };
            entity.SetContent(content);
            return entity;
        }

        private static void CheckContentType(MimeMediaType contentType, string parameterName)
        {
            if (contentType == default(MimeMediaType))
                throw new ArgumentException("The media type is not initialised.", parameterName);
            if (string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("A media type of the top-level type multipart has parts, not content.", parameterName);
        }
    }
}
