using System;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
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
            internal const string FieldName = "FieldName";
            internal const string FieldValue = "FieldValue";
        }
        private const char QuoteCharacter = '"';

        /// <summary>
        /// Size of the buffers of the <see cref="StreamWriter"/> (in characters) and <see cref="FileStream"/> (in bytes)
        /// the writer creates itself.  A caller-supplied <see cref="TextWriter"/> keeps its own buffering.
        /// </summary>
        internal const int StreamBufferSize = 16 * 1024;

        /// <summary>
        /// Encoding used when none is specified, matching the default of <see cref="StreamWriter"/>: UTF-8 without a
        /// byte order mark, throwing on invalid characters.
        /// </summary>
        private static readonly Encoding DefaultEncoding = new UTF8Encoding(false, true);
        private static readonly char[] LineBreakCharacters = { '\r', '\n' };
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private static readonly char[] NoCharacters = new char[0];
#endif
        private static readonly string[] NoFieldNames = new string[0];

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private FieldPlan? _fieldPlan;
        private CsvFileHeaderLine? _headerLine;
        private string? _quoteSearchEmbeddedLineBreak;
#else
        [MaybeNull] private FieldPlan _fieldPlan;
        [MaybeNull] private CsvFileHeaderLine _headerLine;
        [MaybeNull] private string _quoteSearchEmbeddedLineBreak;
#endif
#if NET8_0_OR_GREATER
        private SearchValues<char>? _quoteSearchValues;
#elif NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private char[]? _quoteSearchCharacters;
#else
        [MaybeNull] private char[] _quoteSearchCharacters;
#endif
        private char _quoteSearchDelimiter;
        private string _quoteSearchTerminator = string.Empty;
        private char[] _quoteRequiredCharacters = new char[0];
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private char[] _fieldValueCharacters = NoCharacters;
#endif
        private string[] _fieldNames = NoFieldNames;
        private string[] _unmappedFieldNames = NoFieldNames;
        private string[] _fieldValues = NoFieldNames;
        private string[] _headerLineFieldNames = NoFieldNames;
        private readonly bool _isInternalWriter;
        private readonly CsvFileOptions _options = new CsvFileOptions();
        private int _previousLineFieldCount;
        private readonly TextWriter _writer;

        public CsvFileWriter(Stream csvStream)
        {
            if (csvStream is null)
                throw new ArgumentNullException(nameof(csvStream));

            _writer = CreateStreamWriter(csvStream, DefaultEncoding);
            _isInternalWriter = true;
        }

        public CsvFileWriter(Stream csvStream, CsvFileOptions options)
        {
            if (csvStream is null)
                throw new ArgumentNullException(nameof(csvStream));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            ValidateHandlers(options);

            _options = options;

            // Create the writer once, with the requested encoding if any, so no buffer is allocated and discarded.
            _writer = CreateStreamWriter(csvStream, _options.Encoding ?? DefaultEncoding);
            _isInternalWriter = true;
        }

#if NETSTANDARD2_0_OR_GREATER||NETCOREAPP2_0_OR_GREATER
        public CsvFileWriter(string csvFilePath)
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));

            _writer = CreateFileWriter(csvFilePath, DefaultEncoding);
            _isInternalWriter = true;
        }

        public CsvFileWriter(string csvFilePath, CsvFileOptions options)
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            ValidateHandlers(options);

            _options = options;

            // Open the file once, with the requested encoding if any, so no handle is left orphaned.
            _writer = CreateFileWriter(csvFilePath, _options.Encoding ?? DefaultEncoding);
            _isInternalWriter = true;
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
            _options = options ?? throw new ArgumentNullException(nameof(options));

            ValidateHandlers(_options);

            if (_options.Encoding != null)
            {
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(writer));
            }
        }

        private static StreamWriter CreateStreamWriter(Stream stream, Encoding encoding)
        {
            return new StreamWriter(stream, encoding, StreamBufferSize);
        }

