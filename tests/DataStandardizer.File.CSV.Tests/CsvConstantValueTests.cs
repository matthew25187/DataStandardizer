using System;
using FluentAssertions;
using Xunit;

namespace DataStandardizer.File.CSV.Tests
{
    /// <summary>
    /// Tests that <see cref="CsvConstantValue"/> converts a decimal to the nearest double or float, and a tie to the one with the even significand,
    /// as IEEE 754 round-to-nearest-even does, the same on every target.
    /// </summary>
    /// <remarks>
    /// A reader converts a double or float constant only by way of a decimal of at most 15 significant digits, so the ties, the wide decimals and
    /// the negative values below can't be given to it as constants.  The decimal inputs are delegates, so that xUnit doesn't serialize the rows.
    /// </remarks>
    public class CsvConstantValueTests
    {
        // 2^53 + 1 and 2^53 + 3, each halfway between two doubles, which round to the one with the even significand: 2^53 and 2^53 + 4.
        private const decimal TwoToThe53PlusOne = 9007199254740993m;
        private const double TwoToThe53 = 9007199254740992.0;
        private const decimal TwoToThe53PlusThree = 9007199254740995m;
        private const double TwoToThe53PlusFour = 9007199254740996.0;

        // 2^53 - 0.5, halfway between 2^53 - 1, whose significand is odd, and 2^53, so it rounds up to the next power of two.
        private const decimal TwoToThe53MinusHalf = 9007199254740991.5m;

        // decimal.MaxValue, 2^96 - 1, whose 96-bit numerator is wider than a double's significand, rounds up to 2^96.
        private const decimal TwoToThe96MinusOne = 79228162514264337593543950335m;
        private const double TwoToThe96 = 79228162514264337593543950336.0;

        // A decimal of 28 significant digits and its nearest double, which a cast of the decimal to double misses by one ULP.
        private const decimal TwentyEightDigitDecimal = 0.1234567890123456789012345678m;
        private const double TwentyEightDigitDecimalNearestDouble = 0.12345678901234568;

        // A decimal with a scale above 22, past the largest power of ten a double holds exactly (10^22).
        private const decimal TinyDecimal = 0.00000000000000000000001m;
        private const double TinyDouble = 1e-23;
        private const float TinyFloat = 1e-23f;

        // 2^24 + 1 and 2^24 + 3, each halfway between two floats, which round to the one with the even significand: 2^24 and 2^24 + 4.
        private const decimal TwoToThe24PlusOne = 16777217m;
        private const float TwoToThe24 = 16777216f;
        private const decimal TwoToThe24PlusThree = 16777219m;
        private const float TwoToThe24PlusFour = 16777220f;

        // 2^24 - 0.5, halfway between 2^24 - 1, whose significand is odd, and 2^24, so it rounds up to the next power of two.
        private const decimal TwoToThe24MinusHalf = 16777215.5m;

        // Decimals, each with the double nearest to it.
        public static TheoryData<Func<decimal>, double> DecimalsAndNearestDoubles => new TheoryData<Func<decimal>, double>
        {
            { () => TwoToThe53PlusOne, TwoToThe53 },
            { () => TwoToThe53PlusThree, TwoToThe53PlusFour },
            { () => TwoToThe53MinusHalf, TwoToThe53 },
            { () => TwoToThe96MinusOne, TwoToThe96 },
            { () => TwentyEightDigitDecimal, TwentyEightDigitDecimalNearestDouble },
            { () => TinyDecimal, TinyDouble },
            { () => -TinyDecimal, -TinyDouble },
            { () => 0m, 0d }
        };

        [Theory]
        [MemberData(nameof(DecimalsAndNearestDoubles))]
        public void ToDouble_Decimal_ReturnsNearestDoubleWithTiesToEven(Func<decimal> getValue, double expectedValue)
        {
            // arrange
            var value = getValue();

            // act
            var testResult = CsvConstantValue.ToDouble(value);

            // assert
            testResult.Should().Be(expectedValue);
        }

        // Decimals, each with the float nearest to it.
        public static TheoryData<Func<decimal>, float> DecimalsAndNearestFloats => new TheoryData<Func<decimal>, float>
        {
            { () => TwoToThe24PlusOne, TwoToThe24 },
            { () => TwoToThe24PlusThree, TwoToThe24PlusFour },
            { () => TwoToThe24MinusHalf, TwoToThe24 },
            { () => TinyDecimal, TinyFloat },
            { () => -TinyDecimal, -TinyFloat }
        };

        [Theory]
        [MemberData(nameof(DecimalsAndNearestFloats))]
        public void ToSingle_Decimal_ReturnsNearestFloatWithTiesToEven(Func<decimal> getValue, float expectedValue)
        {
            // arrange
            var value = getValue();

            // act
            var testResult = CsvConstantValue.ToSingle(value);

            // assert
            testResult.Should().Be(expectedValue);
        }
    }
}
