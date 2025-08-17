using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

#pragma warning disable CS0618

namespace DataStandardizer.ISO3166
{
    public static class Iso3166CompatibilityExtensions
    {
        /// <summary>
        /// Get the English name of a country.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <param name="nameType">Type of name to retrieve.</param>
        /// <returns>English name of the country, if found; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso3166Part1Alpha2 countryCode, Iso3166CountryName nameType)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso3166Part1Alpha2 countryCode, Iso3166CountryName nameType)
#endif
        {
            var newCountryCode = (Iso3166Part1Alpha2Country)countryCode;
            return newCountryCode.GetEnglishName(nameType);
        }

        /// <summary>
        /// Get the native name of a country.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <param name="iso639LanguageCode">Language code for the language of the name to retrieve.</param>
        /// <param name="nameType">Type of name to retrieve.</param>
        /// <returns>Native name of the country, if found; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetNativeName(this Iso3166Part1Alpha2 countryCode, string iso639LanguageCode, Iso3166CountryName nameType)
#else
        [CanBeNull]
        public static string GetNativeName(this Iso3166Part1Alpha2 countryCode, [NotNull] string iso639LanguageCode, Iso3166CountryName nameType)
#endif
        {
            var newCountryCode = (Iso3166Part1Alpha2Country)countryCode;
            return newCountryCode.GetNativeName(iso639LanguageCode, nameType);
        }

        /// <summary>
        /// Determine if the country or place is independent.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <returns><c>true</c> if the country or place is independent; <c>false</c> if not.</returns>
        public static bool IsIndependent(this Iso3166Part1Alpha2 countryCode)
        {
            var newCountryCode = (Iso3166Part1Alpha2Country)countryCode;
            return newCountryCode.IsIndependent();
        }

        /// <summary>
        /// Get the English name of a country.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <param name="nameType">Type of name to retrieve.</param>
        /// <returns>English name of the country, if found; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso3166Part1Alpha3 countryCode, Iso3166CountryName nameType)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso3166Part1Alpha3 countryCode, Iso3166CountryName nameType)
#endif
        {
            var newCountryCode = (Iso3166Part1Alpha3Country)countryCode;
            return newCountryCode.GetEnglishName(nameType);
        }

        /// <summary>
        /// Get the native name of a country.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <param name="iso639LanguageCode">Language code for the language of the name to retrieve.</param>
        /// <param name="nameType">Type of name to retrieve.</param>
        /// <returns>Native name of the country, if found; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetNativeName(this Iso3166Part1Alpha3 countryCode, string iso639LanguageCode, Iso3166CountryName nameType)
#else
        [CanBeNull]
        public static string GetNativeName(this Iso3166Part1Alpha3 countryCode, [NotNull] string iso639LanguageCode, Iso3166CountryName nameType)
#endif
        {
            var newCountryCode = (Iso3166Part1Alpha3Country)countryCode;
            return newCountryCode.GetNativeName(iso639LanguageCode, nameType);
        }

        /// <summary>
        /// Determine if the country or place is independent.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <returns><c>true</c> if the country or place is independent; <c>false</c> if not.</returns>
        public static bool IsIndependent(this Iso3166Part1Alpha3 countryCode)
        {
            var newCountryCode = (Iso3166Part1Alpha3Country)countryCode;
            return newCountryCode.IsIndependent();
        }

        /// <summary>
        /// Get the category identifier of a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <returns>Identifier of the subdivision's category, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetSubdivisionCategoryIdentifier(this Iso3166Part2 subdivisionCode)
        {
            var newSubdivisionCode = ConvertOldSubdivisionCodeToNewSubdivisionCode(subdivisionCode);
            return newSubdivisionCode.GetSubdivisionCategoryIdentifier();
        }

        /// <summary>
        /// Get the name of a country subdivision category.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <param name="iso639LanguageCode">ISO 639 language code for the language of the name to retrieve.</param>
        /// <returns>Name of the category, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionCategoryName(this Iso3166Part2 subdivisionCode, string iso639LanguageCode)
#else
        [CanBeNull]
        public static string GetSubdivisionCategoryName(this Iso3166Part2 subdivisionCode, [NotNull] string iso639LanguageCode)
#endif
        {
            var newSubdivisionCode = ConvertOldSubdivisionCodeToNewSubdivisionCode(subdivisionCode);
            return newSubdivisionCode.GetSubdivisionCategoryName(iso639LanguageCode);
        }

