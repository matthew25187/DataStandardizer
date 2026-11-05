using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of what every value type does with a form or a version it doesn't take, and of <see cref="VcardValueTypeRegistry"/>.
    /// </summary>
    public class VcardValueFormatTests
    {
        private const VcardValueFormat NotAFormat = (VcardValueFormat)3;
        private const VcardVersion NotAVersion = (VcardVersion)3;

        public static TheoryData<Action<VcardValueFormat>> ParseAndFormatCalls()
        {
            var date = new VcardDate(1985, 4, 12);
            var time = new VcardTime(10, 22, 0);
            var dateTime = new VcardDateTime(date, time);
            var destination = new char[64];
            return new TheoryData<Action<VcardValueFormat>>
            {
                format => VcardDate.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => date.TryFormat(destination, out _, format),
                format => VcardTime.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => time.TryFormat(destination, out _, format),
                format => VcardDateTime.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => dateTime.TryFormat(destination, out _, format),
                format => VcardTimestamp.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => new VcardTimestamp(date, time).TryFormat(destination, out _, format),
                format => VcardDateAndOrTime.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => new VcardDateAndOrTime(date).TryFormat(destination, out _, format),
                format => VcardUtcOffset.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => new VcardUtcOffset(false, 1, null).TryFormat(destination, out _, format),
                format => VcardName.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => VcardName.TryParseView(default(ReadOnlySpan<char>), format, out _),
                format => VcardAddress.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => VcardAddress.TryParseView(default(ReadOnlySpan<char>), format, out _),
                format => VcardOrganization.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => VcardOrganization.TryParseView(default(ReadOnlySpan<char>), format, out _),
                format => VcardGender.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => VcardClientPidMap.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => VcardPid.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => VcardGeo.TryParse(default(ReadOnlySpan<char>), format, out _),
                format => VcardValue.TryParseText(default(ReadOnlySpan<char>), format, destination, out _),
                format => VcardValue.TryFormatText(default(ReadOnlySpan<char>), destination, out _, format),
                format => VcardValue.TryFormatBoolean(true, destination, out _, format),
                format => VcardValue.TryFormatInteger(0, destination, out _, format),
                format => VcardValue.TryFormatFloat(0, destination, out _, format),
                format => VcardValue.TryFormatUri(new Uri(VcardValueExamples.UriHttp), destination, out _, format),
                format => VcardValue.TryFormatLanguageTag(default(DataStandardizer.LanguageTag.Bcp47LanguageTag), destination, out _, format),
                format => VcardValue.EnumerateList(default(ReadOnlySpan<char>), format),
                format => VcardValue.TryParseBoolean(default(ReadOnlySpan<char>), format, out _),
                format => VcardValue.TryParseInteger(default(ReadOnlySpan<char>), format, out _),
                format => VcardValue.TryParseFloat(default(ReadOnlySpan<char>), format, out _),
                format => VcardValue.TryParseUri(default(ReadOnlySpan<char>), format, out _),
                format => VcardValue.TryParseLanguageTag(default(ReadOnlySpan<char>), format, out _),
            };
        }

        [Theory]
        [MemberData(nameof(ParseAndFormatCalls), DisableDiscoveryEnumeration = true)]
        public void Call_JsonForm_ThrowsNotSupported(Action<VcardValueFormat> call)
        {
            // act
            Action testAction = () => call(VcardValueFormat.Json);

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Theory]
        [MemberData(nameof(ParseAndFormatCalls), DisableDiscoveryEnumeration = true)]
        public void Call_XmlForm_ThrowsNotSupported(Action<VcardValueFormat> call)
        {
            // act
            Action testAction = () => call(VcardValueFormat.Xml);

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Theory]
        [MemberData(nameof(ParseAndFormatCalls), DisableDiscoveryEnumeration = true)]
        public void Call_FormNotRecognised_ThrowsOutOfRange(Action<VcardValueFormat> call)
        {
            // act
            Action testAction = () => call(NotAFormat);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        public static TheoryData<Action> CallsWithAnUnknownVersion()
        {
            var date = new VcardDate(1985, 4, 12);
            var destination = new char[64];
            return new TheoryData<Action>
            {
                () => VcardDate.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => date.TryFormat(destination, out _, VcardValueFormat.Text, NotAVersion),
                () => date.IsDefinedIn(NotAVersion),
                () => VcardTime.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardDateTime.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardTimestamp.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardDateAndOrTime.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardUtcOffset.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardName.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardAddress.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardOrganization.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardGeo.TryParse(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, out _),
                () => VcardValue.TryParseText(default(ReadOnlySpan<char>), VcardValueFormat.Text, NotAVersion, destination, out _),
            };
        }

        [Theory]
        [MemberData(nameof(CallsWithAnUnknownVersion), DisableDiscoveryEnumeration = true)]
        public void Call_VersionNotRecognised_ThrowsOutOfRange(Action testAction)
        {
            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

#if NET6_0_OR_GREATER
        public static TheoryData<ISpanFormattable, string> Formattables()
        {
            return new TheoryData<ISpanFormattable, string>
            {
                { new VcardDate(1985, 4, 12), VcardValueExamples.DateComplete },
                { new VcardTime(10, 22, 0), VcardValueExamples.TimeComplete },
                { new VcardDateTime(new VcardDate(1996, 10, 22), new VcardTime(14, 0, 0)), VcardValueExamples.DateTimeComplete },
                { new VcardTimestamp(new VcardDate(1996, 10, 22), new VcardTime(14, 0, 0)), VcardValueExamples.TimestampLocal },
                { new VcardDateAndOrTime(new VcardDate(1985, 4, 12)), VcardValueExamples.DateComplete },
                { new VcardUtcOffset(true, 5, 0), VcardValueExamples.Version21OffsetBasic },
            };
        }

        [Theory]
        [MemberData(nameof(Formattables))]
        public void ToString_EmptyFormatString_WritesTheTextForm(ISpanFormattable value, string expected)
        {
            // act
            var testResult = value.ToString(string.Empty, null);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(Formattables))]
        public void TryFormat_SpanFormattableWithTextFormatString_WritesTheTextForm(ISpanFormattable value, string expected)
        {
            // arrange
            var destination = new char[64];

            // act
            value.TryFormat(destination, out var charsWritten, "T".AsSpan(), null);

            // assert
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        public static TheoryData<ISpanFormattable, string> OlderVersionFormattables()
        {
            var fraction = VcardTime.Parse("10:22:00,5".AsSpan(), VcardValueFormat.Text, VcardVersion.V3_0);
            var date = new VcardDate(1996, 10, 22);
            return new TheoryData<ISpanFormattable, string>
            {
                { fraction, "10:22:00,5" },
                { new VcardDateTime(date, fraction), "1996-10-22T10:22:00,5" },
                { new VcardTimestamp(date, fraction), "1996-10-22T10:22:00,5" },
                { new VcardDateAndOrTime(new VcardDateTime(date, fraction)), "1996-10-22T10:22:00,5" },
                { VcardUtcOffset.Parse("+2400".AsSpan(), VcardValueFormat.Text, VcardVersion.V2_1), "+24:00" },
            };
        }

        [Theory]
        [MemberData(nameof(OlderVersionFormattables))]
        public void ToString_FormattableOnlyAnOlderVersionHas_WritesAsParameterlessToStringDoes(ISpanFormattable value, string expected)
        {
            // act
            var testResult = value.ToString(string.Empty, null);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(OlderVersionFormattables))]
        public void TryFormat_SpanFormattableOnlyAnOlderVersionHas_WritesAsParameterlessToStringDoes(ISpanFormattable value, string expected)
        {
            // arrange
            var destination = new char[64];

            // act
            value.TryFormat(destination, out var charsWritten, string.Empty.AsSpan(), null);

            // assert
            new string(destination, 0, charsWritten).Should().Be(expected);
        }

        [Fact]
        public void ToString_FormatStringNotRecognised_Throws()
        {
            // arrange
            IFormattable value = new VcardDate(1985, 4, 12);

            // act
            Action testAction = () => value.ToString("Q", null);

            // assert
            testAction.Should().Throw<FormatException>();
        }
#endif

        [Theory]
        [InlineData("FN", VcardValueType.Text)]
        [InlineData("fn", VcardValueType.Text)]
        [InlineData("BDAY", VcardValueType.DateAndOrTime)]
        [InlineData("ANNIVERSARY", VcardValueType.DateAndOrTime)]
        [InlineData("REV", VcardValueType.Timestamp)]
        [InlineData("LANG", VcardValueType.LanguageTag)]
        [InlineData("TZ", VcardValueType.Text)]
        [InlineData("GEO", VcardValueType.Uri)]
        [InlineData("PHOTO", VcardValueType.Uri)]
        [InlineData("TEL", VcardValueType.Text)]
        [InlineData("UID", VcardValueType.Uri)]
        [InlineData("N", VcardValueType.Text)]
        [InlineData("X-ABC", VcardValueType.Unknown)]
        [InlineData("OTHER", VcardValueType.Unknown)]
        public void GetDefaultValueType_PropertyName_GivesItsDefault(string propertyName, VcardValueType expected)
        {
            // act
            var testResult = VcardValueTypeRegistry.GetDefaultValueType(propertyName.AsSpan());

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("BDAY", "text", VcardValueType.Text)]
        [InlineData("BDAY", "TEXT", VcardValueType.Text)]
        [InlineData("TZ", "utc-offset", VcardValueType.UtcOffset)]
        [InlineData("TEL", "uri", VcardValueType.Uri)]
        [InlineData("FN", "date", VcardValueType.Date)]
        [InlineData("FN", "x-other", VcardValueType.Unknown)]
        [InlineData("X-ABC", "uri", VcardValueType.Unknown)]
        public void GetValueType_ValueParameter_OverridesTheDefault(string propertyName, string valueParameter, VcardValueType expected)
        {
            // act
            var testResult = VcardValueTypeRegistry.GetValueType(propertyName.AsSpan(), valueParameter.AsSpan());

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("BDAY", VcardValueType.DateAndOrTime, true)]
        [InlineData("BDAY", VcardValueType.Text, true)]
        [InlineData("BDAY", VcardValueType.Date, false)]
        [InlineData("TZ", VcardValueType.UtcOffset, true)]
        [InlineData("FN", VcardValueType.Uri, false)]
        [InlineData("X-ABC", VcardValueType.Text, false)]
        public void IsAllowedValueType_PropertyAndType_FollowsRfc6350(string propertyName, VcardValueType valueType, bool expected)
        {
            // act
            var testResult = VcardValueTypeRegistry.IsAllowedValueType(propertyName.AsSpan(), valueType);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("FN", true)]
        [InlineData("X-ABC", false)]
        public void IsKnownProperty_PropertyName_SaysIfItIsInRfc6350(string propertyName, bool expected)
        {
            // act
            var testResult = VcardValueTypeRegistry.IsKnownProperty(propertyName.AsSpan());

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void GetValueType_ContentLineWithAValueParameter_UsesTheParameter()
        {
            // arrange
            var line = new VcardContentLine("BDAY", new[] { new VcardContentLineParameter("VALUE", "text") }, "circa 1800");

            // act
            var testResult = VcardValueTypeRegistry.GetValueType(line);

            // assert
            testResult.Should().Be(VcardValueType.Text);
        }

        [Fact]
        public void GetValueType_ContentLineWithNoValueParameter_UsesTheDefault()
        {
            // arrange
            var line = new VcardContentLine("BDAY", VcardValueExamples.DateMonthAndDay);

            // act
            var testResult = VcardValueTypeRegistry.GetValueType(line);

            // assert
            testResult.Should().Be(VcardValueType.DateAndOrTime);
        }

        [Fact]
        public void GetValueType_NullContentLine_Throws()
        {
            // act
            Action testAction = () => VcardValueTypeRegistry.GetValueType(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData("BDAY;VALUE=text:circa 1800\r\n", VcardValueType.Text)]
        [InlineData("BDAY:--0203\r\n", VcardValueType.DateAndOrTime)]
        public void GetValueType_ViewOfALine_UsesThePropertyAndParameter(string text, VcardValueType expected)
        {
            // arrange
            using (var reader = new VcardContentLineReader(new StringReader(text)))
            {
                reader.ReadContentLine();

                // act
                var testResult = VcardValueTypeRegistry.GetValueType(reader.CurrentLine);

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [InlineData("date-and-or-time", VcardValueType.DateAndOrTime)]
        [InlineData("LANGUAGE-TAG", VcardValueType.LanguageTag)]
        [InlineData("utc-offset", VcardValueType.UtcOffset)]
        public void TryParseValueTypeName_NameOfRfc6350_GivesTheType(string name, VcardValueType expected)
        {
            // act
            VcardValueType result;
            var testResult = VcardValueTypeRegistry.TryParseValueTypeName(name.AsSpan(), out result);

            // assert
            testResult.Should().BeTrue();
            result.Should().Be(expected);
        }

        [Fact]
        public void TryParseValueTypeName_OtherName_IsUnknown()
        {
            // act
            VcardValueType result;
            var testResult = VcardValueTypeRegistry.TryParseValueTypeName("binary".AsSpan(), out result);

            // assert
            testResult.Should().BeFalse();
            result.Should().Be(VcardValueType.Unknown);
        }

        [Theory]
        [InlineData(VcardValueType.Text, "text")]
        [InlineData(VcardValueType.DateAndOrTime, "date-and-or-time")]
        [InlineData(VcardValueType.LanguageTag, "language-tag")]
        public void GetValueTypeName_ValueType_GivesTheNameOfRfc6350(VcardValueType valueType, string expected)
        {
            // act
            var testResult = VcardValueTypeRegistry.GetValueTypeName(valueType);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(VcardValueType.Unknown)]
        [InlineData((VcardValueType)13)]
        public void GetValueTypeName_NoName_Throws(VcardValueType valueType)
        {
            // act
            Action testAction = () => VcardValueTypeRegistry.GetValueTypeName(valueType);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
