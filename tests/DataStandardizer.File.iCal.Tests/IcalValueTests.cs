using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class IcalValueTests
{
    #region TEXT

    [Theory]
    [InlineData("Project XYZ Final Review\\nConference Room - 3B\\nCome Prepared.", "Project XYZ Final Review\nConference Room - 3B\nCome Prepared.")]
    [InlineData("a\\\\b\\;c\\,d", "a\\b;c,d")]
    [InlineData("Line\\NBreak", "Line\nBreak")]
    [InlineData("Colon: and \"quotes\" are plain", "Colon: and \"quotes\" are plain")]
    [InlineData("Café 会議 😀", "Café 会議 😀")]
    [InlineData("", "")]
    public void TryParseText_ValidValue_Unescapes(string value, string expected)
    {
        // act
        var testResult = IcalValue.TryParseText(value, IcalValueFormat.Text, out var text);

        // assert
        testResult.Should().BeTrue();
        text.Should().Be(expected);
    }

    [Theory]
    [InlineData("trailing\\")]
    [InlineData("bad \\x escape")]
    [InlineData("colon \\: escape")]
    public void TryParseText_InvalidEscape_Fails(string value)
    {
        // act
        var testResult = IcalValue.TryParseText(value, IcalValueFormat.Text, out var text);

        // assert
        testResult.Should().BeFalse();
        text.Should().BeNull();
    }

    [Fact]
    public void TryParseText_IntoSpan_WritesUnescapedText()
    {
        // arrange
        var destination = new char[10];

        // act
        var testResult = IcalValue.TryParseText("a\\,b", IcalValueFormat.Text, destination, out var charsWritten);

        // assert
        testResult.Should().BeTrue();
        new string(destination, 0, charsWritten).Should().Be("a,b");
    }

    [Fact]
    public void TryParseText_DestinationShorterThanValue_Throws()
    {
        // act
        var act = () => IcalValue.TryParseText("a\\,b", IcalValueFormat.Text, new char[3], out _);

        // assert
        act.Should().Throw<ArgumentException>().WithParameterName("destination");
    }

    [Theory]
    [InlineData("a\\b;c,d", "a\\\\b\\;c\\,d")]
    [InlineData("one\ntwo\r\nthree\rfour", "one\\ntwo\\nthree\\nfour")]
    [InlineData("tab\tand: \"quote\"", "tab\tand: \"quote\"")]
    [InlineData("Café 😀", "Café 😀")]
    [InlineData("", "")]
    public void FormatText_Value_Escapes(string value, string expected)
    {
        // act
        var testResult = IcalValue.FormatText(value, IcalValueFormat.Text);

        // assert
        testResult.Should().Be(expected);
    }

    [Fact]
    public void FormatText_InvalidCharacter_Throws()
    {
        // arrange (xunit replaces a lone surrogate in theory data with U+FFFD, so the values are listed here)
        var values = new[] { "bell\a", "delete\x7F", "lone \uD800 surrogate", "lone \uDC00 surrogate", "reversed \uDC00\uD800 pair" };

        // act / assert
        foreach (var value in values)
        {
            FluentActions.Invoking(() => IcalValue.FormatText(value, IcalValueFormat.Text)).Should().Throw<ArgumentException>(value)
                .WithParameterName("value");
        }
    }

    [Theory]
    [InlineData("a\\\\b;c,d\nTabbed\there")]
    [InlineData("\\\\\\;,,;\\")]
    [InlineData("Café 会議 😀, more")]
    public void FormatText_ThenParse_RoundTrips(string value)
    {
        // act
        var formatted = IcalValue.FormatText(value, IcalValueFormat.Text);
        IcalValue.TryParseText(formatted, IcalValueFormat.Text, out var testResult);

        // assert
        testResult.Should().Be(value);
    }

    [Fact]
    public void TryFormatText_DestinationTooShort_ReturnsFalse()
    {
        // act
        var testResult = IcalValue.TryFormatText("a,b", new char[3], out var charsWritten, IcalValueFormat.Text);

        // assert
        testResult.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    [Theory]
    [InlineData("APPOINTMENT,EDUCATION", new[] { "APPOINTMENT", "EDUCATION" })]
    [InlineData("MEETING", new[] { "MEETING" })]
    [InlineData("a\\,b,c", new[] { "a\\,b", "c" })]
    [InlineData("a\\\\,b", new[] { "a\\\\", "b" })]
    [InlineData("a\\\\\\,b", new[] { "a\\\\\\,b" })]
    [InlineData(",", new[] { "", "" })]
    [InlineData("", new[] { "" })]
    [InlineData("a,,b,", new[] { "a", "", "b", "" })]
    public void EnumerateTextList_SplitsOnUnescapedCommas(string value, string[] expected)
    {
        // arrange
        var testResult = new List<string>();

        // act
        foreach (var item in IcalValue.EnumerateTextList(value, IcalValueFormat.Text))
        {
            testResult.Add(item.ToString());
        }

        // assert
        testResult.Should().Equal(expected);
    }

    #endregion

    #region BOOLEAN

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("FALSE", false)]
    [InlineData("true", true)]
    [InlineData("False", false)]
    public void TryParseBoolean_ValidValue_Parses(string value, bool expected)
    {
        // act
        var testResult = IcalValue.TryParseBoolean(value, IcalValueFormat.Text, out var result);

        // assert
        testResult.Should().BeTrue();
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("YES")]
    [InlineData("1")]
    [InlineData("TRUE ")]
    public void TryParseBoolean_InvalidValue_Fails(string value)
    {
        // act
        var testResult = IcalValue.TryParseBoolean(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, "TRUE")]
    [InlineData(false, "FALSE")]
    public void TryFormatBoolean_WritesUpperCase(bool value, string expected)
    {
        // arrange
        var destination = new char[5];

        // act
        var testResult = IcalValue.TryFormatBoolean(value, destination, out var charsWritten, IcalValueFormat.Text);

        // assert
        testResult.Should().BeTrue();
        new string(destination, 0, charsWritten).Should().Be(expected);
    }

    #endregion

    #region INTEGER

    [Theory]
    [InlineData("1234567890", 1234567890, "1234567890")]
    [InlineData("-1234567890", -1234567890, "-1234567890")]
    [InlineData("+1234567890", 1234567890, "1234567890")]
    [InlineData("432109876", 432109876, "432109876")]
    [InlineData("2147483647", int.MaxValue, "2147483647")]
    [InlineData("-2147483648", int.MinValue, "-2147483648")]
    [InlineData("007", 7, "7")]
    [InlineData("-0", 0, "0")]
    public void TryParseInteger_RfcExampleAndLimits_ParsesAndFormats(string value, int expected, string formatted)
    {
        // arrange
        var destination = new char[11];

        // act
        var testResult = IcalValue.TryParseInteger(value, IcalValueFormat.Text, out var result);
        IcalValue.TryFormatInteger(result, destination, out var charsWritten, IcalValueFormat.Text);

        // assert
        testResult.Should().BeTrue();
        result.Should().Be(expected);
        new string(destination, 0, charsWritten).Should().Be(formatted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    [InlineData("1.0")]
    [InlineData("1e3")]
    [InlineData(" 1")]
    public void TryParseInteger_InvalidValue_Fails(string value)
    {
        // act
        var testResult = IcalValue.TryParseInteger(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    #endregion

    #region FLOAT

    [Theory]
    [InlineData("1000000.0000001", 1000000.0000001)]
    [InlineData("1.333", 1.333)]
    [InlineData("-3.14", -3.14)]
    [InlineData("+5", 5.0)]
    [InlineData("37.386013", 37.386013)]
    [InlineData("-122.082932", -122.082932)]
    public void TryParseFloat_RfcExample_ParsesAndRoundTrips(string value, double expected)
    {
        // act
        var testResult = IcalValue.TryParseFloat(value, IcalValueFormat.Text, out var result);

        // assert
        testResult.Should().BeTrue();
        result.Should().Be(expected);
        double.Parse(IcalValue.FormatFloat(result, IcalValueFormat.Text), System.Globalization.CultureInfo.InvariantCulture).Should().Be(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".5")]
    [InlineData("5.")]
    [InlineData("1e10")]
    [InlineData("1,5")]
    [InlineData("--1")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("1.5 ")]
    public void TryParseFloat_InvalidValue_Fails(string value)
    {
        // act
        var testResult = IcalValue.TryParseFloat(value, IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Fact]
    public void TryParseFloat_TooLarge_Fails()
    {
        // act
        var testResult = IcalValue.TryParseFloat(new string('9', 400), IcalValueFormat.Text, out _);

        // assert
        testResult.Should().BeFalse();
    }

    [Theory]
    [InlineData(1e20, "100000000000000000000")]
    [InlineData(1.5e-7, "0.00000015")]
    [InlineData(-2.5e-5, "-0.000025")]
    [InlineData(1.25e21, "1250000000000000000000")]
    [InlineData(0.0, "0")]
    [InlineData(-3.14, "-3.14")]
    public void FormatFloat_NeedsNoExponent(double value, string expected)
    {
        // act
        var testResult = IcalValue.FormatFloat(value, IcalValueFormat.Text);

        // assert
        testResult.Should().Be(expected);
        IcalValue.TryParseFloat(testResult, IcalValueFormat.Text, out var parsed).Should().BeTrue();
        parsed.Should().Be(value);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FormatFloat_NotFinite_Throws(double value)
    {
        // act
        var act = () => IcalValue.FormatFloat(value, IcalValueFormat.Text);

        // assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion

    #region BINARY

    [Fact]
    public void TryParseBinary_RfcExample_MatchesConvert()
    {
        // arrange
        const string value = "AAABAAEAEBAQAAEABAAoAQAAFgAAACgAAAAQAAAAIAAAAAEABAAA";

        // act
        var testResult = IcalValue.TryParseBinary(value, IcalValueFormat.Text, out var bytes);

        // assert
        testResult.Should().BeTrue();
        bytes.Should().Equal(Convert.FromBase64String(value));
        IcalValue.FormatBinary(bytes!, IcalValueFormat.Text).Should().Be(value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(256)]
    public void FormatBinary_EveryLength_MatchesConvertAndRoundTrips(int length)
    {
        // arrange
        var bytes = Enumerable.Range(0, length).Select(index => (byte)(index * 37 + 11)).ToArray();

        // act
        var testResult = IcalValue.FormatBinary(bytes, IcalValueFormat.Text);
        IcalValue.TryParseBinary(testResult, IcalValueFormat.Text, out var parsed);

        // assert
        testResult.Should().Be(Convert.ToBase64String(bytes));
        parsed.Should().Equal(bytes);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("AAA")]
    [InlineData("AA=A")]
    [InlineData("A===")]
    [InlineData("====")]
    [InlineData("AA AA")]
    [InlineData("AA\r\nAA")]
    [InlineData("AA-_")]
    public void TryParseBinary_InvalidValue_Fails(string value)
    {
        // act
        var testResult = IcalValue.TryParseBinary(value, IcalValueFormat.Text, out var bytes);

        // assert
        testResult.Should().BeFalse();
        bytes.Should().BeNull();
    }

    [Fact]
    public void TryFormatBinary_DestinationTooShort_ReturnsFalse()
    {
        // act
        var testResult = IcalValue.TryFormatBinary(new byte[] { 1, 2, 3, 4 }, new char[7], out var charsWritten, IcalValueFormat.Text);

        // assert
        testResult.Should().BeFalse();
        charsWritten.Should().Be(0);
    }

    #endregion

    #region URI and CAL-ADDRESS

    [Theory]
    [InlineData("http://example.com/my-report.txt")]
    [InlineData("ftp://example.com/pub/docs/agenda.doc")]
    [InlineData("mailto:jane_doe@example.com")]
    [InlineData("urn:uuid:f81d4fae-7dec-11d0-a765-00a0c91e6bf6")]
    public void TryParseUri_RfcExample_ParsesAndRoundTrips(string value)
    {
        // arrange
        var destination = new char[value.Length];

        // act
        var testResult = IcalValue.TryParseUri(value, IcalValueFormat.Text, out var uri);
        IcalValue.TryFormatUri(uri!, destination, out var charsWritten, IcalValueFormat.Text);

        // assert
        testResult.Should().BeTrue();
        new string(destination, 0, charsWritten).Should().Be(value);
    }

    [Fact]
    public void TryParseCalAddress_RfcExample_IsMailto()
    {
        // act
        var testResult = IcalValue.TryParseCalAddress("mailto:jane_doe@example.com", IcalValueFormat.Text, out var address);

        // assert
        testResult.Should().BeTrue();
        address!.Scheme.Should().Be("mailto");
    }

    [Theory]
    [InlineData("")]
    [InlineData("/relative/path")]
    [InlineData("example.com")]
    [InlineData("1http://example.com")]
    [InlineData("C:\\temp\\file.txt")]
    public void TryParseUri_NotAbsoluteUri_Fails(string value)
    {
        // act
        var testResult = IcalValue.TryParseUri(value, IcalValueFormat.Text, out var uri);

        // assert
        testResult.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void TryFormatUri_RelativeUri_Throws()
    {
        // act
        var act = () => IcalValue.TryFormatUri(new Uri("relative", UriKind.Relative), new char[16], out _, IcalValueFormat.Text);

        // assert
        act.Should().Throw<ArgumentException>().WithParameterName("value");
    }

    #endregion
}
