using System;
using System.Linq;
using System.Text.RegularExpressions;
using static DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants;
using Type = DataStandardizer.LanguageTag.SubtagRegistry.SubtagRegistryConstants.Type;

namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47RegistryBasedVariantSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;

        private readonly SubtagRegistry.SubtagRegistry _subtagRegistry;

        static Bcp47RegistryBasedVariantSubtagExpressionFactory()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif
        }

        public Bcp47RegistryBasedVariantSubtagExpressionFactory(SubtagRegistry.SubtagRegistry subtagRegistry)
        {
            _subtagRegistry = subtagRegistry;
        }

        public Regex Create()
        {
            var pattern = GetPattern();
            return new Regex($"^{pattern}$", ExpressionOptions);
        }

        public string GetPattern()
        {
            var variantSubtags = _subtagRegistry
                .Where(record => record.Any(field => field.Item1 == FieldName.Type && field.Item2.Equals(Type.Variant)))
                .Select(record => record.FirstOrDefault(field => field.Item1 == FieldName.Subtag))
                .Where(field => field != null)
                .Cast<Tuple<string, object>>()
                .Select(field => field.Item2 as string)
                .Where(subtag => subtag != null)
                .Cast<string>();
            return string.Join("|", variantSubtags.Select(Regex.Escape));
        }
    }
}