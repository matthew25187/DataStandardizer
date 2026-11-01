namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Values taken from the examples of RFC 5545, each named for the section it comes from.
    /// </summary>
    internal static class Rfc5545Examples
    {
        // ref. RFC 5545 §3.3.4: July 14, 1997.
        public const string Date = "19970714";

        // ref. RFC 5545 §3.3.5: 11 PM on January 18, 1998, as a floating time.
        public const string FloatingDateTime = "19980118T230000";

        // ref. RFC 5545 §3.3.5: 7 AM UTC on January 19, 1998.
        public const string UtcDateTime = "19980119T070000Z";

        // ref. RFC 5545 §3.3.5: 2 AM in New York on January 19, 1998, which is UtcDateTime.
        public const string NewYorkLocalDateTime = "19980119T020000";

        // ref. RFC 5545 §3.3.5: the time zone of NewYorkLocalDateTime.
        public const string NewYorkTimeZoneId = "America/New_York";

        // ref. RFC 5545 §3.3.6: 15 days, 5 hours and 20 seconds.
        public const string Duration = "P15DT5H0M20S";

        // ref. RFC 5545 §3.3.9: a period of time with an explicit start and end.
        public const string ExplicitPeriod = "19970101T180000Z/19970102T070000Z";

        // ref. RFC 5545 §3.3.9: a period of time with a start and a duration.
        public const string StartPeriod = "19970101T180000Z/PT5H30M";

        // ref. RFC 5545 §3.3.9: the start of both example periods.
        public const string PeriodStart = "19970101T180000Z";

        // ref. RFC 5545 §3.3.10: every other year in January, on Sundays, at 8:30 AM and 9:30 AM.
        public const string RecurrenceRule = "FREQ=YEARLY;INTERVAL=2;BYMONTH=1;BYDAY=SU;BYHOUR=8,9;BYMINUTE=30";

        // ref. RFC 5545 §3.8.5.3: every other month on the first and last Sunday of the month, for 10 occurrences.
        public const string MonthlyFirstAndLastSundayRule = "FREQ=MONTHLY;INTERVAL=2;COUNT=10;BYDAY=1SU,-1SU";

        // ref. RFC 5545 §3.8.5.3: every day in January, for 3 years.
        public const string EveryDayInJanuaryRule = "FREQ=YEARLY;UNTIL=20000131T140000Z;BYMONTH=1;BYDAY=SU,MO,TU,WE,TH,FR,SA";

        // ref. RFC 5545 §3.3.12: 7 AM UTC.
        public const string UtcTime = "070000Z";

        // ref. RFC 5545 §3.3.14: five hours behind UTC.
        public const string NegativeUtcOffset = "-0500";

        // ref. RFC 5545 §3.3.14: one hour ahead of UTC.
        public const string PositiveUtcOffset = "+0100";

        // ref. RFC 5545 §3.3.8: a negative INTEGER value.
        public const string NegativeInteger = "-1234567890";
    }
}
