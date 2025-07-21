using System.Text.RegularExpressions;

namespace DataStandardizer.BCP47.InternalState
{
    internal class Bcp47RulesBasedVariantSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal const string Pattern = "(?:(?=[a-zA-Z])[a-zA-Z0-9]{5,8}|(?=[0-9])[a-zA-Z0-9]{4,8})";

        static Bcp47RulesBasedVariantSubtagExpressionFactory()
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