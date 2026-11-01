using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
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

        // The XML declaration of a document that declares UTF-8.
        private const string Utf8Declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";

        // A document of one calendar.
        private const int OneCalendar = 1;

        // The first and second examples of RFC 6321 Appendix B in iCalendar form, one after the other, and their name in FixtureExpectations.
        private const string Example1ThenExample2Name = "Example1IcalThenExample2Ical";
        private static readonly string Example1ThenExample2Ical = Example1Ical + Example2Ical;

        // ref. RFC 6321 Appendix B: each example in xCal form, with the content lines of its iCalendar form and how many components other than
        // VCALENDAR and how many properties it has.
        public static TheoryData<string, string[], int, int> RfcExamples => new TheoryData<string, string[], int, int>
        {
            { Example1Xcal, FixtureExpectations.XcalRoundTripModels[nameof(Example1Ical)], 1, 7 },
            { Example2Xcal, FixtureExpectations.XcalRoundTripModels[nameof(Example2Ical)], 5, 28 },
        };

        [Theory]
        [MemberData(nameof(RfcExamples), DisableDiscoveryEnumeration = true)]
        public async Task ReadCalendarsAsync_RfcExample_ReadsItsContentLines(string xcal, string[] expected, int componentCount, int propertyCount)
        {
            // arrange
            _ = (componentCount, propertyCount);
            var reader = new XcalReader(new AsyncOnlyTextReader(xcal, CharactersPerRead));
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
        public async Task ReadCalendarsAsync_RfcExample_CountsWhatItReads(string xcal, string[] model, int componentCount, int propertyCount)
        {
            // arrange
            _ = model;
            var reader = new XcalReader(new AsyncOnlyTextReader(xcal, CharactersPerRead));
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
        [MemberData(nameof(XcalRoundTripTests.CalendarNames), MemberType = typeof(XcalRoundTripTests))]
        public async Task WriteCalendarsAsync_EveryFixture_WritesXcalThatReadsBackAsTheFixture(string calendarName)
        {
            // arrange
            var (text, options) = XcalRoundTripTests.GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.XcalRoundTripModels[calendarName];
            var stream = new AsyncOnlyWriteStream();
            var writer = new XcalWriter(stream, options ?? new IcalOptions());

            // act
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await writer.WriteCalendarsAsync(calendars);
            });
            var testResult = Model(ReadXcal(Encoding.UTF8.GetString(stream.ToArray()), options), options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(XcalRoundTripTests.CalendarNames), MemberType = typeof(XcalRoundTripTests))]
        public async Task WriteCalendarsAsync_EveryFixture_ReportsProgressForEachCalendar(string calendarName)
        {
            // arrange
            var (text, options) = XcalRoundTripTests.GetCalendar(calendarName);
            var calendars = ReadCalendars(text, options);
            var progress = new RecordingProgress();
            var writer = new XcalWriter(new AsyncOnlyWriteStream(), options ?? new IcalOptions());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(calendars, progress);

                // assert
                progress.Reports.Should().ContainSingle();
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_Stream_ReadsWithoutSynchronousIo()
        {
            // arrange
            var expected = FixtureExpectations.XcalRoundTripModels[nameof(Example2Ical)];
            var reader = new XcalReader(new AsyncOnlyStream(Utf8(Example2Xcal)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = Model(new[] { await reader.ReadCalendarAsync() });

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterTheLastCalendarFromStream_ReturnsNull()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyStream(Utf8(Example2Xcal)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
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
            var reader = new XcalReader(new AsyncOnlyStream(Utf8(Example1Xcal), completeSynchronously: true));
            await AsyncDisposal.UsingAsync(reader, () =>
            {
                // act
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
            var reader = new XcalReader(new AsyncOnlyTextReader(SecondValueDocument, CharactersPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadCalendarAsync().AsTask();

                // assert
                var exception = (await testAction.Should().ThrowAsync<IcalFileException>()).Which;
                exception.Data[IcalFileException.DataItemName.LineNumber].Should().Be(SecondValueLineNumber);
                exception.Data[IcalFileException.DataItemName.LinePosition].Should().Be(SecondValueLinePosition);
            });
        }

        [Fact]
        public async Task ReadCalendarAsync_AfterNotValid_ThrowsInvalidOperation()
        {
            // arrange
            var reader = new XcalReader(new AsyncOnlyTextReader(SecondValueDocument, CharactersPerRead));
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
            var reader = new XcalReader(new AsyncOnlyTextReader(Example1Xcal, CharactersPerRead));
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
            var reader = new XcalReader(new AsyncOnlyTextReader(Example1Xcal, CharactersPerRead));
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
        public async Task ReadCalendarAsync_SyncOnlyXmlReader_ThrowsInvalidOperation()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader(Example1Xcal)))
            {
                var reader = new XcalReader(xmlReader);
                await AsyncDisposal.UsingAsync(reader, () =>
                {
                    // act
                    Action testAction = () => reader.ReadCalendarAsync();

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(AsyncMessage);
                });
            }
        }

        [Fact]
        public async Task ReadCalendar_SyncOnlyXmlReaderAfterAsyncReadRejected_ReadsTheCalendar()
        {
            // arrange
            using (var xmlReader = XmlReader.Create(new StringReader(Example1Xcal)))
            {
                var reader = new XcalReader(xmlReader);
                await AsyncDisposal.UsingAsync(reader, () =>
                {
                    Record.Exception(() => reader.ReadCalendarAsync());

                    // act
                    var testResult = reader.ReadCalendar();

                    // assert
                    testResult.Should().NotBeNull();
                });
            }
        }

        [Fact]
        public async Task ReadCalendarAsync_AsyncXmlReader_Reads()
        {
            // arrange
            var expected = FixtureExpectations.XcalRoundTripModels[nameof(Example1Ical)];
            using (var xmlReader = CreateAsyncXmlReader(new AsyncOnlyTextReader(Example1Xcal, CharactersPerRead)))
            {
                var reader = new XcalReader(xmlReader);
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    // act
                    var testResult = Model(new[] { await reader.ReadCalendarAsync() });

                    // assert
                    testResult.Should().Equal(expected);
                });
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
            var source = new GatedTextReader(Example1Xcal);
            var reader = new XcalReader(source);
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
        public async Task WriteCalendarAsync_TextWriter_WritesWithoutSynchronousIo()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var expected = XcalWriterTests.Normalize(XDocument.Parse(Example1Xcal));
            var target = new AsyncOnlyTextWriter();
            var writer = new XcalWriter(target);
            await AsyncDisposal.FinallyAsync(async () =>
            {
                // act
                await writer.WriteCalendarAsync(calendar);
            }, async () =>
            {
                await writer.FlushAsync();
                await writer.DisposeAsync();
            });

            // assert
            target.ToString().Should().StartWith(Utf8Declaration);
            XcalWriterTests.Normalize(XDocument.Parse(target.ToString())).Should().Be(expected);
            target.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_CountsEach()
        {
            // arrange
            var calendars = ReadCalendars(Example1ThenExample2Ical);
            var writer = new XcalWriter(new StringWriter());
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
            var expected = FixtureExpectations.XcalRoundTripModels[Example1ThenExample2Name];
            var text = new StringWriter();
            var writer = new XcalWriter(text);

            // act
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await writer.WriteCalendarsAsync(AsyncEnumerables.Yielding(calendars));
            });
            var testResult = Model(ReadXcal(text.ToString()));

            // assert
            testResult.Should().Equal(expected);
        }

        [Fact]
        public async Task WriteCalendarAsync_Canceled_ThrowsOperationCanceled()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new XcalWriter(new StringWriter());
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
            var writer = new XcalWriter(new StringWriter());
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
        public async Task WriteCalendarAsync_SyncOnlyXmlWriter_ThrowsInvalidOperation()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new XcalWriter(XmlWriter.Create(new StringWriter()));
            await AsyncDisposal.UsingAsync(writer, () =>
            {
                // act
                Action testAction = () => writer.WriteCalendarAsync(calendar);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(AsyncMessage);
            });
        }

        [Fact]
        public async Task WriteCalendar_SyncOnlyXmlWriterAfterAsyncWriteRejected_DoesNotThrow()
        {
            // arrange
            var calendar = ReadCalendar(Example1Ical);
            var writer = new XcalWriter(XmlWriter.Create(new StringWriter()));
            await AsyncDisposal.UsingAsync(writer, () =>
            {
                Record.Exception(() => writer.WriteCalendarAsync(calendar));

                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                testAction.Should().NotThrow();
            });
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
            await AsyncDisposal.FinallyAsync(() =>
            {
                // act
                Action testAction = () => writer.WriteCalendarAsync(calendar);

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }, async () =>
            {
                target.OpenGate();
                await flush;
                await dispose;
            });
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
