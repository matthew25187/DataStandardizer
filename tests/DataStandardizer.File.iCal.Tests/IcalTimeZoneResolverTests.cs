using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataStandardizer.Chronology;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;

namespace DataStandardizer.File.iCal.Tests
{
    public class IcalTimeZoneResolverTests
    {
        private const string NewYork = Rfc5545Examples.NewYorkTimeZoneId;
        private const string London = "Europe/London";
        private const string Dublin = "Europe/Dublin";
        private const string Kolkata = "Asia/Kolkata";
        private const string EasternStandardTime = "Eastern Standard Time";
        private const string NotAZone = "Not/A_Zone";
        private const string VendorPrefixOnly = "/example.com/";
        private const string TieTimeZoneId = "Test/Tie";
        private const string OrderTimeZoneId = "Test/Order";

        // 09:00 on July 1, 2026, in New York local time and in UTC.
        private const string July1Local = "20260701T090000";
        private const string July1NewYorkInUtc = "20260701T130000Z";

        // ref. RFC 5545 §3.6.5: the offsets and names of Eastern Standard Time and Eastern Daylight Time.
        private const string FromEasternStandard = "TZOFFSETFROM:-0500";
        private const string ToEasternStandard = "TZOFFSETTO:-0500";
        private const string FromEasternDaylight = "TZOFFSETFROM:-0400";
        private const string ToEasternDaylight = "TZOFFSETTO:-0400";
        private const string EasternDaylightName = "TZNAME:EDT";
        private const string EasternStandardName = "TZNAME:EST";

        private const string FromZero = "TZOFFSETFROM:+0000";
        private const string ToZero = "TZOFFSETTO:+0000";
        private const string FromOneHourAhead = "TZOFFSETFROM:+0100";
        private const string ToOneHourAhead = "TZOFFSETTO:+0100";
        private const string FromTwoHoursAhead = "TZOFFSETFROM:+0200";
        private const string FromThreeHoursAhead = "TZOFFSETFROM:+0300";
        private const string ToThreeHoursAhead = "TZOFFSETTO:+0300";
        private const string StartOf1970 = "DTSTART:19700101T000000";
        private const string StartOf2026 = "DTSTART:20260101T000000";
        private const string HalfPastOneOnNewYearsDay = "DTSTART:20260101T013000";
        private const string OrderTimeZone = "TZID:Test/Order";

        private const string FirstResolver = "first";
        private const string NextResolver = "next";

        private static readonly IcalOptions UndefinedTimeZones = new IcalOptions() { AllowUndefinedTimeZones = true };

        private static readonly IcalOptions InvalidValues = new IcalOptions() { AllowInvalidValues = true };

