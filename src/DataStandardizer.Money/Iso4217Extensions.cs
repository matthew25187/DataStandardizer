using System;
using System.Collections.Generic;
using System.Reflection;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Money
{
    public static class Iso4217Extensions
    {
        #region Metadata Caches

        // The ISO 4217 enums are generated and immutable at run time, so their metadata is resolved once
        // per enum rather than by reflecting on every call. Formatting and parsing consult these on hot
        // paths; uncached reflection here was previously repeated for every currency of every operation.
        private static readonly Dictionary<Iso4217CurrencyCurrent, Iso4217CurrencyCodeAttribute> CurrentAttributes = BuildAttributeCache<Iso4217CurrencyCurrent>();
        private static readonly Dictionary<Iso4217CurrencyHistoric, Iso4217CurrencyCodeAttribute> HistoricAttributes = BuildAttributeCache<Iso4217CurrencyHistoric>();
        private static readonly Dictionary<Iso4217CurrencyCurrent, string> CurrentNames = BuildNameCache<Iso4217CurrencyCurrent>();
        private static readonly Dictionary<string, Iso4217CurrencyCurrent> CurrentCodesByName = BuildCodeByNameCache(CurrentNames);

        private static Dictionary<TCurrencyCode, Iso4217CurrencyCodeAttribute> BuildAttributeCache<TCurrencyCode>()
#if NETCOREAPP3_0_OR_GREATER
            where TCurrencyCode : notnull
#endif
        {
            // ISO 4217 List Three reuses numeric codes, so several historic members can share a value. The
            // member kept for a shared value is chosen by its data, never by the order reflection returns
            // fields in: see PrecedesInWithdrawal.
            var cache = new Dictionary<TCurrencyCode, Iso4217CurrencyCodeAttribute>();
            var cachedFieldNames = new Dictionary<TCurrencyCode, string>();
            foreach (var currencyCodeField in typeof(TCurrencyCode).GetTypeInfo().DeclaredFields)
            {
                if (!currencyCodeField.IsStatic)
                {
                    continue;
                }

                var currencyCodeAttribute = currencyCodeField.GetCustomAttribute<Iso4217CurrencyCodeAttribute>();
                if (currencyCodeAttribute is null)
                {
                    continue;
                }

                var currencyCodeValue = currencyCodeField.GetValue(null);
                if (!(currencyCodeValue is TCurrencyCode currencyCode))
                {
                    continue;
                }

                if (cache.TryGetValue(currencyCode, out var cachedAttribute)
                    && !PrecedesInWithdrawal(cachedAttribute.WithdrawalDate, cachedFieldNames[currencyCode], currencyCodeAttribute.WithdrawalDate, currencyCodeField.Name))
                {
                    continue;
                }

                cache[currencyCode] = currencyCodeAttribute;
                cachedFieldNames[currencyCode] = currencyCodeField.Name;
            }

            return cache;
        }

        // Determine whether the first member is withdrawn before the second, so that a value shared by
        // several members resolves to the one withdrawn most recently. Withdrawal dates are ordered by their
        // last year, then their last month, a date that gives no month counting as December. Members withdrawn
        // at the same time are ordered by their alphabetic codes, ordinally.
#if NETCOREAPP3_0_OR_GREATER
        private static bool PrecedesInWithdrawal(string? firstWithdrawalDate, string firstCode, string? secondWithdrawalDate, string secondCode)
#else
        private static bool PrecedesInWithdrawal(string firstWithdrawalDate, string firstCode, string secondWithdrawalDate, string secondCode)
#endif
        {
            var firstOrder = GetWithdrawalOrder(firstWithdrawalDate);
            var secondOrder = GetWithdrawalOrder(secondWithdrawalDate);
            return firstOrder != secondOrder ? firstOrder < secondOrder : string.CompareOrdinal(firstCode, secondCode) < 0;
        }

        // List Three gives a withdrawal date as a month (2003-11), a year range (1989 to 1990, or 1989-1990)
        // or a month range (1989-12 to 1990-01). Only the end of the period counts. Returns year * 100 + month,
        // or 0 for a date that is missing or not in one of those forms.
#if NETCOREAPP3_0_OR_GREATER
        private static int GetWithdrawalOrder(string? withdrawalDate)
#else
        private static int GetWithdrawalOrder(string withdrawalDate)
#endif
        {
            if (withdrawalDate is null)
            {
                return 0;
            }

            const string RangeSeparator = " to ";
            var end = withdrawalDate;
            var rangeSeparatorIndex = end.LastIndexOf(RangeSeparator, StringComparison.Ordinal);
            if (rangeSeparatorIndex >= 0)
            {
                end = end.Substring(rangeSeparatorIndex + RangeSeparator.Length);
            }
            else if (end.Length == 9 && end[4] == '-')
            {
                // yyyy-yyyy
                end = end.Substring(5);
            }

            if (!TryParseDigits(end, 0, 4, out var year))
            {
                return 0;
            }

            const int December = 12;
            if (end.Length == 4)
            {
                return (year * 100) + December;
            }

            return end.Length == 7 && end[4] == '-' && TryParseDigits(end, 5, 2, out var month) && month >= 1 && month <= December
                ? (year * 100) + month
                : 0;
        }

        private static bool TryParseDigits(string text, int startIndex, int length, out int value)
        {
            value = 0;
            if (text.Length < startIndex + length)
            {
                return false;
            }

            for (var index = startIndex; index < startIndex + length; index++)
            {
                var digit = text[index] - '0';
                if (digit < 0 || digit > 9)
                {
                    return false;
                }

                value = (value * 10) + digit;
            }

            return true;
        }

        private static Dictionary<TCurrencyCode, string> BuildNameCache<TCurrencyCode>()
#if NETCOREAPP3_0_OR_GREATER
            where TCurrencyCode : notnull
#endif
        {
            var cache = new Dictionary<TCurrencyCode, string>();
            foreach (var currencyCodeField in typeof(TCurrencyCode).GetTypeInfo().DeclaredFields)
            {
                if (!currencyCodeField.IsStatic)
                {
                    continue;
                }

                var currencyCodeValue = currencyCodeField.GetValue(null);
                if (currencyCodeValue is TCurrencyCode currencyCode)
                {
                    cache[currencyCode] = currencyCodeField.Name;
                }
            }

            return cache;
        }

        private static Dictionary<string, Iso4217CurrencyCurrent> BuildCodeByNameCache(Dictionary<Iso4217CurrencyCurrent, string> names)
        {
            // Ordinal, so that only the exact upper-case code is found.
            var cache = new Dictionary<string, Iso4217CurrencyCurrent>(StringComparer.Ordinal);
            foreach (var name in names)
            {
                cache[name.Value] = name.Key;
            }

            return cache;
        }

        #endregion

        /// <summary>
        /// Find the current currency an ISO 4217 alphabetic code names.
        /// </summary>
        /// <param name="code">The code, which must match the name of a defined <see cref="Iso4217CurrencyCurrent"/> member exactly.</param>
        /// <param name="currencyCode">The currency <paramref name="code"/> names, if it names one.</param>
        /// <returns><c>true</c> if <paramref name="code"/> names a defined currency; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// Unlike <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>, this accepts neither a numeric
        /// value, nor surrounding white space, nor a comma-separated list, nor a different case.
        /// </remarks>
        internal static bool TryGetCurrentCurrency(string code, out Iso4217CurrencyCurrent currencyCode)
        {
            return CurrentCodesByName.TryGetValue(code, out currencyCode);
        }

        /// <summary>
        /// Get the name of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Name of the currency, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencyName(this Iso4217CurrencyCurrent currencyCode)
#else
        [return: MaybeNull]
        public static string GetCurrencyName(this Iso4217CurrencyCurrent currencyCode)
#endif
        {
            return CurrentAttributes.TryGetValue(currencyCode, out var currencyCodeAttribute) ? currencyCodeAttribute.CurrencyName : null;
        }

        /// <summary>
        /// Get the name of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Name of the currency, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// <para>
        /// Members of <see cref="Iso4217CurrencyHistoric"/> that share a numeric code, such as
        /// <see cref="Iso4217CurrencyHistoric.BGJ"/>, <see cref="Iso4217CurrencyHistoric.BGK"/> and
        /// <see cref="Iso4217CurrencyHistoric.BGL"/>, are the same value and can't be told apart, and so
        /// are the codes that have no numeric code, which share -1. For a shared value, this returns the
        /// name of the member withdrawn most recently, such as <see cref="Iso4217CurrencyHistoric.BGL"/>.
        /// Withdrawal dates are compared by their last year, then their last month, a date that gives
        /// no month counting as December; members withdrawn at the same time are compared by their
        /// alphabetic codes, and the one that sorts last ordinally is used.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencyName(this Iso4217CurrencyHistoric currencyCode)
#else
        [return: MaybeNull]
        public static string GetCurrencyName(this Iso4217CurrencyHistoric currencyCode)
#endif
        {
            return HistoricAttributes.TryGetValue(currencyCode, out var currencyCodeAttribute) ? currencyCodeAttribute.CurrencyName : null;
        }

        /// <summary>
        /// Get the standard symbol of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Standard symbol of the currency, if one is defined; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// ISO 4217 does not define currency symbols, so this data is sourced from the Unicode Common
        /// Locale Data Repository (CLDR). Most currencies have no symbol distinct from their currency
        /// code, in which case this method returns <c>null</c> and the currency code should be used.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencySymbol(this Iso4217CurrencyCurrent currencyCode)
#else
        [return: MaybeNull]
        public static string GetCurrencySymbol(this Iso4217CurrencyCurrent currencyCode)
#endif
        {
            return CurrencySymbols.GetSymbol(currencyCode, CurrencySymbolKind.Standard);
        }

        /// <summary>
        /// Get the symbol of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <param name="symbolKind">Form of the symbol required.</param>
        /// <returns>Symbol of the currency in the requested form, if one is defined; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Where a currency has no distinct <see cref="CurrencySymbolKind.Narrow"/> form, its
        /// <see cref="CurrencySymbolKind.Standard"/> form is returned. Narrow symbols may be shared by
        /// several currencies, so they should only be used where the currency is clear from the context.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencySymbol(this Iso4217CurrencyCurrent currencyCode, CurrencySymbolKind symbolKind)
#else
        [return: MaybeNull]
        public static string GetCurrencySymbol(this Iso4217CurrencyCurrent currencyCode, CurrencySymbolKind symbolKind)
#endif
        {
            return CurrencySymbols.GetSymbol(currencyCode, symbolKind);
        }

        /// <summary>
        /// Get the standard symbol of a historic currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Standard symbol of the currency, if one is defined; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// <para>
        /// ISO 4217 does not define currency symbols, so this data is sourced from the Unicode Common
        /// Locale Data Repository (CLDR). Most currencies have no symbol distinct from their currency
        /// code, in which case this method returns <c>null</c> and the currency code should be used.
        /// </para>
        /// <para>
        /// Members of <see cref="Iso4217CurrencyHistoric"/> that share a numeric code, such as
        /// <see cref="Iso4217CurrencyHistoric.BGJ"/>, <see cref="Iso4217CurrencyHistoric.BGK"/> and
        /// <see cref="Iso4217CurrencyHistoric.BGL"/>, are the same value and can't be told apart, and so
        /// are the codes that have no numeric code, which share -1. For a shared value, this returns the
        /// symbol of the member withdrawn most recently, so <see cref="Iso4217CurrencyHistoric.HRD"/> returns
        /// the symbol of <see cref="Iso4217CurrencyHistoric.HRK"/>.
        /// Withdrawal dates are compared by their last year, then their last month, a date that gives
        /// no month counting as December; members withdrawn at the same time are compared by their
        /// alphabetic codes, and the one that sorts last ordinally is used.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode)
#else
        [return: MaybeNull]
        public static string GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode)
#endif
        {
            return CurrencySymbols.GetSymbol(currencyCode, CurrencySymbolKind.Standard);
        }

        /// <summary>
        /// Get the symbol of a historic currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <param name="symbolKind">Form of the symbol required.</param>
        /// <returns>Symbol of the currency in the requested form, if one is defined; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// <para>
        /// Where a currency has no distinct <see cref="CurrencySymbolKind.Narrow"/> form, its
        /// <see cref="CurrencySymbolKind.Standard"/> form is returned. Narrow symbols may be shared by
        /// several currencies, so they should only be used where the currency is clear from the context.
        /// </para>
        /// <para>
        /// Members of <see cref="Iso4217CurrencyHistoric"/> that share a numeric code, such as
        /// <see cref="Iso4217CurrencyHistoric.BGJ"/>, <see cref="Iso4217CurrencyHistoric.BGK"/> and
        /// <see cref="Iso4217CurrencyHistoric.BGL"/>, are the same value and can't be told apart, and so
        /// are the codes that have no numeric code, which share -1. For a shared value, this returns the
        /// symbol of the member withdrawn most recently, so <see cref="Iso4217CurrencyHistoric.HRD"/> returns
        /// the symbol of <see cref="Iso4217CurrencyHistoric.HRK"/>.
        /// Withdrawal dates are compared by their last year, then their last month, a date that gives
        /// no month counting as December; members withdrawn at the same time are compared by their
        /// alphabetic codes, and the one that sorts last ordinally is used.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode, CurrencySymbolKind symbolKind)
#else
        [return: MaybeNull]
        public static string GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode, CurrencySymbolKind symbolKind)
