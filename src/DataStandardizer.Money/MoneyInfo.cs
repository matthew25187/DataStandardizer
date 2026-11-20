using System;
using System.Globalization;
using System.Threading;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using DataStandardizer.LanguageTag;

namespace DataStandardizer.Money
{
    /// <summary>
    /// Provides culture-specific information for formatting and parsing <see cref="Money"/> values.
    /// </summary>
    /// <remarks>
    /// It holds the currency formatting information of a culture, <see cref="CurrencyFormat"/>, and the number formatting the host
    /// gives the culture. Use <see cref="InvariantMoney"/> for culture-independent information.
    /// </remarks>
    public class MoneyInfo : IFormatProvider
    {
#if NETCOREAPP3_0_OR_GREATER
        private CurrencyFormatInfo _currencyFormat = null!; // will always be set on all constructor paths
#else
        CurrencyFormatInfo _currencyFormat;
#endif
        private readonly NumberFormatInfo _numberFormat;
        private bool _isReadOnly;

        // Each slot holds the read-only information last derived for a culture: one for the culture of the
        // current thread, and one for any other culture. A slot is replaced whole, so a reader always sees a
        // culture paired with the information derived from it, and no lock is needed.
#if NETCOREAPP3_0_OR_GREATER
        private static CachedMoneyInfo? _cachedCurrentMoney;
        private static CachedMoneyInfo? _cachedOtherMoney;
#else
        private static CachedMoneyInfo _cachedCurrentMoney;
        private static CachedMoneyInfo _cachedOtherMoney;
#endif

        #region Constructors

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class with culture-independent information.
        /// </summary>
        public MoneyInfo()
            : this(default(CultureInfo))
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class for the neutral culture of a language.
        /// </summary>
        /// <param name="languageCode">ISO 639-1 code of the language.</param>
        /// <remarks>
        /// The currency formatting information is that of the Unicode CLDR locale the language names, whether or
        /// not the host knows the culture. Custom numeric formats and the general format use the host's number
        /// formatting for the culture, or the invariant number formatting where the host doesn't know it.
        /// </remarks>
        public MoneyInfo(Iso639Part1Language languageCode)
            : this(GetCultureName(languageCode, null))
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class for the culture of a language and country.
        /// </summary>
        /// <param name="languageCode">ISO 639-1 code of the language.</param>
        /// <param name="countryCode">ISO 3166-1 alpha-2 code of the country.</param>
        /// <remarks>
        /// The currency formatting information is that of the Unicode CLDR locale the combination names, whether
        /// or not the host knows the culture. Custom numeric formats and the general format use the host's number
        /// formatting for the culture, or the invariant number formatting where the host doesn't know it.
        /// </remarks>
        public MoneyInfo(Iso639Part1Language languageCode, Iso3166Part1Alpha2Country countryCode)
            : this(GetCultureName(languageCode, countryCode))
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class for the culture of a language and country.
        /// </summary>
        /// <param name="languageCode">ISO 639-1 code of the language.</param>
        /// <param name="countryCode">ISO 3166-1 alpha-3 code of the country.</param>
        /// <remarks>
        /// The currency formatting information is that of the Unicode CLDR locale the combination names, whether
        /// or not the host knows the culture. Custom numeric formats and the general format use the host's number
        /// formatting for the culture, or the invariant number formatting where the host doesn't know it.
        /// </remarks>
        public MoneyInfo(Iso639Part1Language languageCode, Iso3166Part1Alpha3Country countryCode)
            : this(GetCultureName(languageCode, (Iso3166Part1Alpha2Country)(ushort)countryCode))
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class for a culture.
        /// </summary>
        /// <param name="culture">Culture whose monetary formatting information is required, or <c>null</c> for the culture-independent information.</param>
        /// <remarks>
        /// A custom numeric format string, and the general format, are applied to the amount with the number
        /// formatting of <paramref name="culture"/>, or the invariant number formatting where it is <c>null</c>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public MoneyInfo(CultureInfo? culture)
#else
        public MoneyInfo(CultureInfo culture)
#endif
        {
            CurrencyFormat = CurrencyFormatInfo.CreateForCulture(culture);
            _numberFormat = culture != null ? culture.NumberFormat : NumberFormatInfo.InvariantInfo;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class for a culture name, which the host
        /// need not know.
        /// </summary>
        private MoneyInfo(string cultureName)
        {
            CurrencyFormat = CurrencyFormatInfo.CreateForCultureName(cultureName);
            _numberFormat = GetHostNumberFormat(cultureName);
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class from currency and number formatting
        /// information supplied by a caller.
        /// </summary>
        /// <param name="currencyFormat">Currency formatting information.</param>
        /// <param name="numberFormat">Number formatting information, applied to the amount for the general format and custom numeric formats.</param>
        internal MoneyInfo(CurrencyFormatInfo currencyFormat, NumberFormatInfo numberFormat)
        {
            CurrencyFormat = currencyFormat;
            _numberFormat = numberFormat;
        }

        #endregion

        #region Public Methods

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets an object that provides formatting information for the requested type.
        /// </summary>
        /// <param name="formatType">Type of the format object required.</param>
        /// <returns>
        /// The monetary formatter for <see cref="ICustomFormatter"/>, <see cref="CurrencyFormat"/> for
        /// <see cref="CurrencyFormatInfo"/>, the number formatting of the culture this instance was created
        /// for (invariant where there is none) for <see cref="NumberFormatInfo"/>; otherwise <c>null</c>.
        /// </returns>
        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return MoneyFormatter.Instance;
            }

            if (formatType == typeof(CurrencyFormatInfo))
            {
                return CurrencyFormat;
            }

            // The general format and custom numeric formats are applied to the amount as a number, which
            // asks for number formatting information. Without it the amount would be formatted with the
            // culture of the current thread rather than the culture of this instance.
            if (formatType == typeof(NumberFormatInfo))
            {
                return _numberFormat;
            }

            return null;
        }
#else
        /// <summary>
        /// Gets an object that provides formatting information for the requested type.
        /// </summary>
        /// <param name="formatType">Type of the format object required.</param>
        /// <returns>
        /// The monetary formatter for <see cref="ICustomFormatter"/>, <see cref="CurrencyFormat"/> for
        /// <see cref="CurrencyFormatInfo"/>, the number formatting of the culture this instance was created
        /// for (invariant where there is none) for <see cref="NumberFormatInfo"/>; otherwise <c>null</c>.
        /// </returns>
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return MoneyFormatter.Instance;
            }

