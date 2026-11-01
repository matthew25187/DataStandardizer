using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
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

        // The same two calendars as Example1ThenExample2, as a writer writes them, and in iCalendar form.
        private static readonly string Example1ThenExample2WithPeriodArray = "[" + Example1Jcal + "," + Example2JcalWithPeriodArray + "]";
        private static readonly string Example1ThenExample2Ical = Example1Ical + Example2Ical;

        // A document of one calendar.
        private const int OneCalendar = 1;

        // ref. RFC 7265 Appendix B: each example in jCal form, with the content lines of its iCalendar form and how many components other than
        // VCALENDAR and how many properties it has.
        public static TheoryData<string, string[], int, int> RfcExamples => new TheoryData<string, string[], int, int>
        {
            { Example1Jcal, FixtureExpectations.JcalRoundTripModels[nameof(Example1Ical)], 1, 7 },
            { Example2Jcal, FixtureExpectations.JcalRoundTripModels[nameof(Example2Ical)], 5, 28 },
        };

        [Theory]
        [MemberData(nameof(RfcExamples), DisableDiscoveryEnumeration = true)]
        public async Task ReadCalendarsAsync_RfcExample_ReadsItsContentLines(string jcal, string[] expected, int componentCount, int propertyCount)
        {
            // arrange
            _ = (componentCount, propertyCount);
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(jcal), maxReadLength: OctetsPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = Model(await AsyncEnumerables.ToListAsync(reader.ReadCalendarsAsync()));

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Theory]
        [MemberData(nameof(RfcExamples), DisableDiscoveryEnumeration = true)]
        public async Task ReadCalendarsAsync_RfcExample_CountsWhatItReads(string jcal, string[] model, int componentCount, int propertyCount)
        {
            // arrange
            _ = model;
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(jcal), maxReadLength: OctetsPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                await AsyncEnumerables.ToListAsync(reader.ReadCalendarsAsync());

                // assert
                reader.CalendarsRead.Should().Be(OneCalendar);
                reader.ComponentsRead.Should().Be(componentCount);
                reader.PropertiesRead.Should().Be(propertyCount);
            });
        }

        [Theory]
        [MemberData(nameof(JcalRoundTripTests.CalendarNames), MemberType = typeof(JcalRoundTripTests))]
        public async Task WriteCalendarsAsync_EveryFixture_WritesJcalThatReadsBackAsTheFixture(string calendarName)
        {
            // arrange
            var (text, options) = JcalRoundTripTests.GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.JcalRoundTripModels[calendarName];
            var stream = new AsyncOnlyWriteStream();
            var writer = new JcalWriter(stream, options ?? new IcalOptions());

            // act
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await writer.WriteCalendarsAsync(calendars);
            });
            var testResult = Model(ReadJcal(Encoding.UTF8.GetString(stream.ToArray()), options), options);

            // assert
            testResult.Should().Equal(expected);
            stream.IsDisposed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(JcalRoundTripTests.CalendarNames), MemberType = typeof(JcalRoundTripTests))]
        public async Task WriteCalendarsAsync_EveryFixture_ReportsProgressForEachCalendar(string calendarName)
        {
            // arrange
            var (text, options) = JcalRoundTripTests.GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var progress = new RecordingProgress();
            var writer = new JcalWriter(new AsyncOnlyWriteStream(), options ?? new IcalOptions());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(calendars, progress);

                // assert
                progress.Reports.Should().ContainSingle();
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_FirstOfSeveralCalendars_ReadsWithoutSynchronousIo()
        {
            // arrange
            var expected = FixtureExpectations.JcalRoundTripModels[nameof(Example1Ical)];
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
            var expected = FixtureExpectations.JcalRoundTripModels[nameof(Example2Ical)];
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
        public async Task ReadCalendarAsync_NotValid_ThrowsWithTheLocation()
        {
            // arrange
            var reader = new JcalReader(new AsyncOnlyStream(Utf8(SecondValueDocument), maxReadLength: OctetsPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadCalendarAsync().AsTask();

                // assert
                var exception = (await testAction.Should().ThrowAsync<IcalFileException>()).Which;
                exception.Data[IcalFileException.DataItemName.JsonPath].Should().Be(SecondValueJsonPath);
                exception.Data[IcalFileException.DataItemName.BytePosition].Should().Be(SecondValueBytePosition);
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
            var expected = JsonNode.Parse(Example1ThenExample2WithPeriodArray);
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
            JsonNode.DeepEquals(JsonNode.Parse(stream.ToArray()), expected).Should().BeTrue();
            stream.FlushCount.Should().Be(2);
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_CountsEach()
        {
            // arrange
            var calendars = ReadCalendars(Example1ThenExample2Ical);
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
            var calendars = ReadCalendars(Example1ThenExample2Ical);
            var expected = JsonNode.Parse(Example1ThenExample2WithPeriodArray);
            var stream = new MemoryStream();
            var writer = new JcalWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(calendars));
            });

            // assert
            JsonNode.DeepEquals(JsonNode.Parse(stream.ToArray()), expected).Should().BeTrue();
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
            var expected = JsonNode.Parse(Example1Jcal);
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
            JsonNode.DeepEquals(JsonNode.Parse(stream.ToArray()), expected).Should().BeTrue();
        }
    }
}
