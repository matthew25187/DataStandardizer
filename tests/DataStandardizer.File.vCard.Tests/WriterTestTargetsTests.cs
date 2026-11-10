using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// Tests of the test targets in <c>WriterTestTargets.cs</c> whose behaviour the writer tests rely on.
    /// </summary>
    public class WriterTestTargetsTests
    {
        private const string Text = "x";

        private static readonly char[] TextChars = Text.ToCharArray();

        private static readonly byte[] OneByte = { 1 };

        private const string FailureCallback = "failure callback";
        private const string ExceptionCreated = "exception created";
        private const string WriteCallback = "write callback";
        private const string FlushCallback = "flush callback";

        private const int NoFlushes = 0;
        private const int OneFlush = 1;

        #region FailingTextWriter

        [Fact]
        public void FailingTextWriter_WriteAsyncFromCall_ThrowsFromTheCall()
        {
            // arrange
            var text = new FailingTextWriter(() => new IOException(), FailureTiming.FromCall);

            // act
            Action testAction = () => text.WriteAsync(TextChars, 0, TextChars.Length);

            // assert
            testAction.Should().Throw<IOException>();
        }

        // Each timing that reports the failure by the task, with an exception, and the status the task ends with.
        public static TheoryData<FailureTiming, Func<Exception>, TaskStatus> TaskFailures => new TheoryData<FailureTiming, Func<Exception>, TaskStatus>
        {
            { FailureTiming.AsFaultedTask, () => new IOException(), TaskStatus.Faulted },
            { FailureTiming.AsFaultedTask, () => new OperationCanceledException(), TaskStatus.Faulted },
            { FailureTiming.AsCanceledTask, () => new IOException(), TaskStatus.Canceled },
            { FailureTiming.AfterYielding, () => new IOException(), TaskStatus.Faulted },
            { FailureTiming.AfterYielding, () => new OperationCanceledException(), TaskStatus.Canceled },
        };

        [Theory]
        [MemberData(nameof(TaskFailures), DisableDiscoveryEnumeration = true)]
        public async Task FailingTextWriter_WriteAsyncByTask_EndsWithStatus(FailureTiming timing, Func<Exception> createException, TaskStatus expected)
        {
            // arrange
            var text = new FailingTextWriter(createException, timing);

            // act: wait for the write to finish without throwing, so that its status can be checked.
            var write = text.WriteAsync(TextChars, 0, TextChars.Length);
            await Task.WhenAny(write);

            // assert
            write.Status.Should().Be(expected);
        }

        // Each timing that reports the failure by a task that has already completed when it is returned.  Whether a task that yields has completed
        // by the time it is returned depends on the thread pool, so AfterYielding isn't checked.
        [Theory]
        [InlineData(FailureTiming.AsFaultedTask)]
        [InlineData(FailureTiming.AsCanceledTask)]
        public void FailingTextWriter_WriteAsyncByCompletedTask_IsCompletedOnReturn(FailureTiming timing)
        {
            // arrange
            var text = new FailingTextWriter(() => new IOException(), timing);

            // act
            var write = text.WriteAsync(TextChars, 0, TextChars.Length);

            // assert
            write.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void FailingTextWriter_FirstFlushAsync_Fails()
        {
            // arrange
            var text = new FailingTextWriter(() => new IOException(), FailureTiming.AsFaultedTask);

            // act
            var flush = text.FlushAsync();

            // assert
            flush.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public void FailingTextWriter_WriteAsyncAfterFailedWrite_AppendsTheTextWithoutWaiting()
        {
            // arrange
            var text = new FailingTextWriter(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = text.WriteAsync(TextChars, 0, TextChars.Length);

            // act
            var write = text.WriteAsync(TextChars, 0, TextChars.Length);

            // assert
            write.Status.Should().Be(TaskStatus.RanToCompletion);
            text.ToString().Should().Be(Text);
        }

        [Fact]
        public void FailingTextWriter_FlushAsyncAfterFailedWrite_SucceedsWithoutWaiting()
        {
            // arrange
            var text = new FailingTextWriter(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = text.WriteAsync(TextChars, 0, TextChars.Length);

            // act
            var flush = text.FlushAsync();

            // assert
            flush.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public void FailingTextWriter_TwoWrites_RunsOnFailureOnceBeforeCreatingTheException()
        {
            // arrange
            var events = new List<string>();
            var text = new FailingTextWriter(() => RecordAndCreate(events), FailureTiming.AsFaultedTask) { OnFailure = () => events.Add(FailureCallback) };

            // act
            _ = text.WriteAsync(TextChars, 0, TextChars.Length);
            _ = text.WriteAsync(TextChars, 0, TextChars.Length);

            // assert
            events.Should().Equal(FailureCallback, ExceptionCreated);
        }

        [Fact]
        public void FailingTextWriter_OnFailureCancelsToken_TokenIsCanceledWhenTheWriteFails()
        {
            // arrange
            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var text = new FailingTextWriter(() => new OperationCanceledException(), FailureTiming.FromCall) { OnFailure = cancellationTokenSource.Cancel };

                // act
                Action testAction = () => text.WriteAsync(TextChars, 0, TextChars.Length);

                // assert
                testAction.Should().Throw<OperationCanceledException>();
                cancellationTokenSource.IsCancellationRequested.Should().BeTrue();
            }
        }

        #endregion

        #region FailingStream

        [Fact]
        public void FailingStream_WriteAsyncFromCall_ThrowsFromTheCall()
        {
            // arrange
            var stream = new FailingStream(() => new IOException(), FailureTiming.FromCall);

            // act
            Action testAction = () => stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            testAction.Should().Throw<IOException>();
        }

        [Theory]
        [MemberData(nameof(TaskFailures), DisableDiscoveryEnumeration = true)]
        public async Task FailingStream_WriteAsyncByTask_EndsWithStatus(FailureTiming timing, Func<Exception> createException, TaskStatus expected)
        {
            // arrange
            var stream = new FailingStream(createException, timing);

            // act: wait for the write to finish without throwing, so that its status can be checked.
            var write = stream.WriteAsync(OneByte, 0, OneByte.Length);
            await Task.WhenAny(write);

            // assert
            write.Status.Should().Be(expected);
        }

        [Fact]
        public void FailingStream_FirstFlushAsync_Fails()
        {
            // arrange
            var stream = new FailingStream(() => new IOException(), FailureTiming.AsFaultedTask);

            // act
            var flush = stream.FlushAsync();

            // assert
            flush.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public void FailingStream_WriteAsyncAfterFailedWrite_WritesTheBytesWithoutWaiting()
        {
            // arrange
            var stream = new FailingStream(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // act
            var write = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            write.Status.Should().Be(TaskStatus.RanToCompletion);
            stream.ToArray().Should().Equal(OneByte);
        }

        [Fact]
        public void FailingStream_FlushAsyncAfterFailedWrite_SucceedsWithoutWaiting()
        {
            // arrange
            var stream = new FailingStream(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // act
            var flush = stream.FlushAsync();

            // assert
            flush.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public void FailingStream_TwoWrites_RunsOnFailureOnceBeforeCreatingTheException()
        {
            // arrange
            var events = new List<string>();
            var stream = new FailingStream(() => RecordAndCreate(events), FailureTiming.AsFaultedTask) { OnFailure = () => events.Add(FailureCallback) };

            // act
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            events.Should().Equal(FailureCallback, ExceptionCreated);
        }

        #endregion

        #region FailingTextReader

        [Fact]
        public void FailingTextReader_ReadAsyncFromCall_ThrowsFromTheCall()
        {
            // arrange
            var reader = new FailingTextReader(() => new IOException(), FailureTiming.FromCall);

            // act
            Action testAction = () => reader.ReadAsync(new char[1], 0, 1);

            // assert
            testAction.Should().Throw<IOException>();
        }

        [Theory]
        [MemberData(nameof(TaskFailures), DisableDiscoveryEnumeration = true)]
        public async Task FailingTextReader_ReadAsyncByTask_EndsWithStatus(FailureTiming timing, Func<Exception> createException, TaskStatus expected)
        {
            // arrange
            var reader = new FailingTextReader(createException, timing);

            // act: wait for the read to finish without throwing, so that its status can be checked.
            var read = reader.ReadAsync(new char[1], 0, 1);
            await Task.WhenAny(read);

            // assert
            read.Status.Should().Be(expected);
        }

        [Fact]
        public void FailingTextReader_SecondReadAsync_FailsAgain()
        {
            // arrange
            var reader = new FailingTextReader(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = reader.ReadAsync(new char[1], 0, 1);

            // act
            var read = reader.ReadAsync(new char[1], 0, 1);

            // assert
            read.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public void FailingTextReader_ReadAsync_RunsOnFailureBeforeCreatingTheException()
        {
            // arrange
            var events = new List<string>();
            var reader = new FailingTextReader(() => RecordAndCreate(events), FailureTiming.AsFaultedTask) { OnFailure = () => events.Add(FailureCallback) };

            // act
            _ = reader.ReadAsync(new char[1], 0, 1);

            // assert
            events.Should().Equal(FailureCallback, ExceptionCreated);
        }

        #endregion

        #region FailingReadStream

        [Fact]
        public void FailingReadStream_ReadAsyncFromCall_ThrowsFromTheCall()
        {
            // arrange
            var stream = new FailingReadStream(() => new IOException(), FailureTiming.FromCall);

            // act
            Action testAction = () => stream.ReadAsync(new byte[1], 0, 1, CancellationToken.None);

            // assert
            testAction.Should().Throw<IOException>();
        }

        [Theory]
        [MemberData(nameof(TaskFailures), DisableDiscoveryEnumeration = true)]
        public async Task FailingReadStream_ReadAsyncByTask_EndsWithStatus(FailureTiming timing, Func<Exception> createException, TaskStatus expected)
        {
            // arrange
            var stream = new FailingReadStream(createException, timing);

            // act: wait for the read to finish without throwing, so that its status can be checked.
            var read = stream.ReadAsync(new byte[1], 0, 1, CancellationToken.None);
            await Task.WhenAny(read);

            // assert
            read.Status.Should().Be(expected);
        }

        [Fact]
        public void FailingReadStream_SecondReadAsync_FailsAgain()
        {
            // arrange
            var stream = new FailingReadStream(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = stream.ReadAsync(new byte[1], 0, 1, CancellationToken.None);

            // act
            var read = stream.ReadAsync(new byte[1], 0, 1, CancellationToken.None);

            // assert
            read.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public void FailingReadStream_ReadAsync_RunsOnFailureBeforeCreatingTheException()
        {
            // arrange
            var events = new List<string>();
            var stream = new FailingReadStream(() => RecordAndCreate(events), FailureTiming.AsFaultedTask) { OnFailure = () => events.Add(FailureCallback) };

            // act
            _ = stream.ReadAsync(new byte[1], 0, 1, CancellationToken.None);

            // assert
            events.Should().Equal(FailureCallback, ExceptionCreated);
        }

        [Fact]
        public void FailingReadStream_SynchronousRead_Throws()
        {
            // arrange
            var stream = new FailingReadStream(() => new IOException(), FailureTiming.AsFaultedTask);

            // act
            Action testAction = () => stream.ReadByte();

            // assert
            testAction.Should().Throw<InvalidOperationException>().WithMessage(AsyncPathMessages.SynchronousRead);
        }

        #endregion

        #region AsyncOnlyWriteStream

        [Fact]
        public void AsyncOnlyWriteStream_FirstWriteException_FaultsTheFirstWriteWithoutWaiting()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream(completeSynchronously: true) { FirstWriteException = new IOException() };

            // act
            var write = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            write.IsFaulted.Should().BeTrue();
            write.Exception.InnerException.Should().BeOfType<IOException>();
        }

        [Fact]
        public void AsyncOnlyWriteStream_WriteAfterFirstWriteException_Writes()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream(completeSynchronously: true) { FirstWriteException = new IOException() };
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // act
            var write = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            write.Status.Should().Be(TaskStatus.RanToCompletion);
            stream.ToArray().Should().Equal(OneByte);
        }

        [Fact]
        public void AsyncOnlyWriteStream_FirstWriteException_RunsOnWriteFirst()
        {
            // arrange
            var events = new List<string>();
            var stream = new AsyncOnlyWriteStream(completeSynchronously: true) { FirstWriteException = new IOException(), OnWrite = () => events.Add(WriteCallback) };

            // act
            var write = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            write.IsFaulted.Should().BeTrue();
            events.Should().Equal(WriteCallback);
        }

        [Fact]
        public async Task AsyncOnlyWriteStream_CreateFlushException_FaultsTheFlushAfterRunningOnFlush()
        {
            // arrange
            var events = new List<string>();
            var stream = new AsyncOnlyWriteStream { CreateFlushException = () => new IOException(), OnFlush = () => events.Add(FlushCallback) };

            // act: wait for the flush to finish without throwing, so that its status can be checked.
            var flush = stream.FlushAsync();
            await Task.WhenAny(flush);

            // assert
            flush.IsFaulted.Should().BeTrue();
            events.Should().Equal(FlushCallback);
            stream.FlushCount.Should().Be(NoFlushes);
        }

        [Fact]
        public async Task AsyncOnlyWriteStream_CreateFlushExceptionReturnsNull_Flushes()
        {
            // arrange
            var stream = new AsyncOnlyWriteStream { CreateFlushException = () => null };

            // act
            await stream.FlushAsync();

            // assert
            stream.FlushCount.Should().Be(OneFlush);
        }

        #endregion

        private static Exception RecordAndCreate(List<string> events)
        {
            events.Add(ExceptionCreated);
            return new IOException();
        }
    }
}
