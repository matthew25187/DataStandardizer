using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.ISO3166
{
    /// <summary>
    /// Describes a territory of a country codified by ISO 3166 Part 1.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class Iso3166CountryTerritoryAttribute : Iso3166LanguageAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166CountryTerritoryAttribute(string? iso639Part1Code, string iso639Part2TCode, ushort territoryIdentifier, string territoryName)
#else
        internal Iso3166CountryTerritoryAttribute([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode, ushort territoryIdentifier, [NotNull] string territoryName)
#endif
            : base(iso639Part1Code, iso639Part2TCode)
        {
            TerritoryIdentifier = territoryIdentifier;
            TerritoryName = territoryName;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the identifier of the territory.
        /// </summary>
        public ushort TerritoryIdentifier { get; }

        /// <summary>
        /// Gets the name of the territory.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string TerritoryName { get; }
#else
        [NotNull]
        public string TerritoryName { get; }
#endif

        #endregion
    }
}