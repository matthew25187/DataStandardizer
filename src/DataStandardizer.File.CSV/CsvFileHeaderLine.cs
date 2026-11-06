using System;
using System.Collections.Generic;

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
        /// <remarks>
        /// The names are the values of the line's fields, in position order.  A value that isn't a string gives an empty name.
        /// </remarks>
        public IReadOnlyList<string> FieldNames => DoGetFieldNames();

        private string[] DoGetFieldNames()
        {
            if (_fieldCount != GetFieldCount())
            {
                var fieldValues = FieldValues;
                var fieldNames = new string[fieldValues.Count];
                for (var fieldIndex = 0; fieldIndex < fieldNames.Length; fieldIndex++)
                {
                    fieldNames[fieldIndex] = fieldValues[fieldIndex] is string fieldName ? fieldName : string.Empty;
                }

                _fieldNames = fieldNames;
                _fieldCount = _fieldNames.Length;
            }

            return _fieldNames;
        }
    }
}
