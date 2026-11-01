using System;
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
        // The number of characters that AsyncOnlyTextReader gives each read, so that reads end at many different points.
        private const int CharactersPerRead = 7;

        private const string AsyncMessage = "*Async*";
        private const string CanceledMessage = "*canceled*";

        // A document whose SUMMARY has a second value, with the line and position of that value.
        private const long SecondValueLineNumber = 1;
        private const long SecondValueLinePosition = 332;

        private static readonly string SecondValueDocument = JournalDocument("<summary><text>a</text><text>b</text></summary>");

        [Theory]
        [MemberData(nameof(XcalRoundTripTests.CalendarNames), MemberType = typeof(XcalRoundTripTests))]
        public async Task ReadCalendarsAsync_EveryFixture_MatchesSync(string calendarName)
        {
            // arrange
            var (text, options) = XcalRoundTripTests.GetCalendar(calendarName);
            var xcal = WriteXcal(ReadCalendars(text, options), options);
            using (var syncReader = new XcalReader(new StringReader(xcal), options ?? new IcalOptions()))
            {
                var expected = Model(syncReader.ReadCalendars().ToList(), options);
                var asyncReader = new XcalReader(new AsyncOnlyTextReader(xcal, CharactersPerRead), options ?? new IcalOptions());
                try
                {
                    // act
                    var testResult = Model(await AsyncEnumerables.ToListAsync(asyncReader.ReadCalendarsAsync()), options);

                    // assert
                    testResult.Should().Equal(expected);
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
            // arrange: the synchronous calendars are written to a StringWriter, which declares UTF-16.
            var (text, options) = XcalRoundTripTests.GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = WriteXcal(calendars, options).Replace("utf-16", "utf-8");
            var stream = new AsyncOnlyWriteStream();
            var progress = new RecordingProgress();
            var writer = new XcalWriter(stream, options ?? new IcalOptions());
            try
            {
                // act
                await writer.WriteCalendarsAsync(calendars, progress);
            }
            finally
            {
                await writer.DisposeAsync();
            }

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
            progress.Reports.Should().HaveCount(calendars.Count);
        }

        [Fact]
        public async Task ReadCalendarAsync_Stream_ReadsWithoutSynchronousIo()
        {
            // arrange
            var expected = Model(ReadCalendars(Example2Ical));
            var reader = new XcalReader(new AsyncOnlyStream(Utf8(Example2Xcal)));
            try
            {
                // act
                var testResult = Model(new[] { await reader.ReadCalendarAsync() });

                // assert
                testResult.Should().Equal(expected);
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterTheLastCalendarFromStream_ReturnsNull()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyStream(Utf8(Example2Xcal)));
            try
            {
                await reader.ReadCalendarAsync();

                // act
                var testResult = await reader.ReadCalendarAsync();

                // assert
                testResult.Should().BeNull();
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
                var testResult = reader.ReadCalendarAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                testResult.Result.Should().NotBeNull();
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
            var syncException = Record.Exception(() => ReadXcal(SecondValueDocument));
            var reader = new XcalReader(new AsyncOnlyTextReader(SecondValueDocument, CharactersPerRead));
            try
            {
                // act
                Func<Task> testAction = () => reader.ReadCalendarAsync().AsTask();

                // assert
                var exception = (await testAction.Should().ThrowAsync<IcalFileException>()).Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(SecondValueLineNumber);
                exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be(SecondValueLinePosition);
                exception.Message.Should().Be(syncException.Message);
                exception.Data.Should().BeEquivalentTo(syncException.Data);
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterNotValid_ThrowsInvalidOperation()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyTextReader(SecondValueDocument, CharactersPerRead));
            try
            {
                await Record.ExceptionAsync(() => reader.ReadCalendarAsync().AsTask());

                // act
                Func<Task> testAction = () => reader.ReadCalendarAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_Canceled_ThrowsOperationCanceled()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyTextReader(Example1Xcal, CharactersPerRead));
            try
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> testAction = () => reader.ReadCalendarAsync(source.Token).AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendar_AfterCancellation_ThrowsInvalidOperation()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyTextReader(Example1Xcal, CharactersPerRead));
            try
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
                    Action testAction = () => reader.ReadCalendarAsync();

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(AsyncMessage);
                }
                finally
                {
                    await reader.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ReadCalendar_SyncOnlyXmlReaderAfterAsyncReadRejected_ReadsTheCalendar()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader(Example1Xcal)))
            {
                var reader = new XcalReader(xmlReader);
                try
                {
                    Record.Exception(() => reader.ReadCalendarAsync());

                    // act
                    var testResult = reader.ReadCalendar();

                    // assert
                    testResult.Should().NotBeNull();
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
            var expected = Model(ReadCalendars(Example1Ical));
            using (var xmlReader = CreateAsyncXmlReader(new AsyncOnlyTextReader(Example1Xcal, CharactersPerRead)))
            {
                var reader = new XcalReader(xmlReader);
                try
                {
                    // act
                    var testResult = Model(new[] { await reader.ReadCalendarAsync() });

                    // assert
                    testResult.Should().Equal(expected);
                }
                finally
                {
                    await reader.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterDisposeWhileReadInProgress_ThrowsObjectDisposed()
        {
            // arrange
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
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
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
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
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
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
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
            var read = reader.ReadCalendarAsync().AsTask();
            await source.ReadStarted;
            try
            {
                // act
                Action testAction = () => reader.ReadCalendarAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("Another read*");
            }
            finally
            {
                source.OpenGate();
                await read;
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_SecondReadRejectedWhileInProgress_ReadsTheCalendar()
        {
            // arrange
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
            try
            {
                var read = reader.ReadCalendarAsync().AsTask();
                await source.ReadStarted;
                Record.Exception(() => reader.ReadCalendarAsync());
                source.OpenGate();

                // act
                var testResult = await read;

                // assert
                testResult.Should().NotBeNull();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteCalendarAsync_TextWriter_WritesWithoutSynchronousIo()
        {
            // arrange: the synchronous calendar is written to a StringWriter, which declares UTF-16.
            var calendar = ReadCalendar(Example1Ical);
            var expected = WriteXcal(new[] { calendar }).Replace("utf-16", "utf-8");
            var target = new AsyncOnlyTextWriter();
            var writer = new XcalWriter(target);
            try
            {
                // act
                await writer.WriteCalendarAsync(calendar);
            }
            finally
            {
                await writer.FlushAsync();
                await writer.DisposeAsync();
            }

            // assert
            target.ToString().Should().Be(expected);
            target.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_CountsEach()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical + Example2Ical);
            var writer = new XcalWriter(new StringWriter());
            try
            {
                // act
                await writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(calendars));

                // assert
                writer.CalendarsWritten.Should().Be(2);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_WritesEach()
        {
            // arrange
            var calendars = ReadCalendars(Example1Ical + Example2Ical);
            var expected = WriteXcal(calendars);
            var text = new StringWriter();
            var writer = new XcalWriter(text);
            try
            {
                // act
                await writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(calendars));
            }
            finally
            {
                await writer.DisposeAsync();
            }

            // assert
            text.ToString().Should().Be(expected);
        }

        [Fact]
        public async Task WriteCalendarAsync_Canceled_ThrowsOperationCanceled()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new XcalWriter(new StringWriter());
            try
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> testAction = () => writer.WriteCalendarAsync(calendar, source.Token).AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteCalendar_AfterCancellation_ThrowsInvalidOperation()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new XcalWriter(new StringWriter());
            try
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
            var calendar = ReadCalendar(Example1Ical);
            var writer = new XcalWriter(XmlWriter.Create(new StringWriter()));
            try
            {
                // act
                Action testAction = () => writer.WriteCalendarAsync(calendar);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(AsyncMessage);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteCalendar_SyncOnlyXmlWriterAfterAsyncWriteRejected_DoesNotThrow()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new XcalWriter(XmlWriter.Create(new StringWriter()));
            try
            {
                Record.Exception(() => writer.WriteCalendarAsync(calendar));

                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                testAction.Should().NotThrow();
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteCalendarAsync_AfterDisposeStartedWhileFlushInProgress_ThrowsObjectDisposed()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var target = new GatedTextWriter();
            var writer = new XcalWriter(target);
            await writer.WriteCalendarAsync(calendar);
            var flush = writer.FlushAsync();
            await target.WriteStarted;
            var dispose = writer.DisposeAsync();
            try
            {
                // act
                Action testAction = () => writer.WriteCalendarAsync(calendar);

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
            finally
            {
                target.OpenGate();
                await flush;
                await dispose;
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
            var dispose = writer.DisposeAsync();
            target.OpenGate();
            await flush;

            // act
            await dispose;

            // assert
            var document = System.Xml.Linq.XDocument.Parse(target.ToString());
            document.Root.Name.LocalName.Should().Be("icalendar");
            document.Root.Elements().Should().ContainSingle();
        }
    }
}
