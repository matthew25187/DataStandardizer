namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The value types of RFC 5545 §3.3, named by the <c>VALUE</c> parameter.
    /// </summary>
    public enum IcalValueType
    {
        /// <summary>
        /// A value whose type is not known, such as that of an unknown, experimental (<c>X-</c>) or IANA-registered property.  It is kept as
        /// raw text, so that it round-trips without loss.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// <c>BINARY</c> (RFC 5545 §3.3.1): base64-encoded bytes.  See <see cref="IcalValue.TryParseBinary"/>.
        /// </summary>
        Binary = 1,

        /// <summary>
        /// <c>BOOLEAN</c> (RFC 5545 §3.3.2).  See <see cref="IcalValue.TryParseBoolean"/>.
        /// </summary>
        Boolean = 2,

        /// <summary>
        /// <c>CAL-ADDRESS</c> (RFC 5545 §3.3.3): a calendar user address.  See <see cref="IcalValue.TryParseCalAddress"/>.
        /// </summary>
        CalAddress = 3,

        /// <summary>
        /// <c>DATE</c> (RFC 5545 §3.3.4).  See <see cref="IcalDate"/>.
        /// </summary>
        Date = 4,

        /// <summary>
        /// <c>DATE-TIME</c> (RFC 5545 §3.3.5).  See <see cref="IcalDateTime"/>.
        /// </summary>
        DateTime = 5,

        /// <summary>
        /// <c>DURATION</c> (RFC 5545 §3.3.6).  See <see cref="IcalDuration"/>.
        /// </summary>
        Duration = 6,

        /// <summary>
        /// <c>FLOAT</c> (RFC 5545 §3.3.7).  See <see cref="IcalValue.TryParseFloat"/>.
        /// </summary>
        Float = 7,

        /// <summary>
        /// <c>INTEGER</c> (RFC 5545 §3.3.8).  See <see cref="IcalValue.TryParseInteger"/>.
        /// </summary>
        Integer = 8,

        /// <summary>
        /// <c>PERIOD</c> (RFC 5545 §3.3.9).  See <see cref="IcalPeriod"/>.
        /// </summary>
        Period = 9,

        /// <summary>
        /// <c>RECUR</c> (RFC 5545 §3.3.10).  See <see cref="IcalRecurrenceRule"/>.
        /// </summary>
        Recur = 10,

        /// <summary>
        /// <c>TEXT</c> (RFC 5545 §3.3.11).  See <see cref="IcalValue.TryParseText(System.ReadOnlySpan{char}, IcalValueFormat, out string)"/>.
        /// </summary>
        Text = 11,

        /// <summary>
        /// <c>TIME</c> (RFC 5545 §3.3.12).  See <see cref="IcalTime"/>.
        /// </summary>
        Time = 12,

        /// <summary>
        /// <c>URI</c> (RFC 5545 §3.3.13).  See <see cref="IcalValue.TryParseUri"/>.
        /// </summary>
        Uri = 13,

        /// <summary>
        /// <c>UTC-OFFSET</c> (RFC 5545 §3.3.14).  See <see cref="IcalUtcOffset"/>.
        /// </summary>
        UtcOffset = 14
    }
}
