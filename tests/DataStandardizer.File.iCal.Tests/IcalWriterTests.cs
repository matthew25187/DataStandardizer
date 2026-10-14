using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.IcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Tests of <see cref="IcalWriter"/>: round trips, VERSION, conformance checks, progress, and the asynchronous members.
/// </summary>
public class IcalWriterTests
{
    #region Round trips

    [Theory]
    [MemberData(nameof(CalendarFixtures.AllNames), MemberType = typeof(CalendarFixtures))]
    public void WriteCalendars_EveryFixture_RoundTripsContentLines(string fixtureName)
    {
        // arrange
        var (text, options) = CalendarFixtures.All[fixtureName];
        var calendars = ReadCalendars(text, options);

        // act
        var testResult = WriteCalendars(calendars, options);

        // assert
        ContentLines(testResult).Should().BeEquivalentTo(ContentLines(text), assertion => assertion.WithStrictOrdering());
        ReadCalendars(testResult, options).Should().HaveCount(calendars.Count);
    }

    [Theory]
    [InlineData(nameof(CalendarFixtures.OutlookExport))]
    [InlineData(nameof(CalendarFixtures.AppleExport))]
    public void WriteCalendars_FixtureFoldedAtDefaultLength_RoundTripsExactly(string fixtureName)
    {
        // arrange
        var (text, options) = CalendarFixtures.All[fixtureName];

        // act
        var testResult = WriteCalendars(ReadCalendars(text, options), options);

        // assert
        testResult.Should().Be(text);
    }

    [Fact]
    public void WriteCalendars_ParametersAsRead_AreKeptExactly()
    {
        // arrange
        var text = EventCalendar("ATTENDEE;CN=\"Doe, Jane\";X-Q=\"plain\";X-C=a^'b:mailto:j@example.com");

        // act
        var testResult = WriteCalendars(ReadCalendars(text));

        // assert
        testResult.Should().Be(text);
    }

    [Fact]
    public void WriteCalendar_BuiltInCode_WritesExpectedLines()
    {
        // arrange
        var calendar = new IcalCalendar { ProductId = "-//Example//Test//EN" };
        var testEvent = new IcalEvent
        {
            Uid = "1@example.com",
            DtStamp = new IcalDateTime(new IcalDate(2026, 10, 15), new IcalTime(9, 0, 0, true)),
            DtStart = new IcalDate(2026, 12, 25),
            Summary = "Christmas; holiday, family",
            RecurrenceRules = [new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly)]
        };
        testEvent.Components.Add(new IcalAlarm { Action = "DISPLAY", Description = "Tomorrow", TriggerDuration = IcalDuration.Parse("-P1D", IcalValueFormat.Text) });
        calendar.Components.Add(testEvent);
        var text = new StringWriter();
        using var writer = new IcalWriter(text);

        // act
        writer.WriteCalendar(calendar);

