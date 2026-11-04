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

        private const string BinaryEntityMessage = "A binary entity can't change its line breaks, as a line break in it isn't one (RFC 2045 §2.9).";

        private static readonly byte[] CrLf = { CarriageReturn, LineFeed };
        private static readonly byte[] Lf = { LineFeed };

        /// <summary>
        /// Changes the line breaks of the bytes of a message.
        /// </summary>
        /// <param name="message">The message that the bytes are of.</param>
        /// <param name="bytes">The bytes.</param>
        /// <param name="toCrLf"><c>true</c> to change them to CRLF, or <c>false</c> to change them to LF.</param>
        /// <returns>The changed bytes.</returns>
        /// <exception cref="ImfFileException">The message has a <c>binary</c> entity.</exception>
        internal static byte[] Convert(ImfMessage message, ReadOnlySpan<byte> bytes, bool toCrLf)
        {
            if (HasBinaryEntity(message.Body))
                throw new ImfFileException(BinaryEntityMessage);

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

        // The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack.
        private static bool HasBinaryEntity(ImfEntity body)
        {
            var pending = new Stack<ImfEntity>();
            pending.Push(body);
            while (pending.Count > 0)
            {
                var entity = pending.Pop();
                if (entity.ContentTransferEncoding == MimeTransferEncoding.Binary) return true;

                if (entity is ImfMessagePart messagePart)
                {
                    pending.Push(messagePart.Message.Body);
                }
                else if (entity is ImfMultipart multipart)
                {
                    foreach (var part in multipart.Parts)
                    {
                        pending.Push(part);
                    }
                }
            }

            return false;
        }
    }
}
