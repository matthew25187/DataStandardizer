using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
#if NETSTANDARD
using JetBrains.Annotations;
#endif

namespace DataStandardizer.File.Csv
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to CSV data.
    /// </summary>
    public class CsvFileReader<TRecordLine> : CsvFileIoBase<TRecordLine>, IDisposable
        where TRecordLine : CsvFileRecordLine, new()
    {
        private static class DataItemName
        {
            internal const string ActualFieldCount = "ActualFieldCount";
            internal const string ExpectedFieldCount = "ExpectedFieldCount";
            internal const string FieldIndex = "FieldIndex";
            internal const string FieldName = "FieldName";
            internal const string FieldNames = "FieldNames";
            internal const string FieldValue = "FieldValue";
            internal const string PropertyName = "Property";
        }

        private static class ErrorMessage
        {
            internal const string FieldCountMismatchTemplate = "Found {0} of {1} expected fields.";
        }
#if NETCOREAPP3_0_OR_GREATER
        private CsvContext? _context;
        private IEnumerable<IList<string>>? _csvSource;
        private CsvFileHeaderLine? _headerLine;
#else
        [CanBeNull] private CsvContext _context;
        [CanBeNull] private IEnumerable<IList<string>> _csvSource;
        [CanBeNull] private CsvFileHeaderLine _headerLine;
#endif
        private readonly bool _isInternalReader;
        private int _expectedFieldCount;
        private readonly CsvFileOptions _options = new CsvFileOptions();
        private readonly TextReader _reader;

        public CsvFileReader(Stream csvStream)
        {
            if (csvStream is null)
                throw new ArgumentNullException(nameof(csvStream));
            if (!csvStream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", nameof(csvStream));

            _reader = new StreamReader(csvStream);
            _isInternalReader = true;
        }

        public CsvFileReader(Stream csvStream, CsvFileOptions options) : this(csvStream)
        {
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            _options = options;

            if (_options.Encoding != null)
            {
                _reader = new StreamReader(csvStream, _options.Encoding);
            }
        }

#if NETSTANDARD2_0_OR_GREATER||NETCOREAPP2_0_OR_GREATER
        public CsvFileReader(string csvFilePath)
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));

            _reader = new StreamReader(csvFilePath);
        }

        public CsvFileReader(string csvFilePath, CsvFileOptions options) : this(csvFilePath)
        {
            _options = options;
        }
#endif

        public CsvFileReader(TextReader reader)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));

            _reader = reader;
        }

        public CsvFileReader(TextReader reader, CsvFileOptions options) : this(reader)
        {
            _options = options;

            if (_options.Encoding != null)
            {
                throw new ArgumentException("Unable to apply an encoding to an existing reader.  Specify the encoding when creating the reader.", nameof(options));
            }
        }

        public void Dispose()
        {
            if (_isInternalReader)
            {
                _reader.Dispose();
            }

            _csvSource = null;
        }

        /// <summary>
        /// Reads a line from the CSV file and returns the data as a CSV line object.
        /// </summary>
        /// <returns>Either a <see cref="CsvFileHeaderLine"/> or a <see cref="CsvFileRecordLine"/>, if available; <c>null</c> if the end of the stream has been reached.</returns>
        /// <exception cref="CsvFileException">The line read did not have the expected number of fields.
        /// -or-
        /// A property could not be mapped to a CSV field.
        /// -or-
        /// A field contained an invalid value.
        /// </exception>
#if NETCOREAPP3_0_OR_GREATER
        public ICsvFileLine? ReadLine()
#else
        [CanBeNull]
        public ICsvFileLine ReadLine()
