using System;
using System.Collections.Generic;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Calendars that each break one rule of RFC 5545 or RFC 7986, with the kind of problem and the property it is reported for.
    /// </summary>
    internal static class ConformanceCases
    {
        public const string MissingVersionCase = "MissingVersion";
        public const string MissingUidCase = "MissingUid";
        public const string TwoSummariesCase = "TwoSummaries";
        public const string RefreshIntervalWithoutValueCase = "RefreshIntervalWithoutValue";
        public const string BinaryWithoutEncodingCase = "BinaryWithoutEncoding";

        private const string DtStart = "DTSTART";
        private const string Duration = "DURATION";
        private const string RecurrenceRule = "RRULE";
        private const string AlarmPath = "VCALENDAR/VEVENT[1]/VALARM[1]";
        private const string TodoPath = "VCALENDAR/VTODO[1]";
        private const string TimeZonePath = "VCALENDAR/VTIMEZONE[1]";
        private const string JournalUid = "UID:j@example.com";
        private const string OtherEventUid = "UID:e@example.com";
        private const string DateStart = "DTSTART;VALUE=DATE:20260101";
        private const string OneHour = "DURATION:PT1H";
        private const string AlarmTrigger = "TRIGGER:-PT5M";
        private const string TimeZoneId = "TZID:Zone";
        private const string ZeroOffsetFrom = "TZOFFSETFROM:+0000";
        private const string ZeroOffsetTo = "TZOFFSETTO:+0000";

        public sealed class Case
        {
            public Case(string text, IcalDiagnosticKind kind, string propertyName, string componentPath)
            {
                Text = text;
                Kind = kind;
                PropertyName = propertyName;
                ComponentPath = componentPath;
                AcceptingOptions = new IcalOptions
                {
                    AllowMissingRequiredProperties = kind == IcalDiagnosticKind.MissingRequiredProperty,
                    AllowCardinalityViolations = kind == IcalDiagnosticKind.CardinalityViolation,
                    AllowUndefinedTimeZones = kind == IcalDiagnosticKind.UndefinedTimeZone,
                    AllowInvalidValues = kind == IcalDiagnosticKind.InvalidValue
                };
                AcceptingAllButOptions = new IcalOptions
                {
                    AllowMissingRequiredProperties = kind != IcalDiagnosticKind.MissingRequiredProperty,
                    AllowCardinalityViolations = kind != IcalDiagnosticKind.CardinalityViolation,
                    AllowUndefinedTimeZones = kind != IcalDiagnosticKind.UndefinedTimeZone,
                    AllowInvalidValues = kind != IcalDiagnosticKind.InvalidValue
                };
            }

            public string Text { get; }

            public IcalDiagnosticKind Kind { get; }

            public string PropertyName { get; }

            public string ComponentPath { get; }

            /// <summary>
            /// Gets options that accept the kind of problem, and no other.
            /// </summary>
            public IcalOptions AcceptingOptions { get; }

            /// <summary>
            /// Gets options with every accommodation turned on except the one that accepts the kind of problem.
            /// </summary>
            public IcalOptions AcceptingAllButOptions { get; }
        }

        public static readonly IReadOnlyDictionary<string, Case> All = new Dictionary<string, Case>
        {
            // Missing required properties and components.
            [MissingUidCase] = new Case(Calendar(BeginEvent, NewYearStamp, NewYearStart, EndEvent),
                IcalDiagnosticKind.MissingRequiredProperty, "UID", EventPath),
            ["MissingDtStamp"] = new Case(Calendar(BeginTodo, "UID:todo@example.com", EndTodo),
                IcalDiagnosticKind.MissingRequiredProperty, "DTSTAMP", TodoPath),
            ["MissingProdId"] = new Case(IcalTestData.Crlf(BeginCalendar, Version20, BeginJournal, JournalUid, NewYearStamp,
                EndJournal, EndCalendar), IcalDiagnosticKind.MissingRequiredProperty, "PRODID", CalendarPath),
            [MissingVersionCase] = new Case(IcalTestData.Crlf(BeginCalendar, TestProductId, BeginJournal, JournalUid,
                NewYearStamp, EndJournal, EndCalendar), IcalDiagnosticKind.MissingRequiredProperty, "VERSION", CalendarPath),
            ["MissingEventStart"] = new Case(Calendar(BeginEvent, OtherEventUid, NewYearStamp, EndEvent),
                IcalDiagnosticKind.MissingRequiredProperty, DtStart, EventPath),
            ["NoCalendarComponent"] = new Case(Calendar(), IcalDiagnosticKind.MissingRequiredProperty, null, CalendarPath),
            ["NoObservance"] = new Case(Calendar(BeginTimeZone, TimeZoneId, EndTimeZone),
                IcalDiagnosticKind.MissingRequiredProperty, null, TimeZonePath),
            ["DurationWithoutRepeat"] = new Case(EventCalendar(BeginAlarm, "ACTION:AUDIO", AlarmTrigger, "DURATION:PT5M", EndAlarm),
                IcalDiagnosticKind.MissingRequiredProperty, "REPEAT", AlarmPath),
            ["DisplayWithoutDescription"] = new Case(EventCalendar(BeginAlarm, "ACTION:DISPLAY", AlarmTrigger, EndAlarm),
                IcalDiagnosticKind.MissingRequiredProperty, "DESCRIPTION", AlarmPath),

            // Cardinality.
            [TwoSummariesCase] = new Case(EventCalendar("SUMMARY:One", "SUMMARY:Two"), IcalDiagnosticKind.CardinalityViolation, "SUMMARY", EventPath),
            ["TwoUids"] = new Case(EventCalendar("UID:other@example.com"), IcalDiagnosticKind.CardinalityViolation, "UID", EventPath),
            ["DtEndAndDuration"] = new Case(EventCalendar("DTEND:20260101T100000Z", OneHour), IcalDiagnosticKind.CardinalityViolation,
                Duration, EventPath),
            ["DueAndDuration"] = new Case(Calendar(BeginTodo, "UID:t@example.com", NewYearStamp, NewYearStart,
                "DUE:20260102T090000Z", OneHour, EndTodo), IcalDiagnosticKind.CardinalityViolation, Duration, TodoPath),
            ["TwoCalendarColors"] = new Case(Calendar("COLOR:red", "COLOR:blue", BeginJournal, JournalUid, NewYearStamp,
                EndJournal), IcalDiagnosticKind.CardinalityViolation, "COLOR", CalendarPath),

            // Time zones.
            ["UndefinedTimeZone"] = new Case(EventCalendar("DTSTART;TZID=America/New_York:20260101T090000"), IcalDiagnosticKind.UndefinedTimeZone,
                DtStart, EventPath),

            // Values.
            ["InvalidDateTime"] = new Case(EventCalendar("DTSTART:20260230T090000Z"), IcalDiagnosticKind.InvalidValue, DtStart, EventPath),
            ["DisallowedValueType"] = new Case(EventCalendar("DTSTART;VALUE=INTEGER:5"), IcalDiagnosticKind.InvalidValue, DtStart, EventPath),
            ["FloatingDtStamp"] = new Case(Calendar(BeginEvent, OtherEventUid, "DTSTAMP:20260101T000000", NewYearStart,
                EndEvent), IcalDiagnosticKind.InvalidValue, "DTSTAMP", EventPath),
            ["DateEndForDateTimeStart"] = new Case(EventCalendar("DTEND;VALUE=DATE:20260102"), IcalDiagnosticKind.InvalidValue, "DTEND", EventPath),
            ["DateTimeUntilForDateStart"] = new Case(EventCalendar(DateStart, "RRULE:FREQ=DAILY;UNTIL=20260110T000000Z"),
                IcalDiagnosticKind.InvalidValue, RecurrenceRule, EventPath),
            ["ByHourForDateStart"] = new Case(EventCalendar(DateStart, "RRULE:FREQ=DAILY;BYHOUR=9"), IcalDiagnosticKind.InvalidValue,
                RecurrenceRule, EventPath),
            ["FloatingUntilForUtcStart"] = new Case(EventCalendar("RRULE:FREQ=DAILY;UNTIL=20260110T000000"), IcalDiagnosticKind.InvalidValue,
                RecurrenceRule, EventPath),
            [RefreshIntervalWithoutValueCase] = new Case(Calendar("REFRESH-INTERVAL:P1D", BeginJournal, JournalUid, NewYearStamp,
                EndJournal), IcalDiagnosticKind.InvalidValue, "REFRESH-INTERVAL", CalendarPath),
            [BinaryWithoutEncodingCase] = new Case(EventCalendar("ATTACH;VALUE=BINARY:AAAA"), IcalDiagnosticKind.InvalidValue, "ATTACH", EventPath),
            ["PriorityOutOfRange"] = new Case(EventCalendar("PRIORITY:10"), IcalDiagnosticKind.InvalidValue, "PRIORITY", EventPath),
            ["InvalidTextEscape"] = new Case(EventCalendar("SUMMARY:Back\\slash"), IcalDiagnosticKind.InvalidValue, "SUMMARY", EventPath),
            ["InvalidGeo"] = new Case(EventCalendar("GEO:37.386013"), IcalDiagnosticKind.InvalidValue, "GEO", EventPath),
            ["RelativeUrl"] = new Case(EventCalendar("URL:/calendar/event"), IcalDiagnosticKind.InvalidValue, "URL", EventPath),
            ["UtcObservanceStart"] = new Case(Calendar(BeginTimeZone, TimeZoneId, BeginStandard, "DTSTART:19700101T000000Z", ZeroOffsetFrom,
                ZeroOffsetTo, EndStandard, EndTimeZone), IcalDiagnosticKind.InvalidValue, DtStart, "VCALENDAR/VTIMEZONE[1]/STANDARD[1]"),
            ["EmptyTimeZoneId"] = new Case(Calendar(BeginTimeZone, "TZID:", BeginStandard, "DTSTART:19700101T000000", ZeroOffsetFrom,
                ZeroOffsetTo, EndStandard, EndTimeZone), IcalDiagnosticKind.InvalidValue, "TZID", TimeZonePath),
            ["TimeDurationForDateStart"] = new Case(EventCalendar(DateStart, OneHour), IcalDiagnosticKind.InvalidValue, Duration, EventPath)
        };

        /// <summary>
        /// The cases that an option accepts.
        /// </summary>
        public static TheoryData<string> Accommodated()
        {
            var data = new TheoryData<string>();
            foreach (var name in All.Keys)
            {
                data.Add(name);
            }

            return data;
        }

        /// <summary>
        /// The cases that xCal and jCal can hold.  Three cannot be written in either: the writer always writes VERSION, the value element or
        /// type identifier of REFRESH-INTERVAL gives it the VALUE parameter it lacked, and a binary value gives its property the ENCODING
        /// parameter it lacked.
        /// </summary>
        public static TheoryData<string> XcalAndJcalCases()
        {
            var data = new TheoryData<string>();
            foreach (var name in All.Keys)
            {
                if (name != MissingVersionCase && name != RefreshIntervalWithoutValueCase && name != BinaryWithoutEncodingCase)
                {
                    data.Add(name);
                }
            }

            return data;
        }
    }
}
