using System;
using System.Linq;
using System.Text.RegularExpressions;
using static DataStandardizer.BCP47.SubtagRegistry.SubtagRegistryConstants;
using Type = DataStandardizer.BCP47.SubtagRegistry.SubtagRegistryConstants.Type;

namespace DataStandardizer.BCP47.InternalState
{
    internal class Bcp47RegistryBasedExtendedLanguageSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;

        private readonly SubtagRegistry.SubtagRegistry _subtagRegistry;

        static Bcp47RegistryBasedExtendedLanguageSubtagExpressionFactory()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif
        }

        public Bcp47RegistryBasedExtendedLanguageSubtagExpressionFactory(SubtagRegistry.SubtagRegistry subtagRegistry)
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
            var extendedLanguageSubtags = _subtagRegistry
                .Where(record => record.Any(field => field.Item1 == FieldName.Type && field.Item2.Equals(Type.ExtendedLanguage)))
                .Select(record => record.FirstOrDefault(field => field.Item1 == FieldName.Subtag))
                .Where(field => field != null)
                .Cast<Tuple<string, object>>()
                .Select(field => field.Item2 as string)
                .Where(subtag => subtag != null)
                .Cast<string>();
            return string.Join("|", extendedLanguageSubtags.Select(Regex.Escape));
        }
    }
}