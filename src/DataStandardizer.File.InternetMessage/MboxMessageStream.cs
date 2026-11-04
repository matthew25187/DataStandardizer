using System;
using System.IO;
#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;
#endif

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The current message of an <see cref="MboxReader"/>, with its quotes removed, as a stream for an <see cref="ImfReader"/> or <see cref="ImfEntityReader"/> to read.
    /// </summary>
    internal sealed class MboxMessageStream : Stream
    {
        private readonly MboxReader _owner;
        private readonly bool _isOwnOperation;

        /// <param name="owner">The reader.</param>
        /// <param name="isOwnOperation"><c>true</c> if each read of this stream is an operation of the reader, for a caller that reads events.  <c>false</c> if the reader is already in an
        /// operation that reads the whole message.</param>
        internal MboxMessageStream(MboxReader owner, bool isOwnOperation)
        {
            _owner = owner;
            _isOwnOperation = isOwnOperation;
        }

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

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _owner.ReadContent(new Memory<byte>(buffer, offset, count), _isOwnOperation);
        }

#if NETSTANDARD2_0_OR_GREATER || NETCOREAPP2_0_OR_GREATER
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return _owner.ReadContentAsync(new Memory<byte>(buffer, offset, count), _isOwnOperation, cancellationToken);
        }
#endif

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return new ValueTask<int>(_owner.ReadContentAsync(buffer, _isOwnOperation, cancellationToken));
        }
#endif

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
}
