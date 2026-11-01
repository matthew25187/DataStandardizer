#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Collections.Generic;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The rules of a time zone taken from the operating system, through <see cref="TimeZoneInfo"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="TimeZoneInfo"/> gives the offset at an instant but no list of transitions on every target, so the transitions are found by
    /// sampling the offset a day apart and narrowing each change down to the tick.  Two changes less than a day apart that cancel out are not
    /// seen.
    /// </remarks>
    internal sealed class IcalSystemTimeZoneRules : IcalTimeZoneRules
    {
        private const int MaxIdentifierLength = 255;

        private readonly TimeZoneInfo _timeZone;

        internal IcalSystemTimeZoneRules(string timeZoneId, TimeZoneInfo timeZone) : base(timeZoneId)
        {
            _timeZone = timeZone;
        }

        /// <summary>
        /// Finds the system time zone for a time zone identifier: the identifier itself, or, for one that starts with a solidus, the part after
        /// a vendor prefix such as <c>/mozilla.org/20050126_1/</c>.
        /// </summary>
        /// <param name="timeZoneId">The time zone identifier.</param>
        /// <param name="findTimeZone">Finds a time zone by its identifier, as <see cref="TimeZoneInfo.FindSystemTimeZoneById"/> does.</param>
        /// <param name="timeZone">The time zone found.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryFindTimeZone(string timeZoneId, Func<string, TimeZoneInfo> findTimeZone, [NotNullWhen(true)] out TimeZoneInfo? timeZone)
#else
        internal static bool TryFindTimeZone(string timeZoneId, Func<string, TimeZoneInfo> findTimeZone, out TimeZoneInfo timeZone)
#endif
        {
            timeZone = null;
            if (timeZoneId.Length == 0) return false;
            if (timeZoneId[0] != '/') return TryFindSystemTimeZone(timeZoneId, findTimeZone, out timeZone);

            // ref. RFC 5545§3.8.3.1: a TZID that starts with a solidus is globally unique, which vendors give by prefixing a path to the TZ
            // Database name.  Each trailing part of the path is tried, longest first.
            for (var index = timeZoneId.IndexOf('/', 1); index >= 0 && index < timeZoneId.Length - 1; index = timeZoneId.IndexOf('/', index + 1))
            {
                if (TryFindSystemTimeZone(timeZoneId.Substring(index + 1), findTimeZone, out timeZone)) return true;
            }

            return false;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static bool TryFindSystemTimeZone(string timeZoneId, Func<string, TimeZoneInfo> findTimeZone,
            [NotNullWhen(true)] out TimeZoneInfo? timeZone)
#else
        private static bool TryFindSystemTimeZone(string timeZoneId, Func<string, TimeZoneInfo> findTimeZone, out TimeZoneInfo timeZone)
#endif
        {
            timeZone = null;

            // A TZID comes from the calendar, which may not be trusted.  On Linux and macOS the identifier names a file under the zoneinfo
            // directory, so an identifier that could name a file elsewhere is refused before it reaches TimeZoneInfo.
            if (!IsSafeIdentifier(timeZoneId)) return false;

            try
            {
                timeZone = findTimeZone(timeZoneId);
                return true;
            }
            catch (TimeZoneNotFoundException)
            {
                return false;
            }
            catch (InvalidTimeZoneException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static bool IsSafeIdentifier(string timeZoneId)
        {
            if (timeZoneId.Length == 0 || timeZoneId.Length > MaxIdentifierLength || timeZoneId[0] == '/' ||
                timeZoneId[timeZoneId.Length - 1] == '/' || timeZoneId.IndexOf("..", StringComparison.Ordinal) >= 0)
                return false;

            foreach (var character in timeZoneId)
            {
                if (character < ' ' || character == '\\' || character == ':' || character == '\u007F') return false;
            }

            return true;
        }

        protected override IcalUtcOffset GetUtcOffsetCore(DateTime utc)
        {
            return new IcalUtcOffset(_timeZone.GetUtcOffset(utc));
        }

        protected override IEnumerable<IcalTimeZoneTransition> GetTransitionsCore(DateTime fromUtc, DateTime toUtc)
        {
            if (fromUtc >= toUtc) yield break;

            var previousTicks = fromUtc.Ticks;
            var previous = GetState(previousTicks);
            if (previousTicks > DateTime.MinValue.Ticks)
            {
                var before = GetState(previousTicks - 1);
                if (!before.Matches(previous))
                {
                    yield return ToTransition(previousTicks, before, previous);
                }
            }

            // The last tick of the period, which excludes its end.
            var lastTicks = toUtc.Ticks - 1;
            while (previousTicks < lastTicks)
            {
                var nextTicks = lastTicks - previousTicks > TimeSpan.TicksPerDay ? previousTicks + TimeSpan.TicksPerDay : lastTicks;
                var next = GetState(nextTicks);
                if (next.Matches(previous))
                {
                    previousTicks = nextTicks;
                    continue;
                }

                // The state changes in (previousTicks, nextTicks]: narrow it down to the first tick of the new state.
                var low = previousTicks;
                var high = nextTicks;
                while (high - low > 1)
                {
                    var middle = low + (high - low) / 2;
                    if (GetState(middle).Matches(previous))
                    {
                        low = middle;
                    }
                    else
                    {
                        high = middle;
                    }
                }

                var current = GetState(high);
                yield return ToTransition(high, previous, current);
                previousTicks = high;
                previous = current;
            }
        }

        protected override IReadOnlyList<IcalUtcOffset> GetUtcOffsetsForLocalCore(DateTime local)
        {
            if (_timeZone.IsInvalidTime(local)) return new IcalUtcOffset[0];

            if (!_timeZone.IsAmbiguousTime(local)) return new[] { new IcalUtcOffset(_timeZone.GetUtcOffset(local)) };

            // The greater offset gives the earlier instant, so it comes first.
            var ambiguousOffsets = _timeZone.GetAmbiguousTimeOffsets(local);
            var offsets = new IcalUtcOffset[ambiguousOffsets.Length];
            for (var index = 0; index < offsets.Length; index++)
            {
                offsets[index] = new IcalUtcOffset(ambiguousOffsets[index]);
            }

            Array.Sort(offsets, (left, right) => right.Offset.CompareTo(left.Offset));
            return offsets;
        }

        private State GetState(long utcTicks)
        {
            var utc = new DateTime(utcTicks, DateTimeKind.Utc);
            return new State(_timeZone.GetUtcOffset(utc), _timeZone.IsDaylightSavingTime(utc));
        }

        private static IcalTimeZoneTransition ToTransition(long utcTicks, State before, State after)
        {
            // TimeZoneInfo names a time zone in the language of the machine, such as "Eastern Daylight Time", and has no customary
            // abbreviation, so the transition is left without a name.
            return new IcalTimeZoneTransition(new DateTime(utcTicks, DateTimeKind.Utc), new IcalUtcOffset(before.Offset),
                new IcalUtcOffset(after.Offset), after.IsDaylightSavingTime ? IcalTimeZoneObservanceKind.Daylight : IcalTimeZoneObservanceKind.Standard,
                null);
        }

        private readonly struct State
        {
            internal State(TimeSpan offset, bool isDaylightSavingTime)
            {
                Offset = offset;
                IsDaylightSavingTime = isDaylightSavingTime;
            }

            internal TimeSpan Offset { get; }

            internal bool IsDaylightSavingTime { get; }

            internal bool Matches(State other)
            {
                return Offset == other.Offset && IsDaylightSavingTime == other.IsDaylightSavingTime;
            }
        }
    }
}
#endif
