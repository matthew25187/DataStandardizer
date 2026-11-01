namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Content lines that many tests use to build calendars.
    /// </summary>
    internal static class IcalLines
    {
        // ref. RFC 5545 §3.4, §3.6: the lines that start and end each component.
        public const string BeginCalendar = "BEGIN:VCALENDAR";
        public const string EndCalendar = "END:VCALENDAR";
        public const string BeginEvent = "BEGIN:VEVENT";
        public const string EndEvent = "END:VEVENT";
        public const string BeginTodo = "BEGIN:VTODO";
        public const string EndTodo = "END:VTODO";
        public const string BeginJournal = "BEGIN:VJOURNAL";
        public const string EndJournal = "END:VJOURNAL";
        public const string BeginFreeBusy = "BEGIN:VFREEBUSY";
        public const string EndFreeBusy = "END:VFREEBUSY";
        public const string BeginTimeZone = "BEGIN:VTIMEZONE";
        public const string EndTimeZone = "END:VTIMEZONE";
        public const string BeginStandard = "BEGIN:STANDARD";
        public const string EndStandard = "END:STANDARD";
        public const string BeginDaylight = "BEGIN:DAYLIGHT";
        public const string EndDaylight = "END:DAYLIGHT";
        public const string BeginAlarm = "BEGIN:VALARM";
        public const string EndAlarm = "END:VALARM";

        // ref. RFC 5545 §3.7.4: the version of the specification that a calendar needs.
        public const string Version20 = "VERSION:2.0";

        // The product identifier of the calendars that ModelTestData.Calendar builds.
        public const string TestProductId = "PRODID:-//Test//Test//EN";

        // The DTSTAMP and DTSTART of the events that ModelTestData.EventCalendar builds.
        public const string NewYearStamp = "DTSTAMP:20260101T000000Z";
        public const string NewYearStart = "DTSTART:20260101T090000Z";

        // The UID of the event that ModelTestData.EventCalendar builds.
        public const string EventUid = "UID:event@example.com";

        // The calendar and event component paths of IcalFileException.DataItemName.ComponentPath.
        public const string CalendarPath = "VCALENDAR";
        public const string EventPath = "VCALENDAR/VEVENT[1]";
    }
}
