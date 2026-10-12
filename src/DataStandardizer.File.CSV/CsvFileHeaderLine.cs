using System;
using System.Collections.Generic;
#if !NET9_0_OR_GREATER
using System.Linq;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Represents a header line from a CSV file.
    /// </summary>
    public sealed class CsvFileHeaderLine : CsvFileLineBase
    {
        private int _fieldCount;
        private string[] _fieldNames = Array.Empty<string>();

        /// <summary>
        /// Gets the names of the fields on the line.
        /// </summary>
        public IReadOnlyList<string> FieldNames => DoGetFieldNames();

        private string[] DoGetFieldNames()
        {
            if (_fieldCount != GetFieldCount())
            {
#if NET9_0_OR_GREATER
                var fieldValues = FieldValues;
                var fieldNames = new string[fieldValues.Count];
                for (var fieldIndex = 0; fieldIndex < fieldNames.Length; fieldIndex++)
                {
                    fieldNames[fieldIndex] = fieldValues[fieldIndex] as string ?? string.Empty;
                }

                _fieldNames = fieldNames;
#else
                _fieldNames = ((ICsvFileLine)this).Values
                    .Cast<object>()
                    .Select(fieldValue => fieldValue as string ?? string.Empty)
                    .ToArray();
#endif
                _fieldCount = _fieldNames.Length;
            }

            return _fieldNames;
        }
    }
}