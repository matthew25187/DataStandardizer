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
        // Field keys resolved from property names, keyed by runtime line type, or custom model type, then property name.  The resolution
        // depends only on type metadata, so it is computed once per (type, property name) for the process lifetime.
        // Nested rather than keyed by a tuple, so the .NET Standard 1.x build needs no System.ValueTuple reference.
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, string>> PropertyKeyCache =
            new ConcurrentDictionary<Type, ConcurrentDictionary<string, string>>();

        /// <summary>
        /// Removes every cached property key.  Called by <see cref="CsvMapperCache.Clear"/>.
        /// </summary>
        internal static void ClearPropertyKeys() => PropertyKeyCache.Clear();

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
            ForgetFieldValueOrigin(fieldName);
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
            ForgetFieldValueOrigin(fieldName);
        }
#endif

        // Stands in a slot for a property that reads as its default value: its mapping's converted value is null, the reader skipped the mapping,
        // or the reader called the bad-value handler for its value.  An empty slot means the property reads the field's value.
        private static readonly object SharedFieldDefault = new object();

        // The mappings that share a field with another mapping, and the value each one's property read, by slot; null if the line was read without
        // such a mapping, or no value has been kept yet.  The line holds one value per field, so properties of different types can't all read
        // theirs from it.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvSharedFieldLayout? _sharedFieldLayout;
        private object?[]? _sharedFieldValues;
#else
        private CsvSharedFieldLayout _sharedFieldLayout;
        private object[] _sharedFieldValues;
#endif

        /// <summary>
        /// Sets the layout of the mappings that share a field, which the reader's values are kept by.  Set by the reader from its field plan.
        /// </summary>
        /// <param name="sharedFieldLayout">The layout, or <c>null</c> if no mapping shares its field.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal void SetSharedFieldLayout(CsvSharedFieldLayout? sharedFieldLayout)
#else
        internal void SetSharedFieldLayout([AllowNull] CsvSharedFieldLayout sharedFieldLayout)
#endif
        {
            _sharedFieldLayout = sharedFieldLayout;
            _sharedFieldValues = null;
        }

        /// <summary>
        /// Sets the value of a field to a mapping's converted value, as the reader does.  When the field is shared with another mapping, the
        /// value is also kept for the mapping's property, which then reads it whatever value the field is left with.
        /// </summary>
        /// <param name="fieldName">The name of the field the mapping resolved to.</param>
        /// <param name="value">The mapping's converted value.</param>
        /// <param name="sharedFieldSlot">The mapping's slot in the shared field layout, or <c>-1</c> if no other mapping resolves to the field.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal void SetMappedFieldValue(string fieldName, object? value, int sharedFieldSlot)
#else
        internal void SetMappedFieldValue(string fieldName, [AllowNull] object value, int sharedFieldSlot)
#endif
        {
            _fieldBag[fieldName] = value;
            _propertyValueKeys?.Remove(fieldName);
            if (sharedFieldSlot >= 0)
            {
                SetSharedFieldValue(sharedFieldSlot, value ?? SharedFieldDefault);
            }
        }

        private void SetSharedFieldValue(int sharedFieldSlot, object value)
        {
            var sharedFieldValues = _sharedFieldValues;
            if (sharedFieldValues is null)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                sharedFieldValues = new object?[_sharedFieldLayout!.FieldNames.Length];
#else
                sharedFieldValues = new object[_sharedFieldLayout.FieldNames.Length];
#endif
                _sharedFieldValues = sharedFieldValues;
            }

            sharedFieldValues[sharedFieldSlot] = value;
        }

        /// <summary>
        /// Gets the value that a property read from a field it shares with another property's mapping.
        /// </summary>
        /// <param name="propertyKey">The key of the property.</param>
        /// <param name="fieldName">The name of the field the property reads and writes.</param>
        /// <param name="value">The property's value, or <c>null</c> if it reads as its default value.</param>
        /// <returns><c>true</c> if the property has its own value; <c>false</c> if it reads the field's value.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private protected bool TryGetSharedFieldValue(string propertyKey, string fieldName, out object? value)
