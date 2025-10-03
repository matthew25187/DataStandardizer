using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.File.CSV
{
    public sealed class CsvFileWriter<TRecordLine> : CsvFileIoBase<TRecordLine>, IDisposable
        where TRecordLine : CsvFileRecordLine
    {
        private static class DataItemName
        {
            internal const string ActualFieldCount = "ActualFieldCount";
            internal const string ExpectedFieldCount = "ExpectedFieldCount";
            internal const string FieldValue = "FieldValue";
        }
#if NETCOREAPP3_0_OR_GREATER
        private CsvFileHeaderLine? _headerLine;
#else
        [CanBeNull] private CsvFileHeaderLine _headerLine;
#endif
        private string[] _fieldNames = Array.Empty<string>();
        private string[] _headerLineFieldNames = Array.Empty<string>();
        private readonly bool _isInternalWriter;
        private readonly CsvFileOptions _options = new CsvFileOptions();
        private int _previousLineFieldCount;
        private readonly TextWriter _writer;

        public CsvFileWriter(Stream csvStream)
        {
            if (csvStream is null)
                throw new ArgumentNullException(nameof(csvStream));

            _writer = new StreamWriter(csvStream);
            _isInternalWriter = true;
        }

        public CsvFileWriter(Stream csvStream, CsvFileOptions options) : this(csvStream)
        {
            _options = options;

            if (_options.Encoding != null)
            {
                _writer = new StreamWriter(csvStream, _options.Encoding);
            }
        }

#if NETSTANDARD2_0_OR_GREATER||NETCOREAPP2_0_OR_GREATER
        public CsvFileWriter(string csvFilePath)
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));

            _writer = new StreamWriter(csvFilePath);
            _isInternalWriter = true;
        }

        public CsvFileWriter(string csvFilePath, CsvFileOptions options) : this(csvFilePath)
        {
            _options = options;

            if (_options.Encoding != null)
            {
                _writer = new StreamWriter(csvFilePath, false, _options.Encoding);
            }
        }
