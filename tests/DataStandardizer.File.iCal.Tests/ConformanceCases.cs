using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Calendars that each break one rule of RFC 5545 or RFC 7986, with the kind of problem and the property it is reported for.
/// </summary>
internal static class ConformanceCases
{
    public sealed record Case(string Text, IcalDiagnosticKind Kind, string? PropertyName, string ComponentPath);

    public static readonly IReadOnlyDictionary<string, Case> All = new Dictionary<string, Case>
    {
        // Missing required properties and components.
        ["MissingUid"] = new(Calendar("BEGIN:VEVENT", "DTSTAMP:20260101T000000Z", "DTSTART:20260101T090000Z", "END:VEVENT"),
            IcalDiagnosticKind.MissingRequiredProperty, "UID", "VCALENDAR/VEVENT[1]"),
        ["MissingDtStamp"] = new(Calendar("BEGIN:VTODO", "UID:todo@example.com", "END:VTODO"),
            IcalDiagnosticKind.MissingRequiredProperty, "DTSTAMP", "VCALENDAR/VTODO[1]"),
        ["MissingProdId"] = new(IcalTestData.Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "BEGIN:VJOURNAL", "UID:j@example.com", "DTSTAMP:20260101T000000Z",
            "END:VJOURNAL", "END:VCALENDAR"), IcalDiagnosticKind.MissingRequiredProperty, "PRODID", "VCALENDAR"),
        ["MissingVersion"] = new(IcalTestData.Crlf("BEGIN:VCALENDAR", "PRODID:-//Test//Test//EN", "BEGIN:VJOURNAL", "UID:j@example.com",
            "DTSTAMP:20260101T000000Z", "END:VJOURNAL", "END:VCALENDAR"), IcalDiagnosticKind.MissingRequiredProperty, "VERSION", "VCALENDAR"),
        ["MissingEventStart"] = new(Calendar("BEGIN:VEVENT", "UID:e@example.com", "DTSTAMP:20260101T000000Z", "END:VEVENT"),
            IcalDiagnosticKind.MissingRequiredProperty, "DTSTART", "VCALENDAR/VEVENT[1]"),
        ["NoCalendarComponent"] = new(Calendar(), IcalDiagnosticKind.MissingRequiredProperty, null, "VCALENDAR"),
        ["NoObservance"] = new(Calendar("BEGIN:VTIMEZONE", "TZID:Zone", "END:VTIMEZONE"),
            IcalDiagnosticKind.MissingRequiredProperty, null, "VCALENDAR/VTIMEZONE[1]"),
        ["DurationWithoutRepeat"] = new(EventCalendar("BEGIN:VALARM", "ACTION:AUDIO", "TRIGGER:-PT5M", "DURATION:PT5M", "END:VALARM"),
            IcalDiagnosticKind.MissingRequiredProperty, "REPEAT", "VCALENDAR/VEVENT[1]/VALARM[1]"),
        ["DisplayWithoutDescription"] = new(EventCalendar("BEGIN:VALARM", "ACTION:DISPLAY", "TRIGGER:-PT5M", "END:VALARM"),
            IcalDiagnosticKind.MissingRequiredProperty, "DESCRIPTION", "VCALENDAR/VEVENT[1]/VALARM[1]"),

        // Cardinality.
        ["TwoSummaries"] = new(EventCalendar("SUMMARY:One", "SUMMARY:Two"), IcalDiagnosticKind.CardinalityViolation, "SUMMARY",
            "VCALENDAR/VEVENT[1]"),
        ["TwoUids"] = new(EventCalendar("UID:other@example.com"), IcalDiagnosticKind.CardinalityViolation, "UID", "VCALENDAR/VEVENT[1]"),
        ["DtEndAndDuration"] = new(EventCalendar("DTEND:20260101T100000Z", "DURATION:PT1H"), IcalDiagnosticKind.CardinalityViolation,
            "DURATION", "VCALENDAR/VEVENT[1]"),
        ["DueAndDuration"] = new(Calendar("BEGIN:VTODO", "UID:t@example.com", "DTSTAMP:20260101T000000Z", "DTSTART:20260101T090000Z",
            "DUE:20260102T090000Z", "DURATION:PT1H", "END:VTODO"), IcalDiagnosticKind.CardinalityViolation, "DURATION", "VCALENDAR/VTODO[1]"),
        ["TwoCalendarColors"] = new(Calendar("COLOR:red", "COLOR:blue", "BEGIN:VJOURNAL", "UID:j@example.com", "DTSTAMP:20260101T000000Z",
            "END:VJOURNAL"), IcalDiagnosticKind.CardinalityViolation, "COLOR", "VCALENDAR"),

        // Time zones.
        ["UndefinedTimeZone"] = new(EventCalendar("DTSTART;TZID=America/New_York:20260101T090000"), IcalDiagnosticKind.UndefinedTimeZone,
            "DTSTART", "VCALENDAR/VEVENT[1]"),

