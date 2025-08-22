using System;
using System.Linq;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.Chronology
{
    public static class TzDataExtensions
    {
        /// <summary>
        /// Get the comment on a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>Comment for the timezone, if available; otherwise, <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetComment(this TzDataTimezone timezone)
#else
        [CanBeNull]
        public static string GetComment(this TzDataTimezone timezone)
#endif
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
            return timezoneAttribute?.Comment;
        }

        /// <summary>
        /// Get the ISO country codes for the countries covered by a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>A collection of ISO country codes related to the timezone.</returns>
        public static string[] GetIsoCountryCodes(this TzDataTimezone timezone)
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
#if NET8_0_OR_GREATER
            return timezoneAttribute?.IsoCountryCodes ?? [];
#elif NETSTANDARD1_3_OR_GREATER || NET
            return timezoneAttribute?.IsoCountryCodes ?? Array.Empty<string>();
#else
            return timezoneAttribute?.IsoCountryCodes ?? new string[] { };
#endif
        }

        /// <summary>
        /// Get the latitude of the principal location within a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>Latitude of the timezone.</returns>
        public static double GetLatitude(this TzDataTimezone timezone)
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
            return timezoneAttribute?.Latitude ?? 0;
        }

        /// <summary>
        /// Get the longitude of the principal location within a timezone.
        /// </summary>
        /// <param name="timezone">A TzData timezone.</param>
        /// <returns>Longitude of the timezone.</returns>
        public static double GetLongitude(this TzDataTimezone timezone)
        {
            var timezoneAttribute = GetTimezoneAttribute(timezone);
            return timezoneAttribute?.Longitude ?? 0;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static TzDataTimezoneAttribute? GetTimezoneAttribute(TzDataTimezone timezone)
#else
        [CanBeNull]
        private static TzDataTimezoneAttribute GetTimezoneAttribute(TzDataTimezone timezone)
#endif
        {
            var timezoneFields = GetTimezoneFields(timezone.GetType());
            var timezoneField = timezoneFields.FirstOrDefault(field => field.GetValue(null) is TzDataTimezone fieldValue && fieldValue.Equals(timezone));
            return timezoneField?.GetCustomAttribute<TzDataTimezoneAttribute>();
        }

        private static FieldInfo[] GetTimezoneFields(Type hostType)
        {
            var timezoneFields = hostType.GetTypeInfo().DeclaredFields
                .Where(field => field.IsPublic && field.IsStatic && field.FieldType == typeof(TzDataTimezone))
                .ToList();

            var nestedTypes = hostType.GetTypeInfo().DeclaredNestedTypes
                .Where(typeInfo => typeInfo.IsNestedPublic && typeInfo.IsClass && typeInfo.IsAbstract && typeInfo.IsSealed)
                .Select(typeInfo => typeInfo.AsType());
            foreach (var nestedType in nestedTypes)
            {
                var nestedTimezoneFields = GetTimezoneFields(nestedType);
                timezoneFields.AddRange(nestedTimezoneFields);
            }

            return timezoneFields.ToArray();
        }
    }
}