using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace DataStandardizer.Money
{
    public class MoneyFormatter : ICustomFormatter
    {
        private static Regex _formatExpression;

        private static class GroupName
        {
            internal const string currencyCode = "CurrencyCode";
            internal const string precisionSpecifier = "Precision";
        }

        static MoneyFormatter()
        {
            var iso4217CurrencyCodes = Enum.GetValues(typeof(Iso4217CurrencyCurrent))
                .Cast<Iso4217CurrencyCurrent>()
                .Where(code => code.IsNationalCurrency())
                .Select(code => Enum.GetName(typeof(Iso4217CurrencyCurrent), code));
            var iso4217CurrencyCodeSubExpression = string.Join("|", iso4217CurrencyCodes);

            var formatExpressionOptions = RegexOptions.None;
#if NETSTANDARD1_3_OR_GREATER||NET
            formatExpressionOptions |= RegexOptions.Compiled; 
#endif

            _formatExpression = new Regex(string.Concat("^(?<", GroupName.currencyCode, ">", iso4217CurrencyCodeSubExpression, ")(?<", GroupName.precisionSpecifier, @">\d+)?$"), formatExpressionOptions);
        }

#if NETCOREAPP3_0_OR_GREATER
        public string Format(string? format, object? arg, IFormatProvider? formatProvider)
#else
        public string Format(string format, object arg, IFormatProvider formatProvider)
#endif
        {
            throw new NotImplementedException();
        }
    }
}