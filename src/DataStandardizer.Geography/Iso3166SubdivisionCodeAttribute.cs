using System;
using DataStandardizer.Core;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Geography
{
    /// <summary>
    /// Describes an <see cref="Iso3166Part2Subdivision"/> code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class Iso3166SubdivisionCodeAttribute : CodeAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166SubdivisionCodeAttribute(ushort subdivisionCategoryIdentifier, string subdivisionCode)
        {
            SubdivisionCategoryIdentifier = subdivisionCategoryIdentifier;
            SubdivisionCode = subdivisionCode;
        }

        internal Iso3166SubdivisionCodeAttribute(ushort subdivisionCategoryIdentifier, string subdivisionCode, string subdivisionParentCode) : this(subdivisionCategoryIdentifier, subdivisionCode)
        {
            SubdivisionParentCode = subdivisionParentCode;
        }
#else
        internal Iso3166SubdivisionCodeAttribute(ushort subdivisionCategoryIdentifier, [DisallowNull] string subdivisionCode)
        {
            SubdivisionCategoryIdentifier = subdivisionCategoryIdentifier;
            SubdivisionCode = subdivisionCode;
        }

        internal Iso3166SubdivisionCodeAttribute(ushort subdivisionCategoryIdentifier, [DisallowNull] string subdivisionCode, [DisallowNull] string subdivisionParentCode) : this(subdivisionCategoryIdentifier, subdivisionCode)
        {
            SubdivisionParentCode = subdivisionParentCode;
        }
#endif

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the subdivision category identifier.
        /// </summary>
        public ushort SubdivisionCategoryIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the subdivision code.
        /// </summary>
        public string SubdivisionCode { get; set; }

        /// <summary>
        /// Gets or sets the parent code.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? SubdivisionParentCode { get; set; }
#else
        [MaybeNull, AllowNull]
        public string SubdivisionParentCode { get; set; }
#endif

        #endregion
    }
}