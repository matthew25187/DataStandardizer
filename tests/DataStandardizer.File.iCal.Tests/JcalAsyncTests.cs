using System.Text;
using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Tests of the asynchronous members of <see cref="JcalReader"/> and <see cref="JcalWriter"/>.  The streams used can only be read or written
/// asynchronously, so these tests also show that the async paths do no synchronous I/O.
/// </summary>
public class JcalAsyncTests
{
    [Theory]
    [MemberData(nameof(JcalRoundTripTests.CalendarNames), MemberType = typeof(JcalRoundTripTests))]
    public async Task ReadCalendarsAsync_EveryFixture_MatchesSync(string calendarName)
    {
        // arrange
        var (text, options) = JcalRoundTripTests.GetCalendar(calendarName);
        var jcal = WriteJcal(ReadCalendars(text, options), options);
        using var syncReader = new JcalReader(new MemoryStream(Utf8(jcal)), options ?? new IcalOptions());
        var expected = syncReader.ReadCalendars().ToList();
        await using var asyncReader = new JcalReader(new AsyncOnlyStream(Utf8(jcal), maxReadLength: 7), options ?? new IcalOptions());

        // act
        var testResult = new List<IcalCalendar>();
        await foreach (var calendar in asyncReader.ReadCalendarsAsync())
        {
            testResult.Add(calendar);
        }

        // assert
        Model(testResult, options).Should().Equal(Model(expected, options));
        asyncReader.CalendarsRead.Should().Be(syncReader.CalendarsRead);
        asyncReader.ComponentsRead.Should().Be(syncReader.ComponentsRead);
        asyncReader.PropertiesRead.Should().Be(syncReader.PropertiesRead);
    }

    [Theory]
    [MemberData(nameof(JcalRoundTripTests.CalendarNames), MemberType = typeof(JcalRoundTripTests))]
    public async Task WriteCalendarsAsync_EveryFixture_MatchesSync(string calendarName)
    {
        // arrange
        var (text, options) = JcalRoundTripTests.GetCalendar(calendarName);
        var calendars = ReadCalendars(text, options);
        var expected = WriteJcal(calendars, options);
        var stream = new AsyncOnlyWriteStream();
        var progress = new RecordingProgress();

        // act
        await using (var writer = new JcalWriter(stream, options ?? new IcalOptions()))
        {
            await writer.WriteCalendarsAsync(calendars, progress);
        }

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
        progress.Reports.Should().HaveCount(calendars.Count);
        stream.IsDisposed.Should().BeFalse();
    }

    [Fact]
    public async Task ReadCalendarAsync_SeveralCalendars_ReadsWithoutSynchronousIo()
    {
        // arrange
        var json = "[" + Example1Jcal + "," + Example2Jcal + "]";
        await using var reader = new JcalReader(new AsyncOnlyStream(Utf8(json), maxReadLength: 3));

        // act
        var first = await reader.ReadCalendarAsync();
        var second = await reader.ReadCalendarAsync();
        var end = await reader.ReadCalendarAsync();

        // assert
        Model([first!, second!]).Should().Equal(Model(ReadCalendars(Example1Ical + Example2Ical)));
        end.Should().BeNull();
    }

    [Fact]
    public async Task ReadCalendarAsync_BufferedData_CompletesSynchronously()
    {
        // arrange
        var json = "[" + Example1Jcal + "," + Example1Jcal + "]";
        await using var reader = new JcalReader(new AsyncOnlyStream(Utf8(json)));
        await reader.ReadCalendarAsync();

        // act: the second calendar is already in the buffer.
        var task = reader.ReadCalendarAsync();

        // assert
        task.IsCompletedSuccessfully.Should().BeTrue();
        (await task).Should().NotBeNull();
    }

