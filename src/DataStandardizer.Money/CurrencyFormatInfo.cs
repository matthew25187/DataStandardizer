using System;
using System.Globalization;

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
        private const Iso4217CurrencyCurrent InvariantCurrencyCode = Iso4217CurrencyCurrent.XXX;
        private const int InvariantCurrencyDecimalDigits = 2;
        private const string InvariantCurrencyDecimalSeparator = ".";
        private const string InvariantCurrencyGroupSeparator = ",";
        private static readonly int[] InvariantCurrencyGroupSizes = { 3 };
        private const int MaximumGroupSize = 9;
        private const int InvariantCurrencyNegativePattern = 0;
        private const int InvariantCurrencyPositivePattern = 0;
        private const string InvariantNegativeSign = "-";

        // The largest values NumberFormatInfo accepts for the properties of the same names.
        private const int MaximumCurrencyDecimalDigits = 99;
        private const int MaximumCurrencyNegativePattern = 16;
        private const int MaximumCurrencyPositivePattern = 3;

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
        /// <para>
        /// The value is exactly the three upper-case letters of a code <see cref="Iso4217CurrencyCurrent"/>
        /// defines. A numeric code, a code in another case or surrounded by white space, and a list of codes are
        /// all rejected.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the property is set to <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the property is set to a value which is not the alphabetic code of a currency
        /// <see cref="Iso4217CurrencyCurrent"/> defines.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public string CurrencyCode
        {
            get => _currencyCode;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!Iso4217Extensions.TryGetCurrentCurrency(value, out _))
                {
                    throw new ArgumentException("The value must be the alphabetic code of a current ISO 4217 currency.", nameof(value));
                }

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
        /// As with <see cref="NumberFormatInfo.CurrencyDecimalDigits"/>, the value is from 0 to 99.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the property is set to a value less than 0 or greater than 99.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set this property on a read-only <see cref="CurrencyFormatInfo"/> instance.
        /// </exception>
        public int CurrencyDecimalDigits
        {
            get => _currencyDecimalDigits;
            set
            {
                if (value < 0 || value > MaximumCurrencyDecimalDigits)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be from 0 to 99.");
                }

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
        /// As with <see cref="NumberFormatInfo.CurrencyDecimalSeparator"/>, it can't be empty.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the property is set to <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the property is set to an empty string.
        /// </exception>
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

                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value.Length == 0)
                {
                    throw new ArgumentException("The decimal separator cannot be the empty string.", nameof(value));
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
        /// <exception cref="ArgumentNullException">
        /// Thrown when the property is set to <see langword="null"/>.
        /// </exception>
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

                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
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
        /// <para>
        /// As with <see cref="NumberFormatInfo.NumberGroupSizes"/>, the getter returns a copy of the sizes and the
        /// setter stores a copy of the array it is given, so changing either array afterwards does not change
        /// this instance. The setter accepts the same sizes as <see cref="NumberFormatInfo.NumberGroupSizes"/>:
        /// every element is from 1 to 9, except the last, which may also be 0 to show that the remaining digits
        /// are not grouped.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the property is set to <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an element of the array is less than 1 or greater than 9, other than a last element of 0.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public int[] CurrencyGroupSizes
        {
            get => (int[])_currencyGroupSizes.Clone();
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (IsReadOnly)
                {
                    throw new InvalidOperationException($"{nameof(CurrencyFormatInfo)} is read only.");
                }

                // The copy is the array that is checked and stored, so a change the caller makes to their own
                // array afterwards can bypass neither.
                var groupSizes = (int[])value.Clone();
                CheckGroupSizes(groupSizes, nameof(value));
                _currencyGroupSizes = groupSizes;
            }
        }

        /// <summary>
        /// Gets the array that holds the group sizes, without copying it.
        /// </summary>
        /// <remarks>
        /// For handing the sizes to <see cref="NumberFormatInfo.NumberGroupSizes"/>, which copies them itself.
        /// The array is never to be changed.
        /// </remarks>
        internal int[] CurrencyGroupSizesUncopied => _currencyGroupSizes;

        /// <summary>
        /// Gets or sets the format pattern for negative currency values.
        /// </summary>
        /// <remarks>
        /// The <see cref="CurrencyNegativePattern"/> property determines how negative currency values are formatted.
        /// The value is an integer that corresponds to a specific pattern, using the same indices as
        /// <see cref="NumberFormatInfo.CurrencyNegativePattern"/>, from 0 to 16. For example:
        /// - 0: ($n)
        /// - 1: -$n
        /// - 2: $-n
        /// - 3: $n-
        /// - 16: $- n
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the property is set to a value less than 0 or greater than 16.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public int CurrencyNegativePattern
        {
            get => _currencyNegativePattern;
            set
            {
                if (value < 0 || value > MaximumCurrencyNegativePattern)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be from 0 to 16.");
                }

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
        /// <see cref="NumberFormatInfo.CurrencyPositivePattern"/>, from 0 to 3.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the property is set to a value less than 0 or greater than 3.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property on a read-only <see cref="CurrencyFormatInfo"/> instance.
        /// </exception>
        public int CurrencyPositivePattern
        {
            get => _currencyPositivePattern;
            set
            {
                if (value < 0 || value > MaximumCurrencyPositivePattern)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "The value must be from 0 to 3.");
                }

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
        /// <exception cref="ArgumentNullException">
        /// Thrown when the property is set to <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when attempting to set the property while the <see cref="CurrencyFormatInfo"/> instance is read-only.
        /// </exception>
        public string NegativeSign
        {
            get => _negativeSign;
            set
            {
                if (value is null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

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
        /// Create a <see cref="CurrencyFormatInfo"/> from the Unicode CLDR data for a culture.
        /// </summary>
        /// <param name="culture">Culture whose information is required, or <c>null</c> for the culture-independent information.</param>
        /// <returns>A writable <see cref="CurrencyFormatInfo"/> for <paramref name="culture"/>.</returns>
        /// <remarks>
        /// <para>
        /// The information depends only on the name of <paramref name="culture"/>, never on the culture data of
        /// the host, so it is the same on every operating system and target. The presentation is that of the
        /// CLDR locale the name resolves to, and the currency is that of the region the name contains. See
        /// <c>CurrencyFormatData.Apply</c>.
        /// </para>
        /// <para>
        /// The result is writable; sealing it is the responsibility of whatever takes ownership of it, so that
        /// a shared instance is never made writable again by a caller.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        internal static CurrencyFormatInfo CreateForCulture(CultureInfo? culture)
#else
        internal static CurrencyFormatInfo CreateForCulture(CultureInfo culture)
#endif
        {
            // A new instance holds the invariant information, which is what a null culture is given.
            return culture != null ? CreateForCultureName(culture.Name) : new CurrencyFormatInfo();
        }

        /// <summary>
        /// Create a <see cref="CurrencyFormatInfo"/> from the Unicode CLDR data for a culture name.
        /// </summary>
        /// <param name="cultureName">Name of the culture, as <see cref="CultureInfo.Name"/> gives it.</param>
        /// <returns>A writable <see cref="CurrencyFormatInfo"/> for <paramref name="cultureName"/>.</returns>
        /// <remarks>
        /// A name that CLDR resolves to root, or to no locale at all, such as the invariant culture's empty
        /// name, keeps the invariant presentation.
        /// </remarks>
        internal static CurrencyFormatInfo CreateForCultureName(string cultureName)
        {
            var result = new CurrencyFormatInfo();
            CurrencyFormatData.Apply(cultureName, result);
            return result;
        }

        #endregion

        #region Private Methods

        private static void CheckGroupSizes(int[] groupSizes, string parameterName)
        {
            // The same rule as NumberFormatInfo.NumberGroupSizes, which receives these sizes when a value is
            // formatted: each size is from 1 to 9, and only the last may be 0.
            for (var index = 0; index < groupSizes.Length; index++)
            {
                var groupSize = groupSizes[index];
                var isLast = index == groupSizes.Length - 1;
                if (groupSize > MaximumGroupSize || groupSize < (isLast ? 0 : 1))
                {
                    throw new ArgumentException(
                        "Every element in the array must be from 1 to 9, except the last, which may also be 0.",
                        parameterName);
                }
            }
        }

        #endregion
    }
}
