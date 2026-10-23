using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class JcalWriterTests
    {
        [Fact]
        public void WriteCalendars_RfcExample1_MatchesJcalForm()
        {
            // act
            var testResult = WriteJcal(ReadCalendars(Example1Ical));

            // assert: the same JSON, ignoring formatting.
            JsonNode.DeepEquals(JsonNode.Parse(testResult), JsonNode.Parse(Example1Jcal)).Should().BeTrue(testResult);
        }

        [Fact]
        public void WriteCalendars_RfcExample2_MatchesJcalFormWithPeriodArray()
        {
            // arrange: RFC 7265 §3.6.9 gives a PERIOD as an array, which Appendix B.2 does not follow.
            var expected = Example2Jcal.Replace("\"2006-01-02T15:00:00/PT2H\"", "[\"2006-01-02T15:00:00\", \"PT2H\"]");

            // act
            var testResult = WriteJcal(ReadCalendars(Example2Ical));

            // assert
            JsonNode.DeepEquals(JsonNode.Parse(testResult), JsonNode.Parse(expected)).Should().BeTrue(testResult);
        }

        [Fact]
        public void WriteCalendar_Stream_WritesCompactUtf8WithoutByteOrderMark()
        {
            // arrange
            var stream = new MemoryStream();
            var calendar = ReadCalendar(EventCalendar("SUMMARY:Café 中 \U0001F600 \"quoted\""));

            // act
            using (var writer = new JcalWriter(stream))
            {
                writer.WriteCalendar(calendar);
            }

            // assert
            var bytes = stream.ToArray();
            bytes.Take(3).Should().NotEqual(new byte[] { 0xEF, 0xBB, 0xBF });
            var json = Encoding.UTF8.GetString(bytes);
            json.Should().StartWith("[\"vcalendar\",[[\"version\",{},\"text\",\"2.0\"],");
            json.Should().NotContain("\n");
            // Characters outside ASCII are written as they are, but System.Text.Json always escapes those outside the Basic Multilingual Plane.
            json.Should().Contain("[\"summary\",{},\"text\",\"Café 中 \\uD83D\\uDE00 \\\"quoted\\\"\"]");
            ReadJcal(json).Single().Events.Single().Summary.Should().Be("Café 中 \U0001F600 \"quoted\"");
            stream.CanWrite.Should().BeTrue();
        }

        [Fact]
        public void WriteCalendars_OneCalendar_IsTheComponentArray()
        {
            // act
            var testResult = JsonNode.Parse(WriteJcal(ReadCalendars(Example1Ical))).AsArray();

            // assert
            testResult[0].GetValue<string>().Should().Be("vcalendar");
        }

        [Fact]
        public void WriteCalendars_SeveralCalendars_IsAnArrayOfComponentArrays()
        {
            // act
            var testResult = JsonNode.Parse(WriteJcal(ReadCalendars(Example1Ical + Example2Ical + Example1Ical))).AsArray();

            // assert
            testResult.Should().HaveCount(3);
            testResult.Select(calendar => calendar[0].GetValue<string>()).Should().AllBe("vcalendar");
        }

        [Fact]
        public void Dispose_NothingWritten_WritesNothing()
        {
            // arrange
            var stream = new MemoryStream();

            // act
            new JcalWriter(stream).Dispose();

            // assert
            stream.ToArray().Should().BeEmpty();
        }

        [Fact]
        public void Flush_FirstCalendar_IsHeldUntilTheNextOrDispose()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            writer.WriteCalendar(ReadCalendar(Example1Ical));

            // act
            writer.Flush();
            var afterFirst = stream.Length;
            writer.WriteCalendar(ReadCalendar(Example1Ical));
            writer.Flush();
            var afterSecond = stream.Length;
            writer.Dispose();

            // assert
            afterFirst.Should().Be(0);
            afterSecond.Should().BeGreaterThan(0);
            JsonNode.Parse(stream.ToArray()).AsArray().Should().HaveCount(2);
        }

        [Fact]
        public void WriteCalendar_ValueTypes_WriteJsonValues()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar(
                "X-BOOLEAN;VALUE=BOOLEAN:FALSE",
                "X-INTEGER;VALUE=INTEGER:+7",
                "X-FLOAT;VALUE=FLOAT:-1.250",
                "X-FLOAT-PLUS;VALUE=FLOAT:+01.5",
                "X-DATES;VALUE=DATE:20260101,20260102",
                "X-TIME;VALUE=TIME:090000Z",
                "X-UTC-OFFSET;VALUE=UTC-OFFSET:-013015",
                "X-PERIOD;VALUE=PERIOD:20260101T090000Z/20260101T100000Z",
                "X-RECUR;VALUE=RECUR:FREQ=MONTHLY;BYMONTHDAY=1,-1;COUNT=3",
                "X-RECUR-UNTIL;VALUE=RECUR:FREQ=WEEKLY;UNTIL=20261231;BYDAY=MO",
                "X-TEXT;VALUE=TEXT:one\\, two,three\\nfour",
                "X-RAW:a\\,b;c",
                "X-OTHER;VALUE=X-NAME:anything",
                "GEO:37.386013;-122.082932",
                "REQUEST-STATUS:3.7;Invalid calendar user;ATTENDEE:\\;mailto:jsmith@example.com",
                "ATTENDEE;DELEGATED-TO=\"mailto:a@example.com\",\"mailto:b@example.com\";RSVP=TRUE:mailto:c@example.com"));

            // act
            var properties = EventProperties(WriteJcal(new[] { calendar }));

            // assert
            properties["x-boolean"].Should().Be("[\"x-boolean\",{},\"boolean\",false]");
            properties["x-integer"].Should().Be("[\"x-integer\",{},\"integer\",7]");
            properties["x-float"].Should().Be("[\"x-float\",{},\"float\",-1.250]");
            properties["x-float-plus"].Should().Be("[\"x-float-plus\",{},\"float\",1.5]");
            properties["x-dates"].Should().Be("[\"x-dates\",{},\"date\",\"2026-01-01\",\"2026-01-02\"]");
            properties["x-time"].Should().Be("[\"x-time\",{},\"time\",\"09:00:00Z\"]");
            properties["x-utc-offset"].Should().Be("[\"x-utc-offset\",{},\"utc-offset\",\"-01:30:15\"]");
            properties["x-period"].Should().Be("[\"x-period\",{},\"period\",[\"2026-01-01T09:00:00Z\",\"2026-01-01T10:00:00Z\"]]");
            properties["x-recur"].Should().Be(
                "[\"x-recur\",{},\"recur\",{\"freq\":\"MONTHLY\",\"bymonthday\":[1,-1],\"count\":3}]");
            properties["x-recur-until"].Should().Be("[\"x-recur-until\",{},\"recur\",{\"freq\":\"WEEKLY\",\"until\":\"2026-12-31\",\"byday\":\"MO\"}]");
            properties["x-text"].Should().Be("[\"x-text\",{},\"text\",\"one, two\",\"three\\nfour\"]");
            properties["x-raw"].Should().Be("[\"x-raw\",{},\"unknown\",\"a\\\\,b;c\"]");
            properties["x-other"].Should().Be("[\"x-other\",{},\"x-name\",\"anything\"]");
            properties["geo"].Should().Be("[\"geo\",{},\"float\",[37.386013,-122.082932]]");
            properties["request-status"].Should().Be("[\"request-status\",{},\"text\",[\"3.7\",\"Invalid calendar user\",\"ATTENDEE:;mailto:jsmith@example.com\"]]");
            properties["attendee"].Should().Be(
                "[\"attendee\",{\"delegated-to\":[\"mailto:a@example.com\",\"mailto:b@example.com\"],\"rsvp\":\"TRUE\"},\"cal-address\",\"mailto:c@example.com\"]");
        }

        [Fact]
        public void WriteCalendar_InvalidValueAllowed_WritesUnknownWithValueParameter()
        {
            // arrange
            var options = new IcalOptions { AllowInvalidValues = true };
            var calendar = ReadCalendar(EventCalendar("DTEND;VALUE=DATE:2026-01-02", "X-COUNT;VALUE=INTEGER:many"), options);

            // act
            var properties = EventProperties(WriteJcal(new[] { calendar }, options));

            // assert
            properties["dtend"].Should().Be("[\"dtend\",{\"value\":\"DATE\"},\"unknown\",\"2026-01-02\"]");
            properties["x-count"].Should().Be("[\"x-count\",{\"value\":\"INTEGER\"},\"unknown\",\"many\"]");
        }

        [Fact]
        public void WriteCalendar_Version_IsAlways20AndFirstIfMissing()
        {
            // arrange
            var options = new IcalOptions { AllowMissingRequiredProperties = true };
            var calendar = ReadCalendar(IcalTestData.Crlf("BEGIN:VCALENDAR", "PRODID:-//Test//Test//EN", "BEGIN:VJOURNAL", "UID:j@example.com",
                "DTSTAMP:20260101T000000Z", "END:VJOURNAL", "END:VCALENDAR"), options);

            // act
            var document = JsonNode.Parse(WriteJcal(new[] { calendar }, options)).AsArray();

            // assert
            var properties = document[1].AsArray();
            properties.Select(property => property[0].GetValue<string>()).Should().Equal("version", "prodid");
            properties[0][3].GetValue<string>().Should().Be("2.0");
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
            using (var writer = new JcalWriter(new MemoryStream()))
            {
                // act
                writer.WriteCalendars(calendars, testResult);

                // assert
                testResult.Reports.Should().Equal(icalProgress.Reports);
                writer.CalendarsWritten.Should().Be(2);
                writer.ComponentsWritten.Should().Be(6);
                writer.PropertiesWritten.Should().Be(35);
            }
        }

        [Fact]
        public void WriteCalendar_NonConformant_ThrowsAndWritesNothing()
        {
            // arrange
            var calendar = ReadCalendar(ConformanceCases.All["MissingUid"].Text, new IcalOptions { AllowMissingRequiredProperties = true });
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);

            // act
            Action act = () => writer.WriteCalendar(calendar);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.MissingRequiredProperty);
            writer.Dispose();
            stream.ToArray().Should().BeEmpty();
        }

        [Fact]
        public void WriteCalendar_SecondCalendarCannotBeWritten_KeepsTheFirst()
        {
            // arrange
            var bad = ReadCalendar(EventCalendar("X-OK:1"));
            bad.Events.Single().Properties.Add(new IcalContentLine("X BAD", "value"));
            var stream = new MemoryStream();

            // act
            using (var writer = new JcalWriter(stream))
            {
                writer.WriteCalendar(ReadCalendar(Example1Ical));
                writer.Invoking(value => value.WriteCalendar(bad)).Should().Throw<IcalFileException>().WithMessage("*X BAD*");
                writer.CalendarsWritten.Should().Be(1);
            }

            // assert
            Model(ReadJcal(Encoding.UTF8.GetString(stream.ToArray()))).Should().Equal(Model(ReadCalendars(Example1Ical)));
        }

        [Fact]
        public void WriteCalendar_RepeatedParameter_Throws()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar("SUMMARY;LANGUAGE=en;language=fr:Hello"));

            // act
            Action act = () => WriteJcal(new[] { calendar });

            // assert
            act.Should().Throw<IcalFileException>().WithMessage("*LANGUAGE parameter appears more than once*")
                .Which.Data[IcalFileException.DataItemName.PropertyName].Should().Be("SUMMARY");
        }

        [Fact]
        public void WriteCalendar_LoneSurrogate_Throws()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar("X-OK:1"));
            calendar.Events.Single().Properties.Add(new IcalContentLine("X-RAW", "a\uD800b"));

            // act
            Action act = () => WriteJcal(new[] { calendar });

            // assert
            act.Should().Throw<IcalFileException>().WithMessage("*X-RAW*");
        }

        [Fact]
        public void Dispose_ThenUse_ThrowsObjectDisposed()
        {
            // arrange
            var writer = new JcalWriter(new MemoryStream());
            writer.Dispose();
            writer.Dispose();
            var calendar = ReadCalendar(Example1Ical);

            // assert
            writer.Invoking(value => value.WriteCalendar(calendar)).Should().Throw<ObjectDisposedException>();
            writer.Invoking(value => value.WriteCalendars(new[] { calendar })).Should().Throw<ObjectDisposedException>();
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
            using (var writer = new JcalWriter(new MemoryStream()))
            {
                IEnumerable<IcalCalendar> Calendars()
                {
                    yield return calendar;
                    writer.WriteCalendar(calendar);
                }

                // act
                Action act = () => writer.WriteCalendars(Calendars());

                // assert
                act.Should().Throw<InvalidOperationException>().WithMessage("Another operation*");
            }
        }

        [Fact]
        public void WriteCalendars_ContainsNull_ThrowsArgumentException()
        {
            // arrange
            using (var writer = new JcalWriter(new MemoryStream()))
            {
                // act
                Action act = () => writer.WriteCalendars(new[] { ReadCalendar(Example1Ical), null });

                // assert
                act.Should().Throw<ArgumentException>();
                writer.CalendarsWritten.Should().Be(1);
            }
        }

        [Fact]
        public void Constructors_InvalidArguments_Throw()
        {
            // assert
            FluentActions.Invoking(() => new JcalWriter((Stream)null)).Should().Throw<ArgumentNullException>().WithParameterName("jcalStream");
            FluentActions.Invoking(() => new JcalWriter(new MemoryStream(), null)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => new JcalWriter(new MemoryStream(new byte[0], false))).Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => new JcalWriter((string)null)).Should().Throw<ArgumentNullException>().WithParameterName("jcalFilePath");
            FluentActions.Invoking(() => new JcalWriter(new MemoryStream(), new IcalOptions { Encoding = Encoding.Unicode }))
                .Should().Throw<ArgumentException>().WithMessage("*UTF-8*");
            new JcalWriter(new MemoryStream(), new IcalOptions { Encoding = Encoding.UTF8 }).Dispose();
        }

        [Fact]
        public void WriteCalendar_File_WritesAndClosesFile()
        {
            // arrange
            var path = Path.GetTempFileName();
            try
            {
                // act
                using (var writer = new JcalWriter(path))
                {
                    writer.WriteCalendar(ReadCalendar(Example1Ical));
                }

                // assert
                using (var reader = new JcalReader(path))
                {
                    Model(reader.ReadCalendars().ToList()).Should().Equal(Model(ReadCalendars(Example1Ical)));
                }

                System.IO.File.Delete(path);
                System.IO.File.Exists(path).Should().BeFalse();
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        /// <summary>
        /// Gets the property arrays of the one VEVENT of a calendar written as jCal, as compact JSON, by property name.
        /// </summary>
        private static Dictionary<string, string> EventProperties(string json)
        {
            var calendar = JsonNode.Parse(json).AsArray();
            var vevent = calendar[2].AsArray().Single(component => component[0].GetValue<string>() == "vevent");
            return vevent[1].AsArray().ToDictionary(property => property[0].GetValue<string>(), property => property.ToJsonString(
                new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
        }
    }
}
