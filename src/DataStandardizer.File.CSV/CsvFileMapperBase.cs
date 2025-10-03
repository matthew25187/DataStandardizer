using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.CSV
{
    public abstract class CsvFileMapperBase<TRecordLine> : ICsvFileMapper where TRecordLine : CsvFileRecordLine
    {
        private readonly IDictionary<string, CsvFieldMapping> _propertyFieldMappings = new Dictionary<string, CsvFieldMapping>();
        private readonly IReadOnlyDictionary<string, CsvFieldMapping> _propertyFieldMappingsWrapper;

        protected CsvFileMapperBase()
        {
            _propertyFieldMappingsWrapper = new ReadOnlyDictionary<string, CsvFieldMapping>(_propertyFieldMappings);
        }

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