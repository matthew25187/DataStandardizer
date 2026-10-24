using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Checks components against the rules of RFC 5545 and RFC 7986 that <see cref="IcalReader"/> and <see cref="IcalWriter"/> enforce.
    /// </summary>
    internal static class IcalConformance
    {
        /// <summary>
        /// The only iCalendar version RFC 5545 defines.
        /// </summary>
        internal const string SupportedVersion = "2.0";

        private const decimal SupportedVersionNumber = 2.0m;

        /// <summary>
        /// The names of the components RFC 5545 defines.  A component with one of these names may be nested only where its parent's rule allows.
        /// </summary>
        private static readonly string[] KnownComponentNames =
        {
            IcalComponentNames.Calendar, IcalComponentNames.Event, IcalComponentNames.Todo, IcalComponentNames.Journal,
            IcalComponentNames.FreeBusy, IcalComponentNames.TimeZone, IcalComponentNames.Standard, IcalComponentNames.Daylight,
            IcalComponentNames.Alarm
        };

        /// <summary>
        /// The properties whose value is a comma-separated list of values of their value type, other than TEXT lists.
        /// </summary>
        internal static readonly string[] ListProperties =
        {
            IcalPropertyNames.ExceptionDates, IcalPropertyNames.RecurrenceDates, IcalPropertyNames.FreeBusy
        };

        /// <summary>
        /// The properties RFC 7986 §5 gives no default value type, so that they need a <c>VALUE</c> parameter.
        /// </summary>
        internal static readonly string[] ValueParameterRequiredProperties =
        {
            IcalPropertyNames.RefreshInterval, IcalPropertyNames.Source, IcalPropertyNames.Image, IcalPropertyNames.Conference
        };

        /// <summary>
        /// The properties whose DATE-TIME value MUST be in UTC.
        /// </summary>
        private static readonly string[] UtcProperties =
        {
            IcalPropertyNames.Completed, IcalPropertyNames.Created, IcalPropertyNames.DtStamp, IcalPropertyNames.LastModified,
            IcalPropertyNames.Trigger
        };

        private static readonly Dictionary<string, ComponentRule> Rules = new Dictionary<string, ComponentRule>(StringComparer.OrdinalIgnoreCase)
        {
            // ref. RFC 5545§3.6 and RFC 7986§5.
            [IcalComponentNames.Calendar] = new ComponentRule(
                new[] { IcalPropertyNames.ProductId, IcalPropertyNames.Version },
                new[]
                {
                    IcalPropertyNames.CalendarScale, IcalPropertyNames.Method, IcalPropertyNames.Uid, IcalPropertyNames.LastModified,
                    IcalPropertyNames.Url, IcalPropertyNames.RefreshInterval, IcalPropertyNames.Source, IcalPropertyNames.Color
                },
                new string[0][],
                new[]
                {
                    IcalComponentNames.Event, IcalComponentNames.Todo, IcalComponentNames.Journal, IcalComponentNames.FreeBusy,
                    IcalComponentNames.TimeZone
                }),

            // ref. RFC 5545§3.6.1 and RFC 7986§5.9.
            [IcalComponentNames.Event] = new ComponentRule(
                new[] { IcalPropertyNames.DtStamp, IcalPropertyNames.Uid },
                new[]
                {
                    IcalPropertyNames.Classification, IcalPropertyNames.Created, IcalPropertyNames.Description, IcalPropertyNames.DtStart,
                    IcalPropertyNames.Geo, IcalPropertyNames.LastModified, IcalPropertyNames.Location, IcalPropertyNames.Organizer,
                    IcalPropertyNames.Priority, IcalPropertyNames.Sequence, IcalPropertyNames.Status, IcalPropertyNames.Summary,
                    IcalPropertyNames.Transparency, IcalPropertyNames.Url, IcalPropertyNames.RecurrenceId, IcalPropertyNames.DtEnd,
                    IcalPropertyNames.Duration, IcalPropertyNames.Color
                },
                new[] { new[] { IcalPropertyNames.DtEnd, IcalPropertyNames.Duration } },
                new[] { IcalComponentNames.Alarm }),

            // ref. RFC 5545§3.6.2 and RFC 7986§5.9.
            [IcalComponentNames.Todo] = new ComponentRule(
                new[] { IcalPropertyNames.DtStamp, IcalPropertyNames.Uid },
                new[]
                {
                    IcalPropertyNames.Classification, IcalPropertyNames.Completed, IcalPropertyNames.Created, IcalPropertyNames.Description,
                    IcalPropertyNames.DtStart, IcalPropertyNames.Geo, IcalPropertyNames.LastModified, IcalPropertyNames.Location,
                    IcalPropertyNames.Organizer, IcalPropertyNames.PercentComplete, IcalPropertyNames.Priority, IcalPropertyNames.RecurrenceId,
                    IcalPropertyNames.Sequence, IcalPropertyNames.Status, IcalPropertyNames.Summary, IcalPropertyNames.Url, IcalPropertyNames.Due,
                    IcalPropertyNames.Duration, IcalPropertyNames.Color
                },
                new[] { new[] { IcalPropertyNames.Due, IcalPropertyNames.Duration } },
                new[] { IcalComponentNames.Alarm }),

            // ref. RFC 5545§3.6.3 and RFC 7986§5.9.
            [IcalComponentNames.Journal] = new ComponentRule(
                new[] { IcalPropertyNames.DtStamp, IcalPropertyNames.Uid },
                new[]
                {
                    IcalPropertyNames.Classification, IcalPropertyNames.Created, IcalPropertyNames.DtStart, IcalPropertyNames.LastModified,
                    IcalPropertyNames.Organizer, IcalPropertyNames.RecurrenceId, IcalPropertyNames.Sequence, IcalPropertyNames.Status,
                    IcalPropertyNames.Summary, IcalPropertyNames.Url, IcalPropertyNames.Color
                },
                new string[0][],
                new string[0]),

            // ref. RFC 5545§3.6.4.
            [IcalComponentNames.FreeBusy] = new ComponentRule(
                new[] { IcalPropertyNames.DtStamp, IcalPropertyNames.Uid },
                new[]
                {
                    IcalPropertyNames.Contact, IcalPropertyNames.DtStart, IcalPropertyNames.DtEnd, IcalPropertyNames.Organizer, IcalPropertyNames.Url
                },
                new string[0][],
                new string[0]),

            // ref. RFC 5545§3.6.5.
            [IcalComponentNames.TimeZone] = new ComponentRule(
                new[] { IcalPropertyNames.TimeZoneId },
                new[] { IcalPropertyNames.LastModified, IcalPropertyNames.TimeZoneUrl },
                new string[0][],
                new[] { IcalComponentNames.Standard, IcalComponentNames.Daylight }),
            [IcalComponentNames.Standard] = new ComponentRule(
                new[] { IcalPropertyNames.DtStart, IcalPropertyNames.TimeZoneOffsetTo, IcalPropertyNames.TimeZoneOffsetFrom },
                new string[0],
                new string[0][],
                new string[0]),
            [IcalComponentNames.Daylight] = new ComponentRule(
                new[] { IcalPropertyNames.DtStart, IcalPropertyNames.TimeZoneOffsetTo, IcalPropertyNames.TimeZoneOffsetFrom },
                new string[0],
                new string[0][],
                new string[0]),

            // ref. RFC 5545§3.6.6.
            [IcalComponentNames.Alarm] = new ComponentRule(
                new[] { IcalPropertyNames.Action, IcalPropertyNames.Trigger },
                new[] { IcalPropertyNames.Duration, IcalPropertyNames.Repeat, IcalPropertyNames.Description, IcalPropertyNames.Summary },
                new string[0][],
                new string[0])
        };

        /// <summary>
        /// Checks a component and the components nested in it.
        /// </summary>
        /// <param name="root">The component.</param>
        /// <param name="diagnostics">Receives the problems found, in the order of the lines they were found on.</param>
        internal static void Validate(IcalComponent root, List<IcalDiagnostic> diagnostics)
        {
            Validate(root, false, diagnostics);
        }

        /// <summary>
        /// Checks a component and the components nested in it.
        /// </summary>
        /// <param name="root">The component.</param>
        /// <param name="isVersionWritten"><c>true</c> if a writer supplies <c>VERSION:2.0</c> itself, so that a missing <c>VERSION</c> is not a
        /// problem.</param>
        /// <param name="diagnostics">Receives the problems found, in the order of the lines they were found on.</param>
        internal static void Validate(IcalComponent root, bool isVersionWritten, List<IcalDiagnostic> diagnostics)
        {
            var found = new List<IcalDiagnostic>();
            var context = new Context(found);
            if (IsName(root.Name, IcalComponentNames.Calendar))
            {
                context.IsCalendar = true;
                context.HasMethod = root.GetProperty(IcalPropertyNames.Method) != null;
                context.IsVersionWritten = isVersionWritten;
                foreach (var timeZone in root.Components)
                {
                    if (!IsName(timeZone.Name, IcalComponentNames.TimeZone)) continue;

                    foreach (var timeZoneId in timeZone.GetProperties(IcalPropertyNames.TimeZoneId))
                    {
                        context.TimeZoneIds.Add(GetTextOrRaw(timeZoneId));
                    }
                }
            }

            ValidateComponent(root, root.Name, context);

            // A stable sort, so problems on the same line, and in a model built in code, stay in the order they were found.
            diagnostics.AddRange(found.OrderBy(diagnostic => diagnostic.LineNumber == 0 ? long.MaxValue : diagnostic.LineNumber));
        }

        /// <summary>
        /// Indicates whether an option accepts a kind of problem.
        /// </summary>
        internal static bool IsAccepted(IcalDiagnosticKind kind, IcalOptions options)
        {
            switch (kind)
            {
                case IcalDiagnosticKind.MissingRequiredProperty:
                    return options.AllowMissingRequiredProperties;
                case IcalDiagnosticKind.CardinalityViolation:
                    return options.AllowCardinalityViolations;
                case IcalDiagnosticKind.UndefinedTimeZone:
                    return options.AllowUndefinedTimeZones;
                case IcalDiagnosticKind.InvalidValue:
                    return options.AllowInvalidValues;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Builds the exception for a problem a reader or writer does not accept.
        /// </summary>
        internal static IcalFileException BuildException(IcalDiagnostic diagnostic)
        {
            var exception = new IcalFileException(diagnostic.Message);
            exception.Data.Add(IcalFileException.DataItemName.ComponentPath, diagnostic.ComponentPath);
            exception.Data.Add(IcalFileException.DataItemName.DiagnosticKind, diagnostic.Kind);
            if (diagnostic.LineNumber != 0)
            {
                exception.Data.Add(IcalFileException.DataItemName.LineNumber, diagnostic.LineNumber);
            }

            if (diagnostic.PropertyName != null)
            {
                exception.Data.Add(IcalFileException.DataItemName.PropertyName, diagnostic.PropertyName);
            }

            return exception;
        }

        /// <summary>
        /// Gets the path of a nested component.
        /// </summary>
        /// <param name="parentPath">The path of the parent.</param>
        /// <param name="name">The name of the component.</param>
        /// <param name="index">The 1-based index of the component among the components of the same name in its parent.</param>
        internal static string GetChildPath(string parentPath, string name, int index)
        {
            return parentPath + "/" + name + "[" + index.ToString(CultureInfo.InvariantCulture) + "]";
        }

        /// <summary>
        /// Indicates whether a <c>VERSION</c> value (RFC 5545 §3.7.4) is or includes iCalendar 2.0: <c>maxver</c> or <c>minver;maxver</c>.
        /// </summary>
        internal static bool IsSupportedVersion(string value)
        {
            var parts = value.Split(';');
            decimal minimum, maximum;
            switch (parts.Length)
            {
                case 1:
                    return TryParseVersion(parts[0], out maximum) && maximum == SupportedVersionNumber;
                case 2:
                    return TryParseVersion(parts[0], out minimum) && TryParseVersion(parts[1], out maximum) && minimum <= SupportedVersionNumber &&
                           SupportedVersionNumber <= maximum;
                default:
                    return false;
            }
        }

        internal static string BuildUnsupportedVersionMessage(string value)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "The calendar has VERSION:{0}, which does not include iCalendar 2.0, the only version RFC 5545 defines.  vCalendar 1.0 " +
                "(VERSION:1.0) is not supported.", value);
        }

        private static bool TryParseVersion(string text, out decimal version)
        {
            // ref. RFC 5545§3.7.4: the versions are text, but the only ones registered are numbers such as 2.0.
            foreach (var character in text)
            {
                if (!(character >= '0' && character <= '9' || character == '.'))
                {
                    version = 0;
                    return false;
                }
            }

            return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out version);
        }

        private static void ValidateComponent(IcalComponent component, string path, Context context)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            ComponentRule? rule;
