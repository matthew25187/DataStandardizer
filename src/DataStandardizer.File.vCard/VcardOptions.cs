using System.Text;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Options to configure the behaviour of a vCard reader or writer.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 6350.  Each accommodation for common non-compliant input or output is
    /// a separate option, off by default.
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
        /// A bare carriage return is never a line break.  Ignored by a writer; see <see cref="LineBreak"/>.
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
        /// <see cref="VcardFileException"/>.  A byte order mark never selects the encoding; see <see cref="Encoding"/>.  Ignored by a writer, which doesn't
        /// write one unless <see cref="Encoding"/> has a preamble.
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
        /// line before <c>VERSION</c> or outside a card is not decoded.  When <c>true</c>, every line is decoded, whatever its card says.  Ignored by a
        /// writer, which only writes caret encoding in a vCard 4.0 card; see <see cref="AllowLossyVersionConversion"/>.
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
        /// without one causes a <see cref="VcardFileException"/>.  Ignored by a writer, which always ends the last line with a line break.
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
        /// Gets a flag indicating if a writer may lose data that the version of the card it is writing can't hold.
        /// </summary>
        /// <remarks>
        /// <para>
        /// RFC 6868 caret encoding updates RFC 6350 only.  The <c>param-value</c> rule of RFC 2426 §4 (vCard 3.0), and vCard 2.1, have no way to write a double
        /// quote or a line break in a parameter value, and neither does a line outside a card or before its <c>VERSION</c> line, whose version isn't known.
        /// When <c>false</c> (the default), writing such a value causes a <see cref="VcardFileException"/>.
        /// </para>
        /// <para>
        /// When <c>true</c>, the line is written without those characters: a double quote is left out, and a line break, including a CRLF, becomes one space.  The line
        /// still follows the grammar of its version, but reading it back gives a different value.  Ignored by a reader.
        /// </para>
        /// </remarks>
        public bool AllowLossyVersionConversion
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
        /// Applies only when the reader or writer is created from a <see cref="System.IO.Stream"/> or file path.  When <c>null</c> (the default), the data
        /// is strict UTF-8, as RFC 6350 §3.1 requires: "There is no way to override this."  A byte sequence that is not valid UTF-8 causes a
        /// <see cref="VcardFileException"/>, and a byte order mark doesn't change the encoding.  A writer writes strict UTF-8 with no byte order mark.
        /// </para>
        /// <para>
        /// Setting another encoding relaxes that rule, for data known to break it.  The encoding is then used as it is: <see cref="Encoding.UTF8"/>
        /// replaces invalid data instead of rejecting it, and an encoding with a preamble has it removed from the start of the data, whatever
        /// <see cref="AcceptByteOrderMark"/> says.  A decoder that throws <see cref="System.Text.DecoderFallbackException"/> gives a
        /// <see cref="VcardFileException"/>.  A writer writes the encoding's preamble, if it has one, at the start of the data.
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
        /// Gets the length, in octets, at which a writer folds content lines.
        /// </summary>
        /// <remarks>
        /// The default is 75, the width RFC 6350 §3.2 says lines should be folded to.  Choosing a longer one relaxes that rule.  Lengths are measured in octets of
        /// the UTF-8 encoding, not counting the line break, and a fold never splits a character, as RFC 6350 §3.2 requires.  The minimum is 4, the longest
        /// UTF-8 sequence; a continuation line that starts with a 4-octet character is then one octet longer, so that it holds at least one character.  The
        /// lines of a vCard 2.1 card are never folded.  Ignored by a reader.
        /// </remarks>
        public int FoldLength
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 75;

        /// <summary>
        /// Gets the white space character a writer puts at the start of each continuation line of a folded content line.
        /// </summary>
        /// <remarks>
        /// Either a space (the default) or a horizontal tab, the two characters RFC 6350 §3.2 allows.  Ignored by a reader, which accepts both.
        /// </remarks>
        public char FoldWhitespace
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = ' ';

        /// <summary>
        /// Gets the line break a writer ends each physical line with, including the lines of a fold.
        /// </summary>
        /// <remarks>
        /// RFC 6350 §3.2 and §3.3 require a CRLF sequence, which is the default.  A bare line feed (<c>"\n"</c>) relaxes that rule, for consumers that expect it; no other
        /// value is accepted.  The line break never depends on <see cref="System.Environment.NewLine"/>.  Ignored by a reader; see <see cref="AcceptBareLineFeed"/>.
        /// </remarks>
        public string LineBreak
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = "\r\n";

        /// <summary>
        /// Gets the maximum length, in characters, of a logical (unfolded) content line, not counting its line break.
        /// </summary>
        /// <remarks>
        /// RFC 6350 sets no limit, so the default is <c>null</c>, meaning no limit.  The reader keeps each logical line in memory, so setting a
        /// limit is recommended when reading untrusted data.  A longer line causes a <see cref="VcardFileException"/>, and so does a line with more
        /// folds than this, since the reader also keeps track of where each fold was.  Ignored by a writer.
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
