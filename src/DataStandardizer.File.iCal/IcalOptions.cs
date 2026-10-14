using System.Text;

namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// Options to configure the behaviour of an iCalendar reader or writer.
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
        /// Gets a flag indicating if a property value that does not follow the syntax of its value type is accepted.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>false</c> (the default), <see cref="IcalReader"/> rejects a calendar holding such a value, and <see cref="IcalWriter"/> refuses
        /// to write one, with an <see cref="IcalFileException"/>.  The checks cover the value types of RFC 5545 §3.3 and the rules that tie a value
        /// to its property: a <c>VALUE</c> parameter the property does not allow, or one it requires; a DATE-TIME that must be in UTC; and
        /// date-times that must have the same value type as the property's <c>DTSTART</c>.
        /// </para>
        /// <para>
        /// When <c>true</c>, the value is kept as written.  A typed accessor such as <see cref="IcalEvent.DtStart"/> throws
        /// <see cref="IcalFileException"/> when it reads the value.
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
        /// Applies only when the reader or writer is created from a <see cref="System.IO.Stream"/> or file path.  When <c>null</c> (the default),
        /// UTF-8 is used, which is the default charset of RFC 5545 §3.1.4.  A reader lets a byte order mark in the data take precedence.  A writer
        /// writes UTF-8 without a byte order mark.
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
        /// Gets the maximum length, in characters, of a logical (unfolded) content line, not counting its line break.
        /// </summary>
        /// <remarks>
        /// RFC 5545 sets no limit, so the default is <c>null</c>, meaning no limit.  The reader keeps each logical line in memory, so setting a
        /// limit is recommended when reading untrusted data.  A longer line causes an <see cref="IcalFileException"/>.
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
