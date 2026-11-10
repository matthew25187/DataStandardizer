using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;

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

        /// <summary>
        /// Size, in bytes, of the buffers of the <see cref="StreamReader"/> and <see cref="FileStream"/> the reader
        /// creates itself.  A caller-supplied <see cref="TextReader"/> keeps its own buffering.
        /// </summary>
        internal const int StreamBufferSize = 16 * 1024;

        /// <summary>
        /// Encoding used when none is specified, matching the default of <see cref="StreamReader"/>.
        /// </summary>
        private static readonly Encoding DefaultEncoding = Encoding.UTF8;

        private static readonly char[] NoCharacters = new char[0];
        private static readonly string[] NoFieldNames = new string[0];

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvContext? _context;
        private FieldPlan? _fieldPlan;
        private ExceptionDispatchInfo? _headerFailure;
        private IReadOnlyList<string>? _headerFieldNames;
        private CsvFileHeaderLine? _headerLine;
        private List<KeyValuePair<string, int>>? _invalidFieldValues;
        private readonly TextReader _reader = null!;
#else
        [MaybeNull] private CsvContext _context;
        [MaybeNull] private FieldPlan _fieldPlan;
        [MaybeNull] private ExceptionDispatchInfo _headerFailure;
        [MaybeNull] private IReadOnlyList<string> _headerFieldNames;
        [MaybeNull] private CsvFileHeaderLine _headerLine;
        [MaybeNull] private List<KeyValuePair<string, int>> _invalidFieldValues;
        [NotNull] private readonly TextReader _reader;
#endif
#if NET8_0_OR_GREATER
        private SearchValues<char>? _unquotedStopCharacters;
        private char _unquotedStopDelimiter;
        private char _unquotedStopTerminatorStart;
#endif
        private char[] _buffer = NoCharacters;
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

        public CsvFileReader(Stream csvStream)
        {
            if (csvStream is null)
                throw new ArgumentNullException(nameof(csvStream));
            if (!csvStream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", nameof(csvStream));

            _reader = CreateStreamReader(csvStream, DefaultEncoding);
            _isInternalReader = true;
        }

        public CsvFileReader(Stream csvStream, CsvFileOptions options)
        {
            if (csvStream is null)
                throw new ArgumentNullException(nameof(csvStream));
            if (!csvStream.CanRead)
                throw new ArgumentException("Expected a stream supporting read.", nameof(csvStream));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            _options = options;

            // Create the reader once, with the requested encoding if any, so no buffer is allocated and discarded.
            _reader = CreateStreamReader(csvStream, _options.Encoding ?? DefaultEncoding);
            _isInternalReader = true;
        }

#if NETSTANDARD2_0_OR_GREATER||NETCOREAPP2_0_OR_GREATER
        public CsvFileReader(string csvFilePath)
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));

            _reader = CreateFileReader(csvFilePath, DefaultEncoding);
            _isInternalReader = true;
        }

        public CsvFileReader(string csvFilePath, CsvFileOptions options)
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            _options = options;

            // Open the file once, with the requested encoding if any, so no handle is left orphaned.
            _reader = CreateFileReader(csvFilePath, _options.Encoding ?? DefaultEncoding);
            _isInternalReader = true;
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

        /// <summary>
        /// Creates a reader with a specific size for the internal character buffer.  Allows tests to force buffer refills at chosen points.
        /// </summary>
        internal CsvFileReader(TextReader reader, CsvFileOptions options, int bufferSize) : this(reader, options)
        {
            if (bufferSize < 1)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            _bufferSize = bufferSize;
        }

        private static StreamReader CreateStreamReader(Stream stream, Encoding encoding)
        {
            return new StreamReader(stream, encoding, true, StreamBufferSize);
        }

#if NETSTANDARD2_0_OR_GREATER||NETCOREAPP2_0_OR_GREATER
        private static StreamReader CreateFileReader(string path, Encoding encoding)
        {
            // Open the file stream explicitly so both buffering layers are sized together.
            var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.SequentialScan);
            try
            {
                return CreateStreamReader(fileStream, encoding);
            }
            catch
            {
                fileStream.Dispose();
                throw;
            }
        }
