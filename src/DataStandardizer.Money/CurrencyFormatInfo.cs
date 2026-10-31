using System;
using System.Collections.Generic;
using System.Globalization;
using DataStandardizer.Money.Properties;

namespace DataStandardizer.Money
{
    /// <summary>
    /// Defines how monetary values are formatted and parsed for a culture.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This type is the monetary counterpart of <see cref="NumberFormatInfo"/>, but it deliberately omits
    /// a currency symbol. A <see cref="Money"/> value carries its own currency, so the symbol, the currency
    /// code and the default precision are all determined by the value being formatted rather than by the
    /// culture formatting it. The culture governs presentation only: separators, group sizes, the placement
    /// of the currency token and the sign.
    /// </para>
    /// <para>
    /// <see cref="CurrencyCode"/> is the exception, and exists for parsing rather than formatting: it is
    /// what allows a shared currency symbol such as <c>$</c> to be resolved to a specific currency using
    /// the context of a culture.
    /// </para>
    /// </remarks>
    public sealed class CurrencyFormatInfo : IFormatProvider
    {
        private static readonly char[] CurrencyGroupSizeSeparators = { ',' };

        private const Iso4217CurrencyCurrent InvariantCurrencyCode = Iso4217CurrencyCurrent.XXX;
        private const int InvariantCurrencyDecimalDigits = 2;
        private const string InvariantCurrencyDecimalSeparator = ".";
        private const string InvariantCurrencyGroupSeparator = ",";
        private static readonly int[] InvariantCurrencyGroupSizes = { 3 };
        private const int InvariantCurrencyNegativePattern = 0;
        private const int InvariantCurrencyPositivePattern = 0;
        private const string InvariantNegativeSign = "-";

        /// <summary>
        /// The generic currency sign (U+00A4), used where a monetary value has no currency of its own.
        /// </summary>
        internal const string InvariantCurrencySymbol = "¤";

        private string _currencyCode;
        private int _currencyDecimalDigits;
        private string _currencyDecimalSeparator;
        private string _currencyGroupSeparator;
        private int[] _currencyGroupSizes;
        private int _currencyNegativePattern;
        private int _currencyPositivePattern;
        private string _negativeSign;

#if NETSTANDARD2_0_OR_GREATER||NET
        private static readonly object CurrentInfoLock = new object();
#if NETCOREAPP3_0_OR_GREATER
        private static CurrencyFormatInfo? _cachedCurrentInfo;
        private static string? _cachedCurrentInfoCultureName;
#else
        private static CurrencyFormatInfo _cachedCurrentInfo;
        private static string _cachedCurrentInfoCultureName;
#endif
#endif

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        /// <summary>
        /// Initialises a new instance of the <see cref="CurrencyFormatInfo"/> class with culture-independent values.
        /// </summary>
        public CurrencyFormatInfo()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            CurrencyCode = InvariantCurrencyCode.ToString();
            CurrencyDecimalDigits = InvariantCurrencyDecimalDigits;
            CurrencyDecimalSeparator = InvariantCurrencyDecimalSeparator;
            CurrencyGroupSeparator = InvariantCurrencyGroupSeparator;
            CurrencyGroupSizes = InvariantCurrencyGroupSizes;
            CurrencyNegativePattern = InvariantCurrencyNegativePattern;
            CurrencyPositivePattern = InvariantCurrencyPositivePattern;
            NegativeSign = InvariantNegativeSign;
        }

#if NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets an object that provides currency formatting information for the requested type.
        /// </summary>
        /// <param name="formatType">Type of the format object required.</param>
        /// <returns>The current instance if <paramref name="formatType"/> is <see cref="CurrencyFormatInfo"/>; otherwise <c>null</c>.</returns>
        public object? GetFormat(Type? formatType)
        {
            if (formatType == typeof(CurrencyFormatInfo))
            {
                return this;
            }

            return null;
        }
#else
        /// <summary>
        /// Gets an object that provides currency formatting information for the requested type.
        /// </summary>
        /// <param name="formatType">Type of the format object required.</param>
        /// <returns>The current instance if <paramref name="formatType"/> is <see cref="CurrencyFormatInfo"/>; otherwise <c>null</c>.</returns>
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof(CurrencyFormatInfo))
            {
                return this;
            }

            return null;
        }
