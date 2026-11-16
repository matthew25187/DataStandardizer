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
        private static readonly HashSet<Iso4217CurrencyHistoric> HistoricSharedValues = BuildHistoricSharedValueSet();
        private static readonly Dictionary<Iso4217CurrencyCurrent, string> CurrentNames = BuildNameCache<Iso4217CurrencyCurrent>();
        private static readonly Dictionary<string, Iso4217CurrencyCurrent> CurrentCodesByName = BuildCodeByNameCache(CurrentNames);

        private static Dictionary<TCurrencyCode, Iso4217CurrencyCodeAttribute> BuildAttributeCache<TCurrencyCode>()
#if NETCOREAPP3_0_OR_GREATER
            where TCurrencyCode : notnull
#endif
        {
            var cache = new Dictionary<TCurrencyCode, Iso4217CurrencyCodeAttribute>();
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
                if (currencyCodeValue is TCurrencyCode currencyCode)
                {
                    cache[currencyCode] = currencyCodeAttribute;
                }
            }

            return cache;
        }

        // ISO 4217 reuses numeric codes across successive denominations, so several historic members can
        // share a value, and the codes that have no numeric code all share -1. Members that share a value
        // can't be told apart, so the historic accessors return no metadata for these values.
        private static HashSet<Iso4217CurrencyHistoric> BuildHistoricSharedValueSet()
        {
            var definedValues = new HashSet<Iso4217CurrencyHistoric>();
            var sharedValues = new HashSet<Iso4217CurrencyHistoric>();
            foreach (var currencyCodeField in typeof(Iso4217CurrencyHistoric).GetTypeInfo().DeclaredFields)
            {
                if (currencyCodeField.IsStatic && currencyCodeField.GetValue(null) is Iso4217CurrencyHistoric currencyCode && !definedValues.Add(currencyCode))
                {
                    sharedValues.Add(currencyCode);
                }
            }

            return sharedValues;
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
        /// Members of <see cref="Iso4217CurrencyHistoric"/> that share a numeric code, such as
        /// <see cref="Iso4217CurrencyHistoric.BGJ"/>, <see cref="Iso4217CurrencyHistoric.BGK"/> and
        /// <see cref="Iso4217CurrencyHistoric.BGL"/>, are the same value and can't be told apart, and so are
        /// the codes that have no numeric code, which share -1. This method returns <c>null</c> for a shared
        /// value.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencyName(this Iso4217CurrencyHistoric currencyCode)
#else
        [return: MaybeNull]
        public static string GetCurrencyName(this Iso4217CurrencyHistoric currencyCode)
#endif
        {
            return !HistoricSharedValues.Contains(currencyCode) && HistoricAttributes.TryGetValue(currencyCode, out var currencyCodeAttribute) ? currencyCodeAttribute.CurrencyName : null;
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
        /// <see cref="Iso4217CurrencyHistoric.HRD"/> and <see cref="Iso4217CurrencyHistoric.HRK"/>, are the
        /// same value and can't be told apart, and so are the codes that have no numeric code, which share
        /// -1. This method returns <c>null</c> for a shared value.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode)
#else
        [return: MaybeNull]
        public static string GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode)
#endif
        {
            return GetCurrencySymbol(currencyCode, CurrencySymbolKind.Standard);
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
        /// <see cref="Iso4217CurrencyHistoric.HRD"/> and <see cref="Iso4217CurrencyHistoric.HRK"/>, are the
        /// same value and can't be told apart, and so are the codes that have no numeric code, which share
        /// -1. This method returns <c>null</c> for a shared value.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode, CurrencySymbolKind symbolKind)
#else
        [return: MaybeNull]
        public static string GetCurrencySymbol(this Iso4217CurrencyHistoric currencyCode, CurrencySymbolKind symbolKind)
#endif
        {
            // Symbols are keyed by value. The generator emits none for a shared value, and this keeps the
            // result null for one even if a later regeneration did.
            return HistoricSharedValues.Contains(currencyCode) ? null : CurrencySymbols.GetSymbol(currencyCode, symbolKind);
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
