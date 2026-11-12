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
            internal const string IndexedFieldNameTakenTemplate =
                "Found no field at index {0} for property '{1}' in a record of {2} fields, and the record already has a field named '{3}'.";
            internal const string MissingIndexedFieldTemplate = "Found no field at index {0} for required property '{1}' in a record of {2} fields.";
            internal const string MissingNamedFieldTemplate = "Found no field named '{0}' for required property '{1}'.";
        }
        private const int DefaultBufferSize = 16 * 1024;
        private const char CarriageReturn = '\r';
        private const char LineFeed = '\n';
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
        private List<KeyValuePair<int, int>>? _invalidFieldValues;
        private Dictionary<int, string>? _invalidRawFieldValues;
        private readonly TextReader _reader = null!;
#else
        [MaybeNull] private CsvContext _context;
        [MaybeNull] private FieldPlan _fieldPlan;
        [MaybeNull] private ExceptionDispatchInfo _headerFailure;
        [MaybeNull] private IReadOnlyList<string> _headerFieldNames;
        [MaybeNull] private CsvFileHeaderLine _headerLine;
        [MaybeNull] private List<KeyValuePair<int, int>> _invalidFieldValues;
        [MaybeNull] private Dictionary<int, string> _invalidRawFieldValues;
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
            _options = options ?? throw new ArgumentNullException(nameof(options));

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

            // Mappings with a constant or generated value can add fields to the line, so errors report the number of fields as read.
            var recordFieldCount = csvLine.Count;
            var isStatic = fieldPlan.GetMappings(recordFieldCount, out var fieldMappingPlans, out var mappedFieldNames);

            // The mapped values stay under their field names, so a property mapped to a field with another name, or by index, reads that field.
            recordLine.SetMappedFieldNames(mappedFieldNames);
            foreach (var fieldMappingPlan in fieldMappingPlans)
            {
                var fieldMapping = fieldMappingPlan.Mapping;
                var mappedFieldName = fieldMappingPlan.FieldName;
                if (isStatic || fieldMappingPlan.IsPastLastField)
                {
                    // A mapping past the last field has no field, whatever fields earlier mappings added to the line.  Its value is added under the
                    // mapping's name, unless the record already has a field with that name, which the value would overwrite.
                    if (fieldMappingPlan.IsSkipped)
                    {
                        continue;
                    }

                    if (fieldMappingPlan.IsMissing)
                    {
                        throw BuildMissingFieldException(fieldMapping, mappedFieldName, recordFieldCount);
                    }

                    if (fieldMappingPlan.IsFieldNameTaken)
                    {
                        throw BuildIndexedFieldNameTakenException(fieldMapping, mappedFieldName, recordFieldCount);
                    }
                }
                else if (!csvLine.Contains(mappedFieldName))
                {
                    // An earlier mapping may have added a field to this line, so check the line's fields as they are now.
                    if (fieldMapping.Value.IsOptional)
                    {
                        continue;
                    }

                    if (!FieldPlan.SuppliesValue(fieldMapping.Value))
                    {
                        throw BuildMissingFieldException(fieldMapping, mappedFieldName, recordFieldCount);
                    }
                }

                // Validate CSV field value.
                if (fieldMapping.Value.Validator is CsvFieldValidate<TRecordLine> validator)
                {
                    var readFieldIndex = GetReadFieldPosition(csvLine, fieldMappingPlan, recordFieldCount);
                    var readFieldValue = GetReadFieldValue(readFieldIndex);
                    var fieldContext = new CsvFieldContext<TRecordLine>(_options)
                    {
                        Model = recordLine,
                        FieldIndex = readFieldIndex,
                        FieldName = mappedFieldName,
                        HeaderLine = _headerLine,
                        RawFieldValue = readFieldValue
                    };
                    if (!validator(fieldContext))
                    {
                        if (_options.BadValueHandler is CsvFieldBadValue<TRecordLine> badValueHandler)
                        {
                            badValueHandler(fieldContext);
                            continue;
                        }

                        var rawFieldValue = readFieldValue ?? string.Empty;
                        throw BuildInvalidMappedValueException(fieldMapping, rawFieldValue, null);
                    }
                }

                // Get the mapped field value, then deserialize it.
                var fieldValue = fieldMappingPlan.IsPastLastField ? null : csvLine[mappedFieldName];
                if (fieldMapping.Value.ConstantValue != null)
                {
                    fieldValue = fieldMapping.Value.ConstantValue;
                }
                else if (fieldMapping.Value.VariableValueGenerator != null)
                {
                    fieldValue = fieldMappingPlan.Accessor.Generate(fieldMapping.Value.VariableValueGenerator);
                }

                // A constant or generated value didn't come from a field, so has no position.  The position is only looked up for a converter, which
                // is given the field's text as it was read from the file, not a value an earlier mapping of the same field put on the line.
                var isFieldValue = !FieldPlan.SuppliesValue(fieldMapping.Value);
                var fieldIndex = isFieldValue && (fieldMapping.Value.FromStringConverter != null || fieldMapping.Value.TypeConverterType != null)
                    ? GetReadFieldPosition(csvLine, fieldMappingPlan, recordFieldCount)
                    : null;
                if (fieldIndex.HasValue)
                {
                    fieldValue = GetReadFieldValue(fieldIndex);
                }

                if (!fieldMappingPlan.Accessor.TryDeserialize(_headerLine, recordLine, options, fieldMapping, mappedFieldName, fieldIndex, fieldValue, out var propertyValue,
                        out var error))
                {
                    // Only a string is converted, so a value that fails to convert is always one.
                    var rawFieldValue = fieldValue as string ?? string.Empty;
                    if (_options.BadValueHandler is CsvFieldBadValue<TRecordLine> badValueHandler)
                    {
                        var fieldContext = new CsvFieldContext<TRecordLine>(_options)
                        {
                            Model = recordLine,
                            FieldIndex = isFieldValue ? GetReadFieldPosition(csvLine, fieldMappingPlan, recordFieldCount) : null,
                            FieldName = mappedFieldName,
                            HeaderLine = _headerLine,
                            RawFieldValue = rawFieldValue
                        };
                        badValueHandler(fieldContext);
                        continue;
                    }

                    throw BuildInvalidMappedValueException(fieldMapping, rawFieldValue, error);
                }

                csvLine[mappedFieldName] = propertyValue;
            }
        }

        /// <summary>
        /// Gets the position of the field a mapping resolved to among the fields read from the file, or <c>null</c> if the mapping is past the last
        /// field, or its field is one that an earlier mapping's constant or generated value added to the line.
        /// </summary>
        private static int? GetReadFieldPosition(ICsvFileLine csvLine, FieldMappingPlan fieldMappingPlan, int recordFieldCount)
        {
            if (fieldMappingPlan.IsPastLastField)
            {
                return null;
            }

            var fieldIndex = csvLine.IndexOf(fieldMappingPlan.FieldName);
            return fieldIndex < 0 || fieldIndex >= recordFieldCount ? (int?)null : fieldIndex;
        }

        /// <summary>
        /// Gets the text of a field as it was read from the file, which an earlier mapping of the same field doesn't change, or <c>null</c> if there
        /// is no such field.
        /// </summary>
        /// <remarks>The line's fields were added in the order they were read, so a field's position in the line is its position in the record.</remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? GetReadFieldValue(int? readFieldIndex)
