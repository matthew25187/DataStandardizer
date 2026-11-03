using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the test infrastructure for the writers.
    /// </summary>
    public class MessageWritesTests
    {
        private static readonly byte[] OneByte = new byte[1];

        [Fact]
        public void NextBoundary_CalledTwice_GivesTheFirstAndSecond()
        {
            // arrange
            var next = MessageWrites.NextBoundary();

            // act
            var testResult = new[] { next(), next() };

            // assert
            testResult.Should().Equal(MessageWrites.FirstBoundary, MessageWrites.SecondBoundary);
        }

        [Fact]
        public void SameBoundary_CalledTwice_GivesTheSameBoundary()
        {
            // arrange
            var next = MessageWrites.SameBoundary("b");

            // act
            var testResult = new[] { next(), next() };

            // assert
            testResult.Should().Equal("b", "b");
        }

        [Fact]
        public void IgnoreFailure_FileException_IsSwallowed()
        {
            // act
            Action testAction = () => MessageWrites.IgnoreFailure(() => throw new ImfFileException());

            // assert
            testAction.Should().NotThrow();
        }

        [Fact]
        public void IgnoreFailure_OtherException_IsThrown()
        {
            // act
            Action testAction = () => MessageWrites.IgnoreFailure(() => throw new ArgumentException());

            // assert
            testAction.Should().Throw<ArgumentException>();
        }

        [Fact]
        public async Task IgnoreFailureAsync_Canceled_IsSwallowed()
        {
            // act
            Func<Task> testAction = () => MessageWrites.IgnoreFailureAsync(() => throw new OperationCanceledException());

            // assert
            await testAction.Should().NotThrowAsync();
        }

        [Fact]
        public void BuildFromRead_MultipartMessage_GivesASourceThatWritesTheSameBytes()
        {
            // arrange
            const string message = "MIME-Version: 1.0\r\nContent-Type: multipart/mixed; boundary=b\r\n\r\npre\r\n--b\r\n\r\nx\r\n--b--\r\npost";

            // act
            var testResult = MessageWrites.Rewrite(message, new ImfOptions());

            // assert
            testResult.Should().Be(message);
        }

        [Fact]
        public void ReadUnfoldedFields_FoldedField_GivesItsNameAndBody()
        {
            // act
            var testResult = MessageWrites.ReadUnfoldedFields("A: 1\r\n 2\r\n");

            // assert
            testResult.Should().Equal("A: 1 2");
        }

        [Fact]
        public void ReadOutline_MessageText_DescribesItsEvents()
        {
            // act
            var testResult = MessageWrites.ReadOutline("A: 1\r\n\r\nx");

            // assert
            testResult.Should().Equal("start /", "field / A", "type / text/plain", "body / [x]", "end /");
        }

        [Fact]
        public void Write_ModeWriteStreamForAsynchronousWrites_Throws()
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);

            // act
            Action testAction = () => stream.Write(OneByte, 0, 1);

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(WriteStreamMessages.SynchronousWrite);
        }

        [Fact]
        public async Task WriteAsync_ModeWriteStreamForSynchronousWrites_Throws()
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.SynchronousOnly);

            // act
            Func<Task> testAction = () => stream.WriteAsync(OneByte, 0, 1);

            // assert
            await testAction.Should().ThrowAsync<InvalidOperationException>().WithMessage(WriteStreamMessages.AsynchronousWrite);
        }

        [Fact]
        public async Task WriteAsync_ModeWriteStreamForAsynchronousWrites_KeepsTheBytes()
        {
            // arrange
            var stream = new ModeWriteStream(BlockStreamMode.AsynchronousOnly);

            // act
            await stream.WriteAsync(OneByte, 0, 1);

            // assert
            stream.Length.Should().Be(1);
        }

        [Fact]
        public void Report_RecordingProgress_KeepsTheLastAndTheCount()
        {
            // arrange
            var progress = new RecordingProgress();
            progress.Report(new ImfWriteProgress(1, 0, 0));

            // act
            progress.Report(new ImfWriteProgress(2, 0, 0));

            // assert
            progress.ReportCount.Should().Be(2);
            progress.Last.Should().Be(new ImfWriteProgress(2, 0, 0));
        }

        [Fact]
        public void CanSeek_ForwardOnlyStream_IsFalse()
        {
            // arrange
            var stream = new ForwardOnlyStream(OneByte);

            // act
            var testResult = stream.CanSeek;

            // assert
            testResult.Should().BeFalse();
        }

        [Fact]
        public void Equals_ProgressWithTheSameCounts_IsTrue()
        {
            // arrange
            var left = new ImfWriteProgress(1, 2, 3);

            // act
            var testResult = left.Equals(new ImfWriteProgress(1, 2, 3));

            // assert
            testResult.Should().BeTrue();
        }

        [Fact]
        public void ToString_Progress_GivesTheCounts()
        {
            // arrange
            var progress = new ImfWriteProgress(1, 2, 3);

            // act
            var testResult = progress.ToString();

            // assert
            testResult.Should().Be("FieldsWritten = 1, EntitiesWritten = 2, BytesWritten = 3");
        }

        [Fact]
        public void CopyWithHeaderWriter_MessageWithABody_GivesTheSameBytes()
        {
            // arrange
            const string message = "A: 1\r\n\r\nbody";

            // act
            var testResult = MessageWrites.CopyWithHeaderWriter(message);

            // assert
            testResult.Should().Be(message);
        }

        [Fact]
        public void ReadContainerDecodedBody_LineFeedMessage_GivesTheBody()
        {
            // act
            var testResult = MessageWrites.ReadContainerDecodedBody("A: 1\n\nx", new ImfOptions());

            // assert
            testResult.Should().Be("x");
        }
    }
}