#endif

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
                _buffer = NoCharacters;
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
        /// <exception cref="InvalidOperationException">The terminator line break option is empty.
        /// -or-
        /// The <see cref="CsvFileOptions.HeaderHandler"/> delegate returned <c>null</c>.
        /// </exception>
        /// <remarks>
        /// Records can't be read without valid field names.  When reading the header line, or getting field names from the
        /// <see cref="CsvFileOptions.HeaderHandler"/> delegate, throws an exception, every later call throws the same
        /// exception and reads no further lines.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public ICsvFileLine? ReadLine()
#else
        [return: MaybeNull]
        public ICsvFileLine ReadLine()
#endif
        {
            ThrowIfDisposed();

            _headerFailure?.Throw();

            // Read a header line from the CSV file.
            // ref. RFC 4180§2¶3
            if ((_options.HeaderHandling == CsvFileHeaderHandling.Use && _headerLine is null) ||
                (_options.HeaderHandling == CsvFileHeaderHandling.Ignore && !_headerLineIgnored))
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                CsvFileHeaderLine? headerLine;
#else
                CsvFileHeaderLine headerLine;
#endif
                try
                {
                    headerLine = ReadHeaderLine();
                }
                catch (Exception exception)
                {
                    _headerFailure = ExceptionDispatchInfo.Capture(exception);
                    throw;
                }

                if (_options.HeaderHandling == CsvFileHeaderHandling.Use)
                {
                    return headerLine;
                }
            }

            var rawFieldValues = _rawFieldValues;
            while (TryReadRecord(rawFieldValues))
            {
                ThrowIfNoFields(rawFieldValues);

                var recordLine = new TRecordLine();
                recordLine.EnsureFieldCapacity(rawFieldValues.Count);
                ICsvFileLine csvLine = recordLine;

                if (_headerFieldNames is null)
                {
                    try
                    {
                        _headerFieldNames = GetHeaderFieldNames();
                    }
                    catch (Exception exception)
                    {
                        _headerFailure = ExceptionDispatchInfo.Capture(exception);
                        throw;
                    }
                }

                var fieldPlan = GetFieldPlan(recordLine);
                var fieldNames = fieldPlan.GetFieldNames(rawFieldValues.Count);
                for (var fieldIndex = 0; fieldIndex < rawFieldValues.Count; fieldIndex++)
                {
                    csvLine.Add(fieldNames[fieldIndex], rawFieldValues[fieldIndex]);
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
                            Model = recordLine,
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
                if (fieldPlan.Mapper.Count > 0)
                {
                    MapCsvFieldsToProperties(recordLine, fieldPlan);
                }

                return recordLine;
            }

            return null;
        }

        /// <summary>
        /// Reads the first line of the file as the header line, and either keeps it or, when the header line is ignored, discards it.
        /// </summary>
        /// <returns>The header line, if it is used; <c>null</c> if it is ignored or the stream is empty.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvFileHeaderLine? ReadHeaderLine()
#else
        [return: MaybeNull]
        private CsvFileHeaderLine ReadHeaderLine()
#endif
        {
            var rawFieldValues = _rawFieldValues;
            if (!TryReadRecord(rawFieldValues)) return null;

            ThrowIfNoFields(rawFieldValues);

            if (_options.HeaderHandling == CsvFileHeaderHandling.Ignore)
            {
                _headerLineIgnored = true;
                return null;
            }

            // Make sure all field names are unique.
            ThrowIfDuplicateFieldNames(rawFieldValues);

            var headerLine = new CsvFileHeaderLine();
            headerLine.EnsureFieldCapacity(rawFieldValues.Count);

            // Add field names to the line.  Key and Value will be identical because the field name (key) is the value in this case.
            foreach (var fieldValue in rawFieldValues)
            {
                headerLine.Add(fieldValue, fieldValue);
            }

            _expectedFieldCount = rawFieldValues.Count;
            _headerLine = headerLine;
            return headerLine;
        }

        /// <summary>
        /// Gets the field names that records are read with: those of the header line, if there is one, or else those the header handler gives.
        /// </summary>
        /// <returns>The field names; <c>null</c> if there is neither a header line nor a header handler.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IReadOnlyList<string>? GetHeaderFieldNames()
#else
        [return: MaybeNull]
        private IReadOnlyList<string> GetHeaderFieldNames()
