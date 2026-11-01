using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests that calendars converted between <c>.ics</c>, jCal and xCal keep their properties and values.
    /// </summary>
    public class JcalRoundTripTests
    {
        /// <summary>
        /// The extension properties of <see cref="XcalRoundTripTests"/>, with an INTEGER that JSON can hold as it is: a JSON number has no plus
        /// sign, so <c>+7</c> comes back from jCal as <c>7</c>.
        /// </summary>
        internal static readonly string Extensions = XcalRoundTripTests.Extensions.Replace("X-INTEGER;VALUE=INTEGER:+7", "X-INTEGER;VALUE=INTEGER:-7");

        // ref. RFC 7265 §5: an unknown property, whose value would be changed if it were taken as TEXT.
        private const string CoffeeDataName = "X-COFFEE-DATA";
        private const string CoffeeDataValue = "Stenophylla;Guinea\\,Africa";
        private const string CoffeeDataJcal = "[\"x-coffee-data\",{},\"unknown\",\"Stenophylla;Guinea\\\\,Africa\"]";
        private const string ComplaintDeadlineName = "X-COMPLAINT-DEADLINE";
        private const string ComplaintDeadlineValue = "20110512T120000Z";

        private static readonly string UnknownPropertiesCalendar =
            EventCalendar(CoffeeDataName + ":" + CoffeeDataValue, ComplaintDeadlineName + ":" + ComplaintDeadlineValue);

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

        // ref. RFC 7265 Appendix B: each example in iCalendar form and in jCal form, with the PERIOD that RFC 7265 §3.6.9 gives as an array.
        public static TheoryData<string, string> RfcExamples => new TheoryData<string, string>
        {
            { Example1Ical, Example1Jcal },
            { Example2Ical, Example2JcalWithPeriodArray },
        };

        [Theory]
        [MemberData(nameof(CalendarNames))]
        public void ReadJcal_CalendarsWrittenAsJcal_LeavesModelUnchanged(string calendarName)
        {
            // arrange
            var (text, options) = GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.JcalRoundTripModels[calendarName];

            // act
            var testResult = Model(ReadJcal(WriteJcal(calendars, options), options), options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(CalendarNames))]
        public void ReadCalendars_JcalCalendarsWrittenAsIcal_LeavesModelUnchanged(string calendarName)
        {
            // arrange
            var (text, options) = GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.JcalRoundTripModels[calendarName];

            // act
            var testResult = Model(ReadCalendars(WriteCalendars(ReadJcal(WriteJcal(calendars, options), options), options), options), options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(RfcExamples))]
        public void WriteJcal_RfcExampleThroughIcal_IsTheRfcJcalForm(string ical, string jcal)
        {
            // arrange
            var calendars = ReadCalendars(ical);
            var expected = JsonNode.Parse(jcal);

            // act
            var testResult = JsonNode.Parse(WriteJcal(ReadCalendars(WriteCalendars(ReadJcal(WriteJcal(calendars))))));

            // assert: the same JSON, ignoring formatting.
            JsonNode.DeepEquals(testResult, expected).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(CalendarNames))]
        public void ReadXcal_JcalCalendarsWrittenAsXcal_LeavesModelUnchanged(string calendarName)
        {
            // arrange
            var (text, options) = GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.JcalRoundTripModels[calendarName];

            // act
            var testResult = Model(XcalTestData.ReadXcal(XcalTestData.WriteXcal(ReadJcal(WriteJcal(calendars, options), options), options), options), options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(RfcExamples))]
        public void WriteJcal_RfcExampleThroughXcal_IsTheRfcJcalForm(string ical, string jcal)
        {
            // arrange
            var calendars = ReadCalendars(ical);
            var expected = JsonNode.Parse(jcal);

            // act
            var testResult = JsonNode.Parse(WriteJcal(XcalTestData.ReadXcal(XcalTestData.WriteXcal(ReadJcal(WriteJcal(calendars))))));

            // assert: the same JSON, ignoring formatting.
            JsonNode.DeepEquals(testResult, expected).Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(CalendarNames))]
        public void ReadJcal_XcalCalendarsWrittenAsJcal_LeavesModelUnchanged(string calendarName)
        {
            // arrange
            var (text, options) = GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.JcalRoundTripModels[calendarName];

            // act
            var testResult = Model(ReadJcal(WriteJcal(XcalTestData.ReadXcal(XcalTestData.WriteXcal(calendars, options), options), options), options), options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(XcalWriterTests.RfcExamples), MemberType = typeof(XcalWriterTests))]
        public void WriteXcal_RfcExampleThroughJcal_IsTheRfcXcalForm(string ical, string xcal)
        {
            // arrange
            var calendars = ReadCalendars(ical);
            var expected = XcalWriterTests.Normalize(XDocument.Parse(xcal));

            // act
            var testResult = XcalWriterTests.Normalize(XDocument.Parse(XcalTestData.WriteXcal(ReadJcal(WriteJcal(XcalTestData.ReadXcal(
                XcalTestData.WriteXcal(calendars)))))));

            // assert: the same XML, ignoring formatting.
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ReadJcal_RfcExample1_GivesTheModelOfTheIcalForm()
        {
            // arrange
            var expected = FixtureExpectations.JcalRoundTripModels[nameof(Example1Ical)];

            // act
            var testResult = Model(ReadJcal(Example1Jcal));

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void ReadXcal_RfcExample1_GivesTheModelOfTheIcalForm()
        {
            // arrange
            var expected = FixtureExpectations.JcalRoundTripModels[nameof(Example1Ical)];

            // act
            var testResult = Model(XcalTestData.ReadXcal(XcalTestData.Example1Xcal));

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void ReadJcal_RfcExample2_GivesTheModelOfTheIcalForm()
        {
            // arrange
            var expected = FixtureExpectations.JcalRoundTripModels[nameof(Example2Ical)];

            // act
            var testResult = Model(ReadJcal(Example2Jcal));

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void WriteJcal_UnknownProperty_WritesItAsUnknownWithItsRawText()
        {
            // arrange
            var calendar = ReadCalendar(UnknownPropertiesCalendar);

            // act
            var testResult = WriteJcal(new[] { calendar });

            // assert
            testResult.Should().Contain(CoffeeDataJcal);
        }

        [Fact]
        public void ReadJcal_UnknownPropertyWrittenAsJcal_KeepsItsRawText()
        {
            // arrange
            var calendar = ReadCalendar(UnknownPropertiesCalendar);

            // act
            var testResult = ReadJcal(WriteJcal(new[] { calendar })).Single().Events.Single();

            // assert
            testResult.Properties.Should().ContainSingle(line => line.Name == CoffeeDataName).Which.Value.Should().Be(CoffeeDataValue);
            testResult.Properties.Should().ContainSingle(line => line.Name == CoffeeDataName).Which.Parameters.Should().BeEmpty();
            testResult.Properties.Should().ContainSingle(line => line.Name == ComplaintDeadlineName).Which.Value.Should().Be(ComplaintDeadlineValue);
        }

        [Fact]
        public void WriteCalendars_UnknownPropertyThroughJcal_WritesTheOriginalText()
        {
            // arrange
            var calendar = ReadCalendar(UnknownPropertiesCalendar);

            // act
            var testResult = WriteCalendars(ReadJcal(WriteJcal(new[] { calendar })));

            // assert
            testResult.Should().Be(UnknownPropertiesCalendar);
        }

        public static TheoryData<string> JcalConformanceCases() => ConformanceCases.XcalAndJcalCases();

        [Theory]
        [MemberData(nameof(JcalConformanceCases))]
        public void ReadJcal_NonConformantCalendarWithDefaults_ThrowsAsIcalReaderDoes(string caseName)
        {
            // arrange: write the calendar with the one accommodation it needs, then read it with the defaults.
            var testCase = ConformanceCases.All[caseName];
            var jcal = WriteJcal(ReadCalendars(testCase.Text, testCase.AcceptingOptions), testCase.AcceptingOptions);

            // act
            Action testAction = () => ReadJcal(jcal);

            // assert
            var exception = testAction.Should().Throw<IcalFileException>(caseName).Which;
            exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind, caseName);
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath, caseName);
            exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(testCase.PropertyName, caseName);
        }

        [Theory]
        [MemberData(nameof(JcalConformanceCases))]
        public void ReadJcal_NonConformantCalendarWithItsOption_ReadsCalendar(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];
            var jcal = WriteJcal(ReadCalendars(testCase.Text, testCase.AcceptingOptions), testCase.AcceptingOptions);

            // act
            var testResult = ReadJcal(jcal, testCase.AcceptingOptions);

            // assert
            testResult.Should().ContainSingle(caseName);
        }
    }
}