        // Values.
        ["InvalidDateTime"] = new(EventCalendar("DTSTART:20260230T090000Z"), IcalDiagnosticKind.InvalidValue, "DTSTART", "VCALENDAR/VEVENT[1]"),
        ["DisallowedValueType"] = new(EventCalendar("DTSTART;VALUE=INTEGER:5"), IcalDiagnosticKind.InvalidValue, "DTSTART",
            "VCALENDAR/VEVENT[1]"),
        ["FloatingDtStamp"] = new(Calendar("BEGIN:VEVENT", "UID:e@example.com", "DTSTAMP:20260101T000000", "DTSTART:20260101T090000Z",
            "END:VEVENT"), IcalDiagnosticKind.InvalidValue, "DTSTAMP", "VCALENDAR/VEVENT[1]"),
        ["DateEndForDateTimeStart"] = new(EventCalendar("DTEND;VALUE=DATE:20260102"), IcalDiagnosticKind.InvalidValue, "DTEND",
            "VCALENDAR/VEVENT[1]"),
        ["DateTimeUntilForDateStart"] = new(EventCalendar("DTSTART;VALUE=DATE:20260101", "RRULE:FREQ=DAILY;UNTIL=20260110T000000Z"),
            IcalDiagnosticKind.InvalidValue, "RRULE", "VCALENDAR/VEVENT[1]"),
        ["ByHourForDateStart"] = new(EventCalendar("DTSTART;VALUE=DATE:20260101", "RRULE:FREQ=DAILY;BYHOUR=9"), IcalDiagnosticKind.InvalidValue,
            "RRULE", "VCALENDAR/VEVENT[1]"),
        ["FloatingUntilForUtcStart"] = new(EventCalendar("RRULE:FREQ=DAILY;UNTIL=20260110T000000"), IcalDiagnosticKind.InvalidValue, "RRULE",
            "VCALENDAR/VEVENT[1]"),
        ["RefreshIntervalWithoutValue"] = new(Calendar("REFRESH-INTERVAL:P1D", "BEGIN:VJOURNAL", "UID:j@example.com", "DTSTAMP:20260101T000000Z",
            "END:VJOURNAL"), IcalDiagnosticKind.InvalidValue, "REFRESH-INTERVAL", "VCALENDAR"),
        ["BinaryWithoutEncoding"] = new(EventCalendar("ATTACH;VALUE=BINARY:AAAA"), IcalDiagnosticKind.InvalidValue, "ATTACH",
            "VCALENDAR/VEVENT[1]"),
        ["PriorityOutOfRange"] = new(EventCalendar("PRIORITY:10"), IcalDiagnosticKind.InvalidValue, "PRIORITY", "VCALENDAR/VEVENT[1]"),
        ["InvalidTextEscape"] = new(EventCalendar("SUMMARY:Back\\slash"), IcalDiagnosticKind.InvalidValue, "SUMMARY", "VCALENDAR/VEVENT[1]"),
        ["InvalidGeo"] = new(EventCalendar("GEO:37.386013"), IcalDiagnosticKind.InvalidValue, "GEO", "VCALENDAR/VEVENT[1]"),
        ["RelativeUrl"] = new(EventCalendar("URL:/calendar/event"), IcalDiagnosticKind.InvalidValue, "URL", "VCALENDAR/VEVENT[1]"),
        ["UtcObservanceStart"] = new(Calendar("BEGIN:VTIMEZONE", "TZID:Zone", "BEGIN:STANDARD", "DTSTART:19700101T000000Z", "TZOFFSETFROM:+0000",
            "TZOFFSETTO:+0000", "END:STANDARD", "END:VTIMEZONE"), IcalDiagnosticKind.InvalidValue, "DTSTART", "VCALENDAR/VTIMEZONE[1]/STANDARD[1]"),
        ["EmptyTimeZoneId"] = new(Calendar("BEGIN:VTIMEZONE", "TZID:", "BEGIN:STANDARD", "DTSTART:19700101T000000", "TZOFFSETFROM:+0000",
            "TZOFFSETTO:+0000", "END:STANDARD", "END:VTIMEZONE"), IcalDiagnosticKind.InvalidValue, "TZID", "VCALENDAR/VTIMEZONE[1]"),
        ["TimeDurationForDateStart"] = new(EventCalendar("DTSTART;VALUE=DATE:20260101", "DURATION:PT1H"), IcalDiagnosticKind.InvalidValue,
            "DURATION", "VCALENDAR/VEVENT[1]")
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
    /// Gets the options that accept a kind of problem.
    /// </summary>
    public static IcalOptions OptionsAccepting(IcalDiagnosticKind kind)
    {
        return kind switch
        {
            IcalDiagnosticKind.MissingRequiredProperty => new IcalOptions { AllowMissingRequiredProperties = true },
            IcalDiagnosticKind.CardinalityViolation => new IcalOptions { AllowCardinalityViolations = true },
            IcalDiagnosticKind.UndefinedTimeZone => new IcalOptions { AllowUndefinedTimeZones = true },
            IcalDiagnosticKind.InvalidValue => new IcalOptions { AllowInvalidValues = true },
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    /// <summary>
    /// Gets options with every accommodation turned on except the one that accepts a kind of problem.
    /// </summary>
    public static IcalOptions OptionsAcceptingAllBut(IcalDiagnosticKind kind)
    {
        return new IcalOptions
        {
            AllowMissingRequiredProperties = kind != IcalDiagnosticKind.MissingRequiredProperty,
            AllowCardinalityViolations = kind != IcalDiagnosticKind.CardinalityViolation,
            AllowUndefinedTimeZones = kind != IcalDiagnosticKind.UndefinedTimeZone,
            AllowInvalidValues = kind != IcalDiagnosticKind.InvalidValue
        };
    }
}
