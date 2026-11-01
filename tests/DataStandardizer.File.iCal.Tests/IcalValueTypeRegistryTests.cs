using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalValueTypeRegistryTests
    {
        private const string DtStart = "DTSTART";
        private const string Attach = "ATTACH";
        private const string RDate = "RDATE";
        private const string Trigger = "TRIGGER";
        private const string ExtensionProperty = "X-PROP";

        // ref. RFC 5545 §3.3.4: the DATE value type name.
        private const string DateValueTypeName = "DATE";

        // ref. RFC 5545 §3.3.4: July 14, 1997, the example DATE value.
        private const string Rfc5545DateExample = "19970714";

        public static TheoryData<IcalContentLine, IcalValueType> ContentLines() => new TheoryData<IcalContentLine, IcalValueType>
        {
            { new IcalContentLine(DtStart, new[] { new IcalContentLineParameter("value", DateValueTypeName) }, Rfc5545DateExample), IcalValueType.Date },
            { new IcalContentLine(DtStart, "19970714T133000"), IcalValueType.DateTime },
            { new IcalContentLine(ExtensionProperty, new[] { new IcalContentLineParameter("VALUE", DateValueTypeName) }, Rfc5545DateExample), IcalValueType.Unknown },
        };

        // ref. RFC 5545 §3.2.20: the value type names that the VALUE parameter takes.
        public static TheoryData<IcalValueType, string> ValueTypeNames() => new TheoryData<IcalValueType, string>
        {
            { IcalValueType.Binary, "BINARY" },
            { IcalValueType.Boolean, "BOOLEAN" },
            { IcalValueType.CalAddress, "CAL-ADDRESS" },
            { IcalValueType.Date, DateValueTypeName },
            { IcalValueType.DateTime, "DATE-TIME" },
            { IcalValueType.Duration, "DURATION" },
            { IcalValueType.Float, "FLOAT" },
            { IcalValueType.Integer, "INTEGER" },
            { IcalValueType.Period, "PERIOD" },
            { IcalValueType.Recur, "RECUR" },
            { IcalValueType.Text, "TEXT" },
            { IcalValueType.Time, "TIME" },
            { IcalValueType.Uri, "URI" },
            { IcalValueType.UtcOffset, "UTC-OFFSET" },
        };

        [Theory]
        [InlineData(DtStart, IcalValueType.DateTime)]
        [InlineData("dtstart", IcalValueType.DateTime)]
        [InlineData("SUMMARY", IcalValueType.Text)]
        [InlineData(Attach, IcalValueType.Uri)]
        [InlineData("ATTENDEE", IcalValueType.CalAddress)]
        [InlineData("GEO", IcalValueType.Float)]
        [InlineData("PRIORITY", IcalValueType.Integer)]
        [InlineData("DURATION", IcalValueType.Duration)]
        [InlineData("FREEBUSY", IcalValueType.Period)]
        [InlineData("RRULE", IcalValueType.Recur)]
        [InlineData("TZOFFSETFROM", IcalValueType.UtcOffset)]
        [InlineData(Trigger, IcalValueType.Duration)]
        [InlineData("COLOR", IcalValueType.Text)]
        [InlineData("REFRESH-INTERVAL", IcalValueType.Duration)]
        [InlineData("CONFERENCE", IcalValueType.Uri)]
        [InlineData("X-WR-CALNAME", IcalValueType.Unknown)]
        [InlineData("NEW-IANA-PROPERTY", IcalValueType.Unknown)]
        [InlineData("", IcalValueType.Unknown)]
        public void GetDefaultValueType_Property_ReturnsDefault(string propertyName, IcalValueType expected)
        {
            // act
            var testResult = IcalValueTypeRegistry.GetDefaultValueType(propertyName.AsSpan());

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(DtStart, true)]
        [InlineData("dtstart", true)]
        [InlineData("SUMMARY", true)]
        [InlineData("COLOR", true)]
        [InlineData("CONFERENCE", true)]
        [InlineData("X-WR-CALNAME", false)]
        [InlineData("NEW-IANA-PROPERTY", false)]
        [InlineData("", false)]
        public void IsKnownProperty_Property_ReturnsWhetherKnown(string propertyName, bool expected)
        {
            // act
            var testResult = IcalValueTypeRegistry.IsKnownProperty(propertyName.AsSpan());

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(DtStart, DateValueTypeName, IcalValueType.Date)]
        [InlineData(DtStart, "date", IcalValueType.Date)]
        [InlineData(Attach, "BINARY", IcalValueType.Binary)]
        [InlineData(RDate, "PERIOD", IcalValueType.Period)]
        [InlineData(Trigger, "DATE-TIME", IcalValueType.DateTime)]
        [InlineData(DtStart, "X-CUSTOM", IcalValueType.Unknown)]
        [InlineData(DtStart, "", IcalValueType.Unknown)]
        [InlineData(ExtensionProperty, DateValueTypeName, IcalValueType.Unknown)]
        [InlineData(DtStart, "INTEGER", IcalValueType.Integer)]
        public void GetValueType_ValueParameter_Overrides(string propertyName, string valueParameter, IcalValueType expected)
        {
            // act
            var testResult = IcalValueTypeRegistry.GetValueType(propertyName.AsSpan(), valueParameter.AsSpan());

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData(DtStart, IcalValueType.DateTime, true)]
        [InlineData(DtStart, IcalValueType.Date, true)]
        [InlineData(DtStart, IcalValueType.Integer, false)]
        [InlineData(RDate, IcalValueType.Period, true)]
        [InlineData("EXDATE", IcalValueType.Period, false)]
        [InlineData("IMAGE", IcalValueType.Binary, true)]
        [InlineData(ExtensionProperty, IcalValueType.Text, false)]
        public void IsAllowedValueType_Property_ReturnsWhetherAllowed(string propertyName, IcalValueType valueType, bool expected)
        {
            // act
            var testResult = IcalValueTypeRegistry.IsAllowedValueType(propertyName.AsSpan(), valueType);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(ContentLines), DisableDiscoveryEnumeration = true)]
        public void GetValueType_ContentLine_UsesValueParameter(IcalContentLine line, IcalValueType expected)
        {
            // act
            var testResult = IcalValueTypeRegistry.GetValueType(line);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("DTSTART;VALUE=DATE:19970714\r\n", IcalValueType.Date)]
        [InlineData("DTSTART:19970714T133000\r\n", IcalValueType.DateTime)]
        [InlineData("X-PROP;VALUE=DATE:19970714\r\n", IcalValueType.Unknown)]
        public void GetValueType_View_UsesValueParameter(string text, IcalValueType expected)
        {
            // arrange
            using (var reader = new IcalContentLineReader(new MemoryStream(Encoding.UTF8.GetBytes(text))))
            {
                reader.ReadContentLine();

                // act
                var testResult = IcalValueTypeRegistry.GetValueType(reader.CurrentLine);

                // assert
                testResult.Should().Be(expected);
            }
        }

        [Theory]
        [MemberData(nameof(ValueTypeNames))]
        public void GetValueTypeName_ValueType_ReturnsRfc5545Name(IcalValueType valueType, string expected)
        {
            // act
            var testResult = IcalValueTypeRegistry.GetValueTypeName(valueType);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(ValueTypeNames))]
        public void TryParseValueTypeName_Rfc5545Name_ReturnsValueType(IcalValueType expected, string name)
        {
            // act
            var testResult = IcalValueTypeRegistry.TryParseValueTypeName(name.AsSpan(), out var parsed);

            // assert
            testResult.Should().BeTrue();
            parsed.Should().Be(expected);
        }

        [Fact]
        public void GetValueTypeName_Unknown_ThrowsArgumentOutOfRange()
        {
            // act
            Action testAction = () => IcalValueTypeRegistry.GetValueTypeName(IcalValueType.Unknown);

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }
    }
}
