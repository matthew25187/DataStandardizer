﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Converts a mapping's constant value to the type of the property a reader gives it to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// An attribute argument can't be of every type a property can be, such as <see cref="decimal"/>, and a number written in an attribute has the
    /// type of its literal, such as <see cref="int"/> for <c>5</c>.  So a number is converted to a property of another number type, as long as the
    /// property can hold the number as it is written.
    /// </para>
    /// <para>
    /// A <see cref="float"/> or <see cref="double"/> doesn't record how it was written, so it is taken to be written as the decimal with the fewest
    /// significant digits its type converts to: up to 7 for a float and 15 for a double, as a cast to <see cref="decimal"/> rounds it.  So
    /// <c>0.1</c> and <c>0.1f</c> are both 0.1, and convert to a decimal, double or float of 0.1.  A value that needs more digits, such as
    /// <c>0.30000000000000004</c>, converts only to a type that holds it exactly.
    /// </para>
    /// </remarks>
    internal static class CsvConstantValue
    {
        // 2^63, 2^64 and 2^96.  Every whole double at or above -2^63 and below 2^63 casts to long without overflow, every one from 0 up to 2^64
        // to ulong, and every one below 2^96 in magnitude is a decimal.
        private const double TwoToThe63 = 9223372036854775808.0;
        private const double TwoToThe64 = 18446744073709551616.0;
        private const double TwoToThe96 = 79228162514264337593543950336.0;

        // The bits of a double's and a float's significand, counting the implicit leading bit, and a double's exponent bias.
        private const int DoubleSignificandBits = 53;
        private const int SingleSignificandBits = 24;
        private const int DoubleExponentBias = 1023;

        // The layout of the fourth part of decimal.GetBits: the scale in bits 16 to 23 and the sign in bit 31.
        private const int DecimalFlagsIndex = 3;
        private const int DecimalScaleShift = 16;
        private const int DecimalScaleMask = 0xFF;

        // The name of the exception data item that holds the property's name, as the reader and the custom model conversions name it.
        private const string PropertyNameDataItem = "PropertyName";

        /// <summary>
        /// Converts a mapping's constant value to the type of its property.
        /// </summary>
        /// <param name="constantValue">The constant value.</param>
        /// <param name="propertyType">The type of the property the mapping is for.</param>
        /// <param name="convertedValue">The value to give the property, or <c>null</c> if it can't be converted.</param>
        /// <returns>
        /// <c>true</c> if the constant is a value of the property type, text, which is converted when it is read like a field's text, or a number
        /// that the property's number type holds as it is written; otherwise <c>false</c>.
        /// </returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal static bool TryConvert(object constantValue, Type propertyType, out object? convertedValue)
#else
        internal static bool TryConvert([DisallowNull] object constantValue, [DisallowNull] Type propertyType, [MaybeNull] out object convertedValue)
#endif
        {
            if (constantValue is string || propertyType.GetTypeInfo().IsAssignableFrom(constantValue.GetType().GetTypeInfo()))
            {
                convertedValue = constantValue;
                return true;
            }

            var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            convertedValue = null;
            if (IsNumberType(targetType))
            {
                switch (constantValue)
                {
                    case sbyte sbyteValue:
                        convertedValue = FromInt64(sbyteValue, targetType);
                        break;
                    case short shortValue:
                        convertedValue = FromInt64(shortValue, targetType);
                        break;
                    case int intValue:
                        convertedValue = FromInt64(intValue, targetType);
                        break;
                    case long longValue:
                        convertedValue = FromInt64(longValue, targetType);
                        break;
                    case byte byteValue:
                        convertedValue = FromInt64(byteValue, targetType);
                        break;
                    case ushort ushortValue:
                        convertedValue = FromInt64(ushortValue, targetType);
                        break;
                    case uint uintValue:
                        convertedValue = FromInt64(uintValue, targetType);
                        break;
                    case ulong ulongValue:
                        convertedValue = FromUInt64(ulongValue, targetType);
                        break;
                    case float floatValue:
                        convertedValue = FromFloat(floatValue, targetType);
                        break;
                    case double doubleValue:
                        convertedValue = FromDouble(doubleValue, targetType);
                        break;
                    case decimal decimalValue:
                        convertedValue = FromDecimal(decimalValue, targetType);
                        break;
                }
            }

            return convertedValue != null;
        }

        /// <summary>
        /// Builds the exception for a mapping whose constant value the property's type can't hold as it is written.
        /// </summary>
        internal static CsvFileException BuildNotConvertibleException(KeyValuePair<string, CsvFieldMapping> propertyFieldMapping)
        {
            var fieldMapping = propertyFieldMapping.Value;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            var constantValue = fieldMapping.ConstantValue!;
#else
            var constantValue = fieldMapping.ConstantValue;
#endif
            var formattedValue = CsvFieldFormatter.Format(constantValue, CultureInfo.InvariantCulture);
            var exception = new CsvFileException(
                $"The constant value {formattedValue} of property '{propertyFieldMapping.Key}' is a {CsvTypeName.Format(constantValue.GetType())}, which " +
                $"the property's type {CsvTypeName.Format(fieldMapping.PropertyType)} can't hold as it is written.");
            exception.Data.Add(PropertyNameDataItem, propertyFieldMapping.Key);
            return exception;
        }

        // The number types a constant converts between.  Each of the From methods below is given one of them.
        private static bool IsNumberType(Type type)
        {
            return type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long)
                || type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong)
                || type == typeof(float) || type == typeof(double) || type == typeof(decimal);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? FromInt64(long value, Type targetType)
