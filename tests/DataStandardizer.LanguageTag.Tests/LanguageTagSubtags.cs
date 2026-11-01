using System.Collections.Generic;

namespace DataStandardizer.LanguageTag.Tests
{
    /// <summary>
    /// The subtags that a <see cref="Bcp47LanguageTag"/> decomposes into, read from its properties, so a test can compare all of them with one assertion.
    /// </summary>
    public sealed class LanguageTagSubtags
    {
        /// <summary>
        /// The subtags of a tag that has none: a grandfathered tag, which is interpreted as a whole (RFC 5646 §2.2.8).
        /// </summary>
        public static readonly LanguageTagSubtags None = new LanguageTagSubtags("", new string[0], null, null, new string[0], new Bcp47KeyedSubtag[0], null);

        public LanguageTagSubtags(string primaryLanguage, IReadOnlyList<string> extendedLanguages, string script, string region, IReadOnlyList<string> variants,
            IReadOnlyList<Bcp47KeyedSubtag> extensions, Bcp47KeyedSubtag? privateUse)
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

        public IReadOnlyList<string> ExtendedLanguages { get; }

        public string Script { get; }

        public string Region { get; }

        public IReadOnlyList<string> Variants { get; }

        public IReadOnlyList<Bcp47KeyedSubtag> Extensions { get; }

        public Bcp47KeyedSubtag? PrivateUse { get; }

        /// <summary>
        /// Read every subtag property of a language tag.
        /// </summary>
        public static LanguageTagSubtags Of(Bcp47LanguageTag languageTag)
        {
            return new LanguageTagSubtags(languageTag.PrimaryLanguageSubtag, languageTag.ExtendedLanguageSubtags, languageTag.ScriptSubtag, languageTag.RegionSubtag,
                languageTag.VariantSubtags, languageTag.ExtensionSubtags, languageTag.PrivateUseSubtag);
        }
    }
}
