using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalLines;
using static DataStandardizer.File.iCal.Tests.ModelTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of <see cref="IcalWriter"/>: round trips, VERSION, conformance checks, progress, and the asynchronous members.
    /// </summary>
    public class IcalWriterTests
    {
        private const string CanceledMessage = "*canceled*";
        private const string OperationInProgressMessage = "Another operation is in progress.*";

        // The calendar that WriteCalendar_BuiltInCode_WritesExpectedLines builds, as the writer writes it.
        private const string BuiltInCodeCalendar =
            "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Example//Test//EN\r\nBEGIN:VEVENT\r\nUID:1@example.com\r\nDTSTAMP:20261015T090000Z\r\n" +
            "DTSTART;VALUE=DATE:20261225\r\nSUMMARY:Christmas\\; holiday\\, family\r\nRRULE:FREQ=YEARLY\r\nBEGIN:VALARM\r\nACTION:DISPLAY\r\n" +
            "DESCRIPTION:Tomorrow\r\nTRIGGER:-P1D\r\nEND:VALARM\r\nEND:VEVENT\r\nEND:VCALENDAR\r\n";

        // The start of ConformanceCases.MissingVersionCase as the writer writes it, with the VERSION it adds.
        private const string MissingVersionStart = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nPRODID:-//Test//Test//EN\r\n";

        // The start of RfcFixtures.Rfc5545Event with a VERSION range, as the writer writes it.
        private const string VersionRangeStart = "BEGIN:VCALENDAR\r\nPRODID:-//xyz Corp//NONSGML PDA Calendar Version 1.0//EN\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\n";

        // The numbers of content lines of CalendarFixtures.GoogleExport and RfcFixtures.Rfc5545Event, and of the components of each.
        private const long GoogleExportLineCount = 60;
        private const long GoogleExportAndRfc5545EventLineCount = 75;
        private const int GoogleExportComponentCount = 6;
        private const int GoogleExportAndRfc5545EventComponentCount = 7;

        // The numbers of calendars and components of CalendarFixtures.GoogleExport and CalendarFixtures.OutlookExport.
        private const int GoogleAndOutlookComponentCount = 11;

        // CalendarFixtures.GoogleExport followed by CalendarFixtures.OutlookExport, and its name in FixtureExpectations.
        private const string GoogleThenOutlookExportName = "GoogleExportThenOutlookExport";
        private static readonly string GoogleThenOutlookExport = CalendarFixtures.GoogleExport + CalendarFixtures.OutlookExport;

        // CalendarFixtures.GoogleExport followed by RfcFixtures.Rfc5545Event.
        private static readonly string GoogleExportThenRfc5545Event = CalendarFixtures.GoogleExport + RfcFixtures.Rfc5545Event;

        public static TheoryData<string, string> VersionRanges()
        {
            var data = new TheoryData<string, string>();
            foreach (var version in new[] { "1.0;2.0", "2.0;3.0" })
            {
                data.Add(version, RfcFixtures.Rfc5545Event.Replace(Version20, "VERSION:" + version));
            }

            return data;
        }

        // The conformance cases that the writer rejects: all but the missing VERSION, which the writer supplies itself.
        public static TheoryData<string> RejectedConformanceCases()
        {
            var data = new TheoryData<string>();
            foreach (var name in ConformanceCases.All.Keys)
            {
                if (name != ConformanceCases.MissingVersionCase)
                {
                    data.Add(name);
                }
            }

            return data;
        }

        public static TheoryData<Func<IcalWriter, object>> DisposedWriterMembers()
        {
            var calendar = new IcalCalendar();
            return new TheoryData<Func<IcalWriter, object>>
            {
                writer => { writer.WriteCalendar(calendar); return null; },
                writer => { writer.WriteCalendars(new[] { calendar }); return null; },
                writer => writer.WriteCalendarAsync(calendar),
                writer => writer.WriteCalendarsAsync(new[] { calendar }),
                writer => writer.WriteCalendarsAsync(ToAsync(new[] { calendar })),
                writer => { writer.Flush(); return null; },
                writer => writer.FlushAsync(),
                writer => writer.CalendarsWritten,
                writer => writer.ComponentsWritten,
                writer => writer.LinesWritten,
            };
        }

        #region Round trips

        [Theory]
        [MemberData(nameof(CalendarFixtures.AllNames), MemberType = typeof(CalendarFixtures))]
        public void WriteCalendars_EveryFixture_RoundTripsContentLines(string fixtureName)
        {
            // arrange
            var (text, options) = CalendarFixtures.All[fixtureName];
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.Models[fixtureName];

            // act
            var testResult = XcalTestData.Model(calendars, options);

            // assert
            testResult.Should().Equal(expected);
        }

        [Theory]
        [MemberData(nameof(CalendarFixtures.AllNames), MemberType = typeof(CalendarFixtures))]
        public void ReadCalendars_EveryFixtureWrittenBack_HasEveryCalendar(string fixtureName)
        {
            // arrange
            var (text, options) = CalendarFixtures.All[fixtureName];
            var calendars = ReadCalendars(text, options);

            // act
            var testResult = ReadCalendars(WriteCalendars(calendars, options), options);

            // assert
            testResult.Should().HaveCount(FixtureExpectations.Counts[fixtureName].Calendars);
        }

        [Theory]
        [MemberData(nameof(CalendarFixtures.AllNames), MemberType = typeof(CalendarFixtures))]
        public void WriteCalendars_EveryFixture_WritesItFoldedAt75Octets(string fixtureName)
        {
            // arrange
            var (text, options) = CalendarFixtures.All[fixtureName];
            var calendars = ReadCalendars(text, options);

            // act
            var testResult = WriteCalendars(calendars, options);

            // assert
            testResult.Should().Be(FixtureExpectations.WrittenTexts[fixtureName]);
        }

        [Fact]
        public void WriteCalendars_ParametersAsRead_AreKeptExactly()
        {
            // arrange
            var text = EventCalendar("ATTENDEE;CN=\"Doe, Jane\";X-Q=\"plain\";X-C=a^'b:mailto:j@example.com");
            var calendars = ReadCalendars(text);

            // act
            var testResult = WriteCalendars(calendars);

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
                RecurrenceRules = new[] { new IcalRecurrenceRule(IcalRecurrenceFrequency.Yearly) }
            };
            testEvent.Components.Add(new IcalAlarm { Action = "DISPLAY", Description = "Tomorrow", TriggerDuration = new IcalDuration(-1, TimeSpan.Zero) });
            calendar.Components.Add(testEvent);
            var text = new StringWriter();
            using (var writer = new IcalWriter(text))
            {
                // act
                writer.WriteCalendar(calendar);

                // assert
                text.ToString().Should().Be(BuiltInCodeCalendar);
                writer.CalendarsWritten.Should().Be(1);
                writer.ComponentsWritten.Should().Be(2);
                writer.LinesWritten.Should().Be(16);
            }
        }

        #endregion

        #region VERSION

        [Fact]
        public void WriteCalendar_NoVersion_WritesVersion20First()
        {
            // arrange
            var calendar = ReadCalendar(ConformanceCases.All[ConformanceCases.MissingVersionCase].Text,
                new IcalOptions { AllowMissingRequiredProperties = true });

            // act
            var testResult = WriteCalendars(new[] { calendar });

            // assert
            testResult.Should().StartWith(MissingVersionStart);
        }

        [Fact]
        public void Validate_CalendarReadWithoutVersion_ReportsVersion()
        {
            // arrange
            var calendar = ReadCalendar(ConformanceCases.All[ConformanceCases.MissingVersionCase].Text,
                new IcalOptions { AllowMissingRequiredProperties = true });

            // act
            var testResult = calendar.Validate();

            // assert
            testResult.Should().ContainSingle().Which.PropertyName.Should().Be("VERSION");
        }

        [Theory]
        [MemberData(nameof(VersionRanges))]
        public void WriteCalendar_VersionRangeIncluding2_WritesVersion20InPlace(string version, string text)
        {
            // arrange
            _ = version;
            var calendar = ReadCalendar(text);

            // act
            var testResult = WriteCalendars(new[] { calendar });

            // assert
            testResult.Should().StartWith(VersionRangeStart);
        }

        [Fact]
        public void WriteCalendar_UnsupportedVersion_Throws()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            calendar.Version = "1.0";
            var text = new StringWriter();
            using (var writer = new IcalWriter(text))
            {
                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                testAction.Should().Throw<IcalFileException>().Which.Data[IcalFileException.DataItemName.DiagnosticKind].Should()
                    .Be(IcalDiagnosticKind.UnsupportedVersion);
                text.ToString().Should().BeEmpty();
            }
        }

        #endregion

        #region Conformance

        [Theory]
        [MemberData(nameof(RejectedConformanceCases))]
        public void WriteCalendar_NonConformantWithDefaults_ThrowsAndWritesNothing(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];
            var calendar = ReadCalendar(testCase.Text, testCase.AcceptingOptions);
            var text = new StringWriter();
            using (var writer = new IcalWriter(text))
            {
                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                var exception = testAction.Should().Throw<IcalFileException>().Which;
                exception.Data[IcalFileException.DataItemName.DiagnosticKind].Should().Be(testCase.Kind);
                exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(testCase.ComponentPath);
                text.ToString().Should().BeEmpty();
                writer.CalendarsWritten.Should().Be(0);
            }
        }

        [Fact]
        public void WriteCalendar_MissingVersionWithDefaults_SuppliesTheVersion()
        {
            // arrange
            var testCase = ConformanceCases.All[ConformanceCases.MissingVersionCase];
            var calendar = ReadCalendar(testCase.Text, testCase.AcceptingOptions);
            using (var writer = new IcalWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                testAction.Should().NotThrow();
            }
        }

        [Theory]
        [MemberData(nameof(RejectedConformanceCases))]
        public void Validate_NonConformantCalendarWrittenWithItsOption_StillReportsTheProblem(string caseName)
        {
            // arrange
            var testCase = ConformanceCases.All[caseName];
            var options = testCase.AcceptingOptions;
            var calendar = ReadCalendar(testCase.Text, options);

            // act
            var testResult = ReadCalendar(WriteCalendars(new[] { calendar }, options), options).Validate();

            // assert
            testResult.Should().ContainSingle();
        }

        [Fact]
        public void Validate_MissingVersionCalendarWrittenWithItsOption_ReportsNothing()
        {
            // arrange
            var testCase = ConformanceCases.All[ConformanceCases.MissingVersionCase];
            var options = testCase.AcceptingOptions;
            var calendar = ReadCalendar(testCase.Text, options);

            // act
            var testResult = ReadCalendar(WriteCalendars(new[] { calendar }, options), options).Validate();

            // assert
            testResult.Should().BeEmpty();
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
            Action testAction = () => WriteCalendars(new[] { calendar });

            // assert
            var exception = testAction.Should().Throw<IcalFileException>().Which;
            exception.Message.Should().Be("The DTSTAMP property must be a DATE-TIME in UTC.");
            exception.Data[IcalFileException.DataItemName.ComponentPath].Should().Be(EventPath);
            exception.Data.Contains(IcalFileException.DataItemName.LineNumber).Should().BeFalse();
        }

        #endregion

        #region Bulk writes and progress

        [Fact]
        public void WriteCalendars_Progress_ReportedAfterEachCalendar()
        {
            // arrange
            var calendars = ReadCalendars(GoogleExportThenRfc5545Event);
            var progress = new RecordingProgress();
            using (var writer = new IcalWriter(new StringWriter()))
            {
                // act
                writer.WriteCalendars(calendars, progress);

                // assert
                progress.Reports.Should().Equal(new IcalWriteProgress(GoogleExportLineCount, GoogleExportComponentCount, 1),
                    new IcalWriteProgress(GoogleExportAndRfc5545EventLineCount, GoogleExportAndRfc5545EventComponentCount, 2));
                writer.LinesWritten.Should().Be(GoogleExportAndRfc5545EventLineCount);
            }
        }

        [Fact]
        public void WriteCalendars_NullCalendar_Throws()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            using (var writer = new IcalWriter(new StringWriter()))
            {
                // act
                Action testAction = () => writer.WriteCalendars(new[] { calendar, null });

                // assert
                testAction.Should().Throw<ArgumentException>().Which.ParamName.Should().Be("calendars");
            }
        }

        [Fact]
        public void WriteCalendars_NullCalendar_WritesTheCalendarsBeforeIt()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var expected = FixtureExpectations.WrittenTexts[nameof(RfcFixtures.Rfc5545Event)];
            var text = new StringWriter();
            using (var writer = new IcalWriter(text))
            {
                // act
                Record.Exception(() => writer.WriteCalendars(new[] { calendar, null }));

                // assert
                text.ToString().Should().Be(expected);
            }
        }

        [Fact]
        public void WriteCalendars_ProgressCallsWriter_Throws()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var writer = new IcalWriter(new StringWriter());
            var progress = new RecordingProgress(_ => writer.WriteCalendar(calendar));

            // act
            Action testAction = () => writer.WriteCalendars(new[] { calendar }, progress);

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(OperationInProgressMessage);
            writer.CalendarsWritten.Should().Be(1);
        }

        #endregion

        #region Asynchronous

        [Theory]
        [MemberData(nameof(CalendarFixtures.AllNames), MemberType = typeof(CalendarFixtures))]
        public async Task WriteCalendarsAsync_EveryFixture_WritesItFoldedAt75Octets(string fixtureName)
        {
            // arrange
            var (text, options) = CalendarFixtures.All[fixtureName];
            var calendars = ReadCalendars(text, options);
            var expected = FixtureExpectations.WrittenTexts[fixtureName];
            var target = new AsyncOnlyTextWriter();
            var progress = new RecordingProgress();
            var writer = new IcalWriter(target, options);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(calendars, progress);
            });

            // assert
            target.ToString().Should().Be(expected);
            progress.Reports.Should().HaveCount(FixtureExpectations.Counts[fixtureName].Calendars);
        }

        [Fact]
        public async Task WriteCalendarsAsync_AsyncEnumerable_WritesEachFoldedAt75Octets()
        {
            // arrange
            var calendars = ReadCalendars(GoogleThenOutlookExport);
            var expected = FixtureExpectations.WrittenTexts[GoogleThenOutlookExportName];
            var target = new AsyncOnlyTextWriter();
            var writer = new IcalWriter(target);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarsAsync(ToAsync(calendars));

                // assert
                target.ToString().Should().Be(expected);
                writer.CalendarsWritten.Should().Be(2);
                writer.ComponentsWritten.Should().Be(GoogleAndOutlookComponentCount);
            });
        }

        [Fact]
        public async Task WriteCalendarAsync_OneCalendar_WritesItFoldedAt75Octets()
        {
            // arrange
            var calendar = ReadCalendar(CalendarFixtures.GoogleExport);
            var expected = FixtureExpectations.WrittenTexts[nameof(CalendarFixtures.GoogleExport)];
            var target = new AsyncOnlyTextWriter();
            var writer = new IcalWriter(target);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.WriteCalendarAsync(calendar);

                // assert
                target.ToString().Should().Be(expected);
                writer.CalendarsWritten.Should().Be(1);
                writer.ComponentsWritten.Should().Be(GoogleExportComponentCount);
            });
        }

        [Fact]
        public async Task WriteCalendarAsync_NonConformant_FaultsTask()
        {
            // arrange
            var testCase = ConformanceCases.All[ConformanceCases.TwoSummariesCase];
            var calendar = ReadCalendar(testCase.Text, testCase.AcceptingOptions);
            var writer = new IcalWriter(new AsyncOnlyTextWriter());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                Func<Task> testAction = () => writer.WriteCalendarAsync(calendar).AsTask();

                // assert
                await testAction.Should().ThrowAsync<IcalFileException>();
            });
        }

        [Fact]
        public async Task WriteCalendarAsync_AfterNonConformantCalendar_WritesTheNextCalendar()
        {
            // arrange
            var testCase = ConformanceCases.All[ConformanceCases.TwoSummariesCase];
            var nonConformant = ReadCalendar(testCase.Text, testCase.AcceptingOptions);
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var expected = FixtureExpectations.WrittenTexts[nameof(RfcFixtures.Rfc5545Event)];
            var target = new AsyncOnlyTextWriter();
            var writer = new IcalWriter(target);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await Record.ExceptionAsync(() => writer.WriteCalendarAsync(nonConformant).AsTask());

                // act
                await writer.WriteCalendarAsync(calendar);

                // assert
                target.ToString().Should().Be(expected);
            });
        }

        [Fact]
        public async Task WriteCalendarsAsync_Canceled_ThrowsOperationCanceled()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var writer = new IcalWriter(new AsyncOnlyTextWriter());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> testAction = () => writer.WriteCalendarsAsync(new[] { calendar }, cancellationToken: source.Token);

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                    writer.CalendarsWritten.Should().Be(0);
                }
            });
        }

        [Fact]
        public async Task WriteCalendarsAsync_AfterCancellation_ThrowsInvalidOperation()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var writer = new IcalWriter(new AsyncOnlyTextWriter());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    await TestTasks.CancelAsync(source);
                    await Record.ExceptionAsync(() => writer.WriteCalendarsAsync(new[] { calendar }, cancellationToken: source.Token));

                    // act
                    Action testAction = () => writer.WriteCalendarsAsync(new[] { calendar });

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
                }
            });
        }

        // On .NET Framework, TextWriter.WriteAsync takes no cancellation token, so a write in progress cannot be canceled.
