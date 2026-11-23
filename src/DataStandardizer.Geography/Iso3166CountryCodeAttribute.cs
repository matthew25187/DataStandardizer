using System;
using DataStandardizer.Core;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Geography
{
    /// <summary>
    /// Describes an <see cref="DataStandardizer.Geography.Iso3166Part1Alpha2Country"/> or <see cref="DataStandardizer.Geography.Iso3166Part1Alpha3Country"/> code with metadata.
    /// </summary>
    /// <remarks>
    /// The inherited <see cref="CodeAttributeBase.EnglishName"/> and <see cref="CodeAttributeBase.FrenchName"/> are the country's English and French
    /// short names from ISO 3166-1, and <see cref="CodeAttributeBase.EnglishNames"/> and <see cref="CodeAttributeBase.FrenchNames"/> each hold that one name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso3166CountryCodeAttribute : CodeAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166CountryCodeAttribute(string englishShortName, string frenchShortName, string englishShortNameUpper) : base([englishShortName], [frenchShortName])
        {
            EnglishShortNameUpper = englishShortNameUpper;
        }

        internal Iso3166CountryCodeAttribute(string englishShortName, string frenchShortName, string englishShortNameUpper, string englishFullName) : this(englishShortName, frenchShortName, englishShortNameUpper)
        {
            EnglishFullName = englishFullName;
        }
#else
        internal Iso3166CountryCodeAttribute([DisallowNull] string englishShortName, [DisallowNull] string frenchShortName, [DisallowNull] string englishShortNameUpper) : base(new[] { englishShortName }, new[] { frenchShortName })
        {
            EnglishShortNameUpper = englishShortNameUpper;
        }

        internal Iso3166CountryCodeAttribute([DisallowNull] string englishShortName, [DisallowNull] string frenchShortName, [DisallowNull] string englishShortNameUpper, [DisallowNull] string englishFullName) : this(englishShortName, frenchShortName, englishShortNameUpper)
        {
            EnglishFullName = englishFullName;
        }
#endif

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the English short name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string EnglishShortName => EnglishName!;
#else
        public string EnglishShortName => EnglishName;
#endif

        /// <summary>
        /// Gets the English short name in uppercase.
        /// </summary>
        public string EnglishShortNameUpper { get; }

        /// <summary>
        /// Gets the English full name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? EnglishFullName { get; }
#else
        [MaybeNull]
        public string EnglishFullName { get; }
#endif
        /// <summary>
        /// Gets or sets a flag indicating if the country is independent.
        /// </summary>
        public bool IsIndependent { get; set; }

        #endregion
    }
}