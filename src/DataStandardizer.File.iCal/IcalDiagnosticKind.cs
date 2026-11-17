namespace DataStandardizer.File.iCal
{
    /// <summary>
    /// The kinds of conformance problem <see cref="IcalComponent.Validate"/> reports.
    /// </summary>
    /// <remarks>
    /// <see cref="InvalidComponentNesting"/> and <see cref="UnsupportedVersion"/> are never accepted.  They break a requirement that says how the data is read, or that keeps its
    /// structure, so no option waives them, and an <see cref="IcalFileException"/> of either kind can't be avoided by setting one.
    /// </remarks>
    public enum IcalDiagnosticKind
    {
        /// <summary>
        /// A required property or component is missing, other than the <c>VERSION</c> of a calendar, which is <see cref="UnsupportedVersion"/>.  Accepted with
        /// <see cref="IcalOptions.AllowMissingRequiredProperties"/>.
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
        /// A component is inside a component where RFC 5545 does not allow it, such as a VEVENT inside a VTODO; is nested in itself; or is
        /// nested more than 64 levels deep.  Or a property is named <c>BEGIN</c> or <c>END</c>, which RFC 5545 §3.4 and §3.6 keep for the lines that start and end a component, so
        /// writing it would change the nesting.  Never accepted.
        /// </summary>
        InvalidComponentNesting = 5,

        /// <summary>
        /// The <c>VERSION</c> property is not <c>2.0</c> or <c>2.0;2.0</c>, the only values RFC 5545 §3.7.4 allows, or a calendar being read has no <c>VERSION</c>, which says how it
        /// is read (RFC 5545 §3.6).  Never accepted.  A writer adds <c>VERSION:2.0</c> to a calendar that has none.
        /// </summary>
        UnsupportedVersion = 6,

        /// <summary>
        /// A parameter whose grammar in RFC 5545 §3.2 or RFC 7986 §6 has no quoted form has a value in double quotes, such as
        /// <c>FMTTYPE="audio/basic"</c>, or, on a line not read from .ics, a value that a writer would have to quote.  Accepted with
        /// <see cref="IcalOptions.AllowQuotedParameterValues"/>.
        /// </summary>
        QuotedParameterValue = 7
    }
}
