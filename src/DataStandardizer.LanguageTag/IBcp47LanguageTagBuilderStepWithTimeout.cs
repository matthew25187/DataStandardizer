using System;

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying a match timeout, which has no effect.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithTimeout
    {
        /// <summary>
        /// Build a language tag with a match timeout, which has no effect.
        /// </summary>
        /// <param name="matchTimeout">Not used: language tags are checked by a parser, which cannot time out. Checked when the tag is built, as for <see cref="Bcp47LanguageTag.Create(string, TimeSpan)"/>.</param>
        /// <returns>Language tag builder.</returns>
        [Obsolete(Bcp47Constants.WithTimeoutObsoleteMessage)]
        IBcp47LanguageTagBuilderStepWithTimeoutNext WithTimeout(TimeSpan matchTimeout);
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified a match timeout.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithTimeoutNext : IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistry, IBcp47LanguageTagBuilderStepUsingLanguageTag, IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtag
    {
    }
}