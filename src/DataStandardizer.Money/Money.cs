using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataStandardizer.Money
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public readonly struct Money : IComparable, IComparable<Money>, IEquatable<Money>, IFormattable
#if NETSTANDARD1_3_OR_GREATER||NET
        , IConvertible
#endif
    {
        private static class GroupName
        {
            internal const string CurrencyCode = "code";
            internal const string CurrencyAmount = "amount";
        }

        private static class ErrorMessage
        {
            internal const string DifferentCurrenciesComparisonTemplate = "Unable to compare {0} values having different currencies.";
            internal const string ExpectedCurrencyCodeTemplate = "Expected a member of {0}.";
            internal const string ExpectedNationalCurrencyCode = "Expected a national currency code.";
        }

        private static readonly Regex CurrencyFormatExpression;
        private const Iso4217CurrencyCurrent DefaultCurrency = Iso4217CurrencyCurrent.XXX;
        private static readonly TimeSpan ExpressionTimeout = TimeSpan.FromSeconds(1);

        private readonly decimal _amount;
        private readonly Iso4217CurrencyCurrent? _currency;
        
        static Money()
        {
            var options = RegexOptions.None;
#if NETSTANDARD1_3_OR_GREATER||NET
            options |= RegexOptions.Compiled;
#endif
            CurrencyFormatExpression = new Regex(@"^[Cc](\d*)$", options, ExpressionTimeout);
        }
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

        public static bool operator ==(Money left, Money right)
        {
            if (left.IsoCurrencyCode != right.IsoCurrencyCode)
            {
                var errorMessage = string.Format(ErrorMessage.DifferentCurrenciesComparisonTemplate, nameof(Money));
                throw new InvalidOperationException(errorMessage);
            }

            return left.Equals(right);
        }

        public static bool operator !=(Money left, Money right)
        {
            if (left.IsoCurrencyCode != right.IsoCurrencyCode)
            {
                var errorMessage = string.Format(ErrorMessage.DifferentCurrenciesComparisonTemplate, nameof(Money));
                throw new InvalidOperationException(errorMessage);
            }

            return !left.Equals(right);
        }

        public static bool operator <(Money left, Money right)
        {
            if (left.IsoCurrencyCode != right.IsoCurrencyCode)
            {
                var errorMessage = string.Format(ErrorMessage.DifferentCurrenciesComparisonTemplate, nameof(Money));
                throw new InvalidOperationException(errorMessage);
            }

            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Money left, Money right)
        {
            if (left.IsoCurrencyCode != right.IsoCurrencyCode)
            {
                var errorMessage = string.Format(ErrorMessage.DifferentCurrenciesComparisonTemplate, nameof(Money));
                throw new InvalidOperationException(errorMessage);
            }

            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Money left, Money right)
        {
            if (left.IsoCurrencyCode != right.IsoCurrencyCode)
            {
                var errorMessage = string.Format(ErrorMessage.DifferentCurrenciesComparisonTemplate, nameof(Money));
                throw new InvalidOperationException(errorMessage);
            }

            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Money left, Money right)
        {
            if (left.IsoCurrencyCode != right.IsoCurrencyCode)
            {
                var errorMessage = string.Format(ErrorMessage.DifferentCurrenciesComparisonTemplate, nameof(Money));
                throw new InvalidOperationException(errorMessage);
            }

            return left.CompareTo(right) >= 0;
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
        public int CompareTo(object? obj)
        {
            return _amount.CompareTo(obj);
        }

#else
        public int CompareTo(object obj)
        {
#if NETSTANDARD2_0_OR_GREATER || NET
            return _amount.CompareTo(obj);
#else
            return ((IComparable)_amount).CompareTo(obj);
#endif
        }
#endif

        public int CompareTo(Money other)
        {
            return _amount.CompareTo(other._amount);
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
        /// <paramref name="currency"/> is not a national currency code or testing code.
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
        /// <paramref name="currency"/> is not a national currency code or testing code.
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
        /// <paramref name="currency"/> is not a national currency code or testing code.
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
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return base.Equals(obj);
        }
#else
        public override bool Equals(object obj)
        {
            return obj is Money other && Equals(other);
        }
#endif

        public bool Equals(Money other)
        {
            return decimal.Equals(_amount, other._amount) && Nullable.Equals(_currency, other._currency);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_amount.GetHashCode() * 397) ^ _currency.GetHashCode();
            }
        }

        /// <summary>
        /// Converts the string representation of a number to its <see cref="Money"/> equivalent.
        /// </summary>
        /// <param name="s">The string representation of the number to convert.</param>
        /// <returns>The <see cref="Money"/> number equivalent to the number contained in <paramref name="s"/>.</returns>
        public static Money Parse(string s)
        {
            if (s is null)
                throw new ArgumentNullException(nameof(s));

            IFormatProvider provider = CultureInfo.CurrentCulture;

            var currencyNegativePattern = BuildCurrencyNegativePattern(provider);
            var (currencyCode, currencyAmount) = ExtractValue(s, currencyNegativePattern, provider);
            if (currencyAmount.HasValue)
            {
                return currencyCode.HasValue ? new Money(-currencyAmount.Value, currencyCode.Value) : new Money(-currencyAmount.Value);
            }

            var currencyPositivePattern = BuildCurrencyPositivePattern(provider);
            (currencyCode, currencyAmount) = ExtractValue(s, currencyPositivePattern, provider);
            if (currencyAmount.HasValue)
            {
                return currencyCode.HasValue ? new Money(currencyAmount.Value, currencyCode.Value) : new Money(currencyAmount.Value);
            }

            throw new FormatException($"{nameof(s)} is not in the correct format.");
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

            var useProvider = provider ?? CultureInfo.CurrentCulture;

            var currencyNegativePattern = BuildCurrencyNegativePattern(useProvider);
            var (currencyCode, currencyAmount) = ExtractValue(s, currencyNegativePattern, useProvider);
            if (currencyAmount.HasValue)
            {
                return currencyCode.HasValue ? new Money(-currencyAmount.Value, currencyCode.Value) : new Money(currencyAmount.Value);
            }

            var currencyPositivePattern = BuildCurrencyPositivePattern(useProvider);
            (currencyCode, currencyAmount) = ExtractValue(s, currencyPositivePattern, useProvider);
            if (currencyAmount.HasValue)
            {
                return currencyCode.HasValue ? new Money(currencyAmount.Value, currencyCode.Value) : new Money(currencyAmount.Value);
            }

            throw new FormatException($"{nameof(s)} is not in the correct format.");
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
            if (!string.IsNullOrEmpty(format) && CurrencyFormatExpression.IsMatch(format) && IsoCurrencyCode != DefaultCurrency)
            {
                var result = _amount.ToString(format);
                var currencyCode = Enum.GetName(typeof(Iso4217CurrencyCurrent), IsoCurrencyCode);
                return result.Replace(CultureInfo.CurrentCulture.NumberFormat.CurrencySymbol, currencyCode);
            }

            return _amount.ToString(format);
        }
