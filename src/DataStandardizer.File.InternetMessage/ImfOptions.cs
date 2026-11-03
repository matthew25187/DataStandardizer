using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.InternetMessage
{
    /// <summary>
    /// Options to configure the behaviour of an Internet message reader.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 5322 and RFC 6532.  Each accommodation for common non-compliant
    /// input is a separate option, off by default.
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
        /// Gets a flag indicating if a bare line feed is accepted as a line break: at the end of a field, in a fold, and as the empty line that
        /// ends the header section.
        /// </summary>
        /// <remarks>
        /// RFC 5322 §2.1 requires lines to end in CRLF, and §2.2 allows CR and LF in a field only as part of a fold.  When <c>false</c> (the
        /// default), a bare line feed causes an <see cref="ImfFileException"/>.  A bare carriage return is never accepted.
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
        /// Gets the resolver that <see cref="HeaderFallbackCharset"/> is resolved through.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="MimeCharsetResolver.Default"/>.  It is used only when <see cref="AllowEightBitHeaders"/> is <c>true</c>.
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
    }
}
