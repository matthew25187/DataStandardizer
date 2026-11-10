using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Reads and writes messages through the object model for the tests, and describes the tree of a message as lines of text.  A line is the path of an entity, or <c>/</c> for
    /// the empty path, its depth, the type of the entity, and its media type: <c>2 1 part message/rfc822</c>.
    /// </summary>
    internal static class EntityTrees
    {
        private const int DefaultBufferSize = 4096;

        public static ImfMessage Read(string message)
        {
            return Read(message, new ImfOptions());
        }

        public static ImfMessage Read(string message, ImfOptions options)
        {
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message)), options))
            {
                return reader.ReadMessage();
            }
        }

        /// <summary>
        /// Reads a message whose line break is a line feed, as a reader of a container format does.
        /// </summary>
        public static ImfMessage ReadWithLineFeeds(string message)
        {
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message)), new ImfOptions(), DefaultBufferSize, true))
            {
                return reader.ReadMessage();
            }
        }

        /// <summary>
        /// Writes a message with line feeds as its line breaks, as a writer of a container format does.
        /// </summary>
        public static string WriteWithLineFeeds(ImfMessage message)
        {
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, new ImfOptions(), true, MessageWrites.NextBoundary()))
            {
                writer.WriteMessage(message);
            }

            return Octets.ToText(output.ToArray());
        }

        /// <summary>
        /// Writes a message, with boundaries that are the same on every run.
        /// </summary>
        public static string Write(ImfMessage message)
        {
            return Write(message, new ImfOptions());
        }

        /// <summary>
        /// Writes a message, with boundaries that are the same on every run.
        /// </summary>
        public static string Write(ImfMessage message, ImfOptions options)
        {
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, options, false, MessageWrites.NextBoundary()))
            {
                writer.WriteMessage(message);
            }

            return Octets.ToText(output.ToArray());
        }

        /// <summary>
        /// Writes a message asynchronously, with boundaries that are the same on every run.
        /// </summary>
        public static async Task<string> WriteAsync(ImfMessage message)
        {
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, new ImfOptions(), false, MessageWrites.NextBoundary()))
            {
                await writer.WriteMessageAsync(message);
            }

            return Octets.ToText(output.ToArray());
        }

        /// <summary>
        /// Gets the lines that describe the tree of a message.
        /// </summary>
        public static List<string> Describe(ImfMessage message)
        {
            var lines = new List<string>();
            foreach (var node in message.EnumerateEntities())
            {
                var kind = node.Entity is ImfMultipart ? "multipart" : node.Entity is ImfMessagePart ? "part" : "leaf";
                var contentType = node.Entity.ContentType;
                lines.Add((node.Path.Length == 0 ? "/" : node.Path) + " " + node.Depth + " " + kind + " " + contentType.Type + "/" + contentType.Subtype);
            }

            return lines;
        }

        /// <summary>
        /// Gets each field of an entity as its name, a colon and its body.
        /// </summary>
        public static List<string> DescribeFields(ImfEntity entity)
        {
            var lines = new List<string>();
            foreach (var field in entity.Fields)
            {
                lines.Add(field.Name + ":" + field.Value);
            }

            return lines;
        }

        /// <summary>
        /// Gets the media type of each entity, without its parameters.
        /// </summary>
        public static List<string> TypesOf(IEnumerable<ImfEntity> entities)
        {
            var types = new List<string>();
            foreach (var entity in entities)
            {
                types.Add(entity.ContentType.Type + "/" + entity.ContentType.Subtype);
            }

            return types;
        }

        /// <summary>
        /// Gets the content of an entity as text with one byte in each character.
        /// </summary>
        public static string ContentOf(ImfEntity entity)
        {
            return Octets.ToText(entity.GetContent().Span);
        }

        /// <summary>
        /// Gets an entity by its path, the first of those with the path, in the order of <see cref="ImfMessage.EnumerateEntities"/>.
        /// </summary>
        public static ImfEntity At(ImfMessage message, string path)
        {
            foreach (var node in message.EnumerateEntities())
            {
                if (node.Path == path) return node.Entity;
            }

            throw new ArgumentException("No entity has the path " + path + ".", nameof(path));
        }

        /// <summary>
        /// Gets a message with one part that holds a number of octets, all of them <c>x</c>.
        /// </summary>
        public static string MessageWithPartOfLength(int length)
        {
            return MimeShapes.Header + "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\n" + new string('x', length) + "\r\n--b--\r\n";
        }

        /// <summary>
        /// Gets a message whose body is multipart entities nested to a depth, with text at the bottom.  They are made from the top down, as adding a part looks through all of that part.
        /// </summary>
        public static ImfMessage MessageNestedToDepth(int depth, ImfOptions options)
        {
            var top = new ImfMultipart(MimeMediaType.MultipartMixed, options);
            var current = top;
            for (var level = 1; level < depth; level++)
            {
                var next = new ImfMultipart(MimeMediaType.MultipartMixed, options);
                current.Parts.Add(next);
                current = next;
            }

            var text = new ImfEntity(options);
            text.SetText("x");
            current.Parts.Add(text);
            return new ImfMessage(options) { Body = top };
        }

        /// <summary>
        /// Gets the bytes of a string, as a body to give an entity.
        /// </summary>
        public static ReadOnlyMemory<byte> Bytes(string text)
        {
            return Octets.Of(text);
        }
    }

    /// <summary>
    /// Gets the encodings of code pages, which .NET Framework has of its own, and .NET has from <c>CodePagesEncodingProvider</c>.
    /// </summary>
    internal sealed class CodePageCharsetResolver : IMimeCharsetResolver
    {
#if NETCOREAPP3_0_OR_GREATER
        public bool TryGetEncoding(MimeCharset charset, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Encoding encoding)
#else
        public bool TryGetEncoding(MimeCharset charset, out Encoding encoding)
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            encoding = CodePagesEncodingProvider.Instance.GetEncoding(charset.Name);
#else
            try
            {
                encoding = Encoding.GetEncoding(charset.Name);
            }
            catch (ArgumentException)
            {
                encoding = null;
            }
#endif
            return !(encoding is null);
        }
    }

    /// <summary>
    /// Messages in the shapes that mail programs send, for the tests of the object model.
    /// </summary>
    internal static class MimeShapes
    {
        public const string Header = "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\nFrom: a@b.test\r\nMIME-Version: 1.0\r\n";

        public const string TextOnly =
            Header +
            "Content-Type: text/plain; charset=utf-8\r\n" +
            "Content-Transfer-Encoding: quoted-printable\r\n" +
            "\r\n" +
            "caf=C3=A9\r\n";

        public const string TextAndHtml =
            Header +
            "Content-Type: multipart/alternative; boundary=alt\r\n" +
            "\r\n" +
            "--alt\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "plain\r\n" +
            "--alt\r\n" +
            "Content-Type: text/html; charset=us-ascii\r\n" +
            "\r\n" +
            "<p>html</p>\r\n" +
            "--alt--\r\n";

        public const string HtmlWithInlineImage =
            Header +
            "Content-Type: multipart/related; boundary=rel; type=\"text/html\"\r\n" +
            "\r\n" +
            "--rel\r\n" +
            "Content-Type: text/html; charset=us-ascii\r\n" +
            "\r\n" +
            "<img src=\"cid:img@b.test\">\r\n" +
            "--rel\r\n" +
            "Content-Type: image/png\r\n" +
            "Content-ID: <img@b.test>\r\n" +
            "Content-Disposition: inline\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "iVBORw==\r\n" +
            "--rel--\r\n";

        public const string HtmlRootNamedByStart =
            Header +
            "Content-Type: multipart/related; boundary=rel; type=\"text/html\"; start=\"<root@b.test>\"\r\n" +
            "\r\n" +
            "--rel\r\n" +
            "Content-Type: image/png\r\n" +
            "Content-ID: <img@b.test>\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "iVBORw==\r\n" +
            "--rel\r\n" +
            "Content-Type: text/html; charset=us-ascii\r\n" +
            "Content-ID: <root@b.test>\r\n" +
            "\r\n" +
            "<p>root</p>\r\n" +
            "--rel--\r\n";

        public const string MixedWithAttachments =
            Header +
            "Content-Type: multipart/mixed; boundary=mix\r\n" +
            "\r\n" +
            "--mix\r\n" +
            "Content-Type: multipart/alternative; boundary=alt\r\n" +
            "\r\n" +
            "--alt\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "plain\r\n" +
            "--alt\r\n" +
            "Content-Type: text/html; charset=us-ascii\r\n" +
            "\r\n" +
            "<p>html</p>\r\n" +
            "--alt--\r\n" +
            "--mix\r\n" +
            "Content-Type: application/pdf\r\n" +
            "Content-Disposition: attachment; filename=report.pdf\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "JVBERg==\r\n" +
            "--mix\r\n" +
            "Content-Type: image/png; name=photo.png\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "iVBORw==\r\n" +
            "--mix\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-Disposition: attachment; filename=notes.txt\r\n" +
            "\r\n" +
            "attached text\r\n" +
            "--mix--\r\n";

        public const string EmbeddedHeader = "Date: Thu, 20 Nov 1997 09:55:06 -0600\r\nFrom: c@d.test\r\nSubject: Original\r\nMIME-Version: 1.0\r\n";

        public const string Forwarded =
            Header +
            "Content-Type: multipart/mixed; boundary=mix\r\n" +
            "\r\n" +
            "--mix\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "\r\n" +
            "See below.\r\n" +
            "--mix\r\n" +
            "Content-Type: message/rfc822\r\n" +
            "Content-Disposition: attachment\r\n" +
            "\r\n" +
            EmbeddedHeader +
            "Content-Type: multipart/mixed; boundary=inner\r\n" +
            "\r\n" +
            "--inner\r\n" +
            "Content-Type: text/plain; charset=us-ascii\r\n" +
            "Content-ID: <inner@d.test>\r\n" +
            "\r\n" +
            "original text\r\n" +
            "--inner\r\n" +
            "Content-Type: application/pdf\r\n" +
            "Content-Disposition: attachment; filename=inner.pdf\r\n" +
            "Content-Transfer-Encoding: base64\r\n" +
            "\r\n" +
            "JVBERg==\r\n" +
            "--inner--\r\n" +
            "\r\n" +
            "--mix--\r\n";

        // Everything a rewrite could lose: an odd header, a preamble, padding after each delimiter, a part with 8-bit text under a multipart entity that has no label for it, a part
        // with no body, and an epilogue.
        public const string OddFraming =
            "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n" +
            "From: a@b.test\r\n" +
            "Subject:   folded,\r\n" +
            "\tkept\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n" +
            "Content-Type:multipart/mixed;\r\n" +
            "  boundary=\"b b\"\r\n" +
            "\r\n" +
            "A preamble.\r\n" +
            "--b b  \r\n" +
            "Content-Type:text/plain;charset=us-ascii\r\n" +
            "\r\n" +
            "old\r\n" +
            "--b b\t\r\n" +
            "Content-Type: text/plain; charset=iso-8859-1\r\n" +
            "Content-Transfer-Encoding : 8bit\r\n" +
            "\r\n" +
            "caf\x00E9\r\n" +
            "--b b \r\n" +
            "X-Empty: yes\r\n" +
            "\r\n" +
            "--b b-- \r\n" +
            "An epilogue.\r\n";

        public const string OddFramingWithNewFirstPart =
            "Date: Fri, 21 Nov 1997 09:55:06 -0600\r\n" +
            "From: a@b.test\r\n" +
            "Subject:   folded,\r\n" +
            "\tkept\r\n" +
            "MIME-Version: 1.0\r\n" +
            "Content-Transfer-Encoding: 8bit\r\n" +
            "Content-Type:multipart/mixed;\r\n" +
            "  boundary=\"b b\"\r\n" +
            "\r\n" +
            "A preamble.\r\n" +
            "--b b  \r\n" +
            "Content-Type:text/plain;charset=us-ascii\r\n" +
            "\r\n" +
            "new\r\n" +
            "--b b\t\r\n" +
            "Content-Type: text/plain; charset=iso-8859-1\r\n" +
            "Content-Transfer-Encoding : 8bit\r\n" +
            "\r\n" +
            "caf\x00E9\r\n" +
            "--b b \r\n" +
            "X-Empty: yes\r\n" +
            "\r\n" +
            "--b b-- \r\n" +
            "An epilogue.\r\n";
    }
}