#else
        [return: MaybeNull]
        private string GetReadFieldValue(int? readFieldIndex)
#endif
        {
            return readFieldIndex is int fieldIndex ? _rawFieldValues[fieldIndex] : null;
        }

        private static CsvFileException BuildIndexedFieldNameTakenException(KeyValuePair<string, CsvFieldMapping> fieldMapping, string fieldName, int fieldCount)
        {
            // The plan only marks a mapping with a field index as past the last field.
            var fieldIndex = fieldMapping.Value.FieldIndex.GetValueOrDefault();
            var message = string.Format(ErrorMessage.IndexedFieldNameTakenTemplate, fieldIndex, fieldMapping.Key, fieldCount, fieldName);
            var dataItems = new Dictionary<string, object>
            {
                { DataItemName.PropertyName, fieldMapping.Key },
                { DataItemName.FieldIndex, fieldIndex },
                { DataItemName.FieldName, fieldName },
                { DataItemName.ActualFieldCount, fieldCount }
            };
            return BuildException(message, dataItems);
        }

        private static CsvFileException BuildMissingFieldException(KeyValuePair<string, CsvFieldMapping> fieldMapping, string fieldName, int fieldCount)
        {
            var dataItems = new Dictionary<string, object>
            {
                { DataItemName.PropertyName, fieldMapping.Key },
                { DataItemName.FieldName, fieldName },
                { DataItemName.ActualFieldCount, fieldCount }
            };
            string message;
            if (fieldMapping.Value.FieldIndex is int fieldIndex)
            {
                message = string.Format(ErrorMessage.MissingIndexedFieldTemplate, fieldIndex, fieldMapping.Key, fieldCount);
                dataItems.Add(DataItemName.FieldIndex, fieldIndex);
            }
            else
            {
                message = string.Format(ErrorMessage.MissingNamedFieldTemplate, fieldName, fieldMapping.Key);
            }

            return BuildException(message, dataItems);
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
            _invalidRawFieldValues?.Clear();

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

                        // An escaped field ends with a closing quote, so a stream that ends inside one breaks the grammar.
                        // ref. RFC 4180§2 (escaped)
                        AddInvalidFieldValue(GetPendingFieldValueLength(), fieldIndex);
                        AddFieldValue(fieldValues, isInvalidValue: true);
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
                    // Skip straight to the next character that could end the field or the record, open a quote, or break the field.
#if NET8_0_OR_GREATER
                    var stopIndex = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition).IndexOfAny(unquotedStopCharacters);
