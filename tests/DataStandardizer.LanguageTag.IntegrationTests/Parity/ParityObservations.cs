using DataStandardizer.LanguageTag.InternalState;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    // What a parity test compares for each input of the corpus. Each is a struct, so two of them are equal when every field is, with no
    // comparison code of the tests' own.

    /// <summary>
    /// Whether each <c>Check*</c> method accepts an input as a subtag of its kind.
    /// </summary>
    internal readonly struct SubtagCheckResults
    {
        internal SubtagCheckResults(bool primaryLanguage, bool extendedLanguage, bool script, bool region, bool variant, bool extension, bool privateUse)
        {
            PrimaryLanguage = primaryLanguage;
            ExtendedLanguage = extendedLanguage;
            Script = script;
            Region = region;
            Variant = variant;
            Extension = extension;
            PrivateUse = privateUse;
        }

        public bool PrimaryLanguage { get; }

        public bool ExtendedLanguage { get; }

        public bool Script { get; }

        public bool Region { get; }

        public bool Variant { get; }

        public bool Extension { get; }

        public bool PrivateUse { get; }
    }

    /// <summary>
    /// Whether an input is a language tag: as a well-formed check, as a <c>TryCreate</c>, and as the status of a parse.
    /// </summary>
    internal readonly struct LanguageTagValidity
    {
        internal LanguageTagValidity(bool isWellFormed, bool tryCreate, Bcp47ParseStatus status)
        {
            IsWellFormed = isWellFormed;
            TryCreate = tryCreate;
            Status = status;
        }

        public bool IsWellFormed { get; }

        public bool TryCreate { get; }

        public Bcp47ParseStatus Status { get; }
    }

    /// <summary>
    /// The text of each kind of subtag of a language tag, with the subtags of a kind that can repeat joined by hyphens as they are in the tag,
    /// and the empty string for a kind the tag doesn't have.
    /// </summary>
    internal readonly struct SubtagTexts
    {
        internal SubtagTexts(string primaryLanguage, string extendedLanguages, string script, string region, string variants, string extensions, string privateUse)
        {
            PrimaryLanguage = primaryLanguage;
            ExtendedLanguages = extendedLanguages;
            Script = script;
            Region = region;
            Variants = variants;
            Extensions = extensions;
            PrivateUse = privateUse;
        }

        public string PrimaryLanguage { get; }

        public string ExtendedLanguages { get; }

        public string Script { get; }

        public string Region { get; }

        public string Variants { get; }

        public string Extensions { get; }

        public string PrivateUse { get; }
    }
}
