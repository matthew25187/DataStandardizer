namespace DataStandardizer.Language
{
    /// <summary>
    /// Scope of an ISO 639 language code.
    /// </summary>
    public enum Iso639LanguageScope
    {
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
        Special
    }
}