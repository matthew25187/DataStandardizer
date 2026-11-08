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
        private static readonly Dictionary<Type, Func<string, object>> FromStringConverters = new Dictionary<Type, Func<string, object>>();

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

                return cache.GetNameByStringValue(ToStringValueOfStringEnum(value, valueType));
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
        /// Returns an indication whether a constant with a specified name or value exists in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The name or value of a constant in <paramref name="enumType"/>.</param>
        /// <returns><c>true</c> if a constant in <paramref name="enumType"/> has a name or a value equal to <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <see cref="Enum.IsDefined(Type, object)"/> compares a string with the names only. A string enumeration's underlying type is <see cref="string"/>, so
        /// <paramref name="value"/> is compared with both the names and the values. Both comparisons are exact (ordinal, case-sensitive), and white space isn't removed.
        /// </remarks>
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
                return IsNameOrValueDefined(GetCache(enumType), value);
            }
        }

        /// <summary>
        /// Returns an indication whether a constant with a specified name or value exists in a specified enumeration.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">A value of <paramref name="enumType"/>, or the name or value of a constant in <paramref name="enumType"/>, as a <see cref="string"/>.</param>
        /// <returns>
        /// <c>true</c> if a constant in <paramref name="enumType"/> has the value of <paramref name="value"/>, or, when <paramref name="value"/> is a <see cref="string"/>, a name
        /// or a value equal to it; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is a value of a string enumeration other than <paramref name="enumType"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="value"/> is neither a string enumeration value nor a <see cref="string"/>.
        /// -or-
        /// The string enumeration has more than one member with the same name.
        /// -or-
        /// <paramref name="enumType"/> defines no conversion to <see cref="string"/>.
        /// </exception>
        /// <remarks>
        /// <para>
        /// As <see cref="Enum.IsDefined(Type, object)"/> does, this checks a value of <paramref name="enumType"/> by its value, rejects a value of another enumeration, and throws
        /// <see cref="InvalidOperationException"/> for a value of a type that can't be an enumeration's underlying type. A <see cref="string"/> is compared as
        /// <see cref="IsDefined(Type, string)"/> compares it.
        /// </para>
        /// <para>
        /// A string enumeration usually converts implicitly to <see cref="string"/>, so a call whose <paramref name="value"/> has a string enumeration type at compile time binds to
        /// <see cref="IsDefined(Type, string)"/> instead. Pass the value as an <see cref="object"/> to have its type checked.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool IsDefined(Type enumType, object value)
#else
        public static bool IsDefined([DisallowNull] Type enumType, [DisallowNull] object value)
