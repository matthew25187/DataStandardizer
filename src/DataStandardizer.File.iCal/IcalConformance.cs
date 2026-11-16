using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DataStandardizer.File.ContentLine;

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

        /// <summary>
        /// The <c>minver;maxver</c> form of <see cref="SupportedVersion"/>: the only range whose ends are both registered identifiers.
        /// </summary>
        private const string SupportedVersionRange = SupportedVersion + ";" + SupportedVersion;

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
        /// The properties whose value is a comma-separated list of TEXT values (RFC 5545 §3.8.1.2 and §3.8.1.10).
        /// </summary>
        internal static readonly string[] TextListProperties ={ IcalPropertyNames.Categories, IcalPropertyNames.Resources };

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

        // ref. RFC 5545§3.8.1.11: statvalue-event, statvalue-todo and statvalue-jour, which have no iana-token or x-name.
        private static readonly string[] EventStatuses = { "TENTATIVE", "CONFIRMED", "CANCELLED" };
        private static readonly string[] TodoStatuses = { "NEEDS-ACTION", "COMPLETED", "IN-PROCESS", "CANCELLED" };
        private static readonly string[] JournalStatuses = { "DRAFT", "FINAL", "CANCELLED" };

        // ref. RFC 5545§3.8.2.7: transvalue = "OPAQUE" / "TRANSPARENT".
        private static readonly string[] TransparencyValues = { "OPAQUE", "TRANSPARENT" };

        // ref. RFC 5545§3.7.1: calvalue = "GREGORIAN".
        private static readonly string[] CalendarScaleValues = { "GREGORIAN" };

        private const long SecondsPerDay = 86400;

        /// <summary>
        /// The largest size of a UTC offset, in seconds: <c>utc-offset = time-numzone</c> (RFC 5545 §3.3.14), whose hour is at most 23, minute
        /// at most 59 and second at most 60.
        /// </summary>
        private const long MaxOffsetSeconds = 23 * 3600 + 59 * 60 + 60;

        /// <summary>
        /// The parameters RFC 5545 §3.2 and RFC 7986 §6 define with one value.  <c>FMTTYPE</c> is left out, as it has a check of its own.
        /// </summary>
        private static readonly string[] SingleValueParameters =
        {
            IcalParameterNames.AlternateRepresentation, IcalParameterNames.CommonName, IcalParameterNames.CalendarUserType,
            IcalParameterNames.Directory, IcalParameterNames.Encoding, IcalParameterNames.FreeBusyType, IcalParameterNames.Language,
            IcalParameterNames.ParticipationStatus, IcalParameterNames.Range, IcalParameterNames.Related, IcalParameterNames.RelationshipType,
            IcalParameterNames.Role, IcalParameterNames.Rsvp, IcalParameterNames.SentBy, IcalParameterNames.TimeZoneId, IcalParameterNames.Value,
            IcalParameterNames.Email, IcalParameterNames.Label
        };

        /// <summary>
        /// The parameters RFC 5545 §3.2 and RFC 7986 §6 define with a list of values.
        /// </summary>
        private static readonly string[] MultipleValueParameters =
        {
            IcalParameterNames.DelegatedFrom, IcalParameterNames.DelegatedTo, IcalParameterNames.Member, IcalParameterNames.Display,
            IcalParameterNames.Feature
        };

        /// <summary>
        /// The parameters RFC 5545 §3.2 and RFC 7986 §6 define with a grammar that has no <c>quoted-string</c>.  The others are <c>ALTREP</c>,
        /// <c>DELEGATED-FROM</c>, <c>DELEGATED-TO</c>, <c>DIR</c>, <c>MEMBER</c> and <c>SENT-BY</c>, whose grammar has <c>DQUOTE</c>, and
        /// <c>CN</c>, <c>EMAIL</c> and <c>LABEL</c>, whose value is a <c>param-value</c>.
        /// </summary>
        private static readonly string[] UnquotedParameters =
        {
            // ref. RFC 5545§3.2.3: cutypeparam = "CUTYPE" "=" ("INDIVIDUAL" / "GROUP" / "RESOURCE" / "ROOM" / "UNKNOWN" / x-name / iana-token)
            IcalParameterNames.CalendarUserType,

            // ref. RFC 5545§3.2.7: encodingparam = "ENCODING" "=" ( "8BIT" / "BASE64" )
            IcalParameterNames.Encoding,

            // ref. RFC 5545§3.2.8: fmttypeparam = "FMTTYPE" "=" type-name "/" subtype-name
            IcalParameterNames.FormatType,

            // ref. RFC 5545§3.2.9: fbtypeparam = "FBTYPE" "=" ("FREE" / "BUSY" / "BUSY-UNAVAILABLE" / "BUSY-TENTATIVE" / x-name / iana-token)
            IcalParameterNames.FreeBusyType,

            // ref. RFC 5545§3.2.10: languageparam = "LANGUAGE" "=" language
            IcalParameterNames.Language,

            // ref. RFC 5545§3.2.12: partstatparam = "PARTSTAT" "=" (partstat-event / partstat-todo / partstat-jour)
            IcalParameterNames.ParticipationStatus,

            // ref. RFC 5545§3.2.13: rangeparam = "RANGE" "=" "THISANDFUTURE"
            IcalParameterNames.Range,

            // ref. RFC 5545§3.2.14: trigrelparam = "RELATED" "=" ( "START" / "END" )
            IcalParameterNames.Related,

            // ref. RFC 5545§3.2.15: reltypeparam = "RELTYPE" "=" ("PARENT" / "CHILD" / "SIBLING" / iana-token / x-name)
            IcalParameterNames.RelationshipType,

            // ref. RFC 5545§3.2.16: roleparam = "ROLE" "=" ("CHAIR" / "REQ-PARTICIPANT" / "OPT-PARTICIPANT" / "NON-PARTICIPANT" / x-name / iana-token)
            IcalParameterNames.Role,

            // ref. RFC 5545§3.2.17: rsvpparam = "RSVP" "=" ("TRUE" / "FALSE")
            IcalParameterNames.Rsvp,

            // ref. RFC 5545§3.2.19: tzidparam = "TZID" "=" [tzidprefix] paramtext
            IcalParameterNames.TimeZoneId,

            // ref. RFC 5545§3.2.20: valuetypeparam = "VALUE" "=" valuetype
            IcalParameterNames.Value,

            // ref. RFC 7986§6.1: displayparam = "DISPLAY" "=" displayval *("," displayval)
            IcalParameterNames.Display,

            // ref. RFC 7986§6.3: featureparam = "FEATURE" "=" featuretext *("," featuretext)
            IcalParameterNames.Feature
        };

        /// <summary>
        /// The characters other than DQUOTE that <c>paramtext</c> excludes (RFC 5545 §3.1), so that a value with one must be quoted.
        /// </summary>
        private static readonly char[] ParamtextExcludedCharacters = { ':', ';', ',' };

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

                    var offsets = GetOffsetRange(timeZone);
                    foreach (var timeZoneId in timeZone.GetProperties(IcalPropertyNames.TimeZoneId))
                    {
                        var id = GetTextOrRaw(timeZoneId);
                        context.TimeZoneIds.Add(id);
                        context.AddOffsetRange(id, offsets);
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
                case IcalDiagnosticKind.QuotedParameterValue:
                    return options.AllowQuotedParameterValues;
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
        /// Gets the message for components nested more than <see cref="IcalCalendarBuilder.MaxComponentDepth"/> levels deep.
        /// </summary>
        internal static string BuildNestedTooDeeplyMessage()
        {
            return string.Format(CultureInfo.InvariantCulture, "Components are nested more than {0} levels deep; RFC 5545§3.6 allows three.",
                IcalCalendarBuilder.MaxComponentDepth);
        }

        /// <summary>
        /// Checks, for a writer walking a calendar it has already validated, that a component is nested no deeper than validation allows.
        /// </summary>
        /// <param name="depth">The level of the component, counting the VCALENDAR as the first.</param>
        /// <exception cref="IcalFileException">The component is nested more than <see cref="IcalCalendarBuilder.MaxComponentDepth"/> levels
        /// deep, because the calendar was changed after it was validated.</exception>
        internal static void CheckWriteDepth(int depth)
        {
            // Validation refuses a component nested in itself or too deeply, but another thread may change the calendar after it, and an
            // unbounded recursion would end the process with a stack overflow.
            if (depth <= IcalCalendarBuilder.MaxComponentDepth) return;

            var exception = new IcalFileException(BuildNestedTooDeeplyMessage());
            exception.Data.Add(IcalFileException.DataItemName.DiagnosticKind, IcalDiagnosticKind.InvalidComponentNesting);
            throw exception;
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
        /// Indicates whether a <c>VERSION</c> value (RFC 5545 §3.7.4) is one RFC 5545 allows: <c>2.0</c>, or the range <c>2.0;2.0</c>.
        /// </summary>
        internal static bool IsSupportedVersion(string value)
        {
            // ref. RFC 5545§3.7.4: vervalue = "2.0" / maxver / (minver ";" maxver), where minver and maxver are IANA-registered iCalendar
            // version identifiers.  The only one is the 2.0 that RFC 5545 itself defines, so other spellings of the number, such as 2 or
            // 02.000, and ranges reaching another version, such as 1.0;2.0, are not allowed.
            return string.Equals(value, SupportedVersion, StringComparison.Ordinal) ||
                   string.Equals(value, SupportedVersionRange, StringComparison.Ordinal);
        }

        /// <summary>
        /// Converts unescaped <c>VERSION</c> text to the form of a content line: each of <c>minver</c> and <c>maxver</c> is escaped as TEXT,
        /// and the semicolon between them is kept.
        /// </summary>
        /// <param name="value">The unescaped text, such as <c>2.0</c> or <c>2.0;2.0</c>.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> holds a character that iCalendar cannot hold.</exception>
        internal static string FormatVersion(string value)
        {
            // ref. RFC 5545§3.7.4: in minver ";" maxver the semicolon separates the two versions, so it is not escaped like a TEXT semicolon.
            var parts = value.Split(';');
            for (var index = 0; index < parts.Length; index++)
            {
                parts[index] = IcalValue.FormatText(parts[index], IcalValueFormat.Text);
            }

            return string.Join(";", parts);
        }

        internal static string BuildUnsupportedVersionMessage(string value)
        {
            return string.Format(CultureInfo.InvariantCulture,
                "The calendar has VERSION:{0}, but RFC 5545 allows only VERSION:2.0 (or the range 2.0;2.0), since 2.0 is the only iCalendar " +
                "version it defines.  vCalendar 1.0 (VERSION:1.0) is not supported.", value);
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
                ValidateValue(property, component.Name, path, context);
                ValidateFormatType(property, path, context);
                ValidateParameters(property, path, context);
                ValidateParameterQuoting(property, path, context);
                if (context.IsCalendar)
                {
                    ValidateTimeZoneReference(property, path, context);
                }
            }

            var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            context.Ancestors.Add(component);
            foreach (var child in component.Components)
            {
                int index;
                indexes.TryGetValue(child.Name, out index);
                indexes[child.Name] = ++index;

                var childPath = GetChildPath(path, child.Name, index);
                // IcalComponent does not override Equals, so this compares references.
                if (context.Ancestors.Contains(child))
                {
                    // A model built in code can hold a component inside itself; checking it would never end.
                    context.Add(IcalDiagnosticKind.InvalidComponentNesting,
                        string.Format(CultureInfo.InvariantCulture, "A {0} component is nested in itself.", child.Name.ToUpperInvariant()),
                        childPath, null, child.LineNumber);
                }
                else if (context.Ancestors.Count >= IcalCalendarBuilder.MaxComponentDepth)
                {
                    // The same limit as the readers, so that a tree built in code cannot end the process with a stack overflow.
                    context.Add(IcalDiagnosticKind.InvalidComponentNesting, BuildNestedTooDeeplyMessage(), childPath, null, child.LineNumber);
                }
                else
                {
                    ValidateComponent(child, childPath, context);
                }
            }

            context.Ancestors.RemoveAt(context.Ancestors.Count - 1);
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
            else if (IsName(name, IcalComponentNames.FreeBusy))
            {
                ValidateFreeBusyPeriod(component, path, context);
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
        /// Checks that the end of an event, or the due date of a to-do, has the value type of its start, is in local time if and only if its
        /// start is, and is later than its start, and that a duration from a DATE is in days or weeks.
        /// </summary>
        private static void ValidateEnd(IcalComponent component, string path, string endName, Context context)
        {
            var start = component.GetProperty(IcalPropertyNames.DtStart);
            IcalDateOrDateTime startValue;
            if (start is null || !TryGetDateOrDateTime(start, out startValue)) return;

            var end = component.GetProperty(endName);
            IcalDateOrDateTime endValue;
            if (end != null && TryGetDateOrDateTime(end, out endValue))
            {
                if (endValue.IsDate != startValue.IsDate)
                {
                    // ref. RFC 5545§3.8.2.2 and §3.8.2.3: the value type MUST be the same as DTSTART.
                    AddInvalidValue(end, path,
                        string.Format(CultureInfo.InvariantCulture, "The {0} property must have the same value type as the DTSTART property, {1}.",
                            endName, IcalValueTypeRegistry.GetValueTypeName(startValue.ValueType)), context);
                }
                else if (!startValue.IsDate && IsFloating(endValue) != IsFloating(startValue))
                {
                    // ref. RFC 5545§3.8.2.2 and §3.8.2.3: it MUST be a date with local time if and only if DTSTART is.
                    AddInvalidValue(end, path,
                        string.Format(CultureInfo.InvariantCulture,
                            "The {0} property must be a date with local time, with no time zone, if and only if the DTSTART property is one.", endName),
                        context);
                }
                else
                {
                    // ref. RFC 5545§3.8.2.2 and §3.8.2.3: its value MUST be later in time than the value of DTSTART.
                    ValidateLater(start, startValue, end, endValue, path, context);
                }
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
        /// Checks that the <c>DTSTART</c> and <c>DTEND</c> of a VFREEBUSY are in UTC, and that <c>DTEND</c> is later than <c>DTSTART</c>
        /// (RFC 5545 §3.8.2.2 and §3.8.2.4).
        /// </summary>
        private static void ValidateFreeBusyPeriod(IcalComponent component, string path, Context context)
        {
            IcalDateOrDateTime startValue, endValue;
            var start = component.GetProperty(IcalPropertyNames.DtStart);
            var end = component.GetProperty(IcalPropertyNames.DtEnd);
            var isStartUtc = IsFreeBusyUtc(start, path, out startValue, context);
            var isEndUtc = IsFreeBusyUtc(end, path, out endValue, context);
            if (start != null && end != null && isStartUtc && isEndUtc)
            {
                ValidateLater(start, startValue, end, endValue, path, context);
            }
        }

        /// <summary>
        /// Indicates whether the <c>DTSTART</c> or <c>DTEND</c> of a VFREEBUSY is a DATE-TIME in UTC, and reports it when it is another valid
        /// value.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static bool IsFreeBusyUtc(IcalContentLine? property, string path, out IcalDateOrDateTime value, Context context)
#else
        private static bool IsFreeBusyUtc(IcalContentLine property, string path, out IcalDateOrDateTime value, Context context)
#endif
        {
            value = default(IcalDateOrDateTime);
            if (property is null || !TryGetDateOrDateTime(property, out value)) return false;

            // ref. RFC 5545§3.8.2.4 and §3.8.2.2: within VFREEBUSY, the time MUST be specified in UTC.
            if (!value.IsDate && value.DateTime.Kind == IcalDateTimeKind.Utc) return true;

            AddInvalidValue(property, path,
                string.Format(CultureInfo.InvariantCulture, "The {0} property of a VFREEBUSY component must be a DATE-TIME in UTC.",
                    property.Name.ToUpperInvariant()), context);
            return false;
        }

        /// <summary>
        /// Reports an end that is not later in time than its start.  The two have the same value type, and are both floating or both not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two dates, two floating date-times, and two date-times in UTC are compared exactly.  A date-time with a time zone is not converted to
        /// UTC with its VTIMEZONE, as that expands the recurrence rules of the time zone's observances, whose cost a calendar sets.  Instead, the
        /// UTC offsets that the VTIMEZONE's observances name bound the instant of the wall-clock time: an end is reported when it falls at or
        /// before its start whatever offsets are in use.  So is an end with the same time zone and wall-clock time as its start, which RFC 5545
        /// §3.3.5 resolves to the same instant.
        /// </para>
        /// <para>
        /// No end later than its start is reported.  An end with a time zone that is earlier than its start by less than the spread of the
        /// offsets is not reported either, as it can fall in a gap, such as when clocks go forward, where RFC 5545 §3.3.5 reads it with the
        /// offset before the gap.
        /// </para>
        /// </remarks>
        private static void ValidateLater(IcalContentLine start, IcalDateOrDateTime startValue, IcalContentLine end, IcalDateOrDateTime endValue,
            string path, Context context)
        {
            bool isNotLater;
            if (startValue.IsDate)
            {
                isNotLater = endValue.Date.DayNumber <= startValue.Date.DayNumber;
            }
            else
            {
                var startTime = startValue.DateTime;
                var endTime = endValue.DateTime;

                // How far the end's wall-clock time is before the start's, in half-seconds.
                var difference = GetClockHalfSeconds(startTime) - GetClockHalfSeconds(endTime);
                if (startTime.Kind == IcalDateTimeKind.Zoned && endTime.Kind == IcalDateTimeKind.Zoned &&
                    string.Equals(startTime.TimeZoneId, endTime.TimeZoneId, StringComparison.Ordinal) && difference == 0)
                {
                    isNotLater = true;
                }
                else
                {
                    // The end's instant, its wall-clock time less its offset, is at or before the start's unless the start's offset exceeds the
                    // end's by more than the difference.
                    isNotLater = difference >= 2 * (GetOffsetBound(startTime, true, context) - GetOffsetBound(endTime, false, context));
                }
            }

            if (!isNotLater) return;

            AddInvalidValue(end, path,
                string.Format(CultureInfo.InvariantCulture, "The {0} property must be later in time than the {1} property.",
                    end.Name.ToUpperInvariant(), start.Name.ToUpperInvariant()), context);
        }

        /// <summary>
        /// Gets the wall-clock time of a date-time, in half-seconds from the start of 0001-01-01.  A leap second (RFC 5545 §3.3.12) falls
        /// between second 59 and the next minute, so that it orders after the one and before the other.
        /// </summary>
        private static long GetClockHalfSeconds(IcalDateTime value)
        {
            var time = value.Time;
            var isLeapSecond = time.Second == 60;
            var seconds = value.Date.DayNumber * SecondsPerDay + time.Hour * 3600L + time.Minute * 60L + (isLeapSecond ? 59 : time.Second);
            return seconds * 2 + (isLeapSecond ? 1 : 0);
        }

        /// <summary>
        /// Gets the largest or smallest UTC offset, in seconds, that a date-time can be in: none for a floating one or one in UTC, the range of
        /// the offsets its VTIMEZONE names for one with a time zone, and any offset at all for a time zone the calendar does not define.
        /// </summary>
        private static long GetOffsetBound(IcalDateTime value, bool isMaximum, Context context)
        {
            if (value.Kind != IcalDateTimeKind.Zoned) return 0;

            OffsetRange range;
            if (!context.TryGetOffsetRange(value.TimeZoneId ?? string.Empty, out range))
            {
                range = OffsetRange.Any;
            }

            return isMaximum ? range.Maximum : range.Minimum;
        }

        /// <summary>
        /// Gets the range of the <c>TZOFFSETFROM</c> and <c>TZOFFSETTO</c> values of a VTIMEZONE's observances: every offset that a local time
        /// in the time zone can be read with, including one in a gap (RFC 5545 §3.3.5) and one before the first onset.
        /// </summary>
        private static OffsetRange GetOffsetRange(IcalComponent timeZone)
        {
            var minimum = long.MaxValue;
            var maximum = long.MinValue;
            foreach (var observance in timeZone.Components)
            {
                if (!IsName(observance.Name, IcalComponentNames.Standard) && !IsName(observance.Name, IcalComponentNames.Daylight)) continue;

                foreach (var property in observance.Properties)
                {
                    if (!IsName(property.Name, IcalPropertyNames.TimeZoneOffsetFrom) && !IsName(property.Name, IcalPropertyNames.TimeZoneOffsetTo))
                        continue;

                    IcalUtcOffset offset;
                    if (!IcalUtcOffset.TryParse(property.Value.AsSpan(), IcalValueFormat.Text, out offset)) return OffsetRange.Any;

                    var seconds = (long)offset.Offset.TotalSeconds;
                    minimum = Math.Min(minimum, seconds);
                    maximum = Math.Max(maximum, seconds);
                }
            }

            return minimum > maximum ? OffsetRange.Any : new OffsetRange(minimum, maximum);
        }

        private static bool IsFloating(IcalDateOrDateTime value)
        {
            return value.DateTime.Kind == IcalDateTimeKind.Floating;
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
        private static void ValidateValue(IcalContentLine property, string componentName, string path, Context context)
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

            ValidateValueRules(property, componentName, path, valueType, context);
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
                    return IsValidText(property.Name, span);
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
        /// Checks a TEXT value: every backslash starts a valid escape, and every comma and semicolon is escaped, except where the property's
        /// grammar uses one to separate the parts of its value.
        /// </summary>
        private static bool IsValidText(string name, ReadOnlySpan<char> value)
        {
            // ref. RFC 5545§3.3.11: TSAFE-CHAR excludes "," and ";", so TEXT escapes them.  An unescaped comma separates the items of a
            // CATEGORIES or RESOURCES list (RFC 5545§3.8.1.2 and §3.8.1.10), an unescaped semicolon the minver and maxver of VERSION
            // (RFC 5545§3.7.4), and one or two the statcode, statdesc and extdata of REQUEST-STATUS (RFC 5545§3.8.8.3).
            var isRequestStatus = IsName(name, IcalPropertyNames.RequestStatus);
            if (isRequestStatus && !StartsWithStatusCode(value)) return false;

            var allowsComma = TextListProperties.Contains(name, StringComparer.OrdinalIgnoreCase);
            var allowedSemicolons = isRequestStatus ? 2 : IsName(name, IcalPropertyNames.Version) ? 1 : 0;
            var semicolons = 0;
            for (var index = 0; index < value.Length; index++)
            {
                switch (value[index])
                {
                    case '\\':
                        // ref. RFC 5545§3.3.11: ESCAPED-CHAR = ("\\" / "\;" / "\," / "\N" / "\n").
                        if (++index == value.Length || !IsEscapedCharacter(value[index])) return false;
                        break;
                    case ',':
                        if (!allowsComma) return false;
                        break;
                    case ';':
                        if (++semicolons > allowedSemicolons) return false;
                        break;
                }
            }

            return true;
        }

        private static bool IsEscapedCharacter(char character)
        {
            return character == '\\' || character == ';' || character == ',' || character == 'N' || character == 'n';
        }

        /// <summary>
        /// Checks that a REQUEST-STATUS value starts with its statcode and the semicolon after it.
        /// </summary>
        private static bool StartsWithStatusCode(ReadOnlySpan<char> value)
        {
            // ref. RFC 5545§3.8.8.3: rstatus = statcode ";" statdesc [";" extdata], statcode = 1*DIGIT 1*2("." 1*DIGIT).
            var index = SkipDigits(value, 0);
            if (index == 0) return false;

            var levels = 0;
            while (index < value.Length && value[index] == '.' && levels < 2)
            {
                var end = SkipDigits(value, index + 1);
                if (end == index + 1) return false;

                index = end;
                levels++;
            }

            return levels > 0 && index < value.Length && value[index] == ';';
        }

        private static int SkipDigits(ReadOnlySpan<char> value, int index)
        {
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                index++;
            }

            return index;
        }

        /// <summary>
        /// Checks the rules for a valid value that RFC 5545 sets on particular properties.
        /// </summary>
        private static void ValidateValueRules(IcalContentLine property, string componentName, string path, IcalValueType valueType,
            Context context)
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
            else if (IsName(name, IcalPropertyNames.Status))
            {
                // ref. RFC 5545§3.8.1.11: the values of each component that defines STATUS.  RFC 5545 defines it in no other component, such
                // as an x-comp, whose lines are any contentline, so its value is not checked there.
                if (IsName(componentName, IcalComponentNames.Event))
                {
                    ValidateValueSet(property, path, EventStatuses, context);
                }
                else if (IsName(componentName, IcalComponentNames.Todo))
                {
                    ValidateValueSet(property, path, TodoStatuses, context);
                }
                else if (IsName(componentName, IcalComponentNames.Journal))
                {
                    ValidateValueSet(property, path, JournalStatuses, context);
                }
            }
            else if (IsName(name, IcalPropertyNames.Transparency) && IsName(componentName, IcalComponentNames.Event))
            {
                // ref. RFC 5545§3.8.2.7: TRANSP is defined in VEVENT only.
                ValidateValueSet(property, path, TransparencyValues, context);
            }
            else if (IsName(name, IcalPropertyNames.CalendarScale) && IsName(componentName, IcalComponentNames.Calendar))
            {
                // ref. RFC 5545§3.7.1: CALSCALE is defined in VCALENDAR only.
                ValidateValueSet(property, path, CalendarScaleValues, context);
            }
        }

        /// <summary>
        /// Checks that a property whose grammar has a closed set of values, with no <c>iana-token</c> or <c>x-name</c>, has one of them.  Like
        /// every enumerated value, they are case-insensitive (RFC 5545 §2).
        /// </summary>
        private static void ValidateValueSet(IcalContentLine property, string path, string[] values, Context context)
        {
            if (values.Contains(property.Value, StringComparer.OrdinalIgnoreCase)) return;

            AddInvalidValue(property, path,
                string.Format(CultureInfo.InvariantCulture, "The value of the {0} property must be {1}.", property.Name.ToUpperInvariant(),
                    values.Length == 1 ? values[0] : string.Join(", ", values, 0, values.Length - 1) + " or " + values[values.Length - 1]),
                context);
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
        /// Checks the <c>FMTTYPE</c> parameter of any property, including one RFC 5545 and RFC 7986 do not define: RFC 5545 §3.2.8 sets its syntax.
        /// RFC 5545 §3.8.1.1 and RFC 7986 §5.10 allow it once on ATTACH and IMAGE, but §3.8.8.2 sets no such rule for a non-standard property.
        /// </summary>
        private static void ValidateFormatType(IcalContentLine property, string path, Context context)
        {
            var count = 0;
            var isValid = true;
            foreach (var parameter in property.ParameterArray)
            {
                if (!IsName(parameter.Name, IcalParameterNames.FormatType)) continue;

                count++;
                isValid &= parameter.ValueArray.Length == 1 && IcalPropertyValue.TryParseFormatType(parameter.ValueArray[0].AsSpan(), out _);
            }

            if (isValid && (count < 2 || !IcalValueTypeRegistry.IsKnownProperty(property.Name.AsSpan()))) return;

            AddInvalidValue(property, path, IcalPropertyValue.BuildInvalidFormatTypeMessage(property.Name.ToUpperInvariant()), context);
        }

        /// <summary>
        /// Checks how many values each parameter RFC 5545 and RFC 7986 define has, and, on a property they define, how often it occurs.
        /// </summary>
        private static void ValidateParameters(IcalContentLine property, string path, Context context)
        {
            var parameters = property.ParameterArray;

            // Only a line with two or more parameters can repeat one, and few do, so most lines allocate nothing here.
            var seen = parameters.Length > 1 && IcalValueTypeRegistry.IsKnownProperty(property.Name.AsSpan())
                ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                : null;
            foreach (var parameter in parameters)
            {
                var isSingleValue = SingleValueParameters.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase);
                if (!isSingleValue && !MultipleValueParameters.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase)) continue;

                // ref. RFC 5545§3.2: the grammar of each of these parameters has one value, so a comma in it must be in a quoted-string.
                if (isSingleValue && parameter.ValueArray.Length > 1)
                {
                    AddInvalidValue(property, path,
                        string.Format(CultureInfo.InvariantCulture, "The {0} parameter of the {1} property can have only one value.",
                            parameter.Name.ToUpperInvariant(), property.Name.ToUpperInvariant()), context);
                }

                // ref. RFC 5545§3.8: each property's parameters "MUST NOT occur more than once".  §3.8.8.2 sets no such rule for a non-standard one.
                if (seen != null && !seen.Add(parameter.Name))
                {
                    AddInvalidValue(property, path,
                        string.Format(CultureInfo.InvariantCulture, "The {0} parameter appears more than once in the {1} property.",
                            parameter.Name.ToUpperInvariant(), property.Name.ToUpperInvariant()), context);
                }
            }
        }

        /// <summary>
        /// Checks that no parameter in <see cref="UnquotedParameters"/> has a quoted value, on any property, including one RFC 5545 and RFC 7986
        /// do not define: the grammar of a parameter is the same wherever it appears.
        /// </summary>
        /// <remarks>
        /// A line that keeps its parameters as they were read is written as read, so its values are quoted where they were.  Any other line,
        /// such as one built in code, or one a typed property setter made from a kept parameter, is formatted afresh, and
        /// <see cref="IcalWriter"/> quotes a value of it that has a character <c>paramtext</c> excludes.
        /// </remarks>
        private static void ValidateParameterQuoting(IcalContentLine property, string path, Context context)
        {
            var isAsRead = property.RawParameters != null;
            foreach (var parameter in property.ParameterArray)
            {
                var isQuoted = isAsRead ? parameter.HasQuotedValue : NeedsQuotes(parameter.ValueArray);
                if (!isQuoted || !UnquotedParameters.Contains(parameter.Name, StringComparer.OrdinalIgnoreCase)) continue;

                var message = isAsRead
                    ? "The {0} parameter of the {1} property has a value in double quotes, which the grammar of the parameter does not allow."
                    : "The {0} parameter of the {1} property has a value with a ':', ';' or ',', which would have to be written in double quotes, " +
                      "and the grammar of the parameter does not allow them.";
                context.Add(IcalDiagnosticKind.QuotedParameterValue,
                    string.Format(CultureInfo.InvariantCulture, message, parameter.Name.ToUpperInvariant(), property.Name.ToUpperInvariant()),
                    path, property.Name, property.LineNumber);
            }
        }

        /// <summary>
        /// Indicates whether a writer formatting the values afresh would quote any of them.  This must match the rule of
        /// <c>ContentLineFormatter</c>, which quotes a value with a character that <c>paramtext</c> excludes.
        /// </summary>
        private static bool NeedsQuotes(string[] values)
        {
            foreach (var value in values)
            {
                // ref. RFC 5545§3.1: paramtext = *SAFE-CHAR, which excludes DQUOTE, ";", ":" and ","; caret encoding replaces any DQUOTE.
                if (value.IndexOfAny(ParamtextExcludedCharacters) >= 0) return true;
            }

            return false;
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
        /// The smallest and largest UTC offsets, in seconds, that a local time in a time zone can be read with.
        /// </summary>
        private readonly struct OffsetRange
        {
            /// <summary>
            /// Every offset <c>utc-offset</c> can express, for a time zone whose offsets are not known.
            /// </summary>
            internal static readonly OffsetRange Any = new OffsetRange(-MaxOffsetSeconds, MaxOffsetSeconds);

            internal OffsetRange(long minimum, long maximum)
            {
                Minimum = minimum;
                Maximum = maximum;
            }

            internal long Minimum { get; }

            internal long Maximum { get; }
        }

        /// <summary>
        /// What the checks of a component need to know about the calendar it is in, and where they report.
        /// </summary>
        private sealed class Context
        {
            private readonly List<IcalDiagnostic> _diagnostics;
            private readonly Dictionary<string, OffsetRange> _offsetRanges = new Dictionary<string, OffsetRange>(StringComparer.Ordinal);

            internal Context(List<IcalDiagnostic> diagnostics)
            {
                _diagnostics = diagnostics;
                TimeZoneIds = new HashSet<string>(StringComparer.Ordinal);
                Ancestors = new List<IcalComponent>();
            }

            /// <summary>
            /// Gets the component being checked and the components it is nested in, outermost first.
            /// </summary>
            internal List<IcalComponent> Ancestors { get; }

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

            /// <summary>
            /// Adds the UTC offsets of a VTIMEZONE to those of any other with the same identifier.
            /// </summary>
            internal void AddOffsetRange(string timeZoneId, OffsetRange range)
            {
                OffsetRange existing;
                _offsetRanges[timeZoneId] = _offsetRanges.TryGetValue(timeZoneId, out existing)
                    ? new OffsetRange(Math.Min(existing.Minimum, range.Minimum), Math.Max(existing.Maximum, range.Maximum))
                    : range;
            }

            /// <summary>
            /// Gets the range of the UTC offsets of the calendar's VTIMEZONE components with an identifier.
            /// </summary>
            internal bool TryGetOffsetRange(string timeZoneId, out OffsetRange range)
            {
                return _offsetRanges.TryGetValue(timeZoneId, out range);
            }

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
