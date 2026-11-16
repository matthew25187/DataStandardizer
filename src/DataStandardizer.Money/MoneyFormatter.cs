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

        /// <summary>
        /// The outcome of interpreting a format string.
        /// </summary>
        private enum FormatParseResult
        {
            /// <summary>
            /// The format string is a money format string.
            /// </summary>
            Recognised,

            /// <summary>
            /// The format string is not a money format string, and may be a custom numeric format string.
            /// </summary>
            NotRecognised,

            /// <summary>
            /// The format string names a currency other than that of the value being formatted.
            /// </summary>
            CurrencyMismatch,

            /// <summary>
            /// The format string is a money format string whose precision is not valid.
            /// </summary>
            InvalidPrecision
        }

        /// <summary>
        /// The formatter, which holds no state, so that one instance serves every caller.
        /// </summary>
        internal static readonly MoneyFormatter Instance = new MoneyFormatter();

        // The standard numeric format of a grouped number, to which a precision may be appended.
        private const string GroupedNumberFormat = "N";

        // The greatest number of digits a decimal holds after the decimal point.
        private const int MaximumDecimalScale = 28;

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
            "({1} {0})",    // 15: (n $)
            "{0}{2} {1}"    // 16: $- n
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

            // The culture of the current thread on every target, as for the number formatting, so that the same
            // inputs give the same output whichever target is in use.
            var currencyFormat = formatProvider?.GetFormat(typeof(CurrencyFormatInfo)) as CurrencyFormatInfo
                                 ?? MoneyInfo.GetCurrentMoney().CurrencyFormat;

            switch (ParseFormat(format, moneyValue.IsoCurrencyCode, out var specifier, out var precision))
            {
                case FormatParseResult.Recognised:
                    break;

                case FormatParseResult.CurrencyMismatch:
                    throw new FormatException($"The format string '{format}' does not match the currency {moneyValue.IsoCurrencyCode} of the value being formatted.");

                case FormatParseResult.InvalidPrecision:
                    throw new FormatException($"The format string '{format}' does not specify a valid precision.");

                default:
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
            //
            // Without a currency token the result cannot be told apart from the general format, so it takes
            // the separators and sign the general format uses rather than those of the currency. Parsing reads
            // an amount without a currency token with those same separators and sign.
            if (moneyValue.IsoCurrencyCode == Money.NoCurrency)
            {
                var amountNumberFormat = NumberFormatInfo.GetInstance(formatProvider ?? CultureInfo.CurrentCulture);
                var currencylessDigits = precision ?? currencyFormat.CurrencyDecimalDigits;
                var currencylessAmount = RoundForDisplay((decimal)moneyValue, currencylessDigits);
                var currencylessIsNegative = currencylessAmount < decimal.Zero;

                // The amount is formatted without its sign, so the negative pattern of the number formatting
                // never applies and only its separators and group sizes are used.
                var currencylessNumberPart = (currencylessIsNegative ? decimal.Negate(currencylessAmount) : currencylessAmount)
                    .ToString(GroupedNumberFormat + currencylessDigits.ToString(CultureInfo.InvariantCulture), amountNumberFormat);

                return currencylessIsNegative
                    ? string.Concat(amountNumberFormat.NegativeSign, currencylessNumberPart)
                    : currencylessNumberPart;
            }

            var currencyToken = ResolveCurrencyToken(specifier, moneyValue.IsoCurrencyCode);

            // An explicit precision is preferred, then the minor units of the currency being formatted, and
            // only then the culture's own default. The currency outranks the culture because the number of
            // minor units is a property of the currency rather than a presentation preference.
            var currencyDecimalDigits = precision ?? moneyValue.CurrencyMinorUnits ?? currencyFormat.CurrencyDecimalDigits;

            // The rounded value is formatted, so that any rounding the value carries is honoured.
            var amount = RoundForDisplay((decimal)moneyValue, currencyDecimalDigits);
            var isNegative = amount < decimal.Zero;

            var numberPart = FormatNumber(isNegative ? decimal.Negate(amount) : amount, currencyDecimalDigits, currencyFormat);
            // CurrencyFormatInfo accepts only the indices these tables hold.
            var pattern = isNegative
                ? NegativePatterns[currencyFormat.CurrencyNegativePattern]
                : PositivePatterns[currencyFormat.CurrencyPositivePattern];

            return string.Format(CultureInfo.InvariantCulture, pattern, currencyToken, numberPart, currencyFormat.NegativeSign);
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
        /// <returns>The outcome of interpreting <paramref name="format"/>.</returns>
        /// <remarks>
        /// The outcome is reported to the caller rather than raised, so that the decision to treat a format
        /// string as an error belongs to the method which was asked to format the value.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        private static FormatParseResult ParseFormat(string? format, Iso4217CurrencyCurrent currencyCode, out char specifier, out int? precision)
#else
        private static FormatParseResult ParseFormat(string format, Iso4217CurrencyCurrent currencyCode, out char specifier, out int? precision)
#endif
        {
            specifier = Specifier.General;
            precision = null;

            // An absent format is the general form, matching the convention for numeric types.
            if (string.IsNullOrEmpty(format))
            {
                return FormatParseResult.Recognised;
            }

            // A currency code names the currency of the value and emits it; it does not select a currency.
            if (TryParseCurrencyCodeFormat(format, out var formatCurrencyCode, out precision))
            {
                if (formatCurrencyCode != currencyCode)
                {
                    return FormatParseResult.CurrencyMismatch;
                }

                specifier = Specifier.CurrencyCode;
                return FormatParseResult.Recognised;
            }

            var candidateSpecifier = char.ToUpperInvariant(format[0]);
            if (candidateSpecifier != Specifier.CurrencySymbol
                && candidateSpecifier != Specifier.NarrowCurrencySymbol
                && candidateSpecifier != Specifier.CurrencyCode
                && candidateSpecifier != Specifier.CurrencyName
                && candidateSpecifier != Specifier.General)
            {
                return FormatParseResult.NotRecognised;
            }

            if (!TryParsePrecision(format, 1, out precision))
            {
                // The specifier is a money format specifier, so a malformed precision is an error rather
                // than grounds to reinterpret the format string as a custom numeric format. Treating it as
                // unrecognised would hand a currency specifier to the number formatter, which would emit the
                // culture's own currency symbol in place of that of the value.
                return FormatParseResult.InvalidPrecision;
            }

            specifier = candidateSpecifier;
            return FormatParseResult.Recognised;
        }

        /// <summary>
        /// Determine whether a format string is the standard symbol format, <c>C</c> with an optional precision.
        /// </summary>
        /// <param name="format">Format string to examine.</param>
        /// <returns><c>true</c> if <paramref name="format"/> renders a value with the standard symbol of its currency; otherwise <c>false</c>.</returns>
        internal static bool IsStandardSymbolFormat(string format)
        {
            // The currency only decides whether a currency code format such as CAD matches the value, and such
            // a format is never the standard symbol format, so any currency serves.
            return ParseFormat(format, default(Iso4217CurrencyCurrent), out var specifier, out _) == FormatParseResult.Recognised
                   && specifier == Specifier.CurrencySymbol;
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

            return Iso4217Extensions.TryGetCurrentCurrency(format.Substring(0, currencyCodeLength), out currencyCode);
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
        /// Round an amount to the precision it is displayed at.
        /// </summary>
        /// <remarks>
        /// Both the digits and the sign of a formatted amount are taken from the amount this returns, so that
        /// they always agree: a negative amount which rounds to zero is displayed as zero with the positive
        /// pattern, as <see cref="decimal"/> displays it. Midpoints are rounded away from zero, as the
        /// <see cref="decimal"/> number format rounds them.
        /// </remarks>
        private static decimal RoundForDisplay(decimal amount, int decimalDigits)
        {
            // A decimal has at most 28 digits after the decimal point, so a greater precision cannot round it.
            var rounded = Math.Round(amount, Math.Min(decimalDigits, MaximumDecimalScale), MidpointRounding.AwayFromZero);

            // A negative amount which rounds to zero keeps its sign bit, which the number format would otherwise
            // be relied upon to drop.
            return rounded == decimal.Zero ? decimal.Zero : rounded;
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
                NumberGroupSizes = currencyFormat.CurrencyGroupSizesUncopied
            };

            return amount.ToString(GroupedNumberFormat, numberFormat);
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
            // back into Money.ToString, which calls this formatter again and would not terminate.
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
