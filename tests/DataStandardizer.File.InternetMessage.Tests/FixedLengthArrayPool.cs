using System.Buffers;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// A pool that rents a new array of one fixed length, whatever length is asked for, and keeps nothing that is returned.  Given a length longer than any asked
    /// for, it shows how code handles the longer arrays that <see cref="ArrayPool{T}.Rent"/> may return.
    /// </summary>
    internal sealed class FixedLengthArrayPool : ArrayPool<byte>
    {
        private readonly int _length;

        public FixedLengthArrayPool(int length)
        {
            _length = length;
        }

        public override byte[] Rent(int minimumLength)
        {
            return new byte[_length];
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
        }
    }
}
