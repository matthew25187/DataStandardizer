using System;
using System.Collections.Generic;
using System.Linq;

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
                _fieldNames = ((ICsvFileLine)this).Values
                    .Cast<object>()
                    .Select(fieldValue => fieldValue as string ?? string.Empty)
                    .ToArray();
                _fieldCount = _fieldNames.Length;
            }

            return _fieldNames;
        }
    }
}