using System.Text;
using System.Xml;
using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests;

public class XcalReaderTests
{
    public static TheoryData<string, string> RfcExamples => new()
    {
        { Example1Xcal, Example1Ical },
        { Example2Xcal, Example2Ical }
    };

    [Theory]
    [MemberData(nameof(RfcExamples))]
    public void ReadCalendars_RfcExample_MatchesICalendarForm(string xcal, string ical)
    {
        // act
        var testResult = ReadXcal(xcal);

        // assert
        Model(testResult).Should().Equal(Model(ReadCalendars(ical)));
    }

    [Fact]
    public void ReadCalendars_RfcExample2_BuildsTypedModel()
    {
        // act
        var calendar = ReadXcal(Example2Xcal).Single();

        // assert
        calendar.ProductId.Should().Be("-//Example Inc.//Example Client//EN");
        var timeZone = calendar.TimeZones.Single();
        timeZone.TimeZoneId.Should().Be("US/Eastern");
        timeZone.Observances.Should().HaveCount(2);
        var master = calendar.Events.First();
        master.DtStart.Should().Be(new IcalDateOrDateTime(new IcalDateTime(new IcalDate(2006, 1, 2), new IcalTime(12, 0, 0), "US/Eastern")));
        master.RecurrenceRules.Single().Count.Should().Be(5);
        master.Description.Should().Be(
            "We are having a meeting all this week at 12 pm for one hour, with an additional meeting on the first day 2 hours long.\n" +
            "Please bring your own lunch for the 12 pm meetings.");
        master.GetProperty("RDATE")!.Parameters.Select(parameter => parameter.Name).Should().Equal("TZID", "VALUE");
    }

    [Fact]
    public void ReadCalendars_Counters_CountWhatWasRead()
    {
        // arrange
        using var reader = new XcalReader(new StringReader(Example2Xcal));

        // act
        reader.ReadCalendars().ToList();

        // assert
        reader.CalendarsRead.Should().Be(1);
        reader.ComponentsRead.Should().Be(5);
        reader.PropertiesRead.Should().Be(28);
    }

    [Theory]
    [InlineData("<binary>SGVs\n bG8=</binary>", "X;VALUE=BINARY;ENCODING=BASE64:SGVsbG8=")]
    [InlineData("<boolean>true</boolean>", "X;VALUE=BOOLEAN:TRUE")]
    [InlineData("<boolean>0</boolean>", "X;VALUE=BOOLEAN:FALSE")]
    [InlineData("<cal-address>mailto:a@example.com</cal-address>", "X;VALUE=CAL-ADDRESS:mailto:a@example.com")]
    [InlineData("<date>2011-05-17</date>", "X;VALUE=DATE:20110517")]
    [InlineData("<date-time>2011-05-17T12:00:00Z</date-time>", "X;VALUE=DATE-TIME:20110517T120000Z")]
    [InlineData("<duration>-P1DT2H</duration>", "X;VALUE=DURATION:-P1DT2H")]
    [InlineData("<float>1.50</float>", "X;VALUE=FLOAT:1.50")]
    [InlineData("<float> 1.5E2 </float>", "X;VALUE=FLOAT:150")]
    [InlineData("<integer>-100</integer>", "X;VALUE=INTEGER:-100")]
    [InlineData("<period><start>2011-05-17T12:00:00Z</start><end>2011-05-17T13:00:00Z</end></period>",
        "X;VALUE=PERIOD:20110517T120000Z/20110517T130000Z")]
    [InlineData("<recur><freq>WEEKLY</freq><until>2011-05-17</until><byday>MO</byday><byday>-1FR</byday><wkst>SU</wkst></recur>",
        "X;VALUE=RECUR:FREQ=WEEKLY;UNTIL=20110517;BYDAY=MO,-1FR;WKST=SU")]
    [InlineData("<text>a,b;c\\d\ne</text>", "X;VALUE=TEXT:a\\,b\\;c\\\\d\\ne")]
    [InlineData("<time>12:00:00Z</time>", "X;VALUE=TIME:120000Z")]
    [InlineData("<uri>http://example.com/</uri>", "X;VALUE=URI:http://example.com/")]
    [InlineData("<utc-offset>+01:30:45</utc-offset>", "X;VALUE=UTC-OFFSET:+013045")]
    [InlineData("<unknown>20110512T120000Z</unknown>", "X:20110512T120000Z")]
    public void ReadCalendar_ValueElement_ConvertsToTextForm(string value, string expected)
    {
        // arrange
        var xml = JournalDocument("<x-property>" + value + "</x-property>");

        // act
        var testResult = JournalProperty(ReadXcal(xml).Single());

        // assert
        $"X{FormatParameters(testResult)}:{testResult.Value}".Should().Be(expected);
        testResult.Name.Should().Be("X-PROPERTY");
    }

