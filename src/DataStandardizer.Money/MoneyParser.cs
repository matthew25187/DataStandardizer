using System;
using System.Globalization;

namespace DataStandardizer.Money
{
    /// <summary>
    /// Parses the string representation of a monetary value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A currency is identified from an ISO 4217 currency code, which is never ambiguous, or from a currency
    /// symbol. Most symbols denote exactly one currency and are resolved without reference to any culture;
    /// the few which are shared -- <c>$</c> is used by some thirty currencies -- are resolved from the
    /// currency of the culture supplied, and only when the caller has opted in to that.
    /// </para>
    /// <para>
    /// Where a currency cannot be identified with certainty, parsing fails. Choosing a currency on a value's
    /// behalf would silently misstate the amount it represents.
    /// </para>
    /// </remarks>
    internal static class MoneyParser
    {
        private const int CurrencyCodeLength = 3;

        /// <summary>
        /// Attempt to parse the string representation of a monetary value.
        /// </summary>
        /// <param name="value">String to parse.</param>
        /// <param name="styles">Elements permitted in <paramref name="value"/>.</param>
        /// <param name="currencyFormat">Currency formatting information of the culture to parse for.</param>
        /// <param name="numberFormat">Number formatting information used to interpret the amount.</param>
        /// <param name="result">Monetary value parsed from <paramref name="value"/>.</param>
        /// <returns><c>true</c> if <paramref name="value"/> was parsed successfully; otherwise <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string? value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, NumberFormatInfo numberFormat, out Money result)
#else
        internal static bool TryParse(string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, NumberFormatInfo numberFormat, out Money result)
#endif
        {
            result = default(Money);

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var remaining = value;

            remaining = TrimWhite(remaining, styles, out var hadLeadingWhite, out var hadTrailingWhite);
            if ((hadLeadingWhite && (styles & MoneyStyles.AllowLeadingWhite) == 0)
                || (hadTrailingWhite && (styles & MoneyStyles.AllowTrailingWhite) == 0))
            {
                return false;
            }

            // Parentheses denote a negative value, and enclose the whole of it.
            var isNegative = false;
            if (remaining.Length > 1 && remaining[0] == '(' && remaining[remaining.Length - 1] == ')')
            {
                if ((styles & MoneyStyles.AllowParentheses) == 0)
                {
                    return false;
                }

                isNegative = true;
                remaining = remaining.Substring(1, remaining.Length - 2).Trim();
            }

            // A sign may appear outside the currency token as well as beside the amount, so it is looked for
            // both before and after the currency token is removed.
            if (!TryExtractSign(ref remaining, styles, currencyFormat, ref isNegative))
            {
                return false;
            }

            remaining = remaining.Trim();

            if (!TryExtractCurrency(ref remaining, styles, currencyFormat, out var currencyCode, out var hasCurrency))
            {
                return false;
            }

            remaining = remaining.Trim();

            if (!TryExtractSign(ref remaining, styles, currencyFormat, ref isNegative))
            {
                return false;
            }

            remaining = remaining.Trim();

            if (!TryParseAmount(remaining, styles, numberFormat, out var amount))
            {
                return false;
            }

            if (isNegative)
            {
                amount = decimal.Negate(amount);
            }

            result = hasCurrency ? Money.Create(amount, currencyCode) : Money.Create(amount);
            return true;
        }

        #region Private Methods

        private static string TrimWhite(string value, MoneyStyles styles, out bool hadLeadingWhite, out bool hadTrailingWhite)
        {
            var trimmed = value.Trim();
            hadLeadingWhite = trimmed.Length != value.Length && !value.StartsWith(trimmed, StringComparison.Ordinal);
            hadTrailingWhite = trimmed.Length != value.Length && !value.EndsWith(trimmed, StringComparison.Ordinal);

            // A value which is white space at both ends satisfies neither test above on its own.
            if (trimmed.Length != value.Length)
            {
                hadLeadingWhite = value.Length > 0 && char.IsWhiteSpace(value[0]);
                hadTrailingWhite = value.Length > 0 && char.IsWhiteSpace(value[value.Length - 1]);
            }

            return trimmed;
        }

