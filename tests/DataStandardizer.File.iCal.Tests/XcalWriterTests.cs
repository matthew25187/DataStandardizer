using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class XcalWriterTests
    {
        private const string PropertiesElement = "properties";
        private const string VcalendarElement = "vcalendar";

        // The octets of "<?xml", which an XML declaration starts with.
        private static readonly byte[] XmlDeclarationStart = { 0x3C, 0x3F, 0x78, 0x6D, 0x6C };

        // The numbers of content lines written after each calendar of Example2Ical and Example1Ical, as IcalWriter reports them.
        private const long Example2LineCount = 40;
        private const long Example2AndExample1LineCount = 51;

        private static readonly XNamespace Ns = "urn:ietf:params:xml:ns:icalendar-2.0";

        private static readonly IcalOptions MissingRequiredProperties = new IcalOptions { AllowMissingRequiredProperties = true };

        public static TheoryData<string, string> RfcExamples => new TheoryData<string, string>
        {
            { Example1Ical, Example1Xcal },
            { Example2Ical, Example2Xcal },
        };

        public static TheoryData<Func<XcalWriter, object>> DisposedWriterMembers()
        {
            var calendar = new IcalCalendar();
            return new TheoryData<Func<XcalWriter, object>>
            {
                writer => { writer.WriteCalendar(calendar); return null; },
                writer => { writer.WriteCalendars(new[] { calendar }); return null; },
                writer => { writer.Flush(); return null; },
                writer => writer.CalendarsWritten,
                writer => writer.ComponentsWritten,
                writer => writer.PropertiesWritten,
            };
        }

        public static TheoryData<Action, Type> InvalidConstructorArguments => new TheoryData<Action, Type>
        {
            { () => new XcalWriter((Stream)null), typeof(ArgumentNullException) },
            { () => new XcalWriter(new MemoryStream(), null), typeof(ArgumentNullException) },
            { () => new XcalWriter(new MemoryStream(new byte[1], false)), typeof(ArgumentException) },
            { () => new XcalWriter(new MemoryStream(), new IcalOptions { LineBreak = "\r" }), typeof(ArgumentException) },
            { () => new XcalWriter((string)null), typeof(ArgumentNullException) },
            { () => new XcalWriter((TextWriter)null), typeof(ArgumentNullException) },
            { () => new XcalWriter(new StringWriter(), new IcalOptions { Encoding = Encoding.UTF8 }), typeof(ArgumentException) },
            { () => new XcalWriter((XmlWriter)null), typeof(ArgumentNullException) },
            { () => new XcalWriter(XmlWriter.Create(new StringWriter()), new IcalOptions { Encoding = Encoding.UTF8 }), typeof(ArgumentException) },
        };

        [Theory]
        [MemberData(nameof(RfcExamples))]
        public void WriteCalendars_RfcExample_MatchesXcalForm(string ical, string xcal)
        {
            // arrange
            var calendars = ReadCalendars(ical);
            var expected = Normalize(XDocument.Parse(xcal));

            // act
            var testResult = Normalize(XDocument.Parse(WriteXcal(calendars)));

            // assert: the same XML, ignoring formatting.
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Dispose_AfterWritingToStream_WritesUtf8DocumentWithoutByteOrderMark()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new XcalWriter(stream);
            writer.WriteCalendar(ReadCalendar(Example1Ical));

            // act
            writer.Dispose();

            // assert
            var bytes = stream.ToArray();
            bytes.Take(5).Should().Equal(XmlDeclarationStart);
            var text = Encoding.UTF8.GetString(bytes);
            text.Should().Contain("encoding=\"utf-8\"");
            text.Should().Contain("<icalendar xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\">");
            text.Should().Contain("\r\n  <vcalendar>");
            text.Should().EndWith("</icalendar>");
            stream.CanWrite.Should().BeTrue();
        }

        [Fact]
        public void Dispose_AfterWritingWithLineBreakOption_HasWrittenThoseLineBreaks()
        {
            // arrange
            var text = new StringWriter();
            var writer = new XcalWriter(text, new IcalOptions { LineBreak = "\n" });
            writer.WriteCalendar(ReadCalendar(Example1Ical));

            // act
            writer.Dispose();

            // assert
            text.ToString().Should().NotContain("\r").And.Contain("\n  <vcalendar>");
        }

        [Fact]
        public void WriteCalendar_ValueTypes_WriteTypedElements()
        {
            // arrange
            var options = new IcalOptions { AllowUndefinedTimeZones = true };
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
                IcalLines.BeginAlarm, "ACTION:AUDIO", "TRIGGER;VALUE=DATE-TIME:20260101T080000Z", IcalLines.EndAlarm), options);

            // act
            var testResult = XDocument.Parse(WriteXcal(new[] { calendar }, options));

            // assert
            var properties = testResult.Descendants(Ns + "vevent").Single().Element(Ns + PropertiesElement);
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
            Inner(testResult.Descendants(Ns + "valarm").Single().Element(Ns + PropertiesElement), "trigger")
                .Should().Be("<date-time>2026-01-01T08:00:00Z</date-time>");
        }

        [Fact]
        public void WriteCalendar_InvalidValueAllowed_WritesUnknownWithValueParameter()
        {
            // arrange
            var options = new IcalOptions { AllowInvalidValues = true };
            var calendar = ReadCalendar(EventCalendar("DTEND;VALUE=DATE:2026-01-02"), options);

            // act
            var testResult = XDocument.Parse(WriteXcal(new[] { calendar }, options));

            // assert
            Inner(testResult.Descendants(Ns + PropertiesElement).Last(), "dtend")
                .Should().Be("<parameters><value><text>DATE</text></value></parameters><unknown>2026-01-02</unknown>");
        }

        [Fact]
        public void WriteCalendar_Version_IsAlways20AndFirstIfMissing()
        {
            // arrange
            var calendar = ReadCalendar(ConformanceCases.All[ConformanceCases.MissingVersionCase].Text, MissingRequiredProperties);

            // act
            var testResult = XDocument.Parse(WriteXcal(new[] { calendar }, MissingRequiredProperties));

            // assert
            var properties = testResult.Root.Element(Ns + VcalendarElement).Element(Ns + PropertiesElement).Elements().ToList();
            properties.Select(element => element.Name.LocalName).Should().Equal("version", "prodid");
            properties[0].Value.Should().Be("2.0");
        }

        [Fact]
        public void WriteCalendars_Progress_IsReportedAsIcalWriterReportsIt()
        {
            // arrange
            var calendars = ReadCalendars(Example2Ical + Example1Ical);
            var testResult = new RecordingProgress();
            using (var writer = new XcalWriter(new StringWriter()))
            {
                // act
                writer.WriteCalendars(calendars, testResult);

                // assert
                testResult.Reports.Should().Equal(new IcalWriteProgress(Example2LineCount, 5, 1), new IcalWriteProgress(Example2AndExample1LineCount, 6, 2));
                writer.CalendarsWritten.Should().Be(2);
                writer.ComponentsWritten.Should().Be(6);
                writer.PropertiesWritten.Should().Be(35);
            }
        }

        [Fact]
        public void WriteCalendar_NonConformant_Throws()
        {
            // arrange
            var calendar = ReadCalendar(ConformanceCases.All[ConformanceCases.MissingUidCase].Text, MissingRequiredProperties);
            var writer = new XcalWriter(new StringWriter());

            // act
            Action testAction = () => writer.WriteCalendar(calendar);

            // assert
            testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should()
                .Be(IcalDiagnosticKind.MissingRequiredProperty);
        }

        [Fact]
        public void Dispose_AfterNonConformantCalendar_HasWrittenNothing()
        {
            // arrange
            var calendar = ReadCalendar(ConformanceCases.All[ConformanceCases.MissingUidCase].Text, MissingRequiredProperties);
            var text = new StringWriter();
            var writer = new XcalWriter(text);
            Record.Exception(() => writer.WriteCalendar(calendar));

            // act
            writer.Dispose();

            // assert
            text.ToString().Should().BeEmpty();
        }

        [Fact]
        public void WriteCalendar_NameNotValidInXml_Throws()
        {
            // arrange
            var calendar = CreateCalendarWithProperty("1X", "value");
            using (var writer = new XcalWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                testAction.Should().Throw<IcalFileException>().WithMessage("*must start with a letter*");
            }
        }

        [Fact]
        public void Dispose_AfterNameNotValidInXml_HasWrittenNothing()
        {
            // arrange
            var calendar = CreateCalendarWithProperty("1X", "value");
            var text = new StringWriter();
            var writer = new XcalWriter(text);
            Record.Exception(() => writer.WriteCalendar(calendar));

            // act
            writer.Dispose();

            // assert
            text.ToString().Should().BeEmpty();
        }

        [Fact]
        public void WriteCalendar_CharacterXmlCannotHold_Throws()
        {
            // arrange
            var calendar = CreateCalendarWithProperty("X-BAD", "a\x0001b");
            using (var writer = new XcalWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                testAction.Should().Throw<IcalFileException>().WithMessage("*XML cannot hold*");
            }
        }

        [Fact]
        public void WriteCalendar_XmlWriterInLargerDocument_WritesElementInPlace()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);

            // act
            var testResult = XDocument.Parse(WriteInEnvelope(calendar));

            // assert
            testResult.Root.Name.LocalName.Should().Be("envelope");
            testResult.Root.Element(Ns + "icalendar").Elements(Ns + VcalendarElement).Should().ContainSingle();
        }

        [Fact]
        public void Dispose_NothingWritten_WritesNothing()
        {
            // arrange
            var text = new StringWriter();
            var writer = new XcalWriter(text);

            // act
            writer.Dispose();

            // assert
            text.ToString().Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(DisposedWriterMembers), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposed(Func<XcalWriter, object> member)
        {
            // arrange
            var writer = new XcalWriter(new StringWriter());
            writer.Dispose();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_Twice_DoesNotThrow()
        {
            // arrange
            var writer = new XcalWriter(new StringWriter());
            writer.Dispose();

            // act
            Action testAction = () => writer.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void WriteCalendars_CallsWriterDuringEnumeration_ThrowsInvalidOperation()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            using (var writer = new XcalWriter(new StringWriter()))
            {
                IEnumerable<IcalCalendar> Calendars()
                {
                    yield return calendar;
                    writer.WriteCalendar(calendar);
                }

                // act
                Action testAction = () => writer.WriteCalendars(Calendars());

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("Another operation*");
            }
        }

        [Fact]
        public void WriteCalendars_ContainsNull_ThrowsArgumentException()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            using (var writer = new XcalWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteCalendars(new[] { calendar, null });

                // assert
                testAction.Should().Throw<ArgumentException>();
                writer.CalendarsWritten.Should().Be(1);
            }
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

        private static IcalCalendar CreateCalendarWithProperty(string name, string value)
        {
            var calendar = ReadCalendar(EventCalendar("X-OK:1"));
            calendar.Events.Single().Properties.Add(new IcalContentLine(name, value));
            return calendar;
        }

        /// <summary>
        /// Writes a calendar with an <see cref="XcalWriter"/> over an <see cref="XmlWriter"/> that is inside an element of a larger document.
        /// </summary>
        private static string WriteInEnvelope(IcalCalendar calendar)
        {
            var text = new StringWriter();
            using (var xmlWriter = XmlWriter.Create(text, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                xmlWriter.WriteStartElement("envelope");
                using (var writer = new XcalWriter(xmlWriter))
                {
                    writer.WriteCalendar(calendar);
                }

                xmlWriter.WriteEndElement();
            }

            return text.ToString();
        }

        /// <summary>
        /// Gets the inner XML of a property element, without namespace declarations or formatting.
        /// </summary>
        private static string Inner(XElement properties, string name)
        {
            var element = properties.Element(Ns + name);
            return string.Concat(element.Elements().Select(child => Normalize(child).Replace(" xmlns=\"urn:ietf:params:xml:ns:icalendar-2.0\"", "")));
        }

        /// <summary>
        /// Gets an element as XML without insignificant white space, so that documents formatted differently compare equal.
        /// </summary>
        private static string Normalize(XNode node)
        {
            var copy = node is XDocument document ? new XElement(document.Root) : new XElement((XElement)node);
            foreach (var text in copy.DescendantNodes().OfType<XText>().Where(text => text.Parent.HasElements).ToList())
            {
                text.Remove();
            }

            return copy.ToString(SaveOptions.DisableFormatting);
        }
    }
}
