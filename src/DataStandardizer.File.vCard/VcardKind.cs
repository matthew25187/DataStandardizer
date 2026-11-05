namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The kind of object a card represents (RFC 6350 §6.1.4).
    /// </summary>
    public enum VcardKind
    {
        /// <summary>
        /// A single person or entity: <c>individual</c>, which is also what a card with no <c>KIND</c> property is.
        /// </summary>
        Individual = 0,

        /// <summary>
        /// A group of persons or entities: <c>group</c>.  Only this kind of card may have <c>MEMBER</c> properties.
        /// </summary>
        Group = 1,

        /// <summary>
        /// An organization: <c>org</c>.
        /// </summary>
        Organization = 2,

        /// <summary>
        /// A named geographical place: <c>location</c>.
        /// </summary>
        Location = 3,

        /// <summary>
        /// Any other value, which <see cref="VcardCard.KindText"/> holds.
        /// </summary>
        Other = 4
    }
}
