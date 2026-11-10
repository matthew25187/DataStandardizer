using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// A Return-Path or Received field (RFC 5322 §3.6.7).  These are strictly informational, and any formal interpretation of them is left to RFC 5321.
    /// </summary>
    /// <remarks>
    /// A property has <c>null</c> or nothing for a field that is not valid, which <see cref="ImfMessage.Validate"/> reports.  The field stays as it was in <see cref="Field"/>.
    /// </remarks>
    public sealed class ImfTraceField
    {
        private static readonly ReadOnlyCollection<KeyValuePair<string, string>> NoPairs = new ReadOnlyCollection<KeyValuePair<string, string>>(new KeyValuePair<string, string>[0]);

        private ImfTraceField(ImfHeaderField field, ImfOptions options, ImfTraceFieldKind kind)
        {
            Field = field;
            Kind = kind;
            ReceivedPairs = NoPairs;
            if (kind == ImfTraceFieldKind.ReturnPath)
            {
                if (ImfAddressParser.TryParsePath(field.ValueBytes.Span, options, out var path)) ReturnPath = path;
            }
            else if (ImfAddressParser.TryParseReceived(field.ValueBytes.Span, options, out var tokens, out var hasDate, out var date))
            {
                ImfDateTime dateTime = default;
                if (hasDate && !ImfDateTime.TryParse(date, options, out dateTime)) return;

                ReceivedPairs = GetPairs(tokens);
                if (hasDate) ReceivedDate = dateTime;
            }
        }

        /// <summary>
        /// Gets the field, which holds what it was read with.
        /// </summary>
        public ImfHeaderField Field { get; }

        /// <summary>
        /// Gets the kind of field.
        /// </summary>
        public ImfTraceFieldKind Kind { get; }

        /// <summary>
        /// Gets the mailbox of a Return-Path field, which is <c>null</c> if the path is empty, as in <c>&lt;&gt;</c>, and for a Received field.
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfMailbox ReturnPath { get; }

        /// <summary>
        /// Gets the name-value pairs of a Received field, in order, such as <c>from</c> and <c>x.y.test</c>; and none for a Return-Path field.
        /// </summary>
        /// <remarks>
        /// RFC 5322 §3.6.7 has only a list of tokens, and RFC 5321 §4.4 has each clause be a name and a value.  So each odd token is taken as a name and the next as its value, and
        /// a last token with none has an empty value.  Comments are not kept.  An address in angle brackets keeps them, as in <c>&lt;mary@example.net&gt;</c>.
        /// </remarks>
        public IReadOnlyList<KeyValuePair<string, string>> ReceivedPairs { get; }

        /// <summary>
        /// Gets the date and time of a Received field, which is <c>null</c> if it has none, as the obsolete syntax allows (RFC 5322 §4.5.7), and for a Return-Path field.
        /// </summary>
        public ImfDateTime? ReceivedDate { get; }

        /// <summary>
        /// Gets the trace fields of a header, in the order of the header.
        /// </summary>
        internal static IReadOnlyList<ImfTraceField> GetFields(ImfHeaderFieldCollection fields, ImfOptions options)
        {
            var traceFields = new List<ImfTraceField>();
            foreach (var field in fields)
            {
                if (field.HasName(ImfFieldNames.ReturnPath))
                {
                    traceFields.Add(new ImfTraceField(field, options, ImfTraceFieldKind.ReturnPath));
                }
                else if (field.HasName(ImfFieldNames.Received))
                {
                    traceFields.Add(new ImfTraceField(field, options, ImfTraceFieldKind.Received));
                }
            }

            return new ReadOnlyCollection<ImfTraceField>(traceFields);
        }

        private static ReadOnlyCollection<KeyValuePair<string, string>> GetPairs(string[] tokens)
        {
            var pairs = new KeyValuePair<string, string>[(tokens.Length + 1) / 2];
            for (var index = 0; index < pairs.Length; index++)
            {
                var valueIndex = index * 2 + 1;
                pairs[index] = new KeyValuePair<string, string>(tokens[index * 2], valueIndex < tokens.Length ? tokens[valueIndex] : string.Empty);
            }

            return new ReadOnlyCollection<KeyValuePair<string, string>>(pairs);
        }
    }
}
