using System;

namespace DataStandardizer.Money
{
    internal partial class CurrencyFormatData
    {
        // An alias or parent chain is a few steps long; the bound only guards against a cycle in the data.
        private const int MaximumResolutionSteps = 16;

        private const char SubtagSeparator = '-';
        private const char SortSuffixSeparator = '_';
        private const int ScriptSubtagLength = 4;
        private const int AlphaRegionSubtagLength = 2;
        private const int NumericRegionSubtagLength = 3;

        #region Internal Methods

        /// <summary>
        /// Apply the CLDR currency formatting information of a culture to a <see cref="CurrencyFormatInfo"/>.
        /// </summary>
        /// <param name="cultureName">Name of the culture, as <see cref="System.Globalization.CultureInfo.Name"/> gives it.</param>
        /// <param name="currencyFormat">Information to update, which holds the invariant values on entry.</param>
        /// <remarks>
        /// <para>
        /// The presentation comes from the locale the name resolves to under CLDR inheritance: the locale
        /// itself, the locale its likely subtags name where its script differs from the language's own, its
        /// explicit CLDR parent, or the name with its last subtag removed, in that order and repeatedly. A
        /// name that resolves to the root locale, or to nothing, keeps the invariant presentation.
        /// </para>
        /// <para>
        /// The currency is that of the region the name contains, and the number of decimal digits is the
        /// number of ISO 4217 minor units of that currency, as CLDR applies them. A name with no region, such
        /// as a neutral culture, denotes no currency.
        /// </para>
        /// </remarks>
        internal static void Apply(string cultureName, CurrencyFormatInfo currencyFormat)
        {
            var localeName = RemoveSortSuffix(cultureName);

            var format = Resolve(localeName);
            if (format != null)
            {
                currencyFormat.CurrencyDecimalSeparator = format.DecimalSeparator;
                currencyFormat.CurrencyGroupSeparator = format.GroupSeparator;
                currencyFormat.CurrencyGroupSizes = format.GroupSizes;
                currencyFormat.CurrencyPositivePattern = format.PositivePattern;
                currencyFormat.CurrencyNegativePattern = format.NegativePattern;
                currencyFormat.NegativeSign = format.NegativeSign;
                currencyFormat.CurrencyDecimalDigits = format.FractionDigits;
            }

            var region = GetRegion(localeName);
            if (region != null && RegionCurrencies.TryGetValue(region, out var currency))
            {
                currencyFormat.CurrencyCode = currency.ToString();

                var minorUnits = currency.GetMinorUnits();
                if (minorUnits.HasValue)
                {
                    currencyFormat.CurrencyDecimalDigits = minorUnits.Value;
                }
            }
        }

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private static Format? Resolve(string localeName)
#else
        private static Format Resolve(string localeName)
#endif
        {
            var name = localeName;
            for (var step = 0; step < MaximumResolutionSteps && name.Length != 0; step++)
            {
                if (LocaleFormats.TryGetValue(name, out var formatIndex))
                {
                    return Formats[formatIndex];
                }

                if (LocaleAliases.TryGetValue(name, out var alias))
                {
                    name = alias;
                    continue;
                }

                if (ParentLocales.TryGetValue(name, out var parent))
                {
                    name = parent;
                    continue;
                }

                name = GetTruncatedParent(name);
            }

            return null;
        }

        /// <summary>
        /// Remove the last subtag of a locale name, applying the CLDR rule that a language written in a
        /// script other than its likely script inherits from root rather than from the language.
        /// </summary>
        private static string GetTruncatedParent(string name)
        {
            var separatorIndex = name.LastIndexOf(SubtagSeparator);
            if (separatorIndex < 0)
            {
                return string.Empty;
            }

            var parent = name.Substring(0, separatorIndex);
            var lastSubtag = name.Substring(separatorIndex + 1);
            var isLanguageAndScript = parent.IndexOf(SubtagSeparator) < 0 && lastSubtag.Length == ScriptSubtagLength;
            if (isLanguageAndScript
                && (!LikelyScripts.TryGetValue(parent, out var likelyScript)
                    || !string.Equals(likelyScript, lastSubtag, StringComparison.OrdinalIgnoreCase)))
            {
                return string.Empty;
            }

            return parent;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? GetRegion(string localeName)
#else
        private static string GetRegion(string localeName)
#endif
        {
            // The region follows the language and an optional script: two letters, or three digits.
            var subtags = localeName.Split(SubtagSeparator);
            for (var index = 1; index < subtags.Length; index++)
            {
                var subtag = subtags[index];
                if (index == 1 && subtag.Length == ScriptSubtagLength)
                {
                    continue;
                }

                return IsRegionSubtag(subtag) ? subtag : null;
            }

            return null;
        }

        private static bool IsRegionSubtag(string subtag)
        {
            if (subtag.Length == AlphaRegionSubtagLength)
            {
                return IsAsciiLetter(subtag[0]) && IsAsciiLetter(subtag[1]);
            }

            if (subtag.Length == NumericRegionSubtagLength)
            {
                return IsAsciiDigit(subtag[0]) && IsAsciiDigit(subtag[1]) && IsAsciiDigit(subtag[2]);
            }

            return false;
        }

        private static bool IsAsciiLetter(char character)
        {
            return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
        }

        private static bool IsAsciiDigit(char character)
        {
            return character >= '0' && character <= '9';
        }

        private static string RemoveSortSuffix(string cultureName)
        {
            // A Windows alternate sort order, such as de-DE_phoneb, does not change how amounts are presented.
            var suffixIndex = cultureName.IndexOf(SortSuffixSeparator);
            return suffixIndex < 0 ? cultureName : cultureName.Substring(0, suffixIndex);
        }

        #endregion

        /// <summary>
        /// The currency formatting information of a CLDR locale.
        /// </summary>
        private sealed class Format
        {
            internal Format(string decimalSeparator, string groupSeparator, int[] groupSizes, int positivePattern, int negativePattern, string negativeSign, int fractionDigits)
            {
                DecimalSeparator = decimalSeparator;
                GroupSeparator = groupSeparator;
                GroupSizes = groupSizes;
                PositivePattern = positivePattern;
                NegativePattern = negativePattern;
                NegativeSign = negativeSign;
                FractionDigits = fractionDigits;
            }

            internal string DecimalSeparator { get; }

            internal string GroupSeparator { get; }

            internal int[] GroupSizes { get; }

            internal int PositivePattern { get; }

            internal int NegativePattern { get; }

            internal string NegativeSign { get; }

            internal int FractionDigits { get; }
        }
    }
}
