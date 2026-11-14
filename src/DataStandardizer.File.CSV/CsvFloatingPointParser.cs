#if !NETCOREAPP3_0_OR_GREATER
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Reads a <see cref="double"/> or <see cref="float"/> as the value nearest to its text, with a tie going to the even significand, as .NET Core
    /// 3.0 and later read it, for the targets that may run on .NET Framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// .NET Framework's parse isn't correctly rounded.  It reads some 16- and 17-digit values as a neighbouring value, rounds some values near zero
    /// the wrong way, reads negative zero as positive zero, and fails some text whose nearest <see cref="float"/> is <see cref="float.MaxValue"/>.
    /// </para>
    /// <para>
    /// The runtime's parse still decides which text is a number, so the accepted text is the same as before.  Only the value of finite text is
    /// worked out here, in exact integer arithmetic.  A runtime whose parse is correctly rounded, such as .NET Core 3.0 and later running a .NET
    /// Standard build, keeps its own value, which is the same and many times faster.
    /// </para>
    /// </remarks>
    internal static class CsvFloatingPointParser
    {
        /// <summary>
        /// The most significant digits kept from the text.  A decimal number halfway between two neighbouring doubles has at most 767 significant
        /// digits, so the digits after these can only tell whether the value is above such a number, never which side of it the value lies on.
        /// Any of them that isn't zero is kept as a single digit 1 after the kept digits.
        /// </summary>
        private const int MaxSignificantDigits = 800;

        /// <summary>
        /// The position of the first significant digit, as a power of 10, beyond which a value rounds to infinity or to zero in both types: double
        /// values lie between about 4.9E-324 and 1.8E+308.  Text beyond it isn't worked out in full, so that a large exponent can't make the
        /// arithmetic slow.
        /// </summary>
        private const int MaxDecimalMagnitude = 400;

        /// <summary>
        /// The exponent written in the text is read up to this value.  Any larger exponent is beyond <see cref="MaxDecimalMagnitude"/> anyway.
        /// </summary>
        private const int MaxWrittenExponent = 100000000;

        // The largest powers of 10 that a double and a float hold exactly, and the largest significands they hold exactly in the digits of the
        // fast paths.  An operation on two exactly held values is rounded once, so it gives the nearest value (Clinger, "How to Read Floating
        // Point Numbers Accurately", 1990).
        private const int DoubleFastPathMaxExponent = 22;
        private const int DoubleFastPathMaxDigits = 15;
        private const int SingleFastPathMaxExponent = 10;
        private const int SingleFastPathMaxDigits = 7;

        private const long DoubleSignBit = long.MinValue;
        private const int SingleSignBit = int.MinValue;

        private static readonly double[] PowersOf10 =
        {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22,
        };

        /// <summary>
        /// Whether the runtime's parse is correctly rounded, as it is on .NET Core 3.0 and later.  It is checked with text that .NET Framework reads
        /// otherwise: a neighbouring value, the wrong side of the smallest normal and subnormal values, positive zero for negative zero, and a
        /// failure for text whose nearest float is the largest.
        /// </summary>
        internal static readonly bool IsRuntimeParseCorrectlyRounded =
            RuntimeDoubleBitsAre("5.882948112288285E-204", 0x15BD82F529288671)
            && RuntimeDoubleBitsAre("2.2250738585072012E-308", 0x0010000000000000)
            && RuntimeDoubleBitsAre("2.4703282292062327E-324", 0)
            && RuntimeDoubleBitsAre("-0", DoubleSignBit)
            && RuntimeSingleBitsAre("7.038531E-26", 0x15AE43FD)
            && RuntimeSingleBitsAre("3.4028235677973366E+38", 0x7F7FFFFF);

        /// <summary>
        /// Parses a <see cref="double"/> with the nearest value to the text, as <see cref="double.TryParse(string, NumberStyles, IFormatProvider, out double)"/>
        /// does on .NET Core 3.0 and later.  Finite text too large for the type reads as infinity.
        /// </summary>
        internal static bool TryParseDouble(string value, NumberStyles styles, NumberFormatInfo numberFormat, out double result)
        {
            if (!double.TryParse(value, styles, numberFormat, out result))
            {
                return false;
            }

            if (IsRuntimeParseCorrectlyRounded || double.IsNaN(result) || double.IsInfinity(result))
            {
                return true;
            }

            var number = ReadDecimal(value, numberFormat);
            var bits = number.Digits.Length <= DoubleFastPathMaxDigits && Math.Abs(number.Exponent) <= DoubleFastPathMaxExponent
                ? BitConverter.DoubleToInt64Bits(ReadDoubleFastPath(number))
                : (long)RoundToBinary(number, CsvFieldFormatter.DoubleFractionBits, CsvFieldFormatter.DoubleExponentBias,
                    CsvFieldFormatter.DoubleExponentMask);
            result = BitConverter.Int64BitsToDouble(number.IsNegative ? bits | DoubleSignBit : bits);
            return true;
        }

        /// <summary>
        /// Parses a <see cref="float"/> with the nearest value to the text, as <see cref="float.TryParse(string, NumberStyles, IFormatProvider, out float)"/>
        /// does on .NET Core 3.0 and later.  Finite text too large for the type reads as infinity.
        /// </summary>
        /// <remarks>
        /// When the runtime's parse isn't correctly rounded, the text is checked with the runtime's <see cref="double"/> parse, which accepts the same
        /// text as its <see cref="float"/> parse except text too large for a <see cref="float"/>, which .NET Framework fails.
        /// </remarks>
        internal static bool TryParseSingle(string value, NumberStyles styles, NumberFormatInfo numberFormat, out float result)
        {
            if (IsRuntimeParseCorrectlyRounded)
            {
                return float.TryParse(value, styles, numberFormat, out result);
            }

            if (!double.TryParse(value, styles, numberFormat, out var doubleResult))
            {
                result = 0;
                return false;
            }

            if (double.IsNaN(doubleResult) || double.IsInfinity(doubleResult))
            {
                result = (float)doubleResult;
                return true;
            }

            var number = ReadDecimal(value, numberFormat);
            var bits = number.Digits.Length <= SingleFastPathMaxDigits && Math.Abs(number.Exponent) <= SingleFastPathMaxExponent
                ? BitConverter.ToInt32(BitConverter.GetBytes(ReadSingleFastPath(number)), 0)
                : (int)RoundToBinary(number, CsvFieldFormatter.SingleFractionBits, CsvFieldFormatter.SingleExponentBias,
                    CsvFieldFormatter.SingleExponentMask);
            result = BitConverter.ToSingle(BitConverter.GetBytes(number.IsNegative ? bits | SingleSignBit : bits), 0);
            return true;
        }

        private static bool RuntimeDoubleBitsAre(string value, long expectedBits)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                   && BitConverter.DoubleToInt64Bits(result) == expectedBits;
        }

        private static bool RuntimeSingleBitsAre(string value, int expectedBits)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                   && BitConverter.ToInt32(BitConverter.GetBytes(result), 0) == expectedBits;
        }

        /// <summary>
        /// Reads the sign, digits and exponent of text that the runtime's parse has accepted as a finite number.
        /// </summary>
        /// <remarks>
        /// The text is <c>[white space][sign]digits[decimal separator[digits]][e[sign]digits][white space]</c>, where the integer digits may hold group
        /// separators, as <see cref="NumberStyles.Float"/> and <see cref="NumberStyles.AllowThousands"/> allow.  The runtime matches the decimal
        /// separator before the group separator, and allows a group separator only before the decimal separator, and so does this.
        /// </remarks>
        private static DecimalNumber ReadDecimal(string value, NumberFormatInfo numberFormat)
        {
            var index = 0;
            while (index < value.Length && IsWhiteSpace(value[index]))
            {
                index++;
            }

            var isNegative = false;
            if (Matches(value, index, numberFormat.PositiveSign))
            {
                index += numberFormat.PositiveSign.Length;
            }
            else if (Matches(value, index, numberFormat.NegativeSign))
            {
                index += numberFormat.NegativeSign.Length;
                isNegative = true;
            }
            else if (index < value.Length && value[index] == '-')
            {
                // .NET Core accepts a hyphen for a negative sign that is another dash, such as U+2212.
                index++;
                isNegative = true;
            }

            // The value is digits * 10^exponent.  Leading zeros aren't kept.
            var digits = new StringBuilder();
            long exponent = 0;
            var hasDecimalSeparator = false;
            var hasDroppedNonZeroDigit = false;
            while (index < value.Length)
            {
                var character = value[index];
                if (character >= '0' && character <= '9')
                {
                    if (digits.Length == 0 && character == '0')
                    {
                        exponent -= hasDecimalSeparator ? 1 : 0;
                    }
                    else if (digits.Length < MaxSignificantDigits)
                    {
                        digits.Append(character);
                        exponent -= hasDecimalSeparator ? 1 : 0;
                    }
                    else
                    {
                        exponent += hasDecimalSeparator ? 0 : 1;
                        hasDroppedNonZeroDigit |= character != '0';
                    }

                    index++;
                }
                else if (!hasDecimalSeparator && Matches(value, index, numberFormat.NumberDecimalSeparator))
                {
                    hasDecimalSeparator = true;
                    index += numberFormat.NumberDecimalSeparator.Length;
                }
                else if (!hasDecimalSeparator && Matches(value, index, numberFormat.NumberGroupSeparator))
                {
                    index += numberFormat.NumberGroupSeparator.Length;
                }
                else
                {
                    break;
                }
            }

            if (hasDroppedNonZeroDigit)
            {
                digits.Append('1');
                exponent--;
            }

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                index++;
                var isExponentNegative = false;
                if (Matches(value, index, numberFormat.PositiveSign))
                {
                    index += numberFormat.PositiveSign.Length;
                }
                else if (Matches(value, index, numberFormat.NegativeSign))
                {
                    index += numberFormat.NegativeSign.Length;
                    isExponentNegative = true;
                }
                else if (index < value.Length && (value[index] == '-' || value[index] == '+'))
                {
                    isExponentNegative = value[index] == '-';
                    index++;
                }

                var writtenExponent = 0;
                while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                {
                    writtenExponent = Math.Min(writtenExponent * 10 + (value[index] - '0'), MaxWrittenExponent);
                    index++;
                }

                exponent += isExponentNegative ? -writtenExponent : writtenExponent;
            }

            // Trailing zeros only make the arithmetic bigger.
            var significantLength = digits.Length;
            while (significantLength > 0 && digits[significantLength - 1] == '0')
            {
                significantLength--;
                exponent++;
            }

            if (significantLength == 0)
            {
                return new DecimalNumber(isNegative, string.Empty, 0);
            }

            // The value is at least 10^firstDigitExponent and below 10 times that.
            var firstDigitExponent = exponent + significantLength - 1;
            if (firstDigitExponent > MaxDecimalMagnitude)
            {
                return new DecimalNumber(isNegative, "1", MaxDecimalMagnitude);
            }

            if (firstDigitExponent < -MaxDecimalMagnitude)
            {
                return new DecimalNumber(isNegative, string.Empty, 0);
            }

            return new DecimalNumber(isNegative, digits.ToString(0, significantLength), (int)exponent);
        }

        /// <summary>
        /// Gets whether a character is white space to the runtime's number parse: a space, or a tab, line feed, vertical tab, form feed or carriage
        /// return.
        /// </summary>
        private static bool IsWhiteSpace(char character) => character == ' ' || (character >= '\t' && character <= '\r');

        private static bool Matches(string value, int index, string symbol)
        {
            return !string.IsNullOrEmpty(symbol)
                   && index + symbol.Length <= value.Length
                   && string.CompareOrdinal(value, index, symbol, 0, symbol.Length) == 0;
        }

        private static double ReadDoubleFastPath(DecimalNumber number)
        {
            if (number.Digits.Length == 0)
            {
                return 0;
            }

            double significand = ulong.Parse(number.Digits, NumberStyles.None, CultureInfo.InvariantCulture);
            return number.Exponent >= 0 ? significand * PowersOf10[number.Exponent] : significand / PowersOf10[-number.Exponent];
        }

        /// <remarks>
        /// The operation is done on doubles, which hold the significand and power of 10 exactly, and then rounded to a float.  Rounding twice gives
        /// the nearest float here, because a double has more than twice as many significand bits as a float, and two more (Figueroa, "When is
        /// Double Rounding Innocuous?", 1995).
        /// </remarks>
        private static float ReadSingleFastPath(DecimalNumber number)
        {
            return (float)ReadDoubleFastPath(number);
        }

        /// <summary>
        /// Rounds a positive decimal number to the nearest binary floating-point value, with a tie going to the even significand.
        /// </summary>
        /// <returns>The bits of the value without the sign: infinity if it is too large for the type.</returns>
        private static ulong RoundToBinary(DecimalNumber number, int fractionBits, int exponentBias, int maxBiasedExponent)
        {
            if (number.Digits.Length == 0)
            {
                return 0;
            }

            // The value is numerator / denominator.
            var numerator = BigInteger.Parse(number.Digits, CultureInfo.InvariantCulture);
            var denominator = BigInteger.One;
            if (number.Exponent >= 0)
            {
                numerator *= BigInteger.Pow(10, number.Exponent);
            }
            else
            {
                denominator = BigInteger.Pow(10, -number.Exponent);
            }

            // Find the exponent of the last significand bit, so that the significand has one more bit than the fraction, or fewer for a subnormal
            // value, whose last bit has the exponent of the smallest normal value's.  The estimate from the bit lengths is at most one too low.
            var significandLimit = BigInteger.One << (fractionBits + 1);
            var minLastBitExponent = 1 - exponentBias - fractionBits;
            var lastBitExponent = Math.Max(GetBitLength(numerator) - GetBitLength(denominator) - fractionBits - 1, minLastBitExponent);
            BigInteger significand;
            BigInteger remainder;
            BigInteger divisor;
            while (true)
            {
                divisor = lastBitExponent >= 0 ? denominator << lastBitExponent : denominator;
                significand = BigInteger.DivRem(lastBitExponent >= 0 ? numerator : numerator << -lastBitExponent, divisor, out remainder);
                if (significand < significandLimit)
                {
                    break;
                }

                lastBitExponent++;
            }

            var comparison = (remainder * 2).CompareTo(divisor);
            if (comparison > 0 || (comparison == 0 && !significand.IsEven))
            {
                significand++;
                if (significand == significandLimit)
                {
                    significand >>= 1;
                    lastBitExponent++;
                }
            }

            var implicitBit = BigInteger.One << fractionBits;
            if (significand < implicitBit)
            {
                // A subnormal value, or zero.
                return (ulong)significand;
            }

            var biasedExponent = lastBitExponent + fractionBits + exponentBias;
            if (biasedExponent >= maxBiasedExponent)
            {
                return (ulong)maxBiasedExponent << fractionBits;
            }

            return ((ulong)biasedExponent << fractionBits) | (ulong)(significand - implicitBit);
        }

        /// <summary>
        /// Gets the number of bits in a positive integer, up to and including its highest set bit.
        /// </summary>
        private static int GetBitLength(BigInteger value)
        {
            // The bytes are little-endian two's complement, so a positive value whose highest bit is the top bit of a byte has a zero byte after it.
            var bytes = value.ToByteArray();
            var topIndex = bytes.Length - 1;
            if (bytes[topIndex] == 0 && topIndex > 0)
            {
                topIndex--;
            }

            var bitLength = topIndex * 8;
            for (int topByte = bytes[topIndex]; topByte != 0; topByte >>= 1)
            {
                bitLength++;
            }

            return bitLength;
        }

        /// <summary>
        /// A decimal number: its sign, and its significant digits and exponent, so that its magnitude is <c>digits * 10^exponent</c>.  No digits is
        /// zero.
        /// </summary>
        private struct DecimalNumber
        {
            internal DecimalNumber(bool isNegative, string digits, int exponent)
            {
                IsNegative = isNegative;
                Digits = digits;
                Exponent = exponent;
            }

            internal bool IsNegative { get; }

            internal string Digits { get; }

            internal int Exponent { get; }
        }
    }
}
#endif
