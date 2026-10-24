#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System;
using System.Buffers;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// A buffer of bytes rented from the shared pool, which grows as it is written to.  The JSON writer encodes a calendar into it, so that
    /// nothing reaches the stream until the whole calendar has been encoded.
    /// </summary>
    internal sealed class JcalBufferWriter : IBufferWriter<byte>
    {
        private const int InitialSize = 16384;

        private byte[] _buffer = ArrayPool<byte>.Shared.Rent(InitialSize);

        /// <summary>
        /// Gets the bytes written so far.
        /// </summary>
        internal byte[] Buffer => _buffer;

        /// <summary>
        /// Gets the number of bytes written so far.
        /// </summary>
        internal int Count { get; private set; }

        public void Advance(int count)
        {
            if (count < 0 || Count + count > _buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            Count += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            Reserve(sizeHint);
            return new Memory<byte>(_buffer, Count, _buffer.Length - Count);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Reserve(sizeHint);
            return new Span<byte>(_buffer, Count, _buffer.Length - Count);
        }

        /// <summary>
        /// Discards what has been written.
        /// </summary>
        internal void Clear()
        {
            Count = 0;
        }

        /// <summary>
        /// Writes a byte.
        /// </summary>
        internal void Write(byte value)
        {
            Reserve(1);
            _buffer[Count++] = value;
        }

        /// <summary>
        /// Returns the buffer to the pool.  The writer cannot be used afterwards.
        /// </summary>
        internal void Release()
        {
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = new byte[0];
            Count = 0;
        }

        private void Reserve(int sizeHint)
        {
            var needed = Math.Max(sizeHint, 1);
            if (_buffer.Length - Count >= needed) return;

            var buffer = ArrayPool<byte>.Shared.Rent(Math.Max(_buffer.Length * 2, Count + needed));
            System.Buffer.BlockCopy(_buffer, 0, buffer, 0, Count);
            ArrayPool<byte>.Shared.Return(_buffer);
            _buffer = buffer;
        }
    }
}
#endif
