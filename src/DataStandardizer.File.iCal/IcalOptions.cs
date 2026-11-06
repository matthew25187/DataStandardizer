using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Options to configure the behaviour of an iCalendar reader or writer, and of recurrence expansion.
    /// </summary>
    /// <remarks>
    /// The default settings for all options ensure behaviour consistent with RFC 5545.  Each accommodation for common non-compliant input is a
    /// separate option, off by default.
    /// </remarks>
#if NET5_0_OR_GREATER
    public sealed record IcalOptions
#else
    public sealed class IcalOptions
#endif
    {
        /// <summary>
        /// Gets a flag indicating if a bare line feed is accepted as a line break, both to end a content line and in a fold.
        /// </summary>
        /// <remarks>
        /// RFC 5545 §3.1 requires a CRLF sequence.  When <c>false</c> (the default), a bare line feed causes an <see cref="IcalFileException"/>.
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
        /// The <c>icalstream</c> rule of RFC 5545 §3.6 starts with <c>BEGIN:VCALENDAR</c>, and RFC 3629 §6 says a protocol that mandates UTF-8 should
        /// forbid the signature.  When <c>false</c> (the default), a byte order mark is a character in the first line, which causes an
        /// <see cref="IcalFileException"/>.  A byte order mark never selects the encoding; see <see cref="Encoding"/>.  Ignored by a writer, which
        /// doesn't write one unless <see cref="Encoding"/> has a preamble, and by <see cref="XcalReader"/> and <c>JcalReader</c>, whose formats
        /// have their own rules.
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
        /// Gets a flag indicating if the last content line may end without a line break.
        /// </summary>
        /// <remarks>
        /// The <c>contentline</c> rule of RFC 5545 §3.1 ends with CRLF, including on the last line.  When <c>false</c> (the default), a last line
        /// without one causes an <see cref="IcalFileException"/>.  Ignored by a writer, which always ends the last line with a line break.
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
        /// Gets a flag indicating if a property value that does not follow the syntax of its value type is accepted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>false</c> (the default), <see cref="IcalReader"/> rejects a calendar holding such a value, and <see cref="IcalWriter"/> refuses
        /// to write one, with an <see cref="IcalFileException"/>.  The checks cover the value types of RFC 5545 §3.3 and the rules that tie a value
        /// to its property: a <c>VALUE</c> parameter the property does not allow, or one it requires; a DATE-TIME that must be in UTC; and
        /// date-times that must have the same value type as the property's <c>DTSTART</c>.  They also cover the <c>FMTTYPE</c> parameter, which
        /// RFC 5545 §3.2.8 makes one type and subtype with no media type parameters, and which may appear only once; a parameter defined with one
        /// value in RFC 5545 §3.2 or RFC 7986 §6 that has more, such as <c>CN=Doe, John</c>, whose comma must be in a quoted string; and a parameter
        /// repeated on a property, where RFC 5545 §3.8 and RFC 7986 §5 say it "MUST NOT occur more than once".
        /// </para>
        /// <para>
        /// When <c>true</c>, the value is kept as written.  A typed accessor such as <see cref="IcalEvent.DtStart"/> throws
        /// <see cref="IcalFileException"/> when it reads the value, as <see cref="IcalContentLine.GetFormatType"/> does for a <c>FMTTYPE</c>
        /// parameter.  For any other parameter that is repeated or has too many values, a typed accessor uses the first value.
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
        /// Gets a flag indicating if a parameter whose grammar has no quoted form may have a value in double quotes, such as
        /// <c>FMTTYPE="audio/basic"</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// RFC 5545 §3.1 allows a parameter value to be a <c>quoted-string</c> in general, but the rule for each parameter narrows that.  Some
        /// rules leave the quoted form out: <c>fmttypeparam = "FMTTYPE" "=" type-name "/" subtype-name</c> in RFC 5545 §3.2.8 is one.  The others
        /// are <c>CUTYPE</c>, <c>ENCODING</c>, <c>FBTYPE</c>, <c>LANGUAGE</c>, <c>PARTSTAT</c>, <c>RANGE</c>, <c>RELATED</c>, <c>RELTYPE</c>,
        /// <c>ROLE</c>, <c>RSVP</c>, <c>TZID</c> and <c>VALUE</c> in RFC 5545 §3.2, and <c>DISPLAY</c> and <c>FEATURE</c> in RFC 7986 §6.  These
        /// parameters are checked on every property, including a non-standard one.
        /// </para>
        /// <para>
        /// When <c>false</c> (the default), <see cref="IcalReader"/> rejects a calendar with such a value, and <see cref="IcalWriter"/> refuses to
        /// write one, with an <see cref="IcalFileException"/>.  The writer also refuses a value it would have to quote, one with a <c>:</c>,
        /// <c>;</c> or <c>,</c>, on a line built in code.  When <c>true</c>, the value is read without its quotes, and <see cref="IcalWriter"/>
        /// writes the parameter back as it was read, with them, and quotes a value built in code that needs it.  jCal and xCal have no quoting,
        /// so <c>JcalReader</c>, <c>JcalWriter</c>, <see cref="XcalReader"/> and <see cref="XcalWriter"/> accept such a value whatever this
        /// option is.
        /// </para>
        /// </remarks>
        public bool AllowQuotedParameterValues
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets a flag indicating if a property is accepted where RFC 5545 allows it at most once but it appears more than once, or where two
        /// properties that exclude each other both appear, such as <c>DTEND</c> and <c>DURATION</c> in a VEVENT.
        /// </summary>
        /// <remarks>
        /// When <c>false</c> (the default), <see cref="IcalReader"/> rejects such a calendar, and <see cref="IcalWriter"/> refuses to write one,
        /// with an <see cref="IcalFileException"/>.  A typed accessor reads the first of the properties.
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
        /// Gets a flag indicating if a required property or component may be missing, such as the <c>UID</c> or <c>DTSTAMP</c> of a VEVENT, or the
        /// STANDARD or DAYLIGHT parts of a VTIMEZONE.
        /// </summary>
        /// <remarks>
        /// When <c>false</c> (the default), <see cref="IcalReader"/> rejects such a calendar, and <see cref="IcalWriter"/> refuses to write one,
        /// with an <see cref="IcalFileException"/>.  A missing <c>VERSION</c> is covered too; <see cref="IcalWriter"/> always writes
        /// <c>VERSION:2.0</c>, so it never refuses a calendar for lacking one.
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
        /// Gets a flag indicating if a <c>TZID</c> parameter may name a time zone the calendar has no VTIMEZONE for.
        /// </summary>
        /// <remarks>
        /// RFC 5545 §3.2.19 requires a VTIMEZONE for each <c>TZID</c>.  When <c>false</c> (the default), <see cref="IcalReader"/> rejects a
        /// calendar without one, and <see cref="IcalWriter"/> refuses to write one, with an <see cref="IcalFileException"/>.
        /// </remarks>
        public bool AllowUndefinedTimeZones
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        }

        /// <summary>
        /// Gets the encoding for the iCalendar data.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Applies only when the reader or writer is created from a <see cref="System.IO.Stream"/> or file path.  When <c>null</c> (the default),
        /// the data is strict UTF-8, the default charset of RFC 5545 §3.1.4.  A byte sequence that is not valid UTF-8 causes an
        /// <see cref="IcalFileException"/>, and a byte order mark doesn't change the encoding; see <see cref="AcceptByteOrderMark"/>.  A writer
        /// writes strict UTF-8 with no byte order mark.
        /// </para>
        /// <para>
        /// Another encoding is used as it is: <see cref="System.Text.Encoding.UTF8"/> replaces invalid data instead of rejecting it, and an
        /// encoding with a preamble has it removed from the start of the data, whatever <see cref="AcceptByteOrderMark"/> says.  A decoder that
        /// throws <see cref="DecoderFallbackException"/> gives an <see cref="IcalFileException"/>.  A writer writes the encoding's preamble, if it
        /// has one, at the start of the data.
        /// </para>
        /// <para>
        /// The exception is <see cref="XcalReader"/>, which, when this is <c>null</c>, takes the encoding from the XML document itself: from its
        /// byte order mark or XML declaration, and otherwise UTF-8.  <c>JcalReader</c> and <c>JcalWriter</c> accept only <c>null</c> or a UTF-8
        /// encoding, since jCal is always UTF-8.
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
        /// The default is 75, the length RFC 5545 §3.1 recommends.  Lengths are measured in octets of the UTF-8 encoding, not counting the line
        /// break, and a fold never splits a character.  The minimum is 4, the longest UTF-8 sequence; a continuation line that starts with a
        /// 4-octet character is then one octet longer, so that it holds at least one character.  Ignored by a reader.
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
        /// Either a space (the default) or a horizontal tab, the two characters RFC 5545 §3.1 allows.  Ignored by a reader, which accepts both.
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
        /// RFC 5545 §3.1 requires a CRLF sequence, which is the default.  A bare line feed (<c>"\n"</c>) may be chosen for consumers that expect
        /// it; no other value is accepted.  The line break never depends on <see cref="System.Environment.NewLine"/>.  Ignored by a reader; see
        /// <see cref="AcceptBareLineFeed"/>.
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
        /// Gets the number of consecutive periods a recurrence rule may step through without producing an occurrence before its expansion
        /// gives up: in the <c>GetOccurrences</c> methods of <see cref="IcalRecurrenceRule"/> that take options, and in those of
        /// <see cref="IcalEvent"/>, <see cref="IcalTodo"/> and <see cref="IcalJournal"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A period is one step of the rule's frequency, such as a year for <c>FREQ=YEARLY</c>; a run of periods that the rule skips as a whole,
        /// such as the seconds of a day that a <c>FREQ=SECONDLY</c> rule excludes, counts as one.  A rule that can never match, such as
        /// <c>FREQ=SECONDLY;BYMONTH=2;BYMONTHDAY=30</c>, then ends with an <see cref="System.InvalidOperationException"/> rather than running for
        /// a very long time.
        /// </para>
        /// <para>
        /// The default is 100,000, far more than the gap between the occurrences of any rule that can match: the longest is the eight years
        /// between two 29 Februaries, which is fewer than 3,000 daily periods.  The value must be at least 1.
        /// </para>
        /// </remarks>
        public int MaxRecurrenceIterations
        {
            get;
#if NET5_0_OR_GREATER
            init;
#else
            set;
#endif
        } = 100000;

        /// <summary>
        /// Gets the maximum length, in characters, of a logical (unfolded) content line, not counting its line break.
        /// </summary>
        /// <remarks>
        /// RFC 5545 sets no limit, so the default is <c>null</c>, meaning no limit.  The reader keeps each logical line in memory, so setting a
        /// limit is recommended when reading untrusted data.  A longer line causes an <see cref="IcalFileException"/>, and so does a line with more
        /// folds than this, since the reader also keeps track of where each fold was.
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
