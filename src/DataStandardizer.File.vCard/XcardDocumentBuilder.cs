using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Builds cards from the nodes of an xCard document (RFC 6351), one node at a time.  Does no I/O, so that the synchronous and asynchronous readers share it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The document's root is a <c>vcards</c> element holding one or more <c>vcard</c> elements (RFC 6351 §5).  Each <c>vcard</c> element becomes a <c>BEGIN</c>, a <c>VERSION</c>
    /// of 4.0, which the namespace gives (§5.1), and an <c>END</c> line, and each property element a content line, which are handed to a <see cref="VcardCardBuilder"/>; so the
    /// cards are built as for <c>.vcf</c> data.
    /// </para>
    /// <para>
    /// A child of a <c>vcard</c> or <c>group</c> element in another namespace is a property that the reader copies as it is into an <c>XML</c> property (RFC 6351 §6); any other
    /// element in another namespace is skipped with its content (§5.1).
    /// </para>
    /// </remarks>
    internal sealed class XcardDocumentBuilder
    {
        private enum FrameKind
        {
            VCards,
            VCard,
            Group,
            Property,
            Skipped
        }

        /// <summary>
        /// What the reader does with an element it has started.
        /// </summary>
        internal enum StartResult
        {
            /// <summary>
            /// Carry on to the next node.
            /// </summary>
            Continue,

            /// <summary>
            /// Read the element's markup, which is a property in another namespace, and give it to <see cref="AddForeignProperty"/>.
            /// </summary>
            ReadForeignProperty
        }

        private readonly VcardCardBuilder _builder;
        private int _cardIndex = -1;
        private readonly List<Frame> _frames = new List<Frame>();
        private bool _isRootStarted;
        private List<VcardDiagnostic> _valueDiagnostics = new List<VcardDiagnostic>();

        internal XcardDocumentBuilder(VcardOptions options)
        {
            _builder = new VcardCardBuilder(options);
        }

        /// <summary>
        /// Gets the number of cards completed.
        /// </summary>
        internal long CardsRead => _builder.CardsRead;

        /// <summary>
        /// Gets a flag indicating if the root element has ended, so that the document holds no more cards.
        /// </summary>
        internal bool IsRootEnded { get; private set; }

        /// <summary>
        /// Gets the number of properties completed, not counting <c>VERSION</c>.
        /// </summary>
        internal long PropertiesRead { get; private set; }

        /// <summary>
        /// Adds the start of an element.
        /// </summary>
        /// <param name="localName">The local name.</param>
        /// <param name="namespaceUri">The namespace.</param>
        /// <param name="groupName">The value of the element's <c>name</c> attribute, with no namespace; or <c>null</c> if it has none.</param>
        /// <param name="isEmpty"><c>true</c> if the element is empty, so that no end will be added for it.</param>
        /// <param name="lineNumber">The 1-based line of the element; or 0 if not known.</param>
        /// <param name="linePosition">The 1-based position of the element on its line; or 0 if not known.</param>
        /// <param name="card">The card, if the element is empty and ends one; otherwise <c>null</c>.  The card has not been checked for conformance.</param>
        /// <returns>What the reader does next.</returns>
        /// <exception cref="VcardFileException">The element is not allowed where it is.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal StartResult StartElement(string localName, string namespaceUri, string? groupName, bool isEmpty, long lineNumber, long linePosition, out VcardCard? card)
#else
        internal StartResult StartElement(string localName, string namespaceUri, string groupName, bool isEmpty, long lineNumber, long linePosition, out VcardCard card)
#endif
        {
            card = null;
            Frame frame;
            if (_frames.Count == 0)
            {
                // ref. RFC 6351 §5: "The <vcards> element MUST be present even when only a single vCard is present in an XML document."
                if (_isRootStarted || localName != XcardNames.VCards || namespaceUri != XcardNames.Namespace)
                    throw XcardConverter.BuildException(
                        Format("Found the element {0}; expected the xCard root element, vcards in the namespace {1}: RFC 6351 §5.", localName, XcardNames.Namespace),
                        lineNumber, linePosition, null);

                _isRootStarted = true;
                frame = new Frame(FrameKind.VCards, localName);
            }
            else
            {
                var parent = _frames[_frames.Count - 1];

                // ref. RFC 6351 §6: "Properties in other namespaces are wrapped as is inside an "XML" property."  An element in no namespace can't be one (RFC 6350 §6.1.5), so it
                // is ignored, as other elements whose name isn't recognised are (RFC 6351 §5.1).
                if ((parent.Kind == FrameKind.VCard || parent.Kind == FrameKind.Group) && namespaceUri != XcardNames.Namespace && namespaceUri.Length != 0)
                    return StartResult.ReadForeignProperty;

                frame = StartChild(parent, localName, namespaceUri, groupName, lineNumber, linePosition);
            }

            _frames.Add(frame);
            if (isEmpty)
            {
                card = EndElement(lineNumber, linePosition);
            }

            return StartResult.Continue;
        }

        /// <summary>
        /// Adds the end of the element most recently started.
        /// </summary>
        /// <returns>The card, if the element ends one; otherwise <c>null</c>.  The card has not been checked for conformance.</returns>
        /// <exception cref="VcardFileException">The element is not valid.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal VcardCard? EndElement(long lineNumber, long linePosition)
#else
        internal VcardCard EndElement(long lineNumber, long linePosition)
#endif
        {
            var frame = _frames[_frames.Count - 1];
            _frames.RemoveAt(_frames.Count - 1);
            switch (frame.Kind)
            {
                case FrameKind.VCards:
                    IsRootEnded = true;
                    return null;
                case FrameKind.VCard:
                    return AddLine(VcardContentLine.FromConversion(null, VcardPropertyNames.End, new VcardContentLineParameter[0], VcardCardBuilder.CardValue, _cardIndex),
                        lineNumber, linePosition);
                case FrameKind.Property:
                    // Only the property element itself, a child of a vcard or group element, becomes a line.
                    var parentKind = _frames[_frames.Count - 1].Kind;
                    if (parentKind != FrameKind.VCard && parentKind != FrameKind.Group) return null;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    var element = frame.Element!;
#else
                    var element = frame.Element;
#endif
                    bool isValueNotInXcardForm;
                    var line = XcardConverter.ToContentLine(element, GetGroup(), _cardIndex, out isValueNotInXcardForm);
                    if (isValueNotInXcardForm)
                    {
                        // ref. RFC 6351 Appendix A: each type has its form in xCard.  The value is kept, and reported as a value that isn't valid, as AllowInvalidValues can accept.
                        _valueDiagnostics.Add(new VcardDiagnostic(VcardDiagnosticKind.InvalidValue,
                            Format("A value of the {0} property isn't in the form RFC 6351 Appendix A gives it in xCard.", line.Name), line.Name, line.Group, 0,
                            _cardIndex));
                    }

                    PropertiesRead++;
                    return AddLine(line, element.LineNumber, element.LinePosition);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Adds a property in another namespace, which becomes an <c>XML</c> property (RFC 6351 §6).
        /// </summary>
        /// <param name="xml">The markup of the element, as the reader read it.</param>
        /// <param name="lineNumber">The 1-based line of the element; or 0 if not known.</param>
        /// <param name="linePosition">The 1-based position of the element on its line; or 0 if not known.</param>
        internal void AddForeignProperty(string xml, long lineNumber, long linePosition)
        {
            // ref. RFC 6350 §6.1.5: the value of XML is TEXT, so its escaping is carried out (RFC 6351 §6).
            var buffer = new char[VcardText.GetMaxEscapedLength(xml.Length)];
            var writer = new VcardSpanWriter(buffer);
            VcardText.AppendEscaped(ref writer, xml.AsSpan(), VcardVersion.V4_0, false);
            int charsWritten;
            var value = VcardValueFormatting.ToString(buffer, writer.TryComplete(out charsWritten), charsWritten);

            PropertiesRead++;
            AddLine(VcardContentLine.FromConversion(GetGroup(), VcardPropertyNames.Xml, new VcardContentLineParameter[0], value, _cardIndex), lineNumber, linePosition);
        }

        /// <summary>
        /// Adds text, CDATA or white space.
        /// </summary>
        /// <exception cref="VcardFileException">Text is not allowed where it is.</exception>
        internal void Text(string text, long lineNumber, long linePosition)
        {
            if (_frames.Count == 0) return;

            var frame = _frames[_frames.Count - 1];
            switch (frame.Kind)
            {
                case FrameKind.Property:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    frame.Element!.AppendText(text);
#else
                    frame.Element.AppendText(text);
#endif
                    return;
                case FrameKind.Skipped:
                    return;
                default:
                    foreach (var character in text)
                    {
                        if (!VcardValueFormatting.IsXmlWhitespace(character))
                            throw XcardConverter.BuildException(Format("Found text in the {0} element, which can hold only elements.", frame.ElementName), lineNumber,
                                linePosition, null);
                    }

                    return;
            }
        }

        /// <summary>
        /// Checks that the data has ended where it may.
        /// </summary>
        /// <exception cref="VcardFileException">The data held no xCard root element, or no card.</exception>
        internal void Complete()
        {
            if (!_isRootStarted)
            {
                var exception = new VcardFileException(Format("The data holds no xCard root element, vcards in the namespace {0}.", XcardNames.Namespace));
                exception.Data[VcardFileException.DataItemName.DiagnosticKind] = VcardDiagnosticKind.InvalidStructure;
                throw exception;
            }

            _builder.Complete();
        }

        /// <summary>
        /// Gets the problems found in the <c>VERSION</c> of the last card, then the values found not to be in the form xCard gives their type.
        /// </summary>
        internal List<VcardDiagnostic> TakeStructureDiagnostics()
        {
            var diagnostics = _builder.TakeStructureDiagnostics();
            diagnostics.AddRange(_valueDiagnostics);
            _valueDiagnostics = new List<VcardDiagnostic>();
            return diagnostics;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private Frame StartChild(Frame parent, string localName, string namespaceUri, string? groupName, long lineNumber, long linePosition)
#else
        private Frame StartChild(Frame parent, string localName, string namespaceUri, string groupName, long lineNumber, long linePosition)
#endif
        {
            // ref. RFC 6351 §5.1: "A vCard XML parser MUST ignore XML elements and attributes for which it doesn't recognize the expanded name."
            if (parent.Kind == FrameKind.Skipped || namespaceUri != XcardNames.Namespace) return new Frame(FrameKind.Skipped, localName);

            switch (parent.Kind)
            {
                case FrameKind.VCards:
                    if (localName != XcardNames.VCard) return new Frame(FrameKind.Skipped, localName);

                    StartCard(lineNumber, linePosition);
                    return new Frame(FrameKind.VCard, localName);
                case FrameKind.VCard:
                case FrameKind.Group:
                    if (localName != XcardNames.Group) return new Frame(FrameKind.Property, new XcardElement(localName, lineNumber, linePosition));

                    // ref. RFC 6351 Appendix A: a group is in a vcard element, holds properties, and has a name attribute.  ref. RFC 6350 §3.3: group = 1*(ALPHA / DIGIT / "-").
                    if (parent.Kind == FrameKind.Group)
                        throw XcardConverter.BuildException("A group element can't be in another: RFC 6351 Appendix A.", lineNumber, linePosition, null);
                    if (groupName is null || !XcardConverter.IsVcardName(groupName))
                        throw XcardConverter.BuildException(
                            "A group element must have a name attribute of one or more letters, digits and hyphens: RFC 6351 §5 and RFC 6350 §3.3.", lineNumber, linePosition,
                            null);

                    return new Frame(FrameKind.Group, localName) { GroupName = groupName };
                default:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    var child = parent.Element!.Add(new XcardElement(localName, lineNumber, linePosition));
#else
                    var child = parent.Element.Add(new XcardElement(localName, lineNumber, linePosition));
#endif
                    return new Frame(FrameKind.Property, child);
            }
        }

        private void StartCard(long lineNumber, long linePosition)
        {
            // ref. RFC 6351 §5.1: VERSION "is absent.  Its role is played by the vCard core namespace identifier, which includes the version number", that of vCard 4.0.
            _cardIndex++;
            AddLine(VcardContentLine.FromConversion(null, VcardPropertyNames.Begin, new VcardContentLineParameter[0], VcardCardBuilder.CardValue, _cardIndex), lineNumber,
                linePosition);
            AddLine(VcardContentLine.FromConversion(null, VcardPropertyNames.Version, new VcardContentLineParameter[0], VcardVersionMapping.Canonical.VersionText, _cardIndex),
                lineNumber, linePosition);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? GetGroup()
#else
        private string GetGroup()
#endif
        {
            var parent = _frames[_frames.Count - 1];
            return parent.Kind == FrameKind.Group ? parent.GroupName : null;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private VcardCard? AddLine(VcardContentLine line, long lineNumber, long linePosition)
#else
        private VcardCard AddLine(VcardContentLine line, long lineNumber, long linePosition)
#endif
        {
            try
            {
                return _builder.Add(line);
            }
            catch (VcardFileException exception)
            {
                // The builder has no lines of .vcf data to give, so the place in the XML document is given instead.
                var data = (IDictionary)exception.Data;
                data.Remove(VcardFileException.DataItemName.LineNumber);
                data.Remove(VcardFileException.DataItemName.PhysicalLineNumber);
                XcardConverter.AddLocation(exception, lineNumber, linePosition);
                throw;
            }
        }

        private static string Format(string format, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, format, arguments);
        }

        /// <summary>
        /// An element being read.
        /// </summary>
        private sealed class Frame
        {
            internal Frame(FrameKind kind, string elementName)
            {
                Kind = kind;
                ElementName = elementName;
            }

            internal Frame(FrameKind kind, XcardElement element) : this(kind, element.Name)
            {
                Element = element;
            }

            /// <summary>
            /// Gets the element being built, for a property element or an element within one.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal XcardElement? Element { get; }
#else
            internal XcardElement Element { get; }
#endif

            internal string ElementName { get; }

            /// <summary>
            /// Gets or sets the name of a group.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal string? GroupName { get; set; }
#else
            internal string GroupName { get; set; }
#endif

            internal FrameKind Kind { get; }
        }
    }
}
