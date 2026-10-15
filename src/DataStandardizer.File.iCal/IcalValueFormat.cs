namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The lexical form in which a value is parsed or formatted.  The value types hold structured data, so one value can be written in any
    /// of these forms.
    /// </summary>
    public enum IcalValueFormat
    {
        /// <summary>
        /// The text form of RFC 5545, used in <c>.ics</c> content lines; for example <c>20130101T120000Z</c> or <c>-0500</c>.
        /// </summary>
        Text = 0,

        /// <summary>
        /// The jCal form of RFC 7265, which uses extended ISO 8601; for example <c>2013-01-01T12:00:00Z</c> or <c>-05:00</c>.  Not supported
        /// yet: parsing or formatting in this form throws <see cref="System.NotSupportedException"/>.
        /// </summary>
        Json = 1,

        /// <summary>
        /// The xCal form of RFC 6321, the content of a value element, which uses extended ISO 8601; for example <c>2013-01-01T12:00:00Z</c> or
        /// <c>-05:00</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// TEXT values have no escaping in this form, and the items of a list are separate elements, so a value is one item.  BOOLEAN values are
        /// <c>true</c> or <c>false</c>.  DURATION, INTEGER, FLOAT, BINARY, URI and CAL-ADDRESS values are as in <see cref="Text"/> form, though
        /// parsing accepts what their XML Schema types allow, such as white space around a number.
        /// </para>
        /// <para>
        /// A PERIOD is the start and end or duration joined by a solidus, as in <see cref="Text"/> form; in an xCal document they are separate
        /// elements.  A RECUR value has no form of its own, since in an xCal document each rule part is an element, so
        /// <see cref="IcalRecurrenceRule"/> throws <see cref="System.NotSupportedException"/> for this form.  <see cref="XcalReader"/> and
        /// <see cref="XcalWriter"/> handle those elements.
        /// </para>
        /// </remarks>
        Xml = 2
    }
}
