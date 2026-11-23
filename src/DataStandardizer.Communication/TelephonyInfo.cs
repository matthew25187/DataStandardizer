using System;
using System.Globalization;
using System.Resources;
using System.Threading;
using DataStandardizer.Communication.E164;
using DataStandardizer.Geography;
using DataStandardizer.LanguageTag;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
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
        // The country code of an instance for no country; no ISO 3166-1 country has the numeric code 0.
        private const ushort NoCountryCode = 0;
        private readonly ushort _countryCode;
#if NETCOREAPP3_0_OR_GREATER
        private ItuE164InternationalNumberFormatter? _formatter; 
#else
        [MaybeNull] private ItuE164InternationalNumberFormatter _formatter; 
#endif
        private bool _isReadOnly;
#if NETCOREAPP3_0_OR_GREATER
        private ItuE164InternationalNumberFormatInfo _ituE164InternationalNumberFormat = null!; 
#else
        private ItuE164InternationalNumberFormatInfo _ituE164InternationalNumberFormat;
#endif

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TelephonyInfo"/> class that is not specific to any country,
        /// with the invariant number format information.
        /// </summary>
        /// <remarks>
        /// The instance is not read-only. Unlike <see cref="InvariantTelephony"/>, its
        /// <see cref="ItuE164InternationalNumberFormat"/> can be replaced and its patterns changed.
        /// </remarks>
        public TelephonyInfo()
            : this(NoCountryCode, Resources.ResourceManager)
        {
        }

        // TODO: This functionality is not yet fully supported; addition of country-specific telephony rules is pending.
        /// <summary>
        /// Initializes a new instance of the <see cref="TelephonyInfo"/> class for a country.
        /// </summary>
        /// <param name="countryCode">The ISO 3166-1 alpha-2 code of the country.</param>
        /// <remarks>
        /// The number format information comes from the country's own resources where it has them, and from the invariant
        /// resources otherwise. Country-specific telephony rules are not yet supported. The instance is not read-only.
        /// </remarks>
        public TelephonyInfo(Iso3166Part1Alpha2Country countryCode)
            : this(countryCode, Resources.ResourceManager)
        {
        }

        // TODO: This functionality is not yet fully supported; addition of country-specific telephony rules is pending.
        /// <summary>
        /// Initializes a new instance of the <see cref="TelephonyInfo"/> class for a country.
        /// </summary>
        /// <param name="countryCode">The ISO 3166-1 alpha-3 code of the country.</param>
        /// <remarks>
        /// The number format information comes from the country's own resources where it has them, and from the invariant
        /// resources otherwise. Country-specific telephony rules are not yet supported. The instance is not read-only.
        /// </remarks>
        public TelephonyInfo(Iso3166Part1Alpha3Country countryCode)
            : this(countryCode, Resources.ResourceManager)
        {
        }

        /// <summary>
        /// Initializes a new instance for a country, loading its number format information from the given resources.
        /// </summary>
        /// <remarks>A seam for tests, which supply their own resources.</remarks>
        internal TelephonyInfo(Iso3166Part1Alpha2Country countryCode, ResourceManager resourceManager)
            : this((ushort)countryCode, resourceManager)
        {
        }

        /// <summary>
        /// Initializes a new instance for a country, loading its number format information from the given resources.
        /// </summary>
        /// <remarks>A seam for tests, which supply their own resources.</remarks>
        internal TelephonyInfo(Iso3166Part1Alpha3Country countryCode, ResourceManager resourceManager)
            : this((ushort)countryCode, resourceManager)
        {
        }

        private TelephonyInfo(ushort countryCode, ResourceManager resourceManager)
        {
            // The country code is set before the format information loads, which takes it as an input, so that a country's
            // patterns come from its own resources.
            _countryCode = countryCode;
            _ituE164InternationalNumberFormat = LoadInternationalNumberFormatInformation(countryCode, resourceManager);
        }

        #endregion

        #region Public Methods

