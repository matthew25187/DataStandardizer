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
        internal void MarkBadValueField(string fieldName)
        {
            if (_badValueFieldNames is null)
            {
                _badValueFieldNames = new HashSet<string>(StringComparer.Ordinal);
            }

            _badValueFieldNames.Add(fieldName);
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
        /// the property key.
        /// </remarks>
        protected string GetPropertyKey(string propertyName)
        {
            var propertyKey = GetCsvFieldKey(propertyName);
            var mappedFieldNames = _mappedFieldNames;
            if (mappedFieldNames is null)
            {
                return propertyKey;
            }

            if (mappedFieldNames.TryGetValue(propertyKey, out var mappedFieldName))
            {
                if (_fieldBag.ContainsKey(mappedFieldName) || !_fieldBag.ContainsKey(propertyKey))
                {
                    return mappedFieldName;
                }
            }

            return propertyKey;
        }

        /// <summary>
        /// Gets the key a mapping's property key resolves to on this line, for the writer to look a value up by when the line has no field with
        /// the mapped field name.
        /// </summary>
        internal string ResolveMappedPropertyKey(string mappingKey) => GetCsvFieldKey(mappingKey);

        private string GetCsvFieldKey(string propertyName)
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
            var pi = CsvMappableProperty.FindDeclaration(lineType, propertyName);
            return pi?.GetPropertyKey() ?? propertyName;
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
