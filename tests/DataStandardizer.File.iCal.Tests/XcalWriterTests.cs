using System.Text;
using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests;

public class XcalWriterTests
{
    private static readonly XNamespace Ns = "urn:ietf:params:xml:ns:icalendar-2.0";

    [Theory]
    [InlineData(nameof(Example1Ical))]
    [InlineData(nameof(Example2Ical))]
    public void WriteCalendars_RfcExample_MatchesXcalForm(string exampleName)
    {
        // arrange
        var (ical, xcal) = exampleName == nameof(Example1Ical) ? (Example1Ical, Example1Xcal) : (Example2Ical, Example2Xcal);

        // act
        var testResult = WriteXcal(ReadCalendars(ical));

        // assert: the same XML, ignoring formatting.
        Normalize(XDocument.Parse(testResult)).Should().Be(Normalize(XDocument.Parse(xcal)));
    }

    [Fact]
    public void WriteCalendar_Stream_WritesUtf8DocumentWithoutByteOrderMark()
    {
        // arrange
        var stream = new MemoryStream();

        // act
        using (var writer = new XcalWriter(stream))
        {
            writer.WriteCalendar(ReadCalendar(Example1Ical));
        }

        // assert
        var bytes = stream.ToArray();
        bytes.Take(5).Should().Equal(Encoding.ASCII.GetBytes("<?xml"));
        var text = Encoding.UTF8.GetString(bytes);
        text.Should().Contain("encoding=\"utf-8\"");
        text.Should().Contain("<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\">");
        text.Should().Contain("\r\n  <vcalendar>");
        text.Should().EndWith("</icalendar>");
        stream.CanWrite.Should().BeTrue();
    }

    [Fact]
    public void WriteCalendar_LineBreakOption_SetsXmlLineBreaks()
    {
        // arrange
        var text = new StringWriter();

        // act
        using (var writer = new XcalWriter(text, new IcalOptions { LineBreak = "\n" }))
        {
            writer.WriteCalendar(ReadCalendar(Example1Ical));
        }

        // assert
        text.ToString().Should().NotContain("\r").And.Contain("\n  <vcalendar>");
    }

    [Fact]
    public void WriteCalendar_ValueTypes_WriteTypedElements()
    {
        // arrange
        var calendar = ReadCalendar(EventCalendar(
            "X-UNKNOWN:20110512T120000Z",
            "X-DATE;VALUE=DATE:20110517",
            "X-LIST;VALUE=TEXT:a\\,b,c",
            "EXDATE;TZID=Europe/London:20260102T090000,20260103T090000",
            "RDATE;VALUE=PERIOD:20260104T090000Z/PT1H,20260105T090000Z/20260105T100000Z",
            "RRULE:FREQ=WEEKLY;UNTIL=20261231T235959Z;WKST=MO;BYDAY=TU,FR",
            "GEO:37.386013;-122.082932",
            "REQUEST-STATUS:3.1;Invalid property value;DTSTART:96-Apr-01",
            "ATTENDEE;RSVP=TRUE;MEMBER=\"mailto:a@example.com\";X-FOO=bar:mailto:jane@example.com",
            "BEGIN:VALARM", "ACTION:AUDIO", "TRIGGER;VALUE=DATE-TIME:20260101T080000Z", "END:VALARM"),
            new IcalOptions { AllowUndefinedTimeZones = true });

        // act
        var document = XDocument.Parse(WriteXcal([calendar], new IcalOptions { AllowUndefinedTimeZones = true }));

        // assert
        var properties = document.Descendants(Ns + "vevent").Single().Element(Ns + "properties")!;
        Inner(properties, "x-unknown").Should().Be("<unknown>20110512T120000Z</unknown>");
        Inner(properties, "x-date").Should().Be("<date>2011-05-17</date>");
        Inner(properties, "x-list").Should().Be("<text>a,b</text><text>c</text>");
        Inner(properties, "exdate").Should().Be(
            "<parameters><tzid><text>Europe/London</text></tzid></parameters>" +
            "<date-time>2026-01-02T09:00:00</date-time><date-time>2026-01-03T09:00:00</date-time>");
        Inner(properties, "rdate").Should().Be(
            "<period><start>2026-01-04T09:00:00Z</start><duration>PT1H</duration></period>" +
            "<period><start>2026-01-05T09:00:00Z</start><end>2026-01-05T10:00:00Z</end></period>");
        Inner(properties, "rrule").Should().Be(
            "<recur><freq>WEEKLY</freq><until>2026-12-31T23:59:59Z</until><wkst>MO</wkst><byday>TU</byday><byday>FR</byday></recur>");
        Inner(properties, "geo").Should().Be("<latitude>37.386013</latitude><longitude>-122.082932</longitude>");
        Inner(properties, "request-status").Should().Be(
            "<code>3.1</code><description>Invalid property value</description><data>DTSTART:96-Apr-01</data>");
        Inner(properties, "attendee").Should().Be(
            "<parameters><rsvp><boolean>true</boolean></rsvp><member><cal-address>mailto:a@example.com</cal-address></member>" +
            "<x-foo><unknown>bar</unknown></x-foo></parameters><cal-address>mailto:jane@example.com</cal-address>");
        Inner(document.Descendants(Ns + "valarm").Single().Element(Ns + "properties")!, "trigger")
            .Should().Be("<date-time>2026-01-01T08:00:00Z</date-time>");
    }