            if (formatType == typeof(CurrencyFormatInfo))
            {
                return CurrencyFormat;
            }

            // The general format and custom numeric formats are applied to the amount as a number, which
            // asks for number formatting information. Without it the amount would be formatted with the
            // culture of the current thread rather than the culture of this instance.
            if (formatType == typeof(NumberFormatInfo))
            {
                return _numberFormat;
            }

            return null;
        }
#endif

        /// <summary>
        /// Get the monetary formatting information for a culture.
        /// </summary>
        /// <param name="culture">Culture whose monetary formatting information is required.</param>
        /// <returns>A read-only <see cref="MoneyInfo"/> for <paramref name="culture"/>.</returns>
        /// <remarks>
        /// This allows a caller to supply a <see cref="CultureInfo"/> where monetary formatting information
        /// is expected, as they would when formatting an intrinsic numeric type.
        /// <para>
        /// The instance may be shared with other callers that ask for the same <see cref="CultureInfo"/> instance.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static MoneyInfo GetMoneyInfo(CultureInfo? culture)
#else
        public static MoneyInfo GetMoneyInfo(CultureInfo culture)
#endif
        {
            return ReferenceEquals(culture, CultureInfo.CurrentCulture)
                ? GetCurrentMoney()
                : GetCachedMoneyInfo(culture, ref _cachedOtherMoney);
        }

        #endregion

        #region Public Properties

#if NETSTANDARD2_0_OR_GREATER||NET
        /// <summary>
        /// Gets the <see cref="MoneyInfo"/> instance representing the current monetary information
        /// based on the current thread's culture settings.
        /// </summary>
        /// <remarks>
        /// The <see cref="CurrentMoney"/> property initializes a <see cref="MoneyInfo"/> instance
        /// using the primary language subtag and region subtag derived from the current thread's culture.
        /// If the language or region cannot be determined, a read-only default instance is returned.
        /// </remarks>
        /// <value>
        /// A <see cref="MoneyInfo"/> instance configured for the current culture.
        /// </value>
        public static MoneyInfo CurrentMoney => GetCurrentMoney();
