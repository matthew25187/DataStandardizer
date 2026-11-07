using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// A stream that can only be written asynchronously.  Its synchronous members throw, so any synchronous I/O on the async path fails the test.
    /// </summary>
    internal sealed class AsyncOnlyWriteStream : Stream
    {
        private readonly MemoryStream _data = new MemoryStream();
        private readonly bool _completeSynchronously;

        /// <param name="completeSynchronously">Whether a write completes synchronously, rather than after yielding.</param>
        public AsyncOnlyWriteStream(bool completeSynchronously = false)
        {
            _completeSynchronously = completeSynchronously;
        }

        /// <summary>
        /// Gets or sets a callback that each write runs first.  A test uses it to dispose of the writer during a write.
        /// </summary>
        public Action OnWrite { get; set; }

        /// <summary>
        /// Gets or sets an exception that the first write throws, after <see cref="OnWrite"/> and without yielding; or <c>null</c>.  Later writes
        /// succeed.
        /// </summary>
        public Exception FirstWriteException { get; set; }

        /// <summary>
        /// Gets or sets a callback that each flush runs first.  A test uses it to dispose of the writer during a flush.
        /// </summary>
        public Action OnFlush { get; set; }

        /// <summary>
        /// Gets or sets a function that each flush calls after yielding, for the exception to fail with, or <c>null</c> to succeed.
        /// </summary>
        public Func<Exception> CreateFlushException { get; set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the number of asynchronous flushes.
        /// </summary>
        public int FlushCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public byte[] ToArray() => _data.ToArray();

        public override void Flush()
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousFlush);
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            OnFlush?.Invoke();
            await Task.Yield();
            var exception = CreateFlushException?.Invoke();
            if (exception != null)
                throw exception;

            FlushCount++;
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
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }
#endif

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
#else
        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
#endif
        {
            OnWrite?.Invoke();
            var exception = FirstWriteException;
            FirstWriteException = null;
            if (exception != null)
                throw exception;

            if (!_completeSynchronously)
            {
                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
            _data.Write(buffer.ToArray(), 0, buffer.Length);
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A text writer that can only be written asynchronously.  Its synchronous members throw, so any synchronous I/O on the async path fails the
    /// test.
    /// </summary>
    internal sealed class AsyncOnlyTextWriter : TextWriter
    {
        private readonly StringBuilder _text = new StringBuilder();
        private readonly bool _completeSynchronously;

        /// <param name="completeSynchronously">Whether a write completes synchronously, rather than after yielding.</param>
        public AsyncOnlyTextWriter(bool completeSynchronously = false)
        {
            _completeSynchronously = completeSynchronously;
        }

        /// <summary>
        /// Gets or sets a callback that each write runs first.  A test uses it to dispose of the writer during a write.
        /// </summary>
        public Action OnWrite { get; set; }

        public override Encoding Encoding => new UTF8Encoding(false);

        public int FlushCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public override string ToString() => _text.ToString();

        public override void Flush()
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousFlush);
        }

        public override async Task FlushAsync()
        {
            await Task.Yield();
            FlushCount++;
        }

#if NET8_0_OR_GREATER
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return FlushAsync();
        }
#endif

        public override void Write(char value)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override void Write(ReadOnlySpan<char> buffer)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }
#endif

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return WriteAsync(buffer.AsMemory(index, count));
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
#else
        public async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
