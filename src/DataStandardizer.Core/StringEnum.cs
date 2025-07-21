using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Core
{
    /// <summary>
    /// Extensions for string enumerations.
    /// </summary>
    /// <remarks>
    /// Methods in this class should not be called from within a string enumeration.  Infinite recursion or stack overflow may result.
    /// </remarks>
    public static class StringEnum
    {
#if NETCOREAPP3_0_OR_GREATER
        private static Dictionary<string, object>? _cacheFieldsByName;
        private static ILookup<object, string>? _cacheFieldsByValue;
        private static Type? _cacheFieldsType;
#else
        private static Dictionary<string, object> _cacheFieldsByName;
        private static ILookup<object, string> _cacheFieldsByValue;
        private static Type _cacheFieldsType;
#endif
        private static readonly object SyncRoot = new object();

        #region Public Methods

        /// <summary>
        /// Retrieves the name of the constant in a string enumeration that has the specified value.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The value of a particular enumerated constant in terms of its underlying type.</param>
        /// <returns>A string containing the name of the enumerated constant in <paramref name="enumType"/> whose value is <paramref name="value"/>, or a <c>null</c> if no such constant is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is a null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enum.
        /// -or-
        /// <paramref name="value"/> is neither of type <paramref name="enumType"/> nor does it have the same underlying type as <paramref name="enumType"/>.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName(Type enumType, object value)
#else
        [CanBeNull]
        public static string GetName([NotNull] Type enumType, [NotNull] object value)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enumeration.", nameof(enumType));
            if (value.GetType() != enumType)
                throw new ArgumentException($"{nameof(value)} is not of type {nameof(enumType)}.", nameof(value));

            lock (SyncRoot)
            {
                var cachedFieldsByValue = GetCachedTypeFieldsByValue(enumType);
                return cachedFieldsByValue[value].FirstOrDefault();
            }
        }

        /// <summary>
        /// Retrieves the name of the constant in a string enumeration that has the specified value.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">The value of a particular enumerated constant in terms of its underlying type.</param>
        /// <returns>A string containing the name of the enumerated constant in <typeparamref name="TEnum"/> whose value is <paramref name="value"/>, or a <c>null</c> if no such constant is found.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName<TEnum>(TEnum value) where TEnum : struct, IStringEnum
#else
        [CanBeNull]
        public static string GetName<TEnum>(TEnum value) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                var cachedFieldsByValue = GetCachedTypeFieldsByValue(typeof(TEnum));
                return cachedFieldsByValue[value].FirstOrDefault();
            }
        }

        /// <summary>
        /// Retrieves an array of the names of the constants in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <returns>A string array of the names of the constants in <paramref name="enumType"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enum.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string[] GetNames(Type enumType)
#else
        [NotNull]
        public static string[] GetNames([NotNull] Type enumType)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enum.", nameof(enumType));

            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(enumType);
                return cachedFieldsByName.Keys.ToArray();
            }
        }

        /// <summary>
        /// Retrieves an array of the names of the constants in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <returns>A string array of the names of the constants in <typeparamref name="TEnum"/>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static string[] GetNames<TEnum>() where TEnum : struct, IStringEnum
#else
        [NotNull]
        public static string[] GetNames<TEnum>() where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(typeof(TEnum));
                return cachedFieldsByName.Keys.ToArray();
            }
        }

        /// <summary>
        /// Retrieves an array of the values of the constants in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <returns>An array that contains the values of the constants in <paramref name="enumType"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enum.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static object[] GetValues(Type enumType)
#else
        [NotNull]
        public static object[] GetValues([NotNull] Type enumType)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enumeration.", nameof(enumType));

            lock (SyncRoot)
            {
                var cachedFieldsByValue = GetCachedTypeFieldsByValue(enumType);
                return cachedFieldsByValue.Select(grp => grp.Key).ToArray();
            }
        }

        /// <summary>
        /// Retrieves an array of the values of the constants in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <returns>An array that contains the values of the constants in <typeparamref name="TEnum"/>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum[] GetValues<TEnum>() where TEnum : struct, IStringEnum