    [Fact]
    public async Task ReadCalendarAsync_NotValid_ThrowsAsSync()
    {
        // arrange
        var json = JournalDocument("[\"summary\",{},\"text\",\"a\",\"b\"]");
        var syncException = FluentActions.Invoking(() => ReadJcal(json)).Should().Throw<IcalFileException>().Which;
        await using var reader = new JcalReader(new AsyncOnlyStream(Utf8(json), maxReadLength: 7));

        // act
        var act = () => reader.ReadCalendarAsync().AsTask();

        // assert
        var exception = (await act.Should().ThrowAsync<IcalFileException>()).Which;
        exception.Message.Should().Be(syncException.Message);
        exception.Data.Should().BeEquivalentTo(syncException.Data);
        await reader.Invoking(value => value.ReadCalendarAsync().AsTask()).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadCalendarAsync_Canceled_FaultsReader()
    {
        // arrange
        await using var reader = new JcalReader(new AsyncOnlyStream(Utf8(Example1Jcal), maxReadLength: 7));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // act
        var act = () => reader.ReadCalendarAsync(source.Token).AsTask();

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        reader.Invoking(value => value.ReadCalendar()).Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadCalendarAsync_WhileReadInProgress_ThrowsAndDisposeWaits()
    {
        // arrange
        var source = new GatedStream(Utf8(Example1Jcal));
        var reader = new JcalReader(source);
        var read = reader.ReadCalendarAsync().AsTask();
        await source.ReadStarted;

        // act
        var secondRead = () => reader.ReadCalendarAsync();
        var syncRead = () => reader.ReadCalendar();
        reader.Dispose();
        source.OpenGate();

        // assert
        secondRead.Should().Throw<ObjectDisposedException>();
        syncRead.Should().Throw<ObjectDisposedException>();
        await FluentActions.Awaiting(() => read).Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task ReadCalendarAsync_SecondReadWhileInProgress_ThrowsInvalidOperation()
    {
        // arrange
        var source = new GatedStream(Utf8(Example1Jcal));
        await using var reader = new JcalReader(source);
        var read = reader.ReadCalendarAsync().AsTask();
        await source.ReadStarted;

        // act
        var secondRead = () => reader.ReadCalendarAsync();

        // assert
        secondRead.Should().Throw<InvalidOperationException>().WithMessage("Another read*");
        source.OpenGate();
        (await read).Should().NotBeNull();
    }

    [Fact]
    public async Task WriteCalendarAsync_Stream_WritesWithoutSynchronousIo()
    {
        // arrange
        var stream = new AsyncOnlyWriteStream();

        // act
        await using (var writer = new JcalWriter(stream))
        {
            await writer.WriteCalendarAsync(ReadCalendar(Example1Ical));
            await writer.WriteCalendarAsync(ReadCalendar(Example2Ical));
            await writer.FlushAsync();
        }

        // assert
        Model(ReadJcal(Encoding.UTF8.GetString(stream.ToArray()))).Should().Equal(Model(ReadCalendars(Example1Ical + Example2Ical)));
        stream.FlushCount.Should().Be(2);
    }

    [Fact]
    public async Task WriteCalendarsAsync_AsyncEnumerable_WritesEach()
    {
        // arrange
        var calendars = ReadCalendars(Example1Ical + Example2Ical);
        var stream = new MemoryStream();

        async IAsyncEnumerable<IcalCalendar> Calendars()
        {
            foreach (var calendar in calendars)
            {
                await Task.Yield();
                yield return calendar;
            }
        }

        // act
        await using (var writer = new JcalWriter(stream))
        {
            await writer.WriteCalendarsAsync(Calendars());
            writer.CalendarsWritten.Should().Be(2);
        }

        // assert
        Encoding.UTF8.GetString(stream.ToArray()).Should().Be(WriteJcal(calendars));
    }

    [Fact]
    public async Task WriteCalendarAsync_Canceled_FaultsWriter()
    {
        // arrange
        var stream = new MemoryStream();
        await using var writer = new JcalWriter(stream);
        using var source = new CancellationTokenSource();
        await source.CancelAsync();
        var calendar = ReadCalendar(Example1Ical);

        // act
        var act = () => writer.WriteCalendarAsync(calendar, source.Token).AsTask();

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        writer.Invoking(value => value.WriteCalendar(calendar)).Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
    }

    [Fact]
    public async Task DisposeAsync_WhileFlushInProgress_EndsDocumentAfterFlush()
    {
        // arrange
        var stream = new GatedWriteStream();
        var writer = new JcalWriter(stream);
        await writer.WriteCalendarAsync(ReadCalendar(Example1Ical));
        var flush = writer.FlushAsync();
        await stream.FlushStarted;

        // act
        var dispose = writer.DisposeAsync();
        var write = () => writer.WriteCalendarAsync(ReadCalendar(Example1Ical));
        stream.OpenGate();
        await flush;
        await dispose;

        // assert
        write.Should().Throw<ObjectDisposedException>();
        Model(ReadJcal(Encoding.UTF8.GetString(stream.ToArray()))).Should().Equal(Model(ReadCalendars(Example1Ical)));
    }
}
