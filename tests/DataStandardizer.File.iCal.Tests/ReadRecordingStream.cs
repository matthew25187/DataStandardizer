using System.Collections.Generic;
using System.IO;

namespace DataStandardizer.File.iCal.Tests
{
    /// <summary>
    /// A stream over bytes in memory that records how many bytes each synchronous read asks for, so that a test can see how large a buffer the
    /// reader of the stream has.
    /// </summary>
    internal sealed class ReadRecordingStream : MemoryStream
    {
        private readonly List<int> _requestedCounts = new List<int>();

        public ReadRecordingStream(byte[] data) : base(data, false)
        {
        }

        /// <summary>
        /// Gets the number of bytes each read asked for, in order.
        /// </summary>
        public IReadOnlyList<int> RequestedCounts => _requestedCounts;

        public override int Read(byte[] buffer, int offset, int count)
        {
            _requestedCounts.Add(count);
            return base.Read(buffer, offset, count);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override int Read(System.Span<byte> buffer)
        {
            _requestedCounts.Add(buffer.Length);
            return base.Read(buffer);
        }
#endif
    }
}
