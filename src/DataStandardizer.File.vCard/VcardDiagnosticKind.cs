namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The kinds of conformance problem that <see cref="VcardCard.Validate"/>, <see cref="VcardReader"/> and <see cref="VcardWriter"/> report.
    /// </summary>
    public enum VcardDiagnosticKind
    {
        /// <summary>
        /// A required property is missing: <c>FN</c>, or the <c>VERSION</c> of a card being read.  Accepted with <see cref="VcardOptions.AllowMissingRequiredProperties"/>.
        /// </summary>
        MissingRequiredProperty = 1,

        /// <summary>
        /// A property appears more often than RFC 6350 §6 allows, or a <c>MEMBER</c> appears in a card whose <c>KIND</c> is not <c>group</c>.  Accepted with
        /// <see cref="VcardOptions.AllowCardinalityViolations"/>.
        /// </summary>
        CardinalityViolation = 2,

        /// <summary>
        /// A value does not follow the syntax of its value type, or a <c>VALUE</c> parameter names a type the property doesn't allow.  Accepted with
        /// <see cref="VcardOptions.AllowInvalidValues"/>.
        /// </summary>
        InvalidValue = 3,

        /// <summary>
        /// A parameter is on a property that doesn't allow it, is repeated, or has a value that is not valid; or a <c>PID</c> names a source that has no <c>CLIENTPIDMAP</c>.
        /// Accepted with <see cref="VcardOptions.AllowInvalidValues"/>.
        /// </summary>
        InvalidParameter = 4,

        /// <summary>
        /// The card says it is of a version that can't be read.  Never accepted.
        /// </summary>
        UnsupportedVersion = 5,

        /// <summary>
        /// A value had to be adapted to convert it to another version.  No conversion reports this yet: it arrives with reading and writing vCard 3.0.
        /// </summary>
        VersionConversion = 6,

        /// <summary>
        /// A card holds data that the version being written can't hold.  Accepted with <see cref="VcardOptions.AllowLossyVersionConversion"/>.  No conversion reports this
        /// yet: it arrives with reading and writing vCard 3.0.
        /// </summary>
        Lossy = 7,

        /// <summary>
        /// A vCard 4.0 card has a form of vCard 2.1: a parameter with no name, or an <c>ENCODING</c> or <c>CHARSET</c> parameter.  A reader accepts it with
        /// <see cref="VcardOptions.AcceptLegacySyntax"/>.  A writer never does.
        /// </summary>
        LegacySyntax = 8,

        /// <summary>
        /// The lines of a card don't have the structure of the <c>vcard</c> rule of RFC 6350 §3.3: for example a <c>BEGIN</c> line with a group, or a <c>VERSION</c> line that
        /// isn't immediately after <c>BEGIN</c>, which a reader accepts with <see cref="VcardOptions.AllowVersionNotFirst"/>.  Nothing else is accepted.
        /// </summary>
        InvalidStructure = 9
    }
}