#endif

        #region Public Properties

        /// <summary>
        /// Gets or sets the currency code associated with the current <see cref="CurrencyFormatInfo"/> instance.
        /// </summary>
        /// <remarks>
        /// The currency code is the ISO 4217 currency code of the culture's own currency (e.g., "USD" for
        /// US Dollar, "EUR" for Euro). It is not the currency of any value being formatted: a
        /// <see cref="Money"/> value carries its own currency. This property exists so that a currency
        /// symbol shared by several currencies can be resolved when parsing.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _currencyCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of decimal places to use in currency values.
        /// </summary>
        /// <remarks>
        /// This is the culture's default precision, and has the lowest priority when formatting: an explicit
        /// precision specifier is preferred, then the minor units of the currency being formatted, and only
        /// then this value. It therefore applies in practice only to currencies which define no minor units.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set this property on a read-only <see cref="CurrencyFormatInfo"/> instance.
        /// </exception>
        public int CurrencyDecimalDigits
        {
            get => _currencyDecimalDigits;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _currencyDecimalDigits = value;
            }
        }

        /// <summary>
        /// Gets or sets the string that separates the integral and fractional parts of a currency value.
        /// </summary>
        /// <remarks>
        /// This property defines the character used as the decimal separator in currency values.
        /// For example, in the currency value "123.45", the period (".") is the decimal separator.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property on a read-only <see cref="CurrencyFormatInfo"/> instance.
        /// </exception>
        public string CurrencyDecimalSeparator
        {
            get => _currencyDecimalSeparator;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _currencyDecimalSeparator = value;
            }
        }

        /// <summary>
        /// Gets or sets the string that separates groups of digits to the left of the decimal point in currency values.
        /// </summary>
        /// <remarks>
        /// This property is used to define the character or string that separates digit groups in formatted currency values.
        /// For example, in the currency value "1,000.00", the comma (",") is the group separator.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public string CurrencyGroupSeparator
        {
            get => _currencyGroupSeparator;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _currencyGroupSeparator = value;
            }
        }

        /// <summary>
        /// Gets or sets the sizes of the groups of digits to the left of the decimal point in currency values.
        /// </summary>
        /// <remarks>
        /// Each element in the array specifies the number of digits in a group. For example, an array of {3, 2}
        /// indicates that the first group contains three digits, and the subsequent groups contain two digits each,
        /// as used in the Indian numbering system.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public int[] CurrencyGroupSizes
        {
            get => _currencyGroupSizes;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _currencyGroupSizes = value;
            }
        }

        /// <summary>
        /// Gets or sets the format pattern for negative currency values.
        /// </summary>
        /// <remarks>
        /// The <see cref="CurrencyNegativePattern"/> property determines how negative currency values are formatted.
        /// The value is an integer that corresponds to a specific pattern, using the same indices as
        /// <see cref="NumberFormatInfo.CurrencyNegativePattern"/>. For example:
        /// - 0: ($n)
        /// - 1: -$n
        /// - 2: $-n
        /// - 3: $n-
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public int CurrencyNegativePattern
        {
            get => _currencyNegativePattern;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _currencyNegativePattern = value;
            }
        }

        /// <summary>
        /// Gets or sets the format pattern for positive currency values.
        /// </summary>
        /// <remarks>
        /// The <see cref="CurrencyPositivePattern"/> property determines the placement of the currency token
        /// and the formatting of positive currency values, using the same indices as
        /// <see cref="NumberFormatInfo.CurrencyPositivePattern"/>.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property on a read-only <see cref="CurrencyFormatInfo"/> instance.
        /// </exception>
        public int CurrencyPositivePattern
        {
            get => _currencyPositivePattern;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _currencyPositivePattern = value;
            }
        }

        /// <summary>
        /// Gets or sets the string that denotes a negative monetary value.
        /// </summary>
        /// <remarks>
        /// This is not always the ASCII hyphen-minus: several cultures use U+2212 MINUS SIGN, and some
        /// prefix it with a directional mark.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public string NegativeSign
        {
            get => _negativeSign;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                _negativeSign = value;
            }
        }

#if NETSTANDARD2_0_OR_GREATER||NET
        /// <summary>
        /// Gets a read-only <see cref="CurrencyFormatInfo"/> object that represents the currency formatting information
        /// for the current culture.
        /// </summary>
        /// <remarks>
        /// The value is re-evaluated when the current culture changes, so that a culture assigned after this
        /// property has first been read is honoured. The result is cached for as long as the current culture
        /// is unchanged.
        /// </remarks>
        /// <value>
        /// A <see cref="CurrencyFormatInfo"/> object containing the currency formatting information
        /// for the current culture.
        /// </value>
        public static CurrencyFormatInfo CurrentInfo
        {
            get
            {
                var currentCulture = CultureInfo.CurrentCulture;
                var currentCultureName = currentCulture.Name;
                lock (CurrentInfoLock)
                {
                    if (_cachedCurrentInfo != null && _cachedCurrentInfoCultureName == currentCultureName)
                    {
                        return _cachedCurrentInfo;
                    }

                    var currentInfo = CreateForCulture(currentCulture);
                    currentInfo.IsReadOnly = true;

                    _cachedCurrentInfo = currentInfo;
                    _cachedCurrentInfoCultureName = currentCultureName;
                    return currentInfo;
                }
            }
        }
