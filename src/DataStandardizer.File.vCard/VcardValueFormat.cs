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
        /// The jCard form of RFC 7095.  The value types throw <see cref="System.NotSupportedException"/> for this form until jCard is implemented.
        /// </summary>
        Json = 1,

        /// <summary>
        /// The xCard form of RFC 6351.  The value types throw <see cref="System.NotSupportedException"/> for this form until xCard is implemented.
        /// </summary>
        Xml = 2
    }
}
