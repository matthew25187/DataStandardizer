using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalRecurrenceRuleTests
    {
        // ref. RFC 5545 §3.3.10: Rfc5545Examples.RecurrenceRule with its parts in the order the writer gives them.
        private const string CanonicalRfcRecurrenceRule = "FREQ=YEARLY;INTERVAL=2;BYMINUTE=30;BYHOUR=8,9;BYDAY=SU;BYMONTH=1";

        // ref. RFC 5545 §3.8.5.3: daily for 10 occurrences.
        private const string DailyForTenOccurrences = "FREQ=DAILY;COUNT=10";

        // The length of DailyForTenOccurrences, less one.
        private const int ShorterThanDailyForTenOccurrences = 18;

        // ref. RFC 5545 §3.8.5.3: daily until December 24, 1997.
        private const string DailyUntilDecember24 = "FREQ=DAILY;UNTIL=19971224T000000Z";

        // ref. RFC 5545 §3.8.5.3: monthly on the first and last day of the month for 10 occurrences.
        private const string MonthlyOnFirstAndLastDay = "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=1,-1";

        // ref. RFC 5545 §3.8.5.3: Monday of week number 20.
        private const string MondayOfWeek20 = "FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO";

        // ref. RFC 5545 §3.6.5: the last Sunday in October, when daylight saving time ends in the examples.
        private const string LastSundayOfOctober = "FREQ=YEARLY;BYDAY=-1SU;BYMONTH=10";

        private const string Daily = "FREQ=DAILY";
        private const string DailyWithIntervalOfOne = "FREQ=DAILY;INTERVAL=1";
        private const string DailyUntilDate = "FREQ=DAILY;UNTIL=19971224";

        // The number of days in a leap year, which is the largest BYYEARDAY value (RFC 5545 §3.3.10).
        private const int DaysInLeapYear = 366;

        /// <summary>
        /// The rules of RFC 5545 §3.3.10 and the examples of §3.8.5.3, and the VTIMEZONE examples of §3.6.5, that are already written in
        /// canonical order.
        /// </summary>
        public static TheoryData<string> CanonicalRfcRules => new TheoryData<string>()
        {
            "FREQ=DAILY;COUNT=10;INTERVAL=2",
            DailyForTenOccurrences,
            DailyUntilDecember24,
            "FREQ=DAILY;INTERVAL=2",
            "FREQ=DAILY;UNTIL=20000131T140000Z;BYMONTH=1",
            "FREQ=WEEKLY;COUNT=10",
            "FREQ=WEEKLY;UNTIL=19971224T000000Z",
            "FREQ=WEEKLY;INTERVAL=2;WKST=SU",
            "FREQ=MONTHLY;COUNT=10;BYDAY=1FR",
            "FREQ=MONTHLY;UNTIL=19971224T000000Z;BYDAY=1FR",
            "FREQ=MONTHLY;COUNT=6;BYDAY=-2MO",
            "FREQ=MONTHLY;BYMONTHDAY=-3",
            "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=2,15",
            MonthlyOnFirstAndLastDay,
            "FREQ=MONTHLY;INTERVAL=2;BYDAY=TU",
            "FREQ=YEARLY;COUNT=10;BYMONTH=6,7",
            "FREQ=YEARLY;BYDAY=20MO",
            "FREQ=YEARLY;BYDAY=TH;BYMONTH=6,7,8",
            "FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13",
            "FREQ=MONTHLY;BYDAY=SA;BYMONTHDAY=7,8,9,10,11,12,13",
            "FREQ=MONTHLY;COUNT=3;BYDAY=TU,WE,TH;BYSETPOS=3",
            "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-2",
            "FREQ=MINUTELY;INTERVAL=20;BYHOUR=9,10,11,12,13,14,15,16",
            LastSundayOfOctober,        };

        /// <summary>
        /// The other rules of RFC 5545 §3.3.10, §3.8.5.3 and §3.6.5, with each written in canonical order.
        /// </summary>
        public static TheoryData<string, string> NonCanonicalRfcRules => new TheoryData<string, string>()
        {
            { Rfc5545Examples.RecurrenceRule, "FREQ=YEARLY;INTERVAL=2;BYMINUTE=30;BYHOUR=8,9;BYDAY=SU;BYMONTH=1" },
            { "FREQ=DAILY;INTERVAL=10;COUNT=5", "FREQ=DAILY;COUNT=5;INTERVAL=10" },
            { Rfc5545Examples.EveryDayInJanuaryRule, "FREQ=YEARLY;UNTIL=20000131T140000Z;BYDAY=SU,MO,TU,WE,TH,FR,SA;BYMONTH=1" },
            { "FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH", "FREQ=WEEKLY;UNTIL=19971007T000000Z;BYDAY=TU,TH;WKST=SU" },
            { "FREQ=WEEKLY;COUNT=10;WKST=SU;BYDAY=TU,TH", "FREQ=WEEKLY;COUNT=10;BYDAY=TU,TH;WKST=SU" },
            { "FREQ=WEEKLY;INTERVAL=2;UNTIL=19971224T000000Z;WKST=SU;BYDAY=MO,WE,FR", "FREQ=WEEKLY;UNTIL=19971224T000000Z;INTERVAL=2;BYDAY=MO,WE,FR;WKST=SU" },
            { "FREQ=WEEKLY;INTERVAL=2;COUNT=8;WKST=SU;BYDAY=TU,TH", "FREQ=WEEKLY;COUNT=8;INTERVAL=2;BYDAY=TU,TH;WKST=SU" },
            { Rfc5545Examples.MonthlyFirstAndLastSundayRule, "FREQ=MONTHLY;COUNT=10;INTERVAL=2;BYDAY=1SU,-1SU" },
            { "FREQ=MONTHLY;INTERVAL=18;COUNT=10;BYMONTHDAY=10,11,12,13,14,15", "FREQ=MONTHLY;COUNT=10;INTERVAL=18;BYMONTHDAY=10,11,12,13,14,15" },
            { "FREQ=YEARLY;INTERVAL=2;COUNT=10;BYMONTH=1,2,3", "FREQ=YEARLY;COUNT=10;INTERVAL=2;BYMONTH=1,2,3" },
            { "FREQ=YEARLY;INTERVAL=3;COUNT=10;BYYEARDAY=1,100,200", "FREQ=YEARLY;COUNT=10;INTERVAL=3;BYYEARDAY=1,100,200" },
            { MondayOfWeek20, "FREQ=YEARLY;BYDAY=MO;BYWEEKNO=20" },
            { "FREQ=YEARLY;BYMONTH=3;BYDAY=TH", "FREQ=YEARLY;BYDAY=TH;BYMONTH=3" },
            { "FREQ=YEARLY;INTERVAL=4;BYMONTH=11;BYDAY=TU;BYMONTHDAY=2,3,4,5,6,7,8", "FREQ=YEARLY;INTERVAL=4;BYDAY=TU;BYMONTHDAY=2,3,4,5,6,7,8;BYMONTH=11" },
            { "FREQ=HOURLY;INTERVAL=3;UNTIL=19970902T170000Z", "FREQ=HOURLY;UNTIL=19970902T170000Z;INTERVAL=3" },
            { "FREQ=MINUTELY;INTERVAL=15;COUNT=6", "FREQ=MINUTELY;COUNT=6;INTERVAL=15" },
            { "FREQ=MINUTELY;INTERVAL=90;COUNT=4", "FREQ=MINUTELY;COUNT=4;INTERVAL=90" },
            { "FREQ=DAILY;BYHOUR=9,10,11,12,13,14,15,16;BYMINUTE=0,20,40", "FREQ=DAILY;BYMINUTE=0,20,40;BYHOUR=9,10,11,12,13,14,15,16" },
            { "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO", "FREQ=WEEKLY;COUNT=4;INTERVAL=2;BYDAY=SU,TU;WKST=MO" },
            { "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=SU", "FREQ=WEEKLY;COUNT=4;INTERVAL=2;BYDAY=SU,TU;WKST=SU" },
            { "FREQ=MONTHLY;BYMONTHDAY=15,30;COUNT=5", "FREQ=MONTHLY;COUNT=5;BYMONTHDAY=15,30" },
            { "FREQ=YEARLY;BYDAY=1SU;BYMONTH=4;UNTIL=19980404T070000Z", "FREQ=YEARLY;UNTIL=19980404T070000Z;BYDAY=1SU;BYMONTH=4" },
            { "FREQ=YEARLY;BYDAY=-1SU;BYMONTH=4;UNTIL=19980404T070000Z", "FREQ=YEARLY;UNTIL=19980404T070000Z;BYDAY=-1SU;BYMONTH=4" },
            { "FREQ=YEARLY;BYMONTH=4;BYDAY=1SU;UNTIL=19730429T070000Z", "FREQ=YEARLY;UNTIL=19730429T070000Z;BYDAY=1SU;BYMONTH=4" },
            { "FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU;UNTIL=20061029T060000Z", "FREQ=YEARLY;UNTIL=20061029T060000Z;BYDAY=-1SU;BYMONTH=10" },
        };

        public static TheoryData<string> RfcRules()
        {
            var data = new TheoryData<string>();
            foreach (var row in (IEnumerable<object[]>)CanonicalRfcRules)
            {
                data.Add((string)row[0]);
            }

            foreach (var row in (IEnumerable<object[]>)NonCanonicalRfcRules)
            {
                data.Add((string)row[0]);
            }

            return data;
        }

        /// <summary>
        /// Gets a rule with every BYYEARDAY value, 1 to 366 and -1 to -366, and how it is written.
        /// </summary>
        public static TheoryData<int[], string> EveryYearDay()
        {
            var yearDays = new List<int>();
            for (var day = 1; day <= DaysInLeapYear; day++)
            {
                yearDays.Add(day);
            }

            for (var day = 1; day <= DaysInLeapYear; day++)
            {
                yearDays.Add(-day);
            }

            return new TheoryData<int[], string> { { yearDays.ToArray(), "FREQ=YEARLY;BYYEARDAY=" + string.Join(",", yearDays) } };
        }

        public static TheoryData<Action> OutOfRangeCalls()
        {
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly);
            return new TheoryData<Action>
            {
                () => rule.WithBySecond(61),
                () => rule.WithByMonthDay(0),
                () => rule.WithByYearDay(-367),
                () => rule.WithByWeekNo(54),
                () => rule.WithByMonth(13),
                () => rule.WithBySetPos(0),
                () => rule.WithCount(0),
                () => rule.WithInterval(0),
                () => rule.WithFrequency(0),
                () => new IcalRecurrenceDay(DayOfWeek.Monday, 54),
            };
        }

        public static TheoryData<Func<IcalRecurrenceRule, int[]>, int[]> RfcExampleByLists => new TheoryData<Func<IcalRecurrenceRule, int[]>, int[]>
        {
            { rule => rule.GetByMonth(), new[] { 1 } },
            { rule => rule.GetByHour(), new[] { 8, 9 } },
            { rule => rule.GetByMinute(), new[] { 30 } },
            { rule => rule.GetBySecond(), new int[0] },
            { rule => rule.GetByMonthDay(), new int[0] },
            { rule => rule.GetByYearDay(), new int[0] },
            { rule => rule.GetByWeekNo(), new int[0] },
            { rule => rule.GetBySetPos(), new int[0] },
        };

        #region Parsing and formatting

        [Theory]
        [MemberData(nameof(RfcRules))]
        public void TryParse_RfcExample_GivesValidRule(string value)
        {
            // act
            var testResult = IcalRecurrenceRule.TryParse(value.AsSpan(), IcalValueFormat.Text, out var rule);

            // assert
            testResult.Should().BeTrue();
            rule.IsValid.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(NonCanonicalRfcRules))]
        public void ToString_RfcExampleNotInCanonicalOrder_WritesCanonicalOrder(string value, string expected)
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(value.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.ToString(IcalValueFormat.Text);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(NonCanonicalRfcRules))]
        public void Equals_RfcExampleAndItsCanonicalOrder_IsTrue(string value, string canonical)
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(value.AsSpan(), IcalValueFormat.Text);
            var canonicalRule = IcalRecurrenceRule.Parse(canonical.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = canonicalRule.Equals(rule);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(CanonicalRfcRules))]
        [InlineData("FREQ=MONTHLY;COUNT=10;INTERVAL=2;BYDAY=1SU,-1SU")]
        [InlineData("FREQ=YEARLY;UNTIL=20000131T140000Z;BYDAY=SU,MO,TU,WE,TH,FR,SA;BYMONTH=1")]
        [InlineData("FREQ=WEEKLY;UNTIL=19971007;BYDAY=TU,TH;WKST=SU")]
        [InlineData("FREQ=YEARLY;BYSECOND=0,60;BYMINUTE=0,59;BYHOUR=0,23;BYDAY=MO,1TU,53TU,-1TU,-53TU;BYMONTHDAY=1,31,-1,-31;BYYEARDAY=1,366,-1,-366;BYMONTH=1,12;BYSETPOS=1,366,-1,-366;WKST=MO")]
        [InlineData("FREQ=YEARLY;BYWEEKNO=1,53,-1,-53")]
        public void ToString_CanonicalRule_IsWrittenAsItIs(string value)
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(value.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.ToString(IcalValueFormat.Text);

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void ToString_PartsInAnyOrder_WritesFrequencyFirst()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("BYMONTH=10;BYDAY=-1SU;FREQ=YEARLY".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.ToString();

            // assert
            testResult.Should().Be(LastSundayOfOctober);
        }

        [Fact]
        public void ToString_ListValues_AreSortedAndDeduplicated()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=MONTHLY;BYMONTHDAY=-2,15,-1,2,15;BYDAY=FR,MO,-1MO,1MO".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.ToString();

            // assert
            testResult.Should().Be("FREQ=MONTHLY;BYDAY=MO,1MO,-1MO,FR;BYMONTHDAY=2,15,-1,-2");
        }

        [Fact]
        public void ToString_ParsedWithLowerCaseAndPlusSigns_WritesCanonicalForm()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("freq=monthly;byday=+1mo;bymonthday=+5;until=19971224t000000z;wkst=su".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.ToString();

            // assert
            testResult.Should().Be("FREQ=MONTHLY;UNTIL=19971224T000000Z;BYDAY=1MO;BYMONTHDAY=5;WKST=SU");
        }

        [Fact]
        public void Parse_RfcExample_ExposesItsFrequencyIntervalAndEnd()
        {
            // act
            var testResult = IcalRecurrenceRule.Parse(Rfc5545Examples.RecurrenceRule.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.Frequency.Should().Be(IcalRecurrenceFrequency.Yearly);
            testResult.Interval.Should().Be(2);
            testResult.Count.Should().BeNull();
            testResult.UntilDate.Should().BeNull();
            testResult.UntilDateTime.Should().BeNull();
            testResult.WeekStart.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(RfcExampleByLists), DisableDiscoveryEnumeration = true)]
        public void GetByList_RfcExample_GivesItsValues(Func<IcalRecurrenceRule, int[]> getByList, int[] expected)
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(Rfc5545Examples.RecurrenceRule.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = getByList(rule);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void GetByDay_RfcExample_GivesSunday()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(Rfc5545Examples.RecurrenceRule.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetByDay();

            // assert
            testResult.Should().Equal(new IcalRecurrenceDay(DayOfWeek.Sunday));
        }

        [Fact]
        public void GetByDay_SignedValues_ArePositiveThenNegative()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;BYDAY=-1SU,2MO".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetByDay();

            // assert
            testResult.Should().Equal(new IcalRecurrenceDay(DayOfWeek.Sunday, -1), new IcalRecurrenceDay(DayOfWeek.Monday, 2));
        }

        [Fact]
        public void GetByYearDay_SignedValues_ArePositiveThenNegative()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;BYYEARDAY=-1,100,1".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetByYearDay();

            // assert
            testResult.Should().Equal(1, 100, -1);
        }

        [Fact]
        public void GetBySetPos_SignedValues_ArePositiveThenNegative()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;BYDAY=MO;BYSETPOS=-366,366".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetBySetPos();

            // assert
            testResult.Should().Equal(366, -366);
        }

        [Fact]
        public void Parse_UntilDate_IsADate()
        {
            // act
            var testResult = IcalRecurrenceRule.Parse(DailyUntilDate.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.UntilDate.Should().Be(new IcalDate(1997, 12, 24));
            testResult.UntilDateTime.Should().BeNull();
        }

        [Fact]
        public void ToString_UntilDate_WritesADate()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithUntil(new IcalDate(1997, 12, 24));

            // act
            var testResult = rule.ToString();

            // assert
            testResult.Should().Be(DailyUntilDate);
        }

        [Fact]
        public void Parse_UntilFloatingDateTime_IsFloating()
        {
            // act
            var testResult = IcalRecurrenceRule.Parse("FREQ=DAILY;UNTIL=19971224T000000".AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.UntilDateTime.Value.Kind.Should().Be(IcalDateTimeKind.Floating);
        }

        [Fact]
        public void Parse_IntervalOfOne_IsKept()
        {
            // act
            var testResult = IcalRecurrenceRule.Parse(DailyWithIntervalOfOne.AsSpan(), IcalValueFormat.Text);

            // assert
            testResult.Interval.Should().Be(1);
        }

        [Fact]
        public void ToString_IntervalOfOne_IsWritten()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithInterval(1);

            // act
            var testResult = rule.ToString();

            // assert
            testResult.Should().Be(DailyWithIntervalOfOne);
        }

        [Fact]
        public void Equals_IntervalOfOneAndNoInterval_IsFalse()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(DailyWithIntervalOfOne.AsSpan(), IcalValueFormat.Text);
            var other = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily);

            // act
            var testResult = rule.Equals(other);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormat_DestinationTooShort_ReturnsFalse()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(DailyForTenOccurrences.AsSpan(), IcalValueFormat.Text);
            var destination = new char[ShorterThanDailyForTenOccurrences];

            // act
            var testResult = rule.TryFormat(destination, out var charsWritten, IcalValueFormat.Text);

            // assert
            testResult.Should().BeFalse();
            charsWritten.Should().Be(0);
        }

        [Theory]
        [MemberData(nameof(EveryYearDay))]
        public void ToString_LongRule_GrowsItsBuffer(int[] yearDays, string expected)
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly).WithByYearDay(yearDays);

            // act
            var testResult = rule.ToString(IcalValueFormat.Text);

            // assert
            testResult.Should().Be(expected);
        }

        #endregion

        #region Invalid values

        [Theory]
        [InlineData("")]
        [InlineData("FREQ=")]
        [InlineData("FREQ=FORTNIGHTLY")]
        [InlineData("FREQ=DAILY;")]
        [InlineData(";FREQ=DAILY")]
        [InlineData("FREQ=DAILY;;COUNT=1")]
        [InlineData("FREQ=DAILY;COUNT")]
        [InlineData("FREQ=DAILY;=1")]
        [InlineData("FREQ=DAILY;FOO=1")]
        [InlineData("FREQ=DAILY;RSCALE=GREGORIAN")]
        [InlineData("FREQ = DAILY")]
        [InlineData("FREQ=DAILY;COUNT=0")]
        [InlineData("FREQ=DAILY;COUNT=-1")]
        [InlineData("FREQ=DAILY;COUNT=1.5")]
        [InlineData("FREQ=DAILY;COUNT=99999999999")]
        [InlineData("FREQ=DAILY;INTERVAL=0")]
        [InlineData("FREQ=DAILY;UNTIL=2026023")]
        [InlineData("FREQ=DAILY;UNTIL=20260230")]
        [InlineData("FREQ=DAILY;UNTIL=20260101T250000Z")]
        [InlineData("FREQ=DAILY;WKST=XX")]
        [InlineData("FREQ=DAILY;WKST=MO,TU")]
        [InlineData("FREQ=DAILY;BYSECOND=61")]
        [InlineData("FREQ=DAILY;BYSECOND=-1")]
        [InlineData("FREQ=DAILY;BYSECOND=005")]
        [InlineData("FREQ=DAILY;BYMINUTE=60")]
        [InlineData("FREQ=DAILY;BYHOUR=24")]
        [InlineData("FREQ=DAILY;BYHOUR=1,,2")]
        [InlineData("FREQ=DAILY;BYHOUR=1,")]
        [InlineData("FREQ=DAILY;BYMONTH=0")]
        [InlineData("FREQ=DAILY;BYMONTH=13")]
        [InlineData("FREQ=DAILY;BYMONTH=+1")]
        [InlineData("FREQ=MONTHLY;BYMONTHDAY=0")]
        [InlineData("FREQ=MONTHLY;BYMONTHDAY=32")]
        [InlineData("FREQ=MONTHLY;BYMONTHDAY=-32")]
        [InlineData("FREQ=MONTHLY;BYMONTHDAY=-")]
        [InlineData("FREQ=YEARLY;BYYEARDAY=0")]
        [InlineData("FREQ=YEARLY;BYYEARDAY=367")]
        [InlineData("FREQ=YEARLY;BYYEARDAY=-367")]
        [InlineData("FREQ=YEARLY;BYYEARDAY=0001")]
        [InlineData("FREQ=YEARLY;BYWEEKNO=0")]
        [InlineData("FREQ=YEARLY;BYWEEKNO=54")]
        [InlineData("FREQ=YEARLY;BYSETPOS=0;BYMONTH=1")]
        [InlineData("FREQ=YEARLY;BYSETPOS=367;BYMONTH=1")]
        [InlineData("FREQ=MONTHLY;BYDAY=0MO")]
        [InlineData("FREQ=MONTHLY;BYDAY=54MO")]
        [InlineData("FREQ=MONTHLY;BYDAY=-54MO")]
        [InlineData("FREQ=MONTHLY;BYDAY=001MO")]
        [InlineData("FREQ=MONTHLY;BYDAY=+MO")]
        [InlineData("FREQ=MONTHLY;BYDAY=M")]
        [InlineData("FREQ=MONTHLY;BYDAY=MONDAY")]
        [InlineData("FREQ=MONTHLY;BYDAY=1 MO")]
        public void TryParse_MalformedValue_Fails(string value)
        {
            // act
            var testResult = IcalRecurrenceRule.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [InlineData("FREQ=DAILY;FREQ=WEEKLY")]
        [InlineData("FREQ=DAILY;COUNT=1;COUNT=2")]
        [InlineData("FREQ=DAILY;INTERVAL=1;INTERVAL=2")]
        [InlineData("FREQ=DAILY;BYHOUR=1;BYHOUR=2")]
        [InlineData("FREQ=DAILY;BYDAY=MO;BYDAY=TU")]
        [InlineData("FREQ=DAILY;WKST=MO;WKST=SU")]
        [InlineData("FREQ=DAILY;UNTIL=19971224;until=19971225")]
        public void TryParse_RulePartTwice_Fails(string value)
        {
            // act
            var testResult = IcalRecurrenceRule.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().BeFalse();
        }

        #endregion

        #region §3.3.10 constraints

        [Theory]
        [InlineData("COUNT=10", false)]
        [InlineData("FREQ=DAILY;UNTIL=19971224T000000Z;COUNT=10", false)]
        [InlineData("FREQ=SECONDLY;BYDAY=1MO", false)]
        [InlineData("FREQ=MINUTELY;BYDAY=1MO", false)]
        [InlineData("FREQ=HOURLY;BYDAY=1MO", false)]
        [InlineData("FREQ=DAILY;BYDAY=1MO", false)]
        [InlineData("FREQ=WEEKLY;BYDAY=-1MO", false)]
        [InlineData("FREQ=WEEKLY;BYDAY=MO", true)]
        [InlineData("FREQ=MONTHLY;BYDAY=1MO", true)]
        [InlineData("FREQ=YEARLY;BYDAY=1MO", true)]
        [InlineData("FREQ=YEARLY;BYWEEKNO=20;BYDAY=1MO", false)]
        [InlineData(MondayOfWeek20, true)]
        [InlineData("FREQ=WEEKLY;BYMONTHDAY=1", false)]
        [InlineData("FREQ=DAILY;BYMONTHDAY=1", true)]
        [InlineData("FREQ=DAILY;BYYEARDAY=1", false)]
        [InlineData("FREQ=WEEKLY;BYYEARDAY=1", false)]
        [InlineData("FREQ=MONTHLY;BYYEARDAY=1", false)]
        [InlineData("FREQ=HOURLY;BYYEARDAY=1", true)]
        [InlineData("FREQ=YEARLY;BYYEARDAY=1", true)]
        [InlineData("FREQ=MONTHLY;BYWEEKNO=1", false)]
        [InlineData("FREQ=DAILY;BYWEEKNO=1", false)]
        [InlineData("FREQ=YEARLY;BYWEEKNO=1", true)]
        [InlineData("FREQ=MONTHLY;BYSETPOS=1", false)]
        [InlineData("FREQ=MONTHLY;COUNT=3;BYSETPOS=1", false)]
        [InlineData("FREQ=MONTHLY;BYSETPOS=1;BYDAY=MO", true)]
        [InlineData("FREQ=DAILY;BYSETPOS=1;BYSECOND=0", true)]
        public void TryParse_Constraint_IsEnforced(string value, bool isValid)
        {
            // act
            var testResult = IcalRecurrenceRule.TryParse(value.AsSpan(), IcalValueFormat.Text, out _);

            // assert
            testResult.Should().Be(isValid);
        }

        [Fact]
        public void IsValid_BuiltRuleBreakingConstraint_IsFalse()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Weekly).WithByDay(new IcalRecurrenceDay(DayOfWeek.Monday, 1));

            // act
            var testResult = rule.IsValid;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void ToStringWithFormat_BuiltRuleBreakingConstraint_ThrowsInvalidOperation()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Weekly).WithByDay(new IcalRecurrenceDay(DayOfWeek.Monday, 1));

            // act
            Action testAction = () => rule.ToString(IcalValueFormat.Text);

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage("*BYDAY*MONTHLY or YEARLY*");
        }

        [Fact]
        public void ToString_BuiltRuleBreakingConstraint_WritesTheRule()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Weekly).WithByDay(new IcalRecurrenceDay(DayOfWeek.Monday, 1));

            // act
            var testResult = rule.ToString();

            // assert
            testResult.Should().Be("FREQ=WEEKLY;BYDAY=1MO");
        }

        [Fact]
        public void IsValid_Default_IsFalse()
        {
            // arrange
            var rule = default(IcalRecurrenceRule);

            // act
            var testResult = rule.IsValid;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryFormat_Default_ThrowsInvalidOperation()
        {
            // arrange
            var rule = default(IcalRecurrenceRule);

            // act
            Action testAction = () => rule.TryFormat(new char[64], out _, IcalValueFormat.Text);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ToString_Default_IsEmpty()
        {
            // arrange
            var rule = default(IcalRecurrenceRule);

            // act
            var testResult = rule.ToString();

            // assert
            testResult.Should().BeEmpty();
        }

        #endregion

        #region Building

        [Fact]
        public void With_PartsOfTheRfcExample_WritesTheRfcExampleInCanonicalOrder()
        {
            // act
            var testResult = new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly)
                .WithInterval(2)
                .WithByMonth(1)
                .WithByDay(new IcalRecurrenceDay(DayOfWeek.Sunday))
                .WithByHour(8, 9)
                .WithByMinute(30)
                .ToString(IcalValueFormat.Text);

            // assert
            testResult.Should().Be(CanonicalRfcRecurrenceRule);
        }

        [Fact]
        public void WithCount_RuleWithUntil_ReplacesUntil()
        {
            // arrange
            var until = new IcalDateTime(new IcalDate(1997, 12, 24), new IcalTime(0, 0, 0, true));
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithUntil(until);

            // act
            var testResult = rule.WithCount(10);

            // assert
            testResult.Count.Should().Be(10);
            testResult.UntilDateTime.Should().BeNull();
        }

        [Fact]
        public void WithUntil_RuleWithCount_ReplacesCount()
        {
            // arrange
            var until = new IcalDateTime(new IcalDate(1997, 12, 24), new IcalTime(0, 0, 0, true));
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithCount(10);

            // act
            var testResult = rule.WithUntil(until);

            // assert
            testResult.Count.Should().BeNull();
            testResult.UntilDateTime.Should().Be(until);
        }

        [Fact]
        public void WithoutEnd_RuleWithUntil_RemovesUntil()
        {
            // arrange
            var until = new IcalDateTime(new IcalDate(1997, 12, 24), new IcalTime(0, 0, 0, true));
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithUntil(until);

            // act
            var testResult = rule.WithoutEnd();

            // assert
            testResult.Should().Be(new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily));
        }

        [Fact]
        public void WithByHour_NoValues_RemovesThePart()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithByHour(9);

            // act
            var testResult = rule.WithByHour();

            // assert
            testResult.Should().Be(new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily));
        }

        [Fact]
        public void WithByDay_Null_RemovesThePart()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithByDay(new IcalRecurrenceDay(DayOfWeek.Monday));

            // act
            var testResult = rule.WithByDay(null);

            // assert
            testResult.Should().Be(new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily));
        }

        [Fact]
        public void WithWeekStart_Null_RemovesWkst()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Weekly).WithWeekStart(DayOfWeek.Sunday);

            // act
            var testResult = rule.WithWeekStart(null);

            // assert
            testResult.WeekStart.Should().BeNull();
        }

        [Theory]
        [MemberData(nameof(OutOfRangeCalls), DisableDiscoveryEnumeration = true)]
        public void With_OutOfRangeValue_ThrowsArgumentOutOfRange(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void WithUntil_ZonedDateTime_ThrowsArgumentException()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly);
            var until = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(9, 0, 0), "Europe/London");

            // act
            Action testAction = () => rule.WithUntil(until);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        #endregion
    }
}
