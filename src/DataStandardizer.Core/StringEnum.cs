using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.Core
{
    /// <summary>
    /// Extensions for string enumerations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The members of a string enumeration are its public static fields of its own type. They may be declared on the enumeration's struct, or on a public nested class at any
    /// depth, where every class between the field and the struct is public. Instance fields, non-public fields, fields of generic nested classes, which have no single value, and fields of
    /// compiler-generated types, such as the closure classes of lambdas, aren't members. A member is named by its field name alone, without the names of the classes that enclose it, so no two members may share a name. If two do,
    /// every method that reads the members, including <c>TryParse</c>, throws an <see cref="InvalidOperationException"/>.
    /// </para>
    /// <para>
    /// Methods in this class should not be called from within a string enumeration.  Infinite recursion or stack overflow may result.
    /// </para>
    /// </remarks>
    public static class StringEnum
    {
#if NETCOREAPP3_0_OR_GREATER
        private static MemberCache? _cache;
#else
        private static MemberCache _cache;
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
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is neither of type <paramref name="enumType"/> nor does it have the same underlying type as <paramref name="enumType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName(Type enumType, object value)
#else
        [return: MaybeNull]
        public static string GetName([DisallowNull] Type enumType, [DisallowNull] object value)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            ThrowIfNotStringEnumType(enumType);
            if (value.GetType() != enumType)
                throw new ArgumentException($"{nameof(value)} is not of type {nameof(enumType)}.", nameof(value));

            lock (SyncRoot)
            {
                return GetCache(enumType).FieldsByValue[value].FirstOrDefault();
            }
        }

        /// <summary>
        /// Retrieves the name of the constant in a string enumeration that has the specified value.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">The value of a particular enumerated constant in terms of its underlying type.</param>
        /// <returns>A string containing the name of the enumerated constant in <typeparamref name="TEnum"/> whose value is <paramref name="value"/>, or a <c>null</c> if no such constant is found.</returns>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName<TEnum>(TEnum value) where TEnum : struct, IStringEnum
#else
        [return: MaybeNull]
        public static string GetName<TEnum>(TEnum value) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                return GetCache(typeof(TEnum)).FieldsByValue[value].FirstOrDefault();
            }
        }

        /// <summary>
        /// Retrieves an array of the names of the constants in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <returns>A string array of the names of the constants in <paramref name="enumType"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string[] GetNames(Type enumType)
#else
        [return: NotNull]
        public static string[] GetNames([DisallowNull] Type enumType)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            lock (SyncRoot)
            {
                return GetCache(enumType).Members.Select(member => member.Name).ToArray();
            }
        }

        /// <summary>
        /// Retrieves an array of the names of the constants in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <returns>A string array of the names of the constants in <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static string[] GetNames<TEnum>() where TEnum : struct, IStringEnum
#else
        [return: NotNull]
        public static string[] GetNames<TEnum>() where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                return GetCache(typeof(TEnum)).Members.Select(member => member.Name).ToArray();
            }
        }

        /// <summary>
        /// Retrieves an array of the values of the constants in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <returns>An array that contains the values of the constants in <paramref name="enumType"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static object[] GetValues(Type enumType)
#else
        [return: NotNull]
        public static object[] GetValues([DisallowNull] Type enumType)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            lock (SyncRoot)
            {
                return GetCache(enumType).FieldsByValue.Select(grp => grp.Key).ToArray();
            }
        }

        /// <summary>
        /// Retrieves an array of the values of the constants in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <returns>An array that contains the values of the constants in <typeparamref name="TEnum"/>.</returns>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum[] GetValues<TEnum>() where TEnum : struct, IStringEnum
#else
        [return: NotNull]
        public static TEnum[] GetValues<TEnum>() where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                return GetCache(typeof(TEnum)).FieldsByValue.Select(grp => grp.Key).Cast<TEnum>().ToArray();
            }
        }

        /// <summary>
        /// Returns an indication whether a constant with a specified value exists in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The value or name of a constant in <paramref name="enumType"/>.</param>
        /// <returns><c>true</c> if a constant in <paramref name="enumType"/> has a value equal to <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsDefined(Type enumType, string value)
#else
        public static bool IsDefined([DisallowNull] Type enumType, [DisallowNull] string value)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            ThrowIfNotStringEnumType(enumType);

            lock (SyncRoot)
            {
                return FindMemberByName(enumType, value, false) != null || FindMemberByValue(enumType, value, false) != null;
            }
        }

        /// <summary>
        /// Returns an indication whether a constant with a specified value exists in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">The value or name of a constant in <typeparamref name="TEnum"/>.</param>
        /// <returns><c>true</c> if a constant in <typeparamref name="TEnum"/> has a value equal to <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsDefined<TEnum>(string value) where TEnum : struct, IStringEnum
