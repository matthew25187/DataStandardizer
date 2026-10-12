using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Reflection;
#if NET9_0_OR_GREATER
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
#endif
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using JetBrains.Annotations;
#endif

namespace DataStandardizer.File.CSV
{
    public abstract class CsvFileLineBase : ICsvFileLine
    {
        // Field keys resolved from property names, keyed by runtime line type then property name.  The resolution
        // depends only on type metadata, so it is computed once per (type, property name) for the process lifetime.
        // Nested rather than keyed by a tuple, so the .NET Standard 1.x build needs no System.ValueTuple reference.
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, string>> PropertyKeyCache =
            new ConcurrentDictionary<Type, ConcurrentDictionary<string, string>>();

#if NET9_0_OR_GREATER
        private const string NonStringKeyMessage = "Field names must be strings.";

        // The generic dictionary stores keys and values inline, without the boxed DictionaryEntry per field and the
        // object-keyed Hashtable that back the non-generic OrderedDictionary.  Field names are always strings, so the
        // non-generic interface members below accept string keys only.
        private readonly OrderedDictionary<string, object?> _fieldBag = new OrderedDictionary<string, object?>();
#else
        private readonly IOrderedDictionary _fieldBag = new OrderedDictionary();
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
#if NET9_0_OR_GREATER
            _fieldBag.EnsureCapacity(capacity);
#endif
        }
#if NET9_0_OR_GREATER
        private protected IList<object?> FieldValues => _fieldBag.Values;

        protected object? GetFieldValue(string fieldName)
        {
            return _fieldBag.TryGetValue(fieldName, out var fieldValue) ? fieldValue : null;
        }

        protected void SetFieldValue(string fieldName, object? value)
        {
            // The indexer adds the key when it is missing, so no separate lookup is needed.
            _fieldBag[fieldName] = value;
        }

        private static string ToFieldName(object key)
        {
            if (key is string fieldName)
            {
                return fieldName;
            }

            ArgumentNullException.ThrowIfNull(key);
            throw new ArgumentException(NonStringKeyMessage, nameof(key));
        }

        // A key that is not a string can never be present, so lookups treat it as missing rather than throwing,
        // matching how the generic dictionaries implement IDictionary.
        private static bool TryGetFieldName(object key, [NotNullWhen(true)] out string? fieldName)
        {
            ArgumentNullException.ThrowIfNull(key);
            fieldName = key as string;
            return fieldName is not null;
        }
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        protected object? GetFieldValue(string fieldName)
        {
            // The indexer returns null for a missing key, so no separate Contains lookup is needed.
            return _fieldBag[fieldName];
        }

        protected void SetFieldValue(string fieldName, object? value)
        {
            // The indexer adds the key when it is missing, so no separate Contains lookup is needed.
            _fieldBag[fieldName] = value;
        }
#else
        [CanBeNull]
        protected object GetFieldValue(string fieldName)
        {
            // The indexer returns null for a missing key, so no separate Contains lookup is needed.
            return _fieldBag[fieldName];
        }