    [Fact]
    public void ReadCalendar_BinaryWithoutEncoding_AddsEncodingParameter()
    {
        // arrange: RFC 6321 §3.6.1 has no ENCODING parameter on a binary value, which RFC 5545 requires.
        var xml = JournalDocument("<attach><parameters><fmttype><text>image/png</text></fmttype></parameters><binary>SGVsbG8=</binary></attach>");

        // act
        var testResult = JournalProperty(ReadXcal(xml).Single());

        // assert
        $"X{FormatParameters(testResult)}:{testResult.Value}".Should().Be("X;FMTTYPE=image/png;VALUE=BINARY;ENCODING=BASE64:SGVsbG8=");
    }

    [Fact]
    public void ReadCalendar_UnknownParameter_ReadAsText()
    {
        // arrange: RFC 6321 §5, second example.
        var xml = JournalDocument(
            "<dtstart><parameters><x-param><unknown>PT30M</unknown></x-param></parameters><date-time>2011-05-12T13:00:00Z</date-time></dtstart>");

        // act
        var testResult = JournalProperty(ReadXcal(xml).Single());

        // assert
        testResult.Name.Should().Be("DTSTART");
        testResult.Parameters.Single().Name.Should().Be("X-PARAM");
        testResult.Parameters.Single().Values.Should().Equal("PT30M");
        testResult.Value.Should().Be("20110512T130000Z");
    }

    [Fact]
    public void ReadCalendar_Parameters_ConvertNamesAndValues()
    {
        // arrange
        var xml = JournalDocument(
            "<attendee><parameters><rsvp><boolean>true</boolean></rsvp><member><cal-address>mailto:a@example.com</cal-address>" +
            "<cal-address>mailto:b@example.com</cal-address></member><cn><text>Jane \"JD\" Doe</text></cn></parameters>" +
            "<cal-address>mailto:jane@example.com</cal-address></attendee>");

        // act
        var testResult = JournalProperty(ReadXcal(xml).Single());

        // assert
        testResult.Parameters.Select(parameter => (parameter.Name, string.Join("|", parameter.Values)))
            .Should().Equal(("RSVP", "TRUE"), ("MEMBER", "mailto:a@example.com|mailto:b@example.com"), ("CN", "Jane \"JD\" Doe"));
        testResult.Value.Should().Be("mailto:jane@example.com");
    }

    [Fact]
    public void ReadCalendar_MultiValuedProperties_JoinValues()
    {
        // arrange
        var xml = JournalDocument(
            "<categories><text>A,B</text><text>C</text></categories>" +
            "<exdate><date>2026-01-02</date><date>2026-01-03</date></exdate>");

        // act
        var journal = ReadXcal(xml).Single().Journals.Single();

        // assert
        journal.GetProperty("CATEGORIES")!.Value.Should().Be("A\\,B,C");
        journal.Categories.Should().Equal("A,B", "C");
        var exceptionDates = journal.GetProperty("EXDATE")!;
        exceptionDates.Value.Should().Be("20260102,20260103");
        exceptionDates.GetParameter("VALUE")!.Values.Should().Equal("DATE");
    }

