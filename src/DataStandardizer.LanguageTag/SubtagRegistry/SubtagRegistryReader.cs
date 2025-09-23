using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#if NETSTANDARD
using JetBrains.Annotations;
#endif
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;

namespace DataStandardizer.LanguageTag.SubtagRegistry
{
    internal class SubtagRegistryReader
    {
        private const string FieldNameGroupName = "FieldName";
        private static readonly Regex FieldNameExpression;

        private readonly TextReader _sourceReader;

        static SubtagRegistryReader()
        {
            var fieldNames = new[]
            {
                FieldName.FileDate, FieldName.Type, FieldName.Subtag, FieldName.Tag, FieldName.Description, FieldName.Added, FieldName.Deprecated, FieldName.PreferredValue, FieldName.Prefix, FieldName.SuppressScript, FieldName.Macrolanguage,
                FieldName.Scope, FieldName.Comments
            };
            var pattern = $"^(?<{FieldNameGroupName}>{string.Join("|", fieldNames.Select(Regex.Escape))}):";
            var options = RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            options |= RegexOptions.Compiled;
#endif
            FieldNameExpression = new Regex(pattern, options);
        }

        internal SubtagRegistryReader(TextReader sourceReader)
        {
            _sourceReader = sourceReader;
        }

#if NETCOREAPP3_0_OR_GREATER
        internal ISubtagRegistryRecord? ReadRecord()
#else
        [CanBeNull]
        internal ISubtagRegistryRecord ReadRecord()
#endif
        {
            var fields = new List<Tuple<string, object>>();

#if NETCOREAPP3_0_OR_GREATER
            string? line;
#else
            string line;
#endif
            string fieldName = string.Empty, fieldBody = string.Empty;
            do
            {
                line = _sourceReader.ReadLine();
                if (line is null || line == "%%")
                {
                    if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldBody))
                    {
                        fields.Add(Tuple.Create<string, object>(fieldName, fieldBody));
                    }

                    continue;
                }

                var fieldNameMatch = FieldNameExpression.Match(line);
                if (fieldNameMatch.Success)
                {
                    if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldBody))
                    {
                        fields.Add(Tuple.Create<string, object>(fieldName, fieldBody));
                    }

                    fieldName = fieldNameMatch.Groups[FieldNameGroupName].Value;
                    fieldBody = line.Substring(fieldNameMatch.Index + fieldNameMatch.Length).TrimStart();
                }
                else
                {
                    fieldBody += line;
                }
            } while (line != null && line != "%%");

            return CreateRecord(fields);
        }

#if NETCOREAPP3_0_OR_GREATER
        internal async Task<ISubtagRegistryRecord?> ReadRecordAsync()
#else
        internal async Task<ISubtagRegistryRecord> ReadRecordAsync()
#endif
        {
            var fields = new List<Tuple<string, object>>();

#if NETCOREAPP3_0_OR_GREATER
            string? line;
#else
            string line;
#endif
            string fieldName = String.Empty, fieldBody = string.Empty;
            do
            {
                line = await _sourceReader.ReadLineAsync();
                if (line is null || line == "%%")
                {
                    if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldBody))
                    {
                        fields.Add(Tuple.Create<string, object>(fieldName, fieldBody));
                    }

                    continue;
                }

                var fieldNameMatch = FieldNameExpression.Match(line);
                if (fieldNameMatch.Success)
                {
                    if (!string.IsNullOrEmpty(fieldName) && !string.IsNullOrEmpty(fieldBody))
                    {
                        fields.Add(Tuple.Create<string, object>(fieldName, fieldBody));
                    }

                    fieldName = fieldNameMatch.Groups[FieldNameGroupName].Value;
                    fieldBody = line.Substring(fieldNameMatch.Index + fieldNameMatch.Length).TrimStart();
                }
                else
                {
                    fieldBody += line;
                }
            } while (line != null && line != "%%");

            return CreateRecord(fields);
        }

#if NETCOREAPP3_0_OR_GREATER
        private ISubtagRegistryRecord? CreateRecord(ICollection<Tuple<string, object>> fields)
#else
        [CanBeNull]
        private ISubtagRegistryRecord CreateRecord([NotNull] ICollection<Tuple<string, object>> fields)
#endif
        {
#if NETCOREAPP3_0_OR_GREATER
            ISubtagRegistryRecord? subtagRegistryRecord = null;
#else
            ISubtagRegistryRecord subtagRegistryRecord = null;
#endif

            var fileDateField = fields.FirstOrDefault(field => field.Item1 == FieldName.FileDate);
            if (fileDateField != null)
            {
                subtagRegistryRecord = new SubtagRegistryFileDateRecord(fileDateField);
            }
            else if (fields.Any(field => field.Item1 == FieldName.Subtag))
            {
                subtagRegistryRecord = new SubtagRegistrySubtagRecord(fields);
            }
            else if (fields.Any(field => field.Item1 == FieldName.Tag))
            {
                subtagRegistryRecord = new SubtagRegistryTagRecord(fields);
            }

            return subtagRegistryRecord;
        }
    }
}