#else
        [NotNull]
        public static TEnum[] GetValues<TEnum>() where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                var cachedFieldsByValue = GetCachedTypeFieldsByValue(typeof(TEnum));
                return cachedFieldsByValue.Select(grp => grp.Key).Cast<TEnum>().ToArray();
            }
        }

        /// <summary>
        /// Returns an indication whether a constant with a specified value exists in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The value or name of a constant in <paramref name="enumType"/>.</param>
        /// <returns><c>true</c> if a constant in <paramref name="enumType"/> has a value equal to <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enum.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsDefined(Type enumType, string value)
#else
        public static bool IsDefined([NotNull] Type enumType, [NotNull] string value)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enumeration.", nameof(enumType));

            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(enumType);
                var cachedFieldsByValue = GetCachedTypeFieldsByValue(enumType);
                return cachedFieldsByName.ContainsKey(value) || cachedFieldsByValue.Select(grp => Cast(grp.Key)).Contains(value);
            }
        }

        /// <summary>
        /// Returns an indication whether a constant with a specified value exists in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">The value or name of a constant in <typeparamref name="TEnum"/>.</param>
        /// <returns><c>true</c> if a constant in <typeparamref name="TEnum"/> has a value equal to <paramref name="value"/>; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsDefined<TEnum>(string value) where TEnum : struct, IStringEnum
#else
        public static bool IsDefined<TEnum>([NotNull] string value) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(typeof(TEnum));
                var cachedFieldsByValue = GetCachedTypeFieldsByValue(typeof(TEnum));
                return cachedFieldsByName.ContainsKey(value) || cachedFieldsByValue.Select(grp => Cast(grp.Key)).Contains(value);
            }
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <returns>An object of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public static object Parse(Type enumType, string value)
#else
        [NotNull]
        public static object Parse([NotNull] Type enumType, [NotNull] string value)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enumeration.", nameof(enumType));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string or only contains white space.", nameof(value));

            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(enumType);
                if (cachedFieldsByName.TryGetValue(value, out var memberValueByNames))
                {
                    return memberValueByNames;
                }

                var cachedFieldsByValue = GetCachedTypeFieldsByValue(enumType);
                var memberValueByValues = cachedFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value)).Select(grp => grp.Key).FirstOrDefault();
                if (memberValueByValues != null)
                {
                    return memberValueByValues;
                }
            }

            throw new ArgumentException($"{nameof(value)} is a name, but not one of the named constants defined for the enumeration.", nameof(value));
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">The <see cref="Type"/> of the enumeration.</param>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <param name="ignoreCase">If <c>true</c>, ignore case; otherwise, regard case.</param>
        /// <returns>An object of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is either an empty string ("") or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public static object Parse(Type enumType, string value, bool ignoreCase)
