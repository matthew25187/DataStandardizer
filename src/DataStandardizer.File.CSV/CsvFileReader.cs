using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DataStandardizer.Core;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using JetBrains.Annotations;
#endif

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Represents a reader that provides fast, non-cached, forward-only access to CSV data.
    /// </summary>
    public sealed class CsvFileReader<TRecordLine> : CsvFileIoBase<TRecordLine>, IDisposable
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
            internal const string DuplicateFieldNamesTemplate = "Found {0} duplicate field names.";
            internal const string FieldCountMismatchTemplate = "Found {0} of {1} expected fields.";
        }
        private const int DefaultBufferSize = 16 * 1024;
        private const char QuoteCharacter = '"';

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvContext? _context;
        private readonly MethodInfo? _deserializeFieldValueMethodDefinition;
        private IReadOnlyList<string>? _headerFieldNames;
        private CsvFileHeaderLine? _headerLine;
        private List<KeyValuePair<string, int>>? _invalidFieldValues;
        private readonly TextReader _reader = null!;
#else
        [CanBeNull] private CsvContext _context;
        [CanBeNull] private readonly MethodInfo _deserializeFieldValueMethodDefinition;
        [CanBeNull] private IReadOnlyList<string> _headerFieldNames;
        [CanBeNull] private CsvFileHeaderLine _headerLine;
        [CanBeNull] private List<KeyValuePair<string, int>> _invalidFieldValues;
        [NotNull] private readonly TextReader _reader;
#endif
#if NET8_0_OR_GREATER
        private SearchValues<char>? _unquotedStopCharacters;
        private char _unquotedStopDelimiter;
        private char _unquotedStopTerminatorStart;
#endif
        private char[] _buffer = Array.Empty<char>();
        private int _bufferEnd;
        private int _bufferLength;
        private int _bufferPosition;
        private readonly int _bufferSize = DefaultBufferSize;
        private bool _endOfStream;
        private int _expectedFieldCount;
        private readonly StringBuilder _fieldValueBuilder = new StringBuilder();
        private int _fieldValueStart;
        private bool _headerLineIgnored;
        private readonly bool _isInternalReader;
        private readonly ICsvFileOptions _options = new CsvFileOptions();
        private readonly List<string> _rawFieldValues = new List<string>();

        private CsvFileReader()
        {
            _deserializeFieldValueMethodDefinition = this
                .GetMethod(nameof(DeserializeCsvLineFieldValue), isProtected: true)
                ?.GetGenericMethodDefinition();
        }

        public CsvFileReader(Stream csvStream) : this()
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
        public CsvFileReader(string csvFilePath) : this()
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));

            _reader = new StreamReader(csvFilePath);
            _isInternalReader = true;
        }

        public CsvFileReader(string csvFilePath, CsvFileOptions options) : this()
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            _options = options;

            // Open the file once, with the requested encoding if any, so no handle is left orphaned.
            _reader = _options.Encoding != null
                ? new StreamReader(csvFilePath, _options.Encoding)
                : new StreamReader(csvFilePath);
            _isInternalReader = true;
        }
