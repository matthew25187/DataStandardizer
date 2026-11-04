using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Reads the entities of a message that is in memory, for <see cref="ImfMessage.Body"/>, with the scanner that an <see cref="ImfEntityReader"/> uses.  Each entity refers to the
    /// range of the bytes of the body that it came from, so nothing is copied.
    /// </summary>
    /// <remarks>
    /// The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack.
    /// </remarks>
    internal static class ImfEntityTree
    {
        private const int BlockSize = 16 * 1024;
        private const byte LineFeed = 0x0A;
        private const string GlobalSubtype = "global";
        private const string Rfc822Subtype = "rfc822";

        private static readonly byte[] CrLf = { 0x0D, 0x0A };
        private static readonly byte[] Lf = { LineFeed };

        /// <summary>
        /// The media type of an entity with no Content-Type field (RFC 2045 §5.2).
        /// </summary>
        internal static readonly MimeMediaType DefaultContentType = MimeMediaType.TextPlain.WithParameter("charset", "us-ascii");

        /// <summary>
        /// Reads the body of a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="diagnostics">Receives the problems that did not stop reading.</param>
        /// <returns>The body, which uses the fields of the message.</returns>
        /// <exception cref="ImfFileException">The body breaks a rule of RFC 2046 §5.1 that the options don't accept, or passes a limit.</exception>
        internal static ImfEntity Read(ImfMessage message, List<ImfDiagnostic> diagnostics)
        {
            var options = message.Options;
            var lineFeedIsLineBreak = message.LineFeedIsLineBreak;
            var header = GetHeaderBytes(message, lineFeedIsLineBreak ? Lf : CrLf, out var headerEnd);
            var body = message.RawBody;

            var scanner = new ImfEntityScanner(new ImfHeaderScanner(BlockSize, options, ImfHeaderReader.GetFallbackEncoding(options), lineFeedIsLineBreak), options, lineFeedIsLineBreak);
            scanner.LeaveBodiesRaw();
            scanner.CountQuotes(message.QuotedLines);
            try
            {
                var root = new Frame(0, null, null);
                var current = root;
                var position = 0L;
                while (true)
                {
                    var status = scanner.Next();
                    if (status == ImfEntityScanner.Status.EndOfMessage) break;

                    if (status == ImfEntityScanner.Status.NeedMoreInput)
                    {
                        scanner.PrepareRefill(out var offset, out var count);
                        var bytesRead = Fill(header, body, position, scanner.HeaderScanner.Buffer, offset, count);
                        position += bytesRead;
                        scanner.HeaderScanner.CompleteRefill(bytesRead);
                        continue;
                    }

                    var start = scanner.EventByteOffset - header.Length;
                    var length = scanner.EventByteLength;
                    switch (scanner.EventKind)
                    {
                        case ImfEntityEventKind.EntityStart:
                            if (scanner.Depth > 0)
                            {
                                // Only a multipart entity has parts, so the child of any other entity is the message it holds.
                                current = new Frame(start, current, IsMultipart(current.ContentType) ? null : new ImfMessage(options));
                            }

                            break;

                        case ImfEntityEventKind.HeaderField:
                            if (scanner.Depth > 0) current.Fields.Add(scanner.HeaderScanner.ToField());

                            break;

                        case ImfEntityEventKind.HeaderEnd:
                            current.ContentType = scanner.ContentType;
                            current.TransferEncoding = scanner.TransferEncoding;
                            current.BodyStart = start + length;
                            current.HeaderEndBytePosition = scanner.EventByteOffset + length;
                            current.HeaderEndLineNumber = scanner.HeaderScanner.PhysicalLinesRead + 1;
                            break;

                        case ImfEntityEventKind.Preamble:
                            if (current.PreambleLength == 0) current.PreambleStart = start;

                            current.PreambleLength += length;
                            break;

                        case ImfEntityEventKind.Epilogue:
                            if (current.EpilogueLength == 0) current.EpilogueStart = start;

                            current.EpilogueLength += length;
                            break;

                        case ImfEntityEventKind.EntityEnd:
                            current.End = start + length;
                            if (current.Parent is null)
                            {
                                BuildRoot(message, current, body, headerEnd);
                            }
                            else
                            {
                                Build(current, body, options, lineFeedIsLineBreak);
                                current.Parent.Children.Add(current);
                                current = current.Parent;
                            }

                            break;
                    }
                }

                diagnostics.AddRange(scanner.Diagnostics);
                return root.Entity;
            }
            finally
            {
                scanner.ReturnBuffers();
            }
        }

        /// <summary>
        /// Gets the bytes of a header as a scanner reads them.  Those of a message that was read are what it read, and the rest are formatted as a writer formats them.
        /// </summary>
        private static byte[] GetHeaderBytes(ImfMessage message, byte[] lineBreak, out byte[] headerEnd)
        {
            var output = new MemoryStream();
            var buffer = new byte[1024];
            foreach (var field in message.Fields)
            {
                var bytes = field.GetOutput(ImfHeaderEncoding.EncodedWords, lineBreak, ref buffer).ToArray();
                output.Write(bytes, 0, bytes.Length);
            }

            // A writer adds the empty line if a body needs one, as there would otherwise be no way to tell where the body starts.
            var readHeaderEnd = message.HeaderEnd;
            headerEnd = readHeaderEnd is null || (readHeaderEnd.Length == 0 && !message.RawBody.IsEmpty) ? lineBreak : readHeaderEnd;
            output.Write(headerEnd, 0, headerEnd.Length);
            return output.ToArray();
        }

        /// <summary>
        /// Copies the next bytes of a header and a body into a block.
        /// </summary>
        /// <returns>The number of bytes copied, which is 0 at the end of the data.</returns>
        private static int Fill(byte[] header, ReadOnlyMemory<byte> body, long position, byte[] block, int offset, int count)
        {
            if (position < header.Length)
            {
                var headerCount = (int)Math.Min(count, header.Length - position);
                Array.Copy(header, (int)position, block, offset, headerCount);
                return headerCount;
            }

            var bodyPosition = (int)(position - header.Length);
            var bodyCount = Math.Min(count, body.Length - bodyPosition);
            body.Span.Slice(bodyPosition, bodyCount).CopyTo(new Span<byte>(block, offset, bodyCount));
            return bodyCount;
        }

        private static void BuildRoot(ImfMessage message, Frame frame, ReadOnlyMemory<byte> body, byte[] headerEnd)
        {
            var entity = CreateEntity(frame, message.Options, message.LineFeedIsLineBreak);
            frame.Entity = entity;
            entity.ShareFields(message.Fields);
            entity.SetRead(default(ReadOnlyMemory<byte>), false, headerEnd, body, frame.ContentType, message.LineFeedIsLineBreak);
            CompleteMultipart(entity, frame, body);
        }

        private static void Build(Frame frame, ReadOnlyMemory<byte> body, ImfOptions options, bool lineFeedIsLineBreak)
        {
            var entity = CreateEntity(frame, options, lineFeedIsLineBreak);
            frame.Entity = entity;

            // ref. RFC 2046 §5.1.1: a part with no body stops before the empty line that ended its header, whose line break belongs to the delimiter.
            var rawEntity = body.Slice((int)frame.Start, (int)(frame.End - frame.Start));
            var fieldsLength = 0;
            foreach (var field in frame.Fields)
            {
                fieldsLength += field.RawBytes.Length;
            }

            var bodyStart = Math.Min(frame.BodyStart, frame.End);
            var rawHeaderEnd = rawEntity.Slice(fieldsLength, (int)(bodyStart - frame.Start) - fieldsLength);
            var rawBody = rawEntity.Slice((int)(bodyStart - frame.Start));

            var message = frame.Message;
            if (message is null)
            {
                foreach (var field in frame.Fields)
                {
                    entity.Fields.Add(field);
                }
            }
            else
            {
                foreach (var field in frame.Fields)
                {
                    message.Fields.Add(field);
                }

                entity.ShareFields(message.Fields);
                message.HeaderEnd = rawHeaderEnd.ToArray();
                message.HeaderEndBytePosition = frame.HeaderEndBytePosition;
                message.HeaderEndLineNumber = frame.HeaderEndLineNumber;
                message.LineFeedIsLineBreak = lineFeedIsLineBreak;
                message.SetReadBody(entity, rawBody);
            }

            entity.SetRead(rawEntity, true, rawHeaderEnd, rawBody, frame.ContentType, lineFeedIsLineBreak);
            CompleteMultipart(entity, frame, body);
        }

        /// <summary>
        /// Creates an entity of the type that its media type calls for.  The types that the scanner follows hold a message, which is empty if there was nothing to read before a
        /// delimiter or the end of the data.
        /// </summary>
        private static ImfEntity CreateEntity(Frame frame, ImfOptions options, bool lineFeedIsLineBreak)
        {
            if (IsMultipart(frame.ContentType)) return new ImfMultipart(options);

            if (frame.Children.Count == 1) return new ImfMessagePart(frame.Children[0].Message, MimeMediaType.MessageRfc822);

            return HoldsMessage(frame) ? new ImfMessagePart(CreateEmptyMessage(options, lineFeedIsLineBreak), MimeMediaType.MessageRfc822) : new ImfEntity(options);
        }

        private static ImfMessage CreateEmptyMessage(ImfOptions options, bool lineFeedIsLineBreak)
        {
            var message = new ImfMessage(options)
            {
                HeaderEnd = new byte[0],
                LineFeedIsLineBreak = lineFeedIsLineBreak,
            };
            var body = new ImfEntity(options);
            body.ShareFields(message.Fields);
            body.SetRead(default(ReadOnlyMemory<byte>), false, default(ReadOnlyMemory<byte>), default(ReadOnlyMemory<byte>), DefaultContentType, lineFeedIsLineBreak);
            message.SetReadBody(body, default(ReadOnlyMemory<byte>));
            return message;
        }

        /// <summary>
        /// Gives a multipart entity its parts, and the bytes around them.
        /// </summary>
        private static void CompleteMultipart(ImfEntity entity, Frame frame, ReadOnlyMemory<byte> body)
        {
            if (!(entity is ImfMultipart multipart)) return;

            var framing = new ReadOnlyMemory<byte>[frame.Children.Count + 1];
            var position = Math.Min(frame.BodyStart, frame.End);
            for (var index = 0; index < frame.Children.Count; index++)
            {
                var child = frame.Children[index];
                multipart.Parts.AddRead(child.Entity);
                framing[index] = body.Slice((int)position, (int)(child.Start - position));
                position = child.End;
            }

            var last = body.Slice((int)position, (int)(frame.End - position));
            framing[frame.Children.Count] = last;

            ReadOnlyMemory<byte>? preamble = null;
            if (frame.PreambleLength > 0) preamble = body.Slice((int)frame.PreambleStart, (int)frame.PreambleLength);

            ReadOnlyMemory<byte>? epilogue = null;
            if (frame.EpilogueLength > 0)
            {
                epilogue = body.Slice((int)frame.EpilogueStart, (int)frame.EpilogueLength);
            }
            else if (!last.IsEmpty && last.Span[last.Length - 1] == LineFeed)
            {
                // Only a line break follows the close delimiter, which an empty epilogue stands for.
                epilogue = default(ReadOnlyMemory<byte>);
            }

            multipart.SetReadFraming(framing, preamble, epilogue);
        }

        private static bool IsMultipart(MimeMediaType contentType)
        {
            return string.Equals(contentType.Type, MimeTopLevelTypes.Multipart, StringComparison.OrdinalIgnoreCase);
        }

        // ref. RFC 6532 §3.7: message/global may have any transfer encoding, so it holds a message only if that leaves the message readable.
        private static bool HoldsMessage(Frame frame)
        {
            var contentType = frame.ContentType;
            if (!string.Equals(contentType.Type, MimeTopLevelTypes.Message, StringComparison.OrdinalIgnoreCase)) return false;

            return string.Equals(contentType.Subtype, Rfc822Subtype, StringComparison.OrdinalIgnoreCase)
                || (string.Equals(contentType.Subtype, GlobalSubtype, StringComparison.OrdinalIgnoreCase) && frame.TransferEncoding.IsIdentity);
        }

        /// <summary>
        /// An entity whose end has not been reached.
        /// </summary>
        private sealed class Frame
        {
            internal Frame(long start, [AllowNull] Frame parent, [AllowNull] ImfMessage message)
            {
                Start = start;
                Parent = parent;
                Message = message;
            }

            internal long Start { get; }

            [AllowNull]
            internal Frame Parent { get; }

            /// <summary>
            /// Gets the message of which this is the body, or <c>null</c> for a part of a multipart entity, or the body of the message being read.
            /// </summary>
            [AllowNull]
            internal ImfMessage Message { get; }

            internal List<ImfHeaderField> Fields { get; } = new List<ImfHeaderField>();

            internal List<Frame> Children { get; } = new List<Frame>();

            internal MimeMediaType ContentType { get; set; }

            internal MimeTransferEncoding TransferEncoding { get; set; }

            internal long BodyStart { get; set; }

            internal long End { get; set; }

            internal long HeaderEndBytePosition { get; set; }

            internal long HeaderEndLineNumber { get; set; }

            internal long PreambleStart { get; set; }

            internal long PreambleLength { get; set; }

            internal long EpilogueStart { get; set; }

            internal long EpilogueLength { get; set; }

            [AllowNull]
            internal ImfEntity Entity { get; set; }
        }
    }
}