#endif
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            ThrowIfNotStringEnumType(enumType);

            var valueType = value.GetType();
            if (value is string valueString)
            {
                lock (SyncRoot)
                {
                    return IsNameOrValueDefined(GetCache(enumType), valueString);
                }
            }

            ThrowIfValueOfOtherType(enumType, valueType, true);

            lock (SyncRoot)
            {
                var cache = GetCache(enumType);
                return cache.GetNameByStringValue(cache.ToStringValue(value)) != null;
            }
        }

        /// <summary>
        /// Returns an indication whether a constant with a specified name or value exists in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">The name or value of a constant in <typeparamref name="TEnum"/>.</param>
        /// <returns><c>true</c> if a constant in <typeparamref name="TEnum"/> has a name or a value equal to <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <see cref="Enum.IsDefined(Type, object)"/> compares a string with the names only. A string enumeration's underlying type is <see cref="string"/>, so
        /// <paramref name="value"/> is compared with both the names and the values. Both comparisons are exact (ordinal, case-sensitive), and white space isn't removed.
        /// </remarks>
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
                return IsNameOrValueDefined(GetCache(typeof(TEnum)), value);
            }
        }

        /// <summary>
        /// Returns an indication whether a constant with a specified value exists in a specified enumeration.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">A value of <typeparamref name="TEnum"/>.</param>
        /// <returns><c>true</c> if a constant in <typeparamref name="TEnum"/> has the value of <paramref name="value"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">
        /// The string enumeration has more than one member with the same name.
        /// -or-
        /// <typeparamref name="TEnum"/> defines no conversion to <see cref="string"/>.
        /// </exception>
        /// <remarks><paramref name="value"/> is compared with the members' values by its string value (ordinal comparison).</remarks>
        public static bool IsDefined<TEnum>(TEnum value) where TEnum : struct, IStringEnum
        {
            lock (SyncRoot)
            {
                var cache = GetCache(typeof(TEnum));
                return cache.GetNameByStringValue(cache.ToStringValue(value)) != null;
            }
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object.
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
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames(Type)"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this throws <see cref="ArgumentException"/> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject(Type, string)"/> to get a
        /// value that isn't defined.
        /// </para>
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

            return ParseCore(enumType, value, false);
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the operation
        /// is case-insensitive.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <param name="ignoreCase">If <c>true</c>, ignore case; otherwise, regard case.</param>
        /// <returns>An object of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames(Type)"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this throws <see cref="ArgumentException"/> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject(Type, string)"/> to get a
        /// value that isn't defined.
        /// </para>
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

            return ParseCore(enumType, value, ignoreCase);
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <returns>An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames{TEnum}"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this throws <see cref="ArgumentException"/> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject{TEnum}(string)"/> to get a
        /// value that isn't defined.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum Parse<TEnum>(string value) where TEnum : struct, IStringEnum
#else
        public static TEnum Parse<TEnum>([DisallowNull] string value) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return (TEnum)ParseCore(typeof(TEnum), value, false);
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the operation
        /// is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">A string containing the name or value to convert.</param>
        /// <param name="ignoreCase">If <c>true</c>, ignore case; otherwise, regard case.</param>
        /// <returns>An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is either an empty string or only contains white space.
        /// -or-
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames{TEnum}"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this throws <see cref="ArgumentException"/> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject{TEnum}(string)"/> to get a
        /// value that isn't defined.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static TEnum Parse<TEnum>(string value, bool ignoreCase) where TEnum : struct, IStringEnum
#else
        public static TEnum Parse<TEnum>([DisallowNull] string value, bool ignoreCase) where TEnum : struct, IStringEnum
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return (TEnum)ParseCore(typeof(TEnum), value, ignoreCase);
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
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">
        /// The value: a <see cref="string"/> (the enumeration's underlying type), a value of <paramref name="enumType"/>, or a value of another string enumeration, whose string value is
        /// used.
        /// </param>
        /// <returns>An enumeration object whose value is <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> or <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is neither a string enumeration value nor a <see cref="string"/>.
        /// -or-
        /// <paramref name="value"/> is a value of another string enumeration that defines no conversion to <see cref="string"/>.
        /// -or-
        /// <paramref name="value"/> is a value of another string enumeration that converts to a <c>null</c> string, as a default value usually does.
        /// </exception>
        /// <exception cref="InvalidOperationException"><paramref name="enumType"/> defines no conversion from <see cref="string"/>.</exception>
        /// <exception cref="InvalidCastException">The enumeration's conversion from <see cref="string"/> rejects the value.</exception>
        /// <remarks>
        /// <para>
        /// As <see cref="Enum.ToObject(Type, object)"/> does, this accepts a value of the underlying type or of any enumeration, and doesn't check that the value is defined. A value
        /// of <paramref name="enumType"/> is returned as it is. A value of another string enumeration is converted to <paramref name="enumType"/> through its string value, as
        /// <see cref="ToObject(Type, string)"/> converts a string, so one that converts to a <c>null</c> string is rejected, as <see cref="ToObject(Type, string)"/> rejects <c>null</c>.
        /// </para>
        /// <para>
        /// A string enumeration usually converts implicitly to <see cref="string"/>, so a call whose <paramref name="value"/> has a string enumeration type at compile time binds to
        /// <see cref="ToObject(Type, string)"/> instead, which converts its string value in the same way.
        /// </para>
        /// <para>Any other exception that a string enumeration's conversion from or to <see cref="string"/> throws is passed on to the caller unchanged.</para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static object ToObject(Type enumType, object value)
#else
        [return: NotNull]
        public static object ToObject([DisallowNull] Type enumType, [DisallowNull] object value)
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            if (value is string valueString)
                return Cast(enumType, valueString);

            var valueType = value.GetType();
            if (valueType == enumType)
                return value;
            if (!IsStringEnumType(valueType))
                throw new ArgumentException($"{nameof(value)} is neither a string enumeration value nor a string.", nameof(value));

            var stringValue = ToStringValueOfStringEnum(value, valueType);
            if (stringValue is null)
                throw new ArgumentException($"{nameof(value)} is of a string enumeration and converts to a null string, which no string enumeration can be made from.", nameof(value));

            return Cast(enumType, stringValue);
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
        /// Converts the specified value of a specified string enumeration to its equivalent string representation according to the specified format.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">A value of <paramref name="enumType"/>, or a <see cref="string"/> (its underlying type).</param>
        /// <param name="format">The output format to use.</param>
        /// <returns>A string representation of <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/>, <paramref name="value"/> or <paramref name="format"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is a value of a string enumeration other than <paramref name="enumType"/>.
        /// -or-
        /// <paramref name="value"/> is neither a string enumeration value nor a <see cref="string"/>.
        /// </exception>
        /// <exception cref="FormatException"><paramref name="format"/> is not one of the format strings that are listed in the remarks.</exception>
        /// <exception cref="InvalidOperationException">
        /// <paramref name="format"/> is "X" or "x", which a string enumeration doesn't support.
        /// -or-
        /// The string enumeration has more than one member with the same name.
        /// -or-
        /// <paramref name="enumType"/> defines no conversion to <see cref="string"/>.
        /// </exception>
        /// <remarks>
        /// <para>The format strings are those of <see cref="Enum.Format(Type, object, string)"/>, for an underlying type of <see cref="string"/>:</para>
        /// <list type="bullet">
        /// <item><description>"G" or "g", and "F" or "f": the name of the member with the value, the first in the order in which <see cref="GetNames(Type)"/> returns the names; or, if no member has it, the value itself. A string enumeration's values can't be combined, so "F" is the same as "G".</description></item>
        /// <item><description>"D" or "d": the value itself.</description></item>
        /// <item><description>"X" or "x": hexadecimal, which has no meaning for a <see cref="string"/>, so it throws <see cref="InvalidOperationException"/>, as <see cref="Enum.Format(Type, object, string)"/> does for an underlying type it can't write in hexadecimal.</description></item>
        /// </list>
        /// <para>The default value of a string enumeration has a <c>null</c> value, which is written as an empty string, unless a member has that value.</para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static string Format(Type enumType, object value, string format)
#else
        [return: NotNull]
        public static string Format([DisallowNull] Type enumType, [DisallowNull] object value, [DisallowNull] string format)
#endif
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));
            if (format is null)
                throw new ArgumentNullException(nameof(format));
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            var valueType = value.GetType();
            var valueString = value as string;
            if (valueString is null)
                ThrowIfValueOfOtherType(enumType, valueType, false);

            var formatCode = format.Length == 1 ? format[0] : '\0';
            if (formatCode == 'X' || formatCode == 'x')
                throw new InvalidOperationException("A string enumeration can't be written in hexadecimal (\"X\"), as its underlying type is string.");
            if (formatCode != 'G' && formatCode != 'g' && formatCode != 'F' && formatCode != 'f' && formatCode != 'D' && formatCode != 'd')
                throw new FormatException("The format string of a string enumeration can only be \"G\", \"g\", \"F\", \"f\", \"D\" or \"d\".");

            lock (SyncRoot)
            {
                var cache = GetCache(enumType);
                var stringValue = valueString ?? cache.ToStringValue(value);
                if (formatCode == 'D' || formatCode == 'd')
                    return stringValue ?? string.Empty;

                return cache.GetNameByStringValue(stringValue) ?? stringValue ?? string.Empty;
            }
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object. The return value indicates whether the
        /// conversion succeeded.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The string representation of the name or value of an enumerated constant.</param>
        /// <param name="result">When this method returns <c>true</c>, contains an enumeration constant that represents the parsed value.</param>
        /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c> and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>An empty <paramref name="value"/>, or one that is only white space, fails, as it does for <see cref="Enum"/>.</para>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames(Type)"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this returns <c>false</c> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject(Type, string)"/> to get a
        /// value that isn't defined.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse(Type enumType, string? value, out object? result)
