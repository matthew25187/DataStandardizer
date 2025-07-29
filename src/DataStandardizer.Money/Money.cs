using System;
using System.Globalization;
using DataStandardizer.ISO4217;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Money
{
    public struct Money : IComparable, IComparable<Money>, IEquatable<Money>
#if NETSTANDARD1_3_OR_GREATER||NET
        , IConvertible
#endif
    {
        private static CultureInfo _defaultCulture = CultureInfo.InvariantCulture;
        private const Iso4217Current DefaultCurrency = Iso4217Current.XXX;

        private readonly decimal _amount;
#if NETCOREAPP3_0_OR_GREATER
        private readonly CultureInfo? _culture;
#else
        [CanBeNull] private readonly CultureInfo _culture;
#endif
        private Iso4217Current? _currency;
        private readonly int? _minorUnits;
        private readonly MidpointRounding? _roundingMethod;

#if NETCOREAPP3_0_OR_GREATER
        public Money(decimal amount, CultureInfo? culture = null)
#else
        public Money(decimal amount, CultureInfo culture = null)
#endif
        {
            _amount = amount;
            _culture = culture;
            _currency = null;
            _minorUnits = null;
            _roundingMethod = null;
        }

#if NETCOREAPP3_0_OR_GREATER
        public Money(decimal amount, Iso4217Current currency, CultureInfo? culture = null)
#else
        public Money(decimal amount, Iso4217Current currency, CultureInfo culture = null)
#endif
            : this(amount)
        {
            if (!Enum.IsDefined(currency.GetType(), currency))
                throw new ArgumentException($"Expected member of {nameof(Iso4217Current)}.", nameof(currency));

            if (!currency.IsNationalCurrency() && !currency.IsSupranationalCurrency() && currency != Iso4217Current.XTS)
                throw new ArgumentException("Expected a national currency code.", nameof(currency));

            _currency = currency;
            _culture = culture;
        }

#if NETCOREAPP3_0_OR_GREATER
        public Money(decimal amount, Iso4217Current currency, int minorUnits, CultureInfo? culture = null)
#else
        public Money(decimal amount, Iso4217Current currency, int minorUnits, CultureInfo culture = null)
#endif
            : this(amount, currency)
        {
            _minorUnits = minorUnits;
            _culture = culture;
        }

#if NETCOREAPP3_0_OR_GREATER
        public Money(decimal amount, Iso4217Current currency, int minorUnits, MidpointRounding roundingMethod, CultureInfo? culture = null)
#else
        public Money(decimal amount, Iso4217Current currency, int minorUnits, MidpointRounding roundingMethod, CultureInfo culture = null)
#endif
            : this(amount, currency, minorUnits)
        {
            _roundingMethod = roundingMethod;
            _culture = culture;
        }

        public static implicit operator decimal(Money value)
        {
            var result = value._amount;
            if (value._minorUnits.HasValue)
            {
                result = !value._roundingMethod.HasValue
                    ? Math.Round(value._amount, value._minorUnits.Value)
                    : Math.Round(value._amount, value._minorUnits.Value, value._roundingMethod.Value);
            }

            return result;
        }

        public static implicit operator Money(decimal value)
        {
            return new Money(value);
        }

        /// <summary>
        /// Gets the culture used for formatting the current value.
        /// </summary>
        public CultureInfo Culture => _culture ?? _defaultCulture;

        /// <summary>
        /// Gets the ISO 4217 currency code for the current value.
        /// </summary>
        public Iso4217Current IsoCurrencyCode => _currency ?? (_currency = DefaultCurrency).Value;

        /// <summary>
        /// Gets the number of digits of precision specified for the current value.
        /// </summary>
        public int? MinorUnits => _minorUnits;
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

        public bool Equals(Money other)
        {
            return decimal.Equals(_amount, other._amount) && Nullable.Equals(_currency, other._currency);
        }

        /// <summary>
        /// Get the number of digits of precision to use for formatting the output of this value.
        /// </summary>
        /// <returns>Digits of precision to use or <c>null</c> if the precision could not be determined.</returns>
        public int? GetMinorUnits()
        {
            var culture = _culture ?? _defaultCulture;
            return GetMinorUnits(culture);
        }

        /// <summary>
        /// Set the default culture to use for monetary values not having their own culture.
        /// </summary>
        /// <param name="culture">Culture to use as the default.</param>
#if NETCOREAPP3_0_OR_GREATER
        public static void SetDefaultCulture(CultureInfo culture)
#else
        public static void SetDefaultCulture([NotNull] CultureInfo culture)
#endif
        {
            _defaultCulture = culture;
        }

        /// <summary>
        /// Return a string representing the current value as a monetary amount with the currency code in place of the currency symbol.
        /// </summary>
        /// <returns>String representation of the monetary amount.</returns>
        public string ToCurrencyWithCurrencyCodeString()
        {
            var currencyCode = Enum.GetName(IsoCurrencyCode.GetType(), IsoCurrencyCode);
            var currencyCulture = _culture ?? _defaultCulture;
            var currencyMinorDigits = GetMinorUnits(currencyCulture);
            var currencyString = _amount.ToString($"C{currencyMinorDigits}", currencyCulture)
                .Replace(currencyCulture.NumberFormat.CurrencySymbol, currencyCode);
            return currencyString;
        }

        /// <summary>
        /// Return a string representing the current value as a monetary amount.
        /// </summary>
        /// <returns>String representation of the monetary amount.</returns>
        public string ToCurrencyWithCurrencySymbolString()
        {
            var currencyCulture = _culture ?? _defaultCulture;
            return _amount.ToString("C", currencyCulture);
        }

        public override string ToString()
        {
            return $"{nameof(_amount)}: {_amount}";
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
#elif NETSTANDARD1_3_OR_GREATER||NET
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
        private int? GetMinorUnits(CultureInfo culture) => _minorUnits ?? IsoCurrencyCode.GetMinorUnits() ?? culture.NumberFormat.CurrencyDecimalDigits;
    }
}