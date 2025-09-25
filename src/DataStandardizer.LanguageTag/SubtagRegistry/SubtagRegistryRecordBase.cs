using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    public abstract class SubtagRegistryRecordBase : ISubtagRegistryRecord
    {
        private readonly IList<Tuple<string, object>> _fieldBag = new List<Tuple<string, object>>();

        public override string ToString()
        {
            var recordBuilder = new StringBuilder();
            foreach (var field in _fieldBag)
            {
                recordBuilder.AppendLine($"{field.Item1}: {field.Item2}");
            }

            return recordBuilder.ToString().TrimEnd();
        }

        #region Protected Methods

#if NETCOREAPP3_0_OR_GREATER
        protected T? GetPropertyValue<T>([CallerMemberName] string? propertyName = null)
#else
        [CanBeNull]
        protected T GetPropertyValue<T>([CallerMemberName] string propertyName = null)
#endif
        {
            var property = this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName ?? string.Empty);
            var fieldAttribute = property?.GetCustomAttribute<SubtagRegistryFieldAttribute>();
            var fieldName = fieldAttribute?.FieldName ?? propertyName;

#if NETCOREAPP3_0_OR_GREATER
            T? result = default;
#else
            T result = default;
#endif
            var field = _fieldBag.FirstOrDefault(f => f.Item1 == fieldName);
            if (field != null)
            {
                if (field.Item2 is T)
                {
#if NETCOREAPP3_0_OR_GREATER
                    result = (T?)field.Item2;
#else
                    result = (T)field.Item2;
#endif
                }
                else if (field.Item2 is string fieldValueString && property != null)
                {
                    var converter = TypeDescriptor.GetConverter(property.PropertyType);
#if NETCOREAPP3_0_OR_GREATER
                    result = (T?)converter.ConvertFromInvariantString(fieldValueString);
#else
                    result = (T)converter.ConvertFromInvariantString(fieldValueString);
#endif
                }
            }

            return result;
        }

#if NETCOREAPP3_0_OR_GREATER
        protected T[] GetPropertyValues<T>([CallerMemberName] string? propertyName = null)
#else
        protected T[] GetPropertyValues<T>([CallerMemberName] string propertyName = null)
#endif
        {
            var property = this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName ?? string.Empty);
            var fieldAttribute = property?.GetCustomAttribute<SubtagRegistryFieldAttribute>();
            var fieldName = fieldAttribute?.FieldName ?? propertyName;

            var fieldValues = _fieldBag
                .Where(field => field.Item1 == fieldName)
                .Select(field =>
                {
#if NETCOREAPP3_0_OR_GREATER
                    T? result = default;
#else
                    T result = default;
#endif
                    if (field.Item2 is T)
                    {
                        result = (T)field.Item2;
                    }
                    else if (field.Item2 is string fieldValueString && property != null)
                    {
                        var converter = TypeDescriptor.GetConverter(property.PropertyType);
#if NETCOREAPP3_0_OR_GREATER
                        result = (T?)converter.ConvertFromInvariantString(fieldValueString);
#else
                        result = (T)converter.ConvertFromInvariantString(fieldValueString);
#endif
                    }

                    return result;
                })
                .Where(fieldValue => fieldValue != null)
                .Cast<T>();
            return fieldValues.ToArray();
        }

        #endregion

        #region Implementation of IEnumerable

        IEnumerator<Tuple<string, object>> IEnumerable<Tuple<string, object>>.GetEnumerator()
        {
            return _fieldBag.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_fieldBag).GetEnumerator();
        }

        #endregion

        #region Implementation of ICollection<Tuple<string,object>>

        void ICollection<Tuple<string, object>>.Add(Tuple<string, object> item)
        {
            _fieldBag.Add(item);
        }

        void ICollection<Tuple<string, object>>.Clear()
        {
            _fieldBag.Clear();
        }

        bool ICollection<Tuple<string, object>>.Contains(Tuple<string, object> item)
        {
            return _fieldBag.Contains(item);
        }

        void ICollection<Tuple<string, object>>.CopyTo(Tuple<string, object>[] array, int arrayIndex)
        {
            _fieldBag.CopyTo(array, arrayIndex);
        }

        bool ICollection<Tuple<string, object>>.Remove(Tuple<string, object> item)
        {
            return _fieldBag.Remove(item);
        }

        int ICollection<Tuple<string, object>>.Count => _fieldBag.Count;

        bool ICollection<Tuple<string, object>>.IsReadOnly => _fieldBag.IsReadOnly;

        #endregion

        #region Implementation of IList<Tuple<string,object>>

        int IList<Tuple<string, object>>.IndexOf(Tuple<string, object> item)
        {
            return _fieldBag.IndexOf(item);
        }

        void IList<Tuple<string, object>>.Insert(int index, Tuple<string, object> item)
        {
            _fieldBag.Insert(index, item);
        }

        void IList<Tuple<string, object>>.RemoveAt(int index)
        {
            _fieldBag.RemoveAt(index);
        }

        Tuple<string, object> IList<Tuple<string, object>>.this[int index]
        {
            get => _fieldBag[index];
            set => _fieldBag[index] = value;
        }

        #endregion
    }
}