using System.Text.RegularExpressions;

namespace DataStandardizer.BCP47.InternalState
{
    internal class Bcp47RulesBasedExtendedLanguageSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal const string Pattern = "[a-zA-Z]{3}"; // ref. RFC 5646 §2.2.2

        static Bcp47RulesBasedExtendedLanguageSubtagExpressionFactory()
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