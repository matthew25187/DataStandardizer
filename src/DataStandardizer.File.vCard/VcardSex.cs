namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The sex component of a GENDER value (RFC 6350 §6.2.7).
    /// </summary>
    public enum VcardSex
    {
        /// <summary>
        /// The component is empty.
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// <c>M</c>: male.
        /// </summary>
        Male = 1,

        /// <summary>
        /// <c>F</c>: female.
        /// </summary>
        Female = 2,

        /// <summary>
        /// <c>O</c>: other.
        /// </summary>
        Other = 3,

        /// <summary>
        /// <c>N</c>: none or not applicable.
        /// </summary>
        NotApplicable = 4,

        /// <summary>
        /// <c>U</c>: unknown.
        /// </summary>
        Unknown = 5
    }
}
