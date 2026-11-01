using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class XcalReaderTests
    {
        // ref. RFC 6321 §3.1: the namespace of xCal, and the root element that holds the calendars.
        private const string IcalendarNamespace = "urn:ietf:params:xml:ns:icalendar-2.0";
        private const string IcalendarStart = "<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\">";
        private const string IcalendarEnd = "</icalendar>";
        private const string VcalendarStart = "<vcalendar>";

        private const string CafeSummary = "<summary><text>Caf\xE9</text></summary>";

        // A journal document with CafeSummary that declares UTF-8, and one with a document type definition that declares an entity.
        private static readonly string CafeDocumentDeclaringUtf8 = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + JournalDocument(CafeSummary);
        private static readonly string DocumentWithEntity = "<!DOCTYPE icalendar [<!ENTITY e \"x\">]>" + JournalDocument("<summary><text>&e;</text></summary>");
        private const string Cafe = "Caf\xE9";
        private const string ValueParameter = "VALUE";
        private const string ExampleNotValidDateTime = "2026-02-30T00:00:00";

        // The calendar element of RfcExample1Xcal, twice in one document.
        private static readonly string TwoCalendars = IcalendarStart + Example1Calendar() + Example1Calendar() + IcalendarEnd;

        // The document of RfcExample1Xcal, in a larger document, with an element before and after it.
        private static readonly string ExampleInEnvelope =
            "<envelope><other/>" + Example1Xcal.Substring(Example1Xcal.IndexOf("<icalendar", StringComparison.Ordinal)) + "<after/></envelope>";

        // Each RFC example, with the content lines of its iCalendar form.
        public static TheoryData<string, string[]> RfcExamples => new TheoryData<string, string[]>()
        {
            { Example1Xcal, FixtureExpectations.XcalRoundTripModels[nameof(Example1Ical)] },
            { Example2Xcal, FixtureExpectations.XcalRoundTripModels[nameof(Example2Ical)] }
        };

        // A document with a summary of "Caf\xE9", encoded in each encoding that its XML declaration names.
        public static TheoryData<string, byte[]> DeclaredEncodings()
        {
            var data = new TheoryData<string, byte[]>();
            foreach (var encodingName in new[] { "utf-16", "iso-8859-1" })
            {
                var encoding = Encoding.GetEncoding(encodingName);
                var xml = "<?xml version=\"1.0\" encoding=\"" + encodingName + "\"?>" + JournalDocument(CafeSummary);
                data.Add(encodingName, encoding.GetPreamble().Concat(encoding.GetBytes(xml)).ToArray());
            }

            return data;
        }

        /// <summary>
        /// Documents that are not valid xCal, with the line and position the error is reported at.  A caret marks that place in each document: the
        /// name of an element, or the start of text.
        /// </summary>
        public static TheoryData<string, string, long, long> StructureErrors()
        {
            var cases = new[]
            {
                ("Wrong root", "<^vcalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"/>"),
                ("Root in no namespace", "<^icalendar/>"),
                ("Unexpected element", IcalendarStart + "\n  <^vevent/></icalendar>"),
                ("Text in container", IcalendarStart + "<vcalendar>^text</vcalendar></icalendar>"),
                ("Two properties elements", Document("<properties/><^properties/>")),
                ("No value", JournalDocument("<^summary/>")),
                ("Not a value element", JournalDocument("<summary><^words>Hi</words></summary>")),
                ("Mixed value types", JournalDocument("<rdate><date>2026-01-01</date><^date-time>2026-01-02T00:00:00</date-time></rdate>")),
                ("Too many values", JournalDocument("<summary><text>a</text><^text>b</text></summary>")),
                ("Element in value", JournalDocument("<summary><text><^b>Hi</b></text></summary>")),
                ("Text in property", JournalDocument("<^summary>Hi</summary>")),
                ("Period without end", JournalDocument("<rdate><^period><start>2026-01-01T00:00:00Z</start></period></rdate>")),
                ("Empty recur", JournalDocument("<rrule><^recur/></rrule>")),
                ("Geo without longitude", JournalDocument("<geo><^latitude>1.5</latitude></geo>")),
                ("Parameter without value", JournalDocument("<summary><parameters><^language/></parameters><text>Hi</text></summary>")),
                ("Name not valid in iCalendar", JournalDocument("<^x_prop><unknown>a</unknown></x_prop>")),
                ("Not well-formed", IcalendarStart + "\n<vcalendar></^icalendar>"),
            };
            var data = new TheoryData<string, string, long, long>();
            foreach (var (name, markedXml) in cases)
            {
                var marker = markedXml.IndexOf('^');
                var lineStart = markedXml.LastIndexOf('\n', marker) + 1;
                var lineNumber = markedXml.Take(marker).Count(character => character == '\n') + 1;
                data.Add(name, markedXml.Remove(marker, 1), lineNumber, marker - lineStart + 1);
            }

            return data;
        }

        public static TheoryData<Action, Type> InvalidConstructorArguments => new TheoryData<Action, Type>
        {
            { () => new XcalReader((Stream)null), typeof(ArgumentNullException) },
            { () => new XcalReader(new MemoryStream(), null), typeof(ArgumentNullException) },
            { () => new XcalReader(new WriteOnlyStream()), typeof(ArgumentException) },
            { () => new XcalReader((string)null), typeof(ArgumentNullException) },
            { () => new XcalReader((TextReader)null), typeof(ArgumentNullException) },
            { () => new XcalReader(new StringReader(""), new IcalOptions { Encoding = Encoding.UTF8 }), typeof(ArgumentException) },
            { () => new XcalReader((XmlReader)null), typeof(ArgumentNullException) },
            { () => new XcalReader(XmlReader.Create(new StringReader("")), new IcalOptions { Encoding = Encoding.UTF8 }), typeof(ArgumentException) },
        };

        public static TheoryData<Func<XcalReader, object>> DisposedReaderMembers => new TheoryData<Func<XcalReader, object>>
        {
            reader => reader.ReadCalendar(),
            reader => reader.ReadCalendars(),
            reader => reader.CalendarsRead,
            reader => reader.ComponentsRead,
            reader => reader.PropertiesRead,
        };

        [Theory]
        [MemberData(nameof(RfcExamples), DisableDiscoveryEnumeration = true)]
        public void ReadCalendars_RfcExample_MatchesICalendarForm(string xcal, string[] expected)
        {
            // act
            var testResult = Model(ReadXcal(xcal));

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void ReadCalendars_RfcExample2_BuildsTypedModel()
        {
            // act
            var testResult = ReadXcal(Example2Xcal).Single();

            // assert
            testResult.ProductId.Should().Be("-//Example Inc.//Example Client//EN");
            var timeZone = testResult.TimeZones.Should().ContainSingle().Subject;
            timeZone.TimeZoneId.Should().Be("US/Eastern");
            timeZone.Observances.Should().HaveCount(2);
            var master = testResult.Events.First();
            master.DtStart.Should().Be(new IcalDateOrDateTime(new IcalDateTime(new IcalDate(2006, 1, 2), new IcalTime(12, 0, 0), "US/Eastern")));
            master.RecurrenceRules.Should().ContainSingle().Which.Count.Should().Be(5);
            master.Description.Should().Be(
                "We are having a meeting all this week at 12 pm for one hour, with an additional meeting on the first day 2 hours long.\n" +
                "Please bring your own lunch for the 12 pm meetings.");
            master.Properties.Should().ContainSingle(property => property.Name == "RDATE").Which.Parameters.Select(parameter => parameter.Name)
                .Should().Equal("TZID", ValueParameter);
        }

        [Fact]
        public void ReadCalendars_Counters_CountWhatWasRead()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(Example2Xcal)))
            {
                // act
                reader.ReadCalendars().ToList();

                // assert
                reader.CalendarsRead.Should().Be(1);
                reader.ComponentsRead.Should().Be(5);
                reader.PropertiesRead.Should().Be(28);
            }
        }

        [Theory]
        [InlineData("<x-property><binary>SGVs\n bG8=</binary></x-property>", "X-PROPERTY;VALUE=BINARY;ENCODING=BASE64:SGVsbG8=")]
        [InlineData("<x-property><boolean>true</boolean></x-property>", "X-PROPERTY;VALUE=BOOLEAN:TRUE")]
        [InlineData("<x-property><boolean>0</boolean></x-property>", "X-PROPERTY;VALUE=BOOLEAN:FALSE")]
        [InlineData("<x-property><cal-address>mailto:a@example.com</cal-address></x-property>", "X-PROPERTY;VALUE=CAL-ADDRESS:mailto:a@example.com")]
        [InlineData("<x-property><date>2011-05-17</date></x-property>", "X-PROPERTY;VALUE=DATE:20110517")]
        [InlineData("<x-property><date-time>2011-05-17T12:00:00Z</date-time></x-property>", "X-PROPERTY;VALUE=DATE-TIME:20110517T120000Z")]
        [InlineData("<x-property><duration>-P1DT2H</duration></x-property>", "X-PROPERTY;VALUE=DURATION:-P1DT2H")]
        [InlineData("<x-property><float>1.50</float></x-property>", "X-PROPERTY;VALUE=FLOAT:1.50")]
        [InlineData("<x-property><float> 1.5E2 </float></x-property>", "X-PROPERTY;VALUE=FLOAT:150")]
        [InlineData("<x-property><integer>-100</integer></x-property>", "X-PROPERTY;VALUE=INTEGER:-100")]
        [InlineData("<x-property><period><start>2011-05-17T12:00:00Z</start><end>2011-05-17T13:00:00Z</end></period></x-property>",
            "X-PROPERTY;VALUE=PERIOD:20110517T120000Z/20110517T130000Z")]
        [InlineData("<x-property><recur><freq>WEEKLY</freq><until>2011-05-17</until><byday>MO</byday><byday>-1FR</byday><wkst>SU</wkst></recur></x-property>",
            "X-PROPERTY;VALUE=RECUR:FREQ=WEEKLY;UNTIL=20110517;BYDAY=MO,-1FR;WKST=SU")]
        [InlineData("<x-property><text>a,b;c\\d\ne</text></x-property>", "X-PROPERTY;VALUE=TEXT:a\\,b\\;c\\\\d\\ne")]
        [InlineData("<x-property><time>12:00:00Z</time></x-property>", "X-PROPERTY;VALUE=TIME:120000Z")]
        [InlineData("<x-property><uri>http://example.com/</uri></x-property>", "X-PROPERTY;VALUE=URI:http://example.com/")]
        [InlineData("<x-property><utc-offset>+01:30:45</utc-offset></x-property>", "X-PROPERTY;VALUE=UTC-OFFSET:+013045")]
        [InlineData("<x-property><unknown>20110512T120000Z</unknown></x-property>", "X-PROPERTY:20110512T120000Z")]
        public void ReadCalendar_ValueElement_ConvertsToTextForm(string property, string expected)
        {
            // arrange
            var xml = JournalDocument(property);

            // act
            var testResult = FormatLine(JournalProperty(ReadXcal(xml).Single()));

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ReadCalendar_BinaryWithoutEncoding_AddsEncodingParameter()
        {
            // arrange: RFC 6321 §3.6.1 has no ENCODING parameter on a binary value, which RFC 5545 requires.
            var xml = JournalDocument("<attach><parameters><fmttype><text>image/png</text></fmttype></parameters><binary>SGVsbG8=</binary></attach>");

            // act
            var testResult = FormatLine(JournalProperty(ReadXcal(xml).Single()));

            // assert
            testResult.Should().Be("ATTACH;FMTTYPE=image/png;VALUE=BINARY;ENCODING=BASE64:SGVsbG8=");
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
            testResult.Parameters.Should().ContainSingle().Which.Name.Should().Be("X-PARAM");
            testResult.Parameters.Should().ContainSingle().Which.Values.Should().Equal("PT30M");
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
            testResult.Parameters.Select(parameter => parameter.Name).Should().Equal("RSVP", "MEMBER", "CN");
            testResult.Parameters.Select(parameter => parameter.Values).Should().BeEquivalentTo(new[]
            {
                new[] { "TRUE" }, new[] { "mailto:a@example.com", "mailto:b@example.com" }, new[] { "Jane \"JD\" Doe" }
            }, options => options.WithStrictOrdering());
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
            var testResult = ReadXcal(xml).Single().Journals.Single();

            // assert
            testResult.Properties.Should().ContainSingle(property => property.Name == "CATEGORIES").Which.Value.Should().Be("A\\,B,C");
            testResult.Categories.Should().Equal("A,B", "C");
            var exceptionDates = testResult.Properties.Should().ContainSingle(property => property.Name == "EXDATE").Subject;
            exceptionDates.Value.Should().Be("20260102,20260103");
            exceptionDates.Parameters.Should().ContainSingle(parameter => parameter.Name == ValueParameter).Which.Values.Should().Equal("DATE");
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
            var testResult = ReadXcal(xml).Single().Journals.Single();

            // assert
            testResult.Properties.Should().ContainSingle(property => property.Name == "GEO").Which.Value.Should().Be("37.386013;-122.082932");
            testResult.Properties.Where(property => property.Name == "REQUEST-STATUS").Select(property => property.Value)
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
            var testResult = ReadXcal(xml).Single();

            // assert
            testResult.Properties.Should().ContainSingle(property => property.Name == "SOURCE").Which.Parameters.Should()
                .ContainSingle(parameter => parameter.Name == ValueParameter).Which.Values.Should().Equal("URI");
            testResult.Source.Should().Be(new Uri("http://example.com/calendar.ics"));
        }

        [Fact]
        public void ReadCalendar_ExtensionsInOtherNamespaces_AreSkipped()
        {
            // arrange
            var xml = JournalDocument(
                "<ext:thing xmlns:ext=\"http://example.com/ext\"><ext:inner>text</ext:inner></ext:thing>" +
                "<summary><ext:note xmlns:ext=\"http://example.com/ext\">ignored</ext:note><text>Hello</text></summary>");

            // act
            var testResult = ReadXcal(xml).Single().Journals.Single();

            // assert
            testResult.Properties.Select(property => property.Name).Should().Equal("UID", "DTSTAMP", "SUMMARY");
            testResult.Summary.Should().Be("Hello");
        }

        [Fact]
        public void ReadCalendar_CommentsCdataAndWhitespace_AreHandled()
        {
            // arrange
            var xml = JournalDocument("<!-- a comment --><summary>\n  <text><![CDATA[a < b]]> &amp; c</text>\n</summary>");

            // act
            var testResult = ReadXcal(xml).Single().Journals.Single();

            // assert
            testResult.Summary.Should().Be("a < b & c");
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
        public void ReadCalendar_FirstOfSeveralCalendars_ReadsIt()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(TwoCalendars)))
            {
                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Should().NotBeNull();
            }
        }

        [Fact]
        public void ReadCalendar_SecondOfSeveralCalendars_ReadsIt()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(TwoCalendars)))
            {
                reader.ReadCalendar();

                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Should().NotBeNull();
            }
        }

        [Fact]
        public void ReadCalendar_AfterTheLastCalendar_ReturnsNull()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(TwoCalendars)))
            {
                reader.ReadCalendar();
                reader.ReadCalendar();

                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadCalendar_AgainAfterTheLastCalendar_ReturnsNull()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(TwoCalendars)))
            {
                reader.ReadCalendar();
                reader.ReadCalendar();
                reader.ReadCalendar();

                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadCalendars_XmlReaderInLargerDocument_StopsAtEndOfRoot()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader(ExampleInEnvelope)))
            {
                xmlReader.ReadToFollowing("icalendar", IcalendarNamespace);
                using (var reader = new XcalReader(xmlReader))
                {
                    // act
                    var testResult = reader.ReadCalendars().ToList();

                    // assert
                    testResult.Should().ContainSingle();
                    xmlReader.NodeType.Should().Be(XmlNodeType.EndElement);
                    xmlReader.LocalName.Should().Be("icalendar");
                }
            }
        }

        [Fact]
        public void Read_XmlReaderAfterCalendarsInLargerDocument_ReadsTheNextElement()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader(ExampleInEnvelope)))
            {
                xmlReader.ReadToFollowing("icalendar", IcalendarNamespace);
                using (var reader = new XcalReader(xmlReader))
                {
                    reader.ReadCalendars().ToList();

                    // act
                    var testResult = xmlReader.Read();

                    // assert
                    testResult.Should().BeTrue();
                    xmlReader.LocalName.Should().Be("after");
                }
            }
        }

        [Theory]
        [MemberData(nameof(DeclaredEncodings))]
        public void ReadCalendar_StreamInDeclaredEncoding_IsDecoded(string encodingName, byte[] data)
        {
            // arrange
            _ = encodingName;
            using (var reader = new XcalReader(new MemoryStream(data)))
            {
                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Journals.Should().ContainSingle().Which.Summary.Should().Be(Cafe);
            }
        }

        [Fact]
        public void ReadCalendar_EncodingOption_OverridesDeclaration()
        {
            // arrange
            using (var reader = new XcalReader(new MemoryStream(IcalTestData.Latin1.GetBytes(CafeDocumentDeclaringUtf8)),
                new IcalOptions { Encoding = IcalTestData.Latin1 }))
            {
                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Journals.Should().ContainSingle().Which.Summary.Should().Be(Cafe);
            }
        }

        [Theory]
        [MemberData(nameof(StructureErrors))]
        public void ReadCalendar_NotValidXcal_ThrowsWithLocation(string caseName, string xml, long lineNumber, long linePosition)
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(xml)))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>(caseName).Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(lineNumber, caseName);
                exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be(linePosition, caseName);
                exception.Data.Contains(IcalFileException.DataItemName.PhysicalLineNumber).Should().BeFalse(caseName);
            }
        }

        [Theory]
        [MemberData(nameof(StructureErrors))]
        public void ReadCalendar_AfterNotValidXcal_ThrowsInvalidOperation(string caseName, string xml, long lineNumber, long linePosition)
        {
            // arrange
            _ = (lineNumber, linePosition);
            using (var reader = new XcalReader(new StringReader(xml)))
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<InvalidOperationException>(caseName);
            }
        }

        [Fact]
        public void ReadCalendar_NotWellFormed_WrapsXmlException()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(IcalendarStart + VcalendarStart)))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<IcalFileException>().WithInnerException<XmlException>();
            }
        }

        [Fact]
        public void ReadCalendar_DocumentTypeDefinition_IsRejected()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader(DocumentWithEntity)))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<IcalFileException>().WithInnerException<XmlException>();
            }
        }

        [Fact]
        public void ReadCalendar_NoCalendar_Throws()
        {
            // arrange
            using (var reader = new XcalReader(new StringReader("<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"/>")))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<IcalFileException>().WithMessage("*no VCALENDAR*");
            }
        }

        [Fact]
        public void ReadCalendar_UnsupportedVersion_ThrowsWithLocation()
        {
            // arrange
            var xml = Document("<properties>\n<version><text>1.0</text></version></properties>");
            using (var reader = new XcalReader(new StringReader(xml)))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.UnsupportedVersion);
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be(2L);
                exception.Data.Contains(IcalFileException.DataItemName.PhysicalLineNumber).Should().BeFalse();
            }
        }

        [Fact]
        public void ReadCalendar_NonConformantCalendar_Throws()
        {
            // arrange
            using (var reader = CreateReaderOfNonConformantThenExample1())
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.MissingRequiredProperty);
                exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VJOURNAL[1]");
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(1L);
            }
        }

        [Fact]
        public void ReadCalendar_AfterNonConformantCalendar_ReadsTheNextCalendar()
        {
            // arrange
            using (var reader = CreateReaderOfNonConformantThenExample1())
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Events.Should().ContainSingle();
            }
        }

        [Fact]
        public void ReadCalendar_InvalidValueByDefault_Throws()
        {
            // arrange
            var xml = JournalDocument("\n<dtstart><date-time>2026-02-30T00:00:00</date-time></dtstart>");

            // act
            Action testAction = () => ReadXcal(xml);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DTSTART");
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
        }

        [Fact]
        public void ReadCalendar_InvalidValueAllowed_KeepsTheValue()
        {
            // arrange
            var xml = JournalDocument("\n<dtstart><date-time>2026-02-30T00:00:00</date-time></dtstart>");

            // act
            var testResult = ReadXcal(xml, new IcalOptions { AllowInvalidValues = true });

            // assert
            JournalProperty(testResult.Single()).Value.Should().Be(ExampleNotValidDateTime);
        }

        [Fact]
        public void ReadCalendar_TextIcalendarCannotHold_Throws()
        {
            // arrange
            var xml = JournalDocument("<summary><text>a\x007Fb</text></summary>");

            // act
            Action testAction = () => ReadXcal(xml);

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*cannot hold*");
        }

        [Theory]
        [MemberData(nameof(InvalidConstructorArguments), DisableDiscoveryEnumeration = true)]
        public void Constructor_InvalidArgument_Throws(Action call, Type expectedType)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentException>().Which.Should().BeOfType(expectedType);
        }

        [Theory]
        [MemberData(nameof(DisposedReaderMembers), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposed(Func<XcalReader, object> member)
        {
            // arrange
            var reader = new XcalReader(new StringReader(Example1Xcal));
            reader.Dispose();

            // act
            Action testAction = () => member(reader);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_Twice_DoesNotThrow()
        {
            // arrange
            var reader = new XcalReader(new StringReader(Example1Xcal));
            reader.Dispose();

            // act
            Action testAction = () => reader.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void Dispose_CallerStream_IsLeftOpen()
        {
            // arrange
            var stream = new MemoryStream(Utf8(Example1Xcal));
            var reader = new XcalReader(stream);

            // act
            reader.Dispose();

            // assert
            stream.CanRead.Should().BeTrue();
        }

        [Fact]
        public void Dispose_CallerXmlReader_IsLeftOpen()
        {
            // arrange
            var xmlReader = XmlReader.Create(new StringReader(Example1Xcal));
            var reader = new XcalReader(xmlReader);

            // act
            reader.Dispose();

            // assert
            xmlReader.ReadState.Should().NotBe(ReadState.Closed);
        }

        /// <summary>
        /// Gets the calendar element of RfcExample1Xcal.
        /// </summary>
        private static string Example1Calendar()
        {
            var calendar = Example1Xcal.Substring(Example1Xcal.IndexOf(VcalendarStart, StringComparison.Ordinal));
            return calendar.Substring(0, calendar.IndexOf(IcalendarEnd, StringComparison.Ordinal));
        }

        /// <summary>
        /// Builds a reader of a document of a calendar whose VJOURNAL has no UID, then the first example calendar.
        /// </summary>
        private static XcalReader CreateReaderOfNonConformantThenExample1()
        {
            var bad = "<vcalendar><properties><prodid><text>-//Test//Test//EN</text></prodid><version><text>2.0</text></version></properties>" +
                      "<components><vjournal><properties>\n<dtstamp><date-time>2026-01-01T00:00:00Z</date-time></dtstamp></properties></vjournal>" +
                      "</components></vcalendar>";
            var good = Example1Xcal.Substring(Example1Xcal.IndexOf(VcalendarStart, StringComparison.Ordinal));
            return new XcalReader(new StringReader(IcalendarStart + bad + good));
        }

        private static string FormatLine(IcalContentLine line)
        {
            return line.Name + string.Concat(line.Parameters.Select(parameter => ";" + parameter.Name + "=" + string.Join(",", parameter.Values))) + ":" +
                line.Value;
        }

        private sealed class WriteOnlyStream : MemoryStream
        {
            public override bool CanRead => false;
        }
    }
}
