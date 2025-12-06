using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static DataStandardizer.LanguageTag.Bcp47Constants;
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;
using Type = DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants.Type;

namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47RegistryBasedLanguageTagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;

        private readonly Dictionary<string, IBcp47ExpressionFactory> _expressionFactories;
        private readonly SubtagRegistry.SubtagRegistry _subtagRegistry;

        static Bcp47RegistryBasedLanguageTagExpressionFactory()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif
        }

        public Bcp47RegistryBasedLanguageTagExpressionFactory(SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            _subtagRegistry = subtagRegistry;
            
            _expressionFactories = new Dictionary<string, IBcp47ExpressionFactory>()
            {
                { Type.PrimaryLanguage, new Bcp47RegistryBasedPrimaryLanguageSubtagExpressionFactory(_subtagRegistry) },
                { Type.ExtendedLanguage, new Bcp47RegistryBasedExtendedLanguageSubtagExpressionFactory(_subtagRegistry) },
                { Type.Script, new Bcp47RegistryBasedScriptSubtagExpressionFactory(_subtagRegistry) },
                { Type.Region, new Bcp47RegistryBasedRegionSubtagExpressionFactory(_subtagRegistry) },
                { Type.Variant, new Bcp47RegistryBasedVariantSubtagExpressionFactory(_subtagRegistry) }
            };
        }

        public Regex Create()
        {
            var pattern = GetPattern();
            return new Regex($"^{pattern}$", ExpressionOptions);
        }

        public string GetPattern()
        {
            // Compose subtag patterns.
            var primaryLanguageSubtagGroupPattern = $"(?<{LanguageTagSubtagGroupName.PrimaryLanguage}>{_expressionFactories[Type.PrimaryLanguage].GetPattern()})";
            var extendedLanguageSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.ExtendedLanguage}>{_expressionFactories[Type.ExtendedLanguage].GetPattern()})){{1,3}})";
            var scriptSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.Script}>{_expressionFactories[Type.Script].GetPattern()}))";
            var regionSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.Region}>{_expressionFactories[Type.Region].GetPattern()}))";
            var variantSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.Variant}>{_expressionFactories[Type.Variant].GetPattern()}))+)";
            var extensionSubtagGroupPattern = $"(?:(?:-(?<{LanguageTagSubtagGroupName.Extension}>{Bcp47RulesBasedExtensionSubtagExpressionFactory.Pattern}))+)";
            var privateUseSubtagGroupPattern = $"(?:-(?<{LanguageTagSubtagGroupName.PrivateUse}>{Bcp47RulesBasedPrivateUseSubtagExpressionFactory.Pattern}))";

            // Compose language tag pattern.
            var grandfatheredLanguageTags = _subtagRegistry
                .Where(record => record.Any(field => field.Item1 == FieldName.Type && field.Item2.Equals(Type.Grandfathered)))
                .Select(record => record.FirstOrDefault(field => field.Item1 == FieldName.Tag))
                .Where(field => field != null)
                .Cast<Tuple<string, object>>()
                .Select(field => field.Item2 as string)
                .Where(subtag => subtag != null)
                .Cast<string>();
            var grandfatheredLanguageTagPattern = string.Join("|", grandfatheredLanguageTags.Select(Regex.Escape));
            var languageTagPattern = string.Concat("(?:",
                grandfatheredLanguageTagPattern ,"|",
                "(?<",LanguageTagSubtagGroupName.PrivateUse,">",Bcp47RulesBasedPrivateUseSubtagExpressionFactory.Pattern,")|",
                primaryLanguageSubtagGroupPattern,
                extendedLanguageSubtagGroupPattern, "?",
                scriptSubtagGroupPattern, "?",
                regionSubtagGroupPattern, "?",
                variantSubtagGroupPattern, "?",
                extensionSubtagGroupPattern, "?",
                privateUseSubtagGroupPattern, "?)");
            return languageTagPattern;
        }
    }
}