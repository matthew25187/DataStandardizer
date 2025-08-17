using System;
using System.Linq;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.UNM49
{
    public static class UnM49Extensions
    {
        /// <summary>
        /// Get all M49 codes.
        /// </summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <returns>A collection of M49 codes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not an <see cref="Enum"/>.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static ushort[] GetM49Codes(Type enumType)
#else
        public static ushort[] GetM49Codes([NotNull] Type enumType)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (!enumType.GetTypeInfo().IsEnum)
                throw new ArgumentException($"{nameof(enumType)} is not an {nameof(Enum)}.", nameof(enumType));

            var codeAttributes = enumType.GetTypeInfo().DeclaredFields.Select(field => field.GetCustomAttribute<UnM49AreaCodeAttribute>()).Where(attribute => attribute != null).ToArray();
            var globalCodes = codeAttributes.Select(attribute => attribute?.GlobalCode).Where(code => code.HasValue).Cast<ushort>();
            var regionCodes = codeAttributes.Select(attribute => attribute?.RegionCode).Where(code => code.HasValue).Cast<ushort>();
            var subRegionCodes = codeAttributes.Select(attribute => attribute?.SubRegionCode).Where(code => code.HasValue).Cast<ushort>();
            var intermediateRegionCodes = codeAttributes.Select(attribute => attribute?.IntermediateRegionCode).Where(code => code.HasValue).Cast<ushort>();
            var m49Codes = Enum.GetValues(enumType).Cast<ushort>();
            return globalCodes.Union(regionCodes).Union(subRegionCodes).Union(intermediateRegionCodes).Union(m49Codes).ToArray();
        }

        /// <summary>
        /// Get all M49 codes.
        /// </summary>
        /// <typeparam name="T">An enumeration type.</typeparam>
        /// <returns>A collection of M49 codes.</returns>
        public static ushort[] GetM49Codes<T>() where T : struct, Enum
        {
            return GetM49Codes(typeof(T));
        }

        /// <summary>
        /// Get the global code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Global code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetGlobalCode(this UnM49AreaByAlpha2CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetGlobalCode(m49Code);
        }

        /// <summary>
        /// Get the global code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Global code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetGlobalCode(this UnM49AreaByAlpha3CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetGlobalCode(m49Code);
        }

        /// <summary>
        /// Get the name for the global code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Global name for the <see cref="UnM49AreaByAlpha2CountryCode"/> code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetGlobalName(this UnM49AreaByAlpha2CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetGlobalName(this UnM49AreaByAlpha2CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetGlobalName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the name for the global code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Global name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetGlobalName(this UnM49AreaByAlpha3CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetGlobalName(this UnM49AreaByAlpha3CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetGlobalName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the region code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetRegionCode(this UnM49AreaByAlpha2CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetRegionCode(m49Code);
        }

        /// <summary>
        /// Get the region code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetRegionCode(this UnM49AreaByAlpha3CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetRegionCode(m49Code);
        }

        /// <summary>
        /// Get the name for the region code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetRegionName(this UnM49AreaByAlpha2CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetRegionName(this UnM49AreaByAlpha2CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetRegionName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the name for the region code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetRegionName(this UnM49AreaByAlpha3CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetRegionName(this UnM49AreaByAlpha3CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetRegionName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the sub-region code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Sub-region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetSubRegionCode(this UnM49AreaByAlpha2CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetSubRegionCode(m49Code);
        }

        /// <summary>
        /// Get the sub-region code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Sub-region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetSubRegionCode(this UnM49AreaByAlpha3CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetSubRegionCode(m49Code);
        }

        /// <summary>
        /// Get the name for the sub-region code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Sub-region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubRegionName(this UnM49AreaByAlpha2CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetSubRegionName(this UnM49AreaByAlpha2CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetSubRegionName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the name for the sub-region code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Sub-region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubRegionName(this UnM49AreaByAlpha3CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetSubRegionName(this UnM49AreaByAlpha3CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetSubRegionName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the intermediate region code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Intermediate region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetIntermediateRegionCode(this UnM49AreaByAlpha2CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIntermediateRegionCode(m49Code);
        }

        /// <summary>
        /// Get the intermediate region code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Intermediate region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetIntermediateRegionCode(this UnM49AreaByAlpha3CountryCode m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIntermediateRegionCode(m49Code);
        }

        /// <summary>
        /// Get the name for the intermediate region code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Intermediate region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIntermediateRegionName(this UnM49AreaByAlpha2CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetIntermediateRegionName(this UnM49AreaByAlpha2CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIntermediateRegionName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the name for the intermediate region code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Intermediate region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIntermediateRegionName(this UnM49AreaByAlpha3CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetIntermediateRegionName(this UnM49AreaByAlpha3CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIntermediateRegionName(m49Code, languageCode);
        }

        /// <summary>
        /// Gets the name for the M49 code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Country or area name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCountryOrAreaName(this UnM49AreaByAlpha2CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetCountryOrAreaName(this UnM49AreaByAlpha2CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetCountryOrAreaName(m49Code, languageCode);
        }

        /// <summary>
        /// Gets the name for the M49 code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Country or area name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCountryOrAreaName(this UnM49AreaByAlpha3CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetCountryOrAreaName(this UnM49AreaByAlpha3CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetCountryOrAreaName(m49Code, languageCode);
        }

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetCountryOrAreaName<T>(T m49Code, string languageCode)
#else
        [CanBeNull]
        private static string DoGetCountryOrAreaName<T>(T m49Code, [NotNull] string languageCode)
