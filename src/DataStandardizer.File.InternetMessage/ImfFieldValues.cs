using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Gets the typed values of header fields for <see cref="ImfMessage"/> and <see cref="ImfResentBlock"/>, and checks the bodies of the fields of RFC 5322 §3.6.
    /// </summary>
    /// <remarks>
    /// A field that is absent or not valid has no value.  Finding those that are not valid is left to <see cref="ImfConformance"/>.
    /// </remarks>
    internal static class ImfFieldValues
    {
        private static readonly Dictionary<string, FieldGrammar> Grammars = new Dictionary<string, FieldGrammar>(StringComparer.OrdinalIgnoreCase)
        {
            { ImfFieldNames.Date, FieldGrammar.DateTime },
            { ImfFieldNames.ResentDate, FieldGrammar.DateTime },
            { ImfFieldNames.From, FieldGrammar.AddressList },
            { ImfFieldNames.ReplyTo, FieldGrammar.AddressList },
            { ImfFieldNames.To, FieldGrammar.AddressList },
            { ImfFieldNames.Cc, FieldGrammar.AddressList },
            { ImfFieldNames.ResentFrom, FieldGrammar.AddressList },
            { ImfFieldNames.ResentReplyTo, FieldGrammar.AddressList },
            { ImfFieldNames.ResentTo, FieldGrammar.AddressList },
            { ImfFieldNames.ResentCc, FieldGrammar.AddressList },
            { ImfFieldNames.Sender, FieldGrammar.Sender },
            { ImfFieldNames.ResentSender, FieldGrammar.Sender },
            { ImfFieldNames.Bcc, FieldGrammar.Bcc },
            { ImfFieldNames.ResentBcc, FieldGrammar.Bcc },
            { ImfFieldNames.MessageId, FieldGrammar.MessageId },
            { ImfFieldNames.ResentMessageId, FieldGrammar.MessageId },
            { ImfFieldNames.InReplyTo, FieldGrammar.MessageIdList },
            { ImfFieldNames.References, FieldGrammar.MessageIdList },
            { ImfFieldNames.Keywords, FieldGrammar.PhraseList },
            { ImfFieldNames.ReturnPath, FieldGrammar.Path },
            { ImfFieldNames.Received, FieldGrammar.Received },
        };

        private enum FieldGrammar
        {
            DateTime,
            AddressList,
            Sender,
            Bcc,
            MessageId,
            MessageIdList,
            PhraseList,
            Path,
            Received,
        }

        internal static ImfDateTime? GetDate([AllowNull] ImfHeaderField field, ImfOptions options)
        {
            if (field is null) return null;

            return ImfDateTime.TryParse(field.ValueBytes.Span, options, out var result) ? result : (ImfDateTime?)null;
        }

        [return: MaybeNull]
        internal static ImfAddressList GetAddressList([AllowNull] ImfHeaderField field, ImfOptions options)
        {
            if (field is null) return null;

            return ImfAddressList.TryParse(field.ValueBytes.Span, options, out var result) ? result : null;
        }

        [return: MaybeNull]
        internal static ImfAddress GetSender([AllowNull] ImfHeaderField field, ImfOptions options)
        {
            if (field is null) return null;

            return ImfAddress.TryParse(field.ValueBytes.Span, options, out var result) && IsValidSender(result) ? result : null;
        }

        [return: MaybeNull]
        internal static ImfMessageId GetMessageId([AllowNull] ImfHeaderField field)
        {
            if (field is null) return null;

            return ImfMessageId.TryParse(field.ValueBytes.Span, out var result) ? result : null;
        }

        [return: MaybeNull]
        internal static IReadOnlyList<ImfMessageId> GetMessageIds([AllowNull] ImfHeaderField field)
        {
            if (field is null) return null;

            return ImfMessageId.TryParseList(field.ValueBytes.Span, out var result) ? result : null;
        }

        /// <summary>
        /// Counts the mailboxes in the addresses of a field, including all those in groups (RFC 6854 §2.1), without keeping the addresses.
        /// </summary>
        /// <returns>The number; or 0 if the field is absent or not valid.</returns>
        internal static int CountMailboxes([AllowNull] ImfHeaderField field, ImfOptions options)
        {
            if (field is null) return 0;

            ImfHeaderText.CheckOptions(options);

            return ImfAddressParser.IsValidAddressList(field.ValueBytes.Span, options, out var count) ? count : 0;
        }

        /// <summary>
        /// Determines if the body of a field follows the grammar of its name, without the accommodation of <see cref="ImfOptions.AllowInvalidValues"/>.  A field of any other
        /// name, or an unstructured field, is valid whatever its body.  The value of the field isn't kept, so this asks for nothing to be built that a <c>TryParse</c> method would.
        /// </summary>
        internal static bool IsValid(ImfHeaderField field, ImfOptions options)
        {
            if (!Grammars.TryGetValue(field.Name, out var grammar)) return true;

            var value = field.ValueBytes.Span;
            switch (grammar)
            {
                case FieldGrammar.DateTime:
                    return ImfDateTime.TryParse(value, out _);

                case FieldGrammar.AddressList:
                    ImfHeaderText.CheckOptions(options);
                    return ImfAddressParser.IsValidAddressList(value, options, out _);

                case FieldGrammar.Sender:
                    // ref. RFC 6854 §2.1: "If the sender field uses group syntax, the group MUST NOT contain more than one mailbox."
                    ImfHeaderText.CheckOptions(options);
                    return ImfAddressParser.IsValidAddress(value, options, out var mailboxCount) && mailboxCount <= 1;

                case FieldGrammar.Bcc:
                    ImfHeaderText.CheckOptions(options);
                    return ImfAddressParser.IsValidAddressList(value, options, out _) || ImfAddressParser.IsEmptyAddressList(value, options);

                case FieldGrammar.MessageId:
                    return ImfAddressParser.IsValidMessageId(value);

                case FieldGrammar.MessageIdList:
                    return ImfAddressParser.IsValidMessageIdList(value);

                case FieldGrammar.PhraseList:
                    ImfHeaderText.CheckOptions(options);
                    return ImfAddressParser.IsValidPhraseList(value, options);

                case FieldGrammar.Path:
                    return ImfAddressParser.IsValidPath(value, options);

                default:
                    return ImfAddressParser.IsValidReceived(value, options, out var hasDate, out var date) && (!hasDate || ImfDateTime.TryParse(date, out _));
            }
        }

        // ref. RFC 6854 §2.1: "If the sender field uses group syntax, the group MUST NOT contain more than one mailbox."
        private static bool IsValidSender(ImfAddress sender)
        {
            return !(sender is ImfGroup group) || group.Mailboxes.Count <= 1;
        }
    }
}
