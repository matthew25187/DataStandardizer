﻿using System;
using System.Collections.Generic;
using System.Globalization;
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
                if (constantValue is sbyte sbyteValue) convertedValue = FromInt64(sbyteValue, targetType);
                else if (constantValue is short shortValue) convertedValue = FromInt64(shortValue, targetType);
                else if (constantValue is int intValue) convertedValue = FromInt64(intValue, targetType);
                else if (constantValue is long longValue) convertedValue = FromInt64(longValue, targetType);
                else if (constantValue is byte byteValue) convertedValue = FromInt64(byteValue, targetType);
                else if (constantValue is ushort ushortValue) convertedValue = FromInt64(ushortValue, targetType);
                else if (constantValue is uint uintValue) convertedValue = FromInt64(uintValue, targetType);
                else if (constantValue is ulong ulongValue) convertedValue = FromUInt64(ulongValue, targetType);
                else if (constantValue is float floatValue) convertedValue = FromFloat(floatValue, targetType);
                else if (constantValue is double doubleValue) convertedValue = FromDouble(doubleValue, targetType);
                else if (constantValue is decimal decimalValue) convertedValue = FromDecimal(decimalValue, targetType);
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
            var formattedValue = constantValue is IFormattable formattable ? formattable.ToString(null, CultureInfo.InvariantCulture) : constantValue.ToString();
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
                if ((float)writtenValue == value)
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
                if ((double)writtenValue == value)
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
                var doubleValue = (double)writtenValue;
                return (decimal)doubleValue == writtenValue ? doubleValue : NoValue();
            }

            if (targetType == typeof(float))
            {
                var floatValue = (float)writtenValue;
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
