using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    public class JcalReaderTests
    {
        /// <summary>
        /// The JSONPath of the property after the UID and DTSTAMP of the VJOURNAL in a document built by <see cref="JcalTestData.JournalDocument"/>.
        /// </summary>
        private const string JournalPropertyPath = "$[2][0][1][2]";

        public static TheoryData<string, string> RfcExamples => new TheoryData<string, string>()
        {
            { Example1Jcal, Example1Ical },
            { Example2Jcal, Example2Ical }
        };

        [Theory]
        [MemberData(nameof(RfcExamples))]
        public void ReadCalendars_RfcExample_MatchesICalendarForm(string jcal, string ical)
        {
            // act
            var testResult = ReadJcal(jcal);

            // assert
            Model(testResult).Should().Equal(Model(ReadCalendars(ical)));
        }

        [Fact]
        public void ReadCalendars_RfcExample2_BuildsTypedModel()
        {
            // act
            var calendar = ReadJcal(Example2Jcal).Single();

            // assert
            calendar.ProductId.Should().Be("-//Example Corp.//Example Client//EN");
            var timeZone = calendar.TimeZones.Single();
            timeZone.Observances.Should().HaveCount(2);
            timeZone.Observances.ElementAt(1).RecurrenceRules.Single().GetByDay().Single().Ordinal.Should().Be(-1);
            var master = calendar.Events.First();
            master.RecurrenceRules.Single().Count.Should().Be(5);
            master.Description.Should().Be(
                "We are having a meeting all this week at 12 pm for one hour, with an additional meeting on the first day 2 hours long.\n" +
                "Please bring your own lunch for the 12 pm meetings.");
            var rdate = master.GetProperty("RDATE");
            rdate.Value.Should().Be("20060102T150000/PT2H");
            rdate.Parameters.Select(parameter => parameter.Name).Should().Equal("TZID", "VALUE");
        }

        [Fact]
        public void ReadCalendars_Counters_CountWhatWasRead()
        {
            // arrange
            using (var reader = new JcalReader(new MemoryStream(Utf8(Example2Jcal))))
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
        [InlineData("\"binary\", \"SGVsbG8=\"", "X;VALUE=BINARY;ENCODING=BASE64:SGVsbG8=")]
        [InlineData("\"boolean\", true", "X;VALUE=BOOLEAN:TRUE")]
        [InlineData("\"boolean\", false", "X;VALUE=BOOLEAN:FALSE")]
        [InlineData("\"cal-address\", \"mailto:a@example.com\"", "X;VALUE=CAL-ADDRESS:mailto:a@example.com")]
        [InlineData("\"date\", \"2011-05-17\"", "X;VALUE=DATE:20110517")]
        [InlineData("\"date-time\", \"2011-05-17T12:00:00Z\"", "X;VALUE=DATE-TIME:20110517T120000Z")]
        [InlineData("\"duration\", \"-P1DT2H\"", "X;VALUE=DURATION:-P1DT2H")]
        [InlineData("\"float\", 1.50", "X;VALUE=FLOAT:1.50")]
        [InlineData("\"float\", 1.5e2", "X;VALUE=FLOAT:150")]
        [InlineData("\"integer\", -100", "X;VALUE=INTEGER:-100")]
        [InlineData("\"integer\", 1e2", "X;VALUE=INTEGER:100")]
        [InlineData("\"period\", [\"2011-05-17T12:00:00Z\", \"2011-05-17T13:00:00Z\"]", "X;VALUE=PERIOD:20110517T120000Z/20110517T130000Z")]
        [InlineData("\"period\", [\"1997-03-08T16:00:00Z\", \"P1D\"]", "X;VALUE=PERIOD:19970308T160000Z/P1D")]
        [InlineData("\"period\", \"1997-03-08T16:00:00Z/P1D\"", "X;VALUE=PERIOD:19970308T160000Z/P1D")]
        [InlineData("\"recur\", {\"freq\": \"YEARLY\", \"count\": 5, \"byday\": [\"-1SU\", \"2MO\"], \"bymonth\": 10}",
            "X;VALUE=RECUR:FREQ=YEARLY;COUNT=5;BYDAY=-1SU,2MO;BYMONTH=10")]
        [InlineData("\"recur\", {\"freq\": \"MONTHLY\", \"interval\": 2, \"bymonthday\": [1, 15, -1], \"until\": \"2013-10-01\"}",
            "X;VALUE=RECUR:FREQ=MONTHLY;INTERVAL=2;BYMONTHDAY=1,15,-1;UNTIL=20131001")]
        [InlineData("\"recur\", {\"freq\": \"DAILY\", \"until\": \"2013-10-01T07:00:00Z\", \"bysetpos\": [1]}",
            "X;VALUE=RECUR:FREQ=DAILY;UNTIL=20131001T070000Z;BYSETPOS=1")]
        [InlineData("\"text\", \"a,b;c\\\\d\\ne\"", "X;VALUE=TEXT:a\\,b\\;c\\\\d\\ne")]
        [InlineData("\"text\", \"one\", \"two\"", "X;VALUE=TEXT:one,two")]
        [InlineData("\"time\", \"12:30:00Z\"", "X;VALUE=TIME:123000Z")]
        [InlineData("\"uri\", \"http://example.com/\"", "X;VALUE=URI:http://example.com/")]
        [InlineData("\"utc-offset\", \"+12:45\"", "X;VALUE=UTC-OFFSET:+1245")]
        [InlineData("\"unknown\", \"Stenophylla;Guinea\\\\,Africa\"", "X:Stenophylla;Guinea\\,Africa")]
        [InlineData("\"x-mytype\", \"anything; at all\"", "X;VALUE=X-MYTYPE:anything; at all")]
        public void ReadCalendar_Value_ConvertsToTextForm(string typeAndValues, string expected)
        {
            // arrange
            var json = JournalDocument("[\"x\", {}, " + typeAndValues + "]");

            // act
            var testResult = JournalProperty(ReadJcal(json, new IcalOptions { AllowInvalidValues = true }).Single());

            // assert
            (testResult.Name + FormatParameters(testResult) + ":" + testResult.Value).Should().Be(expected);
        }

        [Fact]
        public void ReadCalendar_RfcUnknownExamples_AreKeptAsTheyAre()
        {
            // arrange: RFC 7265 §5.3.
            var json = JournalDocument(
                "[\"x-complaint-deadline\", {}, \"unknown\", \"20110512T120000Z\"]," +
                "[\"x-coffee-data\", {}, \"unknown\", \"Stenophylla;Guinea\\\\,Africa\"]," +
                "[\"dtstart\", { \"x-slack\": \"30.3\" }, \"date\", \"2011-05-12\"]");

            // act
            var journal = ReadJcal(json).Single().Journals.Single();

            // assert
            journal.Properties.Skip(2).Select(line => line.Name + FormatParameters(line) + ":" + line.Value).Should().Equal(
                "X-COMPLAINT-DEADLINE:20110512T120000Z",
                "X-COFFEE-DATA:Stenophylla;Guinea\\,Africa",
                "DTSTART;X-SLACK=30.3;VALUE=DATE:20110512");
        }

        [Fact]
        public void ReadCalendar_Parameters_ConvertNamesAndValues()
        {
            // arrange: RFC 7265 §3.5 and §3.5.2.
            var json = JournalDocument(
                "[\"attendee\", {\"partstat\": \"ACCEPTED\", \"rsvp\": \"TRUE\", \"role\": \"REQ-PARTICIPANT\"," +
                " \"delegated-to\": [\"mailto:jdoe@example.org\", \"mailto:jqpublic@example.org\"], \"cn\": [\"Smith, J\"]}," +
                " \"cal-address\", \"mailto:jsmith@example.org\"]");

            // act
            var testResult = JournalProperty(ReadJcal(json).Single());

            // assert
            testResult.Parameters.Select(parameter => (parameter.Name, string.Join("|", parameter.Values))).Should().Equal(
                ("PARTSTAT", "ACCEPTED"), ("RSVP", "TRUE"), ("ROLE", "REQ-PARTICIPANT"),
                ("DELEGATED-TO", "mailto:jdoe@example.org|mailto:jqpublic@example.org"), ("CN", "Smith, J"));
        }

        [Fact]
        public void ReadCalendar_MultiValuedProperties_JoinValues()
        {
            // arrange
            var json = JournalDocument(
                "[\"categories\", {}, \"text\", \"Meetings\", \"Work, and play\"]," +
                "[\"exdate\", {}, \"date-time\", \"2026-01-02T00:00:00Z\", \"2026-01-03T00:00:00Z\"]");

            // act
            var journal = ReadJcal(json).Single().Journals.Single();

            // assert
            journal.GetProperty("CATEGORIES").Value.Should().Be("Meetings,Work\\, and play");
            journal.GetProperty("EXDATE").Value.Should().Be("20260102T000000Z,20260103T000000Z");
        }

        [Fact]
        public void ReadCalendar_GeoAndRequestStatus_ReadFromArrays()
        {
            // arrange: RFC 7265 §3.4.1.
            var json = JournalDocument(
                "[\"geo\", {}, \"float\", [ 37.386013, -122.082932 ] ]," +
                "[\"request-status\", {}, \"text\", [\"2.0\", \"Success\"] ]," +
                "[\"request-status\", {}, \"text\", [\"3.7\", \"Invalid calendar user\", \"ATTENDEE:mailto:jsmith@example.org\"] ]");

            // act
            var journal = ReadJcal(json, new IcalOptions { AllowCardinalityViolations = true }).Single().Journals.Single();

            // assert
            journal.GetProperty("GEO").Value.Should().Be("37.386013;-122.082932");
            journal.GetProperties("REQUEST-STATUS").Select(line => line.Value).Should().Equal(
                "2.0;Success", "3.7;Invalid calendar user;ATTENDEE:mailto:jsmith@example.org");
        }

        [Fact]
        public void ReadCalendar_Rfc7986PropertyWithDefault_AddsValueParameter()
        {
            // arrange
            var json = Document(
                "[\"prodid\",{},\"text\",\"-//Test//Test//EN\"],[\"version\",{},\"text\",\"2.0\"],[\"refresh-interval\",{},\"duration\",\"P1W\"]",
                "[\"vjournal\",[[\"uid\",{},\"text\",\"j@example.com\"],[\"dtstamp\",{},\"date-time\",\"2026-01-01T00:00:00Z\"]],[]]");

            // act
            var testResult = ReadJcal(json).Single().GetProperty("REFRESH-INTERVAL");

            // assert
            FormatParameters(testResult).Should().Be(";VALUE=DURATION");
        }

        [Fact]
        public void ReadCalendar_ExistingValueParameter_IsNotRepeated()
        {
            // arrange
            var json = JournalDocument("[\"dtstart\", {\"value\": \"DATE\"}, \"date\", \"2011-05-12\"]");

            // act
            var testResult = JournalProperty(ReadJcal(json).Single());

            // assert
            FormatParameters(testResult).Should().Be(";VALUE=DATE");
        }

        [Fact]
        public void ReadCalendars_ArrayOfCalendars_ReadsEach()
        {
            // arrange: RFC 7265 §3.2.
            var json = "[" + Example1Jcal + ",\n" + Example2Jcal + "]";

            // act
            var testResult = ReadJcal(json);

            // assert
            Model(testResult).Should().Equal(Model(ReadCalendars(Example1Ical + Example2Ical)));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        public void ReadCalendars_SmallReads_SplitTokensAcrossBlocks(int maxReadLength)
        {
            // arrange
            var json = "[" + Example1Jcal + "," + Example2Jcal + "]";
            using (var reader = new JcalReader(new TrickleStream(Utf8(json), maxReadLength)))
            {
                // act
                var testResult = reader.ReadCalendars().ToList();

                // assert
                Model(testResult).Should().Equal(Model(ReadJcal(json)));
            }
        }

        [Fact]
        public void ReadCalendar_TokenLargerThanBuffer_GrowsBuffer()
        {
            // arrange
            var description = string.Concat(Enumerable.Repeat("0123456789 ", 10000)) + "é中\U0001F600";
            var json = JournalDocument("[\"description\", {}, \"text\", \"" + description + "\"]");
            using (var reader = new JcalReader(new TrickleStream(Utf8(json), 5000)))
            {
                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Journals.Single().Descriptions.Single().GetTextValue().Should().Be(description);
            }
        }

        [Fact]
        public void ReadCalendar_ByteOrderMark_IsSkipped()
        {
            // arrange
            var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Utf8(Example1Jcal)).ToArray();

            // act
            using (var reader = new JcalReader(new TrickleStream(bytes, 1)))
            {
                var testResult = reader.ReadCalendar();

                // assert
                Model(new[] { testResult }).Should().Equal(Model(ReadCalendars(Example1Ical)));
            }
        }

        [Fact]
        public void ReadCalendar_EncodingOption_MustBeUtf8()
        {
            // assert
            FluentActions.Invoking(() => new JcalReader(new MemoryStream(), new IcalOptions { Encoding = IcalTestData.Latin1 }))
                .Should().Throw<ArgumentException>().WithMessage("*UTF-8*");
            using (var reader = new JcalReader(new MemoryStream(Utf8(Example1Jcal)), new IcalOptions { Encoding = new UTF8Encoding(true) }))
            {
                reader.ReadCalendar().Should().NotBeNull();
            }
        }

        [Fact]
        public void ReadCalendar_File_ReadsAndClosesFile()
        {
            // arrange
            var path = Path.GetTempFileName();
            try
            {
                System.IO.File.WriteAllText(path, Example1Jcal);

                // act
                using (var reader = new JcalReader(path))
                {
                    Model(reader.ReadCalendars().ToList()).Should().Equal(Model(ReadCalendars(Example1Ical)));
                }

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
        /// Documents that are not valid jCal, each with the JSONPath of the value at fault, and text that the value starts with, to find its byte
        /// position.
        /// </summary>
        public static TheoryData<string, string, string, string> NotValidJcal => new TheoryData<string, string, string, string>()
        {
            { "RootNotArray", "{\"vcalendar\": []}", "$", "{" },
            { "TopNotVcalendar", "[\"vevent\",[],[]]", "$[0]", "\"vevent\"" },
            { "ComponentTooShort", "[\"vcalendar\",[]]", "$", "[\"vcalendar\"" },
            { "ComponentTooLong", "[\"vcalendar\",[],[],[]]", "$[3]", "[]]" },
            { "ComponentNameNotString", "[[1],[],[]]", "$[0][0]", "1]" },
            { "PropertyNotArray", Document("5"), "$[1][0]", "5]" },
            { "ComponentNotArray", Document("", "\"vevent\""), "$[2][0]", "\"vevent\"" },
            { "PropertyTooShort", JournalDocument("[\"summary\",{},\"text\"]"), JournalPropertyPath, "[\"summary\"" },
            { "PropertyNameNotValid", JournalDocument("[\"sum mary\",{},\"text\",\"a\"]"), JournalPropertyPath + "[0]", "\"sum mary\"" },
            { "ParametersNotObject", JournalDocument("[\"summary\",[],\"text\",\"a\"]"), JournalPropertyPath + "[1]", "[],\"text\"" },
            { "ParameterValueNotString", JournalDocument("[\"summary\",{\"language\":5},\"text\",\"a\"]"), JournalPropertyPath + "[1]['language']", "5}" },
            { "ParameterValueEmptyArray", JournalDocument("[\"summary\",{\"language\":[]},\"text\",\"a\"]"), JournalPropertyPath + "[1]['language']", "[]}" },
            { "ParameterRepeated", JournalDocument("[\"summary\",{\"language\":\"en\",\"LANGUAGE\":\"fr\"},\"text\",\"a\"]"), JournalPropertyPath + "[1]['LANGUAGE']", "\"fr\"" },
            { "TypeNotString", JournalDocument("[\"summary\",{},1,\"a\"]"), JournalPropertyPath + "[2]", "1,\"a\"" },
            { "TextNotString", JournalDocument("[\"summary\",{},\"text\",5]"), JournalPropertyPath + "[3]", "5]" },
            { "BooleanAsNumber", JournalDocument("[\"x-flag\",{},\"boolean\",1]"), JournalPropertyPath + "[3]", "1]" },
            { "ValueNull", JournalDocument("[\"summary\",{},\"text\",null]"), JournalPropertyPath + "[3]", "null" },
            { "SecondValue", JournalDocument("[\"summary\",{},\"text\",\"a\",\"b\"]"), JournalPropertyPath + "[4]", "\"b\"" },
            { "PeriodTooLong", JournalDocument("[\"rdate\",{},\"period\",[\"2011-05-17T12:00:00Z\",\"PT1H\",\"PT2H\"]]"), JournalPropertyPath + "[3]", "[\"2011" },
            { "RulePartNotValid", JournalDocument("[\"rrule\",{},\"recur\",{\"freq\":true}]"), JournalPropertyPath + "[3]['freq']", "true" },
            { "RuleEmpty", JournalDocument("[\"rrule\",{},\"recur\",{}]"), JournalPropertyPath + "[3]", "{}]" },
            { "GeoNotTwoNumbers", JournalDocument("[\"geo\",{},\"float\",[1.5]]"), JournalPropertyPath + "[3]", "[1.5]" },
            { "RequestStatusTooShort", JournalDocument("[\"request-status\",{},\"text\",[\"2.0\"]]"), JournalPropertyPath + "[3]", "[\"2.0\"]" }
        };

        [Theory]
        [MemberData(nameof(NotValidJcal))]
        public void ReadCalendar_NotValidJcal_ThrowsWithLocation(string caseName, string json, string path, string marker)
        {
            // arrange
            using (var reader = new JcalReader(new MemoryStream(Utf8(json))))
            {
                // act
                Action act = () => reader.ReadCalendar();

                // assert
                var exception = act.Should().Throw<IcalFileException>(caseName).Which;
                exception.Data[IcalFileException.DataItemName.JsonPath].Should().Be(path, caseName);
                exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be((long)json.IndexOf(marker, StringComparison.Ordinal), caseName);
                exception.Data.Contains(IcalFileException.DataItemName.PhysicalLineNumber).Should().BeFalse(caseName);
                reader.Invoking(value => value.ReadCalendar()).Should().Throw<InvalidOperationException>(caseName);
            }
        }

        [Theory]
        [InlineData("[\"vcalendar\",[],[]")]
        [InlineData("")]
        [InlineData("[\"vcalendar\",[// comment\n],[]]")]
        [InlineData("<example1>]")]
        [InlineData("<example1> <example1>")]
        public void ReadCalendar_NotValidJson_WrapsJsonException(string json)
        {
            // arrange: what follows a complete calendar is found to be not valid when the next calendar is read.
            json = json.Replace("<example1>", Example1Jcal);
            using (var reader = new JcalReader(new TrickleStream(Utf8(json), 3)))
            {
                // act
                Action act = () => reader.ReadCalendars().ToList();

                // assert
                var exception = act.Should().Throw<IcalFileException>().WithMessage("The data is not valid JSON*").Which;
                exception.InnerException.Should().BeAssignableTo<System.Text.Json.JsonException>();
                exception.Data.Contains(IcalFileException.DataItemName.BytePosition).Should().BeTrue();
                exception.Data.Contains(IcalFileException.DataItemName.LineNumber).Should().BeTrue();
            }
        }

        [Fact]
        public void ReadCalendar_NotValidJson_GivesLineAndPosition()
        {
            // arrange
            var json = "[\"vcalendar\",\n  [x],[]]";

            // act
            Action act = () => ReadJcal(json);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
            exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be(4L);
            exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be((long)json.IndexOf('[', 1) + 1);
        }

        [Fact]
        public void ReadCalendar_StringNotValidUtf8_Throws()
        {
            // arrange
            var bytes = Utf8(JournalDocument("[\"summary\",{},\"text\",\"a#b\"]"));
            bytes[Array.IndexOf(bytes, (byte)'#')] = 0xFF;

            // act
            Action act = () => new JcalReader(new MemoryStream(bytes)).ReadCalendar();

            // assert
            act.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void ReadCalendar_LoneSurrogateEscape_Throws()
        {
            // arrange
            var json = JournalDocument("[\"summary\",{},\"text\",\"a\\uD800b\"]");

            // act
            Action act = () => ReadJcal(json);

            // assert
            act.Should().Throw<IcalFileException>();
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("[\"vcalendar\"")]
        public void ReadCalendar_NoCalendar_Throws(string json)
        {
            // act
            Action act = () => ReadJcal(json);

            // assert
            act.Should().Throw<IcalFileException>();
            if (json == "[]")
            {
                act.Should().Throw<IcalFileException>().WithMessage("*no VCALENDAR*");
            }
        }

        [Fact]
        public void ReadCalendar_UnsupportedVersion_ThrowsWithLocation()
        {
            // arrange
            var json = Document("[\"version\",{},\"text\",\"1.0\"]");

            // act
            Action act = () => ReadJcal(json);

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.UnsupportedVersion);
            exception.Data[IcalFileException.DataItemName.JsonPath].Should().Be("$[1][0]");
            exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be((long)json.IndexOf("[\"version\"", StringComparison.Ordinal));
            exception.Data.Contains(IcalFileException.DataItemName.LineNumber).Should().BeFalse();
        }

        [Fact]
        public void ReadCalendar_NonConformantCalendar_ThrowsAndContinues()
        {
            // arrange: the first calendar's VJOURNAL has no UID.
            var bad = Document(
                "[\"prodid\",{},\"text\",\"-//Test//Test//EN\"],[\"version\",{},\"text\",\"2.0\"]",
                "[\"vjournal\",[[\"dtstamp\",{},\"date-time\",\"2026-01-01T00:00:00Z\"]],[]]");
            using (var reader = new JcalReader(new MemoryStream(Utf8("[" + bad + "," + Example1Jcal + "]"))))
            {
                // act
                Action act = () => reader.ReadCalendar();

                // assert
                var exception = act.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.MissingRequiredProperty);
                exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VJOURNAL[1]");
                reader.ReadCalendar().Events.Should().ContainSingle();
                reader.ReadCalendar().Should().BeNull();
                reader.ReadCalendar().Should().BeNull();
            }
        }

        [Fact]
        public void ReadCalendar_InvalidValue_RejectedUnlessAllowed()
        {
            // arrange
            var json = JournalDocument("[\"dtstart\",{},\"date-time\",\"2026-02-30T00:00:00\"]");

            // act
            Action act = () => ReadJcal(json);
            var allowed = ReadJcal(json, new IcalOptions { AllowInvalidValues = true });

            // assert
            var exception = act.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DTSTART");
            JournalProperty(allowed.Single()).Value.Should().Be("2026-02-30T00:00:00");
        }

        [Fact]
        public void ReadCalendar_ValueAsStringOfOtherType_IsKeptForTheCheck()
        {
            // arrange: a string is taken for any type, so a value that does not follow its type reaches the conformance check.
            var json = JournalDocument("[\"percent-complete\",{},\"integer\",\"lots\"]");

            // act
            Action act = () => ReadJcal(json);

            // assert
            act.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
            JournalProperty(ReadJcal(json, new IcalOptions { AllowInvalidValues = true }).Single()).Value.Should().Be("lots");
        }

        [Fact]
        public void ReadCalendar_TextIcalendarCannotHold_Throws()
        {
            // arrange
            var json = JournalDocument("[\"summary\",{},\"text\",\"a\\u007Fb\"]");

            // act
            Action act = () => ReadJcal(json);

            // assert
            act.Should().Throw<IcalFileException>().WithMessage("*cannot hold*");
        }

        [Fact]
        public void Constructors_InvalidArguments_Throw()
        {
            // assert
            FluentActions.Invoking(() => new JcalReader((Stream)null)).Should().Throw<ArgumentNullException>().WithParameterName("jcalStream");
            FluentActions.Invoking(() => new JcalReader(new MemoryStream(), null)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => new JcalReader(new WriteOnlyStream())).Should().Throw<ArgumentException>();
            FluentActions.Invoking(() => new JcalReader((string)null)).Should().Throw<ArgumentNullException>().WithParameterName("jcalFilePath");
            FluentActions.Invoking(() => new JcalReader("x.json", null)).Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Dispose_ThenUse_ThrowsObjectDisposed()
        {
            // arrange
            var reader = new JcalReader(new MemoryStream(Utf8(Example1Jcal)));
            reader.Dispose();
            reader.Dispose();

            // assert
            reader.Invoking(value => value.ReadCalendar()).Should().Throw<ObjectDisposedException>();
            reader.Invoking(value => value.ReadCalendars()).Should().Throw<ObjectDisposedException>();
            reader.Invoking(value => value.ReadCalendarsAsync()).Should().Throw<ObjectDisposedException>();
            reader.Invoking(value => value.CalendarsRead).Should().Throw<ObjectDisposedException>();
            reader.Invoking(value => value.ComponentsRead).Should().Throw<ObjectDisposedException>();
            reader.Invoking(value => value.PropertiesRead).Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CallerStream_IsLeftOpen()
        {
            // arrange
            var stream = new TrickleStream(Utf8(Example1Jcal), 100);

            // act
            using (var reader = new JcalReader(stream))
            {
                reader.ReadCalendar();
            }

            // assert
            stream.IsDisposed.Should().BeFalse();
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
}
