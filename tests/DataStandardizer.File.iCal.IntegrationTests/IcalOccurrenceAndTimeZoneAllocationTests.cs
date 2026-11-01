using System;
using System.Collections.Generic;
using DataStandardizer.File.iCal.Tests;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// Checks that expanding a recurrence rule allocates nothing for each occurrence, and that the rules of a VTIMEZONE find an offset without
    /// allocating once they have expanded the onsets of its year.
    /// </summary>
    /// <remarks>
    /// These measure what the runtime allocates, which depends on the JIT and the target framework rather than only on the package, so they
    /// are integration tests.
    /// </remarks>
    public class IcalOccurrenceAndTimeZoneAllocationTests
    {
        private const string NewYork = Rfc5545Examples.NewYorkTimeZoneId;

        // 09:00 on New Year's Day 2026.
        private const string NewYear = "20260101T090000";

        // A rule with every kind of expansion and limit that is cheap to get wrong: BYDAY, BYHOUR and BYSETPOS, for 1,000 occurrences.
        private const string ThousandWeekdayOccurrences = "FREQ=MONTHLY;COUNT=1000;BYDAY=MO,TU,WE,TH,FR;BYHOUR=9,17;BYSETPOS=1,-1";

        private static readonly DateTime June2030Utc = new DateTime(2030, 6, 2, 16, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The America/New_York VTIMEZONE of RFC 5545 §3.6.5, from 2007 on.
        /// </summary>
        private static readonly string[] NewYorkFrom2007 =
        {
            BeginTimeZone, "TZID:America/New_York",
            BeginDaylight, "DTSTART:20070311T020000", "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU", "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", EndDaylight,
            BeginStandard, "DTSTART:20071104T020000", "RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU", "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500", EndStandard,
            EndTimeZone
        };

        [Fact]
        public void GetOccurrences_EachOccurrenceAfterTheFirst_DoesNotAllocate()
        {
            // arrange
            var start = IcalDateTime.Parse(NewYear.AsSpan(), NewYork, IcalValueFormat.Text);
            var rule = IcalRecurrenceRule.Parse(ThousandWeekdayOccurrences.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(() => EnumerateAfterTheFirst(rule.GetOccurrences(start, null, null)));

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void GetUtcOffset_CalendarRulesOnceUsed_DoesNotAllocate()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(NewYorkFrom2007));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(() => rules.GetUtcOffset(June2030Utc));

            // assert
            testResult.Should().Be(0);
        }

        /// <summary>
        /// Starts enumerating occurrences and moves to the first, then gives an action that enumerates the rest.
        /// </summary>
        private static Action EnumerateAfterTheFirst(IEnumerable<IcalDateTime> occurrences)
        {
            var enumerator = occurrences.GetEnumerator();
            enumerator.MoveNext();
            return () =>
            {
                while (enumerator.MoveNext())
                {
                }
            };
        }
    }
}
