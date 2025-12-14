using System;
using System.Globalization;
using System.Threading;
using DataStandardizer.Communication.E164;
using DataStandardizer.Geography;
using DataStandardizer.LanguageTag;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Communication
{
    /// <summary>
    /// Represents telephony-related information and provides functionality for formatting and handling 
    /// international telephone numbers in compliance with ITU E.164 standards.
    /// </summary>
    /// <remarks>
    /// This class supports the management of telephony rules and formats, including country-specific 
    /// telephony configurations. It implements <see cref="IFormatProvider"/> to supply custom formatting 
    /// for international telephone numbers.
    /// </remarks>
    public class TelephonyInfo : IFormatProvider
    {
        private readonly ushort _countryCode;
#if NETCOREAPP3_0_OR_GREATER
        private ItuE164InternationalNumberFormatter? _formatter; 
#else
        [CanBeNull] private ItuE164InternationalNumberFormatter _formatter; 
#endif
        private bool _isReadOnly;
        private ItuE164InternationalNumberFormatInfo _ituE164InternationalNumberFormat;

        #region Constructors

        public TelephonyInfo()
        {
            LoadInternationalNumberFormatInformation();
        }

        public TelephonyInfo(Iso3166Part1Alpha2Country countryCode)
            : this()
        {
            // TODO: This functionality is not yet fully supported; addition of country-specific telephony rules is pending.
            _countryCode = (ushort)countryCode;
        }

        public TelephonyInfo(Iso3166Part1Alpha3Country countryCode)
            : this()
        {
            // TODO: This functionality is not yet fully supported; addition of country-specific telephony rules is pending.
            _countryCode = (ushort)countryCode;
        }

        #endregion

        #region Public Methods

#if NETCOREAPP3_0_OR_GREATER
        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return _formatter ??= new ItuE164InternationalNumberFormatter();
            }

            if (formatType == typeof(ItuE164InternationalNumberFormatInfo))
            {
                return ItuE164InternationalNumberFormat;
            }

            return null;
        } 
#else
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(ICustomFormatter))
            {
                return _formatter ?? (_formatter = new ItuE164InternationalNumberFormatter());
            }

            if (formatType == typeof(ItuE164InternationalNumberFormatInfo))
            {
                return ItuE164InternationalNumberFormat;
            }

            return null;
        }
#endif

        #endregion

        #region Public Properties

#if NETSTANDARD2_0_OR_GREATER || NET
        /// <summary>
        /// Gets the <see cref="TelephonyInfo"/> instance representing the telephony settings 
        /// for the current thread's culture.
        /// </summary>
        /// <remarks>
        /// The returned instance is determined based on the region subtag of the current thread's culture.
        /// If the region subtag corresponds to a valid ISO 3166-1 alpha-2 country code, a corresponding 
        /// <see cref="TelephonyInfo"/> instance is returned. Otherwise, a default read-only instance is provided.
        /// </remarks>
        /// <value>
        /// A <see cref="TelephonyInfo"/> instance representing the telephony settings for the current culture.
        /// </value>
        public static TelephonyInfo CurrentTelephony => DoGetCurrentTelephony();
