using System;
using System.Linq;
using DataStandardizer.File.iCal.Tests;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;

namespace DataStandardizer.File.iCal.IntegrationTests
{
    /// <summary>
    /// Tests of <see cref="IcalTimeZoneResolver.System"/> over the operating system's own time zone data.
    /// </summary>
    /// <remarks>
    /// The data comes from the registry on Windows, and from ICU and the zoneinfo files elsewhere, so it depends on the machine and its
    /// updates.  The unit tests cover the same behaviour over time zones that they supply themselves.
    /// </remarks>
    public class IcalSystemTimeZoneResolverTests
    {
        private const string NewYork = Rfc5545Examples.NewYorkTimeZoneId;
        private const string London = "Europe/London";
        private const string EasternStandardTime = "Eastern Standard Time";
        private const string NotAZone = "Not/A_Zone";

        // A vendor prefix of the kind RFC 5545 §3.8.3.1 describes, as Mozilla writes it.
        private const string MozillaPrefix = "/mozilla.org/20050126_1/";

        // An event start in New York local time, with the Windows name of the time zone and no VTIMEZONE.
        private const string WindowsEventStart = "DTSTART;TZID=Eastern Standard Time:20260701T090000";

#if NET6_0_OR_GREATER
        // The names by which the system knows New York and London: .NET 6 and later know the TZ Database names.
        private const string SystemNewYork = NewYork;
        private const string SystemLondon = London;
#else
        // The names by which the system knows New York and London: .NET Framework knows only the Windows names.
        private const string SystemNewYork = EasternStandardTime;
        private const string SystemLondon = "GMT Standard Time";
#endif

        // MozillaPrefix followed by the name by which the system knows New York.
        private const string MozillaPrefixedSystemNewYork = MozillaPrefix + SystemNewYork;

        private static readonly TimeSpan EasternStandardOffset = TimeSpan.FromHours(-5);
        private static readonly TimeSpan EasternDaylightOffset = TimeSpan.FromHours(-4);

        private static readonly DateTime January2026Utc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime StartOf2026Utc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime StartOf2027Utc = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // New York's transitions to daylight and to standard time in 2026, and the day before the first.
        private static readonly DateTime NewYorkDaylightOnset2026 = new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime NewYorkStandardOnset2026 = new DateTime(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime DayBeforeNewYorkDaylightOnset2026 = new DateTime(2026, 3, 7, 7, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime DayAfterNewYorkDaylightOnset2026 = new DateTime(2026, 3, 9, 7, 0, 0, DateTimeKind.Utc);

        // ref. RFC 5545 §3.3.5: a New York local time in the gap of 2026, read with the offset before the gap, and one in the overlap of 2026,
        // read as its first occurrence.
        private static readonly DateTime NewYorkTimeInGap = new DateTime(2026, 3, 8, 2, 30, 0);
        private static readonly DateTime NewYorkTimeInGapInUtc = new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime NewYorkTimeInOverlap = new DateTime(2026, 11, 1, 1, 30, 0);
        private static readonly DateTime NewYorkTimeInOverlapInUtc = new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc);

        // The event start of WindowsEventStart in UTC.
        private static readonly IcalDateTime WindowsEventStartInUtc = new IcalDateTime(new IcalDate(2026, 7, 1), new IcalTime(13, 0, 0, true));

        private static readonly IcalOptions UndefinedTimeZones = new IcalOptions() { AllowUndefinedTimeZones = true };

        // The name by which the system knows each time zone, and its changes of offset in 2026.
        public static TheoryData<string, TimeZoneTransition[]> SystemZones => new TheoryData<string, TimeZoneTransition[]>()
        {
            { SystemNewYork, TimeZoneTransitions.NewYork2026 },
            { SystemLondon, TimeZoneTransitions.LondonAndDublin2026 },
        };

        public static TheoryData<DateTime, DateTime> NewYorkGapAndOverlap => new TheoryData<DateTime, DateTime>
        {
            { NewYorkTimeInGap, NewYorkTimeInGapInUtc },
            { NewYorkTimeInOverlap, NewYorkTimeInOverlapInUtc },
        };

        [Theory]
        [MemberData(nameof(SystemZones), DisableDiscoveryEnumeration = true)]
        public void GetTransitions_SystemZone_GivesTheChangesOfOffsetIn2026(string systemName, TimeZoneTransition[] expected)
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(systemName, null);

            // act
            var testResult = TimeZoneRulesProbe.GetOffsetChanges(rules, StartOf2026Utc, StartOf2027Utc);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(SystemZones), DisableDiscoveryEnumeration = true)]
        public void Resolve_SystemZone_HasTheTimeZoneId(string systemName, TimeZoneTransition[] transitions)
        {
            // arrange
            _ = transitions;

            // act
            var testResult = IcalTimeZoneResolver.System.Resolve(systemName, null);

            // assert
            testResult.TimeZoneId.Should().Be(systemName);
        }

