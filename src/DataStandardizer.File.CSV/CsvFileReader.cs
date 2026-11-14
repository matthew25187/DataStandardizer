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
            internal const string HeaderHandlerFieldCountTemplate =
                "The header handler returned {0} field names for a header line of {1} fields.  It can rename the fields, but not add or remove them.";
            internal const string HeaderHandlerFirstRecordFieldCountTemplate =
                "The header handler returned {0} field names for a first record of {1} fields.  It can rename the fields, but not add or remove them.";
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
        private CsvFileHeaderLine? _ignoredHeaderLine;
        private List<KeyValuePair<int, int>>? _invalidFieldValues;
        private Dictionary<int, string>? _invalidRawFieldValues;
        private readonly TextReader _reader = null!;
        private CsvTextData? _textData;
        private Encoding? _textDataEncoding;
#else
        [MaybeNull] private CsvContext _context;
        [MaybeNull] private FieldPlan _fieldPlan;
        [MaybeNull] private ExceptionDispatchInfo _headerFailure;
        [MaybeNull] private IReadOnlyList<string> _headerFieldNames;
        [MaybeNull] private CsvFileHeaderLine _headerLine;
        [MaybeNull] private CsvFileHeaderLine _ignoredHeaderLine;
        [MaybeNull] private List<KeyValuePair<int, int>> _invalidFieldValues;
        [MaybeNull] private Dictionary<int, string> _invalidRawFieldValues;
        [NotNull] private readonly TextReader _reader;
        [MaybeNull] private CsvTextData _textData;
        [MaybeNull] private Encoding _textDataEncoding;
#endif
#if NET8_0_OR_GREATER
        private SearchValues<char>? _quotedStopCharacters;
        private bool _quotedStopExcept;
        private bool _quotedStopC1Separately;
        private SearchValues<char>? _unquotedStopCharacters;
        private bool _unquotedStopExcept;
        private bool _unquotedStopC1Separately;
        private char _stopDelimiter;
        private CsvTextData? _stopTextData;
        private string _stopTerminator = string.Empty;
