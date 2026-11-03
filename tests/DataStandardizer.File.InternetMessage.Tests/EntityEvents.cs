using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Reads whole messages for the tests, and describes their events as lines of text that a test can compare with literals.
    /// </summary>
    /// <remarks>
    /// Each line is the kind of event and the entity's path, with <c>/</c> for the empty path of the message itself, and then what the event holds: a
    /// field's name, a media type, or between square brackets the bytes of a preamble, body or epilogue, with one byte in each character.  Neighbouring
    /// events of one kind for one entity are merged, so the lines don't depend on how the input was split into chunks.
    /// </remarks>
    internal static class EntityEvents
    {
        public static ImfEntityReader CreateReader(string message)
        {
            return new ImfEntityReader(new MemoryStream(Octets.Of(message)));
        }

        public static ImfEntityReader CreateReader(string message, ImfOptions options)
        {
            return new ImfEntityReader(new MemoryStream(Octets.Of(message)), options);
        }

        /// <summary>
        /// Reads the rest of the message, keeping nothing.
        /// </summary>
        public static void ReadToEnd(ImfEntityReader reader)
        {
            while (reader.ReadEvent() != null)
            {
            }
        }

        /// <summary>
        /// Reads the rest of the message asynchronously, keeping nothing.
        /// </summary>
        public static async Task ReadToEndAsync(ImfEntityReader reader)
        {
            while (await reader.ReadEventAsync() != null)
            {
            }
        }

        /// <summary>
        /// Reads until an error, and swallows the exception, to leave the reader as that error left it.
        /// </summary>
        public static void ReadUntilRejected(ImfEntityReader reader)
        {
            try
            {
                ReadToEnd(reader);
            }
            catch (ImfFileException)
            {
            }
        }

        /// <summary>
        /// Reads until an event of a kind, or the end of the message.
        /// </summary>
        /// <returns>The event, or <c>null</c> if the message ended first.</returns>
        public static ImfEntityEvent? ReadUntil(ImfEntityReader reader, ImfEntityEventKind kind)
        {
            while (reader.ReadEvent() is ImfEntityEvent entityEvent)
            {
                if (entityEvent.Kind == kind) return entityEvent;
            }

            return null;
        }

        /// <summary>
        /// Reads the rest of the message, and describes it.
        /// </summary>
        public static List<string> ReadOutline(ImfEntityReader reader)
        {
            var outline = new OutlineBuilder();
            while (reader.ReadEvent() is ImfEntityEvent entityEvent)
            {
                outline.Add(entityEvent);
            }

            return outline.Lines;
        }

        /// <summary>
        /// Reads the rest of the message asynchronously, and describes it.
        /// </summary>
        public static async Task<List<string>> ReadOutlineAsync(ImfEntityReader reader)
        {
            var outline = new OutlineBuilder();
            while (await reader.ReadEventAsync() is ImfEntityEvent entityEvent)
            {
                outline.Add(entityEvent);
            }

            return outline.Lines;
        }

        /// <summary>
        /// Reads a whole message, and gives for each entity, as it ends, its path and the bytes of the message in its raw byte range.
        /// </summary>
        public static List<string> ReadEntityRanges(string message)
        {
            var ranges = new List<string>();
            using (var reader = CreateReader(message))
            {
                while (reader.ReadEvent() is ImfEntityEvent entityEvent)
                {
                    if (entityEvent.Kind == ImfEntityEventKind.EntityEnd)
                    {
                        ranges.Add(DisplayPath(entityEvent.EntityPath) + " [" + message.Substring((int)entityEvent.ByteOffset, (int)entityEvent.ByteLength) + "]");
                    }
                }
            }

            return ranges;
        }

        /// <summary>
        /// Reads a whole message, and gives the raw byte range of each event as the event's kind, offset and length.
        /// </summary>
        public static List<string> ReadEventRanges(string message)
        {
            var ranges = new List<string>();
            using (var reader = CreateReader(message))
            {
                while (reader.ReadEvent() is ImfEntityEvent entityEvent)
                {
                    ranges.Add(entityEvent.Kind + " " + entityEvent.ByteOffset + " " + entityEvent.ByteLength);
                }
            }

            return ranges;
        }

        private static string DisplayPath(string path)
        {
            return path.Length == 0 ? "/" : path;
        }

        /// <summary>
        /// Turns events into lines, merging neighbouring events that hold data.
        /// </summary>
        private sealed class OutlineBuilder
        {
            private string _dataPrefix = string.Empty;
            private string _data = string.Empty;

            public List<string> Lines { get; } = new List<string>();

            public void Add(ImfEntityEvent entityEvent)
            {
                var path = DisplayPath(entityEvent.EntityPath);
                switch (entityEvent.Kind)
                {
                    case ImfEntityEventKind.EntityStart:
                        Lines.Add("start " + path);
                        break;
                    case ImfEntityEventKind.HeaderField:
                        Lines.Add("field " + path + " " + entityEvent.Field.GetName());
                        break;
                    case ImfEntityEventKind.HeaderEnd:
                        Lines.Add("type " + path + " " + entityEvent.ContentType.Type + "/" + entityEvent.ContentType.Subtype);
                        break;
                    case ImfEntityEventKind.Preamble:
                        AddData("preamble " + path, entityEvent);
                        return;
                    case ImfEntityEventKind.BodyChunk:
                        AddData("body " + path, entityEvent);
                        return;
                    case ImfEntityEventKind.Epilogue:
                        AddData("epilogue " + path, entityEvent);
                        return;
                    default:
                        Lines.Add("end " + path);
                        break;
                }
            }

            private void AddData(string prefix, ImfEntityEvent entityEvent)
            {
                var text = Octets.ToText(entityEvent.Data);
                var line = Lines.Count - 1;
                if (_dataPrefix == prefix && line >= 0 && Lines[line].StartsWith(prefix + " ["))
                {
                    _data += text;
                    Lines[line] = prefix + " [" + _data + "]";
                    return;
                }

                _dataPrefix = prefix;
                _data = text;
                Lines.Add(prefix + " [" + _data + "]");
            }
        }
    }

    /// <summary>
    /// Builds messages too long to write out.
    /// </summary>
    internal static class LongMessages
    {
        private const string EmbeddedMessageHeader = "Content-Type: message/rfc822\r\n\r\n";

        /// <summary>
        /// Gets a message with a chain of embedded messages, whose innermost message is at the given depth.
        /// </summary>
        public static string EmbeddedMessages(int depth)
        {
            var message = new System.Text.StringBuilder();
            for (var level = 0; level < depth; level++)
            {
                message.Append(EmbeddedMessageHeader);
            }

            return message.Append("A:1\r\n").ToString();
        }

        /// <summary>
        /// Gets a message with a chain of multipart entities, whose innermost part is at the given depth.  Each boundary has the one before as a
        /// prefix.
        /// </summary>
        public static string NestedMultiparts(int depth)
        {
            var opening = new System.Text.StringBuilder();
            var closing = new System.Text.StringBuilder();
            var boundary = string.Empty;
            for (var level = 0; level < depth; level++)
            {
                boundary += "b";
                opening.Append("Content-Type: multipart/mixed; boundary=" + boundary + "\r\n\r\n--" + boundary + "\r\n");
                closing.Insert(0, "\r\n--" + boundary + "--");
            }

            return opening.Append("\r\nx").Append(closing).ToString();
        }

        /// <summary>
        /// Gets a message with one multipart entity with the given number of parts.
        /// </summary>
        public static string Parts(int count)
        {
            var message = new System.Text.StringBuilder("Content-Type: multipart/mixed; boundary=b\r\n\r\n");
            for (var part = 0; part < count; part++)
            {
                message.Append("--b\r\n\r\nx\r\n");
            }

            return message.Append("--b--").ToString();
        }

        /// <summary>
        /// Gets a multipart message whose one part has a body of the given length, in lines.
        /// </summary>
        public static string PartWithBody(int length)
        {
            return "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\n" + Lines('x', length) + "\r\n--b--";
        }

        /// <summary>
        /// Gets a message with a base64 body of the given length, in lines.
        /// </summary>
        public static string Base64Body(int length)
        {
            return "Content-Transfer-Encoding: base64\r\n\r\n" + Lines('A', length);
        }

        /// <summary>
        /// Gets a multipart message with one part, and then a line that starts with a dash-boundary and the given number of spaces, which, if it is a
        /// delimiter, starts a second part.
        /// </summary>
        public static string DelimiterLineWithPadding(int paddingLength)
        {
            return "Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\nx\r\n--b" + new string(' ', paddingLength) + "\r\n\r\ny\r\n--b--";
        }

        /// <summary>
        /// Gets text of the given length, in lines of 72 characters and a line break.
        /// </summary>
        private static string Lines(char character, int length)
        {
            var text = new System.Text.StringBuilder(length);
            while (text.Length < length)
            {
                text.Append(character, 72).Append("\r\n");
            }

            return text.ToString();
        }

        /// <summary>
        /// Gets a multipart message whose one part holds many lines that start like a delimiter.
        /// </summary>
        public static string ManyCandidateLines(int count)
        {
            var message = new System.Text.StringBuilder("Content-Type: multipart/mixed; boundary=aaaa\r\n\r\n--aaaa\r\n\r\n");
            for (var line = 0; line < count; line++)
            {
                message.Append("--aaab\r\n");
            }

            return message.Append("\r\n--aaaa--").ToString();
        }
    }
}