#endif
        {
            if (_csvSource is null)
            {
                _csvSource = ReadCsv(_reader, _options);
            }

            if (_csvSource is null) return null;

            foreach (var rawFieldValues in _csvSource)
            {
                if (rawFieldValues.Count == 0)
                {
                    // ref. RFC 4180§2¶4
                    var dataItems = new Dictionary<string, object>() { { DataItemName.ActualFieldCount, rawFieldValues.Count } };
                    throw BuildException("Expected one or more fields.", dataItems);
                }

                ICsvFileLine csvLine;

                // Read a header line from the CSV file.
                // ref. RFC 4180§2¶3
                if (_options.HasHeaderLine && _headerLine is null)
                {
                    csvLine = _headerLine = new CsvFileHeaderLine();

                    // Make sure all field names are unique.
                    var duplicateFieldNames = rawFieldValues
                        .GroupBy(item => item)
                        .Where(grp => grp.Count() > 1)
                        .Select(grp => grp.Key)
                        .ToArray();
                    if (duplicateFieldNames.Length > 0)
                    {
                        var message = $"Duplicate field names detected: {string.Join(", ", duplicateFieldNames)}";
                        var dataItems = new Dictionary<string, object>()
                        {
                            { DataItemName.ActualFieldCount, duplicateFieldNames.Length }, 
                            { DataItemName.FieldNames, duplicateFieldNames }
                        };
                        throw BuildException(message, dataItems);
                    }

                    // Add field names to the line.  Key and Value will be identical because the field name (key) is the value in this case.
                    foreach (var fieldValue in rawFieldValues)
                    {
                        csvLine.Add(fieldValue, fieldValue);
                    }

                    _expectedFieldCount = csvLine.Count;
                }
                // Read a record line from the CSV file.
                else
                {
                    csvLine = new TRecordLine();

                    var fieldIndex = 0;
                    foreach (var rawFieldValue in rawFieldValues)
                    {
                        var defaultFieldName = $"Field {++fieldIndex}";
                        var fieldName = _headerLine?.FieldNames.ElementAtOrDefault(fieldIndex - 1);
                        csvLine.Add(fieldName ?? defaultFieldName, rawFieldValue);
                    }

                    if (_expectedFieldCount == 0)
                    {
                        _expectedFieldCount = csvLine.Count;
                    }

                    if (csvLine.Count != _expectedFieldCount)
                    {
                        if (_options.InconsistentFieldCountHandler is CsvFieldCount<TRecordLine> inconsistentFieldCountHandler)
                        {
                            var context = new CsvFieldContext<TRecordLine>(_options)
                            {
                                Model = (TRecordLine)csvLine,
                                HeaderLine = _headerLine
                            };
                            inconsistentFieldCountHandler(context);
                            continue;
                        }

                        var message = string.Format(ErrorMessage.FieldCountMismatchTemplate, csvLine.Count, _expectedFieldCount);
                        var dataItems = new Dictionary<string, object>
                        {
                            { DataItemName.ExpectedFieldCount, _expectedFieldCount },
                            { DataItemName.ActualFieldCount, csvLine.Count }
                        };
                        throw BuildException(message, dataItems);
                    }
                }

                // Map CSV fields to model properties.
                if (csvLine is TRecordLine recordLine)
                {
                    MapCsvFieldsToProperties(recordLine);
                }

                return csvLine;
            }

            return null;
        }

        /// <summary>
        /// Gets the context of the reader's operation.
        /// </summary>
#pragma warning disable IDE0074
        public CsvContext Context => _context ?? (_context = new CsvContext(ImperativeMapperCache, _options));
