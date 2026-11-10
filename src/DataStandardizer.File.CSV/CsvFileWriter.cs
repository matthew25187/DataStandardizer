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
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private char[] _fieldValueCharacters = NoCharacters;
#endif
        private string[] _fieldNames = NoFieldNames;
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
        /// Invalid field name detected.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
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

            // Check the line for the last non-empty field value.
            var lastFieldIndex = -1;
            if (_options.SuppressTrailingBlankFields)
            {
                lastFieldIndex = GetLastNonEmptyFieldIndex(csvLine);
            }

            // Extract the serialized field values that will comprise the CSV line.  All of them are extracted before any is
            // written, so a line that fails part way through writes nothing.
            var fieldValueCount = csvLine is CsvFileHeaderLine headerLine
                ? GetHeaderLineFieldValues(headerLine, lastFieldIndex)
                : GetRecordLineFieldValues((TRecordLine)csvLine, lastFieldIndex);

            // Write line to CSV output.
            WriteFieldValues(fieldValueCount);
        }

        private int GetHeaderLineFieldValues(CsvFileHeaderLine headerLine, int lastFieldIndex)
        {
            ICsvFileLine csvLine = headerLine;
            _headerLine = headerLine;

            // Extract field names from the header line.
            var fieldValues = GetFieldValueBuffer(csvLine.Count);
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

                fieldValues[fieldValueCount++] = rawFieldValue;
            }

            // Store a copy of the header line's field names so fields on the record lines can be correctly ordered.
            var headerLineFieldNames = new string[fieldValueCount];
            Array.Copy(fieldValues, headerLineFieldNames, fieldValueCount);
            _headerLineFieldNames = headerLineFieldNames;

            return fieldValueCount;
        }

        private int GetRecordLineFieldValues(TRecordLine recordLine, int lastFieldIndex)
        {
            ICsvFileLine csvLine = recordLine;

            // Without a header line, the fields to output include every field seen so far on a record line.
            if (_headerLineFieldNames.Length == 0 && csvLine.Count > _fieldNames.Length)
            {
                AddLineFieldNames(csvLine);
            }

            var fields = GetFieldPlan(recordLine).Fields;

            // Check for an inconsistent field count.
            // ref. RFC 4180§2¶4
            if (_previousLineFieldCount > 0 && fields.Length != _previousLineFieldCount)
            {
                if (_options.InconsistentFieldCountHandler is CsvFieldCount<TRecordLine> fieldCountHandler)
                {
                    var context = new CsvFieldContext<TRecordLine>(_options) { HeaderLine = _headerLine, Model = recordLine };
                    fieldCountHandler(context);
                    return 0;
                }

                var message = $"Expected {_previousLineFieldCount} fields; found {fields.Length} fields.";
                var dataItems = new Dictionary<string, object> { { DataItemName.ExpectedFieldCount, _previousLineFieldCount }, { DataItemName.ActualFieldCount, fields.Length } };
                throw BuildException(message, dataItems);
            }

            _previousLineFieldCount = fields.Length;

            // Extract serialized field values.
            var fieldValues = GetFieldValueBuffer(fields.Length);
            var fieldValueCount = 0;
            var fieldIndex = 0;
            foreach (var field in fields)
            {
                // If a value has not been set for the current field, add a blank value.
                if (!csvLine.Contains(field.FieldName))
                {
                    fieldValues[fieldValueCount++] = string.Empty;
                    continue;
                }

                // If the remaining field values are empty, stop here.
                if (lastFieldIndex > -1 && fieldIndex++ > lastFieldIndex)
                {
                    break;
                }

                // Convert the field value to a string.
                var rawFieldValue = SerializeFieldValue(recordLine, field);

                // Apply embedded line break on field value.  A value without a line break is unchanged by this, so is skipped.
                if (_options.EmbeddedLineBreak != null && !string.IsNullOrEmpty(rawFieldValue) && rawFieldValue.IndexOfAny(LineBreakCharacters) >= 0)
                {
                    rawFieldValue = NormalizeFieldValueLineBreaks(rawFieldValue, _options);
                }

                fieldValues[fieldValueCount++] = rawFieldValue ?? string.Empty;
            }

            return fieldValueCount;
        }

        private void AddLineFieldNames(ICsvFileLine csvLine)
        {
            var fieldNames = new List<string>(_fieldNames);
            var knownFieldNames = new HashSet<string>(_fieldNames);
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
                && (fieldPlan.MapperLineType is null || fieldPlan.MapperLineType == recordLine.GetType()))
            {
                return fieldPlan;
            }

            // A mapper that is not registered was created from the record line's type, so the plan only holds for lines of that type.
            var mapper = GetMapper(recordLine, out var isMapperRegistered);
            fieldPlan = new FieldPlan(mapperVersion, isMapperRegistered ? null : recordLine.GetType(), mapper, _headerLineFieldNames, _fieldNames);
            _fieldPlan = fieldPlan;
            return fieldPlan;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? SerializeFieldValue(TRecordLine recordLine, FieldWritePlan field)
