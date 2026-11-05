#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Builds cards from the tokens of a jCard document (RFC 7095), one token at a time.  Does no I/O, so that the synchronous and asynchronous readers share it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The document is a jCard object for one card, <c>["vcard", [...]]</c>, or an array of them for several (RFC 7095 §3.2).
    /// </para>
    /// <para>
    /// Each jCard object becomes a <c>BEGIN</c> and an <c>END</c> line, and each property array a content line, which are handed to a <see cref="VcardCardBuilder"/>; so the cards are
    /// built, and their <c>VERSION</c> checked, as for <c>.vcf</c> data.
    /// </para>
    /// </remarks>
    internal sealed class JcardDocumentBuilder
    {
        // ref. RFC 7095 §3.3.1.1: "vCards that conform to RFC 6350 will contain the value "4.0"".  jCard represents no other version.
        private const string SupportedVersion = "4.0";

        private enum FrameKind
        {
            /// <summary>
            /// The root array, before its first element shows whether it is a jCard object or an array of them.
            /// </summary>
            Root,
            Stream,
            Card,
            Properties,
            Property,
            Value
        }

        private readonly VcardCardBuilder _builder;
        private int _cardIndex = -1;
        private readonly List<Frame> _frames = new List<Frame>();
        private List<VcardDiagnostic> _valueDiagnostics = new List<VcardDiagnostic>();
        private bool _isRootStarted;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? _memberName;
#else
        private string _memberName;
#endif

        internal JcardDocumentBuilder(VcardOptions options)
        {
            _builder = new VcardCardBuilder(options);
        }

        /// <summary>
        /// Gets the number of cards completed.
        /// </summary>
        internal long CardsRead => _builder.CardsRead;

        /// <summary>
        /// Gets the number of properties completed, not counting <c>VERSION</c>.
        /// </summary>
        internal long PropertiesRead { get; private set; }

        /// <summary>
        /// Adds a token.
        /// </summary>
        /// <param name="tokenType">The type of token.</param>
        /// <param name="text">The text of a string or property name, without its JSON escaping, or of a number; otherwise <c>null</c>.</param>
        /// <param name="bytePosition">The 0-based offset of the token's first byte from the start of the data.</param>
        /// <returns>The card, if the token ends one; otherwise <c>null</c>.  The card has not been checked for conformance, and <see cref="TakeStructureDiagnostics"/> gives what was
        /// found in its <c>VERSION</c>.</returns>
        /// <exception cref="VcardFileException">The token is not allowed where it is, or ends a property that is not valid.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardCard? Add(JsonTokenType tokenType, string? text, long bytePosition)
#else
        internal VcardCard Add(JsonTokenType tokenType, string text, long bytePosition)
#endif
        {
            switch (tokenType)
            {
                case JsonTokenType.PropertyName:
                    _memberName = text;
                    return null;
                case JsonTokenType.EndArray:
                case JsonTokenType.EndObject:
                    return End();
                case JsonTokenType.StartArray:
                    return Start(JcardNodeKind.Array, string.Empty, bytePosition);
                case JsonTokenType.StartObject:
                    return Start(JcardNodeKind.Object, string.Empty, bytePosition);
                case JsonTokenType.String:
                    return Start(JcardNodeKind.String, text ?? string.Empty, bytePosition);
                case JsonTokenType.Number:
                    return Start(JcardNodeKind.Number, text ?? string.Empty, bytePosition);
                case JsonTokenType.True:
                    return Start(JcardNodeKind.True, string.Empty, bytePosition);
                case JsonTokenType.False:
                    return Start(JcardNodeKind.False, string.Empty, bytePosition);
                case JsonTokenType.Null:
                    return Start(JcardNodeKind.Null, string.Empty, bytePosition);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks that the data has ended where it may.
        /// </summary>
        /// <exception cref="VcardFileException">The data held no card.</exception>
        internal void Complete()
        {
            _builder.Complete();
        }

        /// <summary>
        /// Gets the problems found in the <c>VERSION</c> of the last card, then the values found not to be in the form jCard gives their type.
        /// </summary>
        internal List<VcardDiagnostic> TakeStructureDiagnostics()
        {
            var diagnostics = _builder.TakeStructureDiagnostics();
            diagnostics.AddRange(_valueDiagnostics);
            _valueDiagnostics = new List<VcardDiagnostic>();
            return diagnostics;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? Start(JcardNodeKind kind, string text, long bytePosition)
#else
        private VcardCard Start(JcardNodeKind kind, string text, long bytePosition)
#endif
        {
            if (_frames.Count == 0)
            {
                // ref. RFC 7095 §3.2: a jCard object, and several are held in "a simple JSON array".  The JSON reader allows only one value at the root.
                var root = new JcardNode(kind, text, null, 0, null, bytePosition);
                if (_isRootStarted || kind != JcardNodeKind.Array)
                    throw JcardConverter.BuildException(root, null,
                        Format("The data must be a jCard object, or an array of them, but it is {0}.", JcardConverter.DescribeKind(kind)));

                _isRootStarted = true;
                _frames.Add(new Frame(FrameKind.Root, root));
                return null;
            }

            var parent = _frames[_frames.Count - 1];
            var name = _memberName;
            _memberName = null;
            var node = new JcardNode(kind, text, parent.Node, parent.Count++, name, bytePosition);
            switch (parent.Kind)
            {
                case FrameKind.Root:
                    if (kind == JcardNodeKind.String)
                    {
                        // The root array is the jCard object of a single card.
                        parent.Kind = FrameKind.Card;
                        StartCard(node);
                        return null;
                    }

                    // The root array holds the jCard objects of the cards.
                    parent.Kind = FrameKind.Stream;
                    StartInStream(node);
                    return null;
                case FrameKind.Stream:
                    StartInStream(node);
                    return null;
                case FrameKind.Card:
                    StartInCard(node);
                    return null;
                case FrameKind.Properties:
                    if (kind != JcardNodeKind.Array)
                        throw JcardConverter.BuildException(node, null,
                            Format("Found {0} in the properties of a card; expected a property array.", JcardConverter.DescribeKind(kind)));

                    _frames.Add(new Frame(FrameKind.Property, node));
                    return null;
                default:
                    // A value within a property.
                    if (name is null)
                    {
                        parent.Node.Add(node);
                    }
                    else
                    {
                        parent.Node.Add(name, node);
                    }

                    if (kind == JcardNodeKind.Array || kind == JcardNodeKind.Object)
                    {
                        _frames.Add(new Frame(FrameKind.Value, node));
                    }

                    return null;
            }
        }

        private void StartInStream(JcardNode node)
        {
            if (node.Kind != JcardNodeKind.Array)
                throw JcardConverter.BuildException(node, null,
                    Format("Found {0} in the array of cards; expected a jCard object.", JcardConverter.DescribeKind(node.Kind)));

            _frames.Add(new Frame(FrameKind.Card, node));
        }

        private void StartInCard(JcardNode node)
        {
            // ref. RFC 7095 §3.2: "an array with two elements", the string "vcard" and an array of properties.
            switch (node.Index)
            {
                case 0 when node.Kind == JcardNodeKind.String:
                    StartCard(node);
                    return;
                case 1 when node.Kind == JcardNodeKind.Array:
                    _frames.Add(new Frame(FrameKind.Properties, node));
                    return;
                default:
                    throw BuildCardShapeException(node);
            }
        }

        private void StartCard(JcardNode nameNode)
        {
            if (nameNode.Text != JcardConverter.CardName)
                throw JcardConverter.BuildException(nameNode, null,
                    Format("Found \"{0}\" as the first element of a jCard object; expected \"{1}\": RFC 7095 §3.2.", nameNode.Text, JcardConverter.CardName));

            _cardIndex++;
            AddLine(VcardContentLine.FromConversion(null, VcardPropertyNames.Begin, new VcardContentLineParameter[0], VcardCardBuilder.CardValue, _cardIndex), nameNode);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? End()
#else
        private VcardCard End()
#endif
        {
            var frame = _frames[_frames.Count - 1];
            _frames.RemoveAt(_frames.Count - 1);
            switch (frame.Kind)
            {
                case FrameKind.Card:
                    if (frame.Count != 2)
                        throw BuildCardShapeException(frame.Node);

                    return AddLine(VcardContentLine.FromConversion(null, VcardPropertyNames.End, new VcardContentLineParameter[0], VcardCardBuilder.CardValue, _cardIndex),
                        frame.Node);
                case FrameKind.Property:
                    bool isValueNotInJcardForm;
                    var line = JcardConverter.ToContentLine(frame.Node, _cardIndex, out isValueNotInJcardForm);
                    if (isValueNotInJcardForm)
                    {
                        // ref. RFC 7095 §3.5: each type has its form in jCard.  The value is kept, and reported as a value that isn't valid, as AllowInvalidValues can accept.
                        _valueDiagnostics.Add(new VcardDiagnostic(VcardDiagnosticKind.InvalidValue,
                            Format("A value of the {0} property isn't in the form RFC 7095 §3.5 gives its type in jCard.", line.Name), line.Name, line.Group, 0, _cardIndex));
                    }

                    if (string.Equals(line.Name, VcardPropertyNames.Version, StringComparison.Ordinal))
                    {
                        CheckVersion(line, frame.Node);
                    }
                    else
                    {
                        PropertiesRead++;
                    }

                    return AddLine(line, frame.Node);
                default:
                    return null;
            }
        }

        private static void CheckVersion(VcardContentLine line, JcardNode node)
        {
            // ref. RFC 7095 §3.3.1.1: the version property has "the same value as in the vCard".  jCard is the JSON form of RFC 6350, whose VERSION-value is "4.0" (§6.7.9), so a
            // value of another version is one this reader can't read, as VcardReader can't read a version it doesn't know.
            if (line.Value == SupportedVersion) return;

            var exception = VcardConformance.BuildException(new VcardDiagnostic(VcardDiagnosticKind.UnsupportedVersion,
                Format("The card has VERSION:{0}.  jCard (RFC 7095) represents vCard 4.0 alone.", line.Value), line.Name, line.Group, 0, line.CardIndex));
            JcardConverter.AddLocation(exception, node);
            throw exception;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? AddLine(VcardContentLine line, JcardNode node)
#else
        private VcardCard AddLine(VcardContentLine line, JcardNode node)
#endif
        {
            try
            {
                return _builder.Add(line);
            }
            catch (VcardFileException exception)
            {
                // The builder has no line numbers to give, since the lines were not read from .vcf data.
                var data = (IDictionary)exception.Data;
                data.Remove(VcardFileException.DataItemName.LineNumber);
                data.Remove(VcardFileException.DataItemName.PhysicalLineNumber);
                JcardConverter.AddLocation(exception, node);
                throw;
            }
        }

        private static VcardFileException BuildCardShapeException(JcardNode node)
        {
            return JcardConverter.BuildException(node, null,
                "A jCard object must be an array of two elements: the string \"vcard\" and an array of its properties: RFC 7095 §3.2.");
        }

        private static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        /// <summary>
        /// An array or object being read.
        /// </summary>
        private sealed class Frame
        {
            internal Frame(FrameKind kind, JcardNode node)
            {
                Kind = kind;
                Node = node;
            }

            /// <summary>
            /// Gets or sets the number of elements or members read so far.
            /// </summary>
            internal int Count { get; set; }

            internal FrameKind Kind { get; set; }

            internal JcardNode Node { get; }
        }
    }
}
#endif
