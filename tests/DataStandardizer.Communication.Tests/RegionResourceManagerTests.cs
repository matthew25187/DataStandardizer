using System.Globalization;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests
{
    public class RegionResourceManagerTests
    {
        private const string RegionCultureName = "en-AU";
        private const string RegionValue = "region";
        private const string NeutralValue = "neutral";
        private const string ResourceName = "name";

        [Fact]
        public void GetString_RegionCulture_ReturnsRegionValue()
        {
            // arrange
            var testSubject = new RegionResourceManager(RegionCultureName, RegionValue, NeutralValue);
            var culture = CultureInfo.GetCultureInfo(RegionCultureName);

            // act
            var testResult = testSubject.GetString(ResourceName, culture);

            // assert
            testResult.Should().Be(RegionValue);
        }

        [Fact]
        public void GetString_OtherCulture_ReturnsNeutralValue()
        {
            // arrange
            var testSubject = new RegionResourceManager(RegionCultureName, RegionValue, NeutralValue);
            var culture = CultureInfo.GetCultureInfo("en-US");

            // act
            var testResult = testSubject.GetString(ResourceName, culture);

            // assert
            testResult.Should().Be(NeutralValue);
        }

        [Fact]
        public void GetString_NoCulture_ReturnsNeutralValue()
        {
            // arrange
            var testSubject = new RegionResourceManager(RegionCultureName, RegionValue, NeutralValue);

            // act
            var testResult = testSubject.GetString(ResourceName);

            // assert
            testResult.Should().Be(NeutralValue);
        }
    }
}
