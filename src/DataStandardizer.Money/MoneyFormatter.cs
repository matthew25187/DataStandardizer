using System;
using System.Globalization;

namespace DataStandardizer.Money
{
    /// <summary>
    /// Provides functionality to format monetary values into various string representations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements the <see cref="ICustomFormatter"/> interface to allow custom formatting of
    /// <see cref="Money"/> values. It supports the currency symbol, narrow currency symbol, currency code
    /// and currency name forms, as well as a general form which emits no currency token at all.
    /// </para>
    /// <para>
    /// The culture supplies presentation only: separators, group sizes, the placement of the currency token
    /// and the sign. Which currency is denoted, and the default precision, come from the value being
    /// formatted, so that formatting a value under a foreign culture cannot misstate its currency.
    /// </para>
    /// </remarks>
    internal sealed class MoneyFormatter : ICustomFormatter
    {
        private static class Specifier
        {
            internal const char CurrencySymbol = 'C';
            internal const char NarrowCurrencySymbol = 'H';
            internal const char CurrencyCode = 'I';
            internal const char CurrencyName = 'N';
            internal const char General = 'G';
        }

        // Placement templates matching the indices documented for NumberFormatInfo.CurrencyPositivePattern
        // and NumberFormatInfo.CurrencyNegativePattern, so that values taken from a culture may be used
        // directly. {0} is the currency token, {1} the number and {2} the negative sign.
        private static readonly string[] PositivePatterns =
        {
            "{0}{1}",       // 0: $n
            "{1}{0}",       // 1: n$
            "{0} {1}",      // 2: $ n
            "{1} {0}"       // 3: n $
        };

        private static readonly string[] NegativePatterns =
        {
            "({0}{1})",     //  0: ($n)
            "{2}{0}{1}",    //  1: -$n
            "{0}{2}{1}",    //  2: $-n
            "{0}{1}{2}",    //  3: $n-
            "({1}{0})",     //  4: (n$)
            "{2}{1}{0}",    //  5: -n$
            "{1}{2}{0}",    //  6: n-$
            "{1}{0}{2}",    //  7: n$-
            "{2}{1} {0}",   //  8: -n $
            "{2}{0} {1}",   //  9: -$ n
            "{1} {0}{2}",   // 10: n $-
            "{0} {1}{2}",   // 11: $ n-
            "{0} {2}{1}",   // 12: $ -n
            "{1}{2} {0}",   // 13: n- $
            "({0} {1})",    // 14: ($ n)
            "({1} {0})"     // 15: (n $)
        };

        #region Public Methods

