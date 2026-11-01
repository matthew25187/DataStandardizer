using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalValueFormatTests
    {
        // A value of IcalValueFormat that names no form.
        private const IcalValueFormat UndefinedFormat = (IcalValueFormat)99;

        private const string ExampleUri = "http://example.com/";
        private const string ExampleCalAddress = "mailto:a@example.com";

        // ref. RFC 5545 §3.8.5.3: daily for 10 occurrences.
        private const string DailyForTenOccurrencesRule = "FREQ=DAILY;COUNT=10";

        // ref. RFC 7265 §3.6.9, RFC 6321 §3.6.9: Rfc5545Examples.StartPeriod in the jCal and xCal forms, which are the same.
        private const string JsonAndXmlStartPeriod = "1997-01-01T18:00:00Z/PT5H30M";

        public static TheoryData<string, Action> TextFormParseCalls => BindFormat(Parsers(), IcalValueFormat.Text, true);

        public static TheoryData<string, Action> JsonFormParseCalls => BindFormat(Parsers(), IcalValueFormat.Json, false);

        public static TheoryData<string, Action> XmlFormParseCalls => BindFormat(Parsers(), IcalValueFormat.Xml, false);

        public static TheoryData<string, Action> UndefinedFormParseCalls => BindFormat(Parsers(), UndefinedFormat, true);

        public static TheoryData<string, Action> TextFormFormatCalls => BindFormat(Formatters(), IcalValueFormat.Text, true);

        public static TheoryData<string, Action> JsonFormFormatCalls => BindFormat(Formatters(), IcalValueFormat.Json, false);

        public static TheoryData<string, Action> XmlFormFormatCalls => BindFormat(Formatters(), IcalValueFormat.Xml, false);

        public static TheoryData<string, Action> UndefinedFormFormatCalls => BindFormat(Formatters(), UndefinedFormat, true);

        public static TheoryData<Action> RecurrenceRuleJsonAndXmlCalls => new TheoryData<Action>
        {
            () => IcalRecurrenceRule.TryParse("FREQ=DAILY".AsSpan(), IcalValueFormat.Json, out _),
            () => IcalRecurrenceRule.TryParse("FREQ=DAILY".AsSpan(), IcalValueFormat.Xml, out _),
            () => new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).TryFormat(new char[64], out _, IcalValueFormat.Json),
            () => new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).TryFormat(new char[64], out _, IcalValueFormat.Xml),
        };

        public static TheoryData<Action> InvalidValueParseCalls => new TheoryData<Action>
        {
            () => IcalDate.Parse("20260230".AsSpan(), IcalValueFormat.Text),
            () => IcalTime.Parse("250000".AsSpan(), IcalValueFormat.Text),
            () => IcalDateTime.Parse("20260230T000000".AsSpan(), IcalValueFormat.Text),
            () => IcalUtcOffset.Parse("-0000".AsSpan(), IcalValueFormat.Text),
            () => IcalDuration.Parse("P1H".AsSpan(), IcalValueFormat.Text),
            () => IcalPeriod.Parse(Rfc5545Examples.PeriodStart.AsSpan(), IcalValueFormat.Text),
            () => IcalRecurrenceRule.Parse("COUNT=1".AsSpan(), IcalValueFormat.Text),
        };

        [Theory]
        [MemberData(nameof(TextFormParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_TextForm_IsSupported(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().NotThrow(valueType);
        }

        [Theory]
        [MemberData(nameof(JsonFormParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_JsonForm_IsSupported(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().NotThrow(valueType);
        }

        [Theory]
        [MemberData(nameof(XmlFormParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_XmlForm_IsSupported(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().NotThrow(valueType);
        }

        [Theory]
        [MemberData(nameof(UndefinedFormParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_UndefinedForm_ThrowsArgumentOutOfRange(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>(valueType);
        }

        [Theory]
        [MemberData(nameof(TextFormFormatCalls), DisableDiscoveryEnumeration = true)]
        public void TryFormat_TextForm_IsSupported(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().NotThrow(valueType);
        }

        [Theory]
        [MemberData(nameof(JsonFormFormatCalls), DisableDiscoveryEnumeration = true)]
        public void TryFormat_JsonForm_IsSupported(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().NotThrow(valueType);
        }

        [Theory]
        [MemberData(nameof(XmlFormFormatCalls), DisableDiscoveryEnumeration = true)]
        public void TryFormat_XmlForm_IsSupported(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().NotThrow(valueType);
        }

        [Theory]
        [MemberData(nameof(UndefinedFormFormatCalls), DisableDiscoveryEnumeration = true)]
        public void TryFormat_UndefinedForm_ThrowsArgumentOutOfRange(string valueType, Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>(valueType);
        }

        // A recurrence rule has no single lexical form in jCal or xCal.
        [Theory]
        [MemberData(nameof(RecurrenceRuleJsonAndXmlCalls), DisableDiscoveryEnumeration = true)]
        public void TryParseAndTryFormat_RecurrenceRuleInJsonOrXmlForm_ThrowsNotSupported(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void ToString_InterpolatedString_UsesTextForm()
        {
            // arrange
            var dateTime = IcalDateTime.Parse(Rfc5545Examples.UtcDateTime.AsSpan(), IcalValueFormat.Text);
            var duration = IcalDuration.Parse(Rfc5545Examples.Duration.AsSpan(), IcalValueFormat.Text);
            var rule = IcalRecurrenceRule.Parse(DailyForTenOccurrencesRule.AsSpan(), IcalValueFormat.Text);
            var offset = new IcalUtcOffset(TimeSpan.FromHours(-5));
            var date = new IcalDate(1997, 7, 14);
            var time = new IcalTime(7, 0, 0, true);

            // act
            var testResult = $"{dateTime}|{duration:T}|{rule}|{offset}|{date}|{time}";

            // assert
            testResult.Should().Be("19980119T070000Z|P15DT5H0M20S|FREQ=DAILY;COUNT=10|-0500|19970714|070000Z");
        }

#if NET6_0_OR_GREATER
        [Fact]
        public void TryFormat_TextFormatString_WritesTextForm()
        {
            // arrange
            ISpanFormattable period = IcalPeriod.Parse(Rfc5545Examples.StartPeriod.AsSpan(), IcalValueFormat.Text);
            var destination = new char[64];

            // act
            var testResult = period.TryFormat(destination, out var charsWritten, "t", null);

            // assert
            testResult.Should().BeTrue();
            new string(destination, 0, charsWritten).Should().Be(Rfc5545Examples.StartPeriod);
        }

        [Theory]
        [InlineData("t", Rfc5545Examples.StartPeriod)]
        [InlineData("J", JsonAndXmlStartPeriod)]
        [InlineData("x", JsonAndXmlStartPeriod)]
        public void ToString_FormatString_SelectsTheForm(string format, string expected)
        {
            // arrange
            ISpanFormattable period = IcalPeriod.Parse(Rfc5545Examples.StartPeriod.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = period.ToString(format, null);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void ToString_UnknownFormatString_ThrowsFormatException()
        {
            // arrange
            ISpanFormattable period = IcalPeriod.Parse(Rfc5545Examples.StartPeriod.AsSpan(), IcalValueFormat.Text);

            // act
            Action testAction = () => period.ToString("G", null);

            // assert
            testAction.Should().Throw<FormatException>();
        }
#endif

        [Theory]
        [MemberData(nameof(InvalidValueParseCalls), DisableDiscoveryEnumeration = true)]
        public void Parse_InvalidValue_ThrowsFormatException(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<FormatException>();
        }

        /// <summary>
        /// Gets a parse of each value type, in a form that the caller gives.
        /// </summary>
        private static List<KeyValuePair<string, Action<IcalValueFormat>>> Parsers() => new List<KeyValuePair<string, Action<IcalValueFormat>>>
        {
            Call(nameof(IcalDate), format => IcalDate.TryParse(Rfc5545Examples.Date.AsSpan(), format, out _)),
            Call(nameof(IcalTime), format => IcalTime.TryParse("230000".AsSpan(), format, out _)),
            Call(nameof(IcalDateTime), format => IcalDateTime.TryParse(Rfc5545Examples.FloatingDateTime.AsSpan(), format, out _)),
            Call("IcalDateTime zoned", format => IcalDateTime.TryParse(Rfc5545Examples.FloatingDateTime.AsSpan(), "Europe/London", format, out _)),
            Call(nameof(IcalUtcOffset), format => IcalUtcOffset.TryParse(Rfc5545Examples.NegativeUtcOffset.AsSpan(), format, out _)),
            Call(nameof(IcalDuration), format => IcalDuration.TryParse("P1D".AsSpan(), format, out _)),
            Call(nameof(IcalPeriod), format => IcalPeriod.TryParse(Rfc5545Examples.StartPeriod.AsSpan(), format, out _)),
            Call(nameof(IcalRecurrenceRule), format => IcalRecurrenceRule.TryParse("FREQ=DAILY".AsSpan(), format, out _)),
            Call("Text", format => IcalValue.TryParseText("a".AsSpan(), format, out _)),
            Call("Text list", format => IcalValue.EnumerateTextList("a,b".AsSpan(), format)),
            Call("Boolean", format => IcalValue.TryParseBoolean("TRUE".AsSpan(), format, out _)),
            Call("Integer", format => IcalValue.TryParseInteger("1".AsSpan(), format, out _)),
            Call("Float", format => IcalValue.TryParseFloat("1.5".AsSpan(), format, out _)),
            Call("Binary", format => IcalValue.TryParseBinary("AAAA".AsSpan(), format, out _)),
            Call("Uri", format => IcalValue.TryParseUri(ExampleUri.AsSpan(), format, out _)),
            Call("CalAddress", format => IcalValue.TryParseCalAddress(ExampleCalAddress.AsSpan(), format, out _)),
        };

        /// <summary>
        /// Gets a format of each value type, in a form that the caller gives.
        /// </summary>
        private static List<KeyValuePair<string, Action<IcalValueFormat>>> Formatters() => new List<KeyValuePair<string, Action<IcalValueFormat>>>
        {
            Call(nameof(IcalDate), format => new IcalDate(1997, 7, 14).TryFormat(new char[64], out _, format)),
            Call(nameof(IcalTime), format => new IcalTime(23, 0, 0).TryFormat(new char[64], out _, format)),
            Call(nameof(IcalDateTime), format => default(IcalDateTime).TryFormat(new char[64], out _, format)),
            Call(nameof(IcalUtcOffset), format => default(IcalUtcOffset).TryFormat(new char[64], out _, format)),
            Call(nameof(IcalDuration), format => default(IcalDuration).TryFormat(new char[64], out _, format)),
            Call(nameof(IcalPeriod), format => default(IcalPeriod).TryFormat(new char[64], out _, format)),
            Call(nameof(IcalRecurrenceRule), format => new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).TryFormat(new char[64], out _, format)),
            Call("Text", format => IcalValue.TryFormatText("a".AsSpan(), new char[64], out _, format)),
            Call("Boolean", format => IcalValue.TryFormatBoolean(true, new char[64], out _, format)),
            Call("Integer", format => IcalValue.TryFormatInteger(1, new char[64], out _, format)),
            Call("Float", format => IcalValue.TryFormatFloat(1.5, new char[64], out _, format)),
            Call("Binary", format => IcalValue.TryFormatBinary(new byte[3], new char[64], out _, format)),
            Call("Uri", format => IcalValue.TryFormatUri(new Uri(ExampleUri), new char[64], out _, format)),
            Call("CalAddress", format => IcalValue.TryFormatCalAddress(new Uri(ExampleCalAddress), new char[64], out _, format)),
        };

        private static KeyValuePair<string, Action<IcalValueFormat>> Call(string valueType, Action<IcalValueFormat> call)
        {
            return new KeyValuePair<string, Action<IcalValueFormat>>(valueType, call);
        }

        /// <summary>
        /// Makes a case of each call in one form.  A recurrence rule has no single lexical form in jCal or xCal, so it has its own test there.
        /// </summary>
        private static TheoryData<string, Action> BindFormat(List<KeyValuePair<string, Action<IcalValueFormat>>> calls, IcalValueFormat format,
            bool includeRecurrenceRule)
        {
            var data = new TheoryData<string, Action>();
            foreach (var call in calls)
            {
                if (includeRecurrenceRule || call.Key != nameof(IcalRecurrenceRule))
                {
                    var bound = call.Value;
                    data.Add(call.Key, () => bound(format));
                }
            }

            return data;
        }
    }
}