#endif
        /// <summary>
        /// Gets a read-only <see cref="CurrencyFormatInfo"/> object that is culture-independent (invariant).
        /// </summary>
        /// <remarks>
        /// The <see cref="InvariantInfo"/> property provides a default, culture-independent representation of currency formatting.
        /// It uses predefined settings such as the invariant decimal separator, group separator, and patterns.
        /// This property is useful when consistent formatting is required regardless of the current culture.
        /// </remarks>
        public static CurrencyFormatInfo InvariantInfo { get; } = new CurrencyFormatInfo { IsReadOnly = true };

        /// <summary>
        /// Gets a value indicating whether the <see cref="CurrencyFormatInfo"/> object is read-only.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="CurrencyFormatInfo"/> object is read-only; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// When the object is read-only, any attempt to modify its properties will result in an <see cref="InvalidOperationException"/>.
        /// </remarks>
        public bool IsReadOnly { get; internal set; }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Create a <see cref="CurrencyFormatInfo"/> from the resources of a culture.
        /// </summary>
        /// <param name="culture">Culture whose resources are to be loaded, or <c>null</c> for the neutral resources.</param>
        /// <returns>A writable <see cref="CurrencyFormatInfo"/> for <paramref name="culture"/>.</returns>
        /// <remarks>
        /// The result is writable; sealing it is the responsibility of whatever takes ownership of it, so that
        /// a shared instance is never made writable again by a caller.
        /// </remarks>
        /// <exception cref="MissingCultureResourceException">
        /// Thrown when a required resource is absent from both the culture's resources and the neutral resources.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        internal static CurrencyFormatInfo CreateForCulture(CultureInfo? culture)
#else
        internal static CurrencyFormatInfo CreateForCulture(CultureInfo culture)
#endif
        {
            var cultureName = culture != null ? culture.Name : string.Empty;

            // Resource values are looked up with an explicit culture rather than by assigning the resource
            // manager's culture, which is shared static state and cannot be mutated safely from several
            // threads at once.
            var currencyCodeText = GetResource(nameof(Resources.CurrencyCode), culture);
            if (!Enum.TryParse(currencyCodeText, false, out Iso4217CurrencyCurrent currencyCode) || !Enum.IsDefined(typeof(Iso4217CurrencyCurrent), currencyCode))
            {
                throw new MissingCultureResourceException(cultureName, nameof(Resources.CurrencyCode));
            }

            // The culture's default precision falls back to the minor units of its own currency.
            if (!int.TryParse(GetResource(nameof(Resources.CurrencyDecimalDigits), culture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var currencyDecimalDigits))
            {
                var minorUnits = currencyCode.GetMinorUnits();
                if (!minorUnits.HasValue)
                {
                    throw new MissingCultureResourceException(cultureName, nameof(Resources.CurrencyDecimalDigits));
                }

                currencyDecimalDigits = minorUnits.Value;
            }

            if (!int.TryParse(GetResource(nameof(Resources.CurrencyNegativePattern), culture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var currencyNegativePattern))
            {
                throw new MissingCultureResourceException(cultureName, nameof(Resources.CurrencyNegativePattern));
            }

            if (!int.TryParse(GetResource(nameof(Resources.CurrencyPositivePattern), culture), NumberStyles.Integer, CultureInfo.InvariantCulture, out var currencyPositivePattern))
            {
                throw new MissingCultureResourceException(cultureName, nameof(Resources.CurrencyPositivePattern));
            }

            return new CurrencyFormatInfo
            {
                CurrencyCode = currencyCodeText,
                CurrencyDecimalDigits = currencyDecimalDigits,
                CurrencyDecimalSeparator = GetResource(nameof(Resources.CurrencyDecimalSeparator), culture),
                CurrencyGroupSeparator = GetResource(nameof(Resources.CurrencyGroupSeparator), culture),
                CurrencyGroupSizes = ParseGroupSizes(GetResource(nameof(Resources.CurrencyGroupSizes), culture)),
                CurrencyNegativePattern = currencyNegativePattern,
                CurrencyPositivePattern = currencyPositivePattern,
                NegativeSign = GetResource(nameof(Resources.NegativeSign), culture)
            };
        }

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private static string GetResource(string resourceName, CultureInfo? culture)
#else
        private static string GetResource(string resourceName, CultureInfo culture)
#endif
        {
            // A null culture means the neutral resources, but the resource manager interprets it as the
            // current culture, so the invariant culture is named explicitly to reach the neutral values.
            var value = Resources.ResourceManager.GetString(resourceName, culture ?? CultureInfo.InvariantCulture);
            if (string.IsNullOrEmpty(value))
            {
                // A culture-specific file which defines a key as empty would otherwise shadow the neutral value.
                value = Resources.ResourceManager.GetString(resourceName, CultureInfo.InvariantCulture);
            }

            return value ?? string.Empty;
        }

        private static int[] ParseGroupSizes(string groupSizesText)
        {
            var groupSizeTexts = groupSizesText.Split(CurrencyGroupSizeSeparators, StringSplitOptions.RemoveEmptyEntries);
            var groupSizes = new List<int>(groupSizeTexts.Length);
            foreach (var groupSizeText in groupSizeTexts)
            {
                if (int.TryParse(groupSizeText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var groupSize))
                {
                    groupSizes.Add(groupSize);
                }
            }

            return groupSizes.ToArray();
        }

        #endregion
    }
}