#endif
        {
            var headerFieldNames = _headerLine?.FieldNames;
            if (headerFieldNames is null && _options.HeaderHandler is CsvFileHeader headerHandler)
            {
                headerFieldNames = headerHandler(_headerLine);
                if (headerFieldNames is null)
                {
                    throw new InvalidOperationException("The header handler returned null instead of a list of field names.");
                }

                // Check for duplicate names.
                ThrowIfDuplicateFieldNames(headerFieldNames);
            }

            return headerFieldNames;
        }

        private static void ThrowIfNoFields(List<string> rawFieldValues)
        {
            if (rawFieldValues.Count == 0)
            {
                // ref. RFC 4180§2¶4
                var dataItems = new Dictionary<string, object> { { DataItemName.ActualFieldCount, rawFieldValues.Count } };
                throw BuildException("Expected one or more fields.", dataItems);
            }
        }

        private void ThrowIfDuplicateFieldNames(IEnumerable<string> fieldNames)
        {
            var duplicateFieldNames = GetDuplicateFieldNames(fieldNames);
            if (duplicateFieldNames.Length > 0)
            {
                var message = string.Format(ErrorMessage.DuplicateFieldNamesTemplate, duplicateFieldNames.Length);
                var dataItems = new Dictionary<string, object> { { DataItemName.FieldNames, duplicateFieldNames } };
                throw BuildException(message, dataItems);
            }
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
                return _context ?? (_context = new CsvContext(RegisteredMappers, _options));
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
                return NoFieldNames;
#endif
            }

            var strings = fieldNames
                .GroupBy(name => name)
                .Where(grp => grp.Count() > 1)
                .Select(grp => grp.Key)
                .ToArray();
            return strings;
        }

        /// <summary>
        /// Gets the field plan for the current mapper and header field names, building a new one if either has changed since the last line.
        /// </summary>
        private FieldPlan GetFieldPlan(TRecordLine recordLine)
        {
            var mapperVersion = MapperVersion;
            var fieldPlan = _fieldPlan;
            if (fieldPlan != null && fieldPlan.MapperVersion == mapperVersion && ReferenceEquals(fieldPlan.HeaderFieldNames, _headerFieldNames))
            {
                return fieldPlan;
            }

            fieldPlan = new FieldPlan(mapperVersion, GetMapper(recordLine, out _), _headerFieldNames);
            _fieldPlan = fieldPlan;
            return fieldPlan;
        }

        private void MapCsvFieldsToProperties(TRecordLine recordLine, FieldPlan fieldPlan)
        {
            ICsvFileLine csvLine = recordLine;
            var options = (CsvFileOptions)_options;

            var isStatic = fieldPlan.GetMappings(csvLine.Count, out var fieldMappingPlans);
            foreach (var fieldMappingPlan in fieldMappingPlans)
            {
                var fieldMapping = fieldMappingPlan.Mapping;
                var mappedFieldName = fieldMappingPlan.FieldName;
                string fieldKey, valueKey;
                if (isStatic)
                {
                    if (fieldMappingPlan.IsSkipped)
                    {
                        continue;
                    }

                    fieldKey = fieldMappingPlan.FieldKey;
                    valueKey = fieldMappingPlan.ValueKey;
                }
                else
                {
                    // An earlier mapping may have added a field to this line, so check the line's fields as they are now.
                    if (!csvLine.Contains(mappedFieldName) && fieldMapping.Value.IsOptional)
                    {
                        continue;
                    }

                    fieldKey = FieldPlan.GetFieldKey(csvLine.Contains, mappedFieldName, fieldMapping.Key);
                    valueKey = FieldPlan.GetFieldKey(csvLine.Contains, CsvMappingService.GetFieldNameFromMapping(fieldMapping), fieldMapping.Key);
                }

                // Validate CSV field value.
                if (fieldMapping.Value.Validator is CsvFieldValidate<TRecordLine> validator)
                {
                    var fieldContext = new CsvFieldContext<TRecordLine>(_options)
                    {
                        Model = recordLine,
                        FieldIndex = GetFieldPosition(csvLine, fieldKey),
                        FieldName = mappedFieldName,
                        HeaderLine = _headerLine
                    };
                    if (!validator(fieldContext))
                    {
                        if (_options.BadValueHandler is CsvFieldBadValue<TRecordLine> badValueHandler)
                        {
                            badValueHandler(fieldContext);
                            continue;
                        }

                        var rawFieldValue = csvLine[fieldKey] is string stringFieldValue ? stringFieldValue : string.Empty;
                        throw BuildInvalidMappedValueException(fieldMapping, rawFieldValue, null);
                    }
                }

                // Get the mapped field value, then deserialize it.
                var fieldValue = csvLine[valueKey];
                if (fieldMapping.Value.ConstantValue != null)
                {
                    fieldValue = fieldMapping.Value.ConstantValue;
                }
                else if (fieldMapping.Value.VariableValueGenerator != null)
                {
                    fieldValue = fieldMappingPlan.Accessor.Generate(fieldMapping.Value.VariableValueGenerator);
                }

                if (!fieldMappingPlan.Accessor.TryDeserialize(_headerLine, recordLine, options, fieldMapping, fieldValue, out var propertyValue, out var error))
                {
                    // Only a string is converted, so a value that fails to convert is always one.
                    var rawFieldValue = fieldValue as string ?? string.Empty;
                    if (_options.BadValueHandler is CsvFieldBadValue<TRecordLine> badValueHandler)
                    {
                        // A constant or generated value didn't come from a field, so has no position.
                        var isFieldValue = fieldMapping.Value.ConstantValue is null && fieldMapping.Value.VariableValueGenerator is null;
                        var fieldContext = new CsvFieldContext<TRecordLine>(_options)
                        {
                            Model = recordLine,
                            FieldIndex = isFieldValue ? GetFieldPosition(csvLine, valueKey) : null,
                            FieldName = mappedFieldName,
                            HeaderLine = _headerLine,
                            RawFieldValue = rawFieldValue
                        };
                        badValueHandler(fieldContext);
                        continue;
                    }

                    throw BuildInvalidMappedValueException(fieldMapping, rawFieldValue, error);
                }

                csvLine[fieldKey] = propertyValue;
            }
        }

        /// <summary>
        /// Gets the position of a field in a line, or <c>null</c> if the line has no such field.
        /// </summary>
        private static int? GetFieldPosition(ICsvFileLine csvLine, string fieldName)
        {
            var fieldIndex = csvLine.IndexOf(fieldName);
            return fieldIndex < 0 ? (int?)null : fieldIndex;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static CsvFileException BuildInvalidMappedValueException(KeyValuePair<string, CsvFieldMapping> fieldMapping, string rawFieldValue, Exception? innerException)
#else
        private static CsvFileException BuildInvalidMappedValueException(KeyValuePair<string, CsvFieldMapping> fieldMapping, string rawFieldValue, [AllowNull] Exception innerException)
#endif
        {
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

            return BuildException(message, dataItems, innerException);
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
                        AddFieldValue(fieldValues, isInvalidValue);
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
                        AddFieldValue(fieldValues, isInvalidValue);
                        _bufferPosition++;
                        break;
                    }

                    var nextCharacter = buffer[_bufferPosition + 1];
                    if (nextCharacter == terminatorStart && IsTerminatorAt(1, terminator))
                    {
                        AddFieldValue(fieldValues, isInvalidValue);
                        _bufferPosition += 1 + terminator.Length;
                        break;
                    }

                    if (nextCharacter == fieldDelimiter)
                    {
                        AddFieldValue(fieldValues, isInvalidValue);
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

                        AddFieldValue(fieldValues, isInvalidValue);
                        break;
                    }

                    _bufferPosition += stopIndex;
                }
                else if (!EnsureAvailable(1))
                {
                    AddFieldValue(fieldValues, isInvalidValue);
                    break;
                }

                var character = buffer[_bufferPosition];

                // Check for terminator only if not inside quoted field
                // ref. RFC 4180§2¶1
                if (character == terminatorStart && IsTerminatorAt(0, terminator))
                {
                    AddFieldValue(fieldValues, isInvalidValue);
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
                    // Without a bad value handler the first violation is the only one that matters, since it throws, so a record that has one isn't
                    // checked for more.
                    if (!HasReportedViolation() && !IsPendingFieldValueWhiteSpace())
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
                    AddFieldValue(fieldValues, isInvalidValue);
                    fieldIndex++;
                    quotedValue = isInvalidValue = false;
                    _bufferPosition++;
                    _fieldValueStart = _bufferPosition;
                    continue;
                }

                _bufferPosition++;

                // Check for non-whitespace outside a quoted value.
                // ref. RFC 4180§2¶5
                if (quotedValue && !HasReportedViolation() && !char.IsWhiteSpace(character))
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

        /// <summary>
        /// Ends the current field, whether a delimiter or the end of the record ends it, and adds its value to the record.
        /// </summary>
        /// <remarks>
        /// A value that failed validation is added as an empty value, wherever it is in the record, and its text is discarded.  This method doesn't
        /// report the value: <see cref="AddInvalidFieldValue"/> records what the bad value handler is given, as each violation is found.
        /// </remarks>
        private void AddFieldValue(List<string> fieldValues, bool isInvalidValue)
        {
            if (isInvalidValue)
            {
                _fieldValueBuilder.Clear();
                fieldValues.Add(string.Empty);
                return;
            }

            fieldValues.Add(TakeFieldValue());
        }

        /// <summary>
        /// Gets whether the record already has a violation, and no more are to be recorded.
        /// </summary>
        /// <remarks>
        /// With a bad value handler, each violation is reported to it, so more are recorded.  Without one, the first violation throws once the record
        /// is read, so recording the rest would only cost memory and time: a quoted value followed by a long run of characters would otherwise
        /// record a copy of the whole value for each of them.
        /// </remarks>
        private bool HasReportedViolation()
        {
            return !(_options.BadValueHandler is CsvFieldBadValue<TRecordLine>) && _invalidFieldValues != null && _invalidFieldValues.Count > 0;
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

        /// <summary>
        /// Field names and per-mapping work for one mapper and set of header field names, worked out once rather than on every line.
        /// </summary>
        /// <remarks>
        /// A declarative mapper is shared by every reader and writer, so the plan only reads from the mapper and its field mappings and never writes to them.
        /// </remarks>
        private sealed class FieldPlan
        {
            private static readonly FieldMappingPlan[] NoMappings = new FieldMappingPlan[0];

            private readonly string[] _headerFieldNames;
            private readonly string[] _mapperFieldNames;
            private string[] _fieldNames = NoFieldNames;
            private int _mappedFieldCount = -1;
            private FieldMappingPlan[] _mappings = NoMappings;
            private bool _mappingsAreStatic;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal FieldPlan(int mapperVersion, ICsvFileMapper mapper, IReadOnlyList<string>? headerFieldNames)
#else
            internal FieldPlan(int mapperVersion, [DisallowNull] ICsvFileMapper mapper, [AllowNull] IReadOnlyList<string> headerFieldNames)
#endif
            {
                MapperVersion = mapperVersion;
                Mapper = mapper;
                HeaderFieldNames = headerFieldNames;
                _headerFieldNames = headerFieldNames?.ToArray() ?? NoFieldNames;
                _mapperFieldNames = mapper.Count > 0 ? CsvMappingService.GetSortedFieldNamesFromMapper(mapper) : NoFieldNames;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal IReadOnlyList<string>? HeaderFieldNames { get; }
#else
            [MaybeNull] internal IReadOnlyList<string> HeaderFieldNames { get; }
#endif

            internal ICsvFileMapper Mapper { get; }

            internal int MapperVersion { get; }

            /// <summary>
            /// Gets the names of the fields on a line.  Each is the header name, then the mapper name by index, then <c>Field N</c> (1-based).
            /// </summary>
            /// <returns>An array of at least <paramref name="fieldCount"/> names.</returns>
            internal string[] GetFieldNames(int fieldCount)
            {
                if (_fieldNames.Length >= fieldCount)
                {
                    return _fieldNames;
                }

                // A name depends only on its index, so names already worked out are kept.
                var fieldNames = new string[fieldCount];
                Array.Copy(_fieldNames, fieldNames, _fieldNames.Length);
                for (var fieldIndex = _fieldNames.Length; fieldIndex < fieldCount; fieldIndex++)
                {
                    fieldNames[fieldIndex] = (fieldIndex < _headerFieldNames.Length ? _headerFieldNames[fieldIndex] : null)
                                             ?? (fieldIndex < _mapperFieldNames.Length ? _mapperFieldNames[fieldIndex] : null)
                                             ?? $"Field {fieldIndex + 1}";
                }

                _fieldNames = fieldNames;
                return fieldNames;
            }

            /// <summary>
            /// Gets the plan for each mapping for a line with the given number of fields.
            /// </summary>
            /// <returns><c>true</c> if the planned field keys and skips hold for the whole line; <c>false</c> if a mapping adds a field to the line, so
            /// they must be checked against the line as each mapping is applied.</returns>
            internal bool GetMappings(int fieldCount, out FieldMappingPlan[] mappings)
            {
                if (_mappedFieldCount != fieldCount)
                {
                    BuildMappings(fieldCount);
                }

                mappings = _mappings;
                return _mappingsAreStatic;
            }

            private void BuildMappings(int fieldCount)
            {
                // The line's field names are unique, since adding a duplicate to the line would have thrown.
                var lineFieldNames = GetFieldNames(fieldCount).Take(fieldCount).ToArray();
                var lineFieldNameSet = new HashSet<string>(lineFieldNames, StringComparer.Ordinal);

                // Merge the mapper field names with the line's other field names.
                var mapperFieldNameSet = new HashSet<string>(_mapperFieldNames, StringComparer.Ordinal);
                var mergedFieldNames = _mapperFieldNames.Concat(lineFieldNames.Where(fieldName => !mapperFieldNameSet.Contains(fieldName))).ToArray();

                // The planned keys hold unless a mapping stores its value in a field the line does not have, which adds that field to the line.
                var isStatic = true;
                var mappings = new List<FieldMappingPlan>(Mapper.Count);
                foreach (var fieldMapping in Mapper)
                {
                    // Determine the name of the field the property is mapped to.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                    string? mappedFieldName = null;
#else
                    string mappedFieldName = null;
#endif
                    if (fieldMapping.Value.FieldIndex.HasValue && fieldMapping.Value.FieldIndex.Value < mergedFieldNames.Length)
                    {
                        mappedFieldName = mergedFieldNames[fieldMapping.Value.FieldIndex.Value];
                    }

                    if (mappedFieldName is null)
                    {
                        mappedFieldName = CsvMappingService.GetFieldNameFromMapping(fieldMapping);
                    }

                    var isSkipped = !lineFieldNameSet.Contains(mappedFieldName) && fieldMapping.Value.IsOptional;
                    var fieldKey = GetFieldKey(lineFieldNameSet.Contains, mappedFieldName, fieldMapping.Key);
                    var valueKey = GetFieldKey(lineFieldNameSet.Contains, CsvMappingService.GetFieldNameFromMapping(fieldMapping), fieldMapping.Key);
                    if (!isSkipped && !lineFieldNameSet.Contains(fieldKey))
                    {
                        isStatic = false;
                    }

                    // The merged list holds the field name of every mapping, so the mapped field name is always found.
                    var accessor = FieldValueAccessor.For(fieldMapping.Value.PropertyType);
                    mappings.Add(new FieldMappingPlan(fieldMapping, mappedFieldName, fieldKey, valueKey, isSkipped, accessor));
                }

                _mappings = mappings.ToArray();
                _mappingsAreStatic = isStatic;
                _mappedFieldCount = fieldCount;
            }

            /// <summary>
            /// Gets the key of a mapped field on a line: the field name if the line has it, otherwise the property key if the line has that, otherwise the field name.
            /// </summary>
            internal static string GetFieldKey(Func<string, bool> lineHasField, string fieldName, string propertyKey)
            {
                return !lineHasField(fieldName) && lineHasField(propertyKey) ? propertyKey : fieldName;
            }
        }

        /// <summary>
        /// A property mapping resolved against the field names of a line.
        /// </summary>
        private sealed class FieldMappingPlan
        {
            internal FieldMappingPlan(KeyValuePair<string, CsvFieldMapping> mapping, string fieldName, string fieldKey, string valueKey, bool isSkipped,
                FieldValueAccessor accessor)
            {
                Mapping = mapping;
                FieldName = fieldName;
                FieldKey = fieldKey;
                ValueKey = valueKey;
                IsSkipped = isSkipped;
                Accessor = accessor;
            }

            /// <summary>
            /// Gets the accessor that generates and deserializes values of the property type.
            /// </summary>
            internal FieldValueAccessor Accessor { get; }

            /// <summary>
            /// Gets the key of the field the deserialized value is stored in.
            /// </summary>
            internal string FieldKey { get; }

            /// <summary>
            /// Gets the name of the field the property is mapped to.
            /// </summary>
            internal string FieldName { get; }

            /// <summary>
            /// Gets a flag indicating if the mapping is skipped because its field is optional and missing from the line.
            /// </summary>
            internal bool IsSkipped { get; }

            internal KeyValuePair<string, CsvFieldMapping> Mapping { get; }

            /// <summary>
            /// Gets the key of the field the value to deserialize is read from.
            /// </summary>
            internal string ValueKey { get; }
        }
    }
}
