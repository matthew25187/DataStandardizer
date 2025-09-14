using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Collection of mappings from model object properties to CSV fields.
    /// </summary>
    public class CsvFileMapper : ICsvFileMapper
    {
        private readonly IReadOnlyDictionary<string, CsvFieldMapping> _fieldMappingsWrapper;

        internal CsvFileMapper(IDictionary<string, CsvFieldMapping> fieldMappings)
        {
            _fieldMappingsWrapper = new ReadOnlyDictionary<string, CsvFieldMapping>(fieldMappings);
        }

        IEnumerator<KeyValuePair<string, CsvFieldMapping>> IEnumerable<KeyValuePair<string, CsvFieldMapping>>.GetEnumerator()
        {
            return _fieldMappingsWrapper.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_fieldMappingsWrapper).GetEnumerator();
        }

        int IReadOnlyCollection<KeyValuePair<string, CsvFieldMapping>>.Count => _fieldMappingsWrapper.Count;

        bool IReadOnlyDictionary<string, CsvFieldMapping>.ContainsKey(string key)
        {
            return _fieldMappingsWrapper.ContainsKey(key);
        }

#if NETCOREAPP3_0_OR_GREATER
        bool IReadOnlyDictionary<string, CsvFieldMapping>.TryGetValue(string key, [MaybeNullWhen(false)] out CsvFieldMapping value)
        {
            return _fieldMappingsWrapper.TryGetValue(key, out value);
        } 
#else
        bool IReadOnlyDictionary<string, CsvFieldMapping>.TryGetValue(string key, out CsvFieldMapping value)
        {
            return _fieldMappingsWrapper.TryGetValue(key, out value);
        } 
#endif

        CsvFieldMapping IReadOnlyDictionary<string, CsvFieldMapping>.this[string key] => _fieldMappingsWrapper[key];

        IEnumerable<string> IReadOnlyDictionary<string, CsvFieldMapping>.Keys => _fieldMappingsWrapper.Keys;

        IEnumerable<CsvFieldMapping> IReadOnlyDictionary<string, CsvFieldMapping>.Values => _fieldMappingsWrapper.Values;
    }
}