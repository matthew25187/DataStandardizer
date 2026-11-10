using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.vCard.Tests.VcardTestData;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="VcardContentLineReader"/>.  The sources used can only be read asynchronously, so these tests
    /// also show that the async path does no synchronous I/O.
    /// </summary>
    public class VcardContentLineReaderAsyncTests
    {
        private const string LineA = "A:1";
        private const string LineB = "B:2";
        private const string SyntaxErrorLine = "BAD";

        // The number of characters that AsyncOnlyTextReader gives each read, so that a read ends inside lines, folds and line breaks.
        private const int CharactersPerRead = 7;

        // The size of the blocks that the reader reads from a stream, small enough to end inside a line.
        private const int SmallStreamBufferSize = 5;

        // "A:" and the euro sign in UTF-8, E2 82 AC, with a CRLF and a fold's space between its second and third octets; then a line break.
        private static readonly byte[] FoldInsideEuroSign = { 0x41, 0x3A, 0xE2, 0x82, 0x0D, 0x0A, 0x20, 0xAC, 0x0D, 0x0A };
        private const string EuroSign = "\x20AC";

        // "A:" and the byte FF, which never appears in UTF-8 (RFC 3629 §1), and a line break.
        private static readonly byte[] InvalidUtf8Line = { 0x41, 0x3A, 0xFF, 0x0D, 0x0A };

        public static TheoryData<Func<VcardContentLineReader, object>> MembersWhileReadInProgress()
        {
            return new TheoryData<Func<VcardContentLineReader, object>>
            {
                reader => reader.ReadLineAsync(),
                reader => reader.ReadContentLineAsync(),
                reader => reader.ReadLine(),
                reader => reader.ReadContentLine()
            };
        }

        public static TheoryData<Func<VcardContentLineReader, Task>> Disposals()
        {
            return new TheoryData<Func<VcardContentLineReader, Task>>
            {
                DisposeSynchronously,
                reader => reader.DisposeAsync().AsTask()
            };
        }

        public static TheoryData<Func<VcardContentLineReader, object>> MembersAfterCancellation()
        {
            return new TheoryData<Func<VcardContentLineReader, object>>
            {
                reader => reader.ReadLineAsync(),
                reader => reader.ReadLine()
            };
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public async Task ReadLinesAsync_Rfc6350ExampleFromAsyncOnlyTextReader_GivesItsLines(int bufferSize)
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Rfc6350Examples.Example, CharactersPerRead), new VcardOptions { MaxLineLength = null }, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = LineDescription.OfAll(await AsyncEnumerables.ToListAsync(reader.ReadLinesAsync()));

                // assert
                testResult.Should().Equal(Rfc6350Examples.ExampleLines);
            });
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public async Task ReadLinesAsync_Rfc6350ExampleFromAsyncOnlyStream_GivesItsLines(int bufferSize)
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(Rfc6350Examples.Example);
            var reader = new VcardContentLineReader(new AsyncOnlyStream(data), new VcardOptions { MaxLineLength = null }, SmallStreamBufferSize, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = LineDescription.OfAll(await AsyncEnumerables.ToListAsync(reader.ReadLinesAsync()));

                // assert
                testResult.Should().Equal(Rfc6350Examples.ExampleLines);
            });
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public async Task ReadContentLineAsync_Rfc6350Example_ViewAgreesWithReadLine(int bufferSize)
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Rfc6350Examples.Example, CharactersPerRead), new VcardOptions { MaxLineLength = null }, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await DescribeViewsAsync(reader);

                // assert
                testResult.Should().Equal(Rfc6350Examples.ExampleLines);
            });
        }

        [Theory]
        [MemberData(nameof(BufferSizeData), MemberType = typeof(VcardTestData))]
        public async Task ReadLineAsync_FoldInsideUtf8SequenceFromAsyncOnlyStream_RestoresCharacter(int bufferSize)
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyStream(FoldInsideEuroSign), new VcardOptions { MaxLineLength = null }, SmallStreamBufferSize, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await reader.ReadLineAsync();
                var physicalLinesRead = reader.PhysicalLinesRead;

                // assert
                testResult.Value.Should().Be(EuroSign);
                physicalLinesRead.Should().Be(2);
            });
        }

        [Fact]
        public async Task ReadLineAsync_CaretsInVersion40AndVersion30Cards_FollowEachCard()
        {
            // arrange
            const string text = "BEGIN:VCARD\r\nVERSION:4.0\r\nX-A;P=^^:v\r\nEND:VCARD\r\nBEGIN:VCARD\r\nVERSION:3.0\r\nX-A;P=^^:v\r\nEND:VCARD\r\n";
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(text, CharactersPerRead));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await AsyncEnumerables.ToListAsync(reader.ReadLinesAsync());

                // assert
                testResult[2].Parameters[0].Values.Should().Equal("^");
                testResult[6].Parameters[0].Values.Should().Equal("^^");
            });
        }

        [Fact]
        public async Task ReadLineAsync_SyntaxError_FaultsTask()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(SyntaxErrorLine, LineB)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadLineAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<VcardFileException>();
            });
        }

        [Fact]
        public async Task ReadLineAsync_SyntaxErrorOnLineAlreadyBuffered_FaultsTaskRatherThanThrowing()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(LineA, SyntaxErrorLine)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadLineAsync();

                // act
                var testResult = reader.ReadLineAsync().AsTask();

                // assert
                await Assert.ThrowsAsync<VcardFileException>(() => testResult);
            });
        }

        [Fact]
        public async Task ReadLineAsync_AfterSyntaxError_ReadsTheNextLine()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(SyntaxErrorLine, LineB)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                var testResult = await reader.ReadLineAsync();

                // assert
                testResult.Name.Should().Be("B");
            });
        }

        [Fact]
        public async Task ReadLineAsync_BareLineFeed_FaultsTaskAndFaultsReader()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader("A:1\nB:2\r\n"));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                Func<object> testAction = () => reader.ReadLineAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task ReadLineAsync_InvalidUtf8_FaultsTask()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyStream(InvalidUtf8Line));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadLineAsync().AsTask();

                // assert
                (await testAction.Should().ThrowAsync<VcardFileException>()).WithInnerException<DecoderFallbackException>();
            });
        }

        [Fact]
        public async Task ReadLineAsync_AfterInvalidUtf8_CannotContinue()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyStream(InvalidUtf8Line));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                Func<object> testAction = () => reader.ReadLineAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        // ref. #265: an I/O error goes to the returned task, even when the read fails before it first awaits.
        [Fact]
        public async Task ReadLineAsync_SourceThrowsSynchronously_FaultsTaskRatherThanThrowing()
        {
            // arrange
            var reader = new VcardContentLineReader(new FailingTextReader(() => new IOException(), FailureTiming.FromCall));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = reader.ReadLineAsync().AsTask();

                // assert
                await Assert.ThrowsAsync<IOException>(() => testResult);
            });
        }

        [Fact]
        public async Task ReadContentLineAsync_SourceThrowsSynchronously_FaultsTaskRatherThanThrowing()
        {
            // arrange
            var reader = new VcardContentLineReader(new FailingTextReader(() => new IOException(), FailureTiming.FromCall));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = reader.ReadContentLineAsync().AsTask();

                // assert
                await Assert.ThrowsAsync<IOException>(() => testResult);
            });
        }

        [Fact]
        public async Task ReadLineAsync_AfterSourceThrew_CannotContinue()
        {
            // arrange
            var reader = new VcardContentLineReader(new FailingTextReader(() => new IOException(), FailureTiming.FromCall));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                Func<object> testAction = () => reader.ReadLineAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task ReadLineAsync_AlreadyCanceled_CancelsTask()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(LineA)));
            using (var source = new CancellationTokenSource())
            {
                await TestTasks.CancelAsync(source);
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    // act
                    Func<Task> testAction = () => reader.ReadLineAsync(source.Token).AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                });
            }
        }

        [Theory]
        [MemberData(nameof(MembersAfterCancellation))]
        public async Task Member_AfterReadCanceled_CannotContinue(Func<VcardContentLineReader, object> member)
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(LineA)));
            using (var source = new CancellationTokenSource())
            {
                await TestTasks.CancelAsync(source);
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    await Record.ExceptionAsync(() => reader.ReadLineAsync(source.Token).AsTask());

                    // act
                    Action testAction = () => member(reader);

                    // assert
                    testAction.Should().Throw<InvalidOperationException>();
                });
            }
        }

        [Fact]
        public async Task ReadLineAsync_CanceledWhileWaitingForSource_CancelsTask()
        {
            // arrange
            var textReader = new GatedTextReader(Crlf(LineA));
            var reader = new VcardContentLineReader(textReader);
            using (var source = new CancellationTokenSource())
            {
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    var pendingRead = reader.ReadLineAsync(source.Token).AsTask();
                    await textReader.ReadStarted;
                    await TestTasks.CancelAsync(source);

                    // The read on .NET Standard 2.0 takes no token, so it sees the cancellation only once this read returns.
                    textReader.OpenGate();

                    // act
                    Func<Task> testAction = () => pendingRead;

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                });
            }
        }

        [Theory]
        [MemberData(nameof(MembersWhileReadInProgress))]
        public async Task Member_WhileReadInProgress_Throws(Func<VcardContentLineReader, object> member)
        {
            // arrange
            var textReader = new GatedTextReader(Crlf(LineA));
            var reader = new VcardContentLineReader(textReader);
            var pendingRead = reader.ReadLineAsync().AsTask();
            await textReader.ReadStarted;
            await AsyncDisposal.FinallyAsync(() =>
            {
                // act
                Action testAction = () => member(reader);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }, async () =>
            {
                textReader.OpenGate();
                await pendingRead;
                await reader.DisposeAsync();
            });
        }

        [Theory]
        [MemberData(nameof(Disposals))]
        public async Task ReadLineAsync_DisposedWhileInProgress_CompletesWithObjectDisposedException(Func<VcardContentLineReader, Task> disposal)
        {
            // arrange
            var textReader = new GatedTextReader(Crlf(LineA));
            var reader = new VcardContentLineReader(textReader);
            var pendingRead = reader.ReadLineAsync().AsTask();
            await textReader.ReadStarted;
            await disposal(reader);
            textReader.OpenGate();

            // act
            Func<Task> testAction = () => pendingRead;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task ViewFromAsyncRead_AfterNextReadStarts_Throws()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(LineA, LineB)));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadContentLineAsync();
                var view = reader.CurrentLine;
                await reader.ReadContentLineAsync();

                // act
                Action testAction = () => view.ToContentLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task ReadLinesAsync_TokenCanceledBeforeEnumerating_Throws()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(LineA)));
            using (var source = new CancellationTokenSource())
            {
                await TestTasks.CancelAsync(source);
                await AsyncDisposal.UsingAsync(reader, async () =>
                {
                    // act
                    Func<Task> testAction = () => AsyncEnumerables.ToListAsync(reader.ReadLinesAsync(source.Token));

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                });
            }
        }

        [Fact]
        public async Task DisposeAsync_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = new VcardContentLineReader(new AsyncOnlyTextReader(Crlf(LineA)));
            await reader.DisposeAsync();

            // act
            Func<Task> testAction = () => reader.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        private static Task DisposeSynchronously(VcardContentLineReader reader)
        {
            reader.Dispose();
            return Task.CompletedTask;
        }

        #region Cancellation precedence

        private const string SourceFailureMessage = "*failure reading*";
        private const string CanceledMessage = "*canceled*";

        // Each asynchronous read.
        private static readonly (string Name, Func<VcardContentLineReader, CancellationToken, Task> Run)[] AsyncReads =
        {
            ("ReadLineAsync", (reader, token) => reader.ReadLineAsync(token).AsTask()),
            ("ReadContentLineAsync", (reader, token) => reader.ReadContentLineAsync(token).AsTask()),
            ("ReadLinesAsync", (reader, token) => AsyncEnumerables.ToListAsync(reader.ReadLinesAsync(token))),
        };

        // Each way the text reader can report a failure.
        private static readonly FailureTiming[] FailureTimings =
        {
            FailureTiming.FromCall, FailureTiming.AsFaultedTask, FailureTiming.AsCanceledTask, FailureTiming.AfterYielding
        };

        // Each read that returns a task, with each way the text reader can report a failure, each OperationCanceledException it can fail with
        // while the caller's token isn't canceled, and the exception the task then faults with.  A canceled task carries no exception of its own,
        // so awaiting it gives a TaskCanceledException, whatever exception the text reader was given.  ReadLinesAsync is left out: an async
        // iterator reports any OperationCanceledException as a cancellation of its MoveNextAsync.
        public static TheoryData<string, Func<VcardContentLineReader, CancellationToken, Task>, FailureTiming, Func<Exception>, Type> ReadsWithSourceCancellationFaults()
        {
            var exceptions = new (string, Func<Exception>)[]
            {
                ("no token", () => new OperationCanceledException()),
                ("another, canceled token", () => new OperationCanceledException(new CancellationToken(true))),
            };
            var data = new TheoryData<string, Func<VcardContentLineReader, CancellationToken, Task>, FailureTiming, Func<Exception>, Type>();
            foreach (var (name, run) in AsyncReads.Take(2))
            {
                foreach (var timing in FailureTimings)
                {
                    foreach (var (exceptionName, createException) in exceptions)
                    {
                        var expectedType = timing == FailureTiming.AsCanceledTask ? typeof(TaskCanceledException) : typeof(OperationCanceledException);
                        data.Add(name + ", " + timing + ", " + exceptionName, run, timing, createException, expectedType);
                    }
                }
            }

            return data;
        }

        // Each asynchronous read, with each way the text reader can report a failure.
        public static TheoryData<string, Func<VcardContentLineReader, CancellationToken, Task>, FailureTiming> ReadsWithSourceFailures()
        {
            var data = new TheoryData<string, Func<VcardContentLineReader, CancellationToken, Task>, FailureTiming>();
            foreach (var (name, run) in AsyncReads)
            {
                foreach (var timing in FailureTimings)
                {
                    data.Add(name + ", " + timing, run, timing);
                }
            }

            return data;
        }

        // Each read that returns a task, with each way the text reader can report a failure.
        public static TheoryData<string, Func<VcardContentLineReader, CancellationToken, Task>, FailureTiming> TaskReadsWithSourceFailures()
        {
            var data = new TheoryData<string, Func<VcardContentLineReader, CancellationToken, Task>, FailureTiming>();
            foreach (var (name, run) in AsyncReads.Take(2))
            {
                foreach (var timing in FailureTimings)
                {
                    data.Add(name + ", " + timing, run, timing);
                }
            }

            return data;
        }

        [Theory]
        [MemberData(nameof(ReadsWithSourceCancellationFaults), DisableDiscoveryEnumeration = true)]
        public async Task AsyncRead_TextReaderFailsWithOperationCanceledWhileTokenIsNotCanceled_FaultsTask(string scenario,
            Func<VcardContentLineReader, CancellationToken, Task> run, FailureTiming timing, Func<Exception> createException, Type expectedExceptionType)
        {
            // arrange
            _ = scenario;
            var reader = new VcardContentLineReader(new FailingTextReader(createException, timing));

            // act: wait for the read to finish without throwing, so that its status can be checked.
            var task = run(reader, CancellationToken.None);
            await Task.WhenAny(task);

            // assert
            task.IsFaulted.Should().BeTrue();
            task.Exception.InnerException.Should().BeOfType(expectedExceptionType);
        }

        [Theory]
        [MemberData(nameof(ReadsWithSourceFailures), DisableDiscoveryEnumeration = true)]
        public async Task ReadLine_AfterOperationCanceledWhileTokenIsNotCanceled_ThrowsInvalidOperationForTheSource(string scenario,
            Func<VcardContentLineReader, CancellationToken, Task> run, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            var reader = new VcardContentLineReader(new FailingTextReader(() => new OperationCanceledException(), timing));
            await Record.ExceptionAsync(() => run(reader, CancellationToken.None));

            // act
            Action testAction = () => reader.ReadLine();

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(SourceFailureMessage);
        }

        [Theory]
        [MemberData(nameof(TaskReadsWithSourceFailures), DisableDiscoveryEnumeration = true)]
        public async Task AsyncRead_TokenCanceledAndTextReaderFailsWithCancellationOfAnotherToken_CancelsTask(string scenario,
            Func<VcardContentLineReader, CancellationToken, Task> run, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            var source = new FailingTextReader(() => new OperationCanceledException(new CancellationToken(true)), timing);
            var reader = new VcardContentLineReader(source);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                source.OnFailure = cancellationTokenSource.Cancel;

                // act: wait for the read to finish without throwing, so that its status can be checked.
                var task = run(reader, cancellationTokenSource.Token);
                await Task.WhenAny(task);

                // assert
                task.IsCanceled.Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(ReadsWithSourceFailures), DisableDiscoveryEnumeration = true)]
        public async Task ReadLine_AfterTokenCanceledAndTextReaderFailsWithCancellationOfNoToken_ThrowsInvalidOperationForTheCancellation(string scenario,
            Func<VcardContentLineReader, CancellationToken, Task> run, FailureTiming timing)
        {
            // arrange
            _ = scenario;
            var source = new FailingTextReader(() => new OperationCanceledException(), timing);
            var reader = new VcardContentLineReader(source);
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                source.OnFailure = cancellationTokenSource.Cancel;
                await Record.ExceptionAsync(() => run(reader, cancellationTokenSource.Token));

                // act
                Action testAction = () => reader.ReadLine();

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
            }
        }

        #endregion
    }
}