        private static readonly DateTime July2026Utc = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime January2026Utc = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        // ref. RFC 5545 §3.3.5: 02:30 does not exist on 2026-03-08 in New York, and 01:30 happens twice on 2026-11-01.
        private static readonly DateTime NewYorkTimeInGap = new DateTime(2026, 3, 8, 2, 30, 0);
        private static readonly DateTime NewYorkTimeInGapInUtc = new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc);
        private static readonly DateTime NewYorkTimeInOverlap = new DateTime(2026, 11, 1, 1, 30, 0);
        private static readonly DateTime NewYorkTimeInOverlapInUtc = new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc);

        // The first transition of the TestOrder time zones, at 23:30 UTC on December 31, 2025.
        private static readonly DateTime OrderFirstOnsetUtc = new DateTime(2025, 12, 31, 23, 30, 0, DateTimeKind.Utc);

        private static readonly TimeSpan EasternStandardOffset = TimeSpan.FromHours(-5);
        private static readonly TimeSpan EasternDaylightOffset = TimeSpan.FromHours(-4);

        /// <summary>
        /// The America/New_York VTIMEZONE of RFC 5545 §3.6.5, which covers the rules from 1967 with RRULE, RDATE and UNTIL in UTC.
        /// </summary>
        private static readonly string[] RfcNewYork =
        {
            BeginTimeZone, "TZID:America/New_York", "LAST-MODIFIED:20050809T050000Z",
            BeginDaylight, "DTSTART:19670430T020000", "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=-1SU;UNTIL=19730429T070000Z",
            FromEasternStandard, ToEasternDaylight, EasternDaylightName, EndDaylight,
            BeginStandard, "DTSTART:19671029T020000", "RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU;UNTIL=20061029T060000Z",
            FromEasternDaylight, ToEasternStandard, EasternStandardName, EndStandard,
            BeginDaylight, "DTSTART:19740106T020000", "RDATE:19750223T020000", FromEasternStandard, ToEasternDaylight, EasternDaylightName,
            EndDaylight,
            BeginDaylight, "DTSTART:19760425T020000", "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=-1SU;UNTIL=19860427T070000Z",
            FromEasternStandard, ToEasternDaylight, EasternDaylightName, EndDaylight,
            BeginDaylight, "DTSTART:19870405T020000", "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=1SU;UNTIL=20060402T070000Z",
            FromEasternStandard, ToEasternDaylight, EasternDaylightName, EndDaylight,
            BeginDaylight, "DTSTART:20070311T020000", "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU",
            FromEasternStandard, ToEasternDaylight, EasternDaylightName, EndDaylight,
            BeginStandard, "DTSTART:20071104T020000", "RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU",
            FromEasternDaylight, ToEasternStandard, EasternStandardName, EndStandard,
            EndTimeZone
        };

        /// <summary>
        /// The VTIMEZONE Outlook writes for its Windows zone "Eastern Standard Time", with rules from 1601.
        /// </summary>
        private static readonly string[] OutlookEastern =
        {
            BeginTimeZone, "TZID:Eastern Standard Time",
            BeginStandard, "DTSTART:16011104T020000", "RRULE:FREQ=YEARLY;BYDAY=1SU;BYMONTH=11", FromEasternDaylight, ToEasternStandard,
            EndStandard,
            BeginDaylight, "DTSTART:16010311T020000", "RRULE:FREQ=YEARLY;BYDAY=2SU;BYMONTH=3", FromEasternStandard, ToEasternDaylight,
            EndDaylight,
            EndTimeZone
        };

        /// <summary>
        /// A VTIMEZONE that gives America/New_York a fixed offset of +03:00, so that a resolver using it can be told from one using the TZ
        /// Database.
        /// </summary>
        private static readonly string[] FakeNewYork =
        {
            BeginTimeZone, "TZID:America/New_York",
            BeginStandard, StartOf1970, FromThreeHoursAhead, ToThreeHoursAhead, EndStandard,
            EndTimeZone
        };

        /// <summary>
        /// A VTIMEZONE whose two observances have an onset at the same instant (#262).
        /// </summary>
        private static readonly string[] TiedObservances =
        {
            BeginTimeZone, "TZID:Test/Tie",
            BeginStandard, StartOf2026, FromZero, ToOneHourAhead, EndStandard,
            BeginDaylight, StartOf2026, FromZero, "TZOFFSETTO:+0200", EndDaylight,
            EndTimeZone
        };

        /// <summary>
        /// A VTIMEZONE whose TZID is empty (#266).
        /// </summary>
        private static readonly string[] EmptyTimeZoneId =
        {
            BeginTimeZone, "TZID:",
            BeginStandard, StartOf1970, FromOneHourAhead, ToOneHourAhead, EndStandard,
            EndTimeZone
        };

        public static TheoryData<string> AccuracyZones => new TheoryData<string>() { NewYork, London, Dublin };

        public static TheoryData<string> SystemZones => new TheoryData<string>() { NewYork, London };

        public static TheoryData<string, string> NewYorkCalendars => new TheoryData<string, string>()
        {
            { "RFC 5545 example", ModelTestData.Calendar(RfcNewYork) },
            { "FromTzData", WriteCalendar(IcalTimeZone.FromTzData(TzDataTimezone.Parse(NewYork), Utc(2020), Utc(2030))) }
        };

        public static TheoryData<IIcalTimeZoneResolver> Resolvers => new TheoryData<IIcalTimeZoneResolver>
        {
            IcalTimeZoneResolver.Calendar,
            IcalTimeZoneResolver.TzData,
            IcalTimeZoneResolver.System,
            IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.TzData),
        };

        // Each call to the rules with a time of the wrong kind.
        public static TheoryData<Action> WrongKindOfTimeCalls()
        {
            var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);
            return new TheoryData<Action>
            {
                () => rules.GetUtcOffsetsForLocal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                () => rules.ToUtc(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)),
                () => rules.GetUtcOffset(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local)),
            };
        }

        // A local time in a gap and one in an overlap, in New York, with the UTC time that RFC 5545 §3.3.5 gives each.
        public static TheoryData<DateTime, DateTime> NewYorkGapAndOverlap => new TheoryData<DateTime, DateTime>
        {
            { NewYorkTimeInGap, NewYorkTimeInGapInUtc },
            { NewYorkTimeInOverlap, NewYorkTimeInOverlapInUtc },
        };

        // The VTIMEZONEs whose observances are listed in either order, as in #278.
        public static TheoryData<string, string[]> ObservancesInEitherOrder => new TheoryData<string, string[]>
        {
            {
                // STANDARD's local onset is the later, but its UTC onset (23:30Z) is before DAYLIGHT's (01:00Z).
                "STANDARD first",
                new[]
                {
                    BeginTimeZone, OrderTimeZone,
                    BeginStandard, HalfPastOneOnNewYearsDay, FromTwoHoursAhead, ToZero, EndStandard,
                    BeginDaylight, "DTSTART:20260101T010000", FromZero, ToOneHourAhead, EndDaylight,
                    EndTimeZone
                }
            },
            {
                // Both onsets are at 23:30Z, from different offsets; the last observance is the one in use, as in GetTransitions.
                "same instant",
                new[]
                {
                    BeginTimeZone, OrderTimeZone,
                    BeginDaylight, "DTSTART:20260101T003000", FromOneHourAhead, ToThreeHoursAhead, EndDaylight,
                    BeginStandard, HalfPastOneOnNewYearsDay, FromTwoHoursAhead, ToZero, EndStandard,
                    EndTimeZone
                }
            },
        };

        // ref. RFC 5545 §3.6.5: an onset of the New York VTIMEZONE from each of its DTSTART, RDATE and RRULE, with a day either side of it and
        // half a second before it.
        public static TheoryData<string, DateTime, DateTime, DateTime, DateTime> NewYorkOnsets()
        {
            var data = new TheoryData<string, DateTime, DateTime, DateTime, DateTime>();
            var onsets = new[]
            {
                ("DTSTART", new DateTime(1974, 1, 6, 7, 0, 0, DateTimeKind.Utc)),
                ("RDATE", new DateTime(1975, 2, 23, 7, 0, 0, DateTimeKind.Utc)),
                ("RRULE", new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc)),
            };
            foreach (var (source, onset) in onsets)
            {
                data.Add(source, onset, onset.AddDays(-1), onset.AddDays(1), onset.AddMilliseconds(-500));
            }

            return data;
        }

        #region Resolver behaviour

        [Fact]
        public void Default_Get_IsTheCalendarResolver()
        {
            // act
            var testResult = IcalTimeZoneResolver.Default;

            // assert
            testResult.Should().BeSameAs(IcalTimeZoneResolver.Calendar);
        }

        [Fact]
        public void Resolve_DefaultWithVTimeZoneForATzDataName_HasTheTimeZoneId()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));

            // act
            var testResult = IcalTimeZoneResolver.Default.Resolve(NewYork, calendar);

            // assert
            testResult.TimeZoneId.Should().Be(NewYork);
        }

        [Fact]
        public void GetUtcOffset_DefaultWithVTimeZoneForATzDataName_IsTheVTimeZoneOffset()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));
            var rules = IcalTimeZoneResolver.Default.Resolve(NewYork, calendar);

            // act
            var testResult = rules.GetUtcOffset(July2026Utc);

            // assert
            testResult.Offset.Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void TryResolve_DefaultWithoutVTimeZone_ReturnsFalse()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.EventCalendar("DTSTART;TZID=Europe/London:20260701T090000"), UndefinedTimeZones);

            // act
            var testResult = IcalTimeZoneResolver.Default.TryResolve(London, calendar, out var rules);

            // assert
            testResult.Should().BeFalse();
            rules.Should().BeNull();
        }

        [Fact]
        public void Resolve_DefaultWithoutVTimeZone_ThrowsIcalFileException()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.EventCalendar("DTSTART;TZID=Europe/London:20260701T090000"), UndefinedTimeZones);

            // act
            Action testAction = () => IcalTimeZoneResolver.Default.Resolve(London, calendar);

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*'Europe/London'*");
        }

        [Fact]
        public void TryResolve_CalendarResolverWithoutACalendar_ReturnsFalse()
        {
            // act
            var testResult = IcalTimeZoneResolver.Calendar.TryResolve(NewYork, null, out var rules);

            // assert
            testResult.Should().BeFalse();
            rules.Should().BeNull();
        }

        [Fact]
        public void GetUtcOffset_ObservancesWithTheSameOnsetAtTheTransition_IsTheOffsetOfTheTransition()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(TiedObservances));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(TieTimeZoneId, calendar);
            var transition = rules.GetTransitions(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Single();

            // act
            var testResult = rules.GetUtcOffset(transition.InstantUtc);

            // assert
            testResult.Should().Be(transition.OffsetTo);
        }

        [Fact]
        public void GetUtcOffset_ObservancesWithTheSameOnsetAfterTheTransition_IsTheOffsetOfTheTransition()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(TiedObservances));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(TieTimeZoneId, calendar);
            var transition = rules.GetTransitions(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Single();

            // act
            var testResult = rules.GetUtcOffset(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));

            // assert
            testResult.Should().Be(transition.OffsetTo);
        }

        [Fact]
        public void TryResolve_CalendarResolverIdentifierInOtherCase_ReturnsFalse()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));

            // act
            var testResult = IcalTimeZoneResolver.Calendar.TryResolve("america/new_york", calendar, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void TryResolve_CalendarResolverEmptyIdentifier_ReturnsFalse()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(EmptyTimeZoneId), InvalidValues);

            // act
            var testResult = IcalTimeZoneResolver.Calendar.TryResolve("", calendar, out var rules);

            // assert
            testResult.Should().BeFalse();
            rules.Should().BeNull();
        }

        [Fact]
        public void Resolve_CalendarResolverEmptyIdentifier_ThrowsIcalFileException()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(EmptyTimeZoneId), InvalidValues);

            // act
            Action testAction = () => IcalTimeZoneResolver.Calendar.Resolve("", calendar);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void TryResolve_ChainEmptyIdentifier_ReturnsFalse()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(EmptyTimeZoneId), InvalidValues);
            var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.TzData, IcalTimeZoneResolver.Calendar);

            // act
            var testResult = resolver.TryResolve("", calendar, out var rules);

            // assert
            testResult.Should().BeFalse();
            rules.Should().BeNull();
        }

        [Fact]
        public void GetUtcOffset_ChainCalendarThenTzDataWithVTimeZone_UsesTheVTimeZone()
        {
            // arrange
            var calendar = ReadCalendarWithFakeNewYorkAndLondonEvent();
            var rules = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData).Resolve(NewYork, calendar);

            // act
            var testResult = rules.GetUtcOffset(July2026Utc);

            // assert
            testResult.Offset.Should().Be(TimeSpan.FromHours(3));
        }

        [Fact]
        public void GetUtcOffset_ChainCalendarThenTzDataWithoutVTimeZone_UsesTzData()
        {
            // arrange
            var calendar = ReadCalendarWithFakeNewYorkAndLondonEvent();
            var rules = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData).Resolve(London, calendar);

            // act
            var testResult = rules.GetUtcOffset(July2026Utc);

            // assert
            testResult.Offset.Should().Be(TimeSpan.FromHours(1));
        }

        [Fact]
        public void GetUtcOffset_ChainTzDataThenCalendar_UsesTzData()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));
            var rules = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.TzData, IcalTimeZoneResolver.Calendar).Resolve(NewYork, calendar);

            // act
            var testResult = rules.GetUtcOffset(July2026Utc);

            // assert
            testResult.Offset.Should().Be(EasternDaylightOffset);
        }

        [Fact]
        public void TryResolve_Chain_TriesEachResolverInOrderUntilOneResolves()
        {
            // arrange
            var calls = new List<string>();
            var first = new RecordingResolver(FirstResolver, calls, null);
            var second = new RecordingResolver("second", calls, IcalTimeZoneResolver.TzData);
            var third = new RecordingResolver("third", calls, IcalTimeZoneResolver.TzData);
            var chain = IcalTimeZoneResolver.Chain(first, second, third);

            // act
            var testResult = chain.TryResolve(NewYork, null, out var rules);

            // assert
            testResult.Should().BeTrue();
            rules.Should().NotBeNull();
            calls.Should().Equal(FirstResolver, "second");
        }

        [Fact]
        public void TryResolve_ChainWhenNothingResolves_ReturnsFalse()
        {
            // arrange
            var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

            // act
            var testResult = resolver.TryResolve(NotAZone, null, out var rules);

            // assert
            testResult.Should().BeFalse();
            rules.Should().BeNull();
        }

        [Fact]
        public void Chain_Null_ThrowsArgumentNull()
        {
            // act
            Action testAction = () => IcalTimeZoneResolver.Chain(null);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Chain_NoResolvers_ThrowsArgumentException()
        {
            // act
            Action testAction = () => IcalTimeZoneResolver.Chain();

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Chain_NullResolver_ThrowsArgumentException()
        {
            // act
            Action testAction = () => IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, null);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TryResolve_ChainAfterItsArrayChanges_UsesTheResolversItWasGiven()
        {
            // arrange
            var resolvers = new[] { IcalTimeZoneResolver.TzData };
            var chain = IcalTimeZoneResolver.Chain(resolvers);
            resolvers[0] = IcalTimeZoneResolver.Calendar;

            // act
            var testResult = chain.TryResolve(NewYork, null, out _);

            // assert
            testResult.Should().BeTrue();
        }

        [Theory]
        [InlineData(NewYork)]
        [InlineData("US/Eastern")]
        [InlineData("/mozilla.org/20050126_1/America/New_York")]
        [InlineData("/softwarestudio.org/Olson_20011030_5/America/New_York")]
        [InlineData("/citadel.org/20190914_1/America/New_York")]
        public void Resolve_TzDataNameWithOrWithoutVendorPrefix_KeepsTheIdentifier(string timeZoneId)
        {
            // act
            var testResult = IcalTimeZoneResolver.TzData.Resolve(timeZoneId, null);

            // assert
            testResult.TimeZoneId.Should().Be(timeZoneId);
        }

        [Theory]
        [InlineData(NewYork)]
        [InlineData("US/Eastern")]
        [InlineData("/mozilla.org/20050126_1/America/New_York")]
        [InlineData("/softwarestudio.org/Olson_20011030_5/America/New_York")]
        [InlineData("/citadel.org/20190914_1/America/New_York")]
        public void GetUtcOffset_TzDataNameWithOrWithoutVendorPrefix_IsTheNewYorkOffset(string timeZoneId)
        {
            // arrange
            var rules = IcalTimeZoneResolver.TzData.Resolve(timeZoneId, null);

            // act
            var testResult = rules.GetUtcOffset(January2026Utc);

            // assert
            testResult.Offset.Should().Be(EasternStandardOffset);
        }

        [Theory]
        [InlineData(EasternStandardTime)]
        [InlineData("/")]
        [InlineData(VendorPrefixOnly)]
        [InlineData("")]
        public void TryResolve_TzDataOtherName_ReturnsFalse(string timeZoneId)
        {
            // act
            var testResult = IcalTimeZoneResolver.TzData.TryResolve(timeZoneId, null, out _);

            // assert
            testResult.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(Resolvers), DisableDiscoveryEnumeration = true)]
        public void TryResolve_NullIdentifier_ThrowsArgumentNull(IIcalTimeZoneResolver resolver)
        {
            // act
            Action testAction = () => resolver.TryResolve(null, null, out _);

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TryResolve_CalendarObservanceWithoutDtStart_ThrowsIcalFileException()
        {
            // arrange
            var timeZone = new IcalTimeZone { TimeZoneId = "Broken" };
            timeZone.Components.Add(new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard)
            {
                OffsetFrom = new IcalUtcOffset(TimeSpan.Zero), OffsetTo = new IcalUtcOffset(TimeSpan.Zero)
            });
            var calendar = new IcalCalendar();
            calendar.Components.Add(timeZone);

            // act
            Action testAction = () => IcalTimeZoneResolver.Calendar.TryResolve(timeZone.TimeZoneId, calendar, out _);

            // assert
            testAction.Should().Throw<IcalFileException>().WithMessage("*DTSTART*");
        }

        [Fact]
        public void TryResolve_CalendarVTimeZoneWithoutObservances_ThrowsIcalFileException()
        {
            // arrange
            var timeZone = new IcalTimeZone { TimeZoneId = "Empty" };
            var calendar = new IcalCalendar();
            calendar.Components.Add(timeZone);

            // act
            Action testAction = () => IcalTimeZoneResolver.Calendar.TryResolve(timeZone.TimeZoneId, calendar, out _);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void GetUtcOffset_VTimeZoneChangedAfterResolving_UsesTheVTimeZoneAsItWasResolved()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);
            calendar.TimeZones.Single().Observances.Single().OffsetTo = new IcalUtcOffset(TimeSpan.FromHours(7));

            // act
            var testResult = rules.GetUtcOffset(July2026Utc);

            // assert
            testResult.Offset.Should().Be(TimeSpan.FromHours(3));
        }

        #endregion

        #region Conversions

        [Fact]
        public void ToUtc_ZonedTime_ConvertsToUtc()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
            var local = IcalDateTime.Parse(July1Local.AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            var testResult = IcalTimeZoneResolver.Default.ToUtc(local, calendar);

            // assert
            testResult.Should().Be(new IcalDateTime(new IcalDate(2026, 7, 1), new IcalTime(13, 0, 0, true)));
        }

        [Fact]
        public void ToUtc_UtcTime_ReturnsItAsItIs()
        {
            // arrange
            var value = IcalDateTime.Parse("20260701T090000Z".AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = IcalTimeZoneResolver.Default.ToUtc(value, null);

            // assert
            testResult.Should().Be(value);
        }

        [Fact]
        public void ToUtc_FloatingTime_ThrowsArgumentException()
        {
            // arrange
            var value = IcalDateTime.Parse(July1Local.AsSpan(), IcalValueFormat.Text);

            // act
            Action testAction = () => IcalTimeZoneResolver.Default.ToUtc(value, null);

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToUtc_WhenNothingResolves_Throws()
        {
            // arrange
            var value = IcalDateTime.Parse(July1Local.AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            Action testAction = () => IcalTimeZoneResolver.Default.ToUtc(value, null);

            // assert
            testAction.Should().Throw<IcalFileException>();
        }

        [Fact]
        public void ToUtc_LeapSecond_IsTheSecondAfterTheOneBeforeIt()
        {
            // arrange
            var leapSecond = IcalDateTime.Parse("20161231T185960".AsSpan(), NewYork, IcalValueFormat.Text);

            // act
            var testResult = IcalTimeZoneResolver.TzData.ToUtc(leapSecond, null);

            // assert
            testResult.Should().Be(new IcalDateTime(new IcalDate(2017, 1, 1), new IcalTime(0, 0, 0, true)));
        }

        [Theory]
        [MemberData(nameof(WrongKindOfTimeCalls), DisableDiscoveryEnumeration = true)]
        public void Rules_TimeOfTheWrongKind_ThrowsArgumentException(Action call)
        {
            // act
            var testAction = call;

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void GetTransitions_EndBeforeStart_ThrowsArgumentOutOfRange()
        {
            // arrange
            var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

            // act
            Action testAction = () => rules.GetTransitions(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1));

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void GetUtcOffset_UnspecifiedKind_IsTakenAsUtc()
        {
            // arrange
            var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

            // act
            var testResult = rules.GetUtcOffset(new DateTime(2026, 1, 1));

            // assert
            testResult.Offset.Should().Be(EasternStandardOffset);
        }

        [Fact]
        public void ToLocal_UtcTime_AddsTheOffsetInUse()
        {
            // arrange
            var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

            // act
            var testResult = rules.ToLocal(new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc));

            // assert
            testResult.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0));
            testResult.Kind.Should().Be(DateTimeKind.Unspecified);
        }

        #endregion

        #region DST gaps and overlaps (RFC 5545 §3.3.5)

        [Theory]
        [MemberData(nameof(NewYorkCalendars))]
        public void GetUtcOffsetsForLocal_TimeInAGap_IsEmpty(string name, string calendarText)
        {
            // arrange
            _ = name;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(calendarText));

            // act
            var testResult = rules.GetUtcOffsetsForLocal(NewYorkTimeInGap);

            // assert
            testResult.Should().BeEmpty();
        }

        [Theory]
        [MemberData(nameof(NewYorkCalendars))]
        public void ToUtc_TimeInAGap_IsReadWithTheOffsetBeforeIt(string name, string calendarText)
        {
            // arrange
            _ = name;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(calendarText));

            // act
            var testResult = rules.ToUtc(NewYorkTimeInGap);

            // assert
            testResult.Should().Be(NewYorkTimeInGapInUtc);
        }

        [Theory]
        [MemberData(nameof(NewYorkCalendars))]
        public void ToLocal_InstantOfATimeInAGap_IsTheLocalTimeAfterTheGap(string name, string calendarText)
        {
            // arrange
            _ = name;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(calendarText));

            // act
            var testResult = rules.ToLocal(NewYorkTimeInGapInUtc);

            // assert
            testResult.Should().Be(new DateTime(2026, 3, 8, 3, 30, 0));
        }

        [Theory]
        [MemberData(nameof(NewYorkCalendars))]
        public void GetUtcOffsetsForLocal_TimeInAnOverlap_GivesBothOffsetsInOrder(string name, string calendarText)
        {
            // arrange
            _ = name;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(calendarText));

            // act
            var testResult = rules.GetUtcOffsetsForLocal(NewYorkTimeInOverlap);

            // assert
            testResult.Select(offset => offset.Offset).Should().Equal(EasternDaylightOffset, EasternStandardOffset);
        }

        [Theory]
        [MemberData(nameof(NewYorkCalendars))]
        public void ToUtc_TimeInAnOverlap_TakesItsFirstOccurrence(string name, string calendarText)
        {
            // arrange
            _ = name;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(calendarText));

            // act
            var testResult = rules.ToUtc(NewYorkTimeInOverlap);

            // assert
            testResult.Should().Be(NewYorkTimeInOverlapInUtc);
        }

        [Theory]
        [MemberData(nameof(NewYorkGapAndOverlap))]
        public void ToUtc_TzDataTimeInAGapOrOverlap_FollowsRfc5545(DateTime local, DateTime expected)
        {
            // arrange
            var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

            // act
            var testResult = rules.ToUtc(local);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [InlineData("20260329T013000", "20260329T013000Z")] // In the gap: read with GMT, the offset before it.
        [InlineData("20261025T013000", "20261025T003000Z")] // In the overlap: the first occurrence, in BST.
        [InlineData("20260701T120000", "20260701T110000Z")]
        [InlineData("20260115T120000", "20260115T120000Z")]
        public void ToUtc_LondonTimeWithTzDataVTimeZone_FollowsRfc5545(string local, string expectedUtc)
        {
            // arrange
            var timeZone = IcalTimeZone.FromTzData(TzDataTimezone.Parse(London), Utc(2020), Utc(2030));
            var calendar = ModelTestData.ReadCalendar(WriteCalendar(timeZone));
            var value = IcalDateTime.Parse(local.AsSpan(), London, IcalValueFormat.Text);
            var expected = IcalDateTime.Parse(expectedUtc.AsSpan(), IcalValueFormat.Text);

            // act
            var testResult = IcalTimeZoneResolver.Default.ToUtc(value, calendar);

            // assert
            testResult.Should().Be(expected);
        }

        #endregion

        #region Accuracy

        [Theory]
        [MemberData(nameof(AccuracyZones))]
        public void Resolve_CalendarWithTzDataVTimeZone_MatchesTzData(string timeZoneId)
        {
            // arrange
            var from = Utc(2000);
            var to = Utc(2030);
            var calendar = ModelTestData.ReadCalendar(WriteCalendar(IcalTimeZone.FromTzData(TzDataTimezone.Parse(timeZoneId), from, to)));
            var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(timeZoneId, null);

            // act
            var testResult = IcalTimeZoneResolver.Calendar.Resolve(timeZoneId, calendar);

            // assert
            AssertSameRules(testResult, tzDataRules, from.UtcDateTime, to.UtcDateTime);
        }

        [Fact]
        public void Resolve_CalendarWithRfcExample_MatchesTzData()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
            var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

            // act
            var testResult = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);

            // assert
            AssertSameRules(testResult, tzDataRules, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ToUtc_OutlookEventStart_ConvertsWithTheOutlookVTimeZone()
        {
            // arrange
            var calendar = ReadOutlookCalendar();
            var start = calendar.Events.Single().DtStart.Value.DateTime;

            // act
            var testResult = IcalTimeZoneResolver.Default.ToUtc(start, calendar);

            // assert
            testResult.Should().Be(new IcalDateTime(new IcalDate(2026, 7, 1), new IcalTime(13, 0, 0, true)));
        }

        [Theory]
        [MemberData(nameof(NewYorkGapAndOverlap))]
        public void ToUtc_OutlookTimeInAGapOrOverlap_FollowsRfc5545(DateTime local, DateTime expected)
        {
            // arrange
            var rules = IcalTimeZoneResolver.Default.Resolve(EasternStandardTime, ReadOutlookCalendar());

            // act
            var testResult = rules.ToUtc(local);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void Resolve_OutlookWindowsTimeZone_MatchesTzData()
        {
            // arrange
            var calendar = ReadOutlookCalendar();
            var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

            // act
            var testResult = IcalTimeZoneResolver.Default.Resolve(EasternStandardTime, calendar);

            // assert
            AssertSameRules(testResult, tzDataRules, new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void GetUtcOffset_BeforeTheFirstOnset_IsItsOffsetFrom()
        {
            // arrange: the first onset is the DAYLIGHT of 1967-04-30, from -05:00.
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);

            // act
            var testResult = rules.GetUtcOffset(new DateTime(1960, 7, 1, 0, 0, 0, DateTimeKind.Utc));

            // assert
            testResult.Offset.Should().Be(EasternStandardOffset);
        }

        [Theory]
        [MemberData(nameof(ObservancesInEitherOrder))]
        public void GetTransitions_ObservancesInEitherOrder_FirstIsAtTheEarliestUtcOnset(string name, string[] timeZone)
        {
            // arrange
            _ = name;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(OrderTimeZoneId, ModelTestData.ReadCalendar(ModelTestData.Calendar(timeZone)));

            // act
            var testResult = rules.GetTransitions(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .First();

            // assert
            testResult.InstantUtc.Should().Be(OrderFirstOnsetUtc);
            testResult.OffsetFrom.Offset.Should().Be(TimeSpan.FromHours(2));
        }

        [Theory]
        [MemberData(nameof(ObservancesInEitherOrder))]
        public void GetUtcOffset_BeforeTheFirstOnsetOfObservancesInEitherOrder_IsTheOffsetFromOfTheFirstTransition(string name, string[] timeZone)
        {
            // arrange
            _ = name;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(OrderTimeZoneId, ModelTestData.ReadCalendar(ModelTestData.Calendar(timeZone)));

            // act: half an hour before the first onset.
            var testResult = rules.GetUtcOffset(new DateTime(2025, 12, 31, 23, 0, 0, DateTimeKind.Utc));

            // assert
            testResult.Offset.Should().Be(TimeSpan.FromHours(2));
        }

        [Theory]
        [MemberData(nameof(NewYorkOnsets))]
        public void GetTransitions_DayEitherSideOfAnOnset_GivesTheOnset(string source, DateTime onset, DateTime dayBefore, DateTime dayAfter,
            DateTime halfASecondBefore)
        {
            // arrange
            _ = halfASecondBefore;
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork)));

            // act
            var testResult = rules.GetTransitions(dayBefore, dayAfter).ToList();

            // assert
            testResult.Should().ContainSingle(source).Which.InstantUtc.Should().Be(onset, source);
        }

        [Theory]
        [MemberData(nameof(NewYorkOnsets))]
        public void GetUtcOffset_HalfASecondBeforeAnOnset_IsTheOffsetBeforeIt(string source, DateTime onset, DateTime dayBefore, DateTime dayAfter,
            DateTime halfASecondBefore)
        {
            // arrange
            _ = (onset, dayBefore, dayAfter);
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork)));

            // act
            var testResult = rules.GetUtcOffset(halfASecondBefore);

            // assert
            testResult.Offset.Should().Be(EasternStandardOffset, "half a second before the {0} onset", source);
        }

        [Theory]
        [MemberData(nameof(NewYorkOnsets))]
        public void GetUtcOffset_AtAnOnset_IsTheOffsetAfterIt(string source, DateTime onset, DateTime dayBefore, DateTime dayAfter,
            DateTime halfASecondBefore)
        {
            // arrange
            _ = (dayBefore, dayAfter, halfASecondBefore);
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork)));

            // act
            var testResult = rules.GetUtcOffset(onset);

            // assert
            testResult.Offset.Should().Be(EasternDaylightOffset, "at the {0} onset", source);
        }

        [Fact]
        public void GetUtcOffset_CalendarRulesLookedUpInAnyOrder_MatchTzData()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(IcalTestData.Lines(RfcNewYork, OutlookEastern)));
            var rfcRules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);
            var outlookRules = IcalTimeZoneResolver.Calendar.Resolve(EasternStandardTime, calendar);
            var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);
            var random = new Random(263);

            // act / assert
            for (var index = 0; index < 2000; index++)
            {
                var instant = new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(random.Next(0, 500 * 365 * 24 * 60));
                var expected = tzDataRules.GetUtcOffset(instant);

                rfcRules.GetUtcOffset(instant).Should().Be(expected, "at {0:o}", instant);
                outlookRules.GetUtcOffset(instant).Should().Be(expected, "at {0:o}", instant);
            }
        }

        [Fact]
        public void GetUtcOffset_CalendarRulesSharedAcrossThreads_MatchTzData()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);
            var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);
            var instants = Enumerable.Range(0, 4000)
                .Select(hours => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(hours * 61L)).ToArray();
            var expected = instants.Select(tzDataRules.GetUtcOffset).ToArray();
            var testResult = new IcalUtcOffset[instants.Length];

            // act
            Parallel.For(0, instants.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 },
                index => testResult[index] = rules.GetUtcOffset(instants[index]));

            // assert
            testResult.Should().Equal(expected);
        }

        #endregion

        #region Allocations

        [Fact]
        public void ToUtc_RfcRulesIn2030_IsEasternDaylightTime()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);

            // act
            var testResult = rules.ToUtc(new DateTime(2030, 6, 2, 12, 0, 0));

            // assert
            testResult.Should().Be(new DateTime(2030, 6, 2, 16, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ToUtc_CalendarRulesOnceWarmedUp_AllocatesLittle()
        {
            // arrange
            var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
            var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);
            rules.ToUtc(new DateTime(2030, 6, 1, 12, 0, 0));

            // act
            var testResult = MeasureAllocations(() => rules.ToUtc(new DateTime(2030, 6, 2, 12, 0, 0)));

            // assert
            testResult.Should().BeLessThan(1024);
        }

        #endregion

        #region System

        [Theory]
        [MemberData(nameof(SystemZones))]
        public void Resolve_SystemZone_MatchesTzData(string timeZoneId)
        {
            // arrange
            var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(timeZoneId, null);

            // act
            var testResult = IcalTimeZoneResolver.System.Resolve(SystemName(timeZoneId), null);

            // assert
            testResult.TimeZoneId.Should().Be(SystemName(timeZoneId));
            AssertSameRules(testResult, tzDataRules, Utc(2000).UtcDateTime, Utc(2030).UtcDateTime);
        }

        [Fact]
        public void TryResolve_TzDataWindowsName_ReturnsFalse()
        {
            // arrange
            var calendar = ReadCalendarWithWindowsEventStart();

            // act
            var testResult = IcalTimeZoneResolver.TzData.TryResolve(EasternStandardTime, calendar, out _);

            // assert
            testResult.Should().BeFalse();
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
            testResult.Should().Be(new IcalDateTime(new IcalDate(2026, 7, 1), new IcalTime(13, 0, 0, true)));
        }

        [Fact]
        public void Resolve_ChainWithSystemWindowsName_MatchesTzData()
        {
            // arrange
            var calendar = ReadCalendarWithWindowsEventStart();
            var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData, IcalTimeZoneResolver.System);
            var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

            // act
            var testResult = resolver.Resolve(EasternStandardTime, calendar);

            // assert
            AssertSameRules(testResult, tzDataRules, Utc(2007).UtcDateTime, Utc(2030).UtcDateTime);
        }

        [Fact]
        public void GetUtcOffset_SystemVendorPrefixedName_IsTheNewYorkOffset()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve("/mozilla.org/20050126_1/" + SystemName(NewYork), null);

            // act
            var testResult = rules.GetUtcOffset(January2026Utc);

            // assert
            testResult.Offset.Should().Be(EasternStandardOffset);
        }

        [Theory]
        [InlineData(NotAZone)]
        [InlineData("")]
        [InlineData("/")]
        [InlineData(VendorPrefixOnly)]
        [InlineData("../../../../etc/passwd")]
        [InlineData("America/../America/New_York")]
        [InlineData("C:\\Windows\\win.ini")]
        [InlineData("America/New_York\0")]
        public void TryResolve_SystemUnknownOrUnsafeName_ReturnsFalse(string timeZoneId)
        {
            // act
            var testResult = IcalTimeZoneResolver.System.TryResolve(timeZoneId, null, out var rules);

            // assert
            testResult.Should().BeFalse();
            rules.Should().BeNull();
        }

        [Fact]
        public void TryResolve_ChainWhenSystemCannotResolve_TriesTheNextResolver()
        {
            // arrange
            var calls = new List<string>();
            var next = new RecordingResolver(NextResolver, calls, null);
            var chain = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.System, next);

            // act
            var testResult = chain.TryResolve(NotAZone, null, out _);

            // assert
            testResult.Should().BeFalse();
            calls.Should().Equal(NextResolver);
        }

        [Fact]
        public void GetUtcOffsetsForLocal_SystemTimeInAGap_IsEmpty()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemName(NewYork), null);

            // act
            var testResult = rules.GetUtcOffsetsForLocal(NewYorkTimeInGap);

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetUtcOffsetsForLocal_SystemTimeInAnOverlap_GivesBothOffsetsInOrder()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemName(NewYork), null);

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
            var rules = IcalTimeZoneResolver.System.Resolve(SystemName(NewYork), null);

            // act
            var testResult = rules.ToUtc(local);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public void GetTransitions_SystemNewYorkIn2026_GivesTheKindOfEachButNoName()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemName(NewYork), null);

            // act
            var testResult = rules.GetTransitions(Utc(2026).UtcDateTime, Utc(2027).UtcDateTime).ToList();

            // assert
            testResult.Select(transition => (transition.InstantUtc, transition.Kind)).Should().Equal(
                (new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc), IcalTimeZoneObservanceKind.Daylight),
                (new DateTime(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc), IcalTimeZoneObservanceKind.Standard));
            testResult.Should().OnlyContain(transition => transition.Name == null);
        }

        [Fact]
        public void GetTransitions_SystemPeriodStartingAtATransition_GivesIt()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemName(NewYork), null);
            var instant = new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc);

            // act
            var testResult = rules.GetTransitions(instant, new DateTime(2026, 3, 9, 7, 0, 0, DateTimeKind.Utc)).ToList();

            // assert
            testResult.Select(transition => transition.InstantUtc).Should().Equal(instant);
        }

        [Fact]
        public void GetTransitions_SystemPeriodEndingAtATransition_DoesNotGiveIt()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemName(NewYork), null);

            // act
            var testResult = rules.GetTransitions(new DateTime(2026, 3, 7, 7, 0, 0, DateTimeKind.Utc), new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc))
                .ToList();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void GetTransitions_SystemEmptyPeriodAtATransition_GivesNothing()
        {
            // arrange
            var rules = IcalTimeZoneResolver.System.Resolve(SystemName(NewYork), null);
            var instant = new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc);

            // act
            var testResult = rules.GetTransitions(instant, instant).ToList();

            // assert
            testResult.Should().BeEmpty();
        }

        #endregion

        #region FromTzData

        [Fact]
        public void FromTzData_NewYork_HasItsIdentifier()
        {
            // act
            var testResult = IcalTimeZone.FromTzData(TzDataTimezone.Parse(NewYork), Utc(2020), Utc(2023));

            // assert
            testResult.TimeZoneId.Should().Be(NewYork);
        }

        [Fact]
        public void FromTzData_NewYork2020To2023_BuildsThreeObservances()
        {
            // act
            var testResult = IcalTimeZone.FromTzData(TzDataTimezone.Parse(NewYork), Utc(2020), Utc(2023)).Observances.ToList();

            // assert
            testResult.Select(observance => observance.Kind).Should().Equal(IcalTimeZoneObservanceKind.Standard, IcalTimeZoneObservanceKind.Daylight,
                IcalTimeZoneObservanceKind.Standard);
            testResult.Select(observance => observance.DtStart).Should().Equal(
                new IcalDateTime(new IcalDate(2019, 12, 31), new IcalTime(19, 0, 0)),
                new IcalDateTime(new IcalDate(2020, 3, 8), new IcalTime(2, 0, 0)),
                new IcalDateTime(new IcalDate(2020, 11, 1), new IcalTime(2, 0, 0)));
            testResult.Select(observance => observance.TimeZoneName).Should().StartWith(new[] { "EST", "EDT" });
            testResult[0].OffsetFrom.Should().Be(testResult[0].OffsetTo);
            testResult[1].OffsetFrom.Value.Offset.Should().Be(EasternStandardOffset);
            testResult[1].OffsetTo.Value.Offset.Should().Be(EasternDaylightOffset);
        }

        [Fact]
        public void FromTzData_NewYork2020To2023_GivesTheLaterOnsetsAsRecurrenceDates()
        {
            // act
            var testResult = IcalTimeZone.FromTzData(TzDataTimezone.Parse(NewYork), Utc(2020), Utc(2023)).Observances.ToList();

            // assert
            testResult[1].Properties.Where(property => property.Name == IcalPropertyNames.RecurrenceDates).Select(property => property.Value)
                .Should().Equal("20210314T020000", "20220313T020000");
            testResult[2].Properties.Where(property => property.Name == IcalPropertyNames.RecurrenceDates).Select(property => property.Value)
                .Should().Equal("20211107T020000", "20221106T020000");
        }

        [Fact]
        public void FromTzData_WithoutTransitions_GivesOneObservance()
        {
            // act
            var testResult = IcalTimeZone.FromTzData(TzDataTimezone.Parse(Kolkata), Utc(2000), Utc(2030));

            // assert
            testResult.Observances.Should().ContainSingle().Which.OffsetTo.Value.Offset.Should().Be(new TimeSpan(5, 30, 0));
        }

        [Fact]
        public void Validate_FromTzDataWithoutTransitions_ReturnsNothing()
        {
            // arrange
            var timeZone = IcalTimeZone.FromTzData(TzDataTimezone.Parse(Kolkata), Utc(2000), Utc(2030));

            // act
            var testResult = timeZone.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void Validate_CalendarWithTzDataTimeZone_ReturnsNothing()
        {
            // arrange
            var calendar = new IcalCalendar { ProductId = "-//Test//Test//EN" };
            calendar.Components.Add(IcalTimeZone.FromTzData(TzDataTimezone.Parse(Dublin), Utc(2000), Utc(2030)));

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Should().BeEmpty();
        }

        [Fact]
        public void ReadCalendar_WrittenWithTzDataTimeZone_HasItsIdentifier()
        {
            // arrange
            var text = WriteCalendar(IcalTimeZone.FromTzData(TzDataTimezone.Parse(Dublin), Utc(2000), Utc(2030)));

            // act
            var testResult = ModelTestData.ReadCalendar(text);

            // assert
            testResult.TimeZones.Should().ContainSingle().Which.TimeZoneId.Should().Be(Dublin);
        }

        [Fact]
        public void FromTzData_FractionOfASecond_IsDropped()
        {
            // act
            var testResult = IcalTimeZone.FromTzData(TzDataTimezone.Parse(Kolkata), Utc(2000).AddTicks(1234567), Utc(2001));

            // assert
            testResult.Observances.Should().ContainSingle().Which.DtStart.Should()
                .Be(new IcalDateTime(new IcalDate(2000, 1, 1), new IcalTime(5, 30, 0)));
        }

        [Fact]
        public void FromTzData_EndBeforeStart_ThrowsArgumentOutOfRange()
        {
            // arrange
            var timeZone = TzDataTimezone.Parse(NewYork);

            // act
            Action testAction = () => IcalTimeZone.FromTzData(timeZone, Utc(2030), Utc(2020));

            // assert
            testAction.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void FromTzData_DefaultTimeZone_ThrowsInvalidOperation()
        {
            // act
            Action testAction = () => IcalTimeZone.FromTzData(default, Utc(2020), Utc(2030));

            // assert
            testAction.Should().Throw<InvalidOperationException>();
        }

        #endregion

        /// <summary>
        /// Checks that two sets of rules give the same transitions, offsets and conversions of local time over a period.
        /// </summary>
        private static void AssertSameRules(IcalTimeZoneRules actual, IcalTimeZoneRules expected, DateTime fromUtc, DateTime toUtc)
        {
            var expectedTransitions = expected.GetTransitions(fromUtc, toUtc).Where(transition => transition.OffsetFrom != transition.OffsetTo)
                .Select(transition => (transition.InstantUtc, transition.OffsetFrom, transition.OffsetTo)).ToList();
            var actualTransitions = actual.GetTransitions(fromUtc, toUtc).Where(transition => transition.OffsetFrom != transition.OffsetTo)
                .Select(transition => (transition.InstantUtc, transition.OffsetFrom, transition.OffsetTo)).ToList();
            expectedTransitions.Should().NotBeEmpty();
            actualTransitions.Should().Equal(expectedTransitions);

            foreach (var (instant, offsetFrom, _) in expectedTransitions)
            {
                actual.GetUtcOffset(instant.AddSeconds(-1)).Should().Be(expected.GetUtcOffset(instant.AddSeconds(-1)), "just before {0:o}", instant);
                actual.GetUtcOffset(instant).Should().Be(expected.GetUtcOffset(instant), "at {0:o}", instant);

                // Local times either side of the transition, through its gap or overlap.
                var localTransition = DateTime.SpecifyKind(instant + offsetFrom.Offset, DateTimeKind.Unspecified);
                for (var minutes = -150; minutes <= 150; minutes += 15)
                {
                    var local = localTransition.AddMinutes(minutes);
                    actual.GetUtcOffsetsForLocal(local).Should().Equal(expected.GetUtcOffsetsForLocal(local), "at local {0:o}", local);
                    actual.ToUtc(local).Should().Be(expected.ToUtc(local), "at local {0:o}", local);
                }
            }

            for (var instant = fromUtc.AddHours(12); instant < toUtc; instant = instant.AddDays(5))
            {
                actual.GetUtcOffset(instant).Should().Be(expected.GetUtcOffset(instant), "at {0:o}", instant);
            }
        }

        /// <summary>
        /// Writes a calendar holding a VTIMEZONE, so that it is read back as a reader would find it.
        /// </summary>
        private static string WriteCalendar(IcalTimeZone timeZone)
        {
            var calendar = new IcalCalendar { ProductId = "-//Test//Test//EN" };
            calendar.Components.Add(timeZone);
            return ModelTestData.WriteCalendars(new[] { calendar });
        }

        private static IcalCalendar ReadCalendarWithFakeNewYorkAndLondonEvent()
        {
            return ModelTestData.ReadCalendar(ModelTestData.Calendar(IcalTestData.Lines(FakeNewYork, new[]
            {
                BeginEvent, EventUid, NewYearStamp, "DTSTART;TZID=Europe/London:20260701T090000", EndEvent
            })), UndefinedTimeZones);
        }

        private static IcalCalendar ReadOutlookCalendar()
        {
            return ModelTestData.ReadCalendar(ModelTestData.Calendar(IcalTestData.Lines(OutlookEastern, new[]
            {
                BeginEvent, EventUid, NewYearStamp, "DTSTART;TZID=Eastern Standard Time:20260701T090000", EndEvent
            })));
        }

        private static IcalCalendar ReadCalendarWithWindowsEventStart()
        {
            return ModelTestData.ReadCalendar(ModelTestData.Calendar(new[]
            {
                BeginEvent, EventUid, NewYearStamp, "DTSTART;TZID=Eastern Standard Time:20260701T090000", EndEvent
            }), UndefinedTimeZones);
        }

        /// <summary>
        /// Gets the name by which the system knows a time zone: .NET Framework knows only the Windows names.
        /// </summary>
        private static string SystemName(string tzDataName)
        {
#if NET6_0_OR_GREATER
            return tzDataName;
#else
            switch (tzDataName)
            {
                case NewYork:
                    return EasternStandardTime;
                case London:
                    return "GMT Standard Time";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tzDataName));
            }
#endif
        }

        private static long MeasureAllocations(Action action)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            action();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private static DateTimeOffset Utc(int year)
        {
            return new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        }

        private sealed class RecordingResolver : IIcalTimeZoneResolver
        {
            private readonly string _name;
            private readonly List<string> _calls;
            private readonly IIcalTimeZoneResolver _inner;

            public RecordingResolver(string name, List<string> calls, IIcalTimeZoneResolver inner)
            {
                _name = name;
                _calls = calls;
                _inner = inner;
            }

            public bool TryResolve(string timeZoneId, IcalCalendar calendar, out IcalTimeZoneRules rules)
            {
                _calls.Add(_name);
                rules = null;
                return _inner != null && _inner.TryResolve(timeZoneId, calendar, out rules);
            }
        }
    }
}
