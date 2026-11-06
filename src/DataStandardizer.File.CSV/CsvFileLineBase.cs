using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The base of the CSV line types, which holds the fields of a line.
    /// </summary>
    /// <remarks>
    /// Apart from <see cref="Add"/>, the members of <see cref="ICsvFileLine"/> are implemented explicitly, so they don't clash with
    /// the properties of a record model that derives from <see cref="CsvFileRecordLine"/>.
    /// </remarks>
    public abstract class CsvFileLineBase : ICsvFileLine
    {
        // Field keys resolved from property names, keyed by runtime line type then property name.  The resolution
        // depends only on type metadata, so it is computed once per (type, property name) for the process lifetime.
        // Nested rather than keyed by a tuple, so the .NET Standard 1.x build needs no System.ValueTuple reference.
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, string>> PropertyKeyCache =
            new ConcurrentDictionary<Type, ConcurrentDictionary<string, string>>();

        // Both stores keep the fields in position order, inline, with a name lookup beside them.  CsvFieldStore has the members of
        // the generic OrderedDictionary that this class uses, with the same behaviour, so the code below is the same for both.
#if NET9_0_OR_GREATER
        private readonly OrderedDictionary<string, object?> _fieldBag = new OrderedDictionary<string, object?>();
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private readonly CsvFieldStore<object?> _fieldBag = new CsvFieldStore<object?>();
#else
        private readonly CsvFieldStore<object> _fieldBag = new CsvFieldStore<object>();
#endif

        protected int GetFieldCount()
        {
            return _fieldBag.Count;
        }

        /// <summary>
        /// Reserves room for the given number of fields, so a line filled by the reader grows its storage only once.
        /// </summary>
        internal void EnsureFieldCapacity(int capacity)
        {
            _fieldBag.EnsureCapacity(capacity);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private protected IReadOnlyList<object?> FieldValues => _fieldBag.Values;

        protected object? GetFieldValue(string fieldName)
        {
            return _fieldBag.TryGetValue(fieldName, out var fieldValue) ? fieldValue : null;
        }

        protected void SetFieldValue(string fieldName, object? value)
        {
            // The indexer adds the key when it is missing, so no separate lookup is needed.
            _fieldBag[fieldName] = value;
        }
#else
        private protected IReadOnlyList<object> FieldValues => _fieldBag.Values;

        [return: MaybeNull]
        protected object GetFieldValue(string fieldName)
        {
            return _fieldBag.TryGetValue(fieldName, out var fieldValue) ? fieldValue : null;
        }

        protected void SetFieldValue(string fieldName, [AllowNull] object value)
        {
            // The indexer adds the key when it is missing, so no separate lookup is needed.
            _fieldBag[fieldName] = value;
        }
#endif

        protected string GetPropertyKey(string propertyName)
        {
            var lineType = this.GetType();
            if (!PropertyKeyCache.TryGetValue(lineType, out var propertyKeys))
            {
                propertyKeys = PropertyKeyCache.GetOrAdd(lineType, _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));
            }

            if (propertyKeys.TryGetValue(propertyName, out var propertyKey))
            {
                return propertyKey;
            }

            return AddPropertyKey(propertyKeys, lineType, propertyName);
        }

        // Kept out of GetPropertyKey so the closure over lineType is only allocated on a cache miss.
        private static string AddPropertyKey(ConcurrentDictionary<string, string> propertyKeys, Type lineType, string propertyName)
        {
            return propertyKeys.GetOrAdd(propertyName, name => ResolvePropertyKey(lineType, name));
        }

        private static string ResolvePropertyKey(Type lineType, string propertyName)
        {
            var pi = lineType.GetTypeInfo().GetDeclaredProperty(propertyName);
            var fieldAttribute = pi?.GetCustomAttribute<CsvFieldAttribute>();
            return fieldAttribute?.FieldName ?? pi?.Name ?? propertyName;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <inheritdoc cref="ICsvFileLine.Add"/>
        public void Add(string fieldName, object? value)
        {
            _fieldBag.Add(fieldName, value);
        }

        object? ICsvFileLine.this[int index]
        {
            get => _fieldBag.GetAt(index).Value;
            set => _fieldBag.SetAt(index, value);
        }

        object? ICsvFileLine.this[string fieldName]
        {
            get => _fieldBag.TryGetValue(fieldName, out var value) ? value : null;
            set => _fieldBag[fieldName] = value;
        }

        IReadOnlyList<object?> ICsvFileLine.Values => _fieldBag.Values;

        KeyValuePair<string, object?> ICsvFileLine.GetAt(int index)
        {
            return _fieldBag.GetAt(index);
        }

        bool ICsvFileLine.TryGetValue(string fieldName, out object? value)
        {
            return _fieldBag.TryGetValue(fieldName, out value);
        }

        void ICsvFileLine.Insert(int index, string fieldName, object? value)
        {
            _fieldBag.Insert(index, fieldName, value);
        }

        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator()
        {
            return _fieldBag.GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, object?>>.Count => _fieldBag.Count;
#else
        /// <inheritdoc cref="ICsvFileLine.Add"/>
        public void Add(string fieldName, [AllowNull] object value)
        {
            _fieldBag.Add(fieldName, value);
        }

        [MaybeNull]
        object ICsvFileLine.this[int index]
        {
            get => _fieldBag.GetAt(index).Value;
            set => _fieldBag.SetAt(index, value);
        }

        [MaybeNull]
        object ICsvFileLine.this[string fieldName]
        {
            get => _fieldBag.TryGetValue(fieldName, out var value) ? value : null;
            set => _fieldBag[fieldName] = value;
        }

        IReadOnlyList<object> ICsvFileLine.Values => _fieldBag.Values;

        KeyValuePair<string, object> ICsvFileLine.GetAt(int index)
        {
            return _fieldBag.GetAt(index);
        }

        bool ICsvFileLine.TryGetValue(string fieldName, [MaybeNull] out object value)
        {
            return _fieldBag.TryGetValue(fieldName, out value);
        }

        void ICsvFileLine.Insert(int index, string fieldName, [AllowNull] object value)
        {
            _fieldBag.Insert(index, fieldName, value);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _fieldBag.GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _fieldBag.Count;
#endif

        IReadOnlyList<string> ICsvFileLine.Keys => _fieldBag.Keys;

        bool ICsvFileLine.Contains(string fieldName)
        {
            return _fieldBag.ContainsKey(fieldName);
        }

        int ICsvFileLine.IndexOf(string fieldName)
        {
            return _fieldBag.IndexOf(fieldName);
        }

        bool ICsvFileLine.Remove(string fieldName)
        {
            return _fieldBag.Remove(fieldName);
        }

        void ICsvFileLine.RemoveAt(int index)
        {
            _fieldBag.RemoveAt(index);
        }

        void ICsvFileLine.Clear()
        {
            _fieldBag.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fieldBag.GetEnumerator();
        }
    }
}
