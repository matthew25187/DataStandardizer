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
        public async Task ReadLinesAsync_EveryFixture_MatchesSync(string fixtureName, int bufferSize)
        {
            // arrange
            var text = RfcFixtures.All[fixtureName];
            using (var syncReader = CreateReader(text, bufferSize: bufferSize))
            {
                var asyncReader = CreateAsyncOnlyReader(text, bufferSize);
                try
                {
                    var expected = ReadAll(syncReader).Select(LineSnapshot.FromLine).ToList();

                    // act
                    var testResult = (await ReadAllAsync(asyncReader)).Select(LineSnapshot.FromLine).ToList();

                    // assert
                    testResult.Should().NotBeEmpty();
                    testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
                    asyncReader.LinesRead.Should().Be(syncReader.LinesRead);
                    asyncReader.PhysicalLinesRead.Should().Be(syncReader.PhysicalLinesRead);
                }
                finally
                {
                    await asyncReader.DisposeAsync();
                }
            }
        }

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.ConsistencyData), MemberType = typeof(IcalContentLineReaderTests))]
        public async Task ReadContentLineAsync_EveryFixture_ViewMatchesSync(string fixtureName, int bufferSize)
        {
            // arrange
            var text = RfcFixtures.All[fixtureName];
            using (var syncReader = CreateReader(text, bufferSize: bufferSize))
            {
                var asyncReader = CreateAsyncOnlyReader(text, bufferSize);
                try
                {
                    var expected = IcalContentLineReaderTests.ReadAllViews(syncReader);

                    // act
                    var testResult = await ReadAllViewsAsync(asyncReader);

                    // assert
                    testResult.Should().NotBeEmpty();
                    testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
                }
                finally
                {
                    await asyncReader.DisposeAsync();
                }
            }
        }

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.ConsistencyData), MemberType = typeof(IcalContentLineReaderTests))]
        public async Task ReadLinesAsync_EveryFixtureFromStream_MatchesSync(string fixtureName, int bufferSize)
        {
            // arrange
            var data = Encoding.UTF8.GetBytes(RfcFixtures.All[fixtureName]);
            using (var syncReader = CreateStreamReader(data, streamBufferSize: SmallStreamBufferSize, bufferSize: bufferSize))
            {
                var asyncReader = new IcalContentLineReader(new AsyncOnlyStream(data), new IcalOptions(), SmallStreamBufferSize, bufferSize);
                try
                {
                    var expected = ReadAll(syncReader).Select(LineSnapshot.FromLine).ToList();

                    // act
                    var testResult = (await ReadAllAsync(asyncReader)).Select(LineSnapshot.FromLine).ToList();

                    // assert
                    testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
                }
                finally
                {
                    await asyncReader.DisposeAsync();
                }
            }
        }

        [Theory]
        [MemberData(nameof(IcalContentLineReaderTests.StreamBufferSizeData), MemberType = typeof(IcalContentLineReaderTests))]
        public async Task ReadLineAsync_FoldInsideUtf8Sequence_RestoresCharacter(int streamBufferSize, int bufferSize)
        {
            // arrange
            var reader = new IcalContentLineReader(new AsyncOnlyStream(IcalContentLineReaderTests.FoldsInsideUtf8Sequences), new IcalOptions(),
                streamBufferSize, bufferSize);
            try
            {
                // act
                var testResult = await ReadAllAsync(reader);

                // assert
                testResult.Should().HaveCount(2);
                testResult[0].Value.Should().Be(IcalContentLineReaderTests.FoldsInsideUtf8SequencesValue);
                testResult[1].Value.Should().Be("\xE9");
                testResult[1].PhysicalLineNumber.Should().Be(5);
                reader.PhysicalLinesRead.Should().Be(5);
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadLineAsync_SyntaxError_FaultsTask()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, "BAD", LineC));
            try
            {
                await reader.ReadLineAsync();

                // act
                Func<Task> testAction = () => reader.ReadLineAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<IcalFileException>();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadLineAsync_AfterSyntaxError_ReadsTheNextLine()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, "BAD", LineC));
            try
            {
                await reader.ReadLineAsync();
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                var testResult = await reader.ReadLineAsync();

                // assert
                testResult.Name.Should().Be("C");
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadLineAsync_AfterTheLineAfterASyntaxError_ReturnsNull()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, "BAD", LineC));
            try
            {
                await reader.ReadLineAsync();
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());
                await reader.ReadLineAsync();

                // act
                var testResult = await reader.ReadLineAsync();

                // assert
                testResult.Should().BeNull();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadLineAsync_AfterLineBreakError_ThrowsInvalidOperation()
        {
            // arrange
            var reader = CreateAsyncOnlyReader("SUMMARY:a\nX-B:d\r\n");
            try
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                Action testAction = () => reader.ReadLineAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadContentLineAsync_AfterLineBreakError_ThrowsInvalidOperation()
        {
            // arrange
            var reader = CreateAsyncOnlyReader("SUMMARY:a\nX-B:d\r\n");
            try
            {
                await Record.ExceptionAsync(() => reader.ReadLineAsync().AsTask());

                // act
                Action testAction = () => reader.ReadContentLineAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadContentLineAsync_LineAlreadyBuffered_CompletesSynchronously()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB, LineC));
            var reader = new IcalContentLineReader(source);
            try
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
            }
            finally
            {
                await reader.DisposeAsync();
            }
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
            try
            {
                // act
                Action testAction = () => member(reader);

                // assert
                testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
            }
            finally
            {
                source.OpenGate();
                await firstRead;
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadLineAsync_OtherReadRejectedWhileInProgress_ReadsTheLine()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB, LineC));
            var reader = new IcalContentLineReader(source);
            try
            {
                var firstRead = reader.ReadLineAsync();
                await source.ReadStarted;
                Record.Exception(() => reader.ReadLine());
                source.OpenGate();

                // act
                var testResult = await firstRead;

                // assert
                testResult.Name.Should().Be("A");
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadLine_SyncAndAsyncOneAfterAnother_ReadsEveryLine()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB, LineC));
            var reader = new IcalContentLineReader(source);
            try
            {
                source.OpenGate();

                // act
                var testResult = await ReadAsyncThenSyncAsync(reader);

                // assert
                testResult.Select(line => line?.Name).Should().Equal("A", "B", "C", null);
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        #endregion

        #region Cancellation

        [Fact]
        public async Task ReadLineAsync_CanceledBeforeRead_IsCanceled()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, LineB));
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.Cancel();

                    // act
                    var testResult = reader.ReadLineAsync(cancellationTokenSource.Token);

                    // assert
                    testResult.IsCanceled.Should().BeTrue();
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Theory]
        [MemberData(nameof(MembersAfterCancellation), DisableDiscoveryEnumeration = true)]
        public async Task Member_AfterCancellationBeforeRead_ThrowsInvalidOperation(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf(LineA, LineB));
            try
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
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        // On .NET Framework, TextReader.ReadAsync takes no cancellation token, so a read in progress cannot be canceled.
#if NETCOREAPP2_1_OR_GREATER
        [Fact]
        public async Task ReadLineAsync_CanceledWhileInProgress_ThrowsOperationCanceled()
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB));
            var reader = new IcalContentLineReader(source);
            try
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
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Theory]
        [MemberData(nameof(MembersAfterCancellation), DisableDiscoveryEnumeration = true)]
        public async Task Member_AfterCancellationWhileInProgress_ThrowsInvalidOperation(Func<IcalContentLineReader, object> member)
        {
            // arrange
            var source = new GatedTextReader(Crlf(LineA, LineB));
            var reader = new IcalContentLineReader(source);
            try
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
            }
            finally
            {
                await reader.DisposeAsync();
            }
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
                try
                {
                    // act
                    Func<Task> testAction = () => enumerator.MoveNextAsync().AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
                finally
                {
                    await enumerator.DisposeAsync();
                    await reader.DisposeAsync();
                }
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
                try
                {
                    // act
                    Action testAction = () => member(reader);

                    // assert
                    testAction.Should().Throw<InvalidOperationException>().WithMessage(CanceledMessage);
                }
                finally
                {
                    await enumerator.DisposeAsync();
                    await reader.DisposeAsync();
                }
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
            var scanner = ReaderInspector.GetScanner(reader);

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
            var scanner = ReaderInspector.GetScanner(reader);
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
                var scanner = ReaderInspector.GetScanner(reader);

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
            try
            {
                await enumerator.MoveNextAsync();
                await reader.DisposeAsync();

                // act
                Func<Task> testAction = () => enumerator.MoveNextAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<ObjectDisposedException>();
            }
            finally
            {
                await enumerator.DisposeAsync();
            }
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