    [Fact]
    public void ReadCalendar_GeoAndRequestStatus_ReadFromParts()
    {
        // arrange
        var xml = JournalDocument(
            "<geo><latitude>37.386013</latitude><longitude>-122.082932</longitude></geo>" +
            "<request-status><code>3.1</code><description>Invalid property value</description><data>DTSTART:96-Apr-01</data></request-status>" +
            "<request-status><code>2.0</code><description>Success</description></request-status>");

        // act
        var journal = ReadXcal(xml).Single().Journals.Single();

        // assert
        journal.GetProperty("GEO")!.Value.Should().Be("37.386013;-122.082932");
        journal.GetProperties("REQUEST-STATUS").Select(property => property.Value)
            .Should().Equal("3.1;Invalid property value;DTSTART:96-Apr-01", "2.0;Success");
    }

    [Fact]
    public void ReadCalendar_Rfc7986PropertyWithDefault_AddsValueParameter()
    {
        // arrange: RFC 7986 gives SOURCE no default value type, so VALUE=URI is always needed.
        var xml = Document(
            "<properties><prodid><text>-//Test//Test//EN</text></prodid><version><text>2.0</text></version>" +
            "<source><uri>http://example.com/calendar.ics</uri></source></properties>" +
            "<components><vjournal><properties><uid><text>j@example.com</text></uid>" +
            "<dtstamp><date-time>2026-01-01T00:00:00Z</date-time></dtstamp></properties></vjournal></components>");

        // act
        var calendar = ReadXcal(xml).Single();

        // assert
        calendar.GetProperty("SOURCE")!.GetParameter("VALUE")!.Values.Should().Equal("URI");
        calendar.Source.Should().Be(new Uri("http://example.com/calendar.ics"));
    }

    [Fact]
    public void ReadCalendar_ExtensionsInOtherNamespaces_AreSkipped()
    {
        // arrange
        var xml = JournalDocument(
            "<ext:thing xmlns:ext=\"http://example.com/ext\"><ext:inner>text</ext:inner></ext:thing>" +
            "<summary><ext:note xmlns:ext=\"http://example.com/ext\">ignored</ext:note><text>Hello</text></summary>");

        // act
        var journal = ReadXcal(xml).Single().Journals.Single();

        // assert
        journal.Properties.Select(property => property.Name).Should().Equal("UID", "DTSTAMP", "SUMMARY");
        journal.Summary.Should().Be("Hello");
    }

    [Fact]
    public void ReadCalendar_CommentsCdataAndWhitespace_AreHandled()
    {
        // arrange
        var xml = JournalDocument("<!-- a comment --><summary>\n  <text><![CDATA[a < b]]> &amp; c</text>\n</summary>");

        // act
        var journal = ReadXcal(xml).Single().Journals.Single();

        // assert
        journal.Summary.Should().Be("a < b & c");
    }

    [Fact]
    public void ReadCalendar_ExistingValueParameter_IsNotRepeated()
    {
        // arrange: XcalWriter keeps the VALUE parameter for a value it could not convert.
        var xml = JournalDocument("<dtstart><parameters><value><text>DATE</text></value></parameters><date>2026-01-01</date></dtstart>");

        // act
        var testResult = JournalProperty(ReadXcal(xml).Single());

        // assert
        testResult.Parameters.Should().ContainSingle().Which.Values.Should().Equal("DATE");
    }

