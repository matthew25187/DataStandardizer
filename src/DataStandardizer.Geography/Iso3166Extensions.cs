using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Geography
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
        public static string? GetEnglishName(this Iso3166Part1Alpha2Country countryCode, Iso3166CountryName nameType)
#else
        [return: MaybeNull]
        public static string GetEnglishName(this Iso3166Part1Alpha2Country countryCode, Iso3166CountryName nameType)
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
        /// <param name="iso639LanguageCode">ISO 639-1 or ISO 639-2/T language code for the language of the name to retrieve, such as <c>en</c> or <c>eng</c>, matched without regard to case (RFC 5646 §2.1.1). A bibliographic ISO 639-2/B code, such as <c>fre</c>, is not matched, and a <c>null</c> code gives <c>null</c>.</param>
        /// <param name="nameType">Type of name to retrieve.</param>
        /// <returns>Native name of the country, if found; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetNativeName(this Iso3166Part1Alpha2Country countryCode, string iso639LanguageCode, Iso3166CountryName nameType)
#else
        [return: MaybeNull]
        public static string GetNativeName(this Iso3166Part1Alpha2Country countryCode, [DisallowNull] string iso639LanguageCode, Iso3166CountryName nameType)
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
        public static bool IsIndependent(this Iso3166Part1Alpha2Country countryCode)
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
        public static string? GetEnglishName(this Iso3166Part1Alpha3Country countryCode, Iso3166CountryName nameType)
#else
        [return: MaybeNull]
        public static string GetEnglishName(this Iso3166Part1Alpha3Country countryCode, Iso3166CountryName nameType)
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
        /// <param name="iso639LanguageCode">ISO 639-1 or ISO 639-2/T language code for the language of the name to retrieve, such as <c>en</c> or <c>eng</c>, matched without regard to case (RFC 5646 §2.1.1). A bibliographic ISO 639-2/B code, such as <c>fre</c>, is not matched, and a <c>null</c> code gives <c>null</c>.</param>
        /// <param name="nameType">Type of name to retrieve.</param>
        /// <returns>Native name of the country, if found; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetNativeName(this Iso3166Part1Alpha3Country countryCode, string iso639LanguageCode, Iso3166CountryName nameType)
#else
        [return: MaybeNull]
        public static string GetNativeName(this Iso3166Part1Alpha3Country countryCode, [DisallowNull] string iso639LanguageCode, Iso3166CountryName nameType)
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
        public static bool IsIndependent(this Iso3166Part1Alpha3Country countryCode)
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
        public static ushort? GetSubdivisionCategoryIdentifier(this Iso3166Part2Subdivision subdivisionCode)
        {
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);
            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            return subdivisionCodeAttribute?.SubdivisionCategoryIdentifier;
        }

        /// <summary>
        /// Get the name of a country subdivision category.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <param name="iso639LanguageCode">ISO 639-1 or ISO 639-2/T language code for the language of the name to retrieve, such as <c>en</c> or <c>eng</c>, matched without regard to case (RFC 5646 §2.1.1). A bibliographic ISO 639-2/B code, such as <c>fre</c>, is not matched, and a <c>null</c> code gives <c>null</c>.</param>
        /// <returns>Name of the category, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionCategoryName(this Iso3166Part2Subdivision subdivisionCode, string iso639LanguageCode)
#else
        [return: MaybeNull]
        public static string GetSubdivisionCategoryName(this Iso3166Part2Subdivision subdivisionCode, [DisallowNull] string iso639LanguageCode)
#endif
        {
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);

            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            var subdivisionCodeCategoryIdentifier = subdivisionCodeAttribute?.SubdivisionCategoryIdentifier;

            var subdivisionCategoryNameAttributes = countrySubdivisionCodeInformation?.CountryType.GetTypeInfo().GetCustomAttributes<Iso3166SubdivisionCategoryNameAttribute>();
            var subdivisionCategoryNameAttribute = subdivisionCategoryNameAttributes?
                .FirstOrDefault(attribute =>
                    attribute.CategoryIdentifier == subdivisionCodeCategoryIdentifier && MatchesLanguage(attribute, iso639LanguageCode));

            return subdivisionCategoryNameAttribute?.CategoryName;
        }

        /// <summary>
        /// Get the name of a country subdivision category in plural form.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <param name="iso639LanguageCode">ISO 639-1 or ISO 639-2/T language code for the language of the name to retrieve, such as <c>en</c> or <c>eng</c>, matched without regard to case (RFC 5646 §2.1.1). A bibliographic ISO 639-2/B code, such as <c>fre</c>, is not matched, and a <c>null</c> code gives <c>null</c>.</param>
        /// <returns>Plural-form name of the category, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionCategoryNamePlural(this Iso3166Part2Subdivision subdivisionCode, string iso639LanguageCode)
