using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
        // White space: the space separators (Unicode category Zs) and the white-space controls U+0009 to U+000D.
        protected const string PatternWhiteSpaceCharacterClass = @"\p{Zs}\t\n\v\f\r";

        private static readonly Dictionary<(Type, ItuE164InternationalNumberStyles), Regex> ParseExpressions = new Dictionary<(Type, ItuE164InternationalNumberStyles), Regex>();
        private static readonly object ParseExpressionsLock = new object();

        protected internal ItuE164InternationalNumberStructureBase(ulong number)
        {
            Number = number;
        }

        public ulong Number { get; }

        /// <summary>
        /// The number of parse expressions in the cache that every number structure type shares.
        /// </summary>
        /// <remarks>
        /// A seam for the integration tests, which check that concurrent parsing caches each expression once.
        /// </remarks>
        internal static int ParseExpressionCount
        {
            get
            {
                lock (ParseExpressionsLock)
                {
                    return ParseExpressions.Count;
                }
            }
        }

        public abstract ushort CountryCode { get; }

        protected static string ComposePatternForParse(ILookup<ushort, ushort> identificationCodeLookup, ItuE164InternationalNumberStyles numberStyles, int subscriberNumberMinimumDigitCount = 1)
        {
            if (subscriberNumberMinimumDigitCount < 0 || subscriberNumberMinimumDigitCount > MaximumDigitCount)
            {
                throw new ArgumentOutOfRangeException(nameof(subscriberNumberMinimumDigitCount), subscriberNumberMinimumDigitCount, $"Subscriber Number Minimum Digits must be a whole number not more than {MaximumDigitCount}.");
            }

            var patternBuilder = new StringBuilder(ComposePatternStart(4, numberStyles));

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

            // Add Subscriber Number field. The quantifier only keeps the field from running away; the
            // digit-count lookahead at the start of the pattern enforces the limit on the whole number.
            if (identificationCodeLookup.Count > 0)
            {
                patternBuilder.Append(string.Concat("(?<", NumberPart.SubscriberNumber, @">\d(?:[", PatternSeparatorCharacterClass, @"]*\d){", subscriberNumberMinimumDigitCount, ",", MaximumDigitCount - 1, "})"));
            }

            patternBuilder.Append(ComposePatternEnd(numberStyles));

            return patternBuilder.ToString();
        }

        /// <summary>
        /// Composes the start of a parse pattern: the anchor, a lookahead that requires
        /// <paramref name="minimumDigitCount"/> to <see cref="MaximumDigitCount"/> digits in the whole input
        /// (ITU-T E.164 §6.1), and the leading white space and international prefix symbol that
        /// <paramref name="numberStyles"/> allows.
        /// </summary>
        protected static string ComposePatternStart(int minimumDigitCount, ItuE164InternationalNumberStyles numberStyles)
        {
            var patternBuilder = new StringBuilder(string.Concat(@"^(?=(?:\D*\d){", minimumDigitCount, ",", MaximumDigitCount, @"}\D*\z)"));

            // Allow leading white space.
            if (numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowLeadingWhite))
            {
                patternBuilder.Append($"[{PatternWhiteSpaceCharacterClass}]*");
            }

            // Allow an international prefix symbol, which white space may separate from the Country Code.
            if (numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowInternationalPrefixSymbol))
            {
                patternBuilder.Append($@"(?:\{InternationalPrefixSymbol}[{PatternWhiteSpaceCharacterClass}]*)?");
            }

            return patternBuilder.ToString();
        }

        /// <summary>
        /// Composes the end of a parse pattern: the trailing white space that <paramref name="numberStyles"/>
        /// allows, and the anchor at the end of the input.
        /// </summary>
        protected static string ComposePatternEnd(ItuE164InternationalNumberStyles numberStyles)
        {
            // \z, not $, which would also match before a final line feed.
            return numberStyles.HasFlag(ItuE164InternationalNumberStyles.AllowTrailingWhite) ? $@"[{PatternWhiteSpaceCharacterClass}]*\z" : @"\z";
        }

        /// <summary>
        /// Pairs each Country Code of <paramref name="countryCodeEnumType"/> with the identification codes of
        /// <paramref name="identificationCodeEnumType"/> whose <see cref="ItuE164SharedCodeAttribute"/> names it.
        /// </summary>
        /// <remarks>
        /// Reads only the members of the identification code enumeration, which are its static fields. Its
        /// instance field, which holds the value of an instance, is not a member and has no shared code.
        /// Members that share a value with the same shared code appear once.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// A member of <paramref name="identificationCodeEnumType"/> has no <see cref="ItuE164SharedCodeAttribute"/>,
        /// which means the generated enumeration is wrong.
        /// </exception>
        internal static ILookup<ushort, ushort> ComposeIdentificationCodeLookup(Type countryCodeEnumType, Type identificationCodeEnumType)
        {
            var countryCodes = new HashSet<ushort>(Enum.GetValues(countryCodeEnumType).Cast<object>().Select(countryCode => Convert.ToUInt16(countryCode, CultureInfo.InvariantCulture)));

            return (
                    from field in identificationCodeEnumType.GetTypeInfo().DeclaredFields
                    where field.IsStatic
                    let sharedCodeAttribute = field.GetCustomAttribute<ItuE164SharedCodeAttribute>()
                        ?? throw new InvalidOperationException($"Identification code '{field.Name}' of {identificationCodeEnumType.Name} has no shared Country Code.")
                    where countryCodes.Contains(sharedCodeAttribute.CountryCode)
                    select new { sharedCodeAttribute.CountryCode, IdentificationCode = Convert.ToUInt16(field.GetValue(null), CultureInfo.InvariantCulture) })
                .Distinct()
                .OrderBy(pair => pair.CountryCode)
                .ThenBy(pair => pair.IdentificationCode)
                .ToLookup(pair => pair.CountryCode, pair => pair.IdentificationCode);
        }

        protected static Regex GetParseExpression(Type discriminatorType, ItuE164InternationalNumberStyles numberStyles, Func<ItuE164InternationalNumberStyles, string> getParsePattern)
        {
            // The cache is shared by every number structure type and is populated lazily from
            // ItuE164InternationalNumber.Parse/TryParse, so unsynchronized access would let concurrent
            // callers corrupt the Dictionary. Contention on the lock is negligible, as the cache
            // saturates after a handful of calls and every later call is a lookup under the lock.
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

        /// <summary>
        /// Empties the cache of parse expressions that every number structure type shares.
        /// </summary>
        /// <remarks>
        /// A seam for the integration tests, which start each concurrency test from a cold cache.
        /// </remarks>
        internal static void ClearParseExpressions()
        {
            lock (ParseExpressionsLock)
            {
                ParseExpressions.Clear();
            }
        }
    }
}