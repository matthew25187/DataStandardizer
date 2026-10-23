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
        private const string NewYork = "America/New_York";

        /// <summary>
        /// The examples of RFC 5545 §3.8.5.3: the start, the rule, the end of the window for a rule that repeats forever, and the occurrences
        /// listed.  A window ends just after the last occurrence the RFC lists.
        /// </summary>
        public static TheoryData<string, string, string, string[]> RfcExamples => new TheoryData<string, string, string, string[]>()
        {
            // Daily for 10 occurrences.
            { "19970902T090000", "FREQ=DAILY;COUNT=10", null, Range("19970902", "19970911") },
            // Daily until December 24, 1997.
            { "19970902T090000", "FREQ=DAILY;UNTIL=19971224T000000Z", null, Range("19970902", "19971223") },
            // Every other day - forever.
            { "19970902T090000", "FREQ=DAILY;INTERVAL=2", "19971204T000000", Range("19970902", "19971203", 2) },
            // Every 10 days, 5 occurrences.
            { "19970902T090000", "FREQ=DAILY;INTERVAL=10;COUNT=5", null, Dates("19970902", "19970912", "19970922", "19971002", "19971012") },
            // Every day in January, for 3 years, in two forms.
            {
                "19980101T090000", "FREQ=YEARLY;UNTIL=20000131T140000Z;BYMONTH=1;BYDAY=SU,MO,TU,WE,TH,FR,SA", null,
                Join(Range("19980101", "19980131"), Range("19990101", "19990131"), Range("20000101", "20000131"))
            },
            {
                "19980101T090000", "FREQ=DAILY;UNTIL=20000131T140000Z;BYMONTH=1", null,
                Join(Range("19980101", "19980131"), Range("19990101", "19990131"), Range("20000101", "20000131"))
            },
            // Weekly for 10 occurrences.
            { "19970902T090000", "FREQ=WEEKLY;COUNT=10", null, Range("19970902", "19971104", 7) },
            // Weekly until December 24, 1997.
            { "19970902T090000", "FREQ=WEEKLY;UNTIL=19971224T000000Z", null, Range("19970902", "19971223", 7) },
            // Every other week - forever.
            { "19970902T090000", "FREQ=WEEKLY;INTERVAL=2;WKST=SU", "19980218T000000", Range("19970902", "19980217", 14) },
            // Weekly on Tuesday and Thursday for five weeks, in two forms.
            {
                "19970902T090000", "FREQ=WEEKLY;UNTIL=19971007T000000Z;WKST=SU;BYDAY=TU,TH", null,
                Dates("19970902", "19970904", "19970909", "19970911", "19970916", "19970918", "19970923", "19970925", "19970930", "19971002")
            },
            {
                "19970902T090000", "FREQ=WEEKLY;COUNT=10;WKST=SU;BYDAY=TU,TH", null,
                Dates("19970902", "19970904", "19970909", "19970911", "19970916", "19970918", "19970923", "19970925", "19970930", "19971002")
            },
            // Every other week on Monday, Wednesday, and Friday until December 24, 1997, starting on Monday, September 1, 1997.
            {
                "19970901T090000", "FREQ=WEEKLY;INTERVAL=2;UNTIL=19971224T000000Z;WKST=SU;BYDAY=MO,WE,FR", null,
                Dates("19970901", "19970903", "19970905", "19970915", "19970917", "19970919", "19970929", "19971001", "19971003", "19971013",
                    "19971015", "19971017", "19971027", "19971029", "19971031", "19971110", "19971112", "19971114", "19971124", "19971126",
                    "19971128", "19971208", "19971210", "19971212", "19971222")
            },
            // Every other week on Tuesday and Thursday, for 8 occurrences.
            {
                "19970902T090000", "FREQ=WEEKLY;INTERVAL=2;COUNT=8;WKST=SU;BYDAY=TU,TH", null,
                Dates("19970902", "19970904", "19970916", "19970918", "19970930", "19971002", "19971014", "19971016")
            },
            // Monthly on the first Friday for 10 occurrences.
            {
                "19970905T090000", "FREQ=MONTHLY;COUNT=10;BYDAY=1FR", null,
                Dates("19970905", "19971003", "19971107", "19971205", "19980102", "19980206", "19980306", "19980403", "19980501", "19980605")
            },
            // Monthly on the first Friday until December 24, 1997.
            { "19970905T090000", "FREQ=MONTHLY;UNTIL=19971224T000000Z;BYDAY=1FR", null, Dates("19970905", "19971003", "19971107", "19971205") },
            // Every other month on the first and last Sunday of the month for 10 occurrences.
            {
                "19970907T090000", "FREQ=MONTHLY;INTERVAL=2;COUNT=10;BYDAY=1SU,-1SU", null,
                Dates("19970907", "19970928", "19971102", "19971130", "19980104", "19980125", "19980301", "19980329", "19980503", "19980531")
            },
            // Monthly on the second-to-last Monday of the month for 6 months.
            {
                "19970922T090000", "FREQ=MONTHLY;COUNT=6;BYDAY=-2MO", null,
                Dates("19970922", "19971020", "19971117", "19971222", "19980119", "19980216")
            },
            // Monthly on the third-to-the-last day of the month, forever.
            {
                "19970928T090000", "FREQ=MONTHLY;BYMONTHDAY=-3", "19980227T000000",
                Dates("19970928", "19971029", "19971128", "19971229", "19980129", "19980226")
            },
            // Monthly on the 2nd and 15th of the month for 10 occurrences.
            {
                "19970902T090000", "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=2,15", null,
                Dates("19970902", "19970915", "19971002", "19971015", "19971102", "19971115", "19971202", "19971215", "19980102", "19980115")
            },
            // Monthly on the first and last day of the month for 10 occurrences.
            {
                "19970930T090000", "FREQ=MONTHLY;COUNT=10;BYMONTHDAY=1,-1", null,
                Dates("19970930", "19971001", "19971031", "19971101", "19971130", "19971201", "19971231", "19980101", "19980131", "19980201")
            },
            // Every 18 months on the 10th thru 15th of the month for 10 occurrences.
            {
                "19970910T090000", "FREQ=MONTHLY;INTERVAL=18;COUNT=10;BYMONTHDAY=10,11,12,13,14,15", null,
                Join(Range("19970910", "19970915"), Range("19990310", "19990313"))
            },
            // Every Tuesday, every other month.
            {
                "19970902T090000", "FREQ=MONTHLY;INTERVAL=2;BYDAY=TU", "19980401T000000",
                Join(Range("19970902", "19970930", 7), Range("19971104", "19971125", 7), Range("19980106", "19980127", 7),
                    Range("19980303", "19980331", 7))
            },
            // Yearly in June and July for 10 occurrences.
            {
                "19970610T090000", "FREQ=YEARLY;COUNT=10;BYMONTH=6,7", null,
                Dates("19970610", "19970710", "19980610", "19980710", "19990610", "19990710", "20000610", "20000710", "20010610", "20010710")
            },
            // Every other year on January, February, and March for 10 occurrences.
            {
                "19970310T090000", "FREQ=YEARLY;INTERVAL=2;COUNT=10;BYMONTH=1,2,3", null,
                Dates("19970310", "19990110", "19990210", "19990310", "20010110", "20010210", "20010310", "20030110", "20030210", "20030310")
            },
            // Every third year on the 1st, 100th, and 200th day for 10 occurrences.
            {
                "19970101T090000", "FREQ=YEARLY;INTERVAL=3;COUNT=10;BYYEARDAY=1,100,200", null,
                Dates("19970101", "19970410", "19970719", "20000101", "20000409", "20000718", "20030101", "20030410", "20030719", "20060101")
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
                "19970902T090000", "FREQ=MONTHLY;BYDAY=FR;BYMONTHDAY=13", "20001014T000000",
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
            // The third instance into the month of one of Tuesday, Wednesday, or Thursday, for the next 3 months.
            { "19970904T090000", "FREQ=MONTHLY;COUNT=3;BYDAY=TU,WE,TH;BYSETPOS=3", null, Dates("19970904", "19971007", "19971106") },
            // The second-to-last weekday of the month.
            {
                "19970929T090000", "FREQ=MONTHLY;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-2", "19980331T000000",
                Dates("19970929", "19971030", "19971127", "19971230", "19980129", "19980226", "19980330")
            },
            // Every 15 minutes for 6 occurrences.
            { "19970902T090000", "FREQ=MINUTELY;INTERVAL=15;COUNT=6", null, Times("19970902", "090000", "101500", 15) },
            // Every hour and a half for 4 occurrences.
            { "19970902T090000", "FREQ=MINUTELY;INTERVAL=90;COUNT=4", null, Times("19970902", "090000", "133000", 90) },
            // Every 20 minutes from 9:00 AM to 4:40 PM every day, in two forms.
            {
                "19970902T090000", "FREQ=DAILY;BYHOUR=9,10,11,12,13,14,15,16;BYMINUTE=0,20,40", "19970904T000000",
                Join(Times("19970902", "090000", "164000", 20), Times("19970903", "090000", "164000", 20))
            },
            {
                "19970902T090000", "FREQ=MINUTELY;INTERVAL=20;BYHOUR=9,10,11,12,13,14,15,16", "19970904T000000",
                Join(Times("19970902", "090000", "164000", 20), Times("19970903", "090000", "164000", 20))
            },
            // An example where the days generated makes a difference because of WKST, and the same with only WKST changed.
            { "19970805T090000", "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO", null, Dates("19970805", "19970810", "19970819", "19970824") },
            { "19970805T090000", "FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=SU", null, Dates("19970805", "19970817", "19970819", "19970831") },
            // An example where an invalid date (i.e., February 30) is ignored.
            { "20070115T090000", "FREQ=MONTHLY;BYMONTHDAY=15,30;COUNT=5", null, Dates("20070115", "20070130", "20070215", "20070315", "20070330") }
        };

        #region RFC examples

        [Theory]
        [MemberData(nameof(RfcExamples))]
        public void GetOccurrences_RfcExample_GivesTheListedOccurrences(string start, string rule, string to, string[] expected)
        {
            // arrange
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = IcalDateTime.Parse(start.AsSpan(), NewYork, IcalValueFormat.Text);
            var toValue = to is null ? (IcalDateTime?)null : IcalDateTime.Parse(to.AsSpan(), IcalValueFormat.Text);

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
            var start = IcalDateTime.Parse("19970902T090000".AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(start, null, null, NewYorkToUtc, new IcalOptions());

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("19970902T090000", "19970902T120000");
        }

        [Theory]
        [InlineData("19870405T020000", "FREQ=YEARLY;BYMONTH=4;BYDAY=1SU;UNTIL=19980404T070000Z", -5, 4, 1, 1987, 1997)]
        [InlineData("19671029T020000", "FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU;UNTIL=20061029T060000Z", -4, 10, -1, 1967, 2006)]
        public void GetOccurrences_RfcTimeZoneObservance_EndsAtUntilInUtc(string start, string rule, int offsetFromHours, int month,
            int sundayOrdinal, int firstYear, int lastYear)
        {
            // arrange
            // ref. RFC 5545§3.6.5: an observance's DTSTART is in local time and its UNTIL in UTC, so local time is converted with TZOFFSETFROM.
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = IcalDateTime.Parse(start.AsSpan(), IcalValueFormat.Text);
            var expected = Enumerable.Range(firstYear, lastYear - firstYear + 1)
                .Select(year => FindSunday(year, month, sundayOrdinal).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "T020000")
                .ToArray();

            // act
            var testResult = recurrenceRule.GetOccurrences(startValue, null, null,
                local => IcalDateTime.FromDateTime(DateTime.SpecifyKind(local.ToDateTime().AddHours(-offsetFromHours), DateTimeKind.Utc)),
                new IcalOptions());

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(expected);
        }

        #endregion

        #region Rule parts

        [Fact]
        public void GetOccurrences_WeekStart_ChangesTheResult()
        {
            // arrange
            var start = IcalDateTime.Parse("19970805T090000".AsSpan(), IcalValueFormat.Text);
            var monday = IcalRecurrenceRule.Parse("FREQ=WEEKLY;INTERVAL=2;COUNT=4;BYDAY=TU,SU;WKST=MO".AsSpan(), IcalValueFormat.Text);
            var sunday = monday.WithWeekStart(DayOfWeek.Sunday);

            // act
            var testResult = sunday.GetOccurrences(start, null, null);

            // assert
            testResult.Should().NotEqual(monday.GetOccurrences(start, null, null));
        }

        [Theory]
        [InlineData("20260101T100000", "FREQ=MONTHLY;COUNT=4;BYDAY=-1FR", "20260130T100000,20260227T100000,20260327T100000,20260424T100000")]
        [InlineData("20240131T100000", "FREQ=MONTHLY;COUNT=4;BYMONTHDAY=-1", "20240131T100000,20240229T100000,20240331T100000,20240430T100000")]
        [InlineData("20231231T100000", "FREQ=YEARLY;COUNT=3;BYYEARDAY=-1", "20231231T100000,20241231T100000,20251231T100000")]
        [InlineData("20240101T100000", "FREQ=YEARLY;COUNT=3;BYYEARDAY=-366", "20240101T100000,20280101T100000,20320101T100000")]
        [InlineData("20260101T100000", "FREQ=YEARLY;COUNT=3;BYDAY=-1MO", "20261228T100000,20271227T100000,20281225T100000")]
        [InlineData("20260101T100000", "FREQ=YEARLY;COUNT=2;BYWEEKNO=-1;BYDAY=TH", "20261231T100000,20271230T100000")]
        [InlineData("20260101T100000", "FREQ=YEARLY;COUNT=2;BYMONTH=2;BYDAY=-2SU", "20260215T100000,20270221T100000")]
        public void GetOccurrences_NegativeValues_CountFromTheEnd(string start, string rule, string expected)
        {
            // act
            var testResult = Expand(start, rule, null);

            // assert
            testResult.Should().Equal(expected.Split(','));
        }

        [Fact]
        public void GetOccurrences_WeekNumberOne_IncludesDaysOfTheYearBefore()
        {
            // arrange
            // Week 1 of 1997 starts on Monday 30 December 1996, and week 1 of 1998 on Monday 29 December 1997.  1998 has no Monday in week 1.
            const string rule = "FREQ=YEARLY;COUNT=3;BYWEEKNO=1;BYDAY=MO";

            // act
            var testResult = Expand("19961230T090000", rule, null);

            // assert
            testResult.Should().Equal("19961230T090000", "19971229T090000", "19990104T090000");
        }

        [Fact]
        public void GetOccurrences_ByDayWithAndWithoutOrdinals_GivesEitherKind()
        {
            // act
            var testResult = Expand("20260101T100000", "FREQ=MONTHLY;COUNT=4;BYDAY=1MO,FR", null);

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
            var testResult = Expand("20100105T090000", rule, null);

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
            var testResult = Expand("20300627T040000", rule, null);

            // assert
            testResult.Should().Equal("20300701T040000");
        }

        [Theory]
        [InlineData("FREQ=MONTHLY;COUNT=3;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=1,-1","20260101T090000,20260130T090000,20260202T090000")]
        [InlineData("FREQ=MONTHLY;COUNT=3;BYDAY=MO,TU,WE,TH,FR;BYSETPOS=-1,1,1,-23", "20260101T090000,20260130T090000,20260202T090000")]
        [InlineData("FREQ=DAILY;COUNT=4;BYHOUR=9,12,15;BYSETPOS=2,3", "20260101T120000,20260101T150000,20260102T120000,20260102T150000")]
        [InlineData("FREQ=WEEKLY;COUNT=2;BYDAY=MO,WE;BYHOUR=9,17;BYSETPOS=-1", "20260107T170000,20260114T170000")]
        [InlineData("FREQ=MONTHLY;COUNT=1;BYMONTHDAY=1;BYSETPOS=2,-2", "")]
        public void GetOccurrences_BySetPos_PicksFromEachPeriod(string rule, string expected)
        {
            // act
            var testResult = Expand("20260101T090000", rule, "20270101T000000");

            // assert
            testResult.Should().Equal(expected.Length == 0 ? Array.Empty<string>() : expected.Split(','));
        }

        [Fact]
        public void GetOccurrences_TwentyNinthOfFebruary_OccursInLeapYearsOnly()
        {
            // arrange
            var expected = Enumerable.Range(2000, 105)
                .Where(year => DateTime.IsLeapYear(year))
                .Select(year => year.ToString(CultureInfo.InvariantCulture) + "0229T090000")
                .ToArray();

            // act
            var testResult = Expand("20000229T090000", "FREQ=YEARLY;BYMONTHDAY=29;BYMONTH=2", "21050101T000000");

            // assert
            testResult.Should().Equal(expected);
            testResult.Should().NotContain("21000229T090000");
        }

        [Theory]
        [InlineData("20000229T090000", "FREQ=YEARLY;COUNT=3", "20000229T090000,20040229T090000,20080229T090000")]
        [InlineData("20260131T090000", "FREQ=MONTHLY;COUNT=4", "20260131T090000,20260331T090000,20260531T090000,20260731T090000")]
        [InlineData("20260130T090000", "FREQ=MONTHLY;COUNT=3;BYMONTHDAY=30", "20260130T090000,20260330T090000,20260430T090000")]
        [InlineData("20260101T090000", "FREQ=YEARLY;COUNT=2;BYYEARDAY=366", "20281231T090000,20321231T090000")]
        public void GetOccurrences_InvalidDate_IsSkippedNotClamped(string start, string rule, string expected)
        {
            // act
            var testResult = Expand(start, rule, null);

            // assert
            testResult.Should().Equal(expected.Split(','));
        }

        [Theory]
        [InlineData("20260105T093015", "FREQ=WEEKLY;COUNT=2", "20260105T093015,20260112T093015")]
        [InlineData("20260105T093015", "FREQ=YEARLY;COUNT=2;BYMONTH=3", "20260305T093015,20270305T093015")]
        [InlineData("20260105T093015", "FREQ=DAILY;COUNT=2;BYMINUTE=0", "20260106T090015,20260107T090015")]
        [InlineData("20260105T093015", "FREQ=HOURLY;COUNT=2;BYSECOND=0", "20260105T103000,20260105T113000")]
        public void GetOccurrences_PartsNotGiven_AreTakenFromTheStart(string start, string rule, string expected)
        {
            // act
            var testResult = Expand(start, rule, null);

            // assert
            testResult.Should().Equal(expected.Split(','));
        }

        [Theory]
        [InlineData("FREQ=SECONDLY;COUNT=4;BYMINUTE=0;BYSECOND=0,30", "20260101T100000,20260101T100030,20260101T110000,20260101T110030")]
        [InlineData("FREQ=MINUTELY;COUNT=3;BYHOUR=0;BYMINUTE=59", "20260102T005900,20260103T005900,20260104T005900")]
        [InlineData("FREQ=HOURLY;INTERVAL=5;COUNT=3;BYDAY=SU", "20260104T023000,20260104T073000,20260104T123000")]
        [InlineData("FREQ=HOURLY;COUNT=2;BYYEARDAY=-1", "20261231T003000,20261231T013000")]
        public void GetOccurrences_BelowDaily_LimitsAndSkipsAhead(string rule, string expected)
        {
            // act
            var testResult = Expand("20260101T093000", rule, null);

            // assert
            testResult.Should().Equal(expected.Split(','));
        }

        [Fact]
        public void GetOccurrences_LeapSecond_IsNeverGenerated()
        {
            // act
            var testResult = Expand("20261231T235800", "FREQ=MINUTELY;COUNT=2;BYSECOND=59,60", null);

            // assert
            testResult.Should().Equal("20261231T235859", "20261231T235959");
        }

        #endregion

        #region Ends and windows

        [Fact]
        public void GetOccurrences_FromWithCount_CountsFromTheStart()
        {
            // act
            var testResult = Expand("20260101T090000", "FREQ=DAILY;COUNT=5", null, "20260103T090000");

            // assert
            testResult.Should().Equal("20260103T090000", "20260104T090000", "20260105T090000");
        }

        [Fact]
        public void GetOccurrences_Window_IncludesFromAndExcludesTo()
        {
            // act
            var testResult = Expand("20260101T090000", "FREQ=DAILY", "20260104T090000", "20260102T090000");

            // assert
            testResult.Should().Equal("20260102T090000", "20260103T090000");
        }

        [Theory]
        [InlineData("FREQ=WEEKLY;INTERVAL=3;BYDAY=MO,FR")]
        [InlineData("FREQ=MONTHLY;INTERVAL=3;BYDAY=-1FR,2TU")]
        [InlineData("FREQ=YEARLY;INTERVAL=2;BYWEEKNO=1,26,-1;BYDAY=WE;WKST=SU")]
        [InlineData("FREQ=DAILY;INTERVAL=11")]
        [InlineData("FREQ=HOURLY;INTERVAL=7;BYMINUTE=0,45")]
        [InlineData("FREQ=MINUTELY;INTERVAL=97;BYHOUR=8,9,10")]
        [InlineData("FREQ=SECONDLY;INTERVAL=3601;BYDAY=MO")]
        public void GetOccurrences_From_GivesTheSameOccurrencesAsFilteringFromTheStart(string rule)
        {
            // arrange
            const string start = "20200106T081500";
            const string from = "20251229T000000";
            const string to = "20260301T000000";
            var all = Expand(start, rule, to);

            // act
            var testResult = Expand(start, rule, to, from);

            // assert
            testResult.Should().NotBeEmpty();
            testResult.Should().Equal(all.Where(occurrence => string.CompareOrdinal(occurrence, from) >= 0));
        }

        [Theory]
        [InlineData("20260101T090000Z", "FREQ=DAILY;UNTIL=20260103T090000Z", IcalDateTimeKind.Utc)]
        [InlineData("20260101T090000", "FREQ=DAILY;UNTIL=20260103T090000", IcalDateTimeKind.Floating)]
        [InlineData("20260101T090000", "FREQ=DAILY;UNTIL=20260103", IcalDateTimeKind.Floating)]
        public void GetOccurrences_Until_IsInclusive(string start, string rule, IcalDateTimeKind kind)
        {
            // arrange
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = IcalDateTime.Parse(start.AsSpan(), IcalValueFormat.Text);

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
            var rule = IcalRecurrenceRule.Parse("FREQ=DAILY;UNTIL=20260103T140000Z".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20260101T090000".AsSpan(), NewYork, IcalValueFormat.Text);
            var converted = new List<IcalDateTime>();

            // act
            var testResult = rule.GetOccurrences(start, null, null, local =>
            {
                converted.Add(local);
                return NewYorkToUtc(local);
            }, new IcalOptions()).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260101T090000", "20260102T090000", "20260103T090000");
            converted.Should().OnlyContain(local => local.TimeZoneId == NewYork);
        }

        [Fact]
        public void GetOccurrences_OpenEndedRule_IsLazy()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Secondly);
            var start = IcalDateTime.Parse("20260101T000000".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(start, null, IcalDateTime.Parse("99991231T235959".AsSpan(), IcalValueFormat.Text)).Take(3);

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260101T000000", "20260101T000001", "20260101T000002");
        }

        [Fact]
        public void GetOccurrences_EndOfCalendar_Ends()
        {
            // act
            var testResult = Expand("99981231T090000", "FREQ=WEEKLY;BYDAY=FR;WKST=SU", "99991231T235959");

            // assert
            testResult.Should().HaveCount(53);
            testResult[testResult.Count - 1].Should().Be("99991231T090000");
        }

        #endregion

        #region Iteration limit

        [Fact]
        public void GetOccurrences_RuleThatNeverMatches_ThrowsAtTheIterationLimit()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=SECONDLY;BYMONTH=2;BYMONTHDAY=30".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20260101T000000".AsSpan(), IcalValueFormat.Text);
            var occurrences = rule.GetOccurrences(start, null, IcalDateTime.Parse("99991231T000000".AsSpan(), IcalValueFormat.Text));

            // act
            Action testResult = () => occurrences.ToList();

            // assert
            testResult.Should().Throw<InvalidOperationException>().WithMessage("*100000 consecutive periods*");
        }

        [Fact]
        public void GetOccurrences_YearlyRuleThatNeverMatches_EndsAtTheEndOfTheCalendar()
        {
            // arrange
            // Fewer than 8,000 years are left, so the default limit is not reached.
            var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;COUNT=1;BYMONTH=2;BYMONTHDAY=30".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20260101T000000".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(start, null, null);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetOccurrences_LowIterationLimit_Throws()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;COUNT=1;BYMONTH=2;BYMONTHDAY=30".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20260101T000000".AsSpan(), IcalValueFormat.Text);
            var occurrences = rule.GetOccurrences(start, null, null, new IcalOptions { MaxRecurrenceIterations = 100 });

            // act
            Action testResult = () => occurrences.ToList();

            // assert
            testResult.Should().Throw<InvalidOperationException>().WithMessage("*100 consecutive periods*");
        }

        [Fact]
        public void GetOccurrences_SparseRule_StaysWithinTheDefaultLimit()
        {
            // arrange
            // 29 February 2096 is followed by 29 February 2104, the longest gap a matching rule can have.
            var rule = IcalRecurrenceRule.Parse("FREQ=MINUTELY;COUNT=2;BYMONTH=2;BYMONTHDAY=29;BYHOUR=12;BYMINUTE=0".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20960101T000000".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(start, null, null);

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
            var testResult = rule.GetOccurrences(new IcalDate(2026, 1, 6), null, null);

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260106", "20260113", "20260120");
        }

        [Fact]
        public void GetOccurrences_DateStartWithWindow_IncludesFromAndExcludesTo()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=YEARLY;BYMONTH=12;BYMONTHDAY=25".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = rule.GetOccurrences(new IcalDate(2000, 12, 25), new IcalDate(2024, 12, 25), new IcalDate(2026, 12, 25));

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
            Action testResult = () => recurrenceRule.GetOccurrences(new IcalDate(2026, 1, 1), null, null);

            // assert
            testResult.Should().Throw<ArgumentException>().WithParameterName("start");
        }

        #endregion

        #region Arguments

        [Fact]
        public void GetOccurrences_OpenEndedRuleWithoutTo_Throws()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily);
            var start = IcalDateTime.Parse("20260101T090000".AsSpan(), IcalValueFormat.Text);

            // act
            Action testResult = () => rule.GetOccurrences(start, start, null);

            // assert
            testResult.Should().Throw<ArgumentException>().WithParameterName("to");
        }

        [Fact]
        public void GetOccurrences_ZonedStartWithUntilInUtcAndNoConversion_Throws()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=DAILY;UNTIL=20260103T140000Z".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20260101T090000".AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            Action testResult = () => rule.GetOccurrences(start, null, null);

            // assert
            testResult.Should().Throw<ArgumentException>().WithParameterName("start");
        }

        [Fact]
        public void GetOccurrences_InvalidRule_Throws()
        {
            // arrange
            var rule = new IcalRecurrenceRule(IcalRecurrenceFrequency.Daily).WithBySetPos(1);
            var start = IcalDateTime.Parse("20260101T090000".AsSpan(), IcalValueFormat.Text);

            // act
            Action testResult = () => rule.GetOccurrences(start, null, start);
            Action defaultResult = () => default(IcalRecurrenceRule).GetOccurrences(start, null, start);

            // assert
            testResult.Should().Throw<InvalidOperationException>();
            defaultResult.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetOccurrences_NullArguments_Throw()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=DAILY;COUNT=2".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20260101T090000".AsSpan(), IcalValueFormat.Text);

            // act
            Action nullOptions = () => rule.GetOccurrences(start, null, null, null);
            Action nullDateOptions = () => rule.GetOccurrences(new IcalDate(2026, 1, 1), null, null, null);
            Action nullConversion = () => rule.GetOccurrences(start, null, null, null, new IcalOptions());

            // assert
            nullOptions.Should().Throw<ArgumentNullException>().WithParameterName("options");
            nullDateOptions.Should().Throw<ArgumentNullException>().WithParameterName("options");
            nullConversion.Should().Throw<ArgumentNullException>().WithParameterName("toUtc");
        }

        [Fact]
        public void GetOccurrences_IterationLimitBelowOne_Throws()
        {
            // arrange
            var rule = IcalRecurrenceRule.Parse("FREQ=DAILY;COUNT=2".AsSpan(), IcalValueFormat.Text);
            var start = IcalDateTime.Parse("20260101T090000".AsSpan(), IcalValueFormat.Text);

            // act
            Action testResult = () => rule.GetOccurrences(start, null, null, new IcalOptions { MaxRecurrenceIterations = 0 });

            // assert
            testResult.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        #endregion

        #region Allocation

        [Fact]
        public void GetOccurrences_EachOccurrence_DoesNotAllocate()
        {
            // arrange
            var start = IcalDateTime.Parse("20260101T090000".AsSpan(), NewYork, IcalValueFormat.Text);
            var few = IcalRecurrenceRule.Parse("FREQ=MONTHLY;COUNT=10;BYDAY=MO,TU,WE,TH,FR;BYHOUR=9,17;BYSETPOS=1,-1".AsSpan(), IcalValueFormat.Text);
            var many = few.WithCount(1000);
            Enumerate(few, start);

            // act
            var fewBytes = MeasureAllocations(() => Enumerate(few, start));
            var testResult = MeasureAllocations(() => Enumerate(many, start));

            // assert
            testResult.Should().Be(fewBytes);
        }

        private static void Enumerate(IcalRecurrenceRule rule, IcalDateTime start)
        {
            foreach (var _ in rule.GetOccurrences(start, null, null))
            {
            }
        }

        private static long MeasureAllocations(Action action)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            action();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        #endregion

        private static List<string> Expand(string start, string rule, string to, string from = null)
        {
            var recurrenceRule = IcalRecurrenceRule.Parse(rule.AsSpan(), IcalValueFormat.Text);
            var startValue = IcalDateTime.Parse(start.AsSpan(), IcalValueFormat.Text);
            var fromValue = from is null ? (IcalDateTime?)null : IcalDateTime.Parse(from.AsSpan(), IcalValueFormat.Text);
            var toValue = to is null ? (IcalDateTime?)null : IcalDateTime.Parse(to.AsSpan(), IcalValueFormat.Text);
            return recurrenceRule.GetOccurrences(startValue, fromValue, toValue).Select(occurrence => occurrence.ToString()).ToList();
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