#endif
        {
            OnWrite?.Invoke();
            if (!_completeSynchronously)
            {
                await Task.Yield();
            }

            cancellationToken.ThrowIfCancellationRequested();
            _text.Append(buffer.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A text writer whose asynchronous writes wait until the test opens a gate, so a write can be held in progress.
    /// </summary>
    internal sealed class GatedTextWriter : TextWriter
    {
        private TaskCompletionSource<bool> _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly StringBuilder _text = new StringBuilder();
        private TaskCompletionSource<bool> _writeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public override Encoding Encoding => new UTF8Encoding(false);

        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Gets a task that completes when a write starts waiting at the gate.
        /// </summary>
        public Task WriteStarted => _writeStarted.Task;

        /// <summary>
        /// Lets waiting and later writes through.
        /// </summary>
        public void OpenGate()
        {
            _gate.TrySetResult(true);
        }

        /// <summary>
        /// Makes later writes wait again.
        /// </summary>
        public void CloseGate()
        {
            _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _writeStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public override string ToString() => _text.ToString();

        public override void Write(char value)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return WriteAsync(buffer.AsMemory(index, count));
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
#else
        public async Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
#endif
        {
            // Copy now: the writer's buffer belongs to it again once the write completes, not before.
            var text = buffer.ToString();
            _writeStarted.TrySetResult(true);
            await TestTasks.WaitAsync(_gate.Task, cancellationToken);
            _text.Append(text);
        }

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A text writer whose asynchronous writes throw an exception from the call itself, before returning a task.
    /// </summary>
    internal sealed class SynchronouslyThrowingTextWriter : TextWriter
    {
        private readonly Func<Exception> _createException;

        public SynchronouslyThrowingTextWriter(Func<Exception> createException)
        {
            _createException = createException;
        }

        /// <summary>
        /// Gets or sets a callback that each write runs before it throws.  A test uses it to dispose of the writer during a write.
        /// </summary>
        public Action OnWrite { get; set; }

        public override Encoding Encoding => new UTF8Encoding(false);

        public override void Write(char value)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            OnWrite?.Invoke();
            throw _createException();
        }

#if NETCOREAPP2_1_OR_GREATER
        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            OnWrite?.Invoke();
            throw _createException();
        }
#endif
    }

    /// <summary>
    /// When <see cref="FailingTextWriter"/> reports its failure.
    /// </summary>
    public enum FailureTiming
    {
        /// <summary>Thrown from the call, before a task is returned.</summary>
        FromCall,

        /// <summary>By a task that has already faulted when it is returned.</summary>
        AsFaultedTask,

        /// <summary>By a task that has already been canceled, for another token, when it is returned, as a <see cref="StreamWriter"/> does for
        /// a canceled token.  The exception the test supplies isn't used.</summary>
        AsCanceledTask,

        /// <summary>By an async method that throws after yielding.  Its task is canceled for an <see cref="OperationCanceledException"/>, as
        /// the async method builder does, and faulted for any other exception.</summary>
        AfterYielding
    }

    /// <summary>
    /// A text writer whose first asynchronous write or flush fails with an exception the test supplies, at the time <see cref="FailureTiming"/>
    /// says.  Later writes and flushes succeed without waiting.
    /// </summary>
    internal sealed class FailingTextWriter : TextWriter
    {
        private readonly Func<Exception> _createException;
        private readonly StringBuilder _text = new StringBuilder();
        private readonly FailureTiming _timing;
        private bool _hasFailed;

        public FailingTextWriter(Func<Exception> createException, FailureTiming timing)
        {
            _createException = createException;
            _timing = timing;
        }

        /// <summary>
        /// Gets or sets a callback that the failing write or flush runs first.  A test uses it to cancel the caller's token during the call.
        /// </summary>
        public Action OnFailure { get; set; }

        public override Encoding Encoding => new UTF8Encoding(false);

        public override string ToString() => _text.ToString();

        public override void Write(char value)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

        public override void Flush()
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousFlush);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            return WriteAsync(buffer.AsMemory(index, count));
        }

#if NETCOREAPP2_1_OR_GREATER
        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
#else
        public Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
#endif
        {
            if (!_hasFailed)
            {
                return Fail();
            }

            _text.Append(buffer.ToString());
            return Task.CompletedTask;
        }

        public override Task FlushAsync()
        {
            return _hasFailed ? Task.CompletedTask : Fail();
        }

#if NET8_0_OR_GREATER
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return FlushAsync();
        }
#endif

        private Task Fail()
        {
            _hasFailed = true;
            OnFailure?.Invoke();
            return TestFailures.Fail<bool>(_timing, _createException);
        }
    }

    /// <summary>
    /// Reports a failure at the time a <see cref="FailureTiming"/> says, for the test targets and sources that fail.
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
    internal sealed class FailingStream : Stream
    {
        private readonly Func<Exception> _createException;
        private readonly MemoryStream _data = new MemoryStream();
        private readonly FailureTiming _timing;
        private bool _hasFailed;

        public FailingStream(Func<Exception> createException, FailureTiming timing)
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
            throw new InvalidOperationException(AsyncPathMessages.SynchronousFlush);
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
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
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

#if NETCOREAPP2_1_OR_GREATER
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask(WriteAsync(buffer.ToArray(), 0, buffer.Length, cancellationToken));
        }
#endif

        private Task Fail()
        {
            _hasFailed = true;
            OnFailure?.Invoke();
            return TestFailures.Fail<bool>(_timing, _createException);
        }
    }

    /// <summary>
    /// A string writer that counts flushes and records whether it has been disposed.
    /// </summary>
    internal sealed class TrackingStringWriter : StringWriter
    {
        public int FlushCount { get; private set; }

        public bool IsDisposed { get; private set; }

        public override void Flush()
        {
            FlushCount++;
            base.Flush();
        }

        public override Task FlushAsync()
        {
            FlushCount++;
            return Task.CompletedTask;
        }

#if NET8_0_OR_GREATER
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            FlushCount++;
            return Task.CompletedTask;
        }
#endif

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Records progress reports as they are made, rather than posting them to a synchronization context as <see cref="Progress{T}"/> does.
    /// </summary>
    internal sealed class RecordingProgress : IProgress<IcalWriteProgress>
    {
        private readonly Action<IcalWriteProgress> _onReport;

        public RecordingProgress(Action<IcalWriteProgress> onReport = null)
        {
            _onReport = onReport;
        }

        public List<IcalWriteProgress> Reports { get; } = new List<IcalWriteProgress>();

        public void Report(IcalWriteProgress value)
        {
            Reports.Add(value);
            _onReport?.Invoke(value);
        }
    }
}
