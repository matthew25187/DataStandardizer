using System;
#if NET8_0_OR_GREATER
using System.Buffers;
#endif
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
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
            internal const string FieldIndex = "FieldIndex";
            internal const string FieldName = "FieldName";
            internal const string FieldNames = "FieldNames";
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
        private static readonly char[] NoCharacters = new char[0];
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
#if NET8_0_OR_GREATER
        private SearchValues<char>? _textDataSearchValues;
        private bool _textDataSearchExcept;
        private bool _textDataSearchC1Separately;
#else
        private char[] _textDataExemptCharacters = new char[0];
#endif
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private CsvTextData? _textData;
#else
        [MaybeNull] private CsvTextData _textData;
#endif
        private char _textDataDelimiter;
        private string _textDataTerminator = string.Empty;
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

            ValidateHandlers(options, nameof(options));
            ValidateOptionsArgument(options, nameof(options));

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

            ValidateHandlers(options, nameof(options));
            ValidateOptionsArgument(options, nameof(options));

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

            ValidateHandlers(_options, nameof(options));
            ValidateOptionsArgument(_options, nameof(options));

            if (_options.Encoding != null)
            {
                throw new ArgumentException("Unable to apply an encoding to an existing writer.  Specify the encoding when creating the writer.", nameof(writer));
            }
        }

        /// <summary>
        /// Checks that the inconsistent field count handler set in the options is of the type the writer calls.  The writer never calls the bad-value
        /// or header handler, so they aren't checked, and options can be shared with a reader of another line type.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// <see cref="ICsvFileOptions.InconsistentFieldCountHandler"/> isn't a <see cref="CsvFieldCount{TModel}"/> of <typeparamref name="TRecordLine"/>.
        /// </exception>
        private static void ValidateHandlers(ICsvFileOptions options, string paramName)
        {
            ValidateHandler<CsvFieldCount<TRecordLine>>(options.InconsistentFieldCountHandler, nameof(ICsvFileOptions.InconsistentFieldCountHandler), paramName);
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
        /// Invalid field name detected, or a header line has no fields, or a header line has no field for a required mapping by name that doesn't
        /// supply a constant or generated value.  Or a field value holds a character that RFC 4180§2 doesn't allow in a
        /// field, as it isn't a printable character of the writer's character set.  Or a record line has no fields to write, or a different number of fields
        /// from the previous record line, or a field with a value that isn't one of the fields written, and
        /// <see cref="ICsvFileOptions.InconsistentFieldCountHandler"/> isn't set.  Nothing is written.
        /// </exception>
        /// <exception cref="ObjectDisposedException">The writer has been disposed.</exception>
        /// <exception cref="InvalidOperationException">
        /// The options, changed since the writer was created, would write output that can't be read back:
        /// <see cref="ICsvFileOptions.TerminatorLineBreak"/> is <c>null</c> or empty or starts with a double quote,
        /// <see cref="ICsvFileOptions.FieldDelimiterCharacter"/> is a double quote or the first character of the terminator line break, or
        /// <see cref="ICsvFileOptions.QuoteHandling"/> isn't a defined <see cref="CsvFieldQuoteHandling"/> value.  Or the field delimiter, or a
        /// character of the terminator line break, isn't in the character set of the writer's encoding, or a character of
        /// <see cref="ICsvFileOptions.EmbeddedLineBreak"/> other than CR or LF isn't a printable character of it.  Nothing is written.
        /// </exception>
        /// <remarks>
        /// A record line that has a different number of fields from the previous record line breaks RFC 4180§2¶4.  When
        /// <see cref="ICsvFileOptions.InconsistentFieldCountHandler"/> is set, it is called for the line, and the line is skipped: nothing is
        /// written for it, and the lines after it are checked against the same field count as before it.
        /// <para>
        /// A line with no fields is never written, as a header line and each record have one or more fields (RFC 4180§2¶3-4).  A header line with
        /// no fields throws.  A record line has no fields to write when there's no header line, no mapping and no record line written before it,
        /// and it has no fields of its own.  It is handled or throws in the same way as a line with a different number of fields.  A record line
        /// with one field whose value is blank is a record, so it is written, as an empty line, and is read back as that record.
        /// </para>
        /// <para>
        /// A field mapped by index is written at its index, on the header line and on each record line.  The other fields fill the positions
        /// left, in turn from position 0.  With a header line, they are the header line's other fields that a mapping covers by name, followed by
        /// those that no mapping covers, each in the header line's order.  A header line that has
        /// none of the names of the mappings at an index gets the field name of the first of them, or else its property name, at that index.
        /// Without a header line, they are the fields mapped by name, in mapper order, followed by the fields of the first record line written
        /// that no mapping covers, in the order they're on that line.  A position still free below the highest index is written as a blank
        /// field.  Without a header line, the fields are at the positions a reader with the same mapper reads them from.  A header line is laid out
        /// with the registered mapper, or else the declarative mapper of <typeparamref name="TRecordLine"/>, as it has no record type of its own.
        /// To lay out the header line as the record lines are, for record lines of a derived type with mappings of their own, register a mapper.
        /// </para>
        /// <para>
        /// With a header line, the fields written are those of the header line, as laid out.  Without one, they are the mapped fields and the
        /// fields of the first record line written that no mapping covers.  A record line with a field that isn't one
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

            // The options are checked on each line, as they can be changed after the writer is created on targets older than .NET 5.  They are checked
            // before the line changes the writer's state, such as the fields to write.
            if (!TryValidateOptions(_options, out var optionsMessage) || !TryValidateCharacterSetOptions(out optionsMessage))
                throw new InvalidOperationException(optionsMessage);

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

            // The converters, generators and handlers that ran may have changed the options, so they are checked again before the line is written.
            if (!TryValidateOptions(_options, out optionsMessage) || !TryValidateCharacterSetOptions(out optionsMessage))
            {
                Array.Clear(_fieldValues, 0, fieldValueCount);
                throw new InvalidOperationException(optionsMessage);
            }

            // Write line to CSV output.
            WriteFieldValues(fieldValueCount);
        }

        /// <summary>
        /// Checks that the options describe output that can be read back as the lines written.
        /// </summary>
        /// <param name="options">The options to check.</param>
        /// <param name="message">The reason the options are invalid; empty when they are valid.</param>
        /// <returns><c>true</c> if the options are valid; <c>false</c> if they aren't.</returns>
        private static bool TryValidateOptions(CsvFileOptions options, out string message)
        {
            // Without a terminator, the records would run together.
            // ref. RFC 4180§2¶1
            var terminator = options.TerminatorLineBreak;
            if (string.IsNullOrEmpty(terminator))
            {
                message = "The terminator line break must have at least one character.";
                return false;
            }

            // A terminator that starts with a quote, written after the closing quote of an escaped field, reads as an escaped quote, so the field
            // would run on into the records after it.
            // ref. RFC 4180§2¶7
            if (terminator[0] == QuoteCharacter)
            {
                message = "The terminator line break can't start with a double quote.";
                return false;
            }

            // A quote delimiter can't be told apart from the quotes around an escaped field, and a delimiter that starts the terminator can't be told
            // apart from the end of the record, such as after an empty field, so the fields couldn't always be read back.
            // ref. RFC 4180§2¶5-6
            var fieldDelimiter = options.FieldDelimiterCharacter;
            if (fieldDelimiter == QuoteCharacter)
            {
                message = "The field delimiter can't be a double quote.";
                return false;
            }

            if (fieldDelimiter == terminator[0])
            {
                message = "The field delimiter can't be the first character of the terminator line break.";
                return false;
            }

            // Any other value would never quote a field, even one that RFC 4180 requires to be quoted.
            // ref. RFC 4180§2¶6
            if ((uint)options.QuoteHandling > (uint)CsvFieldQuoteHandling.Required)
            {
                message = $"The quote handling '{options.QuoteHandling}' isn't a defined {nameof(CsvFieldQuoteHandling)} value.";
                return false;
            }

            message = string.Empty;
            return true;
        }

        /// <summary>
        /// Checks the options passed to a constructor, before any output is opened.
        /// </summary>
        /// <exception cref="ArgumentException">The options would write output that can't be read back.</exception>
        private static void ValidateOptionsArgument(CsvFileOptions options, string paramName)
        {
            if (!TryValidateOptions(options, out var message))
                throw new ArgumentException(message, paramName);
        }

        private int GetHeaderLineFieldValues(CsvFileHeaderLine headerLine, int lastFieldIndex)
        {
            ICsvFileLine csvLine = headerLine;

            // A header line with no fields would be written as an empty line, which isn't a header line, as it has one or more fields.
            // ref. RFC 4180§2¶3-4
            if (csvLine.Count == 0)
            {
                throw BuildException("A header line has no fields.", new Dictionary<string, object> { { DataItemName.ActualFieldCount, 0 } });
            }

            // Extract field names from the header line.
            var fieldNames = new string[csvLine.Count];
            var fieldNameCount = 0;
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

                fieldNames[fieldNameCount++] = rawFieldValue;
            }

            // The fields mapped by index are put at their indexes, and the header line's other fields fill the positions left, in order.
            var mapper = GetMapper();
            var headerLineFieldNames = LayOutHeaderLineFieldNames(fieldNames, fieldNameCount, mapper);
            var fileFieldNames = GetFileFieldNames(mapper);
            var writtenFieldNames = headerLineFieldNames;
            if (fileFieldNames != null)
            {
                writtenFieldNames = new string[headerLineFieldNames.Length];
                for (var fieldIndex = 0; fieldIndex < writtenFieldNames.Length; fieldIndex++)
                {
                    writtenFieldNames[fieldIndex] = fileFieldNames.TryGetValue(headerLineFieldNames[fieldIndex], out var fileFieldName)
                        ? fileFieldName
                        : headerLineFieldNames[fieldIndex];
                }

                ThrowIfNotReadBack(writtenFieldNames, headerLineFieldNames, mapper);
            }

            // Each name is kept as it is, to order the fields on the record lines, and written with the embedded line break, as the reader, which
            // applies it to every field, will give it back.  A field that a mapping by name renames is written under its name in the file, which
            // the header line may repeat.
            var fieldValues = GetFieldValueBuffer(headerLineFieldNames.Length);
            for (var fieldIndex = 0; fieldIndex < headerLineFieldNames.Length; fieldIndex++)
            {
                var fieldName = writtenFieldNames[fieldIndex];
                var headerFieldValue = _options.EmbeddedLineBreak != null && fieldName.IndexOfAny(LineBreakCharacters) >= 0
                    ? NormalizeFieldValueLineBreaks(fieldName, _options)
                    : fieldName;
                ValidateFieldValueCharacters(headerFieldValue, fieldIndex);
                fieldValues[fieldIndex] = headerFieldValue;
            }

            _headerLine = headerLine;
            _headerLineFieldNames = headerLineFieldNames;

            return headerLineFieldNames.Length;
        }

        /// <summary>
        /// Gets the name in the file of each field that a mapping by name renames with <see cref="CsvFieldMapping.UseFieldName"/>, keyed by the name
        /// it is renamed to.  Where mappings that share a field name it differently in the file, the first one's name is used.
        /// </summary>
        /// <returns>The names, or <c>null</c> if no mapping by name renames its field.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static Dictionary<string, string>? GetFileFieldNames(ICsvFileMapper mapper)
