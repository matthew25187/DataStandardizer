using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Whole calendars for <see cref="IcalReader"/> and <see cref="IcalWriter"/>, with CRLF line breaks, and the options each needs to be read.
/// </summary>
internal static class CalendarFixtures
{
    /// <summary>
    /// RFC 5545 §4, fourth example: a VJOURNAL with a long, folded and escaped DESCRIPTION.
    /// </summary>
    public static readonly string Rfc5545Journal = Crlf(
        "BEGIN:VCALENDAR",
        "VERSION:2.0",
        "PRODID:-//ABC Corporation//NONSGML My Product//EN",
        "BEGIN:VJOURNAL",
        "DTSTAMP:19970324T120000Z",
        "UID:uid5@example.com",
        "ORGANIZER:mailto:jsmith@example.com",
        "STATUS:DRAFT",
        "CLASS:PUBLIC",
        "CATEGORIES:Project Report,XYZ,Weekly Meeting",
        "DESCRIPTION:Project xyz Review Meeting Minutes\\n",
        " Agenda\\n1. Review of project version 1.0 requirements.\\n2.",
        "  Definition",
        " of project processes.\\n3. Review of project schedule.\\n",
        " Participants: John Smith\\, Jane Doe\\, Jim Dandy\\n-It was",
        "  decided that the requirements need to be signed off by",
        " product marketing.\\n-Project processes were accepted.\\n",
        " -Project schedule needs to account for scheduled freeze",
        "  of the Ical.",
        "END:VJOURNAL",
        "END:VCALENDAR");

    /// <summary>
    /// RFC 5545 §4, third example, with the <c>VALUE=DATE-TIME</c> parameter that §3.8.6.3 requires of an absolute <c>TRIGGER</c>, and the
    /// RFC's example omits.
    /// </summary>
    public static readonly string Rfc5545TodoCorrected = RfcFixtures.Rfc5545Todo.Replace("TRIGGER:19980403T120000Z",
        "TRIGGER;VALUE=DATE-TIME:19980403T120000Z", StringComparison.Ordinal);

    /// <summary>
    /// The calendar properties of RFC 7986 §5, with the component properties COLOR, IMAGE and CONFERENCE on a VEVENT, using the RFC's
    /// examples.
    /// </summary>
    public static readonly string Rfc7986 = Crlf(
        "BEGIN:VCALENDAR",
        "VERSION:2.0",
        "PRODID:-//Example Corp.//CalDAV Client//EN",
        "NAME:Company Vacation Days",
        "DESCRIPTION:The description of the calendar.",
        "UID:5FC53010-1267-4F8E-BC28-1D7AE55A7C99",
        "URL:http://example.com/calendar/vacation",
        "LAST-MODIFIED:20110515T000000Z",
        "CATEGORIES:MEETING",
        "REFRESH-INTERVAL;VALUE=DURATION:P1W",
        "SOURCE;VALUE=URI:https://example.com/holidays.ics",
        "COLOR:turquoise",
        "IMAGE;VALUE=URI;DISPLAY=BADGE;FMTTYPE=image/png:http://example.com/images/party.png",
        "BEGIN:VEVENT",
        "DTSTAMP:20160101T120000Z",
        "UID:conference-1@example.com",
        "DTSTART:20160102T150000Z",
        "SUMMARY:Planning meeting",
        "COLOR:red",
        "IMAGE;VALUE=URI;DISPLAY=BADGE;FMTTYPE=image/png:http://example.com/images/party.png",
        "CONFERENCE;VALUE=URI;FEATURE=PHONE,MODERATOR;LABEL=Moderator dial-in:tel:+1-412-555-0123,,,654321",
        "CONFERENCE;VALUE=URI;FEATURE=PHONE;LABEL=Attendee dial-in:tel:+1-412-555-0123,,,555123",
        "CONFERENCE;VALUE=URI;FEATURE=PHONE;LABEL=Attendee dial-in:tel:+1-888-555-0456,,,555123",
        "CONFERENCE;VALUE=URI;FEATURE=CHAT;LABEL=Chat room:xmpp:chat-123@conference.example.com",
        "CONFERENCE;VALUE=URI;FEATURE=AUDIO,VIDEO;LABEL=Attendee dial-in:https://chat.example.com/audio?id=123456",
        "END:VEVENT",
        "END:VCALENDAR");