#else
        [return: MaybeNull]
        public static string GetSubdivisionCategoryNamePlural(this Iso3166Part2Subdivision subdivisionCode, [DisallowNull] string iso639LanguageCode)
#endif
        {
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);

            var subdivisionCodeAttribute = countrySubdivisionCodeInformation?.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            var subdivisionCodeCategoryIdentifier = subdivisionCodeAttribute?.SubdivisionCategoryIdentifier;

            var subdivisionCategoryNameAttributes = countrySubdivisionCodeInformation?.CountryType.GetTypeInfo().GetCustomAttributes<Iso3166SubdivisionCategoryNameAttribute>();
            var subdivisionCategoryNameAttribute = subdivisionCategoryNameAttributes?
                .FirstOrDefault(attribute =>
                    attribute.CategoryIdentifier == subdivisionCodeCategoryIdentifier && MatchesLanguage(attribute, iso639LanguageCode));

            return subdivisionCategoryNameAttribute?.CategoryNamePlural;
        }

        /// <summary>
        /// Get the alpha code for a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <returns>Subdivision code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionCode(this Iso3166Part2Subdivision subdivisionCode)
#else
        [return: MaybeNull]
        public static string GetSubdivisionCode(this Iso3166Part2Subdivision subdivisionCode)
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
        public static string? GetSubdivisionParentCode(this Iso3166Part2Subdivision subdivisionCode)
#else
        public static string GetSubdivisionParentCode(this Iso3166Part2Subdivision subdivisionCode)
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
        /// <param name="iso639LanguageCode">ISO 639-1 or ISO 639-2/T language code for the language of the name to retrieve, such as <c>en</c> or <c>eng</c>, matched without regard to case (RFC 5646 §2.1.1). A bibliographic ISO 639-2/B code, such as <c>fre</c>, is not matched, and a <c>null</c> code gives <c>null</c>.</param>
        /// <param name="romanizationSystem">
        /// System by which the written form of the name was converted from native script to Latin script, or <c>null</c> to accept a name in any romanization system.
        /// </param>
        /// <returns>Native name of the country subdivision, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// ISO 3166-2 can give a subdivision several names in one language, each in a different romanization system.
        /// When <paramref name="romanizationSystem"/> is <c>null</c> and there are several, the name ISO 3166-2 lists first for that language is used.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionNativeName(this Iso3166Part2Subdivision subdivisionCode, string iso639LanguageCode, string? romanizationSystem = null)
#else
        [return: MaybeNull]
        public static string GetSubdivisionNativeName(this Iso3166Part2Subdivision subdivisionCode, [DisallowNull] string iso639LanguageCode, [AllowNull] string romanizationSystem = null)
#endif
        {
            var subdivisionCodeNameAttribute = FindSubdivisionNameAttribute(subdivisionCode, iso639LanguageCode, romanizationSystem);

            return subdivisionCodeNameAttribute?.SubdivisionName;
        }

        /// <summary>
        /// Get the local variant of a native name for a country subdivision.
        /// </summary>
        /// <param name="subdivisionCode">Subdivision code.</param>
        /// <param name="iso639LanguageCode">ISO 639-1 or ISO 639-2/T language code for the language of the name to retrieve, such as <c>en</c> or <c>eng</c>, matched without regard to case (RFC 5646 §2.1.1). A bibliographic ISO 639-2/B code, such as <c>fre</c>, is not matched, and a <c>null</c> code gives <c>null</c>.</param>
        /// <param name="romanizationSystem">
        /// System by which the written form of the name was converted from native script to Latin script, or <c>null</c> to accept a name in any romanization system.
        /// </param>
        /// <returns>Local variant of the country subdivision's native name, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// ISO 3166-2 can give a subdivision several names in one language, each in a different romanization system.
        /// When <paramref name="romanizationSystem"/> is <c>null</c> and there are several, the name ISO 3166-2 lists first for that language is used.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubdivisionNativeNameLocalVariant(this Iso3166Part2Subdivision subdivisionCode, string iso639LanguageCode, string? romanizationSystem = null)
#else
        [return: MaybeNull]
        public static string GetSubdivisionNativeNameLocalVariant(this Iso3166Part2Subdivision subdivisionCode, [DisallowNull] string iso639LanguageCode, [AllowNull] string romanizationSystem = null)
