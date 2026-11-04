using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Checks the header of a message against the rules of RFC 5322 §3.6, for <see cref="ImfMessage.Validate"/>, <see cref="ImfReader"/> and <see cref="ImfWriter"/>.
    /// </summary>
    internal static class ImfConformance
    {
        private const string EntityPath = "";

        private const string InvalidValueMessage = "The body of the {0} field does not follow its grammar (RFC 5322 §3.6, RFC 6854 §2.1).";
        private const string MissingFieldMessage = "The header has no {0} field, which RFC 5322 §3.6 requires.";
        private const string MissingSenderMessage = "The {0} field has more than one mailbox, so a {1} field is required (RFC 5322 §3.6.2 and §3.6.6, RFC 6854 §2.1).";
        private const string MissingResentFieldMessage = "A block of resent fields has no {0} field, which RFC 5322 §3.6.6 requires.";
        private const string TooManyMessage = "The header has more than one {0} field, which RFC 5322 §3.6 allows at most once.";

        private static readonly string[] RequiredFields = { ImfFieldNames.Date, ImfFieldNames.From };

        // ref. RFC 5322 §3.6: the table of fields with a maximum number of 1.
        private static readonly string[] SingleFields =
        {
            ImfFieldNames.Date,
            ImfFieldNames.From,
            ImfFieldNames.Sender,
            ImfFieldNames.ReplyTo,
            ImfFieldNames.To,
            ImfFieldNames.Cc,
            ImfFieldNames.Bcc,
            ImfFieldNames.MessageId,
            ImfFieldNames.InReplyTo,
            ImfFieldNames.References,
            ImfFieldNames.Subject,
        };

        /// <summary>
        /// Finds every problem in the header of a message.
        /// </summary>
        internal static List<ImfDiagnostic> Check(ImfMessage message, ImfOptions options)
        {
            var diagnostics = new List<ImfDiagnostic>();
            var fields = message.Fields;
            foreach (var name in RequiredFields)
            {
                if (!fields.Contains(name)) AddMissing(diagnostics, message, MissingFieldMessage, name);
            }

            foreach (var name in SingleFields)
            {
                var found = fields.FindAll(name);
                for (var index = 1; index < found.Count; index++)
                {
                    diagnostics.Add(Build(ImfDiagnosticKind.CardinalityViolation, string.Format(TooManyMessage, name), found[index]));
                }
            }

            foreach (var field in fields)
            {
                if (!ImfFieldValues.IsValid(field, options))
                {
                    diagnostics.Add(Build(ImfDiagnosticKind.InvalidFieldValue, string.Format(InvalidValueMessage, field.Name), field));
                }
            }

            CheckSender(diagnostics, message, ImfFieldValues.GetAddressList(fields.Find(ImfFieldNames.From), options), fields.Contains(ImfFieldNames.Sender), ImfFieldNames.From, ImfFieldNames.Sender);

            foreach (var block in ImfResentBlock.GetBlocks(fields, options))
            {
                if (block.Find(ImfFieldNames.ResentDate) is null) AddMissing(diagnostics, message, MissingResentFieldMessage, ImfFieldNames.ResentDate);
                if (block.Find(ImfFieldNames.ResentFrom) is null) AddMissing(diagnostics, message, MissingResentFieldMessage, ImfFieldNames.ResentFrom);

                CheckSender(diagnostics, message, block.From, !(block.Find(ImfFieldNames.ResentSender) is null), ImfFieldNames.ResentFrom, ImfFieldNames.ResentSender);
            }

            return diagnostics;
        }

        /// <summary>
        /// Determines if some options accept a kind of problem.
        /// </summary>
        internal static bool IsAllowed(ImfDiagnosticKind kind, ImfOptions options)
        {
            switch (kind)
            {
                case ImfDiagnosticKind.InvalidRelatedStructure:
                    return options.AllowInvalidRelatedStructure;
                case ImfDiagnosticKind.MissingRequiredField:
                    return options.AllowMissingRequiredFields;
                case ImfDiagnosticKind.CardinalityViolation:
                    return options.AllowCardinalityViolations;
                case ImfDiagnosticKind.InvalidFieldValue:
                    return options.AllowInvalidValues;
                case ImfDiagnosticKind.MissingMboxSeparator:
                    return options.AllowMissingMboxSeparator;
                case ImfDiagnosticKind.InvalidMboxSeparator:
                    return options.AllowInvalidMboxSeparator;
                case ImfDiagnosticKind.MissingMboxBlankLine:
                    return options.AllowMissingMboxBlankLine;
                case ImfDiagnosticKind.InvalidMboxContentLength:
                    return options.AllowInvalidMboxContentLength;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a profile or some options accept a problem.
        /// </summary>
        internal static bool IsAllowed(ImfDiagnostic diagnostic, ImfOptions options, ImfConformanceProfile profile)
        {
            // ref. RFC 2557 §3: the top level of an aggregate document is a MIME entity as much as a message, and the origination fields of RFC 5322 §3.6 say nothing about it.  A block of
            // resent fields does say that a message was sent, so its rules stay.
            if (profile == ImfConformanceProfile.Mhtml && diagnostic.Kind == ImfDiagnosticKind.MissingRequiredField
                && (diagnostic.FieldName == ImfFieldNames.Date || diagnostic.FieldName == ImfFieldNames.From || diagnostic.FieldName == ImfFieldNames.Sender))
                return true;

            return IsAllowed(diagnostic.Kind, options);
        }

        /// <summary>
        /// Builds the exception for a problem that the options don't accept.
        /// </summary>
        /// <param name="diagnostic">The problem.</param>
        /// <param name="isFromReader"><c>true</c> if a reader found the problem, so its position in the data is known.</param>
        internal static ImfFileException BuildException(ImfDiagnostic diagnostic, bool isFromReader)
        {
            var exception = new ImfFileException(diagnostic.Message);
            exception.Data[ImfFileException.DataItemName.DiagnosticKind] = diagnostic.Kind;
            exception.Data[ImfFileException.DataItemName.EntityPath] = diagnostic.EntityPath;
            exception.Data[ImfFileException.DataItemName.FieldName] = diagnostic.FieldName;
            if (isFromReader)
            {
                exception.Data[ImfFileException.DataItemName.BytePosition] = diagnostic.BytePosition;
                exception.Data[ImfFileException.DataItemName.LineNumber] = diagnostic.LineNumber;
            }

            return exception;
        }

        // ref. RFC 6854 §2.1: "If the from field contains more than one mailbox specification (including all mailboxes included in any groups), then the sender field [...] MUST appear".
        private static void CheckSender(List<ImfDiagnostic> diagnostics, ImfMessage message, [AllowNull] ImfAddressList from, bool hasSender, string fromName, string senderName)
        {
            if (hasSender || from is null || ImfFieldValues.CountMailboxes(from) <= 1) return;

            diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.MissingRequiredField, string.Format(MissingSenderMessage, fromName, senderName), message.HeaderEndBytePosition,
                EntityPath, senderName, message.HeaderEndLineNumber));
        }

        private static void AddMissing(List<ImfDiagnostic> diagnostics, ImfMessage message, string template, string name)
        {
            diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.MissingRequiredField, string.Format(template, name), message.HeaderEndBytePosition, EntityPath, name,
                message.HeaderEndLineNumber));
        }

        private static ImfDiagnostic Build(ImfDiagnosticKind kind, string text, ImfHeaderField field)
        {
            return new ImfDiagnostic(kind, text, field.BytePosition, EntityPath, field.Name, field.LineNumber);
        }
    }
}
