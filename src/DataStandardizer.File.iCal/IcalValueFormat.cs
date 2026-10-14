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
        /// The xCal form of RFC 6321, which uses extended ISO 8601; for example <c>2013-01-01T12:00:00Z</c> or <c>-05:00</c>.  Not supported
        /// yet: parsing or formatting in this form throws <see cref="System.NotSupportedException"/>.
        /// </summary>
        Xml = 2
    }
}
