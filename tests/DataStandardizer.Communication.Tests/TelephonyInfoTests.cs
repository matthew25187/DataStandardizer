using DataStandardizer.Communication.E164;
using DataStandardizer.Geography;
using FluentAssertions;

namespace DataStandardizer.Communication.Tests;

public class TelephonyInfoTests
{
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
    [InlineData(12135550123L, "g", "+12135550123"),
     InlineData(12135550100L, "G", "+1 2135550100"),
     InlineData(12135550199L, "+cccsssssssssss", "+12135550199"),
     InlineData(12135550199L, "+ cccssssssssssss", "+ 12135550199"),
     InlineData(12135550199L, "+ccc ssssssssssss", "+1 2135550199"),
     InlineData(12135550199L, "+ ccc ssssssssssss", "+ 1 2135550199"),
     InlineData(12135550199L, "+ccc sss sssssssss", "+1 213 5550199"),
     InlineData(12135550199L, "+ ccc sss sssssssss", "+ 1 213 5550199")]
    public void InvariantTelephony_PassedToFormatCapableMethod_ReturnsFormattedOutput(ulong testValue, string testFormat, string expectedResult)
    {
        // arrange
        var useFormat = string.Concat("{0:", testFormat, "}");
        var testSubject = ItuE164InternationalNumber.CreateNumberForGeographicArea(testValue);

        // act
        var testResult = string.Format(TelephonyInfo.InvariantTelephony, useFormat, testSubject);

        // assert
        testResult.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData(12135550150L, "+cccssssssssssss", "+12135550150"),
     InlineData(12135550150L, "+ cccssssssssssss", "+ 12135550150")]
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
    [InlineData(12135550190L, "+ccc ssssssssss", "+1 2135550190"),
     InlineData(12135550190L, "+ ccc ssssssssssss", "+ 1 2135550190"),
     InlineData(12135550190L, "+ccc sss sssssssss", "+1 213 5550190"),
     InlineData(12135550190L, "+ ccc sss sssssssss", "+ 1 213 5550190")]
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
            .WithMessage($"{nameof(TelephonyInfo)} is read only.");
    }
}