#else
        public static bool TryParse([DisallowNull] Type enumType, [AllowNull] string value, [MaybeNullWhen(false)] out object result)
#endif
        {
            return TryParse(enumType, value, false, out result);
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the operation
        /// is case-insensitive. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The string representation of the name or value of an enumerated constant.</param>
        /// <param name="ignoreCase"><c>true</c> to read <paramref name="value"/> in case-insensitive mode; <c>false</c> to read <paramref name="value"/> in case-sensitive mode.</param>
        /// <param name="result">When this method returns <c>true</c>, contains an enumeration constant that represents the parsed value.</param>
        /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c> and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration and <paramref name="value"/> is not <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>An empty <paramref name="value"/>, or one that is only white space, fails, as it does for <see cref="Enum"/>.</para>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames(Type)"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this returns <c>false</c> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject(Type, string)"/> to get a
        /// value that isn't defined.
        /// </para>
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

            result = TryParseCore(enumType, value, ignoreCase);
            return result != null;
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object. The return value indicates whether the
        /// conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type to which to convert <paramref name="value"/>.</typeparam>
        /// <param name="value">The string representation of the enumeration name or value to convert.</param>
        /// <param name="result">When this method returns, contains an object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>An empty <paramref name="value"/>, or one that is only white space, fails, as it does for <see cref="Enum"/>.</para>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames{TEnum}"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this returns <c>false</c> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject{TEnum}(string)"/> to get a
        /// value that isn't defined.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([AllowNull] string value, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            return TryParse(value, false, out result);
        }

        /// <summary>
        /// Converts the string representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the operation
        /// is case-insensitive. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type to which to convert <paramref name="value"/>.</typeparam>
        /// <param name="value">The string representation of the enumeration name or value to convert.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case; <c>false</c> to consider case.</param>
        /// <param name="result">When this method returns, contains an object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>. This parameter is passed uninitialized.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks>
        /// <para>An empty <paramref name="value"/>, or one that is only white space, fails, as it does for <see cref="Enum"/>.</para>
        /// <para>
        /// <paramref name="value"/> is a single name or value. Unlike <see cref="Enum"/>, a list of names separated by commas isn't combined, as the values of a string
        /// enumeration can't be combined, and a value may itself contain a comma.
        /// </para>
        /// <para>
        /// <paramref name="value"/> is compared with the members' names first, then with their values. White space before or after <paramref name="value"/> is ignored when it is
        /// compared with the names, as <see cref="Enum"/> does. It is compared with the values as given, and then, if no value matches, with the white space removed, so a member
        /// whose value begins or ends with white space can still be parsed. When case is ignored, each of those comparisons returns the first member that matches, in the order in
        /// which <see cref="GetNames{TEnum}"/> returns the names, as <see cref="Enum"/> does, even when another member matches exactly.
        /// </para>
        /// <para>
        /// <see cref="Enum"/> returns a value that isn't defined when the text is a number. A string enumeration's underlying type is <see cref="string"/>, so any text could be such
        /// a value, and it couldn't be told apart from a name. So this returns <c>false</c> for a <paramref name="value"/> that is neither the name nor the value of a member. Use <see cref="ToObject{TEnum}(string)"/> to get a
        /// value that isn't defined.
        /// </para>
        /// </remarks>
