namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// What a <see cref="VcardDateAndOrTime"/> holds.
    /// </summary>
    public enum VcardDateAndOrTimeKind
    {
        /// <summary>
        /// A date alone.
        /// </summary>
        Date = 0,

        /// <summary>
        /// A time alone.
        /// </summary>
        Time = 1,

        /// <summary>
        /// A date and a time.
        /// </summary>
        DateTime = 2
    }
}
