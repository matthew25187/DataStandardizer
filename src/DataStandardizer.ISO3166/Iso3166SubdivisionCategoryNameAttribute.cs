using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.ISO3166
{
    /// <summary>
    /// Describes a <see cref="Iso3166Part2Subdivision"/> category name with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class Iso3166SubdivisionCategoryNameAttribute : Iso3166LanguageAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166SubdivisionCategoryNameAttribute(string? iso639Part1Code, string iso639Part2TCode, ushort categoryIdentifier, string categoryName)
#else
        internal Iso3166SubdivisionCategoryNameAttribute([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode, ushort categoryIdentifier, [NotNull] string categoryName)
#endif
            : base(iso639Part1Code, iso639Part2TCode)
        {
            CategoryIdentifier = categoryIdentifier;
            CategoryName = categoryName;
        }

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166SubdivisionCategoryNameAttribute(string? iso639Part1Code, string iso639Part2TCode, ushort categoryIdentifier, string categoryName, string categoryNamePlural)
#else
        internal Iso3166SubdivisionCategoryNameAttribute([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode, ushort categoryIdentifier, [NotNull] string categoryName, [NotNull] string categoryNamePlural)
#endif
            : this(iso639Part1Code, iso639Part2TCode, categoryIdentifier, categoryName)
        {
            CategoryNamePlural = categoryNamePlural;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the category identifier.
        /// </summary>
        public ushort CategoryIdentifier { get; }

        /// <summary>
        /// Gets the category name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string CategoryName { get; }
#else
        [NotNull]
        public string CategoryName { get; }
#endif
        /// <summary>
        /// Gets the plural form of the category name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? CategoryNamePlural { get; set; }
#else
        [CanBeNull]
        public string CategoryNamePlural { get; set; }
#endif

        #endregion
    }
}