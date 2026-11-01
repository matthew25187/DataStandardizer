#if NETCOREAPP3_0_OR_GREATER
using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.LanguageTag.IntegrationTests
{
    public class AllocationMeterTests
    {
        /// <summary>
        /// The size of an <see cref="object"/> on the heap of a 64-bit process: an 8-byte object header, an 8-byte method table pointer, and the
        /// 8 bytes that the runtime reserves for every object.
        /// </summary>
        private const long ObjectSizeIn64BitProcess = 24;

        private static object _allocated;

        [Fact]
        public void MeasureAllocatedBytes_WithActionThatDoesNotAllocate_ReturnsZero()
        {
            // arrange
            Action action = () => { };

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(action);

            // assert
            testResult.Should().Be(0);
        }

        [Fact]
        public void MeasureAllocatedBytes_WithActionThatAllocatesOneObject_ReturnsTheSizeOfTheObject()
        {
            // arrange
            Action action = () => _allocated = new object();

            // act
            var testResult = AllocationMeter.MeasureAllocatedBytes(action);

            // assert
            testResult.Should().Be(ObjectSizeIn64BitProcess, "the tests run in a 64-bit process");
        }
    }
}
#endif
