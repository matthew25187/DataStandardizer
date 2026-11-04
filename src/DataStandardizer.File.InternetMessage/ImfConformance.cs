using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Checks the header of a message against the rules of RFC 5322 §3.6, for <see cref="ImfMessage.Validate"/>, <see cref="ImfReader"/> and <see cref="ImfWriter"/>.
    /// </summary>
    internal static class ImfConformance
    {
        private const string EntityPath = "";

        private const string InvalidValueMessage = "The body of the {0} field does not follow its grammar (RFC 5322 §3.6, RFC 6854 §2.1).";
        private const string MissingMimeVersionMessage = "The header has a MIME field and no MIME-Version field, which RFC 2045 §4 requires.  ImfOptions.AllowMissingMimeVersion accepts it.";
        private const string MissingEmbeddedFieldMessage = "An embedded message has none of From, Subject or Date, one of which RFC 2046 §5.2.1 requires.  ImfOptions.AllowMissingRequiredFields accepts it.";
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
                case ImfDiagnosticKind.MissingMimeVersion:
                    return options.AllowMissingMimeVersion;
                case ImfDiagnosticKind.InvalidBodyOctets:
                    return options.AllowInvalidBodyOctets;
                case ImfDiagnosticKind.LongBodyLine:
                    return options.AllowLongLines;
                case ImfDiagnosticKind.MboxCarriageReturnLineFeed:
                    return options.AllowMboxCarriageReturnLineFeed;
                case ImfDiagnosticKind.EightBitMboxContent:
                    return options.AllowEightBitMboxContent;
                case ImfDiagnosticKind.InvalidEmlxByteCount:
                    return options.AllowInvalidEmlxByteCount;
                case ImfDiagnosticKind.InvalidEmlxMetadata:
                    return options.AllowInvalidEmlxMetadata;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Determines if a reader accepts a problem in the header of a message, which <see cref="Check"/> found.
        /// </summary>
        internal static bool IsAllowedInHeaderWhenReading(ImfDiagnostic diagnostic, ImfOptions options)
        {
            // ref. RFC 5322 §3.5: message = (fields / obs-fields) [CRLF body]; §4: "they MUST be accepted and parsed by a conformant receiver"; and §4.5: obs-fields = *(...), which
            // has no minimum or maximum for any field, and no rule for how they go together.
            return diagnostic.Kind == ImfDiagnosticKind.CardinalityViolation || diagnostic.Kind == ImfDiagnosticKind.MissingRequiredField || IsAllowed(diagnostic.Kind, options);
        }

        /// <summary>
        /// Finds the problems in what a message holds, other than its header, for <see cref="ImfMessage.Validate"/> and a writer: the MIME-Version field, embedded messages, and the bytes of the
        /// bodies that hadn't been changed.
        /// </summary>
        /// <exception cref="ImfFileException"><see cref="ImfMessage.RawBody"/> was set, and it breaks a rule of RFC 2046 §5.1 that the options don't accept.</exception>
        internal static void CheckContent(ImfMessage message, ImfOptions options, List<ImfDiagnostic> diagnostics)
        {
            // ref. RFC 2045 §4.  A writer adds the field to a message whose body has changed.
            if (!message.IsBodyChanged && HasMimeField(message) && !message.Fields.Contains(ImfFieldNames.MimeVersion))
            {
                diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.MissingMimeVersion, MissingMimeVersionMessage, message.HeaderEndBytePosition, EntityPath, ImfFieldNames.MimeVersion, message.HeaderEndLineNumber));
            }

            CheckEmbedded(message, diagnostics);

            var check = new ImfBodyCheck(options, message.LineFeedIsLineBreak);
            CheckBodies(message, check, diagnostics);
        }

        /// <summary>
        /// Finds the embedded messages at any depth with none of the fields that RFC 2046 §5.2.1 requires of them.
        /// </summary>
        internal static void CheckEmbedded(ImfMessage message, List<ImfDiagnostic> diagnostics)
        {
            foreach (var node in message.EnumerateEntities())
            {
                if (node.Entity is ImfMessagePart part)
                {
                    var fields = part.Message.Fields;
                    if (!fields.Contains(ImfFieldNames.From) && !fields.Contains(ImfFieldNames.Subject) && !fields.Contains(ImfFieldNames.Date))
                    {
                        diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.MissingRequiredField, MissingEmbeddedFieldMessage, part.Message.HeaderEndBytePosition, node.Path, ImfFieldNames.From, part.Message.HeaderEndLineNumber));
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a message has an entity with the <c>binary</c> transfer encoding, which has no lines (RFC 2045 §2.9), at any depth.
        /// </summary>
        internal static bool HasBinaryEntity(ImfMessage message)
        {
            foreach (var node in message.EnumerateEntities())
            {
                if (node.Entity.ContentTransferEncoding == MimeTransferEncoding.Binary) return true;
            }

            return false;
        }

        private static bool HasMimeField(ImfMessage message)
        {
            var fields = message.Fields;
            return fields.Contains(ImfFieldNames.ContentType) || fields.Contains(ImfFieldNames.ContentTransferEncoding) || fields.Contains(ImfFieldNames.ContentId)
                || fields.Contains(ImfFieldNames.ContentDescription) || fields.Contains(ImfFieldNames.ContentDisposition) || fields.Contains(ImfFieldNames.ContentLanguage);
        }

        // The open entities are kept in a list, not on the call stack, so nesting can't overflow the stack.
        private static void CheckBodies(ImfMessage message, ImfBodyCheck check, List<ImfDiagnostic> diagnostics)
        {
            var isTopMime = HasMimeField(message) || message.Fields.Contains(ImfFieldNames.MimeVersion);
            var pending = new Stack<PendingBody>();
            pending.Push(new PendingBody(message.Body, isTopMime, MimeDataDomain.Binary, EntityPath));
            while (pending.Count > 0)
            {
                var item = pending.Pop();
                var encoding = item.Entity.ContentTransferEncoding;
                var domain = ImfBodyCheck.GetAllowedDomain(encoding, item.Enclosing);
                if (item.Entity is ImfMessagePart part)
                {
                    pending.Push(new PendingBody(part.Message.Body, item.IsMime || part.Message.Fields.Contains(ImfFieldNames.MimeVersion), domain, item.Path));
                }
                else if (item.Entity is ImfMultipart multipart)
                {
                    CheckBytes(multipart.Preamble ?? default(ReadOnlyMemory<byte>), check, domain, item.IsMime, ImfBodyCheck.MaxLineLength, item.Path, diagnostics);
                    CheckBytes(multipart.Epilogue ?? default(ReadOnlyMemory<byte>), check, domain, item.IsMime, ImfBodyCheck.MaxLineLength, item.Path, diagnostics);
                    for (var index = multipart.Parts.Count - 1; index >= 0; index--)
                    {
                        var number = (index + 1).ToString(CultureInfo.InvariantCulture);
                        pending.Push(new PendingBody(multipart.Parts[index], item.IsMime, domain, item.Path.Length == 0 ? number : item.Path + "." + number));
                    }
                }
                else if (item.Entity.IsRead && !item.Entity.HasChangedBody)
                {
                    // What a writer encodes itself is checked as it is encoded.
                    CheckBytes(item.Entity.RawBody, check, domain, item.IsMime, ImfBodyCheck.GetMaxLineLength(encoding), item.Path, diagnostics);
                }
            }
        }

        private struct PendingBody
        {
            internal readonly ImfEntity Entity;
            internal readonly bool IsMime;
            internal readonly MimeDataDomain Enclosing;
            internal readonly string Path;

            internal PendingBody(ImfEntity entity, bool isMime, MimeDataDomain enclosing, string path)
            {
                Entity = entity;
                IsMime = isMime;
                Enclosing = enclosing;
                Path = path;
            }
        }

        private static void CheckBytes(ReadOnlyMemory<byte> bytes, ImfBodyCheck check, MimeDataDomain domain, bool isMime, int maxLineLength, string path, List<ImfDiagnostic> diagnostics)
        {
            check.Start(domain, isMime, maxLineLength);
            check.Check(bytes.Span, out var foundLongLine, out var foundInvalidOctet, out _, out _);
            foundInvalidOctet |= check.End();
            if (foundLongLine) diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.LongBodyLine, ImfBodyCheck.LongLineMessage, -1, path));
            if (foundInvalidOctet) diagnostics.Add(new ImfDiagnostic(ImfDiagnosticKind.InvalidBodyOctets, ImfBodyCheck.InvalidOctetsMessage, -1, path));
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
