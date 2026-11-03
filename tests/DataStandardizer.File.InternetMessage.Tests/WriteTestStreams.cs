using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// The messages of the exceptions that <see cref="ModeWriteStream"/> throws when it is used the wrong way.
    /// </summary>
    internal static class WriteStreamMessages
    {
        public const string SynchronousWrite = "Synchronous write on the async path.";
        public const string AsynchronousWrite = "Asynchronous write on the sync path.";
    }

    /// <summary>
    /// A stream that keeps what is written to it, and whose members for the other mode throw, so that any I/O on the wrong path fails the test.
    /// </summary>
    internal sealed class ModeWriteStream : Stream
    {
        private readonly MemoryStream _kept = new MemoryStream();
        private readonly BlockStreamMode _mode;

        public ModeWriteStream(BlockStreamMode mode)
        {
            _mode = mode;
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _kept.Length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Gets what has been written.
        /// </summary>
        public byte[] ToArray()
        {
            return _kept.ToArray();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_mode != BlockStreamMode.SynchronousOnly) throw new InvalidOperationException(WriteStreamMessages.SynchronousWrite);

            _kept.Write(buffer, offset, count);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_mode != BlockStreamMode.SynchronousOnly) throw new InvalidOperationException(WriteStreamMessages.SynchronousWrite);

            _kept.Write(buffer);
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_mode != BlockStreamMode.AsynchronousOnly) throw new InvalidOperationException(WriteStreamMessages.AsynchronousWrite);

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            _kept.Write(buffer.Span);
        }
#endif

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (_mode != BlockStreamMode.AsynchronousOnly) throw new InvalidOperationException(WriteStreamMessages.AsynchronousWrite);

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            _kept.Write(buffer, offset, count);
        }

        public override void Flush()
        {
            if (_mode != BlockStreamMode.SynchronousOnly) throw new InvalidOperationException(WriteStreamMessages.SynchronousWrite);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            if (_mode != BlockStreamMode.AsynchronousOnly) throw new InvalidOperationException(WriteStreamMessages.AsynchronousWrite);

            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
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
    }

    /// <summary>
    /// A stream whose asynchronous writes wait until the test opens a gate, so a write can be held in progress.
    /// </summary>
    internal sealed class GatedWriteStream : MemoryStream
    {
        private readonly TaskCompletionSource<bool> _gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Lets the waiting write complete.
        /// </summary>
        public void OpenGate()
        {
            _gate.TrySetResult(true);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _gate.Task;
            await base.WriteAsync(buffer, offset, count, cancellationToken);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await _gate.Task;
            await base.WriteAsync(buffer, cancellationToken);
        }
#endif
    }

    /// <summary>
    /// A stream whose writes and flushes fail with an <see cref="IOException"/>.
    /// </summary>
    internal sealed class FailingWriteStream : MemoryStream
    {
        public const string FailureMessage = "The write failed.";

        public override void Flush()
        {
            throw new IOException(FailureMessage);
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.FromException(new IOException(FailureMessage));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new IOException(FailureMessage);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            throw new IOException(FailureMessage);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask(Task.FromException(new IOException(FailureMessage)));
        }
#endif

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return Task.FromException(new IOException(FailureMessage));
        }
    }

    /// <summary>
    /// A stream that can't be written.
    /// </summary>
    internal sealed class ReadOnlyMemoryStream : MemoryStream
    {
        public override bool CanWrite => false;
    }

    /// <summary>
    /// A stream that holds fixed data, can be read in either mode, and can't seek.
    /// </summary>
    internal sealed class ForwardOnlyStream : MemoryStream
    {
        public ForwardOnlyStream(byte[] data) : base(data)
        {
        }

        public override bool CanSeek => false;
    }

    /// <summary>
    /// Records progress reports as they are made, with no delay, unlike <see cref="Progress{T}"/>.
    /// </summary>
    internal sealed class RecordingProgress : IProgress<ImfWriteProgress>
    {
        /// <summary>
        /// Gets the number of reports made.
        /// </summary>
        public int ReportCount { get; private set; }

        /// <summary>
        /// Gets the last report made.
        /// </summary>
        public ImfWriteProgress Last { get; private set; }

        /// <summary>
        /// Gets the most bytes that any one report added to the one before it.
        /// </summary>
        public long LargestStep { get; private set; }

        /// <summary>
        /// Gets every report made, in order.
        /// </summary>
        public List<ImfWriteProgress> Reports { get; } = new List<ImfWriteProgress>();

        public void Report(ImfWriteProgress value)
        {
            LargestStep = Math.Max(LargestStep, value.BytesWritten - Last.BytesWritten);
            ReportCount++;
            Last = value;
            Reports.Add(value);
        }
    }

    /// <summary>
    /// Progress that throws when it is reported to.
    /// </summary>
    internal sealed class ThrowingProgress : IProgress<ImfWriteProgress>
    {
        public const string FailureMessage = "The progress failed.";

        public void Report(ImfWriteProgress value)
        {
            throw new InvalidOperationException(FailureMessage);
        }
    }
}