#else
        [NotNull]
        public static object Parse([NotNull] Type enumType, [NotNull] string value, bool ignoreCase)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enumeration.", nameof(enumType));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string (\"\") or only contains white space.", nameof(value));

            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(enumType);
                if (ignoreCase)
                {
                    var cachedFieldsByCaseInsensitiveName = new Dictionary<string, object>(cachedFieldsByName, StringComparer.OrdinalIgnoreCase);
                    if (cachedFieldsByCaseInsensitiveName.TryGetValue(value, out var memberValueByNames))
                    {
                        return memberValueByNames;
                    }
                }
                else
                {
                    if (cachedFieldsByName.TryGetValue(value, out var memberValueByNames))
                    {
                        return memberValueByNames;
                    }
                }

                var cachedFieldsByValue = GetCachedTypeFieldsByValue(enumType);
                var memberValueByValues = cachedFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value, comparisonType)).Select(grp => grp.Key).FirstOrDefault();
                if (memberValueByValues != null)
                {
                    return memberValueByValues;
                }
            }

            throw new ArgumentException($"{nameof(value)} is a name, but not one of the named constants defined for the enumeration.", nameof(value));
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <returns>An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum Parse<TEnum>(string value) where TEnum : struct, IStringEnum
#else
        public static TEnum Parse<TEnum>([NotNull] string value) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string or only contains white space.", nameof(value));

            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(typeof(TEnum));
                if (cachedFieldsByName.TryGetValue(value, out var memberValueByNames) && memberValueByNames is TEnum)
                {
                    return (TEnum)memberValueByNames;
                }

                var cachedFieldsByValue = GetCachedTypeFieldsByValue(typeof(TEnum));
                var memberValueByValues = cachedFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value)).Select(grp => grp.Key).FirstOrDefault();
                if (memberValueByValues is TEnum)
                {
                    return (TEnum)memberValueByValues;
                }
            }

            throw new ArgumentException($"{nameof(value)} does not contain enumeration information.", nameof(value));
        }

        /// <summary>
        /// Converts the string representation of the name or numeric value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <typeparam name="TEnum">The <see cref="Type"/>> of the enumeration.</typeparam>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <param name="ignoreCase">If <c>true</c>, ignore case; otherwise, regard case.</param>
        /// <returns>An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is either an empty string ("") or only contains white space.
        /// -or-
        /// <paramref name="value"/> is a name, but not one of the named constants defined for the enumeration.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct, IStringEnum
#else
        public static TEnum Parse<TEnum>([NotNull] string value, bool ignoreCase) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string or only contains white space.", nameof(value));

            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            lock (SyncRoot)
            {
                var cachedFieldsByName = GetCachedTypeFieldsByName(typeof(TEnum));
                if (ignoreCase)
                {
                    var cachedFieldsByCaseInsensitiveName = new Dictionary<string, object>(cachedFieldsByName, StringComparer.OrdinalIgnoreCase);
                    if (cachedFieldsByCaseInsensitiveName.TryGetValue(value, out var memberValueByNames) && memberValueByNames is TEnum result)
                    {
                        return result;
                    }
                }
                else
                {
                    if (cachedFieldsByName.TryGetValue(value, out var memberValueByNames) && memberValueByNames is TEnum result)
                    {
                        return result;
                    }
                }

                var cachedFieldsByValue = GetCachedTypeFieldsByValue(typeof(TEnum));
                var memberValueByValues = cachedFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value, comparisonType)).Select(grp => grp.Key).FirstOrDefault();
                {
                    if (memberValueByValues is TEnum result)
                    {
                        return result;
                    }
                }
            }

            throw new ArgumentException($"{nameof(value)} does not contain enumeration information.", nameof(value));
        }

        /// <summary>
        /// Returns an instance of the specified enumeration set to the specified value.
        /// </summary>
        /// <param name="enumType">An enumeration.</param>
        /// <param name="value">The value.</param>
        /// <returns>An enumeration object whose value is <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static object ToObject(Type enumType, string value)
#else
        [NotNull]
        public static object ToObject([NotNull] Type enumType, [NotNull] string value)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enumeration.", nameof(enumType));

            var castMethodDefinition = typeof(StringEnum).GetTypeInfo().DeclaredMethods.SingleOrDefault(method => method.Name == nameof(Cast) && method.IsGenericMethod)?.GetGenericMethodDefinition();
            var castMethod = castMethodDefinition?.MakeGenericMethod(enumType);
            var result = castMethod?.Invoke(null, new object[] { value });

            if (result is null)
            {
                var oneParameterConstructor = enumType.GetTypeInfo().DeclaredConstructors
                    .SingleOrDefault(constructor =>
                    {
                        var parameters = constructor.GetParameters();
                        return parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
                    });
                result = oneParameterConstructor?.Invoke(new object[] { value });
            }

            if (result is null)
            {
                result = Activator.CreateInstance(enumType);
            }

#if NETCOREAPP3_0_OR_GREATER
            return result!;
#else
            return result;
#endif
        }

        /// <summary>
        /// Returns an instance of the specified enumeration set to the specified value.
        /// </summary>
        /// <typeparam name="TEnum">An enumeration.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>An enumeration object whose value is <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum ToObject<TEnum>(string value) where TEnum : struct, IStringEnum
