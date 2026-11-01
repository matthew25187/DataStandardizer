using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="IcalContentLineWriter"/>.  The targets used can only be written asynchronously, so these
    /// tests also show that the async path does no synchronous I/O.
    /// </summary>
    public class IcalContentLineWriterAsyncTests
    {
        private const string LineAWithCrlf = "A:1\r\n";
        private const string CanceledMessage = "*canceled*";
        private const string DiskFullMessage = "Disk full.";
        private const string UnexpectedMessage = "Unexpected.";

        private static readonly IcalContentLine LineA = new IcalContentLine("A", "1");

        public enum AsyncWriteMode
        {
            WriteLineAsync,
            WriteLinesAsync,
            WriteLinesAsyncFromAsyncEnumerable
        }

        #region Sync and async parity

        public static TheoryData<string, AsyncWriteMode, int> ParityData()
        {
            var data = new TheoryData<string, AsyncWriteMode, int>();
            foreach (var fixtureName in RfcFixtures.All.Keys)
            {
                foreach (var mode in Enum.GetValues(typeof(AsyncWriteMode)).Cast<AsyncWriteMode>())
                {
                    // The stream writer's buffer is at least 128 characters, so the smaller size makes it write through every few lines.
                    foreach (var streamBufferSize in new[] { 1, 16 * 1024 })
                    {
                        data.Add(fixtureName, mode, streamBufferSize);
                    }
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ParityData))]
        public async Task Write_EveryFixtureToStream_MatchesSync(string fixtureName, AsyncWriteMode mode, int streamBufferSize)
        {
            // arrange
            var lines = ReadFixtureLines(fixtureName);
            using (var expectedStream = new MemoryStream())
            {
                using (var syncWriter = new IcalContentLineWriter(expectedStream, new IcalOptions(), streamBufferSize))
                {
                    syncWriter.WriteLines(lines);
                }

                var stream = new AsyncOnlyWriteStream();
                var asyncWriter = new IcalContentLineWriter(stream, new IcalOptions(), streamBufferSize);

                // act
                await WriteAsync(asyncWriter, lines, mode);
                var linesWritten = asyncWriter.LinesWritten;
                await asyncWriter.DisposeAsync();

                // assert
                stream.ToArray().Should().Equal(expectedStream.ToArray());
                linesWritten.Should().Be(lines.Count);
                stream.IsDisposed.Should().BeFalse();
            }
        }

        [Theory]
        [InlineData(AsyncWriteMode.WriteLineAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsyncFromAsyncEnumerable)]
        public async Task Write_FoldedLinesToTextWriter_MatchesSync(AsyncWriteMode mode)
        {
            // arrange
            var options = new IcalOptions { FoldLength = 7, FoldWhitespace = '\t', LineBreak = "\n" };
            var lines = new List<IcalContentLine>
            {
                new IcalContentLine("DESCRIPTION", string.Concat(Enumerable.Repeat("Café 😀 会議 ", 20))),
                new IcalContentLine("ATTENDEE", new[] { new IcalContentLineParameter("CN", "Doe, \"Jane\"\n^"), new IcalContentLineParameter("X-P", "a", "b:c") }, "mailto:x")
            };
            using (var expected = new StringWriter())
            {
                using (var syncWriter = new IcalContentLineWriter(expected, options))
                {
                    syncWriter.WriteLines(lines);
                }

                var text = new AsyncOnlyTextWriter();
                var asyncWriter = new IcalContentLineWriter(text, options);
                try
                {
                    // act
                    await WriteAsync(asyncWriter, lines, mode);

                    // assert
                    text.ToString().Should().Be(expected.ToString());
                }
                finally
                {
                    await asyncWriter.DisposeAsync();
                }
            }
        }

        [Fact]
        public async Task Write_SyncAndAsyncInterleaved_WritesInOrder()
        {
            // arrange
            using (var text = new StringWriter())
            {
                var writer = new IcalContentLineWriter(text);
                try
                {
                    // act
                    writer.WriteLine(new IcalContentLine("A", "1"));
                    await writer.WriteLineAsync(new IcalContentLine("B", "2"));
                    writer.WriteLines(new[] { new IcalContentLine("C", "3") });
                    await writer.WriteLinesAsync(new[] { new IcalContentLine("D", "4") });
                    writer.Flush();
                    await writer.FlushAsync();

                    // assert
                    text.ToString().Should().Be(Crlf("A:1", "B:2", "C:3", "D:4"));
                    writer.LinesWritten.Should().Be(4);
                }
                finally
                {
                    await writer.DisposeAsync();
                }
            }
        }

        [Fact]
        public void WriteLineAsync_TextWriterTakesLineWithoutWaiting_CompletesSynchronously()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new IcalContentLineWriter(text))
            {
                // act
                var testResult = writer.WriteLineAsync(new IcalContentLine("SUMMARY", "a"));

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                text.ToString().Should().Be(Crlf("SUMMARY:a"));
                writer.LinesWritten.Should().Be(1);
            }
        }

        #endregion

        #region Invalid lines

        [Fact]
        public async Task WriteLineAsync_InvalidLine_FaultsTaskAndWriterContinues()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            try
            {
                // act
                var task = writer.WriteLineAsync(new IcalContentLine("X_Y", "1"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<IcalFileException>();
                text.ToString().Should().BeEmpty();
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteLineAsync_AfterInvalidLine_WritesTheLine()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            try
            {
                await Record.ExceptionAsync(() => writer.WriteLineAsync(new IcalContentLine("X_Y", "1")).AsTask());

                // act
                await writer.WriteLineAsync(new IcalContentLine("A", "1"));

                // assert
                text.ToString().Should().Be(LineAWithCrlf);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteLinesAsync_InvalidLine_FaultsTaskAfterWritingLinesBeforeIt()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            try
            {
                var lines = new[] { new IcalContentLine("A", "1"), new IcalContentLine("B", "\n") };

                // act
                var task = writer.WriteLinesAsync(lines);
                Func<Task> testAction = async () => await task;

                // assert
                (await testAction.Should().ThrowAsync<IcalFileException>()).Which.Data[IcalFileException.DataItemName.LineNumber].Should().Be(2L);
                text.ToString().Should().Be(LineAWithCrlf);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteLinesAsync_NullLineFromAsyncEnumerable_FaultsTask()
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            try
            {
                // act
                Func<Task> testAction = async () => await writer.WriteLinesAsync(ToAsyncEnumerable(new[] { new IcalContentLine("A", "1"), null }));

                // assert
                (await testAction.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("lines");
                writer.LinesWritten.Should().Be(1);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        public static TheoryData<Func<IcalContentLineWriter, object>> NullArgumentCalls => new TheoryData<Func<IcalContentLineWriter, object>>
        {
            writer => writer.WriteLineAsync(null),
            writer => writer.WriteLinesAsync((IEnumerable<IcalContentLine>)null),
            writer => writer.WriteLinesAsync((IAsyncEnumerable<IcalContentLine>)null),
        };

        [Theory]
        [MemberData(nameof(NullArgumentCalls), DisableDiscoveryEnumeration = true)]
        public async Task WriteAsync_NullArgument_ThrowsSynchronously(Func<IcalContentLineWriter, object> call)
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            try
            {
                // act
                Action testAction = () => call(writer);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        #endregion

        #region I/O errors

        [Fact]
        public async Task WriteLineAsync_TextWriterThrowsIOExceptionFromCall_FaultsTask()
        {
            // arrange
            var writer = new IcalContentLineWriter(new SynchronouslyThrowingTextWriter(() => new IOException(DiskFullMessage)));
            try
            {
                // act
                var task = writer.WriteLineAsync(new IcalContentLine("A", "1"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<IOException>().WithMessage(DiskFullMessage);
                writer.LinesWritten.Should().Be(0);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteLineAsync_AfterIOExceptionFromTextWriter_WritesToTheTextWriterAgain()
        {
            // arrange: the text writer throws an IOException, then, to show that the writer called it again, another exception.
            var exceptions = new Queue<Exception>(new Exception[] { new IOException(DiskFullMessage), new InvalidOperationException(UnexpectedMessage) });
            var writer = new IcalContentLineWriter(new SynchronouslyThrowingTextWriter(exceptions.Dequeue));
            try
            {
                await Record.ExceptionAsync(() => writer.WriteLineAsync(new IcalContentLine("A", "1")).AsTask());

                // act
                Func<Task> testAction = () => writer.WriteLineAsync(new IcalContentLine("B", "2")).AsTask();

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>().WithMessage(UnexpectedMessage);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteLineAsync_DisposedStreamWriter_FaultsTask()
        {
            // arrange
            var streamWriter = new StreamWriter(new MemoryStream());
            var writer = new IcalContentLineWriter(streamWriter);
            try
            {
                streamWriter.Dispose();

                // act
                var task = writer.WriteLineAsync(new IcalContentLine("SUMMARY", "x"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<ObjectDisposedException>();
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        #endregion

        #region Progress

        [Fact]
        public async Task WriteLinesAsync_Progress_IsCumulativeAcrossCallsAndOverloads()
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            try
            {
                var progress = new RecordingProgress();
                await writer.WriteLineAsync(new IcalContentLine("A", "0"));

                // act
                await writer.WriteLinesAsync(new[] { new IcalContentLine("A", "1"), new IcalContentLine("A", "2") }, progress);
                await writer.WriteLinesAsync(ToAsyncEnumerable(new[] { new IcalContentLine("A", "3") }), progress);

                // assert
                progress.Reports.Should().Equal(new IcalWriteProgress(2), new IcalWriteProgress(3), new IcalWriteProgress(4));
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        #endregion

        #region Cancellation

        /// <summary>
        /// A writer with an operation on it that is canceled.
        /// </summary>
        public sealed class CanceledOperation
        {
            public CanceledOperation(IcalContentLineWriter writer, Func<Task> operation)
            {
                Writer = writer;
                Operation = operation;
            }

            public IcalContentLineWriter Writer { get; }

            public Func<Task> Operation { get; }
        }

        // Each way an operation is canceled, with each member that the writer then rejects.
        public static TheoryData<string, Func<CanceledOperation>, Action<IcalContentLineWriter>> MembersAfterCancellation()
        {
            var scenarios = new List<(string, Func<CanceledOperation>)>
            {
                ("WriteLineAsync", () => CreateCanceledOperation(writer => writer.WriteLineAsync(LineA, new CancellationToken(true)).AsTask())),
                ("WriteLinesAsync", () => CreateCanceledOperation(writer => writer.WriteLinesAsync(new[] { LineA }, cancellationToken: new CancellationToken(true)))),
                ("FlushAsync", () => CreateCanceledOperation(writer => writer.FlushAsync(new CancellationToken(true)))),
                ("Text writer throws", CreateTextWriterCancellation),
#if NETCOREAPP2_1_OR_GREATER
                ("During write", CreateCancellationDuringWrite),
#endif
            };
            var members = new Action<IcalContentLineWriter>[]
            {
                writer => writer.WriteLine(LineA),
                writer => writer.Flush(),
                writer => _ = writer.WriteLineAsync(LineA),
                writer => _ = writer.FlushAsync(),
            };
            var data = new TheoryData<string, Func<CanceledOperation>, Action<IcalContentLineWriter>>();
            foreach (var (name, scenario) in scenarios)
            {
                foreach (var member in members)
                {
                    data.Add(name, scenario, member);
                }
            }

            return data;
        }

        [Fact]
        public async Task WriteLineAsync_CanceledToken_CancelsAndWritesNothing()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            var canceledToken = new CancellationToken(true);

            // act
            Func<Task> testAction = async () => await writer.WriteLineAsync(LineA, canceledToken);

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
            text.ToString().Should().BeEmpty();
        }

        [Fact]
        public async Task WriteLinesAsync_CanceledToken_Cancels()
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());

            // act
            Func<Task> testAction = async () => await writer.WriteLinesAsync(new[] { LineA }, cancellationToken: new CancellationToken(true));

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task FlushAsync_CanceledToken_CancelsWithoutFlushing()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);

            // act
            Func<Task> testAction = async () => await writer.FlushAsync(new CancellationToken(true));

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
            text.FlushCount.Should().Be(0);
        }

        // On .NET Framework, TextWriter.WriteAsync takes no cancellation token, so a write in progress cannot be canceled.
#if NETCOREAPP2_1_OR_GREATER
        [Fact]
        public async Task WriteLinesAsync_CanceledDuringWrite_Cancels()
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new IcalContentLineWriter(text);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = writer.WriteLinesAsync(new[] { LineA, new IcalContentLine("B", "2") }, cancellationToken: cancellationTokenSource.Token);
                await text.WriteStarted;
                await TestTasks.CancelAsync(cancellationTokenSource);

                // act
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            }
        }
#endif

        [Fact]
        public async Task WriteLineAsync_TextWriterThrowsCancellationFromCall_CancelsTask()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await TestTasks.CancelAsync(cancellationTokenSource);
                var writer = new IcalContentLineWriter(new SynchronouslyThrowingTextWriter(() => new OperationCanceledException(cancellationTokenSource.Token)));

                // act
                var task = writer.WriteLineAsync(LineA);
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
                task.IsCanceled.Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(MembersAfterCancellation), DisableDiscoveryEnumeration = true)]
        public async Task Member_AfterCancellation_ThrowsInvalidOperation(string scenario, Func<CanceledOperation> createCanceledOperation,
            Action<IcalContentLineWriter> member)
        {
            // arrange
            _ = scenario;
            var canceled = createCanceledOperation();
            await Record.ExceptionAsync(canceled.Operation);

            // act
            Action testAction = () => member(canceled.Writer);

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
        }

        [Fact]
        public async Task WriteLinesAsync_AsyncEnumerable_ReceivesCancellationToken()
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var observedTokens = new List<CancellationToken>();
                    var lines = AsyncEnumerables.Yielding(new[] { LineA }, observedTokens.Add);

                    // act
                    await writer.WriteLinesAsync(lines, cancellationToken: cancellationTokenSource.Token);

                    // assert
                    observedTokens.Should().Equal(cancellationTokenSource.Token);
                }
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        #endregion

        #region Concurrency and disposal

        public static TheoryData<Action<IcalContentLineWriter>> MembersWhileWriteInProgress => new TheoryData<Action<IcalContentLineWriter>>
        {
            writer => writer.WriteLine(LineA),
            writer => writer.WriteLines(new[] { LineA }),
            writer => writer.Flush(),
            writer => _ = writer.WriteLineAsync(LineA),
            writer => _ = writer.WriteLinesAsync(new[] { LineA }),
            writer => _ = writer.WriteLinesAsync(ToAsyncEnumerable(new[] { LineA })),
            writer => _ = writer.FlushAsync(),
        };

        public static TheoryData<Action<IcalContentLineWriter>> AsyncMembersAfterDispose => new TheoryData<Action<IcalContentLineWriter>>
        {
            writer => _ = writer.WriteLineAsync(LineA),
            writer => _ = writer.WriteLineAsync(null),
            writer => _ = writer.WriteLinesAsync(new[] { LineA }),
            writer => _ = writer.WriteLinesAsync(ToAsyncEnumerable(new[] { LineA })),
            writer => _ = writer.FlushAsync(),
        };

        [Theory]
        [MemberData(nameof(MembersWhileWriteInProgress), DisableDiscoveryEnumeration = true)]
        public async Task Member_WhileWriteInProgress_ThrowsInvalidOperation(Action<IcalContentLineWriter> member)
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new IcalContentLineWriter(text);
            var pending = writer.WriteLineAsync(LineA).AsTask();
            await text.WriteStarted;
            try
            {
                // act
                Action testAction = () => member(writer);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
            }
            finally
            {
                text.OpenGate();
                await pending;
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task WriteLineAsync_AfterOperationsRejectedWhileWriteInProgress_WritesTheLine()
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new IcalContentLineWriter(text);
            try
            {
                var pending = writer.WriteLineAsync(LineA).AsTask();
                await text.WriteStarted;
                Record.Exception(() => writer.WriteLine(LineA));
                text.OpenGate();
                await pending;

                // act
                await writer.WriteLineAsync(new IcalContentLine("B", "2"));

                // assert
                text.ToString().Should().Be("A:1\r\nB:2\r\n");
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task Dispose_WhileWriteInProgress_CompletesWriteWithObjectDisposed()
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new IcalContentLineWriter(text);
            var pending = writer.WriteLineAsync(LineA).AsTask();
            await text.WriteStarted;
            writer.Dispose();
            text.OpenGate();

            // act
            Func<Task> testAction = () => pending;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
            text.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task DisposeAsync_OwnedWriter_FlushesWithoutSynchronousIo()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var writer = new IcalContentLineWriter(stream);
            await writer.WriteLineAsync(LineA);

            // act
            await writer.DisposeAsync();

            // assert
            stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes(LineAWithCrlf));
            stream.FlushCount.Should().BeGreaterThan(0);
            stream.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task DisposeAsync_CallerSuppliedWriter_IsNeitherFlushedNorDisposed()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            await writer.WriteLineAsync(LineA);

            // act
            await writer.DisposeAsync();

            // assert
            text.FlushCount.Should().Be(0);
            text.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task FlushAsync_CallerSuppliedWriter_IsFlushed()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            try
            {
                // act
                await writer.FlushAsync();

                // assert
                text.FlushCount.Should().Be(1);
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task FlushAsync_OwnedWriter_WritesThroughToStream()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var writer = new IcalContentLineWriter(stream);
            try
            {
                await writer.WriteLineAsync(LineA);

                // act
                await writer.FlushAsync();

                // assert
                stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes(LineAWithCrlf));
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Theory]
        [MemberData(nameof(AsyncMembersAfterDispose), DisableDiscoveryEnumeration = true)]
        public async Task AsyncMember_AfterDispose_ThrowsObjectDisposedSynchronously(Action<IcalContentLineWriter> member)
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            await writer.DisposeAsync();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineWriter));
        }

        [Fact]
        public async Task DisposeAndDisposeAsync_InAnyOrder_DoNotThrow()
        {
            // arrange
            var first = new IcalContentLineWriter(new AsyncOnlyWriteStream());
            var second = new IcalContentLineWriter(new MemoryStream());

            // act: the whole sequence of disposals is the one Act.
            Func<Task> testAction = () => DisposeInEveryOrderAsync(first, second);

            // assert
            await testAction.Should().NotThrowAsync();
        }

        #endregion

        private static async Task DisposeInEveryOrderAsync(IcalContentLineWriter first, IcalContentLineWriter second)
        {
            await first.DisposeAsync();
            first.Dispose();
            await first.DisposeAsync();
            second.Dispose();
            await second.DisposeAsync();
        }

        private static CanceledOperation CreateCanceledOperation(Func<IcalContentLineWriter, Task> operation)
        {
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            return new CanceledOperation(writer, () => operation(writer));
        }

        private static CanceledOperation CreateTextWriterCancellation()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var writer = new IcalContentLineWriter(new SynchronouslyThrowingTextWriter(() => new OperationCanceledException(cancellationTokenSource.Token)));
            return new CanceledOperation(writer, () => writer.WriteLineAsync(LineA).AsTask());
        }

#if NETCOREAPP2_1_OR_GREATER
        private static CanceledOperation CreateCancellationDuringWrite()
        {
            var text = new GatedTextWriter();
            var writer = new IcalContentLineWriter(text);
            return new CanceledOperation(writer, async () =>
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var task = writer.WriteLinesAsync(new[] { LineA, new IcalContentLine("B", "2") }, cancellationToken: cancellationTokenSource.Token);
                    await text.WriteStarted;
                    await TestTasks.CancelAsync(cancellationTokenSource);
                    await task;
                }
            });
        }
#endif

        private static List<IcalContentLine> ReadFixtureLines(string fixtureName)
        {
            using (var reader = CreateReader(RfcFixtures.All[fixtureName]))
            {
                var lines = ReadAll(reader);

                // Add lines long enough to fill the stream writer's buffer.
                lines.Add(new IcalContentLine("DESCRIPTION", string.Concat(Enumerable.Repeat("Café 😀 ", 100))));
                lines.Add(new IcalContentLine("X-LONG", new[] { new IcalContentLineParameter("X-P", new string('p', 300)) }, new string('v', 300)));
                return lines;
            }
        }

        private static IAsyncEnumerable<IcalContentLine> ToAsyncEnumerable(IEnumerable<IcalContentLine> lines)
        {
            return AsyncEnumerables.Yielding(lines);
        }

        private static async Task WriteAsync(IcalContentLineWriter writer, IReadOnlyList<IcalContentLine> lines, AsyncWriteMode mode)
        {
            switch (mode)
            {
                case AsyncWriteMode.WriteLineAsync:
                    foreach (var line in lines)
                    {
                        await writer.WriteLineAsync(line);
                    }

                    break;
                case AsyncWriteMode.WriteLinesAsync:
                    await writer.WriteLinesAsync(lines);
                    break;
                default:
                    await writer.WriteLinesAsync(ToAsyncEnumerable(lines));
                    break;
            }
        }
    }
}
