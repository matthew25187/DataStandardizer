using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static DataStandardizer.Communication.E164.ItuE164Constants;

namespace DataStandardizer.Communication.E164
{
    internal abstract class ItuE164InternationalNumberStructureBase : IItuE164InternationalNumber
    {
        protected static class NumberPart
        {
            internal const string CountryCode = "CountryCode";
            internal const string IdentificationCode = "IdentificationCode";
            internal const string SubscriberNumber = "SubscriberNumber";
        }

        private static readonly Dictionary<(Type, ItuE164InternationalNumberStyles), Regex> ParseExpressions = new Dictionary<(Type, ItuE164InternationalNumberStyles), Regex>();

        protected internal ItuE164InternationalNumberStructureBase(ulong number)
        {
            Number = number;
        }

        protected const string PatternSeparatorCharacterClass = @"\p{Zs}\p{Pd}\p{Po}";

        public ulong Number { get; }

        public abstract ushort CountryCode { get; }

        protected static string ComposePatternForParse(ILookup<ushort, ushort> identificationCodeLookup, ItuE164InternationalNumberStyles numberStyles, int subscriberNumberMinimumDigitCount = 1)
        {
            if (subscriberNumberMinimumDigitCount < 0 || subscriberNumberMinimumDigitCount > MaximumDigitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(subscriberNumberMinimumDigitCount), subscriberNumberMinimumDigitCount, $"Subscriber Number Minimum Digits must be a whole number not more than {MaximumDigitCount}.");
            }

            var patternBuilder = new StringBuilder(string.Concat(@"^(?=(?:\D*\d){4,", MaximumDigitCount, @"}\D*$)"));

            // Allow leading white space.
            if (numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowLeadingWhite))
            {
                patternBuilder.Append(@"\p{Zs}*");
            }

            // Add international prefix.
            if (numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowInternationalPrefixSymbol))
            {
                patternBuilder.Append($@"\{InternationalPrefixSymbol}?");
            }

            // Add separation.
            patternBuilder.Append(string.Concat("[", PatternSeparatorCharacterClass, "]*"));

            // Add Country Code/Identification Code combinations.
            var countryCodePatterns = new List<string>();
            foreach (var kvp in identificationCodeLookup)
            {
                var countryCodePattern = string.Concat("(?<", NumberPart.CountryCode, ">", kvp.Key, ")[", PatternSeparatorCharacterClass, "]*(?<", NumberPart.IdentificationCode, ">", string.Join("|", kvp), ")");
                countryCodePatterns.Add(countryCodePattern);
            }

            patternBuilder.Append(string.Concat("(?:", string.Join("|", countryCodePatterns), ")"));

            // Add separation.
            patternBuilder.Append(string.Concat("[", PatternSeparatorCharacterClass, "]*"));

            // Add Subscriber Number field.
            if (identificationCodeLookup.Count > 0)
            {
                patternBuilder.Append(string.Concat("(?<", NumberPart.SubscriberNumber, @">\d(?:[", PatternSeparatorCharacterClass, @"]*\d){", subscriberNumberMinimumDigitCount, ",", MaximumDigitCount - 1, "})"));
            }

            // Allow trailing white space.
            if (numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowTrailingWhite))
            {
                patternBuilder.Append(@"\p{Zs}*");
            }

            patternBuilder.Append("$");

            return patternBuilder.ToString();
        }

        protected static Regex GetParseExpression(Type discriminatorType, ItuE164InternationalNumberStyles numberStyles, Func<ItuE164InternationalNumberStyles, string> getParsePattern)
        {
            if (!ParseExpressions.TryGetValue((discriminatorType, numberStyles), out var parseExpression))
            {
                var parseExpressionOptions = RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER
                parseExpressionOptions |= RegexOptions.Compiled;
#endif
                var parsePattern = getParsePattern(numberStyles);
                parseExpression = new Regex(parsePattern, parseExpressionOptions);
                ParseExpressions.Add((discriminatorType, numberStyles), parseExpression);
            }

            return parseExpression;
        }
    }
}