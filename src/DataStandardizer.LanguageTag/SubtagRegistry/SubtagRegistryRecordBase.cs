using System;
using System.Collections;
#if !NETSTANDARD1_0
using System.Collections.Concurrent;
#endif
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
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
        private const string DateFormat = "yyyy-MM-dd";

        /// <summary>
        /// The field name and value conversion of each record property, resolved once per record type and property name.
        /// </summary>
#if NETSTANDARD1_0
        private static readonly Dictionary<FieldBindingKey, FieldBinding> FieldBindings = new Dictionary<FieldBindingKey, FieldBinding>();
#else
        private static readonly ConcurrentDictionary<FieldBindingKey, FieldBinding> FieldBindings = new ConcurrentDictionary<FieldBindingKey, FieldBinding>();
#endif

        private readonly List<Tuple<string, object>> _fieldBag = new List<Tuple<string, object>>();

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
            var binding = GetFieldBinding(propertyName);

            for (var index = 0; index < _fieldBag.Count; index++)
            {
                var field = _fieldBag[index];
                if (field.Item1 == binding.FieldName) return ConvertFieldValue<T>(binding, field.Item2);
            }

            return default;
        }

#if NETCOREAPP3_0_OR_GREATER
        protected T[] GetPropertyValues<T>([CallerMemberName] string? propertyName = null)
#else
        protected T[] GetPropertyValues<T>([CallerMemberName] string propertyName = null)
#endif
        {
            var binding = GetFieldBinding(propertyName);

            var count = 0;
            for (var index = 0; index < _fieldBag.Count; index++)
            {
                if (_fieldBag[index].Item1 == binding.FieldName) count++;
            }

            if (count == 0) return EmptyArray<T>();

            var fieldValues = new T[count];
            var valueCount = 0;
            for (var index = 0; index < _fieldBag.Count; index++)
            {
                var field = _fieldBag[index];
                if (field.Item1 != binding.FieldName) continue;

                var fieldValue = ConvertFieldValue<T>(binding, field.Item2);
                if (fieldValue != null) fieldValues[valueCount++] = fieldValue;
            }

            if (valueCount < count) Array.Resize(ref fieldValues, valueCount);
            return fieldValues;
        }

        #endregion

        #region Private Methods

#if NETCOREAPP3_0_OR_GREATER
        private FieldBinding GetFieldBinding(string? propertyName)
#else
        [NotNull]
        private FieldBinding GetFieldBinding([CanBeNull] string propertyName)
#endif
        {
            var key = new FieldBindingKey(this.GetType(), propertyName ?? string.Empty);

#if NETSTANDARD1_0
            lock (FieldBindings)
            {
                if (!FieldBindings.TryGetValue(key, out var binding))
                {
                    binding = FieldBinding.Create(key);
                    FieldBindings.Add(key, binding);
                }

                return binding;
            }
#else
            // TryGetValue first, so that reading a cached binding allocates no delegate on targets whose compiler doesn't cache method group conversions
            return FieldBindings.TryGetValue(key, out var binding) ? binding : FieldBindings.GetOrAdd(key, FieldBinding.Create);
#endif
        }

#if NETCOREAPP3_0_OR_GREATER
        private static T? ConvertFieldValue<T>(FieldBinding binding, object fieldValue)
#else
        [CanBeNull]
        private static T ConvertFieldValue<T>([NotNull] FieldBinding binding, [NotNull] object fieldValue)
#endif
        {
            if (fieldValue is T value) return value;
            if (!(fieldValue is string fieldValueString)) return default;

            if (binding.IsDate)
            {
                var date = DateTime.ParseExact(fieldValueString, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);

                // the JIT removes the boxing from these casts when T is a value type
                if (typeof(T) == typeof(DateTime)) return (T)(object)date;
                if (typeof(T) == typeof(DateTime?)) return (T)(object)(DateTime?)date;
                return (T)(object)date;
            }

            if (binding.Converter is null) return default;

#if NETCOREAPP3_0_OR_GREATER
            return (T?)binding.Converter.ConvertFromInvariantString(fieldValueString);
#else
            return (T)binding.Converter.ConvertFromInvariantString(fieldValueString);
#endif
        }

        private static T[] EmptyArray<T>()
        {
#if NETSTANDARD1_0
            return new T[0];
#else
            return Array.Empty<T>();
#endif
        }

        #endregion

        #region Nested Types

        private readonly struct FieldBindingKey : IEquatable<FieldBindingKey>
        {
#if NETCOREAPP3_0_OR_GREATER
            internal FieldBindingKey(Type recordType, string propertyName)
#else
            internal FieldBindingKey([NotNull] Type recordType, [NotNull] string propertyName)
#endif
            {
                RecordType = recordType;
                PropertyName = propertyName;
            }

            internal Type RecordType { get; }

            internal string PropertyName { get; }

            public bool Equals(FieldBindingKey other)
            {
                return RecordType == other.RecordType && string.Equals(PropertyName, other.PropertyName, StringComparison.Ordinal);
            }

#if NETCOREAPP3_0_OR_GREATER
            public override bool Equals(object? obj)
#else
            public override bool Equals(object obj)
#endif
            {
                return obj is FieldBindingKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (RecordType.GetHashCode() * 397) ^ StringComparer.Ordinal.GetHashCode(PropertyName);
                }
            }
        }

        /// <summary>
        /// The field a record property reads, and how the field's string is converted to the property's type.
        /// </summary>
        /// <remarks>
        /// The property is resolved through the type hierarchy, so a property inherited from a base record class is found with its <see cref="SubtagRegistryFieldAttribute"/> (#242).
        /// </remarks>
        private sealed class FieldBinding
        {
#if NETCOREAPP3_0_OR_GREATER
            private FieldBinding(string fieldName, bool isDate, TypeConverter? converter)
#else
            private FieldBinding([NotNull] string fieldName, bool isDate, [CanBeNull] TypeConverter converter)
#endif
            {
                FieldName = fieldName;
                IsDate = isDate;
                Converter = converter;
            }

            internal string FieldName { get; }

            /// <summary>
            /// Gets whether the property is a <see cref="DateTime"/>, or a nullable one, read from a registry date (<c>yyyy-MM-dd</c>, ref. RFC 5646 §3.1.2).
            /// </summary>
            internal bool IsDate { get; }

            /// <summary>
            /// Gets the converter for a property of any other type, or <see langword="null"/> when there is no such property.
            /// </summary>
#if NETCOREAPP3_0_OR_GREATER
            internal TypeConverter? Converter { get; }
#else
            [CanBeNull]
            internal TypeConverter Converter { get; }
#endif

            internal static FieldBinding Create(FieldBindingKey key)
            {
                var property = key.RecordType.GetRuntimeProperty(key.PropertyName);
                if (property is null) return new FieldBinding(key.PropertyName, false, null);

                var fieldName = property.GetCustomAttribute<SubtagRegistryFieldAttribute>()?.FieldName ?? key.PropertyName;
                var propertyType = property.PropertyType;
                if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?)) return new FieldBinding(fieldName, true, null);

                return new FieldBinding(fieldName, false, TypeDescriptor.GetConverter(propertyType));
            }
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

        bool ICollection<Tuple<string, object>>.IsReadOnly => ((ICollection<Tuple<string, object>>)_fieldBag).IsReadOnly;

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