using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Asks a set of time zone rules for many results at once, so that a test can make a single call and compare the results with its
    /// expectations.
    /// </summary>
    /// <remarks>
    /// Test infrastructure: it loops and filters so that the tests that use it don't have to.  It always gives the same results for the same
    /// rules and inputs.
    /// </remarks>
    internal static class TimeZoneRulesProbe
    {
        /// <summary>
        /// Gets the offset that the rules give at each instant, in turn.
        /// </summary>
        public static List<TimeSpan> GetUtcOffsets(IcalTimeZoneRules rules, IEnumerable<DateTime> instants)
        {
            var offsets = new List<TimeSpan>();
            foreach (var instant in instants)
            {
                offsets.Add(rules.GetUtcOffset(instant).Offset);
            }

            return offsets;
        }

        /// <summary>
        /// Gets the transitions from <paramref name="fromUtc"/> to <paramref name="toUtc"/> that change the offset, leaving out those that only
        /// change the name or the kind of time.
        /// </summary>
        public static List<TimeZoneTransition> GetOffsetChanges(IcalTimeZoneRules rules, DateTime fromUtc, DateTime toUtc)
        {
            var changes = new List<TimeZoneTransition>();
            foreach (var transition in rules.GetTransitions(fromUtc, toUtc))
            {
                if (transition.OffsetFrom == transition.OffsetTo) continue;

                changes.Add(new TimeZoneTransition(transition.InstantUtc, transition.OffsetFrom.Offset, transition.OffsetTo.Offset));
            }

            return changes;
        }
    }

    /// <summary>
    /// A change of offset at an instant.
    /// </summary>
    public readonly struct TimeZoneTransition : IEquatable<TimeZoneTransition>
    {
        public TimeZoneTransition(DateTime instantUtc, TimeSpan offsetFrom, TimeSpan offsetTo)
        {
            InstantUtc = instantUtc;
            OffsetFrom = offsetFrom;
            OffsetTo = offsetTo;
        }

        public DateTime InstantUtc { get; }

        public TimeSpan OffsetFrom { get; }

        public TimeSpan OffsetTo { get; }

        public bool Equals(TimeZoneTransition other)
        {
            return InstantUtc == other.InstantUtc && InstantUtc.Kind == other.InstantUtc.Kind && OffsetFrom == other.OffsetFrom &&
                   OffsetTo == other.OffsetTo;
        }

        public override bool Equals(object obj)
        {
            return obj is TimeZoneTransition other && Equals(other);
        }

        public override int GetHashCode()
        {
            return InstantUtc.GetHashCode() ^ OffsetFrom.GetHashCode() ^ OffsetTo.GetHashCode();
        }

        public override string ToString()
        {
            return InstantUtc.ToString("o", System.Globalization.CultureInfo.InvariantCulture) + " " + OffsetFrom + " to " + OffsetTo;
        }
    }
}