#else
        private char[] _quotedExemptCharacters = NoCharacters;
        private char _quotedExemptDelimiter;
        private string _quotedExemptTerminator = string.Empty;
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

            ValidateHandlers(options, nameof(options));

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

            ValidateHandlers(options, nameof(options));

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

            ValidateHandlers(_options, nameof(options));

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

        /// <summary>
        /// Checks that each handler set in the options is of the type the reader calls.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <see cref="ICsvFileOptions.BadValueHandler"/> isn't a <see cref="CsvFieldBadValue{TModel}"/> of <typeparamref name="TRecordLine"/>,
        /// <see cref="ICsvFileOptions.InconsistentFieldCountHandler"/> isn't a <see cref="CsvFieldCount{TModel}"/> of <typeparamref name="TRecordLine"/>,
        /// or <see cref="ICsvFileOptions.HeaderHandler"/> isn't a <see cref="CsvFileHeader"/>.
        /// </exception>
        private static void ValidateHandlers(ICsvFileOptions options, string paramName)
        {
            ValidateHandler<CsvFieldBadValue<TRecordLine>>(options.BadValueHandler, nameof(ICsvFileOptions.BadValueHandler), paramName);
            ValidateHandler<CsvFieldCount<TRecordLine>>(options.InconsistentFieldCountHandler, nameof(ICsvFileOptions.InconsistentFieldCountHandler), paramName);
            ValidateHandler<CsvFileHeader>(options.HeaderHandler, nameof(ICsvFileOptions.HeaderHandler), paramName);
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
        /// The <see cref="CsvFileOptions.HeaderHandler"/> delegate returned <c>null</c>, or a list that contains <c>null</c>.
        /// -or-
        /// The <see cref="CsvFileOptions.HeaderHandler"/> delegate returned a different number of field names from the number of fields
        /// on the file's header line or, when the file has no header line or it is ignored, on its first record.
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
                        _headerFieldNames = GetHeaderFieldNames(rawFieldValues.Count);
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
        /// Reads the first line of the file as the header line.  When the header line is used, the reader keeps the line it takes the field
        /// names from: the header line itself or, when a header handler is set, a line of the names the handler returns for it, which must be
        /// as many as the header line's fields.  When the header line is ignored, the reader discards it, or keeps it to pass to the header
        /// handler as it reads the first record.
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

            var headerHandler = _options.HeaderHandler as CsvFileHeader;
            if (_options.HeaderHandling == CsvFileHeaderHandling.Ignore && headerHandler == null)
            {
                _headerLineIgnored = true;
                return null;
            }

            // Make sure all field names are unique.
            ThrowIfDuplicateFieldNames(rawFieldValues);

            var headerLine = CreateHeaderLine(rawFieldValues);

            // An ignored header line is kept only to pass to the header handler, which is called as the first record is read, as for a file
            // without a header line.
            if (_options.HeaderHandling == CsvFileHeaderHandling.Ignore)
            {
                _ignoredHeaderLine = headerLine;
                _headerLineIgnored = true;
                return null;
            }

            if (headerHandler != null)
            {
                // The handler renames the fields of the header line; the line, not the handler, sets how many fields there are.
                var headerFieldNames =
                    CallHeaderHandler(headerHandler, headerLine, rawFieldValues.Count, ErrorMessage.HeaderHandlerFieldCountTemplate);
                headerLine = CreateHeaderLine(headerFieldNames);
            }

            _expectedFieldCount = rawFieldValues.Count;
            _headerLine = headerLine;
            return headerLine;
        }

        /// <summary>
        /// Creates a header line whose fields are the given field names.
        /// </summary>
        private static CsvFileHeaderLine CreateHeaderLine(IReadOnlyList<string> fieldNames)
        {
            var headerLine = new CsvFileHeaderLine();
            headerLine.EnsureFieldCapacity(fieldNames.Count);

            // Add field names to the line.  Key and Value will be identical because the field name (key) is the value in this case.
            foreach (var fieldName in fieldNames)
            {
                headerLine.Add(fieldName, fieldName);
            }

            return headerLine;
        }

        /// <summary>
        /// Calls the header handler and checks the field names it returns.
        /// </summary>
        /// <param name="headerHandler">The header handler.</param>
        /// <param name="headerLine">The header line read from the file; <c>null</c> if the file has no header line.</param>
        /// <param name="expectedFieldCount">The number of fields the handler must name: those of the header line or of the first record.</param>
        /// <param name="fieldCountTemplate">The message template for a list with a different number of names, which takes the number of names
        /// and <paramref name="expectedFieldCount"/>.</param>
        /// <returns>The field names the header handler returned.</returns>
        /// <exception cref="InvalidOperationException">The header handler returned <c>null</c>, a list with a <c>null</c> name, or a different
        /// number of names from <paramref name="expectedFieldCount"/>.</exception>
        /// <exception cref="CsvFileException">The field names include duplicates.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IReadOnlyList<string> CallHeaderHandler(CsvFileHeader headerHandler, CsvFileHeaderLine? headerLine, int expectedFieldCount,
            string fieldCountTemplate)
#else
        private IReadOnlyList<string> CallHeaderHandler(CsvFileHeader headerHandler, [AllowNull] CsvFileHeaderLine headerLine, int expectedFieldCount,
            string fieldCountTemplate)
#endif
        {
            var headerFieldNames = headerHandler(headerLine);
            if (headerFieldNames is null)
            {
                throw new InvalidOperationException("The header handler returned null instead of a list of field names.");
            }

            if (headerFieldNames.Any(fieldName => fieldName == null))
            {
                throw new InvalidOperationException("The header handler returned a list of field names that contains null.");
            }

            if (headerFieldNames.Count != expectedFieldCount)
            {
                throw new InvalidOperationException(string.Format(fieldCountTemplate, headerFieldNames.Count, expectedFieldCount));
            }

            // Check for duplicate names.
            ThrowIfDuplicateFieldNames(headerFieldNames);

            return headerFieldNames;
        }

        /// <summary>
        /// Gets the field names that records are read with when the reader hasn't already got them from the header line: those the
        /// header handler gives for a file without a header line, or whose header line is ignored.
        /// </summary>
        /// <param name="firstRecordFieldCount">The number of fields of the first record, which has been read but not yet processed.</param>
        /// <returns>The field names; <c>null</c> if there is neither a header line nor a header handler.</returns>
        /// <exception cref="InvalidOperationException">The header handler returned <c>null</c>, a list with a <c>null</c> name, or a different
        /// number of field names from the number of fields of the first record.</exception>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private IReadOnlyList<string>? GetHeaderFieldNames(int firstRecordFieldCount)
