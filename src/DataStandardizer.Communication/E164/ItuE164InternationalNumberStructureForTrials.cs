using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
#if NETSTANDARD
using JetBrains.Annotations; 
#endif

namespace DataStandardizer.Communication.E164
{
    internal sealed class ItuE164InternationalNumberStructureForTrials : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForTrials
    {
        private static class NumberPart
        {
            internal const string CountryCode = "CountryCode";
            internal const string TrialIdentificationCode = "TrialIdentificationCode";
            internal const string SubscriberNumber = "SubscriberNumber";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;
        private static readonly Regex InternationalNumberExpression;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForTrials()
        {
            // Compose patterns for numbers with and without spacing.
            var internationalNumberPatternBuilder = new StringBuilder();
            var isFirst = true;
            var subPatterns =
                from countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForTrials)).Cast<ushort>()
                from trialIdentificationCode in Enum.GetValues(typeof(ItuE164AssignedTrialIdentificationCodesForTrials)).Cast<ItuE164AssignedTrialIdentificationCodesForTrials>()
                let sharedCodeAttribute = typeof(ItuE164AssignedTrialIdentificationCodesForTrials).GetTypeInfo().DeclaredFields
                    .Single(field => field.GetValue(null)?.Equals(trialIdentificationCode) ?? false)
                    .GetCustomAttribute<ItuE164SharedCodeAttribute>()
                where sharedCodeAttribute.CountryCode == countryCode
                select string.Concat("(?<", NumberPart.CountryCode, ">", countryCode, @")\s+(?<", NumberPart.TrialIdentificationCode, ">", (byte)trialIdentificationCode, @")\s+(?<", NumberPart.SubscriberNumber,
                    @">\d{,11})");
            foreach (var subPattern in subPatterns)
            {
                if (!isFirst) internationalNumberPatternBuilder.Append("|");
                internationalNumberPatternBuilder.Append(subPattern);

                isFirst = false;
            }

            // Compose expressions for parsing a number.
            var expressionOptions = RegexOptions.Singleline;
#if NETSTANDARD1_3_OR_GREATER||NET
            expressionOptions |= RegexOptions.Compiled;
#endif
            var internationalNumberPattern = internationalNumberPatternBuilder.ToString();
            InternationalNumberExpression = new Regex(string.Concat(@"^(?:\+\s*)?(?:", !string.IsNullOrEmpty(internationalNumberPattern) ? internationalNumberPattern : @"\d{4,15}", ")"), expressionOptions);
        }

        internal ItuE164InternationalNumberStructureForTrials(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, out ItuE164InternationalNumberStructureForTrials? result)
#else
        internal static bool TryParse(string s, [CanBeNull] out ItuE164InternationalNumberStructureForTrials result)
#endif
        {
            var isParsed = false;

            var parseMatch = InternationalNumberExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var trialIdentificationCodePart = parseMatch.Groups[NumberPart.TrialIdentificationCode].Value;
                var subscriberNumberPart = parseMatch.Groups[NumberPart.SubscriberNumber].Value;
                var number = ulong.Parse(Regex.Replace(countryCodePart + trialIdentificationCodePart + subscriberNumberPart, @"\s", string.Empty), NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForTrials(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.TrialIdentificationCode, trialIdentificationCodePart },
                        { NumberPart.SubscriberNumber, subscriberNumberPart }
                    }
                };
                isParsed = true;
            }
            else
            {
                result = null;
            }

            return isParsed;
        }

        public override ushort CountryCode => DoGetCountryCode();

        public ItuE164AssignedTrialIdentificationCodesForTrials TrialIdentificationCode => DoGetTrialIdentificationCode();

        public ItuE164SubscriberNumber? SubscriberNumber => DoGetSubscriberNumber();

        private ushort DoGetCountryCode()
        {
            // First, try to get delineated Country Code.
            if (_numberParts.TryGetValue(NumberPart.CountryCode, out var countryCodePart))
            {
                return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
            }

            // Second, extract Country Code from whole number.
            countryCodePart = Number.ToString().Substring(0, 3);
            return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
        }

        private ItuE164SubscriberNumber? DoGetSubscriberNumber()
        {
            // First, try to get delineated Subscriber Number.
            if (_numberParts.TryGetValue(NumberPart.SubscriberNumber, out var subscriberNumberPart))
            {
                return new ItuE164SubscriberNumber(subscriberNumberPart);
            }

            // Second, extract Subscriber Number from whole number.
            subscriberNumberPart = Number > 9999 ? Number.ToString().Substring(4) : null;
            return subscriberNumberPart != null ? new ItuE164SubscriberNumber(subscriberNumberPart) : default(ItuE164SubscriberNumber?);
        }

        private ItuE164AssignedTrialIdentificationCodesForTrials DoGetTrialIdentificationCode()
        {
            // First, try to get delineated Trial Identification Code.
            if (_numberParts.TryGetValue(NumberPart.TrialIdentificationCode, out var trialIdentificationCodePart))
            {
                return (ItuE164AssignedTrialIdentificationCodesForTrials)Enum.Parse(typeof(ItuE164AssignedTrialIdentificationCodesForTrials), trialIdentificationCodePart);
            }

            // Second, extract Trial Identification Code from whole number.
            trialIdentificationCodePart = Number.ToString().Substring(3, 1);
            return Enum.TryParse(trialIdentificationCodePart, out ItuE164AssignedTrialIdentificationCodesForTrials trialIdentificationCode) ? trialIdentificationCode : default;
        }
    }
}