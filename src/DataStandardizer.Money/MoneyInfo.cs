using System;
using System.Globalization;
using DataStandardizer.Geography;
using DataStandardizer.Language;
using DataStandardizer.LanguageTag;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Money
{
    public class MoneyInfo : IFormatProvider
    {
#if NETCOREAPP3_0_OR_GREATER
        private CurrencyFormatInfo _currencyFormat = null!; // will always be set on all constructor paths
        private MoneyFormatter? _formatter;
#else
        CurrencyFormatInfo _currencyFormat;
        [CanBeNull] MoneyFormatter _formatter;
#endif
        private bool _isReadOnly;

#if NETSTANDARD2_0_OR_GREATER||NET
        private static readonly object CurrentMoneyLock = new object();
#if NETCOREAPP3_0_OR_GREATER
        private static MoneyInfo? _cachedCurrentMoney;
        private static string? _cachedCurrentMoneyCultureName;
#else
        private static MoneyInfo _cachedCurrentMoney;
        private static string _cachedCurrentMoneyCultureName;
#endif
#endif

        #region Constructors

        public MoneyInfo()
        {
            LoadCurrencyFormatInformation(null, null);
        }

        public MoneyInfo(Iso639Part1Language languageCode)
        {
            LoadCurrencyFormatInformation(languageCode, null);
        }

        public MoneyInfo(Iso639Part1Language languageCode, Iso3166Part1Alpha2Country countryCode)
        {
            LoadCurrencyFormatInformation(languageCode, countryCode);
        }

        public MoneyInfo(Iso639Part1Language languageCode, Iso3166Part1Alpha3Country countryCode)
        {
            var countryCodeNumeric = (ushort)countryCode;
            LoadCurrencyFormatInformation(languageCode, (Iso3166Part1Alpha2Country?)countryCodeNumeric);
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="MoneyInfo"/> class for a culture.
        /// </summary>
        /// <param name="culture">Culture whose monetary formatting information is required, or <c>null</c> for the culture-independent information.</param>
#if NETCOREAPP3_0_OR_GREATER
        public MoneyInfo(CultureInfo? culture)
#else
        public MoneyInfo(CultureInfo culture)
#endif
        {
            CurrencyFormat = CurrencyFormatInfo.CreateForCulture(culture);
        }

        #endregion

        #region Public Methods

#if NETCOREAPP3_0_OR_GREATER
        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return _formatter ??= new MoneyFormatter();
            }

            if (formatType == typeof(CurrencyFormatInfo))
            {
                return CurrencyFormat;
            }

            return null;
        }
#else
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return _formatter ?? (_formatter = new MoneyFormatter());
            }

            if (formatType == typeof(CurrencyFormatInfo))
            {
                return CurrencyFormat;
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
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static MoneyInfo GetMoneyInfo(CultureInfo? culture)
#else
        public static MoneyInfo GetMoneyInfo(CultureInfo culture)
#endif
        {
            return new MoneyInfo(culture) { IsReadOnly = true };
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
        public static MoneyInfo CurrentMoney
        {
            get
            {
                var currentCultureName = CultureInfo.CurrentCulture.Name;
                lock (CurrentMoneyLock)
                {
                    if (_cachedCurrentMoney != null && _cachedCurrentMoneyCultureName == currentCultureName)
                    {
                        return _cachedCurrentMoney;
                    }

                    var currentMoney = DoGetCurrentMoney();

                    _cachedCurrentMoney = currentMoney;
                    _cachedCurrentMoneyCultureName = currentCultureName;
                    return currentMoney;
                }
            }
        }
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
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="MoneyInfo"/> instance is read-only.
        /// </exception>
        public CurrencyFormatInfo CurrencyFormat
        {
            get => _currencyFormat;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(MoneyInfo)} is read only.");
                }

                _currencyFormat = value;
            }
        }

        #endregion

        #region Private Methods

#if NETSTANDARD2_0_OR_GREATER||NET
        private static MoneyInfo DoGetCurrentMoney()
        {
            // The result is cached and handed out to every caller, so it is sealed against modification.
            // Deriving it from the culture directly avoids interpreting the language and region subtags,
            // which are string enumerations rather than System.Enum types.
            return new MoneyInfo(CultureInfo.CurrentCulture) { IsReadOnly = true };
        }
#endif

        private void LoadCurrencyFormatInformation(Iso639Part1Language? languageCode, Iso3166Part1Alpha2Country? countryCode)
        {
            CurrencyFormat = CurrencyFormatInfo.CreateForCulture(GetCulture(languageCode, countryCode));
        }

        /// <summary>
        /// Resolve the culture denoted by a language and country code.
        /// </summary>
        /// <returns>The culture, or <c>null</c> where none is specified or the combination is not a culture known to the host.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private static CultureInfo? GetCulture(Iso639Part1Language? languageCode, Iso3166Part1Alpha2Country? countryCode)
#else
        private static CultureInfo GetCulture(Iso639Part1Language? languageCode, Iso3166Part1Alpha2Country? countryCode)
#endif
        {
            if (!languageCode.HasValue)
            {
                return null;
            }

            var cultureName = countryCode.HasValue
                ? string.Concat(languageCode.Value.ToString(), "-", countryCode.Value.ToString())
                : languageCode.Value.ToString();

            try
            {
                return new CultureInfo(cultureName);
            }
            catch (CultureNotFoundException)
            {
                // Not every well-formed combination of a language and a country is a culture the host knows
                // about. Falling back to the neutral resources is preferable to failing to construct at all.
                return null;
            }
        }

        #endregion
    }
}