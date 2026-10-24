using System;
using System.Linq;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Geography
{
    public static class UnM49Extensions
    {
        /// <summary>
        /// Get all M49 codes.
        /// </summary>
        /// <param name="enumType">An enumeration type.</param>
        /// <returns>A collection of M49 codes.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="enumType"/> is not an <see cref="System.Enum"/>.</exception>
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
        /// Get the global code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Global code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetGlobalCode(this UnM49Area m49Code)
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
        /// Get the name for the global code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Global name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetGlobalName(this UnM49Area m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetGlobalName(this UnM49Area m49Code, [NotNull] string languageCode)
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
        /// Get the region code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetRegionCode(this UnM49Area m49Code)
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
        /// Get the name for the region code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetRegionName(this UnM49Area m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetRegionName(this UnM49Area m49Code, [NotNull] string languageCode)
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
        /// Get the sub-region code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Sub-region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetSubRegionCode(this UnM49Area m49Code)
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
        /// Get the name for the sub-region code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Sub-region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubRegionName(this UnM49Area m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetSubRegionName(this UnM49Area m49Code, [NotNull] string languageCode)
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
        /// Get the intermediate region code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Intermediate region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetIntermediateRegionCode(this UnM49Area m49Code)
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
        /// Get the name for the intermediate region code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Intermediate region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIntermediateRegionName(this UnM49Area m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetIntermediateRegionName(this UnM49Area m49Code, [NotNull] string languageCode)
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

        /// <summary>
        /// Gets the name for the M49 code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Country or area name for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>Returns <c>null</c> where the code identifies the world, a region, a sub-region or an intermediate region rather than a country or area.</remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCountryOrAreaName(this UnM49Area m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetCountryOrAreaName(this UnM49Area m49Code, [NotNull] string languageCode)
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
        /// <returns>Name for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>Equivalent to <see cref="GetCountryOrAreaName(UnM49AreaByAlpha2CountryCode, string)"/>, as every code in this enumeration identifies a country or area.</remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName(this UnM49AreaByAlpha2CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetName(this UnM49AreaByAlpha2CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetName(m49Code, languageCode);
        }

        /// <summary>
        /// Gets the name for the M49 code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Name for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>Equivalent to <see cref="GetCountryOrAreaName(UnM49AreaByAlpha3CountryCode, string)"/>, as every code in this enumeration identifies a country or area.</remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName(this UnM49AreaByAlpha3CountryCode m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetName(this UnM49AreaByAlpha3CountryCode m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetName(m49Code, languageCode);
        }

        /// <summary>
        /// Gets the name of the area identified by the M49 code, at whatever level of the M49 hierarchy that area occupies.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Name of the area for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// A <see cref="UnM49Area"/> code may identify the world, a region, a sub-region, an intermediate region or a
        /// country or area.  This method resolves the name of the level the code itself occupies, from the most specific
        /// level to the least specific.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName(this UnM49Area m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetName(this UnM49Area m49Code, [NotNull] string languageCode)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetName(m49Code, languageCode);
        }

        /// <summary>
        /// Get the level of the UN M49 hierarchy occupied by a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Level of the M49 hierarchy occupied by the code, if found; otherwise <c>null</c>.</returns>
        public static UnM49AreaLevel? GetLevel(this UnM49Area m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetLevel(m49Code);
        }

        /// <summary>
        /// Get the parent area of a <see cref="UnM49Area"/> code in the UN M49 hierarchy.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Parent area for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// The M49 hierarchy is sparse; where the level immediately above a code is not populated, the nearest populated
        /// ancestor is returned.  The code for the world has no parent and returns <c>null</c>.
        /// </remarks>
        public static UnM49Area? GetParent(this UnM49Area m49Code)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            var parentCode = DoGetParentCode(m49Code);
            if (!parentCode.HasValue || !Enum.IsDefined(typeof(UnM49Area), parentCode.Value))
            {
                return null;
            }

            return (UnM49Area)parentCode.Value;
        }

        /// <summary>
        /// Determine whether a <see cref="UnM49Area"/> code falls within another area in the UN M49 hierarchy.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="other">UN M49 code of the area to test containment within.</param>
        /// <returns><c>true</c> if the code falls within <paramref name="other"/>; otherwise <c>false</c>.</returns>
        /// <remarks>An area is not considered to fall within itself.</remarks>
        public static bool IsWithin(this UnM49Area m49Code, UnM49Area other)
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code) || !Enum.IsDefined(other.GetType(), other))
            {
                return false;
            }

            var ancestor = m49Code.GetParent();
            var levelCount = Enum.GetValues(typeof(UnM49AreaLevel)).Length;
            for (var depth = 0; ancestor.HasValue && depth < levelCount; depth++)
            {
                if (ancestor.Value == other)
                {
                    return true;
                }

                ancestor = ancestor.Value.GetParent();
            }

            return false;
        }

        /// <summary>
        /// Get the ISO 3166 Part 1 Alpha-2 country code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>ISO 3166 Part 1 Alpha-2 country code for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Only codes occupying the country or area level of the M49 hierarchy bear a country code; aggregate areas
        /// such as the world, regions, sub-regions and intermediate regions return <c>null</c>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIso3166Part1Alpha2Code(this UnM49AreaByAlpha2CountryCode m49Code)
