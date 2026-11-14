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
    /// worked out here, in exact integer arithmetic, from the text read again with the runtime's rules.  .NET Framework's value is never more than
    /// <see cref="MaxRuntimeErrorUnits"/> units in the last place away from the nearest value, so a value further than that from it means the text
    /// was read differently here, and the runtime's value is kept.  A runtime whose parse is correctly rounded, such as .NET Core 3.0 and later
    /// running a .NET Standard build, keeps its own value, which is the same and many times faster.
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
        /// The most significant digits held in a <see cref="ulong"/>.  Text with more is kept as a string of digits.
        /// </summary>
        private const int MaxUInt64Digits = 19;

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

        /// <summary>
        /// The most units in the last place that the runtime's value may differ from the nearest value before the text is taken to have been read
        /// differently here.  .NET Framework's value differs by at most one.
        /// </summary>
        private const int MaxRuntimeErrorUnits = 2;

        /// <summary>
        /// The <see cref="NumberFormatInfo.NumberNegativePattern"/> <c>"- n"</c>, with which the runtime's parse allows white space after a sign.
        /// </summary>
        private const int SpacedNegativePattern = 2;

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
        /// otherwise: a neighbouring value, a value just above halfway in more digits than it reads, the wrong side of the smallest normal and
        /// subnormal values, positive zero for negative zero, the float a double halfway value rounds to, and a failure for text whose nearest float
        /// is the largest.
        /// </summary>
        internal static readonly bool IsRuntimeParseCorrectlyRounded =
            RuntimeDoubleBitsAre("5.882948112288285E-204", 0x15BD82F529288671)
            && RuntimeDoubleBitsAre("9007199254740993.0000000000000001", 0x4340000000000001)
            && RuntimeDoubleBitsAre("2.2250738585072012E-308", 0x0010000000000000)
            && RuntimeDoubleBitsAre("2.4703282292062327E-324", 0)
            && RuntimeDoubleBitsAre("-0", DoubleSignBit)
            && RuntimeSingleBitsAre("7.038531E-26", 0x15AE43FD)
            && RuntimeSingleBitsAre("1.0000000596046448", 0x3F800001)
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
            var magnitudeBits = number.DigitCount <= DoubleFastPathMaxDigits && Math.Abs(number.Exponent) <= DoubleFastPathMaxExponent
                ? BitConverter.DoubleToInt64Bits(ReadDoubleFastPath(number))
                : (long)RoundToBinary(number, CsvFieldFormatter.DoubleFractionBits, CsvFieldFormatter.DoubleExponentBias,
                    CsvFieldFormatter.DoubleExponentMask);
            if (IsNearRuntimeValue(magnitudeBits, BitConverter.DoubleToInt64Bits(Math.Abs(result)), number.IsNegative, result < 0, result == 0))
            {
                result = BitConverter.Int64BitsToDouble(number.IsNegative ? magnitudeBits | DoubleSignBit : magnitudeBits);
            }

            return true;
        }

        /// <summary>
        /// Parses a <see cref="float"/> with the nearest value to the text, as <see cref="float.TryParse(string, NumberStyles, IFormatProvider, out float)"/>
        /// does on .NET Core 3.0 and later.  Finite text too large for the type reads as infinity.
        /// </summary>
        /// <remarks>
        /// When the runtime's parse isn't correctly rounded, the text is checked with the runtime's <see cref="double"/> parse.  .NET Framework reads
        /// a <see cref="float"/> with the same rules as a <see cref="double"/>, and then fails a value too large for a <see cref="float"/>.
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

            result = (float)doubleResult;
            if (double.IsNaN(doubleResult) || double.IsInfinity(doubleResult))
            {
                return true;
            }

            var number = ReadDecimal(value, numberFormat);
            var magnitudeBits = number.DigitCount <= SingleFastPathMaxDigits && Math.Abs(number.Exponent) <= SingleFastPathMaxExponent
                ? CsvFloatingPointBits.SingleToInt32Bits(ReadSingleFastPath(number))
                : (int)RoundToBinary(number, CsvFieldFormatter.SingleFractionBits, CsvFieldFormatter.SingleExponentBias,
                    CsvFieldFormatter.SingleExponentMask);
            if (IsNearRuntimeValue(magnitudeBits, CsvFloatingPointBits.SingleToInt32Bits(Math.Abs(result)), number.IsNegative, doubleResult < 0,
                    doubleResult == 0))
            {
                result = CsvFloatingPointBits.Int32BitsToSingle(number.IsNegative ? magnitudeBits | SingleSignBit : magnitudeBits);
            }

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
                   && CsvFloatingPointBits.SingleToInt32Bits(result) == expectedBits;
        }

        /// <summary>
        /// Gets whether the value worked out here is close enough to the runtime's value to be the nearest value to the same text.  The bits of
        /// positive values are in the same order as the values, so their difference counts units in the last place.  The runtime may read negative
        /// zero as positive zero, so a zero has either sign.
        /// </summary>
        private static bool IsNearRuntimeValue(long magnitudeBits, long runtimeMagnitudeBits, bool isNegative, bool isRuntimeNegative,
            bool isRuntimeZero)
        {
            return (isRuntimeZero || isNegative == isRuntimeNegative) && Math.Abs(magnitudeBits - runtimeMagnitudeBits) <= MaxRuntimeErrorUnits;
        }

        /// <summary>
        /// Reads the sign, digits and exponent of text that the runtime's parse has accepted as a finite number.
        /// </summary>
        /// <remarks>
        /// The text is <c>[white space][sign][white space]digits[decimal separator[digits]][e[sign]digits][white space]</c>, where the integer digits
        /// may hold group separators, as <see cref="NumberStyles.Float"/> and <see cref="NumberStyles.AllowThousands"/> allow.  These are the
        /// runtime's rules: white space after the sign only with the negative pattern <c>"- n"</c>, the decimal separator matched before the group
        /// separator, a group separator only before the decimal separator, and a space in the text matching a no-break space in a symbol.
        /// </remarks>
        private static DecimalNumber ReadDecimal(string value, NumberFormatInfo numberFormat)
        {
            var index = SkipWhiteSpace(value, 0);
            var isNegative = false;
            var hasSign = true;
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
            else
            {
                hasSign = false;
            }

            if (hasSign && numberFormat.NumberNegativePattern == SpacedNegativePattern)
            {
                index = SkipWhiteSpace(value, index);
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
            var hasDecimalSeparator = false;
            var hasDroppedNonZeroDigit = false;
            while (index < value.Length)
            {
                var character = value[index];
                if (character >= '0' && character <= '9')
                {
                    if (digitCount == 0 && character == '0')
                    {
                        exponent -= hasDecimalSeparator ? 1 : 0;
                    }
                    else if (digitCount < MaxUInt64Digits)
                    {
                        significand = (significand * 10) + (ulong)(character - '0');
                        digitCount++;
                        exponent -= hasDecimalSeparator ? 1 : 0;
                    }
                    else if (digitCount < MaxSignificantDigits)
                    {
                        longDigits = longDigits ?? new StringBuilder(significand.ToString(CultureInfo.InvariantCulture));
                        longDigits.Append(character);
                        digitCount++;
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

            // A dropped digit means the kept digits filled the string.
            if (hasDroppedNonZeroDigit && longDigits != null)
            {
                longDigits.Append('1');
                digitCount++;
                exponent--;
            }

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                exponent += ReadExponent(value, index + 1, numberFormat);
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

            return new DecimalNumber(isNegative, significand, longDigits is null ? string.Empty : longDigits.ToString(0, digitCount), digitCount, (int)exponent);
        }

        /// <summary>
        /// Reads the sign and digits of an exponent, which start after the <c>e</c> or <c>E</c>.
        /// </summary>
        private static int ReadExponent(string value, int index, NumberFormatInfo numberFormat)
        {
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
            else if (index < value.Length && (value[index] == '-' || value[index] == '+'))
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

        private static int SkipWhiteSpace(string value, int index)
        {
            while (index < value.Length && IsWhiteSpace(value[index]))
            {
                index++;
            }

            return index;
        }

        /// <summary>
        /// Gets whether a character is white space to the runtime's number parse: a space, or a tab, line feed, vertical tab, form feed or carriage
        /// return.
        /// </summary>
        private static bool IsWhiteSpace(char character) => character == ' ' || (character >= '\t' && character <= '\r');

        /// <summary>
        /// Gets whether a culture symbol is at a position in the text, as the runtime's parse matches it: a space in the text matches a no-break
        /// space (U+00A0) in the symbol, and on .NET Core a narrow no-break space (U+202F) too.
        /// </summary>
        private static bool Matches(string value, int index, string symbol)
        {
            if (string.IsNullOrEmpty(symbol) || index + symbol.Length > value.Length)
            {
                return false;
            }

            for (var offset = 0; offset < symbol.Length; offset++)
            {
                var character = value[index + offset];
                var symbolCharacter = symbol[offset];
                if (character != symbolCharacter && !(character == ' ' && (symbolCharacter == ' ' || symbolCharacter == ' ')))
                {
                    return false;
                }
            }

            return true;
        }

        private static double ReadDoubleFastPath(DecimalNumber number)
        {
            double significand = number.Significand;
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
        /// <param name="number">The decimal number.  Its sign isn't used.</param>
        /// <param name="fractionBits">The number of fraction bits in the type.</param>
        /// <param name="exponentBias">The exponent bias of the type.</param>
        /// <param name="infinityBiasedExponent">The biased exponent of infinity, which has every exponent bit set.</param>
        /// <returns>The bits of the value without the sign: infinity if it is too large for the type.</returns>
        private static ulong RoundToBinary(DecimalNumber number, int fractionBits, int exponentBias, int infinityBiasedExponent)
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
            var significandLimit = BigInteger.One << (fractionBits + 1);
            var minLastBitExponent = 1 - exponentBias - fractionBits;
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

            var biasedExponent = lastBitExponent + fractionBits + exponentBias;
            if (biasedExponent >= infinityBiasedExponent)
            {
                return (ulong)infinityBiasedExponent << fractionBits;
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
        /// when they aren't.  No digits is
        /// zero.
        /// </summary>
        private struct DecimalNumber
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
