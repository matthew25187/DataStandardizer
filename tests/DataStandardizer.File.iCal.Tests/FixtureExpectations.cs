using System.Collections.Generic;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// What the tests expect to read from each fixture, worked out from the fixture text by RFC 5545 §3.1 and §3.2 and RFC 6868 §3
    /// rather than by the library: each fixture's content lines once unfolded, and how many calendars, components and lines it has.
    /// </summary>
    /// <remarks>
    /// A line is written as its number and the number of the physical line it starts on, where those are given, then its name, each
    /// parameter as <c>;NAME=value|value</c> with quotes removed and RFC 6868 escapes decoded, a colon and its value as it is in the text.
    /// </remarks>
    internal static class FixtureExpectations
    {
        /// <summary>
        /// The content lines of each of <see cref="RfcFixtures.All"/>, with their numbers.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> NumberedLines = new Dictionary<string, string[]>
        {
            [nameof(RfcFixtures.Rfc5545Event)] = new[]
            {
                "1/1 BEGIN:VCALENDAR",
                "2/2 PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN",
                "3/3 VERSION:2.0",
                "4/4 BEGIN:VEVENT",
                "5/5 DTSTAMP:19960704T120000Z",
                "6/6 UID:uid1@example.com",
                "7/7 ORGANIZER:mailto:jsmith@example.com",
                "8/8 DTSTART:19960918T143000Z",
                "9/9 DTEND:19960920T220000Z",
                "10/10 STATUS:CONFIRMED",
                "11/11 CATEGORIES:CONFERENCE",
                "12/12 SUMMARY:Networld+Interop Conference",
                "13/13 DESCRIPTION:Networld+Interop Conference and Exhibit\\nAtlanta World Congress Center\\nAtlanta\\, Georgia",
                "14/16 END:VEVENT",
                "15/17 END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545TimeZone)] = new[]
            {
                "1/1 BEGIN:VCALENDAR",
                "2/2 PRODID:-//RDU Software//NONSGML HandCal//EN",
                "3/3 VERSION:2.0",
                "4/4 BEGIN:VTIMEZONE",
                "5/5 TZID:America/New_York",
                "6/6 BEGIN:STANDARD",
                "7/7 DTSTART:19981025T020000",
                "8/8 TZOFFSETFROM:-0400",
                "9/9 TZOFFSETTO:-0500",
                "10/10 TZNAME:EST",
                "11/11 END:STANDARD",
                "12/12 BEGIN:DAYLIGHT",
                "13/13 DTSTART:19990404T020000",
                "14/14 TZOFFSETFROM:-0500",
                "15/15 TZOFFSETTO:-0400",
                "16/16 TZNAME:EDT",
                "17/17 END:DAYLIGHT",
                "18/18 END:VTIMEZONE",
                "19/19 BEGIN:VEVENT",
                "20/20 DTSTAMP:19980309T231000Z",
                "21/21 UID:guid-1.example.com",
                "22/22 ORGANIZER;ROLE=CHAIR:mailto:mrbig@example.com",
                "23/23 ATTENDEE;RSVP=TRUE;ROLE=REQ-PARTICIPANT;CUTYPE=GROUP:mailto:employee-A@example.com",
                "24/25 DESCRIPTION:Project XYZ Review Meeting",
                "25/26 CATEGORIES:MEETING",
                "26/27 CLASS:PUBLIC",
                "27/28 CREATED:19980309T130000Z",
                "28/29 SUMMARY:XYZ Project Review",
                "29/30 DTSTART;TZID=America/New_York:19980312T083000",
                "30/31 DTEND;TZID=America/New_York:19980312T093000",
                "31/32 LOCATION:1CP Conference Room 4350",
                "32/33 END:VEVENT",
                "33/34 END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545Todo)] = new[]
            {
                "1/1 BEGIN:VCALENDAR",
                "2/2 VERSION:2.0",
                "3/3 PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "4/4 BEGIN:VTODO",
                "5/5 DTSTAMP:19980130T134500Z",
                "6/6 SEQUENCE:2",
                "7/7 UID:uid4@example.com",
                "8/8 ORGANIZER:mailto:unclesam@example.com",
                "9/9 ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "10/10 DUE:19980415T000000",
                "11/11 STATUS:NEEDS-ACTION",
                "12/12 SUMMARY:Submit Income Taxes",
                "13/13 BEGIN:VALARM",
                "14/14 ACTION:AUDIO",
                "15/15 TRIGGER:19980403T120000Z",
                "16/16 ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "17/18 REPEAT:4",
                "18/19 DURATION:PT1H",
                "19/20 END:VALARM",
                "20/21 END:VTODO",
                "21/22 END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545FreeBusy)] = new[]
            {
                "1/1 BEGIN:VCALENDAR",
                "2/2 VERSION:2.0",
                "3/3 PRODID:-//RDU Software//NONSGML HandCal//EN",
                "4/4 BEGIN:VFREEBUSY",
                "5/5 ORGANIZER:mailto:jsmith@example.com",
                "6/6 DTSTART:19980313T141711Z",
                "7/7 DTEND:19980410T141711Z",
                "8/8 FREEBUSY:19980314T233000Z/19980315T003000Z",
                "9/9 FREEBUSY:19980316T153000Z/19980316T163000Z",
                "10/10 FREEBUSY:19980318T030000Z/19980318T040000Z",
                "11/11 URL:http://www.example.com/calendar/busytime/jsmith.ifb",
                "12/12 END:VFREEBUSY",
                "13/13 END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545Parameters)] = new[]
            {
                "1/1 ATTENDEE;DELEGATED-FROM=mailto:jsmith@example.com:mailto:jdoe@example.com",
                "2/2 ATTENDEE;MEMBER=mailto:projectA@example.com|mailto:projectB@example.com:mailto:janedoe@example.com",
                "3/3 ORGANIZER;CN=John Smith:mailto:jsmith@example.com",
                "4/4 ORGANIZER;SENT-BY=mailto:sray@example.com:mailto:jsmith@example.com",
                "5/5 DESCRIPTION;ALTREP=cid:part1.0001@example.org:The Fall'98 Wild Wizards Conference - - Las Vegas\\, NV\\, USA",
                "6/6 ATTENDEE;CUTYPE=GROUP:mailto:ietf-calsch@example.org",
                "7/7 ATTENDEE;ROLE=CHAIR:mailto:mrbig@example.com",
                "8/8 SUMMARY;LANGUAGE=en-US:Company Holiday Party",
                "9/9 LOCATION;LANGUAGE=no:Tyskland",
                "10/10 RDATE;VALUE=DATE:19970304,19970504,19970704,19970904",
                "11/11 RRULE:FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH",
                "12/12 X-ABC-MMSUBJ;VALUE=URI;FMTTYPE=audio/basic:http://www.example.org/mysubj.au",
                "13/13 GEO:37.386013;-122.082932",
                "14/14 REQUEST-STATUS:3.1;Invalid property value;DTSTART:96-Apr-01",
            },
            [nameof(RfcFixtures.Rfc6868)] = new[]
            {
                "1/1 GEO;X-ADDRESS=Pittsburgh Pirates\n115 Federal St\nPittsburgh, PA 15212:40.446816;-80.00679",
                "2/3 ATTENDEE;CN=George Herman \"Babe\" Ruth:mailto:babe@example.com",
            },
        };

        /// <summary>
        /// The content lines of each of <see cref="RfcFixtures.All"/>, without their numbers.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> Lines = new Dictionary<string, string[]>
        {
            [nameof(RfcFixtures.Rfc5545Event)] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:19960704T120000Z",
                "UID:uid1@example.com",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19960918T143000Z",
                "DTEND:19960920T220000Z",
                "STATUS:CONFIRMED",
                "CATEGORIES:CONFERENCE",
                "SUMMARY:Networld+Interop Conference",
                "DESCRIPTION:Networld+Interop Conference and Exhibit\\nAtlanta World Congress Center\\nAtlanta\\, Georgia",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545TimeZone)] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "TZID:America/New_York",
                "BEGIN:STANDARD",
                "DTSTART:19981025T020000",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "TZNAME:EST",
                "END:STANDARD",
                "BEGIN:DAYLIGHT",
                "DTSTART:19990404T020000",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "TZNAME:EDT",
                "END:DAYLIGHT",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:19980309T231000Z",
                "UID:guid-1.example.com",
                "ORGANIZER;ROLE=CHAIR:mailto:mrbig@example.com",
                "ATTENDEE;RSVP=TRUE;ROLE=REQ-PARTICIPANT;CUTYPE=GROUP:mailto:employee-A@example.com",
                "DESCRIPTION:Project XYZ Review Meeting",
                "CATEGORIES:MEETING",
                "CLASS:PUBLIC",
                "CREATED:19980309T130000Z",
                "SUMMARY:XYZ Project Review",
                "DTSTART;TZID=America/New_York:19980312T083000",
                "DTEND;TZID=America/New_York:19980312T093000",
                "LOCATION:1CP Conference Room 4350",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545Todo)] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "BEGIN:VTODO",
                "DTSTAMP:19980130T134500Z",
                "SEQUENCE:2",
                "UID:uid4@example.com",
                "ORGANIZER:mailto:unclesam@example.com",
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "DUE:19980415T000000",
                "STATUS:NEEDS-ACTION",
                "SUMMARY:Submit Income Taxes",
                "BEGIN:VALARM",
                "ACTION:AUDIO",
                "TRIGGER:19980403T120000Z",
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "REPEAT:4",
                "DURATION:PT1H",
                "END:VALARM",
                "END:VTODO",
                "END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545FreeBusy)] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "BEGIN:VFREEBUSY",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19980313T141711Z",
                "DTEND:19980410T141711Z",
                "FREEBUSY:19980314T233000Z/19980315T003000Z",
                "FREEBUSY:19980316T153000Z/19980316T163000Z",
                "FREEBUSY:19980318T030000Z/19980318T040000Z",
                "URL:http://www.example.com/calendar/busytime/jsmith.ifb",
                "END:VFREEBUSY",
                "END:VCALENDAR",
            },
            [nameof(RfcFixtures.Rfc5545Parameters)] = new[]
            {
                "ATTENDEE;DELEGATED-FROM=mailto:jsmith@example.com:mailto:jdoe@example.com",
                "ATTENDEE;MEMBER=mailto:projectA@example.com|mailto:projectB@example.com:mailto:janedoe@example.com",
                "ORGANIZER;CN=John Smith:mailto:jsmith@example.com",
                "ORGANIZER;SENT-BY=mailto:sray@example.com:mailto:jsmith@example.com",
                "DESCRIPTION;ALTREP=cid:part1.0001@example.org:The Fall'98 Wild Wizards Conference - - Las Vegas\\, NV\\, USA",
                "ATTENDEE;CUTYPE=GROUP:mailto:ietf-calsch@example.org",
                "ATTENDEE;ROLE=CHAIR:mailto:mrbig@example.com",
                "SUMMARY;LANGUAGE=en-US:Company Holiday Party",
                "LOCATION;LANGUAGE=no:Tyskland",
                "RDATE;VALUE=DATE:19970304,19970504,19970704,19970904",
                "RRULE:FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH",
                "X-ABC-MMSUBJ;VALUE=URI;FMTTYPE=audio/basic:http://www.example.org/mysubj.au",
                "GEO:37.386013;-122.082932",
                "REQUEST-STATUS:3.1;Invalid property value;DTSTART:96-Apr-01",
            },
            [nameof(RfcFixtures.Rfc6868)] = new[]
            {
                "GEO;X-ADDRESS=Pittsburgh Pirates\n115 Federal St\nPittsburgh, PA 15212:40.446816;-80.00679",
                "ATTENDEE;CN=George Herman \"Babe\" Ruth:mailto:babe@example.com",
            },
        };

        /// <summary>
        /// Each of <see cref="RfcFixtures.All"/> as a writer with the default options writes its lines: each content line unfolded, then
        /// folded again at 75 octets with CRLF and a space (RFC 5545 §3.1).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> WrittenLines = new Dictionary<string, string>
        {
            ["Rfc5545Event"] =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN\r\n" +
                "VERSION:2.0\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTAMP:19960704T120000Z\r\n" +
                "UID:uid1@example.com\r\n" +
                "ORGANIZER:mailto:jsmith@example.com\r\n" +
                "DTSTART:19960918T143000Z\r\n" +
                "DTEND:19960920T220000Z\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "CATEGORIES:CONFERENCE\r\n" +
                "SUMMARY:Networld+Interop Conference\r\n" +
                "DESCRIPTION:Networld+Interop Conference and Exhibit\\nAtlanta World Congress\r\n" +
                "  Center\\nAtlanta\\, Georgia\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545TimeZone"] =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//RDU Software//NONSGML HandCal//EN\r\n" +
                "VERSION:2.0\r\n" +
                "BEGIN:VTIMEZONE\r\n" +
                "TZID:America/New_York\r\n" +
                "BEGIN:STANDARD\r\n" +
                "DTSTART:19981025T020000\r\n" +
                "TZOFFSETFROM:-0400\r\n" +
                "TZOFFSETTO:-0500\r\n" +
                "TZNAME:EST\r\n" +
                "END:STANDARD\r\n" +
                "BEGIN:DAYLIGHT\r\n" +
                "DTSTART:19990404T020000\r\n" +
                "TZOFFSETFROM:-0500\r\n" +
                "TZOFFSETTO:-0400\r\n" +
                "TZNAME:EDT\r\n" +
                "END:DAYLIGHT\r\n" +
                "END:VTIMEZONE\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTAMP:19980309T231000Z\r\n" +
                "UID:guid-1.example.com\r\n" +
                "ORGANIZER;ROLE=CHAIR:mailto:mrbig@example.com\r\n" +
                "ATTENDEE;RSVP=TRUE;ROLE=REQ-PARTICIPANT;CUTYPE=GROUP:mailto:employee-A@exam\r\n" +
                " ple.com\r\n" +
                "DESCRIPTION:Project XYZ Review Meeting\r\n" +
                "CATEGORIES:MEETING\r\n" +
                "CLASS:PUBLIC\r\n" +
                "CREATED:19980309T130000Z\r\n" +
                "SUMMARY:XYZ Project Review\r\n" +
                "DTSTART;TZID=America/New_York:19980312T083000\r\n" +
                "DTEND;TZID=America/New_York:19980312T093000\r\n" +
                "LOCATION:1CP Conference Room 4350\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545Todo"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//ABC Corporation//NONSGML My Product//EN\r\n" +
                "BEGIN:VTODO\r\n" +
                "DTSTAMP:19980130T134500Z\r\n" +
                "SEQUENCE:2\r\n" +
                "UID:uid4@example.com\r\n" +
                "ORGANIZER:mailto:unclesam@example.com\r\n" +
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com\r\n" +
                "DUE:19980415T000000\r\n" +
                "STATUS:NEEDS-ACTION\r\n" +
                "SUMMARY:Submit Income Taxes\r\n" +
                "BEGIN:VALARM\r\n" +
                "ACTION:AUDIO\r\n" +
                "TRIGGER:19980403T120000Z\r\n" +
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud\r\n" +
                "REPEAT:4\r\n" +
                "DURATION:PT1H\r\n" +
                "END:VALARM\r\n" +
                "END:VTODO\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545FreeBusy"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//RDU Software//NONSGML HandCal//EN\r\n" +
                "BEGIN:VFREEBUSY\r\n" +
                "ORGANIZER:mailto:jsmith@example.com\r\n" +
                "DTSTART:19980313T141711Z\r\n" +
                "DTEND:19980410T141711Z\r\n" +
                "FREEBUSY:19980314T233000Z/19980315T003000Z\r\n" +
                "FREEBUSY:19980316T153000Z/19980316T163000Z\r\n" +
                "FREEBUSY:19980318T030000Z/19980318T040000Z\r\n" +
                "URL:http://www.example.com/calendar/busytime/jsmith.ifb\r\n" +
                "END:VFREEBUSY\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545Parameters"] =
                "ATTENDEE;DELEGATED-FROM=\"mailto:jsmith@example.com\":mailto:jdoe@example.com\r\n" +
                "ATTENDEE;MEMBER=\"mailto:projectA@example.com\",\"mailto:projectB@example.com\"\r\n" +
                " :mailto:janedoe@example.com\r\n" +
                "ORGANIZER;CN=\"John Smith\":mailto:jsmith@example.com\r\n" +
                "ORGANIZER;SENT-BY=\"mailto:sray@example.com\":mailto:jsmith@example.com\r\n" +
                "DESCRIPTION;ALTREP=\"cid:part1.0001@example.org\":The Fall'98 Wild Wizards Co\r\n" +
                " nference - - Las Vegas\\, NV\\, USA\r\n" +
                "ATTENDEE;CUTYPE=GROUP:mailto:ietf-calsch@example.org\r\n" +
                "ATTENDEE;ROLE=CHAIR:mailto:mrbig@example.com\r\n" +
                "SUMMARY;LANGUAGE=en-US:Company Holiday Party\r\n" +
                "LOCATION;LANGUAGE=no:Tyskland\r\n" +
                "RDATE;VALUE=DATE:19970304,19970504,19970704,19970904\r\n" +
                "RRULE:FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH\r\n" +
                "X-ABC-MMSUBJ;VALUE=URI;FMTTYPE=audio/basic:http://www.example.org/mysubj.au\r\n" +
                "GEO:37.386013;-122.082932\r\n" +
                "REQUEST-STATUS:3.1;Invalid property value;DTSTART:96-Apr-01\r\n",
            ["Rfc6868"] =
                "GEO;X-ADDRESS=\"Pittsburgh Pirates^n115 Federal St^nPittsburgh, PA 15212\":40\r\n" +
                " .446816;-80.00679\r\n" +
                "ATTENDEE;CN=George Herman ^'Babe^' Ruth:mailto:babe@example.com\r\n",
        };

        /// <summary>
        /// How many content lines each of <see cref="RfcFixtures.All"/> has.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, int> LineCounts = new Dictionary<string, int>
        {
            [nameof(RfcFixtures.Rfc5545Event)] = 15,
            [nameof(RfcFixtures.Rfc5545TimeZone)] = 33,
            [nameof(RfcFixtures.Rfc5545Todo)] = 21,
            [nameof(RfcFixtures.Rfc5545FreeBusy)] = 13,
            [nameof(RfcFixtures.Rfc5545Parameters)] = 14,
            [nameof(RfcFixtures.Rfc6868)] = 2,
        };

        /// <summary>
        /// The content lines of each of <see cref="CalendarFixtures.All"/>, without their numbers, and with the parameters of each in order
        /// of name, as <see cref="XcalTestData.Model"/> gives them.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> Models = new Dictionary<string, string[]>
        {
            ["Rfc5545Event"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:19960704T120000Z",
                "UID:uid1@example.com",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19960918T143000Z",
                "DTEND:19960920T220000Z",
                "STATUS:CONFIRMED",
                "CATEGORIES:CONFERENCE",
                "SUMMARY:Networld+Interop Conference",
                "DESCRIPTION:Networld+Interop Conference and Exhibit\\nAtlanta World Congress Center\\nAtlanta\\, Georgia",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Rfc5545TimeZone"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "TZID:America/New_York",
                "BEGIN:STANDARD",
                "DTSTART:19981025T020000",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "TZNAME:EST",
                "END:STANDARD",
                "BEGIN:DAYLIGHT",
                "DTSTART:19990404T020000",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "TZNAME:EDT",
                "END:DAYLIGHT",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:19980309T231000Z",
                "UID:guid-1.example.com",
                "ORGANIZER;ROLE=CHAIR:mailto:mrbig@example.com",
                "ATTENDEE;CUTYPE=GROUP;ROLE=REQ-PARTICIPANT;RSVP=TRUE:mailto:employee-A@example.com",
                "DESCRIPTION:Project XYZ Review Meeting",
                "CATEGORIES:MEETING",
                "CLASS:PUBLIC",
                "CREATED:19980309T130000Z",
                "SUMMARY:XYZ Project Review",
                "DTSTART;TZID=America/New_York:19980312T083000",
                "DTEND;TZID=America/New_York:19980312T093000",
                "LOCATION:1CP Conference Room 4350",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Rfc5545TodoCorrected"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "BEGIN:VTODO",
                "DTSTAMP:19980130T134500Z",
                "SEQUENCE:2",
                "UID:uid4@example.com",
                "ORGANIZER:mailto:unclesam@example.com",
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "DUE:19980415T000000",
                "STATUS:NEEDS-ACTION",
                "SUMMARY:Submit Income Taxes",
                "BEGIN:VALARM",
                "ACTION:AUDIO",
                "TRIGGER;VALUE=DATE-TIME:19980403T120000Z",
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "REPEAT:4",
                "DURATION:PT1H",
                "END:VALARM",
                "END:VTODO",
                "END:VCALENDAR",
            },
            ["Rfc5545Journal"] = new[]
            {
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
                "DESCRIPTION:Project xyz Review Meeting Minutes\\nAgenda\\n1. Review of project version 1.0 requirements.\\n2. Definitionof project processes.\\n3. Review of project schedule.\\nParticipants: John Smith\\, Jane Doe\\, Jim Dandy\\n-It was decided that the requirements need to be signed off byproduct marketing.\\n-Project processes were accepted.\\n-Project schedule needs to account for scheduled freeze of the Ical.",
                "END:VJOURNAL",
                "END:VCALENDAR",
            },
            ["Rfc7986"] = new[]
            {
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
                "IMAGE;DISPLAY=BADGE;FMTTYPE=image/png;VALUE=URI:http://example.com/images/party.png",
                "BEGIN:VEVENT",
                "DTSTAMP:20160101T120000Z",
                "UID:conference-1@example.com",
                "DTSTART:20160102T150000Z",
                "SUMMARY:Planning meeting",
                "COLOR:red",
                "IMAGE;DISPLAY=BADGE;FMTTYPE=image/png;VALUE=URI:http://example.com/images/party.png",
                "CONFERENCE;FEATURE=PHONE|MODERATOR;LABEL=Moderator dial-in;VALUE=URI:tel:+1-412-555-0123,,,654321",
                "CONFERENCE;FEATURE=PHONE;LABEL=Attendee dial-in;VALUE=URI:tel:+1-412-555-0123,,,555123",
                "CONFERENCE;FEATURE=PHONE;LABEL=Attendee dial-in;VALUE=URI:tel:+1-888-555-0456,,,555123",
                "CONFERENCE;FEATURE=CHAT;LABEL=Chat room;VALUE=URI:xmpp:chat-123@conference.example.com",
                "CONFERENCE;FEATURE=AUDIO|VIDEO;LABEL=Attendee dial-in;VALUE=URI:https://chat.example.com/audio?id=123456",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["GoogleExport"] = new[]
            {
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
                "ATTENDEE;CN=Jo;CUTYPE=INDIVIDUAL;PARTSTAT=ACCEPTED;ROLE=REQ-PARTICIPANT;X-NUM-GUESTS=0:mailto:jo@example.com",
                "CREATED:20260201T000000Z",
                "DESCRIPTION:Daily notes: https://example.com/notes\\nBring\\, as always\\, coffee.",
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
                "END:VCALENDAR",
            },
            ["OutlookExport"] = new[]
            {
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
                "UID:040000008200E00074C5B7101A82E00800000000F0A7A1B8D2F2DA010000000000000000100000004BBE5E0D4C8A7742B0B5C7B2B3E0A4A5",
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
                "END:VCALENDAR",
            },
            ["Rfc5545FreeBusy"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "BEGIN:VFREEBUSY",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19980313T141711Z",
                "DTEND:19980410T141711Z",
                "FREEBUSY:19980314T233000Z/19980315T003000Z",
                "FREEBUSY:19980316T153000Z/19980316T163000Z",
                "FREEBUSY:19980318T030000Z/19980318T040000Z",
                "URL:http://www.example.com/calendar/busytime/jsmith.ifb",
                "END:VFREEBUSY",
                "END:VCALENDAR",
            },
            ["Rfc5545Todo"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "BEGIN:VTODO",
                "DTSTAMP:19980130T134500Z",
                "SEQUENCE:2",
                "UID:uid4@example.com",
                "ORGANIZER:mailto:unclesam@example.com",
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "DUE:19980415T000000",
                "STATUS:NEEDS-ACTION",
                "SUMMARY:Submit Income Taxes",
                "BEGIN:VALARM",
                "ACTION:AUDIO",
                "TRIGGER:19980403T120000Z",
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "REPEAT:4",
                "DURATION:PT1H",
                "END:VALARM",
                "END:VTODO",
                "END:VCALENDAR",
            },
            ["AppleExport"] = new[]
            {
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
                "END:VCALENDAR",
            },
        };

        /// <summary>
        /// The content lines of each calendar of <see cref="XcalRoundTripTests"/>, as <see cref="Models"/> gives them.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> XcalRoundTripModels = new Dictionary<string, string[]>
        {
            ["Rfc5545Event"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:19960704T120000Z",
                "UID:uid1@example.com",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19960918T143000Z",
                "DTEND:19960920T220000Z",
                "STATUS:CONFIRMED",
                "CATEGORIES:CONFERENCE",
                "SUMMARY:Networld+Interop Conference",
                "DESCRIPTION:Networld+Interop Conference and Exhibit\\nAtlanta World Congress Center\\nAtlanta\\, Georgia",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Rfc5545TimeZone"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "TZID:America/New_York",
                "BEGIN:STANDARD",
                "DTSTART:19981025T020000",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "TZNAME:EST",
                "END:STANDARD",
                "BEGIN:DAYLIGHT",
                "DTSTART:19990404T020000",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "TZNAME:EDT",
                "END:DAYLIGHT",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:19980309T231000Z",
                "UID:guid-1.example.com",
                "ORGANIZER;ROLE=CHAIR:mailto:mrbig@example.com",
                "ATTENDEE;CUTYPE=GROUP;ROLE=REQ-PARTICIPANT;RSVP=TRUE:mailto:employee-A@example.com",
                "DESCRIPTION:Project XYZ Review Meeting",
                "CATEGORIES:MEETING",
                "CLASS:PUBLIC",
                "CREATED:19980309T130000Z",
                "SUMMARY:XYZ Project Review",
                "DTSTART;TZID=America/New_York:19980312T083000",
                "DTEND;TZID=America/New_York:19980312T093000",
                "LOCATION:1CP Conference Room 4350",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Rfc5545TodoCorrected"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "BEGIN:VTODO",
                "DTSTAMP:19980130T134500Z",
                "SEQUENCE:2",
                "UID:uid4@example.com",
                "ORGANIZER:mailto:unclesam@example.com",
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "DUE:19980415T000000",
                "STATUS:NEEDS-ACTION",
                "SUMMARY:Submit Income Taxes",
                "BEGIN:VALARM",
                "ACTION:AUDIO",
                "TRIGGER;VALUE=DATE-TIME:19980403T120000Z",
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "REPEAT:4",
                "DURATION:PT1H",
                "END:VALARM",
                "END:VTODO",
                "END:VCALENDAR",
            },
            ["Rfc5545Journal"] = new[]
            {
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
                "DESCRIPTION:Project xyz Review Meeting Minutes\\nAgenda\\n1. Review of project version 1.0 requirements.\\n2. Definitionof project processes.\\n3. Review of project schedule.\\nParticipants: John Smith\\, Jane Doe\\, Jim Dandy\\n-It was decided that the requirements need to be signed off byproduct marketing.\\n-Project processes were accepted.\\n-Project schedule needs to account for scheduled freeze of the Ical.",
                "END:VJOURNAL",
                "END:VCALENDAR",
            },
            ["Rfc7986"] = new[]
            {
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
                "IMAGE;DISPLAY=BADGE;FMTTYPE=image/png;VALUE=URI:http://example.com/images/party.png",
                "BEGIN:VEVENT",
                "DTSTAMP:20160101T120000Z",
                "UID:conference-1@example.com",
                "DTSTART:20160102T150000Z",
                "SUMMARY:Planning meeting",
                "COLOR:red",
                "IMAGE;DISPLAY=BADGE;FMTTYPE=image/png;VALUE=URI:http://example.com/images/party.png",
                "CONFERENCE;FEATURE=PHONE|MODERATOR;LABEL=Moderator dial-in;VALUE=URI:tel:+1-412-555-0123,,,654321",
                "CONFERENCE;FEATURE=PHONE;LABEL=Attendee dial-in;VALUE=URI:tel:+1-412-555-0123,,,555123",
                "CONFERENCE;FEATURE=PHONE;LABEL=Attendee dial-in;VALUE=URI:tel:+1-888-555-0456,,,555123",
                "CONFERENCE;FEATURE=CHAT;LABEL=Chat room;VALUE=URI:xmpp:chat-123@conference.example.com",
                "CONFERENCE;FEATURE=AUDIO|VIDEO;LABEL=Attendee dial-in;VALUE=URI:https://chat.example.com/audio?id=123456",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["GoogleExport"] = new[]
            {
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
                "ATTENDEE;CN=Jo;CUTYPE=INDIVIDUAL;PARTSTAT=ACCEPTED;ROLE=REQ-PARTICIPANT;X-NUM-GUESTS=0:mailto:jo@example.com",
                "CREATED:20260201T000000Z",
                "DESCRIPTION:Daily notes: https://example.com/notes\\nBring\\, as always\\, coffee.",
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
                "END:VCALENDAR",
            },
            ["OutlookExport"] = new[]
            {
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
                "UID:040000008200E00074C5B7101A82E00800000000F0A7A1B8D2F2DA010000000000000000100000004BBE5E0D4C8A7742B0B5C7B2B3E0A4A5",
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
                "END:VCALENDAR",
            },
            ["Rfc5545FreeBusy"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "BEGIN:VFREEBUSY",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19980313T141711Z",
                "DTEND:19980410T141711Z",
                "FREEBUSY:19980314T233000Z/19980315T003000Z",
                "FREEBUSY:19980316T153000Z/19980316T163000Z",
                "FREEBUSY:19980318T030000Z/19980318T040000Z",
                "URL:http://www.example.com/calendar/busytime/jsmith.ifb",
                "END:VFREEBUSY",
                "END:VCALENDAR",
            },
            ["Rfc5545Todo"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "BEGIN:VTODO",
                "DTSTAMP:19980130T134500Z",
                "SEQUENCE:2",
                "UID:uid4@example.com",
                "ORGANIZER:mailto:unclesam@example.com",
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "DUE:19980415T000000",
                "STATUS:NEEDS-ACTION",
                "SUMMARY:Submit Income Taxes",
                "BEGIN:VALARM",
                "ACTION:AUDIO",
                "TRIGGER:19980403T120000Z",
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "REPEAT:4",
                "DURATION:PT1H",
                "END:VALARM",
                "END:VTODO",
                "END:VCALENDAR",
            },
            ["AppleExport"] = new[]
            {
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
                "END:VCALENDAR",
            },
            ["Example1Ical"] = new[]
            {
                "BEGIN:VCALENDAR",
                "CALSCALE:GREGORIAN",
                "PRODID:-//Example Inc.//Example Calendar//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:20080205T191224Z",
                "DTSTART;VALUE=DATE:20081006",
                "SUMMARY:Planning meeting",
                "UID:4088E990AD89CB3DBB484909",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Example2Ical"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//Example Inc.//Example Client//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "LAST-MODIFIED:20040110T032845Z",
                "TZID:US/Eastern",
                "BEGIN:DAYLIGHT",
                "DTSTART:20000404T020000",
                "RRULE:FREQ=YEARLY;BYDAY=1SU;BYMONTH=4",
                "TZNAME:EDT",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "END:DAYLIGHT",
                "BEGIN:STANDARD",
                "DTSTART:20001026T020000",
                "RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10",
                "TZNAME:EST",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "END:STANDARD",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060102T120000",
                "DURATION:PT1H",
                "RRULE:FREQ=DAILY;COUNT=5",
                "RDATE;TZID=US/Eastern;VALUE=PERIOD:20060102T150000/PT2H",
                "SUMMARY:Event #2",
                "DESCRIPTION:We are having a meeting all this week at 12 pm for one hour\\, with an additional meeting on the first day 2 hours long.\\nPlease bring your own lunch for the 12 pm meetings.",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060104T140000",
                "DURATION:PT1H",
                "RECURRENCE-ID;TZID=US/Eastern:20060104T120000",
                "SUMMARY:Event #2 bis",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Extensions"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//Test//Test//EN",
                "BEGIN:VEVENT",
                "UID:event@example.com",
                "DTSTAMP:20260101T000000Z",
                "DTSTART:20260101T090000Z",
                "X-PROPERTY:20110512T120000Z",
                "X-RAW:a\\,b;c:d",
                "X-TEXT;VALUE=TEXT:one\\, two,three\\nfour",
                "X-BINARY;ENCODING=BASE64;VALUE=BINARY:SGVsbG8=",
                "X-BOOLEAN;VALUE=BOOLEAN:FALSE",
                "X-CAL-ADDRESS;VALUE=CAL-ADDRESS:mailto:a@example.com",
                "X-DATES;VALUE=DATE:20260101,20260102",
                "X-DATE-TIME;VALUE=DATE-TIME:20260101T090000Z",
                "X-DURATION;VALUE=DURATION:-P1W",
                "X-FLOAT;VALUE=FLOAT:-1.250",
                "X-INTEGER;VALUE=INTEGER:+7",
                "X-PERIOD;VALUE=PERIOD:20260101T090000Z/PT1H",
                "X-RECUR;VALUE=RECUR:FREQ=MONTHLY;BYMONTHDAY=1,-1;COUNT=3",
                "X-TIME;VALUE=TIME:090000",
                "X-URI;VALUE=URI:http://example.com/a?b=c",
                "X-UTC-OFFSET;VALUE=UTC-OFFSET:-013015",
                "X-OTHER-TYPE;VALUE=X-NAME:anything",
                "DTEND;X-LIST=a|b,c;X-PARAM=PT30M:20260101T100000Z",
                "SUMMARY;LANGUAGE=en;X-QUOTED=a;b:Caret ^ and quote \" kept",
                "IANA-PROP;VALUE=TEXT:iana",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Example1IcalThenExample2Ical"] = new[]
            {
                "BEGIN:VCALENDAR",
                "CALSCALE:GREGORIAN",
                "PRODID:-//Example Inc.//Example Calendar//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:20080205T191224Z",
                "DTSTART;VALUE=DATE:20081006",
                "SUMMARY:Planning meeting",
                "UID:4088E990AD89CB3DBB484909",
                "END:VEVENT",
                "END:VCALENDAR",
                "BEGIN:VCALENDAR",
                "PRODID:-//Example Inc.//Example Client//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "LAST-MODIFIED:20040110T032845Z",
                "TZID:US/Eastern",
                "BEGIN:DAYLIGHT",
                "DTSTART:20000404T020000",
                "RRULE:FREQ=YEARLY;BYDAY=1SU;BYMONTH=4",
                "TZNAME:EDT",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "END:DAYLIGHT",
                "BEGIN:STANDARD",
                "DTSTART:20001026T020000",
                "RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10",
                "TZNAME:EST",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "END:STANDARD",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060102T120000",
                "DURATION:PT1H",
                "RRULE:FREQ=DAILY;COUNT=5",
                "RDATE;TZID=US/Eastern;VALUE=PERIOD:20060102T150000/PT2H",
                "SUMMARY:Event #2",
                "DESCRIPTION:We are having a meeting all this week at 12 pm for one hour\\, with an additional meeting on the first day 2 hours long.\\nPlease bring your own lunch for the 12 pm meetings.",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060104T140000",
                "DURATION:PT1H",
                "RECURRENCE-ID;TZID=US/Eastern:20060104T120000",
                "SUMMARY:Event #2 bis",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "END:VCALENDAR",
            },
        };

        /// <summary>
        /// The content lines of each calendar of <see cref="JcalRoundTripTests"/>, as <see cref="Models"/> gives them.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> JcalRoundTripModels = new Dictionary<string, string[]>
        {
            ["Rfc5545Event"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:19960704T120000Z",
                "UID:uid1@example.com",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19960918T143000Z",
                "DTEND:19960920T220000Z",
                "STATUS:CONFIRMED",
                "CATEGORIES:CONFERENCE",
                "SUMMARY:Networld+Interop Conference",
                "DESCRIPTION:Networld+Interop Conference and Exhibit\\nAtlanta World Congress Center\\nAtlanta\\, Georgia",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Rfc5545TimeZone"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "TZID:America/New_York",
                "BEGIN:STANDARD",
                "DTSTART:19981025T020000",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "TZNAME:EST",
                "END:STANDARD",
                "BEGIN:DAYLIGHT",
                "DTSTART:19990404T020000",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "TZNAME:EDT",
                "END:DAYLIGHT",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:19980309T231000Z",
                "UID:guid-1.example.com",
                "ORGANIZER;ROLE=CHAIR:mailto:mrbig@example.com",
                "ATTENDEE;CUTYPE=GROUP;ROLE=REQ-PARTICIPANT;RSVP=TRUE:mailto:employee-A@example.com",
                "DESCRIPTION:Project XYZ Review Meeting",
                "CATEGORIES:MEETING",
                "CLASS:PUBLIC",
                "CREATED:19980309T130000Z",
                "SUMMARY:XYZ Project Review",
                "DTSTART;TZID=America/New_York:19980312T083000",
                "DTEND;TZID=America/New_York:19980312T093000",
                "LOCATION:1CP Conference Room 4350",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Rfc5545TodoCorrected"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "BEGIN:VTODO",
                "DTSTAMP:19980130T134500Z",
                "SEQUENCE:2",
                "UID:uid4@example.com",
                "ORGANIZER:mailto:unclesam@example.com",
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "DUE:19980415T000000",
                "STATUS:NEEDS-ACTION",
                "SUMMARY:Submit Income Taxes",
                "BEGIN:VALARM",
                "ACTION:AUDIO",
                "TRIGGER;VALUE=DATE-TIME:19980403T120000Z",
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "REPEAT:4",
                "DURATION:PT1H",
                "END:VALARM",
                "END:VTODO",
                "END:VCALENDAR",
            },
            ["Rfc5545Journal"] = new[]
            {
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
                "DESCRIPTION:Project xyz Review Meeting Minutes\\nAgenda\\n1. Review of project version 1.0 requirements.\\n2. Definitionof project processes.\\n3. Review of project schedule.\\nParticipants: John Smith\\, Jane Doe\\, Jim Dandy\\n-It was decided that the requirements need to be signed off byproduct marketing.\\n-Project processes were accepted.\\n-Project schedule needs to account for scheduled freeze of the Ical.",
                "END:VJOURNAL",
                "END:VCALENDAR",
            },
            ["Rfc7986"] = new[]
            {
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
                "IMAGE;DISPLAY=BADGE;FMTTYPE=image/png;VALUE=URI:http://example.com/images/party.png",
                "BEGIN:VEVENT",
                "DTSTAMP:20160101T120000Z",
                "UID:conference-1@example.com",
                "DTSTART:20160102T150000Z",
                "SUMMARY:Planning meeting",
                "COLOR:red",
                "IMAGE;DISPLAY=BADGE;FMTTYPE=image/png;VALUE=URI:http://example.com/images/party.png",
                "CONFERENCE;FEATURE=PHONE|MODERATOR;LABEL=Moderator dial-in;VALUE=URI:tel:+1-412-555-0123,,,654321",
                "CONFERENCE;FEATURE=PHONE;LABEL=Attendee dial-in;VALUE=URI:tel:+1-412-555-0123,,,555123",
                "CONFERENCE;FEATURE=PHONE;LABEL=Attendee dial-in;VALUE=URI:tel:+1-888-555-0456,,,555123",
                "CONFERENCE;FEATURE=CHAT;LABEL=Chat room;VALUE=URI:xmpp:chat-123@conference.example.com",
                "CONFERENCE;FEATURE=AUDIO|VIDEO;LABEL=Attendee dial-in;VALUE=URI:https://chat.example.com/audio?id=123456",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["GoogleExport"] = new[]
            {
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
                "ATTENDEE;CN=Jo;CUTYPE=INDIVIDUAL;PARTSTAT=ACCEPTED;ROLE=REQ-PARTICIPANT;X-NUM-GUESTS=0:mailto:jo@example.com",
                "CREATED:20260201T000000Z",
                "DESCRIPTION:Daily notes: https://example.com/notes\\nBring\\, as always\\, coffee.",
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
                "END:VCALENDAR",
            },
            ["OutlookExport"] = new[]
            {
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
                "UID:040000008200E00074C5B7101A82E00800000000F0A7A1B8D2F2DA010000000000000000100000004BBE5E0D4C8A7742B0B5C7B2B3E0A4A5",
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
                "END:VCALENDAR",
            },
            ["Rfc5545FreeBusy"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//RDU Software//NONSGML HandCal//EN",
                "BEGIN:VFREEBUSY",
                "ORGANIZER:mailto:jsmith@example.com",
                "DTSTART:19980313T141711Z",
                "DTEND:19980410T141711Z",
                "FREEBUSY:19980314T233000Z/19980315T003000Z",
                "FREEBUSY:19980316T153000Z/19980316T163000Z",
                "FREEBUSY:19980318T030000Z/19980318T040000Z",
                "URL:http://www.example.com/calendar/busytime/jsmith.ifb",
                "END:VFREEBUSY",
                "END:VCALENDAR",
            },
            ["Rfc5545Todo"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//ABC Corporation//NONSGML My Product//EN",
                "BEGIN:VTODO",
                "DTSTAMP:19980130T134500Z",
                "SEQUENCE:2",
                "UID:uid4@example.com",
                "ORGANIZER:mailto:unclesam@example.com",
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com",
                "DUE:19980415T000000",
                "STATUS:NEEDS-ACTION",
                "SUMMARY:Submit Income Taxes",
                "BEGIN:VALARM",
                "ACTION:AUDIO",
                "TRIGGER:19980403T120000Z",
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud",
                "REPEAT:4",
                "DURATION:PT1H",
                "END:VALARM",
                "END:VTODO",
                "END:VCALENDAR",
            },
            ["AppleExport"] = new[]
            {
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
                "END:VCALENDAR",
            },
            ["Example1Ical"] = new[]
            {
                "BEGIN:VCALENDAR",
                "CALSCALE:GREGORIAN",
                "PRODID:-//Example Inc.//Example Calendar//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:20080205T191224Z",
                "DTSTART;VALUE=DATE:20081006",
                "SUMMARY:Planning meeting",
                "UID:4088E990AD89CB3DBB484909",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Example2Ical"] = new[]
            {
                "BEGIN:VCALENDAR",
                "PRODID:-//Example Corp.//Example Client//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "LAST-MODIFIED:20040110T032845Z",
                "TZID:US/Eastern",
                "BEGIN:DAYLIGHT",
                "DTSTART:20000404T020000",
                "RRULE:FREQ=YEARLY;BYDAY=1SU;BYMONTH=4",
                "TZNAME:EDT",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "END:DAYLIGHT",
                "BEGIN:STANDARD",
                "DTSTART:20001026T020000",
                "RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10",
                "TZNAME:EST",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "END:STANDARD",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060102T120000",
                "DURATION:PT1H",
                "RRULE:FREQ=DAILY;COUNT=5",
                "RDATE;TZID=US/Eastern;VALUE=PERIOD:20060102T150000/PT2H",
                "SUMMARY:Event #2",
                "DESCRIPTION:We are having a meeting all this week at 12 pm for one hour\\, with an additional meeting on the first day 2 hours long.\\nPlease bring your own lunch for the 12 pm meetings.",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060104T140000",
                "DURATION:PT1H",
                "RECURRENCE-ID;TZID=US/Eastern:20060104T120000",
                "SUMMARY:Event #2 bis",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Extensions"] = new[]
            {
                "BEGIN:VCALENDAR",
                "VERSION:2.0",
                "PRODID:-//Test//Test//EN",
                "BEGIN:VEVENT",
                "UID:event@example.com",
                "DTSTAMP:20260101T000000Z",
                "DTSTART:20260101T090000Z",
                "X-PROPERTY:20110512T120000Z",
                "X-RAW:a\\,b;c:d",
                "X-TEXT;VALUE=TEXT:one\\, two,three\\nfour",
                "X-BINARY;ENCODING=BASE64;VALUE=BINARY:SGVsbG8=",
                "X-BOOLEAN;VALUE=BOOLEAN:FALSE",
                "X-CAL-ADDRESS;VALUE=CAL-ADDRESS:mailto:a@example.com",
                "X-DATES;VALUE=DATE:20260101,20260102",
                "X-DATE-TIME;VALUE=DATE-TIME:20260101T090000Z",
                "X-DURATION;VALUE=DURATION:-P1W",
                "X-FLOAT;VALUE=FLOAT:-1.250",
                "X-INTEGER;VALUE=INTEGER:-7",
                "X-PERIOD;VALUE=PERIOD:20260101T090000Z/PT1H",
                "X-RECUR;VALUE=RECUR:FREQ=MONTHLY;BYMONTHDAY=1,-1;COUNT=3",
                "X-TIME;VALUE=TIME:090000",
                "X-URI;VALUE=URI:http://example.com/a?b=c",
                "X-UTC-OFFSET;VALUE=UTC-OFFSET:-013015",
                "X-OTHER-TYPE;VALUE=X-NAME:anything",
                "DTEND;X-LIST=a|b,c;X-PARAM=PT30M:20260101T100000Z",
                "SUMMARY;LANGUAGE=en;X-QUOTED=a;b:Caret ^ and quote \" kept",
                "IANA-PROP;VALUE=TEXT:iana",
                "END:VEVENT",
                "END:VCALENDAR",
            },
            ["Example1IcalThenExample2Ical"] = new[]
            {
                "BEGIN:VCALENDAR",
                "CALSCALE:GREGORIAN",
                "PRODID:-//Example Inc.//Example Calendar//EN",
                "VERSION:2.0",
                "BEGIN:VEVENT",
                "DTSTAMP:20080205T191224Z",
                "DTSTART;VALUE=DATE:20081006",
                "SUMMARY:Planning meeting",
                "UID:4088E990AD89CB3DBB484909",
                "END:VEVENT",
                "END:VCALENDAR",
                "BEGIN:VCALENDAR",
                "PRODID:-//Example Corp.//Example Client//EN",
                "VERSION:2.0",
                "BEGIN:VTIMEZONE",
                "LAST-MODIFIED:20040110T032845Z",
                "TZID:US/Eastern",
                "BEGIN:DAYLIGHT",
                "DTSTART:20000404T020000",
                "RRULE:FREQ=YEARLY;BYDAY=1SU;BYMONTH=4",
                "TZNAME:EDT",
                "TZOFFSETFROM:-0500",
                "TZOFFSETTO:-0400",
                "END:DAYLIGHT",
                "BEGIN:STANDARD",
                "DTSTART:20001026T020000",
                "RRULE:FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10",
                "TZNAME:EST",
                "TZOFFSETFROM:-0400",
                "TZOFFSETTO:-0500",
                "END:STANDARD",
                "END:VTIMEZONE",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060102T120000",
                "DURATION:PT1H",
                "RRULE:FREQ=DAILY;COUNT=5",
                "RDATE;TZID=US/Eastern;VALUE=PERIOD:20060102T150000/PT2H",
                "SUMMARY:Event #2",
                "DESCRIPTION:We are having a meeting all this week at 12 pm for one hour\\, with an additional meeting on the first day 2 hours long.\\nPlease bring your own lunch for the 12 pm meetings.",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "BEGIN:VEVENT",
                "DTSTAMP:20060206T001121Z",
                "DTSTART;TZID=US/Eastern:20060104T140000",
                "DURATION:PT1H",
                "RECURRENCE-ID;TZID=US/Eastern:20060104T120000",
                "SUMMARY:Event #2 bis",
                "UID:00959BC664CA650E933C892C@example.com",
                "END:VEVENT",
                "END:VCALENDAR",
            },
        };

        /// <summary>
        /// Each of <see cref="CalendarFixtures.All"/> as a writer with the default options writes it: each content line unfolded, then folded
        /// again at 75 octets, without splitting a UTF-8 sequence, with CRLF and a space (RFC 5545 §3.1).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> WrittenTexts = new Dictionary<string, string>
        {
            ["Rfc5545Event"] =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN\r\n" +
                "VERSION:2.0\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTAMP:19960704T120000Z\r\n" +
                "UID:uid1@example.com\r\n" +
                "ORGANIZER:mailto:jsmith@example.com\r\n" +
                "DTSTART:19960918T143000Z\r\n" +
                "DTEND:19960920T220000Z\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "CATEGORIES:CONFERENCE\r\n" +
                "SUMMARY:Networld+Interop Conference\r\n" +
                "DESCRIPTION:Networld+Interop Conference and Exhibit\\nAtlanta World Congress\r\n" +
                "  Center\\nAtlanta\\, Georgia\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545TimeZone"] =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//RDU Software//NONSGML HandCal//EN\r\n" +
                "VERSION:2.0\r\n" +
                "BEGIN:VTIMEZONE\r\n" +
                "TZID:America/New_York\r\n" +
                "BEGIN:STANDARD\r\n" +
                "DTSTART:19981025T020000\r\n" +
                "TZOFFSETFROM:-0400\r\n" +
                "TZOFFSETTO:-0500\r\n" +
                "TZNAME:EST\r\n" +
                "END:STANDARD\r\n" +
                "BEGIN:DAYLIGHT\r\n" +
                "DTSTART:19990404T020000\r\n" +
                "TZOFFSETFROM:-0500\r\n" +
                "TZOFFSETTO:-0400\r\n" +
                "TZNAME:EDT\r\n" +
                "END:DAYLIGHT\r\n" +
                "END:VTIMEZONE\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTAMP:19980309T231000Z\r\n" +
                "UID:guid-1.example.com\r\n" +
                "ORGANIZER;ROLE=CHAIR:mailto:mrbig@example.com\r\n" +
                "ATTENDEE;RSVP=TRUE;ROLE=REQ-PARTICIPANT;CUTYPE=GROUP:mailto:employee-A@exam\r\n" +
                " ple.com\r\n" +
                "DESCRIPTION:Project XYZ Review Meeting\r\n" +
                "CATEGORIES:MEETING\r\n" +
                "CLASS:PUBLIC\r\n" +
                "CREATED:19980309T130000Z\r\n" +
                "SUMMARY:XYZ Project Review\r\n" +
                "DTSTART;TZID=America/New_York:19980312T083000\r\n" +
                "DTEND;TZID=America/New_York:19980312T093000\r\n" +
                "LOCATION:1CP Conference Room 4350\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545TodoCorrected"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//ABC Corporation//NONSGML My Product//EN\r\n" +
                "BEGIN:VTODO\r\n" +
                "DTSTAMP:19980130T134500Z\r\n" +
                "SEQUENCE:2\r\n" +
                "UID:uid4@example.com\r\n" +
                "ORGANIZER:mailto:unclesam@example.com\r\n" +
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com\r\n" +
                "DUE:19980415T000000\r\n" +
                "STATUS:NEEDS-ACTION\r\n" +
                "SUMMARY:Submit Income Taxes\r\n" +
                "BEGIN:VALARM\r\n" +
                "ACTION:AUDIO\r\n" +
                "TRIGGER;VALUE=DATE-TIME:19980403T120000Z\r\n" +
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud\r\n" +
                "REPEAT:4\r\n" +
                "DURATION:PT1H\r\n" +
                "END:VALARM\r\n" +
                "END:VTODO\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545Journal"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//ABC Corporation//NONSGML My Product//EN\r\n" +
                "BEGIN:VJOURNAL\r\n" +
                "DTSTAMP:19970324T120000Z\r\n" +
                "UID:uid5@example.com\r\n" +
                "ORGANIZER:mailto:jsmith@example.com\r\n" +
                "STATUS:DRAFT\r\n" +
                "CLASS:PUBLIC\r\n" +
                "CATEGORIES:Project Report,XYZ,Weekly Meeting\r\n" +
                "DESCRIPTION:Project xyz Review Meeting Minutes\\nAgenda\\n1. Review of projec\r\n" +
                " t version 1.0 requirements.\\n2. Definitionof project processes.\\n3. Review\r\n" +
                "  of project schedule.\\nParticipants: John Smith\\, Jane Doe\\, Jim Dandy\\n-I\r\n" +
                " t was decided that the requirements need to be signed off byproduct market\r\n" +
                " ing.\\n-Project processes were accepted.\\n-Project schedule needs to accoun\r\n" +
                " t for scheduled freeze of the Ical.\r\n" +
                "END:VJOURNAL\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc7986"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//Example Corp.//CalDAV Client//EN\r\n" +
                "NAME:Company Vacation Days\r\n" +
                "DESCRIPTION:The description of the calendar.\r\n" +
                "UID:5FC53010-1267-4F8E-BC28-1D7AE55A7C99\r\n" +
                "URL:http://example.com/calendar/vacation\r\n" +
                "LAST-MODIFIED:20110515T000000Z\r\n" +
                "CATEGORIES:MEETING\r\n" +
                "REFRESH-INTERVAL;VALUE=DURATION:P1W\r\n" +
                "SOURCE;VALUE=URI:https://example.com/holidays.ics\r\n" +
                "COLOR:turquoise\r\n" +
                "IMAGE;VALUE=URI;DISPLAY=BADGE;FMTTYPE=image/png:http://example.com/images/p\r\n" +
                " arty.png\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTAMP:20160101T120000Z\r\n" +
                "UID:conference-1@example.com\r\n" +
                "DTSTART:20160102T150000Z\r\n" +
                "SUMMARY:Planning meeting\r\n" +
                "COLOR:red\r\n" +
                "IMAGE;VALUE=URI;DISPLAY=BADGE;FMTTYPE=image/png:http://example.com/images/p\r\n" +
                " arty.png\r\n" +
                "CONFERENCE;VALUE=URI;FEATURE=PHONE,MODERATOR;LABEL=Moderator dial-in:tel:+1\r\n" +
                " -412-555-0123,,,654321\r\n" +
                "CONFERENCE;VALUE=URI;FEATURE=PHONE;LABEL=Attendee dial-in:tel:+1-412-555-01\r\n" +
                " 23,,,555123\r\n" +
                "CONFERENCE;VALUE=URI;FEATURE=PHONE;LABEL=Attendee dial-in:tel:+1-888-555-04\r\n" +
                " 56,,,555123\r\n" +
                "CONFERENCE;VALUE=URI;FEATURE=CHAT;LABEL=Chat room:xmpp:chat-123@conference.\r\n" +
                " example.com\r\n" +
                "CONFERENCE;VALUE=URI;FEATURE=AUDIO,VIDEO;LABEL=Attendee dial-in:https://cha\r\n" +
                " t.example.com/audio?id=123456\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["GoogleExport"] =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//Google Inc//Google Calendar 70.9054//EN\r\n" +
                "VERSION:2.0\r\n" +
                "CALSCALE:GREGORIAN\r\n" +
                "METHOD:PUBLISH\r\n" +
                "X-WR-CALNAME:Work\r\n" +
                "X-WR-TIMEZONE:Pacific/Auckland\r\n" +
                "BEGIN:VTIMEZONE\r\n" +
                "TZID:Pacific/Auckland\r\n" +
                "X-LIC-LOCATION:Pacific/Auckland\r\n" +
                "BEGIN:DAYLIGHT\r\n" +
                "TZOFFSETFROM:+1200\r\n" +
                "TZOFFSETTO:+1300\r\n" +
                "TZNAME:NZDT\r\n" +
                "DTSTART:19700927T020000\r\n" +
                "RRULE:FREQ=YEARLY;BYMONTH=9;BYDAY=-1SU\r\n" +
                "END:DAYLIGHT\r\n" +
                "BEGIN:STANDARD\r\n" +
                "TZOFFSETFROM:+1300\r\n" +
                "TZOFFSETTO:+1200\r\n" +
                "TZNAME:NZST\r\n" +
                "DTSTART:19700405T030000\r\n" +
                "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=1SU\r\n" +
                "END:STANDARD\r\n" +
                "END:VTIMEZONE\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTART;TZID=Pacific/Auckland:20260302T090000\r\n" +
                "DTEND;TZID=Pacific/Auckland:20260302T093000\r\n" +
                "RRULE:FREQ=WEEKLY;BYDAY=MO\r\n" +
                "DTSTAMP:20261015T010203Z\r\n" +
                "ORGANIZER;CN=Team:mailto:team@example.com\r\n" +
                "UID:7kukuqrfedlm2f9t0vq2ffm7d8@google.com\r\n" +
                "ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED;CN=Jo;X-N\r\n" +
                " UM-GUESTS=0:mailto:jo@example.com\r\n" +
                "CREATED:20260201T000000Z\r\n" +
                "DESCRIPTION:Daily notes: https://example.com/notes\\nBring\\, as always\\, cof\r\n" +
                " fee.\r\n" +
                "LAST-MODIFIED:20260210T000000Z\r\n" +
                "LOCATION:\r\n" +
                "SEQUENCE:0\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "SUMMARY:Stand-up\r\n" +
                "TRANSP:OPAQUE\r\n" +
                "BEGIN:VALARM\r\n" +
                "ACTION:DISPLAY\r\n" +
                "DESCRIPTION:This is an event reminder\r\n" +
                "TRIGGER:-P0DT0H10M0S\r\n" +
                "END:VALARM\r\n" +
                "END:VEVENT\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTART;VALUE=DATE:20261225\r\n" +
                "DTEND;VALUE=DATE:20261226\r\n" +
                "DTSTAMP:20261015T010203Z\r\n" +
                "UID:0a1b2c3d4e5f@google.com\r\n" +
                "CREATED:20260101T000000Z\r\n" +
                "LAST-MODIFIED:20260101T000000Z\r\n" +
                "SEQUENCE:0\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "SUMMARY:Christmas Day\r\n" +
                "TRANSP:TRANSPARENT\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["OutlookExport"] =
                "BEGIN:VCALENDAR\r\n" +
                "METHOD:PUBLISH\r\n" +
                "PRODID:Microsoft Exchange Server 2010\r\n" +
                "VERSION:2.0\r\n" +
                "X-WR-CALNAME:Calendar\r\n" +
                "BEGIN:VTIMEZONE\r\n" +
                "TZID:New Zealand Standard Time\r\n" +
                "BEGIN:STANDARD\r\n" +
                "DTSTART:16010101T030000\r\n" +
                "TZOFFSETFROM:+1300\r\n" +
                "TZOFFSETTO:+1200\r\n" +
                "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=1SU;BYMONTH=4\r\n" +
                "END:STANDARD\r\n" +
                "BEGIN:DAYLIGHT\r\n" +
                "DTSTART:16010101T020000\r\n" +
                "TZOFFSETFROM:+1200\r\n" +
                "TZOFFSETTO:+1300\r\n" +
                "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=9\r\n" +
                "END:DAYLIGHT\r\n" +
                "END:VTIMEZONE\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DESCRIPTION:\\n\r\n" +
                "UID:040000008200E00074C5B7101A82E00800000000F0A7A1B8D2F2DA01000000000000000\r\n" +
                " 0100000004BBE5E0D4C8A7742B0B5C7B2B3E0A4A5\r\n" +
                "SUMMARY:Project review\r\n" +
                "DTSTART;TZID=New Zealand Standard Time:20261020T100000\r\n" +
                "DTEND;TZID=New Zealand Standard Time:20261020T110000\r\n" +
                "CLASS:PUBLIC\r\n" +
                "PRIORITY:5\r\n" +
                "DTSTAMP:20261015T000000Z\r\n" +
                "TRANSP:OPAQUE\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "SEQUENCE:0\r\n" +
                "LOCATION:Room 1\r\n" +
                "X-MICROSOFT-CDO-APPT-SEQUENCE:0\r\n" +
                "X-MICROSOFT-CDO-BUSYSTATUS:BUSY\r\n" +
                "X-MICROSOFT-CDO-INTENDEDSTATUS:BUSY\r\n" +
                "X-MICROSOFT-CDO-ALLDAYEVENT:FALSE\r\n" +
                "X-MICROSOFT-CDO-IMPORTANCE:1\r\n" +
                "X-MICROSOFT-CDO-INSTTYPE:0\r\n" +
                "X-MICROSOFT-DONOTFORWARDMEETING:FALSE\r\n" +
                "X-MICROSOFT-DISALLOW-COUNTER:FALSE\r\n" +
                "BEGIN:VALARM\r\n" +
                "DESCRIPTION:REMINDER\r\n" +
                "TRIGGER;RELATED=START:-PT15M\r\n" +
                "ACTION:DISPLAY\r\n" +
                "END:VALARM\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545FreeBusy"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//RDU Software//NONSGML HandCal//EN\r\n" +
                "BEGIN:VFREEBUSY\r\n" +
                "ORGANIZER:mailto:jsmith@example.com\r\n" +
                "DTSTART:19980313T141711Z\r\n" +
                "DTEND:19980410T141711Z\r\n" +
                "FREEBUSY:19980314T233000Z/19980315T003000Z\r\n" +
                "FREEBUSY:19980316T153000Z/19980316T163000Z\r\n" +
                "FREEBUSY:19980318T030000Z/19980318T040000Z\r\n" +
                "URL:http://www.example.com/calendar/busytime/jsmith.ifb\r\n" +
                "END:VFREEBUSY\r\n" +
                "END:VCALENDAR\r\n",
            ["Rfc5545Todo"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//ABC Corporation//NONSGML My Product//EN\r\n" +
                "BEGIN:VTODO\r\n" +
                "DTSTAMP:19980130T134500Z\r\n" +
                "SEQUENCE:2\r\n" +
                "UID:uid4@example.com\r\n" +
                "ORGANIZER:mailto:unclesam@example.com\r\n" +
                "ATTENDEE;PARTSTAT=ACCEPTED:mailto:jqpublic@example.com\r\n" +
                "DUE:19980415T000000\r\n" +
                "STATUS:NEEDS-ACTION\r\n" +
                "SUMMARY:Submit Income Taxes\r\n" +
                "BEGIN:VALARM\r\n" +
                "ACTION:AUDIO\r\n" +
                "TRIGGER:19980403T120000Z\r\n" +
                "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-files/ssbanner.aud\r\n" +
                "REPEAT:4\r\n" +
                "DURATION:PT1H\r\n" +
                "END:VALARM\r\n" +
                "END:VTODO\r\n" +
                "END:VCALENDAR\r\n",
            ["AppleExport"] =
                "BEGIN:VCALENDAR\r\n" +
                "VERSION:2.0\r\n" +
                "PRODID:-//Apple Inc.//macOS 15.0//EN\r\n" +
                "CALSCALE:GREGORIAN\r\n" +
                "BEGIN:VTIMEZONE\r\n" +
                "TZID:Europe/London\r\n" +
                "BEGIN:DAYLIGHT\r\n" +
                "TZOFFSETFROM:+0000\r\n" +
                "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=-1SU\r\n" +
                "DTSTART:19810329T010000\r\n" +
                "TZNAME:BST\r\n" +
                "TZOFFSETTO:+0100\r\n" +
                "END:DAYLIGHT\r\n" +
                "BEGIN:STANDARD\r\n" +
                "TZOFFSETFROM:+0100\r\n" +
                "RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU\r\n" +
                "DTSTART:19961027T020000\r\n" +
                "TZNAME:GMT\r\n" +
                "TZOFFSETTO:+0000\r\n" +
                "END:STANDARD\r\n" +
                "END:VTIMEZONE\r\n" +
                "BEGIN:VEVENT\r\n" +
                "CREATED:20260901T101010Z\r\n" +
                "UID:4A0C7E1B-2D3F-4E5A-8B9C-0D1E2F3A4B5C\r\n" +
                "DTEND;TZID=Europe/London:20261103T130000\r\n" +
                "TRANSP:OPAQUE\r\n" +
                "X-APPLE-TRAVEL-ADVISORY-BEHAVIOR:AUTOMATIC\r\n" +
                "SUMMARY:Lunch\r\n" +
                "LAST-MODIFIED:20260901T101111Z\r\n" +
                "DTSTAMP:20260901T101111Z\r\n" +
                "DTSTART;TZID=Europe/London:20261103T120000\r\n" +
                "SEQUENCE:1\r\n" +
                "X-APPLE-STRUCTURED-LOCATION;VALUE=URI;X-TITLE=Cafe:geo:51.5,-0.12\r\n" +
                "BEGIN:VALARM\r\n" +
                "X-WR-ALARMUID:9E5D1C7B-1111-2222-3333-444455556666\r\n" +
                "UID:9E5D1C7B-1111-2222-3333-444455556666\r\n" +
                "TRIGGER:-PT30M\r\n" +
                "ATTACH;VALUE=URI:Chord\r\n" +
                "ACTION:AUDIO\r\n" +
                "X-APPLE-DEFAULT-ALARM:TRUE\r\n" +
                "ACKNOWLEDGED:20261103T113000Z\r\n" +
                "END:VALARM\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
            ["GoogleExportThenOutlookExport"] =
                "BEGIN:VCALENDAR\r\n" +
                "PRODID:-//Google Inc//Google Calendar 70.9054//EN\r\n" +
                "VERSION:2.0\r\n" +
                "CALSCALE:GREGORIAN\r\n" +
                "METHOD:PUBLISH\r\n" +
                "X-WR-CALNAME:Work\r\n" +
                "X-WR-TIMEZONE:Pacific/Auckland\r\n" +
                "BEGIN:VTIMEZONE\r\n" +
                "TZID:Pacific/Auckland\r\n" +
                "X-LIC-LOCATION:Pacific/Auckland\r\n" +
                "BEGIN:DAYLIGHT\r\n" +
                "TZOFFSETFROM:+1200\r\n" +
                "TZOFFSETTO:+1300\r\n" +
                "TZNAME:NZDT\r\n" +
                "DTSTART:19700927T020000\r\n" +
                "RRULE:FREQ=YEARLY;BYMONTH=9;BYDAY=-1SU\r\n" +
                "END:DAYLIGHT\r\n" +
                "BEGIN:STANDARD\r\n" +
                "TZOFFSETFROM:+1300\r\n" +
                "TZOFFSETTO:+1200\r\n" +
                "TZNAME:NZST\r\n" +
                "DTSTART:19700405T030000\r\n" +
                "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=1SU\r\n" +
                "END:STANDARD\r\n" +
                "END:VTIMEZONE\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTART;TZID=Pacific/Auckland:20260302T090000\r\n" +
                "DTEND;TZID=Pacific/Auckland:20260302T093000\r\n" +
                "RRULE:FREQ=WEEKLY;BYDAY=MO\r\n" +
                "DTSTAMP:20261015T010203Z\r\n" +
                "ORGANIZER;CN=Team:mailto:team@example.com\r\n" +
                "UID:7kukuqrfedlm2f9t0vq2ffm7d8@google.com\r\n" +
                "ATTENDEE;CUTYPE=INDIVIDUAL;ROLE=REQ-PARTICIPANT;PARTSTAT=ACCEPTED;CN=Jo;X-N\r\n" +
                " UM-GUESTS=0:mailto:jo@example.com\r\n" +
                "CREATED:20260201T000000Z\r\n" +
                "DESCRIPTION:Daily notes: https://example.com/notes\\nBring\\, as always\\, cof\r\n" +
                " fee.\r\n" +
                "LAST-MODIFIED:20260210T000000Z\r\n" +
                "LOCATION:\r\n" +
                "SEQUENCE:0\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "SUMMARY:Stand-up\r\n" +
                "TRANSP:OPAQUE\r\n" +
                "BEGIN:VALARM\r\n" +
                "ACTION:DISPLAY\r\n" +
                "DESCRIPTION:This is an event reminder\r\n" +
                "TRIGGER:-P0DT0H10M0S\r\n" +
                "END:VALARM\r\n" +
                "END:VEVENT\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DTSTART;VALUE=DATE:20261225\r\n" +
                "DTEND;VALUE=DATE:20261226\r\n" +
                "DTSTAMP:20261015T010203Z\r\n" +
                "UID:0a1b2c3d4e5f@google.com\r\n" +
                "CREATED:20260101T000000Z\r\n" +
                "LAST-MODIFIED:20260101T000000Z\r\n" +
                "SEQUENCE:0\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "SUMMARY:Christmas Day\r\n" +
                "TRANSP:TRANSPARENT\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n" +
                "BEGIN:VCALENDAR\r\n" +
                "METHOD:PUBLISH\r\n" +
                "PRODID:Microsoft Exchange Server 2010\r\n" +
                "VERSION:2.0\r\n" +
                "X-WR-CALNAME:Calendar\r\n" +
                "BEGIN:VTIMEZONE\r\n" +
                "TZID:New Zealand Standard Time\r\n" +
                "BEGIN:STANDARD\r\n" +
                "DTSTART:16010101T030000\r\n" +
                "TZOFFSETFROM:+1300\r\n" +
                "TZOFFSETTO:+1200\r\n" +
                "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=1SU;BYMONTH=4\r\n" +
                "END:STANDARD\r\n" +
                "BEGIN:DAYLIGHT\r\n" +
                "DTSTART:16010101T020000\r\n" +
                "TZOFFSETFROM:+1200\r\n" +
                "TZOFFSETTO:+1300\r\n" +
                "RRULE:FREQ=YEARLY;INTERVAL=1;BYDAY=-1SU;BYMONTH=9\r\n" +
                "END:DAYLIGHT\r\n" +
                "END:VTIMEZONE\r\n" +
                "BEGIN:VEVENT\r\n" +
                "DESCRIPTION:\\n\r\n" +
                "UID:040000008200E00074C5B7101A82E00800000000F0A7A1B8D2F2DA01000000000000000\r\n" +
                " 0100000004BBE5E0D4C8A7742B0B5C7B2B3E0A4A5\r\n" +
                "SUMMARY:Project review\r\n" +
                "DTSTART;TZID=New Zealand Standard Time:20261020T100000\r\n" +
                "DTEND;TZID=New Zealand Standard Time:20261020T110000\r\n" +
                "CLASS:PUBLIC\r\n" +
                "PRIORITY:5\r\n" +
                "DTSTAMP:20261015T000000Z\r\n" +
                "TRANSP:OPAQUE\r\n" +
                "STATUS:CONFIRMED\r\n" +
                "SEQUENCE:0\r\n" +
                "LOCATION:Room 1\r\n" +
                "X-MICROSOFT-CDO-APPT-SEQUENCE:0\r\n" +
                "X-MICROSOFT-CDO-BUSYSTATUS:BUSY\r\n" +
                "X-MICROSOFT-CDO-INTENDEDSTATUS:BUSY\r\n" +
                "X-MICROSOFT-CDO-ALLDAYEVENT:FALSE\r\n" +
                "X-MICROSOFT-CDO-IMPORTANCE:1\r\n" +
                "X-MICROSOFT-CDO-INSTTYPE:0\r\n" +
                "X-MICROSOFT-DONOTFORWARDMEETING:FALSE\r\n" +
                "X-MICROSOFT-DISALLOW-COUNTER:FALSE\r\n" +
                "BEGIN:VALARM\r\n" +
                "DESCRIPTION:REMINDER\r\n" +
                "TRIGGER;RELATED=START:-PT15M\r\n" +
                "ACTION:DISPLAY\r\n" +
                "END:VALARM\r\n" +
                "END:VEVENT\r\n" +
                "END:VCALENDAR\r\n",
        };

        /// <summary>
        /// How many calendars, components other than VCALENDAR, content lines and physical lines each of <see cref="CalendarFixtures.All"/>
        /// has.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, FixtureCounts> Counts = new Dictionary<string, FixtureCounts>
        {
            ["Rfc5545Event"] = new FixtureCounts(1, 1, 15, 17),
            ["Rfc5545TimeZone"] = new FixtureCounts(1, 4, 33, 34),
            ["Rfc5545TodoCorrected"] = new FixtureCounts(1, 2, 21, 22),
            ["Rfc5545Journal"] = new FixtureCounts(1, 1, 13, 21),
            ["Rfc7986"] = new FixtureCounts(1, 1, 27, 27),
            ["GoogleExport"] = new FixtureCounts(1, 6, 60, 62),
            ["OutlookExport"] = new FixtureCounts(1, 5, 48, 49),
            ["Rfc5545FreeBusy"] = new FixtureCounts(1, 1, 13, 13),
            ["Rfc5545Todo"] = new FixtureCounts(1, 2, 21, 22),
            ["AppleExport"] = new FixtureCounts(1, 5, 44, 44),
        };
    }

    internal sealed class FixtureCounts
    {
        public FixtureCounts(int calendars, int components, int lines, int physicalLines)
        {
            Calendars = calendars;
            Components = components;
            Lines = lines;
            PhysicalLines = physicalLines;
        }

        public int Calendars { get; }

        public int Components { get; }

        public int Lines { get; }

        public int PhysicalLines { get; }
    }
}
