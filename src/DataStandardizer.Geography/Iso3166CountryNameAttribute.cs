using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Geography
{
    /// <summary>
    /// Describes a <see cref="DataStandardizer.Geography.Iso3166Part1Alpha2Country"/> or <see cref="DataStandardizer.Geography.Iso3166Part1Alpha3Country"/> code with metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public sealed class Iso3166CountryNameAttribute : Iso3166LanguageAttributeBase
    {
        #region Constructors

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166CountryNameAttribute(string? iso639Part1Code, string iso639Part2TCode, string shortName, string shortNameUpper)
#else
        internal Iso3166CountryNameAttribute([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode, [NotNull] string shortName, [NotNull] string shortNameUpper)
#endif
            : base(iso639Part1Code, iso639Part2TCode)
        {
            ShortName = shortName;
            ShortNameUpper = shortNameUpper;
        }

#if NETCOREAPP3_0_OR_GREATER
        internal Iso3166CountryNameAttribute(string? iso639Part1Code, string iso639Part2TCode, string shortName, string shortNameUpper, string fullName)
#else
        internal Iso3166CountryNameAttribute([CanBeNull] string iso639Part1Code, [NotNull] string iso639Part2TCode, [NotNull] string shortName, [NotNull] string shortNameUpper, [NotNull] string fullName)
#endif
            : this(iso639Part1Code, iso639Part2TCode, shortName, shortNameUpper)
        {
            FullName = fullName;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the country's full name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string? FullName { get; }
#else
        [CanBeNull]
        public string FullName { get; }
#endif

        /// <summary>
        /// Gets the country's short name.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string ShortName { get; }
#else
        [NotNull]
        public string ShortName { get; }
#endif
        /// <summary>
        /// Gets the country's short name in uppercase.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        public string ShortNameUpper { get; }
#else
        [NotNull]
        public string ShortNameUpper { get; }
#endif

        #endregion
    }
}