using System;
using DataStandardizer.Communication.E164;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.Communication.Tests.E164
{
    public class ItuE164InternationalNumberFormatInfoTests
    {
        private const string ReadOnlyMessage = "ItuE164InternationalNumberFormatInfo is read only.";
        private const string LongPattern = "++#-###-#######";

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

            // act
            testSubject.LongInternationalNumberPattern = LongPattern;

            // assert
            testSubject.LongInternationalNumberPattern.Should().Be(LongPattern);
        }

        [Fact]
        public void LongInternationalNumberPattern_FormatInfoIsReadOnly_ThrowsInvalidOperationExceptionOnSet()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = true };

            // act
            Action testAction = () => testSubject.LongInternationalNumberPattern = LongPattern;

            // assert
            testAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(ReadOnlyMessage);
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
        public void ShortInternationalNumberPattern_FormatInfoIsReadOnly_ThrowsInvalidOperationExceptionOnSet()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = true };

            // act
            Action testAction = () => testSubject.ShortInternationalNumberPattern = "cc xxxnnnnnnnnnn";

            // assert
            testAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(ReadOnlyMessage);
        }
    }
}
