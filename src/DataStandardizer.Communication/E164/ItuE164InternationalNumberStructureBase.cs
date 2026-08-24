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

        protected const string PatternSeparatorCharacterClass = PatternWhiteSpaceCharacterClass + @"\p{Pd}\p{Po}";
        protected const string PatternWhiteSpaceCharacterClass = @"\p{Zs}\p{Cc}";

        private static readonly Dictionary<(Type, ItuE164InternationalNumberStyles), Regex> ParseExpressions = new Dictionary<(Type, ItuE164InternationalNumberStyles), Regex>();
        private static readonly object ParseExpressionsLock = new object();

        protected internal ItuE164InternationalNumberStructureBase(ulong number)
        {
            Number = number;
        }

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
                patternBuilder.Append($"[{PatternWhiteSpaceCharacterClass}]*");
            }

            // Add international prefix.
            if (numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowInternationalPrefixSymbol))
            {
                patternBuilder.Append($@"\{InternationalPrefixSymbol}?");
            }

            // Add separation.
            patternBuilder.Append(string.Concat("[", PatternWhiteSpaceCharacterClass, "]*"));

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
                patternBuilder.Append($"[{PatternWhiteSpaceCharacterClass}]*");
            }

            patternBuilder.Append("$");

            return patternBuilder.ToString();
        }

        protected static Regex GetParseExpression(Type discriminatorType, ItuE164InternationalNumberStyles numberStyles, Func<ItuE164InternationalNumberStyles, string> getParsePattern)
        {
            // The cache is shared by every number structure type and is populated lazily from
            // ItuE164InternationalNumber.Parse/TryParse, so unsynchronized access would let concurrent
            // callers corrupt the Dictionary. A lock is used in preference to ConcurrentDictionary
            // because the netstandard1.0 target does not offer one; contention is negligible, as the
            // cache saturates after a handful of calls and every later call is a lookup under the lock.
            lock (ParseExpressionsLock)
            {
                if (!ParseExpressions.TryGetValue((discriminatorType, numberStyles), out var parseExpression))
                {
                    var parseExpressionOptions = RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
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
}