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
#if NETCOREAPP3_0_OR_GREATER
        private static readonly Dictionary<Type, Func<object, string?>?> StringConverters = new Dictionary<Type, Func<object, string?>?>();
#else
        private static readonly Dictionary<Type, Func<object, string>> StringConverters = new Dictionary<Type, Func<object, string>>();
#endif

        #region Public Methods

        /// <summary>
        /// Retrieves the name of the constant in a string enumeration that has the specified value.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">
        /// The value of a particular enumerated constant: a value of <paramref name="enumType"/>, a <see cref="string"/> (its underlying type), or a value of another string
        /// enumeration, whose string value is looked up.
        /// </param>
        /// <returns>A string containing the name of the enumerated constant in <paramref name="enumType"/> whose value is <paramref name="value"/>, or a <c>null</c> if no such constant is found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is a null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is neither a string enumeration value nor a <see cref="string"/>.
        /// -or-
        /// <paramref name="value"/> is a value of another string enumeration that defines no conversion to <see cref="string"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The string enumeration has more than one member with the same name.
        /// -or-
        /// <paramref name="enumType"/> defines no conversion to <see cref="string"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Every <paramref name="value"/> is looked up by its string value (ordinal comparison), and, as <see cref="Enum.GetName(Type, object)"/> does, a value of another enumeration
        /// is looked up by its underlying value. When more than one member has the value, the name of the first of them, in the order in which <see cref="GetNames(Type)"/> returns
        /// the names, is returned.
        /// </para>
        /// <para>Any exception that a string enumeration's conversion to <see cref="string"/> throws is passed on to the caller unchanged.</para>
        /// </remarks>
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

            var valueType = value.GetType();
            var valueString = value as string;
            if (valueString is null && !IsStringEnumType(valueType))
                throw new ArgumentException($"{nameof(value)} is neither a string enumeration value nor a string.", nameof(value));

            lock (SyncRoot)
            {
                var cache = GetCache(enumType);
                if (valueString != null)
                    return cache.GetNameByStringValue(valueString);

                var toString = GetStringConverter(valueType);
                if (toString is null)
                    throw new ArgumentException($"{nameof(value)} is of a string enumeration that defines no conversion to string.", nameof(value));

                return cache.GetNameByStringValue(toString(value));
            }
        }

        /// <summary>
        /// Retrieves the name of the constant in a string enumeration that has the specified value.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">The value of a particular enumerated constant.</param>
        /// <returns>A string containing the name of the enumerated constant in <typeparamref name="TEnum"/> whose value is <paramref name="value"/>, or a <c>null</c> if no such constant is found.</returns>
        /// <exception cref="InvalidOperationException">
        /// The string enumeration has more than one member with the same name.
        /// -or-
        /// <typeparamref name="TEnum"/> defines no conversion to <see cref="string"/>.
        /// </exception>
        /// <remarks>
        /// <paramref name="value"/> is looked up by its string value (ordinal comparison). When more than one member has the value, the name of the first of them, in the order in
        /// which <see cref="GetNames{TEnum}"/> returns the names, is returned.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string? GetName<TEnum>(TEnum value) where TEnum : struct, IStringEnum
#else
        [return: MaybeNull]
        public static string GetName<TEnum>(TEnum value) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                var cache = GetCache(typeof(TEnum));
                return cache.GetNameByStringValue(cache.ToStringValue(value));
            }
        }

        /// <summary>
        /// Retrieves an array of the names of the constants in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <returns>
        /// A string array of the names of the constants in <paramref name="enumType"/>, sorted by their values (ordinal comparison), as <see cref="Enum.GetNames(Type)"/> sorts
        /// them by their binary values. Members with the same value are sorted by name (ordinal comparison).
        /// </returns>
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
        /// <returns>
        /// A string array of the names of the constants in <typeparamref name="TEnum"/>, sorted by their values (ordinal comparison), as <see cref="Enum.GetNames(Type)"/> sorts
        /// them by their binary values. Members with the same value are sorted by name (ordinal comparison).
        /// </returns>
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
        /// <returns>
        /// An array of <paramref name="enumType"/> that contains the value of each constant in <paramref name="enumType"/>, in the order in which <see cref="GetNames(Type)"/> returns
        /// their names. As with <see cref="Enum.GetValues(Type)"/>, a value that more than one constant has appears once for each of them.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
