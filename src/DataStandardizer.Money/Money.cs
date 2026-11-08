using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace DataStandardizer.Money
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public readonly struct Money : IComparable, IComparable<Money>, IEquatable<Money>, IFormattable
#if NETSTANDARD1_3_OR_GREATER||NET
        , IConvertible
#endif
#if NET7_0_OR_GREATER
        , ISpanFormattable, IParsable<Money>, ISpanParsable<Money>
#endif
    {
        private static class ErrorMessage
        {
            internal const string DifferentCurrenciesComparisonTemplate = "Unable to compare {0} values having different currencies.";
            internal const string ExpectedCurrencyCodeTemplate = "Expected a member of {0}.";
            internal const string ExpectedNationalCurrencyCode = "Expected a national currency code.";
            internal const string NoMinorUnitsTemplate = "The currency {0} has no minor units, so the value cannot be converted to {1}.";
            internal const string FractionalMinorUnitsTemplate = "The amount is not a whole number of minor units of {0}, so it cannot be converted to {1}.";
            internal const string MinorUnitsOverflowTemplate = "The amount in minor units of {0} is too large or too small for {1}.";
            internal const string UnsignedConversion = "Money converts only to signed integer types, which give the amount in ISO 4217 minor units. To truncate the amount to an unsigned integer, convert through decimal, e.g. (uint)(decimal)money.";
        }

        /// <summary>
        /// The outcome of scaling an amount to the minor units of its currency.
        /// </summary>
        private enum MinorUnitsScaling
        {
            Success,
            NoMinorUnits,
            Fractional,
            Overflow
        }

        /// <summary>
        /// Powers of ten, and the bounds of each integer type that an amount is scaled to, indexed by the
        /// number of digits in the minor unit of a currency.
        /// </summary>
        /// <remarks>
        /// Each bound is the limit of the integer type divided by the power of ten, so an amount within it
        /// scales to a value within the range of the type. They are built from the integer limit and the
        /// scale, so they are exact.
        /// </remarks>
        private static class MinorUnitsScale
        {
            /// <summary>
            /// The most digits a <see cref="decimal"/> can hold after its decimal point.
            /// </summary>
            internal const int MaxDigits = 28;

            internal static readonly decimal[] PowersOfTen = CreatePowersOfTen();

            internal static readonly decimal[] Int64Minimum = CreateBounds(long.MinValue);
            internal static readonly decimal[] Int64Maximum = CreateBounds(long.MaxValue);
            internal static readonly decimal[] Int32Minimum = CreateBounds(int.MinValue);
            internal static readonly decimal[] Int32Maximum = CreateBounds(int.MaxValue);
            internal static readonly decimal[] Int16Minimum = CreateBounds(short.MinValue);
            internal static readonly decimal[] Int16Maximum = CreateBounds(short.MaxValue);
            internal static readonly decimal[] SByteMinimum = CreateBounds(sbyte.MinValue);
            internal static readonly decimal[] SByteMaximum = CreateBounds(sbyte.MaxValue);

            private static decimal[] CreatePowersOfTen()
            {
                var result = new decimal[MaxDigits + 1];
                result[0] = 1m;
                for (var digits = 1; digits <= MaxDigits; digits++)
                {
                    result[digits] = result[digits - 1] * 10m;
                }

                return result;
            }

            private static decimal[] CreateBounds(long limit)
            {
                // The magnitude of long.MinValue isn't a long, so it is taken one less and incremented as a ulong.
                var isNegative = limit < 0;
                var magnitude = isNegative ? (ulong)(-(limit + 1)) + 1UL : (ulong)limit;
                var low = unchecked((int)(uint)magnitude);
                var middle = unchecked((int)(uint)(magnitude >> 32));
                var result = new decimal[MaxDigits + 1];
                for (var digits = 0; digits <= MaxDigits; digits++)
                {
                    result[digits] = new decimal(low, middle, 0, isNegative, (byte)digits);
                }

                return result;
            }
        }

        private const Iso4217CurrencyCurrent DefaultCurrency = Iso4217CurrencyCurrent.XXX;

        /// <summary>
        /// The currency code of a monetary value which carries no currency.
        /// </summary>
        internal const Iso4217CurrencyCurrent NoCurrency = DefaultCurrency;

        private readonly decimal _amount;
        private readonly Iso4217CurrencyCurrent? _currency;

#if NETCOREAPP3_0_OR_GREATER
        private Money(decimal amount)
#else
        private Money(decimal amount)
#endif
        {
            _amount = amount;
            _currency = null;
            RoundingPrecision = null;
            RoundingMethod = null;
        }

#if NETCOREAPP3_0_OR_GREATER
        private Money(decimal amount, Iso4217CurrencyCurrent currency)
#else
        private Money(decimal amount, Iso4217CurrencyCurrent currency)
#endif
            : this(amount)
        {
            _currency = currency;
        }

#if NETCOREAPP3_0_OR_GREATER
        private Money(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision)
#else
        private Money(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision)
#endif
            : this(amount, currency)
        {
            RoundingPrecision = roundingPrecision;
        }

#if NETCOREAPP3_0_OR_GREATER
        private Money(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision, MidpointRounding roundingMethod)
#else
        private Money(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision, MidpointRounding roundingMethod)
#endif
            : this(amount, currency, roundingPrecision)
        {
            RoundingMethod = roundingMethod;
        }

        public static implicit operator decimal(Money value)
        {
            var result = value._amount;
            if (value.RoundingPrecision.HasValue)
            {
                result = !value.RoundingMethod.HasValue
                    ? Math.Round(value._amount, value.RoundingPrecision.Value)
                    : Math.Round(value._amount, value.RoundingPrecision.Value, value.RoundingMethod.Value);
            }

            return result;
        }

        public static implicit operator Money(decimal value)
        {
            return new Money(value);
        }

        /// <summary>
        /// Converts a monetary value to its amount in the minor units of its currency, as a 64-bit signed integer.
        /// </summary>
        /// <param name="value">The monetary value to convert.</param>
        /// <returns>
        /// The amount of <paramref name="value"/> multiplied by ten to the power of the number of minor-unit
        /// digits ISO 4217 gives its currency. For example 12.34 USD gives 1234, 500 JPY gives 500 and 1.234 BHD
        /// gives 1234.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The amount converted is the rounded amount, <c>(decimal)value</c>, with the
        /// <see cref="RoundingPrecision"/> and <see cref="RoundingMethod"/> of <paramref name="value"/> applied.
        /// The amount is never truncated or rounded to make it a whole number of minor units.
        /// </para>
        /// <para>
        /// This differs from the other numeric conversions, which don't scale the amount. <c>(decimal)value</c>,
        /// <c>(double)value</c> and <c>(float)value</c> give the rounded amount. <c>Convert.ToInt64(value)</c> binds to
        /// <see cref="Convert.ToInt64(decimal)"/> and converts the rounded amount, while the <see cref="IConvertible"/>
        /// members, which <see cref="Convert.ToInt64(object)"/> calls, convert the amount before rounding. Both round
        /// a fraction to the nearest integer, with ties to even. To truncate the rounded amount to an integer,
        /// convert through <see cref="decimal"/>, as in <c>(long)(decimal)value</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidCastException">
        /// The currency of <paramref name="value"/> has no minor units in ISO 4217, such as <c>XXX</c> (including
        /// <c>default(Money)</c>) or <c>XTS</c>.
        /// -or-
        /// The amount of <paramref name="value"/> is not a whole number of minor units, such as 1.005 USD.
        /// </exception>
        /// <exception cref="OverflowException">The amount in minor units is less than <see cref="long.MinValue"/> or greater than <see cref="long.MaxValue"/>.</exception>
        public static explicit operator long(Money value)
        {
            return ConvertToMinorUnits(value, MinorUnitsScale.Int64Minimum, MinorUnitsScale.Int64Maximum, nameof(Int64));
        }

        /// <summary>
        /// Converts a monetary value to its amount in the minor units of its currency, as a 32-bit signed integer.
        /// </summary>
        /// <param name="value">The monetary value to convert.</param>
        /// <returns>
        /// The amount of <paramref name="value"/> multiplied by ten to the power of the number of minor-unit
        /// digits ISO 4217 gives its currency. For example 12.34 USD gives 1234, 500 JPY gives 500 and 1.234 BHD
        /// gives 1234.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The amount converted is the rounded amount, <c>(decimal)value</c>, with the
        /// <see cref="RoundingPrecision"/> and <see cref="RoundingMethod"/> of <paramref name="value"/> applied.
        /// The amount is never truncated or rounded to make it a whole number of minor units.
        /// </para>
        /// <para>
        /// This differs from the other numeric conversions, which don't scale the amount. <c>(decimal)value</c>,
        /// <c>(double)value</c> and <c>(float)value</c> give the rounded amount. <c>Convert.ToInt32(value)</c> binds to
        /// <see cref="Convert.ToInt32(decimal)"/> and converts the rounded amount, while the <see cref="IConvertible"/>
        /// members, which <see cref="Convert.ToInt32(object)"/> calls, convert the amount before rounding. Both round
        /// a fraction to the nearest integer, with ties to even. To truncate the rounded amount to an integer,
        /// convert through <see cref="decimal"/>, as in <c>(int)(decimal)value</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidCastException">
        /// The currency of <paramref name="value"/> has no minor units in ISO 4217, such as <c>XXX</c> (including
        /// <c>default(Money)</c>) or <c>XTS</c>.
        /// -or-
        /// The amount of <paramref name="value"/> is not a whole number of minor units, such as 1.005 USD.
        /// </exception>
        /// <exception cref="OverflowException">The amount in minor units is less than <see cref="int.MinValue"/> or greater than <see cref="int.MaxValue"/>.</exception>
        public static explicit operator int(Money value)
        {
            return (int)ConvertToMinorUnits(value, MinorUnitsScale.Int32Minimum, MinorUnitsScale.Int32Maximum, nameof(Int32));
        }

        /// <summary>
        /// Converts a monetary value to its amount in the minor units of its currency, as a 16-bit signed integer.
        /// </summary>
        /// <param name="value">The monetary value to convert.</param>
        /// <returns>
        /// The amount of <paramref name="value"/> multiplied by ten to the power of the number of minor-unit
        /// digits ISO 4217 gives its currency. For example 12.34 USD gives 1234, 500 JPY gives 500 and 1.234 BHD
        /// gives 1234.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The amount converted is the rounded amount, <c>(decimal)value</c>, with the
        /// <see cref="RoundingPrecision"/> and <see cref="RoundingMethod"/> of <paramref name="value"/> applied.
        /// The amount is never truncated or rounded to make it a whole number of minor units.
        /// </para>
        /// <para>
        /// This differs from the other numeric conversions, which don't scale the amount. <c>(decimal)value</c>,
        /// <c>(double)value</c> and <c>(float)value</c> give the rounded amount. <c>Convert.ToInt16(value)</c> binds to
        /// <see cref="Convert.ToInt16(decimal)"/> and converts the rounded amount, while the <see cref="IConvertible"/>
        /// members, which <see cref="Convert.ToInt16(object)"/> calls, convert the amount before rounding. Both round
        /// a fraction to the nearest integer, with ties to even. To truncate the rounded amount to an integer,
        /// convert through <see cref="decimal"/>, as in <c>(short)(decimal)value</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidCastException">
        /// The currency of <paramref name="value"/> has no minor units in ISO 4217, such as <c>XXX</c> (including
        /// <c>default(Money)</c>) or <c>XTS</c>.
        /// -or-
        /// The amount of <paramref name="value"/> is not a whole number of minor units, such as 1.005 USD.
        /// </exception>
        /// <exception cref="OverflowException">The amount in minor units is less than <see cref="short.MinValue"/> or greater than <see cref="short.MaxValue"/>.</exception>
        public static explicit operator short(Money value)
        {
            return (short)ConvertToMinorUnits(value, MinorUnitsScale.Int16Minimum, MinorUnitsScale.Int16Maximum, nameof(Int16));
        }

        /// <summary>
        /// Converts a monetary value to its amount in the minor units of its currency, as an 8-bit signed integer.
        /// </summary>
        /// <param name="value">The monetary value to convert.</param>
        /// <returns>
        /// The amount of <paramref name="value"/> multiplied by ten to the power of the number of minor-unit
        /// digits ISO 4217 gives its currency. For example 1.23 USD gives 123 and 100 JPY gives 100.
        /// </returns>
        /// <remarks>
        /// <para>
        /// The amount converted is the rounded amount, <c>(decimal)value</c>, with the
        /// <see cref="RoundingPrecision"/> and <see cref="RoundingMethod"/> of <paramref name="value"/> applied.
        /// The amount is never truncated or rounded to make it a whole number of minor units.
        /// </para>
        /// <para>
        /// This differs from the other numeric conversions, which don't scale the amount. <c>(decimal)value</c>,
        /// <c>(double)value</c> and <c>(float)value</c> give the rounded amount. <c>Convert.ToSByte(value)</c> binds to
        /// <see cref="Convert.ToSByte(decimal)"/> and converts the rounded amount, while the <see cref="IConvertible"/>
        /// members, which <see cref="Convert.ToSByte(object)"/> calls, convert the amount before rounding. Both round
        /// a fraction to the nearest integer, with ties to even. To truncate the rounded amount to an integer,
        /// convert through <see cref="decimal"/>, as in <c>(sbyte)(decimal)value</c>.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidCastException">
        /// The currency of <paramref name="value"/> has no minor units in ISO 4217, such as <c>XXX</c> (including
        /// <c>default(Money)</c>) or <c>XTS</c>.
        /// -or-
        /// The amount of <paramref name="value"/> is not a whole number of minor units, such as 1.005 USD.
        /// </exception>
        /// <exception cref="OverflowException">The amount in minor units is less than <see cref="sbyte.MinValue"/> or greater than <see cref="sbyte.MaxValue"/>.</exception>
        public static explicit operator sbyte(Money value)
        {
            return (sbyte)ConvertToMinorUnits(value, MinorUnitsScale.SByteMinimum, MinorUnitsScale.SByteMaximum, nameof(SByte));
        }

        /// <summary>
        /// A monetary value doesn't convert to an unsigned integer. Use <c>(ulong)(decimal)value</c> to truncate its amount.
        /// </summary>
        /// <param name="value">The monetary value.</param>
        /// <returns>This conversion never returns.</returns>
        /// <remarks>
        /// This operator exists so that <c>(ulong)value</c> fails to compile, rather than truncating the amount
        /// while the signed integer conversions give it in minor units.
        /// </remarks>
        /// <exception cref="InvalidCastException">Always thrown, when this operator is reached other than through compiled C#.</exception>
        [Obsolete(ErrorMessage.UnsignedConversion, true)]
        public static explicit operator ulong(Money value)
        {
            throw new InvalidCastException(ErrorMessage.UnsignedConversion);
        }

        /// <summary>
        /// A monetary value doesn't convert to an unsigned integer. Use <c>(uint)(decimal)value</c> to truncate its amount.
        /// </summary>
        /// <param name="value">The monetary value.</param>
        /// <returns>This conversion never returns.</returns>
        /// <remarks>
        /// This operator exists so that <c>(uint)value</c> fails to compile, rather than truncating the amount
        /// while the signed integer conversions give it in minor units.
        /// </remarks>
        /// <exception cref="InvalidCastException">Always thrown, when this operator is reached other than through compiled C#.</exception>
        [Obsolete(ErrorMessage.UnsignedConversion, true)]
        public static explicit operator uint(Money value)
        {
            throw new InvalidCastException(ErrorMessage.UnsignedConversion);
        }

        /// <summary>
        /// A monetary value doesn't convert to an unsigned integer. Use <c>(ushort)(decimal)value</c> to truncate its amount.
        /// </summary>
        /// <param name="value">The monetary value.</param>
        /// <returns>This conversion never returns.</returns>
        /// <remarks>
        /// This operator exists so that <c>(ushort)value</c> fails to compile, rather than truncating the amount
        /// while the signed integer conversions give it in minor units.
        /// </remarks>
        /// <exception cref="InvalidCastException">Always thrown, when this operator is reached other than through compiled C#.</exception>
        [Obsolete(ErrorMessage.UnsignedConversion, true)]
        public static explicit operator ushort(Money value)
        {
            throw new InvalidCastException(ErrorMessage.UnsignedConversion);
        }

        /// <summary>
        /// A monetary value doesn't convert to an unsigned integer. Use <c>(byte)(decimal)value</c> to truncate its amount.
        /// </summary>
        /// <param name="value">The monetary value.</param>
        /// <returns>This conversion never returns.</returns>
        /// <remarks>
        /// This operator exists so that <c>(byte)value</c> fails to compile, rather than truncating the amount
        /// while the signed integer conversions give it in minor units.
        /// </remarks>
        /// <exception cref="InvalidCastException">Always thrown, when this operator is reached other than through compiled C#.</exception>
        [Obsolete(ErrorMessage.UnsignedConversion, true)]
        public static explicit operator byte(Money value)
        {
            throw new InvalidCastException(ErrorMessage.UnsignedConversion);
        }

        /// <summary>
        /// Converts the amount of a monetary value to a double-precision floating-point number.
        /// </summary>
        /// <param name="value">The monetary value to convert.</param>
        /// <returns>The rounded amount, <c>(decimal)value</c>, as a <see cref="double"/>.</returns>
        /// <remarks>
        /// This gives the amount itself, not the amount in minor units that the signed integer conversions
        /// give. It's declared so that <c>(double)value</c> keeps converting through <see cref="decimal"/>.
        /// </remarks>
        public static explicit operator double(Money value)
        {
            return (double)(decimal)value;
        }

        /// <summary>
        /// Converts the amount of a monetary value to a single-precision floating-point number.
        /// </summary>
        /// <param name="value">The monetary value to convert.</param>
        /// <returns>The rounded amount, <c>(decimal)value</c>, as a <see cref="float"/>.</returns>
        /// <remarks>
        /// This gives the amount itself, not the amount in minor units that the signed integer conversions
        /// give. It's declared so that <c>(float)value</c> keeps converting through <see cref="decimal"/>.
        /// </remarks>
        public static explicit operator float(Money value)
        {
            return (float)(decimal)value;
        }

        /// <summary>
        /// Convert a monetary value to its amount in minor units, or throw the exception a cast throws.
        /// </summary>
        /// <param name="value">The monetary value to convert.</param>
        /// <param name="minimum">The least amount, for each number of minor-unit digits, that is within range of the target type.</param>
        /// <param name="maximum">The greatest amount, for each number of minor-unit digits, that is within range of the target type.</param>
        /// <param name="targetTypeName">Name of the target type, for the exception message.</param>
        /// <returns>The amount in minor units, which is within range of the target type.</returns>
        private static long ConvertToMinorUnits(Money value, decimal[] minimum, decimal[] maximum, string targetTypeName)
        {
            switch (TryScale((decimal)value, value.CurrencyMinorUnits, minimum, maximum, out var result))
            {
                case MinorUnitsScaling.Success:
                    return result;
                case MinorUnitsScaling.NoMinorUnits:
                    throw new InvalidCastException(string.Format(ErrorMessage.NoMinorUnitsTemplate, value.IsoCurrencyCode, targetTypeName));
                case MinorUnitsScaling.Fractional:
                    throw new InvalidCastException(string.Format(ErrorMessage.FractionalMinorUnitsTemplate, value.IsoCurrencyCode, targetTypeName));
                default:
                    throw new OverflowException(string.Format(ErrorMessage.MinorUnitsOverflowTemplate, value.IsoCurrencyCode, targetTypeName));
            }
        }

        /// <summary>
        /// Scale an amount to the minor units of its currency.
        /// </summary>
        /// <param name="roundedAmount">The amount to scale, with the rounding of the monetary value already applied, as <c>(decimal)value</c> gives it.</param>
        /// <param name="digits">The number of minor-unit digits of the currency, or <c>null</c> if it has no minor units.</param>
        /// <param name="minimum">The least amount, for each number of minor-unit digits, that is within range of the target type.</param>
        /// <param name="maximum">The greatest amount, for each number of minor-unit digits, that is within range of the target type.</param>
        /// <param name="result">When this method returns, the amount in minor units if it was scaled; otherwise zero.</param>
        /// <returns>Whether the amount was scaled, or why it wasn't.</returns>
        /// <remarks>
        /// An amount that isn't a whole number of minor units is reported as fractional whatever its size, so
        /// the outcome doesn't depend on the target type. The range is checked before the amount is multiplied,
        /// so the multiplication can't overflow and is exact, and the result is within range of the target type.
        /// </remarks>
        private static MinorUnitsScaling TryScale(decimal roundedAmount, byte? digits, decimal[] minimum, decimal[] maximum, out long result)
        {
            result = 0;

            if (!digits.HasValue)
            {
                return MinorUnitsScaling.NoMinorUnits;
            }

            var useDigits = digits.Value;
            if (useDigits > MinorUnitsScale.MaxDigits)
            {
                // A decimal can't hold the power of ten to scale by. No ISO 4217 currency has this many
                // minor-unit digits.
                return MinorUnitsScaling.Overflow;
            }

            if (Math.Round(roundedAmount, useDigits) != roundedAmount)
            {
                return MinorUnitsScaling.Fractional;
            }

            if (roundedAmount < minimum[useDigits] || roundedAmount > maximum[useDigits])
            {
                return MinorUnitsScaling.Overflow;
            }

            var scaled = roundedAmount * MinorUnitsScale.PowersOfTen[useDigits];
            result = decimal.ToInt64(scaled);
            return MinorUnitsScaling.Success;
        }

        public static Money operator +(Money moneyValue, decimal decimalValue)
        {
            var result = decimal.Add(moneyValue._amount, decimalValue);
            if (moneyValue.RoundingPrecision.HasValue && moneyValue.RoundingMethod.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value, moneyValue.RoundingMethod.Value);
            }

            if (moneyValue.RoundingPrecision.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value);
            }

            if (moneyValue.IsoCurrencyCode != DefaultCurrency)
            {
                return new Money(result, moneyValue.IsoCurrencyCode);
            }

            return new Money(result);
        }

        public static Money operator -(Money moneyValue, decimal decimalValue)
        {
            var result = decimal.Subtract(moneyValue._amount, decimalValue);
            if (moneyValue.RoundingPrecision.HasValue && moneyValue.RoundingMethod.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value, moneyValue.RoundingMethod.Value);
            }

            if (moneyValue.RoundingPrecision.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value);
            }

            if (moneyValue.IsoCurrencyCode != DefaultCurrency)
            {
                return new Money(result, moneyValue.IsoCurrencyCode);
            }

            return new Money(result);
        }

        public static Money operator *(Money moneyValue, decimal decimalValue)
        {
            var result = decimal.Multiply(moneyValue._amount, decimalValue);
            if (moneyValue.RoundingPrecision.HasValue && moneyValue.RoundingMethod.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value, moneyValue.RoundingMethod.Value);
            }

            if (moneyValue.RoundingPrecision.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value);
            }

            if (moneyValue.IsoCurrencyCode != DefaultCurrency)
            {
                return new Money(result, moneyValue.IsoCurrencyCode);
            }

            return new Money(result);
        }

        public static Money operator /(Money moneyValue, decimal decimalValue)
        {
            var result = decimal.Divide(moneyValue._amount, decimalValue);
            if (moneyValue.RoundingPrecision.HasValue && moneyValue.RoundingMethod.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value, moneyValue.RoundingMethod.Value);
            }

            if (moneyValue.RoundingPrecision.HasValue)
            {
                return new Money(result, moneyValue.IsoCurrencyCode, moneyValue.RoundingPrecision.Value);
            }

            if (moneyValue.IsoCurrencyCode != DefaultCurrency)
            {
                return new Money(result, moneyValue.IsoCurrencyCode);
            }

            return new Money(result);
        }

        /// <summary>
        /// Determines whether two monetary values are equal.
        /// </summary>
        /// <param name="left">The first monetary value to compare.</param>
        /// <param name="right">The second monetary value to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> have the same amount and the same
        /// currency; otherwise <c>false</c>. This is what <see cref="Equals(Money)"/> returns.
        /// </returns>
        /// <remarks>
        /// Values in different currencies are not equal, so this operator returns <c>false</c> for them rather
        /// than throwing, unlike the relational operators.
        /// </remarks>
        public static bool operator ==(Money left, Money right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two monetary values are not equal.
        /// </summary>
        /// <param name="left">The first monetary value to compare.</param>
        /// <param name="right">The second monetary value to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="left"/> and <paramref name="right"/> differ in amount or currency;
        /// otherwise <c>false</c>. This is the negation of what <see cref="Equals(Money)"/> returns.
        /// </returns>
        /// <remarks>
        /// Values in different currencies are not equal, so this operator returns <c>true</c> for them rather
        /// than throwing, unlike the relational operators.
        /// </remarks>
        public static bool operator !=(Money left, Money right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Determines whether one monetary value is less than another in the same currency.
        /// </summary>
        /// <param name="left">The first monetary value to compare.</param>
        /// <param name="right">The second monetary value to compare.</param>
        /// <returns><c>true</c> if the amount of <paramref name="left"/> is less than the amount of <paramref name="right"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="left"/> and <paramref name="right"/> have different currencies, as
        /// <see cref="IsoCurrencyCode"/> reports them. A value with no currency, such as <c>default(Money)</c>,
        /// has the currency <c>XXX</c>.
        /// </exception>
        public static bool operator <(Money left, Money right)
        {
            ThrowIfCurrenciesDiffer(left, right);

            return left._amount < right._amount;
        }

        /// <summary>
        /// Determines whether one monetary value is greater than another in the same currency.
        /// </summary>
        /// <param name="left">The first monetary value to compare.</param>
        /// <param name="right">The second monetary value to compare.</param>
        /// <returns><c>true</c> if the amount of <paramref name="left"/> is greater than the amount of <paramref name="right"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="left"/> and <paramref name="right"/> have different currencies, as
        /// <see cref="IsoCurrencyCode"/> reports them. A value with no currency, such as <c>default(Money)</c>,
        /// has the currency <c>XXX</c>.
        /// </exception>
        public static bool operator >(Money left, Money right)
        {
            ThrowIfCurrenciesDiffer(left, right);

            return left._amount > right._amount;
        }

        /// <summary>
        /// Determines whether one monetary value is less than or equal to another in the same currency.
        /// </summary>
        /// <param name="left">The first monetary value to compare.</param>
        /// <param name="right">The second monetary value to compare.</param>
        /// <returns><c>true</c> if the amount of <paramref name="left"/> is less than or equal to the amount of <paramref name="right"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="left"/> and <paramref name="right"/> have different currencies, as
        /// <see cref="IsoCurrencyCode"/> reports them. A value with no currency, such as <c>default(Money)</c>,
        /// has the currency <c>XXX</c>.
        /// </exception>
        public static bool operator <=(Money left, Money right)
        {
            ThrowIfCurrenciesDiffer(left, right);

            return left._amount <= right._amount;
        }

        /// <summary>
        /// Determines whether one monetary value is greater than or equal to another in the same currency.
        /// </summary>
        /// <param name="left">The first monetary value to compare.</param>
        /// <param name="right">The second monetary value to compare.</param>
        /// <returns><c>true</c> if the amount of <paramref name="left"/> is greater than or equal to the amount of <paramref name="right"/>; otherwise <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="left"/> and <paramref name="right"/> have different currencies, as
        /// <see cref="IsoCurrencyCode"/> reports them. A value with no currency, such as <c>default(Money)</c>,
        /// has the currency <c>XXX</c>.
        /// </exception>
        public static bool operator >=(Money left, Money right)
        {
            ThrowIfCurrenciesDiffer(left, right);

            return left._amount >= right._amount;
        }

        /// <summary>
        /// Gets the number of digits used for the minor units of the currency.
        /// </summary>
        public byte? CurrencyMinorUnits => _currency?.GetMinorUnits();

        /// <summary>
        /// Gets the ISO 4217 currency code for the current value.
        /// </summary>
        public Iso4217CurrencyCurrent IsoCurrencyCode => _currency ?? DefaultCurrency;

        /// <summary>
        /// Gets the method of rounding applied to the amount.
        /// </summary>
        public MidpointRounding? RoundingMethod { get; }

        /// <summary>
        /// Gets the number of digits of precision to which the current value will be rounded.
        /// </summary>
        public int? RoundingPrecision { get; }
#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Compares this monetary value with an object, which must be a monetary value in the same currency.
        /// </summary>
        /// <param name="obj">The object to compare with this instance, or <c>null</c>.</param>
        /// <returns>
        /// A negative number if the amount of this instance is less than the amount of <paramref name="obj"/>,
        /// zero if they are equal, or a positive number if it is greater or <paramref name="obj"/> is <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="obj"/> is not a <see cref="Money"/>.
        /// -or-
        /// <paramref name="obj"/> is a <see cref="Money"/> with a different currency from this instance.
        /// </exception>
        public int CompareTo(object? obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is Money other)
            {
                return CompareTo(other, nameof(obj));
            }

            throw new ArgumentException($"{nameof(obj)} and this instance are not the same type.", nameof(obj));
        }

#else
        /// <summary>
        /// Compares this monetary value with an object, which must be a monetary value in the same currency.
        /// </summary>
        /// <param name="obj">The object to compare with this instance, or <c>null</c>.</param>
        /// <returns>
        /// A negative number if the amount of this instance is less than the amount of <paramref name="obj"/>,
        /// zero if they are equal, or a positive number if it is greater or <paramref name="obj"/> is <c>null</c>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="obj"/> is not a <see cref="Money"/>.
        /// -or-
        /// <paramref name="obj"/> is a <see cref="Money"/> with a different currency from this instance.
        /// </exception>
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is Money other)
            {
                return CompareTo(other, nameof(obj));
            }

            throw new ArgumentException($"{nameof(obj)} and this instance are not the same type.", nameof(obj));
        }
