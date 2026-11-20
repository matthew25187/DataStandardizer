using System;

namespace DataStandardizer.Language
{
    /// <summary>
    /// Type of language represented by an ISO 639 language code.
    /// </summary>
    public enum Iso639LanguageType
    {
        /// <summary>
        /// The language type isn't known.
        /// </summary>
        Unknown,

        /// <summary>
        /// Living languages.
        /// </summary>
        Living,

        /// <summary>
        /// Ancient languages (extinct since ancient times). No longer assigned: ISO 639:2023 merged the
        /// ancient language type into <see cref="Historical"/>, and the ISO 639-3 code table no longer
        /// gives it to any code.
        /// </summary>
        [Obsolete("ISO 639:2023 merged the ancient language type into Historical, and no ISO 639-3 code has it. Use Iso639LanguageType.Historical.")]
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

        /// <summary>
        /// Special codes, which don't identify a language (for example <c>und</c>, <c>mul</c> and <c>zxx</c>).
        /// </summary>
        Special
    }
}