    /// <summary>
    /// An export in the style of Google Calendar: METHOD:PUBLISH, X-WR- calendar properties, an IANA time zone, a weekly event with a
    /// reminder, and an all-day event.
    /// </summary>
    public static readonly string GoogleExport = Crlf(
        "BEGIN:VCALENDAR",
        "PRODID:-//Google Inc//Google Calendar 70.9054//EN",
        "VERSION:2.0",
        "CALSCALE:GREGORIAN",
        "METHOD:PUBLISH",
        "X-WR-CALNAME:Work",
        "X-WR-TIMEZONE:Pacific/Auckland",
        "BEGIN:VTIMEZONE",
        "TZID:Pacific/Auckland",
        "X-LIC-LOCATION:Pacific/Auckland",
        "BEGIN:DAYLIGHT",
        "TZOFFSETFROM:+1200",
        "TZOFFSETTO:+1300",
        "TZNAME:NZDT",
        "DTSTART:19700927T020000",
        "RRULE:FREQ=YEARLY;BYMONTH=9;BYDAY=-1SU",
        "END:DAYLIGHT",
        "BEGIN:STANDARD",
        "TZOFFSETFROM:+1300",
        "TZOFFSETTO:+1200",
        "TZNAME:NZST",
        "DTSTART:19700405T030000",
        "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=1SU",
        "END:STANDARD",
        "END:VTIMEZONE",
        "BEGIN:VEVENT",
        "DTSTART;TZID=Pacific/Auckland:20260302T090000",
        "DTEND;TZID=Pacific/Auckland:20260302T093000",
        "RRULE:FREQ=WEEKLY;BYDAY=MO",
        "DTSTAMP:20261015T010203Z",
        "ORGANIZER;CN=Team:mailto:team@example.com",
        "UID:7kukuqrfedlm2f9t0vq2ffm7d8@google.com",
        "ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED;CN=Jo",
        " ;X-NUM-GUESTS=0:mailto:jo@example.com",
        "CREATED:20260201T000000Z",
        "DESCRIPTION:Daily notes: https://example.com/notes\\nBring\\, as always\\, coffe",
        " e.",
        "LAST-MODIFIED:20260210T000000Z",
        "LOCATION:",
        "SEQUENCE:0",
        "STATUS:CONFIRMED",
        "SUMMARY:Stand-up",
        "TRANSP:OPAQUE",
        "BEGIN:VALARM",
        "ACTION:DISPLAY",
        "DESCRIPTION:This is an event reminder",
        "TRIGGER:-P0DT0H10M0S",
        "END:VALARM",
        "END:VEVENT",
        "BEGIN:VEVENT",
        "DTSTART;VALUE=DATE:20261225",
        "DTEND;VALUE=DATE:20261226",
        "DTSTAMP:20261015T010203Z",
        "UID:0a1b2c3d4e5f@google.com",
        "CREATED:20260101T000000Z",
        "LAST-MODIFIED:20260101T000000Z",
        "SEQUENCE:0",
        "STATUS:CONFIRMED",
        "SUMMARY:Christmas Day",
        "TRANSP:TRANSPARENT",
        "END:VEVENT",
        "END:VCALENDAR");

    /// <summary>
    /// An export in the style of Outlook and Exchange: a Windows time zone name as the TZID, with spaces, observances starting in 1601, and
    /// X-MICROSOFT- properties.
    /// </summary>
    public static readonly string OutlookExport = Crlf(
        "BEGIN:VCALENDAR",
        "METHOD:PUBLISH",
        "PRODID:Microsoft Exchange Server 2010",
        "VERSION:2.0",
        "X-WR-CALNAME:Calendar",
        "BEGIN:VTIMEZONE",
        "TZID:New Zealand Standard Time",
        "BEGIN:STANDARD",
        "DTSTART:16010101T030000",
        "TZOFFSETFROM:+1300",
        "TZOFFSETTO:+1200",
        "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=1SU;BYMONTH=4",
        "END:STANDARD",
        "BEGIN:DAYLIGHT",
        "DTSTART:16010101T020000",
        "TZOFFSETFROM:+1200",
        "TZOFFSETTO:+1300",
        "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=9",
        "END:DAYLIGHT",
        "END:VTIMEZONE",
        "BEGIN:VEVENT",
        "DESCRIPTION:\\n",
        "UID:040000008200E00074C5B7101A82E00800000000F0A7A1B8D2F2DA01000000000000000",
        " 0100000004BBE5E0D4C8A7742B0B5C7B2B3E0A4A5",
        "SUMMARY:Project review",
        "DTSTART;TZID=New Zealand Standard Time:20261020T100000",
        "DTEND;TZID=New Zealand Standard Time:20261020T110000",
        "CLASS:PUBLIC",
        "PRIORITY:5",
        "DTSTAMP:20261015T000000Z",
        "TRANSP:OPAQUE",
        "STATUS:CONFIRMED",
        "SEQUENCE:0",
        "LOCATION:Room 1",
        "X-MICROSOFT-CDO-APPT-SEQUENCE:0",
        "X-MICROSOFT-CDO-BUSYSTATUS:BUSY",
        "X-MICROSOFT-CDO-INTENDEDSTATUS:BUSY",
        "X-MICROSOFT-CDO-ALLDAYEVENT:FALSE",
        "X-MICROSOFT-CDO-IMPORTANCE:1",
        "X-MICROSOFT-CDO-INSTTYPE:0",
        "X-MICROSOFT-DONOTFORWARDMEETING:FALSE",
        "X-MICROSOFT-DISALLOW-COUNTER:FALSE",
        "BEGIN:VALARM",
        "DESCRIPTION:REMINDER",
        "TRIGGER;RELATED=START:-PT15M",
        "ACTION:DISPLAY",
        "END:VALARM",
        "END:VEVENT",
        "END:VCALENDAR");