#else
            ComponentRule rule;
#endif
            if (Rules.TryGetValue(component.Name, out rule))
            {
                ValidateCardinality(component, path, rule, context);
                ValidateChildren(component, path, rule, context);
            }

            ValidateComponentRules(component, path, context);

            foreach (var property in component.Properties)
            {
                ValidateValue(property, path, context);
                if (context.IsCalendar)
                {
                    ValidateTimeZoneReference(property, path, context);
                }
            }

            var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in component.Components)
            {
                int index;
                indexes.TryGetValue(child.Name, out index);
                indexes[child.Name] = ++index;

                ValidateComponent(child, GetChildPath(path, child.Name, index), context);
            }
        }

        private static void ValidateCardinality(IcalComponent component, string path, ComponentRule rule, Context context)
        {
            foreach (var name in rule.Required)
            {
                var properties = component.GetProperties(name);
                if (properties.Count == 0)
                {
                    if (context.IsVersionWritten && IsName(name, IcalPropertyNames.Version)) continue;

                    context.Add(IcalDiagnosticKind.MissingRequiredProperty,
                        string.Format(CultureInfo.InvariantCulture, "The {0} component has no {1} property, which it requires.", component.Name, name),
                        path, name, component.LineNumber);
                }
                else
                {
                    ValidateAtMostOnce(component, path, properties, context);
                }
            }

            foreach (var name in rule.AtMostOnce)
            {
                ValidateAtMostOnce(component, path, component.GetProperties(name), context);
            }

            foreach (var exclusive in rule.Exclusive)
            {
                var first = component.GetProperty(exclusive[0]);
                var second = component.GetProperty(exclusive[1]);
                if (first != null && second != null)
                {
                    var later = second.LineNumber >= first.LineNumber ? second : first;
                    context.Add(IcalDiagnosticKind.CardinalityViolation,
                        string.Format(CultureInfo.InvariantCulture, "The {0} component has both a {1} and a {2} property, which exclude each other.",
                            component.Name, exclusive[0], exclusive[1]),
                        path, later.Name, later.LineNumber);
                }
            }
        }

        private static void ValidateAtMostOnce(IcalComponent component, string path, IReadOnlyList<IcalContentLine> properties, Context context)
        {
            if (properties.Count < 2) return;

            context.Add(IcalDiagnosticKind.CardinalityViolation,
                string.Format(CultureInfo.InvariantCulture, "The {0} component has {1} {2} properties; it allows at most one.", component.Name,
                    properties.Count, properties[0].Name.ToUpperInvariant()),
                path, properties[1].Name, properties[1].LineNumber);
        }

        private static void ValidateChildren(IcalComponent component, string path, ComponentRule rule, Context context)
        {
            var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var child in component.Components)
            {
                int index;
                indexes.TryGetValue(child.Name, out index);
                indexes[child.Name] = ++index;

                if (IsKnownComponent(child.Name) && !rule.Children.Contains(child.Name, StringComparer.OrdinalIgnoreCase))
                {
                    context.Add(IcalDiagnosticKind.InvalidComponentNesting,
                        string.Format(CultureInfo.InvariantCulture, "A {0} component cannot be nested in a {1} component.", child.Name.ToUpperInvariant(),
                            component.Name.ToUpperInvariant()),
                        GetChildPath(path, child.Name, index), null, child.LineNumber);
                }
            }
        }

        /// <summary>
        /// Checks the rules particular to one kind of component.
        /// </summary>
        private static void ValidateComponentRules(IcalComponent component, string path, Context context)
        {
            var name = component.Name;
            if (IsName(name, IcalComponentNames.Calendar))
            {
                var version = component.GetProperty(IcalPropertyNames.Version);
                if (version != null && !IsSupportedVersion(version.Value))
                {
                    context.Add(IcalDiagnosticKind.UnsupportedVersion, BuildUnsupportedVersionMessage(version.Value), path, version.Name,
                        version.LineNumber);
                }

                // ref. RFC 5545§3.6: icalbody = calprops component, where component = 1*(...).
                if (component.Components.Count == 0)
                {
                    context.Add(IcalDiagnosticKind.MissingRequiredProperty, "The VCALENDAR component has no calendar component, which it requires.",
                        path, null, component.LineNumber);
                }
            }
            else if (IsName(name, IcalComponentNames.Event))
            {
                // ref. RFC 5545§3.6.1: DTSTART is required if the calendar has no METHOD.
                if (context.IsCalendar && !context.HasMethod && component.GetProperty(IcalPropertyNames.DtStart) is null)
                {
                    context.Add(IcalDiagnosticKind.MissingRequiredProperty,
                        "The VEVENT component has no DTSTART property, which it requires when the calendar has no METHOD property.", path,
                        IcalPropertyNames.DtStart, component.LineNumber);
                }

                ValidateEnd(component, path, IcalPropertyNames.DtEnd, context);
                ValidateRecurrence(component, path, false, context);
            }
            else if (IsName(name, IcalComponentNames.Todo))
            {
                // ref. RFC 5545§3.6.2: if DURATION appears, DTSTART must too.
                var duration = component.GetProperty(IcalPropertyNames.Duration);
                if (duration != null && component.GetProperty(IcalPropertyNames.DtStart) is null)
                {
                    context.Add(IcalDiagnosticKind.MissingRequiredProperty,
                        "The VTODO component has a DURATION property but no DTSTART property, which DURATION requires.", path,
                        IcalPropertyNames.DtStart, duration.LineNumber);
                }

                ValidateEnd(component, path, IcalPropertyNames.Due, context);
                ValidateRecurrence(component, path, false, context);
            }
            else if (IsName(name, IcalComponentNames.Journal))
            {
                ValidateRecurrence(component, path, false, context);
            }
            else if (IsName(name, IcalComponentNames.TimeZone))
            {
                // ref. RFC 5545§3.8.3.1: TZID identifies the time zone, so it can't be empty.
                foreach (var timeZoneId in component.GetProperties(IcalPropertyNames.TimeZoneId))
                {
                    if (GetTextOrRaw(timeZoneId).Length == 0)
                    {
                        AddInvalidValue(timeZoneId, path, "The TZID property of a VTIMEZONE component must not be empty.", context);
                    }
                }

                var hasObservance = false;
                foreach (var child in component.Components)
                {
                    hasObservance |= IsName(child.Name, IcalComponentNames.Standard) || IsName(child.Name, IcalComponentNames.Daylight);
                }

                if (!hasObservance)
                {
                    context.Add(IcalDiagnosticKind.MissingRequiredProperty,
                        "The VTIMEZONE component has no STANDARD or DAYLIGHT component, and requires at least one.", path, null, component.LineNumber);
                }
            }
            else if (IsName(name, IcalComponentNames.Standard) || IsName(name, IcalComponentNames.Daylight))
            {
                // ref. RFC 5545§3.6.5: DTSTART MUST be specified as a date with local time.
                var start = component.GetProperty(IcalPropertyNames.DtStart);
                IcalDateOrDateTime value;
                if (start != null && TryGetDateOrDateTime(start, out value) && (value.IsDate || value.DateTime.Kind != IcalDateTimeKind.Floating))
                {
                    AddInvalidValue(start, path, "The DTSTART property of a time zone observance must be a DATE-TIME in local time.", context);
                }

                ValidateRecurrence(component, path, true, context);
            }
            else if (IsName(name, IcalComponentNames.Alarm))
            {
                ValidateAlarm(component, path, context);
            }
        }

        /// <summary>
        /// Checks that the end of an event, or the due date of a to-do, has the value type of its start, and that a duration from a DATE is in
        /// days or weeks.
        /// </summary>
        private static void ValidateEnd(IcalComponent component, string path, string endName, Context context)
        {
            var start = component.GetProperty(IcalPropertyNames.DtStart);
            IcalDateOrDateTime startValue;
            if (start is null || !TryGetDateOrDateTime(start, out startValue)) return;

            // ref. RFC 5545§3.8.2.2 and §3.8.2.3: the value type MUST be the same as DTSTART.
            var end = component.GetProperty(endName);
            IcalDateOrDateTime endValue;
            if (end != null && TryGetDateOrDateTime(end, out endValue) && endValue.IsDate != startValue.IsDate)
            {
                AddInvalidValue(end, path,
                    string.Format(CultureInfo.InvariantCulture, "The {0} property must have the same value type as the DTSTART property, {1}.",
                        endName, IcalValueTypeRegistry.GetValueTypeName(startValue.ValueType)), context);
            }

            // ref. RFC 5545§3.6.1: when DTSTART is a DATE, the duration MUST be specified as a dur-day or dur-week value.
            var duration = component.GetProperty(IcalPropertyNames.Duration);
            IcalDuration durationValue;
            if (startValue.IsDate && duration != null && IcalDuration.TryParse(duration.Value.AsSpan(), IcalValueFormat.Text, out durationValue) &&
                durationValue.Time != TimeSpan.Zero)
            {
                AddInvalidValue(duration, path, "The DURATION of a component whose DTSTART is a DATE must be in days or weeks.", context);
            }
        }

        /// <summary>
        /// Checks the recurrence rules (RFC 5545 §3.3.10) and recurrence identifier of a component against its <c>DTSTART</c>.
        /// </summary>
        private static void ValidateRecurrence(IcalComponent component, string path, bool isObservance, Context context)
        {
            var start = component.GetProperty(IcalPropertyNames.DtStart);
            IcalDateOrDateTime startValue;
            if (start is null || !TryGetDateOrDateTime(start, out startValue)) return;

            foreach (var property in component.GetProperties(IcalPropertyNames.RecurrenceRule))
            {
                IcalRecurrenceRule rule;
                if (!IcalRecurrenceRule.TryParse(property.Value.AsSpan(), IcalValueFormat.Text, out rule)) continue;

                var problem = string.Empty;
                if (startValue.IsDate)
                {
                    if (rule.UntilDateTime.HasValue)
                    {
                        problem = "The UNTIL of a recurrence rule must be a DATE when DTSTART is a DATE.";
                    }
                    else if (rule.GetBySecond().Length != 0 || rule.GetByMinute().Length != 0 || rule.GetByHour().Length != 0)
                    {
                        problem = "A recurrence rule cannot have BYSECOND, BYMINUTE or BYHOUR when DTSTART is a DATE.";
                    }
                }
                else if (rule.UntilDate.HasValue)
                {
                    problem = "The UNTIL of a recurrence rule must be a DATE-TIME when DTSTART is a DATE-TIME.";
                }
                else if (rule.UntilDateTime.HasValue)
                {
                    var untilKind = rule.UntilDateTime.Value.Kind;
                    if (isObservance)
                    {
                        // ref. RFC 5545§3.3.10: in a VTIMEZONE, UNTIL MUST always be in UTC.
                        if (untilKind != IcalDateTimeKind.Utc)
                        {
                            problem = "The UNTIL of a recurrence rule in a time zone observance must be in UTC.";
                        }
                    }
                    else if (startValue.DateTime.Kind == IcalDateTimeKind.Floating && untilKind != IcalDateTimeKind.Floating)
                    {
                        problem = "The UNTIL of a recurrence rule must be in local time when DTSTART is in local time.";
                    }
                    else if (startValue.DateTime.Kind != IcalDateTimeKind.Floating && untilKind != IcalDateTimeKind.Utc)
                    {
                        problem = "The UNTIL of a recurrence rule must be in UTC when DTSTART is in UTC or has a time zone.";
                    }
                }

                if (problem.Length != 0)
                {
                    AddInvalidValue(property, path, problem, context);
                }
            }

            // ref. RFC 5545§3.8.4.4: the value type of RECURRENCE-ID MUST be the same as DTSTART.
            var recurrenceId = component.GetProperty(IcalPropertyNames.RecurrenceId);
            IcalDateOrDateTime recurrenceIdValue;
            if (!isObservance && recurrenceId != null && TryGetDateOrDateTime(recurrenceId, out recurrenceIdValue) &&
                recurrenceIdValue.IsDate != startValue.IsDate)
            {
                AddInvalidValue(recurrenceId, path,
                    string.Format(CultureInfo.InvariantCulture, "The RECURRENCE-ID property must have the same value type as the DTSTART property, {0}.",
                        IcalValueTypeRegistry.GetValueTypeName(startValue.ValueType)), context);
            }
        }

        private static void ValidateAlarm(IcalComponent alarm, string path, Context context)
        {
            // ref. RFC 5545§3.6.6: DURATION and REPEAT are both optional, but if one occurs, so MUST the other.
            var duration = alarm.GetProperty(IcalPropertyNames.Duration);
            var repeat = alarm.GetProperty(IcalPropertyNames.Repeat);
            if ((duration is null) != (repeat is null))
            {
                var lineNumber = duration != null ? duration.LineNumber : repeat != null ? repeat.LineNumber : 0;
                context.Add(IcalDiagnosticKind.MissingRequiredProperty,
                    string.Format(CultureInfo.InvariantCulture, "The VALARM component has a {0} property but no {1} property; they must appear together.",
                        duration is null ? IcalPropertyNames.Repeat : IcalPropertyNames.Duration,
                        duration is null ? IcalPropertyNames.Duration : IcalPropertyNames.Repeat),
                    path, duration is null ? IcalPropertyNames.Duration : IcalPropertyNames.Repeat, lineNumber);
            }

            var action = alarm.GetProperty(IcalPropertyNames.Action);
            if (action is null) return;

            if (IsName(action.Value, "AUDIO"))
            {
                ValidateAtMostOnce(alarm, path, alarm.GetProperties(IcalPropertyNames.Attachment), context);
            }
            else if (IsName(action.Value, "DISPLAY"))
            {
                RequireForAction(alarm, path, action, IcalPropertyNames.Description, context);
            }
            else if (IsName(action.Value, "EMAIL"))
            {
                RequireForAction(alarm, path, action, IcalPropertyNames.Description, context);
                RequireForAction(alarm, path, action, IcalPropertyNames.Summary, context);
                RequireForAction(alarm, path, action, IcalPropertyNames.Attendee, context);
            }
        }

        private static void RequireForAction(IcalComponent alarm, string path, IcalContentLine action, string name, Context context)
        {
            if (alarm.GetProperty(name) != null) return;

            context.Add(IcalDiagnosticKind.MissingRequiredProperty,
                string.Format(CultureInfo.InvariantCulture, "A VALARM component with ACTION:{0} requires a {1} property.", action.Value.ToUpperInvariant(),
                    name),
                path, name, alarm.LineNumber);
        }

        /// <summary>
        /// Checks the value of a property RFC 5545 or RFC 7986 defines against its value type.
        /// </summary>
        private static void ValidateValue(IcalContentLine property, string path, Context context)
        {
            var name = property.Name;
            if (!IcalValueTypeRegistry.IsKnownProperty(name.AsSpan())) return;

            // ref. RFC 5545§3.2.20: the VALUE parameter names one of the value types the property allows.
            var valueParameter = IcalPropertyValue.GetParameterValue(property, IcalParameterNames.Value);
            IcalValueType valueType;
            if (valueParameter != null)
            {
                if (!IcalValueTypeRegistry.TryParseValueTypeName(valueParameter.AsSpan(), out valueType) ||
                    !IcalValueTypeRegistry.IsAllowedValueType(name.AsSpan(), valueType))
                {
                    AddInvalidValue(property, path,
                        string.Format(CultureInfo.InvariantCulture, "The {0} property does not allow VALUE={1}.", name.ToUpperInvariant(), valueParameter),
                        context);
                    return;
                }
            }
            else
            {
                if (ValueParameterRequiredProperties.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    AddInvalidValue(property, path,
                        string.Format(CultureInfo.InvariantCulture, "The {0} property requires a VALUE parameter.", name.ToUpperInvariant()), context);
                    return;
                }

                valueType = IcalValueTypeRegistry.GetDefaultValueType(name.AsSpan());
            }

            // ref. RFC 5545§3.3.1: inline binary content MUST also specify ENCODING=BASE64.
            if (valueType == IcalValueType.Binary &&
                !string.Equals(IcalPropertyValue.GetParameterValue(property, IcalParameterNames.Encoding), "BASE64", StringComparison.OrdinalIgnoreCase))
            {
                AddInvalidValue(property, path,
                    string.Format(CultureInfo.InvariantCulture, "The BINARY value of the {0} property requires an ENCODING=BASE64 parameter.",
                        name.ToUpperInvariant()), context);
                return;
            }

            if (!IsValidValue(property, valueType))
            {
                AddInvalidValue(property, path,
                    string.Format(CultureInfo.InvariantCulture, "The value of the {0} property is not a valid {1}.", name.ToUpperInvariant(),
                        IsName(name, IcalPropertyNames.Geo) ? "pair of FLOAT values" : IcalValueTypeRegistry.GetValueTypeName(valueType)),
                    context);
                return;
            }

            ValidateValueRules(property, path, valueType, context);
        }

        private static bool IsValidValue(IcalContentLine property, IcalValueType valueType)
        {
            var value = property.Value;
            if (IsName(property.Name, IcalPropertyNames.Geo))
            {
                // ref. RFC 5545§3.8.1.6: geovalue = float ";" float.
                var parts = value.Split(';');
                return parts.Length == 2 && IcalValue.TryParseFloat(parts[0].AsSpan(), IcalValueFormat.Text, out _) &&
                       IcalValue.TryParseFloat(parts[1].AsSpan(), IcalValueFormat.Text, out _);
            }

            if (ListProperties.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var item in value.Split(','))
                {
                    if (!IsValidItem(property, item, valueType)) return false;
                }

                return true;
            }

            return IsValidItem(property, value, valueType);
        }

        private static bool IsValidItem(IcalContentLine property, string value, IcalValueType valueType)
        {
            var span = value.AsSpan();
            switch (valueType)
            {
                case IcalValueType.Binary:
                    return IcalValue.TryParseBinary(span, IcalValueFormat.Text, out _);
                case IcalValueType.Boolean:
                    return IcalValue.TryParseBoolean(span, IcalValueFormat.Text, out _);
                case IcalValueType.CalAddress:
                    return IcalValue.TryParseCalAddress(span, IcalValueFormat.Text, out _);
                case IcalValueType.Date:
                case IcalValueType.DateTime:
                    return IcalPropertyValue.TryParseDateOrDateTime(property, span, out _);
                case IcalValueType.Duration:
                    return IcalDuration.TryParse(span, IcalValueFormat.Text, out _);
                case IcalValueType.Float:
                    return IcalValue.TryParseFloat(span, IcalValueFormat.Text, out _);
                case IcalValueType.Integer:
                    return IcalValue.TryParseInteger(span, IcalValueFormat.Text, out _);
                case IcalValueType.Period:
                    var timeZoneId = IcalPropertyValue.GetParameterValue(property, IcalParameterNames.TimeZoneId);
                    return timeZoneId?.Length != 0 && IcalPeriod.TryParse(span, timeZoneId, IcalValueFormat.Text, out _);
                case IcalValueType.Recur:
                    return IcalRecurrenceRule.TryParse(span, IcalValueFormat.Text, out _);
                case IcalValueType.Text:
                    return IcalEscaping.TryUnescapeText(span, new char[span.Length], out _, out _);
                case IcalValueType.Time:
                    return IcalTime.TryParse(span, IcalValueFormat.Text, out _);
                case IcalValueType.Uri:
                    return IcalValue.TryParseUri(span, IcalValueFormat.Text, out _);
                case IcalValueType.UtcOffset:
                    return IcalUtcOffset.TryParse(span, IcalValueFormat.Text, out _);
                default:
                    return true;
            }
        }

        /// <summary>
        /// Checks the rules for a valid value that RFC 5545 sets on particular properties.
        /// </summary>
        private static void ValidateValueRules(IcalContentLine property, string path, IcalValueType valueType, Context context)
        {
            var name = property.Name;
            if (valueType == IcalValueType.DateTime && UtcProperties.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                IcalDateOrDateTime value;
                if (TryGetDateOrDateTime(property, out value) && value.DateTime.Kind != IcalDateTimeKind.Utc)
                {
                    AddInvalidValue(property, path,
                        string.Format(CultureInfo.InvariantCulture, "The {0} property must be a DATE-TIME in UTC.", name.ToUpperInvariant()), context);
                }
            }
            else if (IsName(name, IcalPropertyNames.FreeBusy))
            {
                // ref. RFC 5545§3.8.2.6: the date and time values MUST be in UTC.
                foreach (var item in property.Value.Split(','))
                {
                    IcalPeriod period;
                    if (IcalPeriod.TryParse(item.AsSpan(), IcalValueFormat.Text, out period) && (period.Start.Kind != IcalDateTimeKind.Utc ||
                                                                                                 period.End.HasValue && period.End.Value.Kind != IcalDateTimeKind.Utc))
                    {
                        AddInvalidValue(property, path, "The periods of the FREEBUSY property must be in UTC.", context);
                        break;
                    }
                }
            }
            else if (IsName(name, IcalPropertyNames.Priority))
            {
                ValidateRange(property, path, 0, 9, context);
            }
            else if (IsName(name, IcalPropertyNames.PercentComplete))
            {
                ValidateRange(property, path, 0, 100, context);
            }
        }

        private static void ValidateRange(IcalContentLine property, string path, int minimum, int maximum, Context context)
        {
            int value;
            if (IcalValue.TryParseInteger(property.Value.AsSpan(), IcalValueFormat.Text, out value) && (value < minimum || value > maximum))
            {
                AddInvalidValue(property, path,
                    string.Format(CultureInfo.InvariantCulture, "The {0} property must be from {1} to {2}.", property.Name.ToUpperInvariant(), minimum,
                        maximum), context);
            }
        }

        /// <summary>
        /// Checks that a <c>TZID</c> parameter names a VTIMEZONE of the calendar (RFC 5545 §3.2.19).
        /// </summary>
        private static void ValidateTimeZoneReference(IcalContentLine property, string path, Context context)
        {
            var timeZoneId = IcalPropertyValue.GetParameterValue(property, IcalParameterNames.TimeZoneId);
            if (timeZoneId is null || context.TimeZoneIds.Contains(timeZoneId)) return;

            context.Add(IcalDiagnosticKind.UndefinedTimeZone,
                string.Format(CultureInfo.InvariantCulture, "The TZID parameter of the {0} property names the time zone '{1}', which has no VTIMEZONE in the calendar.",
                    property.Name.ToUpperInvariant(), timeZoneId),
                path, property.Name, property.LineNumber);
        }

        private static void AddInvalidValue(IcalContentLine property, string path, string message, Context context)
        {
            context.Add(IcalDiagnosticKind.InvalidValue, message, path, property.Name, property.LineNumber);
        }

        private static bool TryGetDateOrDateTime(IcalContentLine property, out IcalDateOrDateTime value)
        {
            return IcalPropertyValue.TryParseDateOrDateTime(property, property.Value.AsSpan(), out value);
        }

        /// <summary>
        /// Gets a TEXT value with its escaping removed, or as written if the escaping is not valid.
        /// </summary>
        private static string GetTextOrRaw(IcalContentLine property)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            string? text;
