using System.Globalization;
using DataStandardizer.Communication.E164;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.IntegrationTests.E164
{
    /// <summary>
    /// Tests that the culture of the current thread doesn't change how an <see cref="ItuE164InternationalNumber"/> is formatted.
    /// </summary>
    /// <remarks>
    /// These set <see cref="CultureInfo.CurrentCulture"/>, which is state outside the code under test, so they are integration tests.
    /// </remarks>
    public class ItuE164InternationalNumberCultureTests
    {
        // A number for a geographic area in Great Britain, country code 44.
        private const ulong GreatBritainNumber = 443071234567L;

        // GreatBritainNumber in the invariant telephony's long international pattern: the international prefix symbol, the Country Code
        // and, after a space, the national significant number (ITU-T E.123 international notation).
        private const string GreatBritainNumberInInvariantLongInternationalPattern = "+44 3071234567";

        // The long international format.
        private const string LongInternationalFormat = "I";

        // A culture whose region has a country code, used to show that the culture of the current thread doesn't change the result.
        private const string GreatBritainCultureName = "en-GB";

        [Fact]
        public void ToString_LongInternationalFormatAndInvariantCultureUnderGreatBritainCulture_ReturnsInvariantPattern()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(GreatBritainNumber);
            using (new CultureScope(GreatBritainCultureName))
            {
                // act
                var testResult = testSubject.ToString(LongInternationalFormat, CultureInfo.InvariantCulture);

                // assert
                testResult.Should().Be(GreatBritainNumberInInvariantLongInternationalPattern);
            }
        }

        [Fact]
        public void ToString_LongInternationalFormatUnderGreatBritainCulture_ReturnsInvariantPattern()
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(GreatBritainNumber);
            using (new CultureScope(GreatBritainCultureName))
            {
                // act
                var testResult = testSubject.ToString(LongInternationalFormat);

                // assert
                testResult.Should().Be(GreatBritainNumberInInvariantLongInternationalPattern);
            }
        }
    }
}
