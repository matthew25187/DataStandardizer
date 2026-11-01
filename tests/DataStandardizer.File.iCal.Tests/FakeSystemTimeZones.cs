using System;
using System.Collections.Generic;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// A source of time zones for <see cref="IcalTimeZoneResolver.CreateSystemResolver"/> that stands in for the operating system's, so that the
    /// tests of the system resolver don't depend on the machine's time zone data.  It records each identifier it is asked for.
    /// </summary>
    /// <remarks>
    /// It knows one time zone, <see cref="EasternId"/>: <see cref="TimeZoneTransitions.Eastern2007Rules"/>.
    /// </remarks>
    internal sealed class FakeSystemTimeZones
    {
        /// <summary>
        /// The identifier of the one time zone the source knows.
        /// </summary>
        public const string EasternId = "Test/Eastern";

        private readonly List<string> _lookups = new List<string>();

        /// <summary>
        /// Gets the identifiers the source has been asked for, in order.
        /// </summary>
        public IReadOnlyList<string> Lookups => _lookups;

        /// <summary>
        /// Creates the system resolver over a new source.
        /// </summary>
        public static IIcalTimeZoneResolver CreateResolver()
        {
            return IcalTimeZoneResolver.CreateSystemResolver(new FakeSystemTimeZones().FindTimeZone);
        }

        /// <summary>
        /// Finds a time zone as <see cref="TimeZoneInfo.FindSystemTimeZoneById"/> does: it throws <see cref="TimeZoneNotFoundException"/> for
        /// an identifier it doesn't know.
        /// </summary>
        public TimeZoneInfo FindTimeZone(string timeZoneId)
        {
            _lookups.Add(timeZoneId);
            if (timeZoneId == EasternId) return TimeZoneTransitions.Eastern2007Rules;

            throw new TimeZoneNotFoundException();
        }
    }
}
