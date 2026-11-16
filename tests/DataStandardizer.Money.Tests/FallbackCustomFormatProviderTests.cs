using FluentAssertions;

namespace DataStandardizer.Money.Tests;

public class FallbackCustomFormatProviderTests
{
    private const string FixedFormat = "0.0";
    private const decimal TestAmount = 1.5m;

    [Fact]
    public void Format_FormattableArgument_ReturnsItsFormattedValue()
    {
        // arrange
        var testSubject = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResult = testSubject.Format(FixedFormat, TestAmount, CurrencyFormatInfo.InvariantInfo);

        // assert
        testResult.Should().Be("1.5");
    }

    [Fact]
    public void Format_ArgumentWhichCallsBackIntoTheFormatter_ReturnsReentryResult()
    {
        // arrange
        var testSubject = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResult = testSubject.Format(FixedFormat, new ReentrantFormattable(), testSubject);

        // assert
        testResult.Should().Be(FallbackCustomFormatProvider.ReentryResult);
    }

    [Fact]
    public void Format_AfterAnEarlierCall_FormatsAgain()
    {
        // arrange
        var testSubject = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResults = new[]
        {
            testSubject.Format(FixedFormat, TestAmount, CurrencyFormatInfo.InvariantInfo),
            testSubject.Format(FixedFormat, TestAmount, CurrencyFormatInfo.InvariantInfo)
        };

        // assert
        testResults.Should().Equal("1.5", "1.5");
    }

    [Fact]
    public void GetFormat_CurrencyFormatInfo_ReturnsTheCurrencyFormatInfoGiven()
    {
        // arrange
        var currencyFormat = new CurrencyFormatInfo();
        var testSubject = new FallbackCustomFormatProvider(currencyFormat);

        // act
        var testResult = testSubject.GetFormat(typeof(CurrencyFormatInfo));

        // assert
        testResult.Should().BeSameAs(currencyFormat);
    }

    [Fact]
    public void GetFormat_CustomFormatter_ReturnsTheProvider()
    {
        // arrange
        var testSubject = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResult = testSubject.GetFormat(typeof(ICustomFormatter));

        // assert
        testResult.Should().BeSameAs(testSubject);
    }

    [Fact]
    public void GetFormat_OtherType_ReturnsNull()
    {
        // arrange
        var testSubject = new FallbackCustomFormatProvider(CurrencyFormatInfo.InvariantInfo);

        // act
        var testResult = testSubject.GetFormat(typeof(System.Globalization.NumberFormatInfo));

        // assert
        testResult.Should().BeNull();
    }

    /// <summary>
    /// A value which formats itself by calling the provider's custom formatter, as <see cref="Money"/> did.
    /// </summary>
    private sealed class ReentrantFormattable : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) =>
            ((ICustomFormatter)formatProvider!.GetFormat(typeof(ICustomFormatter))!).Format(format, this, formatProvider);
    }
}