#else
        [return: MaybeNull]
        private static Dictionary<string, string> GetFileFieldNames(ICsvFileMapper mapper)
#endif
        {
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            Dictionary<string, string>? fileFieldNames = null;
#else
            Dictionary<string, string> fileFieldNames = null;
#endif
            foreach (var mapping in mapper)
            {
                if (mapping.Value.UseFieldName is string useFieldName && !mapping.Value.FieldIndex.HasValue)
                {
                    if (fileFieldNames is null)
                    {
                        fileFieldNames = new Dictionary<string, string>(StringComparer.Ordinal);
                    }

                    if (!fileFieldNames.ContainsKey(useFieldName))
                    {
                        fileFieldNames.Add(useFieldName, CsvMappingService.GetFileFieldNameFromMapping(mapping));
                    }
                }
            }

            return fileFieldNames;
        }

        /// <summary>
        /// Checks that the mapper reads the names a header line is written with back as the names the line has, as the reader names them.
        /// </summary>
        /// <exception cref="CsvFileException">The mapper can't name the fields written, or names them differently, such as a renamed field written
        /// under a name that another field of the line already has.</exception>
        private static void ThrowIfNotReadBack(string[] writtenFieldNames, string[] headerLineFieldNames, ICsvFileMapper mapper)
        {
            if (CsvMappingService.TryResolveHeaderFieldNames(writtenFieldNames, mapper, out var readFieldNames, out _, out _)
                && readFieldNames.SequenceEqual(headerLineFieldNames, StringComparer.Ordinal))
            {
                return;
            }

            var writtenHeaderLine = string.Join(",", writtenFieldNames);
            throw BuildException($"The header line would be written as '{writtenHeaderLine}', which the mapper doesn't read back with the same field names.",
                new Dictionary<string, object> { { DataItemName.FieldNames, writtenFieldNames } });
        }

        /// <summary>
        /// Lays out the field names of a header line around the fields that the mapper maps by index.
        /// </summary>
        /// <remarks>
        /// A field mapped by index goes at its index.  Its name is the first name, of the mappings at that index, that the header line has, or else
        /// the field name of the first of them.  The header line's other fields fill the positions left, in turn from position 0: first those that a
        /// mapping covers by name, then those that no mapping covers, each in the header line's order.  A position still free below the highest
        /// index is a blank field.
        /// </remarks>
        /// <returns>The field names in the order they are written, with an empty name for each blank field.</returns>
        /// <exception cref="CsvFileException">
        /// A required mapping by name, which doesn't supply a constant or generated value, has no field on the header line, so a reader with the
        /// mapper couldn't read the file.
        /// </exception>
        private static string[] LayOutHeaderLineFieldNames(string[] fieldNames, int fieldNameCount, ICsvFileMapper mapper)
        {
            var headerFieldNames = new HashSet<string>(StringComparer.Ordinal);
            for (var fieldIndex = 0; fieldIndex < fieldNameCount; fieldIndex++)
            {
                headerFieldNames.Add(fieldNames[fieldIndex]);
            }

            // The names of the mappings at each index, in mapper order, and the names of the mappings by name.  A required mapping by name reads
            // nothing from a file whose header line doesn't have its field, so the reader throws.
            var mappedFieldNamesByIndex = new SortedDictionary<int, List<string>>();
            var nameMappedFieldNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapping in mapper)
            {
                var mappedFieldName = CsvMappingService.GetFieldNameFromMapping(mapping);
                if (mapping.Value.FieldIndex is int mappedIndex)
                {
                    if (!mappedFieldNamesByIndex.TryGetValue(mappedIndex, out var mappedFieldNames))
                    {
                        mappedFieldNames = new List<string>();
                        mappedFieldNamesByIndex.Add(mappedIndex, mappedFieldNames);
                    }

                    mappedFieldNames.Add(mappedFieldName);
                    continue;
                }

                nameMappedFieldNames.Add(mappedFieldName);
                if (!headerFieldNames.Contains(mappedFieldName) && !mapping.Value.IsOptional && !mapping.Value.SuppliesValue)
                {
                    throw BuildException($"The header line has no field '{mappedFieldName}', which a required mapping is mapped to.",
                        new Dictionary<string, object> { { DataItemName.FieldName, mappedFieldName } });
                }
            }

            // A name that the header line has, and that no lower index has taken, is preferred to the first mapping's name, so the header line's own
            // field moves to the index.
            var indexedFieldNames = new SortedDictionary<int, string>();
            var takenFieldNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mappedFieldNames in mappedFieldNamesByIndex)
            {
                var indexedFieldName = mappedFieldNames.Value[0];
                foreach (var mappedFieldName in mappedFieldNames.Value)
                {
                    if (headerFieldNames.Contains(mappedFieldName) && !takenFieldNames.Contains(mappedFieldName))
                    {
                        indexedFieldName = mappedFieldName;
                        break;
                    }
                }

                indexedFieldNames.Add(mappedFieldNames.Key, indexedFieldName);
                takenFieldNames.Add(indexedFieldName);
            }

            var layout = new List<string>(fieldNameCount + indexedFieldNames.Count);
            var position = 0;
            AddLayoutFieldNames(layout, ref position, fieldNames, fieldNameCount, indexedFieldNames, takenFieldNames, nameMappedFieldNames, true);
            AddLayoutFieldNames(layout, ref position, fieldNames, fieldNameCount, indexedFieldNames, takenFieldNames, nameMappedFieldNames, false);
            foreach (var indexedFieldName in indexedFieldNames)
            {
                SetLayoutFieldName(layout, indexedFieldName.Key, indexedFieldName.Value);
            }

            return layout.ToArray();
        }

        /// <summary>
        /// Puts the header line's fields that a mapping covers by name, or those that no mapping covers, in the free positions of a layout, in the
        /// header line's order.
        /// </summary>
        private static void AddLayoutFieldNames(List<string> layout, ref int position, string[] fieldNames, int fieldNameCount,
            SortedDictionary<int, string> indexedFieldNames, HashSet<string> takenFieldNames, HashSet<string> nameMappedFieldNames, bool isNameMapped)
        {
            for (var fieldIndex = 0; fieldIndex < fieldNameCount; fieldIndex++)
            {
                var fieldName = fieldNames[fieldIndex];
                if (takenFieldNames.Contains(fieldName) || nameMappedFieldNames.Contains(fieldName) != isNameMapped)
                {
                    continue;
                }

                while (indexedFieldNames.ContainsKey(position))
                {
                    position++;
                }

                SetLayoutFieldName(layout, position++, fieldName);
            }
        }

        /// <summary>
        /// Puts a field name at a position of a layout, adding blank fields up to that position.
        /// </summary>
        private static void SetLayoutFieldName(List<string> layout, int position, string fieldName)
        {
            while (layout.Count <= position)
            {
                layout.Add(string.Empty);
            }

            layout[position] = fieldName;
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

                // The unmapped fields output in the positions the mapped fields leave are those that the first line's own mapper doesn't cover, so a line of another type,
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

            // A line with no fields to write would be written as an empty line, which isn't a record, as a record has one or more fields.
            // ref. RFC 4180§2¶4
            if (fields.Length == 0)
            {
                return HandleInconsistentLine(recordLine, "A record line has no fields to write.",
                    new Dictionary<string, object> { { DataItemName.ActualFieldCount, 0 } });
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
                if (!TryGetFieldValueSource(recordLine, field, out var fieldMapping, out var fieldKey))
                {
                    // If a value has not been set for the current field, or no field is at its position, add a blank value.
                    fieldValues[fieldValueCount++] = string.Empty;
                    continue;
                }

                // Convert the field value to a string.  A converter is given the field's position in the line.  A constant or generated value
                // didn't come from a field, so has no position, as when reading, though the mapping builder doesn't let a mapping that supplies
                // its value have a converter.
                var fieldIndex = fieldMapping != null && fieldMapping.IsSupplied ? (int?)null : fieldValueCount;
                var rawFieldValue = SerializeFieldValue(recordLine, fieldMapping, fieldKey, field.FieldName, fieldIndex);

                // Apply embedded line break on field value.  A value without a line break is unchanged by this, so is skipped.
                if (_options.EmbeddedLineBreak != null && !string.IsNullOrEmpty(rawFieldValue) && rawFieldValue.IndexOfAny(LineBreakCharacters) >= 0)
                {
                    rawFieldValue = NormalizeFieldValueLineBreaks(rawFieldValue, _options);
                }

                rawFieldValue = rawFieldValue ?? string.Empty;
                ValidateFieldValueCharacters(rawFieldValue, fieldValueCount);
                fieldValues[fieldValueCount++] = rawFieldValue;
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
            fieldPlan = new FieldPlan(mapperVersion, isMapperRegistered ? null : recordLine.GetType(), mapper, _headerLineFieldNames, _fieldNames, _unmappedFieldNames);
            _fieldPlan = fieldPlan;
            return fieldPlan;
        }

        /// <summary>
        /// Finds where the value of a field to output comes from: the mapping that supplies it or whose value the line holds, and the key the line
        /// holds the value under.
        /// </summary>
        /// <remarks>
        /// A constant or generated value is supplied by the mapping, so is written whether or not the line holds the field, as it is read.  A line
        /// read or converted with the mapping holds any other mapped value under the field name.  A line built from the model holds it under the
        /// mapping's key, the property key, which differs from the field name when the field is renamed.  Where several mappings share the
        /// field, only one value can be written, so the last mapping, in mapper order, that has a value gives it.
        /// </remarks>
        /// <returns><c>true</c> if the field has a value; <c>false</c> if it is written as a blank value.</returns>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static bool TryGetFieldValueSource(TRecordLine recordLine, FieldWritePlan field, out FieldMappingWritePlan? fieldMapping, out string fieldKey)
#else
        private static bool TryGetFieldValueSource([DisallowNull] TRecordLine recordLine, [DisallowNull] FieldWritePlan field,
            [MaybeNull] out FieldMappingWritePlan fieldMapping, out string fieldKey)
#endif
        {
            ICsvFileLine csvLine = recordLine;
            var fieldName = field.FieldName;
            var mappings = field.Mappings;
            for (var mappingIndex = mappings.Length - 1; mappingIndex >= 0; mappingIndex--)
            {
                // A mapping's value is looked for under its own field name and property key first, so a mapping that shares the field never
                // takes another mapping's value.
                var mapping = mappings[mappingIndex];
                fieldMapping = mapping;
                if (mapping.IsSupplied)
                {
                    fieldKey = fieldName;
                    return true;
                }

                if (csvLine.Contains(mapping.FieldName))
                {
                    fieldKey = mapping.FieldName;
                    return true;
                }

                // A mapper is keyed by property key, so the mapping's key is the key the line holds the property's value under.
                var propertyKey = mapping.Mapping.Key;
                if (csvLine.Contains(propertyKey))
                {
                    fieldKey = propertyKey;
                    return true;
                }
            }

            // Otherwise the line may hold the value under the name of the field it's output as, such as a header line's name for a field mapped by
            // index.  That value is given by the last mapping, or is the field's own value if no mapping covers it.
            fieldMapping = mappings.Length > 0 ? mappings[mappings.Length - 1] : null;
            fieldKey = fieldName;
            return fieldName.Length > 0 && csvLine.Contains(fieldName);
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private string? SerializeFieldValue(TRecordLine recordLine, FieldMappingWritePlan? field, string fieldKey, string fieldName, int? fieldIndex)
#else
        [return: MaybeNull]
        private string SerializeFieldValue([DisallowNull] TRecordLine recordLine, [AllowNull] FieldMappingWritePlan field, [DisallowNull] string fieldKey,
            [DisallowNull] string fieldName, int? fieldIndex)
#endif
        {
            ICsvFileLine csvLine = recordLine;
            if (field is null)
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

            return ConvertFieldValueToString(_headerLine, recordLine, _options, fieldName, fieldIndex, field.Converter, typeConverter, mappedFieldValue);
        }

        /// <summary>
        /// Gets the type converter for a mapped field that has no converter delegate, resolving it on first use.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static TypeConverter? ResolveTypeConverter(FieldMappingWritePlan field)
#else
        [return: MaybeNull]
        private static TypeConverter ResolveTypeConverter([DisallowNull] FieldMappingWritePlan field)
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
        /// <param name="fieldName">The name of the field written, as the line names it, which a reader with the same mapper resolves it to.</param>
        /// <param name="fieldIndex">The position of the field in the line written, or <c>null</c> for a constant or generated value.</param>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? ConvertFieldValueToString(CsvFileHeaderLine? headerLine, TRecordLine recordLine, CsvFileOptions options, string fieldName, int? fieldIndex,
            CsvFieldConvertToString<TRecordLine>? converter, TypeConverter? typeConverter, object fieldValue)
#else
        [return: MaybeNull]
        private static string ConvertFieldValueToString([AllowNull] CsvFileHeaderLine headerLine, [DisallowNull] TRecordLine recordLine, [DisallowNull] CsvFileOptions options,
            [DisallowNull] string fieldName, int? fieldIndex, [AllowNull] CsvFieldConvertToString<TRecordLine> converter, [AllowNull] TypeConverter typeConverter, [DisallowNull] object fieldValue)
#endif
        {
            if (converter != null)
            {
                var context = new CsvFieldContext<TRecordLine>(options)
                {
                    HeaderLine = headerLine,
                    Model = recordLine,
                    FieldName = fieldName,
                    FieldIndex = fieldIndex
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
        /// Converts a field value to a string with <see cref="CsvFieldFormatter"/>, with the number symbols of the <see cref="ICsvFileOptions.Culture"/>,
        /// or the invariant culture if none is set, so the output never depends on the current culture.
        /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        private static string? FormatFieldValue(object fieldValue, ICsvFileOptions options)
#else
        [return: MaybeNull]
        private static string FormatFieldValue([DisallowNull] object fieldValue, [DisallowNull] ICsvFileOptions options)
#endif
        {
            return CsvFieldFormatter.Format(fieldValue, options.Culture ?? CultureInfo.InvariantCulture);
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

        /// <summary>
        /// Throws if a field value holds a character that RFC 4180 doesn't allow in a field.
        /// </summary>
        /// <remarks>
        /// A field holds TEXTDATA, read as the printable characters of the character set the writer writes: that of the encoding in the options,
        /// or the default, when the CSV writer creates the underlying writer, and otherwise that of the caller's writer.  An escaped field also holds
        /// the delimiter, CR, LF, the characters of the terminator line break and double quotes, which make the writer quote the value, so the
        /// characters that require quotes are allowed.
        /// ref. RFC 4180§2 (escaped, non-escaped, TEXTDATA), RFC 4180§3 (charset)
        /// </remarks>
        /// <exception cref="CsvFileException">The value holds a character that isn't allowed.</exception>
        private void ValidateFieldValueCharacters(string fieldValue, int fieldIndex)
        {
            if (fieldValue.Length == 0) return;

            var textData = GetTextData();

            var fieldDelimiter = _options.FieldDelimiterCharacter;
            var terminator = _options.TerminatorLineBreak ?? string.Empty;
            var isCurrent = _textDataDelimiter == fieldDelimiter && string.Equals(_textDataTerminator, terminator, StringComparison.Ordinal);
#if NET8_0_OR_GREATER
            if (_textDataSearchValues is null || !isCurrent)
            {
                _textDataSearchValues = SearchValues.Create(textData.GetSearchCharacters(GetRequiredQuoteCharacters(fieldDelimiter, terminator), NoCharacters,
                    out _textDataSearchExcept, out _textDataSearchC1Separately));
                _textDataDelimiter = fieldDelimiter;
                _textDataTerminator = terminator;
            }

            var searchValues = _textDataSearchValues;
            var searchExcept = _textDataSearchExcept;
            var searchC1Separately = _textDataSearchC1Separately;
#else
            if (_textDataExemptCharacters.Length == 0 || !isCurrent)
            {
                _textDataExemptCharacters = GetRequiredQuoteCharacters(fieldDelimiter, terminator);
                _textDataDelimiter = fieldDelimiter;
                _textDataTerminator = terminator;
            }

            var exemptCharacters = _textDataExemptCharacters;
#endif
            var start = 0;
            while (start < fieldValue.Length)
            {
                // Find the next character that isn't printable US-ASCII, or, when the character set is Unicode, the next control character.
                var remaining = fieldValue.AsSpan(start);
#if NET8_0_OR_GREATER
                var offset = searchExcept ? remaining.IndexOfAnyExcept(searchValues) : remaining.IndexOfAny(searchValues);
                if (searchC1Separately)
                {
                    var c1Offset = CsvTextData.IndexOfC1Control(offset < 0 ? remaining : remaining.Slice(0, offset));
                    if (c1Offset >= 0) offset = c1Offset;
                }
#else
                var offset = textData.IndexOfCharacterToCheck(remaining, exemptCharacters);
#endif
                if (offset < 0) return;

                // A surrogate pair is one character, so it is checked, and named, by its code point.
                var index = start + offset;
                var character = fieldValue[index];
                int codePoint;
                bool isAllowed;
                if (char.IsHighSurrogate(character) && index + 1 < fieldValue.Length && char.IsLowSurrogate(fieldValue[index + 1]))
                {
                    isAllowed = textData.IsAllowed(character, fieldValue[index + 1]);
                    codePoint = char.ConvertToUtf32(character, fieldValue[index + 1]);
                    start = index + 2;
                }
                else
                {
                    isAllowed = textData.IsAllowed(character);
                    codePoint = character;
                    start = index + 1;
                }

                if (!isAllowed)
                {
                    var message = string.Format(CultureInfo.InvariantCulture,
                        "The value of the field at index {0} holds U+{1:X4}, which isn't a printable character of the writer's character set, {2}, so RFC 4180 doesn't allow it in a field.",
                        fieldIndex, codePoint, textData.Name);
                    throw BuildException(message, new Dictionary<string, object> { { DataItemName.FieldIndex, fieldIndex }, { DataItemName.FieldValue, fieldValue } });
                }
            }
        }

        /// <summary>
        /// Gets the characters a field may hold, from the writer's encoding, which never changes, so they are found once.
        /// </summary>
        private CsvTextData GetTextData()
        {
            var textData = _textData;
            if (textData is null)
            {
                textData = CsvTextData.ForEncoding(_writer.Encoding);
                _textData = textData;
            }

            return textData;
        }

        /// <summary>
        /// Checks that the characters the options make the writer write are in its character set.
        /// </summary>
        /// <remarks>
        /// The field delimiter and the characters of the terminator line break are written as they are, so each has to be a character of the
        /// character set, or the encoding would write a replacement, such as '?', and the fields couldn't be read back.  The embedded line break
        /// is written inside a field value, so, other than CR and LF, its characters have to be printable characters of the character set, as
        /// RFC 4180§2 (TEXTDATA) requires of any character in a field.
        /// </remarks>
        /// <param name="message">The reason the options are invalid; empty when they are valid.</param>
        /// <returns><c>true</c> if the options are valid; <c>false</c> if they aren't.</returns>
        private bool TryValidateCharacterSetOptions(out string message)
        {
            var textData = GetTextData();
            var fieldDelimiter = _options.FieldDelimiterCharacter;
            if (!textData.IsInCharacterSet(fieldDelimiter))
            {
                message = string.Format(CultureInfo.InvariantCulture, "The field delimiter U+{0:X4} isn't a character of the writer's character set, {1}.",
                    (int)fieldDelimiter, textData.Name);
                return false;
            }

            foreach (var character in _options.TerminatorLineBreak ?? string.Empty)
            {
                if (!textData.IsInCharacterSet(character))
                {
                    message = string.Format(CultureInfo.InvariantCulture,
                        "The terminator line break holds U+{0:X4}, which isn't a character of the writer's character set, {1}.", (int)character, textData.Name);
                    return false;
                }
            }

            foreach (var character in _options.EmbeddedLineBreak ?? string.Empty)
            {
                if (character != '\r' && character != '\n' && !textData.IsAllowed(character))
                {
                    message = string.Format(CultureInfo.InvariantCulture,
                        "The embedded line break holds U+{0:X4}, which isn't a printable character of the writer's character set, {1}, so RFC 4180 doesn't allow it in a field.",
                        (int)character, textData.Name);
                    return false;
                }
            }

            message = string.Empty;
            return true;
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

            // The keys of the properties mapped to the fields output: the keys of the mappings that cover them.
            private readonly HashSet<string> _propertyKeys;

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal FieldPlan(int mapperVersion, Type? mapperLineType, ICsvFileMapper mapper, string[] headerLineFieldNames, string[] lineFieldNames,
                string[] unmappedCandidateFieldNames)
#else
            internal FieldPlan(int mapperVersion, [AllowNull] Type mapperLineType, [DisallowNull] ICsvFileMapper mapper, [DisallowNull] string[] headerLineFieldNames,
                [DisallowNull] string[] lineFieldNames, [DisallowNull] string[] unmappedCandidateFieldNames)
#endif
            {
                MapperVersion = mapperVersion;
                MapperLineType = mapperLineType;
                HeaderLineFieldNames = headerLineFieldNames;
                LineFieldNames = lineFieldNames;
                UnmappedCandidateFieldNames = unmappedCandidateFieldNames;
                UnmappedLineFieldNames = unmappedCandidateFieldNames;

                // A field mapped by index is output at its index.  The other fields fill the positions left: in header line order if there is a
                // header line, else the fields mapped by name in mapper order, followed by the fields of the first record line that no mapping
                // covers, in the order they're on that line, so their values aren't lost.  A position still free below the highest index is a
                // blank field.  Without a header line, the positions are those the reader reads the fields from.
                var isMapperOutput = headerLineFieldNames.Length == 0 && mapper.Count > 0;
                var fieldNames = new List<string>(headerLineFieldNames.Length > 0 ? headerLineFieldNames : isMapperOutput ? NoFieldNames : lineFieldNames);
                var fieldMappings = new Dictionary<int, List<FieldMappingWritePlan>>();
                if (isMapperOutput)
                {
                    foreach (var position in CsvMappingService.GetFieldNamesByPosition(mapper))
                    {
                        SetFieldName(fieldNames, position.Key, position.Value);
                    }

                    foreach (var position in CsvMappingService.GetFieldPositions(mapper))
                    {
                        AddFieldMapping(fieldNames, fieldMappings, position.Item2, new KeyValuePair<string, CsvFieldMapping>(position.Item1, mapper[position.Item1]));
                    }
                }
                else
                {
                    // A field mapped by name is the first field with its name.  One that no field has isn't output.
                    var positionsByFieldName = new Dictionary<string, int>(StringComparer.Ordinal);
                    for (var position = fieldNames.Count - 1; position >= 0; position--)
                    {
                        if (fieldNames[position].Length > 0)
                        {
                            positionsByFieldName[fieldNames[position]] = position;
                        }
                    }

                    foreach (var mapping in mapper)
                    {
                        if (mapping.Value.FieldIndex is int fieldIndex)
                        {
                            AddFieldMapping(fieldNames, fieldMappings, fieldIndex, mapping);
                        }
                        else if (positionsByFieldName.TryGetValue(CsvMappingService.GetFieldNameFromMapping(mapping), out var position))
                        {
                            AddFieldMapping(fieldNames, fieldMappings, position, mapping);
                        }
                    }
                }

                // A mapping covers a field of a record line if its field is output.  A mapping that supplies a constant or generated value also
                // covers the field the reader puts that value in, though it isn't output, since writing the line without it loses nothing.
                _fieldKeys = new HashSet<string>(StringComparer.Ordinal);
                var coveringMappingKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var fieldName in fieldNames)
                {
                    if (fieldName.Length > 0)
                    {
                        _fieldKeys.Add(fieldName);
                    }
                }

                foreach (var mappings in fieldMappings.Values)
                {
                    foreach (var fieldMapping in mappings)
                    {
                        _fieldKeys.Add(fieldMapping.FieldName);
                        coveringMappingKeys.Add(fieldMapping.Mapping.Key);
                    }
                }

                foreach (var mapping in mapper)
                {
                    if (mapping.Value.SuppliesValue && coveringMappingKeys.Add(mapping.Key))
                    {
                        _fieldKeys.Add(CsvMappingService.GetFieldNameFromMapping(mapping));
                    }
                }

                _propertyKeys = coveringMappingKeys;

                if (isMapperOutput)
                {
                    var unmappedLineFieldNames = new List<string>();
                    var position = 0;
                    foreach (var lineFieldName in unmappedCandidateFieldNames)
                    {
                        if (!_propertyKeys.Contains(lineFieldName) && _fieldKeys.Add(lineFieldName))
                        {
                            unmappedLineFieldNames.Add(lineFieldName);
                            while (position < fieldNames.Count && (fieldNames[position].Length > 0 || fieldMappings.ContainsKey(position)))
                            {
                                position++;
                            }

                            SetFieldName(fieldNames, position++, lineFieldName);
                        }
                    }

                    UnmappedLineFieldNames = unmappedLineFieldNames.ToArray();
                }

                var fields = new FieldWritePlan[fieldNames.Count];
                for (var fieldIndex = 0; fieldIndex < fields.Length; fieldIndex++)
                {
                    fields[fieldIndex] = new FieldWritePlan(fieldNames[fieldIndex],
                        fieldMappings.TryGetValue(fieldIndex, out var mappings) ? mappings.ToArray() : NoFieldMappings);
                }

                Fields = fields;
            }

            private static readonly FieldMappingWritePlan[] NoFieldMappings = new FieldMappingWritePlan[0];

            /// <summary>
            /// Puts a field name at a position, adding blank fields up to that position.
            /// </summary>
            private static void SetFieldName(List<string> fieldNames, int position, string fieldName)
            {
                while (fieldNames.Count <= position)
                {
                    fieldNames.Add(string.Empty);
                }

                fieldNames[position] = fieldName;
            }

            /// <summary>
            /// Adds a mapping to the field at a position, adding blank fields up to that position.  Mappings that share a field keep mapper order.
            /// </summary>
            private static void AddFieldMapping(List<string> fieldNames, Dictionary<int, List<FieldMappingWritePlan>> fieldMappings, int position,
                KeyValuePair<string, CsvFieldMapping> mapping)
            {
                while (fieldNames.Count <= position)
                {
                    fieldNames.Add(string.Empty);
                }

                if (!fieldMappings.TryGetValue(position, out var mappings))
                {
                    mappings = new List<FieldMappingWritePlan>();
                    fieldMappings[position] = mappings;
                }

                mappings.Add(new FieldMappingWritePlan(mapping));
            }


            /// <summary>
            /// Gets the fields to output, in order.
            /// </summary>
            internal FieldWritePlan[] Fields { get; }

            internal string[] HeaderLineFieldNames { get; }

            /// <summary>
            /// Gets the record line field names that may be output, in the positions the mapped fields leave, when there is no header line.
            /// </summary>
            internal string[] UnmappedCandidateFieldNames { get; }

            /// <summary>
            /// Gets the field names of <see cref="UnmappedCandidateFieldNames"/> that no mapping covers, which are output in the positions the mapped fields leave
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

                    if (_propertyKeys.Contains(lineFieldName))
                    {
                        continue;
                    }

                    fieldName = lineFieldName;
                    return true;
                }

                fieldName = string.Empty;
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
        /// A field to output, with the mappings that share it, if any.
        /// </summary>
        private sealed class FieldWritePlan
        {
            internal FieldWritePlan(string fieldName, FieldMappingWritePlan[] mappings)
            {
                FieldName = fieldName;
                Mappings = mappings;
            }

            /// <summary>
            /// Gets the name of the field, which is also the key of its value on a record line, unless the line was built from a model whose
            /// property is mapped to a field with another name.  It is empty for a blank field that pads a gap before a field mapped by index.
            /// </summary>
            internal string FieldName { get; }

            /// <summary>
            /// Gets the mappings of the field, in mapper order; empty if no mapping covers it.
            /// </summary>
            internal FieldMappingWritePlan[] Mappings { get; }
        }

        /// <summary>
        /// A mapping of a field to output, and how to serialize its value.
        /// </summary>
        private sealed class FieldMappingWritePlan
        {
            internal FieldMappingWritePlan(KeyValuePair<string, CsvFieldMapping> mapping)
            {
                Mapping = mapping;
                FieldName = CsvMappingService.GetFieldNameFromMapping(mapping);
                Converter = mapping.Value.ToStringConverter as CsvFieldConvertToString<TRecordLine>;
                IsSupplied = mapping.Value.SuppliesValue;
                if (mapping.Value.VariableValueGenerator != null)
                {
                    Accessor = CsvFieldValueAccessor<TRecordLine>.For(mapping.Value.PropertyType);
                }
            }

            /// <summary>
            /// Gets the accessor that generates values of the property type, if the mapping has a value generator.
            /// </summary>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
            internal CsvFieldValueAccessor<TRecordLine>? Accessor { get; }
#else
            [MaybeNull] internal CsvFieldValueAccessor<TRecordLine> Accessor { get; }
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
            /// Gets the name of the field the mapping names: its field name, or else its property key.
            /// </summary>
            internal string FieldName { get; }

            /// <summary>
            /// Gets a flag indicating if the mapping supplies its value, as a constant or generated value, rather than taking it from the line.
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