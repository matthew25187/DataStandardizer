using System;
using static DataStandardizer.LanguageTag.Bcp47Constants;

namespace DataStandardizer.LanguageTag.InternalState
{
    /// <summary>
    /// Classifies subtags by position, length and character class alone, which is what a well-formed language tag needs.
    /// </summary>
    /// <remarks>
    /// RFC 5646 §2.2.9: "A tag is considered 'well-formed' if it conforms to the ABNF (Section 2.1). Language tags may be well-formed in terms of syntax but not valid in terms of content."
    /// So nothing here looks a subtag up, and the 26 grandfathered tags that the ABNF enumerates are well-formed whatever a registry says.
    /// Repeated variant subtags and extension singletons match the ABNF too: §2.2.9 lists them under validity alone, so <see cref="Bcp47LanguageTagParser"/>
    /// reports them with <see cref="Bcp47ParseStatus.RepeatedSubtag"/> and the caller decides.
    /// </remarks>
    internal sealed class Bcp47SyntaxSubtagClassifier : Bcp47SubtagClassifier
    {
        internal static readonly Bcp47SyntaxSubtagClassifier Instance = new Bcp47SyntaxSubtagClassifier();

        private static readonly Bcp47SubtagSet GrandfatheredLanguageTagSet = new Bcp47SubtagSet(GrandfatheredLanguageTags);

        private Bcp47SyntaxSubtagClassifier()
        {
        }

        internal override bool IsGrandfathered(ReadOnlySpan<char> languageTag)
        {
            return GrandfatheredLanguageTagSet.Contains(languageTag);
        }

        internal override bool IsPrimaryLanguage(ReadOnlySpan<char> subtag, out bool allowsExtendedLanguage)
        {
            // language = 2*3ALPHA ["-" extlang] / 4ALPHA / 5*8ALPHA (ref. RFC 5646 §2.1)
            allowsExtendedLanguage = false;
            if (subtag.Length < 2 || subtag.Length > 8 || !Bcp47LanguageTagParser.IsAsciiLetters(subtag)) return false;

            allowsExtendedLanguage = subtag.Length <= 3;
            return true;
        }

        internal override bool IsExtendedLanguage(ReadOnlySpan<char> subtag)
        {
            // extlang = 3ALPHA *2("-" 3ALPHA) (ref. RFC 5646 §2.1)
            return subtag.Length == 3 && Bcp47LanguageTagParser.IsAsciiLetters(subtag);
        }

        internal override bool IsScript(ReadOnlySpan<char> subtag)
        {
            // script = 4ALPHA (ref. RFC 5646 §2.1)
            return subtag.Length == 4 && Bcp47LanguageTagParser.IsAsciiLetters(subtag);
        }

        internal override bool IsRegion(ReadOnlySpan<char> subtag)
        {
            // region = 2ALPHA / 3DIGIT (ref. RFC 5646 §2.1)
            if (subtag.Length == 2) return Bcp47LanguageTagParser.IsAsciiLetters(subtag);

            return subtag.Length == 3 && Bcp47LanguageTagParser.IsAsciiDigit(subtag[0]) && Bcp47LanguageTagParser.IsAsciiDigit(subtag[1]) && Bcp47LanguageTagParser.IsAsciiDigit(subtag[2]);
        }

        internal override bool IsVariant(ReadOnlySpan<char> subtag)
        {
            // variant = 5*8alphanum / (DIGIT 3alphanum) (ref. RFC 5646 §2.1)
            if (subtag.Length < 4 || subtag.Length > 8 || !Bcp47LanguageTagParser.IsAsciiLettersOrDigits(subtag)) return false;

            return subtag.Length > 4 || Bcp47LanguageTagParser.IsAsciiDigit(subtag[0]);
        }
    }
}