        /// <summary>
        /// Get the name of a country subdivision category in plural form.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <param name="iso639LanguageCode">ISO 639 language code for the language of the name to retrieve.</param>
        /// <returns>Plural-form name of the category, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionCategoryNamePlural(this Iso3166Part2 subdivisionCode, string iso639LanguageCode)
#else
        [CanBeNull]
        public static string GetSubdivisionCategoryNamePlural(this Iso3166Part2 subdivisionCode, [NotNull] string iso639LanguageCode)
#endif
        {
            var newSubdivisionCode = ConvertOldSubdivisionCodeToNewSubdivisionCode(subdivisionCode);
            return newSubdivisionCode.GetSubdivisionCategoryNamePlural(iso639LanguageCode);
        }

        /// <summary>
        /// Get the alpha code for a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <returns>Subdivision code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionCode(this Iso3166Part2 subdivisionCode)
#else
        [CanBeNull]
        public static string GetSubdivisionCode(this Iso3166Part2 subdivisionCode)
#endif
        {
            var newSubdivisionCode = ConvertOldSubdivisionCodeToNewSubdivisionCode(subdivisionCode);
            return newSubdivisionCode.GetSubdivisionCode();
        }

        /// <summary>
        /// Get the parent alpha code for a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <returns>Parent subdivision code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionParentCode(this Iso3166Part2 subdivisionCode)
#else
        [CanBeNull]
        public static string GetSubdivisionParentCode(this Iso3166Part2 subdivisionCode)
#endif
        {
            var newSubdivisionCode = ConvertOldSubdivisionCodeToNewSubdivisionCode(subdivisionCode);
            return newSubdivisionCode.GetSubdivisionParentCode();
        }

        /// <summary>
        /// Get the native name of a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <param name="iso639LanguageCode">ISO 639 language code for the language of the name to retrieve.</param>
        /// <param name="romanizationSystem">System by which the written form of the name was converted from native script to Latin script.</param>
        /// <returns>Native name of the country subdivision, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionNativeName(this Iso3166Part2 subdivisionCode, string iso639LanguageCode, string? romanizationSystem = null)
#else
        [CanBeNull]
        public static string GetSubdivisionNativeName(this Iso3166Part2 subdivisionCode, [NotNull] string iso639LanguageCode, string romanizationSystem = null)
#endif
        {
            var newSubdivisionCode = ConvertOldSubdivisionCodeToNewSubdivisionCode(subdivisionCode);
            return newSubdivisionCode.GetSubdivisionNativeName(iso639LanguageCode, romanizationSystem);
        }

        /// <summary>
        /// Get the local variant of a native name for a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <param name="iso639LanguageCode">ISO 639 language code for the language of the name to retrieve.</param>
        /// <param name="romanizationSystem">System by which the written form of the name was converted from native script to Latin script.</param>
        /// <returns>Local variant of the country subdivision's native name, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionNativeNameLocalVariant(this Iso3166Part2 subdivisionCode, string iso639LanguageCode, string? romanizationSystem = null)
#else
        [CanBeNull]
        public static string GetSubdivisionNativeNameLocalVariant(this Iso3166Part2 subdivisionCode, [NotNull] string iso639LanguageCode, string romanizationSystem = null)
#endif
        {
            var newSubdivisionCode = ConvertOldSubdivisionCodeToNewSubdivisionCode(subdivisionCode);
            return newSubdivisionCode.GetSubdivisionNativeNameLocalVariant(iso639LanguageCode, romanizationSystem);
        }

        private static Iso3166Part2Subdivision ConvertOldSubdivisionCodeToNewSubdivisionCode(Iso3166Part2 subdivisionCode)
        {
#if NETCOREAPP3_0_OR_GREATER
            var code = (string?)subdivisionCode; 
#else
            var code = (string)subdivisionCode;
#endif
            if (code is null) throw new InvalidOperationException("Subdivision code is not valid.");
            
            return (Iso3166Part2Subdivision)code;
        }
    }
}