using System;
using System.Collections.Generic;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Represents a header line from a CSV file.
    /// </summary>
    public sealed class CsvFileHeaderLine : CsvFileLineBase
    {
        private string[] _fieldNames = Array.Empty<string>();

        /// <summary>
        /// Gets the names of the fields on the line.
        /// </summary>
        /// <remarks>
        /// The names are the values of the line's fields, in position order.  A value that isn't a string gives an empty name.
        /// The list is a snapshot.  Reading the property again returns the same list while the names are unchanged, and a new list
        /// once a field has been added, removed or given a different value.
        /// </remarks>
        public IReadOnlyList<string> FieldNames => DoGetFieldNames();

        private string[] DoGetFieldNames()
        {
            // The names are kept while they still match the values, so that reading them again returns the same list.
            var fieldValues = FieldValues;
            if (!HasFieldNames(fieldValues))
            {
                var fieldNames = new string[fieldValues.Count];
                for (var fieldIndex = 0; fieldIndex < fieldNames.Length; fieldIndex++)
                {
                    fieldNames[fieldIndex] = ToFieldName(fieldValues[fieldIndex]);
                }

                _fieldNames = fieldNames;
            }

            return _fieldNames;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private bool HasFieldNames(IReadOnlyList<object?> fieldValues)
#else
        private bool HasFieldNames(IReadOnlyList<object> fieldValues)
#endif
        {
            if (_fieldNames.Length != fieldValues.Count)
            {
                return false;
            }

            for (var fieldIndex = 0; fieldIndex < _fieldNames.Length; fieldIndex++)
            {
                if (!string.Equals(_fieldNames[fieldIndex], ToFieldName(fieldValues[fieldIndex]), StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string ToFieldName(object? fieldValue) => fieldValue is string fieldName ? fieldName : string.Empty;
#else
        private static string ToFieldName(object fieldValue) => fieldValue is string fieldName ? fieldName : string.Empty;
#endif
    }
}
