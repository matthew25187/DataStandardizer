using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Builds calendars from the nodes of an xCal document (RFC 6321), one node at a time.  Does no I/O, so that the synchronous and
    /// asynchronous readers share it.
    /// </summary>
    /// <remarks>
    /// Each component element becomes a <c>BEGIN</c> and an <c>END</c> line, and each property element a content line, which are handed to an
    /// <see cref="IcalCalendarBuilder"/>; so the calendars are built, and their nesting and <c>VERSION</c> checked, as for <c>.ics</c> data.
    /// Elements in another namespace are extensions, and are skipped with their content (RFC 6321 §4.1).
    /// </remarks>
    internal sealed class XcalDocumentBuilder
    {
        private enum FrameKind
        {
            ICalendar,
            Component,
            Properties,
            Components,
            Property,
            Skipped
        }

        private readonly IcalCalendarBuilder _builder = new IcalCalendarBuilder();
        private readonly List<Frame> _frames = new List<Frame>();
        private bool _isRootStarted;

        /// <summary>
        /// Gets the number of VCALENDAR objects completed.
        /// </summary>
        internal long CalendarsRead => _builder.CalendarsRead;

        /// <summary>
        /// Gets the number of components completed, not counting the VCALENDAR objects.
        /// </summary>
        internal long ComponentsRead => _builder.ComponentsRead;

        /// <summary>
        /// Gets a flag indicating if the root element has ended, so that the document holds no more calendars.
        /// </summary>
        internal bool IsRootEnded { get; private set; }

        /// <summary>
        /// Gets the number of properties completed.
        /// </summary>
        internal long PropertiesRead { get; private set; }

        /// <summary>
        /// Adds the start of an element.
        /// </summary>
        /// <returns>The calendar, if the element is empty and ends one; otherwise <c>null</c>.  The calendar has not been checked for
        /// conformance.</returns>
        /// <exception cref="IcalFileException">The element is not allowed where it is.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal IcalCalendar? StartElement(string localName, string namespaceUri, bool isEmpty, long lineNumber, long linePosition)
#else
        internal IcalCalendar StartElement(string localName, string namespaceUri, bool isEmpty, long lineNumber, long linePosition)
#endif
        {
            Frame frame;
            if (_frames.Count == 0)
            {
                // ref. RFC 6321§3.2: the entire stream is contained in the root icalendar element.
                if (_isRootStarted || localName != XcalNames.ICalendar || namespaceUri != XcalNames.Namespace)
                    throw XcalConverter.BuildException(
                        string.Format(CultureInfo.InvariantCulture, "Found the element {0}; expected the xCal root element, icalendar in the namespace {1}.",
                            localName, XcalNames.Namespace), lineNumber, linePosition, null);

                _isRootStarted = true;
                frame = new Frame(FrameKind.ICalendar, localName);
            }
            else
            {
                frame = StartChild(_frames[_frames.Count - 1], localName, namespaceUri, lineNumber, linePosition);
            }

            _frames.Add(frame);
            return isEmpty ? EndElement(lineNumber, linePosition) : null;
        }

        /// <summary>
        /// Adds the end of the element most recently started.
        /// </summary>
        /// <returns>The calendar, if the element ends one; otherwise <c>null</c>.  The calendar has not been checked for conformance.</returns>
        /// <exception cref="IcalFileException">The element is not valid.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal IcalCalendar? EndElement(long lineNumber, long linePosition)
#else
        internal IcalCalendar EndElement(long lineNumber, long linePosition)
#endif
        {
            var frame = _frames[_frames.Count - 1];
            _frames.RemoveAt(_frames.Count - 1);
            switch (frame.Kind)
            {
                case FrameKind.ICalendar:
                    IsRootEnded = true;
                    return null;
                case FrameKind.Component:
                    return AddLine(new IcalContentLine(IcalCalendarBuilder.EndName, frame.ComponentName), lineNumber, linePosition);
                case FrameKind.Property:
                    // Only the property element itself, a child of properties, becomes a line.
                    if (_frames[_frames.Count - 1].Kind != FrameKind.Properties) return null;

                    var element = frame.Element;
                    PropertiesRead++;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    return AddLine(XcalConverter.ToContentLine(element!), element!.LineNumber, element.LinePosition);
#else
                    return AddLine(XcalConverter.ToContentLine(element), element.LineNumber, element.LinePosition);
#endif
                default:
                    return null;
            }
        }

        /// <summary>
        /// Adds text, CDATA or white space.
        /// </summary>
        /// <exception cref="IcalFileException">Text is not allowed where it is.</exception>
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
                        if (!IcalValueFormatting.IsXmlWhitespace(character))
                            throw XcalConverter.BuildException(
                                string.Format(CultureInfo.InvariantCulture, "Found text in the {0} element, which can hold only elements.", frame.ElementName),
                                lineNumber, linePosition, null);
                    }

                    return;
            }
        }

        /// <summary>
        /// Checks that the data has ended where it may.
        /// </summary>
        /// <exception cref="IcalFileException">The data held no xCal root element, or no VCALENDAR.</exception>
        internal void Complete()
        {
            if (!_isRootStarted)
                throw new IcalFileException(
                    string.Format(CultureInfo.InvariantCulture, "The data holds no xCal root element, icalendar in the namespace {0}.", XcalNames.Namespace));

            _builder.Complete();
        }

        private Frame StartChild(Frame parent, string localName, string namespaceUri, long lineNumber, long linePosition)
        {
            // ref. RFC 6321§4.1: elements in another namespace are extensions.  Those not converted to the XML property are ignored.
            if (parent.Kind == FrameKind.Skipped || namespaceUri != XcalNames.Namespace) return new Frame(FrameKind.Skipped, localName);

            switch (parent.Kind)
            {
                case FrameKind.ICalendar:
                    if (localName != "vcalendar")
                        throw BuildUnexpectedElementException(localName, parent, "vcalendar", lineNumber, linePosition);

                    return StartComponent(localName, lineNumber, linePosition);
                case FrameKind.Component:
                    // ref. RFC 6321§3.2: a component holds a properties element and a components element.
                    if (localName == XcalNames.Properties && !parent.HasProperties)
                    {
                        parent.HasProperties = true;
                        return new Frame(FrameKind.Properties, localName);
                    }

                    if (localName == XcalNames.Components && !parent.HasComponents)
                    {
                        parent.HasComponents = true;
                        return new Frame(FrameKind.Components, localName);
                    }

                    throw BuildUnexpectedElementException(localName, parent, "one properties element and one components element", lineNumber,
                        linePosition);
                case FrameKind.Components:
                    return StartComponent(localName, lineNumber, linePosition);
                case FrameKind.Properties:
                    return new Frame(FrameKind.Property, new XcalElement(localName, lineNumber, linePosition));
                default:
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    var child = parent.Element!.Add(new XcalElement(localName, lineNumber, linePosition));
#else
                    var child = parent.Element.Add(new XcalElement(localName, lineNumber, linePosition));
#endif
                    return new Frame(FrameKind.Property, child);
            }
        }

        private Frame StartComponent(string localName, long lineNumber, long linePosition)
        {
            // ref. RFC 6321§4: names are converted to upper case.
            var name = XcalConverter.ToIcalName(new XcalElement(localName, lineNumber, linePosition));
            AddLine(new IcalContentLine(IcalCalendarBuilder.BeginName, name), lineNumber, linePosition);
            return new Frame(FrameKind.Component, localName, name);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? AddLine(IcalContentLine line, long lineNumber, long linePosition)
#else
        private IcalCalendar AddLine(IcalContentLine line, long lineNumber, long linePosition)
#endif
        {
            if (line.LineNumber == 0)
            {
                line = IcalContentLine.FromConversion(line.Name, new IcalContentLineParameter[0], line.Value, lineNumber);
            }

            try
            {
                return _builder.Add(line);
            }
            catch (IcalFileException exception)
            {
                // The builder gives the line, but has no position on it, and no physical line.
                var data = (IDictionary)exception.Data;
                data.Remove(IcalFileException.DataItemName.PhysicalLineNumber);
                XcalConverter.AddLocation(exception, lineNumber, linePosition);
                throw;
            }
        }

        private static IcalFileException BuildUnexpectedElementException(string localName, Frame parent, string expected, long lineNumber,
            long linePosition)
        {
            return XcalConverter.BuildException(
                string.Format(CultureInfo.InvariantCulture, "Found the element {0} in the {1} element; expected {2}.", localName, parent.ElementName,
                    expected), lineNumber, linePosition, null);
        }

        /// <summary>
        /// An element being read.
        /// </summary>
        private sealed class Frame
        {
            internal Frame(FrameKind kind, string elementName) : this(kind, elementName, string.Empty)
            {
            }

            internal Frame(FrameKind kind, string elementName, string componentName)
            {
                Kind = kind;
                ElementName = elementName;
                ComponentName = componentName;
            }

            internal Frame(FrameKind kind, XcalElement element) : this(kind, element.Name)
            {
                Element = element;
            }

            /// <summary>
            /// Gets the name of the component, in upper case, for a component element.
            /// </summary>
            internal string ComponentName { get; }

            /// <summary>
            /// Gets the element being built, for a property element or an element within one.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal XcalElement? Element { get; }
#else
            internal XcalElement Element { get; }
#endif

            internal string ElementName { get; }

            internal bool HasComponents { get; set; }

            internal bool HasProperties { get; set; }

            internal FrameKind Kind { get; }
        }
    }
}
