using System;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Formats type names for messages.
    /// </summary>
    internal static class CsvTypeName
    {
        /// <summary>
        /// Formats a type name the way C# writes it, such as <c>CsvFieldBadValue&lt;OrderLine&gt;</c> rather than <c>CsvFieldBadValue`1[OrderLine]</c>.
        /// </summary>
        internal static string Format(Type type)
        {
            var typeArguments = type.GenericTypeArguments;
            if (typeArguments.Length == 0)
            {
                return type.Name;
            }

            var name = type.Name;
            var arityStart = name.IndexOf('`');
            var formattedArguments = new string[typeArguments.Length];
            for (var index = 0; index < typeArguments.Length; index++)
            {
                formattedArguments[index] = Format(typeArguments[index]);
            }

            return (arityStart < 0 ? name : name.Substring(0, arityStart)) + "<" + string.Join(", ", formattedArguments) + ">";
        }
    }
}
