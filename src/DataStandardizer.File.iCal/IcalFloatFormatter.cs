using System;
using System.Globalization;
#if !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics;
using System.Numerics;
#endif
using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Formats a FLOAT value (RFC 5545 §3.3.7) with the fewest significant digits that read back as the same <see cref="double"/>, in fixed-point
    /// notation, and as the same text on every target.
    /// </summary>
    /// <remarks>
    /// The digits are those <c>"R"</c> gives on .NET Core 3.0 and later.  .NET Framework's <c>"R"</c> gives other digits, some of which don't
    /// read back as the same value, so the targets that may run on it generate the digits themselves.
    /// </remarks>
    internal static class IcalFloatFormatter
    {
        /// <summary>
        /// Formats a finite <see cref="double"/> with the fewest significant digits that read back as the same value, and no exponent.
        /// </summary>
        /// <param name="value">The number, which is neither NaN nor infinite.</param>
        /// <returns>The value in fixed-point notation, with <c>-</c> before a negative value, including negative zero.</returns>
        internal static string Format(double value)
        {
#if NETCOREAPP3_0_OR_GREATER
            return ExpandRoundTripText(value.ToString("R", CultureInfo.InvariantCulture));
#else
            if (IsRuntimeRoundTripFormatShortest) return ExpandRoundTripText(value.ToString("R", CultureInfo.InvariantCulture));

            var bits = BitConverter.DoubleToInt64Bits(value);
            return FormatShortest(bits < 0, (ulong)bits & DoubleFractionMask, (int)((bits >> DoubleFractionBits) & DoubleExponentMask));
#endif
        }

        /// <summary>
        /// Rewrites the text <c>"R"</c> gives on .NET Core 3.0 and later in fixed-point notation, since a FLOAT value has no exponent.
        /// </summary>
        private static string ExpandRoundTripText(string text)
        {
            var exponentIndex = text.IndexOf('E');
            if (exponentIndex < 0) return text;

            var isNegative = text[0] == '-';
            var mantissa = text.Substring(isNegative ? 1 : 0, exponentIndex - (isNegative ? 1 : 0));
            var exponent = int.Parse(text.Substring(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var pointIndex = mantissa.IndexOf('.');
            var digits = pointIndex < 0 ? mantissa : mantissa.Remove(pointIndex, 1);
            return LayOutFixedPoint(isNegative, digits, (pointIndex < 0 ? mantissa.Length : pointIndex) + exponent);
        }

        /// <summary>
        /// Lays out significant digits in fixed-point notation.
        /// </summary>
        /// <param name="isNegative">Whether the value is negative.</param>
        /// <param name="digits">The significant digits, the first of which is not zero.</param>
        /// <param name="decimalExponent">The number of places the decimal point lies to the right of the first digit.</param>
        private static string LayOutFixedPoint(bool isNegative, string digits, int decimalExponent)
        {
            var text = new StringBuilder(digits.Length + Math.Abs(decimalExponent) + 3);
            if (isNegative)
            {
                text.Append('-');
            }

            if (decimalExponent <= 0)
            {
                text.Append("0.").Append('0', -decimalExponent).Append(digits);
            }
            else if (digits.Length <= decimalExponent)
            {
                text.Append(digits).Append('0', decimalExponent - digits.Length);
            }
            else
            {
                text.Append(digits, 0, decimalExponent).Append('.').Append(digits, decimalExponent, digits.Length - decimalExponent);
            }

            return text.ToString();
        }

#if !NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Whether the runtime's <c>"R"</c> writes the digits <see cref="FormatShortest"/> generates, as it does on .NET Core 3.0 and later, so
        /// that a .NET Standard build running there can use it, which is many times faster.  It is checked with values that .NET Framework's
        /// <c>"R"</c> writes otherwise: with too few digits to read back, with too many, without the sign of negative zero, or in scientific
        /// notation.
        /// </summary>
        private static readonly bool IsRuntimeRoundTripFormatShortest =
            string.Equals(901.7426589977661.ToString("R", CultureInfo.InvariantCulture), "901.7426589977661", StringComparison.Ordinal)
            && string.Equals((0.1 + 0.7).ToString("R", CultureInfo.InvariantCulture), "0.7999999999999999", StringComparison.Ordinal)
            && string.Equals((-0.0).ToString("R", CultureInfo.InvariantCulture), "-0", StringComparison.Ordinal)
            && string.Equals(double.Epsilon.ToString("R", CultureInfo.InvariantCulture), "5E-324", StringComparison.Ordinal)
            && string.Equals(1E+16.ToString("R", CultureInfo.InvariantCulture), "10000000000000000", StringComparison.Ordinal);

        // The IEEE 754 binary64 layout, which IcalFloatParser also uses.
        internal const int DoubleFractionBits = 52;
        private const ulong DoubleFractionMask = (1UL << DoubleFractionBits) - 1;
        internal const int DoubleExponentMask = 0x7FF;
        internal const int DoubleExponentBias = 1023;

        // The most significant digits a double needs to read back as the same value.
        private const int DoubleMaxDigits = 17;

        // Taken off the logarithm of a value so that rounding error can't make the estimate of its decimal exponent too high.
        private const double LogarithmTolerance = 1e-10;

        /// <summary>
        /// Formats a finite double with the fewest significant digits that read back as the same value, in fixed-point notation.
        /// </summary>
        /// <remarks>
        /// The digits are generated with the free-format algorithm of Steele and White, as refined by Burger and Dybvig ("Printing Floating-Point
        /// Numbers Quickly and Accurately", 1996), in exact integer arithmetic.  Of the shortest digit strings that read back as the value, the one
        /// closest to it is chosen, and a tie goes to the even last digit.  A digit string reads back as the value when it lies inside the value's
        /// rounding interval, whose bounds are included when the value's significand is even, as round-half-to-even reading includes them.  These
        /// are the digits .NET Core 3.0 and later give for <c>"R"</c>.
        /// </remarks>
        private static string FormatShortest(bool isNegative, ulong fraction, int biasedExponent)
        {
            if (fraction == 0 && biasedExponent == 0)
            {
                return isNegative ? "-0" : "0";
            }

            // The value is significand * 2^exponent.  A subnormal value has no implicit leading bit and the exponent of the smallest normal value.
            ulong significand;
            int exponent;
            if (biasedExponent == 0)
            {
                significand = fraction;
                exponent = 1 - DoubleExponentBias - DoubleFractionBits;
            }
            else
            {
                significand = fraction | (1UL << DoubleFractionBits);
                exponent = biasedExponent - DoubleExponentBias - DoubleFractionBits;
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
            var digits = new StringBuilder(DoubleMaxDigits);
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

            return LayOutFixedPoint(isNegative, digits.ToString(), decimalExponent);
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
#endif
    }
}