#else
        [return: MaybeNull]
        private static object FromInt64(long value, [DisallowNull] Type targetType)
#endif
        {
            // Each narrowing cast is made only once the value is known to be in range.
            if (targetType == typeof(sbyte)) return value >= sbyte.MinValue && value <= sbyte.MaxValue ? (sbyte)value : NoValue();
            if (targetType == typeof(short)) return value >= short.MinValue && value <= short.MaxValue ? (short)value : NoValue();
            if (targetType == typeof(int)) return value >= int.MinValue && value <= int.MaxValue ? (int)value : NoValue();
            if (targetType == typeof(long)) return value;
            if (targetType == typeof(byte)) return value >= byte.MinValue && value <= byte.MaxValue ? (byte)value : NoValue();
            if (targetType == typeof(ushort)) return value >= ushort.MinValue && value <= ushort.MaxValue ? (ushort)value : NoValue();
            if (targetType == typeof(uint)) return value >= uint.MinValue && value <= uint.MaxValue ? (uint)value : NoValue();
            if (targetType == typeof(ulong)) return value >= 0 ? (ulong)value : NoValue();
            if (targetType == typeof(decimal)) return (decimal)value;

            // Left are float and double, which hold a long exactly if it converts back to the same long.  A double of 2^63 is past the range of
            // long, so it can't be one.
            var doubleValue = (double)value;
            return doubleValue < TwoToThe63 && (long)doubleValue == value ? ToExactFloatingPoint(doubleValue, targetType) : NoValue();
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? FromUInt64(ulong value, Type targetType)
#else
        [return: MaybeNull]
        private static object FromUInt64(ulong value, [DisallowNull] Type targetType)
#endif
        {
            if (value <= long.MaxValue)
            {
                return FromInt64((long)value, targetType);
            }

            if (targetType == typeof(ulong)) return value;
            if (targetType == typeof(decimal)) return (decimal)value;
            if (targetType != typeof(float) && targetType != typeof(double)) return NoValue();

            // A double holds a ulong exactly if it converts back to the same ulong.  A double of 2^64 is past the range of ulong, so it can't be one.
            var doubleValue = (double)value;
            return doubleValue < TwoToThe64 && (ulong)doubleValue == value ? ToExactFloatingPoint(doubleValue, targetType) : NoValue();
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? FromFloat(float value, Type targetType)
#else
        [return: MaybeNull]
        private static object FromFloat(float value, [DisallowNull] Type targetType)
#endif
        {
            // Every float is a double, so a float is converted as a double, unless it is written with fewer digits as a float than as a double.
            if (IsFinite(value) && !IsWholeNumber(value))
            {
                var writtenValue = (decimal)value;
                if (ToSingle(writtenValue) == value)
                {
                    return FromWrittenValue(writtenValue, targetType);
                }
            }

            return FromDouble(value, targetType);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? FromDouble(double value, Type targetType)
#else
        [return: MaybeNull]
        private static object FromDouble(double value, [DisallowNull] Type targetType)
#endif
        {
            if (!IsFinite(value))
            {
                return ToExactFloatingPoint(value, targetType);
            }

            if (IsWholeNumber(value))
            {
                // A whole number is held exactly by an integer or decimal type whose range it is in.
                if (value >= -TwoToThe63 && value < TwoToThe63) return FromInt64((long)value, targetType);
                if (value >= 0 && value < TwoToThe64) return FromUInt64((ulong)value, targetType);
                if (targetType == typeof(decimal)) return Math.Abs(value) < TwoToThe96 ? ToDecimal(value) : NoValue();
                return ToExactFloatingPoint(value, targetType);
            }

            // Only a whole number converts to an integer type.
            if (targetType != typeof(decimal) && targetType != typeof(float) && targetType != typeof(double))
            {
                return NoValue();
            }

            // A double that a decimal of at most 15 significant digits converts back to is written as that decimal.  A double below 2^96 in
            // magnitude is in the range of decimal.
            if (Math.Abs(value) < TwoToThe96)
            {
                var writtenValue = (decimal)value;
                if (ToDouble(writtenValue) == value)
                {
                    return FromWrittenValue(writtenValue, targetType);
                }
            }

            return ToExactFloatingPoint(value, targetType);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? FromDecimal(decimal value, Type targetType)
#else
        [return: MaybeNull]
        private static object FromDecimal(decimal value, [DisallowNull] Type targetType)
#endif
        {
            if (decimal.Truncate(value) == value)
            {
                if (value >= long.MinValue && value <= long.MaxValue) return FromInt64((long)value, targetType);
                if (value >= 0 && value <= ulong.MaxValue) return FromUInt64((ulong)value, targetType);
            }

            return FromWrittenValue(value, targetType);
        }

        /// <summary>
        /// Converts a number as it is written to a double or float that holds it as it is written.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? FromWrittenValue(decimal writtenValue, Type targetType)
#else
        [return: MaybeNull]
        private static object FromWrittenValue(decimal writtenValue, [DisallowNull] Type targetType)
#endif
        {
            if (targetType == typeof(decimal))
            {
                return writtenValue;
            }

            // A double or float holds the number if the decimal it converts to, of at most 15 or 7 significant digits, is the number.
            if (targetType == typeof(double))
            {
                var doubleValue = ToDouble(writtenValue);
                return (decimal)doubleValue == writtenValue ? doubleValue : NoValue();
            }

            if (targetType == typeof(float))
            {
                var floatValue = ToSingle(writtenValue);
                return (decimal)floatValue == writtenValue ? floatValue : NoValue();
            }

            return NoValue();
        }

        /// <summary>
        /// Converts a double to a double or float that holds it exactly.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? ToExactFloatingPoint(double value, Type targetType)
#else
        [return: MaybeNull]
        private static object ToExactFloatingPoint(double value, [DisallowNull] Type targetType)
#endif
        {
            if (targetType == typeof(double))
            {
                return value;
            }

            if (targetType == typeof(float))
            {
                var floatValue = (float)value;
                return double.IsNaN(value) || (double)floatValue == value ? floatValue : NoValue();
            }

            return NoValue();
        }

        /// <summary>
        /// Converts a whole double less than 2^96 in magnitude to the decimal of the same value.
        /// </summary>
        private static decimal ToDecimal(double wholeValue)
        {
            // A cast rounds to 15 significant digits, so the value is split into two parts that a ulong holds exactly.  Dividing by a power of two
            // and taking the floor are exact, and so is the remainder, which is a whole number below 2^64.
            var magnitude = Math.Abs(wholeValue);
            var high = Math.Floor(magnitude / TwoToThe64);
            var low = magnitude - high * TwoToThe64;
            var result = (decimal)(ulong)high * 18446744073709551616m + (ulong)low;
            return wholeValue < 0 ? -result : result;
        }

        /// <summary>
        /// Converts a decimal to the nearest double, and a tie to the one with the even significand.
        /// </summary>
        /// <remarks>
        /// A cast divides by a power of ten as a double, which isn't exact past 10^22, so it can give a neighbour of the nearest double.
        /// </remarks>
        internal static double ToDouble(decimal value) => ToNearestBinary(value, DoubleSignificandBits);

        /// <summary>
        /// Converts a decimal to the nearest float, and a tie to the one with the even significand.
        /// </summary>
        /// <remarks>
        /// A cast converts to a double first, so it can round twice.  The double <see cref="ToNearestBinary"/> gives has a significand of at most
        /// 24 bits and is within the range of float, so the float holds it exactly.
        /// </remarks>
        internal static float ToSingle(decimal value) => (float)ToNearestBinary(value, SingleSignificandBits);

        /// <summary>
        /// Rounds a decimal to the nearest number whose significand has at most <paramref name="significandBits"/> bits, and a tie to the one with
        /// the even significand, in exact integer arithmetic.
        /// </summary>
        /// <param name="value">The decimal.</param>
        /// <param name="significandBits">The bits of the significand, counting the leading bit: at most 53.</param>
        /// <returns>The rounded number, as the double that holds it exactly.</returns>
        private static double ToNearestBinary(decimal value, int significandBits)
        {
            // The decimal is numerator / 10^scale, whose numerator is the 96-bit integer of its first three parts.
            var bits = decimal.GetBits(value);
            var numerator = ((new BigInteger((uint)bits[2]) << 32 | (uint)bits[1]) << 32) | (uint)bits[0];
            var magnitude = numerator.IsZero
                ? 0d
                : RoundToBinary(numerator, BigInteger.Pow(10, (bits[DecimalFlagsIndex] >> DecimalScaleShift) & DecimalScaleMask), significandBits);
            return bits[DecimalFlagsIndex] < 0 ? -magnitude : magnitude;
        }

        /// <summary>
        /// Rounds a positive fraction to the nearest number whose significand has at most <paramref name="significandBits"/> bits, and a tie to the
        /// one with the even significand.
        /// </summary>
        /// <remarks>
        /// The fraction is a decimal's magnitude: at least 10^-28, which is above 2^-94, and below 2^96.  So the rounded number is a significand
        /// below 2^53 times 2^exponent, where exponent is between -94 - 52 = -146 and 96 - 23 = 73.  That is inside a double's normal range, so
        /// 2^exponent is built from its bits, and multiplying by it is exact.
        /// </remarks>
        private static double RoundToBinary(BigInteger numerator, BigInteger denominator, int significandBits)
        {
            // Scale by 2^shift so that the quotient has significandBits bits.  Shifting by the difference in bit lengths gives a quotient of either
            // significandBits or significandBits + 1 bits, and the second is shifted one less.
            var shift = significandBits - GetBitLength(numerator) + GetBitLength(denominator);
            var quotient = DivideScaled(numerator, denominator, shift, out var divisor, out var remainder);
            var limit = BigInteger.One << significandBits;
            if (quotient >= limit)
            {
                shift--;
                quotient = DivideScaled(numerator, denominator, shift, out divisor, out remainder);
            }

            // Round the quotient to the nearest whole number, and a tie to the even one.  Rounding up to 2^significandBits leaves a significand of
            // significandBits + 1 bits, which is halved exactly.
            var comparison = (remainder << 1).CompareTo(divisor);
            if (comparison > 0 || (comparison == 0 && !quotient.IsEven))
            {
                quotient++;
                if (quotient == limit)
                {
                    quotient >>= 1;
                    shift--;
                }
            }

            var powerOfTwo = BitConverter.Int64BitsToDouble((long)(DoubleExponentBias - shift) << (DoubleSignificandBits - 1));
            return (ulong)quotient * powerOfTwo;
        }

        // Divides numerator * 2^shift by the denominator.  A negative shift multiplies the denominator by 2^-shift instead.  The divisor is the
        // number divided by, of which the remainder is a fraction.
        private static BigInteger DivideScaled(BigInteger numerator, BigInteger denominator, int shift, out BigInteger divisor, out BigInteger remainder)
        {
            BigInteger dividend;
            if (shift >= 0)
            {
                dividend = numerator << shift;
                divisor = denominator;
            }
            else
            {
                dividend = numerator;
                divisor = denominator << -shift;
            }

            return BigInteger.DivRem(dividend, divisor, out remainder);
        }

        // The number of bits of a positive integer, up to and including its highest set bit.
        private static int GetBitLength(BigInteger value)
        {
#if NET5_0_OR_GREATER
            return (int)value.GetBitLength();
#else
            var bitLength = 0;
            while (value > ulong.MaxValue)
            {
                value >>= 64;
                bitLength += 64;
            }

            for (var top = (ulong)value; top != 0; top >>= 1)
            {
                bitLength++;
            }

            return bitLength;
#endif
        }

        private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        private static bool IsWholeNumber(double value) => Math.Floor(value) == value;

        // The result of a conversion that doesn't hold the number, typed as object so that a conditional with a value of a number type boxes it.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static object? NoValue() => null;
#else
        [return: MaybeNull]
        private static object NoValue() => null;
#endif
    }
}
