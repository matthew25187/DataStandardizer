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
                await writer.WriteLineAsync(new IcalContentLine("A", "1"));
                text.ToString().Should().Be(Crlf("A:1"));
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
                text.ToString().Should().Be(Crlf("A:1"));
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

        [Fact]
        public async Task WriteAsync_NullArguments_ThrowSynchronously()
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            try
            {
                // act
                var testActions = new Action[]
                {
                    () => _ = writer.WriteLineAsync(null),
                    () => _ = writer.WriteLinesAsync((IEnumerable<IcalContentLine>)null),
                    () => _ = writer.WriteLinesAsync((IAsyncEnumerable<IcalContentLine>)null)
                };

                // assert
                foreach (var testAction in testActions)
                {
                    testAction.Should().Throw<ArgumentNullException>();
                }
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        #endregion

        #region I/O errors

        [Fact]
        public async Task WriteLineAsync_TextWriterThrowsIOExceptionFromCall_FaultsTaskAndWriterContinues()
        {
            // arrange
            var throwNext = true;
            var text = new SynchronouslyThrowingTextWriter(() => throwNext ? (Exception)new IOException("Disk full.") : new InvalidOperationException("Unexpected."));
            var writer = new IcalContentLineWriter(text);
            try
            {
                // act
                var task = writer.WriteLineAsync(new IcalContentLine("A", "1"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<IOException>().WithMessage("Disk full.");
                writer.LinesWritten.Should().Be(0);
                throwNext = false;
                Func<Task> nextAction = async () => await writer.WriteLineAsync(new IcalContentLine("B", "2"));
                await nextAction.Should().ThrowAsync<InvalidOperationException>().WithMessage("Unexpected.");
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
#if NETCOREAPP3_0_OR_GREATER
                await streamWriter.DisposeAsync();
#else
                streamWriter.Dispose();
#endif

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

        [Fact]
        public async Task WriteLineAsync_CanceledToken_CancelsAndFaultsWriter()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            var canceledToken = new CancellationToken(true);

            // act
            Func<Task> testAction = async () => await writer.WriteLineAsync(new IcalContentLine("A", "1"), canceledToken);

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
            text.ToString().Should().BeEmpty();
            AssertFaulted(writer);
            await writer.DisposeAsync();
        }

        [Fact]
        public async Task WriteLinesAsync_CanceledToken_CancelsAndFaultsWriter()
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());

            // act
            Func<Task> testAction = async () => await writer.WriteLinesAsync(new[] { new IcalContentLine("A", "1") }, cancellationToken: new CancellationToken(true));

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
            AssertFaulted(writer);
            writer.Dispose();
        }

        [Fact]
        public async Task FlushAsync_CanceledToken_CancelsAndFaultsWriter()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);

            // act
            Func<Task> testAction = async () => await writer.FlushAsync(new CancellationToken(true));

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
            text.FlushCount.Should().Be(0);
            AssertFaulted(writer);
            writer.Dispose();
        }

        // On .NET Framework, TextWriter.WriteAsync takes no cancellation token, so a write in progress cannot be canceled.
#if NETCOREAPP2_1_OR_GREATER
        [Fact]
        public async Task WriteLinesAsync_CanceledDuringWrite_CancelsAndFaultsWriter()
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new IcalContentLineWriter(text);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = writer.WriteLinesAsync(new[] { new IcalContentLine("A", "1"), new IcalContentLine("B", "2") }, cancellationToken: cancellationTokenSource.Token);
                await text.WriteStarted;

                // act
                await TestTasks.CancelAsync(cancellationTokenSource);
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
                AssertFaulted(writer);
                writer.Dispose();
            }
        }
