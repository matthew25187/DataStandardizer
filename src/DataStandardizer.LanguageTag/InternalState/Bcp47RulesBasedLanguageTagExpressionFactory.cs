using System.Linq;
using System.Text.RegularExpressions;
using static DataStandardizer.LanguageTag.Bcp47Constants;

namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47RulesBasedLanguageTagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal static readonly string pattern;

        static Bcp47RulesBasedLanguageTagExpressionFactory()
        {
            // Set options to use when creating expressions.
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif

            // Compose Language Tag expression.
            var extendedLanguageSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.ExtendedLanguage}>{Bcp47RulesBasedExtendedLanguageSubtagExpressionFactory.Pattern})){{1,3}})";
            // Only a two- or three-letter Primary Language subtag may be followed by Extended Language subtags (ref. RFC 5646 §2.1, §2.2.2).
            var primaryLanguageSubtagGroupPattern = string.Concat(
                "(?:(?<", LanguageTagSubtagGroupName.PrimaryLanguage, ">", Bcp47RulesBasedPrimaryLanguageSubtagExpressionFactory.ShortPattern, ")", extendedLanguageSubtagGroupPattern, "?",
                "|(?<", LanguageTagSubtagGroupName.PrimaryLanguage, ">", Bcp47RulesBasedPrimaryLanguageSubtagExpressionFactory.LongPattern, "))");
            var scriptSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.Script}>{Bcp47RulesBasedScriptSubtagExpressionFactory.pattern}))";
            var regionSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.Region}>{Bcp47RulesBasedRegionSubtagExpressionFactory.pattern}))";
            var variantSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.Variant}>{Bcp47RulesBasedVariantSubtagExpressionFactory.Pattern}))+)";
            var extensionSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.Extension}>{Bcp47RulesBasedExtensionSubtagExpressionFactory.Pattern}))+)";
            var privateUseSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.PrivateUse}>{Bcp47RulesBasedPrivateUseSubtagExpressionFactory.Pattern}))";
            // Grandfathered tags come first so that the regular ones are matched as a whole rather than as subtags (ref. RFC 5646 §2.2.8).
            var grandfatheredLanguageTagPattern = string.Join("|", GrandfatheredLanguageTags.Select(Regex.Escape));
            pattern = string.Concat("(?:", grandfatheredLanguageTagPattern, "|(?<",LanguageTagSubtagGroupName.PrivateUse,">", Bcp47RulesBasedPrivateUseSubtagExpressionFactory.Pattern,")|", primaryLanguageSubtagGroupPattern, scriptSubtagGroupPattern, "?", regionSubtagGroupPattern, "?", variantSubtagGroupPattern, "?",
                extensionSubtagGroupPattern, "?", privateUseSubtagGroupPattern, "?)");
        }

        public Regex Create()
        {
            var pattern = GetPattern();
            return new Regex($"^{pattern}\\z", ExpressionOptions);
        }

        public string GetPattern()
        {
            return pattern;
        }
    }
}