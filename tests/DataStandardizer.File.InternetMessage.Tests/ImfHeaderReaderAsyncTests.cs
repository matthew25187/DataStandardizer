using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="ImfHeaderReader"/>.  The streams used can only be read asynchronously, so these tests also
    /// show that the async path does no synchronous I/O.
    /// </summary>
    public class ImfHeaderReaderAsyncTests
    {
        private const string OneField = "A:1\r\n";
        private const string RejectedField = ":1\r\n";

        public static TheoryData<Func<ImfHeaderReader, object>> Reads => new TheoryData<Func<ImfHeaderReader, object>>
        {
            reader => reader.ReadField(),
            reader => reader.ReadFieldAsync(),
        };

        [Theory]
        [MemberData(nameof(ImfHeaderReaderTests.WellFormedSections), MemberType = typeof(ImfHeaderReaderTests))]
        public async Task ReadFieldAsync_WellFormedSection_GivesEachField(string section, FieldSnapshot[] expected)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(section), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfHeaderReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await HeaderSections.ReadAllAsync(reader);

                // assert
                testResult.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
            });
        }

        [Theory]
        [ClassData(typeof(SplitSections))]
        public async Task ReadFieldAsync_SplitAcrossRefills_GivesTheSameFields(int firstReadLength, int bufferSize)
        {
            // arrange
            var stream = new BlockStream(SplitSections.Section, BlockStreamMode.AsynchronousOnly, firstReadLength);
            var reader = new ImfHeaderReader(stream, new ImfOptions(), bufferSize, false);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await HeaderSections.ReadAllAsync(reader);

                // assert
                testResult.Should().BeEquivalentTo(SplitSections.ExpectedFields, options => options.WithStrictOrdering());
            });
        }

        [Fact]
        public async Task ReadFieldAsync_FieldAlreadyInTheBlock_CompletesSynchronously()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(OneField + OneField + OneField), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfHeaderReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadFieldAsync();

                // act
                var testResult = reader.ReadFieldAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
            });
        }

        [Fact]
        public async Task ReadFieldAsync_RejectedFieldInTheBlock_ReportsItThroughTheTask()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(OneField + RejectedField + OneField), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfHeaderReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadFieldAsync();

                // act
                var testResult = reader.ReadFieldAsync();

                // assert
                testResult.IsFaulted.Should().BeTrue();
            });
        }

        [Fact]
        public async Task ReadFieldAsync_RejectedFieldAfterRefill_ReportsItThroughTheTask()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(RejectedField), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfHeaderReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => reader.ReadFieldAsync().AsTask();

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
            });
        }

        [Fact]
        public async Task ReadFieldAsync_StreamFails_ReportsItThroughTheTask()
        {
            // arrange
            var reader = new ImfHeaderReader(new FailingStream());
            await AsyncDisposal.UsingAsync(reader, () =>
            {
                // act
                var testResult = reader.ReadFieldAsync();

                // assert
                testResult.IsFaulted.Should().BeTrue();
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadFieldAsync_TokenAlreadyCanceled_ReturnsACanceledTask()
        {
            // arrange
            var reader = new ImfHeaderReader(new GatedStream());
            await AsyncDisposal.UsingAsync(reader, () =>
            {
                // act
                var testResult = reader.ReadFieldAsync(new CancellationToken(true));

                // assert
                testResult.IsCanceled.Should().BeTrue();
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadFieldAsync_CanceledDuringARead_CancelsTheTask()
        {
            // arrange
            var reader = new ImfHeaderReader(new GatedStream());
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                using (var cancellation = new CancellationTokenSource())
                {
                    var read = reader.ReadFieldAsync(cancellation.Token).AsTask();
                    cancellation.Cancel();

                    // act
                    Func<Task> testAction = () => read;

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Theory]
        [MemberData(nameof(Reads))]
        public async Task Read_AfterACanceledRead_ThrowsInvalidOperationException(Func<ImfHeaderReader, object> read)
        {
            // arrange
            var reader = new ImfHeaderReader(new GatedStream());
            await AsyncDisposal.UsingAsync(reader, () =>
            {
                _ = reader.ReadFieldAsync(new CancellationToken(true));

                // act
                Action testAction = () => read(reader);

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                return Task.CompletedTask;
            });
        }

        [Theory]
        [MemberData(nameof(Reads))]
        public async Task Read_AnotherReadInProgress_ThrowsInvalidOperationException(Func<ImfHeaderReader, object> read)
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfHeaderReader(stream);
            var readInProgress = reader.ReadFieldAsync().AsTask();

            // act
            Action testAction = () => read(reader);

            // assert
            testAction.Should().Throw<InvalidOperationException>();
            stream.OpenGate();
            await readInProgress;
            await reader.DisposeAsync();
        }

        [Fact]
        public async Task ReadFieldAsync_DisposedWhileInProgress_CompletesWithObjectDisposedException()
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfHeaderReader(stream);
            var read = reader.ReadFieldAsync().AsTask();
            reader.Dispose();
            stream.OpenGate();

            // act
            Func<Task> testAction = () => read;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task ReadFieldAsync_ReaderDisposed_ThrowsObjectDisposedException()
        {
            // arrange
            var reader = new ImfHeaderReader(new MemoryStream());
            await reader.DisposeAsync();

            // act
            Action testAction = () => _ = reader.ReadFieldAsync();

            // assert
            testAction.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task DisposeAsync_CalledAfterDispose_DoesNotThrow()
        {
            // arrange
            var reader = new ImfHeaderReader(new MemoryStream());
            reader.Dispose();

            // act
            Func<Task> testAction = () => reader.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Fact]
        public async Task DisposeAsync_CallerSuppliedStream_LeavesItOpen()
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfHeaderReader(stream);

            // act
            await reader.DisposeAsync();

            // assert
            stream.IsDisposed.Should().BeFalse();
        }

        [Fact]
        public async Task ReadField_AfterAnAsynchronousRead_GoesOnFromTheNextField()
        {
            // arrange
            var reader = new ImfHeaderReader(new MemoryStream(Octets.Of("A:1\r\nB:2\r\n")));
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadFieldAsync();

                // act
                var testResult = reader.ReadField();

                // assert
                testResult.Value.GetName().Should().Be("B");
            });
        }
    }
}