    [Fact]
    public void ReadCalendars_SeveralCalendars_ReadsEach()
    {
        // arrange
        var calendar = Example1Xcal.Substring(Example1Xcal.IndexOf("<vcalendar>", StringComparison.Ordinal));
        calendar = calendar.Substring(0, calendar.IndexOf("</icalendar>", StringComparison.Ordinal));
        var xml = "<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\">" + calendar + calendar + "</icalendar>";
        using var reader = new XcalReader(new StringReader(xml));

        // act
        var first = reader.ReadCalendar();
        var second = reader.ReadCalendar();
        var end = reader.ReadCalendar();
        var afterEnd = reader.ReadCalendar();

        // assert
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        end.Should().BeNull();
        afterEnd.Should().BeNull();
    }

    [Fact]
    public void ReadCalendar_XmlReaderInLargerDocument_StopsAtEndOfRoot()
    {
        // arrange
        var xml = "<envelope><other/>" + Example1Xcal.Substring(Example1Xcal.IndexOf("<icalendar", StringComparison.Ordinal)) + "<after/></envelope>";
        using var xmlReader = XmlReader.Create(new StringReader(xml));
        xmlReader.ReadToFollowing("icalendar", "urn:ietf:params:xml:ns:icalendar-2.0");
        using var reader = new XcalReader(xmlReader);

        // act
        var calendars = reader.ReadCalendars().ToList();

        // assert
        calendars.Should().ContainSingle();
        xmlReader.NodeType.Should().Be(XmlNodeType.EndElement);
        xmlReader.LocalName.Should().Be("icalendar");
        xmlReader.Read().Should().BeTrue();
        xmlReader.LocalName.Should().Be("after");
    }

    [Theory]
    [InlineData("utf-16")]
    [InlineData("iso-8859-1")]
    public void ReadCalendar_StreamInDeclaredEncoding_IsDecoded(string encodingName)
    {
        // arrange
        var encoding = Encoding.GetEncoding(encodingName);
        var xml = JournalDocument("<summary><text>Café</text></summary>")
            .Insert(0, $"<?xml version=\"1.0\" encoding=\"{encodingName}\"?>");
        using var reader = new XcalReader(new MemoryStream(encoding.GetPreamble().Concat(encoding.GetBytes(xml)).ToArray()));

        // act
        var testResult = reader.ReadCalendar();

        // assert
        testResult!.Journals.Single().Summary.Should().Be("Café");
    }

