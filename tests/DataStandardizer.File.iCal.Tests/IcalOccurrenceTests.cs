using FluentAssertions;

namespace DataStandardizer.File.iCal.Tests;

public class IcalOccurrenceTests
{
    private const string NewYork = "America/New_York";

    private static readonly IcalOptions UndefinedTimeZones = new() { AllowUndefinedTimeZones = true };

    private static readonly IIcalTimeZoneResolver CalendarThenTzData =
        IcalTimeZoneResolver.Chain(IcalTimeZoneResolver.Calendar, IcalTimeZoneResolver.TzData);

    /// <summary>
    /// The America/New_York VTIMEZONE of RFC 5545 §3.6.5, cut down to the rules in use from 2007.
    /// </summary>
    private static readonly string[] NewYorkTimeZone =
    [
        "BEGIN:VTIMEZONE", "TZID:America/New_York",
        "BEGIN:DAYLIGHT", "DTSTART:20070311T020000", "RRULE:FREQ=YEARLY;BYMONTH=3;BYDAY=2SU",
        "TZOFFSETFROM:-0500", "TZOFFSETTO:-0400", "TZNAME:EDT", "END:DAYLIGHT",
        "BEGIN:STANDARD", "DTSTART:20071104T020000", "RRULE:FREQ=YEARLY;BYMONTH=11;BYDAY=1SU",
        "TZOFFSETFROM:-0400", "TZOFFSETTO:-0500", "TZNAME:EST", "END:STANDARD",
        "END:VTIMEZONE"
    ];

    #region Time zones

    public static TheoryData<string, string, string, int, int> DaylightSavingChanges => new()
    {
        // The zone, the day before a change, the day after it, and the UTC hour of 09:00 local time before and after.
        { NewYork, "20260307", "20260309", 14, 13 },
        { NewYork, "20261031", "20261102", 13, 14 },
        { "Europe/London", "20260328", "20260330", 9, 8 },
        { "Europe/London", "20261024", "20261026", 8, 9 },
        // Ireland observes a negative daylight saving time: its standard time is in summer, and winter is the "daylight" offset of +00:00.
        { "Europe/Dublin", "20260328", "20260330", 9, 8 },
        { "Europe/Dublin", "20261024", "20261026", 8, 9 }
    };

    [Theory]
    [MemberData(nameof(DaylightSavingChanges))]
    public void GetOccurrences_DailyAcrossADaylightSavingChange_KeepsTheLocalTimeAndMovesTheUtcTime(string zone, string before,
        string after, int hourBefore, int hourAfter)
    {
        var calendar = Read(Event($"DTSTART;TZID={zone}:{before}T090000", "DURATION:PT1H", "RRULE:FREQ=DAILY;COUNT=3"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

        occurrences.Select(occurrence => occurrence.Start.DateTime).Should().OnlyContain(start =>
            start.Time == new IcalTime(9, 0, 0) && start.TimeZoneId == zone);
        occurrences[0].StartUtc!.Value.Time.Hour.Should().Be(hourBefore);
        occurrences[2].StartUtc!.Value.Time.Hour.Should().Be(hourAfter);
        occurrences[2].StartUtc!.Value.Date.ToString().Should().Be(after);
        occurrences.Select(occurrence => occurrence.EndUtc!.Value.Time.Hour - occurrence.StartUtc!.Value.Time.Hour).Should().OnlyContain(
            hours => hours == 1);
    }

    [Fact]
    public void GetOccurrences_DefaultResolver_ReadsTheCalendarsVTimeZone()
    {
        var calendar = Read([.. NewYorkTimeZone, .. Event("DTSTART;TZID=America/New_York:20260307T090000", "RRULE:FREQ=DAILY;COUNT=3")]);

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.StartUtc.ToString());

        starts.Should().Equal("20260307T140000Z", "20260308T130000Z", "20260309T130000Z");
    }

    [Fact]
    public void GetOccurrences_DefaultResolver_WithoutAVTimeZone_Throws()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260307T090000"));

        var act = () => calendar.Events.Single().GetOccurrences(calendar, null, null);

        act.Should().Throw<IcalFileException>().WithMessage("*'America/New_York'*");
    }

