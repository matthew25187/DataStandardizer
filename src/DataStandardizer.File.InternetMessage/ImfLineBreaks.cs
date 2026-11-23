using System;
using System.Collections.Generic;
using System.IO;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Changes the line breaks of a whole message, for one going between a container format, which uses LF, and a standalone message, which uses CRLF (RFC 5322 §2.1).
    /// </summary>
    internal static class ImfLineBreaks
    {
        private const byte CarriageReturn = 0x0D;
        private const byte LineFeed = 0x0A;

        internal const string OpaqueEntityMessage = "An entity with the binary transfer encoding or an extension mechanism can't change its line breaks, as a line break in it may not be one (RFC 2045 §2.9 and §6.3).";

        private static readonly byte[] CrLf = { CarriageReturn, LineFeed };
        private static readonly byte[] Lf = { LineFeed };

        /// <summary>
        /// Changes the line breaks of the bytes of a message.
        /// </summary>
        /// <param name="message">The message that the bytes are of.</param>
        /// <param name="bytes">The bytes.</param>
        /// <param name="toCrLf"><c>true</c> to change them to CRLF, or <c>false</c> to change them to LF.</param>
        /// <returns>The changed bytes.</returns>
        /// <exception cref="ImfFileException">The message has a <c>binary</c> entity, or one with an extension mechanism: its
        /// <see cref="ImfFileException.DataItemName.EntityPath"/> item says which, the last in the order of <see cref="ImfMessage.EnumerateEntities"/>, as an entity comes
        /// before what it holds, so that it is one whose own content is labelled so, as an <see cref="ImfEntityWriter"/> reports it.</exception>
        internal static byte[] Convert(ImfMessage message, ReadOnlySpan<byte> bytes, bool toCrLf)
        {
            var opaque = FindLastOpaqueEntity(message.Body);
            if (!(opaque is null))
            {
                var exception = new ImfFileException(OpaqueEntityMessage);
                exception.Data[ImfFileException.DataItemName.EntityPath] = GetPath(message, opaque);
                throw exception;
            }

            return ConvertLines(bytes, toCrLf);
        }

        /// <summary>
        /// Changes each line break, an LF with or without a CR before it, in bytes that hold no binary data, such as a header field.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <param name="toCrLf"><c>true</c> to change them to CRLF, or <c>false</c> to change them to LF.</param>
        /// <returns>The changed bytes.</returns>
        internal static byte[] ConvertLines(ReadOnlySpan<byte> bytes, bool toCrLf)
        {
            var output = new MemoryStream(bytes.Length);
            var rest = bytes;
            while (true)
            {
                var lineFeed = rest.IndexOf(LineFeed);
                if (lineFeed < 0)
                {
                    Write(output, rest);
                    return output.ToArray();
                }

                var hasCarriageReturn = lineFeed > 0 && rest[lineFeed - 1] == CarriageReturn;
                Write(output, rest.Slice(0, hasCarriageReturn ? lineFeed - 1 : lineFeed));
                Write(output, toCrLf ? CrLf : Lf);
                rest = rest.Slice(lineFeed + 1);
            }
        }

        /// <summary>
        /// Writes bytes to a memory stream.
        /// </summary>
        internal static void Write(MemoryStream output, ReadOnlySpan<byte> bytes)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
            output.Write(bytes);
#else
            // A copy, as this target can't write a span.
            var copy = bytes.ToArray();
            output.Write(copy, 0, copy.Length);
#endif
        }

        // The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack.  The parts are pushed last first, so the entities are looked at in the
        // order of ImfMessage.EnumerateEntities, without the cost of a path for each.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static ImfEntity? FindLastOpaqueEntity(ImfEntity body)
#else
        private static ImfEntity FindLastOpaqueEntity(ImfEntity body)
#endif
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            ImfEntity? found = null;
#else
            ImfEntity found = null;
#endif
            var pending = new Stack<ImfEntity>();
            pending.Push(body);
            while (pending.Count > 0)
            {
                var entity = pending.Pop();
                var encoding = entity.ContentTransferEncoding;
                if (encoding == MimeTransferEncoding.Binary || !encoding.IsKnown) found = entity;

                if (entity is ImfMessagePart messagePart)
                {
                    pending.Push(messagePart.Message.Body);
                }
                else if (entity is ImfMultipart multipart)
                {
                    for (var index = multipart.Parts.Count - 1; index >= 0; index--)
                    {
                        pending.Push(multipart.Parts[index]);
                    }
                }
            }

            return found;
        }

        private static string GetPath(ImfMessage message, ImfEntity entity)
        {
            foreach (var node in message.EnumerateEntities())
            {
                if (ReferenceEquals(node.Entity, entity)) return node.Path;
            }

            return string.Empty;
        }
    }
}
