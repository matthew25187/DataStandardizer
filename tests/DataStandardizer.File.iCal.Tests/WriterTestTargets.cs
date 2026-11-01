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
            await Task.Yield();
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
            await Task.Yield();
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
            await Task.Yield();
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

        public override Encoding Encoding => new UTF8Encoding(false);

        public override void Write(char value)
        {
            throw new InvalidOperationException(AsyncPathMessages.SynchronousWrite);
        }

        public override Task WriteAsync(char[] buffer, int index, int count)
        {
            throw _createException();
        }

#if NETCOREAPP2_1_OR_GREATER
        public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default)
        {
            throw _createException();
        }
#endif
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
