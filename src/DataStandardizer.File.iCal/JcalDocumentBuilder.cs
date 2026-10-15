#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Builds calendars from the tokens of a jCal document (RFC 7265), one token at a time.  Does no I/O, so that the synchronous and
    /// asynchronous readers share it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The document is a component array for one calendar, <c>["vcalendar", [...], [...]]</c>, or an array of them for several (RFC 7265 §3.2).
    /// </para>
    /// <para>
    /// Each component array becomes a <c>BEGIN</c> and an <c>END</c> line, and each property array a content line, which are handed to an
    /// <see cref="IcalCalendarBuilder"/>; so the calendars are built, and their nesting and <c>VERSION</c> checked, as for <c>.ics</c> data.
    /// </para>
    /// </remarks>
    internal sealed class JcalDocumentBuilder
    {
        private enum FrameKind
        {
            /// <summary>
            /// The root array, before its first element shows whether it is a component or an array of components.
            /// </summary>
            Root,
            Stream,
            Component,
            Properties,
            Components,
            Property,
            Value
        }

        private readonly IcalCalendarBuilder _builder = new IcalCalendarBuilder();
        private readonly List<Frame> _frames = new List<Frame>();
        private bool _isRootStarted;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? _memberName;
#else
        private string _memberName;
#endif

        /// <summary>
        /// Gets the number of VCALENDAR objects completed.
        /// </summary>
        internal long CalendarsRead => _builder.CalendarsRead;

        /// <summary>
        /// Gets the number of components completed, not counting the VCALENDAR objects.
        /// </summary>
        internal long ComponentsRead => _builder.ComponentsRead;

        /// <summary>
        /// Gets the number of properties completed.
        /// </summary>
        internal long PropertiesRead { get; private set; }

        /// <summary>
        /// Adds a token.
        /// </summary>
        /// <param name="tokenType">The type of token.</param>
        /// <param name="text">The text of a string or property name, without its JSON escaping, or of a number; otherwise <c>null</c>.</param>
        /// <param name="bytePosition">The 0-based offset of the token's first byte from the start of the data.</param>
        /// <returns>The calendar, if the token ends one; otherwise <c>null</c>.  The calendar has not been checked for conformance.</returns>
        /// <exception cref="IcalFileException">The token is not allowed where it is, or ends a value that is not valid.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal IcalCalendar? Add(JsonTokenType tokenType, string? text, long bytePosition)
#else
        internal IcalCalendar Add(JsonTokenType tokenType, string text, long bytePosition)
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
                    return Start(JcalNodeKind.Array, string.Empty, bytePosition);
                case JsonTokenType.StartObject:
                    return Start(JcalNodeKind.Object, string.Empty, bytePosition);
                case JsonTokenType.String:
                    return Start(JcalNodeKind.String, text ?? string.Empty, bytePosition);
                case JsonTokenType.Number:
                    return Start(JcalNodeKind.Number, text ?? string.Empty, bytePosition);
                case JsonTokenType.True:
                    return Start(JcalNodeKind.True, string.Empty, bytePosition);
                case JsonTokenType.False:
                    return Start(JcalNodeKind.False, string.Empty, bytePosition);
                case JsonTokenType.Null:
                    return Start(JcalNodeKind.Null, string.Empty, bytePosition);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Checks that the data has ended where it may.
        /// </summary>
        /// <exception cref="IcalFileException">The data held no VCALENDAR.</exception>
        internal void Complete()
        {
            _builder.Complete();
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? Start(JcalNodeKind kind, string text, long bytePosition)
#else
        private IcalCalendar Start(JcalNodeKind kind, string text, long bytePosition)
#endif
        {
            if (_frames.Count == 0)
            {
                // ref. RFC 7265§3.2: a jCal object is a component array, and several are held in an array.  The JSON reader allows only one
                // value at the root.
                var root = new JcalNode(kind, text, null, 0, null, bytePosition);
                if (_isRootStarted || kind != JcalNodeKind.Array)
                    throw JcalConverter.BuildException(root, null,
                        string.Format(CultureInfo.InvariantCulture,
                            "The data must be a jCal component array, or an array of them, but it is {0}.", JcalConverter.DescribeKind(kind)));

                _isRootStarted = true;
                _frames.Add(new Frame(FrameKind.Root, root));
                return null;
            }

            var parent = _frames[_frames.Count - 1];
            var name = _memberName;
            _memberName = null;
            var node = new JcalNode(kind, text, parent.Node, parent.Count++, name, bytePosition);
            switch (parent.Kind)
            {
                case FrameKind.Root:
                    if (kind == JcalNodeKind.String)
                    {
                        // The root array is the component array of a single calendar.
                        parent.Kind = FrameKind.Component;
                        StartComponent(parent, node, true);
                        return null;
                    }

                    // The root array holds the component arrays of the calendars.
                    parent.Kind = FrameKind.Stream;
                    return StartInStream(node);
                case FrameKind.Stream:
                    return StartInStream(node);
                case FrameKind.Component:
                    return StartInComponent(parent, node);
                case FrameKind.Properties:
                    if (kind != JcalNodeKind.Array)
                        throw JcalConverter.BuildException(node, null,
                            string.Format(CultureInfo.InvariantCulture,
                                "Found {0} in the properties of the {1} component; expected a property array.", JcalConverter.DescribeKind(kind),
                                FindComponentName()));

                    _frames.Add(new Frame(FrameKind.Property, node));
                    return null;
                case FrameKind.Components:
                    if (kind != JcalNodeKind.Array)
                        throw JcalConverter.BuildException(node, null,
                            string.Format(CultureInfo.InvariantCulture,
                                "Found {0} in the components of the {1} component; expected a component array.", JcalConverter.DescribeKind(kind),
                                FindComponentName()));

                    _frames.Add(new Frame(FrameKind.Component, node));
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

                    if (kind == JcalNodeKind.Array || kind == JcalNodeKind.Object)
                    {
                        _frames.Add(new Frame(FrameKind.Value, node));
                    }

                    return null;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? StartInStream(JcalNode node)
#else
        private IcalCalendar StartInStream(JcalNode node)
#endif
        {
            if (node.Kind != JcalNodeKind.Array)
                throw JcalConverter.BuildException(node, null,
                    string.Format(CultureInfo.InvariantCulture, "Found {0} in the array of calendars; expected a vcalendar component array.",
                        JcalConverter.DescribeKind(node.Kind)));

            _frames.Add(new Frame(FrameKind.Component, node));
            return null;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? StartInComponent(Frame component, JcalNode node)
#else
        private IcalCalendar StartInComponent(Frame component, JcalNode node)
#endif
        {
            // ref. RFC 7265§3.3: a component is an array of three elements: its name, an array of properties and an array of components.
            switch (node.Index)
            {
                case 0 when node.Kind == JcalNodeKind.String:
                    StartComponent(component, node, _frames.Count == 1 || _frames[_frames.Count - 2].Kind == FrameKind.Stream);
                    return null;
                case 1 when node.Kind == JcalNodeKind.Array:
                    _frames.Add(new Frame(FrameKind.Properties, node));
                    return null;
                case 2 when node.Kind == JcalNodeKind.Array:
                    _frames.Add(new Frame(FrameKind.Components, node));
                    return null;
                default:
                    throw BuildComponentShapeException(component.ComponentName, node);
            }
        }

        private void StartComponent(Frame frame, JcalNode nameNode, bool isCalendar)
        {
            // ref. RFC 7265§4: names are converted to upper case.
            var name = JcalConverter.ToIcalName(nameNode, null, "component");
            if (isCalendar && name != "VCALENDAR")
                throw JcalConverter.BuildException(nameNode, null,
                    string.Format(CultureInfo.InvariantCulture, "Found the component {0} at the top of the data; expected {1}.", nameNode.Text,
                        JcalConverter.CalendarName));

            frame.ComponentName = name;
            AddLine(new IcalContentLine(IcalCalendarBuilder.BeginName, name), nameNode);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? End()
#else
        private IcalCalendar End()
#endif
        {
            var frame = _frames[_frames.Count - 1];
            _frames.RemoveAt(_frames.Count - 1);
            switch (frame.Kind)
            {
                case FrameKind.Component:
                    if (frame.Count != 3)
                        throw BuildComponentShapeException(frame.ComponentName, frame.Node);

                    return AddLine(new IcalContentLine(IcalCalendarBuilder.EndName, frame.ComponentName), frame.Node);
                case FrameKind.Property:
                    var line = JcalConverter.ToContentLine(frame.Node);
                    PropertiesRead++;
                    return AddLine(line, frame.Node);
                default:
                    return null;
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? AddLine(IcalContentLine line, JcalNode node)
#else
        private IcalCalendar AddLine(IcalContentLine line, JcalNode node)
#endif
        {
            try
            {
                return _builder.Add(line);
            }
            catch (IcalFileException exception)
            {
                // The builder has no line numbers to give, since the lines were not read from .ics data.
                var data = (IDictionary)exception.Data;
                data.Remove(IcalFileException.DataItemName.LineNumber);
                data.Remove(IcalFileException.DataItemName.PhysicalLineNumber);
                JcalConverter.AddLocation(exception, node);
                throw;
            }
        }

        private string FindComponentName()
        {
            for (var index = _frames.Count - 1; index >= 0; index--)
            {
                if (_frames[index].Kind == FrameKind.Component) return _frames[index].ComponentName;
            }

            return string.Empty;
        }

        private static IcalFileException BuildComponentShapeException(string componentName, JcalNode node)
        {
            return JcalConverter.BuildException(node, null,
                string.Format(CultureInfo.InvariantCulture,
                    "A component must be an array of three elements: its name as a string, an array of its properties and an array of its components{0}.",
                    componentName.Length == 0 ? string.Empty : "; the " + componentName + " component is not"));
        }

        /// <summary>
        /// An array or object being read.
        /// </summary>
        private sealed class Frame
        {
            internal Frame(FrameKind kind, JcalNode node)
            {
                Kind = kind;
                Node = node;
                ComponentName = string.Empty;
            }

            /// <summary>
            /// Gets or sets the name of the component, in upper case, for a component array whose name has been read.
            /// </summary>
            internal string ComponentName { get; set; }

            /// <summary>
            /// Gets or sets the number of elements or members read so far.
            /// </summary>
            internal int Count { get; set; }

            internal FrameKind Kind { get; set; }

            internal JcalNode Node { get; }
        }
    }
}
#endif
