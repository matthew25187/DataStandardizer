using System;

namespace DataStandardizer.File.CSV
{
    /// <summary>
    /// Kinds of character outside RFC 4180 <c>TEXTDATA</c> that a field value may hold.
    /// </summary>
    /// <remarks>
    /// RFC 4180§2 builds both escaped and non-escaped fields from <c>TEXTDATA = %x20-21 / %x23-2B / %x2D-7E</c>, the printable characters of
    /// US-ASCII, and lets an escaped field also hold COMMA, CR, LF and a pair of double quotes.  By default, a reader reports any other character
    /// in a field as a bad value, and a writer throws for a value that holds one.  Each member relaxes that rule for one kind of character.
    /// <para>
    /// The field delimiter takes the place of COMMA, and the characters of the terminator line break take the place of CR and LF, so an escaped
    /// field may hold them whatever this is.  A non-escaped field never holds them, as they end the field or the record.
    /// </para>
    /// </remarks>
    [Flags]
    public enum CsvExtendedCharacters
    {
        /// <summary>
        /// Field values hold only the characters that RFC 4180§2 allows.
        /// </summary>
        None = 0,

        /// <summary>
        /// Field values may hold control characters, such as TAB and NUL: <c>%x00-1F</c>, <c>%x7F</c> and, with <see cref="NonAscii"/>,
        /// <c>%x80-9F</c>.
        /// </summary>
        /// <remarks>
        /// Relaxes RFC 4180§2 (<c>TEXTDATA</c>), which leaves out every control character.  CR and LF in a non-escaped field are still bad values,
        /// as RFC 4180§2¶6 requires a field that holds a line break to be enclosed in double quotes.
        /// </remarks>
        Control = 1 << 0,

        /// <summary>
        /// Field values may hold characters outside US-ASCII, such as <c>é</c>, other than the control characters <c>%x80-9F</c>, which also need
        /// <see cref="Control"/>.
        /// </summary>
        /// <remarks>
        /// Relaxes RFC 4180§2 (<c>TEXTDATA</c>), whose grammar is in US-ASCII.  RFC 4180§3 lets a <c>text/csv</c> file declare another character
        /// set with its <c>charset</c> parameter, which this reads as allowing the printable characters of that character set.
        /// </remarks>
        NonAscii = 1 << 1
    }
}