#endif

        public CsvFileWriter(TextWriter writer)
        {
            if (writer is null)
                throw new ArgumentNullException(nameof(writer));

            _writer = writer;
        }

        public CsvFileWriter(TextWriter writer, CsvFileOptions options) : this(writer)
        {
            _options = options;

            if (_options.Encoding != null)
            {
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(writer));
            }
        }

        public void Dispose()
        {
            if (_isInternalWriter)
            {
                _writer.Dispose();
            }
        }

        /// <summary>
        /// Write a line to a CSV file.
        /// </summary>
        /// <param name="csvLine">Line containing fields to be written.</param>
        /// <exception cref="CsvFileException">
        /// Invalid field name detected.
        /// </exception>
        public void WriteLine(ICsvFileLine csvLine)
        {
            // Check the line for the last non-empty field value.
            var lastFieldIndex = -1;
            if (_options.SuppressTrailingBlankFields)
            {
                lastFieldIndex = GetLastNonEmptyFieldIndex(csvLine);
            }

            // Extract the serialized field values that will comprise the CSV line.
            var rawFieldValues = new List<string>();
            switch (csvLine)
            {
                case CsvFileHeaderLine headerLine:
                {
                    _headerLine = headerLine;

                    // Extract field names from the header line.
                    var fieldIndex = 0;
                    foreach (DictionaryEntry field in csvLine)
                    {
                        // If the remaining field values are empty, stop here.
                        if (lastFieldIndex > -1 && fieldIndex++ > lastFieldIndex)
                        {
                            break;
                        }

                        // Convert the field value to a string.
                        var rawFieldValue = field.Value as string;
                        if (string.IsNullOrWhiteSpace(rawFieldValue))
                        {
                            var message = $"Field value '{field.Value}' is not a field name.";
                            var dataItems = new Dictionary<string, object>();
                            if (field.Value != null)
                            {
                                dataItems.Add(DataItemName.FieldValue, field.Value);
                            }

                            throw BuildException(message, dataItems);
                        }

                        rawFieldValues.Add(rawFieldValue);
                    }

                    // Store a copy of the header line's field names so fields on the record lines can be correctly ordered.
                    _headerLineFieldNames = rawFieldValues.ToArray();
                }
                    break;
                case TRecordLine recordLine:
                {
                    // Get a collection of the names of fields to include in the output.
                    string[] fieldNames;
                    if (_headerLineFieldNames.Length > 0)
                    {
                        fieldNames = _headerLineFieldNames;
                    }
                    else
                    {
                        if (csvLine.Count > _fieldNames.Length)
                        {
                            var csvLineFieldNames = csvLine.Keys.Cast<string>();
                            _fieldNames = _fieldNames.Concat(csvLineFieldNames.Except(_fieldNames)).ToArray();
                        }

                        fieldNames = _fieldNames;
                    }

                    var mapper = GetMapper(recordLine);
                    if (mapper.Count > 0 && _headerLineFieldNames.Length == 0)
                    {
                        fieldNames = CsvMappingService.GetSortedFieldNamesFromMapper(mapper);
                    }

                    // Check for an inconsistent field count.
                    // ref. RFC 4180§2¶4
                    if (_previousLineFieldCount > 0 && fieldNames.Length != _previousLineFieldCount)
                    {
                        if (_options.InconsistentFieldCountHandler is CsvFieldCount<TRecordLine> fieldCountHandler)
                        {
                            var context = new CsvFieldContext<TRecordLine>(_options) { HeaderLine = _headerLine, Model = recordLine };
                            fieldCountHandler(context);
                            break;
                        }

                        var message = $"Expected {_previousLineFieldCount} fields; found {fieldNames.Length} fields.";
                        var dataItems = new Dictionary<string, object> { { DataItemName.ExpectedFieldCount, _previousLineFieldCount }, { DataItemName.ActualFieldCount, fieldNames.Length } };
                        throw BuildException(message, dataItems);
                    }

                    _previousLineFieldCount = fieldNames.Length;

                    // Extract serialized field values.
                    var fieldIndex = 0;
                    foreach (var fieldName in fieldNames)
                    {
                        // If a value has not been set for the current field, add a blank value.
                        if (!csvLine.Contains(fieldName))
                        {
                            rawFieldValues.Add(string.Empty);
                            continue;
                        }

                        // If the remaining field values are empty, stop here.
                        if (lastFieldIndex > -1 && fieldIndex++ > lastFieldIndex)
                        {
                            break;
                        }

                        // Convert the field value to a string.
#if NETCOREAPP3_0_OR_GREATER
                        string? rawFieldValue = null;
#else
                        string rawFieldValue = null;
#endif
                        var propertyFieldMapping = mapper.FirstOrDefault(mapping => CsvMappingService.GetFieldNameFromMapping(mapping) == fieldName);
                        if (!string.IsNullOrEmpty(propertyFieldMapping.Key))
                        {
                            var fieldValue = GetCsvLineMappedFieldValue(recordLine, propertyFieldMapping);
                            rawFieldValue = SerializeCsvLineFieldValue(_headerLine, recordLine, _options, propertyFieldMapping, fieldValue);
                        }
                        else if (csvLine.Contains(fieldName))
                        {
                            var serializedFieldValue = _options.Culture != null && csvLine[fieldName] is IFormattable formattableFieldValue
                                ? formattableFieldValue.ToString(null, _options.Culture)
                                : csvLine[fieldName]?.ToString();
                            rawFieldValue = csvLine[fieldName] as string ?? serializedFieldValue;
                        }

                        // Apply embedded line break on field value.
                        if (_options.EmbeddedLineBreak != null && !string.IsNullOrEmpty(rawFieldValue))
                        {
                            rawFieldValue = NormalizeFieldValueLineBreaks(rawFieldValue, _options);
                        }

                        rawFieldValues.Add(rawFieldValue ?? string.Empty);
                    }
                }
                    break;
            }

            // Construct and write line to CSV output.
            var fieldValues = GetQuotedFieldValues(rawFieldValues);
            var line = string.Join(_options.FieldDelimiterCharacter.ToString(), fieldValues);
            _writer.Write(line);
            _writer.Write(_options.TerminatorLineBreak);
        }

        private int GetLastNonEmptyFieldIndex(ICsvFileLine csvLine)
        {
            var lastFieldIndex = -1;
            for (var index = csvLine.Count - 1; index >= 0 && lastFieldIndex == -1; index--)
            {
                // Check for a non-empty string, being the last field on the line.
                // N.B. Whitespace alone does not count as being "empty" as spaces are permitted.
                // ref. RFC 4180§2¶4
                if (csvLine[index] is string stringFieldValue && !string.IsNullOrEmpty(stringFieldValue))
                {
                    lastFieldIndex = index;
                }
                else if (csvLine[index] != null && !(csvLine[index] is string))
                {
                    lastFieldIndex = index;
                }
            }

            return lastFieldIndex;
        }

        private IEnumerable<string> GetQuotedFieldValues(List<string> rawFieldValues)
        {
            var fieldValues = rawFieldValues
                .Select(value =>
                {
                    var fieldValue = value;
                    if (DoesFieldValueRequireQuoting(value, _options))
                    {
                        var escapedFieldValue = fieldValue.Replace("\"", "\"\"");
                        fieldValue = string.Concat('"', escapedFieldValue, '"');
                    }

                    return fieldValue;
                });
            return fieldValues;
        }
    }
}