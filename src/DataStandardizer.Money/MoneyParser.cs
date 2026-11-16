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
    /// <para>
    /// The elements around the amount are read as <see cref="NumberStyles"/> reads those of a number: a
    /// leading part of white space, sign, opening parenthesis and currency token before the amount, and a
    /// trailing part of white space, sign, closing parenthesis and currency token after it.
    /// </para>
    /// </remarks>
    internal static class MoneyParser
    {
        private const int CurrencyCodeLength = 3;
        private const string HyphenMinus = "-";

        // The NumberFormatInfo.NumberNegativePattern "- n", under which NumberStyles reads white space between
        // a leading sign and the number.
        private const int SpaceAfterSignNumberNegativePattern = 2;

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
        /// The outcome of looking for a currency token at one end of a value.
        /// </summary>
        private enum TokenOutcome
        {
            /// <summary>There is no currency token there.</summary>
            NotFound,

            /// <summary>A currency token was read.</summary>
            Read,

            /// <summary>There is a currency token there that the styles do not permit or that cannot be resolved.</summary>
            Rejected
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

            // As for decimal, the number pattern of the culture decides whether white space may follow a sign
            // which no currency token precedes.
            var allowsWhiteAfterSign = amountNumberFormat.NumberNegativePattern == SpaceAfterSignNumberNegativePattern;

            // Where both read the same sign, one reading finds the sign and currency token for either, and the
            // token decides which separators the amount is read with.
            if (string.Equals(currencyNegativeSign, amountNegativeSign, StringComparison.Ordinal))
            {
                return TryParse(value, styles, currencyFormat, standardSymbolsFirst, currencyNegativeSign, allowsWhiteAfterSign, currencyNumberFormat, amountNumberFormat, out result, out readAsStandardSymbol) == ParseOutcome.Parsed;
            }

            switch (TryParse(value, styles, currencyFormat, standardSymbolsFirst, currencyNegativeSign, allowsWhiteAfterSign, currencyNumberFormat, null, out result, out readAsStandardSymbol))
            {
                case ParseOutcome.Parsed:
                    return true;

                case ParseOutcome.NoCurrencyToken:
                    // Read again with the sign of the number formatting, which must not then find a currency
                    // token that the reading with the currency's sign did not.
                    return TryParse(value, styles, currencyFormat, standardSymbolsFirst, amountNegativeSign, allowsWhiteAfterSign, null, amountNumberFormat, out result, out readAsStandardSymbol) == ParseOutcome.Parsed;

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
        /// <param name="allowsWhiteAfterSign">
        /// <c>true</c> if white space may follow a leading sign or opening parenthesis which no currency token
        /// precedes, as under the number pattern <c>- n</c>; otherwise <c>false</c>.
        /// </param>
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
        /// <remarks>
        /// The value is read in place, from each end towards the amount, and only the amount is passed on.
        /// A value carries at most one sign, and parentheses are a sign. Parentheses enclose the whole of the
        /// value, with only white space outside them.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        private static ParseOutcome TryParse(string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, bool standardSymbolsFirst, string negativeSign, bool allowsWhiteAfterSign, NumberFormatInfo? tokenNumberFormat, NumberFormatInfo? tokenlessNumberFormat, out Money result, out bool readAsStandardSymbol)
#else
        private static ParseOutcome TryParse(string value, MoneyStyles styles, CurrencyFormatInfo currencyFormat, bool standardSymbolsFirst, string negativeSign, bool allowsWhiteAfterSign, NumberFormatInfo tokenNumberFormat, NumberFormatInfo tokenlessNumberFormat, out Money result, out bool readAsStandardSymbol)
#endif
        {
            result = default(Money);
            readAsStandardSymbol = false;

            // As for decimal, a hyphen-minus is also read as the sign where the sign is a single dash such as
            // U+2212 MINUS SIGN, which few keyboards can type.
            var alternativeSign = AllowsHyphenMinus(negativeSign) ? HyphenMinus : null;

            var start = 0;
            var end = value.Length;
            var isNegative = false;
            var hasParentheses = false;
            var hasCurrency = false;
            var currencyCode = default(Iso4217CurrencyCurrent);
            var currencyReadAsStandardSymbol = false;
            int length;

            // The leading part. As for decimal, white space may not follow a sign or opening parenthesis until
            // a currency token has been read, so "- 5" and "( 5)" are rejected while "-$ 5" is read.
            while (start < end)
            {
                if (IsWhite(value[start]))
                {
                    if ((styles & MoneyStyles.AllowLeadingWhite) == 0 || (isNegative && !hasCurrency && !allowsWhiteAfterSign))
                    {
                        return ParseOutcome.Failed;
                    }

                    start++;
                    continue;
                }

                if (!isNegative && IsSignAt(value, start, end, true, negativeSign, alternativeSign, out length))
                {
                    if ((styles & MoneyStyles.AllowLeadingSign) == 0)
                    {
                        return ParseOutcome.Failed;
                    }

                    isNegative = true;
                    start += length;
                    continue;
                }

                if (!isNegative && !hasCurrency && value[start] == '(')
                {
                    if ((styles & MoneyStyles.AllowParentheses) == 0)
                    {
                        return ParseOutcome.Failed;
                    }

                    isNegative = true;
                    hasParentheses = true;
                    start++;
                    continue;
                }

                if (!hasCurrency)
                {
                    var tokenOutcome = TryReadCurrency(value, start, end, true, styles, currencyFormat, standardSymbolsFirst, out length, out currencyCode, out currencyReadAsStandardSymbol);
                    if (tokenOutcome == TokenOutcome.Rejected)
                    {
                        return ParseOutcome.Failed;
                    }

                    if (tokenOutcome == TokenOutcome.Read)
                    {
                        hasCurrency = true;
                        start += length;
                        continue;
                    }
                }

                break;
            }

            // The trailing part, read from the end. As for decimal, white space may appear anywhere in it.
            var hasClosingParenthesis = false;
            var hasTrailingElement = false;
            while (end > start)
            {
                if (IsWhite(value[end - 1]))
                {
                    if ((styles & MoneyStyles.AllowTrailingWhite) == 0)
                    {
                        return ParseOutcome.Failed;
                    }

                    end--;
                    continue;
                }

                if (hasParentheses && !hasClosingParenthesis && !hasTrailingElement && value[end - 1] == ')')
                {
                    hasClosingParenthesis = true;
                    end--;
                    continue;
                }

                if (!isNegative && IsSignAt(value, start, end, false, negativeSign, alternativeSign, out length))
                {
                    if ((styles & MoneyStyles.AllowTrailingSign) == 0)
                    {
                        return ParseOutcome.Failed;
                    }

                    isNegative = true;
                    hasTrailingElement = true;
                    end -= length;
                    continue;
                }

                if (!hasCurrency)
                {
                    var tokenOutcome = TryReadCurrency(value, start, end, false, styles, currencyFormat, standardSymbolsFirst, out length, out currencyCode, out currencyReadAsStandardSymbol);
                    if (tokenOutcome == TokenOutcome.Rejected)
                    {
                        return ParseOutcome.Failed;
                    }

                    if (tokenOutcome == TokenOutcome.Read)
                    {
                        hasCurrency = true;
                        hasTrailingElement = true;
                        end -= length;
                        continue;
                    }
                }

                break;
            }

            if (hasParentheses && !hasClosingParenthesis)
            {
                return ParseOutcome.Failed;
            }

            var numberFormat = hasCurrency ? tokenNumberFormat : tokenlessNumberFormat;
            if (numberFormat is null)
            {
                return hasCurrency ? ParseOutcome.Failed : ParseOutcome.NoCurrencyToken;
            }

            if (!TryParseAmount(value, start, end - start, styles, numberFormat, out var amount))
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

        /// <summary>
        /// Determine whether a character is white space as <see cref="NumberStyles"/> defines it: U+0009 to
        /// U+000D and U+0020, and no other character that Unicode classes as white space.
        /// </summary>
        private static bool IsWhite(char character)
        {
            return character == ' ' || (character >= '\t' && character <= '\r');
        }

        /// <summary>
        /// Determine whether a string appears at one end of a range of a value.
        /// </summary>
        /// <param name="value">Value to examine.</param>
        /// <param name="start">Start of the range.</param>
        /// <param name="end">End of the range, exclusive.</param>
        /// <param name="atStart"><c>true</c> to look at the start of the range; <c>false</c> to look at its end.</param>
        /// <param name="affix">String to look for, which is not empty.</param>
        private static bool IsAffixAt(string value, int start, int end, bool atStart, string affix)
        {
            if (affix.Length > end - start)
            {
                return false;
            }

            var index = atStart ? start : end - affix.Length;
            return string.CompareOrdinal(value, index, affix, 0, affix.Length) == 0;
        }

        /// <summary>
        /// Look for a currency token at one end of a range of a value and identify the currency it denotes.
        /// </summary>
        /// <param name="value">Value to examine.</param>
        /// <param name="start">Start of the range.</param>
        /// <param name="end">End of the range, exclusive.</param>
        /// <param name="atStart"><c>true</c> to look at the start of the range; <c>false</c> to look at its end.</param>
        /// <param name="styles">Elements permitted in <paramref name="value"/>.</param>
        /// <param name="currencyFormat">Currency formatting information of the culture to parse for.</param>
        /// <param name="standardSymbolsFirst"><c>true</c> to resolve a symbol from the standard symbols first; otherwise <c>false</c>.</param>
        /// <param name="length">Length of the currency token read.</param>
        /// <param name="currencyCode">Currency the token denotes.</param>
        /// <param name="readAsStandardSymbol"><c>true</c> if the currency was decided by reading a shared symbol as a standard symbol; otherwise <c>false</c>.</param>
        /// <returns>The outcome of the attempt.</returns>
        private static TokenOutcome TryReadCurrency(string value, int start, int end, bool atStart, MoneyStyles styles, CurrencyFormatInfo currencyFormat, bool standardSymbolsFirst, out int length, out Iso4217CurrencyCurrent currencyCode, out bool readAsStandardSymbol)
        {
            length = 0;
            currencyCode = default(Iso4217CurrencyCurrent);
            readAsStandardSymbol = false;

            // An ISO 4217 currency code identifies a currency without ambiguity.
            if (TryReadCurrencyCode(value, start, end, atStart, out var extractedCode))
            {
                if ((styles & MoneyStyles.AllowCurrencyCode) == 0)
                {
                    return TokenOutcome.Rejected;
                }

                length = CurrencyCodeLength;
                currencyCode = extractedCode;
                return TokenOutcome.Read;
            }

            // Symbols are matched longest first so that a longer symbol is preferred over a shorter one
            // which forms part of it, such as NZ$ over $.
            foreach (var symbol in CurrencySymbols.SymbolsByDescendingLength)
            {
                if (symbol.Length == 0 || !IsAffixAt(value, start, end, atStart, symbol))
                {
                    continue;
                }

                if ((styles & MoneyStyles.AllowCurrencySymbol) == 0)
                {
                    return TokenOutcome.Rejected;
                }

                if (CurrencySymbols.TryGetUnambiguousCurrency(symbol, out var unambiguousCurrency))
                {
                    // A symbol can stand for a code that a Money value cannot carry, such as the generic
                    // currency sign for XXX, so it is held to the same test as a currency code.
                    if (!Money.IsCurrencyCodeValidForMoneyValue(unambiguousCurrency))
                    {
                        return TokenOutcome.Rejected;
                    }

                    length = symbol.Length;
                    currencyCode = unambiguousCurrency;
                    return TokenOutcome.Read;
                }

                // In a value known to carry a standard symbol, a shared symbol that is the standard symbol of
                // one currency denotes that currency, though it is also the narrow symbol of others.
                if (standardSymbolsFirst
                    && CurrencySymbols.TryGetSoleStandardCurrency(symbol, out var standardCurrency)
                    && Money.IsCurrencyCodeValidForMoneyValue(standardCurrency))
                {
                    length = symbol.Length;
                    currencyCode = standardCurrency;
                    readAsStandardSymbol = true;
                    return TokenOutcome.Read;
                }

                // A shared symbol is otherwise resolved only from the currency of the culture being parsed for,
                // and only when the caller has accepted that.
                if ((styles & MoneyStyles.AllowAmbiguousCurrencySymbol) == 0)
                {
                    return TokenOutcome.Rejected;
                }

                if (!CurrencySymbols.TryGetAmbiguousCurrencies(symbol, out var candidates) || candidates is null)
                {
                    return TokenOutcome.Rejected;
                }

                if (!Iso4217Extensions.TryGetCurrentCurrency(currencyFormat.CurrencyCode, out var cultureCurrency))
                {
                    return TokenOutcome.Rejected;
                }

                foreach (var candidate in candidates)
                {
                    if (candidate != cultureCurrency || !Money.IsCurrencyCodeValidForMoneyValue(candidate))
                    {
                        continue;
                    }

                    length = symbol.Length;
                    currencyCode = candidate;
                    return TokenOutcome.Read;
                }

                // The culture does not account for this symbol, so the currency cannot be determined.
                return TokenOutcome.Rejected;
            }

            return TokenOutcome.NotFound;
        }

        private static bool TryReadCurrencyCode(string value, int start, int end, bool atStart, out Iso4217CurrencyCurrent currencyCode)
        {
            currencyCode = default(Iso4217CurrencyCurrent);

            if (end - start < CurrencyCodeLength)
            {
                return false;
            }

            // A currency code is three upper-case Latin letters, so other text is passed over without
            // allocating a candidate.
            var index = atStart ? start : end - CurrencyCodeLength;
            for (var offset = 0; offset < CurrencyCodeLength; offset++)
            {
                var character = value[index + offset];
                if (character < 'A' || character > 'Z')
                {
                    return false;
                }
            }

            return Iso4217Extensions.TryGetCurrentCurrency(value.Substring(index, CurrencyCodeLength), out currencyCode)
                   && Money.IsCurrencyCodeValidForMoneyValue(currencyCode);
        }

        /// <summary>
        /// Determine whether a negative sign appears at one end of a range of a value.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        private static bool IsSignAt(string value, int start, int end, bool atStart, string negativeSign, string? alternativeSign, out int signLength)
#else
        private static bool IsSignAt(string value, int start, int end, bool atStart, string negativeSign, string alternativeSign, out int signLength)
#endif
        {
            signLength = IsAffixAt(value, start, end, atStart, negativeSign) ? negativeSign.Length
                : alternativeSign != null && IsAffixAt(value, start, end, atStart, alternativeSign) ? alternativeSign.Length
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

        private static bool TryParseAmount(string value, int start, int length, MoneyStyles styles, NumberFormatInfo numberFormat, out decimal amount)
        {
            amount = decimal.Zero;

            if (length == 0)
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

#if NETCOREAPP2_1_OR_GREATER
            return decimal.TryParse(value.AsSpan(start, length), numberStyles, numberFormat, out amount);
#else
            return decimal.TryParse(value.Substring(start, length), numberStyles, numberFormat, out amount);
#endif
        }

        #endregion
    }
}