#else
        [return: MaybeNull]
        private IReadOnlyList<string> GetHeaderFieldNames(int firstRecordFieldCount)
#endif
        {
            if (_headerLine != null)
            {
                return _headerLine.FieldNames;
            }

            if (!(_options.HeaderHandler is CsvFileHeader headerHandler))
            {
                return null;
            }

            // Without a header line, or with one that is ignored, the first record sets how many fields there are, and the handler names them.
            var ignoredHeaderLine = _ignoredHeaderLine;
            _ignoredHeaderLine = null;
            return CallHeaderHandler(headerHandler, ignoredHeaderLine, firstRecordFieldCount, ErrorMessage.HeaderHandlerFirstRecordFieldCountTemplate);
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
            fieldPlan.GetMappings(recordFieldCount, out var fieldMappingPlans, out var mappedFieldNames, out var sharedFieldLayout);

            // The mapped values stay under their field names, so a property mapped to a field with another name, or by index, reads that field.
            recordLine.SetMappedFieldNames(mappedFieldNames);
            recordLine.SetSharedFieldLayout(sharedFieldLayout);
            foreach (var fieldMappingPlan in fieldMappingPlans)
            {
                // A mapping reads only a field read from the file, never one that another mapping's constant or generated value added to the line.
                // A mapping past the last field has no field.  Its value is added under the mapping's name, unless the record already has a field
                // with that name, which the value would overwrite.
                var fieldMapping = fieldMappingPlan.Mapping;
                var mappedFieldName = fieldMappingPlan.FieldName;
                if (fieldMappingPlan.IsSkipped)
                {
                    // Another mapping of the same field may add it to the line, but its value isn't this mapping's.
                    if (fieldMappingPlan.SharedFieldSlot >= 0)
                    {
                        recordLine.MarkSkippedSharedField(fieldMappingPlan.SharedFieldSlot);
                    }

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

                // Each mapping is given the field's text as it was read from the file, not a value that an earlier mapping of the same field put
                // on the line, so mappings that share a field each convert it to their own type.
                var readFieldIndex = fieldMappingPlan.ReadFieldIndex;
                var readFieldValue = GetReadFieldValue(readFieldIndex);

                // Validate CSV field value.
                if (fieldMapping.Value.Validator is CsvFieldValidate<TRecordLine> validator)
                {
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
                            // Marked first, so a value the handler sets through the model is the one the line keeps.
                            recordLine.MarkBadValueField(mappedFieldName, fieldMappingPlan.SharedFieldSlot);
                            badValueHandler(fieldContext);
                            continue;
                        }

                        var rawFieldValue = readFieldValue ?? string.Empty;
                        throw BuildInvalidMappedValueException(fieldMapping, rawFieldValue, null);
                    }
                }

                // Get the mapped field value, then deserialize it.
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                object? fieldValue = readFieldValue;
#else
                object fieldValue = readFieldValue;
#endif
                if (fieldMapping.Value.ConstantValue != null)
                {
                    // A constant the property can't hold is a mistake in the mapping, not in the file, so no bad-value handler is given it.
                    if (!fieldMappingPlan.IsConstantConvertible)
                    {
                        throw CsvConstantValue.BuildNotConvertibleException(fieldMapping);
                    }

                    fieldValue = fieldMappingPlan.ConstantValue;
                }
                else if (fieldMapping.Value.VariableValueGenerator != null)
                {
                    fieldValue = fieldMappingPlan.Accessor.Generate(fieldMapping.Value.VariableValueGenerator);
                }

                // A constant or generated value didn't come from a field, so has no position.  The position is only given to a converter.
                var isFieldValue = !fieldMapping.Value.SuppliesValue;
                var fieldIndex = isFieldValue && CsvMapperCache.HasFromStringConverter(fieldMapping.Value) ? readFieldIndex : null;
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
                            FieldIndex = isFieldValue ? readFieldIndex : null,
                            FieldName = mappedFieldName,
                            HeaderLine = _headerLine,
                            RawFieldValue = rawFieldValue
                        };
                        // Marked first, so a value the handler sets through the model is the one the line keeps.
                        recordLine.MarkBadValueField(mappedFieldName, fieldMappingPlan.SharedFieldSlot);
                        badValueHandler(fieldContext);
                        continue;
                    }

                    throw BuildInvalidMappedValueException(fieldMapping, rawFieldValue, error);
                }

                // The field holds the value of the last mapping that shares it; each of them keeps its own value for its property.
                recordLine.SetMappedFieldValue(mappedFieldName, propertyValue, fieldMappingPlan.SharedFieldSlot);
            }
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

            _fieldValueBuilder.Clear();
            _fieldValueStart = _bufferPosition;

            // A final record without a terminator is returned, but an empty remainder of the stream is not.
            // ref. RFC 4180§2¶2
            if (!EnsureAvailable(1)) return false;

            // The characters a field may hold come from the reader's encoding, which a StreamReader settles once it has read the start of the stream.
            // ref. RFC 4180§2 (TEXTDATA), RFC 4180§3 (charset)
            var textData = GetTextData();