#endif

        /// <summary>
        /// Compares this monetary value with another in the same currency.
        /// </summary>
        /// <param name="other">The monetary value to compare with this instance.</param>
        /// <returns>
        /// A negative number if the amount of this instance is less than the amount of <paramref name="other"/>,
        /// zero if they are equal, or a positive number if it is greater.
        /// </returns>
        /// <remarks>
        /// Amounts in different currencies have no order, so this method throws for them, as the relational
        /// operators do. Sorting a collection that mixes currencies, for example with
        /// <see cref="Array.Sort(Array)"/> or LINQ's <c>OrderBy</c>, therefore fails rather than ordering the
        /// values by amount alone.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// <paramref name="other"/> has a different currency from this instance, as <see cref="IsoCurrencyCode"/>
        /// reports them. A value with no currency, such as <c>default(Money)</c>, has the currency <c>XXX</c>.
        /// </exception>
        public int CompareTo(Money other)
        {
            return CompareTo(other, nameof(other));
        }

        private int CompareTo(Money other, string paramName)
        {
            if (!HaveSameCurrency(this, other))
            {
                throw new ArgumentException(DifferentCurrenciesComparisonMessage, paramName);
            }

            return _amount.CompareTo(other._amount);
        }

        /// <summary>
        /// The message of the exception thrown when monetary values in different currencies are ordered.
        /// </summary>
        private static string DifferentCurrenciesComparisonMessage =>
            string.Format(ErrorMessage.DifferentCurrenciesComparisonTemplate, nameof(Money));

        /// <summary>
        /// Determine whether two monetary values have the same currency, counting a value with no currency as <c>XXX</c>.
        /// </summary>
        /// <param name="left">The first monetary value.</param>
        /// <param name="right">The second monetary value.</param>
        /// <returns><c>true</c> if <see cref="IsoCurrencyCode"/> is the same for both values; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Ordering and equality both use this rule, so they agree on which values share a currency.
        /// </remarks>
        private static bool HaveSameCurrency(Money left, Money right)
        {
            return left.IsoCurrencyCode == right.IsoCurrencyCode;
        }

        /// <summary>
        /// Throw the exception the relational operators throw when two monetary values have different currencies.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <exception cref="InvalidOperationException">The operands have different currencies.</exception>
        private static void ThrowIfCurrenciesDiffer(Money left, Money right)
        {
            if (!HaveSameCurrency(left, right))
            {
                throw new InvalidOperationException(DifferentCurrenciesComparisonMessage);
            }
        }

        /// <summary>
        /// Creates a monetary value with amount only.
        /// </summary>
        /// <param name="amount">Amount of the monetary value.</param>
        /// <returns>A monetary value whose amount is <paramref name="amount"/>.</returns>
        public static Money Create(decimal amount)
        {
            return new Money(amount);
        }

        /// <summary>
        /// Creates a monetary value with amount and currency.
        /// </summary>
        /// <param name="amount">Amount of the monetary value.</param>
        /// <param name="currency">Currency of the monetary value.</param>
        /// <returns>A monetary value whose amount is <paramref name="amount"/> and currency is <paramref name="currency"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="currency"/> is not a currency code.
        /// -or-
        /// <paramref name="currency"/> is not a national or supranational currency code, or the <see cref="Iso4217CurrencyCurrent.XTS"/> testing code. A funds code, such as <see cref="Iso4217CurrencyCurrent.CLF"/>, is not a currency code.
        /// </exception>
        public static Money Create(decimal amount, Iso4217CurrencyCurrent currency)
        {
            if (!Enum.IsDefined(currency.GetType(), currency))
                throw new ArgumentException(string.Format(ErrorMessage.ExpectedCurrencyCodeTemplate, currency.GetType().Name), nameof(currency));

            if (!IsValidCurrencyCodeForMoneyValue(currency))
                throw new ArgumentException(ErrorMessage.ExpectedNationalCurrencyCode, nameof(currency));

            return new Money(amount, currency);
        }

        /// <summary>
        /// Creates a monetary value with amount, currency, and rounding.
        /// </summary>
        /// <param name="amount">Amount of the monetary value.</param>
        /// <param name="currency">Currency of the monetary value.</param>
        /// <param name="roundingPrecision">Extent to which the amount will be rounded on conversion to a <see cref="decimal"/> value.</param>
        /// <returns>A monetary value with rounding whose amount is <paramref name="amount"/> and currency is <paramref name="currency"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="currency"/> is not a currency code.
        /// -or-
        /// <paramref name="currency"/> is not a national or supranational currency code, or the <see cref="Iso4217CurrencyCurrent.XTS"/> testing code. A funds code, such as <see cref="Iso4217CurrencyCurrent.CLF"/>, is not a currency code.
        /// </exception>
        public static Money Create(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision)
        {
            if (!Enum.IsDefined(currency.GetType(), currency))
                throw new ArgumentException(string.Format(ErrorMessage.ExpectedCurrencyCodeTemplate, currency.GetType().Name), nameof(currency));

            if (!IsValidCurrencyCodeForMoneyValue(currency))
                throw new ArgumentException(ErrorMessage.ExpectedNationalCurrencyCode, nameof(currency));

            return new Money(amount, currency, roundingPrecision);
        }

        /// <summary>
        /// Creates a monetary value with amount, currency, and rounding.
        /// </summary>
        /// <param name="amount">Amount of the monetary value.</param>
        /// <param name="currency">Currency of the monetary value.</param>
        /// <param name="roundingPrecision">Extent to which the amount will be rounded on conversion to a <see cref="decimal"/> value.</param>
        /// <param name="roundingMethod">Method used to apply rounding to the amount.</param>
        /// <returns>A monetary value with rounding whose amount is <paramref name="amount"/> and currency is <paramref name="currency"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="currency"/> is not a currency code.
        /// -or-
        /// <paramref name="currency"/> is not a national or supranational currency code, or the <see cref="Iso4217CurrencyCurrent.XTS"/> testing code. A funds code, such as <see cref="Iso4217CurrencyCurrent.CLF"/>, is not a currency code.
        /// </exception>
        public static Money Create(decimal amount, Iso4217CurrencyCurrent currency, int roundingPrecision, MidpointRounding roundingMethod)
        {
            if (!Enum.IsDefined(currency.GetType(), currency))
                throw new ArgumentException(string.Format(ErrorMessage.ExpectedCurrencyCodeTemplate, currency.GetType().Name), nameof(currency));

            if (!IsValidCurrencyCodeForMoneyValue(currency))
                throw new ArgumentException(ErrorMessage.ExpectedNationalCurrencyCode, nameof(currency));

            return new Money(amount, currency, roundingPrecision, roundingMethod);
        }