#if NETCOREAPP3_0_OR_GREATER
        public static Array GetValues(Type enumType)
#else
        [return: NotNull]
        public static Array GetValues([DisallowNull] Type enumType)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            lock (SyncRoot)
            {
                var members = GetCache(enumType).Members;
                var values = Array.CreateInstance(enumType, members.Count);
                for (var index = 0; index < members.Count; index++)
                {
                    values.SetValue(members[index].Value, index);
                }

                return values;
            }
        }

        /// <summary>
        /// Retrieves an array of the values of the constants in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <returns>
        /// An array that contains the value of each constant in <typeparamref name="TEnum"/>, in the order in which <see cref="GetNames{TEnum}"/> returns their names. As with
        /// <see cref="Enum.GetValues(Type)"/>, a value that more than one constant has appears once for each of them.
        /// </returns>
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
                return GetCache(typeof(TEnum)).Members.Select(member => (TEnum)member.Value).ToArray();
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
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member whose value
        /// begins or ends with white space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// </remarks>
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
                var member = FindMember(enumType, value, false);
                if (member != null)
                {
                    return member;
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
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then with the white space removed, so a member whose value begins or ends with white
        /// space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches more than one name, or more than one value, ignoring case, the member whose name or value
        /// matches it exactly (ordinal comparison) wins: for values, an exact match on <paramref name="value"/> as given, then an exact match with the white space removed. If none
        /// matches exactly, a match on <paramref name="value"/> as given wins over one with the white space removed, and among those the first matching member is returned, in the order
        /// in which <see cref="GetNames(Type)"/> returns the members' names.
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
                var member = FindMember(enumType, value, ignoreCase);
                if (member != null)
                {
                    return member;
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
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member whose value
        /// begins or ends with white space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// </remarks>
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
                if (FindMember(typeof(TEnum), value, false) is TEnum member)
                {
                    return member;
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
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then with the white space removed, so a member whose value begins or ends with white
        /// space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches more than one name, or more than one value, ignoring case, the member whose name or value
        /// matches it exactly (ordinal comparison) wins: for values, an exact match on <paramref name="value"/> as given, then an exact match with the white space removed. If none
        /// matches exactly, a match on <paramref name="value"/> as given wins over one with the white space removed, and among those the first matching member is returned, in the order
        /// in which <see cref="GetNames(Type)"/> returns the members' names.
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
                if (FindMember(typeof(TEnum), value, ignoreCase) is TEnum member)
                {
                    return member;
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
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member whose value
        /// begins or ends with white space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// </remarks>
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
                var member = FindMember(enumType, value, false);
                if (member != null)
                {
                    result = member;
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
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then with the white space removed, so a member whose value begins or ends with white
        /// space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches more than one name, or more than one value, ignoring case, the member whose name or value
        /// matches it exactly (ordinal comparison) wins: for values, an exact match on <paramref name="value"/> as given, then an exact match with the white space removed. If none
        /// matches exactly, a match on <paramref name="value"/> as given wins over one with the white space removed, and among those the first matching member is returned, in the order
        /// in which <see cref="GetNames(Type)"/> returns the members' names.
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
                var member = FindMember(enumType, value, ignoreCase);
                if (member != null)
                {
                    result = member;
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
        /// <remarks>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member whose value
        /// begins or ends with white space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([AllowNull] string value, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                if (value != null && FindMember(typeof(TEnum), value, false) is TEnum member)
                {
                    result = member;
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
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is compared
        /// with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then with the white space removed, so a member whose value begins or ends with white
        /// space can still be parsed. A <paramref name="value"/> that is only white space is never compared as an empty string.
        /// When <paramref name="ignoreCase"/> is <c>true</c> and <paramref name="value"/> matches more than one name, or more than one value, ignoring case, the member whose name or value
        /// matches it exactly (ordinal comparison) wins: for values, an exact match on <paramref name="value"/> as given, then an exact match with the white space removed. If none
        /// matches exactly, a match on <paramref name="value"/> as given wins over one with the white space removed, and among those the first matching member is returned, in the order
        /// in which <see cref="GetNames(Type)"/> returns the members' names.
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([AllowNull] string value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            lock (SyncRoot)
            {
                if (value != null && FindMember(typeof(TEnum), value, ignoreCase) is TEnum member)
                {
                    result = member;
                    return true;
                }
            }

            result = default;
            return false;
        }

        #endregion

        #region Private Methods

        // The compiled conversion of a value of a string enumeration type to string, or null when the type defines no such conversion. Each type's conversion is compiled once,
        // and a type with no conversion is remembered as such. Callers hold SyncRoot.
#if NETCOREAPP3_0_OR_GREATER
        private static Func<object, string?>? GetStringConverter(Type type)
#else
        [return: MaybeNull]
        private static Func<object, string> GetStringConverter(Type type)
#endif
        {
            if (StringConverters.TryGetValue(type, out var converter))
                return converter;

            var p = Expression.Parameter(typeof(object));
            UnaryExpression body;
            try
            {
                body = Expression.Convert(Expression.Convert(p, type), typeof(string));
            }
            catch (InvalidOperationException)
            {
                // Expression.Convert throws when the type defines no conversion to string.
                StringConverters.Add(type, null);
                return null;
            }

#if NETCOREAPP3_0_OR_GREATER
            converter = Expression.Lambda<Func<object, string?>>(body, p).Compile();
#else
            converter = Expression.Lambda<Func<object, string>>(body, p).Compile();
#endif
            StringConverters.Add(type, converter);
            return converter;
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

        // Like Enum.Parse, white space around a name is ignored. A member's value may itself begin or end with white space, so value is compared with the values both as given
        // and with the white space around it removed. A value that is only white space is never trimmed to the empty string.
#if NETCOREAPP3_0_OR_GREATER
        private static object? FindMember(Type type, string value, bool ignoreCase)
#else
        [return: MaybeNull]
        private static object FindMember(Type type, string value, bool ignoreCase)
#endif
        {
            var trimmedValue = value.Trim();
            var hasTrimmedValue = trimmedValue.Length != 0 && trimmedValue.Length != value.Length;

            return FindMemberByName(type, trimmedValue, ignoreCase) ?? FindMemberByValue(type, value, hasTrimmedValue ? trimmedValue : null, ignoreCase);
        }

        // In one pass over the members, in member order: an exact (ordinal) match on value wins, then one on trimmedValue; then, when ignoring case, a match on value ignoring case,
        // then one on trimmedValue ignoring case. A null trimmedValue matches nothing.
#if NETCOREAPP3_0_OR_GREATER
        private static object? FindMemberByValue(Type type, string value, string? trimmedValue, bool ignoreCase)
#else
        [return: MaybeNull]
        private static object FindMemberByValue(Type type, string value, [AllowNull] string trimmedValue, bool ignoreCase)
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            object? trimmedMatch = null;
            object? caseInsensitiveMatch = null;
            object? trimmedCaseInsensitiveMatch = null;
#else
            object trimmedMatch = null;
            object caseInsensitiveMatch = null;
            object trimmedCaseInsensitiveMatch = null;
#endif
            foreach (var member in GetCache(type).Members)
            {
                if (string.Equals(member.StringValue, value, StringComparison.Ordinal))
                    return member.Value;
                if (trimmedValue != null && trimmedMatch is null && string.Equals(member.StringValue, trimmedValue, StringComparison.Ordinal))
                    trimmedMatch = member.Value;
                if (!ignoreCase)
                    continue;
                if (caseInsensitiveMatch is null && string.Equals(member.StringValue, value, StringComparison.OrdinalIgnoreCase))
                    caseInsensitiveMatch = member.Value;
                if (trimmedValue != null && trimmedCaseInsensitiveMatch is null && string.Equals(member.StringValue, trimmedValue, StringComparison.OrdinalIgnoreCase))
                    trimmedCaseInsensitiveMatch = member.Value;
            }

            return trimmedMatch ?? caseInsensitiveMatch ?? trimmedCaseInsensitiveMatch;
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
            var toString = GetStringConverter(type);
            if (toString is null)
                throw new InvalidOperationException($"The string enumeration {type.FullName} defines no conversion to string.");

            var fieldsByName = new Dictionary<string, object>(StringComparer.Ordinal);
            var declaredMembers = new List<Member>();
            foreach (var field in GetMemberFields(type))
            {
                var fieldValue = field.GetValue(null);
                if (fieldValue is null)
                    continue;
                if (fieldsByName.ContainsKey(field.Name))
                    throw new InvalidOperationException($"The string enumeration {type.FullName} has more than one member named {field.Name}.");

                fieldsByName.Add(field.Name, fieldValue);
                declaredMembers.Add(new Member(field.Name, fieldValue, toString(fieldValue)));
            }

            // Like Enum, whose members are sorted by their binary values, members are sorted by their values (ordinal comparison, a null value first). Enum leaves the order of
            // members with the same value unspecified, and reflection doesn't promise an order of fields, so those are sorted by name, which makes the order deterministic.
            var members = declaredMembers.OrderBy(member => member.StringValue, StringComparer.Ordinal).ThenBy(member => member.Name, StringComparer.Ordinal).ToList();

            var namesByStringValue = new Dictionary<string, string>(StringComparer.Ordinal);
#if NETCOREAPP3_0_OR_GREATER
            string? nullValueName = null;
#else
            string nullValueName = null;
#endif
            foreach (var member in members)
            {
                if (member.StringValue is null)
                {
                    if (nullValueName is null)
                        nullValueName = member.Name;
                }
                else if (!namesByStringValue.ContainsKey(member.StringValue))
                    namesByStringValue.Add(member.StringValue, member.Name);
            }

            return new MemberCache(type, fieldsByName, members, toString, namesByStringValue, nullValueName);
        }

        #endregion

        #region Nested Types

        private sealed class MemberCache
        {
#if NETCOREAPP3_0_OR_GREATER
            private readonly string? _nullValueName;
#else
            private readonly string _nullValueName;
#endif
            private readonly Dictionary<string, string> _namesByStringValue;

#if NETCOREAPP3_0_OR_GREATER
            private readonly Func<object, string?> _toString;

            public MemberCache(Type type, Dictionary<string, object> fieldsByName, List<Member> members, Func<object, string?> toString,
                Dictionary<string, string> namesByStringValue, string? nullValueName)
#else
            private readonly Func<object, string> _toString;

            public MemberCache(Type type, Dictionary<string, object> fieldsByName, List<Member> members, Func<object, string> toString,
                Dictionary<string, string> namesByStringValue, string nullValueName)
#endif
            {
                Type = type;
                FieldsByName = fieldsByName;
                Members = members;
                _toString = toString;
                _namesByStringValue = namesByStringValue;
                _nullValueName = nullValueName;
            }

            public Type Type { get; }

            public Dictionary<string, object> FieldsByName { get; }

            // The members in the order GetNames returns them: sorted by value.
            public List<Member> Members { get; }

            // The string value of a value of the cached type.
#if NETCOREAPP3_0_OR_GREATER
            public string? ToStringValue(object value) => _toString(value);
#else
            [return: MaybeNull]
            public string ToStringValue(object value) => _toString(value);
#endif

            // The name of the first member, in the order GetNames returns them, whose string value is stringValue, or null if there is none.
#if NETCOREAPP3_0_OR_GREATER
            public string? GetNameByStringValue(string? stringValue)
#else
            [return: MaybeNull]
            public string GetNameByStringValue([AllowNull] string stringValue)
#endif
            {
                if (stringValue is null)
                    return _nullValueName;

                return _namesByStringValue.TryGetValue(stringValue, out var name) ? name : null;
            }
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