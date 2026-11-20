using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
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
    /// appears twice in a record. It throws as well for a field on a record type that may not have it: a Prefix field outside an extlang or variant
    /// record, or an extlang record without exactly one (§3.1.2, §3.1.8), and a Suppress-Script, Macrolanguage or Scope field outside a language or
    /// extlang record (§3.1.2); for a Scope other than the four values §3.1.2 permits; for a Prefix that is not a well-formed language tag repeating
    /// no variant subtag or extension singleton (§2.2.9, §3.1.8), a Suppress-Script that is not a script subtag, a Macrolanguage that is not a primary
    /// language subtag, and a Preferred-Value not in the form §3.1.2 gives it for the record's Type, each in any case (§2.1.1), and for any of these
    /// fields with no value; for an extlang record whose Preferred-Value is missing or not identical to its Subtag (§2.2.2); for a Subtag that is not a subtag of its record's Type in the
    /// form of §2.2.1 to §2.2.5 and the case of §3.1.4, at both ends of a range; for a Tag that is not formatted as §2.1.1 describes (§3.1.4); and for
    /// a character outside printable US-ASCII in a field that RFC 5646 defines, other than Description and Comments (§3.1.1). Fields that RFC 5646
    /// doesn't define are kept, and the rules for tag and subtag records ignore them, as §3.1.2 asks; the File-Date record has the File-Date field
    /// alone.
    /// <para>
    /// Every line, the last one included, ends with CRLF (ref. RFC 5646 §3.1.1). A line that ends with a line feed alone, and a last line with no line
    /// break, also throw, unless <see cref="SubtagRegistryLoadOptions.AcceptBareLineFeed"/> or
    /// <see cref="SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak"/> accepts it. A carriage return that no line feed follows is never a line break,
    /// so it is a control character in its line.
    /// </para>
    /// <para>
    /// A full-date in the year 0000, which RFC 3339 allows, also throws, as <see cref="DateTime"/> starts at the year 0001.
    /// </para>
    /// </remarks>
    internal class SubtagRegistryReader
    {
        private const string RecordSeparator = "%%";

        // the flags of the fields that appear only in records of the Type 'language' or 'extlang' (ref. RFC 5646 §3.1.2), among those of the fields
        // that may appear only once
        private const int SuppressScriptFlag = 1 << 7;
        private const int MacrolanguageFlag = 1 << 8;
        private const int ScopeFlag = 1 << 9;

        /// <summary>
        /// The number of characters read from the source at a time, which is the size of the buffer of the <see cref="StreamReader"/> that
        /// <see cref="SubtagRegistry"/> reads a stream or a file with, so that one read takes all it has decoded.
        /// </summary>
        private const int BufferSize = 16384;

        private readonly TextReader _sourceReader;
        private readonly bool _acceptBareLineFeed;
        private readonly bool _acceptMissingFinalLineBreak;

        // the characters read from the source and not yet split into lines, which this reader splits itself, as TextReader.ReadLine hides which line
        // break ends a line
        private readonly char[] _buffer = new char[BufferSize];
        private int _bufferPosition;
        private int _bufferLength;
        private bool _isAtEndOfInput;

        private readonly List<KeyValuePair<string, string>> _fields = new List<KeyValuePair<string, string>>();

        // the fields of the record being read, with or without a value, for the rules of §3.1.2: how many there are, and a flag for each field seen
        // that may appear only once
        private int _recordFieldCount;
        private int _singleFieldFlags;
        // the Prefix fields of the record being read, each with a value, which only some types of record may have (ref. RFC 5646 §3.1.2, §3.1.8)
        private int _prefixFieldCount;

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
        // the start of a line that the buffer ended in the middle of
        private StringBuilder? _partialLine;
#else
        private string _fieldName;
        private string _fieldBody;
        private StringBuilder _foldedFieldBody;
        private StringBuilder _partialLine;
#endif

        internal SubtagRegistryReader(TextReader sourceReader, SubtagRegistryLoadOptions options)
        {
            _sourceReader = sourceReader;
            _acceptBareLineFeed = (options & SubtagRegistryLoadOptions.AcceptBareLineFeed) != 0;
            _acceptMissingFinalLineBreak = (options & SubtagRegistryLoadOptions.AcceptMissingFinalLineBreak) != 0;
        }

        /// <summary>
        /// The line break that ends a line.
        /// </summary>
        private enum LineBreak
        {
            /// <summary>
            /// The line is the last, and the input ends after it.
            /// </summary>
            None = 0,

            CarriageReturnLineFeed,
            LineFeed
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
#if NETCOREAPP3_0_OR_GREATER
                string? line;
#else
                string line;
#endif
                LineBreak lineBreak;
                while (!TryTakeLine(out line, out lineBreak))
                {
                    if (_isAtEndOfInput)
                    {
                        line = TakeLastLine(out lineBreak);
                        break;
                    }

                    _bufferLength = _sourceReader.Read(_buffer, 0, _buffer.Length);
                    _bufferPosition = 0;
                    _isAtEndOfInput = _bufferLength == 0;
                }

                if (AddLine(line, lineBreak)) return CreateRecord();
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
#if NETCOREAPP3_0_OR_GREATER
                string? line;
#else
                string line;
#endif
                LineBreak lineBreak;
                while (!TryTakeLine(out line, out lineBreak))
                {
                    if (_isAtEndOfInput)
                    {
                        line = TakeLastLine(out lineBreak);
                        break;
                    }

#if NET
                    _bufferLength = await _sourceReader.ReadAsync(_buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
#else
                    _bufferLength = await _sourceReader.ReadAsync(_buffer, 0, _buffer.Length).ConfigureAwait(false);
#endif
                    _bufferPosition = 0;
                    _isAtEndOfInput = _bufferLength == 0;
                }

                if (AddLine(line, lineBreak)) return CreateRecord();
            }
        }

        /// <summary>
        /// Adds a line to the record being read.
        /// </summary>
        /// <param name="line">The line, or <see langword="null"/> at the end of the input.</param>
        /// <param name="lineBreak">The line break that ends the line.</param>
        /// <returns><see langword="true"/> when the line ends the record.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private bool AddLine(string? line, LineBreak lineBreak)
#else
        private bool AddLine([AllowNull] string line, LineBreak lineBreak)
#endif
        {
            if (line != null)
            {
                // a carriage return that no line feed follows is a control character, not a line break, whatever line breaks the options accept, so it is
                // reported before how the line ends (ref. RFC 5646 §3.1.1)
                if (line.IndexOf('\r') >= 0)
                {
                    throw CreateException(
                        "Line {0} holds a carriage return that no line feed follows, which is not a line break but a control character, which a field may not hold (ref. RFC 5646 §3.1.1).",
                        _lineNumber + 1);
                }

                CheckLineBreak(lineBreak, _lineNumber + 1);
            }

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

        /// <summary>
        /// Checks the line break that ends a line: CRLF, as every line ends, the last one included, or one that an option accepts (ref. RFC 5646 §3.1.1).
        /// </summary>
        private void CheckLineBreak(LineBreak lineBreak, int lineNumber)
        {
            if (lineBreak == LineBreak.LineFeed && !_acceptBareLineFeed)
            {
                throw CreateException("Line {0} ends with a line feed alone, not the CRLF that ends every line (ref. RFC 5646 §3.1.1).", lineNumber);
            }

            if (lineBreak == LineBreak.None && !_acceptMissingFinalLineBreak)
            {
                throw CreateException("Line {0}, the last, has no CRLF after it, which ends every line, the last one included (ref. RFC 5646 §3.1.1).", lineNumber);
            }
        }

        /// <summary>
        /// Takes the next line from the characters read, without its line break.
        /// </summary>
        /// <param name="line">The line, when this method returns <see langword="true"/>.</param>
        /// <param name="lineBreak">The line break that ends the line, or <see cref="LineBreak.None"/> when this method returns <see langword="false"/>.</param>
        /// <returns><see langword="false"/> when the characters read hold no line feed, so more must be read to end the line.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private bool TryTakeLine([NotNullWhen(true)] out string? line, out LineBreak lineBreak)
#else
        private bool TryTakeLine([MaybeNullWhen(false)] out string line, out LineBreak lineBreak)
#endif
        {
            lineBreak = LineBreak.None;
            var start = _bufferPosition;
#if NET
            var lineFeedIndex = _buffer.AsSpan(start, _bufferLength - start).IndexOf('\n');
            if (lineFeedIndex >= 0) lineFeedIndex += start;
#else
            var lineFeedIndex = Array.IndexOf(_buffer, '\n', start, _bufferLength - start);
#endif
            if (lineFeedIndex < 0)
            {
                // the line goes on past the characters read, so it is kept for the next read to end
                if (start < _bufferLength)
                {
                    if (_partialLine is null) _partialLine = new StringBuilder();
                    _partialLine.Append(_buffer, start, _bufferLength - start);
                }

                _bufferPosition = _bufferLength;
                line = null;
                return false;
            }

            _bufferPosition = lineFeedIndex + 1;
            if (_partialLine is null || _partialLine.Length == 0)
            {
                var isCrLf = lineFeedIndex > start && _buffer[lineFeedIndex - 1] == '\r';
                lineBreak = isCrLf ? LineBreak.CarriageReturnLineFeed : LineBreak.LineFeed;
                line = new string(_buffer, start, lineFeedIndex - start - (isCrLf ? 1 : 0));
                return true;
            }

            // a line that started in an earlier read, whose carriage return may have ended that read
            _partialLine.Append(_buffer, start, lineFeedIndex - start);
            var length = _partialLine.Length;
            var endsWithCr = _partialLine[length - 1] == '\r';
            lineBreak = endsWithCr ? LineBreak.CarriageReturnLineFeed : LineBreak.LineFeed;
            line = _partialLine.ToString(0, length - (endsWithCr ? 1 : 0));
            _partialLine.Clear();
            return true;
        }

        /// <summary>
        /// Takes the last line, which no line break ends, at the end of the input.
        /// </summary>
        /// <param name="lineBreak"><see cref="LineBreak.None"/>, as no line break ends the last line.</param>
        /// <returns>The line, or <see langword="null"/> when the input ends with a line break, so it has no more lines.</returns>
#if NETCOREAPP3_0_OR_GREATER
        private string? TakeLastLine(out LineBreak lineBreak)
#else
        [return: MaybeNull]
        private string TakeLastLine(out LineBreak lineBreak)
#endif
        {
            lineBreak = LineBreak.None;
            if (_partialLine is null || _partialLine.Length == 0) return null;

            var line = _partialLine.ToString();
            _partialLine.Clear();
            return line;
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

            var isPrefix = ReferenceEquals(_fieldName, FieldName.Prefix);
            if (isPrefix) _prefixFieldCount++;

            var fieldBody = _foldedFieldBody?.Length > 0 ? _foldedFieldBody.ToString() : _fieldBody;

            // the fields that RFC 5646 defines hold printable US-ASCII characters alone, except Description and Comments, which may hold any Unicode
            // character (ref. RFC 5646 §3.1.1, §3.1.5, §3.1.12); fields that it doesn't define are ignored, as §3.1.2 asks
            if ((singleFieldFlag != 0 || isPrefix) && fieldBody != null)
            {
                var nonAsciiIndex = IndexOfNonPrintableAscii(fieldBody);
                if (nonAsciiIndex >= 0)
                {
                    throw CreateException(
                        "The {1} field at line {0} holds the character U+{2:X4}, which is not one of the printable US-ASCII characters that the field is restricted to (ref. RFC 5646 §3.1.1).",
                        _fieldStartLineNumber, _fieldName, (int)fieldBody[nonAsciiIndex]);
                }
            }

            if (string.IsNullOrEmpty(fieldBody))
            {
                // the grammar allows an empty field body, but the fields that tell a record's kind and type, the fields every tag and subtag record
                // has, the dates, Scope, whose values are listed, Prefix, which holds a language tag, and Suppress-Script, Macrolanguage and
                // Preferred-Value, which hold a subtag or a language range, each need a value (ref. RFC 5646 §3.1.2, §3.1.8)
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
            var singleFieldFlags = _singleFieldFlags;
            var prefixFieldCount = _prefixFieldCount;
            var isFirstRecord = _recordCount == 0;
            _recordCount++;
            _recordLineNumber = 0;
            _fields.Clear();
            _recordFieldCount = 0;
            _singleFieldFlags = 0;
            _prefixFieldCount = 0;
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

            var typeName = SubtagRegistryRecordBase.GetFirstValue(fields, FieldName.Type) ?? string.Empty;
            CheckSubtagOrTag(record, fields, typeName, recordLineNumber);
            CheckFieldsOfType(record, typeName, recordLineNumber, singleFieldFlags, prefixFieldCount);

            return record;
        }

        /// <summary>
        /// Checks the Subtag or Tag field of a record against the form that RFC 5646 §3.1.4 gives it.
        /// </summary>
        private static void CheckSubtagOrTag(SubtagRegistryTagRecordBase record, KeyValuePair<string, string>[] fields, string typeName, int recordLineNumber)
        {
            if (record is SubtagRegistrySubtagRecord subtagRecord)
            {
                // a subtag has the form of its type (ref. RFC 5646 §2.2.1 to §2.2.5) in the case of its type (ref. RFC 5646 §3.1.4), and a range has it
                // at both ends (ref. RFC 5646 §3.1.1)
                if (!SubtagRegistryValueFormat.IsSubtag(record.Type, subtagRecord.Subtag))
                {
                    throw CreateException(
                        "The record at line {0} has the Subtag '{1}', which is not a subtag of the Type '{2}', or a range of them, in the form of RFC 5646 §2.2 and the case of §3.1.4.",
                        recordLineNumber, subtagRecord.Subtag, typeName);
                }

                return;
            }

            // a grandfathered or redundant record has a Tag field, as the record's kind was told by it
            var tag = SubtagRegistryRecordBase.GetFirstValue(fields, FieldName.Tag);
            if (tag != null && !SubtagRegistryValueFormat.IsTag(tag))
            {
                throw CreateException("The record at line {0} has the Tag '{1}', which is not formatted as RFC 5646 §2.1.1 describes (ref. RFC 5646 §3.1.4).", recordLineNumber, tag);
            }
        }

        /// <summary>
        /// Checks that a record has only the fields its type may have, as often as it may have them, and that its Scope has one of the values that
        /// RFC 5646 §3.1.2 permits.
        /// </summary>
        /// <param name="record">The record.</param>
        /// <param name="typeName">The body of the record's Type field.</param>
        /// <param name="recordLineNumber">The record's first line.</param>
        /// <param name="singleFieldFlags">The flags of the fields that may appear only once that the record has, with or without a value.</param>
        /// <param name="prefixFieldCount">The number of Prefix fields the record has.</param>
        private static void CheckFieldsOfType(SubtagRegistryTagRecordBase record, string typeName, int recordLineNumber, int singleFieldFlags, int prefixFieldCount)
        {
            var type = record.Type;

            // Prefix MAY appear in extlang and variant records, and MUST NOT appear in any other (ref. RFC 5646 §3.1.2), and appears exactly once in an
            // extlang record (ref. RFC 5646 §3.1.8)
            if (prefixFieldCount > 0 && type != SubtagRegistryRecordType.ExtendedLanguage && type != SubtagRegistryRecordType.Variant)
            {
                throw CreateException(
                    "The record at line {0} has a Prefix field, which only records of the Type 'extlang' or 'variant' may have, but its Type is '{1}' (ref. RFC 5646 §3.1.2).",
                    recordLineNumber, typeName);
            }

            if (type == SubtagRegistryRecordType.ExtendedLanguage && prefixFieldCount != 1)
            {
                throw CreateException(
                    "The record at line {0} has {1} Prefix fields, but a record of the Type 'extlang' has exactly one (ref. RFC 5646 §3.1.8).", recordLineNumber, prefixFieldCount);
            }

            // Suppress-Script, Macrolanguage and Scope appear only in language and extlang records (ref. RFC 5646 §3.1.2, §3.1.9, §3.1.10)
            if (type != SubtagRegistryRecordType.Language && type != SubtagRegistryRecordType.ExtendedLanguage)
            {
                CheckFieldAbsent(singleFieldFlags, SuppressScriptFlag, FieldName.SuppressScript, typeName, recordLineNumber);
                CheckFieldAbsent(singleFieldFlags, MacrolanguageFlag, FieldName.Macrolanguage, typeName, recordLineNumber);
                CheckFieldAbsent(singleFieldFlags, ScopeFlag, FieldName.Scope, typeName, recordLineNumber);
            }

            // Scope has a value, as an empty one fails when it is read, and the value is one of the four that RFC 5646 §3.1.2 permits
            var scope = record.Scope;
            if (scope != null && scope != ScopeValue.Macrolanguage && scope != ScopeValue.Collection && scope != ScopeValue.Special && scope != ScopeValue.PrivateUse)
            {
                throw CreateException(
                    "The record at line {0} has the Scope '{1}', which is not one of the values 'macrolanguage', 'collection', 'special' and 'private-use' that RFC 5646 §3.1.2 permits.",
                    recordLineNumber, scope);
            }

            CheckFieldForms(record, typeName, recordLineNumber);
        }

        /// <summary>
        /// Checks the Prefix, Suppress-Script, Macrolanguage and Preferred-Value fields of a record against the forms that RFC 5646 §3.1.2 gives them.
        /// </summary>
        private static void CheckFieldForms(SubtagRegistryTagRecordBase record, string typeName, int recordLineNumber)
        {
            // Prefix contains a valid language tag (ref. RFC 5646 §3.1.2, §3.1.8)
            foreach (var prefix in record.Prefix)
            {
                if (!SubtagRegistryValueFormat.IsPrefix(prefix))
                {
                    throw CreateException(
                        "The record at line {0} has the Prefix '{1}', which is not a well-formed language tag that repeats no variant subtag or extension singleton, as a valid language tag is (ref. RFC 5646 §2.2.9, §3.1.2, §3.1.8).",
                        recordLineNumber, prefix);
                }
            }

            // Suppress-Script contains a script subtag (ref. RFC 5646 §3.1.2, §3.1.9)
            var suppressScript = record.SuppressScript;
            if (suppressScript != null && !SubtagRegistryValueFormat.IsScriptSubtag(suppressScript))
            {
                throw CreateException(
                    "The record at line {0} has the Suppress-Script '{1}', which is not a script subtag of four letters (ref. RFC 5646 §2.2.3, §3.1.2, §3.1.9).",
                    recordLineNumber, suppressScript);
            }

            // Macrolanguage contains a primary language subtag (ref. RFC 5646 §3.1.2, §3.1.10)
            var macrolanguage = record.Macrolanguage;
            if (macrolanguage != null && !SubtagRegistryValueFormat.IsPrimaryLanguageSubtag(macrolanguage))
            {
                throw CreateException(
                    "The record at line {0} has the Macrolanguage '{1}', which is not a primary language subtag of two to eight letters (ref. RFC 5646 §2.2.1, §3.1.2, §3.1.10).",
                    recordLineNumber, macrolanguage);
            }

            // an extlang record has a Preferred-Value identical to its Subtag (ref. RFC 5646 §2.2.2), which is also the extended language range that
            // §3.1.2 gives it
            var preferredValue = record.PreferredValue;
            if (record is SubtagRegistrySubtagRecord subtagRecord && record.Type == SubtagRegistryRecordType.ExtendedLanguage)
            {
                if (preferredValue is null)
                {
                    throw CreateException(
                        "The record at line {0} has no Preferred-Value field, which a record of the Type 'extlang' has (ref. RFC 5646 §2.2.2).", recordLineNumber);
                }

                if (!string.Equals(preferredValue, subtagRecord.Subtag, StringComparison.Ordinal))
                {
                    throw CreateException(
                        "The record at line {0} has the Preferred-Value '{1}', which is not identical to its Subtag '{2}', as in a record of the Type 'extlang' (ref. RFC 5646 §2.2.2).",
                        recordLineNumber, preferredValue, subtagRecord.Subtag);
                }

                return;
            }

            // Preferred-Value takes a form that depends on the record's Type (ref. RFC 5646 §3.1.2, §3.1.7)
            if (preferredValue != null && !SubtagRegistryValueFormat.IsPreferredValue(record.Type, preferredValue))
            {
                throw CreateException(
                    "The record at line {0} has the Preferred-Value '{1}', which is not {2}, as a record of the Type '{3}' has (ref. RFC 5646 §3.1.2, §3.1.7).",
                    recordLineNumber, preferredValue, GetPreferredValueForm(record.Type), typeName);
            }
        }

        /// <summary>
        /// Describes the form of the Preferred-Value of a record of a type (ref. RFC 5646 §3.1.2).
        /// </summary>
        private static string GetPreferredValueForm(SubtagRegistryRecordType type)
        {
            switch (type)
            {
                case SubtagRegistryRecordType.Language:
                    return "a primary language subtag";
                case SubtagRegistryRecordType.Script:
                    return "a script subtag";
                case SubtagRegistryRecordType.Region:
                    return "a region subtag";
                case SubtagRegistryRecordType.Variant:
                    return "a variant subtag";
                default:
                    return "an extended language range of RFC 4647 §2.2";
            }
        }

        private static void CheckFieldAbsent(int singleFieldFlags, int fieldFlag, string fieldName, string typeName, int recordLineNumber)
        {
            if ((singleFieldFlags & fieldFlag) == 0) return;

            throw CreateException(
                "The record at line {0} has a {1} field, which only records of the Type 'language' or 'extlang' may have, but its Type is '{2}' (ref. RFC 5646 §3.1.2).",
                recordLineNumber, fieldName, typeName);
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
        /// has, the dates, Scope, whose values §3.1.2 lists, Prefix, which holds a language tag, and Suppress-Script, Macrolanguage and Preferred-Value,
        /// which each hold a subtag or a language range (ref. RFC 5646 §3.1.2, §3.1.8).
        /// </summary>
        private static bool IsValueRequired(string fieldName)
        {
            return ReferenceEquals(fieldName, FieldName.FileDate) || ReferenceEquals(fieldName, FieldName.Subtag) || ReferenceEquals(fieldName, FieldName.Tag) ||
                ReferenceEquals(fieldName, FieldName.Type) || ReferenceEquals(fieldName, FieldName.Description) || ReferenceEquals(fieldName, FieldName.Added) ||
                ReferenceEquals(fieldName, FieldName.Deprecated) || ReferenceEquals(fieldName, FieldName.Scope) || ReferenceEquals(fieldName, FieldName.Prefix) ||
                ReferenceEquals(fieldName, FieldName.SuppressScript) || ReferenceEquals(fieldName, FieldName.Macrolanguage) ||
                ReferenceEquals(fieldName, FieldName.PreferredValue);
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
            if (ReferenceEquals(fieldName, FieldName.SuppressScript)) return SuppressScriptFlag;
            if (ReferenceEquals(fieldName, FieldName.Macrolanguage)) return MacrolanguageFlag;
            if (ReferenceEquals(fieldName, FieldName.Scope)) return ScopeFlag;

            return 0;
        }

        /// <summary>
        /// Finds the first character that is not a printable US-ASCII character, the space among them, which most fields are restricted to
        /// (ref. RFC 5646 §3.1.1).
        /// </summary>
        private static int IndexOfNonPrintableAscii(string fieldBody)
        {
#if NET
            return fieldBody.AsSpan().IndexOfAnyExceptInRange(' ', '~');
#else
            for (var index = 0; index < fieldBody.Length; index++)
            {
                if (fieldBody[index] < ' ' || fieldBody[index] > '~') return index;
            }

            return -1;
#endif
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