#if NETCOREAPP3_0_OR_GREATER
        /// <inheritdoc/>
        /// <remarks>
        /// Returns an <see cref="ICustomFormatter"/> that formats an <see cref="ItuE164InternationalNumber"/>, and any other
        /// argument with the culture of the current thread, when <paramref name="formatType"/> is <see cref="ICustomFormatter"/>; the <see cref="ItuE164InternationalNumberFormat"/>
        /// of this instance when it is <see cref="ItuE164InternationalNumberFormatInfo"/>; and <c>null</c> for any other type.
        /// </remarks>
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
        /// <inheritdoc/>
        /// <remarks>
        /// Returns an <see cref="ICustomFormatter"/> that formats an <see cref="ItuE164InternationalNumber"/>, and any other
        /// argument with the culture of the current thread, when <paramref name="formatType"/> is <see cref="ICustomFormatter"/>; the <see cref="ItuE164InternationalNumberFormat"/>
        /// of this instance when it is <see cref="ItuE164InternationalNumberFormatInfo"/>; and <c>null</c> for any other type.
        /// </remarks>
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
        /// <see cref="TelephonyInfo"/> instance is returned. Otherwise, a default read-only instance is provided. That includes a
        /// region subtag that is a UN M.49 code, such as <c>419</c> in <c>es-419</c>.
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
        public static TelephonyInfo InvariantTelephony { get; } = new TelephonyInfo { IsReadOnly = true };

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

        /// <summary>
        /// Gets a value indicating whether this telephony information is read-only.
        /// </summary>
        /// <value>
        /// <c>true</c> if <see cref="ItuE164InternationalNumberFormat"/> can't be replaced and its patterns can't be changed, as for
        /// <see cref="InvariantTelephony"/>; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly
        {
            get => _isReadOnly;
            internal set => _isReadOnly = ItuE164InternationalNumberFormat.IsReadOnly = value;
        }

        /// <summary>
        /// Gets or sets the format information used to format ITU E.164 international numbers.
        /// </summary>
        /// <value>
        /// The format information, which holds the patterns of the short and long international formats.
        /// </value>
        /// <exception cref="InvalidOperationException">The property is being set and this telephony information is read-only.</exception>
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
            var countryCode = GetCultureCountryCode(Thread.CurrentThread.CurrentCulture.Name);
            return countryCode.HasValue
                ? new TelephonyInfo(countryCode.Value) { IsReadOnly = true }
                : new TelephonyInfo { IsReadOnly = true };
        }
#endif

        /// <summary>
        /// Gets the ISO 3166-1 alpha-2 country that a culture's region subtag names, or <c>null</c> when it names none.
        /// </summary>
        /// <param name="cultureName">The culture's name.</param>
        /// <remarks>A seam for tests, which reach it through <c>InternalsVisibleTo</c>.</remarks>
        internal static Iso3166Part1Alpha2Country? GetCultureCountryCode(string cultureName)
        {
            // The invariant culture's name is empty, which isn't a language tag; like any culture without a country, it has no
            // country code. Nor does a culture whose region subtag is a UN M.49 code, such as 419 in es-419 or 036 in en-036: only an
            // ISO 3166-1 alpha-2 code names a country, and ToIso3166Part1Alpha2 takes only that form, in any case (RFC 5646,
            // section 2.1.1).
            return Bcp47LanguageTag.TryCreate(cultureName, out var cultureTag)
                ? cultureTag.ToIso3166Part1Alpha2()
                : default(Iso3166Part1Alpha2Country?);
        }

        private static ItuE164InternationalNumberFormatInfo LoadInternationalNumberFormatInformation(ushort countryCodeValue, ResourceManager resourceManager)
        {
            var formatInfo = new ItuE164InternationalNumberFormatInfo();

            var regionCulture = Enum.IsDefined(typeof(Iso3166Part1Alpha2Country), countryCodeValue)
                ? GetRegionCulture((Iso3166Part1Alpha2Country)countryCodeValue)
                : null;

            // Each pattern the region's resources don't hold comes from the neutral resources.
            formatInfo.LongInternationalNumberPattern = GetPattern(resourceManager, nameof(formatInfo.LongInternationalNumberPattern), regionCulture);
            formatInfo.ShortInternationalNumberPattern = GetPattern(resourceManager, nameof(formatInfo.ShortInternationalNumberPattern), regionCulture);
            return formatInfo;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string GetPattern(ResourceManager resourceManager, string name, CultureInfo? regionCulture)
#else
        private static string GetPattern(ResourceManager resourceManager, string name, CultureInfo regionCulture)
#endif
        {
            // The neutral resources are those of the invariant culture. The overload without a culture would look the pattern up
            // through the current thread's UI culture, whose satellite resources aren't the neutral ones.
            var pattern = regionCulture == null ? null : resourceManager.GetString(name, regionCulture);
            return string.IsNullOrEmpty(pattern)
                ? resourceManager.GetString(name, CultureInfo.InvariantCulture) ?? string.Empty
                : pattern;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static CultureInfo? GetRegionCulture(Iso3166Part1Alpha2Country countryCode)
#else
        private static CultureInfo GetRegionCulture(Iso3166Part1Alpha2Country countryCode)
#endif
        {
            try
            {
                return new CultureInfo($"en-{countryCode}"); // don't care about the language; just want resources for the region
            }
            catch (ArgumentException)
            {
                // CultureNotFoundException: the platform has no culture for the region, as under invariant globalization mode or
                // for a region its culture data lacks, so the region can have no resources of its own.
                return null;
            }
        }

        #endregion
    }
}