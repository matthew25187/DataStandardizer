using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// The messages of the exceptions that <see cref="BlockStream"/> throws when it is used the wrong way.
    /// </summary>
    internal static class StreamMessages
    {
        public const string SynchronousRead = "Synchronous read on the async path.";
        public const string AsynchronousRead = "Asynchronous read on the sync path.";
    }

    /// <summary>
    /// How a <see cref="BlockStream"/> may be read.
    /// </summary>
    internal enum BlockStreamMode
    {
        SynchronousOnly,
        AsynchronousOnly
    }

    /// <summary>
    /// The parts of a read-only stream that the test streams have in common.
    /// </summary>
    internal abstract class ReadOnlyTestStream : Stream
    {
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

    /// <summary>
    /// A stream that gives a fixed number of bytes in its first read and another fixed number in each read after that, so that a test can end a
    /// read at a chosen point.  The members for the other mode throw, so that any I/O on the wrong path fails the test.
    /// </summary>
    internal sealed class BlockStream : ReadOnlyTestStream
    {
        private readonly byte[] _data;
        private readonly int _firstReadLength;
        private readonly BlockStreamMode _mode;
        private readonly int _readLength;
        private int _position;
        private int _readCount;

        public BlockStream(byte[] data, BlockStreamMode mode, int firstReadLength = int.MaxValue, int readLength = int.MaxValue)
        {
            _data = data;
            _mode = mode;
            _firstReadLength = firstReadLength;
            _readLength = readLength;
        }

        /// <summary>
        /// Gets the number of bytes the stream has given so far.
        /// </summary>
        public int BytesGiven => _position;

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_mode != BlockStreamMode.SynchronousOnly) throw new InvalidOperationException(StreamMessages.SynchronousRead);

            return Give(buffer, offset, count);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            throw new InvalidOperationException(StreamMessages.SynchronousRead);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_mode != BlockStreamMode.AsynchronousOnly) throw new InvalidOperationException(StreamMessages.AsynchronousRead);

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            var bytes = new byte[buffer.Length];
            var count = Give(bytes, 0, bytes.Length);
            bytes.AsMemory(0, count).CopyTo(buffer);
            return count;
        }
#endif

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_mode != BlockStreamMode.AsynchronousOnly) throw new InvalidOperationException(StreamMessages.AsynchronousRead);

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            return Give(buffer, offset, count);
        }

        private int Give(byte[] buffer, int offset, int count)
        {
            var limit = _readCount == 0 ? _firstReadLength : _readLength;
            _readCount++;
            var length = Math.Min(Math.Min(count, limit), _data.Length - _position);
            Array.Copy(_data, _position, buffer, offset, length);
            _position += length;
            return length;
        }
    }

    /// <summary>
    /// A stream whose asynchronous reads wait until the test opens a gate, so a read can be held in progress.  It holds no data.
    /// </summary>
    internal sealed class GatedStream : ReadOnlyTestStream
    {
        private readonly TaskCompletionSource<bool> _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Gets a flag indicating if the stream has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Lets the waiting read complete, at the end of the data.
        /// </summary>
        public void OpenGate()
        {
            _gate.TrySetResult(true);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException(StreamMessages.SynchronousRead);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var canceled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => canceled.TrySetCanceled(cancellationToken)))
            {
                await await Task.WhenAny(_gate.Task, canceled.Task);
            }

            return 0;
        }

#if NETCOREAPP2_1_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(ReadAsync(Array.Empty<byte>(), 0, 0, cancellationToken));
        }
#endif

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// A stream whose reads fail with an <see cref="IOException"/>, thrown when the read is called.
    /// </summary>
    internal sealed class FailingStream : ReadOnlyTestStream
    {
        public const string FailureMessage = "The read failed.";

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new IOException(FailureMessage);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            throw new IOException(FailureMessage);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new IOException(FailureMessage);
        }
#endif
    }

    /// <summary>
    /// A stream that can't be read.
    /// </summary>
    internal sealed class WriteOnlyStream : MemoryStream
    {
        public override bool CanRead => false;
    }
}
