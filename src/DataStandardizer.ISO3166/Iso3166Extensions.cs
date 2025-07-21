using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.ISO3166
{
    public static class Iso3166Extensions
    {
        private static readonly Regex SubdivisionCodeExpression;

        static Iso3166Extensions()
        {
            var options = RegexOptions.CultureInvariant;
#if NETSTANDARD1_3_OR_GREATER||NET
            options |= RegexOptions.Compiled;
#endif
            SubdivisionCodeExpression = new Regex("^(?<country>[A-Z]{2})-(?<subdivision>[0-9A-Z]{1,3})$", options);
        }

        #region Public Methods: ISO 3166-1 Alpha-2

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
            if (!Enum.IsDefined(countryCode.GetType(), countryCode))
            {
                return null;
            }

            return GetEnglishCountryName(countryCode, nameType);
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
            if (!Enum.IsDefined(countryCode.GetType(), countryCode))
            {
                return null;
            }

            return GetNativeCountryName(countryCode, iso639LanguageCode, nameType);
        }

        /// <summary>
        /// Determine if the country or place is independent.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <returns><c>true</c> if the country or place is independent; <c>false</c> if not.</returns>
        public static bool IsIndependent(this Iso3166Part1Alpha2 countryCode)
        {
            if (!Enum.IsDefined(countryCode.GetType(), countryCode))
            {
                return false;
            }

#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(countryCode.GetType(), countryCode)!;
#else
            var codeName = Enum.GetName(countryCode.GetType(), countryCode);
#endif
            var codeAttribute = countryCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso3166CountryCodeAttribute>();
            return codeAttribute?.IsIndependent ?? false;
        }

        #endregion

        #region Public Methods: ISO 3166-1 Alpha-3

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
            if (!Enum.IsDefined(countryCode.GetType(), countryCode))
            {
                return null;
            }

            return GetEnglishCountryName(countryCode, nameType);
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
            if (!Enum.IsDefined(countryCode.GetType(), countryCode))
            {
                return null;
            }

            return GetNativeCountryName(countryCode, iso639LanguageCode, nameType);
        }


        /// <summary>
        /// Determine if the country or place is independent.
        /// </summary>
        /// <param name="countryCode">Country or place code.</param>
        /// <returns><c>true</c> if the country or place is independent; <c>false</c> if not.</returns>
        public static bool IsIndependent(this Iso3166Part1Alpha3 countryCode)
        {
            if (!Enum.IsDefined(countryCode.GetType(), countryCode))
            {
                return false;
            }

#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(countryCode.GetType(), countryCode)!;
#else
            var codeName = Enum.GetName(countryCode.GetType(), countryCode);
#endif
            var codeAttribute = countryCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso3166CountryCodeAttribute>();
            return codeAttribute?.IsIndependent ?? false;
        }

        #endregion

        #region Public Methods: ISO 3166-2

        /// <summary>
        /// Get the category identifier of a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <returns>Identifier of the subdivision's category, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetSubdivisionCategoryIdentifier(this Iso3166Part2 subdivisionCode)
        {
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);
            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            return subdivisionCodeAttribute?.SubdivisionCategoryIdentifier;
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
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);

            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            var subdivisionCodeCategoryIdentifier = subdivisionCodeAttribute?.SubdivisionCategoryIdentifier;

            var subdivisionCategoryNameAttributes = countrySubdivisionCodeInformation?.CountryType.GetTypeInfo().GetCustomAttributes<Iso3166SubdivisionCategoryNameAttribute>();
            var subdivisionCategoryNameAttribute = subdivisionCategoryNameAttributes?
                .FirstOrDefault(attribute =>
                    attribute.CategoryIdentifier == subdivisionCodeCategoryIdentifier && (string.Equals(iso639LanguageCode, attribute.Iso639Part1Code, StringComparison.OrdinalIgnoreCase) ||
                                                                                          string.Equals(iso639LanguageCode, attribute.Iso639Part2TCode, StringComparison.OrdinalIgnoreCase)));

            return subdivisionCategoryNameAttribute?.CategoryName;
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
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);

            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            var subdivisionCodeCategoryIdentifier = subdivisionCodeAttribute?.SubdivisionCategoryIdentifier;

            var subdivisionCategoryNameAttributes = countrySubdivisionCodeInformation?.CountryType.GetTypeInfo().GetCustomAttributes<Iso3166SubdivisionCategoryNameAttribute>();
            var subdivisionCategoryNameAttribute = subdivisionCategoryNameAttributes?
                .FirstOrDefault(attribute =>
                    attribute.CategoryIdentifier == subdivisionCodeCategoryIdentifier && (string.Equals(iso639LanguageCode, attribute.Iso639Part1Code, StringComparison.OrdinalIgnoreCase) ||
                                                                                          string.Equals(iso639LanguageCode, attribute.Iso639Part2TCode, StringComparison.OrdinalIgnoreCase)));

            return subdivisionCategoryNameAttribute?.CategoryNamePlural;
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
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);
            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            return subdivisionCodeAttribute?.SubdivisionCode;
        }

        /// <summary>
        /// Get the parent alpha code for a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <returns>Parent subdivision code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionParentCode(this Iso3166Part2 subdivisionCode)
#else
        public static string GetSubdivisionParentCode(this Iso3166Part2 subdivisionCode)
