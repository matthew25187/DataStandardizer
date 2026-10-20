namespace DataStandardizer.LanguageTag
{
    internal static class Bcp47Constants
    {
        /// <summary>
        /// Message of the <see cref="System.ObsoleteAttribute"/> on the overloads that take a match timeout, which removal in v2.0 (#256) follows.
        /// </summary>
        internal const string MatchTimeoutObsoleteMessage = "Language tags are checked by a parser, which cannot time out, so the match timeout has no effect. Use the overload without a matchTimeout. This member will be removed in version 2.0.";

        /// <summary>
        /// Message of the <see cref="System.ObsoleteAttribute"/> on the builder's timeout step.
        /// </summary>
        internal const string WithTimeoutObsoleteMessage = "Language tags are checked by a parser, which cannot time out, so the match timeout has no effect. Leave out the WithTimeout step. This member will be removed in version 2.0.";

        internal static class LanguageTagSubtagGroupName
        {
            internal const string PrimaryLanguage = "PrimaryLanguage";
            internal const string ExtendedLanguage = "ExtendedLanguage";
            internal const string Script = "Script";
            internal const string Region = "Region";
            internal const string Variant = "Variant";
            internal const string Extension = "Extension";
            internal const string PrivateUse = "PrivateUse";
        }

        internal static class RegistrySubtagGroupName
        {
            internal const string Added = "Added";
            internal const string Comments = "Comments";
            internal const string Deprecated = "Deprecated";
            internal const string Description="Description";
            internal const string Macrolanguage = "Macrolanguage";
            internal const string PreferredValue = "PreferredValue";
            internal const string Prefix = "Prefix";
            internal const string Scope = "Scope";
            internal const string Subtag = "Subtag";
            internal const string SuppressScript = "SuppressScript";
            internal const string Tag = "Tag";
            internal const string Type = "Type";
        }

        internal static class RegistrySubtagTypeName
        {
            internal const string PrimaryLanguage = "language";
            internal const string ExtendedLanguage = "extlang";
            internal const string Script = "script";
            internal const string Region = "region";
            internal const string Variant = "variant";
            internal const string Grandfathered = "grandfathered";
        }

        /// <summary>
        /// The grandfathered language tags, irregular then regular (ref. RFC 5646 §2.1, §2.2.8). Each is interpreted as a whole.
        /// </summary>
        internal static readonly string[] GrandfatheredLanguageTags =
        {
            "en-GB-oed", "i-ami", "i-bnn", "i-default", "i-enochian", "i-hak", "i-klingon", "i-lux", "i-mingo", "i-navajo", "i-pwn", "i-tao", "i-tay", "i-tsu",
            "sgn-BE-FR", "sgn-BE-NL", "sgn-CH-DE",
            "art-lojban", "cel-gaulish", "no-bok", "no-nyn", "zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan", "zh-xiang"
        };
    }
}