    [Fact]
    public void ReadCalendar_EncodingOption_OverridesDeclaration()
    {
        // arrange
        var xml = JournalDocument("<summary><text>Café</text></summary>").Insert(0, "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        using var reader = new XcalReader(new MemoryStream(Encoding.Latin1.GetBytes(xml)), new IcalOptions { Encoding = Encoding.Latin1 });

        // act
        var testResult = reader.ReadCalendar();

        // assert
        testResult!.Journals.Single().Summary.Should().Be("Café");
    }

    [Fact]
    public void ReadCalendar_File_ReadsAndClosesFile()
    {
        // arrange
        var path = Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllText(path, Example1Xcal);

            // act
            List<IcalCalendar> testResult;
            using (var reader = new XcalReader(path))
            {
                testResult = reader.ReadCalendars().ToList();
            }

            // assert
            testResult.Should().ContainSingle();
            System.IO.File.Delete(path);
            System.IO.File.Exists(path).Should().BeFalse();
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    /// <summary>
    /// Documents that are not valid xCal.  A caret marks the place the error is reported at: the name of an element, or the start of text.
    /// </summary>
    public static TheoryData<string, string> StructureErrors => new()
    {
        { "Wrong root", "<^vcalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"/>" },
        { "Root in no namespace", "<^icalendar/>" },
        { "Unexpected element", "ROOT\n  <^vevent/></icalendar>" },
        { "Text in container", "ROOT<vcalendar>^text</vcalendar></icalendar>" },
        { "Two properties elements", Document("<properties/><^properties/>") },
        { "No value", JournalDocument("<^summary/>") },
        { "Not a value element", JournalDocument("<summary><^words>Hi</words></summary>") },
        { "Mixed value types", JournalDocument("<rdate><date>2026-01-01</date><^date-time>2026-01-02T00:00:00</date-time></rdate>") },
        { "Too many values", JournalDocument("<summary><text>a</text><^text>b</text></summary>") },
        { "Element in value", JournalDocument("<summary><text><^b>Hi</b></text></summary>") },
        { "Text in property", JournalDocument("<^summary>Hi</summary>") },
        { "Period without end", JournalDocument("<rdate><^period><start>2026-01-01T00:00:00Z</start></period></rdate>") },
        { "Empty recur", JournalDocument("<rrule><^recur/></rrule>") },
        { "Geo without longitude", JournalDocument("<geo><^latitude>1.5</latitude></geo>") },
        { "Parameter without value", JournalDocument("<summary><parameters><^language/></parameters><text>Hi</text></summary>") },
        { "Name not valid in iCalendar", JournalDocument("<^x_prop><unknown>a</unknown></x_prop>") },
        { "Not well-formed", "ROOT\n<vcalendar></^icalendar>" }
    };

    [Theory]
    [MemberData(nameof(StructureErrors))]
    public void ReadCalendar_NotValidXcal_ThrowsWithLocation(string caseName, string markedXml)
    {
        // arrange
        markedXml = markedXml.Replace("ROOT", "<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\">");
        var marker = markedXml.IndexOf('^');
        var lineStart = markedXml.LastIndexOf('\n', marker) + 1;
        var lineNumber = markedXml.Take(marker).Count(character => character == '\n') + 1;
        var linePosition = marker - lineStart + 1;
        var xml = markedXml.Remove(marker, 1);
        using var reader = new XcalReader(new StringReader(xml));

        // act
        var act = () => reader.ReadCalendar();

        // assert
        var exception = act.Should().Throw<IcalFileException>(caseName).Which;
        exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be((long)lineNumber, caseName);
        exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be((long)linePosition, caseName);
        exception.Data.Contains(IcalFileException.DataItemName.PhysicalLineNumber).Should().BeFalse(caseName);
        reader.Invoking(value => value.ReadCalendar()).Should().Throw<InvalidOperationException>(caseName);
    }

    [Fact]
    public void ReadCalendar_NotWellFormed_WrapsXmlException()
    {
        // arrange
        using var reader = new XcalReader(new StringReader("<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"><vcalendar>"));

        // act
        var act = () => reader.ReadCalendar();

        // assert
        act.Should().Throw<IcalFileException>().WithInnerException<XmlException>();
    }

    [Fact]
    public void ReadCalendar_DocumentTypeDefinition_IsRejected()
    {
        // arrange
        var xml = "<!DOCTYPE icalendar [<!ENTITY e \"x\">]>" + JournalDocument("<summary><text>&e;</text></summary>");
        using var reader = new XcalReader(new StringReader(xml));

        // act
        var act = () => reader.ReadCalendar();

        // assert
        act.Should().Throw<IcalFileException>().WithInnerException<XmlException>();
    }

    [Fact]
    public void ReadCalendar_NoCalendar_Throws()
    {
        // arrange
        using var reader = new XcalReader(new StringReader("<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"/>"));

        // act
        var act = () => reader.ReadCalendar();

        // assert
        act.Should().Throw<IcalFileException>().WithMessage("*no VCALENDAR*");
    }

    [Fact]
    public void ReadCalendar_UnsupportedVersion_ThrowsWithLocation()
    {
        // arrange
        var xml = Document("<properties>\n<version><text>1.0</text></version></properties>");
        using var reader = new XcalReader(new StringReader(xml));

        // act
        var act = () => reader.ReadCalendar();

        // assert
        var exception = act.Should().Throw<IcalFileException>().Which;
        exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.UnsupportedVersion);
        exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
        exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be(2L);
        exception.Data.Contains(IcalFileException.DataItemName.PhysicalLineNumber).Should().BeFalse();
    }

    [Fact]
    public void ReadCalendar_NonConformantCalendar_ThrowsAndContinues()
    {
        // arrange: the first calendar's VJOURNAL has no UID.
        var bad = "<vcalendar><properties><prodid><text>-//Test//Test//EN</text></prodid><version><text>2.0</text></version></properties>" +
                  "<components><vjournal><properties>\n<dtstamp><date-time>2026-01-01T00:00:00Z</date-time></dtstamp></properties></vjournal>" +
                  "</components></vcalendar>";
        var good = Example1Xcal.Substring(Example1Xcal.IndexOf("<vcalendar>", StringComparison.Ordinal));
        using var reader = new XcalReader(new StringReader("<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\">" + bad + good));

        // act
        var act = () => reader.ReadCalendar();

        // assert
        var exception = act.Should().Throw<IcalFileException>().Which;
        exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.MissingRequiredProperty);
        exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VJOURNAL[1]");
        exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(1L);
        reader.ReadCalendar()!.Events.Should().ContainSingle();
    }

    [Fact]
    public void ReadCalendar_InvalidValue_RejectedUnlessAllowed()
    {
        // arrange
        var xml = JournalDocument("\n<dtstart><date-time>2026-02-30T00:00:00</date-time></dtstart>");

        // act
        var act = () => ReadXcal(xml);
        var allowed = ReadXcal(xml, new IcalOptions { AllowInvalidValues = true });

        // assert
        var exception = act.Should().Throw<IcalFileException>().Which;
        exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
        exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DTSTART");
        exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
        JournalProperty(allowed.Single()).Value.Should().Be("2026-02-30T00:00:00");
    }

    [Fact]
    public void ReadCalendar_TextIcalendarCannotHold_Throws()
    {
        // arrange
        var xml = JournalDocument("<summary><text>a\u007Fb</text></summary>");

        // act
        var act = () => ReadXcal(xml);

        // assert
        act.Should().Throw<IcalFileException>().WithMessage("*cannot hold*");
    }

    [Fact]
    public void Constructors_InvalidArguments_Throw()
    {
        // assert
        FluentActions.Invoking(() => new XcalReader((Stream)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalReader(new MemoryStream(), null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalReader(new WriteOnlyStream())).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new XcalReader((string)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalReader((TextReader)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalReader(new StringReader(""), new IcalOptions { Encoding = Encoding.UTF8 }))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => new XcalReader((XmlReader)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new XcalReader(XmlReader.Create(new StringReader("")), new IcalOptions { Encoding = Encoding.UTF8 }))
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Dispose_ThenUse_ThrowsObjectDisposed()
    {
        // arrange
        var reader = new XcalReader(new StringReader(Example1Xcal));
        reader.Dispose();
        reader.Dispose();

        // assert
        reader.Invoking(value => value.ReadCalendar()).Should().Throw<ObjectDisposedException>();
        reader.Invoking(value => value.ReadCalendars()).Should().Throw<ObjectDisposedException>();
        reader.Invoking(value => value.CalendarsRead).Should().Throw<ObjectDisposedException>();
        reader.Invoking(value => value.ComponentsRead).Should().Throw<ObjectDisposedException>();
        reader.Invoking(value => value.PropertiesRead).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void Dispose_CallerStreamAndXmlReader_AreLeftOpen()
    {
        // arrange
        var stream = new MemoryStream(Utf8(Example1Xcal));
        var xmlReader = XmlReader.Create(new StringReader(Example1Xcal));

        // act
        new XcalReader(stream).Dispose();
        new XcalReader(xmlReader).Dispose();

        // assert
        stream.CanRead.Should().BeTrue();
        xmlReader.ReadState.Should().NotBe(ReadState.Closed);
    }

    private static string FormatParameters(IcalContentLine line)
    {
        return string.Concat(line.Parameters.Select(parameter => ";" + parameter.Name + "=" + string.Join(",", parameter.Values)));
    }

    private sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
