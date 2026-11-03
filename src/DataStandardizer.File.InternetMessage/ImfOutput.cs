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
    {
        /// <summary>
        /// Size of the buffer of the file stream that a file-path constructor opens.
        /// </summary>
        internal const int FileBufferSize = 16 * 1024;

        private const int CopyBlockSize = 16 * 1024;

        private readonly string _ownerName;
        private readonly bool _ownsStream;
        private readonly Stream _stream;
        private bool _isBroken;

        // These are changed with Interlocked, so that a Dispose from another thread while an operation is in progress releases the stream once only,
        // after that operation has finished with it.
        private int _isDisposed;
        private int _isOperationInProgress;
        private int _isReleased;

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
            if (Volatile.Read(ref _isDisposed) != 0)
                throw new ObjectDisposedException(_ownerName);
        }

        /// <summary>
        /// Marks an operation as in progress.
        /// </summary>
        /// <exception cref="InvalidOperationException">Another operation is in progress, or the output has been left incomplete.</exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        internal void BeginOperation()
        {
            if (Interlocked.CompareExchange(ref _isOperationInProgress, 1, 0) != 0)
                throw new InvalidOperationException("Another operation is in progress.  Only one operation may be in progress at a time.");

            // Dispose may have run between the caller's check and this one.
            if (Volatile.Read(ref _isDisposed) != 0)
            {
                if (EndOperation()) Release();

                throw new ObjectDisposedException(_ownerName);
            }

            if (_isBroken)
            {
                if (EndOperation()) Release();

                throw new InvalidOperationException("The writer cannot continue after a write failed or was canceled, as its output is incomplete.");
            }
        }

        /// <summary>
        /// Marks the operation as finished.
        /// </summary>
        /// <returns><c>true</c> if the writer was disposed while the operation was in progress, and the caller must now release it.</returns>
        internal bool EndOperation()
        {
            Interlocked.Exchange(ref _isOperationInProgress, 0);
            return Volatile.Read(ref _isDisposed) != 0 && Interlocked.Exchange(ref _isReleased, 1) == 0;
        }

        /// <summary>
        /// Marks the writer as disposed.
        /// </summary>
        /// <returns><c>true</c> if nothing is in progress and this is the first dispose, so the caller must now release it.</returns>
        internal bool MarkDisposed()
        {
            if (Interlocked.Exchange(ref _isDisposed, 1) != 0) return false;

            return Volatile.Read(ref _isOperationInProgress) == 0 && Interlocked.Exchange(ref _isReleased, 1) == 0;
        }

        /// <summary>
        /// Records that the output can't be completed, so that later operations are refused.
        /// </summary>
        internal void MarkBroken()
        {
            _isBroken = true;
        }

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
                        var count = Math.Min(remaining.Length, block.Length);
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
            _stream.Flush();
        }

        /// <summary>
        /// Closes the file the writer opened itself.
        /// </summary>
        internal void Release()
        {
            if (_ownsStream) _stream.Dispose();
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

        internal Task FlushAsync(CancellationToken cancellationToken)
        {
            return _stream.FlushAsync(cancellationToken);
        }

        /// <summary>
        /// Closes the file the writer opened itself, asynchronously.
        /// </summary>
        internal async ValueTask ReleaseAsync()
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

#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private async Task WriteCopiedAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            var block = ArrayPool<byte>.Shared.Rent(Math.Min(bytes.Length, CopyBlockSize));
            try
            {
                for (var remaining = bytes; !remaining.IsEmpty;)
                {
                    var count = Math.Min(remaining.Length, block.Length);
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