#if NET8_0_OR_GREATER
            UpdateStopCharacters(fieldDelimiter, terminator, textData);
            var quotedStopCharacters = _quotedStopCharacters!;
            var quotedStopExcept = _quotedStopExcept;
            var quotedStopC1Separately = _quotedStopC1Separately;
            var unquotedStopCharacters = _unquotedStopCharacters!;
            var unquotedStopExcept = _unquotedStopExcept;
            var unquotedStopC1Separately = _unquotedStopC1Separately;
#else
            var quotedExemptCharacters = GetQuotedExemptCharacters(fieldDelimiter, terminator);
#endif

            bool inQuotes = false, quotedValue = false, isInvalidValue = false, isTextDataReported = false;
            var fieldIndex = 0;

            while (true)
            {
                if (inQuotes)
                {
                    // Skip straight to the next quote, or to a character that an escaped field can't hold.
                    // ref. RFC 4180§2 (escaped, TEXTDATA)
#if NET8_0_OR_GREATER
                    var quotedUnread = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition);
                    var quoteIndex = quotedStopExcept ? quotedUnread.IndexOfAnyExcept(quotedStopCharacters) : quotedUnread.IndexOfAny(quotedStopCharacters);
                    if (quotedStopC1Separately)
                    {
                        var c1Index = CsvTextData.IndexOfC1Control(quoteIndex < 0 ? quotedUnread : quotedUnread.Slice(0, quoteIndex));
                        if (c1Index >= 0) quoteIndex = c1Index;
                    }
#else
                    var quotedUnread = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition);
                    var quoteIndex = quotedUnread.IndexOf(QuoteCharacter);
                    var checkIndex = textData.IndexOfCharacterToCheck(quoteIndex < 0 ? quotedUnread : quotedUnread.Slice(0, quoteIndex), quotedExemptCharacters);
                    if (checkIndex >= 0) quoteIndex = checkIndex;
#endif
                    if (quoteIndex >= 0 && buffer[_bufferPosition + quoteIndex] != QuoteCharacter)
                    {
                        // The character stays in the value.  If it isn't in TEXTDATA, the first such in the field is reported with the value read up
                        // to and including it.
                        _bufferPosition += quoteIndex;
                        if (!PassCharacter(textData, fieldIndex, ref isTextDataReported)) isInvalidValue = true;
                        continue;
                    }

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
                        quotedValue = isInvalidValue = isTextDataReported = false;
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
                    // Skip straight to the next character that could end the field or the record, open a quote, or break the field, as a
                    // character outside TEXTDATA does.
                    // ref. RFC 4180§2 (non-escaped, TEXTDATA)
#if NET8_0_OR_GREATER
                    var unread = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition);
                    var stopIndex = unquotedStopExcept ? unread.IndexOfAnyExcept(unquotedStopCharacters) : unread.IndexOfAny(unquotedStopCharacters);
                    if (unquotedStopC1Separately)
                    {
                        var c1Index = CsvTextData.IndexOfC1Control(stopIndex < 0 ? unread : unread.Slice(0, stopIndex));
                        if (c1Index >= 0) stopIndex = c1Index;
                    }
