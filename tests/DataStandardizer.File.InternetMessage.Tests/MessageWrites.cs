using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Writes header fields and whole messages for the tests, and gives the output as text with one byte in each character.
    /// </summary>
    internal static class MessageWrites
    {
        /// <summary>
        /// The first boundary that <see cref="NextBoundary"/> gives.
        /// </summary>
        public const string FirstBoundary = "=_1";

        /// <summary>
        /// The second boundary that <see cref="NextBoundary"/> gives.
        /// </summary>
        public const string SecondBoundary = "=_2";

        private const int DefaultBlockSize = 4096;

        /// <summary>
        /// Gets a source of boundaries that gives <see cref="FirstBoundary"/>, <see cref="SecondBoundary"/> and so on, so that the output is the same on every
        /// run.
        /// </summary>
        public static Func<string> NextBoundary()
        {
            var count = 0;
            return () =>
            {
                count++;
                return "=_" + count;
            };
        }

        /// <summary>
        /// Gets a source of boundaries that gives the same boundary every time.
        /// </summary>
        public static Func<string> SameBoundary(string boundary)
        {
            return () => boundary;
        }

        public static ImfEntityWriter CreateWriter(Stream stream)
        {
            return new ImfEntityWriter(stream, new ImfOptions(), DefaultBlockSize, false, NextBoundary());
        }

        public static ImfEntityWriter CreateWriter(Stream stream, ImfOptions options)
        {
            return new ImfEntityWriter(stream, options, DefaultBlockSize, false, NextBoundary());
        }

        /// <summary>
        /// Runs a call that is expected to fail, and swallows the exception, to leave the writer as that failure left it.
        /// </summary>
        public static void IgnoreFailure(Action call)
        {
            try
            {
                call();
            }
            catch (Exception exception) when (exception is ImfFileException || exception is IOException)
            {
            }
        }

        /// <summary>
        /// Runs an asynchronous call that is expected to fail, and swallows the exception, to leave the writer as that failure left it.
        /// </summary>
        public static async Task IgnoreFailureAsync(Func<Task> call)
        {
            try
            {
                await call();
            }
            catch (Exception exception) when (exception is ImfFileException || exception is IOException || exception is OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Writes one field with the default options.
        /// </summary>
        public static string WriteField(string name, string value, ImfHeaderFieldKind kind)
        {
            return WriteField(name, value, kind, new ImfOptions());
        }

        /// <summary>
        /// Writes one field.
        /// </summary>
        public static string WriteField(string name, string value, ImfHeaderFieldKind kind, ImfOptions options)
        {
            var stream = new MemoryStream();
            using (var writer = new ImfHeaderWriter(stream, options))
            {
                writer.WriteField(name, value, kind);
            }

            return Octets.ToText(stream.ToArray());
        }

        /// <summary>
        /// Writes a message with the default options.
        /// </summary>
        public static string Write(ImfEntitySource message)
        {
            return Write(message, new ImfOptions());
        }

        /// <summary>
        /// Writes a message.
        /// </summary>
        public static string Write(ImfEntitySource message, ImfOptions options)
        {
            return Write(message, options, DefaultBlockSize, false, NextBoundary());
        }

        /// <summary>
        /// Writes a message, with a chosen block size, line break setting and source of boundaries.
        /// </summary>
        public static string Write(ImfEntitySource message, ImfOptions options, int blockSize, bool lineFeedIsLineBreak, Func<string> boundaries)
        {
            var stream = new MemoryStream();
            using (var writer = new ImfEntityWriter(stream, options, blockSize, lineFeedIsLineBreak, boundaries))
            {
                writer.WriteMessage(message);
            }

            return Octets.ToText(stream.ToArray());
        }

        /// <summary>
        /// Writes a message asynchronously, to a stream that can only be written asynchronously.
        /// </summary>
        public static async Task<string> WriteAsync(ImfEntitySource message, ImfOptions options, int blockSize)
        {
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = new ImfEntityWriter(stream, options, blockSize, false, NextBoundary());
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteMessageAsync(message));
            return Octets.ToText(stream.ToArray());
        }

        /// <summary>
        /// Reads a whole message for its body chunks, decoded, joined together, with one byte in each character.
        /// </summary>
        public static string ReadDecodedBody(string message)
        {
            using (var reader = new ImfEntityReader(new MemoryStream(Octets.Of(message)), new ImfOptions { DecodeTransferEncoding = true }))
            {
                return DecodedBodies.Read(reader);
            }
        }

        /// <summary>
        /// Reads each field of a header section, as its name and its value with no folds.
        /// </summary>
        public static List<string> ReadUnfoldedFields(string section)
        {
            var fields = new List<string>();
            using (var reader = HeaderSections.CreateReader(section))
            {
                while (reader.ReadField() is ImfHeaderFieldView field)
                {
                    fields.Add(field.GetName() + ":" + Octets.ToText(field.Value));
                }
            }

            return fields;
        }

        /// <summary>
        /// Reads a whole message with LF line breaks for its body chunks, decoded, joined together, with one byte in each character.
        /// </summary>
        public static string ReadContainerDecodedBody(string message, ImfOptions options)
        {
            using (var reader = new ImfEntityReader(new MemoryStream(Octets.Of(message)), options, DefaultBlockSize, true))
            {
                return DecodedBodies.Read(reader);
            }
        }

        /// <summary>
        /// Reads a whole message, and describes its events in the form of <see cref="EntityEvents"/>.
        /// </summary>
        public static List<string> ReadOutline(string message)
        {
            using (var reader = EntityEvents.CreateReader(message))
            {
                return EntityEvents.ReadOutline(reader);
            }
        }

        /// <summary>
        /// Copies a message with a header writer for its fields, and the end of the section, and then straight to the stream for the rest.
        /// </summary>
        public static string CopyWithHeaderWriter(string message)
        {
            var bytes = Octets.Of(message);
            var stream = new MemoryStream();
            using (var reader = HeaderSections.CreateReader(message))
            using (var writer = new ImfHeaderWriter(stream))
            {
                writer.WriteFields(reader);
                writer.WriteEndOfHeader();
                var bodyStart = (int)reader.BytePosition;
                stream.Write(bytes, bodyStart, bytes.Length - bodyStart);
            }

            return Octets.ToText(stream.ToArray());
        }

        /// <summary>
        /// Writes a multipart message again, from what an <see cref="ImfEntityReader"/> gives for it: its fields as they were read, the preamble and
        /// epilogue, and each part as a raw range of the data.  A message that keeps all of that unchanged comes out with the same bytes.
        /// </summary>
        public static string Rewrite(string message, ImfOptions options)
        {
            return Write(BuildFromRead(message), options);
        }

        /// <summary>
        /// Builds the description that <see cref="Rewrite"/> writes.
        /// </summary>
        public static ImfEntitySource BuildFromRead(string message)
        {
            var bytes = Octets.Of(message);
            var fields = new List<byte[]>();
            var parts = new List<ImfEntitySource>();
            ReadOnlyMemory<byte>? preamble = null;
            ReadOnlyMemory<byte>? epilogue = null;
            var contentType = default(DataStandardizer.Communication.Mime.MimeMediaType);
            using (var reader = new ImfEntityReader(new MemoryStream(bytes)))
            {
                while (reader.ReadEvent() is ImfEntityEvent entityEvent)
                {
                    switch (entityEvent.Kind)
                    {
                        case ImfEntityEventKind.HeaderField when entityEvent.Depth == 0:
                            fields.Add(entityEvent.Field.RawBytes.ToArray());
                            break;
                        case ImfEntityEventKind.HeaderEnd when entityEvent.Depth == 0:
                            contentType = entityEvent.ContentType;
                            break;
                        case ImfEntityEventKind.Preamble when entityEvent.Depth == 0:
                            preamble = Join(preamble, entityEvent.Data.ToArray());
                            break;
                        case ImfEntityEventKind.Epilogue when entityEvent.Depth == 0:
                            epilogue = Join(epilogue, entityEvent.Data.ToArray());
                            break;
                        case ImfEntityEventKind.EntityEnd when entityEvent.Depth == 1:
                            parts.Add(ImfEntitySource.FromRaw(new ReadOnlyMemory<byte>(bytes, (int)entityEvent.ByteOffset, (int)entityEvent.ByteLength)));
                            break;
                    }
                }
            }

            var source = ImfEntitySource.FromParts(contentType, parts);
            source.Preamble = preamble;
            source.Epilogue = epilogue;
            foreach (var field in fields)
            {
                source.AddRawField(field);
            }

            return source;
        }

        private static ReadOnlyMemory<byte> Join(ReadOnlyMemory<byte>? first, byte[] second)
        {
            if (first is null) return second;

            var joined = new byte[first.Value.Length + second.Length];
            first.Value.CopyTo(joined);
            second.CopyTo(joined, first.Value.Length);
            return joined;
        }
    }
}
