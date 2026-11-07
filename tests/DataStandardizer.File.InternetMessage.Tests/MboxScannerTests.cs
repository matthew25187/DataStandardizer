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
    }
}
