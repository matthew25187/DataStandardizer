using System;
using System.Collections.Generic;
using DataStandardizer.LanguageTag;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    public class VcardValueTests
    {
        private const int DestinationLength = 128;

        /// <summary>
        /// A double that needs 16 significant digits to read back as the same value, which .NET Framework's <c>"R"</c> writes with 15
        /// (<c>901.742658997766</c>), a different double.
        /// </summary>
        private const double SixteenDigitDouble = 901.7426589977661;

        /// <summary>
        /// The sum of 0.1 and 0.7, which needs 16 significant digits, and which .NET Framework's <c>"R"</c> writes with 17
        /// (<c>0.79999999999999993</c>).
        /// </summary>
        private const double SumOfOneTenthAndSevenTenths = 0.7999999999999999;

        /// <summary>
        /// A GEO coordinate that x64 .NET Framework's <c>"R"</c> writes as <c>0.84551240822557</c>, a different double.
        /// </summary>
        private const double GeoCoordinateWithSixteenDigits = 0.8455124082255701;

        // The smallest normal double, which needs 17 significant digits and has 307 zeros after the decimal point.
        private const double SmallestNormalDouble = 2.2250738585072014E-308;
        private const string SmallestNormalDoubleText = "0.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000022250738585072014";

        // The largest subnormal double, which has 307 zeros after the decimal point.
        private const double LargestSubnormalDouble = 2.225073858507201E-308;
        private const string LargestSubnormalDoubleText = "0.00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002225073858507201";

        // The smallest subnormal double, 2^-1074, which has 323 zeros after the decimal point.
        private const string SmallestSubnormalDoubleText = "0.000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000005";

        // 2^60 and 2^-20, powers of two whose neighbour below is half as far away as the one above.
        private const double TwoToTheSixtieth = 1152921504606846976.0;
        private const double TwoToTheMinusTwentieth = 9.5367431640625E-07;

        // Long enough for the smallest subnormal double written without an exponent.
        private const int LongFloatDestinationLength = 400;

        // Doubles in delegates, since xUnit on .NET Framework serializes a double in a theory row with that runtime's "R", which changes some.
        public static TheoryData<string, Func<double>, string> ShortestFloatTexts => new TheoryData<string, Func<double>, string>
        {
            { "needs 16 digits", () => SixteenDigitDouble, "901.7426589977661" },
            { "needs 16 digits, below 1", () => SumOfOneTenthAndSevenTenths, "0.7999999999999999" },
            { "GEO coordinate", () => GeoCoordinateWithSixteenDigits, "0.8455124082255701" },
            { "negative zero", () => -0.0, "-0" },
            { "whole number of 17 digits", () => 1E+16, "10000000000000000" },
            { "whole number of 18 digits", () => 1E+17, "100000000000000000" },
            { "decimal point four places before the first digit", () => 0.00001, "0.00001" },
            { "smallest normal", () => SmallestNormalDouble, SmallestNormalDoubleText },
            { "largest subnormal", () => LargestSubnormalDouble, LargestSubnormalDoubleText },
            { "smallest subnormal", () => double.Epsilon, SmallestSubnormalDoubleText },
            { "power of two, above 1", () => TwoToTheSixtieth, "1152921504606847000" },
            { "power of two, below 1", () => TwoToTheMinusTwentieth, "0.00000095367431640625" },
        };

        [Theory]
        [InlineData("TRUE", true)]
        [InlineData("True", true)]
        [InlineData("true", true)]
        [InlineData("FALSE", false)]
        [InlineData("false", false)]
        public void TryParseBoolean_AnyCase_IsValid(string value, bool expected)
        {
            // act
            bool result;
            var testResult = VcardValue.TryParseBoolean(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("T")]
        [InlineData("yes")]
        [InlineData("1")]
        public void TryParseBoolean_OtherText_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseBoolean(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData(VcardValueExamples.IntegerPositive, 1234567890L)]
        [InlineData(VcardValueExamples.IntegerNegative, -1234556790L)]
        [InlineData(VcardValueExamples.IntegerSigned, 1234556790L)]
        [InlineData(VcardValueExamples.IntegerMaximum, long.MaxValue)]
        [InlineData(VcardValueExamples.IntegerMinimum, long.MinValue)]
        public void TryParseInteger_Rfc6350Example_GivesTheNumber(string value, long expected)
        {
            // act
            long result;
            var testResult = VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("+")]
        [InlineData("-")]
        [InlineData("1.0")]
        [InlineData("1e3")]
        [InlineData(" 1")]
        [InlineData("9223372036854775808")]
        [InlineData("-9223372036854775809")]
        public void TryParseInteger_InvalidValue_ReturnsFalseAndZero(string value)
        {
            // act
            long result;
            var testResult = VcardValue.TryParseInteger(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(0);
        }

        [Theory]
        [InlineData(VcardValueExamples.FloatSimple, 20.30)]
        [InlineData(VcardValueExamples.FloatPrecise, 1000000.0000001)]
        [InlineData("+1", 1.0)]
        [InlineData("-1.5", -1.5)]
        public void TryParseFloat_ValidValue_GivesTheNumber(string value, double expected)
        {
            // act
            double result;
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("")]
        [InlineData("1.")]
        [InlineData(".5")]
        [InlineData("1e3")]
        [InlineData("1.0E2")]
        [InlineData("NaN")]
        [InlineData("1,5")]
        public void TryParseFloat_InvalidValue_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        // A numeral of 1e400, past double.MaxValue (about 1.8e308), so it parses to infinity, which isn't a FLOAT.
        private const string FloatBeyondDoubleWithExponent = "1e400";
        private const string NegativeFloatBeyondDoubleWithExponent = "-1e400";

        // The digit count of a numeral with no exponent that is past double.MaxValue, which has 309 integer digits.
        private const int FloatBeyondDoubleDigitCount = 310;

        public static TheoryData<VcardValueFormat, string> FloatsBeyondDouble() => new TheoryData<VcardValueFormat, string>
        {
            { VcardValueFormat.Text, new string('9', FloatBeyondDoubleDigitCount) },
            { VcardValueFormat.Text, "-" + new string('9', FloatBeyondDoubleDigitCount) },
            { VcardValueFormat.Json, FloatBeyondDoubleWithExponent },
            { VcardValueFormat.Json, NegativeFloatBeyondDoubleWithExponent },
            { VcardValueFormat.Xml, FloatBeyondDoubleWithExponent },
            { VcardValueFormat.Xml, NegativeFloatBeyondDoubleWithExponent },
        };

        [Theory]
        [MemberData(nameof(FloatsBeyondDouble))]
        public void TryParseFloat_BeyondTheRangeOfDouble_ReturnsFalse(VcardValueFormat format, string value)
        {
            // act
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), format, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(FloatsBeyondDouble))]
        public void TryParseFloat_BeyondTheRangeOfDouble_GivesZero(VcardValueFormat format, string value)
        {
            // act
            double result;
            VcardValue.TryParseFloat(value.AsSpan(), format, out result);

            // assert
            result.Should().Be(0);
        }

        /// <summary>
        /// The double nearest to <c>5.882948112288285E-204</c>.  .NET Framework reads that text as the double above it,
        /// <c>5.8829481122882854E-204</c>.
        /// </summary>
        private const double SixteenDigitExponentDouble = 5.8829481122882847E-204;

        /// <summary>
        /// 2^53, the double below 2^53 + 1, which lies halfway between it and <see cref="TwoToThe53PlusTwo"/>.
        /// </summary>
        private const double TwoToThe53 = 9007199254740992d;

        /// <summary>
        /// 2^53 + 2, the double above 2^53 + 1.
        /// </summary>
        private const double TwoToThe53PlusTwo = 9007199254740994d;

        /// <summary>
        /// 2^53 + 1, with a last digit beyond the 17 significant digits a double needs, which puts it just above halfway between
        /// <see cref="TwoToThe53"/> and <see cref="TwoToThe53PlusTwo"/>.  .NET Framework ignores the last digit and reads <see cref="TwoToThe53"/>.
        /// </summary>
        private const string JustAboveHalfwayText = "9007199254740993.0000000000000001";

        /// <summary>
        /// Just above half the smallest subnormal double, so that its nearest double is <see cref="double.Epsilon"/>, not zero.
        /// </summary>
        private const string JustAboveHalfTheSmallestSubnormalText = "2.4703282292062327208828439643411068618252990130716238221279284125033775364E-324";

        /// <summary>
        /// Just below halfway between <see cref="double.MaxValue"/> and 2^1024, so that its nearest double is <see cref="double.MaxValue"/>.
        /// </summary>
        private const string JustBelowHalfwayAboveTheLargestText = "1.797693134862315807937289714053E+308";

        // The zeros between 2^53 + 1 and a last digit 1, more than the 767 significant digits a value halfway between two doubles can have.
        private const int ZerosPastTheHalfwayDigits = 1000;

        // The expected doubles are their bits, so that negative zero differs from positive zero, and so that xUnit on .NET Framework doesn't
        // serialize a double in a theory row with that runtime's "R".
        public static TheoryData<string, string, VcardValueFormat, long> NearestDoubleCases => new TheoryData<string, string, VcardValueFormat, long>
        {
            { "negative zero", "-0", VcardValueFormat.Text, BitConverter.DoubleToInt64Bits(-0.0) },
            { "negative zero, jCard", "-0", VcardValueFormat.Json, BitConverter.DoubleToInt64Bits(-0.0) },
            { "negative zero with a fraction, xCard", "-0.0E0", VcardValueFormat.Xml, BitConverter.DoubleToInt64Bits(-0.0) },
            { "needs 16 digits", "901.7426589977661", VcardValueFormat.Text, BitConverter.DoubleToInt64Bits(SixteenDigitDouble) },
            { "16 digits and an exponent", "5.882948112288285E-204", VcardValueFormat.Json, BitConverter.DoubleToInt64Bits(SixteenDigitExponentDouble) },
            { "smallest normal", SmallestNormalDoubleText, VcardValueFormat.Text, BitConverter.DoubleToInt64Bits(SmallestNormalDouble) },
            { "smallest normal, rounded up", "2.2250738585072012E-308", VcardValueFormat.Xml, BitConverter.DoubleToInt64Bits(SmallestNormalDouble) },
            { "smallest subnormal", SmallestSubnormalDoubleText, VcardValueFormat.Text, BitConverter.DoubleToInt64Bits(double.Epsilon) },
            { "just above halfway", JustAboveHalfwayText, VcardValueFormat.Text, BitConverter.DoubleToInt64Bits(TwoToThe53PlusTwo) },
            { "exactly halfway, odd neighbour above", "9007199254740993", VcardValueFormat.Text, BitConverter.DoubleToInt64Bits(TwoToThe53) },
            { "just above halfway, in more digits than a halfway value can have", "9007199254740993." + new string('0', ZerosPastTheHalfwayDigits) + "1", VcardValueFormat.Text, BitConverter.DoubleToInt64Bits(TwoToThe53PlusTwo) },
            { "just below half the smallest subnormal", "2.4703282292062327E-324", VcardValueFormat.Json, BitConverter.DoubleToInt64Bits(0.0) },
            { "just above half the smallest subnormal", JustAboveHalfTheSmallestSubnormalText, VcardValueFormat.Json, BitConverter.DoubleToInt64Bits(double.Epsilon) },
            { "far below the smallest subnormal", "1E-99999999999", VcardValueFormat.Xml, BitConverter.DoubleToInt64Bits(0.0) },
            { "just below halfway above the largest", JustBelowHalfwayAboveTheLargestText, VcardValueFormat.Xml, BitConverter.DoubleToInt64Bits(double.MaxValue) },
        };

        [Theory]
        [MemberData(nameof(NearestDoubleCases))]
        public void TryParseFloat_Value_GivesTheNearestDoubleOnEveryTarget(string scenario, string value, VcardValueFormat format, long expectedBits)
        {
            // arrange
            _ = scenario;

            // act
            var testResult = VcardValue.TryParseFloat(value.AsSpan(), format, out var result);

            // assert
            testResult.Should().BeTrue();
            BitConverter.DoubleToInt64Bits(result).Should().Be(expectedBits);
        }

        [Theory]
        [InlineData(1000000.0000001, VcardValueExamples.FloatPrecise)]
        [InlineData(1e21, "1000000000000000000000")]
        [InlineData(-1.5e-7, "-0.00000015")]
        public void TryFormatFloat_AnyNumber_WritesNoExponent(double value, string expected)
        {
            // arrange
            var destination = new char[DestinationLength];

            // act
            int charsWritten;
            var testResult = VcardValue.TryFormatFloat(value, destination, out charsWritten, VcardValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(ShortestFloatTexts))]
        public void TryFormatFloat_AnyNumber_WritesTheFewestDigitsThatReadBack(string scenario, Func<double> value, string expected)
        {
            // arrange
            _ = scenario;
            var number = value();
            var destination = new char[LongFloatDestinationLength];

            // act
            var testResult = VcardValue.TryFormatFloat(number, destination, out var charsWritten, VcardValueFormat.Text);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        [Theory]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        public void TryFormatFloat_NotAFiniteNumber_Throws(double value)
        {
            // arrange
            var destination = new char[DestinationLength];

            // act
            Action testAction = () => VcardValue.TryFormatFloat(value, destination, out _, VcardValueFormat.Text);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void TryFormatBoolean_True_WritesTheKeyword()
        {
            // arrange
            var destination = new char[DestinationLength];

            // act
            int charsWritten;
            VcardValue.TryFormatBoolean(true, destination, out charsWritten, VcardValueFormat.Text);

            // assert
            new string(destination, 0, charsWritten).Should().Be("TRUE");
        }

        [Fact]
        public void TryFormatInteger_SmallestValue_WritesTheNumber()
        {
            // arrange
            var destination = new char[DestinationLength];

            // act
            int charsWritten;
            VcardValue.TryFormatInteger(long.MinValue, destination, out charsWritten, VcardValueFormat.Text);

            // assert
            new string(destination, 0, charsWritten).Should().Be(VcardValueExamples.IntegerMinimum);
        }

        [Theory]
        [InlineData(VcardValueExamples.UriHttp)]
        [InlineData(VcardValueExamples.UriLdap)]
        [InlineData(VcardValueExamples.ClientPidMapUri)]
        public void TryParseUri_Rfc6350Example_WritesTheSameText(string value)
        {
            // arrange
            var destination = new char[DestinationLength];

            // act
            Uri result;
            var testResult = VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            int charsWritten;
            VcardValue.TryFormatUri(result, destination, out charsWritten, VcardValueFormat.Text);
            new string(destination, 0, charsWritten).Should().Be(value);
        }

        [Theory]
        [InlineData("http://[::1]/a")]
        [InlineData("geo:1\\,2;a=[b]")]
        public void TryParseUri_BracketsWhereTheirStandardAllowsThem_IsValid(string value)
        {
            // act
            var testResult = VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void TryFormatUri_UriGivenWithASpace_Throws()
        {
            // arrange
            var uri = new Uri("https://example.com/a b");
            var destination = new char[DestinationLength];

            // act
            Action testAction = () => VcardValue.TryFormatUri(uri, destination, out _, VcardValueFormat.Text);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("not a uri")]
        [InlineData("/relative/path")]
        [InlineData("1http://example.com")]
        [InlineData("http://example.com/%zz")]
        [InlineData("http://example.com/%2")]
        [InlineData("http://example.com/a b")]
        [InlineData("http://example.com/<a>")]
        [InlineData("https://example.com/a[b]")]
        [InlineData("https://example.com/?a=[b]")]
        [InlineData("https://example.com/#[b]")]
        [InlineData("urn:a[b]")]
        public void TryParseUri_InvalidValue_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseUri(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("fr")]
        [InlineData("fr-CA")]
        [InlineData("de-AT")]
        public void TryParseLanguageTag_WellFormedTag_IsTheTag(string value)
        {
            // arrange
            var destination = new char[DestinationLength];

            // act
            Bcp47LanguageTag result;
            var testResult = VcardValue.TryParseLanguageTag(value.AsSpan(), VcardValueFormat.Text, out result);

            // assert
            testResult.Should().BeTrue();
            int charsWritten;
            VcardValue.TryFormatLanguageTag(result, destination, out charsWritten, VcardValueFormat.Text);
            new string(destination, 0, charsWritten).Should().Be(value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("-")]
        [InlineData("en-")]
        [InlineData("not_a_tag")]
        [InlineData("toolonglanguagesubtag")]
        public void TryParseLanguageTag_TagThatLanguageTagRejects_ReturnsFalse(string value)
        {
            // act
            var testResult = VcardValue.TryParseLanguageTag(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormatLanguageTag_UninitializedTag_Throws()
        {
            // arrange
            var destination = new char[DestinationLength];

            // act
            Action testAction = () => VcardValue.TryFormatLanguageTag(default(Bcp47LanguageTag), destination, out _, VcardValueFormat.Text);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData(VcardValueExamples.TextEscapedComma, VcardVersion.V4_0, VcardValueExamples.TextUnescapedComma)]
        [InlineData("a\\\\b", VcardVersion.V4_0, "a\\b")]
        [InlineData("a\\nb", VcardVersion.V4_0, "a\nb")]
        [InlineData("a\\Nb", VcardVersion.V4_0, "a\nb")]
        [InlineData("a\\;b", VcardVersion.V4_0, "a;b")]
        [InlineData("a;b", VcardVersion.V4_0, "a;b")]
        [InlineData("a\\;b", VcardVersion.V3_0, "a;b")]
        [InlineData("a\\,b", VcardVersion.V3_0, "a,b")]
        [InlineData("a,b;c\\d", VcardVersion.V2_1, "a,b;c\\d")]
        public void TryParseText_ValidValue_RemovesItsEscaping(string value, VcardVersion version, string expected)
        {
            // arrange
            var destination = new char[value.Length];

            // act
            int charsWritten;
            var testResult = VcardValue.TryParseText(value.AsSpan(), VcardValueFormat.Text, version, destination, out charsWritten);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        [Theory]
        [InlineData("a,b", VcardVersion.V4_0)]
        [InlineData("a\\:b", VcardVersion.V4_0)]
        [InlineData("a\\", VcardVersion.V4_0)]
        [InlineData("a\x0001", VcardVersion.V4_0)]
        [InlineData("a\x007F", VcardVersion.V4_0)]
        [InlineData("a\nb", VcardVersion.V4_0)]
        [InlineData("a,b", VcardVersion.V3_0)]
        [InlineData("a;b", VcardVersion.V3_0)]
        [InlineData("a\x0001", VcardVersion.V2_1)]
        public void TryParseText_InvalidValue_ReturnsFalse(string value, VcardVersion version)
        {
            // arrange
            var destination = new char[value.Length];

            // act
            int charsWritten;
            var testResult = VcardValue.TryParseText(value.AsSpan(), VcardValueFormat.Text, version, destination, out charsWritten);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Fact]
        public void TryParseText_LoneSurrogate_ReturnsFalse()
        {
            // arrange
            const string value = "a\xD800b";

            // act
            var testResult = VcardValue.TryParseText(value.AsSpan(), VcardValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryParseText_DestinationShorterThanValue_Throws()
        {
            // arrange
            var destination = new char[1];

            // act
            Action testAction = () => VcardValue.TryParseText("ab".AsSpan(), VcardValueFormat.Text, destination, out _);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("a,b;c\\d\ne", VcardVersion.V4_0, "a\\,b;c\\\\d\\ne")]
        [InlineData("a,b;c\\d\ne", VcardVersion.V3_0, "a\\,b\\;c\\\\d\\ne")]
        [InlineData("a,b;c\\d", VcardVersion.V2_1, "a,b;c\\d")]
        [InlineData("a\r\nb\rc", VcardVersion.V4_0, "a\\nb\\nc")]
        public void FormatText_TextToEscape_WritesTheEscapingOfTheVersion(string value, VcardVersion version, string expected)
        {
            // act
            var testResult = VcardValue.FormatText(value, VcardValueFormat.Text, version);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void FormatText_ControlCharacter_Throws()
        {
            // act
            Action testAction = () => VcardValue.FormatText("a\x01", VcardValueFormat.Text, VcardVersion.V4_0);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void FormatText_NullValue_Throws()
        {
            // act
            Action testAction = () => VcardValue.FormatText(null, VcardValueFormat.Text, VcardVersion.V4_0);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void EnumerateList_Rfc6350TextList_GivesEachItem()
        {
            // arrange
            var expected = new[] { "this is one value", "this is another" };

            // act
            var testResult = EnumerateListItems(VcardValueExamples.TextList);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [InlineData("", new[] { "" })]
        [InlineData("a\\,b,c", new[] { "a\\,b", "c" })]
        [InlineData(",", new[] { "", "" })]
        public void EnumerateList_EscapedAndEmptyItems_GivesEachItem(string value, string[] expected)
        {
            // act
            var testResult = EnumerateListItems(value);

            // assert
            testResult.Should().Equal(expected);
        }

        /// <summary>
        /// Copies what <see cref="VcardValue.EnumerateList"/> yields, since the items are spans that can't leave the method.
        /// </summary>
        private static List<string> EnumerateListItems(string value)
        {
            var items = new List<string>();
            foreach (var item in VcardValue.EnumerateList(value.AsSpan(), VcardValueFormat.Text))
            {
                items.Add(item.ToString());
            }

            return items;
        }
    }
}
