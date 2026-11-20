namespace DataStandardizer.Language
{
    /// <summary>
    /// Scope of an ISO 639 language code.
    /// </summary>
    public enum Iso639LanguageScope
    {
        /// <summary>
        /// The scope isn't known.
        /// </summary>
        Unknown,

        /// <summary>
        /// Individual language.
        /// </summary>
        Individual,

        /// <summary>
        /// Collections of languages connected, for example genetically or by region.
        /// </summary>
        Collective,

        /// <summary>
        /// Macrolanguages.
        /// </summary>
        Macrolanguage,

        /// <summary>
        /// Special codes, which don't identify a language (for example <c>und</c>, <c>mul</c> and <c>zxx</c>).
        /// </summary>
        Special
    }
}