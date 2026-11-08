using DataStandardizer.Geography;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests
{
    /// <summary>
    /// Tests of <see cref="TelephonyInfo"/> that depend on the thread's culture or the operating system's culture data.
    /// </summary>
    /// <remarks>
    /// Whether the platform has a culture for a region varies with its culture data and globalization mode, so these are integration
    /// tests. Their expected results are the same either way.
    /// </remarks>
    public class TelephonyInfoTests
    {
        // The package's neutral patterns, from its Resources.resx.
        private const string InvariantLongPattern = "+ccc iiiissssssssssssss";
        private const string InvariantShortPattern = "+ccciiiissssssssssssss";

        // A culture whose region is Australia.
        private const string AustralianEnglish = "en-AU";

        // A culture whose region subtag is the UN M.49 code for Latin America and the Caribbean (RFC 5646, section 2.2.4).
        private const string LatinAmericanSpanish = "es-419";

        [Theory]
        [InlineData(""), InlineData("en")]
        public void CurrentTelephony_CurrentCultureWithoutRegion_ReturnsReadOnlyTelephonyWithoutCountry(string cultureName)
        {
            // arrange
            using (new CultureScope(cultureName))
            {
                // act
                var testResult = TelephonyInfo.CurrentTelephony;

                // assert
                testResult.Iso3166Part1Alpha2Code.Should().BeNull();
                testResult.IsReadOnly.Should().BeTrue();
            }
        }

        [Fact]
        public void CurrentTelephony_CurrentCultureWithCountry_ReturnsReadOnlyTelephonyForCountry()
        {
            // arrange
            using (new CultureScope(AustralianEnglish))
            {
                // act
                var testResult = TelephonyInfo.CurrentTelephony;

                // assert
                testResult.Iso3166Part1Alpha2Code.Should().Be(Iso3166Part1Alpha2Country.AU);
                testResult.IsReadOnly.Should().BeTrue();
            }
        }

        [Fact]
        public void CurrentTelephony_CurrentCultureWithUnM49Region_ReturnsTelephonyWithoutCountry()
        {
            // arrange
            using (new CultureScope(LatinAmericanSpanish))
            {
                // act
                var testResult = TelephonyInfo.CurrentTelephony;

                // assert
                testResult.Iso3166Part1Alpha2Code.Should().BeNull();
            }
        }

        [Fact]
        public void Constructor_RegionThePlatformMayHaveNoCultureFor_LoadsInvariantLongInternationalNumberPattern()
        {
            // arrange
            const Iso3166Part1Alpha2Country countryCode = Iso3166Part1Alpha2Country.AQ;

            // act
            var testResult = new TelephonyInfo(countryCode);

            // assert
            testResult.ItuE164InternationalNumberFormat.LongInternationalNumberPattern.Should().Be(InvariantLongPattern);
        }

        [Fact]
        public void Constructor_RegionThePlatformMayHaveNoCultureFor_LoadsInvariantShortInternationalNumberPattern()
        {
            // arrange
            const Iso3166Part1Alpha2Country countryCode = Iso3166Part1Alpha2Country.AQ;

            // act
            var testResult = new TelephonyInfo(countryCode);

            // assert
            testResult.ItuE164InternationalNumberFormat.ShortInternationalNumberPattern.Should().Be(InvariantShortPattern);
        }
    }
}