#else
        [return: MaybeNull]
        private string SerializeFieldValue([DisallowNull] TRecordLine recordLine, [DisallowNull] FieldWritePlan field)
#endif
        {
            ICsvFileLine csvLine = recordLine;
            if (!field.IsMapped)
            {
                var fieldValue = csvLine[field.FieldName];
                return fieldValue is null ? null : fieldValue is string stringFieldValue ? stringFieldValue : FormatFieldValue(fieldValue, _options);
            }

            // Get the mapped field value.  The line has a field with the mapped field name, so the value is read by that name.
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
                mappedFieldValue = csvLine[field.FieldName];
            }

            if (mappedFieldValue is null)
            {
                return null; // if there is no field value then there is nothing to serialize
            }
            else if (mappedFieldValue is string serializedFieldValue)
            {
                return serializedFieldValue; // if the field value is already of the target type then just return it
            }

            return ConvertFieldValueToString(_headerLine, recordLine, _options, field.Mapping, field.Converter, ResolveTypeConverter(field), mappedFieldValue);
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
        /// Converts a mapped field value that is not already a string: with the mapping's converter if it has one, else its type converter if that can
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
                                      || (!string.IsNullOrWhiteSpace(rawFieldValue) && !long.TryParse(rawFieldValue, NumberStyles.Integer, culture, out _)
                                          && !decimal.TryParse(rawFieldValue, NumberStyles.Number, culture, out _));
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
                _quoteSearchValues = SearchValues.Create(GetQuoteSearchCharacters(fieldDelimiter, embeddedLineBreak, terminator));
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
                _quoteSearchCharacters = GetQuoteSearchCharacters(fieldDelimiter, embeddedLineBreak, terminator);
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

            // The hit may be only part of the embedded line break sequence or of the terminator, so confirm it.  A line break, quote or delimiter
            // always requires quoting, even when an embedded line break sequence replaces the line breaks of record values, since a header name has
            // no such replacement.
            // ref. RFC 4180§2¶6
            return fieldValue.IndexOfAny(RequiredQuoteCharacters(fieldDelimiter)) >= 0
                || (embeddedLineBreak != null && fieldValue.IndexOf(embeddedLineBreak.AsSpan()) >= 0)
                || (terminator.Length > 0 && fieldValue.IndexOf(terminator.AsSpan()) >= 0);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static char[] GetQuoteSearchCharacters(char fieldDelimiter, string? embeddedLineBreak, string terminator)
#else
        private static char[] GetQuoteSearchCharacters(char fieldDelimiter, [AllowNull] string embeddedLineBreak, string terminator)
#endif
        {
            var embedded = embeddedLineBreak ?? string.Empty;
            var searchCharacters = new char[embedded.Length + terminator.Length + 4];
            embedded.CopyTo(0, searchCharacters, 0, embedded.Length);
            terminator.CopyTo(0, searchCharacters, embedded.Length, terminator.Length);
            var next = embedded.Length + terminator.Length;
            searchCharacters[next] = '\r';
            searchCharacters[next + 1] = '\n';
            searchCharacters[next + 2] = '"';
            searchCharacters[next + 3] = fieldDelimiter;
            return searchCharacters;
        }

        private static char[] RequiredQuoteCharacters(char fieldDelimiter)
        {
            return new[] { '\r', '\n', '"', fieldDelimiter };
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
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal FieldPlan(int mapperVersion, Type? mapperLineType, ICsvFileMapper mapper, string[] headerLineFieldNames, string[] lineFieldNames)
#else
            internal FieldPlan(int mapperVersion, [AllowNull] Type mapperLineType, [DisallowNull] ICsvFileMapper mapper, [DisallowNull] string[] headerLineFieldNames,
                [DisallowNull] string[] lineFieldNames)
#endif
            {
                MapperVersion = mapperVersion;
                MapperLineType = mapperLineType;
                HeaderLineFieldNames = headerLineFieldNames;
                LineFieldNames = lineFieldNames;

                // Fields are output in header line order if there is one, else in mapper order if there are mappings, else in the order they were first seen on a record line.
                string[] fieldNames;
                if (headerLineFieldNames.Length > 0)
                {
                    fieldNames = headerLineFieldNames;
                }
                else if (mapper.Count > 0)
                {
                    fieldNames = CsvMappingService.GetSortedFieldNamesFromMapper(mapper);
                }
                else
                {
                    fieldNames = lineFieldNames;
                }

                // Where more than one mapping has the same field name, the first is used.
                var mappingsByFieldName = new Dictionary<string, KeyValuePair<string, CsvFieldMapping>>(mapper.Count, StringComparer.Ordinal);
                foreach (var mapping in mapper)
                {
                    var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(mapping);
                    if (!mappingsByFieldName.ContainsKey(mappedFieldName))
                    {
                        mappingsByFieldName.Add(mappedFieldName, mapping);
                    }
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
            /// Gets the name of the field, which is also the key of its value on a record line.
            /// </summary>
            internal string FieldName { get; }

            /// <summary>
            /// Gets a flag indicating if the field has a mapping.
            /// </summary>
            internal bool IsMapped { get; }

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