    [Fact]
    public void WriteCalendar_InvalidValueAllowed_WritesUnknownWithValueParameter()
    {
        // arrange
        var options = new IcalOptions { AllowInvalidValues = true };
        var calendar = ReadCalendar(EventCalendar("DTEND;VALUE=DATE:2026-01-02"), options);

        // act
        var document = XDocument.Parse(WriteXcal([calendar], options));

        // assert
        Inner(document.Descendants(Ns + "properties").Last(), "dtend")
            .Should().Be("<parameters><value><text>DATE</text></value></parameters><unknown>2026-01-02</unknown>");
    }

    [Fact]
    public void WriteCalendar_Version_IsAlways20AndFirstIfMissing()
    {
        // arrange
        var options = new IcalOptions { AllowMissingRequiredProperties = true };
        var calendar = ReadCalendar(IcalTestData.Crlf("BEGIN:VCALENDAR", "PRODID:-//Test//Test//EN", "BEGIN:VJOURNAL", "UID:j@example.com",
            "DTSTAMP:20260101T000000Z", "END:VJOURNAL", "END:VCALENDAR"), options);

        // act
        var document = XDocument.Parse(WriteXcal([calendar], options));

        // assert
        var properties = document.Root!.Element(Ns + "vcalendar")!.Element(Ns + "properties")!.Elements().ToList();
        properties.Select(element => element.Name.LocalName).Should().Equal("version", "prodid");
        properties[0].Value.Should().Be("2.0");
    }

    [Fact]
    public void WriteCalendars_Progress_MatchesIcalWriter()
    {
        // arrange
        var calendars = ReadCalendars(Example2Ical + Example1Ical);
        var icalProgress = new RecordingProgress();
        using (var icalWriter = new IcalWriter(new StringWriter()))
        {
            icalWriter.WriteCalendars(calendars, icalProgress);
        }

        var testResult = new RecordingProgress();
        using var writer = new XcalWriter(new StringWriter());

        // act
        writer.WriteCalendars(calendars, testResult);

        // assert
        testResult.Reports.Should().Equal(icalProgress.Reports);
        writer.CalendarsWritten.Should().Be(2);
        writer.ComponentsWritten.Should().Be(6);
        writer.PropertiesWritten.Should().Be(35);
    }

