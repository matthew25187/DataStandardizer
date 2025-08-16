using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

#pragma warning disable CS0618

namespace DataStandardizer.ISO639
{
    public static class Iso639CompatibilityExtensions
    {
        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part1 languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part1 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishName();
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part1 languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishNames();
        }

        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part2B languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part2B languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishName();
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language codes.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part2B languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishNames();
        }

        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part2T languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part2T languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishName();
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part2T languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishNames();
        }

        /// <summary>
        /// Gets the English name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>English name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part3 languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part3 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishName();
        }

        /// <summary>
        /// Gets the English name of the language family code.
        /// </summary>
        /// <param name="languageCode">Language family code.</param>
        /// <returns>English name of the language family code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso639Part5 languageCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso639Part5 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishName();
        }

        /// <summary>
        /// Gets the English names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of English names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetEnglishNames(this Iso639Part5 languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetEnglishNames();
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part1 languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part1 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchName();
        }

        /// <summary>
        /// Gets the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part1 languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchNames();
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part2B languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part2B languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchName();
        }

        /// <summary>
        /// Gets the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part2B languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchNames();
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part2T languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part2T languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchName();
        }

        /// <summary>
        /// Gets the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part2T languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchNames();
        }

        /// <summary>
        /// Get the French name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>French name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso639Part5 languageCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso639Part5 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchName();
        }

        /// <summary>
        /// Get the French names for the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Collection of French names, if found; otherwise <c>null</c>.</returns>
        public static string[] GetFrenchNames(this Iso639Part5 languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetFrenchNames();
        }

        /// <summary>
        /// Get the print name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Print name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPrintName(this Iso639Part3 languageCode)
#else
        [CanBeNull]
        public static string GetPrintName(this Iso639Part3 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPrintName();
        }

        /// <summary>
        /// Get the inverted name of the language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Inverted name of the language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetInvertedName(this Iso639Part3 languageCode)
#else
        [CanBeNull]
        public static string GetInvertedName(this Iso639Part3 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetInvertedName();
        }

        /// <summary>
        /// Get the related ISO 639 Part 1 Alpha-2 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 1 Alpha-2 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart1Code(this Iso639Part2B languageCode)
#else
        [CanBeNull]
        public static string GetPart1Code(this Iso639Part2B languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPart1Code();
        }

        /// <summary>
        /// Get the related ISO 639 Part 1 Alpha-2 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 1 Alpha-2 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart1Code(this Iso639Part2T languageCode)
#else
        [CanBeNull]
        public static string GetPart1Code(this Iso639Part2T languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPart1Code();
        }

        /// <summary>
        /// Get the related ISO 639 Part 1 Alpha-2 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 1 Alpha-2 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart1Code(this Iso639Part3 languageCode)
#else
        [CanBeNull]
        public static string GetPart1Code(this Iso639Part3 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPart1Code();
        }

        /// <summary>
        /// Get the related ISO 639 Part 2B Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2B Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2BCode(this Iso639Part1 languageCode)
#else
        [CanBeNull]
        public static string GetPart2BCode(this Iso639Part1 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPart2BCode();
        }

        /// <summary>
        /// Get the related ISO 639 Part 2B Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2B Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2BCode(this Iso639Part3 languageCode)
#else
        [CanBeNull]
        public static string GetPart2BCode(this Iso639Part3 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPart2BCode();
        }

        /// <summary>
        /// Get the related ISO 639 Part 2T Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2T Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2TCode(this Iso639Part1 languageCode)
#else
        [CanBeNull]
        public static string GetPart2TCode(this Iso639Part1 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPart2TCode();
        }

        /// <summary>
        /// Get the related ISO 639 Part 2T Alpha-3 code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Part 2T Alpha-3 code related to this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetPart2TCode(this Iso639Part3 languageCode)
#else
        [CanBeNull]
        public static string GetPart2TCode(this Iso639Part3 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetPart2TCode();
        }

        /// <summary>
        /// Get the macrolanguage code related to this language code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Macrolanguage code for this language code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetMacrolanguageCode(this Iso639Part3 languageCode)
#else
        [CanBeNull]
        public static string GetMacrolanguageCode(this Iso639Part3 languageCode)
#endif
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetMacrolanguageCode();
        }

        /// <summary>
        /// Get the scope of the language represented by this code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Scope of the language, if found; otherwise <c>null</c>.</returns>
        public static Iso639LanguageScope? GetScope(this Iso639Part3 languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetScope();
        }

        /// <summary>
        /// Get the language type of the language represented by this code.
        /// </summary>
        /// <param name="languageCode">Language code.</param>
        /// <returns>Type of the language, if found; otherwise <c>null</c>.</returns>
        public static Iso639LanguageType? GetLanguageType(this Iso639Part3 languageCode)
        {
            var newLanguageCode = ConvertOldLanguageCodeToNewLanguageCode(languageCode);
            return newLanguageCode.GetLanguageType();
        }

        private static Iso639Part1Language ConvertOldLanguageCodeToNewLanguageCode(Iso639Part1 languageCode)
        {
#if NETCOREAPP3_0_OR_GREATER
            var code = (string?)languageCode; 
#else
            var code = (string)languageCode;
#endif
            if (code is null) throw new InvalidOperationException("Language code is invalid.");

            return (Iso639Part1Language)code;
        }

        private static Iso639Part2BLanguage ConvertOldLanguageCodeToNewLanguageCode(Iso639Part2B languageCode)
        {
#if NETCOREAPP3_0_OR_GREATER
            var code = (string?)languageCode; 
#else
            var code = (string)languageCode;
#endif
            if (code is null) throw new InvalidOperationException("Language code is invalid.");

            return (Iso639Part2BLanguage)code;
        }

        private static Iso639Part2TLanguage ConvertOldLanguageCodeToNewLanguageCode(Iso639Part2T languageCode)
        {
#if NETCOREAPP3_0_OR_GREATER
            var code = (string?)languageCode; 
#else
            var code = (string)languageCode;
#endif
            if (code is null) throw new InvalidOperationException("Language code is invalid.");

            return (Iso639Part2TLanguage)code;
        }

        private static Iso639Part3Language ConvertOldLanguageCodeToNewLanguageCode(Iso639Part3 languageCode)
        {
#if NETCOREAPP3_0_OR_GREATER
            var code = (string?)languageCode; 
#else
            var code = (string)languageCode;
#endif
            if (code is null) throw new InvalidOperationException("Language code is invalid.");

            return (Iso639Part3Language)code;
        }

        private static Iso639Part5LanguageFamily ConvertOldLanguageCodeToNewLanguageCode(Iso639Part5 languageCode)
        {
#if NETCOREAPP3_0_OR_GREATER
            var code = (string?)languageCode; 
#else
            var code = (string)languageCode;
#endif
            if (code is null) throw new InvalidOperationException("Language code is invalid.");

            return (Iso639Part5LanguageFamily)code;
        }
    }
}