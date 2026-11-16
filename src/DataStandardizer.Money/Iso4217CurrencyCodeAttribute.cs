using System;
using DataStandardizer.Core;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Money
{
    /// <summary>
    /// Describes a <see cref="Iso4217CurrencyCurrent"/> or <see cref="Iso4217CurrencyHistoric"/> code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso4217CurrencyCodeAttribute : CodeAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        public Iso4217CurrencyCodeAttribute(string currencyName) : base([currencyName])
        {

        }

        public Iso4217CurrencyCodeAttribute(string currencyName, byte minorUnits) : this(currencyName)
        {
            MinorUnits = minorUnits;
        }

        // Generated historic members pass the withdrawal date from ISO 4217 List Three.
        internal Iso4217CurrencyCodeAttribute(string currencyName, string withdrawalDate) : this(currencyName)
        {
            WithdrawalDate = withdrawalDate;
        }
#else
        public Iso4217CurrencyCodeAttribute([DisallowNull] string currencyName) : base(new[] { currencyName })
        {

        }

        public Iso4217CurrencyCodeAttribute([DisallowNull] string currencyName, byte minorUnits) : this(currencyName)
        {
            MinorUnits = minorUnits;
        }

        // Generated historic members pass the withdrawal date from ISO 4217 List Three.
        internal Iso4217CurrencyCodeAttribute([DisallowNull] string currencyName, [DisallowNull] string withdrawalDate) : this(currencyName)
        {
            WithdrawalDate = withdrawalDate;
        }
#endif

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the name of the currency.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string CurrencyName => EnglishNames[0];
#else
        [NotNull]
        public string CurrencyName => EnglishNames[0];
#endif
        /// <summary>
        /// Gets or sets a flag indicating if the code is a funds code.
        /// </summary>
        public bool IsFundsCode { get; set; }

        /// <summary>
        /// Gets the number of minor units for the currency.
        /// </summary>
        public byte? MinorUnits { get; }

        #endregion

        #region Internal Properties

        /// <summary>
        /// Gets the withdrawal date of a historic denomination, as ISO 4217 List Three gives it, such as
        /// <c>2003-11</c> or <c>1989 to 1990</c>; or <c>null</c> for a current code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal string? WithdrawalDate { get; }
#else
        [MaybeNull]
        internal string WithdrawalDate { get; }
#endif

        #endregion
    }
}