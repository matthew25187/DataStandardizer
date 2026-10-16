using System.Diagnostics.CodeAnalysis;
using DataStandardizer.Chronology;
using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class IcalTimeZoneResolverTests
{
    private const string NewYork = "America/New_York";

    private static readonly IcalOptions UndefinedTimeZones = new() { AllowUndefinedTimeZones = true };

    /// <summary>
    /// The America/New_York VTIMEZONE of RFC 5545 §3.6.5, which covers the rules from 1967 with RRULE, RDATE and UNTIL in UTC.
    /// </summary>
    private static readonly string[] RfcNewYork =
    [
        "BEGIN:VTIMEZONE", "TZID:America/New_York", "LAST-MODIFIED:20050809T050000Z",
        "BEGIN:DAYLIGHT", "DTSTART:19670430T020000", "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=-1SU;UNTIL=19730429T070000Z",
        "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT", "END:DAYLIGHT",
        "BEGIN:STANDARD", "DTSTART:19671029T020000", "RRULE:FREQ=YEARLY;BYMONTH=10;BYDAY=-1SU;UNTIL=20061029T060000Z",
        "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500", "TZNAME:EST", "END:STANDARD",
        "BEGIN:DAYLIGHT", "DTSTART:19740106T020000", "RDATE:19750223T020000", "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT",
        "END:DAYLIGHT",
        "BEGIN:DAYLIGHT", "DTSTART:19760425T020000", "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=-1SU;UNTIL=19860427T070000Z",
        "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT", "END:DAYLIGHT",
        "BEGIN:DAYLIGHT", "DTSTART:19870405T020000", "RRULE:FREQ=YEARLY;BYMONTH=4;BYDAY=1SU;UNTIL=20060402T070000Z",
        "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT", "END:DAYLIGHT",
        "BEGIN:DAYLIGHT", "DTSTART:20070311T020000", "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU",
        "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT", "END:DAYLIGHT",
        "BEGIN:STANDARD", "DTSTART:20071104T020000", "RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU",
        "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500", "TZNAME:EST", "END:STANDARD",
        "END:VTIMEZONE"
    ];

    /// <summary>
    /// The VTIMEZONE Outlook writes for its Windows zone "Eastern Standard Time", with rules from 1601.
    /// </summary>
    private static readonly string[] OutlookEastern =
    [
        "BEGIN:VTIMEZONE", "TZID:Eastern Standard Time",
        "BEGIN:STANDARD", "DTSTART:16011104T020000", "RRULE:FREQ=YEARLY;BYDAY=1SU;BYMONTH=11", "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500",
        "END:STANDARD",
        "BEGIN:DAYLIGHT", "DTSTART:16010311T020000", "RRULE:FREQ=YEARLY;BYDAY=2SU;BYMONTH=3", "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400",
        "END:DAYLIGHT",
        "END:VTIMEZONE"
    ];

    /// <summary>
    /// A VTIMEZONE that gives America/New_York a fixed offset of +03:00, so that a resolver using it can be told from one using the TZ
    /// Database.
    /// </summary>
    private static readonly string[] FakeNewYork =
    [
        "BEGIN:VTIMEZONE", "TZID:America/New_York",
        "BEGIN:STANDARD", "DTSTART:19700101T000000", "TZOFFSETFROM:+0300", "TZOFFSETTO:+0300", "END:STANDARD",
        "END:VTIMEZONE"
    ];

    /// <summary>
    /// A VTIMEZONE whose two observances have an onset at the same instant (#262).
    /// </summary>
    private static readonly string[] TiedObservances =
    [
        "BEGIN:VTIMEZONE", "TZID:Test/Tie",
        "BEGIN:STANDARD", "DTSTART:20260101T000000", "TZOFFSETFROM:+0000", "TZOFFSETTO:+0100", "END:STANDARD",
        "BEGIN:DAYLIGHT", "DTSTART:20260101T000000", "TZOFFSETFROM:+0000", "TZOFFSETTO:+0200", "END:DAYLIGHT",
        "END:VTIMEZONE"
    ];

    public static TheoryData<string> AccuracyZones => new() { NewYork, "Europe/London", "Europe/Dublin" };

    #region Resolver behaviour

    [Fact]
    public void Default_IsCalendarAlone()
    {
        IcalTimeZoneResolver.Default.Should().BeSameAs(IcalTimeZoneResolver.Calendar);
    }

    [Fact]
    public void Default_ResolvesThroughTheVTimeZone_EvenForATzDataName()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));

        var rules = IcalTimeZoneResolver.Default.Resolve(NewYork, calendar);

        rules.TimeZoneId.Should().Be(NewYork);
        rules.GetUtcOffset(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)).Offset.Should().Be(TimeSpan.FromHours(3));
    }

    [Fact]
    public void Default_DoesNotResolveATimeZoneWithoutVTimeZone()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.EventCalendar("DTSTART;TZID=Europe/London:20260701T090000"), UndefinedTimeZones);

        IcalTimeZoneResolver.Default.TryResolve("Europe/London", calendar, out var rules).Should().BeFalse();
        rules.Should().BeNull();
        var act = () => IcalTimeZoneResolver.Default.Resolve("Europe/London", calendar);
        act.Should().Throw<IcalFileException>().WithMessage("*'Europe/London'*");
    }

    [Fact]
    public void Calendar_WithoutACalendar_ResolvesNothing()
    {
        IcalTimeZoneResolver.Calendar.TryResolve(NewYork, null, out var rules).Should().BeFalse();
        rules.Should().BeNull();
    }

    [Fact]
    public void Calendar_ObservancesWithTheSameOnset_OffsetMatchesTransition()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(TiedObservances));
        var rules = IcalTimeZoneResolver.Calendar.Resolve("Test/Tie", calendar);

        var transition = rules.GetTransitions(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Single();

        rules.GetUtcOffset(transition.InstantUtc).Should().Be(transition.OffsetTo);
        rules.GetUtcOffset(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)).Should().Be(transition.OffsetTo);
    }

    [Fact]
    public void Calendar_ComparesTheTimeZoneIdentifierOrdinally()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));

        IcalTimeZoneResolver.Calendar.TryResolve("america/new_york", calendar, out _).Should().BeFalse();
    }

    [Fact]
    public void ChainCalendarThenTzData_NeverOverridesAVTimeZone()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar([.. FakeNewYork, "BEGIN:VEVENT", "UID:event@example.com",
            "DTSTAMP:20260101T000000Z", "DTSTART;TZID=Europe/London:20260701T090000", "END:VEVENT"]), UndefinedTimeZones);
        var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);
        var july = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);

        resolver.Resolve(NewYork, calendar).GetUtcOffset(july).Offset.Should().Be(TimeSpan.FromHours(3));
        resolver.Resolve("Europe/London", calendar).GetUtcOffset(july).Offset.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void ChainTzDataThenCalendar_PrefersTheTzDatabase()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));
        var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.TzData, IcalTimeZoneResolver.Calendar);

        resolver.Resolve(NewYork, calendar).GetUtcOffset(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)).Offset
            .Should().Be(TimeSpan.FromHours(-4));
    }

    [Fact]
    public void Chain_TriesEachResolverInOrder()
    {
        var calls = new List<string>();
        var first = new RecordingResolver("first", calls, null);
        var second = new RecordingResolver("second", calls, IcalTimeZoneResolver.TzData);
        var third = new RecordingResolver("third", calls, IcalTimeZoneResolver.TzData);

        var resolved = IcalTimeZoneResolver.Chain(first, second, third).TryResolve(NewYork, null, out var rules);

        resolved.Should().BeTrue();
        rules.Should().NotBeNull();
        calls.Should().Equal("first", "second");
    }

    [Fact]
    public void Chain_WhenNothingResolves_ReturnsFalse()
    {
        var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

        resolver.TryResolve("Not/A_Zone", null, out var rules).Should().BeFalse();
        rules.Should().BeNull();
    }

    [Fact]
    public void Chain_ChecksItsArguments()
    {
        FluentActions.Invoking(() => IcalTimeZoneResolver.Chain(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => IcalTimeZoneResolver.Chain()).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, null!)).Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Chain_CopiesTheResolvers()
    {
        var resolvers = new[] { IcalTimeZoneResolver.TzData };
        var chain = IcalTimeZoneResolver.Chain(resolvers);
        resolvers[0] = IcalTimeZoneResolver.Calendar;

        chain.TryResolve(NewYork, null, out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("America/New_York")]
    [InlineData("US/Eastern")]
    [InlineData("/mozilla.org/20050126_1/America/New_York")]
    [InlineData("/softwarestudio.org/Olson_20011030_5/America/New_York")]
    [InlineData("/citadel.org/20190914_1/America/New_York")]
    public void TzData_ResolvesTzDatabaseNames_StrippingVendorPrefixes(string timeZoneId)
    {
        var rules = IcalTimeZoneResolver.TzData.Resolve(timeZoneId, null);

        rules.TimeZoneId.Should().Be(timeZoneId);
        rules.GetUtcOffset(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)).Offset.Should().Be(TimeSpan.FromHours(-5));
    }

    [Theory]
    [InlineData("Eastern Standard Time")]
    [InlineData("/")]
    [InlineData("/example.com/")]
    [InlineData("")]
    public void TzData_DoesNotResolveOtherNames(string timeZoneId)
    {
        IcalTimeZoneResolver.TzData.TryResolve(timeZoneId, null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryResolve_RejectsANullIdentifier()
    {
        FluentActions.Invoking(() => IcalTimeZoneResolver.Calendar.TryResolve(null!, null, out _)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => IcalTimeZoneResolver.TzData.TryResolve(null!, null, out _)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => IcalTimeZoneResolver.System.TryResolve(null!, null, out _)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.TzData).TryResolve(null!, null, out _))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Calendar_RejectsAnObservanceWithoutItsRequiredProperties()
    {
        var timeZone = new IcalTimeZone { TimeZoneId = "Broken" };
        timeZone.Components.Add(new IcalTimeZoneObservance(IcalTimeZoneObservanceKind.Standard)
        {
            OffsetFrom = new IcalUtcOffset(TimeSpan.Zero), OffsetTo = new IcalUtcOffset(TimeSpan.Zero)
        });
        var calendar = new IcalCalendar();
        calendar.Components.Add(timeZone);

        var act = () => IcalTimeZoneResolver.Calendar.TryResolve("Broken", calendar, out _);

        act.Should().Throw<IcalFileException>().WithMessage("*DTSTART*");
    }

    [Fact]
    public void Calendar_RejectsAVTimeZoneWithoutObservances()
    {
        var calendar = new IcalCalendar();
        calendar.Components.Add(new IcalTimeZone { TimeZoneId = "Empty" });

        var act = () => IcalTimeZoneResolver.Calendar.TryResolve("Empty", calendar, out _);

        act.Should().Throw<IcalFileException>();
    }

    [Fact]
    public void Calendar_ReadsTheVTimeZoneWhenResolved()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(FakeNewYork));
        var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);

        calendar.TimeZones.Single().Observances.Single().OffsetTo = new IcalUtcOffset(TimeSpan.FromHours(7));

        rules.GetUtcOffset(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)).Offset.Should().Be(TimeSpan.FromHours(3));
    }

    #endregion

    #region Conversions

    [Fact]
    public void ToUtc_ConvertsAZonedTime()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));

        var utc = IcalTimeZoneResolver.Default.ToUtc(IcalDateTime.Parse("20260701T090000", NewYork, IcalValueFormat.Text), calendar);

        utc.Should().Be(IcalDateTime.Parse("20260701T130000Z", IcalValueFormat.Text));
    }

    [Fact]
    public void ToUtc_ReturnsAUtcTimeAsItIs()
    {
        var value = IcalDateTime.Parse("20260701T090000Z", IcalValueFormat.Text);

        IcalTimeZoneResolver.Default.ToUtc(value, null).Should().Be(value);
    }

    [Fact]
    public void ToUtc_RejectsAFloatingTime()
    {
        var act = () => IcalTimeZoneResolver.Default.ToUtc(IcalDateTime.Parse("20260701T090000", IcalValueFormat.Text), null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void ToUtc_WhenNothingResolves_Throws()
    {
        var act = () => IcalTimeZoneResolver.Default.ToUtc(IcalDateTime.Parse("20260701T090000", NewYork, IcalValueFormat.Text), null);

        act.Should().Throw<IcalFileException>();
    }

    [Fact]
    public void ToUtc_ConvertsALeapSecondAsTheSecondAfterTheOneBeforeIt()
    {
        var utc = IcalTimeZoneResolver.TzData.ToUtc(IcalDateTime.Parse("20161231T185960", NewYork, IcalValueFormat.Text), null);

        utc.Should().Be(IcalDateTime.Parse("20170101T000000Z", IcalValueFormat.Text));
    }

    [Fact]
    public void Rules_CheckTheKindsOfTimes()
    {
        var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

        FluentActions.Invoking(() => rules.GetUtcOffsetsForLocal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)))
            .Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => rules.ToUtc(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => rules.GetUtcOffset(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Local))).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => rules.GetTransitions(new DateTime(2026, 1, 2), new DateTime(2026, 1, 1)))
            .Should().Throw<ArgumentOutOfRangeException>();
        rules.GetUtcOffset(new DateTime(2026, 1, 1)).Offset.Should().Be(TimeSpan.FromHours(-5));
    }

    [Fact]
    public void Rules_ToLocal_AddsTheOffsetInUse()
    {
        var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

        var local = rules.ToLocal(new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc));

        local.Should().Be(new DateTime(2026, 7, 1, 9, 0, 0));
        local.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    #endregion

    #region DST gaps and overlaps (RFC 5545 §3.3.5)

    public static TheoryData<string, string> NewYorkCalendars => new()
    {
        { "RFC 5545 example", ModelTestData.Calendar(RfcNewYork) },
        { "FromTzData", WriteCalendar(IcalTimeZone.FromTzData(TzDataTimezone.Parse(NewYork), Utc(2020), Utc(2030))) }
    };

    [Theory]
    [MemberData(nameof(NewYorkCalendars))]
    public void Calendar_ReadsATimeInAGapWithTheOffsetBeforeIt(string name, string calendarText)
    {
        _ = name;
        var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(calendarText));
        var local = new DateTime(2026, 3, 8, 2, 30, 0);

        rules.GetUtcOffsetsForLocal(local).Should().BeEmpty();
        rules.ToUtc(local).Should().Be(new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc));
        rules.ToLocal(rules.ToUtc(local)).Should().Be(new DateTime(2026, 3, 8, 3, 30, 0));
    }

    [Theory]
    [MemberData(nameof(NewYorkCalendars))]
    public void Calendar_TakesTheFirstOccurrenceOfATimeInAnOverlap(string name, string calendarText)
    {
        _ = name;
        var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, ModelTestData.ReadCalendar(calendarText));
        var local = new DateTime(2026, 11, 1, 1, 30, 0);

        rules.GetUtcOffsetsForLocal(local).Select(offset => offset.Offset).Should().Equal(TimeSpan.FromHours(-4), TimeSpan.FromHours(-5));
        rules.ToUtc(local).Should().Be(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void TzData_FollowsRfc5545InGapsAndOverlaps()
    {
        var rules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

        rules.ToUtc(new DateTime(2026, 3, 8, 2, 30, 0)).Should().Be(new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc));
        rules.ToUtc(new DateTime(2026, 11, 1, 1, 30, 0)).Should().Be(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("20260329T013000", "20260329T013000Z")] // In the gap: read with GMT, the offset before it.
    [InlineData("20261025T013000", "20261025T003000Z")] // In the overlap: the first occurrence, in BST.
    [InlineData("20260701T120000", "20260701T110000Z")]
    [InlineData("20260115T120000", "20260115T120000Z")]
    public void Calendar_ResolvesLondonGapsAndOverlaps(string local, string expectedUtc)
    {
        var timeZone = IcalTimeZone.FromTzData(TzDataTimezone.Parse("Europe/London"), Utc(2020), Utc(2030));
        var calendar = ModelTestData.ReadCalendar(WriteCalendar(timeZone));

        IcalTimeZoneResolver.Default.ToUtc(IcalDateTime.Parse(local, "Europe/London", IcalValueFormat.Text), calendar)
            .Should().Be(IcalDateTime.Parse(expectedUtc, IcalValueFormat.Text));
    }

    #endregion

    #region Accuracy

    [Theory]
    [MemberData(nameof(AccuracyZones))]
    public void FromTzData_CalendarResolverMatchesTzData(string timeZoneId)
    {
        var from = Utc(2000);
        var to = Utc(2030);
        var timeZone = IcalTimeZone.FromTzData(TzDataTimezone.Parse(timeZoneId), from, to);
        var calendar = ModelTestData.ReadCalendar(WriteCalendar(timeZone));
        var calendarRules = IcalTimeZoneResolver.Calendar.Resolve(timeZoneId, calendar);
        var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(timeZoneId, null);

        AssertSameRules(calendarRules, tzDataRules, from.UtcDateTime, to.UtcDateTime);
    }

    [Fact]
    public void RfcExample_CalendarResolverMatchesTzData()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
        var calendarRules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);
        var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

        AssertSameRules(calendarRules, tzDataRules, new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void OutlookWindowsTimeZone_Resolves()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar([.. OutlookEastern, "BEGIN:VEVENT", "UID:event@example.com",
            "DTSTAMP:20260101T000000Z", "DTSTART;TZID=Eastern Standard Time:20260701T090000", "END:VEVENT"]));
        var rules = IcalTimeZoneResolver.Default.Resolve("Eastern Standard Time", calendar);
        var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(NewYork, null);

        IcalTimeZoneResolver.Default.ToUtc(calendar.Events.Single().DtStart!.Value.DateTime, calendar)
            .Should().Be(IcalDateTime.Parse("20260701T130000Z", IcalValueFormat.Text));
        rules.ToUtc(new DateTime(2026, 3, 8, 2, 30, 0)).Should().Be(new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc));
        rules.ToUtc(new DateTime(2026, 11, 1, 1, 30, 0)).Should().Be(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc));
        AssertSameRules(rules, tzDataRules, new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Calendar_BeforeTheFirstOnset_UsesItsOffsetFrom()
    {
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(RfcNewYork));
        var rules = IcalTimeZoneResolver.Calendar.Resolve(NewYork, calendar);

        // The first onset is the DAYLIGHT of 1967-04-30, from -05:00.
        rules.GetUtcOffset(new DateTime(1960, 7, 1, 0, 0, 0, DateTimeKind.Utc)).Offset.Should().Be(TimeSpan.FromHours(-5));
    }

    #endregion

    #region System

    public static TheoryData<string> SystemZones => new() { NewYork, "Europe/London" };

    [Theory]
    [MemberData(nameof(SystemZones))]
    public void System_MatchesTzData(string timeZoneId)
    {
        var systemRules = IcalTimeZoneResolver.System.Resolve(timeZoneId, null);
        var tzDataRules = IcalTimeZoneResolver.TzData.Resolve(timeZoneId, null);

        systemRules.TimeZoneId.Should().Be(timeZoneId);
        AssertSameRules(systemRules, tzDataRules, Utc(2000).UtcDateTime, Utc(2030).UtcDateTime);
    }

    [Fact]
    public void System_ResolvesAWindowsName_ThatTzDataCannot()
    {
        var resolver = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData, IcalTimeZoneResolver.System);
        var calendar = ModelTestData.ReadCalendar(ModelTestData.Calendar(["BEGIN:VEVENT", "UID:event@example.com", "DTSTAMP:20260101T000000Z",
            "DTSTART;TZID=Eastern Standard Time:20260701T090000", "END:VEVENT"]), UndefinedTimeZones);

        IcalTimeZoneResolver.TzData.TryResolve("Eastern Standard Time", calendar, out _).Should().BeFalse();
        resolver.ToUtc(calendar.Events.Single().DtStart!.Value.DateTime, calendar)
            .Should().Be(IcalDateTime.Parse("20260701T130000Z", IcalValueFormat.Text));
        AssertSameRules(resolver.Resolve("Eastern Standard Time", calendar), IcalTimeZoneResolver.TzData.Resolve(NewYork, null),
            Utc(2007).UtcDateTime, Utc(2030).UtcDateTime);
    }

    [Fact]
    public void System_StripsVendorPrefixes()
    {
        var rules = IcalTimeZoneResolver.System.Resolve("/mozilla.org/20050126_1/America/New_York", null);

        rules.GetUtcOffset(new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc)).Offset.Should().Be(TimeSpan.FromHours(-5));
    }

    [Theory]
    [InlineData("Not/A_Zone")]
    [InlineData("")]
    [InlineData("/")]
    [InlineData("/example.com/")]
    [InlineData("../../../../etc/passwd")]
    [InlineData("America/../America/New_York")]
    [InlineData("C:\\Windows\\win.ini")]
    [InlineData("America/New_York\0")]
    public void System_DoesNotResolveUnknownOrUnsafeNames(string timeZoneId)
    {
        IcalTimeZoneResolver.System.TryResolve(timeZoneId, null, out var rules).Should().BeFalse();
        rules.Should().BeNull();
    }

    [Fact]
    public void System_WhenItCannotResolve_FallsThroughToTheNextResolver()
    {
        var calls = new List<string>();
        var next = new RecordingResolver("next", calls, null);

        var resolved = IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.System, next).TryResolve("Not/A_Zone", null, out _);

        resolved.Should().BeFalse();
        calls.Should().Equal("next");
    }

    [Fact]
    public void System_FollowsRfc5545InGapsAndOverlaps()
    {
        var rules = IcalTimeZoneResolver.System.Resolve(NewYork, null);

        rules.GetUtcOffsetsForLocal(new DateTime(2026, 3, 8, 2, 30, 0)).Should().BeEmpty();
        rules.ToUtc(new DateTime(2026, 3, 8, 2, 30, 0)).Should().Be(new DateTime(2026, 3, 8, 7, 30, 0, DateTimeKind.Utc));
        rules.GetUtcOffsetsForLocal(new DateTime(2026, 11, 1, 1, 30, 0)).Select(offset => offset.Offset)
            .Should().Equal(TimeSpan.FromHours(-4), TimeSpan.FromHours(-5));
        rules.ToUtc(new DateTime(2026, 11, 1, 1, 30, 0)).Should().Be(new DateTime(2026, 11, 1, 5, 30, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void System_GivesTheKindOfEachTransitionButNoName()
    {
        var rules = IcalTimeZoneResolver.System.Resolve(NewYork, null);

        var transitions = rules.GetTransitions(Utc(2026).UtcDateTime, Utc(2027).UtcDateTime).ToList();

        transitions.Select(transition => (transition.InstantUtc, transition.Kind)).Should().Equal(
            (new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc), IcalTimeZoneObservanceKind.Daylight),
            (new DateTime(2026, 11, 1, 6, 0, 0, DateTimeKind.Utc), IcalTimeZoneObservanceKind.Standard));
        transitions.Should().OnlyContain(transition => transition.Name == null);
    }

    [Fact]
    public void System_GivesATransitionAtTheStartOfThePeriod_ButNotAtItsEnd()
    {
        var rules = IcalTimeZoneResolver.System.Resolve(NewYork, null);
        var instant = new DateTime(2026, 3, 8, 7, 0, 0, DateTimeKind.Utc);

        rules.GetTransitions(instant, instant.AddDays(1)).Select(transition => transition.InstantUtc).Should().Equal(instant);
        rules.GetTransitions(instant.AddDays(-1), instant).Should().BeEmpty();
        rules.GetTransitions(instant, instant).Should().BeEmpty();
    }

    #endregion

    #region FromTzData

    [Fact]
    public void FromTzData_BuildsObservancesWithRecurrenceDates()
    {
        var timeZone = IcalTimeZone.FromTzData(TzDataTimezone.Parse(NewYork), Utc(2020), Utc(2023));

        timeZone.TimeZoneId.Should().Be(NewYork);
        var observances = timeZone.Observances.ToList();
        observances.Should().HaveCount(3);

        observances[0].Kind.Should().Be(IcalTimeZoneObservanceKind.Standard);
        observances[0].DtStart.Should().Be(IcalDateTime.Parse("20191231T190000", IcalValueFormat.Text));
        observances[0].OffsetFrom.Should().Be(observances[0].OffsetTo);
        observances[0].TimeZoneName.Should().Be("EST");

        observances[1].Kind.Should().Be(IcalTimeZoneObservanceKind.Daylight);
        observances[1].DtStart.Should().Be(IcalDateTime.Parse("20200308T020000", IcalValueFormat.Text));
        observances[1].OffsetFrom!.Value.Offset.Should().Be(TimeSpan.FromHours(-5));
        observances[1].OffsetTo!.Value.Offset.Should().Be(TimeSpan.FromHours(-4));
        observances[1].TimeZoneName.Should().Be("EDT");
        observances[1].GetProperties(IcalPropertyNames.RecurrenceDates).Select(property => property.Value)
            .Should().Equal("20210314T020000", "20220313T020000");

        observances[2].Kind.Should().Be(IcalTimeZoneObservanceKind.Standard);
        observances[2].DtStart.Should().Be(IcalDateTime.Parse("20201101T020000", IcalValueFormat.Text));
        observances[2].GetProperties(IcalPropertyNames.RecurrenceDates).Select(property => property.Value)
            .Should().Equal("20211107T020000", "20221106T020000");
    }

    [Fact]
    public void FromTzData_WithoutTransitions_GivesOneObservance()
    {
        var timeZone = IcalTimeZone.FromTzData(TzDataTimezone.Parse("Asia/Kolkata"), Utc(2000), Utc(2030));

        var observance = timeZone.Observances.Should().ContainSingle().Subject;
        observance.OffsetTo!.Value.Offset.Should().Be(new TimeSpan(5, 30, 0));
        timeZone.Validate().Should().BeEmpty();
    }

    [Fact]
    public void FromTzData_IsWrittenByIcalWriter()
    {
        var calendar = new IcalCalendar { ProductId = "-//Test//Test//EN" };
        calendar.Components.Add(IcalTimeZone.FromTzData(TzDataTimezone.Parse("Europe/Dublin"), Utc(2000), Utc(2030)));

        calendar.Validate().Should().BeEmpty();
        var text = ModelTestData.WriteCalendars([calendar]);

        ModelTestData.ReadCalendar(text).TimeZones.Single().TimeZoneId.Should().Be("Europe/Dublin");
    }

    [Fact]
    public void FromTzData_DropsAFractionOfASecond()
    {
        var timeZone = IcalTimeZone.FromTzData(TzDataTimezone.Parse("Asia/Kolkata"), Utc(2000).AddTicks(1234567), Utc(2001));

        timeZone.Observances.Single().DtStart.Should().Be(IcalDateTime.Parse("20000101T053000", IcalValueFormat.Text));
    }

    [Fact]
    public void FromTzData_ChecksItsArguments()
    {
        FluentActions.Invoking(() => IcalTimeZone.FromTzData(TzDataTimezone.Parse(NewYork), Utc(2030), Utc(2020)))
            .Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => IcalTimeZone.FromTzData(default, Utc(2020), Utc(2030))).Should().Throw<InvalidOperationException>();
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
        return ModelTestData.WriteCalendars([calendar]);
    }

    private static DateTimeOffset Utc(int year)
    {
        return new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    private sealed class RecordingResolver(string name, List<string> calls, IIcalTimeZoneResolver? inner) : IIcalTimeZoneResolver
    {
        public bool TryResolve(string timeZoneId, IcalCalendar? calendar, [NotNullWhen(true)] out IcalTimeZoneRules? rules)
        {
            calls.Add(name);
            rules = null;
            return inner != null && inner.TryResolve(timeZoneId, calendar, out rules);
        }
    }
}