#if NETCOREAPP3_0_OR_GREATER
        public static bool TryParse<TEnum>(string? value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#else
        public static bool TryParse<TEnum>([AllowNull] string value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
#endif
        {
            if (value != null && TryParseCore(typeof(TEnum), value, ignoreCase) is TEnum member)
            {
                result = member;
                return true;
            }

            result = default;
            return false;
        }

#if NET
        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <returns>An object of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is either empty or only contains white space.
        /// -or-
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="Parse(Type, string)"/> reads a string.</remarks>
        public static object Parse(Type enumType, ReadOnlySpan<char> value)
        {
            return Parse(enumType, value, false);
        }

        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the
        /// operation is case-insensitive.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <param name="ignoreCase">If <c>true</c>, ignore case; otherwise, regard case.</param>
        /// <returns>An object of type <paramref name="enumType"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not a string enumeration.
        /// -or-
        /// <paramref name="value"/> is either empty or only contains white space.
        /// -or-
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="Parse(Type, string, bool)"/> reads a string.</remarks>
        public static object Parse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase)
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            return ParseCore(enumType, value, ignoreCase);
        }

        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <returns>An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is either empty or only contains white space.
        /// -or-
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="Parse{TEnum}(string)"/> reads a string.</remarks>
        public static TEnum Parse<TEnum>(ReadOnlySpan<char> value) where TEnum : struct, IStringEnum
        {
            return (TEnum)ParseCore(typeof(TEnum), value, false);
        }

        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the
        /// operation is case-insensitive.
        /// </summary>
        /// <typeparam name="TEnum">A string enumeration type.</typeparam>
        /// <param name="value">A span containing the name or value to convert.</param>
        /// <param name="ignoreCase">If <c>true</c>, ignore case; otherwise, regard case.</param>
        /// <returns>An object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="value"/> is either empty or only contains white space.
        /// -or-
        /// <paramref name="value"/> is neither the name nor the value of a constant defined for the enumeration.
        /// </exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="Parse{TEnum}(string, bool)"/> reads a string.</remarks>
        public static TEnum Parse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase) where TEnum : struct, IStringEnum
        {
            return (TEnum)ParseCore(typeof(TEnum), value, ignoreCase);
        }

        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object. The return value indicates whether
        /// the conversion succeeded.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The span representation of the name or value of an enumerated constant.</param>
        /// <param name="result">When this method returns <c>true</c>, contains an enumeration constant that represents the parsed value.</param>
        /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="TryParse(Type, string, out object)"/> reads a string.</remarks>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, out object? result)
        {
            return TryParse(enumType, value, false, out result);
        }

        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the
        /// operation is case-insensitive. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <param name="enumType">A string enumeration type.</param>
        /// <param name="value">The span representation of the name or value of an enumerated constant.</param>
        /// <param name="ignoreCase"><c>true</c> to read <paramref name="value"/> in case-insensitive mode; <c>false</c> to read <paramref name="value"/> in case-sensitive mode.</param>
        /// <param name="result">When this method returns <c>true</c>, contains an enumeration constant that represents the parsed value.</param>
        /// <returns><c>true</c> if the conversion succeeded; <c>false</c> otherwise.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="enumType"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="enumType"/> is not a string enumeration.</exception>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="TryParse(Type, string, bool, out object)"/> reads a string.</remarks>
        public static bool TryParse(Type enumType, ReadOnlySpan<char> value, bool ignoreCase, out object? result)
        {
            if (enumType is null)
                throw new ArgumentNullException(nameof(enumType));
            ThrowIfNotStringEnumType(enumType);

            result = TryParseCore(enumType, value, ignoreCase);
            return result != null;
        }

        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object. The return value indicates whether
        /// the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type to which to convert <paramref name="value"/>.</typeparam>
        /// <param name="value">The span representation of the enumeration name or value to convert.</param>
        /// <param name="result">When this method returns, contains an object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="TryParse{TEnum}(string, out TEnum)"/> reads a string.</remarks>
        public static bool TryParse<TEnum>(ReadOnlySpan<char> value, out TEnum result) where TEnum : struct, IStringEnum
        {
            return TryParse(value, false, out result);
        }

        /// <summary>
        /// Converts the span of characters representation of the name or value of an enumerated constant to an equivalent enumerated object. A parameter specifies whether the
        /// operation is case-insensitive. The return value indicates whether the conversion succeeded.
        /// </summary>
        /// <typeparam name="TEnum">The enumeration type to which to convert <paramref name="value"/>.</typeparam>
        /// <param name="value">The span representation of the enumeration name or value to convert.</param>
        /// <param name="ignoreCase"><c>true</c> to ignore case; <c>false</c> to consider case.</param>
        /// <param name="result">When this method returns, contains an object of type <typeparamref name="TEnum"/> whose value is represented by <paramref name="value"/>.</param>
        /// <returns><c>true</c> if the <paramref name="value"/> parameter was converted successfully; otherwise, <c>false</c>.</returns>
        /// <exception cref="InvalidOperationException">The string enumeration has more than one member with the same name.</exception>
        /// <remarks><paramref name="value"/> is read as <see cref="TryParse{TEnum}(string, bool, out TEnum)"/> reads a string.</remarks>
        public static bool TryParse<TEnum>(ReadOnlySpan<char> value, bool ignoreCase, out TEnum result) where TEnum : struct, IStringEnum
        {
            if (TryParseCore(typeof(TEnum), value, ignoreCase) is TEnum member)
            {
                result = member;
                return true;
            }

            result = default;
            return false;
        }
