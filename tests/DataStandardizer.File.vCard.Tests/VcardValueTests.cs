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
