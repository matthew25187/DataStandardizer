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
        private const string HyphenMinus = "-";

        /// <summary>
        /// The outcome of an attempt to parse a value with one set of separators and sign.
        /// </summary>
        private enum ParseOutcome
        {
            /// <summary>The value is not a monetary value.</summary>
            Failed,

            /// <summary>The value was parsed.</summary>
            Parsed,

            /// <summary>The value has no currency token, so its amount is to be read with other separators.</summary>
            NoCurrencyToken
        }

        /// <summary>
        /// Attempt to parse the string representation of a monetary value.
        /// </summary>
        /// <param name="value">String to parse.</param>
        /// <param name="styles">Elements permitted in <paramref name="value"/>.</param>
        /// <param name="currencyFormat">Currency formatting information of the culture to parse for.</param>
        /// <param name="currencyNumberFormat">Number formatting information with the separators and negative sign of <paramref name="currencyFormat"/>, used to interpret an amount with a currency token.</param>
        /// <param name="amountNumberFormat">Number formatting information used by the general format, used to interpret an amount without a currency token.</param>
        /// <param name="standardSymbolsFirst">
        /// <c>true</c> to resolve a shared symbol that is the standard symbol of exactly one currency to that
        /// currency, as for a value known to be in the standard symbol form; otherwise <c>false</c>.
        /// </param>
        /// <param name="result">Monetary value parsed from <paramref name="value"/>.</param>
        /// <param name="readAsStandardSymbol">
        /// <c>true</c> if the currency of <paramref name="result"/> was decided by reading a shared symbol as a
        /// standard symbol, so that reading without <paramref name="standardSymbolsFirst"/> could differ;
        /// otherwise <c>false</c>, in which case it would not.
        /// </param>
        /// <returns><c>true</c> if <paramref name="value"/> was parsed successfully; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// An amount is read with the separators and sign it was formatted with. A value with a currency token
        /// is in one of the money forms, which use those of the currency. A value without one is in the
        /// general form, or is a value with no currency, and both use those of the number formatting.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string? value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, NumberFormatInfo currencyNumberFormat, NumberFormatInfo amountNumberFormat, bool standardSymbolsFirst, out Money result, out bool readAsStandardSymbol)
#else
        internal static bool TryParse(string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, NumberFormatInfo currencyNumberFormat, NumberFormatInfo amountNumberFormat, bool standardSymbolsFirst, out Money result, out bool readAsStandardSymbol)
#endif
        {
            result = default(Money);
            readAsStandardSymbol = false;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var currencyNegativeSign = GetNegativeSign(currencyNumberFormat);
            var amountNegativeSign = GetNegativeSign(amountNumberFormat);

            // Where both read the same sign, one reading finds the sign and currency token for either, and the
            // token decides which separators the amount is read with.
            if (string.Equals(currencyNegativeSign, amountNegativeSign, StringComparison.Ordinal))
            {
                return TryParse(value, styles, currencyFormat, standardSymbolsFirst, currencyNegativeSign, currencyNumberFormat, amountNumberFormat, out result, out readAsStandardSymbol) == ParseOutcome.Parsed;
            }

            switch (TryParse(value, styles, currencyFormat, standardSymbolsFirst, currencyNegativeSign, currencyNumberFormat, null, out result, out readAsStandardSymbol))
            {
                case ParseOutcome.Parsed:
                    return true;

                case ParseOutcome.NoCurrencyToken:
                    // Read again with the sign of the number formatting, which must not then find a currency
                    // token that the reading with the currency's sign did not.
                    return TryParse(value, styles, currencyFormat, standardSymbolsFirst, amountNegativeSign, null, amountNumberFormat, out result, out readAsStandardSymbol) == ParseOutcome.Parsed;

                default:
                    return false;
            }
        }

        #region Private Methods

        /// <summary>
        /// Determine the negative sign read for number formatting information, which is a hyphen-minus where
        /// it has none, since an empty sign cannot be recognised in input.
        /// </summary>
        private static string GetNegativeSign(NumberFormatInfo numberFormat)
        {
            return string.IsNullOrEmpty(numberFormat.NegativeSign) ? HyphenMinus : numberFormat.NegativeSign;
        }

        /// <summary>
        /// Attempt to parse the string representation of a monetary value with one negative sign.
        /// </summary>
        /// <param name="value">String to parse, which is neither <c>null</c> nor empty.</param>
        /// <param name="styles">Elements permitted in <paramref name="value"/>.</param>
        /// <param name="currencyFormat">Currency formatting information of the culture to parse for.</param>
        /// <param name="standardSymbolsFirst"><c>true</c> to resolve a symbol from the standard symbols first; otherwise <c>false</c>.</param>
        /// <param name="negativeSign">Negative sign to read.</param>
        /// <param name="tokenNumberFormat">
        /// Number formatting information whose separators are used where <paramref name="value"/> has a
        /// currency token, or <c>null</c> to fail where it has one.
        /// </param>
        /// <param name="tokenlessNumberFormat">
        /// Number formatting information whose separators are used where <paramref name="value"/> has no
        /// currency token, or <c>null</c> to stop with <see cref="ParseOutcome.NoCurrencyToken"/> where it has none.
        /// </param>
        /// <param name="result">Monetary value parsed from <paramref name="value"/>.</param>
        /// <param name="readAsStandardSymbol"><c>true</c> if the currency was decided by reading a shared symbol as a standard symbol; otherwise <c>false</c>.</param>
        /// <returns>The outcome of the attempt.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private static ParseOutcome TryParse(string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, bool standardSymbolsFirst, string negativeSign, NumberFormatInfo? tokenNumberFormat, NumberFormatInfo? tokenlessNumberFormat, out Money result, out bool readAsStandardSymbol)
