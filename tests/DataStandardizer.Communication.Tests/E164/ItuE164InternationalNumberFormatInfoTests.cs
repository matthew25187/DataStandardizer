using DataStandardizer.Communication.E164;
using FluentAssertions;

namespace DataStandardizer.Communication.Tests.E164;

public class ItuE164InternationalNumberFormatInfoTests
{
    [Fact]
    public void GetFormat_ForItuE164InternationalNumberFormatInfo_ReturnsInternationalNumberFormatInfo()
    {
        // arrange
        var testSubject = new ItuE164InternationalNumberFormatInfo();

        // act
        var testResult = testSubject.GetFormat(typeof(ItuE164InternationalNumberFormatInfo));

        // assert
        testResult.Should().BeSameAs(testSubject);
    }

    [Fact]
    public void LongInternationalNumberPattern_FormatInfoIsNotReadOnly_SuccessfullySet()
    {
        // arrange
        var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = false };

        const string testValue = "++#-###-#######";

        // act
        testSubject.LongInternationalNumberPattern = testValue;

        // assert
        testSubject.LongInternationalNumberPattern.Should().Be(testValue);
    }

    [Fact]
    public void LongInternationalNumberPattern_FormatInfoIsReadOnly_ThrowsInvalidOperationExceptionOnSet()
    {
        // arrange
        var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = true };

        // act
        Action testAction = () => testSubject.LongInternationalNumberPattern = "++#-###-#######";

        // assert
        testAction.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"{nameof(ItuE164InternationalNumberFormatInfo)} is read only.");
    }

    [Fact]
    public void ShortInternationalNumberPattern_FormatInfoIsNotReadOnly_SuccessfullySet()
    {
        // arrange
        var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = false };

        const string testValue = "pcssssssssss";

        // act
        testSubject.ShortInternationalNumberPattern = testValue;

        // assert
        testSubject.ShortInternationalNumberPattern.Should().Be(testValue);
    }

    [Fact]
    public void ShortInternationalNumberPattern_FormatInfoIsReadOnly_ThrowsInvalidOperationException()
    {
        // arrange
        var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = true };

        // act
        Action testAction = () => testSubject.ShortInternationalNumberPattern = "cc xxxnnnnnnnnnn";

        // assert
        testAction.Should()
            .Throw<InvalidOperationException>()
            .WithMessage($"{nameof(ItuE164InternationalNumberFormatInfo)} is read only.");
    }
}