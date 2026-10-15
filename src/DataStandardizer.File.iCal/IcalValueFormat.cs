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
        /// The jCal form of RFC 7265, the text of a JSON value, which uses extended ISO 8601; for example <c>2013-01-01T12:00:00Z</c> or
        /// <c>-05:00</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A value in this form is the text of a JSON string with its JSON escaping removed, or the text of a JSON number or literal.  TEXT
        /// values have no iCalendar escaping in this form, and the items of a list are separate JSON strings, so a value is one item.  BOOLEAN
        /// values are <c>true</c> or <c>false</c>.  INTEGER and FLOAT values are JSON numbers, which have no plus sign or leading zeros and may
        /// have an exponent.  DATE, TIME, DATE-TIME and UTC-OFFSET values are as in <see cref="Xml"/> form.  DURATION, BINARY, URI and
        /// CAL-ADDRESS values are as in <see cref="Text"/> form.
        /// </para>
        /// <para>
        /// A PERIOD is the start and end or duration joined by a solidus, as in <see cref="Xml"/> form; in a jCal document they are the two
        /// strings of an array.  A RECUR value has no form of its own, since in a jCal document it is a JSON object with a member for each rule
        /// part, so <see cref="IcalRecurrenceRule"/> throws <see cref="System.NotSupportedException"/> for this form.  <c>JcalReader</c> and
        /// <c>JcalWriter</c> handle the array and the object.
        /// </para>
        /// <para>
        /// This form needs no JSON library, so the value types support it on every target, netstandard1.1 included.  <c>JcalReader</c> and
        /// <c>JcalWriter</c>, which read and write jCal documents, use System.Text.Json and so are not available on netstandard1.1.
        /// </para>
        /// </remarks>
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
