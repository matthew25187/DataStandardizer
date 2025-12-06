using System;
using System.Linq;
using System.Reflection;
using DataStandardizer.Core;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Language
{
    public static class Iso639Extensions
    {
        #region Public Methods

        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part1Language languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part1Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.EnglishName;
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part1Language languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER||NET
            return codeAttribute?.EnglishNames ?? Array.Empty<string>();
#else
            return codeAttribute?.EnglishNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part2BLanguage languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part2BLanguage languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.EnglishName;
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language codes.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part2BLanguage languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER || NET
            return codeAttribute?.EnglishNames ?? Array.Empty<string>();
#else
            return codeAttribute?.EnglishNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part2TLanguage languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part2TLanguage languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.EnglishName;
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part2TLanguage languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER || NET
            return codeAttribute?.EnglishNames ?? Array.Empty<string>();
#else
            return codeAttribute?.EnglishNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part3Language languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part3Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.EnglishName;
        }

        /// <summary>
        /// Gets the English name of the language family code.
        /// </summary>
        /// <param name="languageCode">Language family code.</param>
        /// <returns>English name of the language family code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part5LanguageFamily languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part5LanguageFamily languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.EnglishName;
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part5LanguageFamily languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER||NET
            return codeAttribute?.EnglishNames ?? Array.Empty<string>();
#else
            return codeAttribute?.EnglishNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part1Language languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part1Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.FrenchName;
        }

        /// <summary>
        /// Gets the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part1Language languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER||NET
            return codeAttribute?.FrenchNames ?? Array.Empty<string>();
#else
            return codeAttribute?.FrenchNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part2BLanguage languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part2BLanguage languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.FrenchName;
        }

        /// <summary>
        /// Gets the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part2BLanguage languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER||NET
            return codeAttribute?.FrenchNames ?? Array.Empty<string>();
#else
            return codeAttribute?.FrenchNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part2TLanguage languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part2TLanguage languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.FrenchName;
        }

        /// <summary>
        /// Gets the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part2TLanguage languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER||NET
            return codeAttribute?.FrenchNames ?? Array.Empty<string>();
#else
            return codeAttribute?.FrenchNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part5LanguageFamily languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part5LanguageFamily languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.FrenchName;
        }

        /// <summary>
        /// Get the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part5LanguageFamily languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
#if NETSTANDARD1_3_OR_GREATER||NET
            return codeAttribute?.FrenchNames ?? Array.Empty<string>();
#else
            return codeAttribute?.FrenchNames ?? new string[] { };
#endif
        }

        /// <summary>
        /// Get the print name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Print name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPrintName(this Iso639Part3Language languageCode)
#else
        [CanBeNull]
        public static string GetPrintName(this Iso639Part3Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.PrintName;
        }

        /// <summary>
        /// Get the inverted name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Inverted name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetInvertedName(this Iso639Part3Language languageCode)
#else
        [CanBeNull]
        public static string GetInvertedName(this Iso639Part3Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.InvertedName;
        }

        /// <summary>
        /// Get the related ISO 639 Part 1 Alpha-2 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 1 Alpha-2 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart1Code(this Iso639Part2BLanguage languageCode)
#else
        [CanBeNull]
        public static string GetPart1Code(this Iso639Part2BLanguage languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Part1Code;
        }

        /// <summary>
        /// Get the related ISO 639 Part 1 Alpha-2 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 1 Alpha-2 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart1Code(this Iso639Part2TLanguage languageCode)
#else
        [CanBeNull]
        public static string GetPart1Code(this Iso639Part2TLanguage languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Part1Code;
        }

        /// <summary>
        /// Get the related ISO 639 Part 1 Alpha-2 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 1 Alpha-2 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart1Code(this Iso639Part3Language languageCode)
#else
        [CanBeNull]
        public static string GetPart1Code(this Iso639Part3Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Part1Code;
        }

        /// <summary>
        /// Get the related ISO 639 Part 2B Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2B Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2BCode(this Iso639Part1Language languageCode)
#else
        [CanBeNull]
        public static string GetPart2BCode(this Iso639Part1Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Part2BCode;
        }

        /// <summary>
        /// Get the related ISO 639 Part 2B Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2B Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2BCode(this Iso639Part3Language languageCode)
#else
        [CanBeNull]
        public static string GetPart2BCode(this Iso639Part3Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Part2BCode;
        }

        /// <summary>
        /// Get the related ISO 639 Part 2T Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2T Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2TCode(this Iso639Part1Language languageCode)
#else
        [CanBeNull]
        public static string GetPart2TCode(this Iso639Part1Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Part2TCode;
        }

        /// <summary>
        /// Get the related ISO 639 Part 2T Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2T Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2TCode(this Iso639Part3Language languageCode)
#else
        [CanBeNull]
        public static string GetPart2TCode(this Iso639Part3Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Part2TCode;
        }

        /// <summary>
        /// Get the macrolanguage code related to this language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Macrolanguage code for this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetMacrolanguageCode(this Iso639Part3Language languageCode)
#else
        [CanBeNull]
        public static string GetMacrolanguageCode(this Iso639Part3Language languageCode)
#endif
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.MacrolanguageCode;
        }

        /// <summary>
        /// Get the scope of the language represented by this code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Scope of the language, if found; otherwise <c>null</c>.</returns>
        public static Iso639LanguageScope? GetScope(this Iso639Part3Language languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.Scope;
        }

        /// <summary>
        /// Get the language type of the language represented by this code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Type of the language, if found; otherwise <c>null</c>.</returns>
        public static Iso639LanguageType? GetLanguageType(this Iso639Part3Language languageCode)
        {
            var memberField = GetMemberField(languageCode);
            var codeAttribute = memberField?.GetCustomAttribute<Iso639LanguageCodeAttribute>();
            return codeAttribute?.LanguageType;
        }

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private static FieldInfo? GetMemberField(object enumMember)
#else
        [CanBeNull]
        private static FieldInfo GetMemberField(object enumMember)
#endif
        {
            var fieldName = StringEnum.GetName(enumMember.GetType(), enumMember);
            return enumMember.GetType().GetTypeInfo().DeclaredFields.SingleOrDefault(field => field.IsPublic && field.IsStatic && field.Name == fieldName);
        }

        #endregion
    }
}