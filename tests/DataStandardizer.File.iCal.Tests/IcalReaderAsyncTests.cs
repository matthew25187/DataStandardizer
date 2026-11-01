using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="IcalReader"/>.  The sources used can only be read asynchronously, so these tests also show
    /// that the async path does no synchronous I/O.
    /// </summary>
    public class IcalReaderAsyncTests
    {
        // The number of characters that AsyncOnlyTextReader gives each read, so that reads end at many different points.
        private const int CharactersPerRead = 7;

        private static readonly byte[] GoogleThenOutlookExport = Encoding.UTF8.GetBytes(CalendarFixtures.GoogleExport + CalendarFixtures.OutlookExport);

        [Theory]
        [MemberData(nameof(CalendarFixtures.AllNamesAndBufferSizes), MemberType = typeof(CalendarFixtures))]
        public async Task ReadCalendarsAsync_EveryFixture_ReadsItsContentLines(string fixtureName, int bufferSize)
        {
            // arrange
            var (text, options) = CalendarFixtures.All[fixtureName];
            var expected = FixtureExpectations.Models[fixtureName];
            var reader = new IcalReader(new AsyncOnlyTextReader(text, CharactersPerRead), options, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = XcalTestData.Model(await AsyncEnumerables.ToListAsync(reader.ReadCalendarsAsync()), options);

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Theory]
        [MemberData(nameof(CalendarFixtures.AllNamesAndBufferSizes), MemberType = typeof(CalendarFixtures))]
        public async Task ReadCalendarsAsync_EveryFixture_CountsWhatItReads(string fixtureName, int bufferSize)
        {
            // arrange
            var (text, options) = CalendarFixtures.All[fixtureName];
            var expected = FixtureExpectations.Counts[fixtureName];
            var reader = new IcalReader(new AsyncOnlyTextReader(text, CharactersPerRead), options, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                await AsyncEnumerables.ToListAsync(reader.ReadCalendarsAsync());

                // assert
                reader.CalendarsRead.Should().Be(expected.Calendars);
                reader.ComponentsRead.Should().Be(expected.Components);
                reader.LinesRead.Should().Be(expected.Lines);
                reader.PhysicalLinesRead.Should().Be(expected.PhysicalLines);
            });
        }

        [Theory]
        [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
        public async Task ReadCalendarAsync_NonConformant_ThrowsWithTheDiagnostic(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];
            var reader = new IcalReader(new AsyncOnlyTextReader(testCase.Text, CharactersPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadCalendarAsync().AsTask();

                // assert
                var exception = (await testAction.Should().ThrowAsync<IcalFileException>()).Which;
                exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
                exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath);
                exception.Data[IcalFileException.DataItemName.PropertyName].Should().Be(testCase.PropertyName);
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_FirstCalendarFromStream_ReadsWithoutSynchronousIo()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyStream(GoogleThenOutlookExport));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await reader.ReadCalendarAsync();

                // assert
                testResult.Events.Should().HaveCount(2);
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_SecondCalendarFromStream_ReadsWithoutSynchronousIo()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyStream(GoogleThenOutlookExport));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadCalendarAsync();

                // act
                var testResult = await reader.ReadCalendarAsync();

                // assert
                testResult.TimeZones.Should().ContainSingle().Which.TimeZoneId.Should().Be("New Zealand Standard Time");
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterTheLastCalendarFromStream_ReturnsNull()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyStream(GoogleThenOutlookExport));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadCalendarAsync();
                await reader.ReadCalendarAsync();

                // act
                var testResult = await reader.ReadCalendarAsync();

                // assert
                testResult.Should().BeNull();
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_BufferedData_CompletesSynchronously()
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(RfcFixtures.Rfc5545Event + RfcFixtures.Rfc5545Event);
            var reader = new IcalReader(new AsyncOnlyStream(data, completeSynchronously: true));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadCalendarAsync();

                // act
                var testResult = reader.ReadCalendarAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                testResult.Result.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_Canceled_ThrowsOperationCanceled()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyTextReader(RfcFixtures.Rfc5545Event));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> testAction = () => reader.ReadCalendarAsync(source.Token).AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterCancellation_ThrowsInvalidOperation()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyTextReader(RfcFixtures.Rfc5545Event));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);
                    await Record.ExceptionAsync(() => reader.ReadCalendarAsync(source.Token).AsTask());

                    // act
                    Action testAction = () => reader.ReadCalendarAsync();

                    // assert
                    testAction.Should().Throw<InvalidOperationException>();
                }
            });
        }

        [Fact]
        public async Task ReadCalendarsAsync_EnumeratorTokenCanceledAfterTheFirstCalendar_ThrowsOperationCanceled()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyTextReader(RfcFixtures.Rfc5545Event + RfcFixtures.Rfc5545Event));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    var enumerator = reader.ReadCalendarsAsync().GetAsyncEnumerator(source.Token);
                    await enumerator.MoveNextAsync();
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> testAction = () => enumerator.MoveNextAsync().AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_DisposedWhileInProgress_Throws()
        {
            // arrange
            var source = new GatedTextReader(RfcFixtures.Rfc5545Event);
            var reader = new IcalReader(source);
            var pending = reader.ReadCalendarAsync();
            await source.ReadStarted;
            await reader.DisposeAsync();
            source.OpenGate();

            // act
            Func<Task> testAction = () => pending.AsTask();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }
    }
}