    [Fact]
    public void GetOccurrences_TimeInADaylightSavingGap_IsReadWithTheOffsetBeforeTheGap()
    {
        // ref. RFC 5545§3.3.5: 02:30 does not exist on 2026-03-08 in New York, so it is read as 02:30 EST, which is 03:30 EDT.
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260307T023000", "RRULE:FREQ=DAILY;COUNT=3"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

        occurrences.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20260307T023000", "20260308T023000", "20260309T023000");
        occurrences.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal(
            "20260307T073000Z", "20260308T073000Z", "20260309T063000Z");
    }

    [Fact]
    public void GetOccurrences_TimeInADaylightSavingOverlap_TakesItsFirstOccurrence()
    {
        // ref. RFC 5545§3.3.5: 01:30 happens twice on 2026-11-01 in New York; the first, in EDT, is 05:30 UTC.
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20261031T013000", "RRULE:FREQ=DAILY;COUNT=3"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData)
            .Select(occurrence => occurrence.StartUtc.ToString());

        starts.Should().Equal("20261031T053000Z", "20261101T053000Z", "20261102T063000Z");
    }

    [Fact]
    public void GetOccurrences_FloatingTime_StaysFloating()
    {
        var calendar = Read(Event("DTSTART:20260307T090000", "DTEND:20260307T100000", "RRULE:FREQ=DAILY;COUNT=2"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null).ToList();

        occurrences.Select(occurrence => occurrence.Start.DateTime.Kind).Should().OnlyContain(kind => kind == IcalDateTimeKind.Floating);
        occurrences.Select(occurrence => occurrence.End!.Value.ToString()).Should().Equal("20260307T100000", "20260308T100000");
        occurrences.Should().OnlyContain(occurrence => occurrence.StartUtc == null && occurrence.EndUtc == null);
    }

    [Fact]
    public void GetOccurrences_UtcTime_StaysInUtc()
    {
        var calendar = Read(Event("DTSTART:20260307T090000Z", "DURATION:PT30M", "RRULE:FREQ=WEEKLY;COUNT=2"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null).ToList();

        occurrences.Select(occurrence => occurrence.StartUtc.ToString()).Should().Equal("20260307T090000Z", "20260314T090000Z");
        occurrences.Select(occurrence => occurrence.End!.Value.ToString()).Should().Equal("20260307T093000Z", "20260314T093000Z");
    }

    [Fact]
    public void GetOccurrences_UntilInUtcWithAZonedStart_ComparesTheInstant()
    {
        // 09:00 EDT on 2026-03-10 is 13:00 UTC, after the UNTIL, so the last instance is on the 9th.
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260307T090000", "RRULE:FREQ=DAILY;UNTIL=20260310T125959Z"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260307T090000", "20260308T090000", "20260309T090000");
    }

    #endregion

    #region Durations

    [Fact]
    public void GetOccurrences_NominalAndExactDurations_DifferAcrossADaylightSavingChange()
    {
        var nominal = Read(Event("DTSTART;TZID=America/New_York:20260307T120000", "DURATION:P1D"));
        var exact = Read(Event("DTSTART;TZID=America/New_York:20260307T120000", "DURATION:PT24H"));

        var nominalOccurrence = nominal.Events.Single().GetOccurrences(nominal, null, null, CalendarThenTzData).Single();
        var exactOccurrence = exact.Events.Single().GetOccurrences(exact, null, null, CalendarThenTzData).Single();

        // A nominal day keeps 12:00 local time, so it is 23 hours long; 24 exact hours end at 13:00 EDT.
        nominalOccurrence.End.ToString().Should().Be("20260308T120000");
        nominalOccurrence.EndUtc.ToString().Should().Be("20260308T160000Z");
        exactOccurrence.End.ToString().Should().Be("20260308T130000");
        exactOccurrence.EndUtc.ToString().Should().Be("20260308T170000Z");
        exactOccurrence.End!.Value.DateTime.TimeZoneId.Should().Be(NewYork);
    }

    [Fact]
    public void GetOccurrences_NominalDurationWithAnExactPart_AddsTheExactPartAfterTheDays()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260307T120000", "DURATION:P1DT2H"));

        var occurrence = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).Single();

