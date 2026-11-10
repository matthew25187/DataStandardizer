using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.vCard.Tests
{
    /// <summary>
    /// A stream whose asynchronous reads fail with an exception the test supplies, at the time <see cref="FailureTiming"/> says.  Its
    /// synchronous members throw, so any synchronous I/O on the async path fails the test.
    /// </summary>
    internal sealed class FailingReadStream : Stream
    {
        private readonly Func<Exception> _createException;
        private readonly FailureTiming _timing;

        public FailingReadStream(Func<Exception> createException, FailureTiming timing)
        {
            _createException = createException;
            _timing = timing;
        }

        /// <summary>
        /// Gets or sets a callback that each read runs first.  A test uses it to cancel the caller's token during the read.
        /// </summary>
        public Action OnFailure { get; set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }
#endif

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            OnFailure?.Invoke();
            return TestFailures.Fail<int>(_timing, _createException);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(ReadAsync(new byte[0], 0, 0, cancellationToken));
        }
#endif

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
            throw new NotSupportedException();
        }
    }
}
