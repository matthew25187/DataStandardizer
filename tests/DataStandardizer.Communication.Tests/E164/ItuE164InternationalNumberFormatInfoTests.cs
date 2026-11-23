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

        // The package's neutral patterns, from its Resources.resx.
        private const string InvariantLongPattern = "+ccc iiiissssssssssssss";
        private const string InvariantShortPattern = "+ccciiiissssssssssssss";

        // An ITU-T E.164 international number for geographic areas, under country code 44, and that number in the
        // invariant long and short international formats.
        private const ulong GreatBritainNumber = 443071234567L;
        private const string GreatBritainNumberInInvariantLongFormat = "+44 3071234567";
        private const string GreatBritainNumberInInvariantShortFormat = "+443071234567";

        private const string LongInternationalFormat = "I";
        private const string ShortInternationalFormat = "i";

        [Fact]
        public void LongInternationalNumberPattern_NewInstance_ReturnsInvariantPattern()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo();

            // act
            var testResult = testSubject.LongInternationalNumberPattern;

            // assert
            testResult.Should().Be(InvariantLongPattern);
        }

        [Fact]
        public void ShortInternationalNumberPattern_NewInstance_ReturnsInvariantPattern()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo();

            // act
            var testResult = testSubject.ShortInternationalNumberPattern;

            // assert
            testResult.Should().Be(InvariantShortPattern);
        }

        [Theory]
        [InlineData(LongInternationalFormat, GreatBritainNumberInInvariantLongFormat),
         InlineData(ShortInternationalFormat, GreatBritainNumberInInvariantShortFormat)]
        public void ToString_NewInstanceAsFormatProvider_ReturnsNumberInInvariantFormat(string format, string expectedResult)
        {
            // arrange
            var formatProvider = new ItuE164InternationalNumberFormatInfo();
            var number = ItuE164InternationalNumber.CreateNumberForGeographicArea(GreatBritainNumber);

            // act
            var testResult = number.ToString(format, formatProvider);

            // assert
            testResult.Should().Be(expectedResult);
        }

        [Fact]
        public void LongInternationalNumberPattern_SetToNull_ThrowsArgumentNullException()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo();

            // act
            Action testAction = () => testSubject.LongInternationalNumberPattern = null;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void LongInternationalNumberPattern_ReadOnlySetToNull_ThrowsInvalidOperationException()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = true };

            // act
            Action testAction = () => testSubject.LongInternationalNumberPattern = null;

            // assert
            testAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(ReadOnlyMessage);
        }

        [Fact]
        public void ShortInternationalNumberPattern_ReadOnlySetToNull_ThrowsInvalidOperationException()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo { IsReadOnly = true };

            // act
            Action testAction = () => testSubject.ShortInternationalNumberPattern = null;

            // assert
            testAction.Should()
                .Throw<InvalidOperationException>()
                .WithMessage(ReadOnlyMessage);
        }

        [Fact]
        public void ShortInternationalNumberPattern_SetToNull_ThrowsArgumentNullException()
        {
            // arrange
            var testSubject = new ItuE164InternationalNumberFormatInfo();

            // act
            Action testAction = () => testSubject.ShortInternationalNumberPattern = null;

            // assert
            testAction.Should().Throw<ArgumentNullException>();
        }

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
