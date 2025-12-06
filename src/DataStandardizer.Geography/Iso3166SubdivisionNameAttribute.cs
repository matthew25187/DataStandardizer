using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Geography
{
    /// <summary>
    /// Describes a <see cref="Iso3166Part2Subdivision"/> name with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class Iso3166SubdivisionNameAttribute : Iso3166LanguageAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166SubdivisionNameAttribute(string? iso639Part1Code, string iso639Part2TCode, ushort subdivisionCategoryIdentifier, string subdivisionName)
#else
        internal Iso3166SubdivisionNameAttribute([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode, ushort subdivisionCategoryIdentifier, [NotNull] string subdivisionName)
#endif
            : base(iso639Part1Code, iso639Part2TCode)
        {
            SubdivisionCategoryIdentifier = subdivisionCategoryIdentifier;
            SubdivisionName = subdivisionName;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the romanization system for the name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? RomanizationSystem { get; set; }
#else
        [CanBeNull]
        public string RomanizationSystem { get; set; }
#endif
        /// <summary>
        /// Gets the category identifier for the subdivision.
        /// </summary>
        public ushort SubdivisionCategoryIdentifier { get; }

        /// <summary>
        /// Gets the subdivision's name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string SubdivisionName { get; }
#else
        [NotNull]
        public string SubdivisionName { get; }
#endif
        /// <summary>
        /// Gets the local variant of the subdivision's name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SubdivisionNameLocalVariant { get; set; }
#else
        [CanBeNull]
        public string SubdivisionNameLocalVariant { get; set; }
#endif

        #endregion
    }
}