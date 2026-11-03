using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="ImfEntityReader"/>.  The streams used can only be read asynchronously, so these tests also show
    /// that the async path does no synchronous I/O.
    /// </summary>
    public class ImfEntityReaderAsyncTests
    {
        private const string OneFieldMessage = "A:1\r\nB:2\r\nC:3\r\n";
        private const string MissingBoundary = "Content-Type: multipart/mixed\r\n\r\n";

        public static TheoryData<string, string[]> ValidMessages => EntityMessages.Valid;

        public static TheoryData<string, string> InvalidMessages => ImfEntityReaderTests.InvalidMessages;

        public static TheoryData<string, string> Decoded => ImfEntityReaderTests.Decoded;

        [Theory]
        [MemberData(nameof(ValidMessages))]
        public async Task ReadEventAsync_ValidMessage_GivesTheEvents(string message, string[] expected)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(message), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await EntityEvents.ReadOutlineAsync(reader);

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Theory]
        [MemberData(nameof(ValidMessages))]
        public async Task ReadEventAsync_OneByteAtATime_GivesTheSameEvents(string message, string[] expected)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(message), BlockStreamMode.AsynchronousOnly, 1, 1);
            var reader = new ImfEntityReader(stream, new ImfOptions(), 1, false);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await EntityEvents.ReadOutlineAsync(reader);

                // assert
                testResult.Should().Equal(expected);
            });
        }

        [Theory]
        [ClassData(typeof(SplitMessages))]
        public async Task ReadEventAsync_SplitAcrossRefills_GivesTheSameEvents(int firstReadLength, int bufferSize)
        {
            // arrange
            var stream = new BlockStream(SplitMessages.Message, BlockStreamMode.AsynchronousOnly, firstReadLength);
            var reader = new ImfEntityReader(stream, new ImfOptions(), bufferSize, false);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await EntityEvents.ReadOutlineAsync(reader);

                // assert
                testResult.Should().Equal(SplitMessages.ExpectedEvents);
            });
        }

        [Theory]
        [MemberData(nameof(InvalidMessages))]
        public async Task ReadEventAsync_InvalidMessage_ThrowsWithTheEntityPath(string message, string expectedPath)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(message), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                Func<Task> testAction = () => EntityEvents.ReadToEndAsync(reader);

                // assert
                (await testAction.Should().ThrowAsync<ImfFileException>())
                    .Which.Data[ImfFileException.DataItemName.EntityPath].Should().Be(expectedPath);
            });
        }

        [Theory]
        [MemberData(nameof(Decoded))]
        public async Task ReadEventAsync_DecodingOn_GivesTheDecodedBodies(string message, string expected)
        {
            // arrange
            var stream = new BlockStream(Octets.Of(message), BlockStreamMode.AsynchronousOnly, 1, 1);
            var reader = new ImfEntityReader(stream, new ImfOptions { DecodeTransferEncoding = true }, 1, false);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = await DecodedBodies.ReadAsync(reader);

                // assert
                testResult.Should().Be(expected);
            });
        }

        [Fact]
        public async Task ReadEventAsync_EventAlreadyInTheBlock_CompletesSynchronously()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(OneFieldMessage), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadEventAsync();
                await reader.ReadEventAsync();

                // act
                var testResult = reader.ReadEventAsync();

                // assert
                testResult.IsCompletedSuccessfully.Should().BeTrue();
            });
        }

        [Fact]
        public async Task ReadEventAsync_ErrorInTheBlock_ReportsItThroughTheTask()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(MissingBoundary), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadEventAsync();
                await reader.ReadEventAsync();

                // act
                var testResult = reader.ReadEventAsync();

                // assert
                testResult.IsFaulted.Should().BeTrue();
                await Task.CompletedTask;
                testResult.AsTask().Exception.InnerException.Should().BeOfType<ImfFileException>();
            });
        }

        [Fact]
        public async Task ReadEventAsync_StreamFails_ReportsItThroughTheTask()
        {
            // arrange
            var reader = new ImfEntityReader(new FailingStream());
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadEventAsync();

                // act
                var testResult = reader.ReadEventAsync();

                // assert
                testResult.IsFaulted.Should().BeTrue();
                await Task.CompletedTask;
                testResult.AsTask().Exception.InnerException.Should().BeOfType<IOException>();
            });
        }

        [Fact]
        public async Task ReadEventAsync_AlreadyCanceled_ReportsItThroughTheTask()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(OneFieldMessage), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                // act
                var testResult = reader.ReadEventAsync(new CancellationToken(true));

                // assert
                testResult.IsCanceled.Should().BeTrue();
                await Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadEventAsync_AfterACancel_Throws()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(OneFieldMessage), BlockStreamMode.AsynchronousOnly);
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                _ = reader.ReadEventAsync(new CancellationToken(true));

                // act
                Func<ValueTask<ImfEntityEvent?>> testAction = () => reader.ReadEventAsync();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                await Task.CompletedTask;
            });
        }

        [Fact]
        public async Task ReadEventAsync_CanceledDuringARead_CompletesTheTaskAsCanceled()
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadEventAsync();
                using (var cancellation = new CancellationTokenSource())
                {
                    var pendingRead = reader.ReadEventAsync(cancellation.Token);
                    cancellation.Cancel();

                    // act
                    Func<Task> testAction = () => pendingRead.AsTask();

                    // assert
                    await testAction.Should().ThrowAsync<OperationCanceledException>();
                }
            });
        }

        [Fact]
        public async Task ReadEvent_WhileAReadIsInProgress_Throws()
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfEntityReader(stream);
            await AsyncDisposal.UsingAsync(reader, async () =>
            {
                await reader.ReadEventAsync();
                var pendingRead = reader.ReadEventAsync();

                // act
                Action testAction = () => reader.ReadEvent();

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                stream.OpenGate();
                await pendingRead;
            });
        }

        [Fact]
        public async Task Dispose_WhileAReadIsInProgress_CompletesTheReadWithObjectDisposedException()
        {
            // arrange
            var stream = new GatedStream();
            var reader = new ImfEntityReader(stream);
            await reader.ReadEventAsync();
            var pendingRead = reader.ReadEventAsync();
            reader.Dispose();
            stream.OpenGate();

            // act
            Func<Task> testAction = () => pendingRead.AsTask();

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task DisposeAsync_CalledTwice_DoesNotThrow()
        {
            // arrange
            var reader = new ImfEntityReader(new MemoryStream());
            await reader.DisposeAsync();

            // act
            Func<Task> testAction = () => reader.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }
    }
}
