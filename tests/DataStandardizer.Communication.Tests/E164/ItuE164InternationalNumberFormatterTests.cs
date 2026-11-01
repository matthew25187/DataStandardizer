using DataStandardizer.Communication.E164;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.E164
{
    public class ItuE164InternationalNumberFormatterTests
    {
        // The format strings of ItuE164InternationalNumberFormatter: general, general with separators, and the short and long international
        // number patterns of the format provider.
        private const string GeneralFormat = "g";
        private const string GeneralFormatWithSeparators = "G";
        private const string ShortInternationalFormat = "i";
        private const string LongInternationalFormat = "I";

        // Custom patterns: '+' is the international prefix symbol, 'c' a digit of the country code, 'i' of the identification code, 's' of
        // the subscriber number, and a space a separator. "Spaced" patterns have a space after the prefix symbol.
        private const string CountryThenIdentificationAndSubscriberPattern = "+ccc isssssssssss";
        private const string SpacedCountryThenIdentificationAndSubscriberPattern = "+ ccc isssssssssss";
        private const string CountryIdentificationAndSubscriberPattern = "+cccisssssssssss";
        private const string SpacedCountryIdentificationAndSubscriberPattern = "+ cccisssssssssss";
        private const string CountryThenIdentificationThenSubscriberPattern = "+ccc i sssssssssss";
        private const string SpacedCountryThenIdentificationThenSubscriberPattern = "+ ccc i sssssssssss";
        private const string CountryThenIdentificationThenGroupedSubscriberPattern = "+ccc i sss ssssssss";
        private const string SpacedCountryThenIdentificationThenGroupedSubscriberPattern = "+ ccc i sss ssssssss";
        private const string OneDigitCountryAndSubscriberPattern = "+cssssssssssss";
        private const string OneDigitCountryThenSubscriberPattern = "+c ssssssssssss";
        private const string OneDigitCountryThenGroupedSubscriberPattern = "+c sss sssssssss";
        private const string CountryThenSubscriberInThreesPattern = "+ccc sss sss";
        private const string SpacedCountryThenSubscriberInThreesPattern = "+ ccc sss sss";

        // Numbers for a geographic area, country code 1.
        private const ulong GeographicAreaNumber = 12135550123L;
        private const ulong OtherGeographicAreaNumber = 12505550199L;

        // Numbers for global services, country code 800.
        private const ulong GlobalServiceNumber = 800282828L;
        private const ulong OtherGlobalServiceNumber = 800457457L;

        // Numbers for networks, country codes 881, 882 and 883.
        private const ulong NetworkNumberWithIdentificationCode6 = 88161235550100L;
        private const ulong OtherNetworkNumberWithIdentificationCode6 = 88161235550101L;
        private const ulong NetworkNumberWithIdentificationCode7 = 88171235550102L;
        private const ulong NetworkNumberWithIdentificationCode8 = 88181235550103L;
        private const ulong NetworkNumberWithIdentificationCode9 = 88191235550104L;
        private const ulong NetworkNumberOfCountryCode882 = 882101235550105L;
        private const ulong NetworkNumberOfCountryCode883 = 88321035550199L;

        // Numbers for groups of countries, country code 388, identification code 3.
        private const ulong GroupOfCountriesNumber0102 = 38835550102L;
        private const ulong GroupOfCountriesNumber0103 = 38835550103L;
        private const ulong GroupOfCountriesNumber0104 = 38835550104L;
        private const ulong GroupOfCountriesNumber0105 = 38835550105L;
        private const ulong GroupOfCountriesNumber0106 = 38835550106L;
        private const ulong GroupOfCountriesNumber0107 = 38835550107L;
        private const ulong GroupOfCountriesNumber0108 = 38835550108L;

        // Numbers for trials, country code 991, identification code 1: one with no subscriber number, and others with one.
        private const ulong TrialNumberWithoutSubscriber = 9911L;
        private const ulong TrialNumber0102 = 99115550102L;
        private const ulong TrialNumber0103 = 99115550103L;
        private const ulong TrialNumber0104 = 99115550104L;
        private const ulong TrialNumber0105 = 99115550105L;
        private const ulong TrialNumber0106 = 99115550106L;

        // Formatted numbers that more than one test expects.
        private const string GeographicAreaNumberFormatted = "+12135550123";
        private const string GeographicAreaNumberFormattedWithSeparator = "+1 2135550123";
        private const string GlobalServiceNumberFormatted = "+800282828";
        private const string OtherGlobalServiceNumberFormattedWithSeparator = "+800 457457";
        private const string OtherGlobalServiceNumberInThrees = "+800 457 457";
        private const string OtherGlobalServiceNumberSpacedInThrees = "+ 800 457 457";
        private const string TrialNumberWithoutSubscriberFormatted = "+9911";
        private const string TrialNumberWithoutSubscriberSpaced = "+ 9911";
        private const string TrialNumberWithoutSubscriberWithSeparator = "+991 1";

        [Theory]
        [InlineData(GeographicAreaNumber, GeneralFormat, GeographicAreaNumberFormatted),
         InlineData(GeographicAreaNumber, GeneralFormatWithSeparators, GeographicAreaNumberFormattedWithSeparator)]
        public void Format_InternationalNumberForGeographicAreaWithGeneralFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(OtherGeographicAreaNumber, OneDigitCountryAndSubscriberPattern, "+12505550199")]
        public void Format_InternationalNumberForGeographicAreaWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(ShortInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(OtherGeographicAreaNumber, OneDigitCountryThenSubscriberPattern, "+1 2505550199"),
         InlineData(OtherGeographicAreaNumber, OneDigitCountryThenGroupedSubscriberPattern, "+1 250 5550199"),
         InlineData(OtherGeographicAreaNumber, "+c sss sss ssss", "+1 250 555 0199")]
        public void Format_InternationalNumberForGeographicAreaWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(LongInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(GeographicAreaNumber, "cssssssssssss", "12135550123"),
         InlineData(GeographicAreaNumber, OneDigitCountryAndSubscriberPattern, GeographicAreaNumberFormatted),
         InlineData(GeographicAreaNumber, OneDigitCountryThenSubscriberPattern, GeographicAreaNumberFormattedWithSeparator),
         InlineData(GeographicAreaNumber, OneDigitCountryThenGroupedSubscriberPattern, "+1 213 5550123"),
         InlineData(GeographicAreaNumber, "+c sss sss ssssss", "+1 213 555 0123")]
        public void Format_InternationalNumberForGeographicAreaWithCustomFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(GlobalServiceNumber, GeneralFormat, GlobalServiceNumberFormatted),
         InlineData(OtherGlobalServiceNumber, GeneralFormatWithSeparators, OtherGlobalServiceNumberFormattedWithSeparator)]
        public void Format_InternationalNumberForGlobalServiceWithGeneralFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGlobalService(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(GlobalServiceNumber, "+cccssssssssssss", GlobalServiceNumberFormatted),
         InlineData(GlobalServiceNumber, "+ cccssssssssssss", "+ 800282828")]
        public void Format_InternationalNumberForGlobalServiceWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGlobalService(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(ShortInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(OtherGlobalServiceNumber, "+ccc ssssssssssss", OtherGlobalServiceNumberFormattedWithSeparator),
         InlineData(OtherGlobalServiceNumber, "+ ccc ssssssssssss", "+ 800 457457"),
         InlineData(OtherGlobalServiceNumber, CountryThenSubscriberInThreesPattern, OtherGlobalServiceNumberInThrees),
         InlineData(OtherGlobalServiceNumber, SpacedCountryThenSubscriberInThreesPattern, OtherGlobalServiceNumberSpacedInThrees)]
        public void Format_InternationalNumberForGlobalServiceWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGlobalService(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(LongInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(GlobalServiceNumber, "+ccc ss ss ss", "+800 28 28 28"),
         InlineData(GlobalServiceNumber, "+ ccc ss ss ss", "+ 800 28 28 28"),
         InlineData(OtherGlobalServiceNumber, CountryThenSubscriberInThreesPattern, OtherGlobalServiceNumberInThrees),
         InlineData(OtherGlobalServiceNumber, SpacedCountryThenSubscriberInThreesPattern, OtherGlobalServiceNumberSpacedInThrees)]
        public void Format_InternationalNumberForGlobalServiceWithCustomFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGlobalService(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(88101235550199L, GeneralFormat, "+88101235550199"),
         InlineData(88111235550199L, GeneralFormatWithSeparators, "+881 11235550199")]
        public void Format_InternationalNumberForNetworkWithGeneralFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGlobalService(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(NetworkNumberOfCountryCode883, "+ccciiiissssssss", "+88321035550199"),
         InlineData(NetworkNumberOfCountryCode883, "+ ccciiiissssssss", "+ 88321035550199")]
        public void Format_InternationalNumberForNetworkWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForNetwork(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(ShortInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(NetworkNumberWithIdentificationCode6, CountryThenIdentificationAndSubscriberPattern, "+881 61235550100"),
         InlineData(NetworkNumberWithIdentificationCode6, SpacedCountryThenIdentificationAndSubscriberPattern, "+ 881 61235550100"),
         InlineData(OtherNetworkNumberWithIdentificationCode6, CountryThenIdentificationThenSubscriberPattern, "+881 6 1235550101"),
         InlineData(OtherNetworkNumberWithIdentificationCode6, SpacedCountryThenIdentificationThenSubscriberPattern, "+ 881 6 1235550101")]
        public void Format_InternationalNumberForNetworkWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForNetwork(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(LongInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(OtherNetworkNumberWithIdentificationCode6, CountryIdentificationAndSubscriberPattern, "+88161235550101"),
         InlineData(OtherNetworkNumberWithIdentificationCode6, SpacedCountryIdentificationAndSubscriberPattern, "+ 88161235550101"),
         InlineData(NetworkNumberWithIdentificationCode7, CountryThenIdentificationAndSubscriberPattern, "+881 71235550102"),
         InlineData(NetworkNumberWithIdentificationCode7, SpacedCountryThenIdentificationAndSubscriberPattern, "+ 881 71235550102"),
         InlineData(NetworkNumberWithIdentificationCode8, "+ccc isss ssssssss", "+881 8123 5550103"),
         InlineData(NetworkNumberWithIdentificationCode8, "+ ccc isss ssssssss", "+ 881 8123 5550103"),
         InlineData(NetworkNumberWithIdentificationCode9, CountryThenIdentificationThenGroupedSubscriberPattern, "+881 9 123 5550104"),
         InlineData(NetworkNumberWithIdentificationCode9, "+ ccc i sss sssssss", "+ 881 9 123 5550104"),
         InlineData(NetworkNumberOfCountryCode882, "+ccc iiii sss sss ssss", "+882 10 123 555 0105"),
         InlineData(NetworkNumberOfCountryCode882, "+ ccc iiii sss sss ssss", "+ 882 10 123 555 0105")]
        public void Format_InternationalNumberForNetworkWithCustomFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForNetwork(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(38835550100L, GeneralFormat, "+38835550100"),
         InlineData(38835550101L, GeneralFormatWithSeparators, "+388 3 5550101")]
        public void Format_InternationalNumberForGroupOfCountriesWithGeneralFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(GroupOfCountriesNumber0102, CountryIdentificationAndSubscriberPattern, "+38835550102"),
         InlineData(GroupOfCountriesNumber0102, SpacedCountryIdentificationAndSubscriberPattern, "+ 38835550102")]
        public void Format_InternationalNumberForGroupOfCountriesWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(ShortInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(GroupOfCountriesNumber0103, CountryThenIdentificationAndSubscriberPattern, "+388 35550103"),
         InlineData(GroupOfCountriesNumber0103, SpacedCountryThenIdentificationAndSubscriberPattern, "+ 388 35550103"),
         InlineData(GroupOfCountriesNumber0104, CountryThenIdentificationThenSubscriberPattern, "+388 3 5550104"),
         InlineData(GroupOfCountriesNumber0104, SpacedCountryThenIdentificationThenSubscriberPattern, "+ 388 3 5550104")]
        public void Format_InternationalNumberForGroupOfCountriesWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(LongInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(GroupOfCountriesNumber0105, CountryIdentificationAndSubscriberPattern, "+38835550105"),
         InlineData(GroupOfCountriesNumber0105, SpacedCountryIdentificationAndSubscriberPattern, "+ 38835550105"),
         InlineData(GroupOfCountriesNumber0106, CountryThenIdentificationAndSubscriberPattern, "+388 35550106"),
         InlineData(GroupOfCountriesNumber0106, SpacedCountryThenIdentificationAndSubscriberPattern, "+ 388 35550106"),
         InlineData(GroupOfCountriesNumber0107, CountryThenIdentificationThenSubscriberPattern, "+388 3 5550107"),
         InlineData(GroupOfCountriesNumber0107, SpacedCountryThenIdentificationThenSubscriberPattern, "+ 388 3 5550107"),
         InlineData(GroupOfCountriesNumber0108, CountryThenIdentificationThenGroupedSubscriberPattern, "+388 3 555 0108"),
         InlineData(GroupOfCountriesNumber0108, SpacedCountryThenIdentificationThenGroupedSubscriberPattern, "+ 388 3 555 0108")]
        public void Format_InternationalNumberForGroupOfCountriesWithCustomFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(TrialNumberWithoutSubscriber, GeneralFormat, TrialNumberWithoutSubscriberFormatted),
         InlineData(99115550100L, GeneralFormat, "+99115550100"),
         InlineData(TrialNumberWithoutSubscriber, GeneralFormatWithSeparators, TrialNumberWithoutSubscriberWithSeparator),
         InlineData(99115550101L, GeneralFormatWithSeparators, "+991 1 5550101")]
        public void Format_InternationalNumberForTrialWithGeneralFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForTrial(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(TrialNumberWithoutSubscriber, CountryIdentificationAndSubscriberPattern, TrialNumberWithoutSubscriberFormatted),
         InlineData(TrialNumberWithoutSubscriber, SpacedCountryIdentificationAndSubscriberPattern, TrialNumberWithoutSubscriberSpaced),
         InlineData(TrialNumber0102, CountryIdentificationAndSubscriberPattern, "+99115550102"),
         InlineData(TrialNumber0102, SpacedCountryIdentificationAndSubscriberPattern, "+ 99115550102")]
        public void Format_InternationalNumberForTrialWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForTrial(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(ShortInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(TrialNumberWithoutSubscriber, CountryThenIdentificationAndSubscriberPattern, TrialNumberWithoutSubscriberWithSeparator),
         InlineData(TrialNumberWithoutSubscriber, SpacedCountryThenIdentificationAndSubscriberPattern, "+ 991 1"),
         InlineData(TrialNumber0103, CountryThenIdentificationAndSubscriberPattern, "+991 15550103"),
         InlineData(TrialNumber0103, SpacedCountryThenIdentificationAndSubscriberPattern, "+ 991 15550103")]
        public void Format_InternationalNumberForTrialWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForTrial(testValue);
            var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
            var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(LongInternationalFormat, testNumber, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Theory]
        [InlineData(TrialNumberWithoutSubscriber, CountryIdentificationAndSubscriberPattern, TrialNumberWithoutSubscriberFormatted),
         InlineData(TrialNumberWithoutSubscriber, SpacedCountryIdentificationAndSubscriberPattern, TrialNumberWithoutSubscriberSpaced),
         InlineData(TrialNumber0104, CountryThenIdentificationAndSubscriberPattern, "+991 15550104"),
         InlineData(TrialNumber0104, SpacedCountryThenIdentificationAndSubscriberPattern, "+ 991 15550104"),
         InlineData(TrialNumber0105, CountryThenIdentificationThenSubscriberPattern, "+991 1 5550105"),
         InlineData(TrialNumber0105, SpacedCountryThenIdentificationThenSubscriberPattern, "+ 991 1 5550105"),
         InlineData(TrialNumber0106, CountryThenIdentificationThenGroupedSubscriberPattern, "+991 1 555 0106"),
         InlineData(TrialNumber0106, SpacedCountryThenIdentificationThenGroupedSubscriberPattern, "+ 991 1 555 0106")]
        public void Format_InternationalNumberForTrialWithCustomFormat_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
        {
            // arrange
            var testNumber = ItuE164InternationalNumber.CreateNumberForTrial(testValue);

            var testSubject = new ItuE164InternationalNumberFormatter();

            // act
            var testResult = testSubject.Format(testFormat, testNumber, null);

            // assert
            testResult.Should().Be(expectedResult);
        }
    }
}