        /// <summary>
        /// Remove the currency token from a value and identify the currency it denotes.
        /// </summary>
        private static bool TryExtractCurrency(ref string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, out Iso4217CurrencyCurrent currencyCode, out bool hasCurrency)
        {
            currencyCode = default(Iso4217CurrencyCurrent);
            hasCurrency = false;

            // An ISO 4217 currency code identifies a currency without ambiguity.
            if (TryExtractCurrencyCode(ref value, out var extractedCode))
            {
                if ((styles & MoneyStyles.AllowCurrencyCode) == 0)
                {
                    return false;
                }

                currencyCode = extractedCode;
                hasCurrency = true;
                return true;
            }

            // Symbols are matched longest first so that a longer symbol is preferred over a shorter one
            // which forms part of it, such as NZ$ over $.
            foreach (var symbol in CurrencySymbols.SymbolsByDescendingLength)
            {
                if (!TryRemoveAffix(ref value, symbol, out var withoutSymbol))
                {
                    continue;
                }

                if ((styles & MoneyStyles.AllowCurrencySymbol) == 0)
                {
                    return false;
                }

                if (CurrencySymbols.TryGetUnambiguousCurrency(symbol, out var unambiguousCurrency))
                {
                    value = withoutSymbol;
                    currencyCode = unambiguousCurrency;
                    hasCurrency = true;
                    return true;
                }

                // A shared symbol is resolved only from the currency of the culture being parsed for, and
                // only when the caller has accepted that.
                if ((styles & MoneyStyles.AllowAmbiguousCurrencySymbol) == 0)
                {
                    return false;
                }

                if (!CurrencySymbols.TryGetAmbiguousCurrencies(symbol, out var candidates) || candidates is null)
                {
                    return false;
                }

                if (!Enum.TryParse(currencyFormat.CurrencyCode, false, out Iso4217CurrencyCurrent cultureCurrency))
                {
                    return false;
                }

                foreach (var candidate in candidates)
                {
                    if (candidate != cultureCurrency)
                    {
                        continue;
                    }

                    value = withoutSymbol;
                    currencyCode = candidate;
                    hasCurrency = true;
                    return true;
                }

                // The culture does not account for this symbol, so the currency cannot be determined.
                return false;
            }

            // No currency token at all is permitted; the value simply carries no currency.
            return true;
        }

        private static bool TryExtractCurrencyCode(ref string value, out Iso4217CurrencyCurrent currencyCode)
        {
            currencyCode = default(Iso4217CurrencyCurrent);

            if (value.Length < CurrencyCodeLength)
            {
                return false;
            }

            // A currency code appears at one end of the value or the other.
            var leading = value.Substring(0, CurrencyCodeLength);
            if (IsCurrencyCode(leading, out currencyCode))
            {
                value = value.Substring(CurrencyCodeLength);
                return true;
            }

            var trailing = value.Substring(value.Length - CurrencyCodeLength);
            if (IsCurrencyCode(trailing, out currencyCode))
            {
                value = value.Substring(0, value.Length - CurrencyCodeLength);
                return true;
            }

            return false;
        }

        private static bool IsCurrencyCode(string candidate, out Iso4217CurrencyCurrent currencyCode)
        {
            currencyCode = default(Iso4217CurrencyCurrent);

            foreach (var character in candidate)
            {
                if (character < 'A' || character > 'Z')
                {
                    return false;
                }
            }

            return Enum.TryParse(candidate, false, out currencyCode)
                   && Enum.IsDefined(typeof(Iso4217CurrencyCurrent), currencyCode)
                   && Money.IsCurrencyCodeValidForMoneyValue(currencyCode);
        }

        private static bool TryRemoveAffix(ref string value, string affix, out string withoutAffix)
        {
            withoutAffix = value;

            if (affix.Length == 0 || value.Length < affix.Length)
            {
                return false;
            }

            if (value.StartsWith(affix, StringComparison.Ordinal))
            {
                withoutAffix = value.Substring(affix.Length);
                return true;
            }

            if (value.EndsWith(affix, StringComparison.Ordinal))
            {
                withoutAffix = value.Substring(0, value.Length - affix.Length);
                return true;
            }

            return false;
        }

        private static bool TryExtractSign(ref string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, ref bool isNegative)
        {
            var negativeSign = string.IsNullOrEmpty(currencyFormat.NegativeSign) ? "-" : currencyFormat.NegativeSign;

            if (value.StartsWith(negativeSign, StringComparison.Ordinal))
            {
                if ((styles & MoneyStyles.AllowLeadingSign) == 0)
                {
                    return false;
                }

                isNegative = !isNegative;
                value = value.Substring(negativeSign.Length);
                return true;
            }

            if (value.EndsWith(negativeSign, StringComparison.Ordinal))
            {
                if ((styles & MoneyStyles.AllowTrailingSign) == 0)
                {
                    return false;
                }

                isNegative = !isNegative;
                value = value.Substring(0, value.Length - negativeSign.Length);
                return true;
            }

            return true;
        }

        private static bool TryParseAmount(string value, MoneyStyles styles, NumberFormatInfo numberFormat, out decimal amount)
        {
            amount = decimal.Zero;

            if (value.Length == 0)
            {
                return false;
            }

            var numberStyles = NumberStyles.None;
            if ((styles & MoneyStyles.AllowThousands) != 0)
            {
                numberStyles |= NumberStyles.AllowThousands;
            }

            if ((styles & MoneyStyles.AllowDecimalPoint) != 0)
            {
                numberStyles |= NumberStyles.AllowDecimalPoint;
            }

            return decimal.TryParse(value, numberStyles, numberFormat, out amount);
        }

        #endregion
    }
}
