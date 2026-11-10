using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The fields of a header, in order.  Lookups by name are case-insensitive.
    /// </summary>
    /// <remarks>
    /// A writer writes the fields that were read, and have not been replaced or removed, byte for byte, so changing one field rewrites that field only.
    /// </remarks>
    public sealed class ImfHeaderFieldCollection : IReadOnlyList<ImfHeaderField>
    {
        private readonly List<ImfHeaderField> _fields = new List<ImfHeaderField>();

        /// <inheritdoc />
        public int Count => _fields.Count;

        /// <inheritdoc />
        public ImfHeaderField this[int index] => _fields[index];

        /// <summary>
        /// Adds a field after the others.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
        public void Add(ImfHeaderField field)
        {
            if (field is null)
                throw new ArgumentNullException(nameof(field));

            _fields.Add(field);
        }

        /// <summary>
        /// Adds a field at an index.  RFC 5322 §3.6.6 and §3.6.7 have resent and trace fields put before the fields already there.
        /// </summary>
        /// <param name="index">The index the field gets.</param>
        /// <param name="field">The field.</param>
        /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0, or more than <see cref="Count"/>.</exception>
        public void Insert(int index, ImfHeaderField field)
        {
            if (field is null)
                throw new ArgumentNullException(nameof(field));

            _fields.Insert(index, field);
        }

        /// <summary>
        /// Replaces the field at an index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="field">The field.</param>
        /// <exception cref="ArgumentNullException"><paramref name="field"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0, or not less than <see cref="Count"/>.</exception>
        public void Replace(int index, ImfHeaderField field)
        {
            if (field is null)
                throw new ArgumentNullException(nameof(field));

            _fields[index] = field;
        }

        /// <summary>
        /// Removes the field at an index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0, or not less than <see cref="Count"/>.</exception>
        public void RemoveAt(int index)
        {
            _fields.RemoveAt(index);
        }

        /// <summary>
        /// Removes every field with a name.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <returns>The number of fields removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public int RemoveAll(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            return _fields.RemoveAll(field => field.HasName(name));
        }

        /// <summary>
        /// Determines if there is a field with a name.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <returns><c>true</c> if there is such a field; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public bool Contains(string name)
        {
            return !(Find(name) is null);
        }

        /// <summary>
        /// Finds the first field with a name.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <returns>The field, or <c>null</c> if there is none.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        [return: MaybeNull]
        public ImfHeaderField Find(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            foreach (var field in _fields)
            {
                if (field.HasName(name)) return field;
            }

            return null;
        }

        /// <summary>
        /// Finds every field with a name.
        /// </summary>
        /// <param name="name">The field name.</param>
        /// <returns>The fields, in order, in a read-only list that doesn't follow later changes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="name"/> is <c>null</c>.</exception>
        public IReadOnlyList<ImfHeaderField> FindAll(string name)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));

            return new ReadOnlyCollection<ImfHeaderField>(_fields.FindAll(field => field.HasName(name)));
        }

        /// <inheritdoc />
        public IEnumerator<ImfHeaderField> GetEnumerator()
        {
            return _fields.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Makes some fields the only ones with a name: they go where the first field with that name is, and otherwise after the other fields.
        /// </summary>
        internal void Set(string name, IReadOnlyList<ImfHeaderField> fields)
        {
            var index = _fields.FindIndex(field => field.HasName(name));
            _fields.RemoveAll(field => field.HasName(name));
            _fields.InsertRange(index < 0 ? _fields.Count : index, fields);
        }
    }
}
