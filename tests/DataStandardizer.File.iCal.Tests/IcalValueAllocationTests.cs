using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Checks that parsing a value from a span allocates nothing on the heap.
    /// </summary>
    public class IcalValueAllocationTests
    {
        private const int Iterations = 1000;

        // A rule with every BY part, to parse as many parts as possible.
        private const string EveryPartRule = "FREQ=YEARLY;BYYEARDAY=1,100,-200;BYWEEKNO=20,-1;BYMONTHDAY=1,-1;BYSETPOS=-1;BYSECOND=0,60;WKST=SU";

        // Each call parses a value from a span that the factory made before the test runs.
        public static TheoryData<Action> DateTimeParseCalls()
        {
            var floating = Rfc5545Examples.FloatingDateTime.ToCharArray();
            var utc = Rfc5545Examples.UtcDateTime.ToCharArray();
            return new TheoryData<Action>
            {
                () => IcalDateTime.TryParse(floating, IcalValueFormat.Text, out _),
                () => IcalDateTime.TryParse(utc, IcalValueFormat.Text, out _),
                () => IcalDateTime.TryParse(floating, Rfc5545Examples.NewYorkTimeZoneId, IcalValueFormat.Text, out _),
            };
        }

        public static TheoryData<Action> RecurrenceRuleParseCalls()
        {
            var rule = Rfc5545Examples.RecurrenceRule.ToCharArray();
            var monthlyRule = Rfc5545Examples.MonthlyFirstAndLastSundayRule.ToCharArray();
            var untilRule = Rfc5545Examples.EveryDayInJanuaryRule.ToCharArray();
            var everyPartRule = EveryPartRule.ToCharArray();
            return new TheoryData<Action>
            {
                () => IcalRecurrenceRule.TryParse(rule, IcalValueFormat.Text, out _),
                () => IcalRecurrenceRule.TryParse(monthlyRule, IcalValueFormat.Text, out _),
                () => IcalRecurrenceRule.TryParse(untilRule, IcalValueFormat.Text, out _),
                () => IcalRecurrenceRule.TryParse(everyPartRule, IcalValueFormat.Text, out _),
            };
        }

        public static TheoryData<Action> OtherParseCalls()
        {
            var date = Rfc5545Examples.Date.ToCharArray();
            var time = Rfc5545Examples.UtcTime.ToCharArray();
            var offset = Rfc5545Examples.NegativeUtcOffset.ToCharArray();
            var duration = Rfc5545Examples.Duration.ToCharArray();
            var period = Rfc5545Examples.StartPeriod.ToCharArray();
            var text = "a\\,b\\nc".ToCharArray();
            var textList = "a\\,b,c,d".ToCharArray();
            var integer = Rfc5545Examples.NegativeInteger.ToCharArray();
            var boolean = "TRUE".ToCharArray();
            var destination = new char[16];
            return new TheoryData<Action>
            {
                () => IcalDate.TryParse(date, IcalValueFormat.Text, out _),
                () => IcalTime.TryParse(time, IcalValueFormat.Text, out _),
                () => IcalUtcOffset.TryParse(offset, IcalValueFormat.Text, out _),
                () => IcalDuration.TryParse(duration, IcalValueFormat.Text, out _),
                () => IcalPeriod.TryParse(period, IcalValueFormat.Text, out _),
                () => IcalValue.TryParseText(text, IcalValueFormat.Text, destination, out _),
                () => IcalValue.EnumerateTextList(textList, IcalValueFormat.Text).MoveNext(),
                () => IcalValue.TryParseInteger(integer, IcalValueFormat.Text, out _),
                () => IcalValue.TryParseBoolean(boolean, IcalValueFormat.Text, out _),
            };
        }

        public static TheoryData<Action> FormatCalls()
        {
            var dateTime = IcalDateTime.Parse(Rfc5545Examples.UtcDateTime.AsSpan(), IcalValueFormat.Text);
            var period = IcalPeriod.Parse(Rfc5545Examples.StartPeriod.AsSpan(), IcalValueFormat.Text);
            var rule = IcalRecurrenceRule.Parse(Rfc5545Examples.MonthlyFirstAndLastSundayRule.AsSpan(), IcalValueFormat.Text);
            var text = "a,b;c".ToCharArray();
            var destination = new char[128];
            return new TheoryData<Action>
            {
                () => dateTime.TryFormat(destination, out _, IcalValueFormat.Text),
                () => period.TryFormat(destination, out _, IcalValueFormat.Text),
                () => rule.TryFormat(destination, out _, IcalValueFormat.Text),
                () => IcalValue.TryFormatText(text, destination, out _, IcalValueFormat.Text),
                () => IcalValue.TryFormatInteger(-42, destination, out _, IcalValueFormat.Text),
            };
        }

        [Theory]
        [MemberData(nameof(DateTimeParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_DateTimeFromSpan_DoesNotAllocate(Action parseCall)
        {
            // act
            var testResult = MeasureAllocations(parseCall);

            // assert
            testResult.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(RecurrenceRuleParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_RecurrenceRuleFromSpan_DoesNotAllocate(Action parseCall)
        {
            // act
            var testResult = MeasureAllocations(parseCall);

            // assert
            testResult.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(OtherParseCalls), DisableDiscoveryEnumeration = true)]
        public void TryParse_OtherValueFromSpan_DoesNotAllocate(Action parseCall)
        {
            // act
            var testResult = MeasureAllocations(parseCall);

            // assert
            testResult.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(FormatCalls), DisableDiscoveryEnumeration = true)]
        public void TryFormat_IntoSpan_DoesNotAllocate(Action formatCall)
        {
            // act
            var testResult = MeasureAllocations(formatCall);

            // assert
            testResult.Should().Be(0);
        }

        /// <summary>
        /// Runs an action once to warm it up, then measures the bytes it allocates over many runs.
        /// </summary>
        private static long MeasureAllocations(Action action)
        {
            action();

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                action();
            }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
    }
}
