using System;
using System.Collections.Generic;
using System.Globalization;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The rules of a time zone defined by a VTIMEZONE (RFC 5545 §3.6.5): the onsets of its observances, from each one's <c>DTSTART</c>,
    /// <c>RRULE</c> and <c>RDATE</c>, with its <c>TZOFFSETFROM</c> and <c>TZOFFSETTO</c>.
    /// </summary>
    /// <remarks>
    /// The observances are read when the rules are created, so later changes to the VTIMEZONE do not affect them.  Before the first onset, the
    /// <c>TZOFFSETFROM</c> of that onset is in use.
    /// </remarks>
    internal sealed class IcalCalendarTimeZoneRules : IcalTimeZoneRules
    {
        private const long TicksPerSecond = TimeSpan.TicksPerSecond;

        /// <summary>
        /// The span searched first for the latest onset of an observance before an instant: a little over a year, which holds an onset of any
        /// yearly rule that is still in use.
        /// </summary>
        private static readonly long RecentSearchTicks = TimeSpan.TicksPerDay * 400;

        private static readonly IcalOptions ExpansionOptions = new IcalOptions();

        private readonly Observance[] _observances;
        private readonly Observance _earliest;

        private IcalCalendarTimeZoneRules(string timeZoneId, Observance[] observances) : base(timeZoneId)
        {
            _observances = observances;
            _earliest = observances[0];
            foreach (var observance in observances)
            {
                if (observance.EarliestOnset < _earliest.EarliestOnset)
                {
                    _earliest = observance;
                }
            }
        }

        /// <summary>
        /// Creates the rules of a VTIMEZONE.
        /// </summary>
        /// <exception cref="IcalFileException">The VTIMEZONE has no observances, or an observance lacks <c>DTSTART</c>, <c>TZOFFSETFROM</c>
        /// or <c>TZOFFSETTO</c>, or has a value that is not valid.</exception>
        internal static IcalCalendarTimeZoneRules Create(string timeZoneId, IcalTimeZone timeZone)
        {
            var observances = new List<Observance>();
            foreach (var component in timeZone.Observances)
            {
                observances.Add(Observance.Create(timeZoneId, component));
            }

            if (observances.Count == 0)
                throw new IcalFileException(string.Format(CultureInfo.InvariantCulture,
                    "The VTIMEZONE '{0}' has no STANDARD or DAYLIGHT observance, so it defines no UTC offset.", timeZoneId));

            return new IcalCalendarTimeZoneRules(timeZoneId, observances.ToArray());
        }

        protected override IcalUtcOffset GetUtcOffsetCore(DateTime utc)
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Observance? latest = null;
#else
            Observance latest = null;
#endif
            var latestInstant = DateTime.MinValue;
            foreach (var observance in _observances)
            {
                DateTime instant;
                // >= so that on a tie the last observance is kept, as in GetTransitionsCore.
                if (observance.TryGetLatestOnset(utc, out instant) && (latest is null || instant >= latestInstant))
                {
                    latest = observance;
                    latestInstant = instant;
                }
            }

            return latest is null ? _earliest.OffsetFrom : latest.OffsetTo;
        }

        protected override IEnumerable<IcalTimeZoneTransition> GetTransitionsCore(DateTime fromUtc, DateTime toUtc)
        {
            var transitions = new List<IcalTimeZoneTransition>();
            foreach (var observance in _observances)
            {
                observance.AddTransitions(fromUtc, toUtc, transitions);
            }

            // Sorted by instant, keeping the order of the observances for transitions at the same instant; the last of those is the one in use.
            var sorted = new List<IcalTimeZoneTransition>(transitions.Count);
            var order = new int[transitions.Count];
            for (var index = 0; index < order.Length; index++)
            {
                order[index] = index;
            }

            Array.Sort(order, (left, right) =>
            {
                var difference = transitions[left].InstantUtc.CompareTo(transitions[right].InstantUtc);
                return difference != 0 ? difference : left.CompareTo(right);
            });
            foreach (var index in order)
            {
                var transition = transitions[index];
                if (sorted.Count > 0 && sorted[sorted.Count - 1].InstantUtc == transition.InstantUtc)
                {
                    sorted[sorted.Count - 1] = transition;
                }
                else
                {
                    sorted.Add(transition);
                }
            }

            return sorted;
        }

        /// <summary>
        /// A STANDARD or DAYLIGHT observance, read from its component.
        /// </summary>
        private sealed class Observance
        {
            private readonly IcalDateTime _start;
            private readonly IReadOnlyList<IcalRecurrenceRule> _rules;
            private readonly List<DateTime> _recurrenceDates;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private readonly string? _name;
#else
            private readonly string _name;
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private Observance(IcalTimeZoneObservanceKind kind, string? name, IcalDateTime start, IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo,
                IReadOnlyList<IcalRecurrenceRule> rules, List<DateTime> recurrenceDates)
#else
            private Observance(IcalTimeZoneObservanceKind kind, string name, IcalDateTime start, IcalUtcOffset offsetFrom, IcalUtcOffset offsetTo,
                IReadOnlyList<IcalRecurrenceRule> rules, List<DateTime> recurrenceDates)
#endif
            {
                Kind = kind;
                _name = name;
                _start = start;
                OffsetFrom = offsetFrom;
                OffsetTo = offsetTo;
                _rules = rules;
                _recurrenceDates = recurrenceDates;

                var startLocal = ToLocalDateTime(start);
                EarliestOnset = recurrenceDates.Count > 0 && recurrenceDates[0] < startLocal ? recurrenceDates[0] : startLocal;
            }

            internal IcalTimeZoneObservanceKind Kind { get; }

            internal IcalUtcOffset OffsetFrom { get; }

            internal IcalUtcOffset OffsetTo { get; }

            /// <summary>
            /// Gets the earliest onset, in local time: the earlier of <c>DTSTART</c> and the first <c>RDATE</c>.
            /// </summary>
            internal DateTime EarliestOnset { get; }

            internal static Observance Create(string timeZoneId, IcalTimeZoneObservance component)
            {
                var start = component.DtStart;
                var offsetFrom = component.OffsetFrom;
                var offsetTo = component.OffsetTo;
                if (!start.HasValue || !offsetFrom.HasValue || !offsetTo.HasValue)
                    throw new IcalFileException(string.Format(CultureInfo.InvariantCulture,
                        "A {0} observance of the VTIMEZONE '{1}' lacks DTSTART, TZOFFSETFROM or TZOFFSETTO, so its onsets cannot be found.",
                        component.Name, timeZoneId));

                // ref. RFC 5545§3.6.5: DTSTART and RDATE are local times, in the offset given by TZOFFSETFROM.  Their wall-clock fields are used
                // whatever their form.
                var floatingStart = new IcalDateTime(start.Value.Date, new IcalTime(start.Value.Time.Hour, start.Value.Time.Minute,
                    start.Value.Time.Second));
                return new Observance(component.Kind, component.TimeZoneName, floatingStart, offsetFrom.Value, offsetTo.Value,
                    component.RecurrenceRules, GetRecurrenceDates(component));
            }

            /// <summary>
            /// Gets the latest onset at or before an instant.
            /// </summary>
            internal bool TryGetLatestOnset(DateTime utc, out DateTime instant)
            {
                instant = default(DateTime);

                // An onset is at or before the instant where its local time is at or before the instant in the offset before it.
                var localLimit = AddClamped(utc, OffsetFrom.Offset.Ticks, DateTimeKind.Unspecified);
                if (EarliestOnset > localLimit) return false;

                var to = AddClamped(localLimit, TicksPerSecond, DateTimeKind.Unspecified);
                var latest = GetLatestOnset(AddClamped(localLimit, -RecentSearchTicks, DateTimeKind.Unspecified), to) ??
                             GetLatestOnset(null, to);
                if (!latest.HasValue) return false;

                instant = ToInstant(latest.Value);
                return true;
            }

            /// <summary>
            /// Adds the transitions whose instants fall in a period.
            /// </summary>
            internal void AddTransitions(DateTime fromUtc, DateTime toUtc, List<IcalTimeZoneTransition> transitions)
            {
                var fromLocal = AddClamped(fromUtc, OffsetFrom.Offset.Ticks, DateTimeKind.Unspecified);
                var toLocal = AddClamped(toUtc, OffsetFrom.Offset.Ticks, DateTimeKind.Unspecified);
                foreach (var onset in GetOnsets(fromLocal, toLocal))
                {
                    var instant = ToInstant(onset);
                    if (instant >= fromUtc && instant < toUtc)
                    {
                        transitions.Add(new IcalTimeZoneTransition(instant, OffsetFrom, OffsetTo, Kind, _name));
                    }
                }
            }

            private DateTime? GetLatestOnset(DateTime? fromLocal, DateTime toLocal)
            {
                var onsets = GetOnsets(fromLocal, toLocal);
                return onsets.Count > 0 ? onsets[onsets.Count - 1] : (DateTime?)null;
            }

            /// <summary>
            /// Gets the onsets in a span of local time, in order and without repeats: <c>DTSTART</c>, the occurrences of each <c>RRULE</c>, and
            /// each <c>RDATE</c>.
            /// </summary>
            /// <param name="fromLocal">The start of the span, which is included; or <c>null</c> for none.</param>
            /// <param name="toLocal">The end of the span, which is excluded.</param>
            private List<DateTime> GetOnsets(DateTime? fromLocal, DateTime toLocal)
            {
                var onsets = new List<DateTime>();
                var startLocal = ToLocalDateTime(_start);
                if ((!fromLocal.HasValue || startLocal >= fromLocal.Value) && startLocal < toLocal)
                {
                    onsets.Add(startLocal);
                }

                if (_rules.Count > 0 && startLocal < toLocal)
                {
                    var from = fromLocal.HasValue ? IcalDateTime.FromDateTime(fromLocal.Value) : (IcalDateTime?)null;
                    var to = IcalDateTime.FromDateTime(toLocal);
                    foreach (var rule in _rules)
                    {
                        // ref. RFC 5545§3.6.5: a UNTIL in UTC is compared with an onset converted with TZOFFSETFROM.
                        foreach (var occurrence in rule.GetOccurrences(_start, from, to, ToUtcDateTime, ExpansionOptions))
                        {
                            onsets.Add(ToLocalDateTime(occurrence));
                        }
                    }
                }

                foreach (var recurrenceDate in _recurrenceDates)
                {
                    if ((!fromLocal.HasValue || recurrenceDate >= fromLocal.Value) && recurrenceDate < toLocal)
                    {
                        onsets.Add(recurrenceDate);
                    }
                }

                onsets.Sort();
                for (var index = onsets.Count - 1; index > 0; index--)
                {
                    if (onsets[index] == onsets[index - 1])
                    {
                        onsets.RemoveAt(index);
                    }
                }

                return onsets;
            }

            private DateTime ToInstant(DateTime onset)
            {
                return AddClamped(onset, -OffsetFrom.Offset.Ticks, DateTimeKind.Utc);
            }

            private IcalDateTime ToUtcDateTime(IcalDateTime onset)
            {
                return IcalDateTime.FromDateTime(ToInstant(ToLocalDateTime(onset)));
            }

            /// <summary>
            /// Gets the wall-clock time of a date-time, of kind <see cref="DateTimeKind.Unspecified"/>.  A leap second is read as the second
            /// before it.
            /// </summary>
            private static DateTime ToLocalDateTime(IcalDateTime value)
            {
                var time = value.Time;
                var secondOfDay = time.Hour * 3600 + time.Minute * 60 + Math.Min(time.Second, 59);
                return new DateTime(value.Date.ToDateTime().Ticks + secondOfDay * TicksPerSecond, DateTimeKind.Unspecified);
            }

            /// <summary>
            /// Gets the local times of the <c>RDATE</c> properties of an observance, in order.  A DATE is read as midnight, and a PERIOD as its
            /// start.
            /// </summary>
            private static List<DateTime> GetRecurrenceDates(IcalTimeZoneObservance component)
            {
                var recurrenceDates = new List<DateTime>();
                foreach (var property in component.GetProperties(IcalPropertyNames.RecurrenceDates))
                {
                    var valueType = IcalPropertyValue.GetValueType(property);
                    foreach (var item in property.Value.Split(','))
                    {
                        DateTime local;
                        if (!TryParseRecurrenceDate(property, valueType, item, out local))
                            throw IcalPropertyValue.BuildInvalidValueException(property, valueType);

                        recurrenceDates.Add(local);
                    }
                }

                recurrenceDates.Sort();
                return recurrenceDates;
            }

            private static bool TryParseRecurrenceDate(IcalContentLine property, IcalValueType valueType, string item, out DateTime local)
            {
                local = default(DateTime);
                switch (valueType)
                {
                    case IcalValueType.Date:
                    case IcalValueType.DateTime:
                        IcalDateOrDateTime value;
                        if (!IcalPropertyValue.TryParseDateOrDateTime(property, item.AsSpan(), out value)) return false;

                        local = value.IsDate ? value.Date.ToDateTime() : ToLocalDateTime(value.DateTime);
                        return true;
                    case IcalValueType.Period:
                        IcalPeriod period;
                        if (!IcalPeriod.TryParse(item.AsSpan(), IcalValueFormat.Text, out period)) return false;

                        local = ToLocalDateTime(period.Start);
                        return true;
                    default:
                        return false;
                }
            }
        }
    }
}
