using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A minimal reader and writer of the XML form of a property list, which is what an EMLX file ends with.  Binary property lists aren't supported, as Apple Mail writes XML.
    /// </summary>
    /// <remarks>
    /// The open values are kept in a list, not on the call stack, so nesting can't overflow the stack.
    /// </remarks>
    internal static class EmlxPlist
    {
        private const string ArrayName = "array";
        private const string DataName = "data";
        private const string DateName = "date";
        private const string DictName = "dict";
        private const string FalseName = "false";
        private const string IntegerName = "integer";
        private const string KeyName = "key";
        private const string PlistName = "plist";
        private const string RealName = "real";
        private const string StringName = "string";
        private const string TrueName = "true";
        private const string VersionName = "version";
        private const string VersionValue = "1.0";

        private const string DocumentTypePublicId = "-//Apple//DTD PLIST 1.0//EN";
        private const string DocumentTypeSystemId = "http://www.apple.com/DTDs/PropertyList-1.0.dtd";
        private const string XmlDeclaration = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n";
        private const string LineBreak = "\n";

        // Written as raw text, as an XmlWriter would put a space before the slash, which isn't what Apple's tools write.
        private const string EmptyArray = "<array/>";
        private const string EmptyDict = "<dict/>";
        private const string FalseElement = "<false/>";
        private const string TrueElement = "<true/>";

        /// <summary>
        /// Reads the <c>dict</c> at the root of a property list.
        /// </summary>
        /// <param name="bytes">The bytes of the document.</param>
        /// <param name="maxNestingDepth">The deepest that values may nest.</param>
        /// <param name="entries">Receives the keys and values, or an empty list if the document isn't well formed.</param>
        /// <param name="problem">Receives a description of what is wrong, or an empty string.</param>
        /// <returns><c>true</c> if the document is well formed.</returns>
        internal static bool TryRead(byte[] bytes, int maxNestingDepth, out List<KeyValuePair<string, EmlxPlistValue>> entries, out string problem)
        {
            entries = new List<KeyValuePair<string, EmlxPlistValue>>();
            problem = string.Empty;

            // ref. a DOCTYPE is what every property list starts with, and it is skipped, not processed, so that no entity can expand and nothing outside the file is fetched.
            var settings = new XmlReaderSettings
            {
                CloseInput = false,
                DtdProcessing = DtdProcessing.Ignore,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
            };

            try
            {
                using (var reader = XmlReader.Create(new MemoryStream(bytes, false), settings))
                {
                    var root = ReadDocument(reader, maxNestingDepth, out problem);
                    if (root is null) return false;

                    entries = new List<KeyValuePair<string, EmlxPlistValue>>(root.Entries);
                    return true;
                }
            }
            catch (XmlException exception)
            {
                problem = exception.Message;
                return false;
            }
            catch (ArgumentException exception)
            {
                // An encoding that can't be used, or bytes that it can't decode.
                problem = exception.Message;
                return false;
            }
        }

        /// <summary>
        /// Writes a property list whose root is a <c>dict</c>, laid out as Apple's tools lay it out.
        /// </summary>
        /// <param name="entries">The keys and values.</param>
        /// <returns>The bytes of the document.</returns>
        internal static byte[] Write(IReadOnlyList<KeyValuePair<string, EmlxPlistValue>> entries)
        {
            var output = new MemoryStream();
            var declaration = Encoding.UTF8.GetBytes(XmlDeclaration);
            output.Write(declaration, 0, declaration.Length);

            // The declaration is written above, as an XmlWriter would write its encoding as "utf-8", which isn't what Apple's tools write.
            var settings = new XmlWriterSettings
            {
                CloseOutput = false,
                Encoding = new UTF8Encoding(false),
                NewLineHandling = NewLineHandling.Entitize,
                OmitXmlDeclaration = true,
            };

            using (var writer = XmlWriter.Create(output, settings))
            {
                writer.WriteDocType(PlistName, DocumentTypePublicId, DocumentTypeSystemId, null);
                writer.WriteWhitespace(LineBreak);
                writer.WriteStartElement(PlistName);
                writer.WriteAttributeString(VersionName, VersionValue);
                writer.WriteWhitespace(LineBreak);
                WriteValue(writer, EmlxPlistValue.CreateDictionary(new List<KeyValuePair<string, EmlxPlistValue>>(entries)));
                writer.WriteWhitespace(LineBreak);
                writer.WriteEndElement();
                writer.WriteWhitespace(LineBreak);
            }

            return output.ToArray();
        }

        /// <summary>
        /// Gets how deeply the values under a <c>dict</c> nest, counted as <see cref="TryRead"/> counts them: the <c>dict</c> itself is at depth 1, and each <c>dict</c> or <c>array</c> in it is
        /// one deeper.
        /// </summary>
        /// <param name="entries">The keys and values of the <c>dict</c>.</param>
        internal static int GetDepth(IReadOnlyList<KeyValuePair<string, EmlxPlistValue>> entries)
        {
            var deepest = 1;
            var pending = new Stack<KeyValuePair<EmlxPlistValue, int>>();
            foreach (var entry in entries)
            {
                pending.Push(new KeyValuePair<EmlxPlistValue, int>(entry.Value, 2));
            }

            while (pending.Count > 0)
            {
                var next = pending.Pop();
                var value = next.Key;
                if (value.Kind != EmlxPlistValueKind.Dictionary && value.Kind != EmlxPlistValueKind.Array) continue;

                deepest = Math.Max(deepest, next.Value);
                foreach (var item in value.Items)
                {
                    pending.Push(new KeyValuePair<EmlxPlistValue, int>(item, next.Value + 1));
                }

                foreach (var entry in value.Entries)
                {
                    pending.Push(new KeyValuePair<EmlxPlistValue, int>(entry.Value, next.Value + 1));
                }
            }

            return deepest;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static EmlxPlistValue? ReadDocument(XmlReader reader, int maxNestingDepth, out string problem)
#else
        private static EmlxPlistValue ReadDocument(XmlReader reader, int maxNestingDepth, out string problem)
#endif
        {
            problem = string.Empty;
            var open = new Stack<ReadFrame>();
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            EmlxPlistValue? root = null;
#else
            EmlxPlistValue root = null;
#endif
            var hasPlist = false;
            var isPlistOpen = false;

            var hasNode = reader.Read();
            while (hasNode)
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        var name = reader.Name;
                        if (!isPlistOpen)
                        {
                            if (name != PlistName || hasPlist)
                            {
                                problem = "The root element isn't a plist.";
                                return null;
                            }

                            hasPlist = true;
                            isPlistOpen = !reader.IsEmptyElement;
                            hasNode = reader.Read();
                            continue;
                        }

                        var frame = open.Count == 0 ? null : open.Peek();
                        if (name == KeyName)
                        {
                            if (frame is null || frame.Entries is null || !(frame.PendingKey is null))
                            {
                                problem = "A key is somewhere that a value should be.";
                                return null;
                            }

                            frame.PendingKey = reader.ReadElementContentAsString();
                            hasNode = !reader.EOF;
                            continue;
                        }

                        if (!(frame is null) && !(frame.Entries is null) && frame.PendingKey is null)
                        {
                            problem = "A value in a dict has no key.";
                            return null;
                        }

                        if (frame is null && !(root is null))
                        {
                            problem = "A plist holds more than one value.";
                            return null;
                        }

                        if (name == DictName || name == ArrayName)
                        {
                            if (open.Count >= maxNestingDepth)
                            {
                                problem = "The values nest too deeply.";
                                return null;
                            }

                            var isEmpty = reader.IsEmptyElement;
                            var opened = new ReadFrame(name == DictName);
                            hasNode = reader.Read();
                            if (isEmpty)
                            {
                                AddValue(open, opened.ToValue(), ref root);
                            }
                            else
                            {
                                open.Push(opened);
                            }

                            continue;
                        }

                        if (!TryGetScalarKind(name, out var kind))
                        {
                            problem = "An element isn't one that a property list may hold: " + name;
                            return null;
                        }

                        var text = reader.ReadElementContentAsString();
                        hasNode = !reader.EOF;
                        if (!EmlxPlistValue.IsValidText(kind, text))
                        {
                            problem = "The text of a " + name + " element isn't valid.";
                            return null;
                        }

                        AddValue(open, EmlxPlistValue.CreateScalar(kind, text), ref root);
                        continue;

                    case XmlNodeType.EndElement:
                        if (open.Count > 0)
                        {
                            var closed = open.Pop();
                            if (!(closed.PendingKey is null))
                            {
                                problem = "A key in a dict has no value.";
                                return null;
                            }

                            AddValue(open, closed.ToValue(), ref root);
                        }
                        else
                        {
                            isPlistOpen = false;
                        }

                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.CDATA:
                        problem = "Text is somewhere that an element should be.";
                        return null;
                }

                hasNode = reader.Read();
            }

            if (root is null)
            {
                problem = "The plist holds no value.";
                return null;
            }

            if (root.Kind != EmlxPlistValueKind.Dictionary)
            {
                problem = "The value in the plist isn't a dict.";
                return null;
            }

            return root;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static void AddValue(Stack<ReadFrame> open, EmlxPlistValue value, ref EmlxPlistValue? root)
#else
        private static void AddValue(Stack<ReadFrame> open, EmlxPlistValue value, ref EmlxPlistValue root)
#endif
        {
            if (open.Count == 0)
            {
                root = value;
                return;
            }

            open.Peek().Add(value);
        }

        private static bool TryGetScalarKind(string name, out EmlxPlistValueKind kind)
        {
            switch (name)
            {
                case StringName:
                    kind = EmlxPlistValueKind.String;
                    return true;
                case IntegerName:
                    kind = EmlxPlistValueKind.Integer;
                    return true;
                case RealName:
                    kind = EmlxPlistValueKind.Real;
                    return true;
                case DateName:
                    kind = EmlxPlistValueKind.Date;
                    return true;
                case DataName:
                    kind = EmlxPlistValueKind.Data;
                    return true;
                case TrueName:
                    kind = EmlxPlistValueKind.True;
                    return true;
                case FalseName:
                    kind = EmlxPlistValueKind.False;
                    return true;
                default:
                    kind = default(EmlxPlistValueKind);
                    return false;
            }
        }

        private static void WriteValue(XmlWriter writer, EmlxPlistValue root)
        {
            var open = new Stack<WriteFrame>();
            WriteOpening(writer, root, open);
            while (open.Count > 0)
            {
                var frame = open.Peek();
                if (frame.IsDone)
                {
                    open.Pop();
                    writer.WriteWhitespace(LineBreak + new string('\t', open.Count));
                    writer.WriteEndElement();
                    continue;
                }

                writer.WriteWhitespace(LineBreak + new string('\t', open.Count));
                if (frame.HasKey)
                {
                    writer.WriteElementString(KeyName, frame.CurrentKey);
                    writer.WriteWhitespace(LineBreak + new string('\t', open.Count));
                }

                WriteOpening(writer, frame.TakeValue(), open);
            }
        }

        // Writes a value with nothing in it whole, and otherwise opens it for the loop above to fill.
        private static void WriteOpening(XmlWriter writer, EmlxPlistValue value, Stack<WriteFrame> open)
        {
            switch (value.Kind)
            {
                case EmlxPlistValueKind.Dictionary:
                    if (value.Entries.Count == 0)
                    {
                        writer.WriteRaw(EmptyDict);
                        break;
                    }

                    writer.WriteStartElement(DictName);
                    open.Push(new WriteFrame(value));
                    break;
                case EmlxPlistValueKind.Array:
                    if (value.Items.Count == 0)
                    {
                        writer.WriteRaw(EmptyArray);
                        break;
                    }

                    writer.WriteStartElement(ArrayName);
                    open.Push(new WriteFrame(value));
                    break;
                case EmlxPlistValueKind.True:
                    writer.WriteRaw(TrueElement);
                    break;
                case EmlxPlistValueKind.False:
                    writer.WriteRaw(FalseElement);
                    break;
                case EmlxPlistValueKind.String:
                    writer.WriteElementString(StringName, value.Text);
                    break;
                case EmlxPlistValueKind.Integer:
                    writer.WriteElementString(IntegerName, value.Text);
                    break;
                case EmlxPlistValueKind.Real:
                    writer.WriteElementString(RealName, value.Text);
                    break;
                case EmlxPlistValueKind.Date:
                    writer.WriteElementString(DateName, value.Text);
                    break;
                case EmlxPlistValueKind.Data:
                    writer.WriteElementString(DataName, value.Text);
                    break;
            }
        }

        /// <summary>
        /// A <c>dict</c> or <c>array</c> whose end has not been read.
        /// </summary>
        private sealed class ReadFrame
        {
            private readonly List<EmlxPlistValue> _items = new List<EmlxPlistValue>();

            internal ReadFrame(bool isDictionary)
            {
                if (isDictionary) Entries = new List<KeyValuePair<string, EmlxPlistValue>>();
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal List<KeyValuePair<string, EmlxPlistValue>>? Entries { get; }

            internal string? PendingKey { get; set; }
#else
            internal List<KeyValuePair<string, EmlxPlistValue>> Entries { get; }

            internal string PendingKey { get; set; }
#endif

            internal void Add(EmlxPlistValue value)
            {
                if (Entries is null)
                {
                    _items.Add(value);
                    return;
                }

                Entries.Add(new KeyValuePair<string, EmlxPlistValue>(PendingKey ?? string.Empty, value));
                PendingKey = null;
            }

            internal EmlxPlistValue ToValue()
            {
                return Entries is null ? EmlxPlistValue.CreateArray(_items) : EmlxPlistValue.CreateDictionary(Entries);
            }
        }

        /// <summary>
        /// A <c>dict</c> or <c>array</c> whose end has not been written.
        /// </summary>
        private sealed class WriteFrame
        {
            private readonly EmlxPlistValue _value;
            private int _index;

            internal WriteFrame(EmlxPlistValue value)
            {
                _value = value;
                HasKey = value.Kind == EmlxPlistValueKind.Dictionary;
            }

            internal bool HasKey { get; }

            internal bool IsDone => _index >= (HasKey ? _value.Entries.Count : _value.Items.Count);

            internal string CurrentKey => _value.Entries[_index].Key;

            internal EmlxPlistValue TakeValue()
            {
                var value = HasKey ? _value.Entries[_index].Value : _value.Items[_index];
                _index++;
                return value;
            }
        }
    }
}