#else
        private static ParseOutcome TryParse(string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, bool standardSymbolsFirst, string negativeSign, NumberFormatInfo tokenNumberFormat, NumberFormatInfo tokenlessNumberFormat, out Money result, out bool readAsStandardSymbol)
#endif
        {
            result = default(Money);
            readAsStandardSymbol = false;

            var remaining = value;

            remaining = TrimWhite(remaining, styles, out var hadLeadingWhite, out var hadTrailingWhite);
            if ((hadLeadingWhite && (styles & MoneyStyles.AllowLeadingWhite) == 0)
                || (hadTrailingWhite && (styles & MoneyStyles.AllowTrailingWhite) == 0))
            {
                return ParseOutcome.Failed;
            }

            // Parentheses denote a negative value, and enclose the whole of it.
            var isNegative = false;
            if (remaining.Length > 1 && remaining[0] == '(' && remaining[remaining.Length - 1] == ')')
            {
                if ((styles & MoneyStyles.AllowParentheses) == 0)
                {
                    return ParseOutcome.Failed;
                }

                isNegative = true;
                remaining = remaining.Substring(1, remaining.Length - 2).Trim();
            }

            // A sign may appear outside the currency token as well as beside the amount, so it is looked for
            // both before and after the currency token is removed.
            if (!TryExtractSign(ref remaining, styles, negativeSign, ref isNegative))
            {
                return ParseOutcome.Failed;
            }

            remaining = remaining.Trim();

            if (!TryExtractCurrency(ref remaining, styles, currencyFormat, standardSymbolsFirst, out var currencyCode, out var hasCurrency, out var currencyReadAsStandardSymbol))
            {
                return ParseOutcome.Failed;
            }

            var numberFormat = hasCurrency ? tokenNumberFormat : tokenlessNumberFormat;
            if (numberFormat is null)
            {
                return hasCurrency ? ParseOutcome.Failed : ParseOutcome.NoCurrencyToken;
            }

            remaining = remaining.Trim();

            if (!TryExtractSign(ref remaining, styles, negativeSign, ref isNegative))
            {
                return ParseOutcome.Failed;
            }

            remaining = remaining.Trim();

            if (!TryParseAmount(remaining, styles, numberFormat, out var amount))
            {
                return ParseOutcome.Failed;
            }

            if (isNegative)
            {
                amount = decimal.Negate(amount);
            }

            result = hasCurrency ? Money.Create(amount, currencyCode) : Money.Create(amount);
            readAsStandardSymbol = currencyReadAsStandardSymbol;
            return ParseOutcome.Parsed;
        }

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
        private static bool TryExtractCurrency(ref string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, bool standardSymbolsFirst, out Iso4217CurrencyCurrent currencyCode, out bool hasCurrency, out bool readAsStandardSymbol)
        {
            currencyCode = default(Iso4217CurrencyCurrent);
            hasCurrency = false;
            readAsStandardSymbol = false;

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
                    // A symbol can stand for a code that a Money value cannot carry, such as the generic
                    // currency sign for XXX, so it is held to the same test as a currency code.
                    if (!Money.IsCurrencyCodeValidForMoneyValue(unambiguousCurrency))
                    {
                        return false;
                    }

                    value = withoutSymbol;
                    currencyCode = unambiguousCurrency;
                    hasCurrency = true;
                    return true;
                }

                // In a value known to carry a standard symbol, a shared symbol that is the standard symbol of
                // one currency denotes that currency, though it is also the narrow symbol of others.
                if (standardSymbolsFirst
                    && CurrencySymbols.TryGetSoleStandardCurrency(symbol, out var standardCurrency)
                    && Money.IsCurrencyCodeValidForMoneyValue(standardCurrency))
                {
                    value = withoutSymbol;
                    currencyCode = standardCurrency;
                    hasCurrency = true;
                    readAsStandardSymbol = true;
                    return true;
                }

                // A shared symbol is otherwise resolved only from the currency of the culture being parsed for,
                // and only when the caller has accepted that.
                if ((styles & MoneyStyles.AllowAmbiguousCurrencySymbol) == 0)
                {
                    return false;
                }

                if (!CurrencySymbols.TryGetAmbiguousCurrencies(symbol, out var candidates) || candidates is null)
                {
                    return false;
                }

                if (!Iso4217Extensions.TryGetCurrentCurrency(currencyFormat.CurrencyCode, out var cultureCurrency))
                {
                    return false;
                }

                foreach (var candidate in candidates)
                {
                    if (candidate != cultureCurrency || !Money.IsCurrencyCodeValidForMoneyValue(candidate))
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

        /// <summary>
        /// Remove a negative sign from either end of a value.
        /// </summary>
        /// <remarks>
        /// A value carries at most one sign, and parentheses are a sign, so a sign found once
        /// <paramref name="isNegative"/> is already set fails the parse.
        /// </remarks>
        private static bool TryExtractSign(ref string value, MoneyStyles styles, string negativeSign, ref bool isNegative)
        {
            // As for decimal, a hyphen-minus is also read as the sign where the sign is a single dash such as
            // U+2212 MINUS SIGN, which few keyboards can type.
            var alternativeSign = AllowsHyphenMinus(negativeSign) ? HyphenMinus : null;

            if (StartsWithSign(value, negativeSign, alternativeSign, out var leadingSignLength))
            {
                if ((styles & MoneyStyles.AllowLeadingSign) == 0 || isNegative)
                {
                    return false;
                }

                isNegative = true;
                value = value.Substring(leadingSignLength);
                return true;
            }

            if (EndsWithSign(value, negativeSign, alternativeSign, out var trailingSignLength))
            {
                if ((styles & MoneyStyles.AllowTrailingSign) == 0 || isNegative)
                {
                    return false;
                }

                isNegative = true;
                value = value.Substring(0, value.Length - trailingSignLength);
                return true;
            }

            return true;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static bool StartsWithSign(string value, string negativeSign, string? alternativeSign, out int signLength)
#else
        private static bool StartsWithSign(string value, string negativeSign, string alternativeSign, out int signLength)
#endif
        {
            signLength = value.StartsWith(negativeSign, StringComparison.Ordinal) ? negativeSign.Length
                : alternativeSign != null && value.StartsWith(alternativeSign, StringComparison.Ordinal) ? alternativeSign.Length
                : 0;
            return signLength != 0;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static bool EndsWithSign(string value, string negativeSign, string? alternativeSign, out int signLength)
#else
        private static bool EndsWithSign(string value, string negativeSign, string alternativeSign, out int signLength)
#endif
        {
            signLength = value.EndsWith(negativeSign, StringComparison.Ordinal) ? negativeSign.Length
                : alternativeSign != null && value.EndsWith(alternativeSign, StringComparison.Ordinal) ? alternativeSign.Length
                : 0;
            return signLength != 0;
        }

        /// <summary>
        /// Determine whether a hyphen-minus is also read as a negative sign, by the rule
        /// <see cref="NumberFormatInfo"/> applies when parsing a number.
        /// </summary>
        private static bool AllowsHyphenMinus(string negativeSign)
        {
            if (negativeSign.Length != 1)
            {
                return false;
            }

            switch (negativeSign[0])
            {
                case '\x2012': // FIGURE DASH
                case '\x207B': // SUPERSCRIPT MINUS
                case '\x208B': // SUBSCRIPT MINUS
                case '\x2212': // MINUS SIGN
                case '\x2796': // HEAVY MINUS SIGN
                case '\xFE63': // SMALL HYPHEN-MINUS
                case '\xFF0D': // FULLWIDTH HYPHEN-MINUS
                    return true;

                default:
                    return false;
            }
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
