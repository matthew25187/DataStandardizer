using System;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.ISO3166
{
    /// <summary>
    /// Describe a language used to express names for a country or its subdivisions with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class Iso3166LanguageAttribute : Iso3166LanguageAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166LanguageAttribute(string? iso639Part1Code, string iso639Part2TCode)
#else
        internal Iso3166LanguageAttribute([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode)
#endif
            : base(iso639Part1Code, iso639Part2TCode)
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a flag that indicates if the language is used by the country for administrative purposes.
        /// </summary>
        public bool IsAdministrative { get; set; }

        #endregion
    }
}