    /// <summary>
    /// An export in the style of Apple Calendar, with X-APPLE- properties, an X- property on the VALARM, and the RFC 9074 alarm properties
    /// UID and ACKNOWLEDGED, which RFC 5545 does not define.
    /// </summary>
    public static readonly string AppleExport = Crlf(
        "BEGIN:VCALENDAR",
        "VERSION:2.0",
        "PRODID:-//Apple Inc.//macOS 15.0//EN",
        "CALSCALE:GREGORIAN",
        "BEGIN:VTIMEZONE",
        "TZID:Europe/London",
        "BEGIN:DAYLIGHT",
        "TZOFFSETFROM:+0000",
        "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU",
        "DTSTART:19810329T010000",
        "TZNAME:BST",
        "TZOFFSETTO:+0100",
        "END:DAYLIGHT",
        "BEGIN:STANDARD",
        "TZOFFSETFROM:+0100",
        "RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU",
        "DTSTART:19961027T020000",
        "TZNAME:GMT",
        "TZOFFSETTO:+0000",
        "END:STANDARD",
        "END:VTIMEZONE",
        "BEGIN:VEVENT",
        "CREATED:20260901T101010Z",
        "UID:4A0C7E1B-2D3F-4E5A-8B9C-0D1E2F3A4B5C",
        "DTEND;TZID=Europe/London:20261103T130000",
        "TRANSP:OPAQUE",
        "X-APPLE-TRAVEL-ADVISORY-BEHAVIOR:AUTOMATIC",
        "SUMMARY:Lunch",
        "LAST-MODIFIED:20260901T101111Z",
        "DTSTAMP:20260901T101111Z",
        "DTSTART;TZID=Europe/London:20261103T120000",
        "SEQUENCE:1",
        "X-APPLE-STRUCTURED-LOCATION;VALUE=URI;X-TITLE=Cafe:geo:51.5,-0.12",
        "BEGIN:VALARM",
        "X-WR-ALARMUID:9E5D1C7B-1111-2222-3333-444455556666",
        "UID:9E5D1C7B-1111-2222-3333-444455556666",
        "TRIGGER:-PT30M",
        "ATTACH;VALUE=URI:Chord",
        "ACTION:AUDIO",
        "X-APPLE-DEFAULT-ALARM:TRUE",
        "ACKNOWLEDGED:20261103T113000Z",
        "END:VALARM",
        "END:VEVENT",
        "END:VCALENDAR");

    /// <summary>
    /// Calendars that are read with the default options, by name.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Compliant = new Dictionary<string, string>
    {
        ["Rfc5545Event"] = RfcFixtures.Rfc5545Event,
        ["Rfc5545TimeZone"] = RfcFixtures.Rfc5545TimeZone,
        [nameof(Rfc5545TodoCorrected)] = Rfc5545TodoCorrected,
        [nameof(Rfc5545Journal)] = Rfc5545Journal,
        [nameof(Rfc7986)] = Rfc7986,
        [nameof(GoogleExport)] = GoogleExport,
        [nameof(OutlookExport)] = OutlookExport
    };

    /// <summary>
    /// Every calendar, by name, with the options it needs to be read.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, (string Text, IcalOptions Options)> All = BuildAll();

    public static TheoryData<string> AllNames()
    {
        var data = new TheoryData<string>();
        foreach (var name in All.Keys)
        {
            data.Add(name);
        }

        return data;
    }

    public static TheoryData<string, int> AllNamesAndBufferSizes()
    {
        var data = new TheoryData<string, int>();
        foreach (var name in All.Keys)
        {
            foreach (var bufferSize in new[] { 1, 5, 16 * 1024 })
            {
                data.Add(name, bufferSize);
            }
        }

        return data;
    }

    private static Dictionary<string, (string Text, IcalOptions Options)> BuildAll()
    {
        var all = Compliant.ToDictionary(fixture => fixture.Key, fixture => (fixture.Value, new IcalOptions()));

        // The RFC 5545 §4 VFREEBUSY example has no DTSTAMP or UID, and its VTODO example has an absolute TRIGGER without VALUE=DATE-TIME.
        all["Rfc5545FreeBusy"] = (RfcFixtures.Rfc5545FreeBusy, new IcalOptions { AllowMissingRequiredProperties = true });
        all["Rfc5545Todo"] = (RfcFixtures.Rfc5545Todo, new IcalOptions { AllowInvalidValues = true });

        // Apple writes ATTACH;VALUE=URI:Chord, which is not an absolute URI.
        all[nameof(AppleExport)] = (AppleExport, new IcalOptions { AllowInvalidValues = true });
        return all;
    }
}
