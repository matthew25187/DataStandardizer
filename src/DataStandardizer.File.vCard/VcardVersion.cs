namespace DataStandardizer.File.vCard
{
    /// <summary>
    /// A version of the vCard format.  The versions are not additive: each one has its own grammar for values, so a value that is valid in one can be
    /// invalid in another.
    /// </summary>
    public enum VcardVersion
    {
        /// <summary>
        /// vCard 4.0 (RFC 6350).
        /// </summary>
        V4_0 = 0,

        /// <summary>
        /// vCard 3.0 (RFC 2425 and RFC 2426).
        /// </summary>
        V3_0 = 1,

        /// <summary>
        /// vCard 2.1 (the 1996 IMC specification).
        /// </summary>
        V2_1 = 2
    }
}