#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Determines whether this monetary value is equal to an object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance, or <c>null</c>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="Money"/> equal to this instance, as
        /// <see cref="Equals(Money)"/> decides; otherwise <c>false</c>.
        /// </returns>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is Money other && Equals(other);
        }
#else
        /// <summary>
        /// Determines whether this monetary value is equal to an object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance, or <c>null</c>.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="Money"/> equal to this instance, as
        /// <see cref="Equals(Money)"/> decides; otherwise <c>false</c>.
        /// </returns>
        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return obj is Money other && Equals(other);
        }
#endif

        /// <summary>
        /// Determines whether this monetary value is equal to another.
        /// </summary>
        /// <param name="other">The monetary value to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="other"/> has the same amount and the same currency as this instance;
        /// otherwise <c>false</c>.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Values in different currencies are not equal, and this method never throws for them. A value with no
        /// currency, such as <c>default(Money)</c>, has the currency <c>XXX</c>, so it is not equal to a value in
        /// any other currency. The <c>==</c> and <c>!=</c> operators give the same answer.
        /// </para>
        /// <para>
        /// The amounts are compared before rounding, and <see cref="RoundingPrecision"/> and
        /// <see cref="RoundingMethod"/> are ignored, so 1.004 USD rounded to two digits is not equal to 1.00 USD.
        /// Amounts that differ only in trailing zeros, such as 1.0 and 1.00, are equal.
        /// </para>
        /// </remarks>
        public bool Equals(Money other)
        {
            return _amount == other._amount && HaveSameCurrency(this, other);
        }

        /// <summary>
        /// Returns the hash code for this monetary value.
        /// </summary>
        /// <returns>
        /// A hash code computed from the amount and the currency, so values that <see cref="Equals(Money)"/>
        /// finds equal have the same hash code.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (_amount.GetHashCode() * 397) ^ IsoCurrencyCode.GetHashCode();
            }
        }

        /// <summary>
        /// Converts the string representation of a number to its <see cref="Money"/> equivalent.
        /// </summary>
        /// <param name="s">The string representation of the number to convert.</param>
        /// <returns>The <see cref="Money"/> number equivalent to the number contained in <paramref name="s"/>.</returns>
        public static Money Parse(string s)
        {
            return Parse(s, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Converts the string representation of a number to its <see cref="Money"/> equivalent using the specified culture-specific format information.
        /// </summary>
        /// <param name="s">The string representation of the number to convert.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific parsing information about <paramref name="s"/>.</param>
        /// <returns>The <see cref="Money"/> number equivalent to the number contained in <paramref name="s"/> as specified by <paramref name="provider"/>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static Money Parse(string s, IFormatProvider? provider)
#else
        public static Money Parse(string s, IFormatProvider provider)
#endif
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            return Parse(s, MoneyStyles.Currency, provider ?? CultureInfo.CurrentCulture);
        }

        public override string ToString()
        {
            return _amount.ToString();
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation using the specified culture-specific format information.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="provider"/>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public string ToString(IFormatProvider? provider)
#else
        public string ToString(IFormatProvider provider)
#endif
        {
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            return _amount.ToString(provider);
        }

        /// <summary>
        /// Converts the numeric value of this instance to its equivalent string representation, using the specified format.
        /// </summary>
        /// <param name="format">A standard or custom numeric format string.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format"/>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public string ToString(string? format)
#else
        public string ToString(string format)
#endif
        {
            return ToString(format, null);
        }

        /// <summary>
        /// Converts the value of this instance to its equivalent string representation using the specified
        /// format and culture-specific formatting information.
        /// </summary>
        /// <param name="format">A standard money format string, or a custom numeric format string.</param>
        /// <param name="formatProvider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format"/> and <paramref name="formatProvider"/>.</returns>
        /// <remarks>
        /// <para>
        /// The supported format specifiers are <c>C</c> for the currency symbol, <c>H</c> for the narrow
        /// currency symbol, <c>I</c> for the ISO 4217 currency code, <c>N</c> for the currency name and
        /// <c>G</c> for the amount alone. Each may be followed by a precision; where none is given the minor
        /// units of the currency are used. A format string consisting of an ISO 4217 currency code emits the
        /// code, and asserts that it is the currency of this value.
        /// </para>
        /// <para>
        /// Only the presentation of the value is taken from <paramref name="formatProvider"/>. The currency
        /// denoted, and the default precision, come from the value itself.
        /// </para>
        /// <para>
        /// <paramref name="formatProvider"/> may be a <see cref="MoneyInfo"/>, a <see cref="CurrencyFormatInfo"/>,
        /// a <see cref="CultureInfo"/> or a <see cref="NumberFormatInfo"/>. The general format and custom numeric
        /// format strings are applied to the rounded amount with the number formatting of that provider: of
        /// the culture for a <see cref="CultureInfo"/> or <see cref="MoneyInfo"/>, and of the separators of a
        /// <see cref="CurrencyFormatInfo"/>. Where it is <c>null</c> or supplies neither, the culture of the
        /// current thread is used, or the invariant culture on .NET Standard 1.1.
        /// </para>
        /// </remarks>
        /// <exception cref="FormatException">
        /// Thrown when <paramref name="format"/> is not a valid format string, or names a currency other than
        /// that of this value.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public string ToString(string? format, IFormatProvider? formatProvider)
#else
        public string ToString(string format, IFormatProvider formatProvider)
#endif
        {
            var useProvider = ResolveMoneyProvider(formatProvider);
            var formatter = useProvider.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;

            // The resolved provider always supplies a formatter. Were it not to, the rounded value is formatted,
            // so that any rounding the value carries is still honoured.
            return formatter?.Format(format, this, useProvider) ?? ((decimal)this).ToString(format, useProvider);
        }

        /// <summary>
        /// Resolve the provider of monetary formatting information for a caller-supplied format provider.
        /// </summary>
        /// <param name="formatProvider">Format provider supplied by the caller, which may be <c>null</c>.</param>
        /// <returns>A provider which supplies both a formatter and currency formatting information.</returns>
        /// <remarks>
        /// A caller may reasonably pass a <see cref="CultureInfo"/>, a <see cref="NumberFormatInfo"/> or a
        /// <see cref="CurrencyFormatInfo"/> rather than a <see cref="MoneyInfo"/>. Each is resolved to a
        /// <see cref="MoneyInfo"/> which keeps the formatting information the caller supplied, so that the
        /// caller's provider rather than the culture of the current thread decides the formatting.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        private static IFormatProvider ResolveMoneyProvider(IFormatProvider? formatProvider)
#else
        private static IFormatProvider ResolveMoneyProvider(IFormatProvider formatProvider)
#endif
        {
            if (formatProvider is null || formatProvider is MoneyInfo)
            {
                return formatProvider ?? DefaultMoneyProvider;
            }

            var currencyFormat = formatProvider.GetFormat(typeof(CurrencyFormatInfo)) as CurrencyFormatInfo;
            if (currencyFormat != null)
            {
                // A provider which supplies both currency formatting information and a formatter is used as it
                // stands. One which supplies only the former, such as a CurrencyFormatInfo, is given the money
                // formatter, so that a money format string is never handed to the number formatter.
                if (formatProvider.GetFormat(typeof(ICustomFormatter)) is ICustomFormatter)
                {
                    return formatProvider;
                }

                // Number formatting the provider does not supply is taken from its currency formatting
                // information, as it is when parsing, rather than from the culture of the current thread.
                var currencyNumberFormat = formatProvider.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo
                                           ?? GetNumberFormat(null, currencyFormat);
                return new MoneyInfo(currencyFormat, currencyNumberFormat) { IsReadOnly = true };
            }

            if (formatProvider is CultureInfo culture)
            {
                return MoneyInfo.GetMoneyInfo(culture);
            }

            // A provider of number formatting information alone, such as a NumberFormatInfo, decides the
            // formatting of the amount as a number. The presentation of the currency is the default.
            var defaultProvider = DefaultMoneyProvider;
            if (formatProvider.GetFormat(typeof(NumberFormatInfo)) is NumberFormatInfo numberFormat)
            {
                return new MoneyInfo(defaultProvider.CurrencyFormat, numberFormat) { IsReadOnly = true };
            }

            return defaultProvider;
        }

        /// <summary>
        /// Gets the provider of monetary formatting information used where a caller supplies none.
        /// </summary>
        private static MoneyInfo DefaultMoneyProvider
        {
            get
            {
#if NETSTANDARD2_0_OR_GREATER||NET
                return MoneyInfo.CurrentMoney;
#else
                return MoneyInfo.InvariantMoney;
#endif
            }
        }

        /// <summary>
        /// Converts the string representation of a monetary value to its <see cref="Money"/> equivalent,
        /// permitting only the specified elements.
        /// </summary>
        /// <param name="s">The string representation of the monetary value to convert.</param>
        /// <param name="styles">The elements permitted in <paramref name="s"/>.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific parsing information about <paramref name="s"/>.</param>
        /// <returns>The <see cref="Money"/> value equivalent to the monetary value contained in <paramref name="s"/>.</returns>
        /// <remarks>
        /// A currency is identified from an ISO 4217 currency code, or from a currency symbol which denotes
        /// exactly one currency. A symbol shared by several currencies is resolved from the currency of
        /// <paramref name="provider"/>, and only when <paramref name="styles"/> includes
        /// <see cref="MoneyStyles.AllowAmbiguousCurrencySymbol"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not in a correct format, or its currency cannot be determined.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static Money Parse(string s, MoneyStyles styles, IFormatProvider? provider)
#else
        public static Money Parse(string s, MoneyStyles styles, IFormatProvider provider)
#endif
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (!TryParse(s, styles, provider, out var result))
            {
                throw new FormatException($"{nameof(s)} is not in the correct format.");
            }

            return result;
        }

        /// <summary>
        /// Converts the string representation of a monetary value to its <see cref="Money"/> equivalent,
        /// permitting only the specified elements. A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="s">The string representation of the monetary value to convert.</param>
        /// <param name="styles">The elements permitted in <paramref name="s"/>.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific parsing information about <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="Money"/> value equivalent to <paramref name="s"/>, if the conversion succeeded; otherwise the default value.</param>
        /// <returns><c>true</c> if <paramref name="s"/> was converted successfully; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(string? s, MoneyStyles styles, IFormatProvider? provider, out Money result)
#else
        public static bool TryParse(string s, MoneyStyles styles, IFormatProvider provider, out Money result)
#endif
        {
            var useStyles = styles;
            if (provider is null)
            {
                // Without a culture there is no context in which to resolve a symbol shared by several
                // currencies, and the culture of the current thread is not a safe assumption to make about
                // the origin of a monetary value. Such a symbol is therefore not resolved at all.
                useStyles &= ~MoneyStyles.AllowAmbiguousCurrencySymbol;
            }

            var moneyProvider = ResolveMoneyProvider(provider);
            var currencyFormat = moneyProvider.GetFormat(typeof(CurrencyFormatInfo)) as CurrencyFormatInfo ?? CurrencyFormatInfo.InvariantInfo;
            var numberFormat = GetNumberFormat(provider, currencyFormat);

            return MoneyParser.TryParse(s, useStyles, currencyFormat, numberFormat, out result);
        }

        /// <summary>
        /// Converts the string representation of a monetary value in a specified format to its <see cref="Money"/> equivalent.
        /// </summary>
        /// <param name="s">The string representation of the monetary value to convert.</param>
        /// <param name="format">The format that <paramref name="s"/> must be in.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific parsing information about <paramref name="s"/>.</param>
        /// <returns>The <see cref="Money"/> value equivalent to the monetary value contained in <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> or <paramref name="format"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not in the format specified by <paramref name="format"/>.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static Money ParseExact(string s, string format, IFormatProvider? provider)
#else
        public static Money ParseExact(string s, string format, IFormatProvider provider)
#endif
        {
            if (format is null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            return ParseExact(s, new[] { format }, provider, MoneyStyles.Currency);
        }

        /// <summary>
        /// Converts the string representation of a monetary value in one of a set of specified formats to its
        /// <see cref="Money"/> equivalent.
        /// </summary>
        /// <param name="s">The string representation of the monetary value to convert.</param>
        /// <param name="formats">The formats of which <paramref name="s"/> must be in one.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific parsing information about <paramref name="s"/>.</param>
        /// <param name="styles">The elements permitted in <paramref name="s"/>.</param>
        /// <returns>The <see cref="Money"/> value equivalent to the monetary value contained in <paramref name="s"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="s"/> or <paramref name="formats"/> is <c>null</c>.</exception>
        /// <exception cref="FormatException">Thrown when <paramref name="s"/> is in none of the formats specified by <paramref name="formats"/>.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static Money ParseExact(string s, string[] formats, IFormatProvider? provider, MoneyStyles styles)
#else
        public static Money ParseExact(string s, string[] formats, IFormatProvider provider, MoneyStyles styles)
#endif
        {
            if (s is null)
            {
                throw new ArgumentNullException(nameof(s));
            }

            if (formats is null)
            {
                throw new ArgumentNullException(nameof(formats));
            }

            if (!TryParseExact(s, formats, provider, styles, out var result))
            {
                throw new FormatException($"{nameof(s)} is not in any of the expected formats.");
            }

            return result;
        }

        /// <summary>
        /// Converts the string representation of a monetary value in one of a set of specified formats to its
        /// <see cref="Money"/> equivalent. A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="s">The string representation of the monetary value to convert.</param>
        /// <param name="formats">The formats of which <paramref name="s"/> must be in one.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific parsing information about <paramref name="s"/>.</param>
        /// <param name="styles">The elements permitted in <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="Money"/> value equivalent to <paramref name="s"/>, if the conversion succeeded; otherwise the default value.</param>
        /// <returns><c>true</c> if <paramref name="s"/> was converted successfully; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// A value is accepted only where formatting the parsed result with one of <paramref name="formats"/>
        /// reproduces it. A caller expecting the currency code form may therefore reject a value carrying a
        /// currency symbol, which is otherwise accepted.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParseExact(string? s, string[]? formats, IFormatProvider? provider, MoneyStyles styles, out Money result)
#else
        public static bool TryParseExact(string s, string[] formats, IFormatProvider provider, MoneyStyles styles, out Money result)
#endif
        {
            result = default;

            if (string.IsNullOrEmpty(s) || formats is null || formats.Length == 0)
            {
                return false;
            }

            if (!TryParse(s, styles, provider, out var candidate))
            {
                return false;
            }

            // The parsed value must render back to the value supplied under one of the expected formats.
            // Comparing the rendering rather than inspecting the input keeps the accepted forms exactly in
            // step with those the formatter produces.
            foreach (var format in formats)
            {
                if (format is null)
                {
                    continue;
                }

                string formatted;
                try
                {
                    formatted = candidate.ToString(format, provider);
                }
                catch (FormatException)
                {
                    continue;
                }

                if (string.Equals(formatted, s, StringComparison.Ordinal))
                {
                    result = candidate;
                    return true;
                }
            }

            return false;
        }

#if NET7_0_OR_GREATER
        /// <summary>
        /// Tries to format the value of this instance into the provided span of characters.
        /// </summary>
        /// <param name="destination">The span in which to write this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination"/>.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information.</param>
        /// <returns><c>true</c> if the formatting was successful; otherwise, <c>false</c>.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
        {
            var formatted = ToString(format.Length == 0 ? null : format.ToString(), provider);
            if (formatted.Length > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            formatted.AsSpan().CopyTo(destination);
            charsWritten = formatted.Length;
            return true;
        }

        /// <summary>
        /// Parses a span of characters into a <see cref="Money"/> value.
        /// </summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that supplies culture-specific information about <paramref name="s"/>.</param>
        /// <returns>The <see cref="Money"/> value equivalent to the monetary value contained in <paramref name="s"/>.</returns>
        /// <exception cref="FormatException">Thrown when <paramref name="s"/> is not in a correct format.</exception>
        public static Money Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            return Parse(s.ToString(), provider);
        }

        /// <summary>
        /// Tries to parse a span of characters into a <see cref="Money"/> value.
        /// </summary>
        /// <param name="s">The span of characters to parse.</param>
        /// <param name="provider">An object that supplies culture-specific information about <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the result of parsing <paramref name="s"/>, or the default value on failure.</param>
        /// <returns><c>true</c> if <paramref name="s"/> was parsed successfully; otherwise, <c>false</c>.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out Money result)
        {
            return TryParse(s.ToString(), provider, out result);
        }
#endif

        /// <summary>
        /// Determine the number formatting information used to interpret the amount of a monetary value.
        /// </summary>
        /// <remarks>
        /// The separators are taken from the currency formatting information so that parsing accepts what
        /// formatting produces, rather than differing from it.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        private static NumberFormatInfo GetNumberFormat(IFormatProvider? provider, CurrencyFormatInfo currencyFormat)
#else
        private static NumberFormatInfo GetNumberFormat(IFormatProvider provider, CurrencyFormatInfo currencyFormat)
#endif
        {
            var numberFormat = provider?.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo;
            var result = numberFormat != null ? (NumberFormatInfo)numberFormat.Clone() : new NumberFormatInfo();

            result.NumberDecimalSeparator = currencyFormat.CurrencyDecimalSeparator;
            result.NumberGroupSeparator = currencyFormat.CurrencyGroupSeparator;
            result.NumberGroupSizes = currencyFormat.CurrencyGroupSizes;
            result.NegativeSign = string.IsNullOrEmpty(currencyFormat.NegativeSign) ? "-" : currencyFormat.NegativeSign;

            return result;
        }

        /// <summary>
        /// Converts the string representation of a number to its <see cref="Money"/> equivalent. A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="s">The string representation of the number to convert.</param>
        /// <param name="result">When this method returns, contains the <see cref="Money"/> number that is equivalent to the numeric value contained in <paramref name="s"/>, if the conversion succeeded, or is zero if the conversion failed.</param>
        /// <returns><c>true</c> if <paramref name="s"/> was converted successfully; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(string? s, out Money result)
#else
        public static bool TryParse(string s, out Money result)
#endif
        {
            return TryParse(s, MoneyStyles.Currency, CultureInfo.CurrentCulture, out result);
        }

        /// <summary>
        /// Converts the string representation of a number to its <see cref="Money"/> equivalent using the specified style and culture-specific format. A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        /// <param name="s">The string representation of the number to convert.</param>
        /// <param name="provider">An <see cref="IFormatProvider"/> object that supplies culture-specific parsing information about <paramref name="s"/>.</param>
        /// <param name="result">When this method returns, contains the <see cref="Money"/> number that is equivalent to the numeric value contained in <paramref name="s"/>, if the conversion succeeded, or is zero if the conversion failed.</param>
        /// <returns><c>true</c> if <paramref name="s"/> was converted successfully; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(string? s, IFormatProvider? provider, out Money result)
#else
        public static bool TryParse(string s, IFormatProvider provider, out Money result)
#endif
        {
            return TryParse(s, MoneyStyles.Currency, provider ?? CultureInfo.CurrentCulture, out result);
        }
#if NETCOREAPP3_0_OR_GREATER
        TypeCode IConvertible.GetTypeCode()
        {
            return _amount.GetTypeCode();
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToBoolean(provider);
        }

        byte IConvertible.ToByte(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToByte(provider);
        }

        char IConvertible.ToChar(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToChar(provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToDateTime(provider);
        }

        decimal IConvertible.ToDecimal(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToDecimal(provider);
        }

        double IConvertible.ToDouble(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToDouble(provider);
        }

        short IConvertible.ToInt16(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToInt16(provider);
        }

        int IConvertible.ToInt32(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToInt32(provider);
        }

        long IConvertible.ToInt64(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToInt64(provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToSByte(provider);
        }

        float IConvertible.ToSingle(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToSingle(provider);
        }

        string IConvertible.ToString(IFormatProvider? provider)
        {
            return _amount.ToString(provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToType(conversionType, provider);
        }

        ushort IConvertible.ToUInt16(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToUInt16(provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToUInt32(provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider? provider)
        {
            return ((IConvertible)_amount).ToUInt64(provider);
        }
#elif NETSTANDARD1_3_OR_GREATER || NET
        TypeCode IConvertible.GetTypeCode()
        {
            return _amount.GetTypeCode();
        }

        bool IConvertible.ToBoolean(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToBoolean(provider);
        }

        byte IConvertible.ToByte(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToByte(provider);
        }

        char IConvertible.ToChar(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToChar(provider);
        }

        DateTime IConvertible.ToDateTime(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToDateTime(provider);
        }

        decimal IConvertible.ToDecimal(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToDecimal(provider);
        }

        double IConvertible.ToDouble(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToDouble(provider);
        }

        short IConvertible.ToInt16(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToInt16(provider);
        }

        int IConvertible.ToInt32(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToInt32(provider);
        }

        long IConvertible.ToInt64(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToInt64(provider);
        }

        sbyte IConvertible.ToSByte(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToSByte(provider);
        }

        float IConvertible.ToSingle(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToSingle(provider);
        }

        string IConvertible.ToString(IFormatProvider provider)
        {
            return _amount.ToString(provider);
        }

        object IConvertible.ToType(Type conversionType, IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToType(conversionType, provider);
        }

        ushort IConvertible.ToUInt16(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToUInt16(provider);
        }

        uint IConvertible.ToUInt32(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToUInt32(provider);
        }

        ulong IConvertible.ToUInt64(IFormatProvider provider)
        {
            return ((IConvertible)_amount).ToUInt64(provider);
        }
#endif

        private static bool IsValidCurrencyCodeForMoneyValue(Iso4217CurrencyCurrent currency)
        {
            return currency.IsNationalCurrency() || currency.IsSupranationalCurrency() || currency == Iso4217CurrencyCurrent.XTS;
        }

        /// <summary>
        /// Determine whether a currency code may denote the currency of a monetary value.
        /// </summary>
        /// <param name="currency">Code of the currency.</param>
        /// <returns><c>true</c> if the code may denote the currency of a monetary value; otherwise <c>false</c>.</returns>
        internal static bool IsCurrencyCodeValidForMoneyValue(Iso4217CurrencyCurrent currency)
        {
            return IsValidCurrencyCodeForMoneyValue(currency);
        }
    }
}