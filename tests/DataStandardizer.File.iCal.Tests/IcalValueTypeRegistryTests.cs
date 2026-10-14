using System.Text;
using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class IcalValueTypeRegistryTests
{
    [Theory]
    [InlineData("DTSTART", IcalValueType.DateTime)]
    [InlineData("dtstart", IcalValueType.DateTime)]
    [InlineData("SUMMARY", IcalValueType.Text)]
    [InlineData("ATTACH", IcalValueType.Uri)]
    [InlineData("ATTENDEE", IcalValueType.CalAddress)]
    [InlineData("GEO", IcalValueType.Float)]
    [InlineData("PRIORITY", IcalValueType.Integer)]
    [InlineData("DURATION", IcalValueType.Duration)]
    [InlineData("FREEBUSY", IcalValueType.Period)]
    [InlineData("RRULE", IcalValueType.Recur)]
    [InlineData("TZOFFSETFROM", IcalValueType.UtcOffset)]
    [InlineData("TRIGGER", IcalValueType.Duration)]
    [InlineData("COLOR", IcalValueType.Text)]
    [InlineData("REFRESH-INTERVAL", IcalValueType.Duration)]
    [InlineData("CONFERENCE", IcalValueType.Uri)]
    [InlineData("X-WR-CALNAME", IcalValueType.Unknown)]
    [InlineData("NEW-IANA-PROPERTY", IcalValueType.Unknown)]
    [InlineData("", IcalValueType.Unknown)]
    public void GetDefaultValueType_Property_ReturnsDefault(string propertyName, IcalValueType expected)
    {
        // act
        var testResult = IcalValueTypeRegistry.GetDefaultValueType(propertyName);

        // assert
        testResult.Should().Be(expected);
        IcalValueTypeRegistry.IsKnownProperty(propertyName).Should().Be(expected != IcalValueType.Unknown);
    }

    [Theory]
    [InlineData("DTSTART", "DATE", IcalValueType.Date)]
    [InlineData("DTSTART", "date", IcalValueType.Date)]
    [InlineData("ATTACH", "BINARY", IcalValueType.Binary)]
    [InlineData("RDATE", "PERIOD", IcalValueType.Period)]
    [InlineData("TRIGGER", "DATE-TIME", IcalValueType.DateTime)]
    [InlineData("DTSTART", "X-CUSTOM", IcalValueType.Unknown)]
    [InlineData("DTSTART", "", IcalValueType.Unknown)]
    [InlineData("X-PROP", "DATE", IcalValueType.Unknown)]
    [InlineData("DTSTART", "INTEGER", IcalValueType.Integer)]
    public void GetValueType_ValueParameter_Overrides(string propertyName, string valueParameter, IcalValueType expected)
    {
        // act
        var testResult = IcalValueTypeRegistry.GetValueType(propertyName, valueParameter);

        // assert
        testResult.Should().Be(expected);
    }

    [Theory]
    [InlineData("DTSTART", IcalValueType.DateTime, true)]
    [InlineData("DTSTART", IcalValueType.Date, true)]
    [InlineData("DTSTART", IcalValueType.Integer, false)]
    [InlineData("RDATE", IcalValueType.Period, true)]
    [InlineData("EXDATE", IcalValueType.Period, false)]
    [InlineData("IMAGE", IcalValueType.Binary, true)]
    [InlineData("X-PROP", IcalValueType.Text, false)]
    public void IsAllowedValueType_Property_ReturnsWhetherAllowed(string propertyName, IcalValueType valueType, bool expected)
    {
        // act
        var testResult = IcalValueTypeRegistry.IsAllowedValueType(propertyName, valueType);

        // assert
        testResult.Should().Be(expected);
    }

    [Fact]
    public void GetValueType_ContentLine_UsesValueParameter()
    {
        // arrange
        var withValue = new IcalContentLine("DTSTART", [new IcalContentLineParameter("value", "DATE")], "19970714");
        var withoutValue = new IcalContentLine("DTSTART", "19970714T133000");
        var unknown = new IcalContentLine("X-PROP", [new IcalContentLineParameter("VALUE", "DATE")], "19970714");

        // assert
        IcalValueTypeRegistry.GetValueType(withValue).Should().Be(IcalValueType.Date);
        IcalValueTypeRegistry.GetValueType(withoutValue).Should().Be(IcalValueType.DateTime);
        IcalValueTypeRegistry.GetValueType(unknown).Should().Be(IcalValueType.Unknown);
    }

    [Fact]
    public void GetValueType_View_UsesValueParameter()
    {
        // arrange
        var text = "DTSTART;VALUE=DATE:19970714\r\nDTSTART:19970714T133000\r\nX-PROP;VALUE=DATE:19970714\r\n";
        using var reader = new IcalContentLineReader(new MemoryStream(Encoding.UTF8.GetBytes(text)));
        var testResult = new List<IcalValueType>();

        // act
        while (reader.ReadContentLine())
        {
            testResult.Add(IcalValueTypeRegistry.GetValueType(reader.CurrentLine));
        }

        // assert
        testResult.Should().Equal(IcalValueType.Date, IcalValueType.DateTime, IcalValueType.Unknown);
    }

    [Fact]
    public void ValueTypeNames_RoundTrip()
    {
        // arrange
        var valueTypes = Enum.GetValues<IcalValueType>().Where(valueType => valueType != IcalValueType.Unknown);

        // act / assert
        foreach (var valueType in valueTypes)
        {
            var name = IcalValueTypeRegistry.GetValueTypeName(valueType);
            IcalValueTypeRegistry.TryParseValueTypeName(name, out var parsed).Should().BeTrue();
            parsed.Should().Be(valueType);
        }

        IcalValueTypeRegistry.GetValueTypeName(IcalValueType.UtcOffset).Should().Be("UTC-OFFSET");
        IcalValueTypeRegistry.GetValueTypeName(IcalValueType.CalAddress).Should().Be("CAL-ADDRESS");
        FluentActions.Invoking(() => IcalValueTypeRegistry.GetValueTypeName(IcalValueType.Unknown)).Should().Throw<ArgumentOutOfRangeException>();
    }
}