        // assert
        text.ToString().Should().Be(Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//Example//Test//EN", "BEGIN:VEVENT", "UID:1@example.com",
            "DTSTAMP:20261015T090000Z", "DTSTART;VALUE=DATE:20261225", "SUMMARY:Christmas\\; holiday\\, family", "RRULE:FREQ=YEARLY", "BEGIN:VALARM",
            "ACTION:DISPLAY", "DESCRIPTION:Tomorrow", "TRIGGER:-P1D", "END:VALARM", "END:VEVENT", "END:VCALENDAR"));
        writer.CalendarsWritten.Should().Be(1);
        writer.ComponentsWritten.Should().Be(2);
        writer.LinesWritten.Should().Be(16);
    }

    #endregion

    #region VERSION

    [Fact]
    public void WriteCalendar_NoVersion_WritesVersion20First()
    {
        // arrange
        var calendar = ReadCalendar(ConformanceCases.All["MissingVersion"].Text, new IcalOptions { AllowMissingRequiredProperties = true });

        // act
        var testResult = WriteCalendars([calendar]);

        // assert
        testResult.Should().StartWith(Crlf("BEGIN:VCALENDAR", "VERSION:2.0", "PRODID:-//Test//Test//EN"));
        calendar.Validate().Should().ContainSingle().Which.PropertyName.Should().Be("VERSION");
    }

    [Theory]
    [InlineData("1.0;2.0")]
    [InlineData("2.0;3.0")]
    public void WriteCalendar_VersionRangeIncluding2_WritesVersion20InPlace(string version)
    {
        // arrange
        var calendar = ReadCalendar(RfcFixtures.Rfc5545Event.Replace("VERSION:2.0", "VERSION:" + version, StringComparison.Ordinal));

        // act
        var testResult = WriteCalendars([calendar]);

        // assert
        testResult.Should().StartWith(Crlf("BEGIN:VCALENDAR", "PRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN", "VERSION:2.0",
            "BEGIN:VEVENT"));
    }

    [Fact]
    public void WriteCalendar_UnsupportedVersion_Throws()
    {
        // arrange
        var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
        calendar.Version = "1.0";
        var text = new StringWriter();
        using var writer = new IcalWriter(text);

        // act
        var act = () => writer.WriteCalendar(calendar);

        // assert
        act.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should()
            .Be(IcalDiagnosticKind.UnsupportedVersion);
        text.ToString().Should().BeEmpty();
    }

    #endregion

    #region Conformance

    [Theory]
    [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
    public void WriteCalendar_NonConformantWithDefaults_ThrowsAndWritesNothing(string caseName)
    {
        // arrange
        var testCase = ConformanceCases.All[caseName];
        var calendar = ReadCalendar(testCase.Text, ConformanceCases.OptionsAccepting(testCase.Kind));
        var text = new StringWriter();
        using var writer = new IcalWriter(text);

        // act
        var act = () => writer.WriteCalendar(calendar);

        // assert
        if (caseName == "MissingVersion")
        {
            // The writer supplies VERSION:2.0 itself.
            act.Should().NotThrow();
            return;
        }

        var exception = act.Should().Throw<IcalFileException>().Which;
        exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
        exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath);
        text.ToString().Should().BeEmpty();
        writer.CalendarsWritten.Should().Be(0);
    }

    [Theory]
    [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
    public void WriteCalendar_NonConformantWithItsOption_RoundTrips(string caseName)
    {
        // arrange
        var testCase = ConformanceCases.All[caseName];
        var options = ConformanceCases.OptionsAccepting(testCase.Kind);
        var calendar = ReadCalendar(testCase.Text, options);

        // act
        var testResult = WriteCalendars([calendar], options);

        // assert
        ReadCalendar(testResult, options).Validate().Should().HaveCount(caseName == "MissingVersion" ? 0 : 1);
    }

    [Fact]
    public void WriteCalendars_ModelBuiltInCode_IsChecked()
    {
        // arrange
        var calendar = new IcalCalendar { ProductId = "x" };
        calendar.Components.Add(new IcalEvent
        {
            Uid = "e", DtStamp = new IcalDateTime(new IcalDate(2026, 1, 1), new IcalTime(0, 0, 0)), DtStart = new IcalDate(2026, 1, 1)
        });

        // act
        var act = () => WriteCalendars([calendar]);

        // assert
        var exception = act.Should().Throw<IcalFileException>().Which;
        exception.Message.Should().Be("The DTSTAMP property must be a DATE-TIME in UTC.");
        exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be("VCALENDAR/VEVENT[1]");
        exception.Data.Contains(IcalFileException.DataItemName.LineNumber).Should().BeFalse();
    }

    #endregion

    #region Bulk writes and progress

    [Fact]
    public void WriteCalendars_Progress_ReportedAfterEachCalendar()
    {
        // arrange
        var calendars = ReadCalendars(CalendarFixtures.GoogleExport + RfcFixtures.Rfc5545Event);
        var googleLines = ContentLines(CalendarFixtures.GoogleExport).Count;
        var eventLines = ContentLines(RfcFixtures.Rfc5545Event).Count;
        var progress = new RecordingProgress();
        using var writer = new IcalWriter(new StringWriter());

        // act
        writer.WriteCalendars(calendars, progress);

        // assert
        progress.Reports.Should().Equal(new IcalWriteProgress(googleLines, 6, 1), new IcalWriteProgress(googleLines + eventLines, 7, 2));
        writer.LinesWritten.Should().Be(googleLines + eventLines);
    }

    [Fact]
    public void WriteCalendars_NullCalendar_ThrowsAfterEarlierCalendars()
    {
        // arrange
        var text = new StringWriter();
        using var writer = new IcalWriter(text);

        // act
        var act = () => writer.WriteCalendars([ReadCalendar(RfcFixtures.Rfc5545Event), null!]);

        // assert
        act.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("calendars");
        ReadCalendars(text.ToString()).Should().ContainSingle();
    }

    [Fact]
    public void WriteCalendars_ProgressCallsWriter_Throws()
    {
        // arrange
        var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
        var writer = new IcalWriter(new StringWriter());
        var progress = new RecordingProgress(_ => writer.WriteCalendar(calendar));

        // act
        var act = () => writer.WriteCalendars([calendar], progress);

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Another operation is in progress.*");
        writer.CalendarsWritten.Should().Be(1);
    }

    #endregion

    #region Asynchronous

    [Theory]
    [MemberData(nameof(CalendarFixtures.AllNames), MemberType = typeof(CalendarFixtures))]
    public async Task WriteCalendarsAsync_EveryFixture_MatchesSync(string fixtureName)
    {
        // arrange
        var (text, options) = CalendarFixtures.All[fixtureName];
        var calendars = ReadCalendars(text, options);
        var expected = WriteCalendars(calendars, options);
        var target = new AsyncOnlyTextWriter();
        var progress = new RecordingProgress();

        // act
        await using (var writer = new IcalWriter(target, options))
        {
            await writer.WriteCalendarsAsync(calendars, progress);
            await writer.FlushAsync();
        }

        // assert
        target.ToString().Should().Be(expected);
        progress.Reports.Should().HaveCount(calendars.Count);
    }

    [Fact]
    public async Task WriteCalendarAsync_AsyncEnumerableAndSingle_MatchSync()
    {
        // arrange
        var calendars = ReadCalendars(CalendarFixtures.GoogleExport + CalendarFixtures.OutlookExport);
        var expected = WriteCalendars(calendars.Concat(calendars));
        var target = new AsyncOnlyTextWriter();
        await using var writer = new IcalWriter(target);

        // act
        await writer.WriteCalendarsAsync(ToAsync(calendars));
        foreach (var calendar in calendars)
        {
            await writer.WriteCalendarAsync(calendar);
        }

        // assert
        target.ToString().Should().Be(expected);
        writer.CalendarsWritten.Should().Be(4);
        writer.ComponentsWritten.Should().Be(22);
    }

    [Fact]
    public async Task WriteCalendarAsync_NonConformant_FaultsTaskAndWriterContinues()
    {
        // arrange
        var testCase = ConformanceCases.All["TwoSummaries"];
        var calendar = ReadCalendar(testCase.Text, ConformanceCases.OptionsAccepting(testCase.Kind));
        var target = new AsyncOnlyTextWriter();
        await using var writer = new IcalWriter(target);

        // act
        var task = writer.WriteCalendarAsync(calendar);

        // assert
        await FluentActions.Awaiting(() => task.AsTask()).Should().ThrowAsync<IcalFileException>();
        await writer.WriteCalendarAsync(ReadCalendar(RfcFixtures.Rfc5545Event));
        ReadCalendars(target.ToString()).Should().ContainSingle();
    }

    [Fact]
    public async Task WriteCalendarsAsync_Canceled_WriterCannotContinue()
    {
        // arrange
        var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
        await using var writer = new IcalWriter(new AsyncOnlyTextWriter());
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // act
        var act = () => writer.WriteCalendarsAsync([calendar], cancellationToken: source.Token);

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        Action next = () => writer.WriteCalendarsAsync([calendar]);
        next.Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
        writer.CalendarsWritten.Should().Be(0);
    }

    [Fact]
    public async Task WriteCalendarAsync_CanceledMidCalendar_WriterCannotContinue()
    {
        // arrange
        var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
        var target = new GatedTextWriter();
        await using var writer = new IcalWriter(target);
        using var source = new CancellationTokenSource();
        var pending = writer.WriteCalendarAsync(calendar, source.Token);
        await target.WriteStarted;

        // act
        await source.CancelAsync();

        // assert
        await FluentActions.Awaiting(() => pending.AsTask()).Should().ThrowAsync<OperationCanceledException>();
        var act = () => writer.WriteCalendar(calendar);
        act.Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
    }

    [Fact]
    public async Task WriteCalendar_WhileAsyncWriteInProgress_Throws()
    {
        // arrange
        var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
        var target = new GatedTextWriter();
        await using var writer = new IcalWriter(target);
        var pending = writer.WriteCalendarAsync(calendar);
        await target.WriteStarted;

        // act
        var act = () => writer.WriteCalendar(calendar);

        // assert
        act.Should().Throw<InvalidOperationException>().WithMessage("Another operation is in progress.*");
        target.OpenGate();
        await pending;
        ContentLines(target.ToString()).Should().BeEquivalentTo(ContentLines(RfcFixtures.Rfc5545Event), options => options.WithStrictOrdering());
    }

    #endregion

    #region Disposal

    [Fact]
    public void Dispose_ThenMembers_ThrowObjectDisposedException()
    {
        // arrange
        var calendar = new IcalCalendar();
        var writer = new IcalWriter(new StringWriter());
        writer.Dispose();

        // act
        Action[] members =
        [
            () => writer.WriteCalendar(calendar), () => writer.WriteCalendars([calendar]), () => writer.WriteCalendarAsync(calendar),
            () => writer.WriteCalendarsAsync([calendar]), () => writer.WriteCalendarsAsync(ToAsync([calendar])), () => writer.Flush(),
            () => writer.FlushAsync(), () => _ = writer.CalendarsWritten, () => _ = writer.ComponentsWritten, () => _ = writer.LinesWritten
        ];

        // assert
        foreach (var member in members)
        {
            member.Should().Throw<ObjectDisposedException>();
        }

        writer.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_StreamWriter_FlushesToStream()
    {
        // arrange
        var stream = new MemoryStream();
        var writer = new IcalWriter(stream);
        await writer.WriteCalendarAsync(ReadCalendar(RfcFixtures.Rfc5545Event));

        // act
        await writer.DisposeAsync();

        // assert
        ContentLines(System.Text.Encoding.UTF8.GetString(stream.ToArray())).Should()
            .BeEquivalentTo(ContentLines(RfcFixtures.Rfc5545Event), options => options.WithStrictOrdering());
        await writer.DisposeAsync();
    }

    #endregion

    private static async IAsyncEnumerable<IcalCalendar> ToAsync(IEnumerable<IcalCalendar> calendars)
    {
        foreach (var calendar in calendars)
        {
            await Task.Yield();
            yield return calendar;
        }
    }
}