#else
        public static TEnum ToObject<TEnum>([NotNull] string value) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return Cast<TEnum>(value);
        }

        /// <summary>
        /// Converts the string representation of the name or value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The string representation of the name or value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <c>true</c>, contains an enumeration constant that represents the parsed value.</param>
        /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(Type enumType, string? value, out object? result)
#else
        public static bool TryParse([NotNull] Type enumType, [CanBeNull] string value, [CanBeNull] out object result)
#endif
        {
            if (!IsStringEnumType(enumType))
            {
                result = null;
                return false;
            }

            lock (SyncRoot)
            {
                var memberFieldsByName = GetCachedTypeFieldsByName(enumType);
                if (value != null && memberFieldsByName.TryGetValue(value, out var memberValueByNames))
                {
                    result = memberValueByNames;
                    return true;
                }

                var memberFieldsByValue = GetCachedTypeFieldsByValue(enumType);
                var memberValueByValues = memberFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value)).Select(grp => grp.Key).FirstOrDefault();
                if (memberValueByValues != null)
                {
                    result = memberValueByValues;
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Converts the string representation of the name or value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The string representation of the name or numeric value of one or more enumerated constants.</param>
        /// <param name="ignoreCase"><c>true</c> to read <paramref name="value"/> in case-insensitive mode; <c>false</c> to read <paramref name="value"/> in case-sensitive mode.</param>
        /// <param name="result">When this method returns <c>true</c>, contains an enumeration constant that represents the parsed value.</param>
        /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(Type enumType, string? value, bool ignoreCase, out object? result)
#else
        public static bool TryParse([NotNull] Type enumType, [CanBeNull] string value, bool ignoreCase, [CanBeNull] out object result)
#endif
        {
            if (!IsStringEnumType(enumType))
            {
                result = null;
                return false;
            }

            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            lock (SyncRoot)
            {
                var memberFieldsByName = GetCachedTypeFieldsByName(enumType);
                var memberFieldsByCaseInsensitiveName = new Dictionary<string, object>(memberFieldsByName, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                if (value != null && memberFieldsByCaseInsensitiveName.TryGetValue(value, out var memberValueByNames))
                {
                    result = memberValueByNames;
                    return true;
                }

                var memberFieldsByValue = GetCachedTypeFieldsByValue(enumType);
                var memberValueByValues = memberFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value, comparisonType)).Select(grp => grp.Key).FirstOrDefault();
                if (memberValueByValues != null)
                {
                    result = memberValueByValues;
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Converts the string representation of the name or value of one or more enumerated constants to an equivalent enumerated object. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type to which to convert <paramref name="value"/>>.</typeparam>
        /// <param name="value">The string representation of the enumeration name or underlying value to convert.</param>
        /// <param name="result">When this method returns, contains an object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([CanBeNull] string value, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                var memberFieldsByName = GetCachedTypeFieldsByName(typeof(TEnum));
                {
                    if (value != null && memberFieldsByName.TryGetValue(value, out var memberValueByNames) && memberValueByNames is TEnum memberValueResult)
                    {
                        result = memberValueResult;
                        return true;
                    }
                }

                var memberFieldsByValue = GetCachedTypeFieldsByValue(typeof(TEnum));
                var memberValueByValues = memberFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value)).Select(grp => grp.Key).FirstOrDefault();
                {
                    if (memberValueByValues is TEnum memberValueResult)
                    {
                        result = memberValueResult;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Converts the string representation of the name or value of one or more enumerated constants to an equivalent enumerated object. A parameter specifies whether the operation is case-sensitive. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type to which to convert <paramref name="value"/>.</typeparam>
        /// <param name="value">The string representation of the enumeration name or underlying value to convert.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case; <c>false</c> to consider case.</param>
        /// <param name="result">When this method returns, contains an object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([CanBeNull] string value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            var comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            lock (SyncRoot)
            {
                var memberFieldsByName = GetCachedTypeFieldsByName(typeof(TEnum));
                var memberFieldsByCaseInsensitiveName = new Dictionary<string, object>(memberFieldsByName, ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
                {
                    if (value != null && memberFieldsByCaseInsensitiveName.TryGetValue(value, out var memberValueByNames) && memberValueByNames is TEnum memberValueResult)
                    {
                        result = memberValueResult;
                        return true;
                    }
                }

                var memberFieldsByValue = GetCachedTypeFieldsByValue(typeof(TEnum));
                var memberValueByValues = memberFieldsByValue.Where(grp => string.Equals(Cast(grp.Key), value, comparisonType)).Select(grp => grp.Key).FirstOrDefault();
                {
                    if (memberValueByValues is TEnum memberValueResult)
                    {
                        result = memberValueResult;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private static string? Cast(object? value)
#else
        [CanBeNull]
        private static string Cast([CanBeNull] object value)
#endif
        {
            if (value is null) return null;
            var p = Expression.Parameter(typeof(object));
            var c1 = Expression.Convert(p, value.GetType());
            var c2 = Expression.Convert(c1, typeof(string));
            var e = (Func<object, string>)Expression.Lambda(c2, p).Compile();
            return e(value);
        }

#if NETCOREAPP3_0_OR_GREATER
        private static T? Cast<T>(string? value)
#else
        [CanBeNull]
        private static T Cast<T>([CanBeNull] string value)
#endif
        {
            if (value is null) return default(T);
            var p = Expression.Parameter(typeof(string));
            var c1 = Expression.Convert(p, value.GetType());
            var c2 = Expression.Convert(c1, typeof(T));
            var e = (Func<string, T>)Expression.Lambda(c2, p).Compile();
            return e(value);
        }

        private static IDictionary<string, object> GetCachedTypeFieldsByName(Type type)
        {
            if (type != _cacheFieldsType)
            {
                RefreshCachedTypeFields(type);
            }

#if NETCOREAPP3_0_OR_GREATER
            return _cacheFieldsByName!;
#else
            return _cacheFieldsByName;
#endif
        }

        private static ILookup<object, string> GetCachedTypeFieldsByValue(Type type)
        {
            if (type != _cacheFieldsType)
            {
                RefreshCachedTypeFields(type);
            }

#if NETCOREAPP3_0_OR_GREATER
            return _cacheFieldsByValue!;
#else
            return _cacheFieldsByValue;
#endif
        }

        private static FieldInfo[] GetTypeFields(Type type)
        {
            var nestedTypesQuery = type.GetTypeInfo().DeclaredNestedTypes
                .Where(t => t.IsClass)
                .Select(t => t.AsType());
            return type.GetTypeInfo().DeclaredFields
                .Where(field => field.IsPublic && field.IsStatic && field.FieldType.GetTypeInfo().IsValueType)
                .Union(nestedTypesQuery.SelectMany(t => t.GetTypeInfo().DeclaredFields))
                .ToArray();
        }

        private static bool IsStringEnumType(Type candidateType)
        {
            return candidateType.GetTypeInfo().IsValueType && candidateType.GetTypeInfo().ImplementedInterfaces.Any(type => type.Name == nameof(IStringEnum));
        }

        private static void RefreshCachedTypeFields(Type type)
        {
            var enumFields = GetTypeFields(type).Where(field => field.FieldType == type && field.GetValue(null) != null).ToArray();
#if NETCOREAPP3_0_OR_GREATER
            _cacheFieldsByName = enumFields.ToDictionary(field => field.Name, field => field.GetValue(null)!);
            _cacheFieldsByValue = enumFields.ToLookup(field => field.GetValue(null)!, field => field.Name);
#else
            _cacheFieldsByName = enumFields.ToDictionary(field => field.Name, field => field.GetValue(null));
            _cacheFieldsByValue = enumFields.ToLookup(field => field.GetValue(null), field => field.Name);
#endif
            _cacheFieldsType = type;
        }

        #endregion
    }
}