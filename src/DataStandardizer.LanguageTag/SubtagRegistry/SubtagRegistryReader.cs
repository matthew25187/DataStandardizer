using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    /// <summary>
    /// Reads the records of the Subtag Registry, in the record-jar format of RFC 5646 §3.1.1.
    /// </summary>
    internal class SubtagRegistryReader
    {
        private const string RecordSeparator = "%%";

        private readonly TextReader _sourceReader;

        private readonly List<Tuple<string, object>> _fields = new List<Tuple<string, object>>();

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
        /// Reads the next record, skipping any that is empty or has no File-Date, Subtag or Tag field.
        /// </summary>
        /// <returns>The record, or <see langword="null"/> at the end of the input.</returns>
#if NETCOREAPP3_0_OR_GREATER
        internal ISubtagRegistryRecord? ReadRecord()
#else
        [CanBeNull]
        internal ISubtagRegistryRecord ReadRecord()
#endif
        {
            while (true)
            {
                var line = _sourceReader.ReadLine();
                if (!AddLine(line)) continue;

                var record = CreateRecord();
                if (record != null || line is null) return record;
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
                // checked between lines on every target, as a line already in the reader's buffer is returned without observing the token
                cancellationToken.ThrowIfCancellationRequested();
#if NET7_0_OR_GREATER
                var line = await _sourceReader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
#else
                var line = await _sourceReader.ReadLineAsync().ConfigureAwait(false);
#endif
                if (!AddLine(line)) continue;

                var record = CreateRecord();
                if (record != null || line is null) return record;
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
        private bool AddLine([CanBeNull] string line)
#endif
        {
            if (line is null || line == RecordSeparator)
            {
                AddField();
                return true;
            }

            if (line.Length == 0) return false;

            // a line starting with white space continues the field body on the line before (ref. RFC 5646 §3.1.1), as does, to keep the
            // lenience of earlier versions, any other line that does not start with a field name
            var colonIndex = IsWhiteSpace(line[0]) ? -1 : line.IndexOf(':');
            var nameLength = colonIndex;
            while (nameLength > 0 && line[nameLength - 1] == ' ') nameLength--;

            if (nameLength > 0 && IsFieldName(line, nameLength))
            {
                AddField();
                _fieldName = GetFieldName(line, nameLength);
                _fieldBody = Trim(line, colonIndex + 1);
            }
            else if (_fieldName != null)
            {
                AppendToFieldBody(line);
            }

            return false;
        }

        private void AppendToFieldBody(string line)
        {
            var start = 0;
            var end = line.Length;
            while (start < end && IsWhiteSpace(line[start])) start++;
            while (end > start && IsWhiteSpace(line[end - 1])) end--;
            if (start == end) return;

            // folded lines are unfolded with one space between them, so most fields, which are not folded, need no builder
            if (_foldedFieldBody is null) _foldedFieldBody = new StringBuilder();
            if (_foldedFieldBody.Length == 0) _foldedFieldBody.Append(_fieldBody);
            if (_foldedFieldBody.Length > 0) _foldedFieldBody.Append(' ');
            _foldedFieldBody.Append(line, start, end - start);
        }

        private void AddField()
        {
            if (_fieldName is null) return;

            var fieldBody = _foldedFieldBody?.Length > 0 ? _foldedFieldBody.ToString() : _fieldBody;
            if (!string.IsNullOrEmpty(fieldBody)) _fields.Add(Tuple.Create<string, object>(_fieldName, fieldBody));

            _fieldName = null;
            _fieldBody = null;
            _foldedFieldBody?.Clear();
        }

#if NETCOREAPP3_0_OR_GREATER
        private ISubtagRegistryRecord? CreateRecord()
#else
        [CanBeNull]
        private ISubtagRegistryRecord CreateRecord()
#endif
        {
            var hasSubtag = false;
            var hasTag = false;
            foreach (var field in _fields)
            {
                if (field.Item1 == FieldName.FileDate)
                {
                    _fields.Clear();
                    return new SubtagRegistryFileDateRecord(field);
                }

                hasSubtag |= field.Item1 == FieldName.Subtag;
                hasTag |= field.Item1 == FieldName.Tag;
            }

#if NETCOREAPP3_0_OR_GREATER
            ISubtagRegistryRecord? subtagRegistryRecord = null;
#else
            ISubtagRegistryRecord subtagRegistryRecord = null;
#endif
            if (hasSubtag)
            {
                subtagRegistryRecord = new SubtagRegistrySubtagRecord(_fields);
            }
            else if (hasTag)
            {
                subtagRegistryRecord = new SubtagRegistryTagRecord(_fields);
            }

            _fields.Clear();
            return subtagRegistryRecord;
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
            while (start < end && IsWhiteSpace(line[start])) start++;
            while (end > start && IsWhiteSpace(line[end - 1])) end--;

            return start == end ? string.Empty : line.Substring(start, end - start);
        }

        private static bool IsWhiteSpace(char character)
        {
            return character == ' ' || character == '\t';
        }
    }
}