#if NETSTANDARD2_0_OR_GREATER||NETCOREAPP2_0_OR_GREATER
        private static StreamWriter CreateFileWriter(string path, Encoding encoding)
        {
            // Open the file stream explicitly so both buffering layers are sized together.
            var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, StreamBufferSize);
            try
            {
                return CreateStreamWriter(fileStream, encoding);
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

            if (_isInternalWriter)
            {
                _writer.Dispose();
            }
        }

        /// <summary>
        /// Write a line to a CSV file.
        /// </summary>
        /// <param name="csvLine">
        /// Line containing fields to be written: a <see cref="CsvFileHeaderLine"/>, or a record line of type <typeparamref name="TRecordLine"/>.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="csvLine"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="csvLine"/> is neither a <see cref="CsvFileHeaderLine"/> nor a <typeparamref name="TRecordLine"/>.  Nothing is written.
        /// </exception>
        /// <exception cref="CsvFileException">
        /// Invalid field name detected, or a record line has a different number of fields from the previous record line, or a field with a value
        /// that isn't one of the fields written, and <see cref="ICsvFileOptions.InconsistentFieldCountHandler"/> isn't set.  Nothing is written.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <remarks>
        /// A record line that has a different number of fields from the previous record line breaks RFC 4180§2¶4.  When
        /// <see cref="ICsvFileOptions.InconsistentFieldCountHandler"/> is set, it is called for the line, and the line is skipped: nothing is
        /// written for it, and the lines after it are checked against the same field count as before it.
        /// <para>
        /// With a header line, the fields written are those of the header line.  Without one, they are the mapped fields, followed by the fields
        /// of the first record line written that no mapping covers, in the order they're on that line.  A record line with a field that isn't one
        /// of the fields written, and whose value isn't <c>null</c>, is inconsistent, even if it has as many fields, so it is handled or throws in
        /// the same way, rather than being written without that field.  A record line that lacks one of the fields written is written with a
        /// blank value for it, unless the field's mapping supplies a constant or generated value, which is written instead.
        /// </para>
        /// </remarks>
        public void WriteLine(ICsvFileLine csvLine)
        {
            ThrowIfDisposed();

            if (csvLine is null)
                throw new ArgumentNullException(nameof(csvLine));

            // A line of any other type has no fields the writer can output, and writing it as an empty record would break the rule that a record has one or more fields.
            // ref. RFC 4180§2
            if (!(csvLine is CsvFileHeaderLine) && !(csvLine is TRecordLine))
            {
                throw new ArgumentException($"Expected a {nameof(CsvFileHeaderLine)} or a {typeof(TRecordLine).Name}; found a {csvLine.GetType().Name}.", nameof(csvLine));
            }

            // Extract the serialized field values that will comprise the CSV line.  All of them are extracted before any is
            // written, so a line that fails part way through writes nothing.
            int fieldValueCount;
            if (csvLine is CsvFileHeaderLine headerLine)
            {
                // The header line's fields are written in the line's own order, so its last non-empty field is found on the line.
                var lastFieldIndex = _options.SuppressTrailingBlankFields ? GetLastNonEmptyFieldIndex(csvLine) : -1;
                fieldValueCount = GetHeaderLineFieldValues(headerLine, lastFieldIndex);
            }
            else if (!TryGetRecordLineFieldValues((TRecordLine)csvLine, out fieldValueCount))
            {
                // The inconsistent field count handler handled the line, so it is skipped, as the reader skips it.
                return;
            }

            // Write line to CSV output.
            WriteFieldValues(fieldValueCount);
        }

        private int GetHeaderLineFieldValues(CsvFileHeaderLine headerLine, int lastFieldIndex)
        {
            ICsvFileLine csvLine = headerLine;
            _headerLine = headerLine;

            // Extract field names from the header line.
            var fieldValues = GetFieldValueBuffer(csvLine.Count);
            var fieldNames = new string[csvLine.Count];
            var fieldValueCount = 0;
            for (var fieldIndex = 0; fieldIndex < csvLine.Count; fieldIndex++)
            {
                // If the remaining field values are empty, stop here.
                if (lastFieldIndex > -1 && fieldIndex > lastFieldIndex)
                {
                    break;
                }

                // The field value is the field name, so must be a string.
                var fieldValue = csvLine[fieldIndex];
                if (!(fieldValue is string rawFieldValue) || string.IsNullOrWhiteSpace(rawFieldValue))
                {
                    var message = $"Field value '{fieldValue}' is not a field name.";
                    var dataItems = new Dictionary<string, object>();
                    if (fieldValue != null)
                    {
                        dataItems.Add(DataItemName.FieldValue, fieldValue);
                    }

                    throw BuildException(message, dataItems);
                }

                // The name is kept as it is, to order the fields on the record lines, and written with the embedded line break, as the reader, which
                // applies it to every field, will give it back.
                fieldNames[fieldValueCount] = rawFieldValue;
                fieldValues[fieldValueCount++] = _options.EmbeddedLineBreak != null && rawFieldValue.IndexOfAny(LineBreakCharacters) >= 0
                    ? NormalizeFieldValueLineBreaks(rawFieldValue, _options)
                    : rawFieldValue;
            }

            // Store a copy of the header line's field names so fields on the record lines can be correctly ordered.
            var headerLineFieldNames = new string[fieldValueCount];
            Array.Copy(fieldNames, headerLineFieldNames, fieldValueCount);
            _headerLineFieldNames = headerLineFieldNames;

            return fieldValueCount;
        }

        /// <summary>
        /// Gets the serialized field values of a record line.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the line is to be written; <c>false</c> if the inconsistent field count handler handled it.
        /// </returns>
        /// <remarks>
        /// A line that is not written, because it is handled or because getting its values fails, leaves the field names and field plan as they
        /// were, so it doesn't add its fields to the lines written after it.
        /// </remarks>
        private bool TryGetRecordLineFieldValues(TRecordLine recordLine, out int fieldValueCount)
        {
            var previousFieldNames = _fieldNames;
            var previousUnmappedFieldNames = _unmappedFieldNames;
            var previousFieldPlan = _fieldPlan;
            var isWritten = false;
            try
            {
                isWritten = TryGetFieldValuesOfRecordLine(recordLine, out fieldValueCount);
                return isWritten;
            }
            finally
            {
                if (!isWritten)
                {
                    _fieldNames = previousFieldNames;
                    _unmappedFieldNames = previousUnmappedFieldNames;
                    _fieldPlan = previousFieldPlan;
                }
            }
        }

        private bool TryGetFieldValuesOfRecordLine(TRecordLine recordLine, out int fieldValueCount)
        {
            ICsvFileLine csvLine = recordLine;

            fieldValueCount = 0;

            // Without a header line, the fields of the first record line written are kept, so a line without a mapping can be output, and so can the
            // fields that no mapping covers.  The lines after it are checked against them.
            if (_headerLineFieldNames.Length == 0 && _previousLineFieldCount == 0)
            {
                AddLineFieldNames(csvLine);

                // The fields output after the mapped fields are those that the first line's own mapper doesn't cover, so a line of another type,
                // whose mapper covers other fields, isn't given the fields that the first line's mapper output.
                _unmappedFieldNames = _fieldNames;
                _unmappedFieldNames = GetFieldPlan(recordLine).UnmappedLineFieldNames;
            }

            var fieldPlan = GetFieldPlan(recordLine);
            var fields = fieldPlan.Fields;

            // Check for an inconsistent field count.
            // ref. RFC 4180§2¶4
            if (_previousLineFieldCount > 0 && fields.Length != _previousLineFieldCount)
            {
                return HandleInconsistentFieldCount(recordLine, _previousLineFieldCount, fields.Length);
            }

            // A field of the line that isn't one of the fields output would lose its value, so the line is inconsistent with the lines before it,
            // though it may have as many fields as they do.
            // ref. RFC 4180§2¶4
            if (fieldPlan.TryGetFieldNotOutput(recordLine, out var fieldNameNotOutput))
            {
                if (csvLine.Count != fields.Length)
                {
                    return HandleInconsistentFieldCount(recordLine, fields.Length, csvLine.Count);
                }

                var lineDescription = _headerLineFieldNames.Length > 0 ? "the header line" : "the first record line";
                return HandleInconsistentLine(recordLine, $"Field '{fieldNameNotOutput}' is not one of the {fields.Length} fields of {lineDescription}.",
                    new Dictionary<string, object> { { DataItemName.ExpectedFieldCount, fields.Length }, { DataItemName.FieldName, fieldNameNotOutput } });
            }

            // Extract serialized field values.
            var fieldValues = GetFieldValueBuffer(fields.Length);
            foreach (var field in fields)
            {
                // A constant or generated value is supplied by the mapping, so is written whether or not the line holds the field, as it is read.
                // A line read or converted with the mapping holds any other mapped value under the field name.  A line built from the model holds
                // it under the key that the property resolves to on the line, which differs from the field name when the field is renamed.
                var fieldKey = field.FieldName;
                if (!field.IsSupplied && !csvLine.Contains(fieldKey))
                {
                    var propertyKey = field.IsMapped ? recordLine.ResolveMappedPropertyKey(field.Mapping.Key) : null;
                    if (propertyKey is null || !csvLine.Contains(propertyKey))
                    {
                        // If a value has not been set for the current field, add a blank value.
                        fieldValues[fieldValueCount++] = string.Empty;
                        continue;
                    }

                    fieldKey = propertyKey;
                }

                // Convert the field value to a string.
                var rawFieldValue = SerializeFieldValue(recordLine, field, fieldKey);

                // Apply embedded line break on field value.  A value without a line break is unchanged by this, so is skipped.
                if (_options.EmbeddedLineBreak != null && !string.IsNullOrEmpty(rawFieldValue) && rawFieldValue.IndexOfAny(LineBreakCharacters) >= 0)
                {
                    rawFieldValue = NormalizeFieldValueLineBreaks(rawFieldValue, _options);
                }

                fieldValues[fieldValueCount++] = rawFieldValue ?? string.Empty;
            }

            // The fields are in output order, which may differ from the order of the line's own fields, so the trailing blank values are
            // trimmed from the values to write rather than looked for on the line.  A line whose values are all blank is written whole.
            // N.B. Whitespace alone does not count as being "empty" as spaces are permitted.
            // ref. RFC 4180§2¶4
            if (_options.SuppressTrailingBlankFields)
            {
                fieldValueCount = GetTrimmedFieldValueCount(fieldValues, fieldValueCount);
            }

            // The line's field count becomes the one later lines are checked against only once every value has been extracted, so a line that
            // fails part way through doesn't set it.
            _previousLineFieldCount = fields.Length;
            return true;
        }

        private bool HandleInconsistentFieldCount(TRecordLine recordLine, int expectedFieldCount, int fieldCount)
        {
            var message = $"Expected {expectedFieldCount} fields; found {fieldCount} fields.";
            var dataItems = new Dictionary<string, object> { { DataItemName.ExpectedFieldCount, expectedFieldCount }, { DataItemName.ActualFieldCount, fieldCount } };
            return HandleInconsistentLine(recordLine, message, dataItems);
        }

        /// <summary>
        /// Calls the inconsistent field count handler for a record line, or throws if it isn't set.
        /// </summary>
        /// <returns><c>false</c>, as the line is not to be written.</returns>
        /// <exception cref="CsvFileException">The inconsistent field count handler isn't set.</exception>
        private bool HandleInconsistentLine(TRecordLine recordLine, string message, Dictionary<string, object> dataItems)
        {
            if (_options.InconsistentFieldCountHandler is CsvFieldCount<TRecordLine> fieldCountHandler)
            {
                var context = new CsvFieldContext<TRecordLine>(_options) { HeaderLine = _headerLine, Model = recordLine };
                fieldCountHandler(context);
                return false;
            }

            throw BuildException(message, dataItems);
        }

        private void AddLineFieldNames(ICsvFileLine csvLine)
        {
            var fieldNames = new List<string>(_fieldNames);
            var knownFieldNames = new HashSet<string>(_fieldNames, StringComparer.Ordinal);
            var lineFieldNames = csvLine.Keys;
            for (var fieldIndex = 0; fieldIndex < lineFieldNames.Count; fieldIndex++)
            {
                var fieldName = lineFieldNames[fieldIndex];
                if (knownFieldNames.Add(fieldName))
                {
                    fieldNames.Add(fieldName);
                }
            }

            _fieldNames = fieldNames.ToArray();
        }

        /// <summary>
        /// Gets the field plan for the current header line, mapper and record line field names, building a new one if any of them has changed since the last line.
        /// </summary>
        private FieldPlan GetFieldPlan(TRecordLine recordLine)
        {
            var mapperVersion = MapperVersion;
            var fieldPlan = _fieldPlan;
            if (fieldPlan != null
                && fieldPlan.MapperVersion == mapperVersion
                && ReferenceEquals(fieldPlan.HeaderLineFieldNames, _headerLineFieldNames)
                && ReferenceEquals(fieldPlan.LineFieldNames, _fieldNames)
                && ReferenceEquals(fieldPlan.UnmappedCandidateFieldNames, _unmappedFieldNames)
                && (fieldPlan.MapperLineType is null || fieldPlan.MapperLineType == recordLine.GetType()))
            {
                return fieldPlan;
            }

            // A mapper that is not registered was created from the record line's type, so the plan only holds for lines of that type.
            var mapper = GetMapper(recordLine, out var isMapperRegistered);
            fieldPlan = new FieldPlan(mapperVersion, isMapperRegistered ? null : recordLine.GetType(), mapper, _headerLineFieldNames, _fieldNames, _unmappedFieldNames,
                recordLine);
            _fieldPlan = fieldPlan;
            return fieldPlan;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? SerializeFieldValue(TRecordLine recordLine, FieldWritePlan field, string fieldKey)
#else
        [return: MaybeNull]
        private string SerializeFieldValue([DisallowNull] TRecordLine recordLine, [DisallowNull] FieldWritePlan field, [DisallowNull] string fieldKey)
#endif
        {
            ICsvFileLine csvLine = recordLine;
            if (!field.IsMapped)
            {
                var fieldValue = csvLine[fieldKey];
                return fieldValue is null ? null : fieldValue is string stringFieldValue ? stringFieldValue : FormatFieldValue(fieldValue, _options);
            }

            // Get the mapped field value.  A constant or generated value comes from the mapping; any other value is read from the line by the
            // given key, which the line holds.
            var fieldMapping = field.Mapping.Value;
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            object? mappedFieldValue;
#else
            object mappedFieldValue;
#endif
            if (fieldMapping.ConstantValue != null)
            {
                mappedFieldValue = fieldMapping.ConstantValue;
            }
            else if (fieldMapping.VariableValueGenerator != null)
            {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
                mappedFieldValue = field.Accessor!.Generate(fieldMapping.VariableValueGenerator);
#else
                mappedFieldValue = field.Accessor.Generate(fieldMapping.VariableValueGenerator);
#endif
            }
            else
            {
                mappedFieldValue = csvLine[fieldKey];
            }

            if (mappedFieldValue is null)
            {
                return null; // if there is no field value then there is nothing to serialize
            }

            // A string is written as it is, unless it is the line's value and a converter that converts to a string is configured: such a converter
            // is given the value even when it is a string.  A constant or generated string is always written as it is.
            var typeConverter = ResolveTypeConverter(field);
            if (mappedFieldValue is string serializedFieldValue
                && (fieldMapping.SuppliesValue
                    || (field.Converter is null && (typeConverter is null || !typeConverter.CanConvertTo(typeof(string))))))
            {
                return serializedFieldValue;
            }

            return ConvertFieldValueToString(_headerLine, recordLine, _options, field.Mapping, field.Converter, typeConverter, mappedFieldValue);
        }

        /// <summary>
        /// Gets the type converter for a mapped field that has no converter delegate, resolving it on first use.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private TypeConverter? ResolveTypeConverter(FieldWritePlan field)
#else
        [return: MaybeNull]
        private TypeConverter ResolveTypeConverter([DisallowNull] FieldWritePlan field)
#endif
        {
            var typeConverterType = field.Mapping.Value.TypeConverterType;
            if (field.Converter != null || typeConverterType is null)
            {
                return null;
            }

            if (!field.IsTypeConverterResolved)
            {
                field.TypeConverter = CsvMapperCache.GetTypeConverter(typeConverterType);
                field.IsTypeConverterResolved = true;
            }

            return field.TypeConverter;
        }

        /// <summary>
        /// Converts a mapped field value: with the mapping's converter if it has one, else its type converter if that can
        /// convert to a string, else with <see cref="FormatFieldValue"/>.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? ConvertFieldValueToString(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, KeyValuePair<string, CsvFieldMapping> propertyFieldMapping,
            CsvFieldConvertToString<TRecordLine>? converter, TypeConverter? typeConverter, object fieldValue)
