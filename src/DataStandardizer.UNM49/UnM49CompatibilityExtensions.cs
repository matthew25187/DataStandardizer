#if NETSTANDARD
using JetBrains.Annotations; 
#endif

#pragma warning disable CS0618

namespace DataStandardizer.UNM49
{
    public static class UnM49CompatibilityExtensions
    {
        /// <summary>
        /// Get the global code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Global code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetGlobalCode(this UnM49ByAlpha2Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetGlobalCode();
        }

        /// <summary>
        /// Get the global code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Global code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetGlobalCode(this UnM49ByAlpha3Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetGlobalCode();
        }

        /// <summary>
        /// Get the name for the global code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Global name for the <see cref="UnM49ByAlpha2Code"/> code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetGlobalName(this UnM49ByAlpha2Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetGlobalName(this UnM49ByAlpha2Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetGlobalName(languageCode);
        }

        /// <summary>
        /// Get the name for the global code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Global name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetGlobalName(this UnM49ByAlpha3Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetGlobalName(this UnM49ByAlpha3Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetGlobalName(languageCode);
        }

        /// <summary>
        /// Get the region code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetRegionCode(this UnM49ByAlpha2Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetRegionCode();
        }

        /// <summary>
        /// Get the region code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetRegionCode(this UnM49ByAlpha3Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetRegionCode();
        }

        /// <summary>
        /// Get the name for the region code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetRegionName(this UnM49ByAlpha2Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetRegionName(this UnM49ByAlpha2Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetRegionName(languageCode);
        }

        /// <summary>
        /// Get the name for the region code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetRegionName(this UnM49ByAlpha3Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetRegionName(this UnM49ByAlpha3Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetRegionName(languageCode);
        }

        /// <summary>
        /// Get the sub-region code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Sub-region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetSubRegionCode(this UnM49ByAlpha2Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetSubRegionCode();
        }

        /// <summary>
        /// Get the sub-region code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Sub-region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetSubRegionCode(this UnM49ByAlpha3Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetSubRegionCode();
        }

        /// <summary>
        /// Get the name for the sub-region code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Sub-region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubRegionName(this UnM49ByAlpha2Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetSubRegionName(this UnM49ByAlpha2Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetSubRegionName(languageCode);
        }

        /// <summary>
        /// Get the name for the sub-region code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Sub-region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetSubRegionName(this UnM49ByAlpha3Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetSubRegionName(this UnM49ByAlpha3Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetSubRegionName(languageCode);
        }

        /// <summary>
        /// Get the intermediate region code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Intermediate region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetIntermediateRegionCode(this UnM49ByAlpha2Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetIntermediateRegionCode();
        }

        /// <summary>
        /// Get the intermediate region code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <returns>Intermediate region code for the M49 code, if found; otherwise <c>null</c>.</returns>
        public static ushort? GetIntermediateRegionCode(this UnM49ByAlpha3Code m49Code)
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetIntermediateRegionCode();
        }

        /// <summary>
        /// Get the name for the intermediate region code related to a <see cref="UnM49ByAlpha2Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Intermediate region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIntermediateRegionName(this UnM49ByAlpha2Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetIntermediateRegionName(this UnM49ByAlpha2Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetIntermediateRegionName(languageCode);
        }

        /// <summary>
        /// Get the name for the intermediate region code related to a <see cref="UnM49ByAlpha3Code"/> code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Intermediate region name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetIntermediateRegionName(this UnM49ByAlpha3Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetIntermediateRegionName(this UnM49ByAlpha3Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetIntermediateRegionName(languageCode);
        }

        /// <summary>
        /// Gets the name for the M49 code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Country or area name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCountryOrAreaName(this UnM49ByAlpha2Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetCountryOrAreaName(this UnM49ByAlpha2Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha2CountryCode)m49Code;
            return newM49Code.GetCountryOrAreaName(languageCode);
        }

        /// <summary>
        /// Gets the name for the M49 code.
        /// </summary>
        /// <param name="m49Code">UN M49 code.</param>
        /// <param name="languageCode">ISO 639 language code for the language of the name.</param>
        /// <returns>Country or area name for the M49 code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetCountryOrAreaName(this UnM49ByAlpha3Code m49Code, string languageCode)
#else
        [CanBeNull]
        public static string GetCountryOrAreaName(this UnM49ByAlpha3Code m49Code, string languageCode)
#endif
        {
            var newM49Code = (UnM49AreaByAlpha3CountryCode)m49Code;
            return newM49Code.GetCountryOrAreaName(languageCode);
        }
    }
}