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
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif

            // Compose Language Tag expression.
            var primaryLanguageSubtagGroupPattern = $"(?<{LanguageTagSubtagGroupName.PrimaryLanguage}>{Bcp47RulesBasedPrimaryLanguageSubtagExpressionFactory.pattern})";
            var extendedLanguageSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.ExtendedLanguage}>{Bcp47RulesBasedExtendedLanguageSubtagExpressionFactory.Pattern})){{1,3}})";
            var scriptSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.Script}>{Bcp47RulesBasedScriptSubtagExpressionFactory.pattern}))";
            var regionSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.Region}>{Bcp47RulesBasedRegionSubtagExpressionFactory.pattern}))";
            var variantSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.Variant}>{Bcp47RulesBasedVariantSubtagExpressionFactory.Pattern}))+)";
            var extensionSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.Extension}>{Bcp47RulesBasedExtensionSubtagExpressionFactory.Pattern}))+)";
            var privateUseSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.PrivateUse}>{Bcp47RulesBasedPrivateUseSubtagExpressionFactory.Pattern}))";
            pattern = string.Concat("(?:(?<",LanguageTagSubtagGroupName.PrivateUse,">", Bcp47RulesBasedPrivateUseSubtagExpressionFactory.Pattern,")|", primaryLanguageSubtagGroupPattern, extendedLanguageSubtagGroupPattern, "?", scriptSubtagGroupPattern, "?", regionSubtagGroupPattern, "?", variantSubtagGroupPattern, "?",
                extensionSubtagGroupPattern, "?", privateUseSubtagGroupPattern, "?)");
        }

        public Regex Create()
        {
            var pattern = GetPattern();
            return new Regex($"^{pattern}$", ExpressionOptions);
        }

        public string GetPattern()
        {
            return pattern;
        }
    }
}