using System.Buffers;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.InternetMessage.Tests
{
    /// <summary>
    /// Tests of what <see cref="MboxScanner"/> asks of the driver that wraps it, which the readers don't show.
    /// </summary>
    public class MboxScannerTests
    {
        // The scanner's block, 16 KiB.
        private const int BlockLength = 16384;

        // Longer than the block, as ArrayPool<T>.Rent may give.
        private const int RentedArrayLength = 32768;

        [Fact]
        public void PrepareRefill_RentedArrayLongerThanBlock_AsksForNoMoreThanBlock()
        {
            // arrange
            var subject = new MboxScanner(MboxVariant.Mboxrd, new ImfOptions(), new FixedLengthArrayPool(RentedArrayLength));

            // act
            subject.PrepareRefill(out var offset, out var count);

            // assert
            offset.Should().Be(0);
            count.Should().Be(BlockLength);
        }

        /// <summary>
        /// A pool that rents a new array of one fixed length, whatever length is asked for, and keeps nothing that is returned.
        /// </summary>
        private sealed class FixedLengthArrayPool : ArrayPool<byte>
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
}
