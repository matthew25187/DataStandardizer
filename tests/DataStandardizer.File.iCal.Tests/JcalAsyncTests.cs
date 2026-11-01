using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.JcalTestData;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="JcalReader"/> and <see cref="JcalWriter"/>.  The streams used can only be read or written
    /// asynchronously, so these tests also show that the async paths do no synchronous I/O.
    /// </summary>
    public class JcalAsyncTests
    {
        // The number of octets that AsyncOnlyStream gives each read, so that reads end at many different points.
        private const int OctetsPerRead = 7;

        private const string CanceledMessage = "*canceled*";

        // A document whose SUMMARY has a second value, with the JSONPath and byte position of that value.
        private const string SecondValueJsonPath = "$[2][0][1][2][4]";
        private const long SecondValueBytePosition = 207;

        private static readonly string SecondValueDocument = JournalDocument("[\"summary\",{},\"text\",\"a\",\"b\"]");

        private static readonly string Example1ThenExample2 = "[" + Example1Jcal + "," + Example2Jcal + "]";

        [Theory]
        [MemberData(nameof(JcalRoundTripTests.CalendarNames), MemberType = typeof(JcalRoundTripTests))]
        public async Task ReadCalendarsAsync_EveryFixture_MatchesSync(string calendarName)
        {
            // arrange
            var (text, options) = JcalRoundTripTests.GetCalendar(calendarName);
            var jcal = WriteJcal(ReadCalendars(text, options), options);
            using (var syncReader = new JcalReader(new MemoryStream(Utf8(jcal)), options ?? new IcalOptions()))
            {
                var expected = Model(syncReader.ReadCalendars().ToList(), options);
                var asyncReader = new JcalReader(new AsyncOnlyStream(Utf8(jcal), maxReadLength: OctetsPerRead), options ?? new IcalOptions());
                await AsyncDisposal.UsingAsync(asyncReader, async () =>
                {
                    // act
                    var testResult = Model(await AsyncEnumerables.ToListAsync(asyncReader.ReadCalendarsAsync()), options);

                    // assert
                    testResult.Should().Equal(expected);
                    asyncReader.CalendarsRead.Should().Be(syncReader.CalendarsRead);
                    asyncReader.ComponentsRead.Should().Be(syncReader.ComponentsRead);
                    asyncReader.PropertiesRead.Should().Be(syncReader.PropertiesRead);
                });
            }
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
            var writer = new JcalWriter(stream, options ?? new IcalOptions());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(calendars, progress);
            });

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
            progress.Reports.Should().HaveCount(calendars.Count);
            stream.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task ReadCalendarAsync_FirstOfSeveralCalendars_ReadsWithoutSynchronousIo()
        {
            // arrange
            var expected = Model(ReadCalendars(Example1Ical));
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(Example1ThenExample2), maxReadLength: 3));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = Model(new[] { await reader.ReadCalendarAsync() });

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_SecondOfSeveralCalendars_ReadsWithoutSynchronousIo()
        {
            // arrange
            var expected = Model(ReadCalendars(Example2Ical));
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(Example1ThenExample2), maxReadLength: 3));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadCalendarAsync();

                // act
                var testResult = Model(new[] { await reader.ReadCalendarAsync() });

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterTheLastOfSeveralCalendars_ReturnsNull()
        {
            // arrange
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(Example1ThenExample2), maxReadLength: 3));
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
            var json = "[" + Example1Jcal + "," + Example1Jcal + "]";
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(json)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadCalendarAsync();

                // act: the second calendar is already in the buffer.
                var testResult = reader.ReadCalendarAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                testResult.Result.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_NotValid_ThrowsAsSync()
        {
            // arrange
            var syncException = Record.Exception(() => ReadJcal(SecondValueDocument));
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(SecondValueDocument), maxReadLength: OctetsPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadCalendarAsync().AsTask();

                // assert
                var exception = (await testAction.Should().ThrowAsync<IcalFileException>()).Which;
                exception.Data[IcalFileException.DataItemName.JsonPath].Should().Be(SecondValueJsonPath);
                exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be(SecondValueBytePosition);
                exception.Message.Should().Be(syncException.Message);
                exception.Data.Should().BeEquivalentTo(syncException.Data);
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterNotValid_ThrowsInvalidOperation()
        {
            // arrange
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(SecondValueDocument), maxReadLength: OctetsPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await Record.ExceptionAsync(() => reader.ReadCalendarAsync().AsTask());

                // act
                Func<Task> testAction = () => reader.ReadCalendarAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_Canceled_ThrowsOperationCanceled()
        {
            // arrange
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(Example1Jcal), maxReadLength: OctetsPerRead));
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
        public async Task ReadCalendar_AfterCancellation_ThrowsInvalidOperation()
        {
            // arrange
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(Example1Jcal), maxReadLength: OctetsPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);
                    await Record.ExceptionAsync(() => reader.ReadCalendarAsync(source.Token).AsTask());

                    // act
                    Action testAction = () => reader.ReadCalendar();

                    // assert
                    testAction.Should().Throw<InvalidOperationException>();
                }
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterDisposeWhileReadInProgress_ThrowsObjectDisposed()
        {
            // arrange
            var source = new GatedStream(Utf8(Example1Jcal));
            var reader = new JcalReader(source);
            var read = reader.ReadCalendarAsync().AsTask();
            await source.ReadStarted;
            reader.Dispose();
            source.OpenGate();
            await Record.ExceptionAsync(() => read);

            // act
            Action testAction = () => reader.ReadCalendarAsync();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task ReadCalendar_AfterDisposeWhileReadInProgress_ThrowsObjectDisposed()
        {
            // arrange
            var source = new GatedStream(Utf8(Example1Jcal));
            var reader = new JcalReader(source);
            var read = reader.ReadCalendarAsync().AsTask();
            await source.ReadStarted;
            reader.Dispose();
            source.OpenGate();
            await Record.ExceptionAsync(() => read);

            // act
            Action testAction = () => reader.ReadCalendar();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task ReadCalendarAsync_DisposedWhileInProgress_ThrowsObjectDisposed()
        {
            // arrange
            var source = new GatedStream(Utf8(Example1Jcal));
            var reader = new JcalReader(source);
            var read = reader.ReadCalendarAsync().AsTask();
            await source.ReadStarted;
            reader.Dispose();
            source.OpenGate();

            // act
            Func<Task> testAction = () => read;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task ReadCalendarAsync_SecondReadWhileInProgress_ThrowsInvalidOperation()
        {
            // arrange
            var source = new GatedStream(Utf8(Example1Jcal));
            var reader = new JcalReader(source);
            var read = reader.ReadCalendarAsync().AsTask();
            await source.ReadStarted;
            await AsyncDisposal.FinallyAsync(() =>
            {
                // act
                Action testAction = () => reader.ReadCalendarAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("Another read*");
            }, async () =>
            {
                source.OpenGate();
                await read;
                await reader.DisposeAsync();
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_SecondReadRejectedWhileInProgress_ReadsTheCalendar()
        {
            // arrange
            var source = new GatedStream(Utf8(Example1Jcal));
            var reader = new JcalReader(source);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                var read = reader.ReadCalendarAsync().AsTask();
                await source.ReadStarted;
                Record.Exception(() => reader.ReadCalendarAsync());
                source.OpenGate();

                // act
                var testResult = await read;

                // assert
                testResult.Should().NotBeNull();
            });
        }

        [Fact]
        public async Task WriteCalendarAsync_Stream_WritesWithoutSynchronousIo()
        {
            // arrange
            var expected = WriteJcal(ReadCalendars(Example1Ical + Example2Ical));
            var stream = new AsyncOnlyWriteStream();
            var writer = new JcalWriter(stream);
            await writer.WriteCalendarAsync(ReadCalendar(Example1Ical));
            await AsyncDisposal.FinallyAsync(async () =>
            {
                // act
                await writer.WriteCalendarAsync(ReadCalendar(Example2Ical));
            }, async () =>
            {
                await writer.FlushAsync();
                await writer.DisposeAsync();
            });

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
            stream.FlushCount.Should().Be(2);
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_CountsEach()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical + Example2Ical);
            var writer = new JcalWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(calendars));

                // assert
                writer.CalendarsWritten.Should().Be(2);
            });
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_WritesEach()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical + Example2Ical);
            var expected = WriteJcal(calendars);
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(calendars));
            });

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
        }

        [Fact]
        public async Task WriteCalendarAsync_Canceled_ThrowsOperationCanceled()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new JcalWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> testAction = () => writer.WriteCalendarAsync(calendar, source.Token).AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Fact]
        public async Task WriteCalendar_AfterCancellation_ThrowsInvalidOperation()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new JcalWriter(new MemoryStream());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);
                    await Record.ExceptionAsync(() => writer.WriteCalendarAsync(calendar, source.Token).AsTask());

                    // act
                    Action testAction = () => writer.WriteCalendar(calendar);

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
                }
            });
        }

        [Fact]
        public async Task WriteCalendarAsync_AfterDisposeStartedWhileFlushInProgress_ThrowsObjectDisposed()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var stream = new GatedWriteStream();
            var writer = new JcalWriter(stream);
            await writer.WriteCalendarAsync(calendar);
            var flush = writer.FlushAsync();
            await stream.FlushStarted;
            var dispose = writer.DisposeAsync();
            await AsyncDisposal.FinallyAsync(() =>
            {
                // act
                Action testAction = () => writer.WriteCalendarAsync(calendar);

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }, async () =>
            {
                stream.OpenGate();
                await flush;
                await dispose;
            });
        }

        [Fact]
        public async Task DisposeAsync_WhileFlushInProgress_EndsDocumentAfterFlush()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var expected = WriteJcal(new[] { calendar });
            var stream = new GatedWriteStream();
            var writer = new JcalWriter(stream);
            await writer.WriteCalendarAsync(calendar);
            var flush = writer.FlushAsync();
            await stream.FlushStarted;
            var dispose = writer.DisposeAsync();
            stream.OpenGate();
            await flush;

            // act
            await dispose;

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
        }
    }
}
