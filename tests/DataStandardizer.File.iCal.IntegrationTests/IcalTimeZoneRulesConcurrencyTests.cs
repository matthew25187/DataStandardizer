using System;
using System.Collections.Generic;
using DataStandardizer.File.iCal.Tests;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <remarks>
    /// These tests run calls on several threads at once, so what they exercise depends on how the thread scheduler interleaves them.  Each
    /// still uses fixed inputs and compares its results exactly.
    /// </remarks>
    public class IcalTimeZoneRulesConcurrencyTests
    {
        private const string EasternStandardTime = "Eastern Standard Time";

        // How many threads share the rules, how many instants each looks up, and how far apart the instants are.
        private const int ThreadCount = 8;
        private const int InstantsPerThread = 500;
        private const int HoursApart = 61;

        // Each list of instants is looked up by one thread.
        private const int OneCallPerThread = 1;

        // Each thread starts this many instants further along than the one before it, so that the threads look up different years at once.
        private const int StartingPointStep = 37;

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        private static readonly DateTime StartOf2007Utc = new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The VTIMEZONE Outlook writes for its Windows zone "Eastern Standard Time", whose rules are those of 2007.
        /// </summary>
        private static readonly string[] OutlookEastern =
        {
            BeginTimeZone, "TZID:Eastern Standard Time",
            BeginStandard, "DTSTART:16011104T020000", "RRULE:FREQ=YEARLY;BYDAY=1SU;BYMONTH=11", "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500",
            EndStandard,
            BeginDaylight, "DTSTART:16010311T020000", "RRULE:FREQ=YEARLY;BYDAY=2SU;BYMONTH=3", "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400",
            EndDaylight,
            EndTimeZone
        };

        // For each thread, instants a little over two and a half days apart from 2007, in its own rotated order, and the offset TimeZoneInfo
        // gives each under the rules of 2007.
        public static TheoryData<IReadOnlyList<DateTime[]>, IReadOnlyList<TimeSpan[]>> InstantsForEachThread()
        {
            var instants = new List<DateTime[]>();
            var offsets = new List<TimeSpan[]>();
            for (var thread = 0; thread < ThreadCount; thread++)
            {
                var threadInstants = new DateTime[InstantsPerThread];
                var threadOffsets = new TimeSpan[InstantsPerThread];
                for (var index = 0; index < InstantsPerThread; index++)
                {
                    var step = (index + thread * StartingPointStep) % InstantsPerThread;
                    threadInstants[index] = StartOf2007Utc.AddHours((long)step * HoursApart);
                    threadOffsets[index] = TimeZoneTransitions.Eastern2007Rules.GetUtcOffset(threadInstants[index]);
                }

                instants.Add(threadInstants);
                offsets.Add(threadOffsets);
            }

            return new TheoryData<IReadOnlyList<DateTime[]>, IReadOnlyList<TimeSpan[]>> { { instants, offsets } };
        }

        [Theory]
        [MemberData(nameof(InstantsForEachThread), DisableDiscoveryEnumeration = true)]
        public void GetUtcOffset_CalendarRulesSharedAcrossThreads_MatchTheRulesOf2007(IReadOnlyList<DateTime[]> instants,
            IReadOnlyList<TimeSpan[]> expected)
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(OutlookEastern));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(EasternStandardTime, calendar);

            // act
            var testResult = ConcurrentRunner.Run(instants, OneCallPerThread, threadInstants => TimeZoneRulesProbe.GetUtcOffsets(rules, threadInstants), Timeout);

            // assert
            testResult.Completed.Should().BeTrue();
            testResult.Exceptions.Should().BeEmpty();
            testResult.Results.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }
    }
}
