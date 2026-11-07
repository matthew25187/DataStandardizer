using System;
using System.Buffers;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The stream of a writer, and the rules for using it that the writers share: one operation at a time, a dispose from any thread, and no further
    /// writes once output has been left incomplete.
    /// </summary>
    internal sealed class ImfOutput
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        : ICancellationRecorder
#endif
    {
        /// <summary>
        /// Size of the buffer of the file stream that a file-path constructor opens.
        /// </summary>
        internal const int FileBufferSize = 16 * 1024;

        private const int CopyBlockSize = 16 * 1024;
        private const int DisposedFlag = 2;
        private const int OperationInProgressFlag = 1;
        private const int ReleasedFlag = 4;

        private readonly string _ownerName;
        private readonly bool _ownsStream;
        private readonly Stream _stream;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        private readonly TaskCompletionSource<bool> _released = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
#endif
        private bool _isBroken;

        // The flags below, changed together with Interlocked, so that an operation can't start between the end of another and the decision to release
        // the stream, and a Dispose from another thread while an operation is in progress releases the stream once only, after that operation.
        private int _state;

        internal ImfOutput(Stream stream, bool ownsStream, string ownerName)
        {
            _stream = stream;
            _ownsStream = ownsStream;
            _ownerName = ownerName;
        }

        /// <summary>
        /// Gets the number of bytes handed to the stream so far.
        /// </summary>
        internal long BytePosition { get; private set; }

        internal static Stream ValidateStream(Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException("messageStream");
            if (!stream.CanWrite)
                throw new ArgumentException("Expected a stream supporting write.", "messageStream");

            return stream;
        }

        internal void ThrowIfDisposed()
        {
            if ((Volatile.Read(ref _state) & DisposedFlag) != 0)
                throw new ObjectDisposedException(_ownerName);
        }

        /// <summary>
        /// Marks an operation as in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another operation is in progress, or the output has been left incomplete.</exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        internal void BeginOperation()
        {
            while (true)
            {
                var state = Volatile.Read(ref _state);
                if ((state & OperationInProgressFlag) != 0)
                    throw new InvalidOperationException("Another operation is in progress.  Only one operation may be in progress at a time.");
                if ((state & DisposedFlag) != 0)
                    throw new ObjectDisposedException(_ownerName);
                if (_isBroken)
                    throw new InvalidOperationException("The writer cannot continue after a write failed or was canceled, as its output is incomplete.");

                if (Interlocked.CompareExchange(ref _state, state | OperationInProgressFlag, state) == state) return;
            }
        }

        /// <summary>
        /// Marks the operation as finished.
        /// </summary>
        /// <returns><c>true</c> if the writer was disposed while the operation was in progress, and the caller must now release it.</returns>
        internal bool EndOperation()
        {
            while (true)
            {
                var state = Volatile.Read(ref _state);
                var mustRelease = (state & DisposedFlag) != 0 && (state & ReleasedFlag) == 0;
                var next = state & ~OperationInProgressFlag;
                if (mustRelease) next |= ReleasedFlag;

                if (Interlocked.CompareExchange(ref _state, next, state) == state) return mustRelease;
            }
        }

        /// <summary>
        /// Marks the writer as disposed.
        /// </summary>
        /// <returns><c>true</c> if nothing is in progress and this is the first dispose, so the caller must now release it.</returns>
        internal bool MarkDisposed()
        {
            while (true)
            {
                var state = Volatile.Read(ref _state);
                if ((state & DisposedFlag) != 0) return false;

                var mustRelease = (state & OperationInProgressFlag) == 0;
                var next = state | DisposedFlag;
                if (mustRelease) next |= ReleasedFlag;

                if (Interlocked.CompareExchange(ref _state, next, state) == state) return mustRelease;
            }
        }

        /// <summary>
        /// Records that the output can't be completed, so that later operations are refused.
        /// </summary>
        internal void MarkBroken()
        {
            _isBroken = true;
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        void ICancellationRecorder.RecordCancellation()
        {
            MarkBroken();
        }
#endif

        internal void Write(ReadOnlySpan<byte> bytes)
        {
            try
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                _stream.Write(bytes);
#else
                var block = ArrayPool<byte>.Shared.Rent(Math.Min(bytes.Length, CopyBlockSize));
                try
                {
                    for (var remaining = bytes; !remaining.IsEmpty;)
                    {
                        // A rented array may be longer than asked for, so each write is bounded by the block, not by the array.
                        var count = Math.Min(remaining.Length, CopyBlockSize);
                        remaining.Slice(0, count).CopyTo(block);
                        _stream.Write(block, 0, count);
                        remaining = remaining.Slice(count);
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(block);
                }
#endif
            }
            catch
            {
                _isBroken = true;
                throw;
            }

            BytePosition += bytes.Length;
        }

        internal void Flush()
        {
            try
            {
                _stream.Flush();
            }
            catch
            {
                _isBroken = true;
                throw;
            }
        }

        /// <summary>
        /// Closes the file the writer opened itself.
        /// </summary>
        internal void Release()
        {
            try
            {
                if (_ownsStream) _stream.Dispose();
            }
            finally
            {
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
                _released.TrySetResult(true);
#endif
            }
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        internal async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            try
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
                await _stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
#else
                if (MemoryMarshal.TryGetArray(bytes, out var segment))
                {
                    await _stream.WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await WriteCopiedAsync(bytes, cancellationToken).ConfigureAwait(false);
                }
#endif
            }
            catch
            {
                _isBroken = true;
                throw;
            }

            BytePosition += bytes.Length;
        }

        internal async Task FlushAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                _isBroken = true;
                throw;
            }
        }

        /// <summary>
        /// Closes the file the writer opened itself, asynchronously.
        /// </summary>
        internal async ValueTask ReleaseAsync()
        {
            try
            {
                if (!_ownsStream) return;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                await _stream.DisposeAsync().ConfigureAwait(false);
#else
                try
                {
                    await _stream.FlushAsync().ConfigureAwait(false);
                }
                finally
                {
                    _stream.Dispose();
                }
#endif
            }
            finally
            {
                _released.TrySetResult(true);
            }
        }

        /// <summary>
        /// Gets a task that completes once the writer has been released, for a dispose that is not the one to release it.
        /// </summary>
        internal ValueTask WaitForReleaseAsync()
        {
            return new ValueTask(_released.Task);
        }

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private async Task WriteCopiedAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            var block = ArrayPool<byte>.Shared.Rent(Math.Min(bytes.Length, CopyBlockSize));
            try
            {
                for (var remaining = bytes; !remaining.IsEmpty;)
                {
                    // A rented array may be longer than asked for, so each write is bounded by the block, not by the array.
                    var count = Math.Min(remaining.Length, CopyBlockSize);
                    remaining.Slice(0, count).CopyTo(block);
                    await _stream.WriteAsync(block, 0, count, cancellationToken).ConfigureAwait(false);
                    remaining = remaining.Slice(count);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(block);
            }
        }
#endif
#endif
    }
}
