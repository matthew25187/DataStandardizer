namespace DataStandardizer.BCP47
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for building the language tag.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepBuild
    {
        /// <summary>
        /// Build a BCP 47 language tag.
        /// </summary>
        /// <returns>IETF BCP 47 language tag.</returns>
        Bcp47LanguageTag Build();
    }
}