using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// The resent fields of one resending of a message (RFC 5322 §3.6.6).
    /// </summary>
    /// <remarks>
    /// A property has <c>null</c> for a field that is absent or not valid, which <see cref="ImfMessage.Validate"/> reports.  An obsolete Resent-Reply-To field (RFC 5322 §4.5.6)
    /// is among the <see cref="Fields"/>, and has no property of its own.
    /// </remarks>
    public sealed class ImfResentBlock
    {
        private static readonly string[] ResentFieldNames =
        {
            ImfFieldNames.ResentDate,
            ImfFieldNames.ResentFrom,
            ImfFieldNames.ResentSender,
            ImfFieldNames.ResentTo,
            ImfFieldNames.ResentCc,
            ImfFieldNames.ResentBcc,
            ImfFieldNames.ResentMessageId,
            ImfFieldNames.ResentReplyTo,
        };

        private readonly ImfOptions _options;

        private ImfResentBlock(List<ImfHeaderField> fields, ImfOptions options)
        {
            Fields = new ReadOnlyCollection<ImfHeaderField>(fields);
            _options = options;
        }

        /// <summary>
        /// Gets the fields of the block, in order.
        /// </summary>
        public IReadOnlyList<ImfHeaderField> Fields { get; }

        /// <summary>
        /// Gets the date and time of the Resent-Date field.
        /// </summary>
        public ImfDateTime? Date => ImfFieldValues.GetDate(Find(ImfFieldNames.ResentDate), _options);

        /// <summary>
        /// Gets the addresses of the Resent-From field.
        /// </summary>
        [MaybeNull]
        public ImfAddressList From => ImfFieldValues.GetAddressList(Find(ImfFieldNames.ResentFrom), _options);

        /// <summary>
        /// Gets the address of the Resent-Sender field.
        /// </summary>
        [MaybeNull]
        public ImfAddress Sender => ImfFieldValues.GetSender(Find(ImfFieldNames.ResentSender), _options);

        /// <summary>
        /// Gets the addresses of the Resent-To field.
        /// </summary>
        [MaybeNull]
        public ImfAddressList To => ImfFieldValues.GetAddressList(Find(ImfFieldNames.ResentTo), _options);

        /// <summary>
        /// Gets the addresses of the Resent-Cc field.
        /// </summary>
        [MaybeNull]
        public ImfAddressList Cc => ImfFieldValues.GetAddressList(Find(ImfFieldNames.ResentCc), _options);

        /// <summary>
        /// Gets the addresses of the Resent-Bcc field.  It is also <c>null</c> for a field with no addresses, which RFC 5322 §3.6.6 allows, and which is among the <see cref="Fields"/>.
        /// </summary>
        [MaybeNull]
        public ImfAddressList Bcc => ImfFieldValues.GetAddressList(Find(ImfFieldNames.ResentBcc), _options);

        /// <summary>
        /// Gets the identifier of the Resent-Message-ID field.
        /// </summary>
        [MaybeNull]
        public ImfMessageId MessageId => ImfFieldValues.GetMessageId(Find(ImfFieldNames.ResentMessageId));

        /// <summary>
        /// Groups the resent fields of a header into blocks, in the order of the header.
        /// </summary>
        /// <remarks>
        /// RFC 5322 gives no mark between blocks, and no order within one.  So a block ends at any other field, or at a field that it already has.
        /// </remarks>
        internal static IReadOnlyList<ImfResentBlock> GetBlocks(ImfHeaderFieldCollection fields, ImfOptions options)
        {
            var blocks = new List<ImfResentBlock>();
            var current = new List<ImfHeaderField>();
            foreach (var field in fields)
            {
                var isResent = IsResentField(field);
                if (current.Count > 0 && (!isResent || current.Exists(other => other.HasName(field.Name))))
                {
                    blocks.Add(new ImfResentBlock(current, options));
                    current = new List<ImfHeaderField>();
                }

                if (isResent) current.Add(field);
            }

            if (current.Count > 0) blocks.Add(new ImfResentBlock(current, options));

            return new ReadOnlyCollection<ImfResentBlock>(blocks);
        }

        [return: MaybeNull]
        internal ImfHeaderField Find(string name)
        {
            foreach (var field in Fields)
            {
                if (field.HasName(name)) return field;
            }

            return null;
        }

        private static bool IsResentField(ImfHeaderField field)
        {
            foreach (var name in ResentFieldNames)
            {
                if (field.HasName(name)) return true;
            }

            return false;
        }
    }
}