#endif
        /// <summary>
        /// Gets a read-only instance of <see cref="TelephonyInfo"/> that represents invariant telephony information.
        /// </summary>
        /// <remarks>
        /// This property provides a default telephony configuration that is not specific to any country or region.
        /// It is initialized as a read-only instance and can be used as a fallback or default telephony information provider.
        /// </remarks>
        public static TelephonyInfo InvariantTelephony { get; } = new TelephonyInfo() { IsReadOnly = true };

        /// <summary>
        /// Gets the ISO 3166-1 alpha-2 country code associated with the telephony information.
        /// </summary>
        /// <value>
        /// An <see cref="Iso3166Part1Alpha2Country"/> value representing the ISO 3166-1 alpha-2 country code,
        /// or <c>null</c> if the country code is not defined.
        /// </value>
        /// <remarks>
        /// This property checks whether the country code is defined in the <see cref="Iso3166Part1Alpha2Country"/> enumeration.
        /// If the code is valid, it returns the corresponding enumeration value; otherwise, it returns <c>null</c>.
        /// </remarks>
        public Iso3166Part1Alpha2Country? Iso3166Part1Alpha2Code => Enum.IsDefined(typeof(Iso3166Part1Alpha2Country), _countryCode) ? (Iso3166Part1Alpha2Country)_countryCode : default(Iso3166Part1Alpha2Country?);

        /// <summary>
        /// Gets the ISO 3166-1 alpha-3 country code associated with the telephony information.
        /// </summary>
        /// <value>
        /// An <see cref="Iso3166Part1Alpha3Country"/> enumeration value representing the ISO 3166-1 alpha-3 country code,
        /// or <c>null</c> if the country code is not defined.
        /// </value>
        /// <remarks>
        /// This property checks if the underlying country code is defined in the <see cref="Iso3166Part1Alpha3Country"/> enumeration.
        /// If it is defined, the corresponding enumeration value is returned; otherwise, <c>null</c> is returned.
        /// </remarks>
        public Iso3166Part1Alpha3Country? Iso3166Part1Alpha3Code => Enum.IsDefined(typeof(Iso3166Part1Alpha3Country), _countryCode) ? (Iso3166Part1Alpha3Country)_countryCode : default(Iso3166Part1Alpha3Country?);

        public bool IsReadOnly
        {
            get => _isReadOnly;
            internal set => _isReadOnly = ItuE164InternationalNumberFormat.IsReadOnly = value;
        }

        public ItuE164InternationalNumberFormatInfo ItuE164InternationalNumberFormat
        {
            get => _ituE164InternationalNumberFormat;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(TelephonyInfo)} is read only.");
                }

                _ituE164InternationalNumberFormat = value;
            }
        }

        #endregion

        #region Private Methods

#if NETSTANDARD2_0_OR_GREATER || NET
        private static TelephonyInfo DoGetCurrentTelephony()
        {
            var currentCultureTag = Bcp47LanguageTag.Create(Thread.CurrentThread.CurrentCulture.Name);
            Iso3166Part1Alpha2Country? countryCode = Enum.TryParse(currentCultureTag.RegionSubtag, out Iso3166Part1Alpha2Country countryCodeValue)
                ? countryCodeValue
                : default(Iso3166Part1Alpha2Country?);
            return countryCode.HasValue
                ? new TelephonyInfo(countryCode.Value) { IsReadOnly = true }
                : new TelephonyInfo() { IsReadOnly = true };
        }
#endif

        private void LoadInternationalNumberFormatInformation()
        {
            var formatInfo = new ItuE164InternationalNumberFormatInfo();

            string longInternationalNumberPattern = null, shortInternationalNumberPattern = null;
            if (Enum.ToObject(typeof(Iso3166Part1Alpha2Country), _countryCode) is Iso3166Part1Alpha2Country countryCode && Enum.IsDefined(typeof(Iso3166Part1Alpha2Country), countryCode))
            {
                var culture = new CultureInfo($"en-{countryCode}"); // don't care about the language; just want resources for the region
                longInternationalNumberPattern = Resources.ResourceManager.GetString(nameof(formatInfo.LongInternationalNumberPattern), culture) ?? string.Empty;
                shortInternationalNumberPattern = Resources.ResourceManager.GetString(nameof(formatInfo.ShortInternationalNumberPattern), culture) ?? string.Empty;
            }

            if (string.IsNullOrEmpty(longInternationalNumberPattern) && string.IsNullOrEmpty(shortInternationalNumberPattern))
            {
                longInternationalNumberPattern = Resources.ResourceManager.GetString(nameof(formatInfo.LongInternationalNumberPattern)) ?? string.Empty;
                shortInternationalNumberPattern = Resources.ResourceManager.GetString(nameof(formatInfo.ShortInternationalNumberPattern)) ?? string.Empty;
            }

            formatInfo.LongInternationalNumberPattern = longInternationalNumberPattern;
            formatInfo.ShortInternationalNumberPattern = shortInternationalNumberPattern;
            ItuE164InternationalNumberFormat = formatInfo;
        }

        #endregion
    }
}