#else
        [return: MaybeNull]
        private static string ConvertFieldValueToString([AllowNull] CsvFileHeaderLine headerLine, [DisallowNull] TRecordLine recordLine, [DisallowNull] CsvFileOptions options,
            KeyValuePair<string, CsvFieldMapping> propertyFieldMapping, [AllowNull] CsvFieldConvertToString<TRecordLine> converter, [AllowNull] TypeConverter typeConverter, [DisallowNull] object fieldValue)
#endif
        {
            if (converter != null)
            {
                var context = new CsvFieldContext<TRecordLine>(options)
                {
                    HeaderLine = headerLine,
                    Model = recordLine,
                    FieldName = CsvMappingService.GetFieldNameFromMapping(propertyFieldMapping),
                    FieldIndex = propertyFieldMapping.Value.FieldIndex
                };
                return converter(context);
            }

            if (typeConverter != null && typeConverter.CanConvertTo(typeof(string)))
            {
                return options.Culture != null
                    ? typeConverter.ConvertToString(null, options.Culture, fieldValue)
                    : typeConverter.ConvertToInvariantString(fieldValue);
            }

            return FormatFieldValue(fieldValue, options);
        }

        /// <summary>
        /// Converts a field value to a string.  An <see cref="IFormattable"/> value is formatted with the <see cref="ICsvFileOptions.Culture"/>, or the
        /// invariant culture if none is set, so the output never depends on the current culture.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? FormatFieldValue(object fieldValue, ICsvFileOptions options)
