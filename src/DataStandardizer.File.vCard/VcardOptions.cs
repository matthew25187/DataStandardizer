using System.Text;
using DataStandardizer.Communication.Mime;

namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// Options to configure the behaviour of a vCard reader or writer.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 6350 and its extensions, with their errata.  Each accommodation for common non-compliant input or output is
    /// a separate option, off by default.  A vCard 2.1 card follows its own specification, without needing an option.
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
        /// Gets a flag indicating if a value in a vCard 2.1 card may hold characters outside US-ASCII without <c>ENCODING=8BIT</c>.
        /// </summary>
        /// <remarks>
        /// The vCard 2.1 specification §2.1.5 makes 7-bit the default encoding, but some writers leave the parameter out.  When <c>false</c> (the default), decoding
        /// such a value, with <see cref="VcardContentLine.GetDecodedText()"/>, causes a <see cref="VcardFileException"/>.  When <c>true</c>, it is decoded by its
        /// <c>CHARSET</c>, which must still be able to hold it.  Ignored by a writer.
        /// </remarks>
        public bool AcceptEightBitWithoutEncoding
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if the lexical forms of vCard 2.1 are accepted on every line, and if other non-standard forms that are common in cards are accepted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A vCard 2.1 card, from its <c>VERSION:2.1</c> line up to and including its <c>END:VCARD</c> line, has the forms its specification defines without this option.
        /// RFC 2426 and RFC 6350 define none of them, so when <c>false</c> (the default), a parameter with no name causes a <see cref="VcardFileException"/>, a line
        /// ending in <c>=</c> is complete, and <c>ENCODING=QUOTED-PRINTABLE</c> or <c>CHARSET</c> is an unknown parameter, which doesn't change how the value is read.
        /// That goes for a line before the <c>VERSION</c> line or outside a card too, since its version isn't known.
        /// </para>
        /// <para>
        /// When <c>true</c>, on every line, a parameter may have no name, as in vCard 2.1 §2.9; a quoted-printable value may have soft line breaks and a
        /// <c>BASE64</c> value continues to an empty line, as in vCard 2.1 §2.1.3 and §2.9; and <c>ENCODING</c> and <c>CHARSET</c> apply.  In addition, in any card, it
        /// relaxes these rules:
        /// </para>
        /// <list type="bullet">
        /// <item>vCard 2.1 §2.9 has only a <c>knowntype</c> or, by the example in §2.6.3, an encoding as a parameter with no name.  Any other is accepted, as a
        /// <c>VALUE</c> or <c>CHARSET</c> if it is one, otherwise as a <c>TYPE</c>.</item>
        /// <item>RFC 6350 §3.4 has only four escape sequences.  In <see cref="VcardContentLine.GetTextValue"/>, a backslash before any other character gives that
        /// character, and a backslash at the end of the value stays.</item>
        /// </list>
        /// <para>
        /// Ignored by a writer, which writes the forms of vCard 2.1 only in a vCard 2.1 card.
        /// </para>
        /// <para>
        /// <see cref="VcardReader"/> checks that a vCard 4.0 card has no <c>ENCODING</c> or <c>CHARSET</c> parameter, which RFC 6350 removed.  When <c>true</c>, it accepts them, and the
        /// typed members of <see cref="VcardCard"/> decode the value.  <see cref="VcardWriter"/> refuses to write such a card, so remove them first.
        /// </para>
        /// </remarks>
        public bool AcceptLegacySyntax
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if <see cref="VcardReader"/> reads a property named <c>ORG-URI</c> as <c>ORG-DIRECTORY</c>.
        /// </summary>
        /// <remarks>
        /// RFC 6715 §2.4 defines <c>ORG-DIRECTORY</c>, and IANA registers that name.  The RFC as published also says <c>ORG-URI</c> in §3.1 and §5, which its errata 3340 and 3341 correct.  When
        /// <c>false</c> (the default), <c>ORG-URI</c> is a property nothing defines: it is kept, and <see cref="VcardCard.ExtensionProperties"/> gives it.  When <c>true</c>, its name is changed
        /// to <c>ORG-DIRECTORY</c> as it is read, so it is judged by the rules of that property, <see cref="VcardCard.OrganizationDirectories"/> gives it, and a writer writes that name.  The
        /// content line reader and writer ignore this, and so does a writer given a line named <c>ORG-URI</c>.
        /// </remarks>
        public bool AcceptOrgUriProperty
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if <see cref="VcardReader"/> accepts a comma with no backslash in a URI value of a vCard 4.0 card.
        /// </summary>
        /// <remarks>
        /// RFC 6350 §3.4 says a comma in a value "MUST be escaped", and its verified errata 3845 and 3846 apply that to URI values, as RFC 6474's errata 4557 and 4559 do for its own.  So
        /// <c>GEO:geo:37.386013\,-122.082932</c> is valid, and when <c>false</c> (the default) <c>GEO:geo:37.386013,-122.082932</c> isn't, though the RFC still prints some values that way
        /// and many writers write them.  When <c>true</c>, the comma is escaped as the line is read, so the typed members of <see cref="VcardCard"/> give the value, and a writer writes it
        /// escaped.  It applies to a property whose value type is URI, and to the URI of <c>CLIENTPIDMAP</c>, and not to a line with <c>ENCODING</c> or <c>CHARSET</c>.  The content line
        /// reader and writer ignore this, and so does a writer given a line with such a comma, which it refuses unless <see cref="AllowInvalidValues"/> is set.
        /// </remarks>
        public bool AcceptUnescapedCommasInUris
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a fold in a vCard 2.1 card is unfolded by the rule of RFC 6350 §3.2 rather than its own.
        /// </summary>
        /// <remarks>
        /// By the vCard 2.1 specification §2.1.3, as in RFC 822 §3.1.1, a line break followed by white space is equivalent to the white space, so when <c>false</c> (the
        /// default), only the line break is removed.  Many writers rely on the later rule, which removes one white space character as well, and that is what <c>true</c>
        /// does.  Either way a value folded by the other rule reads differently.  It doesn't affect a quoted-printable soft line break or a <c>BASE64</c> value.  Ignored
        /// by a writer, which doesn't fold a vCard 2.1 card.
        /// </remarks>
        public bool AcceptRfc6350FoldsInVersion21
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a writer writes each <c>TYPE</c> value of a vCard 2.1 card with the parameter's name, as <c>TEL;TYPE=WORK;TYPE=VOICE:</c>.
        /// </summary>
        /// <remarks>
        /// The vCard 2.1 specification §2.9 allows a <c>knowntype</c>, such as <c>WORK</c>, with or without <c>TYPE=</c>.  When <c>false</c> (the default), one is written alone, as
        /// <c>TEL;WORK;VOICE:</c>, which is the form of every example in the specification and the one that the most readers of vCard 2.1 understand.  When <c>true</c>, it is written
        /// with its name.  Either way a <c>TYPE</c> that isn't a <c>knowntype</c>, such as <c>X-CUSTOM</c>, is written with its name, and so is every other parameter.  Ignored by a
        /// reader, which reads both.
        /// </remarks>
        public bool WriteNamedTypesInVersion21
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
        /// Gets a flag indicating if a card may break the cardinality rules of RFC 6350 §6 and its extensions.
        /// </summary>
        /// <remarks>
        /// RFC 6350 §6 and its extensions say how many instances of each property a card may have, and §6.6.5 allows a <c>MEMBER</c> property only if the <c>KIND</c> property is <c>group</c>.  When
        /// <c>false</c> (the default), <see cref="VcardReader"/> and <see cref="VcardWriter"/> throw <see cref="VcardFileException"/> for a card that breaks those rules.  With
        /// <c>true</c> they keep and write every instance, and read two <c>VERSION</c> lines as one.  The content line reader and writer ignore this.
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
        /// Gets a flag indicating if a value or parameter may break the rules of RFC 6350 and its extensions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// RFC 6350 §4 and §6 give the syntax of each value, and §5 and §6 the parameters each property allows and what they may hold.  This covers a <c>VALUE</c> parameter that names
        /// a type the property doesn't allow; a parameter on a property that doesn't allow it, such as a <c>PID</c> on a property that may have only one instance (§5.5); a <c>PREF</c>
        /// outside 1 to 100 (§5.3); and a <c>PID</c> source with no <c>CLIENTPIDMAP</c> (§6.7.7).  It also covers a <c>LEVEL</c> or <c>INDEX</c> that RFC 6715 §3 doesn't allow, a <c>CC</c> that
        /// isn't an ISO 3166 code (RFC 8605 §3.1), and several <c>CONTACT-URI</c> properties with no <c>PREF</c> (RFC 8605 §2.1).
        /// </para>
        /// <para>
        /// When <c>false</c> (the default), <see cref="VcardReader"/> and <see cref="VcardWriter"/> throw <see cref="VcardFileException"/> for such a card.  With <c>true</c> the line is
        /// kept and written as it is, and the typed members of <see cref="VcardCard"/> leave out a property whose value isn't valid, though it stays in
        /// <see cref="VcardCard.Properties"/>.  A value with a character that can't be written at all still can't be.  The content line reader and writer ignore this.
        /// </para>
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
        /// <para>
        /// <see cref="VcardWriter"/> applies the same choice to a whole card.  A card written as another <see cref="WriteVersion"/> may hold data that version doesn't define, such as a <c>KIND</c> or a
        /// <c>PID</c> in vCard 3.0.  When <c>false</c> (the default), that causes a <see cref="VcardFileException"/> and nothing of the card is written.  When <c>true</c>, the data is left out.
        /// <see cref="VcardVersionConverter.Analyze"/> lists it without writing anything.
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
        /// Gets a flag indicating if a card may leave out a property that is required.
        /// </summary>
        /// <remarks>
        /// RFC 6350 §6.2.1 requires at least one <c>FN</c> property, and §6.7.9 a <c>VERSION</c> property.  RFC 8605 §2.1 requires a card with <c>CONTACT-URI</c> properties to have one with a
        /// <c>mailto</c>, <c>http</c> or <c>https</c> URI.  RFC 2426 §5 requires <c>N</c> and <c>FN</c> in vCard 3.0, and the vCard 2.1 specification §2.2.2 an <c>N</c>.  When <c>false</c>
        /// (the default), <see cref="VcardReader"/> and <see cref="VcardWriter"/> throw <see cref="VcardFileException"/> for a card without them.  With <c>true</c> a card with no
        /// <c>VERSION</c> is read as vCard 4.0, or as vCard 2.1 where it is the value of an <c>AGENT</c> in a vCard 2.1 card, and its
        /// <see cref="VcardCard.SourceVersion"/> is <c>null</c>, but RFC 6868 caret encoding isn't decoded in it unless <see cref="AcceptCaretEncodingInAllVersions"/> is set, since the
        /// content line reader doesn't know its version.  A writer always writes <c>VERSION</c>.  The content line reader and writer ignore this.
        /// </remarks>
        public bool AllowMissingRequiredProperties
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a value in a character set that can't be resolved is decoded as ISO-8859-1.
        /// </summary>
        /// <remarks>
        /// The vCard 2.1 specification §2.1.6 allows any character set registered with IANA, and not every runtime has them all; see <see cref="CharsetResolver"/>.
        /// When <c>false</c> (the default), decoding such a value causes a <see cref="VcardFileException"/>.  When <c>true</c>, it is decoded as ISO-8859-1, which
        /// never fails but may give the wrong characters, and <see cref="VcardContentLine.GetDecodedText(out bool)"/> says it did.  Ignored by a writer.
        /// </remarks>
        public bool AllowUnknownCharsetAsLatin1
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if the <c>VERSION</c> property may be somewhere other than immediately after <c>BEGIN:VCARD</c>.
        /// </summary>
        /// <remarks>
        /// RFC 6350 §6.7.9 says it "must appear immediately after BEGIN:VCARD".  When <c>false</c> (the default), <see cref="VcardReader"/> throws <see cref="VcardFileException"/> for a
        /// card with it anywhere else.  With <c>true</c> the card is read, but RFC 6868 caret encoding isn't decoded on the lines before <c>VERSION</c> unless
        /// <see cref="AcceptCaretEncodingInAllVersions"/> is set, since the content line reader doesn't know the version until then.  Ignored by a writer, which always writes it first.
        /// The content line reader ignores this.
        /// </remarks>
        public bool AllowVersionNotFirst
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets what resolves the character sets that <see cref="MimeCharsetResolver.Default"/> doesn't.
        /// </summary>
        /// <remarks>
        /// A <c>CHARSET</c> is resolved by the encodings built into <see cref="MimeCharsetResolver"/>, then those of the runtime, then this.  When <c>null</c> (the
        /// default), there is nothing after the runtime's.  Ignored by a writer.
        /// </remarks>