        [Fact]
        public void ToUtc_ChainWithSystemWindowsName_ConvertsTheEventStart()
        {
            // arrange
            var calendar = ReadCalendarWithWindowsEventStart();
            var start = calendar.Events.Single().DtStart.Value.DateTime;
            var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData, IcalTimeZoneResolver.System);

            // act
            var testResult = resolver.ToUtc(start, calendar);

            // assert
            testResult.Should().Be(WindowsEventStartInUtc);
        }

        [Fact]
        public void GetTransitions_ChainWithSystemWindowsName_GivesTheChangesOfOffsetOfNewYorkIn2026()
        {
            // arrange
            var calendar = ReadCalendarWithWindowsEventStart();
            var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData, IcalTimeZoneResolver.System);
            var rules = resolver.Resolve(EasternStandardTime, calendar);

            // act
            var testResult = TimeZoneRulesProbe.GetOffsetChanges(rules, StartOf2026Utc, StartOf2027Utc);

            // assert
            testResult.Should().Equal(TimeZoneTransitions.NewYork2026);
        }

        [Fact]
        public void GetUtcOffset_SystemVendorPrefixedName_IsTheNewYorkOffset()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(MozillaPrefixedSystemNewYork, null);

            // act
            var testResult = rules.GetUtcOffset(January2026Utc);

            // assert
            testResult.Offset.Should().Be(EasternStandardOffset);
        }

        [Theory]
        [InlineData(NotAZone)]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("/example.com/")]
        [InlineData("../../../../etc/passwd")]
        [InlineData("America/../America/New_York")]
        [InlineData("C:\\Windows\\win.ini")]
        [InlineData("America/New_York\0")]
        public void TryResolve_SystemUnknownOrUnsafeName_ReturnsFalse(string timeZoneId)
        {
            // act
            var testResult = IcalTimeZoneResolver.System.TryResolve(timeZoneId, null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void GetUtcOffsetsForLocal_SystemTimeInAGap_IsEmpty()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemNewYork, null);

            // act
            var testResult = rules.GetUtcOffsetsForLocal(NewYorkTimeInGap);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetUtcOffsetsForLocal_SystemTimeInAnOverlap_GivesBothOffsetsInOrder()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemNewYork, null);

            // act
            var testResult = rules.GetUtcOffsetsForLocal(NewYorkTimeInOverlap);

            // assert
            testResult.Select(offset => offset.Offset).Should().Equal(EasternDaylightOffset, EasternStandardOffset);
        }

        [Theory]
        [MemberData(nameof(NewYorkGapAndOverlap))]
        public void ToUtc_SystemTimeInAGapOrOverlap_FollowsRfc5545(DateTime local, DateTime expected)
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemNewYork, null);

            // act
            var testResult = rules.ToUtc(local);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void GetTransitions_SystemNewYorkIn2026_GivesTheKindOfEachButNoName()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemNewYork, null);

            // act
            var testResult = rules.GetTransitions(StartOf2026Utc, StartOf2027Utc).ToList();

            // assert
            testResult.Select(transition => (transition.InstantUtc, transition.Kind)).Should().Equal(
                (NewYorkDaylightOnset2026, IcalTimeZoneObservanceKind.Daylight),
                (NewYorkStandardOnset2026, IcalTimeZoneObservanceKind.Standard));
            testResult.Should().OnlyContain(transition => transition.Name == null);
        }

        [Fact]
        public void GetTransitions_SystemPeriodStartingAtATransition_GivesIt()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemNewYork, null);

            // act
            var testResult = rules.GetTransitions(NewYorkDaylightOnset2026, DayAfterNewYorkDaylightOnset2026).ToList();

            // assert
            testResult.Select(transition => transition.InstantUtc).Should().Equal(NewYorkDaylightOnset2026);
        }

        [Fact]
        public void GetTransitions_SystemPeriodEndingAtATransition_DoesNotGiveIt()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemNewYork, null);

            // act
            var testResult = rules.GetTransitions(DayBeforeNewYorkDaylightOnset2026, NewYorkDaylightOnset2026).ToList();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetTransitions_SystemEmptyPeriodAtATransition_GivesNothing()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemNewYork, null);

            // act
            var testResult = rules.GetTransitions(NewYorkDaylightOnset2026, NewYorkDaylightOnset2026).ToList();

            // assert
            testResult.Should().BeEmpty();
        }

        private static IcalCalendar ReadCalendarWithWindowsEventStart()
        {
            return ModelTestData.ReadCalendar(ModelTestData.Calendar(new[]
            {
                BeginEvent, EventUid, NewYearStamp, WindowsEventStart, EndEvent
            }), UndefinedTimeZones);
        }
    }
}