#if NETCOREAPP2_1_OR_GREATER
        [Fact]
        public async Task WriteCalendarAsync_CanceledMidCalendar_ThrowsOperationCanceled()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var target = new GatedTextWriter();
            var writer = new IcalWriter(target);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    var pending = writer.WriteCalendarAsync(calendar, source.Token);
                    await target.WriteStarted;
                    await TestTasks.CancelAsync(source);

                    // act
                    Func<Task> testAction = () => pending.AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Fact]
        public async Task WriteCalendar_AfterCancellationMidCalendar_ThrowsInvalidOperation()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var target = new GatedTextWriter();
            var writer = new IcalWriter(target);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                using (var source = new CancellationTokenSource())
                {
                    var pending = writer.WriteCalendarAsync(calendar, source.Token);
                    await target.WriteStarted;
                    await TestTasks.CancelAsync(source);
                    await Record.ExceptionAsync(() => pending.AsTask());

                    // act
                    Action testAction = () => writer.WriteCalendar(calendar);

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
                }
            });
        }
#endif

        [Fact]
        public async Task WriteCalendar_WhileAsyncWriteInProgress_Throws()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var target = new GatedTextWriter();
            var writer = new IcalWriter(target);
            var pending = writer.WriteCalendarAsync(calendar);
            await target.WriteStarted;
            await AsyncDisposal.FinallyAsync(() =>
            {
                // act
                Action testAction = () => writer.WriteCalendar(calendar);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(OperationInProgressMessage);
            }, async () =>
            {
                target.OpenGate();
                await pending;
                await writer.DisposeAsync();
            });
        }

        [Fact]
        public async Task WriteCalendarAsync_SyncWriteRejectedWhileInProgress_WritesTheCalendar()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var expected = FixtureExpectations.WrittenTexts[nameof(RfcFixtures.Rfc5545Event)];
            var target = new GatedTextWriter();
            var writer = new IcalWriter(target);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var pending = writer.WriteCalendarAsync(calendar);
                await target.WriteStarted;
                Record.Exception(() => writer.WriteCalendar(calendar));
                target.OpenGate();

                // act
                await pending;

                // assert
                target.ToString().Should().Be(expected);
            });
        }

        #endregion

        #region Disposal

        [Theory]
        [MemberData(nameof(DisposedWriterMembers), DisableDiscoveryEnumeration = true)]
        public void Member_AfterDispose_ThrowsObjectDisposedException(Func<IcalWriter, object> member)
        {
            // arrange
            var writer = new IcalWriter(new StringWriter());
            writer.Dispose();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_Twice_DoesNotThrow()
        {
            // arrange
            var writer = new IcalWriter(new StringWriter());
            writer.Dispose();

            // act
            Action testAction = () => writer.Dispose();

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public async Task DisposeAsync_StreamWriter_FlushesToStream()
        {
            // arrange
            var calendar = ReadCalendar(RfcFixtures.Rfc5545Event);
            var expected = FixtureExpectations.WrittenTexts[nameof(RfcFixtures.Rfc5545Event)];
            var stream = new MemoryStream();
            var writer = new IcalWriter(stream);
            await writer.WriteCalendarAsync(calendar);

            // act
            await writer.DisposeAsync();

            // assert
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected);
        }

        [Fact]
        public async Task DisposeAsync_Twice_DoesNotThrow()
        {
            // arrange
            var writer = new IcalWriter(new MemoryStream());
            await writer.DisposeAsync();

            // act
            Func<Task> testAction = () => writer.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        #endregion

        private static IAsyncEnumerable<IcalCalendar> ToAsync(IEnumerable<IcalCalendar> calendars)
        {
            return AsyncEnumerables.Yielding(calendars);
        }
    }
}
