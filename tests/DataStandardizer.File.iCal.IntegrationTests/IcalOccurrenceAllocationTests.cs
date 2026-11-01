using System;
using System.Collections.Generic;
using DataStandardizer.File.iCal.Tests;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// Checks that expanding a recurrence rule allocates nothing for each occurrence.
    /// </summary>
    /// <remarks>
    /// These measure what the runtime allocates, which depends on the JIT and the target framework rather than only on the package, so they
    /// are integration tests.
    /// </remarks>
    public class IcalOccurrenceAllocationTests
    {
        private const string NewYork = Rfc5545Examples.NewYorkTimeZoneId;

        // 09:00 on New Year's Day 2026.
        private const string NewYear = "20260101T090000";

        // A rule with every kind of expansion and limit that is cheap to get wrong: BYDAY, BYHOUR and BYSETPOS, for 1,000 occurrences.
        private const string ThousandWeekdayOccurrences = "FREQ=MONTHLY;COUNT=1000;BYDAY=MO,TU,WE,TH,FR;BYHOUR=9,17;BYSETPOS=1,-1";

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
