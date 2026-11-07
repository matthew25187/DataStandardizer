using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// A text reader whose asynchronous reads fail with an exception the test supplies, at the time <see cref="FailureTiming"/> says.  Its
    /// synchronous members throw, so any synchronous I/O on the async path fails the test.
    /// </summary>
    internal sealed class FailingTextReader : TextReader
    {
        private readonly Func<Exception> _createException;
        private readonly FailureTiming _timing;

        public FailingTextReader(Func<Exception> createException, FailureTiming timing)
        {
            _createException = createException;
            _timing = timing;
        }

        /// <summary>
        /// Gets or sets a callback that each read runs first.  A test uses it to cancel the caller's token during the read.
        /// </summary>
        public Action OnFailure { get; set; }

        public override int Peek()
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

        public override int Read()
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

        public override int Read(char[] buffer, int index, int count)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override int Read(Span<char> buffer)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousRead);
        }
#endif

        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            OnFailure?.Invoke();
            return TestFailures.Fail<int>(_timing, _createException);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(ReadAsync(new char[0], 0, 0));
        }
#endif
    }
}
