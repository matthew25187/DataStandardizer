using System.Text;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Options to configure the behaviour of a vCard reader.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 6350.  Each accommodation for common non-compliant input is a
    /// separate option, off by default.
    /// </remarks>
#if NET5_0_OR_GREATER
    public sealed record VcardOptions
#else
    public sealed class VcardOptions
#endif
    {
        /// <summary>
        /// Gets a flag indicating if a bare line feed is accepted as a line break, both to end a content line and in a fold.
        /// </summary>
        /// <remarks>
        /// RFC 6350 §3.2 and §3.3 require a CRLF sequence.  When <c>false</c> (the default), a bare line feed causes a <see cref="VcardFileException"/>.
        /// A bare carriage return is never a line break.
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
        /// Gets a flag indicating if a byte order mark is accepted, and skipped, at the start of the data.
        /// </summary>
        /// <remarks>
        /// The <c>vcard</c> rule of RFC 6350 §3.3 starts with <c>BEGIN:VCARD</c>, and RFC 3629 §6 says a protocol that mandates UTF-8 should forbid the
        /// signature.  When <c>false</c> (the default), a byte order mark is a character in the first line, which causes a
        /// <see cref="VcardFileException"/>.  A byte order mark never selects the encoding; see <see cref="Encoding"/>.
        /// </remarks>
        public bool AcceptByteOrderMark
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if RFC 6868 caret encoding is decoded on every line, not only on those of a vCard 4.0 card.
        /// </summary>
        /// <remarks>
        /// RFC 6868 updates RFC 6350, and neither RFC 2426 (vCard 3.0) nor vCard 2.1, in which a caret is an ordinary character.  When <c>false</c>
        /// (the default), a parameter value is decoded only from a card's <c>VERSION:4.0</c> line up to and including its <c>END:VCARD</c> line.  So a
        /// line before <c>VERSION</c> or outside a card is not decoded.  When <c>true</c>, every line is decoded, whatever its card says.
        /// </remarks>
        public bool AcceptCaretEncodingInAllVersions
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if the last content line may end without a line break.
        /// </summary>
        /// <remarks>
        /// The <c>contentline</c> rule of RFC 6350 §3.3 ends with CRLF, including on the last line.  When <c>false</c> (the default), a last line
        /// without one causes a <see cref="VcardFileException"/>.
        /// </remarks>
        public bool AcceptMissingFinalLineBreak
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets the encoding for the vCard data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Applies only when the reader is created from a <see cref="System.IO.Stream"/> or file path.  When <c>null</c> (the default), the data is
        /// strict UTF-8, as RFC 6350 §3.1 requires: "There is no way to override this."  A byte sequence that is not valid UTF-8 causes a
        /// <see cref="VcardFileException"/>, and a byte order mark doesn't change the encoding.
        /// </para>
        /// <para>
        /// Setting another encoding relaxes that rule, for data known to break it.  The encoding is then used as it is: <see cref="Encoding.UTF8"/>
        /// replaces invalid data instead of rejecting it, and an encoding with a preamble has it removed from the start of the data, whatever
        /// <see cref="AcceptByteOrderMark"/> says.  A decoder that throws <see cref="System.Text.DecoderFallbackException"/> gives a
        /// <see cref="VcardFileException"/>.
        /// </para>
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public Encoding? Encoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }
#else
        public Encoding Encoding { get; set; }
#endif

        /// <summary>
        /// Gets the maximum length, in characters, of a logical (unfolded) content line, not counting its line break.
        /// </summary>
        /// <remarks>
        /// RFC 6350 sets no limit, so the default is <c>null</c>, meaning no limit.  The reader keeps each logical line in memory, so setting a
        /// limit is recommended when reading untrusted data.  A longer line causes a <see cref="VcardFileException"/>.
        /// </remarks>
        public int? MaxLineLength
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }
    }
}