#else
                    // Without SearchValues, an IndexOfAny of more than three values searches once for each value, so search for the three that end
                    // the field first, then for a line break, then for a character outside TEXTDATA, each only up to where the search before it
                    // stopped.
                    var unread = buffer.AsSpan(_bufferPosition, _bufferEnd - _bufferPosition);
                    var stopIndex = unread.IndexOfAny(fieldDelimiter, QuoteCharacter, terminatorStart);
                    var lineBreakIndex = (stopIndex < 0 ? unread : unread.Slice(0, stopIndex)).IndexOfAny(CarriageReturn, LineFeed);
                    if (lineBreakIndex >= 0) stopIndex = lineBreakIndex;
                    var checkIndex = textData.IndexOfCharacterToCheck(stopIndex < 0 ? unread : unread.Slice(0, stopIndex), NoCharacters);
                    if (checkIndex >= 0) stopIndex = checkIndex;
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
                    quotedValue = isInvalidValue = isTextDataReported = false;
                    _bufferPosition++;
                    _fieldValueStart = _bufferPosition;
                    continue;
                }

                // Only a delimiter or the end of the record may follow a closing quote, whitespace included.  A non-escaped field is TEXTDATA, which
                // holds no CR or LF, so a line break that isn't the terminator can only be in a quoted value, and holds no other control character
                // either, nor a character outside the reader's character set.  Any other character that stopped the search, such as a lone first
                // character of a multi-character terminator that is in TEXTDATA, is ordinary content.
                // ref. RFC 4180§2 (escaped, non-escaped, TEXTDATA), RFC 4180§2¶6
                if (quotedValue || character == CarriageReturn || character == LineFeed)
                {
                    _bufferPosition++;
                    AddInvalidFieldValue(GetPendingFieldValueLength(), fieldIndex);
                    isInvalidValue = true;
                }
                else if (!PassCharacter(textData, fieldIndex, ref isTextDataReported))
                {
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
        /// Moves past the character at the current position, and records it as a violation if it isn't in TEXTDATA and is the first such in the field.
        /// </summary>
        /// <returns><c>true</c> if the character is in TEXTDATA; <c>false</c> if it isn't.</returns>
        /// <remarks>
        /// A field whose value is already invalid can't become valid again, so only its first character outside TEXTDATA is reported.  A run of such
        /// characters then costs one report, not one for each character, each with a copy of the value up to it.  A surrogate pair is one character,
        /// so the position moves past both halves and the value reported never ends in half a pair.
        /// </remarks>
        private bool PassCharacter(CsvTextData textData, int fieldIndex, ref bool isTextDataReported)
        {
            var character = _buffer[_bufferPosition++];
            bool isAllowed;
            if (char.IsHighSurrogate(character) && EnsureAvailable(1) && char.IsLowSurrogate(_buffer[_bufferPosition]))
            {
                isAllowed = textData.IsAllowed(character, _buffer[_bufferPosition++]);
            }
            else
            {
                isAllowed = textData.IsAllowed(character);
            }

            if (isAllowed || isTextDataReported) return isAllowed;

            AddInvalidFieldValue(GetPendingFieldValueLength(), fieldIndex);
            isTextDataReported = true;
            return false;
        }

        /// <summary>
        /// Gets the characters a field may hold, from the reader's encoding, again only if the encoding has changed.
        /// </summary>
        private CsvTextData GetTextData()
        {
            if (_reader is StreamReader streamReader)
            {
                var encoding = streamReader.CurrentEncoding;
                if (_textData is null || !ReferenceEquals(encoding, _textDataEncoding))
                {
                    _textData = CsvTextData.ForEncoding(encoding);
                    _textDataEncoding = encoding;
                }

                return _textData;
            }

            return _textData ?? (_textData = CsvTextData.ForReader(_reader));
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
        /// Builds the characters the searches through a field stop at, or pass over, again only if the options they come from change.
        /// </summary>
        /// <remarks>
        /// The search through a non-escaped field stops at the delimiter, a quote, the first character of the terminator, and any character the
        /// field can't hold: CR and LF, and the characters to check against TEXTDATA.  The search through an escaped field
        /// stops at a quote and at any character to check, which doesn't include the delimiter, CR, LF or the characters of the
        /// terminator.
        /// </remarks>
        private void UpdateStopCharacters(char fieldDelimiter, string terminator, CsvTextData textData)
        {
            if (_unquotedStopCharacters != null && _stopDelimiter == fieldDelimiter && ReferenceEquals(_stopTextData, textData)
                && string.Equals(_stopTerminator, terminator, StringComparison.Ordinal))
            {
                return;
            }

            var unquotedCharacters = textData.GetSearchCharacters(NoCharacters,
                [fieldDelimiter, QuoteCharacter, terminator[0], CarriageReturn, LineFeed], out _unquotedStopExcept, out _unquotedStopC1Separately);
            _unquotedStopCharacters = SearchValues.Create(unquotedCharacters);
            var quotedCharacters = textData.GetSearchCharacters(CsvTextData.GetEscapedFieldCharacters(fieldDelimiter, terminator),
                [QuoteCharacter], out _quotedStopExcept, out _quotedStopC1Separately);
            _quotedStopCharacters = SearchValues.Create(quotedCharacters);
            _stopDelimiter = fieldDelimiter;
            _stopTextData = textData;
            _stopTerminator = terminator;
        }
#else
        /// <summary>
        /// Gets the characters outside TEXTDATA that an escaped field holds whatever the options, built again only if the options they come from
        /// change.
        /// </summary>
        private char[] GetQuotedExemptCharacters(char fieldDelimiter, string terminator)
        {
            if (_quotedExemptCharacters.Length == 0 || _quotedExemptDelimiter != fieldDelimiter
                || !string.Equals(_quotedExemptTerminator, terminator, StringComparison.Ordinal))
            {
                _quotedExemptCharacters = CsvTextData.GetEscapedFieldCharacters(fieldDelimiter, terminator);
                _quotedExemptDelimiter = fieldDelimiter;
                _quotedExemptTerminator = terminator;
            }

            return _quotedExemptCharacters;
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

            // Every header and mapping name, then each Field N name in the order GetFieldNames gives them out.  It is only ever added to, so
            // _fieldNames can only be extended, never rebuilt.
            private readonly HashSet<string> _takenFieldNames;
            private string[] _fieldNames = NoFieldNames;
            private int _mappedFieldCount = -1;
            private FieldMappingPlan[] _mappings = NoMappings;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private Dictionary<string, string>? _mappedFieldNames;
            private CsvSharedFieldLayout? _sharedFieldLayout;
#else
            private Dictionary<string, string> _mappedFieldNames;
            private CsvSharedFieldLayout _sharedFieldLayout;
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
                _takenFieldNames = new HashSet<string>(_headerFieldNames, StringComparer.Ordinal);
                _mapperFieldNames = mapper.Count > 0 ? GetMapperFieldNames(mapper, _headerFieldNames.Length, _takenFieldNames) : NoMapperFieldNames;
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
            /// <c>Field N</c> (1-based).  A name already taken by the header or another position is never used again: a mapping's name that is
            /// taken gives way to <c>Field N</c>, and a <c>Field N</c> that is taken becomes the first of <c>Field N (2)</c>,
            /// <c>Field N (3)</c> and so on that is free.
            /// </summary>
            /// <returns>An array of at least <paramref name="fieldCount"/> unique names.</returns>
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
                    fieldNames[fieldIndex] = fieldIndex < _headerFieldNames.Length ? _headerFieldNames[fieldIndex] : GetFieldNamePastHeader(fieldIndex);
                }

                _fieldNames = fieldNames;
                return fieldNames;
            }

            /// <summary>
            /// Gets the field name of each mapping past the header, by position, that the header and every earlier position don't already have.
            /// Each name is added to <paramref name="takenFieldNames"/>, so it is kept for its position before any <c>Field N</c> name is chosen.
            /// </summary>
            private static Dictionary<int, string> GetMapperFieldNames(ICsvFileMapper mapper, int headerFieldCount, HashSet<string> takenFieldNames)
            {
                var mapperFieldNames = new Dictionary<int, string>();
                foreach (var position in CsvMappingService.GetFieldNamesByPosition(mapper).OrderBy(position => position.Key))
                {
                    if (position.Key >= headerFieldCount && takenFieldNames.Add(position.Value))
                    {
                        mapperFieldNames.Add(position.Key, position.Value);
                    }
                }

                return mapperFieldNames;
            }

            /// <summary>
            /// Gets the name of a field past the header: the mapping name kept for its position, or else a <c>Field N</c> name.
            /// </summary>
            private string GetFieldNamePastHeader(int fieldIndex)
            {
                return _mapperFieldNames.TryGetValue(fieldIndex, out var fieldName) ? fieldName : TakePositionalFieldName(fieldIndex);
            }

            /// <summary>
            /// Takes the first of <c>Field N</c>, <c>Field N (2)</c>, <c>Field N (3)</c> and so on that isn't already taken.
            /// </summary>
            private string TakePositionalFieldName(int fieldIndex)
            {
                var positionalFieldName = $"Field {fieldIndex + 1}";
                var fieldName = positionalFieldName;
                for (var suffix = 2; !_takenFieldNames.Add(fieldName); suffix++)
                {
                    fieldName = $"{positionalFieldName} ({suffix})";
                }

                return fieldName;
            }

            /// <summary>
            /// Gets the plan for each mapping for a line with the given number of fields.
            /// </summary>
            /// <param name="mappedFieldNames">The field name of each mapping that maps a property to a field with another name, keyed by property key,
            /// or <c>null</c> if there are none.  Every line with this number of fields shares it, so it is never changed.</param>
            /// <param name="sharedFieldLayout">The slots of the mappings that share a field with another mapping, or <c>null</c> if there are none.
            /// Every line with this number of fields shares it, so it is never changed.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal void GetMappings(int fieldCount, out FieldMappingPlan[] mappings, out Dictionary<string, string>? mappedFieldNames,
                out CsvSharedFieldLayout? sharedFieldLayout)
