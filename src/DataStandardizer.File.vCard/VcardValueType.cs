namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The value types of RFC 6350 §4, named by the <c>VALUE</c> parameter.
    /// </summary>
    public enum VcardValueType
    {
        /// <summary>
        /// A value whose type is not known, such as that of an unknown or experimental (<c>X-</c>) property.  It is kept as raw text, so that it round-trips without loss.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// <c>text</c> (RFC 6350 §4.1).  See <see cref="VcardValue.TryParseText(System.ReadOnlySpan{char}, VcardValueFormat, out string)"/>.
        /// </summary>
        Text = 1,

        /// <summary>
        /// <c>uri</c> (RFC 6350 §4.2).  See <see cref="VcardValue.TryParseUri"/>.
        /// </summary>
        Uri = 2,

        /// <summary>
        /// <c>date</c> (RFC 6350 §4.3.1).  See <see cref="VcardDate"/>.
        /// </summary>
        Date = 3,

        /// <summary>
        /// <c>time</c> (RFC 6350 §4.3.2).  See <see cref="VcardTime"/>.
        /// </summary>
        Time = 4,

        /// <summary>
        /// <c>date-time</c> (RFC 6350 §4.3.3).  See <see cref="VcardDateTime"/>.
        /// </summary>
        DateTime = 5,

        /// <summary>
        /// <c>date-and-or-time</c> (RFC 6350 §4.3.4).  See <see cref="VcardDateAndOrTime"/>.
        /// </summary>
        DateAndOrTime = 6,

        /// <summary>
        /// <c>timestamp</c> (RFC 6350 §4.3.5).  See <see cref="VcardTimestamp"/>.
        /// </summary>
        Timestamp = 7,

        /// <summary>
        /// <c>boolean</c> (RFC 6350 §4.4).  See <see cref="VcardValue.TryParseBoolean"/>.
        /// </summary>
        Boolean = 8,

        /// <summary>
        /// <c>integer</c> (RFC 6350 §4.5).  See <see cref="VcardValue.TryParseInteger"/>.
        /// </summary>
        Integer = 9,

        /// <summary>
        /// <c>float</c> (RFC 6350 §4.6).  See <see cref="VcardValue.TryParseFloat"/>.
        /// </summary>
        Float = 10,

        /// <summary>
        /// <c>utc-offset</c> (RFC 6350 §4.7).  See <see cref="VcardUtcOffset"/>.
        /// </summary>
        UtcOffset = 11,

        /// <summary>
        /// <c>language-tag</c> (RFC 6350 §4.8).  See <see cref="VcardValue.TryParseLanguageTag"/>.
        /// </summary>
        LanguageTag = 12
    }
}
