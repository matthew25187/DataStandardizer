using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
#if !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics;
using System.Numerics;
using System.Text;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Formats a field value that a writer has no converter for, so that it reads back as the same value and is the same text on every target.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>A <see cref="DateTime"/> or <see cref="DateTimeOffset"/>, and on .NET 6 and later a <c>DateOnly</c> or <c>TimeOnly</c>, is
    /// written with the round-trip pattern (<c>"o"</c>), which keeps every tick, the <see cref="DateTime.Kind"/> and the offset.</description></item>
    /// <item><description>A <see cref="double"/> or <see cref="float"/> is written with the fewest significant digits that read back as the same value,
    /// laid out as <c>"R"</c> lays it out on .NET Core 3.0 and later.  .NET Framework's <c>"R"</c> gives other digits, some of which don't read back as
    /// the same value, so the targets that may run on it generate the digits themselves.</description></item>
    /// <item><description>Any other <see cref="IFormattable"/> value is formatted with its general format, and any other value with
    /// <see cref="object.ToString"/>.</description></item>
    /// </list>
    /// </remarks>
    internal static class CsvFieldFormatter
    {
        private const string RoundTripFormat = "o";

        /// <summary>
        /// Formats a field value with a culture's number symbols.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static string? Format(object value, CultureInfo culture)
#else
        [return: MaybeNull]
        internal static string Format([DisallowNull] object value, [DisallowNull] CultureInfo culture)
#endif
        {
            switch (value)
            {
                case DateTime dateTimeValue:
                    return dateTimeValue.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
                case DateTimeOffset dateTimeOffsetValue:
                    return dateTimeOffsetValue.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
#if NET6_0_OR_GREATER
                case DateOnly dateOnlyValue:
                    return dateOnlyValue.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
                case TimeOnly timeOnlyValue:
                    return timeOnlyValue.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
#endif
                case double doubleValue:
                    return FormatDouble(doubleValue, culture.NumberFormat);
                case float floatValue:
                    return FormatSingle(floatValue, culture.NumberFormat);
                case IFormattable formattableValue:
                    return formattableValue.ToString(null, culture);
                default:
                    return value.ToString();
            }
        }

        /// <summary>
        /// Formats a <see cref="double"/> with the fewest significant digits that read back as the same value.
        /// </summary>
        internal static string FormatDouble(double value, NumberFormatInfo numberFormat)
        {
#if NETCOREAPP3_0_OR_GREATER
            return value.ToString("R", numberFormat);
#else
            if (IsRuntimeRoundTripFormatShortest) return value.ToString("R", numberFormat);
            if (double.IsNaN(value)) return numberFormat.NaNSymbol;
            if (double.IsPositiveInfinity(value)) return numberFormat.PositiveInfinitySymbol;
            if (double.IsNegativeInfinity(value)) return numberFormat.NegativeInfinitySymbol;

            var bits = BitConverter.DoubleToInt64Bits(value);
            return FormatShortest(bits < 0, (ulong)bits & DoubleFractionMask, (int)((bits >> DoubleFractionBits) & DoubleExponentMask), DoubleFractionBits,
                DoubleExponentBias, DoubleMaxDigits, numberFormat);
#endif
        }

        /// <summary>
        /// Formats a <see cref="float"/> with the fewest significant digits that read back as the same value.
        /// </summary>
        internal static string FormatSingle(float value, NumberFormatInfo numberFormat)
        {
#if NETCOREAPP3_0_OR_GREATER
            return value.ToString("R", numberFormat);
#else
            if (IsRuntimeRoundTripFormatShortest) return value.ToString("R", numberFormat);
            if (float.IsNaN(value)) return numberFormat.NaNSymbol;
            if (float.IsPositiveInfinity(value)) return numberFormat.PositiveInfinitySymbol;
            if (float.IsNegativeInfinity(value)) return numberFormat.NegativeInfinitySymbol;

            var bits = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            return FormatShortest(bits < 0, (ulong)(bits & SingleFractionMask), (bits >> SingleFractionBits) & SingleExponentMask, SingleFractionBits,
                SingleExponentBias, SingleMaxDigits, numberFormat);
#endif
        }

#if !NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Whether the runtime's <c>"R"</c> writes the digits and layout <see cref="FormatShortest"/> does, as it does on .NET Core 3.0 and later,
        /// so that a .NET Standard build running there can use it, which is many times faster.  It is checked with values that .NET Framework's
        /// <c>"R"</c> writes otherwise: with too few digits to read back, with too many, or in scientific notation.
        /// </summary>
        private static readonly bool IsRuntimeRoundTripFormatShortest =
            string.Equals(901.7426589977661.ToString("R", CultureInfo.InvariantCulture), "901.7426589977661", StringComparison.Ordinal)
            && string.Equals(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), "5E-324", StringComparison.Ordinal)
            && string.Equals(1E+16.ToString("R", CultureInfo.InvariantCulture), "10000000000000000", StringComparison.Ordinal)
            && string.Equals(0.1f.ToString("R", CultureInfo.InvariantCulture), "0.1", StringComparison.Ordinal)
            && string.Equals(0.90000004f.ToString("R", CultureInfo.InvariantCulture), "0.90000004", StringComparison.Ordinal)
            && string.Equals(1E+8f.ToString("R", CultureInfo.InvariantCulture), "100000000", StringComparison.Ordinal);

        // The IEEE 754 binary64 and binary32 layouts, which CsvFloatingPointParser also uses, and the most significant digits "R" writes before it
        // switches to scientific notation.
        internal const int DoubleFractionBits = 52;
        private const ulong DoubleFractionMask = (1UL << DoubleFractionBits) - 1;
        internal const int DoubleExponentMask = 0x7FF;
        internal const int DoubleExponentBias = 1023;
        private const int DoubleMaxDigits = 17;
        internal const int SingleFractionBits = 23;
        private const int SingleFractionMask = (1 << SingleFractionBits) - 1;
        internal const int SingleExponentMask = 0xFF;
        internal const int SingleExponentBias = 127;
        private const int SingleMaxDigits = 9;

        // "R" writes a value in scientific notation if its decimal point would be more than this many places to the left of its first digit.
        private const int MinFixedPointDecimalExponent = -3;

        // Taken off the logarithm of a value so that rounding error can't make the estimate of its decimal exponent too high.
        private const double LogarithmTolerance = 1e-10;

        /// <summary>
        /// Formats a finite binary floating-point value with the fewest significant digits that read back as the same value.
        /// </summary>
        /// <remarks>
        /// The digits are generated with the free-format algorithm of Steele and White, as refined by Burger and Dybvig ("Printing Floating-Point
        /// Numbers Quickly and Accurately", 1996), in exact integer arithmetic.  Of the shortest digit strings that read back as the value, the one
        /// closest to it is chosen, and a tie goes to the even last digit.  A digit string reads back as the value when it lies inside the value's
        /// rounding interval, whose bounds are included when the value's significand is even, as round-half-to-even reading includes them.  These
        /// are the digits .NET Core 3.0 and later give for <c>"R"</c>.
        /// </remarks>
        private static string FormatShortest(bool isNegative, ulong fraction, int biasedExponent, int fractionBits, int exponentBias, int maxDigits,
            NumberFormatInfo numberFormat)
        {
            if (fraction == 0 && biasedExponent == 0)
            {
                return isNegative ? numberFormat.NegativeSign + "0" : "0";
            }

            // The value is significand * 2^exponent.  A subnormal value has no implicit leading bit and the exponent of the smallest normal value.
            ulong significand;
            int exponent;
            if (biasedExponent == 0)
            {
                significand = fraction;
                exponent = 1 - exponentBias - fractionBits;
            }
            else
            {
                significand = fraction | (1UL << fractionBits);
                exponent = biasedExponent - exponentBias - fractionBits;
            }

            // The value lies halfway between its neighbours, except at a power of two above the smallest normal value, where the neighbour below is
            // half as far away as the one above.  Everything is scaled by 2, or by 4 for unequal gaps, so that the gaps are whole numbers.  The value
            // is remainder / scale, and the gaps to the bounds of its rounding interval are marginLow / scale and marginHigh / scale.
            var hasUnequalGaps = fraction == 0 && biasedExponent > 1;
            BigInteger remainder;
            BigInteger scale;
            BigInteger marginLow;
            BigInteger marginHigh;
            if (exponent >= 0)
            {
                var gap = BigInteger.Pow(2, exponent);
                remainder = significand * gap * (hasUnequalGaps ? 4 : 2);
                scale = hasUnequalGaps ? 4 : 2;
                marginLow = gap;
                marginHigh = hasUnequalGaps ? gap * 2 : gap;
            }
            else
            {
                remainder = new BigInteger(significand) * (hasUnequalGaps ? 4 : 2);
                scale = BigInteger.Pow(2, -exponent + (hasUnequalGaps ? 2 : 1));
                marginLow = BigInteger.One;
                marginHigh = hasUnequalGaps ? 2 : BigInteger.One;
            }

            // Scale by a power of 10 so that the value is below 1 and its first digit is the first after the decimal point.  The estimate from the
            // logarithm is never more than one too low, and is corrected below.
            var isSignificandEven = (significand & 1) == 0;
            var decimalExponent = (int)Math.Ceiling(Math.Log10(significand * Math.Pow(2, exponent)) - LogarithmTolerance);
            if (decimalExponent >= 0)
            {
                scale *= BigInteger.Pow(10, decimalExponent);
            }
            else
            {
                var factor = BigInteger.Pow(10, -decimalExponent);
                remainder *= factor;
                marginLow *= factor;
                marginHigh *= factor;
            }

            while (IsAboveHighBound(remainder + marginHigh, scale, isSignificandEven))
            {
                scale *= 10;
                decimalExponent++;
            }

            // Generate digits until the digits so far, rounded down or up, lie inside the rounding interval.
            var digits = new StringBuilder(maxDigits);
            while (true)
            {
                remainder *= 10;
                marginLow *= 10;
                marginHigh *= 10;
                var digit = (int)BigInteger.DivRem(remainder, scale, out remainder);
                var canRoundDown = isSignificandEven ? remainder <= marginLow : remainder < marginLow;
                var canRoundUp = IsAboveHighBound(remainder + marginHigh, scale, isSignificandEven);
                if (!canRoundDown && !canRoundUp)
                {
                    digits.Append((char)('0' + digit));
                    continue;
                }

                bool roundUp;
                if (canRoundDown && canRoundUp)
                {
                    // Both are inside the interval, so take the closer one, or the even digit if the value is halfway between them.
                    var comparison = (remainder * 2).CompareTo(scale);
                    roundUp = comparison > 0 || (comparison == 0 && (digit & 1) != 0);
                }
                else
                {
                    roundUp = canRoundUp;
                }

                if (!roundUp)
                {
                    digits.Append((char)('0' + digit));
                    break;
                }

                // A digit of 9 is never rounded up: the bound above it would have been reached at the digit before, or, for the first digit,
                // the scaling above would have taken the next power of 10.
                Debug.Assert(digit < 9, "A digit of 9 is never rounded up.");
                digits.Append((char)('0' + digit + 1));
                break;
            }

            return LayOut(isNegative, digits.ToString(), decimalExponent, maxDigits, numberFormat);
        }

        /// <summary>
        /// Gets whether a digit string at the given distance above the generated digits would lie above the rounding interval, or on its bound, which
        /// is inside the interval only when the significand is even.
        /// </summary>
        private static bool IsAboveHighBound(BigInteger valueAndMarginHigh, BigInteger scale, bool isSignificandEven)
        {
            var comparison = valueAndMarginHigh.CompareTo(scale);
            return isSignificandEven ? comparison >= 0 : comparison > 0;
        }

        /// <summary>
        /// Lays out significant digits as <c>"R"</c> does: in fixed-point notation unless the decimal point would be more than
        /// <paramref name="maxDigits"/> places to the right of the first digit, or more than three places to its left, and otherwise in scientific
        /// notation with an exponent of at least two digits.
        /// </summary>
        /// <param name="isNegative">Whether the value is negative.</param>
        /// <param name="digits">The significant digits, the first of which is not zero.</param>
        /// <param name="decimalExponent">The number of places the decimal point lies to the right of the first digit.</param>
        /// <param name="maxDigits">The most significant digits the value's type is written with.</param>
        /// <param name="numberFormat">The number symbols to use.</param>
        private static string LayOut(bool isNegative, string digits, int decimalExponent, int maxDigits, NumberFormatInfo numberFormat)
        {
            var text = new StringBuilder(maxDigits + 8);
            if (isNegative)
            {
                text.Append(numberFormat.NegativeSign);
            }

            if (decimalExponent > maxDigits || decimalExponent < MinFixedPointDecimalExponent)
            {
                text.Append(digits[0]);
                if (digits.Length > 1)
                {
                    text.Append(numberFormat.NumberDecimalSeparator).Append(digits, 1, digits.Length - 1);
                }

                var scientificExponent = decimalExponent - 1;
                text.Append('E').Append(scientificExponent < 0 ? numberFormat.NegativeSign : numberFormat.PositiveSign);
                text.Append(Math.Abs(scientificExponent).ToString("00", CultureInfo.InvariantCulture));
            }
            else if (decimalExponent <= 0)
            {
                text.Append('0').Append(numberFormat.NumberDecimalSeparator).Append('0', -decimalExponent).Append(digits);
            }
            else if (digits.Length <= decimalExponent)
            {
                text.Append(digits).Append('0', decimalExponent - digits.Length);
            }
            else
            {
                text.Append(digits, 0, decimalExponent).Append(numberFormat.NumberDecimalSeparator).Append(digits, decimalExponent, digits.Length - decimalExponent);
            }

            return text.ToString();
        }
#endif
    }
}
