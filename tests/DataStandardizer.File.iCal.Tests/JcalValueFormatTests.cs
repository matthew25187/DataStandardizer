using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Tests of the <see cref="IcalValueFormat.Json"/> lexical forms of the value types (RFC 7265 §3.6).
/// </summary>
public class JcalValueFormatTests
{
    [Theory]
    [InlineData("2011-05-17", "20110517")]
    [InlineData("0001-01-01", "00010101")]
    public void Date_Json_RoundTrips(string json, string text)
    {
        // act
        var testResult = IcalDate.Parse(json, IcalValueFormat.Json);

        // assert
        testResult.Should().Be(IcalDate.Parse(text, IcalValueFormat.Text));
        testResult.ToString(IcalValueFormat.Json).Should().Be(json);
    }

    [Theory]
    [InlineData("20110517")]
    [InlineData("2011-05")]
    [InlineData("2011-02-30")]
    [InlineData(" 2011-05-17")]
    public void Date_JsonNotValid_IsRejected(string json)
    {
        // assert
        IcalDate.TryParse(json, IcalValueFormat.Json, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("2012-10-17T12:00:00", "20121017T120000")]
    [InlineData("2012-10-17T12:00:00Z", "20121017T120000Z")]
    public void DateTime_Json_RoundTrips(string json, string text)
    {
        // act
        var testResult = IcalDateTime.Parse(json, IcalValueFormat.Json);

        // assert
        testResult.Should().Be(IcalDateTime.Parse(text, IcalValueFormat.Text));
        testResult.ToString(IcalValueFormat.Json).Should().Be(json);
        IcalDateTime.Parse(json.TrimEnd('Z'), "Europe/Berlin", IcalValueFormat.Json).TimeZoneId.Should().Be("Europe/Berlin");
    }

    [Theory]
    [InlineData("12:30:00", "123000")]
    [InlineData("12:30:00Z", "123000Z")]
    public void Time_Json_RoundTrips(string json, string text)
    {
        // act
        var testResult = IcalTime.Parse(json, IcalValueFormat.Json);

        // assert
        testResult.Should().Be(IcalTime.Parse(text, IcalValueFormat.Text));
        testResult.ToString(IcalValueFormat.Json).Should().Be(json);
    }

    [Theory]
    [InlineData("-05:00", "-0500")]
    [InlineData("+12:45", "+1245")]
    [InlineData("+01:30:45", "+013045")]
    public void UtcOffset_Json_RoundTrips(string json, string text)
    {
        // act
        var testResult = IcalUtcOffset.Parse(json, IcalValueFormat.Json);

        // assert
        testResult.Should().Be(IcalUtcOffset.Parse(text, IcalValueFormat.Text));
        testResult.ToString(IcalValueFormat.Json).Should().Be(json);
        IcalUtcOffset.TryParse(text, IcalValueFormat.Json, out _).Should().BeFalse();
    }

    [Fact]
    public void Duration_Json_IsTheTextForm()
    {
        // act
        var testResult = IcalDuration.Parse("P1D", IcalValueFormat.Json);

        // assert
        testResult.ToString(IcalValueFormat.Json).Should().Be("P1D");
    }

    [Fact]
    public void Period_Json_JoinsTheJsonPartsWithASolidus()
    {
        // act
        var testResult = IcalPeriod.Parse("1997-03-08T16:00:00Z/P1D", IcalValueFormat.Json);

        // assert
        testResult.Should().Be(IcalPeriod.Parse("19970308T160000Z/P1D", IcalValueFormat.Text));
        testResult.ToString(IcalValueFormat.Json).Should().Be("1997-03-08T16:00:00Z/P1D");
    }

    [Fact]
    public void RecurrenceRule_Json_IsNotSupported()
    {
        // assert
        FluentActions.Invoking(() => IcalRecurrenceRule.Parse("FREQ=DAILY", IcalValueFormat.Json)).Should().Throw<NotSupportedException>()
            .WithMessage("*jCal*");
        FluentActions.Invoking(() => new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).ToString(IcalValueFormat.Json))
            .Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Text_Json_HasNoEscaping()
    {
        // act
        IcalValue.TryParseText(@"a\,b;c", IcalValueFormat.Json, out var parsed).Should().BeTrue();
        var formatted = IcalValue.FormatText("hello, world\nnext; line", IcalValueFormat.Json);

        // assert
        parsed.Should().Be(@"a\,b;c");
        formatted.Should().Be("hello, world\nnext; line");
        FluentActions.Invoking(() => IcalValue.FormatText("bell\a", IcalValueFormat.Json)).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => IcalValue.FormatText("lone\uD800", IcalValueFormat.Json)).Should().Throw<ArgumentException>();
        IcalValue.FormatText("pair😀", IcalValueFormat.Json).Should().Be("pair😀");
    }

