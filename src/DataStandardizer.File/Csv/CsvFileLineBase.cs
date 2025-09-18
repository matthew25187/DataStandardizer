using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.File.Csv
{
    public abstract class CsvFileLineBase : ICsvFileLine
    {
        private readonly IOrderedDictionary _fieldBag = new OrderedDictionary();

        protected int GetFieldCount()
        {
            return _fieldBag.Count;
        }
#if NETCOREAPP3_0_OR_GREATER
        protected object? GetFieldValue(string fieldName)
        {
            if (!_fieldBag.Contains(fieldName))
            {
                return null;
            }

            return _fieldBag[fieldName];
        }

        protected void SetFieldValue(string fieldName, object? value)
        {
            if (_fieldBag.Contains(fieldName))
            {
                _fieldBag[fieldName] = value;
                return;
            }

            _fieldBag.Add(fieldName, value);
        }
#else
        [CanBeNull]
        protected object GetFieldValue(string fieldName)
        {
            if (!_fieldBag.Contains(fieldName))
            {
                return null;
            }

            return _fieldBag[fieldName];
        }

        protected void SetFieldValue(string fieldName, [CanBeNull] object value)
        {
            if (_fieldBag.Contains(fieldName))
            {
                _fieldBag[fieldName] = value;
                return;
            }

            _fieldBag.Add(fieldName, value);
        }
#endif

        protected string GetPropertyKey(string propertyName)
        {
            var pi = this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName);
            var fieldAttribute = pi?.GetCustomAttribute<CsvFieldAttribute>();
            return fieldAttribute?.FieldName ?? pi?.Name ?? propertyName;
        }

#if NETCOREAPP3_0_OR_GREATER
        void IDictionary.Add(object key, object? value)
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
        void IDictionary.Add(object key, object value)
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