namespace DataStandardizer.LanguageTag.Registry
{
    internal static class SubtagRegistryConstants
    {
        /// <summary>
        /// The line break that ends every line of the registry, the last one included (ref. RFC 5646 §3.1.1).
        /// </summary>
        internal const string LineBreak = "\r\n";

        /// <summary>
        /// The line that separates records, with its line break (ref. RFC 5646 §3.1.1).
        /// </summary>
        internal const string RecordSeparatorLine = "%%" + LineBreak;

        internal static class FieldName
        {
            internal const string FileDate = "File-Date";
            internal const string Type = "Type";
            internal const string Subtag = "Subtag";
            internal const string Tag = "Tag";
            internal const string Description = "Description";
            internal const string Added = "Added";
            internal const string Deprecated = "Deprecated";
            internal const string PreferredValue = "Preferred-Value";
            internal const string Prefix = "Prefix";
            internal const string SuppressScript = "Suppress-Script";
            internal const string Macrolanguage = "Macrolanguage";
            internal const string Scope = "Scope";
            internal const string Comments = "Comments";
        }

        internal static class Type
        {
            internal const string PrimaryLanguage = "language";
            internal const string ExtendedLanguage = "extlang";
            internal const string Script = "script";
            internal const string Region = "region";
            internal const string Variant = "variant";
            internal const string Grandfathered = "grandfathered";
            internal const string Redundant = "redundant";
        }
    }
}