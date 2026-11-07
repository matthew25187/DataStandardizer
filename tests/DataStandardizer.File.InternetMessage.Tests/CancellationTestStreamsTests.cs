using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of the test streams in <c>CancellationTestStreams.cs</c> whose behaviour the cancellation tests rely on.
    /// </summary>
    public class CancellationTestStreamsTests
    {
        private static readonly byte[] OneByte = { 1 };

        private const string FailureCallback = "failure callback";
        private const string ExceptionCreated = "exception created";

        // Each timing that reports the failure by the task, with an exception, and the status the task ends with.
        public static TheoryData<FailureTiming, Func<Exception>, TaskStatus> TaskFailures => new TheoryData<FailureTiming, Func<Exception>, TaskStatus>
        {
            { FailureTiming.AsFaultedTask, () => new IOException(), TaskStatus.Faulted },
            { FailureTiming.AsFaultedTask, () => new OperationCanceledException(), TaskStatus.Faulted },
            { FailureTiming.AsCanceledTask, () => new IOException(), TaskStatus.Canceled },
            { FailureTiming.AfterYielding, () => new IOException(), TaskStatus.Faulted },
            { FailureTiming.AfterYielding, () => new OperationCanceledException(), TaskStatus.Canceled },
        };

        #region TimedFailingWriteStream

        [Fact]
        public void TimedFailingWriteStream_WriteAsyncFromCall_ThrowsFromTheCall()
        {
            // arrange
            var stream = new TimedFailingWriteStream(() => new IOException(), FailureTiming.FromCall);

            // act
            Action testAction = () => stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            testAction.Should().Throw<IOException>();
        }

        [Theory]
        [MemberData(nameof(TaskFailures), DisableDiscoveryEnumeration = true)]
        public async Task TimedFailingWriteStream_WriteAsyncByTask_EndsWithStatus(FailureTiming timing, Func<Exception> createException, TaskStatus expected)
        {
            // arrange
            var stream = new TimedFailingWriteStream(createException, timing);

            // act: wait for the write to finish without throwing, so that its status can be checked.
            var write = stream.WriteAsync(OneByte, 0, OneByte.Length);
            await Task.WhenAny(write);

            // assert
            write.Status.Should().Be(expected);
        }

        [Fact]
        public void TimedFailingWriteStream_FirstFlushAsync_Fails()
        {
            // arrange
            var stream = new TimedFailingWriteStream(() => new IOException(), FailureTiming.AsFaultedTask);

            // act
            var flush = stream.FlushAsync();

            // assert
            flush.IsFaulted.Should().BeTrue();
        }

        [Fact]
        public void TimedFailingWriteStream_WriteAsyncAfterFailedWrite_WritesTheBytesWithoutWaiting()
        {
            // arrange
            var stream = new TimedFailingWriteStream(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // act
            var write = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            write.Status.Should().Be(TaskStatus.RanToCompletion);
            stream.ToArray().Should().Equal(OneByte);
        }

        [Fact]
        public void TimedFailingWriteStream_FlushAsyncAfterFailedWrite_SucceedsWithoutWaiting()
        {
            // arrange
            var stream = new TimedFailingWriteStream(() => new IOException(), FailureTiming.AsFaultedTask);
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // act
            var flush = stream.FlushAsync();

            // assert
            flush.Status.Should().Be(TaskStatus.RanToCompletion);
        }

        [Fact]
        public void TimedFailingWriteStream_TwoWrites_RunsOnFailureOnceBeforeCreatingTheException()
        {
            // arrange
            var events = new List<string>();
            var stream = new TimedFailingWriteStream(() => RecordAndCreate(events), FailureTiming.AsFaultedTask) { OnFailure = () => events.Add(FailureCallback) };

            // act
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);
            _ = stream.WriteAsync(OneByte, 0, OneByte.Length);

            // assert
            events.Should().Equal(FailureCallback, ExceptionCreated);
        }

        #endregion

        #region TimedFailingReadStream

        [Fact]
        public void TimedFailingReadStream_ReadAsyncFromCall_ThrowsFromTheCall()
        {
            // arrange
            var stream = new TimedFailingReadStream(OneByte, () => new IOException(), FailureTiming.FromCall);

            // act
            Action testAction = () => stream.ReadAsync(new byte[1], 0, 1);

            // assert
            testAction.Should().Throw<IOException>();
        }

        [Theory]
        [MemberData(nameof(TaskFailures), DisableDiscoveryEnumeration = true)]
        public async Task TimedFailingReadStream_ReadAsyncByTask_EndsWithStatus(FailureTiming timing, Func<Exception> createException, TaskStatus expected)
        {
            // arrange
            var stream = new TimedFailingReadStream(OneByte, createException, timing);

            // act: wait for the read to finish without throwing, so that its status can be checked.
            var read = stream.ReadAsync(new byte[1], 0, 1);
            await Task.WhenAny(read);

            // assert
            read.Status.Should().Be(expected);
        }

        [Fact]
        public async Task TimedFailingReadStream_ReadAsyncAfterFailedRead_GivesTheDataWithoutWaiting()
        {
            // arrange
            var stream = new TimedFailingReadStream(OneByte, () => new IOException(), FailureTiming.AsFaultedTask);
            var buffer = new byte[2];
            _ = stream.ReadAsync(buffer, 0, buffer.Length);

            // act
            var read = stream.ReadAsync(buffer, 0, buffer.Length);

            // assert
            read.Status.Should().Be(TaskStatus.RanToCompletion);
            (await read).Should().Be(OneByte.Length);
            buffer[0].Should().Be(OneByte[0]);
        }

#if NETCOREAPP2_1_OR_GREATER
        [Fact]
        public async Task TimedFailingReadStream_ReadAsyncOfMemoryAfterFailedRead_GivesTheDataWithoutWaiting()
        {
            // arrange
            var stream = new TimedFailingReadStream(OneByte, () => new IOException(), FailureTiming.AsFaultedTask);
            var buffer = new byte[2];
            _ = stream.ReadAsync(buffer.AsMemory());

            // act
            var read = stream.ReadAsync(buffer.AsMemory());

            // assert
            read.IsCompletedSuccessfully.Should().BeTrue();
            (await read).Should().Be(OneByte.Length);
            buffer[0].Should().Be(OneByte[0]);
        }

        [Fact]
        public void TimedFailingReadStream_FirstReadAsyncOfMemory_Fails()
        {
            // arrange
            var stream = new TimedFailingReadStream(OneByte, () => new IOException(), FailureTiming.AsFaultedTask);

            // act
            var read = stream.ReadAsync(new byte[1].AsMemory());

            // assert
            read.IsFaulted.Should().BeTrue();
        }
#endif

        [Fact]
        public void TimedFailingReadStream_ReadAsync_RunsOnFailureBeforeCreatingTheException()
        {
            // arrange
            var events = new List<string>();
            var stream = new TimedFailingReadStream(OneByte, () => RecordAndCreate(events), FailureTiming.AsFaultedTask) { OnFailure = () => events.Add(FailureCallback) };

            // act
            _ = stream.ReadAsync(new byte[1], 0, 1);

            // assert
            events.Should().Equal(FailureCallback, ExceptionCreated);
        }

        #endregion

        private static Exception RecordAndCreate(List<string> events)
        {
            events.Add(ExceptionCreated);
            return new IOException();
        }
    }
}