#endif

        /// <summary>
        /// Gets a <see cref="MoneyInfo"/> instance that represents culture-independent monetary information.
        /// </summary>
        /// <remarks>
        /// The <see cref="InvariantMoney"/> property provides a read-only instance of <see cref="MoneyInfo"/> 
        /// that is not tied to any specific language or country. It is typically used for scenarios where 
        /// culture-specific monetary formatting is not required.
        /// </remarks>
        public static MoneyInfo InvariantMoney { get; } = new MoneyInfo { IsReadOnly = true };

        /// <summary>
        /// Gets a value indicating whether the current <see cref="MoneyInfo"/> instance is read-only.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="MoneyInfo"/> instance is read-only; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// When <see cref="IsReadOnly"/> is set to <c>true</c>, modifications to the instance, such as
        /// changing the <see cref="CurrencyFormat"/>, will result in an <see cref="InvalidOperationException"/>.
        /// </remarks>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            internal set
            {
                // Sealing propagates to the currency format information this instance owns, but unsealing does
                // not: a format info which has been shared must never be made writable through its container.
                if (value)
                {
                    CurrencyFormat.IsReadOnly = true;
                }

                _isReadOnly = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="CurrencyFormatInfo"/> object that defines the currency formatting information
        /// for the current <see cref="MoneyInfo"/> instance.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the property is set to <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="MoneyInfo"/> instance is read-only.
        /// </exception>
        public CurrencyFormatInfo CurrencyFormat
        {
            get => _currencyFormat;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(MoneyInfo)} is read only.");
                }

                _currencyFormat = value;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the read-only monetary formatting information for the culture of the current thread.
        /// </summary>
        /// <remarks>
        /// This is <see cref="P:DataStandardizer.Money.MoneyInfo.CurrentMoney"/> on every target, including .NET Standard 1.1, where that property
        /// is not part of the public API, so that the default formatting is the same on every target.
        /// </remarks>
        internal static MoneyInfo GetCurrentMoney()
        {
            // Deriving the information from the culture directly avoids interpreting the language and region
            // subtags, which are string enumerations rather than System.Enum types.
            return GetCachedMoneyInfo(CultureInfo.CurrentCulture, ref _cachedCurrentMoney);
        }

        /// <summary>
        /// Gets the read-only monetary formatting information for a culture, from a cache slot where the slot
        /// holds that culture, and otherwise by deriving it and storing it in the slot.
        /// </summary>
        /// <remarks>
        /// The slot is keyed by the <see cref="CultureInfo"/> instance rather than its name, since the number
        /// formatting of the information is that of the instance. Callers that race to fill a slot each derive
        /// equivalent information, and the slot keeps whichever is stored last.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        private static MoneyInfo GetCachedMoneyInfo(CultureInfo? culture, ref CachedMoneyInfo? slot)
#else
        private static MoneyInfo GetCachedMoneyInfo(CultureInfo culture, ref CachedMoneyInfo slot)
#endif
        {
            var cached = Volatile.Read(ref slot);
            if (cached != null && ReferenceEquals(cached.Culture, culture))
            {
                return cached.MoneyInfo;
            }

            // The result is handed out to every caller, so it is sealed against modification.
            var moneyInfo = new MoneyInfo(culture) { IsReadOnly = true };
            Volatile.Write(ref slot, new CachedMoneyInfo(culture, moneyInfo));
            return moneyInfo;
        }

        /// <summary>
        /// Determine the culture name denoted by a language and an optional country code.
        /// </summary>
        private static string GetCultureName(Iso639Part1Language languageCode, Iso3166Part1Alpha2Country? countryCode)
        {
            return countryCode.HasValue
                ? string.Concat(languageCode.ToString(), "-", countryCode.Value.ToString())
                : languageCode.ToString();
        }

        /// <summary>
        /// Get the host's number formatting for a culture name, or the invariant number formatting where the
        /// host doesn't know the culture.
        /// </summary>
        private static NumberFormatInfo GetHostNumberFormat(string cultureName)
        {
            try
            {
                return new CultureInfo(cultureName).NumberFormat;
            }
            catch (CultureNotFoundException)
            {
                // Not every well-formed combination of a language and a country is a culture the host knows
                // about, and a host in invariant globalization mode knows none.
                return NumberFormatInfo.InvariantInfo;
            }
        }

        #endregion

        /// <summary>
        /// A culture paired with the read-only monetary formatting information derived from it.
        /// </summary>
        private sealed class CachedMoneyInfo
        {
#if NETCOREAPP3_0_OR_GREATER
            public CachedMoneyInfo(CultureInfo? culture, MoneyInfo moneyInfo)
#else
            public CachedMoneyInfo(CultureInfo culture, MoneyInfo moneyInfo)
#endif
            {
                Culture = culture;
                MoneyInfo = moneyInfo;
            }

#if NETCOREAPP3_0_OR_GREATER
            public CultureInfo? Culture { get; }
#else
            public CultureInfo Culture { get; }
#endif

            public MoneyInfo MoneyInfo { get; }
        }
    }
}