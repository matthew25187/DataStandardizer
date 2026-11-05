namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The kinds of conformance problem that <see cref="VcardCard.Validate"/>, <see cref="VcardReader"/> and <see cref="VcardWriter"/> report.
    /// </summary>
    public enum VcardDiagnosticKind
    {
        /// <summary>
        /// A required property is missing: <c>FN</c>, the <c>VERSION</c> of a card being read, or a <c>CONTACT-URI</c> with a <c>mailto</c>, <c>http</c> or <c>https</c> URI in a card
        /// that has others (RFC 8605 §2.1).  Accepted with <see cref="VcardOptions.AllowMissingRequiredProperties"/>.
        /// </summary>
        MissingRequiredProperty = 1,

        /// <summary>
        /// A property appears more often than RFC 6350 §6 or the extension that defines it allows, or a <c>MEMBER</c> appears in a card whose <c>KIND</c> is not <c>group</c>.  Accepted with
        /// <see cref="VcardOptions.AllowCardinalityViolations"/>.
        /// </summary>
        CardinalityViolation = 2,

        /// <summary>
        /// A value does not follow the syntax of its value type, or a <c>VALUE</c> parameter names a type the property doesn't allow.  Accepted with
        /// <see cref="VcardOptions.AllowInvalidValues"/>.
        /// </summary>
        InvalidValue = 3,

        /// <summary>
        /// A parameter is on a property that doesn't allow it, is repeated, or has a value that is not valid; a <c>PID</c> names a source that has no <c>CLIENTPIDMAP</c>; or several
        /// <c>CONTACT-URI</c> properties have no <c>PREF</c> (RFC 8605 §2.1).
        /// Accepted with <see cref="VcardOptions.AllowInvalidValues"/>.
        /// </summary>
        InvalidParameter = 4,

        /// <summary>
        /// The card says it is of a version that can't be read.  Never accepted.
        /// </summary>
        UnsupportedVersion = 5,

        /// <summary>
        /// A property of an older version has no equivalent in vCard 4.0, such as a <c>LABEL</c> that goes with no <c>ADR</c>, and is kept as a property that vCard 4.0 doesn't define.  Nothing is
        /// lost, so it is always accepted.  <see cref="VcardVersionConverter.Analyze"/> reports it.
        /// </summary>
        VersionConversion = 6,

        /// <summary>
        /// A card holds data that the version being written can't hold.  Accepted with <see cref="VcardOptions.AllowLossyVersionConversion"/>, and the data is then left out.
        /// </summary>
        Lossy = 7,

        /// <summary>
        /// A vCard 4.0 or 3.0 card has a form of vCard 2.1: a parameter with no name, a <c>CHARSET</c> parameter, or an <c>ENCODING</c> parameter, other than the <c>b</c> of vCard 3.0.  A reader
        /// accepts it with <see cref="VcardOptions.AcceptLegacySyntax"/>.  A writer never does.
        /// </summary>
        LegacySyntax = 8,

        /// <summary>
        /// The lines of a card don't have the structure of the <c>vcard</c> rule of RFC 6350 §3.3: for example a <c>BEGIN</c> line with a group, or a <c>VERSION</c> line that
        /// isn't immediately after <c>BEGIN</c>, which a reader accepts with <see cref="VcardOptions.AllowVersionNotFirst"/>.  Nothing else is accepted.
        /// </summary>
        InvalidStructure = 9
    }
}