#endif

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

        // Builds the conversion for the type itself rather than going through reflection, so whatever the conversion throws reaches the caller unwrapped. Each type's conversion
        // is compiled once; Expression.Convert throws InvalidOperationException for a type with no conversion from string, and that isn't cached. The conversion runs outside
        // SyncRoot.
        private static object Cast(Type enumType, string value)
        {
            Func<string, object> converter;
            lock (SyncRoot)
            {
                if (!FromStringConverters.TryGetValue(enumType, out var cached))
                {
                    var p = Expression.Parameter(typeof(string));
                    var body = Expression.Convert(Expression.Convert(p, enumType), typeof(object));
                    cached = Expression.Lambda<Func<string, object>>(body, p).Compile();
                    FromStringConverters.Add(enumType, cached);
                }

                converter = cached;
            }

            return converter(value);
        }

        // The string value of a value of any string enumeration, through its conversion to string. Takes SyncRoot, which a caller may already hold, as Monitor is re-entrant.
#if NETCOREAPP3_0_OR_GREATER
        private static string? ToStringValueOfStringEnum(object value, Type valueType)
#else
        [return: MaybeNull]
        private static string ToStringValueOfStringEnum(object value, Type valueType)
#endif
        {
            lock (SyncRoot)
            {
                var toString = GetStringConverter(valueType);
                if (toString is null)
                    throw new ArgumentException($"{nameof(value)} is of a string enumeration that defines no conversion to string.", nameof(value));

                return toString(value);
            }
        }

        // The work of the Parse overloads, once their arguments are checked. On .NET the matching works on a span, so the string and span overloads share it; elsewhere, on a
        // string.