    [Fact]
    public void WriteCalendar_NonConformant_ThrowsAndWritesNothing()
    {
        // arrange
        var calendar = ReadCalendar(ConformanceCases.All["MissingUid"].Text, new IcalOptions { AllowMissingRequiredProperties = true });
        var text = new StringWriter();
        var writer = new XcalWriter(text);

        // act
        var act = () => writer.WriteCalendar(calendar);

        // assert
        var exception = act.Should().Throw<IcalFileException>().Which;
        exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.MissingRequiredProperty);
        writer.Dispose();
        text.ToString().Should().BeEmpty();
    }

    [Fact]
    public void WriteCalendar_NameNotValidInXml_Throws()
    {
        // arrange
        var calendar = ReadCalendar(EventCalendar("X-OK:1"));
        calendar.Events.Single().Properties.Add(new IcalContentLine("1X", "value"));
        var text = new StringWriter();

        // act
        using (var writer = new XcalWriter(text))
        {
            writer.Invoking(value => value.WriteCalendar(calendar)).Should().Throw<IcalFileException>().WithMessage("*must start with a letter*");
        }

        // assert
        text.ToString().Should().BeEmpty();
    }

    [Fact]
    public void WriteCalendar_CharacterXmlCannotHold_Throws()
    {
        // arrange
        var calendar = ReadCalendar(EventCalendar());
        calendar.Events.Single().Properties.Add(new IcalContentLine("X-BAD", "a\u0001b"));

        // act
        using var writer = new XcalWriter(new StringWriter());

        // assert
        writer.Invoking(value => value.WriteCalendar(calendar)).Should().Throw<IcalFileException>().WithMessage("*XML cannot hold*");
    }

    [Fact]
    public void WriteCalendar_XmlWriterInLargerDocument_WritesElementInPlace()
    {
        // arrange
        var text = new StringWriter();
        using var xmlWriter = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true });
        xmlWriter.WriteStartElement("envelope");

        // act
        using (var writer = new XcalWriter(xmlWriter))
        {
            writer.WriteCalendar(ReadCalendar(Example1Ical));
        }

        xmlWriter.WriteEndElement();
        xmlWriter.Flush();

        // assert
        var document = XDocument.Parse(text.ToString());
        document.Root!.Name.LocalName.Should().Be("envelope");
        document.Root.Element(Ns + "icalendar")!.Elements(Ns + "vcalendar").Should().ContainSingle();
    }

    [Fact]
    public void Dispose_NothingWritten_WritesNothing()
    {
        // arrange
        var text = new StringWriter();

        // act
        new XcalWriter(text).Dispose();

        // assert
        text.ToString().Should().BeEmpty();
    }

    [Fact]
    public void Dispose_ThenUse_ThrowsObjectDisposed()
    {
        // arrange
        var writer = new XcalWriter(new StringWriter());
        writer.Dispose();
        writer.Dispose();
        var calendar = ReadCalendar(Example1Ical);

        // assert
        writer.Invoking(value => value.WriteCalendar(calendar)).Should().Throw<ObjectDisposedException>();
        writer.Invoking(value => value.WriteCalendars([calendar])).Should().Throw<ObjectDisposedException>();
        writer.Invoking(value => value.Flush()).Should().Throw<ObjectDisposedException>();
        writer.Invoking(value => value.CalendarsWritten).Should().Throw<ObjectDisposedException>();
        writer.Invoking(value => value.ComponentsWritten).Should().Throw<ObjectDisposedException>();
        writer.Invoking(value => value.PropertiesWritten).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void WriteCalendars_CallsWriterDuringEnumeration_ThrowsInvalidOperation()
    {
        // arrange
        var calendar = ReadCalendar(Example1Ical);
        using var writer = new XcalWriter(new StringWriter());

        IEnumerable<IcalCalendar> Calendars()
        {
            yield return calendar;
            writer.WriteCalendar(calendar);
        }

        // act
        var act = () => writer.WriteCalendars(Calendars());

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Another operation*");
    }

    [Fact]
    public void WriteCalendars_ContainsNull_ThrowsArgumentException()
    {
        // arrange
        using var writer = new XcalWriter(new StringWriter());

        // act
        var act = () => writer.WriteCalendars([ReadCalendar(Example1Ical), null!]);

        // assert
        act.Should().Throw<ArgumentException>();
        writer.CalendarsWritten.Should().Be(1);
    }

    [Fact]
    public void Constructors_InvalidArguments_Throw()
    {
        // assert
        FluentActions.Invoking(() => new XcalWriter((Stream)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalWriter(new MemoryStream(), null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalWriter(new MemoryStream(new byte[1], false))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new XcalWriter(new MemoryStream(), new IcalOptions { LineBreak = "\r" })).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new XcalWriter((string)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalWriter((TextWriter)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalWriter(new StringWriter(), new IcalOptions { Encoding = Encoding.UTF8 }))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new XcalWriter((XmlWriter)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalWriter(XmlWriter.Create(new StringWriter()), new IcalOptions { Encoding = Encoding.UTF8 }))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WriteCalendar_File_WritesAndClosesFile()
    {
        // arrange
        var path = Path.GetTempFileName();
        try
        {
            // act
            using (var writer = new XcalWriter(path))
            {
                writer.WriteCalendar(ReadCalendar(Example1Ical));
            }

            // assert
            Model(ReadXcal(System.IO.File.ReadAllText(path))).Should().Equal(Model(ReadCalendars(Example1Ical)));
            System.IO.File.Delete(path);
            System.IO.File.Exists(path).Should().BeFalse();
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [Fact]
    public void Dispose_FileNotWritten_ClosesFile()
    {
        // arrange
        var path = Path.GetTempFileName();
        try
        {
            var writer = new XcalWriter(path);

            // act
            writer.Dispose();

            // assert
            System.IO.File.Delete(path);
            System.IO.File.Exists(path).Should().BeFalse();
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    /// <summary>
    /// Gets the inner XML of a property element, without namespace declarations or formatting.
    /// </summary>
    private static string Inner(XElement properties, string name)
    {
        var element = properties.Element(Ns + name)!;
        return string.Concat(element.Elements().Select(child => Normalize(child).Replace(" xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"", "")));
    }

    /// <summary>
    /// Gets an element as XML without insignificant white space, so that documents formatted differently compare equal.
    /// </summary>
    private static string Normalize(XNode node)
    {
        var copy = node is XDocument document ? new XElement(document.Root!) : new XElement((XElement)node);
        foreach (var text in copy.DescendantNodes().OfType<XText>().Where(text => text.Parent!.HasElements).ToList())
        {
            text.Remove();
        }

        return copy.ToString(SaveOptions.DisableFormatting);
    }
}
