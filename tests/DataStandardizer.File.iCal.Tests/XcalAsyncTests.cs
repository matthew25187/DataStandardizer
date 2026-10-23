using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.ModelTestData;
using static DataStandardizer.File.iCal.Tests.XcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="XcalReader"/> and <see cref="XcalWriter"/>.  The sources and targets used can only be
    /// read or written asynchronously, so these tests also show that the async paths do no synchronous I/O.
    /// </summary>
    public class XcalAsyncTests
    {
        [Theory]
        [MemberData(nameof(XcalRoundTripTests.CalendarNames), MemberType = typeof(XcalRoundTripTests))]
        public async Task ReadCalendarsAsync_EveryFixture_MatchesSync(string calendarName)
        {
            // arrange
            var (text, options) = XcalRoundTripTests.GetCalendar(calendarName);
            var xcal = WriteXcal(ReadCalendars(text, options), options);
            using (var syncReader = new XcalReader(new StringReader(xcal), options ?? new IcalOptions()))
            {
                var expected = syncReader.ReadCalendars().ToList();
                var asyncReader = new XcalReader(new AsyncOnlyTextReader(xcal, 7), options ?? new IcalOptions());
                try
                {
                    // act
                    var testResult = await AsyncEnumerables.ToListAsync(asyncReader.ReadCalendarsAsync());

                    // assert
                    Model(testResult, options).Should().Equal(Model(expected, options));
                    asyncReader.CalendarsRead.Should().Be(syncReader.CalendarsRead);
                    asyncReader.ComponentsRead.Should().Be(syncReader.ComponentsRead);
                    asyncReader.PropertiesRead.Should().Be(syncReader.PropertiesRead);
                }
                finally
                {
                    await asyncReader.DisposeAsync();
                }
            }
        }

        [Theory]
        [MemberData(nameof(XcalRoundTripTests.CalendarNames), MemberType = typeof(XcalRoundTripTests))]
        public async Task WriteCalendarsAsync_EveryFixture_MatchesSync(string calendarName)
        {
            // arrange
            var (text, options) = XcalRoundTripTests.GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = WriteXcal(calendars, options);
            var stream = new AsyncOnlyWriteStream();
            var progress = new RecordingProgress();

            // act
            var writer = new XcalWriter(stream, options ?? new IcalOptions());
            try
            {
                await writer.WriteCalendarsAsync(calendars, progress);
            }
            finally
            {
                await writer.DisposeAsync();
            }

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected.Replace("utf-16", "utf-8"));
            progress.Reports.Should().HaveCount(calendars.Count);
        }

        [Fact]
        public async Task ReadCalendarAsync_Stream_ReadsWithoutSynchronousIo()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyStream(Utf8(Example2Xcal)));
            try
            {
                // act
                var calendar = await reader.ReadCalendarAsync();
                var end = await reader.ReadCalendarAsync();

                // assert
                Model(new[] { calendar }).Should().Equal(Model(ReadCalendars(Example2Ical)));
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
            var reader = new XcalReader(new AsyncOnlyStream(Utf8(Example1Xcal), completeSynchronously: true));
            try
            {
                // act
                var task = reader.ReadCalendarAsync();

                // assert
                task.IsCompletedSuccessfully.Should().BeTrue();
                (await task).Should().NotBeNull();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_NotValid_ThrowsAsSync()
        {
            // arrange
            var xml = JournalDocument("<summary><text>a</text><text>b</text></summary>");
            var syncException = FluentActions.Invoking(() => ReadXcal(xml)).Should().Throw<IcalFileException>().Which;
            var reader = new XcalReader(new AsyncOnlyTextReader(xml, 7));
            try
            {
                // act
                Func<Task> act = () => reader.ReadCalendarAsync().AsTask();

                // assert
                var exception = (await act.Should().ThrowAsync<IcalFileException>()).Which;
                exception.Message.Should().Be(syncException.Message);
                exception.Data.Should().BeEquivalentTo(syncException.Data);
                await reader.Invoking(value => value.ReadCalendarAsync().AsTask()).Should().ThrowAsync<InvalidOperationException>();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_Canceled_FaultsReader()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyTextReader(Example1Xcal, 7));
            try
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> act = () => reader.ReadCalendarAsync(source.Token).AsTask();

                    // assert
                    await act.Should().ThrowAsync<OperationCanceledException>();
                    reader.Invoking(value => value.ReadCalendar()).Should().Throw<InvalidOperationException>();
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_SyncOnlyXmlReader_ThrowsInvalidOperation()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader(Example1Xcal)))
            {
                var reader = new XcalReader(xmlReader);
                try
                {
                    // act
                    Action act = () => reader.ReadCalendarAsync();

                    // assert
                    act.Should().Throw<InvalidOperationException>().WithMessage("*Async*");
                    reader.ReadCalendar().Should().NotBeNull();
                }
                finally
                {
                    await reader.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_AsyncXmlReader_Reads()
        {
            // arrange
            using (var xmlReader = CreateAsyncXmlReader(new AsyncOnlyTextReader(Example1Xcal, 7)))
            {
                var reader = new XcalReader(xmlReader);
                try
                {
                    // act
                    var testResult = await reader.ReadCalendarAsync();

                    // assert
                    Model(new[] { testResult }).Should().Equal(Model(ReadCalendars(Example1Ical)));
                }
                finally
                {
                    await reader.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_WhileReadInProgress_ThrowsAndDisposeWaits()
        {
            // arrange
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
            var read = reader.ReadCalendarAsync().AsTask();
            await source.ReadStarted;

            // act
            Action secondRead = () => reader.ReadCalendarAsync();
            Action syncRead = () => reader.ReadCalendar();
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
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
            try
            {
                var read = reader.ReadCalendarAsync().AsTask();
                await source.ReadStarted;

                // act
                Action secondRead = () => reader.ReadCalendarAsync();

                // assert
                secondRead.Should().Throw<InvalidOperationException>().WithMessage("Another read*");
                source.OpenGate();
                (await read).Should().NotBeNull();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteCalendarAsync_TextWriter_WritesWithoutSynchronousIo()
        {
            // arrange
            var target = new AsyncOnlyTextWriter();

            // act
            var writer = new XcalWriter(target);
            try
            {
                await writer.WriteCalendarAsync(ReadCalendar(Example1Ical));
                await writer.FlushAsync();
            }
            finally
            {
                await writer.DisposeAsync();
            }

            // assert
            Model(ReadXcal(target.ToString())).Should().Equal(Model(ReadCalendars(Example1Ical)));
            target.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_WritesEach()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical + Example2Ical);
            var text = new StringWriter();

            IAsyncEnumerable<IcalCalendar> Calendars()
            {
                return AsyncEnumerables.Yielding(calendars);
            }

            // act
            var writer = new XcalWriter(text);
            try
            {
                await writer.WriteCalendarsAsync(Calendars());
                writer.CalendarsWritten.Should().Be(2);
            }
            finally
            {
                await writer.DisposeAsync();
            }

            // assert
            text.ToString().Should().Be(WriteXcal(calendars));
        }

        [Fact]
        public async Task WriteCalendarAsync_Canceled_FaultsWriter()
        {
            // arrange
            var writer = new XcalWriter(new StringWriter());
            try
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);
                    var calendar = ReadCalendar(Example1Ical);

                    // act
                    Func<Task> act = () => writer.WriteCalendarAsync(calendar, source.Token).AsTask();

                    // assert
                    await act.Should().ThrowAsync<OperationCanceledException>();
                    writer.Invoking(value => value.WriteCalendar(calendar)).Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
                }
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteCalendarAsync_SyncOnlyXmlWriter_ThrowsInvalidOperation()
        {
            // arrange
            var text = new StringWriter();
            var xmlWriter = XmlWriter.Create(text);
            var writer = new XcalWriter(xmlWriter);
            try
            {
                var calendar = ReadCalendar(Example1Ical);

                // act
                Action act = () => writer.WriteCalendarAsync(calendar);

                // assert
                act.Should().Throw<InvalidOperationException>().WithMessage("*Async*");
                writer.WriteCalendar(calendar);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task DisposeAsync_WhileFlushInProgress_EndsDocumentAfterFlush()
        {
            // arrange
            var target = new GatedTextWriter();
            var writer = new XcalWriter(target);
            await writer.WriteCalendarAsync(ReadCalendar(Example1Ical));
            var flush = writer.FlushAsync();
            await target.WriteStarted;

            // act
            var dispose = writer.DisposeAsync();
            Action write = () => writer.WriteCalendarAsync(ReadCalendar(Example1Ical));
            target.OpenGate();
            await flush;
            await dispose;

            // assert
            write.Should().Throw<ObjectDisposedException>();
            var document = System.Xml.Linq.XDocument.Parse(target.ToString());
            document.Root.Name.LocalName.Should().Be("icalendar");
            document.Root.Elements().Should().ContainSingle();
        }
    }
}
