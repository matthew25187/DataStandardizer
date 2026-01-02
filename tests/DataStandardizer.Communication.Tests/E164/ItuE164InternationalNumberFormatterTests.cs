using DataStandardizer.Communication.E164;
using FluentAssertions;

namespace DataStandardizer.Communication.Tests.E164;

public class ItuE164InternationalNumberFormatterTests
{
    [Theory]
    [InlineData(12135550123L, "g", "+12135550123"),
     InlineData(12135550123L, "G", "+1 2135550123")]
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
    [InlineData(12505550199L, "+cssssssssssss", "+12505550199")]
    public void Format_InternationalNumberForGeographicAreaWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("i", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(12505550199L, "+c ssssssssssss", "+1 2505550199"),
     InlineData(12505550199L, "+c sss sssssssss", "+1 250 5550199"),
     InlineData(12505550199L, "+c sss sss ssss", "+1 250 555 0199")]
    public void Format_InternationalNumberForGeographicAreaWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("I", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(12135550123L, "cssssssssssss", "12135550123"),
     InlineData(12135550123L, "+cssssssssssss", "+12135550123"),
     InlineData(12135550123L, "+c ssssssssssss", "+1 2135550123"),
     InlineData(12135550123L, "+c sss sssssssss", "+1 213 5550123"),
     InlineData(12135550123L, "+c sss sss ssssss", "+1 213 555 0123")]
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
    [InlineData(800282828L, "g", "+800282828"),
     InlineData(800457457L, "G", "+800 457457")]
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
    [InlineData(800282828L, "+cccssssssssssss", "+800282828"),
     InlineData(800282828L, "+ cccssssssssssss", "+ 800282828")]
    public void Format_InternationalNumberForGlobalServiceWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForGlobalService(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("i", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(800457457L, "+ccc ssssssssssss", "+800 457457"),
     InlineData(800457457L, "+ ccc ssssssssssss", "+ 800 457457"),
     InlineData(800457457L, "+ccc sss sss", "+800 457 457"),
     InlineData(800457457L, "+ ccc sss sss", "+ 800 457 457")]
    public void Format_InternationalNumberForGlobalServiceWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForGlobalService(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("I", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(800282828L, "+ccc ss ss ss", "+800 28 28 28"),
     InlineData(800282828L, "+ ccc ss ss ss", "+ 800 28 28 28"),
     InlineData(800457457L, "+ccc sss sss", "+800 457 457"),
     InlineData(800457457L, "+ ccc sss sss", "+ 800 457 457")]
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
    [InlineData(88101235550199L, "g", "+88101235550199"),
     InlineData(88111235550199L, "G", "+881 11235550199")]
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
    [InlineData(88321035550199L, "+ccciiiissssssss", "+88321035550199"),
     InlineData(88321035550199L, "+ ccciiiissssssss", "+ 88321035550199")]
    public void Format_InternationalNumberForNetworkWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForNetwork(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("i", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(88161235550100L, "+ccc isssssssssss", "+881 61235550100"),
     InlineData(88161235550100L, "+ ccc isssssssssss", "+ 881 61235550100"),
     InlineData(88161235550101L, "+ccc i sssssssssss", "+881 6 1235550101"),
     InlineData(88161235550101L, "+ ccc i sssssssssss", "+ 881 6 1235550101")]
    public void Format_InternationalNumberForNetworkWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForNetwork(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("I", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(88161235550101L, "+cccisssssssssss", "+88161235550101"),
     InlineData(88161235550101L, "+ cccisssssssssss", "+ 88161235550101"),
     InlineData(88171235550102L, "+ccc isssssssssss", "+881 71235550102"),
     InlineData(88171235550102L, "+ ccc isssssssssss", "+ 881 71235550102"),
     InlineData(88181235550103L, "+ccc isss ssssssss", "+881 8123 5550103"),
     InlineData(88181235550103L, "+ ccc isss ssssssss", "+ 881 8123 5550103"),
     InlineData(88191235550104L, "+ccc i sss ssssssss", "+881 9 123 5550104"),
     InlineData(88191235550104L, "+ ccc i sss sssssss", "+ 881 9 123 5550104"),
     InlineData(882101235550105L, "+ccc iiii sss sss ssss", "+882 10 123 555 0105"),
     InlineData(882101235550105L, "+ ccc iiii sss sss ssss", "+ 882 10 123 555 0105")]
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
    [InlineData(38835550100L, "g", "+38835550100"),
     InlineData(38835550101L, "G", "+388 3 5550101")]
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
    [InlineData(38835550102L, "+cccisssssssssss", "+38835550102"),
     InlineData(38835550102L, "+ cccisssssssssss", "+ 38835550102")]
    public void Format_InternationalNumberForGroupOfCountriesWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("i", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(38835550103L, "+ccc isssssssssss", "+388 35550103"),
     InlineData(38835550103L, "+ ccc isssssssssss", "+ 388 35550103"),
     InlineData(38835550104L, "+ccc i sssssssssss", "+388 3 5550104"),
     InlineData(38835550104L, "+ ccc i sssssssssss", "+ 388 3 5550104")]
    public void Format_InternationalNumberForGroupOfCountriesWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForGroupOfCountries(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("I", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(38835550105L, "+cccisssssssssss", "+38835550105"),
     InlineData(38835550105L, "+ cccisssssssssss", "+ 38835550105"),
     InlineData(38835550106L, "+ccc isssssssssss", "+388 35550106"),
     InlineData(38835550106L, "+ ccc isssssssssss", "+ 388 35550106"),
     InlineData(38835550107L, "+ccc i sssssssssss", "+388 3 5550107"),
     InlineData(38835550107L, "+ ccc i sssssssssss", "+ 388 3 5550107"),
     InlineData(38835550108L, "+ccc i sss ssssssss", "+388 3 555 0108"),
     InlineData(38835550108L, "+ ccc i sss ssssssss", "+ 388 3 555 0108")]
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
    [InlineData(9911L, "g", "+9911"),
     InlineData(99115550100L, "g", "+99115550100"),
     InlineData(9911L, "G", "+991 1"),
     InlineData(99115550101L, "G", "+991 1 5550101")]
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
    [InlineData(9911L, "+cccisssssssssss", "+9911"),
     InlineData(9911L, "+ cccisssssssssss", "+ 9911"),
     InlineData(99115550102L, "+cccisssssssssss", "+99115550102"),
     InlineData(99115550102L, "+ cccisssssssssss", "+ 99115550102")]
    public void Format_InternationalNumberForTrialWithShortInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForTrial(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { ShortInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("i", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(9911L, "+ccc isssssssssss", "+991 1"),
     InlineData(9911L, "+ ccc isssssssssss", "+ 991 1"),
     InlineData(99115550103L, "+ccc isssssssssss", "+991 15550103"),
     InlineData(99115550103L, "+ ccc isssssssssss", "+ 991 15550103")]
    public void Format_InternationalNumberForTrialWithLongInternationalFormat_ReturnsFormattedOutput(ulong testValue, string internationalFormat, string expectedResult)
    {
        // arrange
        var testNumber = ItuE164InternationalNumber.CreateNumberForTrial(testValue);
        var formatInfo = new ItuE164InternationalNumberFormatInfo { LongInternationalNumberPattern = internationalFormat };
        var formatProvider = new TelephonyInfo { ItuE164InternationalNumberFormat = formatInfo };

        var testSubject = new ItuE164InternationalNumberFormatter();

        // act
        var testResult = testSubject.Format("I", testNumber, formatProvider);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(9911L, "+cccisssssssssss", "+9911"),
     InlineData(9911L, "+ cccisssssssssss", "+ 9911"),
     InlineData(99115550104L, "+ccc isssssssssss", "+991 15550104"),
     InlineData(99115550104L, "+ ccc isssssssssss", "+ 991 15550104"),
     InlineData(99115550105L, "+ccc i sssssssssss", "+991 1 5550105"),
     InlineData(99115550105L, "+ ccc i sssssssssss", "+ 991 1 5550105"),
     InlineData(99115550106L, "+ccc i sss ssssssss", "+991 1 555 0106"),
     InlineData(99115550106L, "+ ccc i sss ssssssss", "+ 991 1 555 0106")]
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