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
        /// Counts the mailboxes in a list of addresses, including all those in groups (RFC 6854 §2.1).
        /// </summary>
        internal static int CountMailboxes(ImfAddressList addresses)
        {
            var count = 0;
            foreach (var address in addresses)
            {
                var group = address as ImfGroup;
                count += group is null ? 1 : group.Mailboxes.Count;
            }

            return count;
        }

        /// <summary>
        /// Determines if the body of a field follows the grammar of its name, without the accommodation of <see cref="ImfOptions.AllowInvalidValues"/>.  A field of any other
        /// name, or an unstructured field, is valid whatever its body.
        /// </summary>
        internal static bool IsValid(ImfHeaderField field, ImfOptions options)
        {
            var value = field.ValueBytes.Span;
            switch (field.Name.ToUpperInvariant())
            {
                case "DATE":
                case "RESENT-DATE":
                    return ImfDateTime.TryParse(value, out _);

                case "FROM":
                case "REPLY-TO":
                case "TO":
                case "CC":
                case "RESENT-FROM":
                case "RESENT-REPLY-TO":
                case "RESENT-TO":
                case "RESENT-CC":
                    return ImfAddressList.TryParse(value, options, out _);

                case "SENDER":
                case "RESENT-SENDER":
                    return ImfAddress.TryParse(value, options, out var sender) && IsValidSender(sender);

                case "BCC":
                case "RESENT-BCC":
                    return ImfAddressList.TryParse(value, options, out _) || ImfAddressParser.IsEmptyAddressList(value, options);

                case "MESSAGE-ID":
                case "RESENT-MESSAGE-ID":
                    return ImfMessageId.TryParse(value, out _);

                case "IN-REPLY-TO":
                case "REFERENCES":
                    return ImfMessageId.TryParseList(value, out _);

                case "KEYWORDS":
                    return ImfPhraseList.TryParse(value, options, out _);

                case "RETURN-PATH":
                    return ImfAddressParser.TryParsePath(value, options, out _);

                case "RECEIVED":
                    return ImfAddressParser.TryParseReceived(value, options, out _, out var hasDate, out var date) && (!hasDate || ImfDateTime.TryParse(date, out _));

                default:
                    return true;
            }
        }

        // ref. RFC 6854 §2.1: "If the sender field uses group syntax, the group MUST NOT contain more than one mailbox."
        private static bool IsValidSender(ImfAddress sender)
        {
            return !(sender is ImfGroup group) || group.Mailboxes.Count <= 1;
        }
    }
}
