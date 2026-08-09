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

        /// <summary>
        /// The currency code of a monetary value which carries no currency.
        /// </summary>
        internal const Iso4217CurrencyCurrent NoCurrency = DefaultCurrency;
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
            if (obj is null)
            {
                return 1;
            }

            if (obj is Money other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException($"{nameof(obj)} and this instance are not the same type.", nameof(obj));
        }

#else
        public int CompareTo(object obj)
        {
            if (obj is null)
            {
                return 1;
            }

            if (obj is Money other)
            {
                return CompareTo(other);
            }

            throw new ArgumentException($"{nameof(obj)} and this instance are not the same type.", nameof(obj));
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
            return obj is Money other && Equals(other);
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
                return currencyCode.HasValue ? new Money(-currencyAmount.Value, currencyCode.Value) : new Money(-currencyAmount.Value);
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

            return formatter?.Format(format, this, useProvider) ?? _amount.ToString(format, formatProvider);
        }

        /// <summary>
        /// Resolve the provider of monetary formatting information for a caller-supplied format provider.
        /// </summary>
        /// <param name="formatProvider">Format provider supplied by the caller, which may be <c>null</c>.</param>
        /// <returns>A provider which supplies both a formatter and currency formatting information.</returns>
        /// <remarks>
        /// A caller may reasonably pass a <see cref="CultureInfo"/> rather than a <see cref="MoneyInfo"/>,
        /// in which case the monetary formatting information for that culture is resolved on their behalf.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        private static IFormatProvider ResolveMoneyProvider(IFormatProvider? formatProvider)
#else
        private static IFormatProvider ResolveMoneyProvider(IFormatProvider formatProvider)
#endif
        {
            if (formatProvider is null)
            {
#if NETSTANDARD2_0_OR_GREATER||NET
                return MoneyInfo.CurrentMoney;
#else
                return MoneyInfo.InvariantMoney;
#endif
            }

            // A provider which already supplies currency formatting information is used as it stands.
            if (formatProvider.GetFormat(typeof(CurrencyFormatInfo)) is CurrencyFormatInfo)
            {
                return formatProvider;
            }

            if (formatProvider is CultureInfo culture)
            {
                return MoneyInfo.GetMoneyInfo(culture);
            }

#if NETSTANDARD2_0_OR_GREATER||NET
            return MoneyInfo.CurrentMoney;
#else
            return MoneyInfo.InvariantMoney;
#endif
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