#else
        public static bool IsDefined<TEnum>([DisallowNull] string value) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            lock (SyncRoot)
            {
                return FindMemberByName(typeof(TEnum), value, false) != null || FindMemberByValue(typeof(TEnum), value, false) != null;
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
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static object Parse(Type enumType, string value)
#else
        [return: NotNull]
        public static object Parse([DisallowNull] Type enumType, [DisallowNull] string value)
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string or only contains white space.", nameof(value));

            lock (SyncRoot)
            {
                var memberValueByNames = FindMemberByName(enumType, value, false);
                if (memberValueByNames != null)
                {
                    return memberValueByNames;
                }

                var memberValueByValues = FindMemberByValue(enumType, value, false);
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
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches
        /// more than one name, or more than one value, ignoring case, the member whose name or value matches it exactly (ordinal comparison) wins. If none matches exactly, the first
        /// matching member is returned, in the order in which <see cref="GetNames(Type)"/> returns the members' names.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static object Parse(Type enumType, string value, bool ignoreCase)
#else
        [return: NotNull]
        public static object Parse([DisallowNull] Type enumType, [DisallowNull] string value, bool ignoreCase)
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string (\"\") or only contains white space.", nameof(value));

            lock (SyncRoot)
            {
                var memberValueByNames = FindMemberByName(enumType, value, ignoreCase);
                if (memberValueByNames != null)
                {
                    return memberValueByNames;
                }

                var memberValueByValues = FindMemberByValue(enumType, value, ignoreCase);
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
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum Parse<TEnum>(string value) where TEnum : struct, IStringEnum
#else
        public static TEnum Parse<TEnum>([DisallowNull] string value) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string or only contains white space.", nameof(value));

            lock (SyncRoot)
            {
                if (FindMemberByName(typeof(TEnum), value, false) is TEnum resultByName)
                {
                    return resultByName;
                }

                if (FindMemberByValue(typeof(TEnum), value, false) is TEnum resultByValue)
                {
                    return resultByValue;
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
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches
        /// more than one name, or more than one value, ignoring case, the member whose name or value matches it exactly (ordinal comparison) wins. If none matches exactly, the first
        /// matching member is returned, in the order in which <see cref="GetNames(Type)"/> returns the members' names.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct, IStringEnum
#else
        public static TEnum Parse<TEnum>([DisallowNull] string value, bool ignoreCase) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string or only contains white space.", nameof(value));

            lock (SyncRoot)
            {
                if (FindMemberByName(typeof(TEnum), value, ignoreCase) is TEnum resultByName)
                {
                    return resultByName;
                }

                if (FindMemberByValue(typeof(TEnum), value, ignoreCase) is TEnum resultByValue)
                {
                    return resultByValue;
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
        /// <exception cref="InvalidOperationException"><paramref name="enumType"/> defines no conversion from <see cref="string"/>.</exception>
        /// <exception cref="InvalidCastException">The enumeration's conversion from <see cref="string"/> rejects <paramref name="value"/>.</exception>
        /// <remarks>Any other exception that the enumeration's conversion from <see cref="string"/> throws is passed on to the caller unchanged.</remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static object ToObject(Type enumType, string value)
#else
        [return: NotNull]
        public static object ToObject([DisallowNull] Type enumType, [DisallowNull] string value)
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            return Cast(enumType, value);
        }

        /// <summary>
        /// Returns an instance of the specified enumeration set to the specified value.
        /// </summary>
        /// <typeparam name="TEnum">An enumeration.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>An enumeration object whose value is <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException"><typeparamref name="TEnum"/> defines no conversion from <see cref="string"/>.</exception>
        /// <exception cref="InvalidCastException">The enumeration's conversion from <see cref="string"/> rejects <paramref name="value"/>.</exception>
        /// <remarks>Any other exception that the enumeration's conversion from <see cref="string"/> throws is passed on to the caller unchanged.</remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum ToObject<TEnum>(string value) where TEnum : struct, IStringEnum
#else
        public static TEnum ToObject<TEnum>([DisallowNull] string value) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return (TEnum)Cast(typeof(TEnum), value);
        }

        /// <summary>
        /// Converts the string representation of the name or value of one or more enumerated constants to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">The enum type to use for parsing.</param>
        /// <param name="value">The string representation of the name or value of one or more enumerated constants.</param>
        /// <param name="result">When this method returns <c>true</c>, contains an enumeration constant that represents the parsed value.</param>
        /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c> and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(Type enumType, string? value, out object? result)
#else
        public static bool TryParse([DisallowNull] Type enumType, [AllowNull] string value, [MaybeNullWhen(false)] out object result)
#endif
        {
            // Like Enum.TryParse, a null value fails before enumType is checked.
            if (value is null)
            {
                result = null;
                return false;
            }

            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            lock (SyncRoot)
            {
                var memberValueByNames = FindMemberByName(enumType, value, false);
                if (memberValueByNames != null)
                {
                    result = memberValueByNames;
                    return true;
                }

                var memberValueByValues = FindMemberByValue(enumType, value, false);
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
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c> and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches
        /// more than one name, or more than one value, ignoring case, the member whose name or value matches it exactly (ordinal comparison) wins. If none matches exactly, the first
        /// matching member is returned, in the order in which <see cref="GetNames(Type)"/> returns the members' names.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(Type enumType, string? value, bool ignoreCase, out object? result)
#else
        public static bool TryParse([DisallowNull] Type enumType, [AllowNull] string value, bool ignoreCase, [MaybeNullWhen(false)] out object result)
#endif
        {
            // Like Enum.TryParse, a null value fails before enumType is checked.
            if (value is null)
            {
                result = null;
                return false;
            }

            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            lock (SyncRoot)
            {
                var memberValueByNames = FindMemberByName(enumType, value, ignoreCase);
                if (memberValueByNames != null)
                {
                    result = memberValueByNames;
                    return true;
                }

                var memberValueByValues = FindMemberByValue(enumType, value, ignoreCase);
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
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([AllowNull] string value, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                if (value != null && FindMemberByName(typeof(TEnum), value, false) is TEnum resultByName)
                {
                    result = resultByName;
                    return true;
                }

                if (FindMemberByValue(typeof(TEnum), value, false) is TEnum resultByValue)
                {
                    result = resultByValue;
                    return true;
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
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches
        /// more than one name, or more than one value, ignoring case, the member whose name or value matches it exactly (ordinal comparison) wins. If none matches exactly, the first
        /// matching member is returned, in the order in which <see cref="GetNames(Type)"/> returns the members' names.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([AllowNull] string value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                if (value != null && FindMemberByName(typeof(TEnum), value, ignoreCase) is TEnum resultByName)
                {
                    result = resultByName;
                    return true;
                }

                if (FindMemberByValue(typeof(TEnum), value, ignoreCase) is TEnum resultByValue)
                {
                    result = resultByValue;
                    return true;
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
        [return: MaybeNull]
        private static string Cast([AllowNull] object value)
#endif
        {
            if (value is null) return null;
            var p = Expression.Parameter(typeof(object));
            var c1 = Expression.Convert(p, value.GetType());
            var c2 = Expression.Convert(c1, typeof(string));
            var e = (Func<object, string>)Expression.Lambda(c2, p).Compile();
            return e(value);
        }

        // Builds the conversion for the type itself rather than going through reflection, so whatever the conversion throws reaches the caller unwrapped.
        private static object Cast(Type enumType, string value)
        {
            var p = Expression.Parameter(typeof(string));
            var c1 = Expression.Convert(p, enumType);
            var c2 = Expression.Convert(c1, typeof(object));
            var e = Expression.Lambda<Func<string, object>>(c2, p).Compile();
            return e(value);
        }

        // An exact (ordinal) match wins. Otherwise, when ignoring case, the first member in member order whose name matches ignoring case.
#if NETCOREAPP3_0_OR_GREATER
        private static object? FindMemberByName(Type type, string value, bool ignoreCase)
#else
        [return: MaybeNull]
        private static object FindMemberByName(Type type, string value, bool ignoreCase)
#endif
        {
            var cache = GetCache(type);
            if (cache.FieldsByName.TryGetValue(value, out var exactMatch))
                return exactMatch;
            if (!ignoreCase)
                return null;

            foreach (var member in cache.Members)
            {
                if (string.Equals(member.Name, value, StringComparison.OrdinalIgnoreCase))
                    return member.Value;
            }

            return null;
        }

        // An exact (ordinal) match wins. Otherwise, when ignoring case, the first member in member order whose value matches ignoring case. A null value matches nothing.
#if NETCOREAPP3_0_OR_GREATER
        private static object? FindMemberByValue(Type type, string? value, bool ignoreCase)
#else
        [return: MaybeNull]
        private static object FindMemberByValue(Type type, [AllowNull] string value, bool ignoreCase)
#endif
        {
            if (value is null)
                return null;

#if NETCOREAPP3_0_OR_GREATER
            object? caseInsensitiveMatch = null;
#else
            object caseInsensitiveMatch = null;
#endif
            foreach (var member in GetCache(type).Members)
            {
                if (string.Equals(member.StringValue, value, StringComparison.Ordinal))
                    return member.Value;
                if (ignoreCase && caseInsensitiveMatch is null && string.Equals(member.StringValue, value, StringComparison.OrdinalIgnoreCase))
                    caseInsensitiveMatch = member.Value;
            }

            return caseInsensitiveMatch;
        }

        // Callers hold SyncRoot.
        private static MemberCache GetCache(Type type)
        {
            var cache = _cache;
            if (cache is null || cache.Type != type)
            {
                cache = BuildCache(type);
                _cache = cache;
            }

            return cache;
        }

        private static List<FieldInfo> GetMemberFields(Type enumType)
        {
            var memberFields = new List<FieldInfo>();
            AddMemberFields(enumType.GetTypeInfo(), enumType, memberFields);
            return memberFields;
        }

        private static void AddMemberFields(TypeInfo declaringType, Type enumType, List<FieldInfo> memberFields)
        {
            memberFields.AddRange(declaringType.DeclaredFields.Where(field => field.IsPublic && field.IsStatic && field.FieldType == enumType));

            var nestedTypes = declaringType.DeclaredNestedTypes
                .Where(nestedType => nestedType.IsClass && nestedType.IsNestedPublic && !nestedType.ContainsGenericParameters && !nestedType.IsDefined(typeof(CompilerGeneratedAttribute), false));
            foreach (var nestedType in nestedTypes)
            {
                AddMemberFields(nestedType, enumType, memberFields);
            }
        }

        private static bool IsStringEnumType(Type candidateType)
        {
            var candidateTypeInfo = candidateType.GetTypeInfo();
            return candidateTypeInfo.IsValueType && !candidateTypeInfo.ContainsGenericParameters && typeof(IStringEnum).GetTypeInfo().IsAssignableFrom(candidateTypeInfo);
        }

        private static void ThrowIfNotStringEnumType(Type enumType)
        {
            if (!IsStringEnumType(enumType))
                throw new ArgumentException($"{nameof(enumType)} is not a string enumeration.", nameof(enumType));
        }

        // Builds the whole cache before it's published, so that a failure leaves the previous cache in place.
        private static MemberCache BuildCache(Type type)
        {
            var fieldsByName = new Dictionary<string, object>(StringComparer.Ordinal);
            var members = new List<Member>();
            foreach (var field in GetMemberFields(type))
            {
                var fieldValue = field.GetValue(null);
                if (fieldValue is null)
                    continue;
                if (fieldsByName.ContainsKey(field.Name))
                    throw new InvalidOperationException($"The string enumeration {type.FullName} has more than one member named {field.Name}.");

                fieldsByName.Add(field.Name, fieldValue);
                members.Add(new Member(field.Name, fieldValue, Cast(fieldValue)));
            }

            var fieldsByValue = members.ToLookup(member => member.Value, member => member.Name);
            return new MemberCache(type, fieldsByName, members, fieldsByValue);
        }

        #endregion

        #region Nested Types

        private sealed class MemberCache
        {
            public MemberCache(Type type, Dictionary<string, object> fieldsByName, List<Member> members, ILookup<object, string> fieldsByValue)
            {
                Type = type;
                FieldsByName = fieldsByName;
                Members = members;
                FieldsByValue = fieldsByValue;
            }

            public Type Type { get; }

            public Dictionary<string, object> FieldsByName { get; }

            // The members in the order GetNames returns them.
            public List<Member> Members { get; }

            public ILookup<object, string> FieldsByValue { get; }
        }

        private readonly struct Member
        {
#if NETCOREAPP3_0_OR_GREATER
            public Member(string name, object value, string? stringValue)
#else
            public Member(string name, object value, string stringValue)
#endif
            {
                Name = name;
                Value = value;
                StringValue = stringValue;
            }

            public string Name { get; }

            public object Value { get; }

#if NETCOREAPP3_0_OR_GREATER
            public string? StringValue { get; }
#else
            public string StringValue { get; }
#endif
        }

        #endregion
    }
}