using System;
using System.Linq;
using System.Reflection;

namespace DataStandardizer.Geography
{
    public static class Iso3166Part2Enum
    {
        private static readonly ILookup<Type, FieldInfo> SubdivisionCodeLookup;

        static Iso3166Part2Enum()
        {
            SubdivisionCodeLookup = typeof(Iso3166Part2Subdivision).GetTypeInfo().DeclaredNestedTypes
                .SelectMany(typeInfo => typeInfo.DeclaredFields, (typeInfo, fieldInfo) => new { CountryType = typeInfo.AsType(), Field = fieldInfo })
                .Where(kvp => kvp.Field.FieldType == typeof(Iso3166Part2Subdivision))
                .ToLookup(kvp => kvp.CountryType, kvp => kvp.Field);
        }

        /// <summary>
        /// Retrieves the names of ISO 3166-2 subdivisions for the specified country.
        /// </summary>
        /// <param name="country">The ISO 3166-1 alpha-2 country code for which to retrieve subdivision names.</param>
        /// <returns>
        /// An array of strings representing the names of the subdivisions for the specified country.
        /// If the country code is invalid or not found, an empty array is returned.
        /// </returns>
        public static string[] GetNames(Iso3166Part1Alpha2Country country)
        {
            var countryCode = Enum.GetName(typeof(Iso3166Part1Alpha2Country), country);
            if (countryCode is null)
            {
#if NET8_0_OR_GREATER
                return [];
#elif NETSTANDARD1_3_OR_GREATER||NET
                return Array.Empty<string>();
#else
                return new string[] { };
#endif
            }

            var countryType = typeof(Iso3166Part2Subdivision).GetTypeInfo().GetDeclaredNestedType(countryCode)?.AsType();
            return countryType != null
                ? SubdivisionCodeLookup[countryType].Select(field => field.Name).ToArray()
#if NET8_0_OR_GREATER
                : [];
#elif NETSTANDARD1_3_OR_GREATER||NET
                : Array.Empty<string>();
#else
                : new string[] { };
#endif
        }

        /// <summary>
        /// Retrieves the names of ISO 3166-2 subdivisions for a specified country.
        /// </summary>
        /// <param name="country">
        /// The ISO 3166-1 alpha-3 country code for which to retrieve subdivision names.
        /// </param>
        /// <returns>
        /// An array of subdivision names corresponding to the specified country. 
        /// If the country code is invalid or not found, an empty array is returned.
        /// </returns>
        public static string[] GetNames(Iso3166Part1Alpha3Country country)
        {
            var countryCode = Enum.GetName(typeof(Iso3166Part1Alpha2Country), (ushort)country);
            if (countryCode is null)
            {
#if NET8_0_OR_GREATER
                return [];
#elif NETSTANDARD1_3_OR_GREATER||NET
                return Array.Empty<string>();
#else
                return new string[] { };
#endif
            }

            var countryType = typeof(Iso3166Part2Subdivision).GetTypeInfo().GetDeclaredNestedType(countryCode)?.AsType();
            return countryType != null
                ? SubdivisionCodeLookup[countryType].Select(field => field.Name).ToArray()
#if NET8_0_OR_GREATER
                : [];
#elif NETSTANDARD1_3_OR_GREATER||NET
                : Array.Empty<string>();
#else
                : new string[] { };
#endif
        }

        /// <summary>
        /// Retrieves an array of ISO 3166-2 subdivisions for the specified country.
        /// </summary>
        /// <param name="country">
        /// The ISO 3166-1 alpha-2 country code for which to retrieve the subdivisions.
        /// </param>
        /// <returns>
        /// An array of <see cref="Iso3166Part2Subdivision"/> instances representing the subdivisions
        /// of the specified country. Returns an empty array if the country code is invalid or has no subdivisions.
        /// </returns>
        public static Iso3166Part2Subdivision[] GetValues(Iso3166Part1Alpha2Country country)
        {
            var countryCode = Enum.GetName(typeof(Iso3166Part1Alpha2Country), country);
            if (countryCode is null)
            {
#if NET8_0_OR_GREATER
                return [];
#elif NETSTANDARD1_3_OR_GREATER||NET
                return Array.Empty<Iso3166Part2Subdivision>();
#else
                return new Iso3166Part2Subdivision[] { };
#endif
            }

            var countryType = typeof(Iso3166Part2Subdivision).GetTypeInfo().GetDeclaredNestedType(countryCode)?.AsType();
            return countryType != null
                ? SubdivisionCodeLookup[countryType].Select(field => field.GetValue(null)).Cast<Iso3166Part2Subdivision>().ToArray()
#if NET8_0_OR_GREATER
                : [];
#elif NETSTANDARD1_3_OR_GREATER || NET
                : Array.Empty<Iso3166Part2Subdivision>();
#else
                : new Iso3166Part2Subdivision[] { };
#endif
        }

        /// <summary>
        /// Retrieves an array of ISO 3166-2 subdivisions for the specified ISO 3166-1 alpha-3 country.
        /// </summary>
        /// <param name="country">
        /// The ISO 3166-1 alpha-3 country for which to retrieve the subdivisions.
        /// </param>
        /// <returns>
        /// An array of <see cref="Iso3166Part2Subdivision"/> representing the subdivisions of the specified country.
        /// If the country code is invalid or no subdivisions are available, an empty array is returned.
        /// </returns>
        public static Iso3166Part2Subdivision[] GetValues(Iso3166Part1Alpha3Country country)
        {
            var countryCode = Enum.GetName(typeof(Iso3166Part1Alpha2Country), (ushort)country);
            if (countryCode is null)
            {
#if NET8_0_OR_GREATER
                return [];
#elif NETSTANDARD1_3_OR_GREATER||NET
                return Array.Empty<Iso3166Part2Subdivision>();
#else
                return new Iso3166Part2Subdivision[] { };
#endif
            }

            var countryType = typeof(Iso3166Part2Subdivision).GetTypeInfo().GetDeclaredNestedType(countryCode)?.AsType();
            return countryType != null
                ? SubdivisionCodeLookup[countryType].Select(field => field.GetValue(null)).Cast<Iso3166Part2Subdivision>().ToArray()
#if NET8_0_OR_GREATER
                : [];
#elif NETSTANDARD1_3_OR_GREATER || NET
                : Array.Empty<Iso3166Part2Subdivision>();
#else
                : new Iso3166Part2Subdivision[] { };
#endif
        }
    }
}