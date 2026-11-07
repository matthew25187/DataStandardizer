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
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="VcardContentLineWriter"/>.  The targets used can only be written asynchronously, so these
    /// tests also show that the async path does no synchronous I/O.
    /// </summary>
    public class VcardContentLineWriterAsyncTests
    {
        private const string LineAWithCrlf = "A:1\r\n";
        private const string CanceledMessage = "*canceled*";
        private const string DiskFullMessage = "Disk full.";
        private const string UnexpectedMessage = "Unexpected.";

        private static readonly VcardContentLine LineA = new VcardContentLine("A", "1");

        private static readonly VcardContentLine[] NoLines = new VcardContentLine[0];

        // The stream of a writer that owns its stream writer is flushed once, when the writer is disposed.
        private const int OneFlush = 1;

        // A value of characters of one to four octets in UTF-8.
        private const string MultiOctetContactUri = "Caf\xE9 \xD83D\xDE00 \x4F1A\x8B70 Caf\xE9 \xD83D\xDE00 \x4F1A\x8B70 ";

        // A vCard 4.0 card, then ref. RFC 6350 §3.2 and RFC 6868 §3: MultiOctetContactUri as a CONTACT-URI, and a NICKNAME whose CN has a comma, quotes, a line
        // break and a caret, which are quoted and caret-encoded, and whose X-P has a colon in its second value, which is quoted; each folded with
        // a tab and a line feed so that no physical line is longer than 7 octets, and without splitting a character.
        private const string MultiOctetLinesFoldedAtSevenOctets =
            "BEGIN:V\n\tCARD\nVERSION\n\t:4.0\n" +
            "CONTACT\n\t-URI:C\n\taf\xE9 \n\t\xD83D\xDE00 \n\t\x4F1A\x8B70\n\t Caf\xE9\n\t \xD83D\xDE00 \n\t\x4F1A\x8B70\n\t \n" +
            "NICKNAM\n\tE;CN=\"\n\tDoe, ^\n\t'Jane^\n\t'^n^^\"\n\t;X-P=a\n\t,\"b:c\"\n\t:mailt\n\to:x\n";

        // Eighty octets, which a fold at 75 octets would break.
        private const string LongValueOfEightyOctets = "abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzab";

        // ref. RFC 6350 §3.2: the line "X-A:" and LongValueOfEightyOctets, folded at 75 octets by the default options.
        private const string LongLineFoldedAt75Octets = "X-A:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrs\r\n tuvwxyzab\r\n";

        // The size of a stream writer's buffer that is smaller than one line, so that writing the line reaches the stream.  A stream writer's buffer is at least
        // 128 characters, and LongLineWrittenThrough is longer.
        private const int StreamBufferSmallerThanALine = 1;

        // A line that reaches the stream as it is written, whatever the fold: 200 characters of value and no parameters.
        private static readonly VcardContentLine LongLineWrittenThrough = new VcardContentLine("X-A", new string('a', 200));

        // ref. RFC 6868 §3: a caret is encoded only in the vCard 4.0 card; and vCard 2.1 output is never folded.
        private const string CardsOfEachVersionWritten =
            "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;X-P=a^^b:x\r\nEND:VCARD\r\n" +
            "BEGIN:VCARD\r\nVERSION:3.0\r\nX-A;X-P=a^b:x\r\nEND:VCARD\r\n" +
            "BEGIN:VCARD\r\nVERSION:2.1\r\nitem1.X-A:abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyzab\r\nEND:VCARD\r\n";

        public enum AsyncWriteMode
        {
            WriteLineAsync,
            WriteLinesAsync,
            WriteLinesAsyncFromAsyncEnumerable
        }

        #region Sync and async parity

        public static TheoryData<AsyncWriteMode, int> ParityData()
        {
            var data = new TheoryData<AsyncWriteMode, int>();
            foreach (var mode in Enum.GetValues(typeof(AsyncWriteMode)).Cast<AsyncWriteMode>())
            {
                // The stream writer's buffer is at least 128 characters, so the smaller size makes it write through every few lines.
                foreach (var streamBufferSize in new[] { 1, 16 * 1024 })
                {
                    data.Add(mode, streamBufferSize);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ParityData))]
        public async Task Write_RfcExampleToStream_WritesItsLines(AsyncWriteMode mode, int streamBufferSize)
        {
            // arrange
            var lines = ReadAll(CreateReader(Rfc6350Examples.Example));
            var expected = Encoding.UTF8.GetBytes(Rfc6350Examples.ExampleWritten);
            var stream = new AsyncOnlyWriteStream();
            var writer = new VcardContentLineWriter(stream, new VcardOptions(), streamBufferSize);

            // act
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await WriteAsync(writer, lines, mode);
            });

            // assert
            stream.ToArray().Should().Equal(expected);
            stream.IsDisposed.Should().BeFalse();
        }

        [Theory]
        [MemberData(nameof(ParityData))]
        public async Task Write_RfcExampleToStream_CountsItsLines(AsyncWriteMode mode, int streamBufferSize)
        {
            // arrange
            var lines = ReadAll(CreateReader(Rfc6350Examples.Example));
            var writer = new VcardContentLineWriter(new AsyncOnlyWriteStream(), new VcardOptions(), streamBufferSize);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await WriteAsync(writer, lines, mode);

                // assert
                writer.LinesWritten.Should().Be(Rfc6350Examples.ExampleLogicalLineCount);
            });
        }

        [Theory]
        [InlineData(AsyncWriteMode.WriteLineAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsyncFromAsyncEnumerable)]
        public async Task Write_LongLineToStreamWithDefaultOptions_FoldsAt75Octets(AsyncWriteMode mode)
        {
            // arrange
            var lines = new List<VcardContentLine> { new VcardContentLine("X-A", LongValueOfEightyOctets) };
            var stream = new AsyncOnlyWriteStream();
            var writer = new VcardContentLineWriter(stream, new VcardOptions());

            // act
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await WriteAsync(writer, lines, mode);
            });

            // assert
            stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes(LongLineFoldedAt75Octets));
        }

        [Theory]
        [InlineData(AsyncWriteMode.WriteLineAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsyncFromAsyncEnumerable)]
        public async Task Write_CardsOfEachVersion_TreatsEachLineAsItsCardSays(AsyncWriteMode mode)
        {
            // arrange
            var lines = new List<VcardContentLine>
            {
                new VcardContentLine("BEGIN", "VCARD"),
                new VcardContentLine("VERSION", "4.0"),
                new VcardContentLine("X-A", new[] { new VcardContentLineParameter("X-P", "a^b") }, "x"),
                new VcardContentLine("END", "VCARD"),
                new VcardContentLine("BEGIN", "VCARD"),
                new VcardContentLine("VERSION", "3.0"),
                new VcardContentLine("X-A", new[] { new VcardContentLineParameter("X-P", "a^b") }, "x"),
                new VcardContentLine("END", "VCARD"),
                new VcardContentLine("BEGIN", "VCARD"),
                new VcardContentLine("VERSION", "2.1"),
                new VcardContentLine("item1", "X-A", new VcardContentLineParameter[0], LongValueOfEightyOctets),
                new VcardContentLine("END", "VCARD"),
            };
            var text = new AsyncOnlyTextWriter();
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await WriteAsync(writer, lines, mode);

                // assert
                text.ToString().Should().Be(CardsOfEachVersionWritten);
            });
        }

        [Theory]
        [InlineData(AsyncWriteMode.WriteLineAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsync)]
        [InlineData(AsyncWriteMode.WriteLinesAsyncFromAsyncEnumerable)]
        public async Task Write_FoldedLinesToTextWriter_FoldsAndEncodesThem(AsyncWriteMode mode)
        {
            // arrange
            var options = new VcardOptions { FoldLength = 7, FoldWhitespace = '\t', LineBreak = "\n" };
            var lines = new List<VcardContentLine>
            {
                new VcardContentLine("BEGIN", "VCARD"),
                new VcardContentLine("VERSION", "4.0"),
                new VcardContentLine("CONTACT-URI", MultiOctetContactUri),
                new VcardContentLine("NICKNAME", new[] { new VcardContentLineParameter("CN", "Doe, \"Jane\"\n^"), new VcardContentLineParameter("X-P", "a", "b:c") }, "mailto:x")
            };
            var text = new AsyncOnlyTextWriter();
            var writer = new VcardContentLineWriter(text, options);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await WriteAsync(writer, lines, mode);

                // assert
                text.ToString().Should().Be(MultiOctetLinesFoldedAtSevenOctets);
            });
        }

        [Fact]
        public async Task Write_SyncAndAsyncInterleaved_WritesInOrder()
        {
            // arrange
            using (var text = new StringWriter())
            {
                var writer = new VcardContentLineWriter(text);
                await AsyncDisposal.UsingAsync(writer, async () =>
                {
                    // act
                    writer.WriteLine(new VcardContentLine("A", "1"));
                    await writer.WriteLineAsync(new VcardContentLine("B", "2"));
                    writer.WriteLines(new[] { new VcardContentLine("C", "3") });
                    await writer.WriteLinesAsync(new[] { new VcardContentLine("D", "4") });
                    writer.Flush();
                    await writer.FlushAsync();

                    // assert
                    text.ToString().Should().Be(Crlf("A:1", "B:2", "C:3", "D:4"));
                    writer.LinesWritten.Should().Be(4);
                });
            }
        }

        [Fact]
        public void WriteLineAsync_TextWriterTakesLineWithoutWaiting_CompletesSynchronously()
        {
            // arrange
            using (var text = new StringWriter())
            using (var writer = new VcardContentLineWriter(text))
            {
                // act
                var testResult = writer.WriteLineAsync(new VcardContentLine("RELATED", "a"));

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                text.ToString().Should().Be(Crlf("RELATED:a"));
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
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteLineAsync(new VcardContentLine("X_Y", "1"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<VcardFileException>();
                text.ToString().Should().BeEmpty();
            });
        }

        [Fact]
        public async Task WriteLineAsync_AfterInvalidLine_WritesTheLine()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await Record.ExceptionAsync(() => writer.WriteLineAsync(new VcardContentLine("X_Y", "1")).AsTask());

                // act
                await writer.WriteLineAsync(new VcardContentLine("A", "1"));

                // assert
                text.ToString().Should().Be(LineAWithCrlf);
            });
        }

        [Fact]
        public async Task WriteLinesAsync_InvalidLine_FaultsTaskAfterWritingLinesBeforeIt()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var lines = new[] { new VcardContentLine("A", "1"), new VcardContentLine("B", "\n") };

                // act
                var task = writer.WriteLinesAsync(lines);
                Func<Task> testAction = async () => await task;

                // assert
                (await testAction.Should().ThrowAsync<VcardFileException>()).Which.Data[VcardFileException.DataItemName.LineNumber].Should().Be(2L);
                text.ToString().Should().Be(LineAWithCrlf);
            });
        }

        [Fact]
        public async Task WriteLinesAsync_NullLineFromAsyncEnumerable_FaultsTask()
        {
            // arrange
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                Func<Task> testAction = async () => await writer.WriteLinesAsync(ToAsyncEnumerable(new[] { new VcardContentLine("A", "1"), null }));

                // assert
                (await testAction.Should().ThrowAsync<ArgumentException>()).Which.ParamName.Should().Be("lines");
                writer.LinesWritten.Should().Be(1);
            });
        }

        public static TheoryData<Func<VcardContentLineWriter, object>> NullArgumentCalls => new TheoryData<Func<VcardContentLineWriter, object>>
        {
            writer => writer.WriteLineAsync(null),
            writer => writer.WriteLinesAsync((IEnumerable<VcardContentLine>)null),
            writer => writer.WriteLinesAsync((IAsyncEnumerable<VcardContentLine>)null),
        };

        [Theory]
        [MemberData(nameof(NullArgumentCalls), DisableDiscoveryEnumeration = true)]
        public async Task WriteAsync_NullArgument_ThrowsSynchronously(Func<VcardContentLineWriter, object> call)
        {
            // arrange
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());
            await AsyncDisposal.UsingAsync(writer, () =>
            {
                // act
                Action testAction = () => call(writer);

                // assert
                testAction.Should().Throw<ArgumentNullException>();
            });
        }

        #endregion

        [Fact]
        public async Task WriteLineAsync_ParameterValueNeedingCaretsOutsideVersion40_FaultsTask()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var line = new VcardContentLine("X-A", new[] { new VcardContentLineParameter("X-P", "a\nb") }, "x");

                // act
                var task = writer.WriteLineAsync(line);
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<VcardFileException>();
                text.ToString().Should().BeEmpty();
            });
        }

        [Fact]
        public async Task WriteLineAsync_VersionLineThatFailsToWrite_DoesNotSetTheVersion()
        {
            // arrange: the text writer fails on the VERSION line only.
            var exceptions = new Queue<Exception>(new Exception[] { new IOException(DiskFullMessage) });
            var text = new AsyncOnlyTextWriterFailingOn("VERSION", exceptions);
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await writer.WriteLineAsync(new VcardContentLine("BEGIN", "VCARD"));
                await Record.ExceptionAsync(() => writer.WriteLineAsync(new VcardContentLine("VERSION", "4.0")).AsTask());

                // act
                await writer.WriteLineAsync(new VcardContentLine("X-A", new[] { new VcardContentLineParameter("X-P", "a^b") }, "x"));

                // assert
                text.ToString().Should().Be(Crlf("BEGIN:VCARD", "X-A;X-P=a^b:x"));
            });
        }

        #region I/O errors

        [Fact]
        public async Task WriteLineAsync_TextWriterThrowsIOExceptionFromCall_FaultsTask()
        {
            // arrange
            var writer = new VcardContentLineWriter(new SynchronouslyThrowingTextWriter(() => new IOException(DiskFullMessage)));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteLineAsync(new VcardContentLine("A", "1"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<IOException>().WithMessage(DiskFullMessage);
                writer.LinesWritten.Should().Be(0);
            });
        }

        [Fact]
        public async Task WriteLineAsync_AfterIOExceptionFromTextWriter_WritesToTheTextWriterAgain()
        {
            // arrange: the text writer throws an IOException, then, to show that the writer called it again, another exception.
            var exceptions = new Queue<Exception>(new Exception[] { new IOException(DiskFullMessage), new InvalidOperationException(UnexpectedMessage) });
            var writer = new VcardContentLineWriter(new SynchronouslyThrowingTextWriter(exceptions.Dequeue));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await Record.ExceptionAsync(() => writer.WriteLineAsync(new VcardContentLine("A", "1")).AsTask());

                // act
                Func<Task> testAction = () => writer.WriteLineAsync(new VcardContentLine("B", "2")).AsTask();

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>().WithMessage(UnexpectedMessage);
            });
        }

        [Fact]
        public void WriteLineAsync_OperationCanceledWithoutCanceledTokenFromTextWriter_FaultsTask()
        {
            // arrange
            var writer = new VcardContentLineWriter(new SynchronouslyThrowingTextWriter(() => new OperationCanceledException()));

            // act
            var write = writer.WriteLineAsync(LineA);

            // assert
            write.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public async Task Dispose_DuringOperationCanceledWithoutCanceledTokenFromTextWriter_FaultsTask()
        {
            // arrange
            var text = new SynchronouslyThrowingTextWriter(() => new OperationCanceledException());
            var writer = new VcardContentLineWriter(text);
            text.OnWrite = writer.Dispose;

            // act: wait for the write to finish without throwing, so that its status can be checked.
            var write = writer.WriteLineAsync(LineA).AsTask();
            await Task.WhenAny(write);

            // assert
            write.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public async Task WriteLineAsync_DisposedStreamWriter_FaultsTask()
        {
            // arrange
            var streamWriter = new StreamWriter(new MemoryStream());
            var writer = new VcardContentLineWriter(streamWriter);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                streamWriter.Dispose();

                // act
                var task = writer.WriteLineAsync(new VcardContentLine("RELATED", "x"));
                Func<Task> testAction = async () => await task;

                // assert
                await testAction.Should().ThrowAsync<ObjectDisposedException>();
            });
        }

        #endregion

        #region Progress

        [Fact]
        public async Task WriteLinesAsync_Progress_IsCumulativeAcrossCallsAndOverloads()
        {
            // arrange
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var progress = new RecordingProgress();
                await writer.WriteLineAsync(new VcardContentLine("A", "0"));

                // act
                await writer.WriteLinesAsync(new[] { new VcardContentLine("A", "1"), new VcardContentLine("A", "2") }, progress);
                await writer.WriteLinesAsync(ToAsyncEnumerable(new[] { new VcardContentLine("A", "3") }), progress);

                // assert
                progress.Reports.Should().Equal(new VcardWriteProgress(2), new VcardWriteProgress(3), new VcardWriteProgress(4));
            });
        }

        #endregion

        #region Cancellation

        /// <summary>
        /// A writer with an operation on it that is canceled.
        /// </summary>
        public sealed class CanceledOperation
        {
            public CanceledOperation(VcardContentLineWriter writer, Func<Task> operation)
            {
                Writer = writer;
                Operation = operation;
            }

            public VcardContentLineWriter Writer { get; }

            public Func<Task> Operation { get; }
        }

        // Each way an operation is canceled, with each member that the writer then rejects.
        public static TheoryData<string, Func<CanceledOperation>, Action<VcardContentLineWriter>> MembersAfterCancellation()
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
            var members = new Action<VcardContentLineWriter>[]
            {
                writer => writer.WriteLine(LineA),
                writer => writer.Flush(),
                writer => _ = writer.WriteLineAsync(LineA),
                writer => _ = writer.FlushAsync(),
            };
            var data = new TheoryData<string, Func<CanceledOperation>, Action<VcardContentLineWriter>>();
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
            var writer = new VcardContentLineWriter(text);
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
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());

            // act
            Func<Task> testAction = async () => await writer.WriteLinesAsync(new[] { LineA }, cancellationToken: new CancellationToken(true));

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
        }

        // Each overload given no lines to write.
        public static TheoryData<Func<VcardContentLineWriter, CancellationToken, Task>> WritesOfNoLines => new TheoryData<Func<VcardContentLineWriter, CancellationToken, Task>>
        {
            (writer, token) => writer.WriteLinesAsync(NoLines, cancellationToken: token),
            (writer, token) => writer.WriteLinesAsync(ToAsyncEnumerable(NoLines), cancellationToken: token),
        };

        [Theory]
        [MemberData(nameof(WritesOfNoLines), DisableDiscoveryEnumeration = true)]
        public async Task WriteLinesAsync_NoLinesAndCanceledToken_Cancels(Func<VcardContentLineWriter, CancellationToken, Task> write)
        {
            // arrange
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());
            var canceledToken = new CancellationToken(true);

            // act
            Func<Task> testAction = () => write(writer, canceledToken);

            // assert
            await testAction.Should().ThrowAsync<OperationCanceledException>();
        }

        [Fact]
        public async Task FlushAsync_CanceledToken_CancelsWithoutFlushing()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new VcardContentLineWriter(text);

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
            var writer = new VcardContentLineWriter(text);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var task = writer.WriteLinesAsync(new[] { LineA, new VcardContentLine("B", "2") }, cancellationToken: cancellationTokenSource.Token);
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
                var writer = new VcardContentLineWriter(new SynchronouslyThrowingTextWriter(() => new OperationCanceledException(cancellationTokenSource.Token)));

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
            Action<VcardContentLineWriter> member)
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
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());
            await AsyncDisposal.UsingAsync(writer, async () =>
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
            });
        }

        #endregion

        #region Concurrency and disposal

        public static TheoryData<Action<VcardContentLineWriter>> MembersWhileWriteInProgress => new TheoryData<Action<VcardContentLineWriter>>
        {
            writer => writer.WriteLine(LineA),
            writer => writer.WriteLines(new[] { LineA }),
            writer => writer.Flush(),
            writer => _ = writer.WriteLineAsync(LineA),
            writer => _ = writer.WriteLinesAsync(new[] { LineA }),
            writer => _ = writer.WriteLinesAsync(ToAsyncEnumerable(new[] { LineA })),
            writer => _ = writer.FlushAsync(),
        };

        public static TheoryData<Action<VcardContentLineWriter>> AsyncMembersAfterDispose => new TheoryData<Action<VcardContentLineWriter>>
        {
            writer => _ = writer.WriteLineAsync(LineA),
            writer => _ = writer.WriteLineAsync(null),
            writer => _ = writer.WriteLinesAsync(new[] { LineA }),
            writer => _ = writer.WriteLinesAsync(ToAsyncEnumerable(new[] { LineA })),
            writer => _ = writer.FlushAsync(),
        };

        [Theory]
        [MemberData(nameof(MembersWhileWriteInProgress), DisableDiscoveryEnumeration = true)]
        public async Task Member_WhileWriteInProgress_ThrowsInvalidOperation(Action<VcardContentLineWriter> member)
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new VcardContentLineWriter(text);
            var pending = writer.WriteLineAsync(LineA).AsTask();
            await text.WriteStarted;
            await AsyncDisposal.FinallyAsync(() =>
            {
                // act
                Action testAction = () => member(writer);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
            }, async () =>
            {
                text.OpenGate();
                await pending;
                await writer.DisposeAsync();
            });
        }

        [Fact]
        public async Task WriteLineAsync_AfterOperationsRejectedWhileWriteInProgress_WritesTheLine()
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var pending = writer.WriteLineAsync(LineA).AsTask();
                await text.WriteStarted;
                Record.Exception(() => writer.WriteLine(LineA));
                text.OpenGate();
                await pending;

                // act
                await writer.WriteLineAsync(new VcardContentLine("B", "2"));

                // assert
                text.ToString().Should().Be("A:1\r\nB:2\r\n");
            });
        }

        [Fact]
        public async Task Dispose_WhileWriteInProgress_CompletesWriteWithObjectDisposed()
        {
            // arrange
            var text = new GatedTextWriter();
            var writer = new VcardContentLineWriter(text);
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
        public async Task Dispose_DuringWriteThatDoesNotWait_CompletesWriteWithObjectDisposed()
        {
            // arrange
            var text = new AsyncOnlyTextWriter(completeSynchronously: true);
            var writer = new VcardContentLineWriter(text);
            text.OnWrite = writer.Dispose;

            // act
            Func<Task> testAction = () => writer.WriteLineAsync(LineA).AsTask();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
            text.ToString().Should().Be(LineAWithCrlf);
        }

        // Shows that the next test disposes of the writer during a write that doesn't wait: the stream writer writes the line through to the stream
        // within the call, and the write completes synchronously.
        [Fact]
        public void WriteLineAsync_OwnedWriterOverStreamThatDoesNotWait_WritesThroughAndCompletesSynchronously()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream(completeSynchronously: true);
            var writer = new VcardContentLineWriter(stream, new VcardOptions(), StreamBufferSmallerThanALine);
            var wroteThrough = false;
            stream.OnWrite = () => wroteThrough = true;

            // act
            var write = writer.WriteLineAsync(LongLineWrittenThrough);

            // assert
            write.IsCompletedSuccessfully.Should().BeTrue();
            wroteThrough.Should().BeTrue();
        }

        [Fact]
        public async Task Dispose_DuringWriteThatDoesNotWaitOnOwnedWriter_FlushesWithoutSynchronousIo()
        {
            // arrange: the stream's synchronous members throw, so a synchronous flush would escape from the call and not from the task.
            var stream = new AsyncOnlyWriteStream(completeSynchronously: true);
            var writer = new VcardContentLineWriter(stream, new VcardOptions(), StreamBufferSmallerThanALine);
            stream.OnWrite = writer.Dispose;

            // act
            Func<Task> testAction = () => writer.WriteLineAsync(LongLineWrittenThrough).AsTask();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task DisposeAsync_OwnedWriter_FlushesWithoutSynchronousIo()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var writer = new VcardContentLineWriter(stream);
            await writer.WriteLineAsync(LineA);

            // act
            await writer.DisposeAsync();

            // assert
            stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes(LineAWithCrlf));
            stream.FlushCount.Should().Be(OneFlush);
            stream.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task DisposeAsync_CallerSuppliedWriter_IsNeitherFlushedNorDisposed()
        {
            // arrange
            var text = new AsyncOnlyTextWriter();
            var writer = new VcardContentLineWriter(text);
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
            var writer = new VcardContentLineWriter(text);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                await writer.FlushAsync();

                // assert
                text.FlushCount.Should().Be(1);
            });
        }

        [Fact]
        public async Task FlushAsync_OwnedWriter_WritesThroughToStream()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream();
            var writer = new VcardContentLineWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await writer.WriteLineAsync(LineA);

                // act
                await writer.FlushAsync();

                // assert
                stream.ToArray().Should().Equal(Encoding.ASCII.GetBytes(LineAWithCrlf));
            });
        }

        [Theory]
        [MemberData(nameof(AsyncMembersAfterDispose), DisableDiscoveryEnumeration = true)]
        public async Task AsyncMember_AfterDispose_ThrowsObjectDisposedSynchronously(Action<VcardContentLineWriter> member)
        {
            // arrange
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());
            await writer.DisposeAsync();

            // act
            Action testAction = () => member(writer);

            // assert
            testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(VcardContentLineWriter));
        }

        [Fact]
        public async Task DisposeAndDisposeAsync_InAnyOrder_DoNotThrow()
        {
            // arrange
            var first = new VcardContentLineWriter(new AsyncOnlyWriteStream());
            var second = new VcardContentLineWriter(new MemoryStream());

            // act: the whole sequence of disposals is the one Act.
            Func<Task> testAction = () => DisposeInEveryOrderAsync(first, second);

            // assert
            await testAction.Should().NotThrowAsync();
        }

        #endregion

        private static async Task DisposeInEveryOrderAsync(VcardContentLineWriter first, VcardContentLineWriter second)
        {
            await first.DisposeAsync();
            first.Dispose();
            await first.DisposeAsync();
            second.Dispose();
            await second.DisposeAsync();
        }

        private static CanceledOperation CreateCanceledOperation(Func<VcardContentLineWriter, Task> operation)
        {
            var writer = new VcardContentLineWriter(new AsyncOnlyTextWriter());
            return new CanceledOperation(writer, () => operation(writer));
        }

        private static CanceledOperation CreateTextWriterCancellation()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var writer = new VcardContentLineWriter(new SynchronouslyThrowingTextWriter(() => new OperationCanceledException(cancellationTokenSource.Token)));
            return new CanceledOperation(writer, () => writer.WriteLineAsync(LineA).AsTask());
        }

#if NETCOREAPP2_1_OR_GREATER
        private static CanceledOperation CreateCancellationDuringWrite()
        {
            var text = new GatedTextWriter();
            var writer = new VcardContentLineWriter(text);
            return new CanceledOperation(writer, async () =>
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var task = writer.WriteLinesAsync(new[] { LineA, new VcardContentLine("B", "2") }, cancellationToken: cancellationTokenSource.Token);
                    await text.WriteStarted;
                    await TestTasks.CancelAsync(cancellationTokenSource);
                    await task;
                }
            });
        }
#endif

        private static IAsyncEnumerable<VcardContentLine> ToAsyncEnumerable(IEnumerable<VcardContentLine> lines)
        {
            return AsyncEnumerables.Yielding(lines);
        }

        private static async Task WriteAsync(VcardContentLineWriter writer, IReadOnlyList<VcardContentLine> lines, AsyncWriteMode mode)
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
