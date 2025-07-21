namespace DataStandardizer.ISO639
{
    /// <summary>
    /// Type of language represented by an ISO 639 language code.
    /// </summary>
    public enum Iso639LanguageType
    {
        Unknown,
        Living,

        /// <summary>
        /// Ancient languages (extinct since ancient times).
        /// </summary>
        Ancient,

        /// <summary>
        /// Historical languages (distinct from their modern form).
        /// </summary>
        Historical,

        /// <summary>
        /// Extinct languages in recent times.
        /// </summary>
        Extinct,

        /// <summary>
        /// Constructed languages.
        /// </summary>
        Constructed,
        Special
    }
}