#else
            internal void GetMappings(int fieldCount, out FieldMappingPlan[] mappings, [MaybeNull] out Dictionary<string, string> mappedFieldNames,
                [MaybeNull] out CsvSharedFieldLayout sharedFieldLayout)
#endif
            {
                if (_mappedFieldCount != fieldCount)
                {
                    BuildMappings(fieldCount);
                }

                mappings = _mappings;
                mappedFieldNames = _mappedFieldNames;
                sharedFieldLayout = _sharedFieldLayout;
            }

            private void BuildMappings(int fieldCount)
            {
                // The line's field names are unique, since GetFieldNames never gives two positions the same name.
                var lineFieldNames = GetFieldNames(fieldCount).Take(fieldCount).ToArray();
                var lineFieldNameSet = new HashSet<string>(lineFieldNames, StringComparer.Ordinal);

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

                    // A mapping reads only the field it is mapped to, among the fields read from the file; a field that another mapping's value
                    // adds to the line isn't one.  An optional mapping without that field is skipped; a required one is an error unless it supplies
                    // its own value, which is then added to the line under the mapping's name.
                    var hasField = !isPastLastField && lineFieldNameSet.Contains(mappedFieldName);
                    var suppliesValue = fieldMapping.Value.SuppliesValue;
                    var isSkipped = !hasField && fieldMapping.Value.IsOptional;
                    var isMissing = !hasField && !isSkipped && !suppliesValue;
                    var isFieldNameTaken = isPastLastField && !isSkipped && suppliesValue && lineFieldNameSet.Contains(mappedFieldName);
                    int? readFieldIndex = hasField ? Array.IndexOf(lineFieldNames, mappedFieldName) : (int?)null;
                    var accessor = CsvFieldValueAccessor<TRecordLine>.For(fieldMapping.Value.PropertyType);
                    mappings.Add(new FieldMappingPlan(fieldMapping, mappedFieldName, readFieldIndex, isSkipped, isFieldNameTaken, isMissing, accessor));
                }

                // Lines already read keep the field names and layout they were read with, so these are new rather than changes to the old ones.
                _mappedFieldNames = mappedFieldNames;
                _sharedFieldLayout = BuildSharedFieldLayout(mappings);
                _mappings = mappings.ToArray();
                _mappedFieldCount = fieldCount;
            }

            /// <summary>
            /// Gives each mapping that shares its field with another mapping a slot, in which a line keeps the value its property reads.  The line
            /// holds one value for each field, so properties of different types can't all read theirs from it.
            /// </summary>
            /// <returns>The layout of the slots, or <c>null</c> if no mapping shares its field.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            private static CsvSharedFieldLayout? BuildSharedFieldLayout(List<FieldMappingPlan> mappings)
