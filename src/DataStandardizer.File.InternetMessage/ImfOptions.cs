using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Options to configure the behaviour of an Internet message reader or writer.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 5322, RFC 6532, RFC 2045, RFC 2046 and RFC 4155.  Each accommodation for common non-compliant
    /// input is a separate option, off by default.  A writer always writes what the standards allow, so the accommodations affect it only where noted.
    /// </remarks>
#if NET5_0_OR_GREATER
    public sealed record ImfOptions
#else
    public sealed class ImfOptions
#endif
    {
        /// <summary>
        /// The default value of <see cref="MaxHeaderFieldCount"/>.
        /// </summary>
        public const int DefaultMaxHeaderFieldCount = 1000;

        /// <summary>
        /// The default value of <see cref="MaxEntitySize"/>, which is 256 MiB.
        /// </summary>
        public const int DefaultMaxEntitySize = 256 * 1024 * 1024;

        /// <summary>
        /// The default value of <see cref="MaxHeaderSectionLength"/>, which is 1 MiB.
        /// </summary>
        public const int DefaultMaxHeaderSectionLength = 1024 * 1024;

        /// <summary>
        /// The default value of <see cref="MaxMessageCount"/>.
        /// </summary>
        public const int DefaultMaxMessageCount = 1000000;

        /// <summary>
        /// The default value of <see cref="MaxNestingDepth"/>.
        /// </summary>
        public const int DefaultMaxNestingDepth = 100;

        /// <summary>
        /// The default value of <see cref="MaxPartCount"/>.
        /// </summary>
        public const int DefaultMaxPartCount = 10000;

        /// <summary>
        /// Gets a flag indicating if a bare line feed is accepted as a line break: at the end of a field, in a fold, and as the empty line that
        /// ends the header section.
        /// </summary>
        /// <remarks>
        /// RFC 5322 §2.1 requires lines to end in CRLF, and §2.2 allows CR and LF in a field only as part of a fold.  When <c>false</c> (the
        /// default), a bare line feed causes an <see cref="ImfFileException"/>.  A bare carriage return is never accepted.  With an
        /// <see cref="ImfEntityReader"/>, the same goes for the line breaks of a multipart delimiter (RFC 2046 §5.1.1) and of a quoted-printable body
        /// being decoded (RFC 2045 §6.7); when <c>false</c>, a delimiter with a bare line feed is content.  A writer never adds a bare line feed, but
        /// with this option it accepts one in a raw field given to <see cref="ImfHeaderWriter.WriteRawField"/> or <see cref="ImfEntitySource.AddRawField"/>,
        /// which it copies as it is.
        /// </remarks>
        public bool AcceptBareLineFeed
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if an <see cref="ImfEntityWriter"/> may give a body the <c>binary</c> transfer encoding, when it has not been told
        /// which to use.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §2.9 and §6.2 define the encoding, but a transport can carry it only if it supports RFC 3030 BINARYMIME, so when <c>false</c> (the default)
        /// such a body is encoded instead.  This setting doesn't relax a rule of the standard: it says what the destination can carry.
        /// </remarks>
        public bool AllowBinaryTransferEncoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a header may have more than one of a field that RFC 5322 §3.6 allows at most once.
        /// </summary>
        /// <remarks>
        /// The table in RFC 5322 §3.6 allows Date, From, Sender, Reply-To, To, Cc, Bcc, Message-ID, In-Reply-To, References and Subject at most once.  When <c>false</c> (the default),
        /// an <see cref="ImfReader"/> or <see cref="ImfWriter"/> throws an <see cref="ImfFileException"/> for a second one.  When <c>true</c>, it records an
        /// <see cref="ImfDiagnostic"/> instead, and the accessors of <see cref="ImfMessage"/> use the first.
        /// </remarks>
        public bool AllowCardinalityViolations
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if an <see cref="ImfEntityWriter"/> may give a body the <c>8bit</c> transfer encoding, when it has not been told which to
        /// use.
        /// </summary>
        /// <remarks>
        /// RFC 2045 §2.8 and §6.2 define the encoding, but a transport can carry it only if it supports RFC 6152 8BITMIME, so when <c>false</c> (the default)
        /// such a body is encoded instead.  This setting doesn't relax a rule of the standard: it says what the destination can carry.
        /// </remarks>
        public bool AllowEightBitTransferEncoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a message of an mbox file may hold octets above 127.
        /// </summary>
        /// <remarks>
        /// RFC 4155 Appendix A requires seven-bit characters, with eight-bit data converted to a seven-bit form.  When <c>false</c> (the default), an octet above 127 causes an
        /// <see cref="ImfFileException"/> from an <see cref="MboxReader"/> or <see cref="MboxWriter"/>.  When <c>true</c>, an <see cref="ImfDiagnostic"/> is recorded instead, once for each
        /// message.  Many mailboxes kept on disk hold such messages.
        /// </remarks>
        public bool AllowEightBitMboxContent
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a field body whose bytes are not valid UTF-8 is accepted.
        /// </summary>
        /// <remarks>
        /// RFC 6532 §3.2 extends header field bodies to UTF-8 only.  When <c>false</c> (the default), any other byte above 127, such as raw
        /// Latin-1 or Shift_JIS in an old message, causes an <see cref="ImfFileException"/>.  When <c>true</c>, the field is read, its
        /// <see cref="ImfHeaderFieldView.NeedsHeaderFallbackCharset"/> is <c>true</c>, and <see cref="ImfHeaderFieldView.GetValue"/> decodes it
        /// with <see cref="HeaderFallbackCharset"/>.  Field names are not affected: RFC 6532 leaves them as printable US-ASCII, so a byte above
        /// 127 in a name is always rejected.
        /// </remarks>
        public bool AllowEightBitHeaders
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a date-time with a day of the week that doesn't match its date is accepted.
        /// </summary>
        /// <remarks>
        /// RFC 5322 §3.3 requires the day of the week, if there is one, to be the day implied by the date.  When <c>false</c> (the default),
        /// <see cref="ImfDateTime.Parse(string, ImfOptions)"/> throws a <see cref="System.FormatException"/> for one that isn't.  When <c>true</c>, the date
        /// decides: <see cref="ImfDateTime.DayOfWeek"/> is the day implied by the date, and that is the day that gets written.  The other rules of
        /// §3.3 are always enforced, because a value that breaks them, such as 30 February, is not a date.  The same goes for the dates of an
        /// <see cref="ImfContentDisposition"/>.  <see cref="ImfMimeFields.ParseContentLanguage(string, ImfOptions)"/> rejects a language tag that isn't well-formed
        /// (RFC 3282 §2).  When <c>true</c>, it leaves that tag out of the list.  An <see cref="ImfReader"/> or <see cref="ImfWriter"/> throws an <see cref="ImfFileException"/> if the
        /// body of a field that RFC 5322 §3.6 defines breaks its grammar, or a Sender or Resent-Sender group has more than one mailbox (RFC 6854 §2.1).  When <c>true</c>, it records an
        /// <see cref="ImfDiagnostic"/> instead, the field stays as it is, and the accessor for it gives <c>null</c>, unless the value is only a day of the week that doesn't match.  The
        /// same goes for an <see cref="MhtmlDocument"/> and a Content-Location field that breaks RFC 2557 §4.1 or §4.4.1.
        /// </remarks>
        public bool AllowInvalidValues
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if an <see cref="MhtmlDocument"/>, <see cref="MhtmlReader"/> or <see cref="MhtmlWriter"/> accepts a <c>multipart/related</c> entity that breaks the rules
        /// of its structure.
        /// </summary>
        /// <remarks>
        /// RFC 2387 §3.1 requires a <c>type</c> parameter that gives the media type of the root part, and §3.2 has the <c>start</c> parameter give the Content-ID of that part, or otherwise
        /// the first part be the root, so there has to be one.  RFC 2557 §7 requires each part to have a different Content-ID, and a Content-Location that resolves to a different URL.
        /// RFC 2045 §7 requires a Content-ID to be unique outside a <c>multipart/related</c> entity too, so two parts outside one with the same Content-ID count here.  When <c>false</c>
        /// (the default), breaking one of these causes an <see cref="ImfFileException"/>.  When <c>true</c>, it records an <see cref="ImfDiagnostic"/> instead, the first part is the root
        /// if <c>start</c> names none, the entity is its own root if it has no parts, and the first of the parts with the same label is the one that is found.  Word saves web pages with
        /// no <c>type</c> parameter.
        /// <see cref="ImfReader"/> and <see cref="ImfWriter"/> don't check these rules, and so don't use this option.
        /// </remarks>
        public bool AllowInvalidRelatedStructure
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if an <see cref="MboxReader"/> accepts a Content-Length field that is missing or wrong, in the <see cref="MboxVariant.Mboxcl"/> and
        /// <see cref="MboxVariant.Mboxcl2"/> variants.
        /// </summary>
        /// <remarks>
        /// These variants find the end of a message from its Content-Length field.  When <c>false</c> (the default), a field that is missing, that is not a single number, or that doesn't lead
        /// to an empty line and then a separator line or the end of the file, causes an <see cref="ImfFileException"/>.  When <c>true</c>, the field is not relied on: a message ends at the next
        /// separator line that follows an empty line, and an <see cref="ImfDiagnostic"/> is recorded if the field doesn't give the length found.  So a body of the
        /// <see cref="MboxVariant.Mboxcl2"/> variant that holds an empty line and then what looks like a separator line is split there, even if its field was right.
        /// </remarks>
        public bool AllowInvalidMboxContentLength
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a separator line of an mbox file may differ from the form that RFC 4155 Appendix A requires.
        /// </summary>
        /// <remarks>
        /// RFC 4155 Appendix A requires an <c>addr-spec</c> and a UTC date and time in the form of <c>ctime</c>.  When <c>false</c> (the default), an <see cref="MboxReader"/> throws an
        /// <see cref="ImfFileException"/> for a first line in any other form, and takes any later line in another form as part of the message, as the standard prescribes no way to quote one, with
        /// an <see cref="ImfDiagnostic"/>.  An <see cref="MboxWriter"/> throws for an <see cref="MboxEnvelope"/> that was read in another form.  When <c>true</c>, a line is a separator line if it
        /// has a sender and a date and time, with the day not padded, no seconds, a time zone before or after the year, or more white space, as Thunderbird and Gmail write, and an
        /// <see cref="ImfDiagnostic"/> is recorded.  <see cref="MboxEnvelope.Sender"/> or <see cref="MboxEnvelope.Date"/> is <c>null</c> if it can't be understood.
        /// </remarks>
        public bool AllowInvalidMboxSeparator
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if <see cref="ImfEntity.GetText"/> accepts octets that are not valid in the character set of the text.
        /// </summary>
        /// <remarks>
        /// RFC 2046 §4.1.2 has the <c>charset</c> parameter say what character set the text is in, with US-ASCII as the default, so other octets are not text of that type.  When
        /// <c>false</c> (the default), they cause an <see cref="ImfFileException"/>.  When <c>true</c>, each is read as U+FFFD, which helps with old messages that send 8-bit text
        /// and name no character set.
        /// </remarks>
        public bool AllowInvalidTextOctets
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a line longer than 998 octets is accepted.
        /// </summary>
        /// <remarks>
        /// RFC 5322 §2.1.1 limits each line to 998 octets, not counting the CRLF.  When <c>false</c> (the default), a longer line causes an
        /// <see cref="ImfFileException"/>.  The limit is on each physical line, so a folded field may be longer once unfolded.  When <c>true</c>,
        /// a line is still limited by <see cref="MaxHeaderSectionLength"/>.
        /// </remarks>
        public bool AllowLongLines
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if RFC 2047 encoded words that don't follow the rules are decoded.
        /// </summary>
        /// <remarks>
        /// When <c>false</c> (the default), a word that breaks a rule of RFC 2047 §2, §4, §5 or §6.3 stays as ordinary text (RFC 2047 §6.1 and §6.2).  When <c>true</c>,
        /// each of the accommodations of <see cref="MimeEncodedWordParseOptions"/> applies: a word longer than the 75 characters of §2
        /// (<see cref="MimeEncodedWordParseOptions.AllowLongWords"/>), a word not separated from the text next to it, which §5 requires
        /// (<see cref="MimeEncodedWordParseOptions.AllowEmbeddedWords"/>), a character set that <see cref="CharsetResolver"/> can't resolve, which §6.2 lets a reader
        /// show as it can (<see cref="MimeEncodedWordParseOptions.AllowUnknownCharsets"/>), lower-case hexadecimal digits, which §4.2 doesn't allow
        /// (<see cref="MimeEncodedWordParseOptions.AllowLowercaseHexDigits"/>), octets that aren't valid in the character set, which §6.3 calls incorrectly formed
        /// (<see cref="MimeEncodedWordParseOptions.AllowInvalidOctets"/>), and a character split between words, which §5 doesn't allow
        /// (<see cref="MimeEncodedWordParseOptions.AllowSplitCharacters"/>).
        /// </remarks>
        public bool AllowMalformedEncodedWords
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if the parameters of a Content-Type or Content-Disposition field are accepted when they don't follow the rules.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>false</c> (the default), a parameter that breaks a rule below makes the field invalid.  When <c>true</c>, each of the accommodations of
        /// <see cref="MimeMediaTypeParseOptions"/> for RFC 2231 applies: sections that break the numbering of §3 (<see cref="MimeMediaTypeParseOptions.AllowNonstandardSections"/>),
        /// an extended value that breaks the grammar of §7 (<see cref="MimeMediaTypeParseOptions.AllowNonstandardExtendedValues"/>), a character set that
        /// <see cref="CharsetResolver"/> can't resolve, or no character set for octets above 127, which §4 doesn't allow (<see cref="MimeMediaTypeParseOptions.AllowUnknownCharsets"/>),
        /// and octets that aren't valid in the character set (<see cref="MimeMediaTypeParseOptions.AllowInvalidOctets"/>).
        /// </para>
        /// <para>
        /// The other accommodations are for <see cref="ImfMimeFields.ParseContentType(string, ImfOptions)"/> and <see cref="ImfContentDisposition.Parse(string, ImfOptions)"/>.
        /// RFC 2047 §5 doesn't allow an encoded word in a parameter.  When <c>true</c>, one is accepted, and it is decoded in the <c>filename</c> of a Content-Disposition field and
        /// the <c>name</c> of a Content-Type field, and stays as written in any other parameter.  A semicolon may end a Content-Type field, which RFC 2045 §5.1 doesn't allow.  A date or
        /// size in a Content-Disposition field that breaks RFC 2183 §2 stays as a parameter, and its property is <c>null</c>.
        /// </para>
        /// <para>
        /// An <see cref="ImfEntityReader"/> uses only the accommodations for RFC 2231.  It doesn't look at other parameters than those it needs to find the parts of a message.  An
        /// <see cref="ImfEntityWriter"/> uses them only to read a Content-Type field added to an entity, which it copies as it is, and never to write a field of its own.
        /// </para>
        /// </remarks>
        public bool AllowMalformedParameters
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a multipart entity with no close delimiter is accepted.
        /// </summary>
        /// <remarks>
        /// RFC 2046 §5.1.1 ends every multipart body with a close delimiter.  When <c>false</c> (the default), an
        /// <see cref="ImfEntityReader"/> throws an <see cref="ImfFileException"/> if the data ends, or a delimiter of an enclosing multipart
        /// entity is found, first.  When <c>true</c>, the multipart entity ends there.  One with no delimiter at all then has no parts, and its
        /// whole body is its preamble.
        /// </remarks>
        public bool AllowMissingBoundaryTerminator
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if an <see cref="MboxReader"/> accepts a message that isn't ended by an empty line.
        /// </summary>
        /// <remarks>
        /// RFC 4155 Appendix A ends each message with an empty line.  When <c>false</c> (the default), a separator line or the end of the file that doesn't follow one causes an
        /// <see cref="ImfFileException"/>.  When <c>true</c>, the message ends there, and an <see cref="ImfDiagnostic"/> is recorded.
        /// </remarks>
        public bool AllowMissingMboxBlankLine
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if an mbox file may have a carriage return and a line feed as a line break.
        /// </summary>
        /// <remarks>
        /// RFC 4155 Appendix A requires a single line feed, and doesn't allow a carriage return and a line feed.  When <c>false</c> (the default), an <see cref="MboxReader"/> throws an
        /// <see cref="ImfFileException"/> for a first separator line that ends in them, or a line of a message that does, and an <see cref="MboxWriter"/> writing line feeds does the same for a
        /// line of a message that ends in them.  When <c>true</c>, an <see cref="ImfDiagnostic"/> is recorded instead.  A reader then reads a file whose first separator line ends in them with
        /// that line break throughout, and in any other file the carriage return is part of its line.  An <see cref="MboxWriter"/> writes this line break only when
        /// <see cref="MboxLineBreak.CarriageReturnLineFeed"/> is given to it, whatever this option says.
        /// </remarks>
        public bool AllowMboxCarriageReturnLineFeed
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if an <see cref="MboxReader"/> accepts a file that doesn't start with a separator line.
        /// </summary>
        /// <remarks>
        /// RFC 4155 Appendix A has a separator line immediately before each message.  When <c>false</c> (the default), a file with any other first line causes an
        /// <see cref="ImfFileException"/>.  When <c>true</c>, everything before the first separator line is skipped, and an <see cref="ImfDiagnostic"/> is recorded.
        /// </remarks>
        public bool AllowMissingMboxSeparator
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a header may lack a field that RFC 5322 requires.
        /// </summary>
        /// <remarks>
        /// RFC 5322 §3.6 requires Date and From.  §3.6.2 and RFC 6854 §2.1 require Sender when From has more than one mailbox, and §3.6.6 applies the same rules to Resent-Date,
        /// Resent-From and Resent-Sender in each block of resent fields.  When <c>false</c> (the default), an <see cref="ImfReader"/> or <see cref="ImfWriter"/> throws an
        /// <see cref="ImfFileException"/> for a message without one.  When <c>true</c>, it records an <see cref="ImfDiagnostic"/> instead.
        /// </remarks>
        public bool AllowMissingRequiredFields
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets the resolver for character sets: <see cref="HeaderFallbackCharset"/>, those named in the extended parameters of a Content-Type or
        /// Content-Disposition field (RFC 2231 §4), and those of RFC 2047 encoded words.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="MimeCharsetResolver.Default"/>.  An <see cref="ImfEntityReader"/> always needs it.  An
        /// <see cref="ImfHeaderReader"/> needs it when <see cref="AllowEightBitHeaders"/> is <c>true</c>, and for
        /// <see cref="ImfHeaderFieldView.GetDecodedValue"/>.
        /// </remarks>
        public IMimeCharsetResolver CharsetResolver
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = MimeCharsetResolver.Default;

        /// <summary>
        /// Gets a flag indicating if an <see cref="ImfEntityReader"/> decodes bodies as it reads them.
        /// </summary>
        /// <remarks>
        /// When <c>false</c> (the default), body chunks are the raw bytes of the message.  When <c>true</c>, a body with a Content-Transfer-Encoding
        /// of <c>quoted-printable</c> or <c>base64</c> is decoded (RFC 2045 §6.7 and §6.8), and one with an extension mechanism is left as it is,
        /// with an <see cref="ImfDiagnostic"/>.  Data that is not valid in its encoding causes an <see cref="ImfFileException"/>.
        /// </remarks>
        public bool DecodeTransferEncoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets the character set that decodes a field body that is not valid UTF-8, when <see cref="AllowEightBitHeaders"/> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// The default is ISO-8859-1, which can decode any bytes, and gives the same result on every runtime.  A reader's constructor throws
        /// <see cref="System.ArgumentException"/> if <see cref="CharsetResolver"/> can't resolve the character set.  It is used only when
        /// <see cref="AllowEightBitHeaders"/> is <c>true</c>.
        /// </remarks>
        public MimeCharset HeaderFallbackCharset
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = MimeCharset.Iso88591;

        /// <summary>
        /// Gets how a writer writes header text that is not US-ASCII.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="ImfHeaderEncoding.EncodedWords"/> (RFC 2047).  <see cref="ImfHeaderWriter.WriteTextField"/> and
        /// <see cref="ImfEntitySource.AddTextField"/> write text that is not US-ASCII as encoded words, as <see cref="ImfHeaderText.FormatUnstructured"/> describes.  A
        /// field body is written as it is given, so with that default it is refused if it is not US-ASCII.  With <see cref="ImfHeaderEncoding.Utf8"/>, valid UTF-8 is
        /// written as it is (RFC 6532).
        /// </remarks>
        public ImfHeaderEncoding HeaderEncoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets the maximum size, in octets, of the body that an <see cref="ImfReader"/> holds in memory, and of the content given to <see cref="ImfEntity.SetContent"/>.
        /// </summary>
        /// <remarks>
        /// RFC 5322 sets no limit.  This one guards against hostile input.  The default is <see cref="DefaultMaxEntitySize"/>, and the value must be at least 1.  One more octet
        /// causes an <see cref="ImfFileException"/>, or an <see cref="System.ArgumentException"/> from <see cref="ImfEntity.SetContent"/>.  A part of a message that was read is within
        /// the body, so it can't pass the limit, and decoding it can only make it shorter.  The readers and writers that don't hold a whole body in memory don't use it.
        /// </remarks>
        public int MaxEntitySize
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = DefaultMaxEntitySize;

        /// <summary>
        /// Gets the maximum number of fields in a header section.
        /// </summary>
        /// <remarks>
        /// RFC 5322 sets no limit.  This one guards against hostile input.  The default is <see cref="DefaultMaxHeaderFieldCount"/>, and the
        /// value must be at least 1.  One more field causes an <see cref="ImfFileException"/>.
        /// </remarks>
        public int MaxHeaderFieldCount
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = DefaultMaxHeaderFieldCount;

        /// <summary>
        /// Gets the maximum length, in octets, of a header section, counting its line breaks and the empty line that ends it.
        /// </summary>
        /// <remarks>
        /// RFC 5322 sets no limit.  This one guards against hostile input, as a reader holds a whole field in memory.  The default is
        /// <see cref="DefaultMaxHeaderSectionLength"/>, and the value must be at least 1.  One more octet causes an
        /// <see cref="ImfFileException"/>.
        /// </remarks>
        public int MaxHeaderSectionLength
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = DefaultMaxHeaderSectionLength;

        /// <summary>
        /// Gets the maximum depth of the entities of a message, in which the message itself is at depth 0.
        /// </summary>
        /// <remarks>
        /// RFC 2046 sets no limit.  This one guards against hostile input.  Each part of a multipart entity, and each embedded message, is one level
        /// deeper than the entity that holds it.  The default is <see cref="DefaultMaxNestingDepth"/>, and the value must be at least 1.  An entity
        /// one level deeper causes an <see cref="ImfFileException"/>.
        /// </remarks>
        public int MaxNestingDepth
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = DefaultMaxNestingDepth;

        /// <summary>
        /// Gets the maximum number of messages that an <see cref="MboxReader"/> reads from a file.
        /// </summary>
        /// <remarks>
        /// RFC 4155 sets no limit.  This one guards against hostile input.  The default is <see cref="DefaultMaxMessageCount"/>, and the value must be at least 1.  One more message causes an
        /// <see cref="ImfFileException"/>.  The other limits apply to each message.
        /// </remarks>
        public int MaxMessageCount
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = DefaultMaxMessageCount;

        /// <summary>
        /// Gets the maximum number of entities in a message, not counting the message itself.
        /// </summary>
        /// <remarks>
        /// RFC 2046 sets no limit.  This one guards against hostile input.  Every part of a multipart entity and every embedded message counts, at
        /// any depth.  The default is <see cref="DefaultMaxPartCount"/>, and the value must be at least 1.  One more entity causes an
        /// <see cref="ImfFileException"/>.
        /// </remarks>
        public int MaxPartCount
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = DefaultMaxPartCount;
    }
}
