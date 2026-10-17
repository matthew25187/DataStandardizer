using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Builds calendars from content lines, checking that <c>BEGIN</c> and <c>END</c> lines are matched and nested.  Does no I/O, so that the
    /// synchronous and asynchronous readers share it.
    /// </summary>
    internal sealed class IcalCalendarBuilder
    {
        /// <summary>
        /// The name of the content line that starts a component (RFC 5545 §3.4 and §3.6).
        /// </summary>
        internal const string BeginName = "BEGIN";

        /// <summary>
        /// The name of the content line that ends a component.
        /// </summary>
        internal const string EndName = "END";

        /// <summary>
        /// The components being built, outermost first.
        /// </summary>
        private readonly List<Frame> _frames = new List<Frame>();

        /// <summary>
        /// Gets the number of VCALENDAR objects completed.
        /// </summary>
        internal long CalendarsRead { get; private set; }

        /// <summary>
        /// Gets the number of components completed, not counting the VCALENDAR objects.
        /// </summary>
        internal long ComponentsRead { get; private set; }

        /// <summary>
        /// Adds a line to the calendar being built.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns>The calendar, if the line ends it; otherwise <c>null</c>.  The calendar has not been checked for conformance.</returns>
        /// <exception cref="IcalFileException">The line is not where it may be: a property outside a calendar, an <c>END</c> that does not match
        /// the open <c>BEGIN</c>, or a <c>VERSION</c> that does not include 2.0.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal IcalCalendar? Add(IcalContentLine line)
#else
        internal IcalCalendar Add(IcalContentLine line)
#endif
        {
            if (IsName(line.Name, BeginName))
            {
                Begin(line);
                return null;
            }

            if (IsName(line.Name, EndName)) return End(line);

            if (_frames.Count == 0)
                throw BuildException(
                    string.Format(CultureInfo.InvariantCulture, "Found a {0} property outside a VCALENDAR; expected BEGIN:VCALENDAR.",
                        line.Name.ToUpperInvariant()), line, null);

            var frame = _frames[_frames.Count - 1];
            frame.Component.Properties.Add(line);

            // ref. RFC 5545§3.7.4: reject another version as soon as it is found, since the rest of the data may not follow RFC 5545 at all.
            if (_frames.Count == 1 && IsName(line.Name, IcalPropertyNames.Version) && !IcalConformance.IsSupportedVersion(line.Value))
            {
                var exception = BuildException(IcalConformance.BuildUnsupportedVersionMessage(line.Value), line, frame.Path);
                exception.Data.Add(IcalFileException.DataItemName.DiagnosticKind, IcalDiagnosticKind.UnsupportedVersion);
                throw exception;
            }

            return null;
        }

        /// <summary>
        /// Checks that the data has ended where it may.
        /// </summary>
        /// <exception cref="IcalFileException">A component has not been ended, or the data held no VCALENDAR.</exception>
        internal void Complete()
        {
            if (_frames.Count != 0)
            {
                var frame = _frames[_frames.Count - 1];
                var name = frame.Component.Name.ToUpperInvariant();
                var exception = IcalContentLineScanner.BuildException(
                    string.Format(CultureInfo.InvariantCulture, "The data ended before END:{0}; the BEGIN:{0} on line {1} is not closed.", name,
                        frame.Component.LineNumber),
                    frame.Component.LineNumber, frame.PhysicalLineNumber, BeginName);
                exception.Data.Add(IcalFileException.DataItemName.ComponentPath, frame.Path);
                throw exception;
            }

            // ref. RFC 5545§3.4: icalstream = 1*icalobject.
            if (CalendarsRead == 0)
                throw new IcalFileException("The data holds no VCALENDAR; RFC 5545 requires at least one.");
        }

        private void Begin(IcalContentLine line)
        {
            var name = line.Value;
            if (line.ParameterArray.Length != 0)
                throw BuildException("A BEGIN line cannot have parameters.", line, CurrentPath);
            if (!IcalComponent.IsValidName(name))
                throw BuildException("A BEGIN line must name a component: one or more letters, digits and hyphens.", line, CurrentPath);

            IcalComponent component;
            string path;
            if (_frames.Count == 0)
            {
                if (!IsName(name, IcalComponentNames.Calendar))
                    throw BuildException(
                        string.Format(CultureInfo.InvariantCulture, "Found BEGIN:{0} outside a VCALENDAR; expected BEGIN:VCALENDAR.", name.ToUpperInvariant()),
                        line, null);

                component = new IcalCalendar(true);
                path = component.Name;
            }
            else
            {
                var parent = _frames[_frames.Count - 1];
                component = CreateComponent(name, parent.Component.Name);
                parent.Component.Components.Add(component);
                path = IcalConformance.GetChildPath(parent.Path, component.Name, parent.NextIndex(component.Name));
            }

            component.LineNumber = line.LineNumber;
            _frames.Add(new Frame(component, path, line.PhysicalLineNumber));
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalCalendar? End(IcalContentLine line)
#else
        private IcalCalendar End(IcalContentLine line)
#endif
        {
            if (line.ParameterArray.Length != 0)
                throw BuildException("An END line cannot have parameters.", line, CurrentPath);
            if (_frames.Count == 0)
                throw BuildException(
                    string.Format(CultureInfo.InvariantCulture, "Found END:{0} with no BEGIN:{0} before it.", line.Value.ToUpperInvariant()), line, null);

            var frame = _frames[_frames.Count - 1];
            var component = frame.Component;
            if (!IsName(line.Value, component.Name))
                throw BuildException(
                    string.Format(CultureInfo.InvariantCulture, "Found END:{0}, but the BEGIN:{1} on line {2} is not closed; expected END:{1}.",
                        line.Value.ToUpperInvariant(), component.Name.ToUpperInvariant(), component.LineNumber),
                    line, frame.Path);

            _frames.RemoveAt(_frames.Count - 1);
            if (_frames.Count != 0)
            {
                ComponentsRead++;
                return null;
            }

            CalendarsRead++;
            return (IcalCalendar)component;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? CurrentPath => _frames.Count == 0 ? null : _frames[_frames.Count - 1].Path;
#else
        private string CurrentPath => _frames.Count == 0 ? null : _frames[_frames.Count - 1].Path;
#endif

        /// <summary>
        /// Creates the typed component for a name, or a generic one for a name RFC 5545 does not define in that place.
        /// </summary>
        private static IcalComponent CreateComponent(string name, string parentName)
        {
            switch (name.ToUpperInvariant())
            {
                case IcalComponentNames.Event:
                    return new IcalEvent();
                case IcalComponentNames.Todo:
                    return new IcalTodo();
                case IcalComponentNames.Journal:
                    return new IcalJournal();
                case IcalComponentNames.FreeBusy:
                    return new IcalFreeBusy();
                case IcalComponentNames.TimeZone:
                    return new IcalTimeZone();
                case IcalComponentNames.Alarm:
                    return new IcalAlarm();
                case IcalComponentNames.Standard:
                    return IsName(parentName, IcalComponentNames.TimeZone)
                        ? new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard)
                        : new IcalComponent(name);
                case IcalComponentNames.Daylight:
                    return IsName(parentName, IcalComponentNames.TimeZone)
                        ? new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Daylight)
                        : new IcalComponent(name);
                default:
                    return new IcalComponent(name);
            }
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static IcalFileException BuildException(string message, IcalContentLine line, string? path)
#else
        private static IcalFileException BuildException(string message, IcalContentLine line, string path)
#endif
        {
            var exception = IcalContentLineScanner.BuildException(message, line.LineNumber, line.PhysicalLineNumber, line.Name);
            if (path != null)
            {
                exception.Data.Add(IcalFileException.DataItemName.ComponentPath, path);
            }

            return exception;
        }

        private static bool IsName(string name, string expected)
        {
            return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// A component being built.
        /// </summary>
        private sealed class Frame
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private Dictionary<string, int>? _indexes;
#else
            private Dictionary<string, int> _indexes;
#endif

            internal Frame(IcalComponent component, string path, long physicalLineNumber)
            {
                Component = component;
                Path = path;
                PhysicalLineNumber = physicalLineNumber;
            }

            internal IcalComponent Component { get; }

            internal string Path { get; }

            /// <summary>
            /// Gets the physical line of the component's <c>BEGIN</c>.
            /// </summary>
            internal long PhysicalLineNumber { get; }

            /// <summary>
            /// Gets the 1-based index of the next nested component with a name.
            /// </summary>
            internal int NextIndex(string name)
            {
                if (_indexes is null)
                {
                    _indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                }

                int index;
                _indexes.TryGetValue(name, out index);
                _indexes[name] = ++index;
                return index;
            }
        }
    }
}