#else
            [return: MaybeNull]
            private static CsvSharedFieldLayout BuildSharedFieldLayout(List<FieldMappingPlan> mappings)
#endif
            {
                var sharedMappings = mappings
                    .GroupBy(mapping => mapping.FieldName, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .SelectMany(group => group)
                    .ToArray();
                if (sharedMappings.Length == 0)
                {
                    return null;
                }

                var slotsByPropertyKey = new Dictionary<string, int>(sharedMappings.Length, StringComparer.Ordinal);
                var fieldNames = new string[sharedMappings.Length];
                for (var sharedFieldSlot = 0; sharedFieldSlot < sharedMappings.Length; sharedFieldSlot++)
                {
                    var mapping = sharedMappings[sharedFieldSlot];
                    mapping.SharedFieldSlot = sharedFieldSlot;
                    slotsByPropertyKey.Add(mapping.Mapping.Key, sharedFieldSlot);
                    fieldNames[sharedFieldSlot] = mapping.FieldName;
                }

                return new CsvSharedFieldLayout(slotsByPropertyKey, fieldNames);
            }
        }

        /// <summary>
        /// A property mapping resolved against the field names of a line.
        /// </summary>
        private sealed class FieldMappingPlan
        {
            internal FieldMappingPlan(KeyValuePair<string, CsvFieldMapping> mapping, string fieldName, int? readFieldIndex, bool isSkipped,
                bool isFieldNameTaken, bool isMissing, CsvFieldValueAccessor<TRecordLine> accessor)
            {
                Mapping = mapping;
                FieldName = fieldName;
                ReadFieldIndex = readFieldIndex;
                IsSkipped = isSkipped;
                IsFieldNameTaken = isFieldNameTaken;
                IsMissing = isMissing;
                Accessor = accessor;

                // The constant is converted once, here, rather than for each line.
                var constantValue = mapping.Value.ConstantValue;
                IsConstantConvertible = constantValue is null || CsvConstantValue.TryConvert(constantValue, mapping.Value.PropertyType, out constantValue);
                ConstantValue = constantValue;
            }

            /// <summary>
            /// Gets the mapping's constant value converted to the property type, or <c>null</c> if it has none or it can't be converted.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal object? ConstantValue { get; }
#else
            [MaybeNull]
            internal object ConstantValue { get; }
#endif

            /// <summary>
            /// Gets a flag indicating if the mapping has no constant value, or one that converts to the property type.
            /// </summary>
            internal bool IsConstantConvertible { get; }

            /// <summary>
            /// Gets the position of the mapping's field among the fields read from the file, or <c>null</c> if no field read from the file is the
            /// mapping's.  The line's fields are added in the order they are read, and a mapping only ever adds a field after them, so a field's
            /// position on the line is its position in the record.
            /// </summary>
            internal int? ReadFieldIndex { get; }

            /// <summary>
            /// Gets the mapping's slot in the plan's shared field layout, or <c>-1</c> if no other mapping resolves to the same field.  Set once, as
            /// the plan is built.
            /// </summary>
            internal int SharedFieldSlot { get; set; } = -1;

            /// <summary>
            /// Gets a flag indicating if the mapping is required, reads its value from a field, and the line has no field for it.
            /// </summary>
            internal bool IsMissing { get; }

            /// <summary>
            /// Gets the accessor that generates and deserializes values of the property type.
            /// </summary>
            internal CsvFieldValueAccessor<TRecordLine> Accessor { get; }

            /// <summary>
            /// Gets the name of the field the property is mapped to, which the value is read from and the deserialized value is stored in.
            /// </summary>
            internal string FieldName { get; }

            /// <summary>
            /// Gets a flag indicating if the mapping is skipped because it is optional and its field is missing from the line.
            /// </summary>
            internal bool IsSkipped { get; }

            /// <summary>
            /// Gets a flag indicating if the mapping is past the last field of the line, supplies its own value, and the line already has a field
            /// with the mapping's name.
            /// </summary>
            internal bool IsFieldNameTaken { get; }

            internal KeyValuePair<string, CsvFieldMapping> Mapping { get; }
        }
    }
}
