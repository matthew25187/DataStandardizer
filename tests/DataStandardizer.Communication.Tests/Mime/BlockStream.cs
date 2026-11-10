using System;
using System.IO;

namespace DataStandardizer.Communication.Tests.Mime
{
    /// <summary>
    /// A stream over bytes in memory that returns at most a fixed number of them from each read, so that a reader decodes them in blocks of
    /// that size.
    /// </summary>
    /// <remarks>
    /// Each read asks for no fewer bytes than the block size, as a <see cref="StreamReader"/> reads into a buffer of at least 128 bytes.
    /// </remarks>
    internal sealed class BlockStream : MemoryStream
    {
        private readonly int _blockSize;

        public BlockStream(byte[] bytes, int blockSize)
            : base(bytes, false)
        {
            _blockSize = blockSize;
        }

        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, _blockSize);

#if NETCOREAPP
        public override int Read(Span<byte> buffer) => base.Read(buffer.Slice(0, _blockSize));
#endif
    }
}
