using System;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

#pragma warning disable CS0618

namespace DataStandardizer.ISO15924
{
    public static class Iso15924CompatibilityExtensions
    {
        /// <summary>
        /// Get the English name of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having the name to retrieve.</param>
        /// <returns>English name of the script code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso15924 scriptCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso15924 scriptCode)
#endif
        {
            var newScriptCode = (Iso15924Script)scriptCode;
            return newScriptCode.GetEnglishName();
        }

        /// <summary>
        /// Get the French name of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having the name to retrieve.</param>
        /// <returns>French name of the script code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso15924 scriptCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso15924 scriptCode)
#endif
        {
            var newScriptCode = (Iso15924Script)scriptCode;
            return newScriptCode.GetFrenchName();
        }

        /// <summary>
        /// Get the alias of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having the alias to retrieve.</param>
        /// <returns>Alias of the script code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetAlias(this Iso15924 scriptCode)
#else
        [CanBeNull]
        public static string GetAlias(this Iso15924 scriptCode)
#endif
        {
            var newScriptCode = (Iso15924Script)scriptCode;
            return newScriptCode.GetAlias();
        }

        /// <summary>
        /// Get the age of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having an age to retrieve.</param>
        /// <returns>Age of the script code, if found; otherwise <c>null</c>.</returns>
        public static double? GetAge(this Iso15924 scriptCode)
        {
            var newScriptCode = (Iso15924Script)scriptCode;
            return newScriptCode.GetAge();
        }

        /// <summary>
        /// Get the date of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having a date to retrieve.</param>
        /// <returns>Date of the script code, if found; otherwise <c>null</c>.</returns>
#if NET6_0_OR_GREATER
        public static DateOnly? GetDate(this Iso15924 scriptCode)
#else
        public static DateTime? GetDate(this Iso15924 scriptCode)
#endif
        {
            var newScriptCode = (Iso15924Script)scriptCode;
            return newScriptCode.GetDate();
        }
    }
}