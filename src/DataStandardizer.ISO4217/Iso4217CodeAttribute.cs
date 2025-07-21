using System;
using DataStandardizer.Core;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.ISO4217
{
    /// <summary>
    /// Describes a <see cref="Iso4217Current"/> or <see cref="Iso4217Historic"/> code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso4217CodeAttribute : CodeAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        public Iso4217CodeAttribute(string currencyName) : base([currencyName])
        {

        }

        public Iso4217CodeAttribute(string currencyName, byte minorUnits) : this(currencyName)
        {
            MinorUnits = minorUnits;
        }
#else
        public Iso4217CodeAttribute([NotNull] string currencyName) : base(new[] { currencyName })
        {

        }

        public Iso4217CodeAttribute([NotNull] string currencyName, byte minorUnits) : this(currencyName)
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
    }
}