#endif
        {
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);
            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            return subdivisionCodeAttribute?.SubdivisionParentCode;
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
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);

            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            var subdivisionCodeNameAttributes = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttributes<Iso3166SubdivisionNameAttribute>();
            var subdivisionCodeNameAttribute = subdivisionCodeNameAttributes?
                .FirstOrDefault(attribute =>
                    attribute.SubdivisionCategoryIdentifier == subdivisionCodeAttribute?.SubdivisionCategoryIdentifier && (string.Equals(attribute.Iso639Part1Code, iso639LanguageCode, StringComparison.OrdinalIgnoreCase) ||
                                                                                                                           string.Equals(attribute.Iso639Part2TCode, iso639LanguageCode, StringComparison.OrdinalIgnoreCase)) &&
                    (romanizationSystem is null || attribute.RomanizationSystem == romanizationSystem));

            return subdivisionCodeNameAttribute?.SubdivisionName;
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
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);

            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            var subdivisionCodeNameAttributes = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttributes<Iso3166SubdivisionNameAttribute>();
            var subdivisionCodeNameAttribute = subdivisionCodeNameAttributes?
                .FirstOrDefault(attribute =>
                    attribute.SubdivisionCategoryIdentifier == subdivisionCodeAttribute?.SubdivisionCategoryIdentifier && (string.Equals(attribute.Iso639Part1Code, iso639LanguageCode, StringComparison.OrdinalIgnoreCase) ||
                                                                                                                           string.Equals(attribute.Iso639Part2TCode, iso639LanguageCode, StringComparison.OrdinalIgnoreCase)) &&
                    (romanizationSystem is null || attribute.RomanizationSystem == romanizationSystem));

            return subdivisionCodeNameAttribute?.SubdivisionNameLocalVariant;
        }

        #endregion

        #region Private Methods

        private static CountrySubdivisionCodeField? GetCountrySubdivisionCodeField(Iso3166Part2 countrySubdivisionCode)
        {
#if NETCOREAPP3_0_OR_GREATER
            var countrySubdivisionCodeString = (string?)countrySubdivisionCode;
#else
            var countrySubdivisionCodeString = (string)countrySubdivisionCode;
#endif
            var countrySubdivisionCodeMatch = SubdivisionCodeExpression.Match(countrySubdivisionCodeString ?? string.Empty);
            if (!countrySubdivisionCodeMatch.Success)
            {
                return null;
            }

            var countryType = countrySubdivisionCode.GetType().GetTypeInfo().DeclaredNestedTypes
                .Where(typeInfo => typeInfo.IsNestedPublic && typeInfo.IsSealed && typeInfo.IsAbstract && typeInfo.Name == countrySubdivisionCodeMatch.Groups["country"].Value)
                .Select(typeInfo => typeInfo.AsType())
                .SingleOrDefault();
            var subdivisionCodeField = countryType?.GetTypeInfo().DeclaredFields.SingleOrDefault(field => field.IsPublic && field.IsStatic && field.Name == $"_{countrySubdivisionCodeMatch.Groups["subdivision"].Value}");
            if (countryType is null || subdivisionCodeField is null)
            {
                return null;
            }

            return new CountrySubdivisionCodeField(countryType, subdivisionCodeField);
        }
#if NETCOREAPP3_0_OR_GREATER
        private static string? GetEnglishCountryName<T>(T countryCode, Iso3166CountryName nameType)
#else
        [CanBeNull]
        private static string GetEnglishCountryName<T>(T countryCode, Iso3166CountryName nameType)
#endif
            where T : struct, Enum
        {
#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(countryCode.GetType(), countryCode)!;
#else
            var codeName = Enum.GetName(countryCode.GetType(), countryCode);
#endif
            var codeAttribute = countryCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso3166CountryCodeAttribute>();

#if NETCOREAPP3_0_OR_GREATER
            string? result = null;
#else
            string result = null;
#endif
            switch (nameType)
            {
                case Iso3166CountryName.Short:
                    result = codeAttribute?.EnglishShortName;
                    break;
                case Iso3166CountryName.ShortUpper:
                    result = codeAttribute?.EnglishShortNameUpper;
                    break;
                case Iso3166CountryName.Full:
                    result = codeAttribute?.EnglishFullName;
                    break;
            }

            return result;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? GetNativeCountryName<T>(T countryCode, string iso639LanguageCode, Iso3166CountryName nameType)
#else
        [CanBeNull]
        private static string GetNativeCountryName<T>(T countryCode, [NotNull] string iso639LanguageCode, Iso3166CountryName nameType)
#endif
            where T : struct, Enum
        {
#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(countryCode.GetType(), countryCode)!;
#else
            var codeName = Enum.GetName(countryCode.GetType(), countryCode);
#endif
            var nameAttribute = countryCode.GetType().GetTypeInfo()
                .GetDeclaredField(codeName)
                ?.GetCustomAttributes<Iso3166CountryNameAttribute>()
                .FirstOrDefault(attribute => string.Equals(attribute.Iso639Part1Code, iso639LanguageCode, StringComparison.OrdinalIgnoreCase) || string.Equals(attribute.Iso639Part2TCode, iso639LanguageCode, StringComparison.OrdinalIgnoreCase));

#if NETCOREAPP3_0_OR_GREATER
            string? result = null;
#else
            string result = null;
#endif
            switch (nameType)
            {
                case Iso3166CountryName.Short:
                    result = nameAttribute?.ShortName;
                    break;
                case Iso3166CountryName.ShortUpper:
                    result = nameAttribute?.ShortNameUpper;
                    break;
                case Iso3166CountryName.Full:
                    result = nameAttribute?.FullName;
                    break;
            }

            return result;
        }

        #endregion

        private struct CountrySubdivisionCodeField
        {
            public CountrySubdivisionCodeField(Type countryType, FieldInfo subdivisionCodeField)
            {
                CountryType = countryType;
                SubdivisionCodeField = subdivisionCodeField;
            }

            public Type CountryType { get; }

            public FieldInfo SubdivisionCodeField { get; }
        }
    }
}