#endif

        public CsvFileReader(TextReader reader) : this()
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

        /// <summary>
        /// Creates a reader with a specific size for the internal character buffer.  Allows tests to force buffer refills at chosen points.
        /// </summary>
        internal CsvFileReader(TextReader reader, CsvFileOptions options, int bufferSize) : this(reader, options)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _bufferSize = bufferSize;
        }

        public void Dispose()
        {
            if (!MarkDisposed()) return;

            if (_isInternalReader)
            {
                _reader.Dispose();
            }

            if (_bufferLength > 0)
            {
                ArrayPool<char>.Shared.Return(_buffer);
                _buffer = Array.Empty<char>();
                _bufferLength = 0;
            }
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
        /// -or-
        /// Duplicate field names were detected.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The terminator line break option is empty.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ICsvFileLine? ReadLine()
#else
        [CanBeNull]
        public ICsvFileLine ReadLine()
#endif
        {
            ThrowIfDisposed();

            var rawFieldValues = _rawFieldValues;
            while (TryReadRecord(rawFieldValues))
            {
                if (rawFieldValues.Count == 0)
                {
                    // ref. RFC 4180§2¶4
                    var dataItems = new Dictionary<string, object> { { DataItemName.ActualFieldCount, rawFieldValues.Count } };
                    throw BuildException("Expected one or more fields.", dataItems);
                }

                ICsvFileLine csvLine;

                // Read a header line from the CSV file.
                // ref. RFC 4180§2¶3
                switch (_options.HeaderHandling)
                {
                    case CsvFileHeaderHandling.Use when _headerLine is null:
                    {
                        csvLine = _headerLine = new CsvFileHeaderLine();

                        // Make sure all field names are unique.
                        var duplicateFieldNames = GetDuplicateFieldNames(rawFieldValues);
                        if (duplicateFieldNames.Length > 0)
                        {
                            var message = string.Format(ErrorMessage.DuplicateFieldNamesTemplate, duplicateFieldNames.Length);
                            var dataItems = new Dictionary<string, object>
                            {
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
                        break;
                    case CsvFileHeaderHandling.Ignore when !_headerLineIgnored:
                        _headerLineIgnored = true;
                        continue;

                    default:
                    {
                        csvLine = new TRecordLine();

                        var mapper = GetMapper((TRecordLine)csvLine);
                        var defaultFieldNames = CsvMappingService.GetSortedFieldNamesFromMapper(mapper);

                        var headerFieldNames = _headerFieldNames;
                        if (headerFieldNames is null)
                        {
                            _headerFieldNames = _headerLine?.FieldNames;
                            if (_headerFieldNames is null && _options.HeaderHandler is CsvFileHeader headerHandler)
                            {
                                _headerFieldNames = headerHandler(_headerLine);

                                // Check for duplicate names.
                                var duplicateFieldNames = GetDuplicateFieldNames(_headerFieldNames);
                                if (duplicateFieldNames.Length > 0)
                                {
                                    var message = string.Format(ErrorMessage.DuplicateFieldNamesTemplate, duplicateFieldNames.Length);
                                    var dataItems = new Dictionary<string, object> { { DataItemName.FieldNames, duplicateFieldNames } };
                                    throw BuildException(message, dataItems);
                                }
                            }

                            headerFieldNames = _headerFieldNames
#if NET8_0_OR_GREATER
                                               ?? [];
#else
                                               ?? Array.Empty<string>();
#endif
                        }

                        var fieldIndex = 0;
                        foreach (var rawFieldValue in rawFieldValues)
                        {
                            var defaultFieldName = defaultFieldNames.ElementAtOrDefault(fieldIndex++) ?? $"Field {fieldIndex}";
                            var fieldName = headerFieldNames.ElementAtOrDefault(fieldIndex - 1);
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

                        // Map CSV fields to model properties.
                        if (mapper.Count > 0)
                        {
                            MapCsvFieldsToProperties((TRecordLine)csvLine);
                        }

                    }
                        break;
                }

                return csvLine;
            }

            return null;
        }

        /// <summary>
        /// Gets the context of the reader's operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">The reader has been disposed.</exception>
        public CsvContext Context
        {
            get
            {
                ThrowIfDisposed();

#pragma warning disable IDE0074
                return _context ?? (_context = new CsvContext(ImperativeMappers, _options));
#pragma warning restore IDE0074
            }
        }

        [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
        private string[] GetDuplicateFieldNames(IEnumerable<string> fieldNames)
        {
            if (!fieldNames.Any())
            {
#if NET8_0_OR_GREATER
                return [];
#else
                return Array.Empty<string>();
#endif
            }

            var strings = fieldNames
                .GroupBy(name => name)
                .Where(grp => grp.Count() > 1)
                .Select(grp => grp.Key)
                .ToArray();
            return strings;
        }

        private void MapCsvFieldsToProperties(TRecordLine recordLine)
        {
            ICsvFileLine csvLine = recordLine;

            var mapper = GetMapper(recordLine);
            var fieldNames = CsvMappingService.GetSortedFieldNames(csvLine, mapper);
            foreach (var fieldMapping in mapper)
            {
                // Determine the name of the field the property is mapped to.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
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
                    mappedFieldName = CsvMappingService.GetFieldNameFromMapping(fieldMapping);
                }

                if (!csvLine.Contains(mappedFieldName) && fieldMapping.Value.IsOptional)
                {
                    continue;
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

                var fieldKey = mappedFieldName;
                if (!csvLine.Contains(fieldKey) && csvLine.Contains(fieldMapping.Key))
                {
                    fieldKey = fieldMapping.Key;
                }

                var rawFieldValue = csvLine[fieldKey] as string ?? string.Empty;

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
                var fieldValue = GetCsvLineMappedFieldValue(recordLine, fieldMapping);
                var deserializeFieldValueMethod = _deserializeFieldValueMethodDefinition?.MakeGenericMethod(fieldMapping.Value.PropertyType);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                var deserializedFieldValue = deserializeFieldValueMethod?.Invoke(this, new object?[] { _headerLine, recordLine, _options, fieldMapping, fieldValue });
#else
                var deserializedFieldValue = deserializeFieldValueMethod?.Invoke(this, new object[] { _headerLine, recordLine, _options, fieldMapping, fieldValue });
#endif
                csvLine[fieldKey] = deserializedFieldValue;
            }
        }

        /// <summary>
        /// Reads the next CSV record from the stream and splits it into raw field values.
        /// </summary>
        /// <remarks>
        /// Characters are read a block at a time and each field value is sliced straight out of the block.  The field value builder is only used when
        /// a value contains an escaped quote, is split around quotes or straddles a buffer refill.  Invalid field values are collected while the record
        /// is read and reported once it is complete, so the stream is always left positioned at the start of the next record.
        /// </remarks>
        /// <param name="fieldValues">The list to receive the raw field values.  It is cleared first.</param>
        /// <returns><c>true</c> if a record was read; <c>false</c> if the end of the stream has been reached.</returns>
        internal bool TryReadRecord(List<string> fieldValues)
        {
            fieldValues.Clear();
            _invalidFieldValues?.Clear();

            var fieldDelimiter = _options.FieldDelimiterCharacter;
            var terminator = _options.TerminatorLineBreak;
            if (string.IsNullOrEmpty(terminator))
                throw new InvalidOperationException("The terminator line break must have at least one character.");

            var terminatorStart = terminator[0];
            var buffer = EnsureBuffer(terminator.Length);
#if NET8_0_OR_GREATER
            var unquotedStopCharacters = GetUnquotedStopCharacters(fieldDelimiter, terminatorStart);
#endif

            _fieldValueBuilder.Clear();
            _fieldValueStart = _bufferPosition;

            // A final record without a terminator is returned, but an empty remainder of the stream is not.
            // ref. RFC 4180§2¶2
            if (!EnsureAvailable(1)) return false;

            bool inQuotes = false, quotedValue = false, isInvalidValue = false;
            var fieldIndex = 0;

            while (true)
            {
                if (inQuotes)
                {
                    var quoteIndex = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition).IndexOf(QuoteCharacter);
                    if (quoteIndex < 0)
                    {
                        _bufferPosition = _bufferEnd;
                        if (EnsureAvailable(1)) continue;

                        // An unterminated quote runs to the end of the stream.
                        fieldValues.Add(TakeFieldValue());
                        break;
                    }

                    _bufferPosition += quoteIndex;
                    var hasNextCharacter = EnsureAvailable(2);

                    // Check for escaped quote
                    // ref. RFC 4180§2¶7
                    if (hasNextCharacter && buffer[_bufferPosition + 1] == QuoteCharacter)
                    {
                        // Keep the first quote of the pair and skip the second.
                        _fieldValueBuilder.Append(buffer, _fieldValueStart, _bufferPosition + 1 - _fieldValueStart);
                        _bufferPosition += 2;
                        _fieldValueStart = _bufferPosition;
                        continue;
                    }

                    // Closing quote.  Usually the field ends straight after it, so the value can still be taken as one slice of the buffer.
                    inQuotes = false;
                    if (!hasNextCharacter)
                    {
                        fieldValues.Add(TakeFieldValue());
                        _bufferPosition++;
                        break;
                    }

                    var nextCharacter = buffer[_bufferPosition + 1];
                    if (nextCharacter == terminatorStart && IsTerminatorAt(1, terminator))
                    {
                        fieldValues.Add(TakeFieldValue());
                        _bufferPosition += 1 + terminator.Length;
                        break;
                    }

                    if (nextCharacter == fieldDelimiter)
                    {
                        AddDelimitedFieldValue(fieldValues, isInvalidValue);
                        fieldIndex++;
                        quotedValue = isInvalidValue = false;
                        _bufferPosition += 2;
                        _fieldValueStart = _bufferPosition;
                        continue;
                    }

                    // Something else follows the closing quote, so the value is no longer one slice of the buffer.
                    AppendPendingFieldValue();
                    _bufferPosition++;
                    _fieldValueStart = _bufferPosition;
                    continue;
                }

                if (!quotedValue)
                {
                    // Skip straight to the next character that could end the field or the record, or open a quote.
#if NET8_0_OR_GREATER
                    var stopIndex = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition).IndexOfAny(unquotedStopCharacters);
#else
                    var stopIndex = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition).IndexOfAny(fieldDelimiter, QuoteCharacter, terminatorStart);
#endif
                    if (stopIndex < 0)
                    {
                        _bufferPosition = _bufferEnd;
                        if (EnsureAvailable(1)) continue;

                        fieldValues.Add(TakeFieldValue());
                        break;
                    }

                    _bufferPosition += stopIndex;
                }
                else if (!EnsureAvailable(1))
                {
                    fieldValues.Add(TakeFieldValue());
                    break;
                }

                var character = buffer[_bufferPosition];

                // Check for terminator only if not inside quoted field
                // ref. RFC 4180§2¶1
                if (character == terminatorStart && IsTerminatorAt(0, terminator))
                {
                    fieldValues.Add(TakeFieldValue());
                    _bufferPosition += terminator.Length;
                    break;
                }

                // ref. RFC 4180§2¶5
                if (character == QuoteCharacter)
                {
                    inQuotes = true;
                    quotedValue = true;

                    // If the field value is not whitespace, the current quote character would be embedded without quoting and thus invalid.
                    // ref. RFC 4180§2¶5
                    if (!IsPendingFieldValueWhiteSpace())
                    {
                        AddInvalidFieldValue(PeekFieldValue(), fieldIndex);
                        isInvalidValue = true;
                    }

                    AppendPendingFieldValue();
                    _bufferPosition++;
                    _fieldValueStart = _bufferPosition;
                    continue;
                }

                if (character == fieldDelimiter)
                {
                    AddDelimitedFieldValue(fieldValues, isInvalidValue);
                    fieldIndex++;
                    quotedValue = isInvalidValue = false;
                    _bufferPosition++;
                    _fieldValueStart = _bufferPosition;
                    continue;
                }

                _bufferPosition++;

                // Check for non-whitespace outside a quoted value.
                // ref. RFC 4180§2¶5
                if (quotedValue && !char.IsWhiteSpace(character))
                {
                    AddInvalidFieldValue(PeekFieldValue(), fieldIndex);
                    isInvalidValue = true;
                }
            }

            if (_invalidFieldValues != null)
            {
                foreach (var invalidFieldValue in _invalidFieldValues)
                {
                    HandleInvalidFieldValue(invalidFieldValue.Key, invalidFieldValue.Value);
                }
            }

            // Normalize embedded line breaks.
            if (_options.EmbeddedLineBreak != null)
            {
                for (int fieldValueIndex = 0; fieldValueIndex < fieldValues.Count; fieldValueIndex++)
                {
                    fieldValues[fieldValueIndex] = NormalizeFieldValueLineBreaks(fieldValues[fieldValueIndex], _options);
                }
            }

            return true;
        }

        private void AddDelimitedFieldValue(List<string> fieldValues, bool isInvalidValue)
        {
            if (isInvalidValue)
            {
                _fieldValueBuilder.Clear();
                fieldValues.Add(string.Empty);
                return;
            }

            fieldValues.Add(TakeFieldValue());
        }

        private void AddInvalidFieldValue(string rawFieldValue, int fieldIndex)
        {
            if (_invalidFieldValues is null)
            {
                _invalidFieldValues = new List<KeyValuePair<string, int>>();
            }

            _invalidFieldValues.Add(new KeyValuePair<string, int>(rawFieldValue, fieldIndex));
        }

        /// <summary>
        /// Moves the part of the field value still in the buffer into the field value builder.
        /// </summary>
        private void AppendPendingFieldValue()
        {
            if (_bufferPosition > _fieldValueStart)
            {
                _fieldValueBuilder.Append(_buffer, _fieldValueStart, _bufferPosition - _fieldValueStart);
                _fieldValueStart = _bufferPosition;
            }
        }

        private char[] EnsureBuffer(int terminatorLength)
        {
            // There must be room to look past a closing quote for a whole terminator.
            var requiredLength = Math.Max(_bufferSize, terminatorLength + 1);
            if (_bufferLength >= requiredLength)
            {
                return _buffer;
            }

            var buffer = ArrayPool<char>.Shared.Rent(requiredLength);
            if (_bufferLength > 0)
            {
                Array.Copy(_buffer, _bufferPosition, buffer, 0, _bufferEnd - _bufferPosition);
                _bufferEnd -= _bufferPosition;
                _bufferPosition = 0;
                ArrayPool<char>.Shared.Return(_buffer);
            }

            _buffer = buffer;
            _bufferLength = requiredLength;
            return buffer;
        }

        /// <summary>
        /// Makes sure the given number of unread characters is in the buffer, reading more from the stream if needed.
        /// </summary>
        /// <returns><c>true</c> if the characters are available; <c>false</c> if the end of the stream comes first.</returns>
        private bool EnsureAvailable(int count)
        {
            if (_bufferEnd - _bufferPosition >= count) return true;
            if (_endOfStream) return false;

            // Keep the part of the field value read so far, then move the unread characters to the front of the buffer.
            AppendPendingFieldValue();
            var buffer = _buffer;
            var remaining = _bufferEnd - _bufferPosition;
            if (remaining > 0 && _bufferPosition > 0)
            {
                Array.Copy(buffer, _bufferPosition, buffer, 0, remaining);
            }

            _bufferPosition = _fieldValueStart = 0;
            _bufferEnd = remaining;

            while (_bufferEnd < count)
            {
                var read = _reader.Read(buffer, _bufferEnd, _bufferLength - _bufferEnd);
                if (read == 0)
                {
                    _endOfStream = true;
                    return false;
                }

                _bufferEnd += read;
            }

            return true;
        }

#if NET8_0_OR_GREATER
        private SearchValues<char> GetUnquotedStopCharacters(char fieldDelimiter, char terminatorStart)
        {
            if (_unquotedStopCharacters is null || _unquotedStopDelimiter != fieldDelimiter || _unquotedStopTerminatorStart != terminatorStart)
            {
                _unquotedStopCharacters = SearchValues.Create([fieldDelimiter, QuoteCharacter, terminatorStart]);
                _unquotedStopDelimiter = fieldDelimiter;
                _unquotedStopTerminatorStart = terminatorStart;
            }

            return _unquotedStopCharacters;
        }
#endif

        private void HandleInvalidFieldValue(string rawFieldValue, int fieldIndex)
        {
            if (_options.BadValueHandler is CsvFieldBadValue<TRecordLine> badValueHandler)
            {
                var context = new CsvFieldContext<TRecordLine>(_options)
                {
                    RawFieldValue = rawFieldValue,
                    FieldIndex = fieldIndex,
                    HeaderLine = _headerLine
                };
                badValueHandler(context);
                return;
            }

            var message = $"Encountered invalid value at field index {fieldIndex}.";
            var dataItems = new Dictionary<string, object> { { DataItemName.FieldIndex, fieldIndex } };
            throw BuildException(message, dataItems);
        }

        private bool IsPendingFieldValueWhiteSpace()
        {
            for (int index = 0; index < _fieldValueBuilder.Length; index++)
            {
                if (!char.IsWhiteSpace(_fieldValueBuilder[index])) return false;
            }

            var buffer = _buffer;
            for (int index = _fieldValueStart; index < _bufferPosition; index++)
            {
                if (!char.IsWhiteSpace(buffer[index])) return false;
            }

            return true;
        }

        /// <summary>
        /// Checks for the terminator at an offset from the current buffer position, reading more from the stream if needed to compare it whole.
        /// </summary>
        /// <remarks>
        /// The caller has already matched the first character of the terminator.
        /// </remarks>
        private bool IsTerminatorAt(int offset, string terminator)
        {
            if (terminator.Length == 1) return true;
            if (!EnsureAvailable(offset + terminator.Length)) return false;

            return _buffer.AsSpan(_bufferPosition + offset, terminator.Length).SequenceEqual(terminator.AsSpan());
        }

        /// <summary>
        /// Gets the field value read so far without consuming it.
        /// </summary>
        private string PeekFieldValue()
        {
            var pendingValue = new string(_buffer, _fieldValueStart, _bufferPosition - _fieldValueStart);
            return _fieldValueBuilder.Length == 0 ? pendingValue : _fieldValueBuilder + pendingValue;
        }

        /// <summary>
        /// Gets the field value read so far and resets the builder for the next field.
        /// </summary>
        private string TakeFieldValue()
        {
            var length = _bufferPosition - _fieldValueStart;
            if (_fieldValueBuilder.Length == 0)
            {
                return length == 0 ? string.Empty : new string(_buffer, _fieldValueStart, length);
            }

            var fieldValue = _fieldValueBuilder.Append(_buffer, _fieldValueStart, length).ToString();
            _fieldValueBuilder.Clear();
            return fieldValue;
        }
    }
}
