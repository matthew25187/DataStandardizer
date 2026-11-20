using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// The base of a mapper that maps the properties of a record line to CSV fields.  A derived class declares its mappings in its constructor
    /// with <see cref="Map"/>.
    /// </summary>
    /// <typeparam name="TRecordLine">The type of the record line.</typeparam>
    public abstract class CsvFileMapperBase<TRecordLine> : ICsvFileMapper where TRecordLine : CsvFileRecordLine
    {
        private readonly IDictionary<string, CsvFieldMapping> _propertyFieldMappings = new Dictionary<string, CsvFieldMapping>();
        private readonly IReadOnlyDictionary<string, CsvFieldMapping> _propertyFieldMappingsWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CsvFileMapperBase{TRecordLine}"/> class with no mappings.
        /// </summary>
        protected CsvFileMapperBase()
        {
            _propertyFieldMappingsWrapper = new ReadOnlyDictionary<string, CsvFieldMapping>(_propertyFieldMappings);
        }

        /// <summary>
        /// Starts a mapping of a property of the record line to a CSV field.  The mapping is added to the mapper when its property is chosen with
        /// <see cref="CsvFileMappingBuilder{TModel}.Property{T}"/>.
        /// </summary>
        /// <returns>A builder that chooses the property to map.</returns>
        protected CsvFileMappingBuilder<TRecordLine> Map()
        {
            var fieldMappingBuilder = new CsvFileMappingBuilder<TRecordLine>(AddFieldMapping);
            return fieldMappingBuilder;
        }

        private void AddFieldMapping(string propertyName, CsvFieldMapping fieldMapping)
        {
            _propertyFieldMappings.Add(propertyName, fieldMapping);
        }

        IEnumerator<KeyValuePair<string, CsvFieldMapping>> IEnumerable<KeyValuePair<string, CsvFieldMapping>>.GetEnumerator()
        {
            return _propertyFieldMappingsWrapper.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_propertyFieldMappingsWrapper).GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, CsvFieldMapping>>.Count => _propertyFieldMappingsWrapper.Count;

        bool IReadOnlyDictionary<string, CsvFieldMapping>.ContainsKey(string key)
        {
            return _propertyFieldMappingsWrapper.ContainsKey(key);
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IReadOnlyDictionary<string, CsvFieldMapping>.TryGetValue(string key, [MaybeNullWhen(false)] out CsvFieldMapping value)
        {
            return _propertyFieldMappingsWrapper.TryGetValue(key, out value);
        } 
#else
        bool IReadOnlyDictionary<string, CsvFieldMapping>.TryGetValue(string key, out CsvFieldMapping value)
        {
            return _propertyFieldMappingsWrapper.TryGetValue(key, out value);
        } 
#endif

        CsvFieldMapping IReadOnlyDictionary<string, CsvFieldMapping>.this[string key] => _propertyFieldMappingsWrapper[key];

        IEnumerable<string> IReadOnlyDictionary<string, CsvFieldMapping>.Keys => _propertyFieldMappingsWrapper.Keys;

        IEnumerable<CsvFieldMapping> IReadOnlyDictionary<string, CsvFieldMapping>.Values => _propertyFieldMappingsWrapper.Values;
    }
}