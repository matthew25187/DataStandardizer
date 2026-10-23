using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Examples taken from RFC 5545 and RFC 6868, with CRLF line breaks.
    /// </summary>
    internal static class RfcFixtures
    {
        /// <summary>
        /// RFC 5545 §4, first example: a VEVENT with a folded, escaped DESCRIPTION.
        /// </summary>
        public static readonly string Rfc5545Event = Crlf(
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
            "DESCRIPTION:Networld+Interop Conference",
            "  and Exhibit\\nAtlanta World Congress Center\\n",
            " Atlanta\\, Georgia",
            "END:VEVENT",
            "END:VCALENDAR");

        /// <summary>
        /// RFC 5545 §4, second example: a VTIMEZONE and a VEVENT using it, with parameters and a fold after the colon.
        /// </summary>
        public static readonly string Rfc5545TimeZone = Crlf(
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
            "ATTENDEE;RSVP=TRUE;ROLE=REQ-PARTICIPANT;CUTYPE=GROUP:",
            " mailto:employee-A@example.com",
            "DESCRIPTION:Project XYZ Review Meeting",
            "CATEGORIES:MEETING",
            "CLASS:PUBLIC",
            "CREATED:19980309T130000Z",
            "SUMMARY:XYZ Project Review",
            "DTSTART;TZID=America/New_York:19980312T083000",
            "DTEND;TZID=America/New_York:19980312T093000",
            "LOCATION:1CP Conference Room 4350",
            "END:VEVENT",
            "END:VCALENDAR");

        /// <summary>
        /// RFC 5545 §4, third example: a VTODO with a VALARM whose ATTACH value is folded.
        /// </summary>
        public static readonly string Rfc5545Todo = Crlf(
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
            "ATTACH;FMTTYPE=audio/basic:http://example.com/pub/audio-",
            " files/ssbanner.aud",
            "REPEAT:4",
            "DURATION:PT1H",
            "END:VALARM",
            "END:VTODO",
            "END:VCALENDAR");

        /// <summary>
        /// RFC 5545 §4, fifth example: a VFREEBUSY.
        /// </summary>
        public static readonly string Rfc5545FreeBusy = Crlf(
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
            "END:VCALENDAR");

        /// <summary>
        /// Content lines from the parameter and property examples of RFC 5545 §3.2 and §3.8.
        /// </summary>
        public static readonly string Rfc5545Parameters = Crlf(
            "ATTENDEE;DELEGATED-FROM=\"mailto:jsmith@example.com\":mailto:jdoe@example.com",
            "ATTENDEE;MEMBER=\"mailto:projectA@example.com\",\"mailto:projectB@example.com\":mailto:janedoe@example.com",
            "ORGANIZER;CN=\"John Smith\":mailto:jsmith@example.com",
            "ORGANIZER;SENT-BY=\"mailto:sray@example.com\":mailto:jsmith@example.com",
            "DESCRIPTION;ALTREP=\"cid:part1.0001@example.org\":The Fall'98 Wild Wizards Conference - - Las Vegas\\, NV\\, USA",
            "ATTENDEE;CUTYPE=GROUP:mailto:ietf-calsch@example.org",
            "ATTENDEE;ROLE=CHAIR:mailto:mrbig@example.com",
            "SUMMARY;LANGUAGE=en-US:Company Holiday Party",
            "LOCATION;LANGUAGE=no:Tyskland",
            "RDATE;VALUE=DATE:19970304,19970504,19970704,19970904",
            "RRULE:FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH",
            "X-ABC-MMSUBJ;VALUE=URI;FMTTYPE=audio/basic:http://www.example.org/mysubj.au",
            "GEO:37.386013;-122.082932",
            "REQUEST-STATUS:3.1;Invalid property value;DTSTART:96-Apr-01");

        /// <summary>
        /// The examples of RFC 6868 §4.
        /// </summary>
        public static readonly string Rfc6868 = Crlf(
            "GEO;X-ADDRESS=\"Pittsburgh Pirates^n115 Federal St^nPittsburgh, PA 15",
            " 212\":40.446816;-80.00679",
            "ATTENDEE;CN=George Herman ^'Babe^' Ruth:mailto:babe@example.com");

        public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>
        {
            [nameof(Rfc5545Event)] = Rfc5545Event,
            [nameof(Rfc5545TimeZone)] = Rfc5545TimeZone,
            [nameof(Rfc5545Todo)] = Rfc5545Todo,
            [nameof(Rfc5545FreeBusy)] = Rfc5545FreeBusy,
            [nameof(Rfc5545Parameters)] = Rfc5545Parameters,
            [nameof(Rfc6868)] = Rfc6868
        };
    }
}
