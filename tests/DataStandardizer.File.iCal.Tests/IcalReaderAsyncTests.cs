using System.Text;
using FluentAssertions;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests;

/// <summary>
/// Tests of the asynchronous members of <see cref="IcalReader"/>.  The sources used can only be read asynchronously, so these tests also show
/// that the async path does no synchronous I/O.
/// </summary>
public class IcalReaderAsyncTests
{
    [Theory]
    [MemberData(nameof(CalendarFixtures.AllNamesAndBufferSizes), MemberType = typeof(CalendarFixtures))]
    public async Task ReadCalendarsAsync_EveryFixture_MatchesSync(string fixtureName, int bufferSize)
    {
        // arrange
        var (text, options) = CalendarFixtures.All[fixtureName];
        using var syncReader = CreateCalendarReader(text, options, bufferSize);
        var expected = syncReader.ReadCalendars().ToList();
        await using var asyncReader = new IcalReader(new AsyncOnlyTextReader(text, 7), options, bufferSize);

        // act
        var testResult = new List<IcalCalendar>();
        await foreach (var calendar in asyncReader.ReadCalendarsAsync())
        {
            testResult.Add(calendar);
        }

        // assert
        WriteCalendars(testResult, options).Should().Be(WriteCalendars(expected, options));
        asyncReader.CalendarsRead.Should().Be(syncReader.CalendarsRead);
        asyncReader.ComponentsRead.Should().Be(syncReader.ComponentsRead);
        asyncReader.LinesRead.Should().Be(syncReader.LinesRead);
        asyncReader.PhysicalLinesRead.Should().Be(syncReader.PhysicalLinesRead);
    }

    [Theory]
    [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
    public async Task ReadCalendarAsync_NonConformant_ThrowsAsSync(string caseName)
    {
        // arrange
        var testCase = ConformanceCases.All[caseName];
        var syncException = FluentActions.Invoking(() => ReadCalendars(testCase.Text)).Should().Throw<IcalFileException>().Which;
        await using var reader = new IcalReader(new AsyncOnlyTextReader(testCase.Text, 7));

        // act
        var act = () => reader.ReadCalendarAsync().AsTask();

        // assert
        var exception = (await act.Should().ThrowAsync<IcalFileException>()).Which;
        exception.Message.Should().Be(syncException.Message);
        exception.Data.Should().BeEquivalentTo(syncException.Data);
    }

    [Fact]
    public async Task ReadCalendarAsync_Stream_ReadsWithoutSynchronousIo()
    {
        // arrange
        var data = Encoding.UTF8.GetBytes(CalendarFixtures.GoogleExport + CalendarFixtures.OutlookExport);
        await using var reader = new IcalReader(new AsyncOnlyStream(data));

        // act
        var first = await reader.ReadCalendarAsync();
        var second = await reader.ReadCalendarAsync();
        var end = await reader.ReadCalendarAsync();

        // assert
        first!.Events.Should().HaveCount(2);
        second!.TimeZones.Single().TimeZoneId.Should().Be("New Zealand Standard Time");
        end.Should().BeNull();
    }

    [Fact]
    public async Task ReadCalendarAsync_BufferedData_CompletesSynchronously()
    {
        // arrange
        var data = Encoding.UTF8.GetBytes(RfcFixtures.Rfc5545Event + RfcFixtures.Rfc5545Event);
        await using var reader = new IcalReader(new AsyncOnlyStream(data, completeSynchronously: true));
        (await reader.ReadCalendarAsync()).Should().NotBeNull();

        // act
        var testResult = reader.ReadCalendarAsync();

        // assert
        testResult.IsCompletedSuccessfully.Should().BeTrue();
        (await testResult).Should().NotBeNull();
    }

    [Fact]
    public async Task ReadCalendarAsync_Canceled_ReaderCannotContinue()
    {
        // arrange
        await using var reader = new IcalReader(new AsyncOnlyTextReader(RfcFixtures.Rfc5545Event));
        using var source = new CancellationTokenSource();
        await source.CancelAsync();

        // act
        var act = () => reader.ReadCalendarAsync(source.Token).AsTask();

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        var next = () => reader.ReadCalendarAsync();
        next.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadCalendarsAsync_EnumeratorToken_IsCombined()
    {
        // arrange
        await using var reader = new IcalReader(new AsyncOnlyTextReader(RfcFixtures.Rfc5545Event + RfcFixtures.Rfc5545Event));
        using var source = new CancellationTokenSource();
        var count = 0;

        // act
        var act = async () =>
        {
            await foreach (var _ in reader.ReadCalendarsAsync().WithCancellation(source.Token))
            {
                count++;
                await source.CancelAsync();
            }
        };

        // assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        count.Should().Be(1);
    }

    [Fact]
    public async Task ReadCalendarAsync_DisposedWhileInProgress_Throws()
    {
        // arrange
        var source = new GatedTextReader(RfcFixtures.Rfc5545Event);
        var reader = new IcalReader(source);
        var pending = reader.ReadCalendarAsync();
        await source.ReadStarted;

        // act
        await reader.DisposeAsync();
        source.OpenGate();

        // assert
        await FluentActions.Awaiting(() => pending.AsTask()).Should().ThrowAsync<ObjectDisposedException>();
    }
}
