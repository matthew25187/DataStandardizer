using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalOccurrenceTests
    {
        private const string NewYork = Rfc5545Examples.NewYorkTimeZoneId;
        private const string London = "Europe/London";
        private const string Dublin = "Europe/Dublin";

        // The starts that the tests give their events.
        private const string March2Start = "DTSTART:20260302T090000Z";
        private const string March3NoonStart = "DTSTART:20260303T120000Z";
        private const string March3TenStart = "DTSTART:20260303T100000Z";
        private const string March7Start = "DTSTART:20260307T090000Z";
        private const string NewYorkMarch2Start = "DTSTART;TZID=America/New_York:20260302T090000";
        private const string NewYorkMarch7Start = "DTSTART;TZID=America/New_York:20260307T090000";
        private const string NewYorkMarch7NoonStart = "DTSTART;TZID=America/New_York:20260307T120000";
        private const string AllDayMarch1Start = "DTSTART;VALUE=DATE:20260301";
        private const string AllDayMarch2Start = "DTSTART;VALUE=DATE:20260302";
        private const string AllDayMarch7Start = "DTSTART;VALUE=DATE:20260307";

        // The values of those starts, and of other instances that the tests expect.
        private const string March2NineUtc = "20260302T090000Z";
        private const string March3NineUtc = "20260303T090000Z";
        private const string March3NoonUtc = "20260303T120000Z";
        private const string March4NineUtc = "20260304T090000Z";
        private const string March7NineUtc = "20260307T090000Z";
        private const string March2NineLocal = "20260302T090000";
        private const string March3NineLocal = "20260303T090000";
        private const string March8NoonLocal = "20260308T120000";
        private const string March2NineNewYorkInUtc = "20260302T140000Z";
        private const string March2NineToTenLocal = "20260302T090000/20260302T100000";
        private const string March2 = "20260302";
        private const string March7 = "20260307";

        private const string March3RecurrenceId = "RECURRENCE-ID:20260303T090000Z";
        private const string March3ThisAndFutureRecurrenceId = "RECURRENCE-ID;RANGE=THISANDFUTURE:20260303T090000Z";
        private const string March3ExDate = "EXDATE:20260303T090000Z";

        private const string Daily = "RRULE:FREQ=DAILY";
        private const string DailyTwice = "RRULE:FREQ=DAILY;COUNT=2";
        private const string DailyThreeTimes = "RRULE:FREQ=DAILY;COUNT=3";
        private const string DailyFourTimes = "RRULE:FREQ=DAILY;COUNT=4";
        private const string WeeklyTwice = "RRULE:FREQ=WEEKLY;COUNT=2";
        private const string WeeklyThreeTimes = "RRULE:FREQ=WEEKLY;COUNT=3";
        private const string MonthlyTwice = "RRULE:FREQ=MONTHLY;COUNT=2";
        private const string TwoMondays = "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=2";
        private const string OneHour = "DURATION:PT1H";
        private const string HalfAnHour = "DURATION:PT30M";
        private const string TodoUid = "UID:todo@example.com";
        private const string BeforeSummary = "Before";
        private const string AfterSummary = "After";

        private static readonly IcalOptions UndefinedTimeZones = new IcalOptions() { AllowUndefinedTimeZones = true };

        private static readonly IIcalTimeZoneResolver CalendarThenTzData =
            IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

        /// <summary>
        /// The America/New_York VTIMEZONE of RFC 5545 §3.6.5, cut down to the rules in use from 2007.
        /// </summary>
        private static readonly string[] NewYorkTimeZone =
        {
            IcalLines.BeginTimeZone, "TZID:America/New_York",
            IcalLines.BeginDaylight, "DTSTART:20070311T020000", "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU",
            "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT", IcalLines.EndDaylight,
            IcalLines.BeginStandard, "DTSTART:20071104T020000", "RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU",
            "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500", "TZNAME:EST", IcalLines.EndStandard,
            IcalLines.EndTimeZone
        };

        #region Time zones

        /// <summary>
        /// A start at 09:00 local time on the day before a change to or from daylight saving time, with the UTC starts and ends of the three
        /// daily one-hour instances from it.
        /// </summary>
        public static TheoryData<string, string, string[], string[]> DaylightSavingChanges => new TheoryData<string, string, string[], string[]>()
        {
            {
                NewYork, NewYorkMarch7Start,
                new[] { "20260307T140000Z", "20260308T130000Z", "20260309T130000Z" },
                new[] { "20260307T150000Z", "20260308T140000Z", "20260309T140000Z" }
            },
            {
                NewYork, "DTSTART;TZID=America/New_York:20261031T090000",
                new[] { "20261031T130000Z", "20261101T140000Z", "20261102T140000Z" },
                new[] { "20261031T140000Z", "20261101T150000Z", "20261102T150000Z" }
            },
            {
                London, "DTSTART;TZID=Europe/London:20260328T090000",
                new[] { "20260328T090000Z", "20260329T080000Z", "20260330T080000Z" },
                new[] { "20260328T100000Z", "20260329T090000Z", "20260330T090000Z" }
            },
            {
                London, "DTSTART;TZID=Europe/London:20261024T090000",
                new[] { "20261024T080000Z", "20261025T090000Z", "20261026T090000Z" },
                new[] { "20261024T090000Z", "20261025T100000Z", "20261026T100000Z" }
            },
            // Ireland observes a negative daylight saving time: its standard time is in summer, and winter is the "daylight" offset of +00:00.
            {
                Dublin, "DTSTART;TZID=Europe/Dublin:20260328T090000",
                new[] { "20260328T090000Z", "20260329T080000Z", "20260330T080000Z" },
                new[] { "20260328T100000Z", "20260329T090000Z", "20260330T090000Z" }
            },
            {
                Dublin, "DTSTART;TZID=Europe/Dublin:20261024T090000",
                new[] { "20261024T080000Z", "20261025T090000Z", "20261026T090000Z" },
                new[] { "20261024T090000Z", "20261025T100000Z", "20261026T100000Z" }
            },
        };

        [Theory]
        [MemberData(nameof(DaylightSavingChanges))]
        public void GetOccurrences_DailyAcrossADaylightSavingChange_KeepsTheLocalTimeAndMovesTheUtcTime(string zone, string start,
            string[] expectedStartsUtc, string[] expectedEndsUtc)
        {
            // arrange
            var calendar = Read(Event(start, OneHour, DailyThreeTimes));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Should().OnlyContain(occurrence => occurrence.Start.DateTime.Time == new IcalTime(9, 0, 0)
                && occurrence.Start.DateTime.TimeZoneId == zone);
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal(expectedStartsUtc);
            testResult.Select(occurrence => occurrence.EndUtc.ToString()).Should().Equal(expectedEndsUtc);
        }

        [Fact]
        public void GetOccurrences_DefaultResolver_ReadsTheCalendarsVTimeZone()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(NewYorkTimeZone, Event(NewYorkMarch7Start, DailyThreeTimes)));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal("20260307T140000Z", "20260308T130000Z", "20260309T130000Z");
        }

        [Fact]
        public void GetOccurrences_DefaultResolver_WithoutAVTimeZone_Throws()
        {
            // arrange
            var calendar = Read(Event(NewYorkMarch7Start));
            var testEvent = calendar.Events.Single();

            // act
            Action testAction = () => testEvent.GetOccurrences(calendar, null, null);

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*'America/New_York'*");
        }

        [Fact]
        public void GetOccurrences_TimeInADaylightSavingGap_IsReadWithTheOffsetBeforeTheGap()
        {
            // arrange: ref. RFC 5545 §3.3.5: 02:30 does not exist on 2026-03-08 in New York, so it is read as 02:30 EST, which is 03:30 EDT.
            var calendar = Read(Event("DTSTART;TZID=America/New_York:20260307T023000", DailyThreeTimes));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20260307T023000", "20260308T023000", "20260309T023000");
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal("20260307T073000Z", "20260308T073000Z", "20260309T063000Z");
        }

        [Fact]
        public void GetOccurrences_TimeInADaylightSavingOverlap_TakesItsFirstOccurrence()
        {
            // arrange: ref. RFC 5545 §3.3.5: 01:30 happens twice on 2026-11-01 in New York; the first, in EDT, is 05:30 UTC.
            var calendar = Read(Event("DTSTART;TZID=America/New_York:20261031T013000", DailyThreeTimes));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal("20261031T053000Z", "20261101T053000Z", "20261102T063000Z");
        }

        [Fact]
        public void GetOccurrences_FloatingTime_StaysFloating()
        {
            // arrange
            var calendar = Read(Event("DTSTART:20260307T090000", "DTEND:20260307T100000", DailyTwice));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Should().OnlyContain(occurrence => occurrence.Start.DateTime.Kind == IcalDateTimeKind.Floating);
            testResult.Select(occurrence => occurrence.End.Value.ToString()).Should().Equal("20260307T100000", "20260308T100000");
            testResult.Should().OnlyContain(occurrence => occurrence.StartUtc == null && occurrence.EndUtc == null);
        }

        [Fact]
        public void GetOccurrences_UtcTime_StaysInUtc()
        {
            // arrange
            var calendar = Read(Event(March7Start, HalfAnHour, WeeklyTwice));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal(March7NineUtc, "20260314T090000Z");
            testResult.Select(occurrence => occurrence.End.Value.ToString()).Should().Equal("20260307T093000Z", "20260314T093000Z");
        }

        [Fact]
        public void GetOccurrences_UntilInUtcWithAZonedStart_ComparesTheInstant()
        {
            // arrange: 09:00 EDT on 2026-03-10 is 13:00 UTC, after the UNTIL, so the last instance is on the 9th.
            var calendar = Read(Event(NewYorkMarch7Start, "RRULE:FREQ=DAILY;UNTIL=20260310T125959Z"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20260307T090000", "20260308T090000", "20260309T090000");
        }

        #endregion

        #region Durations

        [Fact]
        public void GetOccurrences_NominalDayAcrossADaylightSavingChange_KeepsTheLocalTime()
        {
            // arrange: a nominal day keeps 12:00 local time, so it is 23 hours long.
            var calendar = Read(Event(NewYorkMarch7NoonStart, "DURATION:P1D"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.End.ToString()).Should().Equal(March8NoonLocal);
            testResult.Select(occurrence => occurrence.EndUtc.ToString()).Should().Equal("20260308T160000Z");
        }

        [Fact]
        public void GetOccurrences_ExactDayAcrossADaylightSavingChange_KeepsTheExactTime()
        {
            // arrange: 24 exact hours end at 13:00 EDT.
            var calendar = Read(Event(NewYorkMarch7NoonStart, "DURATION:PT24H"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.End.ToString()).Should().Equal("20260308T130000");
            testResult.Select(occurrence => occurrence.EndUtc.ToString()).Should().Equal("20260308T170000Z");
            testResult.Should().OnlyContain(occurrence => occurrence.End.Value.DateTime.TimeZoneId == NewYork);
        }

        [Fact]
        public void GetOccurrences_NominalDurationWithAnExactPart_AddsTheExactPartAfterTheDays()
        {
            // arrange
            var calendar = Read(Event(NewYorkMarch7NoonStart, "DURATION:P1DT2H"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.End.ToString()).Should().Equal("20260308T140000");
            testResult.Select(occurrence => occurrence.EndUtc.ToString()).Should().Equal("20260308T180000Z");
        }

        [Fact]
        public void GetOccurrences_DtEnd_GivesEveryInstanceTheSameExactDuration()
        {
            // arrange: ref. RFC 5545 §3.8.5.3: DTEND gives the first instance 23 exact hours, across the change, and so every instance.
            var calendar = Read(Event(NewYorkMarch7NoonStart, "DTEND;TZID=America/New_York:20260308T120000", DailyTwice));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.End.ToString()).Should().Equal(March8NoonLocal, "20260309T110000");
        }

        [Fact]
        public void GetOccurrences_DtEndInAnotherTimeZone_EndsInThatTimeZone()
        {
            // arrange
            var calendar = Read(Event("DTSTART;TZID=America/New_York:20260601T090000", "DTEND;TZID=Europe/London:20260601T160000", WeeklyTwice));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Should().OnlyContain(occurrence => occurrence.End.Value.DateTime.TimeZoneId == London);
            testResult.Select(occurrence => occurrence.End.ToString()).Should().Equal("20260601T160000", "20260608T160000");
        }

        [Fact]
        public void GetOccurrences_EventWithNeitherDtEndNorDuration_EndsAtItsStart()
        {
            // arrange
            var calendar = Read(Event(NewYorkMarch7Start, DailyTwice));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Should().OnlyContain(occurrence => occurrence.End == occurrence.Start && occurrence.EndUtc == occurrence.StartUtc);
        }

        #endregion

        #region All-day

        [Fact]
        public void GetOccurrences_AllDayEvent_StaysADate()
        {
            // arrange
            var calendar = Read(Event(AllDayMarch1Start, WeeklyThreeTimes));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Should().OnlyContain(occurrence => occurrence.Start.IsDate && occurrence.End.Value.IsDate &&
                                                          occurrence.StartUtc == null && occurrence.EndUtc == null);
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260301/20260302", "20260308/20260309", "20260315/20260316");
        }

        [Fact]
        public void GetOccurrences_AllDayEventWithDtEnd_KeepsItsNumberOfDays()
        {
            // arrange
            var calendar = Read(Event(AllDayMarch1Start, "DTEND;VALUE=DATE:20260304", MonthlyTwice));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.End.ToString()).Should().Equal("20260304", "20260404");
        }

        [Fact]
        public void GetOccurrences_AllDayEventWithAWindowInUtc_ComparesTheDate()
        {
            // arrange
            var calendar = Read(Event(AllDayMarch1Start, Daily));
            var testEvent = calendar.Events.Single();
            var from = Utc("20260305T000000Z");
            var to = Utc("20260308T000000Z");

            // act
            var testResult = testEvent.GetOccurrences(calendar, from, to).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20260305", "20260306", March7);
        }

        #endregion

        #region Recurrence set

        [Fact]
        public void GetOccurrences_DtStartThatTheRuleDoesNotGive_IsTheFirstInstance()
        {
            // arrange: ref. RFC 5545 §3.3.10: DTSTART on a Saturday is an instance, although the rule gives only Mondays, and counts towards COUNT.
            var calendar = Read(Event(March7Start, TwoMondays));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March7NineUtc, "20260309T090000Z");
        }

        [Fact]
        public void GetOccurrences_RuleThatGivesDtStart_CountsItOnce()
        {
            // arrange
            var calendar = Read(Event("DTSTART:20260309T090000Z", TwoMondays));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20260309T090000Z", "20260316T090000Z");
        }

        [Fact]
        public void GetOccurrences_CountOfOneThatDoesNotGiveDtStart_GivesOnlyDtStart()
        {
            // arrange
            var calendar = Read(Event(AllDayMarch7Start, "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=1"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March7);
        }

        [Fact]
        public void GetOccurrences_AllDayDtStartThatTheRuleDoesNotGive_CountsTowardsCount()
        {
            // arrange
            var calendar = Read(Event(AllDayMarch7Start, "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=3"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March7, "20260309", "20260316");
        }

        [Fact]
        public void GetOccurrences_NonRecurringEvent_GivesOneInstance()
        {
            // arrange
            var calendar = Read(Event(March7Start, "DTEND:20260307T100000Z"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260307T090000Z/20260307T100000Z");
            testResult.Should().OnlyContain(occurrence => occurrence.RecurrenceId == occurrence.Start && !occurrence.IsOverride
                && occurrence.Component == testEvent);
        }

        [Fact]
        public void GetOccurrences_RDatePeriod_GivesTheInstanceItsOwnEnd()
        {
            // arrange
            var calendar = Read(Event(March2Start, OneHour, WeeklyTwice, "RDATE;VALUE=PERIOD:20260304T140000Z/20260304T170000Z,20260305T080000Z/PT30M"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260302T090000Z/20260302T100000Z",
                "20260304T140000Z/20260304T170000Z", "20260305T080000Z/20260305T083000Z", "20260309T090000Z/20260309T100000Z");
        }

        [Fact]
        public void GetOccurrences_RDateDatesAndDateTimes_AreAddedInOrder()
        {
            // arrange
            var calendar = Read(Event(NewYorkMarch2Start, "RDATE;TZID=America/New_York:20260310T090000,20260303T090000", "RDATE:20260305T120000Z"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should()
                .Equal(March2NineNewYorkInUtc, "20260303T140000Z", "20260305T120000Z", "20260310T130000Z");
        }

        [Fact]
        public void GetOccurrences_InstanceGivenMoreThanOnce_AppearsOnce()
        {
            // arrange
            var calendar = Read(Event(March2Start, DailyThreeTimes, "RDATE:20260303T090000Z,20260302T090000Z"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, March3NineUtc, March4NineUtc);
        }

        [Fact]
        public void GetOccurrences_ExDate_RemovesTheDtStartInstance()
        {
            // arrange
            var calendar = Read(Event(NewYorkMarch2Start, DailyThreeTimes, "EXDATE;TZID=America/New_York:20260302T090000"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March3NineLocal, "20260304T090000");
        }

        [Fact]
        public void GetOccurrences_ExDateInUtc_RemovesTheZonedInstanceAtThatInstant()
        {
            // arrange
            var calendar = Read(Event(NewYorkMarch2Start, DailyThreeTimes, "EXDATE:20260303T140000Z"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineLocal, "20260304T090000");
        }

        [Fact]
        public void GetOccurrences_ExDateAsADate_RemovesEveryInstanceOnThatDate()
        {
            // arrange
            var calendar = Read(Event(March2Start, "RRULE:FREQ=HOURLY;INTERVAL=8;COUNT=6", "EXDATE;VALUE=DATE:20260303"));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, "20260302T170000Z", "20260304T010000Z");
        }

        [Fact]
        public void GetOccurrences_ExDate_RemovesAnRDateInstance()
        {
            // arrange
            var calendar = Read(Event(March2Start, "RDATE:20260303T090000Z,20260304T090000Z", March3ExDate));
            var testEvent = calendar.Events.Single();

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, March4NineUtc);
        }

        #endregion

        #region Overrides

        [Fact]
        public void GetOccurrences_Override_ReplacesTheInstanceItNames()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(
                Event(NewYorkMarch2Start, OneHour, DailyThreeTimes, "SUMMARY:Stand-up"),
                Override("RECURRENCE-ID;TZID=America/New_York:20260303T090000", "DTSTART;TZID=America/New_York:20260303T113000", HalfAnHour,
                    "SUMMARY:Moved")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(
                March2NineToTenLocal, "20260303T113000/20260303T120000", "20260304T090000/20260304T100000");
            testResult.Select(occurrence => occurrence.Component.Summary).Should().Equal("Stand-up", "Moved", "Stand-up");
            testResult.Select(occurrence => occurrence.IsOverride).Should().Equal(false, true, false);
            testResult.Select(occurrence => occurrence.RecurrenceId.ToString()).Should().Equal(March2NineLocal, March3NineLocal, "20260304T090000");
        }

        [Fact]
        public void GetOccurrences_OverrideThatMovesAnInstanceLater_IsGivenInOrderOfStart()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(Event(March2Start, DailyFourTimes), Override(March3RecurrenceId, "DTSTART:20260305T120000Z")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should()
                .Equal(March2NineUtc, March4NineUtc, "20260305T090000Z", "20260305T120000Z");
        }

        [Fact]
        public void GetOccurrences_OverrideThatMovesAnInstanceIntoTheWindow_IsGiven()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(Event(March2Start, Daily), Override("RECURRENCE-ID:20260302T090000Z", "DTSTART:20270101T080000Z")));
            var master = calendar.Events.First();
            var from = Utc("20261231T120000Z");
            var to = Utc("20270102T000000Z");

            // act
            var testResult = master.GetOccurrences(calendar, from, to).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20270101T080000Z", "20270101T090000Z");
            testResult.Select(occurrence => occurrence.IsOverride).Should().Equal(true, false);
        }

        [Fact]
        public void GetOccurrences_OverrideMatchedByItsInstant_ReplacesAZonedInstance()
        {
            // arrange: the RECURRENCE-ID is in UTC, the instance in New York time; they name the same instant.
            var calendar = Read(IcalTestData.Lines(
                Event(NewYorkMarch2Start, DailyTwice),
                Override("RECURRENCE-ID:20260303T140000Z", "DTSTART;TZID=America/New_York:20260303T100000")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineLocal, "20260303T100000");
        }

        [Fact]
        public void GetOccurrences_ThisAndFutureOverride_ShiftsEveryLaterInstance()
        {
            // arrange: the override moves 09:00 to 10:30 and shortens the meeting, from the 4th onwards; the shift is in local time, so it holds
            // across the change to daylight saving time on the 8th.
            var calendar = Read(IcalTestData.Lines(
                Event(NewYorkMarch2Start, OneHour, "RRULE:FREQ=DAILY;COUNT=8", "SUMMARY:Before"),
                Override("RECURRENCE-ID;RANGE=THISANDFUTURE;TZID=America/New_York:20260304T090000",
                    "DTSTART;TZID=America/New_York:20260304T103000", HalfAnHour, "SUMMARY:After")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal(
                March2NineToTenLocal, "20260303T090000/20260303T100000", "20260304T103000/20260304T110000",
                "20260305T103000/20260305T110000", "20260306T103000/20260306T110000", "20260307T103000/20260307T110000",
                "20260308T103000/20260308T110000", "20260309T103000/20260309T110000");
            testResult.Select(occurrence => occurrence.Component.Summary).Should().Equal(
                BeforeSummary, BeforeSummary, AfterSummary, AfterSummary, AfterSummary, AfterSummary, AfterSummary, AfterSummary);
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should().EndWith("20260309T143000Z");
            testResult.Select(occurrence => occurrence.RecurrenceId.ToString()).Should().EndWith("20260309T090000");
        }

        [Fact]
        public void GetOccurrences_LaterThisAndFutureOverride_TakesOverFromAnEarlierOne()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(
                Event(March2Start, "RRULE:FREQ=DAILY;COUNT=6"),
                Override(March3ThisAndFutureRecurrenceId, March3TenStart),
                Override("RECURRENCE-ID;RANGE=THISANDFUTURE:20260305T090000Z", "DTSTART:20260305T080000Z")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, "20260303T100000Z", "20260304T100000Z",
                "20260305T080000Z", "20260306T080000Z", "20260307T080000Z");
        }

        [Fact]
        public void GetOccurrences_SingleOverrideAfterAThisAndFutureOverride_ReplacesOnlyItsInstance()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(
                Event(March2Start, DailyFourTimes),
                Override(March3ThisAndFutureRecurrenceId, March3TenStart),
                Override("RECURRENCE-ID:20260304T090000Z", "DTSTART:20260304T150000Z")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should()
                .Equal(March2NineUtc, "20260303T100000Z", "20260304T150000Z", "20260305T100000Z");
        }

        [Fact]
        public void GetOccurrences_ThisAndFutureOverrideOfAnAllDayEvent_ShiftsByWholeDays()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(
                Event(AllDayMarch2Start, WeeklyThreeTimes),
                Override("RECURRENCE-ID;RANGE=THISANDFUTURE;VALUE=DATE:20260309", "DTSTART;VALUE=DATE:20260310")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2, "20260310", "20260317");
        }

        [Fact]
        public void GetOccurrences_TwoOverridesOfTheSameInstance_TheHigherSequenceWins()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(
                Event(March2Start, DailyTwice),
                Override(March3RecurrenceId, March3NoonStart, "SEQUENCE:2"),
                Override(March3RecurrenceId, "DTSTART:20260303T110000Z", "SEQUENCE:1")));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, March3NoonUtc);
        }

        [Fact]
        public void GetOccurrences_OverrideOfAnExcludedInstance_IsIgnored()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(Event(March2Start, DailyThreeTimes, March3ExDate), Override(March3RecurrenceId, March3NoonStart)));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, March4NineUtc);
        }

        [Fact]
        public void GetOccurrences_ComponentsOfAnotherUidOrType_AreNotOverrides()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(
                Event(March2Start, DailyTwice),
                new[]
                {
                    IcalLines.BeginEvent, "UID:other@example.com", IcalLines.NewYearStamp, March3RecurrenceId, March3NoonStart, IcalLines.EndEvent,
                    IcalLines.BeginTodo, IcalLines.EventUid, IcalLines.NewYearStamp, March3RecurrenceId, March3NoonStart, IcalLines.EndTodo
                }));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, March3NineUtc);
        }

        [Fact]
        public void GetOccurrences_WithoutACalendar_IgnoresOverrides()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(Event(March2Start, DailyTwice), Override(March3RecurrenceId, March3NoonStart)));
            var master = calendar.Events.First();

            // act
            var testResult = master.GetOccurrences(null, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, March3NineUtc);
        }

        [Fact]
        public void GetOccurrences_OfAnOverride_GivesOnlyItsOwnInstance()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(Event(March2Start, DailyThreeTimes), Override(March3RecurrenceId, March3NoonStart, "DURATION:PT15M")));
            var overriding = calendar.Events.Last();

            // act
            var testResult = overriding.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260303T120000Z/20260303T121500Z");
            testResult.Select(occurrence => occurrence.RecurrenceId.ToString()).Should().Equal(March3NineUtc);
            testResult.Should().OnlyContain(occurrence => occurrence.Component == overriding && !occurrence.IsOverride);
        }

        #endregion

        #region Window and bounds

        [Fact]
        public void GetOccurrences_Window_IncludesItsStartAndExcludesItsEnd()
        {
            // arrange
            var calendar = Read(Event(March2Start, Daily));
            var testEvent = calendar.Events.Single();
            var from = Utc(March4NineUtc);
            var to = Utc("20260306T090000Z");

            // act
            var testResult = testEvent.GetOccurrences(calendar, from, to).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March4NineUtc, "20260305T090000Z");
        }

        [Fact]
        public void GetOccurrences_ZonedWindow_IsResolvedToUtc()
        {
            // arrange
            var calendar = Read(Event("DTSTART:20260302T130000Z", "RRULE:FREQ=HOURLY;COUNT=4"));
            var testEvent = calendar.Events.Single();
            var from = new IcalDateTime(new IcalDate(2026, 3, 2), new IcalTime(9, 0, 0), NewYork);

            // act
            var testResult = testEvent.GetOccurrences(calendar, from, null, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineNewYorkInUtc, "20260302T150000Z", "20260302T160000Z");
        }

        [Fact]
        public void GetOccurrences_WindowFarFromDtStart_GivesTheInstancesInIt()
        {
            // arrange
            var calendar = Read(Event("DTSTART;TZID=America/New_York:20000103T090000", "RRULE:FREQ=WEEKLY;BYDAY=MO"));
            var testEvent = calendar.Events.Single();
            var from = Utc("20260301T000000Z");
            var to = Utc("20260317T000000Z");

            // act
            var testResult = testEvent.GetOccurrences(calendar, from, to, CalendarThenTzData).ToList();

            // assert
            testResult.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal(March2NineNewYorkInUtc, "20260309T130000Z", "20260316T130000Z");
        }

        [Fact]
        public void GetOccurrences_RuleThatRepeatsForeverWithoutAnEnd_Throws()
        {
            // arrange
            var calendar = Read(Event(March2Start, Daily));
            var testEvent = calendar.Events.Single();
            var from = Utc("20260302T000000Z");

            // act
            Action testAction = () => testEvent.GetOccurrences(calendar, from, null);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("to");
        }

        [Fact]
        public void GetOccurrences_RuleThatNeverMatches_ThrowsAtTheIterationCap()
        {
            // arrange
            var calendar = Read(Event(March2Start, "RRULE:FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30"));
            var testEvent = calendar.Events.Single();
            var options = new IcalOptions { MaxRecurrenceIterations = 50 };
            var occurrences = testEvent.GetOccurrences(calendar, Utc("20260303T000000Z"), Utc("21000101T000000Z"), IcalTimeZoneResolver.Default, options);

            // act
            Action testAction = () => occurrences.ToList();

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void GetOccurrences_RuleWithoutEnd_GivesTheFirstInstancesWithoutReadingThemAll()
        {
            // arrange
            var calendar = Read(Event(March2Start, "RRULE:FREQ=MINUTELY"));
            var testEvent = calendar.Events.Single();
            var to = Utc("99991231T000000Z");

            // act
            var testResult = testEvent.GetOccurrences(calendar, null, to).Take(3).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, "20260302T090100Z", "20260302T090200Z");
        }

        [Fact]
        public void GetOccurrences_EnumeratedASecondTime_GivesTheSameInstances()
        {
            // arrange
            var calendar = Read(IcalTestData.Lines(Event(March2Start, DailyThreeTimes, "RDATE:20260310T090000Z"), Override(March3RecurrenceId, March3NoonStart)));
            var occurrences = calendar.Events.First().GetOccurrences(calendar, null, null);
            occurrences.ToList();

            // act
            var testResult = occurrences.ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, March3NoonUtc, March4NineUtc, "20260310T090000Z");
        }

        [Fact]
        public void GetOccurrences_EventWithoutDtStart_HasNoInstances()
        {
            // arrange
            var calendar = Read(Event(March2Start));
            var component = calendar.Events.Single();
            component.DtStart = null;

            // act
            var testResult = component.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetOccurrences_NullResolver_ThrowsArgumentNull()
        {
            // arrange
            var component = new IcalEvent();

            // act
            Action testAction = () => component.GetOccurrences(null, null, null, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("resolver");
        }

        [Fact]
        public void GetOccurrences_NullOptions_ThrowsArgumentNull()
        {
            // arrange
            var component = new IcalEvent();

            // act
            Action testAction = () => component.GetOccurrences(null, null, null, IcalTimeZoneResolver.Default, null);

            // assert
            testAction.Should().Throw<ArgumentNullException>().WithParameterName("options");
        }

        [Fact]
        public void GetOccurrences_NoRecurrenceIterations_ThrowsArgumentException()
        {
            // arrange
            var component = new IcalEvent();
            var options = new IcalOptions { MaxRecurrenceIterations = 0 };

            // act
            Action testAction = () => component.GetOccurrences(null, null, null, IcalTimeZoneResolver.Default, options);

            // assert
            testAction.Should().Throw<ArgumentException>().WithParameterName("options");
        }

        #endregion

        #region To-dos and journal entries

        [Fact]
        public void GetOccurrences_TodoWithDue_EndsAtTheSameExactDuration()
        {
            // arrange
            var calendar = Read(IcalLines.BeginTodo, TodoUid, IcalLines.NewYearStamp, March2Start, "DUE:20260302T170000Z", WeeklyTwice, IcalLines.EndTodo);
            var todo = calendar.Todos.Single();

            // act
            var testResult = todo.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.ToString()).Should().Equal("20260302T090000Z/20260302T170000Z", "20260309T090000Z/20260309T170000Z");
        }

        [Fact]
        public void GetOccurrences_TodoWithoutDueOrDuration_HasNoEnd()
        {
            // arrange
            var calendar = Read(IcalLines.BeginTodo, TodoUid, IcalLines.NewYearStamp, March2Start, DailyTwice, IcalLines.EndTodo);
            var todo = calendar.Todos.Single();

            // act
            var testResult = todo.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Should().HaveCount(2).And.OnlyContain(occurrence => occurrence.End == null && occurrence.EndUtc == null);
        }

        [Fact]
        public void GetOccurrences_TodoOverride_ReplacesTheInstance()
        {
            // arrange
            var calendar = Read(IcalLines.BeginTodo, TodoUid, IcalLines.NewYearStamp, March2Start, DailyTwice, IcalLines.EndTodo,
                IcalLines.BeginTodo, TodoUid, IcalLines.NewYearStamp, March3RecurrenceId, March3TenStart, IcalLines.EndTodo);
            var master = calendar.Todos.First();
            var overriding = calendar.Todos.Last();

            // act
            var testResult = master.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2NineUtc, "20260303T100000Z");
            testResult.Select(occurrence => occurrence.Component).Should().Equal(master, overriding);
        }

        [Fact]
        public void GetOccurrences_Journal_HasNoEnd()
        {
            // arrange
            var calendar = Read(IcalLines.BeginJournal, "UID:journal@example.com", IcalLines.NewYearStamp, AllDayMarch2Start, MonthlyTwice,
                IcalLines.EndJournal);
            var journal = calendar.Journals.Single();

            // act
            var testResult = journal.GetOccurrences(calendar, null, null).ToList();

            // assert
            testResult.Select(occurrence => occurrence.Start.ToString()).Should().Equal(March2, "20260402");
            testResult.Should().OnlyContain(occurrence => occurrence.End == null);
        }

        #endregion

        private static IcalCalendar Read(params string[] lines)
        {
            return ModelTestData.ReadCalendar(ModelTestData.Calendar(lines), UndefinedTimeZones);
        }

        private static string[] Event(params string[] lines)
        {
            return IcalTestData.Lines(new[] { IcalLines.BeginEvent, IcalLines.EventUid, IcalLines.NewYearStamp }, lines, new[] { IcalLines.EndEvent });
        }

        private static string[] Override(params string[] lines)
        {
            return Event(lines);
        }

        private static IcalDateTime Utc(string text)
        {
            return IcalDateTime.Parse(text.AsSpan(), IcalValueFormat.Text);
        }
    }
}