    [Fact]
    public void TextList_Json_IsOneItem()
    {
        // act
        var items = new List<string>();
        foreach (var item in IcalValue.EnumerateTextList("a,b", IcalValueFormat.Json))
        {
            items.Add(item.ToString());
        }

        // assert
        items.Should().Equal("a,b");
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public void Boolean_Json_IsTheJsonLiteral(string json, bool value)
    {
        // assert
        IcalValue.TryParseBoolean(json, IcalValueFormat.Json, out var parsed).Should().BeTrue();
        parsed.Should().Be(value);
        var buffer = new char[8];
        IcalValue.TryFormatBoolean(value, buffer, out var charsWritten, IcalValueFormat.Json).Should().BeTrue();
        new string(buffer, 0, charsWritten).Should().Be(json);
    }

    [Theory]
    [InlineData("TRUE")]
    [InlineData("True")]
    [InlineData("1")]
    [InlineData(" true")]
    public void Boolean_JsonNotValid_IsRejected(string json)
    {
        // assert
        IcalValue.TryParseBoolean(json, IcalValueFormat.Json, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("42", 42)]
    [InlineData("-7", -7)]
    [InlineData("0", 0)]
    [InlineData("1.0", 1)]
    [InlineData("1e2", 100)]
    [InlineData("-2147483648", int.MinValue)]
    public void Integer_Json_IsAWholeJsonNumber(string json, int value)
    {
        // assert
        IcalValue.TryParseInteger(json, IcalValueFormat.Json, out var parsed).Should().BeTrue();
        parsed.Should().Be(value);
    }

    [Theory]
    [InlineData("+42")]
    [InlineData("042")]
    [InlineData("1.5")]
    [InlineData("2147483648")]
    [InlineData("1.")]
    [InlineData(" 1")]
    public void Integer_JsonNotValid_IsRejected(string json)
    {
        // assert
        IcalValue.TryParseInteger(json, IcalValueFormat.Json, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("1.3", 1.3)]
    [InlineData("-122.082932", -122.082932)]
    [InlineData("1.5E3", 1500)]
    [InlineData("0", 0)]
    public void Float_Json_IsAJsonNumber(string json, double value)
    {
        // assert
        IcalValue.TryParseFloat(json, IcalValueFormat.Json, out var parsed).Should().BeTrue();
        parsed.Should().Be(value);
        IcalValue.FormatFloat(value, IcalValueFormat.Json).Should().NotContain("E");
    }

    [Theory]
    [InlineData("+1.3")]
    [InlineData("01.3")]
    [InlineData(".5")]
    [InlineData("NaN")]
    [InlineData("1e999")]
    public void Float_JsonNotValid_IsRejected(string json)
    {
        // assert
        IcalValue.TryParseFloat(json, IcalValueFormat.Json, out _).Should().BeFalse();
    }

    [Fact]
    public void BinaryAndUri_Json_AreTheTextForm()
    {
        // assert
        IcalValue.TryParseBinary("SGVsbG8gV29ybGQh", IcalValueFormat.Json, out var bytes).Should().BeTrue();
        IcalValue.FormatBinary(bytes!, IcalValueFormat.Json).Should().Be("SGVsbG8gV29ybGQh");
        IcalValue.TryParseBinary("SGVs bG8=", IcalValueFormat.Json, out _).Should().BeFalse();
        IcalValue.TryParseCalAddress("mailto:kewisch@example.com", IcalValueFormat.Json, out var uri).Should().BeTrue();
        uri!.OriginalString.Should().Be("mailto:kewisch@example.com");
        IcalValue.TryParseUri(" http://example.org/", IcalValueFormat.Json, out _).Should().BeFalse();
    }
}
