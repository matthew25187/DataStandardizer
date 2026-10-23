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
                    var expected = new List<LineSnapshot>();
                    while (syncReader.ReadContentLine())
                    {
                        expected.Add(LineSnapshot.FromView(syncReader.CurrentLine));
                    }

                    // act
                    var testResult = new List<LineSnapshot>();
                    while (await asyncReader.ReadContentLineAsync())
                    {
                        testResult.Add(LineSnapshot.FromView(asyncReader.CurrentLine));
                    }

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
            using (var syncReader = CreateStreamReader(data, streamBufferSize: 5, bufferSize: bufferSize))
            {
                var asyncReader = new IcalContentLineReader(new AsyncOnlyStream(data), new IcalOptions(), 5, bufferSize);
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
            // "€" is E2 82 AC and "😀" is F0 9F 98 80.  Fold after the first octet of one and inside the other twice.
            var data = new[]
            {
                Encoding.UTF8.GetBytes("SUMMARY:caf"), new byte[] { 0xE2 }, Encoding.ASCII.GetBytes("\r\n "), new byte[] { 0x82, 0xAC },
                Encoding.UTF8.GetBytes(" "), new byte[] { 0xF0, 0x9F }, Encoding.ASCII.GetBytes("\r\n\t"), new byte[] { 0x98 }, Encoding.ASCII.GetBytes("\r\n "), new byte[] { 0x80 },
                Encoding.UTF8.GetBytes("!\r\nX-NEXT:é\r\n")
            }.SelectMany(part => part).ToArray();
            var reader = new IcalContentLineReader(new AsyncOnlyStream(data), new IcalOptions(), streamBufferSize, bufferSize);
            try
            {
                // act
                var testResult = await ReadAllAsync(reader);

                // assert
                testResult.Should().HaveCount(2);
                testResult[0].Value.Should().Be("caf€ 😀!");
                testResult[1].Value.Should().Be("é");
                testResult[1].PhysicalLineNumber.Should().Be(5);
                reader.PhysicalLinesRead.Should().Be(5);
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        [Fact]
        public async Task ReadLineAsync_SyntaxError_FaultsTaskAndReaderCanContinue()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf("A:1", "BAD", "C:3"));
            try
            {
                (await reader.ReadLineAsync()).Name.Should().Be("A");

                // act
                var readTask = reader.ReadLineAsync();
                Func<Task> testAction = async () => await readTask;

                // assert
                await testAction.Should().ThrowAsync<IcalFileException>();
                (await reader.ReadLineAsync()).Name.Should().Be("C");
                (await reader.ReadLineAsync()).Should().BeNull();
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
                Func<Task> firstAction = async () => await reader.ReadLineAsync();
                await firstAction.Should().ThrowAsync<IcalFileException>();

                // act
                Action readLineAction = () => reader.ReadLineAsync();
                Action readContentLineAction = () => reader.ReadContentLineAsync();

                // assert
                readLineAction.Should().Throw<InvalidOperationException>();
                readContentLineAction.Should().Throw<InvalidOperationException>();
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
            var source = new GatedTextReader(Crlf("A:1", "B:2", "C:3"));
            var reader = new IcalContentLineReader(source);
            try
            {
                source.OpenGate();
                (await reader.ReadContentLineAsync()).Should().BeTrue();
                source.CloseGate();

                // act
                var testResult = reader.ReadContentLineAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
                (await testResult).Should().BeTrue();
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

        [Fact]
        public async Task ReadLineAsync_WhileReadInProgress_ThrowsInvalidOperation()
        {
            // arrange
            var source = new GatedTextReader(Crlf("A:1", "B:2", "C:3"));
            var reader = new IcalContentLineReader(source);
            try
            {
                var firstRead = reader.ReadLineAsync();
                await source.ReadStarted;

                // act
                var testActions = new Action[]
                {
                    () => reader.ReadLineAsync(),
                    () => reader.ReadContentLineAsync(),
                    () => reader.ReadLine(),
                    () => reader.ReadContentLine()
                };

                // assert
                foreach (var testAction in testActions)
                {
                    testAction.Should().Throw<InvalidOperationException>().WithMessage("*in progress*");
                }

                source.OpenGate();
                (await firstRead).Name.Should().Be("A");
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
            var source = new GatedTextReader(Crlf("A:1", "B:2", "C:3"));
            var reader = new IcalContentLineReader(source);
            try
            {
                source.OpenGate();

                // act
                var first = await reader.ReadLineAsync();
                var second = reader.ReadLine();
                var third = await reader.ReadLineAsync();
                var end = await reader.ReadLineAsync();

                // assert
                new[] { first, second, third }.Select(line => line.Name).Should().Equal("A", "B", "C");
                end.Should().BeNull();
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        #endregion

        #region Cancellation

        [Fact]
        public async Task ReadLineAsync_CanceledBeforeRead_FaultsReader()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf("A:1", "B:2"));
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    cancellationTokenSource.Cancel();

                    // act
                    var readTask = reader.ReadLineAsync(cancellationTokenSource.Token);

                    // assert
                    readTask.IsCanceled.Should().BeTrue();
                    AssertFaultedByCancellation(reader);
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
        public async Task ReadLineAsync_CanceledWhileInProgress_FaultsReader()
        {
            // arrange
            var source = new GatedTextReader(Crlf("A:1", "B:2"));
            var reader = new IcalContentLineReader(source);
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var readTask = reader.ReadLineAsync(cancellationTokenSource.Token);
                    await source.ReadStarted;

                    // act
                    cancellationTokenSource.Cancel();
                    Func<Task> testAction = async () => await readTask;

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                    AssertFaultedByCancellation(reader);
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }
#endif

        [Fact]
        public async Task ReadLinesAsync_EnumeratorTokenCanceled_FaultsReader()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf("A:1", "B:2"));
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var enumerator = reader.ReadLinesAsync().GetAsyncEnumerator(cancellationTokenSource.Token);
                    try
                    {
                        (await enumerator.MoveNextAsync()).Should().BeTrue();
                        cancellationTokenSource.Cancel();

                        // act
                        Func<Task> testAction = async () => await enumerator.MoveNextAsync();

                        // assert
                        await testAction.Should().ThrowAsync<OperationCanceledException>();
                        AssertFaultedByCancellation(reader);
                    }
                    finally
                    {
                        await enumerator.DisposeAsync();
                    }
                }
            }
            finally
            {
                await reader.DisposeAsync();
            }
        }

        private static void AssertFaultedByCancellation(IcalContentLineReader reader)
        {
            Action readLineAsyncAction = () => reader.ReadLineAsync();
            Action readLineAction = () => reader.ReadLine();
            readLineAsyncAction.Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
            readLineAction.Should().Throw<InvalidOperationException>().WithMessage("*canceled*");
        }

        #endregion

        #region Disposal

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Dispose_WhileReadInProgress_CompletesReadWithObjectDisposed(bool disposeAsync)
        {
            // arrange
            var source = new GatedTextReader(Crlf("A:1", "B:2"));
            var reader = new IcalContentLineReader(source);
            var readTask = reader.ReadLineAsync();
            await source.ReadStarted;
            var scanner = ReaderInspector.GetScanner(reader);

            // act
            if (disposeAsync)
            {
                await reader.DisposeAsync();
            }
            else
            {
                reader.Dispose();
            }

            // assert
            scanner.Buffer.Should().NotBeEmpty("the read in progress still owns the block");
            source.OpenGate();
            Func<Task> testAction = async () => await readTask;
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
            scanner.Buffer.Should().BeEmpty("the block is returned once the read has finished");
        }

        [Fact]
        public async Task DisposeAsync_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf("A:1"));
            await reader.ReadLineAsync();
            await reader.DisposeAsync();

            // act
            Func<Task> testAction = async () => await reader.DisposeAsync();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DisposeAsync_AfterDispose_DoesNotThrow()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf("A:1"));
            reader.Dispose();

            // act
            Func<Task> testAction = async () => await reader.DisposeAsync();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DisposeAsync_InternalStream_IsReleasedAndCallerStreamLeftOpen()
        {
            // arrange
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Crlf("A:1"))))
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

        [Fact]
        public async Task AsyncMembers_AfterDisposeAsync_ThrowObjectDisposedSynchronously()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf("A:1"));
            await reader.DisposeAsync();

            // act
            var testActions = new Action[]
            {
                () => reader.ReadLineAsync(),
                () => reader.ReadContentLineAsync(),
                () => reader.ReadLinesAsync(),
                () => _ = reader.CurrentLine,
                () => _ = reader.LinesRead
            };

            // assert
            foreach (var testAction in testActions)
            {
                testAction.Should().Throw<ObjectDisposedException>().Which.ObjectName.Should().Be(nameof(IcalContentLineReader));
            }
        }

        [Fact]
        public async Task ReadLineAsync_DisposedAfterCancellation_ThrowsObjectDisposed()
        {
            // arrange
            var reader = CreateAsyncOnlyReader(Crlf("A:1"));
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
            var reader = CreateAsyncOnlyReader(Crlf("A:1", "B:2"));
            var enumerator = reader.ReadLinesAsync().GetAsyncEnumerator();
            try
            {
                await enumerator.MoveNextAsync();
                await reader.DisposeAsync();

                // act
                Func<Task> testAction = async () => await enumerator.MoveNextAsync();

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
            // A few characters per read, so a read ends inside lines, folds and line breaks.
            return new IcalContentLineReader(new AsyncOnlyTextReader(text, 7), new IcalOptions(), bufferSize);
        }

        private static async Task<List<IcalContentLine>> ReadAllAsync(IcalContentLineReader reader)
        {
            var lines = await AsyncEnumerables.ToListAsync(reader.ReadLinesAsync());

            return lines;
        }
    }
}
