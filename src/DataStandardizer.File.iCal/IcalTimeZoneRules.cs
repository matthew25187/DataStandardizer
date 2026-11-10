using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The rules of a time zone, as resolved by an <see cref="IIcalTimeZoneResolver"/>: the UTC offset in use at each instant, and how a local
    /// wall-clock time maps to UTC.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A derived type supplies the offset at an instant and the transitions over a period.  The mapping of local time to UTC is built on those
    /// two, and follows RFC 5545 §3.3.5: a time in a daylight saving gap is read with the offset in use before the gap, and a time in an
    /// overlap takes its first occurrence.
    /// </para>
    /// <para>
    /// Times in UTC may be of kind <see cref="DateTimeKind.Utc"/> or <see cref="DateTimeKind.Unspecified"/>.  Local wall-clock times must be of
    /// kind <see cref="DateTimeKind.Unspecified"/>.  A time of kind <see cref="DateTimeKind.Local"/>, which belongs to the time zone of the
    /// machine, is rejected by both.
    /// </para>
    /// </remarks>
    public abstract class IcalTimeZoneRules
    {
        /// <summary>
        /// The longest time zone identifier that is looked up.  It is the longest that the system's time zone files can be named, which is longer
        /// than any name in the TZ Database, so for that source it only bounds the work.
        /// </summary>
        internal const int MaxIdentifierLength = 255;

        /// <summary>
        /// Gets the index of the first solidus in a time zone identifier that starts with one after which the rest is no longer than
        /// <see cref="MaxIdentifierLength"/>, or -1 if there is none.  The parts of such an identifier after each solidus are the candidates for an
        /// identifier that a vendor prefix such as <c>/mozilla.org/20050126_1/</c> was put in front of, so a longer part isn't tried, or copied.
        /// </summary>
        /// <param name="timeZoneId">The time zone identifier, which is not empty.</param>
        internal static int GetFirstTrailingPartSeparator(string timeZoneId)
        {
            return timeZoneId.IndexOf('/', Math.Max(1, timeZoneId.Length - MaxIdentifierLength - 1));
        }

        private const string InstantOutOfRangeMessage = "The instant falls outside the range of DateTime.";

        /// <summary>
        /// Creates the rules of a time zone.
        /// </summary>
        /// <param name="timeZoneId">The time zone identifier, as given by a <c>TZID</c> parameter.</param>
        /// <exception cref="ArgumentNullException"><paramref name="timeZoneId"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="timeZoneId"/> is empty.</exception>
        protected IcalTimeZoneRules(string timeZoneId)
        {
            if (timeZoneId is null)
                throw new ArgumentNullException(nameof(timeZoneId));
            if (timeZoneId.Length == 0)
                throw new ArgumentException("The time zone identifier cannot be empty.", nameof(timeZoneId));

            TimeZoneId = timeZoneId;
        }

        /// <summary>
        /// Gets the time zone identifier the rules were resolved for.
        /// </summary>
        public string TimeZoneId { get; }

        /// <summary>
        /// Gets the UTC offset in use at an instant.
        /// </summary>
        /// <param name="utc">The instant, in UTC.</param>
        /// <returns>The offset.</returns>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        public IcalUtcOffset GetUtcOffset(DateTime utc)
        {
            return GetUtcOffsetCore(ToUtcKind(utc, nameof(utc)));
        }

        /// <summary>
        /// Gets the transitions between UTC offsets over a period.
        /// </summary>
        /// <param name="fromUtc">The start of the period, in UTC, which is included.</param>
        /// <param name="toUtc">The end of the period, in UTC, which is excluded.</param>
        /// <returns>The transitions, in chronological order.</returns>
        /// <exception cref="ArgumentException"><paramref name="fromUtc"/> or <paramref name="toUtc"/> is of kind
        /// <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toUtc"/> precedes <paramref name="fromUtc"/>.</exception>
        public IEnumerable<IcalTimeZoneTransition> GetTransitions(DateTime fromUtc, DateTime toUtc)
        {
            var from = ToUtcKind(fromUtc, nameof(fromUtc));
            var to = ToUtcKind(toUtc, nameof(toUtc));
            if (to < from)
                throw new ArgumentOutOfRangeException(nameof(toUtc), toUtc, "The end of the period must not precede its start.");

            return GetTransitionsCore(from, to);
        }

        /// <summary>
        /// Gets every UTC offset in use at a local wall-clock time.
        /// </summary>
        /// <param name="local">The local time, of kind <see cref="DateTimeKind.Unspecified"/>.</param>
        /// <returns>The offsets, in chronological order of the instants at which the local clock shows the time: none where the time falls in a
        /// gap, as when clocks go forward; one normally; and two where the time falls in an overlap, as when clocks go back.  An offset that
        /// would put the time's instant outside the range of <see cref="DateTime"/> can be left out, so within a day of either end of that
        /// range the result can also be empty, or hold only the occurrence that falls within it.</returns>
        /// <exception cref="ArgumentException"><paramref name="local"/> is of kind <see cref="DateTimeKind.Utc"/> or
        /// <see cref="DateTimeKind.Local"/>.</exception>
        public IReadOnlyList<IcalUtcOffset> GetUtcOffsetsForLocal(DateTime local)
        {
            return GetUtcOffsetsForLocalCore(CheckLocal(local, nameof(local)));
        }

        /// <summary>
        /// Converts a local wall-clock time to UTC, as RFC 5545 §3.3.5 requires.
        /// </summary>
        /// <param name="local">The local time, of kind <see cref="DateTimeKind.Unspecified"/>.</param>
        /// <returns>The instant, of kind <see cref="DateTimeKind.Utc"/>.</returns>
        /// <remarks>
        /// A time in a gap, as when clocks go forward, is read with the offset in use before the gap, which moves it forward by the length of
        /// the gap.  A time in an overlap, as when clocks go back, takes its first occurrence, the earlier instant, even where that falls before
        /// the range of <see cref="DateTime"/> and the second occurrence does not.
        /// </remarks>
        /// <exception cref="ArgumentException"><paramref name="local"/> is of kind <see cref="DateTimeKind.Utc"/> or
        /// <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The instant, which for a time in an overlap is its first occurrence, falls outside the
        /// range of <see cref="DateTime"/>.</exception>
        public DateTime ToUtc(DateTime local)
        {
            CheckLocal(local, nameof(local));

            var offsets = GetUtcOffsetsForLocalCore(local);
            IcalUtcOffset offset;

            // Only within a day of the start of the range can an offset put an occurrence of the time before it.
            if (offsets.Count > 0 && local.Ticks >= DateTime.MinValue.Ticks + TimeSpan.TicksPerDay)
            {
                offset = offsets[0];
            }
            else
            {
                bool isBeforeRange;
                bool isAfterRange;
                var offsetBeforeGap = WalkLocal(local, null, out isBeforeRange, out isAfterRange);

                // ref. RFC 5545 §3.3.5: a time in an overlap takes its first occurrence, which has no instant when it falls before the range.
                if (isBeforeRange)
                    throw new ArgumentOutOfRangeException(nameof(local), local, InstantOutOfRangeMessage);

                if (offsets.Count > 0)
                    offset = offsets[0];
                else if (offsetBeforeGap.HasValue)
                    offset = offsetBeforeGap.Value;
                else if (isAfterRange)
                    throw new ArgumentOutOfRangeException(nameof(local), local, InstantOutOfRangeMessage);
                else
                    throw new InvalidOperationException("The local time has no UTC offset, but falls in no gap between the time zone's offsets.");
            }

            var ticks = local.Ticks - offset.Offset.Ticks;
            if (!IsInRange(ticks))
                throw new ArgumentOutOfRangeException(nameof(local), local, InstantOutOfRangeMessage);

            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Converts an instant to the local wall-clock time of the time zone.
        /// </summary>
        /// <param name="utc">The instant, in UTC.</param>
        /// <returns>The local time, of kind <see cref="DateTimeKind.Unspecified"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="utc"/> is of kind <see cref="DateTimeKind.Local"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The local time falls outside the range of <see cref="DateTime"/>.</exception>
        public DateTime ToLocal(DateTime utc)
        {
            var instant = ToUtcKind(utc, nameof(utc));
            var ticks = instant.Ticks + GetUtcOffsetCore(instant).Offset.Ticks;
            if (!IsInRange(ticks))
                throw new ArgumentOutOfRangeException(nameof(utc), utc, "The local time falls outside the range of DateTime.");

            return new DateTime(ticks, DateTimeKind.Unspecified);
        }

        /// <summary>
        /// When overridden in a derived class, gets the UTC offset in use at an instant.
        /// </summary>
        /// <param name="utc">The instant, of kind <see cref="DateTimeKind.Utc"/>.</param>
        /// <returns>The offset.</returns>
        protected abstract IcalUtcOffset GetUtcOffsetCore(DateTime utc);

        /// <summary>
        /// When overridden in a derived class, gets the transitions between UTC offsets over a period.
        /// </summary>
        /// <param name="fromUtc">The start of the period, of kind <see cref="DateTimeKind.Utc"/>, which is included.</param>
        /// <param name="toUtc">The end of the period, of kind <see cref="DateTimeKind.Utc"/>, which is excluded.  It does not precede
        /// <paramref name="fromUtc"/>.</param>
        /// <returns>The transitions, in chronological order.  The <see cref="IcalTimeZoneTransition.OffsetTo"/> of each must be what
        /// <see cref="GetUtcOffsetCore"/> gives from its instant until the next.</returns>
        protected abstract IEnumerable<IcalTimeZoneTransition> GetTransitionsCore(DateTime fromUtc, DateTime toUtc);

        /// <summary>
        /// Gets every UTC offset in use at a local wall-clock time.
        /// </summary>
        /// <param name="local">The local time, of kind <see cref="DateTimeKind.Unspecified"/>.</param>
        /// <returns>The offsets, in chronological order of the instants at which the local clock shows the time.</returns>
        /// <remarks>
        /// The default implementation takes the offset in use a day before the time and the transitions over the two days either side of it,
        /// which covers every offset of less than a day.  It keeps each offset whose instant, the time less the offset, falls while that offset
        /// is in use, and within the range of <see cref="DateTime"/>.
        /// </remarks>
        protected virtual IReadOnlyList<IcalUtcOffset> GetUtcOffsetsForLocalCore(DateTime local)
        {
            var offsets = new List<IcalUtcOffset>(2);
            WalkLocal(local, offsets, out _, out _);
            return offsets;
        }

        /// <summary>
        /// Walks the periods of constant offset within a day either side of a local time, adding to <paramref name="offsets"/> each offset that
        /// gives the time within the range of <see cref="DateTime"/>, and returns the offset before the first gap the time falls in.
        /// <paramref name="isBeforeRange"/> and <paramref name="isAfterRange"/> tell whether an offset gives the time at an instant before or
        /// after that range.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IcalUtcOffset? WalkLocal(DateTime local, List<IcalUtcOffset>? offsets, out bool isBeforeRange, out bool isAfterRange)
#else
        private IcalUtcOffset? WalkLocal(DateTime local, List<IcalUtcOffset> offsets, out bool isBeforeRange, out bool isAfterRange)
#endif
        {
            isBeforeRange = false;
            isAfterRange = false;

            // An offset is less than a day, so only the offsets in use within a day either side of the time can give it.
            var windowStart = AddClamped(local, -TimeSpan.TicksPerDay, DateTimeKind.Utc);
            var windowEnd = AddClamped(local, TimeSpan.TicksPerDay, DateTimeKind.Utc);
            var offset = GetUtcOffsetCore(windowStart);

            // At the start of the range of DateTime, the first offset is taken to have been in use since before it, as nothing earlier is known.
            var periodStart = windowStart.Ticks == DateTime.MinValue.Ticks ? long.MinValue : windowStart.Ticks;
            IcalUtcOffset? offsetBeforeGap = null;
            foreach (var transition in GetTransitionsCore(windowStart, windowEnd))
            {
                var periodEnd = transition.InstantUtc.Ticks;

                // A transition at the start of the window ends the period of the offset before it.
                if (periodEnd == windowStart.Ticks) offset = transition.OffsetFrom;
                AddIfGivesTime(offsets, local, offset, periodStart, periodEnd, ref isBeforeRange, ref isAfterRange);

                // The time falls in a gap where it comes after the transition on the clock before it, and before it on the clock after it.
                if (!offsetBeforeGap.HasValue && local.Ticks - offset.Offset.Ticks >= periodEnd &&
                    local.Ticks - transition.OffsetTo.Offset.Ticks < periodEnd)
                {
                    offsetBeforeGap = offset;
                }

                offset = transition.OffsetTo;
                periodStart = periodEnd;
            }

            AddIfGivesTime(offsets, local, offset, periodStart, long.MaxValue, ref isBeforeRange, ref isAfterRange);
            return offsetBeforeGap;
        }

        /// <summary>
        /// Adds an offset to <paramref name="offsets"/> where the instant it gives a local time falls within its period and within the range of
        /// <see cref="DateTime"/>, or records that the instant falls before or after that range.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static void AddIfGivesTime(List<IcalUtcOffset>? offsets, DateTime local, IcalUtcOffset offset, long periodStart, long periodEnd,
            ref bool isBeforeRange, ref bool isAfterRange)
#else
        private static void AddIfGivesTime(List<IcalUtcOffset> offsets, DateTime local, IcalUtcOffset offset, long periodStart, long periodEnd,
            ref bool isBeforeRange, ref bool isAfterRange)
#endif
        {
            var candidate = local.Ticks - offset.Offset.Ticks;
            if (candidate < periodStart || candidate >= periodEnd) return;

            if (candidate < DateTime.MinValue.Ticks)
            {
                isBeforeRange = true;
            }
            else if (candidate > DateTime.MaxValue.Ticks)
            {
                isAfterRange = true;
            }
            else if (offsets != null && (offsets.Count == 0 || offsets[offsets.Count - 1] != offset))
            {
                offsets.Add(offset);
            }
        }

        private static bool IsInRange(long ticks)
        {
            return ticks >= DateTime.MinValue.Ticks && ticks <= DateTime.MaxValue.Ticks;
        }

        /// <summary>
        /// Adds ticks to a time, clamped to the range of <see cref="DateTime"/>.
        /// </summary>
        internal static DateTime AddClamped(DateTime value, long ticks, DateTimeKind kind)
        {
            var result = ticks < 0
                ? (value.Ticks < DateTime.MinValue.Ticks - ticks ? DateTime.MinValue.Ticks : value.Ticks + ticks)
                : (value.Ticks > DateTime.MaxValue.Ticks - ticks ? DateTime.MaxValue.Ticks : value.Ticks + ticks);
            return new DateTime(result, kind);
        }

        /// <summary>
        /// Gives a time in UTC the kind <see cref="DateTimeKind.Utc"/>, rejecting one of kind <see cref="DateTimeKind.Local"/>.
        /// </summary>
        internal static DateTime ToUtcKind(DateTime utc, string parameterName)
        {
            if (utc.Kind == DateTimeKind.Local)
                throw new ArgumentException("The time must be in UTC, of kind Utc or Unspecified.", parameterName);

            return DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        }

        private static DateTime CheckLocal(DateTime local, string parameterName)
        {
            if (local.Kind != DateTimeKind.Unspecified)
                throw new ArgumentException("The time must be a local wall-clock time, of kind Unspecified.", parameterName);

            return local;
        }
    }
}