#if NETCOREAPP3_0_OR_GREATER
        public string ToString(string? format, IFormatProvider? formatProvider)
#else
        public string ToString(string format, IFormatProvider formatProvider)
#endif
        {
            if (!string.IsNullOrEmpty(format) && CurrencyFormatExpression.IsMatch(format) && IsoCurrencyCode != DefaultCurrency)
            {
                var result = _amount.ToString(format, formatProvider);

                var numberFormatInfo = formatProvider?.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo ?? CultureInfo.CurrentCulture.NumberFormat;
                var currencyCode = Enum.GetName(typeof(Iso4217CurrencyCurrent), IsoCurrencyCode);
                result = result.Replace(numberFormatInfo.CurrencySymbol, currencyCode);

                return result;
            }

            return _amount.ToString(format, formatProvider);
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
            if (string.IsNullOrEmpty(s))
            {
                result = default;
                return false;
            }

            IFormatProvider provider = CultureInfo.CurrentCulture;

            var currencyNegativePattern = BuildCurrencyNegativePattern(provider);
            var (currencyCode, currencyAmount) = ExtractValue(s, currencyNegativePattern, provider);
            if (currencyAmount.HasValue)
            {
                result = currencyCode.HasValue ? new Money(-currencyAmount.Value, currencyCode.Value) : new Money(-currencyAmount.Value);
                return true;
            }

            var currencyPositivePattern = BuildCurrencyPositivePattern(provider);
            (currencyCode, currencyAmount) = ExtractValue(s, currencyPositivePattern, provider);
            if (currencyAmount.HasValue)
            {
                result = currencyCode.HasValue ? new Money(currencyAmount.Value, currencyCode.Value) : new Money(currencyAmount.Value);
                return true;
            }

            result = default;
            return false;
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
            if (string.IsNullOrEmpty(s))
            {
                result = default;
                return false;
            }

            IFormatProvider useProvider = provider ?? CultureInfo.CurrentCulture;

            var currencyNegativePattern = BuildCurrencyNegativePattern(useProvider);
            var (currencyCode, currencyAmount) = ExtractValue(s, currencyNegativePattern, useProvider);
            if (currencyAmount.HasValue)
            {
                result = currencyCode.HasValue ? new Money(-currencyAmount.Value, currencyCode.Value) : new Money(-currencyAmount.Value);
                return true;
            }

            var currencyPositivePattern = BuildCurrencyPositivePattern(useProvider);
            (currencyCode, currencyAmount) = ExtractValue(s, currencyPositivePattern, useProvider);
            if (currencyAmount.HasValue)
            {
                result = currencyCode.HasValue ? new Money(currencyAmount.Value, currencyCode.Value) : new Money(currencyAmount.Value);
                return true;
            }

            result = default;
            return false;
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
#if NETCOREAPP3_0_OR_GREATER
        private static string BuildCurrencyNegativePattern(IFormatProvider? provider)
#else
        private static string BuildCurrencyNegativePattern(IFormatProvider provider)
#endif
        {
            var amountPattern = GetCurrencyAmountPattern(provider);
            var currencyCodePattern = GetCurrencyCodePattern();
            var currencyNegativePatterns = new[]
            {
                $@"\({currencyCodePattern}{amountPattern}\)",
                $"-{currencyCodePattern}{amountPattern}",
                $"{currencyCodePattern}-{amountPattern}",
                $"{currencyCodePattern}{amountPattern}-",
                $@"\({amountPattern}{currencyCodePattern}\)",
                $"-{amountPattern}{currencyCodePattern}",
                $"{amountPattern}-{currencyCodePattern}",
                $"{amountPattern}{currencyCodePattern}-",
                $"-{amountPattern} {currencyCodePattern}",
                $"-{currencyCodePattern} {amountPattern}",
                $"{amountPattern} {currencyCodePattern}-",
                $"{currencyCodePattern} {amountPattern}-",
                $"{currencyCodePattern} -{amountPattern}",
                $"{amountPattern}- {currencyCodePattern}",
                $@"\({currencyCodePattern} {amountPattern}\)",
                $@"\({amountPattern} {currencyCodePattern}\)"
            };
            return string.Join("|", currencyNegativePatterns);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string BuildCurrencyPositivePattern(IFormatProvider? provider)
#else
        private static string BuildCurrencyPositivePattern(IFormatProvider provider)
#endif
        {
            var amountPattern = GetCurrencyAmountPattern(provider);
            var currencyCodePattern = GetCurrencyCodePattern();
            var currencyPositivePatterns = new[]
            {
                $"{currencyCodePattern}{amountPattern}",
                $"{amountPattern}{currencyCodePattern}",
                $"{currencyCodePattern} {amountPattern}",
                $"{amountPattern} {currencyCodePattern}"
            };
            return string.Join("|", currencyPositivePatterns);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static (Iso4217CurrencyCurrent? CurrencyCode, decimal? CurrencyAmount) ExtractValue(string input, string pattern, IFormatProvider? provider)
#else
        private static (Iso4217CurrencyCurrent? CurrencyCode, decimal? CurrencyAmount) ExtractValue(string input, string pattern, IFormatProvider provider)
#endif
        {
            Iso4217CurrencyCurrent? currencyCode = null;
            Decimal? currencyAmount = null;

            var valueMatch = Regex.Match(input, pattern, RegexOptions.None, ExpressionTimeout);
            if (valueMatch.Success)
            {
                currencyCode = Enum.TryParse(valueMatch.Groups[GroupName.CurrencyCode].Value, out Iso4217CurrencyCurrent useCurrencyCode)
                    ? useCurrencyCode
                    : (Iso4217CurrencyCurrent?)null;
                currencyAmount = decimal.TryParse(valueMatch.Groups[GroupName.CurrencyAmount].Value, NumberStyles.Currency, provider, out var useCurrencyAmount)
                    ? useCurrencyAmount
                    : (decimal?)null;
            }

            return (currencyCode, currencyAmount);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string GetCurrencyAmountPattern(IFormatProvider? provider)
#else
        private static string GetCurrencyAmountPattern(IFormatProvider provider)
#endif
        {
            var numberFormatInfo = provider?.GetFormat(typeof(NumberFormatInfo)) as NumberFormatInfo ?? CultureInfo.CurrentCulture.NumberFormat;
            return $@"(?<{GroupName.CurrencyAmount}>\d+(?:{numberFormatInfo.CurrencyDecimalSeparator}\d+)?)";
        }

        private static string GetCurrencyCodePattern()
        {
            var currencyCodes = Enum.GetValues(typeof(Iso4217CurrencyCurrent))
                .Cast<Iso4217CurrencyCurrent>()
                .Where(code => code.IsNationalCurrency() || code.IsSupranationalCurrency() || code == Iso4217CurrencyCurrent.XTS)
                .Select(code => Enum.GetName(typeof(Iso4217CurrencyCurrent), code));
            return string.Concat("(?<", GroupName.CurrencyCode, ">", string.Join("|", currencyCodes), ")");
        }

        private static bool IsValidCurrencyCodeForMoneyValue(Iso4217CurrencyCurrent currency)
        {
            return currency.IsNationalCurrency() || currency.IsSupranationalCurrency() || currency == Iso4217CurrencyCurrent.XTS;
        }
    }
}