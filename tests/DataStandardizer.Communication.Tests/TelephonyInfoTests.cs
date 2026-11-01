using System;
using DataStandardizer.Communication.E164;
using DataStandardizer.Geography;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests
{
    public class TelephonyInfoTests
    {
        // Numbers for a geographic area with country code 1, the North American Numbering Plan, and the patterns they are formatted with.
        private const ulong NorthAmericanNumber = 12135550199L;
        private const ulong ShortPatternNumber = 12135550150L;
        private const ulong LongPatternNumber = 12135550190L;

        private const string PrefixedCompactPattern = "+cccssssssssssss";
        private const string SpacedPrefixCompactPattern = "+ cccssssssssssss";
        private const string PrefixedCountryThenSubscriberPattern = "+ccc ssssssssssss";
        private const string SpacedPrefixCountryThenSubscriberPattern = "+ ccc ssssssssssss";
        private const string PrefixedCountryAreaThenSubscriberPattern = "+ccc sss sssssssss";
        private const string SpacedPrefixCountryAreaThenSubscriberPattern = "+ ccc sss sssssssss";

        // A culture whose region is Australia.
        private const string AustralianEnglish = "en-AU";

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
        public void GetFormat_RequestICustomFormatter_ReturnsCustomFormatter()
        {
            // arrange
            var testSubject = new TelephonyInfo();

            // act
            var testResult = testSubject.GetFormat(typeof(ICustomFormatter));

            // assert
            testResult.Should().BeOfType<ItuE164InternationalNumberFormatter>();
        }

        [Fact]
        public void GetFormat_RequestItuE164InternationalNumberFormatInfo_ReturnsFormatInfo()
        {
            // arrange
            var testSubject = new TelephonyInfo();

            // act
            var testResult = testSubject.GetFormat(typeof(ItuE164InternationalNumberFormatInfo));

            // assert
            testResult.Should().BeOfType<ItuE164InternationalNumberFormatInfo>();
        }

        [Theory]
        [InlineData(12135550123L, "{0:g}", "+12135550123"),
         InlineData(12135550100L, "{0:G}", "+1 2135550100"),
         InlineData(NorthAmericanNumber, "{0:+cccsssssssssss}", "+12135550199"),
         InlineData(NorthAmericanNumber, "{0:+ cccssssssssssss}", "+ 12135550199"),
         InlineData(NorthAmericanNumber, "{0:+ccc ssssssssssss}", "+1 2135550199"),
         InlineData(NorthAmericanNumber, "{0:+ ccc ssssssssssss}", "+ 1 2135550199"),
         InlineData(NorthAmericanNumber, "{0:+ccc sss sssssssss}", "+1 213 5550199"),
         InlineData(NorthAmericanNumber, "{0:+ ccc sss sssssssss}", "+ 1 213 5550199")]
        public void InvariantTelephony_PassedToFormatCapableMethod_ReturnsFormattedOutput(ulong testValue, string compositeFormat, string expectedResult)
        {
            // arrange
            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);

            // act
            var testResult = string.Format(TelephonyInfo.InvariantTelephony, compositeFormat, testSubject);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(ShortPatternNumber, PrefixedCompactPattern, "+12135550150"),
         InlineData(ShortPatternNumber, SpacedPrefixCompactPattern, "+ 12135550150")]
        public void InvariantTelephony_PassedToFormatCapableMethodWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);

            // act
            var testResult = string.Format(formatProvider, "{0:i}", testSubject);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(LongPatternNumber, "+ccc ssssssssss", "+1 2135550190"),
         InlineData(LongPatternNumber, SpacedPrefixCountryThenSubscriberPattern, "+ 1 2135550190"),
         InlineData(LongPatternNumber, PrefixedCountryAreaThenSubscriberPattern, "+1 213 5550190"),
         InlineData(LongPatternNumber, SpacedPrefixCountryAreaThenSubscriberPattern, "+ 1 213 5550190")]
        public void InvariantTelephony_PassedToFormatCapableMethodWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);

            // act
            var testResult = string.Format(formatProvider, "{0:I}", testSubject);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Fact]
        public void Iso3166Part1Alpha2Code_RegionSpecificFormatInfo_ReturnsCountryCode()
        {
            // arrange
            const Iso3166Part1Alpha2Country countryCode = Iso3166Part1Alpha2Country.US;
            var testSubject = new TelephonyInfo(countryCode);

            // act
            var testResult = testSubject.Iso3166Part1Alpha2Code;

            // assert
            testResult.Should().Be(countryCode);
        }

        [Fact]
        public void Iso3166Part1Alpha3Code_RegionSpecificFormatInfo_ReturnsCountryCode()
        {
            // arrange
            const Iso3166Part1Alpha3Country countryCode = Iso3166Part1Alpha3Country.USA;
            var testSubject = new TelephonyInfo(countryCode);

            // act
            var testResult = testSubject.Iso3166Part1Alpha3Code;

            // assert
            testResult.Should().Be(countryCode);
        }

        [Fact]
        public void ItuE164InternationalNumberFormat_TelephonyInfoIsNotReadOnly_SetsSuccessfully()
        {
            // arrange
            var testSubject = new TelephonyInfo { IsReadOnly = false };
            var numberFormat = new ItuE164InternationalNumberFormatInfo();

            // act
            testSubject.ItuE164InternationalNumberFormat = numberFormat;

            // assert
            testSubject.ItuE164InternationalNumberFormat.Should().BeSameAs(numberFormat);
        }

        [Fact]
        public void ItuE164InternationalNumberFormat_TelephonyInfoIsReadOnly_ThrowsInvalidOperationExceptionOnSet()
        {
            // arrange
            var testSubject = new TelephonyInfo { IsReadOnly = true };
            var numberFormat = new ItuE164InternationalNumberFormatInfo();

            // act
            Action testAction = () => testSubject.ItuE164InternationalNumberFormat = numberFormat;

            // assert
            testAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage("TelephonyInfo is read only.");
        }
    }
}
