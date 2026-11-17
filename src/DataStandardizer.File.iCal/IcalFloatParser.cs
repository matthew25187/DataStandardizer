#if !NETCOREAPP3_0_OR_GREATER
using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Reads a FLOAT value (RFC 5545 §3.3.7, RFC 7265 §3.6.7, RFC 6321 §3.6.7) as the <see cref="double"/> nearest to its text, with a tie going
    /// to the even significand and the sign of zero kept, as .NET Core 3.0 and later read it, for the targets that may run on .NET Framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// .NET Framework's parse isn't correctly rounded.  It reads some 16- and 17-digit values as a neighbouring value, rounds some values near zero
    /// the wrong way, and reads negative zero as positive zero.
    /// </para>
    /// <para>
    /// The text has already been checked against the grammar of its form, so it is an optional sign, digits with at most one decimal point, and an
    /// optional <c>e</c> or <c>E</c> with an optional sign and digits.  A runtime whose parse is correctly rounded, such as .NET Core 3.0 and later
    /// running a .NET Standard build, reads it itself, which gives the same value and is many times faster.  Otherwise the value is worked out here
    /// in exact integer arithmetic.
    /// </para>
    /// </remarks>
    internal static class IcalFloatParser
    {
        /// <summary>
        /// The most significant digits kept from the text.  A decimal number halfway between two neighbouring doubles has at most 767 significant
        /// digits, so the digits after these can only tell whether the value is above such a number, never which side of it the value lies on.
        /// Any of them that isn't zero is kept as a single digit 1 after the kept digits.
        /// </summary>
        private const int MaxSignificantDigits = 800;

        /// <summary>
        /// The most significant digits held in a <see cref="ulong"/>.  Text with more is kept as a string of digits.
        /// </summary>
        private const int MaxUInt64Digits = 19;

        /// <summary>
        /// The position of the first significant digit, as a power of 10, beyond which a value rounds to infinity or to zero: double values lie
        /// between about 4.9E-324 and 1.8E+308.  Text beyond it isn't worked out in full, so that a large exponent can't make the arithmetic slow.
        /// </summary>
        private const int MaxDecimalMagnitude = 400;

        /// <summary>
        /// The exponent written in the text is read up to this value.  Any larger exponent is beyond <see cref="MaxDecimalMagnitude"/> anyway.
        /// </summary>
        private const int MaxWrittenExponent = 100000000;

        // The largest power of 10 that a double holds exactly, and the most significant digits it holds exactly in the fast path.  An operation on
        // two exactly held values is rounded once, so it gives the nearest value (Clinger, "How to Read Floating Point Numbers Accurately", 1990).
        private const int FastPathMaxExponent = 22;
        private const int FastPathMaxDigits = 15;

        private const long DoubleSignBit = long.MinValue;

        private static readonly double[] PowersOf10 =
        {
            1e0, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 1e20, 1e21, 1e22,
        };

        /// <summary>
        /// Whether the runtime's parse is correctly rounded, as it is on .NET Core 3.0 and later.  It is checked with text that .NET Framework reads
        /// otherwise: a neighbouring value, a value just above halfway in more digits than it reads, the wrong side of the smallest normal and
        /// subnormal values, and positive zero for negative zero.
        /// </summary>
        private static readonly bool IsRuntimeParseCorrectlyRounded =
            RuntimeBitsAre("5.882948112288285E-204", 0x15BD82F529288671)
            && RuntimeBitsAre("9007199254740993.0000000000000001", 0x4340000000000001)
            && RuntimeBitsAre("2.2250738585072012E-308", 0x0010000000000000)
            && RuntimeBitsAre("2.4703282292062327E-324", 0)
            && RuntimeBitsAre("-0", DoubleSignBit);

        /// <summary>
        /// Reads FLOAT text, which matches the grammar of its form, as the nearest <see cref="double"/>.
        /// </summary>
        /// <param name="value">The text.</param>
        /// <param name="styles">The styles the runtime's parse reads the text with, when it is correctly rounded.</param>
        /// <param name="result">The nearest value, which is infinite when the text is too large for a <see cref="double"/>.</param>
        /// <returns><c>true</c> if the text was read; otherwise <c>false</c>, when the runtime's parse found it too large.</returns>
        internal static bool TryParse(ReadOnlySpan<char> value, NumberStyles styles, out double result)
        {
            if (IsRuntimeParseCorrectlyRounded)
            {
#if NETSTANDARD2_1_OR_GREATER
                return double.TryParse(value, styles, CultureInfo.InvariantCulture, out result);
#else
                return double.TryParse(value.ToString(), styles, CultureInfo.InvariantCulture, out result);
#endif
            }

            var number = ReadDecimal(value);
            var magnitudeBits = number.DigitCount <= FastPathMaxDigits && Math.Abs(number.Exponent) <= FastPathMaxExponent
                ? BitConverter.DoubleToInt64Bits(ReadFastPath(number))
                : (long)RoundToBinary(number);
            result = BitConverter.Int64BitsToDouble(number.IsNegative ? magnitudeBits | DoubleSignBit : magnitudeBits);
            return true;
        }

        private static bool RuntimeBitsAre(string value, long expectedBits)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                   && BitConverter.DoubleToInt64Bits(result) == expectedBits;
        }

        /// <summary>
        /// Reads the sign, significant digits and exponent of FLOAT text.
        /// </summary>
        private static DecimalNumber ReadDecimal(ReadOnlySpan<char> value)
        {
            var index = 0;
            var isNegative = false;
            if (index < value.Length && (value[index] == '+' || value[index] == '-'))
            {
                isNegative = value[index] == '-';
                index++;
            }

            // The value is the significant digits * 10^exponent.  Leading zeros aren't significant.  The first digits are held in a ulong, and
            // only text with more than it holds is kept as a string.
            ulong significand = 0;
#if NETSTANDARD2_1_OR_GREATER
            StringBuilder? longDigits = null;
#else
            StringBuilder longDigits = null;
#endif
            var digitCount = 0;
            long exponent = 0;
            var hasDecimalPoint = false;
            var hasDroppedNonZeroDigit = false;
            while (index < value.Length)
            {
                var character = value[index];
                if (character >= '0' && character <= '9')
                {
                    if (digitCount == 0 && character == '0')
                    {
                        exponent -= hasDecimalPoint ? 1 : 0;
                    }
                    else if (digitCount < MaxUInt64Digits)
                    {
                        significand = (significand * 10) + (ulong)(character - '0');
                        digitCount++;
                        exponent -= hasDecimalPoint ? 1 : 0;
                    }
                    else if (digitCount < MaxSignificantDigits)
                    {
                        longDigits = longDigits ?? new StringBuilder(significand.ToString(CultureInfo.InvariantCulture));
                        longDigits.Append(character);
                        digitCount++;
                        exponent -= hasDecimalPoint ? 1 : 0;
                    }
                    else
                    {
                        exponent += hasDecimalPoint ? 0 : 1;
                        hasDroppedNonZeroDigit |= character != '0';
                    }
                }
                else if (character == '.')
                {
                    hasDecimalPoint = true;
                }
                else
                {
                    break;
                }

                index++;
            }

            // A dropped digit means the kept digits filled the string.
            if (hasDroppedNonZeroDigit && longDigits != null)
            {
                longDigits.Append('1');
                digitCount++;
                exponent--;
            }

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                exponent += ReadExponent(value, index + 1);
            }

            // Trailing zeros only make the arithmetic bigger.
            if (longDigits is null)
            {
                while (digitCount > 0 && significand % 10 == 0)
                {
                    significand /= 10;
                    digitCount--;
                    exponent++;
                }
            }
            else
            {
                while (longDigits[digitCount - 1] == '0')
                {
                    digitCount--;
                    exponent++;
                }

                // Without its trailing zeros, such as in a whole number of more than 19 digits, the significand may fit a ulong again.
                if (digitCount <= MaxUInt64Digits)
                {
                    significand = ulong.Parse(longDigits.ToString(0, digitCount), NumberStyles.None, CultureInfo.InvariantCulture);
                    longDigits = null;
                }
            }

            if (digitCount == 0)
            {
                return new DecimalNumber(isNegative, 0, string.Empty, 0, 0);
            }

            // The value is at least 10^firstDigitExponent and below 10 times that.
            var firstDigitExponent = exponent + digitCount - 1;
            if (firstDigitExponent > MaxDecimalMagnitude)
            {
                return new DecimalNumber(isNegative, 1, string.Empty, 1, MaxDecimalMagnitude);
            }

            if (firstDigitExponent < -MaxDecimalMagnitude)
            {
                return new DecimalNumber(isNegative, 0, string.Empty, 0, 0);
            }

            return new DecimalNumber(isNegative, significand, longDigits is null ? string.Empty : longDigits.ToString(0, digitCount), digitCount,
                (int)exponent);
        }

        /// <summary>
        /// Reads the sign and digits of an exponent, which start after the <c>e</c> or <c>E</c>.
        /// </summary>
        private static int ReadExponent(ReadOnlySpan<char> value, int index)
        {
            var isNegative = false;
            if (index < value.Length && (value[index] == '+' || value[index] == '-'))
            {
                isNegative = value[index] == '-';
                index++;
            }

            var exponent = 0;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                exponent = Math.Min((exponent * 10) + (value[index] - '0'), MaxWrittenExponent);
                index++;
            }

            return isNegative ? -exponent : exponent;
        }

        private static double ReadFastPath(DecimalNumber number)
        {
            double significand = number.Significand;
            return number.Exponent >= 0 ? significand * PowersOf10[number.Exponent] : significand / PowersOf10[-number.Exponent];
        }

        /// <summary>
        /// Rounds a positive decimal number to the nearest double, with a tie going to the even significand.
        /// </summary>
        /// <param name="number">The decimal number.  Its sign isn't used.</param>
        /// <returns>The bits of the value without the sign: infinity if it is too large for a double.</returns>
        private static ulong RoundToBinary(DecimalNumber number)
        {
            if (number.DigitCount == 0)
            {
                return 0;
            }

            // The value is numerator / denominator.
            var numerator = number.Digits.Length == 0 ? new BigInteger(number.Significand) : BigInteger.Parse(number.Digits, CultureInfo.InvariantCulture);
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
            const int fractionBits = IcalFloatFormatter.DoubleFractionBits;
            var significandLimit = BigInteger.One << (fractionBits + 1);
            var minLastBitExponent = 1 - IcalFloatFormatter.DoubleExponentBias - fractionBits;
            var lastBitExponent = Math.Max(GetBitLength(numerator) - GetBitLength(denominator) - fractionBits - 1, minLastBitExponent);
            var divisor = lastBitExponent >= 0 ? denominator << lastBitExponent : denominator;
            var significand = BigInteger.DivRem(lastBitExponent >= 0 ? numerator : numerator << -lastBitExponent, divisor, out var remainder);
            if (significand >= significandLimit)
            {
                // One bit too many: halve the significand, and keep the bit shifted out in the remainder of the doubled divisor.
                if (!significand.IsEven)
                {
                    remainder += divisor;
                }

                significand >>= 1;
                divisor <<= 1;
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

            var biasedExponent = lastBitExponent + fractionBits + IcalFloatFormatter.DoubleExponentBias;
            if (biasedExponent >= IcalFloatFormatter.DoubleExponentMask)
            {
                return (ulong)IcalFloatFormatter.DoubleExponentMask << fractionBits;
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
        /// A decimal number: its sign, and its significant digits and exponent, so that its magnitude is <c>digits * 10^exponent</c>.  The digits
        /// are <see cref="Significand"/> when there are no more than a <see cref="ulong"/> holds, and <see cref="Digits"/> otherwise, which is empty
        /// when they aren't.  No digits is zero.
        /// </summary>
        private readonly struct DecimalNumber
        {
            internal DecimalNumber(bool isNegative, ulong significand, string digits, int digitCount, int exponent)
            {
                IsNegative = isNegative;
                Significand = significand;
                Digits = digits;
                DigitCount = digitCount;
                Exponent = exponent;
            }

            internal bool IsNegative { get; }

            internal ulong Significand { get; }

            internal string Digits { get; }

            internal int DigitCount { get; }

            internal int Exponent { get; }
        }
    }
}
#endif
