using System.Diagnostics.CodeAnalysis;
using FluentAssertions;

namespace DataStandardizer.Core.Tests
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class IStringEnumTests
    {
        [Fact]
        public void GetInterfaces_IStringEnum_ReturnsOnlyIComparable()
        {
            // act
            var testResult = typeof(IStringEnum).GetInterfaces();

            // assert
            testResult.Should().Equal(typeof(IComparable));
        }
    }
}
