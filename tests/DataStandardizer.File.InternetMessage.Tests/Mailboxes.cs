using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Reads and writes whole mbox files for the tests, and describes what they hold as text that a test can compare with literals.
    /// </summary>
    /// <remarks>
    /// A message is described by its bytes as an <see cref="ImfWriter"/> writes them back, with one byte in each character.  A read that fails is described by the kind of problem that it found.
    /// </remarks>
    internal static class Mailboxes
    {
        /// <summary>
        /// Describes a message, with the line breaks it was read with.
        /// </summary>
        public static string Describe(ImfMessage message)
        {
            var output = new MemoryStream();
            using (var writer = new ImfWriter(output, new ImfOptions { AllowMissingRequiredFields = true, AllowInvalidValues = true, AllowCardinalityViolations = true }, message.LineFeedIsLineBreak, ImfEntityWriter.GenerateBoundary))
            {
                writer.WriteMessage(message);
            }

            return Octets.ToText(output.ToArray());
        }

        /// <summary>
        /// Reads a whole file, and describes each message.
        /// </summary>
        public static List<string> ReadAll(string mailbox, MboxVariant variant = MboxVariant.Mboxrd, ImfOptions options = null)
        {
            using (var reader = new MboxReader(new MemoryStream(Octets.Of(mailbox)), variant, options ?? new ImfOptions()))
            {
                return ReadAll(reader);
            }
        }

        /// <summary>
        /// Reads what is left of a file, and describes each message.
        /// </summary>
        public static List<string> ReadAll(MboxReader reader)
        {
            var messages = new List<string>();
            for (var entry = reader.ReadMessage(); entry != null; entry = reader.ReadMessage())
            {
                messages.Add(Describe(entry.Message));
            }

            return messages;
        }

        /// <summary>
        /// Reads what is left of a file asynchronously, and describes each message.
        /// </summary>
        public static async Task<List<string>> ReadAllAsync(MboxReader reader)
        {
            var messages = new List<string>();
            for (var entry = await reader.ReadMessageAsync(); entry != null; entry = await reader.ReadMessageAsync())
            {
                messages.Add(Describe(entry.Message));
            }

            return messages;
        }

        /// <summary>
        /// Reads a whole file, carrying on after a failed read of a message, and gives for each read the message's description, or the name of the exception's type and the message index it gives.
        /// </summary>
        public static List<string> ReadEach(string mailbox, ImfOptions options = null)
        {
            var results = new List<string>();
            using (var reader = new MboxReader(new MemoryStream(Octets.Of(mailbox)), MboxVariant.Mboxrd, options ?? new ImfOptions()))
            {
                while (true)
                {
                    try
                    {
                        var entry = reader.ReadMessage();
                        if (entry is null) return results;

                        results.Add(Describe(entry.Message));
                    }
                    catch (ImfFileException exception)
                    {
                        results.Add("rejected " + exception.Data[ImfFileException.DataItemName.MessageIndex]);
                    }
                }
            }
        }

        /// <summary>
        /// Reads until an error, and swallows the exception, to leave the reader as that error left it.
        /// </summary>
        public static void ReadUntilRejected(MboxReader reader)
        {
            try
            {
                ReadAll(reader);
            }
            catch (ImfFileException)
            {
            }
        }

        /// <summary>
        /// Starts a read that is canceled, and swallows the exception, to leave the reader as that left it.
        /// </summary>
        public static async Task ReadCanceledAsync(MboxReader reader)
        {
            try
            {
                await reader.ReadMessageAsync(new CancellationToken(true));
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Starts a write that is canceled, and swallows the exception, to leave the writer as that left it.
        /// </summary>
        public static async Task WriteCanceledAsync(MboxWriter writer, MboxEnvelope envelope, ImfMessage message)
        {
            try
            {
                await writer.WriteMessageAsync(envelope, message, new CancellationToken(true));
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Writes a message that is refused, and swallows the exception, to leave the writer as that left it.
        /// </summary>
        public static void WriteRefused(MboxWriter writer, MboxEnvelope envelope, ImfMessage message)
        {
            try
            {
                writer.WriteMessage(envelope, message);
            }
            catch (ImfFileException)
            {
            }
        }

        /// <summary>
        /// Reads the events of every message of a file asynchronously, and gives how many messages there were.
        /// </summary>
        public static async Task<int> ReadAllEventsAsync(MboxReader reader)
        {
            var count = 0;
            for (var events = await reader.ReadEntityEventsAsync(); events != null; events = await reader.ReadEntityEventsAsync())
            {
                await EntityEvents.ReadToEndAsync(events);
                count++;
            }

            return count;
        }

        /// <summary>
        /// Reads a whole file, and gives the kind of each problem with its format that was recorded, message by message.
        /// </summary>
        public static List<ImfDiagnosticKind> ReadDiagnosticKinds(string mailbox, MboxVariant variant, ImfOptions options)
        {
            var kinds = new List<ImfDiagnosticKind>();
            using (var reader = new MboxReader(new MemoryStream(Octets.Of(mailbox)), variant, options))
            {
                for (var entry = reader.ReadMessage(); entry != null; entry = reader.ReadMessage())
                {
                    foreach (var diagnostic in entry.Diagnostics)
                    {
                        kinds.Add(diagnostic.Kind);
                    }
                }

                foreach (var diagnostic in reader.Diagnostics)
                {
                    if (!kinds.Contains(diagnostic.Kind)) kinds.Add(diagnostic.Kind);
                }
            }

            return kinds;
        }

        /// <summary>
        /// Reads a whole file through its events, and gives the separator line of each message followed by the bytes of its bodies.
        /// </summary>
        public static List<string> ReadBodiesFromEvents(string mailbox)
        {
            var results = new List<string>();
            using (var reader = new MboxReader(new MemoryStream(Octets.Of(mailbox))))
            {
                for (var events = reader.ReadEntityEvents(); events != null; events = reader.ReadEntityEvents())
                {
                    var text = new StringBuilder(reader.Envelope.ToString()).Append('|');
                    for (var item = events.ReadEvent(); item != null; item = events.ReadEvent())
                    {
                        if (item.Value.Kind == ImfEntityEventKind.BodyChunk) text.Append(Octets.ToText(item.Value.Data));
                    }

                    results.Add(text.ToString());
                }
            }

            return results;
        }

        /// <summary>
        /// Reads only the first event of each message of a file, and gives the separator lines.
        /// </summary>
        public static List<string> ReadEnvelopesSkippingEvents(string mailbox)
        {
            var results = new List<string>();
            using (var reader = new MboxReader(new MemoryStream(Octets.Of(mailbox))))
            {
                for (var events = reader.ReadEntityEvents(); events != null; events = reader.ReadEntityEvents())
                {
                    events.ReadEvent();
                    results.Add(reader.Envelope.ToString());
                }
            }

            return results;
        }

        /// <summary>
        /// Reads each message of a file and keeps none of them, and gives how many there were and the most that the reader ever had from the stream beyond what it had used.
        /// </summary>
        public static (long Count, long MostHeld) ReadAndMeasure(MboxReader reader, Func<long> bytesGiven)
        {
            long count = 0;
            long mostHeld = 0;
            while (reader.ReadMessage() != null)
            {
                count++;
                mostHeld = Math.Max(mostHeld, bytesGiven() - reader.BytePosition);
            }

            return (count, mostHeld);
        }

        /// <summary>
        /// Reads a message from the bytes of a standalone message.
        /// </summary>
        public static ImfMessage ReadMessage(string message, ImfOptions options = null)
        {
            using (var reader = new ImfReader(new MemoryStream(Octets.Of(message)), options ?? new ImfOptions()))
            {
                return reader.ReadMessage();
            }
        }

        /// <summary>
        /// Reads the first message of a file, with its separator line.
        /// </summary>
        public static MboxEntry ReadFirst(string mailbox, MboxVariant variant = MboxVariant.Mboxrd, ImfOptions options = null)
        {
            using (var reader = new MboxReader(new MemoryStream(Octets.Of(mailbox)), variant, options ?? new ImfOptions()))
            {
                return reader.ReadMessage();
            }
        }

        /// <summary>
        /// Writes a file of one message, and gives its bytes.
        /// </summary>
        public static string WriteOne(MboxEnvelope envelope, ImfMessage message, MboxVariant variant = MboxVariant.Mboxrd, MboxLineBreak lineBreak = MboxLineBreak.LineFeed, ImfOptions options = null)
        {
            var output = new MemoryStream();
            using (var writer = new MboxWriter(output, variant, options ?? new ImfOptions(), lineBreak))
            {
                writer.WriteMessage(envelope, message);
            }

            return Octets.ToText(output.ToArray());
        }

        /// <summary>
        /// Reads a file of messages and writes them in another variant, and gives what was read back from that, as it describes each message.
        /// </summary>
        public static List<string> RoundTrip(string mailbox, MboxVariant variant)
        {
            var output = new MemoryStream();
            using (var reader = new MboxReader(new MemoryStream(Octets.Of(mailbox))))
            using (var writer = new MboxWriter(output, variant, new ImfOptions()))
            {
                for (var entry = reader.ReadMessage(); entry != null; entry = reader.ReadMessage())
                {
                    writer.WriteMessage(entry.Envelope, entry.Message);
                }
            }

            return ReadAll(Octets.ToText(output.ToArray()), variant);
        }

        /// <summary>
        /// Builds a file of many messages, each the same as the given one.
        /// </summary>
        public static byte[] Repeat(string entry, int count)
        {
            var bytes = Octets.Of(entry);
            var output = new byte[bytes.Length * count];
            for (var index = 0; index < count; index++)
            {
                Array.Copy(bytes, 0, output, index * bytes.Length, bytes.Length);
            }

            return output;
        }
    }
}