#if NET
        private static object ParseCore(Type enumType, ReadOnlySpan<char> value, bool ignoreCase)
#else
        private static object ParseCore(Type enumType, string value, bool ignoreCase)
#endif
        {
            if (IsEmptyOrWhiteSpace(value))
                throw new ArgumentException($"{nameof(value)} is either an empty string or only contains white space.", nameof(value));

            lock (SyncRoot)
            {
                var member = FindMember(GetCache(enumType), value, ignoreCase);
                if (member != null)
                    return member;
            }

            // Like Enum.Parse, which gives no parameter name for a value it doesn't find.
            throw new ArgumentException($"Requested value '{value.ToString()}' was not found.");
        }

        // The work of the TryParse overloads, once their arguments are checked: the member value represents, or null. Like Enum.TryParse, an empty value or one that is only
        // white space fails.
#if NET
        private static object? TryParseCore(Type enumType, ReadOnlySpan<char> value, bool ignoreCase)
#elif NETCOREAPP3_0_OR_GREATER
        private static object? TryParseCore(Type enumType, string value, bool ignoreCase)
#else
        [return: MaybeNull]
        private static object TryParseCore(Type enumType, string value, bool ignoreCase)
#endif
        {
            if (IsEmptyOrWhiteSpace(value))
                return null;

            lock (SyncRoot)
            {
                return FindMember(GetCache(enumType), value, ignoreCase);
            }
        }

        // Like Enum.Parse, white space around a name is ignored. A member's value may itself begin or end with white space, so value is compared with the values as given, and
        // then, if none matches, with the white space around it removed. Like Enum.Parse, each comparison returns the first member that matches, in GetNames order, whether or
        // not another member matches exactly when case is ignored. Callers hold SyncRoot, and value isn't empty or only white space.
