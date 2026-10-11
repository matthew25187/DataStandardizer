using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Reflection;
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

        private readonly IOrderedDictionary _fieldBag = new OrderedDictionary();

        protected int GetFieldCount()
        {
            return _fieldBag.Count;
        }
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
            _fieldBag.CopyTo(array, index);
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
            _fieldBag.CopyTo(array, index);
        }

        int ICollection.Count => _fieldBag.Count;

        bool ICollection.IsSynchronized => _fieldBag.IsSynchronized;

        object ICollection.SyncRoot => _fieldBag.SyncRoot;
#endif
    }
}