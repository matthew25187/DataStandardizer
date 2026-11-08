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
    /// (a tab among them), or is neither a field nor a folded line that starts with a space; a field body that ends with a space; a File-Date, Subtag,
    /// Tag or Type field with no value; a File-Date, Added or Deprecated field that is not an RFC 3339 full-date; and a record that is empty, has none
    /// or more than one of the File-Date, Subtag and Tag fields, or has no Type that RFC 5646 §3.1.3 defines for its kind of record. It also throws for
    /// a record that breaks the rules of RFC 5646 §3.1.2: a first record that is not the File-Date record, a second File-Date record, a File-Date record
    /// with another field, a tag or subtag record with no Description or Added field, and a field other than Description, Comments and Prefix that
    /// appears twice in a record. Fields that RFC 5646 doesn't define are kept, and the rules for tag and subtag records ignore them, as §3.1.2 asks;
    /// the File-Date record has the File-Date field alone.
    /// <para>
    /// A full-date in the year 0000, which RFC 3339 allows, also throws, as <see cref="DateTime"/> starts at the year 0001.
    /// </para>
    /// </remarks>
    internal class SubtagRegistryReader
    {
        private const string RecordSeparator = "%%";

        private readonly TextReader _sourceReader;

        private readonly List<KeyValuePair<string, string>> _fields = new List<KeyValuePair<string, string>>();

        // the fields of the record being read, with or without a value, for the rules of §3.1.2: how many there are, and a flag for each field seen
        // that may appear only once
        private int _recordFieldCount;
        private int _singleFieldFlags;

        private int _recordCount;

        private int _lineNumber;
        private int _recordLineNumber;
        // the first line of the field being read, which errors about its whole value name, and its last line so far, which an error about how
        // that line ends names
        private int _fieldStartLineNumber;
        private int _fieldLineNumber;
        private bool _fieldEndsWithSpace;
        private bool _isAtEnd;

        // the File-Date, Added and Deprecated of the record being read, each of which appears at most once, parsed once for the record to take
        private DateTime? _fileDate;
        private DateTime? _added;
        private DateTime? _deprecated;

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
            while (true)
            {
                // checked between lines on every target, as a line already in the reader's buffer is returned without observing the token, and before
                // the end of the input is reported, so a cancelled token never yields a registry
                cancellationToken.ThrowIfCancellationRequested();
                if (_isAtEnd) return null;
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

            // CHARS is %x21-10FFFF, so the space is the only character below it that a field may hold (ref. RFC 5646 §3.1.1)
            var controlIndex = IndexOfControlCharacter(line);
            if (controlIndex >= 0)
            {
                throw CreateException("Line {0} holds the control character U+{1:X4}, which a field may not hold (ref. RFC 5646 §3.1.1).", _lineNumber, (int)line[controlIndex]);
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

            if (nameLength <= 0 || !IsFieldName(line, nameLength)) throw CreateFieldNameException(line, colonIndex, nameLength);

            AddField();
            if (_recordLineNumber == 0) _recordLineNumber = _lineNumber;
            _fieldName = GetFieldName(line, nameLength);
            _fieldBody = Trim(line, colonIndex + 1);
            _fieldLineNumber = _lineNumber;
            _fieldStartLineNumber = _lineNumber;
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

        /// <summary>
        /// Parses the body of a File-Date, Added or Deprecated field, each an RFC 3339 full-date (ref. RFC 5646 §3.1.1).
        /// </summary>
        private DateTime ParseDateField(string fieldBody)
        {
            if (!TryParseFullDate(fieldBody, out var year, out var month, out var day))
            {
                throw CreateException("The {1} field at line {0} has the value '{2}', which is not an RFC 3339 full-date (ref. RFC 5646 §3.1.1).",
                    _fieldStartLineNumber, _fieldName, fieldBody);
            }

            if (year == 0)
            {
                throw CreateException(
                    "The {1} field at line {0} has the value '{2}', a full-date in the year 0000, which is before the year 0001 that a DateTime starts at.",
                    _fieldStartLineNumber, _fieldName, fieldBody);
            }

            return new DateTime(year, month, day);
        }

        private void AddField()
        {
            if (_fieldName is null) return;

            // the field body ends with CHARS, so a space before the line break is allowed only where a folded line follows (ref. RFC 5646 §3.1.1)
            if (_fieldEndsWithSpace) throw CreateException("The {1} field ends with a space at line {0} (ref. RFC 5646 §3.1.1).", _fieldLineNumber, _fieldName);

            // field-names other than Description, Comments and Prefix occur at most once per record (ref. RFC 5646 §3.1.2); fields that RFC 5646 doesn't
            // define have no flag, as §3.1.2 asks that they be ignored
            _recordFieldCount++;
            var singleFieldFlag = GetSingleFieldFlag(_fieldName);
            if (singleFieldFlag != 0)
            {
                if ((_singleFieldFlags & singleFieldFlag) != 0)
                {
                    throw CreateException(
                        "The {1} field at line {0} is the second in its record, which may have only one (ref. RFC 5646 §3.1.2).", _fieldStartLineNumber, _fieldName);
                }

                _singleFieldFlags |= singleFieldFlag;
            }

            var fieldBody = _foldedFieldBody?.Length > 0 ? _foldedFieldBody.ToString() : _fieldBody;
            if (string.IsNullOrEmpty(fieldBody))
            {
                // the grammar allows an empty field body, but the fields that tell a record's kind and type, the fields every tag and subtag record
                // has, and the dates, each need a value (ref. RFC 5646 §3.1.2)
                if (IsValueRequired(_fieldName)) throw CreateException("The {1} field at line {0} has no value (ref. RFC 5646 §3.1.2).", _fieldStartLineNumber, _fieldName);
            }
            else
            {
                if (ReferenceEquals(_fieldName, FieldName.FileDate)) _fileDate = ParseDateField(fieldBody);
                else if (ReferenceEquals(_fieldName, FieldName.Added)) _added = ParseDateField(fieldBody);
                else if (ReferenceEquals(_fieldName, FieldName.Deprecated)) _deprecated = ParseDateField(fieldBody);

                _fields.Add(new KeyValuePair<string, string>(_fieldName, fieldBody));
            }

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
            var hasDescription = false;
            foreach (var field in _fields)
            {
                isFileDate |= field.Key == FieldName.FileDate;
                hasSubtag |= field.Key == FieldName.Subtag;
                hasTag |= field.Key == FieldName.Tag;
                hasDescription |= field.Key == FieldName.Description;
            }

            var recordLineNumber = _recordLineNumber;
            var fields = _fields.ToArray();
            var fileDate = _fileDate;
            var added = _added;
            var deprecated = _deprecated;
            var recordFieldCount = _recordFieldCount;
            var isFirstRecord = _recordCount == 0;
            _recordCount++;
            _recordLineNumber = 0;
            _fields.Clear();
            _recordFieldCount = 0;
            _singleFieldFlags = 0;
            _fileDate = null;
            _added = null;
            _deprecated = null;

            // the three kinds of record are told apart by these fields (ref. RFC 5646 §3.1.2)
            var kindCount = (isFileDate ? 1 : 0) + (hasSubtag ? 1 : 0) + (hasTag ? 1 : 0);
            if (kindCount == 0) throw CreateException("The record at line {0} has no File-Date, Subtag or Tag field (ref. RFC 5646 §3.1.2).", recordLineNumber);
            if (kindCount > 1) throw CreateException("The record at line {0} has more than one of the File-Date, Subtag and Tag fields (ref. RFC 5646 §3.1.2).", recordLineNumber);

            // the first record is always the File-Date record, which occurs only once and has a single field (ref. RFC 5646 §3.1.2)
            if (isFirstRecord && !isFileDate) throw CreateException("The first record, at line {0}, is not the File-Date record (ref. RFC 5646 §3.1.2).", recordLineNumber);
            if (!isFirstRecord && isFileDate)
            {
                throw CreateException(
                    "The record at line {0} is a second File-Date record, which occurs only once, as the first record (ref. RFC 5646 §3.1.2).", recordLineNumber);
            }

            // the File-Date record "contains a single field", so a field that RFC 5646 doesn't define is not ignored there (ref. RFC 5646 §3.1.2)
            if (isFileDate && recordFieldCount > 1)
            {
                throw CreateException("The File-Date record at line {0} has a field other than File-Date (ref. RFC 5646 §3.1.2).", recordLineNumber);
            }

            // a File-Date field has a value, which is a full-date, so the record has its date
            if (fileDate.HasValue) return new SubtagRegistryFileDateRecord(fields, fileDate.Value);

            // every tag and subtag record has a Description field and an Added field (ref. RFC 5646 §3.1.2)
            if (!hasDescription) throw CreateException("The record at line {0} has no Description field (ref. RFC 5646 §3.1.2).", recordLineNumber);
            if (!added.HasValue) throw CreateException("The record at line {0} has no Added field (ref. RFC 5646 §3.1.2).", recordLineNumber);

            var record = hasSubtag
                ? new SubtagRegistrySubtagRecord(fields, added.Value, deprecated)
                : (SubtagRegistryTagRecordBase)new SubtagRegistryTagRecord(fields, added.Value, deprecated);
            if (record.Type == SubtagRegistryRecordType.Unknown)
            {
                // the record reads the first Type field, as the error does
                var type = SubtagRegistryRecordBase.GetFirstValue(fields, FieldName.Type);
                if (type is null) throw CreateException("The record at line {0} has no Type field (ref. RFC 5646 §3.1.2).", recordLineNumber);

                throw CreateException("The record at line {0} has the Type '{1}', which is not one that RFC 5646 §3.1.3 defines.", recordLineNumber, type);
            }

            // grandfathered and redundant records have a Tag field, and records of every other type a Subtag field (ref. RFC 5646 §3.1.2)
            var isTagType = record.Type == SubtagRegistryRecordType.Grandfathered || record.Type == SubtagRegistryRecordType.Redundant;
            if (hasSubtag && isTagType)
            {
                throw CreateException("The record at line {0} has a Subtag field, but its Type '{1}' is one whose records have a Tag field (ref. RFC 5646 §3.1.2).", recordLineNumber, SubtagRegistryRecordBase.GetFirstValue(fields, FieldName.Type));
            }

            if (hasTag && !isTagType)
            {
                throw CreateException("The record at line {0} has a Tag field, but its Type '{1}' is one whose records have a Subtag field (ref. RFC 5646 §3.1.2).", recordLineNumber, SubtagRegistryRecordBase.GetFirstValue(fields, FieldName.Type));
            }

            return record;
        }

        /// <summary>
        /// Creates the exception for a line that doesn't start with a space and whose start is not a field name and a colon, naming what is wrong with it.
        /// </summary>
        private FormatException CreateFieldNameException(string line, int colonIndex, int nameLength)
        {
            // "%%" with spaces after it is not the record separator, which is a line of "%%" alone (ref. RFC 5646 §3.1.1)
            if (line.Length > RecordSeparator.Length && line.StartsWith(RecordSeparator, StringComparison.Ordinal) && line.TrimEnd(' ').Length == RecordSeparator.Length)
            {
                return CreateException("Line {0} has spaces after the \"%%\" that separates records, which is a line of \"%%\" alone (ref. RFC 5646 §3.1.1).", _lineNumber);
            }

            if (colonIndex < 0)
            {
                return CreateException("Line {0} has no colon, so it is neither a field nor a folded line, which starts with a space (ref. RFC 5646 §3.1.1).", _lineNumber);
            }

            if (nameLength <= 0) return CreateException("Line {0} has no field name before its colon (ref. RFC 5646 §3.1.1).", _lineNumber);

            for (var index = 0; index < nameLength; index++)
            {
                if (!IsFieldNameCharacter(line[index]))
                {
                    return CreateException(
                        "Line {0} has the character U+{1:X4} in its field name, which holds only letters, digits and hyphens (ref. RFC 5646 §3.1.1).", _lineNumber, (int)line[index]);
                }
            }

            return CreateException("Line {0} has a field name that starts or ends with a hyphen (ref. RFC 5646 §3.1.1).", _lineNumber);
        }

        /// <summary>
        /// Finds the first character below the space, which no line may hold (ref. RFC 5646 §3.1.1).
        /// </summary>
        private static int IndexOfControlCharacter(string line)
        {
#if NET
            return line.AsSpan().IndexOfAnyInRange('\0', '\x1F');
#else
            for (var index = 0; index < line.Length; index++)
            {
                if (line[index] < ' ') return index;
            }

            return -1;
#endif
        }

        /// <summary>
        /// Gets whether a field needs a value: the fields that tell a record's kind or type, the Description and Added fields every tag and subtag record
        /// has, and the dates (ref. RFC 5646 §3.1.2).
        /// </summary>
        private static bool IsValueRequired(string fieldName)
        {
            return ReferenceEquals(fieldName, FieldName.FileDate) || ReferenceEquals(fieldName, FieldName.Subtag) || ReferenceEquals(fieldName, FieldName.Tag) ||
                ReferenceEquals(fieldName, FieldName.Type) || ReferenceEquals(fieldName, FieldName.Description) || ReferenceEquals(fieldName, FieldName.Added) ||
                ReferenceEquals(fieldName, FieldName.Deprecated);
        }

        /// <summary>
        /// Gets the flag of a field that RFC 5646 §3.1.2 defines and that may appear only once per record, or 0 for any other field: Description, Comments
        /// and Prefix, which may repeat, and a field that RFC 5646 doesn't define.
        /// </summary>
        private static int GetSingleFieldFlag(string fieldName)
        {
            // GetFieldName returns each name that RFC 5646 defines as the constant in FieldName
            if (ReferenceEquals(fieldName, FieldName.FileDate)) return 1 << 0;
            if (ReferenceEquals(fieldName, FieldName.Type)) return 1 << 1;
            if (ReferenceEquals(fieldName, FieldName.Subtag)) return 1 << 2;
            if (ReferenceEquals(fieldName, FieldName.Tag)) return 1 << 3;
            if (ReferenceEquals(fieldName, FieldName.Added)) return 1 << 4;
            if (ReferenceEquals(fieldName, FieldName.Deprecated)) return 1 << 5;
            if (ReferenceEquals(fieldName, FieldName.PreferredValue)) return 1 << 6;
            if (ReferenceEquals(fieldName, FieldName.SuppressScript)) return 1 << 7;
            if (ReferenceEquals(fieldName, FieldName.Macrolanguage)) return 1 << 8;
            if (ReferenceEquals(fieldName, FieldName.Scope)) return 1 << 9;

            return 0;
        }

#if NETCOREAPP3_0_OR_GREATER
        private static FormatException CreateException(string format, params object?[] args)
#else
        private static FormatException CreateException(string format, params object[] args)
#endif
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
                if (!IsFieldNameCharacter(line[index])) return false;
            }

            return true;
        }

        private static bool IsFieldNameCharacter(char character)
        {
            return character >= 'A' && character <= 'Z' || character >= 'a' && character <= 'z' || character >= '0' && character <= '9' || character == '-';
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

        /// <summary>
        /// Checks a value against the full-date rule of RFC 3339 §5.6: <c>date-fullyear "-" date-month "-" date-mday</c>, with a day that the month has
        /// in that year of the Gregorian calendar (RFC 3339 §5.7).
        /// </summary>
        private static bool TryParseFullDate(string value, out int year, out int month, out int day)
        {
            year = 0;
            month = 0;
            day = 0;
            if (value.Length != 10 || value[4] != '-' || value[7] != '-' || !TryParseDigits(value, 0, 4, out year)) return false;
            if (!TryParseDigits(value, 5, 2, out month) || !TryParseDigits(value, 8, 2, out day)) return false;

            return month >= 1 && month <= 12 && day >= 1 && day <= GetDaysInMonth(year, month);
        }

        /// <summary>
        /// Gets the number of days in a month of the Gregorian calendar, for any year from 0000 to 9999 (ref. RFC 3339 §5.7).
        /// </summary>
        private static int GetDaysInMonth(int year, int month)
        {
            switch (month)
            {
                case 2:
                    return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0) ? 29 : 28;
                case 4:
                case 6:
                case 9:
                case 11:
                    return 30;
                default:
                    return 31;
            }
        }

        private static bool TryParseDigits(string value, int start, int length, out int number)
        {
            number = 0;
            for (var index = start; index < start + length; index++)
            {
                var digit = value[index] - '0';
                if (digit < 0 || digit > 9) return false;

                number = number * 10 + digit;
            }

            return true;
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