#endif

        [Fact]
        public async Task WriteLineAsync_TextWriterThrowsCancellationFromCall_CancelsTaskAndFaultsWriter()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                await TestTasks.CancelAsync(cancellationTokenSource);
                var text = new SynchronouslyThrowingTextWriter(() => new OperationCanceledException(cancellationTokenSource.Token));
                var writer = new IcalContentLineWriter(text);

                // act
                var task = writer.WriteLineAsync(new IcalContentLine("A", "1"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
                task.IsCanceled.Should().BeTrue();
                AssertFaulted(writer);
                writer.Dispose();
            }
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

                    IAsyncEnumerable<IcalContentLine> Lines()
                    {
                        return AsyncEnumerables.Yielding(new[] { new IcalContentLine("A", "1") }, observedTokens.Add);
                    }

                    // act
                    await writer.WriteLinesAsync(Lines(), cancellationToken: cancellationTokenSource.Token);

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

        [Fact]
        public async Task Operations_WhileWriteInProgress_ThrowInvalidOperation()
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new IcalContentLineWriter(text);
            try
            {
                var line = new IcalContentLine("A", "1");
                var pending = writer.WriteLineAsync(line).AsTask();
                await text.WriteStarted;

                // act
                var testActions = new Action[]
                {
                    () => writer.WriteLine(line),
                    () => writer.WriteLines(new[] { line }),
                    () => writer.Flush(),
                    () => _ = writer.WriteLineAsync(line),
                    () => _ = writer.WriteLinesAsync(new[] { line }),
                    () => _ = writer.WriteLinesAsync(ToAsyncEnumerable(new[] { line })),
                    () => _ = writer.FlushAsync()
                };

                // assert
                foreach (var testAction in testActions)
                {
                    testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
                }

                text.OpenGate();
                await pending;
                await writer.WriteLineAsync(new IcalContentLine("B", "2"));
                text.ToString().Should().Be(Crlf("A:1", "B:2"));
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
            var pending = writer.WriteLineAsync(new IcalContentLine("A", "1")).AsTask();
            await text.WriteStarted;

            // act
            writer.Dispose();
            text.OpenGate();
            Func<Task> testAction = async () => await pending;

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
            await writer.WriteLineAsync(new IcalContentLine("A", "1"));

            // act
            await writer.DisposeAsync();

            // assert
            stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes("A:1\r\n"));
            stream.FlushCount.Should().BeGreaterThan(0);
            stream.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task DisposeAsync_CallerSuppliedWriter_IsNeitherFlushedNorDisposed()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new IcalContentLineWriter(text);
            await writer.WriteLineAsync(new IcalContentLine("A", "1"));

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
                await writer.WriteLineAsync(new IcalContentLine("A", "1"));

                // act
                await writer.FlushAsync();

                // assert
                stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes("A:1\r\n"));
            }
            finally
            {
                await writer.DisposeAsync();
            }
        }

        [Fact]
        public async Task AsyncMembers_AfterDispose_ThrowObjectDisposedSynchronously()
        {
            // arrange
            var writer = new IcalContentLineWriter(new AsyncOnlyTextWriter());
            await writer.DisposeAsync();
            var line = new IcalContentLine("A", "1");

            // act
            var testActions = new Action[]
            {
                () => _ = writer.WriteLineAsync(line),
                () => _ = writer.WriteLineAsync(null),
                () => _ = writer.WriteLinesAsync(new[] { line }),
                () => _ = writer.WriteLinesAsync(ToAsyncEnumerable(new[] { line })),
                () => _ = writer.FlushAsync()
            };

            // assert
            foreach (var testAction in testActions)
            {
                testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineWriter));
            }
        }

        [Fact]
        public async Task DisposeAndDisposeAsync_InAnyOrder_DoNotThrow()
        {
            // arrange
            var first = new IcalContentLineWriter(new AsyncOnlyWriteStream());
            var second = new IcalContentLineWriter(new MemoryStream());

            // act
            Func<Task> testAction = async () =>
            {
                await first.DisposeAsync();
                first.Dispose();
                await first.DisposeAsync();
                second.Dispose();
                await second.DisposeAsync();
            };

            // assert
            await testAction.Should().NotThrowAsync();
        }

        #endregion

        private static void AssertFaulted(IcalContentLineWriter writer)
        {
            var line = new IcalContentLine("A", "1");
            var testActions = new Action[]
            {
                () => writer.WriteLine(line),
                () => writer.Flush(),
                () => _ = writer.WriteLineAsync(line),
                () => _ = writer.FlushAsync()
            };

            foreach (var testAction in testActions)
            {
                testAction.Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
            }
        }

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
