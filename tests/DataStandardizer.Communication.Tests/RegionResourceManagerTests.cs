using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests
{
    public class RegionResourceManagerTests
    {
        private const string RegionCultureName = "en-AU";
        private const string ResourceName = "name";
        private const string OtherResourceName = "other";
        private const string RegionValue = "region";
        private const string NeutralValue = "neutral";

        [Fact]
        public void GetString_RegionCultureAndNameRegionHolds_ReturnsRegionValue()
        {
            // arrange
            var testSubject = CreateTestSubject();
            var culture = CultureInfo.GetCultureInfo(RegionCultureName);

            // act
            var testResult = testSubject.GetString(ResourceName, culture);

            // assert
            testResult.Should().Be(RegionValue);
        }

        [Fact]
        public void GetString_RegionCultureAndNameRegionDoesNotHold_ReturnsNull()
        {
            // arrange
            var testSubject = CreateTestSubject();
            var culture = CultureInfo.GetCultureInfo(RegionCultureName);

            // act
            var testResult = testSubject.GetString(OtherResourceName, culture);

            // assert
            testResult.Should().BeNull();
        }

        [Fact]
        public void GetString_OtherCulture_ReturnsNeutralValue()
        {
            // arrange
            var testSubject = CreateTestSubject();
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
            var testSubject = CreateTestSubject();

            // act
            var testResult = testSubject.GetString(ResourceName);

            // assert
            testResult.Should().Be(NeutralValue);
        }

        private static RegionResourceManager CreateTestSubject() =>
            new RegionResourceManager(
                RegionCultureName,
                new Dictionary<string, string> { [ResourceName] = RegionValue },
                new Dictionary<string, string> { [ResourceName] = NeutralValue, [OtherResourceName] = NeutralValue });
    }
}
