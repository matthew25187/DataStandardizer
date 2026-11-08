using System;
using System.Collections.Generic;
using System.Resources;
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

        // The names of the resources that hold the number patterns.
        private const string LongPatternResourceName = nameof(ItuE164InternationalNumberFormatInfo.LongInternationalNumberPattern);
        private const string ShortPatternResourceName = nameof(ItuE164InternationalNumberFormatInfo.ShortInternationalNumberPattern);

        // Patterns held by test resources for the region of Australia, and for every other culture.
        private const string AustralianLongPattern = PrefixedCountryAreaThenSubscriberPattern;
        private const string AustralianShortPattern = PrefixedCompactPattern;
        private const string NeutralLongPattern = SpacedPrefixCountryThenSubscriberPattern;
        private const string NeutralShortPattern = SpacedPrefixCompactPattern;

        // Constructors for Australia, by its ISO 3166-1 alpha-2 and alpha-3 codes.
        public static TheoryData<Func<ResourceManager, TelephonyInfo>> AustraliaConstructors => new TheoryData<Func<ResourceManager, TelephonyInfo>>
        {
            resources => new TelephonyInfo(Iso3166Part1Alpha2Country.AU, resources),
            resources => new TelephonyInfo(Iso3166Part1Alpha3Country.AUS, resources),
        };

        // Constructors for the United States, a country the test resources hold nothing for, by its ISO 3166-1 alpha-2 and alpha-3 codes.
        public static TheoryData<Func<ResourceManager, TelephonyInfo>> UnitedStatesConstructors => new TheoryData<Func<ResourceManager, TelephonyInfo>>
        {
            resources => new TelephonyInfo(Iso3166Part1Alpha2Country.US, resources),
            resources => new TelephonyInfo(Iso3166Part1Alpha3Country.USA, resources),
        };

        [Theory]
        [MemberData(nameof(AustraliaConstructors))]
        public void Constructor_CountryWithRegionResources_LoadsRegionLongInternationalNumberPattern(Func<ResourceManager, TelephonyInfo> createTelephony)
        {
            // arrange
            var resources = CreateResources(AustralianLongPattern, AustralianShortPattern);

            // act
            var testResult = createTelephony(resources);

            // assert
            testResult.ItuE164InternationalNumberFormat.LongInternationalNumberPattern.Should().Be(AustralianLongPattern);
        }

        [Theory]
        [MemberData(nameof(AustraliaConstructors))]
        public void Constructor_CountryWithRegionResources_LoadsRegionShortInternationalNumberPattern(Func<ResourceManager, TelephonyInfo> createTelephony)
        {
            // arrange
            var resources = CreateResources(AustralianLongPattern, AustralianShortPattern);

            // act
            var testResult = createTelephony(resources);

            // assert
            testResult.ItuE164InternationalNumberFormat.ShortInternationalNumberPattern.Should().Be(AustralianShortPattern);
        }

        [Theory]
        [MemberData(nameof(UnitedStatesConstructors))]
        public void Constructor_CountryWithoutRegionResources_LoadsNeutralLongInternationalNumberPattern(Func<ResourceManager, TelephonyInfo> createTelephony)
        {
            // arrange
            var resources = CreateResources(AustralianLongPattern, AustralianShortPattern);

            // act
            var testResult = createTelephony(resources);

            // assert
            testResult.ItuE164InternationalNumberFormat.LongInternationalNumberPattern.Should().Be(NeutralLongPattern);
        }

        [Theory]
        [MemberData(nameof(UnitedStatesConstructors))]
        public void Constructor_CountryWithoutRegionResources_LoadsNeutralShortInternationalNumberPattern(Func<ResourceManager, TelephonyInfo> createTelephony)
        {
            // arrange
            var resources = CreateResources(AustralianLongPattern, AustralianShortPattern);

            // act
            var testResult = createTelephony(resources);

            // assert
            testResult.ItuE164InternationalNumberFormat.ShortInternationalNumberPattern.Should().Be(NeutralShortPattern);
        }

        [Fact]
        public void Constructor_RegionResourcesWithoutShortPattern_LoadsRegionLongInternationalNumberPattern()
        {
            // arrange
            var resources = CreateResourcesWithRegionLongPatternOnly(AustralianLongPattern);

            // act
            var testResult = new TelephonyInfo(Iso3166Part1Alpha2Country.AU, resources);

            // assert
            testResult.ItuE164InternationalNumberFormat.LongInternationalNumberPattern.Should().Be(AustralianLongPattern);
        }

        [Fact]
        public void Constructor_RegionResourcesWithoutShortPattern_LoadsNeutralShortInternationalNumberPattern()
        {
            // arrange
            var resources = CreateResourcesWithRegionLongPatternOnly(AustralianLongPattern);

            // act
            var testResult = new TelephonyInfo(Iso3166Part1Alpha2Country.AU, resources);

            // assert
            testResult.ItuE164InternationalNumberFormat.ShortInternationalNumberPattern.Should().Be(NeutralShortPattern);
        }

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
        public void GetCultureCountryCode_CultureWithAlpha2Region_ReturnsCountry()
        {
            // arrange
            const string cultureName = AustralianEnglish;

            // act
            var testResult = TelephonyInfo.GetCultureCountryCode(cultureName);

            // assert
            testResult.Should().Be(Iso3166Part1Alpha2Country.AU);
        }

        // Region subtags that are UN M.49 codes (RFC 5646, section 2.2.4): 419 is Latin America and the Caribbean, an area, and
        // 036 is Australia, a country whose region subtag is AU.
        [Theory]
        [InlineData("es-419"), InlineData("en-036")]
        public void GetCultureCountryCode_CultureWithUnM49Region_ReturnsNull(string cultureName)
        {
            // act
            var testResult = TelephonyInfo.GetCultureCountryCode(cultureName);

            // assert
            testResult.Should().BeNull();
        }

        [Theory]
        [InlineData(""), InlineData("en")]
        public void GetCultureCountryCode_CultureWithoutRegion_ReturnsNull(string cultureName)
        {
            // act
            var testResult = TelephonyInfo.GetCultureCountryCode(cultureName);

            // assert
            testResult.Should().BeNull();
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

        // Resources that hold the given patterns for the region of Australia, and the neutral patterns for every other culture.
        private static RegionResourceManager CreateResources(string regionLongPattern, string regionShortPattern) =>
            new RegionResourceManager(
                AustralianEnglish,
                new Dictionary<string, string> { [LongPatternResourceName] = regionLongPattern, [ShortPatternResourceName] = regionShortPattern },
                CreateNeutralPatterns());

        // Resources that hold only a long pattern for the region of Australia, and the neutral patterns for every other culture.
        private static RegionResourceManager CreateResourcesWithRegionLongPatternOnly(string regionLongPattern) =>
            new RegionResourceManager(
                AustralianEnglish,
                new Dictionary<string, string> { [LongPatternResourceName] = regionLongPattern },
                CreateNeutralPatterns());

        private static Dictionary<string, string> CreateNeutralPatterns() =>
            new Dictionary<string, string> { [LongPatternResourceName] = NeutralLongPattern, [ShortPatternResourceName] = NeutralShortPattern };
    }
}
