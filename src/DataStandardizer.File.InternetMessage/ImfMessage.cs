using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// An Internet message (RFC 5322): an ordered list of header fields, with typed accessors over them, and a body.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Header fields.</b>  <see cref="Fields"/> keeps every field in order, so a field that hasn't changed is written back byte for byte.  Each accessor reads and writes through
    /// it.  Setting one replaces the field where it is, or adds it after the other fields if there is none, and removes any others of that name.  Setting one to <c>null</c>, or to
    /// an empty list, removes the field.  Other fields are not touched.
    /// </para>
    /// <para>
    /// <b>Values.</b>  An accessor gives <c>null</c>, or nothing, if the field is absent or its body is not valid, which <see cref="Validate"/> reports.  If there is more than one
    /// of a field that may appear only once, it gives the first.  Text and display names are decoded from RFC 2047 encoded words.  A value that is set is formatted when it is written,
    /// with the <see cref="ImfOptions.HeaderEncoding"/> of the writer.
    /// </para>
    /// <para>
    /// <b>Body.</b>  <see cref="RawBody"/> holds the bytes after the empty line that ends the header, which are not interpreted or checked.
    /// </para>
    /// </remarks>
    public sealed class ImfMessage
    {
        private static readonly ImfHeaderField[] NoFields = new ImfHeaderField[0];

        /// <summary>
        /// Creates a message with no fields and no body, whose accessors use the default options.
        /// </summary>
        public ImfMessage() : this(new ImfOptions())
        {
        }

        /// <summary>
        /// Creates a message with no fields and no body.
        /// </summary>
        /// <param name="options">The options that the accessors and <see cref="Validate"/> parse fields under.</param>
        /// <exception cref="ArgumentNullException"><paramref name="options"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="options"/> has no <see cref="ImfOptions.CharsetResolver"/>, or an <see cref="ImfOptions.HeaderFallbackCharset"/>
        /// that is needed and can't be resolved.</exception>
        public ImfMessage(ImfOptions options)
        {
            ImfHeaderText.CheckOptions(options);
            Options = options;
        }

        /// <summary>
        /// Gets the options that the accessors and <see cref="Validate"/> parse fields under.
        /// </summary>
        public ImfOptions Options { get; }

        /// <summary>
        /// Gets the header fields, in order.
        /// </summary>
        public ImfHeaderFieldCollection Fields { get; } = new ImfHeaderFieldCollection();

        /// <summary>
        /// Gets or sets the body: the bytes after the empty line that ends the header.  They are written as they are.
        /// </summary>
        /// <remarks>
        /// The memory must not change until a write has finished.
        /// </remarks>
        public ReadOnlyMemory<byte> RawBody { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the Date field (RFC 5322 §3.6.1).
        /// </summary>
        /// <exception cref="ArgumentException">On set: the value was made with <c>default</c>.</exception>
        public ImfDateTime? Date
        {
            get => ImfFieldValues.GetDate(Fields.Find(ImfFieldNames.Date), Options);
            set
            {
                if (value.HasValue && value.Value == default(ImfDateTime))
                    throw new ArgumentException("The date-time is not initialised.", nameof(value));

                var text = value.HasValue ? value.Value.ToString() : null;
                Fields.Set(ImfFieldNames.Date, text is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.Date, encoding => text) });
            }
        }

        /// <summary>
        /// Gets or sets the addresses of the From field (RFC 5322 §3.6.2, RFC 6854).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList From
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.From), Options);
            set => SetAddressList(ImfFieldNames.From, value);
        }

        /// <summary>
        /// Gets or sets the address of the Sender field (RFC 5322 §3.6.2, RFC 6854).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddress Sender
        {
            get => ImfFieldValues.GetSender(Fields.Find(ImfFieldNames.Sender), Options);
            set
            {
                var address = value;
                Fields.Set(ImfFieldNames.Sender, address is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.Sender, address.ToString) });
            }
        }

        /// <summary>
        /// Gets or sets the addresses of the Reply-To field (RFC 5322 §3.6.2).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList ReplyTo
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.ReplyTo), Options);
            set => SetAddressList(ImfFieldNames.ReplyTo, value);
        }

        /// <summary>
        /// Gets or sets the addresses of the To field (RFC 5322 §3.6.3).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList To
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.To), Options);
            set => SetAddressList(ImfFieldNames.To, value);
        }

        /// <summary>
        /// Gets or sets the addresses of the Cc field (RFC 5322 §3.6.3).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList Cc
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.Cc), Options);
            set => SetAddressList(ImfFieldNames.Cc, value);
        }

        /// <summary>
        /// Gets or sets the addresses of the Bcc field (RFC 5322 §3.6.3).  It also gives <c>null</c> for a field with no addresses, which that section allows, and which is among the
        /// <see cref="Fields"/>.
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfAddressList Bcc
        {
            get => ImfFieldValues.GetAddressList(Fields.Find(ImfFieldNames.Bcc), Options);
            set => SetAddressList(ImfFieldNames.Bcc, value);
        }

        /// <summary>
        /// Gets or sets the identifier of the Message-ID field (RFC 5322 §3.6.4).
        /// </summary>
        [AllowNull, MaybeNull]
        public ImfMessageId MessageId
        {
            get => ImfFieldValues.GetMessageId(Fields.Find(ImfFieldNames.MessageId));
            set
            {
                var messageId = value;
                Fields.Set(ImfFieldNames.MessageId, messageId is null ? NoFields : new[] { ImfHeaderField.FromValue(ImfFieldNames.MessageId, encoding => messageId.ToString()) });
            }
        }

        /// <summary>
        /// Gets or sets the identifiers of the In-Reply-To field (RFC 5322 §3.6.4).
        /// </summary>
        /// <exception cref="ArgumentException">On set: the identifiers hold a <c>null</c> element.</exception>
        [AllowNull, MaybeNull]
        public IReadOnlyList<ImfMessageId> InReplyTo
        {
            get => ImfFieldValues.GetMessageIds(Fields.Find(ImfFieldNames.InReplyTo));
            set => SetMessageIds(ImfFieldNames.InReplyTo, value);
        }

        /// <summary>
        /// Gets or sets the identifiers of the References field (RFC 5322 §3.6.4).
        /// </summary>
        /// <exception cref="ArgumentException">On set: the identifiers hold a <c>null</c> element.</exception>
        [AllowNull, MaybeNull]
        public IReadOnlyList<ImfMessageId> References
        {
            get => ImfFieldValues.GetMessageIds(Fields.Find(ImfFieldNames.References));
            set => SetMessageIds(ImfFieldNames.References, value);
        }

        /// <summary>
        /// Gets or sets the text of the Subject field (RFC 5322 §3.6.5).
        /// </summary>
        /// <exception cref="ImfFileException">On set: the text is not valid Unicode, or holds a control character other than tab.</exception>
        [AllowNull, MaybeNull]
        public string Subject
        {
            get => Fields.Find(ImfFieldNames.Subject)?.GetDecodedValue(Options);
            set
            {
                var text = value;
                Fields.Set(ImfFieldNames.Subject, text is null ? NoFields : new[] { ImfHeaderField.FromText(ImfFieldNames.Subject, text) });
            }
        }

        /// <summary>
        /// Gets or sets the text of each Comments field, in order (RFC 5322 §3.6.5).
        /// </summary>
        /// <exception cref="ArgumentNullException">On set: the value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">On set: the value holds a <c>null</c> element.</exception>
        /// <exception cref="ImfFileException">On set: a text is not valid Unicode, or holds a control character other than tab.</exception>
        public IReadOnlyList<string> Comments
        {
            get
            {
                var fields = Fields.FindAll(ImfFieldNames.Comments);
                var comments = new string[fields.Count];
                for (var index = 0; index < comments.Length; index++)
                {
                    comments[index] = fields[index].GetDecodedValue(Options);
                }

                return new ReadOnlyCollection<string>(comments);
            }
            set
            {
                var texts = ImfGrammar.CopyElements(value, nameof(value));
                var fields = new ImfHeaderField[texts.Length];
                for (var index = 0; index < fields.Length; index++)
                {
                    fields[index] = ImfHeaderField.FromText(ImfFieldNames.Comments, texts[index]);
                }

                Fields.Set(ImfFieldNames.Comments, fields);
            }
        }

        /// <summary>
        /// Gets or sets the phrases of each Keywords field, in order (RFC 5322 §3.6.5).  A field that is not valid is left out.
        /// </summary>
        /// <exception cref="ArgumentNullException">On set: the value is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">On set: the value holds a <c>null</c> element.</exception>
        public IReadOnlyList<ImfPhraseList> Keywords
        {
            get
            {
                var keywords = new List<ImfPhraseList>();
                foreach (var keywordField in Fields.FindAll(ImfFieldNames.Keywords))
                {
                    if (ImfPhraseList.TryParse(keywordField.ValueBytes.Span, Options, out var phrases)) keywords.Add(phrases);
                }

                return new ReadOnlyCollection<ImfPhraseList>(keywords);
            }
            set
            {
                var lists = ImfGrammar.CopyElements(value, nameof(value));
                var fields = new ImfHeaderField[lists.Length];
                for (var index = 0; index < fields.Length; index++)
                {
                    fields[index] = ImfHeaderField.FromValue(ImfFieldNames.Keywords, lists[index].ToString);
                }

                Fields.Set(ImfFieldNames.Keywords, fields);
            }
        }

        /// <summary>
        /// Gets the blocks of resent fields, in the order of the header (RFC 5322 §3.6.6).  The list is read-only, and doesn't follow later changes to the fields.  To add a block,
        /// add its fields to <see cref="Fields"/>.
        /// </summary>
        /// <remarks>
        /// RFC 5322 gives no mark between blocks, and no order within one.  So a block ends at any other field, or at a field that it already has.
        /// </remarks>
        public IReadOnlyList<ImfResentBlock> ResentBlocks => ImfResentBlock.GetBlocks(Fields, Options);

        /// <summary>
        /// Gets the Return-Path and Received fields, in the order of the header (RFC 5322 §3.6.7).  The list is read-only, and doesn't follow later changes to the fields.  To add
        /// one, add it to <see cref="Fields"/>.
        /// </summary>
        public IReadOnlyList<ImfTraceField> TraceFields => ImfTraceField.GetFields(Fields, Options);

        /// <summary>
        /// Gets the line break of the empty line that ended the header of a message that was read, which is empty if the data ended first; or <c>null</c> if the message
        /// was not read, in which case a writer adds one.
        /// </summary>
        [MaybeNull]
        internal byte[] HeaderEnd { get; set; }

        /// <summary>
        /// Gets the position of the end of the header of a message that was read, or -1 if the message was not read.
        /// </summary>
        internal long HeaderEndBytePosition { get; set; } = -1;

        /// <summary>
        /// Gets the 1-based number of the line at the end of the header of a message that was read, or 0 if the message was not read.
        /// </summary>
        internal long HeaderEndLineNumber { get; set; }

        /// <summary>
        /// Checks the header against RFC 5322 §3.6, without throwing: the required fields, the fields that may appear only once, and the grammar of the body of each field that §3.6
        /// defines.  The options don't change what is reported, except in how a field body is read.
        /// </summary>
        /// <returns>Every problem found, in a read-only list.</returns>
        public IReadOnlyList<ImfDiagnostic> Validate()
        {
            return new ReadOnlyCollection<ImfDiagnostic>(ImfConformance.Check(this, Options));
        }

        private void SetAddressList(string name, [AllowNull] ImfAddressList value)
        {
            var addresses = value;
            Fields.Set(name, addresses is null ? NoFields : new[] { ImfHeaderField.FromValue(name, addresses.ToString) });
        }

        private void SetMessageIds(string name, [AllowNull] IReadOnlyList<ImfMessageId> value)
        {
            if (value is null)
            {
                Fields.Set(name, NoFields);
                return;
            }

            var messageIds = ImfGrammar.CopyElements(value, nameof(value));
            if (messageIds.Length == 0)
            {
                Fields.Set(name, NoFields);
                return;
            }

            var text = new StringBuilder();
            foreach (var messageId in messageIds)
            {
                if (text.Length > 0) text.Append(' ');

                text.Append(messageId);
            }

            var body = text.ToString();
            Fields.Set(name, new[] { ImfHeaderField.FromValue(name, encoding => body) });
        }
    }
}
