using System;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Language
{
    public static class Iso15924Extensions
    {
        /// <summary>
        /// Get the English name of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having the name to retrieve.</param>
        /// <returns>English name of the script code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetEnglishName(this Iso15924Script scriptCode)
#else
        [CanBeNull]
        public static string GetEnglishName(this Iso15924Script scriptCode)
#endif
        {
            if (!Enum.IsDefined(scriptCode.GetType(), scriptCode))
            {
                return null;
            }

#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(scriptCode.GetType(), scriptCode)!;
#else
            var codeName = Enum.GetName(scriptCode.GetType(), scriptCode);
#endif
            var codeAttribute = scriptCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso15924ScriptCodeAttribute>();
            return codeAttribute?.EnglishName;
        }

        /// <summary>
        /// Get the French name of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having the name to retrieve.</param>
        /// <returns>French name of the script code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetFrenchName(this Iso15924Script scriptCode)
#else
        [CanBeNull]
        public static string GetFrenchName(this Iso15924Script scriptCode)
#endif
        {
            if (!Enum.IsDefined(scriptCode.GetType(), scriptCode))
            {
                return null;
            }

#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(scriptCode.GetType(), scriptCode)!;
#else
            var codeName = Enum.GetName(scriptCode.GetType(), scriptCode);
#endif
            var codeAttribute = scriptCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso15924ScriptCodeAttribute>();
            return codeAttribute?.FrenchName;
        }

        /// <summary>
        /// Get the alias of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having the alias to retrieve.</param>
        /// <returns>Alias of the script code, if found; otherwise <c>null</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetAlias(this Iso15924Script scriptCode)
#else
        [CanBeNull]
        public static string GetAlias(this Iso15924Script scriptCode)
#endif
        {
            if (!Enum.IsDefined(scriptCode.GetType(), scriptCode))
            {
                return null;
            }

#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(scriptCode.GetType(), scriptCode)!;
#else
            var codeName = Enum.GetName(scriptCode.GetType(), scriptCode);
#endif
            var codeAttribute = scriptCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso15924ScriptCodeAttribute>();
            return codeAttribute?.Alias;
        }

        /// <summary>
        /// Get the age of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having an age to retrieve.</param>
        /// <returns>Age of the script code, if found; otherwise <c>null</c>.</returns>
        public static double? GetAge(this Iso15924Script scriptCode)
        {
            if (!Enum.IsDefined(scriptCode.GetType(), scriptCode))
            {
                return null;
            }

#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(scriptCode.GetType(), scriptCode)!;
#else
            var codeName = Enum.GetName(scriptCode.GetType(), scriptCode);
#endif
            var codeAttribute = scriptCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso15924ScriptCodeAttribute>();
            return codeAttribute?.Age;
        }

        /// <summary>
        /// Get the date of a script code.
        /// </summary>
        /// <param name="scriptCode">Script code having a date to retrieve.</param>
        /// <returns>Date of the script code, if found; otherwise <c>null</c>.</returns>
#if NET6_0_OR_GREATER
        public static DateOnly? GetDate(this Iso15924Script scriptCode)
#else
        public static DateTime? GetDate(this Iso15924Script scriptCode)
#endif
        {
            if (!Enum.IsDefined(scriptCode.GetType(), scriptCode))
            {
                return null;
            }

#if NETCOREAPP3_0_OR_GREATER
            string codeName = Enum.GetName(scriptCode.GetType(), scriptCode)!;
#else
            var codeName = Enum.GetName(scriptCode.GetType(), scriptCode);
#endif
            var codeAttribute = scriptCode.GetType().GetTypeInfo().GetDeclaredField(codeName)?.GetCustomAttribute<Iso15924ScriptCodeAttribute>();
            return codeAttribute?.Date;
        }
    }
}