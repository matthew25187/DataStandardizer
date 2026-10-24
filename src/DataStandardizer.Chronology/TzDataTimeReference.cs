namespace DataStandardizer.Chronology
{
    /// <summary>
    /// Identifies the clock against which a TZ Database time of day is measured.
    /// </summary>
    public enum TzDataTimeReference
    {
        /// <summary>
        /// Local wall clock time, including any daylight saving in effect. Written with no suffix, or with the suffix <c>w</c>.
        /// </summary>
        Wall,

        /// <summary>
        /// Local standard time, excluding any daylight saving. Written with the suffix <c>s</c>.
        /// </summary>
        Standard,

        /// <summary>
        /// Universal time. Written with the suffix <c>u</c>, <c>g</c> or <c>z</c>.
        /// </summary>
        Universal
    }
}
