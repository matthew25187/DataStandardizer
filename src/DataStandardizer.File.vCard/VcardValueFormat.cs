namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The lexical form in which a value is parsed or formatted.  The value types hold structured data, so one value can be written in any of these forms.
    /// </summary>
    public enum VcardValueFormat
    {
        /// <summary>
        /// The text form used in <c>.vcf</c> content lines; for example <c>19850412</c> or <c>-0500</c> in vCard 4.0.
        /// </summary>
        Text = 0,

        /// <summary>
        /// The jCard form of RFC 7095, the text of a JSON value, which uses the extended format of ISO 8601; for example <c>1985-04-12</c> or <c>-05:00</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A value in this form is the text of a JSON string with its JSON escaping removed, or the text of a JSON number or literal.  TEXT values have no escaping of vCard in this
        /// form, and the items of a list are separate JSON strings, so a value is one item.  BOOLEAN values are <c>true</c> or <c>false</c>.  INTEGER and FLOAT values are JSON
        /// numbers, which have no plus sign or leading zeros and may have an exponent; an INTEGER may have a fraction, which is dropped (RFC 7095 §3.5.9).  DATE, TIME, DATE-TIME,
        /// DATE-AND-OR-TIME, TIMESTAMP and UTC-OFFSET values are in the extended format of ISO 8601 where it applies (RFC 7095 §3.5.3 to §3.5.7 and §3.5.11).  URI values have no
        /// escaping of their commas, and LANGUAGE-TAG values are as in <see cref="Text"/> form.
        /// </para>
        /// <para>
        /// jCard represents vCard 4.0 alone, so a value type that takes a <see cref="VcardVersion"/> throws <see cref="System.ArgumentException"/> for this form with any other.  A
        /// structured value, such as <see cref="VcardName"/>, has no form of its own, since in a jCard document it is a JSON array with an element for each component (RFC 7095
        /// §3.3.1.3), so it throws <see cref="System.NotSupportedException"/> for this form.  <c>JcardReader</c> and <c>JcardWriter</c> handle the arrays.
        /// </para>
        /// <para>
        /// This form needs no JSON library, so the value types support it on every target, netstandard1.1 included.  <c>JcardReader</c> and <c>JcardWriter</c>, which read and write
        /// jCard documents, use System.Text.Json and so are not available on netstandard1.1.
        /// </para>
        /// </remarks>
        Json = 1,

        /// <summary>
        /// The xCard form of RFC 6351.  The value types throw <see cref="System.NotSupportedException"/> for this form until xCard is implemented.
        /// </summary>
        Xml = 2
    }
}