#else
        private protected bool TryGetSharedFieldValue(string propertyKey, string fieldName, [MaybeNull] out object value)
#endif
        {
            var sharedFieldValues = _sharedFieldValues;
            var sharedFieldLayout = _sharedFieldLayout;
            if (sharedFieldValues != null
                && sharedFieldLayout != null
                && sharedFieldLayout.SlotsByPropertyKey.TryGetValue(propertyKey, out var sharedFieldSlot)
                && sharedFieldValues[sharedFieldSlot] is object sharedFieldValue
                && string.Equals(sharedFieldLayout.FieldNames[sharedFieldSlot], fieldName, StringComparison.Ordinal))
            {
                value = ReferenceEquals(sharedFieldValue, SharedFieldDefault) ? null : sharedFieldValue;
                return true;
            }

            value = null;
            return false;
        }

        /// <summary>
        /// Records that the reader skipped a mapping of a field shared with other mappings, because the field wasn't read from the file, so the
        /// mapping's property reads as its default value rather than another mapping's value.
        /// </summary>
        internal void MarkSkippedSharedField(int sharedFieldSlot)
        {
            SetSharedFieldValue(sharedFieldSlot, SharedFieldDefault);
        }

        // The key of the property that set each field's value through its setter, by field name; null if no property has set a value.  A field
        // whose value was read, or set through the line, has none.  Field names and property keys share one key space on the line, so this is how
        // the writer tells a property's value held under its key from a field's value with the same name.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private Dictionary<string, string>? _propertyValueKeys;
#else
        private Dictionary<string, string> _propertyValueKeys;
#endif

        /// <summary>
        /// Sets a field's value through a property, recording the key of the property that set it.
        /// </summary>
        /// <param name="fieldName">The key of the field the property writes.</param>
        /// <param name="propertyKey">The property's key.</param>
        /// <param name="value">The value.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private protected void SetPropertyFieldValue(string fieldName, string propertyKey, object? value)
#else
        private protected void SetPropertyFieldValue(string fieldName, string propertyKey, [AllowNull] object value)