#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        public IMimeCharsetResolver? CharsetResolver
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }
#else
        public IMimeCharsetResolver CharsetResolver { get; set; }
#endif

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
        /// replaces invalid data instead of rejecting it.  A reader never removes the encoding's preamble, so a byte order mark is still accepted
        /// only by <see cref="AcceptByteOrderMark"/>.  A decoder that throws <see cref="System.Text.DecoderFallbackException"/> gives a
        /// <see cref="VcardFileException"/>.  A writer writes the encoding's preamble, if it has one, at the start of the data.
        /// </para>
        /// <para>
        /// A raw 8-bit vCard 2.1 value in another character set can't be read as UTF-8.  To read one, read the data as ISO-8859-1, which gives each octet as one character,
        /// so that <see cref="VcardContentLine.GetDecodedText()"/> can get the octets back and decode them by the line's <c>CHARSET</c>.
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
        /// Gets the version that <see cref="VcardWriter"/> writes.
        /// </summary>
        /// <remarks>
        /// The default is <see cref="VcardVersion.V4_0"/>, whatever version a card was read from.  <see cref="VcardVersion.V3_0"/> and <see cref="VcardVersion.V2_1"/> convert a card from the
        /// canonical form of the model; see <see cref="AllowLossyVersionConversion"/> for what they can't hold.  Ignored by a reader and by <see cref="VcardContentLineWriter"/>, which writes the
        /// <c>VERSION</c> line it is given.
        /// </remarks>
        public VcardVersion WriteVersion
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = VcardVersion.V4_0;

        /// <summary>
        /// Gets the maximum length, in characters, of a logical (unfolded) content line, not counting its line break.
        /// </summary>
        /// <remarks>
        /// RFC 6350 sets no limit, so the default is <c>null</c>, meaning no limit.  The reader keeps each logical line in memory, so setting a
        /// limit is recommended when reading untrusted data.  A longer line causes a <see cref="VcardFileException"/>, and so does a line with more
        /// folds than this, since the reader also keeps track of where each fold was.  It is checked as each line is read, so the <c>=</c> of a quoted-printable soft line
        /// break counts until it is removed.  Ignored by a writer.
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
