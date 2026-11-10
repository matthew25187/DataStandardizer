namespace DataStandardizer.LanguageTag
{
    internal static class Bcp47Constants
    {
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