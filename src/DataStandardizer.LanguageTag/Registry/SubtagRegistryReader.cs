using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD
using System.Diagnostics.CodeAnalysis;
#endif
using static DataStandardizer.LanguageTag.Registry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// Reads the records of the Subtag Registry, in the record-jar format of RFC 5646 §3.1.1.
    /// </summary>
    /// <remarks>
    /// Input that the format doesn't allow throws a <see cref="FormatException"/> that names its line: a line that is blank, holds a control character
    /// (a tab among them), or is neither a field nor a folded line that starts with a space; a field body that ends with a space; and a record that is
    /// empty, has none or more than one of the File-Date, Subtag and Tag fields, or has no Type that RFC 5646 §3.1.3 defines for its kind of record.
    /// </remarks>
    internal class SubtagRegistryReader
    {
        private const string RecordSeparator = "%%";

        private readonly TextReader _sourceReader;

        private readonly List<KeyValuePair<string, string>> _fields = new List<KeyValuePair<string, string>>();

        private int _lineNumber;
        private int _recordLineNumber;
        private int _fieldLineNumber;
        private bool _fieldEndsWithSpace;
        private bool _isAtEnd;

#if NETCOREAPP3_0_OR_GREATER
        private string? _fieldName;
        private string? _fieldBody;
        private StringBuilder? _foldedFieldBody;
#else
        private string _fieldName;
        private string _fieldBody;
        private StringBuilder _foldedFieldBody;
#endif

        internal SubtagRegistryReader(TextReader sourceReader)
        {
            _sourceReader = sourceReader;
        }

        /// <summary>
        /// Reads the next record.
        /// </summary>
        /// <returns>The record, or <see langword="null"/> at the end of the input.</returns>
        /// <exception cref="FormatException">The input is not in the format of RFC 5646 §3.1.1 and §3.1.2.</exception>
#if NETCOREAPP3_0_OR_GREATER
        internal ISubtagRegistryRecord? ReadRecord()
#else
        [return: MaybeNull]
        internal ISubtagRegistryRecord ReadRecord()
#endif
        {
            if (_isAtEnd) return null;

            while (true)
            {
                var line = _sourceReader.ReadLine();
                if (AddLine(line)) return CreateRecord();
            }
        }

        /// <inheritdoc cref="ReadRecord"/>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="OperationCanceledException">The cancellation token was cancelled.</exception>
#if NETCOREAPP3_0_OR_GREATER
        internal async Task<ISubtagRegistryRecord?> ReadRecordAsync(CancellationToken cancellationToken)
#else
        internal async Task<ISubtagRegistryRecord> ReadRecordAsync(CancellationToken cancellationToken)
#endif
        {
            if (_isAtEnd) return null;

            while (true)
            {
                // checked between lines on every target, as a line already in the reader's buffer is returned without observing the token
                cancellationToken.ThrowIfCancellationRequested();
#if NET7_0_OR_GREATER
                var line = await _sourceReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
                var line = await _sourceReader.ReadLineAsync().ConfigureAwait(false);
#endif
                if (AddLine(line)) return CreateRecord();
            }
        }

        /// <summary>
        /// Adds a line to the record being read.
        /// </summary>
        /// <param name="line">The line, or <see langword="null"/> at the end of the input.</param>
        /// <returns><see langword="true"/> when the line ends the record.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private bool AddLine(string? line)
#else
        private bool AddLine([AllowNull] string line)
#endif
        {
            if (line is null || line == RecordSeparator)
            {
                // registry = record *("%%" CRLF record) and record = 1*field, so the input, and each record after a "%%" line, has at least one field
                if (_fieldName is null && line != null) throw CreateException("The record that ends at line {0} has no fields (ref. RFC 5646 §3.1.1).", _lineNumber + 1);
                if (_fieldName is null && _lineNumber == 0) throw CreateException("The registry has no records (ref. RFC 5646 §3.1.1).");
                if (_fieldName is null) throw CreateException("The registry ends after the \"%%\" at line {0}, with no record after it (ref. RFC 5646 §3.1.1).", _lineNumber);

                AddField();
                _isAtEnd = line is null;
                _lineNumber++;
                return true;
            }

            _lineNumber++;
            if (line.Length == 0) throw CreateException("Line {0} is blank, which a record may not hold (ref. RFC 5646 §3.1.1).", _lineNumber);

            for (var index = 0; index < line.Length; index++)
            {
                // CHARS is %x21-10FFFF, so the space is the only character below it that a field may hold (ref. RFC 5646 §3.1.1)
                if (line[index] < '!' && line[index] != ' ')
                {
                    throw CreateException("Line {0} holds the control character U+{1:X4}, which a field may not hold (ref. RFC 5646 §3.1.1).", _lineNumber, (int)line[index]);
                }
            }

            if (line[0] == ' ')
            {
                // a line starting with a space continues the field body on the line before (ref. RFC 5646 §3.1.1)
                if (_fieldName is null) throw CreateException("Line {0} is a folded line, but no field comes before it in its record (ref. RFC 5646 §3.1.1).", _lineNumber);

                AppendToFieldBody(line);
                return false;
            }

            var colonIndex = line.IndexOf(':');
            var nameLength = colonIndex;
            while (nameLength > 0 && line[nameLength - 1] == ' ') nameLength--;

            if (nameLength <= 0 || !IsFieldName(line, nameLength))
            {
                throw CreateException("Line {0} is neither a field, which starts with a field name and a colon, nor a folded line, which starts with a space (ref. RFC 5646 §3.1.1).", _lineNumber);
            }

            AddField();
            if (_recordLineNumber == 0) _recordLineNumber = _lineNumber;
            _fieldName = GetFieldName(line, nameLength);
            _fieldBody = Trim(line, colonIndex + 1);
            _fieldLineNumber = _lineNumber;
            _fieldEndsWithSpace = _fieldBody.Length > 0 && line[line.Length - 1] == ' ';

            return false;
        }

        private void AppendToFieldBody(string line)
        {
            var start = 0;
            var end = line.Length;
            while (start < end && line[start] == ' ') start++;
            if (start == end) throw CreateException("Line {0} is a folded line with nothing but spaces (ref. RFC 5646 §3.1.1).", _lineNumber);

            while (line[end - 1] == ' ') end--;
            _fieldLineNumber = _lineNumber;
            _fieldEndsWithSpace = end < line.Length;

            // folded lines are unfolded with one space between them, so most fields, which are not folded, need no builder
            if (_foldedFieldBody is null) _foldedFieldBody = new StringBuilder();
            if (_foldedFieldBody.Length == 0) _foldedFieldBody.Append(_fieldBody);
            if (_foldedFieldBody.Length > 0) _foldedFieldBody.Append(' ');
            _foldedFieldBody.Append(line, start, end - start);
        }

        private void AddField()
        {
            if (_fieldName is null) return;

            // the field body ends with CHARS, so a space before the line break is allowed only where a folded line follows (ref. RFC 5646 §3.1.1)
            if (_fieldEndsWithSpace) throw CreateException("The {1} field ends with a space at line {0} (ref. RFC 5646 §3.1.1).", _fieldLineNumber, _fieldName);

            var fieldBody = _foldedFieldBody?.Length > 0 ? _foldedFieldBody.ToString() : _fieldBody;
            if (!string.IsNullOrEmpty(fieldBody)) _fields.Add(new KeyValuePair<string, string>(_fieldName, fieldBody));

            _fieldName = null;
            _fieldBody = null;
            _foldedFieldBody?.Clear();
        }

        /// <summary>
        /// Creates the record from the fields read, which are at least one (ref. RFC 5646 §3.1.1).
        /// </summary>
        private ISubtagRegistryRecord CreateRecord()
        {
            var isFileDate = false;
            var hasSubtag = false;
            var hasTag = false;
#if NETCOREAPP3_0_OR_GREATER
            string? type = null;
#else
            string type = null;
#endif
            foreach (var field in _fields)
            {
                isFileDate |= field.Key == FieldName.FileDate;
                hasSubtag |= field.Key == FieldName.Subtag;
                hasTag |= field.Key == FieldName.Tag;
                if (type is null && field.Key == FieldName.Type) type = field.Value;
            }

            var recordLineNumber = _recordLineNumber;
            var fields = _fields.ToArray();
            _recordLineNumber = 0;
            _fields.Clear();

            // the three kinds of record are told apart by these fields (ref. RFC 5646 §3.1.2)
            var kindCount = (isFileDate ? 1 : 0) + (hasSubtag ? 1 : 0) + (hasTag ? 1 : 0);
            if (kindCount == 0) throw CreateException("The record at line {0} has no File-Date, Subtag or Tag field (ref. RFC 5646 §3.1.2).", recordLineNumber);
            if (kindCount > 1) throw CreateException("The record at line {0} has more than one of the File-Date, Subtag and Tag fields (ref. RFC 5646 §3.1.2).", recordLineNumber);

            if (isFileDate) return new SubtagRegistryFileDateRecord(fields);

            if (type is null) throw CreateException("The record at line {0} has no Type field (ref. RFC 5646 §3.1.2).", recordLineNumber);

            var record = hasSubtag ? new SubtagRegistrySubtagRecord(fields) : (SubtagRegistryTagRecordBase)new SubtagRegistryTagRecord(fields);
            if (record.Type == SubtagRegistryRecordType.Unknown)
            {
                throw CreateException("The record at line {0} has the Type '{1}', which is not one that RFC 5646 §3.1.3 defines.", recordLineNumber, type);
            }

            // grandfathered and redundant records have a Tag field, and records of every other type a Subtag field (ref. RFC 5646 §3.1.2)
            var isTagType = record.Type == SubtagRegistryRecordType.Grandfathered || record.Type == SubtagRegistryRecordType.Redundant;
            if (hasSubtag && isTagType)
            {
                throw CreateException("The record at line {0} has a Subtag field, but its Type '{1}' is one whose records have a Tag field (ref. RFC 5646 §3.1.2).", recordLineNumber, type);
            }

            if (hasTag && !isTagType)
            {
                throw CreateException("The record at line {0} has a Tag field, but its Type '{1}' is one whose records have a Subtag field (ref. RFC 5646 §3.1.2).", recordLineNumber, type);
            }

            return record;
        }

        private static FormatException CreateException(string format, params object[] args)
        {
            return new FormatException(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary>
        /// Checks the start of a line against the field-name rule of RFC 5646 §3.1.1: letters, digits and hyphens, starting and ending with a letter or digit.
        /// </summary>
        private static bool IsFieldName(string line, int length)
        {
            if (line[0] == '-' || line[length - 1] == '-') return false;

            for (var index = 0; index < length; index++)
            {
                var character = line[index];
                if (!(character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9' || character == '-')) return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the name of a field, as the constant in <see cref="FieldName"/> for each name RFC 5646 defines, so that reading one allocates no string.
        /// </summary>
        private static string GetFieldName(string line, int length)
        {
            switch (length)
            {
                case 3:
                    if (StartsWith(line, FieldName.Tag)) return FieldName.Tag;
                    break;
                case 4:
                    if (StartsWith(line, FieldName.Type)) return FieldName.Type;
                    break;
                case 5:
                    if (StartsWith(line, FieldName.Added)) return FieldName.Added;
                    if (StartsWith(line, FieldName.Scope)) return FieldName.Scope;
                    break;
                case 6:
                    if (StartsWith(line, FieldName.Subtag)) return FieldName.Subtag;
                    if (StartsWith(line, FieldName.Prefix)) return FieldName.Prefix;
                    break;
                case 8:
                    if (StartsWith(line, FieldName.Comments)) return FieldName.Comments;
                    break;
                case 9:
                    if (StartsWith(line, FieldName.FileDate)) return FieldName.FileDate;
                    break;
                case 10:
                    if (StartsWith(line, FieldName.Deprecated)) return FieldName.Deprecated;
                    break;
                case 11:
                    if (StartsWith(line, FieldName.Description)) return FieldName.Description;
                    break;
                case 13:
                    if (StartsWith(line, FieldName.Macrolanguage)) return FieldName.Macrolanguage;
                    break;
                case 15:
                    if (StartsWith(line, FieldName.PreferredValue)) return FieldName.PreferredValue;
                    if (StartsWith(line, FieldName.SuppressScript)) return FieldName.SuppressScript;
                    break;
            }

            // a field this version doesn't know, which is kept as a field rather than run into the field before it
            return line.Substring(0, length);
        }

        private static bool StartsWith(string line, string fieldName)
        {
            return string.CompareOrdinal(line, 0, fieldName, 0, fieldName.Length) == 0;
        }

        private static string Trim(string line, int start)
        {
            var end = line.Length;
            while (start < end && line[start] == ' ') start++;
            while (end > start && line[end - 1] == ' ') end--;

            return start == end ? string.Empty : line.Substring(start, end - start);
        }
    }
}
