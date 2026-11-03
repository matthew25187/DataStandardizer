using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Options to configure the behaviour of an Internet message reader or writer.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 5322, RFC 6532, RFC 2045 and RFC 2046.  Each accommodation for common non-compliant
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
        /// The default value of <see cref="MaxHeaderSectionLength"/>, which is 1 MiB.
        /// </summary>
        public const int DefaultMaxHeaderSectionLength = 1024 * 1024;

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
        /// §3.3 are always enforced, because a value that breaks them, such as 30 February, is not a date.
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
        /// Gets the resolver for character sets: <see cref="HeaderFallbackCharset"/>, those named in the extended parameters of a Content-Type
        /// field (RFC 2231 §4), and those of RFC 2047 encoded words.
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
