using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Expands the recurrence set (RFC 5545 §3.8.5) of an event, to-do or journal entry into its instances: DTSTART, the occurrences of each
    /// RRULE and each RDATE, less each EXDATE, with the components that override instances by their RECURRENCE-ID.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The instances are generated in local time and compared by a key: the instant in UTC of a value in UTC or in a named time zone, and the
    /// wall-clock time, read as though it were in UTC, of a floating value or a DATE.
    /// </para>
    /// <para>
    /// An override can move an instance, and a time in a daylight saving gap can resolve to an instant after that of a later local time, so the
    /// instances are not quite in order of their keys as they are generated.  They pass through a buffer that holds each until no later
    /// instance can come before it: no instance moves by more than <see cref="_margin"/>.
    /// </para>
    /// </remarks>
    internal sealed class IcalOccurrenceExpander<TComponent> where TComponent : IcalComponent
    {
        private const long TicksPerDay = TimeSpan.TicksPerDay;
        private const long TicksPerSecond = TimeSpan.TicksPerSecond;

        /// <summary>
        /// The ticks of 10000-01-01, just after the last date.
        /// </summary>
        private const long EndTicks = (IcalDate.MaxDayNumber + 1L) * TicksPerDay;

        /// <summary>
        /// The most by which a local time and its instant in UTC differ, or a time in a daylight saving gap moves, with room to spare.
        /// </summary>
        private const long ClockMargin = 2 * TicksPerDay;

        private readonly TComponent _component;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly IcalCalendar? _calendar;
#else
        private readonly IcalCalendar _calendar;
#endif
        private readonly IIcalTimeZoneResolver _resolver;
        private readonly Dictionary<string, IcalTimeZoneRules> _rules = new Dictionary<string, IcalTimeZoneRules>(StringComparer.Ordinal);

        private Instant _start;
        private EndRule _endRule = EndRule.None;
        private long _fromKey = long.MinValue;
        private long _toKey = long.MaxValue;
        private long _margin = ClockMargin;
        private readonly List<IEnumerable<Instance>> _sources = new List<IEnumerable<Instance>>();
        private readonly HashSet<long> _excludedKeys = new HashSet<long>();
        private readonly HashSet<int> _excludedDays = new HashSet<int>();
        private readonly HashSet<long> _overriddenKeys = new HashSet<long>();
        private readonly List<IcalOccurrence<TComponent>> _overrides = new List<IcalOccurrence<TComponent>>();
        private readonly List<long> _overrideKeys = new List<long>();
        private readonly List<RangeOverride> _ranges = new List<RangeOverride>();

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalOccurrenceExpander(TComponent component, IcalCalendar? calendar, IIcalTimeZoneResolver resolver)
#else
        private IcalOccurrenceExpander(TComponent component, IcalCalendar calendar, IIcalTimeZoneResolver resolver)
#endif
        {
            _component = component;
            _calendar = calendar;
            _resolver = resolver;
        }

        /// <summary>
        /// Gets the instances of a component's recurrence set whose start falls in a window.
        /// </summary>
        /// <remarks>
        /// The properties are read, and the time zones of DTSTART and of the window resolved, before this returns, so a value that is not valid
        /// throws here; the instances are then generated as they are enumerated.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static IEnumerable<IcalOccurrence<TComponent>> GetOccurrences(TComponent component, IcalCalendar? calendar, IcalDateTime? from,
            IcalDateTime? to, IIcalTimeZoneResolver resolver, IcalOptions options)
#else
        internal static IEnumerable<IcalOccurrence<TComponent>> GetOccurrences(TComponent component, IcalCalendar calendar, IcalDateTime? from,
            IcalDateTime? to, IIcalTimeZoneResolver resolver, IcalOptions options)
