using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class IcalValueFormatTests
{
    /// <summary>
    /// A parse in each lexical form, for every value type.
    /// </summary>
    public static TheoryData<string, Action<IcalValueFormat>> Parsers => new()
    {
        { "IcalDate", format => IcalDate.TryParse("19970714", format, out _) },
        { "IcalTime", format => IcalTime.TryParse("230000", format, out _) },
        { "IcalDateTime", format => IcalDateTime.TryParse("19980118T230000", format, out _) },
        { "IcalDateTime zoned", format => IcalDateTime.TryParse("19980118T230000", "Europe/London", format, out _) },
        { "IcalUtcOffset", format => IcalUtcOffset.TryParse("-0500", format, out _) },
        { "IcalDuration", format => IcalDuration.TryParse("P1D", format, out _) },
        { "IcalPeriod", format => IcalPeriod.TryParse("19970101T180000Z/PT5H30M", format, out _) },
        { "IcalRecurrenceRule", format => IcalRecurrenceRule.TryParse("FREQ=DAILY", format, out _) },
        { "Text", format => IcalValue.TryParseText("a", format, out _) },
        { "Text list", format => IcalValue.EnumerateTextList("a,b", format) },
        { "Boolean", format => IcalValue.TryParseBoolean("TRUE", format, out _) },
        { "Integer", format => IcalValue.TryParseInteger("1", format, out _) },
        { "Float", format => IcalValue.TryParseFloat("1.5", format, out _) },
        { "Binary", format => IcalValue.TryParseBinary("AAAA", format, out _) },
        { "Uri", format => IcalValue.TryParseUri("http://example.com/", format, out _) },
        { "CalAddress", format => IcalValue.TryParseCalAddress("mailto:a@example.com", format, out _) }
    };

    /// <summary>
    /// A format in each lexical form, for every value type.
    /// </summary>
    public static TheoryData<string, Action<IcalValueFormat>> Formatters => new()
    {
        { "IcalDate", format => new IcalDate(1997, 7, 14).TryFormat(new char[64], out _, format) },
        { "IcalTime", format => new IcalTime(23, 0, 0).TryFormat(new char[64], out _, format) },
        { "IcalDateTime", format => default(IcalDateTime).TryFormat(new char[64], out _, format) },
        { "IcalUtcOffset", format => default(IcalUtcOffset).TryFormat(new char[64], out _, format) },
        { "IcalDuration", format => default(IcalDuration).TryFormat(new char[64], out _, format) },
        { "IcalPeriod", format => default(IcalPeriod).TryFormat(new char[64], out _, format) },
        { "IcalRecurrenceRule", format => new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).TryFormat(new char[64], out _, format) },
        { "Text", format => IcalValue.TryFormatText("a", new char[64], out _, format) },
        { "Boolean", format => IcalValue.TryFormatBoolean(true, new char[64], out _, format) },
        { "Integer", format => IcalValue.TryFormatInteger(1, new char[64], out _, format) },
        { "Float", format => IcalValue.TryFormatFloat(1.5, new char[64], out _, format) },
        { "Binary", format => IcalValue.TryFormatBinary(new byte[3], new char[64], out _, format) },
        { "Uri", format => IcalValue.TryFormatUri(new Uri("http://example.com/"), new char[64], out _, format) },
        { "CalAddress", format => IcalValue.TryFormatCalAddress(new Uri("mailto:a@example.com"), new char[64], out _, format) }
    };

    [Theory]
    [MemberData(nameof(Parsers))]
    public void Parse_EachForm_IsSupported(string valueType, Action<IcalValueFormat> parse)
    {
        // act / assert
        parse.Invoking(action => action(IcalValueFormat.Text)).Should().NotThrow(valueType);
        AssertSupported(valueType, () => parse(IcalValueFormat.Json));
        AssertSupported(valueType, () => parse(IcalValueFormat.Xml));
        parse.Invoking(action => action((IcalValueFormat)99)).Should().Throw<ArgumentOutOfRangeException>(valueType);
    }

    [Theory]
    [MemberData(nameof(Formatters))]
    public void Format_EachForm_IsSupported(string valueType, Action<IcalValueFormat> format)
    {
        // act / assert
        format.Invoking(action => action(IcalValueFormat.Text)).Should().NotThrow(valueType);
        AssertSupported(valueType, () => format(IcalValueFormat.Json));
        AssertSupported(valueType, () => format(IcalValueFormat.Xml));
        format.Invoking(action => action((IcalValueFormat)99)).Should().Throw<ArgumentOutOfRangeException>(valueType);
    }

    [Fact]
    public void SpanFormattable_InterpolatedString_UsesTextForm()
    {
        // arrange
        var dateTime = IcalDateTime.Parse("19980119T070000Z", IcalValueFormat.Text);
        var duration = IcalDuration.Parse("P15DT5H0M20S", IcalValueFormat.Text);
        var rule = IcalRecurrenceRule.Parse("FREQ=DAILY;COUNT=10", IcalValueFormat.Text);

        // act
        var testResult = $"{dateTime}|{duration:T}|{rule}|{new IcalUtcOffset(TimeSpan.FromHours(-5))}|{new IcalDate(1997, 7, 14)}|{new IcalTime(7, 0, 0, true)}";

        // assert
        testResult.Should().Be("19980119T070000Z|P15DT5H0M20S|FREQ=DAILY;COUNT=10|-0500|19970714|070000Z");
    }

    [Fact]
    public void SpanFormattable_FormatStrings_SelectTheForm()
    {
        // arrange
        ISpanFormattable period = IcalPeriod.Parse("19970101T180000Z/PT5H30M", IcalValueFormat.Text);
        var destination = new char[64];

        // act
        var testResult = period.TryFormat(destination, out var charsWritten, "t", null);

        // assert
        testResult.Should().BeTrue();
        new string(destination, 0, charsWritten).Should().Be("19970101T180000Z/PT5H30M");
        period.ToString("J", null).Should().Be("1997-01-01T18:00:00Z/PT5H30M");
        period.ToString("x", null).Should().Be("1997-01-01T18:00:00Z/PT5H30M");
        period.Invoking(value => value.ToString("G", null)).Should().Throw<FormatException>();
    }

    /// <summary>
    /// Asserts that a value type supports the jCal or xCal form, unless it is a recurrence rule, which has no single lexical form in either.
    /// </summary>
    private static void AssertSupported(string valueType, Action action)
    {
        if (valueType == "IcalRecurrenceRule")
        {
            action.Should().Throw<NotSupportedException>(valueType);
        }
        else
        {
            action.Should().NotThrow(valueType);
        }
    }

    [Fact]
    public void Parse_InvalidValue_ThrowsFormatException()
    {
        // assert
        FluentActions.Invoking(() => IcalDate.Parse("20260230", IcalValueFormat.Text)).Should().Throw<FormatException>();
        FluentActions.Invoking(() => IcalTime.Parse("250000", IcalValueFormat.Text)).Should().Throw<FormatException>();
        FluentActions.Invoking(() => IcalDateTime.Parse("20260230T000000", IcalValueFormat.Text)).Should().Throw<FormatException>();
        FluentActions.Invoking(() => IcalUtcOffset.Parse("-0000", IcalValueFormat.Text)).Should().Throw<FormatException>();
        FluentActions.Invoking(() => IcalDuration.Parse("P1H", IcalValueFormat.Text)).Should().Throw<FormatException>();
        FluentActions.Invoking(() => IcalPeriod.Parse("19970101T180000Z", IcalValueFormat.Text)).Should().Throw<FormatException>();
        FluentActions.Invoking(() => IcalRecurrenceRule.Parse("COUNT=1", IcalValueFormat.Text)).Should().Throw<FormatException>();
    }
}
