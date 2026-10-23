using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
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

        [Fact]
        public void TryParse_DateTime_DoesNotAllocate()
        {
            // arrange
            var floating = "19980118T230000".ToCharArray();
            var utc = "19980119T070000Z".ToCharArray();
            const string timeZoneId = "America/New_York";

            // act
            var testResult = MeasureAllocations(() =>
            {
                IcalDateTime.TryParse(floating, IcalValueFormat.Text, out _);
                IcalDateTime.TryParse(utc, IcalValueFormat.Text, out _);
                IcalDateTime.TryParse(floating, timeZoneId, IcalValueFormat.Text, out _);
            });

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void TryParse_RecurrenceRule_DoesNotAllocate()
        {
            // arrange
            var rules = new[]
            {
                "FREQ=YEARLY;INTERVAL=2;BYMONTH=1;BYDAY=SU;BYHOUR=8,9;BYMINUTE=30".ToCharArray(),
                "FREQ=MONTHLY;INTERVAL=2;COUNT=10;BYDAY=1SU,-1SU".ToCharArray(),
                "FREQ=YEARLY;UNTIL=20000131T140000Z;BYMONTH=1;BYDAY=SU,MO,TU,WE,TH,FR,SA".ToCharArray(),
                "FREQ=YEARLY;BYYEARDAY=1,100,-200;BYWEEKNO=20,-1;BYMONTHDAY=1,-1;BYSETPOS=-1;BYSECOND=0,60;WKST=SU".ToCharArray()
            };

            // act
            var testResult = MeasureAllocations(() =>
            {
                foreach (var rule in rules)
                {
                    IcalRecurrenceRule.TryParse(rule, IcalValueFormat.Text, out _);
                }
            });

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void TryParse_OtherSpanValues_DoNotAllocate()
        {
            // arrange
            var date = "19970714".ToCharArray();
            var time = "070000Z".ToCharArray();
            var offset = "-0500".ToCharArray();
            var duration = "P15DT5H0M20S".ToCharArray();
            var period = "19970101T180000Z/PT5H30M".ToCharArray();
            var text = "a\\,b\\nc".ToCharArray();
            var textList = "a\\,b,c,d".ToCharArray();
            var integer = "-1234567890".ToCharArray();
            var boolean = "TRUE".ToCharArray();
            var destination = new char[16];

            // act
            var testResult = MeasureAllocations(() =>
            {
                IcalDate.TryParse(date, IcalValueFormat.Text, out _);
                IcalTime.TryParse(time, IcalValueFormat.Text, out _);
                IcalUtcOffset.TryParse(offset, IcalValueFormat.Text, out _);
                IcalDuration.TryParse(duration, IcalValueFormat.Text, out _);
                IcalPeriod.TryParse(period, IcalValueFormat.Text, out _);
                IcalValue.TryParseText(text, IcalValueFormat.Text, destination, out _);
                foreach (var item in IcalValue.EnumerateTextList(textList, IcalValueFormat.Text))
                {
                    IcalValue.TryParseText(item, IcalValueFormat.Text, destination, out _);
                }

                IcalValue.TryParseInteger(integer, IcalValueFormat.Text, out _);
                IcalValue.TryParseBoolean(boolean, IcalValueFormat.Text, out _);
            });

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void TryFormat_IntoSpan_DoesNotAllocate()
        {
            // arrange
            var dateTime = IcalDateTime.Parse("19980119T070000Z".AsSpan(), IcalValueFormat.Text);
            var period = IcalPeriod.Parse("19970101T180000Z/PT5H30M".AsSpan(), IcalValueFormat.Text);
            var rule = IcalRecurrenceRule.Parse("FREQ=MONTHLY;INTERVAL=2;COUNT=10;BYDAY=1SU,-1SU".AsSpan(), IcalValueFormat.Text);
            var destination = new char[128];

            // act
            var testResult = MeasureAllocations(() =>
            {
                dateTime.TryFormat(destination, out _, IcalValueFormat.Text);
                period.TryFormat(destination, out _, IcalValueFormat.Text);
                rule.TryFormat(destination, out _, IcalValueFormat.Text);
                IcalValue.TryFormatText("a,b;c".AsSpan(), destination, out _, IcalValueFormat.Text);
                IcalValue.TryFormatInteger(-42, destination, out _, IcalValueFormat.Text);
            });

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
