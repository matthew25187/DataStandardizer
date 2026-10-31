using System.Collections.Generic;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Money
{
    /// <content>
    /// Lookup logic over the generated currency symbol data.
    /// </content>
    /// <remarks>
    /// The generated part of this class holds the data; this part holds the behaviour, so that
    /// re-running the generator never overwrites hand-written code.
    /// </remarks>
    internal partial class CurrencySymbols
    {
        #region Reverse Lookup Caches

        // Symbols resolve to a currency only where exactly one currency uses that symbol. Where a symbol
        // is shared -- '$' is used by thirty currencies -- the candidates are retained separately so that
        // a caller supplying culture context can still resolve the value, and so that a caller without
        // context fails rather than guessing.
        private static readonly Dictionary<string, Iso4217CurrencyCurrent> UnambiguousCurrentSymbols;
        private static readonly Dictionary<string, Iso4217CurrencyCurrent[]> AmbiguousCurrentSymbols;

        // Ordered longest first so that 'NZ$' is matched in preference to '$', and 'L£' in preference to '£'.
        private static readonly string[] CurrentSymbolsByDescendingLength;

        static CurrencySymbols()
        {
            var symbolCandidates = new Dictionary<string, List<Iso4217CurrencyCurrent>>();
            CollectSymbolCandidates(CurrentStandardSymbols, symbolCandidates);
            CollectSymbolCandidates(CurrentNarrowSymbols, symbolCandidates);

            UnambiguousCurrentSymbols = new Dictionary<string, Iso4217CurrencyCurrent>();
            AmbiguousCurrentSymbols = new Dictionary<string, Iso4217CurrencyCurrent[]>();
            foreach (var symbolCandidate in symbolCandidates)
            {
                if (symbolCandidate.Value.Count == 1)
                {
                    UnambiguousCurrentSymbols[symbolCandidate.Key] = symbolCandidate.Value[0];
                }
                else
                {
                    AmbiguousCurrentSymbols[symbolCandidate.Key] = symbolCandidate.Value.ToArray();
                }
            }

            var orderedSymbols = new List<string>(symbolCandidates.Keys);
            orderedSymbols.Sort((first, second) =>
            {
                var lengthComparison = second.Length.CompareTo(first.Length);
                return lengthComparison != 0 ? lengthComparison : string.CompareOrdinal(first, second);
            });
            CurrentSymbolsByDescendingLength = orderedSymbols.ToArray();
        }

        private static void CollectSymbolCandidates(Dictionary<Iso4217CurrencyCurrent, string> symbols, Dictionary<string, List<Iso4217CurrencyCurrent>> symbolCandidates)
        {
            foreach (var symbol in symbols)
            {
                if (!symbolCandidates.TryGetValue(symbol.Value, out var candidates))
                {
                    candidates = new List<Iso4217CurrencyCurrent>();
                    symbolCandidates[symbol.Value] = candidates;
                }

                if (!candidates.Contains(symbol.Key))
                {
                    candidates.Add(symbol.Key);
                }
            }
        }

        #endregion

        #region Symbol Lookup

        /// <summary>
        /// Get the symbol of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <param name="symbolKind">Form of the symbol required.</param>
        /// <returns>Symbol of the currency, if one is defined; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static string? GetSymbol(Iso4217CurrencyCurrent currencyCode, CurrencySymbolKind symbolKind)
#else
        [CanBeNull]
        internal static string GetSymbol(Iso4217CurrencyCurrent currencyCode, CurrencySymbolKind symbolKind)
#endif
        {
            if (symbolKind == CurrencySymbolKind.Narrow && CurrentNarrowSymbols.TryGetValue(currencyCode, out var narrowSymbol))
            {
                return narrowSymbol;
            }

            // A currency without a distinct narrow form falls back to its standard form.
            return CurrentStandardSymbols.TryGetValue(currencyCode, out var standardSymbol) ? standardSymbol : null;
        }

        /// <summary>
        /// Get the symbol of a historic currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <param name="symbolKind">Form of the symbol required.</param>
        /// <returns>Symbol of the currency, if one is defined; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static string? GetSymbol(Iso4217CurrencyHistoric currencyCode, CurrencySymbolKind symbolKind)
#else
        [CanBeNull]
        internal static string GetSymbol(Iso4217CurrencyHistoric currencyCode, CurrencySymbolKind symbolKind)
#endif
        {
            if (symbolKind == CurrencySymbolKind.Narrow && HistoricNarrowSymbols.TryGetValue(currencyCode, out var narrowSymbol))
            {
                return narrowSymbol;
            }

            return HistoricStandardSymbols.TryGetValue(currencyCode, out var standardSymbol) ? standardSymbol : null;
        }

        #endregion

        #region Reverse Lookup

        /// <summary>
        /// Get the symbols of all current currencies, ordered longest first.
        /// </summary>
        /// <remarks>
        /// Matching in this order ensures that a longer symbol is preferred over a shorter symbol which
        /// forms its suffix, so that <c>NZ$</c> is not mistaken for <c>$</c>.
        /// </remarks>
        internal static string[] SymbolsByDescendingLength => CurrentSymbolsByDescendingLength;

        /// <summary>
        /// Determine the currency denoted by a symbol where exactly one currency uses it.
        /// </summary>
        /// <param name="symbol">Symbol of the currency.</param>
        /// <param name="currencyCode">Code of the currency denoted by <paramref name="symbol"/>, if it denotes exactly one.</param>
        /// <returns><c>true</c> if the symbol denotes exactly one currency; otherwise <c>false</c>.</returns>
        internal static bool TryGetUnambiguousCurrency(string symbol, out Iso4217CurrencyCurrent currencyCode)
        {
            return UnambiguousCurrentSymbols.TryGetValue(symbol, out currencyCode);
        }

        /// <summary>
        /// Get the currencies which share a symbol.
        /// </summary>
        /// <param name="symbol">Symbol of the currency.</param>
        /// <param name="currencyCodes">Codes of the currencies which use <paramref name="symbol"/>, if it is shared.</param>
        /// <returns><c>true</c> if the symbol is shared by more than one currency; otherwise <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryGetAmbiguousCurrencies(string symbol, out Iso4217CurrencyCurrent[]? currencyCodes)
#else
        internal static bool TryGetAmbiguousCurrencies(string symbol, out Iso4217CurrencyCurrent[] currencyCodes)
#endif
        {
            return AmbiguousCurrentSymbols.TryGetValue(symbol, out currencyCodes);
        }

        #endregion
    }
}
