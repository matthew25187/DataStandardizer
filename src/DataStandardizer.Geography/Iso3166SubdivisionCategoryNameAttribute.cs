using System;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Geography
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
        internal Iso3166SubdivisionCategoryNameAttribute([AllowNull] string iso639Part1Code, [DisallowNull] string iso639Part2TCode, ushort categoryIdentifier, [DisallowNull] string categoryName)
#endif
            : base(iso639Part1Code, iso639Part2TCode)
        {
            CategoryIdentifier = categoryIdentifier;
            CategoryName = categoryName;
        }

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166SubdivisionCategoryNameAttribute(string? iso639Part1Code, string iso639Part2TCode, ushort categoryIdentifier, string categoryName, string categoryNamePlural)
#else
        internal Iso3166SubdivisionCategoryNameAttribute([AllowNull] string iso639Part1Code, [DisallowNull] string iso639Part2TCode, ushort categoryIdentifier, [DisallowNull] string categoryName, [DisallowNull] string categoryNamePlural)
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
        [MaybeNull, AllowNull]
        public string CategoryNamePlural { get; set; }
#endif

        #endregion
    }
}