using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests that calendars converted between <c>.ics</c> and xCal keep their properties and values.
    /// </summary>
    public class XcalRoundTripTests
    {
        /// <summary>
        /// Unknown and <c>X-</c> properties and parameters, with and without a <c>VALUE</c> parameter, in every value type.
        /// </summary>
        internal static readonly string Extensions = EventCalendar(
            "X-PROPERTY:20110512T120000Z",
            "X-RAW:a\\,b;c:d",
            "X-TEXT;VALUE=TEXT:one\\, two,three\\nfour",
            "X-BINARY;ENCODING=BASE64;VALUE=BINARY:SGVsbG8=",
            "X-BOOLEAN;VALUE=BOOLEAN:FALSE",
            "X-CAL-ADDRESS;VALUE=CAL-ADDRESS:mailto:a@example.com",
            "X-DATES;VALUE=DATE:20260101,20260102",
            "X-DATE-TIME;VALUE=DATE-TIME:20260101T090000Z",
            "X-DURATION;VALUE=DURATION:-P1W",
            "X-FLOAT;VALUE=FLOAT:-1.250",
            "X-INTEGER;VALUE=INTEGER:+7",
            "X-PERIOD;VALUE=PERIOD:20260101T090000Z/PT1H",
            "X-RECUR;VALUE=RECUR:FREQ=MONTHLY;BYMONTHDAY=1,-1;COUNT=3",
            "X-TIME;VALUE=TIME:090000",
            "X-URI;VALUE=URI:http://example.com/a?b=c",
            "X-UTC-OFFSET;VALUE=UTC-OFFSET:-013015",
            "X-OTHER-TYPE;VALUE=X-NAME:anything",
            "DTEND;X-PARAM=PT30M;X-LIST=a,\"b,c\":20260101T100000Z",
            "SUMMARY;LANGUAGE=en;X-QUOTED=\"a;b\":Caret ^ and quote \" kept",
            "IANA-PROP;VALUE=TEXT:iana");

        private static readonly IReadOnlyDictionary<string, (string Text, IcalOptions Options)> Calendars =
            new Dictionary<string, (string Text, IcalOptions Options)>(CalendarFixtures.All.ToDictionary(pair => pair.Key, pair => pair.Value))
            {
                [nameof(Example1Ical)] = (Example1Ical, null),
                [nameof(Example2Ical)] = (Example2Ical, null),
                [nameof(Extensions)] = (Extensions, null),
            };

        public static TheoryData<string> CalendarNames()
        {
            var data = CalendarFixtures.AllNames();
            data.Add(nameof(Example1Ical));
            data.Add(nameof(Example2Ical));
            data.Add(nameof(Extensions));
            return data;
        }

        internal static (string Text, IcalOptions Options) GetCalendar(string name) => Calendars[name];

        [Theory]
        [MemberData(nameof(CalendarNames))]
        public void ReadXcal_CalendarsWrittenAsXcal_LeavesModelUnchanged(string calendarName)
        {
            // arrange
            var (text, options) = GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.XcalRoundTripModels[calendarName];

            // act
            var testResult = Model(ReadXcal(WriteXcal(calendars, options), options), options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(CalendarNames))]
        public void ReadCalendars_XcalCalendarsWrittenAsIcal_LeavesModelUnchanged(string calendarName)
        {
            // arrange
            var (text, options) = GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.XcalRoundTripModels[calendarName];

            // act
            var testResult = Model(ReadCalendars(WriteCalendars(ReadXcal(WriteXcal(calendars, options), options), options), options), options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(XcalWriterTests.RfcExamples), MemberType = typeof(XcalWriterTests))]
        public void WriteXcal_RfcExampleThroughIcal_IsTheRfcXcalForm(string ical, string xcal)
        {
            // arrange
            var calendars = ReadCalendars(ical);
            var expected = XcalWriterTests.Normalize(XDocument.Parse(xcal));

            // act
            var testResult = XcalWriterTests.Normalize(XDocument.Parse(WriteXcal(ReadCalendars(WriteCalendars(ReadXcal(WriteXcal(calendars)))))));

            // assert: the same XML, ignoring formatting.
            testResult.Should().Be(expected);
        }

        public static TheoryData<string> XcalConformanceCases() => ConformanceCases.XcalAndJcalCases();

        [Theory]
        [MemberData(nameof(XcalConformanceCases))]
        public void ReadXcal_NonConformantCalendarWithDefaults_ThrowsWithTheDiagnostic(string caseName)
        {
            // arrange: write the calendar with the one accommodation it needs, then read it with the defaults.
            var testCase = ConformanceCases.All[caseName];
            var xcal = WriteXcal(ReadCalendars(testCase.Text, testCase.AcceptingOptions), testCase.AcceptingOptions);

            // act
            Action testAction = () => ReadXcal(xcal);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>(caseName).Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind, caseName);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath, caseName);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(testCase.PropertyName, caseName);
        }

        [Theory]
        [MemberData(nameof(XcalConformanceCases))]
        public void ReadXcal_NonConformantCalendarWithItsOption_ReadsCalendar(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];
            var xcal = WriteXcal(ReadCalendars(testCase.Text, testCase.AcceptingOptions), testCase.AcceptingOptions);

            // act
            var testResult = ReadXcal(xcal, testCase.AcceptingOptions);

            // assert
            testResult.Should().ContainSingle(caseName);
        }
    }
}
