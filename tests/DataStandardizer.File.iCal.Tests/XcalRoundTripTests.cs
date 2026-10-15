using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Tests that calendars converted between <c>.ics</c> and xCal keep their properties and values.
/// </summary>
public class XcalRoundTripTests
{
    public static TheoryData<string> CalendarNames()
    {
        var data = CalendarFixtures.AllNames();
        data.Add(nameof(Example1Ical));
        data.Add(nameof(Example2Ical));
        data.Add(nameof(Extensions));
        return data;
    }

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
    public void IcalToXcalToIcal_LeavesModelUnchanged(string calendarName)
    {
        // arrange
        var (text, options) = GetCalendar(calendarName);
        var calendars = ReadCalendars(text, options);

        // act
        var testResult = ReadXcal(WriteXcal(calendars, options), options);

        // assert
        Model(testResult, options).Should().Equal(Model(calendars, options));
    }

    [Theory]
    [MemberData(nameof(CalendarNames))]
    public void XcalToIcalToXcal_LeavesModelUnchanged(string calendarName)
    {
        // arrange
        var (text, options) = GetCalendar(calendarName);
        var xcal = WriteXcal(ReadCalendars(text, options), options);
        var calendars = ReadXcal(xcal, options);

        // act
        var testResult = ReadCalendars(WriteCalendars(calendars, options), options);

        // assert
        Model(testResult, options).Should().Equal(Model(calendars, options));
        WriteXcal(testResult, options).Should().Be(xcal);
    }

    [Fact]
    public void IcalToXcalToIcal_ExtensionProperties_KeepTheirValues()
    {
        // arrange
        var calendar = ReadCalendar(Extensions);

        // act
        var testResult = ReadXcal(WriteXcal([calendar])).Single().Events.Single();

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
    /// The conformance cases that xCal can hold.  Three cannot be written in xCal: the writer always writes VERSION, the value element of
    /// REFRESH-INTERVAL gives it the VALUE parameter it lacked, and a binary element gives its property the ENCODING parameter it lacked.
    /// </summary>
    public static TheoryData<string> XcalConformanceCases()
    {
        var data = new TheoryData<string>();
        foreach (var name in ConformanceCases.All.Keys.Where(name => name is not ("MissingVersion" or "RefreshIntervalWithoutValue" or "BinaryWithoutEncoding")))
        {
            data.Add(name);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(XcalConformanceCases))]
    public void ReadXcal_NonConformantCalendar_RejectedAsIcalReaderRejectsIt(string caseName)
    {
        // arrange: write the calendar with the one accommodation it needs, then read it with the defaults.
        var testCase = ConformanceCases.All[caseName];
        var options = ConformanceCases.OptionsAccepting(testCase.Kind);
        var xcal = WriteXcal(ReadCalendars(testCase.Text, options), options);
        var icalException = FluentActions.Invoking(() => ReadCalendars(testCase.Text)).Should().Throw<IcalFileException>().Which;

        // act
        var act = () => ReadXcal(xcal);

        // assert
        var exception = act.Should().Throw<IcalFileException>(caseName).Which;
        exception.Message.Should().Be(icalException.Message, caseName);
        exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind, caseName);
        exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath, caseName);
        ReadXcal(xcal, options).Should().ContainSingle(caseName);
    }
}