#else
        [CanBeNull]
        public static string GetIso3166Part1Alpha2Code(this UnM49AreaByAlpha2CountryCode m49Code)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIso3166Part1Alpha2Code(m49Code);
        }

        /// <summary>
        /// Get the ISO 3166 Part 1 Alpha-2 country code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>ISO 3166 Part 1 Alpha-2 country code for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Only codes occupying the country or area level of the M49 hierarchy bear a country code; aggregate areas
        /// such as the world, regions, sub-regions and intermediate regions return <c>null</c>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIso3166Part1Alpha2Code(this UnM49AreaByAlpha3CountryCode m49Code)
#else
        [CanBeNull]
        public static string GetIso3166Part1Alpha2Code(this UnM49AreaByAlpha3CountryCode m49Code)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIso3166Part1Alpha2Code(m49Code);
        }

        /// <summary>
        /// Get the ISO 3166 Part 1 Alpha-2 country code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>ISO 3166 Part 1 Alpha-2 country code for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Only codes occupying the country or area level of the M49 hierarchy bear a country code; aggregate areas
        /// such as the world, regions, sub-regions and intermediate regions return <c>null</c>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIso3166Part1Alpha2Code(this UnM49Area m49Code)
#else
        [CanBeNull]
        public static string GetIso3166Part1Alpha2Code(this UnM49Area m49Code)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIso3166Part1Alpha2Code(m49Code);
        }

        /// <summary>
        /// Get the ISO 3166 Part 1 Alpha-3 country code related to a <see cref="UnM49AreaByAlpha2CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>ISO 3166 Part 1 Alpha-3 country code for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Only codes occupying the country or area level of the M49 hierarchy bear a country code; aggregate areas
        /// such as the world, regions, sub-regions and intermediate regions return <c>null</c>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIso3166Part1Alpha3Code(this UnM49AreaByAlpha2CountryCode m49Code)
#else
        [CanBeNull]
        public static string GetIso3166Part1Alpha3Code(this UnM49AreaByAlpha2CountryCode m49Code)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIso3166Part1Alpha3Code(m49Code);
        }

        /// <summary>
        /// Get the ISO 3166 Part 1 Alpha-3 country code related to a <see cref="UnM49AreaByAlpha3CountryCode"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>ISO 3166 Part 1 Alpha-3 country code for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Only codes occupying the country or area level of the M49 hierarchy bear a country code; aggregate areas
        /// such as the world, regions, sub-regions and intermediate regions return <c>null</c>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIso3166Part1Alpha3Code(this UnM49AreaByAlpha3CountryCode m49Code)
#else
        [CanBeNull]
        public static string GetIso3166Part1Alpha3Code(this UnM49AreaByAlpha3CountryCode m49Code)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIso3166Part1Alpha3Code(m49Code);
        }

        /// <summary>
        /// Get the ISO 3166 Part 1 Alpha-3 country code related to a <see cref="UnM49Area"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>ISO 3166 Part 1 Alpha-3 country code for the M49 code, if found; otherwise <c>null</c>.</returns>
        /// <remarks>
        /// Only codes occupying the country or area level of the M49 hierarchy bear a country code; aggregate areas
        /// such as the world, regions, sub-regions and intermediate regions return <c>null</c>.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIso3166Part1Alpha3Code(this UnM49Area m49Code)
