namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// The level of an expertise, hobby or interest: the value of the <c>LEVEL</c> parameter (RFC 6715 §3.2).
    /// </summary>
    /// <remarks>
    /// <see cref="Beginner"/>, <see cref="Average"/> and <see cref="Expert"/> go with <c>EXPERTISE</c>, and <see cref="High"/>, <see cref="Medium"/> and <see cref="Low"/> with
    /// <c>HOBBY</c> and <c>INTEREST</c>.
    /// </remarks>
    public enum VcardLevel
    {
        /// <summary>
        /// <c>beginner</c>, for <c>EXPERTISE</c>.
        /// </summary>
        Beginner = 0,

        /// <summary>
        /// <c>average</c>, for <c>EXPERTISE</c>.
        /// </summary>
        Average = 1,

        /// <summary>
        /// <c>expert</c>, for <c>EXPERTISE</c>.
        /// </summary>
        Expert = 2,

        /// <summary>
        /// <c>high</c>, for <c>HOBBY</c> and <c>INTEREST</c>.
        /// </summary>
        High = 3,

        /// <summary>
        /// <c>medium</c>, for <c>HOBBY</c> and <c>INTEREST</c>.
        /// </summary>
        Medium = 4,

        /// <summary>
        /// <c>low</c>, for <c>HOBBY</c> and <c>INTEREST</c>.
        /// </summary>
        Low = 5
    }
}
