using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DataStandardizer.Communication.Mime;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the asynchronous members of <see cref="ImfEntityWriter"/>.  The streams used can only be written asynchronously, and the content streams only
    /// read asynchronously where they can be, so these tests also show that the async path does no synchronous I/O.
    /// </summary>
    public class ImfEntityWriterAsyncTests
    {
        private const string BodyText = "caf\x00C3\x00A9\r\n";
        private const int BlockSizeOfOne = 1;
        private const int DefaultBlockSize = 4096;

        // The most the writer hands to the stream at once when it encodes a body, and an input block whose base64 is longer than that.
        private const int EncodedBlockSize = 16 * 1024;
        private const int LargeBlockSize = 64 * 1024;
        private const string OneByteBody = "x";

        public static TheoryData<string, string> RoundTrips => ImfEntityWriterTests.RoundTrips;

        public static TheoryData<byte[], ImfOptions, string> AllowedEncodings => ImfEntityWriterTests.AllowedEncodings;

        public static TheoryData<int> BlockSizes => new TheoryData<int> { BlockSizeOfOne, 3, DefaultBlockSize };

        [Theory]
        [MemberData(nameof(RoundTrips))]
        public async Task WriteMessageAsync_ContentInMemory_GivesTheSameBytesAsTheSynchronousWrite(string description, string content)
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(content));
            var expected = MessageWrites.Write(source);

            // act
            var testResult = await MessageWrites.WriteAsync(source, new ImfOptions(), BlockSizeOfOne);

            // assert
            testResult.Should().Be(expected, description);
        }

        [Theory]
        [MemberData(nameof(AllowedEncodings))]
        public async Task WriteMessageAsync_EncodingAllowedByOptions_GivesTheSameBytesAsTheSynchronousWrite(byte[] content, ImfOptions options, string expected)
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, content);

            // act
            var testResult = await MessageWrites.WriteAsync(source, options, DefaultBlockSize);

            // assert
            testResult.Should().Be(expected);
        }

        [Theory]
        [MemberData(nameof(BlockSizes))]
        public async Task WriteMessageAsync_ContentInAStream_GivesTheSameBytesAsTheSynchronousWrite(int blockSize)
        {
            // arrange
            var bytes = Octets.Of(BodyText);
            var expected = MessageWrites.Write(ImfEntitySource.FromContent(MimeMediaType.TextPlain, bytes));
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new MemoryStream(bytes));

            // act
            var testResult = await MessageWrites.WriteAsync(source, new ImfOptions(), blockSize);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public async Task WriteMessageAsync_ForwardOnlyContentReadOnlyAsynchronously_EncodesIt()
        {
            // arrange
            var stream = new BlockStream(Octets.Of("abc"), BlockStreamMode.AsynchronousOnly);
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, stream);
            source.TransferEncoding = MimeTransferEncoding.Base64;

            // act
            var testResult = await MessageWrites.WriteAsync(source, new ImfOptions(), DefaultBlockSize);

            // assert
            testResult.Should().EndWith("YWJj");
        }

        [Fact]
        public async Task WriteMessageAsync_RawForwardOnlyStreamReadOnlyAsynchronously_CopiesIt()
        {
            // arrange
            var stream = new BlockStream(Octets.Of(EntityMessages.Rfc2046SimpleBoundaryExample), BlockStreamMode.AsynchronousOnly);
            var source = ImfEntitySource.FromRaw(stream);

            // act
            var testResult = await MessageWrites.WriteAsync(source, new ImfOptions(), DefaultBlockSize);

            // assert
            testResult.Should().Be(EntityMessages.Rfc2046SimpleBoundaryExample);
        }

        [Fact]
        public async Task WriteMessageAsync_Multipart_GivesTheSameBytesAsTheSynchronousWrite()
        {
            // arrange
            var source = ImfEntitySource.FromParts(MimeMediaType.MultipartMixed, new[]
            {
                ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(BodyText)),
                ImfEntitySource.FromMessage(ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of("--=_1"))),
                ImfEntitySource.FromRaw(Octets.Of("A: 1\r\n")),
            });
            var expected = MessageWrites.Write(source);

            // act
            var testResult = await MessageWrites.WriteAsync(source, new ImfOptions(), DefaultBlockSize);

            // assert
            testResult.Should().Be(expected);
        }

        [Fact]
        public async Task WriteMessageAsync_QuotedPrintableOutputThatHoldsAGivenBoundary_FailsThroughTheTask()
        {
            // arrange
            var part = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new MemoryStream(Octets.Of("--given")));
            part.TransferEncoding = MimeTransferEncoding.QuotedPrintable;
            var source = ImfEntitySource.FromParts(MimeMediaType.Parse("multipart/mixed;boundary=given"), new[] { part });
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = MessageWrites.CreateWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteMessageAsync(source);
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<ImfFileException>();
                stream.Length.Should().Be(0);
            });
        }

        [Fact]
        public async Task WriteMessageAsync_Progress_ReportsTheCountsOfTheOutput()
        {
            // arrange
            var progress = new RecordingProgress();
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, Octets.Of(OneByteBody));
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);
            var writer = MessageWrites.CreateWriter(stream);

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteMessageAsync(source, progress));

            // assert
            progress.Last.Should().Be(new ImfWriteProgress(2, 1, stream.Length));
        }

        [Fact]
        public async Task WriteMessageAsync_CanceledToken_FailsThroughTheTask()
        {
            // arrange
            var canceled = new CancellationToken(true);
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteMessageAsync(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)), null, canceled);
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            });
        }

        [Fact]
        public async Task WriteMessage_AfterACanceledWrite_ThrowsThatTheWriterCannotContinue()
        {
            // arrange
            var canceled = new CancellationToken(true);
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await MessageWrites.IgnoreFailureAsync(() => writer.WriteMessageAsync(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)), null, canceled));

                // act
                Action testAction = () => writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task WriteMessageAsync_ContentStreamFailsPartWay_LeavesTheWriterUnableToContinue()
        {
            // arrange
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new FailingStream());
            source.TransferEncoding = MimeTransferEncoding.Base64;
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await MessageWrites.IgnoreFailureAsync(() => writer.WriteMessageAsync(source));

                // act
                Action testAction = () => writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task WriteMessageAsync_ProgressThrows_FailsThroughTheTask()
        {
            // arrange
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.WriteMessageAsync(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)), new ThrowingProgress());
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<InvalidOperationException>().WithMessage(ThrowingProgress.FailureMessage);
            });
        }

        [Fact]
        public async Task WriteMessageAsync_NullMessage_ThrowsFromTheCall()
        {
            // arrange
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, () =>
            {
                // act
                Action testAction = () => _ = writer.WriteMessageAsync(null);

                // assert
                testAction.Should().Throw<ArgumentNullException>().WithParameterName("message");
                return Task.CompletedTask;
            });
        }

        [Fact]
        public async Task WriteMessage_WhileAnAsynchronousWriteIsInProgress_Throws()
        {
            // arrange
            var stream = new GatedWriteStream();
            var writer = MessageWrites.CreateWriter(stream);
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                var pending = writer.WriteMessageAsync(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

                // act
                Action testAction = () => writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

                // assert
                testAction.Should().Throw<InvalidOperationException>();
                stream.OpenGate();
                await pending;
                Octets.ToText(stream.ToArray()).Should().Be(OneByteBody);
            });
        }

        [Fact]
        public async Task DisposeAsync_WhileAWriteIsInProgress_TheWriteFailsWithObjectDisposed()
        {
            // arrange
            var stream = new GatedWriteStream();
            var writer = MessageWrites.CreateWriter(stream);
            var pending = writer.WriteMessageAsync(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));
            var disposing = writer.DisposeAsync().AsTask();
            stream.OpenGate();
            await disposing;

            // act
            Func<Task> testAction = () => pending;

            // assert
            await testAction.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public async Task DisposeAsync_WhileAWriteIsInProgress_DoesNotCompleteUntilTheWriteHasFinished()
        {
            // arrange
            var stream = new GatedWriteStream();
            var writer = MessageWrites.CreateWriter(stream);
            _ = writer.WriteMessageAsync(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

            // act
            var disposing = writer.DisposeAsync().AsTask();

            // assert
            disposing.IsCompleted.Should().BeFalse();
            stream.OpenGate();
            await disposing;
        }

        [Fact]
        public async Task WriteMessage_AfterAFlushFailure_ThrowsThatTheWriterCannotContinue()
        {
            // arrange
            var writer = MessageWrites.CreateWriter(new FailingWriteStream());
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                await MessageWrites.IgnoreFailureAsync(() => writer.FlushAsync());

                // act
                Action testAction = () => writer.WriteMessage(ImfEntitySource.FromRaw(Octets.Of(OneByteBody)));

                // assert
                testAction.Should().Throw<InvalidOperationException>();
            });
        }

        [Fact]
        public async Task WriteMessageAsync_InputBlockLargerThanTheEncodedBlock_ReportsEachBlockOfOutput()
        {
            // arrange
            var progress = new RecordingProgress();
            var source = ImfEntitySource.FromContent(MimeMediaType.TextPlain, new byte[LargeBlockSize]);
            source.TransferEncoding = MimeTransferEncoding.Base64;
            var writer = new ImfEntityWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly), new ImfOptions(), LargeBlockSize, false, MessageWrites.NextBoundary());

            // act
            await AsyncDisposal.UsingAsync(writer, () => writer.WriteMessageAsync(source, progress));

            // assert
            progress.LargestStep.Should().BeLessOrEqualTo(EncodedBlockSize);
        }

        [Fact]
        public async Task FlushAsync_Stream_FlushesAsynchronously()
        {
            // arrange
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                Func<Task> testAction = () => writer.FlushAsync();

                // assert
                await testAction.Should().NotThrowAsync();
            });
        }

        [Fact]
        public async Task FlushAsync_CanceledToken_FailsThroughTheTask()
        {
            // arrange
            var canceled = new CancellationToken(true);
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await AsyncDisposal.UsingAsync(writer, async () =>
            {
                // act
                var task = writer.FlushAsync(canceled);
                Func<Task> testAction = () => task;

                // assert
                await testAction.Should().ThrowAsync<OperationCanceledException>();
            });
        }

        [Fact]
        public async Task DisposeAsync_Twice_DoesNotThrow()
        {
            // arrange
            var writer = MessageWrites.CreateWriter(new ModeWriteStream(BlockStreamMode.AsynchronousOnly));
            await writer.DisposeAsync();

            // act
            Func<Task> testAction = () => writer.DisposeAsync().AsTask();

            // assert
            await testAction.Should().NotThrowAsync();
        }
    }
}
