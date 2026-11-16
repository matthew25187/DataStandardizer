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
        public const string TwoFormatTypesCase = "TwoFormatTypes";
        public const string TwoLanguagesCase = "TwoLanguages";
        public const string TwoEncodingsCase = "TwoEncodings";
        public const string TwoValueParametersCase = "TwoValueParameters";
        public const string ValueParameterWithTwoValuesCase = "ValueParameterWithTwoValues";
        public const string QuotedFormatTypeCase = "QuotedFormatType";
        public const string QuotedTimeZoneIdCase = "QuotedTimeZoneId";
        public const string QuotedLanguageOnExtensionPropertyCase = "QuotedLanguageOnExtensionProperty";
        public const string QuotedSecondFeatureCase = "QuotedSecondFeature";
        public const string UnescapedCommaInTextCase = "UnescapedCommaInText";
        public const string UnescapedSemicolonInTextCase = "UnescapedSemicolonInText";
        public const string UnescapedSemicolonInCategoriesCase = "UnescapedSemicolonInCategories";
        public const string UnescapedCommaInRequestStatusCase = "UnescapedCommaInRequestStatus";
        public const string UnescapedSemicolonInRequestStatusDataCase = "UnescapedSemicolonInRequestStatusData";

        // ref. RFC 5545 §3.3.11: a TEXT value with its comma and semicolon escaped.
        private const string EscapedTextSeparators = "SUMMARY:Lunch\\, then review\\; bring notes";

        // ref. RFC 5545 §3.8.1.2, example.
        private const string Rfc5545Categories = "CATEGORIES:APPOINTMENT,EDUCATION";

        // ref. RFC 5545 §3.8.1.10, example.
        private const string Rfc5545Resources = "RESOURCES:EASEL,PROJECTOR,VCR";

        // ref. RFC 5545 §3.8.8.3, examples: a statcode and statdesc, with extdata, and with escaped separators in the statdesc and extdata.
        private const string Rfc5545RequestStatusSuccess = "REQUEST-STATUS:2.0;Success";
        private const string Rfc5545RequestStatusWithData = "REQUEST-STATUS:3.1;Invalid property value;DTSTART:96-Apr-01";
        private const string Rfc5545RequestStatusWithEscapes =
            "REQUEST-STATUS:2.8; Success\\, repeating event ignored. Scheduled as a single event.;RRULE:FREQ=WEEKLY\\;INTERVAL=2";

        // ref. RFC 5545 §3.6.5, example: the time zone rules for New York City from 2007, an offset of -0500 in winter and -0400 in summer.
        private static readonly string[] Rfc5545NewYorkTimeZone =
        {
            BeginTimeZone, "TZID:America/New_York", "LAST-MODIFIED:20050809T050000Z", BeginDaylight, "DTSTART:20070311T020000",
            "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU", "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT", EndDaylight, BeginStandard,
            "DTSTART:20071104T020000", "RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU", "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500", "TZNAME:EST",
            EndStandard, EndTimeZone
        };

        // 09:00 in New York on 1 January 2026, which is 14:00 UTC.
        private const string NewYorkNewYearStart = "DTSTART;TZID=America/New_York:20260101T090000";

        // 02:30 in New York on 8 March 2026, in the hour skipped when clocks go forward, which RFC 5545 §3.3.5 reads as 03:30 EDT.
        private const string NewYorkGapStart = "DTSTART;TZID=America/New_York:20260308T023000";
        private const string NewYorkGapEnd = "DTEND;TZID=America/New_York:20260308T023000";

        private const string UtcEndBeforeStart = "DTEND:20260101T080000Z";
        private const string UtcEndAfterStart = "DTEND:20260101T100000Z";
        private const string FloatingStart = "DTSTART:20260101T090000";
        private const string FloatingEndAfterStart = "DTEND:20260101T100000";

        // ref. RFC 5545 §3.8.1.11: a status a VJOURNAL allows and a VEVENT does not.
        private const string DraftStatus = "STATUS:DRAFT";
        private const string TodoUid = "UID:t@example.com";
        private const string FreeBusyUid = "UID:f@example.com";
        private const string FreeBusyPath = "VCALENDAR/VFREEBUSY[1]";
        private const string DtEnd = "DTEND";
        private const string Due = "DUE";
        private const string Status = "STATUS";
        private const string Attachment = "ATTACH";
        private const string DtStart = "DTSTART";
        private const string Duration = "DURATION";
        private const string RequestStatus = "REQUEST-STATUS";
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
                    AllowInvalidValues = kind == IcalDiagnosticKind.InvalidValue,
                    AllowQuotedParameterValues = kind == IcalDiagnosticKind.QuotedParameterValue
                };
                AcceptingAllButOptions = new IcalOptions
                {
                    AllowMissingRequiredProperties = kind != IcalDiagnosticKind.MissingRequiredProperty,
                    AllowCardinalityViolations = kind != IcalDiagnosticKind.CardinalityViolation,
                    AllowUndefinedTimeZones = kind != IcalDiagnosticKind.UndefinedTimeZone,
                    AllowInvalidValues = kind != IcalDiagnosticKind.InvalidValue,
                    AllowQuotedParameterValues = kind != IcalDiagnosticKind.QuotedParameterValue
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
            [BinaryWithoutEncodingCase] = new Case(EventCalendar("ATTACH;VALUE=BINARY:AAAA"), IcalDiagnosticKind.InvalidValue, Attachment, EventPath),
            ["PriorityOutOfRange"] = new Case(EventCalendar("PRIORITY:10"), IcalDiagnosticKind.InvalidValue, "PRIORITY", EventPath),
            ["InvalidTextEscape"] = new Case(EventCalendar("SUMMARY:Back\\slash"), IcalDiagnosticKind.InvalidValue, "SUMMARY", EventPath),

            // ref. RFC 5545 §3.3.11: TSAFE-CHAR excludes "," and ";", so TEXT escapes them except where the property's grammar separates with them.
            [UnescapedCommaInTextCase] = new Case(EventCalendar("SUMMARY:Lunch, then review"), IcalDiagnosticKind.InvalidValue, "SUMMARY",
                EventPath),
            [UnescapedSemicolonInTextCase] = new Case(EventCalendar("SUMMARY:Lunch; review"), IcalDiagnosticKind.InvalidValue, "SUMMARY",
                EventPath),
            [UnescapedSemicolonInCategoriesCase] = new Case(EventCalendar("CATEGORIES:APPOINTMENT;EDUCATION"), IcalDiagnosticKind.InvalidValue,
                "CATEGORIES", EventPath),
            [UnescapedCommaInRequestStatusCase] = new Case(EventCalendar("REQUEST-STATUS:2.0;Success, mostly"), IcalDiagnosticKind.InvalidValue,
                RequestStatus, EventPath),
            [UnescapedSemicolonInRequestStatusDataCase] = new Case(EventCalendar("REQUEST-STATUS:3.1;Invalid property value;DTSTART;VALUE=DATE"),
                IcalDiagnosticKind.InvalidValue, RequestStatus, EventPath),

            // ref. RFC 5545 §3.8.8.3: rstatus = statcode ";" statdesc [";" extdata], statcode = 1*DIGIT 1*2("." 1*DIGIT).
            ["RequestStatusWithoutDescription"] = new Case(EventCalendar("REQUEST-STATUS:2.0"), IcalDiagnosticKind.InvalidValue, RequestStatus,
                EventPath),
            ["RequestStatusWithoutCode"] = new Case(EventCalendar("REQUEST-STATUS:Success;Done"), IcalDiagnosticKind.InvalidValue, RequestStatus,
                EventPath),
            ["RequestStatusCodeWithOneLevel"] = new Case(EventCalendar("REQUEST-STATUS:2;Success"), IcalDiagnosticKind.InvalidValue, RequestStatus,
                EventPath),
            ["RequestStatusCodeWithFourLevels"] = new Case(EventCalendar("REQUEST-STATUS:2.0.1.1;Success"), IcalDiagnosticKind.InvalidValue,
                RequestStatus, EventPath),
            ["InvalidGeo"] = new Case(EventCalendar("GEO:37.386013"), IcalDiagnosticKind.InvalidValue, "GEO", EventPath),
            ["RelativeUrl"] = new Case(EventCalendar("URL:/calendar/event"), IcalDiagnosticKind.InvalidValue, "URL", EventPath),
            ["UtcObservanceStart"] = new Case(Calendar(BeginTimeZone, TimeZoneId, BeginStandard, "DTSTART:19700101T000000Z", ZeroOffsetFrom,
                ZeroOffsetTo, EndStandard, EndTimeZone), IcalDiagnosticKind.InvalidValue, DtStart, "VCALENDAR/VTIMEZONE[1]/STANDARD[1]"),
            ["EmptyTimeZoneId"] = new Case(Calendar(BeginTimeZone, "TZID:", BeginStandard, "DTSTART:19700101T000000", ZeroOffsetFrom,
                ZeroOffsetTo, EndStandard, EndTimeZone), IcalDiagnosticKind.InvalidValue, "TZID", TimeZonePath),
            ["TimeDurationForDateStart"] = new Case(EventCalendar(DateStart, OneHour), IcalDiagnosticKind.InvalidValue, Duration, EventPath),

            // ref. RFC 5545 §3.8.2.2 and §3.8.2.3: DTEND and DUE MUST be later in time than DTSTART.
            ["EndBeforeStart"] = new Case(EventCalendar(UtcEndBeforeStart), IcalDiagnosticKind.InvalidValue, DtEnd, EventPath),
            ["EndAtStart"] = new Case(EventCalendar("DTEND:20260101T090000Z"), IcalDiagnosticKind.InvalidValue, DtEnd, EventPath),
            ["DateEndAtDateStart"] = new Case(EventCalendar(DateStart, "DTEND;VALUE=DATE:20260101"), IcalDiagnosticKind.InvalidValue, DtEnd,
                EventPath),
            ["FloatingEndBeforeFloatingStart"] = new Case(EventCalendar(FloatingStart, "DTEND:20260101T080000"), IcalDiagnosticKind.InvalidValue,
                DtEnd, EventPath),
            ["ZonedEndAtZonedStartInGap"] = new Case(NewYorkEventCalendar(NewYorkGapStart, NewYorkGapEnd), IcalDiagnosticKind.InvalidValue, DtEnd, EventPath),
            ["ZonedEndHourBeforeZonedStart"] = new Case(NewYorkEventCalendar(NewYorkNewYearStart, "DTEND;TZID=America/New_York:20260101T080000"),
                IcalDiagnosticKind.InvalidValue, DtEnd, EventPath),
            ["UtcEndBeforeZonedStart"] = new Case(NewYorkEventCalendar(NewYorkNewYearStart, "DTEND:20260101T120000Z"),
                IcalDiagnosticKind.InvalidValue, DtEnd, EventPath),
            ["DueBeforeStart"] = new Case(Calendar(BeginTodo, TodoUid, NewYearStamp, NewYearStart, "DUE:20260101T080000Z", EndTodo),
                IcalDiagnosticKind.InvalidValue, Due, TodoPath),

            // ref. RFC 5545 §3.8.2.2 and §3.8.2.3: a date with local time if and only if DTSTART is one.
            ["UtcEndForFloatingStart"] = new Case(EventCalendar(FloatingStart, UtcEndAfterStart), IcalDiagnosticKind.InvalidValue, DtEnd, EventPath),
            ["FloatingDueForUtcStart"] = new Case(Calendar(BeginTodo, TodoUid, NewYearStamp, NewYearStart, "DUE:20260101T100000", EndTodo),
                IcalDiagnosticKind.InvalidValue, Due, TodoPath),

            // ref. RFC 5545 §3.8.2.4 and §3.8.2.2: in a VFREEBUSY, DTSTART and DTEND MUST be in UTC, and DTEND later than DTSTART.
            ["FloatingFreeBusyStart"] = new Case(Calendar(BeginFreeBusy, FreeBusyUid, NewYearStamp, FloatingStart, UtcEndAfterStart, EndFreeBusy),
                IcalDiagnosticKind.InvalidValue, DtStart, FreeBusyPath),
            ["DateFreeBusyStart"] = new Case(Calendar(BeginFreeBusy, FreeBusyUid, NewYearStamp, DateStart, UtcEndAfterStart, EndFreeBusy),
                IcalDiagnosticKind.InvalidValue, DtStart, FreeBusyPath),
            ["FloatingFreeBusyEnd"] = new Case(Calendar(BeginFreeBusy, FreeBusyUid, NewYearStamp, NewYearStart, FloatingEndAfterStart,
                EndFreeBusy), IcalDiagnosticKind.InvalidValue, DtEnd, FreeBusyPath),
            ["FreeBusyEndBeforeStart"] = new Case(Calendar(BeginFreeBusy, FreeBusyUid, NewYearStamp, NewYearStart, UtcEndBeforeStart, EndFreeBusy),
                IcalDiagnosticKind.InvalidValue, DtEnd, FreeBusyPath),

            // ref. RFC 5545 §3.8.1.11, §3.8.2.7 and §3.7.1: closed sets of values, with no iana-token or x-name.
            ["UnknownStatus"] = new Case(EventCalendar("STATUS:BANANA"), IcalDiagnosticKind.InvalidValue, Status, EventPath),
            ["JournalStatusInEvent"] = new Case(EventCalendar(DraftStatus), IcalDiagnosticKind.InvalidValue, Status, EventPath),
            ["EventStatusInTodo"] = new Case(Calendar(BeginTodo, TodoUid, NewYearStamp, "STATUS:TENTATIVE", EndTodo), IcalDiagnosticKind.InvalidValue,
                Status, TodoPath),
            ["UnknownTransparency"] = new Case(EventCalendar("TRANSP:MAYBE"), IcalDiagnosticKind.InvalidValue, "TRANSP", EventPath),
            ["UnknownCalendarScale"] = new Case(Calendar("CALSCALE:JULIAN", BeginJournal, JournalUid, NewYearStamp, EndJournal),
                IcalDiagnosticKind.InvalidValue, "CALSCALE", CalendarPath),

            // The FMTTYPE parameter, ref. RFC 5545 §3.2.8.
            ["FormatTypeWithoutSubtype"] = new Case(EventCalendar("ATTACH;FMTTYPE=audio:http://example.com/a"), IcalDiagnosticKind.InvalidValue,
                Attachment, EventPath),
            ["FormatTypeWithTwoValues"] = new Case(EventCalendar("ATTACH;FMTTYPE=audio/basic,image/png:http://example.com/a"),
                IcalDiagnosticKind.InvalidValue, Attachment, EventPath),
            [TwoFormatTypesCase] = new Case(EventCalendar("ATTACH;FMTTYPE=audio/basic;FMTTYPE=image/png:http://example.com/a"),
                IcalDiagnosticKind.InvalidValue, Attachment, EventPath),
            ["FormatTypeOnImage"] = new Case(EventCalendar("IMAGE;VALUE=URI;FMTTYPE=png:http://example.com/a.png"), IcalDiagnosticKind.InvalidValue,
                "IMAGE", EventPath),
            ["FormatTypeOnExtensionProperty"] = new Case(EventCalendar("X-ABC-MMSUBJ;VALUE=URI;FMTTYPE=audio:http://www.example.org/mysubj.au"),
                IcalDiagnosticKind.InvalidValue, "X-ABC-MMSUBJ", EventPath),

            // Other parameters, ref. RFC 5545 §3.2 and the rule of each property that a parameter "MUST NOT occur more than once".
            [TwoLanguagesCase] = new Case(EventCalendar("SUMMARY;LANGUAGE=en;LANGUAGE=de:x"), IcalDiagnosticKind.InvalidValue, "SUMMARY", EventPath),
            [TwoEncodingsCase] = new Case(EventCalendar("ATTACH;ENCODING=BASE64;ENCODING=8BIT;VALUE=BINARY:AAAA"), IcalDiagnosticKind.InvalidValue,
                Attachment, EventPath),
            [TwoValueParametersCase] = new Case(EventCalendar("ATTACH;VALUE=URI;VALUE=BINARY:http://example.com/a"), IcalDiagnosticKind.InvalidValue,
                Attachment, EventPath),
            [ValueParameterWithTwoValuesCase] = new Case(EventCalendar("ATTACH;VALUE=URI,BINARY:http://example.com/a"), IcalDiagnosticKind.InvalidValue,
                Attachment, EventPath),
            ["CommonNameWithUnquotedComma"] = new Case(EventCalendar("ATTENDEE;CN=Doe, John:mailto:a@example.com"), IcalDiagnosticKind.InvalidValue,
                "ATTENDEE", EventPath),
            ["LanguageWithTwoValuesOnExtensionProperty"] = new Case(EventCalendar("X-ABC;LANGUAGE=en,de:x"), IcalDiagnosticKind.InvalidValue, "X-ABC",
                EventPath),

            // Quoted values of parameters whose grammar in RFC 5545 §3.2 or RFC 7986 §6 has no quoted-string.
            [QuotedFormatTypeCase] = new Case(EventCalendar("ATTACH;FMTTYPE=\"audio/basic\":http://example.com/a"),
                IcalDiagnosticKind.QuotedParameterValue, Attachment, EventPath),
            [QuotedTimeZoneIdCase] = new Case(Calendar(BeginTimeZone, TimeZoneId, BeginStandard, "DTSTART:19700101T000000", ZeroOffsetFrom,
                    ZeroOffsetTo, EndStandard, EndTimeZone, BeginEvent, OtherEventUid, NewYearStamp, "DTSTART;TZID=\"Zone\":20260101T090000",
                    EndEvent),
                IcalDiagnosticKind.QuotedParameterValue, DtStart, EventPath),
            [QuotedLanguageOnExtensionPropertyCase] = new Case(EventCalendar("X-ABC;LANGUAGE=\"en\":x"), IcalDiagnosticKind.QuotedParameterValue,
                "X-ABC", EventPath),
            [QuotedSecondFeatureCase] = new Case(EventCalendar("CONFERENCE;VALUE=URI;FEATURE=AUDIO,\"VIDEO\":https://example.com/a"),
                IcalDiagnosticKind.QuotedParameterValue, "CONFERENCE", EventPath)
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
        /// The cases whose problem is reported for a property.
        /// </summary>
        public static TheoryData<string> AccommodatedWithPropertyName()
        {
            var data = new TheoryData<string>();
            foreach (var pair in All)
            {
                if (pair.Value.PropertyName != null)
                {
                    data.Add(pair.Key);
                }
            }

            return data;
        }

        /// <summary>
        /// The cases whose problem is reported for a component, not a property.
        /// </summary>
        public static TheoryData<string> AccommodatedWithoutPropertyName()
        {
            var data = new TheoryData<string>();
            foreach (var pair in All)
            {
                if (pair.Value.PropertyName == null)
                {
                    data.Add(pair.Key);
                }
            }

            return data;
        }

        /// <summary>
        /// The cases that xCal can hold.  Five cannot be written in it: the writer always writes VERSION, the value element of REFRESH-INTERVAL
        /// gives it the VALUE parameter it lacked, a binary value gives its property the ENCODING parameter it lacked, and the value element of
        /// a property takes the place of its VALUE parameters, however many there are or however many values they have.  Nor can a quoted
        /// parameter value, since xCal has no quoting, or an unescaped comma or semicolon in a TEXT value, since xCal text has no escaping.
        /// </summary>
        public static TheoryData<string> XcalCases()
        {
            var data = new TheoryData<string>();
            foreach (var name in All.Keys)
            {
                if (name != MissingVersionCase && name != RefreshIntervalWithoutValueCase && name != BinaryWithoutEncodingCase &&
                    name != TwoValueParametersCase && name != ValueParameterWithTwoValuesCase && !IsQuotedParameterValueCase(name) &&
                    !IsUnescapedTextSeparatorCase(name))
                {
                    data.Add(name);
                }
            }

            return data;
        }

        /// <summary>
        /// The cases that jCal can hold: those of <see cref="XcalCases"/>, except for a parameter that appears twice, since each parameter is a key
        /// of a JSON object (RFC 7265 §3.5).
        /// </summary>
        public static TheoryData<string> JcalCases()
        {
            var data = new TheoryData<string>();
            foreach (var name in All.Keys)
            {
                if (name != MissingVersionCase && name != RefreshIntervalWithoutValueCase && name != BinaryWithoutEncodingCase &&
                    name != TwoValueParametersCase && name != ValueParameterWithTwoValuesCase && name != TwoFormatTypesCase &&
                    name != TwoLanguagesCase && name != TwoEncodingsCase && !IsQuotedParameterValueCase(name) && !IsUnescapedTextSeparatorCase(name))
                {
                    data.Add(name);
                }
            }

            return data;
        }

        private static bool IsQuotedParameterValueCase(string name)
        {
            return name == QuotedFormatTypeCase || name == QuotedTimeZoneIdCase || name == QuotedLanguageOnExtensionPropertyCase ||
                   name == QuotedSecondFeatureCase;
        }

        /// <summary>
        /// The cases with an unescaped comma or semicolon in a TEXT value, which jCal and xCal cannot hold, since their text has no escaping.
        /// </summary>
        public static TheoryData<string> UnescapedTextSeparatorCases()
        {
            return new TheoryData<string>
            {
                UnescapedCommaInTextCase, UnescapedSemicolonInTextCase, UnescapedSemicolonInCategoriesCase, UnescapedCommaInRequestStatusCase,
                UnescapedSemicolonInRequestStatusDataCase
            };
        }

        /// <summary>
        /// Properties whose TEXT value has a comma or semicolon that is either escaped or one that the property's grammar uses as a separator.
        /// </summary>
        public static TheoryData<string> TextSeparatorsTheGrammarAllows()
        {
            return new TheoryData<string>
            {
                EscapedTextSeparators, Rfc5545Categories, Rfc5545Resources, Rfc5545RequestStatusSuccess, Rfc5545RequestStatusWithData,
                Rfc5545RequestStatusWithEscapes
            };
        }

        /// <summary>
        /// Calendars whose end and due times, local times and enumerated values RFC 5545 allows, close to ones it does not.
        /// </summary>
        public static TheoryData<string> OrderingsAndValuesTheStandardAllows()
        {
            return new TheoryData<string>
            {
                EventCalendar(UtcEndAfterStart),
                EventCalendar(DateStart, "DTEND;VALUE=DATE:20260102"),
                EventCalendar(FloatingStart, FloatingEndAfterStart),

                // ref. RFC 5545 §3.3.12: the leap second 23:59:60 falls before midnight UTC.
                EventCalendar("DTSTART:20161231T235960Z", "DTEND:20170101T000000Z"),
                Calendar(BeginTodo, TodoUid, NewYearStamp, NewYearStart, "DUE:20260101T100000Z", EndTodo),

                // ref. RFC 5545 §3.3.5: an end in the hour skipped when clocks go forward is read with the offset before it, so 02:30 is 03:30 EDT.
                NewYorkEventCalendar("DTSTART;TZID=America/New_York:20260308T030000", NewYorkGapEnd),

                // 09:00 in New York is 14:00 UTC, and a date with UTC time may end a date with local time and a time zone reference.
                NewYorkEventCalendar(NewYorkNewYearStart, "DTEND:20260101T150000Z"),
                Calendar(BeginFreeBusy, FreeBusyUid, NewYearStamp, NewYearStart, UtcEndAfterStart, EndFreeBusy),

                // ref. RFC 5545 §2: enumerated property values are case-insensitive.
                EventCalendar("STATUS:confirmed"),
                Calendar(BeginTodo, TodoUid, NewYearStamp, "STATUS:IN-PROCESS", EndTodo),
                Calendar(BeginJournal, JournalUid, NewYearStamp, DraftStatus, EndJournal),
                EventCalendar("TRANSP:TRANSPARENT"),
                Calendar("CALSCALE:GREGORIAN", BeginJournal, JournalUid, NewYearStamp, EndJournal),

                // ref. RFC 5545 §3.6: an x-comp holds any contentline, and §3.8.1.11, §3.8.2.7 and §3.7.1 define STATUS, TRANSP and CALSCALE
                // in other components only.
                Calendar(BeginJournal, JournalUid, NewYearStamp, EndJournal, "BEGIN:X-ABC-ITEM", "STATUS:ARCHIVED", "TRANSP:MAYBE",
                    "CALSCALE:JULIAN", "END:X-ABC-ITEM")
            };
        }

        /// <summary>
        /// Builds a calendar holding the New York time zone of RFC 5545 §3.6.5 and one VEVENT with these lines.
        /// </summary>
        private static string NewYorkEventCalendar(params string[] eventLines)
        {
            return Calendar(IcalTestData.Lines(Rfc5545NewYorkTimeZone, new[] { BeginEvent, OtherEventUid, NewYearStamp }, eventLines,
                new[] { EndEvent }));
        }

        private static bool IsUnescapedTextSeparatorCase(string name)
        {
            return name == UnescapedCommaInTextCase || name == UnescapedSemicolonInTextCase || name == UnescapedSemicolonInCategoriesCase ||
                   name == UnescapedCommaInRequestStatusCase || name == UnescapedSemicolonInRequestStatusDataCase;
        }
    }
}