        /// <summary>
        /// Convert a monetary value to its equivalent string representation using the specified format and
        /// culture-specific formatting information.
        /// </summary>
        /// <param name="format">A format string containing formatting specifications.</param>
        /// <param name="arg">The value to format.</param>
        /// <param name="formatProvider">An object that supplies format information about the current instance.</param>
        /// <returns>The string representation of <paramref name="arg"/>, formatted as specified.</returns>
        /// <exception cref="FormatException">
        /// Thrown when <paramref name="format"/> names a currency other than that of <paramref name="arg"/>.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
#else
        public string Format(string format, object arg, IFormatProvider formatProvider)
#endif
        {
            if (!(arg is Money moneyValue))
            {
                return HandleOtherFormats(format, arg, formatProvider);
            }

            var currencyFormat = formatProvider?.GetFormat(typeof(CurrencyFormatInfo)) as CurrencyFormatInfo
#if NETSTANDARD2_0_OR_GREATER||NET
                                 ?? CurrencyFormatInfo.CurrentInfo;
#else
                                 ?? CurrencyFormatInfo.InvariantInfo;
#endif

            if (!TryParseFormat(format, moneyValue.IsoCurrencyCode, out var specifier, out var precision))
            {
                // Not a recognised money format; a custom numeric format string may still apply.
                return HandleOtherFormats(format, arg, formatProvider);
            }

            // The general form is the amount alone, formatted as the underlying number would be.
            if (specifier == Specifier.General)
            {
                return HandleOtherFormats(format, arg, formatProvider);
            }

            // A value with no currency has no currency token to emit. The format string is deliberately not
            // passed on to the number: a currency specifier handed to decimal would emit the culture's own
            // currency symbol, which would misrepresent a value that has no currency of its own.
            if (moneyValue.IsoCurrencyCode == Money.NoCurrency)
            {
                var currencylessAmount = (decimal)moneyValue;
                var currencylessIsNegative = currencylessAmount < decimal.Zero;
                var currencylessDigits = precision ?? currencyFormat.CurrencyDecimalDigits;
                var currencylessNumberPart = FormatNumber(currencylessIsNegative ? decimal.Negate(currencylessAmount) : currencylessAmount, currencylessDigits, currencyFormat);

                return currencylessIsNegative
                    ? string.Concat(currencyFormat.NegativeSign, currencylessNumberPart)
                    : currencylessNumberPart;
            }

            var currencyToken = ResolveCurrencyToken(specifier, moneyValue.IsoCurrencyCode);

            // An explicit precision is preferred, then the minor units of the currency being formatted, and
            // only then the culture's own default. The currency outranks the culture because the number of
            // minor units is a property of the currency rather than a presentation preference.
            var currencyDecimalDigits = precision ?? moneyValue.CurrencyMinorUnits ?? currencyFormat.CurrencyDecimalDigits;

            // The rounded value is formatted, so that any rounding the value carries is honoured.
            var amount = (decimal)moneyValue;
            var isNegative = amount < decimal.Zero;

            var numberPart = FormatNumber(amount < decimal.Zero ? decimal.Negate(amount) : amount, currencyDecimalDigits, currencyFormat);
            var patterns = isNegative ? NegativePatterns : PositivePatterns;
            var patternIndex = isNegative ? currencyFormat.CurrencyNegativePattern : currencyFormat.CurrencyPositivePattern;
            if (patternIndex < 0 || patternIndex >= patterns.Length)
            {
                patternIndex = 0;
            }

            return string.Format(CultureInfo.InvariantCulture, patterns[patternIndex], currencyToken, numberPart, currencyFormat.NegativeSign);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Interpret a money format string.
        /// </summary>
        /// <param name="format">Format string to interpret.</param>
        /// <param name="currencyCode">Currency of the value being formatted.</param>
        /// <param name="specifier">Format specifier, normalised to upper case.</param>
        /// <param name="precision">Precision specified by <paramref name="format"/>, if any.</param>
        /// <returns><c>true</c> if <paramref name="format"/> is a money format string; otherwise <c>false</c>.</returns>
        /// <exception cref="FormatException">
        /// Thrown when <paramref name="format"/> names a currency other than <paramref name="currencyCode"/>.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        private static bool TryParseFormat(string? format, Iso4217CurrencyCurrent currencyCode, out char specifier, out int? precision)
#else
        private static bool TryParseFormat(string format, Iso4217CurrencyCurrent currencyCode, out char specifier, out int? precision)
#endif
        {
            specifier = Specifier.General;
            precision = null;

            // An absent format is the general form, matching the convention for numeric types.
            if (string.IsNullOrEmpty(format))
            {
                return true;
            }

            // A currency code names the currency of the value and emits it; it does not select a currency.
            if (TryParseCurrencyCodeFormat(format, out var formatCurrencyCode, out precision))
            {
                if (formatCurrencyCode != currencyCode)
                {
                    throw new FormatException($"The format string '{format}' does not match the currency {currencyCode} of the value being formatted.");
                }

                specifier = Specifier.CurrencyCode;
                return true;
            }

            var candidateSpecifier = char.ToUpperInvariant(format[0]);
            if (candidateSpecifier != Specifier.CurrencySymbol
                && candidateSpecifier != Specifier.NarrowCurrencySymbol
                && candidateSpecifier != Specifier.CurrencyCode
                && candidateSpecifier != Specifier.CurrencyName
                && candidateSpecifier != Specifier.General)
            {
                return false;
            }

            if (!TryParsePrecision(format, 1, out precision))
            {
                // The specifier is a money format specifier, so a malformed precision is an error rather
                // than grounds to reinterpret the format string as a custom numeric format. Falling through
                // would hand a currency specifier to the number formatter, which would emit the culture's
                // own currency symbol in place of that of the value.
                throw new FormatException($"The format string '{format}' does not specify a valid precision.");
            }

            specifier = candidateSpecifier;
            return true;
        }

        /// <summary>
        /// Interpret a format string consisting of an ISO 4217 currency code and an optional precision.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        private static bool TryParseCurrencyCodeFormat(string format, out Iso4217CurrencyCurrent currencyCode, out int? precision)
#else
        private static bool TryParseCurrencyCodeFormat(string format, out Iso4217CurrencyCurrent currencyCode, out int? precision)
#endif
        {
            currencyCode = default(Iso4217CurrencyCurrent);
            precision = null;

            const int currencyCodeLength = 3;
            if (format.Length < currencyCodeLength)
            {
                return false;
            }

            for (var index = 0; index < currencyCodeLength; index++)
            {
                var character = format[index];
                if (character < 'A' || character > 'Z')
                {
                    return false;
                }
            }

            if (!TryParsePrecision(format, currencyCodeLength, out precision))
            {
                return false;
            }

            return Enum.TryParse(format.Substring(0, currencyCodeLength), false, out currencyCode)
                   && Enum.IsDefined(typeof(Iso4217CurrencyCurrent), currencyCode);
        }

        /// <summary>
        /// Interpret the precision specifier which may follow a format specifier.
        /// </summary>
        private static bool TryParsePrecision(string format, int startIndex, out int? precision)
        {
            precision = null;
            if (format.Length == startIndex)
            {
                return true;
            }

            // Two digits is the practical bound for a monetary precision, and rejecting longer runs keeps
            // pathological input away from the number formatter.
            var precisionLength = format.Length - startIndex;
            if (precisionLength > 2)
            {
                return false;
            }

            if (!int.TryParse(format.Substring(startIndex), NumberStyles.None, CultureInfo.InvariantCulture, out var precisionValue))
            {
                return false;
            }

            precision = precisionValue;
            return true;
        }

        /// <summary>
        /// Determine the token which denotes the currency of a value.
        /// </summary>
        private static string ResolveCurrencyToken(char specifier, Iso4217CurrencyCurrent currencyCode)
        {
            var code = Enum.GetName(typeof(Iso4217CurrencyCurrent), currencyCode) ?? string.Empty;
            switch (specifier)
            {
                case Specifier.CurrencySymbol:
                    return currencyCode.GetCurrencySymbol(CurrencySymbolKind.Standard) ?? code;

                case Specifier.NarrowCurrencySymbol:
                    return currencyCode.GetCurrencySymbol(CurrencySymbolKind.Narrow) ?? code;

                case Specifier.CurrencyName:
                    return currencyCode.GetCurrencyName() ?? code;

                default:
                    return code;
            }
        }

        /// <summary>
        /// Format the numeric part of a monetary value.
        /// </summary>
        /// <remarks>
        /// Grouping and rounding are delegated to <see cref="decimal"/> so that they match the behaviour of
        /// the intrinsic numeric types, including variable group sizes such as those of the Indian numbering
        /// system. Only the placement of the currency token is applied by this class.
        /// </remarks>
        private static string FormatNumber(decimal amount, int currencyDecimalDigits, CurrencyFormatInfo currencyFormat)
        {
            var numberFormat = new NumberFormatInfo
            {
                NumberDecimalDigits = currencyDecimalDigits,
                NumberDecimalSeparator = currencyFormat.CurrencyDecimalSeparator,
                NumberGroupSeparator = currencyFormat.CurrencyGroupSeparator,
                NumberGroupSizes = currencyFormat.CurrencyGroupSizes
            };

            return amount.ToString("N", numberFormat);
        }

        /// <summary>
        /// Format an argument which is not a monetary value, or a monetary value whose format string is not
        /// a money format string.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        private static string HandleOtherFormats(string? format, object? arg, IFormatProvider? formatProvider)
#else
        private static string HandleOtherFormats(string format, object arg, IFormatProvider formatProvider)
#endif
        {
            // A monetary value is converted before being formatted. Formatting it as IFormattable would call
            // back into Money.ToString, which resolves this formatter again and would not terminate.
            if (arg is Money moneyArg)
            {
                return ((decimal)moneyArg).ToString(format, formatProvider ?? CultureInfo.CurrentCulture);
            }

            if (arg is IFormattable formattableArg)
            {
                return formattableArg.ToString(format, formatProvider ?? CultureInfo.CurrentCulture);
            }

            if (arg != null)
            {
#if NETCOREAPP3_0_OR_GREATER
                return arg.ToString() ?? string.Empty;
#else
                return arg.ToString();
#endif
            }

            return string.Empty;
        }

        #endregion
    }
}
