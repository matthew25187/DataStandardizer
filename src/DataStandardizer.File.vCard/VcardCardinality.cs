namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// How many instances of a property a card may have (RFC 6350 §6).  Instances with the same <c>ALTID</c> count as one (RFC 6350 §5.4).
    /// </summary>
    public enum VcardCardinality
    {
        /// <summary>
        /// Any number, written <c>*</c>.
        /// </summary>
        Any = 0,

        /// <summary>
        /// Exactly one, written <c>1</c>.
        /// </summary>
        ExactlyOne = 1,

        /// <summary>
        /// At most one, written <c>*1</c>.
        /// </summary>
        AtMostOne = 2,

        /// <summary>
        /// At least one, written <c>1*</c>.
        /// </summary>
        AtLeastOne = 3
    }
}
