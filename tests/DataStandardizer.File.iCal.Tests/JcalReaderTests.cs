using System;
using System.IO;
using System.Linq;
using System.Text;
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

        // The byte position of "[x]" in NotValidJsonDocument, and of the VERSION property in UnsupportedVersionDocument.
        private const string NotValidJsonDocument = "[\"vcalendar\",\n  [x],[]]";
        private const long NotValidJsonBytePosition = 17;
        private const long UnsupportedVersionBytePosition = 14;

        private const string ExampleNotValidDateTime = "2026-02-30T00:00:00";

        private static readonly string UnsupportedVersionDocument = Document("[\"version\",{},\"text\",\"1.0\"]");

        // ref. RFC 7265 §3.2: a document that is an array of the two example calendars.
        private static readonly string ArrayOfExamples = "[" + Example1Jcal + ",\n" + Example2Jcal + "]";

        // The name in FixtureExpectations of the content lines of the two example calendars, one after the other.
        private const string Example1ThenExample2Name = "Example1IcalThenExample2Ical";

        // Example1Jcal in UTF-8, after the UTF-8 byte order mark.
        private static readonly byte[] Example1JcalWithByteOrderMark = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Utf8(Example1Jcal)).ToArray();

        // A description of more than 100,000 characters, with no character that TEXT escapes, the journal document that holds it, and how many
        // octets each read gives, far fewer than the description has.
        private static readonly string LongDescription = string.Concat(Enumerable.Repeat("0123456789 ", 10000)) + "\xE9\x4E2D\xD83D\xDE00";
        private static readonly string LongDescriptionDocument = JournalDocument("[\"description\", {}, \"text\", \"" + LongDescription + "\"]");
        private const int LongDescriptionReadLength = 5000;

        private static readonly IcalOptions AllowInvalidValues = new IcalOptions { AllowInvalidValues = true };

        // Each RFC example, with the content lines of its iCalendar form.
        public static TheoryData<string, string[]> RfcExamples => new TheoryData<string, string[]>()
        {
            { Example1Jcal, FixtureExpectations.JcalRoundTripModels[nameof(Example1Ical)] },
            { Example2Jcal, FixtureExpectations.JcalRoundTripModels[nameof(Example2Ical)] }
        };

        /// <summary>
        /// Documents that are not valid jCal, each with the JSONPath of the value at fault, and its byte position.
        /// </summary>
        public static TheoryData<string, string, string, long> NotValidJcal()
        {
            var cases = new[]
            {
                // The name of the case, the document, the JSONPath, and text that the value at fault starts with, to find its byte position.
                ("RootNotArray", "{\"vcalendar\": []}", "$", "{"),
                ("TopNotVcalendar", "[\"vevent\",[],[]]", "$[0]", "\"vevent\""),
                ("ComponentTooShort", "[\"vcalendar\",[]]", "$", "[\"vcalendar\""),
                ("ComponentTooLong", "[\"vcalendar\",[],[],[]]", "$[3]", "[]]"),
                ("ComponentNameNotString", "[[1],[],[]]", "$[0][0]", "1]"),
                ("PropertyNotArray", Document("5"), "$[1][0]", "5]"),
                ("ComponentNotArray", Document("", "\"vevent\""), "$[2][0]", "\"vevent\""),
                ("PropertyTooShort", JournalDocument("[\"summary\",{},\"text\"]"), JournalPropertyPath, "[\"summary\""),
                ("PropertyNameNotValid", JournalDocument("[\"sum mary\",{},\"text\",\"a\"]"), JournalPropertyPath + "[0]", "\"sum mary\""),
                ("ParametersNotObject", JournalDocument("[\"summary\",[],\"text\",\"a\"]"), JournalPropertyPath + "[1]", "[],\"text\""),
                ("ParameterValueNotString", JournalDocument("[\"summary\",{\"language\":5},\"text\",\"a\"]"), JournalPropertyPath + "[1]['language']", "5}"),
                ("ParameterValueEmptyArray", JournalDocument("[\"summary\",{\"language\":[]},\"text\",\"a\"]"), JournalPropertyPath + "[1]['language']", "[]}"),
                ("ParameterRepeated", JournalDocument("[\"summary\",{\"language\":\"en\",\"LANGUAGE\":\"fr\"},\"text\",\"a\"]"),
                    JournalPropertyPath + "[1]['LANGUAGE']", "\"fr\""),
                ("TypeNotString", JournalDocument("[\"summary\",{},1,\"a\"]"), JournalPropertyPath + "[2]", "1,\"a\""),
                ("TextNotString", JournalDocument("[\"summary\",{},\"text\",5]"), JournalPropertyPath + "[3]", "5]"),
                ("BooleanAsNumber", JournalDocument("[\"x-flag\",{},\"boolean\",1]"), JournalPropertyPath + "[3]", "1]"),
                ("ValueNull", JournalDocument("[\"summary\",{},\"text\",null]"), JournalPropertyPath + "[3]", "null"),
                ("SecondValue", JournalDocument("[\"summary\",{},\"text\",\"a\",\"b\"]"), JournalPropertyPath + "[4]", "\"b\""),
                ("PeriodTooLong", JournalDocument("[\"rdate\",{},\"period\",[\"2011-05-17T12:00:00Z\",\"PT1H\",\"PT2H\"]]"), JournalPropertyPath + "[3]",
                    "[\"2011"),
                ("RulePartNotValid", JournalDocument("[\"rrule\",{},\"recur\",{\"freq\":true}]"), JournalPropertyPath + "[3]['freq']", "true"),
                ("RuleEmpty", JournalDocument("[\"rrule\",{},\"recur\",{}]"), JournalPropertyPath + "[3]", "{}]"),
                ("GeoNotTwoNumbers", JournalDocument("[\"geo\",{},\"float\",[1.5]]"), JournalPropertyPath + "[3]", "[1.5]"),
                ("RequestStatusTooShort", JournalDocument("[\"request-status\",{},\"text\",[\"2.0\"]]"), JournalPropertyPath + "[3]", "[\"2.0\"]"),
            };
            var data = new TheoryData<string, string, string, long>();
            foreach (var (name, json, path, marker) in cases)
            {
                data.Add(name, json, path, json.IndexOf(marker, StringComparison.Ordinal));
            }

            return data;
        }

        // Documents that are not valid JSON; what follows a complete calendar is found to be not valid when the next calendar is read.
        public static TheoryData<string> NotValidJson => new TheoryData<string>
        {
            "[\"vcalendar\",[],[]",
            "",
            "[\"vcalendar\",[// comment\n],[]]",
            Example1Jcal + "]",
            Example1Jcal + " " + Example1Jcal,
        };

        public static TheoryData<Action, string> NullConstructorArguments => new TheoryData<Action, string>
        {
            { () => new JcalReader((Stream)null), "jcalStream" },
            { () => new JcalReader(new MemoryStream(), null), "options" },
            { () => new JcalReader((string)null), "jcalFilePath" },
            { () => new JcalReader("x.json", null), "options" },
        };

        public static TheoryData<Func<JcalReader, object>> DisposedReaderMembers => new TheoryData<Func<JcalReader, object>>
        {
            reader => reader.ReadCalendar(),
            reader => reader.ReadCalendars(),
            reader => reader.ReadCalendarsAsync(),
            reader => reader.CalendarsRead,
            reader => reader.ComponentsRead,
            reader => reader.PropertiesRead,
        };

        [Theory]
        [MemberData(nameof(RfcExamples), DisableDiscoveryEnumeration = true)]
        public void ReadCalendars_RfcExample_MatchesICalendarForm(string jcal, string[] expected)
        {
            // act
            var testResult = Model(ReadJcal(jcal));

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void ReadCalendars_RfcExample2_BuildsTypedModel()
        {
            // act
            var testResult = ReadJcal(Example2Jcal).Single();

            // assert
            testResult.ProductId.Should().Be("-//Example Corp.//Example Client//EN");
            testResult.TimeZones.Should().ContainSingle().Which.Observances.Should().HaveCount(2);
            var master = testResult.Events.First();
            master.RecurrenceRules.Should().ContainSingle().Which.Count.Should().Be(5);
            master.Description.Should().Be(
                "We are having a meeting all this week at 12 pm for one hour, with an additional meeting on the first day 2 hours long.\n" +
                "Please bring your own lunch for the 12 pm meetings.");
            var rdate = master.Properties.Should().ContainSingle(property => property.Name == "RDATE").Subject;
            rdate.Value.Should().Be("20060102T150000/PT2H");
            rdate.Parameters.Select(parameter => parameter.Name).Should().Equal("TZID", "VALUE");
        }

        [Fact]
        public void GetByDay_RfcExample2SecondObservanceRule_IsTheLastSunday()
        {
            // arrange
            var rule = ReadJcal(Example2Jcal).Single().TimeZones.Single().Observances.ElementAt(1).RecurrenceRules.Single();

            // act
            var testResult = rule.GetByDay();

            // assert
            testResult.Should().ContainSingle().Which.Ordinal.Should().Be(-1);
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
        [InlineData("[\"x\", {}, \"binary\", \"SGVsbG8=\"]", "X;VALUE=BINARY;ENCODING=BASE64:SGVsbG8=")]
        [InlineData("[\"x\", {}, \"boolean\", true]", "X;VALUE=BOOLEAN:TRUE")]
        [InlineData("[\"x\", {}, \"boolean\", false]", "X;VALUE=BOOLEAN:FALSE")]
        [InlineData("[\"x\", {}, \"cal-address\", \"mailto:a@example.com\"]", "X;VALUE=CAL-ADDRESS:mailto:a@example.com")]
        [InlineData("[\"x\", {}, \"date\", \"2011-05-17\"]", "X;VALUE=DATE:20110517")]
        [InlineData("[\"x\", {}, \"date-time\", \"2011-05-17T12:00:00Z\"]", "X;VALUE=DATE-TIME:20110517T120000Z")]
        [InlineData("[\"x\", {}, \"duration\", \"-P1DT2H\"]", "X;VALUE=DURATION:-P1DT2H")]
        [InlineData("[\"x\", {}, \"float\", 1.50]", "X;VALUE=FLOAT:1.50")]
        [InlineData("[\"x\", {}, \"float\", 1.5e2]", "X;VALUE=FLOAT:150")]
        [InlineData("[\"x\", {}, \"integer\", -100]", "X;VALUE=INTEGER:-100")]
        [InlineData("[\"x\", {}, \"integer\", 1e2]", "X;VALUE=INTEGER:100")]
        [InlineData("[\"x\", {}, \"period\", [\"2011-05-17T12:00:00Z\", \"2011-05-17T13:00:00Z\"]]", "X;VALUE=PERIOD:20110517T120000Z/20110517T130000Z")]
        [InlineData("[\"x\", {}, \"period\", [\"1997-03-08T16:00:00Z\", \"P1D\"]]", "X;VALUE=PERIOD:19970308T160000Z/P1D")]
        [InlineData("[\"x\", {}, \"period\", \"1997-03-08T16:00:00Z/P1D\"]", "X;VALUE=PERIOD:19970308T160000Z/P1D")]
        [InlineData("[\"x\", {}, \"recur\", {\"freq\": \"YEARLY\", \"count\": 5, \"byday\": [\"-1SU\", \"2MO\"], \"bymonth\": 10}]",
            "X;VALUE=RECUR:FREQ=YEARLY;COUNT=5;BYDAY=-1SU,2MO;BYMONTH=10")]
        [InlineData("[\"x\", {}, \"recur\", {\"freq\": \"MONTHLY\", \"interval\": 2, \"bymonthday\": [1, 15, -1], \"until\": \"2013-10-01\"}]",
            "X;VALUE=RECUR:FREQ=MONTHLY;INTERVAL=2;BYMONTHDAY=1,15,-1;UNTIL=20131001")]
        [InlineData("[\"x\", {}, \"recur\", {\"freq\": \"DAILY\", \"until\": \"2013-10-01T07:00:00Z\", \"bysetpos\": [1]}]",
            "X;VALUE=RECUR:FREQ=DAILY;UNTIL=20131001T070000Z;BYSETPOS=1")]
        [InlineData("[\"x\", {}, \"text\", \"a,b;c\\\\d\\ne\"]", "X;VALUE=TEXT:a\\,b\\;c\\\\d\\ne")]
        [InlineData("[\"x\", {}, \"text\", \"one\", \"two\"]", "X;VALUE=TEXT:one,two")]
        [InlineData("[\"x\", {}, \"time\", \"12:30:00Z\"]", "X;VALUE=TIME:123000Z")]
        [InlineData("[\"x\", {}, \"uri\", \"http://example.com/\"]", "X;VALUE=URI:http://example.com/")]
        [InlineData("[\"x\", {}, \"utc-offset\", \"+12:45\"]", "X;VALUE=UTC-OFFSET:+1245")]
        [InlineData("[\"x\", {}, \"unknown\", \"Stenophylla;Guinea\\\\,Africa\"]", "X:Stenophylla;Guinea\\,Africa")]
        [InlineData("[\"x\", {}, \"x-mytype\", \"anything; at all\"]", "X;VALUE=X-MYTYPE:anything; at all")]
        public void ReadCalendar_Value_ConvertsToTextForm(string property, string expected)
        {
            // arrange
            var json = JournalDocument(property);

            // act
            var testResult = FormatLine(JournalProperty(ReadJcal(json, AllowInvalidValues).Single()));

            // assert
            testResult.Should().Be(expected);
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
            var testResult = ReadJcal(json).Single().Journals.Single().Properties.Skip(2).Select(FormatLine).ToList();

            // assert
            testResult.Should().Equal(
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
            testResult.Parameters.Select(parameter => parameter.Name).Should().Equal("PARTSTAT", "RSVP", "ROLE", "DELEGATED-TO", "CN");
            testResult.Parameters.Select(parameter => parameter.Values).Should().BeEquivalentTo(new[]
            {
                new[] { "ACCEPTED" }, new[] { "TRUE" }, new[] { "REQ-PARTICIPANT" }, new[] { "mailto:jdoe@example.org", "mailto:jqpublic@example.org" },
                new[] { "Smith, J" }
            }, options => options.WithStrictOrdering());
        }

        [Fact]
        public void ReadCalendar_MultiValuedProperties_JoinValues()
        {
            // arrange
            var json = JournalDocument(
                "[\"categories\", {}, \"text\", \"Meetings\", \"Work, and play\"]," +
                "[\"exdate\", {}, \"date-time\", \"2026-01-02T00:00:00Z\", \"2026-01-03T00:00:00Z\"]");

            // act
            var testResult = ReadJcal(json).Single().Journals.Single();

            // assert
            testResult.Properties.Should().ContainSingle(line => line.Name == "CATEGORIES").Which.Value.Should().Be("Meetings,Work\\, and play");
            testResult.Properties.Should().ContainSingle(line => line.Name == "EXDATE").Which.Value.Should().Be("20260102T000000Z,20260103T000000Z");
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
            var testResult = ReadJcal(json, new IcalOptions { AllowCardinalityViolations = true }).Single().Journals.Single();

            // assert
            testResult.Properties.Should().ContainSingle(line => line.Name == "GEO").Which.Value.Should().Be("37.386013;-122.082932");
            testResult.Properties.Where(line => line.Name == "REQUEST-STATUS").Select(line => line.Value).Should().Equal(
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
            var testResult = ReadJcal(json).Single();

            // assert
            testResult.Properties.Should().ContainSingle(line => line.Name == "REFRESH-INTERVAL").Which.Parameters.Should()
                .ContainSingle(parameter => parameter.Name == "VALUE").Which.Values.Should().Equal("DURATION");
        }

        [Fact]
        public void ReadCalendar_ExistingValueParameter_IsNotRepeated()
        {
            // arrange
            var json = JournalDocument("[\"dtstart\", {\"value\": \"DATE\"}, \"date\", \"2011-05-12\"]");

            // act
            var testResult = JournalProperty(ReadJcal(json).Single());

            // assert
            testResult.Parameters.Should().ContainSingle(parameter => parameter.Name == "VALUE").Which.Values.Should().Equal("DATE");
        }

        [Fact]
        public void ReadCalendars_ArrayOfCalendars_ReadsEach()
        {
            // arrange
            var expected = FixtureExpectations.JcalRoundTripModels[Example1ThenExample2Name];

            // act
            var testResult = Model(ReadJcal(ArrayOfExamples));

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        public void ReadCalendars_SmallReads_SplitTokensAcrossBlocks(int maxReadLength)
        {
            // arrange
            var expected = FixtureExpectations.JcalRoundTripModels[Example1ThenExample2Name];
            using (var reader = new JcalReader(new TrickleStream(Utf8(ArrayOfExamples), maxReadLength)))
            {
                // act
                var testResult = Model(reader.ReadCalendars().ToList());

                // assert
                testResult.Should().Equal(expected);
            }
        }

        [Fact]
        public void ReadCalendar_TokenLargerThanBuffer_GrowsBuffer()
        {
            // arrange
            using (var reader = new JcalReader(new TrickleStream(Utf8(LongDescriptionDocument), LongDescriptionReadLength)))
            {
                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Journals.Should().ContainSingle().Which.Descriptions.Should().ContainSingle().Which.Value.Should().Be(LongDescription);
            }
        }

        [Fact]
        public void ReadCalendar_ByteOrderMark_IsSkipped()
        {
            // arrange
            var expected = FixtureExpectations.JcalRoundTripModels[nameof(Example1Ical)];
            using (var reader = new JcalReader(new TrickleStream(Example1JcalWithByteOrderMark, 1)))
            {
                // act
                var testResult = Model(new[] { reader.ReadCalendar() });

                // assert
                testResult.Should().Equal(expected);
            }
        }

        [Fact]
        public void Constructor_EncodingOtherThanUtf8_ThrowsArgumentException()
        {
            // arrange
            var options = new IcalOptions { Encoding = IcalTestData.Latin1 };

            // act
            Action testAction = () => new JcalReader(new MemoryStream(), options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithMessage("*UTF-8*");
        }

        [Fact]
        public void ReadCalendar_Utf8EncodingWithByteOrderMark_Reads()
        {
            // arrange
            using (var reader = new JcalReader(new MemoryStream(Utf8(Example1Jcal)), new IcalOptions { Encoding = new UTF8Encoding(true) }))
            {
                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Should().NotBeNull();
            }
        }

        [Theory]
        [MemberData(nameof(NotValidJcal))]
        public void ReadCalendar_NotValidJcal_ThrowsWithLocation(string caseName, string json, string path, long bytePosition)
        {
            // arrange
            using (var reader = new JcalReader(new MemoryStream(Utf8(json))))
            {
                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>(caseName).Which;
                exception.Data[IcalFileException.DataItemName.JsonPath].Should().Be(path, caseName);
                exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be(bytePosition, caseName);
                exception.Data.Contains(IcalFileException.DataItemName.PhysicalLineNumber).Should().BeFalse(caseName);
            }
        }

        [Theory]
        [MemberData(nameof(NotValidJcal))]
        public void ReadCalendar_AfterNotValidJcal_ThrowsInvalidOperation(string caseName, string json, string path, long bytePosition)
        {
            // arrange
            _ = (path, bytePosition);
            using (var reader = new JcalReader(new MemoryStream(Utf8(json))))
            {
                Record.Exception(() => reader.ReadCalendar());

                // act
                Action testAction = () => reader.ReadCalendar();

                // assert
                testAction.Should().Throw<InvalidOperationException>(caseName);
            }
        }

        [Theory]
        [MemberData(nameof(NotValidJson))]
        public void ReadCalendar_NotValidJson_WrapsJsonException(string json)
        {
            // arrange
            using (var reader = new JcalReader(new TrickleStream(Utf8(json), 3)))
            {
                // act
                Action testAction = () => reader.ReadCalendars().ToList();

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().WithMessage("The data is not valid JSON*").Which;
                exception.InnerException.Should().BeAssignableTo<System.Text.Json.JsonException>();
                exception.Data.Contains(IcalFileException.DataItemName.BytePosition).Should().BeTrue();
                exception.Data.Contains(IcalFileException.DataItemName.LineNumber).Should().BeTrue();
            }
        }

        [Fact]
        public void ReadCalendar_NotValidJson_GivesLineAndPosition()
        {
            // act
            Action testAction = () => ReadJcal(NotValidJsonDocument);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
            exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be(4L);
            exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be(NotValidJsonBytePosition);
        }

        [Fact]
        public void ReadCalendar_StringNotValidUtf8_Throws()
        {
            // arrange: 0xFF never appears in UTF-8 (RFC 3629 §3).
            var bytes = Utf8(JournalDocument("[\"summary\",{},\"text\",\"a#b\"]"));
            bytes[Array.IndexOf(bytes, (byte)'#')] = 0xFF;
            var reader = new JcalReader(new MemoryStream(bytes));

            // act
            Action testAction = () => reader.ReadCalendar();

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void ReadCalendar_LoneSurrogateEscape_Throws()
        {
            // arrange
            var json = JournalDocument("[\"summary\",{},\"text\",\"a\\uD800b\"]");

            // act
            Action testAction = () => ReadJcal(json);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void ReadCalendar_EmptyArray_ThrowsNoCalendar()
        {
            // act
            Action testAction = () => ReadJcal("[]");

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*no VCALENDAR*");
        }

        [Fact]
        public void ReadCalendar_DocumentEndsInsideTheCalendar_Throws()
        {
            // act
            Action testAction = () => ReadJcal("[\"vcalendar\"");

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void ReadCalendar_UnsupportedVersion_ThrowsWithLocation()
        {
            // act
            Action testAction = () => ReadJcal(UnsupportedVersionDocument);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.UnsupportedVersion);
            exception.Data[IcalFileException.DataItemName.JsonPath].Should().Be("$[1][0]");
            exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be(UnsupportedVersionBytePosition);
            exception.Data.Contains(IcalFileException.DataItemName.LineNumber).Should().BeFalse();
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
        public void ReadCalendar_AfterTheLastCalendar_ReturnsNull()
        {
            // arrange
            using (var reader = CreateReaderOfNonConformantThenExample1())
            {
                Record.Exception(() => reader.ReadCalendar());
                reader.ReadCalendar();
                reader.ReadCalendar();

                // act
                var testResult = reader.ReadCalendar();

                // assert
                testResult.Should().BeNull();
            }
        }

        [Fact]
        public void ReadCalendar_InvalidValueByDefault_Throws()
        {
            // arrange
            var json = JournalDocument("[\"dtstart\",{},\"date-time\",\"2026-02-30T00:00:00\"]");

            // act
            Action testAction = () => ReadJcal(json);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(IcalDiagnosticKind.InvalidValue);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be("DTSTART");
        }

        [Fact]
        public void ReadCalendar_InvalidValueAllowed_KeepsTheValue()
        {
            // arrange
            var json = JournalDocument("[\"dtstart\",{},\"date-time\",\"2026-02-30T00:00:00\"]");

            // act
            var testResult = ReadJcal(json, AllowInvalidValues);

            // assert
            JournalProperty(testResult.Single()).Value.Should().Be(ExampleNotValidDateTime);
        }

        [Fact]
        public void ReadCalendar_ValueAsStringOfOtherTypeByDefault_Throws()
        {
            // arrange: a string is taken for any type, so a value that does not follow its type reaches the conformance check.
            var json = JournalDocument("[\"percent-complete\",{},\"integer\",\"lots\"]");

            // act
            Action testAction = () => ReadJcal(json);

            // assert
            testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should()
                .Be(IcalDiagnosticKind.InvalidValue);
        }

        [Fact]
        public void ReadCalendar_ValueAsStringOfOtherTypeAllowed_KeepsTheString()
        {
            // arrange
            var json = JournalDocument("[\"percent-complete\",{},\"integer\",\"lots\"]");

            // act
            var testResult = ReadJcal(json, AllowInvalidValues);

            // assert
            JournalProperty(testResult.Single()).Value.Should().Be("lots");
        }

        [Fact]
        public void ReadCalendar_TextIcalendarCannotHold_Throws()
        {
            // arrange
            var json = JournalDocument("[\"summary\",{},\"text\",\"a\\u007Fb\"]");

            // act
            Action testAction = () => ReadJcal(json);

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*cannot hold*");
        }

        [Theory]
        [MemberData(nameof(NullConstructorArguments), DisableDiscoveryEnumeration = true)]
        public void Constructor_NullArgument_ThrowsArgumentNull(Action call, string parameterName)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(parameterName);
        }

        [Fact]
        public void Constructor_WriteOnlyStream_ThrowsArgumentException()
        {
            // arrange
            var stream = new WriteOnlyStream();

            // act
            Action testAction = () => new JcalReader(stream);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [MemberData(nameof(DisposedReaderMembers), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposed(Func<JcalReader, object> member)
        {
            // arrange
            var reader = new JcalReader(new MemoryStream(Utf8(Example1Jcal)));
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
            var reader = new JcalReader(new MemoryStream(Utf8(Example1Jcal)));
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
            var stream = new TrickleStream(Utf8(Example1Jcal), 100);
            var reader = new JcalReader(stream);
            reader.ReadCalendar();

            // act
            reader.Dispose();

            // assert
            stream.IsDisposed.Should().BeFalse();
        }

        /// <summary>
        /// Builds a reader of an array of a calendar whose VJOURNAL has no UID, then the first example calendar.
        /// </summary>
        private static JcalReader CreateReaderOfNonConformantThenExample1()
        {
            var bad = Document(
                "[\"prodid\",{},\"text\",\"-//Test//Test//EN\"],[\"version\",{},\"text\",\"2.0\"]",
                "[\"vjournal\",[[\"dtstamp\",{},\"date-time\",\"2026-01-01T00:00:00Z\"]],[]]");
            return new JcalReader(new MemoryStream(Utf8("[" + bad + "," + Example1Jcal + "]")));
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