#else
                    // Without SearchValues, an IndexOfAny of more than three values searches once for each value, so search for the three that end
                    // the field first, then for a line break only up to where the field ends.  Each search stops at the end of the field.
                    var unread = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition);
                    var stopIndex = unread.IndexOfAny(fieldDelimiter, QuoteCharacter, terminatorStart);
                    var lineBreakIndex = (stopIndex < 0 ? unread : unread.Slice(0, stopIndex)).IndexOfAny(CarriageReturn, LineFeed);
                    if (lineBreakIndex >= 0) stopIndex = lineBreakIndex;
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
                    // An escaped field starts with its quote, so anything before the quote, whitespace included, leaves the quote embedded in a
                    // non-escaped field, which can't hold one.  A quote that reopens a quoted value is only reached past a character that was
                    // already reported after the closing quote, and adds nothing to the raw value, so it isn't reported again.
                    // ref. RFC 4180§2 (escaped, non-escaped), RFC 4180§2¶5
                    var reopensQuotedValue = quotedValue;
                    inQuotes = true;
                    quotedValue = true;

                    if (!reopensQuotedValue && GetPendingFieldValueLength() != 0)
                    {
                        AddInvalidFieldValue(GetPendingFieldValueLength(), fieldIndex);
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

                // Only a delimiter or the end of the record may follow a closing quote, whitespace included.  A non-escaped field is TEXTDATA, which
                // holds no CR or LF, so a line break that isn't the terminator can only be in a quoted value.  Any other character that stopped the
                // search, such as a lone first character of a multi-character terminator that isn't CR or LF, is ordinary content.
                // TODO: TEXTDATA also leaves out control characters such as TAB and characters outside US-ASCII, which this doesn't check (#881).
                // ref. RFC 4180§2 (escaped, non-escaped), RFC 4180§2¶6
                if (quotedValue || character == CarriageReturn || character == LineFeed)
                {
                    AddInvalidFieldValue(GetPendingFieldValueLength(), fieldIndex);
                    isInvalidValue = true;
                }
            }

            if (_invalidFieldValues != null && _invalidRawFieldValues != null)
            {
                foreach (var invalidFieldValue in _invalidFieldValues)
                {
                    // The raw value at a violation is the start of the whole raw value, which only grows.  Taking it here, one at a time, keeps the
                    // record's memory linear in its length, however many violations there are.
                    var rawFieldValue = _invalidRawFieldValues[invalidFieldValue.Key].Substring(0, invalidFieldValue.Value);
                    HandleInvalidFieldValue(rawFieldValue, invalidFieldValue.Key);
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
                // The raw value is kept for reporting, and the value in the record is empty.
                if (_invalidRawFieldValues is null)
                {
                    _invalidRawFieldValues = new Dictionary<int, string>();
                }

                _invalidRawFieldValues[fieldValues.Count] = TakeFieldValue();
                fieldValues.Add(string.Empty);
                return;
            }

            fieldValues.Add(TakeFieldValue());
        }

        /// <summary>
        /// Records a violation in a field, as the length of the field's raw value at the point of it.
        /// </summary>
        private void AddInvalidFieldValue(int rawFieldValueLength, int fieldIndex)
        {
            if (_invalidFieldValues is null)
            {
                _invalidFieldValues = new List<KeyValuePair<int, int>>();
            }

            _invalidFieldValues.Add(new KeyValuePair<int, int>(fieldIndex, rawFieldValueLength));
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
        /// <summary>
        /// Gets the characters the search through a non-escaped field stops at: the delimiter, a quote, the first character of the terminator, and
        /// CR and LF, which a non-escaped field can't hold.
        /// </summary>
        private SearchValues<char> GetUnquotedStopCharacters(char fieldDelimiter, char terminatorStart)
        {
            if (_unquotedStopCharacters is null || _unquotedStopDelimiter != fieldDelimiter || _unquotedStopTerminatorStart != terminatorStart)
            {
                _unquotedStopCharacters = SearchValues.Create([fieldDelimiter, QuoteCharacter, terminatorStart, CarriageReturn, LineFeed]);
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
        /// Gets the length of the field value read so far.
        /// </summary>
        private int GetPendingFieldValueLength()
        {
            return _fieldValueBuilder.Length + _bufferPosition - _fieldValueStart;
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
            private static readonly Dictionary<int, string> NoMapperFieldNames = new Dictionary<int, string>();

            private readonly string[] _headerFieldNames;
            private readonly Dictionary<int, string> _mapperFieldNames;
            private string[] _fieldNames = NoFieldNames;
            private int _mappedFieldCount = -1;
            private FieldMappingPlan[] _mappings = NoMappings;
            private bool _mappingsAreStatic;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private Dictionary<string, string>? _mappedFieldNames;
#else
            private Dictionary<string, string> _mappedFieldNames;
#endif

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
                _mapperFieldNames = mapper.Count > 0 ? CsvMappingService.GetFieldNamesByPosition(mapper) : NoMapperFieldNames;
            }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal IReadOnlyList<string>? HeaderFieldNames { get; }
#else
            [MaybeNull] internal IReadOnlyList<string> HeaderFieldNames { get; }
#endif

            internal ICsvFileMapper Mapper { get; }

            internal int MapperVersion { get; }

            /// <summary>
            /// Gets the names of the fields on a line.  Each is the header name, then the field name of the mapping at its position, then
            /// <c>Field N</c> (1-based).
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
                    // A field past the header takes the name of the mapping at its position, unless the header already has that name.
                    var fieldName = fieldIndex < _headerFieldNames.Length ? _headerFieldNames[fieldIndex] : null;
                    if (fieldName is null && (!_mapperFieldNames.TryGetValue(fieldIndex, out fieldName) || Array.IndexOf(_headerFieldNames, fieldName) >= 0))
                    {
                        fieldName = $"Field {fieldIndex + 1}";
                    }

                    fieldNames[fieldIndex] = fieldName;
                }

                _fieldNames = fieldNames;
                return fieldNames;
            }

            /// <summary>
            /// Gets the plan for each mapping for a line with the given number of fields.
            /// </summary>
            /// <param name="mappedFieldNames">The field name of each mapping that maps a property to a field with another name, keyed by property key,
            /// or <c>null</c> if there are none.  Every line with this number of fields shares it, so it is never changed.</param>
            /// <returns><c>true</c> if the planned field keys and skips hold for the whole line; <c>false</c> if a mapping adds a field to the line, so
            /// they must be checked against the line as each mapping is applied.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal bool GetMappings(int fieldCount, out FieldMappingPlan[] mappings, out Dictionary<string, string>? mappedFieldNames)
#else
            internal bool GetMappings(int fieldCount, out FieldMappingPlan[] mappings, [MaybeNull] out Dictionary<string, string> mappedFieldNames)
#endif
            {
                if (_mappedFieldCount != fieldCount)
                {
                    BuildMappings(fieldCount);
                }

                mappings = _mappings;
                mappedFieldNames = _mappedFieldNames;
                return _mappingsAreStatic;
            }

            private void BuildMappings(int fieldCount)
            {
                // The line's field names are unique, since adding a duplicate to the line would have thrown.
                var lineFieldNames = GetFieldNames(fieldCount).Take(fieldCount).ToArray();
                var lineFieldNameSet = new HashSet<string>(lineFieldNames, StringComparer.Ordinal);

                // The planned keys hold unless a mapping stores its value in a field the line does not have, which adds that field to the line.
                var isStatic = true;
                var mappings = new List<FieldMappingPlan>(Mapper.Count);
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                Dictionary<string, string>? mappedFieldNames = null;
#else
                Dictionary<string, string> mappedFieldNames = null;
#endif
                foreach (var fieldMapping in Mapper)
                {
                    // A field index addresses the field at that position on the line, whatever its name.  A line without that position has no
                    // field for the mapping, even if another of its fields has the mapping's name.
                    var fieldIndex = fieldMapping.Value.FieldIndex;
                    var isPastLastField = fieldIndex.HasValue && fieldIndex.Value >= fieldCount;
                    var mappedFieldName = fieldIndex.HasValue && !isPastLastField
                        ? lineFieldNames[fieldIndex.Value]
                        : CsvMappingService.GetFieldNameFromMapping(fieldMapping);
                    if (!string.Equals(mappedFieldName, fieldMapping.Key, StringComparison.Ordinal))
                    {
                        if (mappedFieldNames is null)
                        {
                            mappedFieldNames = new Dictionary<string, string>(StringComparer.Ordinal);
                        }

                        mappedFieldNames[fieldMapping.Key] = mappedFieldName;
                    }

                    // A mapping reads only the field it is mapped to.  An optional mapping without that field is skipped; a required one is an error
                    // unless it supplies its own value, which is then added to the line under the mapping's name.
                    var hasField = !isPastLastField && lineFieldNameSet.Contains(mappedFieldName);
                    var suppliesValue = SuppliesValue(fieldMapping.Value);
                    var isSkipped = !hasField && fieldMapping.Value.IsOptional;
                    var isMissing = !hasField && !isSkipped && !suppliesValue;
                    var isFieldNameTaken = isPastLastField && !isSkipped && suppliesValue && lineFieldNameSet.Contains(mappedFieldName);
                    if (!hasField && !isSkipped)
                    {
                        isStatic = false;
                    }

                    var accessor = FieldValueAccessor.For(fieldMapping.Value.PropertyType);
                    mappings.Add(new FieldMappingPlan(fieldMapping, mappedFieldName, isSkipped, isPastLastField, isFieldNameTaken, isMissing, accessor));
                }

                // Lines already read keep the field names they were read with, so this is a new dictionary rather than a change to the old one.
                _mappedFieldNames = mappedFieldNames;
                _mappings = mappings.ToArray();
                _mappingsAreStatic = isStatic;
                _mappedFieldCount = fieldCount;
            }

            /// <summary>
            /// Gets a flag indicating if a mapping gives its property a constant or generated value, rather than reading it from a field.
            /// </summary>
            internal static bool SuppliesValue(CsvFieldMapping fieldMapping)
            {
                return fieldMapping.ConstantValue != null || fieldMapping.VariableValueGenerator != null;
            }
        }

        /// <summary>
        /// A property mapping resolved against the field names of a line.
        /// </summary>
        private sealed class FieldMappingPlan
        {
            internal FieldMappingPlan(KeyValuePair<string, CsvFieldMapping> mapping, string fieldName, bool isSkipped, bool isPastLastField,
                bool isFieldNameTaken, bool isMissing, FieldValueAccessor accessor)
            {
                Mapping = mapping;
                FieldName = fieldName;
                IsSkipped = isSkipped;
                IsPastLastField = isPastLastField;
                IsFieldNameTaken = isFieldNameTaken;
                IsMissing = isMissing;
                Accessor = accessor;
            }

            /// <summary>
            /// Gets a flag indicating if the mapping is required, reads its value from a field, and the line has no field for it.
            /// </summary>
            internal bool IsMissing { get; }

            /// <summary>
            /// Gets the accessor that generates and deserializes values of the property type.
            /// </summary>
            internal FieldValueAccessor Accessor { get; }

            /// <summary>
            /// Gets the name of the field the property is mapped to, which the value is read from and the deserialized value is stored in.
            /// </summary>
            internal string FieldName { get; }

            /// <summary>
            /// Gets a flag indicating if the mapping is skipped because it is optional and its field is missing from the line.
            /// </summary>
            internal bool IsSkipped { get; }

            /// <summary>
            /// Gets a flag indicating if the mapping has a field index past the last field of the line, so the line has no field for it.
            /// </summary>
            internal bool IsPastLastField { get; }

            /// <summary>
            /// Gets a flag indicating if the mapping is past the last field of the line, supplies its own value, and the line already has a field
            /// with the mapping's name.
            /// </summary>
            internal bool IsFieldNameTaken { get; }

            internal KeyValuePair<string, CsvFieldMapping> Mapping { get; }
        }
    }
}
