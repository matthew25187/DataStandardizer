using System;
using System.Collections.Generic;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// A line of a CSV file, as an ordered collection of fields, each with a name and a value.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The fields keep the order they were added in.  The field at position <c>N</c> is always the <c>N</c>th field of the line, and
    /// <see cref="Insert"/>, <see cref="RemoveAt"/> and <see cref="Remove"/> move the fields after it up or down by one.
    /// Enumerating a line gives its fields in that order.
    /// </para>
    /// <para>
    /// Field names are compared by ordinal, so they are case-sensitive.  A line can't hold two fields with the same name.
    /// </para>
    /// <para>
    /// Adding, inserting or removing a field, or clearing a line that has fields, invalidates any enumerator of the line, its
    /// <see cref="Keys"/> or its <see cref="Values"/>.  Replacing the value of a field that is present doesn't.
    /// </para>
    /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
    public interface ICsvFileLine : IReadOnlyCollection<KeyValuePair<string, object?>>
#else
    public interface ICsvFileLine : IReadOnlyCollection<KeyValuePair<string, object>>
#endif
    {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets or sets the value of the field at a position.
        /// </summary>
        /// <param name="index">The zero-based position of the field.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or not less than <see cref="IReadOnlyCollection{T}.Count"/>.</exception>
        object? this[int index] { get; set; }

        /// <summary>
        /// Gets or sets the value of a field.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <value>
        /// The value of the field, or <c>null</c> if the line has no field with that name.  Setting the value of a field that isn't
        /// present adds it to the end of the line.
        /// </value>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        object? this[string fieldName] { get; set; }
#else
        /// <summary>
        /// Gets or sets the value of the field at a position.
        /// </summary>
        /// <param name="index">The zero-based position of the field.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or not less than <see cref="IReadOnlyCollection{T}.Count"/>.</exception>
        [MaybeNull]
        [AllowNull]
        object this[int index] { get; set; }

        /// <summary>
        /// Gets or sets the value of a field.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <value>
        /// The value of the field, or <c>null</c> if the line has no field with that name.  Setting the value of a field that isn't
        /// present adds it to the end of the line.
        /// </value>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        [MaybeNull]
        [AllowNull]
        object this[string fieldName] { get; set; }
#endif

        /// <summary>
        /// Gets the names of the fields, in position order.
        /// </summary>
        /// <remarks>The list is a live view of the line, not a copy.</remarks>
        IReadOnlyList<string> Keys { get; }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the values of the fields, in position order.
        /// </summary>
        /// <remarks>The list is a live view of the line, not a copy.</remarks>
        IReadOnlyList<object?> Values { get; }

        /// <summary>
        /// Gets the name and value of the field at a position.
        /// </summary>
        /// <param name="index">The zero-based position of the field.</param>
        /// <returns>The name and value of the field.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or not less than <see cref="IReadOnlyCollection{T}.Count"/>.</exception>
        KeyValuePair<string, object?> GetAt(int index);
#else
        /// <summary>
        /// Gets the values of the fields, in position order.
        /// </summary>
        /// <remarks>The list is a live view of the line, not a copy.</remarks>
        IReadOnlyList<object> Values { get; }

        /// <summary>
        /// Gets the name and value of the field at a position.
        /// </summary>
        /// <param name="index">The zero-based position of the field.</param>
        /// <returns>The name and value of the field.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or not less than <see cref="IReadOnlyCollection{T}.Count"/>.</exception>
        KeyValuePair<string, object> GetAt(int index);
#endif

        /// <summary>
        /// Determines whether the line has a field with a name.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns><c>true</c> if the line has a field named <paramref name="fieldName"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        bool Contains(string fieldName);

        /// <summary>
        /// Gets the position of a field.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns>The zero-based position of the field, or <c>-1</c> if the line has no field with that name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        int IndexOf(string fieldName);

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <summary>
        /// Gets the value of a field, if the line has it.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field, if the line has it; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the line has a field named <paramref name="fieldName"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        bool TryGetValue(string fieldName, out object? value);

        /// <summary>
        /// Adds a field to the end of the line.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The line already has a field named <paramref name="fieldName"/>.</exception>
        void Add(string fieldName, object? value);

        /// <summary>
        /// Inserts a field at a position, moving the field at that position and every field after it up by one.
        /// </summary>
        /// <param name="index">The zero-based position to insert the field at.  <see cref="IReadOnlyCollection{T}.Count"/> adds it to the end of the line.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or greater than <see cref="IReadOnlyCollection{T}.Count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The line already has a field named <paramref name="fieldName"/>.</exception>
        void Insert(int index, string fieldName, object? value);
#else
        /// <summary>
        /// Gets the value of a field, if the line has it.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field, if the line has it; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the line has a field named <paramref name="fieldName"/>; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        bool TryGetValue(string fieldName, [MaybeNull] out object value);

        /// <summary>
        /// Adds a field to the end of the line.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field.</param>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The line already has a field named <paramref name="fieldName"/>.</exception>
        void Add(string fieldName, [AllowNull] object value);

        /// <summary>
        /// Inserts a field at a position, moving the field at that position and every field after it up by one.
        /// </summary>
        /// <param name="index">The zero-based position to insert the field at.  <see cref="IReadOnlyCollection{T}.Count"/> adds it to the end of the line.</param>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="value">The value of the field.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or greater than <see cref="IReadOnlyCollection{T}.Count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The line already has a field named <paramref name="fieldName"/>.</exception>
        void Insert(int index, string fieldName, [AllowNull] object value);
#endif

        /// <summary>
        /// Removes a field, moving every field after it down by one.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <returns><c>true</c> if the field was removed; <c>false</c> if the line has no field with that name.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="fieldName"/> is <c>null</c>.</exception>
        bool Remove(string fieldName);

        /// <summary>
        /// Removes the field at a position, moving every field after it down by one.
        /// </summary>
        /// <param name="index">The zero-based position of the field.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero, or not less than <see cref="IReadOnlyCollection{T}.Count"/>.</exception>
        void RemoveAt(int index);

        /// <summary>
        /// Removes every field from the line.
        /// </summary>
        void Clear();
    }
}
