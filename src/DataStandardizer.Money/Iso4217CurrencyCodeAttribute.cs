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
        /// <summary>
        /// Initialises a new instance of the <see cref="Iso4217CurrencyCodeAttribute"/> class for a currency without minor units.
        /// </summary>
        /// <param name="currencyName">The name of the currency.</param>
        public Iso4217CurrencyCodeAttribute(string currencyName) : base([currencyName])
        {

        }

        /// <summary>
        /// Initialises a new instance of the <see cref="Iso4217CurrencyCodeAttribute"/> class for a currency with minor units.
        /// </summary>
        /// <param name="currencyName">The name of the currency.</param>
        /// <param name="minorUnits">The number of digits of the minor unit of the currency.</param>
        public Iso4217CurrencyCodeAttribute(string currencyName, byte minorUnits) : this(currencyName)
        {
            MinorUnits = minorUnits;
        }
#else
        /// <summary>
        /// Initialises a new instance of the <see cref="Iso4217CurrencyCodeAttribute"/> class for a currency without minor units.
        /// </summary>
        /// <param name="currencyName">The name of the currency.</param>
        public Iso4217CurrencyCodeAttribute([DisallowNull] string currencyName) : base(new[] { currencyName })
        {

        }

        /// <summary>
        /// Initialises a new instance of the <see cref="Iso4217CurrencyCodeAttribute"/> class for a currency with minor units.
        /// </summary>
        /// <param name="currencyName">The name of the currency.</param>
        /// <param name="minorUnits">The number of digits of the minor unit of the currency.</param>
        public Iso4217CurrencyCodeAttribute([DisallowNull] string currencyName, byte minorUnits) : this(currencyName)
        {
            MinorUnits = minorUnits;
        }
#endif

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the name of the currency.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string CurrencyName => EnglishName!;
#else
        [NotNull]
        public string CurrencyName => EnglishName;
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
    }
}