#else
        [return: MaybeNull]
        private static string FormatFieldValue([DisallowNull] object fieldValue, [DisallowNull] ICsvFileOptions options)
#endif
        {
            return fieldValue is IFormattable formattableFieldValue
                ? formattableFieldValue.ToString(null, options.Culture ?? CultureInfo.InvariantCulture)
                : fieldValue.ToString();
        }

        private bool DoesFieldValueRequireQuoting(string rawFieldValue, CsvFileOptions options)
        {
            var quotingRequired = false;
            switch (options.QuoteHandling)
            {
                case CsvFieldQuoteHandling.Always:
                    quotingRequired = true;
                    break;
                case CsvFieldQuoteHandling.Auto:
                    // The culture that formats the values, so that the output doesn't depend on the machine's culture.
                    var culture = options.Culture ?? CultureInfo.InvariantCulture;
                    // A value that RFC 4180 requires to be quoted is quoted even when it reads as a number, such as 1,000 with a comma delimiter.
                    // ref. RFC 4180§2¶6
                    quotingRequired = ContainsCharacterRequiringQuotes(rawFieldValue, options)
                                      || (!string.IsNullOrWhiteSpace(rawFieldValue) && !decimal.TryParse(rawFieldValue, NumberStyles.Number, culture, out _));
                    break;
                case CsvFieldQuoteHandling.Required:
                    // ref. RFC 4180§2¶6
                    quotingRequired = ContainsCharacterRequiringQuotes(rawFieldValue, options);
                    break;
            }

            return quotingRequired;
        }

        /// <summary>
        /// Checks a field value for a line break, quote or field delimiter in one scan.
        /// </summary>
        /// <remarks>
        /// A line break is a carriage return or line feed, or the <see cref="ICsvFileOptions.EmbeddedLineBreak"/> sequence when one is set.
        /// </remarks>
        private bool ContainsCharacterRequiringQuotes(string rawFieldValue, ICsvFileOptions options)
        {
            var embeddedLineBreak = options.EmbeddedLineBreak;
            if (embeddedLineBreak != null && embeddedLineBreak.Length == 0)
            {
                return true; // every value contains the empty string
            }

            // The search characters are built once, and again only if the options they come from change.
            var fieldDelimiter = options.FieldDelimiterCharacter;
            var terminator = options.TerminatorLineBreak ?? string.Empty;
#if NET8_0_OR_GREATER
            if (_quoteSearchValues is null || _quoteSearchDelimiter != fieldDelimiter || !string.Equals(_quoteSearchEmbeddedLineBreak, embeddedLineBreak)
                || !string.Equals(_quoteSearchTerminator, terminator))
            {
                _quoteRequiredCharacters = GetRequiredQuoteCharacters(fieldDelimiter, terminator);
                _quoteSearchValues = SearchValues.Create(GetQuoteSearchCharacters(_quoteRequiredCharacters, embeddedLineBreak));
                _quoteSearchDelimiter = fieldDelimiter;
                _quoteSearchEmbeddedLineBreak = embeddedLineBreak;
                _quoteSearchTerminator = terminator;
            }

            var fieldValue = rawFieldValue.AsSpan();
            if (!fieldValue.ContainsAny(_quoteSearchValues))
            {
                return false;
            }
#else
            if (_quoteSearchCharacters is null || _quoteSearchDelimiter != fieldDelimiter || !string.Equals(_quoteSearchEmbeddedLineBreak, embeddedLineBreak)
                || !string.Equals(_quoteSearchTerminator, terminator))
            {
                _quoteRequiredCharacters = GetRequiredQuoteCharacters(fieldDelimiter, terminator);
                _quoteSearchCharacters = GetQuoteSearchCharacters(_quoteRequiredCharacters, embeddedLineBreak);
                _quoteSearchDelimiter = fieldDelimiter;
                _quoteSearchEmbeddedLineBreak = embeddedLineBreak;
                _quoteSearchTerminator = terminator;
            }

            var fieldValue = rawFieldValue.AsSpan();
            if (fieldValue.IndexOfAny(_quoteSearchCharacters) < 0)
            {
                return false;
            }
#endif

            // Every character that requires quoting is in the search, so a hit with no embedded line break sequence settles it.  With one, the hit may be
            // only part of the sequence, so it is confirmed.
            // ref. RFC 4180§2¶6
            if (embeddedLineBreak is null)
            {
                return true;
            }

            return fieldValue.IndexOfAny(_quoteRequiredCharacters) >= 0 || fieldValue.IndexOf(embeddedLineBreak.AsSpan()) >= 0;
        }

        /// <summary>
        /// Gets the characters that require a value to be quoted, whatever the options: a line break, a quote and the delimiter (RFC 4180§2¶6), and
        /// each character of the terminator, since a value that ends with the start of a multi-character terminator would run into the terminator.
        /// </summary>
        private static char[] GetRequiredQuoteCharacters(char fieldDelimiter, string terminator)
        {
            var characters = new List<char> { '\r', '\n', '"', fieldDelimiter };
            foreach (var character in terminator)
            {
                if (!characters.Contains(character)) characters.Add(character);
            }

            return characters.ToArray();
        }

        /// <summary>
        /// Gets the characters to search a value for: the required characters, and the characters of the embedded line break sequence, whose
        /// presence as a whole is confirmed afterwards.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static char[] GetQuoteSearchCharacters(char[] requiredCharacters, string? embeddedLineBreak)
