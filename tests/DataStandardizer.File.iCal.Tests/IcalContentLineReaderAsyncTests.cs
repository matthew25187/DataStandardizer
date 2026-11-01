using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using static DataStandardizer.File.iCal.Tests.IcalTestData;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="IcalContentLineReader"/>.  The sources used can only be read asynchronously, so these tests
    /// also show that the async path does no synchronous I/O.
    /// </summary>
    public class IcalContentLineReaderAsyncTests
    {
        private const string LineA = "A:1";
        private const string LineB = "B:2";
        private const string LineC = "C:3";
        private const string CanceledMessage = "*canceled*";

        // The number of characters that AsyncOnlyTextReader gives each read, so that a read ends inside lines, folds and line breaks.
        private const int CharactersPerRead = 7;

        // The size of the blocks that the reader reads from a stream, small enough to end inside a line.
        private const int SmallStreamBufferSize = 5;

        // The members that a reader rejects after a read was canceled.
        public static TheoryData<Func<IcalContentLineReader, object>> MembersAfterCancellation => new TheoryData<Func<IcalContentLineReader, object>>
        {
            reader => reader.ReadLineAsync(),
            reader => reader.ReadLine(),
        };

        // The members that a reader rejects while a read is in progress.
        public static TheoryData<Func<IcalContentLineReader, object>> MembersWhileReadInProgress => new TheoryData<Func<IcalContentLineReader, object>>
        {
            reader => reader.ReadLineAsync(),
            reader => reader.ReadContentLineAsync(),
            reader => reader.ReadLine(),
            reader => reader.ReadContentLine(),
        };

        public static TheoryData<Func<IcalContentLineReader, object>> MembersAfterDisposeAsync => new TheoryData<Func<IcalContentLineReader, object>>
        {
            reader => reader.ReadLineAsync(),
            reader => reader.ReadContentLineAsync(),
            reader => reader.ReadLinesAsync(),
            reader => reader.CurrentLine.Name.Length,
            reader => reader.LinesRead,
        };

        public static TheoryData<string, Func<IcalContentLineReader, Task>> Disposals => new TheoryData<string, Func<IcalContentLineReader, Task>>
        {
            { nameof(IcalContentLineReader.Dispose), reader => DisposeSynchronously(reader) },
            { nameof(IcalContentLineReader.DisposeAsync), reader => reader.DisposeAsync().AsTask() },
        };

        #region Sync and async parity

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.ConsistencyData), MemberType = typeof(IcalContentLineReaderTests))]
        public async Task ReadLinesAsync_EveryFixture_ReadsItsLines(string fixtureName, int bufferSize)
        {
            // arrange
            var expected = FixtureExpectations.NumberedLines[fixtureName];
            var reader = CreateAsyncOnlyReader(RfcFixtures.All[fixtureName], bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = LineSnapshot.DescribeNumbered(await ReadAllAsync(reader));

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.ConsistencyData), MemberType = typeof(IcalContentLineReaderTests))]
        public async Task ReadContentLineAsync_EveryFixture_ViewsItsLines(string fixtureName, int bufferSize)
        {
            // arrange
            var expected = FixtureExpectations.NumberedLines[fixtureName];
            var reader = CreateAsyncOnlyReader(RfcFixtures.All[fixtureName], bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = LineSnapshot.DescribeNumbered(await ReadAllViewsAsync(reader));

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.ConsistencyData), MemberType = typeof(IcalContentLineReaderTests))]
        public async Task ReadLinesAsync_EveryFixtureFromStream_ReadsItsLines(string fixtureName, int bufferSize)
        {
            // arrange
            var expected = FixtureExpectations.NumberedLines[fixtureName];
            var data = Encoding.UTF8.GetBytes(RfcFixtures.All[fixtureName]);
            var reader = new IcalContentLineReader(new AsyncOnlyStream(data), new IcalOptions(), SmallStreamBufferSize, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = LineSnapshot.DescribeNumbered(await ReadAllAsync(reader));

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.StreamBufferSizeData), MemberType = typeof(IcalContentLineReaderTests))]
        public async Task ReadLineAsync_FoldInsideUtf8Sequence_RestoresCharacter(int streamBufferSize, int bufferSize)
        {
            // arrange
            var reader = new IcalContentLineReader(new AsyncOnlyStream(IcalContentLineReaderTests.FoldsInsideUtf8Sequences), new IcalOptions(),
                streamBufferSize, bufferSize);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await ReadAllAsync(reader);

                // assert
                testResult.Should().HaveCount(2);
                testResult[0].Value.Should().Be(IcalContentLineReaderTests.FoldsInsideUtf8SequencesValue);
                testResult[1].Value.Should().Be("\xE9");
                testResult[1].PhysicalLineNumber.Should().Be(5);
                reader.PhysicalLinesRead.Should().Be(5);
            });
        }

        [Fact]
        public async Task ReadLineAsync_SyntaxError_FaultsTask()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, "BAD", LineC));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadLineAsync();

                // act
                Func<Task> testAction = () => reader.ReadLineAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<IcalFileException>();
            });
        }

        [Fact]
        public async Task ReadLineAsync_AfterSyntaxError_ReadsTheNextLine()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, "BAD", LineC));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadLineAsync();
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                var testResult = await reader.ReadLineAsync();

                // assert
                testResult.Name.Should().Be("C");
            });
        }

        [Fact]
        public async Task ReadLineAsync_AfterTheLineAfterASyntaxError_ReturnsNull()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, "BAD", LineC));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadLineAsync();
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());
                await reader.ReadLineAsync();

                // act
                var testResult = await reader.ReadLineAsync();

                // assert
                testResult.Should().BeNull();
            });
        }

        [Fact]
        public async Task ReadLineAsync_AfterLineBreakError_ThrowsInvalidOperation()
        {
            // arrange
            var reader = CreateAsyncOnlyReader("SUMMARY:a\nX-B:d\r\n");
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                Action testAction = () => reader.ReadLineAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task ReadContentLineAsync_AfterLineBreakError_ThrowsInvalidOperation()
        {
            // arrange
            var reader = CreateAsyncOnlyReader("SUMMARY:a\nX-B:d\r\n");
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                Action testAction = () => reader.ReadContentLineAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task ReadContentLineAsync_LineAlreadyBuffered_CompletesSynchronously()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB, LineC));
            var reader = new IcalContentLineReader(source);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                source.OpenGate();
                await reader.ReadContentLineAsync();
                source.CloseGate();

                // act
                var testResult = reader.ReadContentLineAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                testResult.Result.Should().BeTrue();
                reader.CurrentLine.Name.ToString().Should().Be("B");
                source.ReadsCompleted.Should().Be(1);
            });
        }

        #endregion

        #region One read at a time

        [Theory]
        [MemberData(nameof(MembersWhileReadInProgress), DisableDiscoveryEnumeration = true)]
        public async Task Member_WhileReadInProgress_ThrowsInvalidOperation(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB, LineC));
            var reader = new IcalContentLineReader(source);
            var firstRead = reader.ReadLineAsync();
            await source.ReadStarted;
            await AsyncDisposal.FinallyAsync(() =>
            {
                // act
                Action testAction = () => member(reader);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
            }, async () =>
            {
                source.OpenGate();
                await firstRead;
                await reader.DisposeAsync();
            });
        }

        [Fact]
        public async Task ReadLineAsync_OtherReadRejectedWhileInProgress_ReadsTheLine()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB, LineC));
            var reader = new IcalContentLineReader(source);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                var firstRead = reader.ReadLineAsync();
                await source.ReadStarted;
                Record.Exception(() => reader.ReadLine());
                source.OpenGate();

                // act
                var testResult = await firstRead;

                // assert
                testResult.Name.Should().Be("A");
            });
        }

        [Fact]
        public async Task ReadLine_SyncAndAsyncOneAfterAnother_ReadsEveryLine()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB, LineC));
            var reader = new IcalContentLineReader(source);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                source.OpenGate();

                // act
                var testResult = await ReadAsyncThenSyncAsync(reader);

                // assert
                testResult.Select(line => line?.Name).Should().Equal("A", "B", "C", null);
            });
        }

        #endregion

        #region Cancellation

        [Fact]
        public async Task ReadLineAsync_CanceledBeforeRead_IsCanceled()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, LineB));
            await AsyncDisposal.UsingAsync(reader, () =>
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.Cancel();

                    // act
                    var testResult = reader.ReadLineAsync(cancellationTokenSource.Token);

                    // assert
                    testResult.IsCanceled.Should().BeTrue();
                }
            });
        }

        [Theory]
        [MemberData(nameof(MembersAfterCancellation), DisableDiscoveryEnumeration = true)]
        public async Task Member_AfterCancellationBeforeRead_ThrowsInvalidOperation(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, LineB));
            await AsyncDisposal.UsingAsync(reader, () =>
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.Cancel();
                    _ = reader.ReadLineAsync(cancellationTokenSource.Token);

                    // act
                    Action testAction = () => member(reader);

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
                }
            });
        }

        // On .NET Framework, TextReader.ReadAsync takes no cancellation token, so a read in progress cannot be canceled.
