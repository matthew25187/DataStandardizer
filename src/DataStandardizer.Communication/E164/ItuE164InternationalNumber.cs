using System;
using System.Globalization;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif
using static DataStandardizer.Communication.E164.ItuE164Constants;

namespace DataStandardizer.Communication.E164
{
    /// <summary>
    /// Represents an ITU-T E.164 international telephone number.
    /// </summary>
    /// <remarks>
    /// This struct provides functionality for creating, parsing, and managing
    /// international telephone numbers in compliance with the ITU-T E.164 standard.
    /// It supports various types of numbers, including those for geographic areas,
    /// global services, networks, groups of countries, and trials.
    /// </remarks>
    public readonly struct ItuE164InternationalNumber : IEquatable<ItuE164InternationalNumber>, IFormattable,
        IItuE164InternationalNumberForGeographicAreas,
        IItuE164InternationalNumberForGlobalServices,
        IItuE164InternationalNumberForNetworks,
        IItuE164InternationalNumberForGroupsOfCountries,
        IItuE164InternationalNumberForTrials
    {
        private static class ErrorMessage
        {
            internal const string DigitCountRangeInvalidTemplate = "{0} must be {1} to {2} digits.";
            internal const string FieldNotSupportedTemplate = "This number does not support the {0} field.";
            internal const string FieldValueInvalidTemplate = "'{0}' is not a valid {1}.";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;

#if NETCOREAPP3_0_OR_GREATER
        private readonly IItuE164InternationalNumber? _numberStructure;
#else
        [CanBeNull] private readonly IItuE164InternationalNumber _numberStructure;
#endif

        private ItuE164InternationalNumber(IItuE164InternationalNumber numberStructure)
        {
            _numberStructure = numberStructure;
        }

        public bool Equals(ItuE164InternationalNumber other)
        {
            return _numberStructure?.Number == other._numberStructure?.Number;
        }

        public static explicit operator ulong(ItuE164InternationalNumber value)
        {
            return value._numberStructure?.Number ?? throw new InvalidCastException($"{nameof(ItuE164InternationalNumber)} is uninitialized.");
        }

        #region Public Methods

        /// <summary>
        /// Determines whether the current ITU-T E.164 international telephone number
        /// is associated with a geographic area.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the number is for a geographic area; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// Geographic area numbers are typically associated with specific countries or regions.
        /// This method checks if the underlying number structure implements the
        /// <see cref="IItuE164InternationalNumberForGeographicAreas"/> interface.
        /// </remarks>
        public bool IsNumberForGeographicArea()
        {
            return _numberStructure is IItuE164InternationalNumberForGeographicAreas;
        }

        /// <summary>
        /// Determines whether the current ITU-T E.164 international telephone number
        /// is designated for global services.
        /// </summary>
        /// <remarks>
        /// Global service numbers are assigned for services that are not tied to a specific
        /// geographic location, such as international freephone numbers or universal
        /// personal telecommunications.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the number is for global services; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNumberForGlobalService()
        {
            return _numberStructure is IItuE164InternationalNumberForGlobalServices;
        }

        /// <summary>
        /// Determines whether the current ITU-T E.164 international number
        /// is designated for a group of countries.
        /// </summary>
        /// <remarks>
        /// This method checks if the underlying number structure implements
        /// the <see cref="IItuE164InternationalNumberForGroupsOfCountries"/> interface,
        /// which indicates that the number is associated with a group of countries
        /// as per the ITU-T E.164 standard.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the number is for a group of countries; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNumberForGroupOfCountries()
        {
            return _numberStructure is IItuE164InternationalNumberForGroupsOfCountries;
        }

        /// <summary>
        /// Determines whether the current ITU-T E.164 international number is designated for a network.
        /// </summary>
        /// <remarks>
        /// This method checks if the underlying number structure implements the 
        /// <see cref="IItuE164InternationalNumberForNetworks"/> interface, indicating that the number
        /// is specifically assigned for use within a network.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the number is for a network; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNumberForNetwork()
        {
            return _numberStructure is IItuE164InternationalNumberForNetworks;
        }

        /// <summary>
        /// Determines whether the current ITU-T E.164 international number is designated for trial purposes.
        /// </summary>
        /// <remarks>
        /// This method checks if the underlying number structure implements the 
        /// <see cref="IItuE164InternationalNumberForTrials"/> interface, indicating that the number
        /// is intended for trial use as per the ITU-T E.164 standard.
        /// </remarks>
        /// <returns>
        /// <c>true</c> if the number is for trial purposes; otherwise, <c>false</c>.
        /// </returns>
        public bool IsNumberForTrial()
        {
            return _numberStructure is IItuE164InternationalNumberForTrials;
        }

        public override string ToString()
        {
            var formatter = TelephonyInfo.InvariantTelephony.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
            return formatter?.Format("G", this, null) ?? base.ToString() ?? string.Empty;
        }

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Converts the current <see cref="ItuE164InternationalNumber"/> instance to its string representation
        /// using the specified format string.
        /// </summary>
        /// <param name="format">
        /// A format string that specifies the format to use. If <c>null</c> or empty, the default format is used.
        /// </param>
        /// <returns>
        /// A string representation of the current <see cref="ItuE164InternationalNumber"/> instance
        /// formatted according to the specified format string.
        /// </returns>
        /// <remarks>
        /// This method uses an <see cref="ICustomFormatter"/> implementation, if available, to format the number.
        /// If no custom formatter is provided, the default <see cref="object.ToString"/> implementation is used.
        /// </remarks>
        public string ToString(string? format)
        {
            var formatter = TelephonyInfo.InvariantTelephony.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
            return formatter?.Format(format, this, null) ?? base.ToString() ?? string.Empty;
        }
        /// <summary>
        /// Converts the current <see cref="ItuE164InternationalNumber"/> instance to its string representation
        /// using the specified format provider.
        /// </summary>
        /// <param name="formatProvider">
        /// An object that provides culture-specific formatting information.
        /// </param>
        /// <returns>
        /// A string representation of the current <see cref="ItuE164InternationalNumber"/> instance.
        /// </returns>
        public string ToString(IFormatProvider? formatProvider)
        {
            var formatter = formatProvider?.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
            return formatter?.Format("G", this, formatProvider) ?? base.ToString() ?? string.Empty;
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            var formatter = formatProvider?.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
            return formatter?.Format(format, this, formatProvider) ?? base.ToString() ?? string.Empty;
        }
#else
        /// <summary>
        /// Converts the current <see cref="ItuE164InternationalNumber"/> instance to its string representation
        /// using the specified format string.
        /// </summary>
        /// <param name="format">
        /// A format string that specifies the format to use. If <c>null</c> or empty, the default format is used.
        /// </param>
        /// <returns>
        /// A string representation of the current <see cref="ItuE164InternationalNumber"/> instance
        /// formatted according to the specified format string.
        /// </returns>
        /// <remarks>
        /// This method utilizes a custom formatter, if available, to format the number.
        /// If no custom formatter is found, the default string representation is returned.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the <see cref="ItuE164InternationalNumber"/> instance is uninitialized.
        /// </exception>
        public string ToString(string format)
        {
            var formatter = TelephonyInfo.InvariantTelephony.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
            return formatter?.Format(format, this, null) ?? base.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Converts the current <see cref="ItuE164InternationalNumber"/> instance to its string representation
        /// using the specified format provider.
        /// </summary>
        /// <param name="formatProvider">
        /// An object that provides culture-specific formatting information. If <c>null</c>, the default formatting
        /// is applied.
        /// </param>
        /// <returns>
        /// A string representation of the current <see cref="ItuE164InternationalNumber"/> instance, formatted
        /// according to the specified <paramref name="formatProvider"/>.
        /// </returns>
        public string ToString(IFormatProvider formatProvider)
        {
            var formatter = formatProvider?.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
            return formatter?.Format("G", this, formatProvider) ?? base.ToString() ?? string.Empty;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            var formatter = formatProvider?.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
            return formatter?.Format(format, this, formatProvider) ?? base.ToString() ?? string.Empty;
        }
#endif

        #endregion

        #region Public Properties

        public ulong Number => _numberStructure?.Number ?? throw new InvalidOperationException($"{nameof(ItuE164InternationalNumber)} is uninitialized.");

        ushort IItuE164InternationalNumber.CountryCode => _numberStructure?.CountryCode ??
                                                          throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Country Code"));

        ItuE164NationalSignificantNumber IItuE164InternationalNumberForGeographicAreas.NationalSignificantNumber => _numberStructure is IItuE164InternationalNumberForGeographicAreas numberStructureForGeographicAreas
            ? numberStructureForGeographicAreas.NationalSignificantNumber
            : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "National Significant Number"));

        ItuE164AssignedIdentificationCodesForNetworks IItuE164InternationalNumberForNetworks.IdentificationCode => _numberStructure is IItuE164InternationalNumberForNetworks numberStructureForNetworks
            ? numberStructureForNetworks.IdentificationCode
            : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Identification Code"));

        ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries IItuE164InternationalNumberForGroupsOfCountries.GroupIdentificationCode =>
            _numberStructure is IItuE164InternationalNumberForGroupsOfCountries numberStructureForGroupsOfCountries
                ? numberStructureForGroupsOfCountries.GroupIdentificationCode
                : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Group Identification Code"));

        ItuE164AssignedTrialIdentificationCodesForTrials IItuE164InternationalNumberForTrials.TrialIdentificationCode => _numberStructure is IItuE164InternationalNumberForTrials numberStructureForTrials
            ? numberStructureForTrials.TrialIdentificationCode
            : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Trial Identification Code"));

        ItuE164SubscriberNumber? IItuE164InternationalNumberForTrials.SubscriberNumber => _numberStructure is IItuE164InternationalNumberForTrials numberStructureForTrials
            ? numberStructureForTrials.SubscriberNumber
            : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Subscriber Number"));

        ItuE164SubscriberNumber IItuE164InternationalNumberForGroupsOfCountries.SubscriberNumber => _numberStructure is IItuE164InternationalNumberForGroupsOfCountries numberStructureForGroupsOfCountries
            ? numberStructureForGroupsOfCountries.SubscriberNumber
            : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Subscriber Number"));

        ItuE164SubscriberNumber IItuE164InternationalNumberForNetworks.SubscriberNumber => _numberStructure is IItuE164InternationalNumberForNetworks numberStructureForNetworks
            ? numberStructureForNetworks.SubscriberNumber
            : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Subscriber Number"));

        ItuE164GlobalSubscriberNumber IItuE164InternationalNumberForGlobalServices.GlobalSubscriberNumber => _numberStructure is IItuE164InternationalNumberForGlobalServices numberStructureForGlobalServices
            ? numberStructureForGlobalServices.GlobalSubscriberNumber
            : throw new NotSupportedException(string.Format(ErrorMessage.FieldNotSupportedTemplate, "Global Subscriber Number"));

        #endregion

        #region Static Methods

        /// <summary>
        /// Creates an ITU-T E.164 international telephone number for a geographic area.
        /// </summary>
        /// <param name="number">
        /// The complete telephone number, including the country code and national significant number,
        /// represented as an unsigned 64-bit integer.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the specified number
        /// for a geographic area.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="number"/> exceeds the maximum allowed digit count of 15.
        /// </exception>
        /// <remarks>
        /// This method validates the provided number to ensure it adheres to the ITU-T E.164 standard
        /// for geographic area numbers. If the validation passes, the number is encapsulated within
        /// an appropriate strategy for geographic areas.
        /// </remarks>
        public static ItuE164InternationalNumber CreateNumberForGeographicArea(ulong number)
        {
            var numberDigitCount = number.ToString().Length;

            if (numberDigitCount > MaximumDigitCount)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Number", 1, MaximumDigitCount);
                throw new ArgumentOutOfRangeException(nameof(number), number, message);
            }

            var numberStructure = new ItuE164InternationalNumberStructureForGeographicAreas(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an ITU E.164 international number for a geographic area.
        /// </summary>
        /// <param name="countryCode">
        ///     The country code assigned to the geographic area, represented as an <see cref="ItuE164AssignedCountryCodesForGeographicAreas"/>.
        /// </param>
        /// <param name="nationalSignificantNumber">
        ///     The national significant number (NSN) within the geographic area.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the constructed international number.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the provided <paramref name="countryCode"/> is not a valid or defined country code.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the digit count of <paramref name="nationalSignificantNumber"/> exceeds the allowable range
        /// based on the maximum digit count for the international number.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForGeographicArea(ItuE164AssignedCountryCodesForGeographicAreas countryCode, ItuE164NationalSignificantNumber nationalSignificantNumber)
        {
            var countryCodeDigitCount = $"{(ushort)countryCode}".Length;

            if (!Enum.IsDefined(typeof(ItuE164AssignedCountryCodesForGeographicAreas), countryCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, countryCode, "Country Code");
                throw new ArgumentException(message, nameof(countryCode));
            }

            if (nationalSignificantNumber.DigitCount > MaximumDigitCount - countryCodeDigitCount)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "National Significant Number", 1, MaximumDigitCount - countryCodeDigitCount);
                throw new ArgumentOutOfRangeException(nameof(nationalSignificantNumber), nationalSignificantNumber, message);
            }

            var number = ulong.Parse($"{(ushort)countryCode}{nationalSignificantNumber}", NumberStyles, CultureInfo.InvariantCulture);
            var numberStructure = new ItuE164InternationalNumberStructureForGeographicAreas(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an ITU-T E.164 international telephone number for global services.
        /// </summary>
        /// <param name="number">
        /// The global service number to be assigned. The number must contain between 1 and 15 digits.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the global service number.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="number"/> exceeds the maximum allowed digit count of 15.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForGlobalService(ulong number)
        {
            var numberDigitCount = number.ToString().Length;

            if (numberDigitCount > MaximumDigitCount)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Number", 1, MaximumDigitCount);
                throw new ArgumentOutOfRangeException(nameof(number), number, message);
            }

            var numberStructure = new ItuE164InternationalNumberStructureForGlobalServices(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an ITU E.164 international number for global services using the specified country code and global subscriber number.
        /// </summary>
        /// <param name="countryCode">
        ///     The country code representing the global service. Must be a valid value from <see cref="ItuE164AssignedCountryCodesForGlobalServices"/>.
        /// </param>
        /// <param name="globalSubscriberNumber">
        ///     The global subscriber number. Must contain between 1 and 12 digits.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the global service number.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the <paramref name="countryCode"/> is not a valid value from <see cref="ItuE164AssignedCountryCodesForGlobalServices"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="globalSubscriberNumber"/> exceeds the maximum allowed digit count of 12.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForGlobalService(ItuE164AssignedCountryCodesForGlobalServices countryCode, ItuE164GlobalSubscriberNumber globalSubscriberNumber)
        {
            if (!Enum.IsDefined(typeof(ItuE164AssignedCountryCodesForGlobalServices), countryCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, countryCode, "Country Code");
                throw new ArgumentException(message, nameof(countryCode));
            }

            if (globalSubscriberNumber.DigitCount > 12)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Global Subscriber Number", 1, 12);
                throw new ArgumentOutOfRangeException(nameof(globalSubscriberNumber), globalSubscriberNumber, message);
            }

            var number = ulong.Parse($"{(ushort)countryCode}{globalSubscriberNumber}", NumberStyles, CultureInfo.InvariantCulture);
            var numberStructure = new ItuE164InternationalNumberStructureForGlobalServices(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an ITU-T E.164 international telephone number for a network using the specified number.
        /// </summary>
        /// <param name="number">
        /// The network-specific number. The number must contain between 1 and 15 digits.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the network-specific number.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="number"/> exceeds the maximum allowed digit count of 15.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForNetwork(ulong number)
        {
            var numberDigitCount = number.ToString().Length;

            if (numberDigitCount > MaximumDigitCount)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Number", 1, MaximumDigitCount);
                throw new ArgumentOutOfRangeException(nameof(number), number, message);
            }

            var numberStructure = new ItuE164InternationalNumberStructureForNetworks(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an ITU E.164 international number for a network using the specified country code, 
        /// identification code, and subscriber number.
        /// </summary>
        /// <param name="countryCode">
        /// The country code assigned to the network, represented as an <see cref="ItuE164AssignedCountryCodesForNetworks"/>.
        /// </param>
        /// <param name="identificationCode">
        /// The identification code assigned to the network, represented as an <see cref="ItuE164AssignedIdentificationCodesForNetworks"/>.
        /// </param>
        /// <param name="subscriberNumber">
        /// The subscriber number associated with the network, represented as an <see cref="ItuE164SubscriberNumber"/>.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the constructed international number.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the <paramref name="countryCode"/> or <paramref name="identificationCode"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="subscriberNumber"/> exceeds the maximum allowable digit count 
        /// for the combination of country code and identification code.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForNetwork(ItuE164AssignedCountryCodesForNetworks countryCode, ItuE164AssignedIdentificationCodesForNetworks identificationCode, ItuE164SubscriberNumber subscriberNumber)
        {
            if (!Enum.IsDefined(typeof(ItuE164AssignedCountryCodesForNetworks), countryCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, countryCode, "Country Code");
                throw new ArgumentException(message, nameof(countryCode));
            }

            if (!Enum.IsDefined(typeof(ItuE164AssignedIdentificationCodesForNetworks), identificationCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, identificationCode, "Identification Code");
                throw new ArgumentException(message, nameof(identificationCode));
            }

            var countryCodeDigitCount = $"{(ushort)countryCode}".Length;
            var identificationCodeDigitCount = $"{(ushort)identificationCode}".Length;
            if (subscriberNumber.DigitCount > MaximumDigitCount - countryCodeDigitCount - identificationCodeDigitCount)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Subscriber Number", 1, MaximumDigitCount - countryCodeDigitCount - identificationCodeDigitCount);
                throw new ArgumentOutOfRangeException(nameof(subscriberNumber), subscriberNumber, message);
            }

            var number = ulong.Parse($"{(ushort)countryCode}{(ushort)identificationCode}{subscriberNumber}", NumberStyles, CultureInfo.InvariantCulture);
            var numberStructure = new ItuE164InternationalNumberStructureForNetworks(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an ITU-T E.164 international telephone number for a group of countries.
        /// </summary>
        /// <param name="number">
        /// The full international telephone number, including the country code and group identification code.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the specified number for a group of countries.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="number"/> exceeds the maximum allowed digit count of 15.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForGroupOfCountries(ulong number)
        {
            var numberDigitCount = number.ToString().Length;

            if (numberDigitCount < 5 || numberDigitCount > MaximumDigitCount)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Number", 5, MaximumDigitCount);
                throw new ArgumentOutOfRangeException(nameof(number), number, message);
            }

            var numberStructure = new ItuE164InternationalNumberStructureForGroupsOfCountries(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an instance of <see cref="ItuE164InternationalNumber"/> for a group of countries.
        /// </summary>
        /// <param name="countryCode">
        /// The country code assigned to the group of countries, represented by <see cref="ItuE164AssignedCountryCodesForGroupsOfCountries"/>.
        /// </param>
        /// <param name="groupIdentificationCode">
        /// The group identification code assigned to the group of countries, represented by <see cref="ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries"/>.
        /// </param>
        /// <param name="subscriberNumber">
        /// The subscriber number associated with the group of countries, represented by <see cref="ItuE164SubscriberNumber"/>.
        /// </param>
        /// <returns>
        /// A new instance of <see cref="ItuE164InternationalNumber"/> representing the international number for the specified group of countries.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the <paramref name="countryCode"/> or <paramref name="groupIdentificationCode"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="subscriberNumber"/> exceeds the maximum allowed digit count.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForGroupOfCountries(ItuE164AssignedCountryCodesForGroupsOfCountries countryCode, ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries groupIdentificationCode,
            ItuE164SubscriberNumber subscriberNumber)
        {
            if (!Enum.IsDefined(typeof(ItuE164AssignedCountryCodesForGroupsOfCountries), countryCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, countryCode, "Country Code");
                throw new ArgumentException(message, nameof(countryCode));
            }

            if (!Enum.IsDefined(typeof(ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries), groupIdentificationCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, groupIdentificationCode, "Group Identification Code");
                throw new ArgumentException(message, nameof(groupIdentificationCode));
            }

            if (subscriberNumber.DigitCount > 11)
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, subscriberNumber, "Subscriber Number");
                throw new ArgumentOutOfRangeException(nameof(subscriberNumber), subscriberNumber, message);
            }

            var number = ulong.Parse($"{(ushort)countryCode}{(byte)groupIdentificationCode}{subscriberNumber}", NumberStyles, CultureInfo.InvariantCulture);
            var numberStructure = new ItuE164InternationalNumberStructureForGroupsOfCountries(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an ITU-T E.164 international telephone number for trial purposes.
        /// </summary>
        /// <param name="number">
        /// The trial number to be used. The number must contain between 1 and 15 digits.
        /// </param>
        /// <returns>
        /// An instance of <see cref="ItuE164InternationalNumber"/> representing the trial number.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="number"/> exceeds the maximum allowed digit count of 15.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForTrial(ulong number)
        {
            var numberDigitCount = number.ToString().Length;

            if (numberDigitCount < 4 || numberDigitCount > MaximumDigitCount)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Number", 4, MaximumDigitCount);
                throw new ArgumentOutOfRangeException(nameof(number), number, message);
            }

            var numberStructure = new ItuE164InternationalNumberStructureForTrials(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Creates an instance of <see cref="ItuE164InternationalNumber"/> for trial purposes.
        /// </summary>
        /// <param name="countryCode">
        /// The country code assigned for trials, represented by <see cref="ItuE164AssignedCountryCodesForTrials"/>.
        /// </param>
        /// <param name="trialIdentificationCode">
        /// The trial identification code, represented by <see cref="ItuE164AssignedTrialIdentificationCodesForTrials"/>.
        /// </param>
        /// <param name="subscriberNumber">
        /// An optional subscriber number, represented by <see cref="ItuE164SubscriberNumber"/>. 
        /// The digit count must not exceed 11 if provided.
        /// </param>
        /// <returns>
        /// A new instance of <see cref="ItuE164InternationalNumber"/> configured for trials.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when the <paramref name="countryCode"/> or <paramref name="trialIdentificationCode"/> is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the <paramref name="subscriberNumber"/> exceeds the maximum allowed digit count of 11.
        /// </exception>
        public static ItuE164InternationalNumber CreateNumberForTrial(ItuE164AssignedCountryCodesForTrials countryCode, ItuE164AssignedTrialIdentificationCodesForTrials trialIdentificationCode, ItuE164SubscriberNumber? subscriberNumber)
        {
            if (!Enum.IsDefined(typeof(ItuE164AssignedCountryCodesForTrials), countryCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, countryCode, "Country Code");
                throw new ArgumentException(message, nameof(countryCode));
            }

            if (!Enum.IsDefined(typeof(ItuE164AssignedTrialIdentificationCodesForTrials), trialIdentificationCode))
            {
                var message = string.Format(ErrorMessage.FieldValueInvalidTemplate, trialIdentificationCode, "Trial Identification Code");
                throw new ArgumentException(message, nameof(trialIdentificationCode));
            }

            if (subscriberNumber.HasValue && subscriberNumber.Value.DigitCount > 11)
            {
                var message = string.Format(ErrorMessage.DigitCountRangeInvalidTemplate, "Subscriber Number", 1, 11);
                throw new ArgumentOutOfRangeException(nameof(subscriberNumber), subscriberNumber, message);
            }

            var number = ulong.Parse($"{(ushort)countryCode}{(byte)trialIdentificationCode}{subscriberNumber?.ToString() ?? string.Empty}", NumberStyles, CultureInfo.InvariantCulture);
            var numberStructure = new ItuE164InternationalNumberStructureForTrials(number);
            return new ItuE164InternationalNumber(numberStructure);
        }

        /// <summary>
        /// Parses the specified string representation of an ITU E.164 international number
        /// and returns its equivalent <see cref="ItuE164InternationalNumber"/> structure.
        /// </summary>
        /// <param name="s">The string representation of the ITU E.164 international number to parse.</param>
        /// <returns>
        /// An <see cref="ItuE164InternationalNumber"/> structure equivalent to the number
        /// contained in <paramref name="s"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="s"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when <paramref name="s"/> is not in a valid ITU E.164 international number format.
        /// </exception>
        public static ItuE164InternationalNumber Parse(string s)
        {
            return Parse(s, ItuE164InternationalNumberStyles.None);
        }

        /// <summary>
        /// Parses the specified string representation of an ITU E.164 international number
        /// using the provided <see cref="ItuE164InternationalNumberStyles"/> and returns
        /// an equivalent <see cref="ItuE164InternationalNumber"/> instance.
        /// </summary>
        /// <param name="s">The string representation of the ITU E.164 international number to parse.</param>
        /// <param name="numberStyles">
        /// A bitwise combination of enumeration values that specify the permitted styles for parsing.
        /// </param>
        /// <returns>An <see cref="ItuE164InternationalNumber"/> instance that represents the parsed number.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the input string <paramref name="s"/> is <c>null</c>.
        /// </exception>
        /// <exception cref="FormatException">
        /// Thrown when the input string <paramref name="s"/> is not in a valid ITU E.164 format
        /// or does not conform to the specified <paramref name="numberStyles"/>.
        /// </exception>
        public static ItuE164InternationalNumber Parse(string s, ItuE164InternationalNumberStyles numberStyles)
        {
            ItuE164InternationalNumber result;
            if (ItuE164InternationalNumberStructureForGeographicAreas.TryParse(s, numberStyles, out var numberForGeographicAreasResult) && numberForGeographicAreasResult != null)
            {
                result = new ItuE164InternationalNumber(numberForGeographicAreasResult);
            }
            else if (ItuE164InternationalNumberStructureForGlobalServices.TryParse(s, numberStyles, out var numberForGlobalServicesResult) && numberForGlobalServicesResult != null)
            {
                result = new ItuE164InternationalNumber(numberForGlobalServicesResult);
            }
            else if (ItuE164InternationalNumberStructureForNetworks.TryParse(s, numberStyles, out var numberForNetworksResult) && numberForNetworksResult != null)
            {
                result = new ItuE164InternationalNumber(numberForNetworksResult);
            }
            else if (ItuE164InternationalNumberStructureForGroupsOfCountries.TryParse(s, numberStyles, out var numberForGroupsOfCountriesResult) && numberForGroupsOfCountriesResult != null)
            {
                result = new ItuE164InternationalNumber(numberForGroupsOfCountriesResult);
            }
            else if (ItuE164InternationalNumberStructureForTrials.TryParse(s, numberStyles, out var numberForTrialsResult) && numberForTrialsResult != null)
            {
                result = new ItuE164InternationalNumber(numberForTrialsResult);
            }
            else
            {
                throw new FormatException($"The input string '{s}' was not in a correct format.");
            }

            return result;
        }

        /// <summary>
        /// Attempts to parse the specified string representation of an ITU E.164 international number.
        /// </summary>
        /// <param name="s">The string representation of the ITU E.164 international number to parse.</param>
        /// <param name="result">
        /// When this method returns, contains the parsed <see cref="ItuE164InternationalNumber"/> if the parsing succeeded,
        /// or the default value of <see cref="ItuE164InternationalNumber"/> if the parsing failed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the string was successfully parsed into an <see cref="ItuE164InternationalNumber"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryParse(string s, out ItuE164InternationalNumber result)
        {
            return TryParse(s, ItuE164InternationalNumberStyles.None, out result);
        }

        /// <summary>
        /// Attempts to parse the specified string representation of an ITU E.164 international number
        /// into an <see cref="ItuE164InternationalNumber"/> instance.
        /// </summary>
        /// <param name="s">
        /// The string representation of the ITU E.164 international number to parse.
        /// </param>
        /// <param name="numberStyles">
        /// A combination of one or more <see cref="ItuE164InternationalNumberStyles"/> values that specify the permitted format of <paramref name="s"/>.
        /// </param>
        /// <param name="result">
        /// When this method returns, contains the parsed <see cref="ItuE164InternationalNumber"/> if the parsing succeeded,
        /// or the default value of <see cref="ItuE164InternationalNumber"/> if the parsing failed.
        /// </param>
        /// <returns>
        /// <c>true</c> if the parsing succeeded and <paramref name="s"/> was successfully converted to an
        /// <see cref="ItuE164InternationalNumber"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool TryParse(string s, ItuE164InternationalNumberStyles numberStyles, out ItuE164InternationalNumber result)
        {
            var isParsed = false;
            if (ItuE164InternationalNumberStructureForGeographicAreas.TryParse(s, numberStyles, out var numberForGeographicAreasResult) && numberForGeographicAreasResult != null)
            {
                result = new ItuE164InternationalNumber(numberForGeographicAreasResult);
                isParsed = true;
            }
            else if (ItuE164InternationalNumberStructureForGlobalServices.TryParse(s, numberStyles, out var numberForGlobalServicesResult) && numberForGlobalServicesResult != null)
            {
                result = new ItuE164InternationalNumber(numberForGlobalServicesResult);
                isParsed = true;
            }
            else if (ItuE164InternationalNumberStructureForNetworks.TryParse(s, numberStyles, out var numberForNetworksResult) && numberForNetworksResult != null)
            {
                result = new ItuE164InternationalNumber(numberForNetworksResult);
                isParsed = true;
            }
            else if (ItuE164InternationalNumberStructureForGroupsOfCountries.TryParse(s, numberStyles, out var numberForGroupsOfCountriesResult) && numberForGroupsOfCountriesResult != null)
            {
                result = new ItuE164InternationalNumber(numberForGroupsOfCountriesResult);
                isParsed = true;
            }
            else if (ItuE164InternationalNumberStructureForTrials.TryParse(s, numberStyles, out var numberForTrialsResult) && numberForTrialsResult != null)
            {
                result = new ItuE164InternationalNumber(numberForTrialsResult);
                isParsed = true;
            }
            else
            {
                result = default;
            }

            return isParsed;
        }

        #endregion
    }
}