#endif
        {
            SetFieldValue(fieldName, value);
            if (_propertyValueKeys is null)
            {
                _propertyValueKeys = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            _propertyValueKeys[fieldName] = propertyKey;
        }

        /// <summary>
        /// Gets the key of the property that set a field's value through its setter, if a property set it.
        /// </summary>
        internal bool TryGetPropertyValueKey(string fieldName, out string propertyKey)
        {
            if (_propertyValueKeys != null && _propertyValueKeys.TryGetValue(fieldName, out var key))
            {
                propertyKey = key;
                return true;
            }

            propertyKey = string.Empty;
            return false;
        }

        /// <summary>
        /// Forgets where a field's value came from, once the field's value is changed or removed through the line.
        /// </summary>
        private void ForgetFieldValueOrigin(string fieldName)
        {
            _propertyValueKeys?.Remove(fieldName);
            ForgetSharedFieldValues(fieldName);
        }

        /// <summary>
        /// Forgets the values that properties read from a field they share, once the field's value is changed, so they all read the new value.
        /// </summary>
        private void ForgetSharedFieldValues(string fieldName)
        {
            var sharedFieldValues = _sharedFieldValues;
            if (sharedFieldValues is null)
            {
                return;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            var fieldNames = _sharedFieldLayout!.FieldNames;
#else
            var fieldNames = _sharedFieldLayout.FieldNames;
#endif
            for (var sharedFieldSlot = 0; sharedFieldSlot < fieldNames.Length; sharedFieldSlot++)
            {
                if (string.Equals(fieldNames[sharedFieldSlot], fieldName, StringComparison.Ordinal))
                {
                    sharedFieldValues[sharedFieldSlot] = null;
                }
            }
        }

        // The field names of the mappings that map a property to a field with another name, keyed by property key, or null if
        // the line was not filled through such a mapping.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private Dictionary<string, string>? _mappedFieldNames;
#else
        private Dictionary<string, string> _mappedFieldNames;
#endif

        // The names of the fields whose values the reader called the bad-value handler for, or null if there are none.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private HashSet<string>? _badValueFieldNames;
#else
        private HashSet<string> _badValueFieldNames;
#endif

        /// <summary>
        /// Records that the reader called the bad-value handler for a field's value, so a property of another type than the value reads as its
        /// default value instead of throwing.  The field keeps its value, so a line read and then written gives back the field's text.
        /// </summary>
        /// <param name="fieldName">The name of the field.</param>
        /// <param name="sharedFieldSlot">The mapping's slot in the shared field layout, or <c>-1</c> if no other mapping resolves to the field.</param>
        internal void MarkBadValueField(string fieldName, int sharedFieldSlot)
        {
            if (_badValueFieldNames is null)
            {
                _badValueFieldNames = new HashSet<string>(StringComparer.Ordinal);
            }

            _badValueFieldNames.Add(fieldName);
            if (sharedFieldSlot >= 0)
            {
                SetSharedFieldValue(sharedFieldSlot, SharedFieldDefault);
            }
        }

        /// <summary>
        /// Gets whether the reader called the bad-value handler for a field's value.
        /// </summary>
        internal bool IsBadValueField(string fieldName)
        {
            return _badValueFieldNames != null && _badValueFieldNames.Contains(fieldName);
        }

        /// <summary>
        /// Sets the field names that the line's properties are mapped to, so a property mapped to a field with another name
        /// reads and writes that field.  Set by the reader, and by <see cref="CsvExtensions.ToCsvLine"/>, from their mapper.
        /// </summary>
        /// <param name="mappedFieldNames">The field names keyed by property key, or <c>null</c> if no property is mapped to a field with another
        /// name.  <see cref="CsvExtensions.ToCsvLine"/> takes them from <see cref="CsvMappingService.GetMappedFieldNames"/>; the reader works them
        /// out for each field count, so a property mapped by index has the name of the field at that position.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        internal void SetMappedFieldNames(Dictionary<string, string>? mappedFieldNames)
#else
        internal void SetMappedFieldNames([AllowNull] Dictionary<string, string> mappedFieldNames)
#endif
        {
            _mappedFieldNames = mappedFieldNames;
        }

        /// <summary>
        /// Gets the key of the field that a property reads and writes.
        /// </summary>
        /// <remarks>
        /// The key is the name given by <see cref="CsvFieldAttribute"/>, else the property name.  On a line filled through a mapping that maps
        /// the property to a field with another name, it is that field's name instead, unless the line has no such field but does have one with
        /// the property key that isn't the field another property is mapped to.
        /// </remarks>
        protected string GetPropertyKey(string propertyName)
        {
            return GetMappedFieldName(GetCsvFieldKey(propertyName));
        }

        /// <summary>
        /// Gets the key of the field that a property reads and writes, from the property's key, as <see cref="GetPropertyKey"/> does.
        /// </summary>
        private protected string GetMappedFieldName(string propertyKey)
        {
            var mappedFieldNames = _mappedFieldNames;
            if (mappedFieldNames is null)
            {
                return propertyKey;
            }

            if (mappedFieldNames.TryGetValue(propertyKey, out var mappedFieldName))
            {
                // A field named like the property key is the property's only if it isn't the field of another mapping, as field names and
                // property keys share one key space on the line.
                if (_fieldBag.ContainsKey(mappedFieldName) || !_fieldBag.ContainsKey(propertyKey) || IsOtherMappedFieldName(mappedFieldNames, propertyKey))
                {
                    return mappedFieldName;
                }
            }

            return propertyKey;
        }

        /// <summary>
        /// Gets whether a property key is the name of the field that another property is mapped to.
        /// </summary>
        private static bool IsOtherMappedFieldName(Dictionary<string, string> mappedFieldNames, string propertyKey)
        {
            foreach (var mappedFieldName in mappedFieldNames)
            {
                if (string.Equals(mappedFieldName.Value, propertyKey, StringComparison.Ordinal)
                    && !string.Equals(mappedFieldName.Key, propertyKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private protected string GetCsvFieldKey(string propertyName) => GetCachedPropertyKey(this.GetType(), propertyName);

        /// <summary>
        /// Gets the key of a property of a record line or custom model type: the name given by <see cref="CsvFieldAttribute"/>, else the property
        /// name.  A name that isn't a property of the type is its own key.
        /// </summary>
        /// <remarks>
        /// A mapping built with <see cref="CsvFileMappingBuilder{TModel}"/> is keyed by this too, so it agrees with the line's properties.
        /// </remarks>
        internal static string GetCachedPropertyKey(Type type, string propertyName)
        {
            if (!PropertyKeyCache.TryGetValue(type, out var propertyKeys))
            {
                propertyKeys = PropertyKeyCache.GetOrAdd(type, _ => new ConcurrentDictionary<string, string>(StringComparer.Ordinal));
            }

            if (propertyKeys.TryGetValue(propertyName, out var propertyKey))
            {
                return propertyKey;
            }

            return AddPropertyKey(propertyKeys, type, propertyName);
        }

        // Kept out of GetCachedPropertyKey so the closure over type is only allocated on a cache miss.
        private static string AddPropertyKey(ConcurrentDictionary<string, string> propertyKeys, Type type, string propertyName)
        {
            return propertyKeys.GetOrAdd(propertyName, name => ResolvePropertyKey(type, name));
        }

        private static string ResolvePropertyKey(Type type, string propertyName)
        {
            var pi = CsvMappableProperty.FindDeclaration(type, propertyName);
            return pi?.GetPropertyKey() ?? propertyName;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <inheritdoc cref="ICsvFileLine.Add"/>
        public void Add(string fieldName, object? value)
        {
            _fieldBag.Add(fieldName, value);
            ForgetFieldValueOrigin(fieldName);
        }

        object? ICsvFileLine.this[int index]
        {
            get => _fieldBag.GetAt(index).Value;
            set
            {
                ForgetFieldValueOrigin(_fieldBag.GetAt(index).Key);
                _fieldBag.SetAt(index, value);
            }
        }

        object? ICsvFileLine.this[string fieldName]
        {
            get => _fieldBag.TryGetValue(fieldName, out var value) ? value : null;
            set => SetFieldValue(fieldName, value);
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
            ForgetFieldValueOrigin(fieldName);
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
            ForgetFieldValueOrigin(fieldName);
        }

        [MaybeNull]
        object ICsvFileLine.this[int index]
        {
            get => _fieldBag.GetAt(index).Value;
            set
            {
                ForgetFieldValueOrigin(_fieldBag.GetAt(index).Key);
                _fieldBag.SetAt(index, value);
            }
        }

        [MaybeNull]
        object ICsvFileLine.this[string fieldName]
        {
            get => _fieldBag.TryGetValue(fieldName, out var value) ? value : null;
            set => SetFieldValue(fieldName, value);
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
            ForgetFieldValueOrigin(fieldName);
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
            ForgetFieldValueOrigin(fieldName);
            return _fieldBag.Remove(fieldName);
        }

        void ICsvFileLine.RemoveAt(int index)
        {
            ForgetFieldValueOrigin(_fieldBag.GetAt(index).Key);
            _fieldBag.RemoveAt(index);
        }

        void ICsvFileLine.Clear()
        {
            _sharedFieldValues = null;
            _propertyValueKeys = null;
            _fieldBag.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _fieldBag.GetEnumerator();
        }
    }
}
