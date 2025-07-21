using System;
using System.Linq;
using DataStandardizer.Core;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.ISO3166
{
    /// <summary>
    /// Describes an <see cref="Iso3166Part1Alpha2"/> or <see cref="Iso3166Part1Alpha3"/> code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso3166CountryCodeAttribute : CodeAttributeBase
    {
        #region Declarations

        private enum EnglishNameIndex
        {
            ShortName,
            ShortNameUpper,
            FullName
        }

        #endregion

        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166CountryCodeAttribute(string englishShortName, string englishShortNameUpper) : base([englishShortName, englishShortNameUpper])
        {

        }

        internal Iso3166CountryCodeAttribute(string englishShortName, string englishShortNameUpper, string englishFullName) : base([englishShortName, englishShortNameUpper, englishFullName])
        {

        }
#else
        internal Iso3166CountryCodeAttribute([NotNull] string englishShortName, [NotNull] string englishShortNameUpper) : base(new[] { englishShortName, englishShortNameUpper })
        {

        }

        internal Iso3166CountryCodeAttribute([NotNull] string englishShortName, [NotNull] string englishShortNameUpper, [NotNull] string englishFullName) : base(new[] { englishShortName, englishShortNameUpper, englishFullName })
        {

        }
#endif

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the English short name.
        /// </summary>
        public string EnglishShortName => EnglishNames[(int)EnglishNameIndex.ShortName];

        /// <summary>
        /// Gets the English short name in uppercase.
        /// </summary>
        public string EnglishShortNameUpper => EnglishNames[(int)EnglishNameIndex.ShortNameUpper];

        /// <summary>
        /// Gets the English full name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishFullName => EnglishNames.ElementAtOrDefault((int)EnglishNameIndex.FullName);
#else
        [CanBeNull]
        public string EnglishFullName => EnglishNames.ElementAtOrDefault((int)EnglishNameIndex.FullName);
#endif
        /// <summary>
        /// Gets or sets a flag indicating if the country is independent.
        /// </summary>
        public bool IsIndependent { get; set; }

        #endregion
    }
}