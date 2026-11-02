using System.Linq;
using System.Text.RegularExpressions;

namespace DataStandardizer.LanguageTag.IntegrationTests.Parity
{
    /// <summary>
    /// A regular expression transcribed from the ABNF of RFC 5646 §2.1, one production to a constant, as a statement of the grammar that shares nothing with the parser.
    /// </summary>
    /// <remarks>
    /// RFC 5646 §2.2.9: "A tag is considered 'well-formed' if it conforms to the ABNF (Section 2.1)." So this expression decides whether a tag is well-formed, and it is
    /// the reference for the well-formed path. It uses no code list, and it lets a variant subtag or an extension singleton repeat, as the grammar does. Do not change it
    /// to follow the package. ABNF strings are case-insensitive (RFC 5234 §2.3), hence <see cref="RegexOptions.IgnoreCase"/>. That option also lets the classes match
    /// non-ASCII case equivalents such as the Kelvin sign, which the grammar's ALPHA does not, so <see cref="Match"/> rejects any input that isn't ASCII first.
    /// </remarks>
    internal static class Rfc5646AbnfExpression
    {
        // Core rules of RFC 5234 Appendix B.1.
        private const string Alpha = "[A-Za-z]";
        private const string Digit = "[0-9]";

        // alphanum = (ALPHA / DIGIT)
        private const string Alphanum = "[A-Za-z0-9]";

        // extlang = 3ALPHA *2("-" 3ALPHA)
        private const string Extlang = Alpha + "{3}(?:-" + Alpha + "{3}){0,2}";

        // script = 4ALPHA
        private const string Script = Alpha + "{4}";

        // region = 2ALPHA / 3DIGIT
        private const string Region = "(?:" + Alpha + "{2}|" + Digit + "{3})";

        // variant = 5*8alphanum / (DIGIT 3alphanum)
        private const string Variant = "(?:" + Alphanum + "{5,8}|" + Digit + Alphanum + "{3})";

        // singleton = DIGIT / %x41-57 / %x59-5A / %x61-77 / %x79-7A
        private const string Singleton = "[0-9A-WY-Za-wy-z]";

        // extension = singleton 1*("-" (2*8alphanum))
        private const string Extension = Singleton + "(?:-" + Alphanum + "{2,8})+";

        // privateuse = "x" 1*("-" (1*8alphanum))
        private const string PrivateUse = "x(?:-" + Alphanum + "{1,8})+";

        private const RegexOptions ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled;

        // irregular and regular, as enumerated in RFC 5646 §2.1.
        private static readonly string[] Grandfathered =
        {
            "en-GB-oed", "i-ami", "i-bnn", "i-default", "i-enochian", "i-hak", "i-klingon", "i-lux", "i-mingo", "i-navajo", "i-pwn", "i-tao", "i-tay", "i-tsu", "sgn-BE-FR", "sgn-BE-NL", "sgn-CH-DE",
            "art-lojban", "cel-gaulish", "no-bok", "no-nyn", "zh-guoyu", "zh-hakka", "zh-min", "zh-min-nan", "zh-xiang"
        };

        private static readonly Regex LanguageTag = Compose();

        /// <summary>
        /// Match an input against the grammar. The match has the groups named in <see cref="FrozenRulesBasedExpressions"/>.
        /// </summary>
        internal static Match Match(string input)
        {
            return FrozenRulesBasedExpressions.IsAscii(input) ? LanguageTag.Match(input) : System.Text.RegularExpressions.Match.Empty;
        }

        private static Regex Compose()
        {
            // language = 2*3ALPHA ["-" extlang] / 4ALPHA / 5*8ALPHA
            var language = string.Concat(
                "(?:(?<", FrozenRulesBasedExpressions.PrimaryLanguageGroupName, ">", Alpha, "{2,3})(?:-", ExtlangWithCaptures(), ")?",
                "|(?<", FrozenRulesBasedExpressions.PrimaryLanguageGroupName, ">", Alpha, "{4})",
                "|(?<", FrozenRulesBasedExpressions.PrimaryLanguageGroupName, ">", Alpha, "{5,8}))");

            // langtag = language ["-" script] ["-" region] *("-" variant) *("-" extension) ["-" privateuse]
            var langtag = string.Concat(
                language,
                "(?:-(?<", FrozenRulesBasedExpressions.ScriptGroupName, ">", Script, "))?",
                "(?:-(?<", FrozenRulesBasedExpressions.RegionGroupName, ">", Region, "))?",
                "(?:-(?<", FrozenRulesBasedExpressions.VariantGroupName, ">", Variant, "))*",
                "(?:-(?<", FrozenRulesBasedExpressions.ExtensionGroupName, ">", Extension, "))*",
                "(?:-(?<", FrozenRulesBasedExpressions.PrivateUseGroupName, ">", PrivateUse, "))?");

            // Language-Tag = langtag / privateuse / grandfathered
            var languageTag = string.Concat(
                "(?:", string.Join("|", Grandfathered.Select(Regex.Escape)),
                "|(?<", FrozenRulesBasedExpressions.PrivateUseGroupName, ">", PrivateUse, ")",
                "|", langtag, ")");

            return new Regex("^" + languageTag + "\\z", ExpressionOptions);
        }

        /// <summary>
        /// The production <see cref="Extlang"/> with each subtag captured, as the parity tests compare them.
        /// </summary>
        private static string ExtlangWithCaptures()
        {
            var subtag = string.Concat("(?<", FrozenRulesBasedExpressions.ExtendedLanguageGroupName, ">", Alpha, "{3})");
            return string.Concat(subtag, "(?:-", subtag, "){0,2}");
        }
    }
}
