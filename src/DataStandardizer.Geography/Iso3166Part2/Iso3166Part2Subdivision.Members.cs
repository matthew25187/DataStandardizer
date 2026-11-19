using System;
using System.Collections.Generic;
#if !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq;
using System.Reflection;

namespace DataStandardizer.Geography
{
    public readonly partial struct Iso3166Part2Subdivision
    {
        // Built on first use rather than in a static field initializer: each nested country class creates its subdivisions with the
        // struct's constructor, so building it while the struct is first initialized would read fields that aren't yet assigned.
        private static readonly Lazy<Dictionary<string, Iso3166Part2Subdivision>> SubdivisionsByCode =
            new Lazy<Dictionary<string, Iso3166Part2Subdivision>>(CreateSubdivisionsByCode);

#if NETCOREAPP3_0_OR_GREATER
        private static Iso3166Part2Subdivision ConvertFromCode(string? value)
#else
        private static Iso3166Part2Subdivision ConvertFromCode([AllowNull] string value)
#endif
        {
            if (value is null)
                throw new InvalidCastException("A null code cannot be converted to an ISO 3166-2 subdivision.");

            if (!SubdivisionsByCode.Value.TryGetValue(value, out var result))
                throw new InvalidCastException($"'{value}' is not an ISO 3166-2 subdivision code.");

            return result;
        }

        private static Dictionary<string, Iso3166Part2Subdivision> CreateSubdivisionsByCode()
        {
            return typeof(Iso3166Part2Subdivision).GetTypeInfo().DeclaredNestedTypes
                .Where(typeInfo => typeInfo.IsNestedPublic)
                .SelectMany(typeInfo => typeInfo.DeclaredFields)
                .Where(field => field.IsPublic && field.IsStatic && field.FieldType == typeof(Iso3166Part2Subdivision))
#if NETCOREAPP3_0_OR_GREATER
                .Select(field => (Iso3166Part2Subdivision)field.GetValue(null)!)
                .ToDictionary(subdivision => subdivision._value!, StringComparer.Ordinal);
#else
                .Select(field => (Iso3166Part2Subdivision)field.GetValue(null))
                .ToDictionary(subdivision => subdivision._value, StringComparer.Ordinal);
#endif
        }
    }
}
