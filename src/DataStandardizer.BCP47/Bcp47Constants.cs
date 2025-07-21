namespace DataStandardizer.BCP47
{
    internal static class Bcp47Constants
    {
        internal static class LanguageTagStateName
        {
            internal const string Default = "default";
            internal const string Registry = "registry";
        }

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
    }
}