#pragma warning restore IDE0074
        private void MapCsvFieldsToProperties(TRecordLine recordLine)
        {
            ICsvFileLine csvLine = recordLine;

            string[] fieldNames = csvLine.Keys.Cast<string>().ToArray();
            var mapper = GetMapper(recordLine);
            foreach (var fieldMapping in mapper)
            {
                // Determine the name of the field the property is mapped to.
#if NETCOREAPP3_0_OR_GREATER
                string? mappedFieldName = null;
#else
                string mappedFieldName = null;
#endif
                if (fieldMapping.Value.FieldIndex.HasValue)
                {
                    mappedFieldName = fieldNames.ElementAtOrDefault(fieldMapping.Value.FieldIndex.Value);
                }

                if (mappedFieldName is null)
                {
                    mappedFieldName = fieldMapping.Value.FieldName;
                }

                if (mappedFieldName is null)
                {
                    if (fieldMapping.Value.IsOptional)
                    {
                        continue;
                    }

                    var message = $"Property '{fieldMapping.Key}' not mapped to CSV field {string.Join(",", fieldMapping.Value.FieldName)}.";
                    var dataItems = new Dictionary<string, object>
                    {
                        { DataItemName.PropertyName, fieldMapping.Key },
                    };
                    if (!string.IsNullOrWhiteSpace(fieldMapping.Value.FieldName))
                    {
                        dataItems.Add(DataItemName.FieldName, fieldMapping.Value.FieldName);
                    }
                    throw BuildException(message, dataItems);
                }

                var fieldIndex = fieldNames
                    .Select((key, index) => new { FieldName = key, Index = index })
                    .First(item => item.FieldName == mappedFieldName).Index;
                var fieldContext = new CsvFieldContext<TRecordLine>(_options)
                {
                    Model = recordLine,
                    FieldIndex = fieldIndex,
                    FieldName = mappedFieldName,
                    HeaderLine = _headerLine
                };

                var rawFieldValue = csvLine[mappedFieldName] as string ?? String.Empty;

                // Validate CSV field value.
                if (fieldMapping.Value.Validator is CsvFieldValidate<TRecordLine> validator && !validator(fieldContext))
                {
                    if (_options.BadValueHandler is CsvFieldBadValue<TRecordLine> badValueHandler)
                    {
                        badValueHandler(fieldContext);
                        continue;
                    }

                    var message = $"Encountered invalid value '{rawFieldValue}' in CSV field {string.Join(",", fieldMapping.Value.FieldName)}.";
                    var dataItems = new Dictionary<string, object>
                    {
                        { DataItemName.PropertyName, fieldMapping.Key },
                        { DataItemName.FieldValue, rawFieldValue }
                    };
                    if (!string.IsNullOrWhiteSpace(fieldMapping.Value.FieldName))
                    {
                        dataItems.Add(DataItemName.FieldName, fieldMapping.Value.FieldName);
                    }
                    throw BuildException(message, dataItems);
                }

                // Deserialize the field value.
                var deserializeFieldValueMethodDefinition = this.GetType().GetTypeInfo().DeclaredMethods
                    .Single(method => method.Name == nameof(DeserializeCsvLineFieldValue))
                    .GetGenericMethodDefinition();
                var deserializeFieldValueMethod = deserializeFieldValueMethodDefinition.MakeGenericMethod(fieldMapping.Value.PropertyType);
#if NETCOREAPP3_0_OR_GREATER
                var deserializedFieldValue = deserializeFieldValueMethod.Invoke(this, new object?[] { _headerLine, recordLine, _options, mappedFieldName });
#else
                var deserializedFieldValue = deserializeFieldValueMethod.Invoke(this, new object[] { _headerLine, recordLine, _options, mappedFieldName });
#endif
                csvLine[mappedFieldName] = deserializedFieldValue;
            }
        }

        private IList<string> ParseCsvRecord(string record)
        {
            var fieldValues = new List<string>();
            var fieldValueBuilder = new StringBuilder();
            bool inQuotes = false, quotedValue = false;
            bool isInvalidValue = false;
            var fieldIndex = 0;

            void HandleInvalidFieldValue(string s, int i)
            {
                if (_options.BadValueHandler is CsvFieldBadValue<TRecordLine> badValueHandler)
                {
                    var context = new CsvFieldContext<TRecordLine>(_options)
                    {
                        RawFieldValue = s,
                        FieldIndex = i,
                        HeaderLine = _headerLine
                    };
                    badValueHandler(context);
                    return;
                }

                var message = $"Encountered invalid value at field index {i}.";
                var dataItems = new Dictionary<string, object> { { DataItemName.FieldIndex, i } };
                throw BuildException(message, dataItems);
            }

            for (int characterIndex = 0; characterIndex < record.Length; characterIndex++)
            {
                char character = record[characterIndex];

                if (inQuotes)
                {
                    if (character == '"')
                    {
                        // Check for escaped quote
                        // ref. RFC 4180§2¶7
                        if (characterIndex + 1 < record.Length && record[characterIndex + 1] == '"')
                        {
                            fieldValueBuilder.Append('"');
                            characterIndex++; // Skip the second quote
                        }
                        else
                        {
                            inQuotes = false; // Closing quote
                        }
                    }
                    else
                    {
                        fieldValueBuilder.Append(character);
                    }
                }
                else
                {
                    // ref. RFC 4180§2¶5
                    if (character == '"')
                    {
                        inQuotes = true;
                        quotedValue = true;

                        // Check if the field value is all whitespace.
                        var isFieldValueWhiteSpace = true;
                        for (int index = 0; index < fieldValueBuilder.Length && isFieldValueWhiteSpace; index++)
                        {
                            if (!char.IsWhiteSpace(fieldValueBuilder[index]))
                            {
                                isFieldValueWhiteSpace = false;
                            }
                        }

                        // If the field value is not whitespace, the current quote character would be embedded without quoting and thus invalid.
                        // ref. RFC 4180§2¶5
                        if (!isFieldValueWhiteSpace)
                        {
                            HandleInvalidFieldValue(fieldValueBuilder.ToString(), fieldIndex);
                            isInvalidValue = true;
                        }
                    }
                    else if (character == _options.FieldDelimiterCharacter)
                    {
                        fieldIndex++;
                        quotedValue = false;

                        if (isInvalidValue)
                        {
                            isInvalidValue = false;

                            fieldValues.Add(String.Empty);
                            fieldValueBuilder.Clear();
                            continue;
                        }

                        fieldValues.Add(fieldValueBuilder.ToString());
                        fieldValueBuilder.Clear();
                    }
                    else
                    {
                        fieldValueBuilder.Append(character);

                        // Check for non-whitespace outside a quoted value.
                        // ref. RFC 4180§2¶5
                        if (quotedValue && !char.IsWhiteSpace(character))
                        {
                            HandleInvalidFieldValue(fieldValueBuilder.ToString(), fieldIndex);
                            isInvalidValue = true;
                        }
                    }
                }
            }

            // Add last field
            fieldValues.Add(fieldValueBuilder.ToString());

            // Normalize embedded line breaks.
            if (_options.EmbeddedLineBreak != null)
            {
                for (int fieldValueIndex = 0; fieldValueIndex < fieldValues.Count; fieldValueIndex++)
                {
                    var fieldValue = fieldValues[fieldValueIndex];
                    fieldValues[fieldValueIndex] = NormalizeFieldValueLineBreaks(fieldValue, _options);
                }
            }

            return fieldValues;
        }

        private IEnumerable<IList<string>> ReadCsv(TextReader reader, CsvFileOptions options)
        {
            var recordBuffer = new StringBuilder();
            var quoteCount = 0;

            bool EndsWith(StringBuilder sb, string lineBreak)
            {
                if (sb.Length < lineBreak.Length)
                {
                    return false;
                }

                var lineBreakIndex = 0;
                return lineBreak
                    .All(character => character == sb[sb.Length - lineBreak.Length + lineBreakIndex++]);
            }

            var endOfStream = false;
            while (!endOfStream)
            {
                var characterValue = reader.Read();
                if (characterValue == -1)
                {
                    endOfStream = true;
                    continue;
                }

                recordBuffer.Append((char)characterValue);

                if ((char)characterValue == '"') quoteCount++;

                // Check for terminator only if not inside quoted field
                // ref. RFC 4180§2¶1
                if (EndsWith(recordBuffer, options.TerminatorLineBreak) && quoteCount % 2 == 0)
                {
                    string record = recordBuffer.ToString(0, recordBuffer.Length - options.TerminatorLineBreak.Length);
                    recordBuffer.Clear();
                    quoteCount = 0;
                    yield return ParseCsvRecord(record);
                }
            }

            // Handle final record if no terminator
            // ref. RFC 4180§2¶2
            if (recordBuffer.Length > 0)
            {
                yield return ParseCsvRecord(recordBuffer.ToString());
            }
        }
    }
}
