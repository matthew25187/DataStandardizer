using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class JcalWriterTests
    {
        private const string ComponentName = "vcalendar";

        // A summary with characters inside and outside the Basic Multilingual Plane, and quotation marks.
        private const string NonAsciiSummary = "Caf\xE9 \x4E2D \xD83D\xDE00 \"quoted\"";

        // NonAsciiSummary as System.Text.Json writes it: characters outside ASCII as they are, except those outside the Basic Multilingual Plane.
        private const string NonAsciiSummaryJson = "[\"summary\",{},\"text\",\"Caf\xE9 \x4E2D \\uD83D\\uDE00 \\\"quoted\\\"\"]";

        // The numbers of content lines written after each calendar of Example2Ical and Example1Ical, as IcalWriter reports them.
        private const long Example2LineCount = 40;
        private const long Example2AndExample1LineCount = 51;

        private static readonly IcalOptions MissingRequiredProperties = new IcalOptions { AllowMissingRequiredProperties = true };

        public static TheoryData<Func<JcalWriter, object>> DisposedWriterMembers()
        {
            var calendar = new IcalCalendar();
            return new TheoryData<Func<JcalWriter, object>>
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
            { () => new JcalWriter((Stream)null), typeof(ArgumentNullException) },
            { () => new JcalWriter(new MemoryStream(), null), typeof(ArgumentNullException) },
            { () => new JcalWriter(new MemoryStream(new byte[0], false)), typeof(ArgumentException) },
            { () => new JcalWriter((string)null), typeof(ArgumentNullException) },
        };

        [Fact]
        public void WriteCalendars_RfcExample1_MatchesJcalForm()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical);
            var expected = JsonNode.Parse(Example1Jcal);

            // act
            var testResult = WriteJcal(calendars);

            // assert: the same JSON, ignoring formatting.
            JsonNode.DeepEquals(JsonNode.Parse(testResult), expected).Should().BeTrue(testResult);
        }

        [Fact]
        public void WriteCalendars_RfcExample2_MatchesJcalFormWithPeriodArray()
        {
            // arrange: RFC 7265 §3.6.9 gives a PERIOD as an array, which Appendix B.2 does not follow.
            var calendars = ReadCalendars(Example2Ical);
            var expected = JsonNode.Parse(Example2Jcal.Replace("\"2006-01-02T15:00:00/PT2H\"", "[\"2006-01-02T15:00:00\", \"PT2H\"]"));

            // act
            var testResult = WriteJcal(calendars);

            // assert
            JsonNode.DeepEquals(JsonNode.Parse(testResult), expected).Should().BeTrue(testResult);
        }

        [Fact]
        public void WriteCalendar_Stream_WritesCompactUtf8WithoutByteOrderMark()
        {
            // arrange
            var stream = new MemoryStream();
            var calendar = ReadCalendar(EventCalendar("SUMMARY:" + NonAsciiSummary));

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
            json.Should().Contain(NonAsciiSummaryJson);
            stream.CanWrite.Should().BeTrue();
        }

        [Fact]
        public void ReadJcal_WrittenNonAsciiSummary_IsTheSummary()
        {
            // arrange
            var json = WriteJcal(new[] { ReadCalendar(EventCalendar("SUMMARY:" + NonAsciiSummary)) });

            // act
            var testResult = ReadJcal(json);

            // assert
            testResult.Should().ContainSingle().Which.Events.Should().ContainSingle().Which.Summary.Should().Be(NonAsciiSummary);
        }

        [Fact]
        public void WriteCalendars_OneCalendar_IsTheComponentArray()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical);

            // act
            var testResult = JsonNode.Parse(WriteJcal(calendars)).AsArray();

            // assert
            testResult[0].GetValue<string>().Should().Be(ComponentName);
        }

        [Fact]
        public void WriteCalendars_SeveralCalendars_IsAnArrayOfComponentArrays()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical + Example2Ical + Example1Ical);

            // act
            var testResult = JsonNode.Parse(WriteJcal(calendars)).AsArray();

            // assert
            testResult.Should().HaveCount(3);
            testResult.Select(calendar => calendar[0].GetValue<string>()).Should().AllBe(ComponentName);
        }

        [Fact]
        public void Dispose_NothingWritten_WritesNothing()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);

            // act
            writer.Dispose();

            // assert
            stream.ToArray().Should().BeEmpty();
        }

        [Fact]
        public void Flush_FirstCalendar_IsHeldUntilTheNext()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            writer.WriteCalendar(ReadCalendar(Example1Ical));

            // act
            writer.Flush();

            // assert
            stream.Length.Should().Be(0);
        }

        [Fact]
        public void Flush_SecondCalendar_WritesTheFirst()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            writer.WriteCalendar(ReadCalendar(Example1Ical));
            writer.Flush();
            writer.WriteCalendar(ReadCalendar(Example1Ical));

            // act
            writer.Flush();

            // assert
            stream.Length.Should().BePositive();
        }

        [Fact]
        public void Dispose_AfterTwoCalendars_WritesAnArrayOfBoth()
        {
            // arrange
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            writer.WriteCalendar(ReadCalendar(Example1Ical));
            writer.Flush();
            writer.WriteCalendar(ReadCalendar(Example1Ical));
            writer.Flush();

            // act
            writer.Dispose();

            // assert
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
            var testResult = EventProperties(WriteJcal(new[] { calendar }));

            // assert
            testResult["x-boolean"].Should().Be("[\"x-boolean\",{},\"boolean\",false]");
            testResult["x-integer"].Should().Be("[\"x-integer\",{},\"integer\",7]");
            testResult["x-float"].Should().Be("[\"x-float\",{},\"float\",-1.250]");
            testResult["x-float-plus"].Should().Be("[\"x-float-plus\",{},\"float\",1.5]");
            testResult["x-dates"].Should().Be("[\"x-dates\",{},\"date\",\"2026-01-01\",\"2026-01-02\"]");
            testResult["x-time"].Should().Be("[\"x-time\",{},\"time\",\"09:00:00Z\"]");
            testResult["x-utc-offset"].Should().Be("[\"x-utc-offset\",{},\"utc-offset\",\"-01:30:15\"]");
            testResult["x-period"].Should().Be("[\"x-period\",{},\"period\",[\"2026-01-01T09:00:00Z\",\"2026-01-01T10:00:00Z\"]]");
            testResult["x-recur"].Should().Be(
                "[\"x-recur\",{},\"recur\",{\"freq\":\"MONTHLY\",\"bymonthday\":[1,-1],\"count\":3}]");
            testResult["x-recur-until"].Should().Be("[\"x-recur-until\",{},\"recur\",{\"freq\":\"WEEKLY\",\"until\":\"2026-12-31\",\"byday\":\"MO\"}]");
            testResult["x-text"].Should().Be("[\"x-text\",{},\"text\",\"one, two\",\"three\\nfour\"]");
            testResult["x-raw"].Should().Be("[\"x-raw\",{},\"unknown\",\"a\\\\,b;c\"]");
            testResult["x-other"].Should().Be("[\"x-other\",{},\"x-name\",\"anything\"]");
            testResult["geo"].Should().Be("[\"geo\",{},\"float\",[37.386013,-122.082932]]");
            testResult["request-status"].Should().Be("[\"request-status\",{},\"text\",[\"3.7\",\"Invalid calendar user\",\"ATTENDEE:;mailto:jsmith@example.com\"]]");
            testResult["attendee"].Should().Be(
                "[\"attendee\",{\"delegated-to\":[\"mailto:a@example.com\",\"mailto:b@example.com\"],\"rsvp\":\"TRUE\"},\"cal-address\",\"mailto:c@example.com\"]");
        }

        [Fact]
        public void WriteCalendar_InvalidValueAllowed_WritesUnknownWithValueParameter()
        {
            // arrange
            var options = new IcalOptions { AllowInvalidValues = true };
            var calendar = ReadCalendar(EventCalendar("DTEND;VALUE=DATE:2026-01-02", "X-COUNT;VALUE=INTEGER:many"), options);

            // act
            var testResult = EventProperties(WriteJcal(new[] { calendar }, options));

            // assert
            testResult["dtend"].Should().Be("[\"dtend\",{\"value\":\"DATE\"},\"unknown\",\"2026-01-02\"]");
            testResult["x-count"].Should().Be("[\"x-count\",{\"value\":\"INTEGER\"},\"unknown\",\"many\"]");
        }

        [Fact]
        public void WriteCalendar_Version_IsAlways20AndFirstIfMissing()
        {
            // arrange
            var calendar = ReadCalendar(ConformanceCases.All[ConformanceCases.MissingVersionCase].Text, MissingRequiredProperties);

            // act
            var testResult = JsonNode.Parse(WriteJcal(new[] { calendar }, MissingRequiredProperties)).AsArray();

            // assert
            var properties = testResult[1].AsArray();
            properties.Select(property => property[0].GetValue<string>()).Should().Equal("version", "prodid");
            properties[0][3].GetValue<string>().Should().Be("2.0");
        }

        [Fact]
        public void WriteCalendars_Progress_IsReportedAsIcalWriterReportsIt()
        {
            // arrange
            var calendars = ReadCalendars(Example2Ical + Example1Ical);
            var testResult = new RecordingProgress();
            using (var writer = new JcalWriter(new MemoryStream()))
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
            var writer = new JcalWriter(new MemoryStream());

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
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            Record.Exception(() => writer.WriteCalendar(calendar));

            // act
            writer.Dispose();

            // assert
            stream.ToArray().Should().BeEmpty();
        }

        [Fact]
        public void WriteCalendar_SecondCalendarCannotBeWritten_Throws()
        {
            // arrange
            var bad = CreateCalendarWithBadPropertyName();
            using (var writer = new JcalWriter(new MemoryStream()))
            {
                writer.WriteCalendar(ReadCalendar(Example1Ical));

                // act
                Action testAction = () => writer.WriteCalendar(bad);

                // assert
                testAction.Should().Throw<IcalFileException>().WithMessage("*X BAD*");
                writer.CalendarsWritten.Should().Be(1);
            }
        }

        [Fact]
        public void Dispose_AfterSecondCalendarCannotBeWritten_KeepsTheFirst()
        {
            // arrange
            var expected = Model(ReadCalendars(Example1Ical));
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            writer.WriteCalendar(ReadCalendar(Example1Ical));
            Record.Exception(() => writer.WriteCalendar(CreateCalendarWithBadPropertyName()));

            // act
            writer.Dispose();

            // assert
            Model(ReadJcal(Encoding.UTF8.GetString(stream.ToArray()))).Should().Equal(expected);
        }

        [Fact]
        public void WriteCalendar_RepeatedParameter_Throws()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar("SUMMARY;LANGUAGE=en;language=fr:Hello"));

            // act
            Action testAction = () => WriteJcal(new[] { calendar });

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*LANGUAGE parameter appears more than once*")
                .Which.Data[IcalFileException.DataItemName.PropertyName].Should().Be("SUMMARY");
        }

        [Fact]
        public void WriteCalendar_LoneSurrogate_Throws()
        {
            // arrange
            var calendar = ReadCalendar(EventCalendar("X-OK:1"));
            calendar.Events.Single().Properties.Add(new IcalContentLine("X-RAW", "a\xD800b"));

            // act
            Action testAction = () => WriteJcal(new[] { calendar });

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*X-RAW*");
        }

        [Theory]
        [MemberData(nameof(DisposedWriterMembers), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposed(Func<JcalWriter, object> member)
        {
            // arrange
            var writer = new JcalWriter(new MemoryStream());
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
            var writer = new JcalWriter(new MemoryStream());
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
            using (var writer = new JcalWriter(new MemoryStream()))
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
            using (var writer = new JcalWriter(new MemoryStream()))
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

        [Fact]
        public void Constructor_EncodingOtherThanUtf8_ThrowsArgumentException()
        {
            // arrange
            var options = new IcalOptions { Encoding = Encoding.Unicode };

            // act
            Action testAction = () => new JcalWriter(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage("*UTF-8*");
        }

        [Fact]
        public void Constructor_Utf8Encoding_DoesNotThrow()
        {
            // arrange
            var options = new IcalOptions { Encoding = Encoding.UTF8 };

            // act
            Action testAction = () => new JcalWriter(new MemoryStream(), options).Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        private static IcalCalendar CreateCalendarWithBadPropertyName()
        {
            var calendar = ReadCalendar(EventCalendar("X-OK:1"));
            calendar.Events.Single().Properties.Add(new IcalContentLine("X BAD", "value"));
            return calendar;
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