#else
        [CanBeNull]
        public static string GetIso3166Part1Alpha3Code(this UnM49Area m49Code)
#endif
        {
            if (!Enum.IsDefined(m49Code.GetType(), m49Code))
            {
                return null;
            }

            return DoGetIso3166Part1Alpha3Code(m49Code);
        }

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private static UnM49AreaCodeAttribute? DoGetCodeAttribute<T>(T m49Code)
#else
        [CanBeNull]
        private static UnM49AreaCodeAttribute DoGetCodeAttribute<T>(T m49Code)
#endif
            where T : struct, Enum
        {
            var m49CodeName = Enum.GetName(m49Code.GetType(), m49Code);
            return m49Code.GetType().GetTypeInfo().GetDeclaredField(m49CodeName ?? string.Empty)?.GetCustomAttribute<UnM49AreaCodeAttribute>();
        }

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

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetIso3166Part1Alpha2Code<T>(T m49Code)
#else
        [CanBeNull]
        private static string DoGetIso3166Part1Alpha2Code<T>(T m49Code)
#endif
            where T : struct, Enum
        {
            var codeAttribute = DoGetCodeAttribute(m49Code);
            return codeAttribute?.Iso3166Part1Alpha2Code;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetIso3166Part1Alpha3Code<T>(T m49Code)
#else
        [CanBeNull]
        private static string DoGetIso3166Part1Alpha3Code<T>(T m49Code)
#endif
            where T : struct, Enum
        {
            var codeAttribute = DoGetCodeAttribute(m49Code);
            return codeAttribute?.Iso3166Part1Alpha3Code;
        }

        private static UnM49AreaLevel DoGetLevel<T>(T m49Code) where T : struct, Enum
        {
            var codeAttribute = DoGetCodeAttribute(m49Code);
            if (codeAttribute is null)
            {
                return UnM49AreaLevel.Unknown;
            }

            var codeValue = Convert.ToUInt16(m49Code);
            if (codeAttribute.IntermediateRegionCode == codeValue)
            {
                return UnM49AreaLevel.IntermediateRegion;
            }

            if (codeAttribute.SubRegionCode == codeValue)
            {
                return UnM49AreaLevel.SubRegion;
            }

            if (codeAttribute.RegionCode == codeValue)
            {
                return UnM49AreaLevel.Region;
            }

            if (codeAttribute.GlobalCode == codeValue)
            {
                return UnM49AreaLevel.Global;
            }

            // The code is absent from the attribute's codes, so it can only identify a country or area:
            // a country or area is deliberately not represented by a constructor parameter or a property.
            return UnM49AreaLevel.CountryOrArea;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static string? DoGetName<T>(T m49Code, string languageCode)
#else
        [CanBeNull]
        private static string DoGetName<T>(T m49Code, [NotNull] string languageCode)
#endif
            where T : struct, Enum
        {
            return DoGetCountryOrAreaName(m49Code, languageCode)
                   ?? DoGetIntermediateRegionName(m49Code, languageCode)
                   ?? DoGetSubRegionName(m49Code, languageCode)
                   ?? DoGetRegionName(m49Code, languageCode)
                   ?? DoGetGlobalName(m49Code, languageCode);
        }

        private static ushort? DoGetParentCode<T>(T m49Code) where T : struct, Enum
        {
            var codeAttribute = DoGetCodeAttribute(m49Code);
            if (codeAttribute is null)
            {
                return null;
            }

            switch (DoGetLevel(m49Code))
            {
                case UnM49AreaLevel.CountryOrArea:
                    return codeAttribute.IntermediateRegionCode ?? codeAttribute.SubRegionCode ?? codeAttribute.RegionCode ?? codeAttribute.GlobalCode;
                case UnM49AreaLevel.IntermediateRegion:
                    return codeAttribute.SubRegionCode ?? codeAttribute.RegionCode ?? codeAttribute.GlobalCode;
                case UnM49AreaLevel.SubRegion:
                    return codeAttribute.RegionCode ?? codeAttribute.GlobalCode;
                case UnM49AreaLevel.Region:
                    return codeAttribute.GlobalCode;
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