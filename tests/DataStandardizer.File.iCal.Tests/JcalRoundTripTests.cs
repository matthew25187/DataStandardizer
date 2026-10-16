using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests;

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

    public static TheoryData<string> CalendarNames()
    {
        var data = CalendarFixtures.AllNames();
        data.Add(nameof(Example1Ical));
        data.Add(nameof(Example2Ical));
        data.Add(nameof(Extensions));
        return data;
    }

    internal static (string Text, IcalOptions? Options) GetCalendar(string name)
    {
        switch (name)
        {
            case nameof(Example1Ical):
                return (Example1Ical, null);
            case nameof(Example2Ical):
                return (Example2Ical, null);
            case nameof(Extensions):
                return (Extensions, null);
            default:
                var (text, options) = CalendarFixtures.All[name];
                return (text, options);
        }
    }

    [Theory]
    [MemberData(nameof(CalendarNames))]
    public void IcalToJcalToIcal_LeavesModelUnchanged(string calendarName)
    {
        // arrange
        var (text, options) = GetCalendar(calendarName);
        var calendars = ReadCalendars(text, options);

        // act
        var testResult = ReadJcal(WriteJcal(calendars, options), options);

        // assert
        Model(testResult, options).Should().Equal(Model(calendars, options));
    }

    [Theory]
    [MemberData(nameof(CalendarNames))]
    public void JcalToIcalToJcal_LeavesModelAndJsonUnchanged(string calendarName)
    {
        // arrange
        var (text, options) = GetCalendar(calendarName);
        var jcal = WriteJcal(ReadCalendars(text, options), options);
        var calendars = ReadJcal(jcal, options);

        // act
        var testResult = ReadCalendars(WriteCalendars(calendars, options), options);

        // assert
        Model(testResult, options).Should().Equal(Model(calendars, options));
        WriteJcal(testResult, options).Should().Be(jcal);
    }

    [Theory]
    [MemberData(nameof(CalendarNames))]
    public void JcalToXcalToJcal_LeavesModelAndJsonUnchanged(string calendarName)
    {
        // arrange
        var (text, options) = GetCalendar(calendarName);
        var jcal = WriteJcal(ReadCalendars(text, options), options);
        var calendars = ReadJcal(jcal, options);

        // act
        var testResult = XcalTestData.ReadXcal(XcalTestData.WriteXcal(calendars, options), options);

        // assert
        Model(testResult, options).Should().Equal(Model(calendars, options));
        WriteJcal(testResult, options).Should().Be(jcal);
    }

    [Theory]
    [MemberData(nameof(CalendarNames))]
    public void XcalToJcalToXcal_LeavesModelAndXmlUnchanged(string calendarName)
    {
        // arrange
        var (text, options) = GetCalendar(calendarName);
        var xcal = XcalTestData.WriteXcal(ReadCalendars(text, options), options);
        var calendars = XcalTestData.ReadXcal(xcal, options);

        // act
        var testResult = ReadJcal(WriteJcal(calendars, options), options);

        // assert
        Model(testResult, options).Should().Equal(Model(calendars, options));
        XcalTestData.WriteXcal(testResult, options).Should().Be(xcal);
    }

    [Fact]
    public void RfcExamples_ReadFromEachFormat_GiveTheSameModel()
    {
        // assert
        Model(ReadJcal(Example1Jcal)).Should().Equal(Model(XcalTestData.ReadXcal(XcalTestData.Example1Xcal)))
            .And.Equal(Model(ReadCalendars(Example1Ical)));
        Model(ReadJcal(Example2Jcal)).Should().Equal(Model(ReadCalendars(Example2Ical)));
    }

    [Fact]
    public void IcalToJcalToIcal_UnknownProperty_KeepsItsRawText()
    {
        // arrange: RFC 7265 §5; the value would be changed if it were taken as TEXT.
        var calendar = ReadCalendar(EventCalendar("X-COFFEE-DATA:Stenophylla;Guinea\\,Africa", "X-COMPLAINT-DEADLINE:20110512T120000Z"));

        // act
        var jcal = WriteJcal([calendar]);
        var testResult = ReadJcal(jcal).Single().Events.Single();

        // assert
        jcal.Should().Contain("[\"x-coffee-data\",{},\"unknown\",\"Stenophylla;Guinea\\\\,Africa\"]");
        testResult.GetProperty("X-COFFEE-DATA")!.Value.Should().Be("Stenophylla;Guinea\\,Africa");
        testResult.GetProperty("X-COFFEE-DATA")!.Parameters.Should().BeEmpty();
        testResult.GetProperty("X-COMPLAINT-DEADLINE")!.Value.Should().Be("20110512T120000Z");
        WriteCalendars([ReadJcal(jcal).Single()]).Should().Be(WriteCalendars([calendar]));
    }

    [Fact]
    public void IcalToJcalToIcal_ExtensionProperties_KeepTheirValues()
    {
        // arrange
        var calendar = ReadCalendar(Extensions);

        // act
        var testResult = ReadJcal(WriteJcal([calendar])).Single().Events.Single();

        // assert
        var expected = calendar.Events.Single();
        foreach (var property in expected.Properties)
        {
            var converted = testResult.GetProperties(property.Name).Single(line => line.Value == property.Value);
            converted.Parameters.Select(parameter => (parameter.Name, string.Join("|", parameter.Values)))
                .Should().BeEquivalentTo(property.Parameters.Select(parameter => (parameter.Name, string.Join("|", parameter.Values))), property.Name);
        }

        testResult.Properties.Should().HaveCount(expected.Properties.Count);
    }

    /// <summary>
    /// The conformance cases that jCal can hold; the same three as xCal cannot: the writer always writes VERSION, the type identifier of
    /// REFRESH-INTERVAL gives it the VALUE parameter it lacked, and a binary value gives its property the ENCODING parameter it lacked.
    /// </summary>
    public static TheoryData<string> JcalConformanceCases() => XcalRoundTripTests.XcalConformanceCases();

    [Theory]
    [MemberData(nameof(JcalConformanceCases))]
    public void ReadJcal_NonConformantCalendar_RejectedAsIcalReaderRejectsIt(string caseName)
    {
        // arrange: write the calendar with the one accommodation it needs, then read it with the defaults.
        var testCase = ConformanceCases.All[caseName];
        var options = ConformanceCases.OptionsAccepting(testCase.Kind);
        var jcal = WriteJcal(ReadCalendars(testCase.Text, options), options);
        var icalException = FluentActions.Invoking(() => ReadCalendars(testCase.Text)).Should().Throw<IcalFileException>().Which;

        // act
        var act = () => ReadJcal(jcal);

        // assert
        var exception = act.Should().Throw<IcalFileException>(caseName).Which;
        exception.Message.Should().Be(icalException.Message, caseName);
        exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind, caseName);
        exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath, caseName);
        ReadJcal(jcal, options).Should().ContainSingle(caseName);
    }
}
