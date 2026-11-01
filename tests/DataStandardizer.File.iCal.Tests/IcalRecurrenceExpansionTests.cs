using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalRecurrenceExpansionTests
    {
        private const string NewYork = Rfc5545Examples.NewYorkTimeZoneId;

        // ref. RFC 5545 §3.8.5.3: the DTSTART of most of the examples, in New York time.
        private const string September2Start = "19970902T090000";

        // ref. RFC 5545 §3.8.5.3: the start of the example where the days generated make a difference because of WKST.
        private const string WeekStartExampleStart = "19970805T090000";

        private const string NewYear = "20260101T090000";

        // The start of the rules of OccurrencesFromLaterPeriods: 08:15 on Monday, January 6, 2020.
        private const string FromStart = "20200106T081500";
        private const string NewYearTen = "20260101T100000";
        private const string NewYearMidnight = "20260101T000000";
        private const string StartWithSeconds = "20260105T093015";
        private const string DailyTwice = "FREQ=DAILY;COUNT=2";
        private const string DailyUntilJanuary3InUtc = "FREQ=DAILY;UNTIL=20260103T140000Z";
        private const string NeverOnFebruary30 = "FREQ=YEARLY;COUNT=1;BYMONTH=2;BYMONTHDAY=30";

        // ref. RFC 5545 §3.8.5.3: the occurrences of the examples that are given in two forms.
        private static readonly string[] EveryDayInJanuaryForThreeYears = Join(Range("19980101", "19980131"), Range("19990101", "19990131"),
            Range("20000101", "20000131"));

        private static readonly string[] TuesdaysAndThursdaysForFiveWeeks = Dates("19970902", "19970904", "19970909", "19970911", "19970916",
            "19970918", "19970923", "19970925", "19970930", "19971002");

        private static readonly string[] EveryTwentyMinutesForTwoDays = Join(Times("19970902", "090000", "164000", 20),
            Times("19970903", "090000", "164000", 20));

        /// <summary>
        /// The examples of RFC 5545 §3.8.5.3 that end, by COUNT or UNTIL: the start, the rule, and the occurrences listed.
        /// </summary>
        public static TheoryData<string, string, string[]> RfcExamplesWithEnd => new TheoryData<string, string, string[]>()
        {
            // Daily for 10 occurrences.
            { September2Start, "FREQ=DAILY;COUNT=10", Range("19970902", "19970911") },
            // Daily until December 24, 1997.
            { September2Start, "FREQ=DAILY;UNTIL=19971224T000000Z", Range("19970902", "19971223") },
            // Every 10 days, 5 occurrences.
            { September2Start, "FREQ=DAILY;INTERVAL=10;COUNT=5", Dates("19970902", "19970912", "19970922", "19971002", "19971012") },
            // Every day in January, for 3 years, in two forms.
            {
                "19980101T090000", "FREQ=YEARLY;UNTIL=20000131T140000Z;BYMONTH=1;BYDAY=SU,MO,TU,WE,TH,FR,SA",
                EveryDayInJanuaryForThreeYears
            },
            {
                "19980101T090000", "FREQ=DAILY;UNTIL=20000131T140000Z;BYMONTH=1",
                EveryDayInJanuaryForThreeYears
            },
            // Weekly for 10 occurrences.
            { September2Start, "FREQ=WEEKLY;COUNT=10", Range("19970902", "19971104", 7) },
            // Weekly until December 24, 1997.
            { September2Start, "FREQ=WEEKLY;UNTIL=19971224T000000Z", Range("19970902", "19971223", 7) },
            // Weekly on Tuesday and Thursday for five weeks, in two forms.
            {
                September2Start, "FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH",
                TuesdaysAndThursdaysForFiveWeeks
            },
            {
                September2Start, "FREQ=WEEKLY;COUNT=10;WKST=SU;BYDAY=TU,TH",
                TuesdaysAndThursdaysForFiveWeeks
            },
            // Every other week on Monday, Wednesday, and Friday until December 24, 1997, starting on Monday, September 1, 1997.
            {
                "19970901T090000", "FREQ=WEEKLY;INTERVAL=2;UNTIL=19971224T000000Z;WKST=SU;BYDAY=MO,WE,FR",
                Dates("19970901", "19970903", "19970905", "19970915", "19970917", "19970919", "19970929", "19971001", "19971003", "19971013",
                    "19971015", "19971017", "19971027", "19971029", "19971031", "19971110", "19971112", "19971114", "19971124", "19971126",
                    "19971128", "19971208", "19971210", "19971212", "19971222")
            },
            // Every other week on Tuesday and Thursday, for 8 occurrences.
            {
                September2Start, "FREQ=WEEKLY;INTERVAL=2;COUNT=8;WKST=SU;BYDAY=TU,TH",
                Dates("19970902", "19970904", "19970916", "19970918", "19970930", "19971002", "19971014", "19971016")
            },
            // Monthly on the first Friday for 10 occurrences.
            {
                "19970905T090000", "FREQ=MONTHLY;COUNT=10;BYDAY=1FR",
                Dates("19970905", "19971003", "19971107", "19971205", "19980102", "19980206", "19980306", "19980403", "19980501", "19980605")
            },
            // Monthly on the first Friday until December 24, 1997.
            { "19970905T090000", "FREQ=MONTHLY;UNTIL=19971224T000000Z;BYDAY=1FR", Dates("19970905", "19971003", "19971107", "19971205") },
            // Every other month on the first and last Sunday of the month for 10 occurrences.
            {
                "19970907T090000", "FREQ=MONTHLY;INTERVAL=2;COUNT=10;BYDAY=1SU,-1SU",
                Dates("19970907", "19970928", "19971102", "19971130", "19980104", "19980125", "19980301", "19980329", "19980503", "19980531")
            },
            // Monthly on the second-to-last Monday of the month for 6 months.
            {
                "19970922T090000", "FREQ=MONTHLY;COUNT=6;BYDAY=-2MO",
                Dates("19970922", "19971020", "19971117", "19971222", "19980119", "19980216")
            },
            // Monthly on the 2nd and 15th of the month for 10 occurrences.
            {
                September2Start, "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=2,15",
                Dates("19970902", "19970915", "19971002", "19971015", "19971102", "19971115", "19971202", "19971215", "19980102", "19980115")
            },
            // Monthly on the first and last day of the month for 10 occurrences.
            {
                "19970930T090000", "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=1,-1",
                Dates("19970930", "19971001", "19971031", "19971101", "19971130", "19971201", "19971231", "19980101", "19980131", "19980201")
            },
            // Every 18 months on the 10th thru 15th of the month for 10 occurrences.
            {
                "19970910T090000", "FREQ=MONTHLY;INTERVAL=18;COUNT=10;BYMONTHDAY=10,11,12,13,14,15",
                Join(Range("19970910", "19970915"), Range("19990310", "19990313"))
            },
            // Yearly in June and July for 10 occurrences.
            {
                "19970610T090000", "FREQ=YEARLY;COUNT=10;BYMONTH=6,7",
                Dates("19970610", "19970710", "19980610", "19980710", "19990610", "19990710", "20000610", "20000710", "20010610", "20010710")
            },
            // Every other year on January, February, and March for 10 occurrences.
            {
                "19970310T090000", "FREQ=YEARLY;INTERVAL=2;COUNT=10;BYMONTH=1,2,3",
                Dates("19970310", "19990110", "19990210", "19990310", "20010110", "20010210", "20010310", "20030110", "20030210", "20030310")
            },
            // Every third year on the 1st, 100th, and 200th day for 10 occurrences.
            {
                "19970101T090000", "FREQ=YEARLY;INTERVAL=3;COUNT=10;BYYEARDAY=1,100,200",
                Dates("19970101", "19970410", "19970719", "20000101", "20000409", "20000718", "20030101", "20030410", "20030719", "20060101")
            },
            // The third instance into the month of one of Tuesday, Wednesday, or Thursday, for the next 3 months.
            { "19970904T090000", "FREQ=MONTHLY;COUNT=3;BYDAY=TU,WE,TH;BYSETPOS=3", Dates("19970904", "19971007", "19971106") },
            // Every 15 minutes for 6 occurrences.
            { September2Start, "FREQ=MINUTELY;INTERVAL=15;COUNT=6", Times("19970902", "090000", "101500", 15) },
            // Every hour and a half for 4 occurrences.
            { September2Start, "FREQ=MINUTELY;INTERVAL=90;COUNT=4", Times("19970902", "090000", "133000", 90) },
            // An example where the days generated makes a difference because of WKST, and the same with only WKST changed.
            { WeekStartExampleStart, "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO", Dates("19970805", "19970810", "19970819", "19970824") },
            { WeekStartExampleStart, "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=SU", Dates("19970805", "19970817", "19970819", "19970831") },
            // An example where an invalid date (i.e., February 30) is ignored.
            { "20070115T090000", "FREQ=MONTHLY;BYMONTHDAY=15,30;COUNT=5", Dates("20070115", "20070130", "20070215", "20070315", "20070330") },
        };

        /// <summary>
        /// The examples of RFC 5545 §3.8.5.3 that repeat forever: the start, the rule, the end of the window, and the occurrences listed.  A
        /// window ends just after the last occurrence the RFC lists.
        /// </summary>
        public static TheoryData<string, string, string, string[]> RfcExamplesForever => new TheoryData<string, string, string, string[]>()
        {
            // Every other day - forever.
            { September2Start, "FREQ=DAILY;INTERVAL=2", "19971204T000000", Range("19970902", "19971203", 2) },
            // Every other week - forever.
            { September2Start, "FREQ=WEEKLY;INTERVAL=2;WKST=SU", "19980218T000000", Range("19970902", "19980217", 14) },
            // Monthly on the third-to-the-last day of the month, forever.
            {
                "19970928T090000", "FREQ=MONTHLY;BYMONTHDAY=-3", "19980227T000000",
                Dates("19970928", "19971029", "19971128", "19971229", "19980129", "19980226")
            },
            // Every Tuesday, every other month.
            {
                September2Start, "FREQ=MONTHLY;INTERVAL=2;BYDAY=TU", "19980401T000000",
                Join(Range("19970902", "19970930", 7), Range("19971104", "19971125", 7), Range("19980106", "19980127", 7),
                    Range("19980303", "19980331", 7))
            },
            // Every 20th Monday of the year, forever.
            { "19970519T090000", "FREQ=YEARLY;BYDAY=20MO", "20000101T000000", Dates("19970519", "19980518", "19990517") },
            // Monday of week number 20 (where the default start of the week is Monday), forever.
            { "19970512T090000", "FREQ=YEARLY;BYWEEKNO=20;BYDAY=MO", "20000101T000000", Dates("19970512", "19980511", "19990517") },
            // Every Thursday in March, forever.
            {
                "19970313T090000", "FREQ=YEARLY;BYMONTH=3;BYDAY=TH", "20000101T000000",
                Join(Range("19970313", "19970327", 7), Range("19980305", "19980326", 7), Range("19990304", "19990325", 7))
            },
            // Every Thursday, but only during June, July, and August, forever.
            {
                "19970605T090000", "FREQ=YEARLY;BYDAY=TH;BYMONTH=6,7,8", "20000101T000000",
                Join(Range("19970605", "19970828", 7), Range("19980604", "19980827", 7), Range("19990603", "19990826", 7))
            },
            // Every Friday the 13th, forever.  DTSTART does not match the rule, so it is not generated, and the RFC excludes it with EXDATE.
            {
                September2Start, "FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13", "20001014T000000",
                Dates("19980213", "19980313", "19981113", "19990813", "20001013")
            },
            // The first Saturday that follows the first Sunday of the month, forever.
            {
                "19970913T090000", "FREQ=MONTHLY;BYDAY=SA;BYMONTHDAY=7,8,9,10,11,12,13", "19980614T000000",
                Dates("19970913", "19971011", "19971108", "19971213", "19980110", "19980207", "19980307", "19980411", "19980509", "19980613")
            },
            // Every 4 years, the first Tuesday after a Monday in November, forever (U.S. Presidential Election day).
            {
                "19961105T090000", "FREQ=YEARLY;INTERVAL=4;BYMONTH=11;BYDAY=TU;BYMONTHDAY=2,3,4,5,6,7,8", "20041103T000000",
                Dates("19961105", "20001107", "20041102")
            },
            // The second-to-last weekday of the month.
            {
                "19970929T090000", "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-2", "19980331T000000",
                Dates("19970929", "19971030", "19971127", "19971230", "19980129", "19980226", "19980330")
            },
            // Every 20 minutes from 9:00 AM to 4:40 PM every day, in two forms.
            {
                September2Start, "FREQ=DAILY;BYHOUR=9,10,11,12,13,14,15,16;BYMINUTE=0,20,40", "19970904T000000",
                EveryTwentyMinutesForTwoDays
            },
            {
                September2Start, "FREQ=MINUTELY;INTERVAL=20;BYHOUR=9,10,11,12,13,14,15,16", "19970904T000000",
                EveryTwentyMinutesForTwoDays
            },
        };

        // ref. RFC 5545 §3.6.5: the start of daylight saving time in New York from 1987 to 1997, and of standard time from 1967 to 2006, at
        // 02:00 local time, as the observances of the RFC's VTIMEZONE examples give them.
        public static TheoryData<string, string, int, string[]> RfcTimeZoneObservances => new TheoryData<string, string, int, string[]>
        {
            {
                "19870405T020000", "FREQ=YEARLY;BYMONTH=4;BYDAY=1SU;UNTIL=19980404T070000Z", -5,
                new[]
                {
                    "19870405T020000", "19880403T020000", "19890402T020000", "19900401T020000", "19910407T020000", "19920405T020000",
                    "19930404T020000", "19940403T020000", "19950402T020000", "19960407T020000", "19970406T020000"
                }
            },
            {
                "19671029T020000", "FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU;UNTIL=20061029T060000Z", -4,
                new[]
                {
                    "19671029T020000", "19681027T020000", "19691026T020000", "19701025T020000", "19711031T020000", "19721029T020000",
                    "19731028T020000", "19741027T020000", "19751026T020000", "19761031T020000", "19771030T020000", "19781029T020000",
                    "19791028T020000", "19801026T020000", "19811025T020000", "19821031T020000", "19831030T020000", "19841028T020000",
                    "19851027T020000", "19861026T020000", "19871025T020000", "19881030T020000", "19891029T020000", "19901028T020000",
                    "19911027T020000", "19921025T020000", "19931031T020000", "19941030T020000", "19951029T020000", "19961027T020000",
                    "19971026T020000", "19981025T020000", "19991031T020000", "20001029T020000", "20011028T020000", "20021027T020000",
                    "20031026T020000", "20041031T020000", "20051030T020000", "20061029T020000"
                }
            },
        };

        public static TheoryData<Action, string> NullArgumentCalls()
        {
            var rule = IcalRecurrenceRule.Parse(DailyTwice.AsSpan(), IcalValueFormat.Text);
            var start = At(NewYear);
            return new TheoryData<Action, string>
            {
                { () => rule.GetOccurrences(start, null, null, null), "options" },
                { () => rule.GetOccurrences(new IcalDate(2026, 1, 1), null, null, null), "options" },
                { () => rule.GetOccurrences(start, null, null, null, new IcalOptions()), "toUtc" },
            };
        }

        #region RFC examples

        [Theory]
        [MemberData(nameof(RfcExamplesWithEnd))]
        public void GetOccurrences_RfcExampleWithEnd_GivesTheListedOccurrences(string start, string rule, string[] expected)
        {
            // arrange
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = IcalDateTime.Parse(start.AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            var testResult = recurrenceRule.GetOccurrences(startValue, null, null, NewYorkToUtc, new IcalOptions()).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(expected);
            testResult.Should().OnlyContain(occurrence => occurrence.Kind == IcalDateTimeKind.Zoned && occurrence.TimeZoneId == NewYork);
        }

        [Theory]
        [MemberData(nameof(RfcExamplesForever))]
        public void GetOccurrences_RfcExampleForever_GivesTheListedOccurrencesInTheWindow(string start, string rule, string to, string[] expected)
        {
            // arrange
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = IcalDateTime.Parse(start.AsSpan(), NewYork, IcalValueFormat.Text);
            var toValue = At(to);

            // act
            var testResult = recurrenceRule.GetOccurrences(startValue, null, toValue, NewYorkToUtc, new IcalOptions()).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(expected);
            testResult.Should().OnlyContain(occurrence => occurrence.Kind == IcalDateTimeKind.Zoned && occurrence.TimeZoneId == NewYork);
        }

        [Fact]
        public void GetOccurrences_RfcHourlyUntilExample_StopsAtUntil()
        {
            // arrange
            // The RFC lists 09:00, 12:00 and 15:00 for "every 3 hours from 9:00 AM to 5:00 PM on a specific day", but its UNTIL of 17:00 UTC is
            // 13:00 EDT, so a strict reading of UNTIL stops after 12:00 (16:00 UTC); 15:00 EDT is 19:00 UTC.
            var rule = IcalRecurrenceRule.Parse("FREQ=HOURLY;INTERVAL=3;UNTIL=19970902T170000Z".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse(September2Start.AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(start, null, null, NewYorkToUtc, new IcalOptions()).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(September2Start, "19970902T120000");
        }

        [Theory]
        [MemberData(nameof(RfcTimeZoneObservances))]
        public void GetOccurrences_RfcTimeZoneObservance_EndsAtUntilInUtc(string start, string rule, int offsetFromHours, string[] expected)
        {
            // arrange
            // ref. RFC 5545 §3.6.5: an observance's DTSTART is in local time and its UNTIL in UTC, so local time is converted with TZOFFSETFROM.
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = IcalDateTime.Parse(start.AsSpan(), IcalValueFormat.Text);
            var toUtc = new FixedOffsetConversion(offsetFromHours);

            // act
            var testResult = recurrenceRule.GetOccurrences(startValue, null, null, toUtc.ToUtc, new IcalOptions()).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(expected);
        }

        #endregion

        #region Rule parts

        [Fact]
        public void GetOccurrences_WeekStartChangedToSunday_GivesTheRfcSundayResult()
        {
            // arrange: ref. RFC 5545 §3.8.5.3, the example where the days generated make a difference because of WKST.
            var start = At(WeekStartExampleStart);
            var rule = IcalRecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO".AsSpan(), IcalValueFormat.Text)
                .WithWeekStart(DayOfWeek.Sunday);

            // act
            var testResult = rule.GetOccurrences(start, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(WeekStartExampleStart, "19970817T090000", "19970819T090000", "19970831T090000");
        }

        [Theory]
        [InlineData(NewYearTen, "FREQ=MONTHLY;COUNT=4;BYDAY=-1FR", new[] { "20260130T100000", "20260227T100000", "20260327T100000", "20260424T100000" })]
        [InlineData("20240131T100000", "FREQ=MONTHLY;COUNT=4;BYMONTHDAY=-1", new[] { "20240131T100000", "20240229T100000", "20240331T100000", "20240430T100000" })]
        [InlineData("20231231T100000", "FREQ=YEARLY;COUNT=3;BYYEARDAY=-1", new[] { "20231231T100000", "20241231T100000", "20251231T100000" })]
        [InlineData("20240101T100000", "FREQ=YEARLY;COUNT=3;BYYEARDAY=-366", new[] { "20240101T100000", "20280101T100000", "20320101T100000" })]
        [InlineData(NewYearTen, "FREQ=YEARLY;COUNT=3;BYDAY=-1MO", new[] { "20261228T100000", "20271227T100000", "20281225T100000" })]
        [InlineData(NewYearTen, "FREQ=YEARLY;COUNT=2;BYWEEKNO=-1;BYDAY=TH", new[] { "20261231T100000", "20271230T100000" })]
        [InlineData(NewYearTen, "FREQ=YEARLY;COUNT=2;BYMONTH=2;BYDAY=-2SU", new[] { "20260215T100000", "20270221T100000" })]
        public void GetOccurrences_NegativeValues_CountFromTheEnd(string start, string rule, string[] expected)
        {
            // act
            var testResult = Expand(start, rule);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void GetOccurrences_WeekNumberOne_IncludesDaysOfTheYearBefore()
        {
            // arrange
            // Week 1 of 1997 starts on Monday 30 December 1996, and week 1 of 1998 on Monday 29 December 1997.  1998 has no Monday in week 1.
            const string rule = "FREQ=YEARLY;COUNT=3;BYWEEKNO=1;BYDAY=MO";

            // act
            var testResult = Expand("19961230T090000", rule);

            // assert
            testResult.Should().Equal("19961230T090000", "19971229T090000", "19990104T090000");
        }

        [Fact]
        public void GetOccurrences_ByDayWithAndWithoutOrdinals_GivesEitherKind()
        {
            // act
            var testResult = Expand(NewYearTen, "FREQ=MONTHLY;COUNT=4;BYDAY=1MO,FR");

            // assert
            testResult.Should().Equal("20260102T100000", "20260105T100000", "20260109T100000", "20260116T100000");
        }

        [Fact]
        public void GetOccurrences_WeekNumber53_OccursOnlyInYearsWithThatWeek()
        {
            // arrange
            // 2010 to 2014 have 52 weeks, so 1 January 2011 is in week 52 of 2010.  Week 53 of 2015 runs from 28 December 2015 to 3 January 2016.
            const string rule = "FREQ=YEARLY;COUNT=1;BYWEEKNO=53;BYDAY=SA";

            // act
            var testResult = Expand("20100105T090000", rule);

            // assert
            testResult.Should().Equal("20160102T090000");
        }

        [Fact]
        public void GetOccurrences_BySetPosInTheFirstWeek_PicksFromTheWholeWeek()
        {
            // arrange
            // The first position of the week of Thursday 27 June 2030 is Monday 24 June, before the start, so the first occurrence is the
            // following Monday rather than the start.
            const string rule = "FREQ=WEEKLY;COUNT=1;BYDAY=MO,TH;BYSETPOS=1";

            // act
            var testResult = Expand("20300627T040000", rule);

            // assert
            testResult.Should().Equal("20300701T040000");
        }

        [Theory]
        [InlineData("FREQ=MONTHLY;COUNT=3;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1,-1", new[] { NewYear, "20260130T090000", "20260202T090000" })]
        [InlineData("FREQ=MONTHLY;COUNT=3;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1,1,1,-23", new[] { NewYear, "20260130T090000", "20260202T090000" })]
        [InlineData("FREQ=DAILY;COUNT=4;BYHOUR=9,12,15;BYSETPOS=2,3", new[] { "20260101T120000", "20260101T150000", "20260102T120000", "20260102T150000" })]
        [InlineData("FREQ=WEEKLY;COUNT=2;BYDAY=MO,WE;BYHOUR=9,17;BYSETPOS=-1", new[] { "20260107T170000", "20260114T170000" })]
        [InlineData("FREQ=MONTHLY;COUNT=1;BYMONTHDAY=1;BYSETPOS=2,-2", new string[0])]
        public void GetOccurrences_BySetPos_PicksFromEachPeriod(string rule, string[] expected)
        {
            // act
            var testResult = Expand(NewYear, rule, "20270101T000000");

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void GetOccurrences_TwentyNinthOfFebruary_OccursInLeapYearsOnly()
        {
            // act
            var testResult = Expand("20000229T090000", "FREQ=YEARLY;BYMONTHDAY=29;BYMONTH=2", "21050101T000000");

            // assert: 2100 is not a leap year.
            testResult.Should().Equal("20000229T090000", "20040229T090000", "20080229T090000", "20120229T090000", "20160229T090000",
                "20200229T090000", "20240229T090000", "20280229T090000", "20320229T090000", "20360229T090000", "20400229T090000", "20440229T090000",
                "20480229T090000", "20520229T090000", "20560229T090000", "20600229T090000", "20640229T090000", "20680229T090000", "20720229T090000",
                "20760229T090000", "20800229T090000", "20840229T090000", "20880229T090000", "20920229T090000", "20960229T090000", "21040229T090000");
        }

        [Theory]
        [InlineData("20000229T090000", "FREQ=YEARLY;COUNT=3", new[] { "20000229T090000", "20040229T090000", "20080229T090000" })]
        [InlineData("20260131T090000", "FREQ=MONTHLY;COUNT=4", new[] { "20260131T090000", "20260331T090000", "20260531T090000", "20260731T090000" })]
        [InlineData("20260130T090000", "FREQ=MONTHLY;COUNT=3;BYMONTHDAY=30", new[] { "20260130T090000", "20260330T090000", "20260430T090000" })]
        [InlineData(NewYear, "FREQ=YEARLY;COUNT=2;BYYEARDAY=366", new[] { "20281231T090000", "20321231T090000" })]
        public void GetOccurrences_InvalidDate_IsSkippedNotClamped(string start, string rule, string[] expected)
        {
            // act
            var testResult = Expand(start, rule);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [InlineData("FREQ=WEEKLY;COUNT=2", new[] { StartWithSeconds, "20260112T093015" })]
        [InlineData("FREQ=YEARLY;COUNT=2;BYMONTH=3", new[] { "20260305T093015", "20270305T093015" })]
        [InlineData("FREQ=DAILY;COUNT=2;BYMINUTE=0", new[] { "20260106T090015", "20260107T090015" })]
        [InlineData("FREQ=HOURLY;COUNT=2;BYSECOND=0", new[] { "20260105T103000", "20260105T113000" })]
        public void GetOccurrences_PartsNotGiven_AreTakenFromTheStart(string rule, string[] expected)
        {
            // act
            var testResult = Expand(StartWithSeconds, rule);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [InlineData("FREQ=SECONDLY;COUNT=4;BYMINUTE=0;BYSECOND=0,30", new[] { "20260101T100000", "20260101T100030", "20260101T110000", "20260101T110030" })]
        [InlineData("FREQ=MINUTELY;COUNT=3;BYHOUR=0;BYMINUTE=59", new[] { "20260102T005900", "20260103T005900", "20260104T005900" })]
        [InlineData("FREQ=HOURLY;INTERVAL=5;COUNT=3;BYDAY=SU", new[] { "20260104T023000", "20260104T073000", "20260104T123000" })]
        [InlineData("FREQ=HOURLY;COUNT=2;BYYEARDAY=-1", new[] { "20261231T003000", "20261231T013000" })]
        public void GetOccurrences_BelowDaily_LimitsAndSkipsAhead(string rule, string[] expected)
        {
            // act
            var testResult = Expand("20260101T093000", rule);

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public void GetOccurrences_LeapSecond_IsNeverGenerated()
        {
            // act
            var testResult = Expand("20261231T235800", "FREQ=MINUTELY;COUNT=2;BYSECOND=59,60");

            // assert
            testResult.Should().Equal("20261231T235859", "20261231T235959");
        }

        #endregion

        #region Ends and windows

        [Fact]
        public void GetOccurrences_FromWithCount_CountsFromTheStart()
        {
            // act
            var testResult = ExpandFrom(NewYear, "FREQ=DAILY;COUNT=5", "20260103T090000");

            // assert
            testResult.Should().Equal("20260103T090000", "20260104T090000", "20260105T090000");
        }

        [Fact]
        public void GetOccurrences_Window_IncludesFromAndExcludesTo()
        {
            // act
            var testResult = Expand(NewYear, "FREQ=DAILY", "20260104T090000", "20260102T090000");

            // assert
            testResult.Should().Equal("20260102T090000", "20260103T090000");
        }

        // Rules of each frequency, with a period after many years of their occurrences from FromStart, and the occurrences in that period,
        // worked out with python-dateutil's rrule, an implementation of RFC 5545 §3.3.10 independent of the library.
        public static TheoryData<string, string, string, string[]> OccurrencesFromLaterPeriods => new TheoryData<string, string, string, string[]>
        {
            { "FREQ=WEEKLY;INTERVAL=3;BYDAY=MO,FR", "20251229T000000", "20260301T000000", new[] { "20251229T081500", "20260102T081500", "20260119T081500", "20260123T081500", "20260209T081500", "20260213T081500" } },
            { "FREQ=MONTHLY;INTERVAL=3;BYDAY=-1FR,2TU", "20251229T000000", "20260301T000000", new[] { "20260113T081500", "20260130T081500" } },
            { "FREQ=YEARLY;INTERVAL=2;BYWEEKNO=1,26,-1;BYDAY=WE;WKST=SU", "20251229T000000", "20260301T000000", new[] { "20260107T081500" } },
            { "FREQ=DAILY;INTERVAL=11", "20251229T000000", "20260301T000000", new[] { "20260103T081500", "20260114T081500", "20260125T081500", "20260205T081500", "20260216T081500", "20260227T081500" } },
            { "FREQ=HOURLY;INTERVAL=7;BYMINUTE=0,45", "20251229T000000", "20251230T000000", new[] { "20251229T010000", "20251229T014500", "20251229T080000", "20251229T084500", "20251229T150000", "20251229T154500", "20251229T220000", "20251229T224500" } },
            { "FREQ=MINUTELY;INTERVAL=97;BYHOUR=8,9,10", "20251229T000000", "20251231T000000", new[] { "20251229T092600", "20251230T080400", "20251230T094100" } },
            { "FREQ=SECONDLY;INTERVAL=3601;BYDAY=MO", "20251229T000000", "20251229T060000", new[] { "20251229T004814", "20251229T014815", "20251229T024816", "20251229T034817", "20251229T044818", "20251229T054819" } },
        };

        [Theory]
        [MemberData(nameof(OccurrencesFromLaterPeriods), DisableDiscoveryEnumeration = true)]
        public void GetOccurrences_FromLaterPeriod_GivesTheOccurrencesInThePeriod(string rule, string from, string to, string[] expected)
        {
            // act
            var testResult = Expand(FromStart, rule, to, from);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [InlineData("20260101T090000Z", "FREQ=DAILY;UNTIL=20260103T090000Z", IcalDateTimeKind.Utc)]
        [InlineData(NewYear, "FREQ=DAILY;UNTIL=20260103T090000", IcalDateTimeKind.Floating)]
        [InlineData(NewYear, "FREQ=DAILY;UNTIL=20260103", IcalDateTimeKind.Floating)]
        public void GetOccurrences_Until_IsInclusive(string start, string rule, IcalDateTimeKind kind)
        {
            // arrange
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = At(start);

            // act
            var testResult = recurrenceRule.GetOccurrences(startValue, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Date.ToString()).Should().Equal("20260101", "20260102", "20260103");
            testResult.Should().OnlyContain(occurrence => occurrence.Kind == kind);
        }

        [Fact]
        public void GetOccurrences_ZonedStartWithUntilInUtc_ConvertsEachOccurrence()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(DailyUntilJanuary3InUtc.AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse(NewYear.AsSpan(), NewYork, IcalValueFormat.Text);
            var conversion = new RecordingNewYorkConversion();

            // act
            var testResult = rule.GetOccurrences(start, null, null, conversion.ToUtc, new IcalOptions()).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(NewYear, "20260102T090000", "20260103T090000");
            conversion.Converted.Should().OnlyContain(local => local.TimeZoneId == NewYork);
        }

        [Fact]
        public void GetOccurrences_OpenEndedRule_GivesTheFirstOccurrencesWithoutReadingThemAll()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Secondly);
            var start = At(NewYearMidnight);
            var to = At("99991231T235959");

            // act
            var testResult = rule.GetOccurrences(start, null, to).Take(3).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(NewYearMidnight, "20260101T000001", "20260101T000002");
        }

        [Fact]
        public void GetOccurrences_EndOfCalendar_Ends()
        {
            // act
            var testResult = Expand("99981231T090000", "FREQ=WEEKLY;BYDAY=FR;WKST=SU", "99991231T235959");

            // assert: the Fridays of 9998 and 9999, the last of which is 31 December 9999.
            testResult.Should().HaveCount(53).And.EndWith("99991231T090000");
        }

        #endregion

        #region Iteration limit

        [Fact]
        public void GetOccurrences_RuleThatNeverMatches_ThrowsAtTheIterationLimit()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=SECONDLY;BYMONTH=2;BYMONTHDAY=30".AsSpan(), IcalValueFormat.Text);
            var start = At(NewYearMidnight);
            var occurrences = rule.GetOccurrences(start, null, At("99991231T000000"));

            // act
            Action testAction = () => occurrences.ToList();

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage("*100000 consecutive periods*");
        }

        [Fact]
        public void GetOccurrences_YearlyRuleThatNeverMatches_EndsAtTheEndOfTheCalendar()
        {
            // arrange
            // Fewer than 8,000 years are left, so the default limit is not reached.
            var rule = IcalRecurrenceRule.Parse(NeverOnFebruary30.AsSpan(), IcalValueFormat.Text);
            var start = At(NewYearMidnight);

            // act
            var testResult = rule.GetOccurrences(start, null, null).ToList();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetOccurrences_LowIterationLimit_Throws()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(NeverOnFebruary30.AsSpan(), IcalValueFormat.Text);
            var start = At(NewYearMidnight);
            var occurrences = rule.GetOccurrences(start, null, null, new IcalOptions { MaxRecurrenceIterations = 100 });

            // act
            Action testAction = () => occurrences.ToList();

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage("*100 consecutive periods*");
        }

        [Fact]
        public void GetOccurrences_SparseRule_StaysWithinTheDefaultLimit()
        {
            // arrange
            // 29 February 2096 is followed by 29 February 2104, the longest gap a matching rule can have.
            var rule = IcalRecurrenceRule.Parse("FREQ=MINUTELY;COUNT=2;BYMONTH=2;BYMONTHDAY=29;BYHOUR=12;BYMINUTE=0".AsSpan(), IcalValueFormat.Text);
            var start = At("20960101T000000");

            // act
            var testResult = rule.GetOccurrences(start, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20960229T120000", "21040229T120000");
        }

        #endregion

        #region Dates

        [Fact]
        public void GetOccurrences_DateStart_GivesDates()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=WEEKLY;UNTIL=20260120;BYDAY=TU".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(new IcalDate(2026, 1, 6), null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260106", "20260113", "20260120");
        }

        [Fact]
        public void GetOccurrences_DateStartWithWindow_IncludesFromAndExcludesTo()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=12;BYMONTHDAY=25".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(new IcalDate(2000, 12, 25), new IcalDate(2024, 12, 25), new IcalDate(2026, 12, 25)).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20241225", "20251225");
        }

        [Theory]
        [InlineData("FREQ=HOURLY;COUNT=2")]
        [InlineData("FREQ=DAILY;COUNT=2;BYHOUR=9")]
        [InlineData("FREQ=DAILY;COUNT=2;BYMINUTE=0")]
        [InlineData("FREQ=DAILY;COUNT=2;BYSECOND=0")]
        public void GetOccurrences_DateStartWithTimeOfDay_Throws(string rule)
        {
            // arrange
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);

            // act
            Action testAction = () => recurrenceRule.GetOccurrences(new IcalDate(2026, 1, 1), null, null);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("start");
        }

        #endregion

        #region Arguments

        [Fact]
        public void GetOccurrences_OpenEndedRuleWithoutTo_Throws()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily);
            var start = At(NewYear);

            // act
            Action testAction = () => rule.GetOccurrences(start, start, null);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("to");
        }

        [Fact]
        public void GetOccurrences_ZonedStartWithUntilInUtcAndNoConversion_Throws()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(DailyUntilJanuary3InUtc.AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse(NewYear.AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            Action testAction = () => rule.GetOccurrences(start, null, null);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("start");
        }

        [Fact]
        public void GetOccurrences_RuleBreakingAConstraint_ThrowsInvalidOperation()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithBySetPos(1);
            var start = At(NewYear);

            // act
            Action testAction = () => rule.GetOccurrences(start, null, start);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetOccurrences_DefaultRule_ThrowsInvalidOperation()
        {
            // arrange
            var rule = default(IcalRecurrenceRule);
            var start = At(NewYear);

            // act
            Action testAction = () => rule.GetOccurrences(start, null, start);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [MemberData(nameof(NullArgumentCalls), DisableDiscoveryEnumeration = true)]
        public void GetOccurrences_NullArgument_ThrowsArgumentNull(Action call, string parameterName)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName(parameterName);
        }

        [Fact]
        public void GetOccurrences_IterationLimitBelowOne_Throws()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse(DailyTwice.AsSpan(), IcalValueFormat.Text);
            var start = At(NewYear);

            // act
            Action testAction = () => rule.GetOccurrences(start, null, null, new IcalOptions { MaxRecurrenceIterations = 0 });

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        #endregion

        private static IcalDateTime At(string text)
        {
            return IcalDateTime.Parse(text.AsSpan(), IcalValueFormat.Text);
        }

        private static List<string> Expand(string start, string rule)
        {
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            return recurrenceRule.GetOccurrences(At(start), null, null).Select(occurrence => occurrence.ToString()).ToList();
        }

        private static List<string> Expand(string start, string rule, string to)
        {
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            return recurrenceRule.GetOccurrences(At(start), null, At(to)).Select(occurrence => occurrence.ToString()).ToList();
        }

        private static List<string> Expand(string start, string rule, string to, string from)
        {
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            return recurrenceRule.GetOccurrences(At(start), At(from), At(to)).Select(occurrence => occurrence.ToString()).ToList();
        }

        private static List<string> ExpandFrom(string start, string rule, string from)
        {
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            return recurrenceRule.GetOccurrences(At(start), At(from), null).Select(occurrence => occurrence.ToString()).ToList();
        }

        /// <summary>
        /// Converts a local time to UTC with a fixed offset, as a VTIMEZONE observance does with its TZOFFSETFROM.
        /// </summary>
        private sealed class FixedOffsetConversion
        {
            private readonly int _offsetHours;

            public FixedOffsetConversion(int offsetHours)
            {
                _offsetHours = offsetHours;
            }

            public IcalDateTime ToUtc(IcalDateTime local)
            {
                return IcalDateTime.FromDateTime(System.DateTime.SpecifyKind(local.ToDateTime().AddHours(-_offsetHours), DateTimeKind.Utc));
            }
        }

        /// <summary>
        /// Converts New York times to UTC, and records each time it converts.
        /// </summary>
        private sealed class RecordingNewYorkConversion
        {
            public List<IcalDateTime> Converted { get; } = new List<IcalDateTime>();

            public IcalDateTime ToUtc(IcalDateTime local)
            {
                Converted.Add(local);
                return NewYorkToUtc(local);
            }
        }

        /// <summary>
        /// Converts a local time in New York to UTC, by the United States daylight saving rules of 1987 onwards.
        /// </summary>
        private static IcalDateTime NewYorkToUtc(IcalDateTime local)
        {
            var dateTime = local.ToDateTime();
            DateTime daylightStart, daylightEnd;
            if (dateTime.Year >= 2007)
            {
                daylightStart = FindSunday(dateTime.Year, 3, 2).AddHours(2);
                daylightEnd = FindSunday(dateTime.Year, 11, 1).AddHours(2);
            }
            else
            {
                daylightStart = FindSunday(dateTime.Year, 4, 1).AddHours(2);
                daylightEnd = FindSunday(dateTime.Year, 10, -1).AddHours(2);
            }

            var offsetHours = dateTime >= daylightStart && dateTime < daylightEnd ? 4 : 5;
            return IcalDateTime.FromDateTime(DateTime.SpecifyKind(dateTime.AddHours(offsetHours), DateTimeKind.Utc));
        }

        /// <summary>
        /// Finds the nth Sunday of a month, or with a negative ordinal the nth from the end.
        /// </summary>
        private static DateTime FindSunday(int year, int month, int ordinal)
        {
            var sundays = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
                .Select(day => new DateTime(year, month, day))
                .Where(date => date.DayOfWeek == DayOfWeek.Sunday)
                .ToList();
            return ordinal > 0 ? sundays[ordinal - 1] : sundays[sundays.Count + ordinal];
        }

        private static string[] Dates(params string[] dates)
        {
            return dates.Select(date => date + "T090000").ToArray();
        }

        private static string[] Range(string first, string last, int stepDays = 1)
        {
            var end = DateTime.ParseExact(last, "yyyyMMdd", CultureInfo.InvariantCulture);
            var dates = new List<string>();
            for (var date = DateTime.ParseExact(first, "yyyyMMdd", CultureInfo.InvariantCulture); date <= end; date = date.AddDays(stepDays))
            {
                dates.Add(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "T090000");
            }

            return dates.ToArray();
        }

        private static string[] Times(string date, string first, string last, int stepMinutes)
        {
            var day = DateTime.ParseExact(date, "yyyyMMdd", CultureInfo.InvariantCulture);
            var end = day + TimeSpan.ParseExact(last, "hhmmss", CultureInfo.InvariantCulture);
            var times = new List<string>();
            for (var time = day + TimeSpan.ParseExact(first, "hhmmss", CultureInfo.InvariantCulture); time <= end; time = time.AddMinutes(stepMinutes))
            {
                times.Add(time.ToString("yyyyMMdd'T'HHmmss", CultureInfo.InvariantCulture));
            }

            return times.ToArray();
        }

        private static string[] Join(params string[][] parts)
        {
            return parts.SelectMany(part => part).ToArray();
        }
    }
}
