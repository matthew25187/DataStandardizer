using System.Text.RegularExpressions;

namespace DataStandardizer.BCP47.InternalState
{
    internal class Bcp47RulesBasedPrivateUseSubtagExpressionFactory : IBcp47ExpressionFactory
    {
        private static readonly RegexOptions ExpressionOptions;
        internal const string Pattern = "x(?:-[0-9a-zA-Z]{1,8})+"; // ref. RFC 5646 §2.2.7

        static Bcp47RulesBasedPrivateUseSubtagExpressionFactory()
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