#else
            string text;
#endif
            return IcalValue.TryParseText(property.Value.AsSpan(), IcalValueFormat.Text, out text) ? text : property.Value;
        }

        private static bool IsKnownComponent(string name)
        {
            return KnownComponentNames.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsName(string name, string expected)
        {
            return string.Equals(name, expected, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The properties and nested components RFC 5545 allows a component.
        /// </summary>
        private sealed class ComponentRule
        {
            internal ComponentRule(string[] required, string[] atMostOnce, string[][] exclusive, string[] children)
            {
                Required = required;
                AtMostOnce = atMostOnce;
                Exclusive = exclusive;
                Children = children;
            }

            /// <summary>
            /// Gets the known components that may be nested in the component.  Other components may always be nested.
            /// </summary>
            internal string[] Children { get; }

            /// <summary>
            /// Gets the pairs of properties that must not both appear.
            /// </summary>
            internal string[][] Exclusive { get; }

            /// <summary>
            /// Gets the properties that may appear once at most.
            /// </summary>
            internal string[] AtMostOnce { get; }

            /// <summary>
            /// Gets the properties that must appear exactly once.
            /// </summary>
            internal string[] Required { get; }
        }

        /// <summary>
        /// What the checks of a component need to know about the calendar it is in, and where they report.
        /// </summary>
        private sealed class Context
        {
            private readonly List<IcalDiagnostic> _diagnostics;

            internal Context(List<IcalDiagnostic> diagnostics)
            {
                _diagnostics = diagnostics;
                TimeZoneIds = new HashSet<string>(StringComparer.Ordinal);
            }

            /// <summary>
            /// Gets or sets a flag indicating if the calendar has a <c>METHOD</c> property.
            /// </summary>
            internal bool HasMethod { get; set; }

            /// <summary>
            /// Gets or sets a flag indicating if the component checked is a calendar, so that checks that depend on the calendar can be made.
            /// </summary>
            internal bool IsCalendar { get; set; }

            /// <summary>
            /// Gets or sets a flag indicating if a writer supplies the <c>VERSION</c>.
            /// </summary>
            internal bool IsVersionWritten { get; set; }

            /// <summary>
            /// Gets the identifiers of the calendar's VTIMEZONE components.
            /// </summary>
            internal HashSet<string> TimeZoneIds { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal void Add(IcalDiagnosticKind kind, string message, string path, string? propertyName, long lineNumber)
#else
            internal void Add(IcalDiagnosticKind kind, string message, string path, string propertyName, long lineNumber)
#endif
            {
                _diagnostics.Add(new IcalDiagnostic(kind, message, path,
                    propertyName is null ? null : propertyName.ToUpperInvariant(), lineNumber));
            }
        }
    }
}
