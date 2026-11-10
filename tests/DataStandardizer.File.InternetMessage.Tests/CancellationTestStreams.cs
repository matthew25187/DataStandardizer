using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// When <see cref="TimedFailingWriteStream"/> or <see cref="TimedFailingReadStream"/> reports its failure.
    /// </summary>
    public enum FailureTiming
    {
        /// <summary>Thrown from the call, before a task is returned.</summary>
        FromCall,

        /// <summary>By a task that has already faulted when it is returned.</summary>
        AsFaultedTask,

        /// <summary>By a task that has already been canceled, for another token, when it is returned.  The exception the test supplies isn't
        /// used.</summary>
        AsCanceledTask,

        /// <summary>By an async method that throws after yielding.  Its task is canceled for an <see cref="OperationCanceledException"/>, as
        /// the async method builder does, and faulted for any other exception.</summary>
        AfterYielding
    }

    /// <summary>
    /// Reports a failure at the time a <see cref="FailureTiming"/> says, for the test streams that fail.
    /// </summary>
    internal static class TestFailures
    {
        /// <summary>
        /// Fails with the exception that <paramref name="createException"/> creates, at the time <paramref name="timing"/> says.
        /// </summary>
        /// <returns>A task that fails as <paramref name="timing"/> says, unless the exception is thrown from the call.</returns>
        public static Task<TResult> Fail<TResult>(FailureTiming timing, Func<Exception> createException)
        {
            switch (timing)
            {
                case FailureTiming.FromCall:
                    throw createException();
                case FailureTiming.AsFaultedTask:
                    return Task.FromException<TResult>(createException());
                case FailureTiming.AsCanceledTask:
                    return Task.FromCanceled<TResult>(new CancellationToken(true));
                default:
                    return FailAfterYieldingAsync<TResult>(createException);
            }
        }

        private static async Task<TResult> FailAfterYieldingAsync<TResult>(Func<Exception> createException)
        {
            await Task.Yield();
            throw createException();
        }
    }

    /// <summary>
    /// A stream whose first asynchronous write or flush fails with an exception the test supplies, at the time <see cref="FailureTiming"/>
    /// says.  Later writes and flushes succeed without waiting.  Its synchronous members throw, so any synchronous I/O on the async path fails
    /// the test.
    /// </summary>
    internal sealed class TimedFailingWriteStream : Stream
    {
        private readonly Func<Exception> _createException;
        private readonly MemoryStream _data = new MemoryStream();
        private readonly FailureTiming _timing;
        private bool _hasFailed;

        public TimedFailingWriteStream(Func<Exception> createException, FailureTiming timing)
        {
            _createException = createException;
            _timing = timing;
        }

        /// <summary>
        /// Gets or sets a callback that the failing write or flush runs first.  A test uses it to cancel the caller's token during the call.
        /// </summary>
        public Action OnFailure { get; set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public byte[] ToArray() => _data.ToArray();

        public override void Flush()
        {
            throw new InvalidOperationException(WriteStreamMessages.SynchronousWrite);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return _hasFailed ? Task.CompletedTask : Fail();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(WriteStreamMessages.SynchronousWrite);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new InvalidOperationException(WriteStreamMessages.SynchronousWrite);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask(WriteAsync(buffer.ToArray(), 0, buffer.Length, cancellationToken));
        }
#endif

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_hasFailed)
            {
                return Fail();
            }

            _data.Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        private Task Fail()
        {
            _hasFailed = true;
            OnFailure?.Invoke();
            return TestFailures.Fail<bool>(_timing, _createException);
        }
    }

    /// <summary>
    /// A stream whose first asynchronous read fails with an exception the test supplies, at the time <see cref="FailureTiming"/> says.  Later
    /// reads give its data without waiting.  Its synchronous members throw, so any synchronous I/O on the async path fails the test.
    /// </summary>
    internal sealed class TimedFailingReadStream : ReadOnlyTestStream
    {
        private readonly Func<Exception> _createException;
        private readonly MemoryStream _data;
        private readonly FailureTiming _timing;
        private bool _hasFailed;

        public TimedFailingReadStream(byte[] data, Func<Exception> createException, FailureTiming timing)
        {
            _data = new MemoryStream(data);
            _createException = createException;
            _timing = timing;
        }

        /// <summary>
        /// Gets or sets a callback that the failing read runs first.  A test uses it to cancel the caller's token during the read.
        /// </summary>
        public Action OnFailure { get; set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(StreamMessages.SynchronousRead);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (!_hasFailed)
            {
                _hasFailed = true;
                OnFailure?.Invoke();
                return TestFailures.Fail<int>(_timing, _createException);
            }

            return Task.FromResult(_data.Read(buffer, offset, count));
        }

#if NETCOREAPP2_1_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!_hasFailed)
            {
                _hasFailed = true;
                OnFailure?.Invoke();
                return new ValueTask<int>(TestFailures.Fail<int>(_timing, _createException));
            }

            return new ValueTask<int>(_data.Read(buffer.Span));
        }
#endif
    }
}
