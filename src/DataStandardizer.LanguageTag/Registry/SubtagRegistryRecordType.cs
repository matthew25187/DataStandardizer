namespace DataStandardizer.LanguageTag.Registry
{
    /// <summary>
    /// The value of the Type field of a Subtag Registry record (ref. RFC 5646 §3.1.3).
    /// </summary>
    public enum SubtagRegistryRecordType
    {
        /// <summary>
        /// The default value, which no record of a loaded <see cref="SubtagRegistry"/> has: a record with no Type field, or with a value that RFC 5646 §3.1.3 doesn't define, fails the load.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// A Primary Language subtag (<c>language</c>).
        /// </summary>
        Language,

        /// <summary>
        /// An Extended Language subtag (<c>extlang</c>).
        /// </summary>
        ExtendedLanguage,

        /// <summary>
        /// A Script subtag (<c>script</c>).
        /// </summary>
        Script,

        /// <summary>
        /// A Region subtag (<c>region</c>).
        /// </summary>
        Region,

        /// <summary>
        /// A Variant subtag (<c>variant</c>).
        /// </summary>
        Variant,

        /// <summary>
        /// A grandfathered tag (<c>grandfathered</c>).
        /// </summary>
        Grandfathered,

        /// <summary>
        /// A redundant tag (<c>redundant</c>).
        /// </summary>
        Redundant,
    }
}