#if NET
        private static object? FindMember(MemberCache cache, ReadOnlySpan<char> value, bool ignoreCase)
#elif NETCOREAPP3_0_OR_GREATER
        private static object? FindMember(MemberCache cache, string value, bool ignoreCase)
#else
        [return: MaybeNull]
        private static object FindMember(MemberCache cache, string value, bool ignoreCase)
#endif
        {
            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var trimmedValue = value.Trim();

            var member = FindMemberByName(cache, trimmedValue, comparison) ?? FindMemberByValue(cache, value, comparison);
            if (member is null && trimmedValue.Length != value.Length)
                member = FindMemberByValue(cache, trimmedValue, comparison);

            return member;
        }

#if NET
        private static bool IsEmptyOrWhiteSpace(ReadOnlySpan<char> value) => value.IsWhiteSpace();
#else
        private static bool IsEmptyOrWhiteSpace(string value) => string.IsNullOrWhiteSpace(value);
#endif

        // Whether value is the name or the value of a member, compared exactly (ordinal, case-sensitive). Callers hold SyncRoot.
        private static bool IsNameOrValueDefined(MemberCache cache, string value)
        {
            return FindMemberByName(cache, value, StringComparison.Ordinal) != null || FindMemberByValue(cache, value, StringComparison.Ordinal) != null;
        }

        // The first member, in GetNames order, whose name matches value. Names are unique, so an ordinal match is a lookup.
#if NET
        private static object? FindMemberByName(MemberCache cache, ReadOnlySpan<char> value, StringComparison comparison)
#elif NETCOREAPP3_0_OR_GREATER
        private static object? FindMemberByName(MemberCache cache, string value, StringComparison comparison)
#else
        [return: MaybeNull]
        private static object FindMemberByName(MemberCache cache, string value, StringComparison comparison)
#endif
        {
            if (comparison == StringComparison.Ordinal)
#if NET
                return cache.FieldsByName.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(value, out var member) ? member : null;
#else
                return cache.FieldsByName.TryGetValue(value, out var member) ? member : null;
#endif

            foreach (var candidate in cache.Members)
            {
                if (value.Equals(candidate.Name, comparison))
                    return candidate.Value;
            }

            return null;
        }

        // The first member, in GetNames order, whose value matches value.
#if NET
        private static object? FindMemberByValue(MemberCache cache, ReadOnlySpan<char> value, StringComparison comparison)
#elif NETCOREAPP3_0_OR_GREATER
        private static object? FindMemberByValue(MemberCache cache, string value, StringComparison comparison)
#else
        [return: MaybeNull]
        private static object FindMemberByValue(MemberCache cache, string value, StringComparison comparison)
#endif
        {
            foreach (var candidate in cache.Members)
            {
                // A null value matches nothing. A null string would otherwise convert to an empty span.
                if (candidate.StringValue != null && value.Equals(candidate.StringValue, comparison))
                    return candidate.Value;
            }

            return null;
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

        // Throws unless valueType is enumType. Like Enum.IsDefined and Enum.Format, which give no parameter name for a value of the wrong type: a value of another string
        // enumeration throws ArgumentException, and a value of any other type throws InvalidOperationException, as Enum.IsDefined does, or ArgumentException, as Enum.Format does.
        private static void ThrowIfValueOfOtherType(Type enumType, Type valueType, bool otherTypeIsInvalidOperation)
        {
            if (valueType == enumType)
                return;
            if (IsStringEnumType(valueType))
                throw new ArgumentException($"The value is of the string enumeration {valueType.FullName}, not of {enumType.FullName}.");

            const string message = "value is neither a string enumeration value nor a string.";
            if (otherTypeIsInvalidOperation)
                throw new InvalidOperationException(message);

            throw new ArgumentException(message);
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