using System;
using System.Collections.Generic;
using DataStandardizer.Chronology;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The rules of a time zone taken from the TZ Database, through <see cref="TzDataTimezone"/>.
    /// </summary>
    internal sealed class IcalTzDataTimeZoneRules : IcalTimeZoneRules
    {
        private readonly TzDataTimezone _timezone;

        internal IcalTzDataTimeZoneRules(string timeZoneId, TzDataTimezone timezone) : base(timeZoneId)
        {
            _timezone = timezone;
        }

        /// <summary>
        /// Finds the TZ Database zone for a time zone identifier: the identifier itself, or, for one that starts with a solidus, the part after
        /// a vendor prefix such as <c>/mozilla.org/20050126_1/</c>.
        /// </summary>
        internal static bool TryFindTimezone(string timeZoneId, out TzDataTimezone timezone)
        {
            if (TzDataTimezone.TryParse(timeZoneId, out timezone)) return true;

            // ref. RFC 5545§3.8.3.1: a TZID that starts with a solidus is globally unique, which vendors give by prefixing a path to the TZ
            // Database name.  Each trailing part of the path is tried, longest first.
            if (timeZoneId.Length == 0 || timeZoneId[0] != '/') return false;

            for (var index = timeZoneId.IndexOf('/', 1); index >= 0 && index < timeZoneId.Length - 1; index = timeZoneId.IndexOf('/', index + 1))
            {
                if (TzDataTimezone.TryParse(timeZoneId.Substring(index + 1), out timezone)) return true;
            }

            return false;
        }

        protected override IcalUtcOffset GetUtcOffsetCore(DateTime utc)
        {
            return new IcalUtcOffset(_timezone.GetUtcOffset(utc));
        }

        protected override IEnumerable<IcalTimeZoneTransition> GetTransitionsCore(DateTime fromUtc, DateTime toUtc)
        {
            foreach (var transition in _timezone.GetTransitions(fromUtc, toUtc))
            {
                yield return ToTransition(transition);
            }
        }

        protected override IReadOnlyList<IcalUtcOffset> GetUtcOffsetsForLocalCore(DateTime local)
        {
            var offsetInfos = _timezone.GetOffsetInfosForLocal(local);
            var offsets = new IcalUtcOffset[offsetInfos.Count];
            for (var index = 0; index < offsets.Length; index++)
            {
                offsets[index] = new IcalUtcOffset(offsetInfos[index].UtcOffset);
            }

            return offsets;
        }

        internal static IcalTimeZoneTransition ToTransition(TzDataTransition transition)
        {
            return new IcalTimeZoneTransition(transition.InstantUtc, new IcalUtcOffset(transition.Before.UtcOffset),
                new IcalUtcOffset(transition.After.UtcOffset), GetKind(transition.After), GetName(transition.After));
        }

        internal static IcalTimeZoneObservanceKind GetKind(TzDataOffsetInfo offsetInfo)
        {
            return offsetInfo.IsDaylightSavingTime ? IcalTimeZoneObservanceKind.Daylight : IcalTimeZoneObservanceKind.Standard;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string? GetName(TzDataOffsetInfo offsetInfo)
#else
        internal static string GetName(TzDataOffsetInfo offsetInfo)
#endif
        {
            return offsetInfo.Abbreviation.Length == 0 ? null : offsetInfo.Abbreviation;
        }
    }
}
