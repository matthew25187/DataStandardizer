using System;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// The changes of offset of real time zones in particular years, worked out by hand from the rules in force in each, so that tests can
    /// compare time zone rules with values that come from outside the library.
    /// </summary>
    internal static class TimeZoneTransitions
    {
        private static readonly TimeSpan EasternStandard = TimeSpan.FromHours(-5);
        private static readonly TimeSpan EasternDaylight = TimeSpan.FromHours(-4);
        private static readonly TimeSpan Greenwich = TimeSpan.Zero;
        private static readonly TimeSpan OneHourAhead = TimeSpan.FromHours(1);

        /// <summary>
        /// New York in 1970, under the Uniform Time Act of 1966: daylight time from 02:00 on the last Sunday in April to 02:00 on the last
        /// Sunday in October.
        /// </summary>
        public static readonly TimeZoneTransition[] NewYork1970 =
        {
            new TimeZoneTransition(new DateTime(1970, 4, 26, 7, 0, 0, DateTimeKind.Utc), EasternStandard, EasternDaylight),
            new TimeZoneTransition(new DateTime(1970, 10, 25, 6, 0, 0, DateTimeKind.Utc), EasternDaylight, EasternStandard),
        };

        /// <summary>
        /// New York in 1987, the first year in which daylight time started on the first Sunday in April.
        /// </summary>
        public static readonly TimeZoneTransition[] NewYork1987 =
        {
            new TimeZoneTransition(new DateTime(1987, 4, 5, 7, 0, 0, DateTimeKind.Utc), EasternStandard, EasternDaylight),
            new TimeZoneTransition(new DateTime(1987, 10, 25, 6, 0, 0, DateTimeKind.Utc), EasternDaylight, EasternStandard),
        };

        /// <summary>
        /// New York in 2006, the last year of daylight time from the first Sunday in April to the last Sunday in October.
        /// </summary>
        public static readonly TimeZoneTransition[] NewYork2006 =
        {
            new TimeZoneTransition(new DateTime(2006, 4, 2, 7, 0, 0, DateTimeKind.Utc), EasternStandard, EasternDaylight),
            new TimeZoneTransition(new DateTime(2006, 10, 29, 6, 0, 0, DateTimeKind.Utc), EasternDaylight, EasternStandard),
        };

        /// <summary>
        /// New York in 2007, the first year under the Energy Policy Act of 2005: daylight time from 02:00 on the second Sunday in March to 02:00
        /// on the first Sunday in November.
        /// </summary>
        public static readonly TimeZoneTransition[] NewYork2007 =
        {
            new TimeZoneTransition(new DateTime(2007, 3, 11, 7, 0, 0, DateTimeKind.Utc), EasternStandard, EasternDaylight),
            new TimeZoneTransition(new DateTime(2007, 11, 4, 6, 0, 0, DateTimeKind.Utc), EasternDaylight, EasternStandard),
        };

        /// <summary>
        /// New York in 2026, under the rules of 2007.
        /// </summary>
        public static readonly TimeZoneTransition[] NewYork2026 =
        {
            new TimeZoneTransition(new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc), EasternStandard, EasternDaylight),
            new TimeZoneTransition(new DateTime(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc), EasternDaylight, EasternStandard),
        };

        /// <summary>
        /// New York in 2029, under the rules of 2007.
        /// </summary>
        public static readonly TimeZoneTransition[] NewYork2029 =
        {
            new TimeZoneTransition(new DateTime(2029, 3, 11, 7, 0, 0, DateTimeKind.Utc), EasternStandard, EasternDaylight),
            new TimeZoneTransition(new DateTime(2029, 11, 4, 6, 0, 0, DateTimeKind.Utc), EasternDaylight, EasternStandard),
        };

        /// <summary>
        /// London and Dublin in 2026, under the European Union's summer time rules: summer time from 01:00 UTC on the last Sunday in March to
        /// 01:00 UTC on the last Sunday in October.
        /// </summary>
        public static readonly TimeZoneTransition[] LondonAndDublin2026 =
        {
            new TimeZoneTransition(new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc), Greenwich, OneHourAhead),
            new TimeZoneTransition(new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc), OneHourAhead, Greenwich),
        };

        /// <summary>
        /// US Eastern Time under the rules of 2007, built with <see cref="TimeZoneInfo.CreateCustomTimeZone(string, TimeSpan, string, string, string, TimeZoneInfo.AdjustmentRule[])"/>
        /// from fixed rules rather than taken from the operating system: UTC-05:00, with daylight time of an hour from 02:00 on the second Sunday
        /// in March to 02:00 on the first Sunday in November.
        /// </summary>
        public static readonly TimeZoneInfo Eastern2007Rules = TimeZoneInfo.CreateCustomTimeZone("Test/Eastern", EasternStandard, "Test Eastern",
            "Test Eastern Standard", "Test Eastern Daylight", new[]
            {
                TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2007, 1, 1), DateTime.MaxValue.Date, TimeSpan.FromHours(1),
                    TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday),
                    TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday))
            });
    }
}
