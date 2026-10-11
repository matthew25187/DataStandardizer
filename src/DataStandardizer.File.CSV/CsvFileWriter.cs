using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP3_0_OR_GREATER
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
        private const char QuoteCharacter = '"';
        private static readonly char[] LineBreakCharacters = { '\r', '\n' };

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private FieldPlan? _fieldPlan;
        private CsvFileHeaderLine? _headerLine;
#else
        [CanBeNull] private FieldPlan _fieldPlan;
        [CanBeNull] private CsvFileHeaderLine _headerLine;
#endif
#if !NETSTANDARD2_1_OR_GREATER && !NETCOREAPP2_1_OR_GREATER
        private char[] _fieldValueCharacters = Array.Empty<char>();
#endif
        private string[] _fieldNames = Array.Empty<string>();
        private string[] _fieldValues = Array.Empty<string>();
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
            if (options is null)
                throw new ArgumentNullException(nameof(options));

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

        public CsvFileWriter(string csvFilePath, CsvFileOptions options)
        {
            if (csvFilePath is null)
                throw new ArgumentNullException(nameof(csvFilePath));
            if (options is null)
                throw new ArgumentNullException(nameof(options));

            _options = options;

            // Open the file once, with the requested encoding if any, so no handle is left orphaned.
            _writer = _options.Encoding != null
                ? new StreamWriter(csvFilePath, false, _options.Encoding)
                : new StreamWriter(csvFilePath);
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
            _options = options;

            if (_options.Encoding != null)
            {
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(writer));
            }
        }

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
        /// <param name="csvLine">Line containing fields to be written.</param>
        /// <exception cref="CsvFileException">
        /// Invalid field name detected.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        public void WriteLine(ICsvFileLine csvLine)
        {
            ThrowIfDisposed();

            // Check the line for the last non-empty field value.
            var lastFieldIndex = -1;
            if (_options.SuppressTrailingBlankFields)
            {
                lastFieldIndex = GetLastNonEmptyFieldIndex(csvLine);
            }

            // Extract the serialized field values that will comprise the CSV line.  All of them are extracted before any is
            // written, so a line that fails part way through writes nothing.
            var fieldValueCount = 0;
            switch (csvLine)
            {
                case CsvFileHeaderLine headerLine:
                    fieldValueCount = GetHeaderLineFieldValues(headerLine, lastFieldIndex);
                    break;
                case TRecordLine recordLine:
                    fieldValueCount = GetRecordLineFieldValues(recordLine, lastFieldIndex);
                    break;
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
            var fieldValueCount = 0;
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
            foreach (var key in csvLine.Keys)
            {
                var fieldName = (string)key;
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
            var mapperCacheVersion = MapperCacheVersion;
            var fieldPlan = _fieldPlan;
            if (fieldPlan != null
                && fieldPlan.MapperCacheVersion == mapperCacheVersion
                && ReferenceEquals(fieldPlan.HeaderLineFieldNames, _headerLineFieldNames)
                && ReferenceEquals(fieldPlan.LineFieldNames, _fieldNames)
                && (fieldPlan.UncachedMapperLineType is null || fieldPlan.UncachedMapperLineType == recordLine.GetType()))
            {
                return fieldPlan;
            }

            // A mapper that is not cached was created from the record line's type, so the plan only holds for lines of that type.
            var mapper = GetMapper(recordLine, out var isMapperCached);
            fieldPlan = new FieldPlan(mapperCacheVersion, isMapperCached ? null : recordLine.GetType(), mapper, _headerLineFieldNames, _fieldNames);
            _fieldPlan = fieldPlan;
            return fieldPlan;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? SerializeFieldValue(TRecordLine recordLine, FieldWritePlan field)
#else
        [CanBeNull]
        private string SerializeFieldValue([NotNull] TRecordLine recordLine, [NotNull] FieldWritePlan field)
#endif
        {
            ICsvFileLine csvLine = recordLine;
            if (!field.IsMapped)
            {
                var fieldValue = csvLine[field.FieldName];
                return fieldValue is null ? null : fieldValue as string ?? FormatFieldValue(fieldValue, _options);
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
        [CanBeNull]
        private TypeConverter ResolveTypeConverter([NotNull] FieldWritePlan field)
#endif
        {
            var typeConverterType = field.Mapping.Value.TypeConverterType;
            if (field.Converter != null || typeConverterType is null)
            {
                return null;
            }

            if (!field.IsTypeConverterResolved)
            {
                field.TypeConverter = GetTypeConverter(typeConverterType);
                field.IsTypeConverterResolved = true;
            }

            return field.TypeConverter;
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
        /// Mappers are shared by every reader and writer, so the plan only reads from the mapper and its field mappings and never writes to them.
        /// </remarks>
        private sealed class FieldPlan
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal FieldPlan(int mapperCacheVersion, Type? uncachedMapperLineType, ICsvFileMapper mapper, string[] headerLineFieldNames, string[] lineFieldNames)
#else
            internal FieldPlan(int mapperCacheVersion, [CanBeNull] Type uncachedMapperLineType, [NotNull] ICsvFileMapper mapper, [NotNull] string[] headerLineFieldNames,
                [NotNull] string[] lineFieldNames)
#endif
            {
                MapperCacheVersion = mapperCacheVersion;
                UncachedMapperLineType = uncachedMapperLineType;
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

            internal int MapperCacheVersion { get; }

            /// <summary>
            /// Gets the record line type the mapper was created from when it is not cached, or <c>null</c> if it is cached.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal Type? UncachedMapperLineType { get; }
#else
            [CanBeNull] internal Type UncachedMapperLineType { get; }
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
            [CanBeNull] internal FieldValueAccessor Accessor { get; }
#endif

            /// <summary>
            /// Gets the mapping's converter delegate, if it has one.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal CsvFieldConvertToString<TRecordLine>? Converter { get; }
#else
            [CanBeNull] internal CsvFieldConvertToString<TRecordLine> Converter { get; }
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
            [CanBeNull] internal TypeConverter TypeConverter { get; set; }
#endif
        }
    }
}