        protected void SetFieldValue(string fieldName, [CanBeNull] object value)
        {
            // The indexer adds the key when it is missing, so no separate Contains lookup is needed.
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

        // Copies DictionaryEntry items in field order, the same order as enumeration.  The non-generic OrderedDictionary
        // copies from its Hashtable, in hash order, and the generic one copies KeyValuePair items, so neither is used
        // directly.  Array.CopyTo supplies the argument and element type checks.
        private void CopyFieldsTo(Array array, int index)
        {
            var entries = new DictionaryEntry[_fieldBag.Count];
            var enumerator = ((IDictionary)_fieldBag).GetEnumerator();
            for (var entryIndex = 0; enumerator.MoveNext(); entryIndex++)
            {
                entries[entryIndex] = enumerator.Entry;
            }

            entries.CopyTo(array, index);
        }

#if NET9_0_OR_GREATER
        public void Add(object key, object? value)
        {
            _fieldBag.Add(ToFieldName(key), value);
        }

        void IDictionary.Clear()
        {
            _fieldBag.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            return TryGetFieldName(key, out var fieldName) && _fieldBag.ContainsKey(fieldName);
        }

        IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
        {
            return ((IDictionary)_fieldBag).GetEnumerator();
        }

        void IOrderedDictionary.Insert(int index, object key, object? value)
        {
            _fieldBag.Insert(index, ToFieldName(key), value);
        }

        void IOrderedDictionary.RemoveAt(int index)
        {
            _fieldBag.RemoveAt(index);
        }

        object? IOrderedDictionary.this[int index]
        {
            get => _fieldBag.GetAt(index).Value;
            set => _fieldBag.SetAt(index, value);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)_fieldBag).GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            if (TryGetFieldName(key, out var fieldName))
            {
                _fieldBag.Remove(fieldName);
            }
        }

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        object? IDictionary.this[object key]
        {
            get => TryGetFieldName(key, out var fieldName) && _fieldBag.TryGetValue(fieldName, out var fieldValue) ? fieldValue : null;
            set => _fieldBag[ToFieldName(key)] = value;
        }

        ICollection IDictionary.Keys => _fieldBag.Keys;

        ICollection IDictionary.Values => _fieldBag.Values;

        IEnumerator IEnumerable.GetEnumerator()
        {
            // Enumerate DictionaryEntry items, as the non-generic OrderedDictionary does, not KeyValuePair items.
            return ((IDictionary)_fieldBag).GetEnumerator();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            CopyFieldsTo(array, index);
        }

        int ICollection.Count => _fieldBag.Count;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => ((ICollection)_fieldBag).SyncRoot;
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public void Add(object key, object? value)
        {
            _fieldBag.Add(key, value);
        }

        void IDictionary.Clear()
        {
            _fieldBag.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            return _fieldBag.Contains(key);
        }

        IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
        {
            return _fieldBag.GetEnumerator();
        }

        void IOrderedDictionary.Insert(int index, object key, object? value)
        {
            _fieldBag.Insert(index, key, value);
        }

        void IOrderedDictionary.RemoveAt(int index)
        {
            _fieldBag.RemoveAt(index);
        }

        object? IOrderedDictionary.this[int index]
        {
            get => _fieldBag[index];
            set => _fieldBag[index] = value;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)_fieldBag).GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            _fieldBag.Remove(key);
        }

        bool IDictionary.IsFixedSize => _fieldBag.IsFixedSize;

        bool IDictionary.IsReadOnly => _fieldBag.IsReadOnly;

        object? IDictionary.this[object key]
        {
            get => _fieldBag[key];
            set => _fieldBag[key] = value;
        }

        ICollection IDictionary.Keys => _fieldBag.Keys;

        ICollection IDictionary.Values => _fieldBag.Values;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_fieldBag).GetEnumerator();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            CopyFieldsTo(array, index);
        }

        int ICollection.Count => _fieldBag.Count;

        bool ICollection.IsSynchronized => _fieldBag.IsSynchronized;

        object ICollection.SyncRoot => _fieldBag.SyncRoot; 
#else
        public void Add(object key, object value)
        {
            _fieldBag.Add(key, value);
        }

        void IDictionary.Clear()
        {
            _fieldBag.Clear();
        }

        bool IDictionary.Contains(object key)
        {
            return _fieldBag.Contains(key);
        }

        IDictionaryEnumerator IOrderedDictionary.GetEnumerator()
        {
            return _fieldBag.GetEnumerator();
        }

        void IOrderedDictionary.Insert(int index, object key, object value)
        {
            _fieldBag.Insert(index, key, value);
        }

        void IOrderedDictionary.RemoveAt(int index)
        {
            _fieldBag.RemoveAt(index);
        }

        object IOrderedDictionary.this[int index]
        {
            get => _fieldBag[index];
            set => _fieldBag[index] = value;
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary)_fieldBag).GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            _fieldBag.Remove(key);
        }

        bool IDictionary.IsFixedSize => _fieldBag.IsFixedSize;

        bool IDictionary.IsReadOnly => _fieldBag.IsReadOnly;

        object IDictionary.this[object key]
        {
            get => _fieldBag[key];
            set => _fieldBag[key] = value;
        }

        ICollection IDictionary.Keys => _fieldBag.Keys;

        ICollection IDictionary.Values => _fieldBag.Values;

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_fieldBag).GetEnumerator();
        }

        void ICollection.CopyTo(Array array, int index)
        {
            CopyFieldsTo(array, index);
        }

        int ICollection.Count => _fieldBag.Count;

        bool ICollection.IsSynchronized => _fieldBag.IsSynchronized;

        object ICollection.SyncRoot => _fieldBag.SyncRoot;
#endif
    }
}