#if NETCOREAPP2_1_OR_GREATER
        [Fact]
        public async Task ReadLineAsync_CanceledWhileInProgress_ThrowsOperationCanceled()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB));
            var reader = new IcalContentLineReader(source);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var readTask = reader.ReadLineAsync(cancellationTokenSource.Token);
                    await source.ReadStarted;
                    cancellationTokenSource.Cancel();

                    // act
                    Func<Task> testAction = () => readTask.AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Theory]
        [MemberData(nameof(MembersAfterCancellation), DisableDiscoveryEnumeration = true)]
        public async Task Member_AfterCancellationWhileInProgress_ThrowsInvalidOperation(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB));
            var reader = new IcalContentLineReader(source);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var readTask = reader.ReadLineAsync(cancellationTokenSource.Token);
                    await source.ReadStarted;
                    cancellationTokenSource.Cancel();
                    await Record.ExceptionAsync(() => readTask.AsTask());

                    // act
                    Action testAction = () => member(reader);

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
                }
            });
        }
#endif

        [Fact]
        public async Task ReadLinesAsync_EnumeratorTokenCanceled_ThrowsOperationCanceled()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, LineB));
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var enumerator = reader.ReadLinesAsync().GetAsyncEnumerator(cancellationTokenSource.Token);
                await enumerator.MoveNextAsync();
                cancellationTokenSource.Cancel();
                await AsyncDisposal.FinallyAsync(async () =>
                {
                    // act
                    Func<Task> testAction = () => enumerator.MoveNextAsync().AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }, async () =>
                {
                    await enumerator.DisposeAsync();
                    await reader.DisposeAsync();
                });
            }
        }

        [Theory]
        [MemberData(nameof(MembersAfterCancellation), DisableDiscoveryEnumeration = true)]
        public async Task Member_AfterEnumeratorTokenCanceled_ThrowsInvalidOperation(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, LineB));
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var enumerator = reader.ReadLinesAsync().GetAsyncEnumerator(cancellationTokenSource.Token);
                await enumerator.MoveNextAsync();
                cancellationTokenSource.Cancel();
                await Record.ExceptionAsync(() => enumerator.MoveNextAsync().AsTask());
                await AsyncDisposal.FinallyAsync(() =>
                {
                    // act
                    Action testAction = () => member(reader);

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
                }, async () =>
                {
                    await enumerator.DisposeAsync();
                    await reader.DisposeAsync();
                });
            }
        }

        #endregion

        #region Disposal

        [Theory]
        [MemberData(nameof(Disposals), DisableDiscoveryEnumeration = true)]
        public async Task Dispose_WhileReadInProgress_LeavesTheBlockWithTheRead(string disposal, Func<IcalContentLineReader, Task> dispose)
        {
            // arrange
            _ = disposal;
            var source = new GatedTextReader(Crlf(LineA, LineB));
            var reader = new IcalContentLineReader(source);
            var readTask = reader.ReadLineAsync();
            await source.ReadStarted;
            var scanner = reader.Scanner;

            // act
            await dispose(reader);

            // assert
            scanner.Buffer.Should().NotBeEmpty("the read in progress still owns the block");
            source.OpenGate();
            await Record.ExceptionAsync(() => readTask.AsTask());
        }

        [Theory]
        [MemberData(nameof(Disposals), DisableDiscoveryEnumeration = true)]
        public async Task Dispose_WhileReadInProgress_CompletesReadWithObjectDisposed(string disposal, Func<IcalContentLineReader, Task> dispose)
        {
            // arrange
            _ = disposal;
            var source = new GatedTextReader(Crlf(LineA, LineB));
            var reader = new IcalContentLineReader(source);
            var readTask = reader.ReadLineAsync();
            await source.ReadStarted;
            await dispose(reader);
            source.OpenGate();

            // act
            Func<Task> testAction = () => readTask.AsTask();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Theory]
        [MemberData(nameof(Disposals), DisableDiscoveryEnumeration = true)]
        public async Task Dispose_WhileReadInProgress_ReturnsTheBlockOnceTheReadHasFinished(string disposal, Func<IcalContentLineReader, Task> dispose)
        {
            // arrange
            _ = disposal;
            var source = new GatedTextReader(Crlf(LineA, LineB));
            var reader = new IcalContentLineReader(source);
            var readTask = reader.ReadLineAsync();
            await source.ReadStarted;
            var scanner = reader.Scanner;
            await dispose(reader);
            source.OpenGate();

            // act
            await Record.ExceptionAsync(() => readTask.AsTask());

            // assert
            scanner.Buffer.Should().BeEmpty("the block is returned once the read has finished");
        }

        [Fact]
        public async Task DisposeAsync_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA));
            await reader.ReadLineAsync();
            await reader.DisposeAsync();

            // act
            Func<Task> testAction = () => reader.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DisposeAsync_AfterDispose_DoesNotThrow()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA));
            reader.Dispose();

            // act
            Func<Task> testAction = () => reader.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DisposeAsync_InternalStream_IsReleasedAndCallerStreamLeftOpen()
        {
            // arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Crlf(LineA))))
            {
                var reader = new IcalContentLineReader(stream);
                await reader.ReadLineAsync();
                var scanner = reader.Scanner;

                // act
                await reader.DisposeAsync();

                // assert
                scanner.Buffer.Should().BeEmpty();
                stream.CanRead.Should().BeTrue();
            }
        }

        [Theory]
        [MemberData(nameof(MembersAfterDisposeAsync), DisableDiscoveryEnumeration = true)]
        public async Task Member_AfterDisposeAsync_ThrowsObjectDisposedSynchronously(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA));
            await reader.DisposeAsync();

            // act
            Action testAction = () => member(reader);

            // assert
            testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineReader));
        }

        [Fact]
        public async Task ReadLineAsync_DisposedAfterCancellation_ThrowsObjectDisposed()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA));
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                cancellationTokenSource.Cancel();
                _ = reader.ReadLineAsync(cancellationTokenSource.Token);
                await reader.DisposeAsync();

                // act
                Action testAction = () => reader.ReadLineAsync();

                // assert
                testAction.Should().Throw<ObjectDisposedException>();
            }
        }

        [Fact]
        public async Task ReadLinesAsync_DisposedDuringEnumeration_ThrowsObjectDisposed()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, LineB));
            var enumerator = reader.ReadLinesAsync().GetAsyncEnumerator();
            await AsyncDisposal.UsingAsync(enumerator, async () =>
            {
                await enumerator.MoveNextAsync();
                await reader.DisposeAsync();

                // act
                Func<Task> testAction = () => enumerator.MoveNextAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<ObjectDisposedException>();
            });
        }

        #endregion

        private static IcalContentLineReader CreateAsyncOnlyReader(string text, int bufferSize = 16 * 1024)
        {
            return new IcalContentLineReader(new AsyncOnlyTextReader(text, CharactersPerRead), new IcalOptions(), bufferSize);
        }

        private static Task DisposeSynchronously(IcalContentLineReader reader)
        {
            reader.Dispose();
            return Task.CompletedTask;
        }

        private static async Task<List<IcalContentLine>> ReadAllAsync(IcalContentLineReader reader)
        {
            return await AsyncEnumerables.ToListAsync(reader.ReadLinesAsync());
        }

        /// <summary>
        /// Reads every line through the view that <see cref="IcalContentLineReader.ReadContentLineAsync"/> exposes.
        /// </summary>
        private static async Task<List<LineSnapshot>> ReadAllViewsAsync(IcalContentLineReader reader)
        {
            var views = new List<LineSnapshot>();
            while (await reader.ReadContentLineAsync())
            {
                views.Add(LineSnapshot.FromView(reader.CurrentLine));
            }

            return views;
        }

        /// <summary>
        /// Reads a line asynchronously, the next synchronously, and the two after that asynchronously.
        /// </summary>
        private static async Task<IcalContentLine[]> ReadAsyncThenSyncAsync(IcalContentLineReader reader)
        {
            var first = await reader.ReadLineAsync();
            var second = reader.ReadLine();
            var third = await reader.ReadLineAsync();
            var end = await reader.ReadLineAsync();
            return new[] { first, second, third, end };
        }
    }
}