#endif
        {
            return CurrencySymbols.GetSymbol(currencyCode, symbolKind);
        }

        /// <summary>
        /// Get the number of digits used for the minor units of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Number of minor unit digits, if found; otherwise <c>null</c>.</returns>
        public static byte? GetMinorUnits(this Iso4217CurrencyCurrent currencyCode)
        {
            return CurrentAttributes.TryGetValue(currencyCode, out var currencyCodeAttribute) ? currencyCodeAttribute.MinorUnits : null;
        }

        /// <summary>
        /// Determine if a code represents a funds code.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns><c>true</c> if the code is a funds code, otherwise <c>false</c>.</returns>
        public static bool IsFundCode(this Iso4217CurrencyCurrent currencyCode)
        {
            return CurrentAttributes.TryGetValue(currencyCode, out var currencyCodeAttribute) && currencyCodeAttribute.IsFundsCode;
        }

        /// <summary>
        /// Determine if a code is for a national currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns><c>true</c> if the code represents a national currency or <c>false</c> if not.</returns>
        /// <remarks>
        /// A funds code, such as <see cref="Iso4217CurrencyCurrent.CLF"/> or <see cref="Iso4217CurrencyCurrent.USN"/>, is not a national currency.
        /// </remarks>
        public static bool IsNationalCurrency(this Iso4217CurrencyCurrent currencyCode)
        {
            if (!CurrentNames.TryGetValue(currencyCode, out var currencyCodeName) || !CurrentAttributes.TryGetValue(currencyCode, out var currencyCodeAttribute))
            {
                return false;
            }

            return !IsSupranationalOrSpecialCodeName(currencyCodeName) && currencyCodeAttribute.MinorUnits.HasValue && !currencyCodeAttribute.IsFundsCode;
        }

        /// <summary>
        /// Determine if a code is for a supranational currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns><c>true</c> if the code represents a supranational currency or <c>false</c> if not.</returns>
        public static bool IsSupranationalCurrency(this Iso4217CurrencyCurrent currencyCode)
        {
            if (!CurrentNames.TryGetValue(currencyCode, out var currencyCodeName) || !CurrentAttributes.TryGetValue(currencyCode, out var currencyCodeAttribute))
            {
                return false;
            }

            return IsSupranationalOrSpecialCodeName(currencyCodeName) && currencyCodeAttribute.MinorUnits.HasValue;
        }

        // ISO 4217 reserves codes beginning with X for supranational currencies, precious metals and special codes.
        private static bool IsSupranationalOrSpecialCodeName(string currencyCodeName)
        {
            return currencyCodeName.Length != 0 && currencyCodeName[0] == 'X';
        }
    }
}
