namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Outcome of parsing a language tag.
    /// </summary>
    internal enum Bcp47ParseStatus
    {
        /// <summary>
        /// The string is not a language tag, or the classifier rejected one of its subtags.
        /// </summary>
        Invalid,

        /// <summary>
        /// The classifier accepted every subtag, but the string repeats a variant subtag or an extension singleton (ref. RFC 5646 §2.2.5¶5, §2.2.6¶3).
        /// The string is well-formed but not valid (ref. RFC 5646 §2.2.9). The subtags are still decomposed.
        /// </summary>
        RepeatedSubtag,

        /// <summary>
        /// The classifier accepted every subtag, and the string repeats none of the subtags that must not repeat.
        /// </summary>
        Accepted
    }

    /// <summary>
    /// Range of characters in a language tag.
    /// </summary>
    internal readonly struct Bcp47SubtagRange
    {
        internal Bcp47SubtagRange(int start, int length)
        {
            Start = start;
            Length = length;
        }

        internal int Start { get; }

        /// <summary>
        /// Number of characters in the range; 0 when the language tag has no such subtag.
        /// </summary>
        internal int Length { get; }

        internal bool IsEmpty => Length == 0;
    }

    /// <summary>
    /// Where each kind of subtag lies in a parsed language tag.
    /// </summary>
    /// <remarks>
    /// Each range covers every subtag of its kind, which are always adjacent, without the leading hyphen; for example the
    /// <see cref="Variants"/> of <c>sl-rozaj-biske-1994</c> are <c>rozaj-biske-1994</c>. A grandfathered tag has no subtags, and a
    /// private use tag has only <see cref="PrivateUse"/>.
    /// </remarks>
    internal struct Bcp47LanguageTagRanges
    {
        internal Bcp47SubtagRange PrimaryLanguage;
        internal Bcp47SubtagRange ExtendedLanguages;
        internal Bcp47SubtagRange Script;
        internal Bcp47SubtagRange Region;
        internal Bcp47SubtagRange Variants;
        internal Bcp47SubtagRange Extensions;
        internal Bcp47SubtagRange PrivateUse;
    }
}
