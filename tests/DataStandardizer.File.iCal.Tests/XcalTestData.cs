using System.Text;
using System.Xml;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// The examples of RFC 6321, and helpers to read and write xCal for the tests.
/// </summary>
internal static class XcalTestData
{
    /// <summary>
    /// RFC 6321 Appendix B.1, in iCalendar form.
    /// </summary>
    public static readonly string Example1Ical = Crlf(
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
        "END:VCALENDAR");

    /// <summary>
    /// RFC 6321 Appendix B.1, in xCal form.
    /// </summary>
    /// <remarks>
    /// The iCalendar form in the RFC has <c>DTSTART:20081006</c> with no <c>VALUE=DATE</c>, which RFC 5545 requires for a DATE; the xCal
    /// form, with its <c>date</c> element, is the one followed here.
    /// </remarks>
    public const string Example1Xcal =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <icalendar xmlns="urn:ietf:params:xml:ns:icalendar-2.0">
         <vcalendar>
          <properties>
           <calscale>
             <text>GREGORIAN</text>
           </calscale>
           <prodid>
            <text>-//Example Inc.//Example Calendar//EN</text>
           </prodid>
           <version>
             <text>2.0</text>
           </version>
          </properties>
          <components>
           <vevent>
            <properties>
             <dtstamp>
               <date-time>2008-02-05T19:12:24Z</date-time>
             </dtstamp>
             <dtstart>
               <date>2008-10-06</date>
             </dtstart>
             <summary>
              <text>Planning meeting</text>
             </summary>
             <uid>
              <text>4088E990AD89CB3DBB484909</text>
             </uid>
            </properties>
           </vevent>
          </components>
         </vcalendar>
        </icalendar>
        """;

    /// <summary>
    /// RFC 6321 Appendix B.2, in iCalendar form.
    /// </summary>
    /// <remarks>
    /// The RFC leaves out <c>BEGIN:VCALENDAR</c>, and its PRODID and the order of its calendar properties differ from the xCal form's; all are
    /// corrected here.
    /// </remarks>
    public static readonly string Example2Ical = Crlf(
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
        "DESCRIPTION:We are having a meeting all this week at 12 pm fo",
        " r one hour\\, with an additional meeting on the first day 2 h",
        " ours long.\\nPlease bring your own lunch for the 12 pm meetin",
        " gs.",
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
        "END:VCALENDAR");

    /// <summary>
    /// RFC 6321 Appendix B.2, in xCal form.
    /// </summary>
    /// <remarks>
    /// The RFC gives the TZID property of the VTIMEZONE as <c>&lt;tzid&gt;US/Eastern&lt;/tzid&gt;</c>, with no <c>text</c> element, and wraps
    /// the description over several lines to fit the page; both are corrected here.
    /// </remarks>
    public const string Example2Xcal =
        """
        <?xml version="1.0" encoding="utf-8" ?>
        <icalendar xmlns="urn:ietf:params:xml:ns:icalendar-2.0">
          <vcalendar>
            <properties>
              <prodid>
                <text>-//Example Inc.//Example Client//EN</text>
              </prodid>
              <version>
                <text>2.0</text>
              </version>
            </properties>
            <components>
              <vtimezone>
                <properties>
                  <last-modified>
                    <date-time>2004-01-10T03:28:45Z</date-time>
                  </last-modified>
                  <tzid><text>US/Eastern</text></tzid>
                </properties>
                <components>
                  <daylight>
                    <properties>
                      <dtstart>
                        <date-time>2000-04-04T02:00:00</date-time>
                      </dtstart>
                      <rrule>
                        <recur>
                          <freq>YEARLY</freq>
                          <byday>1SU</byday>
                          <bymonth>4</bymonth>
                        </recur>
                      </rrule>
                      <tzname>
                        <text>EDT</text>
                      </tzname>
                      <tzoffsetfrom>
                        <utc-offset>-05:00</utc-offset>
                      </tzoffsetfrom>
                      <tzoffsetto>
                        <utc-offset>-04:00</utc-offset>
                      </tzoffsetto>
                    </properties>
                  </daylight>
                  <standard>
                    <properties>
                      <dtstart>
                        <date-time>2000-10-26T02:00:00</date-time>
                      </dtstart>
                      <rrule>
                        <recur>
                          <freq>YEARLY</freq>
                          <byday>-1SU</byday>
                          <bymonth>10</bymonth>
                        </recur>
                      </rrule>
                      <tzname>
                        <text>EST</text>
                      </tzname>
                      <tzoffsetfrom>
                        <utc-offset>-04:00</utc-offset>
                      </tzoffsetfrom>
                      <tzoffsetto>
                        <utc-offset>-05:00</utc-offset>
                      </tzoffsetto>
                    </properties>
                  </standard>
                </components>
              </vtimezone>
              <vevent>
                <properties>
                  <dtstamp>
                    <date-time>2006-02-06T00:11:21Z</date-time>
                  </dtstamp>
                  <dtstart>
                    <parameters>
                      <tzid><text>US/Eastern</text></tzid>
                    </parameters>
                    <date-time>2006-01-02T12:00:00</date-time>
                  </dtstart>
                  <duration>
                    <duration>PT1H</duration>
                  </duration>
                  <rrule>
                    <recur>
                      <freq>DAILY</freq>
                      <count>5</count>
                    </recur>
                  </rrule>
                  <rdate>
                    <parameters>
                      <tzid><text>US/Eastern</text></tzid>
                    </parameters>
                    <period>
                      <start>2006-01-02T15:00:00</start>
                      <duration>PT2H</duration>
                    </period>
                  </rdate>
                  <summary>
                    <text>Event #2</text>
                  </summary>
                  <description>
                    <text>We are having a meeting all this week at 12 pm for one hour, with an additional meeting on the first day 2 hours long.&#x0a;Please bring your own lunch for the 12 pm meetings.</text>
                  </description>
                  <uid>
                    <text>00959BC664CA650E933C892C@example.com</text>
                  </uid>
                </properties>
              </vevent>
              <vevent>
                <properties>
                  <dtstamp>
                    <date-time>2006-02-06T00:11:21Z</date-time>
                  </dtstamp>
                  <dtstart>
                    <parameters>
                      <tzid><text>US/Eastern</text></tzid>
                    </parameters>
                    <date-time>2006-01-04T14:00:00</date-time>
                  </dtstart>
                  <duration>
                    <duration>PT1H</duration>
                  </duration>
                  <recurrence-id>
                    <parameters>
                      <tzid><text>US/Eastern</text></tzid>
                    </parameters>
                    <date-time>2006-01-04T12:00:00</date-time>
                  </recurrence-id>
                  <summary>
                    <text>Event #2 bis</text>
                  </summary>
                  <uid>
                    <text>00959BC664CA650E933C892C@example.com</text>
                  </uid>
                </properties>
              </vevent>
            </components>
          </vcalendar>
        </icalendar>
        """;

    /// <summary>
    /// Wraps elements in an xCal document holding one calendar, whose properties are given.
    /// </summary>
    public static string Document(string calendarContent)
    {
        return "<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"><vcalendar>" + calendarContent + "</vcalendar></icalendar>";
    }

    /// <summary>
    /// Builds an xCal document holding one calendar with the required properties and one VJOURNAL holding the given property elements.
    /// </summary>
    public static string JournalDocument(string journalProperties)
    {
        return Document(
            "<properties><prodid><text>-//Test//Test//EN</text></prodid><version><text>2.0</text></version></properties>" +
            "<components><vjournal><properties><uid><text>j@example.com</text></uid><dtstamp><date-time>2026-01-01T00:00:00Z</date-time></dtstamp>" +
            journalProperties + "</properties></vjournal></components>");
    }

    public static List<IcalCalendar> ReadXcal(string xml, IcalOptions? options = null)
    {
        using var reader = new XcalReader(new StringReader(xml), options ?? new IcalOptions());
        return reader.ReadCalendars().ToList();
    }

    public static string WriteXcal(IEnumerable<IcalCalendar> calendars, IcalOptions? options = null)
    {
        var text = new StringWriter();
        using (var writer = new XcalWriter(text, options ?? new IcalOptions()))
        {
            writer.WriteCalendars(calendars);
        }

        return text.ToString();
    }

    /// <summary>
    /// Gets the content lines of calendars, each as its name, parameters and value, without line numbers or the quoting of parameter values,
    /// so that calendars with the same properties compare equal whichever format they were read from.  The parameters are sorted, since their
    /// order has no meaning and the VALUE parameter is placed last when it is read from xCal.
    /// </summary>
    public static List<string> Model(IEnumerable<IcalCalendar> calendars, IcalOptions? options = null)
    {
        return ModelTestData.ContentLines(ModelTestData.WriteCalendars(calendars, options))
            .Select(line => line.Name + string.Concat(line.Parameters.OrderBy(parameter => parameter.Name, StringComparer.Ordinal)
                                .Select(parameter => ";" + parameter.Name + "=" + string.Join("|", parameter.Values))) + ":" + line.Value)
            .ToList();
    }

    /// <summary>
    /// Gets the content lines of the one property of the VJOURNAL in a calendar built by <see cref="JournalDocument"/>, after the UID and
    /// DTSTAMP.
    /// </summary>
    public static IcalContentLine JournalProperty(IcalCalendar calendar)
    {
        return calendar.Journals.Single().Properties[2];
    }

    public static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);

    public static XmlReader CreateAsyncXmlReader(TextReader reader)
    {
        return XmlReader.Create(reader, new XmlReaderSettings { Async = true });
    }
}
