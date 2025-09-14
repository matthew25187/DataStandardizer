using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.Csv
{
    public abstract class CsvFileMapperBase<TRecordLine> : ICsvFileMapper where TRecordLine : CsvFileRecordLine
    {
        private readonly IDictionary<string, CsvFieldMapping> _propertyMappings = new Dictionary<string, CsvFieldMapping>();
        private readonly IReadOnlyDictionary<string, CsvFieldMapping> _propertyMappingsWrapper;

        protected CsvFileMapperBase()
        {
            _propertyMappingsWrapper = new ReadOnlyDictionary<string, CsvFieldMapping>(_propertyMappings);
        }

        protected CsvFileMappingBuilder<TRecordLine> Map()
        {
            var fieldMappingBuilder = new CsvFileMappingBuilder<TRecordLine>(AddFieldMapping);
            return fieldMappingBuilder;
        }

        private void AddFieldMapping(string propertyName, CsvFieldMapping fieldMapping)
        {
            _propertyMappings.Add(propertyName, fieldMapping);
        }

        IEnumerator<KeyValuePair<string, CsvFieldMapping>> IEnumerable<KeyValuePair<string, CsvFieldMapping>>.GetEnumerator()
        {
            return _propertyMappingsWrapper.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_propertyMappingsWrapper).GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, CsvFieldMapping>>.Count => _propertyMappingsWrapper.Count;

        bool IReadOnlyDictionary<string, CsvFieldMapping>.ContainsKey(string key)
        {
            return _propertyMappingsWrapper.ContainsKey(key);
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IReadOnlyDictionary<string, CsvFieldMapping>.TryGetValue(string key, [MaybeNullWhen(false)] out CsvFieldMapping value)
        {
            return _propertyMappingsWrapper.TryGetValue(key, out value);
        } 
#else
        bool IReadOnlyDictionary<string, CsvFieldMapping>.TryGetValue(string key, out CsvFieldMapping value)
        {
            return _propertyMappingsWrapper.TryGetValue(key, out value);
        } 
#endif

        CsvFieldMapping IReadOnlyDictionary<string, CsvFieldMapping>.this[string key] => _propertyMappingsWrapper[key];

        IEnumerable<string> IReadOnlyDictionary<string, CsvFieldMapping>.Keys => _propertyMappingsWrapper.Keys;

        IEnumerable<CsvFieldMapping> IReadOnlyDictionary<string, CsvFieldMapping>.Values => _propertyMappingsWrapper.Values;
    }
}