#endif
            where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            switch (languageCode)
            {
                case "en":
                case "eng":
                    return codeAttribute?.EnglishCountryOrAreaName;
                case "zh":
                case "zho":
                    return codeAttribute?.ChineseCountryOrAreaName;
                case "ru":
                case "rus":
                    return codeAttribute?.RussianCountryOrAreaName;
                case "fr":
                case "fra":
                    return codeAttribute?.FrenchCountryOrAreaName;
                case "es":
                case "spa":
                    return codeAttribute?.SpanishCountryOrAreaName;
                case "ar":
                case "ara":
                    return codeAttribute?.ArabicCountryOrAreaName;
            }

            return null;
        }

        private static ushort? DoGetGlobalCode<T>(T m49Code) where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            return codeAttribute?.GlobalCode;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetGlobalName<T>(T m49Code,string languageCode)
#else
        [CanBeNull]
        private static string DoGetGlobalName<T>(T m49Code, [NotNull] string languageCode)
#endif
            where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            switch (languageCode)
            {
                case "en":
                case "eng":
                    return codeAttribute?.EnglishGlobalName;
                case "zh":
                case "zho":
                    return codeAttribute?.ChineseGlobalName;
                case "ru":
                case "rus":
                    return codeAttribute?.RussianGlobalName;
                case "fr":
                case "fra":
                    return codeAttribute?.FrenchGlobalName;
                case "es":
                case "spa":
                    return codeAttribute?.SpanishGlobalName;
                case "ar":
                case "ara":
                    return codeAttribute?.ArabicGlobalName;
            }

            return null;
        }

        private static ushort? DoGetIntermediateRegionCode<T>(T m49Code) where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            return codeAttribute?.IntermediateRegionCode;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetIntermediateRegionName<T>(T m49Code, string languageCode)
#else
        [CanBeNull]
        private static string DoGetIntermediateRegionName<T>(T m49Code, [NotNull] string languageCode)
#endif
            where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            switch (languageCode)
            {
                case "en":
                case "eng":
                    return codeAttribute?.EnglishIntermediateRegionName;
                case "zh":
                case "zho":
                    return codeAttribute?.ChineseIntermediateRegionName;
                case "ru":
                case "rus":
                    return codeAttribute?.RussianIntermediateRegionName;
                case "fr":
                case "fra":
                    return codeAttribute?.FrenchIntermediateRegionName;
                case "es":
                case "spa":
                    return codeAttribute?.SpanishIntermediateRegionName;
                case "ar":
                case "ara":
                    return codeAttribute?.ArabicIntermediateRegionName;
            }

            return null;
        }

        private static ushort? DoGetRegionCode<T>(T m49Code) where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            return codeAttribute?.RegionCode;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetRegionName<T>(T m49Code, string languageCode)
#else
        [CanBeNull]
        private static string DoGetRegionName<T>(T m49Code, [NotNull] string languageCode)
#endif
            where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            switch (languageCode)
            {
                case "en":
                case "eng":
                    return codeAttribute?.EnglishRegionName;
                case "zh":
                case "zho":
                    return codeAttribute?.ChineseRegionName;
                case "ru":
                case "rus":
                    return codeAttribute?.RussianRegionName;
                case "fr":
                case "fra":
                    return codeAttribute?.FrenchRegionName;
                case "es":
                case "spa":
                    return codeAttribute?.SpanishRegionName;
                case "ar":
                case "ara":
                    return codeAttribute?.ArabicRegionName;
            }

            return null;
        }

        private static ushort? DoGetSubRegionCode<T>(T m49Code) where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            return codeAttribute?.SubRegionCode;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetSubRegionName<T>(T m49Code, string languageCode)
#else
        [CanBeNull]
        private static string DoGetSubRegionName<T>(T m49Code, [NotNull] string languageCode)
#endif
            where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            var codeAttribute = m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
            switch (languageCode)
            {
                case "en":
                case "eng":
                    return codeAttribute?.EnglishSubRegionName;
                case "zh":
                case "zho":
                    return codeAttribute?.ChineseSubRegionName;
                case "ru":
                case "rus":
                    return codeAttribute?.RussianSubRegionName;
                case "fr":
                case "fra":
                    return codeAttribute?.FrenchSubRegionName;
                case "es":
                case "spa":
                    return codeAttribute?.SpanishSubRegionName;
                case "ar":
                case "ara":
                    return codeAttribute?.ArabicSubRegionName;
            }

            return null;
        }

        #endregion
    }
}