#else
        private static char[] GetQuoteSearchCharacters(char[] requiredCharacters, [AllowNull] string embeddedLineBreak)
#endif
        {
            var characters = new List<char>(requiredCharacters);
            foreach (var character in embeddedLineBreak ?? string.Empty)
            {
                if (!characters.Contains(character)) characters.Add(character);
            }

            return characters.ToArray();
        }

        private string[] GetFieldValueBuffer(int fieldCount)
        {
            if (_fieldValues.Length < fieldCount)
            {
                _fieldValues = new string[fieldCount];
            }

            return _fieldValues;
        }

        private void WriteFieldValues(int fieldValueCount)
        {
            var fieldValues = _fieldValues;
            for (var fieldIndex = 0; fieldIndex < fieldValueCount; fieldIndex++)
            {
                if (fieldIndex > 0)
                {
                    _writer.Write(_options.FieldDelimiterCharacter);
                }

                WriteFieldValue(fieldValues[fieldIndex]);
            }

            _writer.Write(_options.TerminatorLineBreak);

            // Release the values so the buffer does not keep them alive.
            Array.Clear(fieldValues, 0, fieldValueCount);
        }

        private void WriteFieldValue(string fieldValue)
        {
            if (!DoesFieldValueRequireQuoting(fieldValue, _options))
            {
                _writer.Write(fieldValue);
                return;
            }

            // Enclose the value in quotes, doubling any quote within it.
            // ref. RFC 4180§2¶7
            _writer.Write(QuoteCharacter);
            var quoteIndex = fieldValue.AsSpan().IndexOf(QuoteCharacter);
            if (quoteIndex < 0)
            {
                _writer.Write(fieldValue);
            }
            else
            {
                WriteEscapedFieldValue(fieldValue, quoteIndex);
            }

            _writer.Write(QuoteCharacter);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER
        private void WriteEscapedFieldValue(string fieldValue, int quoteIndex)
        {
            var remainingFieldValue = fieldValue.AsSpan();
            while (quoteIndex >= 0)
            {
                // Write up to and including the quote, then a second quote to escape it.
                _writer.Write(remainingFieldValue.Slice(0, quoteIndex + 1));
                _writer.Write(QuoteCharacter);
                remainingFieldValue = remainingFieldValue.Slice(quoteIndex + 1);
                quoteIndex = remainingFieldValue.IndexOf(QuoteCharacter);
            }

            _writer.Write(remainingFieldValue);
        }
#else
        private void WriteEscapedFieldValue(string fieldValue, int quoteIndex)
        {
            // There is no span overload of TextWriter.Write on this target, so the segments are written from a copy of the value in a reusable buffer.
            if (_fieldValueCharacters.Length < fieldValue.Length)
            {
                _fieldValueCharacters = new char[Math.Max(fieldValue.Length, _fieldValueCharacters.Length * 2)];
            }

            var fieldValueCharacters = _fieldValueCharacters;
            fieldValue.CopyTo(0, fieldValueCharacters, 0, fieldValue.Length);

            var segmentStart = 0;
            while (quoteIndex >= 0)
            {
                // Write up to and including the quote, then a second quote to escape it.
                _writer.Write(fieldValueCharacters, segmentStart, quoteIndex + 1);
                _writer.Write(QuoteCharacter);
                segmentStart += quoteIndex + 1;
                quoteIndex = fieldValue.AsSpan(segmentStart).IndexOf(QuoteCharacter);
            }

            _writer.Write(fieldValueCharacters, segmentStart, fieldValue.Length - segmentStart);
        }
#endif

        private static int GetTrimmedFieldValueCount(string[] fieldValues, int fieldValueCount)
        {
            for (var fieldIndex = fieldValueCount - 1; fieldIndex >= 0; fieldIndex--)
            {
                if (fieldValues[fieldIndex].Length > 0)
                {
                    return fieldIndex + 1;
                }
            }

            return fieldValueCount;
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

        /// <summary>
        /// The fields to output and how to serialize each, worked out once for a header line, mapper and set of record line field names rather than on every line.
        /// </summary>
        /// <remarks>
        /// A declarative mapper is shared by every reader and writer, so the plan only reads from the mapper and its field mappings and never writes to them.
        /// </remarks>
        private sealed class FieldPlan
        {
            // The keys a record line holds the values of the fields output under, apart from the keys of the properties mapped to them.
            private readonly HashSet<string> _fieldKeys;

            // The keys of the properties mapped to the fields output, as they resolve on a line of the type the plan was built for.
            private readonly HashSet<string> _propertyKeys;

            // The keys of the mappings that cover a field of a record line, for resolving their property keys on a line of another type.
            private readonly string[] _coveringMappingKeys;
            private readonly Type _lineType;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal FieldPlan(int mapperVersion, Type? mapperLineType, ICsvFileMapper mapper, string[] headerLineFieldNames, string[] lineFieldNames,
                string[] unmappedCandidateFieldNames, TRecordLine recordLine)
#else
            internal FieldPlan(int mapperVersion, [AllowNull] Type mapperLineType, [DisallowNull] ICsvFileMapper mapper, [DisallowNull] string[] headerLineFieldNames,
                [DisallowNull] string[] lineFieldNames, [DisallowNull] string[] unmappedCandidateFieldNames, [DisallowNull] TRecordLine recordLine)
#endif
            {
                MapperVersion = mapperVersion;
                MapperLineType = mapperLineType;
                HeaderLineFieldNames = headerLineFieldNames;
                LineFieldNames = lineFieldNames;
                UnmappedCandidateFieldNames = unmappedCandidateFieldNames;
                UnmappedLineFieldNames = unmappedCandidateFieldNames;
                _lineType = recordLine.GetType();

                // Fields are output in header line order if there is one.  Without one, the mapped fields are output in mapper order, followed by the
                // fields of the first record line that no mapping covers, in the order they're on that line, so their values aren't lost.
                var isMapperOutput = headerLineFieldNames.Length == 0 && mapper.Count > 0;
                var fieldNames = headerLineFieldNames.Length > 0 ? headerLineFieldNames
                    : isMapperOutput ? CsvMappingService.GetSortedFieldNamesFromMapper(mapper)
                    : lineFieldNames;

                // A mapping covers a field of a record line if its field is output.  A mapping that supplies a constant or generated value also
                // covers the field the reader puts that value in, though it isn't output, since writing the line without it loses nothing.
                // Where more than one mapping has the same field name, the first is used to output the field.
                _fieldKeys = new HashSet<string>(fieldNames, StringComparer.Ordinal);
                var mappingsByFieldName = new Dictionary<string, KeyValuePair<string, CsvFieldMapping>>(mapper.Count, StringComparer.Ordinal);
                var suppliedFieldNames = new List<string>();
                var coveringMappingKeys = new List<string>();
                foreach (var mapping in mapper)
                {
                    var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(mapping);
                    if (!mappingsByFieldName.ContainsKey(mappedFieldName))
                    {
                        mappingsByFieldName.Add(mappedFieldName, mapping);
                    }

                    if (_fieldKeys.Contains(mappedFieldName))
                    {
                        coveringMappingKeys.Add(mapping.Key);
                    }
                    else if (mapping.Value.SuppliesValue)
                    {
                        suppliedFieldNames.Add(mappedFieldName);
                        coveringMappingKeys.Add(mapping.Key);
                    }
                }

                _fieldKeys.UnionWith(suppliedFieldNames);
                _coveringMappingKeys = coveringMappingKeys.ToArray();
                _propertyKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var mappingKey in _coveringMappingKeys)
                {
                    _propertyKeys.Add(recordLine.ResolveMappedPropertyKey(mappingKey));
                }

                if (isMapperOutput)
                {
                    var unmappedLineFieldNames = new List<string>();
                    foreach (var lineFieldName in unmappedCandidateFieldNames)
                    {
                        if (!_propertyKeys.Contains(lineFieldName) && _fieldKeys.Add(lineFieldName))
                        {
                            unmappedLineFieldNames.Add(lineFieldName);
                        }
                    }

                    UnmappedLineFieldNames = unmappedLineFieldNames.ToArray();
                    var outputFieldNames = new string[fieldNames.Length + UnmappedLineFieldNames.Length];
                    fieldNames.CopyTo(outputFieldNames, 0);
                    UnmappedLineFieldNames.CopyTo(outputFieldNames, fieldNames.Length);
                    fieldNames = outputFieldNames;
                }

                var fields = new FieldWritePlan[fieldNames.Length];
                for (var fieldIndex = 0; fieldIndex < fieldNames.Length; fieldIndex++)
                {
                    mappingsByFieldName.TryGetValue(fieldNames[fieldIndex], out var mapping);
                    fields[fieldIndex] = new FieldWritePlan(fieldNames[fieldIndex], mapping);
                }

                Fields = fields;
            }

            /// <summary>
            /// Gets the fields to output, in order.
            /// </summary>
            internal FieldWritePlan[] Fields { get; }

            internal string[] HeaderLineFieldNames { get; }

            /// <summary>
            /// Gets the record line field names that may be output after the mapped fields when there is no header line.
            /// </summary>
            internal string[] UnmappedCandidateFieldNames { get; }

            /// <summary>
            /// Gets the field names of <see cref="UnmappedCandidateFieldNames"/> that no mapping covers, which are output after the mapped fields
            /// when there is no header line, or <see cref="UnmappedCandidateFieldNames"/> itself when the mapper has no mappings.
            /// </summary>
            internal string[] UnmappedLineFieldNames { get; }

            /// <summary>
            /// Gets the first field of a record line that holds a value and isn't output, if there is one.
            /// </summary>
            /// <remarks>
            /// A field is output if its name is the name of a field output, or if the line was built from a model and the field is the property
            /// that a mapping of a field output is mapped from.  A field whose value is <c>null</c> or empty is written as a blank value whether it
            /// is output or not, and a field whose value a mapping supplies can be supplied again, so neither loses anything.
            /// </remarks>
            internal bool TryGetFieldNotOutput(TRecordLine recordLine, out string fieldName)
            {
                ICsvFileLine csvLine = recordLine;
                var fields = Fields;
                var lineFieldNames = csvLine.Keys;
                for (var fieldIndex = 0; fieldIndex < lineFieldNames.Count; fieldIndex++)
                {
                    // A line usually has the fields output, in the same order, so the name at the same position is compared first.
                    var lineFieldName = lineFieldNames[fieldIndex];
                    if ((fieldIndex < fields.Length && string.Equals(lineFieldName, fields[fieldIndex].FieldName, StringComparison.Ordinal))
                        || _fieldKeys.Contains(lineFieldName))
                    {
                        continue;
                    }

                    var fieldValue = csvLine[fieldIndex];
                    if (fieldValue is null || (fieldValue is string stringFieldValue && stringFieldValue.Length == 0))
                    {
                        continue;
                    }

                    // A property key depends on the line's type, and a registered mapper's plan is used for lines of every type.
                    if (recordLine.GetType() == _lineType ? _propertyKeys.Contains(lineFieldName) : IsCoveredByMappedProperty(recordLine, lineFieldName))
                    {
                        continue;
                    }

                    fieldName = lineFieldName;
                    return true;
                }

                fieldName = string.Empty;
                return false;
            }

            /// <summary>
            /// Gets whether a field of a record line is the property that a covering mapping is mapped from, for a line of another type than the one
            /// the plan was built for.
            /// </summary>
            private bool IsCoveredByMappedProperty(TRecordLine recordLine, string lineFieldName)
            {
                foreach (var mappingKey in _coveringMappingKeys)
                {
                    if (string.Equals(recordLine.ResolveMappedPropertyKey(mappingKey), lineFieldName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }

            internal string[] LineFieldNames { get; }

            internal int MapperVersion { get; }

            /// <summary>
            /// Gets the record line type the mapper was created from when it is not registered, or <c>null</c> if it is registered and applies to a line of any type.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal Type? MapperLineType { get; }
#else
            [MaybeNull] internal Type MapperLineType { get; }
#endif
        }

        /// <summary>
        /// A field to output, with the mapping for it if there is one.
        /// </summary>
        private sealed class FieldWritePlan
        {
            internal FieldWritePlan(string fieldName, KeyValuePair<string, CsvFieldMapping> mapping)
            {
                FieldName = fieldName;
                Mapping = mapping;
                IsMapped = !string.IsNullOrEmpty(mapping.Key);
                if (IsMapped)
                {
                    Converter = mapping.Value.ToStringConverter as CsvFieldConvertToString<TRecordLine>;
                    IsSupplied = mapping.Value.SuppliesValue;
                    if (mapping.Value.VariableValueGenerator != null)
                    {
                        Accessor = FieldValueAccessor.For(mapping.Value.PropertyType);
                    }
                }
            }

            /// <summary>
            /// Gets the accessor that generates values of the property type, if the mapping has a value generator.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal FieldValueAccessor? Accessor { get; }
#else
            [MaybeNull] internal FieldValueAccessor Accessor { get; }
#endif

            /// <summary>
            /// Gets the mapping's converter delegate, if it has one.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal CsvFieldConvertToString<TRecordLine>? Converter { get; }
#else
            [MaybeNull] internal CsvFieldConvertToString<TRecordLine> Converter { get; }
#endif

            /// <summary>
            /// Gets the name of the field, which is also the key of its value on a record line, unless the line was built from a model whose
            /// property is mapped to a field with another name.
            /// </summary>
            internal string FieldName { get; }

            /// <summary>
            /// Gets a flag indicating if the field has a mapping.
            /// </summary>
            internal bool IsMapped { get; }

            /// <summary>
            /// Gets a flag indicating if the field's mapping supplies its value, as a constant or generated value, rather than taking it from the line.
            /// </summary>
            internal bool IsSupplied { get; }

            internal bool IsTypeConverterResolved { get; set; }

            internal KeyValuePair<string, CsvFieldMapping> Mapping { get; }

            /// <summary>
            /// Gets or sets the mapping's type converter, once resolved.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal TypeConverter? TypeConverter { get; set; }
#else
            [MaybeNull, AllowNull] internal TypeConverter TypeConverter { get; set; }
#endif
        }
    }
}