using System;
using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Calculates the offsets from universal time in effect in a TZ Database zone, and the transitions between them, from its zone lines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The calculation follows the <c>outzone</c> function of zic. The zone lines are walked in order, the rule transitions of
    /// each rule-based line are found year by year, and their wall, standard and universal times are resolved against the
    /// offset in effect immediately before them. The time at which a line begins takes the offset of the latest rule in
    /// effect at that moment, and transitions that change nothing are dropped.
    /// </para>
    /// <para>
    /// Transitions up to the last year for which the zone has explicit data are precomputed. After that, the rules of the
    /// last zone line that apply indefinitely are evaluated on demand, up to the end of the range of <see cref="DateTime"/>.
    /// All instants are held as ticks of universal time, so that a calculation near either end of that range cannot overflow.
    /// </para>
    /// </remarks>
    internal sealed class TzDataZoneCalculator
    {
        private const int YearMinimum = 1;
        private const int YearMaximum = 9999;
        private const long TicksMinimum = 0;
        private static readonly long TicksMaximum = DateTime.MaxValue.Ticks;

        private readonly TzDataZoneLine[] _zoneLines;
        private readonly long[] _zoneLineStarts;
        private readonly State _initialState;
        private readonly Change[] _changes;

        // The rules that apply indefinitely, evaluated on demand from _tailFirstYear, or null where there are none.
#if NETCOREAPP3_0_OR_GREATER
        private readonly TzDataRule[]? _tailRules;
#else
        [CanBeNull]
        private readonly TzDataRule[] _tailRules;
#endif
        private readonly int _tailFirstYear;
        private readonly State _tailEntryState;
        private readonly long _tailFirstTicks;

        internal TzDataZoneCalculator(
#if NETSTANDARD
            [NotNull]
#endif
            TzDataZoneLine[] zoneLines)
        {
            if (zoneLines is null)
                throw new ArgumentNullException(nameof(zoneLines));

            if (zoneLines.Length == 0)
                throw new ArgumentException("A zone has at least one zone line.", nameof(zoneLines));

            _zoneLines = zoneLines;

            var lastZoneLine = zoneLines[zoneLines.Length - 1];
            var lastExplicitYear = GetLastExplicitYear(zoneLines);
            var tailRules = GetTailRules(lastZoneLine);
            // Where every rule that applies indefinitely leaves the same offset in effect, the year after the last explicit year changes the offset at most once, and nothing changes after it.
            var precomputedLastYear = tailRules is null ? Math.Min(lastExplicitYear + 1, YearMaximum) : lastExplicitYear;

            _zoneLineStarts = new long[zoneLines.Length];
            var rawChanges = ComputeChanges(zoneLines, precomputedLastYear, _zoneLineStarts, out var initialState);
            _initialState = initialState;
            _changes = Optimize(rawChanges, initialState);

            _tailFirstTicks = long.MaxValue;
            _tailEntryState = _changes.Length > 0 ? _changes[_changes.Length - 1].State : initialState;
            if (tailRules != null && precomputedLastYear < YearMaximum)
            {
                _tailRules = tailRules;
                _tailFirstYear = precomputedLastYear + 1;

                using (var tailChanges = GetTailChanges(_tailFirstYear).GetEnumerator())
                {
                    if (tailChanges.MoveNext())
                        _tailFirstTicks = tailChanges.Current.Instant;
                    else
                        _tailRules = null;
                }
            }
        }

        /// <summary>
        /// Gets the zone line in force at an instant.
        /// </summary>
        /// <param name="utc">The instant, in universal time.</param>
#if NETSTANDARD
        [NotNull]
#endif
        internal TzDataZoneLine GetZoneLine(DateTime utc)
        {
            var ticks = utc.Ticks;

            // The first zone line applies from the beginning of time, so the search finds the last line that starts at or before the instant.
            var lower = 0;
            var upper = _zoneLineStarts.Length - 1;
            while (lower < upper)
            {
                var middle = lower + (upper - lower + 1) / 2;
                if (_zoneLineStarts[middle] <= ticks)
                    lower = middle;
                else
                    upper = middle - 1;
            }

            return _zoneLines[lower];
        }

        /// <summary>
        /// Gets the offset in effect at an instant, and the period over which it is in effect.
        /// </summary>
        /// <param name="utc">The instant, in universal time.</param>
        internal TzDataOffsetInfo GetOffsetInfo(DateTime utc)
        {
            var ticks = utc.Ticks;

            using (var changes = EnumerateChangesFrom(ticks).GetEnumerator())
            {
                changes.MoveNext();
                var current = changes.Current;
                var next = changes.MoveNext() ? changes.Current : null;
                return CreateOffsetInfo(current, next);
            }
        }

        /// <summary>
        /// Gets the transitions that occur from one instant up to, but not including, another.
        /// </summary>
        /// <param name="fromUtc">The instant from which to include transitions, in universal time.</param>
        /// <param name="toUtc">The instant before which to include transitions, in universal time.</param>
        internal IEnumerable<TzDataTransition> GetTransitions(DateTime fromUtc, DateTime toUtc)
        {
            var fromTicks = fromUtc.Ticks;
            var toTicks = toUtc.Ticks;

            // The changes begin with the one in effect immediately before the first instant, which gives the offset before the first transition.
#if NETCOREAPP3_0_OR_GREATER
            Change? previous = null;
            Change? current = null;
#else
            Change previous = null;
            Change current = null;
#endif
            foreach (var next in EnumerateChangesFrom(fromTicks - 1))
            {
                if (current != null && previous != null && current.Instant >= fromTicks)
                    yield return new TzDataTransition(ToUtc(current.Instant), CreateOffsetInfo(previous, current), CreateOffsetInfo(current, next));

                if (next.Instant >= toTicks)
                    yield break;

                previous = current;
                current = next;
            }

            if (current != null && previous != null && current.Instant >= fromTicks)
                yield return new TzDataTransition(ToUtc(current.Instant), CreateOffsetInfo(previous, current), CreateOffsetInfo(current, null));
        }

        /// <summary>
        /// Formats a zone abbreviation, as zic does.
        /// </summary>
        /// <param name="format">The FORMAT column of the zone line.</param>
        /// <param name="letter">The LETTER column of the rule in effect, or <see langword="null"/> where none is known.</param>
        /// <param name="isDaylightSavingTime">Whether daylight saving time is in effect.</param>
        /// <param name="utcOffset">The total offset from universal time in effect.</param>
#if NETSTANDARD
        [NotNull]
#endif
        internal static string FormatAbbreviation(
#if NETSTANDARD
            [NotNull]
#endif
            string format,
#if NETCOREAPP3_0_OR_GREATER
            string? letter,
#else
            [CanBeNull] string letter,
#endif
            bool isDaylightSavingTime,
            TimeSpan utcOffset)
        {
            var slashIndex = format.IndexOf('/');
            if (slashIndex >= 0)
                return isDaylightSavingTime ? format.Substring(slashIndex + 1) : format.Substring(0, slashIndex);

            if (format.IndexOf("%z", StringComparison.Ordinal) >= 0)
                return format.Replace("%z", FormatNumericOffset(utcOffset));

            return format.Replace("%s", letter ?? string.Empty);
        }

        private static string FormatNumericOffset(TimeSpan utcOffset)
        {
            // The shortest of +hh, +hhmm and +hhmmss that represents the offset exactly.
            var sign = utcOffset < TimeSpan.Zero ? '-' : '+';
            var totalSeconds = Math.Abs((long)utcOffset.TotalSeconds);
            var hours = totalSeconds / 3600;
            var minutes = totalSeconds / 60 % 60;
            var seconds = totalSeconds % 60;

            if (seconds != 0)
                return $"{sign}{hours:00}{minutes:00}{seconds:00}";

            if (minutes != 0)
                return $"{sign}{hours:00}{minutes:00}";

            return $"{sign}{hours:00}";
        }

        private static int GetLastExplicitYear(TzDataZoneLine[] zoneLines)
        {
            var lastYear = YearMinimum;

            foreach (var zoneLine in zoneLines)
            {
                if (zoneLine.Until.HasValue)
                    lastYear = Math.Max(lastYear, zoneLine.Until.Value.Year);

                foreach (var rule in zoneLine.Rules)
                {
                    lastYear = Math.Max(lastYear, rule.FromYear);
                    if (rule.ToYear != int.MaxValue)
                        lastYear = Math.Max(lastYear, rule.ToYear);
                }
            }

            return Math.Min(lastYear, YearMaximum);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static TzDataRule[]? GetTailRules(TzDataZoneLine lastZoneLine)
#else
        [CanBeNull]
        private static TzDataRule[] GetTailRules(TzDataZoneLine lastZoneLine)
#endif
        {
            var tailRules = new List<TzDataRule>();
            foreach (var rule in lastZoneLine.Rules)
            {
                if (rule.ToYear == int.MaxValue)
                    tailRules.Add(rule);
            }

            // Rules that all leave the same offset in effect never change it after the first of them, so they need no evaluation on demand.
            // Otherwise, every year changes the offset at least twice.
            var standardOffset = lastZoneLine.StandardOffset.Ticks;
            var firstState = tailRules.Count > 0 ? CreateState(lastZoneLine, tailRules[0].Letter, standardOffset, tailRules[0].Save.Ticks, tailRules[0].IsDaylight) : null;
            for (var index = 1; index < tailRules.Count; index++)
            {
                if (!CreateState(lastZoneLine, tailRules[index].Letter, standardOffset, tailRules[index].Save.Ticks, tailRules[index].IsDaylight).Equals(firstState))
                    return tailRules.ToArray();
            }

            return null;
        }

        private static List<Change> ComputeChanges(TzDataZoneLine[] zoneLines, int lastYear, long[] zoneLineStarts, out State initialState)
        {
            var changes = new List<Change>();
#if NETCOREAPP3_0_OR_GREATER
            State? defaultState = null;
#else
            State defaultState = null;
#endif
            var startTicks = long.MinValue;
            zoneLineStarts[0] = long.MinValue;

            for (var lineIndex = 0; lineIndex < zoneLines.Length; lineIndex++)
            {
                var zoneLine = zoneLines[lineIndex];
                var standardOffset = zoneLine.StandardOffset.Ticks;
                var useStart = lineIndex > 0;
                var until = zoneLine.Until;
                var save = 0L;
                var startOffset = standardOffset;
#if NETCOREAPP3_0_OR_GREATER
                string? startAbbreviation = null;
#else
                string startAbbreviation = null;
#endif

                if (zoneLine.RuleKind != TzDataZoneRuleKind.RuleSet)
                {
                    save = zoneLine.FixedSave.GetValueOrDefault().Ticks;
                    var state = CreateState(zoneLine, null, standardOffset, save, save != 0);

                    if (useStart)
                        changes.Add(new Change(startTicks, state));
                    else
                        defaultState = state;
                }
                else
                {
                    var rules = zoneLine.Rules;
                    var firstYear = YearMaximum;
                    foreach (var rule in rules)
                        firstYear = Math.Min(firstYear, Math.Max(rule.FromYear, YearMinimum));
                    var finalYear = until.HasValue ? until.Value.Year : lastYear;

                    var pending = new long?[rules.Count];
                    for (var year = firstYear; year <= finalYear; year++)
                    {
                        for (var ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                            pending[ruleIndex] = GetRuleLocalTicks(rules[ruleIndex], year);

                        while (true)
                        {
                            var untilTicks = until.HasValue ? GetUntilTicks(until.Value, standardOffset, save) : long.MaxValue;

                            // The rule that takes effect earliest in the year, of those not yet applied.
                            var ruleFound = -1;
                            var ruleTicks = 0L;
                            for (var ruleIndex = 0; ruleIndex < rules.Count; ruleIndex++)
                            {
                                if (!pending[ruleIndex].HasValue)
                                    continue;

                                var candidateTicks = pending[ruleIndex].GetValueOrDefault() - GetReferenceOffset(rules[ruleIndex].AtTimeReference, standardOffset, save);
                                if (ruleFound < 0 || candidateTicks < ruleTicks)
                                {
                                    ruleFound = ruleIndex;
                                    ruleTicks = candidateTicks;
                                }
                            }

                            if (ruleFound < 0)
                                break;

                            var foundRule = rules[ruleFound];
                            var foundSave = foundRule.Save.Ticks;
                            pending[ruleFound] = null;

                            if (ruleTicks >= untilTicks)
                            {
                                if (startAbbreviation is null && standardOffset + foundSave == startOffset)
                                    startAbbreviation = FormatAbbreviation(zoneLine.Format, foundRule.Letter, foundRule.IsDaylight, new TimeSpan(standardOffset + foundSave));
                                break;
                            }

                            save = foundSave;

                            if (useStart && ruleTicks == startTicks)
                                useStart = false;

                            if (useStart)
                            {
                                // A rule that took effect before the line began gives the offset with which the line begins.
                                if (ruleTicks < startTicks)
                                {
                                    startOffset = standardOffset + save;
                                    startAbbreviation = FormatAbbreviation(zoneLine.Format, foundRule.Letter, foundRule.IsDaylight, new TimeSpan(startOffset));
                                    continue;
                                }

                                if (startAbbreviation is null && startOffset == standardOffset + save)
                                    startAbbreviation = FormatAbbreviation(zoneLine.Format, foundRule.Letter, foundRule.IsDaylight, new TimeSpan(startOffset));
                            }

                            var ruleState = CreateState(zoneLine, foundRule.Letter, standardOffset, foundSave, foundRule.IsDaylight);
                            if (defaultState is null && !foundRule.IsDaylight)
                                defaultState = ruleState;

                            changes.Add(new Change(ruleTicks, ruleState));
                        }
                    }

                    if (useStart)
                    {
                        var isDaylightSavingTime = startOffset != standardOffset;
                        var startState = new State(
                            zoneLine.StandardOffset,
                            new TimeSpan(startOffset - standardOffset),
                            isDaylightSavingTime,
                            startAbbreviation ?? FormatAbbreviation(zoneLine.Format, null, isDaylightSavingTime, new TimeSpan(startOffset)));
                        if (defaultState is null && !isDaylightSavingTime)
                            defaultState = startState;

                        changes.Add(new Change(startTicks, startState));
                    }
                }

                // The next line begins when this one ends, measured against the offset in effect at its end.
                if (until.HasValue)
                {
                    startTicks = GetUntilTicks(until.Value, standardOffset, save);
                    zoneLineStarts[lineIndex + 1] = startTicks;
                }
            }

            initialState = defaultState ?? (changes.Count > 0 ? changes[0].State : CreateState(zoneLines[0], null, zoneLines[0].StandardOffset.Ticks, 0, false));
            return changes;
        }

        private static Change[] Optimize(List<Change> rawChanges, State initialState)
        {
            // Changes are ordered by instant. Those at the same instant keep the order in which they were found, so that the later wins.
            var ordered = new List<KeyValuePair<int, Change>>(rawChanges.Count);
            for (var index = 0; index < rawChanges.Count; index++)
                ordered.Add(new KeyValuePair<int, Change>(index, rawChanges[index]));
            ordered.Sort((left, right) =>
            {
                var comparison = left.Value.Instant.CompareTo(right.Value.Instant);
                return comparison != 0 ? comparison : left.Key.CompareTo(right.Key);
            });

            var changes = new List<Change>(ordered.Count);
            foreach (var pair in ordered)
            {
                var change = pair.Value;

                if (changes.Count > 0)
                {
                    var previous = changes[changes.Count - 1];
                    var beforePrevious = changes.Count > 1 ? changes[changes.Count - 2].State : initialState;

                    // As zic does, a change that occurs no later on the local clock than the change before it replaces that change's offset, rather than following it.
                    if (change.Instant == previous.Instant ||
                        change.Instant + previous.State.UtcOffsetTicks <= previous.Instant + beforePrevious.UtcOffsetTicks)
                    {
                        changes[changes.Count - 1] = new Change(previous.Instant, change.State);
                        continue;
                    }
                }

                var current = changes.Count > 0 ? changes[changes.Count - 1].State : initialState;
                if (!change.State.Equals(current))
                    changes.Add(change);
            }

            // Replacing a change's offset can leave it the same as the offset before it.
            var result = new List<Change>(changes.Count);
            var state = initialState;
            foreach (var change in changes)
            {
                if (change.State.Equals(state))
                    continue;

                result.Add(change);
                state = change.State;
            }

            return result.ToArray();
        }

        /// <summary>
        /// Enumerates the changes in order, beginning with the one in effect at an instant.
        /// </summary>
        /// <remarks>
        /// The first change is the initial offset, with an instant of <see cref="long.MinValue"/>, where no change occurs at or before the instant.
        /// </remarks>
        private IEnumerable<Change> EnumerateChangesFrom(long ticks)
        {
            if (ticks < _tailFirstTicks)
            {
                var index = FindChange(ticks);
                if (index < 0)
                {
                    yield return new Change(long.MinValue, _initialState);
                    index = 0;
                }

                for (; index < _changes.Length; index++)
                    yield return _changes[index];

                if (_tailRules != null)
                {
                    foreach (var change in GetTailChanges(_tailFirstYear))
                        yield return change;
                }

                yield break;
            }

            // Each year of the evaluated rules has at least two changes, so starting two years early finds the change in effect at the instant.
            // Should it not, the evaluation starts again from the first evaluated year, whose first change is known to precede the instant.
            var startYear = Math.Max(_tailFirstYear, ToUtc(ticks).Year - 2);
            while (true)
            {
                using (var changes = GetTailChanges(startYear).GetEnumerator())
                {
                    if (changes.MoveNext() && changes.Current.Instant <= ticks)
                    {
                        var current = changes.Current;
                        while (changes.MoveNext())
                        {
                            if (changes.Current.Instant > ticks)
                            {
                                yield return current;
                                do
                                {
                                    yield return changes.Current;
                                } while (changes.MoveNext());

                                yield break;
                            }

                            current = changes.Current;
                        }

                        yield return current;
                        yield break;
                    }
                }

                if (startYear == _tailFirstYear)
                    throw new InvalidOperationException("No change is in effect at the instant.");

                startYear = _tailFirstYear;
            }
        }

        private int FindChange(long ticks)
        {
            // The index of the last change at or before the instant, or -1 where there is none.
            var lower = -1;
            var upper = _changes.Length - 1;
            while (lower < upper)
            {
                var middle = lower + (upper - lower + 1) / 2;
                if (_changes[middle].Instant <= ticks)
                    lower = middle;
                else
                    upper = middle - 1;
            }

            return lower;
        }

        private IEnumerable<Change> GetTailChanges(int startYear)
        {
            var tailRules = _tailRules;
            if (tailRules is null)
                yield break;

            var zoneLine = _zoneLines[_zoneLines.Length - 1];
            var standardOffset = zoneLine.StandardOffset.Ticks;
            var state = startYear == _tailFirstYear ? _tailEntryState : GetTailYearEndState(startYear - 1);
            var save = state.DaylightSavings.Ticks;
            var pending = new long?[tailRules.Length];

            for (var year = startYear; year <= YearMaximum; year++)
            {
                for (var ruleIndex = 0; ruleIndex < tailRules.Length; ruleIndex++)
                    pending[ruleIndex] = GetRuleLocalTicks(tailRules[ruleIndex], year);

                while (true)
                {
                    var ruleFound = -1;
                    var ruleTicks = 0L;
                    for (var ruleIndex = 0; ruleIndex < tailRules.Length; ruleIndex++)
                    {
                        if (!pending[ruleIndex].HasValue)
                            continue;

                        var candidateTicks = pending[ruleIndex].GetValueOrDefault() - GetReferenceOffset(tailRules[ruleIndex].AtTimeReference, standardOffset, save);
                        if (ruleFound < 0 || candidateTicks < ruleTicks)
                        {
                            ruleFound = ruleIndex;
                            ruleTicks = candidateTicks;
                        }
                    }

                    if (ruleFound < 0)
                        break;

                    pending[ruleFound] = null;

                    if (ruleTicks > TicksMaximum)
                        yield break;

                    var foundRule = tailRules[ruleFound];
                    save = foundRule.Save.Ticks;
                    var ruleState = CreateState(zoneLine, foundRule.Letter, standardOffset, save, foundRule.IsDaylight);
                    if (ruleState.Equals(state))
                        continue;

                    state = ruleState;
                    if (ruleTicks >= TicksMinimum)
                        yield return new Change(ruleTicks, ruleState);
                }
            }
        }

        private State GetTailYearEndState(int year)
        {
            // The rules that apply indefinitely fall months apart, so the last of them on the local clock is the last to take effect.
            var tailRules = _tailRules ?? new TzDataRule[0];
            var zoneLine = _zoneLines[_zoneLines.Length - 1];
#if NETCOREAPP3_0_OR_GREATER
            TzDataRule? lastRule = null;
#else
            TzDataRule lastRule = null;
#endif
            var lastTicks = long.MinValue;

            foreach (var rule in tailRules)
            {
                var localTicks = GetRuleLocalTicks(rule, year);
                if (localTicks.HasValue && (lastRule is null || localTicks.Value > lastTicks))
                {
                    lastRule = rule;
                    lastTicks = localTicks.Value;
                }
            }

            if (lastRule is null)
                return _tailEntryState;

            return CreateState(zoneLine, lastRule.Letter, zoneLine.StandardOffset.Ticks, lastRule.Save.Ticks, lastRule.IsDaylight);
        }

        private static long? GetRuleLocalTicks(TzDataRule rule, int year)
        {
            if (year < rule.FromYear || year > rule.ToYear)
                return null;

            try
            {
                return rule.GetTransitionDate(year).Ticks + rule.AtTime.Ticks;
            }
            catch (ArgumentOutOfRangeException)
            {
                // The date falls outside the range of DateTime, as for a weekday counted back from the first day of year 1.
                return null;
            }
        }

        private static long GetUntilTicks(TzDataUntil until, long standardOffset, long save)
        {
            var date = TzDataDayRule.Resolve(until.Year, until.Month, until.DayKind, until.Day, until.DayOfWeek);
            return date.Ticks + until.Time.Ticks - GetReferenceOffset(until.TimeReference, standardOffset, save);
        }

        private static long GetReferenceOffset(TzDataTimeReference timeReference, long standardOffset, long save)
        {
            switch (timeReference)
            {
                case TzDataTimeReference.Universal:
                    return 0;
                case TzDataTimeReference.Standard:
                    return standardOffset;
                default:
                    return standardOffset + save;
            }
        }

        private static State CreateState(
            TzDataZoneLine zoneLine,
#if NETCOREAPP3_0_OR_GREATER
            string? letter,
#else
            [CanBeNull] string letter,
#endif
            long standardOffset,
            long save,
            bool isDaylightSavingTime)
        {
            var abbreviation = FormatAbbreviation(zoneLine.Format, letter, isDaylightSavingTime, new TimeSpan(standardOffset + save));
            return new State(new TimeSpan(standardOffset), new TimeSpan(save), isDaylightSavingTime, abbreviation);
        }

        private static TzDataOffsetInfo CreateOffsetInfo(
            Change current,
#if NETCOREAPP3_0_OR_GREATER
            Change? next)
#else
            [CanBeNull] Change next)
#endif
        {
            var validFromUtc = current.Instant >= TicksMinimum ? ToUtc(current.Instant) : (DateTime?)null;
            var validUntilUtc = next != null ? ToUtc(next.Instant) : (DateTime?)null;
            var state = current.State;

            return new TzDataOffsetInfo(state.StandardOffset, state.DaylightSavings, state.IsDaylightSavingTime, state.Abbreviation, validFromUtc, validUntilUtc);
        }

        private static DateTime ToUtc(long ticks)
        {
            return new DateTime(Math.Min(Math.Max(ticks, TicksMinimum), TicksMaximum), DateTimeKind.Utc);
        }

        /// <summary>
        /// An offset from universal time, with the abbreviation by which it is known.
        /// </summary>
        private sealed class State : IEquatable<State>
        {
            internal State(TimeSpan standardOffset, TimeSpan daylightSavings, bool isDaylightSavingTime, string abbreviation)
            {
                StandardOffset = standardOffset;
                DaylightSavings = daylightSavings;
                IsDaylightSavingTime = isDaylightSavingTime;
                Abbreviation = abbreviation;
            }

            internal TimeSpan StandardOffset { get; }

            internal TimeSpan DaylightSavings { get; }

            internal bool IsDaylightSavingTime { get; }

            internal string Abbreviation { get; }

            internal long UtcOffsetTicks => StandardOffset.Ticks + DaylightSavings.Ticks;

#if NETCOREAPP3_0_OR_GREATER
            public bool Equals(State? other)
#else
            public bool Equals(State other)
#endif
            {
                return other != null &&
                       StandardOffset == other.StandardOffset &&
                       DaylightSavings == other.DaylightSavings &&
                       IsDaylightSavingTime == other.IsDaylightSavingTime &&
                       string.Equals(Abbreviation, other.Abbreviation, StringComparison.Ordinal);
            }

#if NETCOREAPP3_0_OR_GREATER
            public override bool Equals(object? obj)
#else
            public override bool Equals(object obj)
#endif
            {
                return obj is State other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = StandardOffset.GetHashCode();
                    hashCode = (hashCode * 397) ^ DaylightSavings.GetHashCode();
                    hashCode = (hashCode * 397) ^ IsDaylightSavingTime.GetHashCode();
                    hashCode = (hashCode * 397) ^ StringComparer.Ordinal.GetHashCode(Abbreviation);
                    return hashCode;
                }
            }
        }

        /// <summary>
        /// The instant, in ticks of universal time, from which an offset is in effect.
        /// </summary>
        private sealed class Change
        {
            internal Change(long instant, State state)
            {
                Instant = instant;
                State = state;
            }

            internal long Instant { get; }

            internal State State { get; }
        }

        /// <summary>
        /// The calculators of the timezones, keyed by identifier, built on first use of each.
        /// </summary>
        internal sealed class Cache
        {
            // netstandard1.0 has no ConcurrentDictionary, so the calculators are added under a lock.
            private readonly object _lock = new object();
            private readonly Dictionary<string, TzDataZoneCalculator> _calculators = new Dictionary<string, TzDataZoneCalculator>(StringComparer.Ordinal);

#if NETSTANDARD
            [NotNull]
#endif
            internal TzDataZoneCalculator GetCalculator(
#if NETSTANDARD
                [NotNull]
#endif
                string identifier,
#if NETSTANDARD
                [NotNull]
#endif
                TzDataZoneLine[] zoneLines)
            {
                lock (_lock)
                {
                    if (!_calculators.TryGetValue(identifier, out var calculator))
                    {
                        calculator = new TzDataZoneCalculator(zoneLines);
                        _calculators.Add(identifier, calculator);
                    }

                    return calculator;
                }
            }
        }
    }
}
