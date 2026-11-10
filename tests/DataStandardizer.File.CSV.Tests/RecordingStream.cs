using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Xunit;
namespace DataStandardizer.File.CSV.Tests
{

    /// <summary>
    /// An in-memory stream that records the size of every read it is asked for and every write it is given, so that tests
    /// can see the size of the buffer a reader or writer uses on it.
    /// </summary>
    /// <remarks>
    /// It wraps a <see cref="MemoryStream"/> rather than deriving from one, because the span overloads of a class derived
    /// from <see cref="MemoryStream"/> call its array overloads, which would record each read or write twice.
    /// </remarks>
    internal sealed class RecordingStream : Stream
    {
        private readonly MemoryStream _stream;

        public RecordingStream()
        {
            _stream = new MemoryStream();
        }

        public RecordingStream(byte[] buffer)
        {
            _stream = new MemoryStream(buffer);
        }

        /// <summary>
        /// Gets the number of bytes asked for by each read, in order.
        /// </summary>
        public List<int> ReadSizes { get; } = new List<int>();

        /// <summary>
        /// Gets the number of bytes given to each write, in order.
        /// </summary>
        public List<int> WriteSizes { get; } = new List<int>();

        public override bool CanRead => _stream.CanRead;

        public override bool CanSeek => _stream.CanSeek;

        public override bool CanWrite => _stream.CanWrite;

        public override long Length => _stream.Length;

        public override long Position
        {
            get => _stream.Position;
            set => _stream.Position = value;
        }

        public override void Flush() => _stream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            ReadSizes.Add(count);
            return _stream.Read(buffer, offset, count);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override int Read(Span<byte> buffer)
        {
            ReadSizes.Add(buffer.Length);
            return _stream.Read(buffer);
        }
#endif

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);

        public override void SetLength(long value) => _stream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteSizes.Add(count);
            _stream.Write(buffer, offset, count);
        }

#if NETCOREAPP2_1_OR_GREATER
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            WriteSizes.Add(buffer.Length);
            _stream.Write(buffer);
        }
#endif

        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
            base.Dispose(disposing);
        }
    }
}
