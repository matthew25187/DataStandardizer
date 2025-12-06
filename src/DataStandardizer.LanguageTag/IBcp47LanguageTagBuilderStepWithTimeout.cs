using System;

namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// BCP 47 Language Tag Builder step for specifying a regular expression match timeout.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithTimeout
    {
        /// <summary>
        /// Build a language tag with a match timeout for the regular expressions.
        /// </summary>
        /// <param name="matchTimeout">Time limit for regular expressions to complete evaluation.</param>
        /// <returns>Language tag builder.</returns>
        IBcp47LanguageTagBuilderStepWithTimeoutNext WithTimeout(TimeSpan matchTimeout);
    }

    /// <summary>
    /// BCP 47 Language Tag Builder steps that follow from having specified a match timeout.
    /// </summary>
    public interface IBcp47LanguageTagBuilderStepWithTimeoutNext : IBcp47LanguageTagBuilderStepWithLanguageSubtagRegistry, IBcp47LanguageTagBuilderStepUsingLanguageTag, IBcp47LanguageTagBuilderStepUsingPrimaryLanguageSubtag
    {
    }
}