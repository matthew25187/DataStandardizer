namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The kinds of conformance problem <see cref="IcalComponent.Validate"/> reports.
    /// </summary>
    public enum IcalDiagnosticKind
    {
        /// <summary>
        /// A required property or component is missing.  Accepted with <see cref="IcalOptions.AllowMissingRequiredProperties"/>.
        /// </summary>
        MissingRequiredProperty = 1,

        /// <summary>
        /// A property appears more often than allowed, or two properties that exclude each other both appear.  Accepted with
        /// <see cref="IcalOptions.AllowCardinalityViolations"/>.
        /// </summary>
        CardinalityViolation = 2,

        /// <summary>
        /// A <c>TZID</c> parameter names a time zone that has no VTIMEZONE in the calendar.  Accepted with
        /// <see cref="IcalOptions.AllowUndefinedTimeZones"/>.
        /// </summary>
        UndefinedTimeZone = 3,

        /// <summary>
        /// A property value does not follow the syntax of its value type, or breaks a rule that ties it to its property; or a parameter that
        /// RFC 5545 or RFC 7986 defines is repeated or has too many values, or a <c>FMTTYPE</c> parameter does not follow RFC 5545 §3.2.8.
        /// Accepted with <see cref="IcalOptions.AllowInvalidValues"/>.
        /// </summary>
        InvalidValue = 4,

        /// <summary>
        /// A component is inside a component where RFC 5545 does not allow it, such as a VEVENT inside a VTODO.  Never accepted.
        /// </summary>
        InvalidComponentNesting = 5,

        /// <summary>
        /// The <c>VERSION</c> property does not include iCalendar 2.0.  Never accepted.
        /// </summary>
        UnsupportedVersion = 6,

        /// <summary>
        /// A parameter whose grammar in RFC 5545 §3.2 or RFC 7986 §6 has no quoted form has a value in double quotes, such as
        /// <c>FMTTYPE="audio/basic"</c>.  Accepted with <see cref="IcalOptions.AllowQuotedParameterValues"/>.
        /// </summary>
        QuotedParameterValue = 7
    }
}
