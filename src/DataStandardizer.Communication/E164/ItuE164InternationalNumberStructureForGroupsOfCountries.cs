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
    internal sealed class ItuE164InternationalNumberStructureForGroupsOfCountries : ItuE164InternationalNumberStructureBase, IItuE164InternationalNumberForGroupsOfCountries
    {
        private static class NumberPart
        {
            internal const string CountryCode = "CountryCode";
            internal const string GroupIdentificationCode = "GroupIdentificationCode";
            internal const string SubscriberNumber = "SubscriberNumber";
        }

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.None;
        private static readonly Regex InternationalNumberExpression;

        private Dictionary<string, string> _numberParts = new Dictionary<string, string>();

        static ItuE164InternationalNumberStructureForGroupsOfCountries()
        {
            // Compose patterns for numbers with and without spacing.
            var internationalNumberPatternBuilder = new StringBuilder();
            var isFirst = true;
            var subPatterns = from countryCode in Enum.GetValues(typeof(ItuE164AssignedCountryCodesForGroupsOfCountries)).Cast<ushort>()
                from groupIdentificationCode in Enum.GetValues(typeof(ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries)).Cast<ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries>()
                let sharedCodeAttribute = typeof(ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries).GetTypeInfo().DeclaredFields
                    .Single(field => field.GetValue(null)?.Equals(groupIdentificationCode) ?? false)
                    .GetCustomAttribute<ItuE164SharedCodeAttribute>()
                where sharedCodeAttribute.CountryCode == countryCode
                select string.Concat("(?<", NumberPart.CountryCode, ">", countryCode, @")\s*(?<", NumberPart.GroupIdentificationCode, ">", (byte)groupIdentificationCode, @")\s*(?<", NumberPart.SubscriberNumber,
                    @">\d{1,11})");
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
            InternationalNumberExpression = new Regex(string.Concat(@"^(?:\+\s*)?(?:", !string.IsNullOrEmpty(internationalNumberPattern) ? internationalNumberPattern : @"\d{5,15}", ")"), expressionOptions);
        }

        internal ItuE164InternationalNumberStructureForGroupsOfCountries(ulong number) : base(number)
        {
        }

#if NETCOREAPP3_0_OR_GREATER
        internal static bool TryParse(string s, out ItuE164InternationalNumberStructureForGroupsOfCountries? result)
#else
        internal static bool TryParse(string s, [CanBeNull] out ItuE164InternationalNumberStructureForGroupsOfCountries result)
#endif
        {
            var isParsed = false;

            var parseMatch = InternationalNumberExpression.Match(s);
            if (parseMatch.Success)
            {
                var countryCodePart = parseMatch.Groups[NumberPart.CountryCode].Value;
                var groupIdentificationCodePart = parseMatch.Groups[NumberPart.GroupIdentificationCode].Value;
                var subscriberNumberPart = parseMatch.Groups[NumberPart.SubscriberNumber].Value;
                var numberWhole = new string(string.Concat(countryCodePart, groupIdentificationCodePart, subscriberNumberPart).ToCharArray().Where(character => !char.IsWhiteSpace(character)).ToArray());
                var number = ulong.Parse(numberWhole, NumberStyles, CultureInfo.InvariantCulture);
                result = new ItuE164InternationalNumberStructureForGroupsOfCountries(number)
                {
                    _numberParts = new Dictionary<string, string>
                    {
                        { NumberPart.CountryCode, countryCodePart },
                        { NumberPart.GroupIdentificationCode, groupIdentificationCodePart },
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

        public ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries GroupIdentificationCode => DoGetGroupIdentificationCode();

        public ItuE164SubscriberNumber SubscriberNumber => DoGetSubscriberNumber();

        private ushort DoGetCountryCode()
        {
            // First, try to get the delineated Country Code.
            if (_numberParts.TryGetValue(NumberPart.CountryCode, out var countryCodePart))
            {
                return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
            }

            // Second, extract the Country Code from the whole number.
            countryCodePart = Number.ToString().Substring(0, 3);
            return ushort.Parse(countryCodePart, NumberStyles, CultureInfo.InvariantCulture);
        }

        private ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries DoGetGroupIdentificationCode()
        {
            if (!_numberParts.TryGetValue(NumberPart.GroupIdentificationCode, out var groupIdentificationCodePart))
            {
                groupIdentificationCodePart = Number.ToString().Substring(3, 1);
            }

            return Enum.TryParse(groupIdentificationCodePart, out ItuE164AssignedGroupIdentificationCodesForGroupsOfCountries groupIdentificationCode) ? groupIdentificationCode : default;
        }

        private ItuE164SubscriberNumber DoGetSubscriberNumber()
        {
            // First, try to get the delineated Subscriber Number.
            if (_numberParts.TryGetValue(NumberPart.SubscriberNumber, out var subscriberNumberPart))
            {
                return new ItuE164SubscriberNumber(subscriberNumberPart);
            }

            // Second, extract the Subscriber Number from the whole number.
            subscriberNumberPart = Number.ToString().Substring(4);
            return new ItuE164SubscriberNumber(subscriberNumberPart);
        }
    }
}