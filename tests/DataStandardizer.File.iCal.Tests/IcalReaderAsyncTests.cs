using System;
using System.IO;
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
        [Theory]
        [MemberData(nameof(CalendarFixtures.AllNamesAndBufferSizes), MemberType = typeof(CalendarFixtures))]
        public async Task ReadCalendarsAsync_EveryFixture_MatchesSync(string fixtureName, int bufferSize)
        {
            // arrange
            var (text, options) = CalendarFixtures.All[fixtureName];
            using (var syncReader = CreateCalendarReader(text, options, bufferSize))
            {
                var expected = syncReader.ReadCalendars().ToList();
                var asyncReader = new IcalReader(new AsyncOnlyTextReader(text, 7), options, bufferSize);
                try
                {
                    // act
                    var testResult = await AsyncEnumerables.ToListAsync(asyncReader.ReadCalendarsAsync());

                    // assert
                    WriteCalendars(testResult, options).Should().Be(WriteCalendars(expected, options));
                    asyncReader.CalendarsRead.Should().Be(syncReader.CalendarsRead);
                    asyncReader.ComponentsRead.Should().Be(syncReader.ComponentsRead);
                    asyncReader.LinesRead.Should().Be(syncReader.LinesRead);
                    asyncReader.PhysicalLinesRead.Should().Be(syncReader.PhysicalLinesRead);
                }
                finally
                {
                    await asyncReader.DisposeAsync();
                }
            }
        }

        [Theory]
        [MemberData(nameof(ConformanceCases.Accommodated), MemberType = typeof(ConformanceCases))]
        public async Task ReadCalendarAsync_NonConformant_ThrowsAsSync(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];
            var syncException = FluentActions.Invoking(() => ReadCalendars(testCase.Text)).Should().Throw<IcalFileException>().Which;
            var reader = new IcalReader(new AsyncOnlyTextReader(testCase.Text, 7));
            try
            {
                // act
                Func<Task> act = () => reader.ReadCalendarAsync().AsTask();

                // assert
                var exception = (await act.Should().ThrowAsync<IcalFileException>()).Which;
                exception.Message.Should().Be(syncException.Message);
                exception.Data.Should().BeEquivalentTo(syncException.Data);
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_Stream_ReadsWithoutSynchronousIo()
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(CalendarFixtures.GoogleExport + CalendarFixtures.OutlookExport);
            var reader = new IcalReader(new AsyncOnlyStream(data));
            try
            {
                // act
                var first = await reader.ReadCalendarAsync();
                var second = await reader.ReadCalendarAsync();
                var end = await reader.ReadCalendarAsync();

                // assert
                first.Events.Should().HaveCount(2);
                second.TimeZones.Single().TimeZoneId.Should().Be("New Zealand Standard Time");
                end.Should().BeNull();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_BufferedData_CompletesSynchronously()
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(RfcFixtures.Rfc5545Event + RfcFixtures.Rfc5545Event);
            var reader = new IcalReader(new AsyncOnlyStream(data, completeSynchronously: true));
            try
            {
                (await reader.ReadCalendarAsync()).Should().NotBeNull();

                // act
                var testResult = reader.ReadCalendarAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                (await testResult).Should().NotBeNull();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_Canceled_ReaderCannotContinue()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyTextReader(RfcFixtures.Rfc5545Event));
            try
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> act = () => reader.ReadCalendarAsync(source.Token).AsTask();

                    // assert
                    await act.Should().ThrowAsync<OperationCanceledException>();
                    Action next = () => reader.ReadCalendarAsync();
                    next.Should().Throw<InvalidOperationException>();
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarsAsync_EnumeratorToken_IsCombined()
        {
            // arrange
            var reader = new IcalReader(new AsyncOnlyTextReader(RfcFixtures.Rfc5545Event + RfcFixtures.Rfc5545Event));
            try
            {
                using (var source = new CancellationTokenSource())
                {
                    var count = 0;

                    // act
                    Func<Task> act = async () =>
                    {
                        var enumerator = reader.ReadCalendarsAsync().GetAsyncEnumerator(source.Token);
                        try
                        {
                            while (await enumerator.MoveNextAsync())
                            {
                                count++;
                                await TestTasks.CancelAsync(source);
                            }
                        }
                        finally
                        {
                            await enumerator.DisposeAsync();
                        }
                    };

                    // assert
                    await act.Should().ThrowAsync<OperationCanceledException>();
                    count.Should().Be(1);
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }
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
}