#endif
        {
            var subdivisionCodeNameAttribute = FindSubdivisionNameAttribute(subdivisionCode, iso639LanguageCode, romanizationSystem);

            return subdivisionCodeNameAttribute?.SubdivisionNameLocalVariant;
        }

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private static Iso3166SubdivisionNameAttribute? FindSubdivisionNameAttribute(Iso3166Part2Subdivision subdivisionCode, string iso639LanguageCode, string? romanizationSystem)
#else
        [return: MaybeNull]
        private static Iso3166SubdivisionNameAttribute FindSubdivisionNameAttribute(Iso3166Part2Subdivision subdivisionCode, [DisallowNull] string iso639LanguageCode, [AllowNull] string romanizationSystem)
#endif
        {
            var countrySubdivisionCodeInformation = GetCountrySubdivisionCodeField(subdivisionCode);
            if (countrySubdivisionCodeInformation is null)
            {
                return null;
            }

            var subdivisionCodeAttribute = countrySubdivisionCodeInformation.Value.SubdivisionCodeField.GetCustomAttribute<Iso3166SubdivisionCodeAttribute>();
            var subdivisionCodeNameAttributes = countrySubdivisionCodeInformation.Value.SubdivisionCodeField.GetCustomAttributes<Iso3166SubdivisionNameAttribute>();
            return SelectSubdivisionNameAttribute(subdivisionCodeNameAttributes, subdivisionCodeAttribute?.SubdivisionCategoryIdentifier, iso639LanguageCode, romanizationSystem);
        }

        /// <summary>
        /// Selects the name attribute for a language and, optionally, a romanization system. When several names match, the one with the lowest
        /// <see cref="Iso3166SubdivisionNameAttribute.Sequence"/> is returned, whatever order <paramref name="subdivisionNameAttributes"/> is in.
        /// Remaining ties, which the generated data doesn't contain, are broken by ordinal comparison of the ISO 639-2T code, the name and the
        /// romanization system, so the result never depends on enumeration order.
        /// </summary>
#if NETCOREAPP3_0_OR_GREATER
        internal static Iso3166SubdivisionNameAttribute? SelectSubdivisionNameAttribute(IEnumerable<Iso3166SubdivisionNameAttribute> subdivisionNameAttributes, ushort? subdivisionCategoryIdentifier, string iso639LanguageCode, string? romanizationSystem)
#else
        [return: MaybeNull]
        internal static Iso3166SubdivisionNameAttribute SelectSubdivisionNameAttribute(IEnumerable<Iso3166SubdivisionNameAttribute> subdivisionNameAttributes, ushort? subdivisionCategoryIdentifier, [DisallowNull] string iso639LanguageCode, [AllowNull] string romanizationSystem)
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            Iso3166SubdivisionNameAttribute? result = null;
#else
            Iso3166SubdivisionNameAttribute result = null;
#endif
            foreach (var attribute in subdivisionNameAttributes)
            {
                if (attribute.SubdivisionCategoryIdentifier != subdivisionCategoryIdentifier ||
                    !MatchesLanguage(attribute, iso639LanguageCode) ||
                    (romanizationSystem != null && attribute.RomanizationSystem != romanizationSystem))
                {
                    continue;
                }

                if (result is null || CompareSubdivisionNamePreference(attribute, result) < 0)
                {
                    result = attribute;
                }
            }

            return result;
        }

        private static int CompareSubdivisionNamePreference(Iso3166SubdivisionNameAttribute x, Iso3166SubdivisionNameAttribute y)
        {
            var result = x.Sequence.CompareTo(y.Sequence);
            if (result == 0)
            {
                result = string.CompareOrdinal(x.Iso639Part2TCode, y.Iso639Part2TCode);
            }

            if (result == 0)
            {
                result = string.CompareOrdinal(x.SubdivisionName, y.SubdivisionName);
            }

            if (result == 0)
            {
                result = string.CompareOrdinal(x.RomanizationSystem, y.RomanizationSystem);
            }

            return result;
        }

        private static bool MatchesLanguage(Iso3166LanguageAttributeBase attribute, string iso639LanguageCode)
        {
            return Iso639LanguageCode.Matches(iso639LanguageCode, attribute.Iso639Part1Code) ||
                   Iso639LanguageCode.Matches(iso639LanguageCode, attribute.Iso639Part2TCode);
        }

        private static CountrySubdivisionCodeField? GetCountrySubdivisionCodeField(Iso3166Part2Subdivision countrySubdivisionCode)
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
        [return: MaybeNull]
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
        [return: MaybeNull]
        private static string GetNativeCountryName<T>(T countryCode, [DisallowNull] string iso639LanguageCode, Iso3166CountryName nameType)
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
                .FirstOrDefault(attribute => MatchesLanguage(attribute, iso639LanguageCode));

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