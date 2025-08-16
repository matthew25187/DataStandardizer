#if NETSTANDARD
using JetBrains.Annotations; 
#endif

#pragma warning disable CS0618

namespace DataStandardizer.ISO4217
{
    public static class Iso4217CompatibilityExtensions
    {
        /// <summary>
        /// Get the name of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Name of the currency, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencyName(this Iso4217Current currencyCode)
#else
        [CanBeNull]
        public static string GetCurrencyName(this Iso4217Current currencyCode)
#endif
        {
            var newCurrencyCode = (Iso4217CurrencyCurrent)currencyCode;
            return newCurrencyCode.GetCurrencyName();
        }

        /// <summary>
        /// Get the name of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Name of the currency, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCurrencyName(this Iso4217Historic currencyCode)
#else
        [CanBeNull]
        public static string GetCurrencyName(this Iso4217Historic currencyCode)
#endif
        {
            var newCurrencyCode = (Iso4217CurrencyHistoric)currencyCode;
            return newCurrencyCode.GetCurrencyName();
        }

        /// <summary>
        /// Get the number of digits used for the minor units of a currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns>Number of minor unit digits, if found; otherwise <c>null</c>.</returns>
        public static byte? GetMinorUnits(this Iso4217Current currencyCode)
        {
            var newCurrencyCode = (Iso4217CurrencyCurrent)currencyCode;
            return newCurrencyCode.GetMinorUnits();
        }

        /// <summary>
        /// Determine if a code represents a funds code.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns><c>true</c> if the code is a funds code, otherwise <c>false</c>.</returns>
        public static bool IsFundCode(this Iso4217Current currencyCode)
        {
            var newCurrencyCode = (Iso4217CurrencyCurrent)currencyCode;
            return newCurrencyCode.IsFundCode();
        }

        /// <summary>
        /// Determine if a code is for a national currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns><c>true</c> if the code represents a national currency or <c>false</c> if not.</returns>
        public static bool IsNationalCurrency(this Iso4217Current currencyCode)
        {
            var newCurrencyCode = (Iso4217CurrencyCurrent)currencyCode;
            return newCurrencyCode.IsNationalCurrency();
        }

        /// <summary>
        /// Determine if a code is for a supranational currency.
        /// </summary>
        /// <param name="currencyCode">Code of the currency.</param>
        /// <returns><c>true</c> if the code represents a supranational currency or <c>false</c> if not.</returns>
        public static bool IsSupranationalCurrency(this Iso4217Current currencyCode)
        {
            var newCurrencyCode = (Iso4217CurrencyCurrent)currencyCode;
            return newCurrencyCode.IsSupranationalCurrency();
        }
    }
}