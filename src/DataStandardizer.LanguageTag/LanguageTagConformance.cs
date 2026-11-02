namespace DataStandardizer.LanguageTag
{
    /// <summary>
    /// What was checked when a language tag was created.
    /// </summary>
    /// <remarks>
    /// RFC 5646 §2.2.9 defines two classes of conformance. <see cref="WellFormed"/> and <see cref="Valid"/> are those classes.
    /// <see cref="IsoCodeLists"/> is not defined by the RFC: it is an approximation of validity for a caller that cannot load a Subtag Registry.
    /// </remarks>
    public enum LanguageTagConformance
    {
        /// <summary>
        /// The language tag matches the ABNF of RFC 5646 §2.1, and nothing else was checked: "A tag is considered 'well-formed' if it conforms to the ABNF" (RFC 5646 §2.2.9).
        /// </summary>
        /// <remarks>
        /// A well-formed language tag may use subtags that nothing has assigned, such as <c>zz</c>, and may repeat a variant subtag or an extension singleton, such as <c>de-1901-1901</c>.
        /// The 26 grandfathered language tags of RFC 5646 §2.1 are well-formed.
        /// </remarks>
        WellFormed,

        /// <summary>
        /// The language tag is well-formed, its primary language, script and region subtags are codes in the ISO 639, ISO 15924, ISO 3166-1 and UN M.49 lists compiled into the
        /// <c>DataStandardizer.Language</c> and <c>DataStandardizer.Geography</c> packages, or codes that RFC 5646 reserves for local or private use, and it repeats no variant subtag or extension singleton.
        /// </summary>
        /// <remarks>
        /// This is an approximation of <see cref="Valid"/> for a caller that cannot load a Subtag Registry, and it is not RFC 5646 validity.
        /// It does not check variant subtags, primary language subtags of four to eight letters, or the prefixes of extended language subtags,
        /// and its lists are only as current as those packages.
        /// A primary language subtag is an ISO 639-1 code, an ISO 639-2/T or ISO 639-3 code of a language with no ISO 639-1 code, an ISO 639-5 code, or in <c>qaa</c> to <c>qtz</c>:
        /// a three-letter code of a language that has an ISO 639-1 code, such as <c>eng</c>, is rejected (RFC 5646 §2.2.1).
        /// So is <c>hbs</c>: the IANA registry defines <c>sh</c> for Serbo-Croatian and not <c>hbs</c>, although <c>sh</c> is not an ISO 639-1 code, so this opt-in accepts neither.
        /// A script subtag is an ISO 15924 code or in <c>Qaaa</c> to <c>Qabx</c>. A region subtag is an ISO 3166-1 code, a code that §2.2.4 reserves, or a UN M.49 area other than 830.
        /// </remarks>
        IsoCodeLists,

        /// <summary>
        /// The language tag is valid against a Subtag Registry (RFC 5646 §2.2.9): it is well-formed, either it is a grandfathered tag in the registry or
        /// all of its primary language, extended language, script, region and variant subtags appear in the registry, and it repeats no variant subtag or extension singleton.
        /// </summary>
        /// <remarks>
        /// The <c>Prefix</c> and <c>Deprecated</c> fields of the registry's records are not checked, because §2.2.9 does not list them.
        /// </remarks>
        Valid
    }
}