#endif
        {
            if (resolver is null)
                throw new ArgumentNullException(nameof(resolver));
            if (options is null)
                throw new ArgumentNullException(nameof(options));
            if (options.MaxRecurrenceIterations < 1)
                throw new ArgumentException("MaxRecurrenceIterations must be at least 1.", nameof(options));

            var start = component.GetDateOrDateTime(IcalPropertyNames.DtStart);
            if (!start.HasValue) return new IcalOccurrence<TComponent>[0];

            var expander = new IcalOccurrenceExpander<TComponent>(component, calendar, resolver);
            expander.Prepare(start.Value, from, to, options);
            return expander.Expand();
        }

        #region Preparation

        private void Prepare(IcalDateOrDateTime start, IcalDateTime? from, IcalDateTime? to, IcalOptions options)
        {
            _start = ToInstant(start);
            _endRule = GetEndRule(_component, _start);
            if (from.HasValue)
            {
                _fromKey = ToInstant(from.Value).Key;
            }

            if (to.HasValue)
            {
                _toKey = ToInstant(to.Value).Key;
            }

            // ref. RFC 5545§3.8.4.4: a component with a RECURRENCE-ID is one instance of a recurrence set, so it gives only that instance.
            var recurrenceId = _component.GetDateOrDateTime(IcalPropertyNames.RecurrenceId);
            if (recurrenceId.HasValue)
            {
                AddOverride(_component, recurrenceId.Value, false);
                return;
            }

            ReadExceptionDates();
            ReadOverrides();
            _sources.Add(new[] { new Instance(_start, EndRule.Inherit) });
            AddRuleSources(options, to.HasValue);
            AddRecurrenceDateSource();
        }

        /// <summary>
        /// Reads the EXDATE properties: a DATE-TIME excludes the instance at that instant, and a DATE every instance on that local date.
        /// </summary>
        private void ReadExceptionDates()
        {
            foreach (var property in _component.GetProperties(IcalPropertyNames.ExceptionDates))
            {
                foreach (var item in property.Value.Split(','))
                {
                    IcalDateOrDateTime value;
                    if (!IcalPropertyValue.TryParseDateOrDateTime(property, item.AsSpan(), out value))
                        throw IcalPropertyValue.BuildInvalidValueException(property, IcalPropertyValue.GetValueType(property));

                    if (value.IsDate || _start.Value.IsDate)
                    {
                        _excludedDays.Add(value.Date.DayNumber);
                    }
                    else
                    {
                        _excludedKeys.Add(ToInstant(value).Key);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the components of the calendar that override instances: those of the same type and UID with a RECURRENCE-ID.
        /// </summary>
        private void ReadOverrides()
        {
            var uid = _component.GetText(IcalPropertyNames.Uid);
            if (_calendar is null || uid is null) return;

            // Of two overrides of the same instance, the one with the higher SEQUENCE wins, then the later one.
            var byKey = new Dictionary<long, Candidate>();
            var order = new List<long>();
            foreach (var component in _calendar.Components)
            {
                var candidate = component as TComponent;
                if (candidate is null || ReferenceEquals(candidate, _component) || candidate.GetType() != _component.GetType() ||
                    !string.Equals(candidate.GetText(IcalPropertyNames.Uid), uid, StringComparison.Ordinal)) continue;

                var recurrenceId = candidate.GetDateOrDateTime(IcalPropertyNames.RecurrenceId);
                if (!recurrenceId.HasValue) continue;

                var key = ToInstant(recurrenceId.Value).Key;
                var sequence = candidate.GetInteger(IcalPropertyNames.Sequence) ?? 0;
                Candidate existing;
                if (byKey.TryGetValue(key, out existing))
                {
                    if (sequence < existing.Sequence) continue;
                }
                else
                {
                    order.Add(key);
                }

                byKey[key] = new Candidate(candidate, recurrenceId.Value, sequence);
            }

            foreach (var key in order)
            {
                var candidate = byKey[key];
                if (IsExcluded(ToInstant(candidate.RecurrenceId))) continue;

                var property = candidate.Component.GetProperty(IcalPropertyNames.RecurrenceId);
                var range = property is null ? null : IcalPropertyValue.GetParameterValue(property, IcalParameterNames.Range);
                AddOverride(candidate.Component, candidate.RecurrenceId,
                    string.Equals(range, "THISANDFUTURE", StringComparison.OrdinalIgnoreCase));
            }

            _ranges.Sort((left, right) => left.Key.CompareTo(right.Key));
        }

        /// <summary>
        /// Adds the instance a component overrides, and for <c>RANGE=THISANDFUTURE</c> the shift it gives every later instance.
        /// </summary>
        private void AddOverride(TComponent component, IcalDateOrDateTime recurrenceId, bool isRange)
        {
            var original = ToInstant(recurrenceId);
            var start = ToInstant(component.GetDateOrDateTime(IcalPropertyNames.DtStart) ?? recurrenceId);
            var endRule = GetEndRule(component, start);
            _overriddenKeys.Add(original.Key);
            if (start.Key >= _fromKey && start.Key < _toKey)
            {
                var occurrence = CreateOccurrence(component, !ReferenceEquals(component, _component), recurrenceId, start, endRule);
                var index = FindInsertionIndex(_overrideKeys, start.Key);
                _overrideKeys.Insert(index, start.Key);
                _overrides.Insert(index, occurrence);
            }

            if (!isRange) return;

            // ref. RFC 5545§3.8.4.4: a change of start is applied to every later instance as the same shift, here in the local time of
            // DTSTART so that it keeps its time of day across a daylight saving change.
            var shift = GetWallClockTicks(ToFrame(start)) - GetWallClockTicks(ToFrame(original));
            if (_start.Value.IsDate)
            {
                shift = shift / TicksPerDay * TicksPerDay;
            }

            _ranges.Add(new RangeOverride(original.Key, component, shift, endRule));
            _margin = Math.Max(_margin, ClockMargin + Math.Abs(shift));
        }

        private void AddRuleSources(IcalOptions options, bool hasTo)
        {
            var rules = _component.GetRecurrenceRules(IcalPropertyNames.RecurrenceRule);
            if (rules.Count == 0) return;

            // The rules are applied in local time, so their window is widened by the most an instance can move.
            var fromTicks = _fromKey == long.MinValue ? (long?)null : Clamp(_fromKey - _margin);
            var toTicks = hasTo ? Clamp(_toKey + _margin) : (long?)null;
            foreach (var rule in rules)
            {
                // ref. RFC 5545§3.3.10: DTSTART always counts as the first occurrence, so where the rule does not give it, the rule gives one
                // fewer.  Both forms are created here, so that a rule that is not valid throws now.
                var count = rule.Count ?? 0;
                var shortened = count > 1 ? rule.WithCount(count - 1) : rule;
                if (_start.Value.IsDate)
                {
                    var start = _start.Value.Date;
                    var from = fromTicks.HasValue ? GetDate(fromTicks.Value / TicksPerDay) : (IcalDate?)null;
                    var to = toTicks.HasValue ? GetDate(toTicks.Value / TicksPerDay) : (IcalDate?)null;
                    var dates = rule.GetOccurrences(start, from, to, options);
                    var shortenedDates = count == 0 ? dates : count == 1 ? new IcalDate[0] : shortened.GetOccurrences(start, from, to, options);
                    _sources.Add(ToInstances(SelectDates(rule, start, dates, shortenedDates, options)));
                }
                else
                {
                    var start = _start.Value.DateTime;
                    var from = fromTicks.HasValue ? GetDateTime(fromTicks.Value, IcalDateTimeKind.Floating, null) : (IcalDateTime?)null;
                    var to = toTicks.HasValue ? GetDateTime(toTicks.Value, IcalDateTimeKind.Floating, null) : (IcalDateTime?)null;
                    var dateTimes = rule.GetOccurrences(start, from, to, ToUtcForUntil, options);
                    var shortenedDateTimes = count == 0
                        ? dateTimes
                        : count == 1
                            ? new IcalDateTime[0]
                            : shortened.GetOccurrences(start, from, to, ToUtcForUntil, options);
                    _sources.Add(ToInstances(SelectDateTimes(rule, start, dateTimes, shortenedDateTimes, options)));
                }
            }
        }

        /// <summary>
        /// Gives the occurrences of a rule from an all-day start, or those of the rule with one fewer COUNT if the rule does not give the start.
        /// </summary>
        private static IEnumerable<IcalDate> SelectDates(IcalRecurrenceRule rule, IcalDate start, IEnumerable<IcalDate> dates,
            IEnumerable<IcalDate> shortenedDates, IcalOptions options)
        {
            var givesStart = false;
            foreach (var date in rule.GetOccurrences(start, null, GetDate(start.DayNumber + 1L), options))
            {
                givesStart = date == start;
            }

            foreach (var date in givesStart ? dates : shortenedDates)
            {
                yield return date;
            }
        }

        /// <summary>
        /// Gives the occurrences of a rule from a start, or those of the rule with one fewer COUNT if the rule does not give the start.
        /// </summary>
        private IEnumerable<IcalDateTime> SelectDateTimes(IcalRecurrenceRule rule, IcalDateTime start, IEnumerable<IcalDateTime> dateTimes,
            IEnumerable<IcalDateTime> shortenedDateTimes, IcalOptions options)
        {
            var givesStart = false;
            var next = GetDateTime(GetWallClockTicks(start) + TicksPerSecond, IcalDateTimeKind.Floating, null);
            foreach (var dateTime in rule.GetOccurrences(start, null, next, ToUtcForUntil, options))
            {
                givesStart = IcalDateTime.CompareClock(dateTime, start) == 0;
            }

            foreach (var dateTime in givesStart ? dateTimes : shortenedDateTimes)
            {
                yield return dateTime;
            }
        }

        /// <summary>
        /// Reads the RDATE properties, in order of their keys.  A PERIOD gives the instance its own end.
        /// </summary>
        private void AddRecurrenceDateSource()
        {
            var instances = new List<Instance>();
            foreach (var property in _component.GetProperties(IcalPropertyNames.RecurrenceDates))
            {
                var valueType = IcalPropertyValue.GetValueType(property);
                foreach (var item in property.Value.Split(','))
                {
                    if (valueType == IcalValueType.Period)
                    {
                        IcalPeriod period;
                        var timeZoneId = IcalPropertyValue.GetParameterValue(property, IcalParameterNames.TimeZoneId);
                        if (timeZoneId?.Length == 0 || !IcalPeriod.TryParse(item.AsSpan(), timeZoneId, IcalValueFormat.Text, out period))
                            throw IcalPropertyValue.BuildInvalidValueException(property, valueType);

                        var start = ToInstant(period.Start);
                        var endRule = period.End.HasValue
                            ? EndRule.Fixed(ToInstant(period.End.Value))
                            : EndRule.Nominal(period.Duration ?? default(IcalDuration));
                        instances.Add(new Instance(start, endRule));
                        continue;
                    }

                    IcalDateOrDateTime value;
                    if (!IcalPropertyValue.TryParseDateOrDateTime(property, item.AsSpan(), out value))
                        throw IcalPropertyValue.BuildInvalidValueException(property, valueType);

                    instances.Add(new Instance(ToInstant(value), EndRule.Inherit));
                }
            }

            if (instances.Count == 0) return;

            // A stable sort, so that of two RDATE values with the same key the first is kept.
            var sorted = new List<Instance>(instances.Count);
            foreach (var instance in instances)
            {
                var index = sorted.Count;
                while (index > 0 && sorted[index - 1].Start.Key > instance.Start.Key)
                {
                    index--;
                }

                sorted.Insert(index, instance);
            }

            _sources.Add(sorted);
        }

        #endregion

        #region Expansion

        private IEnumerable<IcalOccurrence<TComponent>> Expand()
        {
            var pending = new List<IcalOccurrence<TComponent>>();
            var pendingKeys = new List<long>();
            var overrideIndex = 0;
            var seen = new HashSet<long>();
            var seenOrder = new Queue<long>();
            foreach (var instance in Merge())
            {
                var key = instance.Start.Key;
                if (_toKey != long.MaxValue && key >= _toKey + _margin) break;

                // ref. RFC 5545§3.8.5: an instance that DTSTART, a rule and an RDATE all give is one instance.
                while (seenOrder.Count > 0 && seenOrder.Peek() < key - 2 * _margin)
                {
                    seen.Remove(seenOrder.Dequeue());
                }

                if (!seen.Add(key)) continue;

                seenOrder.Enqueue(key);
                if (!IsExcluded(instance.Start) && !_overriddenKeys.Contains(key))
                {
                    var occurrence = CreateOccurrence(instance);
                    var occurrenceKey = GetKey(occurrence);
                    if (occurrenceKey >= _fromKey && occurrenceKey < _toKey)
                    {
                        var index = FindInsertionIndex(pendingKeys, occurrenceKey);
                        pendingKeys.Insert(index, occurrenceKey);
                        pending.Insert(index, occurrence);
                    }
                }

                // No later instance can start before this one less the margin.
                var limit = key - _margin;
                while (true)
                {
                    var next = TakeNext(pending, pendingKeys, ref overrideIndex, limit);
                    if (next is null) break;

                    yield return next;
                }
            }

            while (true)
            {
                var next = TakeNext(pending, pendingKeys, ref overrideIndex, long.MaxValue);
                if (next is null) break;

                yield return next;
            }
        }

        /// <summary>
        /// Takes the earliest instance waiting to be given, if it starts before a limit: either one generated, or one an override gives.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalOccurrence<TComponent>? TakeNext(List<IcalOccurrence<TComponent>> pending, List<long> pendingKeys, ref int overrideIndex,
            long limit)
#else
        private IcalOccurrence<TComponent> TakeNext(List<IcalOccurrence<TComponent>> pending, List<long> pendingKeys, ref int overrideIndex,
            long limit)
#endif
        {
            var hasPending = pending.Count > 0 && pendingKeys[0] < limit;
            var hasOverride = overrideIndex < _overrides.Count && _overrideKeys[overrideIndex] < limit;
            if (hasOverride && (!hasPending || _overrideKeys[overrideIndex] < pendingKeys[0]))
            {
                return _overrides[overrideIndex++];
            }

            if (!hasPending) return null;

            var occurrence = pending[0];
            pending.RemoveAt(0);
            pendingKeys.RemoveAt(0);
            return occurrence;
        }

        /// <summary>
        /// Merges the sources of instances, taking the one with the earliest key each time.
        /// </summary>
        private IEnumerable<Instance> Merge()
        {
            var active = new List<IEnumerator<Instance>>();
            foreach (var source in _sources)
            {
                var enumerator = source.GetEnumerator();
                if (enumerator.MoveNext())
                {
                    active.Add(enumerator);
                }
            }

            while (active.Count > 0)
            {
                var earliest = 0;
                for (var index = 1; index < active.Count; index++)
                {
                    if (active[index].Current.Start.Key < active[earliest].Current.Start.Key)
                    {
                        earliest = index;
                    }
                }

                yield return active[earliest].Current;
                if (!active[earliest].MoveNext())
                {
                    active.RemoveAt(earliest);
                }
            }
        }

        private IEnumerable<Instance> ToInstances(IEnumerable<IcalDate> dates)
        {
            foreach (var date in dates)
            {
                yield return new Instance(ToInstant(date), EndRule.Inherit);
            }
        }

        private IEnumerable<Instance> ToInstances(IEnumerable<IcalDateTime> dateTimes)
        {
            foreach (var dateTime in dateTimes)
            {
                yield return new Instance(ToInstant(dateTime), EndRule.Inherit);
            }
        }

        /// <summary>
        /// Creates the occurrence of an instance that no component overrides by its own RECURRENCE-ID, shifted by the latest
        /// <c>RANGE=THISANDFUTURE</c> override before it.
        /// </summary>
        private IcalOccurrence<TComponent> CreateOccurrence(Instance instance)
        {
            var original = instance.Start;
            var index = FindInsertionIndex(_ranges, original.Key) - 1;
            if (index < 0)
                return CreateOccurrence(_component, false, original.Value, original,
                    ReferenceEquals(instance.EndRule, EndRule.Inherit) ? _endRule : instance.EndRule);

            var range = _ranges[index];
            var framed = ToFrame(original);
            var shifted = _start.Value.IsDate
                ? new IcalDateOrDateTime(GetDate((GetWallClockTicks(framed) + range.Shift) / TicksPerDay))
                : new IcalDateOrDateTime(GetDateTime(GetWallClockTicks(framed) + range.Shift, _start.Value.DateTime.Kind,
                    _start.Value.DateTime.TimeZoneId));
            return CreateOccurrence(range.Component, true, original.Value, ToInstant(shifted), range.EndRule);
        }

        private IcalOccurrence<TComponent> CreateOccurrence(TComponent component, bool isOverride, IcalDateOrDateTime recurrenceId, Instant start,
            EndRule endRule)
        {
            var end = GetEnd(endRule, start);
            return new IcalOccurrence<TComponent>(component, isOverride, recurrenceId, start.Value, start.Utc,
                end.HasValue ? end.Value.Value : (IcalDateOrDateTime?)null, end.HasValue ? end.Value.Utc : null);
        }

        private static long GetKey(IcalOccurrence<TComponent> occurrence)
        {
            return occurrence.StartUtc.HasValue ? GetWallClockTicks(occurrence.StartUtc.Value) : GetWallClockTicks(occurrence.Start);
        }

        #endregion

        #region End rules

        /// <summary>
        /// Gets how a component's instances end, from the start of the component.
        /// </summary>
        private EndRule GetEndRule(IcalComponent component, Instant start)
        {
            IcalDateOrDateTime? end;
            if (component is IcalEvent)
            {
                end = component.GetDateOrDateTime(IcalPropertyNames.DtEnd);
            }
            else if (component is IcalTodo)
            {
                end = component.GetDateOrDateTime(IcalPropertyNames.Due);
            }
            else
            {
                return EndRule.None;
            }

            // ref. RFC 5545§3.8.5.3: an end given by DTEND or DUE gives every instance the same exact duration; one given by DURATION, the
            // same nominal duration.
            if (end.HasValue) return GetExactEndRule(start, end.Value);

            var duration = component.GetDuration(IcalPropertyNames.Duration);
            if (duration.HasValue) return EndRule.Nominal(duration.Value);
            if (component is IcalTodo) return EndRule.None;

            // ref. RFC 5545§3.6.1: an event with neither ends a day after a DATE start, and at a DATE-TIME start.
            return start.Value.IsDate ? EndRule.Nominal(new IcalDuration(1, TimeSpan.Zero)) : EndRule.Same;
        }

        private EndRule GetExactEndRule(Instant start, IcalDateOrDateTime end)
        {
            if (start.Value.IsDate) return EndRule.Nominal(new IcalDuration(end.Date.DayNumber - start.Value.Date.DayNumber, TimeSpan.Zero));

            var ticks = ToInstant(end).Key - start.Key;
            var kind = end.IsDate ? IcalDateTimeKind.Floating : end.DateTime.Kind;
            var timeZoneId = end.IsDate ? null : end.DateTime.TimeZoneId;
            var startDateTime = start.Value.DateTime;
            if (ticks == 0 && kind == startDateTime.Kind && string.Equals(timeZoneId, startDateTime.TimeZoneId, StringComparison.Ordinal))
                return EndRule.Same;

            return EndRule.Exact(ticks, kind, timeZoneId);
        }

        /// <summary>
        /// Gets the end of an instance; or <c>null</c> if it has none.
        /// </summary>
        private Instant? GetEnd(EndRule rule, Instant start)
        {
            switch (rule.Kind)
            {
                case EndRuleKind.None:
                    return null;
                case EndRuleKind.Same:
                    return start;
                case EndRuleKind.Fixed:
                    return rule.End;
                case EndRuleKind.Nominal:
                    return AddNominal(start, rule.Duration);
                default:
                    return AddExact(start, rule);
            }
        }

        /// <summary>
        /// Adds a nominal duration: its weeks and days to the local date, keeping the time of day, then its exact time to the instant.
        /// </summary>
        private Instant AddNominal(Instant start, IcalDuration duration)
        {
            var days = duration.Weeks * 7L + duration.Days;
            var exactTicks = duration.Time.Ticks;
            if (start.Value.IsDate) return ToInstant(GetDate(start.Value.Date.DayNumber + days + exactTicks / TicksPerDay));

            var dateTime = start.Value.DateTime;
            var middle = GetDateTime(GetWallClockTicks(dateTime) + days * TicksPerDay, dateTime.Kind, dateTime.TimeZoneId);
            if (exactTicks == 0) return ToInstant(middle);
            if (dateTime.Kind != IcalDateTimeKind.Zoned) return ToInstant(GetDateTime(GetWallClockTicks(middle) + exactTicks, dateTime.Kind, null));

            // A zoned time is resolved before the exact time is added, so that the exact time spans any change of offset.
            var timeZoneId = dateTime.TimeZoneId ?? string.Empty;
            var rules = GetRules(timeZoneId);
            return FromUtc(rules, timeZoneId, GetWallClockTicks(IcalTimeZoneResolver.ToUtc(rules, middle)) + exactTicks);
        }

        /// <summary>
        /// Adds an exact duration to the instant of the start, giving the end in the kind and time zone of the component's end.
        /// </summary>
        private Instant AddExact(Instant start, EndRule rule)
        {
            if (start.Value.IsDate) return ToInstant(GetDate(start.Value.Date.DayNumber + rule.Ticks / TicksPerDay));
            if (!start.Utc.HasValue || rule.EndKind == IcalDateTimeKind.Floating)
                return ToInstant(GetDateTime(GetWallClockTicks(start.Value.DateTime) + rule.Ticks, IcalDateTimeKind.Floating, null));

            var endTicks = GetWallClockTicks(start.Utc.Value) + rule.Ticks;
            if (rule.EndKind == IcalDateTimeKind.Utc) return ToInstant(GetDateTime(endTicks, IcalDateTimeKind.Utc, null));

            var timeZoneId = rule.TimeZoneId ?? string.Empty;
            return FromUtc(GetRules(timeZoneId), timeZoneId, endTicks);
        }

        #endregion

        #region Time

        /// <summary>
        /// Gets the instant of a value, resolving its time zone if it is zoned.
        /// </summary>
        private Instant ToInstant(IcalDateOrDateTime value)
        {
            if (value.IsDate) return new Instant(value, (long)value.Date.DayNumber * TicksPerDay, null);

            var dateTime = value.DateTime;
            switch (dateTime.Kind)
            {
                case IcalDateTimeKind.Utc:
                    return new Instant(value, GetWallClockTicks(dateTime), dateTime);
                case IcalDateTimeKind.Floating:
                    return new Instant(value, GetWallClockTicks(dateTime), null);
                default:
                    var utc = IcalTimeZoneResolver.ToUtc(GetRules(dateTime.TimeZoneId ?? string.Empty), dateTime);
                    return new Instant(value, GetWallClockTicks(utc), utc);
            }
        }

        private static Instant FromUtc(IcalTimeZoneRules rules, string timeZoneId, long utcTicks)
        {
            var utc = GetDateTime(utcTicks, IcalDateTimeKind.Utc, null);
            var local = IcalDateTime.FromDateTime(rules.ToLocal(new DateTime(utcTicks, DateTimeKind.Utc)), timeZoneId);
            return new Instant(local, utcTicks, utc);
        }

        /// <summary>
        /// Gets a value in the kind and time zone of DTSTART: a DATE for an all-day start; otherwise a DATE-TIME, converted through UTC where
        /// both have an instant.
        /// </summary>
        private IcalDateOrDateTime ToFrame(Instant instant)
        {
            var value = instant.Value;
            if (_start.Value.IsDate) return value.IsDate ? value : new IcalDateOrDateTime(value.Date);

            var start = _start.Value.DateTime;
            var ticks = GetWallClockTicks(value);
            if (!value.IsDate && instant.Utc.HasValue)
            {
                var dateTime = value.DateTime;
                if (start.Kind == IcalDateTimeKind.Zoned && dateTime.Kind == IcalDateTimeKind.Zoned &&
                    string.Equals(start.TimeZoneId, dateTime.TimeZoneId, StringComparison.Ordinal)) return value;

                if (start.Kind == IcalDateTimeKind.Utc) return instant.Utc.Value;
                if (start.Kind == IcalDateTimeKind.Zoned)
                {
                    var timeZoneId = start.TimeZoneId ?? string.Empty;
                    return FromUtc(GetRules(timeZoneId), timeZoneId, instant.Key).Value;
                }
            }

            return GetDateTime(ticks, start.Kind, start.TimeZoneId);
        }

        /// <summary>
        /// Converts an occurrence of a rule to UTC, to compare it with a UNTIL in UTC.  A floating time is read as though it were in UTC.
        /// </summary>
        private IcalDateTime ToUtcForUntil(IcalDateTime value)
        {
            return value.Kind == IcalDateTimeKind.Zoned
                ? IcalTimeZoneResolver.ToUtc(GetRules(value.TimeZoneId ?? string.Empty), value)
                : GetDateTime(GetWallClockTicks(value), IcalDateTimeKind.Utc, null);
        }

        private bool IsExcluded(Instant instant)
        {
            return _excludedKeys.Contains(instant.Key) || _excludedDays.Count > 0 && _excludedDays.Contains(ToFrame(instant).Date.DayNumber);
        }

        private IcalTimeZoneRules GetRules(string timeZoneId)
        {
            if (_rules.TryGetValue(timeZoneId, out var cached)) return cached;

            var rules = _resolver.Resolve(timeZoneId, _calendar);
            _rules.Add(timeZoneId, rules);
            return rules;
        }

        private static long GetWallClockTicks(IcalDateOrDateTime value)
        {
            return value.IsDate ? (long)value.Date.DayNumber * TicksPerDay : GetWallClockTicks(value.DateTime);
        }

        private static long GetWallClockTicks(IcalDateTime value)
        {
            var time = value.Time;
            return (long)value.Date.DayNumber * TicksPerDay + (time.Hour * 3600L + time.Minute * 60 + time.Second) * TicksPerSecond;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static IcalDateTime GetDateTime(long ticks, IcalDateTimeKind kind, string? timeZoneId)
#else
        private static IcalDateTime GetDateTime(long ticks, IcalDateTimeKind kind, string timeZoneId)
#endif
        {
            var date = GetDate(ticks < 0 ? -1 : ticks / TicksPerDay);
            var secondOfDay = (int)(ticks % TicksPerDay / TicksPerSecond);
            var time = new IcalTime(secondOfDay / 3600, secondOfDay / 60 % 60, secondOfDay % 60, kind == IcalDateTimeKind.Utc);
            return kind == IcalDateTimeKind.Zoned ? new IcalDateTime(date, time, timeZoneId ?? string.Empty) : new IcalDateTime(date, time);
        }

        private static IcalDate GetDate(long dayNumber)
        {
            if (dayNumber < 0 || dayNumber > IcalDate.MaxDayNumber)
                throw new ArgumentOutOfRangeException(nameof(dayNumber), dayNumber,
                    "An occurrence falls outside the range of dates, 0001-01-01 to 9999-12-31.");

            return IcalDate.FromDayNumber((int)dayNumber);
        }

        private static long Clamp(long ticks)
        {
            return ticks < 0 ? 0 : ticks >= EndTicks ? EndTicks - TicksPerSecond : ticks;
        }

        /// <summary>
        /// Gets the index after the last key no greater than a key, in a list sorted by key.
        /// </summary>
        private static int FindInsertionIndex(List<long> keys, long key)
        {
            int low = 0, high = keys.Count;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                if (keys[middle] <= key)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        /// <summary>
        /// Gets the index after the last override whose instance is before a key, in the list sorted by key.
        /// </summary>
        private static int FindInsertionIndex(List<RangeOverride> ranges, long key)
        {
            int low = 0, high = ranges.Count;
            while (low < high)
            {
                var middle = low + (high - low) / 2;
                if (ranges[middle].Key < key)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        #endregion

        /// <summary>
        /// A value with its key: its instant in UTC if it has one, otherwise its wall-clock time.
        /// </summary>
        private readonly struct Instant
        {
            internal Instant(IcalDateOrDateTime value, long key, IcalDateTime? utc)
            {
                Value = value;
                Key = key;
                Utc = utc;
            }

            internal IcalDateOrDateTime Value { get; }

            internal long Key { get; }

            internal IcalDateTime? Utc { get; }
        }

        /// <summary>
        /// An instance of the recurrence set before overrides, with the end of an RDATE PERIOD.
        /// </summary>
        private readonly struct Instance
        {
            internal Instance(Instant start, EndRule endRule)
            {
                Start = start;
                EndRule = endRule;
            }

            internal Instant Start { get; }

            /// <summary>
            /// Gets how the instance ends; <see cref="EndRule.Inherit"/> for the end of the component.
            /// </summary>
            internal EndRule EndRule { get; }
        }

        private readonly struct Candidate
        {
            internal Candidate(TComponent component, IcalDateOrDateTime recurrenceId, int sequence)
            {
                Component = component;
                RecurrenceId = recurrenceId;
                Sequence = sequence;
            }

            internal TComponent Component { get; }

            internal IcalDateOrDateTime RecurrenceId { get; }

            internal int Sequence { get; }
        }

        /// <summary>
        /// An override with <c>RANGE=THISANDFUTURE</c>: the key of the instance it names, and the shift and end it gives later instances.
        /// </summary>
        private readonly struct RangeOverride
        {
            internal RangeOverride(long key, TComponent component, long shift, EndRule endRule)
            {
                Key = key;
                Component = component;
                Shift = shift;
                EndRule = endRule;
            }

            internal long Key { get; }

            internal TComponent Component { get; }

            /// <summary>
            /// Gets the shift, in ticks of local time.
            /// </summary>
            internal long Shift { get; }

            internal EndRule EndRule { get; }
        }

        private enum EndRuleKind
        {
            None,
            Same,
            Fixed,
            Nominal,
            Exact
        }

        /// <summary>
        /// How an instance ends: not at all, at its start, at a fixed value, or after a nominal or exact duration.
        /// </summary>
        private sealed class EndRule
        {
            internal static readonly EndRule None = new EndRule(EndRuleKind.None);
            internal static readonly EndRule Same = new EndRule(EndRuleKind.Same);

            /// <summary>
            /// Stands for the end of the component, for an instance that has none of its own.
            /// </summary>
            internal static readonly EndRule Inherit = new EndRule(EndRuleKind.None);

            private EndRule(EndRuleKind kind)
            {
                Kind = kind;
            }

            internal EndRuleKind Kind { get; }

            internal IcalDuration Duration { get; private set; }

            internal long Ticks { get; private set; }

            internal IcalDateTimeKind EndKind { get; private set; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal string? TimeZoneId { get; private set; }
#else
            internal string TimeZoneId { get; private set; }
#endif

            internal Instant End { get; private set; }

            internal static EndRule Fixed(Instant end)
            {
                return new EndRule(EndRuleKind.Fixed) { End = end };
            }

            internal static EndRule Nominal(IcalDuration duration)
            {
                return new EndRule(EndRuleKind.Nominal) { Duration = duration };
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal static EndRule Exact(long ticks, IcalDateTimeKind endKind, string? timeZoneId)
#else
            internal static EndRule Exact(long ticks, IcalDateTimeKind endKind, string timeZoneId)
#endif
            {
                return new EndRule(EndRuleKind.Exact) { Ticks = ticks, EndKind = endKind, TimeZoneId = timeZoneId };
            }
        }
    }
}