        occurrence.End.ToString().Should().Be("20260308T140000");
        occurrence.EndUtc.ToString().Should().Be("20260308T180000Z");
    }

    [Fact]
    public void GetOccurrences_DtEnd_GivesEveryInstanceTheSameExactDuration()
    {
        // ref. RFC 5545§3.8.5.3: DTEND gives the first instance 23 exact hours, across the change, and so every instance.
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260307T120000", "DTEND;TZID=America/New_York:20260308T120000",
            "RRULE:FREQ=DAILY;COUNT=2"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

        occurrences.Select(occurrence => occurrence.End.ToString()).Should().Equal("20260308T120000", "20260309T110000");
    }

    [Fact]
    public void GetOccurrences_DtEndInAnotherTimeZone_EndsInThatTimeZone()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260601T090000", "DTEND;TZID=Europe/London:20260601T160000",
            "RRULE:FREQ=WEEKLY;COUNT=2"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

        occurrences.Select(occurrence => occurrence.End!.Value.DateTime.TimeZoneId).Should().OnlyContain(zone => zone == "Europe/London");
        occurrences.Select(occurrence => occurrence.End.ToString()).Should().Equal("20260601T160000", "20260608T160000");
    }

    [Fact]
    public void GetOccurrences_EventWithNeitherDtEndNorDuration_EndsAtItsStart()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260307T090000", "RRULE:FREQ=DAILY;COUNT=2"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

        occurrences.Should().OnlyContain(occurrence => occurrence.End == occurrence.Start && occurrence.EndUtc == occurrence.StartUtc);
    }

    #endregion

    #region All-day

    [Fact]
    public void GetOccurrences_AllDayEvent_StaysADate()
    {
        var calendar = Read(Event("DTSTART;VALUE=DATE:20260301", "RRULE:FREQ=WEEKLY;COUNT=3"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null).ToList();

        occurrences.Should().OnlyContain(occurrence => occurrence.Start.IsDate && occurrence.End!.Value.IsDate &&
                                                       occurrence.StartUtc == null && occurrence.EndUtc == null);
        occurrences.Select(occurrence => occurrence.ToString()).Should().Equal(
            "20260301/20260302", "20260308/20260309", "20260315/20260316");
    }

    [Fact]
    public void GetOccurrences_AllDayEventWithDtEnd_KeepsItsNumberOfDays()
    {
        var calendar = Read(Event("DTSTART;VALUE=DATE:20260301", "DTEND;VALUE=DATE:20260304", "RRULE:FREQ=MONTHLY;COUNT=2"));

        var ends = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.End.ToString());

        ends.Should().Equal("20260304", "20260404");
    }

    [Fact]
    public void GetOccurrences_AllDayEventWithAWindowInUtc_ComparesTheDate()
    {
        var calendar = Read(Event("DTSTART;VALUE=DATE:20260301", "RRULE:FREQ=DAILY"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, Utc("20260305T000000Z"), Utc("20260308T000000Z"))
            .Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260305", "20260306", "20260307");
    }

    #endregion

    #region Recurrence set

    [Fact]
    public void GetOccurrences_DtStartThatTheRuleDoesNotGive_IsTheFirstInstance()
    {
        // ref. RFC 5545§3.3.10: DTSTART on a Saturday is an instance, although the rule gives only Mondays, and counts towards COUNT.
        var calendar = Read(Event("DTSTART:20260307T090000Z", "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=2"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260307T090000Z", "20260309T090000Z");
    }

    [Fact]
    public void GetOccurrences_RuleThatGivesDtStart_CountsItOnce()
    {
        var calendar = Read(Event("DTSTART:20260309T090000Z", "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=2"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260309T090000Z", "20260316T090000Z");
    }

    [Fact]
    public void GetOccurrences_CountOfOneThatDoesNotGiveDtStart_GivesOnlyDtStart()
    {
        var calendar = Read(Event("DTSTART;VALUE=DATE:20260307", "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=1"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260307");
    }

    [Fact]
    public void GetOccurrences_AllDayDtStartThatTheRuleDoesNotGive_CountsTowardsCount()
    {
        var calendar = Read(Event("DTSTART;VALUE=DATE:20260307", "RRULE:FREQ=WEEKLY;BYDAY=MO;COUNT=3"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260307", "20260309", "20260316");
    }

    [Fact]
    public void GetOccurrences_NonRecurringEvent_GivesOneInstance()
    {
        var calendar = Read(Event("DTSTART:20260307T090000Z", "DTEND:20260307T100000Z"));

        var occurrence = calendar.Events.Single().GetOccurrences(calendar, null, null).Single();

        occurrence.ToString().Should().Be("20260307T090000Z/20260307T100000Z");
        occurrence.RecurrenceId.Should().Be(occurrence.Start);
        occurrence.IsOverride.Should().BeFalse();
        occurrence.Component.Should().BeSameAs(calendar.Events.Single());
    }

    [Fact]
    public void GetOccurrences_RDatePeriod_GivesTheInstanceItsOwnEnd()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "DURATION:PT1H", "RRULE:FREQ=WEEKLY;COUNT=2",
            "RDATE;VALUE=PERIOD:20260304T140000Z/20260304T170000Z,20260305T080000Z/PT30M"));

        var occurrences = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.ToString());

        occurrences.Should().Equal("20260302T090000Z/20260302T100000Z", "20260304T140000Z/20260304T170000Z",
            "20260305T080000Z/20260305T083000Z", "20260309T090000Z/20260309T100000Z");
    }

    [Fact]
    public void GetOccurrences_RDateDatesAndDateTimes_AreAddedInOrder()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260302T090000", "RDATE;TZID=America/New_York:20260310T090000,20260303T090000",
            "RDATE:20260305T120000Z"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData)
            .Select(occurrence => occurrence.StartUtc.ToString());

        starts.Should().Equal("20260302T140000Z", "20260303T140000Z", "20260305T120000Z", "20260310T130000Z");
    }

    [Fact]
    public void GetOccurrences_InstanceGivenMoreThanOnce_AppearsOnce()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=3", "RDATE:20260303T090000Z,20260302T090000Z"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260303T090000Z", "20260304T090000Z");
    }

    [Fact]
    public void GetOccurrences_ExDate_RemovesTheDtStartInstance()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260302T090000", "RRULE:FREQ=DAILY;COUNT=3",
            "EXDATE;TZID=America/New_York:20260302T090000"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260303T090000", "20260304T090000");
    }

    [Fact]
    public void GetOccurrences_ExDateInUtc_RemovesTheZonedInstanceAtThatInstant()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20260302T090000", "RRULE:FREQ=DAILY;COUNT=3", "EXDATE:20260303T140000Z"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null, CalendarThenTzData).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000", "20260304T090000");
    }

    [Fact]
    public void GetOccurrences_ExDateAsADate_RemovesEveryInstanceOnThatDate()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "RRULE:FREQ=HOURLY;INTERVAL=8;COUNT=6", "EXDATE;VALUE=DATE:20260303"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260302T170000Z", "20260304T010000Z");
    }

    [Fact]
    public void GetOccurrences_ExDate_RemovesAnRDateInstance()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "RDATE:20260303T090000Z,20260304T090000Z", "EXDATE:20260303T090000Z"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260304T090000Z");
    }

    #endregion

    #region Overrides

    [Fact]
    public void GetOccurrences_Override_ReplacesTheInstanceItNames()
    {
        var calendar = Read([
            .. Event("DTSTART;TZID=America/New_York:20260302T090000", "DURATION:PT1H", "RRULE:FREQ=DAILY;COUNT=3", "SUMMARY:Stand-up"),
            .. Override("RECURRENCE-ID;TZID=America/New_York:20260303T090000", "DTSTART;TZID=America/New_York:20260303T113000",
                "DURATION:PT30M", "SUMMARY:Moved")
        ]);
        var master = calendar.Events.First();

        var occurrences = master.GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

        occurrences.Select(occurrence => occurrence.ToString()).Should().Equal(
            "20260302T090000/20260302T100000", "20260303T113000/20260303T120000", "20260304T090000/20260304T100000");
        occurrences.Select(occurrence => occurrence.Component.Summary).Should().Equal("Stand-up", "Moved", "Stand-up");
        occurrences.Select(occurrence => occurrence.IsOverride).Should().Equal(false, true, false);
        occurrences[1].RecurrenceId.ToString().Should().Be("20260303T090000");
    }

    [Fact]
    public void GetOccurrences_OverrideThatMovesAnInstanceLater_IsGivenInOrderOfStart()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=4"),
            .. Override("RECURRENCE-ID:20260303T090000Z", "DTSTART:20260305T120000Z")
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260304T090000Z", "20260305T090000Z", "20260305T120000Z");
    }

    [Fact]
    public void GetOccurrences_OverrideThatMovesAnInstanceIntoTheWindow_IsGiven()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY"),
            .. Override("RECURRENCE-ID:20260302T090000Z", "DTSTART:20270101T080000Z")
        ]);

        var occurrences = calendar.Events.First().GetOccurrences(calendar, Utc("20261231T120000Z"), Utc("20270102T000000Z")).ToList();

        occurrences.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20270101T080000Z", "20270101T090000Z");
        occurrences.Select(occurrence => occurrence.IsOverride).Should().Equal(true, false);
    }

    [Fact]
    public void GetOccurrences_OverrideMatchedByItsInstant_ReplacesAZonedInstance()
    {
        // The RECURRENCE-ID is in UTC, the instance in New York time; they name the same instant.
        var calendar = Read([
            .. Event("DTSTART;TZID=America/New_York:20260302T090000", "RRULE:FREQ=DAILY;COUNT=2"),
            .. Override("RECURRENCE-ID:20260303T140000Z", "DTSTART;TZID=America/New_York:20260303T100000")
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null, CalendarThenTzData).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000", "20260303T100000");
    }

    [Fact]
    public void GetOccurrences_ThisAndFutureOverride_ShiftsEveryLaterInstance()
    {
        // The override moves 09:00 to 10:30 and shortens the meeting, from the 4th onwards; the shift is in local time, so it holds across
        // the change to daylight saving time on the 8th.
        var calendar = Read([
            .. Event("DTSTART;TZID=America/New_York:20260302T090000", "DURATION:PT1H", "RRULE:FREQ=DAILY;COUNT=8", "SUMMARY:Before"),
            .. Override("RECURRENCE-ID;RANGE=THISANDFUTURE;TZID=America/New_York:20260304T090000",
                "DTSTART;TZID=America/New_York:20260304T103000", "DURATION:PT30M", "SUMMARY:After")
        ]);

        var occurrences = calendar.Events.First().GetOccurrences(calendar, null, null, CalendarThenTzData).ToList();

        occurrences.Select(occurrence => occurrence.ToString()).Should().Equal(
            "20260302T090000/20260302T100000", "20260303T090000/20260303T100000", "20260304T103000/20260304T110000",
            "20260305T103000/20260305T110000", "20260306T103000/20260306T110000", "20260307T103000/20260307T110000",
            "20260308T103000/20260308T110000", "20260309T103000/20260309T110000");
        occurrences.Select(occurrence => occurrence.Component.Summary).Should().Equal(
            "Before", "Before", "After", "After", "After", "After", "After", "After");
        occurrences[7].StartUtc.ToString().Should().Be("20260309T143000Z");
        occurrences[7].RecurrenceId.ToString().Should().Be("20260309T090000");
    }

    [Fact]
    public void GetOccurrences_LaterThisAndFutureOverride_TakesOverFromAnEarlierOne()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=6"),
            .. Override("RECURRENCE-ID;RANGE=THISANDFUTURE:20260303T090000Z", "DTSTART:20260303T100000Z"),
            .. Override("RECURRENCE-ID;RANGE=THISANDFUTURE:20260305T090000Z", "DTSTART:20260305T080000Z")
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260303T100000Z", "20260304T100000Z", "20260305T080000Z", "20260306T080000Z",
            "20260307T080000Z");
    }

    [Fact]
    public void GetOccurrences_SingleOverrideAfterAThisAndFutureOverride_ReplacesOnlyItsInstance()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=4"),
            .. Override("RECURRENCE-ID;RANGE=THISANDFUTURE:20260303T090000Z", "DTSTART:20260303T100000Z"),
            .. Override("RECURRENCE-ID:20260304T090000Z", "DTSTART:20260304T150000Z")
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260303T100000Z", "20260304T150000Z", "20260305T100000Z");
    }

    [Fact]
    public void GetOccurrences_ThisAndFutureOverrideOfAnAllDayEvent_ShiftsByWholeDays()
    {
        var calendar = Read([
            .. Event("DTSTART;VALUE=DATE:20260302", "RRULE:FREQ=WEEKLY;COUNT=3"),
            .. Override("RECURRENCE-ID;RANGE=THISANDFUTURE;VALUE=DATE:20260309", "DTSTART;VALUE=DATE:20260310")
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302", "20260310", "20260317");
    }

    [Fact]
    public void GetOccurrences_TwoOverridesOfTheSameInstance_TheHigherSequenceWins()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=2"),
            .. Override("RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T120000Z", "SEQUENCE:2"),
            .. Override("RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T110000Z", "SEQUENCE:1")
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260303T120000Z");
    }

    [Fact]
    public void GetOccurrences_OverrideOfAnExcludedInstance_IsIgnored()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=3", "EXDATE:20260303T090000Z"),
            .. Override("RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T120000Z")
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260304T090000Z");
    }

    [Fact]
    public void GetOccurrences_ComponentsOfAnotherUidOrType_AreNotOverrides()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=2"),
            "BEGIN:VEVENT", "UID:other@example.com", "DTSTAMP:20260101T000000Z", "RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T120000Z",
            "END:VEVENT",
            "BEGIN:VTODO", "UID:event@example.com", "DTSTAMP:20260101T000000Z", "RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T120000Z",
            "END:VTODO"
        ]);

        var starts = calendar.Events.First().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260303T090000Z");
    }

    [Fact]
    public void GetOccurrences_WithoutACalendar_IgnoresOverrides()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=2"),
            .. Override("RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T120000Z")
        ]);

        var starts = calendar.Events.First().GetOccurrences(null, null, null).Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260303T090000Z");
    }

    [Fact]
    public void GetOccurrences_OfAnOverride_GivesOnlyItsOwnInstance()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=3"),
            .. Override("RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T120000Z", "DURATION:PT15M")
        ]);
        var overriding = calendar.Events.Last();

        var occurrence = overriding.GetOccurrences(calendar, null, null).Single();

        occurrence.ToString().Should().Be("20260303T120000Z/20260303T121500Z");
        occurrence.RecurrenceId.ToString().Should().Be("20260303T090000Z");
        occurrence.Component.Should().BeSameAs(overriding);
        occurrence.IsOverride.Should().BeFalse();
    }

    #endregion

    #region Window and bounds

    [Fact]
    public void GetOccurrences_Window_IncludesItsStartAndExcludesItsEnd()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, Utc("20260304T090000Z"), Utc("20260306T090000Z"))
            .Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260304T090000Z", "20260305T090000Z");
    }

    [Fact]
    public void GetOccurrences_ZonedWindow_IsResolvedToUtc()
    {
        var calendar = Read(Event("DTSTART:20260302T130000Z", "RRULE:FREQ=HOURLY;COUNT=4"));
        var from = new IcalDateTime(new IcalDate(2026, 3, 2), new IcalTime(9, 0, 0), NewYork);

        var starts = calendar.Events.Single().GetOccurrences(calendar, from, null, CalendarThenTzData)
            .Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T140000Z", "20260302T150000Z", "20260302T160000Z");
    }

    [Fact]
    public void GetOccurrences_WindowFarFromDtStart_GivesTheInstancesInIt()
    {
        var calendar = Read(Event("DTSTART;TZID=America/New_York:20000103T090000", "RRULE:FREQ=WEEKLY;BYDAY=MO"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, Utc("20260301T000000Z"), Utc("20260317T000000Z"), CalendarThenTzData)
            .Select(occurrence => occurrence.StartUtc.ToString());

        starts.Should().Equal("20260302T140000Z", "20260309T130000Z", "20260316T130000Z");
    }

    [Fact]
    public void GetOccurrences_RuleThatRepeatsForeverWithoutAnEnd_Throws()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY"));

        var act = () => calendar.Events.Single().GetOccurrences(calendar, Utc("20260302T000000Z"), null);

        act.Should().Throw<ArgumentException>().WithParameterName("to");
    }

    [Fact]
    public void GetOccurrences_RuleThatNeverMatches_ThrowsAtTheIterationCap()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "RRULE:FREQ=YEARLY;BYMONTH=2;BYMONTHDAY=30"));
        var options = new IcalOptions { MaxRecurrenceIterations = 50 };

        var act = () => calendar.Events.Single()
            .GetOccurrences(calendar, Utc("20260303T000000Z"), Utc("21000101T000000Z"), IcalTimeZoneResolver.Default, options).ToList();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetOccurrences_IsLazy()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z", "RRULE:FREQ=MINUTELY"));

        var starts = calendar.Events.Single().GetOccurrences(calendar, null, Utc("99991231T000000Z")).Take(3)
            .Select(occurrence => occurrence.Start.ToString());

        starts.Should().Equal("20260302T090000Z", "20260302T090100Z", "20260302T090200Z");
    }

    [Fact]
    public void GetOccurrences_CanBeEnumeratedMoreThanOnce()
    {
        var calendar = Read([
            .. Event("DTSTART:20260302T090000Z", "RRULE:FREQ=DAILY;COUNT=3", "RDATE:20260310T090000Z"),
            .. Override("RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T120000Z")
        ]);

        var occurrences = calendar.Events.First().GetOccurrences(calendar, null, null);

        occurrences.Select(occurrence => occurrence.Start.ToString()).Should().Equal(
            occurrences.Select(occurrence => occurrence.Start.ToString())).And.HaveCount(4);
    }

    [Fact]
    public void GetOccurrences_EventWithoutDtStart_HasNoInstances()
    {
        var calendar = Read(Event("DTSTART:20260302T090000Z"));
        var component = calendar.Events.Single();
        component.DtStart = null;

        component.GetOccurrences(calendar, null, null).Should().BeEmpty();
    }

    [Fact]
    public void GetOccurrences_NullArguments_Throw()
    {
        var component = new IcalEvent();

        var noResolver = () => component.GetOccurrences(null, null, null, null!);
        var noOptions = () => component.GetOccurrences(null, null, null, IcalTimeZoneResolver.Default, null!);
        var badOptions = () => component.GetOccurrences(null, null, null, IcalTimeZoneResolver.Default, new IcalOptions { MaxRecurrenceIterations = 0 });

        noResolver.Should().Throw<ArgumentNullException>().WithParameterName("resolver");
        noOptions.Should().Throw<ArgumentNullException>().WithParameterName("options");
        badOptions.Should().Throw<ArgumentException>().WithParameterName("options");
    }

    #endregion

    #region To-dos and journal entries

    [Fact]
    public void GetOccurrences_TodoWithDue_EndsAtTheSameExactDuration()
    {
        var calendar = Read("BEGIN:VTODO", "UID:todo@example.com", "DTSTAMP:20260101T000000Z", "DTSTART:20260302T090000Z",
            "DUE:20260302T170000Z", "RRULE:FREQ=WEEKLY;COUNT=2", "END:VTODO");

        var occurrences = calendar.Todos.Single().GetOccurrences(calendar, null, null).Select(occurrence => occurrence.ToString());

        occurrences.Should().Equal("20260302T090000Z/20260302T170000Z", "20260309T090000Z/20260309T170000Z");
    }

    [Fact]
    public void GetOccurrences_TodoWithoutDueOrDuration_HasNoEnd()
    {
        var calendar = Read("BEGIN:VTODO", "UID:todo@example.com", "DTSTAMP:20260101T000000Z", "DTSTART:20260302T090000Z",
            "RRULE:FREQ=DAILY;COUNT=2", "END:VTODO");

        var occurrences = calendar.Todos.Single().GetOccurrences(calendar, null, null).ToList();

        occurrences.Should().HaveCount(2).And.OnlyContain(occurrence => occurrence.End == null && occurrence.EndUtc == null);
    }

    [Fact]
    public void GetOccurrences_TodoOverride_ReplacesTheInstance()
    {
        var calendar = Read("BEGIN:VTODO", "UID:todo@example.com", "DTSTAMP:20260101T000000Z", "DTSTART:20260302T090000Z",
            "RRULE:FREQ=DAILY;COUNT=2", "END:VTODO",
            "BEGIN:VTODO", "UID:todo@example.com", "DTSTAMP:20260101T000000Z", "RECURRENCE-ID:20260303T090000Z", "DTSTART:20260303T100000Z",
            "END:VTODO");

        var occurrences = calendar.Todos.First().GetOccurrences(calendar, null, null).ToList();

        occurrences.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20260302T090000Z", "20260303T100000Z");
        occurrences[1].Component.Should().BeSameAs(calendar.Todos.Last());
    }

    [Fact]
    public void GetOccurrences_Journal_HasNoEnd()
    {
        var calendar = Read("BEGIN:VJOURNAL", "UID:journal@example.com", "DTSTAMP:20260101T000000Z", "DTSTART;VALUE=DATE:20260302",
            "RRULE:FREQ=MONTHLY;COUNT=2", "END:VJOURNAL");

        var occurrences = calendar.Journals.Single().GetOccurrences(calendar, null, null).ToList();

        occurrences.Select(occurrence => occurrence.Start.ToString()).Should().Equal("20260302", "20260402");
        occurrences.Should().OnlyContain(occurrence => occurrence.End == null);
    }

    #endregion

    private static IcalCalendar Read(params string[] lines)
    {
        return ModelTestData.ReadCalendar(ModelTestData.Calendar(lines), UndefinedTimeZones);
    }

    private static string[] Event(params string[] lines)
    {
        return ["BEGIN:VEVENT", "UID:event@example.com", "DTSTAMP:20260101T000000Z", .. lines, "END:VEVENT"];
    }

    private static string[] Override(params string[] lines)
    {
        return Event(lines);
    }

    private static IcalDateTime Utc(string text)
    {
        return IcalDateTime.Parse(text, IcalValueFormat.Text);
    }
}
