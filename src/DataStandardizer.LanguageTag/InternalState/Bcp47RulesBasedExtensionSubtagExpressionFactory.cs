using System.Text.RegularExpressions;

namespace DataStandardizer.LanguageTag.InternalState
{
    internal class Bcp47RulesBasedExtensionSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal const string Pattern = "(?!x)[0-9a-zA-Z](?:-[0-9a-zA-Z]{2,8})+"; // ref. RFC 5646 §2.2.6

        static Bcp47RulesBasedExtensionSubtagExpressionFactory()
        {
            ExpressionOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            ExpressionOptions |= RegexOptions.Compiled;
#endif
        }

        public Regex Create()
        {
            return new Regex($"^{Pattern}$", ExpressionOptions);
        }

        public string GetPattern()
        {
            return Pattern;
        }
    }
}